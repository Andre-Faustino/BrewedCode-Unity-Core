namespace BrewedCode.Events
{
    /// <summary>
    /// Base interface for event channels. Allows type-safe access to listener count without reflection.
    /// </summary>
    public interface IEventChannel
    {
        /// <summary>
        /// Gets the number of active listeners on this channel. Editor-only for debug windows.
        /// </summary>
        int ListenerCount { get; }
    }
}
