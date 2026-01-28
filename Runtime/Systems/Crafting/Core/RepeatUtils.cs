namespace BrewedCode.Crafting
{
    public static class RepeatUtils
    {
        public static void Repeat(this int times, System.Action action)
        {
            for (int i = 0; i < times; i++)
            {
                action();
            }
        }

        public static void Repeat(this int times, System.Action<int> actionWithIndex)
        {
            for (int i = 0; i < times; i++)
            {
                actionWithIndex(i);
            }
        }
    }
}