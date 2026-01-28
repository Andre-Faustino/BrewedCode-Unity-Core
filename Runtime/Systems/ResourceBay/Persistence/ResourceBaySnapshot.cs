// BrewedCode/ResourceBay/ResourceBaySnapshot.cs
using System;
using System.Collections.Generic;

namespace BrewedCode.ResourceBay
{
    /// <summary>
    /// Serializable snapshot of ResourceBay state (capacities and allocated totals).
    /// allocatedTotal will normally be zero (no allocation system yet).
    /// </summary>
    [Serializable]
    public sealed class ResourceBaySnapshot
    {
        [Serializable]
        public sealed class ResourceEntry
        {
            public string key;
            public long capacity;
            public long allocatedTotal;
        }

        public List<ResourceEntry> resources = new List<ResourceEntry>();
    }
}