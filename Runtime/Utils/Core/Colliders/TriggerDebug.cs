using System.Text;
using System.Collections.Generic;
using BrewedCode.Logging;
using UnityEngine;

namespace BrewedCode.Utils
{
    /// <summary>
    /// TriggerDebug2D
    /// Attach to a GameObject that has a Collider2D with IsTrigger enabled.
    /// Logs detailed info about trigger interactions and common misconfigurations.
    /// </summary>
    [DisallowMultipleComponent]
    public class TriggerDebug2D : MonoBehaviour
    {
        private ILog? _logger;

        [Header("Logging")]
        [SerializeField] private bool logEnter = true;
        [SerializeField] private bool logStay = false;
        [SerializeField] private bool logExit = true;

        [Tooltip("If enabled, logs only the first time a root GameObject enters; avoids multi-collider spam.")]
        [SerializeField] private bool dedupeByRootObject = true;

        [Tooltip("If enabled, logs extra info about colliders and rigidbodies involved.")]
        [SerializeField] private bool verbose = true;

        [Header("Stay Logging")]
        [Tooltip("If logStay is enabled, log at most once every N seconds per object.")]
        [SerializeField, Min(0.01f)] private float stayLogCooldownSeconds = 0.5f;

        [Header("Filter (optional)")]
        [SerializeField] private LayerMask onlyLogTheseLayers = ~0; // Everything by default
        [SerializeField] private bool ignoreTriggersEntering = false;

        // Tracks how many colliders of the same root object are currently inside.
        private readonly Dictionary<int, int> _insideCountsByRootId = new();
        private readonly Dictionary<int, float> _lastStayLogTimeByRootId = new();

        private Collider2D _selfCollider2D;

        private void Awake()
        {
            try
            {
                _logger = LoggingRoot.Instance.Service.GetLogger(nameof(TriggerDebug2D));
            }
            catch
            {
                _logger = null;
            }

            _selfCollider2D = GetComponent<Collider2D>();

            if (_selfCollider2D == null)
            {
                _logger?.WarningSafe($"[TriggerDebug2D] {name} has no Collider2D. This script requires a Collider2D.");
                return;
            }

            if (!_selfCollider2D.isTrigger)
            {
                _logger?.WarningSafe($"[TriggerDebug2D] {name} Collider2D is NOT marked as IsTrigger. OnTrigger*2D won't fire.");
            }

            // Helpful "setup summary"
            _logger?.InfoSafe(BuildSelfSummary());
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!logEnter) return;
            if (!PassesFilter(other)) return;

            var root = GetRootObject(other);
            int rootId = root.GetInstanceID();

            // Multi-collider handling
            int prev = _insideCountsByRootId.TryGetValue(rootId, out int count) ? count : 0;
            _insideCountsByRootId[rootId] = prev + 1;

            if (dedupeByRootObject && prev > 0)
            {
                // Already "inside", just increment count, don't spam.
                return;
            }

            _logger?.InfoSafe(BuildEventLog("ENTER", other));
            WarnIfSuspiciousSetup(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!logStay) return;
            if (!PassesFilter(other)) return;

            var root = GetRootObject(other);
            int rootId = root.GetInstanceID();

            // Cooldown per root
            float now = Time.time;
            if (_lastStayLogTimeByRootId.TryGetValue(rootId, out float last) && (now - last) < stayLogCooldownSeconds)
                return;

            _lastStayLogTimeByRootId[rootId] = now;

            _logger?.InfoSafe(BuildEventLog("STAY", other));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!logExit) return;
            if (!PassesFilter(other)) return;

            var root = GetRootObject(other);
            int rootId = root.GetInstanceID();

            int prev = _insideCountsByRootId.TryGetValue(rootId, out int count) ? count : 0;
            int next = Mathf.Max(0, prev - 1);

            if (next == 0) _insideCountsByRootId.Remove(rootId);
            else _insideCountsByRootId[rootId] = next;

            if (dedupeByRootObject && next > 0)
            {
                // Still has other colliders inside, don't log exit yet.
                return;
            }

