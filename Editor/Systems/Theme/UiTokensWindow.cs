#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;
using BrewedCode.Theme; 

namespace BrewedCode.Theme.Editor
{
    public class UiTokensWindow : EditorWindow
    {
        [MenuItem("BrewedCode/Theme/UiTokens Editor")]
        public static void Open()
        {
            var window = GetWindow<UiTokensWindow>("UiTokens");
            window.minSize = new Vector2(700, 400);
        }

        [SerializeField] private UiTokens _tokens;

        private enum Tab { Colors, Typography }
        private Tab _currentTab = Tab.Colors;

        private Vector2 _scrollColors;
        private Vector2 _scrollTypography;

        // Foldouts das árvores
        private readonly Dictionary<string, bool> _colorFoldouts = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _typoFoldouts = new Dictionary<string, bool>();

        // RawPalette de referência para preview na aba Colors
        [SerializeField] private RawPalette _refPalette;
        private readonly Dictionary<string, Color> _rawPreviewMap = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

        private class TokenNode
        {
            public string name;
            public Dictionary<string, TokenNode> children = new Dictionary<string, TokenNode>(StringComparer.OrdinalIgnoreCase);
            public List<int> tokenIndices = new List<int>();
        }

        private Dictionary<string, TokenNode> _cachedColorRoots;
        private bool _needsRebuildColorTree = true;

        private Dictionary<string, TokenNode> _cachedTypographyRoots;
        private bool _needsRebuildTypographyTree = true;

        private int _editingColorIndex = -1;
        private UiTokens.ColorToken _editingColorOriginal;
        private UiTokens.ColorToken _editingColorCurrent;

        private int _editingTypographyIndex = -1;
        private string _editingTypographyControlName;
        private UiTokens.TypographyToken _editingTypographyOriginal;
        private UiTokens.TypographyToken _editingTypographyCurrent;

        private void OnGUI()
        {
            EditorGUILayout.Space();

            DrawTokensSelector();

            if (_tokens == null)
            {
                EditorGUILayout.HelpBox("Selecione um UiTokens para visualizar/editar os tokens.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            DrawTabs();

            EditorGUILayout.Space();

            switch (_currentTab)
            {
                case Tab.Colors:
                    DrawColorsSection();
                    break;

                case Tab.Typography:
                    DrawTypographySection();
                    break;
            }
        }

        private void DrawTokensSelector()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("UiTokens", EditorStyles.boldLabel, GUILayout.Width(70));
                _tokens = (UiTokens)EditorGUILayout.ObjectField(_tokens, typeof(UiTokens), false);

                if (_tokens != null)
                {
                    if (GUILayout.Button("Select Asset", GUILayout.Width(100)))
                    {
                        Selection.activeObject = _tokens;
                        EditorGUIUtility.PingObject(_tokens);
                    }
                }
            }
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();

            GUIStyle tabStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontStyle = FontStyle.Bold
            };

            if (GUILayout.Toggle(_currentTab == Tab.Colors, "Colors", tabStyle, GUILayout.Height(25)))
                _currentTab = Tab.Colors;

            if (GUILayout.Toggle(_currentTab == Tab.Typography, "Typography", tabStyle, GUILayout.Height(25)))
                _currentTab = Tab.Typography;

            EditorGUILayout.EndHorizontal();
        }

        // ===============================
        // COLORS SECTION
        // ===============================

        private void DrawColorsSection()
        {
            if (_refPalette != null && _rawPreviewMap.Count == 0)
            {
                UpdateRawPreviewMap();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Reference RawPalette", GUILayout.Width(140));
                var newRef = (RawPalette)EditorGUILayout.ObjectField(_refPalette, typeof(RawPalette), false);

                if (newRef != _refPalette)
                {
                    _refPalette = newRef;
                    UpdateRawPreviewMap();
                }

                if (_refPalette != null)
                {
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeObject = _refPalette;
                        EditorGUIUtility.PingObject(_refPalette);
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(60)))
                    {
                        _refPalette = null;
                        _rawPreviewMap.Clear();
                    }
                }

                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Color Token", GUILayout.Width(140)))
                    AddColorToken();

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(4);

