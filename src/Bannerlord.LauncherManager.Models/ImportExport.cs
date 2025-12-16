using System;
using System.Collections.Generic;

namespace Bannerlord.LauncherManager.Models;

/// <summary>
/// Supported import/export formats.
/// </summary>
public enum LoadOrderFormat
{
    /// <summary>
    /// Native LauncherManager JSON format.
    /// </summary>
    Native,

    /// <summary>
    /// Vortex mod manager format.
    /// </summary>
    Vortex,

    /// <summary>
    /// BUTRLoader format.
    /// </summary>
    BUTRLoader,

    /// <summary>
    /// Mod Organizer 2 format.
    /// </summary>
    ModOrganizer2,

    /// <summary>
    /// Simple text list (one mod per line).
    /// </summary>
    TextList,

    /// <summary>
    /// CSV format.
    /// </summary>
    Csv
}

/// <summary>
/// Entry in an imported load order.
/// </summary>
public class ImportedLoadOrderEntry
{
    /// <summary>
    /// Module ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Module name (if available).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Whether the module is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Position in load order.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Version from import (if available).
    /// </summary>
    public string? Version { get; set; }
}

/// <summary>
/// Result of an import operation.
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Whether import was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Format that was detected/used.
    /// </summary>
    public LoadOrderFormat Format { get; set; }

    /// <summary>
    /// Imported entries.
    /// </summary>
    public List<ImportedLoadOrderEntry> Entries { get; set; } = new();

    /// <summary>
    /// Modules that were found and matched.
    /// </summary>
    public int MatchedModules { get; set; }

    /// <summary>
    /// Modules that couldn't be found.
    /// </summary>
    public int MissingModules { get; set; }

    /// <summary>
    /// List of missing module IDs.
    /// </summary>
    public List<string> MissingModuleIds { get; set; } = new();

    /// <summary>
    /// Warnings generated during import.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    public static ImportResult AsSuccess(LoadOrderFormat format, List<ImportedLoadOrderEntry> entries) =>
        new() { Success = true, Format = format, Entries = entries };

    public static ImportResult AsError(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Options for export operations.
/// </summary>
public class ExportOptions
{
    /// <summary>
    /// Target format.
    /// </summary>
    public LoadOrderFormat Format { get; set; } = LoadOrderFormat.Native;

    /// <summary>
    /// Whether to include disabled modules.
    /// </summary>
    public bool IncludeDisabled { get; set; } = true;

    /// <summary>
    /// Whether to include version information.
    /// </summary>
    public bool IncludeVersions { get; set; } = true;

    /// <summary>
    /// Whether to include native modules.
    /// </summary>
    public bool IncludeNative { get; set; } = false;

    /// <summary>
    /// Custom header/comment for export.
    /// </summary>
    public string? Header { get; set; }
}

/// <summary>
/// Result of an export operation.
/// </summary>
public class ExportResult
{
    /// <summary>
    /// Whether export was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The exported content.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Format used for export.
    /// </summary>
    public LoadOrderFormat Format { get; set; }

    /// <summary>
    /// Number of modules exported.
    /// </summary>
    public int ModuleCount { get; set; }

    /// <summary>
    /// Suggested file extension.
    /// </summary>
    public string FileExtension { get; set; } = ".json";

    public static ExportResult AsSuccess(string content, LoadOrderFormat format, int count, string extension) =>
        new() { Success = true, Content = content, Format = format, ModuleCount = count, FileExtension = extension };

    public static ExportResult AsError(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Vortex-specific load order structure.
/// </summary>
public class VortexLoadOrder
{
    public string? GameId { get; set; }
    public List<VortexLoadOrderEntry> LoadOrder { get; set; } = new();
}

public class VortexLoadOrderEntry
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public bool Enabled { get; set; }
    public int Index { get; set; }
    public string? ModId { get; set; }
}

/// <summary>
/// BUTRLoader-specific configuration.
/// </summary>
public class BUTRLoaderConfig
{
    public List<BUTRLoaderEntry> Modules { get; set; } = new();
}

public class BUTRLoaderEntry
{
    public string? Id { get; set; }
    public bool IsSelected { get; set; }
}
