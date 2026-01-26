using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrewedCode.Utils
{
    [ExecuteAlways]
    public class SceneRefHelper : MonoBehaviour
    {
        [Header("Play Mode")]
        [Tooltip("Disables this GameObject when the game starts.")]
        public bool disableOnPlay = true;

        [Header("Gizmos")]
        public bool drawGizmos = true;
        public Color gizmoColor = new Color(1f, .6f, 0f, .4f);
        [Tooltip("Draws a cube on the ground with the size of the SpriteRenderer/Collider.")]
        public bool gizmoFromBounds = true;
        public float radius = 0.5f;
        public string label = "Ref";

        [Header("Validation")]
        public bool forceLayer = false;
        public string targetLayer = "Default";
        public bool forceSortingLayer = false;
        public string sortingLayerName = "Default";
        public int orderInLayer = 0;

        // --- Runtime/Editor lifecycle ---
        private void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            if (disableOnPlay)
                gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (!Application.isPlaying)
                ValidateNow();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                ValidateNow();
        }

        private void ValidateNow()
        {
            // Layer
            if (forceLayer && !string.IsNullOrEmpty(targetLayer))
            {
                int idx = LayerMask.NameToLayer(targetLayer);
                if (idx >= 0 && gameObject.layer != idx)
                    gameObject.layer = idx;
            }

            // Sorting layer / order (if SpriteRenderer exists)
            if (forceSortingLayer)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (!string.IsNullOrEmpty(sortingLayerName))
                        sr.sortingLayerName = sortingLayerName;
                    sr.sortingOrder = orderInLayer;
                }
            }
        }

        // --- Gizmos ---
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Gizmos.color = gizmoColor;

            Bounds b = new Bounds(transform.position, Vector3.one * radius * 2f);

            var sr = GetComponent<SpriteRenderer>();
            if (gizmoFromBounds && sr != null && sr.sprite != null)
                b = sr.bounds;

            var col = GetComponent<Collider>();
            if (gizmoFromBounds && col != null)
                b = col.bounds;

            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(b.center, b.size);

#if UNITY_EDITOR
            Handles.Label(b.center + Vector3.up * 0.2f, string.IsNullOrEmpty(label) ? name : label);
#endif
        }

        // --- Handy Context Menus in the Inspector (⋮) ---
        [ContextMenu("Snap To Grid (0.5)")]
        private void SnapToGrid05()
        {
            var p = transform.position;
            transform.position = new Vector3(
                Mathf.Round(p.x * 2f) / 2f,
                Mathf.Round(p.y * 2f) / 2f,
                Mathf.Round(p.z * 2f) / 2f
            );
        }

        [ContextMenu("Center On Parent")]
        private void CenterOnParent()
        {
            if (transform.parent != null)
                transform.position = transform.parent.position;
        }

        [ContextMenu("Toggle Children Renderers")]
        private void ToggleChildrenRenderers()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.enabled = !r.enabled;
        }

        [ContextMenu("Ping In Project (Sprite/Material)")]
        private void PingMainAsset()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr && sr.sprite) EditorGUIUtility.PingObject(sr.sprite);
            var mr = GetComponent<Renderer>();
            if (mr && mr.sharedMaterial) EditorGUIUtility.PingObject(mr.sharedMaterial);
        }

        [ContextMenu("Select Parent")]
        private void SelectParent()
        {
            if (transform.parent) Selection.activeTransform = transform.parent;
        }
#endif
    }
}
