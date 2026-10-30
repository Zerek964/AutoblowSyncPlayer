# KNOWLEDGE.md — Autoblow Sync Player

## Technical Findings

### K-001: LibVLCSharp Initialization
- **Finding**: LibVLCSharp 3.9.4 does NOT require `Core.Initialize()`. Create LibVLC via `new LibVLC()` directly.
- **Source**: Official LibVLCSharp documentation
- **Verified**: Yes, tested in LibVlcVideoPlayer

### K-002: LibVLC Native Directory
- **Finding**: `core.CurrentDirectory` must be set to the LibVLC native package path **before** creating LibVLC instance.
- **Source**: LibVLCSharp WPF samples
- **Verified**: Yes, implemented in LibVlcVideoPlayer constructor

### C-003: LibVLCSharp Event Namespaces
- **Finding**: Events (PositionChanged, TimeChanged, LengthChanged, EncounteredError, Ended) are in `LibVLCSharp.Shared` namespace.
- **Source**: LibVLCSharp API reference
- **Verified**: Yes, used in LibVlcVideoPlayer

### K-004: .NET 10 Target Frameworks
- **Finding**: Library projects use `net10.0`, WPF App project uses `net10.0-windows`.
- **Source**: Microsoft .NET documentation
- **Verified**: Yes, confirmed in csproj files

### K-005: CommunityToolkit.Mvvm Usage
- **Finding**: `[ObservableObject]` with `[ObservableProperty]` provides minimal boilerplate MVVM.
- **Source**: Microsoft docs
- **Verified**: Yes, used in all ViewModels

### K-006: DPAPI Platform Warning
- **Finding**: `ProtectedData.Protect/Unprotect` generates CA1416 warnings because DPAPI is Windows-only.
- **Impact**: Expected and acceptable — the app targets Windows only.
- **Verified**: Yes, 4 warnings in DpapiCredentialStorage.cs

### K-007: Autoblow Cluster Discovery
- **Finding**: Device cluster must be discovered via `GET /autoblow/connected` — never hardcoded.
- **Source**: Official Autoblow API docs
- **Verified**: Implemented in AutoblowDeviceClient.CheckConnectionAsync

### K-008: Autoblow Device Type Verification
- **Finding**: `deviceType` must be checked before enabling device control. Only `autoblow-ultra` is supported.
- **Source**: Official Autoblow API docs
- **Verified**: Implemented in AutoblowDeviceClient

### K-009: Funscript Upload Method
- **Finding**: Funscript upload uses `PUT /autoblow/sync-script/upload-funscript` with `multipart/form-data` and file part named `file`.
- **Source**: Official Autoblow API docs
- **Verified**: Implemented in AutoblowDeviceClient

### K-010: Funscript Upload Timing
- **Finding**: Allow at least 60 seconds for upload and loading. Wait for device acknowledgment.
- **Source**: Official Autoblow API docs
- **Strategy**: Poll device state until loading complete or timeout

### K-011: LibVLC Deadlock Prevention
- **Finding**: LibVLC event handlers must NOT read native properties like `_mediaPlayer.Time` or `_mediaPlayer.Length`. These are blocking P/Invoke calls that deadlock when `Stop()` holds internal LibVLC locks.
- **Root Cause**: Race condition — thread passes `_eventsEnabled` gate, then blocks on native read while `Stop()` waits for threads to settle → deadlock.
- **Fix**: Use values from event args (`MediaPlayerTimeChangedEventArgs.Time`, `MediaPlayerLengthChangedEventArgs.Length`) instead. These are non-blocking values passed by LibVLC in the callback.
- **Verified**: Yes, fixed in LibVlcVideoPlayer.SubscribeToLibVlcEvents() — 235/235 tests pass

### K-012: Sync Script Stop — No Body
- **Finding**: `PUT /autoblow/sync-script/stop` requires NO JSON body.
- **Source**: Official Autoblow API docs
- **Verified**: Implemented in AutoblowDeviceClient

### K-013: Sync Engine Deadband
- **Finding**: Deadband of 2000ms (default) prevents unnecessary sync corrections for minor drift.
- **Strategy**: Only adjust device if drift exceeds deadband threshold
- **Verified**: 30 unit tests in SyncServiceTests

### K-014: Funscript Validation Limits
- **Finding**: Max 100,000 actions, max timestamp 16,777,214 ms, positions 0-100.
- **Source**: Official Autoblow API docs
- **Verified**: 23 validation tests in FunscriptFileValidatorTests

### K-015: Token Security
- **Finding**: Device token must never be logged, committed to Git, or sent to telemetry.
- **Strategy**: Store via Windows DPAPI (current user), mask in UI
- **Verified**: DpapiCredentialStorage with 14 tests

