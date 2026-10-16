using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>Resolve local film references using AssetDatabase (including encoded Tuanjie GUIDs).</summary>
[InitializeOnLoad]
public static class ExhibitVideoSetup
{
    static readonly string[] ScreenNames = { "vedio1", "vedio2", "vedio3" };
    static readonly string[] Films = { "helmet_history.mp4", "helmet_component.mp4", "Diver.mp4" };

    static ExhibitVideoSetup()
    {
        EditorApplication.delayCall += BindOpenScenes;
        EditorSceneManager.sceneOpened += (scene, mode) => Bind(scene, false);
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) BindOpenScenes();
        };
    }

    [MenuItem("Tools/Experience/Video/Assign local exhibit films")]
    public static void BindOpenScenes()
    {
        if (EditorApplication.isPlaying) return;
        for (int i = 0; i < SceneManager.sceneCount; i++) Bind(SceneManager.GetSceneAt(i), false);
    }

    public static void Bind(Scene scene, bool building)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var screen in root.GetComponentsInChildren<ExhibitVideoScreen>(true))
        {
            int index = Array.IndexOf(ScreenNames, screen.name);
            if (index < 0) continue;
            string path = "Assets/Vedio/" + Films[index];
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
            if (!clip)
            {
                string error = "Exhibit film missing or not imported as VideoClip: " + path;
                if (building) throw new UnityEditor.Build.BuildFailedException(error);
                Debug.LogWarning(error, screen);
                continue;
            }
            if (screen.videoClip == clip) continue;
            if (!building) Undo.RecordObject(screen, "Assign local exhibit film");
            screen.videoClip = clip;
            if (!building)
            {
                EditorUtility.SetDirty(screen);
                if (PrefabUtility.IsPartOfPrefabInstance(screen)) PrefabUtility.RecordPrefabInstancePropertyModifications(screen);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log(screen.name + " video assigned: " + path, screen);
            }
        }
    }
}

public sealed class ExhibitVideoBuildSetup : UnityEditor.Build.IProcessSceneWithReport
{
    public int callbackOrder => 0;
    public void OnProcessScene(Scene scene, UnityEditor.Build.Reporting.BuildReport report)
    {
        ExhibitVideoSetup.Bind(scene, true);
    }
}
