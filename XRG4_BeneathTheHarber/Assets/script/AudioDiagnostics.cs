using UnityEngine;

public static class AudioDiagnostics
{
#if UNITY_EDITOR
    public const string EditorModeKey = "ER.AudioDiagnostics.EditorMode";
    static int editorMode;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void CaptureEditorMode()
    {
        // Snapshot before scene Awake, including when domain reload is disabled.
        editorMode = UnityEditor.SessionState.GetInt(EditorModeKey, 0);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SilenceAuthoredSceneAudio;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += SilenceAuthoredSceneAudio;
    }
    static void SilenceAuthoredSceneAudio(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (!PlaybackDisabled) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
            { source.Stop(); source.playOnAwake = false; source.enabled = false; }
            foreach (var video in root.GetComponentsInChildren<UnityEngine.Video.VideoPlayer>(true))
            { video.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None; video.controlledAudioTrackCount = 0; }
        }
    }
#endif
    public static bool PlaybackDisabled
    {
        get
        {
#if UNITY_EDITOR
            return Disabled || editorMode == 3;
#else
            return Disabled;
#endif
        }
    }

    public static bool Disabled
    {
        get
        {
#if ER_AUDIO_DISABLED
            return true;
#else
            return false;
#endif
        }
    }
    public static bool AmbienceDisabled
    {
        get
        {
#if ER_AUDIO_DISABLED || ER_AUDIO_NO_AMBIENCE
            return true;
#elif UNITY_EDITOR
            return editorMode == 1 || editorMode == 3;
#else
            return false;
#endif
        }
    }
    public static bool NarrationAndVideoDisabled
    {
        get
        {
#if ER_AUDIO_DISABLED || ER_AUDIO_NO_NARRATION_VIDEO
            return true;
#elif UNITY_EDITOR
            return editorMode == 2 || editorMode == 3;
#else
            return false;
#endif
        }
    }
    public static string Mode => Disabled ? "AUDIO_DISABLED" : PlaybackDisabled ? "EDITOR_NO_PLAYBACK_ENGINE_ON" : AmbienceDisabled ? "NO_AMBIENCE" :
        NarrationAndVideoDisabled ? "NO_NARRATION_VIDEO" : "NORMAL";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IdentifyBuild()
    {
        Debug.Log("ER AUDIO DIAGNOSTIC: mode=" + Mode + " engineAudio=" + !Disabled +
            " ambience=" + !AmbienceDisabled + " narrationAndVideo=" + !NarrationAndVideoDisabled +
            " Unity=" + Application.unityVersion + " platform=" + Application.platform);
    }
}
