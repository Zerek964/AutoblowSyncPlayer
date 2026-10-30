# Manual Test Checklist — Autoblow Sync Player Phase 7

## Overview
This checklist covers manual verification of all VR3D features. Tests marked with ⚠️ require real hardware (VR video files or physical Autoblow device).

---

## 1. Standard 2D Playback Regression

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 1.1 | Open a standard MP4 video | Video plays normally, no visual artifacts | ☐ |
| 1.2 | Play/Pause/Stop controls | All controls work correctly | ☐ |
| 1.3 | Seek to different position | Video jumps to correct timestamp | ☐ |
| 1.4 | Volume control | Volume changes without affecting sync | ☐ |
| 1.5 | Load `.funscript` file | File loads and validates without errors | ☐ |
| 1.6 | Drag-and-drop video file | Video loads and starts playing | ☐ |
| 1.7 | Close video, open another | Old video releases, new video plays | ☐ |

---

## 2. VR360° Playback ⚠️ (Requires VR360 MP4)

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 2.1 | Load a 360° equirectangular MP4 | Video renders as spherical projection | ☐ |
| 2.2 | Set mode to VR360Mono via UI | Camera controls become enabled | ☐ |
| 2.3 | Click and drag to look around | View rotates smoothly in all directions | ☐ |
| 2.4 | Scroll mouse wheel | Zoom in (FOV decreases) and out (FOV increases) | ☐ |
| 2.5 | Zoom to minimum FOV (~30°) | Zoom stops at minimum, no crash | ☐ |
| 2.6 | Zoom to maximum FOV (~120°) | Zoom stops at maximum, no crash | ☐ |
| 2.7 | Press R to reset camera | Camera returns to forward-facing (yaw=0, pitch=0, fov=90) | ☐ |
| 2.8 | Look around rapidly | No stuttering, no crashes | ☐ |
| 2.9 | Audio plays during camera movement | Audio is continuous, no gaps | ☐ |
| 2.10 | Playback timer advances during camera movement | Timer is unaffected by camera control | ☐ |

---

## 3. VR180° Playback ⚠️ (Requires VR180 MP4)

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 3.1 | Load a VR180 MP4 | Video renders correctly in VR180 mode | ☐ |
| 3.2 | Camera yaw is clamped to ±90° | Cannot rotate beyond 180° field of view | ☐ |
| 3.3 | Pitch still works ±90° | Can look up and down normally | ☐ |
| 3.4 | Reset camera works | Returns to center view | ☐ |

---

## 4. 3D Stereo Playback ⚠️ (Requires SBS or OU MP4)

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 4.1 | Load SBS video, set mode to Flat3dSbsLeft | Left eye view rendered | ☐ |
| 4.2 | Switch stereo eye to Right | Right eye view rendered | ☐ |
| 4.3 | Load OU video, set mode to OverUnder | Correct rendering | ☐ |

---

## 5. Sync Isolation (Camera + Funscript) ⚠️ (Requires VR Video + Device)

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 5.1 | Connect device, upload funscript, start playback | Sync starts normally | ☐ |
| 5.2 | Move camera during synced playback | Device continues syncing, no interruption | ☐ |
| 5.3 | Seek video while moving camera | Seek works, camera doesn't break sync | ☐ |
| 5.4 | Pause/Resume during camera movement | Pause/resume works correctly | ☐ |
| 5.5 | Reset camera (R key) during synced playback | Camera resets, sync unaffected | ☐ |

---

## 6. Settings Persistence

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 6.1 | Change camera sensitivity, close app, reopen | Sensitivity persists | ☐ |
| 6.2 | Invert vertical mouse, close app, reopen | Invert setting persists | ☐ |
| 6.3 | Set last video mode, close app, reopen | Video mode persists | ☐ |
| 6.4 | Verify settings.json contains camera fields | JSON has CameraYaw, CameraPitch, etc. | ☐ |

---

## 7. Fullscreen Mode

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 7.1 | Enter fullscreen with standard video | Fullscreen works normally | ☐ |
| 7.2 | Enter fullscreen with VR360 video | Fullscreen + camera controls work | ☐ |
| 7.3 | Exit fullscreen | Returns to windowed mode | ☐ |

---

## 8. Performance & Stability

| # | Test | Expected Result | Status |
|---|------|-----------------|--------|
| 8.1 | Play 1080p video for 10+ minutes | No memory leak, stable playback | ☐ |
| 8.2 | Load/unload 5 different videos | Resources cleanup properly | ☐ |
| 8.3 | Rapid camera movement for 1 minute | No stuttering, no crash | ☐ |
| 8.4 | Switch between 2D and VR modes | Clean transition, no artifacts | ☐ |

---

## Test Results Summary

| Category | Tests | Pass | Fail | N/A |
|----------|-------|------|------|-----|
| 2D Regression | 7 | | | |
| VR360 Playback | 10 | | | |
| VR180 Playback | 4 | | | |
| 3D Stereo | 3 | | | |
| Sync Isolation | 5 | | | |
| Settings | 4 | | | |
| Fullscreen | 3 | | | |
| Performance | 4 | | | |
| **Total** | **40** | | | |

---

## Notes

- Tests marked ⚠️ require specific media files or hardware that may not be available during every session.
- Untested scenarios should be marked N/A rather than Fail.
- Record any unexpected behavior in the section below.

### Issues Found

(Record any issues discovered during manual testing)

