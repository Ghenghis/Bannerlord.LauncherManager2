# Package Publishing Guide

This document describes how to publish the Bannerlord.LauncherManager2 packages.

## Packages Overview

| Package | Type | Description |
|---------|------|-------------|
| `Bannerlord.LauncherManager.Models` | NuGet | Data models and DTOs |
| `Bannerlord.LauncherManager.Localization` | NuGet | Localization strings |
| `Bannerlord.LauncherManager` | NuGet | Core logic and handlers |
| `@butr/vortexextensionnative` | npm | TypeScript/Node.js bindings |

## NuGet Package Publishing

### Prerequisites

1. .NET 8.0 SDK installed
2. NuGet API key from [nuget.org](https://www.nuget.org/)

### Build NuGet Packages

```bash
# Build all packages
dotnet pack src/Bannerlord.LauncherManager.Models/Bannerlord.LauncherManager.Models.csproj -c Release
dotnet pack src/Bannerlord.LauncherManager.Localization/Bannerlord.LauncherManager.Localization.csproj -c Release
dotnet pack src/Bannerlord.LauncherManager/Bannerlord.LauncherManager.csproj -c Release
```

### Publish to NuGet

```bash
# Set your API key
dotnet nuget push **/*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

### Version Management

Version is controlled in each `.csproj` file:

```xml
<PropertyGroup>
  <VersionOverride>0</VersionOverride>
  <Version>2.0.$(VersionOverride)</Version>
</PropertyGroup>
```

To release a new version, update `VersionOverride` in all projects.

## npm Package Publishing

### Prerequisites

1. Node.js 22+ installed
2. npm account with publish access to `@butr` scope

### Build npm Package

```bash
cd src/Bannerlord.LauncherManager.Native.TypeScript

# Install dependencies
npm install

# Build TypeScript
npm run build-ts

# Build native bindings (requires native build tools)
npm run build-native
```

### Publish to npm

```bash
# Login to npm
npm login

# Publish (from TypeScript package directory)
npm publish --access public
```

### Package Contents

The npm package includes:
- `dist/main/` - CommonJS build
- `dist/module/` - ES Module build
- `dist/Bannerlord.LauncherManager.Native.dll` - Native .NET library
- `dist/launchermanager.node` - Node.js native addon

## GitHub Actions CI/CD

The project includes GitHub Actions workflows for automated builds:

```yaml
# .github/workflows/publish.yml
name: Publish Packages

on:
  release:
    types: [published]

jobs:
  nuget:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet pack -c Release
      - run: dotnet nuget push **/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json

  npm:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          registry-url: 'https://registry.npmjs.org'
      - run: npm ci
        working-directory: src/Bannerlord.LauncherManager.Native.TypeScript
      - run: npm run build
        working-directory: src/Bannerlord.LauncherManager.Native.TypeScript
      - run: npm publish --access public
        working-directory: src/Bannerlord.LauncherManager.Native.TypeScript
        env:
          NODE_AUTH_TOKEN: ${{ secrets.NPM_TOKEN }}
```

## Feature Branches

All 16 features are implemented in separate branches:

| Branch | Feature |
|--------|---------|
| `feature/backup-restore` | Backup and restore functionality |
| `feature/mod-conflict` | Mod conflict detection |
| `feature/mod-profiles` | Mod profile management |
| `feature/steam-workshop` | Steam Workshop integration |
| `feature/nexus-mods` | Nexus Mods integration |
| `feature/mod-update-checker` | Mod update checking |
| `feature/save-analysis` | Save game analysis |
| `feature/load-order-optimizer` | Load order optimization |
| `feature/preset-sharing` | Preset sharing system |
| `feature/save-editor` | Save game editing (War Sails support) |
| ... | Additional features |

### Merge Strategy

```bash
# Merge all feature branches to main
git checkout main
git merge feature/backup-restore
git merge feature/mod-conflict
git merge feature/mod-profiles
# ... continue for all branches
git push origin main
```

## Testing

### Run Integration Tests

```bash
cd tests/Bannerlord.LauncherManager.Tests
dotnet run
```

### Test Output

```
=== Save Editor Integration Tests ===
Save Directory: C:\Users\...\Mount and Blade II Bannerlord\Game Saves
Found 7 save files
...
=== Results: 7 passed, 0 failed ===
```

## Important Paths

| Path | Purpose |
|------|---------|
| `C:\Users\Admin\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves` | Save files |
| `C:\Users\Admin\AppData\Roaming\Vortex\mountandblade2bannerlord\mods` | Vortex mods |
| `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules` | Game modules |

## Save File Format

Bannerlord saves use a JSON-based format:
- First 4 bytes: Header length (little-endian int32)
- Next N bytes: JSON metadata including module list
- Remaining: Compressed game data

```csharp
// Reading save header
using var reader = new BinaryReader(stream);
var headerLength = reader.ReadInt32();
var headerJson = Encoding.UTF8.GetString(reader.ReadBytes(headerLength));
```
