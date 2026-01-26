using NUnit.Framework;
using System;
using System.Collections.Generic;
using BrewedCode.Events;

namespace BrewedCode.TimerManager.Tests
{
    /// <summary>
    /// Unit tests for TimerService (pure C# business logic).
    ///
    /// Tests critical scenarios:
    /// - Timer creation and lifecycle
    /// - State transitions
    /// - Event publishing
    /// - All timer types
    /// - Error handling
    ///
    /// No Unity runtime dependencies. Runs in pure C#.
    /// </summary>
    [TestFixture]
    public class TimerServiceTests
    {
        private TimerService _service;
        private MockEventBus _eventBus;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new MockEventBus();
            _service = new TimerService(_eventBus);
        }

        // ===== Basic Timer Tests =====

        [Test]
        public void TryStartTimer_WithValidDuration_ReturnsTrue()
        {
            var id = TimerId.New();

            bool result = _service.TryStartTimer(id, 5f, false, out string error);

            Assert.IsTrue(result);
            Assert.AreEqual("", error);
            Assert.AreEqual(1, _eventBus.PublishedEvents.Count);
            Assert.IsInstanceOf<TimerStartedEvent>(_eventBus.PublishedEvents[0]);
        }

        [Test]
        public void TryStartTimer_WithZeroDuration_ReturnsFalse()
        {
            var id = TimerId.New();

            bool result = _service.TryStartTimer(id, 0f, false, out string error);

            Assert.IsFalse(result);
            Assert.AreEqual("Duration must be positive.", error);
            Assert.AreEqual(0, _eventBus.PublishedEvents.Count);
        }

        [Test]
        public void TryStartTimer_WithNegativeDuration_ReturnsFalse()
        {
            var id = TimerId.New();

            bool result = _service.TryStartTimer(id, -5f, false, out string error);

            Assert.IsFalse(result);
            Assert.AreEqual("Duration must be positive.", error);
        }

