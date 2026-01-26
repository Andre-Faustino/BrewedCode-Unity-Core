using System;
using BrewedCode.Events;

namespace BrewedCode.Crafting
{
    /// <summary>
    /// Legacy event system for backward compatibility.
    /// Consider using the new IEventBus events instead.
    /// </summary>
    public enum CraftingStationEventType
    {
        ChangeState,
        Cancelled,
        Finished
    }

    /// <summary>
    /// Legacy event struct for backward compatibility.
    /// Consider using the new IEventBus events instead.
    /// </summary>
    public struct CraftingStationEvent
    {
        public CraftingStationEventType EventType;
        public CraftingStation CraftingStation;

        public CraftingStationEvent(CraftingStationEventType eventType, CraftingStation craftingStation)
        {
            EventType = eventType;
            CraftingStation = craftingStation;
        }

        public static void Trigger(CraftingStationEventType eventType, CraftingStation craftingStation)
        {
            EventChannel<CraftingStationEvent>.Trigger(new CraftingStationEvent(eventType, craftingStation));
        }
    }

    /// <summary>
    /// Event published when crafting starts at a station.
    /// </summary>
    public sealed class CraftingStartedEvent
    {
        /// <summary>The station where crafting started.</summary>
        public CraftingStationId StationId { get; set; }

        /// <summary>The unique process ID for this crafting session.</summary>
        public Guid ProcessId { get; set; }

        /// <summary>The item being crafted.</summary>
        public ICraftable Craftable { get; set; }
    }

    /// <summary>
    /// Event published as crafting progresses (once per frame during crafting).
    /// </summary>
    public sealed class CraftingProgressEvent
    {
        /// <summary>The station where crafting is progressing.</summary>
        public CraftingStationId StationId { get; set; }

        /// <summary>The unique process ID for this crafting session.</summary>
        public Guid ProcessId { get; set; }

        /// <summary>Progress as a normalized value (0-1).</summary>
        public float Progress { get; set; }

        /// <summary>Time elapsed in the current crafting session (seconds).</summary>
        public float TimeElapsed { get; set; }

        /// <summary>Time remaining in the current crafting session (seconds).</summary>
        public float TimeRemaining { get; set; }
    }

    /// <summary>
    /// Event published when crafting completes successfully at a station.
    /// </summary>
    public sealed class CraftingCompletedEvent
    {
        /// <summary>The station where crafting completed.</summary>
        public CraftingStationId StationId { get; set; }

        /// <summary>The unique process ID for this crafting session.</summary>
        public Guid ProcessId { get; set; }
    }

    /// <summary>
    /// Event published when crafting is paused at a station.
    /// </summary>
    public sealed class CraftingPausedEvent
    {
        /// <summary>The station where crafting was paused.</summary>
        public CraftingStationId StationId { get; set; }

        /// <summary>The unique process ID for this crafting session.</summary>
        public Guid ProcessId { get; set; }
    }

    /// <summary>
    /// Event published when paused crafting is resumed at a station.
    /// </summary>
    public sealed class CraftingResumedEvent
    {
        /// <summary>The station where crafting was resumed.</summary>
        public CraftingStationId StationId { get; set; }

        /// <summary>The unique process ID for this crafting session.</summary>
        public Guid ProcessId { get; set; }
    }

    /// <summary>
    /// Event published when crafting is stopped/cancelled at a station.
    /// </summary>
    public sealed class CraftingStoppedEvent
    {
        /// <summary>The station where crafting was stopped.</summary>
        public CraftingStationId StationId { get; set; }
    }
}