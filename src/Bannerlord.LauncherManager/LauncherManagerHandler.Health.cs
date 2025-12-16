using Bannerlord.LauncherManager.Models;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager;

partial class LauncherManagerHandler
{
    /// <summary>
    /// External<br/>
    /// Performs a comprehensive health check on all modules.
    /// </summary>
    public async Task<HealthReport> CheckModuleHealthAsync(HealthCheckOptions? options = null)
    {
        options ??= new HealthCheckOptions();
        var stopwatch = Stopwatch.StartNew();
        var report = new HealthReport();

        try
        {
            var modules = await GetModulesAsync();
            var installPath = await GetInstallPathAsync();

            foreach (var module in modules)
            {
                if (options.ModulesToCheck != null && !options.ModulesToCheck.Contains(module.Id))
                    continue;

                if (!options.IncludeNativeModules && module.IsNative())
                    continue;

                var moduleStatus = await CheckSingleModuleHealthAsync(module, installPath, options);
                report.ModuleStatuses.Add(moduleStatus);
                report.AllIssues.AddRange(moduleStatus.Issues);

                if (moduleStatus.IsHealthy)
                    report.HealthyModules++;
                else
                    report.UnhealthyModules++;
            }

            report.TotalModules = report.ModuleStatuses.Count;
            report.TotalIssues = report.AllIssues.Count;
            report.CriticalIssues = report.AllIssues.Count(i => i.Severity == HealthIssueSeverity.Critical);
            report.ErrorIssues = report.AllIssues.Count(i => i.Severity == HealthIssueSeverity.Error);
            report.WarningIssues = report.AllIssues.Count(i => i.Severity == HealthIssueSeverity.Warning);
            report.AutoRepairableIssues = report.AllIssues.Count(i => i.CanAutoRepair);
            report.IsHealthy = report.CriticalIssues == 0 && report.ErrorIssues == 0;

            stopwatch.Stop();
            report.Duration = stopwatch.Elapsed;
            report.Summary = GenerateHealthSummary(report);
        }
        catch (Exception ex)
        {
            report.Summary = $"Health check failed: {ex.Message}";
            report.IsHealthy = false;
        }

        return report;
    }

    /// <summary>
    /// External<br/>
    /// Checks health of a single module.
    /// </summary>
    public async Task<ModuleHealthStatus> CheckSingleModuleHealthAsync(string moduleId, HealthCheckOptions? options = null)
    {
        var modules = await GetModulesAsync();
        var module = modules.FirstOrDefault(m => m.Id == moduleId);
        
        if (module == null)
        {
            return new ModuleHealthStatus
            {
                ModuleId = moduleId,
                ModuleName = moduleId,
                IsHealthy = false,
                Issues = new List<ModuleHealthIssue>
                {
                    new()
                    {
                        Type = HealthIssueType.MissingFile,
                        Severity = HealthIssueSeverity.Critical,
                        ModuleId = moduleId,
                        Description = "Module not found"
                    }
                }
            };
        }

        var installPath = await GetInstallPathAsync();
        return await CheckSingleModuleHealthAsync(module, installPath, options ?? new HealthCheckOptions());
    }

    /// <summary>
    /// External<br/>
    /// Validates all module files exist.
    /// </summary>
    public async Task<IReadOnlyList<ModuleHealthIssue>> ValidateModuleFilesAsync(string moduleId)
    {
        var issues = new List<ModuleHealthIssue>();
        var modules = await GetModulesAsync();
        var module = modules.FirstOrDefault(m => m.Id == moduleId);

        if (module == null)
        {
            issues.Add(new ModuleHealthIssue
            {
                Type = HealthIssueType.MissingFile,
                Severity = HealthIssueSeverity.Critical,
                ModuleId = moduleId,
                Description = "Module not found"
            });
            return issues;
        }

        var installPath = await GetInstallPathAsync();
        var modulePath = Path.Combine(installPath, Constants.ModulesFolder, module.Id);

        if (!Directory.Exists(modulePath))
        {
            issues.Add(new ModuleHealthIssue
            {
                Type = HealthIssueType.MissingFile,
                Severity = HealthIssueSeverity.Critical,
                ModuleId = moduleId,
                ModuleName = module.Name,
                Description = "Module directory not found",
                AffectedFile = modulePath
            });
            return issues;
        }

        // Check SubModule.xml
        var subModulePath = Path.Combine(modulePath, Constants.SubModuleName);
        if (!File.Exists(subModulePath))
        {
            issues.Add(new ModuleHealthIssue
            {
                Type = HealthIssueType.InvalidSubModule,
                Severity = HealthIssueSeverity.Critical,
                ModuleId = moduleId,
                ModuleName = module.Name,
                Description = "SubModule.xml not found",
                AffectedFile = subModulePath
            });
        }

        return issues;
    }

