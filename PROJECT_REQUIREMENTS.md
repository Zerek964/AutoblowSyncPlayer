# PROJECT_REQUIREMENTS.md — Autoblow Sync Player

## Primary Objective

Build a Windows desktop application that:

1. Plays locally stored MP4 video files.
2. Opens and validates matching `.funscript` files.
3. Connects to a supported Autoblow device using a device token and the official Autoblow API.
4. Uploads the selected funscript to the device through the API.
5. Synchronizes the device's script playback with the MP4 video's playback time.
6. Supports Play, Pause, Stop, Resume, and seeking.
7. Provides a visual Windows desktop interface.
8. Remembers non-sensitive settings.
9. Recovers gracefully from connection problems.
10. Can be compiled, tested, and published as a Windows executable.

**Scope:** Local Windows desktop application only. MP4 video stays local. Only the script and device commands are transmitted via the Autoblow API.

---

## Development Environment

| Aspect | Value |
|---|---|
| OS | Windows 11 |
| Language | C# 12 |
| Framework | .NET 10 |
| UI | WPF (MVVM) |
| IDE | Visual Studio Code |
| Shell | PowerShell |
| Target | Windows x64 |

### Key Dependencies
- LibVLCSharp + LibVLCSharp.WPF + VideoLAN.LibVLC.Windows
- CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- xUnit, FluentAssertions, Moq (testing)

---

## Autoblow API Requirements

### Cluster Discovery
- Determine the actual cluster using `GET /autoblow/connected`.
- All subsequent commands use the returned cluster URL.
- Validate cluster is an authorized HTTPS Autoblow API host.
- Do not hardcode a specific regional cluster.

### Authentication
- Use `x-device-token` HTTP request header.
- Never log, commit, or store the token in plaintext.
- Store credentials using Windows DPAPI.
- Provide a masked token field with show/hide toggle in UI.

### Device Compatibility
- Verify `deviceType` from the connection response before enabling device control.
- Report incompatibility for unsupported models.
- Do not assume authentication = compatible device.

### API Endpoints

| Function | Method | Path |
|---|---|---|
| Check connection | GET | `/autoblow/connected` |
| Device information | GET | `/autoblow/info` |
| Device state | GET | `/autoblow/state` |
| Upload funscript | PUT | `/autoblow/sync-script/upload-funscript` |
| Load uploaded script | PUT | `/autoblow/sync-script/load-token` |
| Start/seek script | PUT | `/autoblow/sync-script/start` |
| Pause script | PUT | `/autoblow/sync-script/stop` |
| Set timing offset | PUT | `/autoblow/sync-script/offset` |
| Set script looping | PUT | `/autoblow/sync-script/loop` |

### API Constraints
- Funscript actions: time `at` in ms, position `pos` 0–100.
- Max actions: 100,000. Max timestamp: 16,777,214 ms. Upload limit: 60 MB.
- Script tokens expire after ~9 days.
- Handle HTTP 400, 401, 403, 429, 502, timeouts, and malformed responses.
- Do not retry indefinitely or overwhelm the API.

---

## UI Requirements

### Dark Theme WPF Interface

#### Device Connection Panel
- Token entry, connect/disconnect buttons
- Connection indicator (Disconnected, Connecting, Connected, Device Offline, Unsupported, etc.)
- Detected device type, cluster, and operational state

#### Video Player
- Large embedded LibVLC display
- Load Video, Play/Pause, Stop, seek bar
- Current time / total duration
- Volume, mute, fullscreen
- Drag-and-drop MP4 support
- Use actual media engine clock (not a simulated timer)

#### Script Panel
- Load Funscript, upload button
- Validation indicator, action count, duration
- Upload progress/status, sync readiness indicator
- Auto-detect matching `.funscript` by same base filename

#### Playback and Sync Controls
- Play Video + Script, Pause Both, Stop Both, Restart
- Sync offset control (ms), Reset Offset
- Video timestamp, device script timestamp, sync difference
- Emergency Stop button

#### Settings
- Save device token securely (DPAPI)
- Remember window size
- Default sync offset
- Auto-detect same-name script (toggle)
- Upload confirmation (toggle)
- Diagnostic logging (toggle)

---

## Funscript Handling

- Parse JSON using `System.Text.Json`.
- Validate actions array, timestamps (ordered), positions (0–100).
- Detect duplicates and malformed actions.
- Enforce API limits (100K actions, 16,777,214 ms max timestamp).
- Report validation errors clearly.
- Do not silently modify script contents.
- Show duration warning if script/video differ by more than 10s.

---

## Synchronization Engine

### Workflow
1. **Connect** — check online, discover cluster, verify device type, retrieve state.
2. **Load MP4** — display duration and position. No autoplay.
3. **Load Funscript** — validate and display details.
4. **Prepare** — upload script, wait for loading, confirm ready state.
5. **Play** — verify device + script, start video + script with `startTimeMs`.
6. **Pause** — pause video, issue `/stop`, preserve position.
7. **Resume** — read position, issue `/start` with position, resume playback.
8. **Seek** — sync device to new timestamp if playing; hold if paused.
9. **End** — stop device script, return to safe state.

### Accuracy
- Use actual video clock, not a simulated timer.
- Device performs script playback after full upload.
- Configurable offset in ms.
- Poll device state conservatively (every few seconds).
- Deadband (2000ms) and cooldown to prevent excessive adjustments.
- Do not send individual funscript actions as continuous HTTP commands.

---

## Safety Requirements

- No motion on app launch.
- No auto-resume of hardware after reconnect.
- Stop device on app close or playback failure.
- Emergency Stop button with prominent warning on failure.
- Stop request has priority over ordinary playback ops.
- Handle disconnection without freezing UI.
- No auto-resume after API timeout.
- No uncontrolled or repeated movement commands.

### Privacy
- Video files stay local. No analytics, tracking, or telemetry.
- No external web service or SQL database.
- Never include device token in diagnostics.
- Real device movement requires explicit user authorization.

---

## Architecture

```
Core (abstractions) → Infrastructure (implementations) → App (UI)
```

- Core: Models, service interfaces (no external deps except CommunityToolkit.Mvvm).
- Infrastructure: Service implementations (LibVLC, API client, sync engine, credentials).
- App: WPF views, viewmodels, DI registration.
- Tests: xUnit with mocks, no real hardware required.

---

## Testing Requirements

See `docs/TEST_PLAN.md` for the full test matrix. Current coverage: 89 tests across 6 files.

Key test areas: funscript parsing/validation, API error handling, mock device client, sync engine polling and lifecycle, settings persistence, model classes.

---

## Build and Publish

```powershell
# Build
dotnet build AutoblowSyncPlayer.sln

# Test
dotnet test AutoblowSyncPlayer.sln

# Publish
dotnet publish src\AutoblowSyncPlayer.App\AutoblowSyncPlayer.App.csproj -c Release -r win-x64 --self-contained true
```
