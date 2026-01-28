#if ANIMANCER
using System.Collections.Generic;
using UnityEngine;
using Animancer;

namespace BrewedCode.Utils.Animancer
{
    public static class TimeSyncReverseAware
    {
        public static void StorePhase<T>(this TimeSynchronizer<T> ts, AnimancerLayer layer)
            => ts.StorePhase(layer.CurrentState);

        /// Guarda a fase "para frente": se o state estiver reverso, salva (1 - t).
        public static void StorePhase<T>(this TimeSynchronizer<T> ts, AnimancerState state)
        {
            double t = (state == null)
                ? double.NaN
                : (state.EffectiveSpeed >= 0 ? state.NormalizedTimeD : 1.0 - state.NormalizedTimeD);

            ts.StoreTime(state, t);
        }

        public static bool SyncPhase<T>(this TimeSynchronizer<T> ts, AnimancerLayer layer, T group,
            float deltaTime = float.NaN)
            => ts.SyncPhase(layer.CurrentState, group, deltaTime);

        /// Aplica a fase guardada; se o alvo estiver reverso, usa (1 - tGuardado).
        public static bool SyncPhase<T>(this TimeSynchronizer<T> ts, AnimancerState state, T group,
            float deltaTime = float.NaN)
        {
            if (float.IsNaN(deltaTime)) deltaTime = Time.deltaTime;

            if (state == null ||
                state == ts.State ||
                double.IsNaN(ts.NormalizedTime) ||
                !EqualityComparer<T>.Default.Equals(ts.CurrentGroup, group) ||
                (!ts.SynchronizeDefaultGroup && EqualityComparer<T>.Default.Equals(default, group)))
            {
                ts.CurrentGroup = group;
                return false;
            }

            // t guardado sempre representa a fase "para frente".
            double t = ts.NormalizedTime;
            if (state.EffectiveSpeed < 0) t = 1.0 - t; // inverter para alvo reverso

            // Aplica como o TimeSynchronizer faz, respeitando o delta e a velocidade atual.
            state.MoveTime(t * state.Length + deltaTime * state.EffectiveSpeed, false);
            return true;
        }
    }

    public static class AnimancerUtilExtensions
    {
        private static readonly Dictionary<AnimancerComponent, SpriteRenderer> _spriteCache = new();

        public static void StopAndClearSpriteRenderer(this AnimancerComponent animancer)
        {
            if (!animancer) return;
            if (!animancer.IsPlaying()) return;

            animancer.Stop();

            if (!_spriteCache.TryGetValue(animancer, out var spriteRenderer) || !spriteRenderer)
            {
                spriteRenderer = animancer.GetComponent<SpriteRenderer>()
                                 ?? animancer.GetComponentInChildren<SpriteRenderer>();

                _spriteCache[animancer] = spriteRenderer;
            }

            if (spriteRenderer)
            {
                spriteRenderer.sprite = null;
                // spriteRenderer.enabled = false;
            }
        }
    }
}
#endif
