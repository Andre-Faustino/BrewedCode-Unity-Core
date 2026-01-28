using System;
using UnityEngine;

namespace BrewedCode.Signals
{
    public static class SignalExtensions
    {
        public static void Subscribe(this Signal signal, MonoBehaviour owner, Action handler)
            => signal.Subscribe(handler).AddTo(owner);

        public static void Subscribe<T>(this Signal<T> signal, MonoBehaviour owner, Action<T> handler)
            => signal.Subscribe(handler).AddTo(owner);

        public static void Subscribe<T1, T2>(this Signal<T1, T2> signal, MonoBehaviour owner, Action<T1, T2> handler)
            => signal.Subscribe(handler).AddTo(owner);

        public static void Subscribe<T1, T2, T3>(this Signal<T1, T2, T3> signal, MonoBehaviour owner, Action<T1, T2, T3> handler)
            => signal.Subscribe(handler).AddTo(owner);

        public static void SubscribeOnce(this Signal signal, MonoBehaviour owner, Action handler)
            => signal.SubscribeOnce(handler).AddTo(owner);

        public static void SubscribeOnce<T>(this Signal<T> signal, MonoBehaviour owner, Action<T> handler)
            => signal.SubscribeOnce(handler).AddTo(owner);

        public static void SubscribeOnce<T1, T2>(this Signal<T1, T2> signal, MonoBehaviour owner, Action<T1, T2> handler)
            => signal.SubscribeOnce(handler).AddTo(owner);

        public static void SubscribeOnce<T1, T2, T3>(this Signal<T1, T2, T3> signal, MonoBehaviour owner, Action<T1, T2, T3> handler)
            => signal.SubscribeOnce(handler).AddTo(owner);
    }
}
