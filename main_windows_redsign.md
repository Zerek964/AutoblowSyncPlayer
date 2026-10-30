\# AUTOBLOW SYNC PLAYER — COMPLETE MAIN WINDOW UI REDESIGN



\## YOUR ROLE



You are a senior C#/.NET 10 developer, Windows WPF UI architect, MVVM specialist, and multimedia application engineer.



You are continuing development of my existing \*\*Autoblow Sync Player\*\* application.



The application is already functional and supports:



\- Local video playback

\- Funscript loading and validation

\- Funscript motion heatmap

\- Video seeking

\- Play, Pause, and Stop

\- Autoblow API connectivity

\- Funscript uploading

\- Device synchronization

\- 3D/VR video playback development

\- Camera controls

\- Fullscreen playback

\- Application settings and status reporting



Your new assignment is to \*\*redesign the main application window to maximize video viewing space and move all settings from the right sidebar into dropdown menus in the top toolbar.\*\*



This is a UI layout redesign, not a new application.



Do not rewrite or replace working services.



You must inspect the actual existing code, create a new implementation plan, update project documentation, implement the changes, and test the resulting interface.



\---



\# PART 1 — READ ALL EXISTING PROJECT DOCUMENTATION



Before changing any source code, locate and read:



\- AGENTS.md

\- PLAN.md

\- PROGRESS.md

\- KNOWLEDGE.md

\- PROJECT\_REQUIREMENTS.md

\- ARCHITECTURE.md

\- VR3D\_PLAN.md

\- VR3D\_REQUIREMENTS.md

\- VR3D\_ARCHITECTURE.md

\- VR3D\_TEST\_PLAN.md

\- TIMELINE\_HEATMAP\_PLAN.md

\- README.md

\- .github/copilot-instructions.md



Also locate and review any additional files containing PLAN, PHASE, PROGRESS, ROADMAP, REQUIREMENTS, or DEVELOPMENT in their filenames.



Read the Current State / Resume Here section in PROGRESS.md first.



Inspect the actual source code and Git status.



Determine:



1\. Current solution structure.

2\. Existing main window XAML layout.

3\. Existing right sidebar controls.

4\. Current menu and toolbar implementation.

5\. Existing view models and commands.

6\. Existing video viewport implementation.

7\. Existing heatmap component.

8\. Current playback slider and buttons.

9\. Existing device connection state properties.

10\. Existing synchronization state properties.

11\. Existing video resizing and fullscreen behavior.

12\. Current 3D/VR control bindings.

13\. Latest verified build and test results.



Do not assume that the documentation always reflects the implementation.



Do not overwrite previous development progress.



Do not reset existing plans.



\---



\# PART 2 — NEW MAIN WINDOW LAYOUT



The existing application currently has a video player on the left and a large right sidebar containing the Video, Funscript, Device Connection, 3D/VR Controls, and Synchronization sections.



\*\*Remove the entire right sidebar and relocate all of its functionality into top-toolbar dropdown menus.\*\*



The final layout must be:



```text

┌────────────────────────────────────────────────────────────────┐

│ File  View  Video● Funscript● Device Connection● 3D/VR Sync Help│

├────────────────────────────────────────────────────────────────┤

│                                                                │

│                                                                │

│                                                                │

│                                                                │

│                     VIDEO PLAYBACK                             │

│                                                                │

│                   2D / 3D / VR VIDEO                           │

│                                                                │

│                                                                │

│                                                                │

│                                                                │

│                                                                │

├────────────────────────────────────────────────────────────────┤

│ FUNSCRIPT HEATMAP                                              │

│ ░░▓▓██████▓▓░░▓▓████░░████████▓▓░░▓▓████░░                   │

│                                                                │

│ VIDEO SEEK SLIDER                                              │

│ 00:00:00 ━━━━━━━━━━━━━●━━━━━━━━━━━━━━━━━━━━━━ 00:29:01       │

│                                                                │

│                  \[ Play ] \[ Pause ] \[ Stop ]                    │

├────────────────────────────────────────────────────────────────┤

│ Device: Connected        Playback: Paused        Sync: +0 ms    │

└────────────────────────────────────────────────────────────────┘

```



