using System.Collections.Generic;
using System.Linq;
using BrewedCode.Crafting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BrewedCode.Crafting.Debug
{
    /// <summary>
    /// Runtime debug panel for the Crafting System.
    /// Shows all stations, their status, queues, and provides control options.
    /// </summary>
    [AddComponentMenu("Crafting/Debug/Crafting Debugger")]
    public class CraftingDebugger : MonoBehaviour
    {
        [SerializeField] private Canvas _debugCanvas;
        [SerializeField] private Transform _stationsContainer;
        [SerializeField] private GameObject _stationPanelPrefab;
        [SerializeField] private TextMeshProUGUI _statsText;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private Button _refreshButton;

        private ICraftingService _craftingService;
        private Dictionary<CraftingStationId, GameObject> _stationPanels = new();
        private bool _isVisible = true;

        private void Awake()
        {
            _craftingService = CraftingRoot.Instance?.Service;

            if (_debugCanvas == null)
                CreateDefaultUI();

            if (_toggleButton != null)
                _toggleButton.onClick.AddListener(ToggleDebugPanel);

            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(RefreshDebugInfo);
        }

        private void Update()
        {
            // Allow toggle with key
            if (Input.GetKeyDown(KeyCode.F9))
                ToggleDebugPanel();

            RefreshDebugInfo();
        }

        private void ToggleDebugPanel()
        {
            _isVisible = !_isVisible;
            if (_debugCanvas != null)
                _debugCanvas.gameObject.SetActive(_isVisible);
        }

        private void RefreshDebugInfo()
        {
            if (!_isVisible || _craftingService == null)
                return;

            UpdateStations();
            UpdateStats();
        }

        private void UpdateStations()
        {
            // This is a simplification - ideally we'd iterate registered stations
            // For now, find all CraftingStations in scene
            var allStations = FindObjectsOfType<CraftingStation>();

            foreach (var station in allStations)
            {
                if (!_stationPanels.ContainsKey(station.Id))
                {
                    var panelObj = Instantiate(_stationPanelPrefab, _stationsContainer);
                    _stationPanels[station.Id] = panelObj;
                }

                UpdateStationPanel(station, _stationPanels[station.Id]);
            }
        }

        private void UpdateStationPanel(CraftingStation station, GameObject panelObj)
        {
            var stationInfo = _craftingService.GetStationInfo(station.Id);
            if (stationInfo == null)
                return;

            var panel = panelObj.GetComponent<CraftingDebugStationPanel>();
            if (panel == null)
                panel = panelObj.AddComponent<CraftingDebugStationPanel>();

            panel.UpdateDisplay(station, stationInfo, _craftingService);
        }

        private void UpdateStats()
        {
            if (_statsText == null)
                return;

            var allStations = FindObjectsOfType<CraftingStation>();
            var activeCrafting = allStations.Count(s => s.IsCrafting);
            var totalQueued = allStations.Sum(s => s.CraftingRemainingAmount);

            var stats = $"<b>CRAFTING SYSTEM DEBUG</b>\n" +
                        $"━━━━━━━━━━━━━━━━━━━━━\n" +
                        $"<color=yellow>Total Stations:</color> {allStations.Length}\n" +
                        $"<color=green>Active Crafting:</color> {activeCrafting}\n" +
                        $"<color=cyan>Total Queued:</color> {totalQueued}\n" +
                        $"<color=orange>Idle:</color> {allStations.Length - activeCrafting}\n" +
                        $"━━━━━━━━━━━━━━━━━━━━━\n" +
                        $"<size=70%><color=gray>Press F9 to toggle\n" +
                        $"Right-click stations to control</color></size>";

            _statsText.text = stats;
        }

        private void CreateDefaultUI()
        {
            // Create a simple canvas structure if not provided
            var canvasObj = new GameObject("CraftingDebugCanvas");
            _debugCanvas = canvasObj.AddComponent<Canvas>();
            _debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // Background
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(_debugCanvas.transform, false);
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Title
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_debugCanvas.transform, false);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "🔧 CRAFTING DEBUG MENU";
            titleText.alignment = TextAlignmentOptions.TopLeft;
            titleText.fontSize = 36;
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(20, -20);
            titleRect.sizeDelta = new Vector2(600, 100);

            _debugCanvas.gameObject.SetActive(_isVisible);
        }
    }

    /// <summary>
    /// Individual station debug panel showing detailed info and controls.
    /// </summary>
    public class CraftingDebugStationPanel : MonoBehaviour
    {
        private TextMeshProUGUI _infoText;
        private CraftingStation _station;
        private ICraftingService _service;

        public void UpdateDisplay(CraftingStation station, CraftingStationInfo stationInfo, ICraftingService service)
        {
            _station = station;
            _service = service;

            if (_infoText == null)
            {
                var textObj = new GameObject("StationInfo");
                textObj.transform.SetParent(transform, false);
                _infoText = textObj.AddComponent<TextMeshProUGUI>();
                _infoText.fontSize = 16;
                var rect = textObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(400, 200);
            }

            var stateColor = stationInfo.State == CraftingStationState.Crafting ? "<color=lime>" :
                           stationInfo.State == CraftingStationState.Paused ? "<color=yellow>" :
                           "<color=gray>";

            var itemName = stationInfo.CurrentCraftable?.GetType().Name ?? "None";
            var progress = stationInfo.Progress * 100f;

            var text = $"<b>[{station.Id.GetHashCode()}]</b> {station.gameObject.name}\n" +
                      $"{stateColor}{stationInfo.State}</color>\n" +
                      $"━━━━━━━━━━━━━━━━━━\n" +
                      $"<color=cyan>Crafting:</color> {itemName}\n" +
                      $"<color=orange>Progress:</color> {progress:F1}%\n" +
                      $"<color=magenta>Time:</color> {stationInfo.TimeElapsed:F1}s / {stationInfo.TimeTotal:F1}s\n" +
                      $"<color=yellow>Queued:</color> {stationInfo.QueuedCount - 1} items\n" +
                      $"━━━━━━━━━━━━━━━━━━\n" +
                      $"<size=80%><color=gray>Left-click to Cancel\n" +
                      $"Right-click for more options</color></size>";

            _infoText.text = text;

            // Add buttons for control
            var button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
                button.onClick.AddListener(() => _service.TryStopCrafting(station.Id, out _));
            }

            // Add background
            if (GetComponent<Image>() == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = stationInfo.State == CraftingStationState.Crafting
                    ? new Color(0.2f, 0.5f, 0.2f, 0.7f)
                    : stationInfo.State == CraftingStationState.Paused
                    ? new Color(0.5f, 0.5f, 0.2f, 0.7f)
                    : new Color(0.3f, 0.3f, 0.3f, 0.7f);
            }

            var rect2 = GetComponent<RectTransform>();
            if (rect2.sizeDelta == Vector2.zero)
                rect2.sizeDelta = new Vector2(420, 220);
        }
    }
}
