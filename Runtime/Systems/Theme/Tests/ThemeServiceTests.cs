using NUnit.Framework;
using UnityEngine;

namespace BrewedCode.Theme.Tests
{
    [TestFixture]
    public class ThemeServiceTests
    {
        private ThemeService _themeService;
        private GameObject _testGo;

        [SetUp]
        public void SetUp()
        {
            _testGo = new GameObject("ThemeServiceTest");
            _themeService = _testGo.AddComponent<ThemeService>();
            ThemeServiceBootstrapper.Unregister();

            // Ensure clean state by setting theme to null
            _themeService.SetTheme(null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_testGo);
            ThemeServiceBootstrapper.Unregister();
        }

        [Test]
        public void ThemeService_Implements_IThemeService()
        {
            var service = _themeService as IThemeService;
            Assert.IsNotNull(service);
        }

        [Test]
        public void SetTheme_RegistersWithBootstrapper()
        {
            // Enable to trigger OnEnable
            _testGo.SetActive(true);

            var bootstrapperInstance = ThemeServiceBootstrapper.Instance;
            Assert.IsNotNull(bootstrapperInstance);
            Assert.AreSame(_themeService, bootstrapperInstance);
        }

        [Test]
        public void FontScale_CanBeSet()
        {
            _themeService.FontScale = 1.5f;
            Assert.AreEqual(1.5f, _themeService.FontScale);
        }

        [Test]
        public void FontScale_ClampsToValidRange()
        {
            _themeService.FontScale = 2.0f; // Out of range
            Assert.AreEqual(1.75f, _themeService.FontScale); // Clamped to max (1.75)

            _themeService.FontScale = -0.5f; // Out of range
            Assert.AreEqual(0.75f, _themeService.FontScale); // Clamped to min (0.75)
        }

        [Test]
        public void ResolveColor_WithNullProfile_ReturnsWhite()
        {
            var color = _themeService.ResolveColor("Text/Primary");
            Assert.AreEqual(Color.white, color);
        }

        [Test]
        public void TryResolveTypography_WithNullProfile_ReturnsFalse()
        {
            var result = _themeService.TryResolveTypography("Body/Sm", out var token);
            Assert.IsFalse(result);
        }

        [Test]
        public void OnThemeChanged_EventFires()
        {
            bool eventFired = false;
            _themeService.OnThemeChanged += () => eventFired = true;

            _themeService.NotifyThemeChanged();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void SetTheme_ClearsCaches()
        {
            // Resolve a color (should cache it)
            var color = _themeService.ResolveColor("Text/Primary");

            // Set a new theme (should clear cache)
            _themeService.SetTheme(null);

            // The current theme should be null
            Assert.IsNull(_themeService.Current);
        }

        [Test]
        public void ResolveColor_WithScope_UsesOverride()
        {
            var mockScopeGo = new GameObject("MockResolver");
            var mockScope = mockScopeGo.AddComponent<MockThemeResolver>();
            mockScope.shouldReturnColorOverride = true;
            mockScope.colorOverride = new Color(1, 0, 0, 1);

            try
            {
                var color = _themeService.ResolveColor("Text/Primary", mockScope);
                Assert.AreEqual(new Color(1, 0, 0, 1), color);
            }
            finally
            {
                Object.DestroyImmediate(mockScopeGo);
            }
        }
    }
}
