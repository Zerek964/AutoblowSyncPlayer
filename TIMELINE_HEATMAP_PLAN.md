# Funscript Timeline Heatmap — Detailed Implementation Plan

## Overview

Rearrange the video playback area to add a color-coded funscript motion heatmap between the video player and seek slider, and relocate the seek slider and Play/Pause/Stop buttons below the video player. This is a **UI layout and visualization** feature that must not break existing 3D/VR camera, device sync, or fullscreen functionality.

## Current Architecture Facts

### Funscript Model
- `Funscript` (Core/Models): `IReadOnlyList<FunscriptAction> Actions`, `DurationMs` (derived from last action), `ActionCount`
- `FunscriptAction` (Core/Models): `long At` (ms), `int Pos` (0-100)
- `IFunscriptService` / `FunscriptService`: Parse and validate `.funscript` JSON files

### Video Clock
- `IVideoPlayerService.PositionMs` is the **single source of truth** for video timeline
- `IVideoPlayerService.DurationMs`: total video duration in ms
- `IVideoPlayerService.SeekToAsync(long positionMs)`: seek to absolute ms
- LibVLC: `_mediaPlayer.Time` and `_mediaPlayer.Length`

### Sync Service
- `ISyncService.SeekAsync(long newVideoPositionMs, bool isPlaying)`: resync device on seek
- `ISyncService.StartPlaybackAsync(long videoPositionMs)`: start/resume device sync

### MainViewModel
- `VideoProgress` (double, 0-100): bound to seek slider, updated via `OnVideoPositionChanged`
- `OnVideoPositionChanged(object?, long positionMs)`: dispatcher callback updates VideoProgress, VideoTimestamp, SyncDifference
- `SeekToPositionAsync(double value)`: converts percentage → ms, seeks video + sync
- `OnVideoProgressChanged(double value)`: property changed handler triggers seek if difference > 1s
- `_validatedScriptPath`, `_funscriptService`: loaded script path and service
- Commands: `PlayCommand`, `PauseCommand`, `StopCommand`, `LoadVideoCommand`, `LoadScriptCommand`, `RestartCommand`

### MainWindow.xaml Layout (Current)
- Grid with 2 columns (2*:1*): left = video player, right = controls panel
- **Video player**: `vlc:VideoView` with camera mouse handlers, fullscreen button overlay
- **Right panel**: ScrollViewer with GroupBoxes for Video (Load Video, Restart, Play/Pause/Stop, seek slider), Funscript, Device Connection, 3D/VR Camera
- **Status bar**: sync indicator, status message, VideoTimestamp, SyncDifference, volume controls

## New Layout

```
┌──────────────────────────────────────────────┐
│                                              │
│              VIDEO PLAYER                    │
│              (VideoView + fullscreen btn)     │
│                                              │
├──────────────────────────────────────────────┤
│ FUNSCRIPT MOTION HEATMAP                      │
│ [color bands]       ▲ live position marker    │
├──────────────────────────────────────────────┤
│ SEEK SLIDER (relocated from right panel)     │
│ 00:00 ━━━━━━━━━●━━━━━━━━━━━━━━━━━━ 12:30    │
├──────────────────────────────────────────────┤
│ [▶ Play]  [⏸ Pause]  [⏹ Stop]               │
│ (relocated from right panel)                 │
└──────────────────────────────────────────────┘
```

Right panel will still contain: Funscript controls, Device Connection, 3D/VR Camera (all preserved).

---

## Phase H1 — Discovery and UI Planning (Current Phase)

**Status:** ✅ In Progress

**Tasks:**
- [x] Read existing documentation (AGENTS.md, PLAN.md, PROGRESS.md, etc.)
- [x] Inspect MainWindow.xaml layout and control positions
- [x] Inspect funscript models and parser
- [x] Inspect video clock (`PositionMs`) and sync logic
- [x] Run baseline build and tests (214/214 ✅)
- [ ] Create this plan document
- [ ] Update PLAN.md with reference

**Acceptance:** Existing app builds, current control bindings identified, plan exists.

---

## Phase H2 — Heatmap Data and Rendering

**Goal:** Implement the heatmap data calculation and WPF rendering control.

### H2.1 — Activity Calculation Service

**New file:** `src/AutoblowSyncPlayer.Core/Models/HeatmapActivity.cs`
- Represents a single time bucket of activity data: `{ long StartMs; long EndMs; double ActivityLevel; }`

