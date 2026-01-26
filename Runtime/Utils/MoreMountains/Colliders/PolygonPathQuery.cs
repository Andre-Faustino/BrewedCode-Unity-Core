#if MOREMOUNTAINS_TOOLS
using System.Collections.Generic;
using BrewedCode.Logging;
using BrewedCode.Signals;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.Events;
#if CINEMACHINE
using Cinemachine;
#endif

// Uncomment if you use TDE in your project to pull the InputManager from there
// #define MOREMOUNTAINS_TOPDOWNENGINE

/// Detects in which PATH(s) (areas) of a single PolygonCollider2D a point falls.
namespace BrewedCode.Utils.MoreMountains
{
    public class PolygonPathQuery : MMMonoBehaviour
    {
        private ILog? _logger;

        // ---------- Types & Config ----------
        public enum SourceMode
        {
            Mouse,
            Transform
        }

        public enum GizmoMode
        {
            Off,
            SelectedOnly,
            Always
        }

        [GroupPreset(InspectorGroupPreset.Settings)]
        public SourceMode sourceMode = SourceMode.Mouse;

        [Tooltip("Transform to sample when SourceMode = Transform")]
        [MMEnumCondition("sourceMode", (int)SourceMode.Transform)]
        public Transform targetTransform;

        [Header("World Camera")]
        [Tooltip("If empty, tries Camera.main and, if present, CinemachineBrain.OutputCamera.")]
        public Camera worldCamera;

        [Header("Paths Collider")]
        [Tooltip("PolygonCollider2D with multiple paths (e.g., 8 sectors).")]
        public PolygonCollider2D polygon;

        [Header("Target Layers (optional)")]
        [Tooltip("If true, validates the target GameObject's Layer on assignment and/or at runtime.")]
        [MMEnumCondition("sourceMode", (int)SourceMode.Transform)]
        public bool respectTargetLayers = false;

        [Tooltip("Allowed layers for the target GameObject (when SourceMode = Transform).")]
        [MMEnumCondition("sourceMode", (int)SourceMode.Transform)]
        public LayerMask allowedTargetLayers = ~0;

        [Tooltip("Validate the layer at every check as well (not only on assignment).")]
        [MMEnumCondition("sourceMode", (int)SourceMode.Transform)]
        public bool validateLayersEveryCheck = false;

        [Header("Activity")]
        [Tooltip("If false, automatic checking in Update is disabled.")]
        public bool checkActive = true;

        [Tooltip("If true, skips Update checks; call ForceCheck() manually.")]
        public bool manualChecksOnly = false;

        [GroupPreset(InspectorGroupPreset.Debug)]
        [Header("Gizmos")]
        public GizmoMode gizmoMode = GizmoMode.SelectedOnly;
        public bool drawIndices = true;
        public Color gizmoColor = new(0f, 1f, 1f, 0.85f);
        public Color highlightColor = new(1f, 0.8f, 0.2f, 1f);
        public int highlightPathIndex = -1; // optional debug

        [Header("Debug")]
        public bool highlightHitPathIndex = true; // optional debug
        [SerializeField, MMReadOnly] private string hitsPreview = "—";
        public Color highlightHitColor = new(1f, 0f, 0.2f, 1f);

        // ---------- State ----------
        [Tooltip("Current primary index (first matched path) or -1.")]
        public int CurrentPrimaryPathIndex { get; private set; } = -1;

        /// <summary>Immutable list with ALL paths hit by the last check.</summary>
        public IReadOnlyList<int> CurrentHitPaths => _lastHits;

        // ---------- Events (Signals) ----------
        /// <summary>Invoked EVERY time a check is performed (hit or not).</summary>
        public Signal<Vector2, IReadOnlyList<int>> OnChecked = new();

        /// <summary>Invoked when there is at least 1 hit (provides all indices).</summary>
        public Signal<Vector2, IReadOnlyList<int>> OnHit = new();

        /// <summary>Invoked when there are no hits.</summary>
        public Signal<Vector2> OnNoHit = new();

        /// <summary>Invoked when the primary path changes (oldIndex, newIndex).</summary>
        public Signal<int, int> OnPrimaryPathChanged = new();

        // ---------- Internals ----------
        private readonly List<Vector2[]> _paths = new();
        private int _cachedPathCount = -1;
        private readonly List<int> _hitsBuffer = new();
        private readonly List<int> _lastHits = new();
        private Vector3 _lastScreenPos; // saves checks when mouse is idle

        // ---------- Lifecycle ----------
        void Reset()
        {
            if (!polygon) polygon = GetComponent<PolygonCollider2D>();
        }

