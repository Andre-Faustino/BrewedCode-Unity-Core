using System.Collections.Generic;

namespace BrewedCode.Theme
{
    public static class RawPaletteValidator
    {
        public static HashSet<string> Validate(RawPalette palette, ThemeValidationResult result, string profileName)
        {
            var set = new HashSet<string>();

            if (!palette)
            {
                result.AddError($"[{profileName}] RawPalette is NULL.");
                return set;
            }

            foreach (var sw in palette.swatches)
            {
                if (string.IsNullOrWhiteSpace(sw.name))
                {
                    result.AddWarning($"[{profileName}] RawPalette '{palette.name}' has EMPTY swatch name.");
                    continue;
                }

                if (!set.Add(sw.name))
                {
                    result.AddWarning($"[{profileName}] RawPalette '{palette.name}' has DUPLICATE name '{sw.name}'.");
                }
            }

            if (set.Count == 0)
                result.AddWarning($"[{profileName}] RawPalette '{palette.name}' has NO swatches.");

            return set;
        }
    }
}