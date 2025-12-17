// <copyright file="CharacterEditor.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Services;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.Validation;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for editing character/hero data.
/// </summary>
public sealed class CharacterEditor
{
    private readonly ILogger<CharacterEditor>? _logger;
    private readonly IValidationService _validation;

    public CharacterEditor(IValidationService? validation = null, ILogger<CharacterEditor>? logger = null)
    {
        _validation = validation ?? new ValidationService();
        _logger = logger;
    }

    #region Attributes

    public void SetAttribute(HeroData hero, AttributeType attribute, int value)
    {
        if (value < 0)
            throw new EditorException($"Attribute {attribute} cannot be negative");

        _logger?.LogDebug("Setting {Hero} {Attribute} to {Value}", hero.Name, attribute, value);
        hero.Attributes[attribute] = value;
    }

    public void AddAttributePoints(HeroData hero, AttributeType attribute, int points)
    {
        var current = hero.Attributes[attribute];
        SetAttribute(hero, attribute, current + points);
    }

    public void SetAllAttributes(HeroData hero, int value)
    {
        foreach (AttributeType attr in Enum.GetValues<AttributeType>())
        {
            SetAttribute(hero, attr, value);
        }
    }

    public int GetExpectedAttributePoints(int level)
    {
        // Base 6 (1 per attribute) + 1 per level
        return 6 + level;
    }

    #endregion

    #region Skills

    public void SetSkill(HeroData hero, SkillType skill, int value)
    {
        if (value < 0)
            throw new EditorException($"Skill {skill} cannot be negative");
        if (value > 300)
            throw new EditorException($"Skill {skill} cannot exceed 300");

        _logger?.LogDebug("Setting {Hero} {Skill} to {Value}", hero.Name, skill, value);
        hero.Skills[skill] = value;
    }

    public void AddSkillXP(HeroData hero, SkillType skill, int xp)
    {
        var current = hero.Skills[skill];
        var newValue = Math.Min(300, current + xp / 100); // Simplified XP to level conversion
        SetSkill(hero, skill, newValue);
    }

    public void SetAllSkills(HeroData hero, int value)
    {
        value = Math.Clamp(value, 0, 300);
        foreach (SkillType skill in Enum.GetValues<SkillType>())
        {
            SetSkill(hero, skill, value);
        }
    }

    public void MaximizeSkills(HeroData hero)
    {
        SetAllSkills(hero, 300);
    }

    #endregion

    #region Naval Skills (War Sails)

    public void SetNavalSkill(HeroData hero, NavalSkillType skill, int value)
    {
        if (value < 0)
            throw new EditorException($"Naval skill {skill} cannot be negative");
        if (value > 300)
            throw new EditorException($"Naval skill {skill} cannot exceed 300");

        hero.NavalSkills ??= new NavalSkillSet();
        _logger?.LogDebug("Setting {Hero} Naval {Skill} to {Value}", hero.Name, skill, value);

        switch (skill)
        {
            case NavalSkillType.Navigation:
                hero.NavalSkills.Navigation = value;
                break;
            case NavalSkillType.NavalTactics:
                hero.NavalSkills.NavalTactics = value;
                break;
            case NavalSkillType.NavalStewardship:
                hero.NavalSkills.NavalStewardship = value;
                break;
        }
    }

    public void MaximizeNavalSkills(HeroData hero)
    {
        hero.NavalSkills = new NavalSkillSet
        {
            Navigation = 300,
            NavalTactics = 300,
            NavalStewardship = 300
        };
    }

    #endregion

    #region Perks

    public void UnlockPerk(HeroData hero, string perkId)
    {
        if (hero.UnlockedPerks.Contains(perkId))
            return;

        _logger?.LogDebug("Unlocking perk {Perk} for {Hero}", perkId, hero.Name);
        hero.UnlockedPerks.Add(perkId);
    }

    public void LockPerk(HeroData hero, string perkId)
    {
        _logger?.LogDebug("Locking perk {Perk} for {Hero}", perkId, hero.Name);
        hero.UnlockedPerks.Remove(perkId);
    }

