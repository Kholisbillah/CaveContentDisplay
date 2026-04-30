using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace CaveContentDisplay
{
    public class CaveHUD
    {
        // ── Layout Constants ───────────────────────────────────────────────────
        private const int PanelPadding = 10;
        private const int LineHeight   = 24;
        private const int MinPanelWidth= 200;
        private const int IconSize     = 16;
        private const int IconGap      = 6;

        // ── Warna ─────────────────────────────────────────────────────────────
        private static readonly Color BackgroundColor = Color.Black * 0.5f;
        private static readonly Color TextColor = Color.White * 0.85f;
        private static readonly Color TitleColor = Color.Gold * 0.95f;

        // ── Cached layout data (Issue 3.3 / 3.4) ─────────────────────────────
        private Dictionary<string, ScannedItem>? _lastContents;
        private SortMode _lastSortMode;
        private List<ScannedItem> _cachedSorted = new();
        private float _cachedPanelWidth;

        public void Draw(SpriteBatch spriteBatch, Dictionary<string, ScannedItem> contents, bool visible, ModConfig config)
        {
            if (!visible) return;

            var font = Game1.smallFont;
            if (font == null) return;

            // Re-sort and re-measure only when contents reference or sort mode changes (Issue 3.3 / 3.4)
            if (!ReferenceEquals(contents, _lastContents) || config.SortMode != _lastSortMode)
            {
                _lastContents = contents;
                _lastSortMode = config.SortMode;
                RebuildSortedList(contents, config.SortMode);
                RecalcPanelWidth(font, config);
            }

            var items = _cachedSorted;
            bool empty = items.Count == 0;

            // Calculate dimensions based on scale
            float scale = config.GuiScale;
            float scaledPadding = PanelPadding * scale;
            float scaledIconSize = IconSize * scale;
            float scaledIconGap = IconGap * scale;
            float minLineHeight = (font.MeasureString("A").Y * scale);
            float scaledLineHeight = Math.Max(minLineHeight, config.ShowIcons ? scaledIconSize : 0);

            string titleText = "Floor Contents:";
            float titleWidth = font.MeasureString(titleText).X * scale;
            float extraIconWidth = config.ShowIcons && !empty ? scaledIconSize + scaledIconGap : 0;
            float panelWidth = Math.Max(_cachedPanelWidth + extraIconWidth, titleWidth) + scaledPadding * 2;

            float titleHeight = scaledLineHeight + (4 * scale);
            int rows = empty ? 1 : items.Count;
            float panelHeight = scaledPadding * 2 + titleHeight + rows * scaledLineHeight;

            // Clamp position to viewport bounds (Issue 6.1)
            int vpW = Game1.uiViewport.Width;
            int vpH = Game1.uiViewport.Height;
            float startX = Math.Clamp((float)config.HudX, 0, Math.Max(0, vpW - panelWidth));
            float startY = Math.Clamp((float)config.HudY, 0, Math.Max(0, vpH - panelHeight));

            // Background
            var backgroundRect = new Rectangle((int)startX, (int)startY, (int)panelWidth, (int)panelHeight);
            spriteBatch.Draw(Game1.fadeToBlackRect, backgroundRect, BackgroundColor);

            // Title
            float currentY = startY + scaledPadding;
            spriteBatch.DrawString(font, titleText, new Vector2(startX + scaledPadding, currentY), TitleColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
            currentY += titleHeight;

            // Separator
            var separatorRect = new Rectangle((int)(startX + scaledPadding), (int)(currentY - (4 * scale)), (int)(panelWidth - scaledPadding * 2), 1);
            spriteBatch.Draw(Game1.fadeToBlackRect, separatorRect, Color.White * 0.4f);

            // Items
            string emptyMsg = "No objects found.";
            if (empty)
            {
                spriteBatch.DrawString(font, emptyMsg, new Vector2(startX + scaledPadding, currentY), TextColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                return;
            }

            foreach (var item in items)
            {
                float currentX = startX + scaledPadding;

                if (config.ShowIcons)
                {
                    DrawItemIcon(spriteBatch, item, currentX, currentY, scaledIconSize);
                    currentX += scaledIconSize + scaledIconGap;
                }

                string text = $"{item.DisplayName} x{item.Count}";
                spriteBatch.DrawString(font, text, new Vector2(currentX, currentY), TextColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                currentY += scaledLineHeight;
            }
        }

        /// <summary>Invalidate cached layout so next Draw re-sorts and re-measures.</summary>
        public void Invalidate() => _lastContents = null;

        private void RebuildSortedList(Dictionary<string, ScannedItem> contents, SortMode sortMode)
        {
            var itemsList = contents.Values.AsEnumerable();
            _cachedSorted = sortMode switch
            {
                SortMode.QuantityDesc  => itemsList.OrderByDescending(i => i.Count).ThenBy(i => i.DisplayName).ToList(),
                SortMode.QuantityAsc   => itemsList.OrderBy(i => i.Count).ThenBy(i => i.DisplayName).ToList(),
                SortMode.Category      => itemsList.OrderBy(i => i.Category).ThenBy(i => i.DisplayName).ToList(),
                _                      => itemsList.OrderBy(i => i.DisplayName).ToList(),
            };
        }

        private void RecalcPanelWidth(SpriteFont font, ModConfig config)
        {
            float scale = config.GuiScale;
            float maxTextWidth = 0;
            if (_cachedSorted.Count == 0)
            {
                maxTextWidth = font.MeasureString("No objects found.").X * scale;
            }
            else
            {
                foreach (var item in _cachedSorted)
                {
                    float w = font.MeasureString($"{item.DisplayName} x{item.Count}").X * scale;
                    if (w > maxTextWidth) maxTextWidth = w;
                }
            }
            _cachedPanelWidth = maxTextWidth;
        }

        private void DrawItemIcon(SpriteBatch spriteBatch, ScannedItem item, float x, float y, float size)
        {
            Rectangle destRect = new Rectangle((int)x, (int)y, (int)size, (int)size);
            Texture2D? tex = item.CustomTexture;
            Rectangle? sourceRect = item.CustomSourceRect;

            if (tex != null && sourceRect.HasValue)
            {
                spriteBatch.Draw(tex, destRect, sourceRect.Value, Color.White);
            }
            else
            {
                // Fallback kotak kosong jika icon tak ditemukan
                spriteBatch.Draw(Game1.fadeToBlackRect, destRect, Color.White * 0.2f);
            }
        }
    }
}
