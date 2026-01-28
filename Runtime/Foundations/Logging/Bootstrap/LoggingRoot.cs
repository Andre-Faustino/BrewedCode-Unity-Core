using BrewedCode.Singleton;
using UnityEngine;
using BrewedCode.Events;

namespace BrewedCode.Logging
{
    /// <summary>
    /// Unity bootstrap for the Logging System.
    /// Singleton MonoBehaviour that initializes and owns the LoggingService.
    /// </summary>
    [DefaultExecutionOrder(-200)] // Initialize before all other systems
    public sealed class LoggingRoot : PersistentMonoSingleton<LoggingRoot>
    {
        [SerializeField] private MonoBehaviour? _eventBusProvider;
        [SerializeField] private bool _enableFileLogging = false;
        [SerializeField] private bool _useColoredConsole = true;

        private ILoggingService? _serviceImpl;
        private IEventBus? _eventBusImpl;

        /// <summary>Access the logging service from anywhere.</summary>
        public ILoggingService Service =>
            _serviceImpl ?? throw new System.InvalidOperationException(
                "Logging System not initialized. Make sure LoggingRoot exists in the scene.");

        /// <summary>Access the event bus used by the logging service.</summary>
        public IEventBus EventBus =>
            _eventBusImpl ?? throw new System.InvalidOperationException(
                "Logging System not initialized. Make sure LoggingRoot exists in the scene.");

        protected override void OnInitializing()
        {
            InitializeIfNeeded();
        }

        private void InitializeIfNeeded()
        {
            if (_serviceImpl != null) return;

            // Get event bus: from inspector or fallback to UnityEventChannelBus
            _eventBusImpl = _eventBusProvider as IEventBus ?? new UnityEventChannelBus();

            // Create pure C# service
            _serviceImpl = new LoggingService(_eventBusImpl);

            // Register default sinks
            _serviceImpl.AddSink(new UnityConsoleLogSink(_serviceImpl, _useColoredConsole));

            if (_enableFileLogging)
            {
                var logPath = System.IO.Path.Combine(
                    Application.persistentDataPath,
                    "Logs",
                    $"log_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                _serviceImpl.AddSink(new FileLogSink(logPath));
            }
        }
    }
}
