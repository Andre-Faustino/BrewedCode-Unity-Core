using UnityEngine;

namespace BrewedCode.Theme
{
    [CreateAssetMenu(menuName = "Theme/UI/ComponentTheme")]
    public class ComponentTheme : ScriptableObject
    {
        [System.Serializable] public struct StateColor
        {
            public string state;
            public string colorTokenPath;
        }

        public string componentId; // ex: "Button/Primary"
        public StateColor[] colors; // Normal/Hover/Pressed/Disabled
    }
}