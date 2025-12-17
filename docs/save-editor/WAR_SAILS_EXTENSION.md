# War Sails Extension Support

## Overview

The Save Editor provides full support for the War Sails (Naval DLC) expansion, enabling editing of ships, fleets, and naval-specific features.

## Detecting War Sails Saves

```csharp
var save = await saveService.LoadAsync(path);
if (save.HasWarSails)
{
    // Naval features are available
    var fleets = save.Fleets;
}
```

## Naval Data Structures

### FleetData

```csharp
public class FleetData
{
    public MBGUID Id { get; set; }
    public string Name { get; set; }
    public HeroData Admiral { get; set; }
    public MBGUID? AdmiralId { get; set; }
    public ClanData Clan { get; set; }
    public IList<ShipData> Ships { get; set; }
    public ShipData Flagship { get; set; }
    public NavalPosition Position { get; set; }
    public SeaRegion CurrentRegion { get; set; }
    public Port CurrentPort { get; set; }
    public FleetState State { get; set; }
    
    // Computed properties
    public int TotalCrewCount { get; }
    public int TotalCargoCapacity { get; }
    public int TotalCargoWeight { get; }
}
```

### ShipData

```csharp
public class ShipData
{
    public MBGUID Id { get; set; }
    public string Name { get; set; }
    public ShipType Type { get; set; }
    
    // Hull
    public int CurrentHull { get; set; }
    public int MaxHull { get; set; }
    public float HullPercentage { get; }
    
    // Crew
    public int CrewCount { get; set; }
    public int MaxCrew { get; set; }
    public int MinCrew { get; set; }
    
    // Cargo
    public int CargoCapacity { get; set; }
    public int CurrentCargoWeight { get; set; }
    public IList<CargoItem> Cargo { get; set; }
    
    // Upgrades
    public ISet<ShipUpgrade> Upgrades { get; set; }
    
    // Combat
    public int CannonCount { get; set; }
    public int BallistaCount { get; set; }
    public bool HasRam { get; set; }
}
```

### Ship Types

```csharp
public enum ShipType
{
    Cog,           // Small trade ship
    Galley,        // Oar-powered warship
    Longship,      // Nordic raider
    Carrack,       // Large cargo ship
    Warship,       // Heavy combat vessel
    Trireme,       // Classical warship
    Dhow,          // Arabian trader
    Junk           // Eastern vessel
}
```

### Ship Upgrades

```csharp
public enum ShipUpgrade
{
    ReinforcedHull,
    ExpandedCargo,
    ImprovedSails,
    GreekFireDefense,
    ReinforcedRam,
    ExtraCannons,
    MarineBunks,
    NavigationEquipment,
    MedicalBay,
    SecretCompartment
}
```

## Fleet Editor Operations

### Repair All Ships

```csharp
var fleetEditor = new FleetEditor();
foreach (var ship in fleet.Ships)
{
    await fleetEditor.RepairShipAsync(ship);
}
```

### Add Ship Upgrade

```csharp
await fleetEditor.AddUpgradeAsync(ship, ShipUpgrade.ReinforcedHull);
```

### Modify Crew

```csharp
await fleetEditor.SetCrewCountAsync(ship, ship.MaxCrew);
```

### Transfer Cargo

```csharp
await fleetEditor.TransferCargoAsync(sourceShip, targetShip, cargoItem, quantity);
```

## Validation Rules

### Ship Constraints
- Hull cannot exceed MaxHull
- Crew must be between MinCrew and MaxCrew
- Cargo cannot exceed CargoCapacity
- Upgrades must be compatible with ship type

### Fleet Constraints
- Fleet must have at least one ship
- Admiral must be a valid hero
- Flagship must be in the fleet's ship list

## Example: Full Fleet Edit

```csharp
// Load save with War Sails
var save = await saveService.LoadAsync(path);
if (!save.HasWarSails) return;

var fleetEditor = new FleetEditor();

// Find player's main fleet
var playerFleet = save.Fleets.FirstOrDefault(f => 
    f.Clan?.Leader?.IsMainHero == true);

if (playerFleet != null)
{
    foreach (var ship in playerFleet.Ships)
    {
        // Repair all ships
        await fleetEditor.RepairShipAsync(ship);
        
        // Max out crew
        await fleetEditor.SetCrewCountAsync(ship, ship.MaxCrew);
        
        // Add reinforced hull if not present
        if (!ship.Upgrades.Contains(ShipUpgrade.ReinforcedHull))
        {
            await fleetEditor.AddUpgradeAsync(ship, ShipUpgrade.ReinforcedHull);
        }
    }
}

// Save changes
await saveService.SaveAsync(save);
```
