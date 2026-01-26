namespace BrewedCode.Logging
{
    /// <summary>
    /// Severity levels for log entries.
    /// Controls visibility, stack trace inclusion, and sink behavior.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>Detailed diagnostic information (disabled in builds).</summary>
        Trace = 0,

        /// <summary>Informational messages for normal operation.</summary>
        Info = 1,

        /// <summary>Potentially harmful situations.</summary>
        Warning = 2,

        /// <summary>Error events that might still allow the application to continue.</summary>
        Error = 3,

        /// <summary>Severe errors causing application failure.</summary>
        Fatal = 4
    }
}
