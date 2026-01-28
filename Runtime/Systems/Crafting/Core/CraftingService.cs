using System;
using System.Collections.Generic;
using BrewedCode.Events;
using BrewedCode.Logging;

namespace BrewedCode.Crafting
{
    /// <summary>
    /// Pure C# crafting service - domain logic for crafting operations.
    ///
    /// Responsibilities:
    /// - Register/unregister crafting stations
    /// - Queue crafting processes (FIFO: 1 active + N queued)
    /// - Manage process state machine (Waiting → Processing → Paused → Finished)
    /// - Validate resource costs and handle atomic withdrawal
    /// - Advance processes via Tick(deltaTime)
    /// - Publish crafting events for UI/game reactions
    ///
    /// Architecture:
    /// - One active process per station (being crafted)
    /// - Queue of pending processes (waiting to start)
    /// - Auto-advance: when active finishes, start next queued
    /// - All business logic 100% testable via unit tests
    /// - No Unity dependencies, no FindObjectOfType
    ///
    /// Key Safety:
    /// - Atomic cost withdrawal (all-or-nothing)
    /// - Rollback on failure (refund allocated resources)
    /// - Cost deducted ONLY after validation
    /// - Queue preserved even if withdrawal fails
    /// </summary>
    public sealed class CraftingService : ICraftingService
    {
        /// <summary>
        /// Internal state machine for individual crafting processes.
        ///
        /// Waiting → Processing → Paused → Processing → Finished
        ///        ↓                        ↓
        ///        └──────────────────────→ Cancelled (via TryStopCrafting)
        /// </summary>
        private enum ProcessState
        {
            Waiting,    // Queued, not yet started
            Processing, // Active, time counting down
            Paused,     // Active but paused (via TryPauseCrafting)
            Finished,   // Time completed (ready to remove from queue)
        }

        private readonly IEventBus _eventBus;
        private readonly ILog? _logger;
        private readonly Dictionary<CraftingStationId, StationState> _stations = new();
        private readonly Dictionary<Guid, ProcessStateData> _processes = new();

        /// <summary>
        /// Initializes the crafting service with an event bus and logging service.
        /// </summary>
        /// <param name="eventBus">Event bus for publishing crafting events.</param>
        /// <param name="loggingService">Logging service for system diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown if eventBus or loggingService is null.</exception>
        public CraftingService(IEventBus eventBus, ILoggingService loggingService)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = loggingService?.GetLogger<CraftingService>() ?? throw new ArgumentNullException(nameof(loggingService));
        }

        /// <inheritdoc/>
        public void RegisterStation(CraftingStationId stationId, ICraftingStationController? controller)
        {
            if (_stations.ContainsKey(stationId))
            {
                _logger.Warning($"Crafting station {stationId} is already registered.");
                return;
            }

            _stations[stationId] = new StationState
            {
                Id = stationId,
                State = CraftingStationState.Idle,
                Controller = controller,
            };

            _logger.Info($"Crafting station {stationId} registered and ready.");
        }

        /// <inheritdoc/>
        public void UnregisterStation(CraftingStationId stationId)
        {
            if (!_stations.TryGetValue(stationId, out var station))
                return;

            int queuedCount = (station.ActiveProcessId.HasValue ? 1 : 0) + station.QueuedProcessIds.Count;
            _logger.Info($"Unregistering crafting station {stationId} (active: {station.State}, queued: {queuedCount})");

            // Cancel any active crafting
            if (station.ActiveProcessId.HasValue)
            {
                if (_processes.TryGetValue(station.ActiveProcessId.Value, out var process))
                {
                    _logger.Info($"Stopping active process on station {stationId}");
                    StopProcess(stationId, process);
                }
                station.ActiveProcessId = null;
            }

            // Clear queued processes
            if (station.QueuedProcessIds.Count > 0)
            {
                _logger.Warning($"Discarding {station.QueuedProcessIds.Count} queued items on station {stationId}");
                foreach (var processId in station.QueuedProcessIds)
                {
                    _processes.Remove(processId);
                }
                station.QueuedProcessIds.Clear();
            }

            _stations.Remove(stationId);
            _logger.Info($"Crafting station {stationId} unregistered.");
        }

