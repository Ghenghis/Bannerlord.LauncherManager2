// <copyright file="SaveParser.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Parsers;

using Bannerlord.SaveEditor.Core.Compression;
using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Interfaces;
using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.WarSails;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

/// <summary>
/// Parses Bannerlord .sav files.
/// </summary>
public sealed class SaveParser
{
    private readonly ILogger<SaveParser>? _logger;
    private readonly IZlibHandler _zlibHandler;

    // Segment identifiers
    private const ushort SegmentCampaignTime = 0x0001;
    private const ushort SegmentHeroes = 0x0010;
    private const ushort SegmentParties = 0x0020;
    private const ushort SegmentSettlements = 0x0030;
    private const ushort SegmentFactions = 0x0040;
    private const ushort SegmentClans = 0x0050;
    private const ushort SegmentKingdoms = 0x0060;
    private const ushort SegmentQuests = 0x0070;
    private const ushort SegmentWorkshops = 0x0080;
    private const ushort SegmentCaravans = 0x0090;
    private const ushort SegmentFleets = 0x0100;
    private const ushort SegmentShips = 0x0101;

    public SaveParser(IZlibHandler? zlibHandler = null, ILogger<SaveParser>? logger = null)
    {
        _zlibHandler = zlibHandler ?? new ZlibHandler();
        _logger = logger;
    }

    /// <summary>
    /// Loads a save file from disk.
    /// </summary>
    public async Task<SaveFile> LoadAsync(string path, LoadOptions? options = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        options ??= new LoadOptions();

        _logger?.LogInformation("Loading save file: {Path}", path);

        if (!File.Exists(path))
            throw new SaveParseException($"Save file not found: {path}");

        var fileInfo = new FileInfo(path);
        var save = new SaveFile
        {
            FilePath = path,
            Name = Path.GetFileNameWithoutExtension(path),
            LastModified = fileInfo.LastWriteTimeUtc,
            FileSize = fileInfo.Length
        };

        await using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Parse header
        save.Header = ParseHeader(reader);
        _logger?.LogDebug("Parsed header: version {Version}, game {GameVersion}", save.Header.Version, save.Header.GameVersion);

        // Parse module list
        save.Modules = ParseModules(reader).ToList();
        _logger?.LogDebug("Found {Count} modules", save.Modules.Count);

        // Parse metadata JSON
        save.Metadata = ParseMetadata(reader);
        _logger?.LogDebug("Character: {Name}, Level {Level}, Day {Day}", save.Metadata.CharacterName, save.Metadata.Level, save.Metadata.DayNumber);

        if (options.MetadataOnly)
            return save;

        // Read compressed data size
        var compressedSize = reader.ReadInt32();
        save.Header.CompressedSize = compressedSize;

        // Read and decompress campaign data
        var compressedData = reader.ReadBytes(compressedSize);
        var decompressedData = await _zlibHandler.DecompressAsync(compressedData, null, ct);
        save.Header.UncompressedSize = decompressedData.Length;

        if (options.KeepRawData)
            save.RawData = decompressedData;

        _logger?.LogDebug("Decompressed {Compressed} bytes to {Decompressed} bytes",
            compressedSize, decompressedData.Length);

        // Parse campaign data segments
        ParseCampaignData(save, decompressedData, options);

        return save;
    }

    /// <summary>
    /// Loads only metadata without full parsing.
    /// </summary>
    public async Task<SaveFileInfo> LoadInfoAsync(string path, CancellationToken ct = default)
    {
        var save = await LoadAsync(path, new LoadOptions { MetadataOnly = true }, ct);

        return new SaveFileInfo
        {
            Path = path,
            Name = save.Name,
            CharacterName = save.Metadata.CharacterName,
            Level = save.Metadata.Level,
            Day = save.Metadata.DayNumber,
            LastModified = save.LastModified,
            FileSize = save.FileSize,
            GameVersion = save.Header.GameVersion,
            HasWarSails = save.Modules.Any(m => m.Id.Equals("WarSails", StringComparison.OrdinalIgnoreCase)),
            ModuleIds = save.Modules.Select(m => m.Id).ToList()
        };
    }

    private SaveHeader ParseHeader(BinaryReader reader)
    {
        var header = new SaveHeader();

        // Read magic number
        var magic = new string(reader.ReadChars(4));
        if (magic != SaveHeader.MagicNumber)
            throw new SaveParseException($"Invalid magic number: expected '{SaveHeader.MagicNumber}', got '{magic}'");

        // Read version
        header.Version = reader.ReadInt32();

        // Read game version string
        var versionLength = reader.ReadInt32();
        header.GameVersion = new string(reader.ReadChars(versionLength));

        return header;
    }

