using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager.Tests;

/// <summary>
/// Advanced Save Editor tests with backup, edit, and verify capabilities.
/// Tests editing of save file header data and validates changes.
/// </summary>
public class SaveEditorAdvancedTests
{
    private static readonly string SaveDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "OneDrive", "Documents", "Mount and Blade II Bannerlord", "Game Saves");

    private static readonly string BackupDirectory = Path.Combine(SaveDirectory, "TestBackups");

    /// <summary>
    /// Editable fields in the save header with their data types.
    /// </summary>
    public static readonly Dictionary<string, Type> EditableFields = new()
    {
        // Hero Stats
        { "MainHeroLevel", typeof(int) },
        { "MainHeroGold", typeof(int) },
        { "HealthPercentage", typeof(int) },
        { "CharacterName", typeof(string) },
        
        // Party Stats
        { "MainPartyFood", typeof(int) },
        { "MainPartyHealthyMemberCount", typeof(int) },
        { "MainPartyPrisonerMemberCount", typeof(int) },
        { "MainPartyWoundedMemberCount", typeof(int) },
        
        // Clan Stats
        { "ClanInfluence", typeof(int) },
        { "ClanFiefs", typeof(int) },
        
        // Game State
        { "DayLong", typeof(int) },
        { "IronmanMode", typeof(int) },
    };

    /// <summary>
    /// Read-only fields for reference (cannot be safely edited).
    /// </summary>
    public static readonly string[] ReadOnlyFields = new[]
    {
        "Modules",
        "ApplicationVersion",
        "CreationTime",
        "UniqueGameId",
        "Version",
        "ClanBannerCode",
        "MainHeroVisual",
    };

    /// <summary>
    /// Creates a backup of a save file before editing.
    /// </summary>
    public static string CreateBackup(string savePath)
    {
        if (!Directory.Exists(BackupDirectory))
            Directory.CreateDirectory(BackupDirectory);

        var fileName = Path.GetFileNameWithoutExtension(savePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(BackupDirectory, $"{fileName}_backup_{timestamp}.sav");

        File.Copy(savePath, backupPath, overwrite: true);
        Console.WriteLine($"  Backup created: {Path.GetFileName(backupPath)}");
        return backupPath;
    }

    /// <summary>
    /// Reads and parses the save header.
    /// </summary>
    public static (int headerLength, JsonObject header, byte[] rawData) ReadSaveHeader(string savePath)
    {
        var rawData = File.ReadAllBytes(savePath);
        var headerLength = BitConverter.ToInt32(rawData, 0);
        var headerJson = Encoding.UTF8.GetString(rawData, 4, headerLength);
        var header = JsonNode.Parse(headerJson)?.AsObject() 
            ?? throw new InvalidDataException("Failed to parse save header");
        return (headerLength, header, rawData);
    }

    /// <summary>
    /// Writes a modified save file with new header data.
    /// </summary>
    public static void WriteSave(string savePath, JsonObject header, byte[] originalData, int originalHeaderLength)
    {
        var listNode = header["List"]?.AsObject();
        if (listNode == null)
            throw new InvalidDataException("Header missing 'List' node");

        // Serialize new header
        var newHeaderJson = header.ToJsonString();
        var newHeaderBytes = Encoding.UTF8.GetBytes(newHeaderJson);
        var newHeaderLength = newHeaderBytes.Length;

        // Calculate data offset (after original header)
        var dataOffset = 4 + originalHeaderLength;
        var dataLength = originalData.Length - dataOffset;

        // Create new save file
        using var stream = File.Create(savePath);
        using var writer = new BinaryWriter(stream);

        // Write new header length
        writer.Write(newHeaderLength);
        // Write new header
        writer.Write(newHeaderBytes);
        // Write original compressed data unchanged
        writer.Write(originalData, dataOffset, dataLength);

        Console.WriteLine($"  Save written: {newHeaderLength} byte header + {dataLength} byte data");
    }

    /// <summary>
    /// Gets a field value from the save header.
    /// </summary>
    public static string? GetFieldValue(JsonObject header, string fieldName)
    {
        var listNode = header["List"]?.AsObject();
        return listNode?[fieldName]?.ToString();
    }

    /// <summary>
    /// Sets a field value in the save header.
    /// </summary>
    public static void SetFieldValue(JsonObject header, string fieldName, string value)
    {
        var listNode = header["List"]?.AsObject();
        if (listNode == null)
            throw new InvalidDataException("Header missing 'List' node");

        listNode[fieldName] = value;
    }

    /// <summary>
    /// Test: Edit gold and verify change persists.
    /// </summary>
    public static async Task<bool> TestEditGoldAsync(string savePath)
    {
        Console.WriteLine("\n=== Test: Edit Gold ===");
        
        try
        {
            // 1. Create backup
            var backupPath = CreateBackup(savePath);

            // 2. Read save
            var (headerLength, header, rawData) = ReadSaveHeader(savePath);
            var originalGold = GetFieldValue(header, "MainHeroGold");
            Console.WriteLine($"  Original Gold: {originalGold}");

            // 3. Modify gold
            var newGold = (int.Parse(originalGold ?? "0") + 10000).ToString();
            SetFieldValue(header, "MainHeroGold", newGold);
            Console.WriteLine($"  New Gold: {newGold}");

            // 4. Write modified save to test file
            var testPath = savePath.Replace(".sav", "_test_gold.sav");
            WriteSave(testPath, header, rawData, headerLength);

            // 5. Verify change
            var (_, verifyHeader, _) = ReadSaveHeader(testPath);
            var verifiedGold = GetFieldValue(verifyHeader, "MainHeroGold");
            
            var success = verifiedGold == newGold;
            Console.WriteLine($"  Verified Gold: {verifiedGold} - {(success ? "PASS ✓" : "FAIL ✗")}");

            // 6. Cleanup test file
            if (File.Exists(testPath))
                File.Delete(testPath);

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Test: Edit multiple fields at once.
    /// </summary>
    public static async Task<bool> TestEditMultipleFieldsAsync(string savePath)
    {
        Console.WriteLine("\n=== Test: Edit Multiple Fields ===");
        
        try
        {
            // 1. Create backup
            CreateBackup(savePath);

            // 2. Read save
            var (headerLength, header, rawData) = ReadSaveHeader(savePath);

            // 3. Store original values and set new values
            var edits = new Dictionary<string, (string? original, string newValue)>
            {
                { "MainHeroGold", (GetFieldValue(header, "MainHeroGold"), "99999") },
                { "MainPartyFood", (GetFieldValue(header, "MainPartyFood"), "100") },
                { "ClanInfluence", (GetFieldValue(header, "ClanInfluence"), "500") },
            };

            foreach (var (field, (original, newValue)) in edits)
            {
                Console.WriteLine($"  {field}: {original} -> {newValue}");
                SetFieldValue(header, field, newValue);
            }

            // 4. Write modified save
            var testPath = savePath.Replace(".sav", "_test_multi.sav");
            WriteSave(testPath, header, rawData, headerLength);

            // 5. Verify all changes
            var (_, verifyHeader, _) = ReadSaveHeader(testPath);
            var allPassed = true;

            foreach (var (field, (_, newValue)) in edits)
            {
                var verified = GetFieldValue(verifyHeader, field);
                var passed = verified == newValue;
                Console.WriteLine($"  Verify {field}: {verified} - {(passed ? "PASS ✓" : "FAIL ✗")}");
                allPassed &= passed;
            }

            // 6. Cleanup
            if (File.Exists(testPath))
                File.Delete(testPath);

            return allPassed;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Test: List all editable fields and their current values.
    /// </summary>
    public static void ListEditableFields(string savePath)
    {
        Console.WriteLine("\n=== Editable Fields Analysis ===");
        
        try
        {
            var (_, header, _) = ReadSaveHeader(savePath);
            var listNode = header["List"]?.AsObject();
            
            if (listNode == null)
            {
                Console.WriteLine("  ERROR: Could not read header");
                return;
            }

            Console.WriteLine("\n  EDITABLE FIELDS:");
            Console.WriteLine("  " + new string('-', 50));
            foreach (var (field, type) in EditableFields)
            {
                var value = listNode[field]?.ToString() ?? "(not found)";
                Console.WriteLine($"  {field,-35} = {value}");
            }

            Console.WriteLine("\n  READ-ONLY FIELDS:");
            Console.WriteLine("  " + new string('-', 50));
            foreach (var field in ReadOnlyFields)
            {
                var value = listNode[field]?.ToString() ?? "(not found)";
                // Truncate long values
                if (value.Length > 40)
                    value = value.Substring(0, 37) + "...";
                Console.WriteLine($"  {field,-35} = {value}");
            }

            // Check for War Sails specific data
            var modules = listNode["Modules"]?.ToString() ?? "";
            if (modules.Contains("Naval"))
            {
                Console.WriteLine("\n  WAR SAILS DETECTED - Additional naval data may be in compressed section");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// Test: Restore from backup.
    /// </summary>
    public static bool TestRestoreBackup(string originalPath, string backupPath)
    {
        Console.WriteLine("\n=== Test: Restore Backup ===");
        
        try
        {
            File.Copy(backupPath, originalPath, overwrite: true);
            Console.WriteLine($"  Restored from: {Path.GetFileName(backupPath)}");
            
            // Verify restore
            var (_, header, _) = ReadSaveHeader(originalPath);
            var gold = GetFieldValue(header, "MainHeroGold");
            Console.WriteLine($"  Verified Gold after restore: {gold}");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Run all advanced tests.
    /// </summary>
    public static async Task<int> RunAllTestsAsync()
    {
        Console.WriteLine("=== Save Editor Advanced Tests ===\n");

        // Find War Sails save
        var warSailsSave = Directory.GetFiles(SaveDirectory, "*warsails*.sav").FirstOrDefault()
            ?? Directory.GetFiles(SaveDirectory, "*.sav").FirstOrDefault();

        if (warSailsSave == null)
        {
            Console.WriteLine("ERROR: No save files found");
            return 1;
        }

        Console.WriteLine($"Using save: {Path.GetFileName(warSailsSave)}");

        // List all editable fields
        ListEditableFields(warSailsSave);

        // Run edit tests
        var passed = 0;
        var failed = 0;

        if (await TestEditGoldAsync(warSailsSave)) passed++; else failed++;
        if (await TestEditMultipleFieldsAsync(warSailsSave)) passed++; else failed++;

        Console.WriteLine($"\n=== Results: {passed} passed, {failed} failed ===");

        // List backups
        if (Directory.Exists(BackupDirectory))
        {
            var backups = Directory.GetFiles(BackupDirectory, "*.sav");
            Console.WriteLine($"\nBackups available in: {BackupDirectory}");
            foreach (var backup in backups.Take(5))
            {
                Console.WriteLine($"  - {Path.GetFileName(backup)}");
            }
        }

        return failed > 0 ? 1 : 0;
    }

    public static async Task Main(string[] args)
    {
        var result = await RunAllTestsAsync();
        Environment.Exit(result);
    }
}
