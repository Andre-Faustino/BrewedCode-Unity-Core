using UnityEngine;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Sink that writes logs to Unity's Debug.Log console.
    /// The ONLY place in the Logging System where Debug.Log is called.
    /// </summary>
    public sealed class UnityConsoleLogSink : ILogSink
    {
        private readonly ILoggingService _loggingService;
        private readonly bool _useRichText;

        public UnityConsoleLogSink(ILoggingService loggingService, bool useRichText = true)
        {
            _loggingService = loggingService;
            _useRichText = useRichText;
        }

        public void Write(LogEntry entry)
        {
            string message;

            if (_useRichText)
            {
                var channelDef = _loggingService.GetChannelDefinition(entry.Channel);
                message = LogFormatter.FormatUnityColored(entry, channelDef);
            }
            else
            {
                message = LogFormatter.Format(entry, includeTimestamp: true, includeChannel: true);
            }

            switch (entry.Level)
            {
                case LogLevel.Trace:
                case LogLevel.Info:
                    Debug.Log(message);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(message);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    Debug.LogError(message);
                    if (entry.Exception != null)
                        Debug.LogException(entry.Exception);
                    break;
            }
        }
    }
}
