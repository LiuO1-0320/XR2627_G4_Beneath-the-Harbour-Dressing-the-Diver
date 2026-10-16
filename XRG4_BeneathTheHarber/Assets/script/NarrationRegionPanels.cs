using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Add replay controls below existing regional text, without duplicating instructions.</summary>
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4NarrationRegionPanels")]
public sealed class NarrationRegionPanels : MonoBehaviour, IResettable
{
    public int ResetOrder => 100;
    public void PrepareReset() { requestPending=false; }
    public void RestoreReset() { requestPending=false; requestedSession=Guid.Empty; foreach(var entry in entries) if(entry.root) entry.root.SetActive(false); }

    sealed class Entry { public Canvas canvas; public GameObject root; public Button button; public Text label; public ExperienceStage region; public Guid displayedSession; }
    readonly List<Entry> entries = new List<Entry>();
    StageCoordinator coordinator;
    Font font;
    float nextScan;
    bool requestPending;
    Guid requestedSession;
    ExperienceStage requestedRegion;
    void Start()
    {
        coordinator = GetComponent<StageCoordinator>();
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 28);
    }
    void Update()
    {
        if (!coordinator || !coordinator.Audio) return;
        if (requestPending)
        {
            requestPending = false;
            coordinator.Audio.RequestRegionNarration(requestedSession,requestedRegion,true);
        }
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 1;
            entries.RemoveAll(e => !e.canvas || !e.root);
            // Exhibit boards are generated with HideAndDontSave. FindObjectsOfType
            // excludes these even with includeInactive=true, so search loaded objects
            // and explicitly reject assets/prefabs that do not belong to a loaded scene.
            foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (!canvas.gameObject.scene.IsValid() || !canvas.gameObject.scene.isLoaded) continue;
                ExperienceStage region;
                switch (canvas.name)
                {
                    case "Helmet instructions (generated)": region = ExperienceStage.TheCase; break;
                    case "Helmet 2 Detail Controls": region = ExperienceStage.ScaleUp; break;
                    case "Act 3 Instructions (generated)": region = ExperienceStage.DressTheDiver; break;
                    case "Introduction Display (generated)": region = ExperienceStage.Briefing; break;
                    case "Surface descent controls": region = ExperienceStage.Descent; break;
                    case "海底清理说明 / Cleanup instructions": region = ExperienceStage.SeabedCleanup; break;
                    default: continue;
                }
                if (!entries.Exists(e => e.canvas == canvas)) Attach(canvas,region);
            }
        }
        var state = coordinator.Controller.Session;
        foreach (var entry in entries)
        {
            if (!entry.root) continue;
            bool visible = state.Stage == ExperienceStage.FreeExploration && state.Status != SessionStatus.Completed
                && coordinator.IsVisitorInNarrationRegion(entry.region);
            entry.root.SetActive(visible); entry.displayedSession = state.SessionId;
            bool hasClip = coordinator.Audio.HasRegionNarration(entry.region);
            entry.button.interactable = visible && hasClip && state.Status == SessionStatus.Running;
            entry.label.text = hasClip ? "再次播放本区解说\nReplay this area's narration" : "本区解说尚未提供\nNarration not yet available";
        }
    }
    void Attach(Canvas canvas,ExperienceStage region)
    {
        if (!canvas.GetComponent<TrackedDeviceGraphicRaycaster>()) canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        var root = new GameObject("Replay regional narration",typeof(RectTransform),typeof(Image),typeof(AccessibleButton));
        root.transform.SetParent(canvas.transform,false);
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(660,100);
        rect.anchoredPosition = new Vector2(0,-((RectTransform)canvas.transform).rect.height/2 - (region == ExperienceStage.Briefing ? 200 : 60));
        var image = root.GetComponent<Image>(); image.color = new Color(.08f,.35f,.38f,.98f);
        var button = root.GetComponent<Button>(); button.targetGraphic = image;
        var child = new GameObject("Replay caption",typeof(RectTransform),typeof(Text)); child.transform.SetParent(root.transform,false);
        child.GetComponent<RectTransform>().sizeDelta = new Vector2(640,90);
        var label = child.GetComponent<Text>(); label.font = font; label.fontSize = 28;
        label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.raycastTarget = false;
        var entry = new Entry { canvas=canvas,root=root,button=button,label=label,region=region };
        entries.Add(entry);
        SessionTelemetry.Report("NarrationReplayControlAttached", canvas, region.ToString());
        button.onClick.AddListener(() =>
        {
            if (requestPending) return;
            requestPending = true; requestedSession = entry.displayedSession; requestedRegion = entry.region;
        });
        root.SetActive(false);
    }
    void OnDestroy()
    {
        foreach (var entry in entries) if (entry.root) Destroy(entry.root);
        if (font) Destroy(font);
    }
}
