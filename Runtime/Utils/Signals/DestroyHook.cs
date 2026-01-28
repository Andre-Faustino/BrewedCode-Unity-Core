using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrewedCode.Signals
{
    [DisallowMultipleComponent]
    public sealed class DestroyHook : MonoBehaviour
    {
        private readonly List<IDisposable> _disposables = new();

        public static void Bind(MonoBehaviour owner, IDisposable d)
        {
            if (owner == null) { d?.Dispose(); return; }
            var hook = owner.GetComponent<DestroyHook>() ?? owner.gameObject.AddComponent<DestroyHook>();
            hook._disposables.Add(d);
        }

        private void OnDestroy()
        {
            for (int i = _disposables.Count - 1; i >= 0; i--)
            {
                try { _disposables[i]?.Dispose(); } catch { /* noop */ }
            }
            _disposables.Clear();
        }
    }

    public static class DisposableExtensions
    {
        public static void AddTo(this IDisposable disposable, MonoBehaviour owner)
            => DestroyHook.Bind(owner, disposable);
    }
}
