using System;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace BrewedCode.Utils
{
    /// <summary>Kotlin-inspired extension methods with Unity "fake-null" awareness:</summary>
    /// <list type="bullet">
    ///   <item><description>let / run → map (transform) if not null</description></item>
    /// —  <item><description>also → do side-effect and return receiver</description></item>
    /// —  <item><description>apply → configure and return receiver</description></item>
    ///  —  <item><description>takeIf → return receiver if predicate true, else default</description></item>
    ///  —  <item><description>takeUnless → return receiver if predicate false, else default</description></item>
    ///  —  <item><description>OrElse → null-coalescing that respects Unity fake-null</description></item>
    /// </list>

    public static class FunctionalExtensions
    {
        // ---------------------------
        // Null helpers
        // ---------------------------

        /// <summary>
        /// Checks "null-likeness" including Unity's fake null.
        /// </summary>
        private static bool IsNullLike<T>(T obj)
        {
            if (obj == null) return true;

#if UNITY_5_3_OR_NEWER
            // If it's a UnityEngine.Object, use Unity's overloaded == operator.
            if (obj is UnityEngine.Object uo)
            {
                // This can be true even when managed reference is not null (destroyed object).
                return uo == null;
            }
#endif
            return false;
        }

        // ---------------------------
        // let
        // ---------------------------

        /// <summary>
        /// Kotlin's let: if receiver is not null-like, apply func and return its result; otherwise default.
        /// </summary>
        public static TResult Let<T, TResult>(this T obj, Func<T, TResult> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            return IsNullLike(obj) ? default : func(obj);
        }

        // ---------------------------
        // run (alias of let, semantic sugar)
        // ---------------------------

        /// <summary>
        /// Kotlin's run: alias of Let in this context (executes lambda and returns result).
        /// </summary>
        public static TResult Run<T, TResult>(this T obj, Func<T, TResult> func) => obj.Let(func);

        // ---------------------------
        // also
        // ---------------------------

        /// <summary>
        /// Kotlin's also: executes action for side-effects if receiver is not null-like, then returns the receiver.
        /// </summary>
        public static T Also<T>(this T obj, Action<T> action)
        {
            if (!IsNullLike(obj))
            {
                action?.Invoke(obj);
            }

            return obj;
        }

        // ---------------------------
        // apply
        // ---------------------------

        /// <summary>
        /// Kotlin's apply: executes action to configure the receiver if not null-like, then returns the receiver.
        /// </summary>
        public static T Apply<T>(this T obj, Action<T> action)
        {
            if (!IsNullLike(obj))
            {
                action?.Invoke(obj);
            }

            return obj;
        }

        // ---------------------------
        // takeIf / takeUnless
        // ---------------------------

        /// <summary>
        /// Kotlin's takeIf (reference types): returns receiver if not null-like and predicate is true; otherwise null.
        /// </summary>
        public static T TakeIf<T>(this T obj, Func<T, bool> predicate) where T : class
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return !IsNullLike(obj) && predicate(obj) ? obj : null;
        }

        /// <summary>
        /// Kotlin's takeUnless (reference types): returns receiver if not null-like and predicate is false; otherwise null.
        /// </summary>
        public static T TakeUnless<T>(this T obj, Func<T, bool> predicate) where T : class
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return !IsNullLike(obj) && !predicate(obj) ? obj : null;
        }

        /// <summary>
        /// Kotlin's takeIf (value types): returns receiver as nullable if predicate is true; otherwise null.
        /// </summary>
        public static T? TakeIf<T>(this T? obj, Func<T, bool> predicate) where T : struct
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (!obj.HasValue) return null;
            return predicate(obj.Value) ? obj : null;
        }

        /// <summary>
        /// Kotlin's takeUnless (value types): returns receiver as nullable if predicate is false; otherwise null.
        /// </summary>
        public static T? TakeUnless<T>(this T? obj, Func<T, bool> predicate) where T : struct
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (!obj.HasValue) return null;
            return !predicate(obj.Value) ? obj : null;
        }

        // ---------------------------
        // Elvis-like null coalescing that respects Unity fake-null
        // ---------------------------

        /// <summary>
        /// Returns receiver if not null-like; otherwise fallback.
        /// (Similar to Kotlin's ?: but aware of Unity fake-null.)
        /// </summary>
        public static T OrElse<T>(this T obj, T fallback)
        {
            return IsNullLike(obj) ? fallback : obj;
        }

        /// <summary>
        /// Returns receiver if not null-like; otherwise evaluates and returns fallbackFactory().
        /// </summary>
        public static T OrElseGet<T>(this T obj, Func<T> fallbackFactory)
        {
            if (!IsNullLike(obj)) return obj;
            if (fallbackFactory == null) throw new ArgumentNullException(nameof(fallbackFactory));
            return fallbackFactory();
        }

        // ---------------------------
        // Tap (alias of Also) & With (scoped transform)
        // ---------------------------

        /// <summary>
        /// Tap: alias of Also (popular in JS/Swift communities).
        /// </summary>
        public static T Tap<T>(this T obj, Action<T> action) => obj.Also(action);

        /// <summary>
        /// With: like Kotlin's 'with' scoped transform: returns func(obj) if not null-like; else default.
        /// </summary>
        public static TResult With<T, TResult>(this T obj, Func<T, TResult> func) => obj.Let(func);
    }
}
