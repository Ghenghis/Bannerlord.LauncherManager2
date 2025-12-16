using System;
using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Severity level of a health issue.
/// </summary>
public enum HealthIssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Type of health issue detected.
/// </summary>
public enum HealthIssueType
{
    MissingFile,
    CorruptedFile,
    InvalidChecksum,
    MissingDll,
    IncompatibleDll,
    InvalidSubModule,
    MissingAsset,
    PermissionDenied,
    InvalidVersion,
    DuplicateModule,
    ObfuscatedCode
}

/// <summary>
/// Represents a health issue found in a module.
/// </summary>
public class ModuleHealthIssue
{
    /// <summary>
    /// Type of issue.
    /// </summary>
    public HealthIssueType Type { get; set; }

    /// <summary>
    /// Severity of the issue.
    /// </summary>
    public HealthIssueSeverity Severity { get; set; }

    /// <summary>
    /// Module ID affected.
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>
    /// Module name for display.
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the issue.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Affected file path (if applicable).
    /// </summary>
    public string? AffectedFile { get; set; }

    /// <summary>
    /// Expected value (for checksum/version mismatches).
    /// </summary>
    public string? ExpectedValue { get; set; }

    /// <summary>
    /// Actual value found.
    /// </summary>
    public string? ActualValue { get; set; }

    /// <summary>
    /// Suggested fix for the issue.
    /// </summary>
    public string? SuggestedFix { get; set; }

    /// <summary>
    /// Whether this issue can be auto-repaired.
    /// </summary>
    public bool CanAutoRepair { get; set; }
}

/// <summary>
/// Health status of a single module.
/// </summary>
public class ModuleHealthStatus
{
    /// <summary>
    /// Module ID.
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>
    /// Module name.
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Module version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Overall health status (true = healthy).
    /// </summary>
    public bool IsHealthy { get; set; } = true;

    /// <summary>
    /// Total file count in the module.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Number of verified files.
    /// </summary>
    public int VerifiedFiles { get; set; }

    /// <summary>
    /// Number of DLLs in the module.
    /// </summary>
    public int DllCount { get; set; }

    /// <summary>
    /// Whether the module contains obfuscated code.
    /// </summary>
    public bool HasObfuscatedCode { get; set; }

    /// <summary>
    /// List of issues found.
    /// </summary>
    public List<ModuleHealthIssue> Issues { get; set; } = new();

    /// <summary>
    /// When the health check was performed.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Time taken to check this module.
    /// </summary>
    public TimeSpan CheckDuration { get; set; }
}

/// <summary>
/// Overall health report for all modules.
/// </summary>
public class HealthReport
{
    /// <summary>
    /// Whether the overall installation is healthy.
    /// </summary>
    public bool IsHealthy { get; set; } = true;

    /// <summary>
    /// Total modules checked.
    /// </summary>
    public int TotalModules { get; set; }

    /// <summary>
    /// Number of healthy modules.
    /// </summary>
    public int HealthyModules { get; set; }

    /// <summary>
    /// Number of modules with issues.
    /// </summary>
    public int UnhealthyModules { get; set; }

    /// <summary>
    /// Total issues found.
    /// </summary>
    public int TotalIssues { get; set; }

    /// <summary>
    /// Critical issues count.
    /// </summary>
    public int CriticalIssues { get; set; }

    /// <summary>
    /// Error issues count.
    /// </summary>
    public int ErrorIssues { get; set; }

    /// <summary>
    /// Warning issues count.
    /// </summary>
    public int WarningIssues { get; set; }

    /// <summary>
    /// Issues that can be auto-repaired.
    /// </summary>
    public int AutoRepairableIssues { get; set; }

    /// <summary>
    /// Individual module health statuses.
    /// </summary>
    public List<ModuleHealthStatus> ModuleStatuses { get; set; } = new();

    /// <summary>
    /// All issues aggregated.
    /// </summary>
    public List<ModuleHealthIssue> AllIssues { get; set; } = new();

    /// <summary>
    /// When the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total time to generate the report.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Summary message.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Options for health checks.
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// Whether to verify file checksums.
    /// </summary>
    public bool VerifyChecksums { get; set; } = true;

    /// <summary>
    /// Whether to check DLL compatibility.
    /// </summary>
    public bool CheckDllCompatibility { get; set; } = true;

    /// <summary>
    /// Whether to detect obfuscated code.
    /// </summary>
    public bool DetectObfuscation { get; set; } = true;

    /// <summary>
    /// Whether to check file permissions.
    /// </summary>
    public bool CheckPermissions { get; set; } = true;

    /// <summary>
    /// Specific modules to check (null = all).
    /// </summary>
    public List<string>? ModulesToCheck { get; set; }

    /// <summary>
    /// Whether to include native modules.
    /// </summary>
    public bool IncludeNativeModules { get; set; } = false;
}

/// <summary>
/// Result of a repair operation.
/// </summary>
public class RepairResult
{
    /// <summary>
    /// Whether repair was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of issues repaired.
    /// </summary>
    public int RepairedCount { get; set; }

    /// <summary>
    /// Number of issues that couldn't be repaired.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Details of each repair attempt.
    /// </summary>
    public List<RepairAttempt> Attempts { get; set; } = new();
}

/// <summary>
/// Details of a single repair attempt.
/// </summary>
public class RepairAttempt
{
    public ModuleHealthIssue Issue { get; set; } = null!;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ActionTaken { get; set; }
}
