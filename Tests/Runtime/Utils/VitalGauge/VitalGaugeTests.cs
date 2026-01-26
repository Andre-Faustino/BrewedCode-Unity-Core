using NUnit.Framework;
using BrewedCode.VitalGauge;

namespace BrewedCode.VitalGauge.Tests
{
    /// <summary>
    /// Comprehensive unit tests for the VitalGauge class.
    /// Tests initialization, value management, state transitions, and event publishing.
    /// </summary>
    [TestFixture]
    public class VitalGaugeTests
    {
        private MockEventBus _eventBus;
        private VitalGauge _gauge;
        private GaugeConfig _config;

        [SetUp]
        public void Setup()
        {
            _eventBus = new MockEventBus();
            _gauge = new VitalGauge(_eventBus);
            _config = new GaugeConfig
            {
                Id = "test_gauge",
                Max = 100f,
                Start = 80f,
                RatePerSecond = 10f,
                LowThreshold01 = 0.25f
            };
        }

        #region Initialization Tests

        [Test]
        public void Init_WithValidConfig_SetsPropertiesCorrectly()
        {
            _gauge.Init(_config);

            Assert.AreEqual("test_gauge", _gauge.Id);
            Assert.AreEqual(100f, _gauge.Max);
            Assert.AreEqual(80f, _gauge.Current);
            Assert.AreEqual(0.8f, _gauge.Normalized, 0.001f);
            Assert.AreEqual(GaugeState.Normal, _gauge.State);
        }

        [Test]
        public void Init_WithZeroMax_ClampsMaxToZero()
        {
            _config.Max = -50f;
            _gauge.Init(_config);

            Assert.AreEqual(0f, _gauge.Max);
        }

        [Test]
        public void Init_WithStartGreaterThanMax_ClampsCurrentToMax()
        {
            _config.Start = 150f;
            _gauge.Init(_config);

            Assert.AreEqual(100f, _gauge.Current);
        }

        [Test]
        public void Init_PublishesGaugeChanged()
        {
            _gauge.Init(_config);

            var changedEvents = _eventBus.GetEventsOfType<GaugeChanged>();
            Assert.AreEqual(1, changedEvents.Count);
            Assert.AreEqual("test_gauge", changedEvents[0].Id);
            Assert.AreEqual(80f, changedEvents[0].Current);
            Assert.AreEqual(100f, changedEvents[0].Max);
        }

        #endregion

        #region SetMax Tests

        [Test]
        public void SetMax_ChangeMax_UpdatesProperty()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.SetMax(200f);

            Assert.AreEqual(200f, _gauge.Max);
        }

        [Test]
        public void SetMax_DecreaseMaxBelowCurrent_ClampsCurrent()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.SetMax(50f);

