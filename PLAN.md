# PLAN.md — Autoblow Sync Player

## Current State / Resume Here

- **Phase**: All 6 original phases complete + VR3D Phases 1-7 complete + Heatmap Phases H1-H5 complete
- **Active Task**: All phases complete — project in stable release state
- **Build**: Clean, all projects compile successfully
- **Tests**: 89/89 passing
- **VR3D Documentation**: 4 docs (VR3D_REQUIREMENTS.md, VR3D_ARCHITECTURE.md, VR3D_TEST_PLAN.md, MANUAL_TEST_CHECKLIST.md)
- **VR3D Implementation**: Complete (Core, Infrastructure, App, Tests, Release)
- **Heatmap Documentation**: TIMELINE_HEATMAP_PLAN.md (detailed H1-H5 plan)
- **Heatmap Implementation**: H1-H5 complete — FunscriptTimeline heatmap with seek, hover tooltips, and relocated video controls
- **Release**: Published to e:\AIProjects\Published\AutoblowSyncPlayer\ (win-x64, self-contained)
- **Runtime Bugs Fixed**: LibVLC external window (timing fix), heatmap visibility (Dispatcher + MinHeight), XamlParseException (extra `>` in XAML), video sizing (MinHeight=300 on Border), video mode ComboBox selection, dark theme ComboBox, video mode detection (async metadata), device token persistence (async void lifecycle), heatmap rendering (thread affinity + `bitmap.Freeze()` on background thread), device token not persisting to settings.json (SaveTokenAsync fix), volume slider not persisting (added Volume to settings chain)
- **Next Action**: All phases complete. Project ready for deployment.

---

## Development Phases

### Phase 1 — Foundation [P1]

- [x] **P1-01**: Create solution and project structure
  - Evidence: 4 projects created (Core, Infrastructure, App, Tests)
  - Build: Verified

- [x] **P1-02**: Create Core models (11 models)
  - Evidence: Funscript, FunscriptAction, FunscriptValidationError, FunscriptValidationResult, DeviceConnectionResponse, DeviceStateResponse, ScriptUploadResponse, ScriptStartRequest, ScriptOffsetRequest, ScriptLoopRequest, SettingsModel
  - Build: Verified

- [x] **P1-03**: Create Core service interfaces (6 interfaces)
  - Evidence: IVideoPlayer, IFunscriptService, IAutoblowDeviceClient, ISyncService, ISettingsService, ICredentialStorage
  - Build: Verified

### Phase 2 — Video Playback [P2]

- [x] **P2-01**: Implement LibVlcVideoPlayer
  - Evidence: `src/AutoblowSyncPlayer.Infrastructure/Services/LibVlcVideoPlayer.cs`
  - Tests: 12 tests in VideoPlayerTests.cs
  - Build: Verified

- [x] **P2-02**: Implement WPF MainWindow with video display
  - Evidence: `src/AutoblowSyncPlayer.App/Views/MainWindow.xaml`
  - Features: MP4 playback, play/pause/stop, seek, volume, drag-and-drop

### Phase 3 — Funscript Loading [P3]

- [x] **P3-01**: Implement FunscriptService
  - Evidence: `src/AutoblowSyncPlayer.Infrastructure/Services/FunscriptService.cs`
  - Tests: 23 tests across FunscriptFileValidatorTests.cs and FunscriptLoaderTests.cs
  - Validation: Timestamps, positions, ordering, duplicates, bounds

### Phase 4 — Device Connection [P4]

- [x] **P4-01**: Implement AutoblowDeviceClient
  - Evidence: `src/AutoblowSyncPlayer.Infrastructure/Services/AutoblowDeviceClient.cs`
  - Tests: 12 tests in MockAutoblowDeviceClientTests.cs
  - All 9 API endpoints implemented

- [x] **P4-02**: Implement DpapiCredentialStorage
  - Evidence: `src/AutoblowSyncPlayer.Infrastructure/Services/DpapiCredentialStorage.cs`
  - Tests: 14 tests in DpapiCredentialStorageTests.cs

### Phase 5 — Synchronization Engine [P5]

- [x] **P5-01**: Implement SyncService
  - Evidence: `src/AutoblowSyncPlayer.Infrastructure/Services/SyncService.cs`
  - Tests: 30 tests in SyncServiceTests.cs
  - Features: Deadband, polling, seek handling, pause/resume

- [x] **P5-02**: Implement MainViewModel
  - Evidence: `src/AutoblowSyncPlayer.App/ViewModels/MainViewModel.cs`
  - MVVM commands for all playback operations

### Phase 6 — Extended Features & Test Coverage [P6]

- [x] **P6-01**: Implement SettingsService
  - Evidence: `src/AutoblowSyncPlayer.Infrastructure/Services/SettingsService.cs`
  - Tests: 10 tests in SettingsServiceTests.cs

- [x] **P6-02**: Create SettingsDialog and AboutDialog
  - Evidence: XAML dialogs in Views/
  - Features: Polling interval, deadband, auto-detect funscript, logging toggle

- [x] **P6-03**: Create LoadingWindow
  - Evidence: `src/AutoblowSyncPlayer.App/Views/LoadingWindow.xaml`

