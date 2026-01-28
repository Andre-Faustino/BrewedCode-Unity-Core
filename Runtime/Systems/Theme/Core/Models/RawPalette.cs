using UnityEngine;

namespace BrewedCode.Theme
{
    [CreateAssetMenu(menuName = "Theme/Core/RawPalette")]
    public class RawPalette : ScriptableObject
    {
        [System.Serializable] public struct Swatch
        {
            public string name;
            public Color color;
        }

        public Swatch[] swatches; // Cyan500, Gray900, etc.
    }
}