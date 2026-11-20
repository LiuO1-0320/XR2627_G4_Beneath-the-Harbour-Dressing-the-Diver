# Audio crash isolation — first diagnostic build

This is an isolation experiment, not a confirmed repair for the native resampler crash.

## Build

1. Exit Play mode and save the project.
2. Use the same Tuanjie version as the baseline (current project: 1.6.14).
3. Install Windows Build Support for that editor if necessary.
4. Select `Tools > Experience > Audio > Build Windows - AUDIO DISABLED diagnostic`.
5. Choose a NEW empty output folder and the executable name. Do not overwrite v1.
6. Copy the entire generated folder to the test PC, including the Data directory and DLLs.

The command temporarily enables `Disable Unity Audio`, adds the build-only
`ER_AUDIO_DISABLED` define, disables audio on built scene copies, and restores the
original audio setting in `finally`, including after a normal build failure.
Normal Build Settings builds retain the project's original audio configuration.
If the EDITOR itself crashes while building, check Project Settings > Audio >
Disable Unity Audio before the next normal build: a process crash cannot run finally.

Verify `AUDIO_DIAGNOSTIC.txt` alongside the executable and the startup log line
`ER AUDIO DIAGNOSTIC: engine audio disabled at build time` in Player.log.
A silent volume slider alone is not proof that the correct package was tested.
Video images and controls remain available, with video audio output set to None.
The experience audio director creates no audio sources in this build.
Water-position detection remains active; narration replay is unavailable.
Task completion remains driven by interactions, not fabricated narration-finished events.

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
This change implements only the fully-disabled comparison, not those later variants.
