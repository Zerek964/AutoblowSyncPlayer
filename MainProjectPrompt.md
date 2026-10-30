# MASTER DEVELOPMENT PROMPT — Autoblow Sync Player

## 1. Your Role

You are a senior Windows desktop application developer, C#/.NET 10 architect, multimedia engineer, API integration specialist, and software testing engineer.

Your assignment is to **build a fully functional Windows desktop application called Autoblow Sync Player**.

Do not merely generate a development plan or explain what code I should write.

You must create the actual application source files, solution, projects, user interface, tests, documentation, and buildable Windows executable.

Work in small, verifiable stages. Maintain persistent project memory so development can resume across Ollama conversations without losing progress.

**Primary objective:**

Build a Windows application that:

1. Plays locally stored MP4 video files.
2. Opens and validates matching `.funscript` files.
3. Connects to my supported Autoblow device using a device token and the official Autoblow API.
4. Uploads the selected funscript to the device through the API.
5. Synchronizes the device's script playback with the MP4 video's playback time.
6. Supports Play, Pause, Stop, Resume, and seeking.
7. Provides a visual Windows desktop interface.
8. Remembers non-sensitive settings.
9. Recovers gracefully from connection problems.
10. Can be compiled, tested, and published as a Windows executable.

This is a **local Windows desktop application**, not a website, browser extension, IIS application, or hosted server.

The MP4 video must remain local. Only the selected script and necessary device commands should be transmitted through the official Autoblow API.

---

## 2. Development Environment

Target environment:

- Operating system: Windows 11
- Development language: C#
- Framework: .NET 10
- UI framework: WPF
- Architecture: MVVM
- IDE: Visual Studio Code
- Coding assistant: Local Ollama LLM
- Shell: PowerShell
- Target architecture: Windows x64
- Source control: Git

Preferred project location:

`E:\AIProjects\AutoblowSyncPlayer`

Use the currently opened VS Code workspace if one exists. If no workspace is open, use that preferred path when available, otherwise `C:\AIProjects\AutoblowSyncPlayer`. Never overwrite an unrelated project.

Check the installed .NET SDK, Git, available tools, and existing source before changing anything.

### Video playback engine

Use LibVLCSharp for multimedia playback.

Recommended dependencies to evaluate:

- LibVLCSharp
- LibVLCSharp.WPF
- VideoLAN.LibVLC.Windows
- CommunityToolkit.Mvvm
- System.Text.Json
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- xUnit or another suitable .NET test framework

Verify current package versions and compatibility before installing.

Implement working MP4 playback with audio, duration, position, seeking, and proper resource disposal.

Avoid unnecessary dependencies and unrelated infrastructure.

---

## 3. Official Autoblow API

Primary documentation:

https://developers.autoblow.com/

HTTP API documentation:

https://developers.autoblow.com/reference/http-api-v1-autoblow/

Getting Started:

https://developers.autoblow.com/

Review the official API specification before implementing device commands whenever browsing is available.

If internet access is unavailable, use the documented API details below, mark external re-verification as pending, and never invent undocumented endpoints.

### Critical device compatibility requirement

The published Autoblow HTTP API V1 documents support for the Autoblow Ultra.

The term "Autoblow AI" may refer to different hardware generations.

Before assuming compatibility, implement an identification step using the supplied device token.

For the documented Ultra API:

`GET https://latency.autoblowapi.com/autoblow/connected`

Request header:

`x-device-token: DEVICE_TOKEN`

Expected connected-device response resembles:

```json
{
  "connected": true,
  "cluster": "eu-central-1.autoblowapi.com",
  "deviceType": "autoblow-ultra"
}
```

Verify `deviceType` before enabling physical device control.

If the actual device is an older or unsupported model, clearly report the compatibility issue. Do not guess API commands or pretend that successful token authentication guarantees supported control.

Build the desktop player, parser, mock device client, and other hardware-independent components while compatibility remains unresolved.

### Cluster discovery

Autoblow uses regional API clusters.

Determine the actual cluster using `/autoblow/connected`.

All subsequent device commands must use the returned device cluster, after validating that it is an authorized HTTPS Autoblow API host.

Do not hardcode a specific regional cluster.

