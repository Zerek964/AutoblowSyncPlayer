# AGENTS.md — Development Session Resume Instructions

## Before Starting Any Work

**Read these files in order:**

1. `MainProjectPrompt.md` — Complete project requirements and specifications.
2. `PROJECT_REQUIREMENTS.md` — Persisted requirements summary.
3. `VR3D_PLAN.md` — **CRITICAL** — Master plan for 3D/VR video playback expansion (7 new phases).
4. `VR3D_REQUIREMENTS.md` — Detailed VR3D requirements (video modes, camera, sync preservation).
5. `VR3D_ARCHITECTURE.md` — VR3D architecture design (new interfaces, single clock, thread safety).
6. `VR3D_TEST_PLAN.md` — VR3D test strategy (automated + manual tests).
7. `.github/copilot-instructions.md` — Coding standards and architecture rules.
8. `docs/ARCHITECTURE.md` — Application component and dependency structure.
9. `docs/API_CONTRACT.md` — Autoblow API endpoint contracts.
10. `docs/DEPLOYMENT.md` — Build, publish, and deployment instructions.
11. `docs/TEST_PLAN.md` — Current test coverage and test strategy.

## Identify Current State

- Check Git status: `git status`
- Check latest build: `dotnet build AutoblowSyncPlayer.sln`
- Check latest tests: `dotnet test AutoblowSyncPlayer.sln`
- Review `PROGRESS.md` for the most recent activity log and current phase.
- Review repository memory files (`/memories/repo/`) for phase completion status.
- Review `PLAN.md` for VR3D expansion status and phase tracking.

## Development Rules

- **Preserve previous work.** Do not overwrite or delete existing implementations unless explicitly asked.
- **Continue the next unfinished task.** Do not restart completed phases.
- **Run applicable builds and tests** after every significant change.
- **Update progress and knowledge documents** after completing each task.
- **Use the layered architecture:** Core (abstractions) → Infrastructure (implementations) → App (UI).
- **Never commit secrets.** Do not put device tokens, API keys, or credentials in source files, logs, or memory documents.
- **Use mock devices by default.** Never initiate real device movement or real cloud uploads without explicit user authorization.
- **Keep the UI responsive.** All I/O operations must be async/await.
- **Follow C# 12 conventions.** Use primary constructors, collection expressions, and other C# 12 features where appropriate.

## VR3D Development Rules

- **Camera movement must NEVER interrupt video playback or funscript synchronization.** This is the #1 constraint for all VR3D work.
- **Single media clock rule:** `PositionMs` from the video player is the single source of truth for sync. Camera controller must NEVER affect video timeline.
- **Architectural isolation:** Camera controller must have NO access to sync-related methods or properties.
- **Existing test preservation:** All 89 existing tests must continue passing after any VR3D code changes.
- **LibVLC viewpoint API:** Use `MediaPlayer.UpdateViewpoint(yaw, pitch, roll, fov, absolute)` for 360° camera control.
- **Thread safety:** Camera updates run on UI thread (same thread as MediaPlayer in WPF).
- **Video mode detection:** Implement auto-detection with manual override UI for edge cases.

## Project Context

- **Framework:** .NET 10 (target `net10.0` for libraries, `net10.0-windows` for WPF)
- **UI:** WPF with MVVM (CommunityToolkit.Mvvm)
- **Video:** LibVLCSharp 3.9.4 + VideoLAN.LibVLC.Windows 3.0.21
- **Testing:** xUnit 2.9.3, FluentAssertions 8.1.0, Moq 4.20.72
- **Solution:** Traditional `.sln` format (4 projects)
- **IDE:** Visual Studio Code on Windows 11
- **Shell:** PowerShell
- **VR3D Status:** Phase 1 complete (documentation/planning), Phase 2 pending (Core layer implementations)

## After Completing Work

1. Update `PROGRESS.md` with the activity log entry.
2. Update `KNOWLEDGE.md` with any new technical findings.
3. Update `VR3D_TEST_PLAN.md` if VR3D test coverage changed.
4. Update `PLAN.md` if phase status changed.
5. Ensure `dotnet build` and `dotnet test` both pass.
