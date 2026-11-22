# Audio crash isolation — editor playback tests

## Editor-only playback isolation

Open `Tools > Experience > Audio > Editor audio test panel`.
Exit Play, choose Normal / No ambience / No narration and video / No playback / No narration / No video audio,
then enter Play and connect the headset as usual. Controls are locked during Play.
The selection lasts for the current editor session only, is not committed to the
project and is not included in player builds. The selected mode is captured before
scene Awake, including when domain reload is disabled. Check the Console startup
line `ER AUDIO DIAGNOSTIC: mode=...` for each run.

No playback skips experience audio source creation, blocks video audio, and stops
authored scene AudioSources after scene load. It does not shut down the editor's
native audio system/device and does not fully disable native engine audio.
Scene playOnAwake audio can initialize before sceneLoaded. The dedicated Windows
audio diagnostic build menus have been removed; ordinary Build uses normal player settings. Partial modes gate the
project AudioDirector and ExhibitVideoScreen; independently added third-party
sources are not classified automatically. Existing task and water detection stay active.

For editor tests, collect the editor log, not Player.log. Copy it after each run:
Windows `%LOCALAPPDATA%/Tuanjie/Editor/Editor.log` (if absent, check the editor's
Console menu > Open Editor Log); macOS `~/Library/Logs/Tuanjie/Editor.log`.
Use the actual log opened by the editor if the installation uses a different path.

For the next split comparison, alternate NO_NARRATION and NO_VIDEO using the same
operations and similar duration; include video play/pause/seek and region-arrival
narration triggers. Exit Play before every change. Verify the startup log's Mode,
narration and video fields. Save a copy of Editor.log immediately after each run.
Neither split mode is a mute slider or a confirmed fix; repeated non-crash runs
alone do not prove which subsystem caused a prior crash. Preserve every crash dump.
