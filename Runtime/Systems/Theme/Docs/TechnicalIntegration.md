# Theme System: Technical Integration Guide

This guide is for developers integrating the Theme System into gameplay logic, UI systems, and custom components.

## Quick Start: 5-Minute Integration

### 1. Ensure ThemeService Exists in Your Scene

```csharp
// In your initialization script
if (ThemeService.Instance == null)
{
    var serviceObj = new GameObject("ThemeService");
    var service = serviceObj.AddComponent<ThemeService>();
    // Assign initial theme in inspector or via code
}
```

The service is a `PersistentMonoSingleton`, so it persists across scenes.

### 2. Apply Theme to a Text Element

```csharp
using UnityEngine;
using TMPro;
using BrewedCode.Theme;

public class MyUI : MonoBehaviour
{
    void Start()
    {
        var textElement = GetComponentInChildren<TextMeshProUGUI>();

        // Option A: Use binding component (recommended)
        var binding = textElement.gameObject.AddComponent<TMPStyleBinding>();
        binding.SetColorPath("Text/Primary");
        binding.SetTypographyPath("Body/Small");

        // Option B: Manual resolution
        var service = ThemeService.Instance;
        textElement.color = service.ResolveColor("Text/Primary");
    }
}
```

### 3. Listen for Theme Changes

```csharp
void OnEnable()
{
    ThemeService.Instance.OnThemeChanged += UpdateMyUI;
}

void OnDisable()
{
    ThemeService.Instance.OnThemeChanged -= UpdateMyUI;
}

void UpdateMyUI()
{
    // Re-resolve colors and update your components
    var color = ThemeService.Instance.ResolveColor("Text/Primary");
    myText.color = color;
}
```

## Core APIs

### ThemeService Resolution

```csharp
// Get singleton instance
var service = ThemeService.Instance;

// Resolve color by path
Color color = service.ResolveColor("Text/Primary");

// Resolve with local context override
var context = GetComponentInParent<IThemeResolver>();
Color contextColor = service.ResolveColor("Text/Primary", context);

// Resolve typography token
if (service.TryResolveTypography("Body/Small", out var typoToken))
{
    myText.font = typoToken.font;
    myText.fontSize = Mathf.RoundToInt(typoToken.size * service.fontScale);
}

// Listen to theme changes
service.OnThemeChanged += () => Debug.Log("Theme changed!");

// Switch active theme
var newTheme = Resources.Load<ThemeProfile>("Themes/DarkTheme");
service.SetTheme(newTheme);

// Access current theme
ThemeProfile current = service.Current;

// Adjust global font scale
service.fontScale = 1.2f;  // 20% larger
```

### IThemeResolver Interface

When you need to provide theme scope (local overrides):

```csharp
public interface IThemeResolver
{
    // Return alternative profile for this scope, or null to use global
    ThemeProfile LocalProfile { get; }

    // Override color for specific path (takes precedence over profile)
    bool TryResolveColor(string path, out Color color);

    // Override typography for specific path
    bool TryResolveTypography(string path, out UiTokens.TypographyToken ty);
}
```

### ThemeContext (Built-in IThemeResolver)

Use when you need local scope with overrides:

```csharp
// Add to container GameObject
var context = myContainer.AddComponent<ThemeContext>();

// Assign a different theme for this scope (optional)
context.localProfile = alternativeTheme;

// Add color overrides (optional)
context.colorOverrides = new[]
{
    new ThemeContext.ColorOverride
    {
        path = "Button/Primary/Background",
        color = new Color(0.2f, 0.8f, 0.2f, 1f)  // Custom green
    }
};

// Add typography overrides (optional)
context.typographyOverrides = new[]
{
    new ThemeContext.TypographyOverride
    {
        path = "Body/Small",
        token = customToken
    }
};
```

**Resolution Order (highest to lowest priority):**
1. Context's color/typography overrides
2. Context's local profile theme
3. Global theme (ThemeService.Current)
4. White color / default (fallback)