**New file:** `src/AutoblowSyncPlayer.Core/Services/IHeatmapCalculator.cs`
```csharp
public interface IHeatmapCalculator
{
    /// <summary>
    /// Calculate activity data for a funscript mapped to a video duration.
    /// </summary>
    IReadOnlyList<HeatmapActivity> CalculateActivities(Funscript funscript, long videoDurationMs, int bucketCount);
    
    /// <summary>
    /// Get activity level at a specific millisecond position.
    /// </summary>
    double GetActivityAtPosition(IReadOnlyList<HeatmapActivity> activities, long positionMs);
}
```

**New file:** `src/AutoblowSyncPlayer.Infrastructure/Services/HeatmapCalculator.cs`

**Algorithm:**
1. Divide the video timeline into `bucketCount` equal time buckets (default: min(width_pixels, 2000) for performance)
2. For each pair of consecutive funscript actions, calculate the movement speed: `|pos2 - pos1| / (at2 - at1)` (units: position change per ms)
3. Distribute this speed across all time buckets that fall within the interval `[at1, at2]`
4. Accumulate speeds within each bucket (sum of speeds for overlapping intervals)
5. Normalize bucket values to 0.0–1.0 range using percentile-based clamping (95th percentile = 1.0 to handle outliers)
6. Handle edge cases:
   - No script loaded: return empty list (dark gray visualization)
   - Script shorter than video: activity after script ends = 0
   - Script longer than video: clip to video duration

**Why speed-based, not position-based:** The spec says "Do not treat funscript position values alone as activity intensity." Speed = movement distance per time = actual device activity intensity.

**Tests:**
- `HeatmapCalculatorTests.cs` (20+ tests):
  - Empty script returns empty activities
  - Single action returns all-zero activities
  - Two actions produce correct speed distribution
  - Actions at different speeds produce proportional activity levels
  - Normalization caps at 95th percentile
  - Script shorter than video clips correctly
  - Script longer than video clips correctly
  - 100K actions complete within 500ms (performance test)
  - Bucket count respects max limit

### H2.2 — Heatmap WPF Control

**New file:** `src/AutoblowSyncPlayer.App/Controls/FunscriptHeatmap.xaml`
**New file:** `src/AutoblowSyncPlayer.App/Controls/FunscriptHeatmap.xaml.cs`

A custom WPF `UserControl` with:
- `WriteableBitmap` for efficient rendering (no element-per-action)
- `Canvas` overlay for the playback position marker
- Properties:
  - `Activities`: `IReadOnlyList<HeatmapActivity>` — bound to calculated data
  - `VideoProgress`: `double` 0-100 — bound to MainViewModel.VideoProgress for live marker
  - `HasScript`: `bool` — shows "Load a funscript" message when false
  - `ScriptExceedsVideo`: `bool` — shows warning when script extends past video

**Color Scale:**
| Activity Level | Color         | Category     |
|---------------|---------------|--------------|
| 0.0           | Dark gray     | None         |
| 0.0–0.25      | Blue/teal     | Low          |
| 0.25–0.50     | Yellow        | Moderate     |
| 0.50–0.75     | Orange        | High         |
| 0.75–1.0      | Red           | Very High    |

**Rendering strategy:**
- Precompute pixel colors from activity buckets
- Render to `WriteableBitmap` in background thread
- Update only when activities change or control width changes (not on every video frame)
- Playback marker is a separate `Line` element updated via `VideoProgress` binding (cheap, no re-render)

**Height:** ~30px fixed height, scalable on resize.

### H2.3 — MainViewModel Heatmap Properties

Add to `MainViewModel`:
```csharp
private IReadOnlyList<HeatmapActivity>? _heatmapActivities;
public IReadOnlyList<HeatmapActivity>? HeatmapActivities
{
    get => _heatmapActivities;
    set => SetProperty(ref _heatmapActivities, value);
}

public bool HasScript => !string.IsNullOrEmpty(_validatedScriptPath);
public bool ScriptExceedsVideo { get; private set; }
```

Update `ValidateScriptFileAsync` to calculate heatmap after successful validation:
```csharp
// After script is validated successfully
var activities = _heatmapCalculator.CalculateActivities(
    result.Script, _videoPlayer.DurationMs, BUCKET_COUNT);
HeatmapActivities = activities;
ScriptExceedsVideo = result.Script.DurationMs > _videoPlayer.DurationMs;
```

