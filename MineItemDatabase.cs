using System.Collections.Generic;
using System.Linq;

namespace CaveContentDisplay
{
    /// <summary>
    /// Describes a single item in the mine item master database.
    /// </summary>
    public class MineItemData
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        /// <summary>Qualified item ID for icon lookup, e.g. "(O)80". Null for monsters/clumps without item IDs.</summary>
        public string? QualifiedItemId { get; set; }
        /// <summary>True if this is a ResourceClump (2x2 object), which requires separate scan logic.</summary>
        public bool IsResourceClump { get; set; }
        /// <summary>parentSheetIndex value for ResourceClump identification. Only relevant when IsResourceClump is true.</summary>
        public int? ResourceClumpId { get; set; }
    }

    /// <summary>
    /// Category string constants for the 5 filter tabs.
    /// </summary>
    public static class MineCategory
    {
        public const string Ores       = "Ores & Minerals";
        public const string Gems       = "Gems & Minerals";
        public const string Monsters   = "Monsters";
        public const string Objects    = "Objects & Containers";
        public const string Forageables = "Forageables & Drops";
    }

    /// <summary>
    /// Static master list of all items, monsters, and resource clumps
    /// that can appear in The Mines, Skull Cavern, Quarry Mine, and Volcano Dungeon.
    /// ResourceClumps (2x2 objects) have IsResourceClump = true and are handled
    /// with separate scan logic in ModEntry.ScanCurrentFloor().
    /// </summary>
    public static class MineItemDatabase
    {
        public static readonly IReadOnlyList<MineItemData> AllItems;
        public static readonly IReadOnlyDictionary<string, IReadOnlyList<MineItemData>> ByCategory;

        static MineItemDatabase()
        {
            var list = new List<MineItemData>
            {
                // ════════════════════════════════════════════════════════════════════
                // CATEGORY 1 — ORES & MINERALS
                // Includes stone types, ore drops, ore nodes (mining rocks), and geodes
                // ════════════════════════════════════════════════════════════════════

                new() { Name = "Stone",            Category = MineCategory.Ores, QualifiedItemId = "(O)390" },
                new() { Name = "Coal",              Category = MineCategory.Ores, QualifiedItemId = "(O)382" },
                new() { Name = "Copper Ore",        Category = MineCategory.Ores, QualifiedItemId = "(O)378" },
                new() { Name = "Iron Ore",          Category = MineCategory.Ores, QualifiedItemId = "(O)380" },
                new() { Name = "Gold Ore",          Category = MineCategory.Ores, QualifiedItemId = "(O)384" },
                new() { Name = "Iridium Ore",       Category = MineCategory.Ores, QualifiedItemId = "(O)386" },
                new() { Name = "Radioactive Ore",   Category = MineCategory.Ores, QualifiedItemId = "(O)909" },
                new() { Name = "Quartz",            Category = MineCategory.Ores, QualifiedItemId = "(O)80"  },
                new() { Name = "Earth Crystal",     Category = MineCategory.Ores, QualifiedItemId = "(O)86"  },
                new() { Name = "Frozen Tear",       Category = MineCategory.Ores, QualifiedItemId = "(O)84"  },
                new() { Name = "Fire Quartz",       Category = MineCategory.Ores, QualifiedItemId = "(O)82"  },
                new() { Name = "Refined Quartz",    Category = MineCategory.Ores, QualifiedItemId = "(O)338" },

                // Ore stones — single-tile rocks that yield ore when mined
                new() { Name = "Copper Stone",      Category = MineCategory.Ores, QualifiedItemId = "(O)751" },
                new() { Name = "Iron Stone",        Category = MineCategory.Ores, QualifiedItemId = "(O)290" },
                new() { Name = "Gold Stone",        Category = MineCategory.Ores, QualifiedItemId = "(O)764" },
                new() { Name = "Iridium Stone",     Category = MineCategory.Ores, QualifiedItemId = "(O)765" },
                new() { Name = "Radioactive Stone", Category = MineCategory.Ores, QualifiedItemId = "(O)95"  },
                new() { Name = "Diamond Stone",     Category = MineCategory.Ores, QualifiedItemId = "(O)2"   },
                new() { Name = "Mystic Stone",      Category = MineCategory.Ores, QualifiedItemId = "(O)46"  },
                new() { Name = "Fossil Stone",      Category = MineCategory.Ores, QualifiedItemId = "(O)816" },

                // Gem/Mineral Nodes (ore rocks that drop gems/geode minerals)
                new() { Name = "Copper Node",       Category = MineCategory.Ores, QualifiedItemId = "(O)751" },
                new() { Name = "Iron Node",         Category = MineCategory.Ores, QualifiedItemId = "(O)290" },
                new() { Name = "Gold Node",         Category = MineCategory.Ores, QualifiedItemId = "(O)764" },
                new() { Name = "Iridium Node",      Category = MineCategory.Ores, QualifiedItemId = "(O)765" },
                new() { Name = "Radioactive Node",  Category = MineCategory.Ores, QualifiedItemId = "(O)95"  },
                new() { Name = "Coal Node",         Category = MineCategory.Ores, QualifiedItemId = "(O)343" },
                new() { Name = "Amethyst Node",     Category = MineCategory.Ores, QualifiedItemId = "(O)8"   },
                new() { Name = "Aquamarine Node",   Category = MineCategory.Ores, QualifiedItemId = "(O)14"  },
                new() { Name = "Diamond Node",      Category = MineCategory.Ores, QualifiedItemId = "(O)2"   },
                new() { Name = "Emerald Node",      Category = MineCategory.Ores, QualifiedItemId = "(O)10"  },
                new() { Name = "Jade Node",         Category = MineCategory.Ores, QualifiedItemId = "(O)12"  },
                new() { Name = "Ruby Node",         Category = MineCategory.Ores, QualifiedItemId = "(O)4"   },
                new() { Name = "Topaz Node",        Category = MineCategory.Ores, QualifiedItemId = "(O)6"   },
                new() { Name = "Gem Node",          Category = MineCategory.Ores, QualifiedItemId = "(O)44"  },
                new() { Name = "Geode Node",        Category = MineCategory.Ores, QualifiedItemId = "(O)75"  },
                new() { Name = "Frozen Geode Node", Category = MineCategory.Ores, QualifiedItemId = "(O)76"  },
                new() { Name = "Magma Geode Node",  Category = MineCategory.Ores, QualifiedItemId = "(O)77"  },
                new() { Name = "Bone Node",         Category = MineCategory.Ores, QualifiedItemId = "(O)816" },
                new() { Name = "Cinder Shard Node", Category = MineCategory.Ores, QualifiedItemId = "(O)843" },

                // Geode items (dropped as objects on floor)
                new() { Name = "Geode",             Category = MineCategory.Ores, QualifiedItemId = "(O)535" },
                new() { Name = "Frozen Geode",      Category = MineCategory.Ores, QualifiedItemId = "(O)536" },
                new() { Name = "Magma Geode",       Category = MineCategory.Ores, QualifiedItemId = "(O)537" },
                new() { Name = "Omni Geode",        Category = MineCategory.Ores, QualifiedItemId = "(O)749" },

                // ════════════════════════════════════════════════════════════════════
                // CATEGORY 2 — GEMS & GEODE MINERALS
                // Gems (direct drops) + all geode-cracked minerals
                // ════════════════════════════════════════════════════════════════════

                // Gemstones
                new() { Name = "Amethyst",          Category = MineCategory.Gems, QualifiedItemId = "(O)66"  },
                new() { Name = "Aquamarine",        Category = MineCategory.Gems, QualifiedItemId = "(O)62"  },
                new() { Name = "Diamond",           Category = MineCategory.Gems, QualifiedItemId = "(O)72"  },
                new() { Name = "Emerald",           Category = MineCategory.Gems, QualifiedItemId = "(O)60"  },
                new() { Name = "Jade",              Category = MineCategory.Gems, QualifiedItemId = "(O)70"  },
                new() { Name = "Ruby",              Category = MineCategory.Gems, QualifiedItemId = "(O)64"  },
                new() { Name = "Topaz",             Category = MineCategory.Gems, QualifiedItemId = "(O)68"  },
                new() { Name = "Prismatic Shard",   Category = MineCategory.Gems, QualifiedItemId = "(O)74"  },

                // Geode minerals
                new() { Name = "Aerinite",          Category = MineCategory.Gems, QualifiedItemId = "(O)537" },
                new() { Name = "Alamite",           Category = MineCategory.Gems, QualifiedItemId = "(O)553" },
                new() { Name = "Baryte",            Category = MineCategory.Gems, QualifiedItemId = "(O)561" },
                new() { Name = "Basalt",            Category = MineCategory.Gems, QualifiedItemId = "(O)557" },
                new() { Name = "Calcite",           Category = MineCategory.Gems, QualifiedItemId = "(O)552" },
                new() { Name = "Celestine",         Category = MineCategory.Gems, QualifiedItemId = "(O)571" },
                new() { Name = "Dolomite",          Category = MineCategory.Gems, QualifiedItemId = "(O)570" },
                new() { Name = "Esperite",          Category = MineCategory.Gems, QualifiedItemId = "(O)562" },
                new() { Name = "Fluorapatite",      Category = MineCategory.Gems, QualifiedItemId = "(O)567" },
                new() { Name = "Fluorite",          Category = MineCategory.Gems, QualifiedItemId = "(O)581" },
                new() { Name = "Geminite",          Category = MineCategory.Gems, QualifiedItemId = "(O)556" },
                new() { Name = "Ghost Crystal",     Category = MineCategory.Gems, QualifiedItemId = "(O)565" },
                new() { Name = "Helvite",           Category = MineCategory.Gems, QualifiedItemId = "(O)575" },
                new() { Name = "Jamborite",         Category = MineCategory.Gems, QualifiedItemId = "(O)560" },
                new() { Name = "Jagoite",           Category = MineCategory.Gems, QualifiedItemId = "(O)564" },
                new() { Name = "Kyanite",           Category = MineCategory.Gems, QualifiedItemId = "(O)580" },
                new() { Name = "Lemon Stone",       Category = MineCategory.Gems, QualifiedItemId = "(O)579" },
                new() { Name = "Lunarite",          Category = MineCategory.Gems, QualifiedItemId = "(O)558" },
                new() { Name = "Malachite",         Category = MineCategory.Gems, QualifiedItemId = "(O)554" },
                new() { Name = "Nekoite",           Category = MineCategory.Gems, QualifiedItemId = "(O)551" },
                new() { Name = "Neptunite",         Category = MineCategory.Gems, QualifiedItemId = "(O)569" },
                new() { Name = "Ocean Stone",       Category = MineCategory.Gems, QualifiedItemId = "(O)576" },
                new() { Name = "Orpiment",          Category = MineCategory.Gems, QualifiedItemId = "(O)555" },
                new() { Name = "Petrified Slime",   Category = MineCategory.Gems, QualifiedItemId = "(O)560" },
                new() { Name = "Pyrite",            Category = MineCategory.Gems, QualifiedItemId = "(O)572" },
                new() { Name = "Slate",             Category = MineCategory.Gems, QualifiedItemId = "(O)559" },
                new() { Name = "Soapstone",         Category = MineCategory.Gems, QualifiedItemId = "(O)574" },
                new() { Name = "Star Shards",       Category = MineCategory.Gems, QualifiedItemId = "(O)578" },
                new() { Name = "Thunder Egg",       Category = MineCategory.Gems, QualifiedItemId = "(O)563" },
                new() { Name = "Tigerseye",         Category = MineCategory.Gems, QualifiedItemId = "(O)566" },
                new() { Name = "Void Shard",        Category = MineCategory.Gems, QualifiedItemId = "(O)337" },

                // ════════════════════════════════════════════════════════════════════
                // CATEGORY 3 — MONSTERS
                // All monsters from all mine areas (no QualifiedItemId, use sprite)
                // ════════════════════════════════════════════════════════════════════

                // Slimes
                new() { Name = "Green Slime",       Category = MineCategory.Monsters },
                new() { Name = "Frost Jelly",       Category = MineCategory.Monsters },
                new() { Name = "Red Sludge",        Category = MineCategory.Monsters },
                new() { Name = "Purple Slime",      Category = MineCategory.Monsters },
                new() { Name = "Copper Slime",      Category = MineCategory.Monsters },
                new() { Name = "Iron Slime",        Category = MineCategory.Monsters },
                new() { Name = "Tiger Slime",       Category = MineCategory.Monsters },
                new() { Name = "Big Slime",         Category = MineCategory.Monsters },

                // Bugs & Flies
                new() { Name = "Bug",               Category = MineCategory.Monsters },
                new() { Name = "Armored Bug",       Category = MineCategory.Monsters },
                new() { Name = "Cave Fly",          Category = MineCategory.Monsters },
                new() { Name = "Mutant Fly",        Category = MineCategory.Monsters },
                new() { Name = "Grub",              Category = MineCategory.Monsters },
                new() { Name = "Mutant Grub",       Category = MineCategory.Monsters },
                new() { Name = "Spider",            Category = MineCategory.Monsters },
                new() { Name = "Stick Bug",         Category = MineCategory.Monsters },
                new() { Name = "Assassin Bug",      Category = MineCategory.Monsters },

                // Bats
                new() { Name = "Bat",               Category = MineCategory.Monsters },
                new() { Name = "Frost Bat",         Category = MineCategory.Monsters },
                new() { Name = "Lava Bat",          Category = MineCategory.Monsters },
                new() { Name = "Iridium Bat",       Category = MineCategory.Monsters },

                // Crabs
                new() { Name = "Rock Crab",         Category = MineCategory.Monsters },
                new() { Name = "Lava Crab",         Category = MineCategory.Monsters },
                new() { Name = "Iridium Crab",      Category = MineCategory.Monsters },
                new() { Name = "False Magma Cap",   Category = MineCategory.Monsters },

                // Ghosts & Skulls
                new() { Name = "Ghost",             Category = MineCategory.Monsters },
                new() { Name = "Carbon Ghost",      Category = MineCategory.Monsters },
                new() { Name = "Putrid Ghost",      Category = MineCategory.Monsters },
                new() { Name = "Haunted Skull",     Category = MineCategory.Monsters },

                // Volcano Monsters
                new() { Name = "Magma Sprite",      Category = MineCategory.Monsters },
                new() { Name = "Magma Sparker",     Category = MineCategory.Monsters },

                // Undead
                new() { Name = "Skeleton",          Category = MineCategory.Monsters },
                new() { Name = "Skeleton Mage",     Category = MineCategory.Monsters },
                new() { Name = "Pepper Rex",        Category = MineCategory.Monsters },
                new() { Name = "Mummy",             Category = MineCategory.Monsters },

                // Shadows
                new() { Name = "Shadow Brute",      Category = MineCategory.Monsters },
                new() { Name = "Shadow Shaman",     Category = MineCategory.Monsters },
                new() { Name = "Shadow Sniper",     Category = MineCategory.Monsters },

                // Duggies & Golems
                new() { Name = "Duggy",             Category = MineCategory.Monsters },
                new() { Name = "Magma Duggy",       Category = MineCategory.Monsters },
                new() { Name = "Stone Golem",       Category = MineCategory.Monsters },
                new() { Name = "Wilderness Golem",  Category = MineCategory.Monsters },

                // Others
                new() { Name = "Dust Sprite",       Category = MineCategory.Monsters },
                new() { Name = "Metal Head",        Category = MineCategory.Monsters },
                new() { Name = "Hot Head",          Category = MineCategory.Monsters },
                new() { Name = "Squid Kid",         Category = MineCategory.Monsters },
                new() { Name = "Blue Squid",        Category = MineCategory.Monsters },
                new() { Name = "Serpent",           Category = MineCategory.Monsters },
                new() { Name = "Royal Serpent",     Category = MineCategory.Monsters },
                new() { Name = "Dwarvish Sentry",   Category = MineCategory.Monsters },
                new() { Name = "Lava Lurk",         Category = MineCategory.Monsters },

                // ════════════════════════════════════════════════════════════════════
                // CATEGORY 4 — OBJECTS & CONTAINERS
                // Regular mine objects + ResourceClumps (IsResourceClump = true)
                // ════════════════════════════════════════════════════════════════════

                // Common floor objects
                new() { Name = "Boulder",           Category = MineCategory.Objects, QualifiedItemId = "(O)390" },
                new() { Name = "Ice Crystal",       Category = MineCategory.Objects, QualifiedItemId = "(O)84"  },
                new() { Name = "Weeds",             Category = MineCategory.Objects, QualifiedItemId = "(O)0"   },
                new() { Name = "Twig",              Category = MineCategory.Objects, QualifiedItemId = "(O)294" },
                new() { Name = "Mushroom Husk",     Category = MineCategory.Objects, QualifiedItemId = "(O)281" },

                // Containers
                new() { Name = "Crate",             Category = MineCategory.Objects, QualifiedItemId = "(O)130" },
                new() { Name = "Barrel",            Category = MineCategory.Objects, QualifiedItemId = "(O)130" },
                new() { Name = "Metal Crate",       Category = MineCategory.Objects, QualifiedItemId = "(O)130" },
                new() { Name = "Common Chest",      Category = MineCategory.Objects, QualifiedItemId = "(O)78"  },
                new() { Name = "Rare Chest",        Category = MineCategory.Objects, QualifiedItemId = "(O)279" },

                // Special objects
                new() { Name = "Ladder",            Category = MineCategory.Objects, QualifiedItemId = null     },
                new() { Name = "Shaft",             Category = MineCategory.Objects, QualifiedItemId = null     },
                new() { Name = "Torch",             Category = MineCategory.Objects, QualifiedItemId = "(O)93"  },
                new() { Name = "Bomb",              Category = MineCategory.Objects, QualifiedItemId = "(O)286" },
                new() { Name = "Cherry Bomb",       Category = MineCategory.Objects, QualifiedItemId = "(O)287" },
                new() { Name = "Mega Bomb",         Category = MineCategory.Objects, QualifiedItemId = "(O)288" },
                new() { Name = "Mystery Box",       Category = MineCategory.Objects, QualifiedItemId = "(O)897" },
                new() { Name = "Golden Mystery Box",Category = MineCategory.Objects, QualifiedItemId = "(O)898" },
                new() { Name = "Skull Key",         Category = MineCategory.Objects, QualifiedItemId = "(O)322" },
                new() { Name = "Stardrop",          Category = MineCategory.Objects, QualifiedItemId = "(O)434" },

                // ── ResourceClumps (2x2 objects — handled separately in scan) ──────
                new() { Name = "Mine Rock (Large)", Category = MineCategory.Objects, IsResourceClump = true, ResourceClumpId = 600 },
                new() { Name = "Icy Boulder",       Category = MineCategory.Objects, IsResourceClump = true, ResourceClumpId = 752 },
                new() { Name = "Lava Boulder",      Category = MineCategory.Objects, IsResourceClump = true, ResourceClumpId = 754 },
                new() { Name = "Iridium Boulder",   Category = MineCategory.Objects, IsResourceClump = true, ResourceClumpId = 756 },
                new() { Name = "Meteorite",         Category = MineCategory.Objects, IsResourceClump = true, ResourceClumpId = 622 },

                // ════════════════════════════════════════════════════════════════════
                // CATEGORY 5 — FORAGEABLES & DROPS
                // Mushrooms, reagents, equipment drops, special items
                // ════════════════════════════════════════════════════════════════════

                // Mushrooms
                new() { Name = "Red Mushroom",      Category = MineCategory.Forageables, QualifiedItemId = "(O)420" },
                new() { Name = "Purple Mushroom",   Category = MineCategory.Forageables, QualifiedItemId = "(O)422" },
                new() { Name = "Morel",             Category = MineCategory.Forageables, QualifiedItemId = "(O)257" },
                new() { Name = "Chanterelle",       Category = MineCategory.Forageables, QualifiedItemId = "(O)281" },
                new() { Name = "Common Mushroom",   Category = MineCategory.Forageables, QualifiedItemId = "(O)281" },
                new() { Name = "Magma Cap",         Category = MineCategory.Forageables, QualifiedItemId = "(O)851" },

                // Cave forageables
                new() { Name = "Cave Carrot",       Category = MineCategory.Forageables, QualifiedItemId = "(O)78"  },
                new() { Name = "Fiddlehead Fern",   Category = MineCategory.Forageables, QualifiedItemId = "(O)259" },
                new() { Name = "Ginger",            Category = MineCategory.Forageables, QualifiedItemId = "(O)829" },
                new() { Name = "Moss",              Category = MineCategory.Forageables, QualifiedItemId = "(O)moss"},

                // Monster drops
                new() { Name = "Slime",             Category = MineCategory.Forageables, QualifiedItemId = "(O)766" },
                new() { Name = "Bug Meat",          Category = MineCategory.Forageables, QualifiedItemId = "(O)684" },
                new() { Name = "Bat Wing",          Category = MineCategory.Forageables, QualifiedItemId = "(O)767" },
                new() { Name = "Solar Essence",     Category = MineCategory.Forageables, QualifiedItemId = "(O)768" },
                new() { Name = "Void Essence",      Category = MineCategory.Forageables, QualifiedItemId = "(O)769" },
                new() { Name = "Bone Fragment",     Category = MineCategory.Forageables, QualifiedItemId = "(O)881" },
                new() { Name = "Cinder Shard",      Category = MineCategory.Forageables, QualifiedItemId = "(O)848" },
                new() { Name = "Dragon Tooth",      Category = MineCategory.Forageables, QualifiedItemId = "(O)852" },
                new() { Name = "Qi Gem",            Category = MineCategory.Forageables, QualifiedItemId = "(O)858" },

                // Consumables & special items
                new() { Name = "Life Elixir",       Category = MineCategory.Forageables, QualifiedItemId = "(O)773" },
                new() { Name = "Warp Totem: Farm",  Category = MineCategory.Forageables, QualifiedItemId = "(O)261" },
                new() { Name = "Warp Totem: Island",Category = MineCategory.Forageables, QualifiedItemId = "(O)886" },

                // Rare drops
                new() { Name = "Basilisk Paw",      Category = MineCategory.Forageables, QualifiedItemId = "(O)892" },
                new() { Name = "Fairy Box",         Category = MineCategory.Forageables, QualifiedItemId = "(O)897" },
                new() { Name = "Frog Egg",          Category = MineCategory.Forageables, QualifiedItemId = "(O)857" },
                new() { Name = "Golden Spur",       Category = MineCategory.Forageables, QualifiedItemId = "(O)893" },
                new() { Name = "Ice Rod",           Category = MineCategory.Forageables, QualifiedItemId = "(O)60"  },
                new() { Name = "Magic Quiver",      Category = MineCategory.Forageables, QualifiedItemId = "(O)891" },
                new() { Name = "Parrot Egg",        Category = MineCategory.Forageables, QualifiedItemId = "(O)872" },
                new() { Name = "Small Glow Ring",   Category = MineCategory.Forageables, QualifiedItemId = "(O)516" },
                new() { Name = "Small Magnet Ring", Category = MineCategory.Forageables, QualifiedItemId = "(O)518" },
                new() { Name = "Glow Ring",         Category = MineCategory.Forageables, QualifiedItemId = "(O)517" },
                new() { Name = "Magnet Ring",       Category = MineCategory.Forageables, QualifiedItemId = "(O)519" },

                // Boots
                new() { Name = "Leather Boots",     Category = MineCategory.Forageables, QualifiedItemId = "(B)504" },
                new() { Name = "Tundra Boots",      Category = MineCategory.Forageables, QualifiedItemId = "(B)508" },
                new() { Name = "Firewalker Boots",  Category = MineCategory.Forageables, QualifiedItemId = "(B)509" },
                new() { Name = "Space Boots",       Category = MineCategory.Forageables, QualifiedItemId = "(B)512" },
                new() { Name = "Crystal Shoes",     Category = MineCategory.Forageables, QualifiedItemId = "(B)514" },
                new() { Name = "Mermaid Boots",     Category = MineCategory.Forageables, QualifiedItemId = "(B)510" },
                new() { Name = "Dragonscale Boots", Category = MineCategory.Forageables, QualifiedItemId = "(B)513" },
            };

            AllItems = list.AsReadOnly();

            // Build by-category lookup
            var byCategory = new Dictionary<string, IReadOnlyList<MineItemData>>();
            foreach (var cat in new[]
            {
                MineCategory.Ores, MineCategory.Gems,
                MineCategory.Monsters, MineCategory.Objects,
                MineCategory.Forageables
            })
            {
                byCategory[cat] = list.Where(x => x.Category == cat).ToList().AsReadOnly();
            }
            ByCategory = byCategory;
        }

        /// <summary>
        /// Returns the ResourceClump name for a given parentSheetIndex, or null if not known.
        /// </summary>
        public static string? GetResourceClumpName(int parentSheetIndex)
        {
            return parentSheetIndex switch
            {
                600 => "Mine Rock (Large)",
                622 => "Meteorite",
                752 => "Icy Boulder",
                754 => "Lava Boulder",
                756 => "Iridium Boulder",
                _   => null
            };
        }

        /// <summary>
        /// Returns true if the given name belongs to a ResourceClump entry.
        /// </summary>
        public static bool IsResourceClumpName(string name)
        {
            return AllItems.Any(x => x.IsResourceClump && x.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