            _scrollColors = EditorGUILayout.BeginScrollView(_scrollColors);

            bool editingActive = _editingColorIndex >= 0;

            if ((_cachedColorRoots == null || _needsRebuildColorTree) && !editingActive)
            {
                _cachedColorRoots = BuildTreeFromPaths(_tokens.colors, ct => ct.path);
                _needsRebuildColorTree = false;
            }

            var roots = _cachedColorRoots ?? BuildTreeFromPaths(_tokens.colors, ct => ct.path);

            if (roots.Count == 0)
            {
                EditorGUILayout.HelpBox("Nenhum ColorToken definido.", MessageType.Info);
            }
            else
            {
                EditorGUI.indentLevel++;

                var rootKeys = GetSortedKeys(roots);
                foreach (var key in rootKeys)
                    DrawColorNodeRecursive(key, roots[key], "");

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();

            if (_editingColorIndex >= 0)
            {
                string focused = GUI.GetNameOfFocusedControl();
                string pathName = $"ColorPath_{_editingColorIndex}";
                string rawName = $"ColorRaw_{_editingColorIndex}";

                if (focused != pathName && focused != rawName)
                {
                    FinalizeColorEdit();
                }
            }
        }

        private void UpdateRawPreviewMap()
        {
            _rawPreviewMap.Clear();

            if (_refPalette == null || _refPalette.swatches == null)
                return;

            foreach (var sw in _refPalette.swatches)
            {
                if (sw.name == null)
                    continue;

                var key = sw.name.Trim();

                if (string.IsNullOrEmpty(key))
                    continue;

                if (!_rawPreviewMap.ContainsKey(key))
                    _rawPreviewMap.Add(key, sw.color);
            }
        }

        private void AddColorToken()
        {
            Undo.RecordObject(_tokens, "Add Color Token");

            var list = new List<UiTokens.ColorToken>(_tokens.colors ?? Array.Empty<UiTokens.ColorToken>());

            string basePath = "New/Color";
            string finalPath = basePath;
            int counter = 1;

            while (list.Exists(c => string.Equals(c.path, finalPath, StringComparison.OrdinalIgnoreCase)))
            {
                finalPath = $"{basePath}{counter}";
                counter++;
            }

            list.Add(new UiTokens.ColorToken(finalPath, ""));

            _tokens.colors = list.ToArray();
            EditorUtility.SetDirty(_tokens);
            _needsRebuildColorTree = true;
            Repaint();
        }

        private void DeleteColorToken(int index)
        {
            if (_tokens.colors == null || index < 0 || index >= _tokens.colors.Length)
                return;

            Undo.RecordObject(_tokens, "Delete Color Token");

            var list = new List<UiTokens.ColorToken>(_tokens.colors);
            list.RemoveAt(index);

            _tokens.colors = list.ToArray();
            EditorUtility.SetDirty(_tokens);
            _needsRebuildColorTree = true;
            Repaint();
        }

        private void DrawColorNodeRecursive(string nodeName, TokenNode node, string parent)
        {
            string fullPath = string.IsNullOrEmpty(parent) ? nodeName : $"{parent}/{nodeName}";
            bool expanded = GetFoldoutState(_colorFoldouts, fullPath);

            expanded = EditorGUILayout.Foldout(expanded, nodeName, true);
            _colorFoldouts[fullPath] = expanded;

            if (!expanded) return;

            EditorGUI.indentLevel++;

            foreach (int idx in node.tokenIndices)
                DrawColorTokenRow(idx);

            var childKeys = GetSortedKeys(node.children);
            foreach (var c in childKeys)
                DrawColorNodeRecursive(c, node.children[c], fullPath);

            EditorGUI.indentLevel--;
        }

