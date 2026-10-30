# VR3D Architecture — Autoblow Sync Player 3D/VR Playback Expansion

## Architectural Principles

1. **Preserve existing functionality** — No breaking changes to existing playback, sync, or UI.
2. **Single media clock** — `IVideoPlayerService.PositionMs` remains the authoritative timeline.
3. **Camera is viewing-only** — Camera state is orthogonal to playback timeline and sync engine.
4. **Layered architecture** — Core (abstractions) → Infrastructure (implementations) → App (UI).
5. **Progressive enhancement** — Start with LibVLC built-in 360°, add custom rendering only where needed.

---

## 1. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         WPF Application                         │
│                                                                 │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────────┐    │
│  │  Video View   │   │  Control UI  │   │  3D/VR Controls  │    │
│  │  (VideoView)  │   │  (existing)  │   │   (new section)  │    │
│  └──────┬───────┘   └──────┬───────┘   └────────┬─────────┘    │
│         │                  │                     │              │
│         ▼                  ▼                     ▼              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                 MainViewModel                             │   │
│  │  - Existing playback commands                              │   │
│  │  - New camera commands                                    │   │
│  │  - Video mode selector                                    │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│              AutoblowSyncPlayer.Infrastructure                   │
│                                                                 │
│  ┌─────────────────────┐  ┌──────────────────────────────┐     │
│  │ LibVlcVideoPlayer   │  │  (Optional) CustomRenderer    │     │
│  │ - UpdateViewpoint() │  │  - GPU spherical projection   │     │
│  │ - PositionMs        │  │  - Stereo eye extraction      │     │
│  │ - Existing events   │  │  - VR180 fisheye mapping      │     │
│  └──────────┬──────────┘  └──────────────────────────────┘     │
│             │                                                   │
└─────────────┼───────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│               AutoblowSyncPlayer.Core                            │
│                                                                 │
│  ┌─────────────────────┐  ┌──────────────────────────────┐     │
│  │ IVideoPlayerService │  │  ICameraController (new)      │     │
│  │ + UpdateViewpoint() │  │  IVideoModeDetector (new)     │     │
│  │ + VideoMode property│  │  IVideoRenderer (new)         │     │
│  └─────────────────────┘  └──────────────────────────────┘     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Core Layer Changes

### 2.1 New Interfaces

#### `ICameraController` (Core/Services/ICameraController.cs)
```csharp
namespace AutoblowSyncPlayer.Core.Services;

public interface ICameraController
{
    float Yaw { get; }
    float Pitch { get; }
    float Roll { get; }
    float FieldOfView { get; }
    float MouseSensitivity { get; set; }
    bool InvertVertical { get; set; }
    
    void SetViewpoint(float yaw, float pitch, float roll = 0f, float fov = -1f);
    void ResetToDefault();
    void HandleMouseDelta(float deltaX, float deltaY);
    void HandleWheelDelta(float delta);
}
```

#### `IVideoModeDetector` (Core/Services/IVideoModeDetector.cs)
```csharp
namespace AutoblowSyncPlayer.Core.Services;

public enum VideoMode
{
    Standard2D,
    Flat3dSbsLeft,
    Flat3dSbsRight,
    Flat3dOverUnderLeft,
    Flat3dOverUnderRight,
    Vr180Mono,
    Vr180Sbs,
    Vr180OverUnder,
    Vr360Mono,
    Vr360Sbs,
    Vr360OverUnder,
    Vr180Fisheye
}

public enum StereoEye
{
    Auto,
    Left,
    Right
}

public interface IVideoModeDetector
{
    VideoMode DetectMode(string filePath, VideoMetadata metadata);
    VideoMode GetModeWithFallback(VideoMode detected, VideoMode userOverride);
}

public record VideoMetadata(
    int Width,
    int Height,
    string Projection,        // "spherical", "rectangular", "unknown"
    string StereoPacking,     // "mono", "sbs", "over_under", "unknown"
    double DurationSeconds,
    string ContainerFormat
);
```

#### `IVideoRenderer` (Core/Services/IVideoRenderer.cs)
```csharp
namespace AutoblowSyncPlayer.Core.Services;

public interface IVideoRenderer
{
    VideoMode CurrentMode { get; }
    StereoEye CurrentEye { get; }
    
    Task InitializeAsync();
    Task SetModeAsync(VideoMode mode, StereoEye eye = StereoEye.Auto);
    Task UpdateViewpointAsync(float yaw, float pitch, float roll, float fov);
    Task DisposeAsync();
    
    event EventHandler<VideoModeChangedEventArgs>? ModeChanged;
}

public class VideoModeChangedEventArgs : EventArgs
{
    public VideoMode PreviousMode { get; set; }
    public VideoMode NewMode { get; set; }
}
```

### 2.2 Modified Interfaces

