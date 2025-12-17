# Save Editor Features

## Character Editing

### Basic Stats
| Feature | Description | Validation |
|---------|-------------|------------|
| Level | Set character level (1-62) | Recalculates XP |
| Experience | Set XP directly | Updates level |
| Gold | Set character gold | No negative values |
| Health | Set current/max health | Cannot exceed max |

### Attributes
| Attribute | Range | Effect |
|-----------|-------|--------|
| Vigor | 0-10 | Melee combat |
| Control | 0-10 | Ranged combat |
| Endurance | 0-10 | Stamina/HP |
| Cunning | 0-10 | Movement/tactics |
| Social | 0-10 | Trading/leadership |
| Intelligence | 0-10 | Engineering/medicine |

### Skills
- Set skill levels (0-300)
- Set focus points per skill (0-5)
- Automatic skill cap calculation based on attributes

### Perks
- Add unlocked perks
- Remove perks
- View perk tree status
- Validate perk prerequisites

### Traits
- Add/remove character traits
- Modify trait values
- View trait effects

## Party Editing

### Troops
| Feature | Description |
|---------|-------------|
| Add troops | Add specific troop types |
| Remove troops | Remove by type or count |
| Heal wounded | Heal all wounded troops |
| Upgrade troops | Upgrade to next tier |

### Prisoners
- Add prisoners
- Release prisoners
- Convert to troops (with persuasion)

### Party Resources
- Food supplies
- Party gold
- Morale adjustment

### Inventory
- Add items
- Remove items
- Modify item modifiers

## Fleet Editing (War Sails)

### Ships
| Feature | Description |
|---------|-------------|
| Repair | Restore hull to max |
| Rename | Change ship name |
| Add crew | Set crew count |
| Add upgrades | Install ship upgrades |

### Fleets
- Assign admiral
- Set flagship
- Transfer ships between fleets
- Modify fleet position

### Naval Resources
- Cargo management
- Naval supplies
- Ship equipment

## Settlement Editing

### Towns & Castles
- Prosperity level
- Food stocks
- Garrison troops
- Loyalty/Security

### Villages
- Hearth count
- Production bonuses
- Village notables

## Kingdom & Clan

### Clan
- Renown
- Influence
- Gold
- Tier

### Kingdom
- Treasury
- Mercenary contracts
- War/peace status

## Validation System

### Pre-Edit Checks
- Value range validation
- Reference integrity
- Dependency validation

### Post-Edit Verification
- Data consistency
- Save file integrity
- Checksum verification

## Batch Operations

### Mass Edit
```csharp
// Heal all heroes
await editor.BatchEditAsync(save.Heroes, hero => 
    editor.SetHealthAsync(hero, hero.MaxHealth));

// Max all skills for main hero
await editor.BatchEditAsync(save.MainHero.Skills, skill =>
    editor.SetSkillLevelAsync(save.MainHero, skill.Id, 300));
```

### Templates
- Save edit configurations as templates
- Apply templates to multiple saves
- Share templates

## Undo/Redo Support

```csharp
// Make edits
await editor.SetGoldAsync(hero, 100000);
await editor.SetLevelAsync(hero, 50);

// Undo last edit
await editor.UndoAsync();

// Redo
await editor.RedoAsync();

// View history
var history = editor.GetEditHistory();
```

## Export/Import

### Export Formats
- JSON (full data)
- CSV (tabular data)
- XML (structured)

### Import
- Character builds
- Party compositions
- Fleet configurations
