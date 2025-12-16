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
    private const string PresetCollectionFile = "preset_collection.json";
    private PresetCollection? _presetCollection;

    private static readonly JsonSerializerOptions PresetJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// External<br/>
    /// Creates a preset from current mod configuration.
    /// </summary>
    public async Task<ModPreset> CreatePresetAsync(string name, string? description = null, PresetCreateOptions? options = null)
    {
        options ??= new PresetCreateOptions();
        await EnsurePresetCollectionLoadedAsync();

        var preset = new ModPreset
        {
            Name = name,
            Description = description,
            GameVersion = await GetGameVersionAsync()
        };

        var viewModels = await GetModuleViewModelsAsync();
        if (viewModels == null)
            return preset;

        var modules = await GetModulesAsync();
        var moduleDict = modules.ToDictionary(m => m.Id, m => m);

        var loadOrder = 0;
        foreach (var vm in viewModels)
        {
            var module = vm.ModuleInfoExtended;

            // Skip native if not included
            if (!options.IncludeNative && module.IsNative())
                continue;

            // Skip disabled if enabled only
            if (options.EnabledOnly && !vm.IsSelected)
                continue;

            var entry = new PresetModuleEntry
            {
                Id = module.Id,
                Name = module.Name,
                Version = options.IncludeVersions ? module.Version.ToString() : string.Empty,
                IsEnabled = vm.IsSelected,
                LoadOrder = loadOrder++,
                IsRequired = !module.IsNative()
            };

            // Add download URLs if available
            if (options.IncludeDownloadUrls && !string.IsNullOrEmpty(module.Url))
            {
                entry.DownloadUrl = module.Url;
                
                if (module.Url.Contains("nexusmods.com", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract NexusMods ID from URL
                    var match = System.Text.RegularExpressions.Regex.Match(module.Url, @"/mods/(\d+)");
                    if (match.Success)
                        entry.NexusModsId = match.Groups[1].Value;
                }
                else if (module.Url.Contains("steamcommunity.com", StringComparison.OrdinalIgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(module.Url, @"id=(\d+)");
                    if (match.Success)
                        entry.SteamWorkshopId = match.Groups[1].Value;
                }
            }

            preset.Modules.Add(entry);
        }

        _presetCollection!.Presets.Add(preset);
        await SavePresetCollectionAsync();

        return preset;
    }

    /// <summary>
    /// External<br/>
    /// Gets all user presets.
    /// </summary>
    public async Task<IReadOnlyList<ModPreset>> GetPresetsAsync()
    {
        await EnsurePresetCollectionLoadedAsync();
        return _presetCollection!.Presets.OrderByDescending(p => p.UpdatedAt).ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets a preset by ID.
    /// </summary>
    public async Task<ModPreset?> GetPresetByIdAsync(string presetId)
    {
        await EnsurePresetCollectionLoadedAsync();
        return _presetCollection!.Presets.FirstOrDefault(p => p.Id == presetId)
            ?? _presetCollection.ImportedPresets.FirstOrDefault(p => p.Id == presetId);
    }

    /// <summary>
    /// External<br/>
    /// Gets a preset by share code.
    /// </summary>
    public async Task<ModPreset?> GetPresetByShareCodeAsync(string shareCode)
    {
        var id = shareCode.Replace("BL-", "", StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
        return await GetPresetByIdAsync(id);
    }

    /// <summary>
    /// External<br/>
    /// Updates an existing preset.
    /// </summary>
    public async Task<bool> UpdatePresetAsync(string presetId, string? name = null, string? description = null, List<string>? tags = null)
    {
        await EnsurePresetCollectionLoadedAsync();
        var preset = _presetCollection!.Presets.FirstOrDefault(p => p.Id == presetId);
        
        if (preset == null)
            return false;

        if (name != null)
            preset.Name = name;
        if (description != null)
            preset.Description = description;
        if (tags != null)
            preset.Tags = tags;

        preset.UpdatedAt = DateTime.UtcNow;
        await SavePresetCollectionAsync();
        return true;
    }

    /// <summary>
    /// External<br/>
    /// Refreshes preset with current mod configuration.
    /// </summary>
    public async Task<ModPreset?> RefreshPresetAsync(string presetId, PresetCreateOptions? options = null)
    {
        await EnsurePresetCollectionLoadedAsync();
        var existing = _presetCollection!.Presets.FirstOrDefault(p => p.Id == presetId);
        
        if (existing == null)
            return null;

        // Create new preset with same metadata
        var newPreset = await CreatePresetAsync(existing.Name, existing.Description, options);
        newPreset.Id = existing.Id;
        newPreset.Tags = existing.Tags;
        newPreset.Visibility = existing.Visibility;
        newPreset.CreatedAt = existing.CreatedAt;

        // Replace in collection
        _presetCollection.Presets.Remove(existing);
        _presetCollection.Presets.Add(newPreset);
        await SavePresetCollectionAsync();

        return newPreset;
    }

    /// <summary>
    /// External<br/>
    /// Deletes a preset.
    /// </summary>
    public async Task<bool> DeletePresetAsync(string presetId)
    {
        await EnsurePresetCollectionLoadedAsync();
        var preset = _presetCollection!.Presets.FirstOrDefault(p => p.Id == presetId);
        
        if (preset == null)
        {
            // Try imported presets
            preset = _presetCollection.ImportedPresets.FirstOrDefault(p => p.Id == presetId);
            if (preset != null)
            {
                _presetCollection.ImportedPresets.Remove(preset);
                await SavePresetCollectionAsync();
                return true;
            }
            return false;
        }

        _presetCollection.Presets.Remove(preset);
        _presetCollection.Favorites.Remove(presetId);
        await SavePresetCollectionAsync();
        return true;
    }

    /// <summary>
    /// External<br/>
    /// Applies a preset to current configuration.
    /// </summary>
    public async Task<PresetApplyResult> ApplyPresetAsync(string presetId, PresetApplyOptions? options = null)
    {
        options ??= new PresetApplyOptions();
        var result = new PresetApplyResult();

        var preset = await GetPresetByIdAsync(presetId);
        if (preset == null)
        {
            result.Status = PresetApplyStatus.Failed;
            result.ErrorMessage = "Preset not found.";
            return result;
        }

        try
        {
            var modules = await GetModulesAsync();
            var moduleDict = modules.ToDictionary(m => m.Id, m => m);

            // Check for missing modules
            foreach (var entry in preset.Modules)
            {
                if (!moduleDict.ContainsKey(entry.Id))
                {
                    result.Missing.Add(entry);
                }
                else if (!options.IgnoreVersions && !string.IsNullOrEmpty(entry.Version))
                {
                    var installed = moduleDict[entry.Id];
                    if (installed.Version.ToString() != entry.Version)
                    {
                        result.VersionMismatches.Add(entry);
                    }
                }
            }

            // Check if we should proceed
            if (result.Missing.Any(m => m.IsRequired) && !options.SkipMissing)
            {
                result.Status = PresetApplyStatus.MissingMods;
                result.ErrorMessage = $"Missing {result.Missing.Count} required mods.";
                return result;
            }

            // Build load order
            var loadOrder = new LoadOrder();
            foreach (var entry in preset.Modules.OrderBy(m => m.LoadOrder))
            {
                if (!moduleDict.TryGetValue(entry.Id, out var module))
                    continue;

                loadOrder[entry.Id] = new LoadOrderEntry
                {
                    Id = entry.Id,
                    Name = module.Name,
                    IsSelected = entry.IsEnabled,
                    IsDisabled = false,
                    Index = entry.LoadOrder
                };

                result.Applied.Add(entry.Id);
            }

            // Disable others if requested
            if (options.DisableOthers)
            {
                var presetIds = preset.Modules.Select(m => m.Id).ToHashSet();
                var index = preset.Modules.Count;
                foreach (var module in modules.Where(m => !presetIds.Contains(m.Id)))
                {
                    if (!loadOrder.ContainsKey(module.Id))
                    {
                        loadOrder[module.Id] = new LoadOrderEntry
                        {
                            Id = module.Id,
                            Name = module.Name,
                            IsSelected = false,
                            IsDisabled = false,
                            Index = index++
                        };
                    }
                }
            }

            await SetGameParameterLoadOrderAsync(loadOrder);

            // Update last applied
            _presetCollection!.LastAppliedId = presetId;
            await SavePresetCollectionAsync();

            result.Status = result.Missing.Any() || result.VersionMismatches.Any()
                ? PresetApplyStatus.PartialSuccess
                : PresetApplyStatus.Success;
        }
        catch (Exception ex)
        {
            result.Status = PresetApplyStatus.Failed;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// External<br/>
    /// Exports preset to JSON string.
    /// </summary>
    public async Task<string> ExportPresetAsync(string presetId)
    {
        var preset = await GetPresetByIdAsync(presetId);
        if (preset == null)
            return string.Empty;

        return JsonSerializer.Serialize(preset, PresetJsonOptions);
    }

    /// <summary>
    /// External<br/>
    /// Exports preset to file.
    /// </summary>
    public async Task<bool> ExportPresetToFileAsync(string presetId, string filePath)
    {
        var json = await ExportPresetAsync(presetId);
        if (string.IsNullOrEmpty(json))
            return false;

        await File.WriteAllTextAsync(filePath, json);
        return true;
    }

    /// <summary>
    /// External<br/>
    /// Imports preset from JSON string.
    /// </summary>
    public async Task<PresetImportResult> ImportPresetAsync(string json)
    {
        var result = new PresetImportResult();

        try
        {
            var preset = JsonSerializer.Deserialize<ModPreset>(json, PresetJsonOptions);
            if (preset == null)
            {
                result.ErrorMessage = "Invalid preset format.";
                return result;
            }

            await EnsurePresetCollectionLoadedAsync();

            // Check for duplicates
            var existing = _presetCollection!.ImportedPresets.FirstOrDefault(p => p.Id == preset.Id);
            if (existing != null)
            {
                result.WasDuplicate = true;
                result.Preset = existing;
                result.Success = true;
                return result;
            }

            _presetCollection.ImportedPresets.Add(preset);
            await SavePresetCollectionAsync();

            result.Success = true;
            result.Preset = preset;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Import failed: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// External<br/>
    /// Imports preset from file.
    /// </summary>
    public async Task<PresetImportResult> ImportPresetFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new PresetImportResult { ErrorMessage = "File not found." };
        }

        var json = await File.ReadAllTextAsync(filePath);
        return await ImportPresetAsync(json);
    }

    /// <summary>
    /// External<br/>
    /// Gets imported presets.
    /// </summary>
    public async Task<IReadOnlyList<ModPreset>> GetImportedPresetsAsync()
    {
        await EnsurePresetCollectionLoadedAsync();
        return _presetCollection!.ImportedPresets.OrderByDescending(p => p.UpdatedAt).ToList();
    }

    /// <summary>
    /// External<br/>
    /// Toggles favorite status for a preset.
    /// </summary>
    public async Task<bool> ToggleFavoriteAsync(string presetId)
    {
        await EnsurePresetCollectionLoadedAsync();
        
        if (_presetCollection!.Favorites.Contains(presetId))
        {
            _presetCollection.Favorites.Remove(presetId);
        }
        else
        {
            _presetCollection.Favorites.Add(presetId);
        }

        await SavePresetCollectionAsync();
        return _presetCollection.Favorites.Contains(presetId);
    }

    /// <summary>
    /// External<br/>
    /// Gets favorite presets.
    /// </summary>
    public async Task<IReadOnlyList<ModPreset>> GetFavoritePresetsAsync()
    {
        await EnsurePresetCollectionLoadedAsync();
        var allPresets = _presetCollection!.Presets.Concat(_presetCollection.ImportedPresets);
        return allPresets.Where(p => _presetCollection.Favorites.Contains(p.Id)).ToList();
    }

    /// <summary>
    /// External<br/>
    /// Compares two presets.
    /// </summary>
    public async Task<PresetComparisonResult> ComparePresetsAsync(string presetId1, string presetId2)
    {
        var result = new PresetComparisonResult();

        var preset1 = await GetPresetByIdAsync(presetId1);
        var preset2 = await GetPresetByIdAsync(presetId2);

        if (preset1 == null || preset2 == null)
            return result;

        var ids1 = preset1.Modules.Select(m => m.Id).ToHashSet();
        var ids2 = preset2.Modules.Select(m => m.Id).ToHashSet();

        result.OnlyInFirst = ids1.Except(ids2).ToList();
        result.OnlyInSecond = ids2.Except(ids1).ToList();
        result.InBoth = ids1.Intersect(ids2).ToList();

        // Check order differences
        var order1 = preset1.Modules.ToDictionary(m => m.Id, m => m.LoadOrder);
        var order2 = preset2.Modules.ToDictionary(m => m.Id, m => m.LoadOrder);

        foreach (var id in result.InBoth)
        {
            if (order1[id] != order2[id])
            {
                result.DifferentOrder.Add(id);
            }
        }

        // Calculate similarity
        var totalUnique = ids1.Union(ids2).Count();
        result.SimilarityPercent = totalUnique > 0 
            ? (int)((double)result.InBoth.Count / totalUnique * 100)
            : 100;

        return result;
    }

    /// <summary>
    /// External<br/>
    /// Gets the last applied preset.
    /// </summary>
    public async Task<ModPreset?> GetLastAppliedPresetAsync()
    {
        await EnsurePresetCollectionLoadedAsync();
        if (string.IsNullOrEmpty(_presetCollection!.LastAppliedId))
            return null;

        return await GetPresetByIdAsync(_presetCollection.LastAppliedId);
    }

    /// <summary>
    /// External<br/>
    /// Generates share code for a preset.
    /// </summary>
    public async Task<string> GetShareCodeAsync(string presetId)
    {
        var preset = await GetPresetByIdAsync(presetId);
        return preset?.ShareCode ?? string.Empty;
    }

    private async Task EnsurePresetCollectionLoadedAsync()
    {
        if (_presetCollection != null)
            return;

        var installPath = await GetInstallPathAsync();
        var dataPath = Path.Combine(installPath, PresetCollectionFile);

        if (File.Exists(dataPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(dataPath);
                _presetCollection = JsonSerializer.Deserialize<PresetCollection>(json, PresetJsonOptions);
            }
            catch
            {
                _presetCollection = null;
            }
        }

        _presetCollection ??= new PresetCollection();
    }

    private async Task SavePresetCollectionAsync()
    {
        if (_presetCollection == null)
            return;

        var installPath = await GetInstallPathAsync();
        var dataPath = Path.Combine(installPath, PresetCollectionFile);

        var json = JsonSerializer.Serialize(_presetCollection, PresetJsonOptions);
        await File.WriteAllTextAsync(dataPath, json);
    }
}
