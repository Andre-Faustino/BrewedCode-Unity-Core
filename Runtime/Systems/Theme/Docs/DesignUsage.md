# Theme System: Design Usage Guide

This guide is for designers and creative directors working with the Theme System to define visual standards and token hierarchies.

## Design Philosophy

The Theme System is built on **semantic tokens** — named design values that express intent rather than appearance. Instead of thinking "blue color," think "primary action color" or "success state."

**Key Principle:** A token name describes its *purpose* in the interface, not its visual properties.

## Token Hierarchy

Tokens follow a **two-level structure**: Base Colors → Semantic Tokens → Components.

```
┌─────────────────────────────────────────┐
│  RawPalette (Base Colors)               │
│  Cyan500, Gray900, Error400, Success200│
│  (Raw design values)                    │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│  UiTokens (Semantic Mapping)            │
│  Text/Primary → Cyan500                 │
│  Button/Danger/Background → Error400    │
│  (Intent-based naming)                  │
└────────────┬────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────┐
│  ComponentTheme (State Styling)         │
│  Button/Primary/Normal → Text/Primary   │
│  Button/Primary/Hover → Text/Accent     │
│  (Component-specific rules)             │
└─────────────────────────────────────────┘
```

This structure provides **flexibility**: you can swap colors by changing one token, and all dependent components update automatically.

## Creating a Palette (RawPalette)

A **RawPalette** is your foundational color library. It contains named color swatches with no semantic meaning — purely the raw design values.

### Naming Conventions

Use **family + number** format:

- **Neutral colors:** `Gray50`, `Gray100`, `Gray900` (lightest to darkest)
- **Primary colors:** `Cyan300`, `Cyan500`, `Cyan700`
- **Semantic colors:** `Success400`, `Error500`, `Warning300`, `Info400`
- **Accent colors:** `Orange600`, `Purple400`

**Rationale:**
- `Gray50` is clearly lighter than `Gray900`
- Numbers help with ordering and intensity
- Family names group related colors visually

### Palette Size Guidelines

- **Minimal:** 3-5 base color families (Neutral, Primary, Accent)
- **Standard:** 6-8 families covering (Neutral, Primary, Secondary, Success, Error, Warning, Info)
- **Large:** 10+ families for specialized UI systems

**Example minimal palette:**
```
Cyan50, Cyan100, Cyan500, Cyan700, Cyan900
Gray50, Gray100, Gray500, Gray700, Gray900
Error400, Error500, Error600
Success400, Success500, Success600
```

### Accessibility in Palettes

Ensure sufficient **contrast** in your palette:
- Text on `Gray100` background needs contrast with darker colors
- Interactive elements (buttons) need sufficient luminosity difference from backgrounds
- Consider colorblind-safe combinations: avoid pure red-green combinations

## Creating Semantic Tokens (UiTokens)

Once you have a palette, create **semantic tokens** that map palette colors to their *usage* in the UI.

### Color Token Structure

**Path:** Use hierarchical naming with `/` separators

```
Category/Role/Variant
```

**Examples:**
- `Text/Primary` — Main readable text
- `Text/Secondary` — De-emphasized text (captions, labels)
- `Text/Inverse` — Text on dark backgrounds
- `Button/Background` — Button fill color
- `Button/Hover` — Button color on hover (optional, handled by ComponentTheme)
- `Background/Primary` — Main surface color
- `Background/Elevated` — Cards, panels above primary surface
- `Border/Default` — Standard borders
- `Status/Success`, `Status/Error`, `Status/Warning` — Semantic feedback

### Token Naming Best Practices

| ✅ Good | ❌ Bad | Why |
|---------|--------|-----|
| `Text/Primary` | `MainText` | Clear hierarchy & nesting |
| `Button/Primary/Background` | `BlueButton` | Semantic, not color-based |
| `Status/Error` | `RedAlert` | Conveys intent, not appearance |
| `Interactive/Hover` | `OnHover` | Clear category for related tokens |
| `Surface/Card` | `CardBg` | Expands later: `Surface/Card/Primary`, etc. |

### Color Token Alpha and Transparency

When creating color tokens, you can control transparency two ways:

**1. Inherit from Palette:**
```
ColorToken: "Background/Glass"
  → rawRef: "Gray700"
  → alpha: 0.8
  → inheritRawAlpha: true
```
Result: Gray700's alpha × 0.8

**2. Override Completely:**
```
ColorToken: "Overlay/Dark"
  → rawRef: "Gray900"
  → alpha: 0.7
  → inheritRawAlpha: false
```
Result: Exactly 0.7 alpha, ignoring Gray900's original alpha

