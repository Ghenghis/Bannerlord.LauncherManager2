using System;
using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Type of backup content.
/// </summary>
public enum BackupType
{
    /// <summary>
    /// Full backup including modules, configs, and saves.
    /// </summary>
    Full,

    /// <summary>
    /// Only module configurations and load order.
    /// </summary>
    ModulesOnly,

    /// <summary>
    /// Only save files.
    /// </summary>
    SavesOnly,

    /// <summary>
    /// Only profiles and settings.
    /// </summary>
    SettingsOnly
}

/// <summary>
/// Status of a backup operation.
/// </summary>
public enum BackupStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Metadata about a backup.
/// </summary>
public class BackupMetadata
{
    /// <summary>
    /// Unique identifier for this backup.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name for the backup.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of backup.
    /// </summary>
    public BackupType Type { get; set; }

    /// <summary>
    /// When the backup was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Size of the backup in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Path to the backup file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Game version at time of backup.
    /// </summary>
    public string? GameVersion { get; set; }

    /// <summary>
    /// Number of modules included.
    /// </summary>
    public int ModuleCount { get; set; }

    /// <summary>
    /// Number of save files included.
    /// </summary>
    public int SaveCount { get; set; }

    /// <summary>
    /// List of module IDs included in the backup.
    /// </summary>
    public List<string> IncludedModules { get; set; } = new();

    /// <summary>
    /// List of save file names included.
    /// </summary>
    public List<string> IncludedSaves { get; set; } = new();

    /// <summary>
    /// Whether this backup was created automatically.
    /// </summary>
    public bool IsAutoBackup { get; set; }

    /// <summary>
    /// Reason for automatic backup (if applicable).
    /// </summary>
    public string? AutoBackupReason { get; set; }

    /// <summary>
    /// Checksum for integrity verification.
    /// </summary>
    public string? Checksum { get; set; }
}

/// <summary>
/// Options for creating a backup.
/// </summary>
public class BackupOptions
{
    /// <summary>
    /// Type of backup to create.
    /// </summary>
    public BackupType Type { get; set; } = BackupType.Full;

    /// <summary>
    /// Custom name for the backup.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description for the backup.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether to compress the backup.
    /// </summary>
    public bool Compress { get; set; } = true;

    /// <summary>
    /// Specific modules to include (null = all).
    /// </summary>
    public List<string>? IncludeModules { get; set; }

    /// <summary>
    /// Specific saves to include (null = all).
    /// </summary>
    public List<string>? IncludeSaves { get; set; }

    /// <summary>
    /// Whether to include actual mod files or just configs.
    /// </summary>
    public bool IncludeModFiles { get; set; } = false;

    /// <summary>
    /// Whether to generate a checksum.
    /// </summary>
    public bool GenerateChecksum { get; set; } = true;
}

/// <summary>
/// Options for restoring a backup.
/// </summary>
public class RestoreOptions
{
    /// <summary>
    /// Whether to restore module configurations.
    /// </summary>
    public bool RestoreModuleConfigs { get; set; } = true;

    /// <summary>
    /// Whether to restore save files.
    /// </summary>
    public bool RestoreSaves { get; set; } = true;

    /// <summary>
    /// Whether to restore profiles.
    /// </summary>
    public bool RestoreProfiles { get; set; } = true;

    /// <summary>
    /// Whether to restore settings.
    /// </summary>
    public bool RestoreSettings { get; set; } = true;

    /// <summary>
    /// Whether to overwrite existing files.
    /// </summary>
    public bool OverwriteExisting { get; set; } = true;

    /// <summary>
    /// Whether to create a backup before restoring.
    /// </summary>
    public bool BackupBeforeRestore { get; set; } = true;

    /// <summary>
    /// Whether to verify checksum before restoring.
    /// </summary>
    public bool VerifyChecksum { get; set; } = true;
}

/// <summary>
/// Result of a backup operation.
/// </summary>
public class BackupResult
{
    /// <summary>
    /// Whether the backup was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The backup metadata.
    /// </summary>
    public BackupMetadata? Backup { get; set; }

    /// <summary>
    /// Time taken to complete the backup.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Any warnings generated during backup.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    public static BackupResult AsSuccess(BackupMetadata backup, TimeSpan duration) =>
        new() { Success = true, Backup = backup, Duration = duration };

    public static BackupResult AsError(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Result of a restore operation.
/// </summary>
public class RestoreResult
{
    /// <summary>
    /// Whether the restore was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of modules restored.
    /// </summary>
    public int ModulesRestored { get; set; }

    /// <summary>
    /// Number of saves restored.
    /// </summary>
    public int SavesRestored { get; set; }

    /// <summary>
    /// Number of profiles restored.
    /// </summary>
    public int ProfilesRestored { get; set; }

    /// <summary>
    /// Time taken to complete the restore.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Backup created before restore (if applicable).
    /// </summary>
    public BackupMetadata? PreRestoreBackup { get; set; }

    /// <summary>
    /// Any warnings generated during restore.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    public static RestoreResult AsSuccess(int modules, int saves, int profiles, TimeSpan duration) =>
        new() { Success = true, ModulesRestored = modules, SavesRestored = saves, ProfilesRestored = profiles, Duration = duration };

    public static RestoreResult AsError(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Settings for automatic backups.
/// </summary>
public class AutoBackupSettings
{
    /// <summary>
    /// Whether auto-backup is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Backup before mod installations.
    /// </summary>
    public bool BeforeModInstall { get; set; } = true;

    /// <summary>
    /// Backup before mod updates.
    /// </summary>
    public bool BeforeModUpdate { get; set; } = true;

    /// <summary>
    /// Backup before game updates.
    /// </summary>
    public bool BeforeGameUpdate { get; set; } = true;

    /// <summary>
    /// Maximum number of auto-backups to keep.
    /// </summary>
    public int MaxAutoBackups { get; set; } = 10;

    /// <summary>
    /// Minimum hours between auto-backups.
    /// </summary>
    public int MinHoursBetweenBackups { get; set; } = 1;
}
