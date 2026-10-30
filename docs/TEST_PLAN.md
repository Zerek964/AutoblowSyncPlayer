# Test Plan

## Test Framework

- **Framework**: xUnit 2.9.3
- **Assertions**: FluentAssertions 8.1.0
- **Mocking**: Moq 4.20.72
- **Test Runner**: `dotnet test`

## Running Tests

```bash
# Run all tests
dotnet test AutoblowSyncPlayer.sln

# Run tests with verbose output
dotnet test AutoblowSyncPlayer.sln -v normal

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

## Test Organization

Tests are located in `tests/AutoblowSyncPlayer.Tests/` organized by area:

```
tests/AutoblowSyncPlayer.Tests/
├── Core/
│   ├── FunscriptServiceTests.cs          # 17 tests
│   ├── MainViewModelCameraTests.cs       # 20 tests
│   ├── MockAutoblowDeviceClientTests.cs  # 16 tests
│   ├── SettingsServiceTests.cs           # 14 tests
│   └── SyncServiceTests.cs               # 19 tests
├── Infrastructure/
│   ├── CameraControllerTests.cs          # 19 tests
│   ├── HeatmapCalculatorTests.cs         # 21 tests
│   ├── SettingsServiceCameraTests.cs     # 22 tests
│   ├── SyncIsolationTests.cs             # 12 tests
│   ├── VideoModeDetectorTests.cs         # 18 tests
│   └── VideoRendererTests.cs             # 18 tests
└── Models/
    ├── FunscriptModelTests.cs            # 17 tests
    ├── SettingsModelCameraTests.cs       # 13 tests
    └── SettingsModelTests.cs             # 6 tests
