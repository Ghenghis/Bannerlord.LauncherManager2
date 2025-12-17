# Bannerlord Save File Format

## Overview

Bannerlord save files (`.sav`) use a binary format with a JSON header and ZLIB-compressed game data.

## File Structure

```
┌────────────────────────────────────────┐
│ Magic Number (4 bytes): "TWSV"         │
├────────────────────────────────────────┤
│ Header Version (4 bytes): int32 LE     │
├────────────────────────────────────────┤
│ Header Length (4 bytes): int32 LE      │
├────────────────────────────────────────┤
│ JSON Header (N bytes)                  │
├────────────────────────────────────────┤
│ Compressed Length (4 bytes): int32 LE  │
├────────────────────────────────────────┤
│ Uncompressed Length (4 bytes): int32 LE│
├────────────────────────────────────────┤
│ ZLIB Compressed Data (M bytes)         │
└────────────────────────────────────────┘
```

## Magic Number

- Bytes: `0x54 0x57 0x53 0x56` ("TWSV")
- Purpose: Identifies the file as a valid Bannerlord save

## Header Version

- Current version: 7 (as of game version 1.2.x)
- Used to maintain backward compatibility

## JSON Header

Contains metadata about the save:

```json
{
  "Version": 7,
  "Modules": "Native*v1.2.9;SandBoxCore*v1.2.9;Sandbox*v1.2.9;...",
  "CharacterName": "Ghenghis",
  "MainHeroLevel": 25,
  "MainHeroGold": 50000,
  "ClanName": "dey Arryn",
  "ClanInfluence": 250,
  "MainPartyFood": 45,
  "MainPartyHealthyMemberCount": 125,
  "MainPartyPrisonerMemberCount": 12,
  "MainPartyWoundedMemberCount": 8,
  "DayLong": 365,
  "IronmanMode": 0,
  "ApplicationVersion": "v1.2.9.38523",
  "CreationTime": "2024-01-15T14:30:00"
}
```

## Compressed Data Section

### Compression Format
- Algorithm: ZLIB (RFC 1950)
- Compression Level: Optimal (default)

### Data Structure
The decompressed data contains serialized game objects:

```
┌─────────────────────────────────────────┐
│ Campaign Data                           │
│  ├── Time (day, season, year)           │
│  ├── Map state                          │
│  └── Global modifiers                   │
├─────────────────────────────────────────┤
│ Heroes                                  │
│  ├── Main hero                          │
│  ├── Companions                         │
│  ├── Family members                     │
│  └── All NPCs                           │
├─────────────────────────────────────────┤
│ Parties                                 │
│  ├── Player party                       │
│  ├── Lord parties                       │
│  └── Bandit parties                     │
├─────────────────────────────────────────┤
│ Settlements                             │
│  ├── Towns                              │
│  ├── Castles                            │
│  └── Villages                           │
├─────────────────────────────────────────┤
│ Kingdoms & Clans                        │
├─────────────────────────────────────────┤
│ Quests                                  │
├─────────────────────────────────────────┤
│ Fleets (War Sails only)                 │
│  ├── Player fleets                      │
│  ├── Ships                              │
│  └── Naval settlements                  │
└─────────────────────────────────────────┘
```

## Editable Header Fields

Safe to modify in the JSON header:

| Field | Type | Description |
|-------|------|-------------|
| `MainHeroLevel` | int | Character level |
| `MainHeroGold` | int | Gold/Denars |
| `MainPartyFood` | int | Food supplies |
| `ClanInfluence` | int | Political influence |

## Read-Only Fields

Do NOT modify these fields:

| Field | Reason |
|-------|--------|
| `Modules` | Must match installed mods |
| `ApplicationVersion` | Game version validation |
| `Version` | Save format version |
| `CreationTime` | Timestamp integrity |

## Code Example: Reading Header

```csharp
using var stream = File.OpenRead(savePath);
using var reader = new BinaryReader(stream);

// Verify magic number
var magic = reader.ReadBytes(4);
if (!magic.SequenceEqual(new byte[] { 0x54, 0x57, 0x53, 0x56 }))
    throw new InvalidOperationException("Not a valid save file");

// Read header version
var headerVersion = reader.ReadInt32();

// Read JSON header
var headerLength = reader.ReadInt32();
var headerJson = Encoding.UTF8.GetString(reader.ReadBytes(headerLength));
var header = JsonSerializer.Deserialize<SaveHeader>(headerJson);
```

## War Sails Detection

Check for War Sails (Naval DLC) presence:

```csharp
bool hasWarSails = header.Modules.Contains("NavalDLC") ||
                   header.Modules.Contains("WarSails");
```
