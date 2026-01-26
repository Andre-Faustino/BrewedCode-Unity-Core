using System;
using UnityEngine;

namespace BrewedCode.Theme
{
    public interface IThemeService
    {
        /// <summary>
        /// Gets the currently active theme profile.
        /// </summary>
        ThemeProfile Current { get; }

        /// <summary>
        /// Global font scale multiplier for accessibility (0.75 - 1.75).
        /// </summary>
        float FontScale { get; set; }

        /// <summary>
        /// Fired when the theme changes (SetTheme or NotifyThemeChanged).
        /// </summary>
        event Action OnThemeChanged;

        /// <summary>
        /// Changes the active theme profile and clears caches.
        /// </summary>
        void SetTheme(ThemeProfile profile);

        /// <summary>
        /// Resolves a color token path to its final color value.
        /// Considers local scope overrides if provided.
        /// </summary>
        Color ResolveColor(string path, IThemeResolver? scope = null);

        /// <summary>
        /// Attempts to resolve a typography token path to its token value.
        /// Considers local scope overrides if provided.
        /// </summary>
        bool TryResolveTypography(string path, out UiTokens.TypographyToken token, IThemeResolver? scope = null);

        /// <summary>
        /// Manually triggers a theme changed event and clears internal caches.
        /// Useful when theme assets are modified at runtime.
        /// </summary>
        void NotifyThemeChanged();
    }
}
