// <copyright file="SaveServiceTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Interfaces;
using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.Services;
using Bannerlord.SaveEditor.Core.Validation;
using FluentAssertions;
using System.IO.Compression;
using Xunit;

public class SaveServiceTests : IDisposable
{
    private readonly SaveService _service;
    private readonly string _testDirectory;

    public SaveServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"SaveServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        var options = new SaveServiceOptions
        {
            SaveDirectory = _testDirectory
        };

        _service = new SaveService(options: options);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }
        catch { }
    }

    private SaveFile CreateTestSaveFile()
    {
        return new SaveFile
        {
            FilePath = Path.Combine(_testDirectory, "test.sav"),
            Name = "Test Save",
            Header = new SaveHeader
            {
                Version = 7,
                GameVersion = "v1.2.10"
            },
            Metadata = new SaveMetadata
            {
                CharacterName = "Test Hero",
                Level = 20,
                DayNumber = 100,
                Gold = 10000,
                ClanName = "Test Clan"
            },
            LastModified = DateTime.UtcNow
        };
    }

    #region DiscoverSaves Tests

    [Fact]
    public async Task DiscoverSavesAsync_EmptyDirectory_ReturnsEmpty()
    {
        // Act
        var saves = await _service.DiscoverSavesAsync();

        // Assert
        saves.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverSavesAsync_NonExistentDirectory_ReturnsEmpty()
    {
        // Arrange
        var options = new SaveServiceOptions { SaveDirectory = "/nonexistent/path" };
        var service = new SaveService(options: options);

        // Act
        var saves = await service.DiscoverSavesAsync();

        // Assert
        saves.Should().BeEmpty();
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidateAsync_ValidSave_ReturnsReport()
    {
        // Arrange
        var save = CreateTestSaveFile();

        // Act
        var report = await _service.ValidateAsync(save);

        // Assert
        report.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_SaveWithHeroes_ValidatesHeroes()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Heroes.Add(new HeroData
        {
            Id = MBGUID.Generate(MBGUIDType.Hero),
            Name = "Test Hero",
            Level = 10,
            Gold = 1000,
            Attributes = new HeroAttributes { Vigor = 5 },
            Skills = new SkillSet { OneHanded = 100 }
        });

        // Act
        var report = await _service.ValidateAsync(save);

        // Assert
        report.Should().NotBeNull();
    }

    #endregion

    #region VerifyIntegrity Tests

    [Fact]
    public async Task VerifyIntegrityAsync_NonExistentFile_ReturnsFalse()
    {
        // Act
        var result = await _service.VerifyIntegrityAsync("/nonexistent/file.sav");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Event Tests

    [Fact]
    public void SaveLoaded_Event_IsAccessible()
    {
        // Arrange
        var eventRaised = false;
        _service.SaveLoaded += (s, e) => eventRaised = true;

        // Assert - just verify the event can be subscribed to
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void SaveSaving_Event_IsAccessible()
    {
        // Arrange
        var eventRaised = false;
        _service.SaveSaving += (s, e) => eventRaised = true;

        // Assert - just verify the event can be subscribed to
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void SaveSaved_Event_IsAccessible()
    {
        // Arrange
        var eventRaised = false;
        _service.SaveSaved += (s, e) => eventRaised = true;

        // Assert - just verify the event can be subscribed to
        eventRaised.Should().BeFalse();
    }

    #endregion

    #region SaveServiceOptions Tests

    [Fact]
    public void SaveServiceOptions_DefaultValues()
    {
        // Arrange & Act
        var options = new SaveServiceOptions();

        // Assert
        options.SaveDirectory.Should().BeNull();
        options.AutoBackupOnLoad.Should().BeFalse();
        options.AutoBackupOnSave.Should().BeTrue();
        options.StrictValidation.Should().BeFalse();
    }

    [Fact]
    public void SaveServiceOptions_CustomValues()
    {
        // Arrange & Act
        var options = new SaveServiceOptions
        {
            SaveDirectory = "/custom/path",
            AutoBackupOnLoad = true,
            AutoBackupOnSave = false,
            StrictValidation = true
        };

        // Assert
        options.SaveDirectory.Should().Be("/custom/path");
        options.AutoBackupOnLoad.Should().BeTrue();
        options.AutoBackupOnSave.Should().BeFalse();
        options.StrictValidation.Should().BeTrue();
    }

    #endregion

    #region Exception Tests

    [Fact]
    public void SaveLoadException_Message_IsSet()
    {
        // Arrange & Act
        var ex = new SaveLoadException("Test error");

        // Assert
        ex.Message.Should().Be("Test error");
    }

    [Fact]
    public void SaveLoadException_InnerException_IsSet()
    {
        // Arrange
        var inner = new Exception("Inner");

        // Act
        var ex = new SaveLoadException("Test error", inner);

        // Assert
        ex.InnerException.Should().Be(inner);
    }

    #endregion
}