### K-016: API Error Handling
- **Finding**: Handle 400, 401, 403, 429, 502, timeouts, and malformed responses.
- **Strategy**: HttpClient with timeouts, structured exceptions, no infinite retries
- **Verified**: Implemented in AutoblowDeviceClient

### K-017: Package Versions
| Package | Version |
|---|---|
| LibVLCSharp | 3.9.4 |
| LibVLCSharp.WPF | 3.9.4 |
| VideoLAN.LibVLC.Windows | 3.0.21 |
| CommunityToolkit.Mvvm | 8.4.0 |
| Microsoft.Extensions.DependencyInjection | 10.0.0 |
| xUnit | 2.9.3 |
| FluentAssertions | 8.1.0 |
| Moq | 4.20.72 |

### K-018: Same-Name Funscript Detection
- **Finding**: User setting to auto-detect `.funscript` with same base name as MP4.
- **Strategy**: Check for `{videoName}.funscript` in same directory when loading video
- **Verified**: Implemented in MainWindow

---

## VR3D Technical Findings

### K-019: LibVLCSharp 360° Video Camera Control
- **Finding**: `MediaPlayer.UpdateViewpoint(float yaw, float pitch, float roll, float fov, bool absolute = true)` controls camera orientation for 360° video playback.
- **Source**: LibVLCSharp 3.9.4 API, verified via documentation and source code review.
- **Verified**: API exists and is accessible on MediaPlayer instances.
- **Notes**: Yaw/pitch/roll are in degrees. FOV is in degrees (default ~70-90°). Absolute mode sets absolute position; relative mode offsets from current position.

### K-020: LibVLC Native 360° Support
- **Finding**: LibVLC 3.0+ has built-in 360° video projection via the `projection` video filter. Setting `--input-slave=projection=sphere` enables 360° rendering.
- **Source**: LibVLC documentation, vlc-project.org
- **Verified**: LibVLC 3.0.21 (VideoLAN.LibVLC.Windows package) supports this.
- **Notes**: For VR180 and stereoscopic 3D, additional processing may be needed beyond native LibVLC filters.

### K-021: Stereoscopic Video Modes
- **Finding**: Common stereoscopic formats include side-by-side (SBS) horizontal/vertical, top-bottom, and anaglyph (red-cyan).
- **Source**: VR video format documentation.
- **Strategy**: Implement video effects to crop/deinterlace SBS content for each eye, or use LibVLC's `deinterleave` and `crop` filters.
- **Notes**: GPU-based rendering preferred for performance, especially at high resolutions.

### K-022: Single Clock Architecture for Sync Preservation
- **Finding**: Video player `PositionMs` must remain the single source of truth for sync timing.
- **Strategy**: Camera controller and video mode detector must NEVER call sync-related methods or affect video timeline.
- **Notes**: Architectural isolation enforced by interface design (camera controller has no sync dependencies).

### K-023: WPF + LibVLC Rendering Constraints
- **Finding**: WPF uses `vlc:VideoView` for rendering LibVLC output. Custom video effects (for 3D/VR) may require direct LibVLC video filter configuration rather than WPF-level rendering.
- **Source**: LibVLCSharp WPF documentation, project source code review.
- **Notes**: For complex 3D rendering, consider using LibVLC's video output filters (`--vout-filter`) rather than trying to compose in WPF.

### K-024: Thread Safety for Camera Controls
- **Finding**: Mouse events fire on UI thread; LibVLC `UpdateViewpoint` should be called on the same thread as the MediaPlayer was created (typically UI thread in WPF).
- **Strategy**: Camera controller runs on UI thread, directly or via dispatcher.
- **Notes**: Avoid background threads for camera updates to prevent cross-thread issues with LibVLC.

### K-025: Video Mode Detection Heuristics
- **Finding**: 360° video metadata (SPHERE, VR180) can be detected via video streams properties in LibVLC (`MediaPlayer.Media.Tracks`). Resolution aspect ratio can hint at SBS content (e.g., 3:2 or 6:4 aspect often indicates SBS).
- **Source**: LibVLC Media.TrackInfo, VR video format specs.
- **Notes**: Auto-detection may require manual override UI for edge cases.

### K-019: Settings Persistence
- **Finding**: Non-sensitive settings saved to `%APPDATA%\AutoblowSyncPlayer\settings.json`.
- **Strategy**: JSON serialization with recovery for malformed files
- **Verified**: 10 tests in SettingsServiceTests

### K-020: Playback Speed
- **Finding**: Playback speed fixed at 1x for initial release (sync accuracy requirement).

### K-026: LibVLC media.Parse() Is Async
- **Finding**: `media.Parse()` returns immediately without waiting for metadata. Reading `media.Tracks` immediately after Parse returns null/empty.
- **Strategy**: Use `MediaPlayer.Play()` + `Vout` event to wait for video output initialization, then read `media.Tracks`.
- **Verified**: Fixed VideoModeDetector to use MediaPlayer+Vout pattern with 10-second timeout via `Task.WhenAny`.
- **Notes**: `completedTask.Wait()` blocks synchronously because detection runs outside async context.

