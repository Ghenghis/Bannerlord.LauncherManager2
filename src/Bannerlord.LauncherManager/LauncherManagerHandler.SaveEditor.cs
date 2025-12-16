using Bannerlord.LauncherManager.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager;

partial class LauncherManagerHandler
{
    private const string SaveEditorBackupDir = "_SaveEditorBackups";
    private SaveEditData? _currentSaveData;

    private static readonly JsonSerializerOptions SaveEditorJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// External<br/>
    /// Loads a save file for editing.
    /// </summary>
    public async Task<SaveEditData> LoadSaveForEditAsync(string savePath, SaveLoadOptions? options = null)
    {
        options ??= new SaveLoadOptions();

        if (!File.Exists(savePath))
            throw new FileNotFoundException("Save file not found", savePath);

        var saveData = new SaveEditData
        {
            FilePath = savePath,
            FileName = Path.GetFileName(savePath)
        };

        try
        {
            await using var stream = File.OpenRead(savePath);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            // Read and validate magic number "TWSV"
            var magic = reader.ReadBytes(4);
            if (!magic.SequenceEqual(new byte[] { 0x54, 0x57, 0x53, 0x56 }))
            {
                if (options.Permissive)
                {
                    saveData.Header.HeaderVersion = 0;
                }
                else
                {
                    throw new InvalidOperationException("Invalid save file format");
                }
            }

            // Read header
            saveData.Header = await ReadSaveHeaderAsync(reader);

            // Check for War Sails
            saveData.HasWarSails = saveData.Header.Modules.Any(m => 
                m.Id.Equals("WarSails", StringComparison.OrdinalIgnoreCase));

            if (!options.MetadataOnly)
            {
                // Read compressed data
                var compressedLength = reader.ReadInt32();
                var uncompressedLength = reader.ReadInt32();
                var compressedData = reader.ReadBytes(compressedLength);

                // Decompress
                var decompressedData = await DecompressSaveDataAsync(compressedData, uncompressedLength);

                // Parse campaign data
                await ParseCampaignDataAsync(decompressedData, saveData);

                // Compute checksum
                saveData.Checksum = ComputeSHA256(decompressedData);
            }

            // Extract metadata
            saveData.Metadata = ExtractMetadata(saveData);

            _currentSaveData = saveData;
        }
        catch (Exception ex)
        {
            if (!options.Permissive)
                throw;

            saveData.Metadata.CharacterName = "Unknown (Parse Error)";
        }

        return saveData;
    }

    /// <summary>
    /// External<br/>
    /// Gets the currently loaded save data.
    /// </summary>
    public Task<SaveEditData?> GetCurrentSaveDataAsync()
    {
        return Task.FromResult(_currentSaveData);
    }

