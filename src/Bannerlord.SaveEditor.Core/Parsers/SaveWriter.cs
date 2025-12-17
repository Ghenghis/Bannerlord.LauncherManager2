// <copyright file="SaveWriter.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Parsers;

using Bannerlord.SaveEditor.Core.Compression;
using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.WarSails;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>
/// Writes Bannerlord .sav files with atomic operations.
/// </summary>
public sealed class SaveWriter
{
    private readonly ILogger<SaveWriter>? _logger;
    private readonly IZlibHandler _zlibHandler;

    // Segment identifiers (matching SaveParser)
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

    public SaveWriter(IZlibHandler? zlibHandler = null, ILogger<SaveWriter>? logger = null)
    {
        _zlibHandler = zlibHandler ?? new ZlibHandler();
        _logger = logger;
    }

    /// <summary>
    /// Saves a SaveFile to disk using atomic write operations.
    /// </summary>
    public async Task SaveAsync(SaveFile save, string path, CompressionLevel level = CompressionLevel.Optimal, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _logger?.LogInformation("Saving to: {Path}", path);

        // Generate temp file path
        var tempPath = path + ".tmp";
        var backupPath = path + ".bak";

        try
        {
            // Write to temp file first
            await using (var stream = File.Create(tempPath))
            await using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                // Write header
                WriteHeader(writer, save.Header);

                // Write modules
                WriteModules(writer, save.Modules);

                // Write metadata
                WriteMetadata(writer, save);

                // Serialize campaign data
                var campaignData = SerializeCampaignData(save);

                // Compress campaign data
                var compressedData = await _zlibHandler.CompressAsync(campaignData, level, ct);

                // Write compressed size and data
                writer.Write(compressedData.Length);
                writer.Write(compressedData);

                _logger?.LogDebug("Wrote {Uncompressed} bytes compressed to {Compressed} bytes",
                    campaignData.Length, compressedData.Length);
            }

            // Verify temp file integrity
            if (!await VerifyIntegrityAsync(tempPath, ct))
            {
                throw new SaveWriteException("Written file failed integrity check");
            }

            // Atomic replacement: backup existing, rename temp to target
            if (File.Exists(path))
            {
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                File.Move(path, backupPath);
            }

            File.Move(tempPath, path);
            _logger?.LogInformation("Save completed successfully");

            // Clean up backup if everything succeeded
            if (File.Exists(backupPath))
                File.Delete(backupPath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Save failed, attempting recovery");

            // Cleanup temp file
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch { /* ignore cleanup errors */ }
            }

            // Restore backup if original was moved
            if (!File.Exists(path) && File.Exists(backupPath))
            {
                try { File.Move(backupPath, path); }
                catch { /* ignore restore errors */ }
            }

            throw new SaveWriteException($"Failed to save: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Verifies integrity of a save file.
    /// </summary>
    public async Task<bool> VerifyIntegrityAsync(string path, CancellationToken ct = default)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            // Verify magic number
            var magic = new string(reader.ReadChars(4));
            if (magic != SaveHeader.MagicNumber)
            {
                _logger?.LogWarning("Invalid magic number in {Path}", path);
                return false;
            }

            // Verify we can read version
            var version = reader.ReadInt32();
            if (version < 1 || version > 20)
            {
                _logger?.LogWarning("Invalid version {Version} in {Path}", version, path);
                return false;
            }

            // Verify game version string
            var versionLength = reader.ReadInt32();
            if (versionLength <= 0 || versionLength > 100)
            {
                _logger?.LogWarning("Invalid version string length {Length} in {Path}", versionLength, path);
                return false;
            }

            // Skip to compressed data and verify ZLIB header
            reader.ReadChars(versionLength); // game version
            var moduleCount = reader.ReadInt32();
            for (var i = 0; i < moduleCount; i++)
            {
                var idLen = reader.ReadInt32();
                reader.ReadChars(idLen);
                var verLen = reader.ReadInt32();
                reader.ReadChars(verLen);
                reader.ReadBoolean();
            }
            var metaLen = reader.ReadInt32();
            reader.ReadBytes(metaLen);

            var compressedSize = reader.ReadInt32();
            if (compressedSize <= 0 || compressedSize > stream.Length - stream.Position)
            {
                _logger?.LogWarning("Invalid compressed size {Size} in {Path}", compressedSize, path);
                return false;
            }

            // Verify ZLIB header
            var header = reader.ReadBytes(2);
            if (!_zlibHandler.ValidateHeader(header))
            {
                _logger?.LogWarning("Invalid ZLIB header in {Path}", path);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Integrity check failed for {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Computes SHA256 checksum of a file.
    /// </summary>
    public static async Task<string> ComputeChecksumAsync(string path, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private void WriteHeader(BinaryWriter writer, SaveHeader header)
    {
        // Magic number
        writer.Write(SaveHeader.MagicNumber.ToCharArray());

        // Version
        writer.Write(header.Version);

        // Game version string
        writer.Write(header.GameVersion.Length);
        writer.Write(header.GameVersion.ToCharArray());
    }

    private void WriteModules(BinaryWriter writer, IList<ModuleInfo> modules)
    {
        writer.Write(modules.Count);

        foreach (var module in modules)
        {
            writer.Write(module.Id.Length);
            writer.Write(module.Id.ToCharArray());

            writer.Write(module.Version.Length);
            writer.Write(module.Version.ToCharArray());

            writer.Write(module.IsOfficial);
        }
    }

    private void WriteMetadata(BinaryWriter writer, SaveFile save)
    {
        var metadata = new Dictionary<string, object>
        {
            ["CharacterName"] = save.Metadata.CharacterName,
            ["MainHeroLevel"] = save.Metadata.Level,
            ["DayLong"] = (double)save.Metadata.DayNumber,
            ["PlayTime"] = save.Metadata.PlayTimeSeconds,
            ["Gold"] = save.Metadata.Gold
        };

        if (!string.IsNullOrEmpty(save.Metadata.ClanName))
            metadata["ClanName"] = save.Metadata.ClanName;

        var json = JsonSerializer.Serialize(metadata);
        var bytes = Encoding.UTF8.GetBytes(json);

        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private byte[] SerializeCampaignData(SaveFile save)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8);

        // Write campaign time
        WriteSegment(writer, SegmentCampaignTime, w => w.Write(save.CampaignTime.Ticks));

        // Write heroes
        WriteSegment(writer, SegmentHeroes, w => WriteHeroes(w, save.Heroes));

        // Write parties
        WriteSegment(writer, SegmentParties, w => WriteParties(w, save.Parties));

        // Write settlements
        WriteSegment(writer, SegmentSettlements, w => WriteSettlements(w, save.Settlements));

        // Write factions
        WriteSegment(writer, SegmentFactions, w => WriteFactions(w, save.Factions));

        // Write clans
        WriteSegment(writer, SegmentClans, w => WriteClans(w, save.Clans));

        // Write kingdoms
        WriteSegment(writer, SegmentKingdoms, w => WriteKingdoms(w, save.Kingdoms));

        // Write fleets (War Sails)
        if (save.HasWarSails && save.Fleets.Count > 0)
        {
            WriteSegment(writer, SegmentFleets, w => WriteFleets(w, save.Fleets));
            WriteSegment(writer, SegmentShips, w => WriteShips(w, save.Ships));
        }

        // Write unknown segments (round-trip preservation)
        foreach (var segment in save.UnknownSegments)
        {
            writer.Write(segment.SegmentId);
            writer.Write(segment.Data.Length);
            writer.Write(segment.Data);
        }

        return stream.ToArray();
    }

    private void WriteSegment(BinaryWriter writer, ushort segmentId, Action<BinaryWriter> writeContent)
    {
        writer.Write(segmentId);

        // Write placeholder for size
        var sizePosition = writer.BaseStream.Position;
        writer.Write(0);

        // Write content
        var contentStart = writer.BaseStream.Position;
        writeContent(writer);
        var contentEnd = writer.BaseStream.Position;

        // Go back and write actual size
        var size = (int)(contentEnd - contentStart);
        writer.BaseStream.Position = sizePosition;
        writer.Write(size);
        writer.BaseStream.Position = contentEnd;
    }

    private void WriteHeroes(BinaryWriter writer, IList<HeroData> heroes)
    {
        writer.Write(heroes.Count);
        foreach (var hero in heroes)
        {
            WriteHero(writer, hero);
        }
    }

    private void WriteHero(BinaryWriter writer, HeroData hero)
    {
        writer.Write(hero.Id.InternalValue);
        WriteString(writer, hero.HeroId);
        WriteString(writer, hero.Name);
        WriteNullableString(writer, hero.FirstName);
        writer.Write((byte)hero.Gender);
        writer.Write(hero.Age);
        writer.Write(hero.IsMainHero);
        writer.Write(hero.IsAlive);
        writer.Write(hero.Level);
        writer.Write(hero.Experience);
        writer.Write(hero.UnspentAttributePoints);
        writer.Write(hero.UnspentFocusPoints);
        writer.Write(hero.Gold);
        writer.Write(hero.Health);
        writer.Write((byte)hero.State);

        // Write attributes
        writer.Write(hero.Attributes.Vigor);
        writer.Write(hero.Attributes.Control);
        writer.Write(hero.Attributes.Endurance);
        writer.Write(hero.Attributes.Cunning);
        writer.Write(hero.Attributes.Social);
        writer.Write(hero.Attributes.Intelligence);

        // Write skills
        writer.Write(hero.Skills.OneHanded);
        writer.Write(hero.Skills.TwoHanded);
        writer.Write(hero.Skills.Polearm);
        writer.Write(hero.Skills.Bow);
        writer.Write(hero.Skills.Crossbow);
        writer.Write(hero.Skills.Throwing);
        writer.Write(hero.Skills.Riding);
        writer.Write(hero.Skills.Athletics);
        writer.Write(hero.Skills.Crafting);
        writer.Write(hero.Skills.Scouting);
        writer.Write(hero.Skills.Tactics);
        writer.Write(hero.Skills.Roguery);
        writer.Write(hero.Skills.Charm);
        writer.Write(hero.Skills.Leadership);
        writer.Write(hero.Skills.Trade);
        writer.Write(hero.Skills.Steward);
        writer.Write(hero.Skills.Medicine);
        writer.Write(hero.Skills.Engineering);

        // Write naval skills
        writer.Write(hero.NavalSkills != null);
        if (hero.NavalSkills != null)
        {
            writer.Write(hero.NavalSkills.Navigation);
            writer.Write(hero.NavalSkills.NavalTactics);
            writer.Write(hero.NavalSkills.NavalStewardship);
        }

        // Write perks
        writer.Write(hero.UnlockedPerks.Count);
        foreach (var perk in hero.UnlockedPerks)
        {
            WriteString(writer, perk);
        }

        // Write references
        WriteNullableMBGUID(writer, hero.ClanId);
        WriteNullableMBGUID(writer, hero.PartyId);
        WriteNullableMBGUID(writer, hero.FleetId);
    }

    private void WriteParties(BinaryWriter writer, IList<PartyData> parties)
    {
        writer.Write(parties.Count);
        foreach (var party in parties)
        {
            writer.Write(party.Id.InternalValue);
            WriteString(writer, party.Name);
            writer.Write((byte)party.Type);
            writer.Write((byte)party.State);
            writer.Write(party.Gold);
            writer.Write(party.Food);
            writer.Write(party.Morale);
            writer.Write(party.PartySizeLimit);
            writer.Write(party.PrisonerLimit);
            writer.Write(party.Position.X);
            writer.Write(party.Position.Y);
            WriteNullableMBGUID(writer, party.LeaderId);
            WriteNullableMBGUID(writer, party.ClanId);
            WriteNullableMBGUID(writer, party.CurrentSettlementId);

            // Write troops
            writer.Write(party.Troops.Count);
            foreach (var troop in party.Troops)
            {
                WriteString(writer, troop.TroopId);
                WriteString(writer, troop.TroopName);
                writer.Write(troop.Count);
                writer.Write(troop.WoundedCount);
                writer.Write(troop.Tier);
                writer.Write(troop.IsHero);
                WriteNullableMBGUID(writer, troop.HeroId);
            }

            // Write prisoners
            writer.Write(party.Prisoners.Count);
            foreach (var prisoner in party.Prisoners)
            {
                WriteString(writer, prisoner.TroopId);
                WriteString(writer, prisoner.TroopName);
                writer.Write(prisoner.Count);
                writer.Write(prisoner.WoundedCount);
                writer.Write(prisoner.Tier);
                writer.Write(prisoner.IsHero);
                WriteNullableMBGUID(writer, prisoner.HeroId);
            }
        }
    }

    private void WriteSettlements(BinaryWriter writer, IList<SettlementData> settlements)
    {
        writer.Write(settlements.Count);
        foreach (var settlement in settlements)
        {
            writer.Write(settlement.Id.InternalValue);
            WriteString(writer, settlement.SettlementId);
            WriteString(writer, settlement.Name);
            writer.Write((byte)settlement.Type);
            writer.Write(settlement.Position.X);
            writer.Write(settlement.Position.Y);
            writer.Write(settlement.Prosperity);
            writer.Write(settlement.Loyalty);
            writer.Write(settlement.Security);
            writer.Write(settlement.FoodStocks);
            writer.Write(settlement.Militia);
            writer.Write(settlement.Garrison);
            writer.Write(settlement.WallLevel);
        }
    }

    private void WriteFactions(BinaryWriter writer, IList<FactionData> factions)
    {
        writer.Write(factions.Count);
        foreach (var faction in factions)
        {
            writer.Write(faction.Id.InternalValue);
            WriteString(writer, faction.FactionId);
            WriteString(writer, faction.Name);
            writer.Write((byte)faction.Type);
        }
    }

    private void WriteClans(BinaryWriter writer, IList<ClanData> clans)
    {
        writer.Write(clans.Count);
        foreach (var clan in clans)
        {
            writer.Write(clan.Id.InternalValue);
            WriteString(writer, clan.ClanId);
            WriteString(writer, clan.Name);
            writer.Write(clan.Tier);
            writer.Write(clan.Renown);
            writer.Write(clan.Influence);
            writer.Write(clan.Gold);
            writer.Write(clan.IsPlayerClan);
        }
    }

    private void WriteKingdoms(BinaryWriter writer, IList<KingdomData> kingdoms)
    {
        writer.Write(kingdoms.Count);
        foreach (var kingdom in kingdoms)
        {
            writer.Write(kingdom.Id.InternalValue);
            WriteString(writer, kingdom.KingdomId);
            WriteString(writer, kingdom.Name);
        }
    }

    private void WriteFleets(BinaryWriter writer, IList<FleetData> fleets)
    {
        writer.Write(fleets.Count);
        foreach (var fleet in fleets)
        {
            writer.Write(fleet.Id.InternalValue);
            WriteString(writer, fleet.Name);
            WriteNullableMBGUID(writer, fleet.AdmiralId);
            WriteNullableMBGUID(writer, fleet.ClanId);
            WriteNullableMBGUID(writer, fleet.FlagshipId);
            writer.Write((byte)fleet.State);
            writer.Write((byte)fleet.Formation);
            writer.Write(fleet.Morale);
            writer.Write(fleet.Gold);
            writer.Write(fleet.Position.X);
            writer.Write(fleet.Position.Y);
            writer.Write(fleet.Position.Heading);
        }
    }

    private void WriteShips(BinaryWriter writer, IList<ShipData> ships)
    {
        writer.Write(ships.Count);
        foreach (var ship in ships)
        {
            writer.Write(ship.Id.InternalValue);
            WriteString(writer, ship.Name);
            writer.Write((byte)ship.Type);
            writer.Write(ship.CurrentHullPoints);
            writer.Write(ship.CrewCount);
            writer.Write((byte)ship.CrewQuality);
            writer.Write(ship.CrewMorale);
            WriteNullableMBGUID(writer, ship.FleetId);

            // Write upgrades
            writer.Write(ship.Upgrades.Count);
            foreach (var upgrade in ship.Upgrades)
            {
                writer.Write((int)upgrade);
            }

            // Write cargo
            writer.Write(ship.Cargo.Count);
            foreach (var cargo in ship.Cargo)
            {
                WriteString(writer, cargo.ItemId);
                WriteString(writer, cargo.ItemName);
                writer.Write(cargo.Count);
                writer.Write(cargo.Weight);
                writer.Write(cargo.Value);
            }
        }
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        value ??= string.Empty;
        writer.Write(value.Length);
        if (value.Length > 0)
            writer.Write(value.ToCharArray());
    }

    private static void WriteNullableString(BinaryWriter writer, string? value)
    {
        writer.Write(value != null);
        if (value != null)
            WriteString(writer, value);
    }

    private static void WriteNullableMBGUID(BinaryWriter writer, MBGUID? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
            writer.Write(value.Value.InternalValue);
    }
}

/// <summary>
/// Exception during save file writing.
/// </summary>
public class SaveWriteException : Exception
{
    public SaveWriteException(string message) : base(message) { }
    public SaveWriteException(string message, Exception inner) : base(message, inner) { }
}
