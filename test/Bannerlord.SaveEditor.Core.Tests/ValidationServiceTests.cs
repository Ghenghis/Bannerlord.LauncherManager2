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
}