            Assert.AreEqual(50f, _gauge.Current);
            Assert.AreEqual(50f, _gauge.Max);
        }

        [Test]
        public void SetMax_PublishesGaugeChanged()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.SetMax(150f);

            var changedEvents = _eventBus.GetEventsOfType<GaugeChanged>();
            Assert.AreEqual(1, changedEvents.Count);
        }

        #endregion

        #region SetCurrent Tests

        [Test]
        public void SetCurrent_ValidValue_UpdatesCurrent()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.SetCurrent(50f);

            Assert.AreEqual(50f, _gauge.Current);
        }

        [Test]
        public void SetCurrent_BelowZero_ClampsToCero()
        {
            _gauge.Init(_config);
            _gauge.SetCurrent(-10f);

            Assert.AreEqual(0f, _gauge.Current);
        }

        [Test]
        public void SetCurrent_AboveMax_ClampsToMax()
        {
            _gauge.Init(_config);
            _gauge.SetCurrent(150f);

            Assert.AreEqual(100f, _gauge.Current);
        }

        [Test]
        public void SetCurrent_PublishesGaugeChanged()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.SetCurrent(60f);

            var changedEvents = _eventBus.GetEventsOfType<GaugeChanged>();
            Assert.Greater(changedEvents.Count, 0);
        }

        #endregion

        #region Tick Tests

        [Test]
        public void Tick_WithPositiveRate_DecreasesCurrent()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.Tick(1f); // 10 per second * 1 second = 10 drain

            Assert.AreEqual(70f, _gauge.Current, 0.001f);
        }

        [Test]
        public void Tick_WithNegativeRate_IncreasesCurrent()
        {
            _config.RatePerSecond = -5f; // Regen
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.Tick(2f); // -5 per second * 2 seconds = +10 regen

            Assert.AreEqual(90f, _gauge.Current, 0.001f);
        }

        [Test]
        public void Tick_WithZeroDeltaTime_DoesNothing()
        {
            _gauge.Init(_config);
            var initialCurrent = _gauge.Current;
            _eventBus.Clear();

            _gauge.Tick(0f);

            Assert.AreEqual(initialCurrent, _gauge.Current);
            Assert.AreEqual(0, _eventBus.GetEventCount<GaugeChanged>());
        }

        [Test]
        public void Tick_BelowZero_ClampsToZero()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.Tick(10f); // 10 per second * 10 seconds = 100 drain (more than max)

            Assert.AreEqual(0f, _gauge.Current);
        }

        [Test]
        public void Tick_PublishesGaugeChanged()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.Tick(0.5f);

            var changedEvents = _eventBus.GetEventsOfType<GaugeChanged>();
            Assert.Greater(changedEvents.Count, 0);
        }

        #endregion

        #region State Transition Tests

        [Test]
        public void StateTransition_NormalToLow_PublishesStateChangeAndEdgeEvent()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            // Transition from Normal (0.8) to Low (0.25)
            _gauge.SetCurrent(25f);

            var stateChangedEvents = _eventBus.GetEventsOfType<GaugeStateChanged>();
            var lowEvents = _eventBus.GetEventsOfType<GaugeBecameLow>();

            Assert.AreEqual(1, stateChangedEvents.Count);
            Assert.AreEqual(GaugeState.Normal, stateChangedEvents[0].From);
            Assert.AreEqual(GaugeState.Low, stateChangedEvents[0].To);
            Assert.AreEqual(1, lowEvents.Count);
        }

        [Test]
        public void StateTransition_LowToEmpty_PublishesStateChangeAndEdgeEvent()
        {
            _config.Start = 20f;
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.SetCurrent(0f);

            var stateChangedEvents = _eventBus.GetEventsOfType<GaugeStateChanged>();
            var emptyEvents = _eventBus.GetEventsOfType<GaugeBecameEmpty>();

            Assert.AreEqual(1, stateChangedEvents.Count);
            Assert.AreEqual(GaugeState.Low, stateChangedEvents[0].From);
            Assert.AreEqual(GaugeState.Empty, stateChangedEvents[0].To);
            Assert.AreEqual(1, emptyEvents.Count);
        }

        [Test]
        public void StateTransition_NormalToFull_PublishesStateChangeAndEdgeEvent()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.SetCurrent(100f);

            var stateChangedEvents = _eventBus.GetEventsOfType<GaugeStateChanged>();
            var fullEvents = _eventBus.GetEventsOfType<GaugeBecameFull>();

            Assert.AreEqual(1, stateChangedEvents.Count);
            Assert.AreEqual(GaugeState.Normal, stateChangedEvents[0].From);
            Assert.AreEqual(GaugeState.Full, stateChangedEvents[0].To);
            Assert.AreEqual(1, fullEvents.Count);
        }

        #endregion

        #region Edge-Trigger Tests

        [Test]
        public void EdgeTrigger_LowEvent_FiresOnlyOnce()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            // First transition to Low
            _gauge.SetCurrent(25f);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameLow>());

            // Second call while staying in Low - should not fire again
            _gauge.SetCurrent(24f);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameLow>());

            // Stay in Low
            _gauge.Tick(0.1f);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameLow>());
        }

        [Test]
        public void EdgeTrigger_EmptyEvent_FiresOnlyOnce()
        {
            _config.Start = 1f;
            _gauge.Init(_config);
            _eventBus.Clear();

            // First transition to Empty
            _gauge.SetCurrent(0f);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameEmpty>());

            // Stay in Empty
            _gauge.Tick(1f);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameEmpty>());

            // Try to set current to another empty value
            _gauge.SetCurrent(0.00001f);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameEmpty>());
        }

        [Test]
        public void EdgeTrigger_FullEvent_FiresOnlyOnce()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            // First transition to Full
            _gauge.SetCurrent(100f);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameFull>());

            // Stay in Full
            _gauge.SetCurrent(100f);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameFull>());
        }

        [Test]
        public void EdgeTrigger_ReEnteringState_FiresAgain()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            // Enter Low
            _gauge.SetCurrent(25f);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameLow>());

            // Exit Low back to Normal
            _gauge.SetCurrent(50f);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameLow>());

            // Re-enter Low
            _gauge.SetCurrent(25f);
            Assert.AreEqual(2, _eventBus.GetEventCount<GaugeBecameLow>());
        }

        #endregion

        #region Epsilon Tests

        [Test]
        public void Epsilon_VerySmallCurrent_ConsideredEmpty()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.SetCurrent(0.00001f);

            Assert.AreEqual(GaugeState.Empty, _gauge.State);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameEmpty>());
        }

        [Test]
        public void Epsilon_VeryCloseToCurrent_ConsideredFull()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.SetCurrent(99.99999f);

            Assert.AreEqual(GaugeState.Full, _gauge.State);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameFull>());
        }

        [Test]
        public void Epsilon_ZeroMax_NormalizedIsZero()
        {
            _config.Max = 0f;
            _gauge.Init(_config);

            Assert.AreEqual(0f, _gauge.Normalized);
        }

        #endregion

        #region Tick Source Binding Tests

        [Test]
        public void BindTicker_BoundSourceEmitsTick_GaugeDrains()
        {
            _gauge.Init(_config);
            var tickSource = new MockTickSource();
            _eventBus.Clear();

            _gauge.BindTicker(tickSource);
            tickSource.EmitTick(1f);

            Assert.AreEqual(70f, _gauge.Current, 0.001f);
        }

        [Test]
        public void BindTicker_MultipleTicks_AccumulateDrain()
        {
            _gauge.Init(_config);
            var tickSource = new MockTickSource();
            _eventBus.Clear();

            _gauge.BindTicker(tickSource);
            tickSource.EmitTick(0.5f); // 5 drain
            tickSource.EmitTick(0.5f); // 5 drain

            Assert.AreEqual(70f, _gauge.Current, 0.001f);
        }

        [Test]
        public void UnbindTicker_AfterUnbind_NoLongerDrains()
        {
            _gauge.Init(_config);
            var tickSource = new MockTickSource();
            _gauge.BindTicker(tickSource);

            tickSource.EmitTick(0.5f);
            Assert.AreEqual(75f, _gauge.Current, 0.001f);

            _gauge.UnbindTicker();
            tickSource.EmitTick(0.5f); // Should not drain

            Assert.AreEqual(75f, _gauge.Current, 0.001f);
        }

        [Test]
        public void ReBindTicker_ReplacesPreviousBinding()
        {
            _gauge.Init(_config);
            var tickSource1 = new MockTickSource();
            var tickSource2 = new MockTickSource();

            _gauge.BindTicker(tickSource1);
            _gauge.BindTicker(tickSource2);

            // First source should not affect gauge
            tickSource1.EmitTick(1f);
            Assert.AreEqual(80f, _gauge.Current);

            // Second source should work
            tickSource2.EmitTick(1f);
            Assert.AreEqual(70f, _gauge.Current);
        }

        #endregion

        #region Normalized Tests

        [Test]
        public void Normalized_Empty_IsZero()
        {
            _gauge.Init(_config);
            _gauge.SetCurrent(0f);

            Assert.AreEqual(0f, _gauge.Normalized);
        }

        [Test]
        public void Normalized_Full_IsOne()
        {
            _gauge.Init(_config);
            _gauge.SetCurrent(100f);

            Assert.AreEqual(1f, _gauge.Normalized);
        }

        [Test]
        public void Normalized_Half_IsPointFive()
        {
            _gauge.Init(_config);
            _gauge.SetCurrent(50f);

            Assert.AreEqual(0.5f, _gauge.Normalized, 0.001f);
        }

        #endregion

        #region State Priority Tests

        [Test]
        public void StatePriority_Empty_TakesPrecedenceOverLow()
        {
            _gauge.Init(_config);

            _gauge.SetCurrent(0f);

            // Should be Empty, not Low (even though 0 <= 0.25)
            Assert.AreEqual(GaugeState.Empty, _gauge.State);
        }

        [Test]
        public void StatePriority_Full_TakesPrecedenceOverLow()
        {
            _gauge.Init(_config);

            _gauge.SetCurrent(100f);

            // Should be Full, not Low
            Assert.AreEqual(GaugeState.Full, _gauge.State);
        }

        [Test]
        public void StatePriority_LowThreshold_WorksCorrectly()
        {
            _config.LowThreshold01 = 0.3f;
            _gauge.Init(_config);

            // At 30% = 30
            _gauge.SetCurrent(30f);
            Assert.AreEqual(GaugeState.Low, _gauge.State);

            // Above 30% = 31
            _gauge.SetCurrent(31f);
            Assert.AreEqual(GaugeState.Normal, _gauge.State);
        }

        #endregion

        #region Multiple Event Publishing Tests

        [Test]
        public void MultipleEvents_StateChange_PublishesAllRelevantEvents()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.SetCurrent(25f); // Normal -> Low

            // Should publish: GaugeChanged + GaugeStateChanged + GaugeBecameLow
            Assert.Greater(_eventBus.GetEventCount<GaugeChanged>(), 0);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeStateChanged>());
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeBecameLow>());
        }

        [Test]
        public void MultipleEvents_NoStateChange_OnlyPublishesGaugeChanged()
        {
            _gauge.Init(_config);
            _eventBus.Clear();

            _gauge.SetCurrent(50f); // Normal -> Normal (no state change)

            Assert.Greater(_eventBus.GetEventCount<GaugeChanged>(), 0);
            Assert.AreEqual(0, _eventBus.GetEventCount<GaugeStateChanged>());
            Assert.AreEqual(0, _eventBus.GetEventCount<GaugeBecameLow>());
            Assert.AreEqual(0, _eventBus.GetEventCount<GaugeBecameEmpty>());
            Assert.AreEqual(0, _eventBus.GetEventCount<GaugeBecameFull>());
        }

        #endregion

        #region Custom Threshold Tests

        [Test]
        public void CustomThreshold_SingleThreshold_FiresCrossingEvent()
        {
            var config = new GaugeConfig
            {
                Id = "test_gauge",
                Max = 100f,
                Start = 80f,
                RatePerSecond = 10f,
                Thresholds = new[] { new GaugeThreshold("Warning", 0.5f, 0) }
            };
            _gauge.Init(config);
            _eventBus.Clear();

            _gauge.SetCurrent(50f); // Enter threshold at 0.5

            var crossingEvents = _eventBus.GetEventsOfType<GaugeThresholdCrossed>();
            Assert.AreEqual(1, crossingEvents.Count);
            Assert.AreEqual("Warning", crossingEvents[0].ThresholdName);
            Assert.AreEqual(CrossingDirection.Entering, crossingEvents[0].Direction);
            Assert.AreEqual(0.5f, crossingEvents[0].ThresholdValue, 0.001f);
        }

        [Test]
        public void CustomThreshold_ExitingThreshold_PublishesExitingEvent()
        {
            var config = new GaugeConfig
            {
                Id = "test_gauge",
                Max = 100f,
                Start = 50f,
                RatePerSecond = 0f,
                Thresholds = new[] { new GaugeThreshold("Warning", 0.5f, 0) }
            };
            _gauge.Init(config);
            _eventBus.Clear();

            _gauge.SetCurrent(60f); // Exit threshold at 0.5

            var crossingEvents = _eventBus.GetEventsOfType<GaugeThresholdCrossed>();
            Assert.AreEqual(1, crossingEvents.Count);
            Assert.AreEqual("Warning", crossingEvents[0].ThresholdName);
            Assert.AreEqual(CrossingDirection.Exiting, crossingEvents[0].Direction);
        }

        [Test]
        public void CustomThreshold_MultipleThresholds_FiresIndependently()
        {
            var config = new GaugeConfig
            {
                Id = "test_gauge",
                Max = 100f,
                Start = 80f,
                RatePerSecond = 0f,
                Thresholds = new[]
                {
                    new GaugeThreshold("Warning", 0.5f, 0),
                    new GaugeThreshold("Critical", 0.25f, 1)
                }
            };
            _gauge.Init(config);
            _eventBus.Clear();

            _gauge.SetCurrent(25f); // Enter both thresholds

            var crossingEvents = _eventBus.GetEventsOfType<GaugeThresholdCrossed>();
            Assert.AreEqual(2, crossingEvents.Count);
        }

        [Test]
        public void CustomThreshold_EdgeTrigger_FiresOnlyOnCrossing()
        {
            var config = new GaugeConfig
            {
                Id = "test_gauge",
                Max = 100f,
                Start = 80f,
                RatePerSecond = 0f,
                Thresholds = new[] { new GaugeThreshold("Warning", 0.5f, 0) }
            };
            _gauge.Init(config);
            _eventBus.Clear();

            // First crossing
            _gauge.SetCurrent(50f);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeThresholdCrossed>());

            // Stay at same value - should not fire again
            _gauge.SetCurrent(49f);
            Assert.AreEqual(1, _eventBus.GetEventCount<GaugeThresholdCrossed>());

            // Exit and re-enter
            _gauge.SetCurrent(51f);
            Assert.AreEqual(2, _eventBus.GetEventCount<GaugeThresholdCrossed>());

            _gauge.SetCurrent(50f);
            Assert.AreEqual(3, _eventBus.GetEventCount<GaugeThresholdCrossed>());
        }

        [Test]
        public void CustomThreshold_PriorityDetermineLowState()
        {
            var config = new GaugeConfig
            {
                Id = "test_gauge",
                Max = 100f,
                Start = 50f,
                RatePerSecond = 0f,
                Thresholds = new[]
                {
                    new GaugeThreshold("Warning", 0.6f, 0),
                    new GaugeThreshold("Critical", 0.3f, 1) // Higher priority
                }
            };
            _gauge.Init(config);
            _eventBus.Clear();

            _gauge.SetCurrent(40f); // Between thresholds (above Critical)

            // Should remain Normal (40% > Critical threshold of 30%, so Critical not crossed)
            // Only the highest priority threshold determines Low state
            Assert.AreEqual(GaugeState.Normal, _gauge.State);

            _gauge.SetCurrent(25f); // Below Critical threshold

            // Now should be Low (25% <= Critical threshold of 30%, so Critical is crossed)
            Assert.AreEqual(GaugeState.Low, _gauge.State);

            var lowEvents = _eventBus.GetEventsOfType<GaugeBecameLow>();
            Assert.AreEqual(1, lowEvents.Count);
        }

        [Test]
        public void CustomThreshold_EmptyStateBypassesThresholds()
        {
            var config = new GaugeConfig
            {
                Id = "test_gauge",
                Max = 100f,
                Start = 50f,
                RatePerSecond = 0f,
                Thresholds = new[] { new GaugeThreshold("Warning", 0.5f, 0) }
            };
            _gauge.Init(config);
            _eventBus.Clear();

            _gauge.SetCurrent(0f); // Enter Empty state

            var crossingEvents = _eventBus.GetEventsOfType<GaugeThresholdCrossed>();
            // No threshold crossing events should be published in Empty state
            Assert.AreEqual(0, crossingEvents.Count);

            var emptyEvents = _eventBus.GetEventsOfType<GaugeBecameEmpty>();
            Assert.AreEqual(1, emptyEvents.Count);
        }

        [Test]
        public void CustomThreshold_FullStateBypassesThresholds()
        {
            var config = new GaugeConfig
            {
                Id = "test_gauge",
                Max = 100f,
                Start = 50f,
                RatePerSecond = 0f,
                Thresholds = new[] { new GaugeThreshold("Warning", 0.5f, 0) }
            };
            _gauge.Init(config);
            _eventBus.Clear();

            _gauge.SetCurrent(100f); // Enter Full state

            var crossingEvents = _eventBus.GetEventsOfType<GaugeThresholdCrossed>();
            // No threshold crossing events should be published in Full state
            Assert.AreEqual(0, crossingEvents.Count);

            var fullEvents = _eventBus.GetEventsOfType<GaugeBecameFull>();
            Assert.AreEqual(1, fullEvents.Count);
        }

        [Test]
        public void CustomThreshold_BackwardCompatibility_EmptyThresholdsUsesLowThreshold01()
        {
            var config = new GaugeConfig
            {
                Id = "test_gauge",
                Max = 100f,
                Start = 80f,
                RatePerSecond = 0f,
                LowThreshold01 = 0.3f,
                Thresholds = null // No explicit thresholds
            };
            _gauge.Init(config);
            _eventBus.Clear();

            _gauge.SetCurrent(25f); // Below 30%

            Assert.AreEqual(GaugeState.Low, _gauge.State);
            var lowEvents = _eventBus.GetEventsOfType<GaugeBecameLow>();
            Assert.AreEqual(1, lowEvents.Count);
        }

        [Test]
        public void CustomThreshold_BackwardCompatibility_EmptyArrayMigratesFromLowThreshold()
        {
            var config = new GaugeConfig
            {
                Id = "test_gauge",
                Max = 100f,
                Start = 80f,
                RatePerSecond = 0f,
                LowThreshold01 = 0.4f,
                Thresholds = new GaugeThreshold[0] // Empty array
            };
            _gauge.Init(config);
            _eventBus.Clear();

            _gauge.SetCurrent(40f); // At 40%

            Assert.AreEqual(GaugeState.Low, _gauge.State);
        }

        [Test]
        public void CustomThreshold_ExplicitThresholdsIgnoreLowThreshold01()
        {
            var config = new GaugeConfig
            {
                Id = "test_gauge",
                Max = 100f,
                Start = 80f,
                RatePerSecond = 0f,
                LowThreshold01 = 0.2f, // This should be ignored
                Thresholds = new[] { new GaugeThreshold("CustomWarning", 0.6f, 0) }
            };
            _gauge.Init(config);
            _eventBus.Clear();

            // At 50% - should be Low (explicit threshold at 0.6 means crossed when <= 0.6, overrides LowThreshold01)
            _gauge.SetCurrent(50f);
            Assert.AreEqual(GaugeState.Low, _gauge.State);

            // At 80% - should be Normal (above the 0.6 threshold, not crossed)
            _gauge.SetCurrent(80f);
            Assert.AreEqual(GaugeState.Normal, _gauge.State);
        }

        [Test]
        public void CustomThreshold_MultipleCrossingsInOneTick()
        {
            var config = new GaugeConfig
            {
                Id = "test_gauge",
                Max = 100f,
                Start = 80f,
                RatePerSecond = 0f,
                Thresholds = new[]
                {
                    new GaugeThreshold("Warning", 0.7f, 0),
                    new GaugeThreshold("Critical", 0.4f, 1),
                    new GaugeThreshold("Danger", 0.2f, 2)
                }
            };
            _gauge.Init(config);
            _eventBus.Clear();

            // Cross all three thresholds in one operation
            _gauge.SetCurrent(10f);

            var crossingEvents = _eventBus.GetEventsOfType<GaugeThresholdCrossed>();
            Assert.AreEqual(3, crossingEvents.Count);

            // Verify all are entering
            foreach (var evt in crossingEvents)
                Assert.AreEqual(CrossingDirection.Entering, evt.Direction);
        }

        [Test]
        public void CustomThreshold_ColorHexPreserved()
        {
            var threshold = new GaugeThreshold("Warning", 0.5f, 0, "#FF0000");

            Assert.AreEqual("Warning", threshold.Name);
            Assert.AreEqual(0.5f, threshold.Value);
            Assert.AreEqual(0, threshold.Priority);
            Assert.AreEqual("#FF0000", threshold.ColorHex);
        }

        #endregion
    }
}