    public void UnlockAllPerks(HeroData hero, SkillType skill)
    {
        var perks = PerkDatabase.GetPerksForSkill(skill);
        foreach (var perk in perks)
        {
            UnlockPerk(hero, perk);
        }
    }

    public void LockAllPerks(HeroData hero)
    {
        hero.UnlockedPerks.Clear();
    }

    #endregion

    #region Level & Experience

    public void SetLevel(HeroData hero, int level)
    {
        if (level < 1)
            throw new EditorException("Level cannot be less than 1");
        if (level > 62)
            _logger?.LogWarning("Setting level above normal max (62): {Level}", level);

        _logger?.LogDebug("Setting {Hero} level to {Level}", hero.Name, level);
        hero.Level = level;
        hero.Experience = CalculateXPForLevel(level);
    }

    public void AddLevels(HeroData hero, int levels)
    {
        SetLevel(hero, hero.Level + levels);
    }

    public void SetExperience(HeroData hero, int xp)
    {
        hero.Experience = Math.Max(0, xp);
    }

    private static int CalculateXPForLevel(int level)
    {
        // Simplified XP curve
        return level * level * 1000;
    }

    #endregion

    #region Gold & Resources

    public void SetGold(HeroData hero, int gold)
    {
        if (gold < 0)
            throw new EditorException("Gold cannot be negative");

        _logger?.LogDebug("Setting {Hero} gold to {Gold}", hero.Name, gold);
        hero.Gold = gold;
    }

    public void AddGold(HeroData hero, int amount)
    {
        SetGold(hero, hero.Gold + amount);
    }

    #endregion

    #region Health & State

    public void SetHealth(HeroData hero, float health)
    {
        hero.Health = Math.Clamp(health, 0, hero.MaxHealth);
    }

    public void FullHeal(HeroData hero)
    {
        hero.Health = hero.MaxHealth;
        hero.IsWounded = false;
    }

    public void SetState(HeroData hero, HeroState state)
    {
        _logger?.LogDebug("Setting {Hero} state to {State}", hero.Name, state);
        hero.State = state;
    }

    public void Resurrect(HeroData hero)
    {
        if (hero.State == HeroState.Dead)
        {
            hero.State = HeroState.Active;
            FullHeal(hero);
            _logger?.LogInformation("Resurrected hero: {Hero}", hero.Name);
        }
    }

    #endregion

    #region Age & Appearance

    public void SetAge(HeroData hero, int age)
    {
        if (age < 18)
            _logger?.LogWarning("Setting age below 18: {Age}", age);
        if (age > 100)
            _logger?.LogWarning("Setting age above 100: {Age}", age);

        hero.Age = age;
    }

    public void ImportAppearance(HeroData hero, string faceCode)
    {
        hero.Appearance = AppearanceData.FromFaceCode(faceCode);
    }

    public string ExportAppearance(HeroData hero)
    {
        return hero.Appearance?.ToFaceCode() ?? string.Empty;
    }

    #endregion

    #region Templates

    public CharacterTemplate ExportTemplate(HeroData hero)
    {
        return new CharacterTemplate
        {
            Name = hero.Name,
            Attributes = hero.Attributes.Clone(),
            Skills = hero.Skills.Clone(),
            NavalSkills = hero.NavalSkills?.Clone(),
            Perks = hero.UnlockedPerks.ToList(),
            Appearance = hero.Appearance?.Clone(),
            ExportedAt = DateTime.UtcNow,
            Version = "2.0"
        };
    }

