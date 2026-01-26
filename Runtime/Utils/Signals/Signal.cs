using System;

namespace BrewedCode.Signals
{
    public sealed class Signal
    {
        private event Action _handlers;

        public IDisposable Subscribe(Action handler)
        {
            _handlers += handler;
            return new Subscription(() => _handlers -= handler);
        }

        public IDisposable SubscribeOnce(Action handler)
        {
            Action wrap = null;
            wrap = () =>
            {
                handler?.Invoke();
                _handlers -= wrap;
            };
            _handlers += wrap;
            return new Subscription(() => _handlers -= wrap);
        }

        public void Raise() => _handlers?.Invoke();

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;
            public Subscription(Action dispose) => _dispose = dispose;
            public void Dispose() { _dispose?.Invoke(); _dispose = null; }
        }
    }

    public sealed class Signal<T>
    {
        private event Action<T> _handlers;

        public IDisposable Subscribe(Action<T> handler)
        {
            _handlers += handler;
            return new Subscription(() => _handlers -= handler);
        }

        public IDisposable SubscribeOnce(Action<T> handler)
        {
            Action<T> wrap = null;
            wrap = (t) =>
            {
                handler?.Invoke(t);
                _handlers -= wrap;
            };
            _handlers += wrap;
            return new Subscription(() => _handlers -= wrap);
        }

        public void Raise(T value) => _handlers?.Invoke(value);

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;
            public Subscription(Action dispose) => _dispose = dispose;
            public void Dispose() { _dispose?.Invoke(); _dispose = null; }
        }
    }

    public sealed class Signal<T1, T2>
    {
        private event Action<T1, T2> _handlers;

        public IDisposable Subscribe(Action<T1, T2> handler)
        {
            _handlers += handler;
            return new Subscription(() => _handlers -= handler);
        }

        public IDisposable SubscribeOnce(Action<T1, T2> handler)
        {
            Action<T1, T2> wrap = null;
            wrap = (t1, t2) =>
            {
                handler?.Invoke(t1, t2);
                _handlers -= wrap;
            };
            _handlers += wrap;
            return new Subscription(() => _handlers -= wrap);
        }

        public void Raise(T1 arg1, T2 arg2) => _handlers?.Invoke(arg1, arg2);

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;
            public Subscription(Action dispose) => _dispose = dispose;
            public void Dispose() { _dispose?.Invoke(); _dispose = null; }
        }
    }

    public sealed class Signal<T1, T2, T3>
    {
        private event Action<T1, T2, T3> _handlers;

        public IDisposable Subscribe(Action<T1, T2, T3> handler)
        {
            _handlers += handler;
            return new Subscription(() => _handlers -= handler);
        }

        public IDisposable SubscribeOnce(Action<T1, T2, T3> handler)
        {
            Action<T1, T2, T3> wrap = null;
            wrap = (t1, t2, t3) =>
            {
                handler?.Invoke(t1, t2, t3);
                _handlers -= wrap;
            };
            _handlers += wrap;
            return new Subscription(() => _handlers -= wrap);
        }

        public void Raise(T1 arg1, T2 arg2, T3 arg3) => _handlers?.Invoke(arg1, arg2, arg3);

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;
            public Subscription(Action dispose) => _dispose = dispose;
            public void Dispose() { _dispose?.Invoke(); _dispose = null; }
        }
    }
}