        /// <inheritdoc/>
        public bool TryStartCrafting(CraftingStationId stationId, ICraftable craftable, int amount, out string error)
        {
            error = "";

            if (!_stations.TryGetValue(stationId, out var station))
            {
                error = $"Station {stationId} not registered.";
                _logger.Error(error);
                return false;
            }

            if (!craftable.ValidateRequirements(amount))
            {
                error = "Crafting requirements not met.";
                _logger.Warning($"Cannot start crafting on {stationId}: {error}");
                return false;
            }

            if (station.State != CraftingStationState.Idle)
            {
                error = $"Station is not idle (current state: {station.State}).";
                _logger.Warning($"Cannot start crafting on {stationId}: {error}");
                return false;
            }

            // Create and queue processes
            var processIds = new List<Guid>();
            for (int i = 0; i < amount; i++)
            {
                var process = CreateProcess(craftable);
                processIds.Add(process.Id);
                station.QueuedProcessIds.Enqueue(process.Id);
            }

            // Start the first process with atomic cost withdrawal
            if (station.QueuedProcessIds.Count > 0)
            {
                var firstProcessId = station.QueuedProcessIds.Dequeue();
                if (_processes.TryGetValue(firstProcessId, out var firstProcess))
                {
                    // Get cost (must be defined)
                    var cost = firstProcess.Craftable.GetCraftingCost();
                    if (cost == null)
                    {
                        error = "Craftable has no cost defined.";
                        _logger.Error($"Cannot start crafting on {stationId}: {error}");
                        CleanupQueuedProcesses(station, firstProcessId);
                        return false;
                    }

                    // Attempt atomic cost withdrawal (includes rollback if allocation fails)
                    if (!cost.WithdrawCost(stationId))
                    {
                        error = "Insufficient resources for crafting (items/resources insufficient).";
                        _logger.Warning($"Cannot start crafting on {stationId}: {error}");
                        CleanupQueuedProcesses(station, firstProcessId);
                        return false;
                    }

                    // Cost successfully withdrawn - transition to processing
                    firstProcess.State = ProcessState.Processing;
                    firstProcess.Cost = cost;
                    firstProcess.AllocationId = cost.AllocationId;
                    station.ActiveProcessId = firstProcessId;
                    station.State = CraftingStationState.Crafting;

                    int totalQueued = (station.ActiveProcessId.HasValue ? 1 : 0) + station.QueuedProcessIds.Count;
                    _logger.Info($"Crafting started on station {stationId}: {craftable} x{amount} (total in queue: {totalQueued})");

                    _eventBus.Publish(new CraftingStartedEvent
                    {
                        StationId = stationId,
                        ProcessId = firstProcessId,
                        Craftable = craftable,
                    });

                    return true;
                }
            }

            error = "Failed to create crafting process.";
            _logger.Error($"Cannot start crafting on {stationId}: {error}");
            return false;
        }

        /// <inheritdoc/>
        public bool TryPauseCrafting(CraftingStationId stationId, out string error)
        {
            error = "";

            if (!_stations.TryGetValue(stationId, out var station))
            {
                error = $"Station {stationId} not registered.";
                _logger.Error(error);
                return false;
            }

            if (station.State != CraftingStationState.Crafting)
            {
                error = "Station is not currently crafting.";
                _logger.Warning($"Cannot pause on {stationId}: {error}");
                return false;
            }

            if (!station.ActiveProcessId.HasValue || !_processes.TryGetValue(station.ActiveProcessId.Value, out var process))
            {
                error = "No active process found.";
                _logger.Error(error);
                return false;
            }

            process.State = ProcessState.Paused;
            station.State = CraftingStationState.Paused;

            _logger.Info($"Crafting paused on station {stationId} (progress: {(process.DurationTotal > 0 ? process.TimeElapsed / process.DurationTotal : 0):P0})");

            _eventBus.Publish(new CraftingPausedEvent
            {
                StationId = stationId,
                ProcessId = process.Id,
            });

            return true;
        }

        /// <inheritdoc/>
        public bool TryResumeCrafting(CraftingStationId stationId, out string error)
        {
            error = "";

            if (!_stations.TryGetValue(stationId, out var station))
            {
                error = $"Station {stationId} not registered.";
                _logger.Error(error);
                return false;
            }

            if (station.State != CraftingStationState.Paused)
            {
                error = "Station is not paused.";
                _logger.Warning($"Cannot resume on {stationId}: {error}");
                return false;
            }

            if (!station.ActiveProcessId.HasValue || !_processes.TryGetValue(station.ActiveProcessId.Value, out var process))
            {
                error = "No active process found.";
                _logger.Error(error);
                return false;
            }

            process.State = ProcessState.Processing;
            station.State = CraftingStationState.Crafting;

            _logger.Info($"Crafting resumed on station {stationId}");

            _eventBus.Publish(new CraftingResumedEvent
            {
                StationId = stationId,
                ProcessId = process.Id,
            });

            return true;
        }

