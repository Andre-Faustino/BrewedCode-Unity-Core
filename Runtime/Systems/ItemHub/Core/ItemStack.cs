namespace BrewedCode.ItemHub
{
    /// <summary>
    /// Represents a consolidated stack entry for commodities.
    /// </summary>
    public readonly struct ItemStack
    {
        public ItemId Id { get; }
        public int Quantity { get; }

        public ItemStack(ItemId id, int quantity)
        {
            Id = id;
            Quantity = quantity;
        }

        public override string ToString() => $"{Id} x {Quantity}";
    }
}