using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrewedCode.Events;
using BrewedCode.Logging;
using UnityEditor;
using UnityEngine;

namespace BrewedCode.ItemHub
{
    /// <summary>
    /// EditorWindow to inspect and manipulate ItemHub commodities, monitor events
    /// and save/load snapshots as JSON. Dev-only tooling.
    /// </summary>
    public sealed class CargoBayDebuggerWindow : EditorWindow,
        EventListener<ItemHubEvents.CommodityAdded>,
        EventListener<ItemHubEvents.CommodityRemoved>,
        EventListener<ItemHubEvents.InstanceCreated>,
        EventListener<ItemHubEvents.InstanceUpdated>,
        EventListener<ItemHubEvents.InstanceDeleted>
    {
        private const int DefaultMaxLOG = 200;

        // UI state
        private string m_Search = "";
        private int m_IncIndex = 1;
        private readonly int[] m_Increments = { 1, 10, 100, 1000 };

        private string m_AddId = "";
        private int m_AddQty = 1;

        // ---- Category filter state ----
        private bool m_ShowZeros;

        private string[] m_AllCategories = Array.Empty<string>();
        private int m_CategoryMask = -1; // all on by default


        // Sorting modes for the commodity list
        private enum SortMode
        {
            NameAsc,
            NameDesc,
            CategoryThenName,
            CategoryDescThenName
        }

        private SortMode m_Sort = SortMode.CategoryThenName;

        // JSON load/save
        private string m_SavePath = "";
        private string m_LoadPath = "";
        private TextAsset? m_DragJson;

        // Data cache for rendering
        private readonly List<Entry> m_Entries = new();
        private Vector2 m_ScrollList;
        private Vector2 m_ScrollLog;

        // Event log
        private readonly List<string> m_LOG = new();
        private int m_MaxLog = DefaultMaxLOG;
        private ILog? _logger;

        // Helper model for list
        private struct Entry
        {
            public ItemId Id;
            public string Display;
            public string Category;
            public int Quantity;
        }

        [MenuItem("BrewedCode/ItemHub/Cargo Bay (Debug)")]
        public static void Open()
        {
            var w = GetWindow<CargoBayDebuggerWindow>("Cargo Bay (Debug)");
            w.minSize = new Vector2(520, 420);
            w.Focus();
        }

        private void OnEnable()
        {
            InitializeLogger();
            SubscribeEvents();
            BuildCategoryList();
            RefreshFromHub();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void InitializeLogger()
        {
            try
            {
                _logger = LoggingRoot.Instance.Service.GetLogger(nameof(CargoBayDebuggerWindow));
            }
            catch
            {
                _logger = null;
            }
        }

        private static ItemHubService? hubService => ItemHubRoot.Instance ? ItemHubRoot.Instance.Hub : null;
        private static IItemCatalog? catalog => ItemHubRoot.Instance ? ItemHubRoot.Instance.CatalogProvider : null;

        private void SubscribeEvents()
        {
            this.EventStartListening<ItemHubEvents.CommodityAdded>();
            this.EventStartListening<ItemHubEvents.CommodityRemoved>();
            this.EventStartListening<ItemHubEvents.InstanceCreated>();
            this.EventStartListening<ItemHubEvents.InstanceUpdated>();
            this.EventStartListening<ItemHubEvents.InstanceDeleted>();
        }

        private void UnsubscribeEvents()
        {
            this.EventStopListening<ItemHubEvents.CommodityAdded>();
            this.EventStopListening<ItemHubEvents.CommodityRemoved>();
            this.EventStopListening<ItemHubEvents.InstanceCreated>();
            this.EventStopListening<ItemHubEvents.InstanceUpdated>();
            this.EventStopListening<ItemHubEvents.InstanceDeleted>();
        }

        public void OnEvent(ItemHubEvents.CommodityAdded e)
        {
            AppendLog($"+ {e.ItemId} x{e.Delta} → {e.NewTotal}");
            RefreshFromHub();
            Repaint();
        }

        public void OnEvent(ItemHubEvents.CommodityRemoved e)
        {
            AppendLog($"- {e.ItemId} x{e.Delta} → {e.NewTotal}");
            RefreshFromHub();
            Repaint();
        }

        public void OnEvent(ItemHubEvents.InstanceCreated e)
        {
            AppendLog($"instance + {e.DefinitionId} [{e.InstanceId}]");
            Repaint();
        }

        public void OnEvent(ItemHubEvents.InstanceUpdated e)
        {
            AppendLog($"instance * {e.DefinitionId} [{e.InstanceId}]");
            Repaint();
        }

        public void OnEvent(ItemHubEvents.InstanceDeleted e)
        {
            AppendLog($"instance - {e.DefinitionId} [{e.InstanceId}]");
            Repaint();
        }

        private void AppendLog(string line)
        {
            var stamp = DateTime.Now.ToString("HH:mm:ss.fff");
            m_LOG.Add($"[{stamp}] {line}");
            if (m_LOG.Count > m_MaxLog) m_LOG.RemoveRange(0, m_LOG.Count - m_MaxLog);
            m_ScrollLog.y = 0f;
        }

        private void RefreshFromHub()
        {
            m_Entries.Clear();
            var hub = hubService;
            if (hub == null) return;

            // 1) snapshot → map ItemId → qty
            var quantities = new Dictionary<ItemId, int>();
            var snap = hub.GetSnapshot();

            foreach (var c in snap.commodities)
            {
                if (string.IsNullOrWhiteSpace(c.itemId)) continue;
                var id = new ItemId(c.itemId);
                quantities[id] = c.quantity;
            }


            // 2) build entries
            if (m_ShowZeros && catalog != null)
            {
                // Merge: all catalog items (qty default = 0) + override with snapshot quantities
                foreach (var meta in catalog.GetAllMetas())
                {
                    var id = meta.Id;
                    quantities.TryGetValue(id, out var qty);

                    var displayName = string.IsNullOrWhiteSpace(meta.DisplayName) ? id.Value : meta.DisplayName;
                    var category = string.IsNullOrWhiteSpace(meta.Category) ? "Uncategorized" : meta.Category;

                    m_Entries.Add(new Entry
                    {
                        Id = id,
                        Display = displayName,
                        Category = category,
                        Quantity = qty
                    });
                }
            }
            else
            {
                // Only present commodities (qty > 0)
                foreach (var kv in quantities)
                {
                    var id = kv.Key;
                    var qty = kv.Value;
                    if (qty <= 0) continue;

                    string displayName = id.Value;
                    string category = "Uncategorized";
                    if (catalog != null && catalog.TryGetMeta(id, out var meta))
                    {
                        displayName = string.IsNullOrWhiteSpace(meta.DisplayName) ? id.Value : meta.DisplayName;
                        category = string.IsNullOrWhiteSpace(meta.Category) ? "Uncategorized" : meta.Category;
                    }

                    m_Entries.Add(new Entry
                    {
                        Id = id,
                        Display = displayName,
                        Category = category,
                        Quantity = qty
                    });
                }
            }

            // 3) sort: Category → Name
            switch (m_Sort)
            {
                case SortMode.NameAsc:
                    m_Entries.Sort((a, b) => string.Compare(a.Display, b.Display, StringComparison.Ordinal));
                    break;

                case SortMode.NameDesc:
                    m_Entries.Sort((a, b) => string.Compare(b.Display, a.Display, StringComparison.Ordinal));
                    break;

                case SortMode.CategoryThenName:
                    m_Entries.Sort((a, b) =>
                    {
                        int c = string.Compare(a.Category, b.Category, StringComparison.Ordinal);
                        return c != 0 ? c : string.Compare(a.Display, b.Display, StringComparison.Ordinal);
                    });
                    break;

                case SortMode.CategoryDescThenName:
                    m_Entries.Sort((a, b) =>
                    {
                        int c = string.Compare(b.Category, a.Category, StringComparison.Ordinal);
                        return c != 0 ? c : string.Compare(a.Display, b.Display, StringComparison.Ordinal);
                    });
                    break;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            DrawToolbar();
            EditorGUILayout.Space(6);
            DrawCommodityList();
            EditorGUILayout.Space(6);
            DrawJsonSection();
            EditorGUILayout.Space(6);
            DrawEventLog();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                // search
                m_Search = EditorGUILayout.TextField(new GUIContent("Search"), m_Search);

                // increments
                EditorGUIUtility.labelWidth = 30f; // "Inc"
                m_IncIndex = EditorGUILayout.Popup(new GUIContent("Inc"), m_IncIndex,
                    m_Increments.Select(i => new GUIContent(i.ToString())).ToArray(), GUILayout.Width(120));

                if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                {
                    BuildCategoryList();
                    RefreshFromHub();
                }

                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                float oldLabel = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80f; // "Add by Id"

                m_AddId = EditorGUILayout.TextField(new GUIContent("Add by Id"), m_AddId, GUILayout.MinWidth(160),
                    GUILayout.ExpandWidth(true));

                EditorGUIUtility.labelWidth = 30f; // "Qty"
                m_AddQty = EditorGUILayout.IntField(new GUIContent("Qty"), m_AddQty,
                    GUILayout.Width(100)); // 30 label + 70 field aprox

                EditorGUIUtility.labelWidth = oldLabel;

                if (GUILayout.Button("Add", GUILayout.Width(72)))
                {
                    if (hubService != null && !string.IsNullOrWhiteSpace(m_AddId))
                    {
                        if (m_AddQty <= 0) m_AddQty = 1;
                        if (!hubService.AddCommodity(m_AddId, m_AddQty, out var err) && !string.IsNullOrEmpty(err))
                        {
                            _logger.WarningSafe(err);
                        }
                    }
                }
            }
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                bool newShow =
                    EditorGUILayout.ToggleLeft(new GUIContent("Show zeros"), m_ShowZeros, GUILayout.Width(110));
                if (newShow != m_ShowZeros)
                {
                    m_ShowZeros = newShow;
                    BuildCategoryList();
                    RefreshFromHub();
                }

                if (m_AllCategories.Length > 0)
                {
                    EditorGUIUtility.labelWidth = 70f; // "Categories"
                    m_CategoryMask = EditorGUILayout.MaskField(
                        new GUIContent("Categories"),
                        m_CategoryMask,
                        m_AllCategories,
                        GUILayout.Width(280)
                    );
                }
            }

            EditorGUIUtility.labelWidth = 32f; // "Sort"
            m_Sort = (SortMode)EditorGUILayout.EnumPopup(new GUIContent("Sort"), m_Sort, GUILayout.Width(220));
        }

        private void DrawCommodityList()
        {
            var hub = CargoBayDebuggerWindow.hubService;
            if (hub == null)
            {
                EditorGUILayout.HelpBox("ItemHubRoot not found in scene.", MessageType.Warning);
                return;
            }

            var showCategoryHeaders = m_Sort == SortMode.CategoryThenName || m_Sort == SortMode.CategoryDescThenName;

            using var scroll =
                new EditorGUILayout.ScrollViewScope(m_ScrollList, GUILayout.Height(position.height * 0.45f));
            m_ScrollList = scroll.scrollPosition;

            var filter = m_Search.Trim();
            var inc = m_Increments[Mathf.Clamp(m_IncIndex, 0, m_Increments.Length - 1)];

            string lastCategory = null;

            var entriesSnapshot = m_Entries.ToArray();
            foreach (var e in entriesSnapshot)
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    if (!(e.Display.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                          || e.Id.Value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                          || e.Category.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
                        continue;
                }

                if (!CategoryAllowed(e.Category))
                    continue;

                if (showCategoryHeaders && lastCategory != e.Category)
                {
                    if (lastCategory != null) EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField(e.Category, EditorStyles.boldLabel);
                    lastCategory = e.Category;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(new GUIContent(e.Display, e.Id.Value), GUILayout.MinWidth(180));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(e.Quantity.ToString(), GUILayout.Width(60));

                    if (GUILayout.Button($"+{inc}", GUILayout.Width(50)))
                    {
                        if (!hub.AddCommodity(e.Id, inc, out var err) && !string.IsNullOrEmpty(err))
                        {
                            _logger.WarningSafe(err);
                        }
                    }

                    if (GUILayout.Button($"-{inc}", GUILayout.Width(50)))
                    {
                        if (!hub.RemoveCommodity(e.Id, inc, out var err) && !string.IsNullOrEmpty(err))
                        {
                            _logger.WarningSafe(err);
                        }
                    }
                }
            }
        }

        private void DrawJsonSection()
        {
            EditorGUILayout.LabelField("Snapshot (JSON)", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Save
                using (new EditorGUILayout.HorizontalScope())
                {
                    m_SavePath = EditorGUILayout.TextField(new GUIContent("Save Path"), m_SavePath);
                    if (GUILayout.Button("...", GUILayout.Width(30)))
                    {
                        var p = EditorUtility.SaveFilePanel("Save ItemHub Snapshot", "", "CargoBaySnapshot.json",
                            "json");
                        if (!string.IsNullOrEmpty(p)) m_SavePath = p;
                    }

                    if (GUILayout.Button("Save", GUILayout.Width(70)))
                    {
                        TrySaveSnapshot(m_SavePath);
                    }
                }

                // Load by path
                using (new EditorGUILayout.HorizontalScope())
                {
                    m_LoadPath = EditorGUILayout.TextField(new GUIContent("Load Path"), m_LoadPath);
                    if (GUILayout.Button("...", GUILayout.Width(30)))
                    {
                        var p = EditorUtility.OpenFilePanel("Load ItemHub Snapshot", "", "json");
                        if (!string.IsNullOrEmpty(p)) m_LoadPath = p;
                    }

                    if (GUILayout.Button("Load", GUILayout.Width(70)))
                    {
                        TryLoadSnapshotFromPath(m_LoadPath);
                    }
                }

                // Drag & drop
                EditorGUIUtility.labelWidth = 85f; // "Or Drag JSON"
                m_DragJson = (TextAsset)EditorGUILayout.ObjectField(new GUIContent("Or Drag JSON"), m_DragJson,
                    typeof(TextAsset), false);
                if (m_DragJson)
                {
                    if (GUILayout.Button("Load From TextAsset", GUILayout.Width(180)))
                    {
                        TryLoadSnapshotFromTextAsset(m_DragJson);
                    }
                }
            }
        }

        private void DrawEventLog()
        {
            EditorGUILayout.LabelField("Event Log", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUIUtility.labelWidth = 30f; // "Max"
                m_MaxLog = EditorGUILayout.IntField(new GUIContent("Max"), m_MaxLog, GUILayout.Width(120));
                if (GUILayout.Button("Clear", GUILayout.Width(70)))
                    m_LOG.Clear();
                GUILayout.FlexibleSpace();
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(m_ScrollLog))
            {
                m_ScrollLog = scroll.scrollPosition;
                for (var i = m_LOG.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.LabelField(m_LOG[i]);
                }
            }
        }

        // -------- JSON helpers --------

        private void TrySaveSnapshot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.WarningSafe("Save path is empty.");
                return;
            }

