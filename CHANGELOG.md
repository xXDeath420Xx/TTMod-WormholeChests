# Changelog

All notable changes to WormholeChests will be documented in this file.

## [3.1.1] - 2026-01-07

### Fixed
- Minor stability improvements
- Compatibility updates

## [3.1.1] - 2025-01-06

### Changed
- **Repositioned channel UI** - Channel input and Create/Link button now positioned on either side of the storage title area
  - Input box at 28% from left (left of "Storage" title)
  - Create/Link button at 58% from left (right of "Storage" title)
  - Y position at 22% from top to align with storage title area
- Added new config option `Relative Button X Position` for independent button positioning
- UI no longer overlaps with storage contents (fixes GitHub issue #1)

### Fixed
- Channel input positioning on ultrawide (3440x1440) and other resolutions

## [3.1.0] - 2025-01-06

### Added
- Resolution-independent UI positioning using percentage-based coordinates
- Works correctly on all resolutions including ultrawide (3440x1440) and 4K
- New config options for relative positioning

### Changed
- Default positioning updated for better visibility across screen sizes

## [3.0.4] - 2026-01-03

### Fixed
- Included CHANGELOG.md in Thunderstore package for proper changelog display

## [3.0.3] - 2026-01-03

### Changed
- **Reduced debug logging** - Removed verbose LogInfo calls in GetAimedAtChest()
  - Cleaner BepInEx log output
  - Slight performance improvement from reduced string formatting

### Performance
- Removed 8+ debug log calls that ran every time a chest was targeted
- Error and warning logging preserved for troubleshooting

## [3.0.2] - 2026-01-03

### Changed
- Published to Thunderstore with proper packaging and metadata
- Verified compatibility with latest EMU 6.1.3

## [3.0.1] - 2026-01-03

### Changed
- Updated README with proper attribution and links to original author Equinox

## [3.0.0] - 2026-01-02

### Added
- Global unlock check to disable mod when tech not researched
- Comprehensive null checks and exception handling

### Changed
- **API Migration to EMU 6.1.3 nested class structure**

### Fixed
- Various NullReferenceException and crash issues