        /// <inheritdoc/>
        public bool TryStopCrafting(CraftingStationId stationId, out string error)
        {
            error = "";

            if (!_stations.TryGetValue(stationId, out var station))
            {
                error = $"Station {stationId} not registered.";
                _logger.Error(error);
                return false;
            }

            if (station.State == CraftingStationState.Idle)
            {
                error = "Station is already idle.";
                _logger.Warning($"Cannot stop on {stationId}: {error}");
                return false;
            }

            int queuedCount = (station.ActiveProcessId.HasValue ? 1 : 0) + station.QueuedProcessIds.Count;
            _logger.Info($"Stopping all crafting on station {stationId} (stopping {queuedCount} items)");

            // Stop active process
            if (station.ActiveProcessId.HasValue && _processes.TryGetValue(station.ActiveProcessId.Value, out var process))
            {
                StopProcess(stationId, process);
            }

            // Clear queue
            foreach (var processId in station.QueuedProcessIds)
            {
                _processes.Remove(processId);
            }
            station.QueuedProcessIds.Clear();

            station.ActiveProcessId = null;
            station.State = CraftingStationState.Idle;

            _eventBus.Publish(new CraftingStoppedEvent
            {
                StationId = stationId,
            });

            return true;
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            var stationsToUpdate = new List<CraftingStationId>();
            foreach (var station in _stations.Values)
            {
                if (station.State == CraftingStationState.Crafting && station.ActiveProcessId.HasValue)
                {
                    stationsToUpdate.Add(station.Id);
                }
            }

            foreach (var stationId in stationsToUpdate)
            {
                UpdateStation(stationId, deltaTime);
            }
        }

        /// <inheritdoc/>
        public CraftingStationInfo? GetStationInfo(CraftingStationId stationId)
        {
            if (!_stations.TryGetValue(stationId, out var station))
                return null;

            float progress = 0f;
            float timeElapsed = 0f;
            float timeTotal = 0f;
            float timeRemaining = 0f;
            ICraftable currentCraftable = null;

            if (station.ActiveProcessId.HasValue && _processes.TryGetValue(station.ActiveProcessId.Value, out var process))
            {
                timeTotal = process.DurationTotal;
                timeElapsed = process.TimeElapsed;
                timeRemaining = process.TimeRemaining;
                progress = timeTotal > 0 ? timeElapsed / timeTotal : 0f;
                currentCraftable = process.Craftable;
            }

            return new CraftingStationInfo(
                id: stationId,
                state: station.State,
                queuedCount: (station.ActiveProcessId.HasValue ? 1 : 0) + station.QueuedProcessIds.Count,
                progress: progress,
                timeElapsed: timeElapsed,
                timeTotal: timeTotal,
                timeRemaining: timeRemaining,
                currentCraftable: currentCraftable
            );
        }

