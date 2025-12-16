using System;
using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Compatibility status between save and mods.
/// </summary>
public enum SaveCompatibilityStatus
{
    Compatible,
    MinorIssues,
    MajorIssues,
    Incompatible,
    Unknown
}

/// <summary>
/// Type of save compatibility issue.
/// </summary>
public enum SaveIssueType
{
    MissingMod,
    VersionMismatch,
    ExtraMod,
    LoadOrderDifference,
    CorruptedData,
    GameVersionMismatch
}

/// <summary>
/// Severity of a save issue.
/// </summary>
public enum SaveIssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Information about a mod required by a save.
/// </summary>
public class SaveModuleInfo
{
    /// <summary>
    /// Module ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Module name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Version used when save was created.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether this module is currently installed.
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// Currently installed version (if different).
    /// </summary>
    public string? InstalledVersion { get; set; }

    /// <summary>
    /// Position in load order when saved.
    /// </summary>
    public int LoadOrder { get; set; }

    /// <summary>
    /// Whether the module is essential for the save.
    /// </summary>
    public bool IsEssential { get; set; }
}

/// <summary>
/// A specific issue found during save analysis.
/// </summary>
public class SaveIssue
{
    /// <summary>
    /// Type of issue.
    /// </summary>
    public SaveIssueType Type { get; set; }

    /// <summary>
    /// Severity level.
    /// </summary>
    public SaveIssueSeverity Severity { get; set; }

    /// <summary>
    /// Affected module ID.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// Description of the issue.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Suggested fix.
    /// </summary>
    public string? Suggestion { get; set; }

    /// <summary>
    /// Whether loading might still work.
    /// </summary>
    public bool MightStillLoad { get; set; }
}

/// <summary>
/// Complete analysis of a save file.
/// </summary>
public class SaveAnalysisResult
{
    /// <summary>
    /// Save file name.
    /// </summary>
    public string SaveFileName { get; set; } = string.Empty;

    /// <summary>
    /// When the save was created.
    /// </summary>
    public DateTime? SaveDate { get; set; }

    /// <summary>
    /// Game version when saved.
    /// </summary>
    public string? GameVersion { get; set; }

    /// <summary>
    /// Current game version.
    /// </summary>
    public string? CurrentGameVersion { get; set; }

    /// <summary>
    /// Overall compatibility status.
    /// </summary>
    public SaveCompatibilityStatus Status { get; set; } = SaveCompatibilityStatus.Unknown;

    /// <summary>
    /// Modules required by the save.
    /// </summary>
    public List<SaveModuleInfo> RequiredModules { get; set; } = new();

    /// <summary>
    /// Modules currently installed but not in save.
    /// </summary>
    public List<SaveModuleInfo> ExtraModules { get; set; } = new();

    /// <summary>
    /// Issues found during analysis.
    /// </summary>
    public List<SaveIssue> Issues { get; set; } = new();

    /// <summary>
    /// Number of missing mods.
    /// </summary>
    public int MissingModCount => RequiredModules.Count(m => !m.IsInstalled);

    /// <summary>
    /// Number of version mismatches.
    /// </summary>
    public int VersionMismatchCount => RequiredModules.Count(m => 
        m.IsInstalled && !string.IsNullOrEmpty(m.InstalledVersion) && m.InstalledVersion != m.Version);

    /// <summary>
    /// Whether it's safe to load.
    /// </summary>
    public bool IsSafeToLoad => Status == SaveCompatibilityStatus.Compatible || 
                                 Status == SaveCompatibilityStatus.MinorIssues;

    /// <summary>
    /// Recommended action.
    /// </summary>
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Options for save analysis.
/// </summary>
public class SaveAnalysisOptions
{
    /// <summary>
    /// Check version mismatches.
    /// </summary>
    public bool CheckVersions { get; set; } = true;

    /// <summary>
    /// Check load order differences.
    /// </summary>
    public bool CheckLoadOrder { get; set; } = true;

    /// <summary>
    /// Include informational issues.
    /// </summary>
    public bool IncludeInfoIssues { get; set; }

    /// <summary>
    /// Check for extra mods not in save.
    /// </summary>
    public bool CheckExtraMods { get; set; } = true;
}

/// <summary>
/// Summary of multiple saves.
/// </summary>
public class SaveCollectionSummary
{
    /// <summary>
    /// Total saves analyzed.
    /// </summary>
    public int TotalSaves { get; set; }

    /// <summary>
    /// Saves that are compatible.
    /// </summary>
    public int CompatibleSaves { get; set; }

    /// <summary>
    /// Saves with issues.
    /// </summary>
    public int SavesWithIssues { get; set; }

    /// <summary>
    /// Saves that cannot be loaded.
    /// </summary>
    public int IncompatibleSaves { get; set; }

    /// <summary>
    /// Most commonly missing mods.
    /// </summary>
    public List<string> CommonlyMissingMods { get; set; } = new();
}

/// <summary>
/// Export format for save mod requirements.
/// </summary>
public class SaveModRequirements
{
    /// <summary>
    /// Save file name.
    /// </summary>
    public string SaveFileName { get; set; } = string.Empty;

    /// <summary>
    /// Game version.
    /// </summary>
    public string? GameVersion { get; set; }

    /// <summary>
    /// Required module IDs in order.
    /// </summary>
    public List<string> ModuleIds { get; set; } = new();

    /// <summary>
    /// Module versions.
    /// </summary>
    public Dictionary<string, string> ModuleVersions { get; set; } = new();

    /// <summary>
    /// Export timestamp.
    /// </summary>
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}
