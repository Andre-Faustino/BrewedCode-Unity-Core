# Theme System Guide

## Overview

The Theme System is a centralized module for managing UI appearance, colors, typography, and component styling across the application. It provides a token-based approach to theming, allowing consistent visual design through semantic naming rather than hard-coded values.

**Key Purpose:** Decouple design tokens (colors, fonts, sizing) from implementation, enabling theme switching, accessibility customization, and visual consistency.

## Responsibilities and Boundaries

### What the Theme System Does

- **Token Resolution:** Maps semantic paths (e.g., `"Text/Primary"`, `"Body/Large"`) to concrete design values
- **Hierarchical Theme Management:** Supports global themes with local scope overrides
- **Color and Typography Control:** Manages all text styling and color application
- **State-Based Styling:** Tracks component states (Normal/Hover/Pressed/Disabled) for interactive elements
- **Accessibility Support:** Font scaling and contrast/color-blind safe modes
- **Cache Optimization:** Caches resolved values for performance
- **Event Broadcasting:** Notifies subscribers when themes change

### What the Theme System Does NOT Do

- Manage animation or transition effects
- Control layout or positioning
- Handle scene or prefab persistence
- Apply themes to non-UI elements (meshes, materials, etc.)
- Provide runtime theme creation (themes are design assets)

## Architecture

### Core Components

```
Theme System
├── ThemeService (Singleton)
│   └── Central hub for all theme resolution
├── Data Models
│   ├── RawPalette: Base color definitions
│   ├── UiTokens: Semantic color and typography tokens
│   ├── ThemeProfile: Complete theme asset
│   └── ComponentTheme: State-based styling for components
├── Resolution Scope
│   ├── IThemeResolver: Interface for scoped overrides
│   └── ThemeContext: Hierarchical scope implementation
└── Bindings
    ├── TMPStyleBinding: Text styling
    └── UIButtonBinding: Button state styling
```

## Public API

### ThemeService (Singleton)

The main entry point for all theme operations.

```csharp
namespace BrewedCode.Theme
{
    public sealed class ThemeService : PersistentMonoSingleton<ThemeService>
    {
        // Current active theme
        public ThemeProfile Current { get; }

        // Switch active theme
        public void SetTheme(ThemeProfile profile)

        // Resolve a color token with optional scope override
        public Color ResolveColor(string path, IThemeResolver scope = null)

        // Resolve typography token with optional scope override
        public bool TryResolveTypography(string path, out UiTokens.TypographyToken token, IThemeResolver scope = null)

        // Notify all bindings of theme changes
        public void NotifyThemeChanged()

        // Global font scale for accessibility (0.75 - 1.75)
        public float fontScale

        // Event fired when theme changes
        public event Action OnThemeChanged
    }
}
```

### ThemeProfile (Root Asset)

Combines all theme data into a single asset.

```csharp
public class ThemeProfile : ScriptableObject
{
    // Base color palette
    public RawPalette raw;

    // Semantic tokens (colors and typography)
    public UiTokens ui;

    // State-based component styling
    public ComponentTheme[] components;

    // Accessibility settings
    public bool highContrast;
    public bool colorVisionSafe;
    public float defaultFontScale = 1f;  // 0.85 - 1.5 range

    // Validation (Editor only)
    [ContextMenu("Validate Profile")]
    public void ValidateProfile()
}
```

### RawPalette

Stores base colors with semantic names.

```csharp
[CreateAssetMenu(menuName = "Theme/Core/RawPalette")]
public class RawPalette : ScriptableObject
{
    [System.Serializable] public struct Swatch
    {
        public string name;        // e.g., "Cyan500", "Gray900"
        public Color color;        // RGBA value
    }

    public Swatch[] swatches;
}
```

### UiTokens

Defines semantic design tokens.

