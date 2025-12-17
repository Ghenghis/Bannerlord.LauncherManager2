// <copyright file="FleetEditor.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Services;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.Validation;
using Bannerlord.SaveEditor.Core.WarSails;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for editing fleet and ship data (War Sails expansion).
/// </summary>
public sealed class FleetEditor
{
    private readonly ILogger<FleetEditor>? _logger;
    private readonly IValidationService _validation;

    public FleetEditor(IValidationService? validation = null, ILogger<FleetEditor>? logger = null)
    {
        _validation = validation ?? new ValidationService();
        _logger = logger;
    }

    #region Fleet Operations

    public FleetData CreateFleet(string name, HeroData? admiral = null, ClanData? clan = null)
    {
        var fleet = new FleetData
        {
            Id = MBGUID.Generate(MBGUIDType.Fleet),
            Name = name,
            AdmiralId = admiral?.Id,
            Admiral = admiral,
            ClanId = clan?.Id,
            Clan = clan,
            State = FleetState.Docked,
            Formation = FleetFormation.Line,
            Morale = 50,
            Gold = 0,
            FoodSupplies = 100
        };

        _logger?.LogInformation("Created fleet: {Name}", name);
        return fleet;
    }

    public void SetAdmiral(FleetData fleet, HeroData? admiral)
    {
        fleet.AdmiralId = admiral?.Id;
        fleet.Admiral = admiral;
        
        if (admiral != null)
        {
            admiral.FleetId = fleet.Id;
            admiral.Fleet = fleet;
        }

        _logger?.LogDebug("Set admiral of {Fleet} to {Admiral}", fleet.Name, admiral?.Name ?? "None");
    }

    public void SetFlagship(FleetData fleet, ShipData? ship)
    {
        if (ship != null && !fleet.Ships.Contains(ship))
        {
            throw new EditorException("Ship is not in this fleet");
        }

        fleet.FlagshipId = ship?.Id;
        fleet.Flagship = ship;

        if (ship != null)
        {
            ship.ShipClass = ShipClass.Flagship;
        }

        _logger?.LogDebug("Set flagship of {Fleet} to {Ship}", fleet.Name, ship?.Name ?? "None");
    }

    public void SetFleetState(FleetData fleet, FleetState state)
    {
        fleet.State = state;
        _logger?.LogDebug("Set {Fleet} state to {State}", fleet.Name, state);
    }

    public void SetFleetFormation(FleetData fleet, FleetFormation formation)
    {
        fleet.Formation = formation;
        _logger?.LogDebug("Set {Fleet} formation to {Formation}", fleet.Name, formation);
    }

    public void SetFleetMorale(FleetData fleet, float morale)
    {
        fleet.Morale = Math.Clamp(morale, 0, 100);
    }

    public void SetFleetGold(FleetData fleet, int gold)
    {
        if (gold < 0)
            throw new EditorException("Gold cannot be negative");

        fleet.Gold = gold;
    }

    public void SetFleetFood(FleetData fleet, float food)
    {
        fleet.FoodSupplies = Math.Max(0, food);
    }

    public void SetFleetPosition(FleetData fleet, float x, float y, float heading = 0)
    {
        fleet.Position = new NavalPosition(x, y, heading);
    }

    #endregion

    #region Ship Operations

    public ShipData CreateShip(string name, ShipType type)
    {
        var ship = new ShipData
        {
            Id = MBGUID.Generate(MBGUIDType.Ship),
            Name = name,
            Type = type,
            ShipClass = ShipClass.Standard,
            CurrentHullPoints = ShipData.GetBaseHullPoints(type),
            CrewCount = 0,
            CrewQuality = CrewQuality.Regular,
            CrewMorale = 50
        };

        _logger?.LogInformation("Created ship: {Name} ({Type})", name, type);
        return ship;
    }

    public void AddShipToFleet(FleetData fleet, ShipData ship)
    {
        if (fleet.Ships.Contains(ship))
            return;

        ship.FleetId = fleet.Id;
        ship.Fleet = fleet;
        fleet.Ships.Add(ship);

        // Set as flagship if first ship
        if (fleet.Flagship == null)
        {
            SetFlagship(fleet, ship);
        }

        _logger?.LogDebug("Added {Ship} to {Fleet}", ship.Name, fleet.Name);
    }

