\# AUTOBLOW SYNC PLAYER — 3D/VR PLAYBACK EXPANSION



\## YOUR ROLE



You are a senior C#/.NET 10 developer, Windows WPF engineer, graphics programmer, 3D video rendering specialist, and multimedia synchronization engineer.



You are continuing development of an \*\*existing, working application named Autoblow Sync Player\*\*.



The application has already been developed and can:



\- Open and play local MP4 files.

\- Open and validate funscript files.

\- Connect to my Autoblow device through its API.

\- Upload funscripts to the device.

\- Play video while the device executes the synchronized script.

\- Control playback, pause, stop, and seeking.



\*\*These features are already working. Do not rebuild or replace them.\*\*



Your new assignment is to extend the existing application with \*\*3D, VR180, and VR360 video playback with an interactive, mouse-controlled camera\*\*.



I want to load an immersive 3D video, move the camera around inside the video window, zoom in and out, and change the viewing angle while the video and Autoblow funscript continue playing normally.



The application must retain all existing functionality.



This project uses a local Ollama coding agent in Visual Studio Code. Your task is to inspect the actual project, create an additional development plan, implement code in phases, run builds and tests, and update all existing project-memory documents.



Do not answer with only a plan or code examples. Make actual source changes using your available tools.



\---



\# PART 1 — MANDATORY PROJECT DISCOVERY



\## 1.1 Read all existing project documentation before changing code



Your first task is to inspect the entire existing workspace.



Locate and read every relevant project documentation file, including:



\- `AGENTS.md`

\- `PLAN.md`

\- `PROGRESS.md`

\- `KNOWLEDGE.md`

\- `PROJECT\_REQUIREMENTS.md`

\- `ARCHITECTURE.md`

\- `API\_CONTRACT.md`

\- `TEST\_PLAN.md`

\- `DEPLOYMENT.md`

\- `README.md`

\- `.github/copilot-instructions.md`



Also locate and read any additional files containing PLAN, PROGRESS, PHASE, ROADMAP, TODO, DEVELOPMENT, or REQUIREMENTS in their filenames.



Read relevant documentation from subfolders and previous phases, not just the root directory. Exclude generated build folders such as `bin`, `obj`, and `.git` from recursive searches.



Do not assume file paths or project structure.



You must establish:



1\. The current solution and project structure.

2\. Which development phases have already been completed.

3\. The application's current playback implementation.

4\. Which media library is actually being used.

5\. How the video clock operates.

6\. How the Autoblow synchronization service works.

7\. How the video view is displayed in WPF.

8\. How application settings are saved.

9\. How fullscreen mode is implemented.

10\. Which automated tests currently exist.

11\. What known problems remain.

12\. Whether there are uncommitted Git changes.



Read the relevant source code, not just documentation.



\### Important



Do not create a new application.



Do not initialize a replacement solution.



Do not remove working functionality.



Do not restart development phases that are already complete.



Do not overwrite the existing PLAN.md or erase old PROGRESS.md entries.



Preserve all previous work.



\---



\# PART 2 — CREATE A NEW 3D DEVELOPMENT PLAN



Before implementing the 3D functionality, create the following additional files in the existing repository root:



```text

VR3D\_PLAN.md

VR3D\_REQUIREMENTS.md

VR3D\_ARCHITECTURE.md

VR3D\_TEST\_PLAN.md

```



\## 2.1 VR3D\_PLAN.md



Create a detailed implementation plan for adding immersive video playback to the existing application.



Use numbered development phases starting after the existing completed phases.



If the original project ended at Phase 6, begin at Phase 7. Otherwise continue after its actual highest phase number.



Do not duplicate existing phase IDs.



Use unique task identifiers:



\- VR-P01-01

\- VR-P01-02

\- VR-P02-01

\- VR-P02-02



Each task must contain:



\- Description

\- Required source changes

\- Dependencies

\- Acceptance criteria

\- Test requirements

\- Completion checkbox

\- Actual verification evidence

\- Any known blockers



Only check a task complete after it is implemented and verified.



\## 2.2 Update existing PLAN.md



Add a new section named:



\*\*3D/VR Video Playback Expansion\*\*