Update video loading to recalculate heatmap with new duration.

**Tests:**
- `FunscriptHeatmapControlTests.cs`: Visual tests for rendering, color mapping, marker position
- Unit tests for `IHeatmapCalculator` implementation

---

## Phase H3 — Playback Marker and Interaction

**Goal:** Live playback marker, hover tooltips, click-to-seek.

### H3.1 — Live Playback Marker

- Already handled in H2.2: `VideoProgress` binding on `FunscriptHeatmap` drives a white vertical line
- Marker position: `X = (VideoProgress / 100) * heatmapWidth`
- Updated automatically when `OnVideoPositionChanged` fires (existing callback)

### H3.2 — Hover Tooltip

In `FunscriptHeatmap` control:
- `MouseMove` handler: calculate timestamp from mouse X position
- Look up activity level at that timestamp from precomputed activities
- Show tooltip: `"02:34 — High activity (67%)"` using actual values

### H3.3 — Click-to-Seek

In `FunscriptHeatmap` control:
- `MouseDown` handler: calculate target position percentage from mouse X
- Call `MainViewModel.SeekToPositionAsync(targetPercentage)` — reuse existing seek command
- This automatically triggers `_videoPlayer.SeekToAsync()` + `_syncService.SeekAsync()` (existing behavior)
- Do NOT create new seek logic

### H3.4 — Pause/Stop/Seek Marker Behavior

- `OnVideoPositionChanged` already handles all cases (play, pause, seek, stop, load new video)
- Marker follows `VideoProgress` which is driven by the single video clock
- No additional timer or state tracking needed

**Tests:**
- Click heatmap at various positions triggers correct seek percentages
- Hover shows correct timestamp and activity level
- Seek during playback resyncs device (via existing sync service)

---

## Phase H4 — Relocate Video Controls

**Goal:** Move seek slider and Play/Pause/Stop buttons from right panel to below the video player.

### H4.1 — MainWindow.xaml Restructure

Change left column from a single `Border` to a `StackPanel`:

**Before:**
```xml
<Border Grid.Column="0" ...>
    <Grid>
        <vlc:VideoView .../>
        <Button ... ToggleFullscreen .../>
    </Grid>
</Border>
```

**After:**
```xml
<StackPanel Grid.Column="0" Margin="10">
    <!-- Video Player (stretches to fill available space) -->
    <Border BorderBrush="..." BorderThickness="1" Background="Black" CornerRadius="4"
            VerticalAlignment="Stretch" HorizontalAlignment="Stretch">
        <Grid>
            <vlc:VideoView .../>
            <Button ... ToggleFullscreen .../>
        </Grid>
    </Border>
    
    <!-- Funscript Heatmap -->
    <Controls:FunscriptHeatmap 
        Activities="{Binding HeatmapActivities}"
        VideoProgress="{Binding VideoProgress}"
        HasScript="{Binding HasScript}"
        ScriptExceedsVideo="{Binding ScriptExceedsVideo}"
        Margin="0,5,0,0"/>
    
    <!-- Seek Slider (relocated from right panel Video GroupBox) -->
    <Slider Value="{Binding VideoProgress, Mode=TwoWay}" ...
            Margin="0,10,0,0"/>
    <TextBlock ... "Drag slider to seek ..." HorizontalAlignment="Center"/>
    
    <!-- Play/Pause/Stop (relocated from right panel Video GroupBox) -->
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,10,0,0">
        <Button Command="{Binding PlayCommand}" ...>▶ Play</Button>
        <Button Command="{Binding PauseCommand}" ...>⏸ Pause</Button>
        <Button Command="{Binding StopCommand}" ...>⏹ Stop</Button>
    </StackPanel>
</StackPanel>
```

### H4.2 — Remove Duplicate Controls

From the right panel "Video" GroupBox, remove:
- Play/Pause/Stop buttons
- Seek slider
- "Drag slider to seek" text

The right panel will retain:
- "Load Video..." and "↺ Restart" buttons (keep in Video GroupBox)
- Funscript GroupBox (all contents)
- Device Connection GroupBox (all contents)
- 3D/VR Camera GroupBox (all contents)

### H4.3 — Video Sizing