```csharp
[CreateAssetMenu(menuName = "Theme/UI/UiTokens")]
public class UiTokens : ScriptableObject
{
    [System.Serializable] public struct ColorToken
    {
        public string path;               // e.g., "Text/Primary"
        public string rawRef;             // References swatch name
        public float alpha;               // 0-1 override
        public bool inheritRawAlpha;      // Use swatch's alpha
    }

    [System.Serializable] public struct TypographyToken
    {
        public string path;               // e.g., "Body/Large"
        public TMP_FontAsset font;        // TextMeshPro font
        public int size;                  // Size in pixels
        public float lineSpacing;         // Relative spacing
        public float characterSpacing;    // Tracking
        public float wordSpacing;         // Word spacing
        public float paragraphSpacing;    // Paragraph spacing
        public FontStyles fontStyle;      // Bold/Italic/SmallCaps
        public bool allCaps;              // Force uppercase
    }

    public ColorToken[] colors;
    public TypographyToken[] typography;
}
```

### ComponentTheme

Defines state-based colors for interactive components.

```csharp
[CreateAssetMenu(menuName = "Theme/UI/ComponentTheme")]
public class ComponentTheme : ScriptableObject
{
    [System.Serializable] public struct StateColor
    {
        public string state;              // "Normal", "Hover", "Pressed", "Disabled"
        public string colorTokenPath;     // Path to color token
    }

    public string componentId;            // e.g., "Button/Primary"
    public StateColor[] colors;
}
```

### IThemeResolver (Scoping)

Interface for hierarchical theme overrides.

```csharp
public interface IThemeResolver
{
    // Alternative theme profile for this scope
    ThemeProfile LocalProfile { get; }

    // Override color for specific path
    bool TryResolveColor(string path, out Color color);

    // Override typography for specific path
    bool TryResolveTypography(string path, out UiTokens.TypographyToken ty);
}
```

### ThemeContext (Hierarchical Scoping)

Implements `IThemeResolver` to create local theme scopes.

```csharp
public sealed class ThemeContext : MonoBehaviour, IThemeResolver
{
    [SerializeField] private ThemeProfile localProfile;

    [System.Serializable] public struct ColorOverride
    {
        public string path;
        public Color color;
    }

    [System.Serializable] public struct TypographyOverride
    {
        public string path;
        public UiTokens.TypographyToken token;
    }

    [SerializeField] private ColorOverride[] colorOverrides;
    [SerializeField] private TypographyOverride[] typographyOverrides;

    public ThemeProfile LocalProfile => localProfile;

    public bool TryResolveColor(string path, out Color c)
    public bool TryResolveTypography(string path, out UiTokens.TypographyToken ty)
}
```

## How to Use the Theme System

### Setup

1. **Create a RawPalette asset:**
   - Right-click in Project → Create → Theme/Core/RawPalette
   - Add color swatches with semantic names: `Cyan500`, `Gray900`, `Error400`, etc.

2. **Create a UiTokens asset:**
   - Right-click in Project → Create → Theme/UI/UiTokens
   - Define color tokens: `Text/Primary`, `Button/Background`, etc.
     - Each token references a swatch from RawPalette
   - Define typography tokens: `Body/Small`, `Heading/Large`, etc.
     - Each token specifies font, size, and spacing

3. **Create ComponentTheme assets (optional):**
   - Right-click in Project → Create → Theme/UI/ComponentTheme
   - Define component styling: `Button/Primary`, `Button/Secondary`, etc.
   - For each component, map states to color tokens:
     - `Normal` → `"Button/Primary/Background"`
     - `Hover` → `"Button/Primary/Hover"`
     - etc.

4. **Create a ThemeProfile asset:**
   - Right-click in Project → Create → Theme (no menu, use inspector)
   - Assign RawPalette and UiTokens
   - Assign ComponentTheme array if using state-based styling
   - Set accessibility flags if needed
   - **Validate** using context menu "Validate Profile"

5. **Place ThemeService in your scene:**
   - Create empty GameObject with ThemeService component
   - Assign initial theme in inspector
   - ThemeService persists across scenes (PersistentMonoSingleton)

### Styling a Text Element

**Using TMPStyleBinding (Recommended):**