Include a reference to `VR3D\_PLAN.md`, a summary of the feature, the current phase, and a pointer to unfinished tasks.



Do not modify or remove previously completed tasks.



\## 2.3 Update existing PROGRESS.md



Preserve the previous development history.



Update the existing Current State / Resume Here section.



Append a new timestamped entry documenting the start of the 3D expansion, the original application baseline, and the additional development plan.



Do not fabricate successful testing or implementation.



\## 2.4 Update existing KNOWLEDGE.md



Add verified findings covering:



\- LibVLCSharp capabilities

\- Existing media renderer

\- 360° video playback

\- VR180 video playback

\- Stereoscopic video layouts

\- Camera controls

\- GPU rendering

\- WPF rendering limitations

\- Performance considerations

\- Integration with the existing synchronization service



Use official source URLs, applicable versions, and unique knowledge IDs.



Distinguish documented behavior from locally tested behavior and assumptions.



\## 2.5 Update AGENTS.md



Add instructions requiring every future coding session to read the new VR3D files along with the original project memory.



Do not replace existing instructions.



\---



\# PART 3 — NEW VIDEO PLAYBACK REQUIREMENTS



\## 3.1 Required Video Types



Extend the existing video player to support immersive and stereoscopic video formats.



The initial target formats are:



| Video Type | Required Behavior |

|---|---|

| Standard 2D MP4 | Preserve existing playback |

| 3D Side-by-Side (SBS) | Correctly select/display eye imagery |

| 3D Top/Bottom (Over-Under) | Correctly select/display eye imagery |

| VR180 Equirectangular | Interactive 180-degree viewpoint |

| VR180 SBS | Interactive viewpoint with stereo eye separation |

| VR180 Over-Under | Interactive viewpoint with stereo eye separation |

| VR360 Equirectangular | Interactive 360-degree viewpoint |

| VR360 SBS | Interactive 360-degree viewpoint with stereo eye handling |

| VR360 Over-Under | Interactive 360-degree viewpoint with stereo eye handling |

| VR180 Dual-Fisheye | Implement after proving correct mapping, or clearly identify as unsupported until implemented |



Support MP4 and other media containers already compatible with the existing video engine, such as MKV, where practical.



These requirements concern \*\*immersive or stereoscopic video files\*\*, not 3D mesh models such as OBJ, FBX, or GLB.



\### Important format distinctions



A standard flat stereoscopic SBS video is not necessarily a VR180 or VR360 video.



Do not assume SBS means spherical.



On a regular desktop monitor, selecting one eye provides a correctly proportioned mono view, not true stereoscopic depth. Provide clearly labeled left-eye and right-eye modes.



Do not falsely claim that selecting one eye produces stereoscopic depth.



VR180 and VR360 content requires the appropriate spherical projection, not simple image cropping or stretching.



Fisheye formats require the appropriate lens/projection mapping.



Do not mark a format supported until its geometry and layout have been verified.



\---



\# PART 4 — INTERACTIVE CAMERA SYSTEM



This is the highest-priority new feature.



The video player must allow me to change the viewing direction while the movie plays.



\## 4.1 Mouse Camera Movement



When 3D/VR mode is enabled, implement these controls:



| Action | Required Behavior |

|---|---|

| Left mouse button + drag left/right | Rotate camera horizontally |

| Left mouse button + drag up/down | Rotate camera vertically |

| Mouse wheel up | Zoom in |

| Mouse wheel down | Zoom out |

| Double-click video | Enter/exit fullscreen where compatible with current controls |

| R key | Reset camera to default viewpoint |

| Arrow keys | Rotate camera |

| + / - keys | Zoom in/out |

| Escape | Exit fullscreen |



Dragging must move the \*\*virtual camera viewpoint\*\*, not drag the video frame like a flat image.



Mouse sensitivity must be adjustable.



Provide an optional Invert Vertical Mouse Movement setting.



Support smooth camera movement without noticeable stuttering.



The user must be able to:



\- Look left

\- Look right

\- Look up

\- Look down

\- Zoom in

\- Zoom out

\- Reset to the original view

\- Continue watching while changing camera angles



For a 360° video, support horizontal rotation through the complete panorama.