            _logger?.InfoSafe(BuildEventLog("EXIT", other));
        }

        // ---------------------------
        // Helpers
        // ---------------------------

        private bool PassesFilter(Collider2D other)
        {
            if (other == null) return false;

            if (ignoreTriggersEntering && other.isTrigger)
                return false;

            int otherLayerMaskBit = 1 << other.gameObject.layer;
            if ((onlyLogTheseLayers.value & otherLayerMaskBit) == 0)
                return false;

            return true;
        }

        private static GameObject GetRootObject(Collider2D other)
        {
            // Usually Character hierarchy has multiple child colliders; root keeps logs tidy
            return other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.transform.root.gameObject;
        }

        private string BuildSelfSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[TriggerDebug2D] READY on '{name}'");
            sb.AppendLine($"  Scene: {gameObject.scene.name}");
            sb.AppendLine($"  Path: {GetHierarchyPath(transform)}");
            sb.AppendLine($"  Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})  Tag: {gameObject.tag}");
            sb.AppendLine($"  Position: {transform.position}");

            var col = _selfCollider2D;
            if (col != null)
            {
                sb.AppendLine($"  Collider2D: {col.GetType().Name}  isTrigger={col.isTrigger}  enabled={col.enabled}");
                sb.AppendLine($"  Collider offset={col.offset} bounds.center={col.bounds.center} bounds.size={col.bounds.size}");
            }

            var rb2d = GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                sb.AppendLine($"  Rigidbody2D: bodyType={rb2d.bodyType} simulated={rb2d.simulated} vel={rb2d.linearVelocity}");
            }
            else
            {
                sb.AppendLine($"  Rigidbody2D: (none on trigger object)");
            }

            sb.AppendLine($"  Note: For 2D trigger callbacks, at least one of the two colliders must have a Rigidbody2D (simulated).");
            return sb.ToString();
        }

        private string BuildEventLog(string evt, Collider2D other)
        {
            var sb = new StringBuilder(512);

            var otherGO = other.gameObject;
            var rootGO = GetRootObject(other);

            sb.AppendLine($"[TriggerDebug2D] {evt} @ frame={Time.frameCount} time={Time.time:0.000}");
            sb.AppendLine($"  Trigger: '{name}'  layer={LayerMask.LayerToName(gameObject.layer)}({gameObject.layer}) tag={gameObject.tag}");
            sb.AppendLine($"  Trigger pos={transform.position} colliderIsTrigger={_selfCollider2D != null && _selfCollider2D.isTrigger}");

            sb.AppendLine($"  Other collider: '{otherGO.name}' ({other.GetType().Name}) isTrigger={other.isTrigger} enabled={other.enabled}");
            sb.AppendLine($"    other layer={LayerMask.LayerToName(otherGO.layer)}({otherGO.layer}) tag={otherGO.tag}");
            sb.AppendLine($"    other path={GetHierarchyPath(other.transform)}");

            sb.AppendLine($"  Root object: '{rootGO.name}' (id={rootGO.GetInstanceID()})");
            sb.AppendLine($"    root layer={LayerMask.LayerToName(rootGO.layer)}({rootGO.layer}) tag={rootGO.tag}");
            sb.AppendLine($"    root pos={rootGO.transform.position}");

            // Rigidbody2D details
            Rigidbody2D otherRB = other.attachedRigidbody;
            Rigidbody2D rootRB = rootGO.GetComponent<Rigidbody2D>();
            if (verbose)
            {
                sb.AppendLine($"  Rigidbody2D (other.attachedRigidbody): {(otherRB ? otherRB.bodyType.ToString() : "NONE")}");
                if (otherRB != null)
                    sb.AppendLine($"    simulated={otherRB.simulated} vel={otherRB.linearVelocity} angVel={otherRB.angularVelocity:0.###}");

                sb.AppendLine($"  Rigidbody2D (root): {(rootRB ? rootRB.bodyType.ToString() : "NONE")}");
                if (rootRB != null)
                    sb.AppendLine($"    simulated={rootRB.simulated} vel={rootRB.linearVelocity}");
            }

            // Bounds / overlap-ish hints
            if (_selfCollider2D != null)
            {
                sb.AppendLine($"  Bounds:");
                sb.AppendLine($"    trigger bounds center={_selfCollider2D.bounds.center} size={_selfCollider2D.bounds.size}");
                sb.AppendLine($"    other   bounds center={other.bounds.center} size={other.bounds.size}");
            }

            // How many colliders from this root are inside (helps multi-collider spam)
            int rootId = rootGO.GetInstanceID();
            int insideCount = _insideCountsByRootId.TryGetValue(rootId, out int c) ? c : 0;
            sb.AppendLine($"  InsideCount for root='{rootGO.name}': {insideCount}");

            // 3D mixing hint
            var otherCollider3D = otherGO.GetComponent<Collider>();
            var otherRB3D = otherGO.GetComponent<Rigidbody>();
            if (verbose && (otherCollider3D != null || otherRB3D != null))
            {
                sb.AppendLine($"  ⚠ 3D components detected on OTHER:");
                if (otherCollider3D != null) sb.AppendLine($"    Collider(3D): {otherCollider3D.GetType().Name}");
                if (otherRB3D != null) sb.AppendLine($"    Rigidbody(3D): isKinematic={otherRB3D.isKinematic}");
                sb.AppendLine($"    Note: 2D physics does NOT interact with 3D physics.");
            }

            return sb.ToString();
        }

        private void WarnIfSuspiciousSetup(Collider2D other)
        {
            // This is where we scream about the usual suspects.
            var selfRB = GetComponent<Rigidbody2D>();
            var otherRB = other.attachedRigidbody;

            bool hasRBInPair = (selfRB != null && selfRB.simulated) || (otherRB != null && otherRB.simulated);
            if (!hasRBInPair)
            {
                _logger?.WarningSafe($"[TriggerDebug2D] Suspicious: Neither trigger nor other collider seems to have a simulated Rigidbody2D. Trigger callbacks may not fire.");
            }

            if (_selfCollider2D != null && !_selfCollider2D.isTrigger)
            {
                _logger?.WarningSafe($"[TriggerDebug2D] Suspicious: Trigger collider is not set to IsTrigger.");
            }

            if (!gameObject.activeInHierarchy)
            {
                _logger?.WarningSafe($"[TriggerDebug2D] Suspicious: Trigger GameObject is not active in hierarchy.");
            }

            // Layer collision matrix check isn't directly queryable, but we can hint:
            // Physics2D.GetIgnoreLayerCollision only checks explicit ignore pairs (not full matrix UI),
            // still useful sometimes.
            int a = gameObject.layer;
            int b = other.gameObject.layer;
            if (Physics2D.GetIgnoreLayerCollision(a, b))
            {
                _logger?.WarningSafe($"[TriggerDebug2D] Layers are set to ignore collisions: {LayerMask.LayerToName(a)} x {LayerMask.LayerToName(b)} (Physics2D.GetIgnoreLayerCollision=true)");
            }
        }

        private static string GetHierarchyPath(Transform t)
        {
            if (t == null) return "<null>";
            var sb = new StringBuilder();
            sb.Append(t.name);
            while (t.parent != null)
            {
                t = t.parent;
                sb.Insert(0, t.name + "/");
            }
            return sb.ToString();
        }
    }
}
