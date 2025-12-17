// <copyright file="ValidationService.cs" company="BUTR Team">
// Copyright (c) BUTR Team. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Bannerlord.SaveEditor.Core.Validation;

using Bannerlord.SaveEditor.Core.Entities;
using Bannerlord.SaveEditor.Core.Models;
using Bannerlord.SaveEditor.Core.WarSails;
using Microsoft.Extensions.Logging;

/// <summary>
/// Interface for validation service.
/// </summary>
public interface IValidationService
{
    ValidationReport Validate(SaveFile save);
    ValidationReport ValidateHero(HeroData hero);
    ValidationReport ValidateParty(PartyData party);
    ValidationReport ValidateFleet(FleetData fleet);
    void RegisterValidator<T>(IValidator<T> validator);
    void SetValidationMode(ValidationMode mode);
}

/// <summary>
/// Validation modes.
/// </summary>
public enum ValidationMode
{
    /// <summary>Strict validation - all warnings treated as errors.</summary>
    Strict,
    /// <summary>Normal validation - errors and warnings separate.</summary>
    Normal,
    /// <summary>Permissive - only critical errors reported.</summary>
    Permissive
}

/// <summary>
/// Validation report containing all issues found.
/// </summary>
public sealed class ValidationReport
{
    public bool IsValid => Errors.Count == 0;
    public bool HasWarnings => Warnings.Count > 0;
    public IList<ValidationIssue> Errors { get; } = new List<ValidationIssue>();
    public IList<ValidationIssue> Warnings { get; } = new List<ValidationIssue>();
    public IList<ValidationIssue> Info { get; } = new List<ValidationIssue>();

    public void AddError(string code, string message, string? path = null, object? context = null)
        => Errors.Add(new ValidationIssue(ValidationSeverity.Error, code, message, path, context));

    public void AddWarning(string code, string message, string? path = null, object? context = null)
        => Warnings.Add(new ValidationIssue(ValidationSeverity.Warning, code, message, path, context));

    public void AddInfo(string code, string message, string? path = null, object? context = null)
        => Info.Add(new ValidationIssue(ValidationSeverity.Info, code, message, path, context));

    public void Merge(ValidationReport other)
    {
        foreach (var error in other.Errors) Errors.Add(error);
        foreach (var warning in other.Warnings) Warnings.Add(warning);
        foreach (var info in other.Info) Info.Add(info);
    }
}

/// <summary>
/// Single validation issue.
/// </summary>
public sealed class ValidationIssue
{
    public ValidationSeverity Severity { get; }
    public string Code { get; }
    public string Message { get; }
    public string? Path { get; }
    public object? Context { get; }

    public ValidationIssue(ValidationSeverity severity, string code, string message, string? path = null, object? context = null)
    {
        Severity = severity;
        Code = code;
        Message = message;
        Path = path;
        Context = context;
    }

    public override string ToString() => $"[{Severity}] {Code}: {Message}" + (Path != null ? $" at {Path}" : "");
}

/// <summary>
/// Validation severity levels.
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Generic validator interface.
/// </summary>
public interface IValidator<T>
{
    ValidationReport Validate(T entity, ValidationContext context);
}

/// <summary>
/// Validation context with shared state.
/// </summary>
public sealed class ValidationContext
{
    public ValidationMode Mode { get; init; } = ValidationMode.Normal;
    public SaveFile? Save { get; init; }
    public Dictionary<MBGUID, object> EntityCache { get; } = new();
    public string CurrentPath { get; set; } = string.Empty;
}