```csharp
// Add TMPStyleBinding to TextMeshPro component
var textObject = new GameObject("MyText");
var tmpText = textObject.AddComponent<TextMeshProUGUI>();
var binding = textObject.AddComponent<TMPStyleBinding>();

// Configure in Inspector or via code:
binding.SetColorPath("Text/Primary");      // Semantic path
binding.SetTypographyPath("Body/Small");   // Semantic path
```

The binding automatically:
- Applies color and typography on startup
- Listens for theme changes
- Respects local `ThemeContext` overrides
- Scales fonts based on accessibility settings

**Manual Resolution (for non-binding code):**

```csharp
var service = ThemeService.Instance;

// Resolve a color
Color textColor = service.ResolveColor("Text/Primary");

// Resolve typography
if (service.TryResolveTypography("Body/Small", out var typo))
{
    tmpText.font = typo.font;
    tmpText.fontSize = Mathf.RoundToInt(typo.size * service.fontScale);
    tmpText.fontStyle = typo.fontStyle;
}

// Listen for theme changes
service.OnThemeChanged += () => {
    // Re-apply styling
};
```

### Styling a Button with States

```csharp
// Add UIButtonBinding to UI Button
var buttonObject = GetComponent<Button>();
var binding = buttonObject.AddComponent<UIButtonBinding>();

// Configure in Inspector:
// - Base Path: "Button/Primary"
// - Target: Button's Image component

// The binding automatically:
// - Applies Normal state color
// - Switches to Hover when mouse enters
// - Switches to Pressed when clicked
// - Returns to Normal when released
```

### Creating Local Theme Scopes

Use `ThemeContext` to override theme for a subtree:

```csharp
// Create a container with local theme
var container = new GameObject("LocalThemeContainer");
var context = container.AddComponent<ThemeContext>();

// Assign a different profile or leave null to use global
context.localProfile = alternativeTheme;

// Add color overrides (optional)
context.colorOverrides = new[]
{
    new ThemeContext.ColorOverride
    {
        path = "Text/Primary",
        color = new Color(1, 0, 0, 1)  // Red text in this scope
    }
};

// All children resolve tokens through this context first
// If not found in overrides, falls back to localProfile or global theme
```

**Resolution Priority:**
1. Local context color overrides
2. Local context typography overrides
3. Local profile theme (if context has one)
4. Global theme (ThemeService.Current)

### Switching Themes at Runtime

```csharp
var service = ThemeService.Instance;

// Load alternative theme
var darkTheme = Resources.Load<ThemeProfile>("Themes/DarkTheme");

// Switch theme (broadcasts OnThemeChanged)
service.SetTheme(darkTheme);

// All bound components automatically update
```

### Accessibility

```csharp
var service = ThemeService.Instance;

// Global font scale (affects all typography)
service.fontScale = 1.25f;  // 25% larger text

// Theme profile settings (informational for display logic)
var profile = service.Current;
if (profile.highContrast)
{
    // Optional: Apply additional contrast boosting
}
if (profile.colorVisionSafe)
{
    // Colors chosen to be safe for color blindness
}
```

## Minimal Usage Example

```csharp
using UnityEngine;
using BrewedCode.Theme;

public class HUDPanel : MonoBehaviour
{
    void Start()
    {
        var service = ThemeService.Instance;

        // Method 1: Bind TextMeshPro
        var titleText = GetComponentInChildren<TextMeshProUGUI>();
        var binding = titleText.gameObject.AddComponent<TMPStyleBinding>();
        binding.SetColorPath("Text/Heading");
        binding.SetTypographyPath("Heading/Large");

        // Method 2: Manual resolution
        var bodyText = GetComponentInChildren<TextMeshProUGUI>();
        bodyText.color = service.ResolveColor("Text/Secondary");
        if (service.TryResolveTypography("Body/Medium", out var typo))
        {
            bodyText.font = typo.font;
            bodyText.fontSize = Mathf.RoundToInt(typo.size * service.fontScale);
        }

        // Method 3: Listen to theme changes
        service.OnThemeChanged += UpdateUI;
    }

    void UpdateUI()
    {
        // Re-resolve and apply all colors/typography
    }
}
```