    private IEnumerable<ModuleInfo> ParseModules(BinaryReader reader)
    {
        var moduleCount = reader.ReadInt32();

        for (var i = 0; i < moduleCount; i++)
        {
            var idLength = reader.ReadInt32();
            var id = new string(reader.ReadChars(idLength));

            var versionLength = reader.ReadInt32();
            var version = new string(reader.ReadChars(versionLength));

            var isOfficial = reader.ReadBoolean();

            yield return new ModuleInfo
            {
                Id = id,
                Version = version,
                IsOfficial = isOfficial
            };
        }
    }

    private SaveMetadata ParseMetadata(BinaryReader reader)
    {
        var metadataSize = reader.ReadInt32();
        var metadataBytes = reader.ReadBytes(metadataSize);
        var metadataJson = Encoding.UTF8.GetString(metadataBytes);

        try
        {
            var jsonDoc = JsonDocument.Parse(metadataJson);
            var root = jsonDoc.RootElement;

            return new SaveMetadata
            {
                CharacterName = root.TryGetProperty("CharacterName", out var cn) ? cn.GetString() ?? "" : "",
                Level = root.TryGetProperty("MainHeroLevel", out var lvl) ? lvl.GetInt32() : 0,
                DayNumber = root.TryGetProperty("DayLong", out var day) ? (int)day.GetDouble() : 0,
                PlayTimeSeconds = root.TryGetProperty("PlayTime", out var pt) ? pt.GetDouble() : 0,
                ClanName = root.TryGetProperty("ClanName", out var clan) ? clan.GetString() : null,
                Gold = root.TryGetProperty("Gold", out var gold) ? gold.GetInt32() : 0
            };
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex, "Failed to parse metadata JSON, using defaults");
            return new SaveMetadata();
        }
    }

    private void ParseCampaignData(SaveFile save, byte[] data, LoadOptions options)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream, Encoding.UTF8);

        while (stream.Position < stream.Length - 4)
        {
            var segmentId = reader.ReadUInt16();
            var segmentSize = reader.ReadInt32();
            var segmentStart = stream.Position;

            _logger?.LogTrace("Parsing segment 0x{SegmentId:X4}, size {Size} at position {Position}",
                segmentId, segmentSize, segmentStart);

            try
            {
                switch (segmentId)
                {
                    case SegmentCampaignTime:
                        save.CampaignTime = ParseCampaignTime(reader);
                        break;
                    case SegmentHeroes:
                        save.Heroes = ParseHeroes(reader, segmentSize).ToList();
                        break;
                    case SegmentParties:
                        save.Parties = ParseParties(reader, segmentSize).ToList();
                        break;
                    case SegmentSettlements:
                        save.Settlements = ParseSettlements(reader, segmentSize).ToList();
                        break;
                    case SegmentFactions:
                        save.Factions = ParseFactions(reader, segmentSize).ToList();
                        break;
                    case SegmentClans:
                        save.Clans = ParseClans(reader, segmentSize).ToList();
                        break;
                    case SegmentKingdoms:
                        save.Kingdoms = ParseKingdoms(reader, segmentSize).ToList();
                        break;
                    case SegmentFleets:
                        save.Fleets = ParseFleets(reader, segmentSize).ToList();
                        break;
                    case SegmentShips:
                        save.Ships = ParseShips(reader, segmentSize).ToList();
                        break;
                    default:
                        // Unknown segment - preserve for round-trip
                        stream.Position = segmentStart;
                        var segmentData = reader.ReadBytes(segmentSize);
                        save.UnknownSegments.Add(new UnknownSegment
                        {
                            SegmentId = segmentId,
                            Data = segmentData,
                            OriginalPosition = segmentStart - 6 // Include header
                        });
                        break;
                }
            }
            catch (Exception ex) when (!options.Permissive)
            {
                throw new SaveParseException($"Failed to parse segment 0x{segmentId:X4}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to parse segment 0x{SegmentId:X4}, skipping", segmentId);
                stream.Position = segmentStart + segmentSize;
            }
        }

        // Resolve entity references
        ResolveReferences(save);
    }

    private CampaignTime ParseCampaignTime(BinaryReader reader)
    {
        var ticks = reader.ReadInt64();
        return new CampaignTime(ticks);
    }

    private IEnumerable<HeroData> ParseHeroes(BinaryReader reader, int segmentSize)
    {
        var endPosition = reader.BaseStream.Position + segmentSize;
        var count = reader.ReadInt32();

        for (var i = 0; i < count && reader.BaseStream.Position < endPosition; i++)
        {
            yield return ParseHero(reader);
        }
    }

    private HeroData ParseHero(BinaryReader reader)
    {
        var hero = new HeroData
        {
            Id = new MBGUID(reader.ReadUInt64()),
            HeroId = ReadString(reader),
            Name = ReadString(reader),
            FirstName = ReadNullableString(reader),
            Gender = (Gender)reader.ReadByte(),
            Age = reader.ReadInt32(),
            IsMainHero = reader.ReadBoolean(),
            IsAlive = reader.ReadBoolean(),
            Level = reader.ReadInt32(),
            Experience = reader.ReadInt32(),
            UnspentAttributePoints = reader.ReadInt32(),
            UnspentFocusPoints = reader.ReadInt32(),
            Gold = reader.ReadInt32(),
            Health = reader.ReadSingle(),
            State = (HeroState)reader.ReadByte()
        };

        // Parse attributes
        hero.Attributes = new HeroAttributes
        {
            Vigor = reader.ReadInt32(),
            Control = reader.ReadInt32(),
            Endurance = reader.ReadInt32(),
            Cunning = reader.ReadInt32(),
            Social = reader.ReadInt32(),
            Intelligence = reader.ReadInt32()
        };

        // Parse skills
        hero.Skills = new SkillSet
        {
            OneHanded = reader.ReadInt32(),
            TwoHanded = reader.ReadInt32(),
            Polearm = reader.ReadInt32(),
            Bow = reader.ReadInt32(),
            Crossbow = reader.ReadInt32(),
            Throwing = reader.ReadInt32(),
            Riding = reader.ReadInt32(),
            Athletics = reader.ReadInt32(),
            Crafting = reader.ReadInt32(),
            Scouting = reader.ReadInt32(),
            Tactics = reader.ReadInt32(),
            Roguery = reader.ReadInt32(),
            Charm = reader.ReadInt32(),
            Leadership = reader.ReadInt32(),
            Trade = reader.ReadInt32(),
            Steward = reader.ReadInt32(),
            Medicine = reader.ReadInt32(),
            Engineering = reader.ReadInt32()
        };

        // Parse naval skills if present
        var hasNavalSkills = reader.ReadBoolean();
        if (hasNavalSkills)
        {
            hero.NavalSkills = new NavalSkillSet
            {
                Navigation = reader.ReadInt32(),
                NavalTactics = reader.ReadInt32(),
                NavalStewardship = reader.ReadInt32()
            };
        }

        // Parse perks
        var perkCount = reader.ReadInt32();
        for (var i = 0; i < perkCount; i++)
        {
            hero.UnlockedPerks.Add(ReadString(reader));
        }

        // Parse references
        hero.ClanId = ReadNullableMBGUID(reader);
        hero.PartyId = ReadNullableMBGUID(reader);
        hero.FleetId = ReadNullableMBGUID(reader);

        return hero;
    }

    private IEnumerable<PartyData> ParseParties(BinaryReader reader, int segmentSize)
    {
        var endPosition = reader.BaseStream.Position + segmentSize;
        var count = reader.ReadInt32();

        for (var i = 0; i < count && reader.BaseStream.Position < endPosition; i++)
        {
            yield return ParseParty(reader);
        }
    }

    private PartyData ParseParty(BinaryReader reader)
    {
        var party = new PartyData
        {
            Id = new MBGUID(reader.ReadUInt64()),
            Name = ReadString(reader),
            Type = (PartyType)reader.ReadByte(),
            State = (PartyState)reader.ReadByte(),
            Gold = reader.ReadInt32(),
            Food = reader.ReadSingle(),
            Morale = reader.ReadSingle(),
            PartySizeLimit = reader.ReadInt32(),
            PrisonerLimit = reader.ReadInt32(),
            Position = new Vec2(reader.ReadSingle(), reader.ReadSingle()),
            LeaderId = ReadNullableMBGUID(reader),
            ClanId = ReadNullableMBGUID(reader),
            CurrentSettlementId = ReadNullableMBGUID(reader)
        };

        // Parse troops
        var troopCount = reader.ReadInt32();
        for (var i = 0; i < troopCount; i++)
        {
            party.Troops.Add(new TroopStack
            {
                TroopId = ReadString(reader),
                TroopName = ReadString(reader),
                Count = reader.ReadInt32(),
                WoundedCount = reader.ReadInt32(),
                Tier = reader.ReadInt32(),
                IsHero = reader.ReadBoolean(),
                HeroId = ReadNullableMBGUID(reader)
            });
        }

        // Parse prisoners
        var prisonerCount = reader.ReadInt32();
        for (var i = 0; i < prisonerCount; i++)
        {
            party.Prisoners.Add(new TroopStack
            {
                TroopId = ReadString(reader),
                TroopName = ReadString(reader),
                Count = reader.ReadInt32(),
                WoundedCount = reader.ReadInt32(),
                Tier = reader.ReadInt32(),
                IsHero = reader.ReadBoolean(),
                HeroId = ReadNullableMBGUID(reader)
            });
        }

        return party;
    }

    private IEnumerable<SettlementData> ParseSettlements(BinaryReader reader, int segmentSize)
    {
        var endPosition = reader.BaseStream.Position + segmentSize;
        var count = reader.ReadInt32();

        for (var i = 0; i < count && reader.BaseStream.Position < endPosition; i++)
        {
            yield return new SettlementData
            {
                Id = new MBGUID(reader.ReadUInt64()),
                SettlementId = ReadString(reader),
                Name = ReadString(reader),
                Type = (SettlementType)reader.ReadByte(),
                Position = new Vec2(reader.ReadSingle(), reader.ReadSingle()),
                Prosperity = reader.ReadInt32(),
                Loyalty = reader.ReadInt32(),
                Security = reader.ReadInt32(),
                FoodStocks = reader.ReadInt32(),
                Militia = reader.ReadInt32(),
                Garrison = reader.ReadInt32(),
                WallLevel = reader.ReadInt32()
            };
        }
    }

    private IEnumerable<FactionData> ParseFactions(BinaryReader reader, int segmentSize)
    {
        var endPosition = reader.BaseStream.Position + segmentSize;
        var count = reader.ReadInt32();

        for (var i = 0; i < count && reader.BaseStream.Position < endPosition; i++)
        {
            yield return new FactionData
            {
                Id = new MBGUID(reader.ReadUInt64()),
                FactionId = ReadString(reader),
                Name = ReadString(reader),
                Type = (FactionType)reader.ReadByte()
            };
        }
    }

    private IEnumerable<ClanData> ParseClans(BinaryReader reader, int segmentSize)
    {
        var endPosition = reader.BaseStream.Position + segmentSize;
        var count = reader.ReadInt32();

        for (var i = 0; i < count && reader.BaseStream.Position < endPosition; i++)
        {
            yield return new ClanData
            {
                Id = new MBGUID(reader.ReadUInt64()),
                ClanId = ReadString(reader),
                Name = ReadString(reader),
                Tier = reader.ReadInt32(),
                Renown = reader.ReadInt32(),
                Influence = reader.ReadInt32(),
                Gold = reader.ReadInt32(),
                IsPlayerClan = reader.ReadBoolean()
            };
        }
    }

    private IEnumerable<KingdomData> ParseKingdoms(BinaryReader reader, int segmentSize)
    {
        var endPosition = reader.BaseStream.Position + segmentSize;
        var count = reader.ReadInt32();

        for (var i = 0; i < count && reader.BaseStream.Position < endPosition; i++)
        {
            yield return new KingdomData
            {
                Id = new MBGUID(reader.ReadUInt64()),
                KingdomId = ReadString(reader),
                Name = ReadString(reader)
            };
        }
    }

    private IEnumerable<FleetData> ParseFleets(BinaryReader reader, int segmentSize)
    {
        var endPosition = reader.BaseStream.Position + segmentSize;
        var count = reader.ReadInt32();

        for (var i = 0; i < count && reader.BaseStream.Position < endPosition; i++)
        {
            yield return new FleetData
            {
                Id = new MBGUID(reader.ReadUInt64()),
                Name = ReadString(reader),
                AdmiralId = ReadNullableMBGUID(reader),
                ClanId = ReadNullableMBGUID(reader),
                FlagshipId = ReadNullableMBGUID(reader),
                State = (FleetState)reader.ReadByte(),
                Formation = (FleetFormation)reader.ReadByte(),
                Morale = reader.ReadSingle(),
                Gold = reader.ReadInt32(),
                Position = new NavalPosition(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
            };
        }
    }

    private IEnumerable<ShipData> ParseShips(BinaryReader reader, int segmentSize)
    {
        var endPosition = reader.BaseStream.Position + segmentSize;
        var count = reader.ReadInt32();

        for (var i = 0; i < count && reader.BaseStream.Position < endPosition; i++)
        {
            var ship = new ShipData
            {
                Id = new MBGUID(reader.ReadUInt64()),
                Name = ReadString(reader),
                Type = (ShipType)reader.ReadByte(),
                CurrentHullPoints = reader.ReadInt32(),
                CrewCount = reader.ReadInt32(),
                CrewQuality = (CrewQuality)reader.ReadByte(),
                CrewMorale = reader.ReadSingle(),
                FleetId = ReadNullableMBGUID(reader)
            };

            // Parse upgrades
            var upgradeCount = reader.ReadInt32();
            for (var j = 0; j < upgradeCount; j++)
            {
                ship.Upgrades.Add((ShipUpgrade)reader.ReadInt32());
            }

            // Parse cargo
            var cargoCount = reader.ReadInt32();
            for (var j = 0; j < cargoCount; j++)
            {
                ship.Cargo.Add(new CargoItem
                {
                    ItemId = ReadString(reader),
                    ItemName = ReadString(reader),
                    Count = reader.ReadInt32(),
                    Weight = reader.ReadInt32(),
                    Value = reader.ReadInt32()
                });
            }

            yield return ship;
        }
    }

    private void ResolveReferences(SaveFile save)
    {
        var heroDict = save.Heroes.ToDictionary(h => h.Id);
        var partyDict = save.Parties.ToDictionary(p => p.Id);
        var clanDict = save.Clans.ToDictionary(c => c.Id);
        var fleetDict = save.Fleets.ToDictionary(f => f.Id);
        var shipDict = save.Ships.ToDictionary(s => s.Id);

        // Resolve hero references
        foreach (var hero in save.Heroes)
        {
            if (hero.ClanId.HasValue && clanDict.TryGetValue(hero.ClanId.Value, out var clan))
                hero.Clan = clan;
            if (hero.PartyId.HasValue && partyDict.TryGetValue(hero.PartyId.Value, out var party))
                hero.Party = party;
            if (hero.FleetId.HasValue && fleetDict.TryGetValue(hero.FleetId.Value, out var fleet))
                hero.Fleet = fleet;
        }

        // Resolve party references
        foreach (var party in save.Parties)
        {
            if (party.LeaderId.HasValue && heroDict.TryGetValue(party.LeaderId.Value, out var leader))
                party.Leader = leader;
            if (party.ClanId.HasValue && clanDict.TryGetValue(party.ClanId.Value, out var clan))
                party.Clan = clan;
        }

        // Resolve fleet references
        foreach (var fleet in save.Fleets)
        {
            if (fleet.AdmiralId.HasValue && heroDict.TryGetValue(fleet.AdmiralId.Value, out var admiral))
                fleet.Admiral = admiral;
            if (fleet.ClanId.HasValue && clanDict.TryGetValue(fleet.ClanId.Value, out var clan))
                fleet.Clan = clan;

            // Link ships to fleet
            fleet.Ships = save.Ships.Where(s => s.FleetId == fleet.Id).ToList();
            if (fleet.FlagshipId.HasValue && shipDict.TryGetValue(fleet.FlagshipId.Value, out var flagship))
                fleet.Flagship = flagship;
        }

        // Resolve ship references
        foreach (var ship in save.Ships)
        {
            if (ship.FleetId.HasValue && fleetDict.TryGetValue(ship.FleetId.Value, out var fleet))
                ship.Fleet = fleet;
        }
    }

    private static string ReadString(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        if (length <= 0) return string.Empty;
        return new string(reader.ReadChars(length));
    }

    private static string? ReadNullableString(BinaryReader reader)
    {
        var hasValue = reader.ReadBoolean();
        return hasValue ? ReadString(reader) : null;
    }

    private static MBGUID? ReadNullableMBGUID(BinaryReader reader)
    {
        var hasValue = reader.ReadBoolean();
        return hasValue ? new MBGUID(reader.ReadUInt64()) : null;
    }
}

/// <summary>
/// Exception during save file parsing.
/// </summary>
public class SaveParseException : Exception
{
    public SaveParseException(string message) : base(message) { }
    public SaveParseException(string message, Exception inner) : base(message, inner) { }
}
