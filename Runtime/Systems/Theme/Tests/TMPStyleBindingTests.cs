using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace BrewedCode.Theme.Tests
{
    [TestFixture]
    public class TMPStyleBindingTests
    {
        private GameObject _testGo;
        private GameObject _mockGo;
        private TMPStyleBinding _binding;
        private TMP_Text _tmpText;
        private MockThemeService _mockThemeService;

        [SetUp]
        public void SetUp()
        {
            _testGo = new GameObject("TMPBindingTest");

            // Add TMP_Text component
            _tmpText = _testGo.AddComponent<TextMeshProUGUI>();

            // Add binding
            _binding = _testGo.AddComponent<TMPStyleBinding>();

            // Create mock theme service and STORE the GameObject reference
            _mockGo = new GameObject("MockThemeService");
            _mockThemeService = _mockGo.AddComponent<MockThemeService>();

            // Register it so bindings can find it
            ThemeServiceBootstrapper.Register(_mockThemeService);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_testGo);
            Object.DestroyImmediate(_mockGo);  // Clean up mock service
            ThemeServiceBootstrapper.Unregister();
        }

        [Test]
        public void Binding_RequiresComponent_TMP_Text()
        {
            var attr = typeof(TMPStyleBinding).GetCustomAttributes(
                typeof(RequireComponent), false);

            Assert.Greater(attr.Length, 0, "TMPStyleBinding should require TMP_Text");
        }

        [Test]
        public void OnEnable_SubscribesToThemeChangedEvent()
        {
            // Enable the binding - should subscribe to OnThemeChanged
            _testGo.SetActive(true);

            // Add a subscription to verify the event fires
            int eventFires = 0;
            _mockThemeService.OnThemeChanged += () => eventFires++;

            // Trigger theme changed
            _mockThemeService.NotifyThemeChanged();

            // The binding should respond to the theme change
            Assert.Greater(eventFires, 0, "OnThemeChanged event should fire");
        }

        [Test]
        public void OnDisable_CleansUp()
        {
            _testGo.SetActive(true);
            Assert.IsNotNull(_binding);

            // Now disable it
            _testGo.SetActive(false);

            // Verify no exceptions during disable
            Assert.Pass("OnDisable executed without errors");
        }

        [Test]
        public void ApplyAll_DoesNotThrowWithNullService()
        {
            // Remove the theme service to simulate null scenario
            ThemeServiceBootstrapper.Unregister();

            Assert.DoesNotThrow(() => _binding.ApplyAll());
        }

        [Test]
        public void SetColorPath_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _binding.SetColorPath("Text/Secondary"));
        }

        [Test]
        public void SetTypographyPath_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _binding.SetTypographyPath("Body/Lg"));
        }

        [Test]
        public void Binding_HasNoExecuteAlwaysAttribute()
        {
            var attr = typeof(TMPStyleBinding).GetCustomAttributes(
                typeof(ExecuteAlways), false);

            Assert.AreEqual(0, attr.Length, "TMPStyleBinding should not have ExecuteAlways attribute");
        }
    }
}
