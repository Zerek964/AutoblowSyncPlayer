# API Contracts

## Service Interfaces

This document defines the public API contracts for all services in the Core layer.

---

## IFunscriptService

Parses and validates `.funscript` JSON files.

### Methods

#### `Task<FunscriptValidationResult> ValidateAsync(string filePath, CancellationToken cancellationToken = default)`
Validates a funscript file by path.

**Returns**: `FunscriptValidationResult` with validation status and errors.

**Validations performed**:
- File existence
- File size ≤ 60 MB
- Valid JSON structure
- Non-empty actions array
- Position values in range 0–100
- No negative timestamps
- Timestamps sorted in ascending order (critical error)
- Duplicate timestamps detected (warning, non-critical)
- Max 100,000 actions
- Max timestamp 16,777,214 ms

---

#### `Task<FunscriptValidationResult> ValidateFromTextAsync(string jsonText, CancellationToken cancellationToken = default)`
Validates funscript content from a JSON string.

Same validation as `ValidateAsync` but accepts raw JSON text.

---

#### `string? FindMatchingScriptPath(string videoFilePath)`
Finds a matching `.funscript` file for a video file.

**Returns**: Path to the `.funscript` file if it exists in the same directory with the same name as the video, otherwise `null`.

**Example**: `video.mp4` → looks for `video.funscript`

---

## IAutoblowDeviceClient

HTTP client for the Autoblow device REST API.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `HasToken` | `bool` | `true` if a device token is configured |
| `CurrentCluster` | `string?` | Current API cluster URL (null when disconnected) |
| `IsConnected` | `bool` | `true` if connected to the device |

### Methods

#### `void SetDeviceToken(string deviceToken)`
Synchronously sets the device authentication token.

**Note**: This method is synchronous and returns `void`.

---

#### `Task<DeviceConnectionResult?> CheckConnectionAsync(CancellationToken cancellationToken = default)`
Checks if the device is reachable and authenticated.

**Returns**: `DeviceConnectionResult` if connected, `null` if no token or connection failed.

**Result contains**:
- `IsConnected` — connection status
- `Cluster` — API cluster URL
- `DeviceType` — device model identifier
- `IsSupported` — whether the device type is supported

---

#### `Task<ScriptUploadResult> UploadScriptAsync(string filePath, CancellationToken cancellationToken = default)`
Uploads a funscript to the device.

**Returns**: `ScriptUploadResult` with upload status.

**Result contains**:
- `Success` — `true` if upload succeeded
- `SyncScriptToken` — token for subsequent playback operations (if successful)
- `ErrorMessage` — error description (if failed)

**Requirements**: Device must be connected first.

---

#### `Task<bool> LoadScriptAsync(string syncScriptToken, CancellationToken cancellationToken = default)`
Loads an uploaded script onto the device for playback.

**Returns**: `true` if script loaded successfully.

---

#### `Task<bool> StartScriptAsync(long startTimeMs, CancellationToken cancellationToken = default)`
Starts script playback at the specified timestamp.

**Returns**: `true` if playback started.

**Requirements**: Device connected and script loaded.

---

#### `Task<bool> StopScriptAsync(CancellationToken cancellationToken = default)`
Stops script playback on the device.

**Returns**: `true` if playback stopped.

---

#### `Task<bool> SetOffsetAsync(long offsetMs, CancellationToken cancellationToken = default)`
Sets a playback offset to compensate for drift.

**Returns**: `true` if offset applied.

---

#### `Task<bool> SetLoopAsync(bool enableLoop, CancellationToken cancellationToken = default)`
Enables or disables script loop playback.

**Returns**: `true` if loop setting applied.

---

#### `Task<DeviceStateResponse?> GetDeviceStateAsync(CancellationToken cancellationToken = default)`
Gets the current state of the device.

**Returns**: `DeviceStateResponse` if connected, `null` if not.

**Response contains**:
- `State` — current device state string
- `ScriptPositionMs` — current playback position in milliseconds

---

#### `Task<string?> GetDeviceInfoAsync(CancellationToken cancellationToken = default)`
Gets raw device information as a JSON string.

**Returns**: JSON string with device info, or `null` if not connected.

---

## ISyncService

Synchronizes video playback with device script playback.

### Methods

#### `Task StartAsync(IVideoPlayerService videoPlayer, IAutoblowDeviceClient deviceClient)`
Starts the synchronization engine.

Polls the device every 5 seconds and compares video position with device position. Applies offset adjustments if drift exceeds 2-second deadband.

---

#### `Task StopAsync()`
Stops the synchronization engine.

---

#### `Task SeekToPositionAsync(long positionMs)`
Seeks both the video and device to the specified position.

---

#### `Task EmergencyStopAsync()`
Immediately stops both video playback and device script playback.

---

## ICredentialStorage

Secure storage for device credentials using Windows DPAPI.

### Methods

#### `Task<string?> GetTokenAsync()`
Retrieves the stored device token.

**Returns**: Token string if stored, `null` if no token exists.

---

#### `Task SaveTokenAsync(string token)`
Saves the device token encrypted using DPAPI.

---

#### `Task DeleteTokenAsync()`
Deletes the stored device token.

---

## IVideoPlayerService

Controls video playback via LibVLC.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsPlaying` | `bool` | `true` if video is currently playing |
| `CurrentPositionMs` | `long` | Current playback position in milliseconds |
| `DurationMs` | `long` | Total video duration in milliseconds |

### Methods

#### `Task<bool> LoadVideoAsync(string filePath)`
Loads a video file for playback.

**Returns**: `true` if file loaded successfully.

---

#### `Task<bool> Play()`
Starts or resumes video playback.

**Returns**: `true` if playback started.

---

#### `Task<bool> Pause()`
Pauses video playback.

**Returns**: `true` if paused.

---

#### `Task<bool> Stop()`
Stops video playback and resets position.

**Returns**: `true` if stopped.

---

#### `Task<bool> SeekToPositionAsync(long positionMs)`
Seeks to a specific position in the video.

**Returns**: `true` if seek succeeded.

---

## Funscript Format Specification

### JSON Structure
```json
{
  "deviceType": "autoblow",
  "name": "Script Name",
  "description": "Script description",
  "tags": ["tag1", "tag2"],
  "actions": [
    { "at": 1000, "pos": 50 },
    { "at": 2000, "pos": 75 },
    { "at": 3000, "pos": 25 }
  ]
}
```

### Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `deviceType` | string | Optional | Target device type |
| `name` | string | Optional | Script display name |
| `description` | string | Optional | Script description |
| `tags` | string[] | Optional | Classification tags |
| `actions` | array | **Required** | Sequence of actions |

### Action Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `at` | integer | **Required** | Timestamp in milliseconds (0–16,777,214) |
| `pos` | integer | **Required** | Position value (0–100) |

### Validation Rules
- Actions array must not be empty
- Maximum 100,000 actions
- Timestamps must be non-negative and ≤ 16,777,214 ms
- Position values must be 0–100
- Timestamps should be sorted ascending
- Duplicate timestamps are warned (non-critical)