        private void DrawColorTokenRow(int index)
        {
            if (_tokens.colors == null || index < 0 || index >= _tokens.colors.Length)
                return;

            var colors = _tokens.colors;
            bool isEditingThis = _editingColorIndex == index;

            UiTokens.ColorToken ct = isEditingThis ? _editingColorCurrent : colors[index];

            Rect rowRect = EditorGUILayout.BeginVertical();

            EditorGUI.BeginChangeCheck();

            string pathControlName = $"ColorPath_{index}";
            string rawControlName = $"ColorRaw_{index}";

            GUI.SetNextControlName(pathControlName);
            string newPath = EditorGUILayout.TextField("Path", ct.path);

            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName(rawControlName);
            string newRawRef = EditorGUILayout.TextField("Raw", ct.rawRef);

            DrawRawPreview(newRawRef, ct.alpha);
            bool delete = GUILayout.Button("X", GUILayout.Width(22));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Alpha", GUILayout.Width(40));
            float newAlpha = EditorGUILayout.Slider(ct.alpha, 0f, 1f);
            bool newInherit = EditorGUILayout.ToggleLeft("Inherit Raw Alpha", ct.inheritRawAlpha, GUILayout.Width(140));
            EditorGUILayout.EndHorizontal();

            bool changed = EditorGUI.EndChangeCheck();

            string focused = GUI.GetNameOfFocusedControl();

            if (!isEditingThis && (focused == pathControlName || focused == rawControlName) &&
                (changed || Event.current.type == EventType.MouseDown || Event.current.type == EventType.KeyDown))
            {
                _editingColorIndex = index;
                _editingColorOriginal = colors[index];
                _editingColorCurrent = colors[index];
                isEditingThis = true;
            }

            if (changed && isEditingThis)
            {
                _editingColorCurrent.path = newPath;
                _editingColorCurrent.rawRef = newRawRef;
                _editingColorCurrent.alpha = newAlpha;
                _editingColorCurrent.inheritRawAlpha = newInherit;
            }

            if (delete)
            {
                if (isEditingThis)
                    ClearColorEditingState();

                DeleteColorToken(index);
                EditorGUILayout.EndVertical();
                return;
            }

            if (isEditingThis &&
                Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) &&
                (focused == pathControlName || focused == rawControlName))
            {
                Event.current.Use();
                FinalizeColorEdit();
            }

            EditorGUILayout.EndVertical();

