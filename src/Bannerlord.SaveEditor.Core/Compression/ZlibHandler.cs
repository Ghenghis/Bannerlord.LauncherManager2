// <copyright file="ZlibHandler.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Compression;

using Microsoft.Extensions.Logging;
using System.IO.Compression;

/// <summary>
/// Interface for ZLIB compression/decompression operations.
/// </summary>
public interface IZlibHandler
{
    /// <summary>
    /// Decompresses ZLIB-compressed data.
    /// </summary>
    /// <param name="compressedData">The compressed data including ZLIB header.</param>
    /// <param name="expectedSize">Expected uncompressed size (optional, for validation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Decompressed data.</returns>
    Task<byte[]> DecompressAsync(byte[] compressedData, long? expectedSize = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compresses data using ZLIB format.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <param name="level">Compression level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compressed data with ZLIB header.</returns>
    Task<byte[]> CompressAsync(byte[] data, CompressionLevel level = CompressionLevel.Optimal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates ZLIB header bytes.
    /// </summary>
    /// <param name="header">First two bytes of compressed data.</param>
    /// <returns>True if valid ZLIB header.</returns>
    bool ValidateHeader(ReadOnlySpan<byte> header);
}

/// <summary>
/// ZLIB compression handler for Bannerlord save files.
/// </summary>
public sealed class ZlibHandler : IZlibHandler
{
    private readonly ILogger<ZlibHandler>? _logger;

    /// <summary>
    /// Standard ZLIB compression method (deflate).
    /// </summary>
    private const byte ZlibCompressionMethod = 0x08;

    /// <summary>
    /// Default compression info (32K window).
    /// </summary>
    private const byte ZlibCompressionInfo = 0x78;

    /// <summary>
    /// Creates a new ZlibHandler instance.
    /// </summary>
    public ZlibHandler(ILogger<ZlibHandler>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<byte[]> DecompressAsync(byte[] compressedData, long? expectedSize = null, CancellationToken cancellationToken = default)
    {
        if (compressedData is null || compressedData.Length < 6)
        {
            throw new DecompressionException("Compressed data is null or too short");
        }

        // Validate ZLIB header
        if (!ValidateHeader(compressedData.AsSpan(0, 2)))
        {
            _logger?.LogWarning("Invalid ZLIB header: 0x{Header:X4}", (compressedData[0] << 8) | compressedData[1]);
            throw new DecompressionException($"Invalid ZLIB header: 0x{compressedData[0]:X2}{compressedData[1]:X2}");
        }

        _logger?.LogDebug("Decompressing {CompressedSize} bytes", compressedData.Length);

        try
        {
            // Skip 2-byte ZLIB header, decompress, skip 4-byte Adler-32 checksum
            await using var inputStream = new MemoryStream(compressedData, 2, compressedData.Length - 6);
            await using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);

            // Use expected size for initial buffer if available
            var initialCapacity = expectedSize.HasValue && expectedSize.Value > 0
                ? (int)Math.Min(expectedSize.Value, int.MaxValue)
                : compressedData.Length * 4;

            await using var outputStream = new MemoryStream(initialCapacity);
            await deflateStream.CopyToAsync(outputStream, cancellationToken);

            var result = outputStream.ToArray();

            // Validate expected size
            if (expectedSize.HasValue && result.Length != expectedSize.Value)
            {
                _logger?.LogWarning(
                    "Decompressed size mismatch: expected {Expected}, got {Actual}",
                    expectedSize.Value, result.Length);
            }

            // Verify Adler-32 checksum
            var storedChecksum = ReadAdler32(compressedData.AsSpan(compressedData.Length - 4));
            var computedChecksum = ComputeAdler32(result);

            if (storedChecksum != computedChecksum)
            {
                _logger?.LogWarning(
                    "Adler-32 checksum mismatch: stored 0x{Stored:X8}, computed 0x{Computed:X8}",
                    storedChecksum, computedChecksum);
                // Don't throw - some saves may have incorrect checksums but still be valid
            }

            _logger?.LogDebug("Decompressed to {DecompressedSize} bytes", result.Length);
            return result;
        }
        catch (InvalidDataException ex)
        {
            _logger?.LogError(ex, "Deflate decompression failed");
            throw new DecompressionException("Failed to decompress data: corrupted deflate stream", ex);
        }
        catch (Exception ex) when (ex is not DecompressionException)
        {
            _logger?.LogError(ex, "Unexpected decompression error");
            throw new DecompressionException($"Unexpected decompression error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> CompressAsync(byte[] data, CompressionLevel level = CompressionLevel.Optimal, CancellationToken cancellationToken = default)
    {
        if (data is null || data.Length == 0)
        {
            throw new ArgumentException("Data to compress cannot be null or empty", nameof(data));
        }

        _logger?.LogDebug("Compressing {Size} bytes with level {Level}", data.Length, level);

        try
        {
            await using var outputStream = new MemoryStream();

            // Write ZLIB header
            outputStream.WriteByte(ZlibCompressionInfo);
            outputStream.WriteByte(GetFlagByte(level));

            // Compress with deflate
            await using (var deflateStream = new DeflateStream(outputStream, level, leaveOpen: true))
            {
                await deflateStream.WriteAsync(data, cancellationToken);
            }

            // Write Adler-32 checksum
            var checksum = ComputeAdler32(data);
            WriteAdler32(outputStream, checksum);

            var result = outputStream.ToArray();
            _logger?.LogDebug("Compressed to {CompressedSize} bytes (ratio: {Ratio:P1})",
                result.Length, (double)result.Length / data.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Compression failed");
            throw new CompressionException($"Failed to compress data: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public bool ValidateHeader(ReadOnlySpan<byte> header)
    {
        if (header.Length < 2)
            return false;

        var cmf = header[0];
        var flg = header[1];

        // Check compression method (should be 8 = deflate)
        var compressionMethod = cmf & 0x0F;
        if (compressionMethod != 8)
            return false;

        // Check FCHECK: (CMF * 256 + FLG) must be divisible by 31
        var check = (cmf * 256 + flg) % 31;
        return check == 0;
    }

    /// <summary>
    /// Computes Adler-32 checksum.
    /// </summary>
    public static uint ComputeAdler32(ReadOnlySpan<byte> data)
    {
        const uint ModAdler = 65521;
        uint a = 1, b = 0;

        foreach (var byteValue in data)
        {
            a = (a + byteValue) % ModAdler;
            b = (b + a) % ModAdler;
        }

        return (b << 16) | a;
    }

    private static uint ReadAdler32(ReadOnlySpan<byte> data)
    {
        return (uint)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3]);
    }

    private static void WriteAdler32(Stream stream, uint checksum)
    {
        stream.WriteByte((byte)(checksum >> 24));
        stream.WriteByte((byte)(checksum >> 16));
        stream.WriteByte((byte)(checksum >> 8));
        stream.WriteByte((byte)checksum);
    }

    private static byte GetFlagByte(CompressionLevel level)
    {
        // Flag byte calculation for ZLIB
        var flevel = level switch
        {
            CompressionLevel.NoCompression => 0,
            CompressionLevel.Fastest => 1,
            CompressionLevel.Optimal => 2,
            CompressionLevel.SmallestSize => 3,
            _ => 2
        };

        // Base flag byte with compression level
        var flg = (byte)(flevel << 6);

        // Calculate FCHECK to make (CMF * 256 + FLG) divisible by 31
        var cmf = ZlibCompressionInfo;
        var check = (cmf * 256 + flg) % 31;
        if (check != 0)
        {
            flg += (byte)(31 - check);
        }

        return flg;
    }
}

/// <summary>
/// Exception thrown during decompression.
/// </summary>
public class DecompressionException : Exception
{
    public DecompressionException(string message) : base(message) { }
    public DecompressionException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown during compression.
/// </summary>
public class CompressionException : Exception
{
    public CompressionException(string message) : base(message) { }
    public CompressionException(string message, Exception innerException) : base(message, innerException) { }
}
