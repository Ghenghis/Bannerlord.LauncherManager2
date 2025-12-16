using Bannerlord.ModuleManager;

using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Types of conflicts that can occur between modules.
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// A required dependency is missing.
    /// </summary>
    MissingDependency,

    /// <summary>
    /// The version of a dependency doesn't match requirements.
    /// </summary>
    VersionMismatch,

    /// <summary>
    /// Two modules are marked as incompatible.
    /// </summary>
    Incompatible,

    /// <summary>
    /// Module load order is incorrect.
    /// </summary>
    LoadOrderConflict,

    /// <summary>
    /// A dependency of a dependency is missing.
    /// </summary>
    TransitiveDependencyMissing,

    /// <summary>
    /// Circular dependency detected.
    /// </summary>
    CircularDependency
}

/// <summary>
/// Types of resolutions that can be applied to conflicts.
/// </summary>
public enum ResolutionType
{
    /// <summary>
    /// Install the missing dependency.
    /// </summary>
    InstallDependency,

    /// <summary>
    /// Update a module to a compatible version.
    /// </summary>
    UpdateModule,

    /// <summary>
    /// Disable the conflicting module.
    /// </summary>
    DisableModule,

    /// <summary>
    /// Reorder modules to fix load order.
    /// </summary>
    ReorderModules,

    /// <summary>
    /// Enable a disabled dependency.
    /// </summary>
    EnableDependency,

    /// <summary>
    /// User must manually resolve this conflict.
    /// </summary>
    ManualResolution,

    /// <summary>
    /// Ignore the conflict (not recommended).
    /// </summary>
    Ignore
}

/// <summary>
/// Represents a suggested resolution for a conflict.
/// </summary>
public class SuggestedResolution
{
    /// <summary>
    /// Type of resolution being suggested.
    /// </summary>
    public ResolutionType Type { get; set; }

    /// <summary>
    /// Human-readable description of the resolution.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The module ID affected by this resolution.
    /// </summary>
    public string? TargetModuleId { get; set; }

    /// <summary>
    /// For InstallDependency/UpdateModule: the required version.
    /// </summary>
    public string? RequiredVersion { get; set; }

    /// <summary>
    /// For ReorderModules: the new index position.
    /// </summary>
    public int? NewIndex { get; set; }

    /// <summary>
    /// Whether this resolution can be applied automatically.
    /// </summary>
    public bool CanAutoResolve { get; set; }

    /// <summary>
    /// Priority of this resolution (higher = more preferred).
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// Represents a detected conflict between modules.
/// </summary>
public class ModuleConflict
{
    /// <summary>
    /// Unique identifier for this conflict.
    /// </summary>
    public string Id { get; set; } = System.Guid.NewGuid().ToString();

    /// <summary>
    /// Type of conflict detected.
    /// </summary>
    public ConflictType Type { get; set; }

    /// <summary>
    /// Severity level (1-5, 5 being most severe).
    /// </summary>
    public int Severity { get; set; } = 3;

    /// <summary>
    /// The source module that has the conflict.
    /// </summary>
    public string SourceModuleId { get; set; } = string.Empty;

    /// <summary>
    /// The source module name.
    /// </summary>
    public string SourceModuleName { get; set; } = string.Empty;

    /// <summary>
    /// The target module involved in the conflict (if applicable).
    /// </summary>
    public string? TargetModuleId { get; set; }

    /// <summary>
    /// The target module name (if applicable).
    /// </summary>
    public string? TargetModuleName { get; set; }

    /// <summary>
    /// Human-readable description of the conflict.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Detailed technical information about the conflict.
    /// </summary>
    public string? TechnicalDetails { get; set; }

    /// <summary>
    /// Version information related to the conflict.
    /// </summary>
    public string? RequiredVersion { get; set; }

    /// <summary>
    /// Current version (if version mismatch).
    /// </summary>
    public string? CurrentVersion { get; set; }

    /// <summary>
    /// List of suggested resolutions for this conflict.
    /// </summary>
    public List<SuggestedResolution> SuggestedResolutions { get; set; } = new();

    /// <summary>
    /// Whether this conflict has been resolved.
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// The resolution that was applied (if resolved).
    /// </summary>
    public SuggestedResolution? AppliedResolution { get; set; }
}

/// <summary>
/// Result of a conflict detection operation.
/// </summary>
public class ConflictDetectionResult
{
    /// <summary>
    /// Whether any conflicts were found.
    /// </summary>
    public bool HasConflicts => Conflicts.Count > 0;

    /// <summary>
    /// Number of conflicts that can be auto-resolved.
    /// </summary>
    public int AutoResolvableCount { get; set; }

    /// <summary>
    /// Number of conflicts requiring manual resolution.
    /// </summary>
    public int ManualResolutionCount { get; set; }

    /// <summary>
    /// List of all detected conflicts.
    /// </summary>
    public List<ModuleConflict> Conflicts { get; set; } = new();

    /// <summary>
    /// Conflicts grouped by severity.
    /// </summary>
    public Dictionary<int, List<ModuleConflict>> ConflictsBySeverity { get; set; } = new();

    /// <summary>
    /// Summary message about the conflicts.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Result of applying a resolution.
/// </summary>
public class ResolutionResult
{
    /// <summary>
    /// Whether the resolution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if resolution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The conflict that was resolved.
    /// </summary>
    public ModuleConflict? Conflict { get; set; }

    /// <summary>
    /// The resolution that was applied.
    /// </summary>
    public SuggestedResolution? Resolution { get; set; }

    /// <summary>
    /// Any new conflicts that were introduced by this resolution.
    /// </summary>
    public List<ModuleConflict> NewConflicts { get; set; } = new();

    public static ResolutionResult AsSuccess(ModuleConflict conflict, SuggestedResolution resolution) =>
        new() { Success = true, Conflict = conflict, Resolution = resolution };

    public static ResolutionResult AsError(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Result of auto-resolving all conflicts.
/// </summary>
public class AutoResolveResult
{
    /// <summary>
    /// Whether all conflicts were resolved.
    /// </summary>
    public bool AllResolved { get; set; }

    /// <summary>
    /// Number of conflicts that were resolved.
    /// </summary>
    public int ResolvedCount { get; set; }

    /// <summary>
    /// Number of conflicts that could not be resolved.
    /// </summary>
    public int UnresolvedCount { get; set; }

    /// <summary>
    /// Results for each resolution attempt.
    /// </summary>
    public List<ResolutionResult> Results { get; set; } = new();

    /// <summary>
    /// Remaining unresolved conflicts.
    /// </summary>
    public List<ModuleConflict> RemainingConflicts { get; set; } = new();
}
