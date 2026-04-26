using System.Collections.Generic;

namespace CaveContentDisplay
{
    public enum RefreshMode
    {
        RealTime,
        Sec3,
        Sec5,
        Sec8,
        Sec10
    }

    public enum SortMode
    {
        Alphabetical,
        QuantityDesc,
        QuantityAsc,
        Category
    }

    public class ModConfig
    {
        public bool ShowIcons { get; set; } = true;
        public RefreshMode RefreshMode { get; set; } = RefreshMode.RealTime;
        public SortMode SortMode { get; set; } = SortMode.Alphabetical;
        public int HudX { get; set; } = 20;
        public int HudY { get; set; } = 100;
        public float GuiScale { get; set; } = 1.0f;
        public StardewModdingAPI.SButton ToggleKey { get; set; } = StardewModdingAPI.SButton.H;
        /// <summary>Key to open the filter picker menu.</summary>
        public StardewModdingAPI.SButton FilterMenuKey { get; set; } = StardewModdingAPI.SButton.R;
        /// <summary>Items to show in HUD. Empty = show all.</summary>
        public List<string> FilteredItems { get; set; } = new();
    }
}
