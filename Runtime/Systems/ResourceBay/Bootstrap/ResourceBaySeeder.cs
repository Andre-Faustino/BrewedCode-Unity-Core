// BrewedCode/ResourceBay/ResourceBaySeeder.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using BrewedCode.Logging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrewedCode.ResourceBay
{
    /// <summary>
    /// Simple runtime/editor seeder for ResourceBay capacities.
    /// Useful for sandbox scenes and quick test setups.
    /// </summary>
    public sealed class ResourceBaySeeder : MonoBehaviour
    {
        [Serializable]
        public struct SeedEntry
        {
            [Tooltip("Resource key (e.g., water, energy, compute).")]
            public string key;

            [Min(0)]
            [Tooltip("Capacity to set for this resource.")]
            public long capacity;
        }

        public enum SeedMode
        {
            Upsert,  // Define/update each resource with the provided capacity
            Replace  // Replace the entire resource set via LoadSnapshot (requires no active allocations)
        }

        [Header("Seed Entries")]
        [Tooltip("List of (key, capacity) to seed into the ResourceBay.")]
        public List<SeedEntry> entries = new();

        [Header("Options")]
        public SeedMode mode = SeedMode.Upsert;

        [Tooltip("Automatically apply on Start().")]
        public bool applyOnStart = true;

        [Min(0f)]
        [Tooltip("Optional delay before applying the seed on Start(), seconds.")]
        public float delaySeconds = 0f;

        private IResourceBay? Service => ResourceBayRoot.Instance.Service;
        private ILog? _logger;

        private void Awake()
        {
            InitializeLogger();
        }

        private void InitializeLogger()
        {
            try
            {
                _logger = LoggingRoot.Instance.Service.GetLogger(nameof(ResourceBaySeeder));
            }
            catch
            {
                _logger = null;
            }
        }

        private bool Validate(out string error)
        {
            error = null;
            if (Service == null)
            {
                error = "[ResourceBaySeeder] ResourceBayRoot/Service not found in scene.";
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

            try
            {
                switch (mode)
                {
                    case SeedMode.Upsert:
                        ApplyUpsert();
                        break;
                    case SeedMode.Replace:
                        ApplyReplace();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorSafe($"Apply failed: {ex.Message}");
            }
        }

        [ContextMenu("Clear All Resources")]
        public void ClearAllResources()
        {
            if (!Validate(out var error))
            {
                _logger.WarningSafe(error);
                return;
            }

            try
            {
                // Will throw if there are active allocations (by design).
                var empty = new ResourceBaySnapshot();
                Service.LoadSnapshot(empty);
                _logger.InfoSafe("All resources cleared via empty snapshot.");
            }
            catch (Exception ex)
            {
                _logger.ErrorSafe($"ClearAllResources failed: {ex.Message}");
            }
        }

        [ContextMenu("Capture From Bay (Capacities)")]
        private void CaptureFromBay()
        {
            if (!Validate(out var error))
            {
                _logger.WarningSafe(error);
                return;
            }

#if UNITY_EDITOR
            Undo.RecordObject(this, "Capture ResourceBay Seed From Bay");
#endif

            try
            {
                entries.Clear();

                // Prefer totals if available (capacity is explicit). Fallback to snapshot.
                Dictionary<string, ResourceTotals> totals = null;
                try { totals = Service.GetTotals(); } catch { /* older service without GetTotals */ }

                if (totals != null)
                {
                    foreach (var kv in totals)
                    {
                        entries.Add(new SeedEntry { key = kv.Key, capacity = Math.Max(0, kv.Value.Capacity) });
                    }
                }
                else
                {
                    var snap = Service.GetSnapshot();
                    if (snap?.resources != null)
                    {
                        foreach (var r in snap.resources)
                        {
                            if (string.IsNullOrWhiteSpace(r.key)) continue;
                            entries.Add(new SeedEntry { key = r.key, capacity = Math.Max(0, r.capacity) });
                        }
                    }
                }

                entries.Sort((a, b) => string.Compare(a.key, b.key, StringComparison.Ordinal));

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif

                _logger.InfoSafe($"Captured {entries.Count} resources from current bay.");
            }
            catch (Exception ex)
            {
                _logger.ErrorSafe($"Capture failed: {ex.Message}");
            }
        }

        private void ApplyUpsert()
        {
            int applied = 0;
            foreach (var e in entries)
            {
                if (string.IsNullOrWhiteSpace(e.key) || e.capacity < 0)
                    continue;

                Service.DefineResource(e.key.Trim(), e.capacity);
                applied++;
            }
            _logger.InfoSafe($"Upsert seed applied. Entries processed: {applied}.");
        }

        private void ApplyReplace()
        {
            // Replace the entire resource set. This will fail if there are active allocations.
            var snap = new ResourceBaySnapshot();

            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (string.IsNullOrWhiteSpace(e.key) || e.capacity < 0)
                        continue;

                    snap.resources.Add(new ResourceBaySnapshot.ResourceEntry
                    {
                        key = e.key.Trim(),
                        capacity = e.capacity,
                        allocatedTotal = 0 // fresh replace ignores previous allocations
                    });
                }
            }

            Service.LoadSnapshot(snap);
            _logger.InfoSafe($"Replace seed applied. Resources set to {snap.resources.Count} entries.");
        }

        private void Start()
        {
            if (!applyOnStart) return;
            if (delaySeconds <= 0f) ApplyNow();
            else Invoke(nameof(ApplyNow), delaySeconds);
        }
    }
}