## Binding Components

### TMPStyleBinding: Automatic Text Styling

Apply to any TextMeshPro component:

```csharp
var binding = textObject.AddComponent<TMPStyleBinding>();

// Configure (in code or inspector)
binding.SetColorPath("Text/Primary");
binding.SetTypographyPath("Body/Small");

// The binding automatically:
// ✓ Applies initial styling
// ✓ Listens to theme changes
// ✓ Respects IThemeResolver overrides
// ✓ Scales fonts by fontScale
// ✓ Updates on EnableDisable
// ✓ Previews in editor
```

**What it applies:**
- Color from token
- Font asset from typography token
- Font size (scaled by `fontScale`)
- Line spacing, character spacing, word spacing, paragraph spacing
- Font style (bold, italic, etc.)
- All caps formatting (if enabled)

**Configuration:**
```csharp
public string colorPath = "Text/Primary";           // Color token path
public string typographyPath = "Body/Small";        // Typography token path
public bool applyColor = true;                      // Apply color from token
public bool applyTypography = true;                 // Apply typography from token
public bool liveUpdate = true;                      // Listen to theme changes
```

### UIButtonBinding: State-Based Button Styling

Apply to UI Button components:

```csharp
var binding = button.AddComponent<UIButtonBinding>();

// The binding tracks button states and applies colors:
// Normal → hover → Pressed → back to hover → Normal
```

**State Tracking:**
- **Normal:** Default appearance
- **Hover:** Mouse enters button
- **Pressed:** Mouse button down
- **Back to Hover:** Mouse button up (without leaving button)

**How it works:**
```
Button/Primary/Normal   → (mouse enter)   → Normal color
     ↓
Button/Primary/Hover    → (mouse down)    → Hover color
     ↓
Button/Primary/Pressed  → (mouse up)      → Pressed color
     ↓
Button/Primary/Hover    → (mouse exit)    → Back to Hover color
     ↓
Button/Primary/Normal                     → Normal color
```

**Configuration:**
```csharp
public string basePath = "Button/Primary";  // Component base path
public Graphic target;                      // Image or Text to color
```

States are appended: `{basePath}/{state}` = `"Button/Primary/Normal"`

### TMPUnderlayStyleBinding: Text Shadow/Underlay Styling

Apply underlay styling to TextMeshPro:

```csharp
var binding = textObject.AddComponent<TMPUnderlayStyleBinding>();
```

Configures TextMeshPro's built-in underlay/shadow effect.

## Creating Custom Bindings

Extend theme application to custom components:

```csharp
using UnityEngine;
using BrewedCode.Theme;

public class CustomGraphicBinding : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private string colorPath = "Text/Primary";
    [SerializeField] private bool liveUpdate = true;

    private ThemeService _service;
    private IThemeResolver _scope;

    void Awake()
    {
        _scope = GetComponentInParent<IThemeResolver>(includeInactive: true);
    }

    void OnEnable()
    {
        _service = ThemeService.Instance;
        ApplyTheme();

        if (liveUpdate)
            _service.OnThemeChanged += ApplyTheme;
    }

    void OnDisable()
    {
        if (liveUpdate && _service != null)
            _service.OnThemeChanged -= ApplyTheme;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            _service ??= FindThemeService();
            ApplyTheme();
        }
    }
#endif

    void ApplyTheme()
    {
        if (!targetImage || !_service) return;

        targetImage.color = _service.ResolveColor(colorPath, _scope);
    }

    private ThemeService FindThemeService()
    {
        if (Application.isPlaying)
            return ThemeService.Instance;

#if UNITY_EDITOR
#if UNITY_2022_2_OR_NEWER
        return FindFirstObjectByType<ThemeService>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<ThemeService>(true);
#endif
#else
        return null;
#endif
    }

    public void SetColorPath(string path)
    {
        colorPath = path;
        ApplyTheme();
    }
}
```

