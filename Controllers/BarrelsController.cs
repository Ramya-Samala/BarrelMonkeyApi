using Microsoft.AspNetCore.Mvc;
using BarrelMonkeyApi.Models;
using BarrelMonkeyApi.Services;
using BarrelMonkeyApi.Auth;

namespace BarrelMonkeyApi.Controllers;

// Everything you need to manage barrels.
// Barrels are the "parent" side of the relationship — each barrel can have many monkeys.
[ApiController]
[Route("api/[controller]")]
[ApiKeyRequired]  // all barrel endpoints require a valid API key
[Produces("application/json")]
public class BarrelsController : ControllerBase
{
    private readonly FileDataStore _store;
    private readonly ILogger<BarrelsController> _logger;

    public BarrelsController(FileDataStore store, ILogger<BarrelsController> logger)
    {
        _store = store;
        _logger = logger;
    }

    // GET /api/barrels 
    // Returns all barrels in the system.
    [HttpGet]
    [ProducesResponseType(typeof(List<Barrel>), 200)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Fetching all barrels");
        var barrels = await _store.GetAllBarrelsAsync();
        return Ok(barrels);
    }

    // GET /api/barrels/{id}
    // Returns a single barrel by ID, including its monkey list.
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Barrel), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var barrel = await _store.GetBarrelByIdAsync(id);
        if (barrel is null)
        {
            _logger.LogWarning("Barrel {Id} not found", id);
            return NotFound(new { message = $"No barrel found with id {id}" });
        }

        return Ok(barrel);
    }

    // GET /api/barrels/{id}/monkeys 
    // Returns all monkeys currently living in the specified barrel.
    // Handy shortcut to filter monkeys by barrel.
    [HttpGet("{id:guid}/monkeys")]
    [ProducesResponseType(typeof(List<Monkey>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMonkeys(Guid id)
    {
        var barrel = await _store.GetBarrelByIdAsync(id);
        if (barrel is null)
            return NotFound(new { message = $"No barrel found with id {id}" });

        var monkeys = await _store.GetMonkeysByBarrelAsync(id);
        _logger.LogInformation("Barrel {Id} has {Count} monkeys", id, monkeys.Count);
        return Ok(monkeys);
    }

    // POST /api/barrels 
    //Creates a new barrel. Returns the created barrel with its assigned ID.
    [HttpPost]
    [ProducesResponseType(typeof(Barrel), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateBarrelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Barrel name is required" });

        if (request.CapacityLiters <= 0)
            return BadRequest(new { message = "Capacity must be greater than zero" });

        var barrel = new Barrel
        {
            Name = request.Name.Trim(),
            Material = request.Material.Trim(),
            CapacityLiters = request.CapacityLiters,
        };

        await _store.SaveBarrelAsync(barrel);
        _logger.LogInformation("Created new barrel '{Name}' with id {Id}", barrel.Name, barrel.Id);

        // 201 Created with a Location header pointing to the new resource
        return CreatedAtAction(nameof(GetById), new { id = barrel.Id }, barrel);
    }

    // PUT /api/barrels/{id} 
    // Updates an existing barrel. Only the fields you include will be changed —
    // omitted fields keep their current values.
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Barrel), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBarrelRequest request)
    {
        var barrel = await _store.GetBarrelByIdAsync(id);
        if (barrel is null)
            return NotFound(new { message = $"No barrel found with id {id}" });

        // Only apply changes for fields that were actually sent
        if (request.Name is not null) barrel.Name = request.Name.Trim();
        if (request.Material is not null) barrel.Material = request.Material.Trim();
        if (request.CapacityLiters.HasValue) barrel.CapacityLiters = request.CapacityLiters.Value;
        if (request.IsActive.HasValue) barrel.IsActive = request.IsActive.Value;

        barrel.UpdatedAt = DateTime.UtcNow;

        await _store.SaveBarrelAsync(barrel);
        _logger.LogInformation("Updated barrel {Id}", id);
        return Ok(barrel);
    }

    // DELETE /api/barrels/{id} 
    // Deletes a barrel. Note: this does NOT automatically delete the monkeys inside —
    // they'll just become barrel-less. Consider reassigning or deleting them separately.
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _store.DeleteBarrelAsync(id);
        if (!deleted)
            return NotFound(new { message = $"No barrel found with id {id}" });

        // When a barrel is deleted, orphan any monkeys that were inside it
        var orphanedMonkeys = await _store.GetMonkeysByBarrelAsync(id);
        foreach (var monkey in orphanedMonkeys)
        {
            monkey.BarrelId = null;
            monkey.UpdatedAt = DateTime.UtcNow;
            await _store.SaveMonkeyAsync(monkey);
        }

        if (orphanedMonkeys.Count > 0)
            _logger.LogInformation("Barrel {Id} deleted — {Count} monkeys are now unassigned", id, orphanedMonkeys.Count);
        else
            _logger.LogInformation("Barrel {Id} deleted", id);

        return NoContent();
    }
}
