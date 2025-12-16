using Bannerlord.LauncherManager.Models;
using Bannerlord.ModuleManager;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Bannerlord.LauncherManager.Utils;

/// <summary>
/// Detects and resolves conflicts between modules.
/// </summary>
public static class ConflictResolver
{
    /// <summary>
    /// Detects all conflicts in the given module list.
    /// </summary>
    public static ConflictDetectionResult DetectConflicts(
        IReadOnlyList<ModuleInfoExtended> allModules,
        IReadOnlyList<IModuleViewModel> moduleViewModels)
    {
        var result = new ConflictDetectionResult();
        var selectedModules = moduleViewModels
            .Where(vm => vm.IsSelected)
            .Select(vm => vm.ModuleInfoExtended)
            .ToList();

        var moduleDict = allModules.ToDictionary(m => m.Id, m => m);
        var selectedDict = selectedModules.ToDictionary(m => m.Id, m => m);

        foreach (var module in selectedModules)
        {
            // Check for missing dependencies
            DetectMissingDependencies(module, moduleDict, selectedDict, result);

            // Check for version mismatches
            DetectVersionMismatches(module, moduleDict, selectedDict, result);

            // Check for incompatible modules
            DetectIncompatibilities(module, selectedDict, result);
        }

        // Check for load order conflicts
        DetectLoadOrderConflicts(moduleViewModels, result);

        // Calculate summary
        result.AutoResolvableCount = result.Conflicts.Count(c => c.SuggestedResolutions.Any(r => r.CanAutoResolve));
        result.ManualResolutionCount = result.Conflicts.Count - result.AutoResolvableCount;

        // Group by severity
        result.ConflictsBySeverity = result.Conflicts
            .GroupBy(c => c.Severity)
            .ToDictionary(g => g.Key, g => g.ToList());

        result.Summary = GenerateSummary(result);

        return result;
    }

    /// <summary>
    /// Attempts to automatically resolve all auto-resolvable conflicts.
    /// </summary>
    public static AutoResolveResult AutoResolveAll(
        IReadOnlyList<ModuleInfoExtended> allModules,
        List<IModuleViewModel> moduleViewModels,
        Dictionary<string, IModuleViewModel> moduleViewModelLookup)
    {
        var result = new AutoResolveResult();
        var detection = DetectConflicts(allModules, moduleViewModels);

        foreach (var conflict in detection.Conflicts.OrderByDescending(c => c.Severity))
        {
            var autoResolution = conflict.SuggestedResolutions
                .Where(r => r.CanAutoResolve)
                .OrderByDescending(r => r.Priority)
                .FirstOrDefault();

            if (autoResolution != null)
            {
                var resolutionResult = ApplyResolution(conflict, autoResolution, moduleViewModels, moduleViewModelLookup);
                result.Results.Add(resolutionResult);

                if (resolutionResult.Success)
                {
                    result.ResolvedCount++;
                    conflict.IsResolved = true;
                    conflict.AppliedResolution = autoResolution;
                }
                else
                {
                    result.RemainingConflicts.Add(conflict);
                }
            }
            else
            {
                result.RemainingConflicts.Add(conflict);
            }
        }

        result.UnresolvedCount = result.RemainingConflicts.Count;
        result.AllResolved = result.UnresolvedCount == 0;

        return result;
    }

    /// <summary>
    /// Applies a specific resolution to a conflict.
    /// </summary>
    public static ResolutionResult ApplyResolution(
        ModuleConflict conflict,
        SuggestedResolution resolution,
        List<IModuleViewModel> moduleViewModels,
        Dictionary<string, IModuleViewModel> moduleViewModelLookup)
    {
        try
        {
            switch (resolution.Type)
            {
                case ResolutionType.DisableModule:
                    return ApplyDisableModule(conflict, resolution, moduleViewModels, moduleViewModelLookup);

                case ResolutionType.EnableDependency:
                    return ApplyEnableDependency(conflict, resolution, moduleViewModels, moduleViewModelLookup);

                case ResolutionType.ReorderModules:
                    return ApplyReorderModules(conflict, resolution, moduleViewModels, moduleViewModelLookup);

                case ResolutionType.Ignore:
                    conflict.IsResolved = true;
                    conflict.AppliedResolution = resolution;
                    return ResolutionResult.AsSuccess(conflict, resolution);

                case ResolutionType.InstallDependency:
                case ResolutionType.UpdateModule:
                case ResolutionType.ManualResolution:
                    return ResolutionResult.AsError($"Resolution type '{resolution.Type}' requires manual intervention.");

                default:
                    return ResolutionResult.AsError($"Unknown resolution type: {resolution.Type}");
            }
        }
        catch (Exception ex)
        {
            return ResolutionResult.AsError($"Failed to apply resolution: {ex.Message}");
        }
    }

