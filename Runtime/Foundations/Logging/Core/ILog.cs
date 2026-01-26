using System;
using System.Collections.Generic;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Logger interface for per-class logging.
    /// Provides ergonomic logging methods with automatic source tracking.
    /// </summary>
    public interface ILog
    {
        // Basic logging methods
        void Trace(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message);
        void Fatal(string message);

        // Logging with metadata
        void Trace(string message, IReadOnlyDictionary<string, object> metadata);
        void Info(string message, IReadOnlyDictionary<string, object> metadata);
        void Warning(string message, IReadOnlyDictionary<string, object> metadata);
        void Error(string message, IReadOnlyDictionary<string, object> metadata);
        void Fatal(string message, IReadOnlyDictionary<string, object> metadata);

        // Logging with exception
        void Error(string message, Exception exception);
        void Fatal(string message, Exception exception);

        // Conditional logging (only if level is enabled)
        bool IsEnabled(LogLevel level);
    }
}
