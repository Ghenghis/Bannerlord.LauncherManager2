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

    #region Additional Edge Case Tests

    [Fact]
    public void SetAttribute_AllAttributeTypes_SetsCorrectly()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act & Assert - test all attribute types
        _editor.SetAttribute(hero, AttributeType.Vigor, 5);
        hero.Attributes.Vigor.Should().Be(5);

        _editor.SetAttribute(hero, AttributeType.Control, 6);
        hero.Attributes.Control.Should().Be(6);

        _editor.SetAttribute(hero, AttributeType.Endurance, 7);
        hero.Attributes.Endurance.Should().Be(7);

        _editor.SetAttribute(hero, AttributeType.Cunning, 8);
        hero.Attributes.Cunning.Should().Be(8);

        _editor.SetAttribute(hero, AttributeType.Social, 9);
        hero.Attributes.Social.Should().Be(9);

        _editor.SetAttribute(hero, AttributeType.Intelligence, 10);
        hero.Attributes.Intelligence.Should().Be(10);
    }

    [Fact]
    public void SetSkill_AllSkillTypes_SetsCorrectly()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act & Assert - test multiple skill types
        _editor.SetSkill(hero, SkillType.TwoHanded, 100);
        hero.Skills.TwoHanded.Should().Be(100);

        _editor.SetSkill(hero, SkillType.Polearm, 150);
        hero.Skills.Polearm.Should().Be(150);

        _editor.SetSkill(hero, SkillType.Bow, 200);
        hero.Skills.Bow.Should().Be(200);

        _editor.SetSkill(hero, SkillType.Crossbow, 250);
        hero.Skills.Crossbow.Should().Be(250);
    }

    [Fact]
    public void SetAllSkills_SetsAllToValue()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetAllSkills(hero, 100);

        // Assert
        hero.Skills.OneHanded.Should().Be(100);
        hero.Skills.TwoHanded.Should().Be(100);
        hero.Skills.Bow.Should().Be(100);
    }

    [Fact]
    public void MaximizeSkills_SetsAllTo300()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.MaximizeSkills(hero);

        // Assert
        hero.Skills.OneHanded.Should().Be(300);
        hero.Skills.TwoHanded.Should().Be(300);
    }

    [Fact]
    public void SetNavalSkill_AllNavalSkillTypes_SetsCorrectly()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act & Assert
        _editor.SetNavalSkill(hero, NavalSkillType.Navigation, 100);
        hero.NavalSkills!.Navigation.Should().Be(100);

        _editor.SetNavalSkill(hero, NavalSkillType.NavalTactics, 150);
        hero.NavalSkills.NavalTactics.Should().Be(150);

        _editor.SetNavalSkill(hero, NavalSkillType.NavalStewardship, 200);
        hero.NavalSkills.NavalStewardship.Should().Be(200);
    }

    [Fact]
    public void SetNavalSkill_NegativeValue_ThrowsException()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetNavalSkill(hero, NavalSkillType.Navigation, -1))
            .Should().Throw<Exception>();
    }

    [Fact]
    public void SetNavalSkill_ExceedsMax_ThrowsException()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetNavalSkill(hero, NavalSkillType.Navigation, 301))
            .Should().Throw<Exception>();
    }

    [Fact]
    public void MaximizeNavalSkills_SetsAllTo300()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.MaximizeNavalSkills(hero);

        // Assert
        hero.NavalSkills.Should().NotBeNull();
        hero.NavalSkills!.Navigation.Should().Be(300);
        hero.NavalSkills.NavalTactics.Should().Be(300);
        hero.NavalSkills.NavalStewardship.Should().Be(300);
    }

    [Fact]
    public void AddSkillXP_IncreasesSkill()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.Skills.OneHanded = 50;

        // Act
        _editor.AddSkillXP(hero, SkillType.OneHanded, 10000);

        // Assert
        hero.Skills.OneHanded.Should().BeGreaterThan(50);
    }

    [Fact]
    public void GetExpectedAttributePoints_ReturnsCorrectValue()
    {
        // Act
        var points = _editor.GetExpectedAttributePoints(10);

        // Assert
        points.Should().Be(16); // 6 base + 10 level
    }

    [Fact]
    public void ImportTemplate_PartialImport_OnlyImportsSelected()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.Attributes.Vigor = 5;
        hero.Skills.OneHanded = 100;

        var template = new CharacterTemplate
        {
            Attributes = new HeroAttributes { Vigor = 10 },
            Skills = new SkillSet { OneHanded = 250 },
            Perks = new List<string> { "test_perk" }
        };
        var options = new TemplateImportOptions
        {
            ImportAttributes = true,
            ImportSkills = false,
            ImportPerks = false
        };

        // Act
        _editor.ImportTemplate(hero, template, options);

        // Assert
        hero.Attributes.Vigor.Should().Be(10); // Imported
        hero.Skills.OneHanded.Should().Be(100); // Not imported
        hero.UnlockedPerks.Should().NotContain("test_perk"); // Not imported
    }

    [Fact]
    public void ImportTemplate_WithNavalSkills_ImportsNavalSkills()
    {
        // Arrange
        var hero = CreateTestHero();
        var template = new CharacterTemplate
        {
            Attributes = new HeroAttributes(),
            Skills = new SkillSet(),
            NavalSkills = new NavalSkillSet { Navigation = 200 },
            Perks = new List<string>()
        };
        var options = new TemplateImportOptions { ImportNavalSkills = true };

        // Act
        _editor.ImportTemplate(hero, template, options);

        // Assert
        hero.NavalSkills.Should().NotBeNull();
        hero.NavalSkills!.Navigation.Should().Be(200);
    }

    [Fact]
    public void ImportTemplate_WithAppearance_ImportsAppearance()
    {
        // Arrange
        var hero = CreateTestHero();
        var template = new CharacterTemplate
        {
            Attributes = new HeroAttributes(),
            Skills = new SkillSet(),
            Appearance = new AppearanceData { FaceCode = "test_face" },
            Perks = new List<string>()
        };
        var options = new TemplateImportOptions { ImportAppearance = true };

        // Act
        _editor.ImportTemplate(hero, template, options);

        // Assert
        hero.Appearance.Should().NotBeNull();
        hero.Appearance!.FaceCode.Should().Be("test_face");
    }

    [Fact]
    public void ExportTemplate_WithNavalSkills_IncludesNavalSkills()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.NavalSkills = new NavalSkillSet { Navigation = 150 };

        // Act
        var template = _editor.ExportTemplate(hero);

        // Assert
        template.NavalSkills.Should().NotBeNull();
        template.NavalSkills!.Navigation.Should().Be(150);
    }

    [Fact]
    public void ExportTemplate_WithAppearance_IncludesAppearance()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.Appearance = new AppearanceData { FaceCode = "exported_face" };

        // Act
        var template = _editor.ExportTemplate(hero);

        // Assert
        template.Appearance.Should().NotBeNull();
        template.Appearance!.FaceCode.Should().Be("exported_face");
    }

    #endregion

    #region Level & Experience Tests

    [Fact]
    public void SetLevel_ValidLevel_SetsLevelAndXP()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetLevel(hero, 25);

        // Assert
        hero.Level.Should().Be(25);
        hero.Experience.Should().BeGreaterThan(0);
    }

    [Fact]
    public void SetLevel_HighLevel_SetsWithWarning()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act - level above 62 should work but warn
        _editor.SetLevel(hero, 70);

        // Assert
        hero.Level.Should().Be(70);
    }

    [Fact]
    public void AddLevels_IncreasesLevel()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.Level = 10;

        // Act
        _editor.AddLevels(hero, 5);

        // Assert
        hero.Level.Should().Be(15);
    }

    [Fact]
    public void SetExperience_SetsXP()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetExperience(hero, 50000);

        // Assert
        hero.Experience.Should().Be(50000);
    }

    [Fact]
    public void SetExperience_NegativeValue_ClampsToZero()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetExperience(hero, -1000);

        // Assert
        hero.Experience.Should().Be(0);
    }

    #endregion

    #region Gold & Resources Tests

    [Fact]
    public void AddGold_PositiveAmount_IncreasesGold()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.Gold = 1000;

        // Act
        _editor.AddGold(hero, 500);

        // Assert
        hero.Gold.Should().Be(1500);
    }

    [Fact]
    public void AddGold_NegativeAmount_ThrowsWhenResultNegative()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.Gold = 100;

        // Act & Assert - SetGold throws when negative
        FluentActions.Invoking(() => _editor.AddGold(hero, -200))
            .Should().Throw<EditorException>();
    }

    #endregion

    #region Health & State Tests

    [Fact]
    public void SetHealth_ClampsToRange()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetHealth(hero, 150);

        // Assert
        hero.Health.Should().BeLessOrEqualTo(hero.MaxHealth);
    }

    [Fact]
    public void SetHealth_NegativeValue_ClampsToZero()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetHealth(hero, -50);

        // Assert
        hero.Health.Should().Be(0);
    }

    [Fact]
    public void FullHeal_RestoresHealthAndClearsWounded()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.Health = 50;
        hero.IsWounded = true;

        // Act
        _editor.FullHeal(hero);

        // Assert
        hero.Health.Should().Be(hero.MaxHealth);
        hero.IsWounded.Should().BeFalse();
    }

    [Fact]
    public void SetState_ChangesState()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetState(hero, HeroState.Prisoner);

        // Assert
        hero.State.Should().Be(HeroState.Prisoner);
    }

    [Fact]
    public void Resurrect_DeadHero_ResurrectsAndHeals()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.State = HeroState.Dead;
        hero.Health = 0;

        // Act
        _editor.Resurrect(hero);

        // Assert
        hero.State.Should().Be(HeroState.Active);
        hero.Health.Should().Be(hero.MaxHealth);
    }

    [Fact]
    public void Resurrect_AliveHero_DoesNothing()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.State = HeroState.Active;
        hero.Health = 50;

        // Act
        _editor.Resurrect(hero);

        // Assert
        hero.State.Should().Be(HeroState.Active);
        hero.Health.Should().Be(50);
    }

    #endregion

    #region Age & Appearance Tests

    [Fact]
    public void SetAge_ValidAge_SetsAge()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetAge(hero, 35);

        // Assert
        hero.Age.Should().Be(35);
    }

    [Fact]
    public void SetAge_BelowMinimum_SetsWithWarning()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetAge(hero, 15);

        // Assert
        hero.Age.Should().Be(15);
    }

    [Fact]
    public void SetAge_AboveMaximum_SetsWithWarning()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetAge(hero, 110);

        // Assert
        hero.Age.Should().Be(110);
    }

    [Fact]
    public void ExportAppearance_NullAppearance_ReturnsEmpty()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.Appearance = null;

        // Act
        var result = _editor.ExportAppearance(hero);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Perk Tests

    [Fact]
    public void UnlockPerk_NewPerk_AddsPerk()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.UnlockPerk(hero, "test_perk");

        // Assert
        hero.UnlockedPerks.Should().Contain("test_perk");
    }

    [Fact]
    public void UnlockPerk_ExistingPerk_DoesNotDuplicate()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.UnlockedPerks.Add("test_perk");

        // Act
        _editor.UnlockPerk(hero, "test_perk");

        // Assert
        hero.UnlockedPerks.Count(p => p == "test_perk").Should().Be(1);
    }

    [Fact]
    public void LockPerk_ExistingPerk_RemovesPerk()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.UnlockedPerks.Add("test_perk");

        // Act
        _editor.LockPerk(hero, "test_perk");

        // Assert
        hero.UnlockedPerks.Should().NotContain("test_perk");
    }

    [Fact]
    public void LockAllPerks_ClearsAllPerks()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.UnlockedPerks.Add("perk1");
        hero.UnlockedPerks.Add("perk2");

        // Act
        _editor.LockAllPerks(hero);

        // Assert
        hero.UnlockedPerks.Should().BeEmpty();
    }

    [Fact]
    public void UnlockAllPerks_ForSkill_UnlocksSkillPerks()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.UnlockAllPerks(hero, SkillType.OneHanded);

        // Assert
        hero.UnlockedPerks.Should().NotBeEmpty();
    }

    #endregion

    #region SetAllSkills Tests

    [Fact]
    public void SetAllSkills_ClampsValue()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetAllSkills(hero, 500); // Over max

        // Assert
        hero.Skills.OneHanded.Should().Be(300);
        hero.Skills.TwoHanded.Should().Be(300);
    }

    [Fact]
    public void SetAllSkills_ClampsNegativeToZero()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetAllSkills(hero, -50); // Negative

        // Assert
        hero.Skills.OneHanded.Should().Be(0);
    }

    #endregion

    #region Naval Skills Tests

    [Fact]
    public void SetNavalSkill_Navigation_SetsValue()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetNavalSkill(hero, NavalSkillType.Navigation, 150);

        // Assert
        hero.NavalSkills!.Navigation.Should().Be(150);
    }

    [Fact]
    public void SetNavalSkill_NavalTactics_SetsValue()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetNavalSkill(hero, NavalSkillType.NavalTactics, 200);

        // Assert
        hero.NavalSkills!.NavalTactics.Should().Be(200);
    }

    [Fact]
    public void SetNavalSkill_NavalStewardship_SetsValue()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetNavalSkill(hero, NavalSkillType.NavalStewardship, 100);

        // Assert
        hero.NavalSkills!.NavalStewardship.Should().Be(100);
    }

    [Fact]
    public void SetNavalSkill_NegativeNavigation_ThrowsException()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetNavalSkill(hero, NavalSkillType.Navigation, -10))
            .Should().Throw<EditorException>();
    }

    [Fact]
    public void SetNavalSkill_ExceedsMaxNavigation_ThrowsException()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act & Assert
        FluentActions.Invoking(() => _editor.SetNavalSkill(hero, NavalSkillType.Navigation, 350))
            .Should().Throw<EditorException>();
    }

    [Fact]
    public void MaximizeNavalSkills_SetsAllToMax()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.MaximizeNavalSkills(hero);

        // Assert
        hero.NavalSkills!.Navigation.Should().Be(300);
        hero.NavalSkills.NavalTactics.Should().Be(300);
        hero.NavalSkills.NavalStewardship.Should().Be(300);
    }

    #endregion


    #region Attribute Tests Extended

    [Fact]
    public void AddAttributePoints_AddsToExisting()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.Attributes.Vigor = 5;

        // Act
        _editor.AddAttributePoints(hero, AttributeType.Vigor, 3);

        // Assert
        hero.Attributes.Vigor.Should().Be(8);
    }

    [Fact]
    public void SetAllAttributes_SetsAllToValue()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.SetAllAttributes(hero, 10);

        // Assert
        hero.Attributes.Vigor.Should().Be(10);
        hero.Attributes.Control.Should().Be(10);
        hero.Attributes.Endurance.Should().Be(10);
        hero.Attributes.Cunning.Should().Be(10);
        hero.Attributes.Social.Should().Be(10);
        hero.Attributes.Intelligence.Should().Be(10);
    }

    [Fact]
    public void GetExpectedAttributePoints_Level10_Returns16()
    {
        // Act
        var result = _editor.GetExpectedAttributePoints(10);

        // Assert - Base 6 + level
        result.Should().Be(16);
    }

    #endregion

    #region Skill XP Tests

    [Fact]
    public void AddSkillXP_LargeAmount_IncreasesSkill()
    {
        // Arrange
        var hero = CreateTestHero();
        hero.Skills.OneHanded = 100;

        // Act
        _editor.AddSkillXP(hero, SkillType.OneHanded, 5000);

        // Assert
        hero.Skills.OneHanded.Should().BeGreaterThan(100);
    }

    [Fact]
    public void MaximizeSkills_SetsAllToMax()
    {
        // Arrange
        var hero = CreateTestHero();

        // Act
        _editor.MaximizeSkills(hero);

        // Assert
        hero.Skills.OneHanded.Should().Be(300);
        hero.Skills.TwoHanded.Should().Be(300);
        hero.Skills.Athletics.Should().Be(300);
    }

    #endregion

}
