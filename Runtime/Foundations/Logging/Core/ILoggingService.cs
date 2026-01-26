using System.Collections.Generic;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Public API for the Logging System.
    /// Provides logger creation, channel management, and configuration.
    /// </summary>
    public interface ILoggingService
    {
        // Logger factory methods
        ILog GetLogger<T>();
        ILog GetLogger(string sourceName);

        // Channel management
        void EnableChannel(LogChannel channel);
        void DisableChannel(LogChannel channel);
        bool IsChannelEnabled(LogChannel channel);
        LogChannelDefinition? GetChannelDefinition(LogChannel channel);
        IReadOnlyList<LogChannelDefinition> GetAllChannels();

        // Global configuration
        void SetGlobalMinLevel(LogLevel level);
        LogLevel GetGlobalMinLevel();

        // Sink management
        void AddSink(ILogSink sink);
        void RemoveSink(ILogSink sink);
    }
}