            var hub = CargoBayDebuggerWindow.hubService;
            if (hub == null) return;

            var snap = hub.GetSnapshot();
            var json = JsonUtility.ToJson(snap, true);
            File.WriteAllText(path, json);
            _logger.InfoSafe($"Snapshot saved to {path}");
        }

        private void TryLoadSnapshotFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                _logger.WarningSafe("Load path invalid.");
                return;
            }

            var text = File.ReadAllText(path);
            TryLoadSnapshotFromJson(text);
        }

        private void TryLoadSnapshotFromTextAsset(TextAsset ta)
        {
            if (!ta) return;
            TryLoadSnapshotFromJson(ta.text);
        }

        private void TryLoadSnapshotFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                var snap = JsonUtility.FromJson<ItemHubSnapshot>(json);
                if (snap == null)
                {
                    _logger.WarningSafe("Failed to parse JSON into ItemHubSnapshot.");
                    return;
                }

                var hub = CargoBayDebuggerWindow.hubService;
                if (hub == null) return;

                hub.LoadSnapshot(snap);
                AppendLog("snapshot loaded");
                RefreshFromHub();
                Repaint();
            }
            catch (Exception ex)
            {
                _logger.ErrorSafe($"Error loading snapshot: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds the unique, sorted list of categories from the catalog.
        /// </summary>
        private void BuildCategoryList()
        {
            if (catalog == null)
            {
                m_AllCategories = Array.Empty<string>();
                m_CategoryMask = -1;
                return;
            }

            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var meta in catalog.GetAllMetas())
            {
                var cat = string.IsNullOrWhiteSpace(meta.Category) ? "Uncategorized" : meta.Category;
                set.Add(cat);
            }

            m_AllCategories = set.OrderBy(s => s, StringComparer.Ordinal).ToArray();

            // Turn all bits on for available categories (MaskField usa int; prático até 32 categorias)
            m_CategoryMask = (m_AllCategories.Length >= 32) ? ~0 : (1 << m_AllCategories.Length) - 1;
        }

        /// <summary>
        /// Returns true if this category is enabled in the mask.
        /// </summary>
        private bool CategoryAllowed(string category)
        {
            if (m_AllCategories.Length == 0) return true;
            var cat = string.IsNullOrWhiteSpace(category) ? "Uncategorized" : category;
            var idx = Array.IndexOf(m_AllCategories, cat);
            if (idx < 0) return false;
            return (m_CategoryMask & (1 << idx)) != 0;
        }
    }
}