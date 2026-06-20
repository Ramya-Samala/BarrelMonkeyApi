using Microsoft.AspNetCore.Mvc;
using BarrelMonkeyApi.Models;
using BarrelMonkeyApi.Services;
using BarrelMonkeyApi.Auth;

namespace BarrelMonkeyApi.Controllers;

/// <summary>
/// Everything you need to manage monkeys.
/// A monkey belongs to at most one barrel at a time (the "child" side of the relationship).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiKeyRequired]
[Produces("application/json")]
public class MonkeysController : ControllerBase
{
    private readonly FileDataStore _store;
    private readonly ILogger<MonkeysController> _logger;

    public MonkeysController(FileDataStore store, ILogger<MonkeysController> logger)
    {
        _store = store;
        _logger = logger;
    }

    // GET /api/monkeys 
    // Returns all monkeys. Optionally filter by barrel using ?barrelId=...
    [HttpGet]
    [ProducesResponseType(typeof(List<Monkey>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? barrelId)
    {
        List<Monkey> monkeys;

        if (barrelId.HasValue)
        {
            // Scoped to a specific barrel — useful for barrel management UIs
            monkeys = await _store.GetMonkeysByBarrelAsync(barrelId.Value);
            _logger.LogInformation("Fetching monkeys for barrel {BarrelId} — found {Count}", barrelId, monkeys.Count);
        }
        else
        {
            monkeys = await _store.GetAllMonkeysAsync();
            _logger.LogInformation("Fetching all monkeys — found {Count}", monkeys.Count);
        }

        return Ok(monkeys);
    }

    //  GET /api/monkeys/{id} 
    // Returns a single monkey by ID.
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Monkey), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var monkey = await _store.GetMonkeyByIdAsync(id);
        if (monkey is null)
        {
            _logger.LogWarning("Monkey {Id} not found", id);
            return NotFound(new { message = $"No monkey found with id {id}" });
        }

        return Ok(monkey);
    }

    //POST /api/monkeys 
    // Creates a new monkey. Optionally assign them to a barrel right away via barrelId.
    // If barrelId is provided but the barrel doesn't exist, the request will fail.
    [HttpPost]
    [ProducesResponseType(typeof(Monkey), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Create([FromBody] CreateMonkeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Monkey name is required" });

        if (request.AgeYears < 0)
            return BadRequest(new { message = "Age can't be negative — even for very old monkeys" });

        // If a barrelId was specified, make sure that barrel actually exists
        if (request.BarrelId.HasValue)
        {
            var barrel = await _store.GetBarrelByIdAsync(request.BarrelId.Value);
            if (barrel is null)
                return NotFound(new { message = $"Can't assign monkey to barrel {request.BarrelId} — that barrel doesn't exist" });
        }

        var monkey = new Monkey
        {
            Name = request.Name.Trim(),
            Species = request.Species.Trim(),
            AgeYears = request.AgeYears,
            BarrelId = request.BarrelId,
        };

        await _store.SaveMonkeyAsync(monkey);

        // If the monkey got assigned to a barrel, update that barrel's MonkeyIds list too
        if (monkey.BarrelId.HasValue)
            await AddMonkeyToBarrelIndex(monkey.Id, monkey.BarrelId.Value);

        _logger.LogInformation("Created monkey '{Name}' (id: {Id}) in barrel {BarrelId}", monkey.Name, monkey.Id, monkey.BarrelId?.ToString() ?? "none");
        return CreatedAtAction(nameof(GetById), new { id = monkey.Id }, monkey);
    }

    //  PUT /api/monkeys/{id} 
    // Updates an existing monkey. Partial updates are supported.
    // To move a monkey to a different barrel, just send the new barrelId.
    // To make a monkey barrel-less, explicitly send barrelId as null.
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Monkey), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMonkeyRequest request)
    {
        var monkey = await _store.GetMonkeyByIdAsync(id);
        if (monkey is null)
            return NotFound(new { message = $"No monkey found with id {id}" });

        var previousBarrelId = monkey.BarrelId;

        if (request.Name is not null) monkey.Name = request.Name.Trim();
        if (request.Species is not null) monkey.Species = request.Species.Trim();
        if (request.AgeYears.HasValue) monkey.AgeYears = request.AgeYears.Value;
        if (request.IsActive.HasValue) monkey.IsActive = request.IsActive.Value;

        // Handle barrel reassignment — need to update the index on both old and new barrel
        if (request.BarrelId != monkey.BarrelId)
        {
            // Validate the new barrel if one was provided
            if (request.BarrelId.HasValue)
            {
                var newBarrel = await _store.GetBarrelByIdAsync(request.BarrelId.Value);
                if (newBarrel is null)
                    return NotFound(new { message = $"Target barrel {request.BarrelId} doesn't exist" });
            }

            // Remove from old barrel's index
            if (previousBarrelId.HasValue)
                await RemoveMonkeyFromBarrelIndex(monkey.Id, previousBarrelId.Value);

            // Add to new barrel's index (if there is one)
            monkey.BarrelId = request.BarrelId;
            if (monkey.BarrelId.HasValue)
                await AddMonkeyToBarrelIndex(monkey.Id, monkey.BarrelId.Value);
        }

        monkey.UpdatedAt = DateTime.UtcNow;
        await _store.SaveMonkeyAsync(monkey);

        _logger.LogInformation("Updated monkey {Id} — barrel: {OldBarrel} → {NewBarrel}",
            id, previousBarrelId?.ToString() ?? "none", monkey.BarrelId?.ToString() ?? "none");

        return Ok(monkey);
    }

    //  DELETE /api/monkeys/{id} 
    // Deletes a monkey and removes it from its barrel's index.
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var monkey = await _store.GetMonkeyByIdAsync(id);
        if (monkey is null)
            return NotFound(new { message = $"No monkey found with id {id}" });

        // Clean up the barrel's index before deleting
        if (monkey.BarrelId.HasValue)
            await RemoveMonkeyFromBarrelIndex(monkey.Id, monkey.BarrelId.Value);

        await _store.DeleteMonkeyAsync(id);
        _logger.LogInformation("Deleted monkey {Id} ('{Name}')", id, monkey.Name);
        return NoContent();
    }

    // PRIVATE HELPERS 

    // Keeps barrel.MonkeyIds in sync when monkeys move in
    private async Task AddMonkeyToBarrelIndex(Guid monkeyId, Guid barrelId)
    {
        var barrel = await _store.GetBarrelByIdAsync(barrelId);
        if (barrel is not null && !barrel.MonkeyIds.Contains(monkeyId))
        {
            barrel.MonkeyIds.Add(monkeyId);
            await _store.SaveBarrelAsync(barrel);
        }
    }

    // Keeps barrel.MonkeyIds in sync when monkeys move out
    private async Task RemoveMonkeyFromBarrelIndex(Guid monkeyId, Guid barrelId)
    {
        var barrel = await _store.GetBarrelByIdAsync(barrelId);
        if (barrel is not null)
        {
            barrel.MonkeyIds.Remove(monkeyId);
            await _store.SaveBarrelAsync(barrel);
        }
    }
}
