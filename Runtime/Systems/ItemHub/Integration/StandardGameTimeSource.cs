namespace BrewedCode.ItemHub
{
    public class StandardGameTimeSource : IGameTimeSource
    {
        public long Now()
        {
            return System.DateTime.Now.Ticks;
        }
    }
}
