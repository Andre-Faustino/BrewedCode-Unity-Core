using UnityEditor;
using UnityEngine;
using BrewedCode.Crafting;
using System.Linq;

namespace BrewedCode.Crafting.Debug.Editor
{
    /// <summary>
    /// Comprehensive Crafting Debug Window with all functionality in one place.
    /// Displays statistics, station list, and controls for debugging.
    /// </summary>
    public class CraftingDebugWindow : EditorWindow
    {
        private Vector2 _scrollPosition = Vector2.zero;
        private float _refreshTimer = 0f;
        private GUIStyle? _headerStyle;
        private GUIStyle? _stationPanelStyle;
        private GUIStyle? _statBoxStyle;

        [MenuItem("BrewedCode/Crafting Debug")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<CraftingDebugWindow>("Crafting Debug");
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("⏸️ Play the game to see live debug info", MessageType.Info);
                return;
            }

            var craftingRoot = CraftingRoot.Instance;
            if (craftingRoot == null)
            {
                EditorGUILayout.HelpBox("❌ CraftingRoot not found in scene", MessageType.Error);
                return;
            }

            var allStations = Object.FindObjectsOfType<CraftingStation>();

            // Header
            EditorGUILayout.LabelField("🔧 CRAFTING DEBUG SYSTEM", _headerStyle);
            EditorGUILayout.Space(5);

            // Global Controls
            DrawGlobalControls(allStations);

            EditorGUILayout.Space(10);

            // Statistics
            DrawStatistics(allStations);

            EditorGUILayout.Space(10);

            // Stations List
            DrawStationsList(craftingRoot, allStations);
        }

        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };

                _statBoxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    alignment = TextAnchor.UpperLeft
                };

                _stationPanelStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }

        private void DrawGlobalControls(CraftingStation[] allStations)
        {
            EditorGUILayout.LabelField("🎮 GLOBAL CONTROLS", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("🛑 Stop All Crafting", GUILayout.Height(30)))
                {
                    var stoppedCount = 0;
                    foreach (var station in allStations)
                    {
                        if (station.IsCrafting || station.IsPaused)
                        {
                            station.StopCrafting();
                            stoppedCount++;
                        }
                    }
                }

                if (GUILayout.Button("⏸️ Pause All Active", GUILayout.Height(30)))
                {
                    foreach (var station in allStations)
                    {
                        if (station.IsCrafting)
                        {
                            station.PauseCrafting();
                        }
                    }
                }

                if (GUILayout.Button("▶️ Resume All Paused", GUILayout.Height(30)))
                {
                    foreach (var station in allStations)
                    {
                        if (station.IsPaused)
                        {
                            station.ResumeCrafting();
                        }
                    }
                }
            }

            EditorGUILayout.Space(5);
        }

        private void DrawStatistics(CraftingStation[] allStations)
        {
            EditorGUILayout.LabelField("📊 STATISTICS", EditorStyles.boldLabel);

            var activeCount = allStations.Count(s => s.IsCrafting);
            var pausedCount = allStations.Count(s => s.IsPaused);
            var idleCount = allStations.Count(s => s.IsIdle);
            var totalQueued = allStations.Sum(s => Mathf.Max(0, s.CraftingRemainingAmount - 1));

            using (new EditorGUILayout.VerticalScope(_statBoxStyle))
            {
                EditorGUILayout.LabelField($"Total Stations: {allStations.Length}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"🟢 Active Crafting: {activeCount}", EditorStyles.label);
                EditorGUILayout.LabelField($"🟡 Paused: {pausedCount}", EditorStyles.label);
                EditorGUILayout.LabelField($"⚫ Idle: {idleCount}", EditorStyles.label);
                EditorGUILayout.LabelField($"📦 Total in Queue: {totalQueued}", EditorStyles.label);
            }

            EditorGUILayout.Space(5);
        }

        private void DrawStationsList(CraftingRoot craftingRoot, CraftingStation[] allStations)
        {
            EditorGUILayout.LabelField("🏭 STATIONS", EditorStyles.boldLabel);

            if (allStations.Length == 0)
            {
                EditorGUILayout.HelpBox("No CraftingStations found in scene", MessageType.Warning);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var station in allStations)
            {
                var info = craftingRoot.Service.GetStationInfo(station.Id);
                if (info == null) continue;

                DrawStationPanel(station, info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStationPanel(CraftingStation station, CraftingStationInfo info)
        {
            using (new EditorGUILayout.VerticalScope(_stationPanelStyle))
            {
                // Header with status
                var statusIcon = info.State == CraftingStationState.Crafting ? "🟢" :
                               info.State == CraftingStationState.Paused ? "🟡" :
                               "⚫";

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{statusIcon} {station.gameObject.name}", EditorStyles.boldLabel);

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        EditorGUIUtility.PingObject(station.gameObject);
                    }

                    if (GUILayout.Button("Cancel", GUILayout.Width(60)))
                    {
                        station.StopCrafting();
                    }
                }

                EditorGUILayout.Space(5);

                // Status info
                EditorGUILayout.LabelField($"State: {info.State}", EditorStyles.label);

                var itemName = info.CurrentCraftable?.GetType().Name ?? "None";
                EditorGUILayout.LabelField($"Item: {itemName}", EditorStyles.label);

                // Progress
                if (info.State != CraftingStationState.Idle)
                {
                    var rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    DrawProgressBar(rect, info.Progress, info.TimeElapsed, info.TimeTotal);

                    EditorGUILayout.LabelField($"Time: {info.TimeElapsed:F1}s / {info.TimeTotal:F1}s", EditorStyles.label);
                    EditorGUILayout.LabelField($"Remaining: {info.TimeRemaining:F1}s", EditorStyles.label);
                }

                // Queue info
                var queueCount = info.QueuedCount - 1;
                if (queueCount > 0)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField($"📦 Queue: {queueCount} items waiting", EditorStyles.label);
                }

                EditorGUILayout.Space(5);
            }

            EditorGUILayout.Space(3);
        }

        private void DrawProgressBar(Rect rect, float progress, float elapsed, float total)
        {
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * progress, rect.height),
                             new Color(0.2f, 0.8f, 0.2f, 1f));

            EditorGUI.LabelField(rect, $"{progress * 100:F0}% ({elapsed:F1}s / {total:F1}s)",
                               EditorStyles.miniLabel);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
