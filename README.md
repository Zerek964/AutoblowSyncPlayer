# Autoblow Sync Player

<img width="990" height="697" alt="image" src="https://github.com/user-attachments/assets/c13d2b2c-d0b3-46ae-91a9-ee2d81ced246" />


# Bugs
3D videos play but are still playing in 2D
Large over 5GB+ MP4 files have issues, they play but you will get stutters
You need to wait for the funscript to upload to the device before you hit the play button, trying to think of a solution for this so it disables the Play button until the script is fully uplaoded

Windows desktop application that synchronizes Autoblow device playback with local MP4 videos using `.funscript` files. Supports immersive 3D/VR video playback (VR360, VR180, 3D SBS/OU) with mouse-controlled camera navigation — while preserving full sync accuracy.

## Features

### Playback & Synchronization
- **Local MP4 Video Playback** — Play video files stored on your machine using LibVLC
- **Funscript Loading & Validation** — Open and validate `.funscript` JSON files with comprehensive error checking
- **Device Connection** — Connect to your Autoblow device using a device token and the official Autoblow API
- **Script Upload** — Upload funscripts to the device through the API
- **Synchronized Playback** — Keep device script playback in sync with video playback time
- **Sync Offset Adjustment** — Fine-tune synchronization with an adjustable offset slider (±10 seconds)

### Interactive Camera Controls
- **Mouse Left-Click Drag** — Rotate camera yaw (horizontal) and pitch (vertical)
- **Mouse Scroll Wheel** — Zoom in/out (30°–120° FOV range)
- **Keyboard Controls** — `R` to reset camera, arrow keys for yaw/pitch, `+`/`-` for zoom, `Escape` to exit fullscreen
- **Double-Click Video** — Toggle fullscreen mode
- **Camera Settings Panel** — Adjustable mouse sensitivity, invert vertical movement toggle, yaw/pitch/FOV readouts

### Session Persistence
- **Settings Storage** — Automatically remembers your device token, last video/script paths, sync offset, and camera settings
- **Credential Security** — Device tokens encrypted using Windows DPAPI
- **Auto-Upload on Connect** — When connecting to a device, a validated funscript is automatically uploaded
- **Auto-Upload on Script Load** — When loading a new funscript with an already-connected device, it auto-uploads

### User Interface
- **Funscript Timeline Heatmap** — Visual activity intensity overlay below the video player showing script activity hotspots (speed-based bucket aggregation with 95th percentile normalization)
- **Heatmap Interactions** — Hover to see timestamp tooltips, click to seek to that position
- **Playback Buttons** — Image-based Play/Pause/Stop buttons with hover and pressed visual states
- **Seek Slider** — Click-to-seek slider with live position tracking (no snapping during drag)
- **Loading Screen** — Splash screen shown during startup while restoring previous session
- **Error Notifications** — Graceful error handling with dismissible warning banners for session restore failures
- **Application Icon** — Custom icon for the main window and dialogs
- **Dynamic Status Display** — Window title reflects current sync status
- **Menu Bar** — File (Load Video, Load Script, Exit) and Help (About) menus
- **About Dialog** — Version information and technology stack details

### Thread Safety & Reliability
- **Deadlock-Free Playback** — All LibVLC interactions use non-blocking event arguments and cached position reads to prevent cross-thread deadlocks
- **Reliable Stop/Pause/Seek** — Volatile flag gating prevents event handler accumulation and dispatcher flooding during blocking native calls
- **Thread-Safe Position Caching** — `Interlocked`-based cached position reads ensure the seek bar never blocks the UI thread

## Requirements

- Windows 10/11
- .NET 10 Runtime
- An Autoblow device with a valid device token

## Building

```bash
# Clone the repository
git clone <repository-url>
cd AutoblowSyncPlayer

# Build the solution
dotnet build AutoblowSyncPlayer.sln

# Run tests
dotnet test AutoblowSyncPlayer.sln

# Run the application
dotnet run --project src/AutoblowSyncPlayer.App
```

## Architecture

The project follows a layered architecture with three main layers:

| Layer | Description |
|-------|-------------|
| **Core** | Abstractions only — models, interfaces, enums. No external dependencies except CommunityToolkit.Mvvm |
| **Infrastructure** | Implementations of Core interfaces — funscript parsing, device API client, sync engine, credential storage, video player |
| **App** | WPF presentation layer — MainWindow, ViewModels, DI registration |

See [ARCHITECTURE.md](./docs/ARCHITECTURE.md) for detailed architecture documentation.

## Testing

The project uses xUnit with FluentAssertions and Moq for unit testing:

```bash
# Run all tests
dotnet test AutoblowSyncPlayer.sln

# Run tests with coverage (requires dotnet-coverage tool)
dotnet test /p:CollectCoverage=true
```

## Project Structure

```
AutoblowSyncPlayer/
├── src/
│   ├── AutoblowSyncPlayer.Core/          # Core abstractions
│   │   ├── Models/                       # Domain models
│   │   └── Services/                     # Service interfaces
│   ├── AutoblowSyncPlayer.Infrastructure/ # Service implementations
│   │   └── Services/                     # Concrete services
│   └── AutoblowSyncPlayer.App/           # WPF application
│       ├── Views/                        # XAML views
│       └── ViewModels/                   # MVVM view models
├── tests/
│   └── AutoblowSyncPlayer.Tests/         # Unit tests
├── docs/                                 # Documentation
└── AutoblowSyncPlayer.sln               # Solution file
```

## Documentation

- [Architecture Overview](./docs/ARCHITECTURE.md)
- [API Contracts](./docs/API_CONTRACT.md)
- [Test Plan](./docs/TEST_PLAN.md)
- [Deployment Guide](./docs/DEPLOYMENT.md)

## Deployment

### Prerequisites
- Windows 10/11 (x64)
- .NET 10 SDK (for building)

### Build Self-Contained Executable

To create a fully self-contained executable that doesn't require .NET to be installed on the target machine:

```bash
# Publish for Windows x64
dotnet publish -c Release -r win-x64 --self-contained true

# Output location: src/AutoblowSyncPlayer.App/bin/Release/net10.0-windows/win-x64/publish/
```

The published output will include:
- `AutoblowSyncPlayer.exe` — Main application executable
- All required .NET runtime and native dependencies

### Run from Source

Alternatively, run directly from source with the .NET SDK installed:

```bash
dotnet run --project src/AutoblowSyncPlayer.App
```

### Settings Location

Application settings are stored in JSON format at:
```
%LOCALAPPDATA%\AutoblowSyncPlayer\settings.json
```

Device tokens are securely stored using Windows DPAPI credential manager.

## License

GNU General Public License (GPL)
