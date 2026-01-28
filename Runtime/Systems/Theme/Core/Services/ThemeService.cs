// Assets/Systems/Theming/Runtime/ThemeService.cs

using System;
using System.Collections.Generic;
using UnityEngine;
using BrewedCode.Logging;

namespace BrewedCode.Theme
{
    [DefaultExecutionOrder(-100)]
    public sealed class ThemeService : MonoBehaviour, IThemeService
    {
        [SerializeField] private ThemeProfile? current;
        [Range(0.75f, 1.75f)] [SerializeField] private float fontScale = 1.0f;

        private ILog? _logger;

        public event Action? OnThemeChanged;

        // Profile lookups (built on SetTheme)
        private readonly Dictionary<string, UiTokens.ColorToken> _colorTokenLookup = new();
        private readonly Dictionary<string, UiTokens.TypographyToken> _typographyTokenLookup = new();
        private readonly Dictionary<string, RawPalette.Swatch> _swatchLookup = new();

        // Resolved value caches
        private readonly Dictionary<string, Color> _colorCache = new();
        private readonly Dictionary<string, UiTokens.TypographyToken> _typoCache = new();

        public ThemeProfile? Current => current;
        public float FontScale
        {
            get => fontScale;
            set
            {
                var newScale = Mathf.Clamp(value, 0.75f, 1.75f);
                if (!Mathf.Approximately(fontScale, newScale))
                {
                    _logger.InfoSafe($"FontScale changed: {fontScale} → {newScale}");
                    fontScale = newScale;
                }
            }
        }

        private void OnEnable()
        {
            InitializeLogger();

            // Register with bootstrapper for legacy access
            ThemeServiceBootstrapper.Register(this);

            if (current)
            {
                RebuildLookups();
                if (Mathf.Approximately(fontScale, 1.0f))
                    fontScale = current.defaultFontScale;
                _logger.InfoSafe($"ThemeService initialized with profile: {current.name}");
            }
        }

        private void OnDisable()
        {
            ThemeServiceBootstrapper.Unregister();
        }

        private void InitializeLogger()
        {
            try
            {
                _logger = LoggingRoot.Instance.Service.GetLogger(nameof(ThemeService));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ThemeService] Failed to initialize logger: {ex}");
                _logger = null;
            }
        }

        public void SetTheme(ThemeProfile profile)
        {
            if (profile == current) return;

            var oldName = current?.name ?? "null";
            current = profile;
            _logger.InfoSafe($"Theme changed: {oldName} → {profile?.name}");
            RebuildLookups();
            _colorCache.Clear();
            _typoCache.Clear();
            OnThemeChanged?.Invoke();
        }

        private void RebuildLookups()
        {
            _colorTokenLookup.Clear();
            _typographyTokenLookup.Clear();
            _swatchLookup.Clear();

            if (current == null)
            {
                _logger.WarningSafe("RebuildLookups called with null profile");
                return;
            }

            int colorCount = 0;
            int typoCount = 0;
            int swatchCount = 0;

            // Build color token lookup
            if (current.ui?.colors != null)
            {
                foreach (var token in current.ui.colors)
                {
                    if (!string.IsNullOrEmpty(token.path))
                    {
                        _colorTokenLookup[token.path] = token;
                        colorCount++;
                    }
                }
            }

            // Build typography token lookup
            if (current.ui?.typography != null)
            {
                foreach (var token in current.ui.typography)
                {
                    if (!string.IsNullOrEmpty(token.path))
                    {
                        _typographyTokenLookup[token.path] = token;
                        typoCount++;
                    }
                }
            }

            // Build swatch lookup
            if (current.raw?.swatches != null)
            {
                foreach (var swatch in current.raw.swatches)
                {
                    if (!string.IsNullOrEmpty(swatch.name))
                    {
                        _swatchLookup[swatch.name] = swatch;
                        swatchCount++;
                    }
                }
            }

            _logger.InfoSafe($"Theme lookups rebuilt: {colorCount} colors, {typoCount} typography, {swatchCount} swatches (profile: {current.name})");
        }

