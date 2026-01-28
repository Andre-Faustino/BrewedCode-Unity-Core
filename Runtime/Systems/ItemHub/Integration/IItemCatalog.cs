using System.Collections.Generic;

namespace BrewedCode.ItemHub
{
    /// <summary>
    /// Catalog contract to resolve ItemMeta by ItemId.
    /// </summary>
    public interface IItemCatalog
    {
        bool TryGetMeta(ItemId id, out ItemMeta meta);
        ItemId AddToCatalog(ItemMeta meta);
        IEnumerable<ItemMeta> GetAllMetas();
    }
}