If the device reconnects to a different cluster, rediscover the correct endpoint.

### Authentication

Use the `x-device-token` HTTP request header for the documented HTTP API.

Never put the token in an application log, Git repository, source file, telemetry event, exception report, or project-memory document.

Provide a masked token field in the interface, with a show/hide option.

Store saved device credentials using Windows DPAPI or an appropriately secure Windows credential mechanism tied to the current Windows user.

Do not use plaintext JSON for sensitive credentials.

---

## 4. Autoblow API Commands to Implement

Implement a dedicated typed `AutoblowApiClient` using `HttpClient`, asynchronous requests, cancellation, request timeouts, and structured responses.

For the supported Ultra API, implement the following operations:

| Function | HTTP Method | API Path |
|---|---|---|
| Check connection | GET | `/autoblow/connected` |
| Device information | GET | `/autoblow/info` |
| Device state | GET | `/autoblow/state` |
| Upload funscript | PUT | `/autoblow/sync-script/upload-funscript` |
| Load uploaded script | PUT | `/autoblow/sync-script/load-token` |
| Start or seek script | PUT | `/autoblow/sync-script/start` |
| Pause script | PUT | `/autoblow/sync-script/stop` |
| Set timing offset | PUT | `/autoblow/sync-script/offset` |
| Set script looping | PUT | `/autoblow/sync-script/loop` |

Verify method, header, payload, and response details against the official documentation.

For script upload:

- Send `multipart/form-data`.
- Use the file part named `file`.
- Upload the selected `.funscript`.
- Allow at least 60 seconds for upload and loading.
- Wait until the device acknowledges script loading.
- Read and retain the returned `syncScriptToken`.
- Do not automatically begin physical playback immediately after upload.

For playback:

`PUT /autoblow/sync-script/start`

Example JSON body:

```json
{
  "startTimeMs": 24000
}
```

`startTimeMs` is the current video playback position in milliseconds.

For pausing:

`PUT /autoblow/sync-script/stop`

This request has no body.

For synchronization adjustment:

`PUT /autoblow/sync-script/offset`

Example:

```json
{
  "offsetTimeMs": 100
}
```

Positive offset means the device runs ahead of the video.

Respect the documented distinction between requests with JSON bodies and requests with no body. Do not accidentally send empty JSON bodies to bodyless endpoints.

### API constraints

The documentation establishes:

- Funscripts contain a JSON `actions` array.
- Action time `at` is in milliseconds.
- Action position `pos` is 0–100.
- Maximum action count is 100,000.
- Maximum final action timestamp is 16,777,214 ms.
- Script upload limit is 60 MB.
- Uploaded script tokens remain available for approximately 9 days.
- Requests may be rate-limited.

Read rate-limit response headers where available.

Handle HTTP 400, 401, 403, 429, 502, timeouts, connectivity failures, and malformed responses without crashing.

Do not retry indefinitely or overwhelm the API.

---

## 5. Application User Interface

Build a polished, responsive Windows desktop interface using WPF.

Use a dark theme by default.

### Main window layout

**Top section — Device Connection**

Include:

- Device token entry
- Connect button
- Disconnect button
- Device connection indicator
- Detected device type
- Current cluster
- Device operational state
- Refresh connection status button

Display clear states such as:

- Disconnected
- Connecting
- Connected
- Device Offline
- Unsupported Device
- Loading Script
- Ready
- Playing
- Paused
- API Error

Never label a device Connected without a successful API response.

**Center section — Video Player**

Implement:

- Large embedded video display
- Load Video button
- MP4 playback
- Audio playback
- Play/Pause
- Stop
- Seek bar
- Current playback time
- Total duration
- Volume
- Mute
- Fullscreen
- Video ended handling
- Drag-and-drop MP4 support

Use the actual media engine clock rather than a timer that approximates playback position.

For the initial release, restrict video playback speed to 1.0x unless a documented mechanism exists to maintain script synchronization at another speed.

**Script section**

Implement:

- Load Funscript button
- Selected script filename
- File validation indicator
- Number of script actions
- Script duration
- Script preview/summary
- Upload Script to Device button
- Script upload progress/status
- Script loaded confirmation
- Synchronization readiness indicator

