using UnityEngine;
using BrewedCode.Singleton;
using BrewedCode.Events;

namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Unity bootstrap for the TimerManager system.
    ///
    /// Singleton MonoBehaviour that initializes and owns the TimerService.
    /// Calls Tick() from Update() with Time.deltaTime.
    ///
    /// Pattern matches CraftingRoot exactly.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class TimerManagerRoot : PersistentMonoSingleton<TimerManagerRoot>
    {
        [SerializeField] private MonoBehaviour? _eventBusProvider;

        private ITimerService? _serviceImpl;

        /// <summary>Access the timer service from anywhere.</summary>
        public ITimerService Service =>
            _serviceImpl ?? throw new System.InvalidOperationException(
                "TimerManager not initialized. Make sure TimerManagerRoot exists in the scene.");

        protected override void OnInitializing()
        {
            InitializeIfNeeded();
        }

        private void InitializeIfNeeded()
        {
            if (_serviceImpl != null) return;

            // Get event bus: from inspector or create default
            var eventBus = _eventBusProvider as IEventBus ?? new UnityEventChannelBus();

            // Create pure C# service
            _serviceImpl = new TimerService(eventBus);
        }

        private void Update()
        {
            if (_serviceImpl != null)
            {
                // Only place where Time.deltaTime enters the system
                _serviceImpl.Tick(Time.deltaTime);
            }
        }
    }
}
