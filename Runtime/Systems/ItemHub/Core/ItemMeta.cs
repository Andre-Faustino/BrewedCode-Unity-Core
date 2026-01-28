using System.Collections.Generic;

namespace BrewedCode.ItemHub
{
    /// <summary>
    /// Item metadata resolved from an external catalog (no direct SO references here).
    /// </summary>
    public sealed class ItemMeta
    {
        public ItemId Id { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public StorageMode StorageMode { get; set; } = StorageMode.Commodity;
        
    }
}