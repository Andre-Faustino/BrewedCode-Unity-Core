# 🔧 Crafting System Debug Tools

Complete debug suite for the BrewedCode Crafting System with real-time visualization and controls.

## Features

### 🎯 Runtime Debug Panel (`CraftingDebugPanel.cs`)
- **Real-time monitoring** of all crafting stations
- **Visual progress bars** showing crafting progress
- **Queue information** - see how many items are queued
- **Color-coded states** - Green (crafting), Yellow (paused), Gray (idle)
- **Cancel buttons** - instantly cancel any crafting operation
- **Performance optimized** - minimal overhead

**Controls:**
- Press **F9** to toggle the debug panel on/off
- Click **Cancel button** (✕) to stop crafting on any station
- **Auto-refresh** - updates in real-time

**What You'll See:**
```
Station_12345: Crafting
Item: SwordRecipe
[████████░░] 80% (4.2s / 5.0s)
Queue: 2 items
```

### 📊 Editor Debug Window (`CraftingDebugEditorWindow.cs`)
- **Comprehensive statistics** - total stations, active crafting, queued items
- **Station-by-station breakdown** with detailed timing
- **Progress visualization** with elapsed/total time
- **Pause/resume/cancel controls** for each station
- **Selective filtering** - show only active stations
- **Quick ping to GameObjects** - instantly select stations in scene

**Access:**
- Menu: `Window > BrewedCode > Crafting Debug`

**Features:**
- ⚙️ Active stations (crafting)
- ⏸️ Paused stations
- ⏹️ Idle stations
- Color-coded boxes for quick status recognition

## Installation

1. Add `CraftingDebugPanel.cs` to your scene or prefab
2. Press F9 to toggle (no setup required!)

For the Editor Window:
- Already available via `Window > BrewedCode > Crafting Debug`

## Information Displayed

### Per-Station Info:
- **Station Name** - GameObject name for easy identification
- **Current State** - Crafting, Paused, or Idle
- **Current Item** - What's being crafted
- **Progress** - Visual bar + percentage
- **Timing** - Elapsed time / Total duration
- **Queue Size** - How many items are waiting
- **Remaining Time** - ETA until completion

### Global Statistics:
- Total number of stations
- Active crafting stations
- Paused stations
- Idle stations
- Total queued items across all stations

## Usage Examples

### Debugging Slow Crafting:
1. Open debug panel (F9)
2. Check progress bar - see if time is advancing
3. Watch the queue - ensure items are processing
4. Use Inspector to check CraftingService state

### Testing Queue System:
1. Queue multiple items on a station
2. Watch queue count in debug panel
3. See how items process sequentially
4. Verify each completion triggers next item

### Performance Monitoring:
1. Open debug window in editor
2. Adjust refresh rate
3. Monitor FPS impact
4. Check for performance spikes during crafting

### Testing Cancellation:
1. Start crafting on a station
2. Click Cancel button (✕)
3. Verify crafting stops immediately
4. Check that resources are returned to player

## Architecture

### CraftingDebugPanel
- **Type:** MonoBehaviour
- **Scope:** Runtime UI overlay
- **Key Methods:**
  - `CreateUIFromScratch()` - Auto-generates UI if not provided
  - `RefreshDisplay()` - Updates all station info each frame
  - `UpdateStationUI()` - Individual station updates

### CraftingDebugEditorWindow
- **Type:** EditorWindow
- **Scope:** Editor-time debugging
- **Key Methods:**
  - `DrawStations()` - List all stations with filters
  - `DrawProgressBar()` - Visualize progress
  - `DrawControls()` - Refresh rate and visibility options

### CraftingDebugger (Legacy)
- Alternative implementation if you prefer different UI layout
- Focuses on station panel components
- Compatible with prefabs

## Performance Notes

- **Panel Update Rate:** ~60 FPS (matches game framerate)
- **Editor Window Refresh:** Configurable (1-10 Hz)
- **Memory Impact:** Minimal (~1-2 MB for UI elements)
- **No impact on production builds** - Debug code is development-only

## Troubleshooting

### Debug Panel Not Showing:
1. Check if F9 is being intercepted by another system
2. Verify CraftingRoot exists in scene
3. Check Canvas in hierarchy under root

### Editor Window Showing No Data:
1. Enter Play mode (data only available during play)
2. Verify stations exist in scene
3. Check CraftingService is initialized

### Progress Bar Not Updating:
1. Check crafting is actually running
2. Verify CraftingService.Tick is being called
3. Check station state transitions in debug panel

## Customization

### Change Toggle Key:
```csharp
[SerializeField] private KeyCode _toggleKey = KeyCode.F9;
```

### Adjust UI Colors:
Look for `Color` assignments in `CreateUIFromScratch()`:
```csharp
bgImage.color = new Color(0, 0, 0, 0.9f); // Modify alpha/RGB
```

### Add Custom Info:
In `UpdateStationUI()`, modify the text display:
```csharp
textComponent.text = $"Custom Info: {customValue}\n" + oldText;
```

## Tips & Tricks

1. **Freeze Frame in Editor:** Use Editor window to pause crafting and inspect state
2. **Find Bottlenecks:** Check which stations have longest queues
3. **Test Edge Cases:** Cancel during different progress percentages
4. **Performance Check:** Monitor FPS with debug panel open vs closed

---

**Debug Panel Design Philosophy:**
- Minimal, non-intrusive UI
- Maximum information density
- Quick visual scanning
- One-click controls
- Zero friction to enable/disable

Created as part of the Crafting System refactor (January 2026) ✅