The diagram is conceptual. Match the application's existing dark theme and use responsive spacing.



\### Main requirements



1\. Remove the fixed right sidebar.

2\. Move all sidebar settings to top dropdown menus.

3\. Make the video viewport occupy the available main content area.

4\. Dock the heatmap at the bottom.

5\. Dock the video seek slider below the heatmap.

6\. Dock Play, Pause, and Stop beneath the seek slider.

7\. Preserve the existing bottom application status bar.

8\. Make the video resize automatically with the main window.

9\. Maintain the correct video aspect ratio.

10\. Preserve all existing device and playback functionality.



Do not place the new bottom controls inside a fixed-height video surface.



Do not require vertical scrolling to access the primary video controls.



\---



\# PART 3 — TOP TOOLBAR REDESIGN



The current application has File, View, and Help menus.



Keep those existing menus.



Insert the following new menus immediately after View:



```text

File

View

Video

Funscript

Device Connection

3D/VR Controls

Synchronization

Help

```



Use normal horizontal menu alignment.



If the application window becomes narrow, provide appropriate responsive behavior, such as a compact menu or overflow mechanism, without losing access to any settings.



\### Critical requirement



\*\*Clicking a menu must display the settings and controls that previously existed in the corresponding right sidebar section.\*\*



Do not simply create empty menu headings.



Every existing control must remain accessible.



Use anchored dropdowns or flyout panels that can contain buttons, text fields, checkboxes, dropdowns, sliders, and status information.



Standard WPF MenuItem behavior may not be appropriate for every interactive setting. If necessary, use a custom anchored Popup or flyout with correctly managed focus, mouse capture, keyboard input, and outside-click dismissal.



A user must be able to type into the device token field, adjust sliders, and use dropdowns without the menu closing unexpectedly.



Large settings panels must remain fully accessible on smaller screens.



\---



\# PART 4 — VIDEO MENU



Add a top-level menu named:



\*\*Video\*\*



When clicked, display all controls previously located in the Video section.



Required items:



\- Load Video

\- Restart Video

\- Current filename

\- Current video path or file details where appropriate

\- Video duration

\- Video resolution when available

\- Existing video information and settings

\- Any existing video-specific options



Reuse existing commands.



Do not implement a duplicate video-loading system.



The current loaded video must continue playing when the Video menu opens or closes.



Loading a new video must update the existing video player and corresponding status indicator.



Keep the main playback controls at the bottom of the window rather than inside this menu.



\---



\# PART 5 — FUNSCRIPT MENU



Add a top-level menu named:



\*\*Funscript\*\*



When clicked, display the existing funscript settings.



Include:



\- Load Funscript

\- Current funscript filename

\- Script duration

\- Total action count

\- Validation status

\- Script information and metadata

\- Any existing script-specific controls

\- Current loaded-script status



Preserve current funscript validation and parsing behavior.



Do not create another funscript parser.



Do not reload or upload the script merely because the menu opens.



If the application currently provides script-related options in other locations, relocate them appropriately without removing functionality.



The heatmap must remain visible at the bottom of the main window, not inside this dropdown.



\---



\# PART 6 — DEVICE CONNECTION MENU



Add a top-level menu named:



\*\*Device Connection\*\*



Relocate all existing device controls into this menu.



Include:



\- Device token entry

\- Show/Hide Token

\- Save Token

\- Connect

\- Disconnect

\- Refresh Connection Status

\- Current device state

\- Device model

\- Device type

\- Current API cluster

\- Upload Script to Device

\- Script uploaded/loaded status

\- Existing device error messages

\- Other existing device connection settings



Keep existing security behavior for credential storage.



