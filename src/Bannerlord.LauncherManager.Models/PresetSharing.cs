using System;
using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Visibility of a preset.
/// </summary>
public enum PresetVisibility
{
    Private,
    Public,
    Unlisted
}

/// <summary>
/// Status of applying a preset.
/// </summary>
public enum PresetApplyStatus
{
    Success,
    PartialSuccess,
    MissingMods,
    Failed
}

/// <summary>
/// A module entry in a preset.
/// </summary>
public class PresetModuleEntry
{
    /// <summary>
    /// Module ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Module name for display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Version when preset was created.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether the module is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Position in load order.
    /// </summary>
    public int LoadOrder { get; set; }

    /// <summary>
    /// Whether this is a required mod.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Download URL if known.
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// NexusMods ID if applicable.
    /// </summary>
    public string? NexusModsId { get; set; }

    /// <summary>
    /// Steam Workshop ID if applicable.
    /// </summary>
    public string? SteamWorkshopId { get; set; }
}

/// <summary>
/// A shareable mod preset.
/// </summary>
public class ModPreset
{
    /// <summary>
    /// Unique preset ID.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    /// <summary>
    /// Preset name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Author name.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Game version this preset is for.
    /// </summary>
    public string? GameVersion { get; set; }

    /// <summary>
    /// Visibility setting.
    /// </summary>
    public PresetVisibility Visibility { get; set; } = PresetVisibility.Private;

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Modules in this preset.
    /// </summary>
    public List<PresetModuleEntry> Modules { get; set; } = new();

    /// <summary>
    /// Short share code (e.g., BL-ABC123).
    /// </summary>
    public string ShareCode => $"BL-{Id}";

    /// <summary>
    /// Total module count.
    /// </summary>
    public int ModuleCount => Modules.Count;

    /// <summary>
    /// Enabled module count.
    /// </summary>
    public int EnabledCount => Modules.Count(m => m.IsEnabled);

    /// <summary>
    /// Preset format version.
    /// </summary>
    public int FormatVersion { get; set; } = 1;
}

/// <summary>
/// Result of applying a preset.
/// </summary>
public class PresetApplyResult
{
    /// <summary>
    /// Overall status.
    /// </summary>
    public PresetApplyStatus Status { get; set; }

    /// <summary>
    /// Modules successfully applied.
    /// </summary>
    public List<string> Applied { get; set; } = new();

    /// <summary>
    /// Modules that were missing.
    /// </summary>
    public List<PresetModuleEntry> Missing { get; set; } = new();

    /// <summary>
    /// Modules with version mismatches.
    /// </summary>
    public List<PresetModuleEntry> VersionMismatches { get; set; } = new();

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether all required mods were found.
    /// </summary>
    public bool AllRequiredFound => !Missing.Any(m => m.IsRequired);
}

/// <summary>
/// Options for creating a preset.
/// </summary>
public class PresetCreateOptions
{
    /// <summary>
    /// Include only enabled modules.
    /// </summary>
    public bool EnabledOnly { get; set; } = true;

    /// <summary>
    /// Include native modules.
    /// </summary>
    public bool IncludeNative { get; set; }

    /// <summary>
    /// Include download URLs.
    /// </summary>
    public bool IncludeDownloadUrls { get; set; } = true;

    /// <summary>
    /// Include version information.
    /// </summary>
    public bool IncludeVersions { get; set; } = true;
}

/// <summary>
/// Options for applying a preset.
/// </summary>
public class PresetApplyOptions
{
    /// <summary>
    /// Skip missing mods and apply what's available.
    /// </summary>
    public bool SkipMissing { get; set; }

    /// <summary>
    /// Ignore version mismatches.
    /// </summary>
    public bool IgnoreVersions { get; set; } = true;

    /// <summary>
    /// Disable mods not in preset.
    /// </summary>
    public bool DisableOthers { get; set; }

    /// <summary>
    /// Create backup before applying.
    /// </summary>
    public bool CreateBackup { get; set; } = true;
}

/// <summary>
/// Collection of saved presets.
/// </summary>
public class PresetCollection
{
    /// <summary>
    /// User's presets.
    /// </summary>
    public List<ModPreset> Presets { get; set; } = new();

    /// <summary>
    /// Imported presets.
    /// </summary>
    public List<ModPreset> ImportedPresets { get; set; } = new();

    /// <summary>
    /// Favorite preset IDs.
    /// </summary>
    public List<string> Favorites { get; set; } = new();

    /// <summary>
    /// Last applied preset ID.
    /// </summary>
    public string? LastAppliedId { get; set; }

    /// <summary>
    /// Format version.
    /// </summary>
    public int Version { get; set; } = 1;
}

/// <summary>
/// Import result from share code or URL.
/// </summary>
public class PresetImportResult
{
    /// <summary>
    /// Whether import was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Imported preset.
    /// </summary>
    public ModPreset? Preset { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether this was a duplicate.
    /// </summary>
    public bool WasDuplicate { get; set; }
}

/// <summary>
/// Preset comparison result.
/// </summary>
public class PresetComparisonResult
{
    /// <summary>
    /// Modules only in first preset.
    /// </summary>
    public List<string> OnlyInFirst { get; set; } = new();

    /// <summary>
    /// Modules only in second preset.
    /// </summary>
    public List<string> OnlyInSecond { get; set; } = new();

    /// <summary>
    /// Modules in both.
    /// </summary>
    public List<string> InBoth { get; set; } = new();

    /// <summary>
    /// Modules with different positions.
    /// </summary>
    public List<string> DifferentOrder { get; set; } = new();

    /// <summary>
    /// Similarity percentage.
    /// </summary>
    public int SimilarityPercent { get; set; }
}
