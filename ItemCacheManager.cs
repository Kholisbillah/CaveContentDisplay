using System;
using System.Collections.Generic;
using StardewModdingAPI;

namespace CaveContentDisplay
{
    /// <summary>
    /// Cached entry for an item encountered during cave scanning.
    /// Stored in "data/scanned-items.json".
    /// </summary>
    public class CachedItemEntry
    {
        public string Name           { get; set; } = "";
        public string Category       { get; set; } = "";
        public string? QualifiedItemId { get; set; }
        public bool IsResourceClump  { get; set; }
        public int TimesFound        { get; set; }
        public string LastSeen       { get; set; } = "";
        /// <summary>True if this item is NOT in the MineItemDatabase master list (i.e. from a mod).</summary>
        public bool IsModded         { get; set; }
    }

    /// <summary>
    /// Manages loading, updating, and saving the persistent item scan cache.
    /// Uses SMAPI's IModHelper.Data.WriteJsonFile / ReadJsonFile for serialization.
    /// Cache path: "data/scanned-items.json" (relative to mod folder).
    /// </summary>
    public class ItemCacheManager
    {
        private const string CachePath = "data/scanned-items.json";

        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;

        /// <summary>In-memory cache keyed by item name (case-insensitive).</summary>
        public Dictionary<string, CachedItemEntry> Cache { get; private set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public ItemCacheManager(IModHelper helper, IMonitor monitor)
        {
            _helper  = helper;
            _monitor = monitor;
        }

        // ── Load ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads the cache from disk. Gracefully falls back to an empty cache
        /// if the file is missing or corrupt.
        /// </summary>
        public void LoadCache()
        {
            try
            {
                var data = _helper.Data.ReadJsonFile<Dictionary<string, CachedItemEntry>>(CachePath);
                if (data != null)
                {
                    Cache = new Dictionary<string, CachedItemEntry>(data, StringComparer.OrdinalIgnoreCase);
                    _monitor.Log($"[Cache] Loaded {Cache.Count} cached item(s).", LogLevel.Debug);
                }
                else
                {
                    Cache = new(StringComparer.OrdinalIgnoreCase);
                    _monitor.Log("[Cache] No cache file found. Starting fresh.", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Cache = new(StringComparer.OrdinalIgnoreCase);
                _monitor.Log($"[Cache] Failed to load cache (corrupt?): {ex.Message}. Starting fresh.", LogLevel.Warn);
            }
        }

        // ── Update ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Merges a floor scan result into the cache.
        /// Increments TimesFound for each item and updates LastSeen timestamp.
        /// </summary>
        public void MergeFloorScan(IEnumerable<ScannedItem> scannedItems)
        {
            var masterNames = new System.Collections.Generic.HashSet<string>(
                MineItemDatabase.AllItems.Select(x => x.Name),
                StringComparer.OrdinalIgnoreCase);

            string now = DateTime.UtcNow.ToString("o");

            foreach (var item in scannedItems)
            {
                if (string.IsNullOrWhiteSpace(item.DisplayName)) continue;

                if (!Cache.TryGetValue(item.DisplayName, out var entry))
                {
                    entry = new CachedItemEntry
                    {
                        Name            = item.DisplayName,
                        Category        = item.Category,
                        QualifiedItemId = item.QualifiedItemId,
                        IsModded        = !masterNames.Contains(item.DisplayName),
                    };
                    Cache[item.DisplayName] = entry;
                }
                entry.TimesFound += item.Count;
                entry.LastSeen    = now;
                // Update modded flag in case master list changed
                entry.IsModded = !masterNames.Contains(item.DisplayName);
            }
        }

        // ── Save ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Saves the current cache to disk.
        /// </summary>
        public void SaveCache()
        {
            try
            {
                _helper.Data.WriteJsonFile(CachePath, Cache);
                _monitor.Log($"[Cache] Saved {Cache.Count} item(s).", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                _monitor.Log($"[Cache] Failed to save cache: {ex.Message}", LogLevel.Warn);
            }
        }

        // ── Reset ─────────────────────────────────────────────────────────────────

        /// <summary>Clears the in-memory cache and deletes the file.</summary>
        public void ResetCache()
        {
            Cache.Clear();
            try { _helper.Data.WriteJsonFile(CachePath, new Dictionary<string, CachedItemEntry>()); }
            catch { /* ignore */ }
            _monitor.Log("[Cache] Cache reset.", LogLevel.Info);
        }
    }
}
