# Architecture Overview

## High-Level Architecture

AutoblowSyncPlayer follows a clean layered architecture with unidirectional dependencies:

```
┌─────────────────────────────────────────────────────────────┐
│                       App Layer                              │
│  ┌───────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │  WPF UI   │  │  ViewModels  │  │  DI Container (App)  │  │
│  │ (XAML)    │  │  (MVVM)      │  │                      │  │
│  └─────┬─────┘  └──────┬───────┘  └──────────┬───────────┘  │
│        │               │                      │              │
└────────┼───────────────┼──────────────────────┼──────────────┘
         │               │                      │
         ▼               ▼                      ▼
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                        │
│  ┌────────────┐ ┌────────────┐ ┌────────┐ ┌──────────────┐  │
│  │ Funscript  │ │ Autoblow   │ │ Sync   │ │ LibVLC Video │  │
│  │ Service    │ │ Device     │ │ Service│ │ Player       │  │
│  │            │ │ Client     │ │        │ │              │  │
│  └────────────┘ └────────────┘ └────────┘ └──────────────┘  │
│  ┌────────────┐                                             │
│  │ DPAPI      │                                             │
│  │ Credential │                                             │
│  │ Storage    │                                             │
│  └────────────┘                                             │
└──────────────────────────┬──────────────────────────────────┘
                           │ implements
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                        Core Layer                             │
│  ┌────────────────────────┐  ┌──────────────────────────┐   │
│  │ Models                 │  │ Services (Interfaces)     │   │
│  │ - Funscript            │  │ - IFunscriptService        │   │
│  │ - FunscriptAction      │  │ - IAutoblowDeviceClient    │   │
│  │ - FunscriptValidation  │  │ - ISyncService             │   │
│  │ - DeviceConnection*    │  │ - ICredentialStorage       │   │
│  │ - ScriptUploadResult   │  │ - IVideoPlayerService      │   │
│  └────────────────────────┘  └──────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Layer Responsibilities

### Core Layer (`AutoblowSyncPlayer.Core`)
- **Dependency**: CommunityToolkit.Mvvm only (no other external dependencies)
- **Models**: Domain objects representing funscripts, device state, validation results
- **Interfaces**: Service contracts that Infrastructure must implement
- **Purpose**: Define the problem domain without any implementation details

### Infrastructure Layer (`AutoblowSyncPlayer.Infrastructure`)
- **Dependencies**: Core layer + external libraries (LibVLC, System.Net.Http, etc.)
- **Implementations**: Concrete services implementing Core interfaces
- **Services**:
  - `FunscriptService` — JSON parsing and validation of .funscript files
  - `AutoblowDeviceClient` — HTTP client for the Autoblow REST API
  - `MockAutoblowDeviceClient` — Simulated device for testing/development
  - `SyncService` — Timer-based synchronization engine
  - `DpapiCredentialStorage` — Windows DPAPI credential storage
  - `LibVlcVideoPlayer` — LibVLC-based video playback

### App Layer (`AutoblowSyncPlayer.App`)
- **Dependencies**: Core + Infrastructure
- **Presentation**: WPF UI with XAML views and MVVM view models
- **DI**: Service registration and lifecycle management in `App.xaml.cs`
- **ViewModels**: `MainViewModel` orchestrates all services

## Key Design Patterns

### MVVM (Model-View-ViewModel)
- Views (XAML) bind to ViewModel properties via data binding
- Commands (`RelayCommand`) handle user actions
- `ObservableObject` provides property change notifications
- No code-behind logic in views

### Dependency Injection
- All services registered in `App.xaml.cs` via DI container
- ViewModels receive dependencies via constructor injection
- Services resolved from container with appropriate lifetimes

### Interface Segregation
- Each service interface has a single, focused responsibility
- Implementations are swappable (real device vs mock)

## Data Flow

### Video + Script Loading
```
User selects MP4 → VideoPlayerService loads file
User selects funscript → FunscriptService validates → ValidationResult displayed
```

### Device Connection & Upload
```
User enters token → CredentialStorage saves → DeviceClient sets token
User clicks connect → DeviceClient.CheckConnectionAsync → ConnectionState updated
User uploads script → DeviceClient.UploadScriptAsync → UploadResult displayed
```

### Synchronized Playback
```
User starts playback → VideoPlayer.Play() + SyncService.Start()
                    → SyncService polls device every 5s
                    → Compares video position vs device position
                    → Adjusts offset if drift > 2s deadband
User seeks → VideoPlayer.Seek() + SyncService.SetPosition()
User stops → VideoPlayer.Stop() + DeviceClient.StopScriptAsync()
```

## Technology Stack

| Component | Technology | Version |
|-----------|------------|---------|
| Framework | .NET | 10.0 |
| UI | WPF | net10.0-windows |
| MVVM | CommunityToolkit.Mvvm | 8.4.0 |
| Video | LibVLCSharp + VideoLAN.LibVLC.Windows | 3.9.4 / 3.0.21 |
| DI | Microsoft.Extensions.DependencyInjection | 10.0.0 |
| Logging | Microsoft.Extensions.Logging | 10.0.0 |
| Security | System.Security.Cryptography.ProtectedData | 8.0.0 |
| Testing | xUnit + Moq + FluentAssertions | 2.9.3 / 4.20.72 / 8.1.0 |
