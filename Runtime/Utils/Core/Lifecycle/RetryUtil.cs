using System;
using System.Collections;
using UnityEngine;

namespace BrewedCode.Utils
{
    public static class RetryUtil
    {
        // Simple handle to allow external cancellation
        public sealed class Handle
        {
            internal bool CancelRequested;
            public bool IsCancelled => CancelRequested;
            public void Cancel() => CancelRequested = true;
        }

        /// Retries an action that may throw. Stops on first success (no exception).
        public static Handle Retry(
            MonoBehaviour runner,
            Action attempt,
            int tries = 3,
            float intervalSec = 0.2f,
            bool useUnscaledTime = false,
            Action<int, Exception> onFailedAttempt = null,
            Action onGiveUp = null)
        {
            var h = new Handle();
            runner.StartCoroutine(RetryOnExceptionRoutine(h, attempt, tries, intervalSec, useUnscaledTime,
                onFailedAttempt, onGiveUp));
            return h;
        }

        /// Retries until predicate returns true. Stops on first true.
        public static Handle RetryUntil(
            MonoBehaviour runner,
            Func<bool> attempt,
            int tries = 10,
            float intervalSec = 0.2f,
            bool useUnscaledTime = false,
            Action<int> onFailedAttempt = null,
            Action onSuccess = null,
            Action onGiveUp = null)
        {
            var h = new Handle();
            runner.StartCoroutine(RetryUntilRoutine(h, attempt, tries, intervalSec, useUnscaledTime, onFailedAttempt,
                onSuccess, onGiveUp));
            return h;
        }

        /// Runs an action once after a delay.
        public static Handle After(
            MonoBehaviour runner,
            float delaySec,
            Action action,
            bool useUnscaledTime = false)
        {
            var h = new Handle();
            runner.StartCoroutine(AfterRoutine(h, delaySec, useUnscaledTime, action));
            return h;
        }

        // ------------------ Routines ------------------

        private static IEnumerator RetryOnExceptionRoutine(
            Handle h, Action attempt, int tries, float intervalSec, bool unscaled,
            Action<int, Exception> onFailedAttempt, Action onGiveUp)
        {
            var clamped = Mathf.Max(0f, intervalSec);

            for (int i = 1; i <= Mathf.Max(1, tries) && !h.CancelRequested; i++)
            {
                bool success = false;
                Exception lastEx = null;

                try
                {
                    attempt?.Invoke();
                    success = true; // <- no yield here
                }
                catch (Exception ex)
                {
                    lastEx = ex; // <- no yield here
                    onFailedAttempt?.Invoke(i, ex);
                }

                if (h.CancelRequested) yield break;

                if (success)
                {
                    yield break; // outside try/catch => OK
                }

                if (i < tries)
                {
                    if (unscaled) yield return new WaitForSecondsRealtime(clamped);
                    else yield return new WaitForSeconds(clamped);
                }
            }

            if (!h.CancelRequested)
                onGiveUp?.Invoke();
        }

        private static IEnumerator RetryUntilRoutine(
            Handle h, Func<bool> attempt, int tries, float intervalSec, bool unscaled,
            Action<int> onFailedAttempt, Action onSuccess, Action onGiveUp)
        {
            var clamped = Mathf.Max(0f, intervalSec);

            for (int i = 1; i <= Mathf.Max(1, tries) && !h.CancelRequested; i++)
            {
                bool ok = false;

                try
                {
                    ok = attempt?.Invoke() ?? false;
                }
                catch
                {
                    ok = false;
                } // no yield inside try/catch

                if (h.CancelRequested) yield break;

                if (ok)
                {
                    onSuccess?.Invoke();
                    yield break; // outside try/catch => OK
                }

                onFailedAttempt?.Invoke(i);

                if (i < tries)
                {
                    if (unscaled) yield return new WaitForSecondsRealtime(clamped);
                    else yield return new WaitForSeconds(clamped);
                }
            }

            if (!h.CancelRequested)
                onGiveUp?.Invoke();
        }

        private static IEnumerator AfterRoutine(Handle h, float delaySec, bool unscaled, Action action)
        {
            if (delaySec > 0f)
            {
                if (unscaled) yield return new WaitForSecondsRealtime(delaySec);
                else yield return new WaitForSeconds(delaySec);
            }

            if (!h.CancelRequested)
                action?.Invoke();
        }
    }
}
