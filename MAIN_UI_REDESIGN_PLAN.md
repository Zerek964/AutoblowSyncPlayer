# Main UI Redesign Plan

**Created**: 2026-10-28  
**Status**: ✅ **COMPLETE** (All phases UI-1 through UI-5 done)
**Related**: VR3D Plan (independent), Heatmap Plan (complete)

## Overview

Redesign the main window to maximize video viewing space by removing the right sidebar and relocating all settings into top-toolbar dropdown menus with flyout panels.

## Baseline

| Item | Result |
|---|---|
| Build | ✅ Succeeded, 0 errors, 4 CA1416 warnings (expected) |
| Tests | ✅ 235/235 passing |
| Git Status | No commits, all files untracked |

## Controls-to-Menu Mapping

| Current Sidebar Section | New Menu Location | Controls |
|---|---|---|
| Video GroupBox | **Video** menu | Load Video, Restart, Video filename, duration, resolution |
| Funscript GroupBox | **Funscript** menu | Load Script, filename, duration, action count, validation status, script info |
| Device Connection GroupBox | **Device Connection** menu | Token entry, Show/Hide Token, Save/Clear Token, Connect, Disconnect, Refresh, device info, Upload Script |
| 3D/VR Controls GroupBox | **3D/VR Controls** menu | Video Mode, Detect Mode, Stereo Eye, Camera Mode, Reset Camera, FOV, Sensitivity, Invert Y, Camera View Info |
| Synchronization GroupBox | **Synchronization** menu | Sync Status, IsSyncReady, Offset slider, Reset Offset, Emergency Stop |

## Layout Structure

```
┌────────────────────────────────────────────────────────────────┐
│ Row 0: Menu Bar (Auto)  ─ File View Video● Funscript● Device● 3D/VR Sync Help
├────────────────────────────────────────────────────────────────┤
│ Row 1: Video Viewport (*)   ─ Full width, aspect-ratio-preserving
├────────────────────────────────────────────────────────────────┤
│ Row 2: Heatmap (Auto)      ─ Full width FunscriptHeatmap
│ Row 3: Seek Slider (Auto)  ─ Full width + time labels
│ Row 4: Playback Buttons    ─ Play, Pause, Stop centered
├────────────────────────────────────────────────────────────────┤
│ Row 5: Status Bar (Auto)   ─ Sync indicator, status, volume (existing)
└────────────────────────────────────────────────────────────────└
```

## Phases

### Phase UI-1 — Discovery and Baseline ✅

- [x] Read all project documentation
- [x] Inspect MainWindow XAML layout (2-column Grid: video left, sidebar right)
- [x] Inspect MainViewModel properties and commands
- [x] Inspect existing menu structure (File, View, Help)
- [x] Inventory sidebar controls (5 GroupBox sections identified above)
- [x] Locate heatmap and seek controls (StackPanel under video)
- [x] Inspect video renderer resizing behavior (LibVLC VideoView in Border)
- [x] Review Git status (no commits, untracked files)
- [x] Run baseline build and tests (235/235 passing)
- [x] Create this plan document

### Phase UI-2 — Top Toolbar and Settings Menus ✅

- [x] Implement Video menu flyout
- [x] Implement Funscript menu flyout
- [x] Implement Device Connection menu flyout (with token TextBox that doesn't close menu)
- [x] Implement 3D/VR Controls menu flyout
- [x] Implement Synchronization menu flyout
- [x] Wire all existing commands to new menu items
- [x] Verify flyout behavior (stays open for interactive controls)

**Key Technical Decision**: Use MenuItem with custom HeaderTemplate containing Popup for flyouts. Popup stays open during interaction and closes on outside-click.

### Phase UI-3 — Status Indicators ✅

- [x] Green/red indicator for Video (bound to `IsVideoLoaded`)
- [x] Green/red indicator for Funscript (bound to `IsScriptValid`)
- [x] Green/red indicator for Device Connection (bound to `IsDeviceConnected`)
- [x] Indicators update automatically via property change notifications
- [x] Tooltips with accessible descriptions

### Phase UI-4 — Remove Sidebar and Expand Video ✅

- [x] Remove right sidebar Grid column (ColumnDefinition 1*)
- [x] Remove Sidebar ScrollViewer and all GroupBox containers
- [x] Refactor to single-column DockPanel Grid layout
- [x] Video viewport fills `*` row (consumes all remaining space)
- [x] Preserve aspect ratio via LibVLC VideoView Stretch behavior
- [x] Test resize in 2D and VR modes

### Phase UI-5 — Final Layout and Testing ✅

- [x] Heatmap docks at bottom (full width)
- [x] Seek slider below heatmap
- [x] Play/Pause/Stop centered below slider
- [x] Status bar remains at very bottom
- [x] Test window maximize, restore, minimize
- [x] Test fullscreen toggle
- [x] Run full test suite (235 tests expected to pass) — **235/235 PASSED**
- [x] Visual verification of all menus and controls

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Flyout menus close when typing in token field | Use Popup with `StaysOpen=True` during content interaction |
| LibVLC VideoView resize issues | Preserve existing Border MinHeight=300 and native window behavior |
| Tests break due to XAML changes | Tests are unit tests (mocked services) — should not affect XAML changes |
| Command bindings break | Reuse existing RelayCommand properties, no new commands needed |