            if (isEditingThis && Event.current.type == EventType.Repaint)
            {
                string proposedPath = _editingColorCurrent.path ?? "";
                string p = proposedPath.Trim();

                bool hasPath = !string.IsNullOrEmpty(p);
                bool isDuplicate = hasPath && IsDuplicateColorPath(_tokens.colors, index, p);

                if (isDuplicate)
                {
                    DrawBorder(rowRect, Color.red);
                }
                else if (!hasPath)
                {
                    DrawBorder(rowRect, Color.yellow);
                }
                else
                {
                    DrawBorder(rowRect, Color.green);
                }
            }
        }

        private void DrawRawPreview(string rawRef, float alpha = 1f)
        {
            string key = rawRef != null ? rawRef.Trim() : string.Empty;

            if (string.IsNullOrEmpty(key) || _rawPreviewMap.Count == 0)
            {
                Rect rEmpty = GUILayoutUtility.GetRect(40, 18);
                EditorGUI.DrawRect(rEmpty, new Color(0, 0, 0, 0.1f));
                return;
            }

            if (_rawPreviewMap.TryGetValue(key, out var color))
            {
                Rect r = GUILayoutUtility.GetRect(40, 18);
                var c = color;
                c.a *= alpha;
                EditorGUI.DrawRect(r, c);
                return;
            }

            Rect rFail = GUILayoutUtility.GetRect(40, 18);
            EditorGUI.DrawRect(rFail, new Color(0.2f, 0.0f, 0.0f, 0.4f));
            EditorGUI.LabelField(
                rFail,
                "?",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    normal = { textColor = Color.white }
                }
            );
        }

        private void FinalizeColorEdit()
        {
            if (_tokens == null || _tokens.colors == null)
            {
                ClearColorEditingState();
                return;
            }

            var colors = _tokens.colors;
            if (_editingColorIndex < 0 || _editingColorIndex >= colors.Length)
            {
                ClearColorEditingState();
                return;
            }

            string proposedPath = _editingColorCurrent.path ?? "";
            string p = proposedPath.Trim();

            bool hasPath = !string.IsNullOrEmpty(p);
            bool isDuplicate = hasPath && IsDuplicateColorPath(colors, _editingColorIndex, p);

            if (isDuplicate)
            {
                Undo.RecordObject(_tokens, "Revert Color Token Edit");
                colors[_editingColorIndex] = _editingColorOriginal;
            }
            else
            {
                Undo.RecordObject(_tokens, "Edit Color Token");
                colors[_editingColorIndex] = _editingColorCurrent;
            }

            _tokens.colors = colors;
            EditorUtility.SetDirty(_tokens);
            _needsRebuildColorTree = true;
            ClearColorEditingState();
            Repaint();
        }

        private void ClearColorEditingState()
        {
            _editingColorIndex = -1;
            _editingColorOriginal = default;
            _editingColorCurrent = default;
        }

        private static bool IsDuplicateColorPath(UiTokens.ColorToken[] colors, int index, string path)
        {
            if (colors == null || string.IsNullOrEmpty(path))
                return false;

            for (int i = 0; i < colors.Length; i++)
            {
                if (i == index) continue;

                var op = colors[i].path?.Trim();
                if (!string.IsNullOrEmpty(op) &&
                    string.Equals(op, path, StringComparison.OrdinalIgnoreCase))
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

        // ===============================
        // TYPOGRAPHY SECTION
        // ===============================

        private void DrawTypographySection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Typography", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Reference RawPalette", GUILayout.Width(140));
                var newRef = (RawPalette)EditorGUILayout.ObjectField(_refPalette, typeof(RawPalette), false);

                if (newRef != _refPalette)
                {
                    _refPalette = newRef;
                    UpdateRawPreviewMap();
                }

                if (_refPalette != null)
                {
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeObject = _refPalette;
                        EditorGUIUtility.PingObject(_refPalette);
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(60)))
                    {
                        _refPalette = null;
                        _rawPreviewMap.Clear();
                    }
                }

                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Typography Token", GUILayout.Width(160)))
                    AddTypographyToken();

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(4);

            _scrollTypography = EditorGUILayout.BeginScrollView(_scrollTypography);

            bool editingActive = _editingTypographyIndex >= 0;

            if ((_cachedTypographyRoots == null || _needsRebuildTypographyTree) && !editingActive)
            {
                _cachedTypographyRoots = BuildTreeFromPaths(_tokens.typography, tt => tt.path);
                _needsRebuildTypographyTree = false;
            }

            var roots = _cachedTypographyRoots ?? BuildTreeFromPaths(_tokens.typography, tt => tt.path);

            if (roots.Count == 0)
            {
                EditorGUILayout.HelpBox("Nenhum TypographyToken definido.", MessageType.Info);
            }
            else
            {
                EditorGUI.indentLevel++;

                var rootKeys = GetSortedKeys(roots);
                foreach (var key in rootKeys)
                    DrawTypographyNodeRecursive(key, roots[key], "");

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();

            if (_editingTypographyIndex >= 0)
            {
                string focused = GUI.GetNameOfFocusedControl();
                if (focused != _editingTypographyControlName)
                {
                    FinalizeTypographyEdit();
                }
            }
        }

        private void AddTypographyToken()
        {
            Undo.RecordObject(_tokens, "Add Typography Token");

            var list = new List<UiTokens.TypographyToken>(_tokens.typography ?? Array.Empty<UiTokens.TypographyToken>());

            string basePath = "New/Type";
            string finalPath = basePath;
            int counter = 1;

            while (list.Exists(t => string.Equals(t.path, finalPath, StringComparison.OrdinalIgnoreCase)))
            {
                finalPath = $"{basePath}{counter}";
                counter++;
            }

            var newToken = new UiTokens.TypographyToken
            {
                path = finalPath,
                font = null,
                size = 16,
                lineSpacing = 0f,
                characterSpacing = 0f,
                wordSpacing = 0f,
                paragraphSpacing = 0f,
                fontStyle = FontStyles.Normal,
                allCaps = false
            };

            list.Add(newToken);

            _tokens.typography = list.ToArray();
            EditorUtility.SetDirty(_tokens);
            _needsRebuildTypographyTree = true;
            Repaint();
        }

        private void DeleteTypographyToken(int index)
        {
            if (_tokens.typography == null || index < 0 || index >= _tokens.typography.Length)
                return;

            Undo.RecordObject(_tokens, "Delete Typography Token");

            var list = new List<UiTokens.TypographyToken>(_tokens.typography);
            list.RemoveAt(index);

            _tokens.typography = list.ToArray();
            EditorUtility.SetDirty(_tokens);
            _needsRebuildTypographyTree = true;
            Repaint();
        }

        private void DrawTypographyNodeRecursive(string nodeName, TokenNode node, string parent)
        {
            string fullPath = string.IsNullOrEmpty(parent) ? nodeName : $"{parent}/{nodeName}";
            bool expanded = GetFoldoutState(_typoFoldouts, fullPath);

            expanded = EditorGUILayout.Foldout(expanded, nodeName, true);
            _typoFoldouts[fullPath] = expanded;

            if (!expanded) return;

            EditorGUI.indentLevel++;

            foreach (int idx in node.tokenIndices)
                DrawTypographyTokenRow(idx);

            var childKeys = GetSortedKeys(node.children);
            foreach (var c in childKeys)
                DrawTypographyNodeRecursive(c, node.children[c], fullPath);

            EditorGUI.indentLevel--;
        }

        private void DrawTypographyTokenRow(int index)
        {
            if (_tokens.typography == null || index < 0 || index >= _tokens.typography.Length)
                return;

            var arr = _tokens.typography;
            bool isEditingThis = _editingTypographyIndex == index;

            UiTokens.TypographyToken tt = isEditingThis ? _editingTypographyCurrent : arr[index];

            Rect rowRect = EditorGUILayout.BeginVertical();

            EditorGUI.BeginChangeCheck();

            string controlName = $"TypoPath_{index}";
            GUI.SetNextControlName(controlName);
            string newPath = EditorGUILayout.TextField("Path", tt.path);

            TMP_FontAsset newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Font", tt.font, typeof(TMP_FontAsset), false);

            EditorGUILayout.BeginHorizontal();
            int newSize = EditorGUILayout.IntField("Size", tt.size);
            bool newAllCaps = EditorGUILayout.ToggleLeft("All Caps", tt.allCaps, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            float newLineSpacing = EditorGUILayout.FloatField("Line Spacing (Δ)", tt.lineSpacing);
            float newCharSpacing = EditorGUILayout.FloatField("Char Spacing", tt.characterSpacing);
            float newWordSpacing = EditorGUILayout.FloatField("Word Spacing", tt.wordSpacing);
            float newParagraphSpacing = EditorGUILayout.FloatField("Paragraph Spacing", tt.paragraphSpacing);

            FontStyles newFontStyle = (FontStyles)EditorGUILayout.EnumFlagsField("Font Style", tt.fontStyle);

            bool delete = GUILayout.Button("Delete Typography", GUILayout.Width(160));

            bool changed = EditorGUI.EndChangeCheck();

            string focused = GUI.GetNameOfFocusedControl();
            if (focused == controlName)
            {
                if (!isEditingThis)
                {
                    _editingTypographyIndex = index;
                    _editingTypographyControlName = controlName;
                    _editingTypographyOriginal = arr[index];
                    _editingTypographyCurrent = arr[index];
                    isEditingThis = true;
                }
            }

            if (changed && isEditingThis)
            {
                _editingTypographyCurrent.path = newPath;
                _editingTypographyCurrent.font = newFont;
                _editingTypographyCurrent.size = newSize;
                _editingTypographyCurrent.allCaps = newAllCaps;
                _editingTypographyCurrent.lineSpacing = newLineSpacing;
                _editingTypographyCurrent.characterSpacing = newCharSpacing;
                _editingTypographyCurrent.wordSpacing = newWordSpacing;
                _editingTypographyCurrent.paragraphSpacing = newParagraphSpacing;
                _editingTypographyCurrent.fontStyle = newFontStyle;
            }

            if (delete)
            {
                if (isEditingThis)
                    ClearTypographyEditingState();

                DeleteTypographyToken(index);
                EditorGUILayout.EndVertical();
                return;
            }

            if (isEditingThis &&
                Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) &&
                focused == controlName)
            {
                Event.current.Use();
                FinalizeTypographyEdit();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();

            if (isEditingThis && Event.current.type == EventType.Repaint)
            {
                string proposedPath = _editingTypographyCurrent.path ?? "";
                string p = proposedPath.Trim();

                bool hasPath = !string.IsNullOrEmpty(p);
                bool isDuplicate = hasPath && IsDuplicateTypographyPath(_tokens.typography, index, p);

                if (isDuplicate)
                {
                    DrawBorder(rowRect, Color.red);
                }
                else if (!hasPath)
                {
                    DrawBorder(rowRect, Color.yellow);
                }
                else
                {
                    DrawBorder(rowRect, Color.green);
                }
            }
        }

        private void FinalizeTypographyEdit()
        {
            if (_tokens == null || _tokens.typography == null)
            {
                ClearTypographyEditingState();
                return;
            }

            var arr = _tokens.typography;
            if (_editingTypographyIndex < 0 || _editingTypographyIndex >= arr.Length)
            {
                ClearTypographyEditingState();
                return;
            }

            string proposedPath = _editingTypographyCurrent.path ?? "";
            string p = proposedPath.Trim();

            bool hasPath = !string.IsNullOrEmpty(p);
            bool isDuplicate = hasPath && IsDuplicateTypographyPath(arr, _editingTypographyIndex, p);

            if (isDuplicate)
            {
                Undo.RecordObject(_tokens, "Revert Typography Token Edit");
                arr[_editingTypographyIndex] = _editingTypographyOriginal;
            }
            else
            {
                Undo.RecordObject(_tokens, "Edit Typography Token");
                arr[_editingTypographyIndex] = _editingTypographyCurrent;
            }

            _tokens.typography = arr;

            EditorUtility.SetDirty(_tokens);
            _needsRebuildTypographyTree = true;
            ClearTypographyEditingState();
            Repaint();
        }

        private void ClearTypographyEditingState()
        {
            _editingTypographyIndex = -1;
            _editingTypographyControlName = null;
            _editingTypographyOriginal = default;
            _editingTypographyCurrent = default;
        }

        private static bool IsDuplicateTypographyPath(UiTokens.TypographyToken[] arr, int index, string path)
        {
            if (arr == null || string.IsNullOrEmpty(path))
                return false;

            for (int i = 0; i < arr.Length; i++)
            {
                if (i == index) continue;

                var op = arr[i].path?.Trim();
                if (!string.IsNullOrEmpty(op) &&
                    string.Equals(op, path, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        // ===============================
        // TREE HELPERS
        // ===============================

        private Dictionary<string, TokenNode> BuildTreeFromPaths<T>(T[] tokens, Func<T, string> getPath)
        {
            var roots = new Dictionary<string, TokenNode>(StringComparer.OrdinalIgnoreCase);
            if (tokens == null) return roots;

            for (int i = 0; i < tokens.Length; i++)
            {
                string path = getPath(tokens[i]) ?? "";
                var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length == 0)
                {
                    const string rootKey = "<Root>";
                    if (!roots.TryGetValue(rootKey, out var rootNode))
                    {
                        rootNode = new TokenNode { name = rootKey };
                        roots.Add(rootKey, rootNode);
                    }
                    rootNode.tokenIndices.Add(i);
                    continue;
                }

                Dictionary<string, TokenNode> level = roots;
                TokenNode currentNode = null;

                foreach (var seg in segments)
                {
                    if (!level.TryGetValue(seg, out var next))
                    {
                        next = new TokenNode { name = seg };
                        level.Add(seg, next);
                    }

                    currentNode = next;
                    level = next.children;
                }

                currentNode.tokenIndices.Add(i);
            }

            return roots;
        }

        private bool GetFoldoutState(Dictionary<string, bool> dict, string key)
        {
            if (!dict.TryGetValue(key, out bool state))
            {
                state = true;
                dict[key] = state;
            }
            return state;
        }

        private static List<string> GetSortedKeys(Dictionary<string, TokenNode> dict)
        {
            var keys = new List<string>(dict.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            return keys;
        }
    }
}
#endif
