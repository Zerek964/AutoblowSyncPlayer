\# Autoblow Sync Player — Update Playback Buttons and Bottom Control Layout



\## Project instructions



You are continuing development of the existing Autoblow Sync Player .NET desktop application.



Before making changes:



1\. Read the existing `Plan.md`, `Progress.md`, and any project instruction or architecture files.

2\. Inspect the current application structure and identify the UI framework being used (WPF, WinForms, or another framework).

3\. Locate the current Play, Pause, and Stop buttons, video seek bar, funscript heat map, and playback status bar.

4\. Inspect the existing playback event handlers and bindings before making changes.

5\. Preserve all existing application functionality.



\*\*Do not rebuild the application or create a new playback interface from scratch. Modify the existing implementation.\*\*



\## Phase 1 — Replace Play, Pause, and Stop buttons



Replace the existing rectangular text-based Play, Pause, and Stop buttons with the new dark-gray image buttons.



The new graphics are located in the `Individual\_Buttons` folder.



Each playback button has three PNG files representing its visual states:



| Button | Normal | Hover | Pressed |

|---|---|---|---|

| Play | `Play\_Main.png` | `Play\_Hover.png` | `Play\_Pressed.png` |

| Pause | `Pause\_Main.png` | `Pause\_Hover.png` | `Pause\_Pressed.png` |

| Stop | `Stop\_Main.png` | `Stop\_Hover.png` | `Stop\_Pressed.png` |



\### Button requirements



\- Use the actual PNG graphics from the `Individual\_Buttons` folder.

\- Preserve their transparent backgrounds.

\- Remove the old visible Play, Pause, and Stop text labels.

\- Display the normal image when idle.

\- Switch to the Hover image when the mouse pointer enters the button.

\- Switch to the Pressed image while the mouse button is held down.

\- Restore the appropriate image when the mouse is released or exits the button.

\- Keep all three buttons the same dimensions.

\- Initially use approximately 44 × 44 pixels per button, adjusting as necessary to fit the layout.

\- Maintain the original image aspect ratio without distortion.

\- Do not display default Windows button borders or colored button backgrounds.

\- Retain accessible button names and tooltips.

\- Ensure keyboard activation and disabled states remain functional.



Use the framework's native styling, templates, bindings, and mouse interaction mechanisms wherever possible.



Ensure all nine PNG images are included in the build and correctly loaded when the published application runs on another computer.



If the assets are not present in the project, identify the missing files rather than silently replacing them with unrelated icons.



\## Phase 2 — Reorganize the playback controls



The current application has the following layout:



1\. Top application menu.

2\. Main video display.

3\. Funscript heat map.

4\. Video seek bar.

5\. Centered Play, Pause, and Stop buttons.

6\. Bottom playback status bar.



Change this so the playback controls occupy a \*\*single compact section at the very bottom of the application, immediately above the existing status bar.\*\*



\### New layout



The bottom playback section should have two areas.



\*\*Left side: Playback buttons\*\*



\- Position the Play, Pause, and Stop image buttons at the far-left edge.

\- Arrange horizontally in the order Play, Pause, Stop.

\- Use approximately 6 pixels of spacing between buttons.

\- Vertically center the buttons in the playback section.

\- Keep left padding to approximately 8–10 pixels.

\- Do not center the buttons across the entire application.



\*\*Right side: Heat map and seek bar\*\*



\- Place the existing funscript heat map to the right of the playback buttons.

\- Position the seek bar directly beneath the heat map.

\- Both controls must begin at the same horizontal position.

\- Both controls should expand horizontally to fill all remaining available window width.

\- The heat map must retain its existing colors, scaling, and functionality.

\- The seek bar must retain its existing playback-position indicators, seeking interactions, and time synchronization.



\### Layout illustration



The bottom of the application should be organized approximately as follows:



```text

┌──────────────────────────────────────────────┐

│                                              │

│                                              │

│                 VIDEO                        │

│                                              │

│                                              │

├──────────────────────────────────────────────┤

│                 │ Funscript Heat Map         │

│  ▶    ❚❚    ■   │ ████▓▓████▓▓█████████      │

│                 │─────────────────────────── │

│                 │ Video Seek Bar             │

├──────────────────────────────────────────────┤

│ Status | Video Time | Diff | Volume | - | +  │

└──────────────────────────────────────────────┘

```



The three playback buttons should appear immediately to the left of the heat map and seek bar, sharing one compact horizontal control section.



The existing status bar must remain below this section.



\## Phase 3 — Improve resizing and positioning



Implement proper responsive desktop-window resizing.



Requirements:



1\. Anchor or dock the new playback section to the bottom of the application.

2\. Keep the existing status bar at the very bottom.

3\. Allow the main video region to grow and shrink as the application window changes size.

4\. Preserve the video's aspect ratio.

5\. Eliminate the previous standalone playback-button row.

6\. Prevent unnecessary empty space between the video and playback controls.

7\. Ensure the heat map and seek bar dynamically resize to the available width.

8\. Keep the playback buttons at fixed dimensions instead of stretching them.

9\. Prevent overlapping, clipping, or disappearing controls when the window is resized.

10\. Keep the new layout compatible with maximized and restored window states.



Do not introduce fixed screen-width positioning that would break on different window sizes or DPI scaling settings.



\## Phase 4 — Preserve existing functionality



All current playback features must continue working.



Specifically:



\- Play must continue playing or resuming the loaded video.

\- Pause must continue pausing video playback.

\- Stop must continue stopping playback and perform the application's existing reset behavior.

\- Preserve the current synchronization behavior between the video and funscript.

\- Preserve communication with the connected Autoblow device.

\- Preserve all existing seeking functionality.

\- Preserve the funscript heat map calculations, colors, and timeline alignment.

\- Preserve the video playback clock and synchronization-difference indicator.

\- Preserve the volume controls and status indicators.



Reuse the existing commands, bindings, events, and playback services.



\*\*Do not introduce new playback logic unless required to integrate the replacement controls.\*\*



\## Phase 5 — Build and validate



After implementing the changes:



1\. Verify all nine image resources are correctly referenced.

2\. Verify normal, hover, and pressed states work for all three buttons.

3\. Verify mouse and keyboard interactions.

4\. Confirm the buttons remain at the far left while the heat map and seek bar fill the remaining width.

5\. Confirm the playback section remains anchored immediately above the status bar.

6\. Verify the video resizes correctly without losing its aspect ratio.

7\. Build the existing .NET solution and fix any compilation errors introduced by your changes.

8\. Run available relevant tests.

9\. Review the modified files to ensure no unrelated functionality was removed.



If the application cannot be launched or visually tested in your environment, clearly identify those validations as pending rather than claiming they passed.



\## Phase 6 — Update project documentation



After completing the changes:



\- Update `Progress.md` to describe the completed work and any remaining issues.

\- Update `Plan.md` if the UI layout or development phases have changed.

\- Record the modified source files and any added image resources.

\- Document any manual testing still required.



\## Final instructions



Make the actual changes to the existing project files rather than simply providing example code.



Keep modifications focused on the playback controls and their layout.



Use the provided dark-gray PNG assets rather than generating new buttons.



Do not change the top menu, existing video engine, Autoblow connection logic, or synchronization algorithms.



When complete, provide a summary of the modified files, build results, and any outstanding issues.

