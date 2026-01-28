#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrewedCode.Theme.Editor
{
    public class RawPaletteWindow : EditorWindow
    {
        [MenuItem("BrewedCode/Theme/Raw Palette Editor")]
        public static void Open()
        {
            var window = GetWindow<RawPaletteWindow>("Raw Palette");
            window.minSize = new Vector2(600, 300);
        }

        [SerializeField] private RawPalette? _palette;
        private Vector2 _scroll;

        // cache do ThemeService na cena (editor)
        private ThemeService svc;

        // view model só pra UI
        private class SwatchView
        {
            public int index;     // índice no array do RawPalette
            public string family; // "Cyan"
            public int level;     // 300
            public string name;   // "Cyan300"
            public Color color;
        }

        private Dictionary<string, List<SwatchView>> _cachedGroupedSwatches;
        private bool _needsRebuildGrouped = true;

        private int _editingIndex = -1;
        private string _editingControlName;
        private string _editingOriginalName;
        private Color _editingOriginalColor;
        private string _editingCurrentName;
        private Color _editingCurrentColor;

        private void OnGUI()
        {
            EditorGUILayout.Space();

            DrawPaletteSelector();

            if (!_palette)
            {
                EditorGUILayout.HelpBox(
                    "Selecione um RawPalette para visualizar e editar os swatches.",
                    MessageType.Info
                );
                return;
            }

            EditorGUILayout.Space();

            DrawToolbar();

            EditorGUILayout.Space();

            DrawSwatchesGrid();
        }

        private void DrawPaletteSelector()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Raw Palette", EditorStyles.boldLabel, GUILayout.Width(90));
                _palette = (RawPalette)EditorGUILayout.ObjectField(_palette, typeof(RawPalette), false);

                if (_palette != null)
                {
                    if (GUILayout.Button("Select Asset", GUILayout.Width(100)))
                    {
                        Selection.activeObject = _palette;
                        EditorGUIUtility.PingObject(_palette);
                    }
                }
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add New Swatch", GUILayout.Width(140)))
                {
                    AddNewSwatch();
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawSwatchesGrid()
        {
            if (_palette == null)
                return;

            bool editingActive = _editingIndex >= 0;

            if ((_cachedGroupedSwatches == null || _needsRebuildGrouped) && !editingActive)
            {
                _cachedGroupedSwatches = BuildGroupedSwatches(_palette);
                _needsRebuildGrouped = false;
            }

            var grouped = _cachedGroupedSwatches ?? BuildGroupedSwatches(_palette);

            if (grouped.Count == 0)
            {
                EditorGUILayout.HelpBox("Nenhum swatch encontrado no RawPalette.", MessageType.Warning);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.BeginHorizontal();

            foreach (var kvp in grouped)
            {
                string family = kvp.Key;
                List<SwatchView> swatches = kvp.Value;

                EditorGUILayout.BeginVertical("box", GUILayout.MinWidth(160));
                GUILayout.Label(family, EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                foreach (var view in swatches)
                {
                    DrawSwatchRow(view);
                    EditorGUILayout.Space(4);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            if (_editingIndex >= 0)
            {
                string focused = GUI.GetNameOfFocusedControl();
                if (focused != _editingControlName)
                {
                    FinalizeEdit();
                }
            }
        }

        /// <summary>
        /// Adiciona um novo swatch no palette.
        /// </summary>
        private void AddNewSwatch()
        {
            if (_palette == null) return;

            Undo.RecordObject(_palette, "Add Swatch");

            var list = new List<RawPalette.Swatch>();
            if (_palette.swatches != null && _palette.swatches.Length > 0)
                list.AddRange(_palette.swatches);

            string baseName = "NewSwatch";
            string finalName = baseName;
            int counter = 1;

            // evita nomes duplicados
            while (Array.Exists(_palette.swatches ?? Array.Empty<RawPalette.Swatch>(),
                       s => string.Equals(s.name, finalName, StringComparison.OrdinalIgnoreCase)))
            {
                finalName = $"{baseName}_{counter}";
                counter++;
            }

            var newSwatch = new RawPalette.Swatch
            {
                name = finalName,
                color = Color.white
            };

            list.Add(newSwatch);
            _palette.swatches = list.ToArray();

            EditorUtility.SetDirty(_palette);
            GUI.FocusControl(null);
            Repaint();

            _needsRebuildGrouped = true;

            NotifyThemeChangedFromEditor();
        }

        /// <summary>
        /// Agrupa os swatches por família (prefixo de letras) e ordena famílias alfabeticamente.
        /// A ordem interna da família segue o índice original (estável pra edição).
        /// </summary>
        private Dictionary<string, List<SwatchView>> BuildGroupedSwatches(RawPalette? palette)
        {
            var result = new Dictionary<string, List<SwatchView>>(StringComparer.OrdinalIgnoreCase);

            var swatches = palette?.swatches ?? Array.Empty<RawPalette.Swatch>();

            for (int i = 0; i < swatches.Length; i++)
            {
                var s = swatches[i];

                ParseSwatchName(s.name, out string family, out int level);

                if (string.IsNullOrEmpty(family))
                    family = "<Unnamed>";

                if (!result.TryGetValue(family, out var list))
                {
                    list = new List<SwatchView>();
                    result.Add(family, list);
                }

                list.Add(new SwatchView
                {
                    index = i,
                    family = family,
                    level = level,
                    name = s.name,
                    color = s.color
                });
            }

            // ordenar só as famílias (colunas)
            var sortedFamilies = new List<string>(result.Keys);
            sortedFamilies.Sort(StringComparer.OrdinalIgnoreCase);

            var sortedDict = new Dictionary<string, List<SwatchView>>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in sortedFamilies)
            {
                // NÃO reordenar a lista aqui -> mantém ordem de índice
                sortedDict[family] = result[family];
            }

            return sortedDict;
        }

        /// <summary>
        /// UI de um único swatch (nome + color picker).
        /// </summary>
        private void DrawSwatchRow(SwatchView view)
        {
            if (_palette == null) return;

            var swatches = _palette.swatches;
            if (swatches == null) return;
            if (view.index < 0 || view.index >= swatches.Length) return;

            var swatch = swatches[view.index];

            Rect rowRect = EditorGUILayout.BeginVertical();

            EditorGUI.BeginChangeCheck();

            string controlName = $"SwatchName_{view.index}";
            GUI.SetNextControlName(controlName);

            string baseName = (_editingIndex == view.index && _editingCurrentName != null)
                ? _editingCurrentName
                : swatch.name;

            string newName = EditorGUILayout.TextField(baseName);

            EditorGUILayout.BeginHorizontal();

            Color baseColor = (_editingIndex == view.index)
                ? (_editingCurrentColor != default ? _editingCurrentColor : swatch.color)
                : swatch.color;

            Color newColor = EditorGUILayout.ColorField(baseColor);
            bool delete = GUILayout.Button("X", GUILayout.Width(22));

            EditorGUILayout.EndHorizontal();

            bool changed = EditorGUI.EndChangeCheck();

            string focused = GUI.GetNameOfFocusedControl();
            if (focused == controlName)
            {
                if (_editingIndex != view.index)
                {
                    _editingIndex = view.index;
                    _editingControlName = controlName;
                    _editingOriginalName = swatch.name;
                    _editingOriginalColor = swatch.color;
                    _editingCurrentName = swatch.name;
                    _editingCurrentColor = swatch.color;
                }
            }

            if (changed && _editingIndex == view.index)
            {
                _editingCurrentName = newName;
                _editingCurrentColor = newColor;
            }

            if (delete)
            {
                if (_editingIndex == view.index)
                {
                    ClearEditingState();
                }

                DeleteSwatch(view.index);
                NotifyThemeChangedFromEditor();
            }

            if (_editingIndex == view.index &&
                Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) &&
                focused == controlName)
            {
                Event.current.Use();
                FinalizeEdit();
            }

            EditorGUILayout.EndVertical();

            if (_editingIndex == view.index && Event.current.type == EventType.Repaint)
            {
                string proposedName = _editingCurrentName ?? swatch.name;
                bool isDuplicate = IsDuplicateName(swatches, view.index, proposedName);

                Color borderColor;
                if (isDuplicate)
                {
                    borderColor = Color.red;
                }
                else
                {
                    ParseSwatchName(proposedName, out string family, out int level);
                    bool onlyFamily = string.Equals(family, proposedName, StringComparison.Ordinal);

                    if (onlyFamily)
                        borderColor = Color.yellow;
                    else
                        borderColor = Color.green;
                }

                DrawBorder(rowRect, borderColor);
            }
        }

        /// <summary>
        /// Quebra o nome em família + nível. Ex: "Cyan300" -> ("Cyan", 300).
        /// Se não houver dígitos no final, nível = 0.
        /// </summary>
        private static void ParseSwatchName(string fullName, out string family, out int level)
        {
            family = fullName;
            level = 0;

            if (string.IsNullOrEmpty(fullName))
                return;

            int pos = fullName.Length - 1;
            while (pos >= 0 && char.IsDigit(fullName[pos]))
            {
                pos--;
            }

            // nenhum dígito no final
            if (pos == fullName.Length - 1)
            {
                family = fullName;
                level = 0;
                return;
            }

            family = fullName.Substring(0, pos + 1);
            var numStr = fullName.Substring(pos + 1);

            if (!int.TryParse(numStr, out level))
                level = 0;
        }

        private void DeleteSwatch(int index)
        {
            if (_palette == null) return;
            if (_palette.swatches == null) return;
            if (index < 0 || index >= _palette.swatches.Length) return;

            Undo.RecordObject(_palette, "Delete Swatch");

            var list = new List<RawPalette.Swatch>(_palette.swatches);
            list.RemoveAt(index);
            _palette.swatches = list.ToArray();

            EditorUtility.SetDirty(_palette);
            GUI.FocusControl(null);
            Repaint();

            _needsRebuildGrouped = true;
        }

        private void FinalizeEdit()
        {
            if (_palette == null || _palette.swatches == null)
            {
                ClearEditingState();
                return;
            }

            var swatches = _palette.swatches;
            if (_editingIndex < 0 || _editingIndex >= swatches.Length)
            {
                ClearEditingState();
                return;
            }

            var swatch = swatches[_editingIndex];

            string proposedName = _editingCurrentName ?? _editingOriginalName;
            Color proposedColor = _editingCurrentColor;

            bool isDuplicate = IsDuplicateName(swatches, _editingIndex, proposedName);

            if (isDuplicate)
            {
                Undo.RecordObject(_palette, "Revert Swatch Edit");

                swatch.name = _editingOriginalName;
                swatch.color = _editingOriginalColor;
            }
            else
            {
                Undo.RecordObject(_palette, "Edit Swatch");

                swatch.name = proposedName;
                swatch.color = proposedColor;
            }

            swatches[_editingIndex] = swatch;
            _palette.swatches = swatches;

            EditorUtility.SetDirty(_palette);
            _needsRebuildGrouped = true;

            NotifyThemeChangedFromEditor();
            ClearEditingState();
        }

        private void ClearEditingState()
        {
            _editingIndex = -1;
            _editingControlName = null;
            _editingOriginalName = null;
            _editingOriginalColor = default;
            _editingCurrentName = null;
            _editingCurrentColor = default;
        }

        private static bool IsDuplicateName(RawPalette.Swatch[] swatches, int index, string name)
        {
            if (swatches == null || string.IsNullOrEmpty(name))
                return false;

            for (int i = 0; i < swatches.Length; i++)
            {
                if (i == index) continue;
                if (string.Equals(swatches[i].name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void DrawBorder(Rect rect, Color color)
        {
            const float thickness = 2f;

            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }

        private void NotifyThemeChangedFromEditor()
        {
#if UNITY_2022_2_OR_NEWER
            svc ??= FindFirstObjectByType<ThemeService>(FindObjectsInactive.Include);
#else
            svc = UnityEngine.Object.FindObjectOfType<ThemeService>();
#endif
            if (svc == null) return;

            svc.NotifyThemeChanged();
            SceneView.RepaintAll();
        }
    }
}
#endif
