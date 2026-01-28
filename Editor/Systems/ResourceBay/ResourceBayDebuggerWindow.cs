// Assets/YourFolder/Editor/ResourceBayDebuggerWindow.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using BrewedCode.Logging;
using UnityEditor;
using UnityEngine;

namespace BrewedCode.ResourceBay
{
    /// <summary>
    /// Dev-only window to inspect and manipulate ResourceBay state.
    /// - View and edit resource capacities
    /// - List and release allocations
    /// - Reset all allocations
    /// </summary>
    public sealed class ResourceBayDebuggerWindow : EditorWindow
    {
        private const float TABLE_ROW_HEIGHT = 22f;

        // UI state
        private string _resourceSearch = "";
        private string _newKey = "";
        private long _newCapacity = 0;
        private int _incIndex = 1;
        private readonly long[] _increments = { 1, 10, 100, 1000, 10000 };

        private string _allocOwnerFilter = "";
        private string _releaseOwner = "";

        private Vector2 _scrollResources;
        private Vector2 _scrollAllocations;

        // Cache
        private List<ResourceRow> _rows = new();
        private List<AllocationInfo> _allocs = new();
        private ILog? _logger;

        private struct ResourceRow
        {
            public string Key;
            public long Capacity;
            public long Allocated;
            public long Available;
        }

        [MenuItem("BrewedCode/ResourceBay/Resource Bay (Debug)")]
        public static void Open()
        {
            var w = GetWindow<ResourceBayDebuggerWindow>("Resource Bay (Debug)");
            w.minSize = new Vector2(680, 480);
            w.Focus();
            w.RefreshAll();
        }

        private void OnEnable()
        {
            InitializeLogger();
            RefreshAll();
        }

        private void InitializeLogger()
        {
            try
            {
                _logger = LoggingRoot.Instance.Service.GetLogger(nameof(ResourceBayDebuggerWindow));
            }
            catch
            {
                _logger = null;
            }
        }

        private static IResourceBay? GetBay() => ResourceBayRoot.Instance ? ResourceBayRoot.Instance.Service : null;

