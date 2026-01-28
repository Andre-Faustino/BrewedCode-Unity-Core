using System.Collections.Generic;
using System.Linq;
using BrewedCode.Crafting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BrewedCode.Crafting.Debug
{
    /// <summary>
    /// Complete runtime debug panel for Crafting System with visualization and controls.
    /// Press F9 to toggle. Shows all stations, queues, progress, and provides control options.
    /// </summary>
    [AddComponentMenu("Crafting/Debug/Crafting Debug Panel")]
    public class CraftingDebugPanel : MonoBehaviour
    {
        [SerializeField] private bool _startVisible = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F9;

        private Canvas _canvas;
        private ScrollRect _scrollRect;
        private Transform _contentTransform;
        private TextMeshProUGUI _statsText;
        private TextMeshProUGUI _titleText;
        private ICraftingService _service;
        private Dictionary<CraftingStationId, GameObject> _stationUICache = new();
        private bool _isVisible;

        private void Awake()
        {
            _service = CraftingRoot.Instance?.Service;
            if (_service == null)
            {
                return;
            }

            CreateOrUpdateUI();
            _isVisible = _startVisible;
            UpdateCanvasVisibility();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _isVisible = !_isVisible;
                UpdateCanvasVisibility();
            }

            if (_isVisible)
                RefreshDisplay();
        }

        private void CreateOrUpdateUI()
        {
            // Find or create canvas
            var existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null && existingCanvas.gameObject.name == "CraftingDebugCanvas")
            {
                _canvas = existingCanvas;
                _contentTransform = _canvas.transform.Find("ScrollView/Viewport/Content");
            }
            else
            {
                CreateUIFromScratch();
            }
        }

        private void CreateUIFromScratch()
        {
            // Canvas
            var canvasObj = new GameObject("CraftingDebugCanvas");
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;

            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // Background panel
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(_canvas.transform, false);
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.9f);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = new Vector2(1, 1);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Header panel
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(_canvas.transform, false);
            var headerImage = headerObj.AddComponent<Image>();
            headerImage.color = new Color(0.1f, 0.2f, 0.3f, 0.95f);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = Vector2.one;
            headerRect.offsetMin = new Vector2(0, -50);
            headerRect.offsetMax = Vector2.zero;

            // Title
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            _titleText = titleObj.AddComponent<TextMeshProUGUI>();
            _titleText.text = "🔧 CRAFTING DEBUG (F9 to toggle)";
            _titleText.fontSize = 32;
            _titleText.alignment = TextAlignmentOptions.MidlineLeft;
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, 0);

            // Stats text
            var statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(_canvas.transform, false);
            _statsText = statsObj.AddComponent<TextMeshProUGUI>();
            _statsText.fontSize = 16;
            _statsText.alignment = TextAlignmentOptions.TopLeft;
            var statsRect = statsObj.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 1);
            statsRect.anchorMax = new Vector2(0.3f, 1);
            statsRect.offsetMin = new Vector2(10, -100);
            statsRect.offsetMax = new Vector2(-10, -60);

            // ScrollView
            var scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(_canvas.transform, false);
            _scrollRect = scrollObj.AddComponent<ScrollRect>();
            _scrollRect.vertical = true;
            _scrollRect.horizontal = false;
            var scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(10, 10);
            scrollRect.offsetMax = new Vector2(-10, -110);

            // Scrollbar
            var scrollbarObj = new GameObject("Scrollbar");
            scrollbarObj.transform.SetParent(scrollObj.transform, false);
            var scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            var scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.offsetMin = new Vector2(-20, 0);
            scrollbarRect.offsetMax = Vector2.zero;
            _scrollRect.verticalScrollbar = scrollbar;
            _scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // Viewport
            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);
            var viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-20, 0);
            _scrollRect.viewport = viewportRect;

            // Content
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 5;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.childForceExpandHeight = false;
            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            _contentTransform = contentObj.transform;
            _scrollRect.content = contentRect;
        }

        private void RefreshDisplay()
        {
            if (_service == null)
                return;

            UpdateStatsDisplay();
            UpdateStationsDisplay();
        }

        private void UpdateStatsDisplay()
        {
            var allStations = FindObjectsOfType<CraftingStation>();
            var activeCrafting = allStations.Count(s => s.IsCrafting);
            var totalQueued = allStations.Sum(s => Mathf.Max(0, s.CraftingRemainingAmount - 1));
            var paused = allStations.Count(s => s.IsPaused);

            _statsText.text = $"<b>📊 STATS</b>\n" +
                             $"<color=lime>Total:</color> {allStations.Length}\n" +
                             $"<color=green>Active:</color> {activeCrafting}\n" +
                             $"<color=yellow>Paused:</color> {paused}\n" +
                             $"<color=cyan>Queued:</color> {totalQueued}\n" +
                             $"<color=gray>Idle:</color> {allStations.Length - activeCrafting - paused}";
        }

        private void UpdateStationsDisplay()
        {
            var allStations = FindObjectsOfType<CraftingStation>();

            // Remove missing stations
            var missingStations = _stationUICache.Keys
                .Where(id => !allStations.Any(s => s.Id == id))
                .ToList();

            foreach (var stationId in missingStations)
            {
                if (_stationUICache.TryGetValue(stationId, out var uiObj))
                {
                    Destroy(uiObj);
                    _stationUICache.Remove(stationId);
                }
            }

            // Update existing stations
            foreach (var station in allStations)
            {
                if (!_stationUICache.TryGetValue(station.Id, out var uiObj))
                {
                    uiObj = CreateStationUI(station);
                    _stationUICache[station.Id] = uiObj;
                }

                UpdateStationUI(station, uiObj);
            }
        }

        private GameObject CreateStationUI(CraftingStation station)
        {
            var panelObj = new GameObject($"Station_{station.Id.GetHashCode()}");
            panelObj.transform.SetParent(_contentTransform, false);

            var panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400, 120);

            var layoutElement = panelObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 120;
            layoutElement.preferredWidth = 400;

            // Text content
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(panelObj.transform, false);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.TopLeft;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);

            // Cancel button
            var buttonObj = new GameObject("CancelButton");
            buttonObj.transform.SetParent(panelObj.transform, false);
            var button = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(1, 0.2f, 0.2f, 0.7f);
            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1, 0);
            buttonRect.anchorMax = new Vector2(1, 0);
            buttonRect.sizeDelta = new Vector2(60, 30);
            buttonRect.anchoredPosition = new Vector2(-65, 5);

            var buttonText = new GameObject("Text");
            buttonText.transform.SetParent(buttonObj.transform, false);
            var buttonTextComponent = buttonText.AddComponent<TextMeshProUGUI>();
            buttonTextComponent.text = "✕";
            buttonTextComponent.fontSize = 20;
            buttonTextComponent.alignment = TextAlignmentOptions.Center;
            var buttonTextRect = buttonText.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;

            button.onClick.AddListener(() =>
            {
                _service.TryStopCrafting(station.Id, out _);
            });

            return panelObj;
        }

        private void UpdateStationUI(CraftingStation station, GameObject uiObj)
        {
            var stationInfo = _service.GetStationInfo(station.Id);
            if (stationInfo == null)
                return;

            var textComponent = uiObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent == null)
                return;

            var stateColor = stationInfo.State == CraftingStationState.Crafting ? "<color=lime>" :
                           stationInfo.State == CraftingStationState.Paused ? "<color=yellow>" :
                           "<color=gray>";

            var endColor = "</color>";
            var itemName = stationInfo.CurrentCraftable?.GetType().Name ?? "None";
            var progress = stationInfo.Progress * 100f;
            var progressBar = GetProgressBar(stationInfo.Progress);

            textComponent.text =
                $"<b>{station.gameObject.name}</b> {stateColor}{stationInfo.State}{endColor}\n" +
                $"Item: <b>{itemName}</b>\n" +
                $"<color=cyan>{progressBar}</color> {progress:F0}% ({stationInfo.TimeElapsed:F1}s / {stationInfo.TimeTotal:F1}s)\n" +
                $"Queue: {Mathf.Max(0, stationInfo.QueuedCount - 1)} items";

            // Update panel color based on state
            var panelImage = uiObj.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = stationInfo.State == CraftingStationState.Crafting
                    ? new Color(0.2f, 0.4f, 0.2f, 0.8f)
                    : stationInfo.State == CraftingStationState.Paused
                    ? new Color(0.4f, 0.4f, 0.1f, 0.8f)
                    : new Color(0.15f, 0.15f, 0.15f, 0.8f);
            }
        }

        private string GetProgressBar(float progress)
        {
            int filled = (int)(progress * 10);
            return "[" + new string('█', filled) + new string('░', 10 - filled) + "]";
        }

        private void UpdateCanvasVisibility()
        {
            if (_canvas != null)
                _canvas.gameObject.SetActive(_isVisible);
        }
    }
}