For a 180° video, respect the actual field of coverage and avoid displaying unrecorded portions as though they were valid video.



Clamp vertical and horizontal movement appropriately.



\## 4.2 Camera Movement While Playing



\*\*Critical requirement: camera movement must not interrupt video playback or funscript synchronization.\*\*



When I drag the mouse:



\- The video must continue playing.

\- The audio must continue playing.

\- The playback timestamp must continue advancing.

\- The funscript must continue executing.

\- The device must remain connected.

\- No new script upload should occur.

\- The synchronization service must not restart.

\- No start, stop, pause, or seek API commands should be generated just because the camera moved.



The camera is a viewing function, independent from the playback timeline.



Camera control is not a video seek operation.



\## 4.3 Camera Movement While Paused



If I pause the video:



\- Keep the video paused.

\- Keep the device's script stopped according to existing behavior.

\- Allow camera rotation on the displayed frame if the renderer supports it.

\- Do not resume playback simply because I rotate the camera.



\## 4.4 Camera Settings



Provide:



\- Horizontal angle (yaw)

\- Vertical angle (pitch)

\- Optional roll

\- Field of view (FOV)

\- Mouse sensitivity

\- Invert Y-axis

\- Reset camera

\- Default viewpoint

\- Optional movement smoothing



Show camera settings only where appropriate.



Save non-sensitive preferred camera settings locally.



\---



\# PART 5 — 3D/VR VIDEO MODE SELECTOR



Add a Video Mode selector to the existing application's user interface.



Options should include:



```text

Auto Detect

Standard 2D

Flat 3D SBS — Left Eye

Flat 3D SBS — Right Eye

Flat 3D Over-Under — Left Eye

Flat 3D Over-Under — Right Eye

VR180 Mono

VR180 SBS

VR180 Over-Under

VR360 Mono

VR360 SBS

VR360 Over-Under

VR180 Fisheye (only when supported)

```



Use a sensible interface that groups related choices rather than overwhelming the main screen.



\## 5.1 Automatic Detection



When opening a video, inspect available media metadata to detect:



\- Spherical projection

\- Stereo packing

\- Video resolution

\- Aspect ratio

\- Relevant VR projection metadata

\- Available video tracks



Use only reliable metadata to automatically choose a mode.



Filenames may be used to suggest a mode, but must not be treated as proof.



If detection is ambiguous, allow the user to manually select a mode.



Do not make a mode appear supported if the corresponding renderer has not been implemented.



\## 5.2 Switching Modes



Allow switching video display modes without reloading or re-uploading the funscript.



Prefer renderer changes that preserve the same media clock.



If a particular transition cannot be performed seamlessly, document its limitations and provide a safe transition that does not trigger unintended device motion.



\---



\# PART 6 — GRAPHICS AND MEDIA RENDERING



\## 6.1 Reuse Existing Playback Infrastructure



Inspect the currently implemented media engine first.



If LibVLCSharp is already being used, investigate its built-in 360° video capabilities before introducing another graphics library.



Official reference:



https://docs.videolan.me/libvlcsharp/api/LibVLCSharp.Shared.MediaPlayer.html



LibVLCSharp provides:



```csharp

MediaPlayer.UpdateViewpoint(

&#x20;   float yaw,

&#x20;   float pitch,

&#x20;   float roll,

&#x20;   float fov,

&#x20;   bool absolute = true

);

```



This can adjust the viewpoint for compatible spherical video playback.



Verify support using the versions and source code present in the actual application.



Check return values and verify visually that the viewpoint really changes.



Do not assume calling this method converts a flat SBS, VR180, or fisheye source into correctly projected video.



\## 6.2 Rendering Feasibility Investigation



Before significant modifications, create a small isolated proof of concept using the current media engine.



Prove that:



1\. A compatible 360° video can be displayed.

2\. Mouse input reaches the viewport.

3\. Camera orientation changes visibly.

4\. Playback continues without restarting.

5\. The existing media clock continues advancing.

6\. The application's WPF interface remains functional.



Evaluate whether current LibVLCSharp rendering can support each required format.



For formats not supported natively, evaluate an appropriate GPU-accelerated projection pipeline.



Possible technologies include:



