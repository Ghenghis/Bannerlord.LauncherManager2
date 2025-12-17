// <copyright file="FleetEditorTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Services;
using Bannerlord.SaveEditor.Core.WarSails;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for FleetEditor (War Sails extension).
/// </summary>
public sealed class FleetEditorTests
{
    private readonly FleetEditor _editor;

    public FleetEditorTests()
    {
        _editor = new FleetEditor();
    }

    #region Helper Methods

    private FleetData CreateTestFleet()
    {
        return _editor.CreateFleet("Test Fleet");
    }

    private ShipData CreateTestShip(ShipType type = ShipType.Longship)
    {
        return _editor.CreateShip("Test Ship", type);
    }

    private HeroData CreateTestHero()
    {
        return new HeroData
        {
            Id = MBGUID.Generate(MBGUIDType.Hero),
            Name = "Test Admiral",
            IsAlive = true
        };
    }

    #endregion

    #region Fleet Creation Tests

    [Fact]
    public void CreateFleet_CreatesValidFleet()
    {
        // Act
        var fleet = _editor.CreateFleet("Royal Navy");

        // Assert
        fleet.Should().NotBeNull();
        fleet.Name.Should().Be("Royal Navy");
        fleet.Id.Should().NotBeNull();
        fleet.State.Should().Be(FleetState.Docked);
    }

    [Fact]
    public void CreateFleet_WithAdmiral_SetsAdmiral()
    {
        // Arrange
        var admiral = CreateTestHero();

        // Act
        var fleet = _editor.CreateFleet("Admiral's Fleet", admiral);

        // Assert
        fleet.AdmiralId.Should().Be(admiral.Id);
        fleet.Admiral.Should().Be(admiral);
    }

    #endregion

    #region Admiral Tests

    [Fact]
    public void SetAdmiral_AssignsAdmiral()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var admiral = CreateTestHero();

        // Act
        _editor.SetAdmiral(fleet, admiral);

