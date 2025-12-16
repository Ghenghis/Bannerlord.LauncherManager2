# Bannerlord Save Editor - Editable Fields Reference

This document lists all fields that can be safely edited in Bannerlord save files, including War Sails (NavalDLC) saves.

## Save File Format

Bannerlord saves use a **JSON header + compressed data** format:
- **Bytes 0-3**: Header length (little-endian int32)
- **Bytes 4-N**: JSON header containing metadata
- **Bytes N+1-end**: Compressed game data (ZLIB)

## Editable Header Fields

These fields are in the JSON header and can be safely modified:

### Hero Stats

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `MainHeroLevel` | int | Character level | `25` |
| `MainHeroGold` | int | Gold/Denars | `99999` |
| `HealthPercentage` | int | Current health % | `100` |
| `CharacterName` | string | Hero name | `"Garmi"` |

### Party Stats

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `MainPartyFood` | int | Food supplies | `100` |
| `MainPartyHealthyMemberCount` | int | Healthy troops | `150` |
| `MainPartyPrisonerMemberCount` | int | Prisoners | `25` |
| `MainPartyWoundedMemberCount` | int | Wounded troops | `10` |

### Clan Stats

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `ClanInfluence` | int | Political influence | `500` |
| `ClanFiefs` | int | Number of fiefs | `3` |

### Game State

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `DayLong` | int | Days elapsed | `365` |
| `IronmanMode` | int | Ironman enabled (0/1) | `0` |

## Read-Only Fields (Do Not Edit)

These fields should NOT be modified as they may corrupt the save:

| Field | Reason |
|-------|--------|
| `Modules` | Module list must match installed mods |
| `ApplicationVersion` | Game version validation |
| `CreationTime` | Timestamp for save ordering |
| `UniqueGameId` | Save identification |
| `Version` | Save format version |
| `ClanBannerCode` | Banner visual data |
| `MainHeroVisual` | Character appearance data |
| `Module_*` | Individual module versions |

## War Sails (NavalDLC) Specific Data

When NavalDLC is active, the save contains additional naval data:

### In Header
- `Modules` will include `NavalDLC`
- `Module_NavalDLC` shows the naval DLC version (e.g., `v1.0.7.104956`)

### In Compressed Data (Advanced)
The compressed data section contains:
- **Fleet data**: Ship collections and fleet commanders
- **Ship data**: Hull, crew, cargo, upgrades
- **Naval settlements**: Ports and harbors
- **Naval characters**: Crew and captains

> **Note**: Editing compressed data requires decompression, modification, and recompression. The header fields above are the safe entry point for basic edits.

## Backup Strategy

Always create backups before editing:

```csharp
// Create timestamped backup
var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
var backupPath = Path.Combine(BackupDir, $"{saveName}_backup_{timestamp}.sav");
File.Copy(originalPath, backupPath);
```

## Code Example: Edit Gold

```csharp
// 1. Read save
var rawData = File.ReadAllBytes(savePath);
var headerLength = BitConverter.ToInt32(rawData, 0);
var headerJson = Encoding.UTF8.GetString(rawData, 4, headerLength);
var header = JsonNode.Parse(headerJson).AsObject();

// 2. Modify gold
header["List"]["MainHeroGold"] = "99999";

// 3. Write modified save
var newHeaderJson = header.ToJsonString();
var newHeaderBytes = Encoding.UTF8.GetBytes(newHeaderJson);

using var stream = File.Create(savePath);
using var writer = new BinaryWriter(stream);
writer.Write(newHeaderBytes.Length);           // New header length
writer.Write(newHeaderBytes);                   // New header
writer.Write(rawData, 4 + headerLength,         // Original compressed data
             rawData.Length - 4 - headerLength);
```

## Testing Verification

After editing, verify the save loads correctly:

1. **Header check**: Re-read and parse JSON header
2. **Field check**: Verify edited values match
3. **Game test**: Load save in-game to confirm functionality

## Save Locations

| Platform | Path |
|----------|------|
| Steam (Windows) | `%USERPROFILE%\Documents\Mount and Blade II Bannerlord\Game Saves` |
| Steam (OneDrive) | `%USERPROFILE%\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves` |
| Xbox Game Pass | `%LOCALAPPDATA%\Packages\TaleWorldsEntertainment...\LocalState` |

## Related Files

- `SaveEditorTests.cs` - Basic save parsing tests
- `SaveEditorAdvancedTests.cs` - Edit/backup/verify workflow tests
- `LauncherManagerHandler.SaveEditor.cs` - Core save editor implementation
- `SaveEditor.cs` (Models) - Data models for save entities
