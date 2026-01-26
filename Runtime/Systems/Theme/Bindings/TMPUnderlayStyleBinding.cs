using UnityEngine;
using TMPro;
using static TMPro.ShaderUtilities;

namespace BrewedCode.Theme
{
    [RequireComponent(typeof(TMP_Text))]
    public class TMPUnderlayStyleBinding : MonoBehaviour
    {
        [Header("Tokens")]
        [SerializeField] private string underlayColorPath = "Text/Shadow";

        [Header("Underlay Settings")]
        [SerializeField, Range(-1f, 1f)] private float offsetX = 0.41f;
        [SerializeField, Range(-1f, 1f)] private float offsetY = -0.97f;
        [SerializeField, Range(-1f, 1f)] private float dilate = -0.23f;
        [SerializeField, Range(0f, 1f)] private float softness = 0.3f;

        [Header("Options")]
        [SerializeField] private bool applyColor = true;
        [SerializeField] private bool liveUpdate = true;

        private TMP_Text _tmp;
        private IThemeResolver? _scope;
        private IThemeService? _themeService;

        // guard para evitar múltiplas instâncias
        private bool _materialInstanced;

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

            ApplyUnderlay();

            if (liveUpdate && _themeService != null)
                _themeService.OnThemeChanged += ApplyUnderlay;
        }

        void OnDisable()
        {
            if (liveUpdate && _themeService != null)
                _themeService.OnThemeChanged -= ApplyUnderlay;
        }

        public void ApplyUnderlay()
        {
            if (_tmp == null || _themeService == null)
                return;

            var shared = _tmp.fontSharedMaterial;
            if (shared == null)
                return;

            EnsureMaterialInstance(shared);

            var mat = _tmp.fontMaterial;
            if (mat == null)
                return;

            mat.EnableKeyword(Keyword_Underlay);

            if (applyColor && !string.IsNullOrEmpty(underlayColorPath))
            {
                var c = _themeService.ResolveColor(underlayColorPath, _scope);
                mat.SetColor(ID_UnderlayColor, c);
            }

            mat.SetFloat(ID_UnderlayOffsetX, offsetX);
            mat.SetFloat(ID_UnderlayOffsetY, offsetY);
            mat.SetFloat(ID_UnderlayDilate, dilate);
            mat.SetFloat(ID_UnderlaySoftness, softness);

            // força atualização visual
            _tmp.UpdateMeshPadding();
            _tmp.SetVerticesDirty();
        }

        private void EnsureMaterialInstance(Material shared)
        {
            var current = _tmp.fontMaterial;

            // se ainda estiver usando o shared, cria instância
            if (!_materialInstanced || current == null || current == shared)
            {
                var instanced = new Material(shared)
                {
                    name = $"{shared.name} (Instance - {gameObject.name})"
                };

                _tmp.fontMaterial = instanced;
                _materialInstanced = true;
            }
        }

        public void SetUnderlayColorPath(string path)
        {
            underlayColorPath = path;
            ApplyUnderlay();
        }
    }
}
