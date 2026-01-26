using NUnit.Framework;

namespace BrewedCode.TimerManager.Tests
{
    /// <summary>
    /// Unit tests for TimerScheduler.
    ///
    /// Tests internal scheduling logic, determinism, and completion detection.
    /// </summary>
    [TestFixture]
    public class SchedulerTests
    {
        private TimerScheduler _scheduler;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new TimerScheduler();
        }

        [Test]
        public void AddTimer_StoresTimer()
        {
            var id = TimerId.New();
            var timer = new Timer(id, 5f);

            _scheduler.AddTimer(timer);

            var retrieved = _scheduler.GetTimer(id);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(id, retrieved!.Id);
        }

        [Test]
        public void GetTimer_NonExistent_ReturnsNull()
        {
            var id = TimerId.New();

            var timer = _scheduler.GetTimer(id);

            Assert.IsNull(timer);
        }

        [Test]
        public void RemoveTimer_Succeeds()
        {
            var id = TimerId.New();
            var timer = new Timer(id, 5f);

            _scheduler.AddTimer(timer);
            bool removed = _scheduler.RemoveTimer(id);

            Assert.IsTrue(removed);
            Assert.IsNull(_scheduler.GetTimer(id));
        }

        [Test]
        public void RemoveTimer_NonExistent_ReturnsFalse()
        {
            var id = TimerId.New();

            bool removed = _scheduler.RemoveTimer(id);

            Assert.IsFalse(removed);
        }

        [Test]
        public void AdvanceAll_AdvancesAllRunningTimers()
        {
            var id1 = TimerId.New();
            var id2 = TimerId.New();

            var t1 = new Timer(id1, 10f);
            var t2 = new Timer(id2, 10f);

            t1.Start();
            t2.Start();

            _scheduler.AddTimer(t1);
            _scheduler.AddTimer(t2);

            _scheduler.AdvanceAll(2f);

            Assert.AreEqual(2f, t1.Elapsed, 0.01f);
            Assert.AreEqual(2f, t2.Elapsed, 0.01f);
        }

        [Test]
        public void AdvanceAll_SkipsPausedTimers()
        {
            var id = TimerId.New();
            var timer = new Timer(id, 10f);

            timer.Start();
            timer.Pause();

            _scheduler.AddTimer(timer);
            _scheduler.AdvanceAll(5f);

            Assert.AreEqual(0f, timer.Elapsed); // Paused, not advanced
        }

        [Test]
        public void AdvanceAll_SkipsStoppedTimers()
        {
            var id = TimerId.New();
            var timer = new Timer(id, 10f);

            timer.Start();
            timer.Stop();

            _scheduler.AddTimer(timer);
            _scheduler.AdvanceAll(5f);

            Assert.AreEqual(0f, timer.Elapsed); // Not running, not advanced
        }

        [Test]
        public void AdvanceAll_DetectsCompletions()
        {
            var id = TimerId.New();
            var timer = new Timer(id, 5f);

            timer.Start();
            _scheduler.AddTimer(timer);

            var completed = _scheduler.AdvanceAll(10f); // Overshoot

            var completedList = new System.Collections.Generic.List<TimerId>(completed);
            Assert.AreEqual(1, completedList.Count);
            Assert.AreEqual(id, completedList[0]);
        }

        [Test]
        public void AdvanceAll_NoCompletions_ReturnsEmpty()
        {
            var id = TimerId.New();
            var timer = new Timer(id, 10f);

            timer.Start();
            _scheduler.AddTimer(timer);

            var completed = _scheduler.AdvanceAll(2f); // No completion

            var completedList = new System.Collections.Generic.List<TimerId>(completed);
            Assert.AreEqual(0, completedList.Count);
        }

        [Test]
        public void GetAllTimers_ReturnsAllTimers()
        {
            var ids = new[] { TimerId.New(), TimerId.New(), TimerId.New() };

            foreach (var id in ids)
            {
                _scheduler.AddTimer(new Timer(id, 10f));
            }

            var all = _scheduler.GetAllTimers();
            var allList = new System.Collections.Generic.List<TimerBase>(all);

            Assert.AreEqual(3, allList.Count);
        }

        [Test]
        public void GetTimerCount_Accurate()
        {
            _scheduler.AddTimer(new Timer(TimerId.New(), 10f));
            _scheduler.AddTimer(new Timer(TimerId.New(), 10f));

            Assert.AreEqual(2, _scheduler.GetTimerCount());

            _scheduler.RemoveTimer(new TimerId());
            Assert.AreEqual(2, _scheduler.GetTimerCount()); // No change (didn't exist)
        }

        [Test]
        public void AdvanceAll_DeterministicOrdering()
        {
            // Add timers in specific order
            var ids = new[] { TimerId.New(), TimerId.New(), TimerId.New() };
            foreach (var id in ids)
            {
                var timer = new Timer(id, 10f);
                timer.Start();
                _scheduler.AddTimer(timer);
            }

            // Advance multiple times - ordering should be consistent
            _scheduler.AdvanceAll(1f);
            var order1 = new System.Collections.Generic.List<TimerBase>(_scheduler.GetAllTimers());

            _scheduler.AdvanceAll(1f);
            var order2 = new System.Collections.Generic.List<TimerBase>(_scheduler.GetAllTimers());

            Assert.AreEqual(order1.Count, order2.Count);
            for (int i = 0; i < order1.Count; i++)
            {
                Assert.AreEqual(order1[i].Id, order2[i].Id);
            }
        }

        [Test]
        public void MultipleCompletions_AllDetected()
        {
            var ids = new[] { TimerId.New(), TimerId.New() };

            var t1 = new Timer(ids[0], 3f);
            var t2 = new Timer(ids[1], 5f);

            t1.Start();
            t2.Start();

            _scheduler.AddTimer(t1);
            _scheduler.AddTimer(t2);

            var completed = _scheduler.AdvanceAll(10f); // Both overshoot

            var completedList = new System.Collections.Generic.List<TimerId>(completed);
            Assert.AreEqual(2, completedList.Count);
        }
    }
}
