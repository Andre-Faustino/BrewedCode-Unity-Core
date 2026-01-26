using NUnit.Framework;
using System;
using System.Collections.Generic;
using BrewedCode.Events;
using BrewedCode.Logging;

namespace BrewedCode.Crafting.Tests
{
    [TestFixture]
    public class CraftingServiceTests
    {
        private CraftingService _service;
        private MockEventBus _eventBus;
        private MockLoggingService _loggingService;
        private MockCraftable _mockCraftable;
        private CraftingStationId _testStationId;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new MockEventBus();
            _loggingService = new MockLoggingService();
            _service = new CraftingService(_eventBus, _loggingService);
            _mockCraftable = new MockCraftable();
            _testStationId = CraftingStationId.New();

            // Register a test station
            _service.RegisterStation(_testStationId, null);
        }

        [Test]
        public void TryStartCrafting_WithValidCost_SucceedsAndPublishesEvent()
        {
            // Arrange
            var cost = new MockResourceCost(shouldWithdraw: true);
            _mockCraftable.SetCost(cost);

            // Act
            bool result = _service.TryStartCrafting(_testStationId, _mockCraftable, 1, out string error);

            // Assert
            Assert.IsTrue(result, $"TryStartCrafting should succeed but got error: {error}");
            Assert.AreEqual("", error);
            Assert.AreEqual(1, _eventBus.PublishedEvents.Count);
            Assert.IsInstanceOf<CraftingStartedEvent>(_eventBus.PublishedEvents[0]);

            var startEvent = (CraftingStartedEvent)_eventBus.PublishedEvents[0];
            Assert.AreEqual(_testStationId, startEvent.StationId);
        }

        [Test]
        public void TryStartCrafting_WithFailedCostWithdrawal_FailsAndDoesNotPublishEvent()
        {
            // Arrange
            var cost = new MockResourceCost(shouldWithdraw: false);
            _mockCraftable.SetCost(cost);

            // Act
            bool result = _service.TryStartCrafting(_testStationId, _mockCraftable, 1, out string error);

            // Assert
            Assert.IsFalse(result, "TryStartCrafting should fail when cost withdrawal fails");
            Assert.AreEqual("Insufficient resources for crafting (items/resources insufficient).", error);
            Assert.AreEqual(0, _eventBus.PublishedEvents.Count, "No events should be published on failure");
        }

        [Test]
        public void TryStartCrafting_WithNullCost_FailsAndDoesNotPublishEvent()
        {
            // Arrange
            _mockCraftable.SetCost(null);

            // Act
            bool result = _service.TryStartCrafting(_testStationId, _mockCraftable, 1, out string error);

            // Assert
            Assert.IsFalse(result, "TryStartCrafting should fail when cost is null");
            Assert.AreEqual("Craftable has no cost defined.", error);
            Assert.AreEqual(0, _eventBus.PublishedEvents.Count);
        }

        [Test]
        public void TryStartCrafting_WithQueuedItems_ProcessesSequentially()
        {
            // Arrange
            var cost1 = new MockResourceCost(shouldWithdraw: true);
            var cost2 = new MockResourceCost(shouldWithdraw: true);
            _mockCraftable.SetCost(cost1);

            // Act - Queue 2 items
            bool result1 = _service.TryStartCrafting(_testStationId, _mockCraftable, 2, out string error1);

            // Assert - First item should start immediately
            Assert.IsTrue(result1, "First crafting batch should start");
            Assert.AreEqual(1, _eventBus.PublishedEvents.Count);

            // Now complete the first process to trigger next in queue
            var stationInfo = _service.GetStationInfo(_testStationId);
            Assert.IsNotNull(stationInfo, "Station should exist");
            Assert.AreEqual(CraftingStationState.Crafting, stationInfo.State, "Station should be crafting");

            // Simulate completion via Tick
            _mockCraftable.SetCost(cost2);
            _service.Tick(100f); // Complete the crafting

            // After completion, second item in queue should start
            // Expected events: CraftingStartedEvent (initial) + CraftingProgressEvent + CraftingCompletedEvent + CraftingStartedEvent (next) = 4
            Assert.AreEqual(4, _eventBus.PublishedEvents.Count, "Should have: initial start, progress update, completion, and next start events");
            Assert.IsInstanceOf<CraftingStartedEvent>(_eventBus.PublishedEvents[0], "Event[0] should be initial CraftingStartedEvent");
            Assert.IsInstanceOf<CraftingProgressEvent>(_eventBus.PublishedEvents[1], "Event[1] should be CraftingProgressEvent");
            Assert.IsInstanceOf<CraftingCompletedEvent>(_eventBus.PublishedEvents[2], "Event[2] should be CraftingCompletedEvent");
            Assert.IsInstanceOf<CraftingStartedEvent>(_eventBus.PublishedEvents[3], "Event[3] should be next CraftingStartedEvent");
        }

