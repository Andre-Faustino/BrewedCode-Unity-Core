using System;
using System.Collections.Generic;
using BrewedCode.Events;
using BrewedCode.Logging;
using UnityEngine;

namespace BrewedCode.VitalGauge
{
    /// <summary>
    /// Generic gauge/vital meter component for health, stamina, oxygen, mana, etc.
    ///
    /// Design Principles:
    /// - Pure C# (no Unity dependencies) - 100% testable
    /// - Decoupled time via ITickSource injection
    /// - Edge-trigger events (state enter only, not every frame)
    /// - Epsilon-safe floating-point comparisons
    /// - Simple state machine with priority ordering
    ///
    /// State Machine:
    /// Priority 3: Empty   (Current ≈ 0)
    /// Priority 2: Full    (Current ≈ Max)
    /// Priority 1: Low     (Normalized ≤ threshold)
    /// Priority 0: Normal  (default)
    /// → First matching state is active
    /// → Events fire only on state transitions
    ///
    /// Rates:
    /// - SetRate(float) changes drain/regen per second
    /// - Rate applied continuously via Tick(deltaTime)
    /// - Rate persists until changed
    ///
    /// Events:
    /// - OnChanged: Every value update
    /// - OnStateChanged: On state transition
    /// - OnBecameLow/Empty/Full: State entry (once)
    /// - OnThresholdCrossed: Custom threshold crossing
    ///
    /// Use Cases:
    /// - Health bars with critical/low warnings
    /// - Stamina drain/regen systems
    /// - Oxygen depletion mechanics
    /// - Mana regeneration
    /// </summary>
    public class VitalGauge
    {
        private const float EPSILON = 0.0001f;

        private readonly IEventBus _eventBus;
        private readonly ILog? _logger;
        private ITickSource _tickSource;

        private string _id;
        private float _current;
        private float _max;
        private float _ratePerSecond;
        private float _lowThreshold01;
        private GaugeState _state;

        // Threshold tracking for custom named thresholds
        private GaugeThreshold[] _thresholds;
        private Dictionary<string, bool> _thresholdStates;
        private GaugeThreshold? _highestPriorityThreshold;

        public VitalGauge(IEventBus eventBus, ILoggingService loggingService = null)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = loggingService?.GetLogger<VitalGauge>();
            _state = GaugeState.Normal;
        }

        #region Public API

        /// <summary>Initializes the gauge with configuration values.</summary>
        public void Init(GaugeConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            _id = config.Id;
            _max = Math.Max(0, config.Max);
            _ratePerSecond = config.RatePerSecond;
            _lowThreshold01 = Clamp01(config.LowThreshold01);
            _current = Clamp(config.Start, 0, _max);

            InitializeThresholds(config);
            InitializeThresholdStates();
            UpdateThresholdStates();

            _state = CalculateState();

            _logger.InfoSafe($"VitalGauge initialized: {_id} (max={_max:F2}, current={_current:F2}, state={_state})");

            PublishChanged();
        }

        /// <summary>Sets the maximum value and clamps current if necessary.</summary>
        public void SetMax(float value)
        {
            var oldMax = _max;
            _max = Math.Max(0, value);
            _current = Clamp(_current, 0, _max);

            if (oldMax != _max)
                _logger.InfoSafe($"VitalGauge max changed: {_id} {oldMax:F2} → {_max:F2}");

            UpdateAndPublish();
        }

        /// <summary>Sets the current value and clamps to [0, Max].</summary>
        public void SetCurrent(float value)
        {
            var oldValue = _current;
            _current = Clamp(value, 0, _max);
            if (!Mathf.Approximately(oldValue, _current))
            {
                _logger.TraceSafe($"VitalGauge current set: {_id} {oldValue:F2} → {_current:F2} ({Normalized:P0})");
                UpdateAndPublish();
            }
        }

        /// <summary>Sets the drain/regen rate in value per second.</summary>
        public void SetRate(float ratePerSecond)
        {
            if (!Mathf.Approximately(_ratePerSecond, ratePerSecond))
            {
                _logger.TraceSafe($"VitalGauge rate set: {_id} {_ratePerSecond:F2}/s → {ratePerSecond:F2}/s");
                _ratePerSecond = ratePerSecond;
            }
        }