Use inheritance for subtle effects; use override for precise control.

### Typography Tokens

Similar structure, but maps to fonts and sizing:

**Path:** `Category/Size` or `Category/Role`

**Examples:**
- `Heading/Large` — Page titles (font: HeadingFont, size: 32px)
- `Heading/Medium` — Section headers (size: 24px)
- `Body/Large` — Body text (font: BodyFont, size: 16px)
- `Body/Small` — Captions, labels (size: 12px)
- `Label/Compact` — UI labels (size: 11px, tight spacing)
- `Mono/Code` — Code snippets (font: MonoFont, size: 13px)

**Typography Token Attributes:**
- **Font:** The TMP_FontAsset to use
- **Size:** Base pixel size (before accessibility scaling)
- **Line Spacing:** Vertical space between lines (relative, e.g., +0.2)
- **Character Spacing:** Letter spacing/tracking (relative)
- **Word Spacing:** Space between words (relative)
- **Paragraph Spacing:** Space between paragraphs (relative)
- **Font Style:** Bold, Italic, SmallCaps
- **All Caps:** Force uppercase rendering

### Organizing Tokens as You Scale

**Start simple:**
```
Text/Primary
Text/Secondary
Button/Background
```

**Expand with specificity:**
```
Text/Primary/Default
Text/Primary/Interactive
Text/Secondary/Muted
Text/Secondary/Disabled
Button/Primary/Background
Button/Secondary/Background
```

**Group by component families:**
```
Button/Primary/*
Button/Secondary/*
Button/Tertiary/*
Card/Header/*
Card/Content/*
Modal/Overlay/*
```

## Component Styling (ComponentTheme)

For interactive components, **ComponentTheme** maps component states to color tokens.

### State-Based Styling

A component can have multiple states:

```
Button/Primary
├── Normal: → Text/Primary, Background/Button
├── Hover:  → Text/Primary (brightened), Background/Button/Hover
├── Pressed: → Text/Contrast, Background/Button/Active
└── Disabled: → Text/Disabled, Background/Disabled
```

Each state points to a color token path. The system resolves the token to get the actual color.

### Design Pattern: Interactive State Colors

| State | Design Intent | Typical Token |
|-------|---------------|---------------|
| **Normal** | Default appearance | `Button/Primary/Background` |
| **Hover** | "This is interactive" | `Button/Primary/Hover` (slightly lighter/darker) |
| **Pressed** | Active, being clicked | `Button/Primary/Active` (stronger contrast) |
| **Disabled** | Cannot interact | `Surface/Disabled` (low contrast, often gray) |
| **Focus** | Keyboard navigation indicator | Usually added via outline, not color |

### Component Naming

Use the **component family + variant** pattern:

```
Button/Primary
Button/Secondary
Button/Tertiary
Button/Danger

Card/Default
Card/Elevated
Card/Flat

Badge/Success
Badge/Error
Badge/Warning
```

This enables grouping and makes clear that all `Button/*` variants are button-related.

## Theme Profile Assembly

A **ThemeProfile** combines all tokens into a cohesive theme.

### What Goes Into a Profile

1. **RawPalette** — Your base color library
2. **UiTokens** — Semantic color and typography mappings
3. **ComponentTheme array** — State-based component styling
4. **Accessibility flags:**
   - `highContrast` — Indicates this theme prioritizes contrast
   - `colorVisionSafe` — Colors are chosen for color-blind accessibility
   - `defaultFontScale` — Base font scaling (0.85-1.5) for readability

### Multi-Theme Projects

Create multiple profiles for different scenarios:

```
Theme/Light/LightTheme.asset
  → RawPalette: Light colors (bright backgrounds)
  → UiTokens: Light-optimized tokens

Theme/Dark/DarkTheme.asset
  → RawPalette: Dark colors (dark backgrounds)
  → UiTokens: Dark-optimized tokens

Theme/HighContrast/HighContrastTheme.asset
  → RawPalette: High-contrast colors
  → highContrast: true
  → defaultFontScale: 1.15
```

## Design Workflow: From Figma to Theme System

### Step 1: Document Design System

In your design tool (Figma, etc.), organize colors and typography:

```
Colors
├── Neutral (Gray50 - Gray900)
├── Primary (Cyan300 - Cyan700)
├── Feedback (Success, Error, Warning)

Typography
├── Headings (Large, Medium, Small)
├── Body (Large, Medium, Small)
└── Labels (Compact, Standard)
```

