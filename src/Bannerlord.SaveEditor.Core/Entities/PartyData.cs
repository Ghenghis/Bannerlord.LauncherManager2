// <copyright file="PartyData.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Entities;

using Bannerlord.SaveEditor.Core.Models;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a party (troop group) on the campaign map.
/// </summary>
public sealed class PartyData : IEditable
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public MBGUID Id { get; set; }

    /// <summary>
    /// Gets or sets the party name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the party leader.
    /// </summary>
    [JsonIgnore]
    public HeroData? Leader { get; set; }

    /// <summary>
    /// Gets or sets the leader ID reference.
    /// </summary>
    public MBGUID? LeaderId { get; set; }

    /// <summary>
    /// Gets or sets the owning clan.
    /// </summary>
    [JsonIgnore]
    public ClanData? Clan { get; set; }

    /// <summary>
    /// Gets or sets the clan ID reference.
    /// </summary>
    public MBGUID? ClanId { get; set; }

    /// <summary>
    /// Gets or sets the party type.
    /// </summary>
    public PartyType Type { get; set; }

    /// <summary>
    /// Gets or sets the troop roster.
    /// </summary>
    public IList<TroopStack> Troops { get; set; } = new List<TroopStack>();

    /// <summary>
    /// Gets or sets the prisoner roster.
    /// </summary>
    public IList<TroopStack> Prisoners { get; set; } = new List<TroopStack>();

    /// <summary>
    /// Gets the total troop count (excluding prisoners).
    /// </summary>
    public int TotalTroopCount => Troops.Sum(t => t.Count);

    /// <summary>
    /// Gets the total prisoner count.
    /// </summary>
    public int TotalPrisonerCount => Prisoners.Sum(p => p.Count);

    /// <summary>
    /// Gets the total healthy troop count.
    /// </summary>
    public int HealthyTroopCount => Troops.Sum(t => t.HealthyCount);

    /// <summary>
    /// Gets the total wounded troop count.
    /// </summary>
    public int WoundedTroopCount => Troops.Sum(t => t.WoundedCount);

    /// <summary>
    /// Gets or sets the party size limit.
    /// </summary>
    public int PartySizeLimit { get; set; }

    /// <summary>
    /// Gets or sets the prisoner limit.
    /// </summary>
    public int PrisonerLimit { get; set; }

    /// <summary>
    /// Gets or sets the inventory items.
    /// </summary>
    public IList<ItemStack> Inventory { get; set; } = new List<ItemStack>();

    /// <summary>
    /// Gets or sets the party gold.
    /// </summary>
    public int Gold { get; set; }

    /// <summary>
    /// Gets or sets the food amount.
    /// </summary>
    public float Food { get; set; }

    /// <summary>
    /// Gets or sets the party's position on the map.
    /// </summary>
    public Vec2 Position { get; set; }

    /// <summary>
    /// Gets or sets the current settlement (if stationed).
    /// </summary>
    [JsonIgnore]
    public SettlementData? CurrentSettlement { get; set; }

    /// <summary>
    /// Gets or sets the settlement ID reference.
    /// </summary>
    public MBGUID? CurrentSettlementId { get; set; }

    /// <summary>
    /// Gets or sets the party state.
    /// </summary>
    public PartyState State { get; set; }

    /// <summary>
    /// Gets or sets the party morale (0-100).
    /// </summary>
    public float Morale { get; set; }

    /// <summary>
    /// Gets or sets the base speed.
    /// </summary>
    public float BaseSpeed { get; set; }

    /// <summary>
    /// Gets or sets the current speed.
    /// </summary>
    public float CurrentSpeed { get; set; }

    /// <summary>
    /// Gets or sets the scout party flag.
    /// </summary>
    public bool IsScout { get; set; }

    /// <summary>
    /// Gets or sets the caravan party flag.
    /// </summary>
    public bool IsCaravan { get; set; }

    /// <summary>
    /// Gets or sets the garrison flag.
    /// </summary>
    public bool IsGarrison { get; set; }

    /// <summary>
    /// Gets or sets the militia flag.
    /// </summary>
    public bool IsMilitia { get; set; }

    /// <summary>
    /// Gets or sets the formation settings.
    /// </summary>
    public PartyFormation Formation { get; set; } = new();

    /// <inheritdoc />
    public bool IsDirty { get; set; }

    /// <inheritdoc />
    public void MarkDirty() => IsDirty = true;
}

