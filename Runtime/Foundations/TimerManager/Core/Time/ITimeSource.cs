namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Abstraction for time providers.
    ///
    /// Allows different implementations: Unity Time, manual test time, etc.
    /// Used for injecting time into the TimerService.
    /// </summary>
    public interface ITimeSource
    {
        /// <summary>
        /// Returns the current game time.
        /// </summary>
        GameTime GetCurrentTime();
    }
}