    /// <summary>
    /// External<br/>
    /// Detects corrupted modules.
    /// </summary>
    public async Task<IReadOnlyList<ModuleHealthIssue>> DetectCorruptedModulesAsync()
    {
        var report = await CheckModuleHealthAsync(new HealthCheckOptions
        {
            VerifyChecksums = true,
            CheckDllCompatibility = true
        });

        return report.AllIssues
            .Where(i => i.Type is HealthIssueType.CorruptedFile or HealthIssueType.InvalidChecksum)
            .ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets modules with obfuscated code.
    /// </summary>
    public async Task<IReadOnlyList<ModuleHealthStatus>> GetObfuscatedModulesAsync()
    {
        var report = await CheckModuleHealthAsync(new HealthCheckOptions
        {
            DetectObfuscation = true
        });

        return report.ModuleStatuses.Where(m => m.HasObfuscatedCode).ToList();
    }

    /// <summary>
    /// External<br/>
    /// Attempts to repair auto-repairable issues.
    /// </summary>
    public async Task<RepairResult> RepairModuleIssuesAsync(string moduleId)
    {
        var result = new RepairResult();
        var status = await CheckSingleModuleHealthAsync(moduleId);

        foreach (var issue in status.Issues.Where(i => i.CanAutoRepair))
        {
            var attempt = new RepairAttempt { Issue = issue };

            try
            {
                switch (issue.Type)
                {
                    case HealthIssueType.PermissionDenied:
                        // Attempt to fix file permissions
                        if (!string.IsNullOrEmpty(issue.AffectedFile) && File.Exists(issue.AffectedFile))
                        {
                            var attributes = File.GetAttributes(issue.AffectedFile);
                            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                            {
                                File.SetAttributes(issue.AffectedFile, attributes & ~FileAttributes.ReadOnly);
                                attempt.Success = true;
                                attempt.ActionTaken = "Removed read-only attribute";
                                result.RepairedCount++;
                            }
                        }
                        break;

                    default:
                        attempt.Success = false;
                        attempt.ErrorMessage = "Repair not implemented for this issue type";
                        result.FailedCount++;
                        break;
                }
            }
            catch (Exception ex)
            {
                attempt.Success = false;
                attempt.ErrorMessage = ex.Message;
                result.FailedCount++;
            }

            result.Attempts.Add(attempt);
        }

        result.Success = result.FailedCount == 0;
        return result;
    }

    /// <summary>
    /// External<br/>
    /// Gets a quick health summary without full scan.
    /// </summary>
    public async Task<string> GetQuickHealthSummaryAsync()
    {
        var modules = await GetModulesAsync();
        var installPath = await GetInstallPathAsync();
        var issues = 0;

        foreach (var module in modules)
        {
            var modulePath = Path.Combine(installPath, Constants.ModulesFolder, module.Id);
            if (!Directory.Exists(modulePath))
                issues++;
            else if (!File.Exists(Path.Combine(modulePath, Constants.SubModuleName)))
                issues++;
        }

        if (issues == 0)
            return $"All {modules.Count()} modules appear healthy.";
        
        return $"Found {issues} potential issues in {modules.Count()} modules. Run full health check for details.";
    }

    private async Task<ModuleHealthStatus> CheckSingleModuleHealthAsync(
        ModuleInfoExtendedWithMetadata module,
        string installPath,
        HealthCheckOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var status = new ModuleHealthStatus
        {
            ModuleId = module.Id,
            ModuleName = module.Name,
            Version = module.Version.ToString()
        };

        var modulePath = Path.Combine(installPath, Constants.ModulesFolder, module.Id);

        // Check if module directory exists
        if (!Directory.Exists(modulePath))
        {
            status.IsHealthy = false;
            status.Issues.Add(new ModuleHealthIssue
            {
                Type = HealthIssueType.MissingFile,
                Severity = HealthIssueSeverity.Critical,
                ModuleId = module.Id,
                ModuleName = module.Name,
                Description = "Module directory not found",
                AffectedFile = modulePath
            });
            return status;
        }

        // Count files
        try
        {
            var files = Directory.GetFiles(modulePath, "*", SearchOption.AllDirectories);
            status.TotalFiles = files.Length;

            // Check DLLs
            var dlls = files.Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).ToArray();
            status.DllCount = dlls.Length;

            if (options.CheckDllCompatibility)
            {
                foreach (var dll in dlls)
                {
                    await CheckDllHealthAsync(dll, module, status);
                }
            }

            // Check for obfuscation
            if (options.DetectObfuscation)
            {
                status.HasObfuscatedCode = await IsObfuscatedAsync(module);
                if (status.HasObfuscatedCode)
                {
                    status.Issues.Add(new ModuleHealthIssue
                    {
                        Type = HealthIssueType.ObfuscatedCode,
                        Severity = HealthIssueSeverity.Info,
                        ModuleId = module.Id,
                        ModuleName = module.Name,
                        Description = "Module contains obfuscated code"
                    });
                }
            }

            // Check file permissions
            if (options.CheckPermissions)
            {
                CheckFilePermissions(modulePath, module, status);
            }

            // Verify SubModule.xml
            var subModulePath = Path.Combine(modulePath, Constants.SubModuleName);
            if (!File.Exists(subModulePath))
            {
                status.IsHealthy = false;
                status.Issues.Add(new ModuleHealthIssue
                {
                    Type = HealthIssueType.InvalidSubModule,
                    Severity = HealthIssueSeverity.Critical,
                    ModuleId = module.Id,
                    ModuleName = module.Name,
                    Description = "SubModule.xml not found",
                    AffectedFile = subModulePath
                });
            }
            else
            {
                status.VerifiedFiles++;
            }

            status.VerifiedFiles += dlls.Length;
        }
        catch (Exception ex)
        {
            status.Issues.Add(new ModuleHealthIssue
            {
                Type = HealthIssueType.PermissionDenied,
                Severity = HealthIssueSeverity.Error,
                ModuleId = module.Id,
                ModuleName = module.Name,
                Description = $"Error scanning module: {ex.Message}"
            });
        }

        status.IsHealthy = !status.Issues.Any(i => i.Severity >= HealthIssueSeverity.Error);
        stopwatch.Stop();
        status.CheckDuration = stopwatch.Elapsed;

        return status;
    }

