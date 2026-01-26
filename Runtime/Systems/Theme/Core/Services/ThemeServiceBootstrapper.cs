using UnityEngine;

namespace BrewedCode.Theme
{
    /// <summary>
    /// Single point of access to IThemeService for legacy code.
    /// New code should use dependency injection instead of this static accessor.
    /// </summary>
    public static class ThemeServiceBootstrapper
    {
        private static IThemeService? _instance;

        /// <summary>
        /// Gets the global IThemeService instance.
        /// Returns null if no ThemeService exists in the scene.
        /// </summary>
        public static IThemeService? Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var themeService = Object.FindFirstObjectByType<ThemeService>();
                if (themeService != null)
                {
                    _instance = themeService;
                }

                return _instance;
            }
        }

        /// <summary>
        /// Explicitly set the theme service instance.
        /// Called automatically by ThemeService.OnEnable() in future versions.
        /// </summary>
        internal static void Register(IThemeService service)
        {
            _instance = service;
        }

        /// <summary>
        /// Clear the instance reference.
        /// Called automatically by ThemeService.OnDisable() in future versions.
        /// </summary>
        internal static void Unregister()
        {
            _instance = null;
        }
    }
}
