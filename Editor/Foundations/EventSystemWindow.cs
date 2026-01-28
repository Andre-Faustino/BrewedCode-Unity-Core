using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BrewedCode.Events.Editor
{
    /// <summary>
    /// Editor window for inspecting and debugging the Event System.
    /// Shows active channels, dispatch history with filtering, and event details.
    /// </summary>
    public sealed class EventSystemWindow : EditorWindow
    {
        private enum Tab
        {
            Channels,
            History,
            Inspector
        }

        private Tab m_CurrentTab = Tab.Channels;

        // Filter state
        private string m_EventTypeFilter = "";
        private bool m_FilterGlobal = false;
        private bool m_FilterScene = false;
        private bool m_FilterInstance = false;

        // Multi-select event type filter
        private bool m_ShowEventTypeFilter = false;
        private readonly HashSet<Type> m_SelectedEventTypes = new();

        // Scroll positions
        private Vector2 m_ChannelScroll;
        private Vector2 m_HistoryScroll;
        private Vector2 m_InspectorScroll;
        private Vector2 m_DetailScroll;
        private Vector2 m_EventTypeFilterScroll;

        // Cached data
        private readonly List<ChannelEntry> m_Channels = new();
        private double m_LastRefresh;
        private const double RefreshInterval = 0.5;

        // Selected record for detail view
        private int m_SelectedRecordIndex = -1;
        private EventDispatchRecord? m_SelectedRecord;

        // Selected GameObject for Inspector tab
        private GameObject? m_SelectedObject;

        // Layout constants
        private const float DetailPanelHeight = 200f;

        private struct ChannelEntry
        {
            public ChannelKey Key;
            public string EventTypeName;
            public string ScopeName;
            public int ListenerCount;
            public int DispatchCount;
        }

        [MenuItem("BrewedCode/Events")]
        public static void Open()
        {
            var window = GetWindow<EventSystemWindow>("Events");
            window.minSize = new Vector2(700, 500);
            window.Focus();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            RefreshChannelList();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            m_SelectedObject = Selection.activeGameObject;
            Repaint();
        }

        private void OnGUI()
        {
            // Auto refresh
            if (EditorApplication.timeSinceStartup - m_LastRefresh > RefreshInterval)
            {
                RefreshChannelList();
                m_LastRefresh = EditorApplication.timeSinceStartup;
            }

            DrawToolbar();
            DrawTabs();
            EditorGUILayout.Space(4);

            switch (m_CurrentTab)
            {
                case Tab.Channels:
                    DrawChannelsTab();
                    break;
                case Tab.History:
                    DrawHistoryTab();
                    break;
                case Tab.Inspector:
                    DrawInspectorTab();
                    break;
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Capture toggle
                var wasCapturing = EventDebugCapture.IsCapturing;
                var captureStyle = new GUIStyle(EditorStyles.toolbarButton);
                if (wasCapturing)
                {
                    captureStyle.normal.textColor = Color.red;
                }

                EventDebugCapture.IsCapturing = GUILayout.Toggle(
                    wasCapturing,
                    wasCapturing ? "● REC" : "○ REC",
                    captureStyle,
                    GUILayout.Width(50));

                GUILayout.Space(8);

                // Search filter
                EditorGUIUtility.labelWidth = 40;
                m_EventTypeFilter = EditorGUILayout.TextField("Filter", m_EventTypeFilter,
                    EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));

                GUILayout.Space(4);

                // Event type multi-select filter toggle
                var filterActive = m_SelectedEventTypes.Count > 0;
                var filterButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
                if (filterActive)
                {
                    filterButtonStyle.fontStyle = FontStyle.Bold;
                }

                var filterLabel = filterActive ? $"Types ({m_SelectedEventTypes.Count})" : "Types ▼";
                if (GUILayout.Button(filterLabel, filterButtonStyle, GUILayout.MinWidth(90)))
                {
                    m_ShowEventTypeFilter = !m_ShowEventTypeFilter;
                }

                GUILayout.Space(8);

                // Scope filter buttons
                m_FilterGlobal = GUILayout.Toggle(m_FilterGlobal, "Global",
                    EditorStyles.toolbarButton, GUILayout.Width(50));
                m_FilterScene = GUILayout.Toggle(m_FilterScene, "Scene",
                    EditorStyles.toolbarButton, GUILayout.Width(45));
                m_FilterInstance = GUILayout.Toggle(m_FilterInstance, "Instance",
                    EditorStyles.toolbarButton, GUILayout.Width(55));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(45)))
                {
                    EventDebugCapture.ClearRecords();
                    m_SelectedRecord = null;
                    m_SelectedRecordIndex = -1;
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(55)))
                {
                    RefreshChannelList();
                }
            }

            // Event type filter dropdown
            if (m_ShowEventTypeFilter)
            {
                DrawEventTypeFilterDropdown();
            }
        }

        private void DrawEventTypeFilterDropdown()
        {
            var knownTypes = EventDebugCapture.GetKnownEventTypes().OrderBy(t => t.Name).ToList();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox,
                       GUILayout.MinHeight(220),
                       GUILayout.ExpandHeight(false)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Filter by Event Type:", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("All", GUILayout.Width(40)))
                    {
                        m_SelectedEventTypes.Clear();
                        foreach (var type in knownTypes)
                            m_SelectedEventTypes.Add(type);
                        ApplyEventTypeFilter();
                    }

                    if (GUILayout.Button("None", GUILayout.Width(45)))
                    {
                        m_SelectedEventTypes.Clear();
                        ApplyEventTypeFilter();
                    }

                    if (GUILayout.Button("Close", GUILayout.Width(45)))
                    {
                        m_ShowEventTypeFilter = false;
                    }
                }

                if (knownTypes.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "No event types have been captured yet. Start recording to see event types.", MessageType.Info);
                }
                else
                {
                    var maxHeight = Mathf.Min(150, knownTypes.Count * 20 + 10);
                    using (var scroll =
                           new EditorGUILayout.ScrollViewScope(m_EventTypeFilterScroll, GUILayout.MaxHeight(maxHeight)))
                    {
                        m_EventTypeFilterScroll = scroll.scrollPosition;

                        foreach (var type in knownTypes)
                        {
                            var isSelected = m_SelectedEventTypes.Contains(type);
                            var newSelected = EditorGUILayout.ToggleLeft(type.Name, isSelected);

                            if (newSelected != isSelected)
                            {
                                if (newSelected)
                                    m_SelectedEventTypes.Add(type);
                                else
                                    m_SelectedEventTypes.Remove(type);

                                ApplyEventTypeFilter();
                            }
                        }
                    }
                }
            }
        }

        private void ApplyEventTypeFilter()
        {
            EventDebugCapture.SetEnabledEventTypes(m_SelectedEventTypes);
        }

        private void DrawTabs()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(m_CurrentTab == Tab.Channels, "Channels", EditorStyles.toolbarButton))
                    m_CurrentTab = Tab.Channels;
                if (GUILayout.Toggle(m_CurrentTab == Tab.History, "History", EditorStyles.toolbarButton))
                    m_CurrentTab = Tab.History;
                if (GUILayout.Toggle(m_CurrentTab == Tab.Inspector, "Inspector", EditorStyles.toolbarButton))
                    m_CurrentTab = Tab.Inspector;
            }
        }

        private void DrawChannelsTab()
        {
            EditorGUILayout.LabelField("Active Event Channels", EditorStyles.boldLabel);

            // Table header
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Event Type", EditorStyles.boldLabel, GUILayout.MinWidth(180));
                GUILayout.Label("Scope", EditorStyles.boldLabel, GUILayout.Width(120));
                GUILayout.Label("Listeners", EditorStyles.boldLabel, GUILayout.Width(60));
                GUILayout.Label("Dispatches", EditorStyles.boldLabel, GUILayout.Width(70));
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(m_ChannelScroll))
            {
                m_ChannelScroll = scroll.scrollPosition;

                var filtered = GetFilteredChannels();

                foreach (var entry in filtered)
                {
                    DrawChannelRow(entry);
                }

                if (filtered.Count == 0)
                {
                    EditorGUILayout.HelpBox("No active channels match the filter.", MessageType.Info);
                }
            }
        }

        private void DrawChannelRow(ChannelEntry entry)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(entry.EventTypeName, GUILayout.MinWidth(180));

                var scopeStyle = new GUIStyle(EditorStyles.label);
                switch (entry.Key.ScopeKey.Type)
                {
                    case EventScopeType.Global:
                        scopeStyle.normal.textColor = new Color(0.4f, 0.7f, 1f);
                        break;
                    case EventScopeType.Scene:
                        scopeStyle.normal.textColor = new Color(0.4f, 1f, 0.4f);
                        break;
                    case EventScopeType.Instance:
                        scopeStyle.normal.textColor = new Color(1f, 0.8f, 0.4f);
                        break;
                }

                GUILayout.Label(entry.ScopeName, scopeStyle, GUILayout.Width(120));
                GUILayout.Label(entry.ListenerCount.ToString(), GUILayout.Width(60));
                GUILayout.Label(entry.DispatchCount.ToString(), GUILayout.Width(70));
            }
        }

        private void DrawHistoryTab()
        {
            if (!EventDebugCapture.IsCapturing)
            {
                EditorGUILayout.HelpBox("Capture is disabled. Click 'REC' to start recording events.",
                    MessageType.Warning);
            }

            var records = EventDebugCapture.GetRecords();
            var filteredRecords = GetFilteredRecords(records);

            EditorGUILayout.LabelField($"Events: {filteredRecords.Count} / {records.Count}", EditorStyles.miniLabel);

            // Calculate available height for the list (minus detail panel if selected)
            var listHeight = position.height - 140;
            if (m_SelectedRecord.HasValue)
            {
                listHeight -= DetailPanelHeight;
            }

            // Table header
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Frame", EditorStyles.boldLabel, GUILayout.Width(55));
                GUILayout.Label("Time", EditorStyles.boldLabel, GUILayout.Width(65));
                GUILayout.Label("Event Type", EditorStyles.boldLabel, GUILayout.MinWidth(150));
                GUILayout.Label("Scope", EditorStyles.boldLabel, GUILayout.Width(80));
                GUILayout.Label("Lst", EditorStyles.boldLabel, GUILayout.Width(30));
            }

            // History list
            using (var scroll = new EditorGUILayout.ScrollViewScope(m_HistoryScroll, GUILayout.Height(listHeight)))
            {
                m_HistoryScroll = scroll.scrollPosition;

                for (int i = filteredRecords.Count - 1; i >= 0; i--)
                {
                    var (record, originalIndex) = filteredRecords[i];
                    DrawHistoryRow(record, originalIndex);
                }

                if (filteredRecords.Count == 0)
                {
                    EditorGUILayout.HelpBox("No events match the current filter.", MessageType.Info);
                }
            }

            // Detail panel
            if (m_SelectedRecord.HasValue)
            {
                DrawDetailPanel(m_SelectedRecord.Value);
            }
        }

        private List<(EventDispatchRecord Record, int OriginalIndex)> GetFilteredRecords(
            IReadOnlyList<EventDispatchRecord> records)
        {
            var result = new List<(EventDispatchRecord, int)>();

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];

                // Text filter
                if (!string.IsNullOrEmpty(m_EventTypeFilter))
                {
                    if (!record.EventType.Name.Contains(m_EventTypeFilter, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                // Event type multi-select filter
                if (m_SelectedEventTypes.Count > 0 && !m_SelectedEventTypes.Contains(record.EventType))
                    continue;

                // Scope filter
                if (!PassesScopeFilter(record.ScopeKey))
                    continue;

                result.Add((record, i));
            }

            return result;
        }

        private void DrawHistoryRow(EventDispatchRecord record, int index)
        {
            var isSelected = m_SelectedRecordIndex == index;
            var rowStyle = isSelected ? EditorStyles.selectionRect : GUIStyle.none;

            var rect = EditorGUILayout.BeginHorizontal(rowStyle);

            // Make the whole row clickable
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                if (m_SelectedRecordIndex == index)
                {
                    // Deselect if clicking on already selected
                    m_SelectedRecordIndex = -1;
                    m_SelectedRecord = null;
                }
                else
                {
                    m_SelectedRecordIndex = index;
                    m_SelectedRecord = record;
                }

                Event.current.Use();
                Repaint();
            }

            // Frame
            GUILayout.Label($"{record.FrameCount}", EditorStyles.miniLabel, GUILayout.Width(55));

            // Timestamp (seconds since start)
            GUILayout.Label($"{record.Timestamp:F2}s", EditorStyles.miniLabel, GUILayout.Width(65));

            // Event type name
            var typeStyle = new GUIStyle(EditorStyles.label);
            if (isSelected) typeStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label(record.EventType.Name, typeStyle, GUILayout.MinWidth(150));

            // Scope (abbreviated)
            var scopeName = EventDebugCapture.GetScopeName(record.ScopeKey);
            var scopeAbbrev = record.ScopeKey.Type switch
            {
                EventScopeType.Global => "G",
                EventScopeType.Scene => "S",
                EventScopeType.Instance => "I",
                _ => "?"
            };
            var scopeStyle = new GUIStyle(EditorStyles.miniLabel);
            scopeStyle.normal.textColor = record.ScopeKey.Type switch
            {
                EventScopeType.Global => new Color(0.4f, 0.7f, 1f),
                EventScopeType.Scene => new Color(0.4f, 1f, 0.4f),
                EventScopeType.Instance => new Color(1f, 0.8f, 0.4f),
                _ => Color.white
            };
            GUILayout.Label($"{scopeAbbrev}", scopeStyle, GUILayout.Width(80));

            // Listeners notified
            GUILayout.Label($"{record.ListenersNotified}", EditorStyles.miniLabel, GUILayout.Width(30));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDetailPanel(EventDispatchRecord record)
        {
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(DetailPanelHeight)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Event Details", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Copy JSON", GUILayout.Width(80)))
                    {
                        EditorGUIUtility.systemCopyBuffer = record.EventDataJson;
                    }

                    if (GUILayout.Button("×", GUILayout.Width(20)))
                    {
                        m_SelectedRecord = null;
                        m_SelectedRecordIndex = -1;
                    }
                }

                EditorGUILayout.Space(2);

                // Event info
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Type:", GUILayout.Width(50));
                    EditorGUILayout.SelectableLabel(record.EventType.FullName, EditorStyles.textField,
                        GUILayout.Height(18));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Frame:", GUILayout.Width(50));
                    EditorGUILayout.LabelField($"{record.FrameCount} (t={record.Timestamp:F3}s)");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Scope:", GUILayout.Width(50));
                    EditorGUILayout.LabelField(
                        $"{EventDebugCapture.GetScopeName(record.ScopeKey)} ({record.ScopeKey.Type})");
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Data:", EditorStyles.boldLabel);

                // JSON body with scroll
                using (var scroll = new EditorGUILayout.ScrollViewScope(m_DetailScroll))
                {
                    m_DetailScroll = scroll.scrollPosition;

                    var jsonStyle = new GUIStyle(EditorStyles.textArea)
                    {
                        wordWrap = true,
                        richText = false,
                        font = GetMonospaceFont()
                    };

                    EditorGUILayout.TextArea(record.EventDataJson, jsonStyle, GUILayout.ExpandHeight(true));
                }
            }
        }

        private static Font? s_MonospaceFont;

        private static Font? GetMonospaceFont()
        {
            if (s_MonospaceFont == null)
            {
                s_MonospaceFont = Font.CreateDynamicFontFromOSFont("Consolas", 12);
                if (s_MonospaceFont == null)
                    s_MonospaceFont = Font.CreateDynamicFontFromOSFont("Courier New", 12);
            }

            return s_MonospaceFont;
        }

        private void DrawInspectorTab()
        {
            EditorGUILayout.LabelField("GameObject Event Inspector", EditorStyles.boldLabel);

            if (m_SelectedObject == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject in the scene to inspect its event scope.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.ObjectField("Selected", m_SelectedObject, typeof(GameObject), true);

            var scope = m_SelectedObject.GetComponent<IEventScope>();
            if (scope == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Selected GameObject does not implement IEventScope.", MessageType.Info);
                DrawListenerInfo(m_SelectedObject);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scope Key", scope.ScopeKey.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Scope Type", scope.ScopeKey.Type.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Associated Channels:", EditorStyles.boldLabel);

            using (var scroll = new EditorGUILayout.ScrollViewScope(m_InspectorScroll))
            {
                m_InspectorScroll = scroll.scrollPosition;

                var channels = EventDebugCapture.GetChannelInfos()
                    .Where(kvp => kvp.Key.ScopeKey.Equals(scope.ScopeKey))
                    .ToList();

                if (channels.Count == 0)
                {
                    EditorGUILayout.HelpBox("No active channels for this scope.", MessageType.Info);
                }
                else
                {
                    foreach (var kvp in channels)
                    {
                        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                        {
                            GUILayout.Label(kvp.Key.EventType.Name, GUILayout.MinWidth(150));
                            GUILayout.Label($"Listeners: {EventDebugCapture.GetListenerCount(kvp.Key)}",
                                GUILayout.Width(100));
                            GUILayout.Label($"Dispatches: {kvp.Value.DispatchCount}", GUILayout.Width(100));
                        }
                    }
                }
            }
        }

        private void DrawListenerInfo(GameObject obj)
        {
            var components = obj.GetComponents<MonoBehaviour>();
            var listenerTypes = new List<Type>();

            foreach (var comp in components)
            {
                if (comp == null) continue;
                var compType = comp.GetType();
                var interfaces = compType.GetInterfaces();

                foreach (var iface in interfaces)
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(EventListener<>))
                    {
                        var eventType = iface.GetGenericArguments()[0];
                        listenerTypes.Add(eventType);
                    }
                }
            }

            if (listenerTypes.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Listening to Events:", EditorStyles.boldLabel);

                foreach (var eventType in listenerTypes.Distinct())
                {
                    EditorGUILayout.LabelField($"  • {eventType.Name}");
                }
            }
        }

        private void RefreshChannelList()
        {
            m_Channels.Clear();

            var activeKeys = EventChannelRegistry.GetActiveChannelKeys();
            var channelInfos = EventDebugCapture.GetChannelInfos();

            foreach (var key in activeKeys)
            {
                channelInfos.TryGetValue(key, out var info);

                m_Channels.Add(new ChannelEntry
                {
                    Key = key,
                    EventTypeName = key.EventType.Name,
                    ScopeName = EventDebugCapture.GetScopeName(key.ScopeKey),
                    ListenerCount = EventChannelRegistry.GetListenerCount(key),
                    DispatchCount = info.DispatchCount
                });
            }

            m_Channels.Sort((a, b) => string.Compare(a.EventTypeName, b.EventTypeName, StringComparison.Ordinal));

            Repaint();
        }

        private List<ChannelEntry> GetFilteredChannels()
        {
            var result = new List<ChannelEntry>();

            foreach (var entry in m_Channels)
            {
                if (!string.IsNullOrEmpty(m_EventTypeFilter))
                {
                    if (!entry.EventTypeName.Contains(m_EventTypeFilter, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (!PassesScopeFilter(entry.Key.ScopeKey))
                    continue;

                result.Add(entry);
            }

            return result;
        }

        private bool PassesScopeFilter(EventScopeKey scopeKey)
        {
            if (!m_FilterGlobal && !m_FilterScene && !m_FilterInstance)
                return true;

            return scopeKey.Type switch
            {
                EventScopeType.Global => m_FilterGlobal,
                EventScopeType.Scene => m_FilterScene,
                EventScopeType.Instance => m_FilterInstance,
                _ => true
            };
        }
    }
}