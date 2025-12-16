using Bannerlord.LauncherManager.Models;
using Bannerlord.LauncherManager.Utils;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager;

partial class LauncherManagerHandler
{
    /// <summary>
    /// External<br/>
    /// Detects all conflicts in the current module configuration.
    /// </summary>
    public async Task<ConflictDetectionResult> DetectConflictsAsync()
    {
        var allModules = ExtendedModuleInfoCache.Values.ToList();
        var viewModels = await GetAllModuleViewModelsAsync() ?? [];

        return ConflictResolver.DetectConflicts(allModules, viewModels);
    }

    /// <summary>
    /// External<br/>
    /// Attempts to automatically resolve all auto-resolvable conflicts.
    /// </summary>
    public async Task<AutoResolveResult> AutoResolveConflictsAsync()
    {
        var allModules = ExtendedModuleInfoCache.Values.ToList();
        var viewModels = (await GetAllModuleViewModelsAsync())?.ToList() ?? [];
        var viewModelLookup = viewModels.ToDictionary(vm => vm.ModuleInfoExtended.Id, vm => vm);

        var result = ConflictResolver.AutoResolveAll(allModules, viewModels, viewModelLookup);

        // Save the updated view models
        await SetModuleViewModelsAsync(viewModels);

        return result;
    }

    /// <summary>
    /// External<br/>
    /// Applies a specific resolution to a conflict.
    /// </summary>
    public async Task<ResolutionResult> ApplyResolutionAsync(ModuleConflict conflict, SuggestedResolution resolution)
    {
        var viewModels = (await GetAllModuleViewModelsAsync())?.ToList() ?? [];
        var viewModelLookup = viewModels.ToDictionary(vm => vm.ModuleInfoExtended.Id, vm => vm);

        var result = ConflictResolver.ApplyResolution(conflict, resolution, viewModels, viewModelLookup);

        if (result.Success)
        {
            await SetModuleViewModelsAsync(viewModels);
        }

        return result;
    }

    /// <summary>
    /// External<br/>
    /// Gets conflicts filtered by severity level.
    /// </summary>
    public async Task<IReadOnlyList<ModuleConflict>> GetConflictsBySeverityAsync(int minSeverity)
    {
        var detection = await DetectConflictsAsync();
        return detection.Conflicts
            .Where(c => c.Severity >= minSeverity)
            .OrderByDescending(c => c.Severity)
            .ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets only critical conflicts (severity 5).
    /// </summary>
    public async Task<IReadOnlyList<ModuleConflict>> GetCriticalConflictsAsync()
    {
        return await GetConflictsBySeverityAsync(5);
    }

    /// <summary>
    /// External<br/>
    /// Checks if the current configuration has any conflicts.
    /// </summary>
    public async Task<bool> HasConflictsAsync()
    {
        var detection = await DetectConflictsAsync();
        return detection.HasConflicts;
    }

    /// <summary>
    /// External<br/>
    /// Gets a summary of the current conflict state.
    /// </summary>
    public async Task<string> GetConflictSummaryAsync()
    {
        var detection = await DetectConflictsAsync();
        return detection.Summary;
    }

    /// <summary>
    /// External<br/>
    /// Validates the current configuration and returns true if no critical conflicts exist.
    /// </summary>
    public async Task<bool> ValidateConfigurationAsync()
    {
        var criticalConflicts = await GetCriticalConflictsAsync();
        return criticalConflicts.Count == 0;
    }

    /// <summary>
    /// External<br/>
    /// Gets conflicts for a specific module.
    /// </summary>
    public async Task<IReadOnlyList<ModuleConflict>> GetConflictsForModuleAsync(string moduleId)
    {
        var detection = await DetectConflictsAsync();
        return detection.Conflicts
            .Where(c => c.SourceModuleId == moduleId || c.TargetModuleId == moduleId)
            .ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets conflicts by type.
    /// </summary>
    public async Task<IReadOnlyList<ModuleConflict>> GetConflictsByTypeAsync(ConflictType type)
    {
        var detection = await DetectConflictsAsync();
        return detection.Conflicts
            .Where(c => c.Type == type)
            .ToList();
    }
}