\- Existing LibVLCSharp capabilities

\- Direct3D 11 rendering

\- GPU shader-based spherical projection

\- Vortice.Windows or another maintained .NET graphics interface

\- Hardware-accelerated video frame integration



Choose technologies based on a working proof of concept, not assumptions.



Do not introduce an entirely new media player without compelling verified reasons.



\## 6.3 Stereo and Spherical Rendering



For immersive stereo sources, investigate and implement:



\- Correct left/right eye extraction

\- Side-by-side image packing

\- Over-under image packing

\- Full equirectangular projection

\- Half-equirectangular 180-degree projection

\- Fisheye mapping where actual source geometry and metadata permit

\- Correct aspect ratios

\- Proper texture coordinates

\- Camera orientation

\- Field-of-view adjustments



On the desktop screen, the initial priority is a correct, interactive viewport using a selectable eye.



True binocular stereoscopic output, anaglyph display, or VR headset support is optional future work and must not delay the essential mouse-controlled viewport.



Do not simply zoom or stretch a side-by-side image and call that immersive VR rendering.



For custom rendering, use an efficient hardware-accelerated approach where possible.



Avoid copying full decoded frames through managed memory on every frame if a practical GPU-based path exists.



\## 6.4 WPF Video Surface Integration



LibVLCSharp's WPF VideoView may use a native rendering window, which introduces input and overlay limitations.



Research and handle those limitations properly.



Ensure:



\- Mouse drag events reach the camera controller.

\- Wheel events can control FOV.

\- Fullscreen continues working.

\- Existing playback controls remain accessible.

\- No transparent overlay breaks mouse input.

\- Camera movement does not steal input from unrelated UI controls.

\- The video remains visible when resizing the window.

\- Graphics resources are released on media changes and application exit.



Use the most reliable implementation for the actual project.



\---



\# PART 7 — PRESERVE AUTOBLOW SYNCHRONIZATION



The current synchronized playback system must remain intact.



This is a strict requirement.



\## 7.1 One Playback Timeline



Maintain one authoritative media playback clock.



Do not create an independent timeline for the new 3D renderer.



The funscript synchronization engine must continue obtaining playback time from the existing authoritative clock.



Camera state and script state must remain separate.



Suggested conceptual architecture:



```text

Existing Video / Media Clock

&#x20;           |

&#x20;           +------ 3D Rendering System

&#x20;           |            |

&#x20;           |            +-- Projection

&#x20;           |            +-- Camera Yaw/Pitch

&#x20;           |            +-- Zoom / FOV

&#x20;           |            +-- Stereo Eye Selection

&#x20;           |

&#x20;           +------ Existing Synchronization Service

&#x20;                        |

&#x20;                        +-- Funscript

&#x20;                        +-- Autoblow API

&#x20;                        +-- Device Playback

```



Camera changes must not alter the position or timing of the existing video clock.



\## 7.2 Required Behavior



| User Action | Video | Device Script |

|---|---|---|

| Rotate camera | Continues | Continues |

| Zoom camera | Continues | Continues |

| Reset camera | Continues | Continues |

| Switch stereo eye | Continues if renderer permits | Continues |

| Pause video | Pauses | Stops/pauses as currently implemented |

| Resume video | Resumes | Resynchronizes |

| Seek video | Seeks | Resynchronizes |

| Stop video | Stops | Stops |

| Video ends | Ends | Stops |



Do not change existing device request formats, API endpoints, credentials, or successful script loading behavior.



Only modify device synchronization code where integration demonstrably requires it.



\## 7.3 Integration Tests



Create tests proving that camera interaction does not trigger:



\- Script reuploads

\- Playback restarts

\- Video seeks

\- Device stop commands

\- Device start commands

\- Excessive API state requests



Use mocked device clients and a controlled media clock.



Real hardware is not required for automated tests.



\---



\# PART 8 — NEW USER INTERFACE



Modify the existing UI rather than creating another application window or a separate program.



\## 8.1 Add a 3D Control Area



Include a collapsible or compact group named:



\*\*3D / VR Controls\*\*



It should contain:



\- Video mode selector

\- Left/Right eye selection when appropriate

