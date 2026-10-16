using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AudioSetup
{
    [InitializeOnLoadMethod]
    static void ScheduleImport()
    {
        // Resolve through AssetDatabase instead of copying GUID text: Tuanjie may encrypt .meta GUIDs.
        EditorApplication.delayCall += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode) ImportNarration(); };
    }
    [MenuItem("Tools/Experience/Audio/Validate local audio assets")]
    public static void Validate()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var map = Resources.Load<AudioMap>("ExperienceAudio/DefaultAudioMap");
        if (!map || !map.mixer) throw new InvalidOperationException("Missing audio map or mixer.");
        foreach (var name in new[] { "Narration", "Ambience", "Feedback" })
            if (!map.mixer.FindMatchingGroups(name).Any(g => g.name == name)) throw new InvalidOperationException("Missing bus: " + name);
        if (!map.narrationGroup || !map.ambienceGroup || !map.feedbackGroup) throw new InvalidOperationException("Unassigned mixer routing.");
        foreach (var clip in new[] { map.museumMusic, map.harbour, map.underwater, map.interaction, map.success })
        {
            if (!clip || clip.length <= 0) throw new InvalidOperationException("Missing or empty local sound clip.");
            Debug.Log("Experience AUDIO ASSET: " + clip.name + " duration=" + clip.length + " channels=" + clip.channels);
        }
        foreach (var stage in map.stages)
            Debug.Log("Experience AUDIO NARRATION: " + stage.stage + " clips=" + stage.narration.Length);
        Debug.Log("Experience AUDIO VALIDATION PASSED");
    }

    [MenuItem("Tools/Experience/Audio/Import ACT1-3 narration by filename")]
    public static void ImportNarration()
    {
        const string folder = "Assets/Audio/Narration";
        if (!AssetDatabase.IsValidFolder(folder)) { Debug.LogWarning("Experience: missing " + folder); return; }
        var map = Resources.Load<AudioMap>("ExperienceAudio/DefaultAudioMap");
        if (!map) throw new InvalidOperationException("Missing default audio map.");
        var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).ToArray();
        ExperienceStage[] stages = { ExperienceStage.TheCase, ExperienceStage.ScaleUp, ExperienceStage.DressTheDiver };
        for (int i = 0; i < stages.Length; i++)
        {
            string pattern = @"(?i)(^|[^a-z0-9])act[ _-]*0?" + (i + 1) + @"([^0-9]|$)";
            var matches = clips.Where(c => System.Text.RegularExpressions.Regex.IsMatch(c.name, pattern)).ToArray();
            var entry = Array.Find(map.stages, s => s.stage == stages[i]);
            if (entry == null || entry.narration.Length > 0) continue;
            if (matches.Length == 1) { entry.narration = matches; EditorUtility.SetDirty(map); }
            else Debug.LogWarning("Experience: ACT" + (i + 1) + " has " + matches.Length + " candidates. Assign approved recordings manually; no ordering is guessed.");
        }
        AssetDatabase.SaveAssets();
    }

    // Invoked with -executeMethod AudioSetup.BatchValidate; failures produce nonzero exit.
    public static void BatchValidate()
    {
        try { ImportNarration(); Validate(); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}

public sealed class AudioBuildCheck : UnityEditor.Build.IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        AudioSetup.ImportNarration();
        AudioSetup.Validate();
    }
}
