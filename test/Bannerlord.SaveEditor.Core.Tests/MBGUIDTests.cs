// <copyright file="MBGUIDTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Entities;
using FluentAssertions;
using Xunit;

public class MBGUIDTests
{
    [Fact]
    public void Constructor_WithValue_SetsInternalValue()
    {
        // TypeId=1 in high 32 bits, UniqueId=0x1234 in low 32 bits
        var guid = new MBGUID(0x0000_0001_0000_1234UL);

        guid.InternalValue.Should().Be(0x0000_0001_0000_1234UL);
        guid.TypeId.Should().Be(1U);
        guid.UniqueId.Should().Be(0x1234U);
    }

    [Fact]
    public void Constructor_WithTypeAndId_CreatesCorrectValue()
    {
        var guid = new MBGUID(MBGUIDType.Hero, 1234);

        guid.TypeId.Should().Be((int)MBGUIDType.Hero);
        guid.UniqueId.Should().Be(1234U);
    }

    [Fact]
    public void Parse_ValidFormat_ReturnsCorrectGUID()
    {
        var guid = MBGUID.Parse("1-1234");

        guid.TypeId.Should().Be(1U);
        guid.UniqueId.Should().Be(1234U);
    }

    [Fact]
    public void Parse_HexFormat_ReturnsCorrectGUID()
    {
        // TypeId=1 in high 32 bits, UniqueId=0x1234 in low 32 bits
        var guid = MBGUID.Parse("0x0000000100001234");

        guid.TypeId.Should().Be(1U);
        guid.UniqueId.Should().Be(0x1234U);
    }

    [Fact]
    public void TryParse_InvalidFormat_ReturnsFalse()
    {
        var success = MBGUID.TryParse("invalid", out var guid);

        success.Should().BeFalse();
        guid.Should().Be(default(MBGUID));
    }

    [Fact]
    public void Generate_CreatesUniqueValues()
    {
        var guid1 = MBGUID.Generate(MBGUIDType.Hero);
        var guid2 = MBGUID.Generate(MBGUIDType.Hero);

        guid1.Should().NotBe(guid2);
        guid1.TypeId.Should().Be((int)MBGUIDType.Hero);
        guid2.TypeId.Should().Be((int)MBGUIDType.Hero);
    }