The video player `Border` will use `VerticalAlignment="Stretch"` within the `StackPanel`. The `StackPanel` itself stretches within the Grid column. On window resize:
- Video player height adjusts based on remaining space after heatmap + slider + buttons
- Minimum height constraints prevent controls from being squashed

### H4.4 — Binding Verification

All existing bindings remain unchanged:
- `Slider.Value` → `MainViewModel.VideoProgress` (TwoWay)
- `Button.Command` → `PlayCommand`, `PauseCommand`, `StopCommand`
- IsEnabled states preserved via existing bindings

**Tests:**
- Visual verification: controls appear in correct order below video
- No duplicate controls in right panel
- Click Play/Pause/Stop from new location works correctly
- Seek slider works from new location
- Window resize doesn't break layout

---

## Phase H5 — Regression Testing and Release

**Goal:** Verify no regressions in existing functionality.

### Automated Tests
- [ ] `dotnet build` — 0 errors
- [ ] `dotnet test` — all 214 existing tests pass + 25+ new heatmap tests

### Manual Verification Checklist
- [ ] Load MP4 → video plays normally
- [ ] Load funscript → heatmap renders with correct color bands
- [ ] No funscript → heatmap shows "Load a funscript" message (dark gray)
- [ ] Play → playback marker moves left-to-right
- [ ] Pause → marker freezes at current position
- [ ] Stop → marker resets to start
- [ ] Click heatmap → seeks to clicked position (video + device)
- [ ] Hover heatmap → tooltip shows timestamp + activity
- [ ] Drag seek slider → video seeks + marker follows
- [ ] 3D/VR video + camera mode → camera drag doesn't affect heatmap
- [ ] Heatmap interaction doesn't rotate camera
- [ ] Fullscreen → video goes fullscreen without heatmap interfering
- [ ] Volume controls → still work from status bar
- [ ] Device connect/disconnect/upload → still work from right panel
- [ ] Window resize → layout adjusts without breaking
- [ ] Large funscript (100K actions) → heatmap renders within 1 second
- [ ] Script shorter than video → activity clips correctly
- [ ] Script longer than video → warning shown, activity clipped

### Documentation Updates
- [ ] Update `KNOWLEDGE.md` with heatmap activity algorithm
- [ ] Update `PROGRESS.md` with Phase H completion
- [ ] Update `docs/TEST_PLAN.md` with new test coverage
- [ ] Update `docs/DEPLOYMENT.md` if deployment changes
- [ ] Update `README.md` if user-facing features need documentation

### Publish
- [ ] `dotnet publish` → `E:\AIProjects\Published\AutoblowSyncPlayer`

---

## File Change Summary

| Phase | New Files | Modified Files |
|-------|-----------|----------------|
| H1    | TIMELINE_HEATMAP_PLAN.md | PLAN.md |
| H2    | Core/Models/HeatmapActivity.cs, Core/Services/IHeatmapCalculator.cs, Infrastructure/Services/HeatmapCalculator.cs, App/Controls/FunscriptHeatmap.xaml, App/Controls/FunscriptHeatmap.xaml.cs | App/ViewModels/MainViewModel.cs |
| H3    | — | App/Controls/FunscriptHeatmap.xaml.cs (tooltip, click-to-seek) |
| H4    | — | App/Views/MainWindow.xaml |
| H5    | — | KNOWLEDGE.md, PROGRESS.md, docs/* |
| Tests | tests/.../Models/HeatmapActivityTests.cs, tests/.../Infrastructure/HeatmapCalculatorTests.cs, tests/.../Controls/FunscriptHeatmapControlTests.cs | — |

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Heatmap blocks video thread | High | Use `WriteableBitmap` on background thread |
| 100K actions cause UI lag | High | Precompute on script load, bucket aggregation |
| Camera drag affects heatmap | Medium | Separate WPF controls, no overlay |
| Heatmap clicks trigger camera | Medium | Mouse events isolated to heatmap control |
| Video resize breaks layout | Medium | StackPanel + stretch alignment |
| Existing tests break | Low | No changes to Core/Infrastructure logic |

## Dependencies

- **Phase H1:** None (current phase)
- **Phase H2:** Depends on H1 plan approval
- **Phase H3:** Depends on H2 heatmap control
- **Phase H4:** Depends on H2+H3 for complete left panel
- **Phase H5:** Depends on all previous phases
