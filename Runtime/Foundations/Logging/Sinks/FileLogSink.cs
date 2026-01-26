using System;
using System.IO;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Sink that writes logs to a file.
    /// File location: Application.persistentDataPath/Logs/log_{timestamp}.txt
    /// </summary>
    public sealed class FileLogSink : ILogSink, IDisposable
    {
        private readonly StreamWriter _writer;
        private bool _disposed;

        public FileLogSink(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _writer = new StreamWriter(filePath, append: true) { AutoFlush = true };
        }

        public void Write(LogEntry entry)
        {
            if (_disposed) return;

            var message = LogFormatter.Format(entry, includeTimestamp: true, includeChannel: true);
            _writer.WriteLine(message);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _writer?.Dispose();
        }
    }
}
