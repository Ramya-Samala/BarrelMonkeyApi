namespace BarrelMonkeyApi.Models;

// A monkey. Lives in a barrel (one barrel can have many monkeys).
public class Monkey
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Every monkey deserves a name
    public string Name { get; set; } = string.Empty;

    // Species — because not all monkeys are created equal
    public string Species { get; set; } = "Unknown";

    // Age in years (approximate — monkeys aren't great at keeping records)
    public int AgeYears { get; set; }

    // The barrel this monkey calls home. Nullable because a monkey might be
    // in between barrels.
    public Guid? BarrelId { get; set; }

    // Is this monkey currently "active" (alive, tracked)?
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
