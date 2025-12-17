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
}
