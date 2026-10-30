# Deployment Guide

## Prerequisites

### Build Requirements
- **OS**: Windows 10/11 (x64)
- **.NET SDK**: Version 10.0.x (currently 10.0.403)
- **Git**: v2.56.0+ (for source control)

### Runtime Requirements
- **OS**: Windows 10/11 (x64) — DPAPI requires Windows
- **.NET Desktop Runtime**: 10.0.x
- **LibVLC Native Libraries**: Bundled via `VideoLAN.LibVLC.Windows` NuGet package

## Building from Source

### 1. Clone the Repository
```bash
git clone <repository-url>
cd AutoblowSyncPlayer
```

### 2. Restore Dependencies
```bash
dotnet restore AutoblowSyncPlayer.sln
```

### 3. Build the Solution
```bash
# Debug build (default)
dotnet build AutoblowSyncPlayer.sln

# Release build (optimized)
dotnet build AutoblowSyncPlayer.sln -c Release
```

### 4. Run Tests
```bash
dotnet test AutoblowSyncPlayer.sln
```

## Deployment Options

### Option 1: Self-Contained Publish (Recommended)

Publishes the app with all dependencies, including the .NET runtime, for a standalone deployment:

```bash
# Publish for Windows x64
dotnet publish src/AutoblowSyncPlayer.App/AutoblowSyncPlayer.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o ./publish/win-x64
```

**Output**: `./publish/win-x64/AutoblowSyncPlayer.App.exe` — includes .NET runtime (~200MB total)

**Advantages**:
- No .NET runtime installation required on target machine
- Single-folder deployment
- Works on clean Windows installations

### Option 2: Framework-Dependent Publish

Publishes the app requiring the .NET 10 Desktop Runtime to be installed on the target machine:

```bash
dotnet publish src/AutoblowSyncPlayer.App/AutoblowSyncPlayer.App.csproj `
  -c Release `
  -o ./publish/framework-dependent
```

**Output**: `./publish/framework-dependent/AutoblowSyncPlayer.App.exe` (~5MB total)

**Requirements**: Target machine must have .NET 10.0 Windows Desktop Runtime installed

**Advantages**:
- Smaller download size
- Faster publish time
- Shared runtime across apps

### Option 3: Single-File Publish

Bundles the app into a single executable for clean deployment:

```bash
dotnet publish src/AutoblowSyncPlayer.App/AutoblowSyncPlayer.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish/single-file
```

**Output**: `./publish/single-file/AutoblowSyncPlayer.App.exe` (single ~200MB executable)

**Advantages**:
- Cleanest deployment (single file)
- Easy distribution
- No scattered dependencies

## LibVLC Native Libraries

The `VideoLAN.LibVLC.Windows` NuGet package automatically includes native LibVLC binaries. At runtime, the app must locate these files.

### How It Works
1. On startup, `LibVlcVideoPlayer` resolves the LibVLC native package path
2. Sets `Directory.SetCurrentDirectory()` to the native folder
3. Creates `new LibVLC()` which loads the native DLLs

### Deployment Considerations
- **Self-contained publish**: LibVLC natives are automatically included
- **Framework-dependent publish**: LibVLC natives are automatically included
- **Single-file publish**: LibVLC natives are extracted to a temp folder at runtime

## Configuration

### Device API Configuration

The app connects to Autoblow devices via REST API. Configuration is stored in the app settings or environment:

| Setting | Description | Default |
|---------|-------------|---------|
| `DeviceClusterUrl` | Base URL for device API | User-configured |
| `DeviceId` | Unique device identifier | User-configured |
| `DeviceToken` | Authentication token | DPAPI-encrypted |

### Credential Storage

Device tokens are stored using Windows DPAPI (`DpapiCredentialStorage`):
- **Scope**: Current user, current machine
- **Encryption**: Windows Data Protection API
- **Location**: User's profile directory

## Troubleshooting

### Build Errors

| Error | Cause | Solution |
|-------|-------|----------|
| ".NET SDK not found" | .NET 10 SDK not installed | Install from dotnet.microsoft.com |
| "LibVLCSharp namespace not found" | NuGet restore failed | Run `dotnet restore` |
| "Target framework not recognized" | Old .NET SDK | Update to .NET 10 SDK |

### Runtime Errors

| Error | Cause | Solution |
|-------|-------|----------|
| "LibVLC native libs not found" | Publish didn't include natives | Use self-contained publish |
| "DPAPI not available" | Running on non-Windows OS | Install on Windows 10/11 |
| "Device connection failed" | Network/API issues | Check cluster URL and device token |

### Test Failures

| Issue | Solution |
|-------|----------|
| Tests fail to compile | Check FluentAssertions syntax (use `.Contain()` not `ContainAny()`) |
| DPAPI tests fail | Ensure tests run on Windows with user profile |
| LibVLC tests fail | Mock LibVLC in tests; don't use real video files |

## CI/CD Considerations

### GitHub Actions Example
```yaml
name: Build and Test
on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Restore
        run: dotnet restore AutoblowSyncPlayer.sln
      - name: Build
        run: dotnet build AutoblowSyncPlayer.sln -c Release --no-restore
      - name: Test
        run: dotnet test AutoblowSyncPlayer.sln --no-build --verbosity normal
      - name: Publish
        run: dotnet publish src/AutoblowSyncPlayer.App/AutoblowSyncPlayer.App.csproj -c Release -r win-x64 --self-contained true
```

## Future Deployment Enhancements

- [ ] ClickOnce deployment for easy updates
- [ ] MSIX package for Microsoft Store distribution
- [ ] Installer creation (WiX / Inno Setup)
- [ ] Automatic update mechanism
- [ ] App configuration via `appsettings.json`
