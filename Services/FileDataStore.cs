using System.Text.Json;
using BarrelMonkeyApi.Models;

namespace BarrelMonkeyApi.Services;

// Handles reading and writing our data to local JSON files.

public class FileDataStore
{
    private readonly string _dataDirectory;
    private readonly string _barrelsFile;
    private readonly string _monkeysFile;
    private readonly ILogger<FileDataStore> _logger;

    // In-memory cache — loaded once, kept in sync with disk
    private List<Barrel>? _barrels;
    private List<Monkey>? _monkeys;

    // Thread-safety for concurrent requests — a simple lock is enough here
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public FileDataStore(IConfiguration config, ILogger<FileDataStore> logger)
    {
        _logger = logger;

        // Data directory is configurable, defaults to a "data" folder next to the executable
        _dataDirectory = config["DataDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "data");
        _barrelsFile = Path.Combine(_dataDirectory, "barrels.json");
        _monkeysFile = Path.Combine(_dataDirectory, "monkeys.json");

        // Make sure the directory exists — create it if not
        Directory.CreateDirectory(_dataDirectory);
        _logger.LogInformation("Data store initialized. Files will live in: {Dir}", _dataDirectory);
    }

    // BARREL OPERATIONS 

    public async Task<List<Barrel>> GetAllBarrelsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _barrels ??= await LoadFromFileAsync<Barrel>(_barrelsFile);
            return _barrels.ToList(); // return a copy so callers can't mutate the cache directly
        }
        finally { _lock.Release(); }
    }

    public async Task<Barrel?> GetBarrelByIdAsync(Guid id)
    {
        var all = await GetAllBarrelsAsync();
        return all.FirstOrDefault(b => b.Id == id);
    }

    public async Task<Barrel> SaveBarrelAsync(Barrel barrel)
    {
        await _lock.WaitAsync();
        try
        {
            _barrels ??= await LoadFromFileAsync<Barrel>(_barrelsFile);

            // Replace existing or add new
            var existing = _barrels.FindIndex(b => b.Id == barrel.Id);
            if (existing >= 0)
                _barrels[existing] = barrel;
            else
                _barrels.Add(barrel);

            await PersistToFileAsync(_barrelsFile, _barrels);
            _logger.LogDebug("Barrel {Id} saved to disk", barrel.Id);
            return barrel;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> DeleteBarrelAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            _barrels ??= await LoadFromFileAsync<Barrel>(_barrelsFile);
            var removed = _barrels.RemoveAll(b => b.Id == id);
            if (removed > 0)
            {
                await PersistToFileAsync(_barrelsFile, _barrels);
                _logger.LogDebug("Barrel {Id} deleted from disk", id);
            }
            return removed > 0;
        }
        finally { _lock.Release(); }
    }

    //  MONKEY OPERATIONS 

    public async Task<List<Monkey>> GetAllMonkeysAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _monkeys ??= await LoadFromFileAsync<Monkey>(_monkeysFile);
            return _monkeys.ToList();
        }
        finally { _lock.Release(); }
    }

    public async Task<Monkey?> GetMonkeyByIdAsync(Guid id)
    {
        var all = await GetAllMonkeysAsync();
        return all.FirstOrDefault(m => m.Id == id);
    }

    public async Task<List<Monkey>> GetMonkeysByBarrelAsync(Guid barrelId)
    {
        var all = await GetAllMonkeysAsync();
        return all.Where(m => m.BarrelId == barrelId).ToList();
    }

    public async Task<Monkey> SaveMonkeyAsync(Monkey monkey)
    {
        await _lock.WaitAsync();
        try
        {
            _monkeys ??= await LoadFromFileAsync<Monkey>(_monkeysFile);

            var existing = _monkeys.FindIndex(m => m.Id == monkey.Id);
            if (existing >= 0)
                _monkeys[existing] = monkey;
            else
                _monkeys.Add(monkey);

            await PersistToFileAsync(_monkeysFile, _monkeys);
            _logger.LogDebug("Monkey {Id} saved to disk", monkey.Id);
            return monkey;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> DeleteMonkeyAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            _monkeys ??= await LoadFromFileAsync<Monkey>(_monkeysFile);
            var removed = _monkeys.RemoveAll(m => m.Id == id);
            if (removed > 0)
                await PersistToFileAsync(_monkeysFile, _monkeys);
            return removed > 0;
        }
        finally { _lock.Release(); }
    }

    // PRIVATE HELPERS 

    private async Task<List<T>> LoadFromFileAsync<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            // No file yet — that's fine, just means we start fresh
            _logger.LogInformation("Data file not found at {Path}, starting with empty list", filePath);
            return new List<T>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
        }
        catch (Exception ex)
        {
            // If the file is corrupt or unreadable, log it and start fresh rather than crashing
            _logger.LogError(ex, "Failed to read data file {Path}, starting with empty list", filePath);
            return new List<T>();
        }
    }

    private async Task PersistToFileAsync<T>(string filePath, List<T> items)
    {
        var json = JsonSerializer.Serialize(items, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }
}
