namespace BrewedCode.Logging
{
    /// <summary>
    /// Published when a channel is enabled or disabled.
    /// </summary>
    public sealed class ChannelStateChangedEvent
    {
        public LogChannel Channel { get; set; }
        public bool IsEnabled { get; set; }
    }
}
