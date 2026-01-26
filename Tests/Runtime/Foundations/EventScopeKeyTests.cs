using NUnit.Framework;

namespace BrewedCode.Events.Tests
{
    [TestFixture]
    public class EventScopeKeyTests
    {
        [Test]
        public void Global_HasGlobalType()
        {
            var global = EventScopeKey.Global;

            Assert.AreEqual(EventScopeType.Global, global.Type);
            Assert.IsTrue(global.IsGlobal);
            Assert.AreEqual(0, global.Id);
        }

        [Test]
        public void ForScene_CreatesSceneType()
        {
            int sceneHandle = 12345;
            var sceneKey = EventScopeKey.ForScene(sceneHandle);

            Assert.AreEqual(EventScopeType.Scene, sceneKey.Type);
            Assert.AreEqual(sceneHandle, sceneKey.Id);
            Assert.IsFalse(sceneKey.IsGlobal);
        }

        [Test]
        public void ForInstance_CreatesInstanceType()
        {
            int instanceId = 67890;
            var instanceKey = EventScopeKey.ForInstance(instanceId);

            Assert.AreEqual(EventScopeType.Instance, instanceKey.Type);
            Assert.AreEqual(instanceId, instanceKey.Id);
            Assert.IsFalse(instanceKey.IsGlobal);
        }

        [Test]
        public void Equals_SameGlobal_ReturnsTrue()
        {
            var a = EventScopeKey.Global;
            var b = EventScopeKey.Global;

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
        }

        [Test]
        public void Equals_SameSceneHandle_ReturnsTrue()
        {
            var a = EventScopeKey.ForScene(100);
            var b = EventScopeKey.ForScene(100);

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
        }

        [Test]
        public void Equals_DifferentSceneHandle_ReturnsFalse()
        {
            var a = EventScopeKey.ForScene(100);
            var b = EventScopeKey.ForScene(200);

            Assert.IsFalse(a.Equals(b));
            Assert.IsTrue(a != b);
        }

        [Test]
        public void Equals_DifferentTypes_ReturnsFalse()
        {
            var scene = EventScopeKey.ForScene(100);
            var instance = EventScopeKey.ForInstance(100);

            Assert.IsFalse(scene.Equals(instance));
            Assert.IsTrue(scene != instance);
        }

        [Test]
        public void Equals_GlobalVsScene_ReturnsFalse()
        {
            var global = EventScopeKey.Global;
            var scene = EventScopeKey.ForScene(0);

            Assert.IsFalse(global.Equals(scene));
        }

        [Test]
        public void GetHashCode_SameKeys_ReturnSameHash()
        {
            var a = EventScopeKey.ForScene(100);
            var b = EventScopeKey.ForScene(100);

            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void GetHashCode_DifferentKeys_ReturnDifferentHash()
        {
            var a = EventScopeKey.ForScene(100);
            var b = EventScopeKey.ForScene(200);

            Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void ToString_Global_ReturnsGlobal()
        {
            Assert.AreEqual("Global", EventScopeKey.Global.ToString());
        }

        [Test]
        public void ToString_Scene_ReturnsTypeAndId()
        {
            var scene = EventScopeKey.ForScene(123);
            Assert.AreEqual("Scene:123", scene.ToString());
        }

        [Test]
        public void ToString_Instance_ReturnsTypeAndId()
        {
            var instance = EventScopeKey.ForInstance(456);
            Assert.AreEqual("Instance:456", instance.ToString());
        }
    }
}