    private static void DetectMissingDependencies(
        ModuleInfoExtended module,
        Dictionary<string, ModuleInfoExtended> allModules,
        Dictionary<string, ModuleInfoExtended> selectedModules,
        ConflictDetectionResult result)
    {
        foreach (var dep in module.DependentModules.Where(d => !d.IsOptional))
        {
            if (!allModules.ContainsKey(dep.Id))
            {
                var conflict = new ModuleConflict
                {
                    Type = ConflictType.MissingDependency,
                    Severity = 5,
                    SourceModuleId = module.Id,
                    SourceModuleName = module.Name,
                    TargetModuleId = dep.Id,
                    TargetModuleName = dep.Id,
                    Description = $"Module '{module.Name}' requires '{dep.Id}' which is not installed.",
                    RequiredVersion = dep.Version.ToString()
                };

                conflict.SuggestedResolutions.Add(new SuggestedResolution
                {
                    Type = ResolutionType.InstallDependency,
                    Description = $"Install '{dep.Id}' version {dep.Version} or later.",
                    TargetModuleId = dep.Id,
                    RequiredVersion = dep.Version.ToString(),
                    CanAutoResolve = false,
                    Priority = 10
                });

                conflict.SuggestedResolutions.Add(new SuggestedResolution
                {
                    Type = ResolutionType.DisableModule,
                    Description = $"Disable '{module.Name}' to remove this requirement.",
                    TargetModuleId = module.Id,
                    CanAutoResolve = true,
                    Priority = 1
                });

                result.Conflicts.Add(conflict);
            }
            else if (!selectedModules.ContainsKey(dep.Id))
            {
                var conflict = new ModuleConflict
                {
                    Type = ConflictType.MissingDependency,
                    Severity = 4,
                    SourceModuleId = module.Id,
                    SourceModuleName = module.Name,
                    TargetModuleId = dep.Id,
                    TargetModuleName = allModules[dep.Id].Name,
                    Description = $"Module '{module.Name}' requires '{allModules[dep.Id].Name}' which is not enabled."
                };

                conflict.SuggestedResolutions.Add(new SuggestedResolution
                {
                    Type = ResolutionType.EnableDependency,
                    Description = $"Enable '{allModules[dep.Id].Name}'.",
                    TargetModuleId = dep.Id,
                    CanAutoResolve = true,
                    Priority = 10
                });

                conflict.SuggestedResolutions.Add(new SuggestedResolution
                {
                    Type = ResolutionType.DisableModule,
                    Description = $"Disable '{module.Name}'.",
                    TargetModuleId = module.Id,
                    CanAutoResolve = true,
                    Priority = 1
                });

                result.Conflicts.Add(conflict);
            }
        }
    }

