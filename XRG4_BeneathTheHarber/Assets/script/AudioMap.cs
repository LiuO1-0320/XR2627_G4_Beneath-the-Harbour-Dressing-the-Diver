using System;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Experience/Audio map")]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4AudioMap")]
public sealed class AudioMap : ScriptableObject
{
    [Serializable] public sealed class StageAudio
    {
        public ExperienceStage stage;
        [Tooltip("Approved local recordings, played sequentially. Empty means silent, never generated speech.")]
        public AudioClip[] narration = new AudioClip[0];
    }
    public AudioMixer mixer;
    public AudioMixerGroup narrationGroup, ambienceGroup, feedbackGroup;
    public AudioClip harbour, underwater, interaction, success;
    [Tooltip("Local gentle music away from the harbour; selected by visitor position, not stage.")]
    public AudioClip museumMusic;
    public AudioClip waterEntry;
    [Range(0, 1)] public float waterEntryVolume = 0.55f;
    public AudioClip waterExit;
    [Range(0, 1)] public float waterExitVolume = 0.5f;
    public AudioClip[] insideNarration = new AudioClip[0];
    public StageAudio[] stages = new StageAudio[0];
    [Range(0, 1)] public float narrationVolume = 0.8f;
    [Range(0, 0.5f)] public float ambienceVolume = 0.25f;
    [Range(0, 0.5f)] public float feedbackVolume = 0.18f;
    [Range(0, 1)] public float duckMultiplier = 0.3f;
    [Min(0.1f)] public float crossfadeSeconds = 2.5f;
    public AudioClip[] Narration(ExperienceStage stage)
    {
        foreach (var entry in stages) if (entry != null && entry.stage == stage) return entry.narration ?? new AudioClip[0];
        return new AudioClip[0];
    }
}
