using System;
using System.Collections.Generic;
using BrewedCode.Events;
using BrewedCode.Singleton;
using UnityEngine;

namespace BrewedCode.ItemHub
{
    public abstract class CatalogProvider : PersistentMonoSingleton<CatalogProvider>, IItemCatalog
    {
        public abstract bool TryGetMeta(ItemId id, out ItemMeta meta);
        public abstract ItemId AddToCatalog(ItemMeta meta);
        public abstract IEnumerable<ItemMeta> GetAllMetas();
    }
    
    public abstract class EventBusProvider : PersistentMonoSingleton<EventBusProvider>, IEventBus
    {
        public abstract void Publish<TEvent>(TEvent evt);
        public abstract IDisposable Subscribe<TEvent>(Action<TEvent> handler);
    }
    
    public abstract class GameTimeSourceProvider : MonoBehaviour, IGameTimeSource
    {
        public abstract long Now();
    }
}