// <copyright file="CharacterEditorTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Services;
using FluentAssertions;
using Xunit;

public class CharacterEditorTests
{
    private readonly CharacterEditor _editor;

    public CharacterEditorTests()
    {
        _editor = new CharacterEditor();
    }

    private HeroData CreateTestHero()
    {
        return new HeroData
        {
            Id = MBGUID.Generate(MBGUIDType.Hero),
            HeroId = "test_hero",
            Name = "Test Hero",
            Level = 10,
            Gold = 1000,
            Age = 25,
            IsAlive = true,
            Attributes = new HeroAttributes
            {
                Vigor = 3,
                Control = 3,
                Endurance = 3,
                Cunning = 3,
                Social = 3,
                Intelligence = 3
            },
            Skills = new SkillSet()
        };
    }

    [Fact]
    public void SetAttribute_ValidValue_SetsCorrectly()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetAttribute(hero, AttributeType.Vigor, 8);

        // Assert
        hero.Attributes.Vigor.Should().Be(8);
    }

    [Fact]
    public void SetAttribute_NegativeValue_ThrowsException()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetAttribute(hero, AttributeType.Vigor, -1))
            .Should().Throw<Exception>();
    }

    [Fact]
    public void SetSkill_ValidValue_SetsCorrectly()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetSkill(hero, SkillType.OneHanded, 150);

        // Assert
        hero.Skills.OneHanded.Should().Be(150);
    }

    [Fact]
    public void SetSkill_ExceedsMax_ThrowsException()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetSkill(hero, SkillType.OneHanded, 301))
            .Should().Throw<Exception>();
    }

    [Fact]
    public void SetNavalSkill_CreatesNavalSkillsIfNull()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.NavalSkills = null;

        // Act
        _editor.SetNavalSkill(hero, NavalSkillType.Navigation, 100);

        // Assert
        hero.NavalSkills.Should().NotBeNull();
        hero.NavalSkills!.Navigation.Should().Be(100);
    }

    [Fact]
    public void UnlockPerk_AddsNewPerk()
    {
        // Arrange
        var hero = CreateTestHero();
        var perkId = "swift_strike";

        // Act
        _editor.UnlockPerk(hero, perkId);

        // Assert
        hero.UnlockedPerks.Should().Contain(perkId);
    }

    [Fact]
    public void UnlockPerk_DuplicatePerk_DoesNotAddAgain()
    {
        // Arrange
        var hero = CreateTestHero();
        var perkId = "swift_strike";
        _editor.UnlockPerk(hero, perkId);

        // Act
        _editor.UnlockPerk(hero, perkId);

        // Assert
        hero.UnlockedPerks.Count(p => p == perkId).Should().Be(1);
    }

    [Fact]
    public void LockPerk_RemovesPerk()
    {
        // Arrange
        var hero = CreateTestHero();
        var perkId = "swift_strike";
        _editor.UnlockPerk(hero, perkId);

        // Act
        _editor.LockPerk(hero, perkId);

        // Assert
        hero.UnlockedPerks.Should().NotContain(perkId);
    }

    [Fact]
    public void SetGold_ValidValue_SetsCorrectly()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetGold(hero, 5000);

        // Assert
        hero.Gold.Should().Be(5000);
    }

    [Fact]
    public void SetGold_NegativeValue_ThrowsException()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetGold(hero, -100))
            .Should().Throw<Exception>();
    }

    [Fact]
    public void SetLevel_UpdatesExperience()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetLevel(hero, 20);

        // Assert
        hero.Level.Should().Be(20);
        hero.Experience.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ExportTemplate_IncludesAllData()
    {
        // Arrange
        var hero = CreateTestHero();
        _editor.SetAttribute(hero, AttributeType.Vigor, 8);
        _editor.SetSkill(hero, SkillType.OneHanded, 200);
        _editor.UnlockPerk(hero, "swift_strike");

        // Act
        var template = _editor.ExportTemplate(hero);

        // Assert
        template.Name.Should().Be(hero.Name);
        template.Attributes.Should().NotBeNull();
        template.Attributes.Vigor.Should().Be(8);
        template.Skills.Should().NotBeNull();
        template.Skills.OneHanded.Should().Be(200);
        template.Perks.Should().Contain("swift_strike");
    }

    [Fact]
    public void ImportTemplate_AppliesData()
    {
        // Arrange
        var hero = CreateTestHero();
        var template = new CharacterTemplate
        {
            Attributes = new HeroAttributes { Vigor = 10, Control = 8 },
            Skills = new SkillSet { OneHanded = 250 },
            Perks = new List<string> { "basher" }
        };
        var options = new TemplateImportOptions
        {
            ImportAttributes = true,
            ImportSkills = true,
            ImportPerks = true
        };

        // Act
        _editor.ImportTemplate(hero, template, options);

        // Assert
        hero.Attributes.Vigor.Should().Be(10);
        hero.Attributes.Control.Should().Be(8);
        hero.Skills.OneHanded.Should().Be(250);
        hero.UnlockedPerks.Should().Contain("basher");
    }

    [Fact]
    public void Validate_ReturnsReport()
    {
        // Arrange
        var hero = CreateTestHero();
        _editor.SetAttribute(hero, AttributeType.Vigor, 10);
        _editor.SetSkill(hero, SkillType.OneHanded, 200);
        _editor.SetLevel(hero, 50);
        _editor.UnlockPerk(hero, "swift_strike");

        // Act
        var report = _editor.Validate(hero);

        // Assert
        report.Should().NotBeNull();
    }
}
