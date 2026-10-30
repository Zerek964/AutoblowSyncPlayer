# API_CONTRACT.md — Autoblow HTTP API V1

## Official Documentation

- **Developer Portal:** https://developers.autoblow.com/
- **HTTP API Reference:** https://developers.autoblow.com/reference/http-api-v1-autoblow/
- **Getting Started:** https://developers.autoblow.com/

## Base URL

All endpoints use the cluster URL returned by `/autoblow/connected`. The initial discovery endpoint is:

```
https://latency.autoblowapi.com/autoblow/connected
```

Subsequent requests use the discovered cluster, e.g.:

```
https://eu-central-1.autoblowapi.com/autoblow/...
```

## Authentication

Every request requires the `x-device-token` header:

```
x-device-token: <DEVICE_TOKEN>
```

- Token is obtained from the user via the device app.
- **Never log or store in plaintext.** Use Windows DPAPI.
- Token is valid indefinitely unless revoked.
- Uploaded script tokens (`syncScriptToken`) expire after ~9 days.

## Endpoints

### 1. Check Connection

| Aspect | Value |
|---|---|
| Method | `GET` |
| Path | `/autoblow/connected` |
| Body | None |
| Purpose | Verify device online, discover cluster, check device type |

**Response:**
```json
{
  "connected": true,
  "cluster": "eu-central-1.autoblowapi.com",
  "deviceType": "autoblow-ultra"
}
```

**Known device types:**
- `autoblow-ultra` — fully supported
- Other values — may be unsupported; verify before enabling device control

**Error responses:**
- `401` — invalid or expired device token
- `403` — token not authorized
- `502` — device offline or unreachable
- Timeout — network failure or device unreachable

---

### 2. Device Information

| Aspect | Value |
|---|---|
| Method | `GET` |
| Path | `/autoblow/info` |
| Body | None |
| Purpose | Retrieve device metadata and capabilities |

**Response:**
```json
{
  "deviceType": "autoblow-ultra",
  "firmwareVersion": "...",
  "serialNumber": "..."
}
```

**Error responses:** Same as Check Connection.

---

### 3. Device State

| Aspect | Value |
|---|---|
| Method | `GET` |
| Path | `/autoblow/state` |
| Body | None |
| Purpose | Current device operational state and script position |

**Response:**
```json
{
  "deviceState": "SYNC_SCRIPT_PAUSED",
  "syncScriptPositionMs": 12345,
  "syncScriptToken": "abc123..."
}
```

**Known device states:**
- `IDLE` — no script loaded
- `SYNC_SCRIPT_LOADED` — script loaded, ready to play
- `SYNC_SCRIPT_PAUSED` — script loaded and paused
- `SYNC_SCRIPT_PLAYING` — script actively playing
- `ERROR` — device in error state

**Error responses:** Same as Check Connection.

---

### 4. Upload Funscript

| Aspect | Value |
|---|---|
| Method | `PUT` |
| Path | `/autoblow/sync-script/upload-funscript` |
| Content-Type | `multipart/form-data` |
| File part name | `file` |
| Purpose | Upload a validated `.funscript` to the device cloud |

**Request:**
```
POST /autoblow/sync-script/upload-funscript
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary...

------WebKitFormBoundary
Content-Disposition: form-data; name="file"; filename="script.funscript"
Content-Type: application/json

{
  "actions": [
    { "at": 0, "pos": 0 },
    { "at": 500, "pos": 100 },
    { "at": 1000, "pos": 0 }
  ]
}
------WebKitFormBoundary--
```

**Response:**
```json
{
  "success": true,
  "syncScriptToken": "abc123..."
}
```

**Constraints:**
- Max actions: 100,000
- Max timestamp: 16,777,214 ms
- Max file size: 60 MB
- Timestamps must be in ascending order
- Positions must be 0–100
- Allow up to 60 seconds for upload + processing
- After upload, poll `/state` until `SYNC_SCRIPT_LOADED` or `SYNC_SCRIPT_PAUSED`

**Error responses:**
- `400` — malformed script or invalid actions
- `401` / `403` — authentication failure
- `429` — rate limited
- `502` — device offline

---

### 5. Load Uploaded Script

| Aspect | Value |
|---|---|
| Method | `PUT` |
| Path | `/autoblow/sync-script/load-token` |
| Body | JSON with `syncScriptToken` |
| Purpose | Load a previously uploaded script by token |

