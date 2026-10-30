# VR3D Test Plan — Autoblow Sync Player 3D/VR Playback Expansion

## Test Strategy

All existing tests (89 tests) must continue passing. New tests cover camera system, video modes, sync isolation, and format detection. Real device testing is NOT required for automated tests — mock device client is sufficient.

---

## 1. Regression Tests (Existing)

### 1.1 Existing Test Suite
| Test File | Tests | Status |
|---|---|---|
| `FunscriptServiceTests.cs` | 17 | Must pass |
| `MockAutoblowDeviceClientTests.cs` | 16 | Must pass |
| `SettingsServiceTests.cs` | 14 | Must pass |
| `SyncServiceTests.cs` | 19 | Must pass |
| `FunscriptModelTests.cs` | 17 | Must pass |
| `SettingsModelTests.cs` | 6 | Must pass |

**Verification:** Run `dotnet test` after every VR3D code change. If any existing test fails, stop and fix before proceeding.

---

## 2. Camera Controller Tests

### 2.1 `CameraControllerTests.cs` (New)

| Test | Condition | Expected Result |
|---|---|---|
| `Constructor_DefaultValues` | New instance | Yaw=0, Pitch=0, Roll=0, FOV=90 |
| `SetViewpoint_UpdatesAllAngles` | Set yaw=45, pitch=-30 | Properties updated |
| `SetViewpoint_ClampsYaw360` | Set yaw=200 (VR360) | Clamped to ±180 |
| `SetViewpoint_ClampsYaw180` | Set yaw=100 (VR180) | Clamped to ±90 |
| `SetViewpoint_ClampsPitch` | Set pitch=100 | Clamped to ±90 |
| `SetViewpoint_InvalidFov` | Set FOV=10 | Clamped to min (30) |
| `SetViewpoint_ExcessiveFov` | Set FOV=200 | Clamped to max (120) |
| `HandleMouseDelta_AppliesSensitivity` | Delta=10, sensitivity=2 | Movement = 20 units |
| `HandleMouseDelta_InvertedVertical` | DeltaY=10, invert=true | Pitch decreases |
| `HandleMouseDelta_NormalVertical` | DeltaY=10, invert=false | Pitch increases |
| `HandleWheelDelta_ZoomIn` | Positive delta | FOV decreases |
| `HandleWheelDelta_ZoomOut` | Negative delta | FOV increases |
| `ResetToDefault_ResetsState` | After various changes | Back to 0,0,0,90 |
| `MouseSensitivity_Bounds` | Set to 0.05 | Clamped to min (0.1) |
| `MouseSensitivity_Excessive` | Set to 5.0 | Clamped to max (3.0) |

---

## 3. Sync Isolation Tests

### 3.1 `SyncIsolationTests.cs` (New)

**Critical:** These tests verify that camera movement does NOT trigger sync engine actions.

| Test | Condition | Expected Result |
|---|---|---|
| `CameraMovement_DoesNotTriggerPositionChanged` | Camera controller updates viewpoint | Sync service `PositionChanged` not fired |
| `CameraMovement_DoesNotTriggerSeek` | Multiple camera updates during playback | No `SeekAsync` calls on video player |
| `CameraMovement_DoesNotTriggerPause` | Camera updates while playing | `IsPlaying` remains true |
| `CameraMovement_DoesNotTriggerScriptUpload` | Camera updates during synced playback | Mock device `UploadScriptAsync` not called |
| `CameraMovement_DoesNotTriggerDeviceStart` | Camera updates during playback | Mock device `StartScriptAsync` not called |
| `CameraMovement_DoesNotTriggerDeviceStop` | Camera updates during playback | Mock device `StopScriptAsync` not called |
| `ViewpointUpdate_DoesNotChangePositionMs` | Update viewpoint, check PositionMs | PositionMs unchanged |
| `ViewpointUpdate_DoesNotChangeDurationMs` | Update viewpoint, check DurationMs | DurationMs unchanged |
| `VideoModeSwitch_DoesNotRestartPlayback` | Switch VR360→VR180→2D | Playback continues, PositionMs advancing |
| `SyncService_UnaffectedByCameraEvents` | Camera fires ViewpointChanged | Sync service state unchanged |

