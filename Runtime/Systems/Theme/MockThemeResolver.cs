using UnityEngine;

namespace BrewedCode.Theme.Tests
{
    /// <summary>
    /// Mock implementation of IThemeResolver for testing.
    /// </summary>
    public class MockThemeResolver : MonoBehaviour, IThemeResolver
    {
        public ThemeProfile localProfile;
        public Color colorOverride;
        public bool shouldReturnColorOverride;

        public UiTokens.TypographyToken typographyOverride;
        public bool shouldReturnTypographyOverride;

        public ThemeProfile LocalProfile => localProfile;

        public bool TryResolveColor(string path, out Color color)
        {
            if (shouldReturnColorOverride)
            {
                color = colorOverride;
                return true;
            }

            color = default;
            return false;
        }

        public bool TryResolveTypography(string path, out UiTokens.TypographyToken token)
        {
            if (shouldReturnTypographyOverride)
            {
                token = typographyOverride;
                return true;
            }

            token = default;
            return false;
        }
    }
}
