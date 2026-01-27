#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BrewedCode.Theme.Editor
{
    [CustomEditor(typeof(TMPUnderlayStyleBinding))]
    public class TMPUnderlayStyleBindingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            if (GUILayout.Button("Preview Underlay", GUILayout.Height(30)))
            {
                var binding = (TMPUnderlayStyleBinding)target;
                binding.ApplyUnderlay();
            }

            EditorGUILayout.HelpBox(
                "Editor preview is now on-demand via the Preview button above. " +
                "Live updates no longer run automatically in the editor.",
                MessageType.Info);
        }
    }
}
#endif
