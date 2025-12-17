# Save Editor API Specification

## ISaveService

Primary interface for save file operations.

### Methods

#### DiscoverSavesAsync
```csharp
Task<IReadOnlyList<SaveFileInfo>> DiscoverSavesAsync(CancellationToken ct = default);
```
Discovers all save files in the default saves directory.

#### GetSaveInfoAsync
```csharp
Task<SaveFileInfo> GetSaveInfoAsync(string path, CancellationToken ct = default);
```
Gets metadata about a specific save file without loading full content.

#### LoadAsync
```csharp
Task<SaveFile> LoadAsync(string path, LoadOptions? options = null, CancellationToken ct = default);
```
Loads a complete save file for editing.

#### SaveAsync
```csharp
Task SaveAsync(SaveFile save, string? path = null, SaveOptions? options = null, CancellationToken ct = default);
```
Saves changes to a save file.

#### ValidateAsync
```csharp
Task<ValidationReport> ValidateAsync(SaveFile save, CancellationToken ct = default);
```
Validates a save file for integrity and consistency.

#### VerifyIntegrityAsync
```csharp
Task<bool> VerifyIntegrityAsync(string path, CancellationToken ct = default);
```
Verifies the integrity of a save file on disk.

### Events

- `SaveLoaded`: Raised when a save is loaded
- `SaveSaving`: Raised before a save is written
- `SaveSaved`: Raised after a save is written

## IBackupService

Interface for backup management.

### Methods

#### CreateSnapshotAsync
```csharp
Task<BackupInfo> CreateSnapshotAsync(SaveFile save, BackupTrigger trigger, CancellationToken ct = default);
Task<BackupInfo> CreateSnapshotAsync(string savePath, BackupTrigger trigger, CancellationToken ct = default);
```
Creates a backup snapshot of a save file.

#### RestoreAsync
```csharp
Task RestoreAsync(BackupInfo backup, string targetPath, CancellationToken ct = default);
```
Restores a backup to the specified path.

#### GetBackupsAsync
```csharp
Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(string? savePath = null, CancellationToken ct = default);
```
Gets all backups, optionally filtered by original save path.

#### GetLatestBackupAsync
```csharp
Task<BackupInfo?> GetLatestBackupAsync(string savePath, CancellationToken ct = default);
```
Gets the most recent backup for a save file.

#### PruneBackupsAsync
```csharp
Task<int> PruneBackupsAsync(RetentionPolicy policy, CancellationToken ct = default);
```
Prunes old backups according to retention policy.

## CharacterEditor

Service for character editing (synchronous methods).

### Methods

#### SetLevel
```csharp
void SetLevel(HeroData hero, int level);
```
Sets the hero's level.

#### SetGold
```csharp
void SetGold(HeroData hero, int gold);
```
Sets the hero's gold amount.

#### SetAttribute
```csharp
void SetAttribute(HeroData hero, AttributeType attribute, int value);
```
Sets a specific attribute value.

#### SetSkill
```csharp
void SetSkill(HeroData hero, SkillType skill, int value);
```
Sets a skill level (0-300).

#### UnlockPerk / RemovePerk
```csharp
void UnlockPerk(HeroData hero, string perkId);
void RemovePerk(HeroData hero, string perkId);
```
Manages hero perks.

#### SetNavalSkill
```csharp
void SetNavalSkill(HeroData hero, NavalSkillType skill, int value);
```
Sets naval skill level (War Sails).

#### ExportTemplate / ImportTemplate
```csharp
CharacterTemplate ExportTemplate(HeroData hero);
void ImportTemplate(HeroData hero, CharacterTemplate template, TemplateImportOptions options);
```
Export/import character templates.

## FleetEditor (War Sails)

Service for fleet and ship editing (synchronous methods).

### Methods

#### RepairShip
```csharp
void RepairShip(ShipData ship);
```
Restores ship hull to maximum.

#### AddUpgrade / RemoveUpgrade
```csharp
void AddUpgrade(ShipData ship, ShipUpgrade upgrade);
void RemoveUpgrade(ShipData ship, ShipUpgrade upgrade);
```
Manages ship upgrades.

#### SetCrewCount
```csharp
void SetCrewCount(ShipData ship, int count);
```
Sets the crew count.

#### TransferShip
```csharp
void TransferShip(ShipData ship, FleetData sourceFleet, FleetData targetFleet);
```
Transfers ship between fleets.

#### SetFlagship
```csharp
void SetFlagship(FleetData fleet, ShipData ship);
```
Sets the fleet flagship.

## Data Models

### SaveFile
```csharp
public class SaveFile
{
    public string FilePath { get; set; }
    public SaveHeader Header { get; set; }
    public SaveMetadata Metadata { get; set; }
    public CampaignData Campaign { get; set; }
    public HeroData MainHero { get; set; }
    public IReadOnlyList<HeroData> Heroes { get; set; }
    public IReadOnlyList<PartyData> Parties { get; set; }
    public IReadOnlyList<FleetData> Fleets { get; set; } // War Sails
    public bool HasWarSails { get; set; }
}
```

### HeroData
```csharp
public class HeroData
{
    public MBGUID Id { get; set; }
    public string Name { get; set; }
    public string HeroId { get; set; }
    public int Level { get; set; }
    public int Age { get; set; }
    public int Gold { get; set; }
    public int Health { get; set; }
    public HeroState State { get; set; }
    public HeroAttributes Attributes { get; set; }
    public SkillSet Skills { get; set; }
    public NavalSkillSet? NavalSkills { get; set; } // War Sails
    public ISet<string> UnlockedPerks { get; set; }
    public AppearanceData? Appearance { get; set; }
    public bool IsAlive { get; set; }
}
```

### FleetData (War Sails)
```csharp
public class FleetData
{
    public MBGUID Id { get; set; }
    public string Name { get; set; }
    public HeroData Admiral { get; set; }
    public IList<ShipData> Ships { get; set; }
    public NavalPosition Position { get; set; }
    public FleetState State { get; set; }
}
```

### ShipData (War Sails)
```csharp
public class ShipData
{
    public MBGUID Id { get; set; }
    public string Name { get; set; }
    public ShipType Type { get; set; }
    public int CurrentHullPoints { get; set; }
    public int MaxHullPoints { get; set; }
    public int CrewCount { get; set; }
    public int CrewCapacity { get; set; }
    public float CargoCapacity { get; set; }
    public float CurrentCargoWeight { get; set; }
    public float CrewMorale { get; set; }
    public ISet<ShipUpgrade> Upgrades { get; set; }
}
```