Never display the token in logs or project-memory files.



Do not hardcode a token in XAML or source code.



Do not initiate a connection, upload, or device movement simply because the menu opens.



Only execute an existing device action when the user explicitly requests it or when already required by the existing application workflow.



Use the existing device connection service, commands, and status properties.



If the device disconnects, update the menu's status indicator immediately.



\---



\# PART 7 — 3D/VR CONTROLS MENU



Add a top-level menu named:



\*\*3D/VR Controls\*\*



Move all existing 3D/VR controls into this dropdown.



Preserve all currently implemented controls, including where available:



\- Video Mode

\- Detect Mode from Video

\- Stereo Eye selection

\- Auto/Left/Right eye options

\- Camera Mode Enabled

\- Camera movement sensitivity

\- Field of View

\- Invert Y-axis

\- Reset Camera

\- Camera orientation

\- Zoom

\- Existing VR180/VR360 settings

\- Other existing 3D/VR options



The dropdown may contain grouped sections or a scrollable flyout.



Do not remove any existing settings.



\### Critical



The 3D/VR camera must continue working after the sidebar is removed.



Mouse drag inside the video must still rotate the camera.



The mouse wheel must retain the current zoom behavior where supported.



Moving the camera must not change the video timeline or restart the device script.



\---



\# PART 8 — SYNCHRONIZATION MENU



Add a top-level menu named:



\*\*Synchronization\*\*



Move all existing synchronization settings into this menu.



Inspect the actual implementation and preserve all current controls.



Where already supported, include:



\- Current synchronization status

\- Synchronization offset

\- Offset adjustment controls

\- Reset Offset

\- Current video timestamp

\- Script playback timestamp

\- Synchronization difference

\- Resynchronization controls

\- Existing sync monitoring information

\- Any current timing-related configuration



Do not invent additional device commands simply to populate this menu.



Do not change existing offset semantics.



Changing an offset must continue using the existing safe synchronization logic.



Opening the Synchronization menu must not restart or interrupt device playback.



\---



\# PART 9 — GREEN/RED STATUS INDICATORS



Add status indicators next to the following three top menu items:



1\. Video

2\. Funscript

3\. Device Connection



These must be small, clearly visible, circular colored indicators or indicator buttons immediately to the right of each menu label.



\### Video indicator



GREEN when a video file is successfully loaded and ready in the media player.



RED when no video is loaded or loading failed.



\### Funscript indicator



GREEN when a funscript has successfully loaded and passed validation.



RED when no funscript is loaded or validation failed.



\### Device Connection indicator



GREEN when the Autoblow device is confirmed connected.



RED when disconnected, offline, or the connection failed.



During a transient operation, an optional amber state is acceptable, but successful and unsuccessful settled states must use the required green/red colors.



\### Example



```text

Video             ● GREEN

Funscript         ● GREEN

Device Connection ● GREEN

```



If no content or device is available:



```text

Video             ● RED

Funscript         ● RED

Device Connection ● RED

```



\### Behavior



Indicators must:



\- Update automatically when application state changes.

\- Use existing authoritative view-model properties.

\- Implement INotifyPropertyChanged or equivalent notification correctly.

\- Not rely on hardcoded colors.

\- Not require the user to refresh the UI.

\- Reset correctly when a file is cleared or a device disconnects.

\- Have clear tooltips and accessible labels.



The indicators must not be confused with whether the script has been uploaded to the device.



A valid locally loaded funscript can show green even if it has not yet been uploaded.



The device indicator must reflect actual confirmed connectivity, not merely whether a token was entered.



If indicator buttons are clickable, they should open the corresponding settings flyout. They must not trigger connection, playback, or hardware movement.



For accessibility, include non-color status text in tooltips or accessible descriptions.



\---



\# PART 10 — REMOVE THE RIGHT SIDEBAR



After all controls have been relocated and verified:



Remove the right sidebar from the main layout.



This includes:



\- The sidebar Grid column

