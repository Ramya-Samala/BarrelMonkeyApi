namespace BarrelMonkeyApi.Models;


// A barrel. Could be full of water, wine or monkeys.
// Each barrel can hold many monkeys (one-to-many relationship).
public class Barrel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // What kind of barrel is this? A name is helpful for distinguishing them.
    public string Name { get; set; } = string.Empty;

    // Material the barrel is made of — mostly for flavor/categorization
    public string Material { get; set; } = "Oak";

    // How many liters this thing can hold
    public int CapacityLiters { get; set; }

    // Whether it's currently in active use or just sitting around
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation: all the monkeys currently assigned to this barrel
    // We store monkey IDs here to keep the file persistence simple (no FK joins)
    public List<Guid> MonkeyIds { get; set; } = new();
}
