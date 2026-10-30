# ARCHITECTURE.md — Autoblow Sync Player

## Overview

Autoblow Sync Player is a Windows WPF desktop application that synchronizes Autoblow device playback with locally stored MP4 videos using `.funscript` files. It follows a layered architecture with MVVM pattern on the UI layer.

## Component Diagram

```
┌─────────────────────────────────────────────────────┐
│  AutoblowSyncPlayer.App (WPF UI)                     │
│  ┌──────────┐ ┌──────────┐ ┌───────────┐            │
│  │ MainWindow│ │Settings │ │AboutDialog│            │
│  └─────┬────┘ └────┬─────┘ └─────┬─────┘            │
│        │          │            │                     │
│  ┌─────▼──────────▼────────────▼───────┐            │
│  │         MainViewModel               │            │
│  │  ┌───────────┐ ┌─────────────────┐  │            │
│  │  │  DI Container               │  │            │
│  │  └─────┬─────┘ └────────┬────────┘  │            │
│  └────┬───┬─────────────────┬──────────┘            │
└───────┬───┬─────────────────┬──────────────────────┘
        │                   │
        │                   │
┌───────▼───┬────────────────▼────────────────────────┐
│  AutoblowSyncPlayer.Core           AutoblowSyncPlayer.Infrastructure │
│  ┌──────────────┐              ┌─────────────────┐ │
│  │    Models     │              │   Services       │ │
│  │              │              │                 │ │
│  │ Funscript     │              │ AutoblowDevice   │ │
│  │ FunscriptAction│             │ Client           │ │
│  │ Funscript...  │              │ MockAutoblow... │ │
│  │ Device...     │              │ LibVlcVideoPlayer│ │
│  │ Script...     │              │ FunscriptService │ │
│  │ SettingsModel │              │ SyncService      │ │
│  └──────────────┘              │ SettingsService   │ │
│                                │ DpapiCredential   │ │
│  ┌─────────────────────────────────┐               │ │
│  │       Service Interfaces        │               │ │
│  │ IAutoblowDeviceClient           │               │ │
│  │ IVideoPlayerService             │               │ │
│  │ IFunscriptService               │               │ │
│  │ ISyncService                    │               │ │
│  │ ISettingsService                │               │ │
│  │ ICredentialStorage              │               │ │
│  └─────────────────────────────────┘               │ │
│                              └────────────────────────┘
```

## Layers

### 1. Core (`AutoblowSyncPlayer.Core`)

**Responsibilities:**
- Domain models (Funscript, DeviceConnectionResult, Settings, etc.)
- Service interfaces (contracts)
- CommunityToolkit.Mvvm `ObservableObject` for data binding

**Key constraints:**
- **No external dependencies** except CommunityToolkit.Mvvm
- Pure C# 12 / .NET 10 — no UI, no file I/O, no HTTP
- Defines the API surface for all layers

**Models:**
| Model | Purpose |
|---|---|
| `Funscript` | Parsed funscript with actions array |
| `FunscriptAction` | Single `{at, pos}` entry |
| `FunscriptValidationError` | Validation error with severity |
| `FunscriptValidationResult` | Aggregated validation report |
| `DeviceConnectionResponse` | `/autoblow/connected` result |
| `DeviceConnectionState` | Connection lifecycle enum |
| `DeviceStateResponse` | `/autoblow/state` result |
| `ScriptUploadResponse` | `/autoblow/sync-script/upload-funscript` result |
| `ScriptStartRequest` | `PUT /start` payload (`startTimeMs`) |
| `ScriptOffsetRequest` | `PUT /offset` payload (`offsetTimeMs`) |
| `ScriptLoopRequest` | `PUT /loop` payload (`isLooping`) |
| `SettingsModel` | Persisted application settings |

**Interfaces:**
| Interface | Key Operations |
|---|---|
| `IAutoblowDeviceClient` | CheckConnection, GetDeviceInfo, GetDeviceState, UploadFunscript, StartScript, PauseScript, SetOffset, SetLoop |
| `IVideoPlayerService` | Load, Play, Pause, Stop, SetPosition, Volume, events for position/duration/ended |
| `IFunscriptService` | Load and validate `.funscript` files |
| `ISyncService` | Coordinate video + device playback, emergency stop |
| `ISettingsService` | Save/load `SettingsModel` |
| `ICredentialStorage` | Store/read device token (DPAPI-encrypted) |

### 2. Infrastructure (`AutoblowSyncPlayer.Infrastructure`)

