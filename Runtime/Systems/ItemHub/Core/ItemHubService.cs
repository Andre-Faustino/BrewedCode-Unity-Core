using System;
using System.Collections.Generic;
using BrewedCode.Events;
using BrewedCode.Logging;

namespace BrewedCode.ItemHub
{
    /// <summary>
    /// ItemHub facade: commodity and instance operations, catalog validation and event publication.
    /// </summary>
    public sealed class ItemHubService
    {
        private readonly IItemCatalog _catalog;
        private readonly IEventBus _bus;
        private readonly IGameTimeSource _getGameTime;
        private readonly ILog? _logger;

        private readonly Dictionary<ItemId, int> _commodities = new();
        private readonly Dictionary<InstanceId, InstanceRecord> _instances = new();
        private readonly Dictionary<ItemId, HashSet<InstanceId>> _byDefinition = new();

        public ItemHubService(IItemCatalog catalog, IEventBus bus, IGameTimeSource getGameTime, ILoggingService loggingService = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _getGameTime = getGameTime ?? throw new ArgumentNullException(nameof(getGameTime));
            _logger = loggingService?.GetLogger<ItemHubService>();
        }

        #region Commodities

        public bool CanAddCommodity(ItemId id, int qty, out string error)
        {
            error = null;
            if (qty <= 0)
            {
                error = "Quantity must be > 0.";
                _logger.WarningSafe($"Cannot add commodity {id}: {error}");
                return false;
            }

            if (!EnsureStorageMode(id, StorageMode.Commodity, out error)) return false;
            return true;
        }

        public bool AddCommodity(ItemId id, int qty, out string error)
        {
            if (!CanAddCommodity(id, qty, out error)) return false;

            _commodities.TryGetValue(id, out var cur);
            var newTotal = checked(cur + qty);
            _commodities[id] = newTotal;

            _logger.InfoSafe($"Commodity added: {id} +{qty} (now: {newTotal})");

            _bus.Publish(new ItemHubEvents.CommodityAdded(id, qty, newTotal, _getGameTime.Now()));
            return true;
        }

        public bool CanRemoveCommodity(ItemId id, int qty, out string error)
        {
            error = null;
            if (qty <= 0)
            {
                error = "Quantity must be > 0.";
                _logger.WarningSafe($"Cannot remove commodity {id}: {error}");
                return false;
            }

            if (!EnsureStorageMode(id, StorageMode.Commodity, out error)) return false;
            if (!_commodities.TryGetValue(id, out var cur) || cur < qty)
            {
                error = "Not enough quantity.";
                _logger.WarningSafe($"Cannot remove commodity {id}: {error} (have: {cur}, want: {qty})");
                return false;
            }

            return true;
        }

        public bool RemoveCommodity(ItemId id, int qty, out string error)
        {
            if (!CanRemoveCommodity(id, qty, out error)) return false;

            var newTotal = _commodities[id] - qty;
            if (newTotal <= 0) _commodities.Remove(id);
            else _commodities[id] = newTotal;

            _logger.InfoSafe($"Commodity removed: {id} -{qty} (now: {Math.Max(newTotal, 0)})");

            _bus.Publish(new ItemHubEvents.CommodityRemoved(id, qty, Math.Max(newTotal, 0), _getGameTime.Now()));
            return true;
        }

        public int CountCommodity(ItemId id)
        {
            int count = _commodities.TryGetValue(id, out var cur) ? cur : 0;
            _logger.TraceSafe($"Commodity count: {id} = {count}");
            return count;
        }

        #endregion

        #region Instances

        public bool CanCreateInstance(ItemId definitionId, out string error)
        {
            error = null;
            if (!EnsureStorageMode(definitionId, StorageMode.Instance, out error)) return false;
            return true;
        }

        public bool TryCreateInstance(ItemId definitionId, string initialPayloadJson, out InstanceId instanceId,
            out string error)
        {
            instanceId = default;
            if (!CanCreateInstance(definitionId, out error)) return false;

            var now = _getGameTime.Now();
            instanceId = InstanceId.New();

            var rec = new InstanceRecord
            {
                InstanceId = instanceId,
                DefinitionId = definitionId,
                Version = 1,
                CreatedAt = now,
                PayloadJson = string.IsNullOrWhiteSpace(initialPayloadJson) ? "{}" : initialPayloadJson
            };

            _instances.Add(instanceId, rec);

            if (!_byDefinition.TryGetValue(definitionId, out var set))
            {
                set = new HashSet<InstanceId>();
                _byDefinition[definitionId] = set;
            }

            set.Add(instanceId);

            _logger.InfoSafe($"Instance created: {instanceId} (def: {definitionId})");

            _bus.Publish(new ItemHubEvents.InstanceCreated(instanceId, definitionId, now));
            return true;
        }

