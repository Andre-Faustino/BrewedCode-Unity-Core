namespace BrewedCode.Crafting
{
    public interface IResourceCost
    {
        /// <summary>
        /// ID of the resource allocation (for deallocation during payback).
        /// Empty Guid if no allocation was made (e.g., no resource requirements).
        /// </summary>
        System.Guid AllocationId { get; }

        /// <summary>
        /// Withdraws the required resources for crafting from the specified station.
        /// </summary>
        /// <param name="stationId">The ID of the crafting station making this request.</param>
        /// <returns>True if cost was successfully withdrawn, false otherwise.</returns>
        bool WithdrawCost(CraftingStationId stationId);

        /// <summary>
        /// Returns the required resources for crafting to the specified station.
        /// </summary>
        /// <param name="stationId">The ID of the crafting station making this request.</param>
        /// <param name="allocationId">The ID of the resource allocation to deallocate.</param>
        /// <returns>True if cost was successfully returned, false otherwise.</returns>
        bool PaybackCost(CraftingStationId stationId, System.Guid allocationId);
    }

}