### K-027: CommunityToolkit.Mvvm Property Change Handlers
- **Finding**: `partial void On[Property]Changed(Type value)` provides the right hook for fire-and-forget async side-effects on property changes.
- **Strategy**: Use `OnVideoModeChanged` to call `ApplyVideoModeAsync` instead of making the command itself async.
- **Verified**: Fixed ComboBox selection not applying video mode.
- **Notes**: Fire-and-forget pattern avoids double-calling when both command and property handler try to do the same work.

### K-028: WPF ComboBox Dark Theme
- **Finding**: ComboBox dropdown items don't inherit dark theme colors from parent window by default.
- **Strategy**: Add explicit `Style TargetType="ComboBox"` in App.xaml with correct `Background` and `Foreground`.
- **Verified**: Added style using `BackgroundTertiaryBrush` (#2D2D30) and `TextBrush` (#FFFFFF).

### K-029: Funscript Timeline Heatmap Algorithm
- **Finding**: Heatmap visualizes script activity intensity per time bucket using speed-based aggregation.
- **Strategy**: For each time bucket, calculate `speed = |pos2 - pos1| / (at2 - at1)` from action pairs. Normalize using 95th percentile clamping to avoid outlier saturation. Render as `WriteableBitmap` gradient from dark blue (low) → green → yellow → red (high).
- **Source**: `HeatmapCalculator.cs` — bucket aggregation with speed calculation.
- **Verified**: 21 unit tests in `HeatmapCalculatorTests.cs` covering edge cases (empty script, single action, zero duration, exceeding video, all-zero positions, normalization).
- **Notes**: Render resolution is configurable (default 100 buckets). `FunscriptHeatmap` control handles hover tooltips ("~X ms") and click-to-seek via `SeekRequested` event.

### K-021: Test Architecture
- **Finding**: 6 test files, 89 total tests (80 `[Fact]` + 1 `[Theory]` with 9 `[InlineData]`).
- **Strategy**: Core logic tested in isolation; infrastructure mocked via Moq
- **Verified**: All tests pass in both Debug and Release

### K-022: Synchronization Strategy
- **Finding**: Sync engine polls device state every N seconds (configurable, default 5000ms).
- **Strategy**: Compare video clock to device clock, apply offset only if drift exceeds deadband.
- **Verified**: 30 tests covering pause/resume/seek/drift scenarios

### K-023: LibVLC Native Deployment (Single-File Publish)
- **Finding**: Single-file publish (`PublishSingleFile=true`) strips `Content` items from NuGet packages. LibVLC native DLLs (libvlc.dll, libvlccore.dll, plugins/, lua/, etc.) disappear from publish output.
- **Fix**: MSBuild `AfterTargets="Publish"` target explicitly copies all natives from `$(NuGetPackageRoot)videolan.libvlc.windows\3.0.21\build\x64\**\*.*` to publish output `libvlc\win-x64\%(RecursiveDir)%(Filename)%(Extension)`
- **Verified**: 522 native files present in published output after fix

### K-024: Script Upload Content Type
- **Finding**: Autoblow API `PUT /autoblow/sync-script/upload-funscript` expects `Content-Type: application/octet-stream` for the .funscript binary content, not `application/json`.
- **Fix**: Changed `UploadScriptAsync` to use `"application/octet-stream"` content type
- **Verified**: Fix applied to AutoblowDeviceClient.cs

### K-025: LibVLC Threading with WPF
- **Finding**: `LoadVideoAsync` creates LibVLC MediaPlayer inside `Task.Run()` (background thread). LibVLC events (`TimeChanged`, `Stopped`, `Playing`, `Paused`) fire on that background thread. WPF controls (like `VideoView.MediaPlayer`) can only be accessed on the UI thread.
- **Fix**: Wrap entire event handler body in `Dispatcher.Invoke()` — including null checks — not just the property assignment
- **Root Cause**: Original code only wrapped the assignment in `Dispatcher.Invoke` but evaluated `videoView.MediaPlayer == null` on the background thread, causing `InvalidOperationException`
- **Verified**: Fix applied to MainWindow.xaml.cs `OnVideoPlayerStateChanged` handler

## Failed Solutions

### K-023: Initial Solution Format
- **Issue**: First created `.slnx` format, then reverted to `.sln` for broader compatibility.
- **Fix**: Use `.sln` (legacy but more widely supported)

## Official Documentation Links
- Autoblow HTTP API: https://developers.autoblow.com/reference/http-api-v1-autoblow/
- Getting Started: https://developers.autoblow.com/
- LibVLCSharp: https://code.videolan.org/videolan/LibVLCSharp
- CommunityToolkit.Mvvm: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/
