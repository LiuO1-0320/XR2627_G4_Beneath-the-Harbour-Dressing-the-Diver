using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>Local-only three-bus audio. Coordinator is the only stage authority.</summary>
[DefaultExecutionOrder(1100)]
[DisallowMultipleComponent]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4AudioDirector")]
public sealed class AudioDirector : MonoBehaviour, IResettable
{
    public int ResetOrder => -700;
    public void PrepareReset() { StopAll(); }
    public void RestoreReset() { StopAll(); session=null; }

    public AudioMap map;
    [Header("Location-based ambience")]
    [Tooltip("Optional harbour area. If empty, use the existing water volumes' shoreline bounds.")]
    public Collider harbourArea;
    [Min(0)] public float harbourFullDistance = 5f;
    [Min(.1f)] public float harbourFadeDistance = 14f;
    public string Status { get; private set; } = "Not initialized";
    public bool NarrationPlaying => voice && voice.isPlaying;
    public bool IsVisitorUnderwater => headInWater;
    public float NarrationPosition => voice && voice.clip ? voice.time : 0;
    public string CurrentNarration => voice && voice.clip ? voice.clip.name : "";
    public string Soundscape => active ? (submerged ? "Underwater" :
        harbourBlend < .01f ? "Museum music" : harbourBlend > .99f ? "Harbour" : "Museum / harbour transition") : "Silent";
    AudioSource voice, museum, harbour, underwater, feedback, splash;
    AudioLowPassFilter museumFilter, harbourFilter, underwaterFilter;
    readonly Queue<AudioClip> pending = new Queue<AudioClip>();
    readonly HashSet<AudioClip> scheduled = new HashSet<AudioClip>();
    readonly NarrationArrivalGate arrivalGate = new NarrationArrivalGate();
    readonly RegionNarrationHistory regionHistory = new RegionNarrationHistory();
    StageCoordinator coordinator;
    float nextReplayTime;
    DivingHelmetWearable[] helmets;
    float harbourBlend;
    WaterVolumeSwimming[] waterVolumes;
    bool headInWater;
    WaterVolumeSwimming currentWater;
    bool suppressWaterTransition, waterBlendTransition;
    ExperienceStage stage;
    string session;
    bool initialized, active, paused, submerged, insideScheduled, hadVoice;
    float blend, envelope, voiceEnvelope, duck = 1, nextFeedback;

