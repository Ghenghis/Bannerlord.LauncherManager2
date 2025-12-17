// <copyright file="EdgeCaseTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.WarSails;
using FluentAssertions;
using Xunit;

public class EdgeCaseTests
{
    #region MBGUID Edge Cases

    [Fact]
    public void MBGUID_Empty_IsEmpty()
    {
        // Assert
        MBGUID.Empty.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void MBGUID_Generated_IsNotEmpty()
    {
        // Act
        var guid = MBGUID.Generate(MBGUIDType.Hero);

        // Assert
        guid.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void MBGUID_TwoGeneratedGuids_AreDifferent()
    {
        // Act
        var guid1 = MBGUID.Generate(MBGUIDType.Hero);
        var guid2 = MBGUID.Generate(MBGUIDType.Hero);

        // Assert
        guid1.Should().NotBe(guid2);
    }

    #endregion

    #region CampaignTime Edge Cases

    [Fact]
    public void CampaignTime_NegativeTicks_Handled()
    {
        // Arrange - negative ticks could represent time before start
        var time = new CampaignTime(-1000L);

        // Assert - should not throw
        time.Ticks.Should().Be(-1000L);
    }

    [Fact]
    public void CampaignTime_VeryLargeTicks_Handled()
    {
        // Arrange
        var time = new CampaignTime(long.MaxValue / 2);

        // Assert - should not throw
        time.Ticks.Should().BeGreaterThan(0);
    }

    #endregion

    #region Collection Edge Cases

    [Fact]
    public void HeroData_EmptyPerks_CanBeCleared()
    {
        // Arrange
        var hero = new HeroData();

        // Act
        hero.UnlockedPerks.Clear();

        // Assert
        hero.UnlockedPerks.Should().BeEmpty();
    }

    [Fact]
    public void FleetData_EmptyShips_TotalCrewIsZero()
    {
        // Arrange
        var fleet = new FleetData();
        fleet.Ships.Clear();

        // Assert
        fleet.TotalCrewCount.Should().Be(0);
    }

    [Fact]
    public void CampaignData_AllCollections_CanBeCleared()
    {
        // Arrange
        var campaign = new CampaignData();
        campaign.Heroes.Add(new HeroData());
        campaign.Parties.Add(new PartyData());

        // Act
        campaign.Heroes.Clear();
        campaign.Parties.Clear();

        // Assert
        campaign.Heroes.Should().BeEmpty();
        campaign.Parties.Should().BeEmpty();
    }

    #endregion

    #region Null Reference Edge Cases

    [Fact]
    public void HeroData_NullClan_IsNull()
    {
        // Arrange
        var hero = new HeroData();

        // Assert
        hero.Clan.Should().BeNull();
        hero.ClanId.Should().BeNull();
    }

    [Fact]
    public void HeroData_NullParty_IsNull()
    {
        // Arrange
        var hero = new HeroData();

        // Assert
        hero.Party.Should().BeNull();
        hero.PartyId.Should().BeNull();
    }

    [Fact]
    public void FleetData_NullAdmiral_IsNull()
    {
        // Arrange
        var fleet = new FleetData();

        // Assert
        fleet.Admiral.Should().BeNull();
        fleet.AdmiralId.Should().BeNull();
    }

    [Fact]
    public void QuestData_NullQuestGiver_IsNull()
    {
        // Arrange
        var quest = new QuestData();

        // Assert
        quest.QuestGiver.Should().BeNull();
    }

    #endregion

    #region String Edge Cases

    [Fact]
    public void HeroData_EmptyName_IsValid()
    {
        // Arrange
        var hero = new HeroData { Name = string.Empty };

        // Assert
        hero.Name.Should().BeEmpty();
    }

    [Fact]
    public void HeroData_WhitespaceName_IsValid()
    {
        // Arrange
        var hero = new HeroData { Name = "   " };

        // Assert
        hero.Name.Should().Be("   ");
    }

    [Fact]
    public void HeroData_UnicodeNameCharacters_IsValid()
    {
        // Arrange
        var hero = new HeroData { Name = "日本語テスト" };

        // Assert
        hero.Name.Should().Be("日本語テスト");
    }

    #endregion

    #region Numeric Edge Cases

    [Fact]
    public void HeroData_ZeroAge_IsValid()
    {
        // Arrange
        var hero = new HeroData { Age = 0 };

        // Assert
        hero.Age.Should().Be(0);
    }

    [Fact]
    public void HeroData_NegativeExperience_IsValid()
    {
        // Arrange - edge case, might not be realistic
        var hero = new HeroData { Experience = -100 };

        // Assert
        hero.Experience.Should().Be(-100);
    }

    [Fact]
    public void SettlementData_MaxProsperity_IsValid()
    {
        // Arrange
        var settlement = new SettlementData { Prosperity = int.MaxValue };

        // Assert
        settlement.Prosperity.Should().Be(int.MaxValue);
    }

    [Fact]
    public void WorkshopData_NegativeCapital_IsValid()
    {
        // Arrange - edge case for debt
        var workshop = new WorkshopData { Capital = -1000 };

        // Assert
        workshop.Capital.Should().Be(-1000);
    }

    #endregion

    #region Enum Edge Cases

    [Fact]
    public void HeroState_AllValues_AreDistinct()
    {
        // Assert
        var values = Enum.GetValues<HeroState>();
        values.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void QuestState_AllValues_AreDistinct()
    {
        // Assert
        var values = Enum.GetValues<QuestState>();
        values.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void SettlementType_AllValues_AreDistinct()
    {
        // Assert
        var values = Enum.GetValues<SettlementType>();
        values.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Gender_AllValues_AreDistinct()
    {
        // Assert
        var values = Enum.GetValues<Gender>();
        values.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Position Edge Cases

    [Fact]
    public void Vec2_NegativeCoordinates_AreValid()
    {
        // Arrange
        var vec = new Vec2 { X = -100f, Y = -200f };

        // Assert
        vec.X.Should().Be(-100f);
        vec.Y.Should().Be(-200f);
    }

    [Fact]
    public void NavalPosition_NegativeCoordinates_AreValid()
    {
        // Arrange
        var pos = new NavalPosition { X = -50f, Y = -75f };

        // Assert
        pos.X.Should().Be(-50f);
        pos.Y.Should().Be(-75f);
    }

    #endregion

    #region Reference Integrity Edge Cases

    [Fact]
    public void HeroData_CanSetAndClearClanId()
    {
        // Arrange
        var hero = new HeroData();
        var clanId = MBGUID.Generate(MBGUIDType.Clan);

        // Act
        hero.ClanId = clanId;
        hero.ClanId = null;

        // Assert
        hero.ClanId.Should().BeNull();
    }

    [Fact]
    public void FleetData_CanSetAndClearFlagshipId()
    {
        // Arrange
        var fleet = new FleetData();
        var shipId = MBGUID.Generate(MBGUIDType.Hero);

        // Act
        fleet.FlagshipId = shipId;
        fleet.FlagshipId = null;

        // Assert
        fleet.FlagshipId.Should().BeNull();
    }

    #endregion
}
