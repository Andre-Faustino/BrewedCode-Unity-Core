using NUnit.Framework;
using UnityEngine;

namespace BrewedCode.Theme.Tests
{
    [TestFixture]
    public class ThemeServiceBootstrapperTests
    {
        [TearDown]
        public void TearDown()
        {
            ThemeServiceBootstrapper.Unregister();
        }

        [Test]
        public void Instance_ReturnsNull_WhenNotRegistered()
        {
            ThemeServiceBootstrapper.Unregister();
            Assert.IsNull(ThemeServiceBootstrapper.Instance);
        }

        [Test]
        public void Instance_ReturnsRegisteredService()
        {
            var mockGo = new GameObject("MockService");
            var mockService = mockGo.AddComponent<MockThemeService>();

            ThemeServiceBootstrapper.Register(mockService);

            var instance = ThemeServiceBootstrapper.Instance;
            Assert.AreSame(mockService, instance);

            Object.DestroyImmediate(mockGo);
        }

        [Test]
        public void Instance_ReturnsInterface()
        {
            var mockGo = new GameObject("MockService");
            var mockService = mockGo.AddComponent<MockThemeService>();

            ThemeServiceBootstrapper.Register(mockService);

            var instance = ThemeServiceBootstrapper.Instance;
            Assert.IsNotNull(instance);

            Object.DestroyImmediate(mockGo);
        }

        [Test]
        public void Unregister_ClearsInstance()
        {
            var mockGo = new GameObject("MockService");
            var mockService = mockGo.AddComponent<MockThemeService>();

            ThemeServiceBootstrapper.Register(mockService);
            Assert.IsNotNull(ThemeServiceBootstrapper.Instance);

            ThemeServiceBootstrapper.Unregister();
            Assert.IsNull(ThemeServiceBootstrapper.Instance);

            Object.DestroyImmediate(mockGo);
        }

        [Test]
        public void Instance_FindsThemeServiceInScene_AsFallback()
        {
            var serviceGo = new GameObject("ThemeService");
            var service = serviceGo.AddComponent<ThemeService>();

            // Manually enable to trigger registration
            serviceGo.SetActive(true);

            var instance = ThemeServiceBootstrapper.Instance;
            Assert.IsNotNull(instance);
            Assert.AreSame(service, instance);

            Object.DestroyImmediate(serviceGo);
        }

        [Test]
        public void Register_OverwritesPreviousRegistration()
        {
            var mockGo1 = new GameObject("MockService1");
            var mockService1 = mockGo1.AddComponent<MockThemeService>();

            var mockGo2 = new GameObject("MockService2");
            var mockService2 = mockGo2.AddComponent<MockThemeService>();

            ThemeServiceBootstrapper.Register(mockService1);
            Assert.AreSame(mockService1, ThemeServiceBootstrapper.Instance);

            ThemeServiceBootstrapper.Register(mockService2);
            Assert.AreSame(mockService2, ThemeServiceBootstrapper.Instance);

            Object.DestroyImmediate(mockGo1);
            Object.DestroyImmediate(mockGo2);
        }
    }
}
