namespace BarrelMonkeyApi.Models;

//  BARREL DTOs

// What you send when creating a new barrel. Id, timestamps, and monkey list
// are all assigned server-side

public class CreateBarrelRequest
{
    public string Name { get; set; } = string.Empty;
    public string Material { get; set; } = "Oak";
    public int CapacityLiters { get; set; }
}

// What you send to update an existing barrel.
// All fields are optional — only the ones you send will be updated.
public class UpdateBarrelRequest
{
    public string? Name { get; set; }
    public string? Material { get; set; }
    public int? CapacityLiters { get; set; }
    public bool? IsActive { get; set; }
}

//  MONKEY DTOs

// What you send when adding a new monkey.
// BarrelId is optional — the monkey will be unassigned until you move them in.
public class CreateMonkeyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = "Unknown";
    public int AgeYears { get; set; }
    public Guid? BarrelId { get; set; }
}


// What you send to update monkey details.
// Same partial-update pattern as barrels — only send what changed.

public class UpdateMonkeyRequest
{
    public string? Name { get; set; }
    public string? Species { get; set; }
    public int? AgeYears { get; set; }
    public Guid? BarrelId { get; set; }  // set to null to "evict" a monkey from its barrel
    public bool? IsActive { get; set; }
}
