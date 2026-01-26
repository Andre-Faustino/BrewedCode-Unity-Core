using UnityEngine;
using TMPro;

namespace BrewedCode.Theme
{
    [RequireComponent(typeof(TMP_Text))]
    public class TMPStyleBinding : MonoBehaviour
    {
        [Header("Tokens")]
        [SerializeField] private string colorPath = "Text/Primary";
        [SerializeField] private string typographyPath = "Body/Sm";

        [Header("Options")]
        [SerializeField] private bool applyColor = true;
        [SerializeField] private bool applyTypography = true;
        [SerializeField] private bool liveUpdate = true;

        private TMP_Text _tmp;
        private IThemeResolver? _scope;
        private IThemeService? _themeService;

        void Awake()
        {
            _tmp = GetComponent<TMP_Text>();
            _scope = GetComponentInParent<IThemeResolver>(includeInactive: true);
        }

        void OnEnable()
        {
            // Try to get injected service, fallback to bootstrapper
            _themeService = GetComponentInParent<IThemeService>();
            if (_themeService == null)
            {
                _themeService = ThemeServiceBootstrapper.Instance;
            }

            ApplyAll();

            if (liveUpdate && _themeService != null)
                _themeService.OnThemeChanged += ApplyAll;
        }

        void OnDisable()
        {
            if (liveUpdate && _themeService != null)
                _themeService.OnThemeChanged -= ApplyAll;
        }

        public void ApplyAll()
        {
            if (!_tmp || _themeService is null ) return;
            float fontScale = _themeService.FontScale;

            // COLOR
            if (applyColor && !string.IsNullOrEmpty(colorPath))
            {
                var c = _themeService.ResolveColor(colorPath, _scope);
                _tmp.color = c;
            }

            // TYPOGRAPHY
            if (applyTypography && !string.IsNullOrEmpty(typographyPath))
            {
                if (_themeService.TryResolveTypography(typographyPath, out var ty, _scope))
                {
                    if (ty.font != null && _tmp.font != ty.font)
                        _tmp.font = ty.font;

                    var scale = Mathf.Max(0.1f, fontScale);
                    _tmp.fontSize = Mathf.RoundToInt(ty.size * scale);

                    _tmp.lineSpacing       = ty.lineSpacing;
                    _tmp.characterSpacing  = ty.characterSpacing;
                    _tmp.wordSpacing       = ty.wordSpacing;
                    _tmp.paragraphSpacing  = ty.paragraphSpacing;

                    _tmp.fontStyle = ty.fontStyle;
                    _tmp.enableAutoSizing = false;

                    if (ty.allCaps && !string.IsNullOrEmpty(_tmp.text))
                        _tmp.text = _tmp.text.ToUpperInvariant();

                    _tmp.SetAllDirty();
                }
            }
        }

        // API pública pra trocar tokens por script
        public void SetColorPath(string path)      { colorPath = path; ApplyAll(); }
        public void SetTypographyPath(string path) { typographyPath = path; ApplyAll(); }
    }
}
