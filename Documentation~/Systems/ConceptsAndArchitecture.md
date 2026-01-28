# Theme System: Concepts and Architecture

This document explains the design philosophy, architectural decisions, and fundamental concepts that make the Theme System work.

## Core Philosophy

The Theme System is built on a single principle:

> **Separate Design Intent from Implementation**

Instead of writing code that says "text should be cyan," we write "text should be primary." The system maps semantic intent to concrete values.

Benefits:
- **Consistency:** All "primary" text uses the same color automatically
- **Flexibility:** Change "primary" color once, update everywhere
- **Scalability:** New components don't require color definitions
- **Accessibility:** Swap the entire theme for high-contrast or color-blind safe mode

## The Three-Layer Model

### Layer 1: Raw Design Values (RawPalette)

```
RawPalette: The Uninterpreted Truth
├── Cyan500 = #0891B2
├── Error400 = #F87171
└── Gray900 = #111827
```

Properties:
- **Direct mapping:** Color name → RGB value
- **No semantics:** Just names and values, no interpretation
- **Reusable:** Can be referenced by many tokens
- **Stable:** Changes rarely without design direction

### Layer 2: Semantic Tokens (UiTokens)

```
UiTokens: Interpreted Intent
├── Text/Primary → Cyan500 (with alpha)
├── Status/Error → Error400
└── Surface/Background → Gray900
```

Properties:
- **Semantic naming:** Path reflects purpose
- **Indirection:** Names map to palette colors
- **Composable:** Can layer paths (Text → Text/Primary)
- **Flexible:** One token can change many UI elements

### Layer 3: Component Styling (ComponentTheme)

```
ComponentTheme: State-Based Application
├── Button/Primary
│   ├── Normal: Text/Primary + Background/Button
│   ├── Hover: Text/Primary + Background/Button/Hover
│   └── Pressed: Text/Contrast + Background/Button/Active
└── Card/Header
    └── Normal: Text/Heading + Background/Elevated
```

Properties:
- **Component-focused:** Defines styling for specific component types
- **State-aware:** Different colors for different interaction states
- **Hierarchical:** Groups related component variants
- **Optional:** Many UIs can use tokens directly without component definitions

### Why Three Layers?

**Flexibility at each level:**

| Level | Change | Impact |
|-------|--------|--------|
| Raw palette | Cyan500 = #00FF00 | All "primary" UI changes |
| Semantic token | Text/Primary → Purple500 | All primary text changes |
| Component theme | Button/Primary/Hover → Text/Accent | Only button hover state |

Each layer can change without affecting others.

## Token Resolution: The Dependency Chain

When you ask "what color is Text/Primary?", the system follows a chain:

```
Token Path "Text/Primary"
    ↓
UiTokens asset
    ↓
ColorToken { path: "Text/Primary", rawRef: "Cyan500", alpha: 1.0 }
    ↓
RawPalette asset
    ↓
Swatch { name: "Cyan500", color: #0891B2 }
    ↓
Final Color: #0891B2 (with alpha applied)
```

### Resolution Algorithm

```
ResolveColor(path, context)
  1. If context has override for path → return it
  2. Determine profile: context.LocalProfile ?? global theme
  3. Check cache for (profile, path) → return if hit
  4. Find ColorToken in profile.ui matching path
  5. Get rawRef swatch from profile.raw
  6. Apply alpha logic:
     - If inheritRawAlpha: color.alpha = swatch.alpha * token.alpha
     - Else: color.alpha = token.alpha
  7. Cache and return color
```

Key points:
- **Early exit:** Overrides and cache prevent expensive lookups
- **Safe fallbacks:** Returns white if token or swatch missing
- **Alpha flexibility:** Supports both inheritance and override modes
- **Deterministic:** Same path always produces same color

## Hierarchical Scope Resolution

The system supports **scoped themes** through the `IThemeResolver` interface:

```
Global Theme
└── ThemeService.Current

Local Scope (ThemeContext)
├── localProfile: Alternative theme for subtree
├── colorOverrides: Path → Color mappings
└── typographyOverrides: Path → Typography mappings
```

### Scope Chain

```
Bound Component
    ↓
IThemeResolver? (parent in hierarchy)
    ↓
Local Profile + Overrides
    ↓
Global ThemeService
```