**Key patterns:**
1. **Awake:** Get IThemeResolver parent scope
2. **OnEnable:** Get service, apply theme, subscribe to changes
3. **OnDisable:** Unsubscribe from changes
4. **OnValidate:** Editor preview support
5. **Theme method:** Resolve and apply
6. **Path setter:** Allow code-based path changes

## Styling Patterns

### Pattern 1: Direct Color Application

For simple components needing one color:

```csharp
void ApplyTheme()
{
    var color = ThemeService.Instance.ResolveColor("Text/Primary");
    myGraphic.color = color;
}
```

### Pattern 2: Multi-Color Components

For components with multiple colored parts:

```csharp
void ApplyTheme()
{
    var service = ThemeService.Instance;

    // Background
    background.color = service.ResolveColor("Card/Background");

    // Border
    border.color = service.ResolveColor("Border/Default");

    // Title text
    title.color = service.ResolveColor("Text/Primary");

    // Body text
    body.color = service.ResolveColor("Text/Secondary");
}
```

### Pattern 3: State-Dependent Styling

For components with multiple states:

```csharp
void UpdateForState(ComponentState state)
{
    var service = ThemeService.Instance;

    var colorPath = state switch
    {
        ComponentState.Active => "Status/Active",
        ComponentState.Warning => "Status/Warning",
        ComponentState.Error => "Status/Error",
        _ => "Status/Neutral"
    };

    myGraphic.color = service.ResolveColor(colorPath);
}
```

### Pattern 4: Theme-Aware Layout

Adjust layout based on theme metrics (if stored in tokens):

```csharp
void ApplyTheme()
{
    var service = ThemeService.Instance;

    // Use typography token to determine spacing
    if (service.TryResolveTypography("Body/Small", out var typo))
    {
        // Size-dependent spacing: smaller fonts → tighter spacing
        var spacing = typo.size * 0.5f;
        layoutGroup.spacing = spacing;
    }
}
```

## Resolution Performance

### Caching

The service caches resolved colors and typography for performance:

```csharp
// First call: reads from palette, caches result
Color c1 = service.ResolveColor("Text/Primary");  // O(n)

// Second call: returns cached value
Color c2 = service.ResolveColor("Text/Primary");  // O(1)
```

**Cache invalidation:**
- `SetTheme()` clears caches (theme changed)
- `NotifyThemeChanged()` manually clears caches
- Only use if modifying theme at runtime

### Avoiding Excessive Lookups

**Bad:** Looking up in Update()
```csharp
void Update()
{
    myText.color = service.ResolveColor("Text/Primary");  // Every frame!
}
```

**Good:** Cache and update on changes
```csharp
void OnEnable()
{
    service.OnThemeChanged += ApplyTheme;
    ApplyTheme();
}

void ApplyTheme()
{
    cachedColor = service.ResolveColor("Text/Primary");
}

void Update()
{
    myText.color = cachedColor;  // Just use cached value
}
```

## Handling Missing Tokens

### Graceful Fallbacks

```csharp
// ResolveColor returns white if token not found
var color = service.ResolveColor("Text/Nonexistent");  // Returns white

// TryResolveTypography returns false if not found
if (!service.TryResolveTypography("Nonexistent/Token", out var typo))
{
    // Token not found, use defaults
    myText.font = defaultFont;
    myText.fontSize = 16;
}
```

### Validation in Editor

```csharp
#if UNITY_EDITOR
[ContextMenu("Validate Token Paths")]
void ValidateTokens()
{
    var profile = ThemeService.Instance.Current;
    var result = ThemeValidator.Validate(profile);
    result.PrintToConsole();

    // Log any issues
    if (!result.IsValid)
        Debug.LogError("Theme has validation errors!");
}
#endif
```

## Accessibility Integration

### Font Scaling

Apply global font scale from theme:

