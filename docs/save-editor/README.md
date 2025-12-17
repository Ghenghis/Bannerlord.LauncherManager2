# Bannerlord Save Editor

A comprehensive save game editing library for Mount & Blade II: Bannerlord, including support for the War Sails (Naval DLC) expansion.

## Features

- **Save File Parsing**: Read and parse Bannerlord save files (`.sav`)
- **Character Editing**: Modify hero stats, skills, attributes, and perks
- **Party Management**: Edit troops, prisoners, and party gold
- **Fleet Editing**: Full War Sails support for ships and naval operations
- **Backup System**: Automatic backup with compression and integrity verification
- **Validation**: Comprehensive validation to prevent save corruption

## Installation

### NuGet Package

```bash
dotnet add package Bannerlord.SaveEditor.Core
dotnet add package Bannerlord.SaveEditor.Backup
```

### From Source

```bash
git clone https://github.com/Ghenghis/Bannerlord.LauncherManager2.git
cd Bannerlord.LauncherManager2
dotnet build Bannerlord.SaveEditor.sln -c Release
```

## Quick Start

```csharp
using Bannerlord.SaveEditor.Core.Services;
using Bannerlord.SaveEditor.Core.Parsers;

// Create parser and load save
var parser = new SaveParser();
var save = await parser.LoadAsync("path/to/save.sav");

// Edit character
var characterEditor = new CharacterEditor();
characterEditor.SetGold(save.Heroes[0], 100000);
characterEditor.SetLevel(save.Heroes[0], 30);
characterEditor.SetAttribute(save.Heroes[0], AttributeType.Vigor, 10);

// Save changes
var writer = new SaveWriter();
await writer.SaveAsync(save, "path/to/save.sav");
```

## Documentation

- [Architecture](ARCHITECTURE.md) - System architecture and design
- [API Specification](API_SPEC.md) - Complete API reference
- [Save File Format](SAVE_FILE_FORMAT.md) - Technical format details
- [War Sails Extension](WAR_SAILS_EXTENSION.md) - Naval DLC support
- [Backup Failsafe](BACKUP_FAILSAFE.md) - Backup system details
- [Editor Features](EDITOR_FEATURES.md) - Complete feature list
- [Changelog](CHANGELOG.md) - Version history

## Requirements

- .NET 8.0 or later
- Mount & Blade II: Bannerlord (for save files)
- War Sails DLC (optional, for naval features)

## License

MIT License - see [LICENSE](../../LICENSE) for details.