    private static void DetectVersionMismatches(
        ModuleInfoExtended module,
        Dictionary<string, ModuleInfoExtended> allModules,
        Dictionary<string, ModuleInfoExtended> selectedModules,
        ConflictDetectionResult result)
    {
        foreach (var dep in module.DependentModules.Where(d => !d.IsOptional))
        {
            if (selectedModules.TryGetValue(dep.Id, out var installedModule))
            {
                if (!IsVersionCompatible(dep.Version, installedModule.Version))
                {
                    var conflict = new ModuleConflict
                    {
                        Type = ConflictType.VersionMismatch,
                        Severity = 4,
                        SourceModuleId = module.Id,
                        SourceModuleName = module.Name,
                        TargetModuleId = dep.Id,
                        TargetModuleName = installedModule.Name,
                        Description = $"Module '{module.Name}' requires '{installedModule.Name}' version {dep.Version}, but version {installedModule.Version} is installed.",
                        RequiredVersion = dep.Version.ToString(),
                        CurrentVersion = installedModule.Version.ToString()
                    };

                    conflict.SuggestedResolutions.Add(new SuggestedResolution
                    {
                        Type = ResolutionType.UpdateModule,
                        Description = $"Update '{installedModule.Name}' to version {dep.Version} or later.",
                        TargetModuleId = dep.Id,
                        RequiredVersion = dep.Version.ToString(),
                        CanAutoResolve = false,
                        Priority = 10
                    });

                    conflict.SuggestedResolutions.Add(new SuggestedResolution
                    {
                        Type = ResolutionType.Ignore,
                        Description = "Ignore this version mismatch (may cause issues).",
                        CanAutoResolve = true,
                        Priority = 0
                    });

                    result.Conflicts.Add(conflict);
                }
            }
        }
    }

    private static void DetectIncompatibilities(
        ModuleInfoExtended module,
        Dictionary<string, ModuleInfoExtended> selectedModules,
        ConflictDetectionResult result)
    {
        foreach (var incompatible in module.IncompatibleModules)
        {
            if (selectedModules.ContainsKey(incompatible.Id))
            {
                // Only add conflict once (from the perspective of alphabetically first module)
                if (string.Compare(module.Id, incompatible.Id, StringComparison.Ordinal) > 0)
                    continue;

                var conflict = new ModuleConflict
                {
                    Type = ConflictType.Incompatible,
                    Severity = 5,
                    SourceModuleId = module.Id,
                    SourceModuleName = module.Name,
                    TargetModuleId = incompatible.Id,
                    TargetModuleName = selectedModules[incompatible.Id].Name,
                    Description = $"Module '{module.Name}' is incompatible with '{selectedModules[incompatible.Id].Name}'."
                };

                conflict.SuggestedResolutions.Add(new SuggestedResolution
                {
                    Type = ResolutionType.DisableModule,
                    Description = $"Disable '{module.Name}'.",
                    TargetModuleId = module.Id,
                    CanAutoResolve = true,
                    Priority = 5
                });

                conflict.SuggestedResolutions.Add(new SuggestedResolution
                {
                    Type = ResolutionType.DisableModule,
                    Description = $"Disable '{selectedModules[incompatible.Id].Name}'.",
                    TargetModuleId = incompatible.Id,
                    CanAutoResolve = true,
                    Priority = 5
                });

                result.Conflicts.Add(conflict);
            }
        }
    }

    private static void DetectLoadOrderConflicts(
        IReadOnlyList<IModuleViewModel> moduleViewModels,
        ConflictDetectionResult result)
    {
        var selectedModules = moduleViewModels
            .Where(vm => vm.IsSelected)
            .OrderBy(vm => vm.Index)
            .ToList();

        var moduleDict = selectedModules.ToDictionary(vm => vm.ModuleInfoExtended.Id, vm => vm);

        foreach (var vm in selectedModules)
        {
            var module = vm.ModuleInfoExtended;

            foreach (var dep in module.DependentModules.Where(d => !d.IsOptional))
            {
                if (moduleDict.TryGetValue(dep.Id, out var depVm))
                {
                    if (depVm.Index > vm.Index)
                    {
                        var conflict = new ModuleConflict
                        {
                            Type = ConflictType.LoadOrderConflict,
                            Severity = 3,
                            SourceModuleId = module.Id,
                            SourceModuleName = module.Name,
                            TargetModuleId = dep.Id,
                            TargetModuleName = depVm.ModuleInfoExtended.Name,
                            Description = $"Module '{module.Name}' must load after '{depVm.ModuleInfoExtended.Name}'.",
                            TechnicalDetails = $"Current order: {module.Name}@{vm.Index}, {depVm.ModuleInfoExtended.Name}@{depVm.Index}"
                        };

                        conflict.SuggestedResolutions.Add(new SuggestedResolution
                        {
                            Type = ResolutionType.ReorderModules,
                            Description = $"Move '{module.Name}' after '{depVm.ModuleInfoExtended.Name}'.",
                            TargetModuleId = module.Id,
                            NewIndex = depVm.Index + 1,
                            CanAutoResolve = true,
                            Priority = 10
                        });

                        result.Conflicts.Add(conflict);
                    }
                }
            }
        }
    }

