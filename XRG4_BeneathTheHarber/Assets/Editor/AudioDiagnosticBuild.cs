using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

// Audio settings are a build-time switch: muting AudioSources cannot isolate native resampling.
public sealed class AudioDiagnosticBuild : IProcessSceneWithReport
{
    public static bool Building { get; private set; }
    enum Mode { Normal, Disabled, NoAmbience, NoNarrationVideo }
    static Mode mode;
    public int callbackOrder => 1000;

    [MenuItem("Tools/Experience/Audio/Build Windows - NORMAL audio control")]
    public static void BuildNormal() { Build(Mode.Normal); }
    [MenuItem("Tools/Experience/Audio/Build Windows - NO AMBIENCE diagnostic")]
    public static void BuildNoAmbience() { Build(Mode.NoAmbience); }
    [MenuItem("Tools/Experience/Audio/Build Windows - NO NARRATION AND VIDEO diagnostic")]
    public static void BuildNoNarrationVideo() { Build(Mode.NoNarrationVideo); }


    [MenuItem("Tools/Experience/Audio/Build Windows - AUDIO DISABLED diagnostic")]
    public static void BuildWindows() { Build(Mode.Disabled); }

    static void Build(Mode requestedMode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before building.");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            throw new InvalidOperationException("Install Windows Build Support for this Tuanjie editor, then run this command again.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var diagnostics = new[] { "ER_AUDIO_DISABLED", "ER_AUDIO_NO_AMBIENCE", "ER_AUDIO_NO_NARRATION_VIDEO" };
        var globalDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(';');
        if (globalDefines.Any(d => diagnostics.Contains(d.Trim())))
            throw new BuildFailedException("Remove ER_AUDIO_* diagnostic symbols from Player Settings. Use the dedicated build menus to select the mode.");
        string symbol = requestedMode == Mode.Disabled ? diagnostics[0] : requestedMode == Mode.NoAmbience ? diagnostics[1] :
            requestedMode == Mode.NoNarrationVideo ? diagnostics[2] : null;
        string label = requestedMode == Mode.Disabled ? "AUDIO_DISABLED" : requestedMode == Mode.NoAmbience ? "NO_AMBIENCE" :
            requestedMode == Mode.NoNarrationVideo ? "NO_NARRATION_VIDEO" : "NORMAL";
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/BasicScene.unity" };
        string output = EditorUtility.SaveFilePanel("Windows audio test: " + label, "", "ER_" + label + ".exe", "exe");
        if (string.IsNullOrEmpty(output)) return;
        if (Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(output)).Any())
            throw new BuildFailedException("Choose a NEW EMPTY output folder for each audio variant to avoid mixing build files.");
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")[0]);
        var disabled = settings.FindProperty("m_DisableAudio");
        if (disabled == null) throw new BuildFailedException("Cannot locate Disable Unity Audio; diagnostic build cancelled.");
        bool original = disabled.boolValue;
        try
        {
            Building = true; mode = requestedMode;
            disabled.boolValue = requestedMode == Mode.Disabled; settings.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
            if (disabled.boolValue != (requestedMode == Mode.Disabled)) throw new BuildFailedException("Engine audio setting does not match selected mode.");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes, locationPathName = output, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
                extraScriptingDefines = symbol == null ? new string[0] : new[] { symbol }
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Audio diagnostic build failed: " + report.summary.result);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "AUDIO_DIAGNOSTIC.txt"),
                "Mode=" + label + "\nEngineAudio=" + (requestedMode != Mode.Disabled) +
                "\nAmbience=" + (requestedMode != Mode.Disabled && requestedMode != Mode.NoAmbience) +
                "\nNarrationAndVideo=" + (requestedMode != Mode.Disabled && requestedMode != Mode.NoNarrationVideo) +
                "\nFeedback=" + (requestedMode != Mode.Disabled) +
                "\nUTC=" + DateTime.UtcNow.ToString("O") + "\nUnity=" + Application.unityVersion +
                "\nBuildGUID=" + report.summary.guid + "\nOptions=Development\nScenes=" + string.Join(",", scenes) + "\n");
            Debug.Log("Audio diagnostic build completed: mode=" + label + " path=" + output);
        }
        finally
        {
            settings.Update(); settings.FindProperty("m_DisableAudio").boolValue = original;
            settings.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets(); Building = false;
        }
    }
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (!Building || report == null) return;
        // Only build copies are changed; authored scene components remain untouched.
        foreach (var root in scene.GetRootGameObjects())
        {
            if (mode == Mode.Disabled)
                foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
                { source.playOnAwake = false; source.clip = null; source.enabled = false; }
            if (mode == Mode.Disabled || mode == Mode.NoNarrationVideo)
                foreach (var video in root.GetComponentsInChildren<VideoPlayer>(true))
                { video.audioOutputMode = VideoAudioOutputMode.None; video.controlledAudioTrackCount = 0; }
        }
    }
}
