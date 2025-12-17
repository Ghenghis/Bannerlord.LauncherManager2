// <copyright file="SaveFile.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Models;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.WarSails;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a complete Bannerlord save game file.
/// </summary>
public sealed class SaveFile
{
    /// <summary>
    /// Gets or sets the file path of this save.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of this save.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last modification time.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// Gets or sets the save header information.
    /// </summary>
    public SaveHeader Header { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of active modules when save was created.
    /// </summary>
    public IList<ModuleInfo> Modules { get; set; } = new List<ModuleInfo>();

    /// <summary>
    /// Gets or sets the save metadata.
    /// </summary>
    public SaveMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the campaign time.
    /// </summary>
    public CampaignTime CampaignTime { get; set; } = new();

    /// <summary>
    /// Gets the main hero (player character).
    /// </summary>
    public HeroData? MainHero => Heroes.FirstOrDefault(h => h.IsMainHero);

    /// <summary>
    /// Gets or sets all heroes in the save.
    /// </summary>
    public IList<HeroData> Heroes { get; set; } = new List<HeroData>();

    /// <summary>
    /// Gets or sets all parties in the save.
    /// </summary>
    public IList<PartyData> Parties { get; set; } = new List<PartyData>();

    /// <summary>
    /// Gets or sets all settlements in the save.
    /// </summary>
    public IList<SettlementData> Settlements { get; set; } = new List<SettlementData>();

    /// <summary>
    /// Gets or sets all factions in the save.
    /// </summary>
    public IList<FactionData> Factions { get; set; } = new List<FactionData>();

    /// <summary>
    /// Gets or sets all clans in the save.
    /// </summary>
    public IList<ClanData> Clans { get; set; } = new List<ClanData>();

    /// <summary>
    /// Gets or sets all kingdoms in the save.
    /// </summary>
    public IList<KingdomData> Kingdoms { get; set; } = new List<KingdomData>();

    /// <summary>
    /// Gets or sets all active quests.
    /// </summary>
    public IList<QuestData> Quests { get; set; } = new List<QuestData>();

    /// <summary>
    /// Gets or sets all workshops.
    /// </summary>
    public IList<WorkshopData> Workshops { get; set; } = new List<WorkshopData>();

    /// <summary>
    /// Gets or sets all caravans.
    /// </summary>
    public IList<CaravanData> Caravans { get; set; } = new List<CaravanData>();

    /// <summary>
    /// Gets whether this save has War Sails expansion data.
    /// </summary>
    [JsonIgnore]
    public bool HasWarSails => NavalData is not null || Fleets.Count > 0 ||
                               Modules.Any(m => m.Id.Equals("WarSails", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets or sets the naval campaign data (War Sails).
    /// </summary>
    public NavalCampaignData? NavalData { get; set; }

    /// <summary>
    /// Gets or sets all fleets (War Sails).
    /// </summary>
    public IList<FleetData> Fleets { get; set; } = new List<FleetData>();

    /// <summary>
    /// Gets or sets all ships (War Sails).
    /// </summary>
    public IList<ShipData> Ships { get; set; } = new List<ShipData>();

    /// <summary>
    /// Gets or sets unknown segments preserved for round-trip fidelity.
    /// </summary>
    public IList<UnknownSegment> UnknownSegments { get; set; } = new List<UnknownSegment>();

    /// <summary>
    /// Gets or sets the raw decompressed data bytes (for debugging).
    /// </summary>
    [JsonIgnore]
    public byte[]? RawData { get; set; }

    /// <summary>
    /// Gets or sets the validation report.
    /// </summary>
    [JsonIgnore]
    public Bannerlord.SaveEditor.Core.Validation.ValidationReport? ValidationReport { get; set; }

    /// <summary>
    /// Creates a shallow copy of this save file.
    /// </summary>
    public SaveFile Clone() => new()
    {
        FilePath = FilePath,
        Name = Name,
        LastModified = LastModified,
        FileSize = FileSize,
        Header = Header,
        Modules = new List<ModuleInfo>(Modules),
        Metadata = Metadata,
        CampaignTime = CampaignTime,
        Heroes = new List<HeroData>(Heroes),
        Parties = new List<PartyData>(Parties),
        Settlements = new List<SettlementData>(Settlements),
        Factions = new List<FactionData>(Factions),
        Clans = new List<ClanData>(Clans),
        Kingdoms = new List<KingdomData>(Kingdoms),
        Quests = new List<QuestData>(Quests),
        Workshops = new List<WorkshopData>(Workshops),
        Caravans = new List<CaravanData>(Caravans),
        NavalData = NavalData,
        Fleets = new List<FleetData>(Fleets),
        Ships = new List<ShipData>(Ships),
        UnknownSegments = new List<UnknownSegment>(UnknownSegments),
        RawData = RawData
    };
}

/// <summary>
/// Save file header information.
/// </summary>
public sealed class SaveHeader
{
    /// <summary>
    /// Magic number identifying Bannerlord saves: "TWSV"
    /// </summary>
    public const string MagicNumber = "TWSV";

    /// <summary>
    /// Gets or sets the save format version.
    /// </summary>
    public int Version { get; set; } = 7;

    /// <summary>
    /// Gets or sets the game version string.
    /// </summary>
    public string GameVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets the parsed game version.
    /// </summary>
    [JsonIgnore]
    public Version? ParsedGameVersion
    {
        get
        {
            if (string.IsNullOrEmpty(GameVersion)) return null;
            var versionPart = GameVersion.TrimStart('v', 'e').Split('-')[0];
            return System.Version.TryParse(versionPart, out var v) ? v : null;
        }
    }

    /// <summary>
    /// Gets or sets the compressed data size.
    /// </summary>
    public long CompressedSize { get; set; }

    /// <summary>
    /// Gets or sets the uncompressed data size.
    /// </summary>
    public long UncompressedSize { get; set; }
}

/// <summary>
/// Module information from save header.
/// </summary>
public sealed class ModuleInfo
{
    /// <summary>
    /// Gets or sets the module ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the module version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the module is official.
    /// </summary>
    public bool IsOfficial { get; set; }
}

/// <summary>
/// Save metadata from JSON header.
/// </summary>
public sealed class SaveMetadata
{
    /// <summary>
    /// Gets or sets the character name.
    /// </summary>
    public string CharacterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the character level.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the in-game day number.
    /// </summary>
    public int DayNumber { get; set; }

    /// <summary>
    /// Gets or sets the total play time in seconds.
    /// </summary>
    public double PlayTimeSeconds { get; set; }

    /// <summary>
    /// Gets the formatted play time.
    /// </summary>
    [JsonIgnore]
    public TimeSpan PlayTime => TimeSpan.FromSeconds(PlayTimeSeconds);

    /// <summary>
    /// Gets or sets the clan name.
    /// </summary>
    public string? ClanName { get; set; }

    /// <summary>
    /// Gets or sets the main hero gold.
    /// </summary>
    public int Gold { get; set; }
}

/// <summary>
/// Represents an unknown data segment preserved for round-trip fidelity.
/// </summary>
public sealed class UnknownSegment
{
    /// <summary>
    /// Gets or sets the segment identifier.
    /// </summary>
    public ushort SegmentId { get; set; }

    /// <summary>
    /// Gets or sets the raw segment data.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the segment position in the original file.
    /// </summary>
    public long OriginalPosition { get; set; }
}
