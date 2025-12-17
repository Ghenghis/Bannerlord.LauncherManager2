// <copyright file="HeroData.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Entities;

using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.WarSails;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a hero (character) in the game.
/// </summary>
public sealed class HeroData : IEditable
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public MBGUID Id { get; set; }

    /// <summary>
    /// Gets or sets the string ID used in game data.
    /// </summary>
    public string HeroId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the hero's gender.
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// Gets or sets the hero's age in years.
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Gets or sets whether this is the main player hero.
    /// </summary>
    public bool IsMainHero { get; set; }

    /// <summary>
    /// Gets or sets whether this hero is alive.
    /// </summary>
    public bool IsAlive { get; set; } = true;

    /// <summary>
    /// Gets or sets the current level.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the current experience points.
    /// </summary>
    public int Experience { get; set; }

    /// <summary>
    /// Gets or sets unspent attribute points.
    /// </summary>
    public int UnspentAttributePoints { get; set; }

    /// <summary>
    /// Gets or sets unspent focus points.
    /// </summary>
    public int UnspentFocusPoints { get; set; }

    /// <summary>
    /// Gets or sets the hero's attributes.
    /// </summary>
    public HeroAttributes Attributes { get; set; } = new();

    /// <summary>
    /// Gets or sets the hero's skills.
    /// </summary>
    public SkillSet Skills { get; set; } = new();

    /// <summary>
    /// Gets or sets the naval skills (War Sails).
    /// </summary>
    public NavalSkillSet? NavalSkills { get; set; }

    /// <summary>
    /// Gets the collection of unlocked perks.
    /// </summary>
    public ISet<string> UnlockedPerks { get; set; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets the battle equipment set.
    /// </summary>
    public EquipmentSet BattleEquipment { get; set; } = new();

    /// <summary>
    /// Gets or sets the civilian equipment set.
    /// </summary>
    public EquipmentSet CivilianEquipment { get; set; } = new();

    /// <summary>
    /// Gets or sets the spare equipment set.
    /// </summary>
    public EquipmentSet SpareEquipment { get; set; } = new();

    /// <summary>
    /// Gets or sets the hero's appearance data.
    /// </summary>
    public AppearanceData Appearance { get; set; } = new();

    /// <summary>
    /// Gets or sets the hero's clan.
    /// </summary>
    [JsonIgnore]
    public ClanData? Clan { get; set; }

    /// <summary>
    /// Gets or sets the clan ID reference.
    /// </summary>
    public MBGUID? ClanId { get; set; }

    /// <summary>
    /// Gets or sets the hero's current party.
    /// </summary>
    [JsonIgnore]
    public PartyData? Party { get; set; }

    /// <summary>
    /// Gets or sets the party ID reference.
    /// </summary>
    public MBGUID? PartyId { get; set; }

    /// <summary>
    /// Gets or sets the hero's fleet (War Sails).
    /// </summary>
    [JsonIgnore]
    public FleetData? Fleet { get; set; }

    /// <summary>
    /// Gets or sets the fleet ID reference (War Sails).
    /// </summary>
    public MBGUID? FleetId { get; set; }

    /// <summary>
    /// Gets or sets the hero's current state.
    /// </summary>
    public HeroState State { get; set; }

    /// <summary>
    /// Gets or sets the hero's personal gold.
    /// </summary>
    public int Gold { get; set; }

    /// <summary>
    /// Gets or sets the hero's health (0.0 - 1.0).
    /// </summary>
    public float Health { get; set; } = 1.0f;

    /// <summary>
    /// Gets the maximum health value.
    /// </summary>
    public float MaxHealth => 1.0f;

    /// <summary>
    /// Gets or sets whether the hero is wounded.
    /// </summary>
    public bool IsWounded { get; set; }

    /// <summary>
    /// Gets or sets hero traits.
    /// </summary>
    public IList<TraitData> Traits { get; set; } = new List<TraitData>();

    /// <inheritdoc />
    public bool IsDirty { get; set; }

    /// <inheritdoc />
    public void MarkDirty() => IsDirty = true;
}

/// <summary>
/// Hero attribute values.
/// </summary>
public sealed class HeroAttributes : ICloneable
{
    /// <summary>
    /// Vigor - affects melee damage and hit points.
    /// </summary>
    public int Vigor { get; set; }

    /// <summary>
    /// Control - affects ranged accuracy and riding.
    /// </summary>
    public int Control { get; set; }

    /// <summary>
    /// Endurance - affects hit points and athletics.
    /// </summary>
    public int Endurance { get; set; }

    /// <summary>
    /// Cunning - affects tactics and roguery.
    /// </summary>
    public int Cunning { get; set; }

    /// <summary>
    /// Social - affects charm and leadership.
    /// </summary>
    public int Social { get; set; }

    /// <summary>
    /// Intelligence - affects engineering and medicine.
    /// </summary>
    public int Intelligence { get; set; }

