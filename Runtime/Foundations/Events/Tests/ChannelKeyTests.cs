using System;
using NUnit.Framework;

namespace BrewedCode.Events.Tests
{
    [TestFixture]
    public class ChannelKeyTests
    {
        private struct TestEvent { public int Value; }
        private struct OtherEvent { public string Name; }

        [Test]
        public void Constructor_StoresTypeAndScopeKey()
        {
            var scopeKey = EventScopeKey.ForScene(100);
            var channelKey = new ChannelKey(typeof(TestEvent), scopeKey);

            Assert.AreEqual(typeof(TestEvent), channelKey.EventType);
            Assert.AreEqual(scopeKey, channelKey.ScopeKey);
        }

        [Test]
        public void Equals_SameTypeAndScope_ReturnsTrue()
        {
            var scope = EventScopeKey.ForInstance(50);
            var a = new ChannelKey(typeof(TestEvent), scope);
            var b = new ChannelKey(typeof(TestEvent), scope);

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
        }

        [Test]
        public void Equals_DifferentType_ReturnsFalse()
        {
            var scope = EventScopeKey.Global;
            var a = new ChannelKey(typeof(TestEvent), scope);
            var b = new ChannelKey(typeof(OtherEvent), scope);

            Assert.IsFalse(a.Equals(b));
            Assert.IsTrue(a != b);
        }

        [Test]
        public void Equals_DifferentScope_ReturnsFalse()
        {
            var a = new ChannelKey(typeof(TestEvent), EventScopeKey.Global);
            var b = new ChannelKey(typeof(TestEvent), EventScopeKey.ForScene(1));

            Assert.IsFalse(a.Equals(b));
            Assert.IsTrue(a != b);
        }

        [Test]
        public void Equals_Object_SameKey_ReturnsTrue()
        {
            var scope = EventScopeKey.Global;
            var a = new ChannelKey(typeof(TestEvent), scope);
            object b = new ChannelKey(typeof(TestEvent), scope);

            Assert.IsTrue(a.Equals(b));
        }

        [Test]
        public void Equals_Object_DifferentType_ReturnsFalse()
        {
            var a = new ChannelKey(typeof(TestEvent), EventScopeKey.Global);

            Assert.IsFalse(a.Equals("not a channel key"));
            Assert.IsFalse(a.Equals(null));
        }

        [Test]
        public void GetHashCode_SameKeys_ReturnSameHash()
        {
            var scope = EventScopeKey.ForScene(100);
            var a = new ChannelKey(typeof(TestEvent), scope);
            var b = new ChannelKey(typeof(TestEvent), scope);

            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void GetHashCode_DifferentKeys_ReturnDifferentHash()
        {
            var a = new ChannelKey(typeof(TestEvent), EventScopeKey.Global);
            var b = new ChannelKey(typeof(OtherEvent), EventScopeKey.Global);

            Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void ToString_FormatsCorrectly()
        {
            var key = new ChannelKey(typeof(TestEvent), EventScopeKey.Global);

            Assert.AreEqual("TestEvent@Global", key.ToString());
        }

        [Test]
        public void ToString_WithSceneScope_FormatsCorrectly()
        {
            var key = new ChannelKey(typeof(TestEvent), EventScopeKey.ForScene(123));

            Assert.AreEqual("TestEvent@Scene:123", key.ToString());
        }

        [Test]
        public void CanBeUsedAsDictionaryKey()
        {
            var dict = new System.Collections.Generic.Dictionary<ChannelKey, string>();
            var key1 = new ChannelKey(typeof(TestEvent), EventScopeKey.Global);
            var key2 = new ChannelKey(typeof(TestEvent), EventScopeKey.ForScene(1));

            dict[key1] = "global";
            dict[key2] = "scene";

            Assert.AreEqual("global", dict[key1]);
            Assert.AreEqual("scene", dict[key2]);
            Assert.AreEqual(2, dict.Count);
        }
    }
}
