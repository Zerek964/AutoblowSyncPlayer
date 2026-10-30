\# AUTOBLOW SYNC PLAYER — FUNSCRIPT HEATMAP AND VIDEO CONTROL LAYOUT EXPANSION



\## YOUR ROLE



You are continuing development of my existing Windows .NET 10 WPF application, \*\*Autoblow Sync Player\*\*.



The application already supports video playback, Autoblow API connectivity, funscript uploads, device synchronization, and ongoing development of 3D/VR video playback with interactive camera movement.



\*\*Do not rebuild the application or replace working features.\*\*



Your new assignment is to redesign the video playback area by adding a visual funscript motion heatmap and relocating the existing playback controls underneath the video.



You must implement these changes in the actual project, not simply provide instructions.



\---



\## 1. READ ALL EXISTING PROJECT FILES FIRST



Before making any changes, locate and read:



\- `AGENTS.md`

\- `PLAN.md`

\- `PROGRESS.md`

\- `KNOWLEDGE.md`

\- `PROJECT\_REQUIREMENTS.md`

\- `ARCHITECTURE.md`

\- `VR3D\_PLAN.md`

\- `VR3D\_REQUIREMENTS.md`

\- `VR3D\_ARCHITECTURE.md`

\- `VR3D\_TEST\_PLAN.md`

\- `.github/copilot-instructions.md`



Also inspect any other phase, roadmap, development plan, and progress-tracking files.



Read the \*\*Current State / Resume Here\*\* section of PROGRESS.md and inspect the actual code.



Determine:



1\. Current project phase and active task.

2\. Existing video player implementation.

3\. Where the seek slider is currently located.

4\. Where Play, Pause, and Stop controls are currently located.

5\. How the funscript file is parsed and stored.

6\. How the video duration and playback position are exposed.

7\. How the video and device synchronization works.

8\. Which 3D/VR features are implemented.

9\. Which WPF controls and view models are already available.

10\. Whether any current code or Git changes are unfinished.



Do not overwrite previous development history or completed phases.



\---



\## 2. NEW REQUIRED USER INTERFACE LAYOUT



Reorganize the existing video player so the layout follows this structure:



```text

┌──────────────────────────────────────────────┐

│                                              │

│                                              │

│              VIDEO PLAYER                    │

│              2D / 3D / VR                    │

│                                              │

│                                              │

├──────────────────────────────────────────────┤

│ FUNSCRIPT MOTION HEATMAP                      │

│ \[blue]\[yellow]\[orange]\[red]\[yellow]\[red]     │

│                     ▲ Current position       │

│                                              │

│ VIDEO SEEK SLIDER                            │

│ 00:00 ━━━━━━━━━●━━━━━━━━━━━━━━━━━━ 12:30    │

│                                              │

│         \[ PLAY ] \[ PAUSE ] \[ STOP ]          │

└──────────────────────────────────────────────┘

```



\### Required changes



Move the existing video seek slider underneath the video.



Move the existing Play, Pause, and Stop buttons underneath the video.



Add a new funscript heatmap between the video display and the seek slider.



The order must be:



1\. Video playback surface

2\. Funscript heatmap

3\. Video seek slider with current and total time

4\. Play, Pause, and Stop controls



Keep these elements inside the same video player section.



The video should continue resizing correctly when the application window changes size.



Do not leave duplicate playback controls in their previous positions.



Do not remove other existing controls such as volume, fullscreen, camera rotation, or device connection controls.



If existing commands and bindings already work, reuse them rather than creating replacement playback logic.



\---



\## 3. FUNSCRIPT HEATMAP REQUIREMENTS



Create a new visual component called `FunscriptHeatmap`.



Its purpose is to display script movement activity across the video timeline.



The heatmap must represent the actual loaded funscript, not randomly generated colors or sample activity.



\### 3.1 Color-coded visualization



Use a smooth or segmented horizontal color visualization.



Suggested colors:



\- Blue/teal: Low movement activity

\- Yellow: Moderate movement activity

\- Orange: High movement activity

\- Red: Very high movement activity

\- Dark gray: No script activity



The heatmap must visually show where the script contains more frequent or faster movement.



Do not treat funscript position values alone as activity intensity.