#### `IVideoPlayerService` — Additions
```csharp
// Existing interface, add these properties/methods:
VideoMode CurrentVideoMode { get; }
StereoEye CurrentStereoEye { get; }

Task UpdateViewpointAsync(float yaw, float pitch, float roll, float fov);
Task SetVideoModeAsync(VideoMode mode, StereoEye eye = StereoEye.Auto);
```

**Rationale:** The existing `IVideoPlayerService` is the abstraction the sync service uses. Camera viewpoint and mode changes go through this interface, keeping the sync engine unaware of 3D details.

#### `SettingsModel` — Additions
```csharp
// Camera settings
public float CameraYaw { get; set; }
public float CameraPitch { get; set; }
public float CameraFov { get; set; }
public float MouseSensitivity { get; set; } = 1.0f;
public bool InvertVerticalMouse { get; set; }
public bool EnableMovementSmoothing { get; set; }
public VideoMode LastVideoMode { get; set; } = VideoMode.Standard2D;
```

---

## 3. Infrastructure Layer Changes

### 3.1 LibVlcVideoPlayer Extensions

The existing `LibVlcVideoPlayer` will be extended to support viewpoint updates and video modes.

**Key LibVLC API:**
```csharp
// LibVLCSharp 3.9.4 — documented at docs.videolan.org
MediaPlayer.UpdateViewpoint(float yaw, float pitch, float roll, float fov, bool absolute = true);
```

**Implementation strategy:**
1. Wrap `MediaPlayer.UpdateViewpoint` in `UpdateViewpointAsync` method
2. Clamp yaw/pitch to valid ranges (VR360: ±180° yaw, ±90° pitch; VR180: ±90° yaw, ±90° pitch)
3. FOV range: 30° (zoomed in) to 120° (wide)
4. Track current viewpoint state internally

**Critical:** `UpdateViewpoint` is called on the LibVLC thread, NOT the WPF dispatcher thread. Use `Task.Run()` to avoid blocking UI.

### 3.2 Camera Controller Service

New `CameraController` class implementing `ICameraController`:

```
Responsibilities:
- Maintain current yaw, pitch, roll, FOV state
- Apply mouse sensitivity and invert settings
- Clamp angles to mode-appropriate limits (VR180 vs VR360)
- Forward viewpoint changes to IVideoPlayerService
- Provide ResetToDefault() restoring (0, 0, 0, 90)
```

**Thread safety:** Camera state updates from mouse input (UI thread) must not block the LibVLC playback thread. Use a concurrent queue or `Channel<T>` for viewpoint update commands.

### 3.3 Video Mode Detector

New `VideoModeDetector` class implementing `IVideoModeDetector`:

```
Detection Heuristic (priority order):
1. LibVLC media metadata — check for spherical projection flags
2. LibVLC media metadata — check for stereo packing flags
3. Resolution/aspect ratio heuristics (360° videos often have 2:1 aspect)
4. Filename pattern hints ("/vr360/", "_vr180_", etc.) — suggestive only
5. Default to Standard2D if nothing detected
```

### 3.4 Custom Renderer (Phase 3+)

For formats NOT supported by LibVLC's `UpdateViewpoint`:
- Flat SBS/Over-Under eye extraction
- VR180 dual-fisheye projection
- Custom equirectangular mapping with stereo eye separation

**Technology:** Evaluate Direct3D 11 via Vortice.Windows or SharpDX for GPU shader-based projection. Deferred until Phase 2 proves LibVLC limits.

---

## 4. Application Layer Changes

### 4.1 MainViewModel Extensions

New observable properties:
```csharp
[ObservableProperty] partial VideoMode _videoMode = VideoMode.Standard2D;
[ObservableProperty] partial StereoEye _stereoEye = StereoEye.Auto;
[ObservableProperty] partial float _cameraYaw;
[ObservableProperty] partial float _cameraPitch;
[ObservableProperty] partial float _cameraFov;
[ObservableProperty] partial bool _cameraModeEnabled;
[ObservableProperty] partial float _mouseSensitivity = 1.0f;
[ObservableProperty] partial bool _invertVertical;
```

New commands:
```csharp
[RelayCommand] void ResetCamera()
[RelayCommand] void ToggleCameraMode()
[RelayCommand] void SetVideoMode(VideoMode mode)
[RelayCommand] void SetStereoEye(StereoEye eye)
[RelayCommand] void OnMouseDelta(float deltaX, float deltaY)
[RelayCommand] void OnWheelDelta(float delta)
```

### 4.2 MainWindow XAML Changes

New `GroupBox` in the right control panel:
```
3D / VR Controls
├── Video Mode: [ComboBox]
├── Stereo Eye: [Left/Right RadioButtons] (visible when applicable)
├── Mouse Navigation: [Enabled Indicator]
├── Reset View: [Button]
├── FOV: [Slider 30°—120°]
├── Sensitivity: [Slider 0.1—3.0]
├── Invert Vertical: [CheckBox]
└── Current View: [Yaw: ___° Pitch: ___°] (read-only)
```

