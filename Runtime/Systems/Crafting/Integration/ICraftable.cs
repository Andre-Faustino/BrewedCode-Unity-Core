namespace BrewedCode.Crafting
{
    public interface ICraftable
    {
        public bool ValidateRequirements(int amount = 1);
        public float GetCraftDuration();
        public IResourceCost GetCraftingCost();
    }
}