\### 3.2 Calculate heatmap activity



Use the existing funscript parsing service where possible.



Funscript actions contain:



```json

{

&#x20; "actions": \[

&#x20;   { "at": 0, "pos": 0 },

&#x20;   { "at": 500, "pos": 100 },

&#x20;   { "at": 1000, "pos": 0 }

&#x20; ]

}

```



The `at` property represents time in milliseconds.



The `pos` property represents position between 0 and 100.



Create an efficient, deterministic algorithm that calculates movement activity over time.



Consider:



\- Number of actions within each time interval

\- Absolute position changes between actions

\- Time elapsed between consecutive actions

\- Movement speed

\- Periods of inactivity



A reasonable default is to calculate movement distance per unit time for each timeline bucket, distributing movement across the time interval between actions.



Normalize the activity to an appropriate visual scale, with reasonable handling for outliers.



Document the calculation in KNOWLEDGE.md.



Do not interpret colors as physical device temperature or an actual hardware intensity measurement.



\### 3.3 Timeline mapping



The heatmap must use the same timeline as the video player.



Requirements:



\- Heatmap starts at video timestamp 0.

\- Heatmap ends at the video duration.

\- Each horizontal position represents a corresponding video timestamp.

\- Script activity must appear at the correct video time.

\- Handle scripts shorter or longer than the video.

\- Respect the application's actual synchronization offset semantics if an offset affects where script events align with video time.

\- Update when a different video or script is loaded.



If the script extends beyond the video duration, clip its visible activity to the displayed timeline and optionally show a warning.



If the loaded video has no funscript, display a neutral empty heatmap with a message such as "Load a funscript to display activity."



Do not generate fake activity data.



\---



\## 4. LIVE PLAYBACK POSITION INDICATOR



Add a visible playback-position marker over the heatmap.



The marker should move from left to right as the video plays.



Requirements:



\- Use the existing authoritative video playback clock.

\- Update the marker during playback.

\- Freeze at the current position when paused.

\- Follow the video when seeking.

\- Reset according to existing Stop behavior.

\- Update after loading another video.

\- Remain aligned with the seek slider.



The marker can be a thin white vertical line or another clearly visible indicator.



Do not create another playback timer that attempts to independently track the media timeline.



The heatmap playhead and video seek slider must represent the same playback position.



\---



\## 5. INTERACTIVE HEATMAP



Implement useful mouse interactions.



\### Mouse Hover



When hovering over the heatmap, show a tooltip containing:



\- Corresponding video timestamp

\- Relative script movement activity

\- Activity category (Low, Medium, High, or Very High)



Use the actual calculated values.



\### Click to Seek



Allow clicking the heatmap to seek to the corresponding video position.



Use the same existing seek command and synchronization logic as the normal video slider.



If the video is playing, follow the application's existing seek and device resynchronization behavior.



If the video is paused, remain paused and preserve the correct position for resuming.



Do not directly send separate Autoblow API commands from the heatmap component.



Do not trigger a script reupload.



\### Performance



The funscript may contain up to 100,000 actions.



The heatmap must remain responsive with large scripts.



Precompute activity data when the script loads or when necessary timeline parameters change.



Avoid creating one WPF visual element per funscript action.



Consider a custom lightweight WPF drawing control, cached bitmap, or another efficient rendering approach.



Re-render when the control width or required data changes, but avoid rebuilding the entire heatmap on every video frame.



The moving playback indicator should be updated independently from the heatmap's static activity visualization.



\---



\## 6. VIDEO SEEK SLIDER RELOCATION



Locate the existing working video seek slider.



Move it directly underneath the funscript heatmap.



Requirements:



\- Show current playback time.

\- Show total video duration.

\- Allow click-to-seek.

\- Allow dragging the slider.

\- Continue supporting all existing video formats.

\- Keep the slider aligned with the heatmap width.

\- Do not create duplicate seek logic.

\- Preserve existing synchronization behavior.



\### Important Seek Behavior



While dragging the slider, avoid sending excessive seek or device commands.



Preserve the current implementation's safe seek behavior, including resynchronizing the funscript at the correct committed playback timestamp.



Do not allow moving the slider to unexpectedly reload or restart the funscript.



