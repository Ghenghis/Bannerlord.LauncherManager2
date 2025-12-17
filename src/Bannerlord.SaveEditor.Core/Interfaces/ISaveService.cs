// <copyright file="ISaveService.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Interfaces;

using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.Validation;

/// <summary>
/// Primary service interface for save file operations.
/// </summary>
public interface ISaveService
{
    /// <summary>
    /// Discovers all save files in the default saves directory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of discovered save file information.</returns>
    Task<IReadOnlyList<SaveFileInfo>> DiscoverSavesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a specific save file.
    /// </summary>
    /// <param name="path">Path to the save file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Save file information.</returns>
    Task<SaveFileInfo> GetSaveInfoAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a save file.
    /// </summary>
    /// <param name="path">Path to the save file.</param>
    /// <param name="options">Load options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Loaded save file.</returns>
    Task<SaveFile> LoadAsync(string path, LoadOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to a save file.
    /// </summary>
    /// <param name="save">The save file to save.</param>
    /// <param name="path">Target path (or null to overwrite original).</param>
    /// <param name="options">Save options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(SaveFile save, string? path = null, SaveOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a save file.
    /// </summary>
    /// <param name="save">The save file to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation report.</returns>
    Task<ValidationReport> ValidateAsync(SaveFile save, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the integrity of a save file on disk.
    /// </summary>
    /// <param name="path">Path to the save file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if file is valid and not corrupted.</returns>
    Task<bool> VerifyIntegrityAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a save is loaded.
    /// </summary>
    event EventHandler<SaveLoadedEventArgs>? SaveLoaded;

    /// <summary>
    /// Event raised before a save is written.
    /// </summary>
    event EventHandler<SaveSavingEventArgs>? SaveSaving;

    /// <summary>
    /// Event raised after a save is written.
    /// </summary>
    event EventHandler<SaveSavedEventArgs>? SaveSaved;
}

/// <summary>
/// Basic save file information (without loading full content).
/// </summary>
public sealed class SaveFileInfo
{
    public string Path { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string CharacterName { get; init; } = string.Empty;
    public int Level { get; init; }
    public int Day { get; init; }
    public DateTime LastModified { get; init; }
    public long FileSize { get; init; }
    public string GameVersion { get; init; } = string.Empty;
    public bool HasWarSails { get; init; }
    public IReadOnlyList<string> ModuleIds { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Options for loading save files.
/// </summary>
public sealed class LoadOptions
{
    /// <summary>
    /// If true, continues loading despite minor errors.
    /// </summary>
    public bool Permissive { get; init; }

    /// <summary>
    /// If true, loads only metadata without full content.
    /// </summary>
    public bool MetadataOnly { get; init; }

    /// <summary>
    /// If true, skips validation during load.
    /// </summary>
    public bool SkipValidation { get; init; }

    /// <summary>
    /// If true, keeps raw decompressed data for debugging.
    /// </summary>
    public bool KeepRawData { get; init; }
}

/// <summary>
/// Options for saving files.
/// </summary>
public sealed class SaveOptions
{
    /// <summary>
    /// If true, creates a backup before saving.
    /// </summary>
    public bool CreateBackup { get; init; } = true;

    /// <summary>
    /// If true, validates before saving.
    /// </summary>
    public bool ValidateBeforeSave { get; init; } = true;

    /// <summary>
    /// Compression level for the save file.
    /// </summary>
    public System.IO.Compression.CompressionLevel CompressionLevel { get; init; } = System.IO.Compression.CompressionLevel.Optimal;

    /// <summary>
    /// If true, verifies the written file after saving.
    /// </summary>
    public bool VerifyAfterSave { get; init; } = true;
}

/// <summary>
/// Event args for SaveLoaded event.
/// </summary>
public sealed class SaveLoadedEventArgs : EventArgs
{
    public SaveFile Save { get; init; } = null!;
    public TimeSpan LoadTime { get; init; }
    public string Path { get; init; } = string.Empty;
}

/// <summary>
/// Event args for SaveSaving event.
/// </summary>
public sealed class SaveSavingEventArgs : EventArgs
{
    public SaveFile Save { get; init; } = null!;
    public string TargetPath { get; init; } = string.Empty;
    public bool Cancel { get; set; }
    public string? CancelReason { get; set; }
}

/// <summary>
/// Event args for SaveSaved event.
/// </summary>
public sealed class SaveSavedEventArgs : EventArgs
{
    public SaveFile Save { get; init; } = null!;
    public string Path { get; init; } = string.Empty;
    public BackupInfo? Backup { get; init; }
    public TimeSpan SaveTime { get; init; }
    public long FileSize { get; init; }
}

/// <summary>
/// Backup information.
/// </summary>
public sealed class BackupInfo
{
    public string BackupPath { get; init; } = string.Empty;
    public string OriginalPath { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public long OriginalSize { get; init; }
    public long BackupSize { get; init; }
    public string Checksum { get; init; } = string.Empty;
    public BackupTrigger Trigger { get; init; }
}

/// <summary>
/// What triggered the backup.
/// </summary>
public enum BackupTrigger
{
    Manual,
    PreEdit,
    Scheduled,
    OnClose,
    BeforeRestore
}
