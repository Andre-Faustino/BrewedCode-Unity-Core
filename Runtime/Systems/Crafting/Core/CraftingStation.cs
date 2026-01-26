using System;
using UnityEngine;

namespace BrewedCode.Crafting
{
    public enum CraftingStationState
    {
        Idle,
        Crafting,
        Paused
    }

    /// <summary>
    /// Thin MonoBehaviour controller for a crafting station.
    /// Delegates all business logic to ICraftingService.
    /// Maintains backward compatibility with legacy API.
    /// </summary>
    public class CraftingStation : MonoBehaviour, ICraftingStationController
    {
        public readonly CraftingStationId Id = CraftingStationId.New();

        private ICraftingService? _craftingService;
        private bool _isRegistered;

        // Legacy properties for backward compatibility
        [Obsolete("Use State property instead")]
        public CraftingStationState craftingStationState
        {
            get => State;
            set { } // Ignored - state is read-only from service
        }

        /// <summary>Current state of this station (read-only from service).</summary>
        public CraftingStationState State
        {
            get
            {
                var info = _craftingService?.GetStationInfo(Id);
                return info?.State ?? CraftingStationState.Idle;
            }
        }

        public bool IsCrafting => State == CraftingStationState.Crafting;
        public bool IsPaused => State == CraftingStationState.Paused;
        public bool IsIdle => State == CraftingStationState.Idle;

        /// <summary>Number of items queued for crafting (including the current one).</summary>
        public int CraftingRemainingAmount
        {
            get
            {
                var info = _craftingService?.GetStationInfo(Id);
                return info?.QueuedCount ?? 0;
            }
        }

        public float CraftProgress
        {
            get
            {
                var info = _craftingService?.GetStationInfo(Id);
                return info?.Progress ?? 0f;
            }
        }

        public float CraftTime => TimeElapsed;
        public float CraftTimeRemaining => TimeRemaining;
        public float CraftTimeElapsed => TimeElapsed;
        public float CraftTimeTotal => TimeTotal;

        public float TimeElapsed
        {
            get
            {
                var info = _craftingService?.GetStationInfo(Id);
                return info?.TimeElapsed ?? 0f;
            }
        }

        public float TimeRemaining
        {
            get
            {
                var info = _craftingService?.GetStationInfo(Id);
                return info?.TimeRemaining ?? 0f;
            }
        }

        public float TimeTotal
        {
            get
            {
                var info = _craftingService?.GetStationInfo(Id);
                return info?.TimeTotal ?? 0f;
            }
        }

        // Legacy events for backward compatibility
        public event Action OnCraftProgressEvent;
        public event Action OnCraftingStateChangeEvent;

        // Legacy currentCraftingProcess for backward compatibility
        private CraftingProcess? _legacyProcess;
        [Obsolete("Legacy property. Use State and CraftingService.GetStationInfo() instead.")]
        public CraftingProcess? currentCraftingProcess
        {
            get
            {
                var stationInfo = _craftingService?.GetStationInfo(Id);
                if (stationInfo != null && stationInfo.State != CraftingStationState.Idle && stationInfo.CurrentCraftable != null)
                {
                    if (_legacyProcess == null)
                    {
                        _legacyProcess = CraftingProcess.Create(this, stationInfo.CurrentCraftable);
                    }
                    // Update progress values
                    _legacyProcess.craftTimeProgressTotal = stationInfo.TimeTotal;
                    _legacyProcess.craftTimeProgressElapsed = stationInfo.TimeElapsed;
                    _legacyProcess.craftTimeProgressRemaining = stationInfo.TimeRemaining;
                    _legacyProcess.craftTimeProgress = stationInfo.Progress;
                    return _legacyProcess;
                }
                else
                {
                    _legacyProcess = null;
                    return null;
                }
            }
            set => _legacyProcess = value;
        }

        private void Awake()
        {
            _craftingService = CraftingRoot.Instance?.Service;
        }

        private void OnEnable()
        {
            OnCraftingStateChangeEvent += HandleCraftingStateChange;

            if (_craftingService != null && !_isRegistered)
            {
                _craftingService.RegisterStation(Id, this);
                _isRegistered = true;
            }
        }

        private void OnDisable()
        {
            OnCraftingStateChangeEvent -= HandleCraftingStateChange;

            if (_craftingService != null && _isRegistered)
            {
                _craftingService.UnregisterStation(Id);
                _isRegistered = false;
            }
        }

        private void HandleCraftingStateChange()
        {
            CraftingStationEvent.Trigger(CraftingStationEventType.ChangeState, this);
        }

        private void Update()
        {
            // For legacy event compatibility, invoke progress and state change events
            var currentState = State;
            if (currentState == CraftingStationState.Crafting || currentState == CraftingStationState.Paused)
            {
                OnCraftProgressEvent?.Invoke();
                OnCraftingStateChangeEvent?.Invoke();
            }
        }

        /// <summary>
        /// Starts crafting the specified item at this station.
        /// </summary>
        /// <param name="craftable">The item to craft.</param>
        /// <param name="amount">Number of items to craft sequentially.</param>
        public void StartCrafting(ICraftable craftable, int amount = 1)
        {
            if (_craftingService == null)
            {
                return;
            }

            if (!_craftingService.TryStartCrafting(Id, craftable, amount, out var error))
            {
                return;
            }
        }

        /// <summary>
        /// Pauses crafting at this station.
        /// </summary>
        public void PauseCrafting()
        {
            if (_craftingService == null) return;

            _craftingService.TryPauseCrafting(Id, out var error);
        }

        /// <summary>
        /// Resumes crafting at this station.
        /// </summary>
        public void ResumeCrafting()
        {
            if (_craftingService == null) return;

            _craftingService.TryResumeCrafting(Id, out var error);
        }

        /// <summary>
        /// Stops/cancels crafting at this station.
        /// </summary>
        public void StopCrafting()
        {
            if (_craftingService == null) return;

            _craftingService.TryStopCrafting(Id, out var error);
            CraftingStationEvent.Trigger(CraftingStationEventType.Cancelled, this);
        }
    }
}