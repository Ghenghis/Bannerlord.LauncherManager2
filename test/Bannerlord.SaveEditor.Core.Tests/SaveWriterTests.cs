// <copyright file="SaveWriterTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Compression;
using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Interfaces;
using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.Parsers;
using Bannerlord.SaveEditor.Core.WarSails;
using FluentAssertions;
using System.IO.Compression;
using Xunit;

/// <summary>
/// Tests for SaveWriter.
/// </summary>
public sealed class SaveWriterTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SaveWriter _writer;
    private readonly SaveParser _parser;

    public SaveWriterTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"SaveWriterTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _writer = new SaveWriter();
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

    private SaveFile CreateTestSaveFile()
    {
        return new SaveFile
        {
            FilePath = Path.Combine(_testDirectory, "test.sav"),
            Name = "TestSave",
            LastModified = DateTime.UtcNow,
            Header = new SaveHeader
            {
                Version = 7,
                GameVersion = "v1.2.9"
            },
            Modules = new List<ModuleInfo>
            {
                new() { Id = "Native", Version = "v1.2.9", IsOfficial = true },
                new() { Id = "SandBoxCore", Version = "v1.2.9", IsOfficial = true }
            },
            Metadata = new SaveMetadata
            {
                CharacterName = "TestHero",
                Level = 25,
                DayNumber = 150,
                PlayTimeSeconds = 72000,
                ClanName = "TestClan",
                Gold = 100000
            },
            CampaignTime = new CampaignTime(10000000L),
            Heroes = new List<HeroData>
            {
                CreateTestHero(true)
            },
            Parties = new List<PartyData>
            {
                CreateTestParty()
            },
            Settlements = new List<SettlementData>(),
            Factions = new List<FactionData>(),
            Clans = new List<ClanData>(),
            Kingdoms = new List<KingdomData>(),
            Fleets = new List<FleetData>(),
            Ships = new List<ShipData>()
        };
    }

    private HeroData CreateTestHero(bool isMainHero = false)
    {
        return new HeroData
        {
            Id = MBGUID.Generate(MBGUIDType.Hero),
            HeroId = "test_hero",
            Name = "Test Hero",
            Gender = Gender.Male,
            Age = 30,
            IsMainHero = isMainHero,
            IsAlive = true,
            Level = 25,
            Gold = 50000,
            Health = 100f,
            State = HeroState.Active,
            Attributes = new HeroAttributes
            {
                Vigor = 5, Control = 4, Endurance = 5,
                Cunning = 3, Social = 4, Intelligence = 5
            },
            Skills = new SkillSet
            {
                OneHanded = 150, TwoHanded = 100, Polearm = 120,
                Bow = 80, Crossbow = 50, Throwing = 60,
                Riding = 140, Athletics = 130, Crafting = 90,
                Scouting = 100, Tactics = 110, Roguery = 70,
                Charm = 80, Leadership = 150, Trade = 85,
                Steward = 95, Medicine = 75, Engineering = 60
            }
        };
    }

    private PartyData CreateTestParty()
    {
        return new PartyData
        {
            Id = MBGUID.Generate(MBGUIDType.Party),
            Name = "Test Party",
            Type = PartyType.Lord,
            State = PartyState.Active,
            Gold = 10000,
            Food = 50f,
            Morale = 75f,
            PartySizeLimit = 100,
            PrisonerLimit = 20,
            Position = new Vec2(100f, 200f)
        };
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_CreatesValidFile()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "output.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert
        File.Exists(savePath).Should().BeTrue();
        new FileInfo(savePath).Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SaveAsync_WritesCorrectMagicNumber()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "magic.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert
        using var stream = File.OpenRead(savePath);
        using var reader = new BinaryReader(stream);
        var magic = new string(reader.ReadChars(4));
        magic.Should().Be("TWSV");
    }

    [Fact]
    public async Task SaveAsync_WritesCorrectVersion()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Header.Version = 8;
        var savePath = Path.Combine(_testDirectory, "version.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert
        using var stream = File.OpenRead(savePath);
        using var reader = new BinaryReader(stream);
        reader.ReadChars(4); // Skip magic
        var version = reader.ReadInt32();
        version.Should().Be(8);
    }

    [Fact]
    public async Task SaveAsync_WritesGameVersion()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Header.GameVersion = "v1.3.0-beta";
        var savePath = Path.Combine(_testDirectory, "gameversion.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert
        using var stream = File.OpenRead(savePath);
        using var reader = new BinaryReader(stream);
        reader.ReadChars(4); // Skip magic
        reader.ReadInt32();  // Skip version
        var versionLength = reader.ReadInt32();
        var gameVersion = new string(reader.ReadChars(versionLength));
        gameVersion.Should().Be("v1.3.0-beta");
    }

    [Fact]
    public async Task SaveAsync_WritesModules()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Modules = new List<ModuleInfo>
        {
            new() { Id = "TestMod1", Version = "v1.0", IsOfficial = false },
            new() { Id = "TestMod2", Version = "v2.0", IsOfficial = true }
        };
        var savePath = Path.Combine(_testDirectory, "modules.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert - verify by re-loading
        var loaded = await _parser.LoadAsync(savePath, new LoadOptions { MetadataOnly = true });
        loaded.Modules.Should().HaveCount(2);
        loaded.Modules.Should().Contain(m => m.Id == "TestMod1");
        loaded.Modules.Should().Contain(m => m.Id == "TestMod2" && m.IsOfficial);
    }

    #endregion

    #region Atomic Operations Tests

    [Fact]
    public async Task SaveAsync_CreatesTemporaryFile()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "atomic.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert - temp file should be cleaned up
        File.Exists(savePath + ".tmp").Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_CleansUpOnSuccess()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "cleanup.sav");

        // Create existing file to trigger backup
        await File.WriteAllTextAsync(savePath, "existing");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert - backup should be cleaned up
        File.Exists(savePath + ".bak").Should().BeFalse();
        File.Exists(savePath + ".tmp").Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingFile()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "overwrite.sav");

        // Create existing file
        await File.WriteAllTextAsync(savePath, "old content");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert
        var content = await File.ReadAllBytesAsync(savePath);
        System.Text.Encoding.ASCII.GetString(content, 0, 4).Should().Be("TWSV");
    }

    #endregion

    #region Integrity Verification Tests

    [Fact]
    public async Task VerifyIntegrityAsync_ValidFile_ReturnsTrue()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "valid.sav");
        await _writer.SaveAsync(save, savePath);

        // Act
        var isValid = await _writer.VerifyIntegrityAsync(savePath);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyIntegrityAsync_InvalidMagic_ReturnsFalse()
    {
        // Arrange
        var savePath = Path.Combine(_testDirectory, "invalid.sav");
        await File.WriteAllTextAsync(savePath, "XXXX invalid content");

        // Act
        var isValid = await _writer.VerifyIntegrityAsync(savePath);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyIntegrityAsync_NonexistentFile_ReturnsFalse()
    {
        // Arrange
        var savePath = Path.Combine(_testDirectory, "nonexistent.sav");

        // Act
        var isValid = await _writer.VerifyIntegrityAsync(savePath);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Checksum Tests

    [Fact]
    public async Task ComputeChecksumAsync_ReturnsConsistentHash()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "checksum.sav");
        await _writer.SaveAsync(save, savePath);

        // Act
        var hash1 = await SaveWriter.ComputeChecksumAsync(savePath);
        var hash2 = await SaveWriter.ComputeChecksumAsync(savePath);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64); // SHA256 hex string
    }

    [Fact]
    public async Task ComputeChecksumAsync_DifferentFilesHaveDifferentHashes()
    {
        // Arrange
        var save1 = CreateTestSaveFile();
        save1.Metadata.CharacterName = "Hero1";
        var path1 = Path.Combine(_testDirectory, "checksum1.sav");
        await _writer.SaveAsync(save1, path1);

        var save2 = CreateTestSaveFile();
        save2.Metadata.CharacterName = "Hero2";
        var path2 = Path.Combine(_testDirectory, "checksum2.sav");
        await _writer.SaveAsync(save2, path2);

        // Act
        var hash1 = await SaveWriter.ComputeChecksumAsync(path1);
        var hash2 = await SaveWriter.ComputeChecksumAsync(path2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    #endregion

    #region Compression Tests

    [Fact]
    public async Task SaveAsync_CompressesData()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "compressed.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert - verify by loading and checking sizes
        var loaded = await _parser.LoadAsync(savePath);
        loaded.Header.CompressedSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SaveAsync_OptimalCompression_ProducesValidFile()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "optimal.sav");

        // Act
        await _writer.SaveAsync(save, savePath, CompressionLevel.Optimal);

        // Assert
        var isValid = await _writer.VerifyIntegrityAsync(savePath);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_FastestCompression_ProducesValidFile()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "fastest.sav");

        // Act
        await _writer.SaveAsync(save, savePath, CompressionLevel.Fastest);

        // Assert
        var isValid = await _writer.VerifyIntegrityAsync(savePath);
        isValid.Should().BeTrue();
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public async Task RoundTrip_PreservesHeader()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Header.Version = 7;
        save.Header.GameVersion = "v1.2.10.12345";
        var savePath = Path.Combine(_testDirectory, "roundtrip.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Header.Version.Should().Be(7);
        loaded.Header.GameVersion.Should().Be("v1.2.10.12345");
    }

    [Fact]
    public async Task RoundTrip_PreservesMetadata()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Metadata = new SaveMetadata
        {
            CharacterName = "Sir Roundtrip",
            Level = 50,
            DayNumber = 500,
            Gold = 999999,
            ClanName = "RoundClan"
        };
        var savePath = Path.Combine(_testDirectory, "roundtrip_meta.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Metadata.CharacterName.Should().Be("Sir Roundtrip");
        loaded.Metadata.Level.Should().Be(50);
        loaded.Metadata.DayNumber.Should().Be(500);
        loaded.Metadata.Gold.Should().Be(999999);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task SaveAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "cancelled.sav");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await FluentActions.Invoking(() => _writer.SaveAsync(save, savePath, CompressionLevel.Optimal, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}
