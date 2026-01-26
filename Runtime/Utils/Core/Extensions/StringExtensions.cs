using System.Text.RegularExpressions;

namespace BrewedCode.Utils
{
    public static class StringExtensions
    {
        public static string SplitPascalCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return Regex.Replace(input, "(?<=[a-z0-9])(?=[A-Z])", " ");
        }
    }
}
