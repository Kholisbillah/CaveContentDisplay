<div align="center">

# ⛏️ Cave Contents HUD ⛏️

**A real-time, highly customizable HUD mod for Stardew Valley 1.6**
*Know exactly what's on every mine floor before you swing your pickaxe.*

[![SMAPI](https://img.shields.io/badge/SMAPI-4.0.0+-brightgreen?style=for-the-badge&logo=data:image/png;base64,iVBORw0KGgo=)](https://smapi.io)
[![Stardew Valley](https://img.shields.io/badge/Stardew%20Valley-1.6+-orange?style=for-the-badge)](https://www.stardewvalley.net/)
[![Nexus Mods](https://img.shields.io/badge/Nexus%20Mods-Download-orange?style=for-the-badge)](https://www.nexusmods.com/stardewvalley)
[![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)](LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/Kholisbillah/CaveContentDisplay?style=for-the-badge)](https://github.com/Kholisbillah/CaveContentDisplay/stargazers)

---

![Cave Contents HUD Banner](https://i.ibb.co/nqSxWq1f/CAVE-CONTENT-HUD.jpg)

</div>

---

## 📖 Table of Contents

- [About](#-about)
- [Features](#-features)
- [Screenshots](#-screenshots)
- [Installation](#️-installation)
- [Configuration](#️-configuration)
- [Item Filter System](#-item-filter-system)
- [Supported Locations](#️-supported-locations)
- [Compatibility](#-compatibility)
- [FAQ](#-faq)
- [Changelog](#-changelog)
- [Contributing](#-contributing)
- [Credits](#-credits)

---

## 🪨 About

Ever descended into the mines only to realize you wasted a perfectly good day on a floor with nothing valuable?

**Cave Contents HUD** solves that. The moment you step onto any mine floor, a sleek overlay appears on your screen showing you **exactly** what's there — every ore node, monster, barrel, and rare gem — in real time.

Whether you're hunting for an elusive **Prismatic Shard** in Skull Cavern, farming **Iridium Ore**, or just trying to clear a floor efficiently, this mod gives you the information you need to make smart decisions instantly.

---

## ✨ Features

### 🔴 Real-Time Floor Scanning

The HUD updates **instantly** as you mine, fight, and explore. Break a rock — it's gone from the list. Kill a monster — removed immediately. Powered by an event-driven dirty flag system for maximum efficiency with zero lag.

### 🖼️ Native Item Icons

Every item in the HUD displays its **original Stardew Valley sprite** directly from the game's assets. No external image files, no blurry icons — just pixel-perfect game sprites you already know.

### 🔍 Smart Item Filter System

The crown jewel of the mod. Open the **Filter Picker** with `R` and choose exactly which items you want to track:

- Browse items by category with a tabbed interface
- Items you've found before show **"Found Xx"** with a green badge
- Items from the master list you haven't seen yet show **"Not seen yet"**
- Items from modded caves show a **[Modded]** tag automatically
- Persistent cache remembers what you've found across sessions

### 🗂️ Smart Stone Grouping

All stone variants (Stone, Snowy Stone, Lava Stone, etc.) are grouped together as a single **"Stone"** entry. Filter for Stone once — catch them all.

### 📊 Sorting Options

- **Category** — Grouped by type (default & recommended)
- **Highest Quantity** — Most abundant items at the top
- **Lowest Quantity** — Rarest items at the top
- **Alphabetical** — For the organized miners

### ⚙️ Fully Configurable via GMCM

Every aspect of the HUD is adjustable in-game without restarting:

- Toggle item icons on/off
- Choose refresh mode (real-time or interval)
- Adjust HUD position (X & Y)
- Scale the entire UI (50% to 200%)
- Configure keybinds

### 🎮 Toggle Anywhere

Press `H` to instantly show/hide the HUD overlay whenever you need a clean screen.

---

## 🛠️ Installation

### Requirements

| Dependency                                                                | Version | Required?                        |
| ------------------------------------------------------------------------- | ------- | -------------------------------- |
| [SMAPI](https://smapi.io/)                                                   | 4.0.0+  | ✅ Required                      |
| [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) | Latest  | ⚡ Optional (for in-game config) |

### Steps

**1.** Install [SMAPI](https://smapi.io/) if you haven't already.

**2.** *(Optional)* Install [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) for in-game configuration.

**3.** Download the latest `CaveContentsHUD.zip` from [Nexus Mods](https://www.nexusmods.com/stardewvalley) or [GitHub Releases](https://github.com/Kholisbillah/CaveContentDisplay/releases).

**4.** Extract the zip file and place the `CaveContentDisplay` folder into your mods directory:

```
Stardew Valley/
└── Mods/
    └── CaveContentDisplay/   ← place here
        ├── CaveContentDisplay.dll
        └── manifest.json
```

**5.** Launch the game via `StardewModdingAPI.exe` and enjoy!

---

## ⚙️ Configuration

All settings are available via **Generic Mod Config Menu** in-game, or by editing `config.json` manually.

| Setting                 | Default          | Description                                                                         |
| ----------------------- | ---------------- | ----------------------------------------------------------------------------------- |
| `ShowIcons`           | `true`         | Show/hide item icons next to names                                                  |
| `RefreshMode`         | `RealTime`     | How often HUD updates (`RealTime`, `Sec3`, `Sec5`, `Sec8`, `Sec10`)       |
| `SortMode`            | `QuantityDesc` | Item sort order (`Category`, `QuantityDesc`, `QuantityAsc`, `Alphabetical`) |
| `HudX`                | `20`           | HUD horizontal position                                                             |
| `HudY`                | `100`          | HUD vertical position                                                               |
| `GuiScale`            | `1.3`          | HUD size multiplier (0.5 – 2.0)                                                    |
| `FilteredItems`       | `[]`           | Items to track (empty = show all)                                                   |
| `OpenFilterPickerKey` | `R`            | Keybind to open filter picker                                                       |
| `ToggleHudKey`        | `H`            | Keybind to toggle HUD visibility                                                    |

> 💡 **Tip:** If you use UI Info Suite 2 or other mods that occupy the top-left corner, adjust `HudX` and `HudY` to move the overlay to a free area of your screen.

---

## 🔍 Item Filter System

The filter system is designed for targeted mining runs — hunt specific items without the noise.

### How It Works

**1.** Press `R` anywhere in a cave to open the **Item Filter** menu.

**2.** Browse items by category using the tabs at the top:

| Tab            | Contents                                      |
| -------------- | --------------------------------------------- |
| ⛏️ Ores      | Stone variants, ore nodes, coal, geodes       |
| 💎 Gems        | All gems and geode minerals                   |
| 👾 Monsters    | Every cave enemy including dangerous variants |
| 📦 Objects     | Barrels, boulders, chests, ladders            |
| 🌿 Forageables | Mushrooms, cave plants, monster drops         |

**3.** Check the items you want to track.

**4.** Click **Save & Close** — your HUD now only shows those items.

**5.** To show everything again, simply clear all filters.

### Item Badges

| Badge            | Color    | Meaning                              |
| ---------------- | -------- | ------------------------------------ |
| `Found 23×`   | 🟢 Green | Found on current floor this session  |
| `Not seen yet` | ⚫ Gray  | In master list but never encountered |
| `[Modded]`     | 🔵 Blue  | From a modded cave, auto-detected    |

### Persistent Cache

The mod remembers every item you've ever encountered across all sessions, stored in `data/scanned-items.json`. Items from modded caves are automatically added to the cache and appear in the filter picker with a `[Modded]` badge.

---

## 🗺️ Supported Locations

Cave Contents HUD works in **all underground cave locations**, including modded ones:

| Location                        | Supported        |
| ------------------------------- | ---------------- |
| The Mines (Floor 1–120)        | ✅               |
| Skull Cavern (Floor 121+)       | ✅               |
| Volcano Dungeon (Ginger Island) | ✅               |
| Quarry Mine                     | ✅               |
| Sewers / Mutant Bug Lair        | ✅               |
| Modded caves (Cave Extra, etc.) | ✅ Auto-detected |

The mod uses a **multi-layer detection system** to identify cave locations — if it has monsters, mineable objects, or resource clumps, it will be detected automatically.

---

## 🔗 Compatibility

| Mod                     | Status        | Notes                            |
| ----------------------- | ------------- | -------------------------------- |
| UI Info Suite 2         | ✅ Compatible | Adjust HUD position if needed    |
| Generic Mod Config Menu | ✅ Supported  | Optional, enables in-game config |
| Cave Extra              | ✅ Compatible | Modded items auto-detected       |
| CJB Cheats Menu         | ✅ Compatible | No known conflicts               |
| Stardew Valley Expanded | ✅ Compatible | New cave locations supported     |

> ⚠️ If you encounter a conflict with another mod, please [open an issue](https://github.com/Kholisbillah/CaveContentDisplay/issues).

---

## ❓ FAQ

**Does this work in multiplayer?**
Yes! The HUD reads local client data, so it accurately reflects what's on your screen regardless of who hosts.

**Why does the HUD say "No objects found" on some floors?**
Some floors generate with very few or no objects — that's just how Stardew Valley works. The HUD is working correctly.

**Can I use this with modded ores or items?**
Absolutely. Any item that appears in a cave will be scanned and added to your persistent cache. It will show up in the filter picker with a `[Modded]` badge.

**Will this affect my game performance?**
No. The mod uses an event-driven system (dirty flags) instead of polling every tick. It only re-scans when something actually changes on the floor.

**How do I reset the item cache?**
Open GMCM → CaveContentDisplay → click **"Reset Item Cache"**.

---

## 📋 Changelog

### v1.1.0 — Filter System Overhaul

- ✨ Added **Item Filter Picker** — browse and select items by category
- ✨ Added **persistent item cache** — remembers all items ever found
- ✨ Added **modded item auto-detection** with `[Modded]` badge
- ✨ Added **master item database** — filter items before finding them
- ✨ Added **universal cave detection** — supports all underground locations
- ✨ Stone variants now grouped as single "Stone" entry
- 🐛 Fixed keyboard input triggering chat while typing in search bar
- 🐛 Fixed dropped items being detected as floor objects
- 🐛 Fixed Found count showing data from previous floors
- 🐛 Fixed Volcano Dungeon not being detected

### v1.0.0 — Initial Release

- ✨ Real-time HUD showing floor contents
- ✨ Native item icons from game sprites
- ✨ GMCM integration with full configuration
- ✨ Toggle HUD with `H` key
- ✨ Sorting options and GUI scale

---

## 🤝 Contributing

Contributions are welcome! If you'd like to improve the mod:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Commit your changes (`git commit -m 'Add some feature'`)
4. Push to the branch (`git push origin feature/your-feature`)
5. Open a Pull Request

### Reporting Bugs

Please [open an issue](https://github.com/Kholisbillah/CaveContentDisplay/issues) with:

- Your SMAPI log (`%appdata%/StardewValley/ErrorLogs/`)
- Steps to reproduce
- Expected vs actual behavior

---

## 💝 Credits

<div align="center">

Made with ❤️ for the Stardew Valley community

**Author:** Kholisbillah
**Special Thanks:** RafiaBee on NexusMods — for the item filter feature suggestion

*This mod uses only original Stardew Valley assets and does not include any third-party graphics.*

---

⭐ If you enjoy the mod, consider leaving an endorsement on [Nexus Mods](https://www.nexusmods.com/stardewvalley)!

</div>
