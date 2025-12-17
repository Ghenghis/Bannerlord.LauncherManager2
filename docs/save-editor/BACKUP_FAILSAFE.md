# Backup and Failsafe System

## Overview

The backup system provides comprehensive protection against data loss during save editing operations. It automatically creates backups before modifications and supports restoration to any previous state.

## Backup Triggers

| Trigger | Description |
|---------|-------------|
| `Manual` | User-initiated backup |
| `PreEdit` | Automatic before any edit operation |
| `Scheduled` | Periodic automatic backups |
| `OnClose` | When the editor is closed |
| `BeforeRestore` | Before restoring another backup |

## Backup Storage

### Directory Structure

```
_Backups/
├── pre-edit/           # Pre-edit snapshots
│   ├── 2024-01-15T10-30-00_Save1.sav.gz
│   └── 2024-01-15T10-35-00_Save1.sav.gz
├── snapshots/          # Manual/scheduled backups
│   ├── 2024-01-14T20-00-00_Save1.sav.gz
│   └── 2024-01-15T08-00-00_Save1.sav.gz
└── manifests/          # Backup metadata
    ├── 2024-01-15T10-30-00_Save1.json
    └── 2024-01-15T10-35-00_Save1.json
```

### Manifest Format

```json
{
  "Version": 2,
  "SnapshotId": "550e8400-e29b-41d4-a716-446655440000",
  "CreatedAt": "2024-01-15T10:30:00Z",
  "Trigger": "PreEdit",
  "Original": {
    "Path": "C:/Users/.../Save1.sav",
    "Size": 15728640,
    "Sha256": "a1b2c3d4...",
    "LastModified": "2024-01-15T10:28:00Z"
  },
  "Backup": {
    "Path": "_Backups/pre-edit/2024-01-15T10-30-00_Save1.sav.gz",
    "Size": 4194304,
    "Compression": "GZip",
    "Sha256": "e5f6g7h8..."
  },
  "Metadata": {
    "CharacterName": "Ghenghis",
    "Level": 25,
    "Day": 365,
    "GameVersion": "v1.2.9"
  }
}
```

## Compression Options

| Type | Extension | Ratio | Speed |
|------|-----------|-------|-------|
| None | `.sav` | 1.0x | Fastest |
| GZip | `.sav.gz` | ~4x | Fast |
| LZ4 | `.sav.lz4` | ~3x | Fastest |
| LZMA | `.sav.xz` | ~6x | Slow |

## Retention Policies

```csharp
var policy = new RetentionPolicy
{
    MaxAge = TimeSpan.FromDays(30),      // Keep backups for 30 days
    MaxBackupsPerSave = 10,               // Keep up to 10 per save
    MaxTotalSize = 10L * 1024 * 1024 * 1024, // 10 GB total limit
    KeepAtLeastOne = true                 // Always keep newest backup
};
```

## API Usage

### Create Backup

```csharp
var backupService = new BackupService(new BackupOptions
{
    BackupDirectory = "_Backups",
    Compression = BackupCompression.GZip,
    ComputeChecksums = true
});

var backup = await backupService.CreateSnapshotAsync(
    savePath, 
    BackupTrigger.Manual
);
```

### List Backups

```csharp
var backups = await backupService.GetBackupsAsync(savePath);
foreach (var backup in backups)
{
    Console.WriteLine($"{backup.CreatedAt}: {backup.BackupPath}");
}
```

### Restore Backup

```csharp
var latest = await backupService.GetLatestBackupAsync(savePath);
if (latest != null)
{
    await backupService.RestoreAsync(latest, savePath);
}
```

### Verify Backup

```csharp
bool isValid = await backupService.VerifyBackupAsync(backup);
```

### Prune Old Backups

```csharp
int deleted = await backupService.PruneBackupsAsync(RetentionPolicy.Default);
```

## Failsafe Mechanisms

### Pre-Edit Verification
1. Check disk space before backup
2. Verify source file integrity
3. Create backup with checksum
4. Verify backup integrity
5. Only then proceed with edit

### Atomic Writes
1. Write to temporary file first
2. Verify written file integrity
3. Rename original to `.bak`
4. Rename temp to original
5. Delete `.bak` only after success

### Recovery Process
1. Detect corrupted save on load
2. Automatically locate latest valid backup
3. Offer restoration with confirmation
4. Log all recovery actions

## Best Practices

1. **Enable Auto-Backup**: Always enable `CreateBackup` in SaveOptions
2. **Verify After Save**: Keep `VerifyAfterSave = true`
3. **Set Retention Policy**: Prevent disk space issues
4. **Periodic Cleanup**: Run PruneBackupsAsync regularly
5. **Test Restores**: Periodically verify backups can be restored