\- Mouse navigation enabled indicator

\- Reset View button

\- Field-of-view slider

\- Mouse sensitivity adjustment

\- Invert Y toggle

\- Optional current yaw/pitch display

\- Fullscreen button



Keep the existing Autoblow controls visible and usable.



\## 8.2 Video Viewport



Make camera interaction intuitive.



While immersive playback is active, mouse dragging inside the video window should rotate the camera.



Allow returning to a neutral viewing position.



Avoid creating another unrelated player window.



The current media player should become capable of both regular 2D playback and immersive playback.



\## 8.3 Fullscreen



Camera rotation and zoom must also work in fullscreen mode.



Ensure:



\- Mouse interaction remains functional.

\- The view does not unexpectedly reset.

\- Escape exits fullscreen.

\- Existing playback keyboard shortcuts remain usable.

\- Returning to the normal window preserves playback position.



\---



\# PART 9 — DEVELOPMENT PHASES



Create the new implementation plan using the following phases.



Adapt the numbering to follow the actual existing PLAN.md. Preserve the phase names and task dependencies.



\## NEW PHASE 1 — Existing Application Audit and 3D Planning



Tasks:



\- Read all existing project-memory files.

\- Inspect source, Git status, media engine, and synchronization flow.

\- Record the existing working build and test baseline.

\- Identify where the player is created and how the media clock is exposed.

\- Identify WPF input and fullscreen limitations.

\- Create VR3D\_PLAN.md.

\- Create VR3D\_REQUIREMENTS.md.

\- Create VR3D\_ARCHITECTURE.md.

\- Create VR3D\_TEST\_PLAN.md.

\- Link the new plan into PLAN.md.

\- Update PROGRESS.md and KNOWLEDGE.md.



Acceptance:



\- Existing solution still builds.

\- No existing functionality is removed.

\- New phase plan is saved.

\- Existing and new documentation are consistent.

\- New feature architecture is documented.



\## NEW PHASE 2 — 360° Video and Camera Proof of Concept



Tasks:



\- Verify LibVLCSharp viewpoint capabilities.

\- Add a basic VR360 playback mode.

\- Implement camera yaw and pitch.

\- Implement mouse-drag camera control.

\- Implement mouse-wheel FOV.

\- Implement Reset View.

\- Implement keyboard viewpoint control.

\- Test during actual playback.

\- Verify camera movement does not change playback position.

\- Record supported renderer behavior.



Acceptance:



\- A compatible 360° video renders correctly.

\- I can move the view with my mouse.

\- Zoom works.

\- Video continues playing while I move the camera.

\- The existing funscript synchronization remains unchanged.

\- Appropriate tests pass.

\- Any manual visual verification not performed remains documented as pending.



\## NEW PHASE 3 — VR180 and Stereoscopic Playback



Tasks:



\- Implement SBS layout handling.

\- Implement Over-Under layout handling.

\- Implement correct left/right eye selection.

\- Implement VR180 equirectangular projection.

\- Implement VR360 stereo handling.

\- Investigate and implement dual-fisheye mapping where feasible.

\- Add manual format overrides.

\- Preserve correct image aspect ratios.

\- Validate representative file formats.

\- Fix projection and orientation issues.



Acceptance:



\- Supported stereoscopic videos no longer appear as two unprocessed images in the normal single-view viewport.

\- VR180 and VR360 viewpoints use correct geometry.

\- Switching eyes works when supported.

\- Manual mode selection is functional.

\- Unsupported fisheye/mesh formats are clearly identified instead of incorrectly displayed as supported.



\## NEW PHASE 4 — Full Camera UI and Fullscreen Integration



Tasks:



\- Add the 3D/VR Controls section.

\- Add video mode selection.

\- Add eye selection.

\- Add sensitivity controls.

\- Add camera angle controls.

\- Add FOV control.

\- Add reset camera.

\- Integrate WPF mouse events.

\- Ensure fullscreen mouse interaction.

\- Save camera preferences.

\- Fix UI focus and input conflicts.



Acceptance:



\- Mouse dragging is smooth and consistent.

\- Fullscreen camera controls work.

\- Reset restores the expected viewpoint.

\- Existing video controls still work.

