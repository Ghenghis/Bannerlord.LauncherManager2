// <copyright file="PartyEditorTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.Services;
using FluentAssertions;
using Xunit;

public class PartyEditorTests
{
    private readonly PartyEditor _editor;

    public PartyEditorTests()
    {
        _editor = new PartyEditor();
    }

    private PartyData CreateTestParty()
    {
        return new PartyData
        {
            Id = MBGUID.Generate(MBGUIDType.Party),
            Name = "Test Party",
            Type = PartyType.Lord,
            State = PartyState.Active,
            Gold = 1000,
            Food = 50,
            Morale = 50,
            PartySizeLimit = 100,
            PrisonerLimit = 50,
            Position = new Vec2(100, 100)
        };
    }

    #region Troop Operations

    [Fact]
    public void AddTroops_AddsNewTroopStack()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.AddTroops(party, "imperial_infantry", "Imperial Infantry", 10, 3);

        // Assert
        party.Troops.Should().HaveCount(1);
        party.Troops[0].TroopId.Should().Be("imperial_infantry");
        party.Troops[0].Count.Should().Be(10);
        party.Troops[0].Tier.Should().Be(3);
    }

    [Fact]
    public void AddTroops_ExistingTroop_IncrementsCount()
    {
        // Arrange
        var party = CreateTestParty();
        _editor.AddTroops(party, "imperial_infantry", "Imperial Infantry", 10, 3);

        // Act
        _editor.AddTroops(party, "imperial_infantry", "Imperial Infantry", 5, 3);

        // Assert
        party.Troops.Should().HaveCount(1);
        party.Troops[0].Count.Should().Be(15);
    }


    [Fact]
    public void AddTroops_NegativeCount_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();

        // Act & Assert
        FluentActions.Invoking(() => _editor.AddTroops(party, "test", "Test", -5, 1))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RemoveTroops_RemovesFromExistingStack()
    {
        // Arrange
        var party = CreateTestParty();
        _editor.AddTroops(party, "imperial_infantry", "Imperial Infantry", 10, 3);

        // Act
        var removed = _editor.RemoveTroops(party, "imperial_infantry", 5);

        // Assert
        removed.Should().Be(5);
        party.Troops[0].Count.Should().Be(5);
    }

    [Fact]
    public void RemoveTroops_AllTroops_RemovesStack()
    {
        // Arrange
        var party = CreateTestParty();
        _editor.AddTroops(party, "imperial_infantry", "Imperial Infantry", 10, 3);

        // Act
        var removed = _editor.RemoveTroops(party, "imperial_infantry");

        // Assert
        removed.Should().Be(10);
        party.Troops.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTroops_NonExistentTroop_ReturnsZero()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        var removed = _editor.RemoveTroops(party, "nonexistent");

        // Assert
        removed.Should().Be(0);
    }

    [Fact]
    public void SetTroopCount_UpdatesCount()
    {
        // Arrange
        var party = CreateTestParty();
        _editor.AddTroops(party, "imperial_infantry", "Imperial Infantry", 10, 3);

        // Act
        _editor.SetTroopCount(party, "imperial_infantry", 25);

        // Assert
        party.Troops[0].Count.Should().Be(25);
    }

    [Fact]
    public void SetTroopCount_ToZero_RemovesStack()
    {
        // Arrange
        var party = CreateTestParty();
        _editor.AddTroops(party, "imperial_infantry", "Imperial Infantry", 10, 3);

        // Act
        _editor.SetTroopCount(party, "imperial_infantry", 0);

        // Assert
        party.Troops.Should().BeEmpty();
    }

    [Fact]
    public void HealTroops_HealsAllWounded()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack
        {
            TroopId = "test",
            TroopName = "Test",
            Count = 10,
            WoundedCount = 5
        });

        // Act
        _editor.HealTroops(party);

        // Assert
        party.Troops[0].WoundedCount.Should().Be(0);
    }

    [Fact]
    public void HealTroops_SpecificTroop_HealsOnlyThat()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack { TroopId = "troop1", TroopName = "Troop 1", Count = 10, WoundedCount = 5 });
        party.Troops.Add(new TroopStack { TroopId = "troop2", TroopName = "Troop 2", Count = 10, WoundedCount = 3 });

        // Act
        _editor.HealTroops(party, "troop1");

        // Assert
        party.Troops[0].WoundedCount.Should().Be(0);
        party.Troops[1].WoundedCount.Should().Be(3);
    }

    [Fact]
    public void UpgradeTroops_TransfersToNewStack()
    {
        // Arrange
        var party = CreateTestParty();
        _editor.AddTroops(party, "recruit", "Recruit", 10, 1);

        // Act
        _editor.UpgradeTroops(party, "recruit", "infantry", "Infantry", 5, 2);

        // Assert
        party.Troops.Should().HaveCount(2);
        party.Troops.Should().Contain(t => t.TroopId == "recruit" && t.Count == 5);
        party.Troops.Should().Contain(t => t.TroopId == "infantry" && t.Count == 5);
    }

    #endregion

    #region Prisoner Operations

    [Fact]
    public void AddPrisoners_AddsNewPrisonerStack()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.AddPrisoners(party, "bandit", "Bandit", 5, 1);

        // Assert
        party.Prisoners.Should().HaveCount(1);
        party.Prisoners[0].TroopId.Should().Be("bandit");
        party.Prisoners[0].Count.Should().Be(5);
    }

    [Fact]
    public void ReleasePrisoners_ReleasesAll()
    {
        // Arrange
        var party = CreateTestParty();
        _editor.AddPrisoners(party, "bandit1", "Bandit 1", 5, 1);
        _editor.AddPrisoners(party, "bandit2", "Bandit 2", 3, 1);

        // Act
        var released = _editor.ReleasePrisoners(party);

        // Assert
        released.Should().Be(8);
        party.Prisoners.Should().BeEmpty();
    }

    [Fact]
    public void RecruitPrisoners_ConvertsToPrisoners()
    {
        // Arrange
        var party = CreateTestParty();
        _editor.AddPrisoners(party, "looter", "Looter", 10, 1);

        // Act
        _editor.RecruitPrisoners(party, "looter", 5);

        // Assert
        party.Prisoners[0].Count.Should().Be(5);
        party.Troops.Should().Contain(t => t.TroopId == "looter" && t.Count == 5);
    }

    #endregion


    #region Party Resources

    [Fact]
    public void SetGold_SetsValue()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetGold(party, 50000);

        // Assert
        party.Gold.Should().Be(50000);
    }

    [Fact]
    public void SetGold_NegativeValue_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetGold(party, -100))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddGold_AddsToExisting()
    {
        // Arrange
        var party = CreateTestParty();
        party.Gold = 1000;

        // Act
        _editor.AddGold(party, 500);

        // Assert
        party.Gold.Should().Be(1500);
    }

    [Fact]
    public void AddGold_NegativeAmount_SubtractsGold()
    {
        // Arrange
        var party = CreateTestParty();
        party.Gold = 1000;

        // Act
        _editor.AddGold(party, -300);

        // Assert
        party.Gold.Should().Be(700);
    }

    [Fact]
    public void AddGold_ClampsToZero()
    {
        // Arrange
        var party = CreateTestParty();
        party.Gold = 100;

        // Act
        _editor.AddGold(party, -500);

        // Assert
        party.Gold.Should().Be(0);
    }

    [Fact]
    public void SetFood_SetsValue()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetFood(party, 75.5f);

        // Assert
        party.Food.Should().BeApproximately(75.5f, 0.01f);
    }


    [Fact]
    public void SetFood_NegativeValue_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetFood(party, -10f))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetMorale_SetsValue()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetMorale(party, 85f);

        // Assert
        party.Morale.Should().BeApproximately(85f, 0.01f);
    }

    [Fact]
    public void SetMorale_ClampsToRange()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetMorale(party, 150f);

        // Assert
        party.Morale.Should().Be(100f);
    }

    [Fact]
    public void SetMorale_ClampsToZero()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetMorale(party, -20f);

        // Assert
        party.Morale.Should().Be(0f);
    }

    [Fact]
    public void SetPartySizeLimit_SetsValue()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetPartySizeLimit(party, 200);

        // Assert
        party.PartySizeLimit.Should().Be(200);
    }

    [Fact]
    public void SetPartySizeLimit_ZeroOrNegative_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetPartySizeLimit(party, 0))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetPrisonerLimit_SetsValue()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetPrisonerLimit(party, 50);

        // Assert
        party.PrisonerLimit.Should().Be(50);
    }

    [Fact]
    public void SetPrisonerLimit_Negative_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetPrisonerLimit(party, -1))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Party Movement

    [Fact]
    public void MoveTo_SetsPosition()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.MoveTo(party, 150.5f, 250.75f);

        // Assert
        party.Position.X.Should().BeApproximately(150.5f, 0.01f);
        party.Position.Y.Should().BeApproximately(250.75f, 0.01f);
    }


    [Fact]
    public void SetState_SetsValue()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetState(party, PartyState.Active);

        // Assert
        party.State.Should().Be(PartyState.Active);
    }

    [Fact]
    public void TeleportToSettlement_SetsPositionAndState()
    {
        // Arrange
        var party = CreateTestParty();
        var settlement = new SettlementData
        {
            Id = MBGUID.Generate(MBGUIDType.Settlement),
            Name = "Test Town",
            Position = new Vec2(500f, 600f)
        };

        // Act
        _editor.TeleportToSettlement(party, settlement);

        // Assert
        party.Position.X.Should().Be(500f);
        party.Position.Y.Should().Be(600f);
        party.CurrentSettlementId.Should().Be(settlement.Id);
        party.State.Should().Be(PartyState.InSettlement);
    }

    #endregion

    #region Party Statistics

    [Fact]
    public void GetPartyStatistics_ReturnsCorrectTotals()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack { TroopId = "t1", TroopName = "T1", Count = 10, WoundedCount = 2, Tier = 1 });
        party.Troops.Add(new TroopStack { TroopId = "t2", TroopName = "T2", Count = 5, WoundedCount = 0, Tier = 3 });
        party.Prisoners.Add(new TroopStack { TroopId = "p1", TroopName = "P1", Count = 3, Tier = 1 });

        // Act
        var stats = _editor.GetPartyStatistics(party);

        // Assert
        stats.TotalTroops.Should().Be(15);
        stats.WoundedTroops.Should().Be(2);
        stats.HealthyTroops.Should().Be(13);
        stats.TotalPrisoners.Should().Be(3);
        stats.UniqueUnits.Should().Be(2);
    }

    [Fact]
    public void GetPartyStatistics_CalculatesTroopsByTier()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack { TroopId = "t1", TroopName = "T1", Count = 10, Tier = 1 });
        party.Troops.Add(new TroopStack { TroopId = "t2", TroopName = "T2", Count = 5, Tier = 3 });
        party.Troops.Add(new TroopStack { TroopId = "t3", TroopName = "T3", Count = 8, Tier = 1 });

        // Act
        var stats = _editor.GetPartyStatistics(party);

        // Assert
        stats.TroopsByTier.Should().ContainKey(1).WhoseValue.Should().Be(18);
        stats.TroopsByTier.Should().ContainKey(3).WhoseValue.Should().Be(5);
    }

    [Fact]
    public void GetPartyStatistics_DetectsOverLimit()
    {
        // Arrange
        var party = CreateTestParty();
        party.PartySizeLimit = 10;
        party.PrisonerLimit = 5;
        party.Troops.Add(new TroopStack { TroopId = "t1", TroopName = "T1", Count = 20, Tier = 1 });
        party.Prisoners.Add(new TroopStack { TroopId = "p1", TroopName = "P1", Count = 10, Tier = 1 });

        // Act
        var stats = _editor.GetPartyStatistics(party);

        // Assert
        stats.IsOverPartyLimit.Should().BeTrue();
        stats.IsOverPrisonerLimit.Should().BeTrue();
    }

    [Fact]
    public void GetPartyStatistics_EmptyParty_ReturnsZeroStats()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        var stats = _editor.GetPartyStatistics(party);

        // Assert
        stats.TotalTroops.Should().Be(0);
        stats.AverageTier.Should().Be(0);
        stats.UniqueUnits.Should().Be(0);
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public void SetWoundedCount_ClampsToMax()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack { TroopId = "test", TroopName = "Test", Count = 10, WoundedCount = 0 });

        // Act
        _editor.SetWoundedCount(party, "test", 20); // More than total count

        // Assert
        party.Troops[0].WoundedCount.Should().Be(10); // Clamped to count
    }

    [Fact]
    public void SetWoundedCount_ClampsToZero()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack { TroopId = "test", TroopName = "Test", Count = 10, WoundedCount = 5 });

        // Act
        _editor.SetWoundedCount(party, "test", -5);

        // Assert
        party.Troops[0].WoundedCount.Should().Be(0);
    }

    [Fact]
    public void SetWoundedCount_NonExistentTroop_DoesNothing()
    {
        // Arrange
        var party = CreateTestParty();

        // Act - should not throw
        _editor.SetWoundedCount(party, "nonexistent", 5);

        // Assert
        party.Troops.Should().BeEmpty();
    }

    [Fact]
    public void SetTroopCount_NonExistentTroop_Positive_LogsWarning()
    {
        // Arrange
        var party = CreateTestParty();

        // Act - should not throw, just log warning
        _editor.SetTroopCount(party, "nonexistent", 10);

        // Assert - no troop added
        party.Troops.Should().BeEmpty();
    }

    [Fact]
    public void SetTroopCount_NegativeValue_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetTroopCount(party, "test", -5))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpgradeTroops_NotEnoughTroops_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();
        _editor.AddTroops(party, "recruit", "Recruit", 5, 1);

        // Act & Assert
        FluentActions.Invoking(() => _editor.UpgradeTroops(party, "recruit", "infantry", "Infantry", 10, 2))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpgradeTroops_NonExistentTroop_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();

        // Act & Assert
        FluentActions.Invoking(() => _editor.UpgradeTroops(party, "nonexistent", "infantry", "Infantry", 5, 2))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddPrisoners_ExistingPrisoner_IncrementsCount()
    {
        // Arrange
        var party = CreateTestParty();
        _editor.AddPrisoners(party, "bandit", "Bandit", 5, 1);

        // Act
        _editor.AddPrisoners(party, "bandit", "Bandit", 3, 1);

        // Assert
        party.Prisoners.Should().HaveCount(1);
        party.Prisoners[0].Count.Should().Be(8);
    }

    [Fact]
    public void AddPrisoners_NegativeCount_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();

        // Act & Assert
        FluentActions.Invoking(() => _editor.AddPrisoners(party, "bandit", "Bandit", -5, 1))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddPrisoners_WithHeroId_SetsIsHero()
    {
        // Arrange
        var party = CreateTestParty();
        var heroId = MBGUID.Generate(MBGUIDType.Hero);

        // Act
        _editor.AddPrisoners(party, "lord", "Lord Prisoner", 1, 5, heroId);

        // Assert
        party.Prisoners[0].IsHero.Should().BeTrue();
        party.Prisoners[0].HeroId.Should().Be(heroId);
    }

    [Fact]
    public void ReleasePrisoners_SpecificTroop_PartialRelease()
    {
        // Arrange
        var party = CreateTestParty();
        _editor.AddPrisoners(party, "bandit", "Bandit", 10, 1);

        // Act
        var released = _editor.ReleasePrisoners(party, "bandit", 5);

        // Assert
        released.Should().Be(5);
        party.Prisoners[0].Count.Should().Be(5);
    }

    [Fact]
    public void ReleasePrisoners_NonExistentTroop_ReturnsZero()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        var released = _editor.ReleasePrisoners(party, "nonexistent", 5);

        // Assert
        released.Should().Be(0);
    }

    [Fact]
    public void RecruitPrisoners_NotEnoughPrisoners_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();
        _editor.AddPrisoners(party, "looter", "Looter", 5, 1);

        // Act & Assert
        FluentActions.Invoking(() => _editor.RecruitPrisoners(party, "looter", 10))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RecruitPrisoners_NonExistentPrisoner_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();

        // Act & Assert
        FluentActions.Invoking(() => _editor.RecruitPrisoners(party, "nonexistent", 5))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveTroops_PartialRemove_AdjustsWounded()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack 
        { 
            TroopId = "test", 
            TroopName = "Test", 
            Count = 10, 
            WoundedCount = 8 
        });

        // Act
        _editor.RemoveTroops(party, "test", 5);

        // Assert
        party.Troops[0].Count.Should().Be(5);
        party.Troops[0].WoundedCount.Should().Be(5); // Adjusted to match count
    }

    [Fact]
    public void SetTroopCount_ReducesCount_AdjustsWounded()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack 
        { 
            TroopId = "test", 
            TroopName = "Test", 
            Count = 10, 
            WoundedCount = 8 
        });

        // Act
        _editor.SetTroopCount(party, "test", 5);

        // Assert
        party.Troops[0].Count.Should().Be(5);
        party.Troops[0].WoundedCount.Should().Be(5); // Adjusted to match new count
    }

    [Fact]
    public void AddTroops_ZeroCount_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();

        // Act & Assert
        FluentActions.Invoking(() => _editor.AddTroops(party, "test", "Test", 0, 1))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddPrisoners_ZeroCount_ThrowsException()
    {
        // Arrange
        var party = CreateTestParty();

        // Act & Assert
        FluentActions.Invoking(() => _editor.AddPrisoners(party, "test", "Test", 0, 1))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Comprehensive Troop Tier Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void AddTroops_AllTiers_AddsCorrectly(int tier)
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.AddTroops(party, $"troop_t{tier}", $"Troop Tier {tier}", 10, tier);

        // Assert
        party.Troops.Should().Contain(t => t.TroopId == $"troop_t{tier}" && t.Tier == tier);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void AddPrisoners_AllTiers_AddsCorrectly(int tier)
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.AddPrisoners(party, $"prisoner_t{tier}", $"Prisoner Tier {tier}", 5, tier);

        // Assert
        party.Prisoners.Should().Contain(p => p.TroopId == $"prisoner_t{tier}" && p.Tier == tier);
    }

    #endregion

    #region Comprehensive Gold Tests

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    [InlineData(100000)]
    public void SetGold_ValidAmounts_SetsCorrectly(int gold)
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetGold(party, gold);

        // Assert
        party.Gold.Should().Be(gold);
    }

    [Fact]
    public void AddGold_MultipleAdditions_AccumulatesCorrectly()
    {
        // Arrange
        var party = CreateTestParty();
        party.Gold = 1000;

        // Act
        _editor.AddGold(party, 500);
        _editor.AddGold(party, 300);
        _editor.AddGold(party, 200);

        // Assert
        party.Gold.Should().Be(2000);
    }

    [Fact]
    public void AddGold_NegativeAmount_ReducesGold()
    {
        // Arrange
        var party = CreateTestParty();
        party.Gold = 1000;

        // Act
        _editor.AddGold(party, -500);

        // Assert
        party.Gold.Should().Be(500);
    }

    #endregion


    #region Comprehensive Morale Tests

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(100)]
    public void SetMorale_ValidValues_SetsCorrectly(float morale)
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetMorale(party, morale);

        // Assert
        party.Morale.Should().Be(morale);
    }

    [Fact]
    public void SetMorale_AboveMax_ClampsTo100()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetMorale(party, 150);

        // Assert
        party.Morale.Should().Be(100);
    }

    [Fact]
    public void SetMorale_BelowMin_ClampsToZero()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetMorale(party, -50);

        // Assert
        party.Morale.Should().Be(0);
    }

    #endregion

    #region Comprehensive Troop Count Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void AddTroops_VariousCounts_AddsCorrectly(int count)
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.AddTroops(party, "test", "Test", count, 1);

        // Assert
        party.Troops.Should().Contain(t => t.TroopId == "test" && t.Count == count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void AddPrisoners_VariousCounts_AddsCorrectly(int count)
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.AddPrisoners(party, "test", "Test", count, 1);

        // Assert
        party.Prisoners.Should().Contain(p => p.TroopId == "test" && p.Count == count);
    }

    #endregion

    #region Comprehensive Remove Tests

    [Fact]
    public void RemoveTroops_ExactCount_RemovesStack()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack { TroopId = "test", TroopName = "Test", Count = 10 });

        // Act
        _editor.RemoveTroops(party, "test", 10);

        // Assert
        party.Troops.Should().NotContain(t => t.TroopId == "test");
    }

    [Fact]
    public void RemoveTroops_MoreThanExists_RemovesStack()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack { TroopId = "test", TroopName = "Test", Count = 10 });

        // Act
        _editor.RemoveTroops(party, "test", 100);

        // Assert
        party.Troops.Should().NotContain(t => t.TroopId == "test");
    }


    #endregion

    #region Comprehensive Heal Tests

    [Fact]
    public void HealTroops_AllWounded_HealsAll()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack { TroopId = "t1", TroopName = "T1", Count = 10, WoundedCount = 5 });
        party.Troops.Add(new TroopStack { TroopId = "t2", TroopName = "T2", Count = 8, WoundedCount = 3 });

        // Act
        _editor.HealTroops(party);

        // Assert
        party.Troops.All(t => t.WoundedCount == 0).Should().BeTrue();
    }


    #endregion

    #region Comprehensive Recruit Prisoners Tests

    [Fact]
    public void RecruitPrisoners_ValidAmount_ConvertsToTroops()
    {
        // Arrange
        var party = CreateTestParty();
        party.Prisoners.Add(new TroopStack { TroopId = "prisoner", TroopName = "Prisoner", Count = 10, Tier = 2 });

        // Act
        _editor.RecruitPrisoners(party, "prisoner", 5);

        // Assert
        party.Prisoners.First(p => p.TroopId == "prisoner").Count.Should().Be(5);
        party.Troops.Should().Contain(t => t.TroopId == "prisoner" && t.Count == 5);
    }

    [Fact]
    public void RecruitPrisoners_AllPrisoners_RemovesPrisonerStack()
    {
        // Arrange
        var party = CreateTestParty();
        party.Prisoners.Add(new TroopStack { TroopId = "prisoner", TroopName = "Prisoner", Count = 5, Tier = 2 });

        // Act
        _editor.RecruitPrisoners(party, "prisoner", 5);

        // Assert
        party.Prisoners.Should().NotContain(p => p.TroopId == "prisoner");
        party.Troops.Should().Contain(t => t.TroopId == "prisoner" && t.Count == 5);
    }

    [Fact]
    public void RecruitPrisoners_ToExistingTroop_MergesStack()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack { TroopId = "unit", TroopName = "Unit", Count = 10, Tier = 2 });
        party.Prisoners.Add(new TroopStack { TroopId = "unit", TroopName = "Unit", Count = 5, Tier = 2 });

        // Act
        _editor.RecruitPrisoners(party, "unit", 5);

        // Assert
        party.Troops.First(t => t.TroopId == "unit").Count.Should().Be(15);
        party.Prisoners.Should().NotContain(p => p.TroopId == "unit");
    }

    #endregion

    #region Party Limit Tests

    [Fact]
    public void SetPartySizeLimit_ValidValue_SetsLimit()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetPartySizeLimit(party, 150);

        // Assert
        party.PartySizeLimit.Should().Be(150);
    }

    [Fact]
    public void SetPrisonerLimit_ValidValue_SetsLimit()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetPrisonerLimit(party, 50);

        // Assert
        party.PrisonerLimit.Should().Be(50);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(500)]
    public void SetPartySizeLimit_VariousValues_SetsCorrectly(int limit)
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.SetPartySizeLimit(party, limit);

        // Assert
        party.PartySizeLimit.Should().Be(limit);
    }

    #endregion

    #region Multiple Stack Tests

    [Fact]
    public void AddTroops_MultipleStacks_AddsAllCorrectly()
    {
        // Arrange
        var party = CreateTestParty();

        // Act
        _editor.AddTroops(party, "infantry", "Infantry", 20, 1);
        _editor.AddTroops(party, "cavalry", "Cavalry", 10, 3);
        _editor.AddTroops(party, "archers", "Archers", 15, 2);

        // Assert
        party.Troops.Count.Should().Be(3);
        party.Troops.Sum(t => t.Count).Should().Be(45);
    }

    [Fact]
    public void RemoveTroops_NonExistentTroop_NoChange()
    {
        // Arrange
        var party = CreateTestParty();
        party.Troops.Add(new TroopStack { TroopId = "test", TroopName = "Test", Count = 10 });

        // Act - should not throw
        _editor.RemoveTroops(party, "nonexistent", 5);

        // Assert
        party.Troops.Count.Should().Be(1);
    }

    #endregion
}
