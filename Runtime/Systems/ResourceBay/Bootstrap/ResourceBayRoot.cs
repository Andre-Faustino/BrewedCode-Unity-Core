// BrewedCode/ResourceBay/ResourceBayRoot.cs
using BrewedCode.Singleton;
using UnityEngine;
using BrewedCode.Logging;
using BrewedCode.Events;

namespace BrewedCode.ResourceBay
{
    /// <summary>
    /// Singleton bootstrap for ResourceBay.
    /// Wires ResourceBayService to an optional IEventBus provider.
    /// Falls back to NoopEventBus if none is assigned.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class ResourceBayRoot : PersistentMonoSingleton<ResourceBayRoot>
    {
        [Header("Dependencies")]
        [Tooltip("Optional MonoBehaviour that implements IEventBus. If null, falls back to NoopEventBus.")]
        [SerializeField] private ResourceBayEventBusProvider? _eventBusProvider;

        public IResourceBay Service => resourceBayServiceProvider;

        private ResourceBayService? resourceBayServiceProvider;
        private IEventBus defaultEventBusProvider => new UnityEventChannelBus();
        private ILog? _logger;

        protected override void OnInitializing()
        {
            InitializeIfNeeded();
        }

        private void InitializeIfNeeded()
        {
            if (resourceBayServiceProvider != null) return;

            InitializeLogger();
            var bus = _eventBusProvider ?? defaultEventBusProvider;

            ILoggingService loggingService = null;
            try
            {
                loggingService = LoggingRoot.Instance.Service;
            }
            catch
            {
                // Logging not available
            }

            resourceBayServiceProvider = new ResourceBayService(bus, loggingService);

            _logger.InfoSafe("ResourceBayService initialized.");
        }

        private void InitializeLogger()
        {
            try
            {
                _logger = LoggingRoot.Instance.Service.GetLogger(nameof(ResourceBayRoot));
            }
            catch
            {
                _logger = null;
            }
        }
    }
}