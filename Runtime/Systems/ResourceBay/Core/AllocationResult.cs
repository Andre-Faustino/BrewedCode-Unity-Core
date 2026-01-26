// BrewedCode/ResourceBay/AllocationResult.cs
using System;
using System.Collections.Generic;

namespace BrewedCode.ResourceBay
{
    /// <summary>
    /// Result of an allocation attempt. If Success=false, Error contains details.
    /// For Success=true, AllocationId and Granted are set.
    /// </summary>
    public sealed class AllocationResult
    {
        public bool Success { get; set; }
        public Guid AllocationId { get; set; }
        public Dictionary<string, long> Granted { get; set; } = new(StringComparer.Ordinal);
        public Exception? Error { get; set; } // InsufficientResourceException or ResourceNotFoundException
    }
}