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
    public int callbackOrder => 1000;

    [MenuItem("Tools/Experience/Audio/Build Windows - AUDIO DISABLED diagnostic")]
    public static void BuildWindows()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before building.");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            throw new InvalidOperationException("Install Windows Build Support for this Tuanjie editor, then run this command again.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/BasicScene.unity" };
        string output = EditorUtility.SaveFilePanel("Audio-disabled diagnostic build", "", "ER_AudioDisabled.exe", "exe");
        if (string.IsNullOrEmpty(output)) return;
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset")[0]);
        var disabled = settings.FindProperty("m_DisableAudio");
        if (disabled == null) throw new BuildFailedException("Cannot locate Disable Unity Audio; diagnostic build cancelled.");
        bool original = disabled.boolValue;
        try
        {
            Building = true;
            disabled.boolValue = true; settings.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
            if (!disabled.boolValue) throw new BuildFailedException("Engine audio was not disabled.");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes, locationPathName = output, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
                extraScriptingDefines = new[] { "ER_AUDIO_DISABLED" }
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Audio diagnostic build failed: " + report.summary.result);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "AUDIO_DIAGNOSTIC.txt"),
                "ER_AUDIO_DISABLED\nEngine audio disabled; video audio disabled; experience playback disabled.\n" +
                "UTC=" + DateTime.UtcNow.ToString("O") + "\nUnity=" + Application.unityVersion +
                "\nScenes=" + string.Join(",", scenes) + "\n");
            Debug.Log("Audio-disabled diagnostic build completed: " + output);
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
            foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
            { source.playOnAwake = false; source.clip = null; source.enabled = false; }
            foreach (var video in root.GetComponentsInChildren<VideoPlayer>(true))
            { video.audioOutputMode = VideoAudioOutputMode.None; video.controlledAudioTrackCount = 0; }
        }
    }
}