\- Sidebar ScrollViewer

\- Video settings container

\- Funscript settings container

\- Device Connection container

\- 3D/VR Controls container

\- Synchronization container



Remove obsolete layout-only elements and their unused resources.



Do not delete commands, services, data models, or view-model properties that are still used by the new menus.



Do not leave hidden duplicate controls that consume space.



The right sidebar must no longer reserve width in the main application window.



The central video playback area must expand into that space.



\---



\# PART 11 — VIDEO FILLS THE AVAILABLE APPLICATION WINDOW



This is one of the most important requirements.



Currently the video occupies only the upper-left portion of the main application.



After removing the right sidebar, the video should occupy the maximum available area inside the application window.



\### Required behavior



When the application launches:



\- Top toolbar remains visible.

\- Bottom playback controls remain visible.

\- Bottom status bar remains visible.

\- Video occupies all remaining available space.



When I maximize the application:



\- Video automatically expands.

\- Video remains centered.

\- Video aspect ratio remains correct.

\- Playback controls stay docked at the bottom.



When I resize the application smaller:



\- Video automatically shrinks.

\- Aspect ratio stays correct.

\- Controls remain accessible.

\- No unnecessary scrollbars appear.



\### Aspect ratio requirements



Do not stretch the video disproportionately.



Support typical resolutions and display ratios, including:



\- 1920x1080

\- 1280x720

\- 3840x2160

\- 4:3

\- 16:9

\- Other valid source ratios

\- VR180 and VR360 projection modes



Use the appropriate effective display aspect ratio, accounting for relevant media metadata and the current rendering mode.



Letterboxing or pillarboxing is acceptable when necessary to preserve the correct ratio.



Do not force every source to fill the entire area through cropping or stretching.



Center the video within its available region.



\### WPF implementation



Refactor the main window layout using an appropriately structured Grid.



Preferred conceptual layout:



```text

MainWindow

│

├── Row 0: Top Menu / Toolbar       Auto

│

├── Row 1: Video Viewport           \*

│

├── Row 2: Funscript Heatmap        Auto

│

├── Row 3: Video Seek Slider        Auto

│

├── Row 4: Playback Buttons         Auto

│

└── Row 5: Application Status Bar   Auto

```



The `\*` viewport row should consume remaining space.



Do not wrap the entire player and its bottom controls inside a vertical ScrollViewer.



Inspect the actual LibVLCSharp VideoView or custom 3D renderer before implementing resizing.



If the video renderer uses a native window or special surface, accommodate those rendering constraints properly.



Ensure the video surface is measured and arranged correctly rather than simply setting Stretch or Viewbox properties that may not control the native video.



Avoid unnecessarily recreating the player, media, or rendering surface during ordinary resizing.



\---



\# PART 12 — MOVE HEATMAP AND PLAYBACK CONTROLS TO BOTTOM



Relocate these existing controls to the bottom of the main window.



Required order:



1\. Funscript heatmap

2\. Video seek slider

3\. Play button

4\. Pause button

5\. Stop button



Keep the existing status bar below these controls.



\### Funscript heatmap



The heatmap should:



\- Stretch horizontally across the available player width.

\- Remain visible while resizing.

\- Continue displaying actual funscript motion activity.

\- Continue showing the current playback position.

\- Retain existing hover and click behavior.

\- Stay synchronized with the seek slider.

\- Update when another script is loaded.

\- Avoid excessive repainting during resizing.



Do not recreate the heatmap analysis algorithm unless the existing code is defective.



\### Video seek slider



Move the existing working slider directly below the heatmap.



Requirements:



\- Slider stretches horizontally.

\- Shows current playback position.

\- Shows total duration.

\- Supports normal seeking.

\- Preserves existing seek commit behavior.

\- Maintains device synchronization.

\- Uses the existing playback clock.

\- Does not create duplicate API commands.



\### Play, Pause, Stop