        void Awake()
        {
            try
            {
                _logger = LoggingRoot.Instance.Service.GetLogger(nameof(PolygonPathQuery));
            }
            catch
            {
                _logger = null;
            }

            if (!polygon) polygon = GetComponent<PolygonCollider2D>();

            if (!worldCamera)
            {
                worldCamera = Camera.main;
#if CINEMACHINE
            if (worldCamera == null)
            {
                var brain = FindObjectOfType<CinemachineBrain>();
                if (brain != null) worldCamera = brain.OutputCamera;
            }
#endif
            }

            RebuildCache();

            // Initial layer validation (if applicable)
            if (!respectTargetLayers || sourceMode != SourceMode.Transform || !targetTransform) return;
            if (!IsLayerAllowed(targetTransform.gameObject.layer))
            {
                _logger?.WarningSafe(
                    $"[{nameof(PolygonPathQuery)}] Target '{targetTransform.name}' (layer {LayerMask.LayerToName(targetTransform.gameObject.layer)}) is not in allowedTargetLayers.");
            }
        }


#if UNITY_EDITOR
        void OnValidate()
        {
            if (!polygon) polygon = GetComponent<PolygonCollider2D>();
            if (polygon)
                RebuildCache();
        }
#endif

        void Update()
        {
            if (!checkActive || manualChecksOnly) return;

            if (sourceMode == SourceMode.Mouse && Application.isPlaying)
            {
                Vector3 currentScreen = GetMouseScreenPosition();
                if (currentScreen == _lastScreenPos) return;
                _lastScreenPos = currentScreen;
            }

            ForceCheck();
        }

        // ---------- Public API ----------
        /// <summary>Performs a check immediately (even if manualChecksOnly = true).</summary>
        public void ForceCheck()
        {
            if (!polygon || !worldCamera) return;

            // World point based on the source
            if (!TryGetWorldPoint(out var worldPoint)) return;

            // Optional runtime layer validation
            if (respectTargetLayers && validateLayersEveryCheck && sourceMode == SourceMode.Transform &&
                targetTransform != null)
            {
                if (!IsLayerAllowed(targetTransform.gameObject.layer))
                {
                    // no hits — publish
                    PublishResult(worldPoint, noHit: true);
                    return;
                }
            }

            // Re-cache if paths changed (edit-time)
            if (polygon.pathCount != _cachedPathCount)
                RebuildCache();

            // Collect ALL paths that contain the point
            _hitsBuffer.Clear();
            Vector2 local = polygon.transform.InverseTransformPoint(worldPoint);

            for (int i = 0; i < _paths.Count; i++)
            {
                var path = _paths[i];
                if (path == null || path.Length < 3) continue;
                if (PointInPolygon(local, path))
                    _hitsBuffer.Add(i);
            }

            // Update state + raise events
            ApplyHitsAndPublish(worldPoint, _hitsBuffer);
        }

        /// <summary>Assigns the target Transform with optional layer validation. Returns true if accepted.</summary>
        public bool SetTargetTransform(Transform t, bool warnIfRejected = true)
        {
            targetTransform = t;
            if (respectTargetLayers && t != null)
            {
                if (!IsLayerAllowed(t.gameObject.layer))
                {
                    if (warnIfRejected)
                        _logger?.WarningSafe(
                            $"[{nameof(PolygonPathQuery)}] Target '{t.name}' (layer {LayerMask.LayerToName(t.gameObject.layer)}) is not in allowedTargetLayers.");
                    // Keep the target; just warn — or uncomment next line if you prefer rejecting:
                    // targetTransform = null; return false;
                }
            }

            return true;
        }

        /// <summary>Enables/disables automatic Update checks.</summary>
        public void SetCheckActive(bool active) => checkActive = active;

        /// <summary>Returns all path indices that would contain a specific WORLD point.</summary>
        public int[] QueryPathsAtPoint(Vector2 worldPoint)
        {
            if (polygon == null) return System.Array.Empty<int>();
            if (polygon.pathCount != _cachedPathCount) RebuildCache();

            List<int> outList = new List<int>(polygon.pathCount);
            Vector2 local = polygon.transform.InverseTransformPoint(worldPoint);
            for (int i = 0; i < _paths.Count; i++)
            {
                var path = _paths[i];
                if (path == null || path.Length < 3) continue;
                if (PointInPolygon(local, path)) outList.Add(i);
            }

            return outList.ToArray();
        }

        /// <summary>Centroid (WORLD) of a path.</summary>
        public Vector3 GetPathCentroidWorld(int pathIndex)
        {
            if (polygon == null || pathIndex < 0 || pathIndex >= polygon.pathCount) return transform.position;
            var path = polygon.GetPath(pathIndex);
            var cLocal = Centroid(path);
            return polygon.transform.TransformPoint(cLocal);
        }

        // ---------- Internals: hits & events ----------
        private void ApplyHitsAndPublish(Vector2 worldPoint, List<int> hits)
        {
            // Update immutable exposure list
            _lastHits.Clear();
            _lastHits.AddRange(hits);

            // Primary path = first in the list (or -1)
            int newPrimary = (hits.Count > 0) ? hits[0] : -1;

            // Always event
            OnChecked?.Raise(worldPoint, _lastHits);

            if (hits.Count > 0)
            {
                // Primary changed?
                if (newPrimary != CurrentPrimaryPathIndex)
                {
                    int old = CurrentPrimaryPathIndex;
                    CurrentPrimaryPathIndex = newPrimary;
                    OnPrimaryPathChanged?.Raise(old, newPrimary);
                }

                OnHit?.Raise(worldPoint, _lastHits);
            }
            else
            {
                // No hits
                if (CurrentPrimaryPathIndex != -1)
                {
                    int old = CurrentPrimaryPathIndex;
                    CurrentPrimaryPathIndex = -1;
                    OnPrimaryPathChanged?.Raise(old, -1);
                }

                OnNoHit?.Raise(worldPoint);
            }
        }

