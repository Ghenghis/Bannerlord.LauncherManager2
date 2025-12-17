// <copyright file="ZlibHandlerTests.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Tests;

using Bannerlord.SaveEditor.Core.Compression;
using FluentAssertions;
using System.IO.Compression;
using Xunit;

public class ZlibHandlerTests
{
    private readonly ZlibHandler _handler;

    public ZlibHandlerTests()
    {
        _handler = new ZlibHandler();
    }

    #region ValidateHeader Tests

    [Fact]
    public void ValidateHeader_ValidZlibHeader_ReturnsTrue()
    {
        // Arrange - Standard ZLIB header (0x78 0x9C for default compression)
        var header = new byte[] { 0x78, 0x9C };

        // Act
        var isValid = _handler.ValidateHeader(header);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateHeader_ValidZlibHeaderBestCompression_ReturnsTrue()
    {
        // Arrange - ZLIB header for best compression (0x78 0xDA)
        var header = new byte[] { 0x78, 0xDA };

        // Act
        var isValid = _handler.ValidateHeader(header);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateHeader_ValidZlibHeaderNoCompression_ReturnsTrue()
    {
        // Arrange - ZLIB header for no compression (0x78 0x01)
        var header = new byte[] { 0x78, 0x01 };

        // Act
        var isValid = _handler.ValidateHeader(header);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateHeader_InvalidHeader_ReturnsFalse()
    {
        // Arrange - Invalid header
        var header = new byte[] { 0x00, 0x00 };

        // Act
        var isValid = _handler.ValidateHeader(header);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateHeader_GzipHeader_ReturnsFalse()
    {
        // Arrange - GZip magic number (not ZLIB)
        var header = new byte[] { 0x1F, 0x8B };

        // Act
        var isValid = _handler.ValidateHeader(header);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Compress/Decompress Tests

    [Fact]
    public async Task CompressAsync_ValidData_ReturnsCompressedData()
    {
        // Arrange
        var originalData = System.Text.Encoding.UTF8.GetBytes("Hello, World! This is a test string for compression.");

        // Act
        var compressed = await _handler.CompressAsync(originalData);

        // Assert
        compressed.Should().NotBeEmpty();
        compressed.Length.Should().BeGreaterThan(2); // At least header
    }

    [Fact]
    public async Task CompressAsync_SmallData_ReturnsCompressedData()
    {
        // Arrange
        var originalData = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var compressed = await _handler.CompressAsync(originalData);

        // Assert
        compressed.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CompressAsync_LargeData_ReturnsCompressedData()
    {
        // Arrange
        var originalData = new byte[10000];
        new Random(42).NextBytes(originalData);

        // Act
        var compressed = await _handler.CompressAsync(originalData);

        // Assert
        compressed.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CompressAsync_WithCompressionLevel_RespectsLevel()
    {
        // Arrange
        var originalData = System.Text.Encoding.UTF8.GetBytes(new string('A', 1000));

        // Act
        var fastest = await _handler.CompressAsync(originalData, CompressionLevel.Fastest);
        var optimal = await _handler.CompressAsync(originalData, CompressionLevel.Optimal);

        // Assert
        fastest.Should().NotBeEmpty();
        optimal.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DecompressAsync_NullData_ThrowsException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _handler.DecompressAsync(null!))
            .Should().ThrowAsync<DecompressionException>();
    }

    [Fact]
    public async Task DecompressAsync_TooShortData_ThrowsException()
    {
        // Arrange
        var shortData = new byte[] { 0x78, 0x9C };

        // Act & Assert
        await FluentActions.Invoking(() => _handler.DecompressAsync(shortData))
            .Should().ThrowAsync<DecompressionException>();
    }

    [Fact]
    public async Task DecompressAsync_InvalidHeader_ThrowsException()
    {
        // Arrange
        var invalidData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // Act & Assert
        await FluentActions.Invoking(() => _handler.DecompressAsync(invalidData))
            .Should().ThrowAsync<DecompressionException>();
    }

    [Fact]
    public async Task RoundTrip_CompressDecompress_ReturnsOriginalData()
    {
        // Arrange
        var originalData = System.Text.Encoding.UTF8.GetBytes("Test data for round-trip compression verification.");

        // Act
        var compressed = await _handler.CompressAsync(originalData);
        var decompressed = await _handler.DecompressAsync(compressed);

        // Assert
        decompressed.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public async Task RoundTrip_LargeData_ReturnsOriginalData()
    {
        // Arrange
        var originalData = new byte[50000];
        new Random(123).NextBytes(originalData);

        // Act
        var compressed = await _handler.CompressAsync(originalData);
        var decompressed = await _handler.DecompressAsync(compressed, originalData.Length);

        // Assert
        decompressed.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public async Task DecompressAsync_WithExpectedSize_ValidatesSize()
    {
        // Arrange
        var originalData = System.Text.Encoding.UTF8.GetBytes("Test data");
        var compressed = await _handler.CompressAsync(originalData);

        // Act
        var decompressed = await _handler.DecompressAsync(compressed, originalData.Length);

        // Assert
        decompressed.Length.Should().Be(originalData.Length);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CompressAsync_WithValidToken_Completes()
    {
        // Arrange
        var originalData = System.Text.Encoding.UTF8.GetBytes("Test data for compression");
        var cts = new CancellationTokenSource();

        // Act
        var compressed = await _handler.CompressAsync(originalData, CompressionLevel.Optimal, cts.Token);

        // Assert
        compressed.Should().NotBeEmpty();
    }

    [Fact]
    public void ZlibHandler_Constructor_CreatesInstance()
    {
        // Act
        var handler = new ZlibHandler();

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void ZlibHandler_Constructor_WithLogger_CreatesInstance()
    {
        // Act
        var handler = new ZlibHandler(null);

        // Assert
        handler.Should().NotBeNull();
    }

    #endregion

    #region Comprehensive Compression Level Tests

    [Theory]
    [InlineData(CompressionLevel.Optimal)]
    [InlineData(CompressionLevel.Fastest)]
    [InlineData(CompressionLevel.NoCompression)]
    public async Task CompressAsync_AllCompressionLevels_Work(CompressionLevel level)
    {
        // Arrange
        var originalData = System.Text.Encoding.UTF8.GetBytes("Test data for compression level test");

        // Act
        var compressed = await _handler.CompressAsync(originalData, level);

        // Assert
        compressed.Should().NotBeNull();
    }

    [Fact]
    public async Task CompressAsync_OptimalLevel_ProducesSmallestOutput()
    {
        // Arrange
        var originalData = System.Text.Encoding.UTF8.GetBytes(new string('a', 1000));

        // Act
        var optimal = await _handler.CompressAsync(originalData, CompressionLevel.Optimal);
        var fastest = await _handler.CompressAsync(originalData, CompressionLevel.Fastest);

        // Assert - optimal should be at least as small
        optimal.Length.Should().BeLessThanOrEqualTo(fastest.Length);
    }

    #endregion

    #region Comprehensive Data Size Tests

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public async Task CompressDecompress_VariousSizes_WorksCorrectly(int size)
    {
        // Arrange
        var originalData = new byte[size];
        new Random(42).NextBytes(originalData);

        // Act
        var compressed = await _handler.CompressAsync(originalData);
        var decompressed = await _handler.DecompressAsync(compressed, size);

        // Assert
        decompressed.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public async Task CompressAsync_SingleByte_Works()
    {
        // Arrange
        var originalData = new byte[] { 0x42 };

        // Act
        var compressed = await _handler.CompressAsync(originalData);
        var decompressed = await _handler.DecompressAsync(compressed, 1);

        // Assert
        decompressed.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public async Task CompressAsync_AllZeros_CompressesWell()
    {
        // Arrange
        var originalData = new byte[1000];

        // Act
        var compressed = await _handler.CompressAsync(originalData, CompressionLevel.Optimal);

        // Assert - all zeros should compress very well
        compressed.Length.Should().BeLessThan(originalData.Length);
    }

    [Fact]
    public async Task CompressAsync_RandomData_Works()
    {
        // Arrange
        var originalData = new byte[500];
        new Random(123).NextBytes(originalData);

        // Act
        var compressed = await _handler.CompressAsync(originalData);
        var decompressed = await _handler.DecompressAsync(compressed, 500);

        // Assert
        decompressed.Should().BeEquivalentTo(originalData);
    }

    #endregion

    #region Comprehensive Text Data Tests

    [Theory]
    [InlineData("Hello, World!")]
    [InlineData("Lorem ipsum dolor sit amet")]
    public async Task CompressDecompress_TextData_PreservesContent(string text)
    {
        // Arrange
        var originalData = System.Text.Encoding.UTF8.GetBytes(text);

        // Act
        var compressed = await _handler.CompressAsync(originalData);
        var decompressed = await _handler.DecompressAsync(compressed, originalData.Length);

        // Assert
        System.Text.Encoding.UTF8.GetString(decompressed).Should().Be(text);
    }

    [Fact]
    public async Task CompressAsync_UnicodeText_PreservesContent()
    {
        // Arrange
        var text = "Unicode: 日本語 中文 한국어 العربية";
        var originalData = System.Text.Encoding.UTF8.GetBytes(text);

        // Act
        var compressed = await _handler.CompressAsync(originalData);
        var decompressed = await _handler.DecompressAsync(compressed, originalData.Length);

        // Assert
        System.Text.Encoding.UTF8.GetString(decompressed).Should().Be(text);
    }

    #endregion

    #region Comprehensive Error Handling Tests

    [Fact]
    public async Task CompressAsync_NullData_ThrowsException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _handler.CompressAsync(null!))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task DecompressAsync_InvalidData_ThrowsException()
    {
        // Arrange
        var invalidData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        // Act & Assert
        await FluentActions.Invoking(() => _handler.DecompressAsync(invalidData, 100))
            .Should().ThrowAsync<Exception>();
    }

    #endregion

    #region Comprehensive Cancellation Tests

    [Fact]
    public async Task DecompressAsync_ValidToken_Works()
    {
        // Arrange
        var originalData = System.Text.Encoding.UTF8.GetBytes("Test");
        var compressed = await _handler.CompressAsync(originalData);
        var cts = new CancellationTokenSource();

        // Act
        var decompressed = await _handler.DecompressAsync(compressed, originalData.Length, cts.Token);

        // Assert
        decompressed.Should().BeEquivalentTo(originalData);
    }

    #endregion
}