\- Existing Autoblow UI remains functional.



\## NEW PHASE 5 — Funscript Synchronization Integration



Tasks:



\- Confirm a single media clock is used.

\- Test camera movement during synced playback.

\- Test zoom during synced playback.

\- Test pause and resume.

\- Test seeking.

\- Test switching video modes where supported.

\- Test fullscreen changes.

\- Test script completion.

\- Test device disconnect during playback.

\- Test video end behavior.

\- Add regression tests covering all existing playback functionality.



Acceptance:



\- Moving the camera does not interrupt synchronized device playback.

\- Zooming does not cause device commands.

\- Existing pause and resume remain functional.

\- Existing seeking synchronization remains functional.

\- No repeated script upload occurs because of camera movement.

\- All relevant existing and new tests pass.



\## NEW PHASE 6 — Performance and Rendering Optimization



Tasks:



\- Evaluate GPU acceleration.

\- Verify hardware video decoding where available.

\- Reduce unnecessary frame copying.

\- Profile CPU, GPU, memory, and frame rendering.

\- Test 1080p, 4K, and higher-resolution content where representative samples are available.

\- Test long playback sessions.

\- Test rapid camera movements.

\- Test repeated video loading.

\- Check memory disposal and GPU resource cleanup.

\- Fix rendering stutter and resource leaks.



Acceptance:



\- Interactive camera movement remains responsive on the available test hardware.

\- Regular video and audio playback are stable.

\- The renderer avoids unnecessary high-CPU operations.

\- Resource usage is measured and documented.

\- The application does not accumulate stale media resources after repeated playback.

\- Unsupported hardware or untested resolutions are reported accurately.



\## NEW PHASE 7 — Final Testing, Packaging, and Release



Tasks:



\- Run complete solution build.

\- Run all automated tests.

\- Run regression tests for normal 2D playback.

\- Test supported VR180/VR360 video formats.

\- Test mouse navigation.

\- Test fullscreen.

\- Test synchronized playback with a mock device.

\- Prepare approved real-hardware integration testing.

\- Update README.md.

\- Update deployment instructions.

\- Publish a Windows x64 release build.

\- Verify native dependencies.

\- Update all plan and progress files.



Acceptance:



\- Existing standard MP4 player still works.

\- Existing funscript loading and synchronization still work.

\- New supported immersive video modes work.

\- Camera navigation works during playback.

\- Release build completes.

\- All executed test results are documented.

\- Untested hardware scenarios remain explicitly labeled.



\---



\# PART 10 — PERFORMANCE REQUIREMENTS



Prioritize reliable playback and camera responsiveness.



The application should:



\- Use GPU-assisted rendering where supported.

\- Preserve hardware-accelerated decoding where practical.

\- Avoid excessive CPU usage for camera rotation.

\- Avoid unnecessary per-frame managed allocations.

\- Keep video/audio timing stable.

\- Keep device API operations independent of frame rendering.

\- Remain responsive while moving the mouse rapidly.

\- Properly release media and GPU resources.



Set realistic performance acceptance thresholds after examining the actual renderer and test hardware.



Do not claim 4K, 6K, or 8K performance without testing those resolutions.



Do not introduce performance optimizations that compromise device synchronization.



\---



\# PART 11 — REQUIRED TESTING



Create automated tests and manual acceptance tests covering:



\### Camera Tests



\- Rotate left

\- Rotate right

\- Rotate up

\- Rotate down

\- Zoom in

\- Zoom out

\- Reset view

\- Maximum/minimum FOV

\- Pitch clamping

\- 180-degree boundary handling

\- 360-degree wrapping

\- Camera movement during playback

\- Camera movement while paused

\- Fullscreen camera controls

\- Window resizing



\### Projection Tests



\- Standard 2D

\- 360 mono

\- 180 mono

\- SBS left eye

\- SBS right eye

\- Over-Under top eye

\- Over-Under bottom eye

\- VR180 stereo

\- VR360 stereo

\- Correct aspect ratio

\- Unsupported projection handling

\- Incorrect metadata

\- Missing metadata



\### Synchronization Tests



\- Video/script initial start

\- Camera rotation while script plays

\- Camera zoom while script plays

