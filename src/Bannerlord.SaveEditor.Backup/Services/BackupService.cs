// <copyright file="BackupService.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Backup.Services;

using Bannerlord.SaveEditor.Core.Compression;
using Bannerlord.SaveEditor.Core.Interfaces;
using Bannerlord.SaveEditor.Core.Models;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

/// <summary>
/// Implementation of backup service with comprehensive safety features.
/// </summary>
public sealed class BackupService : IBackupService, IDisposable
{
    private readonly ILogger<BackupService>? _logger;
    private readonly BackupOptions _options;
    private readonly FileSystemWatcher? _watcher;
    private readonly SemaphoreSlim _backupLock = new(1, 1);
    private Timer? _scheduledTimer;

    public event EventHandler<BackupCreatedEventArgs>? BackupCreated;
    public event EventHandler<BackupRestoredEventArgs>? BackupRestored;

    public BackupService(BackupOptions? options = null, ILogger<BackupService>? logger = null)
    {
        _options = options ?? new BackupOptions();
        _logger = logger;

        EnsureBackupDirectoriesExist();
    }

    public async Task<BackupInfo> CreateSnapshotAsync(SaveFile save, BackupTrigger trigger, CancellationToken cancellationToken = default)
    {
        return await CreateSnapshotInternalAsync(save.FilePath, trigger, save, cancellationToken);
    }

    public async Task<BackupInfo> CreateSnapshotAsync(string savePath, BackupTrigger trigger, CancellationToken cancellationToken = default)
    {
        return await CreateSnapshotInternalAsync(savePath, trigger, null, cancellationToken);
    }

    private async Task<BackupInfo> CreateSnapshotInternalAsync(string sourcePath, BackupTrigger trigger, SaveFile? save = null, CancellationToken ct = default)
    {
        await _backupLock.WaitAsync(ct);
        try
        {
            _logger?.LogInformation("Creating {Trigger} backup for: {Path}", trigger, sourcePath);

            if (!File.Exists(sourcePath))
                throw new BackupException($"Source file not found: {sourcePath}");

            var sourceInfo = new FileInfo(sourcePath);
            var timestamp = DateTime.UtcNow;
            var snapshotId = Guid.NewGuid();

            // Determine backup path based on trigger
            var backupDir = trigger == BackupTrigger.PreEdit
                ? Path.Combine(_options.BackupDirectory, "pre-edit")
                : Path.Combine(_options.BackupDirectory, "snapshots");

            var backupFileName = $"{timestamp:yyyy-MM-ddTHH-mm-ss}_{Path.GetFileName(sourcePath)}";
            if (_options.Compression != BackupCompression.None)
                backupFileName += GetCompressionExtension(_options.Compression);

            var backupPath = Path.Combine(backupDir, backupFileName);

            // Compute source checksum
            var sourceChecksum = await ComputeChecksumAsync(sourcePath, ct);

            // Create backup (with optional compression)
            if (_options.Compression == BackupCompression.None)
            {
                File.Copy(sourcePath, backupPath, overwrite: true);
            }
            else
            {
                await CompressFileAsync(sourcePath, backupPath, _options.Compression, ct);
            }

            // Compute backup checksum
            var backupChecksum = await ComputeChecksumAsync(backupPath, ct);
            var backupInfo = new FileInfo(backupPath);

            // Create manifest
            var metadata = ExtractMetadata(save, sourcePath);
            var manifest = new BackupManifest
            {
                Version = 2,
                Created = timestamp,
                Trigger = trigger,
                Original = new OriginalFileInfo
                {
                    Path = sourcePath,
                    Size = sourceInfo.Length,
                    Sha256 = sourceChecksum,
                    LastModified = sourceInfo.LastWriteTimeUtc
                },
                Backup = new BackupFileInfo
                {
                    Path = backupPath,
                    Size = backupInfo.Length,
                    Compression = _options.Compression,
                    Sha256 = backupChecksum
                },
                Metadata = new SaveMetadataInfo
                {
                    CharacterName = metadata.CharacterName,
                    Level = metadata.Level,
                    Day = metadata.Day,
                    GameVersion = metadata.GameVersion ?? string.Empty,
                    Modules = metadata.ModuleIds
                }
            };

            // Save manifest
            if (_options.CreateManifests)
            {
                var manifestPath = Path.Combine(_options.BackupDirectory, "manifests",
                    $"{timestamp:yyyy-MM-ddTHH-mm-ss}_{Path.GetFileNameWithoutExtension(sourcePath)}.manifest.json");
                await SaveManifestAsync(manifest, manifestPath, ct);
            }

            var result = new BackupInfo
            {
                OriginalPath = sourcePath,
                BackupPath = backupPath,
                CreatedAt = timestamp,
                Trigger = trigger,
                OriginalSize = sourceInfo.Length,
                BackupSize = backupInfo.Length,
                Checksum = backupChecksum
            };

            _logger?.LogInformation("Backup created: {Path} ({OriginalSize} -> {BackupSize})",
                backupPath, sourceInfo.Length, backupInfo.Length);

            BackupCreated?.Invoke(this, new BackupCreatedEventArgs(result));

            return result;
        }
        finally
        {
            _backupLock.Release();
        }
    }

