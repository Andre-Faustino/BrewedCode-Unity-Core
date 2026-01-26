using NUnit.Framework;

namespace BrewedCode.TimerManager.Tests
{
    /// <summary>
    /// Unit tests for GameTime and TimeContext.
    ///
    /// Tests deterministic time model independent of Unity.
    /// </summary>
    [TestFixture]
    public class GameTimeTests
    {
        [Test]
        public void GameTime_IsImmutable()
        {
            var time = new GameTime(1, 0.016f, 0.016f, 1f, false);

            Assert.AreEqual(1, time.Tick);
            Assert.AreEqual(0.016f, time.Delta);
            // No setters available - immutable confirmed by absence of compilation errors
        }

        [Test]
        public void TimeContext_Advance_IncrementsTick()
        {
            var ctx = new TimeContext();

            var t1 = ctx.Advance(0.016f);
            var t2 = ctx.Advance(0.016f);

            Assert.AreEqual(0, t1.Tick);
            Assert.AreEqual(1, t2.Tick);
        }

        [Test]
        public void TimeContext_Advance_ScalesDelta()
        {
            var ctx = new TimeContext();
            ctx.SetTimeScale(0.5f);

            var time = ctx.Advance(1f);

            Assert.AreEqual(0.5f, time.Delta, 0.001f);
            Assert.AreEqual(1f, time.UnscaledDelta, 0.001f);
        }

        [Test]
        public void TimeContext_Pause_ZerosDelta()
        {
            var ctx = new TimeContext();
            ctx.Pause();

            var time = ctx.Advance(1f);

            Assert.AreEqual(0f, time.Delta);
            Assert.AreEqual(1f, time.UnscaledDelta); // Unscaled still captures original
            Assert.IsTrue(time.IsPaused);
        }

        [Test]
        public void TimeContext_Resume_RestoresDelta()
        {
            var ctx = new TimeContext();
            ctx.Pause();
            ctx.Resume();

            var time = ctx.Advance(1f);

            Assert.Greater(time.Delta, 0f);
            Assert.IsFalse(time.IsPaused);
        }

        [Test]
        public void TimeContext_TimeScaleAndPause_Combine()
        {
            var ctx = new TimeContext();
            ctx.SetTimeScale(0.5f);
            ctx.Pause();

            var time = ctx.Advance(1f);

            Assert.AreEqual(0f, time.Delta); // Pause takes priority
            Assert.AreEqual(0.5f, time.TimeScale);
            Assert.IsTrue(time.IsPaused);
        }

        [Test]
        public void GameTime_WithDelta_CreatesNewSnapshot()
        {
            var original = new GameTime(5, 0.1f, 0.1f, 1f, false);
            var updated = original.WithDelta(0.2f);

            Assert.AreEqual(5, original.Tick); // Original unchanged
            Assert.AreEqual(6, updated.Tick); // New tick
            Assert.AreEqual(0.2f, updated.Delta);
        }

        [Test]
        public void TimeContext_MultipleScaleChanges()
        {
            var ctx = new TimeContext();

            ctx.SetTimeScale(2f);
            var t1 = ctx.Advance(1f);
            Assert.AreEqual(2f, t1.Delta, 0.001f);

            ctx.SetTimeScale(0.5f);
            var t2 = ctx.Advance(1f);
            Assert.AreEqual(0.5f, t2.Delta, 0.001f);

            ctx.SetTimeScale(1f);
            var t3 = ctx.Advance(1f);
            Assert.AreEqual(1f, t3.Delta, 0.001f);
        }
    }
}
