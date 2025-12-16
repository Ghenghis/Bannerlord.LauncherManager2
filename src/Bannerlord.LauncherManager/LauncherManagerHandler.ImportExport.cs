using Bannerlord.LauncherManager.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bannerlord.LauncherManager;

partial class LauncherManagerHandler
{
    private static readonly JsonSerializerOptions ImportExportJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// External<br/>
    /// Imports load order from a string in the specified format.
    /// </summary>
    public async Task<ImportResult> ImportLoadOrderAsync(string content, LoadOrderFormat? format = null)
    {
        try
        {
            // Auto-detect format if not specified
            format ??= DetectFormat(content);

            var entries = format switch
            {
                LoadOrderFormat.Native => ParseNativeFormat(content),
                LoadOrderFormat.Vortex => ParseVortexFormat(content),
                LoadOrderFormat.BUTRLoader => ParseBUTRLoaderFormat(content),
                LoadOrderFormat.ModOrganizer2 => ParseModOrganizer2Format(content),
                LoadOrderFormat.TextList => ParseTextListFormat(content),
                LoadOrderFormat.Csv => ParseCsvFormat(content),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };

            var result = ImportResult.AsSuccess(format.Value, entries);

            // Validate against installed modules
            var modules = await GetModulesAsync();
            var moduleIds = modules.Select(m => m.Id).ToHashSet();

            foreach (var entry in entries)
            {
                if (moduleIds.Contains(entry.Id))
                {
                    result.MatchedModules++;
                }
                else
                {
                    result.MissingModules++;
                    result.MissingModuleIds.Add(entry.Id);
                }
            }

            if (result.MissingModules > 0)
            {
                result.Warnings.Add($"{result.MissingModules} modules not found and will be skipped.");
            }

            return result;
        }
        catch (Exception ex)
        {
            return ImportResult.AsError($"Import failed: {ex.Message}");
        }
    }

    /// <summary>
    /// External<br/>
    /// Imports load order from a file.
    /// </summary>
    public async Task<ImportResult> ImportLoadOrderFromFileAsync(string filePath, LoadOrderFormat? format = null)
    {
        try
        {
            if (!File.Exists(filePath))
                return ImportResult.AsError("File not found.");

            var content = await File.ReadAllTextAsync(filePath);
            
            // Try to detect format from file extension if not specified
            if (format == null)
            {
                format = Path.GetExtension(filePath).ToLowerInvariant() switch
                {
                    ".json" => null, // Will auto-detect from content
                    ".txt" => LoadOrderFormat.TextList,
                    ".csv" => LoadOrderFormat.Csv,
                    _ => null
                };
            }

            return await ImportLoadOrderAsync(content, format);
        }
        catch (Exception ex)
        {
            return ImportResult.AsError($"Import from file failed: {ex.Message}");
        }
    }

