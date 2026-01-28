using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using BrewedCode.Logging;
using Debug = UnityEngine.Debug;

namespace BrewedCode.Logging.Debug
{
    /// <summary>
    /// Editor window for viewing, filtering, and analyzing logs from the Logging System.
    /// Provides real-time log display with channel/level filtering, search, pause/resume, and export.
    /// </summary>
    public sealed class LogViewerEditorWindow : EditorWindow
    {
        // Window constants
        private const int MaxEntries = 1000;
        private const float ToolbarHeight = 40f;
        private const float FilterHeight = 80f;
        private const float StatusBarHeight = 20f;

        // Internal data model
        private class LogEntryData
        {
            public LogEntry Entry { get; }
            public string FormattedTime { get; }
            public string FormattedMessage { get; }

            public LogEntryData(LogEntry entry)
            {
                Entry = entry;
                FormattedTime = entry.Timestamp.ToString("HH:mm:ss.fff");
                FormattedMessage = $"{entry.Source}: {entry.Message}";
            }
        }

        // State management
        private List<LogEntryData> _allLogs = new();
        private List<LogEntryData> _filteredLogs = new();
        private Vector2 _scrollPosition = Vector2.zero;
        private bool _isPaused = false;
        private bool _autoScroll = true;
        private bool _filterPanelExpanded = true;
        private string _searchText = "";
        private double _lastRepaintTime = 0;
        private IDisposable _eventSubscription;
        private LogEntryData _selectedLogEntry = null;
        private Vector2 _detailsScrollPosition = Vector2.zero;
        private float _splitterPosition = 0.6f; // 60% for logs list, 40% for details

        // Filter state
        private Dictionary<LogChannel, bool> _channelFilters = new();
        private Dictionary<LogLevel, bool> _levelFilters = new();
        private HashSet<LogChannel> _allChannels = new();

        [MenuItem("BrewedCode/Log Viewer")]
        public static void ShowWindow()
        {
            GetWindow<LogViewerEditorWindow>("Log Viewer");
        }

        private void OnEnable()
        {
            // Initialize filters
            InitializeFilters();

            // Link static collections to instance collections
            _staticAllLogs = _allLogs;
            _staticFilteredLogs = _filteredLogs;
            _staticIsPaused = _isPaused;
            _windowInstance = this;

            // Try to subscribe to log events (will retry in OnGUI if fails)
            TrySubscribeToEvents();

            // Repaint on update
            EditorApplication.update += Repaint;
        }

