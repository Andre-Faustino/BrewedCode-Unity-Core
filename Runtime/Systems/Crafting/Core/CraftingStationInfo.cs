namespace BrewedCode.Crafting
{
    /// <summary>
    /// Read-only data transfer object containing snapshot information about a crafting station.
    /// Allows external code to query station state without direct access to internal state.
    /// </summary>
    public sealed class CraftingStationInfo
    {
        /// <summary>
        /// Unique identifier of the station.
        /// </summary>
        public CraftingStationId Id { get; }

        /// <summary>
        /// Current state of the station (Idle, Crafting, Paused).
        /// </summary>
        public CraftingStationState State { get; }

        /// <summary>
        /// Number of crafting processes currently queued (including the active one).
        /// </summary>
        public int QueuedCount { get; }

        /// <summary>
        /// Progress of the current crafting process (0-1).
        /// 0 when not crafting, 1 when complete.
        /// </summary>
        public float Progress { get; }

        /// <summary>
        /// Time elapsed in the current crafting process (seconds).
        /// </summary>
        public float TimeElapsed { get; }

        /// <summary>
        /// Total time required for the current crafting process (seconds).
        /// </summary>
        public float TimeTotal { get; }

        /// <summary>
        /// Time remaining in the current crafting process (seconds).
        /// </summary>
        public float TimeRemaining { get; }

        /// <summary>
        /// The craftable item currently being crafted (null if Idle).
        /// </summary>
        public ICraftable CurrentCraftable { get; }

        /// <summary>
        /// Creates a new station info snapshot.
        /// </summary>
        public CraftingStationInfo(
            CraftingStationId id,
            CraftingStationState state,
            int queuedCount,
            float progress,
            float timeElapsed,
            float timeTotal,
            float timeRemaining,
            ICraftable currentCraftable = null)
        {
            Id = id;
            State = state;
            QueuedCount = queuedCount;
            Progress = progress;
            TimeElapsed = timeElapsed;
            TimeTotal = timeTotal;
            TimeRemaining = timeRemaining;
            CurrentCraftable = currentCraftable;
        }
    }
}
