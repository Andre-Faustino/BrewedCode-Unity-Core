using System;
using System.Collections.Generic;
using UnityEngine;
using BrewedCode.Logging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrewedCode.ItemHub
{
    /// <summary>
    /// Simple runtime seeder for ItemHub commodities.
    /// Useful for sandbox scenes and quick test setups.
    /// </summary>
    public sealed class ItemHubSeeder : MonoBehaviour
    {
        [Serializable]
        public struct SeedEntry
        {
            [Tooltip("Item definition id (e.g., atom.fe, material.steel).")]
            public string itemId;

            [Min(1)]
            [Tooltip("Quantity to add or to set when Replace mode is used.")]
            public int quantity;
        }

        public enum SeedMode
        {
            Additive, // Adds quantities on top of existing cargo
            Replace // Replaces current commodities with exactly these entries (instances untouched)
        }

        [Header("Seed Entries")]
        [Tooltip("List of (itemId, quantity) to seed into the hub.")]
        public List<SeedEntry> entries = new();

        [Header("Options")]
        public SeedMode mode = SeedMode.Additive;

        [Tooltip("Automatically apply on Start().")]
        public bool applyOnStart = true;

        [Min(0f)]
        [Tooltip("Optional delay before applying the seed on Start(), seconds.")]
        public float delaySeconds;

        [Header("Safety")]
        [Tooltip("When Replace mode is selected, clear commodities even if 'entries' is empty.")]
        public bool allowEmptyReplace;

        private ItemHubService? Hub => ItemHubRoot.Instance != null ? ItemHubRoot.Instance.Hub : null;
        private ILog? _logger;

        private void Awake()
        {
            InitializeLogger();
        }

        private void InitializeLogger()
        {
            try
            {
                _logger = LoggingRoot.Instance.Service.GetLogger(nameof(ItemHubSeeder));
            }
            catch
            {
                _logger = null;
            }
        }

        private bool Validate(out string error)
        {
            error = null;
            if (Hub == null)
            {
                error = "[ItemHubSeeder] ItemHubRoot/HUB not found in scene.";
                return false;
            }

            return true;
        }

        [ContextMenu("Apply Now")]
        public void ApplyNow()
        {
            if (!Validate(out var error))
            {
                _logger.WarningSafe(error);
                return;
            }

            switch (mode)
            {
                case SeedMode.Additive:
                    ApplyAdditive();
                    break;
                case SeedMode.Replace:
                    ApplyReplace();
                    break;
            }
        }

        [ContextMenu("Clear Commodities")]
        public void ClearCommodities()
        {
            if (!Validate(out var error))
            {
                _logger.WarningSafe(error);
                return;
            }

            // Build an empty snapshot that preserves instances but clears commodities
            var empty = new ItemHubSnapshot();
            empty.instances = Hub.GetSnapshot().instances; // preserve current instances as-is
            Hub.LoadSnapshot(empty);
            _logger.InfoSafe("Commodities cleared via empty snapshot.");
        }


        // ---- Context Menus ----

        [ContextMenu("Capture From Hub (Commodities)")]
        private void CaptureFromHub_Commodities()
        {
            if (!Validate(out var error))
            {
                _logger.WarningSafe(error);
                return;
            }

#if UNITY_EDITOR
            Undo.RecordObject(this, "Capture ItemHub Seed From Hub");
#endif

            var snap = Hub.GetSnapshot();
            entries.Clear();

            if (snap?.commodities != null)
            {
                foreach (var c in snap.commodities)
                {
                    if (string.IsNullOrWhiteSpace(c.itemId))
                        continue;

                    // capture exactly what's in the hub (including zeros)
                    entries.Add(new SeedEntry { itemId = c.itemId, quantity = Mathf.Max(0, c.quantity) });
                }
            }

            // deterministic presentation: sort by id
            entries.Sort((a, b) => string.Compare(a.itemId, b.itemId, StringComparison.Ordinal));

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif

            _logger.InfoSafe($"Captured {entries.Count} entries from Hub snapshot.");
        }

        [ContextMenu("Capture From Catalog (Zeros)")]
        private void CaptureFromCatalog_Zeros()
        {
            if (!Validate(out var error))
            {
                _logger.WarningSafe(error);
                return;
            }

            var root = ItemHubRoot.Instance;
            if (root == null || root.CatalogProvider == null)
            {
                _logger.WarningSafe("CatalogProvider not found on ItemHubRoot.");
                return;
            }

#if UNITY_EDITOR
            Undo.RecordObject(this, "Capture ItemHub Seed From Catalog");
#endif

            entries.Clear();

            foreach (var meta in root.CatalogProvider.GetAllMetas())
            {
                if (meta == null || meta.Id.Value == null)
                    continue;

                entries.Add(new SeedEntry { itemId = meta.Id.Value, quantity = 0 });
            }

            entries.Sort((a, b) => string.Compare(a.itemId, b.itemId, StringComparison.Ordinal));

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif

            _logger.InfoSafe($"Captured {entries.Count} entries from Catalog (zeros).");
        }


        private void ApplyAdditive()
        {
            int applied = 0;
            foreach (var e in entries)
            {
                if (string.IsNullOrWhiteSpace(e.itemId) || e.quantity <= 0)
                    continue;

                if (!Hub.AddCommodity(e.itemId, e.quantity, out var err) && !string.IsNullOrEmpty(err))
                {
                    _logger.WarningSafe($"{err} (id='{e.itemId}', qty={e.quantity})");
                    continue;
                }

                applied++;
            }

            _logger.InfoSafe($"Additive seed applied. Entries processed: {applied}.");
        }

        private void ApplyReplace()
        {
            // Build a snapshot containing only the configured commodities; preserve instances.
            var current = Hub.GetSnapshot();

            if ((entries == null || entries.Count == 0) && !allowEmptyReplace)
            {
                _logger.WarningSafe("Replace mode requested with empty 'entries'. Nothing done (set 'allowEmptyReplace' to true to force clearing).");
                return;
            }

            var snap = new ItemHubSnapshot();
            // Preserve instances from current snapshot
            if (current.instances != null)
            {
                snap.instances.AddRange(current.instances);
            }

            // Insert commodities from entries
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (string.IsNullOrWhiteSpace(e.itemId) || e.quantity <= 0)
                        continue;

                    snap.commodities.Add(new ItemHubSnapshot.CommodityEntry
                    {
                        itemId = e.itemId,
                        quantity = e.quantity
                    });
                }
            }

            Hub.LoadSnapshot(snap);
            _logger.InfoSafe($"Replace seed applied. Commodities set to {snap.commodities.Count} entries; instances preserved: {snap.instances.Count}.");
        }

        private void Start()
        {
            if (!applyOnStart) return;

            if (delaySeconds <= 0f) ApplyNow();
            else Invoke(nameof(ApplyNow), delaySeconds);
        }
    }
}