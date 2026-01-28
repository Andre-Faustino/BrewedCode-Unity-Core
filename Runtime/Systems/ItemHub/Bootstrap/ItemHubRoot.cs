using BrewedCode.Singleton;
using UnityEngine;
using BrewedCode.Logging;
using BrewedCode.Events;

namespace BrewedCode.ItemHub
{
    /// <summary>
    /// Singleton bootstrap that wires ItemHubService with external implementations.
    /// Provide IItemCatalog and an optional IGameTimeSource via Inspector.
    /// Uses UnityEventChannelBus as the event bus by default.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class ItemHubRoot : PersistentMonoSingleton<ItemHubRoot>
    {
        [Header("Dependencies")]
        [Tooltip("MonoBehaviour that implements IItemCatalog (required).")]
        [SerializeField] private CatalogProvider? _catalogProvider;

        [Tooltip("Optional MonoBehaviour that implements IEventBus. If null, falls back to Unity EventChannel.")]
        [SerializeField] private EventBusProvider? _eventBusProvider;

        [Tooltip("Optional MonoBehaviour that implements IGameTimeSource. If null, falls back to Unity time.")]
        [SerializeField] private GameTimeSourceProvider? _timeSourceProvider;

        private ItemHubService? hub;
        private ILog? _logger;

        /// <summary>
        /// Exposes the configured ItemHubService instance.
        /// </summary>
        public ItemHubService? Hub => hub;
        public CatalogProvider? CatalogProvider => _catalogProvider;

        // Default Providers
        private IEventBus defaultEventBusProvider => new UnityEventChannelBus();
        private IGameTimeSource defaultGameTimeSource => new StandardGameTimeSource();

        protected override void OnInitializing()
        {
            InitializeIfNeeded();
        }

        private void InitializeIfNeeded()
        {
            if (hub != null) return;

            InitializeLogger();

            if (!_catalogProvider)
            {
                _logger.ErrorSafe("IItemCatalog is not assigned.");
                return;
            }

            var timeSource = _timeSourceProvider ?? defaultGameTimeSource;
            var eventBus = _eventBusProvider ?? defaultEventBusProvider;

            ILoggingService loggingService = null;
            try
            {
                loggingService = LoggingRoot.Instance.Service;
            }
            catch
            {
                // Logging not available
            }

            hub = new ItemHubService(_catalogProvider, eventBus, timeSource, loggingService);

            _logger.InfoSafe("ItemHubService initialized.");
        }

        private void InitializeLogger()
        {
            try
            {
                _logger = LoggingRoot.Instance.Service.GetLogger(nameof(ItemHubRoot));
            }
            catch
            {
                _logger = null;
            }
        }
    }
}