## Extension Points

The Theme System is designed to be extended through composition rather than inheritance.

### Creating Custom Bindings

Implement a component that applies theme tokens to any graphic element:

```csharp
public class CustomGraphicBinding : MonoBehaviour
{
    [SerializeField] private Graphic target;
    [SerializeField] private string colorPath = "Text/Primary";

    void OnEnable()
    {
        var service = ThemeService.Instance;
        service.OnThemeChanged += ApplyTheme;
        ApplyTheme();
    }

    void OnDisable()
    {
        var service = ThemeService.Instance;
        service.OnThemeChanged -= ApplyTheme;
    }

    void ApplyTheme()
    {
        var service = ThemeService.Instance;
        var scope = GetComponentInParent<IThemeResolver>();
        target.color = service.ResolveColor(colorPath, scope);
    }
}
```

### Creating Layered Theme Profiles

Create multiple theme assets and switch between them:

```csharp
// In a theme manager
public class ThemeManager : MonoBehaviour
{
    [SerializeField] private ThemeProfile[] themes;

    public void SetThemeByIndex(int index)
    {
        ThemeService.Instance.SetTheme(themes[index]);
    }
}
```

### Runtime Token Overrides

For per-instance customization, use `ThemeContext`:

```csharp
// Dialog with custom colors
var dialog = Instantiate(dialogPrefab);
var context = dialog.AddComponent<ThemeContext>();
context.colorOverrides = new[]
{
    new ThemeContext.ColorOverride
    {
        path = "Button/Primary/Background",
        color = new Color(0.2f, 0.8f, 0.2f, 1)  // Custom green
    }
};
```

### Event-Driven Updates

Listen to theme changes and update non-UI systems:

```csharp
ThemeService.Instance.OnThemeChanged += () =>
{
    // Rebuild UI layouts that depend on text metrics
    // Update visual overlays or particle effects
    // Refresh cached resources
};
```

## Token Path Naming Conventions

Based on the system's design, tokens use hierarchical paths with `/` separators:

- **Color paths:** `"Text/Primary"`, `"Button/Background"`, `"Error/400"`
- **Typography paths:** `"Body/Small"`, `"Heading/Large"`, `"Label/Compact"`
- **Component paths:** `"Button/Primary"`, `"Card/Header"`, `"Badge/Warning"`
- **State paths:** `"Button/Primary/Normal"`, `"Button/Primary/Hover"`

## Design Principles

1. **Token-Based:** All styling flows through semantic tokens, never hard-coded colors
2. **Hierarchical:** Local scopes can override global theme
3. **Cacheable:** Values are cached for performance
4. **Event-Driven:** Changes broadcast to all listeners
5. **Accessor-Friendly:** Simple `ResolveColor()` and `TryResolveTypography()` APIs
6. **Validatable:** Theme profiles can be validated for integrity
7. **Accessible:** Built-in font scaling and contrast/color-blindness modes

## Assumptions

- **Theme Assets are Design Data:** Themes are created in the editor and don't change programmatically
- **Singleton Pattern:** Only one `ThemeService` exists globally
- **TextMeshPro Focus:** Typography support assumes TextMeshPro, not legacy UI
- **UI-Only Scope:** Theme System applies only to UI, not to materials or 3D objects
- **Path-Based Resolution:** Colors/typography are always resolved by string path, enabling flexible customization

## Validation

Themes should be validated before use to catch missing references and naming issues:

```csharp
// In editor
var profile = AssetDatabase.LoadAssetAtPath<ThemeProfile>("Assets/Themes/MyTheme.asset");
var result = ThemeValidator.Validate(profile);
if (!result.IsValid)
    result.PrintToConsole();
```

The validator checks:
- RawPalette swatches have unique names
- ColorToken paths are unique and reference valid swatches
- TypographyToken paths are unique
- ComponentTheme references valid color token paths
