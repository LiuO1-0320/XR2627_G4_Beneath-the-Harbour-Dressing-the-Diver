using UnityEngine;

public static class AudioDiagnostics
{
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
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IdentifyBuild()
    {
        if (Disabled)
            Debug.Log("ER AUDIO DIAGNOSTIC: engine audio disabled at build time; video tracks and experience playback disabled. Unity=" + Application.unityVersion + " platform=" + Application.platform);
    }
}
