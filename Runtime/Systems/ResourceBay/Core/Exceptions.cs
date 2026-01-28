// BrewedCode/ResourceBay/Exceptions.cs
using System;
using System.Collections.Generic;

namespace BrewedCode.ResourceBay
{
    /// <summary>
    /// Thrown when a resource key is not defined in the pool.
    /// Considered a programming/configuration error, not a normal flow.
    /// </summary>
    public sealed class ResourceNotFoundException : Exception
    {
        public string Key { get; }

        public ResourceNotFoundException(string key)
            : base($"Resource '{key}' was not found in ResourceBay.")
        {
            Key = key;
        }
    }
    
    /// <summary>
    /// Normal-flow failure when an allocation cannot be satisfied due to shortages.
    /// Contains the missing amounts per resource key.
    /// </summary>
    public sealed class InsufficientResourceException : Exception
    {
        /// <summary>Map: resource key -> missing amount (always > 0).</summary>
        public IReadOnlyDictionary<string, long> Shortages { get; }

        public InsufficientResourceException(IDictionary<string, long> shortages)
            : base("Allocation failed due to insufficient resources.")
        {
            Shortages = new Dictionary<string, long>(shortages, StringComparer.Ordinal);
        }
    }
}