using System;

namespace BrewedCode.Utils
{
    public static class EnumExtensions
    {
        public static string Name<T>(this T value) where T : struct, Enum
        {
            return Enum.GetName(typeof(T), value) ?? value.ToString();
        }
    }
}
