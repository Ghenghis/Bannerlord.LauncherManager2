// <copyright file="ValidationServiceTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.Validation;
using Bannerlord.SaveEditor.Core.WarSails;
using FluentAssertions;
using Xunit;

public class ValidationServiceTests
{
    private readonly ValidationService _service;

    public ValidationServiceTests()
    {
        _service = new ValidationService();
    }

    [Fact]
    public void ValidateHero_ValidHero_ReturnsNoErrors()
    {
        var hero = CreateValidHero();

        var report = _service.ValidateHero(hero);

        report.IsValid.Should().BeTrue();
        report.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateHero_NegativeAttribute_ReturnsError()
    {
        var hero = CreateValidHero();
        hero.Attributes.Vigor = -1;

        var report = _service.ValidateHero(hero);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle(e => e.Code == "HERO_ATTR_001");
    }

    [Fact]
    public void ValidateHero_NegativeSkill_ReturnsError()
    {
        var hero = CreateValidHero();
        hero.Skills.OneHanded = -1;

        var report = _service.ValidateHero(hero);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle(e => e.Code == "HERO_SKILL_001");
    }

    [Fact]
    public void ValidateHero_SkillOver300_ReturnsError()
    {
        var hero = CreateValidHero();
        hero.Skills.OneHanded = 350;

        var report = _service.ValidateHero(hero);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle(e => e.Code == "HERO_SKILL_002");
    }

    [Fact]
    public void ValidateHero_LevelBelowOne_ReturnsError()
    {
        var hero = CreateValidHero();
        hero.Level = 0;

        var report = _service.ValidateHero(hero);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle(e => e.Code == "HERO_LEVEL_001");
    }

    [Fact]
    public void ValidateHero_NegativeGold_ReturnsError()
    {
        var hero = CreateValidHero();
        hero.Gold = -100;

        var report = _service.ValidateHero(hero);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle(e => e.Code == "HERO_GOLD_001");
    }

    [Fact]
    public void ValidateParty_NegativeTroopCount_ReturnsError()
    {
        var party = CreateValidParty();
        party.Troops.Add(new TroopStack { TroopName = "Invalid", Count = -5 });

        var report = _service.ValidateParty(party);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle(e => e.Code == "PARTY_TROOP_001");
    }

    [Fact]
    public void ValidateParty_WoundedExceedsCount_ReturnsError()
    {
        var party = CreateValidParty();
        party.Troops.Add(new TroopStack { TroopName = "Test", Count = 10, WoundedCount = 15 });

        var report = _service.ValidateParty(party);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle(e => e.Code == "PARTY_TROOP_002");
    }

    [Fact]
    public void ValidateFleet_NoShips_ReturnsWarning()
    {
        var fleet = new FleetData
        {
            Id = MBGUID.Generate(MBGUIDType.Fleet),
            Name = "Empty Fleet",
            Morale = 50
        };

        var report = _service.ValidateFleet(fleet);

        report.HasWarnings.Should().BeTrue();
        report.Warnings.Should().ContainSingle(w => w.Code == "FLEET_SHIPS_001");
    }

    [Fact]
    public void ValidateFleet_FlagshipNotInFleet_ReturnsError()
    {
        var fleet = new FleetData
        {
            Id = MBGUID.Generate(MBGUIDType.Fleet),
            Name = "Test Fleet",
            Morale = 50
        };

        var ship = new ShipData
        {
            Id = MBGUID.Generate(MBGUIDType.Ship),
            Name = "Test Ship",
            Type = ShipType.Longship
        };

        // Set flagship without adding to ships list
        fleet.Flagship = ship;

        var report = _service.ValidateFleet(fleet);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainSingle(e => e.Code == "FLEET_FLAG_001");
    }

    private static HeroData CreateValidHero()
    {
        return new HeroData
        {
            Id = MBGUID.Generate(MBGUIDType.Hero),
            Name = "Test Hero",
            HeroId = "test_hero_1",
            Level = 10,
            Age = 25,
            Gold = 1000,
            Health = 100,
            IsAlive = true,
            State = HeroState.Active,
            Attributes = new HeroAttributes
            {
                Vigor = 5,
                Control = 5,
                Endurance = 5,
                Cunning = 5,
                Social = 5,
                Intelligence = 5
            },
            Skills = new SkillSet
            {
                OneHanded = 100,
                TwoHanded = 50
            }
        };
    }

    private static PartyData CreateValidParty()
    {
        return new PartyData
        {
            Id = MBGUID.Generate(MBGUIDType.Party),
            Name = "Test Party",
            Gold = 500,
            Food = 50,
            Morale = 50,
            PartySizeLimit = 100,
            State = PartyState.Active
        };
    }

    #region Additional Edge Case Tests

    [Fact]
    public void ValidateHero_AttributeOver10_InStrictMode_ReturnsWarning()
    {
        var hero = CreateValidHero();
        hero.Attributes.Vigor = 15;
        _service.SetValidationMode(ValidationMode.Strict);

        var report = _service.ValidateHero(hero);

        report.HasWarnings.Should().BeTrue();
    }

    [Fact]
    public void ValidateHero_MultipleErrors_ReturnsAllErrors()
    {
        var hero = CreateValidHero();
        hero.Attributes.Vigor = -1;
        hero.Skills.OneHanded = -1;
        hero.Gold = -100;

        var report = _service.ValidateHero(hero);

        report.IsValid.Should().BeFalse();
        report.Errors.Count.Should().BeGreaterOrEqualTo(3);
    }

    [Fact]
    public void ValidateParty_ValidParty_ReturnsNoErrors()
    {
        var party = CreateValidParty();
        party.Troops.Add(new TroopStack { TroopName = "Soldier", Count = 10, WoundedCount = 2 });

        var report = _service.ValidateParty(party);

        report.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateParty_NegativeGold_ReturnsError()
    {
        var party = CreateValidParty();
        party.Gold = -100;

        var report = _service.ValidateParty(party);

        report.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateParty_NegativeFood_ReturnsError()
    {
        var party = CreateValidParty();
        party.Food = -50;

        var report = _service.ValidateParty(party);

        report.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateParty_MoraleOutOfRange_ReturnsWarning()
    {
        var party = CreateValidParty();
        party.Morale = 150;

        var report = _service.ValidateParty(party);

        report.HasWarnings.Should().BeTrue();
    }

    [Fact]
    public void ValidateFleet_ValidFleet_ReturnsNoErrors()
    {
        var fleet = new FleetData
        {
            Id = MBGUID.Generate(MBGUIDType.Fleet),
            Name = "Valid Fleet",
            Morale = 50
        };

        var ship = new ShipData
        {
            Id = MBGUID.Generate(MBGUIDType.Ship),
            Name = "Test Ship",
            Type = ShipType.Longship
        };

        fleet.Ships.Add(ship);
        fleet.Flagship = ship;

        var report = _service.ValidateFleet(fleet);

        report.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateFleet_NegativeMorale_ReturnsWarning()
    {
        var fleet = new FleetData
        {
            Id = MBGUID.Generate(MBGUIDType.Fleet),
            Name = "Test Fleet",
            Morale = -10
        };

        var report = _service.ValidateFleet(fleet);

        report.HasWarnings.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidSave_ReturnsNoErrors()
    {
        var save = new SaveFile
        {
            FilePath = "test.sav",
            Name = "Test Save",
            Header = new SaveHeader { Version = 7, GameVersion = "v1.2.0" },
            Metadata = new SaveMetadata { CharacterName = "Test", Level = 10 }
        };

        var report = _service.Validate(save);

        report.Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithHeroes_ValidatesAllHeroes()
    {
        var save = new SaveFile
        {
            FilePath = "test.sav",
            Name = "Test Save",
            Header = new SaveHeader { Version = 7, GameVersion = "v1.2.0" },
            Metadata = new SaveMetadata { CharacterName = "Test", Level = 10 }
        };
        save.Heroes.Add(CreateValidHero());

        var report = _service.Validate(save);

        report.Should().NotBeNull();
    }

    [Fact]
    public void Validate_WithParties_ValidatesAllParties()
    {
        var save = new SaveFile
        {
            FilePath = "test.sav",
            Name = "Test Save",
            Header = new SaveHeader { Version = 7, GameVersion = "v1.2.0" },
            Metadata = new SaveMetadata { CharacterName = "Test", Level = 10 }
        };
        save.Parties.Add(CreateValidParty());

        var report = _service.Validate(save);

        report.Should().NotBeNull();
    }

    #endregion

    #region Ship Validation Tests

    [Fact]
    public void ValidateFleet_WithShips_ValidatesEachShip()
    {
        // Arrange
        var fleet = new FleetData
        {
            Id = MBGUID.Generate(MBGUIDType.Fleet),
            Name = "Test Fleet",
            Morale = 50
        };

        var ship1 = new ShipData
        {
            Id = MBGUID.Generate(MBGUIDType.Ship),
            Name = "Ship 1",
            Type = ShipType.Longship,
            CurrentHullPoints = 100,
            CrewCount = 50
        };

        var ship2 = new ShipData
        {
            Id = MBGUID.Generate(MBGUIDType.Ship),
            Name = "Ship 2",
            Type = ShipType.Galley,
            CurrentHullPoints = -10 // Invalid - negative hull
        };

        fleet.Ships.Add(ship1);
        fleet.Ships.Add(ship2);

        // Act
        var report = _service.ValidateFleet(fleet);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(e => e.Code == "SHIP_HULL_001");
    }

    [Fact]
    public void ValidateFleet_ShipHullExceedsMax_ReturnsError()
    {
        // Arrange
        var fleet = new FleetData
        {
            Id = MBGUID.Generate(MBGUIDType.Fleet),
            Name = "Test Fleet",
            Morale = 50
        };

        var ship = new ShipData
        {
            Id = MBGUID.Generate(MBGUIDType.Ship),
            Name = "Overpowered Ship",
            Type = ShipType.Snekkja, // Small ship with 300 max hull
            CurrentHullPoints = 5000 // Way exceeds max
        };

        fleet.Ships.Add(ship);
        fleet.Flagship = ship;

        // Act
        var report = _service.ValidateFleet(fleet);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(e => e.Code == "SHIP_HULL_002");
    }

    [Fact]
    public void ValidateFleet_ShipCrewExceedsCapacity_ReturnsError()
    {
        // Arrange
        var fleet = new FleetData
        {
            Id = MBGUID.Generate(MBGUIDType.Fleet),
            Name = "Test Fleet",
            Morale = 50
        };

        var ship = new ShipData
        {
            Id = MBGUID.Generate(MBGUIDType.Ship),
            Name = "Overcrowded Ship",
            Type = ShipType.Snekkja, // Small ship with 20 crew capacity
            CurrentHullPoints = 100,
            CrewCount = 500 // Way exceeds capacity
        };

        fleet.Ships.Add(ship);
        fleet.Flagship = ship;

        // Act
        var report = _service.ValidateFleet(fleet);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(e => e.Code == "SHIP_CREW_002");
    }

    [Fact]
    public void ValidateFleet_ShipNegativeCrew_ReturnsError()
    {
        // Arrange
        var fleet = new FleetData
        {
            Id = MBGUID.Generate(MBGUIDType.Fleet),
            Name = "Test Fleet",
            Morale = 50
        };

        var ship = new ShipData
        {
            Id = MBGUID.Generate(MBGUIDType.Ship),
            Name = "Ghost Ship",
            Type = ShipType.Longship,
            CurrentHullPoints = 100,
            CrewCount = -10 // Invalid negative
        };

        fleet.Ships.Add(ship);
        fleet.Flagship = ship;

        // Act
        var report = _service.ValidateFleet(fleet);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(e => e.Code == "SHIP_CREW_001");
    }

    [Fact]
    public void ValidateFleet_ShipMoraleOutOfRange_ReturnsWarning()
    {
        // Arrange
        var fleet = new FleetData
        {
            Id = MBGUID.Generate(MBGUIDType.Fleet),
            Name = "Test Fleet",
            Morale = 50
        };

        var ship = new ShipData
        {
            Id = MBGUID.Generate(MBGUIDType.Ship),
            Name = "Unhappy Ship",
            Type = ShipType.Longship,
            CurrentHullPoints = 100,
            CrewMorale = 150 // Out of range
        };

        fleet.Ships.Add(ship);
        fleet.Flagship = ship;

        // Act
        var report = _service.ValidateFleet(fleet);

        // Assert
        report.HasWarnings.Should().BeTrue();
        report.Warnings.Should().Contain(w => w.Code == "SHIP_MORALE_001");
    }

    #endregion

    #region Full Save Validation Tests

    [Fact]
    public void Validate_SaveWithInvalidHero_ReturnsErrors()
    {
        // Arrange
        var save = new SaveFile
        {
            FilePath = "test.sav",
            Name = "Test Save",
            Header = new SaveHeader { Version = 7, GameVersion = "v1.2.0" },
            Metadata = new SaveMetadata { CharacterName = "Test", Level = 10 }
        };

        save.Heroes.Add(new HeroData
        {
            Id = MBGUID.Generate(MBGUIDType.Hero),
            Name = "Invalid Hero",
            Level = 0, // Invalid
            Gold = -100, // Invalid
            Attributes = new HeroAttributes { Vigor = -5 }, // Invalid
            Skills = new SkillSet()
        });

        // Act
        var report = _service.Validate(save);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Count.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public void Validate_SaveWithFleets_ValidatesFleets()
    {
        // Arrange
        var save = new SaveFile
        {
            FilePath = "test.sav",
            Name = "Test Save",
            Header = new SaveHeader { Version = 7, GameVersion = "v1.2.0" },
            Metadata = new SaveMetadata { CharacterName = "Test", Level = 10 }
        };

        var fleet = new FleetData
        {
            Id = MBGUID.Generate(MBGUIDType.Fleet),
            Name = "Invalid Fleet",
            Morale = -50 // Invalid
        };
        save.Fleets.Add(fleet);

        // Act
        var report = _service.Validate(save);

        // Assert
        report.HasWarnings.Should().BeTrue();
    }

    [Fact]
    public void Validate_SaveWithMissingVersion_ReturnsWarning()
    {
        // Arrange
        var save = new SaveFile
        {
            FilePath = "test.sav",
            Name = "Test Save",
            Header = new SaveHeader { Version = 7, GameVersion = "" },
            Metadata = new SaveMetadata { CharacterName = "Test", Level = 10 }
        };

        // Act
        var report = _service.Validate(save);

        // Assert
        report.HasWarnings.Should().BeTrue();
        report.Warnings.Should().Contain(w => w.Code == "HEADER_001");
    }

    [Fact]
    public void Validate_SaveWithUnusualVersion_ReturnsWarning()
    {
        // Arrange
        var save = new SaveFile
        {
            FilePath = "test.sav",
            Name = "Test Save",
            Header = new SaveHeader { Version = 999, GameVersion = "v1.0" },
            Metadata = new SaveMetadata { CharacterName = "Test", Level = 10 }
        };

        // Act
        var report = _service.Validate(save);

        // Assert
        report.HasWarnings.Should().BeTrue();
        report.Warnings.Should().Contain(w => w.Code == "HEADER_002");
    }

    #endregion

    #region Validation Mode Tests

    [Fact]
    public void SetValidationMode_Strict_ReturnsMoreWarnings()
    {
        // Arrange
        var hero = CreateValidHero();
        hero.Attributes.Vigor = 15; // Over 10
        _service.SetValidationMode(ValidationMode.Strict);

        // Act
        var report = _service.ValidateHero(hero);

        // Assert
        report.HasWarnings.Should().BeTrue();
    }

    [Fact]
    public void SetValidationMode_Permissive_ReturnsFewerWarnings()
    {
        // Arrange
        var hero = CreateValidHero();
        hero.Attributes.Vigor = 15;
        _service.SetValidationMode(ValidationMode.Permissive);

        // Act
        var report = _service.ValidateHero(hero);

        // Assert
        report.IsValid.Should().BeTrue();
    }

    #endregion

    #region ValidationReport Tests

    [Fact]
    public void ValidationReport_Merge_CombinesReports()
    {
        // Arrange
        var report1 = new ValidationReport();
        report1.AddError("ERR1", "Error 1");
        report1.AddWarning("WARN1", "Warning 1");

        var report2 = new ValidationReport();
        report2.AddError("ERR2", "Error 2");
        report2.AddInfo("INFO1", "Info 1");

        // Act
        report1.Merge(report2);

        // Assert
        report1.Errors.Should().HaveCount(2);
        report1.Warnings.Should().HaveCount(1);
        report1.Info.Should().HaveCount(1);
    }

    [Fact]
    public void ValidationIssue_ToString_FormatsCorrectly()
    {
        // Arrange
        var issue = new ValidationIssue(ValidationSeverity.Error, "TEST_001", "Test message", "Test.Path");

        // Act
        var result = issue.ToString();

        // Assert
        result.Should().Contain("[Error]");
        result.Should().Contain("TEST_001");
        result.Should().Contain("Test message");
        result.Should().Contain("Test.Path");
    }

    [Fact]
    public void ValidationIssue_ToString_NoPath_FormatsWithoutPath()
    {
        // Arrange
        var issue = new ValidationIssue(ValidationSeverity.Warning, "WARN_001", "Warning message");

        // Act
        var result = issue.ToString();

        // Assert
        result.Should().Contain("[Warning]");
        result.Should().NotContain(" at ");
    }

    #endregion

    #region PerkDatabase Tests

    [Fact]
    public void PerkDatabase_IsValidPerk_KnownPerk_ReturnsTrue()
    {
        // Act
        var result = PerkDatabase.IsValidPerk("swift_strike");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PerkDatabase_IsValidPerk_ModPerk_ReturnsTrue()
    {
        // Act - mod_ prefixed perks should be valid
        var result = PerkDatabase.IsValidPerk("mod_custom_perk");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PerkDatabase_IsValidPerk_UnknownPerk_ReturnsFalse()
    {
        // Act
        var result = PerkDatabase.IsValidPerk("completely_unknown_perk");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Additional ValidationIssue Tests

    [Fact]
    public void ValidationIssue_ErrorSeverity_ToString_ContainsError()
    {
        // Arrange
        var issue = new ValidationIssue(ValidationSeverity.Error, "ERR_001", "Error message");

        // Act
        var result = issue.ToString();

        // Assert
        result.Should().Contain("Error");
    }

    [Fact]
    public void ValidationIssue_WarningSeverity_ToString_ContainsWarning()
    {
        // Arrange
        var issue = new ValidationIssue(ValidationSeverity.Warning, "WARN_001", "Warning message");

        // Act
        var result = issue.ToString();

        // Assert
        result.Should().Contain("Warning");
    }

    [Fact]
    public void ValidationIssue_Code_IsPreserved()
    {
        // Arrange
        var issue = new ValidationIssue(ValidationSeverity.Error, "CUSTOM_CODE", "Message");

        // Act & Assert
        issue.Code.Should().Be("CUSTOM_CODE");
    }

    [Fact]
    public void ValidationIssue_Message_IsPreserved()
    {
        // Arrange
        var issue = new ValidationIssue(ValidationSeverity.Error, "CODE", "Custom message");

        // Act & Assert
        issue.Message.Should().Be("Custom message");
    }

    #endregion

    #region Additional PerkDatabase Tests

    [Theory]
    [InlineData("mod_perk_1")]
    [InlineData("mod_perk_2")]
    public void PerkDatabase_ModPrefixedPerks_AreRecognized(string perkId)
    {
        // Act
        var result = PerkDatabase.IsValidPerk(perkId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PerkDatabase_EmptyPerk_ReturnsFalse()
    {
        // Act
        var result = PerkDatabase.IsValidPerk(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Comprehensive ValidationSeverity Tests

    [Theory]
    [InlineData(ValidationSeverity.Error)]
    [InlineData(ValidationSeverity.Warning)]
    [InlineData(ValidationSeverity.Info)]
    public void ValidationIssue_AllSeverities_CanBeCreated(ValidationSeverity severity)
    {
        // Arrange & Act
        var issue = new ValidationIssue(severity, "CODE", "Message");

        // Assert
        issue.Severity.Should().Be(severity);
    }

    [Fact]
    public void ValidationSeverity_Error_HasExpectedValue()
    {
        // Assert
        ((int)ValidationSeverity.Error).Should().BeGreaterThan(0);
    }

    [Fact]
    public void ValidationSeverity_Warning_HasExpectedValue()
    {
        // Assert
        ((int)ValidationSeverity.Warning).Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Comprehensive Path Tests

    [Fact]
    public void ValidationIssue_WithPath_PreservesPath()
    {
        // Arrange
        var issue = new ValidationIssue(ValidationSeverity.Error, "CODE", "Message", "Hero.Health");

        // Act & Assert
        issue.Path.Should().Be("Hero.Health");
    }

    [Fact]
    public void ValidationIssue_WithNullPath_HasNullPath()
    {
        // Arrange
        var issue = new ValidationIssue(ValidationSeverity.Error, "CODE", "Message");

        // Act & Assert
        issue.Path.Should().BeNull();
    }

    [Theory]
    [InlineData("Hero.Name")]
    [InlineData("Party.Troops[0]")]
    [InlineData("Save.Header.Version")]
    public void ValidationIssue_VariousPaths_ArePreserved(string path)
    {
        // Arrange & Act
        var issue = new ValidationIssue(ValidationSeverity.Error, "CODE", "Message", path);

        // Assert
        issue.Path.Should().Be(path);
    }

    #endregion
}
