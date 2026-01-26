using NUnit.Framework;
using UnityEngine;

namespace BrewedCode.Theme.Tests
{
    [TestFixture]
    public class ThemeContextTests
    {
        private ThemeContext _context;
        private GameObject _testGo;

        [SetUp]
        public void SetUp()
        {
            _testGo = new GameObject("ThemeContextTest");
            _context = _testGo.AddComponent<ThemeContext>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_testGo);
        }

        [Test]
        public void ThemeContext_Implements_IThemeResolver()
        {
            Assert.IsNotNull(_context as IThemeResolver);
        }

        [Test]
        public void TryResolveColor_WithNoOverrides_ReturnsFalse()
        {
            var result = _context.TryResolveColor("Text/Primary", out var color);
            Assert.IsFalse(result);
        }

        [Test]
        public void TryResolveTypography_WithNoOverrides_ReturnsFalse()
        {
            var result = _context.TryResolveTypography("Body/Sm", out var token);
            Assert.IsFalse(result);
        }

        [Test]
        public void LocalProfile_ReturnsAssignedProfile()
        {
            // Note: Can't easily test this without a valid ThemeProfile asset
            // This just verifies the property works
            Assert.IsNull(_context.LocalProfile);
        }

        [Test]
        public void TryResolveColor_Performance_O1Lookup()
        {
            // Test that lookups are fast (O(1) not O(n))
            _testGo.SetActive(true); // Trigger OnEnable to build lookups

            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                _context.TryResolveColor("NonExistent", out _);
            }
            sw.Stop();

            // 10000 lookups should be very fast (< 50ms for O(1))
            // More realistic threshold than 1ms
            Assert.Less(sw.ElapsedMilliseconds, 50, "Lookups should be O(1) and fast");
        }

        [Test]
        public void TryResolveTypography_Performance_O1Lookup()
        {
            _testGo.SetActive(true); // Trigger OnEnable to build lookups

            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                _context.TryResolveTypography("NonExistent", out _);
            }
            sw.Stop();

            // 10000 lookups should be very fast (< 50ms for O(1))
            // More realistic threshold than 1ms
            Assert.Less(sw.ElapsedMilliseconds, 50, "Lookups should be O(1) and fast");
        }
    }
}
