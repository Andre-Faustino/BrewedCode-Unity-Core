namespace BrewedCode.Logging
{
    /// <summary>
    /// Interface for log output destinations.
    /// Sinks are responsible for writing log entries to specific outputs
    /// (console, file, network, etc).
    /// </summary>
    public interface ILogSink
    {
        void Write(LogEntry entry);
    }
}