        // Assert
        fleet.Admiral.Should().Be(admiral);
        fleet.AdmiralId.Should().Be(admiral.Id);
        admiral.Fleet.Should().Be(fleet);
    }

    [Fact]
    public void SetAdmiral_Null_ClearsAdmiral()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var admiral = CreateTestHero();
        _editor.SetAdmiral(fleet, admiral);

        // Act
        _editor.SetAdmiral(fleet, null);

        // Assert
        fleet.Admiral.Should().BeNull();
        fleet.AdmiralId.Should().BeNull();
    }

    #endregion

    #region Ship Management Tests

    [Fact]
    public void CreateShip_CreatesValidShip()
    {
        // Act
        var ship = _editor.CreateShip("Sea Wolf", ShipType.Warship);

        // Assert
        ship.Should().NotBeNull();
        ship.Name.Should().Be("Sea Wolf");
        ship.Type.Should().Be(ShipType.Warship);
        ship.CurrentHullPoints.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AddShipToFleet_AddsShip()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var ship = CreateTestShip();

        // Act
        _editor.AddShipToFleet(fleet, ship);

        // Assert
        fleet.Ships.Should().Contain(ship);
        ship.Fleet.Should().Be(fleet);
        ship.FleetId.Should().Be(fleet.Id);
    }

    [Fact]
    public void AddShipToFleet_FirstShip_SetAsFlagship()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var ship = CreateTestShip();

        // Act
        _editor.AddShipToFleet(fleet, ship);

        // Assert
        fleet.Flagship.Should().Be(ship);
    }

    [Fact]
    public void RemoveShipFromFleet_RemovesShip()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var ship = CreateTestShip();
        _editor.AddShipToFleet(fleet, ship);

        // Act
        _editor.RemoveShipFromFleet(fleet, ship);

        // Assert
        fleet.Ships.Should().NotContain(ship);
        ship.Fleet.Should().BeNull();
    }

    [Fact]
    public void SetFlagship_SetsFlagship()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var ship1 = CreateTestShip();
        var ship2 = _editor.CreateShip("Ship 2", ShipType.Galley);
        _editor.AddShipToFleet(fleet, ship1);
        _editor.AddShipToFleet(fleet, ship2);

        // Act
        _editor.SetFlagship(fleet, ship2);

        // Assert
        fleet.Flagship.Should().Be(ship2);
        ship2.ShipClass.Should().Be(ShipClass.Flagship);
    }

    #endregion

    #region Fleet State Tests

    [Fact]
    public void SetFleetState_SetsState()
    {
        // Arrange
        var fleet = CreateTestFleet();

        // Act
        _editor.SetFleetState(fleet, FleetState.Sailing);

        // Assert
        fleet.State.Should().Be(FleetState.Sailing);
    }

    [Fact]
    public void SetFleetFormation_SetsFormation()
    {
        // Arrange
        var fleet = CreateTestFleet();

        // Act
        _editor.SetFleetFormation(fleet, FleetFormation.Wedge);

        // Assert
        fleet.Formation.Should().Be(FleetFormation.Wedge);
    }

    [Fact]
    public void SetFleetMorale_ClampsToBounds()
    {
        // Arrange
        var fleet = CreateTestFleet();

        // Act
        _editor.SetFleetMorale(fleet, 150);

        // Assert
        fleet.Morale.Should().Be(100);
    }

    [Fact]
    public void SetFleetGold_SetsGold()
    {
        // Arrange
        var fleet = CreateTestFleet();

        // Act
        _editor.SetFleetGold(fleet, 50000);

        // Assert
        fleet.Gold.Should().Be(50000);
    }

    [Fact]
    public void SetFleetPosition_SetsPosition()
    {
        // Arrange
        var fleet = CreateTestFleet();

        // Act
        _editor.SetFleetPosition(fleet, 100.5f, 200.5f, 45f);

        // Assert
        fleet.Position.X.Should().BeApproximately(100.5f, 0.01f);
        fleet.Position.Y.Should().BeApproximately(200.5f, 0.01f);
        fleet.Position.Heading.Should().BeApproximately(45f, 0.01f);
    }

    #endregion

    #region Ship Stats Tests

    [Fact]
    public void SetShipHull_SetsHullPoints()
    {
        // Arrange
        var ship = CreateTestShip();

        // Act
        _editor.SetShipHull(ship, 500);

        // Assert
        ship.CurrentHullPoints.Should().BeLessOrEqualTo(ship.MaxHullPoints);
    }

    [Fact]
    public void RepairShip_RestoresFullHealth()
    {
        // Arrange
        var ship = CreateTestShip();
        ship.CurrentHullPoints = 50;

        // Act
        _editor.RepairShip(ship);

        // Assert
        ship.CurrentHullPoints.Should().Be(ship.MaxHullPoints);
    }

    [Fact]
    public void RepairFleet_RepairsAllShips()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var ship1 = CreateTestShip();
        var ship2 = _editor.CreateShip("Ship 2", ShipType.Galley);
        ship1.CurrentHullPoints = 50;
        ship2.CurrentHullPoints = 30;
        _editor.AddShipToFleet(fleet, ship1);
        _editor.AddShipToFleet(fleet, ship2);

        // Act
        _editor.RepairFleet(fleet);

        // Assert
        ship1.CurrentHullPoints.Should().Be(ship1.MaxHullPoints);
        ship2.CurrentHullPoints.Should().Be(ship2.MaxHullPoints);
    }

    [Fact]
    public void SetShipCrew_SetsCrew()
    {
        // Arrange
        var ship = CreateTestShip();

        // Act
        _editor.SetShipCrew(ship, 50);

        // Assert
        ship.CrewCount.Should().Be(50);
    }

    [Fact]
    public void FillCrew_FillsToCapacity()
    {
        // Arrange
        var ship = CreateTestShip();
        ship.CrewCount = 10;

        // Act
        _editor.FillCrew(ship);

        // Assert
        ship.CrewCount.Should().Be(ship.CrewCapacity);
    }

    [Fact]
    public void SetCrewQuality_SetsQuality()
    {
        // Arrange
        var ship = CreateTestShip();

        // Act
        _editor.SetCrewQuality(ship, CrewQuality.Elite);

        // Assert
        ship.CrewQuality.Should().Be(CrewQuality.Elite);
    }

    #endregion

    #region Upgrade Tests

    [Fact]
    public void AddUpgrade_AddsUpgrade()
    {
        // Arrange
        var ship = CreateTestShip();

        // Act
        _editor.AddUpgrade(ship, ShipUpgrade.ReinforcedHull);

        // Assert
        ship.Upgrades.Should().Contain(ShipUpgrade.ReinforcedHull);
    }

    [Fact]
    public void AddUpgrade_Duplicate_DoesNotAddTwice()
    {
        // Arrange
        var ship = CreateTestShip();
        _editor.AddUpgrade(ship, ShipUpgrade.SpeedSails);

        // Act
        _editor.AddUpgrade(ship, ShipUpgrade.SpeedSails);

        // Assert
        ship.Upgrades.Count(u => u == ShipUpgrade.SpeedSails).Should().Be(1);
    }

    [Fact]
    public void RemoveUpgrade_RemovesUpgrade()
    {
        // Arrange
        var ship = CreateTestShip();
        _editor.AddUpgrade(ship, ShipUpgrade.RammingProw);

        // Act
        _editor.RemoveUpgrade(ship, ShipUpgrade.RammingProw);

        // Assert
        ship.Upgrades.Should().NotContain(ShipUpgrade.RammingProw);
    }

    [Fact]
    public void ClearUpgrades_RemovesAllUpgrades()
    {
        // Arrange
        var ship = CreateTestShip();
        _editor.AddUpgrade(ship, ShipUpgrade.ReinforcedHull);
        _editor.AddUpgrade(ship, ShipUpgrade.SpeedSails);

        // Act
        _editor.ClearUpgrades(ship);

        // Assert
        ship.Upgrades.Should().BeEmpty();
    }

    #endregion

    #region Cargo Tests

    [Fact]
    public void AddCargo_AddsCargo()
    {
        // Arrange
        var ship = CreateTestShip();
        var cargo = new CargoItem
        {
            ItemId = "grain",
            ItemName = "Grain",
            Count = 10,
            Weight = 5,
            Value = 50
        };

        // Act
        _editor.AddCargo(ship, cargo);

        // Assert
        ship.Cargo.Should().Contain(c => c.ItemId == "grain" && c.Count == 10);
    }

    [Fact]
    public void AddCargo_ExistingItem_IncrementsCount()
    {
        // Arrange
        var ship = CreateTestShip();
        _editor.AddCargo(ship, new CargoItem { ItemId = "grain", ItemName = "Grain", Count = 10, Weight = 5 });

        // Act
        _editor.AddCargo(ship, new CargoItem { ItemId = "grain", ItemName = "Grain", Count = 5, Weight = 5 });

        // Assert
        ship.Cargo.First(c => c.ItemId == "grain").Count.Should().Be(15);
    }

    [Fact]
    public void RemoveCargo_RemovesCargo()
    {
        // Arrange
        var ship = CreateTestShip();
        _editor.AddCargo(ship, new CargoItem { ItemId = "grain", ItemName = "Grain", Count = 10, Weight = 5 });

        // Act
        _editor.RemoveCargo(ship, "grain", 5);

        // Assert
        ship.Cargo.First(c => c.ItemId == "grain").Count.Should().Be(5);
    }

    [Fact]
    public void RemoveCargo_AllItems_RemovesEntry()
    {
        // Arrange
        var ship = CreateTestShip();
        _editor.AddCargo(ship, new CargoItem { ItemId = "grain", ItemName = "Grain", Count = 10, Weight = 5 });

        // Act
        _editor.RemoveCargo(ship, "grain", 10);

        // Assert
        ship.Cargo.Should().NotContain(c => c.ItemId == "grain");
    }

    [Fact]
    public void ClearCargo_RemovesAllCargo()
    {
        // Arrange
        var ship = CreateTestShip();
        _editor.AddCargo(ship, new CargoItem { ItemId = "grain", ItemName = "Grain", Count = 10, Weight = 5 });
        _editor.AddCargo(ship, new CargoItem { ItemId = "wine", ItemName = "Wine", Count = 5, Weight = 10 });

        // Act
        _editor.ClearCargo(ship);

        // Assert
        ship.Cargo.Should().BeEmpty();
    }

    #endregion

    #region Weapons Tests

    [Fact]
    public void AddWeapon_AddsWeapon()
    {
        // Arrange
        var ship = CreateTestShip();
        var weapon = new ShipWeapon { Type = ShipWeaponType.Ballista, Position = WeaponPosition.Bow };

        // Act
        _editor.AddWeapon(ship, weapon);

        // Assert
        ship.Weapons.Should().Contain(w => w.Type == ShipWeaponType.Ballista);
    }

    [Fact]
    public void ClearWeapons_RemovesAllWeapons()
    {
        // Arrange
        var ship = CreateTestShip();
        _editor.AddWeapon(ship, new ShipWeapon { Type = ShipWeaponType.Ballista });
        _editor.AddWeapon(ship, new ShipWeapon { Type = ShipWeaponType.Catapult });

        // Act
        _editor.ClearWeapons(ship);

        // Assert
        ship.Weapons.Should().BeEmpty();
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public void SetFlagship_Null_ClearsFlagship()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var ship = CreateTestShip();
        _editor.AddShipToFleet(fleet, ship);

        // Act
        _editor.SetFlagship(fleet, null);

        // Assert
        fleet.Flagship.Should().BeNull();
    }

    [Fact]
    public void SetFlagship_ShipNotInFleet_ThrowsException()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var ship = CreateTestShip(); // Not added to fleet

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetFlagship(fleet, ship))
            .Should().Throw<EditorException>();
    }

    [Fact]
    public void SetFleetGold_NegativeValue_ThrowsException()
    {
        // Arrange
        var fleet = CreateTestFleet();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetFleetGold(fleet, -100))
            .Should().Throw<EditorException>();
    }

    [Fact]
    public void SetFleetMorale_NegativeValue_ClampsToZero()
    {
        // Arrange
        var fleet = CreateTestFleet();

        // Act
        _editor.SetFleetMorale(fleet, -50);

        // Assert
        fleet.Morale.Should().Be(0);
    }

    [Fact]
    public void SetFleetFood_SetsFood()
    {
        // Arrange
        var fleet = CreateTestFleet();

        // Act
        _editor.SetFleetFood(fleet, 500f);

        // Assert
        fleet.FoodSupplies.Should().Be(500f);
    }

    [Fact]
    public void SetFleetFood_NegativeValue_ClampsToZero()
    {
        // Arrange
        var fleet = CreateTestFleet();

        // Act
        _editor.SetFleetFood(fleet, -100f);

        // Assert
        fleet.FoodSupplies.Should().Be(0f);
    }

    [Fact]
    public void AddShipToFleet_AlreadyInFleet_DoesNotAddAgain()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var ship = CreateTestShip();
        _editor.AddShipToFleet(fleet, ship);

        // Act
        _editor.AddShipToFleet(fleet, ship);

        // Assert
        fleet.Ships.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveShipFromFleet_NotInFleet_DoesNothing()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var ship = CreateTestShip();

        // Act - should not throw
        _editor.RemoveShipFromFleet(fleet, ship);

        // Assert
        fleet.Ships.Should().BeEmpty();
    }

    [Fact]
    public void RemoveShipFromFleet_Flagship_SelectsNewFlagship()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var ship1 = CreateTestShip();
        var ship2 = _editor.CreateShip("Ship 2", ShipType.Galley);
        _editor.AddShipToFleet(fleet, ship1);
        _editor.AddShipToFleet(fleet, ship2);
        _editor.SetFlagship(fleet, ship1);

        // Act
        _editor.RemoveShipFromFleet(fleet, ship1);

        // Assert
        fleet.Flagship.Should().Be(ship2);
    }

    [Fact]
    public void SetShipHull_NegativeValue_ThrowsException()
    {
        // Arrange
        var ship = CreateTestShip();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetShipHull(ship, -100))
            .Should().Throw<EditorException>();
    }

    [Fact]
    public void SetShipHull_ExceedsMax_ClampsToMax()
    {
        // Arrange
        var ship = CreateTestShip();
        var maxHull = ship.MaxHullPoints;

        // Act
        _editor.SetShipHull(ship, maxHull + 1000);

        // Assert
        ship.CurrentHullPoints.Should().Be(maxHull);
    }

    [Fact]
    public void SetShipCrew_NegativeValue_ThrowsException()
    {
        // Arrange
        var ship = CreateTestShip();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetShipCrew(ship, -10))
            .Should().Throw<EditorException>();
    }

    [Fact]
    public void SetShipCrew_ExceedsCapacity_ThrowsException()
    {
        // Arrange
        var ship = CreateTestShip();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetShipCrew(ship, ship.CrewCapacity + 100))
            .Should().Throw<EditorException>();
    }

    [Fact]
    public void FillFleetCrew_FillsAllShips()
    {
        // Arrange
        var fleet = CreateTestFleet();
        var ship1 = CreateTestShip();
        var ship2 = _editor.CreateShip("Ship 2", ShipType.Galley);
        _editor.AddShipToFleet(fleet, ship1);
        _editor.AddShipToFleet(fleet, ship2);

        // Act
        _editor.FillFleetCrew(fleet);

        // Assert
        ship1.CrewCount.Should().Be(ship1.CrewCapacity);
        ship2.CrewCount.Should().Be(ship2.CrewCapacity);
    }

    [Fact]
    public void SetCrewMorale_ClampsToRange()
    {
        // Arrange
        var ship = CreateTestShip();

        // Act
        _editor.SetCrewMorale(ship, 150f);

        // Assert
        ship.CrewMorale.Should().Be(100f);
    }

    [Fact]
    public void SetCrewMorale_NegativeValue_ClampsToZero()
    {
        // Arrange
        var ship = CreateTestShip();

        // Act
        _editor.SetCrewMorale(ship, -50f);

        // Assert
        ship.CrewMorale.Should().Be(0f);
    }

    [Fact]
    public void CreateFleet_WithAdmiralAndNoClan_SetsBothCorrectly()
    {
        // Arrange
        var admiral = CreateTestHero();

        // Act
        var fleet = _editor.CreateFleet("Admiral Fleet", admiral, null);

        // Assert
        fleet.Admiral.Should().Be(admiral);
        fleet.Clan.Should().BeNull();
    }

    [Fact]
    public void CreateShip_AllTypes_CreatesValidShips()
    {
        // Test all ship types
        var types = new[] { ShipType.Snekkja, ShipType.Knarr, ShipType.Cog, ShipType.Longship, 
                           ShipType.Galley, ShipType.Warship, ShipType.Carrack, ShipType.ManOfWar };

        foreach (var type in types)
        {
            // Act
            var ship = _editor.CreateShip($"Test {type}", type);

            // Assert
            ship.Should().NotBeNull();
            ship.Type.Should().Be(type);
            ship.CurrentHullPoints.Should().BeGreaterThan(0);
        }
    }

    #endregion
}