The heatmap playback-position indicator must follow the selected position.



\---



\## 7. PLAY, PAUSE, AND STOP BUTTON RELOCATION



Locate the existing Play, Pause, and Stop buttons.



Move them directly underneath the video seek slider.



Display the buttons in a centered horizontal layout.



Preferred design:



`\[ ▶ Play ]    \[ ⏸ Pause ]    \[ ■ Stop ]`



Requirements:



\- Keep existing working button commands and view-model bindings.

\- Preserve current enabled/disabled states.

\- Preserve existing device synchronization behavior.

\- Play starts or resumes playback using existing logic.

\- Pause pauses video and script using existing logic.

\- Stop performs the existing safe stop operation.

\- No duplicate buttons remain in the old location.

\- Keyboard shortcuts continue to work.

\- Controls remain accessible in fullscreen where currently supported.



Use the existing application's theme and styling conventions.



Do not rewrite the API client or synchronization service simply to move buttons.



\---



\## 8. PRESERVE 3D/VR CAMERA FUNCTIONALITY



The ongoing 3D/VR development must remain functional.



The new heatmap and playback controls must work with:



\- Standard 2D videos

\- Supported VR180 videos

\- Supported VR360 videos

\- Supported stereoscopic SBS videos

\- Supported Over-Under videos

\- Mouse-controlled camera movement

\- Mouse-wheel zoom

\- Fullscreen playback

\- Video seeking

\- Funscript synchronization



Do not allow the new heatmap or controls to cover the camera viewport.



For WPF/LibVLCSharp, account for any native video surface or airspace limitations.



Place the heatmap and buttons in their own layout rows outside the actual video rendering surface rather than relying on fragile transparent overlays.



Camera dragging inside the video must not accidentally move the video slider.



Interacting with the heatmap must not rotate the camera.



\---



\## 9. PRESERVE AUTOBLOW SYNCHRONIZATION



The application already has working device synchronization.



Do not break it.



Expected behavior:



| Action | Video | Funscript |

|---|---|---|

| Move camera | Continues | Continues |

| Zoom camera | Continues | Continues |

| Hover heatmap | Continues | Continues |

| Click heatmap | Seeks | Resynchronizes using existing behavior |

| Drag slider | Seeks according to current seek rules | Resynchronizes when appropriate |

| Press Play | Plays | Plays |

| Press Pause | Pauses | Pauses/stops |

| Press Stop | Stops | Stops |



Keep one authoritative media playback clock.



The heatmap is a visualization of script data and must not independently control the device.



Avoid unnecessary new network requests, script uploads, or device reconnections.



\---



\## 10. CREATE A NEW FEATURE DEVELOPMENT PLAN



Before making substantial code changes, create:



`TIMELINE\_HEATMAP\_PLAN.md`



Add a reference to this plan in the existing PLAN.md and, where relevant, VR3D\_PLAN.md.



Do not reset active phases or modify completed task status unnecessarily.



\### PHASE H1 — Discovery and UI Planning



\- Inspect current layout and playback controls.

\- Inspect current funscript models and parser.

\- Inspect video clock and synchronization logic.

\- Run baseline build and tests.

\- Create the new plan.

\- Document layout changes.

\- Record the current working baseline.



Acceptance: existing app builds, current control bindings are identified, and the new layout plan exists.



\### PHASE H2 — Heatmap Data and Rendering



\- Implement activity calculation.

\- Map script activity to video duration.

\- Implement color scale.

\- Render heatmap under the video.

\- Handle large scripts efficiently.

\- Handle missing/invalid script data.

\- Add calculation tests.



Acceptance: heatmap renders actual data at correct times, and tests pass.



\### PHASE H3 — Playback Marker and Interaction



\- Add live playback marker.

\- Bind marker to video position.

\- Add hover tooltips.

\- Add click-to-seek.

\- Handle pause, stop, and seek.

\- Add relevant interaction tests.



Acceptance: marker follows the real video clock, and heatmap seeking uses existing playback commands.



\### PHASE H4 — Relocate Video Controls



\- Move seek slider under heatmap.

\- Move Play button.

\- Move Pause button.

\- Move Stop button.

\- Align controls beneath the video.

\- Remove duplicate old controls.

