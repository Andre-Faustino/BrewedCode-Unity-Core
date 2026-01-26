using System;
using System.Collections.Generic;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Internal implementation of ILog.
    /// Delegates to LoggingService for actual log emission.
    /// </summary>
    internal sealed class Logger : ILog
    {
        private readonly string _source;
        private readonly LogChannel _channel;
        private readonly LoggingService _service;

        public Logger(string source, LogChannel channel, LoggingService service)
        {
            _source = source;
            _channel = channel;
            _service = service;
        }

        public void Trace(string message) => Log(LogLevel.Trace, message, null, null);
        public void Info(string message) => Log(LogLevel.Info, message, null, null);
        public void Warning(string message) => Log(LogLevel.Warning, message, null, null);
        public void Error(string message) => Log(LogLevel.Error, message, null, null);
        public void Fatal(string message) => Log(LogLevel.Fatal, message, null, null);

        public void Trace(string message, IReadOnlyDictionary<string, object> metadata) =>
            Log(LogLevel.Trace, message, null, metadata);

        public void Info(string message, IReadOnlyDictionary<string, object> metadata) =>
            Log(LogLevel.Info, message, null, metadata);

        public void Warning(string message, IReadOnlyDictionary<string, object> metadata) =>
            Log(LogLevel.Warning, message, null, metadata);

        public void Error(string message, IReadOnlyDictionary<string, object> metadata) =>
            Log(LogLevel.Error, message, null, metadata);

        public void Fatal(string message, IReadOnlyDictionary<string, object> metadata) =>
            Log(LogLevel.Fatal, message, null, metadata);

        public void Error(string message, Exception exception) =>
            Log(LogLevel.Error, message, exception, null);

        public void Fatal(string message, Exception exception) =>
            Log(LogLevel.Fatal, message, exception, null);

        public bool IsEnabled(LogLevel level) => _service.IsLogEnabled(_channel, level);

        private void Log(LogLevel level, string message, Exception? exception,
            IReadOnlyDictionary<string, object>? metadata)
        {
            _service.EmitLog(_source, _channel, level, message, exception, metadata);
        }
    }
}
