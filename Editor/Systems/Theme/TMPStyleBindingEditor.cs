#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BrewedCode.Theme.Editor
{
    [CustomEditor(typeof(TMPStyleBinding))]
    public class TMPStyleBindingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            if (GUILayout.Button("Preview Theme", GUILayout.Height(30)))
            {
                var binding = (TMPStyleBinding)target;
                binding.ApplyAll();
            }
        }
    }
}
#endif
