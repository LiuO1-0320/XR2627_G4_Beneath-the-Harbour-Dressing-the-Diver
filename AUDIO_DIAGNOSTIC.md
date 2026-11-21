# Audio crash isolation — Windows comparison builds

This is an isolation experiment, not a confirmed repair for the native resampler crash.

## Build

Use Tuanjie 1.6.13 and the Windows IL2CPP/C++ toolchain on the build computer.
Exit Play mode and save, then use `Tools > Experience > Audio`:

| Menu suffix | Mode in marker/log | Playback |
| --- | --- | --- |
| Build Windows - NORMAL audio control | NORMAL | All current audio enabled |
| Build Windows - AUDIO DISABLED diagnostic | AUDIO_DISABLED | Engine audio off, all video tracks off |
| Build Windows - NO AMBIENCE diagnostic | NO_AMBIENCE | Museum music, harbour and underwater loops off; narration, video and feedback on |
| Build Windows - NO NARRATION AND VIDEO diagnostic | NO_NARRATION_VIDEO | All AudioDirector narration (including helmet interior) and video tracks off; ambience and feedback on |

NPC text, animation and task interactions remain available. No mode restores the
previously removed part pickup sounds. Partial modes prevent the relevant clips
from being scheduled/played; they do not merely lower the volume to zero.

Choose a NEW EMPTY output folder for each variant. All four menus use Development
Build and the current project backend; use the same commit, scenes, backend and
engine version for comparisons. The menus do not install Windows Build Support,
Visual Studio C++ tools or a Windows SDK. Do not set ER_AUDIO_* symbols manually
in Player Settings: the menus pass them only to the selected player build.

Each successful build writes AUDIO_DIAGNOSTIC.txt beside the executable with mode,
audio switches, UTC time, engine version and BuildGUID. Player.log prints the same
mode at startup (`ER AUDIO DIAGNOSTIC: mode=...`). A marker is written for NORMAL too,
so its mere presence no longer means fully silent: read its Mode field.

The command temporarily changes Disable Unity Audio (off for normal/partial modes,
on only for fully disabled), then restores the original value even after a normal
build failure. The build copies of scenes are processed; source scenes are untouched.
If the EDITOR itself crashes while building, check Project Settings > Audio before
the next build: a process crash cannot run finally.

Copy the ENTIRE successful output folder to the headset test computer, including
its Data directory and DLLs. That computer needs no IL2CPP development tools.
Keep generated BackUpThisFolder/Burst debug information for crash analysis.

## Same-machine test

Record executable/folder, source commit, engine version, Windows version, headset,
XR runtime/version, actual output device, start/end time and each outcome.
Use the same PC/headset that produced v1 crashes; do not change engine, drivers,
audio formats and gameplay together during the first comparison.

Repeat at least 10 full rounds, also exceeding the previous longest time to failure:
- ACT2: grab/return every part; play/pause/seek the video.
- ACT4: NPC explanation; descend, enter water, return to land.
- Pause/resume, Back to ACT2/ACT3, reset and restart.

Retain Player.log and crash dumps for each run. Record whether XR audio-device
errors persist, even if the test does not crash. No-crash runs increase confidence;
they do not prove the root cause or guarantee future stability.

If it still crashes, resolve the NEW package's stack using its matching symbols.
If stable, the next comparison is engine audio enabled with no playback, then
restore narration, video sound, ambience and water/feedback separately.
The menus implement the four comparisons above. An engine-on/no-playback variant
is not yet implemented; add it if the initial results require device-only isolation.

## Editor-only playback isolation

Open `Tools > Experience > Audio > Editor audio test panel`.
Exit Play, choose Normal / No ambience / No narration and video / No playback,
then enter Play and connect the headset as usual. Controls are locked during Play.
The selection lasts for the current editor session only, is not committed to the
project and is not included in player builds. The selected mode is captured before
scene Awake, including when domain reload is disabled. Check the Console startup
line `ER AUDIO DIAGNOSTIC: mode=...` for each run.

No playback skips experience audio source creation, blocks video audio, and stops
authored scene AudioSources after scene load. It does not shut down the editor's
native audio system/device and is not equivalent to AUDIO_DISABLED builds.
Scene playOnAwake audio can initialize before sceneLoaded, so use the fully-disabled
standalone package for complete native-audio isolation. Partial modes gate the
project AudioDirector and ExhibitVideoScreen; independently added third-party
sources are not classified automatically. Existing task and water detection stay active.

For editor tests, collect the editor log, not Player.log. Copy it after each run:
Windows `%LOCALAPPDATA%/Tuanjie/Editor/Editor.log` (if absent, check the editor's
Console menu > Open Editor Log); macOS `~/Library/Logs/Tuanjie/Editor.log`.
Use the actual log opened by the editor if the installation uses a different path.
