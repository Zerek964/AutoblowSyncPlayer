# VR3D Requirements — Autoblow Sync Player 3D/VR Playback Expansion

## Scope

Extend the existing Autoblow Sync Player with immersive 3D/VR video playback and interactive mouse-controlled camera, while preserving all existing functionality and funscript synchronization.

**Critical Constraint:** Camera movement must NEVER interrupt video playback or funscript device synchronization.

---

## 1. Video Format Support

### 1.1 Existing Formats (Preserve)
| Format | Behavior |
|---|---|
| Standard 2D MP4 | Preserve existing LibVLC playback unchanged |
| Standard 2D MKV | Preserve existing LibVLC playback where supported |

### 1.2 New Immersive Formats
| Format | Required Behavior | Priority |
|---|---|---|
| Flat 3D SBS (Side-by-Side) | Select left or right eye, display correctly proportioned mono view | P1 |
| Flat 3D Over-Under (Top/Bottom) | Select left or right eye, display correctly proportioned mono view | P1 |
| VR180 Equirectangular Mono | Interactive 180° viewpoint with camera yaw/pitch | P1 |
| VR360 Equirectangular Mono | Interactive 360° viewpoint with camera yaw/pitch | P1 |
| VR180 SBS | Interactive viewpoint with stereo eye separation | P2 |
| VR360 SBS | Interactive 360° viewpoint with stereo eye handling | P2 |
| VR180 Over-Under | Interactive viewpoint with stereo eye handling | P2 |
| VR360 Over-Under | Interactive 360° viewpoint with stereo eye handling | P2 |
| VR180 Dual-Fisheye | Implement after verifying correct lens mapping, or clearly label as unsupported | P3 |

### 1.3 Format Distinctions
- **SBS does not imply VR.** Flat SBS videos are stereoscopic but not spherical.
- **Selecting one eye produces a mono view on desktop, not stereoscopic depth.**
- **VR180/VR360 require spherical projection**, not simple cropping.
- **Fisheye requires specific lens/projection mapping.**
- Do not mark a format "supported" until geometry and rendering are verified.

---

## 2. Interactive Camera System

### 2.1 Mouse Controls
| Action | Behavior |
|---|---|
| Left-click + drag left/right | Rotate camera yaw (horizontal) |
| Left-click + drag up/down | Rotate camera pitch (vertical) |
| Mouse wheel up | Zoom in (decrease FOV) |
| Mouse wheel down | Zoom out (increase FOV) |
| Double-click video | Toggle fullscreen |

### 2.2 Keyboard Controls
| Key | Behavior |
|---|---|
| R | Reset camera to default viewpoint |
| Arrow left/right | Rotate yaw |
| Arrow up/down | Rotate pitch |
| + / - | Zoom in/out |
| Escape | Exit fullscreen |

### 2.3 Camera Settings
- Adjustable mouse sensitivity (slider or numeric input)
- Optional "Invert Vertical Mouse Movement" toggle
- Horizontal angle (yaw) display
- Vertical angle (pitch) display
- Field of view (FOV) control
- Reset camera button
- Optional movement smoothing

### 2.4 Camera Behavior During Playback
**Critical:** Camera is a viewing function independent of playback timeline.

| User Action | Video | Audio | Playback Clock | Funscript | Device |
|---|---|---|---|---|---|
| Rotate camera | Continues | Continues | Advances | Continues | Connected |
| Zoom camera | Continues | Continues | Advances | Continues | Connected |
| Reset camera | Continues | Continues | Advances | Continues | Connected |
| Switch stereo eye | Continues (if possible) | Continues | Advances | Continues | Connected |
| Pause video | Pauses | Pauses | Holds | Pauses | Stops |
| Resume video | Resumes | Resumes | Advances | Resyncs | Starts |
| Seek video | Seeks | Seeks | Jumps | Resyncs | Updates |
| Stop video | Stops | Stops | Holds | Stops | Stops |
| Video ends | Ends | Ends | Holds | Stops | Stops |

Camera movement must NOT trigger:
- Script reuploads
- Playback restarts
- Video seeks
- Device stop/start commands
- Excessive API state requests

### 2.5 Camera Behavior While Paused
- Video remains paused
- Device script remains stopped (existing behavior)
- Camera rotation allowed on current frame (if renderer supports)
- Camera movement does not resume playback

### 2.6 Clamping
- VR360: Full 360° horizontal, appropriate vertical limits
- VR180: 180° horizontal, appropriate vertical limits — do not show unrecorded areas
- Standard 2D: No camera rotation (existing playback)

