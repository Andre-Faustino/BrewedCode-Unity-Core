// BrewedCode/ResourceBay/IResourceBay.cs
using System;
using System.Collections.Generic;

namespace BrewedCode.ResourceBay
{
    /// <summary>
    /// Public contract for the Resource Bay service.
    /// </summary>
    public interface IResourceBay
    {
        // ---- Resource definition / capacity ----
        void DefineResource(string key, long capacity);
        bool RemoveResource(string key);
        bool Contains(string key);
        long GetCapacity(string key);
        bool HasCapacity(string key, long amount);
        long GetAvailable(string key);
        bool HasAvailable(string key, long amount);
        long GetAllocatedTotal(string key);
        void SetCapacity(string key, long newCapacity);
        void AdjustCapacity(string key, long delta);
        IEnumerable<string> GetAllKeys();

        // ---- Snapshot ----
        ResourceBaySnapshot GetSnapshot();
        void LoadSnapshot(ResourceBaySnapshot snapshot);

        // ---- Allocation API ----
        AllocationResult TryAllocate(AllocationRequest request);
        void Release(Guid allocationId);
        void ReleasePartial(Guid allocationId, Dictionary<string, long> releaseMap);
        void ReleaseByOwner(string ownerId);
        List<AllocationInfo> GetOwnerAllocations(string ownerId);

        // ---- New utilities (Item 1) ----
        bool TryGetAllocation(Guid id, out AllocationInfo info);
        List<AllocationInfo> GetAllAllocations();
        Dictionary<string, ResourceTotals> GetTotals();
        void ResetAllAllocations();
    }

    /// <summary>
    /// Compact per-resource totals.
    /// </summary>
    public readonly struct ResourceTotals
    {
        public long Capacity { get; }
        public long Allocated { get; }
        public long Available => Capacity - Allocated;

        public ResourceTotals(long capacity, long allocated)
        {
            Capacity = capacity;
            Allocated = allocated;
        }
    }
}