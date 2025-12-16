using Bannerlord.LauncherManager.Models;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager;

partial class LauncherManagerHandler
{
    private const string BackupFolder = "Backups";
    private const string BackupMetadataFile = "backup_metadata.json";
    private AutoBackupSettings _autoBackupSettings = new();

    private static readonly JsonSerializerOptions BackupJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// External<br/>
    /// Creates a backup with the specified options.
    /// </summary>
    public async Task<BackupResult> CreateBackupAsync(BackupOptions? options = null)
    {
        options ??= new BackupOptions();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var installPath = await GetInstallPathAsync();
            var backupDir = Path.Combine(installPath, BackupFolder);
            
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupName = options.Name ?? $"Backup_{timestamp}";
            var fileName = $"{backupName}_{options.Type}.zip";
            var backupPath = Path.Combine(backupDir, fileName);

            var metadata = new BackupMetadata
            {
                Name = backupName,
                Description = options.Description,
                Type = options.Type,
                FilePath = backupPath,
                GameVersion = await GetGameVersionAsync()
            };

            using (var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create))
            {
                // Backup based on type
                switch (options.Type)
                {
                    case BackupType.Full:
                        await BackupModuleConfigsAsync(archive, metadata, options);
                        await BackupSavesAsync(archive, metadata, options);
                        await BackupSettingsAsync(archive, metadata);
                        break;

                    case BackupType.ModulesOnly:
                        await BackupModuleConfigsAsync(archive, metadata, options);
                        break;

                    case BackupType.SavesOnly:
                        await BackupSavesAsync(archive, metadata, options);
                        break;

                    case BackupType.SettingsOnly:
                        await BackupSettingsAsync(archive, metadata);
                        break;
                }

                // Add metadata to archive
                var metadataJson = JsonSerializer.Serialize(metadata, BackupJsonOptions);
                var metadataEntry = archive.CreateEntry(BackupMetadataFile);
                await using var writer = new StreamWriter(metadataEntry.Open());
                await writer.WriteAsync(metadataJson);
            }

            // Calculate file size and checksum
            var fileInfo = new FileInfo(backupPath);
            metadata.SizeBytes = fileInfo.Length;

            if (options.GenerateChecksum)
            {
                metadata.Checksum = await CalculateChecksumAsync(backupPath);
            }