    [Fact]
    public void IsEmpty_NewGUID_ReturnsTrue()
    {
        var guid = new MBGUID();

        guid.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_NonZeroGUID_ReturnsFalse()
    {
        var guid = new MBGUID(1, 1);

        guid.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        var guid = new MBGUID(1, 1234);

        guid.ToString().Should().Be("1-1234");
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        var guid = MBGUID.Parse("");
        guid.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Parse_WhitespaceString_ReturnsEmpty()
    {
        var guid = MBGUID.Parse("   ");
        guid.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Parse_PlainDecimal_ReturnsCorrectGUID()
    {
        var guid = MBGUID.Parse("4294967297"); // 0x100000001
        guid.TypeId.Should().Be(1U);
        guid.UniqueId.Should().Be(1U);
    }

    [Fact]
    public void Parse_InvalidFormat_ThrowsFormatException()
    {
        FluentActions.Invoking(() => MBGUID.Parse("not-a-guid-format-xyz"))
            .Should().Throw<FormatException>();
    }

    [Fact]
    public void TryParse_NullString_ReturnsFalse()
    {
        var success = MBGUID.TryParse(null, out var guid);
        success.Should().BeFalse();
        guid.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        var success = MBGUID.TryParse("", out var guid);
        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_ValidFormat_ReturnsTrue()
    {
        var success = MBGUID.TryParse("1-1234", out var guid);
        success.Should().BeTrue();
        guid.TypeId.Should().Be(1U);
        guid.UniqueId.Should().Be(1234U);
    }

    [Fact]
    public void Type_ReturnsCorrectMBGUIDType()
    {
        var guid = MBGUID.Generate(MBGUIDType.Party);
        guid.Type.Should().Be(MBGUIDType.Party);
    }

    [Fact]
    public void Empty_IsStaticEmptyGUID()
    {
        MBGUID.Empty.IsEmpty.Should().BeTrue();
        MBGUID.Empty.InternalValue.Should().Be(0UL);
    }

    [Fact]
    public void CompareTo_SameValue_ReturnsZero()
    {
        var guid1 = new MBGUID(1, 1234);
        var guid2 = new MBGUID(1, 1234);
        guid1.CompareTo(guid2).Should().Be(0);
    }

    [Fact]
    public void CompareTo_DifferentValue_ReturnsNonZero()
    {
        var guid1 = new MBGUID(1, 1234);
        var guid2 = new MBGUID(1, 5678);
        guid1.CompareTo(guid2).Should().NotBe(0);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        var guid1 = new MBGUID(1, 1234);
        var guid2 = new MBGUID(1, 1234);
        guid1.GetHashCode().Should().Be(guid2.GetHashCode());
    }

    [Fact]
    public void Equality_SameValues_ReturnsTrue()
    {
        var guid1 = new MBGUID(1, 1234);
        var guid2 = new MBGUID(1, 1234);

        (guid1 == guid2).Should().BeTrue();
        guid1.Equals(guid2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValues_ReturnsFalse()
    {
        var guid1 = new MBGUID(1, 1234);
        var guid2 = new MBGUID(1, 5678);

        (guid1 != guid2).Should().BeTrue();
        guid1.Equals(guid2).Should().BeFalse();
    }

    #region Comprehensive Type Tests

    [Theory]
    [InlineData(MBGUIDType.None)]
    [InlineData(MBGUIDType.Hero)]
    [InlineData(MBGUIDType.Party)]
    [InlineData(MBGUIDType.Settlement)]
    [InlineData(MBGUIDType.Clan)]
    [InlineData(MBGUIDType.Kingdom)]
    [InlineData(MBGUIDType.Fleet)]
    [InlineData(MBGUIDType.Ship)]
    public void Generate_AllTypes_CreatesValidGUID(MBGUIDType type)
    {
        // Act
        var guid = MBGUID.Generate(type);

        // Assert
        guid.Type.Should().Be(type);
        guid.IsEmpty.Should().BeFalse();
    }

    [Theory]
    [InlineData(MBGUIDType.None)]
    [InlineData(MBGUIDType.Hero)]
    [InlineData(MBGUIDType.Party)]
    [InlineData(MBGUIDType.Settlement)]
    [InlineData(MBGUIDType.Clan)]
    [InlineData(MBGUIDType.Kingdom)]
    [InlineData(MBGUIDType.Fleet)]
    [InlineData(MBGUIDType.Ship)]
    public void Constructor_AllTypes_SetsTypeCorrectly(MBGUIDType type)
    {
        // Act
        var guid = new MBGUID((byte)type, 12345);

        // Assert
        guid.TypeId.Should().Be((byte)type);
    }

    #endregion

    #region Comprehensive Parse Edge Cases

    [Theory]
    [InlineData("1-1")]
    [InlineData("5-12345")]
    [InlineData("255-4294967295")]
    public void Parse_ValidFormats_ParsesCorrectly(string input)
    {
        // Act
        var guid = MBGUID.Parse(input);

        // Assert - non-zero inputs should parse to non-empty GUIDs
        guid.IsEmpty.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("not-a-guid")]
    [InlineData("abc")]
    public void TryParse_InvalidFormats_ReturnsFalse(string input)
    {
        // Act
        var success = MBGUID.TryParse(input, out var guid);

        // Assert
        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_ValidDashFormat_ReturnsTrue()
    {
        // Act
        var success = MBGUID.TryParse("1-12345", out var guid);

        // Assert
        success.Should().BeTrue();
        guid.TypeId.Should().Be(1);
        guid.UniqueId.Should().Be(12345);
    }

    [Fact]
    public void TryParse_EmptyInput_ReturnsFalse()
    {
        // Act
        var success = MBGUID.TryParse(string.Empty, out var guid);

        // Assert
        success.Should().BeFalse();
    }

    [Fact]
    public void TryParse_NullInput_ReturnsFalse()
    {
        // Act
        var success = MBGUID.TryParse(null!, out var guid);

        // Assert
        success.Should().BeFalse();
    }

    #endregion

    #region Comprehensive Equality Edge Cases

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Hero);

        // Act & Assert
        guid.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSelf_ReturnsTrue()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Hero);

        // Act & Assert
        guid.Equals(guid).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Hero);

        // Act & Assert
        guid.Equals("not a guid").Should().BeFalse();
    }

    [Fact]
    public void Equals_EmptyGuids_ReturnsTrue()
    {
        // Act & Assert
        MBGUID.Empty.Equals(MBGUID.Empty).Should().BeTrue();
    }

    [Fact]
    public void Equality_Operator_EmptyGuids_ReturnsTrue()
    {
        // Act & Assert
        (MBGUID.Empty == MBGUID.Empty).Should().BeTrue();
    }

    [Fact]
    public void Inequality_Operator_EmptyAndNonEmpty_ReturnsTrue()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Hero);

        // Act & Assert
        (MBGUID.Empty != guid).Should().BeTrue();
    }

    #endregion

    #region Comprehensive CompareTo Edge Cases

    [Fact]
    public void CompareTo_EmptyWithEmpty_ReturnsZero()
    {
        // Act & Assert
        MBGUID.Empty.CompareTo(MBGUID.Empty).Should().Be(0);
    }

    [Fact]
    public void CompareTo_EmptyWithNonEmpty_ReturnsNegative()
    {
        // Arrange
        var guid = new MBGUID(1, 1);

        // Act & Assert
        MBGUID.Empty.CompareTo(guid).Should().BeLessThan(0);
    }

    [Fact]
    public void CompareTo_NonEmptyWithEmpty_ReturnsPositive()
    {
        // Arrange
        var guid = new MBGUID(1, 1);

        // Act & Assert
        guid.CompareTo(MBGUID.Empty).Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_SameType_DifferentUniqueId_ComparesCorrectly()
    {
        // Arrange
        var guid1 = new MBGUID(1, 100);
        var guid2 = new MBGUID(1, 200);

        // Act & Assert
        guid1.CompareTo(guid2).Should().BeLessThan(0);
        guid2.CompareTo(guid1).Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_DifferentTypes_ComparesCorrectly()
    {
        // Arrange
        var guid1 = new MBGUID(1, 100);
        var guid2 = new MBGUID(2, 100);

        // Act & Assert
        guid1.CompareTo(guid2).Should().NotBe(0);
    }

    #endregion

    #region Comprehensive GetHashCode Edge Cases

    [Fact]
    public void GetHashCode_EmptyGuid_ReturnsConsistentValue()
    {
        // Act
        var hash1 = MBGUID.Empty.GetHashCode();
        var hash2 = MBGUID.Empty.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_DifferentGuids_ReturnsDifferentHashes()
    {
        // Arrange - use Generate to ensure non-zero values
        var guid1 = MBGUID.Generate(MBGUIDType.Hero);
        var guid2 = MBGUID.Generate(MBGUIDType.Party);

        // Act & Assert - while collisions are possible, different types should differ
        guid1.GetHashCode().Should().NotBe(guid2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_MultipleCallsSameGuid_ReturnsSameHash()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Hero);

        // Act
        var hash1 = guid.GetHashCode();
        var hash2 = guid.GetHashCode();
        var hash3 = guid.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
        hash2.Should().Be(hash3);
    }

    #endregion

    #region Comprehensive ToString Edge Cases

    [Fact]
    public void ToString_EmptyGuid_ReturnsExpectedFormat()
    {
        // Act
        var result = MBGUID.Empty.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ToString_GeneratedGuid_ContainsTypeAndId()
    {
        // Arrange
        var guid = new MBGUID(5, 12345);

        // Act
        var result = guid.ToString();

        // Assert
        result.Should().Contain("5");
        result.Should().Contain("12345");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(255, 999999)]
    public void ToString_VariousValues_FormatsCorrectly(byte typeId, uint uniqueId)
    {
        // Arrange
        var guid = new MBGUID(typeId, uniqueId);

        // Act
        var result = guid.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Comprehensive InternalValue Tests

    [Fact]
    public void InternalValue_EmptyGuid_IsZero()
    {
        // Assert
        MBGUID.Empty.InternalValue.Should().Be(0UL);
    }

    [Fact]
    public void InternalValue_NonEmptyGuid_IsNotZero()
    {
        // Arrange
        var guid = new MBGUID(1, 1);

        // Assert
        guid.InternalValue.Should().NotBe(0UL);
    }

    [Fact]
    public void InternalValue_SameGuids_HaveSameValue()
    {
        // Arrange
        var guid1 = new MBGUID(5, 12345);
        var guid2 = new MBGUID(5, 12345);

        // Assert
        guid1.InternalValue.Should().Be(guid2.InternalValue);
    }

    [Fact]
    public void InternalValue_DifferentGuids_HaveDifferentValues()
    {
        // Arrange
        var guid1 = new MBGUID(1, 100);
        var guid2 = new MBGUID(2, 200);

        // Assert
        guid1.InternalValue.Should().NotBe(guid2.InternalValue);
    }

    #endregion

    #region Comprehensive IsEmpty Tests

    [Fact]
    public void IsEmpty_DefaultConstructor_ReturnsTrue()
    {
        // Arrange
        var guid = default(MBGUID);

        // Assert
        guid.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_StaticEmpty_ReturnsTrue()
    {
        // Assert
        MBGUID.Empty.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_NonZeroTypeId_ReturnsFalse()
    {
        // Arrange - use explicit byte cast
        var guid = new MBGUID((byte)1, (uint)0);

        // Assert
        guid.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_GeneratedGuid_IsNotEmpty()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Party);

        // Assert
        guid.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_GeneratedGuid_ReturnsFalse()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Hero);

        // Assert
        guid.IsEmpty.Should().BeFalse();
    }

    #endregion

    #region Comprehensive Generate Tests

    [Fact]
    public void Generate_MultipleCallsSameType_CreatesUniqueGuids()
    {
        // Act
        var guids = Enumerable.Range(0, 100)
            .Select(_ => MBGUID.Generate(MBGUIDType.Hero))
            .ToList();

        // Assert - all should be unique
        guids.Distinct().Count().Should().Be(100);
    }

    [Fact]
    public void Generate_DifferentTypes_CreatesDifferentTypeIds()
    {
        // Act
        var heroGuid = MBGUID.Generate(MBGUIDType.Hero);
        var partyGuid = MBGUID.Generate(MBGUIDType.Party);
        var fleetGuid = MBGUID.Generate(MBGUIDType.Fleet);

        // Assert
        heroGuid.TypeId.Should().NotBe(partyGuid.TypeId);
        partyGuid.TypeId.Should().NotBe(fleetGuid.TypeId);
    }

    #endregion

    #region Comprehensive Type Conversion Tests

    [Theory]
    [InlineData(MBGUIDType.Hero)]
    [InlineData(MBGUIDType.Party)]
    [InlineData(MBGUIDType.Settlement)]
    [InlineData(MBGUIDType.Fleet)]
    [InlineData(MBGUIDType.Clan)]
    [InlineData(MBGUIDType.Kingdom)]
    public void Generate_AllTypes_CreateValidGuids(MBGUIDType type)
    {
        // Act
        var guid = MBGUID.Generate(type);

        // Assert
        guid.IsEmpty.Should().BeFalse();
        guid.Type.Should().Be(type);
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(100u)]
    [InlineData(1000u)]
    [InlineData(uint.MaxValue)]
    public void Constructor_VariousUniqueIds_SetsCorrectly(uint uniqueId)
    {
        // Act
        var guid = new MBGUID(1, uniqueId);

        // Assert
        guid.UniqueId.Should().Be(uniqueId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Constructor_VariousTypeIds_SetsCorrectly(byte typeId)
    {
        // Act
        var guid = new MBGUID(typeId, 100);

        // Assert
        guid.TypeId.Should().Be(typeId);
    }

    #endregion

    #region Comprehensive Operator Tests

    [Fact]
    public void EqualityOperator_SameGuids_ReturnsTrue()
    {
        // Arrange
        var guid1 = new MBGUID(1, 100);
        var guid2 = new MBGUID(1, 100);

        // Act & Assert
        (guid1 == guid2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_DifferentGuids_ReturnsFalse()
    {
        // Arrange
        var guid1 = new MBGUID(1, 100);
        var guid2 = new MBGUID(1, 200);

        // Act & Assert
        (guid1 == guid2).Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_DifferentGuids_ReturnsTrue()
    {
        // Arrange
        var guid1 = new MBGUID(1, 100);
        var guid2 = new MBGUID(2, 100);

        // Act & Assert
        (guid1 != guid2).Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_SameGuids_ReturnsFalse()
    {
        // Arrange
        var guid1 = new MBGUID(1, 100);
        var guid2 = new MBGUID(1, 100);

        // Act & Assert
        (guid1 != guid2).Should().BeFalse();
    }

    #endregion

    #region Comprehensive Equals Tests

    [Fact]
    public void Equals_SameInstance_ReturnsTrue()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Hero);

        // Act & Assert
        guid.Equals(guid).Should().BeTrue();
    }

    [Fact]
    public void Equals_NullObject_ReturnsFalse()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Hero);

        // Act & Assert
        guid.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Hero);

        // Act & Assert
        guid.Equals("not a guid").Should().BeFalse();
    }

    [Fact]
    public void Equals_EqualGuids_ReturnsTrue()
    {
        // Arrange
        var guid1 = new MBGUID(3, 300);
        var guid2 = new MBGUID(3, 300);

        // Act & Assert
        guid1.Equals((object)guid2).Should().BeTrue();
    }

    #endregion

    #region Comprehensive Collection Tests

    [Fact]
    public void HashSet_SameGuids_CountsAsOne()
    {
        // Arrange
        var guid1 = new MBGUID(1, 100);
        var guid2 = new MBGUID(1, 100);
        var set = new HashSet<MBGUID> { guid1, guid2 };

        // Assert
        set.Count.Should().Be(1);
    }

    [Fact]
    public void HashSet_DifferentGuids_CountsAsSeparate()
    {
        // Arrange
        var guid1 = MBGUID.Generate(MBGUIDType.Hero);
        var guid2 = MBGUID.Generate(MBGUIDType.Party);
        var set = new HashSet<MBGUID> { guid1, guid2 };

        // Assert
        set.Count.Should().Be(2);
    }

    [Fact]
    public void Dictionary_CanUseAsKey()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Hero);
        var dict = new Dictionary<MBGUID, string> { { guid, "test" } };

        // Act & Assert
        dict[guid].Should().Be("test");
    }

    [Fact]
    public void List_CanContainGuids()
    {
        // Arrange
        var list = new List<MBGUID>
        {
            MBGUID.Generate(MBGUIDType.Hero),
            MBGUID.Generate(MBGUIDType.Party),
            MBGUID.Generate(MBGUIDType.Settlement)
        };

        // Assert
        list.Count.Should().Be(3);
    }

    #endregion

    #region Comprehensive Type Tests

    [Theory]
    [InlineData(MBGUIDType.Hero)]
    [InlineData(MBGUIDType.Party)]
    [InlineData(MBGUIDType.Settlement)]
    [InlineData(MBGUIDType.Clan)]
    [InlineData(MBGUIDType.Kingdom)]
    public void Generate_AllTypes_CreatesValidGuid(MBGUIDType type)
    {
        // Act
        var guid = MBGUID.Generate(type);

        // Assert
        guid.IsEmpty.Should().BeFalse();
        guid.Type.Should().Be(type);
    }

    [Fact]
    public void Type_ReturnsCorrectType()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Hero);

        // Act & Assert
        guid.Type.Should().Be(MBGUIDType.Hero);
    }

    #endregion

    #region Comprehensive InternalValue Tests

    [Fact]
    public void InternalValue_SameGuids_AreEqual()
    {
        // Arrange
        var guid1 = new MBGUID(5, 500);
        var guid2 = new MBGUID(5, 500);

        // Assert
        guid1.InternalValue.Should().Be(guid2.InternalValue);
    }

    [Fact]
    public void InternalValue_DifferentGuids_AreDifferent()
    {
        // Arrange
        var guid1 = new MBGUID(5, 500);
        var guid2 = new MBGUID(5, 501);

        // Assert
        guid1.InternalValue.Should().NotBe(guid2.InternalValue);
    }

    #endregion

    #region Comprehensive String Conversion Tests

    [Fact]
    public void ToString_NonEmpty_ReturnsNonEmptyString()
    {
        // Arrange
        var guid = MBGUID.Generate(MBGUIDType.Hero);

        // Act
        var result = guid.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ToString_Empty_ReturnsValidString()
    {
        // Arrange
        var guid = MBGUID.Empty;

        // Act
        var result = guid.ToString();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ToString_ConsistentForSameGuid()
    {
        // Arrange
        var guid = new MBGUID(10, 1000);

        // Act
        var result1 = guid.ToString();
        var result2 = guid.ToString();

        // Assert
        result1.Should().Be(result2);
    }

    #endregion

    #region Comprehensive Comparison Tests

    [Fact]
    public void CompareTo_SameGuid_ReturnsZero()
    {
        // Arrange
        var guid1 = new MBGUID(1, 100);
        var guid2 = new MBGUID(1, 100);

        // Act & Assert
        guid1.CompareTo(guid2).Should().Be(0);
    }

    [Fact]
    public void CompareTo_LesserGuid_ReturnsNegative()
    {
        // Arrange
        var guid1 = new MBGUID(1, 100);
        var guid2 = new MBGUID(1, 200);

        // Act & Assert
        guid1.CompareTo(guid2).Should().BeLessThan(0);
    }

    [Fact]
    public void CompareTo_GreaterGuid_ReturnsPositive()
    {
        // Arrange
        var guid1 = new MBGUID(1, 200);
        var guid2 = new MBGUID(1, 100);

        // Act & Assert
        guid1.CompareTo(guid2).Should().BeGreaterThan(0);
    }

    #endregion
}
