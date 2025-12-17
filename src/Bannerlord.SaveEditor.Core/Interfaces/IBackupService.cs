// <copyright file="IBackupService.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Interfaces;

using Bannerlord.SaveEditor.Core.Models;

/// <summary>
/// Service interface for backup management.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a backup snapshot of a save file.
    /// </summary>
    /// <param name="save">The save file to backup.</param>
    /// <param name="trigger">What triggered this backup.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the created backup.</returns>
    Task<BackupInfo> CreateSnapshotAsync(SaveFile save, BackupTrigger trigger, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a backup snapshot from a file path.
    /// </summary>
    /// <param name="savePath">Path to the save file.</param>
    /// <param name="trigger">What triggered this backup.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the created backup.</returns>
    Task<BackupInfo> CreateSnapshotAsync(string savePath, BackupTrigger trigger, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a backup to the specified path.
    /// </summary>
    /// <param name="backup">The backup to restore.</param>
    /// <param name="targetPath">Target path for restoration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreAsync(BackupInfo backup, string targetPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all backups, optionally filtered by original save path.
    /// </summary>
    /// <param name="savePath">Optional filter by original save path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of backup information.</returns>
    Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(string? savePath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent backup for a save file.
    /// </summary>
    /// <param name="savePath">Path to the original save file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Latest backup info or null if none exist.</returns>
    Task<BackupInfo?> GetLatestBackupAsync(string savePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prunes old backups according to retention policy.
    /// </summary>
    /// <param name="policy">Retention policy to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of backups deleted.</returns>
    Task<int> PruneBackupsAsync(RetentionPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total size of all backups.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total size in bytes.</returns>
    Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a backup's integrity.
    /// </summary>
    /// <param name="backup">The backup to verify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if backup is valid.</returns>
    Task<bool> VerifyBackupAsync(BackupInfo backup, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific backup.
    /// </summary>
    /// <param name="backup">The backup to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteBackupAsync(BackupInfo backup, CancellationToken cancellationToken = default);
}

/// <summary>
/// Backup retention policy.
/// </summary>
public sealed class RetentionPolicy
{
    /// <summary>
    /// Maximum age of backups to keep.
    /// </summary>
    public TimeSpan MaxAge { get; init; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Maximum number of backups to keep per save file.
    /// </summary>
    public int MaxBackupsPerSave { get; init; } = 10;

    /// <summary>
    /// Maximum total backup storage size in bytes.
    /// </summary>
    public long MaxTotalSize { get; init; } = 10L * 1024 * 1024 * 1024; // 10 GB

    /// <summary>
    /// If true, keeps at least one backup per save regardless of age.
    /// </summary>
    public bool KeepAtLeastOne { get; init; } = true;

    /// <summary>
    /// Default retention policy.
    /// </summary>
    public static RetentionPolicy Default => new();
}

/// <summary>
/// Backup service options.
/// </summary>
public sealed class BackupOptions
{
    /// <summary>
    /// Directory to store backups.
    /// </summary>
    public string BackupDirectory { get; init; } = "_Backups";

    /// <summary>
    /// Compression type for backups.
    /// </summary>
    public BackupCompression Compression { get; init; } = BackupCompression.GZip;

    /// <summary>
    /// Retention policy for automatic cleanup.
    /// </summary>
    public RetentionPolicy RetentionPolicy { get; init; } = RetentionPolicy.Default;

    /// <summary>
    /// If true, computes and stores checksums.
    /// </summary>
    public bool ComputeChecksums { get; init; } = true;

    /// <summary>
    /// If true, creates manifest files with metadata.
    /// </summary>
    public bool CreateManifests { get; init; } = true;
}

/// <summary>
/// Backup compression types.
/// </summary>
public enum BackupCompression
{
    None,
    GZip,
    LZ4,
    LZMA
}

/// <summary>
/// Backup manifest file content.
/// </summary>
public sealed class BackupManifest
{
    public int Version { get; init; } = 2;
    public DateTime Created { get; init; }
    public BackupTrigger Trigger { get; init; }
    public OriginalFileInfo Original { get; init; } = new();
    public BackupFileInfo Backup { get; init; } = new();
    public SaveMetadataInfo Metadata { get; init; } = new();
}

/// <summary>
/// Original file information in manifest.
/// </summary>
public sealed class OriginalFileInfo
{
    public string Path { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public DateTime LastModified { get; init; }
}

/// <summary>
/// Backup file information in manifest.
/// </summary>
public sealed class BackupFileInfo
{
    public string Path { get; init; } = string.Empty;
    public long Size { get; init; }
    public BackupCompression Compression { get; init; }
    public string Sha256 { get; init; } = string.Empty;
}

/// <summary>
/// Save metadata in manifest for quick reference.
/// </summary>
public sealed class SaveMetadataInfo
{
    public string CharacterName { get; init; } = string.Empty;
    public int Level { get; init; }
    public int Day { get; init; }
    public string GameVersion { get; init; } = string.Empty;
    public IReadOnlyList<string> Modules { get; init; } = Array.Empty<string>();
}