            stopwatch.Stop();
            return BackupResult.AsSuccess(metadata, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return BackupResult.AsError($"Backup failed: {ex.Message}");
        }
    }

    /// <summary>
    /// External<br/>
    /// Creates an automatic backup with the specified reason.
    /// </summary>
    public async Task<BackupResult> CreateAutoBackupAsync(string reason)
    {
        if (!_autoBackupSettings.Enabled)
            return BackupResult.AsError("Auto-backup is disabled.");

        var options = new BackupOptions
        {
            Type = BackupType.ModulesOnly,
            Name = $"AutoBackup_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
            Description = $"Automatic backup: {reason}"
        };

        var result = await CreateBackupAsync(options);
        
        if (result.Success && result.Backup != null)
        {
            result.Backup.IsAutoBackup = true;
            result.Backup.AutoBackupReason = reason;

            // Cleanup old auto-backups
            await CleanupOldAutoBackupsAsync();
        }

        return result;
    }

    /// <summary>
    /// External<br/>
    /// Restores from a backup.
    /// </summary>
    public async Task<RestoreResult> RestoreBackupAsync(string backupId, RestoreOptions? options = null)
    {
        options ??= new RestoreOptions();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var backup = await GetBackupByIdAsync(backupId);
            if (backup == null)
                return RestoreResult.AsError("Backup not found.");

            if (!File.Exists(backup.FilePath))
                return RestoreResult.AsError("Backup file not found.");

            // Verify checksum if requested
            if (options.VerifyChecksum && !string.IsNullOrEmpty(backup.Checksum))
            {
                var currentChecksum = await CalculateChecksumAsync(backup.FilePath);
                if (currentChecksum != backup.Checksum)
                    return RestoreResult.AsError("Backup checksum verification failed. File may be corrupted.");
            }

            // Create pre-restore backup if requested
            BackupMetadata? preRestoreBackup = null;
            if (options.BackupBeforeRestore)
            {
                var preBackupResult = await CreateBackupAsync(new BackupOptions
                {
                    Type = BackupType.Full,
                    Name = $"PreRestore_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                    Description = "Backup created before restore operation"
                });

                if (preBackupResult.Success)
                    preRestoreBackup = preBackupResult.Backup;
            }

            var installPath = await GetInstallPathAsync();
            int modulesRestored = 0, savesRestored = 0, profilesRestored = 0;
            var warnings = new List<string>();

            using (var archive = ZipFile.OpenRead(backup.FilePath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name == BackupMetadataFile)
                        continue;

                    var relativePath = entry.FullName;
                    
                    // Determine if we should restore this entry
                    var shouldRestore = ShouldRestoreEntry(relativePath, options);
                    if (!shouldRestore)
                        continue;

                    var targetPath = Path.Combine(installPath, relativePath);
                    var targetDir = Path.GetDirectoryName(targetPath);

                    if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);

                    if (File.Exists(targetPath) && !options.OverwriteExisting)
                    {
                        warnings.Add($"Skipped existing file: {relativePath}");
                        continue;
                    }

                    entry.ExtractToFile(targetPath, overwrite: true);

                    // Track what was restored
                    if (relativePath.Contains("Modules") || relativePath.Contains("loadorder"))
                        modulesRestored++;
                    else if (relativePath.Contains("Saves") || relativePath.Contains(".sav"))
                        savesRestored++;
                    else if (relativePath.Contains("profiles"))
                        profilesRestored++;
                }
            }

            stopwatch.Stop();
            var result = RestoreResult.AsSuccess(modulesRestored, savesRestored, profilesRestored, stopwatch.Elapsed);
            result.PreRestoreBackup = preRestoreBackup;
            result.Warnings = warnings;
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return RestoreResult.AsError($"Restore failed: {ex.Message}");
        }
    }

    /// <summary>
    /// External<br/>
    /// Lists all available backups.
    /// </summary>
    public async Task<IReadOnlyList<BackupMetadata>> ListBackupsAsync()
    {
        var backups = new List<BackupMetadata>();

        try
        {
            var installPath = await GetInstallPathAsync();
            var backupDir = Path.Combine(installPath, BackupFolder);

            if (!Directory.Exists(backupDir))
                return backups;

            foreach (var file in Directory.GetFiles(backupDir, "*.zip"))
            {
                try
                {
                    using var archive = ZipFile.OpenRead(file);
                    var metadataEntry = archive.GetEntry(BackupMetadataFile);
                    
                    if (metadataEntry != null)
                    {
                        using var reader = new StreamReader(metadataEntry.Open());
                        var json = await reader.ReadToEndAsync();
                        var metadata = JsonSerializer.Deserialize<BackupMetadata>(json, BackupJsonOptions);
                        
                        if (metadata != null)
                        {
                            metadata.FilePath = file;
                            metadata.SizeBytes = new FileInfo(file).Length;
                            backups.Add(metadata);
                        }
                    }
                }
                catch
                {
                    // Skip invalid backup files
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return backups.OrderByDescending(b => b.CreatedAt).ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets a backup by ID.
    /// </summary>
    public async Task<BackupMetadata?> GetBackupByIdAsync(string id)
    {
        var backups = await ListBackupsAsync();
        return backups.FirstOrDefault(b => b.Id == id);
    }

    /// <summary>
    /// External<br/>
    /// Deletes a backup.
    /// </summary>
    public async Task<bool> DeleteBackupAsync(string backupId)
    {
        var backup = await GetBackupByIdAsync(backupId);
        if (backup == null || !File.Exists(backup.FilePath))
            return false;

        try
        {
            File.Delete(backup.FilePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// External<br/>
    /// Gets or sets auto-backup settings.
    /// </summary>
    public AutoBackupSettings GetAutoBackupSettings() => _autoBackupSettings;

    public void SetAutoBackupSettings(AutoBackupSettings settings)
    {
        _autoBackupSettings = settings;
    }

    /// <summary>
    /// External<br/>
    /// Verifies a backup's integrity.
    /// </summary>
    public async Task<bool> VerifyBackupAsync(string backupId)
    {
        var backup = await GetBackupByIdAsync(backupId);
        if (backup == null || string.IsNullOrEmpty(backup.Checksum))
            return false;

        try
        {
            var currentChecksum = await CalculateChecksumAsync(backup.FilePath);
            return currentChecksum == backup.Checksum;
        }
        catch
        {
            return false;
        }
    }

    private async Task BackupModuleConfigsAsync(ZipArchive archive, BackupMetadata metadata, BackupOptions options)
    {
        var modules = await GetModulesAsync();
        var viewModels = await GetModuleViewModelsAsync();

        // Save load order configuration
        if (viewModels != null)
        {
            var loadOrderJson = JsonSerializer.Serialize(viewModels.Select(vm => new
            {
                vm.ModuleInfoExtended.Id,
                vm.IsSelected,
                vm.IsDisabled,
                vm.Index
            }), BackupJsonOptions);

            var loadOrderEntry = archive.CreateEntry("loadorder.json");
            await using var writer = new StreamWriter(loadOrderEntry.Open());
            await writer.WriteAsync(loadOrderJson);
        }

        foreach (var module in modules)
        {
            if (options.IncludeModules != null && !options.IncludeModules.Contains(module.Id))
                continue;

            metadata.IncludedModules.Add(module.Id);
        }

        metadata.ModuleCount = metadata.IncludedModules.Count;
    }

    private async Task BackupSavesAsync(ZipArchive archive, BackupMetadata metadata, BackupOptions options)
    {
        var saves = await GetSaveFilesAsync();
        var installPath = await GetInstallPathAsync();

        foreach (var save in saves)
        {
            if (options.IncludeSaves != null && !options.IncludeSaves.Contains(save.Name))
                continue;

            var savePath = await GetSaveFilePathAsync(save.Name);
            if (File.Exists(savePath))
            {
                var relativePath = Path.GetRelativePath(installPath, savePath);
                archive.CreateEntryFromFile(savePath, $"Saves/{Path.GetFileName(savePath)}");
                metadata.IncludedSaves.Add(save.Name);
            }
        }

        metadata.SaveCount = metadata.IncludedSaves.Count;
    }

    private async Task BackupSettingsAsync(ZipArchive archive, BackupMetadata metadata)
    {
        var installPath = await GetInstallPathAsync();

        // Backup profiles if ProfileManager exists
        var profilesPath = Path.Combine(installPath, "profiles.json");
        if (File.Exists(profilesPath))
        {
            archive.CreateEntryFromFile(profilesPath, "profiles.json");
        }

        // Backup auto-backup settings
        var settingsJson = JsonSerializer.Serialize(_autoBackupSettings, BackupJsonOptions);
        var settingsEntry = archive.CreateEntry("settings/autobackup.json");
        await using var writer = new StreamWriter(settingsEntry.Open());
        await writer.WriteAsync(settingsJson);
    }

    private static bool ShouldRestoreEntry(string path, RestoreOptions options)
    {
        if (path.Contains("loadorder") || path.Contains("Modules"))
            return options.RestoreModuleConfigs;

        if (path.Contains("Saves") || path.EndsWith(".sav"))
            return options.RestoreSaves;

        if (path.Contains("profiles"))
            return options.RestoreProfiles;

        if (path.Contains("settings"))
            return options.RestoreSettings;

        return true;
    }

    private async Task CleanupOldAutoBackupsAsync()
    {
        var backups = await ListBackupsAsync();
        var autoBackups = backups.Where(b => b.IsAutoBackup).OrderByDescending(b => b.CreatedAt).ToList();

        while (autoBackups.Count > _autoBackupSettings.MaxAutoBackups)
        {
            var oldest = autoBackups.Last();
            await DeleteBackupAsync(oldest.Id);
            autoBackups.Remove(oldest);
        }
    }

    private static async Task<string> CalculateChecksumAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hash);
    }
}
