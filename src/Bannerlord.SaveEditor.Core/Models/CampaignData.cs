// <copyright file="CampaignData.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Models;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.WarSails;

/// <summary>
/// Represents the complete campaign state.
/// </summary>
public sealed class CampaignData
{
    /// <summary>
    /// Gets or sets all heroes.
    /// </summary>
    public IList<HeroData> Heroes { get; set; } = new List<HeroData>();

    /// <summary>
    /// Gets or sets all parties.
    /// </summary>
    public IList<PartyData> Parties { get; set; } = new List<PartyData>();

    /// <summary>
    /// Gets or sets all settlements.
    /// </summary>
    public IList<SettlementData> Settlements { get; set; } = new List<SettlementData>();

    /// <summary>
    /// Gets or sets all factions.
    /// </summary>
    public IList<FactionData> Factions { get; set; } = new List<FactionData>();

    /// <summary>
    /// Gets or sets all clans.
    /// </summary>
    public IList<ClanData> Clans { get; set; } = new List<ClanData>();

    /// <summary>
    /// Gets or sets all kingdoms.
    /// </summary>
    public IList<KingdomData> Kingdoms { get; set; } = new List<KingdomData>();

    /// <summary>
    /// Gets or sets all quests.
    /// </summary>
    public IList<QuestData> Quests { get; set; } = new List<QuestData>();

    /// <summary>
    /// Gets or sets all workshops.
    /// </summary>
    public IList<WorkshopData> Workshops { get; set; } = new List<WorkshopData>();

    /// <summary>
    /// Gets or sets all caravans.
    /// </summary>
    public IList<CaravanData> Caravans { get; set; } = new List<CaravanData>();

    /// <summary>
    /// Gets or sets all fleets (War Sails).
    /// </summary>
    public IList<FleetData> Fleets { get; set; } = new List<FleetData>();
}