        public bool DeleteInstance(InstanceId instanceId, out string error)
        {
            error = null;
            if (!_instances.TryGetValue(instanceId, out var rec))
            {
                error = "Instance not found.";
                _logger.WarningSafe($"Cannot delete instance {instanceId}: {error}");
                return false;
            }

            _instances.Remove(instanceId);
            if (_byDefinition.TryGetValue(rec.DefinitionId, out var set))
            {
                set.Remove(instanceId);
                if (set.Count == 0) _byDefinition.Remove(rec.DefinitionId);
            }

            _logger.InfoSafe($"Instance deleted: {instanceId} (def: {rec.DefinitionId})");

            _bus.Publish(new ItemHubEvents.InstanceDeleted(instanceId, rec.DefinitionId, _getGameTime.Now()));
            return true;
        }

        public InstanceRecord GetInstance(InstanceId instanceId)
        {
            if (_instances.TryGetValue(instanceId, out var rec))
            {
                _logger.TraceSafe($"Instance retrieved: {instanceId} (def: {rec.DefinitionId})");
                return rec.Clone();
            }
            _logger.WarningSafe($"Instance not found: {instanceId}");
            return null;
        }

        public bool UpdateInstance(InstanceId instanceId, string newPayloadJson, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(newPayloadJson))
            {
                error = "Payload JSON must not be empty.";
                _logger.WarningSafe($"Cannot update instance {instanceId}: {error}");
                return false;
            }

            if (!_instances.TryGetValue(instanceId, out var rec))
            {
                error = "Instance not found.";
                _logger.WarningSafe($"Cannot update instance {instanceId}: {error}");
                return false;
            }

            rec.PayloadJson = newPayloadJson;
            _logger.TraceSafe($"Instance updated: {instanceId} (def: {rec.DefinitionId})");
            _bus.Publish(new ItemHubEvents.InstanceUpdated(instanceId, rec.DefinitionId, _getGameTime.Now()));
            return true;
        }

        public IReadOnlyCollection<InstanceId> ListInstancesByDefinition(ItemId definitionId)
        {
            if (_byDefinition.TryGetValue(definitionId, out var set))
            {
                _logger.TraceSafe($"Instances by definition: {definitionId} = {set.Count} instance(s)");
                return set;
            }
            _logger.TraceSafe($"Instances by definition: {definitionId} = 0 instance(s)");
            return Array.Empty<InstanceId>();
        }

        #endregion

        #region Snapshot

        public ItemHubSnapshot GetSnapshot()
        {
            var snap = new ItemHubSnapshot();

            foreach (var kv in _commodities)
            {
                snap.commodities.Add(new ItemHubSnapshot.CommodityEntry
                {
                    itemId = kv.Key.Value,
                    quantity = kv.Value
                });
            }

            foreach (var kv in _instances)
            {
                var rec = kv.Value;
                snap.instances.Add(new ItemHubSnapshot.InstanceEntry
                {
                    instanceId = rec.InstanceId.Value,
                    definitionId = rec.DefinitionId.Value,
                    version = rec.Version,
                    createdAt = rec.CreatedAt,
                    payloadJson = rec.PayloadJson
                });
            }

            return snap;
        }

        public void LoadSnapshot(ItemHubSnapshot? snapshot)
        {
            _commodities.Clear();
            _instances.Clear();
            _byDefinition.Clear();

            if (snapshot != null)
            {
                foreach (var c in snapshot.commodities)
                {
                    if (!string.IsNullOrWhiteSpace(c.itemId) && c.quantity > 0)
                        _commodities[new ItemId(c.itemId)] = c.quantity;
                }


                foreach (var i in snapshot.instances)
                {
                    if (string.IsNullOrWhiteSpace(i.instanceId) || string.IsNullOrWhiteSpace(i.definitionId)) continue;

                    var instId = new InstanceId(i.instanceId);
                    var defId = new ItemId(i.definitionId);

                    var rec = new InstanceRecord
                    {
                        InstanceId = instId,
                        DefinitionId = defId,
                        Version = i.version,
                        CreatedAt = i.createdAt,
                        PayloadJson = string.IsNullOrWhiteSpace(i.payloadJson) ? "{}" : i.payloadJson
                    };

                    _instances[instId] = rec;

                    if (!_byDefinition.TryGetValue(defId, out var set))
                    {
                        set = new HashSet<InstanceId>();
                        _byDefinition[defId] = set;
                    }

                    set.Add(instId);
                }

                _logger.InfoSafe($"ItemHub snapshot loaded: {_commodities.Count} commodities, {_instances.Count} instances");
            }
            else
            {
                _logger.InfoSafe("ItemHub cleared (snapshot is null)");
            }
        }

        #endregion

        #region Helpers

        private bool EnsureStorageMode(ItemId id, StorageMode expected, out string error)
        {
            error = null;
            if (!_catalog.TryGetMeta(id, out var meta))
            {
                error = $"Item meta not found for '{id}'.";
                _logger.ErrorSafe($"Storage mode check failed: {error}");
                return false;
            }

            if (meta.StorageMode != expected)
            {
                error = $"StorageMode mismatch for '{id}'. Expected {expected}, got {meta.StorageMode}.";
                _logger.ErrorSafe($"Storage mode check failed: {error}");
                return false;
            }

            return true;
        }

        #endregion
    }
}