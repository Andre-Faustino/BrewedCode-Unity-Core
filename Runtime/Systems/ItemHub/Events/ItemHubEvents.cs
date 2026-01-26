namespace BrewedCode.ItemHub
{
    public static class ItemHubEvents
    {
        public readonly struct CommodityAdded
        {
            public ItemId ItemId { get; }
            public int Delta { get; }
            public int NewTotal { get; }
            public long Timestamp { get; }

            public CommodityAdded(ItemId itemId, int delta, int newTotal, long timestamp)
            {
                ItemId = itemId;
                Delta = delta;
                NewTotal = newTotal;
                Timestamp = timestamp;
            }
        }

        public readonly struct CommodityRemoved
        {
            public ItemId ItemId { get; }
            public int Delta { get; }
            public int NewTotal { get; }
            public long Timestamp { get; }

            public CommodityRemoved(ItemId itemId, int delta, int newTotal, long timestamp)
            {
                ItemId = itemId;
                Delta = delta;
                NewTotal = newTotal;
                Timestamp = timestamp;
            }
        }

        public readonly struct InstanceCreated
        {
            public InstanceId InstanceId { get; }
            public ItemId DefinitionId { get; }
            public long Timestamp { get; }

            public InstanceCreated(InstanceId instanceId, ItemId definitionId, long timestamp)
            {
                InstanceId = instanceId;
                DefinitionId = definitionId;
                Timestamp = timestamp;
            }
        }

        public readonly struct InstanceUpdated
        {
            public InstanceId InstanceId { get; }
            public ItemId DefinitionId { get; }
            public long Timestamp { get; }

            public InstanceUpdated(InstanceId instanceId, ItemId definitionId, long timestamp)
            {
                InstanceId = instanceId;
                DefinitionId = definitionId;
                Timestamp = timestamp;
            }
        }

        public readonly struct InstanceDeleted
        {
            public InstanceId InstanceId { get; }
            public ItemId DefinitionId { get; }
            public long Timestamp { get; }

            public InstanceDeleted(InstanceId instanceId, ItemId definitionId, long timestamp)
            {
                InstanceId = instanceId;
                DefinitionId = definitionId;
                Timestamp = timestamp;
            }
        }
    }
}
