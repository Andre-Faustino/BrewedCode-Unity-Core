// BrewedCode/ResourceBay/ResourceBayService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using BrewedCode.Events;
using BrewedCode.Logging;

namespace BrewedCode.ResourceBay
{
    /// <summary>
    /// Thread-safe resource allocation system for managing shared resource pools.
    ///
    /// Key Responsibilities:
    /// - Define resource types with capacity limits
    /// - Accept allocation requests from multiple owners
    /// - Guarantee all-or-nothing allocation (atomic)
    /// - Track allocations by GUID for release/refund
    /// - Support partial refunds and owner-based queries
    /// - Publish allocation events
    ///
    /// Thread Safety:
    /// - ALL public methods protected by _sync lock
    /// - Safe for concurrent allocation/release from multiple threads
    /// - Allocation failures don't modify state (atomic transactions)
    ///
    /// Allocation Lifecycle:
    /// 1. TryAllocate(request) → Validate availability
    /// 2. If sufficient: Reserve resources, return AllocationId
    /// 3. If insufficient: Return failure, state unchanged
    /// 4. Later: Release(allocationId) or ReleasePartial(allocationId, partial)
    /// 5. On release: Resources return to available pool
    ///
    /// Memory Management:
    /// - Allocations NEVER auto-expire - caller must Release()
    /// - Unreleased allocations stay locked forever (potential leak)
    /// - Design intent: Explicit resource lifecycle management
    /// </summary>
    public sealed class ResourceBayService : IResourceBay
    {
        /// <summary>Lock object for thread-safe resource access.</summary>
        private readonly object _sync = new object();
        private IEventBus _bus;
        private readonly ILog? _logger;

        public ResourceBayService(IEventBus? bus = null, ILoggingService loggingService = null)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = loggingService?.GetLogger<ResourceBayService>();
        }

        /// <summary>Allows late injection from a Unity root.</summary>
        public void SetEventBus(IEventBus bus) => _bus = bus;

        private readonly Dictionary<string, ResourceState> _resources =
            new Dictionary<string, ResourceState>(StringComparer.Ordinal);

        private struct ResourceState
        {
            public long Capacity;
            public long AllocatedTotal;
            public long Available => Capacity - AllocatedTotal;
        }

        private sealed class AllocationRecord
        {
            public Guid Id;
            public string OwnerId;
            public DateTime CreatedUtc;
            public Dictionary<string, long> Map; // key -> amount (>0)
            public string[] Tags;
            public string Context;
        }

        private readonly Dictionary<Guid, AllocationRecord> _allocById = new();
        private readonly Dictionary<string, HashSet<Guid>> _allocByOwner = new(StringComparer.Ordinal);

        // ---------- helpers ----------

        /// <summary>
        /// Executes a critical section under lock and dispatches queued actions outside the lock.
        /// </summary>
        private void SyncAndDispatch(Action<List<Action>> insideLock)
        {
            var post = new List<Action>();
            lock (_sync)
            {
                insideLock(post);
            }
            foreach (var a in post) a?.Invoke();
        }

        /// <summary>
        /// Executes a critical section under lock, returns a value, and dispatches actions outside the lock.
        /// </summary>
        private T SyncAndDispatch<T>(Func<List<Action>, T> insideLock)
        {
            var post = new List<Action>();
            T result;
            lock (_sync)
            {
                result = insideLock(post);
            }
            foreach (var a in post) a?.Invoke();
            return result;
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Resource key cannot be null or empty.", nameof(key));
        }

        /// <summary>Must be called inside the lock. Throws if not found.</summary>
        private ResourceState GetState(string key)
        {
            if (!_resources.TryGetValue(key, out var value))
                throw new ResourceNotFoundException(key);
            return value;
        }

        private void SetState(string key, ResourceState state) => _resources[key] = state;

        // ====== Phase 1 API (with events and improved messages) ======

