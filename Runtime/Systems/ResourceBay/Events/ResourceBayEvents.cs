using System;
using System.Collections.Generic;

namespace BrewedCode.ResourceBay
{
    
    /// <summary>
    /// Event payload types for ResourceBay.
    /// </summary>
    public static class ResourceBayEvents
    {
        public sealed class ResourceDefined { public string Key { get; init; } public long Capacity { get; init; } }
        public sealed class ResourceRemoved { public string Key { get; init; } }
        public sealed class CapacityChanged { public string Key { get; init; } public long OldCapacity { get; init; } public long NewCapacity { get; init; } }

        public sealed class AllocationGranted
        {
            public Guid AllocationId { get; init; }
            public string OwnerId { get; init; }
            public IReadOnlyDictionary<string, long> Granted { get; init; }
            public string Context { get; init; }
            public IReadOnlyList<string> Tags { get; init; }
        }

        public sealed class AllocationRejected
        {
            public string OwnerId { get; init; }
            public IReadOnlyDictionary<string, long> Requested { get; init; }
            public Exception Error { get; init; } // InsufficientResourceException or ResourceNotFoundException
            public string Context { get; init; }
            public IReadOnlyList<string> Tags { get; init; }
        }

        public sealed class AllocationReleased
        {
            public Guid AllocationId { get; init; }
            public string OwnerId { get; init; }
            public IReadOnlyDictionary<string, long> Released { get; init; }
        }

        public sealed class AllocationPartiallyReleased
        {
            public Guid AllocationId { get; init; }
            public string OwnerId { get; init; }
            public IReadOnlyDictionary<string, long> ReleasedPartial { get; init; }
            public IReadOnlyDictionary<string, long> Remaining { get; init; }
        }

        public sealed class SnapshotLoaded { public int ResourceCount { get; init; } }
    }
}