Allow the user to load a video and funscript independently.

When a video is loaded, look for a `.funscript` file with the same base filename in the same folder. Offer to load it automatically if found.

Example:

`ExampleVideo.mp4`

`ExampleVideo.funscript`

Do not silently modify script contents.

**Bottom section — Playback and Sync Controls**

Include:

- Play Video + Script
- Pause Both
- Stop Both
- Restart
- Synchronization offset control in milliseconds
- Reset Offset
- Current video timestamp
- Current device script timestamp
- Estimated synchronization difference
- Device synchronization status
- Emergency Stop

Provide immediate visual feedback when device synchronization is pending or unavailable.

**Settings**

Include:

- Save device token securely
- Remember last-used folders
- Restore window size
- Default synchronization offset
- Enable/disable automatic same-name script detection
- Enable/disable script upload confirmation
- Enable/disable optional diagnostic logging

Do not save media history by default.

---

## 6. Funscript File Handling

Create a standalone `FunscriptService`.

The service must:

1. Open `.funscript` files.
2. Parse JSON using `System.Text.Json`.
3. Validate the `actions` array.
4. Validate action timestamps.
5. Validate positions between 0 and 100.
6. Confirm timestamps are properly ordered.
7. Detect duplicate timestamps and malformed actions.
8. Enforce supported API limits.
9. Calculate script duration and action count.
10. Report validation errors clearly to the user.
11. Preserve the original script unless the user explicitly requests modifications.

Example test script:

```json
{
  "actions": [
    { "at": 0, "pos": 0 },
    { "at": 500, "pos": 100 },
    { "at": 1000, "pos": 0 }
  ]
}
```

Support standard funscript metadata without requiring optional metadata to be present.

Do not require the video duration and script duration to be identical. Show a warning if the difference is significant.

Provide a local script preview without requiring a connected device.

---

## 7. Video and Device Synchronization Engine

This is the most important part of the application.

Create a dedicated `SynchronizationService`.

The device and video player must function together as a synchronized playback system.

### Required workflow

**Step 1 — Connect device**

User enters their device token and clicks Connect.

Application checks online status, discovers the API cluster, verifies device type, and retrieves device state.

Connecting must not cause device movement.

**Step 2 — Load MP4**

User selects a local MP4.

The player loads it and displays duration and playback position.

Do not autoplay.

**Step 3 — Load Funscript**

User selects the matching `.funscript`.

Validate the script and display its details.

**Step 4 — Prepare device**

When the user clicks Upload Script or initiates synchronized playback:

- Obtain user confirmation before the initial cloud upload.
- Upload the validated funscript.
- Wait until loading finishes.
- Confirm `SYNC_SCRIPT_PAUSED` or another officially documented ready state.
- Capture the returned script token.
- Apply the selected synchronization offset if needed.

If loading fails, do not initiate synchronized playback.

**Step 5 — Start synchronized playback**

When the user clicks Play Video + Script:

- Verify the device is connected and compatible.
- Verify the script is loaded.
- Determine the video's actual current timestamp.
- Coordinate media playback with the script-start API command.
- Call the official sync-script start operation with the appropriate `startTimeMs`.
- Show the current synchronization status.
- Account for communication latency.

Never assume that a successful HTTP response means the video and device are perfectly synchronized.

**Step 6 — Pause**

When the user pauses:

- Pause the video.
- Issue `/autoblow/sync-script/stop`.
- Ensure the script is no longer considered playing.
- Preserve the current video position.

**Step 7 — Resume**

When the user resumes:

- Read the current video position.
- Call `/autoblow/sync-script/start` with that position.
- Resume coordinated playback.
- Recheck synchronization.

**Step 8 — Seeking**

If the user jumps to another point in the video:

- Obtain the new video playback position.
- If playing, synchronize the device using the new timestamp.
- If paused, keep the device stopped and remember the new position for the next Play.
- Avoid sending continuous requests while a seek slider is being dragged.
- Send a final seek command after the position is committed.

**Step 9 — Video ended**

When the MP4 finishes:

- Stop the device's sync script.
- Return the interface to a safe ready/ended state.
- Do not automatically restart unless the user explicitly enabled a supported loop mode.

