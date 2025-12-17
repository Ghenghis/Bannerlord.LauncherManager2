// <copyright file="FleetData.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.WarSails;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Models;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a naval fleet (War Sails expansion).
/// </summary>
public sealed class FleetData : IEditable
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public MBGUID Id { get; set; }

    /// <summary>
    /// Gets or sets the fleet name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fleet admiral (commander).
    /// </summary>
    [JsonIgnore]
    public HeroData? Admiral { get; set; }

    /// <summary>
    /// Gets or sets the admiral ID reference.
    /// </summary>
    public MBGUID? AdmiralId { get; set; }

    /// <summary>
    /// Gets or sets the owning clan.
    /// </summary>
    [JsonIgnore]
    public ClanData? Clan { get; set; }

    /// <summary>
    /// Gets or sets the clan ID reference.
    /// </summary>
    public MBGUID? ClanId { get; set; }

    /// <summary>
    /// Gets or sets the ships in this fleet.
    /// </summary>
    public IList<ShipData> Ships { get; set; } = new List<ShipData>();

    /// <summary>
    /// Gets or sets the flagship.
    /// </summary>
    [JsonIgnore]
    public ShipData? Flagship { get; set; }

    /// <summary>
    /// Gets or sets the flagship ID reference.
    /// </summary>
    public MBGUID? FlagshipId { get; set; }

    /// <summary>
    /// Gets the total crew across all ships.
    /// </summary>
    public int TotalCrewCount => Ships.Sum(s => s.CrewCount);

    /// <summary>
    /// Gets the total cargo capacity across all ships.
    /// </summary>
    public int TotalCargoCapacity => Ships.Sum(s => s.CargoCapacity);

    /// <summary>
    /// Gets the current total cargo weight.
    /// </summary>
    public int TotalCargoWeight => Ships.Sum(s => s.CurrentCargoWeight);

    /// <summary>
    /// Gets or sets the fleet's current position.
    /// </summary>
    public NavalPosition Position { get; set; } = new();

    /// <summary>
    /// Gets or sets the current sea region.
    /// </summary>
    [JsonIgnore]
    public SeaRegion? CurrentRegion { get; set; }

    /// <summary>
    /// Gets or sets the region ID reference.
    /// </summary>
    public string? CurrentRegionId { get; set; }

    /// <summary>
    /// Gets or sets the current port (if docked).
    /// </summary>
    [JsonIgnore]
    public Port? CurrentPort { get; set; }

    /// <summary>
    /// Gets or sets the port ID reference.
    /// </summary>
    public MBGUID? CurrentPortId { get; set; }

    /// <summary>
    /// Gets or sets the fleet state.
    /// </summary>
    public FleetState State { get; set; }

    /// <summary>
    /// Gets or sets the fleet formation.
    /// </summary>
    public FleetFormation Formation { get; set; }

    /// <summary>
    /// Gets or sets the fleet morale (0-100).
    /// </summary>
    public float Morale { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the food supplies.
    /// </summary>
    public float FoodSupplies { get; set; }

    /// <summary>
    /// Gets or sets the gold treasury.
    /// </summary>
    public int Gold { get; set; }

    /// <summary>
    /// Gets the average fleet speed.
    /// </summary>
    public float AverageSpeed => Ships.Count > 0 ? Ships.Average(s => s.CurrentSpeed) : 0;

    /// <summary>
    /// Gets the slowest ship speed (fleet moves at slowest ship speed).
    /// </summary>
    public float FleetSpeed => Ships.Count > 0 ? Ships.Min(s => s.CurrentSpeed) : 0;

    /// <inheritdoc />
    public bool IsDirty { get; set; }

    /// <inheritdoc />
    public void MarkDirty() => IsDirty = true;
}

