using System.Collections.Generic;
using UnityEngine;

namespace BrewedCode.Theme
{
    public interface IThemeResolver
    {
        ThemeProfile LocalProfile { get; }
        bool TryResolveColor(string path, out Color color);
        bool TryResolveTypography(string path, out UiTokens.TypographyToken ty);
    }

    public sealed class ThemeContext : MonoBehaviour, IThemeResolver
    {
        [Header("Local Profile (escopo)")]
        [SerializeField] private ThemeProfile? localProfile;

        [System.Serializable] public struct ColorOverride
        {
            public string path;
            public Color color;
        }

        [System.Serializable] public struct TypographyOverride
        {
            public string path;
            public UiTokens.TypographyToken token;
        }

        [Header("Overrides (opcionais)")]
        [SerializeField] private ColorOverride[] colorOverrides;
        [SerializeField] private TypographyOverride[] typographyOverrides;

        // Lazy-built lookup caches
        private Dictionary<string, Color>? _colorOverrideLookup;
        private Dictionary<string, UiTokens.TypographyToken>? _typographyOverrideLookup;

        public ThemeProfile? LocalProfile => localProfile;

        private void OnEnable()
        {
            // Rebuild lookups when enabled
            RebuildLookups();
        }

        private void RebuildLookups()
        {
            _colorOverrideLookup = new Dictionary<string, Color>();
            _typographyOverrideLookup = new Dictionary<string, UiTokens.TypographyToken>();

            if (colorOverrides != null)
            {
                foreach (var @override in colorOverrides)
                {
                    if (!string.IsNullOrEmpty(@override.path))
                        _colorOverrideLookup[@override.path] = @override.color;
                }
            }

            if (typographyOverrides != null)
            {
                foreach (var @override in typographyOverrides)
                {
                    if (!string.IsNullOrEmpty(@override.path))
                        _typographyOverrideLookup[@override.path] = @override.token;
                }
            }
        }

        public bool TryResolveColor(string path, out Color c)
        {
            if (_colorOverrideLookup == null)
                RebuildLookups();

            return _colorOverrideLookup!.TryGetValue(path, out c);
        }

        public bool TryResolveTypography(string path, out UiTokens.TypographyToken ty)
        {
            if (_typographyOverrideLookup == null)
                RebuildLookups();

            return _typographyOverrideLookup!.TryGetValue(path, out ty);
        }
    }
}