        // --- Helpers de resolução (consideram contexto) ---

        public Color ResolveColor(string path, IThemeResolver? scope = null)
        {
            // 1) Override explícito no escopo (já vem com alpha certo)
            if (scope != null && scope.TryResolveColor(path, out var cOverride)) return cOverride;

            // If scope has local profile, resolve using its data
            if (scope?.LocalProfile && scope.LocalProfile != current)
            {
                return ResolveColorFromProfile(path, scope.LocalProfile);
            }

            // Use cache for the current profile
            if (_colorCache.TryGetValue(path, out var cached)) return cached;

            // Resolve using the current profile
            var resolved = ResolveColorFromProfile(path, current);
            _colorCache[path] = resolved;
            return resolved;
        }

        private Color ResolveColorFromProfile(string path, ThemeProfile? profile)
        {
            if (profile == null)
            {
                _logger.WarningSafe($"ResolveColor failed: profile is null (path: {path})");
                return Color.white;
            }

            // Use lookup dictionary instead of Array.Find
            if (!_colorTokenLookup.TryGetValue(path, out var colorToken))
            {
                _logger.WarningSafe($"ResolveColor failed: token not found (path: {path})");
                return Color.white;
            }

            if (string.IsNullOrEmpty(colorToken.rawRef))
            {
                _logger.WarningSafe($"ResolveColor failed: rawRef is empty (path: {path})");
                return Color.white;
            }

            if (!_swatchLookup.TryGetValue(colorToken.rawRef, out var swatch))
            {
                _logger.WarningSafe($"ResolveColor failed: swatch not found (path: {path}, rawRef: {colorToken.rawRef})");
                return Color.white;
            }

            var baseColor = swatch.color;

            float finalA = colorToken.inheritRawAlpha
                ? baseColor.a * Mathf.Clamp01(colorToken.alpha)
                : Mathf.Clamp01(colorToken.alpha <= 0f && baseColor.a > 0f ? baseColor.a : colorToken.alpha);

            baseColor.a = finalA;
            return baseColor;
        }

        public bool TryResolveTypography(string path, out UiTokens.TypographyToken token, IThemeResolver? scope = null)
        {
            // 1) Override explícito
            if (scope != null && scope.TryResolveTypography(path, out token)) return true;

            // If scope has local profile, resolve using its data
            if (scope?.LocalProfile != null && scope.LocalProfile != current)
            {
                return TryResolveTypographyFromProfile(path, scope.LocalProfile, out token);
            }

            // Use cache for the current profile
            if (_typoCache.TryGetValue(path, out var cached))
            {
                token = cached;
                return true;
            }

            // Resolve using the current profile
            if (TryResolveTypographyFromProfile(path, current, out token))
            {
                _typoCache[path] = token;
                return true;
            }

            return false;
        }

        private bool TryResolveTypographyFromProfile(string path, ThemeProfile? profile, out UiTokens.TypographyToken token)
        {
            if (profile == null)
            {
                _logger.WarningSafe($"TryResolveTypography failed: profile is null (path: {path})");
                token = default;
                return false;
            }

            // Use lookup dictionary instead of Array.Find
            if (_typographyTokenLookup.TryGetValue(path, out token))
            {
                if (!string.IsNullOrEmpty(token.path))
                    return true;
                _logger.WarningSafe($"TryResolveTypography failed: token path is empty (path: {path})");
                return false;
            }

            _logger.TraceSafe($"TryResolveTypography: token not found (path: {path})");
            token = default;
            return false;
        }
        
        public void NotifyThemeChanged()
        {
            _logger.InfoSafe($"Theme change notification received (profile: {current?.name ?? "null"})");
            RebuildLookups();
            _colorCache.Clear();
            _typoCache.Clear();
            OnThemeChanged?.Invoke();
            _logger.TraceSafe("Theme change notification processed");
        }
    }
}