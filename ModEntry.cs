using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using xTile.Layers;
using SObject = StardewValley.Object;

namespace CaveContentDisplay
{
    public class ScannedItem
    {
        /// <summary>Stable, language-independent key (e.g. "Item:Stone", "Monster:Green Slime").</summary>
        public string CanonicalKey       { get; set; } = "";
        /// <summary>Localized display name for HUD rendering.</summary>
        public string DisplayName        { get; set; } = "";
        public string? QualifiedItemId   { get; set; }
        public int Count                 { get; set; }
        public string Category           { get; set; } = "";
        public Texture2D? CustomTexture       { get; set; }
        public Rectangle? CustomSourceRect    { get; set; }
    }

    /// <summary>
    /// Entry point for the CaveContentDisplay mod.
    /// </summary>
    public class ModEntry : Mod
    {
        // ── State ──────────────────────────────────────────────────────────────
        private CaveHUD _hud = null!;
        private ModConfig Config = null!;
        private ItemCacheManager _cacheManager = null!;

        /// <summary>Current floor scan result (unfiltered): canonicalKey → item info.</summary>
        private Dictionary<string, ScannedItem> _floorContents = new();

        /// <summary>Filtered view of _floorContents for HUD display.</summary>
        private Dictionary<string, ScannedItem> _filteredContents = new();

        private bool _hudVisible = true;
        private bool _isDirty    = true;
        private int  _tickCounter = 0;
        /// <summary>True when heavy per-tick events are subscribed (only in caves).</summary>
        private bool _caveEventsActive = false;
        /// <summary>Cached tile-based items (ladders/shafts) — only rescanned on warp (Issue 3.1).</summary>
        private Dictionary<string, ScannedItem>? _cachedTileItems;

        // ── SMAPI Entry ────────────────────────────────────────────────────────
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            _hud   = new CaveHUD();
            _cacheManager = new ItemCacheManager(helper, Monitor);
            _cacheManager.LoadCache();

            // ── Migrate old English-name-based FilteredItems to canonical keys ──
            MigrateFilteredItems();

            helper.Events.GameLoop.GameLaunched   += OnGameLaunched;
            helper.Events.Input.ButtonPressed     += OnButtonPressed;
            helper.Events.Player.Warped           += OnWarped;
            helper.Events.GameLoop.SaveLoaded     += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            // Note: UpdateTicked, RenderedHud, ObjectListChanged, NpcListChanged
            // are subscribed/unsubscribed dynamically via SubscribeCaveEvents/UnsubscribeCaveEvents
            // to avoid per-tick overhead when the player is not in a cave (Issue 5.1).

            Monitor.Log("CaveContentDisplay loaded!", LogLevel.Info);
        }

        /// <summary>
        /// One-time migration: converts old English display-name entries in FilteredItems
        /// to canonical keys (e.g. "Stone" → "Item:Stone").
        /// </summary>
        private void MigrateFilteredItems()
        {
            bool changed = false;
            for (int i = 0; i < Config.FilteredItems.Count; i++)
            {
                string entry = Config.FilteredItems[i];
                if (entry.Contains(':')) continue; // already a canonical key

                if (MineItemDatabase.EnglishNameToKey.TryGetValue(entry, out var canonical))
                {
                    Config.FilteredItems[i] = canonical;
                    Monitor.Log($"[Migration] Filter '{entry}' → '{canonical}'", LogLevel.Debug);
                    changed = true;
                }
                else
                {
                    // Unknown name — wrap as Item:{name}
                    Config.FilteredItems[i] = CanonicalPrefix.Build(CanonicalPrefix.Item, entry);
                    changed = true;
                }
            }
            if (changed)
                Helper.WriteConfig(Config);
        }

