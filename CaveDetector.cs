using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Locations;

namespace CaveContentDisplay
{
    /// <summary>
    /// Determines whether a game location qualifies as a "cave/underground" area
    /// by using a layered detection strategy rather than a single hardcoded type-check.
    ///
    /// Detection layers (applied in order, first match wins):
    ///   1. Blacklist  — instantly rejects well-known overworld locations.
    ///   2. Native SDV types — MineShaft, VolcanoDungeon, etc.
    ///   3. Location name keywords — "mine", "cave", "dungeon", "sewer", …
    ///   4. Map properties — IsMine, IsDungeon, isDarkOut tags.
    ///   5. Darkness/monsters heuristic — has darkness flag OR has monsters AND
    ///      resource clumps (strong signal for modded combat caves).
    /// </summary>
    public static class CaveDetector
    {
        // ── Layer 1 — Overworld Blacklist ─────────────────────────────────────────
        // Exact location names that must never be flagged, no matter what other
        // layers detect (e.g. Beach can have isThereDarkness at night).
        private static readonly HashSet<string> _blacklist = new(StringComparer.OrdinalIgnoreCase)
        {
            "Farm", "FarmHouse", "FarmCave",
            "Town", "Beach", "Forest", "BusStop",
            "Mountain", "Railroad", "Desert",
            "Hospital", "Saloon", "Shop", "SeedShop",
            "Blacksmith", "FishShop", "JodiFarm",
            "WizardHouse", "WizardHouseBasement",
            "ScienceHouse", "SebastianRoom",
            "AnimalShop", "LeahHouse",
            "Sewer",                 // SDV Sewer is overworld-ish; user didn't request it
            "Submarine", "MermaidHouse",
            "Bathhouse_Pool", "BathHouse_Pool",
            "SlimeHutch",
            "Greenhouse",
            "IslandSouth", "IslandNorth", "IslandEast", "IslandWest",
            "IslandFarmHouse", "IslandFarm", "IslandFieldOffice",
            "IslandShrine", "IslandHut", "IslandSouthEast",
            "Trailer", "Trailer_Big",
            "SandyHouse", "AdventureGuild",
            "ManorHouse", "JoshHouse", "HaleyHouse",
            "ElliottHouse", "HarveyRoom", "SamHouse",
            "Tent",
        };

        // ── Layer 3 — Name Keywords ───────────────────────────────────────────────
        // Substrings matched case-insensitively against the location's Name.
        private static readonly string[] _caveKeywords =
        {
            "mine", "cave", "dungeon", "skull",
            "volcano", "quarry", "sewer", "slime",
            "underground", "cavern", "grotto", "lair",
            "cellar", "crypt", "tunnel",
        };

        // ── Layer 3 — Name Whitelist (explicit known cave names) ─────────────────
        // Full location names that should always be treated as caves.
        private static readonly HashSet<string> _caveWhitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            "UndergroundMine",
            "SkullCave",
            "VolcanoDungeon",
            "QiNutRoom",        // Qi's Walnut Room — underground chamber
            "MutantBugLair",
            "WitchSwamp",       // accessible through underground path
            "Bugland",
        };

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if <paramref name="location"/> should be treated as a
        /// cave/underground area by the HUD and scan system.
        /// </summary>
        public static bool IsCaveLocation(GameLocation? location)
        {
            if (location == null) return false;

            string name = location.Name ?? "";

            // Layer 1: Blacklist — hard reject
            if (_blacklist.Contains(name)) return false;

            // Layer 2a: Native SDV types (most reliable)
            if (location is MineShaft)      return true;
            if (location is VolcanoDungeon) return true;

            // Layer 2b: Name whitelist (known cave names not covered by types)
            if (_caveWhitelist.Contains(name)) return true;

            // Layer 3: Name keyword scan (handles modded caves by name convention)
            if (ContainsCaveKeyword(name)) return true;

            // Layer 4: Map properties
            if (HasCaveMapProperty(location)) return true;

            // Layer 5: Content heuristic — darkness flag + either monsters OR
            // resource clumps (avoids false positives on empty modded locations)
            if (HasCaveContentHeuristic(location)) return true;

            return false;
        }

        // ── Layer Helpers ─────────────────────────────────────────────────────────

        private static bool ContainsCaveKeyword(string name)
        {
            foreach (string kw in _caveKeywords)
            {
                if (name.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool HasCaveMapProperty(GameLocation location)
        {
            try
            {
                var map = location.map;
                if (map == null) return false;

                if (map.Properties.TryGetValue("IsMine",    out _)) return true;
                if (map.Properties.TryGetValue("IsDungeon", out _)) return true;

                // isDarkOut by itself isn't conclusive (night outside), but
                // combined with no outdoor flag it's a reliable cave signal.
                bool isDark     = map.Properties.ContainsKey("isDarkOut");
                bool isOutdoors = location.IsOutdoors;
                if (isDark && !isOutdoors) return true;
            }
            catch { /* map may be null for unloaded locations */ }

            return false;
        }

        private static bool HasCaveContentHeuristic(GameLocation location)
        {
            // Must not be an outdoor location
            if (location.IsOutdoors) return false;

            bool hasMonsters = false;
            bool hasClumps   = false;

            try
            {
                hasMonsters = location.characters.Any(c => c is StardewValley.Monsters.Monster);
            }
            catch { }

            try
            {
                hasClumps = location.resourceClumps.Count > 0;
            }
            catch { }

            // Require at least monsters OR (clumps AND not a normal building)
            return hasMonsters || hasClumps;
        }
    }
}