/// <summary>
/// Implementation of validation service.
/// </summary>
public sealed class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService>? _logger;
    private readonly Dictionary<Type, object> _validators = new();
    private ValidationMode _mode = ValidationMode.Normal;

    public ValidationService(ILogger<ValidationService>? logger = null)
    {
        _logger = logger;
        RegisterDefaultValidators();
    }

    public void SetValidationMode(ValidationMode mode) => _mode = mode;

    public void RegisterValidator<T>(IValidator<T> validator)
    {
        _validators[typeof(T)] = validator;
    }

    public ValidationReport Validate(SaveFile save)
    {
        var report = new ValidationReport();
        var context = new ValidationContext { Mode = _mode, Save = save };

        _logger?.LogDebug("Validating save file: {Name}", save.Name);

        // Validate header
        if (string.IsNullOrEmpty(save.Header.GameVersion))
            report.AddWarning("HEADER_001", "Game version is missing from header");

        if (save.Header.Version < 1 || save.Header.Version > 10)
            report.AddWarning("HEADER_002", $"Unusual save version: {save.Header.Version}");

        // Validate heroes
        foreach (var hero in save.Heroes)
        {
            context.CurrentPath = $"Heroes[{hero.Id}]";
            report.Merge(ValidateHero(hero));
        }

        // Validate parties
        foreach (var party in save.Parties)
        {
            context.CurrentPath = $"Parties[{party.Id}]";
            report.Merge(ValidateParty(party));
        }

        // Validate fleets (War Sails)
        if (save.HasWarSails)
        {
            foreach (var fleet in save.Fleets)
            {
                context.CurrentPath = $"Fleets[{fleet.Id}]";
                report.Merge(ValidateFleet(fleet));
            }
        }

        // Validate entity references
        ValidateEntityReferences(save, report);

        _logger?.LogDebug("Validation complete: {Errors} errors, {Warnings} warnings",
            report.Errors.Count, report.Warnings.Count);

        return report;
    }

    public ValidationReport ValidateHero(HeroData hero)
    {
        var report = new ValidationReport();

        // Attribute validation
        foreach (AttributeType attr in Enum.GetValues<AttributeType>())
        {
            var value = hero.Attributes[attr];
            if (value < 0)
                report.AddError("HERO_ATTR_001", $"Attribute {attr} cannot be negative: {value}", $"Hero.{hero.Name}.Attributes.{attr}");
            if (value > 10 && _mode == ValidationMode.Strict)
                report.AddWarning("HERO_ATTR_002", $"Attribute {attr} exceeds normal maximum (10): {value}", $"Hero.{hero.Name}.Attributes.{attr}");
        }

        // Skill validation
        foreach (SkillType skill in Enum.GetValues<SkillType>())
        {
            var value = hero.Skills[skill];
            if (value < 0)
                report.AddError("HERO_SKILL_001", $"Skill {skill} cannot be negative: {value}", $"Hero.{hero.Name}.Skills.{skill}");
            if (value > 300)
                report.AddError("HERO_SKILL_002", $"Skill {skill} exceeds maximum (300): {value}", $"Hero.{hero.Name}.Skills.{skill}");
        }

        // Level validation
        if (hero.Level < 1)
            report.AddError("HERO_LEVEL_001", "Hero level cannot be less than 1", $"Hero.{hero.Name}.Level");
        if (hero.Level > 62)
            report.AddWarning("HERO_LEVEL_002", $"Hero level exceeds normal maximum (62): {hero.Level}", $"Hero.{hero.Name}.Level");

        // Gold validation
        if (hero.Gold < 0)
            report.AddError("HERO_GOLD_001", "Gold cannot be negative", $"Hero.{hero.Name}.Gold");

        // Age validation
        if (hero.Age < 18 && hero.IsAlive)
            report.AddWarning("HERO_AGE_001", $"Hero age is unusually low: {hero.Age}", $"Hero.{hero.Name}.Age");
        if (hero.Age > 100)
            report.AddWarning("HERO_AGE_002", $"Hero age exceeds normal maximum: {hero.Age}", $"Hero.{hero.Name}.Age");

        // Perk validation
        foreach (var perkId in hero.UnlockedPerks)
        {
            if (!PerkDatabase.IsValidPerk(perkId))
                report.AddWarning("HERO_PERK_001", $"Unknown perk ID: {perkId}", $"Hero.{hero.Name}.Perks");
        }

        return report;
    }

    public ValidationReport ValidateParty(PartyData party)
    {
        var report = new ValidationReport();

        // Troop count validation
        if (party.TotalTroopCount > party.PartySizeLimit * 2)
            report.AddWarning("PARTY_SIZE_001", $"Party size ({party.TotalTroopCount}) greatly exceeds limit ({party.PartySizeLimit})", $"Party.{party.Name}");

        // Gold validation
        if (party.Gold < 0)
            report.AddError("PARTY_GOLD_001", "Party gold cannot be negative", $"Party.{party.Name}.Gold");

        // Troop validation
        foreach (var troop in party.Troops)
        {
            if (troop.Count < 0)
                report.AddError("PARTY_TROOP_001", $"Troop count cannot be negative: {troop.TroopName}", $"Party.{party.Name}.Troops");
            if (troop.WoundedCount > troop.Count)
                report.AddError("PARTY_TROOP_002", $"Wounded count exceeds total: {troop.TroopName}", $"Party.{party.Name}.Troops");
        }

        // Food validation
        if (party.Food < 0)
            report.AddError("PARTY_FOOD_001", "Food cannot be negative", $"Party.{party.Name}.Food");

        // Morale validation
        if (party.Morale < 0 || party.Morale > 100)
            report.AddWarning("PARTY_MORALE_001", $"Morale out of range (0-100): {party.Morale}", $"Party.{party.Name}.Morale");

        return report;
    }

    public ValidationReport ValidateFleet(FleetData fleet)
    {
        var report = new ValidationReport();

        // Ship count validation
        if (fleet.Ships.Count == 0)
            report.AddWarning("FLEET_SHIPS_001", "Fleet has no ships", $"Fleet.{fleet.Name}");

        // Flagship validation
        if (fleet.Flagship != null && !fleet.Ships.Contains(fleet.Flagship))
            report.AddError("FLEET_FLAG_001", "Flagship is not in fleet's ship list", $"Fleet.{fleet.Name}");

        // Validate each ship
        foreach (var ship in fleet.Ships)
        {
            report.Merge(ValidateShip(ship, fleet));
        }

        // Morale validation
        if (fleet.Morale < 0 || fleet.Morale > 100)
            report.AddWarning("FLEET_MORALE_001", $"Fleet morale out of range (0-100): {fleet.Morale}", $"Fleet.{fleet.Name}");

        return report;
    }

    private ValidationReport ValidateShip(ShipData ship, FleetData fleet)
    {
        var report = new ValidationReport();

        // Hull validation
        if (ship.CurrentHullPoints < 0)
            report.AddError("SHIP_HULL_001", "Hull points cannot be negative", $"Fleet.{fleet.Name}.Ship.{ship.Name}");
        if (ship.CurrentHullPoints > ship.MaxHullPoints)
            report.AddError("SHIP_HULL_002", $"Hull points ({ship.CurrentHullPoints}) exceed maximum ({ship.MaxHullPoints})", $"Fleet.{fleet.Name}.Ship.{ship.Name}");

        // Crew validation
        if (ship.CrewCount < 0)
            report.AddError("SHIP_CREW_001", "Crew count cannot be negative", $"Fleet.{fleet.Name}.Ship.{ship.Name}");
        if (ship.CrewCount > ship.CrewCapacity)
            report.AddError("SHIP_CREW_002", $"Crew count ({ship.CrewCount}) exceeds capacity ({ship.CrewCapacity})", $"Fleet.{fleet.Name}.Ship.{ship.Name}");

        // Cargo validation
        if (ship.CurrentCargoWeight > ship.CargoCapacity)
            report.AddError("SHIP_CARGO_001", $"Cargo weight ({ship.CurrentCargoWeight}) exceeds capacity ({ship.CargoCapacity})", $"Fleet.{fleet.Name}.Ship.{ship.Name}");

        // Morale validation
        if (ship.CrewMorale < 0 || ship.CrewMorale > 100)
            report.AddWarning("SHIP_MORALE_001", $"Crew morale out of range (0-100): {ship.CrewMorale}", $"Fleet.{fleet.Name}.Ship.{ship.Name}");

        return report;
    }

    private void ValidateEntityReferences(SaveFile save, ValidationReport report)
    {
        var heroIds = save.Heroes.Select(h => h.Id).ToHashSet();
        var partyIds = save.Parties.Select(p => p.Id).ToHashSet();
        var clanIds = save.Clans.Select(c => c.Id).ToHashSet();

        // Validate hero references
        foreach (var hero in save.Heroes)
        {
            if (hero.ClanId.HasValue && !hero.ClanId.Value.IsEmpty && !clanIds.Contains(hero.ClanId.Value))
                report.AddWarning("REF_001", $"Hero {hero.Name} references non-existent clan: {hero.ClanId}", $"Hero.{hero.Name}.ClanId");

            if (hero.PartyId.HasValue && !hero.PartyId.Value.IsEmpty && !partyIds.Contains(hero.PartyId.Value))
                report.AddWarning("REF_002", $"Hero {hero.Name} references non-existent party: {hero.PartyId}", $"Hero.{hero.Name}.PartyId");
        }

        // Validate party references
        foreach (var party in save.Parties)
        {
            if (party.LeaderId.HasValue && !party.LeaderId.Value.IsEmpty && !heroIds.Contains(party.LeaderId.Value))
                report.AddWarning("REF_003", $"Party {party.Name} references non-existent leader: {party.LeaderId}", $"Party.{party.Name}.LeaderId");
        }
    }

    private void RegisterDefaultValidators()
    {
        // Default validators are built into the service methods
    }
}

/// <summary>
/// Perk database for validation.
/// </summary>
public static class PerkDatabase
{
    private static readonly HashSet<string> KnownPerks = new(StringComparer.OrdinalIgnoreCase)
    {
        // One-Handed perks
        "swift_strike", "cavalry", "basher", "deflect", "to_be_blunt",
        // Two-Handed perks  
        "strong_grip", "on_the_edge", "head_basher", "show_of_strength",
        // Add more known perks...
    };

    public static bool IsValidPerk(string perkId) => KnownPerks.Contains(perkId) || perkId.StartsWith("mod_");
}