**Request:**
```json
{
  "syncScriptToken": "abc123..."
}
```

**Response:**
```json
{
  "success": true
}
```

**Error responses:**
- `400` — invalid token
- `401` / `403` — authentication failure

---

### 6. Start or Seek Script

| Aspect | Value |
|---|---|
| Method | `PUT` |
| Path | `/autoblow/sync-script/start` |
| Body | JSON with `startTimeMs` |
| Purpose | Start script playback or seek to position |

**Request:**
```json
{
  "startTimeMs": 24000
}
```

**Response:**
```json
{
  "success": true
}
```

**Notes:**
- `startTimeMs` is in milliseconds
- Use this to sync device to current video position
- Calling while already playing seeks to the new position
- Account for network latency — device does not start instantly

**Error responses:**
- `400` — invalid timestamp or no script loaded
- `401` / `403` — authentication failure

---

### 7. Pause Script

| Aspect | Value |
|---|---|
| Method | `PUT` |
| Path | `/autoblow/sync-script/stop` |
| Body | **None** — empty body |
| Purpose | Pause / stop script playback on device |

**Request:**
```
PUT /autoblow/sync-script/stop
(no body)
```

**Response:**
```json
{
  "success": true
}
```

**Critical:** Do NOT send an empty JSON body — this endpoint expects no body at all.

**Error responses:**
- `400` — no script loaded
- `401` / `403` — authentication failure

---

### 8. Set Timing Offset

| Aspect | Value |
|---|---|
| Method | `PUT` |
| Path | `/autoblow/sync-script/offset` |
| Body | JSON with `offsetTimeMs` |
| Purpose | Adjust device script timing relative to video |

**Request:**
```json
{
  "offsetTimeMs": 100
}
```

**Notes:**
- Positive offset: device runs ahead of video
- Negative offset: device runs behind video
- Use to compensate for measured drift

**Error responses:**
- `400` — invalid offset
- `401` / `403` — authentication failure

---

### 9. Set Script Looping

| Aspect | Value |
|---|---|
| Method | `PUT` |
| Path | `/autoblow/sync-script/loop` |
| Body | JSON with `isLooping` |
| Purpose | Enable/disable script loop mode |

**Request:**
```json
{
  "isLooping": false
}
```

**Response:**
```json
{
  "success": true
}
```

**Error responses:**
- `400` — invalid payload
- `401` / `403` — authentication failure

---

## Rate Limiting

- Autoblow API enforces rate limits
- Check `Retry-After` header on `429` responses
- Do not poll faster than necessary
- Recommended: poll `/state` every 2–5 seconds
- Implement exponential backoff on retry

## Common Error Codes

| Code | Meaning | Handling |
|---|---|---|
| `400` | Bad request | Validate payload before retry |
| `401` | Unauthorized | Token invalid or expired — prompt user |
| `403` | Forbidden | Token not authorized — prompt user |
| `429` | Rate limited | Wait for `Retry-After` header, back off |
| `502` | Bad gateway | Device offline — retry with backoff |
| Timeout | Network failure | Retry once, then report to user |

## Funscript Format

```json
{
  "actions": [
    { "at": 0, "pos": 0 },
    { "at": 500, "pos": 100 },
    { "at": 1000, "pos": 0 }
  ]
}
```

| Field | Type | Range | Description |
|---|---|---|---|
| `actions[].at` | integer | 0 – 16,777,214 | Timestamp in ms |
| `actions[].pos` | number | 0 – 100 | Device position percentage |

**Constraints:**
- Max 100,000 actions
- Timestamps must be in ascending order
- File size limit: 60 MB
- Optional metadata fields (`name`, `description`, etc.) are supported but not required

## Verified Endpoints

| Endpoint | Status | Evidence |
|---|---|---|
| `GET /autoblow/connected` | Verified | Official docs + unit tests |
| `GET /autoblow/info` | Verified | Official docs + unit tests |
| `GET /autoblow/state` | Verified | Official docs + unit tests |
| `PUT /upload-funscript` | Verified | Official docs + unit tests |
| `PUT /load-token` | Verified | Official docs + unit tests |
| `PUT /start` | Verified | Official docs + unit tests |
| `PUT /stop` (no body) | Verified | Official docs + unit tests |
| `PUT /offset` | Verified | Official docs + unit tests |
| `PUT /loop` | Verified | Official docs + unit tests |