    /// <summary>
    /// Gets the total of all attributes.
    /// </summary>
    public int Total => Vigor + Control + Endurance + Cunning + Social + Intelligence;

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public HeroAttributes Clone() => new()
    {
        Vigor = Vigor,
        Control = Control,
        Endurance = Endurance,
        Cunning = Cunning,
        Social = Social,
        Intelligence = Intelligence
    };

    object ICloneable.Clone() => Clone();

    /// <summary>
    /// Gets or sets an attribute by type.
    /// </summary>
    public int this[AttributeType type]
    {
        get => type switch
        {
            AttributeType.Vigor => Vigor,
            AttributeType.Control => Control,
            AttributeType.Endurance => Endurance,
            AttributeType.Cunning => Cunning,
            AttributeType.Social => Social,
            AttributeType.Intelligence => Intelligence,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        set
        {
            switch (type)
            {
                case AttributeType.Vigor: Vigor = value; break;
                case AttributeType.Control: Control = value; break;
                case AttributeType.Endurance: Endurance = value; break;
                case AttributeType.Cunning: Cunning = value; break;
                case AttributeType.Social: Social = value; break;
                case AttributeType.Intelligence: Intelligence = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}

/// <summary>
/// Attribute types.
/// </summary>
public enum AttributeType
{
    Vigor,
    Control,
    Endurance,
    Cunning,
    Social,
    Intelligence
}

/// <summary>
/// Hero skill values.
/// </summary>
public sealed class SkillSet : ICloneable
{
    // Combat Skills
    public int OneHanded { get; set; }
    public int TwoHanded { get; set; }
    public int Polearm { get; set; }
    public int Bow { get; set; }
    public int Crossbow { get; set; }
    public int Throwing { get; set; }

    // Movement Skills
    public int Riding { get; set; }
    public int Athletics { get; set; }

    // Support Skills
    public int Crafting { get; set; }
    public int Scouting { get; set; }
    public int Tactics { get; set; }
    public int Roguery { get; set; }

    // Social Skills
    public int Charm { get; set; }
    public int Leadership { get; set; }
    public int Trade { get; set; }

    // Knowledge Skills
    public int Steward { get; set; }
    public int Medicine { get; set; }
    public int Engineering { get; set; }

    /// <summary>
    /// Gets or sets a skill by type.
    /// </summary>
    public int this[SkillType type]
    {
        get => type switch
        {
            SkillType.OneHanded => OneHanded,
            SkillType.TwoHanded => TwoHanded,
            SkillType.Polearm => Polearm,
            SkillType.Bow => Bow,
            SkillType.Crossbow => Crossbow,
            SkillType.Throwing => Throwing,
            SkillType.Riding => Riding,
            SkillType.Athletics => Athletics,
            SkillType.Crafting => Crafting,
            SkillType.Scouting => Scouting,
            SkillType.Tactics => Tactics,
            SkillType.Roguery => Roguery,
            SkillType.Charm => Charm,
            SkillType.Leadership => Leadership,
            SkillType.Trade => Trade,
            SkillType.Steward => Steward,
            SkillType.Medicine => Medicine,
            SkillType.Engineering => Engineering,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        set
        {
            switch (type)
            {
                case SkillType.OneHanded: OneHanded = value; break;
                case SkillType.TwoHanded: TwoHanded = value; break;
                case SkillType.Polearm: Polearm = value; break;
                case SkillType.Bow: Bow = value; break;
                case SkillType.Crossbow: Crossbow = value; break;
                case SkillType.Throwing: Throwing = value; break;
                case SkillType.Riding: Riding = value; break;
                case SkillType.Athletics: Athletics = value; break;
                case SkillType.Crafting: Crafting = value; break;
                case SkillType.Scouting: Scouting = value; break;
                case SkillType.Tactics: Tactics = value; break;
                case SkillType.Roguery: Roguery = value; break;
                case SkillType.Charm: Charm = value; break;
                case SkillType.Leadership: Leadership = value; break;
                case SkillType.Trade: Trade = value; break;
                case SkillType.Steward: Steward = value; break;
                case SkillType.Medicine: Medicine = value; break;
                case SkillType.Engineering: Engineering = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public SkillSet Clone() => new()
    {
        OneHanded = OneHanded,
        TwoHanded = TwoHanded,
        Polearm = Polearm,
        Bow = Bow,
        Crossbow = Crossbow,
        Throwing = Throwing,
        Riding = Riding,
        Athletics = Athletics,
        Crafting = Crafting,
        Scouting = Scouting,
        Tactics = Tactics,
        Roguery = Roguery,
        Charm = Charm,
        Leadership = Leadership,
        Trade = Trade,
        Steward = Steward,
        Medicine = Medicine,
        Engineering = Engineering
    };

    object ICloneable.Clone() => Clone();
}

/// <summary>
/// Skill types.
/// </summary>
public enum SkillType
{
    // Combat
    OneHanded, TwoHanded, Polearm, Bow, Crossbow, Throwing,
    // Movement
    Riding, Athletics,
    // Support
    Crafting, Scouting, Tactics, Roguery,
    // Social
    Charm, Leadership, Trade,
    // Knowledge
    Steward, Medicine, Engineering
}

/// <summary>
/// Naval skill set (War Sails expansion).
/// </summary>
public sealed class NavalSkillSet : ICloneable
{
    /// <summary>
    /// Navigation - affects ship speed and weather handling.
    /// </summary>
    public int Navigation { get; set; }

    /// <summary>
    /// Naval Tactics - affects combat bonuses and formations.
    /// </summary>
    public int NavalTactics { get; set; }

    /// <summary>
    /// Naval Stewardship - affects crew morale and cargo capacity.
    /// </summary>
    public int NavalStewardship { get; set; }

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public NavalSkillSet Clone() => new()
    {
        Navigation = Navigation,
        NavalTactics = NavalTactics,
        NavalStewardship = NavalStewardship
    };

    object ICloneable.Clone() => Clone();
}

/// <summary>
/// Equipment set containing all slots.
/// </summary>
public sealed class EquipmentSet
{
    public EquipmentItem? Head { get; set; }
    public EquipmentItem? Body { get; set; }
    public EquipmentItem? Hands { get; set; }
    public EquipmentItem? Legs { get; set; }
    public EquipmentItem? Cape { get; set; }
    public EquipmentItem? Weapon0 { get; set; }
    public EquipmentItem? Weapon1 { get; set; }
    public EquipmentItem? Weapon2 { get; set; }
    public EquipmentItem? Weapon3 { get; set; }
    public EquipmentItem? Horse { get; set; }
    public EquipmentItem? HorseHarness { get; set; }
}

/// <summary>
/// Single equipment item.
/// </summary>
public sealed class EquipmentItem
{
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public ItemModifier? Modifier { get; set; }
}

/// <summary>
/// Item modifier (prefix/suffix).
/// </summary>
public sealed class ItemModifier
{
    public string ModifierId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Hero appearance data.
/// </summary>
public sealed class AppearanceData : ICloneable
{
    /// <summary>
    /// Face code for importing/exporting.
    /// </summary>
    public string FaceCode { get; set; } = string.Empty;

    /// <summary>
    /// Individual face sliders (64 values).
    /// </summary>
    public float[] FaceSliders { get; set; } = new float[64];

    /// <summary>
    /// Hair color (ARGB).
    /// </summary>
    public int HairColor { get; set; }

    /// <summary>
    /// Skin color (ARGB).
    /// </summary>
    public int SkinColor { get; set; }

    /// <summary>
    /// Eye color (ARGB).
    /// </summary>
    public int EyeColor { get; set; }

    /// <summary>
    /// Hair style index.
    /// </summary>
    public int HairStyle { get; set; }

    /// <summary>
    /// Beard style index.
    /// </summary>
    public int BeardStyle { get; set; }

    /// <summary>
    /// Tattoo index.
    /// </summary>
    public int TattooIndex { get; set; }

    /// <summary>
    /// Body properties (build, weight, etc).
    /// </summary>
    public BodyProperties Body { get; set; } = new();

    /// <summary>
    /// Voice type.
    /// </summary>
    public string VoiceType { get; set; } = string.Empty;

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public AppearanceData Clone() => new()
    {
        FaceCode = FaceCode,
        FaceSliders = (float[])FaceSliders.Clone(),
        HairColor = HairColor,
        SkinColor = SkinColor,
        EyeColor = EyeColor,
        HairStyle = HairStyle,
        BeardStyle = BeardStyle,
        TattooIndex = TattooIndex,
        Body = new BodyProperties
        {
            Build = Body.Build,
            Weight = Body.Weight,
            Height = Body.Height
        },
        VoiceType = VoiceType
    };

    object ICloneable.Clone() => Clone();

    /// <summary>
    /// Creates appearance from a face code string.
    /// </summary>
    public static AppearanceData FromFaceCode(string faceCode)
    {
        return new AppearanceData { FaceCode = faceCode };
    }

    /// <summary>
    /// Exports appearance as a face code string.
    /// </summary>
    public string ToFaceCode() => FaceCode;
}

/// <summary>
/// Body properties.
/// </summary>
public sealed class BodyProperties
{
    public float Build { get; set; } = 0.5f;
    public float Weight { get; set; } = 0.5f;
    public float Height { get; set; } = 0.5f;
}

/// <summary>
/// Hero trait data.
/// </summary>
public sealed class TraitData
{
    public string TraitId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
}

/// <summary>
/// Hero gender.
/// </summary>
public enum Gender
{
    Male,
    Female
}

/// <summary>
/// Hero state.
/// </summary>
public enum HeroState
{
    Active,
    Fugitive,
    Prisoner,
    Released,
    Dead,
    Disabled,
    NotSpawned,
    Traveling
}

/// <summary>
/// Interface for editable entities.
/// </summary>
public interface IEditable
{
    bool IsDirty { get; set; }
    void MarkDirty();
}