        private void RefreshAll()
        {
            var bay = GetBay();
            _rows.Clear();
            _allocs.Clear();
            if (bay == null) return;

            try
            {
                var totals = bay.GetTotals();
                foreach (var kv in totals.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    _rows.Add(new ResourceRow
                    {
                        Key = kv.Key,
                        Capacity = kv.Value.Capacity,
                        Allocated = kv.Value.Allocated,
                        Available = kv.Value.Available
                    });
                }

                _allocs = bay.GetAllAllocations()
                             .OrderByDescending(a => a.CreatedUtc)
                             .ToList();
            }
            catch (Exception ex)
            {
                _logger.ErrorSafe($"Refresh failed: {ex.Message}");
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);
            DrawResourcesTable();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Allocations", EditorStyles.boldLabel);
            DrawAllocations();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Search resources
                    EditorGUIUtility.labelWidth = 55f;
                    _resourceSearch = EditorGUILayout.TextField(new GUIContent("Search"), _resourceSearch, GUILayout.MinWidth(160));

                    // Increment popup
                    EditorGUIUtility.labelWidth = 26f;
                    _incIndex = EditorGUILayout.Popup(new GUIContent("Inc"), _incIndex,
                        _increments.Select(i => new GUIContent(i.ToString())).ToArray(),
                        GUILayout.Width(120));

                    if (GUILayout.Button("Refresh", GUILayout.Width(90)))
                        RefreshAll();

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space(2);

                using (new EditorGUILayout.HorizontalScope())
                {
                    // New resource
                    EditorGUIUtility.labelWidth = 28f;
                    _newKey = EditorGUILayout.TextField(new GUIContent("Key"), _newKey, GUILayout.MinWidth(180));
                    EditorGUIUtility.labelWidth = 60f;
                    _newCapacity = EditorGUILayout.LongField(new GUIContent("Capacity"), _newCapacity, GUILayout.Width(200));

                    if (GUILayout.Button("Define/Update", GUILayout.Width(120)))
                    {
                        var bay = GetBay();
                        if (bay != null && !string.IsNullOrWhiteSpace(_newKey))
                        {
                            try
                            {
                                bay.DefineResource(_newKey.Trim(), _newCapacity);
                                RefreshAll();
                            }
                            catch (Exception ex)
                            {
                                _logger.ErrorSafe($"DefineResource('{_newKey}') failed: {ex.Message}");
                            }
                        }
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Reset All Allocations", GUILayout.Width(180)))
                    {
                        var bay = GetBay();
                        if (bay != null)
                        {
                            try
                            {
                                bay.ResetAllAllocations();
                                RefreshAll();
                            }
                            catch (Exception ex)
                            {
                                _logger.ErrorSafe($"ResetAllAllocations failed: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private void DrawResourcesTable()
        {
            var bay = GetBay();
            if (bay == null)
            {
                EditorGUILayout.HelpBox("ResourceBayRoot/Service not found in scene.", MessageType.Warning);
                return;
            }

            var inc = _increments[Mathf.Clamp(_incIndex, 0, _increments.Length - 1)];
            var filter = _resourceSearch?.Trim() ?? "";

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollResources, GUILayout.Height(position.height * 0.45f)))
            {
                _scrollResources = scroll.scrollPosition;

                // Header
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Key", EditorStyles.boldLabel, GUILayout.MinWidth(200));
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Capacity", EditorStyles.boldLabel, GUILayout.Width(90));
                    GUILayout.Label("Allocated", EditorStyles.boldLabel, GUILayout.Width(90));
                    GUILayout.Label("Available", EditorStyles.boldLabel, GUILayout.Width(90));
                    GUILayout.Label(" ", GUILayout.Width(190)); // buttons space
                }

                EditorGUILayout.Space(2);

                foreach (var r in _rows.ToArray())
                {
                    if (!string.IsNullOrEmpty(filter) &&
                        r.Key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Key
                        EditorGUILayout.LabelField(r.Key, GUILayout.MinWidth(200));
                        GUILayout.FlexibleSpace();

                        // Numbers
                        EditorGUILayout.LabelField(r.Capacity.ToString(), GUILayout.Width(90));
                        EditorGUILayout.LabelField(r.Allocated.ToString(), GUILayout.Width(90));
                        EditorGUILayout.LabelField(r.Available.ToString(), GUILayout.Width(90));

                        // Buttons
                        if (GUILayout.Button($"+{inc}", GUILayout.Width(60)))
                        {
                            try { bay.AdjustCapacity(r.Key, inc); RefreshAll(); }
                            catch (Exception ex)
                            {
                                _logger.ErrorSafe($"AdjustCapacity('{r.Key}', +{inc}) failed: {ex.Message}");
                            }
                        }
                        if (GUILayout.Button($"-{inc}", GUILayout.Width(60)))
                        {
                            try { bay.AdjustCapacity(r.Key, -inc); RefreshAll(); }
                            catch (Exception ex)
                            {
                                _logger.ErrorSafe($"AdjustCapacity('{r.Key}', -{inc}) failed: {ex.Message}");
                            }
                        }
                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            var confirm = EditorUtility.DisplayDialog("Remove resource?",
                                $"Remove resource '{r.Key}'?\n\nNote: removal is blocked if there are active allocations referencing this key.",
                                "Remove", "Cancel");
                            if (confirm)
                            {
                                try
                                {
                                    if (!bay.RemoveResource(r.Key))
                                    {
                                        _logger.WarningSafe($"RemoveResource('{r.Key}') returned false (key may not exist).");
                                    }
                                    RefreshAll();
                                }
                                catch (Exception ex)
                                {
                                    _logger.ErrorSafe($"RemoveResource('{r.Key}') failed: {ex.Message}");
                                }
                            }
                        }
                    }

                    GUILayout.Space(2);
                }
            }
        }

        private void DrawAllocations()
        {
            var bay = GetBay();
            if (bay == null)
            {
                EditorGUILayout.HelpBox("ResourceBayRoot/Service not found in scene.", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUIUtility.labelWidth = 48f;
                    _allocOwnerFilter = EditorGUILayout.TextField(new GUIContent("Owner"), _allocOwnerFilter, GUILayout.MinWidth(160));
                    if (GUILayout.Button("Refresh", GUILayout.Width(90)))
                        RefreshAll();

                    GUILayout.FlexibleSpace();

                    EditorGUIUtility.labelWidth = 90f;
                    _releaseOwner = EditorGUILayout.TextField(new GUIContent("Release Owner"), _releaseOwner, GUILayout.MinWidth(200));
                    if (GUILayout.Button("Release By Owner", GUILayout.Width(140)))
                    {
                        if (!string.IsNullOrWhiteSpace(_releaseOwner))
                        {
                            try { bay.ReleaseByOwner(_releaseOwner.Trim()); RefreshAll(); }
                            catch (Exception ex)
                            {
                                _logger.ErrorSafe($"ReleaseByOwner('{_releaseOwner}') failed: {ex.Message}");
                            }
                        }
                    }
                }

                EditorGUILayout.Space(4);

                using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollAllocations, GUILayout.Height(position.height * 0.30f)))
                {
                    _scrollAllocations = scroll.scrollPosition;

                    foreach (var a in _allocs)
                    {
                        if (!string.IsNullOrEmpty(_allocOwnerFilter) &&
                            !((a.OwnerId ?? "").Contains(_allocOwnerFilter, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.SelectableLabel($"Id: {a.AllocationId}", GUILayout.Height(TABLE_ROW_HEIGHT));
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Release", GUILayout.Width(80)))
                                {
                                    try { bay.Release(a.AllocationId); RefreshAll(); }
                                    catch (Exception ex)
                                    {
                                        _logger.ErrorSafe($"Release('{a.AllocationId}') failed: {ex.Message}");
                                    }
                                }
                            }
                            
                            EditorGUILayout.LabelField($"Owner: {a.OwnerId ?? "(null)"}", GUILayout.Width(260));
                            EditorGUILayout.LabelField(a.CreatedUtc.ToString("u"), GUILayout.Width(180));

                            // Map
                            if (a.Resources != null && a.Resources.Count > 0)
                            {
                                foreach (var kv in a.Resources)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        EditorGUILayout.LabelField(kv.Key, GUILayout.MinWidth(200));
                                        GUILayout.FlexibleSpace();
                                        EditorGUILayout.LabelField(kv.Value.ToString(), GUILayout.Width(100));
                                    }
                                }
                            }

                            // Tags / Context
                            string tags = (a.Tags != null && a.Tags.Count > 0) ? string.Join(", ", a.Tags) : "";
                            if (!string.IsNullOrEmpty(tags) || !string.IsNullOrEmpty(a.Context))
                            {
                                EditorGUILayout.LabelField($"Tags: {tags}");
                                if (!string.IsNullOrEmpty(a.Context))
                                    EditorGUILayout.LabelField($"Context: {a.Context}");
                            }
                        }
                    }
                }
            }
        }
    }
}