**Example: Dialog with custom colors**

```
Main UI (Global Theme)
├── Dialog Container (ThemeContext with overrides)
│   ├── Button (uses Dialog's overrides)
│   └── Text (uses Dialog's overrides)
└── Other UI (uses Global Theme)
```

When Button resolves "Text/Primary":
1. Dialog's ThemeContext checks overrides → "Text/Primary" override exists
2. Returns custom color
3. Button displays custom color

Without the override, would fall through to global theme.

## The Binding Pattern

Bindings automatically apply tokens to components:

```
Component (e.g., TextMeshPro)
    ↓
Binding (e.g., TMPStyleBinding)
    ├─ Awake: Find scope (IThemeResolver parent)
    ├─ OnEnable: Subscribe to OnThemeChanged
    ├─ ApplyAll(): Resolve + Apply
    └─ OnThemeChanged: Re-apply
```

**Why bindings?**

1. **Separation of concerns:** Tokens are pure data, bindings apply them
2. **Reusability:** One binding type handles all components of that type
3. **Automatic updates:** Theme changes trigger re-application
4. **Flexibility:** Can bind to any component with inspector configuration

**Alternative:** Manual resolution (for custom logic)

```csharp
var color = service.ResolveColor("Text/Primary");
myGraphic.color = color;
```

Bindings are **convenience**, not requirement.

## Caching Strategy

The service caches two types of values:

```csharp
private Dictionary<(ThemeProfile, string), Color> _colorCache;
private Dictionary<(ThemeProfile, string), TypographyToken> _typoCache;
```

**Why cache?**

- **Array searches are O(n):** Finding token in array requires scan
- **Frequent access:** Same tokens resolved repeatedly
- **Predictable:** Resolution is pure (no side effects)

**When to clear cache:**

```csharp
SetTheme(newProfile)        // Different profile
NotifyThemeChanged()        // Profile contents changed
```

**Limitation:** Cache is per-service, not global. If you modify a theme asset at runtime without calling `NotifyThemeChanged()`, stale values remain.

## Event-Driven Architecture

The system broadcasts changes through events:

```csharp
public event Action OnThemeChanged;
```

**Publishing:**
- `SetTheme()` → clear cache → invoke event
- `NotifyThemeChanged()` → clear cache → invoke event

**Subscribing:**
- Bindings listen to stay synchronized
- Custom code can listen to react to changes
- Loose coupling: no hardcoded dependencies

**Advantage:** Decentralized updates. Multiple systems can react without knowing about each other.

## Asset Structure

### ThemeProfile (Root)

```
ThemeProfile
├── raw: RawPalette (required)
├── ui: UiTokens (required)
├── components: ComponentTheme[] (optional)
├── highContrast: bool
├── colorVisionSafe: bool
└── defaultFontScale: float
```

**Design decision:** Single asset combines everything. Alternatives considered:

| Approach | Pros | Cons |
|----------|------|------|
| **Single asset** | Simple, single reference, bundling | Monolithic, harder to version |
| **Split assets** | Modular, reusable | Complex references, harder to validate |
| **Prefab-based** | Scene-integrated | Couples to scene |

Single asset chosen for simplicity and bundling efficiency.

### RawPalette (Immutable Reference)

```
RawPalette
└── swatches: Swatch[]
    ├── name: string (immutable)
    └── color: Color (editable)
```

**Design decision:** Only swatches, no hierarchy. Why?

- **Simplicity:** Designer just adds colors
- **Flexibility:** Any hierarchy is added via tokens
- **Stability:** Swatch names don't change when building structure

### UiTokens (Pure Mapping)

```
UiTokens
├── colors: ColorToken[]
│   ├── path: string
│   ├── rawRef: string (swatch name)
│   ├── alpha: float
│   └── inheritRawAlpha: bool
└── typography: TypographyToken[]
    ├── path: string
    ├── font: TMP_FontAsset
    ├── size: int
    └── spacing, style, etc.
```

**Design decision:** Strings for references, not object references. Why?

```csharp
// String reference (chosen)
rawRef: "Cyan500"
// Advantages:
// - Swatch can be renamed without breaking token
// - Easy to debug and validate
// - No circular dependencies
// - Can exist without RawPalette in asset

// Object reference (rejected)
rawRef: <RawPalette.Swatch reference>
// Disadvantages:
// - Tight coupling
// - Breaks if swatch deleted
// - Harder to validate
// - Circular dependencies possible
```