    /// <summary>
    /// External<br/>
    /// Applies an imported load order.
    /// </summary>
    public async Task<bool> ApplyImportedLoadOrderAsync(ImportResult importResult)
    {
        if (!importResult.Success || importResult.Entries.Count == 0)
            return false;

        try
        {
            var loadOrder = new LoadOrder();
            var modules = await GetModulesAsync();
            var moduleDict = modules.ToDictionary(m => m.Id, m => m);

            foreach (var entry in importResult.Entries.Where(e => moduleDict.ContainsKey(e.Id)))
            {
                var module = moduleDict[entry.Id];
                loadOrder[entry.Id] = new LoadOrderEntry
                {
                    Id = entry.Id,
                    Name = module.Name,
                    IsSelected = entry.IsEnabled,
                    IsDisabled = false,
                    Index = entry.Index
                };
            }

            await SetGameParameterLoadOrderAsync(loadOrder);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// External<br/>
    /// Exports current load order in the specified format.
    /// </summary>
    public async Task<ExportResult> ExportLoadOrderAsync(ExportOptions? options = null)
    {
        options ??= new ExportOptions();

        try
        {
            var viewModels = await GetModuleViewModelsAsync();
            if (viewModels == null)
                return ExportResult.AsError("No modules available.");

            var entries = viewModels
                .Where(vm => options.IncludeDisabled || vm.IsSelected)
                .Where(vm => options.IncludeNative || !vm.ModuleInfoExtended.IsNative())
                .OrderBy(vm => vm.Index)
                .ToList();

            var content = options.Format switch
            {
                LoadOrderFormat.Native => ExportNativeFormat(entries, options),
                LoadOrderFormat.Vortex => ExportVortexFormat(entries, options),
                LoadOrderFormat.BUTRLoader => ExportBUTRLoaderFormat(entries),
                LoadOrderFormat.TextList => ExportTextListFormat(entries, options),
                LoadOrderFormat.Csv => ExportCsvFormat(entries, options),
                _ => ExportNativeFormat(entries, options)
            };

            var extension = options.Format switch
            {
                LoadOrderFormat.Native => ".json",
                LoadOrderFormat.Vortex => ".json",
                LoadOrderFormat.BUTRLoader => ".json",
                LoadOrderFormat.TextList => ".txt",
                LoadOrderFormat.Csv => ".csv",
                _ => ".json"
            };

            return ExportResult.AsSuccess(content, options.Format, entries.Count, extension);
        }
        catch (Exception ex)
        {
            return ExportResult.AsError($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// External<br/>
    /// Exports load order to a file.
    /// </summary>
    public async Task<ExportResult> ExportLoadOrderToFileAsync(string filePath, ExportOptions? options = null)
    {
        var result = await ExportLoadOrderAsync(options);
        
        if (!result.Success || string.IsNullOrEmpty(result.Content))
            return result;

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(filePath, result.Content);
            return result;
        }
        catch (Exception ex)
        {
            return ExportResult.AsError($"Export to file failed: {ex.Message}");
        }
    }

    /// <summary>
    /// External<br/>
    /// Converts between load order formats.
    /// </summary>
    public async Task<ExportResult> ConvertLoadOrderFormatAsync(string content, LoadOrderFormat sourceFormat, LoadOrderFormat targetFormat)
    {
        var importResult = await ImportLoadOrderAsync(content, sourceFormat);
        if (!importResult.Success)
            return ExportResult.AsError(importResult.ErrorMessage ?? "Import failed");

        // Build a temporary view model list from imports
        var modules = await GetModulesAsync();
        var moduleDict = modules.ToDictionary(m => m.Id, m => m);

        var viewModels = importResult.Entries
            .Where(e => moduleDict.ContainsKey(e.Id))
            .Select(e => new ModuleViewModel
            {
                ModuleInfoExtended = moduleDict[e.Id],
                IsSelected = e.IsEnabled,
                IsDisabled = false,
                Index = e.Index,
                IsValid = true
            })
            .ToList();

        var exportContent = targetFormat switch
        {
            LoadOrderFormat.Native => ExportNativeFormat(viewModels, new ExportOptions()),
            LoadOrderFormat.Vortex => ExportVortexFormat(viewModels, new ExportOptions()),
            LoadOrderFormat.BUTRLoader => ExportBUTRLoaderFormat(viewModels),
            LoadOrderFormat.TextList => ExportTextListFormat(viewModels, new ExportOptions()),
            LoadOrderFormat.Csv => ExportCsvFormat(viewModels, new ExportOptions()),
            _ => ExportNativeFormat(viewModels, new ExportOptions())
        };

        var extension = targetFormat switch
        {
            LoadOrderFormat.TextList => ".txt",
            LoadOrderFormat.Csv => ".csv",
            _ => ".json"
        };

        return ExportResult.AsSuccess(exportContent, targetFormat, viewModels.Count, extension);
    }

    /// <summary>
    /// External<br/>
    /// Detects the format of a load order string.
    /// </summary>
    public LoadOrderFormat DetectLoadOrderFormat(string content)
    {
        return DetectFormat(content);
    }

    private static LoadOrderFormat DetectFormat(string content)
    {
        content = content.Trim();

        // Try JSON formats
        if (content.StartsWith("{") || content.StartsWith("["))
        {
            try
            {
                // Try Vortex format
                if (content.Contains("\"loadOrder\"") || content.Contains("\"gameId\""))
                    return LoadOrderFormat.Vortex;

                // Try BUTRLoader format
                if (content.Contains("\"isSelected\""))
                    return LoadOrderFormat.BUTRLoader;

                // Try native format
                if (content.Contains("\"isSelected\"") && content.Contains("\"index\""))
                    return LoadOrderFormat.Native;

                return LoadOrderFormat.Native;
            }
            catch
            {
                // Fall through to other formats
            }
        }

        // Check for CSV
        if (content.Contains(",") && (content.Contains("Id,") || content.Contains("id,")))
            return LoadOrderFormat.Csv;

        // Default to text list
        return LoadOrderFormat.TextList;
    }

    private static List<ImportedLoadOrderEntry> ParseNativeFormat(string content)
    {
        var loadOrder = JsonSerializer.Deserialize<Dictionary<string, LoadOrderEntry>>(content, ImportExportJsonOptions);
        if (loadOrder == null)
            return new List<ImportedLoadOrderEntry>();

        return loadOrder.Values.Select(e => new ImportedLoadOrderEntry
        {
            Id = e.Id,
            Name = e.Name,
            IsEnabled = e.IsSelected,
            Index = e.Index
        }).OrderBy(e => e.Index).ToList();
    }

    private static List<ImportedLoadOrderEntry> ParseVortexFormat(string content)
    {
        var vortex = JsonSerializer.Deserialize<VortexLoadOrder>(content, ImportExportJsonOptions);
        if (vortex?.LoadOrder == null)
            return new List<ImportedLoadOrderEntry>();

        return vortex.LoadOrder.Select(e => new ImportedLoadOrderEntry
        {
            Id = e.Id ?? string.Empty,
            Name = e.Name,
            IsEnabled = e.Enabled,
            Index = e.Index
        }).OrderBy(e => e.Index).ToList();
    }

    private static List<ImportedLoadOrderEntry> ParseBUTRLoaderFormat(string content)
    {
        var butr = JsonSerializer.Deserialize<BUTRLoaderConfig>(content, ImportExportJsonOptions);
        if (butr?.Modules == null)
            return new List<ImportedLoadOrderEntry>();

        return butr.Modules.Select((e, i) => new ImportedLoadOrderEntry
        {
            Id = e.Id ?? string.Empty,
            IsEnabled = e.IsSelected,
            Index = i
        }).ToList();
    }

    private static List<ImportedLoadOrderEntry> ParseModOrganizer2Format(string content)
    {
        // MO2 uses a simple text format with + or - prefix
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var entries = new List<ImportedLoadOrderEntry>();
        var index = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                continue;

            var enabled = !trimmed.StartsWith("-");
            var id = trimmed.TrimStart('+', '-', ' ');

            entries.Add(new ImportedLoadOrderEntry
            {
                Id = id,
                IsEnabled = enabled,
                Index = index++
            });
        }

        return entries;
    }

    private static List<ImportedLoadOrderEntry> ParseTextListFormat(string content)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var entries = new List<ImportedLoadOrderEntry>();
        var index = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith("//"))
                continue;

            entries.Add(new ImportedLoadOrderEntry
            {
                Id = trimmed,
                IsEnabled = true,
                Index = index++
            });
        }

        return entries;
    }