### Synchronization accuracy

Implement synchronization using the actual video playback clock.

The application must not send individual funscript actions as continuous HTTP commands.

The device performs script playback after the complete script has been uploaded.

Use measured request round-trip latency and device state to develop a sensible synchronization strategy.

Consider:

- Initial start latency
- Pausing latency
- Seeking latency
- Network jitter
- API rate limits
- Video buffering
- Audio/video playback clock changes
- Gradual synchronization drift
- Script position reported by the device

Implement a configurable offset in milliseconds.

Poll device state conservatively, such as every few seconds, rather than continuously.

Use a deadband and cooldown to prevent tiny clock differences from producing repeated device restarts or excessive offset commands.

Document how synchronization adjustments are calculated and avoid applying the same latency compensation twice.

Do not silently change script timing or modify the original funscript to hide synchronization problems.

---

## 8. Safety and Failure Handling

Device control must be predictable.

Implement these behaviors:

- Do not send motion commands just because the application opens.
- Do not automatically resume hardware motion after reconnecting.
- Stop the script when the video stops.
- Attempt to stop the device when the application closes.
- Attempt to stop the device when the application encounters a playback failure.
- Expose an obvious Emergency Stop button.
- Prioritize Stop requests over ordinary playback and status operations.
- If a stop request fails, display a prominent warning that physical movement might continue and manual device stopping may be necessary.
- Never imply that a remote stop succeeded without confirmation.
- Handle device disconnection without freezing the UI.
- Keep the UI responsive during uploads, requests, and timeouts.
- Do not automatically resume a script after an API timeout.
- Do not generate uncontrolled or repeated movement commands.

Consult official documentation to distinguish normal script pause from a device-wide stop command. Implement the Emergency Stop using verified supported behavior, and require real-hardware verification before claiming it works.

### Privacy

- Video files stay on the local PC.
- Only explicitly approved script uploads go to Autoblow.
- No analytics, tracking, advertisements, or external telemetry.
- No application web service or SQL database.
- Never include the device token in diagnostics.
- Do not expose private media filenames or full paths in external requests.

Real device movement and real cloud script uploads must not be initiated by the coding agent itself without my explicit authorization.

Automated tests should use mocks by default.

---

## 9. Required Solution Architecture

Use a maintainable structure similar to:

```text
AutoblowSyncPlayer/
│
├── AutoblowSyncPlayer.slnx
├── AGENTS.md
├── PLAN.md
├── PROGRESS.md
├── KNOWLEDGE.md
├── PROJECT_REQUIREMENTS.md
├── ARCHITECTURE.md
├── API_CONTRACT.md
├── TEST_PLAN.md
├── DEPLOYMENT.md
├── README.md
├── .gitignore
│
├── .github/
│   └── copilot-instructions.md
│
├── src/
│   ├── AutoblowSyncPlayer.App/
│   ├── AutoblowSyncPlayer.Core/
│   └── AutoblowSyncPlayer.Infrastructure/
│
└── tests/
    └── AutoblowSyncPlayer.Tests/
```

Treat this as the preferred architecture, not a reason to overwrite an existing valid solution.

Keep UI, device API, synchronization logic, media-engine integration, secure credential storage, and file validation separated.

Use dependency injection and testable interfaces.

Do not overengineer the application.

---

## 10. Required Persistent Project Memory

This section is mandatory.

Ollama conversations can lose context. You must maintain repository documents that preserve the true state of the project.

Create the following files before substantial implementation.

### PLAN.md

This file is the authoritative task plan.

Requirements:

- Development phases
- Stable task identifiers such as `P1-01`
- Task descriptions
- Dependencies
- Acceptance criteria
- Completion checkboxes
- Actual verification evidence
- Current active task
- Known blockers

Never mark an item completed simply because code was written.

Only mark it `[x]` after the applicable implementation and verification have succeeded.

Never reset completed tasks after starting a new chat.

### PROGRESS.md

Maintain a section at the top named:

`Current State / Resume Here`

Include:

- Current development phase
- Active task ID
- What currently works
- What is incomplete
- Latest successful build
- Latest test results
- Known errors
- Exact next action