**Implementation:** Use `Mock<IVideoPlayerService>` and `Mock<IAutoblowDeviceClient>` to verify no sync-related method calls occur during camera operations.

---

## 4. Video Mode Tests

### 4.1 `VideoModeDetectorTests.cs` (New)

| Test | Condition | Expected Result |
|---|---|---|
| `DetectMode_SphericalMetadata` | Metadata has projection="spherical" | Returns VR360Mono |
| `DetectMode_SbsMetadata` | Metadata has stereoPacking="sbs" | Returns Flat3dSbs or VR mode |
| `DetectMode_OverUnderMetadata` | Metadata has stereoPacking="over_under" | Returns Flat3dOu or VR mode |
| `DetectMode_RectangularProjection` | Metadata has projection="rectangular" | Returns Standard2D |
| `DetectMode_UnknownMetadata` | All fields "unknown" | Returns Standard2D |
| `DetectMode_FilenameHintOnly` | Filename contains "vr360" but no metadata | Returns Standard2D (hints not authoritative) |
| `GetModeWithFallback_UserOverride` | User selects VR180Mono | Returns VR180Mono |
| `GetModeWithFallback_NoOverride` | User override = Standard2D, detected = VR360 | Returns detected (VR360) |

### 4.2 `VideoModeEnumsTests.cs` (New)

| Test | Condition | Expected Result |
|---|---|---|
| `VideoMode_AllValuesValid` | Enum iteration | All expected modes present |
| `StereoEye_AllValuesValid` | Enum iteration | Auto, Left, Right present |
| `VideoMode_IsSbs_HandlesCorrectly` | SBS modes return true for IsSbs() | Extension method works |
| `VideoMode_IsOverUnder_HandlesCorrectly` | OU modes return true | Extension method works |
| `VideoMode_IsVr360_HandlesCorrectly` | VR360 modes identified | Extension method works |
| `VideoMode_IsVr180_HandlesCorrectly` | VR180 modes identified | Extension method works |

---

## 5. LibVLC Integration Tests

### 5.1 `LibVlcVideoPlayerViewpointTests.cs` (New)

| Test | Condition | Expected Result |
|---|---|---|
| `UpdateViewpointAsync_CallsMediaPlayer` | Valid yaw/pitch | MediaPlayer.UpdateViewpoint called |
| `UpdateViewpointAsync_HandlesNullMediaPlayer` | MediaPlayer is null | No exception, graceful no-op |
| `UpdateViewpointAsync_ThreadSafety` | Concurrent updates | No deadlocks, no crashes |
| `ViewpointState_TrackedInternally` | After update | Internal state matches last call |

### 5.2 `LibVlcVideoPlayerRegressionTests.cs` (New)

| Test | Condition | Expected Result |
|---|---|---|
| `PositionMs_UnchangedByViewpoint` | Update viewpoint, read PositionMs | Same value |
| `IsPlaying_UnchangedByViewpoint` | Update viewpoint while playing | IsPlaying still true |
| `Events_NotFiredByViewpoint` | Update viewpoint | PositionChanged not fired |
| `LoadVideoAsync_ClearsViewpoint` | Load new video | Viewpoint resets to defaults |

---

## 6. Settings Tests

### 6.1 `SettingsModelCameraTests.cs` (New)

| Test | Condition | Expected Result |
|---|---|---|
| `CameraSettings_DefaultValues` | New SettingsModel | Sensitivity=1.0, Invert=false |
| `CameraSettings_Serialization` | Save/load round-trip | Values preserved |
| `VideoMode_Serialization` | Save VR360Mono, load | Mode preserved |
| `CameraAngle_Serialization` | Save yaw/pitch/fov | Values preserved |

### 6.2 `SettingsServiceCameraTests.cs` (New)

| Test | Condition | Expected Result |
|---|---|---|
| `SaveSettings_PersistsCameraSettings` | Save with camera config | File contains camera JSON |
| `LoadSettings_RestoresCameraSettings` | Load from file with camera config | Properties match saved values |

