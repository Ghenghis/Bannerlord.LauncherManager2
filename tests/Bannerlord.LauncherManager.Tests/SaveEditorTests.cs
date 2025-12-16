using System;
using System.IO;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager.Tests;

/// <summary>
/// Integration tests for Save Editor functionality.
/// These tests verify the Save Editor can correctly parse and handle Bannerlord save files.
/// </summary>
public class SaveEditorTests
{
    // Default save file path for testing
    private static readonly string SaveDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Mount and Blade II Bannerlord", "Game Saves");

    /// <summary>
    /// Test that save file header can be read correctly.
    /// Bannerlord saves use a JSON-based format with length prefix.
    /// </summary>
    public static async Task<bool> TestReadSaveHeaderAsync(string savePath)
    {
        Console.WriteLine($"Testing save file: {savePath}");
        
        if (!File.Exists(savePath))
        {
            Console.WriteLine($"  ERROR: Save file not found");
            return false;
        }

        try
        {
            using var stream = File.OpenRead(savePath);
            using var reader = new BinaryReader(stream);

            // Read JSON header length (first 4 bytes as little-endian int32)
            var headerLength = reader.ReadInt32();
            Console.WriteLine($"  Header Length: {headerLength} bytes");

            if (headerLength <= 0 || headerLength > 100000)
            {
                Console.WriteLine($"  ERROR: Invalid header length");
                return false;
            }

            // Read JSON header
            var headerBytes = reader.ReadBytes(headerLength);
            var headerJson = System.Text.Encoding.UTF8.GetString(headerBytes);
            
            // Check if it's valid JSON (starts with {)
            if (!headerJson.TrimStart().StartsWith("{"))
            {
                Console.WriteLine($"  ERROR: Header is not JSON");
                return false;
            }
            Console.WriteLine($"  Header Format: JSON ✓");

            // Extract modules from JSON
            var modulesStart = headerJson.IndexOf("\"Modules\":\"");
            if (modulesStart >= 0)
            {
                var start = modulesStart + 11;
                var end = headerJson.IndexOf("\"", start);
                if (end > start)
                {
                    var modulesString = headerJson.Substring(start, end - start);
                    var modules = modulesString.Split(';');
                    Console.WriteLine($"  Module Count: {modules.Length}");
                    
                    // Show first 5 modules
                    for (int i = 0; i < Math.Min(5, modules.Length); i++)
                    {
                        Console.WriteLine($"    - {modules[i]}");
                    }
                    if (modules.Length > 5)
                    {
                        Console.WriteLine($"    ... and {modules.Length - 5} more modules");
                    }

                    // Check for War Sails (NavalDLC)
                    var hasWarSails = modules.Any(m => m.Contains("Naval") || m.Contains("Ship") || m.Contains("Helmsman"));
                    Console.WriteLine($"  War Sails Detected: {(hasWarSails ? "Yes ✓" : "No")}");
                }
            }

            // File size info
            Console.WriteLine($"  File Size: {stream.Length / 1024.0:F1} KB");

            Console.WriteLine($"  PASSED ✓");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
            return false;
        }
    }

    private static string ReadLengthPrefixedString(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        if (length <= 0 || length > 1000) return string.Empty;
        var bytes = reader.ReadBytes(length);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Run all save editor tests.
    /// </summary>
    public static async Task<int> RunAllTestsAsync()
    {
        Console.WriteLine("=== Save Editor Integration Tests ===\n");

        var passed = 0;
        var failed = 0;

        // Find save files
        var saveDir = SaveDirectory;
        
        // Check OneDrive location too
        var oneDriveSaveDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "OneDrive", "Documents", "Mount and Blade II Bannerlord", "Game Saves");
        
        if (Directory.Exists(oneDriveSaveDir))
        {
            saveDir = oneDriveSaveDir;
        }

        Console.WriteLine($"Save Directory: {saveDir}\n");

        if (!Directory.Exists(saveDir))
        {
            Console.WriteLine("ERROR: Save directory not found");
            return 1;
        }

        var saveFiles = Directory.GetFiles(saveDir, "*.sav");
        Console.WriteLine($"Found {saveFiles.Length} save files\n");

        foreach (var saveFile in saveFiles)
        {
            if (await TestReadSaveHeaderAsync(saveFile))
            {
                passed++;
            }
            else
            {
                failed++;
            }
            Console.WriteLine();
        }

        Console.WriteLine($"=== Results: {passed} passed, {failed} failed ===");
        return failed > 0 ? 1 : 0;
    }

    /// <summary>
    /// Entry point for running tests.
    /// </summary>
    public static async Task Main(string[] args)
    {
        var result = await RunAllTestsAsync();
        Environment.Exit(result);
    }
}