        [Test]
        public void TryStartTimer_WithDuplicateId_ReturnsFalse()
        {
            var id = TimerId.New();

            _service.TryStartTimer(id, 5f, false, out _);
            bool result = _service.TryStartTimer(id, 3f, false, out string error);

            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("already exists"));
        }

        // ===== Time Advancement =====

        [Test]
        public void Tick_AdvancesTimerCorrectly()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 10f, false, out _);
            _eventBus.PublishedEvents.Clear();

            _service.Tick(5f);

            var info = _service.GetTimerInfo(id);
            Assert.IsNotNull(info);
            Assert.AreEqual(5f, info!.Elapsed, 0.01f);
            Assert.AreEqual(5f, info.Remaining, 0.01f);
            Assert.AreEqual(0.5f, info.Progress, 0.01f);
        }

        [Test]
        public void Tick_MultipleAdvances_Accumulates()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 10f, false, out _);
            _eventBus.PublishedEvents.Clear();

            _service.Tick(2f);
            _service.Tick(2f);
            _service.Tick(1f);

            var info = _service.GetTimerInfo(id);
            Assert.AreEqual(5f, info!.Elapsed, 0.01f);
        }

        // ===== Completion =====

        [Test]
        public void Tick_CompletesTimerAndPublishesEvent()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 5f, false, out _);
            _eventBus.PublishedEvents.Clear();

            _service.Tick(10f); // Overshoot

            var completedEvents = _eventBus.PublishedEvents.FindAll(e => e is TimerCompletedEvent);
            Assert.AreEqual(1, completedEvents.Count);

            var evt = (TimerCompletedEvent)completedEvents[0];
            Assert.AreEqual(id, evt.TimerId);

            var info = _service.GetTimerInfo(id);
            Assert.AreEqual(5f, info!.Elapsed);
            Assert.IsTrue(info.IsCompleted);
        }

        [Test]
        public void Tick_LimitedOvershoot_ClampsToExactDuration()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 5f, false, out _);

            _service.Tick(6f); // Overshoot by 1

            var info = _service.GetTimerInfo(id);
            Assert.AreEqual(5f, info!.Elapsed); // Clamped, not 6f
        }

        // ===== Pause & Resume =====

        [Test]
        public void TryPauseTimer_PausesCorrectly()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 10f, false, out _);

            bool result = _service.TryPauseTimer(id, out string error);

            Assert.IsTrue(result);
            Assert.AreEqual("", error);

            var info = _service.GetTimerInfo(id);
            Assert.IsTrue(info!.IsPaused);
        }

        [Test]
        public void Tick_WhilePaused_DoesNotAdvance()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 10f, false, out _);
            _service.TryPauseTimer(id, out _);
            _eventBus.PublishedEvents.Clear();

            _service.Tick(5f);

            var info = _service.GetTimerInfo(id);
            Assert.AreEqual(0f, info!.Elapsed); // No advancement
        }

        [Test]
        public void TryResumeTimer_ResumesCorrectly()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 10f, false, out _);
            _service.TryPauseTimer(id, out _);

            bool result = _service.TryResumeTimer(id, out string error);

            Assert.IsTrue(result);
            Assert.AreEqual("", error);

            var info = _service.GetTimerInfo(id);
            Assert.IsFalse(info!.IsPaused);
        }

        [Test]
        public void Pause_PausesAllTimers()
        {
            var id1 = TimerId.New();
            var id2 = TimerId.New();

            _service.TryStartTimer(id1, 10f, false, out _);
            _service.TryStartTimer(id2, 10f, false, out _);

            _service.Pause();
            _service.Tick(5f);

            var info1 = _service.GetTimerInfo(id1);
            var info2 = _service.GetTimerInfo(id2);

            Assert.AreEqual(0f, info1!.Elapsed);
            Assert.AreEqual(0f, info2!.Elapsed);
        }

        [Test]
        public void Resume_ResumesAllTimers()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 10f, false, out _);
            _service.Pause();
            _service.Resume();

            _service.Tick(3f);

            var info = _service.GetTimerInfo(id);
            Assert.Greater(info!.Elapsed, 0f);
        }

        // ===== Stop & Removal =====

        [Test]
        public void TryStopTimer_RemovesTimerAndPublishesEvent()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 10f, false, out _);
            _eventBus.PublishedEvents.Clear();

            bool result = _service.TryStopTimer(id, out string error);

            Assert.IsTrue(result);
            var info = _service.GetTimerInfo(id);
            Assert.IsNull(info);

            var cancelledEvents = _eventBus.PublishedEvents.FindAll(e => e is TimerCancelledEvent);
            Assert.AreEqual(1, cancelledEvents.Count);
        }

        [Test]
        public void TryStopTimer_NonExistent_ReturnsFalse()
        {
            var id = TimerId.New();

            bool result = _service.TryStopTimer(id, out string error);

            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("not found"));
        }

        // ===== Looping Timers =====

        [Test]
        public void LoopingTimer_RestartsAfterCompletion()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 5f, true, out _);

            _service.Tick(7f); // Overshoot by 2

            var info = _service.GetTimerInfo(id);
            Assert.IsNotNull(info);
            Assert.IsTrue(info!.IsRunning);
            Assert.Less(info.Elapsed, 5f);
            Assert.Greater(info.Elapsed, 1.5f); // Should be around 2f
        }

        [Test]
        public void LoopingTimer_MultipleCycles()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 5f, true, out _);

            _service.Tick(17f); // 3.4 cycles

            var info = _service.GetTimerInfo(id);
            Assert.IsTrue(info!.IsRunning);
            Assert.Less(info.Elapsed, 5f);
        }

        // ===== Countdown Timer =====

        [Test]
        public void TryStartCountdown_Works()
        {
            var id = TimerId.New();

            bool result = _service.TryStartCountdown(id, 5f, out string error);

            Assert.IsTrue(result);
            Assert.AreEqual("", error);

            _service.Tick(2f);

            var info = _service.GetTimerInfo(id);
            Assert.AreEqual(2f, info!.Elapsed);
        }

        // ===== Cooldown Timer =====

        [Test]
        public void TryStartCooldown_Works()
        {
            var id = TimerId.New();

            bool result = _service.TryStartCooldown(id, 3f, out string error);

            Assert.IsTrue(result);
            _service.Tick(3.5f);

            var info = _service.GetTimerInfo(id);
            Assert.IsTrue(info!.IsCompleted);
        }

        // ===== Time Scale =====

        [Test]
        public void SetTimeScale_Half_SlowsTimer()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 10f, false, out _);
            _service.SetTimeScale(0.5f);

            _service.Tick(1f); // Real 1s, but only 0.5s passes

            var info = _service.GetTimerInfo(id);
            Assert.AreEqual(0.5f, info!.Elapsed, 0.01f);
        }

        [Test]
        public void SetTimeScale_Double_SpeedsTimer()
        {
            var id = TimerId.New();
            _service.TryStartTimer(id, 10f, false, out _);
            _service.SetTimeScale(2f);

            _service.Tick(1f); // Real 1s, but 2s passes

            var info = _service.GetTimerInfo(id);
            Assert.AreEqual(2f, info!.Elapsed, 0.01f);
        }

        // ===== Queries =====

        [Test]
        public void GetAllTimers_ReturnsAllActiveTimers()
        {
            var ids = new[] { TimerId.New(), TimerId.New(), TimerId.New() };

            foreach (var id in ids)
            {
                _service.TryStartTimer(id, 10f, false, out _);
            }

            var allTimers = _service.GetAllTimers();

            Assert.AreEqual(3, allTimers.Count);
        }

        [Test]
        public void GetTimerInfo_NonExistent_ReturnsNull()
        {
            var info = _service.GetTimerInfo(TimerId.New());
            Assert.IsNull(info);
        }

        // ===== Mock Event Bus =====

        private class MockEventBus : IEventBus
        {
            public List<object> PublishedEvents { get; } = new();

            public void Publish<T>(T evt)
            {
                PublishedEvents.Add(evt!);
            }

            public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
            {
                return null!;
            }
        }
    }
}
