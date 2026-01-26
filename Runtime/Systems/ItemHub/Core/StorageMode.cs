namespace BrewedCode.ItemHub
{
    public enum StorageMode
    {
        Commodity = 0, // stackable / immutable (stored as total quantity)
        Instance  = 1  // non-stackable / mutable (stored as individual records)
    }
}