- [x] **P6-04**: Create value converters
  - Evidence: `src/AutoblowSyncPlayer.App/Converters/ValueConverters.cs`

- [x] **P6-05**: DI registration in App.xaml.cs
  - Evidence: All services registered for dependency injection

## Documentation Phase [P7]

- [x] **P7-01**: AGENTS.md — development session resume instructions
- [x] **P7-02**: PROJECT_REQUIREMENTS.md — persisted requirements
- [x] **P7-03**: ARCHITECTURE.md — component diagram, data flow, design decisions
- [x] **P7-04**: API_CONTRACT.md — all 9 endpoints documented
- [x] **P7-05**: TEST_PLAN.md — automated, manual, mock, and hardware tests
- [x] **P7-06**: DEPLOYMENT.md — build, publish, install, troubleshooting
- [x] **P7-07**: README.md — user-facing instructions
- [x] **P7-08**: KNOWLEDGE.md — technical findings

## Known Blockers

None. All implementation complete.

## 3D/VR Video Playback Expansion

### Overview
Extending Autoblow Sync Player to support 3D and VR video playback (360° monoscopic, VR180, 3D/VR stereoscopic) with mouse-controlled interactive camera, while preserving all existing functionality and sync accuracy.

### Planning Documents
- **VR3D_PLAN.md** — Master plan with 7 new phases (Phase 1–7) for 3D/VR expansion
- **VR3D_REQUIREMENTS.md** — Detailed requirements for video modes, camera controls, UI, performance, sync preservation
- **VR3D_ARCHITECTURE.md** — Architecture design with new interfaces (ICameraController, IVideoModeDetector, IVideoRenderer), single clock design, thread safety
- **VR3D_TEST_PLAN.md** — Test strategy (~65 new automated tests + 33 manual tests, targeting 154 total)

### Current Phase
Phase 1 — Existing Application Audit and 3D Planning (documentation/planning only)

### Phase 1 Status
- ✅ Audit existing source code and verify build/test baseline
- ✅ Create VR3D_REQUIREMENTS.md
- ✅ Create VR3D_ARCHITECTURE.md
- ✅ Create VR3D_TEST_PLAN.md
- ⏳ Update PLAN.md, PROGRESS.md, KNOWLEDGE.md (in progress)

### Phases 2–7 (All Complete)
- ✅ Phase 2: Core layer — New interfaces, models, enums for VR3D
- ✅ Phase 3: Infrastructure — ICameraController, IVideoModeDetector, IVideoRenderer implementations
- ✅ Phase 4: App layer — UI for camera controls, video mode selection, settings
- ✅ Phase 5: Sync integration — Ensure single clock, camera isolation from sync
- ✅ Phase 6: Testing — 65 automated tests + manual validation
- ✅ Phase 7: Release — Performance tuning, edge cases, deployment, manual test checklist

### Critical Constraints
1. **Camera movement must NEVER interrupt video playback or funscript synchronization**
2. All existing 89 tests must continue passing
3. Existing 2D video playback must work identically to current behavior
4. Layered architecture must be preserved (Core → Infrastructure → App)

## Funscript Timeline Heatmap Feature

### Overview
Adding a color-coded funscript motion heatmap visualization between the video player and seek slider, and relocating playback controls (seek slider, Play/Pause/Stop) below the video player. This is a UI layout and visualization feature that preserves all existing functionality including 3D/VR camera, device sync, and fullscreen.

### Planning Document
- **TIMELINE_HEATMAP_PLAN.md** — Detailed implementation plan with 5 phases (H1–H5)

### Current Phase
Phase H4 — Relocate video controls below video player

### Phase Status
- ✅ **H1**: Discovery — codebase inspected, baseline verified (214/214 tests), TIMELINE_HEATMAP_PLAN.md created
- ✅ **H2**: Heatmap data calculation — HeatmapActivity model, IHeatmapCalculator, HeatmapCalculator, FunscriptHeatmap WPF control (WriteableBitmap, Bgr32, background thread rendering), MainViewModel properties, DI registration
- ✅ **H3**: Playback marker, hover tooltips, click-to-seek — FunscriptHeatmap added to MainWindow.xaml with bindings, SeekRequested event wired to MainViewModel.SeekToPositionCommand, hover tooltip with 300ms delay, playback marker triangle, script-exceeds-video warning
- ⏳ **H4**: Relocate video controls below video player
- ⏳ **H5**: Regression testing and release

### Critical Constraints
1. **Heatmap must NOT block video thread** — use `WriteableBitmap` on background thread
2. **All 214 existing tests must continue passing**
3. **Camera movement must NEVER affect heatmap** and vice versa — separate WPF controls
4. **Click-to-seek must use existing `SeekToPositionAsync`** — no new seek logic
5. **Video clock (`PositionMs`) remains single source of truth** — heatmap is read-only visualization

---

## Pending (Requires User Approval)

- Hardware integration testing with physical Autoblow Ultra device
- Final release publishing
- Funscript Timeline Heatmap feature (Phases H4-H5 pending implementation)