Move the existing controls into a centered horizontal row underneath the slider.



Buttons:



`\[ ▶ Play ] \[ ⏸ Pause ] \[ ■ Stop ]`



Preserve:



\- Existing commands

\- Enabled/disabled states

\- Existing keyboard shortcuts

\- Playback behavior

\- Device synchronization

\- Safe stop behavior



Do not create replacement playback services.



\---



\# PART 13 — WINDOW RESIZING AND RESPONSIVE DESIGN



The entire application must remain responsive to window-size changes.



\### Large window



\- Maximum video viewing area.

\- Menus across the top.

\- Heatmap and playback controls at the bottom.

\- No right sidebar.



\### Medium window



\- Video resizes automatically.

\- Bottom controls remain visible.

\- Top menus remain usable.

\- Status indicators retain their functionality.



\### Small window



\- Respect a sensible minimum window size.

\- Keep playback controls accessible.

\- Prevent settings flyouts from extending off-screen.

\- Allow long settings panels to scroll independently.

\- Use menu overflow or compact presentation where needed.



Do not implement a fixed video width or height that prevents the application from expanding.



Do not make the heatmap or playback controls float in the middle of the content area.



Do not make the bottom controls disappear when the main window is resized.



\---



\# PART 14 — PRESERVE CURRENT FUNCTIONALITY



This redesign must not break:



\- Video loading

\- Standard MP4 playback

\- Supported 3D/VR playback

\- Funscript loading

\- Funscript validation

\- Funscript heatmap rendering

\- Heatmap seeking

\- Video slider seeking

\- Play

\- Pause

\- Stop

\- Restart

\- Device token management

\- Connect

\- Disconnect

\- Upload Script to Device

\- Device connection status

\- Synchronization offset

\- Device playback synchronization

\- Camera movement

\- Camera zoom

\- Fullscreen playback

\- Application status information



Do not change existing Autoblow API endpoints or synchronization timing logic.



Do not send any device commands simply because a menu was opened or closed.



Maintain the existing authoritative video clock.



All normal automated tests must use mocked device clients.



Real device actions and hardware tests require explicit authorization.



\---



\# PART 15 — NEW UI REDESIGN DEVELOPMENT PLAN



Create an additional plan named:



`MAIN\_UI\_REDESIGN\_PLAN.md`



Reference it from PLAN.md.



Do not reset or overwrite existing VR3D or heatmap plans.



Use unique task IDs, such as UI-P1-01, UI-P1-02, UI-P2-01.



Each task must include acceptance criteria, dependencies, actual test evidence, and completion status.



\## PHASE UI-1 — Discovery and Baseline



\- Read all project documentation.

\- Inspect MainWindow XAML.

\- Inspect view models and command bindings.

\- Inspect existing menu structure.

\- Inventory every sidebar control.

\- Locate heatmap and seek controls.

\- Inspect video renderer resizing behavior.

\- Review existing Git changes.

\- Run baseline build and tests.

\- Create MAIN\_UI\_REDESIGN\_PLAN.md.

\- Document existing controls-to-menu mapping.



Acceptance: every existing sidebar control is accounted for, baseline build/test results are recorded, and the new plan exists.



\## PHASE UI-2 — Top Toolbar and Settings Menus



\- Implement Video menu.

\- Implement Funscript menu.

\- Implement Device Connection menu.

\- Implement 3D/VR Controls menu.

\- Implement Synchronization menu.

\- Move existing settings into appropriate flyouts.

\- Preserve existing commands and bindings.

\- Verify interactive flyout behavior.

\- Confirm all settings remain accessible.



Acceptance: every previous sidebar control can be used from its new top menu without unnecessary behavior changes.



\## PHASE UI-3 — Status Indicators



\- Implement Video green/red indicator.

\- Implement Funscript green/red indicator.

\- Implement Device Connection green/red indicator.

\- Bind status to actual application state.

\- Add tooltip/accessibility descriptions.

