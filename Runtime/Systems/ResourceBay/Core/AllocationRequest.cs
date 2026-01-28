// BrewedCode/ResourceBay/AllocationRequest.cs
using System;
using System.Collections.Generic;

namespace BrewedCode.ResourceBay
{
    /// <summary>
    /// Allocation request describing how much of each resource is needed.
    /// AllOrNothing: if true (default), the request succeeds only if all resources fit.
    /// OwnerId/Tags/Context: metadata for tracking and debug.
    /// </summary>
    public sealed class AllocationRequest
    {
        /// <summary>Map: resource key -> amount (must be > 0).</summary>
        public Dictionary<string, long> Resources { get; set; } = new(StringComparer.Ordinal);

        /// <summary>If true, the request succeeds only if all resources fit. Default true.</summary>
        public bool AllOrNothing { get; set; } = true;

        /// <summary>Optional owner identifier (e.g., "PlantPlot#42").</summary>
        public string OwnerId { get; set; }

        /// <summary>Optional free-form tags for diagnostics.</summary>
        public string[] Tags { get; set; }

        /// <summary>Optional text context for diagnostics.</summary>
        public string Context { get; set; }
    }
}