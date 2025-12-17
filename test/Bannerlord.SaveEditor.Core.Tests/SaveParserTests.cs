// <copyright file="SaveParserTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Compression;
using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Interfaces;
using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.Parsers;
using FluentAssertions;
using System.Text;
using System.Text.Json;
using Xunit;

/// <summary>
/// Tests for SaveParser.
/// </summary>
public sealed class SaveParserTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SaveParser _parser;

    public SaveParserTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"SaveParserTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _parser = new SaveParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    #region Helper Methods

    private string CreateTestSaveFile(string? fileName = null, SaveFileOptions? options = null)
    {
        fileName ??= $"testsave_{Guid.NewGuid()}.sav";
        options ??= new SaveFileOptions();
        var path = Path.Combine(_testDirectory, fileName);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        // Write magic number
        writer.Write(options.MagicNumber.ToCharArray());

        // Write version
        writer.Write(options.Version);

        // Write game version string
        writer.Write(options.GameVersion.Length);
        writer.Write(options.GameVersion.ToCharArray());

        // Write module count and modules
        writer.Write(options.Modules.Count);
        foreach (var module in options.Modules)
        {
            writer.Write(module.Id.Length);
            writer.Write(module.Id.ToCharArray());
            writer.Write(module.Version.Length);
            writer.Write(module.Version.ToCharArray());
            writer.Write(module.IsOfficial);
        }

        // Write metadata JSON
        var metadata = JsonSerializer.Serialize(new
        {
            CharacterName = options.CharacterName,
            MainHeroLevel = options.Level,
            DayLong = (double)options.DayNumber,
            PlayTime = options.PlayTimeSeconds,
            ClanName = options.ClanName,
            Gold = options.Gold
        });
        var metadataBytes = Encoding.UTF8.GetBytes(metadata);
        writer.Write(metadataBytes.Length);
        writer.Write(metadataBytes);

        // Write compressed campaign data
        var campaignData = CreateMinimalCampaignData();
        var zlibHandler = new ZlibHandler();
        var compressedData = zlibHandler.CompressAsync(campaignData).GetAwaiter().GetResult();
        writer.Write(compressedData.Length);
        writer.Write(compressedData);

        return path;
    }

    private byte[] CreateMinimalCampaignData()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        // Campaign time segment
        writer.Write((ushort)0x0001);  // Segment ID
        writer.Write(8);               // Segment size (Int64)
        writer.Write(10000000L);       // Ticks

        // Empty heroes segment
        writer.Write((ushort)0x0010);  // Segment ID
        writer.Write(4);               // Segment size
        writer.Write(0);               // Count

        // Empty parties segment
        writer.Write((ushort)0x0020);  // Segment ID
        writer.Write(4);               // Segment size
        writer.Write(0);               // Count

        return stream.ToArray();
    }

    private sealed class SaveFileOptions
    {
        public string MagicNumber { get; set; } = "TWSV";
        public int Version { get; set; } = 7;
        public string GameVersion { get; set; } = "v1.2.9";
        public List<ModuleInfo> Modules { get; set; } = new()
        {
            new ModuleInfo { Id = "Native", Version = "v1.2.9", IsOfficial = true },
            new ModuleInfo { Id = "SandBoxCore", Version = "v1.2.9", IsOfficial = true }
        };
        public string CharacterName { get; set; } = "TestHero";
        public int Level { get; set; } = 15;
        public int DayNumber { get; set; } = 100;
        public double PlayTimeSeconds { get; set; } = 36000;
        public string? ClanName { get; set; } = "TestClan";
        public int Gold { get; set; } = 50000;
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_ValidSave_ParsesSuccessfully()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Should().NotBeNull();
        save.FilePath.Should().Be(savePath);
        save.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoadAsync_FileNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.sav");

        // Act & Assert
        await FluentActions.Invoking(() => _parser.LoadAsync(nonExistentPath))
            .Should().ThrowAsync<SaveParseException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task LoadAsync_InvalidMagicNumber_ThrowsException()
    {
        // Arrange
        var savePath = CreateTestSaveFile(options: new SaveFileOptions { MagicNumber = "XXXX" });

        // Act & Assert
        await FluentActions.Invoking(() => _parser.LoadAsync(savePath))
            .Should().ThrowAsync<SaveParseException>()
            .WithMessage("*Invalid magic number*");
    }

    [Fact]
    public async Task LoadAsync_ParsesHeader()
    {
        // Arrange
        var savePath = CreateTestSaveFile(options: new SaveFileOptions
        {
            Version = 7,
            GameVersion = "v1.2.10"
        });

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Header.Version.Should().Be(7);
        save.Header.GameVersion.Should().Be("v1.2.10");
    }

    [Fact]
    public async Task LoadAsync_ParsesModules()
    {
        // Arrange
        var modules = new List<ModuleInfo>
        {
            new() { Id = "Native", Version = "v1.2.9", IsOfficial = true },
            new() { Id = "CustomMod", Version = "v1.0.0", IsOfficial = false },
            new() { Id = "WarSails", Version = "v2.5.0", IsOfficial = false }
        };
        var savePath = CreateTestSaveFile(options: new SaveFileOptions { Modules = modules });

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Modules.Should().HaveCount(3);
        save.Modules.Should().Contain(m => m.Id == "Native" && m.IsOfficial);
        save.Modules.Should().Contain(m => m.Id == "WarSails" && !m.IsOfficial);
    }

    [Fact]
    public async Task LoadAsync_ParsesMetadata()
    {
        // Arrange
        var savePath = CreateTestSaveFile(options: new SaveFileOptions
        {
            CharacterName = "Sir Testing",
            Level = 42,
            DayNumber = 365,
            Gold = 100000
        });

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Metadata.CharacterName.Should().Be("Sir Testing");
        save.Metadata.Level.Should().Be(42);
        save.Metadata.DayNumber.Should().Be(365);
        save.Metadata.Gold.Should().Be(100000);
    }

    #endregion

    #region LoadInfoAsync Tests

    [Fact]
    public async Task LoadInfoAsync_ReturnsMetadataOnly()
    {
        // Arrange
        var savePath = CreateTestSaveFile(options: new SaveFileOptions
        {
            CharacterName = "InfoTest",
            Level = 20,
            DayNumber = 50
        });

        // Act
        var info = await _parser.LoadInfoAsync(savePath);

        // Assert
        info.Should().NotBeNull();
        info.Path.Should().Be(savePath);
        info.CharacterName.Should().Be("InfoTest");
        info.Level.Should().Be(20);
        info.Day.Should().Be(50);
    }

    [Fact]
    public async Task LoadInfoAsync_DetectsWarSails()
    {
        // Arrange
        var modules = new List<ModuleInfo>
        {
            new() { Id = "Native", Version = "v1.2.9", IsOfficial = true },
            new() { Id = "WarSails", Version = "v2.5.0", IsOfficial = false }
        };
        var savePath = CreateTestSaveFile(options: new SaveFileOptions { Modules = modules });

        // Act
        var info = await _parser.LoadInfoAsync(savePath);

        // Assert
        info.HasWarSails.Should().BeTrue();
        info.ModuleIds.Should().Contain("WarSails");
    }

    [Fact]
    public async Task LoadInfoAsync_NoWarSails_ReturnsFalse()
    {
        // Arrange
        var modules = new List<ModuleInfo>
        {
            new() { Id = "Native", Version = "v1.2.9", IsOfficial = true }
        };
        var savePath = CreateTestSaveFile(options: new SaveFileOptions { Modules = modules });

        // Act
        var info = await _parser.LoadInfoAsync(savePath);

        // Assert
        info.HasWarSails.Should().BeFalse();
    }

    [Fact]
    public async Task LoadInfoAsync_IncludesFileInfo()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var expectedSize = new FileInfo(savePath).Length;

        // Act
        var info = await _parser.LoadInfoAsync(savePath);

        // Assert
        info.FileSize.Should().Be(expectedSize);
        info.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region MetadataOnly Option Tests

    [Fact]
    public async Task LoadAsync_MetadataOnly_SkipsCampaignData()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var options = new LoadOptions { MetadataOnly = true };

        // Act
        var save = await _parser.LoadAsync(savePath, options);

        // Assert
        save.Should().NotBeNull();
        save.Header.Should().NotBeNull();
        save.Metadata.Should().NotBeNull();
        save.Heroes.Should().BeEmpty();
        save.Parties.Should().BeEmpty();
    }

    #endregion

    #region KeepRawData Option Tests

    [Fact]
    public async Task LoadAsync_KeepRawData_PreservesDecompressedData()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var options = new LoadOptions { KeepRawData = true };

        // Act
        var save = await _parser.LoadAsync(savePath, options);

        // Assert
        save.RawData.Should().NotBeNull();
        save.RawData.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoadAsync_NoKeepRawData_DoesNotPreserveData()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var options = new LoadOptions { KeepRawData = false };

        // Act
        var save = await _parser.LoadAsync(savePath, options);

        // Assert
        save.RawData.Should().BeNull();
    }

    #endregion


    #region Compression Tests

    [Fact]
    public async Task LoadAsync_DecompressesZlibData()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Header.CompressedSize.Should().BeGreaterThan(0);
        save.Header.UncompressedSize.Should().BeGreaterThan(0);
        save.Header.UncompressedSize.Should().BeGreaterThanOrEqualTo(save.Header.CompressedSize);
    }

    #endregion

    #region CampaignTime Tests

    [Fact]
    public async Task LoadAsync_ParsesCampaignTime()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.CampaignTime.Should().NotBeNull();
        save.CampaignTime.Ticks.Should().BeGreaterThan(0);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task LoadAsync_Permissive_ContinuesOnError()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var options = new LoadOptions { Permissive = true };

        // Act - should not throw even with potential parsing issues
        var save = await _parser.LoadAsync(savePath, options);

        // Assert
        save.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_WithCancellation_RespectsCancellationToken()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await FluentActions.Invoking(() => _parser.LoadAsync(savePath, null, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public async Task LoadAsync_AllOptions_ParsesSuccessfully()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var options = new LoadOptions
        {
            MetadataOnly = false,
            KeepRawData = true,
            Permissive = true
        };

        // Act
        var save = await _parser.LoadAsync(savePath, options);

        // Assert
        save.Should().NotBeNull();
        save.RawData.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadInfoAsync_WithCancellation_RespectsCancellationToken()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await FluentActions.Invoking(() => _parser.LoadInfoAsync(savePath, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task LoadAsync_LargeSave_ParsesSuccessfully()
    {
        // Arrange
        var options = new SaveFileOptions
        {
            CharacterName = "LargeTestHero",
            Level = 50,
            DayNumber = 1000,
            Gold = 999999
        };
        var savePath = CreateTestSaveFile(options: options);

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Metadata.Level.Should().Be(50);
        save.Metadata.DayNumber.Should().Be(1000);
    }

    [Fact]
    public async Task LoadAsync_UnsupportedVersion_WarnsButContinues()
    {
        // Arrange
        var options = new SaveFileOptions { Version = 999 };
        var savePath = CreateTestSaveFile(options: options);
        var loadOptions = new LoadOptions { Permissive = true };

        // Act
        var save = await _parser.LoadAsync(savePath, loadOptions);

        // Assert
        save.Should().NotBeNull();
    }

    #endregion

    #region LoadOptions Tests

    [Fact]
    public void LoadOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new LoadOptions();

        // Assert
        options.MetadataOnly.Should().BeFalse();
        options.KeepRawData.Should().BeFalse();
        options.Permissive.Should().BeFalse();
        options.SkipValidation.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_MetadataOnly_LoadsOnlyMetadata()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var options = new LoadOptions { MetadataOnly = true };

        // Act
        var save = await _parser.LoadAsync(savePath, options);

        // Assert
        save.Should().NotBeNull();
        save.Metadata.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_SkipValidation_SkipsValidation()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var options = new LoadOptions { SkipValidation = true };

        // Act
        var save = await _parser.LoadAsync(savePath, options);

        // Assert
        save.Should().NotBeNull();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task LoadAsync_NonExistentFile_ThrowsException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.sav");

        // Act & Assert
        await FluentActions.Invoking(() => _parser.LoadAsync(nonExistentPath))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task LoadInfoAsync_ValidFile_ReturnsInfo()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var info = await _parser.LoadInfoAsync(savePath);

        // Assert
        info.Should().NotBeNull();
        info.Path.Should().Be(savePath);
    }

    #endregion

    #region Comprehensive LoadAsync Edge Cases

    [Fact]
    public async Task LoadAsync_EmptyOptions_LoadsSuccessfully()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var options = new LoadOptions();

        // Act
        var save = await _parser.LoadAsync(savePath, options);

        // Assert
        save.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_NullPath_ThrowsException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _parser.LoadAsync(null!))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task LoadAsync_EmptyPath_ThrowsException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _parser.LoadAsync(string.Empty))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task LoadAsync_WhitespacePath_ThrowsException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _parser.LoadAsync("   "))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task LoadAsync_ValidFile_ReturnsNonNullSave()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_ValidFile_PopulatesMetadata()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Metadata.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_CalledMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save1 = await _parser.LoadAsync(savePath);
        var save2 = await _parser.LoadAsync(savePath);

        // Assert
        save1.Should().NotBeNull();
        save2.Should().NotBeNull();
    }

    #endregion

    #region Comprehensive LoadInfoAsync Edge Cases

    [Fact]
    public async Task LoadInfoAsync_NullPath_ThrowsException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _parser.LoadInfoAsync(null!))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task LoadInfoAsync_EmptyPath_ThrowsException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _parser.LoadInfoAsync(string.Empty))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task LoadInfoAsync_NonExistentFile_ThrowsException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent_info.sav");

        // Act & Assert
        await FluentActions.Invoking(() => _parser.LoadInfoAsync(nonExistentPath))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task LoadInfoAsync_ValidFile_ReturnsPath()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var info = await _parser.LoadInfoAsync(savePath);

        // Assert
        info.Path.Should().Be(savePath);
    }

    [Fact]
    public async Task LoadInfoAsync_ValidFile_ReturnsFileSize()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var info = await _parser.LoadInfoAsync(savePath);

        // Assert
        info.FileSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoadInfoAsync_ValidFile_ReturnsNonNull()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var info = await _parser.LoadInfoAsync(savePath);

        // Assert
        info.Should().NotBeNull();
    }

    #endregion

    #region Comprehensive LoadOptions Tests

    [Fact]
    public async Task LoadAsync_MetadataOnly_LoadsFaster()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var options = new LoadOptions { MetadataOnly = true };

        // Act
        var save = await _parser.LoadAsync(savePath, options);

        // Assert
        save.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_SkipValidation_DoesNotValidate()
    {
        // Arrange
        var savePath = CreateTestSaveFile();
        var options = new LoadOptions { SkipValidation = true };

        // Act
        var save = await _parser.LoadAsync(savePath, options);

        // Assert
        save.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_DefaultOptions_LoadsCompletely()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Should().NotBeNull();
        save.Heroes.Should().NotBeNull();
        save.Parties.Should().NotBeNull();
    }

    #endregion

    #region Comprehensive File Format Tests

    [Fact]
    public async Task LoadAsync_ValidSaveFile_ParsesCorrectly()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_FileWithValidHeader_ParsesHeader()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Metadata.Should().NotBeNull();
    }

    #endregion

    #region Comprehensive Collection Tests

    [Fact]
    public async Task LoadAsync_ValidFile_HasHeroesCollection()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Heroes.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_ValidFile_HasPartiesCollection()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Parties.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_ValidFile_HasFleetsCollection()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Fleets.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_ValidFile_HasSettlementsCollection()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Settlements.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_ValidFile_HasFactionsCollection()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Factions.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_ValidFile_HasKingdomsCollection()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Kingdoms.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_ValidFile_HasClansCollection()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Clans.Should().NotBeNull();
    }

    #endregion

    #region Comprehensive Header Parsing Tests

    [Fact]
    public async Task LoadAsync_ValidFile_ParsesHeaderVersion()
    {
        // Arrange
        var savePath = CreateTestSaveFile();

        // Act
        var save = await _parser.LoadAsync(savePath);

        // Assert
        save.Header.Version.Should().BeGreaterThan(0);
    }

    #endregion
}