    public void RemoveShipFromFleet(FleetData fleet, ShipData ship)
    {
        if (!fleet.Ships.Contains(ship))
            return;

        ship.FleetId = null;
        ship.Fleet = null;
        fleet.Ships.Remove(ship);

        // Clear flagship if it was this ship
        if (fleet.Flagship == ship)
        {
            fleet.FlagshipId = null;
            fleet.Flagship = fleet.Ships.FirstOrDefault();
        }

        _logger?.LogDebug("Removed {Ship} from {Fleet}", ship.Name, fleet.Name);
    }

    public void SetShipHull(ShipData ship, int hullPoints)
    {
        if (hullPoints < 0)
            throw new EditorException("Hull points cannot be negative");

        ship.CurrentHullPoints = Math.Min(hullPoints, ship.MaxHullPoints);
    }

    public void RepairShip(ShipData ship)
    {
        ship.CurrentHullPoints = ship.MaxHullPoints;
        _logger?.LogDebug("Repaired ship {Ship} to full hull ({HP} HP)", ship.Name, ship.MaxHullPoints);
    }

    public void RepairFleet(FleetData fleet)
    {
        foreach (var ship in fleet.Ships)
        {
            RepairShip(ship);
        }
        _logger?.LogInformation("Repaired all ships in fleet {Fleet}", fleet.Name);
    }

    public void SetShipCrew(ShipData ship, int count)
    {
        if (count < 0)
            throw new EditorException("Crew count cannot be negative");
        if (count > ship.CrewCapacity)
            throw new EditorException($"Crew count ({count}) exceeds capacity ({ship.CrewCapacity})");

        ship.CrewCount = count;
    }

    public void FillCrew(ShipData ship)
    {
        ship.CrewCount = ship.CrewCapacity;
        _logger?.LogDebug("Filled crew of {Ship} to capacity ({Count})", ship.Name, ship.CrewCapacity);
    }

    public void FillFleetCrew(FleetData fleet)
    {
        foreach (var ship in fleet.Ships)
        {
            FillCrew(ship);
        }
    }

    public void SetCrewQuality(ShipData ship, CrewQuality quality)
    {
        ship.CrewQuality = quality;
    }

    public void SetCrewMorale(ShipData ship, float morale)
    {
        ship.CrewMorale = Math.Clamp(morale, 0, 100);
    }

    #endregion

    #region Upgrades

    public void AddUpgrade(ShipData ship, ShipUpgrade upgrade)
    {
        if (ship.Upgrades.Contains(upgrade))
            return;

        // Check for conflicting upgrades
        var conflicts = GetConflictingUpgrades(upgrade);
        foreach (var conflict in conflicts)
        {
            if (ship.Upgrades.Contains(conflict))
            {
                throw new EditorException($"Upgrade {upgrade} conflicts with existing upgrade {conflict}");
            }
        }

        ship.Upgrades.Add(upgrade);
        _logger?.LogDebug("Added upgrade {Upgrade} to {Ship}", upgrade, ship.Name);
    }

    public void RemoveUpgrade(ShipData ship, ShipUpgrade upgrade)
    {
        ship.Upgrades.Remove(upgrade);
        _logger?.LogDebug("Removed upgrade {Upgrade} from {Ship}", upgrade, ship.Name);
    }

    public void ClearUpgrades(ShipData ship)
    {
        ship.Upgrades.Clear();
    }

    public void AddAllUpgrades(ShipData ship)
    {
        // Add non-conflicting upgrades
        var addedCategories = new HashSet<string>();
        foreach (ShipUpgrade upgrade in Enum.GetValues<ShipUpgrade>())
        {
            var category = GetUpgradeCategory(upgrade);
            if (!addedCategories.Contains(category))
            {
                ship.Upgrades.Add(upgrade);
                addedCategories.Add(category);
            }
        }
    }

