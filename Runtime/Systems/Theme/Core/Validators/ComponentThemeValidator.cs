using System.Collections.Generic;

namespace BrewedCode.Theme
{
    public static class ComponentThemeValidator
    {
        public static void Validate(
            ComponentTheme[] components,
            HashSet<string> colorTokenPaths,
            ThemeValidationResult result,
            string profileName
        )
        {
            if (components.Length == 0)
            {
                result.AddWarning($"[{profileName}] No ComponentThemes assigned.");
                return;
            }

            foreach (var c in components)
            {
                if (!c)
                {
                    result.AddWarning($"[{profileName}] ComponentTheme is NULL.");
                    continue;
                }

                string compName = string.IsNullOrWhiteSpace(c.componentId) ? c.name : c.componentId;

                if (c.colors.Length == 0)
                {
                    result.AddWarning($"[{profileName}] ComponentTheme '{compName}' has NO state colors.");
                    continue;
                }

                foreach (var sc in c.colors)
                {
                    if (string.IsNullOrWhiteSpace(sc.colorTokenPath))
                    {
                        result.AddError($"[{profileName}] ComponentTheme '{compName}' has EMPTY colorTokenPath.");
                        continue;
                    }

                    if (!colorTokenPaths.Contains(sc.colorTokenPath))
                        result.AddError($"[{profileName}] ComponentTheme '{compName}' references UNKNOWN token '{sc.colorTokenPath}'.");
                }
            }
        }
    }
}