using System;

namespace BrewedCode.Utils
{
    public static class IntExtensions
    {

        public static void ForEach(this int count, Action<int> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
                action(i);
        }

        /// <summary>
        /// No index versions of ForEach
        /// </summary>
        public static void ForEach(this int count, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
                action();
        }
    }
}
