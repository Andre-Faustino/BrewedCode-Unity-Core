// BrewedCode/ResourceBay/AllocationInfo.cs
using System;
using System.Collections.Generic;

namespace BrewedCode.ResourceBay
{
    /// <summary>
    /// Immutable view of an active allocation (for queries/inspection).
    /// </summary>
    public sealed class AllocationInfo
    {
        public Guid AllocationId { get; }
        public string OwnerId { get; }
        public DateTime CreatedUtc { get; }
        public IReadOnlyDictionary<string, long> Resources { get; }
        public IReadOnlyList<string> Tags { get; }
        public string Context { get; }

        public AllocationInfo(Guid id, string ownerId, DateTime createdUtc,
            IReadOnlyDictionary<string, long> resources,
            IReadOnlyList<string> tags, string context)
        {
            AllocationId = id;
            OwnerId = ownerId;
            CreatedUtc = createdUtc;
            Resources = resources;
            Tags = tags;
            Context = context;
        }
    }
}