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
using SObject = StardewValley.Object;

namespace CaveContentDisplay
{
    public class ScannedItem
    {
        public string DisplayName      { get; set; } = "";
        public string? QualifiedItemId { get; set; }
        public int Count               { get; set; }
        public string Category         { get; set; } = "";
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

        /// <summary>Current floor scan result: name → item info.</summary>
        private Dictionary<string, ScannedItem> _floorContents = new();

        private bool _hudVisible = true;
        private bool _isDirty    = true;
        private int  _tickCounter = 0;

        // ── SMAPI Entry ────────────────────────────────────────────────────────
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            _hud   = new CaveHUD();
            _cacheManager = new ItemCacheManager(helper, Monitor);
            _cacheManager.LoadCache();

            helper.Events.GameLoop.GameLaunched   += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked   += OnUpdateTicked;
            helper.Events.Display.RenderedHud     += OnRenderedHud;
            helper.Events.Input.ButtonPressed     += OnButtonPressed;
            helper.Events.Player.Warped           += OnWarped;
            helper.Events.World.ObjectListChanged += OnObjectListChanged;
            helper.Events.World.NpcListChanged    += OnNpcListChanged;

            Monitor.Log("CaveContentDisplay dimuat!", LogLevel.Info);
        }

        // ── GMCM ──────────────────────────────────────────────────────────────
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config));

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.showIcons.name").ToString(),
                getValue: () => Config.ShowIcons,
                setValue: value => Config.ShowIcons = value);

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.refreshMode.name").ToString(),
                getValue: () => Config.RefreshMode.ToString(),
                setValue: value => Config.RefreshMode = Enum.Parse<RefreshMode>(value),
                allowedValues: Enum.GetNames(typeof(RefreshMode)));

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.sortMode.name").ToString(),
                getValue: () => Config.SortMode.ToString(),
                setValue: value => Config.SortMode = Enum.Parse<SortMode>(value),
                allowedValues: Enum.GetNames(typeof(SortMode)));

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.hudX.name").ToString(),
                getValue: () => Config.HudX,
                setValue: value => Config.HudX = value);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.hudY.name").ToString(),
                getValue: () => Config.HudY,
                setValue: value => Config.HudY = value);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.guiScale.name").ToString(),
                getValue: () => Config.GuiScale,
                setValue: value => Config.GuiScale = value,
                min: 0.5f, max: 2.0f, interval: 0.1f);

            // Read-only display: how many items are currently filtered
            configMenu.AddParagraph(
                mod: ModManifest,
                text: () =>
                {
                    int c = Config.FilteredItems.Count;
                    return c == 0
                        ? "Active Filters: (All items shown)"
                        : $"Active Filters: {c} item(s) selected — press [{Config.FilterMenuKey}] to change";
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
            if (e.Location == Game1.currentLocation) _isDirty = true;
        }

        private void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
        {
            if (e.Location == Game1.currentLocation) _isDirty = true;
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

            if (CaveDetector.IsCaveLocation(e.NewLocation))
            {
                _isDirty      = true;
                _tickCounter  = GetIntervalTicks(); // force immediate update
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !IsInMine()) return;

            _tickCounter++;
            if (_tickCounter >= GetIntervalTicks())
            {
                _tickCounter = 0;
                if (_isDirty)
                {
                    _floorContents = ScanCurrentFloor();
                    // Merge this scan into the persistent cache (in-memory only; saved on warp)
                    _cacheManager.MergeFloorScan(_floorContents.Values);
                    _isDirty = false;
                }
            }
        }

        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady || !IsInMine()) return;
            _hud.Draw(e.SpriteBatch, _floorContents, _hudVisible, Config);
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
            }
            else if (e.Button == Config.FilterMenuKey && Game1.activeClickableMenu == null)
            {
                OpenFilterPicker();
            }
        }

        // ── Core Helpers ────────────────────────────────────────────────────────

        private static bool IsInMine()
            => CaveDetector.IsCaveLocation(Game1.currentLocation);

        // Ore stones that have distinct drops — must NOT be normalized to "Stone"
        private static readonly HashSet<string> _oreStoneNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Copper Stone", "Iron Stone", "Gold Stone", "Iridium Stone",
            "Radioactive Stone", "Diamond Stone", "Fossil Stone", "Mystic Stone",
        };

        /// <summary>
        /// Normalizes cosmetic stone variant names (Snowy Stone, Lava Stone, etc.)
        /// to the canonical "Stone". Ore stones with distinct drops are preserved as-is.
        /// Rule: name contains "Stone", NOT "Node", and NOT in the ore-stone blacklist.
        /// </summary>
        private static string NormalizeItemName(string name)
        {
            if (_oreStoneNames.Contains(name)) return name; // ore stone — keep identity
            if (name.Contains("Stone", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("Node",  StringComparison.OrdinalIgnoreCase))
                return "Stone"; // cosmetic variant — merge into "Stone"
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
        /// Scans the current mine floor in 3 passes:
        /// 1. Regular objects (SObject)
        /// 2. ResourceClumps (2x2 objects)
        /// 3. Monsters (NPC subclass)
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
                string name = NormalizeItemName(GetObjectDisplayName(obj));
                if (!IsMatch(name)) continue;

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
                    Monitor.Log($"Icon load failed for '{name}' ({qid}): {ex.Message}", LogLevel.Trace);
                }

                AddOrIncrement(result, name, qid, cat, tex, rect);
            }

            // ── Pass 2: ResourceClumps (2x2 objects) ──────────────────────────
            foreach (ResourceClump clump in location.resourceClumps)
            {
                int psi = clump.parentSheetIndex.Value;
                string? name = MineItemDatabase.GetResourceClumpName(psi);
                if (name == null) continue; // unknown clump type — skip
                if (!IsMatch(name)) continue;

                // Use best-effort static icon for resource clumps
                Texture2D? tex  = null;
                Rectangle? rect = null;
                try
                {
                    // Map to a representative item icon
                    string iconQid = psi switch
                    {
                        600 => "(O)390", // Stone
                        622 => "(O)386", // Iridium Ore (Meteorite)
                        752 => "(O)84",  // Frozen Tear (ice)
                        754 => "(O)848", // Cinder Shard (lava)
                        756 => "(O)386", // Iridium Ore
                        _   => "(O)390"
                    };
                    var data = ItemRegistry.GetDataOrErrorItem(iconQid);
                    tex  = data?.GetTexture();
                    rect = data?.GetSourceRect();
                }
                catch { }

                AddOrIncrement(result, name, null, MineCategory.Objects, tex, rect);
            }

            // ── Pass 3: Monsters ──────────────────────────────────────────────
            foreach (var character in location.characters)
            {
                if (character is not Monster monster) continue;

                string name = monster.displayName
                    ?? monster.Name
                    ?? Helper.Translation.Get("name.UnknownMonster").ToString();
                if (!IsMatch(name)) continue;

                Texture2D? tex  = monster.Sprite?.Texture;
                Rectangle? rect = monster.Sprite?.sourceRect;
                AddOrIncrement(result, name, null, MineCategory.Monsters, tex, rect);
            }

            return result;
        }

        // ── Match / Category Helpers ───────────────────────────────────────────

        private bool IsMatch(string itemName)
        {
            if (Config.FilteredItems.Count == 0) return true;
            return Config.FilteredItems.Contains(itemName, StringComparer.OrdinalIgnoreCase);
        }

        private string GetObjectDisplayName(SObject obj)
        {
            return obj.DisplayName ?? obj.Name
                ?? Helper.Translation.Get("name.UnknownObject").ToString();
        }

        private string GetObjectCategory(SObject obj)
        {
            if (obj is BreakableContainer) return MineCategory.Objects;
            string cat = obj.getCategoryName() ?? "";
            return string.IsNullOrWhiteSpace(cat) ? MineCategory.Ores : cat;
        }

        private static void AddOrIncrement(
            Dictionary<string, ScannedItem> dict,
            string name,
            string? qid,
            string cat,
            Texture2D? customTex  = null,
            Rectangle? customRect = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!dict.TryGetValue(name, out var item))
            {
                item = new ScannedItem
                {
                    DisplayName      = name,
                    QualifiedItemId  = qid,
                    Count            = 0,
                    Category         = cat,
                    CustomTexture    = customTex,
                    CustomSourceRect = customRect,
                };
                dict[name] = item;
            }
            item.Count++;
        }
    }
}