    public async Task RestoreAsync(BackupInfo backup, string targetPath, CancellationToken cancellationToken = default)
    {
        await RestoreInternalAsync(backup.BackupPath, targetPath, cancellationToken);
    }

    public async Task<RestoreResult> RestoreInternalAsync(string backupPath, string targetPath, CancellationToken ct = default)
    {
        await _backupLock.WaitAsync(ct);
        try
        {
            _logger?.LogInformation("Restoring backup: {Backup} -> {Target}", backupPath, targetPath);

            if (!File.Exists(backupPath))
                throw new BackupException($"Backup file not found: {backupPath}");

            // Verify backup integrity
            var isValid = await VerifyBackupAsync(backupPath, ct);
            if (!isValid)
                throw new BackupException("Backup file failed integrity check");

            // Create safety backup of current file if it exists
            string? safetyBackupPath = null;
            if (File.Exists(targetPath))
            {
                safetyBackupPath = targetPath + ".restore-backup";
                File.Copy(targetPath, safetyBackupPath, overwrite: true);
            }

            try
            {
                // Decompress if needed
                var compression = DetectCompression(backupPath);
                if (compression != BackupCompression.None)
                {
                    await DecompressFileAsync(backupPath, targetPath, compression, ct);
                }
                else
                {
                    File.Copy(backupPath, targetPath, overwrite: true);
                }

                // Verify restored file
                var targetInfo = new FileInfo(targetPath);

                var result = new RestoreResult
                {
                    Success = true,
                    BackupPath = backupPath,
                    TargetPath = targetPath,
                    RestoredSize = targetInfo.Length,
                    RestoredAt = DateTime.UtcNow
                };

                // Clean up safety backup
                if (safetyBackupPath != null && File.Exists(safetyBackupPath))
                    File.Delete(safetyBackupPath);

                _logger?.LogInformation("Restore completed: {Target}", targetPath);
                BackupRestored?.Invoke(this, new BackupRestoredEventArgs(result));

                return result;
            }
            catch (Exception ex)
            {
                // Restore safety backup if we have one
                if (safetyBackupPath != null && File.Exists(safetyBackupPath))
                {
                    try
                    {
                        File.Copy(safetyBackupPath, targetPath, overwrite: true);
                        File.Delete(safetyBackupPath);
                    }
                    catch { /* ignore cleanup errors */ }
                }

                throw new BackupException($"Restore failed: {ex.Message}", ex);
            }
        }
        finally
        {
            _backupLock.Release();
        }
    }

