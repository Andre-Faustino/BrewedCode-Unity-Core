namespace BrewedCode.Logging
{
    /// <summary>
    /// No-op sink for testing or disabling all log output.
    /// </summary>
    public sealed class NullLogSink : ILogSink
    {
        public void Write(LogEntry entry)
        {
            // Intentionally empty
        }
    }
}
