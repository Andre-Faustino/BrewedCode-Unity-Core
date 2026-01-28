#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace BrewedCode.Theme
{
    public class ThemeProfile : ScriptableObject
    {
        public RawPalette? raw;
        public UiTokens? ui;
        public ComponentTheme[] components;

        public bool highContrast;
        public bool colorVisionSafe;
        [Range(0.85f, 1.5f)] public float defaultFontScale = 1f;

#if UNITY_EDITOR
        [ContextMenu("Validate Profile")]
        public void ValidateProfile()
        {
            var result = ThemeValidator.Validate(this);
            result.PrintToConsole();
        }
#endif
    }
}