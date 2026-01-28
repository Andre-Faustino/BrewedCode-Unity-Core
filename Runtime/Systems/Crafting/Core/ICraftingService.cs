namespace BrewedCode.Crafting
{
    /// <summary>
    /// Public API for the Crafting System.
    /// Manages crafting stations and processes.
    /// All dependencies injected via constructor.
    /// </summary>
    public interface ICraftingService
    {
        /// <summary>
        /// Registers a crafting station with the service.
        /// Stations must be registered before they can start crafting.
        /// </summary>
        /// <param name="stationId">The unique identifier of the station.</param>
        /// <param name="controller">Optional controller for the station (can be null).</param>
        void RegisterStation(CraftingStationId stationId, ICraftingStationController? controller);

        /// <summary>
        /// Unregisters a crafting station from the service.
        /// Any active crafting at the station is cancelled.
        /// </summary>
        /// <param name="stationId">The unique identifier of the station.</param>
        void UnregisterStation(CraftingStationId stationId);

        /// <summary>
        /// Attempts to start a crafting process at a station.
        /// Fails if cost withdrawal fails or requirements aren't met.
        /// </summary>
        /// <param name="stationId">The station where crafting should start.</param>
        /// <param name="craftable">The item to craft.</param>
        /// <param name="amount">The number of items to craft sequentially (default 1).</param>
        /// <param name="error">If the operation fails, contains a description of the error.</param>
        /// <returns>True if crafting started successfully; otherwise, false.</returns>
        bool TryStartCrafting(CraftingStationId stationId, ICraftable craftable, int amount, out string error);

        /// <summary>
        /// Attempts to pause crafting at a station.
        /// </summary>
        /// <param name="stationId">The station to pause.</param>
        /// <param name="error">If the operation fails, contains a description of the error.</param>
        /// <returns>True if crafting was paused successfully; otherwise, false.</returns>
        bool TryPauseCrafting(CraftingStationId stationId, out string error);

        /// <summary>
        /// Attempts to resume paused crafting at a station.
        /// </summary>
        /// <param name="stationId">The station to resume.</param>
        /// <param name="error">If the operation fails, contains a description of the error.</param>
        /// <returns>True if crafting was resumed successfully; otherwise, false.</returns>
        bool TryResumeCrafting(CraftingStationId stationId, out string error);

        /// <summary>
        /// Attempts to stop/cancel crafting at a station.
        /// </summary>
        /// <param name="stationId">The station where crafting should stop.</param>
        /// <param name="error">If the operation fails, contains a description of the error.</param>
        /// <returns>True if crafting was stopped successfully; otherwise, false.</returns>
        bool TryStopCrafting(CraftingStationId stationId, out string error);

        /// <summary>
        /// Updates all active crafting processes by the specified delta time.
        /// Should be called once per frame from MonoBehaviour.Update().
        /// </summary>
        /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
        void Tick(float deltaTime);

        /// <summary>
        /// Gets read-only information about a station's state.
        /// </summary>
        /// <param name="stationId">The station to query.</param>
        /// <returns>Station information if registered; null if not found.</returns>
        CraftingStationInfo? GetStationInfo(CraftingStationId stationId);
    }

    /// <summary>
    /// Marker interface for crafting station controllers.
    /// Allows loose coupling between service and station implementations.
    /// </summary>
    public interface ICraftingStationController
    {
    }
}
