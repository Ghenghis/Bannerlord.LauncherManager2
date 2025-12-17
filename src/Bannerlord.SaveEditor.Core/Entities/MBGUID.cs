// <copyright file="MBGUID.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Entities;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>
/// Mount &amp; Blade Global Unique Identifier.
/// 64-bit identifier used throughout the game for entity references.
/// </summary>
public readonly struct MBGUID : IEquatable<MBGUID>, IComparable<MBGUID>
{
    /// <summary>
    /// Empty/null GUID.
    /// </summary>
    public static readonly MBGUID Empty = new(0);

    private readonly ulong _internalValue;

    /// <summary>
    /// Gets the internal 64-bit value.
    /// </summary>
    public ulong InternalValue => _internalValue;

    /// <summary>
    /// Gets the type ID portion (high 32 bits).
    /// </summary>
    public uint TypeId => (uint)(_internalValue >> 32);

    /// <summary>
    /// Gets the unique ID portion (low 32 bits).
    /// </summary>
    public uint UniqueId => (uint)(_internalValue & 0xFFFFFFFF);

    /// <summary>
    /// Gets the entity type.
    /// </summary>
    public MBGUIDType Type => (MBGUIDType)TypeId;

    /// <summary>
    /// Gets whether this GUID is empty.
    /// </summary>
    public bool IsEmpty => _internalValue == 0;

    /// <summary>
    /// Creates a new MBGUID from raw value.
    /// </summary>
    public MBGUID(ulong internalValue)
    {
        _internalValue = internalValue;
    }

    /// <summary>
    /// Creates a new MBGUID from type and unique ID.
    /// </summary>
    public MBGUID(uint typeId, uint uniqueId)
    {
        _internalValue = ((ulong)typeId << 32) | uniqueId;
    }

    /// <summary>
    /// Creates a new MBGUID from type and unique ID.
    /// </summary>
    public MBGUID(MBGUIDType type, uint uniqueId)
        : this((uint)type, uniqueId)
    {
    }

    /// <summary>
    /// Parses a MBGUID from string representation.
    /// </summary>
    /// <param name="value">String in format "TypeId-UniqueId" or hex format.</param>
    /// <returns>Parsed MBGUID.</returns>
    /// <exception cref="FormatException">If string is not valid.</exception>
    public static MBGUID Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Empty;

        // Try "TypeId-UniqueId" format
        if (value.Contains('-'))
        {
            var parts = value.Split('-');
            if (parts.Length == 2 &&
                uint.TryParse(parts[0], out var typeId) &&
                uint.TryParse(parts[1], out var uniqueId))
            {
                return new MBGUID(typeId, uniqueId);
            }
        }

        // Try hex format "0x..."
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (ulong.TryParse(value.AsSpan(2), NumberStyles.HexNumber, null, out var hexValue))
                return new MBGUID(hexValue);
        }

        // Try plain decimal
        if (ulong.TryParse(value, out var decValue))
            return new MBGUID(decValue);

        throw new FormatException($"Invalid MBGUID format: {value}");
    }

    /// <summary>
    /// Tries to parse a MBGUID from string representation.
    /// </summary>
    public static bool TryParse(string? value, out MBGUID result)
    {
        result = Empty;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            result = Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a new MBGUID of the specified type.
    /// </summary>
    /// <param name="type">Entity type.</param>
    /// <returns>New MBGUID with random unique ID.</returns>
    public static MBGUID Generate(MBGUIDType type)
    {
        var uniqueId = (uint)Random.Shared.Next(1, int.MaxValue);
        return new MBGUID(type, uniqueId);
    }

    /// <inheritdoc />
    public override string ToString() => $"{TypeId}-{UniqueId}";

    /// <summary>
    /// Returns hex string representation.
    /// </summary>
    public string ToHexString() => $"0x{_internalValue:X16}";

    /// <inheritdoc />
    public bool Equals(MBGUID other) => _internalValue == other._internalValue;

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is MBGUID other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _internalValue.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(MBGUID other) => _internalValue.CompareTo(other._internalValue);

    public static bool operator ==(MBGUID left, MBGUID right) => left.Equals(right);
    public static bool operator !=(MBGUID left, MBGUID right) => !left.Equals(right);
    public static bool operator <(MBGUID left, MBGUID right) => left.CompareTo(right) < 0;
    public static bool operator >(MBGUID left, MBGUID right) => left.CompareTo(right) > 0;
    public static bool operator <=(MBGUID left, MBGUID right) => left.CompareTo(right) <= 0;
    public static bool operator >=(MBGUID left, MBGUID right) => left.CompareTo(right) >= 0;

    public static implicit operator ulong(MBGUID guid) => guid._internalValue;
    public static explicit operator MBGUID(ulong value) => new(value);
}

/// <summary>
/// MBGUID entity types.
/// </summary>
public enum MBGUIDType : uint
{
    None = 0,

    // Core entities
    Hero = 1,
    Party = 2,
    Settlement = 3,
    Clan = 4,
    Kingdom = 5,
    Faction = 6,

    // Army/Battle
    Army = 10,
    MapEvent = 11,
    Siege = 12,

    // Economy
    Workshop = 20,
    Caravan = 21,
    Village = 22,
    Town = 23,
    Castle = 24,

    // Quests
    Quest = 30,
    Issue = 31,

    // Items
    ItemObject = 50,
    ItemRoster = 51,
    Equipment = 52,

    // War Sails (100+)
    Fleet = 100,
    Ship = 101,
    Port = 102,
    SeaRoute = 103,
    NavalBattle = 104,

    // Misc
    CharacterObject = 200,
    CultureObject = 201,
    PolicyObject = 202,
    BuildingType = 203,

    // Custom/Mod (1000+)
    Custom = 1000
}
