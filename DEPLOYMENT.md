# DEPLOYMENT.md — Autoblow Sync Player

## Build

### Prerequisites

| Requirement | Minimum Version |
|---|---|
| .NET SDK | 10.0.x |
| Windows | 10 or later (WPF + DPAPI) |
| IDE | Visual Studio Code, Visual Studio 2022, or CLI |

### Build Commands

```powershell
# From project root (e:\AIProjects\AutoblowSyncPlayer)

# Build all projects
dotnet build AutoblowSyncPlayer.sln

# Build Release configuration
dotnet build AutoblowSyncPlayer.sln -c Release
```

Expected output:
- `src\AutoblowSyncPlayer.Core\bin\Release\net10.0\AutoblowSyncPlayer.Core.dll`
- `src\AutoblowSyncPlayer.Infrastructure\bin\Release\net10.0\AutoblowSyncPlayer.Infrastructure.dll`
- `src\AutoblowSyncPlayer.App\bin\Release\net10.0-windows\win-x64\AutoblowSyncPlayer.dll`

## Publish

### Self-Contained Windows x64

```powershell
dotnet publish src\AutoblowSyncPlayer.App\AutoblowSyncPlayer.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o ./publish/win-x64
```

This produces a standalone directory with:
- `AutoblowSyncPlayer.exe` — entry point
- All .NET runtime DLLs
- LibVLC native libraries
- WPF dependencies

### Framework-Dependent (smaller, requires .NET 10 on target)

```powershell
dotnet publish src\AutoblowSyncPlayer.App\AutoblowSyncPlayer.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -o ./publish/framework-dependent
```

### Single File (optional)

```powershell
dotnet publish src\AutoblowSyncPlayer.App\AutoblowSyncPlayer.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish/single-file
```

## Run

### Development

```powershell
dotnet run --project src/AutoblowSyncPlayer.App/AutoblowSyncPlayer.App.csproj
```

### Published Executable

Double-click `AutoblowSyncPlayer.exe` in the publish output directory.

## Install

### Manual Deployment

1. Publish using the self-contained command above.
2. Copy the entire `publish/win-x64` directory to the target machine.
3. Run `AutoblowSyncPlayer.exe`.

### Shortcut Creation (Optional)

```powershell
# PowerShell — create Desktop shortcut
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\Autoblow Sync Player.lnk")
$Shortcut.TargetPath = "C:\Path\To\AutoblowSyncPlayer.exe"
$Shortcut.WorkingDirectory = "C:\Path\To\"
$Shortcut.IconLocation = "C:\Path\To\AutoblowSyncPlayer.exe,0"
$Shortcut.Save()
```

## Settings & Data Storage

| Data | Location |
|---|---|
| Application settings | `%APPDATA%\AutoblowSyncPlayer\settings.json` |
| Device token | Windows DPAPI (current user) |
| Diagnostic logs | `%APPDATA%\AutoblowSyncPlayer\logs\` (if enabled) |

No SQL database or registry entries are used.

## Troubleshooting

### "Unable to find .NET runtime"

- Either install .NET 10 SDK/runtime, or use `--self-contained true` publish.

### "LibVLC native libraries not found"

- The `VideoLAN.LibVLC.Windows` package must be present.
- Ensure the publish output includes the `libvlc` native DLLs.
- Check that `core.CurrentDirectory` is set to the native package path before creating LibVLC.

### "Device connection fails with 502"

- The device may be offline or on a different network.
- Verify the device is powered on and connected to WiFi.
- Check that the cluster URL is being rediscovered from `/autoblow/connected`.

### "CA1416 warnings during build"

- **Expected.** `DpapiCredentialStorage` uses Windows-only APIs.
- These are informational and do not affect the build.

### "Funscript upload fails with 400"

- The script may have invalid actions (out-of-order timestamps, positions outside 0–100).
- Check validation results in the Script panel before uploading.

### "App freezes during playback"

- All I/O is async; freezing usually indicates a LibVLC issue.
- Verify the MP4 uses a supported codec (H.264 recommended).
- Check available disk space and memory.

### "Sync drift increases during long playback"

- Increase or decrease `PollingIntervalMs` in Settings.
- Check that the deadband (default 2000ms) is appropriate for your network latency.
- Review diagnostic logs if enabled.

## Uninstall

1. Close the application.
2. Delete the installation directory.
3. Optionally delete `%APPDATA%\AutoblowSyncPlayer\` to remove settings and credentials.
4. Remove any Desktop shortcut manually.

No installer or registry cleanup is required — the app is portable.

## Known Limitations

| Limitation | Reason |
|---|---|
| Only Autoblow Ultra supported | API only documented for Ultra |
| Playback speed fixed at 1x | Script timing is 1:1 with video |
| No multi-device support | One token, one device |
| Windows-only | WPF + DPAPI are Windows-specific |
