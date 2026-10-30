# TEST_PLAN.md — Autoblow Sync Player

## Overview

This document describes the testing strategy for Autoblow Sync Player, covering:
1. Automated unit tests (xUnit)
2. Manual MP4 playback tests
3. Offline mock-device tests
4. Hardware integration tests (require approval)

## 1. Automated Unit Tests

### Framework
- **xUnit 2.9.3** — test runner and assertions
- **FluentAssertions 8.1.0** — fluent assertion syntax
- **Moq 4.20.72** — mocking dependencies

### Test Location
`tests/AutoblowSyncPlayer.Tests/`

### Running Tests
```powershell
dotnet test AutoblowSyncPlayer.sln
```

### Test Coverage (89 tests across 6 files)

| Test File | Subject | Test Count | Key Scenarios |
|---|---|---|---|
| `FunscriptFileValidatorTests.cs` | Script validation | 14 | Valid scripts, empty file, missing required fields, out-of-order timestamps, position bounds, duplicate action IDs |
| `FunscriptLoaderTests.cs` | File I/O + parsing | 9 | Valid file load, missing file, malformed JSON, empty file, encoding handling |
| `VideoPlayerTests.cs` | LibVLC wrapper | 12 | Play/pause/stop, seek, volume control, event firing, media loading, double-stop safety |
| `SyncEngineTests.cs` | Core sync algorithm | 30 | Position alignment, deadband logic, speed changes, pause/resume, error recovery, state transitions |
| `DpapiCredentialStorageTests.cs` | Credential persistence | 14 | Store/retrieve/delete, first-time vs cached, clear all, Windows-only behavior |
| `AppSettingsTests.cs` | Settings persistence | 10 | Load/save, default values, invalid JSON recovery, polling interval bounds, deadband bounds |

### Test Method Naming Convention
`{MethodName}_{Condition}_ExpectedResult`

Example: `LoadAsync_FileNotFound_ThrowsFileNotFoundException`

### Test Philosophy
- **Core layer**: Test business logic, validation, and algorithms in isolation
- **Infrastructure layer**: Mock external dependencies (file system, network, device API)
- **No integration tests with real devices**: All device communication is mocked

## 2. Manual MP4 Playback Tests

### Purpose
Verify that the WPF UI and LibVLC integration work correctly with real video files.

### Prerequisites
- Application built in Debug or Release mode
- Sample MP4 files with various codecs and resolutions
- Sample `.funscript` files

### Test Cases

| ID | Scenario | Steps | Expected Result |
|---|---|---|---|
| MP4-1 | Load valid MP4 | Click "Browse Video" → select MP4 | Video appears in player, controls enabled |
| MP4-2 | Play/Pause toggle | Click Play → Click Pause | Video pauses at current position |
| MP4-3 | Seek during playback | Drag timeline slider | Video jumps to new position |
| MP4-4 | Stop playback | Click Stop | Video returns to 0:00 |
| MP4-5 | Volume control | Adjust volume slider | Audio level changes accordingly |
| MP4-6 | Unsupported codec | Select MP4 with unsupported codec | Error message displayed, no crash |
| MP4-7 | Very large file | Select 4K+ MP4 (2GB+) | Loads and plays without OOM |
| MP4-8 | Simultaneous video + funscript | Load MP4 + `.funscript` → Play | Video plays, sync engine activates |

### Test Environment
- Windows 10 or later
- Integrated browser for device connection (if testing with device)

## 3. Offline Mock-Device Tests

### Purpose
Verify the sync engine and device communication layer without a physical device.

### Setup
1. Build the application
2. Mock the device API by intercepting HTTP calls (or use the existing Moq tests)
3. Use the `MockAutoblowService` or a local HTTP mock server

### Test Cases

| ID | Scenario | Steps | Expected Result |
|---|---|---|---|
| MOCK-1 | Connection to mock device | Enter mock token → Connect | Connection succeeds, status shows "Connected" |
| MOCK-2 | Script upload to mock | Load valid `.funscript` → Upload | Upload succeeds, `ScriptUploadResult.Success = true` |
| MOCK-3 | Speed control via mock | Sync engine sends speed change | Mock device receives correct `device/speed` command |
| MOCK-4 | Disconnection recovery | Disconnect → Reconnect | Device status resets, then reconnects |
| MOCK-5 | Invalid token | Use expired/wrong token | Connection fails gracefully with error message |
| MOCK-6 | Timeout during sync | Mock server delays response | Sync engine retries, does not crash |
| MOCK-7 | Polling interval changes | Change setting → restart sync | Polling uses new interval |

### Tools
- **WireMock** or **Microsoft.AspNetCore.TestHost** for HTTP mocking
- **Postman** or **curl** for manual endpoint testing

## 4. Hardware Integration Tests

### ⚠️ Require Approval

Hardware integration tests require:
- **Physical Autoblow Ultra device** — must be available and powered on
- **Network connectivity** — device and PC on same network
- **User approval** — tests involve actual device movement

### Prerequisites
- [ ] User has confirmed device availability
- [ ] User has confirmed network setup
- [ ] User has approved hardware testing

### Test Cases

| ID | Scenario | Steps | Expected Result |
|---|---|---|---|
| HW-1 | End-to-end sync (short video) | Connect device → load 1-min MP4 + funscript → Play | Device follows script positions within deadband tolerance |
| HW-2 | End-to-end sync (long video) | Connect device → load 30-min MP4 + funscript → Play | Sync drift remains within tolerance throughout |
| HW-3 | Pause/Resume with device | Pause video → Resume | Device pauses, then resumes from correct position |
| HW-4 | Seek during playback | Seek forward/backward | Device adjusts position to match new script timestamp |
| HW-5 | Network interruption | Disconnect WiFi mid-playback | App shows error, device stops safely |
| HW-6 | Multiple script uploads | Upload → play → upload different → play | Second script replaces first correctly |
| HW-7 | Device token persistence | Close app → reopen | Token cached, auto-connects on startup |
| HW-8 | Error recovery | Device becomes unresponsive | App shows status change, retry button works |

### Acceptance Criteria
- Sync drift < 2 seconds over 30-minute playback (default deadband)
- No crashes or memory leaks during extended testing
- Device stops safely on all error conditions

## Test Execution Priority

1. **Automated tests** — run on every build (CI-friendly)
2. **Manual MP4 tests** — run after UI changes
3. **Mock-device tests** — run after sync engine or API changes
4. **Hardware tests** — run before release, with user approval

## Continuous Integration

The automated test suite can run in CI:
```yaml
# Example GitHub Actions step
- name: Run tests
  run: dotnet test AutoblowSyncPlayer.sln --no-build
```

Hardware and manual tests are excluded from CI.