## Validation Philosophy

The validator is **structural**, not semantic:

```
✓ Validates: Names, references, uniqueness
✗ Doesn't validate: Color contrast, colorblind safety, aesthetic choices
```

**Why?**

- **Structural issues are objective:** "Swatch doesn't exist" is clear
- **Semantic issues are subjective:** "Color is too bright" is opinion
- **Tool support:** Contrast checkers exist elsewhere; we catch missing links

**Validation layers:**

```
UiTokens Level:
  ✓ ColorToken paths are unique
  ✓ rawRef values exist in palette
  ✓ typography paths are unique

ComponentTheme Level:
  ✓ colorTokenPath references exist in tokens
  ✓ component IDs are consistent

ThemeProfile Level:
  ✓ All sub-assets exist
  ✓ No cycles or broken chains
```

## Accessibility Integration

### Font Scaling

```csharp
public float fontScale = 1.0f;
// Range: 0.75 (compact) to 1.75 (large text)
```

**Design decision:** Single global value. Why?

- **Simplicity:** One setting affects all text
- **Consistency:** All sizes scale proportionally
- **Player control:** Can adjust without changing theme

Alternative: Per-token sizing (rejected)
- Complex to override
- Inconsistent if not careful
- Global scaling still needed for override

### Theme Accessibility Flags

```csharp
public bool highContrast;      // Increased contrast ratios
public bool colorVisionSafe;   // Colors chosen for accessibility
```

**Design decision:** Informational flags, not enforcing. Why?

- **Designer controls:** Can create accessible themes
- **Code reacts:** Can adjust UI if accessibility enabled
- **Flexible:** Doesn't override player preferences

Example usage:

```csharp
if (profile.highContrast)
    DisplayAdditionalVisualIndicators();
```

## Singleton Pattern: PersistentMonoSingleton

```csharp
public sealed class ThemeService : PersistentMonoSingleton<ThemeService>
```

**Design decision:** Singleton, not static class. Why?

| Aspect | Static Class | Singleton |
|--------|-------------|-----------|
| **Scene independence** | ✗ | ✓ |
| **Inspector control** | ✗ | ✓ |
| **Lifecycle hooks** | ✗ | ✓ |
| **Persistent** | N/A | ✓ |
| **Testing** | ✗ | ✓ |

Singleton allows:
- Initializing with inspector-assigned theme
- Persisting across scene loads
- Testing with instance creation

## Extension Points

The system is designed for composition (extending behavior) not inheritance:

### Extension 1: Custom Bindings

**Pattern:** Implement logic similar to `TMPStyleBinding`

```csharp
public class CustomBinding : MonoBehaviour
{
    void ApplyTheme()
    {
        var service = ThemeService.Instance;
        var scope = GetComponentInParent<IThemeResolver>();
        target.color = service.ResolveColor(colorPath, scope);
    }
}
```

**Why not inheritance?**
- Each component type is different
- Single base class can't handle all cases
- Composition is simpler than deep hierarchies

### Extension 2: Theme Layering

**Pattern:** Multiple profiles with fallback

```csharp
var darkTheme = LoadTheme("dark");
var lightTheme = LoadTheme("light");

// Switch at runtime
service.SetTheme(season == "winter" ? darkTheme : lightTheme);
```

### Extension 3: Runtime Token Overrides

**Pattern:** ThemeContext for scoped customization

```csharp
var context = container.AddComponent<ThemeContext>();
context.colorOverrides = new[] { /* custom colors */ };
```

### Extension 4: Custom Resolution Logic

**Pattern:** Implement IThemeResolver

```csharp
public class CustomResolver : MonoBehaviour, IThemeResolver
{
    public ThemeProfile LocalProfile => theme;

    public bool TryResolveColor(string path, out Color color)
    {
        // Custom resolution logic
    }
}
```

## Non-Goals (What We Don't Do)

The Theme System deliberately doesn't:

### 1. Animate Transitions

**Reason:** Theming is about values, not animation. Animations are better handled by separate systems.

**Alternative:** Use `OnThemeChanged` to trigger animations:

