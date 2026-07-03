# Metal Detector Game

A cozy first-person treasure-hunting game built in Unity and C#. The player explores a stylized island with a metal detector, follows signal strength to locate buried objects, digs up hidden chests, searches them for loot, and uses the rewards to upgrade their equipment and unlock better search areas.

This is a solo indie project focused on building complete gameplay systems rather than a small prototype: detection, digging, loot, inventory, shops, quests, progression, localization, save data, and early multiplayer support.

## Gameplay

The core loop is simple and satisfying:

1. Explore the island with a metal detector.
2. Hold scan to search the ground and follow the signal.
3. Dig up a marked treasure spot.
4. Search the revealed chest to discover the item.
5. Sell or collect finds, then upgrade gear and unlock new areas.

## Features

- First-person metal detector gameplay with signal strength feedback
- Buried treasure detection, reveal markers, digging, and searchable chests
- Weighted loot tables with common, rare, and epic finds
- Inventory/backpack system with item sizes, values, and selling flow
- Economy and upgrades for detector, shovel, backpack, and search areas
- NPCs for selling, upgrades, and quests
- Day/night cycle with sleeping and daily treasure regeneration
- Save/load support for player progress and world state
- Localization support for English, Polish, Norwegian, German, Spanish, Swedish, and Danish
- Character selection and generated player avatars
- Early multiplayer/co-op systems, including shared sleep readiness

## Technical Highlights

- Built with Unity and C#
- Modular gameplay architecture split across detector, scanner, digging, treasure, inventory, shop, UI, quest, and world systems
- Procedural treasure spawning with rarity-based weighted loot pools
- Runtime UI systems for HUD, inventory, shops, settings, interaction prompts, and reward notifications
- Localized UI text with language switching from the settings menu
- Day/night state management with treasure reset logic
- Co-op synchronization work for shared world events and team sleep state

## Controls

- `WASD` - Move
- `Mouse` - Look around
- `LMB` - Scan with the metal detector
- `E` - Interact, dig, search, talk, or use nearby objects
- `TAB` - Open backpack
- `ESC` - Close menus / pause

## Project Status

The game is currently in active development. The main gameplay loop is playable, with ongoing work on polish, balancing, multiplayer flow, presentation, and content expansion.

## Tech Stack

- Unity
- C#
- TextMeshPro
- UMA avatar system
- Steam-ready multiplayer groundwork
- Built with help from OpenAI Codex

## Portfolio Summary

Developed a first-person Unity treasure-hunting game featuring detector-based exploration, procedural loot spawning, digging and chest interactions, inventory and economy systems, gear upgrades, quests, localization, save/load support, and early co-op synchronization.

## Credits

Created by Kamil with development assistance from OpenAI Codex.

## License

This repository is intended for portfolio review. Third-party assets remain under their original licenses.
