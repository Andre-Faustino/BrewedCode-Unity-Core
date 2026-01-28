// BrewedCode/ResourceBay/ResourceBayEventBusProvider.cs
using System;
using BrewedCode.Events;
using BrewedCode.Singleton;
using UnityEngine;

namespace BrewedCode.ResourceBay
{
    /// <summary>
    /// Base class to expose an IEventBus via a MonoBehaviour (assignable in the Inspector).
    /// Implement Publish/Subscribe to bridge your bus (e.g., UnityEventChannelBus).
    /// </summary>
    public abstract class ResourceBayEventBusProvider : PersistentMonoSingleton<ResourceBayEventBusProvider>, IEventBus
    {
        public abstract void Publish<TEvent>(TEvent evt);
        public abstract IDisposable Subscribe<TEvent>(Action<TEvent> handler);
    }
}