---

## 3. Video Mode Selector UI

### 3.1 Mode Options
```
Auto Detect
Standard 2D
Flat 3D SBS — Left Eye
Flat 3D SBS — Right Eye
Flat 3D Over-Under — Left Eye
Flat 3D Over-Under — Right Eye
VR180 Mono
VR180 SBS
VR180 Over-Under
VR360 Mono
VR360 SBS
VR360 Over-Under
VR180 Fisheye (when supported)
```

### 3.2 Auto Detection
- Inspect media metadata for spherical projection flags, stereo packing, resolution, aspect ratio
- Use filename hints as suggestions only, never as proof
- Allow manual override when detection is ambiguous
- Do not auto-select a mode that the renderer does not support

### 3.3 Mode Switching
- Switch display modes without reloading video file
- Switch modes without re-uploading funscript
- Preserve the same media clock across mode switches
- Document any mode transitions that cannot be seamless

---

## 4. Rendering Architecture Requirements

### 4.1 Media Engine
- Reuse existing LibVLCSharp 3.9.4 + VideoLAN.LibVLC.Windows 3.0.21
- Leverage `MediaPlayer.UpdateViewpoint(yaw, pitch, roll, fov, absolute)` for 360° video
- Verify `UpdateViewpoint` works with project's actual LibVLC version
- Do not assume `UpdateViewpoint` handles SBS, VR180, or fisheye natively

### 4.2 Single Media Clock
- One authoritative playback clock (existing `PositionMs` from `IVideoPlayerService`)
- Camera state and script state remain separate
- Sync service continues to read from the same clock
- No independent timeline for 3D renderer

### 4.3 GPU Acceleration
- Prefer GPU-accelerated projection where possible
- Avoid per-frame managed memory copies of full decoded frames
- Maintain hardware-accelerated video decoding
- Properly release GPU resources on media change and app exit

### 4.4 WPF Integration
- Mouse events must reach camera controller through `vlc:VideoView`
- Fullscreen mouse interaction must work
- No transparent overlays breaking mouse input
- Camera movement does not steal input from other UI controls
- Video visible during window resize

---

## 5. UI Requirements

### 5.1 3D/VR Controls Group
Collapsible group in the main window control panel containing:
- Video mode selector (dropdown)
- Left/Right eye toggle (when applicable)
- Mouse navigation enabled indicator
- Reset View button
- FOV slider
- Mouse sensitivity slider
- Invert Y toggle
- Optional current yaw/pitch readout

### 5.2 Existing UI Preservation
- All existing video/funscript/device controls remain visible and functional
- Settings dialog unchanged (camera settings added in separate section)
- Status bar continues showing playback and device state
- Menu bar functionality preserved

### 5.3 Fullscreen
- Camera rotation and zoom work in fullscreen
- Escape exits fullscreen
- Keyboard shortcuts work in fullscreen
- Returning to window preserves playback position

---

## 6. Performance Requirements

### 6.1 Baseline
- Interactive camera movement responsive on test hardware
- Stable video and audio playback during camera movement
- No excessive CPU usage from camera rotation
- No per-frame managed allocations causing GC pressure
- Stable video/audio timing

### 6.2 Resource Management
- Proper disposal of LibVLC media and GPU resources
- No resource leaks after repeated video loads
- No stale media objects after playback stop

### 6.3 Resolutions
- Test and document: 1080p, 4K (where samples available)
- Do not claim support for resolutions not tested

---

## 7. Testing Requirements

### 7.1 Automated Tests
- Camera movement does not trigger sync events (mocked device)
- Video mode switching preserves media clock
- Format detection logic (metadata parsing)
- Camera angle clamping (VR180 vs VR360 limits)
- Settings persistence for camera preferences
- Regression: all existing 89 tests must continue passing

### 7.2 Manual Visual Tests
- 360° video renders correctly with mouse-controlled camera
- Audio continues during camera movement
- Fullscreen camera controls work
- Stereoscopic formats display correct eye view
- Mode switching works without reload

### 7.3 Integration Tests
- Camera movement during funscript playback (mock device)
- Sync service unaffected by camera state changes
- Script upload not triggered by camera actions

---

## 8. Non-Goals (Out of Scope)

- VR headset (HMD) support
- True stereoscopic 3D output on desktop (anaglyph, etc.)
- 3D mesh/model rendering (OBJ, FBX, GLB)
- Multi-display support
- Streaming from network sources
- Real-device movement testing in automated tests