        /// <summary>Advances the gauge by one tick with the given delta time.</summary>
        public void Tick(float dt)
        {
            if (dt <= 0) return;
            _current = UnityEngine.Mathf.Clamp(_current - _ratePerSecond * dt, 0, _max);
            UpdateAndPublish();
        }

        /// <summary>Binds this gauge to a tick source for automatic updates.</summary>
        public void BindTicker(ITickSource ticker)
        {
            if (ticker == null) throw new ArgumentNullException(nameof(ticker));
            UnbindTicker();
            _tickSource = ticker;
            _tickSource.OnTick += Tick;
            _logger.InfoSafe($"VitalGauge ticker bound: {_id}");
        }

        /// <summary>Unbinds from the current tick source.</summary>
        public void UnbindTicker()
        {
            if (_tickSource != null)
            {
                _tickSource.OnTick -= Tick;
                _tickSource = null;
                _logger.InfoSafe($"VitalGauge ticker unbound: {_id}");
            }
        }

        #endregion

        #region Properties

        /// <summary>Unique identifier of this gauge.</summary>
        public string Id => _id;

        /// <summary>Current value of the gauge.</summary>
        public float Current => _current;

        /// <summary>Maximum value of the gauge.</summary>
        public float Max => _max;

        /// <summary>Normalized value (Current / Max), clamped to [0, 1].</summary>
        public float Normalized => _max <= EPSILON ? 0f : _current / _max;

        /// <summary>Current state of the gauge (Normal, Low, Empty, or Full).</summary>
        public GaugeState State => _state;

        #endregion

        #region Internal Logic

        /// <summary>
        /// Updates the gauge state and publishes appropriate events.
        /// Always publishes GaugeChanged.
        /// Publishes threshold crossing events (edge-trigger).
        /// Publishes state transition events only when state differs from previous.
        /// </summary>
        private void UpdateAndPublish()
        {
            var oldState = _state;
            _state = CalculateState();
            PublishChanged();
            DetectAndPublishThresholdCrossings();
            PublishStateIfChanged(oldState, _state);
        }

        /// <summary>
        /// Calculates the current gauge state based on current/max values and thresholds.
        ///
        /// State Priority:
        /// 1. Empty if Current <= EPSILON
        /// 2. Full if Current >= Max - EPSILON
        /// 3. Low if highest priority threshold is crossed (and not Empty/Full)
        /// 4. Normal otherwise
        /// </summary>
        private GaugeState CalculateState()
        {
            // Check Empty first (highest priority)
            if (_current <= EPSILON)
                return GaugeState.Empty;

            // Check Full second
            if (_current >= _max - EPSILON)
                return GaugeState.Full;

            // Check Low third - use highest priority threshold
            if (_highestPriorityThreshold.HasValue && Normalized <= _highestPriorityThreshold.Value.Value)
                return GaugeState.Low;

            // Default to Normal
            return GaugeState.Normal;
        }

        /// <summary>Publishes a GaugeChanged event with current state.</summary>
        private void PublishChanged()
        {
            _eventBus.Publish(new GaugeChanged(_id, _current, _max, Normalized));
        }

        /// <summary>
        /// Publishes state transition events if state has changed.
        /// Publishes edge-trigger events (Low/Empty/Full) only on entering that state.
        /// Always publishes GaugeStateChanged when state differs from previous.
        /// </summary>
        private void PublishStateIfChanged(GaugeState oldState, GaugeState newState)
        {
            if (oldState == newState)
                return;

            _logger.InfoSafe($"VitalGauge state changed: {_id} {oldState} → {newState}");

            // Always publish state transition
            _eventBus.Publish(new GaugeStateChanged(_id, oldState, newState));

            // Edge-trigger: only publish when entering each state
            if (newState == GaugeState.Low)
                _eventBus.Publish(new GaugeBecameLow(_id));
            else if (newState == GaugeState.Empty)
                _eventBus.Publish(new GaugeBecameEmpty(_id));
            else if (newState == GaugeState.Full)
                _eventBus.Publish(new GaugeBecameFull(_id));
        }

