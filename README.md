# Wormhole Chests

A Techtonica mod that enables quantum-linked storage containers. Link multiple chests together across any distance to create shared inventories - items placed in one linked chest instantly appear in all others on the same channel.

## Table of Contents

- [Features](#features)
- [How It Works](#how-it-works)
- [Installation](#installation)
- [Usage Guide](#usage-guide)
- [Configuration Options](#configuration-options)
- [Requirements](#requirements)
- [Compatibility](#compatibility)
- [Known Issues](#known-issues)
- [Credits](#credits)
- [License](#license)
- [Links](#links)
- [Changelog](#changelog)

## Features

- **Linked Chest Networks**: Connect unlimited chests together using named channels
- **Shared Inventory**: All chests on the same channel share a single 56-slot inventory
- **Instant Synchronization**: Items added to any linked chest are immediately accessible from all others
- **Named Channels**: Create custom channel names for organized storage systems
- **Tech Tree Integration**: Unlockable through research (requires 2000 Blue Research Cores)
- **Configurable UI**: Adjustable GUI positioning for different screen resolutions
- **Resolution-Aware Positioning**: Automatic UI scaling with percentage-based positioning
- **Persistent Storage**: Wormhole data saves automatically with your world
- **Deconstruction Safety**: Prevents duplicate item drops when deconstructing linked chests

## How It Works

Wormhole Chests uses a channel-based system to link storage containers:

1. **Channels**: Each wormhole network is identified by a unique channel name (e.g., "iron", "ores", "building-mats")
2. **Shared Inventory**: All chests assigned to the same channel access a single shared inventory pool
3. **Inventory Merging**: When linking an existing chest to a channel, its contents merge with the shared inventory
4. **Network Persistence**: Channel assignments and inventories persist across game sessions

### Technical Details

- Each wormhole channel maintains a 56-slot inventory
- Creating or linking a chest costs Blue Research Cores (exponential scaling based on total linked chests)
- The cost formula is: `100 * 1.05^(number of existing linked chests)`
- Wormhole data is stored in `%AppData%/LocalLow/Fire Hose Games/Techtonica/WormholeChests/[WorldName]/`

## Installation

### Using r2modman (Recommended)

1. Install [r2modman](https://thunderstore.io/package/ebkr/r2modman/) if you haven't already
2. Search for "Wormhole Chests" in the mod browser
3. Click "Download" to install the mod and all dependencies automatically

### Manual Installation

1. Install [BepInEx 5.4.21](https://github.com/BepInEx/BepInEx/releases) or later
2. Install the required dependencies (see [Requirements](#requirements))
3. Download the latest `WormholeChests.dll` from the releases page
4. Place the DLL in your `BepInEx/plugins/` folder

## Usage Guide

### Unlocking Wormhole Chests

1. Progress through the tech tree until you reach Tier 2 research
2. Research the "Wormhole Chests" technology (located in the Modded category)
3. This requires 2000 Blue Research Cores (Research Core 480nm)

### Creating a Wormhole Channel

1. Place or approach any standard chest in your factory
2. Open the chest's inventory (interact with it)
3. A channel input box will appear in the UI
4. Enter a unique channel name in the text field
5. Click "Create" to establish a new wormhole channel
6. The chest is now linked and shows "Linked!" confirmation

### Linking Additional Chests

1. Open another chest's inventory
2. Enter an existing channel name in the text field
3. Click "Link" to join the existing channel
4. The chest's current contents will merge into the shared inventory
5. Both chests now access the same inventory

### Managing Linked Chests

- **View Channel**: When opening a linked chest, its channel name appears in the input field
- **Deconstruct**: Safely deconstruct linked chests - items remain in the shared inventory unless it's the last chest on that channel
- **Change Channel**: Currently, chests cannot be reassigned to different channels; deconstruct and create new links as needed

## Configuration Options

Configuration file location: `BepInEx/config/com.equinox.WormholeChests.cfg`

### General Settings

| Option | Default | Description |
|--------|---------|-------------|
| `Free Wormhole Chests` | `false` | Disables the Blue Core cost for creating/linking chests (cheat mode) |

### GUI Layout (Relative Positioning)

| Option | Default | Description |
|--------|---------|-------------|
| `Use Relative Positioning` | `true` | Enables percentage-based UI positioning for multi-resolution support |
| `Relative X Position` | `0.28` | Horizontal position of channel input (0.0=left, 0.5=center, 1.0=right) |
| `Relative Y Position` | `0.22` | Vertical position (0.0=top, 0.5=center, 1.0=bottom) |
| `Relative Button X Position` | `0.58` | Horizontal position of Create/Link button |

### GUI Layout (Fixed Pixel Offsets)

| Option | Default | Description |
|--------|---------|-------------|
| `Channel Box X Offset` | `32` | Pixel offset from screen center (used when relative positioning is disabled) |
| `Channel Box Y Offset` | `-550` | Pixel offset from screen center (ultrawide displays may need adjustment) |
| `Channel Box Width` | `240` | Width of the channel input box in pixels |
| `Create Button X Offset` | `444` | Horizontal offset of Create/Link button from channel box |

## Requirements

### Required Dependencies

| Dependency | Minimum Version | Purpose |
|------------|-----------------|---------|
| [BepInEx](https://github.com/BepInEx/BepInEx) | 5.4.21 | Mod framework |
| [EquinoxsModUtils (EMU)](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/) | 6.1.3 | Core modding utilities |
| [EMUAdditions](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/) | 2.0.0 | Extended mod utilities for custom unlocks |
| [TechtonicaFramework](https://thunderstore.io/c/techtonica/p/CertiFried/TechtonicaFramework/) | Latest | Tech tree integration framework |

### Game Version

- Techtonica (latest version recommended)

## Compatibility

- **Multiplayer**: Not tested - use at your own risk in multiplayer sessions
- **Save Compatibility**: Wormhole data is stored separately from save files; removing the mod will not corrupt saves, but linked chest data will be lost
- **Other Mods**: Generally compatible with other mods; no known conflicts

## Known Issues

- UI positioning may require adjustment for non-standard aspect ratios
- Chests cannot be reassigned to different channels without deconstruction

## Credits

### Original Author

- **Equinox** - Original mod creator
  - GitHub: [CubeSuite/TTMod-WormholeChests](https://github.com/CubeSuite/TTMod-WormholeChests)

### Current Maintainer

- **CertiFried** - Ongoing updates and maintenance
  - API migration to EMU 6.1.3
  - Performance improvements
  - Tech tree integration via TechtonicaFramework

### Development Assistance

- **Claude Code** (Anthropic) - AI-assisted code refactoring, documentation, and debugging

### Special Thanks

- The Techtonica modding community
- Fire Hose Games for creating Techtonica
- The BepInEx team for the modding framework

## License

This mod is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.

You are free to:
- Use this mod for personal and commercial purposes
- Modify the source code
- Distribute copies and modifications

Under the following conditions:
- Source code must be made available when distributing
- Modifications must be released under the same license
- Original copyright and license notices must be preserved

For the full license text, see: https://www.gnu.org/licenses/gpl-3.0.html

## Links

- **Source Code**: [GitHub - CubeSuite/TTMod-WormholeChests](https://github.com/CubeSuite/TTMod-WormholeChests)
- **Thunderstore**: [Wormhole Chests on Thunderstore](https://thunderstore.io/c/techtonica/p/CertiFried/WormholeChests/)
- **Techtonica Discord**: [Techtonica Official Discord](https://discord.gg/techtonica)
- **BepInEx Documentation**: [BepInEx Docs](https://docs.bepinex.dev/)
- **EquinoxsModUtils**: [EMU on Thunderstore](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/)

## Changelog

### [3.1.1] - Current
- Latest stable release
- Full EMU 6.1.3 compatibility

### [3.0.6] - 2025-01-05
- Version bump for bulk update

### [3.0.3] - 2025-01-03
- Reduced debug logging for cleaner output
- Performance improvements

### [3.0.0] - 2025-01-02
- Global unlock check to disable mod when tech not researched
- Comprehensive null checks and exception handling
- API migration to EMU 6.1.3 nested class structure
- Tech tree integration via TechtonicaFramework

---

*For bug reports, feature requests, or contributions, please visit the GitHub repository or join the Techtonica Discord community.*
