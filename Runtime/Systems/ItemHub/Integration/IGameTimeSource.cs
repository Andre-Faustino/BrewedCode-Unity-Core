namespace BrewedCode.ItemHub
{
    /// <summary>
    /// Provides current game time as a monotonic long (ticks/seconds/ms as you decide).
    /// Implement on a MonoBehaviour and assign it to ItemHubRoot if you don't want the Unity time fallback.
    /// </summary>
    public interface IGameTimeSource
    {
        long Now();
    }
}