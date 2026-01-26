using System;
using System.Collections.Generic;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Immutable log entry containing all contextual information.
    /// Represents a single log event with timestamp, source, level, channel, message, and metadata.
    /// </summary>
    public sealed class LogEntry
    {
        public DateTime Timestamp { get; }
        public string Source { get; }          // Class name (e.g., "TimerService")
        public LogChannel Channel { get; }
        public LogLevel Level { get; }
        public string Message { get; }
        public string? StackTrace { get; }
        public Exception? Exception { get; }
        public IReadOnlyDictionary<string, object>? Metadata { get; }

        public LogEntry(
            DateTime timestamp,
            string source,
            LogChannel channel,
            LogLevel level,
            string message,
            string? stackTrace = null,
            Exception? exception = null,
            IReadOnlyDictionary<string, object>? metadata = null)
        {
            Timestamp = timestamp;
            Source = source ?? "Unknown";
            Channel = channel;
            Level = level;
            Message = message ?? "";
            StackTrace = stackTrace;
            Exception = exception;
            Metadata = metadata;
        }
    }
}