### Step 2: Create RawPalette

Transcribe all base colors into a RawPalette asset. This is your "ground truth" that designers reference.

### Step 3: Map to Semantic Tokens

For each design component, determine which colors it uses:

**Example: Primary Button**
- Background → `Cyan500` (swatch)
- Text → `White` or `Gray900` (swatch)
- Hover → `Cyan600` (swatch)
- Pressed → `Cyan700` (swatch)

Create semantic tokens:
- `Button/Primary/Background` → `Cyan500`
- `Button/Primary/Text` → `Gray900`
- `Button/Primary/Hover` → `Cyan600`
- `Button/Primary/Active` → `Cyan700`

### Step 4: Component Styling

Group related tokens into ComponentTheme definitions:

```
ComponentTheme: Button/Primary
├── Normal state → [Background/Button/Primary, Text/Primary]
├── Hover state → [Background/Button/Primary/Hover, Text/Primary]
├── Pressed state → [Background/Button/Primary/Active, Text/Contrast]
└── Disabled state → [Background/Disabled, Text/Disabled]
```

### Step 5: Validate and Review

Use the editor validation tools to ensure:
- All referenced swatches exist
- Token paths are consistent (no typos)
- No circular or missing references

## Design Iteration and Maintenance

### Updating Colors

**To change a color everywhere:**
1. Edit the swatch in RawPalette
2. All tokens using that swatch update automatically
3. All UI using those tokens updates automatically

**To change only button colors:**
1. Create new ComponentTheme or update existing one
2. Point it to different color tokens
3. Components can switch themes at runtime

### Token Organization as Growth

Start with flat token paths:
```
Text/Primary
Text/Secondary
```

As complexity grows, add specificity:
```
Text/Primary/Regular
Text/Primary/Strong
Text/Secondary/Regular
Text/Secondary/Muted
```

Use the validation tools to catch naming inconsistencies.

### Accessibility Refinement

After initial design, test and refine:

1. **Contrast:** Use WCAG contrast checker tools
   - AAA: 7:1 contrast ratio (highest standard)
   - AA: 4.5:1 contrast ratio (standard)
   - AA Large: 3:1 for large text

2. **Color Blindness:** Test palettes with deuteranopia/protanopia simulators
   - Avoid red-green only differentiators
   - Use texture, pattern, or saturation differences

3. **Scalability:** Set `defaultFontScale` appropriately for accessibility
   - 1.0 = standard
   - 1.15-1.25 = improved readability
   - 1.5 = maximum (for accessibility)

## Design Checklist

- [ ] RawPalette created with base colors
- [ ] Color names follow convention (Family + Number)
- [ ] UiTokens created for all UI categories
- [ ] Token paths are hierarchical and semantic
- [ ] Typography tokens defined for all text styles
- [ ] ComponentTheme created for interactive components
- [ ] All state transitions documented (Normal → Hover → Pressed)
- [ ] High contrast theme available (if required)
- [ ] Colorblind-safe palette verified
- [ ] Accessibility font scale set appropriately
- [ ] Theme validated in-editor
- [ ] Design changes documented alongside tokens

## Common Design Patterns

### Dark Mode

Create a separate palette with inverted luminosity:

```
Light: Gray50, Gray100, ... Gray900
Dark:  Gray900, Gray800, ... Gray50

Light Button: Gray100 background + Gray900 text
Dark Button:  Gray800 background + Gray100 text
```

### Accent Colors

Reserve specific color families for actions and feedback:

```
Primary Accent: Cyan500 (main call-to-action)
Danger Accent: Error500 (destructive actions)
Success Accent: Success500 (confirmations)
Warning Accent: Warning500 (cautions)
```

### Disabled State

Create a consistent disabled palette (low contrast, often gray):

```
Background/Disabled → Gray200 (light)
Text/Disabled → Gray500 (medium gray)
```

Use across all components for consistency.

### Surface Elevation

Use color saturation to indicate layering:

```
Surface/Base → Gray50
Surface/Elevated → Gray100
Surface/Overlay → Gray200
```

Lighter colors feel "raised" to the user.

## Validation

Before shipping a theme:

1. **Visual review:** All UI should look intentional, no misaligned colors
2. **Semantic check:** Token names match their usage
3. **Contrast audit:** Text meets WCAG AA or AAA
4. **State review:** Hover/Active/Disabled clearly differentiated
5. **Scale test:** Font scaling works at 0.75x and 1.5x
6. **Colorblind test:** Use simulator tools to verify accessibility

The system's validator catches structural issues; design review catches intent.