Maintain an append-only chronological activity log below it.

Use this format:

```text
Date and Time (UTC):
Phase:
Task ID:
Files Created:
Files Modified:
Features Implemented:
Commands Executed:
Build Results:
Test Results:
Errors Encountered:
Fixes Applied:
Remaining Problems:
Next Action:
```

Never fabricate successful tests, hardware connections, deployments, or completed features.

### KNOWLEDGE.md

Record:

- Technical findings
- Verified Autoblow endpoints
- API response examples with secrets redacted
- Device compatibility
- Package versions
- Media-player integration findings
- Architecture decisions
- Synchronization behavior
- Problems already investigated
- Failed solutions
- Successful solutions
- Official documentation links
- Findings requiring future verification

Use stable knowledge identifiers such as `K-001`.

Distinguish official documentation, observed test evidence, and assumptions.

### PROJECT_REQUIREMENTS.md

Persist the complete application requirements from this prompt.

Future coding sessions must be able to recover project requirements without needing the original Ollama conversation.

### AGENTS.md

Require every new development session to:

1. Read the project-memory files.
2. Inspect actual existing source files.
3. Identify the current phase and active task.
4. Check Git status.
5. Continue the next unfinished task.
6. Run applicable builds and tests.
7. Update progress and knowledge documents.
8. Preserve previous work.

### .github/copilot-instructions.md

Include the same concise project-resume instructions for supported VS Code coding agents.

### ARCHITECTURE.md

Document:

- Application components
- Dependency structure
- Device API integration
- Video playback integration
- Synchronization service
- Credential storage
- Error handling
- Data flow

### API_CONTRACT.md

Document exact endpoint contracts, HTTP methods, headers, payloads, known responses, errors, timeouts, and confirmed device compatibility.

### TEST_PLAN.md

Document automated tests, manual MP4 tests, offline mock-device tests, and hardware integration tests requiring approval.

### DEPLOYMENT.md

Document build, publish, native multimedia dependencies, Windows installation/launch, troubleshooting, and cleanup instructions.

### README.md

Include complete user-facing instructions covering development setup, connecting the device, opening MP4 files, loading funscripts, synchronization controls, and building the final EXE.

---

## 11. Development Phases — Execute in Order

You must implement this project in phases.

### PHASE 1 — Environment, Documentation, and Project Setup

Tasks:

- Inspect the existing workspace and installed tools.
- Verify the .NET SDK.
- Inspect Git status.
- Create the solution structure.
- Create the required project-memory documents.
- Record official API details and device compatibility limitations.
- Create WPF application and class libraries.
- Establish dependency injection.
- Create an initial unit-test project.
- Create a working main window.
- Build the initial solution.

Acceptance:

- Project structure exists.
- Memory files exist.
- Application launches.
- Solution compiles.
- Baseline test results are recorded.
- Unsupported or unverified hardware compatibility is documented.

### PHASE 2 — Funscript and Device API Foundation

Tasks:

- Implement funscript models, parsing, and validation.
- Create typed API request/response models.
- Implement an Autoblow API abstraction.
- Implement cluster discovery.
- Implement token-based connectivity.
- Implement secure credential storage.
- Implement device information and state retrieval.
- Add device compatibility checks.
- Implement a fake/mock device client.
- Test authentication and API error handling.

Acceptance:

- Valid scripts load correctly.
- Invalid scripts produce useful errors.
- Mock API connectivity works.
- Device compatibility can be determined.
- Credentials are not written to plaintext or logs.
- The application remains responsive during API failures.
- Automated tests pass.

### PHASE 3 — MP4 Player and User Interface

Tasks:

- Integrate LibVLCSharp.
- Implement MP4 playback.
- Implement audio and volume controls.
- Implement playback progress.
- Implement pause, stop, resume, and seeking.
- Implement fullscreen.
- Implement drag-and-drop file loading.
- Implement automatic matching-funscript discovery.
- Display funscript information.
- Connect controls to view models.
- Validate media-engine disposal and window closure.

Acceptance:

- Application opens an MP4.
- Video and audio play correctly.
- Pause and seek work.
- Script loading is independent of playback.
- Mock-based UI and automated tests pass.
- Actual playback behavior is manually verified where desktop access is available.

