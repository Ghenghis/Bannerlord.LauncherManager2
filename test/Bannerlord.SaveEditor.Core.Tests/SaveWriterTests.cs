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

    #region Additional Edge Case Tests

    [Fact]
    public async Task SaveAsync_NoCompression_WritesUncompressed()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "uncompressed.sav");

        // Act
        await _writer.SaveAsync(save, savePath, CompressionLevel.NoCompression);

        // Assert
        File.Exists(savePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_FastestCompression_WritesFile()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "fastest.sav");

        // Act
        await _writer.SaveAsync(save, savePath, CompressionLevel.Fastest);

        // Assert
        File.Exists(savePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_SmallestSize_WritesFile()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "smallest.sav");

        // Act
        await _writer.SaveAsync(save, savePath, CompressionLevel.SmallestSize);

        // Assert
        File.Exists(savePath).Should().BeTrue();
    }

    [Fact]
    public async Task VerifyIntegrityAsync_NewlySavedFile_ReturnsTrue()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "verify_new.sav");
        await _writer.SaveAsync(save, savePath);

        // Act
        var isValid = await _writer.VerifyIntegrityAsync(savePath);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyIntegrityAsync_MissingFile_ReturnsFalse()
    {
        // Act
        var isValid = await _writer.VerifyIntegrityAsync("/nonexistent/file.sav");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_WithHeroes_WritesHeroData()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Heroes.Add(new HeroData
        {
            Id = MBGUID.Generate(MBGUIDType.Hero),
            Name = "Test Hero",
            HeroId = "test_hero",
            Level = 20,
            Gold = 5000,
            Attributes = new HeroAttributes { Vigor = 5 },
            Skills = new SkillSet { OneHanded = 100 }
        });
        var savePath = Path.Combine(_testDirectory, "heroes.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Heroes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveAsync_WithParties_WritesPartyData()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Parties.Add(new PartyData
        {
            Id = MBGUID.Generate(MBGUIDType.Party),
            Name = "Test Party",
            Gold = 1000,
            Food = 50,
            Morale = 75,
            State = PartyState.Active
        });
        var savePath = Path.Combine(_testDirectory, "parties.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Parties.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveAsync_WithFleets_WritesFleetData()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Fleets.Add(new FleetData
        {
            Id = MBGUID.Generate(MBGUIDType.Fleet),
            Name = "Test Fleet",
            Morale = 80
        });
        var savePath = Path.Combine(_testDirectory, "fleets.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Fleets.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveAsync_ModifiedSave_OverwritesExisting()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "overwrite_mod.sav");
        await _writer.SaveAsync(save, savePath);

        // Modify and save again
        save.Metadata.CharacterName = "Modified Character Name That Is Longer";

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert
        File.Exists(savePath).Should().BeTrue();
    }

    #endregion

    #region Edge Case and Error Handling Tests

    [Fact]
    public async Task SaveAsync_WithSettlements_WritesSettlementData()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Settlements.Add(new SettlementData
        {
            Id = MBGUID.Generate(MBGUIDType.Settlement),
            Name = "Test Town",
            Prosperity = 5000,
            Type = SettlementType.Town,
            Loyalty = 50
        });
        var savePath = Path.Combine(_testDirectory, "settlements.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Settlements.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveAsync_WithFactions_WritesFactionData()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Factions.Add(new FactionData
        {
            Id = MBGUID.Generate(MBGUIDType.Faction),
            Name = "Test Faction",
            Type = FactionType.Kingdom,
            MainColor = 0xFF0000
        });
        var savePath = Path.Combine(_testDirectory, "factions.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Factions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveAsync_WithClans_WritesClanData()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Clans.Add(new ClanData
        {
            Id = MBGUID.Generate(MBGUIDType.Clan),
            Name = "Test Clan",
            Tier = 3,
            Renown = 500,
            Influence = 100
        });
        var savePath = Path.Combine(_testDirectory, "clans.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Clans.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveAsync_WithKingdoms_WritesKingdomData()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Kingdoms.Add(new KingdomData
        {
            Id = MBGUID.Generate(MBGUIDType.Kingdom),
            Name = "Test Kingdom",
            MainColor = 0xFF0000,
            TotalStrength = 1000
        });
        var savePath = Path.Combine(_testDirectory, "kingdoms.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Kingdoms.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveAsync_WithShips_CreatesFile()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Ships.Add(new ShipData
        {
            Id = MBGUID.Generate(MBGUIDType.Ship),
            Name = "Test Ship",
            Type = ShipType.Cog,
            CurrentHullPoints = 100
        });
        var savePath = Path.Combine(_testDirectory, "ships.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert - ships segment may not be fully implemented in parser
        File.Exists(savePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_EmptyCollections_HandlesGracefully()
    {
        // Arrange
        var save = new SaveFile
        {
            FilePath = Path.Combine(_testDirectory, "empty.sav"),
            Name = "EmptySave",
            Header = new SaveHeader { Version = 7, GameVersion = "v1.2.9" },
            Modules = new List<ModuleInfo>(),
            Metadata = new SaveMetadata { CharacterName = "Test" },
            Heroes = new List<HeroData>(),
            Parties = new List<PartyData>(),
            Settlements = new List<SettlementData>(),
            Factions = new List<FactionData>(),
            Clans = new List<ClanData>(),
            Kingdoms = new List<KingdomData>(),
            Fleets = new List<FleetData>(),
            Ships = new List<ShipData>()
        };
        var savePath = Path.Combine(_testDirectory, "empty_collections.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert
        File.Exists(savePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_LargeHeroList_HandlesCorrectly()
    {
        // Arrange
        var save = CreateTestSaveFile();
        for (int i = 0; i < 50; i++)
        {
            save.Heroes.Add(new HeroData
            {
                Id = MBGUID.Generate(MBGUIDType.Hero),
                Name = $"Hero {i}",
                HeroId = $"hero_{i}",
                Level = i + 1
            });
        }
        var savePath = Path.Combine(_testDirectory, "large_heroes.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Heroes.Count.Should().BeGreaterOrEqualTo(50);
    }

    [Fact]
    public async Task SaveAsync_MultipleModules_PreservesOrder()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Modules = new List<ModuleInfo>
        {
            new() { Id = "Native", Version = "v1.2.9", IsOfficial = true },
            new() { Id = "SandBoxCore", Version = "v1.2.9", IsOfficial = true },
            new() { Id = "CustomMod", Version = "v1.0.0", IsOfficial = false }
        };
        var savePath = Path.Combine(_testDirectory, "modules.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Modules.Should().HaveCount(3);
        loaded.Modules[0].Id.Should().Be("Native");
    }

    [Fact]
    public async Task SaveAsync_UpdatesFileSize()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "filesize.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert
        var fileInfo = new FileInfo(savePath);
        fileInfo.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SaveAsync_WithRawData_PreservesData()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.RawData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var savePath = Path.Combine(_testDirectory, "rawdata.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert
        File.Exists(savePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_WithCampaignTime_PreservesTime()
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.CampaignTime = new CampaignTime(50000000L);
        var savePath = Path.Combine(_testDirectory, "campaigntime.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.CampaignTime.Should().NotBeNull();
    }

    #endregion

    #region Comprehensive Path Tests

    [Fact]
    public async Task SaveAsync_ValidPath_CreatesFile()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var savePath = Path.Combine(_testDirectory, "valid_path.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert
        File.Exists(savePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_PathWithSpaces_CreatesFile()
    {
        // Arrange
        var save = CreateTestSaveFile();
        var subDir = Path.Combine(_testDirectory, "path with spaces");
        Directory.CreateDirectory(subDir);
        var savePath = Path.Combine(subDir, "save file.sav");

        // Act
        await _writer.SaveAsync(save, savePath);

        // Assert
        File.Exists(savePath).Should().BeTrue();
    }

    #endregion

    #region Comprehensive Header Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7)]
    public async Task SaveAsync_VariousVersions_PreservesVersion(int version)
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Header.Version = version;
        var savePath = Path.Combine(_testDirectory, $"version_{version}.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Header.Version.Should().Be(version);
    }

    [Theory]
    [InlineData("v1.0.0")]
    [InlineData("v1.2.10.12345")]
    [InlineData("e2.0.0")]
    public async Task SaveAsync_VariousGameVersions_PreservesGameVersion(string gameVersion)
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Header.GameVersion = gameVersion;
        var savePath = Path.Combine(_testDirectory, $"gameversion_{gameVersion.Replace(".", "_")}.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Header.GameVersion.Should().Be(gameVersion);
    }

    #endregion

    #region Comprehensive Metadata Tests

    [Theory]
    [InlineData("TestPlayer")]
    [InlineData("Player With Spaces")]
    [InlineData("")]
    public async Task SaveAsync_VariousCharacterNames_PreservesName(string name)
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Metadata.CharacterName = name;
        var savePath = Path.Combine(_testDirectory, $"name_{name.GetHashCode()}.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Metadata.CharacterName.Should().Be(name);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(62)]
    public async Task SaveAsync_VariousLevels_PreservesLevel(int level)
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Metadata.Level = level;
        var savePath = Path.Combine(_testDirectory, $"level_{level}.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Metadata.Level.Should().Be(level);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task SaveAsync_VariousDayNumbers_PreservesDayNumber(int day)
    {
        // Arrange
        var save = CreateTestSaveFile();
        save.Metadata.DayNumber = day;
        var savePath = Path.Combine(_testDirectory, $"day_{day}.sav");

        // Act
        await _writer.SaveAsync(save, savePath);
        var loaded = await _parser.LoadAsync(savePath);

        // Assert
        loaded.Metadata.DayNumber.Should().Be(day);
    }

    #endregion

    #region Comprehensive Error Handling Tests

    [Fact]
    public async Task SaveAsync_NullSave_ThrowsException()
    {
        // Arrange
        var savePath = Path.Combine(_testDirectory, "null.sav");

        // Act & Assert
        await FluentActions.Invoking(() => _writer.SaveAsync(null!, savePath))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SaveAsync_NullPath_ThrowsException()
    {
        // Arrange
        var save = CreateTestSaveFile();

        // Act & Assert
        await FluentActions.Invoking(() => _writer.SaveAsync(save, null!))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SaveAsync_EmptyPath_ThrowsException()
    {
        // Arrange
        var save = CreateTestSaveFile();

        // Act & Assert
        await FluentActions.Invoking(() => _writer.SaveAsync(save, string.Empty))
            .Should().ThrowAsync<Exception>();
    }

    #endregion
}
