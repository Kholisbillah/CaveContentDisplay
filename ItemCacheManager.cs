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
        /// <summary>Stable canonical key (e.g. "Item:Stone", "Monster:Green Slime").</summary>
        public string CanonicalKey    { get; set; } = "";
        /// <summary>English display name (for readability in JSON and legacy fallback).</summary>
        public string Name            { get; set; } = "";
        public string Category        { get; set; } = "";
        public string? QualifiedItemId { get; set; }
        public bool IsResourceClump   { get; set; }
        public int TimesFound         { get; set; }
        public string LastSeen        { get; set; } = "";
        /// <summary>True if this item is NOT in the MineItemDatabase master list.</summary>
        public bool IsModded          { get; set; }
    }

    /// <summary>
    /// Manages loading, updating, and saving the persistent item scan cache.
    /// Cache path: "data/scanned-items.json" (relative to mod folder).
    /// Keyed by CanonicalKey (language-independent).
    /// </summary>
    public class ItemCacheManager
    {
        /// <summary>
        /// Returns the per-save cache path. Falls back to a shared path if no save is loaded.
        /// This prevents split-screen/multiplayer from corrupting a shared file (Issue 2.4).
        /// </summary>
        private static string CachePath =>
            string.IsNullOrEmpty(Constants.SaveFolderName)
                ? "data/scanned-items.json"
                : $"data/{Constants.SaveFolderName}/scanned-items.json";

        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;

        /// <summary>In-memory cache keyed by canonical key (case-insensitive).</summary>
        public Dictionary<string, CachedItemEntry> Cache { get; private set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public ItemCacheManager(IModHelper helper, IMonitor monitor)
        {
            _helper  = helper;
            _monitor = monitor;
        }

        // ── Load ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads the cache from disk. Migrates old name-based keys to canonical keys.
        /// </summary>
        public void LoadCache()
        {
            try
            {
                var data = _helper.Data.ReadJsonFile<Dictionary<string, CachedItemEntry>>(CachePath);
                if (data != null)
                {
                    var migrated = new Dictionary<string, CachedItemEntry>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kvp in data)
                    {
                        string key = kvp.Key;
                        var entry = kvp.Value;

                        // Migration: if key looks like an old English name (no colon prefix),
                        // convert it to a canonical key using the master database.
                        if (!key.Contains(':'))
                        {
                            if (MineItemDatabase.EnglishNameToKey.TryGetValue(key, out var canonical))
                            {
                                key = canonical;
                                entry.CanonicalKey = canonical;
                                _monitor.Log($"[Cache] Migrated key '{kvp.Key}' → '{canonical}'", LogLevel.Debug);
                            }
                            else
                            {
                                // Unknown old name — treat as modded, keep as "Item:{name}"
                                key = CanonicalPrefix.Build(CanonicalPrefix.Item, key);
                                entry.CanonicalKey = key;
                            }
                        }

                        if (string.IsNullOrEmpty(entry.CanonicalKey))
                            entry.CanonicalKey = key;

                        migrated.TryAdd(key, entry);
                    }
                    Cache = migrated;
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
        /// Items are keyed by CanonicalKey.
        /// </summary>
        public void MergeFloorScan(IEnumerable<ScannedItem> scannedItems)
        {
            string now = DateTime.UtcNow.ToString("o");

            foreach (var item in scannedItems)
            {
                if (string.IsNullOrWhiteSpace(item.CanonicalKey)) continue;

                if (!Cache.TryGetValue(item.CanonicalKey, out var entry))
                {
                    // Extract internal English name from canonical key (e.g. "Item:Stone" → "Stone")
                    string internalName = item.CanonicalKey;
                    int colonIdx = internalName.IndexOf(':');
                    if (colonIdx >= 0) internalName = internalName.Substring(colonIdx + 1);

                    entry = new CachedItemEntry
                    {
                        CanonicalKey    = item.CanonicalKey,
                        Name            = internalName,
                        Category        = item.Category,
                        QualifiedItemId = item.QualifiedItemId,
                        IsModded        = !MineItemDatabase.ByCanonicalKey.ContainsKey(item.CanonicalKey),
                    };
                    Cache[item.CanonicalKey] = entry;
                }
                entry.TimesFound += item.Count;
                entry.LastSeen    = now;
                entry.IsModded    = !MineItemDatabase.ByCanonicalKey.ContainsKey(item.CanonicalKey);
            }
        }

        // ── Save ──────────────────────────────────────────────────────────────────

        /// <summary>Saves the current cache to disk.</summary>
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