    public void ImportTemplate(HeroData hero, CharacterTemplate template, TemplateImportOptions options)
    {
        _logger?.LogInformation("Importing template {Template} to {Hero}", template.Name, hero.Name);

        if (options.ImportAttributes)
        {
            hero.Attributes = template.Attributes.Clone();
        }

        if (options.ImportSkills)
        {
            hero.Skills = template.Skills.Clone();
        }

        if (options.ImportNavalSkills && template.NavalSkills != null)
        {
            hero.NavalSkills = template.NavalSkills.Clone();
        }

        if (options.ImportPerks)
        {
            hero.UnlockedPerks = new HashSet<string>(template.Perks);
        }

        if (options.ImportAppearance && template.Appearance != null)
        {
            hero.Appearance = template.Appearance.Clone();
        }
    }

    #endregion

    #region Validation

    public ValidationReport Validate(HeroData hero)
    {
        return _validation.ValidateHero(hero);
    }

    #endregion
}

/// <summary>
/// Character template for import/export.
/// </summary>
public class CharacterTemplate
{
    public string Name { get; set; } = string.Empty;
    public HeroAttributes Attributes { get; set; } = new();
    public SkillSet Skills { get; set; } = new();
    public NavalSkillSet? NavalSkills { get; set; }
    public List<string> Perks { get; set; } = new();
    public AppearanceData? Appearance { get; set; }
    public DateTime ExportedAt { get; set; }
    public string Version { get; set; } = "2.0";
}

/// <summary>
/// Options for template import.
/// </summary>
public class TemplateImportOptions
{
    public bool ImportAttributes { get; set; } = true;
    public bool ImportSkills { get; set; } = true;
    public bool ImportNavalSkills { get; set; } = true;
    public bool ImportPerks { get; set; } = true;
    public bool ImportAppearance { get; set; } = false;
}

/// <summary>
/// Naval skill types for War Sails.
/// </summary>
public enum NavalSkillType
{
    Navigation,
    NavalTactics,
    NavalStewardship
}

/// <summary>
/// General editor exception.
/// </summary>
public class EditorException : Exception
{
    public EditorException(string message) : base(message) { }
}

/// <summary>
/// Perk database for lookups.
/// </summary>
public static partial class PerkDatabase
{
    private static readonly Dictionary<SkillType, List<string>> SkillPerks = new()
    {
        [SkillType.OneHanded] = new() { "swift_strike", "cavalry", "basher", "deflect", "to_be_blunt" },
        [SkillType.TwoHanded] = new() { "strong_grip", "on_the_edge", "head_basher", "show_of_strength" },
        [SkillType.Polearm] = new() { "cavalry_tactics", "phalanx", "unstoppable_force" },
        [SkillType.Bow] = new() { "quick_draw", "mounted_archery", "deadly_aim" },
        [SkillType.Crossbow] = new() { "piercing_bolts", "steady_crossbow", "sniper" },
        [SkillType.Throwing] = new() { "quick_throw", "shield_breaker", "head_hunter" },
        [SkillType.Riding] = new() { "full_speed", "mounted_combat", "horse_master" },
        [SkillType.Athletics] = new() { "morning_exercise", "well_built", "powerful" },
        [SkillType.Crafting] = new() { "practical_smith", "artisan_smith", "legendary_smith" },
        [SkillType.Scouting] = new() { "keen_sight", "tracker", "ranger" },
        [SkillType.Tactics] = new() { "tight_formations", "ambush", "decisive_battle" },
        [SkillType.Roguery] = new() { "no_mercy", "scarface", "slave_trader" },
        [SkillType.Charm] = new() { "virile", "self_promoter", "oratory" },
        [SkillType.Leadership] = new() { "combat_tips", "raise_the_meek", "veteran_respect" },
        [SkillType.Trade] = new() { "appraiser", "wholesale_trader", "caravan_master" },
        [SkillType.Steward] = new() { "frugal", "assessor", "master_of_planning" },
        [SkillType.Medicine] = new() { "preventive_medicine", "physician", "walking_pharmacy" },
        [SkillType.Engineering] = new() { "scaffolds", "carpenters", "siege_engineer" }
    };

    public static IEnumerable<string> GetPerksForSkill(SkillType skill)
    {
        return SkillPerks.TryGetValue(skill, out var perks) ? perks : Enumerable.Empty<string>();
    }
}
