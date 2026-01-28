namespace BrewedCode.Logging
{
    /// <summary>
    /// Published whenever a log entry is emitted.
    /// Subscribers can use this for debug overlays, telemetry, or export.
    /// </summary>
    public sealed class LogEmittedEvent
    {
        public LogEntry Entry { get; set; } = null!;
    }
}
