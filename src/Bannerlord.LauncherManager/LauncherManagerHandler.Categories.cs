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
    private const string CategoryDataFile = "module_categories.json";
    private ModuleCategoryData? _categoryData;

    private static readonly JsonSerializerOptions CategoryJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// External<br/>
    /// Gets all categories.
    /// </summary>
    public async Task<IReadOnlyList<ModuleCategory>> GetCategoriesAsync()
    {
        await EnsureCategoryDataLoadedAsync();
        return _categoryData!.Categories;
    }

    /// <summary>
    /// External<br/>
    /// Creates a new category.
    /// </summary>
    public async Task<ModuleCategory> CreateCategoryAsync(string name, string? description = null, string? color = null)
    {
        await EnsureCategoryDataLoadedAsync();

        var category = new ModuleCategory
        {
            Name = name,
            Description = description,
            Color = color,
            IsPredefined = false,
            SortOrder = _categoryData!.Categories.Count
        };

        _categoryData.Categories.Add(category);
        await SaveCategoryDataAsync();

        return category;
    }

    /// <summary>
    /// External<br/>
    /// Deletes a category.
    /// </summary>
    public async Task<bool> DeleteCategoryAsync(string categoryId)
    {
        await EnsureCategoryDataLoadedAsync();

        var category = _categoryData!.Categories.FirstOrDefault(c => c.Id == categoryId);
        if (category == null || category.IsPredefined)
            return false;

        _categoryData.Categories.Remove(category);

        // Clear assignments for this category
        foreach (var assignment in _categoryData.Assignments.Where(a => a.CategoryId == categoryId))
        {
            assignment.CategoryId = string.Empty;
        }

        await SaveCategoryDataAsync();
        return true;
    }

    /// <summary>
    /// External<br/>
    /// Sets a module's category.
    /// </summary>
    public async Task SetModuleCategoryAsync(string moduleId, string categoryId)
    {
        await EnsureCategoryDataLoadedAsync();

        var assignment = GetOrCreateAssignment(moduleId);
        assignment.CategoryId = categoryId;

        await SaveCategoryDataAsync();
    }

    /// <summary>
    /// External<br/>
    /// Gets a module's category.
    /// </summary>
    public async Task<ModuleCategory?> GetModuleCategoryAsync(string moduleId)
    {
        await EnsureCategoryDataLoadedAsync();

        var assignment = _categoryData!.Assignments.FirstOrDefault(a => a.ModuleId == moduleId);
        if (assignment == null || string.IsNullOrEmpty(assignment.CategoryId))
            return null;

        return _categoryData.Categories.FirstOrDefault(c => c.Id == assignment.CategoryId);
    }

    /// <summary>
    /// External<br/>
    /// Gets all modules in a category.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetModulesByCategoryAsync(string categoryId)
    {
        await EnsureCategoryDataLoadedAsync();

        return _categoryData!.Assignments
            .Where(a => a.CategoryId == categoryId)
            .Select(a => a.ModuleId)
            .ToList();
    }

    /// <summary>
    /// External<br/>
    /// Gets all tags.
    /// </summary>
    public async Task<IReadOnlyList<ModuleTag>> GetTagsAsync()
    {
        await EnsureCategoryDataLoadedAsync();
        return _categoryData!.Tags;
    }

    /// <summary>
    /// External<br/>
    /// Creates a new tag.
    /// </summary>
    public async Task<ModuleTag> CreateTagAsync(string name, string? color = null)
    {
        await EnsureCategoryDataLoadedAsync();

        var existing = _categoryData!.Tags.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            return existing;

        var tag = new ModuleTag
        {
            Name = name,
            Color = color
        };

        _categoryData.Tags.Add(tag);
        await SaveCategoryDataAsync();

        return tag;
    }

    /// <summary>
    /// External<br/>
    /// Deletes a tag.
    /// </summary>
    public async Task<bool> DeleteTagAsync(string tagName)
    {
        await EnsureCategoryDataLoadedAsync();

        var tag = _categoryData!.Tags.FirstOrDefault(t => t.Name == tagName);
        if (tag == null)
            return false;

        _categoryData.Tags.Remove(tag);

        // Remove tag from all assignments
        foreach (var assignment in _categoryData.Assignments)
        {
            assignment.Tags.Remove(tagName);
        }

        await SaveCategoryDataAsync();
        return true;
    }

    /// <summary>
    /// External<br/>
    /// Adds a tag to a module.
    /// </summary>
    public async Task AddModuleTagAsync(string moduleId, string tagName)
    {
        await EnsureCategoryDataLoadedAsync();

        // Ensure tag exists
        if (!_categoryData!.Tags.Any(t => t.Name == tagName))
        {
            await CreateTagAsync(tagName);
        }

        var assignment = GetOrCreateAssignment(moduleId);
        if (!assignment.Tags.Contains(tagName))
        {
            assignment.Tags.Add(tagName);
            await SaveCategoryDataAsync();
        }
    }

    /// <summary>
    /// External<br/>
    /// Removes a tag from a module.
    /// </summary>
    public async Task RemoveModuleTagAsync(string moduleId, string tagName)
    {
        await EnsureCategoryDataLoadedAsync();

        var assignment = _categoryData!.Assignments.FirstOrDefault(a => a.ModuleId == moduleId);
        if (assignment != null && assignment.Tags.Remove(tagName))
        {
            await SaveCategoryDataAsync();
        }
    }

    /// <summary>
    /// External<br/>
    /// Gets all tags for a module.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetModuleTagsAsync(string moduleId)
    {
        await EnsureCategoryDataLoadedAsync();

        var assignment = _categoryData!.Assignments.FirstOrDefault(a => a.ModuleId == moduleId);
        return assignment?.Tags ?? new List<string>();
    }

    /// <summary>
    /// External<br/>
    /// Gets all modules with a specific tag.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetModulesByTagAsync(string tagName)
    {
        await EnsureCategoryDataLoadedAsync();

        return _categoryData!.Assignments
            .Where(a => a.Tags.Contains(tagName))
            .Select(a => a.ModuleId)
            .ToList();
    }

    /// <summary>
    /// External<br/>
    /// Sets a module as favorite.
    /// </summary>
    public async Task SetModuleFavoriteAsync(string moduleId, bool isFavorite)
    {
        await EnsureCategoryDataLoadedAsync();

        var assignment = GetOrCreateAssignment(moduleId);
        assignment.IsFavorite = isFavorite;

        await SaveCategoryDataAsync();
    }

    /// <summary>
    /// External<br/>
    /// Gets all favorite modules.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetFavoriteModulesAsync()
    {
        await EnsureCategoryDataLoadedAsync();

        return _categoryData!.Assignments
            .Where(a => a.IsFavorite)
            .Select(a => a.ModuleId)
            .ToList();
    }

    /// <summary>
    /// External<br/>
    /// Sets a module's rating.
    /// </summary>
    public async Task SetModuleRatingAsync(string moduleId, int rating)
    {
        await EnsureCategoryDataLoadedAsync();

        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var assignment = GetOrCreateAssignment(moduleId);
        assignment.Rating = rating;

        await SaveCategoryDataAsync();
    }

    /// <summary>
    /// External<br/>
    /// Sets notes for a module.
    /// </summary>
    public async Task SetModuleNotesAsync(string moduleId, string? notes)
    {
        await EnsureCategoryDataLoadedAsync();

        var assignment = GetOrCreateAssignment(moduleId);
        assignment.Notes = notes;

        await SaveCategoryDataAsync();
    }

    /// <summary>
    /// External<br/>
    /// Gets the full assignment for a module.
    /// </summary>
    public async Task<ModuleCategoryAssignment?> GetModuleAssignmentAsync(string moduleId)
    {
        await EnsureCategoryDataLoadedAsync();
        return _categoryData!.Assignments.FirstOrDefault(a => a.ModuleId == moduleId);
    }

    /// <summary>
    /// External<br/>
    /// Gets category summary statistics.
    /// </summary>
    public async Task<IReadOnlyList<CategorySummary>> GetCategorySummaryAsync()
    {
        await EnsureCategoryDataLoadedAsync();

        var viewModels = await GetModuleViewModelsAsync();
        var enabledIds = viewModels?.Where(vm => vm.IsSelected).Select(vm => vm.ModuleInfoExtended.Id).ToHashSet() 
                         ?? new HashSet<string>();

        var summaries = new List<CategorySummary>();

        foreach (var category in _categoryData!.Categories)
        {
            var moduleIds = _categoryData.Assignments
                .Where(a => a.CategoryId == category.Id)
                .Select(a => a.ModuleId)
                .ToList();

            summaries.Add(new CategorySummary
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                ModuleCount = moduleIds.Count,
                EnabledCount = moduleIds.Count(id => enabledIds.Contains(id))
            });
        }

        return summaries;
    }

    /// <summary>
    /// External<br/>
    /// Filters modules by options.
    /// </summary>
    public async Task<IReadOnlyList<string>> FilterModulesAsync(ModuleFilterOptions options)
    {
        await EnsureCategoryDataLoadedAsync();

        var modules = await GetModulesAsync();
        var viewModels = await GetModuleViewModelsAsync();
        var enabledIds = viewModels?.Where(vm => vm.IsSelected).Select(vm => vm.ModuleInfoExtended.Id).ToHashSet()
                         ?? new HashSet<string>();

        var result = modules.Select(m => m.Id).ToList();

        // Filter by category
        if (!string.IsNullOrEmpty(options.CategoryId))
        {
            var categoryModules = await GetModulesByCategoryAsync(options.CategoryId);
            result = result.Where(id => categoryModules.Contains(id)).ToList();
        }

        // Filter by tags
        if (options.Tags?.Count > 0)
        {
            var assignments = _categoryData!.Assignments;
            result = result.Where(id =>
            {
                var assignment = assignments.FirstOrDefault(a => a.ModuleId == id);
                return assignment != null && options.Tags.Any(t => assignment.Tags.Contains(t));
            }).ToList();
        }

        // Filter favorites
        if (options.FavoritesOnly == true)
        {
            var favorites = await GetFavoriteModulesAsync();
            result = result.Where(id => favorites.Contains(id)).ToList();
        }

        // Filter by rating
        if (options.MinRating.HasValue)
        {
            result = result.Where(id =>
            {
                var assignment = _categoryData!.Assignments.FirstOrDefault(a => a.ModuleId == id);
                return assignment?.Rating >= options.MinRating;
            }).ToList();
        }

        // Filter enabled
        if (options.EnabledOnly == true)
        {
            result = result.Where(id => enabledIds.Contains(id)).ToList();
        }

        // Filter by search text
        if (!string.IsNullOrEmpty(options.SearchText))
        {
            var searchLower = options.SearchText.ToLowerInvariant();
            result = result.Where(id =>
            {
                var module = modules.FirstOrDefault(m => m.Id == id);
                return module != null &&
                       (module.Id.ToLowerInvariant().Contains(searchLower) ||
                        module.Name.ToLowerInvariant().Contains(searchLower));
            }).ToList();
        }

        return result;
    }

    private async Task EnsureCategoryDataLoadedAsync()
    {
        if (_categoryData != null)
            return;

        var installPath = await GetInstallPathAsync();
        var dataPath = Path.Combine(installPath, CategoryDataFile);

        if (File.Exists(dataPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(dataPath);
                _categoryData = JsonSerializer.Deserialize<ModuleCategoryData>(json, CategoryJsonOptions);
            }
            catch
            {
                _categoryData = null;
            }
        }

        if (_categoryData == null)
        {
            _categoryData = CreateDefaultCategoryData();
            await SaveCategoryDataAsync();
        }
    }

    private async Task SaveCategoryDataAsync()
    {
        if (_categoryData == null)
            return;

        var installPath = await GetInstallPathAsync();
        var dataPath = Path.Combine(installPath, CategoryDataFile);

        var json = JsonSerializer.Serialize(_categoryData, CategoryJsonOptions);
        await File.WriteAllTextAsync(dataPath, json);
    }

    private ModuleCategoryAssignment GetOrCreateAssignment(string moduleId)
    {
        var assignment = _categoryData!.Assignments.FirstOrDefault(a => a.ModuleId == moduleId);
        if (assignment == null)
        {
            assignment = new ModuleCategoryAssignment { ModuleId = moduleId };
            _categoryData.Assignments.Add(assignment);
        }
        return assignment;
    }

    private static ModuleCategoryData CreateDefaultCategoryData()
    {
        var data = new ModuleCategoryData();

        // Add predefined categories
        var predefinedCategories = new[]
        {
            (ModuleCategoryType.Gameplay, "Gameplay", "#4CAF50"),
            (ModuleCategoryType.Combat, "Combat", "#F44336"),
            (ModuleCategoryType.Graphics, "Graphics", "#2196F3"),
            (ModuleCategoryType.UI, "UI", "#9C27B0"),
            (ModuleCategoryType.Audio, "Audio", "#FF9800"),
            (ModuleCategoryType.QualityOfLife, "Quality of Life", "#00BCD4"),
            (ModuleCategoryType.Overhaul, "Overhaul", "#E91E63"),
            (ModuleCategoryType.Troops, "Troops", "#795548"),
            (ModuleCategoryType.Items, "Items", "#FFC107"),
            (ModuleCategoryType.Framework, "Framework", "#607D8B"),
            (ModuleCategoryType.Utility, "Utility", "#9E9E9E")
        };

        foreach (var (type, name, color) in predefinedCategories)
        {
            data.Categories.Add(new ModuleCategory
            {
                Id = type.ToString(),
                Name = name,
                Color = color,
                IsPredefined = true,
                SortOrder = (int)type
            });
        }

        return data;
    }
}
