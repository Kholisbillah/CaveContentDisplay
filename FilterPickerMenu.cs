using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace CaveContentDisplay
{
    public class FilterPickerMenu : IClickableMenu
    {
        private const int MenuWidth  = 800;
        private const int MenuHeight = 600;
        private const int RowHeight  = 40;
        private const int IconSize   = 32;
        private const int Padding    = 16;
        private const int TitleHeight= 48;
        private const int SearchH    = 36;
        private const int TabBarH    = 38;
        private const int BottomBarH = 52;

        // Cached RasterizerState instances to avoid per-frame allocation (Issue 2.1)
        private static readonly RasterizerState ScissorEnabled  = new() { ScissorTestEnable = true };
        private static readonly RasterizerState ScissorDisabled = new() { ScissorTestEnable = false };

        private static readonly string[] TabLabels = { "Ores", "Gems", "Monsters", "Objects", "Forageables" };
        private static readonly string[] TabCategories =
        {
            MineCategory.Ores, MineCategory.Gems,
            MineCategory.Monsters, MineCategory.Objects,
            MineCategory.Forageables
        };

        private int _activeTab = 0;
        private Rectangle[] _tabRects = new Rectangle[TabLabels.Length];

        private readonly ModConfig _config;
        private readonly Action<ModConfig> _saveConfig;
        private readonly List<FilterEntry>[] _entriesByTab;
        private List<FilterEntry> _visibleEntries = new();

        private string _searchText = "";
        private bool _searchFocused;
        private int _scrollIndex;
        private int _maxVisibleRows;
        private int _blinkCounter;  // Frame-based cursor blink (Issue 6.6)

        private Rectangle _searchBox;
        private Rectangle _listArea;
        private Rectangle _saveBtn;
        private Rectangle _clearBtn;
        private Rectangle _modeBtn;
        private int _hoveredRow = -1;

        public FilterPickerMenu(
            Dictionary<string, CachedItemEntry> itemCache,
            Dictionary<string, ScannedItem> floorContents,
            ModConfig config,
            Action<ModConfig> saveConfig)
            : base(0, 0, 0, 0, showUpperRightCloseButton: true)
        {
            // Clamp dimensions to viewport to prevent off-screen rendering (Issue 4.3)
            int actualWidth  = Math.Min(MenuWidth,  Game1.uiViewport.Width  - 64);
            int actualHeight = Math.Min(MenuHeight, Game1.uiViewport.Height - 64);
            width  = actualWidth;
            height = actualHeight;
            xPositionOnScreen = (Game1.uiViewport.Width  - actualWidth)  / 2;
            yPositionOnScreen = (Game1.uiViewport.Height - actualHeight) / 2;
            // Reposition close button after adjusting dimensions
            if (upperRightCloseButton != null)
            {
                upperRightCloseButton.bounds = new Rectangle(
                    xPositionOnScreen + actualWidth - 36, yPositionOnScreen - 8, 48, 48);
            }
            _config     = config;
            _saveConfig = saveConfig;

            var masterKeys = new HashSet<string>(
                MineItemDatabase.AllItems.Select(x => x.CanonicalKey),
                StringComparer.OrdinalIgnoreCase);

            _entriesByTab = new List<FilterEntry>[TabLabels.Length];
            for (int t = 0; t < TabLabels.Length; t++)
                _entriesByTab[t] = new List<FilterEntry>();

            // 1. Master-list items
            foreach (var data in MineItemDatabase.AllItems)
            {
                int tabIndex = Array.IndexOf(TabCategories, data.Category);
                if (tabIndex < 0) continue;

                bool active = config.FilteredItems.Contains(data.CanonicalKey, StringComparer.OrdinalIgnoreCase);
                itemCache.TryGetValue(data.CanonicalKey, out var cached);
                floorContents.TryGetValue(data.CanonicalKey, out var floor);

                string displayName = MineItemDatabase.GetLocalizedDisplayName(data);

                Texture2D? tex = null; Rectangle? rect = null;
                if (!data.IsResourceClump && !string.IsNullOrEmpty(data.QualifiedItemId))
                {
                    try
                    {
                        var pd = ItemRegistry.GetDataOrErrorItem(data.QualifiedItemId);
                        if (pd != null && !pd.IsErrorItem) { tex = pd.GetTexture(); rect = pd.GetSourceRect(); }
                    }
                    catch { }
                }

                _entriesByTab[tabIndex].Add(new FilterEntry
                {
                    CanonicalKey    = data.CanonicalKey,
                    DisplayName     = displayName,
                    Category        = data.Category,
                    IsChecked       = active,
                    TimesFound      = cached?.TimesFound ?? 0,
                    FloorCount      = floor?.Count ?? 0,
                    IsModded        = false,
                    IsResourceClump = data.IsResourceClump,
                    QualifiedItemId = data.QualifiedItemId,
                    IconTex         = tex,
                    IconRect        = rect,
                });
            }

            // 2. Modded items from cache not in master list
            foreach (var kvp in itemCache)
            {
                if (masterKeys.Contains(kvp.Key)) continue;
                bool active = config.FilteredItems.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase);
                int tabIndex = Array.FindIndex(TabCategories,
                    c => c.Equals(kvp.Value.Category, StringComparison.OrdinalIgnoreCase));
                if (tabIndex < 0) tabIndex = 2;

                floorContents.TryGetValue(kvp.Key, out var floorMod);
                _entriesByTab[tabIndex].Add(new FilterEntry
                {
                    CanonicalKey = kvp.Key,
                    DisplayName  = kvp.Value.Name,
                    Category     = kvp.Value.Category,
                    IsChecked    = active,
                    TimesFound   = kvp.Value.TimesFound,
                    FloorCount   = floorMod?.Count ?? 0,
                    IsModded     = true,
                });
            }

            // 3. Sort
            for (int t = 0; t < _entriesByTab.Length; t++)
            {
                _entriesByTab[t] = _entriesByTab[t]
                    .OrderBy(e => e.IsModded ? 2 : (e.TimesFound > 0 ? 0 : 1))
                    .ThenByDescending(e => e.TimesFound)
                    .ThenBy(e => e.DisplayName)
                    .ToList();
            }

            CalculateLayout();
            ApplySearch();
        }

        private void CalculateLayout()
        {
            int x = xPositionOnScreen, y = yPositionOnScreen;
            _searchBox = new Rectangle(x + Padding, y + TitleHeight, width - Padding * 2, SearchH);
            int tabY = _searchBox.Bottom + 6;
            int tabW = (width - Padding * 2) / TabLabels.Length;
            for (int i = 0; i < TabLabels.Length; i++)
                _tabRects[i] = new Rectangle(x + Padding + i * tabW, tabY, tabW, TabBarH);

            int listTop = tabY + TabBarH + 4;
            int listBottom = y + height - BottomBarH - 4;
            _listArea = new Rectangle(x + Padding, listTop, width - Padding * 2 - 14, listBottom - listTop);
            _maxVisibleRows = _listArea.Height / RowHeight;

            int btnY = y + height - BottomBarH + (BottomBarH - 40) / 2;
            int btnW = 130;
            _clearBtn = new Rectangle(x + Padding, btnY, btnW, 40);
            _modeBtn  = new Rectangle(x + (width - btnW) / 2, btnY, btnW, 40);
            _saveBtn  = new Rectangle(x + width - Padding - btnW, btnY, btnW, 40);
        }

        private void ApplySearch()
        {
            _scrollIndex = 0;
            var tab = _entriesByTab[_activeTab];
            if (string.IsNullOrWhiteSpace(_searchText))
                _visibleEntries = tab.ToList();
            else
            {
                string q = _searchText.Trim();
                _visibleEntries = tab
                    .Where(e => e.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        // ── Draw ──────────────────────────────────────────────────────────────────
        public override void draw(SpriteBatch b)
        {
            _blinkCounter++;  // advance blink timer each frame
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.55f);
            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                xPositionOnScreen, yPositionOnScreen, width, height, Color.White, drawShadow: true);

            DrawTitle(b); DrawSearchBar(b); DrawTabs(b); DrawItemList(b); DrawScrollBar(b); DrawBottomBar(b);
            if (_visibleEntries.Count == 0) DrawEmptyState(b);
            upperRightCloseButton?.draw(b);
            drawMouse(b);
        }

        private void DrawTitle(SpriteBatch b)
        {
            bool isBlacklist = _config.FilterMode == FilterMode.Blacklist;
            string title = isBlacklist ? "Item Filter (Blacklist)" : "Item Filter (Whitelist)";
            Vector2 sz = Game1.dialogueFont.MeasureString(title);
            b.DrawString(Game1.dialogueFont, title,
                new Vector2(xPositionOnScreen + (width - sz.X) / 2f, yPositionOnScreen + 12),
                Game1.textColor);
        }

        private void DrawSearchBar(SpriteBatch b)
        {
            Color borderColor = _searchFocused ? new Color(150, 120, 80) : new Color(100, 80, 60);
            DrawFilledRect(b, new Rectangle(_searchBox.X - 2, _searchBox.Y - 2, _searchBox.Width + 4, _searchBox.Height + 4), borderColor * 0.8f);
            DrawFilledRect(b, _searchBox, new Color(245, 235, 220));
            string display = string.IsNullOrEmpty(_searchText) ? "Search items..." : _searchText;
            Color textColor = string.IsNullOrEmpty(_searchText) ? Color.Gray : new Color(60, 40, 20);
            b.DrawString(Game1.smallFont, display,
                new Vector2(_searchBox.X + 8, _searchBox.Y + (_searchBox.Height - Game1.smallFont.LineSpacing) / 2f + 1), textColor);
            if (_searchFocused && !string.IsNullOrEmpty(_searchText) && (_blinkCounter / 30) % 2 == 0)
            {
                float cx = _searchBox.X + 8 + Game1.smallFont.MeasureString(_searchText).X + 1;
                b.DrawString(Game1.smallFont, "|", new Vector2(cx, _searchBox.Y + (_searchBox.Height - Game1.smallFont.LineSpacing) / 2f + 1), new Color(60, 40, 20));
            }
        }

        private void DrawTabs(SpriteBatch b)
        {
            for (int i = 0; i < TabLabels.Length; i++)
            {
                bool active = i == _activeTab;
                Color bgColor = active ? new Color(210, 180, 140) : new Color(170, 140, 110) * 0.6f;
                DrawFilledRect(b, _tabRects[i], bgColor);
                Color borderCol = active ? new Color(150, 110, 70) : new Color(120, 90, 60) * 0.5f;
                DrawRectBorder(b, _tabRects[i], active ? 2 : 1, borderCol);
                if (active)
                    DrawFilledRect(b, new Rectangle(_tabRects[i].X + 1, _tabRects[i].Bottom - 2, _tabRects[i].Width - 2, 3), bgColor);

                // Show checked count badge on each tab (Issue 6.7)
                int checkedCount = _entriesByTab[i].Count(e => e.IsChecked);
                string label = checkedCount > 0 ? $"{TabLabels[i]} ({checkedCount})" : TabLabels[i];
                Vector2 lsz = Game1.smallFont.MeasureString(label);
                Color labelColor = active ? new Color(60, 35, 15) : new Color(80, 60, 40) * 0.8f;
                b.DrawString(Game1.smallFont, label,
                    new Vector2(_tabRects[i].X + (_tabRects[i].Width - lsz.X) / 2f, _tabRects[i].Y + (_tabRects[i].Height - lsz.Y) / 2f), labelColor);
                if (active)
                {
                    int dotSize = 5;
                    DrawFilledRect(b, new Rectangle(_tabRects[i].X + (_tabRects[i].Width - dotSize) / 2, _tabRects[i].Y + 4, dotSize, dotSize), new Color(180, 100, 40));
                }
            }
        }

        private void DrawItemList(SpriteBatch b)
        {
            Rectangle prevScissor = b.GraphicsDevice.ScissorRectangle;
            try
            {
                b.End();
                b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, ScissorEnabled);
            }
            catch (InvalidOperationException)
            {
                // SpriteBatch was in an unexpected state — another mod may have interfered.
                // Fall through and draw without scissor clipping rather than crashing.
                return;
            }
            b.GraphicsDevice.ScissorRectangle = _listArea;

            bool isBlacklist = _config.FilterMode == FilterMode.Blacklist;
            _hoveredRow = -1;
            int end = Math.Min(_scrollIndex + _maxVisibleRows, _visibleEntries.Count);
            var mouse = Game1.getMousePosition();

            for (int i = _scrollIndex; i < end; i++)
            {
                var entry = _visibleEntries[i];
                int relRow = i - _scrollIndex;
                int rowY = _listArea.Y + relRow * RowHeight;
                var rowRect = new Rectangle(_listArea.X, rowY, _listArea.Width, RowHeight);

                if (relRow % 2 == 0) DrawFilledRect(b, rowRect, Color.SandyBrown * 0.08f);
                if (rowRect.Contains(mouse)) { _hoveredRow = i; DrawFilledRect(b, rowRect, Color.Wheat * 0.35f); }
                if (entry.IsChecked)
                {
                    Color hlColor = isBlacklist ? new Color(200, 80, 80) * 0.18f : new Color(120, 200, 100) * 0.18f;
                    DrawFilledRect(b, rowRect, hlColor);
                }

                int checkX = _listArea.X + 6;
                int checkY = rowY + (RowHeight - 20) / 2;
                DrawCheckbox(b, checkX, checkY, entry.IsChecked, isBlacklist);

                int iconX = checkX + 24;
                int iconY = rowY + (RowHeight - IconSize) / 2;
                DrawIcon(b, entry, new Rectangle(iconX, iconY, IconSize, IconSize));

                float nameX = iconX + IconSize + 8;
                float nameY = rowY + (RowHeight - Game1.smallFont.LineSpacing) / 2f;
                Color nameColor = entry.IsChecked
                    ? (isBlacklist ? new Color(180, 60, 60) : new Color(60, 120, 20))
                    : entry.IsModded ? new Color(60, 100, 180) : Game1.textColor;
                b.DrawString(Game1.smallFont, entry.DisplayName, new Vector2(nameX, nameY), nameColor);

                DrawBadge(b, entry, rowRect);
                DrawFilledRect(b, new Rectangle(_listArea.X, rowY + RowHeight - 1, _listArea.Width, 1), Color.SaddleBrown * 0.12f);
            }

            try
            {
                b.End();
                b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            }
            catch (InvalidOperationException) { /* best-effort restore */ }
            b.GraphicsDevice.ScissorRectangle = prevScissor;
        }

        private void DrawCheckbox(SpriteBatch b, int x, int y, bool isChecked, bool isBlacklist)
        {
            DrawRectBorder(b, new Rectangle(x, y, 20, 20), 2, new Color(120, 90, 60) * 0.7f);
            Color fill = isChecked
                ? (isBlacklist ? new Color(200, 70, 70) * 0.8f : new Color(80, 180, 80) * 0.8f)
                : Color.White * 0.25f;
            DrawFilledRect(b, new Rectangle(x + 2, y + 2, 16, 16), fill);
            if (isChecked)
            {
                string mark = isBlacklist ? "✗" : "✓";
                b.DrawString(Game1.smallFont, mark, new Vector2(x + 2, y + 1), Color.White);
            }
        }

        private void DrawIcon(SpriteBatch b, FilterEntry entry, Rectangle dest)
        {
            if (entry.IsResourceClump)
            {
                DrawFilledRect(b, dest, Color.SlateGray * 0.4f);
                b.DrawString(Game1.smallFont, "⛏", new Vector2(dest.X + 2, dest.Y + 6), Color.White * 0.8f);
                return;
            }
            if (entry.IconTex != null && entry.IconRect.HasValue) { b.Draw(entry.IconTex, dest, entry.IconRect.Value, Color.White); return; }
            if (!string.IsNullOrEmpty(entry.QualifiedItemId))
            {
                try
                {
                    var pd = ItemRegistry.GetDataOrErrorItem(entry.QualifiedItemId);
                    if (pd != null && !pd.IsErrorItem) { b.Draw(pd.GetTexture(), dest, pd.GetSourceRect(), Color.White); return; }
                }
                catch { }
            }
            DrawFilledRect(b, dest, Color.Gray * 0.25f);
            b.DrawString(Game1.smallFont, "?", new Vector2(dest.X + dest.Width / 2f - 5, dest.Y + dest.Height / 2f - 8), Color.Gray * 0.6f);
        }

        private void DrawBadge(SpriteBatch b, FilterEntry entry, Rectangle rowRect)
        {
            string badge; Color badgeColor;
            if (entry.IsModded) { badge = "[Modded]"; badgeColor = new Color(70, 130, 210); }
            else if (entry.FloorCount > 0) { badge = $"Found {entry.FloorCount}×"; badgeColor = new Color(60, 160, 60); }
            else { badge = "Not seen yet"; badgeColor = Color.Gray; }
            Vector2 bsz = Game1.smallFont.MeasureString(badge);
            float bx = rowRect.Right - bsz.X - 8;
            float by = rowRect.Y + (RowHeight - bsz.Y) / 2f;
            b.DrawString(Game1.smallFont, badge, new Vector2(bx, by), badgeColor * 0.9f, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        }

        private void DrawScrollBar(SpriteBatch b)
        {
            if (_visibleEntries.Count <= _maxVisibleRows) return;
            int trackH = _listArea.Height;
            int thumbH = Math.Max(32, trackH * _maxVisibleRows / Math.Max(1, _visibleEntries.Count));
            int maxScroll = _visibleEntries.Count - _maxVisibleRows;
            int thumbY = _listArea.Y + (maxScroll > 0 ? (_scrollIndex * (trackH - thumbH)) / maxScroll : 0);
            int scrollX = _listArea.Right + 4;
            DrawFilledRect(b, new Rectangle(scrollX, _listArea.Y, 8, trackH), Color.SaddleBrown * 0.15f);
            DrawRectBorder(b, new Rectangle(scrollX, _listArea.Y, 8, trackH), 1, Color.SaddleBrown * 0.3f);
            DrawFilledRect(b, new Rectangle(scrollX + 1, thumbY, 6, thumbH), Color.SaddleBrown * 0.6f);
        }

        private void DrawBottomBar(SpriteBatch b)
        {
            DrawFilledRect(b, new Rectangle(xPositionOnScreen + Padding, _listArea.Bottom + 4, width - Padding * 2, 1), Color.SaddleBrown * 0.3f);
            DrawSdvButton(b, _clearBtn, "Clear All", new Color(160, 50, 50));

            // Mode toggle button
            bool isBlacklist = _config.FilterMode == FilterMode.Blacklist;
            string modeLabel = isBlacklist ? "Blacklist" : "Whitelist";
            Color modeColor = isBlacklist ? new Color(180, 60, 60) : new Color(50, 120, 50);
            DrawSdvButton(b, _modeBtn, modeLabel, modeColor);

            DrawSdvButton(b, _saveBtn, "Save & Close", new Color(50, 140, 50));
        }

        private void DrawSdvButton(SpriteBatch b, Rectangle btn, string label, Color textColor)
        {
            bool hovered = btn.Contains(Game1.getMousePosition());
            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                btn.X, btn.Y, btn.Width, btn.Height, hovered ? Color.Wheat : Color.White, drawShadow: false);
            Vector2 sz = Game1.smallFont.MeasureString(label);
            b.DrawString(Game1.smallFont, label,
                new Vector2(btn.X + (btn.Width - sz.X) / 2f, btn.Y + (btn.Height - sz.Y) / 2f), textColor);
        }

        private void DrawEmptyState(SpriteBatch b)
        {
            string msg = _allEntriesForCurrentTab.Count == 0 ? "No items found. Enter a cave first!" : "No items match your search.";
            Vector2 sz = Game1.smallFont.MeasureString(msg);
            b.DrawString(Game1.smallFont, msg,
                new Vector2(_listArea.X + (_listArea.Width - sz.X) / 2f, _listArea.Y + (_listArea.Height - sz.Y) / 2f), Color.Gray);
        }

        private List<FilterEntry> _allEntriesForCurrentTab => _entriesByTab[_activeTab];

        private static void DrawFilledRect(SpriteBatch b, Rectangle r, Color c) => b.Draw(Game1.fadeToBlackRect, r, c);
        private static void DrawRectBorder(SpriteBatch b, Rectangle r, int t, Color c)
        {
            b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X, r.Y, r.Width, t), c);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X, r.Y, t, r.Height), c);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
        }

        // ── Input ─────────────────────────────────────────────────────────────────
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (upperRightCloseButton?.containsPoint(x, y) == true) { Save(); exitThisMenu(); return; }

            for (int i = 0; i < _tabRects.Length; i++)
            {
                if (_tabRects[i].Contains(x, y))
                {
                    if (_activeTab != i) { _activeTab = i; _scrollIndex = 0; ApplySearch(); Game1.playSound("smallSelect"); }
                    return;
                }
            }

            if (_saveBtn.Contains(x, y)) { Save(); exitThisMenu(); return; }
            if (_clearBtn.Contains(x, y))
            {
                foreach (var tab in _entriesByTab) foreach (var e in tab) e.IsChecked = false;
                Game1.playSound("drumkit6"); return;
            }

            // Mode toggle
            if (_modeBtn.Contains(x, y))
            {
                _config.FilterMode = _config.FilterMode == FilterMode.Whitelist
                    ? FilterMode.Blacklist : FilterMode.Whitelist;
                Game1.playSound("smallSelect");
                return;
            }

            if (_searchBox.Contains(x, y)) { _searchFocused = true; return; }
            _searchFocused = false;

            int end = Math.Min(_scrollIndex + _maxVisibleRows, _visibleEntries.Count);
            for (int i = _scrollIndex; i < end; i++)
            {
                int relRow = i - _scrollIndex;
                var rowRect = new Rectangle(_listArea.X, _listArea.Y + relRow * RowHeight, _listArea.Width, RowHeight);
                if (rowRect.Contains(x, y)) { _visibleEntries[i].IsChecked = !_visibleEntries[i].IsChecked; Game1.playSound("drumkit6"); return; }
            }
        }

        public override void receiveScrollWheelAction(int direction)
        {
            int maxScroll = Math.Max(0, _visibleEntries.Count - _maxVisibleRows);
            _scrollIndex = Math.Clamp(_scrollIndex - Math.Sign(direction), 0, maxScroll);
        }

        public override void receiveKeyPress(Keys key)
        {
            if (key == Keys.Escape) { if (_searchFocused) _searchFocused = false; else exitThisMenu(); return; }
            if (key == Keys.Enter) { if (_searchFocused) _searchFocused = false; else { Save(); exitThisMenu(); } return; }
            if (_searchFocused)
            {
                if (key == Keys.Back && _searchText.Length > 0) { _searchText = _searchText[..^1]; ApplySearch(); return; }
                char? c = KeyToChar(key, Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift));
                if (c.HasValue) AppendSearchChar(c.Value);
                return;
            }
            base.receiveKeyPress(key);
        }

        private static char? KeyToChar(Keys key, bool shift)
        {
            if (key >= Keys.A && key <= Keys.Z) return shift ? (char)('A' + (key - Keys.A)) : (char)('a' + (key - Keys.A));
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                char[] shifted = { ')', '!', '@', '#', '$', '%', '^', '&', '*', '(' };
                return shift ? shifted[key - Keys.D0] : (char)('0' + (key - Keys.D0));
            }
            if (key == Keys.Space) return ' ';
            return null;
        }

        public override void receiveGamePadButton(Buttons b)
        {
            switch (b)
            {
                case Buttons.B: exitThisMenu(); break;
                case Buttons.Start: Save(); exitThisMenu(); break;
                case Buttons.DPadDown: receiveScrollWheelAction(-1); break;
                case Buttons.DPadUp: receiveScrollWheelAction(1); break;
                case Buttons.RightTrigger: ChangeTab(1); break;
                case Buttons.LeftTrigger: ChangeTab(-1); break;
            }
        }

        private void ChangeTab(int direction)
        {
            _activeTab = (_activeTab + direction + TabLabels.Length) % TabLabels.Length;
            _scrollIndex = 0; ApplySearch(); Game1.playSound("smallSelect");
        }

        private void AppendSearchChar(char c)
        {
            if (!_searchFocused || c < ' ') return;
            _searchText += c; ApplySearch();
        }

        public void ReceiveSuppressedKey(Keys key) => receiveKeyPress(key);

        // ── Save ──────────────────────────────────────────────────────────────────
        private void Save()
        {
            _config.FilteredItems = _entriesByTab
                .SelectMany(tab => tab)
                .Where(e => e.IsChecked)
                .Select(e => e.CanonicalKey)
                .ToList();
            _saveConfig(_config);
            Game1.playSound("money");
        }

        private class FilterEntry
        {
            public string CanonicalKey     { get; set; } = "";
            public string DisplayName      { get; set; } = "";
            public string Category         { get; set; } = "";
            public bool IsChecked          { get; set; }
            public int TimesFound          { get; set; }
            public int FloorCount          { get; set; }
            public bool IsModded           { get; set; }
            public bool IsResourceClump    { get; set; }
            public string? QualifiedItemId { get; set; }
            public Texture2D? IconTex      { get; set; }
            public Rectangle? IconRect     { get; set; }
        }
    }
}
