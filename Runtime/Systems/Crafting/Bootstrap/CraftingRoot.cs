using BrewedCode.Singleton;
using UnityEngine;
using BrewedCode.Logging;
using BrewedCode.Events;

namespace BrewedCode.Crafting
{
    /// <summary>
    /// Singleton bootstrap for the Crafting System.
    /// Initializes CraftingService with dependency injection.
    /// Provides global access to the service via Instance.Service.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class CraftingRoot : PersistentMonoSingleton<CraftingRoot>
    {
        [Header("Dependencies")]
        [Tooltip("Optional MonoBehaviour that implements IEventBus. If null, falls back to UnityEventChannelBus.")]
        [SerializeField] private MonoBehaviour? _eventBusProvider;

        [Tooltip("Optional MonoBehaviour that implements ILoggingService. If null, falls back to default initialization.")]
        [SerializeField] private MonoBehaviour? _loggingServiceProvider;

        /// <summary>
        /// Provides access to the initialized ICraftingService.
        /// </summary>
        public ICraftingService Service => _serviceImpl ?? throw new System.InvalidOperationException("CraftingService not initialized. Ensure CraftingRoot.OnInitializing was called.");

        private ICraftingService? _serviceImpl;
        private IEventBus _defaultEventBus => new UnityEventChannelBus();

        protected override void OnInitializing()
        {
            InitializeIfNeeded();
        }

        /// <summary>
        /// Ensures the service is initialized.
        /// Safe to call multiple times.
        /// </summary>
        private void InitializeIfNeeded()
        {
            if (_serviceImpl != null) return;

            // Resolve event bus
            var eventBus = _eventBusProvider as IEventBus ?? _defaultEventBus;

            // Resolve logging service: from inspector or fallback to LoggingRoot singleton
            var loggingService = _loggingServiceProvider as ILoggingService ?? LoggingRoot.Instance.Service;

            // Create pure C# service
            _serviceImpl = new CraftingService(eventBus, loggingService);
        }

        private void Update()
        {
            if (_serviceImpl != null)
            {
                _serviceImpl.Tick(Time.deltaTime);
            }
        }
    }
}
