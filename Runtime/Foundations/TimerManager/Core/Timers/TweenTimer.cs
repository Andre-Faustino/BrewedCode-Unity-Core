using UnityEngine;

namespace BrewedCode.TimerManager
{
    /// <summary>
    /// Timer with curve-based interpolation.
    ///
    /// Evaluates an AnimationCurve to produce smooth animations.
    /// EvaluatedValue goes from 0 to 1 over the timer's duration,
    /// following the curve's shape.
    ///
    /// Note: Uses UnityEngine.AnimationCurve, but logic is still
    /// testable by mocking the curve.
    /// </summary>
    public sealed class TweenTimer : TimerBase
    {
        /// <summary>Curve used for interpolation (0 to 1 range).</summary>
        public AnimationCurve Curve { get; set; }

        /// <summary>Current interpolated value following the curve (0 to 1).</summary>
        public float EvaluatedValue { get; private set; }

        public TweenTimer(TimerId id, float duration, AnimationCurve? curve = null)
            : base(id, duration)
        {
            Curve = curve ?? AnimationCurve.Linear(0, 0, 1, 1);
        }

        public override void Advance(float delta)
        {
            if (!IsRunning || IsPaused) return;

            Elapsed += delta;

            if (Elapsed >= Duration)
            {
                Elapsed = Duration;
                IsCompleted = true;
                IsRunning = false;
            }

            EvaluatedValue = Curve.Evaluate(Progress);
        }

        public override void Reset()
        {
            Elapsed = 0f;
            IsCompleted = false;
            IsRunning = false;
            IsPaused = false;
            EvaluatedValue = 0f;
        }
    }
}
