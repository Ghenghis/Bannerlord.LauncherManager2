using Bannerlord.LauncherManager.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager;

partial class LauncherManagerHandler
{
    private static readonly JsonSerializerOptions SaveAnalysisJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// External<br/>
    /// Analyzes a save file for mod compatibility.
    /// </summary>
    public async Task<SaveAnalysisResult> AnalyzeSaveAsync(string saveFileName, SaveAnalysisOptions? options = null)
    {
        options ??= new SaveAnalysisOptions();
        
        var result = new SaveAnalysisResult
        {
            SaveFileName = saveFileName,
            CurrentGameVersion = await GetGameVersionAsync()
        };

        try
        {
            // Get save metadata
            var saves = await GetSaveFilesAsync();
            var saveMetadata = saves.FirstOrDefault(s => s.Name == saveFileName);

            if (saveMetadata == null)
            {
                result.Status = SaveCompatibilityStatus.Unknown;
                result.Issues.Add(new SaveIssue
                {
                    Type = SaveIssueType.CorruptedData,
                    Severity = SaveIssueSeverity.Critical,
                    Description = "Save file not found or could not be read."
                });
                return result;
            }

            // Extract save info
            result.GameVersion = saveMetadata.ApplicationVersion?.ToString();

            // Get current modules
            var installedModules = await GetModulesAsync();
            var installedDict = installedModules.ToDictionary(m => m.Id, m => m);
            
            var viewModels = await GetModuleViewModelsAsync();
            var enabledIds = viewModels?.Where(vm => vm.IsSelected).Select(vm => vm.ModuleInfoExtended.Id).ToHashSet()
                             ?? new HashSet<string>();

            // Parse modules from save
            if (saveMetadata.Modules != null)
            {
                var loadOrder = 0;
                foreach (var saveModule in saveMetadata.Modules)
                {
                    var moduleInfo = new SaveModuleInfo
                    {
                        Id = saveModule.Id,
                        Name = saveModule.Name,
                        Version = saveModule.Version?.ToString() ?? "Unknown",
                        LoadOrder = loadOrder++,
                        IsInstalled = installedDict.ContainsKey(saveModule.Id)
                    };

                    if (moduleInfo.IsInstalled)
                    {
                        moduleInfo.InstalledVersion = installedDict[saveModule.Id].Version.ToString();
                    }

                    // Check if essential (non-native)
                    moduleInfo.IsEssential = !installedDict.TryGetValue(saveModule.Id, out var mod) || !mod.IsNative();

                    result.RequiredModules.Add(moduleInfo);

                    // Check for missing mod
                    if (!moduleInfo.IsInstalled)
                    {
                        result.Issues.Add(new SaveIssue
                        {
                            Type = SaveIssueType.MissingMod,
                            Severity = moduleInfo.IsEssential ? SaveIssueSeverity.Error : SaveIssueSeverity.Warning,
                            ModuleId = saveModule.Id,
                            Description = $"Module '{saveModule.Name}' is required but not installed.",
                            Suggestion = $"Install '{saveModule.Name}' version {moduleInfo.Version} or compatible.",
                            MightStillLoad = !moduleInfo.IsEssential
                        });
                    }
                    // Check for version mismatch
                    else if (options.CheckVersions && moduleInfo.InstalledVersion != moduleInfo.Version)
                    {
                        result.Issues.Add(new SaveIssue
                        {
                            Type = SaveIssueType.VersionMismatch,
                            Severity = SaveIssueSeverity.Warning,
                            ModuleId = saveModule.Id,
                            Description = $"Module '{saveModule.Name}' version mismatch: save has {moduleInfo.Version}, installed is {moduleInfo.InstalledVersion}.",
                            Suggestion = "Version differences may cause issues. Consider matching versions.",
                            MightStillLoad = true
                        });
                    }
                }
            }

            // Check for extra mods
            if (options.CheckExtraMods)
            {
                var saveModuleIds = result.RequiredModules.Select(m => m.Id).ToHashSet();
                foreach (var installed in installedModules.Where(m => enabledIds.Contains(m.Id) && !saveModuleIds.Contains(m.Id)))
                {
                    if (installed.IsNative())
                        continue;

                    result.ExtraModules.Add(new SaveModuleInfo
                    {
                        Id = installed.Id,
                        Name = installed.Name,
                        Version = installed.Version.ToString(),
                        IsInstalled = true,
                        InstalledVersion = installed.Version.ToString()
                    });

                    if (options.IncludeInfoIssues)
                    {
                        result.Issues.Add(new SaveIssue
                        {
                            Type = SaveIssueType.ExtraMod,
                            Severity = SaveIssueSeverity.Info,
                            ModuleId = installed.Id,
                            Description = $"Module '{installed.Name}' is enabled but was not in the save.",
                            MightStillLoad = true
                        });
                    }
                }
            }

            // Check game version
            if (!string.IsNullOrEmpty(result.GameVersion) && result.GameVersion != result.CurrentGameVersion)
            {
                result.Issues.Add(new SaveIssue
                {
                    Type = SaveIssueType.GameVersionMismatch,
                    Severity = SaveIssueSeverity.Warning,
                    Description = $"Save was created with game version {result.GameVersion}, current version is {result.CurrentGameVersion}.",
                    MightStillLoad = true
                });
            }

            // Determine overall status
            result.Status = DetermineCompatibilityStatus(result);
            result.RecommendedAction = GetRecommendedAction(result);
        }
        catch (Exception ex)
        {
            result.Status = SaveCompatibilityStatus.Unknown;
            result.Issues.Add(new SaveIssue
            {
                Type = SaveIssueType.CorruptedData,
                Severity = SaveIssueSeverity.Critical,
                Description = $"Error analyzing save: {ex.Message}"
            });
        }

        return result;
    }

