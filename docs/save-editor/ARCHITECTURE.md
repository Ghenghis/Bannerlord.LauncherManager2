# Save Editor Architecture

## Overview

The Bannerlord Save Editor is designed as a modular, service-oriented library that can be integrated into various applications including CLI tools, desktop applications, and web services.

## Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Application Layer                         │
│  (CLI Tool, Desktop App, Web Service, Vortex Extension)         │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Service Layer                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ SaveService │  │ BackupService│  │ ValidationService       │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Editor Layer                              │
│  ┌───────────────┐  ┌─────────────┐  ┌─────────────────────┐    │
│  │CharacterEditor│  │ PartyEditor │  │ FleetEditor         │    │
│  └───────────────┘  └─────────────┘  └─────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Parser Layer                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ SaveParser  │  │ SaveWriter  │  │ ZlibHandler             │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Model Layer                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  SaveFile   │  │  HeroData   │  │ FleetData (War Sails)   │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Core Components

### SaveService
Primary service for save file operations:
- Discovery of save files
- Loading and saving
- Integrity verification
- Event notifications

### BackupService
Automatic backup management:
- Pre-edit snapshots
- Compression (GZip, LZ4)
- Retention policies
- Integrity verification

### ValidationService
Comprehensive validation:
- Data integrity checks
- Reference validation
- Constraint verification
- Error reporting

### Editors
Specialized editing services:
- **CharacterEditor**: Hero stats, skills, perks
- **PartyEditor**: Troops, prisoners, inventory
- **FleetEditor**: Ships, upgrades, naval operations

### Parsers
Low-level file handling:
- **SaveParser**: Read and decompress save files
- **SaveWriter**: Compress and write save files
- **ZlibHandler**: ZLIB compression/decompression

## Data Flow

```
Load Flow:
  File → SaveParser → Decompress → Parse JSON → Build Models → SaveFile

Save Flow:
  SaveFile → Validate → Serialize → Compress → Write File → Verify
```

## Dependency Injection

```csharp
services.AddSaveEditor(options =>
{
    options.SaveDirectory = customPath;
    options.AutoBackupOnSave = true;
});
```

## Thread Safety

All services are designed to be thread-safe:
- Immutable models where possible
- Async/await patterns throughout
- SemaphoreSlim for critical sections
- CancellationToken support

## Error Handling

Structured exception hierarchy:
- `SaveLoadException`: Loading failures
- `SaveWriteException`: Writing failures
- `ValidationException`: Validation failures
- `BackupException`: Backup operation failures