    private static Task CheckDllHealthAsync(string dllPath, ModuleInfoExtendedWithMetadata module, ModuleHealthStatus status)
    {
        try
        {
            // Check if DLL is readable
            using var stream = File.OpenRead(dllPath);
            if (stream.Length == 0)
            {
                status.Issues.Add(new ModuleHealthIssue
                {
                    Type = HealthIssueType.CorruptedFile,
                    Severity = HealthIssueSeverity.Error,
                    ModuleId = module.Id,
                    ModuleName = module.Name,
                    Description = "DLL file is empty",
                    AffectedFile = dllPath
                });
            }

            // Check PE header
            var buffer = new byte[2];
            stream.Read(buffer, 0, 2);
            if (buffer[0] != 'M' || buffer[1] != 'Z')
            {
                status.Issues.Add(new ModuleHealthIssue
                {
                    Type = HealthIssueType.CorruptedFile,
                    Severity = HealthIssueSeverity.Error,
                    ModuleId = module.Id,
                    ModuleName = module.Name,
                    Description = "Invalid PE header - DLL may be corrupted",
                    AffectedFile = dllPath
                });
            }
        }
        catch (Exception ex)
        {
            status.Issues.Add(new ModuleHealthIssue
            {
                Type = HealthIssueType.PermissionDenied,
                Severity = HealthIssueSeverity.Warning,
                ModuleId = module.Id,
                ModuleName = module.Name,
                Description = $"Cannot read DLL: {ex.Message}",
                AffectedFile = dllPath,
                CanAutoRepair = true
            });
        }

        return Task.CompletedTask;
    }

    private static void CheckFilePermissions(string modulePath, ModuleInfoExtendedWithMetadata module, ModuleHealthStatus status)
    {
        try
        {
            var testFile = Path.Combine(modulePath, ".permission_test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch
        {
            status.Issues.Add(new ModuleHealthIssue
            {
                Type = HealthIssueType.PermissionDenied,
                Severity = HealthIssueSeverity.Warning,
                ModuleId = module.Id,
                ModuleName = module.Name,
                Description = "Cannot write to module directory",
                AffectedFile = modulePath,
                SuggestedFix = "Run as administrator or check folder permissions"
            });
        }
    }

    private static string GenerateHealthSummary(HealthReport report)
    {
        if (report.IsHealthy)
            return $"All {report.TotalModules} modules are healthy.";

        var parts = new List<string>();
        if (report.CriticalIssues > 0)
            parts.Add($"{report.CriticalIssues} critical");
        if (report.ErrorIssues > 0)
            parts.Add($"{report.ErrorIssues} errors");
        if (report.WarningIssues > 0)
            parts.Add($"{report.WarningIssues} warnings");

        var summary = $"Found {report.TotalIssues} issues ({string.Join(", ", parts)}) in {report.UnhealthyModules}/{report.TotalModules} modules.";

        if (report.AutoRepairableIssues > 0)
            summary += $" {report.AutoRepairableIssues} can be auto-repaired.";

        return summary;
    }
}