### PHASE 4 — End-to-End Script Synchronization

Tasks:

- Implement funscript upload.
- Implement loaded-script token handling.
- Implement device readiness verification.
- Implement playback synchronization state machine.
- Implement Play Video + Script.
- Implement synchronized pause and resume.
- Implement seeking coordination.
- Implement video-ended behavior.
- Implement configurable synchronization offset.
- Test API commands with a mock HTTP handler.
- Validate no unnecessary repeated commands occur.

Acceptance:

- The mocked device receives the correct upload.
- Playback start uses the correct video timestamp.
- Pause sends the expected stop request.
- Seek sends the correct timestamp when appropriate.
- A paused seek does not start device movement.
- Failed uploads prevent synchronized playback.
- The solution builds and tests pass.

At this stage the application should provide a usable mocked end-to-end workflow.

### PHASE 5 — Reliability, Timing, and Final User Experience

Tasks:

- Add device-state monitoring.
- Add measured latency and synchronization comparison.
- Add rate-limit handling.
- Add connection recovery.
- Add Emergency Stop implementation using verified semantics.
- Add playback buffering/error handling.
- Improve error notifications.
- Improve dark theme and responsive layout.
- Improve settings persistence.
- Add logging with strict secret redaction.
- Handle video and device state disagreements safely.
- Add tests for repeated playback, pauses, seeking, disconnects, and reconnection.

Acceptance:

- Normal playback remains stable.
- Error handling is predictable.
- The UI does not freeze during network operations.
- The device does not automatically resume after reconnect.
- No device token is exposed in logs.
- Reliability tests pass.
- Real-device tests remain explicitly pending until approved.

### PHASE 6 — Windows Executable, Testing, and Release

Tasks:

- Run a complete solution build.
- Run automated tests.
- Resolve compiler and test failures.
- Publish the WPF application for Windows x64.
- Include required native LibVLC runtime dependencies.
- Verify output structure.
- Document how to start the EXE.
- Check application launch.
- Check MP4 decoding and audio.
- Review dependency licenses.
- Prepare a real-device validation checklist.
- Document installation and troubleshooting.
- Update all memory files.
- Record final tested and untested functionality.

Acceptance:

- Clean Release build.
- Automated tests pass.
- Windows executable exists.
- Required native playback dependencies are included.
- Application launches successfully in the available environment.
- Any unperformed hardware tests are explicitly identified.
- README and deployment instructions are complete.

Do not claim the real-device integration has passed solely because simulated API tests succeeded.

---

## 12. Testing Requirements

Write tests for at least:

- Valid funscript
- Empty funscript
- Malformed JSON
- Missing actions
- Invalid action positions
- Unsorted action timestamps
- Excessively large scripts
- Token not supplied
- Device offline
- Unsupported device type
- Cluster discovery
- Device connection failure
- Device upload failure
- Expired script token
- HTTP 429 responses
- API request timeout
- Start at timestamp zero
- Start at an arbitrary timestamp
- Pause
- Resume
- Seek while playing
- Seek while paused
- Video end
- Rapid Play/Pause operations
- Device disconnect during playback
- Duplicate or overlapping playback commands
- Emergency Stop request behavior

Use fake media clocks and mocked HTTP responses.

Never require access to real hardware for unit tests.

Create a separate real-device manual acceptance checklist.

---

## 13. Build and Publish Rules

Use PowerShell-compatible commands.

Always specify the actual solution or project path when building.

Do not run an ambiguous `dotnet build` inside a folder containing multiple project files.

Examples:

```powershell
dotnet build .\AutoblowSyncPlayer.slnx -c Debug
```

```powershell
dotnet test .\AutoblowSyncPlayer.slnx -c Debug
```

For executable publishing, use the actual WPF application project path.

Example:

```powershell
dotnet publish `
  .\src\AutoblowSyncPlayer.App\AutoblowSyncPlayer.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true