/// <summary>
/// Represents a single ship (War Sails expansion).
/// </summary>
public sealed class ShipData : IEditable
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public MBGUID Id { get; set; }

    /// <summary>
    /// Gets or sets the ship name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ship type.
    /// </summary>
    public ShipType Type { get; set; }

    /// <summary>
    /// Gets the default ship class based on type.
    /// </summary>
    public ShipClass DefaultClass => Type switch
    {
        ShipType.Snekkja => ShipClass.Light,
        ShipType.Cog or ShipType.Knarr => ShipClass.Medium,
        ShipType.Longship or ShipType.Galley => ShipClass.Medium,
        ShipType.Warship or ShipType.Carrack => ShipClass.Heavy,
        ShipType.ManOfWar => ShipClass.Flagship,
        _ => ShipClass.Light
    };

    /// <summary>
    /// Gets or sets the ship class (can be overridden from default).
    /// </summary>
    public ShipClass ShipClass { get; set; } = ShipClass.Standard;

    /// <summary>
    /// Gets the base hull points for a ship type.
    /// </summary>
    public static int GetBaseHullPoints(ShipType type) => type switch
    {
        ShipType.Snekkja => 300,
        ShipType.Cog => 500,
        ShipType.Knarr => 450,
        ShipType.Longship => 600,
        ShipType.Galley => 700,
        ShipType.Warship => 1000,
        ShipType.Carrack => 1200,
        ShipType.ManOfWar => 1500,
        _ => 400
    };

    /// <summary>
    /// Gets the base crew capacity for a ship type.
    /// </summary>
    public static int GetBaseCrewCapacity(ShipType type) => type switch
    {
        ShipType.Snekkja => 20,
        ShipType.Cog => 25,
        ShipType.Knarr => 30,
        ShipType.Longship => 50,
        ShipType.Galley => 80,
        ShipType.Warship => 100,
        ShipType.Carrack => 120,
        ShipType.ManOfWar => 200,
        _ => 30
    };

    /// <summary>
    /// Gets the base cargo capacity for a ship type.
    /// </summary>
    public static int GetBaseCargoCapacity(ShipType type) => type switch
    {
        ShipType.Snekkja => 100,
        ShipType.Cog => 500,
        ShipType.Knarr => 400,
        ShipType.Longship => 200,
        ShipType.Galley => 150,
        ShipType.Warship => 250,
        ShipType.Carrack => 800,
        ShipType.ManOfWar => 400,
        _ => 200
    };

    /// <summary>
    /// Gets or sets the current hull points.
    /// </summary>
    public int CurrentHullPoints { get; set; }

    /// <summary>
    /// Gets the maximum hull points based on type.
    /// </summary>
    public int MaxHullPoints => GetMaxHullPoints(Type);

    /// <summary>
    /// Gets the hull integrity percentage.
    /// </summary>
    public float HullIntegrity => MaxHullPoints > 0 ? (float)CurrentHullPoints / MaxHullPoints : 0;

    /// <summary>
    /// Gets or sets the current crew count.
    /// </summary>
    public int CrewCount { get; set; }

    /// <summary>
    /// Gets the crew capacity based on type.
    /// </summary>
    public int CrewCapacity => GetCrewCapacity(Type);

    /// <summary>
    /// Gets or sets the crew quality.
    /// </summary>
    public CrewQuality CrewQuality { get; set; } = CrewQuality.Regular;

    /// <summary>
    /// Gets or sets the crew morale (0-100).
    /// </summary>
    public float CrewMorale { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the cargo items.
    /// </summary>
    public IList<CargoItem> Cargo { get; set; } = new List<CargoItem>();

    /// <summary>
    /// Gets the cargo capacity based on type.
    /// </summary>
    public int CargoCapacity => GetCargoCapacity(Type);

    /// <summary>
    /// Gets the current cargo weight.
    /// </summary>
    public int CurrentCargoWeight => Cargo.Sum(c => c.Weight * c.Count);

    /// <summary>
    /// Gets the remaining cargo capacity.
    /// </summary>
    public int RemainingCargoCapacity => CargoCapacity - CurrentCargoWeight;

    /// <summary>
    /// Gets or sets the installed upgrades.
    /// </summary>
    public ISet<ShipUpgrade> Upgrades { get; set; } = new HashSet<ShipUpgrade>();

    /// <summary>
    /// Gets or sets the installed weapons.
    /// </summary>
    public IList<ShipWeapon> Weapons { get; set; } = new List<ShipWeapon>();

    /// <summary>
    /// Gets the base speed for this ship type.
    /// </summary>
    public float BaseSpeed => GetBaseSpeed(Type);

    /// <summary>
    /// Gets the current speed (affected by hull, crew, cargo, upgrades).
    /// </summary>
    public float CurrentSpeed
    {
        get
        {
            var speed = BaseSpeed;

            // Hull damage penalty
            speed *= 0.5f + (HullIntegrity * 0.5f);

            // Crew efficiency
            var crewRatio = CrewCapacity > 0 ? (float)CrewCount / CrewCapacity : 0;
            speed *= 0.3f + (crewRatio * 0.7f);

            // Cargo weight penalty
            var cargoRatio = CargoCapacity > 0 ? (float)CurrentCargoWeight / CargoCapacity : 0;
            speed *= 1f - (cargoRatio * 0.3f);

            // Speed sail upgrade
            if (Upgrades.Contains(ShipUpgrade.SpeedSails))
                speed *= 1.15f;

            return speed;
        }
    }

    /// <summary>
    /// Gets the base maneuverability for this ship type.
    /// </summary>
    public float Maneuverability => GetManeuverability(Type);

    /// <summary>
    /// Gets or sets the owning fleet.
    /// </summary>
    [JsonIgnore]
    public FleetData? Fleet { get; set; }

    /// <summary>
    /// Gets or sets the fleet ID reference.
    /// </summary>
    public MBGUID? FleetId { get; set; }

    /// <inheritdoc />
    public bool IsDirty { get; set; }

    /// <inheritdoc />
    public void MarkDirty() => IsDirty = true;

    private static int GetMaxHullPoints(ShipType type) => type switch
    {
        ShipType.Snekkja => 300,
        ShipType.Cog => 500,
        ShipType.Knarr => 450,
        ShipType.Longship => 600,
        ShipType.Galley => 700,
        ShipType.Warship => 1000,
        ShipType.Carrack => 1200,
        ShipType.ManOfWar => 1500,
        _ => 400
    };

    private static int GetCrewCapacity(ShipType type) => type switch
    {
        ShipType.Snekkja => 20,
        ShipType.Cog => 25,
        ShipType.Knarr => 30,
        ShipType.Longship => 50,
        ShipType.Galley => 80,
        ShipType.Warship => 100,
        ShipType.Carrack => 120,
        ShipType.ManOfWar => 200,
        _ => 30
    };

    private static int GetCargoCapacity(ShipType type) => type switch
    {
        ShipType.Snekkja => 100,
        ShipType.Cog => 500,
        ShipType.Knarr => 400,
        ShipType.Longship => 200,
        ShipType.Galley => 150,
        ShipType.Warship => 250,
        ShipType.Carrack => 800,
        ShipType.ManOfWar => 400,
        _ => 200
    };

    private static float GetBaseSpeed(ShipType type) => type switch
    {
        ShipType.Snekkja => 12f,
        ShipType.Cog => 6f,
        ShipType.Knarr => 7f,
        ShipType.Longship => 10f,
        ShipType.Galley => 9f,
        ShipType.Warship => 7f,
        ShipType.Carrack => 5f,
        ShipType.ManOfWar => 4f,
        _ => 6f
    };

    private static float GetManeuverability(ShipType type) => type switch
    {
        ShipType.Snekkja => 0.9f,
        ShipType.Cog => 0.4f,
        ShipType.Knarr => 0.5f,
        ShipType.Longship => 0.8f,
        ShipType.Galley => 0.7f,
        ShipType.Warship => 0.5f,
        ShipType.Carrack => 0.3f,
        ShipType.ManOfWar => 0.2f,
        _ => 0.5f
    };
}

