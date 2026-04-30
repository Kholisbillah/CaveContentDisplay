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

    /// <summary>
    /// Determines how the FilteredItems list is interpreted.
    /// </summary>
    public enum FilterMode
    {
        /// <summary>Only checked items are shown in the HUD. Empty list = show all.</summary>
        Whitelist,
        /// <summary>Checked items are hidden from the HUD. Empty list = show all.</summary>
        Blacklist,
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
        public StardewModdingAPI.SButton FilterMenuKey { get; set; } = StardewModdingAPI.SButton.F7;
        /// <summary>How the filter list is interpreted: Whitelist (show checked) or Blacklist (hide checked).</summary>
        public FilterMode FilterMode { get; set; } = FilterMode.Whitelist;
        /// <summary>Canonical keys of items to filter. Interpretation depends on FilterMode.</summary>
        public List<string> FilteredItems { get; set; } = new();
    }
}
