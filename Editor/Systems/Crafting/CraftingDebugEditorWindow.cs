using UnityEditor;
using UnityEngine;
using BrewedCode.Crafting;
using System.Collections.Generic;
using System.Linq;

namespace BrewedCode.Crafting.Debug.Editor
{
    /// <summary>
    /// EditorWindow for debugging the Crafting System.
    /// Shows comprehensive information about all stations and their states.
    /// </summary>
    public class CraftingDebugEditorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _showOnlyActive = false;
        private bool _showQueues = true;
        private bool _showStats = true;
        private float _refreshRate = 0.5f;
        private float _lastRefresh;
        private GUIStyle _headerStyle;
        private GUIStyle _activeStyle;
        private GUIStyle _idleStyle;
        private GUIStyle _warningStyle;

        [MenuItem("Window/BrewedCode/Crafting Debug")]
        public static void ShowWindow()
        {
            GetWindow<CraftingDebugEditorWindow>("Crafting Debug");
        }

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.LabelField("🔧 CRAFTING SYSTEM DEBUG", _headerStyle);
            EditorGUILayout.Space(10);

            // Controls
            DrawControls();

            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                DrawStats();
                EditorGUILayout.Space();
                DrawStations();
            }
            else
            {
                EditorGUILayout.HelpBox("Play to see live debug info", MessageType.Info);
            }
        }

        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };

                _activeStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = MakeTex(2, 2, new Color(0.2f, 0.6f, 0.2f, 0.3f)) }
                };

                _idleStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.3f, 0.3f)) }
                };

                _warningStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = MakeTex(2, 2, new Color(0.8f, 0.6f, 0.2f, 0.3f)) }
                };
            }
        }

        private void DrawControls()
        {
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _showOnlyActive = EditorGUILayout.Toggle("Show Only Active", _showOnlyActive);
                _showQueues = EditorGUILayout.Toggle("Show Queues", _showQueues);
                _showStats = EditorGUILayout.Toggle("Show Stats", _showStats);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _refreshRate = EditorGUILayout.Slider("Refresh Rate (Hz)", _refreshRate, 1f, 10f);
                if (GUILayout.Button("🔄 Refresh Now", GUILayout.Width(120)))
                {
                    _lastRefresh = 0; // Force refresh
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawStats()
        {
            if (!_showStats)
                return;

            var craftingRoot = CraftingRoot.Instance;
            if (craftingRoot == null)
            {
                EditorGUILayout.HelpBox("CraftingRoot not found in scene", MessageType.Error);
                return;
            }

            var allStations = FindObjectsOfType<CraftingStation>();
            var activeCrafting = allStations.Count(s => s.IsCrafting);
            var totalQueued = allStations.Sum(s => s.CraftingRemainingAmount - 1);
            var paused = allStations.Count(s => s.IsPaused);

            EditorGUILayout.LabelField("📊 Statistics", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                var statsText = $"Total Stations: <b>{allStations.Length}</b>\n" +
                               $"<color=green>Active Crafting:</color> <b>{activeCrafting}</b>\n" +
                               $"<color=yellow>Paused:</color> <b>{paused}</b>\n" +
                               $"<color=blue>Idle:</color> <b>{allStations.Length - activeCrafting - paused}</b>\n" +
                               $"<color=cyan>Total in Queue:</color> <b>{totalQueued}</b>";

                EditorGUILayout.HelpBox(statsText, MessageType.None);
            }
        }

        private void DrawStations()
        {
            var allStations = FindObjectsOfType<CraftingStation>();

            if (allStations.Length == 0)
            {
                EditorGUILayout.HelpBox("No CraftingStations found in scene", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"🏭 Stations ({allStations.Length})", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var station in allStations)
            {
                if (_showOnlyActive && !station.IsCrafting)
                    continue;

                DrawStationPanel(station);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStationPanel(CraftingStation station)
        {
            var stationInfo = CraftingRoot.Instance.Service.GetStationInfo(station.Id);
            if (stationInfo == null)
                return;

            var boxStyle = stationInfo.State == CraftingStationState.Crafting ? _activeStyle :
                          stationInfo.State == CraftingStationState.Paused ? _warningStyle :
                          _idleStyle;

            using (new EditorGUILayout.VerticalScope(boxStyle))
            {
                // Header
                using (new EditorGUILayout.HorizontalScope())
                {
                    var stateIcon = stationInfo.State == CraftingStationState.Crafting ? "⚙️" :
                                   stationInfo.State == CraftingStationState.Paused ? "⏸️" :
                                   "⏹️";

                    EditorGUILayout.LabelField($"{stateIcon} {station.gameObject.name}", EditorStyles.boldLabel);

                    if (GUILayout.Button("Select", GUILayout.Width(80)))
                        EditorGUIUtility.PingObject(station.gameObject);

                    if (stationInfo.State != CraftingStationState.Idle)
                    {
                        if (GUILayout.Button("Cancel", GUILayout.Width(80)))
                        {
                            CraftingRoot.Instance.Service.TryStopCrafting(station.Id, out _);
                        }
                    }
                }

                EditorGUILayout.Space(5);

                // Status info
                EditorGUILayout.LabelField("Status", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"State: {stationInfo.State}");
                EditorGUILayout.LabelField($"Current Item: {stationInfo.CurrentCraftable?.GetType().Name ?? "None"}");

                // Progress bar
                if (stationInfo.State != CraftingStationState.Idle)
                {
                    var progress = stationInfo.Progress;
                    var rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    DrawProgressBar(rect, progress, stationInfo.TimeElapsed, stationInfo.TimeTotal);
                }

                // Timing
                EditorGUILayout.LabelField("Timing", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Elapsed: {stationInfo.TimeElapsed:F2}s / {stationInfo.TimeTotal:F2}s");
                EditorGUILayout.LabelField($"Remaining: {stationInfo.TimeRemaining:F2}s");

                // Queue info
                if (_showQueues && stationInfo.QueuedCount > 1)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"Queue", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Items in Queue: {stationInfo.QueuedCount - 1}");
                }

                EditorGUILayout.Space(5);
            }
        }

        private void DrawProgressBar(Rect rect, float progress, float elapsed, float total)
        {
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * progress, rect.height),
                             new Color(0.2f, 0.8f, 0.2f, 1f));

            EditorGUI.LabelField(rect, $"{progress * 100:F1}% ({elapsed:F1}s / {total:F1}s)",
                               EditorStyles.miniLabel);
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            var pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void OnInspectorUpdate()
        {
            // Refresh periodically
            if (Time.realtimeSinceStartup - _lastRefresh > 1f / _refreshRate)
            {
                _lastRefresh = Time.realtimeSinceStartup;
                Repaint();
            }
        }
    }
}