        // ── GMCM ──────────────────────────────────────────────────────────────
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => { Config = new ModConfig(); MigrateFilteredItems(); },
                save: () => Helper.WriteConfig(Config));

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.showIcons.name").ToString(),
                tooltip: () => Helper.Translation.Get("config.showIcons.desc").ToString(),
                getValue: () => Config.ShowIcons,
                setValue: value => Config.ShowIcons = value);

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.refreshMode.name").ToString(),
                tooltip: () => Helper.Translation.Get("config.refreshMode.desc").ToString(),
                getValue: () => Config.RefreshMode.ToString(),
                setValue: value => Config.RefreshMode = Enum.Parse<RefreshMode>(value),
                allowedValues: Enum.GetNames(typeof(RefreshMode)));

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.sortMode.name").ToString(),
                tooltip: () => Helper.Translation.Get("config.sortMode.desc").ToString(),
                getValue: () => Config.SortMode.ToString(),
                setValue: value => Config.SortMode = Enum.Parse<SortMode>(value),
                allowedValues: Enum.GetNames(typeof(SortMode)));

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.filterMode.name").ToString(),
                tooltip: () => Helper.Translation.Get("config.filterMode.desc").ToString(),
                getValue: () => Config.FilterMode.ToString(),
                setValue: value => Config.FilterMode = Enum.Parse<FilterMode>(value),
                allowedValues: Enum.GetNames(typeof(FilterMode)));

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.hudX.name").ToString(),
                tooltip: () => Helper.Translation.Get("config.hudX.desc").ToString(),
                getValue: () => Config.HudX,
                setValue: value => Config.HudX = value);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.hudY.name").ToString(),
                tooltip: () => Helper.Translation.Get("config.hudY.desc").ToString(),
                getValue: () => Config.HudY,
                setValue: value => Config.HudY = value);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.guiScale.name").ToString(),
                tooltip: () => Helper.Translation.Get("config.guiScale.desc").ToString(),
                getValue: () => Config.GuiScale,
                setValue: value => Config.GuiScale = value,
                min: 0.5f, max: 2.0f, interval: 0.1f);

            // Read-only display: how many items are currently filtered
            configMenu.AddParagraph(
                mod: ModManifest,
                text: () =>
                {
                    int c = Config.FilteredItems.Count;
                    string mode = Config.FilterMode == FilterMode.Whitelist ? "Whitelist" : "Blacklist";
                    return c == 0
                        ? $"Active Filters ({mode}): (All items shown)"
                        : $"Active Filters ({mode}): {c} item(s) — press [{Config.FilterMenuKey}] to change";
                });

            configMenu.AddKeybind(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.filterMenuKey.name").ToString(),
                tooltip: () => Helper.Translation.Get("config.filterMenuKey.desc").ToString(),
                getValue: () => Config.FilterMenuKey,
                setValue: value => Config.FilterMenuKey = value);

            configMenu.AddKeybind(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.toggleKey.name").ToString(),
                tooltip: () => Helper.Translation.Get("config.toggleKey.desc").ToString(),
                getValue: () => Config.ToggleKey,
                setValue: value => Config.ToggleKey = value);
        }

        // ── Event Handlers ─────────────────────────────────────────────────────

        private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
        {
            if (Game1.currentLocation != null && e.Location == Game1.currentLocation)
                _isDirty = true;
        }

        private void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
        {
            if (Game1.currentLocation != null && e.Location == Game1.currentLocation)
                _isDirty = true;
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            // Save cache when leaving any cave/underground area
            if (CaveDetector.IsCaveLocation(e.OldLocation) && _floorContents.Count > 0)
            {
                _cacheManager.MergeFloorScan(_floorContents.Values);
                _cacheManager.SaveCache();
            }

            _floorContents.Clear();
            _filteredContents.Clear();
            _cachedTileItems = null;  // Invalidate tile cache for new floor (Issue 3.1)

            if (CaveDetector.IsCaveLocation(e.NewLocation))
            {
                _isDirty      = true;
                _tickCounter  = GetIntervalTicks(); // force immediate update
                SubscribeCaveEvents();
            }
            else
            {
                UnsubscribeCaveEvents();
            }
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        /// <summary>Reload per-save cache when a save is loaded (Issue 5.4).</summary>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            _floorContents.Clear();
            _filteredContents.Clear();
            _cachedTileItems = null;
            _hudVisible = true;
            _isDirty = true;
            _cacheManager.LoadCache();
        }

        /// <summary>Clear stale state when returning to title screen (Issue 5.4).</summary>
        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            _floorContents.Clear();
            _filteredContents.Clear();
            _cachedTileItems = null;
            _hudVisible = true;
            _isDirty = true;
            UnsubscribeCaveEvents();
        }

        // ── Dynamic Event Subscription (Issue 5.1) ─────────────────────────────

        private void SubscribeCaveEvents()
        {
            if (_caveEventsActive) return;
            _caveEventsActive = true;
            Helper.Events.GameLoop.UpdateTicked   += OnUpdateTicked;
            Helper.Events.Display.RenderedHud     += OnRenderedHud;
            Helper.Events.World.ObjectListChanged += OnObjectListChanged;
            Helper.Events.World.NpcListChanged    += OnNpcListChanged;
        }

        private void UnsubscribeCaveEvents()
        {
            if (!_caveEventsActive) return;
            _caveEventsActive = false;
            Helper.Events.GameLoop.UpdateTicked   -= OnUpdateTicked;
            Helper.Events.Display.RenderedHud     -= OnRenderedHud;
            Helper.Events.World.ObjectListChanged -= OnObjectListChanged;
            Helper.Events.World.NpcListChanged    -= OnNpcListChanged;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !IsInMine()) return;

            _tickCounter++;
            if (_tickCounter >= GetIntervalTicks() || _isDirty)
            {
                _tickCounter = 0;
                _floorContents = ScanCurrentFloor();
                // Merge full (unfiltered) scan into cache so no items are lost
                _cacheManager.MergeFloorScan(_floorContents.Values);
                // Apply filter for HUD display only
                _filteredContents = ApplyFilter(_floorContents);
                _isDirty = false;
            }
        }

        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            // Check visibility first to skip unnecessary work (Issue 5.2)
            if (!_hudVisible || !Context.IsWorldReady) return;
            _hud.Draw(e.SpriteBatch, _filteredContents, _hudVisible, Config);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // ── Intercept ALL keyboard input when FilterPickerMenu is open ──────
            // This prevents game hotkeys (T=chat, E=inventory, etc.) from firing
            // while the user is typing in the search box.
            if (Game1.activeClickableMenu is FilterPickerMenu picker)
            {
                if (e.Button.TryGetKeyboard(out Keys key))
                {
                    Helper.Input.Suppress(e.Button);
                    picker.ReceiveSuppressedKey(key);
                    return;
                }
            }

            if (e.Button == Config.ToggleKey)
            {
                _hudVisible = !_hudVisible;
                Game1.playSound("drumkit6");
            }
            else if (e.Button == Config.FilterMenuKey && Game1.activeClickableMenu == null)
            {
                OpenFilterPicker();
            }
        }

        // ── Core Helpers ────────────────────────────────────────────────────────

        private static bool IsInMine()
            => CaveDetector.IsCaveLocation(Game1.currentLocation);

        // Ore stones that have distinct drops — must NOT be normalized to "Stone".
        // Derived from MineItemDatabase to stay in sync automatically (Issue 4.5).
        private static readonly HashSet<string> _oreStoneNames = new(
            MineItemDatabase.AllItems
                .Where(x => x.Category == MineCategory.Ores && x.Name.EndsWith(" Stone", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Name),
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// Cosmetic stone variant internal names (obj.Name) that all drop Stone (390).
        /// Uses exact matching to avoid false positives on items like
        /// "Soapstone", "Lemon Stone", or "Limestone" which are distinct items.
        /// </summary>
        private static readonly HashSet<string> _cosmeticStoneVariants = new(StringComparer.OrdinalIgnoreCase)
        {
            "Stone",
        };

        /// <summary>
        /// Normalizes cosmetic stone variant names to the canonical "Stone".
        /// Ore stones with distinct drops are preserved as-is.
        /// Operates on internal English names (obj.Name), not localized DisplayName.
        /// </summary>
        private static string NormalizeItemName(string name)
        {
            if (_oreStoneNames.Contains(name)) return name;
            if (_cosmeticStoneVariants.Contains(name)) return "Stone";
            return name;
        }

        private int GetIntervalTicks() => Config.RefreshMode switch
        {
            RefreshMode.RealTime => 1,
            RefreshMode.Sec3     => 180,
            RefreshMode.Sec5     => 300,
            RefreshMode.Sec8     => 480,
            RefreshMode.Sec10    => 600,
            _                    => 1
        };

        private void OpenFilterPicker()
        {
            Game1.activeClickableMenu = new FilterPickerMenu(
                _cacheManager.Cache,
                _floorContents,
                Config,
                cfg =>
                {
                    Config = cfg;
                    Helper.WriteConfig(Config);
                    _isDirty = true;
                });
        }

        // ── Scan Logic ─────────────────────────────────────────────────────────

        /// <summary>
        /// Scans the current mine floor in 4 passes:
        /// 1. Regular objects (SObject)
        /// 2. ResourceClumps (2x2 objects)
        /// 3. Monsters (NPC subclass)
        /// 4. Ladders &amp; Shafts (map tiles on Buildings layer)
        /// </summary>
        private Dictionary<string, ScannedItem> ScanCurrentFloor()
        {
            var result = new Dictionary<string, ScannedItem>(StringComparer.OrdinalIgnoreCase);
            var location = Game1.currentLocation;
            if (location == null || !CaveDetector.IsCaveLocation(location))
                return result;

            // ── Pass 1: Regular Objects ────────────────────────────────────────
            foreach (SObject obj in location.objects.Values)
            {
                // Use internal English name (obj.Name) for stable, language-independent key building
                string internalName = obj.Name ?? "UnknownObject";
                string normalizedName = NormalizeItemName(internalName);

                // Build canonical key from internal English name
                string canonicalKey;
                if (MineItemDatabase.EnglishNameToKey.TryGetValue(normalizedName, out var knownKey))
                    canonicalKey = knownKey;
                else
                    canonicalKey = CanonicalPrefix.Build(CanonicalPrefix.Item, normalizedName);



                // Resolve localized display name for HUD
                string displayName = MineItemDatabase.GetLocalizedDisplayName(canonicalKey);

                string? qid = obj.QualifiedItemId;
                string cat  = GetObjectCategory(obj);

                Texture2D? tex  = null;
                Rectangle? rect = null;
                try
                {
                    if (obj is BreakableContainer)
                    {
                        var data = ItemRegistry.GetDataOrErrorItem("(O)130");
                        tex  = data?.GetTexture();
                        rect = data?.GetSourceRect();
                    }
                    else if (!string.IsNullOrEmpty(qid))
                    {
                        var data = ItemRegistry.GetDataOrErrorItem(qid);
                        if (data == null || data.IsErrorItem)
                        {
                            tex  = Game1.objectSpriteSheet;
                            rect = Game1.getSourceRectForStandardTileSheet(tex, obj.ParentSheetIndex, 16, 16);
                        }
                        else
                        {
                            tex  = data.GetTexture();
                            rect = data.GetSourceRect();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Icon load failed for '{displayName}' ({qid}): {ex.Message}", LogLevel.Trace);
                }

                AddOrIncrement(result, canonicalKey, displayName, qid, cat, tex, rect);
            }

            // ── Pass 2: ResourceClumps (2x2 objects) ──────────────────────────
            foreach (ResourceClump clump in location.resourceClumps)
            {
                int psi = clump.parentSheetIndex.Value;
                string? name = MineItemDatabase.GetResourceClumpName(psi);
                if (name == null) continue;

                string canonicalKey = CanonicalPrefix.Build(CanonicalPrefix.RC, psi.ToString());


                Texture2D? tex  = null;
                Rectangle? rect = null;
                try
                {
                    string iconQid = psi switch
                    {
                        600 => "(O)390",
                        622 => "(O)74",   // Meteorite → Prismatic Shard (distinctive icon)
                        752 => "(O)84",
                        754 => "(O)848",
                        756 => "(O)386",
                        _   => "(O)390"
                    };
                    var data = ItemRegistry.GetDataOrErrorItem(iconQid);
                    tex  = data?.GetTexture();
                    rect = data?.GetSourceRect();
                }
                catch { }

                AddOrIncrement(result, canonicalKey, name, null, MineCategory.Objects, tex, rect);
            }

            // ── Pass 3: Monsters ──────────────────────────────────────────────
            foreach (var character in location.characters)
            {
                if (character is not Monster monster) continue;

                // monster.Name is the internal English name; displayName is localized
                string internalName = monster.Name ?? "UnknownMonster";
                string canonicalKey = CanonicalPrefix.Build(CanonicalPrefix.Monster, internalName);


                string displayName = monster.displayName
                    ?? monster.Name
                    ?? Helper.Translation.Get("name.UnknownMonster").ToString();

                Texture2D? tex  = monster.Sprite?.Texture;
                Rectangle? rect = monster.Sprite?.sourceRect;
                AddOrIncrement(result, canonicalKey, displayName, null, MineCategory.Monsters, tex, rect);
            }

            // ── Pass 4: Ladders & Shafts (cached per-floor, Issue 3.1) ───────
            // Tile layout is static per floor — scan once, reuse until warp.
            _cachedTileItems ??= ScanTileItems(location);
            foreach (var kvp in _cachedTileItems)
            {
                var src = kvp.Value;
                if (!result.ContainsKey(kvp.Key))
                {
                    result[kvp.Key] = new ScannedItem
                    {
                        CanonicalKey     = src.CanonicalKey,
                        DisplayName      = src.DisplayName,
                        QualifiedItemId  = src.QualifiedItemId,
                        Count            = src.Count,
                        Category         = src.Category,
                        CustomTexture    = src.CustomTexture,
                        CustomSourceRect = src.CustomSourceRect,
                    };
                }
            }

            return result;
        }

        /// <summary>
        /// One-time tile scan for ladders/shafts on the Buildings layer.
        /// Result is cached in _cachedTileItems until the player warps (Issue 3.1).
        /// </summary>
        private Dictionary<string, ScannedItem> ScanTileItems(GameLocation location)
        {
            var tileResult = new Dictionary<string, ScannedItem>(StringComparer.OrdinalIgnoreCase);
            try
            {
                Layer? buildingsLayer = location.map?.GetLayer("Buildings");
                if (buildingsLayer != null)
                {
                    for (int tx = 0; tx < buildingsLayer.LayerWidth; tx++)
                    {
                        for (int ty = 0; ty < buildingsLayer.LayerHeight; ty++)
                        {
                            var tile = buildingsLayer.Tiles[tx, ty];
                            if (tile == null) continue;
                            int tileIndex = tile.TileIndex;
                            if (tileIndex != 173 && tileIndex != 174) continue;

                            string name = tileIndex == 173 ? "Ladder" : "Shaft";
                            string canonicalKey = CanonicalPrefix.Build(CanonicalPrefix.Special, name);

                            Texture2D? tex  = null;
                            Rectangle? rect = null;
                            try
                            {
                                tex  = Game1.objectSpriteSheet;
                                rect = Game1.getSourceRectForStandardTileSheet(tex, tileIndex, 16, 16);
                            }
                            catch { }

                            AddOrIncrement(tileResult, canonicalKey, name, null, MineCategory.Objects, tex, rect);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Ladder/Shaft scan failed: {ex.Message}", LogLevel.Trace);
            }
            return tileResult;
        }

        // ── Match / Category Helpers ───────────────────────────────────────────

        /// <summary>
        /// Checks if an item with the given canonical key passes the current filter.
        /// Supports both Whitelist and Blacklist modes.
        /// </summary>
        private bool IsMatch(string canonicalKey)
        {
            if (Config.FilteredItems.Count == 0) return true;
            bool isInList = Config.FilteredItems.Contains(canonicalKey, StringComparer.OrdinalIgnoreCase);
            return Config.FilterMode == FilterMode.Whitelist ? isInList : !isInList;
        }

        /// <summary>
        /// Creates a filtered copy of the full scan results for HUD display.
        /// Only includes items that pass the current Whitelist/Blacklist filter.
        /// </summary>
        private Dictionary<string, ScannedItem> ApplyFilter(Dictionary<string, ScannedItem> fullScan)
        {
            if (Config.FilteredItems.Count == 0)
                return fullScan; // no filter active — return reference directly

            var filtered = new Dictionary<string, ScannedItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in fullScan)
            {
                if (IsMatch(kvp.Key))
                    filtered[kvp.Key] = kvp.Value;
            }
            return filtered;
        }

        private string GetObjectCategory(SObject obj)
        {
            if (obj is BreakableContainer) return MineCategory.Objects;
            string cat = obj.getCategoryName() ?? "";
            return string.IsNullOrWhiteSpace(cat) ? MineCategory.Ores : cat;
        }

        private static void AddOrIncrement(
            Dictionary<string, ScannedItem> dict,
            string canonicalKey,
            string displayName,
            string? qid,
            string cat,
            Texture2D? customTex  = null,
            Rectangle? customRect = null)
        {
            if (string.IsNullOrWhiteSpace(canonicalKey)) return;
            if (!dict.TryGetValue(canonicalKey, out var item))
            {
                item = new ScannedItem
                {
                    CanonicalKey     = canonicalKey,
                    DisplayName      = displayName,
                    QualifiedItemId  = qid,
                    Count            = 0,
                    Category         = cat,
                    CustomTexture    = customTex,
                    CustomSourceRect = customRect,
                };
                dict[canonicalKey] = item;
            }
            item.Count++;
        }
    }
}