\- Preserve existing styles and commands.

\- Test resizing and fullscreen compatibility.



Acceptance: controls work from their new location and existing bindings remain functional.



\### PHASE H5 — Regression Testing and Release



\- Run solution build.

\- Run unit tests.

\- Test standard MP4.

\- Test supported 3D/VR formats.

\- Test funscript loading.

\- Test heatmap data and alignment.

\- Test normal seeking.

\- Test heatmap seeking.

\- Test synchronized Play/Pause/Stop.

\- Test fullscreen.

\- Test device mock behavior.

\- Verify no duplicate device commands.

\- Update user documentation.

\- Publish an updated Windows executable when appropriate.



Acceptance: no verified regression in existing functionality, all applicable automated tests pass, and manual verification results are documented.



\---



\## 11. PROJECT MEMORY REQUIREMENTS



After each completed task, update:



\*\*TIMELINE\_HEATMAP\_PLAN.md\*\*

\- Task completion

\- Acceptance status

\- Build/test evidence

\- Next unfinished task



\*\*PROGRESS.md\*\*

\- Current State / Resume Here

\- Current feature phase

\- Files modified

\- Commands executed

\- Actual test results

\- Next action



\*\*KNOWLEDGE.md\*\*

\- Activity calculation algorithm

\- Rendering design

\- Timeline mapping decisions

\- WPF limitations

\- Important discoveries

\- Failed approaches



\*\*AGENTS.md\*\*

\- Add a reference requiring future development sessions to read the new heatmap plan.



\*\*VR3D\_PLAN.md\*\*

\- Keep its existing phases and completion history intact.

\- Add a cross-reference only if the new changes impact 3D development.



Do not erase any existing documentation or previous development history.



\---



\## 12. IMPLEMENTATION AND TESTING RULES



Follow these rules:



1\. Do not recreate the application.

2\. Do not replace the existing video playback engine.

3\. Do not rewrite functioning synchronization code unnecessarily.

4\. Do not create duplicate playback buttons.

5\. Do not create a second playback timer.

6\. Do not use fake or hardcoded heatmap data.

7\. Do not allow heatmap interactions to generate excessive device commands.

8\. Do not interfere with mouse-based VR camera controls.

9\. Do not overwrite existing completed plans.

10\. Do not discard uncommitted Git changes.

11\. Make small, verifiable source changes.

12\. Build after meaningful modifications.

13\. Run applicable tests.

14\. Fix compiler errors.

15\. Update the project-memory files.

16\. Never fabricate successful testing.

17\. Do not connect to or actuate real hardware during automated tests.

18\. Stop for my review at a completed feature phase boundary.



If a task is blocked, document the reason and proceed with another safe, independent task.



\---



\## 13. START IMPLEMENTATION NOW



Do the following immediately:



1\. Read all existing project plans and progress documents.

2\. Inspect the video player XAML and view models.

3\. Locate the existing seek slider and playback buttons.

4\. Inspect existing funscript parsing and data models.

5\. Inspect the video clock and synchronization services.

6\. Run the baseline build and relevant tests.

7\. Create TIMELINE\_HEATMAP\_PLAN.md.

8\. Update PLAN.md and PROGRESS.md.

9\. Implement the next incomplete heatmap task.

10\. Build and test actual changes.

11\. Continue through the current feature phase.

12\. Update progress and knowledge documents.



\*\*Do not stop after creating the plan. Begin coding immediately.\*\*



At the end of the phase report:



\- Current phase

\- Completed task IDs

\- Files modified

\- Features implemented

\- Actual build results

\- Actual test results

\- Known problems

\- Next development task



\## FINAL OBJECTIVE



When I open Autoblow Sync Player, the video should appear at the top of the video player section.



Directly below it, I should see a color-coded heatmap showing the movement activity of the loaded funscript across the video's timeline.



Below the heatmap should be the video seek slider with current and total playback time.



Below that should be the Play, Pause, and Stop buttons.



The heatmap should show a moving playback-position marker and allow me to click a section to seek to that position.



The video, funscript, device synchronization, and 3D camera functionality must continue working exactly as before.



\*\*Preserve the existing application, implement the new layout and heatmap, and track the work through separate development phases.\*\*



