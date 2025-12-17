# Changelog

All notable changes to the Save Editor will be documented in this file.

## [Unreleased]

### Added
- Initial Save Editor implementation
- Character editing (level, gold, attributes, skills, perks)
- Party editing (troops, prisoners, resources)
- Fleet editing (War Sails support)
- Backup system with compression
- Validation framework
- Integration tests

### Known Issues
- Some Clone methods not implemented on model classes
- ShipUpgrade enum missing some values
- Parameter naming inconsistencies in backup service

## [0.1.0] - 2024-XX-XX

### Added
- Core save file parsing (TWSV format)
- ZLIB compression/decompression
- JSON header parsing
- Save file discovery
- Basic validation

### Changed
- N/A (initial release)

### Deprecated
- N/A

### Removed
- N/A

### Fixed
- N/A

### Security
- SHA256 checksum verification for backups
- Atomic file writes to prevent corruption

---

## Version History

### Planned Features

#### v0.2.0
- [ ] Settlement editing
- [ ] Kingdom editing
- [ ] Improved validation messages
- [ ] Performance optimizations

#### v0.3.0
- [ ] Undo/redo support
- [ ] Edit templates
- [ ] Batch operations
- [ ] Export/import functionality

#### v1.0.0
- [ ] Full API stability
- [ ] Complete documentation
- [ ] Comprehensive test coverage
- [ ] Production-ready release