\- Verify state changes update automatically.

\- Test load/unload, connect/disconnect, and failure scenarios.



Acceptance: all three indicators correctly reflect actual current state.



\## PHASE UI-4 — Remove Sidebar and Expand Video



\- Remove old sidebar layout.

\- Remove unused sidebar containers.

\- Expand main video viewport.

\- Implement proper responsive Grid sizing.

\- Preserve aspect ratio.

\- Handle native video-surface resize requirements.

\- Test resizing in 2D mode.

\- Test resizing in supported 3D/VR modes.

\- Test maximize/restore and fullscreen transitions.



Acceptance: video fills available space without distortion, and the right sidebar no longer exists.



\## PHASE UI-5 — Bottom Heatmap and Controls



\- Move existing heatmap to the bottom.

\- Move video seek slider under the heatmap.

\- Move Play, Pause, Stop beneath the slider.

\- Align elements properly.

\- Preserve existing data bindings.

\- Preserve playback position synchronization.

\- Test heatmap interaction.

\- Test video seeking.

\- Test window resizing.

\- Test playback and stop behavior.



Acceptance: bottom layout matches requirements and all controls remain functional.



\## PHASE UI-6 — Final Testing, Cleanup, and Release



\- Run complete solution build.

\- Run automated tests.

\- Check for WPF binding errors.

\- Test dropdown menus.

\- Test device status indicators.

\- Test file loading.

\- Test device connection states.

\- Test video resize behavior.

\- Test 2D playback.

\- Test supported 3D/VR playback.

\- Test camera input.

\- Test heatmap.

\- Test seeking.

\- Test synchronized Play/Pause/Stop.

\- Test fullscreen.

\- Check for duplicate controls.

\- Verify layout visually in an actual running application.

\- Update README.md.

\- Update progress and knowledge documentation.

\- Publish updated Windows x64 executable where appropriate.



Acceptance: clean build, passing applicable tests, verified layout, and no known regression in previously working features.



Do not claim visual acceptance based only on successful XAML compilation.



\---



\# PART 16 — TESTING REQUIREMENTS



Add or update tests for the redesigned user interface.



\### Top menu tests



\- Video menu opens.

\- Funscript menu opens.

\- Device Connection menu opens.

\- 3D/VR Controls menu opens.

\- Synchronization menu opens.

\- Existing controls still execute the correct commands.

\- Dropdown remains usable when interacting with input fields.

\- Dropdown closes appropriately.

\- Keyboard navigation works.



\### Status indicator tests



\- Video absent → Red

\- Video loaded → Green

\- Video loading failed → Red

\- Funscript absent → Red

\- Valid funscript loaded → Green

\- Invalid funscript → Red

\- Device disconnected → Red

\- Device connected → Green

\- Device connection failed → Red

\- Device disconnects after connection → Red



\### Video resize tests



\- Initial application window

\- Window maximized

\- Window restored

\- Window resized larger

\- Window resized smaller

\- Different aspect ratios

\- Fullscreen enter/exit

\- Camera movement after resize

\- Video playback continues through resizing



\### Bottom control tests



\- Heatmap visible

\- Heatmap playhead moves

\- Seek slider follows video

\- Click heatmap to seek

\- Play

\- Pause

\- Stop

\- Restart from Video menu

\- Device script stays synchronized

\- No duplicate or unintended device commands



Use existing test infrastructure and mock device clients.



Record which tests are automated and which require visual/manual testing.



\---



\# PART 17 — UPDATE PROJECT MEMORY



After every completed task, update:



\*\*MAIN\_UI\_REDESIGN\_PLAN.md\*\*

\- Current phase

\- Task completion

\- Acceptance criteria

\- Verification evidence

\- Next task



\*\*PROGRESS.md\*\*

\- Current State / Resume Here

\- Actual code changes

\- Build and test results

\- Problems encountered

\- Fixes attempted

\- Remaining issues

\- Exact next action