/// <summary>
/// Party type.
/// </summary>
public enum PartyType
{
    Lord,
    Caravan,
    Garrison,
    Militia,
    Bandit,
    Villager,
    Quest,
    Special
}

/// <summary>
/// Party state.
/// </summary>
public enum PartyState
{
    Active,
    InSettlement,
    InArmy,
    Besieging,
    BeingBesieged,
    Defeated,
    Disbanded
}

/// <summary>
/// Troop stack in a party roster.
/// </summary>
public sealed class TroopStack
{
    /// <summary>
    /// Gets or sets the troop ID.
    /// </summary>
    public string TroopId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string TroopName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the wounded count.
    /// </summary>
    public int WoundedCount { get; set; }

    /// <summary>
    /// Gets the healthy count.
    /// </summary>
    public int HealthyCount => Count - WoundedCount;

    /// <summary>
    /// Gets or sets the tier.
    /// </summary>
    public int Tier { get; set; }

    /// <summary>
    /// Gets or sets whether these are hero troops.
    /// </summary>
    public bool IsHero { get; set; }

    /// <summary>
    /// Gets or sets the hero reference (if IsHero).
    /// </summary>
    public MBGUID? HeroId { get; set; }

    /// <summary>
    /// Gets or sets the experience.
    /// </summary>
    public int Experience { get; set; }

    /// <summary>
    /// Creates a copy of this troop stack.
    /// </summary>
    public TroopStack Clone() => new()
    {
        TroopId = TroopId,
        TroopName = TroopName,
        Count = Count,
        WoundedCount = WoundedCount,
        Tier = Tier,
        IsHero = IsHero,
        HeroId = HeroId,
        Experience = Experience
    };
}

/// <summary>
/// Item stack in inventory.
/// </summary>
public sealed class ItemStack
{
    /// <summary>
    /// Gets or sets the item ID.
    /// </summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the item modifier.
    /// </summary>
    public ItemModifier? Modifier { get; set; }

    /// <summary>
    /// Gets or sets the item value.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the item weight.
    /// </summary>
    public float Weight { get; set; }

    /// <summary>
    /// Gets or sets the item type.
    /// </summary>
    public ItemType Type { get; set; }

    /// <summary>
    /// Creates a copy of this item stack.
    /// </summary>
    public ItemStack Clone() => new()
    {
        ItemId = ItemId,
        ItemName = ItemName,
        Count = Count,
        Modifier = Modifier,
        Value = Value,
        Weight = Weight,
        Type = Type
    };
}

/// <summary>
/// Item types.
/// </summary>
public enum ItemType
{
    Invalid,
    Horse,
    OneHandedWeapon,
    TwoHandedWeapon,
    Polearm,
    Arrows,
    Bolts,
    Shield,
    Bow,
    Crossbow,
    Thrown,
    Goods,
    HeadArmor,
    BodyArmor,
    LegArmor,
    HandArmor,
    Pistol,
    Musket,
    Bullets,
    Animal,
    Book,
    Cape,
    HorseHarness,
    Banner
}

/// <summary>
/// Party formation settings.
/// </summary>
public sealed class PartyFormation
{
    public FormationType Infantry { get; set; } = FormationType.Line;
    public FormationType Ranged { get; set; } = FormationType.Loose;
    public FormationType Cavalry { get; set; } = FormationType.Line;
    public FormationType HorseArcher { get; set; } = FormationType.Skirmish;
    public FormationType Skirmisher { get; set; } = FormationType.Scatter;
    public FormationType HeavyInfantry { get; set; } = FormationType.ShieldWall;
    public FormationType LightCavalry { get; set; } = FormationType.Line;
    public FormationType HeavyCavalry { get; set; } = FormationType.Line;
}

/// <summary>
/// Formation types.
/// </summary>
public enum FormationType
{
    Line,
    ShieldWall,
    Loose,
    Circle,
    Square,
    Skirmish,
    Column,
    Scatter
}