/// <summary>
/// Ship types available in War Sails.
/// </summary>
public enum ShipType
{
    /// <summary>Light scout/raider vessel.</summary>
    Snekkja,
    /// <summary>Standard trading vessel.</summary>
    Cog,
    /// <summary>Versatile cargo ship.</summary>
    Knarr,
    /// <summary>Fast raiding longship.</summary>
    Longship,
    /// <summary>Oar-powered warship.</summary>
    Galley,
    /// <summary>Heavy combat vessel.</summary>
    Warship,
    /// <summary>Large trade/war ship.</summary>
    Carrack,
    /// <summary>Flagship-class warship.</summary>
    ManOfWar
}

/// <summary>
/// Ship class categories.
/// </summary>
public enum ShipClass
{
    Light,
    Medium,
    Heavy,
    Flagship,
    Standard
}

/// <summary>
/// Fleet operational state.
/// </summary>
public enum FleetState
{
    Docked,
    Sailing,
    Anchored,
    InCombat,
    Blockading,
    Fleeing,
    Disabled
}

/// <summary>
/// Fleet formation types.
/// </summary>
public enum FleetFormation
{
    Line,
    Column,
    Wedge,
    Circle,
    Scatter
}

/// <summary>
/// Crew quality levels.
/// </summary>
public enum CrewQuality
{
    Recruit,
    Regular,
    Veteran,
    Elite
}

/// <summary>
/// Naval position on the sea map.
/// </summary>
public sealed class NavalPosition
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Heading { get; set; }

    public NavalPosition() { }

    public NavalPosition(float x, float y, float heading = 0)
    {
        X = x;
        Y = y;
        Heading = heading;
    }
}

/// <summary>
/// Cargo item in a ship's hold.
/// </summary>
public sealed class CargoItem
{
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Weight { get; set; }
    public int Value { get; set; }
    public CargoType Type { get; set; }
}