**Responsibilities:**
- Implement all Core interfaces
- HTTP communication with Autoblow API
- LibVLC video engine integration
- Sync engine (polling, deadband, cooldown)
- Windows DPAPI credential storage
- JSON settings persistence

**Service implementations:**

| Service | Key Details |
|---|---|
| `AutoblowDeviceClient` | `HttpClient`-based REST client. Cluster discovery via `/autoblow/connected`. Rate-limit aware. Timeout handling. |
| `MockAutoblowDeviceClient` | Full mock for testing. Simulates states, no real HTTP. |
| `LibVlcVideoPlayer` | Wraps LibVLCSharp 3.9.4. Real video clock (not timer). Events for position/duration/ended. |
| `FunscriptService` | `System.Text.Json` parsing. Validates actions, timestamps, positions, API limits. |
| `SyncService` | Core sync engine. Polls device every N seconds. Deadband (2000ms default). Cooldown timer. Configurable offset. |
| `SettingsService` | JSON file persistence at `%APPDATA%\AutoblowSyncPlayer\settings.json`. |
| `DpapiCredentialStorage` | Windows DPAPI encrypted storage for device token. |

### 3. App (`AutoblowSyncPlayer.App`)

**Responsibilities:**
- WPF views (MainWindow, SettingsDialog, AboutDialog, LoadingWindow)
- ViewModels (`MainViewModel`) using CommunityToolkit.Mvvm
- Dependency injection setup (`App.xaml.cs`)
- Drag-and-drop, fullscreen, dark theme
- Application lifecycle (startup splash, shutdown cleanup)

**Dependency Injection:**
- Registered in `App.xaml.cs` via `Microsoft.Extensions.DependencyInjection`
- Real services in normal mode, mocks available for testing

## Data Flow

### Synchronized Playback Sequence

```
User clicks Play Video + Script
  │
  ├─→ MainViewModel.PlayCommand
  │     │
  │     ├─→ ISyncService.StartSync()
  │     │     │
  │     │     ├─→ IAutoblowDeviceClient.UploadFunscript()  [if needed]
  │     │     │     │
  │     │     │     └─→ HTTP PUT /upload-funscript (multipart/form-data)
  │     │     │
  │     │     ├─→ IVideoPlayerService.Play()
  │     │     │     │
  │     │     │     └─→ LibVLC MediaPlayer.Play(uri)
  │     │     │
  │     │     └─→ IAutoblowDeviceClient.StartScript(startTimeMs)
  │     │           │
  │     │           └─→ HTTP PUT /start {"startTimeMs": N}
  │     │
  │     └─→ Background poll: ISyncService.PollDeviceState()
  │             │
  │             └─→ Every N seconds: GET /state → compare positions → adjust offset
  │
  └─→ UI updates via INotifyPropertyChanged
```

### Sync Engine Details

```
┌────────────────────────────────────────────┐
│          SyncService (background)           │
│                                            │
│  Every PollingIntervalMs (default 3000ms):   │
│    1. GET /autoblow/state → devicePos       │
│    2. IVideoPlayerService.Position → videoPos│
│    3. drift = |devicePos - videoPos|        │
│    4. if drift > deadband (2000ms)           │
│         AND cooldown elapsed                │
│         → PUT /offset → correct drift       │
│         → reset cooldown                    │
│    5. Update SyncStatus UI binding          │
│                                            │
│  VideoEnded → PUT /stop → safe state       │
│  EmergencyStop → PUT /stop → priority high  │
└────────────────────────────────────────────┘
```

## Key Design Decisions

| Decision | Rationale |
|---|---|
| Core layer has zero external deps | Maximum testability, no LibVLC in unit tests |
| Sync uses polling, not streaming | Autoblow API is REST-only, no WebSocket |
| DPAPI for token storage | Windows-only app, DPAPI is OS-level encryption |
| JSON for settings | Simple, human-readable, no ORM overhead |
| MockAutoblowDeviceClient | Full test coverage without real hardware |
| Actual video clock (LibVLC) | Timer-based simulation causes drift |
| Deadband + cooldown | Prevents oscillation on minor clock differences |
| .NET 10 | Latest LTS framework, C# 12 features |

## Threading

- UI thread: WPF dispatcher, bound viewmodels
- Background: SyncService polling, HTTP requests (all async/await)
- LibVLC: Internal rendering thread, callbacks marshaled to UI
- No blocking calls on UI thread (all I/O is `async`)
