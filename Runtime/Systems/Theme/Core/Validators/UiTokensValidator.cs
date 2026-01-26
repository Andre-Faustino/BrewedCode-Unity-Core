using System.Collections.Generic;

namespace BrewedCode.Theme
{
    public static class UiTokensValidator
    {
        public static HashSet<string> Validate(
            UiTokens tokens,
            HashSet<string> rawNames,
            ThemeValidationResult result,
            string profileName
        )
        {
            var tokenPaths = new HashSet<string>();

            if (!tokens)
            {
                result.AddError($"[{profileName}] UiTokens is NULL.");
                return tokenPaths;
            }

            // Colors
            if (tokens.colors != null)
            {
                foreach (var ct in tokens.colors)
                {
                    if (string.IsNullOrWhiteSpace(ct.path))
                    {
                        result.AddWarning($"[{profileName}] UiTokens '{tokens.name}' has ColorToken with EMPTY path.");
                        continue;
                    }

                    if (!tokenPaths.Add(ct.path))
                        result.AddWarning($"[{profileName}] Duplicate ColorToken path '{ct.path}'.");

                    if (!rawNames.Contains(ct.rawRef))
                        result.AddError($"[{profileName}] ColorToken '{ct.path}' references NON-EXISTING raw color '{ct.rawRef}'.");
                }
            }

            return tokenPaths;
        }
    }
}