```csharp
void ApplyTheme()
{
    var service = ThemeService.Instance;
    var fontScale = service.fontScale;

    if (service.TryResolveTypography("Body/Small", out var typo))
    {
        // Apply scaling
        myText.fontSize = Mathf.RoundToInt(typo.size * fontScale);
    }
}
```

Font scale is:
- **0.75:** 25% smaller (for dense UI)
- **1.0:** Standard size
- **1.5:** 50% larger (for accessibility)

### Theme Accessibility Flags

```csharp
var profile = service.Current;

if (profile.highContrast)
{
    // This theme prioritizes contrast
    // Optional: boost contrast further with additional logic
}

if (profile.colorVisionSafe)
{
    // Colors are chosen to be distinguishable for color blindness
    // Optional: adjust additional visual indicators
}

// Font scale is user-configurable
service.fontScale = 1.25f;
```

## Scene Setup Examples

### Minimal Setup

```csharp
public class GameInitializer : MonoBehaviour
{
    [SerializeField] private ThemeProfile initialTheme;

    void Start()
    {
        // Theme service may already exist (persistent singleton)
        // Just set the initial theme if needed
        if (initialTheme != null)
            ThemeService.Instance.SetTheme(initialTheme);
    }
}
```

### With Multiple Themes

```csharp
public class ThemeManager : MonoBehaviour
{
    [SerializeField] private ThemeProfile[] themes;
    [SerializeField] private int currentThemeIndex = 0;

    void Start()
    {
        ApplyTheme(currentThemeIndex);
    }

    public void CycleTheme()
    {
        currentThemeIndex = (currentThemeIndex + 1) % themes.Length;
        ApplyTheme(currentThemeIndex);
    }

    void ApplyTheme(int index)
    {
        if (index >= 0 && index < themes.Length)
            ThemeService.Instance.SetTheme(themes[index]);
    }

    void OnGUI()
    {
        if (GUILayout.Button("Cycle Theme"))
            CycleTheme();
    }
}
```

### With Persistent Settings

```csharp
public class ThemeSettingsManager : MonoBehaviour
{
    private const string ThemeKey = "player_theme";
    private const string FontScaleKey = "player_font_scale";

    void Start()
    {
        // Load saved preferences
        LoadSettings();
    }

    void LoadSettings()
    {
        var service = ThemeService.Instance;

        // Load theme
        var themeName = PlayerPrefs.GetString(ThemeKey, "Light");
        var theme = Resources.Load<ThemeProfile>($"Themes/{themeName}");
        if (theme != null)
            service.SetTheme(theme);

        // Load font scale
        var scale = PlayerPrefs.GetFloat(FontScaleKey, 1.0f);
        service.fontScale = Mathf.Clamp(scale, 0.75f, 1.75f);
    }

    public void SetTheme(string themeName)
    {
        var theme = Resources.Load<ThemeProfile>($"Themes/{themeName}");
        if (theme != null)
        {
            ThemeService.Instance.SetTheme(theme);
            PlayerPrefs.SetString(ThemeKey, themeName);
        }
    }

    public void SetFontScale(float scale)
    {
        var service = ThemeService.Instance;
        service.fontScale = Mathf.Clamp(scale, 0.75f, 1.75f);
        PlayerPrefs.SetFloat(FontScaleKey, service.fontScale);
    }
}
```

## Testing and Validation

### Validating Themes Programmatically

```csharp
#if UNITY_EDITOR
[MenuItem("BrewedCode/Theme/Validate All Themes")]
static void ValidateAllThemes()
{
    var guids = UnityEditor.AssetDatabase.FindAssets("t:ThemeProfile");

    foreach (var guid in guids)
    {
        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<ThemeProfile>(path);

        var result = ThemeValidator.Validate(profile);

        if (!result.IsValid)
            Debug.LogError($"Theme {path} has errors:\n{result}", profile);
        else
            Debug.Log($"Theme {path} is valid", profile);
    }
}
#endif
```

### Unit Testing Theme Resolution