```
**Total: 235 tests across 14 test files** (89 original + 125 VR3D + 21 HeatmapCalculator)

## Current Test Coverage

### FunscriptServiceTests (17 tests)

| Category | Tests |
|----------|-------|
| File validation | File not found, file size > 60MB, invalid JSON, valid script |
| Parsing from text | Valid text, null JSON, empty actions |
| Position validation | Position out of range (0-100) |
| Timestamp validation | Negative timestamps, unsorted timestamps, duplicate timestamps |
| Metadata parsing | Script with name, description, deviceType, tags |
| Limits | Max actions exceeded (100k+), max timestamp exceeded (16,777,214ms) |
| Discovery | FindMatchingScriptPath for existing and non-existing files |

### MockAutoblowDeviceClientTests (16 tests)

| Category | Tests |
|----------|-------|
| Token management | Set/clear token, HasToken property |
| Connection | CheckConnection with/without token, cluster/device info |
| Script operations | Upload, load, start, stop |
| Playback control | Set offset, set loop, get device state |
| Device info | GetDeviceInfo when connected/disconnected |
| Edge cases | Operations fail without token |

### SettingsServiceTests (14 tests)

| Category | Tests |
|----------|-------|
| Property pass-through | All interface properties correctly pass through to internal settings model |
| New settings | ResumeLastPosition, PollingIntervalMs, RememberWindowSize, StartMinimized, WindowSize |
| Legacy settings | DeviceToken, LastScriptPath, LastVideoPath, LastDeviceCluster, SyncOffsetMs, UploadConfirmation, AutoDetectScript, LoggingEnabled |
| Save method | Save() persists without throwing |

### SyncServiceTests (19 tests)

| Category | Tests |
|----------|-------|
| Constructor | Initial status, default offset, monitoring state |
| SetOffset | Updates offset correctly |
| PollingInterval clamping | 9 Theory cases: 500→1000, 1000→1000, 3000→3000, 5000→5000, 10000→10000, 30000→30000, 60000→30000, 0→1000, -500→1000 |
| Playback lifecycle | Start/stop with connected and disconnected device |
| Monitoring | Start/stop monitoring, prevent duplicate start |
| Emergency stop | Stops both video and device playback |
| Pause | Pauses video and device |
| Video ended | Auto-stops on video end |
| Dispose | Stops monitoring on disposal |

### FunscriptModelTests (17 tests)

| Category | Tests |
|----------|-------|
| Funscript model | Properties, computed DurationMs, computed ActionCount |
| FunscriptAction model | At and Pos properties, range values |
| FunscriptValidationResult | IsValid, Errors, Script, DurationMs, ActionCount |
| FunscriptValidationError | Message, IsCritical |
| Enums | DeviceConnectionState values |
| Result models | DeviceConnectionResult, ScriptUploadResult, ScriptStartRequest, ScriptOffsetRequest, ScriptLoopRequest, DeviceStateResponse |

### SettingsModelTests (6 tests)

| Category | Tests |
|----------|-------|
| Default constructor | All properties initialized to correct defaults |
| New property defaults | ResumeLastPosition, PollingIntervalMs, RememberWindowSize, StartMinimized defaults verified |
| Individual set/get | Each new property can be set and retrieved correctly |
| WindowSize | WindowSize property persistence |

### HeatmapCalculatorTests (21 tests)

| Category | Tests |
|----------|-------|
| Basic calculation | Normal script produces activities, correct bucket count, values in 0-1 range |
| Empty/minimal script | Empty actions returns all-zero activities, single action returns all-zero |
| Edge cases | Zero duration between actions, null script throws ArgumentException |
| Normalization | 95th percentile clamping works, outlier values don't saturate heatmap |
| Position handling | All-zero positions produce zero activity, exceeding video duration handled correctly |
| Color rendering | Gradient colors correct for low/medium/high intensity values |

## Test Naming Convention

Tests follow the pattern: `{MethodName}_{Condition}_ExpectedResult`

**Examples**:
- `ValidateAsync_FileNotFound_ReturnsInvalid`
- `ValidateFromTextAsync_InvalidJSON_ReturnsCriticalError`
- `SetDeviceToken_ShouldSetHasTokenToTrue`
- `DurationMs_ComputedFromLastActionTimestamp`

## Test Patterns

### Assertion Style
- Use FluentAssertions `.Should()` fluent syntax
- Use `.Contain(predicate)` for collection assertions (not `ContainAny`)
- Assert both positive and negative cases

### Mock Usage
- Use `MockAutoblowDeviceClient` for testing device interaction without real hardware
- Mock simulates successful operations when connected
- Note: Mock doesn't reset `_connected` when token is cleared (known behavior)

### Async Testing
- All async methods marked `async` and use `await`
- No `.Result` or `.Wait()` calls

## Future Test Areas

### Planned test suites:
1. ~~SyncServiceTests~~ ✅ **DONE** — Synchronization engine timing, drift detection, emergency stop
2. **LibVlcVideoPlayerTests** — Video playback lifecycle (requires mocking LibVLC)
3. **DpapiCredentialStorageTests** — Token save/load/delete (requires Windows test environment)
4. **MainViewModelTests** — MVVM command execution and property changes
5. **Integration Tests** — End-to-end video + device sync flow

## Test Status

| Suite | Tests | Status |
|-------|-------|--------|
| FunscriptServiceTests | 17 | ✅ Passing |
| MockAutoblowDeviceClientTests | 16 | ✅ Passing |
| FunscriptModelTests | 17 | ✅ Passing |
| SettingsServiceTests | 14 | ✅ Passing |
| SyncServiceTests | 19 | ✅ Passing |
| SettingsModelTests | 6 | ✅ Passing |
| CameraControllerTests | 19 | ✅ Passing |
| MainViewModelCameraTests | 20 | ✅ Passing |
| MockAutoblowDeviceClientTests | 16 | ✅ Passing (updated) |
| SettingsServiceTests | 14 | ✅ Passing |
| SettingsServiceCameraTests | 22 | ✅ Passing |
| SyncIsolationTests | 12 | ✅ Passing |
| VideoModeDetectorTests | 18 | ✅ Passing |
| VideoRendererTests | 18 | ✅ Passing |
| FunscriptModelTests | 17 | ✅ Passing |
| SettingsModelCameraTests | 13 | ✅ Passing |
| HeatmapCalculatorTests | 21 | ✅ Passing |
| **Total** | **235** | **All Passing** |
