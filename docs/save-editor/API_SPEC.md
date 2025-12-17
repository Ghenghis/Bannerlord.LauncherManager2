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

## ICharacterEditor

Interface for character editing.

### Methods

#### SetLevelAsync
```csharp
Task<EditResult> SetLevelAsync(HeroData hero, int level, CancellationToken ct = default);
```

#### SetGoldAsync
```csharp
Task<EditResult> SetGoldAsync(HeroData hero, int gold, CancellationToken ct = default);
```

#### SetAttributeAsync
```csharp
Task<EditResult> SetAttributeAsync(HeroData hero, AttributeType attribute, int value, CancellationToken ct = default);
```

#### SetSkillLevelAsync
```csharp
Task<EditResult> SetSkillLevelAsync(HeroData hero, string skillId, int level, CancellationToken ct = default);
```

#### AddPerkAsync
```csharp
Task<EditResult> AddPerkAsync(HeroData hero, string perkId, CancellationToken ct = default);
```

## IFleetEditor (War Sails)

Interface for fleet and ship editing.

### Methods

#### RepairShipAsync
```csharp
Task<EditResult> RepairShipAsync(ShipData ship, CancellationToken ct = default);
```

#### AddUpgradeAsync
```csharp
Task<EditResult> AddUpgradeAsync(ShipData ship, ShipUpgrade upgrade, CancellationToken ct = default);
```

#### SetCrewCountAsync
```csharp
Task<EditResult> SetCrewCountAsync(ShipData ship, int count, CancellationToken ct = default);
```

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
    public int Level { get; set; }
    public int Experience { get; set; }
    public int Gold { get; set; }
    public HeroAttributes Attributes { get; set; }
    public SkillSet Skills { get; set; }
    public ISet<string> Perks { get; set; }
    public bool IsMainHero { get; set; }
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
    public int CurrentHull { get; set; }
    public int MaxHull { get; set; }
    public int CrewCount { get; set; }
    public int MaxCrew { get; set; }
    public ISet<ShipUpgrade> Upgrades { get; set; }
}
```