---

## 7. MainViewModel Tests

### 7.1 `MainViewModelCameraTests.cs` (New)

| Test | Condition | Expected Result |
|---|---|---|
| `ResetCamera_CommandExecutes` | Execute ResetCameraCommand | Camera at default values |
| `SetVideoMode_UpdatesProperty` | Set mode to VR360Mono | VideoMode property updated |
| `SetVideoMode_CameraModeEnabled` | Set to VR360Mono | CameraModeEnabled = true |
| `SetVideoMode_CameraModeDisabled` | Set to Standard2D | CameraModeEnabled = false |
| `OnMouseDelta_ForwardedToController` | Mouse delta event | Camera controller received delta |
| `OnWheelDelta_ForwardedToController` | Wheel event | Camera controller received delta |
| `OnMouseDelta_IgnoresWhenCameraDisabled` | Camera mode off | No controller calls |

---

## 8. Manual Visual Tests (Non-Automated)

### 8.1 VR360 Playback
- [ ] Load a 360° equirectangular MP4
- [ ] Video renders as spherical projection (not flat stretched image)
- [ ] Mouse drag rotates view smoothly
- [ ] Mouse wheel zooms in/out
- [ ] Audio plays continuously during camera movement
- [ ] Playback timer advances during camera movement
- [ ] Reset camera (R key) restores default view

### 8.2 VR180 Playback
- [ ] Load a VR180 equirectangular MP4
- [ ] Video renders as 180° projection
- [ ] Camera clamped to 180° field (no black void beyond edges)
- [ ] Mouse drag works within 180° bounds

### 8.3 Stereoscopic Playback
- [ ] Load SBS video, select left eye → correct left image shown
- [ ] Load SBS video, select right eye → correct right image shown
- [ ] Load Over-Under video, select left eye → correct top/bottom image
- [ ] Aspect ratio correct (not stretched)

### 8.4 Fullscreen
- [ ] Enter fullscreen → video fills screen
- [ ] Mouse drag works in fullscreen
- [ ] Keyboard controls work in fullscreen
- [ ] Escape exits fullscreen, playback position preserved

### 8.5 Sync During Camera Movement (Mock Device)
- [ ] Load video + funscript, start synced playback
- [ ] Move camera continuously during playback
- [ ] Device executes script actions at correct timestamps
- [ ] No script reupload detected in logs
- [ ] No device disconnect during camera movement

---

## 9. Performance Tests

### 9.1 CPU and Memory
- [ ] CPU usage during camera movement < 50% on test machine (single core equivalent)
- [ ] No GC spikes during continuous camera rotation (1 min test)
- [ ] Memory stable after 5 video load/play/stop cycles
- [ ] No memory leak after 30 min continuous playback with camera movement

### 9.2 Resolution Tests (where samples available)
- [ ] 1080p VR360 playback stable
- [ ] 4K VR360 playback tested (if sample available)
- [ ] Frame rate maintained during camera movement

---

## 10. Test Execution Order

1. **After each code change:** `dotnet test` — verify all tests pass
2. **After Phase 2 (PoC):** Manual VR360 visual tests
3. **After Phase 3 (Stereo):** Manual stereoscopic visual tests
4. **After Phase 4 (UI):** Full manual test suite
5. **After Phase 5 (Sync):** All sync isolation tests + manual sync tests
6. **After Phase 6 (Performance):** Performance benchmark suite
7. **After Phase 7 (Release):** Complete regression + visual suite

---

## 11. Test Targets

| Category | Automated Tests (Target) | Manual Tests |
|---|---|---|
| Camera Controller | ~15 | — |
| Sync Isolation | ~10 | ~5 |
| Video Mode Detection | ~10 | ~3 |
| LibVLC Integration | ~10 | ~10 |
| Settings | ~8 | — |
| ViewModel | ~7 | ~5 |
| Performance | ~5 (automated benchmarks) | ~10 |
| **Total** | **~65 new tests** | **~33 manual** |

**Combined total:** 89 existing + 65 new = **154 automated tests**