        private void PublishResult(Vector2 worldPoint, bool noHit)
        {
            _lastHits.Clear();
            if (noHit)
            {
                if (CurrentPrimaryPathIndex != -1)
                {
                    int old = CurrentPrimaryPathIndex;
                    CurrentPrimaryPathIndex = -1;
                    OnPrimaryPathChanged?.Raise(old, -1);
                }

                OnChecked?.Raise(worldPoint, _lastHits);
                OnNoHit?.Raise(worldPoint);
            }
        }

        // ---------- Internals: utilities ----------

        private Vector3 GetMouseScreenPosition()
        {
            Vector3 screen;
#if MOREMOUNTAINS_TOPDOWNENGINE
            screen = InputManager.Instance.MousePosition;
#else
        screen = Input.mousePosition;
#endif
            return worldCamera.ScreenToWorldPoint(screen);
        }

        private bool TryGetWorldPoint(out Vector2 worldPoint)
        {
            if (sourceMode == SourceMode.Transform)
            {
                if (!targetTransform)
                {
                    worldPoint = default;
                    return false;
                }

                worldPoint = targetTransform.position;
                return true;
            }
            else // Mouse
            {
                var w = GetMouseScreenPosition();
                worldPoint = new Vector2(w.x, w.y);
                return true;
            }
        }

        private bool IsLayerAllowed(int layer)
            => (allowedTargetLayers.value & (1 << layer)) != 0;

        /// Rebuilds and stores all paths from the PolygonCollider2D (in local space).
        public void RebuildCache()
        {
            _paths.Clear();
            if (!polygon) return;

            _cachedPathCount = polygon.pathCount;
            for (int i = 0; i < _cachedPathCount; i++)
            {
                var path = polygon.GetPath(i); // local space
                _paths.Add(path);
            }
        }

        // Point-in-polygon (even-odd rule), points must be in local space.
        private static bool PointInPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                var pi = poly[i];
                var pj = poly[j];

                bool intersect = ((pi.y > p.y) != (pj.y > p.y)) &&
                                 (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x);
                if (intersect) inside = !inside;
            }

            return inside;
        }

        private static Vector2 Centroid(Vector2[] v)
        {
            float area = 0f, cx = 0f, cy = 0f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
            {
                float cross = v[j].x * v[i].y - v[i].x * v[j].y;
                area += cross;
                cx += (v[j].x + v[i].x) * cross;
                cy += (v[j].y + v[i].y) * cross;
            }

            area *= 0.5f;
            if (Mathf.Abs(area) < 1e-6f)
            {
                Vector2 sum = Vector2.zero;
                foreach (var p in v) sum += p;
                return sum / v.Length;
            }

            return new Vector2(cx / (6 * area), cy / (6 * area));
        }

        // ---------- Gizmos ----------
        void OnDrawGizmos()
        {
            if (gizmoMode != GizmoMode.Always) return;
            DrawGizmosCommon();
        }

        void OnDrawGizmosSelected()
        {
            if (gizmoMode == GizmoMode.Off) return;
            if (gizmoMode == GizmoMode.SelectedOnly) DrawGizmosCommon();
        }

        private void DrawGizmosCommon()
        {
            if (!polygon) polygon = GetComponent<PolygonCollider2D>();
            if (!polygon) return;

            if (_cachedPathCount != polygon.pathCount || _paths.Count == 0)
                RebuildCache();

            var oldColor = Gizmos.color;

            for (int i = 0; i < _paths.Count; i++)
            {
                var path = _paths[i];
                if (path == null || path.Length < 2) continue;

                Gizmos.color = (i == highlightPathIndex) ? highlightColor : gizmoColor;

                if (highlightHitPathIndex && _lastHits.Contains(i))
                    Gizmos.color = highlightHitColor;

                for (int a = 0; a < path.Length; a++)
                {
                    var p0 = polygon.transform.TransformPoint(path[a]);
                    var p1 = polygon.transform.TransformPoint(path[(a + 1) % path.Length]);
                    Gizmos.DrawLine(p0, p1);
                }

#if UNITY_EDITOR
                if (drawIndices)
                {
                    var centroidLocal = Centroid(path);
                    var centroidWorld = polygon.transform.TransformPoint(centroidLocal);
                    UnityEditor.Handles.Label(centroidWorld, $"#{i}");
                }
#endif
            }

            Gizmos.color = oldColor;
        }

        void OnEnable()
        {
            RebuildCache();

#if UNITY_EDITOR
            OnHit.Subscribe(this, (_, paths) => hitsPreview = $"[{string.Join(", ", paths)}]");
            OnNoHit.Subscribe(this, _ => hitsPreview = "—");
#endif
        }
    }
}
#endif
