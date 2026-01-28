namespace BrewedCode.Theme
{
    public static class ThemeValidator
    {
        public static ThemeValidationResult Validate(ThemeProfile profile)
        {
            var result = new ThemeValidationResult();
            string name = profile.name;

            // Validar RawPalette
            var rawNames = RawPaletteValidator.Validate(profile.raw, result, name);

            // Validar UiTokens
            var tokenPaths = UiTokensValidator.Validate(profile.ui, rawNames, result, name);

            // Validar ComponentThemes
            ComponentThemeValidator.Validate(profile.components, tokenPaths, result, name);

            return result;
        }
    }
}