        /// <summary>
        /// Updates a single station's crafting process by delta time.
        /// </summary>
        private void UpdateStation(CraftingStationId stationId, float deltaTime)
        {
            if (!_stations.TryGetValue(stationId, out var station))
                return;

            if (!station.ActiveProcessId.HasValue)
                return;

            var processId = station.ActiveProcessId.Value;
            if (!_processes.TryGetValue(processId, out var process))
                return;

            if (process.State != ProcessState.Processing)
                return;

            // Update process time
            process.TimeElapsed += deltaTime;
            process.TimeRemaining = System.Math.Max(0f, process.DurationTotal - process.TimeElapsed);

            // Publish progress event
            _eventBus.Publish(new CraftingProgressEvent
            {
                StationId = stationId,
                ProcessId = processId,
                Progress = process.DurationTotal > 0 ? process.TimeElapsed / process.DurationTotal : 0f,
                TimeElapsed = process.TimeElapsed,
                TimeRemaining = process.TimeRemaining,
            });

            // Check if process is complete
            const float COMPLETION_EPSILON = 0.0001f;
            if (process.TimeElapsed >= process.DurationTotal - COMPLETION_EPSILON)
            {
                process.State = ProcessState.Finished;

                _logger.Info($"Crafting completed on station {stationId}: {process.Craftable}");

                _eventBus.Publish(new CraftingCompletedEvent
                {
                    StationId = stationId,
                    ProcessId = processId,
                });

                // Process next in queue
                station.ActiveProcessId = null;

                if (station.QueuedProcessIds.Count > 0)
                {
                    var nextProcessId = station.QueuedProcessIds.Dequeue();
                    if (_processes.TryGetValue(nextProcessId, out var nextProcess))
                    {
                        // Attempt atomic cost withdrawal for next process
                        var cost = nextProcess.Craftable.GetCraftingCost();
                        if (cost != null && cost.WithdrawCost(stationId))
                        {
                            nextProcess.State = ProcessState.Processing;
                            nextProcess.Cost = cost;
                            nextProcess.AllocationId = cost.AllocationId;
                            station.ActiveProcessId = nextProcessId;

                            int remainingQueued = station.QueuedProcessIds.Count;
                            _logger.Info($"Auto-starting next item on station {stationId}: {nextProcess.Craftable} ({remainingQueued} more queued)");

                            _eventBus.Publish(new CraftingStartedEvent
                            {
                                StationId = stationId,
                                ProcessId = nextProcessId,
                                Craftable = nextProcess.Craftable,
                            });
                        }
                        else
                        {
                            // Cost withdrawal failed - rollback already handled by cost.WithdrawCost()
                            _logger.Warning($"Failed to auto-start next item on station {stationId}: insufficient resources or cost withdrawal failed");

                            // Clean up this process and remaining queued
                            _processes.Remove(nextProcessId);
                            while (station.QueuedProcessIds.Count > 0)
                            {
                                _processes.Remove(station.QueuedProcessIds.Dequeue());
                            }
                            station.State = CraftingStationState.Idle;

                            // Publish event that crafting has stopped
                            _eventBus.Publish(new CraftingStoppedEvent
                            {
                                StationId = stationId,
                            });
                        }
                    }
                }
                else
                {
                    // No more processes, station goes idle
                    _logger.Info($"Station {stationId} queue empty, returning to idle");
                    station.State = CraftingStationState.Idle;

                    // Publish event that crafting has stopped
                    _eventBus.Publish(new CraftingStoppedEvent
                    {
                        StationId = stationId,
                    });
                }
            }
        }

        /// <summary>
        /// Cleans up all queued processes including the specified first process ID.
        /// Removes process state data without triggering payback or events.
        /// Used during failed crafting startup to maintain consistency.
        /// </summary>
        private void CleanupQueuedProcesses(StationState station, Guid firstProcessId)
        {
            _processes.Remove(firstProcessId);
            foreach (var id in station.QueuedProcessIds)
            {
                _processes.Remove(id);
            }
            station.QueuedProcessIds.Clear();
        }

        /// <summary>
        /// Stops a process and triggers cost payback.
        /// </summary>
        private void StopProcess(CraftingStationId stationId, ProcessStateData process)
        {
            if (process.Cost != null)
            {
                process.Cost.PaybackCost(stationId, process.AllocationId);
            }
            process.State = ProcessState.Finished;
        }

        /// <summary>
        /// Creates a new process from a craftable.
        /// </summary>
        private ProcessStateData CreateProcess(ICraftable craftable)
        {
            var process = new ProcessStateData
            {
                Id = Guid.NewGuid(),
                Craftable = craftable,
                DurationTotal = craftable.GetCraftDuration(),
                State = ProcessState.Waiting,
                TimeElapsed = 0f,
                TimeRemaining = craftable.GetCraftDuration(),
            };

            _processes[process.Id] = process;
            return process;
        }

        /// <summary>
        /// Internal state for a crafting station.
        /// </summary>
        private sealed class StationState
        {
            public CraftingStationId Id { get; set; }
            public CraftingStationState State { get; set; }
            public ICraftingStationController? Controller { get; set; }
            public Guid? ActiveProcessId { get; set; }
            public Queue<Guid> QueuedProcessIds { get; } = new();
        }

        /// <summary>
        /// Internal state for a crafting process.
        /// </summary>
        private sealed class ProcessStateData
        {
            public Guid Id { get; set; }
            public ICraftable Craftable { get; set; }
            public IResourceCost? Cost { get; set; }
            public Guid AllocationId { get; set; } // Tracks resource allocation for deallocation
            public ProcessState State { get; set; }
            public float DurationTotal { get; set; }
            public float TimeElapsed { get; set; }
            public float TimeRemaining { get; set; }
        }
    }
}