**Mouse input wiring:**
- `videoView` control gets `MouseLeftButtonDown`, `MouseMove`, `MouseWheel` handlers
- Handlers pass delta to `MainViewModel.OnMouseDelta` / `OnWheelDelta`
- Input only active when `_cameraModeEnabled` is true (VR180/VR360 mode)

### 4.3 Fullscreen Handling

Fullscreen mode (`ToggleFullscreen` existing command) already uses LibVLC's fullscreen. Camera controls must work in fullscreen:
- Keyboard shortcuts (arrow keys, R, +/-, Escape) handled at window level
- Mouse input routed to camera controller in fullscreen
- `Escape` key exits fullscreen (existing behavior preserved)

---

## 5. Synchronization Preservation

### 5.1 Single Clock Architecture

```
┌─────────────────────────┐
│   LibVlcVideoPlayer      │
│   PositionMs (master)   │
│   DurationMs            │
│   IsPlaying             │
└─────────┬───────────────┘
          │
     ┌────┴────────────────────────────────┐
     │                                     │
     ▼                                     ▼
┌──────────────────┐              ┌──────────────────┐
│ SyncService       │              │ CameraController │
│ (existing)        │              │ (new)            │
│ - Reads PositionMs│              │ - Reads PositionMs│
│ - Sends device   │              │   (for mode detection)
│   commands       │              │ - Updates viewport │
│ - Timer-based    │              │   independently   │
└──────────────────┘              └──────────────────┘
```

**Key isolation:**
- `SyncService` reads `PositionMs` → maps to funscript timeline → sends device commands
- `CameraController` reads video mode → handles mouse input → calls `UpdateViewpoint` on MediaPlayer
- Camera controller does NOT call `SeekAsync`, `PauseAsync`, `PlayAsync`, or any sync-related method
- Viewpoint updates do not change `PositionMs` value (verified in Phase 2)

### 5.2 Event Isolation

Existing events on `IVideoPlayerService`:
- `PositionChanged` — Sync service only. Camera controller does NOT subscribe.
- `PlaybackEnded` — Sync service only.
- `IsPlayingChanged` — Sync service only.

New events:
- `ViewpointChanged` — Camera UI only. Sync service does NOT subscribe.
- `VideoModeChanged` — UI only. Sync service does NOT subscribe.

---

## 6. Development Phases

### Phase 1 (Current) — Audit and Planning
- ✅ Audit complete
- ✅ Documentation created

### Phase 2 — 360° Video and Camera PoC
- Verify `MediaPlayer.UpdateViewpoint` works with project LibVLC version
- Implement `ICameraController` + `CameraController`
- Add basic VR360 Mono mode
- Wire mouse input to camera controller
- Verify sync isolation

### Phase 3 — VR180 and Stereoscopic Playback
- Implement SBS/Over-Under eye extraction
- Implement VR180 equirectangular clamping
- Video mode detector
- Mode switching without reload

### Phase 4 — Full Camera UI and Fullscreen
- 3D/VR Controls UI section
- Fullscreen camera integration
- Settings persistence
- Keyboard controls

### Phase 5 — Funscript Sync Integration Tests
- Comprehensive sync isolation tests
- Camera movement during playback (mock device)
- Regression test suite

### Phase 6 — Performance Optimization
- GPU rendering evaluation
- Memory profiling
- Resource cleanup verification

### Phase 7 — Final Testing and Release
- Full test suite
- Release build
- Documentation updates

---

## 7. Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| `UpdateViewpoint` doesn't work with project LibVLC version | Phase 2 PoC verifies before committing to architecture |
| Camera updates block playback thread | Use `Task.Run()` and async queue for viewpoint commands |
| Mouse input stolen by WPF controls | Use `e.Handled = true` and explicit routing in video viewport |
| Fullscreen breaks camera controls | Keyboard shortcuts at window level, not video control level |
| LibVLC native libs not found for 360° | Same native libs used for standard playback; already working |
| Sync engine triggered by camera movement | Architectural isolation: camera controller has no sync method access |

---

## 8. Dependencies

### Existing (No Changes)
- .NET 10 runtime
- LibVLCSharp 3.9.4
- VideoLAN.LibVLC.Windows 3.0.21
- CommunityToolkit.Mvvm 8.4.0
- Serilog

### Potential New (Phase 3+)
- **Vortice.Windows** — Direct3D 11 interop for custom GPU rendering (if needed)
- **SharpDX** — Alternative D3D11 wrapper (if Vortice insufficient)

**Decision point:** Custom renderer packages added only after Phase 2 proves LibVLC limits.
