using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrewedCode.Theme
{
    public class UIButtonBinding : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler
    {
        [SerializeField] private string basePath = "Button/Primary";
        [SerializeField] private Graphic? target; // Image/Text
        string _state = "Normal";

        private IThemeService? _themeService;

        void OnEnable()
        {
            // Try to get injected service, fallback to bootstrapper
            _themeService = GetComponentInParent<IThemeService>() ?? ThemeServiceBootstrapper.Instance;

            Apply();

            if (_themeService != null)
                _themeService.OnThemeChanged += Apply;
        }

        void OnDisable()
        {
            if (_themeService != null)
                _themeService.OnThemeChanged -= Apply;
        }

        void Apply()
        {
            if (_themeService == null) return;
            if (target) target.color = Resolve($"{basePath}/{_state}");
        }

        Color Resolve(string path)
        {
            if (_themeService == null) return Color.white;

            var ctx = GetComponentInParent<IThemeResolver>();
            if (ctx != null && ctx.TryResolveColor(path, out var c)) return c;
            return _themeService.ResolveColor(path);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            _state = "Hover";
            Apply();
        }

        public void OnPointerExit(PointerEventData e)
        {
            _state = "Normal";
            Apply();
        }

        public void OnPointerDown(PointerEventData e)
        {
            _state = "Pressed";
            Apply();
        }

        public void OnPointerUp(PointerEventData e)
        {
            _state = "Hover";
            Apply();
        }
    }
}