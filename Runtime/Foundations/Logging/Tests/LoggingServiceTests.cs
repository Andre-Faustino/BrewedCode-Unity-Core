using System.Collections.Generic;
using NUnit.Framework;

namespace BrewedCode.Logging.Tests
{
    [TestFixture]
    public class LoggingServiceTests
    {
        private LoggingService _service;
        private MockEventBus _eventBus;
        private TestLogSink _sink;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new MockEventBus();
            _service = new LoggingService(_eventBus);
            _sink = new TestLogSink();
            _service.AddSink(_sink);
        }

        [Test]
        public void GetLogger_CreatesLoggerWithCorrectSource()
        {
            var logger = _service.GetLogger<LoggingServiceTests>();

            logger.Info("Test message");

            Assert.AreEqual(1, _sink.Entries.Count);
            Assert.AreEqual("LoggingServiceTests", _sink.Entries[0].Source);
        }

        [Test]
        public void DisableChannel_PreventsLogging()
        {
            _service.DisableChannel(LogChannel.Default);
            var logger = _service.GetLogger("TestSource");

            logger.Info("Should not appear");

            Assert.AreEqual(0, _sink.Entries.Count);
        }

        [Test]
        public void SetGlobalMinLevel_FiltersLowerLevels()
        {
            _service.SetGlobalMinLevel(LogLevel.Warning);
            var logger = _service.GetLogger("TestSource");

            logger.Info("Filtered");
            logger.Warning("Visible");

            Assert.AreEqual(1, _sink.Entries.Count);
            Assert.AreEqual(LogLevel.Warning, _sink.Entries[0].Level);
        }

        [Test]
        public void ErrorLog_IncludesStackTrace()
        {
            var logger = _service.GetLogger("TestSource");

            logger.Error("Error message");

            Assert.AreEqual(1, _sink.Entries.Count);
            Assert.IsNotNull(_sink.Entries[0].StackTrace);
            Assert.IsNotEmpty(_sink.Entries[0].StackTrace);
        }

        [Test]
        public void LogEmitted_PublishesEvent()
        {
            var logger = _service.GetLogger("TestSource");

            logger.Info("Test");

            Assert.AreEqual(1, _eventBus.PublishedEvents.Count);
            Assert.IsInstanceOf<LogEmittedEvent>(_eventBus.PublishedEvents[0]);
        }

        [Test]
        public void EnableChannel_AllowsLogging()
        {
            _service.DisableChannel(LogChannel.Default);
            _service.EnableChannel(LogChannel.Default);
            var logger = _service.GetLogger("TestSource");

            logger.Info("Should appear");

            Assert.AreEqual(1, _sink.Entries.Count);
        }

        [Test]
        public void DisableSpecificChannel_DisablesOnlyThatChannel()
        {
            // Initially all channels are enabled
            Assert.IsTrue(_service.IsChannelEnabled(LogChannel.Crafting));
            Assert.IsTrue(_service.IsChannelEnabled(LogChannel.Default));
            Assert.IsTrue(_service.IsChannelEnabled(LogChannel.Timer));

            // Disable Crafting channel
            _service.DisableChannel(LogChannel.Crafting);

            // Verify Crafting is disabled but others are still enabled
            Assert.IsFalse(_service.IsChannelEnabled(LogChannel.Crafting));
            Assert.IsTrue(_service.IsChannelEnabled(LogChannel.Default));
            Assert.IsTrue(_service.IsChannelEnabled(LogChannel.Timer));

            // Verify logging still works for enabled channels
            var defaultLogger = _service.GetLogger("DefaultSource");
            defaultLogger.Info("Should appear");

            Assert.AreEqual(1, _sink.Entries.Count);
            Assert.AreEqual("DefaultSource", _sink.Entries[0].Source);
        }

        [Test]
        public void DisableChannel_ThenEnableChannel_ResumesLogging()
        {
            var logger = _service.GetLogger("TestSource");

            // Initially logging works
            logger.Info("First message");
            Assert.AreEqual(1, _sink.Entries.Count);

            // Disable Default channel
            _service.DisableChannel(LogChannel.Default);
            logger.Info("Second message - should not appear");
            Assert.AreEqual(1, _sink.Entries.Count); // Still 1, new message not added

            // Re-enable Default channel
            _service.EnableChannel(LogChannel.Default);
            logger.Info("Third message - should appear");
            Assert.AreEqual(2, _sink.Entries.Count); // Now 2
        }

        [Test]
        public void LoggerFactory_CachesLoggers()
        {
            var logger1 = _service.GetLogger<LoggingServiceTests>();
            var logger2 = _service.GetLogger<LoggingServiceTests>();

            Assert.AreSame(logger1, logger2);
        }

        [Test]
        public void IsLogEnabled_ChecksChannelAndLevel()
        {
            _service.SetGlobalMinLevel(LogLevel.Warning);
            var logger = _service.GetLogger("TestSource");

            Assert.IsFalse(logger.IsEnabled(LogLevel.Info));
            Assert.IsTrue(logger.IsEnabled(LogLevel.Warning));
            Assert.IsTrue(logger.IsEnabled(LogLevel.Error));
        }

        private sealed class TestLogSink : ILogSink
        {
            public List<LogEntry> Entries { get; } = new();

            public void Write(LogEntry entry) => Entries.Add(entry);
        }
    }
}