        public void DefineResource(string key, long capacity)
        {
            ValidateKey(key);
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), $"Capacity cannot be negative (key='{key}').");

            SyncAndDispatch(post =>
            {
                if (_resources.TryGetValue(key, out var state))
                {
                    if (state.AllocatedTotal > capacity)
                        throw new InvalidOperationException($"Cannot set capacity < allocated for '{key}'. Allocated={state.AllocatedTotal}, NewCapacity={capacity}");
                    long old = state.Capacity;
                    state.Capacity = capacity;
                    SetState(key, state);

                    _logger.InfoSafe($"Resource capacity changed: {key} {old} → {capacity}");

                    post.Add(() => _bus.Publish(new ResourceBayEvents.CapacityChanged
                    {
                        Key = key, OldCapacity = old, NewCapacity = capacity
                    }));
                }
                else
                {
                    SetState(key, new ResourceState { Capacity = capacity, AllocatedTotal = 0 });
                    _logger.InfoSafe($"Resource defined: {key} capacity={capacity}");

                    post.Add(() => _bus.Publish(new ResourceBayEvents.ResourceDefined
                    {
                        Key = key, Capacity = capacity
                    }));
                }
            });
        }

        public bool RemoveResource(string key)
        {
            ValidateKey(key);
            return SyncAndDispatch(post =>
            {
                int refs = 0;
                foreach (var rec in _allocById.Values)
                {
                    if (rec.Map.ContainsKey(key)) refs++;
                }
                if (refs > 0)
                    throw new InvalidOperationException($"Cannot remove resource '{key}' with {refs} active allocation(s).");

                bool removed = _resources.Remove(key);
                if (removed)
                {
                    _logger.InfoSafe($"Resource removed: {key}");
                    post.Add(() => _bus.Publish(new ResourceBayEvents.ResourceRemoved { Key = key }));
                }
                return removed;
            });
        }

        public bool Contains(string key)
        {
            ValidateKey(key);
            lock (_sync) { return _resources.ContainsKey(key); }
        }

        public long GetCapacity(string key)
        {
            ValidateKey(key);
            lock (_sync) { return GetState(key).Capacity; }
        }

        public bool HasCapacity(string key, long amount)
        {
            return GetCapacity(key) - amount >= 0;
        }

        public long GetAvailable(string key)
        {
            ValidateKey(key);
            lock (_sync) { return GetState(key).Available; }
        }
        
        public bool HasAvailable(string key, long amount)
        {
            return GetAvailable(key) - amount >= 0;
        }

        public long GetAllocatedTotal(string key)
        {
            ValidateKey(key);
            lock (_sync) { return GetState(key).AllocatedTotal; }
        }

        public void SetCapacity(string key, long newCapacity)
        {
            if (newCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(newCapacity), $"Capacity cannot be negative (key='{key}').");
            ValidateKey(key);

            SyncAndDispatch(post =>
            {
                var s = GetState(key);
                if (s.AllocatedTotal > newCapacity)
                    throw new InvalidOperationException($"Cannot set capacity < allocated for '{key}'. Allocated={s.AllocatedTotal}, NewCapacity={newCapacity}");
                long old = s.Capacity;
                s.Capacity = newCapacity;
                SetState(key, s);

                _logger.InfoSafe($"Resource capacity set: {key} {old} → {newCapacity}");

                post.Add(() => _bus.Publish(new ResourceBayEvents.CapacityChanged
                {
                    Key = key, OldCapacity = old, NewCapacity = newCapacity
                }));
            });
        }

        public void AdjustCapacity(string key, long delta)
        {
            ValidateKey(key);

            SyncAndDispatch(post =>
            {
                var s = GetState(key);
                var target = checked(s.Capacity + delta);
                if (target < s.AllocatedTotal)
                    throw new InvalidOperationException($"Cannot reduce capacity below allocated for '{key}'. Allocated={s.AllocatedTotal}, TargetCapacity={target}");
                long old = s.Capacity;
                s.Capacity = target;
                SetState(key, s);

                _logger.InfoSafe($"Resource capacity adjusted: {key} {old} → {target} (delta: {delta:+#;-#;0})");

                post.Add(() => _bus.Publish(new ResourceBayEvents.CapacityChanged
                {
                    Key = key, OldCapacity = old, NewCapacity = target
                }));
            });
        }

        public IEnumerable<string> GetAllKeys()
        {
            lock (_sync) { return new List<string>(_resources.Keys); }
        }

        public ResourceBaySnapshot GetSnapshot()
        {
            var snap = new ResourceBaySnapshot();
            lock (_sync)
            {
                foreach (var kv in _resources)
                {
                    snap.resources.Add(new ResourceBaySnapshot.ResourceEntry
                    {
                        key = kv.Key,
                        capacity = kv.Value.Capacity,
                        allocatedTotal = kv.Value.AllocatedTotal
                    });
                }
            }
            return snap;
        }

        public void LoadSnapshot(ResourceBaySnapshot snapshot)
        {
            SyncAndDispatch(post =>
            {
                if (_allocById.Count > 0)
                    throw new InvalidOperationException("Cannot load snapshot while allocations are active. Release them first.");

                _resources.Clear();
                if (snapshot?.resources != null)
                {
                    foreach (var r in snapshot.resources)
                    {
                        ValidateKey(r.key);
                        if (r.capacity < 0)
                            throw new ArgumentOutOfRangeException(nameof(snapshot), $"Capacity cannot be negative (key='{r.key}').");
                        if (r.allocatedTotal < 0)
                            throw new ArgumentOutOfRangeException(nameof(snapshot), $"Allocated cannot be negative (key='{r.key}').");
                        if (r.allocatedTotal > r.capacity)
                            throw new InvalidOperationException(
                                $"Snapshot has allocatedTotal({r.allocatedTotal}) > capacity({r.capacity}) for '{r.key}'.");

                        SetState(r.key, new ResourceState
                        {
                            Capacity = r.capacity,
                            AllocatedTotal = r.allocatedTotal
                        });
                    }
                }

                int count = snapshot?.resources?.Count ?? 0;
                _logger.InfoSafe($"ResourceBay snapshot loaded: {count} resources");
                post.Add(() => _bus.Publish(new ResourceBayEvents.SnapshotLoaded { ResourceCount = count }));
            });
        }

        /// <summary>
        /// Attempts to allocate resources. Returns Success=false with an Exception explaining the reason if it fails.
        /// If AllOrNothing=true (default) and any resource is short, nothing is debited.
        /// </summary>
        public AllocationResult TryAllocate(AllocationRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Resources == null)
                throw new ArgumentException($"Resources map cannot be null (owner='{request.OwnerId ?? "?"}').", nameof(request));
            if (request.Resources.Count == 0)
            {
                return new AllocationResult
                {
                    Success = true,
                    AllocationId = Guid.Empty,
                    Granted = new Dictionary<string, long>(StringComparer.Ordinal)
                };
            }

            return SyncAndDispatch(post =>
            {
                var result = new AllocationResult();

                // Validate input and compute shortages
                var shortages = new Dictionary<string, long>(StringComparer.Ordinal);
                foreach (var kv in request.Resources)
                {
                    var key = kv.Key;
                    var amount = kv.Value;

                    if (string.IsNullOrWhiteSpace(key))
                        throw new ArgumentException($"Resource key cannot be null or empty (owner='{request.OwnerId ?? "?"}').", nameof(request));
                    if (amount <= 0)
                        throw new ArgumentOutOfRangeException(nameof(request), $"Requested amount must be > 0 (key='{key}', owner='{request.OwnerId ?? "?"}').");

                    var s = GetState(key); // throws ResourceNotFoundException if missing
                    var available = s.Available;

                    if (available < amount)
                    {
                        shortages[key] = amount - available; // missing positive amount
                    }
                }

                // All-or-nothing enforcement
                if (request.AllOrNothing && shortages.Count > 0)
                {
                    var error = new InsufficientResourceException(shortages);
                    result.Success = false;
                    result.Error = error;

                    var shortageStr = string.Join(", ", shortages.Select(kv => $"{kv.Key}:-{kv.Value}"));
                    _logger.WarningSafe($"Allocation rejected (owner: {request.OwnerId}, all-or-nothing): shortage {shortageStr}");

                    var requestedCopy = new Dictionary<string, long>(request.Resources, StringComparer.Ordinal);
                    post.Add(() => _bus.Publish(new ResourceBayEvents.AllocationRejected
                    {
                        OwnerId = request.OwnerId,
                        Requested = requestedCopy,
                        Error = error,
                        Context = request.Context,
                        Tags = request.Tags ?? Array.Empty<string>()
                    }));

                    return result;
                }

                // Compute granted map
                var granted = new Dictionary<string, long>(StringComparer.Ordinal);
                foreach (var kv in request.Resources)
                {
                    var key = kv.Key;
                    var need = kv.Value;
                    var s = GetState(key);

                    long give = need;
                    if (!request.AllOrNothing && s.Available < need)
                        give = s.Available; // partial grant

                    if (give <= 0) continue;

                    s.AllocatedTotal = checked(s.AllocatedTotal + give);
                    if (s.AllocatedTotal > s.Capacity)
                        throw new InvalidOperationException($"Over-allocation detected on '{key}' (owner='{request.OwnerId ?? "?"}'). Capacity={s.Capacity}, Allocated={s.AllocatedTotal}");
                    SetState(key, s);

                    granted[key] = give;
                }

                if (granted.Count == 0)
                {
                    var error = new InsufficientResourceException(shortages.Count > 0
                        ? shortages
                        : new Dictionary<string, long>(StringComparer.Ordinal));
                    result.Success = false;
                    result.Error = error;

                    var shortageStr = shortages.Count > 0
                        ? string.Join(", ", shortages.Select(kv => $"{kv.Key}:-{kv.Value}"))
                        : "unknown";
                    _logger.WarningSafe($"Allocation rejected (owner: {request.OwnerId}): no resources granted, shortage {shortageStr}");

                    var requestedCopy = new Dictionary<string, long>(request.Resources, StringComparer.Ordinal);
                    post.Add(() => _bus.Publish(new ResourceBayEvents.AllocationRejected
                    {
                        OwnerId = request.OwnerId,
                        Requested = requestedCopy,
                        Error = error,
                        Context = request.Context,
                        Tags = request.Tags ?? Array.Empty<string>()
                    }));

                    return result;
                }

                // Create allocation record
                var id = Guid.NewGuid();
                var rec = new AllocationRecord
                {
                    Id = id,
                    OwnerId = request.OwnerId,
                    CreatedUtc = DateTime.UtcNow,
                    Map = granted,
                    Tags = request.Tags,
                    Context = request.Context
                };

                _allocById[id] = rec;

                if (!string.IsNullOrEmpty(rec.OwnerId))
                {
                    if (!_allocByOwner.TryGetValue(rec.OwnerId, out var set))
                    {
                        set = new HashSet<Guid>();
                        _allocByOwner[rec.OwnerId] = set;
                    }
                    set.Add(id);
                }

                result.Success = true;
                result.AllocationId = id;
                result.Granted = new Dictionary<string, long>(granted, StringComparer.Ordinal);

                var grantedStr = string.Join(", ", granted.Select(kv => $"{kv.Key}:{kv.Value}"));
                _logger.InfoSafe($"Allocation granted: {id} (owner: {request.OwnerId}, granted: {grantedStr})");

                var grantedCopy = new Dictionary<string, long>(granted, StringComparer.Ordinal);
                post.Add(() => _bus.Publish(new ResourceBayEvents.AllocationGranted
                {
                    AllocationId = id,
                    OwnerId = rec.OwnerId,
                    Granted = grantedCopy,
                    Context = rec.Context,
                    Tags = rec.Tags ?? Array.Empty<string>()
                }));

                return result;
            });
        }

        /// <summary>
        /// Releases an entire allocation. Throws if allocationId not found.
        /// </summary>
        public void Release(Guid allocationId)
        {
            SyncAndDispatch(post =>
            {
                if (!_allocById.TryGetValue(allocationId, out var rec))
                    throw new ArgumentException($"Allocation '{allocationId}' not found (Release).", nameof(allocationId));

                // Credit back
                foreach (var kv in rec.Map)
                {
                    var key = kv.Key;
                    var amount = kv.Value;

                    var s = GetState(key); // throws if resource missing
                    s.AllocatedTotal = checked(s.AllocatedTotal - amount);
                    if (s.AllocatedTotal < 0)
                        throw new InvalidOperationException($"Allocated total negative for '{key}' while releasing allocation '{allocationId}' (owner='{rec.OwnerId ?? "?"}').");
                    SetState(key, s);
                }

                var releasedStr = string.Join(", ", rec.Map.Select(kv => $"{kv.Key}:{kv.Value}"));
                _logger.InfoSafe($"Allocation released: {allocationId} (owner: {rec.OwnerId}, released: {releasedStr})");

                _allocById.Remove(allocationId);
                if (!string.IsNullOrEmpty(rec.OwnerId) && _allocByOwner.TryGetValue(rec.OwnerId, out var set))
                {
                    set.Remove(allocationId);
                    if (set.Count == 0) _allocByOwner.Remove(rec.OwnerId);
                }

                var releasedCopy = new Dictionary<string, long>(rec.Map, StringComparer.Ordinal);
                var owner = rec.OwnerId;
                post.Add(() => _bus.Publish(new ResourceBayEvents.AllocationReleased
                {
                    AllocationId = allocationId,
                    OwnerId = owner,
                    Released = releasedCopy
                }));
            });
        }

        /// <summary>
        /// Releases part of an allocation (per resource). If the allocation becomes empty, it is removed.
        /// Throws if amounts exceed what was granted.
        /// </summary>
        public void ReleasePartial(Guid allocationId, Dictionary<string, long> releaseMap)
        {
            if (releaseMap == null) throw new ArgumentNullException(nameof(releaseMap));

            SyncAndDispatch(post =>
            {
                if (!_allocById.TryGetValue(allocationId, out var rec))
                    throw new ArgumentException($"Allocation '{allocationId}' not found (ReleasePartial).", nameof(allocationId));

                // Validate
                foreach (var kv in releaseMap)
                {
                    var key = kv.Key;
                    var amount = kv.Value;
                    if (string.IsNullOrWhiteSpace(key))
                        throw new ArgumentException($"Resource key cannot be null or empty (allocation='{allocationId}', owner='{rec.OwnerId ?? "?"}').", nameof(releaseMap));
                    if (amount <= 0)
                        throw new ArgumentOutOfRangeException(nameof(releaseMap), $"Release amount must be > 0 (key='{key}', allocation='{allocationId}', owner='{rec.OwnerId ?? "?"}').");
                    if (!rec.Map.TryGetValue(key, out var granted))
                        throw new ArgumentException($"Allocation does not contain resource '{key}' (allocation='{allocationId}', owner='{rec.OwnerId ?? "?"}').", nameof(releaseMap));
                    if (amount > granted)
                        throw new ArgumentException($"Cannot release more than granted for '{key}' (allocation='{allocationId}', owner='{rec.OwnerId ?? "?"}').", nameof(releaseMap));
                }

                // Apply credit
                foreach (var kv in releaseMap)
                {
                    var key = kv.Key;
                    var amount = kv.Value;

                    var s = GetState(key);
                    s.AllocatedTotal = checked(s.AllocatedTotal - amount);
                    if (s.AllocatedTotal < 0)
                        throw new InvalidOperationException($"Allocated total negative for '{key}' while partial releasing allocation '{allocationId}' (owner='{rec.OwnerId ?? "?"}').");
                    SetState(key, s);

                    var newGranted = rec.Map[key] - amount;
                    if (newGranted > 0) rec.Map[key] = newGranted;
                    else rec.Map.Remove(key);
                }

                var owner = rec.OwnerId;

                if (rec.Map.Count == 0)
                {
                    _allocById.Remove(allocationId);
                    if (!string.IsNullOrEmpty(owner) && _allocByOwner.TryGetValue(owner, out var set))
                    {
                        set.Remove(allocationId);
                        if (set.Count == 0) _allocByOwner.Remove(owner);
                    }

                    post.Add(() => _bus.Publish(new ResourceBayEvents.AllocationReleased
                    {
                        AllocationId = allocationId,
                        OwnerId = owner,
                        Released = new Dictionary<string, long>(releaseMap, StringComparer.Ordinal)
                    }));
                }
                else
                {
                    post.Add(() => _bus.Publish(new ResourceBayEvents.AllocationPartiallyReleased
                    {
                        AllocationId = allocationId,
                        OwnerId = owner,
                        ReleasedPartial = new Dictionary<string, long>(releaseMap, StringComparer.Ordinal),
                        Remaining = new Dictionary<string, long>(rec.Map, StringComparer.Ordinal)
                    }));
                }
            });
        }

        public void ReleaseByOwner(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return;

            // Reuse Release(id) which already publishes events safely.
            List<Guid> idsToRelease = null;
            lock (_sync)
            {
                if (_allocByOwner.TryGetValue(ownerId, out var set) && set.Count > 0)
                    idsToRelease = new List<Guid>(set);
            }

            if (idsToRelease == null) return;

            _logger.InfoSafe($"Releasing all allocations for owner: {ownerId} (count: {idsToRelease.Count})");
            foreach (var id in idsToRelease)
                Release(id);
        }

        public List<AllocationInfo> GetOwnerAllocations(string ownerId)
        {
            var list = new List<AllocationInfo>();
            if (string.IsNullOrWhiteSpace(ownerId)) return list;

            lock (_sync)
            {
                if (!_allocByOwner.TryGetValue(ownerId, out var set) || set.Count == 0)
                    return list;

                foreach (var id in set)
                {
                    if (!_allocById.TryGetValue(id, out var rec)) continue;
                    list.Add(ToInfo(rec));
                }
            }
            return list;

            static AllocationInfo ToInfo(AllocationRecord r)
            {
                return new AllocationInfo(
                    r.Id,
                    r.OwnerId,
                    r.CreatedUtc,
                    new Dictionary<string, long>(r.Map, StringComparer.Ordinal),
                    r.Tags != null ? Array.AsReadOnly(r.Tags) : Array.Empty<string>(),
                    r.Context
                );
            }
        }

        // ====== New APIs (Item 1) ======

        /// <summary>Tries to fetch a single allocation as an AllocationInfo snapshot.</summary>
        public bool TryGetAllocation(Guid id, out AllocationInfo info)
        {
            lock (_sync)
            {
                if (_allocById.TryGetValue(id, out var rec))
                {
                    info = new AllocationInfo(
                        rec.Id,
                        rec.OwnerId,
                        rec.CreatedUtc,
                        new Dictionary<string, long>(rec.Map, StringComparer.Ordinal),
                        rec.Tags != null ? Array.AsReadOnly(rec.Tags) : Array.Empty<string>(),
                        rec.Context
                    );
                    return true;
                }
            }
            info = null;
            return false;
        }

        /// <summary>Returns a snapshot list of all active allocations.</summary>
        public List<AllocationInfo> GetAllAllocations()
        {
            var list = new List<AllocationInfo>();
            lock (_sync)
            {
                foreach (var rec in _allocById.Values)
                {
                    list.Add(new AllocationInfo(
                        rec.Id,
                        rec.OwnerId,
                        rec.CreatedUtc,
                        new Dictionary<string, long>(rec.Map, StringComparer.Ordinal),
                        rec.Tags != null ? Array.AsReadOnly(rec.Tags) : Array.Empty<string>(),
                        rec.Context
                    ));
                }
            }
            return list;
        }

        /// <summary>Lightweight totals per resource key.</summary>
        public Dictionary<string, ResourceTotals> GetTotals()
        {
            var dict = new Dictionary<string, ResourceTotals>(StringComparer.Ordinal);
            lock (_sync)
            {
                foreach (var kv in _resources)
                {
                    var s = kv.Value;
                    dict[kv.Key] = new ResourceTotals(s.Capacity, s.AllocatedTotal);
                }
            }
            return dict;
        }

        /// <summary>
        /// Releases all active allocations (publishes AllocationReleased for each). No-op if empty.
        /// </summary>
        public void ResetAllAllocations()
        {
            // Snapshot IDs to reuse existing Release(id) logic (safe and eventful).
            List<Guid> ids;
            lock (_sync)
            {
                if (_allocById.Count == 0) return;
                ids = new List<Guid>(_allocById.Keys);
            }

            foreach (var id in ids)
                Release(id);
        }
    }
}