    void Initialize()
    {
        if (initialized) return;
        coordinator = GetComponent<StageCoordinator>();
        if (!map) map = Resources.Load<AudioMap>("ExperienceAudio/DefaultAudioMap");
        voice = Source("Narration", map ? map.narrationGroup : null, false, 0);
        museum = Source("Museum music", map ? map.ambienceGroup : null, true, 128);
        harbour = Source("Harbour", map ? map.ambienceGroup : null, true, 128);
        underwater = Source("Underwater", map ? map.ambienceGroup : null, true, 128);
        feedback = Source("Feedback", map ? map.feedbackGroup : null, false, 64);
        splash = Source("Water entry", map ? map.feedbackGroup : null, false, 48);
        harbourFilter = harbour.gameObject.AddComponent<AudioLowPassFilter>();
        museumFilter = museum.gameObject.AddComponent<AudioLowPassFilter>();
        underwaterFilter = underwater.gameObject.AddComponent<AudioLowPassFilter>();
        harbourFilter.cutoffFrequency = 12000; underwaterFilter.cutoffFrequency = 850;
        helmets = FindObjectsOfType<DivingHelmetWearable>(true);
        waterVolumes = FindObjectsOfType<WaterVolumeSwimming>(true);
        initialized = true;
        Status = map ? "Ready" : "Missing Resources/ExperienceAudio/DefaultAudioMap";
    }
    AudioSource Source(string label, AudioMixerGroup group, bool loop, int priority)
    {
        var child = new GameObject("Experience Audio - " + label); child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>(); source.playOnAwake = false;
        source.loop = loop; source.spatialBlend = 0; source.volume = 0;
        source.priority = priority; source.outputAudioMixerGroup = group;
        return source;
    }
    public void Synchronize(SessionState state, AudioClip legacyNarration)
    {
        Initialize();
        if (!isActiveAndEnabled) { StopAll(); return; }
        if (!map) return;
        bool nextActive = AudioPolicy.Active(state.Status);
        if (!nextActive) { StopAll(); session = null; return; }
        bool changed = !active || AudioPolicy.StageChanged(session, stage, state.SessionId.ToString(), state.Stage);
        if (changed)
        {
            bool newSession = session != state.SessionId.ToString();
            if (newSession) { scheduled.Clear(); insideScheduled = false; }
            // Let genuine success feedback finish across a normal transition; Skip/Reset cancel it.
            bool completedTransition = active && !newSession && stage != state.Stage && state.Result(stage) == StageResult.Completed;
            voice.Stop(); voice.clip = null;
            if (!completedTransition)
            {
                feedback.Stop(); splash.Stop();
                suppressWaterTransition = true;
            }
            pending.Clear(); hadVoice = false;
            voiceEnvelope = 0;
            session = state.SessionId.ToString(); stage = state.Stage; active = true;
            regionHistory.Begin(session);
            arrivalGate.Begin(session, stage);
            if (!harbour.clip) harbour.clip = map.harbour;
            if (!museum.clip) museum.clip = map.museumMusic;
            if (!underwater.clip) underwater.clip = map.underwater;
            if (harbour.clip && !harbour.isPlaying && !paused) harbour.Play();
            if (museum.clip && !museum.isPlaying && !paused) museum.Play();
            if (underwater.clip && !underwater.isPlaying && !paused) underwater.Play();
            // Existing authored stage recordings take precedence over the default map.
            Status = "Waiting for first arrival at a narrated region";
            SessionTelemetry.Report("AudioStage", this, stage + ": " + Status);
        }
        SetPaused(state.Status == SessionStatus.Paused);
    }
    public void ConfirmArrival(Guid sessionId, ExperienceStage arrivedStage)
    {
        if (!active || !arrivalGate.Confirm(sessionId.ToString(), arrivedStage, !paused)) return;
        Status = pending.Count > 0 ? "Narration queued after arrival" : "No approved narration assigned for " + stage;
        SessionTelemetry.Report("NarrationArrivalConfirmed", this, arrivedStage.ToString());
        RequestRegionNarration(sessionId, arrivedStage, false);
    }
    AudioClip[] RegionClips(ExperienceStage region)
    {
        if (coordinator && (int)region < coordinator.stages.Length && coordinator.stages[(int)region].narration)
            return new[] { coordinator.stages[(int)region].narration };
        return map ? map.Narration(region) : new AudioClip[0];
    }
    public bool HasRegionNarration(ExperienceStage region) => Array.Exists(RegionClips(region), clip => clip);
    public bool RequestRegionNarration(Guid id, ExperienceStage region, bool replay)
    {
        if (!active || paused || !coordinator || !coordinator.NarrationArrivalReady(region) || !HasRegionNarration(region)) return false;
        var state = coordinator.Controller.Session;
        if (replay && Time.unscaledTime < nextReplayTime) return false;
        if (!regionHistory.Request(id.ToString(), region, replay, state.Stage == ExperienceStage.FreeExploration,
            state.Status == SessionStatus.Running && state.Stage != ExperienceStage.ExperienceComplete)) return false;
        if (replay) nextReplayTime = Time.unscaledTime + .75f;
        voice.Stop(); voice.clip = null; pending.Clear(); hadVoice = false; voiceEnvelope = 0;
        foreach (var clip in RegionClips(region)) if (clip) pending.Enqueue(clip);
        Status = (replay ? "Replaying region: " : "First visit narration: ") + region;
        SessionTelemetry.Report(replay ? "NarrationReplayRequested" : "RegionFirstNarration", this, region.ToString());
        return true;
    }
    void Enqueue(AudioClip[] clips)
    {
        if (clips == null) return;
        foreach (var clip in clips) if (clip && scheduled.Add(clip)) pending.Enqueue(clip);
    }
    void SetPaused(bool value)
    {
        if (paused == value) return;
        paused = value;
        foreach (var source in new[] { voice, museum, harbour, underwater, feedback, splash })
            if (value) source.Pause(); else source.UnPause();
    }
    void LateUpdate()
    {
        if (!initialized || !map || !active || paused) return;
        // Physical region visits are independent of task success; first visits in free mode count too.
        if (coordinator)
        {
            if (HasRegionNarration(stage) && coordinator.IsVisitorInNarrationRegion(stage))
                RequestRegionNarration(coordinator.Controller.Session.SessionId, stage, false);
            else for (int i = 0; i <= (int)ExperienceStage.SeabedCleanup; i++)
                if (HasRegionNarration((ExperienceStage)i) && coordinator.IsVisitorInNarrationRegion((ExperienceStage)i))
                { RequestRegionNarration(coordinator.Controller.Session.SessionId, (ExperienceStage)i, false); break; }
        }
        bool worn = false;
        foreach (var helmet in helmets)
            if (helmet && helmet.IsWorn && !helmet.GetComponent<DiverNpcInteraction>() && Camera.main
                && helmet.head == Camera.main.transform) { worn = true; break; }
        bool nowInWater = false;
        WaterVolumeSwimming detectedWater = null;
        if (Camera.main) foreach (var water in waterVolumes)
            if (water && water.ContainsHead(Camera.main.transform.position, headInWater && water == currentWater))
            { nowInWater = true; detectedWater = water; break; }
        if (!suppressWaterTransition)
        {
            if (nowInWater && !headInWater) PlayWaterEntry();
            if (!nowInWater && headInWater)
            {
                if (Camera.main && currentWater && currentWater.IsAboveSurface(Camera.main.transform.position)) PlayWaterExit();
                else splash.Stop();
            }
        }
        if (headInWater != nowInWater) waterBlendTransition = true;
        suppressWaterTransition = false; currentWater = detectedWater;
        headInWater = nowInWater;
        submerged = AudioPolicy.Underwater(headInWater);
        if (worn && !insideScheduled)
        {
            insideScheduled = true; Enqueue(map.insideNarration);
            SessionTelemetry.Report("AudioHelmetInterior", this, "Actual visitor helmet fitted");
        }
        if (!voice.isPlaying)
        {
            if (hadVoice)
            {
                SessionTelemetry.Report("NarrationFinished", this, CurrentNarration); hadVoice = false;
                if (pending.Count == 0) Status = "Narration complete";
            }
            if (pending.Count > 0)
            {
                var next = pending.Peek();
                if (next.loadState == AudioDataLoadState.Unloaded) next.LoadAudioData();
                if (next.loadState == AudioDataLoadState.Failed)
                {
                    pending.Dequeue(); Status = "Narration audio failed to load: " + next.name;
                    SessionTelemetry.Report("NarrationLoadFailed", this, next.name);
                }
                else if (next.loadState == AudioDataLoadState.Loaded)
                {
                    voice.clip = pending.Dequeue(); voice.volume = 0; voiceEnvelope = 0; voice.Play(); hadVoice = true;
                    Status = "Playing: " + CurrentNarration;
                    SessionTelemetry.Report("NarrationStarted", this, CurrentNarration);
                }
                else Status = "Loading narration: " + next.name;
            }
        }
        float dt = Time.unscaledDeltaTime;
        voiceEnvelope = Mathf.MoveTowards(voiceEnvelope, voice.isPlaying ? 1 : 0, dt / 0.18f);
        float ending = voice.clip ? Mathf.Clamp01((voice.clip.length - voice.time) / 0.15f) : 0;
        voice.volume = Mathf.Clamp01(map.narrationVolume) * voiceEnvelope * ending;
        // Acoustic space changes at the surface crossing; splash overlays it rather than delaying it.
        float transition = waterBlendTransition ? 0.12f : Mathf.Max(0.1f, map.crossfadeSeconds);
        blend = Mathf.MoveTowards(blend, submerged ? 1 : 0, dt / transition);
        if (Mathf.Approximately(blend, submerged ? 1 : 0)) waterBlendTransition = false;
        envelope = Mathf.MoveTowards(envelope, 1, dt / Mathf.Max(0.1f, map.crossfadeSeconds));
        duck = Mathf.MoveTowards(duck, voice.isPlaying ? Mathf.Clamp01(map.duckMultiplier) : 1, dt / (voice.isPlaying ? 0.25f : 1.2f));
        float volume = Mathf.Clamp(map.ambienceVolume, 0, 0.5f) * envelope * duck;
        float harbourTarget = HarbourAtVisitorPosition();
        if (!museum.clip) harbourTarget = 1;
        // The spatial curve is already smooth. Temporal damping adds direction/speed-dependent
        // lag, so the same physical position must always produce the same blend immediately.
        harbourBlend = Mathf.Clamp01(harbourTarget);
        // Constant-power blend keeps unrelated music and wind/waves audible through the handover.
        museum.volume = volume * (1 - blend) * Mathf.Cos(harbourBlend * Mathf.PI * .5f);
        harbour.volume = volume * (1 - blend) * Mathf.Sin(harbourBlend * Mathf.PI * .5f);
        underwater.volume = volume * blend;
        // The distant shore first sounds soft, then gains detail. The museum gently
        // loses brightness as it recedes, without stopping or restarting either loop.
        museumFilter.cutoffFrequency = Mathf.Exp(Mathf.Lerp(Mathf.Log(12000), Mathf.Log(2800), harbourBlend));
        float shoreCutoff = Mathf.Exp(Mathf.Lerp(Mathf.Log(2200), Mathf.Log(12000), harbourBlend));
        harbourFilter.cutoffFrequency = Mathf.Lerp(shoreCutoff, 900, blend);
    }
    float HarbourAtVisitorPosition()
    {
        if (!Camera.main) return harbourBlend;
        Vector3 position = Camera.main.transform.position;
        float distance = float.PositiveInfinity;
        if (harbourArea && harbourArea.enabled && harbourArea.gameObject.activeInHierarchy)
            distance = HorizontalDistance(position, harbourArea.bounds);
        else foreach (var water in waterVolumes)
            if (water) distance = Mathf.Min(distance, water.DistanceToShore(position));
        return AudioPolicy.HarbourByDistance(distance, harbourFullDistance, harbourFadeDistance);
    }
    public static float HorizontalDistance(Vector3 point, Bounds bounds)
    {
        point.y = bounds.center.y;
        return Vector3.Distance(point, bounds.ClosestPoint(point));
    }
    public void PlayFeedback(bool success)
    {
        if (!initialized || !map || !active || paused || Time.unscaledTime < nextFeedback) return;
        var clip = success ? map.success : map.interaction;
        if (!clip) return;
        nextFeedback = Time.unscaledTime + 0.15f;
        feedback.Stop(); feedback.clip = clip;
        feedback.volume = Mathf.Clamp(map.feedbackVolume, 0, 0.5f) * (voice.isPlaying ? 0.5f : 1);
        feedback.Play();
    }
    void PlayWaterEntry()
    {
        SessionTelemetry.Report("VisitorEnteredWater", this, "Head crossed actual water surface");
        var clip = map.waterEntry ? map.waterEntry : Resources.Load<AudioClip>("ExperienceAudio/WaterEntrySoft");
        if (!clip) return;
        splash.Stop(); splash.clip = clip;
        splash.volume = Mathf.Clamp01(map.waterEntryVolume);
        splash.Play();
    }
    void PlayWaterExit()
    {
        splash.Stop();
        SessionTelemetry.Report("VisitorExitedWater", this, "Head rose above actual water surface");
        splash.clip = map.waterExit ? map.waterExit : Resources.Load<AudioClip>("ExperienceAudio/WaterExitSplash");
        splash.volume = Mathf.Clamp01(map.waterExitVolume);
        if (splash.clip) splash.Play();
    }
    public void StopAll()
    {
        if (!initialized) return;
        foreach (var source in new[] { voice, museum, harbour, underwater, feedback, splash }) if (source) { source.Stop(); source.volume = 0; source.clip = null; }
        pending.Clear(); scheduled.Clear(); active = paused = hadVoice = insideScheduled = submerged = false;
        arrivalGate.Clear();
        regionHistory.Clear(); nextReplayTime = 0;
        headInWater = false;
        currentWater = null; suppressWaterTransition = true; waterBlendTransition = false;
        envelope = blend = voiceEnvelope = 0; duck = 1; nextFeedback = 0; Status = "Silent";
        harbourBlend = 0;
        museumFilter.cutoffFrequency = 12000;
        harbourFilter.cutoffFrequency = 2200;
    }
    void OnDisable() { StopAll(); session = null; }
}