\- Camera reset while script plays

\- Pause and resume

\- Seek

\- Switch display mode

\- Video completion

\- Device disconnect

\- Script load failure

\- Repeated video loads

\- No extra API calls caused by camera changes



Keep real device commands disabled in automated tests.



Use mock media and API services wherever practical.



Do not add private user video files to the source repository.



\---



\# PART 12 — DEVELOPMENT RULES



Follow these rules throughout implementation.



1\. Do not start a new application.

2\. Do not replace the existing WPF application.

3\. Do not rewrite working Autoblow API code unnecessarily.

4\. Do not break standard MP4 playback.

5\. Do not break working funscript synchronization.

6\. Do not assume that all 3D file layouts are equivalent.

7\. Do not claim proper 3D projection based only on successful decoding.

8\. Do not install a new graphics framework without testing a smaller approach first.

9\. Do not create a second independent playback clock.

10\. Do not restart or reupload scripts when the camera moves.

11\. Do not destroy or overwrite previous plans and progress history.

12\. Do not mark phases complete without verification.

13\. Do not invent test results.

14\. Use existing project patterns and dependency injection.

15\. Use small, testable changes.

16\. Run builds and tests after meaningful implementation steps.

17\. Fix compiler errors before continuing.

18\. Maintain accurate project documentation.

19\. Do not run physical-device tests without authorization.

20\. Do not delete, reset, or overwrite uncommitted user changes.



If internet access is available, use current official documentation to verify APIs and graphics-library integration.



If not, record assumptions and verification blockers.



Stop at each completed phase boundary for my review. Continue autonomously through ordinary small development tasks within the active phase.



\---



\# PART 13 — PROJECT MEMORY AND CONTINUATION RULES



Ollama may reach its context limit during development.



Every new session must be able to resume using the project files.



Before ending each development session:



\- Update VR3D\_PLAN.md.

\- Update the Current State / Resume Here section of PROGRESS.md.

\- Append accurate progress history.

\- Record meaningful technical findings in KNOWLEDGE.md.

\- Update architectural documentation if the implementation changes.

\- Record the exact unfinished task.

\- Record the next action.



If a development phase is partially complete, leave it marked in progress.



Never mark tasks completed because they were described in a chat response.



If a new Ollama session starts, it must read the existing project documents and continue from the next unfinished task.



Do not recreate the project or reset the development plan.



\---



\# PART 14 — BEGIN IMPLEMENTATION NOW



START IMMEDIATELY.



Perform these steps in order:



1\. Inspect the current VS Code workspace and source repository.

2\. Locate and read ALL existing PLAN, PROGRESS, KNOWLEDGE, AGENTS, REQUIREMENTS, and architecture documents.

3\. Inspect the working video player, rendering implementation, synchronization service, UI, and tests.

4\. Check Git status without discarding modifications.

5\. Run the existing solution build and relevant baseline tests.

6\. Record the known-good application baseline.

7\. Create the new 3D development documents.

8\. Add the additional phases to VR3D\_PLAN.md.

9\. Reference the new plan in the existing PLAN.md.

10\. Update AGENTS.md and PROGRESS.md without resetting previous work.

11\. Research and verify the current LibVLCSharp viewpoint functionality.

12\. Start implementing the first small, verifiable task in the active 3D phase.

13\. Build and test the actual changes.

14\. Record the result.

15\. Continue until the current development phase is complete or genuinely blocked.



\*\*Do not stop after generating the plan. Begin implementing the feature.\*\*



At the end of the phase, report:



\- Current phase

\- Completed task IDs

\- Files created and modified

\- Actual build results

\- Actual test results

\- Working new functionality

\- Known issues

\- Next phase



\## FINAL OBJECTIVE



I want to open my existing Autoblow Sync Player, connect to my device, load a VR180 or VR360 video and its funscript, and click Play.



While the video and synchronized script are playing, I want to click and drag my mouse inside the video player to move my viewpoint around the 3D environment, zoom in and out, and reset the camera whenever I want.



The video must keep playing and the device must remain synchronized regardless of my camera movement.



\*\*Preserve the working application and add this as a professionally implemented, fully documented, separately planned expansion.\*\*



