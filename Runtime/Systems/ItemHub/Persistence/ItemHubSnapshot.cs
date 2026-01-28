using System;
using System.Collections.Generic;

namespace BrewedCode.ItemHub
{
    /// <summary>
    /// Serializable snapshot for saving/loading ItemHub state.
    /// </summary>
    [Serializable]
    public sealed class ItemHubSnapshot
    {
        [Serializable]
        public sealed class CommodityEntry
        {
            public string itemId;
            public int quantity;
        }

        [Serializable]
        public sealed class InstanceEntry
        {
            public string instanceId;
            public string definitionId;
            public int version;
            public long createdAt;
            public string payloadJson;
        }

        public List<CommodityEntry> commodities = new();
        public List<InstanceEntry> instances = new();
    }
}