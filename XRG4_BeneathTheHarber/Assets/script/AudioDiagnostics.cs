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
    public static bool AmbienceDisabled
    {
        get
        {
#if ER_AUDIO_DISABLED || ER_AUDIO_NO_AMBIENCE
            return true;
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
#else
            return false;
#endif
        }
    }
    public static string Mode => Disabled ? "AUDIO_DISABLED" : AmbienceDisabled ? "NO_AMBIENCE" :
        NarrationAndVideoDisabled ? "NO_NARRATION_VIDEO" : "NORMAL";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void IdentifyBuild()
    {
        Debug.Log("ER AUDIO DIAGNOSTIC: mode=" + Mode + " engineAudio=" + !Disabled +
            " ambience=" + !AmbienceDisabled + " narrationAndVideo=" + !NarrationAndVideoDisabled +
            " Unity=" + Application.unityVersion + " platform=" + Application.platform);
    }
}