```csharp
service.OnThemeChanged += () =>
{
    // Animate color change
    StartCoroutine(AnimateColorChange());
};
```

### 2. Manage 3D Materials or Meshes

**Reason:** UI theming and 3D material theming are fundamentally different. Mixing them causes complexity.

**Alternative:** Create a separate system for 3D theming, or manually apply theme values to 3D materials:

```csharp
material.color = ThemeService.Instance.ResolveColor("StatusColor");
```

### 3. Create Themes Dynamically

**Reason:** Themes are design assets. Dynamic creation adds complexity without clear benefit.

**Alternative:** Pre-create theme assets, load at runtime.

### 4. Handle Font Installation

**Reason:** Font management is Unity's job, not Theme System's.

**Alternative:** Fonts are assigned in typography tokens; Unity handles the rest.

### 5. Apply Themes to Non-UI Elements

**Reason:** Out of scope. UI has different styling needs than 3D.

**Alternative:** Use theme values (ResolveColor) in your own rendering code if needed.

## Performance Characteristics

### Resolution Time

```
First call: O(n) — Array search in tokens
Cached call: O(1) — Dictionary lookup
Failure: O(n) — Full array scan returns default
```

**Typical values:**
- 10 tokens: < 0.1ms per lookup
- 100 tokens: < 0.5ms per lookup (rare)

Resolution is not a bottleneck for UI.

### Memory Usage

```
ColorCache: ~48 bytes per entry
TypographyCache: ~100 bytes per entry

Typical: 20-50 unique tokens = 1-5 KB cache
```

Negligible compared to other UI systems.

### Binding Overhead

```
OnEnable: Resolve colors + apply to component (~0.5ms)
OnThemeChanged: Re-resolve + apply (~0.5ms per binding)
Update: Nothing (cached or subscribed events)
```

Bindings have no Update-loop cost.

## Constraints and Trade-offs

### No Runtime Asset Modification

```csharp
// This doesn't work
var token = profile.ui.colors[0];
token.alpha = 0.5f;  // Local change, not reflected globally
```

**Reason:** Bindings won't know to re-apply, cache will be stale.

**Workaround:** Use ThemeContext overrides instead:

```csharp
var context = gameObject.AddComponent<ThemeContext>();
context.colorOverrides = new[] { /* new values */ };
```

### Token Paths Are Strings

```csharp
// No autocomplete
service.ResolveColor("Text/Primarry");  // Typo silently returns white
```

**Reason:** Flexibility. String paths allow:
- Runtime-determined paths
- Dynamic token selection
- No hardcoded token registry

**Workaround:** Use constants:

```csharp
public static class TokenPaths
{
    public const string TextPrimary = "Text/Primary";
    public const string TextSecondary = "Text/Secondary";
}

service.ResolveColor(TokenPaths.TextPrimary);
```

### No Partial Overrides

```csharp
// Can't override just one aspect
var typo = ResolveTypography("Body/Large");
typo.size = 20;  // Change doesn't persist
```

**Reason:** Returned values are copies, not references.

**Workaround:** Full override via ThemeContext:

```csharp
context.typographyOverrides = new[]
{
    new ThemeContext.TypographyOverride
    {
        path = "Body/Large",
        token = customToken  // Full token
    }
};
```

## Design Rationale Summary

| Decision | Benefit | Trade-off |
|----------|---------|-----------|
| **Three-layer model** | Flexibility | Slight complexity |
| **String-based references** | Runtime flexibility | No autocomplete |
| **Singleton service** | Persistent, testable | Global state |
| **Event-driven** | Loose coupling | Harder to debug |
| **Composition-based** | Flexible, simple | More code for customization |
| **No runtime modification** | Cache validity | Need workarounds |
| **Pure resolution** | Deterministic | No partial application |
| **No animation** | Separation of concerns | Need separate system |

Each decision prioritizes **simplicity and flexibility** over complexity and power.

## Future Evolution Possibilities

The system could be extended (without modification) to:

- **Localized typography:** Different font sizes per language
- **Device-specific tokens:** Responsive design tokens
- **Animated transitions:** Custom resolver returns interpolated values
- **Constraint-based theming:** "Maintain 4.5:1 contrast ratio"
- **Token composition:** One token references another
- **Theme inheritance:** Default theme + overrides

Current design doesn't prevent these; they'd be additions, not changes.