\*\*KNOWLEDGE.md\*\*

\- UI architecture decisions

\- WPF menu/flyout behavior

\- Resizing implementation

\- LibVLCSharp rendering limitations

\- Successful and unsuccessful approaches



\*\*AGENTS.md\*\*

\- Add instructions requiring future development sessions to read MAIN\_UI\_REDESIGN\_PLAN.md.



\*\*ARCHITECTURE.md\*\*

\- Update the UI architecture to reflect the removal of the sidebar and the new layout.



Preserve all previous development history.



Do not mark work complete without verification.



\---



\# PART 18 — DEVELOPMENT RULES



Follow these rules throughout implementation.



1\. Do not recreate the existing application.

2\. Do not replace the existing playback engine.

3\. Do not rewrite working Autoblow API services.

4\. Do not create duplicate settings panels.

5\. Do not leave duplicate Play/Pause/Stop controls.

6\. Do not leave an unused sidebar column.

7\. Do not use fixed-size video dimensions.

8\. Do not distort video aspect ratios.

9\. Do not create a second video playback clock.

10\. Do not interrupt synchronization while opening menus.

11\. Do not trigger device commands from status indicator updates.

12\. Do not discard uncommitted changes.

13\. Do not overwrite previous project plans.

14\. Use the existing application's theme.

15\. Reuse existing view models and commands.

16\. Implement small, verifiable changes.

17\. Run builds and relevant tests.

18\. Fix errors before continuing.

19\. Maintain accurate project-memory files.

20\. Do not invent successful builds or visual tests.



Stop for my review at each completed UI redesign phase boundary.



Do not repeatedly ask for permission for ordinary local code changes, builds, or tests.



\---



\# PART 19 — START IMPLEMENTATION NOW



Begin immediately.



Perform these actions in order:



1\. Read every existing project plan and progress file.

2\. Inspect MainWindow XAML and relevant source files.

3\. Inventory all settings and controls currently in the sidebar.

4\. Inspect current menu and toolbar implementation.

5\. Inspect existing video resizing and rendering behavior.

6\. Run the current baseline build and tests.

7\. Create MAIN\_UI\_REDESIGN\_PLAN.md.

8\. Update PLAN.md, AGENTS.md, and PROGRESS.md.

9\. Begin implementing the new top menus.

10\. Implement their settings flyouts and preserve bindings.

11\. Implement the green/red status indicators.

12\. Remove the right sidebar after its settings are relocated.

13\. Expand the video viewport into the available space.

14\. Move the heatmap, seek slider, and playback buttons to the bottom.

15\. Run appropriate builds and tests as each task is completed.

16\. Fix encountered issues.

17\. Update project memory with verified results.



Work through the active phase autonomously.



At each phase boundary, report:



\- Current phase

\- Completed task IDs

\- Files modified

\- UI changes implemented

\- Actual build results

\- Actual test results

\- Remaining issues

\- Next development phase



Do not just explain what you plan to do. Make the actual source-code changes using the available tools.



\## FINAL OBJECTIVE



I want to open Autoblow Sync Player and see a clean, modern application with:



\- A top toolbar containing File, View, Video, Funscript, Device Connection, 3D/VR Controls, Synchronization, and Help.

\- Green/red indicators showing whether the video and funscript are loaded and whether the device is connected.

\- Dropdown menus containing all the settings previously in the right sidebar.

\- No right sidebar.

\- A large video player occupying all remaining available window space.

\- Automatic video resizing when the window is resized or maximized.

\- Correct video aspect ratio without stretching.

\- A funscript heatmap docked immediately below the video.

\- A seek slider directly below the heatmap.

\- Play, Pause, and Stop buttons directly below the slider.

\- The existing application status bar at the bottom.

\- All existing video, 3D/VR, device, and synchronization functionality preserved.



\*\*Implement this as a complete, carefully tested UI redesign with separate development phases, preserving all existing working functionality.\*\*