/// <summary>
/// Represents in-game campaign time.
/// </summary>
public readonly struct CampaignTime : IEquatable<CampaignTime>, IComparable<CampaignTime>
{
    /// <summary>
    /// Ticks per hour in game time.
    /// </summary>
    public const long TicksPerHour = 2500;

    /// <summary>
    /// Ticks per day in game time.
    /// </summary>
    public const long TicksPerDay = TicksPerHour * 24;

    /// <summary>
    /// Ticks per season in game time.
    /// </summary>
    public const long TicksPerSeason = TicksPerDay * 21;

    /// <summary>
    /// Ticks per year in game time.
    /// </summary>
    public const long TicksPerYear = TicksPerSeason * 4;

    /// <summary>
    /// Gets the raw tick count.
    /// </summary>
    public long Ticks { get; init; }

    /// <summary>
    /// Gets the current year.
    /// </summary>
    public int Year => (int)(Ticks / TicksPerYear) + 1084;

    /// <summary>
    /// Gets the current season (0-3).
    /// </summary>
    public int Season => (int)((Ticks % TicksPerYear) / TicksPerSeason);

    /// <summary>
    /// Gets the season name.
    /// </summary>
    public string SeasonName => Season switch
    {
        0 => "Spring",
        1 => "Summer",
        2 => "Autumn",
        3 => "Winter",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the day of the season (1-21).
    /// </summary>
    public int DayOfSeason => (int)((Ticks % TicksPerSeason) / TicksPerDay) + 1;

    /// <summary>
    /// Gets the hour of the day (0-23).
    /// </summary>
    public int HourOfDay => (int)((Ticks % TicksPerDay) / TicksPerHour);

    /// <summary>
    /// Gets the total number of days elapsed.
    /// </summary>
    public int TotalDays => (int)(Ticks / TicksPerDay);

    /// <summary>
    /// Creates a new CampaignTime from ticks.
    /// </summary>
    public CampaignTime(long ticks) => Ticks = ticks;

    /// <summary>
    /// Creates a CampaignTime from year, season, day components.
    /// </summary>
    public static CampaignTime FromComponents(int year, int season, int day, int hour = 12)
    {
        var adjustedYear = year - 1084;
        var ticks = (adjustedYear * TicksPerYear) +
                    (season * TicksPerSeason) +
                    ((day - 1) * TicksPerDay) +
                    (hour * TicksPerHour);
        return new CampaignTime(ticks);
    }

    /// <inheritdoc />
    public override string ToString() => $"Year {Year}, {SeasonName}, Day {DayOfSeason}";

    /// <inheritdoc />
    public bool Equals(CampaignTime other) => Ticks == other.Ticks;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CampaignTime other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Ticks.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(CampaignTime other) => Ticks.CompareTo(other.Ticks);

    public static bool operator ==(CampaignTime left, CampaignTime right) => left.Equals(right);
    public static bool operator !=(CampaignTime left, CampaignTime right) => !left.Equals(right);
    public static bool operator <(CampaignTime left, CampaignTime right) => left.CompareTo(right) < 0;
    public static bool operator >(CampaignTime left, CampaignTime right) => left.CompareTo(right) > 0;
    public static bool operator <=(CampaignTime left, CampaignTime right) => left.CompareTo(right) <= 0;
    public static bool operator >=(CampaignTime left, CampaignTime right) => left.CompareTo(right) >= 0;
}

/// <summary>
/// Quest data.
/// </summary>
public sealed class QuestData
{
    public MBGUID Id { get; set; }
    public string QuestId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public QuestState State { get; set; }
    public CampaignTime StartTime { get; set; }
    public CampaignTime? DueTime { get; set; }
    public HeroData? QuestGiver { get; set; }
    public int Progress { get; set; }
}

/// <summary>
/// Quest state.
/// </summary>
public enum QuestState
{
    NotStarted,
    Active,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Workshop data.
/// </summary>
public sealed class WorkshopData
{
    public MBGUID Id { get; set; }
    public string WorkshopType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SettlementData? Settlement { get; set; }
    public HeroData? Owner { get; set; }
    public int Capital { get; set; }
    public int LastRunProfit { get; set; }
    public bool IsRunning { get; set; }
    public float Efficiency { get; set; } = 1.0f;
}

/// <summary>
/// Caravan data.
/// </summary>
public sealed class CaravanData
{
    public MBGUID Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PartyData? Party { get; set; }
    public HeroData? Owner { get; set; }
    public HeroData? CaravanLeader { get; set; }
    public int Gold { get; set; }
    public IList<ItemStack> Goods { get; set; } = new List<ItemStack>();
}

/// <summary>
/// Settlement data.
/// </summary>
public sealed class SettlementData
{
    public MBGUID Id { get; set; }
    public string SettlementId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SettlementType Type { get; set; }
    public Vec2 Position { get; set; }
    public ClanData? OwnerClan { get; set; }
    public HeroData? Governor { get; set; }
    public int Prosperity { get; set; }
    public int Loyalty { get; set; }
    public int Security { get; set; }
    public int FoodStocks { get; set; }
    public int Militia { get; set; }
    public int Garrison { get; set; }
    public int WallLevel { get; set; }
    public bool IsUnderSiege { get; set; }
    public IList<BuildingData> Buildings { get; set; } = new List<BuildingData>();
}

/// <summary>
/// Settlement type.
/// </summary>
public enum SettlementType
{
    Town,
    Castle,
    Village,
    Hideout
}

/// <summary>
/// Building data within a settlement.
/// </summary>
public sealed class BuildingData
{
    public string BuildingType { get; set; } = string.Empty;
    public int CurrentLevel { get; set; }
    public float BuildProgress { get; set; }
    public bool IsCurrentlyBuilding { get; set; }
}

/// <summary>
/// Faction data.
/// </summary>
public sealed class FactionData
{
    public MBGUID Id { get; set; }
    public string FactionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public FactionType Type { get; set; }
    public int MainColor { get; set; }
    public int SecondaryColor { get; set; }
    public IList<RelationData> Relations { get; set; } = new List<RelationData>();
}

/// <summary>
/// Faction type.
/// </summary>
public enum FactionType
{
    Kingdom,
    Clan,
    MinorFaction,
    Bandit,
    Neutral
}

/// <summary>
/// Relation data between factions.
/// </summary>
public sealed class RelationData
{
    public MBGUID TargetFactionId { get; set; }
    public int RelationValue { get; set; }
    public bool AtWar { get; set; }
}

/// <summary>
/// Clan data.
/// </summary>
public sealed class ClanData
{
    public MBGUID Id { get; set; }
    public string ClanId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public HeroData? Leader { get; set; }
    public KingdomData? Kingdom { get; set; }
    public int Tier { get; set; }
    public int Renown { get; set; }
    public int Influence { get; set; }
    public int Gold { get; set; }
    public int MainColor { get; set; }
    public int SecondaryColor { get; set; }
    public bool IsPlayerClan { get; set; }
    public bool IsBanditClan { get; set; }
    public bool IsMinorFaction { get; set; }
    public IList<HeroData> Members { get; set; } = new List<HeroData>();
    public IList<SettlementData> Fiefs { get; set; } = new List<SettlementData>();
    public IList<PartyData> Parties { get; set; } = new List<PartyData>();
}

/// <summary>
/// Kingdom data.
/// </summary>
public sealed class KingdomData
{
    public MBGUID Id { get; set; }
    public string KingdomId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public HeroData? Ruler { get; set; }
    public ClanData? RulingClan { get; set; }
    public int MainColor { get; set; }
    public int SecondaryColor { get; set; }
    public float TotalStrength { get; set; }
    public IList<ClanData> Clans { get; set; } = new List<ClanData>();
    public IList<PolicyData> ActivePolicies { get; set; } = new List<PolicyData>();
    public IList<ArmyData> Armies { get; set; } = new List<ArmyData>();
}

/// <summary>
/// Kingdom policy data.
/// </summary>
public sealed class PolicyData
{
    public string PolicyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Army data.
/// </summary>
public sealed class ArmyData
{
    public MBGUID Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public HeroData? Leader { get; set; }
    public IList<PartyData> Parties { get; set; } = new List<PartyData>();
    public int TotalTroops { get; set; }
    public float Cohesion { get; set; }
}

/// <summary>
/// 2D position vector.
/// </summary>
public readonly struct Vec2 : IEquatable<Vec2>
{
    public float X { get; init; }
    public float Y { get; init; }

    public Vec2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float Length => MathF.Sqrt(X * X + Y * Y);

    public static Vec2 Zero => new(0, 0);

    public static float Distance(Vec2 a, Vec2 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public override string ToString() => $"({X:F2}, {Y:F2})";

    public bool Equals(Vec2 other) => Math.Abs(X - other.X) < 0.001f && Math.Abs(Y - other.Y) < 0.001f;
    public override bool Equals(object? obj) => obj is Vec2 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public static bool operator ==(Vec2 left, Vec2 right) => left.Equals(right);
    public static bool operator !=(Vec2 left, Vec2 right) => !left.Equals(right);
}
