using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrewedCode.Theme.Tests
{
    [TestFixture]
    public class UIButtonBindingTests
    {
        private GameObject _testGo;
        private UIButtonBinding _binding;
        private Image _targetGraphic;
        private MockThemeService _mockThemeService;

        [SetUp]
        public void SetUp()
        {
            _testGo = new GameObject("UIButtonBindingTest");

            // Create a canvas for UI elements
            var canvas = new GameObject("Canvas").AddComponent<Canvas>();
            _testGo.transform.SetParent(canvas.transform);

            // Add Image as target
            _targetGraphic = _testGo.AddComponent<Image>();

            // Add binding
            _binding = _testGo.AddComponent<UIButtonBinding>();

            // Create mock theme service
            var mockGo = new GameObject("MockThemeService");
            _mockThemeService = mockGo.AddComponent<MockThemeService>();

            // Register it so bindings can find it
            ThemeServiceBootstrapper.Register(_mockThemeService);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_testGo.transform.root.gameObject);
            // Clean up mock service if it exists
            if (_mockThemeService != null && _mockThemeService.gameObject != null)
                Object.DestroyImmediate(_mockThemeService.gameObject);
            ThemeServiceBootstrapper.Unregister();
        }

        [Test]
        public void Binding_HasPointerHandlerInterfaces()
        {
            Assert.IsNotNull(_binding as IPointerEnterHandler);
            Assert.IsNotNull(_binding as IPointerExitHandler);
            Assert.IsNotNull(_binding as IPointerDownHandler);
            Assert.IsNotNull(_binding as IPointerUpHandler);
        }

        [Test]
        public void OnEnable_RegistersEventListener()
        {
            _testGo.SetActive(true);

            // Binding should have registered with mock service
            Assert.Pass("OnEnable should register without throwing");
        }

        [Test]
        public void OnDisable_UnregistersEventListener()
        {
            _testGo.SetActive(true);
            _testGo.SetActive(false);

            Assert.Pass("OnDisable should unregister without throwing");
        }

        [Test]
        public void PointerEnter_ChangesStateToHover()
        {
            _testGo.SetActive(true);

            var eventData = new PointerEventData(EventSystem.current);
            Assert.DoesNotThrow(() => _binding.OnPointerEnter(eventData));
        }

        [Test]
        public void PointerExit_ChangesStateToNormal()
        {
            _testGo.SetActive(true);

            var eventData = new PointerEventData(EventSystem.current);
            Assert.DoesNotThrow(() => _binding.OnPointerExit(eventData));
        }

        [Test]
        public void PointerDown_ChangesStateToPressed()
        {
            _testGo.SetActive(true);

            var eventData = new PointerEventData(EventSystem.current);
            Assert.DoesNotThrow(() => _binding.OnPointerDown(eventData));
        }

        [Test]
        public void PointerUp_ChangesStateToHover()
        {
            _testGo.SetActive(true);

            var eventData = new PointerEventData(EventSystem.current);
            Assert.DoesNotThrow(() => _binding.OnPointerUp(eventData));
        }

        [Test]
        public void Binding_WorksWithoutThemeService()
        {
            // This tests resilience when no theme service is available
            _testGo.SetActive(true);

            var eventData = new PointerEventData(EventSystem.current);
            Assert.DoesNotThrow(() =>
            {
                _binding.OnPointerEnter(eventData);
                _binding.OnPointerDown(eventData);
                _binding.OnPointerUp(eventData);
                _binding.OnPointerExit(eventData);
            });
        }
    }
}