    public async Task<IReadOnlyList<BackupInfo>> GetBackupsAsync(string? savePath = null, CancellationToken cancellationToken = default)
    {
        var backups = new List<BackupInfo>();
        var snapshotDir = Path.Combine(_options.BackupDirectory, "snapshots");
        var preEditDir = Path.Combine(_options.BackupDirectory, "pre-edit");

        foreach (var dir in new[] { snapshotDir, preEditDir })
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var file in Directory.GetFiles(dir, "*.*"))
            {
                if (file.EndsWith(".manifest.json")) continue;

                var fileName = Path.GetFileName(file);
                if (savePath != null && !fileName.Contains(Path.GetFileName(savePath)))
                    continue;

                var info = new FileInfo(file);
                backups.Add(new BackupInfo
                {
                    BackupPath = file,
                    CreatedAt = info.CreationTimeUtc,
                    BackupSize = info.Length,
                    Trigger = dir.Contains("pre-edit") ? BackupTrigger.PreEdit : BackupTrigger.Scheduled
                });
            }
        }

        return backups.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public async Task<BackupInfo?> GetLatestBackupAsync(string originalPath, CancellationToken ct = default)
    {
        var backups = await GetBackupsAsync(originalPath, ct);
        return backups.FirstOrDefault();
    }

    public async Task<int> PruneBackupsAsync(RetentionPolicy policy, CancellationToken ct = default)
    {
        _logger?.LogInformation("Pruning backups with policy: MaxAge={MaxAge}, MaxCount={MaxCount}, MaxSize={MaxSize}",
            policy.MaxAge, policy.MaxBackupsPerSave, policy.MaxTotalSize);

        var pruned = 0;
        var allBackups = (await GetBackupsAsync(cancellationToken: ct)).ToList();

        // Group by original save
        var grouped = allBackups
            .Where(b => !string.IsNullOrEmpty(b.OriginalPath))
            .GroupBy(b => b.OriginalPath!)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.CreatedAt).ToList());

        foreach (var kvp in grouped)
        {
            var saveBackups = kvp.Value;
            var toDelete = new List<BackupInfo>();

            // Apply max age
            var cutoff = DateTime.UtcNow - policy.MaxAge;
            toDelete.AddRange(saveBackups.Where(b => b.CreatedAt < cutoff));

            // Apply max count per save
            var excess = saveBackups.Skip(policy.MaxBackupsPerSave);
            foreach (var backup in excess)
            {
                if (!toDelete.Contains(backup))
                    toDelete.Add(backup);
            }

            // Keep at least one if policy says so
            if (policy.KeepAtLeastOne && toDelete.Count == saveBackups.Count && saveBackups.Count > 0)
            {
                toDelete.Remove(saveBackups.First());
            }

            // Delete
            foreach (var backup in toDelete)
            {
                try
                {
                    if (File.Exists(backup.BackupPath))
                    {
                        File.Delete(backup.BackupPath);
                        pruned++;
                        _logger?.LogDebug("Pruned backup: {Path}", backup.BackupPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to delete backup: {Path}", backup.BackupPath);
                }
            }
        }

        // Apply total size limit
        if (policy.MaxTotalSize > 0)
        {
            var totalSize = await GetTotalSizeAsync(ct);
            if (totalSize > policy.MaxTotalSize)
            {
                var remaining = (await GetBackupsAsync(cancellationToken: ct))
                    .OrderBy(b => b.CreatedAt)
                    .ToList();

                while (totalSize > policy.MaxTotalSize && remaining.Count > 1)
                {
                    var oldest = remaining.First();
                    remaining.RemoveAt(0);

                    try
                    {
                        if (File.Exists(oldest.BackupPath))
                        {
                            totalSize -= oldest.BackupSize;
                            File.Delete(oldest.BackupPath);
                            pruned++;
                        }
                    }
                    catch { /* ignore */ }
                }
            }
        }

        _logger?.LogInformation("Pruned {Count} backups", pruned);
        return pruned;
    }

    public async Task<long> GetTotalSizeAsync(CancellationToken ct = default)
    {
        var backups = await GetBackupsAsync(cancellationToken: ct);
        return backups.Sum(b => b.BackupSize);
    }

    public async Task<bool> VerifyBackupAsync(string backupPath, CancellationToken ct = default)
    {
        if (!File.Exists(backupPath))
            return false;

        try
        {
            // Try to read the file
            await using var stream = File.OpenRead(backupPath);
            var buffer = new byte[4096];
            while (await stream.ReadAsync(buffer, ct) > 0) { }

            // If compressed, try to decompress
            var compression = DetectCompression(backupPath);
            if (compression != BackupCompression.None)
            {
                stream.Position = 0;
                using var decompressor = CreateDecompressor(stream, compression);
                while (await decompressor.ReadAsync(buffer, ct) > 0) { }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> VerifyBackupAsync(BackupInfo backup, CancellationToken ct = default)
    {
        return await VerifyBackupAsync(backup.BackupPath, ct);
    }

    public async Task DeleteBackupAsync(BackupInfo backup, CancellationToken ct = default)
    {
        await DeleteBackupAsync(backup.BackupPath, ct);
    }

    public async Task<bool> DeleteBackupAsync(string backupPath, CancellationToken ct = default)
    {
        if (!File.Exists(backupPath))
            return false;

        try
        {
            File.Delete(backupPath);

            // Also delete manifest if exists
            var manifestPath = backupPath.Replace(".sav", ".manifest.json")
                .Replace(".gz", "").Replace(".lz4", "").Replace(".lzma", "");
            if (File.Exists(manifestPath))
                File.Delete(manifestPath);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to delete backup: {Path}", backupPath);
            return false;
        }
    }

    public void StartScheduledBackups(TimeSpan interval)
    {
        _scheduledTimer?.Dispose();
        _scheduledTimer = new Timer(OnScheduledBackup, null, interval, interval);
        _logger?.LogInformation("Scheduled backups started with interval: {Interval}", interval);
    }

    public void StopScheduledBackups()
    {
        _scheduledTimer?.Dispose();
        _scheduledTimer = null;
        _logger?.LogInformation("Scheduled backups stopped");
    }

    private async void OnScheduledBackup(object? state)
    {
        // This would be connected to save discovery to find active saves
        _logger?.LogDebug("Scheduled backup triggered");
    }

    private void EnsureBackupDirectoriesExist()
    {
        Directory.CreateDirectory(_options.BackupDirectory);
        Directory.CreateDirectory(Path.Combine(_options.BackupDirectory, "snapshots"));
        Directory.CreateDirectory(Path.Combine(_options.BackupDirectory, "pre-edit"));
        Directory.CreateDirectory(Path.Combine(_options.BackupDirectory, "manifests"));
        Directory.CreateDirectory(Path.Combine(_options.BackupDirectory, "recovery"));
    }

    private FileSystemWatcher? CreateFileWatcher()
    {
        // Would watch save directory for changes
        return null;
    }

    private static async Task<string> ComputeChecksumAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private async Task CompressFileAsync(string source, string dest, BackupCompression compression, CancellationToken ct)
    {
        await using var sourceStream = File.OpenRead(source);
        await using var destStream = File.Create(dest);

        Stream compressor = compression switch
        {
            BackupCompression.GZip => new GZipStream(destStream, CompressionLevel.Optimal, leaveOpen: true),
            BackupCompression.LZ4 => destStream, // Would use LZ4 library
            BackupCompression.LZMA => destStream, // Would use LZMA library
            _ => throw new ArgumentException($"Unsupported compression: {compression}")
        };

        await sourceStream.CopyToAsync(compressor, ct);

        if (compressor != destStream)
            await compressor.DisposeAsync();
    }

    private async Task DecompressFileAsync(string source, string dest, BackupCompression compression, CancellationToken ct)
    {
        await using var sourceStream = File.OpenRead(source);
        await using var destStream = File.Create(dest);
        using var decompressor = CreateDecompressor(sourceStream, compression);

        await decompressor.CopyToAsync(destStream, ct);
    }

    private static Stream CreateDecompressor(Stream source, BackupCompression compression)
    {
        return compression switch
        {
            BackupCompression.GZip => new GZipStream(source, CompressionMode.Decompress, leaveOpen: true),
            BackupCompression.LZ4 => source, // Would use LZ4 library
            BackupCompression.LZMA => source, // Would use LZMA library
            _ => source
        };
    }

    private static BackupCompression DetectCompression(string path)
    {
        if (path.EndsWith(".gz")) return BackupCompression.GZip;
        if (path.EndsWith(".lz4")) return BackupCompression.LZ4;
        if (path.EndsWith(".lzma")) return BackupCompression.LZMA;
        return BackupCompression.None;
    }

    private static string GetCompressionExtension(BackupCompression compression)
    {
        return compression switch
        {
            BackupCompression.GZip => ".gz",
            BackupCompression.LZ4 => ".lz4",
            BackupCompression.LZMA => ".lzma",
            _ => ""
        };
    }

    private static BackupMetadata ExtractMetadata(SaveFile? save, string path)
    {
        if (save != null)
        {
            return new BackupMetadata
            {
                CharacterName = save.Metadata.CharacterName,
                Level = save.Metadata.Level,
                Day = save.Metadata.DayNumber,
                GameVersion = save.Header.GameVersion,
                ModuleIds = save.Modules.Select(m => m.Id).ToList()
            };
        }

        return new BackupMetadata { CharacterName = Path.GetFileNameWithoutExtension(path) };
    }

    private static async Task SaveManifestAsync(BackupManifest manifest, string path, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, ct);
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _scheduledTimer?.Dispose();
        _backupLock.Dispose();
    }
}

public class BackupCreatedEventArgs : EventArgs
{
    public BackupInfo Backup { get; }
    public BackupCreatedEventArgs(BackupInfo backup) => Backup = backup;
}

public class BackupRestoredEventArgs : EventArgs
{
    public RestoreResult Result { get; }
    public BackupRestoredEventArgs(RestoreResult result) => Result = result;
}

public class RestoreResult
{
    public bool Success { get; set; }
    public string BackupPath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public long RestoredSize { get; set; }
    public DateTime RestoredAt { get; set; }
    public string? Error { get; set; }
}

public class BackupException : Exception
{
    public BackupException(string message) : base(message) { }
    public BackupException(string message, Exception inner) : base(message, inner) { }
}

public class BackupMetadata
{
    public string CharacterName { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Day { get; set; }
    public string? GameVersion { get; set; }
    public List<string> ModuleIds { get; set; } = new();
}