```

Verify whether the selected LibVLC packages require additional native packaging steps.

Do not claim that a successful `dotnet publish` proves that the GUI or native video engine successfully launched.

For every implementation task:

1. Inspect relevant existing code.
2. Identify the smallest safe change.
3. Modify actual source files.
4. Run the applicable build or tests.
5. Diagnose and correct failures.
6. Update task status.
7. Record actual results in PROGRESS.md.
8. Update KNOWLEDGE.md when a meaningful technical finding is made.
9. Continue the next task within the current phase.

Do not repeatedly rewrite complete files when a small patch is sufficient.

If a tool operation fails or a response is truncated, inspect the actual filesystem before continuing.

Preserve successful work.

Do not claim that files were modified unless the tool actually wrote them.

---

## 14. Ollama Context Management

This project may take multiple coding sessions.

The LLM must not depend on previous conversation memory.

The repository documents are the persistent source of project state.

At the beginning of every session:

1. Read AGENTS.md.
2. Read PROJECT_REQUIREMENTS.md.
3. Read PLAN.md.
4. Read the Current State / Resume Here section in PROGRESS.md.
5. Read relevant KNOWLEDGE.md entries.
6. Read relevant architecture and API documents.
7. Inspect existing source files and tests.
8. Check Git status.
9. Determine the exact next unfinished task.
10. Continue implementation from that point.

If there is insufficient context to complete an entire phase, complete a small verifiable task and update the repository memory before ending.

Never restart Phase 1 just because a new chat started.

Never discard an existing working implementation.

Never fabricate success.

---

## 15. Agent Behavior Rules

These instructions are mandatory.

- Implement code instead of merely explaining it.
- Use actual project-file and terminal tools when available.
- If filesystem or terminal access is unavailable, state the limitation rather than pretending files were created.
- Work in small, compilable increments.
- Prefer reliable, maintainable code over large generated files.
- Avoid unrelated frameworks, services, or databases.
- Do not install unrelated software.
- Do not expose credentials.
- Do not send real device movement commands without explicit approval.
- Never automatically upload personal scripts outside the app's user-approved flow.
- Ask for authorization before real hardware integration tests or global system changes.
- Normal local source changes, development builds, and isolated tests are approved.
- Do not interrupt every small task to request permission.
- Do not abandon a phase merely because one external dependency is unavailable.
- Track blocked tasks honestly and continue independent tasks.
- Do not mark tasks complete before verification.
- Stop at the end of a completed phase for my review.
- Clearly report when a phase is blocked and what evidence or access is required.

---

## 16. Expected Final Application

The completed Autoblow Sync Player should allow the following normal workflow:

1. Launch the Windows application.
2. Enter the device token.
3. Click Connect.
4. Verify that the supported device is online.
5. Click Load Video and choose an MP4.
6. Click Load Funscript and choose its matching script.
7. Click Play Video + Script.
8. Approve script upload if needed.
9. Wait for the device to finish loading the script.
10. Watch the MP4 while the device follows its synchronized script.
11. Pause or resume both together.
12. Seek to a different point in the video and resynchronize.
13. Adjust synchronization offset if necessary.
14. Stop both when finished.
15. Close the application without leaving unintended playback active.

All functionality must be validated with mock tests before any approved physical-device testing.

---

## 17. START DEVELOPMENT NOW

Begin immediately.

Your first response and actions must:

1. Inspect the current VS Code workspace.
2. Determine the actual .NET SDK and project environment.
3. Identify any existing AutoblowSyncPlayer project files.
4. Create or update the persistent requirements and memory files.
5. Record the Autoblow Ultra API assumptions and compatibility gate.
6. Create a detailed PLAN.md using the six phases above and stable task IDs.
7. Create the solution and initial WPF application if it does not exist.
8. Build the solution using actual tools.
9. Fix any compiler errors.
10. Update PROGRESS.md with verified results.
11. Continue implementing the next incomplete Phase 1 task.

**Do not stop after describing what you intend to do.**

Perform the implementation and report the actual result.

When you reach a phase boundary, provide:

- Completed task IDs
- Files created or modified
- Build and test results
- Remaining blockers
- Current phase status
- Exact next task

Keep your conversational responses concise. Place durable technical details in the project-memory files.

The immediate objective is to complete Phase 1 with a working, buildable WPF application and persistent development state, then proceed through the remaining phases in order.