        /// <summary>Try to subscribe to logging events, silently fail if LoggingRoot doesn't exist.</summary>
        private void TrySubscribeToEvents()
        {
            if (_eventSubscription != null) return; // Already subscribed

            try
            {
                var loggingRootInstance = LoggingRoot.Instance;
                var eventBus = loggingRootInstance.EventBus;
                _eventSubscription = eventBus.Subscribe<LogEmittedEvent>(OnLogEmittedStatic);
            }
            catch
            {
                // LoggingRoot doesn't exist yet - will try again on next repaint
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from log events
            _eventSubscription?.Dispose();

            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            // Try to subscribe if not already subscribed
            if (_eventSubscription == null)
            {
                TrySubscribeToEvents();
            }

            // Check if LoggingRoot is available
            if (!IsLoggingRootAvailable())
            {
                EditorGUILayout.HelpBox("LoggingRoot not found in scene. Please add a LoggingRoot GameObject to the scene to use the Log Viewer.", MessageType.Info);
                return;
            }

            // Sync static paused state
            _staticIsPaused = _isPaused;

            // Toolbar
            DrawToolbar();

            // Filter panel
            DrawFilterPanel();

            // Split view: Log list (left) + Details (right)
            DrawSplitView();

            // Status bar
            DrawStatusBar();
        }

        /// <summary>Check if LoggingRoot exists and is initialized.</summary>
        private bool IsLoggingRootAvailable()
        {
            try
            {
                var _ = LoggingRoot.Instance.Service;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Initialize filter dictionaries with default values.</summary>
        private void InitializeFilters()
        {
            // Get all predefined channels
            var channelFields = typeof(LogChannel).GetFields(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            foreach (var field in channelFields)
            {
                if (field.FieldType == typeof(LogChannel))
                {
                    var channel = (LogChannel)field.GetValue(null);
                    _allChannels.Add(channel);
                    _channelFilters[channel] = true; // All channels enabled by default
                }
            }

            // Initialize level filters (all enabled by default)
            _levelFilters[LogLevel.Trace] = true;
            _levelFilters[LogLevel.Info] = true;
            _levelFilters[LogLevel.Warning] = true;
            _levelFilters[LogLevel.Error] = true;
            _levelFilters[LogLevel.Fatal] = true;
        }

        // Static event handler to avoid instance issues with EventChannel
        private static List<LogEntryData> _staticAllLogs;
        private static List<LogEntryData> _staticFilteredLogs;
        private static bool _staticIsPaused;
        private static LogViewerEditorWindow _windowInstance;

        /// <summary>Static event handler for incoming log events.</summary>
        private static void OnLogEmittedStatic(LogEmittedEvent evt)
        {
            if (_staticIsPaused) return;
            if (_staticAllLogs == null) return;

            _staticAllLogs.Add(new LogEntryData(evt.Entry));

            // Enforce max limit
            if (_staticAllLogs.Count > MaxEntries)
            {
                _staticAllLogs.RemoveAt(0);
            }

            // Request repaint from window instance
            if (_windowInstance != null)
            {
                _windowInstance.Repaint();
            }
        }

        /// <summary>Draw the toolbar with controls.</summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight));

            // Clear button
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
            {
                _allLogs.Clear();
                _filteredLogs.Clear();
            }

            GUILayout.Space(5);

            // Pause/Resume toggle
            bool newPausedState = GUILayout.Toggle(_isPaused, _isPaused ? "⏸ Paused" : "▶ Recording",
                EditorStyles.toolbarButton, GUILayout.Width(80));
            if (newPausedState != _isPaused)
            {
                _isPaused = newPausedState;
            }

            GUILayout.Space(5);

            // Export button
            if (GUILayout.Button("Export", EditorStyles.toolbarButton))
            {
                ExportLogs();
            }

            GUILayout.Space(5);

            // Auto-scroll toggle
            bool newAutoScroll = GUILayout.Toggle(_autoScroll, "Auto-scroll", EditorStyles.toolbarButton);
            if (newAutoScroll != _autoScroll)
            {
                _autoScroll = newAutoScroll;
            }

            GUILayout.FlexibleSpace();

            // Entry count
            int filteredCount = _filteredLogs.Count;
            int totalCount = _allLogs.Count;
            EditorGUILayout.LabelField($"{filteredCount} / {totalCount}", EditorStyles.miniLabel, GUILayout.Width(60));

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Draw the filter panel with channel, level, and search filters.</summary>
        private void DrawFilterPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Height(_filterPanelExpanded ? FilterHeight : 20));

            // Collapsible header
            _filterPanelExpanded = EditorGUILayout.Foldout(_filterPanelExpanded, "Filters", true);

            if (_filterPanelExpanded)
            {
                // Channel filters
                EditorGUILayout.LabelField("Channels", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("All", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    foreach (var ch in _allChannels)
                        _channelFilters[ch] = true;
                }

                if (GUILayout.Button("None", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    foreach (var ch in _allChannels)
                        _channelFilters[ch] = false;
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                // Channel buttons (responsive wrapping)
                EditorGUILayout.LabelField("Channel Filters", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical("box");

                if (IsLoggingRootAvailable())
                {
                    // Wrap channels into rows based on available width
                    int buttonWidth = 80;
                    int buttonsPerRow = Mathf.Max(1, (int)(EditorGUIUtility.currentViewWidth / buttonWidth) - 1);
                    int currentButtonInRow = 0;

                    EditorGUILayout.BeginHorizontal();

                    foreach (var channel in _allChannels.OrderBy(c => c.Name))
                    {
                        // Start new row if needed
                        if (currentButtonInRow >= buttonsPerRow && currentButtonInRow > 0)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            currentButtonInRow = 0;
                        }

                        bool isEnabled = _channelFilters[channel];

                        // Get channel definition for color
                        var channelDef = LoggingRoot.Instance.Service.GetChannelDefinition(channel);
                        Color channelColor = Color.white;
                        if (channelDef != null)
                        {
                            LogViewerStyles.TryGetChannelColor(channelDef, out channelColor);
                        }

                        // Count logs for this channel
                        int count = _allLogs.Count(l => l.Entry.Channel.Equals(channel));

                        // Draw colored button
                        GUI.backgroundColor = isEnabled ? channelColor : new Color(0.3f, 0.3f, 0.3f);
                        if (GUILayout.Button($"{channel.Name} ({count})", EditorStyles.miniButton, GUILayout.Width(buttonWidth), GUILayout.Height(25)))
                        {
                            _channelFilters[channel] = !isEnabled;
                        }
                        GUI.backgroundColor = Color.white;

                        currentButtonInRow++;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();

                // Level filters (responsive wrapping)
                EditorGUILayout.LabelField("Levels", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical("box");

                // Wrap levels into rows based on available width
                int levelButtonWidth = 80;
                int levelButtonsPerRow = Mathf.Max(1, (int)(EditorGUIUtility.currentViewWidth / levelButtonWidth) - 1);
                int currentLevelButtonInRow = 0;

                EditorGUILayout.BeginHorizontal();

                foreach (LogLevel level in System.Enum.GetValues(typeof(LogLevel)))
                {
                    // Start new row if needed
                    if (currentLevelButtonInRow >= levelButtonsPerRow && currentLevelButtonInRow > 0)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        currentLevelButtonInRow = 0;
                    }

                    bool isEnabled = _levelFilters[level];
                    Color levelColor = LogViewerStyles.GetLevelColor(level);

                    GUI.backgroundColor = isEnabled ? levelColor : new Color(0.3f, 0.3f, 0.3f);
                    if (GUILayout.Button(level.ToString(), EditorStyles.miniButton, GUILayout.Width(levelButtonWidth), GUILayout.Height(25)))
                    {
                        _levelFilters[level] = !isEnabled;
                    }
                    GUI.backgroundColor = Color.white;

                    currentLevelButtonInRow++;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                // Search box
                EditorGUILayout.LabelField("Search", EditorStyles.boldLabel);
                string newSearchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
                if (newSearchText != _searchText)
                {
                    _searchText = newSearchText;
                }
            }

            EditorGUILayout.EndVertical();

            // Apply filters
            ApplyFilters();
        }

        /// <summary>Draw split view: log list on left, details panel on right.</summary>
        private void DrawSplitView()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            // Left panel: Log list
            EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * _splitterPosition - 3));
            DrawLogList();
            EditorGUILayout.EndVertical();

            // Splitter handle
            EditorGUILayout.BeginVertical(GUILayout.Width(6));
            GUI.Box(GUILayoutUtility.GetRect(-1, -1), "", "box");
            Rect splitterRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
            }
            if (EditorGUIUtility.hotControl >= 0 && Event.current.type == EventType.MouseDrag)
            {
                _splitterPosition += Event.current.delta.x / position.width;
                _splitterPosition = Mathf.Clamp(_splitterPosition, 0.3f, 0.7f);
                Event.current.Use();
                Repaint();
            }
            if (Event.current.type == EventType.MouseUp)
            {
                EditorGUIUtility.hotControl = 0;
            }

            EditorGUILayout.EndVertical();

            // Right panel: Details
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawDetailsPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Draw the log list with scrolling.</summary>
        private void DrawLogList()
        {
            EditorGUILayout.BeginVertical("box");

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            if (_filteredLogs.Count == 0)
            {
                EditorGUILayout.LabelField("No logs to display", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                for (int i = 0; i < _filteredLogs.Count; i++)
                {
                    DrawLogListItem(_filteredLogs[i], i);
                }

                // Auto-scroll to bottom
                if (_autoScroll)
                {
                    _scrollPosition.y = float.MaxValue;
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>Draw a single log entry in the list (clickable).</summary>
        private void DrawLogListItem(LogEntryData logData, int index)
        {
            var entry = logData.Entry;
            GUIStyle entryStyle = (_selectedLogEntry == logData)
                ? LogViewerStyles.LogEntrySelected
                : (index % 2 == 0) ? LogViewerStyles.LogEntryEven : LogViewerStyles.LogEntryOdd;

            EditorGUILayout.BeginHorizontal(entryStyle);

            // Timestamp (gray, small)
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            EditorGUILayout.LabelField(logData.FormattedTime, EditorStyles.miniLabel, GUILayout.Width(90));
            GUI.color = Color.white;

            // Channel (colored)
            Color channelColor = Color.white;
            var channelDef = LoggingRoot.Instance.Service.GetChannelDefinition(entry.Channel);
            if (channelDef != null)
            {
                LogViewerStyles.TryGetChannelColor(channelDef, out channelColor);
            }
            GUI.color = channelColor;
            EditorGUILayout.LabelField($"[{entry.Channel.Name}]", EditorStyles.miniLabel, GUILayout.Width(80));
            GUI.color = Color.white;

            // Level (colored)
            Color levelColor = LogViewerStyles.GetLevelColor(entry.Level);
            GUI.color = levelColor;
            EditorGUILayout.LabelField($"[{entry.Level}]", EditorStyles.miniLabel, GUILayout.Width(70));
            GUI.color = Color.white;

            // Message (clickable)
            if (GUILayout.Button(logData.FormattedMessage, EditorStyles.label))
            {
                _selectedLogEntry = logData;
                _detailsScrollPosition = Vector2.zero;
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Draw the details panel for the selected log entry.</summary>
        private void DrawDetailsPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.MinHeight(200));

            // Header
            EditorGUILayout.LabelField("Details Panel", EditorStyles.boldLabel);

            if (_selectedLogEntry == null)
            {
                EditorGUILayout.LabelField("Select a log entry to view details", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            var entry = _selectedLogEntry.Entry;

            _detailsScrollPosition = EditorGUILayout.BeginScrollView(
                _detailsScrollPosition,
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)
            );

            EditorGUILayout.BeginVertical();

            // Full timestamp
            EditorGUILayout.LabelField("Timestamp", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"), EditorStyles.textField);

            EditorGUILayout.Space(3);

            // Channel
            EditorGUILayout.LabelField("Channel", EditorStyles.boldLabel);
            Color channelColor = Color.white;
            var channelDef = IsLoggingRootAvailable() ? LoggingRoot.Instance.Service.GetChannelDefinition(entry.Channel) : null;
            if (channelDef != null)
            {
                LogViewerStyles.TryGetChannelColor(channelDef, out channelColor);
            }
            GUI.color = channelColor;
            EditorGUILayout.LabelField(entry.Channel.Name, EditorStyles.textField);
            GUI.color = Color.white;

            EditorGUILayout.Space(3);

            // Level
            EditorGUILayout.LabelField("Level", EditorStyles.boldLabel);
            Color levelColor = LogViewerStyles.GetLevelColor(entry.Level);
            GUI.color = levelColor;
            EditorGUILayout.LabelField(entry.Level.ToString(), EditorStyles.textField);
            GUI.color = Color.white;

            EditorGUILayout.Space(3);

            // Source
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(entry.Source, EditorStyles.textField);

            EditorGUILayout.Space(3);

            // Message (with scroll support)
            EditorGUILayout.LabelField("Message", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(entry.Message, GUILayout.Height(60), GUILayout.ExpandWidth(true));

            // Stack trace
            if (!string.IsNullOrEmpty(entry.StackTrace))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Stack Trace", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(entry.StackTrace, GUILayout.Height(100), GUILayout.ExpandWidth(true));
            }

            // Exception
            if (entry.Exception != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Exception", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(entry.Exception.ToString(), GUILayout.Height(120), GUILayout.ExpandWidth(true));
            }

            // Metadata
            if (entry.Metadata != null && entry.Metadata.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Metadata", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                foreach (var kvp in entry.Metadata)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(kvp.Key, EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField(kvp.Value?.ToString() ?? "null", EditorStyles.textField);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }


        /// <summary>Draw the status bar at the bottom.</summary>
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(StatusBarHeight));

            // Filtered count
            EditorGUILayout.LabelField($"Showing {_filteredLogs.Count} of {_allLogs.Count} logs",
                EditorStyles.miniLabel, GUILayout.Width(150));

            // Status
            string status = _isPaused ? "⏸ PAUSED" : "▶ RECORDING";
            Color statusColor = _isPaused ? Color.yellow : Color.green;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(status, EditorStyles.miniLabel, GUILayout.Width(80));
            GUI.color = Color.white;

            // Last update
            EditorGUILayout.LabelField($"Updated {DateTime.Now:HH:mm:ss}", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Apply current filters to create the filtered log list.</summary>
        private void ApplyFilters()
        {
            _filteredLogs.Clear();

            foreach (var logData in _allLogs)
            {
                // Channel filter
                if (!_channelFilters.TryGetValue(logData.Entry.Channel, out var isChannelEnabled) || !isChannelEnabled)
                    continue;

                // Level filter
                if (!_levelFilters.TryGetValue(logData.Entry.Level, out var isLevelEnabled) || !isLevelEnabled)
                    continue;

                // Search filter (case-insensitive)
                if (!string.IsNullOrEmpty(_searchText))
                {
                    string searchLower = _searchText.ToLower();
                    if (!logData.Entry.Message.ToLower().Contains(searchLower) &&
                        !logData.Entry.Source.ToLower().Contains(searchLower) &&
                        (logData.Entry.Exception == null || !logData.Entry.Exception.ToString().ToLower().Contains(searchLower)))
                    {
                        continue;
                    }
                }

                _filteredLogs.Add(logData);
            }
        }

        /// <summary>Export all logs to a timestamped text file.</summary>
        private void ExportLogs()
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"logs_{timestamp}.txt";
            string path = EditorUtility.SaveFilePanel("Export Logs", "", filename, "txt");

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Log Export - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Total Logs: {_allLogs.Count}, Showing: {_filteredLogs.Count}");
                sb.AppendLine(new string('=', 80));
                sb.AppendLine();

                foreach (var logData in _filteredLogs)
                {
                    var entry = logData.Entry;
                    sb.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Channel.Name}] [{entry.Level}]");
                    sb.AppendLine($"Source: {entry.Source}");
                    sb.AppendLine($"Message: {entry.Message}");

                    if (!string.IsNullOrEmpty(entry.StackTrace))
                    {
                        sb.AppendLine("Stack Trace:");
                        sb.AppendLine(entry.StackTrace);
                    }

                    if (entry.Exception != null)
                    {
                        sb.AppendLine("Exception:");
                        sb.AppendLine(entry.Exception.ToString());
                    }

                    if (entry.Metadata != null && entry.Metadata.Count > 0)
                    {
                        sb.AppendLine("Metadata:");
                        foreach (var kvp in entry.Metadata)
                        {
                            sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
                        }
                    }

                    sb.AppendLine(new string('-', 80));
                    sb.AppendLine();
                }

                System.IO.File.WriteAllText(path, sb.ToString());
                EditorUtility.DisplayDialog("Success", $"Logs exported to:\n{path}", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to export logs:\n{ex.Message}", "OK");
            }
        }
    }
}