        /// <summary>
        /// Initializes thresholds from config, with auto-migration from LowThreshold01 if needed.
        /// </summary>
        private void InitializeThresholds(GaugeConfig config)
        {
            // Use explicit thresholds if provided, otherwise auto-migrate from LowThreshold01
            if (config.Thresholds != null && config.Thresholds.Length > 0)
            {
                _thresholds = config.Thresholds;
            }
            else
            {
                // Auto-migration: create default "Low" threshold from LowThreshold01
                _thresholds = new[] { new GaugeThreshold("Low", config.LowThreshold01, 0) };
            }

            FindHighestPriorityThreshold();
        }

        /// <summary>Finds and caches the highest priority threshold.</summary>
        private void FindHighestPriorityThreshold()
        {
            if (_thresholds == null || _thresholds.Length == 0)
            {
                _highestPriorityThreshold = null;
                return;
            }

            GaugeThreshold highest = _thresholds[0];
            for (int i = 1; i < _thresholds.Length; i++)
            {
                if (_thresholds[i].Priority > highest.Priority)
                    highest = _thresholds[i];
            }
            _highestPriorityThreshold = highest;
        }

        /// <summary>Initializes the threshold state tracking dictionary.</summary>
        private void InitializeThresholdStates()
        {
            _thresholdStates = new Dictionary<string, bool>();
            if (_thresholds == null || _thresholds.Length == 0)
                return;

            // Initialize all thresholds as not crossed
            foreach (var threshold in _thresholds)
            {
                _thresholdStates[threshold.Name] = false;
            }
        }

        /// <summary>
        /// Updates threshold crossing states based on current normalized value (no events).
        /// Used during initialization to set correct initial states.
        /// </summary>
        private void UpdateThresholdStates()
        {
            if (_thresholds == null || _thresholds.Length == 0)
                return;

            float normalized = Normalized;

            foreach (var threshold in _thresholds)
            {
                // Check if threshold is crossed (normalized <= threshold value)
                bool isCrossed = normalized <= threshold.Value;
                _thresholdStates[threshold.Name] = isCrossed;
            }
        }

        /// <summary>
        /// Detects threshold crossings and publishes GaugeThresholdCrossed events (edge-trigger).
        /// Skips threshold checks when gauge is in Empty or Full state.
        /// </summary>
        private void DetectAndPublishThresholdCrossings()
        {
            // Skip threshold checks when in Empty/Full state (optimization)
            if (_state == GaugeState.Empty || _state == GaugeState.Full)
                return;

            if (_thresholds == null || _thresholds.Length == 0)
                return;

            float normalized = Normalized;

            foreach (var threshold in _thresholds)
            {
                // Check if threshold is crossed (normalized <= threshold value)
                bool isCrossed = normalized <= threshold.Value;

                // Get previous state (default to not crossed)
                bool wasCrossed = _thresholdStates.ContainsKey(threshold.Name) && _thresholdStates[threshold.Name];

                // Detect crossing change (edge-trigger)
                if (isCrossed != wasCrossed)
                {
                    _thresholdStates[threshold.Name] = isCrossed;

                    // Determine crossing direction
                    CrossingDirection direction = isCrossed ? CrossingDirection.Entering : CrossingDirection.Exiting;

                    _logger.InfoSafe($"VitalGauge threshold crossed: {_id} '{threshold.Name}' {direction} (threshold: {threshold.Value}, normalized: {normalized:F2})");

                    // Publish threshold crossing event
                    _eventBus.Publish(new GaugeThresholdCrossed(_id, threshold.Name, direction, threshold.Value, normalized));
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>Clamps a value between min and max (standard C# equivalent of Mathf.Clamp).</summary>
        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>Clamps a value between 0 and 1 (standard C# equivalent of Mathf.Clamp01).</summary>
        private static float Clamp01(float value)
        {
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }

        #endregion
    }
}
