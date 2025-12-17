// <copyright file="PartyEditor.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Services;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// High-level party editing operations.
/// </summary>
public sealed class PartyEditor
{
    private readonly ILogger<PartyEditor>? _logger;

    public PartyEditor(ILogger<PartyEditor>? logger = null)
    {
        _logger = logger;
    }

    #region Troop Operations

    /// <summary>
    /// Adds troops to a party.
    /// </summary>
    public void AddTroops(PartyData party, string troopId, string troopName, int count, int tier = 1)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");

        var existing = party.Troops.FirstOrDefault(t => t.TroopId == troopId);
        if (existing != null)
        {
            existing.Count += count;
        }
        else
        {
            party.Troops.Add(new TroopStack
            {
                TroopId = troopId,
                TroopName = troopName,
                Count = count,
                WoundedCount = 0,
                Tier = tier,
                IsHero = false
            });
        }

        _logger?.LogDebug("Added {Count}x {Troop} to {Party}", count, troopName, party.Name);
    }

    /// <summary>
    /// Removes troops from a party.
    /// </summary>
    public int RemoveTroops(PartyData party, string troopId, int? count = null)
    {
        var troop = party.Troops.FirstOrDefault(t => t.TroopId == troopId);
        if (troop == null) return 0;

        int removed;
        if (count == null || count >= troop.Count)
        {
            removed = troop.Count;
            party.Troops.Remove(troop);
        }
        else
        {
            removed = count.Value;
            troop.Count -= removed;
            troop.WoundedCount = Math.Min(troop.WoundedCount, troop.Count);
        }

        _logger?.LogDebug("Removed {Count}x {Troop} from {Party}", removed, troopId, party.Name);
        return removed;
    }

    /// <summary>
    /// Sets the count for a specific troop type.
    /// </summary>
    public void SetTroopCount(PartyData party, string troopId, int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

        var troop = party.Troops.FirstOrDefault(t => t.TroopId == troopId);
        if (troop != null)
        {
            if (count == 0)
            {
                party.Troops.Remove(troop);
            }
            else
            {
                troop.Count = count;
                troop.WoundedCount = Math.Min(troop.WoundedCount, count);
            }
        }
        else if (count > 0)
        {
            _logger?.LogWarning("Cannot set count for unknown troop {TroopId}", troopId);
        }
    }

    /// <summary>
    /// Heals wounded troops.
    /// </summary>
    public void HealTroops(PartyData party, string? troopId = null)
    {
        if (troopId != null)
        {
            var troop = party.Troops.FirstOrDefault(t => t.TroopId == troopId);
            if (troop != null)
            {
                troop.WoundedCount = 0;
            }
        }
        else
        {
            foreach (var troop in party.Troops)
            {
                troop.WoundedCount = 0;
            }
        }

        _logger?.LogDebug("Healed troops in {Party}", party.Name);
    }

    /// <summary>
    /// Sets wounded count for a troop type.
    /// </summary>
    public void SetWoundedCount(PartyData party, string troopId, int wounded)
    {
        var troop = party.Troops.FirstOrDefault(t => t.TroopId == troopId);
        if (troop == null) return;

        troop.WoundedCount = Math.Clamp(wounded, 0, troop.Count);
    }

    /// <summary>
    /// Upgrades troops to a higher tier.
    /// </summary>
    public void UpgradeTroops(PartyData party, string fromTroopId, string toTroopId, string toTroopName, int count, int newTier)
    {
        var fromTroop = party.Troops.FirstOrDefault(t => t.TroopId == fromTroopId);
        if (fromTroop == null || fromTroop.Count < count)
        {
            throw new InvalidOperationException("Not enough troops to upgrade");
        }

        // Remove from source
        RemoveTroops(party, fromTroopId, count);

        // Add to target
        AddTroops(party, toTroopId, toTroopName, count, newTier);

        _logger?.LogDebug("Upgraded {Count}x troops from {From} to {To}", count, fromTroopId, toTroopId);
    }

    #endregion

    #region Prisoner Operations

    /// <summary>
    /// Adds prisoners to a party.
    /// </summary>
    public void AddPrisoners(PartyData party, string troopId, string troopName, int count, int tier = 1, MBGUID? heroId = null)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

        var existing = party.Prisoners.FirstOrDefault(p => p.TroopId == troopId);
        if (existing != null)
        {
            existing.Count += count;
        }
        else
        {
            party.Prisoners.Add(new TroopStack
            {
                TroopId = troopId,
                TroopName = troopName,
                Count = count,
                WoundedCount = 0,
                Tier = tier,
                IsHero = heroId.HasValue,
                HeroId = heroId
            });
        }

        _logger?.LogDebug("Added {Count}x {Troop} as prisoners to {Party}", count, troopName, party.Name);
    }

    /// <summary>
    /// Releases prisoners from a party.
    /// </summary>
    public int ReleasePrisoners(PartyData party, string? troopId = null, int? count = null)
    {
        if (troopId == null)
        {
            var total = party.Prisoners.Sum(p => p.Count);
            party.Prisoners.Clear();
            _logger?.LogDebug("Released all {Count} prisoners from {Party}", total, party.Name);
            return total;
        }

        var prisoner = party.Prisoners.FirstOrDefault(p => p.TroopId == troopId);
        if (prisoner == null) return 0;

        int released;
        if (count == null || count >= prisoner.Count)
        {
            released = prisoner.Count;
            party.Prisoners.Remove(prisoner);
        }
        else
        {
            released = count.Value;
            prisoner.Count -= released;
        }

        _logger?.LogDebug("Released {Count}x {Troop} from {Party}", released, troopId, party.Name);
        return released;
    }

    /// <summary>
    /// Recruits prisoners into the party.
    /// </summary>
    public void RecruitPrisoners(PartyData party, string troopId, int count)
    {
        var prisoner = party.Prisoners.FirstOrDefault(p => p.TroopId == troopId);
        if (prisoner == null || prisoner.Count < count)
        {
            throw new InvalidOperationException("Not enough prisoners to recruit");
        }

        // Add as troops
        AddTroops(party, troopId, prisoner.TroopName, count, prisoner.Tier);

        // Remove from prisoners
        ReleasePrisoners(party, troopId, count);

        _logger?.LogDebug("Recruited {Count}x {Troop} from prisoners in {Party}", count, troopId, party.Name);
    }

    #endregion

    #region Party Resources

    /// <summary>
    /// Sets party gold.
    /// </summary>
    public void SetGold(PartyData party, int gold)
    {
        if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
        party.Gold = gold;
        _logger?.LogDebug("Set {Party}.Gold = {Gold}", party.Name, gold);
    }

    /// <summary>
    /// Adds gold to party.
    /// </summary>
    public void AddGold(PartyData party, int amount)
    {
        party.Gold = Math.Max(0, party.Gold + amount);
        _logger?.LogDebug("Added {Amount} gold to {Party} (total: {Total})", amount, party.Name, party.Gold);
    }

    /// <summary>
    /// Sets party food.
    /// </summary>
    public void SetFood(PartyData party, float food)
    {
        if (food < 0) throw new ArgumentOutOfRangeException(nameof(food));
        party.Food = food;
        _logger?.LogDebug("Set {Party}.Food = {Food}", party.Name, food);
    }

    /// <summary>
    /// Sets party morale.
    /// </summary>
    public void SetMorale(PartyData party, float morale)
    {
        party.Morale = Math.Clamp(morale, 0f, 100f);
        _logger?.LogDebug("Set {Party}.Morale = {Morale}", party.Name, morale);
    }

    /// <summary>
    /// Sets party size limit.
    /// </summary>
    public void SetPartySizeLimit(PartyData party, int limit)
    {
        if (limit < 1) throw new ArgumentOutOfRangeException(nameof(limit));
        party.PartySizeLimit = limit;
        _logger?.LogDebug("Set {Party}.PartySizeLimit = {Limit}", party.Name, limit);
    }

    /// <summary>
    /// Sets prisoner limit.
    /// </summary>
    public void SetPrisonerLimit(PartyData party, int limit)
    {
        if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit));
        party.PrisonerLimit = limit;
        _logger?.LogDebug("Set {Party}.PrisonerLimit = {Limit}", party.Name, limit);
    }

    #endregion

    #region Party Movement

    /// <summary>
    /// Moves party to a position.
    /// </summary>
    public void MoveTo(PartyData party, float x, float y)
    {
        party.Position = new Vec2(x, y);
        _logger?.LogDebug("Moved {Party} to ({X}, {Y})", party.Name, x, y);
    }

    /// <summary>
    /// Sets party state.
    /// </summary>
    public void SetState(PartyData party, PartyState state)
    {
        party.State = state;
        _logger?.LogDebug("Set {Party}.State = {State}", party.Name, state);
    }

    /// <summary>
    /// Teleports party to a settlement.
    /// </summary>
    public void TeleportToSettlement(PartyData party, SettlementData settlement)
    {
        party.Position = settlement.Position;
        party.CurrentSettlementId = settlement.Id;
        party.State = PartyState.InSettlement;
        _logger?.LogDebug("Teleported {Party} to {Settlement}", party.Name, settlement.Name);
    }

    #endregion

    #region Party Statistics

    /// <summary>
    /// Gets party statistics summary.
    /// </summary>
    public PartyStatistics GetPartyStatistics(PartyData party)
    {
        return new PartyStatistics
        {
            TotalTroops = party.TotalTroopCount,
            HealthyTroops = party.HealthyTroopCount,
            WoundedTroops = party.WoundedTroopCount,
            TotalPrisoners = party.TotalPrisonerCount,
            UniqueUnits = party.Troops.Count,
            AverageTier = party.Troops.Count > 0
                ? party.Troops.Average(t => (float)t.Tier * t.Count) / party.TotalTroopCount
                : 0,
            TroopsByTier = party.Troops
                .GroupBy(t => t.Tier)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Count)),
            IsOverPartyLimit = party.TotalTroopCount > party.PartySizeLimit,
            IsOverPrisonerLimit = party.TotalPrisonerCount > party.PrisonerLimit
        };
    }

    #endregion
}

/// <summary>
/// Party statistics summary.
/// </summary>
public sealed class PartyStatistics
{
    public int TotalTroops { get; set; }
    public int HealthyTroops { get; set; }
    public int WoundedTroops { get; set; }
    public int TotalPrisoners { get; set; }
    public int UniqueUnits { get; set; }
    public float AverageTier { get; set; }
    public Dictionary<int, int> TroopsByTier { get; set; } = new();
    public bool IsOverPartyLimit { get; set; }
    public bool IsOverPrisonerLimit { get; set; }
}