        [Test]
        public void StationState_TransitionsCorrectly()
        {
            // Arrange
            var cost = new MockResourceCost(shouldWithdraw: true);
            _mockCraftable.SetCost(cost);
            var stationInfo = _service.GetStationInfo(_testStationId);

            // Act & Assert - Initial state
            Assert.AreEqual(CraftingStationState.Idle, stationInfo.State);

            // Start crafting
            _service.TryStartCrafting(_testStationId, _mockCraftable, 1, out _);
            stationInfo = _service.GetStationInfo(_testStationId);
            Assert.AreEqual(CraftingStationState.Crafting, stationInfo.State);

            // Complete crafting via time
            _service.Tick(100f);
            stationInfo = _service.GetStationInfo(_testStationId);
            Assert.AreEqual(CraftingStationState.Idle, stationInfo.State);
        }

        [Test]
        public void TryStopCrafting_RefundsCost()
        {
            // Arrange
            var cost = new MockResourceCost(shouldWithdraw: true);
            _mockCraftable.SetCost(cost);
            _service.TryStartCrafting(_testStationId, _mockCraftable, 1, out _);

            // Act
            bool result = _service.TryStopCrafting(_testStationId, out string error);

            // Assert
            Assert.IsTrue(result, "StopCrafting should succeed");
            Assert.AreEqual(1, cost.PaybackCount, "Cost should be paid back once");
            Assert.IsTrue(cost.LastPaybackStationId != default, "Payback should receive valid station ID");
        }

        [Test]
        public void CostWithdrawal_PassesCorrectStationId()
        {
            // Arrange
            var cost = new MockResourceCost(shouldWithdraw: true);
            _mockCraftable.SetCost(cost);

            // Act
            _service.TryStartCrafting(_testStationId, _mockCraftable, 1, out _);

            // Assert
            Assert.AreEqual(1, cost.WithdrawCount, "Cost withdrawal should be called once");
            Assert.AreEqual(_testStationId, cost.LastWithdrawStationId, "WithdrawCost should receive correct station ID");
        }

        // Mock implementations
        private class MockEventBus : IEventBus
        {
            public List<object> PublishedEvents { get; } = new();

            public void Publish<T>(T evt)
            {
                PublishedEvents.Add(evt);
            }

            public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
            {
                // Not needed for tests
                return null;
            }
        }

        private class MockCraftable : ICraftable
        {
            private IResourceCost _cost;

            public void SetCost(IResourceCost cost)
            {
                _cost = cost;
            }

            public bool ValidateRequirements(int amount = 1)
            {
                return true; // Always valid for testing
            }

            public float GetCraftDuration()
            {
                return 1f;
            }

            public IResourceCost GetCraftingCost()
            {
                return _cost;
            }
        }

        private class MockResourceCost : IResourceCost
        {
            private readonly bool _shouldWithdraw;
            private Guid _allocationId = Guid.Empty;

            public Guid AllocationId => _allocationId;
            public int WithdrawCount { get; private set; }
            public int PaybackCount { get; private set; }
            public CraftingStationId LastWithdrawStationId { get; private set; }
            public CraftingStationId LastPaybackStationId { get; private set; }

            public MockResourceCost(bool shouldWithdraw)
            {
                _shouldWithdraw = shouldWithdraw;
            }

            public bool WithdrawCost(CraftingStationId stationId)
            {
                WithdrawCount++;
                LastWithdrawStationId = stationId;
                if (_shouldWithdraw)
                {
                    _allocationId = Guid.NewGuid();
                }
                return _shouldWithdraw;
            }

            public bool PaybackCost(CraftingStationId stationId, Guid allocationId)
            {
                PaybackCount++;
                LastPaybackStationId = stationId;
                return true;
            }
        }

        // Mock logging implementations
        private class MockLog : ILog
        {
            public void Trace(string message) { }
            public void Info(string message) { }
            public void Warning(string message) { }
            public void Error(string message) { }
            public void Fatal(string message) { }
            public void Trace(string message, IReadOnlyDictionary<string, object> metadata) { }
            public void Info(string message, IReadOnlyDictionary<string, object> metadata) { }
            public void Warning(string message, IReadOnlyDictionary<string, object> metadata) { }
            public void Error(string message, IReadOnlyDictionary<string, object> metadata) { }
            public void Fatal(string message, IReadOnlyDictionary<string, object> metadata) { }
            public void Error(string message, Exception exception) { }
            public void Fatal(string message, Exception exception) { }
            public bool IsEnabled(LogLevel level) => true;
        }

        private class MockLoggingService : ILoggingService
        {
            private readonly MockLog _log = new();

            public ILog GetLogger<T>() => _log;
            public ILog GetLogger(string sourceName) => _log;
            public void EnableChannel(LogChannel channel) { }
            public void DisableChannel(LogChannel channel) { }
            public bool IsChannelEnabled(LogChannel channel) => true;
            public LogChannelDefinition? GetChannelDefinition(LogChannel channel) => null;
            public IReadOnlyList<LogChannelDefinition> GetAllChannels() => new List<LogChannelDefinition>();
            public void SetGlobalMinLevel(LogLevel level) { }
            public LogLevel GetGlobalMinLevel() => LogLevel.Trace;
            public void AddSink(ILogSink sink) { }
            public void RemoveSink(ILogSink sink) { }
        }
    }
}
