// <copyright file="SaveService.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Services;

using Bannerlord.SaveEditor.Core.Compression;
using Bannerlord.SaveEditor.Core.Interfaces;
using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.Parsers;
using Bannerlord.SaveEditor.Core.Validation;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

/// <summary>
/// Primary service for save file operations.
/// </summary>
public sealed class SaveService : ISaveService
{
    private readonly ILogger<SaveService>? _logger;
    private readonly SaveParser _parser;
    private readonly SaveWriter _writer;
    private readonly IValidationService _validation;
    private readonly IBackupService? _backupService;
    private readonly SaveServiceOptions _options;

    private static readonly string DefaultSavePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Mount and Blade II Bannerlord", "Game Saves");

    public event EventHandler<SaveLoadedEventArgs>? SaveLoaded;
    public event EventHandler<SaveSavingEventArgs>? SaveSaving;
    public event EventHandler<SaveSavedEventArgs>? SaveSaved;

    public SaveService(
        IValidationService? validation = null,
        IBackupService? backupService = null,
        SaveServiceOptions? options = null,
        ILogger<SaveService>? logger = null)
    {
        _logger = logger;
        _options = options ?? new SaveServiceOptions();
        _validation = validation ?? new ValidationService();
        _backupService = backupService;
        _parser = new SaveParser(new ZlibHandler(), logger as ILogger<SaveParser>);
        _writer = new SaveWriter(new ZlibHandler(), logger as ILogger<SaveWriter>);
    }

    public async Task<IReadOnlyList<SaveFileInfo>> DiscoverSavesAsync(CancellationToken ct = default)
    {
        var saves = new List<SaveFileInfo>();
        var searchPath = _options.SaveDirectory ?? DefaultSavePath;

        _logger?.LogInformation("Discovering saves in: {Path}", searchPath);

        if (!Directory.Exists(searchPath))
        {
            _logger?.LogWarning("Save directory does not exist: {Path}", searchPath);
            return saves;
        }

        foreach (var dir in Directory.GetDirectories(searchPath))
        {
            foreach (var file in Directory.GetFiles(dir, "*.sav"))
            {
                try
                {
                    var info = await _parser.LoadInfoAsync(file, ct);
                    saves.Add(info);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to load save info: {Path}", file);
                }
            }
        }

        // Also check root directory for any stray saves
        foreach (var file in Directory.GetFiles(searchPath, "*.sav"))
        {
            try
            {
                var info = await _parser.LoadInfoAsync(file, ct);
                saves.Add(info);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load save info: {Path}", file);
            }
        }

        _logger?.LogInformation("Discovered {Count} save files", saves.Count);
        return saves.OrderByDescending(s => s.LastModified).ToList();
    }

    public async Task<SaveFileInfo> GetSaveInfoAsync(string path, CancellationToken ct = default)
    {
        return await _parser.LoadInfoAsync(path, ct);
    }

    public async Task<SaveFile> LoadAsync(string path, LoadOptions? options = null, CancellationToken ct = default)
    {
        options ??= new LoadOptions();
        _logger?.LogInformation("Loading save: {Path}", path);

        var save = await _parser.LoadAsync(path, options, ct);

        // Validate if requested
        if (!options.SkipValidation)
        {
            var report = _validation.Validate(save);
            save.ValidationReport = report;

            if (!options.Permissive && !report.IsValid)
            {
                throw new SaveLoadException($"Save validation failed with {report.Errors.Count} errors");
            }
        }

        SaveLoaded?.Invoke(this, new SaveLoadedEventArgs { Save = save, Path = path });
        return save;
    }

    public async Task SaveAsync(SaveFile save, string? path = null, SaveOptions? options = null, CancellationToken ct = default)
    {
        options ??= new SaveOptions();
        path ??= save.FilePath;

        if (string.IsNullOrEmpty(path))
            throw new SaveWriteException("No save path specified");

        _logger?.LogInformation("Saving to: {Path}", path);
        SaveSaving?.Invoke(this, new SaveSavingEventArgs { Save = save, TargetPath = path });

        // Create backup before saving
        if (options.CreateBackup && _backupService != null && File.Exists(path))
        {
            await _backupService.CreateSnapshotAsync(path, BackupTrigger.PreEdit, cancellationToken: ct);
        }

        // Validate before save
        if (options.ValidateBeforeSave)
        {
            var report = _validation.Validate(save);
            if (!report.IsValid)
            {
                throw new SaveWriteException($"Validation failed: {string.Join(", ", report.Errors.Select(e => e.Message))}");
            }
        }

        // Update metadata
        save.Metadata.PlayTimeSeconds += (DateTime.UtcNow - save.LastModified).TotalSeconds;
        save.LastModified = DateTime.UtcNow;

        // Write the file
        await _writer.SaveAsync(save, path, options.CompressionLevel, ct);

        // Verify after save
        if (options.VerifyAfterSave)
        {
            var verified = await _writer.VerifyIntegrityAsync(path, ct);
            if (!verified)
            {
                throw new SaveWriteException("Save verification failed after write");
            }
        }

        save.FilePath = path;
        SaveSaved?.Invoke(this, new SaveSavedEventArgs { Save = save, Path = path });
        _logger?.LogInformation("Save completed: {Path}", path);
    }

    public async Task<ValidationReport> ValidateAsync(SaveFile save, CancellationToken ct = default)
    {
        await Task.CompletedTask; // Satisfy async requirement
        return _validation.Validate(save);
    }

    public async Task<bool> VerifyIntegrityAsync(string path, CancellationToken ct = default)
    {
        return await _writer.VerifyIntegrityAsync(path, ct);
    }

    public async Task<SaveFile> CreateCopyAsync(SaveFile source, string newName, CancellationToken ct = default)
    {
        var newPath = Path.Combine(
            Path.GetDirectoryName(source.FilePath) ?? DefaultSavePath,
            $"{newName}.sav");

        var copy = source.Clone();
        copy.Name = newName;
        copy.FilePath = newPath;

        await SaveAsync(copy, newPath, new SaveOptions { CreateBackup = false }, ct);
        return copy;
    }

    public async Task DeleteAsync(string path, bool createBackup = true, CancellationToken ct = default)
    {
        if (!File.Exists(path))
            return;

        _logger?.LogInformation("Deleting save: {Path}", path);

        if (createBackup && _backupService != null)
        {
            await _backupService.CreateSnapshotAsync(path, BackupTrigger.Manual, cancellationToken: ct);
        }

        File.Delete(path);
    }
}

/// <summary>
/// Options for the save service.
/// </summary>
public class SaveServiceOptions
{
    public string? SaveDirectory { get; set; }
    public bool AutoBackupOnLoad { get; set; } = false;
    public bool AutoBackupOnSave { get; set; } = true;
    public bool StrictValidation { get; set; } = false;
}


/// <summary>
/// Exception during save load.
/// </summary>
public class SaveLoadException : Exception
{
    public SaveLoadException(string message) : base(message) { }
    public SaveLoadException(string message, Exception inner) : base(message, inner) { }
}