```csharp
#if UNITY_EDITOR
[TestFixture]
public class ThemeResolutionTests
{
    private ThemeService _service;
    private ThemeProfile _testTheme;

    [SetUp]
    public void Setup()
    {
        // Create test theme
        _testTheme = ScriptableObject.CreateInstance<ThemeProfile>();
        _service = new GameObject().AddComponent<ThemeService>();
        _service.SetTheme(_testTheme);
    }

    [Test]
    public void ResolveColor_WithValidPath_ReturnsColor()
    {
        var color = _service.ResolveColor("Text/Primary");
        Assert.AreNotEqual(Color.white, color);
    }

    [Test]
    public void ResolveColor_WithInvalidPath_ReturnsWhite()
    {
        var color = _service.ResolveColor("Nonexistent/Path");
        Assert.AreEqual(Color.white, color);
    }
}
#endif
```

## Common Integration Patterns

### Pattern: HUD Updates on Theme Change

```csharp
public class HUD : MonoBehaviour
{
    private List<TMPStyleBinding> _bindings = new();

    void Start()
    {
        // Find all bindings in HUD
        _bindings.AddRange(GetComponentsInChildren<TMPStyleBinding>());

        // Listen to theme changes
        ThemeService.Instance.OnThemeChanged += RefreshHUD;
    }

    void RefreshHUD()
    {
        // Bindings update automatically, but you can do additional work
        foreach (var binding in _bindings)
            binding.ApplyAll();
    }

    void OnDestroy()
    {
        ThemeService.Instance.OnThemeChanged -= RefreshHUD;
    }
}
```

### Pattern: Dialog with Custom Colors

```csharp
public class CustomDialog : MonoBehaviour
{
    public void SetColors(Color background, Color text)
    {
        var context = gameObject.AddComponent<ThemeContext>();

        context.colorOverrides = new[]
        {
            new ThemeContext.ColorOverride
            {
                path = "Dialog/Background",
                color = background
            },
            new ThemeContext.ColorOverride
            {
                path = "Text/Primary",
                color = text
            }
        };
    }
}
```

### Pattern: Transient UI

```csharp
public class PopupMessage : MonoBehaviour
{
    public static void Show(string message, string colorPath = "Text/Primary")
    {
        var popup = Instantiate(popupPrefab);
        var text = popup.GetComponentInChildren<TextMeshProUGUI>();

        var binding = text.gameObject.AddComponent<TMPStyleBinding>();
        binding.SetColorPath(colorPath);

        text.text = message;
    }
}
```

## Debugging

### Logging Token Resolution

```csharp
void DebugTokenResolution(string path)
{
    var service = ThemeService.Instance;
    var profile = service.Current;

    var color = service.ResolveColor(path);
    Debug.Log($"Token '{path}' resolved to {color}");

    if (profile.ui != null)
    {
        var token = System.Array.Find(profile.ui.colors, t => t.path == path);
        if (!string.IsNullOrEmpty(token.path))
            Debug.Log($"  → References swatch: {token.rawRef} with alpha: {token.alpha}");
    }
}
```

### Checking Theme Scope

```csharp
void DebugScope()
{
    var resolver = GetComponentInParent<IThemeResolver>();

    if (resolver == null)
        Debug.Log("No theme scope parent, using global theme");
    else
        Debug.Log($"Using theme scope: {resolver.GetType().Name}");
}
```

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Colors not applying | No ThemeService in scene | Add ThemeService component to a persistent GameObject |
| Text always white | Token not found | Validate theme, check token path spelling |
| Theme doesn't switch | Theme not set or null | Call `SetTheme()` with valid profile |
| No font scaling | Font scale is 1.0 | Set `service.fontScale` to desired value |
| Binding not updating | `liveUpdate = false` | Enable live update toggle in inspector |
| Child UI inherits wrong colors | Wrong scope resolution | Check IThemeResolver parent hierarchy |