/// <summary>
/// Cargo type categories.
/// </summary>
public enum CargoType
{
    TradeGoods,
    Provisions,
    Equipment,
    Treasure,
    Prisoners,
    Contraband
}

/// <summary>
/// Ship upgrades available.
/// </summary>
public enum ShipUpgrade
{
    // Hull
    ReinforcedHull,
    IronPlating,
    WaterproofingTar,
    CopperSheathing,

    // Sails
    SpeedSails,
    StormSails,
    NavigatorSails,
    LatinSails,

    // Combat
    BallistaMount,
    CatapultMount,
    RammingProw,
    GreekFireSiphon,
    BoardingRamps,
    ArrowSlits,

    // Cargo
    ExtendedHold,
    SecureVault,
    LivestockPens,
    WaterBarrels,

    // Crew
    CrewQuarters,
    TrainingDummy,
    Infirmary,
    CaptainsCabin,

    // Navigation
    Astrolabe,
    ImprovedRudder,
    HeavyAnchor,
    Compass,
    Charts
}

/// <summary>
/// Ship weapon installation.
/// </summary>
public sealed class ShipWeapon
{
    public string WeaponId { get; set; } = string.Empty;
    public ShipWeaponType Type { get; set; }
    public int Damage { get; set; }
    public int Range { get; set; }
    public int Ammunition { get; set; }
    public int MaxAmmunition { get; set; }
    public WeaponPosition Position { get; set; }
}

/// <summary>
/// Ship weapon types.
/// </summary>
public enum ShipWeaponType
{
    Ballista,
    Catapult,
    Scorpion,
    GreekFire,
    Cannon
}

/// <summary>
/// Weapon position on ship.
/// </summary>
public enum WeaponPosition
{
    Bow,
    Stern,
    PortSide,
    StarboardSide,
    Deck
}

/// <summary>
/// Sea region data.
/// </summary>
public sealed class SeaRegion
{
    public string RegionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SeaRegionType Type { get; set; }
    public IList<string> ConnectedRegions { get; set; } = new List<string>();
    public IList<Port> Ports { get; set; } = new List<Port>();
    public float DangerLevel { get; set; }
    public WeatherCondition CurrentWeather { get; set; }
}

/// <summary>
/// Sea region types.
/// </summary>
public enum SeaRegionType
{
    CoastalWaters,
    OpenSea,
    Strait,
    Bay,
    River,
    Lake
}

/// <summary>
/// Weather conditions at sea.
/// </summary>
public enum WeatherCondition
{
    Calm,
    Fair,
    Windy,
    Stormy,
    Hurricane,
    Foggy
}

/// <summary>
/// Port data.
/// </summary>
public sealed class Port
{
    public MBGUID Id { get; set; }
    public string PortId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SettlementData? Settlement { get; set; }
    public MBGUID? SettlementId { get; set; }
    public PortType Type { get; set; }
    public int DockCapacity { get; set; }
    public int ShipyardLevel { get; set; }
    public bool HasMarket { get; set; }
    public bool HasTavern { get; set; }
    public bool HasShipyard { get; set; }
}

/// <summary>
/// Port types.
/// </summary>
public enum PortType
{
    FishingVillage,
    TradingPost,
    Harbor,
    MajorPort,
    NavalBase
}

/// <summary>
/// Naval campaign data container.
/// </summary>
public sealed class NavalCampaignData
{
    public string WarSailsVersion { get; set; } = string.Empty;
    public IList<SeaRegion> SeaRegions { get; set; } = new List<SeaRegion>();
    public IList<Port> Ports { get; set; } = new List<Port>();
    public IList<NavalRoute> TradeRoutes { get; set; } = new List<NavalRoute>();
    public NavalSettings Settings { get; set; } = new();
}

/// <summary>
/// Naval trade route.
/// </summary>
public sealed class NavalRoute
{
    public string RouteId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MBGUID StartPortId { get; set; }
    public MBGUID EndPortId { get; set; }
    public IList<string> WaypointRegions { get; set; } = new List<string>();
    public float Distance { get; set; }
    public float DangerLevel { get; set; }
}

/// <summary>
/// Naval gameplay settings.
/// </summary>
public sealed class NavalSettings
{
    public float CrewWageMultiplier { get; set; } = 1.0f;
    public float ShipMaintenanceMultiplier { get; set; } = 1.0f;
    public float PirateActivityLevel { get; set; } = 0.5f;
    public float StormFrequency { get; set; } = 0.3f;
    public bool EnableNavalBattles { get; set; } = true;
    public bool EnableTradeRoutes { get; set; } = true;
}