    /// <summary>
    /// External<br/>
    /// Saves the edited save file.
    /// </summary>
    public async Task<EditResult> SaveEditedSaveAsync(string? targetPath = null, SaveWriteOptions? options = null)
    {
        options ??= new SaveWriteOptions();
        var result = new EditResult();

        if (_currentSaveData == null)
        {
            result.Success = false;
            result.ErrorMessage = "No save data loaded";
            return result;
        }

        targetPath ??= _currentSaveData.FilePath;

        try
        {
            // Create backup if requested
            if (options.CreateBackup)
            {
                await CreateSaveBackupAsync(_currentSaveData.FilePath);
            }

            // Write to temp file first (atomic write)
            var tempPath = targetPath + ".tmp";
            
            // TODO: Implement full save writing with serialization
            // For now, we validate that edits are tracked
            
            if (_currentSaveData.IsModified)
            {
                // Serialize and write save data
                await WriteSaveDataAsync(tempPath, _currentSaveData, options);

                // Verify if requested
                if (options.VerifyAfterSave)
                {
                    var verified = await VerifySaveIntegrityAsync(tempPath);
                    if (!verified)
                    {
                        File.Delete(tempPath);
                        result.Success = false;
                        result.ErrorMessage = "Save verification failed";
                        return result;
                    }
                }

                // Atomic rename
                if (File.Exists(targetPath))
                    File.Delete(targetPath);
                File.Move(tempPath, targetPath);

                _currentSaveData.IsModified = false;
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// External<br/>
    /// Edits a hero in the current save.
    /// </summary>
    public async Task<EditResult> EditHeroAsync(HeroEditRequest request)
    {
        var result = new EditResult();

        if (_currentSaveData == null)
        {
            result.Success = false;
            result.ErrorMessage = "No save data loaded";
            return result;
        }

        var hero = _currentSaveData.Heroes.FirstOrDefault(h => h.Id == request.HeroId);
        if (hero == null)
        {
            result.Success = false;
            result.ErrorMessage = "Hero not found";
            return result;
        }

        // Validate and apply edits
        if (request.Level.HasValue)
        {
            if (request.Level.Value < 1 || request.Level.Value > 62)
            {
                result.Validations.Add(new EditValidation
                {
                    Severity = EditValidationSeverity.Error,
                    Field = "Level",
                    Message = "Level must be between 1 and 62"
                });
            }
            else
            {
                hero.Level = request.Level.Value;
            }
        }

        if (request.Experience.HasValue)
        {
            if (request.Experience.Value < 0)
            {
                result.Validations.Add(new EditValidation
                {
                    Severity = EditValidationSeverity.Error,
                    Field = "Experience",
                    Message = "Experience cannot be negative"
                });
            }
            else
            {
                hero.Experience = request.Experience.Value;
            }
        }

        if (request.Gold.HasValue)
        {
            if (request.Gold.Value < 0)
            {
                result.Validations.Add(new EditValidation
                {
                    Severity = EditValidationSeverity.Error,
                    Field = "Gold",
                    Message = "Gold cannot be negative"
                });
            }
            else
            {
                hero.Gold = request.Gold.Value;
            }
        }

        if (request.Health.HasValue)
        {
            if (request.Health.Value < 0 || request.Health.Value > hero.MaxHealth)
            {
                result.Validations.Add(new EditValidation
                {
                    Severity = EditValidationSeverity.Warning,
                    Field = "Health",
                    Message = $"Health should be between 0 and {hero.MaxHealth}"
                });
            }
            hero.Health = Math.Clamp(request.Health.Value, 0, hero.MaxHealth);
        }

        if (request.Attributes != null)
        {
            // Validate attributes (0-10 base, can be higher with perks)
            var attrs = request.Attributes;
            var maxAttr = 15; // Allow some buffer for perks

            if (attrs.Vigor < 0 || attrs.Vigor > maxAttr ||
                attrs.Control < 0 || attrs.Control > maxAttr ||
                attrs.Endurance < 0 || attrs.Endurance > maxAttr ||
                attrs.Cunning < 0 || attrs.Cunning > maxAttr ||
                attrs.Social < 0 || attrs.Social > maxAttr ||
                attrs.Intelligence < 0 || attrs.Intelligence > maxAttr)
            {
                result.Validations.Add(new EditValidation
                {
                    Severity = EditValidationSeverity.Warning,
                    Field = "Attributes",
                    Message = $"Attributes should typically be between 0 and 10"
                });
            }

            hero.Attributes = attrs;
        }

        if (request.SkillLevels != null)
        {
            foreach (var (skillId, level) in request.SkillLevels)
            {
                var skill = hero.Skills.FirstOrDefault(s => s.SkillId == skillId);
                if (skill != null)
                {
                    if (level < 0 || level > 300)
                    {
                        result.Validations.Add(new EditValidation
                        {
                            Severity = EditValidationSeverity.Error,
                            Field = $"Skill:{skillId}",
                            Message = "Skill level must be between 0 and 300"
                        });
                    }
                    else
                    {
                        skill.Level = level;
                    }
                }
            }
        }

        if (request.PerksToAdd != null)
        {
            foreach (var perk in request.PerksToAdd)
            {
                if (!hero.Perks.Contains(perk))
                    hero.Perks.Add(perk);
            }
        }

        if (request.PerksToRemove != null)
        {
            foreach (var perk in request.PerksToRemove)
            {
                hero.Perks.Remove(perk);
            }
        }

        result.Success = !result.HasErrors;
        if (result.Success)
        {
            _currentSaveData.IsModified = true;
        }

        return await Task.FromResult(result);
    }

    /// <summary>
    /// External<br/>
    /// Edits a party in the current save.
    /// </summary>
    public async Task<EditResult> EditPartyAsync(PartyEditRequest request)
    {
        var result = new EditResult();

        if (_currentSaveData == null)
        {
            result.Success = false;
            result.ErrorMessage = "No save data loaded";
            return result;
        }

        var party = _currentSaveData.Parties.FirstOrDefault(p => p.Id == request.PartyId);
        if (party == null)
        {
            result.Success = false;
            result.ErrorMessage = "Party not found";
            return result;
        }

        if (request.Gold.HasValue)
        {
            if (request.Gold.Value < 0)
            {
                result.Validations.Add(new EditValidation
                {
                    Severity = EditValidationSeverity.Error,
                    Field = "Gold",
                    Message = "Gold cannot be negative"
                });
            }
            else
            {
                party.Gold = request.Gold.Value;
            }
        }

        if (request.HealAllWounded == true)
        {
            foreach (var troop in party.Troops)
            {
                troop.WoundedCount = 0;
            }
        }

        if (request.TroopsToAdd != null)
        {
            foreach (var (troopId, count) in request.TroopsToAdd)
            {
                var existing = party.Troops.FirstOrDefault(t => t.TroopId == troopId);
                if (existing != null)
                {
                    existing.Count += count;
                }
                else
                {
                    party.Troops.Add(new TroopStackData
                    {
                        TroopId = troopId,
                        TroopName = troopId, // Would need lookup
                        Count = count
                    });
                }
            }
        }

        if (request.TroopsToRemove != null)
        {
            foreach (var (troopId, count) in request.TroopsToRemove)
            {
                var existing = party.Troops.FirstOrDefault(t => t.TroopId == troopId);
                if (existing != null)
                {
                    existing.Count -= count;
                    if (existing.Count <= 0)
                    {
                        party.Troops.Remove(existing);
                    }
                }
            }
        }

        result.Success = !result.HasErrors;
        if (result.Success)
        {
            _currentSaveData.IsModified = true;
        }

        return await Task.FromResult(result);
    }

    /// <summary>
    /// External<br/>
    /// Edits a ship in the current save (War Sails).
    /// </summary>
    public async Task<EditResult> EditShipAsync(ShipEditRequest request)
    {
        var result = new EditResult();

        if (_currentSaveData == null)
        {
            result.Success = false;
            result.ErrorMessage = "No save data loaded";
            return result;
        }

        if (!_currentSaveData.HasWarSails)
        {
            result.Success = false;
            result.ErrorMessage = "Save does not have War Sails data";
            return result;
        }

        var ship = _currentSaveData.Ships.FirstOrDefault(s => s.Id == request.ShipId);
        if (ship == null)
        {
            result.Success = false;
            result.ErrorMessage = "Ship not found";
            return result;
        }

        if (request.Name != null)
        {
            ship.Name = request.Name;
        }

        if (request.RepairFully == true)
        {
            ship.CurrentHull = ship.MaxHull;
        }
        else if (request.CurrentHull.HasValue)
        {
            if (request.CurrentHull.Value < 0 || request.CurrentHull.Value > ship.MaxHull)
            {
                result.Validations.Add(new EditValidation
                {
                    Severity = EditValidationSeverity.Error,
                    Field = "CurrentHull",
                    Message = $"Hull must be between 0 and {ship.MaxHull}"
                });
            }
            else
            {
                ship.CurrentHull = request.CurrentHull.Value;
            }
        }

        if (request.CrewCount.HasValue)
        {
            if (request.CrewCount.Value < 0 || request.CrewCount.Value > ship.MaxCrew)
            {
                result.Validations.Add(new EditValidation
                {
                    Severity = EditValidationSeverity.Warning,
                    Field = "CrewCount",
                    Message = $"Crew should be between 0 and {ship.MaxCrew}"
                });
            }
            ship.CrewCount = Math.Clamp(request.CrewCount.Value, 0, ship.MaxCrew);
        }

        if (request.UpgradesToAdd != null)
        {
            foreach (var upgrade in request.UpgradesToAdd)
            {
                if (!ship.Upgrades.Contains(upgrade))
                    ship.Upgrades.Add(upgrade);
            }
        }

        if (request.UpgradesToRemove != null)
        {
            foreach (var upgrade in request.UpgradesToRemove)
            {
                ship.Upgrades.Remove(upgrade);
            }
        }

        result.Success = !result.HasErrors;
        if (result.Success)
        {
            _currentSaveData.IsModified = true;
        }

        return await Task.FromResult(result);
    }

    /// <summary>
    /// External<br/>
    /// Gets all heroes from current save.
    /// </summary>
    public Task<IReadOnlyList<SaveHeroData>> GetSaveHeroesAsync()
    {
        if (_currentSaveData == null)
            return Task.FromResult<IReadOnlyList<SaveHeroData>>(new List<SaveHeroData>());

        return Task.FromResult<IReadOnlyList<SaveHeroData>>(_currentSaveData.Heroes);
    }

    /// <summary>
    /// External<br/>
    /// Gets all parties from current save.
    /// </summary>
    public Task<IReadOnlyList<SavePartyData>> GetSavePartiesAsync()
    {
        if (_currentSaveData == null)
            return Task.FromResult<IReadOnlyList<SavePartyData>>(new List<SavePartyData>());

        return Task.FromResult<IReadOnlyList<SavePartyData>>(_currentSaveData.Parties);
    }

    /// <summary>
    /// External<br/>
    /// Gets all fleets from current save (War Sails).
    /// </summary>
    public Task<IReadOnlyList<SaveFleetData>> GetSaveFleetsAsync()
    {
        if (_currentSaveData == null || !_currentSaveData.HasWarSails)
            return Task.FromResult<IReadOnlyList<SaveFleetData>>(new List<SaveFleetData>());

        return Task.FromResult<IReadOnlyList<SaveFleetData>>(_currentSaveData.Fleets);
    }

    /// <summary>
    /// External<br/>
    /// Gets all ships from current save (War Sails).
    /// </summary>
    public Task<IReadOnlyList<SaveShipData>> GetSaveShipsAsync()
    {
        if (_currentSaveData == null || !_currentSaveData.HasWarSails)
            return Task.FromResult<IReadOnlyList<SaveShipData>>(new List<SaveShipData>());

        return Task.FromResult<IReadOnlyList<SaveShipData>>(_currentSaveData.Ships);
    }

    /// <summary>
    /// External<br/>
    /// Verifies save file integrity.
    /// </summary>
    public async Task<bool> VerifySaveIntegrityAsync(string savePath)
    {
        try
        {
            await using var stream = File.OpenRead(savePath);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            // Check magic number
            var magic = reader.ReadBytes(4);
            if (!magic.SequenceEqual(new byte[] { 0x54, 0x57, 0x53, 0x56 }))
                return false;

            // Basic header validation
            var headerVersion = reader.ReadInt32();
            if (headerVersion < 5 || headerVersion > 10)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// External<br/>
    /// Creates a backup of a save file.
    /// </summary>
    public async Task<string> CreateSaveBackupAsync(string savePath)
    {
        var installPath = await GetInstallPathAsync();
        var backupDir = Path.Combine(installPath, SaveEditorBackupDir);
        Directory.CreateDirectory(backupDir);

        var fileName = Path.GetFileName(savePath);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HHmmss");
        var backupName = $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}.sav.gz";
        var backupPath = Path.Combine(backupDir, backupName);

        await using var input = File.OpenRead(savePath);
        await using var output = File.Create(backupPath);
        await using var gzip = new GZipStream(output, CompressionLevel.Optimal);
        await input.CopyToAsync(gzip);

        return backupPath;
    }

    /// <summary>
    /// External<br/>
    /// Closes the current save without saving.
    /// </summary>
    public Task CloseSaveEditorAsync()
    {
        _currentSaveData = null;
        return Task.CompletedTask;
    }

    private async Task<SaveHeader> ReadSaveHeaderAsync(BinaryReader reader)
    {
        var header = new SaveHeader
        {
            HeaderVersion = reader.ReadInt32(),
            GameVersionMajor = reader.ReadInt32(),
            GameVersionMinor = reader.ReadInt32(),
            GameVersionBuild = reader.ReadInt32()
        };

        var moduleCount = reader.ReadInt32();
        for (var i = 0; i < moduleCount; i++)
        {
            header.Modules.Add(new SaveModuleInfo
            {
                Id = ReadLengthPrefixedString(reader),
                Version = ReadLengthPrefixedString(reader)
            });
        }

        return await Task.FromResult(header);
    }

    private static string ReadLengthPrefixedString(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        if (length <= 0) return string.Empty;
        var bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    private static async Task<byte[]> DecompressSaveDataAsync(byte[] compressed, int expectedSize)
    {
        using var input = new MemoryStream(compressed);
        using var output = new MemoryStream(expectedSize);

        // Skip ZLIB header if present
        if (compressed.Length > 2 && compressed[0] == 0x78)
        {
            input.Position = 2;
        }

        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        await deflate.CopyToAsync(output);

        return output.ToArray();
    }

    private static async Task ParseCampaignDataAsync(byte[] data, SaveEditData saveData)
    {
        // Simplified parsing - would need full implementation for production
        // This creates placeholder data for demonstration
        saveData.MainHero = new SaveHeroData
        {
            Id = "main_hero",
            StringId = "main_hero",
            Name = "Main Hero",
            IsMainHero = true,
            Level = 1,
            Gold = 1000,
            Health = 100,
            MaxHealth = 100,
            Attributes = new HeroAttributes
            {
                Vigor = 3,
                Control = 3,
                Endurance = 3,
                Cunning = 3,
                Social = 3,
                Intelligence = 3
            }
        };

        saveData.Heroes.Add(saveData.MainHero);

        await Task.CompletedTask;
    }

    private static SaveMetadataInfo ExtractMetadata(SaveEditData saveData)
    {
        return new SaveMetadataInfo
        {
            CharacterName = saveData.MainHero?.Name ?? "Unknown",
            CharacterLevel = saveData.MainHero?.Level ?? 1,
            SaveTimestamp = DateTime.UtcNow
        };
    }

    private static async Task WriteSaveDataAsync(string path, SaveEditData saveData, SaveWriteOptions options)
    {
        // Placeholder for full write implementation
        await Task.CompletedTask;
    }

    private static string ComputeSHA256(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
