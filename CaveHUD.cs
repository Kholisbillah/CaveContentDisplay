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

        public void Draw(SpriteBatch spriteBatch, Dictionary<string, ScannedItem> contents, bool visible, ModConfig config)
        {
            if (!visible) return;

            var font = Game1.smallFont;
            if (font == null) return;

            var itemsList = contents.Values.AsEnumerable();
            switch (config.SortMode)
            {
                case SortMode.QuantityDesc:
                    itemsList = itemsList.OrderByDescending(i => i.Count).ThenBy(i => i.DisplayName);
                    break;
                case SortMode.QuantityAsc:
                    itemsList = itemsList.OrderBy(i => i.Count).ThenBy(i => i.DisplayName);
                    break;
                case SortMode.Category:
                    itemsList = itemsList.OrderBy(i => i.Category).ThenBy(i => i.DisplayName);
                    break;
                case SortMode.Alphabetical:
                default:
                    itemsList = itemsList.OrderBy(i => i.DisplayName);
                    break;
            }

            var items = itemsList.ToList();
            bool empty = items.Count == 0;

            // Calculate dimensions based on scale
            float scale = config.GuiScale;
            float scaledPadding = PanelPadding * scale;
            float scaledIconSize = IconSize * scale;
            float scaledIconGap = IconGap * scale;
            float minLineHeight = (font.MeasureString("A").Y * scale);
            float scaledLineHeight = Math.Max(minLineHeight, config.ShowIcons ? scaledIconSize : 0);
            
            float maxTextWidth = 0;
            if (empty)
            {
                maxTextWidth = font.MeasureString("No objects found.").X * scale;
            }
            else
            {
                foreach (var item in items)
                {
                    float w = font.MeasureString($"{item.DisplayName} x{item.Count}").X * scale;
                    if (w > maxTextWidth) maxTextWidth = w;
                }
            }

            float extraIconWidth = config.ShowIcons && !empty ? scaledIconSize + scaledIconGap : 0;
            float titleWidth = font.MeasureString("Floor Contents:").X * scale;
            float panelWidth = Math.Max(maxTextWidth + extraIconWidth, titleWidth) + scaledPadding * 2;
            
            float titleHeight = scaledLineHeight + (4 * scale);
            int rows = empty ? 1 : items.Count;
            float panelHeight = scaledPadding * 2 + titleHeight + rows * scaledLineHeight;

            float startX = config.HudX;
            float startY = config.HudY;

            // Background
            var backgroundRect = new Rectangle((int)startX, (int)startY, (int)panelWidth, (int)panelHeight);
            spriteBatch.Draw(Game1.fadeToBlackRect, backgroundRect, BackgroundColor);

            // Title
            float currentY = startY + scaledPadding;
            spriteBatch.DrawString(font, "Floor Contents:", new Vector2(startX + scaledPadding, currentY), TitleColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
            currentY += titleHeight;

            // Separator
            var separatorRect = new Rectangle((int)(startX + scaledPadding), (int)(currentY - (4 * scale)), (int)(panelWidth - scaledPadding * 2), 1);
            spriteBatch.Draw(Game1.fadeToBlackRect, separatorRect, Color.White * 0.4f);

            // Items
            if (empty)
            {
                spriteBatch.DrawString(font, "No objects found.", new Vector2(startX + scaledPadding, currentY), TextColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
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

        private void DrawItemIcon(SpriteBatch spriteBatch, ScannedItem item, float x, float y, float size)
        {
            Rectangle destRect = new Rectangle((int)x, (int)y, (int)size, (int)size);
            Texture2D? tex = item.CustomTexture;
            Rectangle? sourceRect = item.CustomSourceRect;

            if (tex == null && !string.IsNullOrEmpty(item.QualifiedItemId))
            {
                try
                {
                    ParsedItemData data = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
                    if (data != null && !data.IsErrorItem)
                    {
                        tex = data.GetTexture();
                        sourceRect = data.GetSourceRect();
                    }
                }
                catch
                {
                    // Ignore fail
                }
            }

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

