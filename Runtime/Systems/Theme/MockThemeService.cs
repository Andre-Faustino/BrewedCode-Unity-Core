using System;
using UnityEngine;

namespace BrewedCode.Theme.Tests
{
    /// <summary>
    /// Mock implementation of IThemeService for testing.
    /// Allows full control over behavior without needing real theme assets.
    /// </summary>
    public class MockThemeService : MonoBehaviour, IThemeService
    {
        public ThemeProfile CurrentProfile;
        private float _fontScale = 1.0f;

        public event Action OnThemeChanged;

        public ThemeProfile Current => CurrentProfile;

        public float FontScale
        {
            get => _fontScale;
            set => _fontScale = Mathf.Clamp01(value);
        }

        public void SetTheme(ThemeProfile profile)
        {
            CurrentProfile = profile;
            OnThemeChanged?.Invoke();
        }

        public Color ResolveColor(string path, IThemeResolver scope = null)
        {
            // Mock always returns a test color
            return new Color(1, 0, 0, 1); // Red
        }

        public bool TryResolveTypography(string path, out UiTokens.TypographyToken token, IThemeResolver scope = null)
        {
            // Mock returns default token
            token = default;
            return false;
        }

        public void NotifyThemeChanged()
        {
            OnThemeChanged?.Invoke();
        }
    }
}