    /// <summary>
    /// External<br/>
    /// Analyzes all saves.
    /// </summary>
    public async Task<IReadOnlyList<SaveAnalysisResult>> AnalyzeAllSavesAsync(SaveAnalysisOptions? options = null)
    {
        var saves = await GetSaveFilesAsync();
        var results = new List<SaveAnalysisResult>();

        foreach (var save in saves)
        {
            var result = await AnalyzeSaveAsync(save.Name, options);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// External<br/>
    /// Gets a summary of all saves compatibility.
    /// </summary>
    public async Task<SaveCollectionSummary> GetSaveCollectionSummaryAsync()
    {
        var results = await AnalyzeAllSavesAsync(new SaveAnalysisOptions { IncludeInfoIssues = false });
        var summary = new SaveCollectionSummary
        {
            TotalSaves = results.Count,
            CompatibleSaves = results.Count(r => r.Status == SaveCompatibilityStatus.Compatible),
            SavesWithIssues = results.Count(r => r.Status == SaveCompatibilityStatus.MinorIssues || 
                                                  r.Status == SaveCompatibilityStatus.MajorIssues),
            IncompatibleSaves = results.Count(r => r.Status == SaveCompatibilityStatus.Incompatible)
        };

        // Find commonly missing mods
        var missingMods = results
            .SelectMany(r => r.RequiredModules.Where(m => !m.IsInstalled).Select(m => m.Id))
            .GroupBy(id => id)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToList();

        summary.CommonlyMissingMods = missingMods;

        return summary;
    }

    /// <summary>
    /// External<br/>
    /// Gets missing mods for a save.
    /// </summary>
    public async Task<IReadOnlyList<SaveModuleInfo>> GetMissingModsForSaveAsync(string saveFileName)
    {
        var result = await AnalyzeSaveAsync(saveFileName);
        return result.RequiredModules.Where(m => !m.IsInstalled).ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets version mismatches for a save.
    /// </summary>
    public async Task<IReadOnlyList<SaveModuleInfo>> GetVersionMismatchesForSaveAsync(string saveFileName)
    {
        var result = await AnalyzeSaveAsync(saveFileName);
        return result.RequiredModules
            .Where(m => m.IsInstalled && !string.IsNullOrEmpty(m.InstalledVersion) && m.InstalledVersion != m.Version)
            .ToList();
    }

    /// <summary>
    /// External<br/>
    /// Checks if a save is safe to load.
    /// </summary>
    public async Task<bool> IsSaveSafeToLoadAsync(string saveFileName)
    {
        var result = await AnalyzeSaveAsync(saveFileName);
        return result.IsSafeToLoad;
    }

    /// <summary>
    /// External<br/>
    /// Exports save mod requirements.
    /// </summary>
    public async Task<SaveModRequirements> ExportSaveRequirementsAsync(string saveFileName)
    {
        var result = await AnalyzeSaveAsync(saveFileName);
        
        return new SaveModRequirements
        {
            SaveFileName = saveFileName,
            GameVersion = result.GameVersion,
            ModuleIds = result.RequiredModules.OrderBy(m => m.LoadOrder).Select(m => m.Id).ToList(),
            ModuleVersions = result.RequiredModules.ToDictionary(m => m.Id, m => m.Version)
        };
    }

    /// <summary>
    /// External<br/>
    /// Exports save requirements to JSON string.
    /// </summary>
    public async Task<string> ExportSaveRequirementsJsonAsync(string saveFileName)
    {
        var requirements = await ExportSaveRequirementsAsync(saveFileName);
        return JsonSerializer.Serialize(requirements, SaveAnalysisJsonOptions);
    }

    /// <summary>
    /// External<br/>
    /// Configures mods to match a save's requirements.
    /// </summary>
    public async Task<bool> ConfigureModsForSaveAsync(string saveFileName)
    {
        var result = await AnalyzeSaveAsync(saveFileName);
        
        if (result.MissingModCount > 0)
            return false; // Can't configure if mods are missing

        try
        {
            var loadOrder = new LoadOrder();
            var modules = await GetModulesAsync();
            var moduleDict = modules.ToDictionary(m => m.Id, m => m);

            foreach (var reqModule in result.RequiredModules.OrderBy(m => m.LoadOrder))
            {
                if (!moduleDict.ContainsKey(reqModule.Id))
                    continue;

                var module = moduleDict[reqModule.Id];
                loadOrder[reqModule.Id] = new LoadOrderEntry
                {
                    Id = reqModule.Id,
                    Name = module.Name,
                    IsSelected = true,
                    IsDisabled = false,
                    Index = reqModule.LoadOrder
                };
            }

            await SetGameParameterLoadOrderAsync(loadOrder);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// External<br/>
    /// Gets saves compatible with current mod setup.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetCompatibleSavesAsync()
    {
        var results = await AnalyzeAllSavesAsync();
        return results
            .Where(r => r.IsSafeToLoad)
            .Select(r => r.SaveFileName)
            .ToList();
    }

    private static SaveCompatibilityStatus DetermineCompatibilityStatus(SaveAnalysisResult result)
    {
        var criticalCount = result.Issues.Count(i => i.Severity == SaveIssueSeverity.Critical);
        var errorCount = result.Issues.Count(i => i.Severity == SaveIssueSeverity.Error);
        var warningCount = result.Issues.Count(i => i.Severity == SaveIssueSeverity.Warning);

        if (criticalCount > 0)
            return SaveCompatibilityStatus.Incompatible;
        if (errorCount > 0)
            return SaveCompatibilityStatus.MajorIssues;
        if (warningCount > 0)
            return SaveCompatibilityStatus.MinorIssues;

        return SaveCompatibilityStatus.Compatible;
    }

    private static string GetRecommendedAction(SaveAnalysisResult result)
    {
        return result.Status switch
        {
            SaveCompatibilityStatus.Compatible => "Save is fully compatible. Safe to load.",
            SaveCompatibilityStatus.MinorIssues => $"Save has {result.Issues.Count} minor issues. Loading should work but may have problems.",
            SaveCompatibilityStatus.MajorIssues => $"Save is missing {result.MissingModCount} required mods. Install them before loading.",
            SaveCompatibilityStatus.Incompatible => "Save cannot be loaded with current setup. Critical issues detected.",
            _ => "Unable to determine compatibility."
        };
    }
}