    private static IEnumerable<ShipUpgrade> GetConflictingUpgrades(ShipUpgrade upgrade)
    {
        // Upgrades in same category conflict
        var category = GetUpgradeCategory(upgrade);
        return Enum.GetValues<ShipUpgrade>()
            .Where(u => u != upgrade && GetUpgradeCategory(u) == category);
    }

    private static string GetUpgradeCategory(ShipUpgrade upgrade)
    {
        return upgrade switch
        {
            ShipUpgrade.ReinforcedHull or ShipUpgrade.IronPlating or ShipUpgrade.WaterproofingTar or ShipUpgrade.CopperSheathing => "Hull",
            ShipUpgrade.SpeedSails or ShipUpgrade.StormSails or ShipUpgrade.NavigatorSails or ShipUpgrade.LatinSails => "Sails",
            ShipUpgrade.RammingProw => "Ram",
            ShipUpgrade.ExtendedHold or ShipUpgrade.SecureVault or ShipUpgrade.LivestockPens or ShipUpgrade.WaterBarrels => "Cargo",
            ShipUpgrade.CrewQuarters or ShipUpgrade.TrainingDummy or ShipUpgrade.Infirmary or ShipUpgrade.CaptainsCabin => "Crew",
            ShipUpgrade.BallistaMount or ShipUpgrade.CatapultMount or ShipUpgrade.GreekFireSiphon or ShipUpgrade.BoardingRamps or ShipUpgrade.ArrowSlits => "Combat",
            ShipUpgrade.Astrolabe or ShipUpgrade.ImprovedRudder or ShipUpgrade.HeavyAnchor or ShipUpgrade.Compass or ShipUpgrade.Charts => "Navigation",
            _ => upgrade.ToString()
        };
    }

    #endregion

    #region Cargo

    public void AddCargo(ShipData ship, CargoItem item)
    {
        var newWeight = ship.CurrentCargoWeight + (item.Weight * item.Count);
        if (newWeight > ship.CargoCapacity)
        {
            throw new EditorException($"Adding cargo would exceed capacity ({newWeight}/{ship.CargoCapacity})");
        }

        var existing = ship.Cargo.FirstOrDefault(c => c.ItemId == item.ItemId);
        if (existing != null)
        {
            existing.Count += item.Count;
        }
        else
        {
            ship.Cargo.Add(item);
        }

        _logger?.LogDebug("Added {Count}x {Item} to {Ship}", item.Count, item.ItemName, ship.Name);
    }

    public void RemoveCargo(ShipData ship, string itemId, int count)
    {
        var existing = ship.Cargo.FirstOrDefault(c => c.ItemId == itemId);
        if (existing == null)
            return;

        existing.Count -= count;
        if (existing.Count <= 0)
        {
            ship.Cargo.Remove(existing);
        }
    }

    public void ClearCargo(ShipData ship)
    {
        ship.Cargo.Clear();
    }

    public void TransferCargo(ShipData source, ShipData target, string itemId, int count)
    {
        var item = source.Cargo.FirstOrDefault(c => c.ItemId == itemId);
        if (item == null || item.Count < count)
        {
            throw new EditorException("Not enough cargo to transfer");
        }

        RemoveCargo(source, itemId, count);
        AddCargo(target, new CargoItem
        {
            ItemId = item.ItemId,
            ItemName = item.ItemName,
            Count = count,
            Weight = item.Weight,
            Value = item.Value
        });
    }

    #endregion

    #region Weapons

    public void AddWeapon(ShipData ship, ShipWeapon weapon)
    {
        ship.Weapons.Add(weapon);
        _logger?.LogDebug("Added {Weapon} to {Ship}", weapon.Type, ship.Name);
    }

    public void RemoveWeapon(ShipData ship, ShipWeapon weapon)
    {
        ship.Weapons.Remove(weapon);
    }

    public void ClearWeapons(ShipData ship)
    {
        ship.Weapons.Clear();
    }

    #endregion

    #region Validation

    public ValidationReport ValidateFleet(FleetData fleet)
    {
        return _validation.ValidateFleet(fleet);
    }

    #endregion
}

