using System;
using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Predefined module categories.
/// </summary>
public enum ModuleCategoryType
{
    Uncategorized,
    Gameplay,
    Combat,
    Graphics,
    UI,
    Audio,
    QualityOfLife,
    Overhaul,
    Troops,
    Items,
    Maps,
    Quests,
    Economy,
    Diplomacy,
    Cheats,
    Utility,
    Framework,
    Compatibility
}

/// <summary>
/// Custom category definition.
/// </summary>
public class ModuleCategory
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Color for UI display (hex format).
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Icon name or path.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Whether this is a predefined category.
    /// </summary>
    public bool IsPredefined { get; set; }

    /// <summary>
    /// Sort order for display.
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Module tag for custom organization.
/// </summary>
public class ModuleTag
{
    /// <summary>
    /// Tag name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Color for UI display.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// When the tag was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Category assignment for a module.
/// </summary>
public class ModuleCategoryAssignment
{
    /// <summary>
    /// Module ID.
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>
    /// Assigned category ID.
    /// </summary>
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>
    /// Assigned tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Custom notes for the module.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User rating (1-5).
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// Whether this is a favorite.
    /// </summary>
    public bool IsFavorite { get; set; }
}

/// <summary>
/// Collection of all category data.
/// </summary>
public class ModuleCategoryData
{
    /// <summary>
    /// All defined categories.
    /// </summary>
    public List<ModuleCategory> Categories { get; set; } = new();

    /// <summary>
    /// All defined tags.
    /// </summary>
    public List<ModuleTag> Tags { get; set; } = new();

    /// <summary>
    /// Module assignments.
    /// </summary>
    public List<ModuleCategoryAssignment> Assignments { get; set; } = new();

    /// <summary>
    /// Version for migration.
    /// </summary>
    public int Version { get; set; } = 1;
}

/// <summary>
/// Summary of categories.
/// </summary>
public class CategorySummary
{
    /// <summary>
    /// Category ID.
    /// </summary>
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>
    /// Category name.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Number of modules in this category.
    /// </summary>
    public int ModuleCount { get; set; }

    /// <summary>
    /// Number of enabled modules.
    /// </summary>
    public int EnabledCount { get; set; }
}

/// <summary>
/// Filter options for module queries.
/// </summary>
public class ModuleFilterOptions
{
    /// <summary>
    /// Filter by category.
    /// </summary>
    public string? CategoryId { get; set; }

    /// <summary>
    /// Filter by tags (any match).
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Only favorites.
    /// </summary>
    public bool? FavoritesOnly { get; set; }

    /// <summary>
    /// Minimum rating.
    /// </summary>
    public int? MinRating { get; set; }

    /// <summary>
    /// Only enabled modules.
    /// </summary>
    public bool? EnabledOnly { get; set; }

    /// <summary>
    /// Search text.
    /// </summary>
    public string? SearchText { get; set; }
}
