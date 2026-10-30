# Copilot Instructions for AutoblowSyncPlayer

## Project Overview
Windows WPF desktop application that synchronizes Autoblow device playback with local MP4 videos using .funscript files. Built with .NET 10, layered architecture, and MVVM pattern.

## Coding Standards
- Use C# 12 features (primary constructors, collections expressions, etc.)
- Follow layered architecture: Core (abstractions) → Infrastructure (implementations) → App (UI)
- MVVM with CommunityToolkit.Mvvm (`ObservableObject`, `RelayCommand`)
- Async/await for all I/O operations
- Meaningful XML doc comments on public APIs

## Architecture Rules
- Core layer contains NO external dependencies except CommunityToolkit.Mvvm
- Infrastructure implements Core interfaces only
- App layer depends on both Core and Infrastructure
- All services registered via DI in App.xaml.cs

## LibVLC Notes
- LibVLCSharp 3.9.4: no `Core.Initialize()`, use `new LibVLC()` directly
- MediaPlayer created via `new MediaPlayer(libvlc)` constructor
- Events in `LibVLCSharp.Shared` namespace
- Must set `core.CurrentDirectory` to native package path before creating LibVLC

## Testing Guidelines
- xUnit framework with FluentAssertions for assertions
- Use Moq for mocking dependencies
- Test file naming: `{ClassName}Tests.cs`
- Test method naming: `{MethodName}_{Condition}_ExpectedResult`
- Use `.Contain(predicate)` not `ContainAny` for collection assertions

## API Integration
- Autoblow API uses REST endpoints under cluster URLs
- Device token authentication required for all operations
- CheckConnectionAsync returns nullable `DeviceConnectionResult?`
- SetDeviceToken is synchronous (void return)
- Script upload returns `ScriptUploadResult` with `.Success` property

## Common Commands
```bash
# Build solution
dotnet build AutoblowSyncPlayer.sln

# Run tests
dotnet test AutoblowSyncPlayer.sln

# Run app
dotnet run --project src/AutoblowSyncPlayer.App
```

## File Locations
- Solution: `AutoblowSyncPlayer.sln`
- Core models: `src/AutoblowSyncPlayer.Core/Models/`
- Core interfaces: `src/AutoblowSyncPlayer.Core/Services/`
- Infrastructure: `src/AutoblowSyncPlayer.Infrastructure/Services/`
- UI: `src/AutoblowSyncPlayer.App/Views/`
- ViewModels: `src/AutoblowSyncPlayer.App/ViewModels/`
- Tests: `tests/AutoblowSyncPlayer.Tests/`