    private static List<ImportedLoadOrderEntry> ParseCsvFormat(string content)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var entries = new List<ImportedLoadOrderEntry>();
        var isFirstLine = true;
        var index = 0;

        foreach (var line in lines)
        {
            if (isFirstLine)
            {
                isFirstLine = false;
                continue; // Skip header
            }

            var parts = line.Split(',');
            if (parts.Length == 0) continue;

            entries.Add(new ImportedLoadOrderEntry
            {
                Id = parts[0].Trim().Trim('"'),
                Name = parts.Length > 1 ? parts[1].Trim().Trim('"') : null,
                IsEnabled = parts.Length > 2 && bool.TryParse(parts[2].Trim(), out var enabled) && enabled,
                Index = index++
            });
        }

        return entries;
    }

    private static string ExportNativeFormat(IEnumerable<IModuleViewModel> viewModels, ExportOptions options)
    {
        var loadOrder = new Dictionary<string, object>();

        foreach (var vm in viewModels)
        {
            loadOrder[vm.ModuleInfoExtended.Id] = new
            {
                id = vm.ModuleInfoExtended.Id,
                name = vm.ModuleInfoExtended.Name,
                isSelected = vm.IsSelected,
                isDisabled = vm.IsDisabled,
                index = vm.Index,
                version = options.IncludeVersions ? vm.ModuleInfoExtended.Version.ToString() : null
            };
        }

        return JsonSerializer.Serialize(loadOrder, ImportExportJsonOptions);
    }

    private static string ExportVortexFormat(IEnumerable<IModuleViewModel> viewModels, ExportOptions options)
    {
        var vortex = new
        {
            gameId = "mountandblade2bannerlord",
            loadOrder = viewModels.Select(vm => new
            {
                id = vm.ModuleInfoExtended.Id,
                name = vm.ModuleInfoExtended.Name,
                enabled = vm.IsSelected,
                index = vm.Index
            }).ToList()
        };

        return JsonSerializer.Serialize(vortex, ImportExportJsonOptions);
    }

    private static string ExportBUTRLoaderFormat(IEnumerable<IModuleViewModel> viewModels)
    {
        var butr = new
        {
            modules = viewModels.Select(vm => new
            {
                id = vm.ModuleInfoExtended.Id,
                isSelected = vm.IsSelected
            }).ToList()
        };

        return JsonSerializer.Serialize(butr, ImportExportJsonOptions);
    }

    private static string ExportTextListFormat(IEnumerable<IModuleViewModel> viewModels, ExportOptions options)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(options.Header))
        {
            sb.AppendLine($"# {options.Header}");
            sb.AppendLine();
        }

        foreach (var vm in viewModels.Where(v => v.IsSelected))
        {
            sb.AppendLine(vm.ModuleInfoExtended.Id);
        }

        return sb.ToString();
    }

    private static string ExportCsvFormat(IEnumerable<IModuleViewModel> viewModels, ExportOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Enabled,Index" + (options.IncludeVersions ? ",Version" : ""));

        foreach (var vm in viewModels)
        {
            var line = $"\"{vm.ModuleInfoExtended.Id}\",\"{vm.ModuleInfoExtended.Name}\",{vm.IsSelected},{vm.Index}";
            if (options.IncludeVersions)
                line += $",\"{vm.ModuleInfoExtended.Version}\"";
            sb.AppendLine(line);
        }

        return sb.ToString();
    }
}