    private static ResolutionResult ApplyDisableModule(
        ModuleConflict conflict,
        SuggestedResolution resolution,
        List<IModuleViewModel> moduleViewModels,
        Dictionary<string, IModuleViewModel> moduleViewModelLookup)
    {
        if (resolution.TargetModuleId == null)
            return ResolutionResult.AsError("No target module specified.");

        if (!moduleViewModelLookup.TryGetValue(resolution.TargetModuleId, out var vm))
            return ResolutionResult.AsError($"Module '{resolution.TargetModuleId}' not found.");

        SortHelper.ToggleModuleSelection(moduleViewModels, moduleViewModelLookup, vm);

        return ResolutionResult.AsSuccess(conflict, resolution);
    }

    private static ResolutionResult ApplyEnableDependency(
        ModuleConflict conflict,
        SuggestedResolution resolution,
        List<IModuleViewModel> moduleViewModels,
        Dictionary<string, IModuleViewModel> moduleViewModelLookup)
    {
        if (resolution.TargetModuleId == null)
            return ResolutionResult.AsError("No target module specified.");

        if (!moduleViewModelLookup.TryGetValue(resolution.TargetModuleId, out var vm))
            return ResolutionResult.AsError($"Module '{resolution.TargetModuleId}' not found.");

        if (!vm.IsSelected)
        {
            SortHelper.ToggleModuleSelection(moduleViewModels, moduleViewModelLookup, vm);
        }

        return ResolutionResult.AsSuccess(conflict, resolution);
    }

    private static ResolutionResult ApplyReorderModules(
        ModuleConflict conflict,
        SuggestedResolution resolution,
        List<IModuleViewModel> moduleViewModels,
        Dictionary<string, IModuleViewModel> moduleViewModelLookup)
    {
        if (resolution.TargetModuleId == null || !resolution.NewIndex.HasValue)
            return ResolutionResult.AsError("Missing target module or new index.");

        if (!moduleViewModelLookup.TryGetValue(resolution.TargetModuleId, out var vm))
            return ResolutionResult.AsError($"Module '{resolution.TargetModuleId}' not found.");

        var changeResult = SortHelper.ChangeModulePosition(moduleViewModels, moduleViewModelLookup, vm, resolution.NewIndex.Value);

        if (!changeResult.Success)
            return ResolutionResult.AsError("Failed to reorder modules.");

        return ResolutionResult.AsSuccess(conflict, resolution);
    }

    private static bool IsVersionCompatible(ApplicationVersion required, ApplicationVersion actual)
    {
        if (required.ApplicationVersionType == ApplicationVersionType.Invalid)
            return true;

        return actual.Major >= required.Major &&
               (actual.Major > required.Major || actual.Minor >= required.Minor);
    }

    private static string GenerateSummary(ConflictDetectionResult result)
    {
        if (!result.HasConflicts)
            return "No conflicts detected.";

        var parts = new List<string>();

        var criticalCount = result.Conflicts.Count(c => c.Severity >= 5);
        var warningCount = result.Conflicts.Count(c => c.Severity >= 3 && c.Severity < 5);
        var infoCount = result.Conflicts.Count(c => c.Severity < 3);

        if (criticalCount > 0)
            parts.Add($"{criticalCount} critical");
        if (warningCount > 0)
            parts.Add($"{warningCount} warnings");
        if (infoCount > 0)
            parts.Add($"{infoCount} info");

        var summary = $"Found {result.Conflicts.Count} conflicts ({string.Join(", ", parts)}).";

        if (result.AutoResolvableCount > 0)
            summary += $" {result.AutoResolvableCount} can be auto-resolved.";

        return summary;
    }
}
