using TMPro;
using UnityEngine;

namespace BrewedCode.Theme
{
    [CreateAssetMenu(menuName = "Theme/UI/UiTokens")]
    public class UiTokens : ScriptableObject
    {
        [System.Serializable] public struct ColorToken
        {
            public string path;
            public string rawRef;
            [Range(0f, 1f)] public float alpha;
            public bool inheritRawAlpha;
            
            public ColorToken(string path, string rawRef)
            {
                this.path = path;
                this.rawRef = rawRef;
                alpha = 1f;
                inheritRawAlpha = false;
            }
        }

        [System.Serializable] public struct TypographyToken
        {
            public string path;
            public TMP_FontAsset font;
            [Min(1)] public int size;                 // em px
            public float lineSpacing;                 // + / - em relação ao default da fonte
            public float characterSpacing;            // tracking
            public float wordSpacing;
            public float paragraphSpacing;
            public FontStyles fontStyle;              // Bold/Italic/SmallCaps etc.
            public bool allCaps;                      // opcional
        }

        public ColorToken[] colors; // ex: "Text/Primary" -> "Gray100"
        public TypographyToken[] typography; // ex: "Body/Sm" -> TMP + 16
    }
}