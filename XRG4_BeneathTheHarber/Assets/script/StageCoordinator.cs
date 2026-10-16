using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(SessionController))]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4StageCoordinator")]
public sealed class StageCoordinator : MonoBehaviour
{
    [Serializable] public sealed class StageContent
    {
        public string title;
        public AudioClip narration;
        [Tooltip("Optional standing-space volume used only to dismiss route guidance; arrival never completes a task.")]
        public Collider arrivalArea;
        public GameObject[] objects = new GameObject[0];
        [Tooltip("Components implementing IStageActivity, including M3 endpoints.")]
        public MonoBehaviour[] activities = new MonoBehaviour[0];
    }
    public StageContent[] stages = {
        new StageContent { title = "Briefing" },
        new StageContent { title = "The Case" },
        new StageContent { title = "Scale-up" },
        new StageContent { title = "Inside (included in ACT1)" },
        new StageContent { title = "Dress the Diver" },
        new StageContent { title = "Descent" },
        new StageContent { title = "海底垃圾回收 / Seabed cleanup" },
        new StageContent { title = "Experience complete — choose what next" },
        new StageContent { title = "Free exploration" }
    };
    public PlayerQuickTravel navigation;
    [Header("Exploration space / 区域自由活动空间")]
    [Tooltip("Extra horizontal space around arrival areas where route guidance stays hidden. Does not trigger narration.")]
    [Min(0)] public float explorationAreaPadding = 2f;
    [Tooltip("Exploration radius when a region has no Arrival Area. Narration keeps its original smaller radius.")]
    [Min(3)] public float explorationFallbackRadius = 5f;
    public static StageCoordinator Current { get; private set; }
    public SessionController Controller { get; private set; }
    public string IntegrationStatus { get; private set; }
    public string Title => Controller.Session.Status == SessionStatus.Idle ? "Waiting for staff"
        : Controller.Session.Stage == ExperienceStage.SeabedCleanup && Controller.Session.CleanupResolved
            ? "上浮返回陆地平台 / Return to land platform" : stages[(int)Controller.Session.Stage].title;
    public AudioDirector Audio { get; private set; }
    public GlobalReset GlobalReset { get; private set; }
    RegionGuide routeGuide;
    ExperienceStage? entered;
    bool ready;
    public bool IsReady => ready && enabled;
    Guid descentSession;
    bool descentFinished;
    struct VisitorInteraction { public Guid session; public ExperienceStage stage; public VisitorAction action; }
    VisitorInteraction? pendingInteraction;
    Guid pendingDiverSession;
    DiverNpcInteraction pendingDiver;
    DiveRopeStation rope;
    SeabedCleanup cleanup;
    bool cleanupReturnGuided;
    HelmetDetailInspection[] inspections;
    DivingHelmetWearable[] wearables;
    DiverNpcInteraction[] divers;
    ExhibitVideoScreen[] videos;
    readonly Dictionary<XRBaseInteractable, bool> grabs = new Dictionary<XRBaseInteractable, bool>();
    struct BodyState { public bool kinematic; public Vector3 velocity, angularVelocity; }
    readonly Dictionary<Rigidbody, BodyState> bodies = new Dictionary<Rigidbody, BodyState>();
    readonly List<Behaviour> pausedLocomotion = new List<Behaviour>();
    readonly List<IStageActivity> activeActivities = new List<IStageActivity>();
    readonly List<GameObject> escortObjects = new List<GameObject>();

    void Awake()
    {
        if (Current && Current != this) { enabled = false; return; }
        Current = this; Controller = GetComponent<SessionController>();
        GlobalReset = GetComponent<GlobalReset>();
        if (!GlobalReset) GlobalReset = gameObject.AddComponent<GlobalReset>();
    }
    void Start()
    {
        // Upgrade existing seven-stage scenes without changing their authored references.
        if (stages != null && stages.Length == 7)
        {
            Array.Resize(ref stages, 9);
            stages[7] = new StageContent { title = "Experience complete — choose what next" };
            stages[8] = new StageContent { title = "Free exploration" };
        }
        if (stages == null || stages.Length != 9 || Array.Exists(stages, s => s == null))
        { Debug.LogError("Experience requires nine configured stages (legacy seven-stage scenes upgrade automatically).", this); enabled = false; return; }
        // Preserve serialized slot 6 while replacing the obsolete proposal label.
        stages[(int)ExperienceStage.SeabedCleanup].title = "海底垃圾回收 / Seabed cleanup";
        Audio = GetComponent<AudioDirector>();
        if (!Audio) Audio = gameObject.AddComponent<AudioDirector>();
        routeGuide = GetComponent<RegionGuide>();
        if (!routeGuide) routeGuide = gameObject.AddComponent<RegionGuide>();
        routeGuide.ArrivalConfirmed += Audio.ConfirmArrival;
        if (!navigation) navigation = FindObjectOfType<PlayerQuickTravel>();
        inspections = FindObjectsOfType<HelmetDetailInspection>();
        wearables = FindObjectsOfType<DivingHelmetWearable>();
        divers = FindObjectsOfType<DiverNpcInteraction>();
        foreach (var diver in divers) diver.EscortStarted += OnEscortStarted;
        videos = FindObjectsOfType<ExhibitVideoScreen>();
        rope = FindObjectOfType<DiveRopeStation>();
        cleanup = FindObjectOfType<SeabedCleanup>(true);
        if (rope) rope.DescentCompleted += OnDescentCompleted;
        foreach (var stage in stages) foreach (var item in stage.objects) if (item) item.SetActive(false);
        Controller.Session.Changed += Synchronize;
        ready = true; Synchronize();
        var ui = gameObject.AddComponent<StaffSessionUI>(); ui.coordinator = this;
        gameObject.AddComponent<ExperienceEndingUI>();
        gameObject.AddComponent<NarrationRegionPanels>();
        if (!GetComponent<GazeInput>()) gameObject.AddComponent<GazeInput>();
        if (!GetComponent<AccessibilityUI>()) gameObject.AddComponent<AccessibilityUI>();
    }
    public static bool Allows(ExperienceStage stage)
    {
        return !Current || (Current.Controller.Session.Status == SessionStatus.Running &&
            (Current.Controller.Session.Stage == stage || Current.Controller.Session.Stage == ExperienceStage.FreeExploration));
    }
    public static bool AllowsHelmet(DivingHelmetWearable helmet)
    {
        return helmet.GetComponent<DiverNpcInteraction>() ? Allows(ExperienceStage.DressTheDiver)
            : Allows(ExperienceStage.TheCase);
    }
    public void QueueInteraction(Guid session, ExperienceStage stage, VisitorAction action)
    {
        var state = Controller.Session;
        if (state.Status != SessionStatus.Running || state.SessionId != session || state.Stage != stage) return;
        if (!pendingInteraction.HasValue) pendingInteraction = new VisitorInteraction { session = session, stage = stage, action = action };
        SessionTelemetry.Report("VisitorAction", this, action.ToString());
    }
    void OnDescentCompleted() { descentFinished = true; }
    void OnEscortStarted(DiverNpcInteraction diver, Guid session)
    {
        // An explicitly configured M3 activity keeps ownership of its own success criteria.
        var state = Controller.Session;
        if (activeActivities.Count != 0 || !diver.HasFittedHelmet || !Allows(ExperienceStage.DressTheDiver) || session != state.SessionId) return;
        pendingDiver = diver; pendingDiverSession = session;
    }
    void Update()
    {
        if (!ready) return;
        // Both completed and skipped cleanup must physically return to the supported dock.
        // Breaking the water surface is insufficient, and travel must finish before ending.
        var state = Controller.Session;
        if (state.Status == SessionStatus.Running && state.Stage == ExperienceStage.SeabedCleanup
            && state.CleanupResolved && rope && rope.IsVisitorOnSurfacePlatform)
            state.ReportPlatformReturn(state.SessionId);
        if (pendingDiver && Controller.Session.Status == SessionStatus.Running)
        {
            var diver = pendingDiver; var session = pendingDiverSession;
            pendingDiver = null; pendingDiverSession = Guid.Empty;
            if (diver.HasFittedHelmet) Controller.Session.CompleteTask(session, ExperienceStage.DressTheDiver);
        }
        if (pendingInteraction.HasValue && Controller.Session.Status == SessionStatus.Running)
        {
            var interaction = pendingInteraction.Value; pendingInteraction = null;
            Controller.Session.ReportInteraction(interaction.session, interaction.stage, interaction.action);
        }
        // Flush outside event dispatch; an immediate activity callback must not re-enter a transition.
        if (descentFinished && Controller.Session.CompleteTask(descentSession, ExperienceStage.Descent)) descentFinished = false;
        foreach (var inspection in inspections) if (inspection && inspection.helmet) Gate(inspection.helmet, Allows(ExperienceStage.ScaleUp));
        foreach (var wearable in wearables) if (wearable && wearable.helmet) Gate(wearable.helmet, AllowsHelmet(wearable));
        foreach (var inspection in inspections) if (inspection) inspection.SetSessionInteraction(Allows(ExperienceStage.ScaleUp));
        if (cleanup) Gate(cleanup.transform, Allows(ExperienceStage.SeabedCleanup)
            && (state.Stage == ExperienceStage.FreeExploration || !state.CleanupResolved));
    }
    void Gate(Transform root, bool allowed)
    {
        foreach (var grab in root.GetComponentsInChildren<XRBaseInteractable>(true))
        {
            if (!allowed && grab.enabled) { if (!grabs.ContainsKey(grab)) grabs.Add(grab, true); grab.enabled = false; }
            else if (allowed && grabs.Remove(grab)) grab.enabled = true;
        }
        foreach (var body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            if (!allowed && !bodies.ContainsKey(body))
            {
                bodies.Add(body, new BodyState { kinematic = body.isKinematic, velocity = body.velocity, angularVelocity = body.angularVelocity });
                body.isKinematic = true;
            }
            else if (allowed && bodies.TryGetValue(body, out var original)) { RestoreBody(body, original); bodies.Remove(body); }
        }
    }
    void RestoreGates()
    {
        foreach (var pair in grabs) if (pair.Key) pair.Key.enabled = pair.Value;
        grabs.Clear();
        foreach (var pair in bodies) if (pair.Key) RestoreBody(pair.Key, pair.Value);
        bodies.Clear();
    }
    static void RestoreBody(Rigidbody body, BodyState state)
    {
        body.isKinematic = state.kinematic;
        if (!state.kinematic) { body.velocity = state.velocity; body.angularVelocity = state.angularVelocity; }
    }
    void SetLocomotionPaused(bool paused)
    {
        if (!paused)
        {
            foreach (var component in pausedLocomotion) if (component) component.enabled = true;
            pausedLocomotion.Clear(); return;
        }
        if (!Camera.main) return;
        var origin = Camera.main.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
        if (!origin) return;
        foreach (var provider in origin.GetComponentsInChildren<LocomotionProvider>())
            if (provider.enabled) { pausedLocomotion.Add(provider); provider.enabled = false; }
        foreach (var component in origin.GetComponentsInChildren<XRBodyTransformer>())
            if (component.enabled) { pausedLocomotion.Add(component); component.enabled = false; }
    }
    void Synchronize()
    {
        if (GlobalReset && GlobalReset.IsResetting) return;
        var session = Controller.Session;
        bool hasStage = session.Status == SessionStatus.Running || session.Status == SessionStatus.Paused;
        if (!hasStage || entered != session.Stage)
        {
            bool keepFittedHelmet = entered == ExperienceStage.TheCase && hasStage && session.Stage == ExperienceStage.ScaleUp
                && session.Result(ExperienceStage.TheCase) == StageResult.Completed;
            bool keepDiverEscort = hasStage && entered.HasValue &&
                SessionState.PreserveDiverEscort(entered.Value, session.Stage, session.Result(entered.Value));
            LeaveStage(keepFittedHelmet, keepDiverEscort);
            if (hasStage) EnterStage(session.Stage);
            else if (session.Status == SessionStatus.Idle)
            {
                foreach (var content in stages) foreach (var component in content.activities)
                    if (component is IStageActivity activity) activity.ResetActivity();
                Navigate(0);
                foreach (var cleanup in FindObjectsOfType<SeabedCleanup>(true)) cleanup.ResetForSession();
            }
        }
        bool paused = session.Status == SessionStatus.Paused;
        foreach (var activity in activeActivities) activity.SetStagePaused(paused);
        Audio.Synchronize(session, hasStage ? stages[(int)session.Stage].narration : null);
        if (session.Stage == ExperienceStage.SeabedCleanup)
        {
            IntegrationStatus = cleanup ? "Collected " + session.CleanupCollected + " / " + cleanup.TotalCount
                + (session.CleanupResolved ? "; ascend and return to the land platform." : "; release litter inside the basket.")
                : "Cleanup activity missing. Staff may Skip.";
            if (session.CleanupResolved && !cleanupReturnGuided)
            { cleanupReturnGuided = true; GuideToStage(ExperienceStage.SeabedCleanup); }
        }
        SetLocomotionPaused(session.Status != SessionStatus.Running || session.Stage == ExperienceStage.ExperienceComplete);
        if (session.Status == SessionStatus.Completed || session.Stage == ExperienceStage.ExperienceComplete)
            foreach (var interactable in FindObjectsOfType<XRBaseInteractable>())
                if (interactable.enabled)
                { if (!grabs.ContainsKey(interactable)) grabs.Add(interactable, true); interactable.enabled = false; }
        foreach (var video in videos) if (video) video.SetSessionPaused(session.Status != SessionStatus.Running);
        Debug.Log($"Experience session={session.SessionId} state={session.Status} stage={session.Stage} revision={session.Revision}", this);
        foreach (ExperienceStage stage in Enum.GetValues(typeof(ExperienceStage)))
            Debug.Log($"Experience result session={session.SessionId} stage={stage} result={session.Result(stage)}", this);
    }
    public void PrepareGlobalReset()
    {
        // Restore pre-pause enabled states, then freeze again with a clean baseline.
        RestoreGates(); SetLocomotionPaused(false); SetLocomotionPaused(true);
        activeActivities.Clear();
        entered=null; pendingInteraction=null; pendingDiver=null; pendingDiverSession=Guid.Empty;
        descentFinished=false; descentSession=Guid.Empty; cleanupReturnGuided=false;
        IntegrationStatus="";
    }
    public void FinishGlobalReset()
    {
        foreach(var content in stages) foreach(var item in content.objects) if(item) item.SetActive(false);
        foreach(var item in escortObjects) if(item) item.SetActive(false);
        escortObjects.Clear();
        foreach(var interactable in Resources.FindObjectsOfTypeAll<XRBaseInteractable>())
            if(interactable && interactable.gameObject.scene.IsValid() && interactable.gameObject.scene.isLoaded && interactable.enabled)
            { if(!grabs.ContainsKey(interactable)) grabs.Add(interactable,true); interactable.enabled=false; }
        SetLocomotionPaused(true);
        IntegrationStatus="Waiting — reset complete";
    }
    public void ReportResetResult(bool succeeded)
    { IntegrationStatus=succeeded ? "Waiting — ready for the next visitor" : "Reset incomplete — retry Reset; Start is blocked"; }
    void EnterStage(ExperienceStage stage)
    {
        entered = stage;
        cleanupReturnGuided = false;
        var content = stages[(int)stage];
        if (stage == ExperienceStage.FreeExploration)
            foreach (var item in stages) foreach (var obj in item.objects) if (obj) obj.SetActive(true);
        foreach (var item in content.objects) if (item) item.SetActive(true);
        foreach (var component in content.activities)
            if (component is IStageActivity activity)
            { activeActivities.Add(activity); activity.BeginStage(Controller.Session.SessionId, stage); }
        IntegrationStatus = SessionState.IsTask(stage) && stage != ExperienceStage.Descent && activeActivities.Count == 0
            ? "Awaiting M3 integration: no task provider assigned. Staff may Skip; no success will be inferred." : "";
        if (stage == ExperienceStage.DressTheDiver && activeActivities.Count == 0 && Array.Exists(divers, diver => diver))
            IntegrationStatus = "Existing diver activity: fit the helmet, then follow the diver to the descent platform.";
        if (stage == ExperienceStage.Descent)
        {
            descentSession = Controller.Session.SessionId; descentFinished = false;
            if (!rope) IntegrationStatus = "Descent station missing. Staff may Skip.";
        }
        GuideToStage(stage);
    }
    void LeaveStage(bool keepFittedHelmet = false, bool keepDiverEscort = false)
    {
        if (routeGuide) routeGuide.Clear();
        foreach (var video in videos) if (video) video.PausePreservingProgress();
        // Disable grabs first: cancellation releases held parts before restoring their transforms.
        foreach (var inspection in inspections) if (inspection) inspection.PrepareSessionReset();
        foreach (var wearable in wearables) if (wearable && !KeepFitted(wearable, keepFittedHelmet, keepDiverEscort)) wearable.PrepareSessionReset();
        RestoreGates();
        foreach (var activity in activeActivities) activity.EndStage();
        activeActivities.Clear();
        foreach (var inspection in inspections) if (inspection) inspection.ResetExhibit();
        foreach (var wearable in wearables) if (wearable && !KeepFitted(wearable, keepFittedHelmet, keepDiverEscort)) wearable.ReturnToStand();
        foreach (var diver in divers) if (diver && !(keepDiverEscort && diver.HasFittedHelmet)) diver.ResetActivity();
        if (rope) rope.CancelSessionTravel();
        if (entered == ExperienceStage.FreeExploration)
            foreach (var content in stages) foreach (var item in content.objects) if (item) item.SetActive(false);
        if (!keepDiverEscort)
        {
            foreach (var item in escortObjects) if (item) item.SetActive(false);
            escortObjects.Clear();
        }
        if (entered.HasValue) foreach (var item in stages[(int)entered.Value].objects)
        {
            if (!item) continue;
            bool carriesDiver = keepDiverEscort && Array.Exists(divers, diver => diver && diver.HasFittedHelmet &&
                (diver.transform.IsChildOf(item.transform) || (diver.helmet && diver.helmet.IsChildOf(item.transform))));
            if (carriesDiver)
            { if (!escortObjects.Contains(item)) escortObjects.Add(item); }
            else item.SetActive(false);
        }
        entered = null; descentFinished = false; pendingInteraction = null; pendingDiver = null; pendingDiverSession = Guid.Empty; IntegrationStatus = "";
    }
    static bool KeepFitted(DivingHelmetWearable wearable, bool keepFittedHelmet, bool keepDiverEscort)
    {
        return wearable.IsWorn && (wearable.GetComponent<DiverNpcInteraction>() ? keepDiverEscort : keepFittedHelmet);
    }
    void Navigate(int destination)
    {
        if (navigation) navigation.SessionTravelTo(destination);
    }
    public bool IsVisitorInNarrationRegion(ExperienceStage region)
    {
        if (!ready || !Camera.main || region == ExperienceStage.Inside || region >= ExperienceStage.ExperienceComplete) return false;
        var area = stages[(int)region].arrivalArea;
        var head = Camera.main.transform;
        var origin = head.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
        var feet = head.position; feet.y = origin ? origin.transform.position.y : feet.y - 1.6f;
        if (region == ExperienceStage.SeabedCleanup)
            return cleanup && cleanup.CollectionTarget && Vector3.Distance(feet, cleanup.CollectionTarget.position) <= 3;
        if (area) return area.enabled && area.gameObject.activeInHierarchy &&
            (area.ClosestPoint(feet + Vector3.up * .25f) - (feet + Vector3.up * .25f)).sqrMagnitude < .0025f;
        if (!navigation) return false;
        Transform[] targets = { navigation.start, navigation.act1, navigation.act2, null, navigation.act3, navigation.act4, navigation.start };
        var target = targets[(int)region]; if (!target) return false;
        var position = region == ExperienceStage.Descent && rope ? rope.SurfaceDock : target.position;
        return Vector2.Distance(new Vector2(feet.x,feet.z),new Vector2(position.x,position.z)) <= 3
            && Mathf.Abs(feet.y - position.y) <= 2;
    }
    public bool NarrationArrivalReady(ExperienceStage region)
    {
        if (!IsVisitorInNarrationRegion(region)) return false;
        bool guidedTarget = Controller.Session.Stage == region &&
            (region == ExperienceStage.TheCase || region == ExperienceStage.ScaleUp || region == ExperienceStage.DressTheDiver);
        return !guidedTarget || (routeGuide && routeGuide.ArrivalFinished);
    }
    // Presence suppresses route UI even in a previously completed or not-yet-active area.
    // It never changes progress or grants arrival at a different destination.
    public bool IsVisitorInAnyExperienceRegion()
    {
        if (!ready || !Camera.main) return false;
        var head = Camera.main.transform;
        var origin = head.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
        var feet = head.position; feet.y = origin ? origin.transform.position.y : feet.y - 1.6f;
        foreach (var region in new[] { ExperienceStage.Briefing, ExperienceStage.TheCase, ExperienceStage.ScaleUp,
            ExperienceStage.DressTheDiver, ExperienceStage.Descent, ExperienceStage.SeabedCleanup })
        {
            if (IsVisitorInNarrationRegion(region)) return true;
            var area = stages[(int)region].arrivalArea;
            if (area)
            {
                if (!area.enabled || !area.gameObject.activeInHierarchy) continue;
                var probe = feet + Vector3.up*.25f;
                var delta = area.ClosestPoint(probe) - probe;
                // Expand only horizontally: the surface platform must not include the seabed.
                if (Mathf.Abs(delta.y) <= .05f && new Vector2(delta.x,delta.z).magnitude <= Mathf.Max(0,explorationAreaPadding)) return true;
                continue;
            }
            Transform target = null;
            if (navigation)
            {
                switch (region)
                {
                    case ExperienceStage.Briefing: target = navigation.start; break;
                    case ExperienceStage.TheCase: target = navigation.act1; break;
                    case ExperienceStage.ScaleUp: target = navigation.act2; break;
                    case ExperienceStage.DressTheDiver: target = navigation.act3; break;
                    case ExperienceStage.Descent: target = navigation.act4; break;
                }
            }
            if (region == ExperienceStage.SeabedCleanup && cleanup) target = cleanup.CollectionTarget;
            if (!target) continue;
            var centre = region == ExperienceStage.Descent && rope ? rope.SurfaceDock : target.position;
            if (Mathf.Abs(feet.y-centre.y) <= 2 &&
                Vector2.Distance(new Vector2(feet.x,feet.z),new Vector2(centre.x,centre.z)) <= Mathf.Max(3,explorationFallbackRadius)) return true;
        }
        // The entire underwater activity space is a region, not just the collection basket.
        if (Camera.main && Audio && Audio.IsVisitorUnderwater) return true;
        return false;
    }
    void GuideToStage(ExperienceStage stage)
    {
        if (stage == ExperienceStage.Inside || stage >= ExperienceStage.ExperienceComplete) return;
        if (!navigation) { IntegrationStatus = "Route guidance unavailable: assign PlayerQuickTravel destinations."; return; }
        if (stage == ExperienceStage.SeabedCleanup && !Controller.Session.CleanupResolved)
        {
            if (cleanup && cleanup.CollectionTarget)
                routeGuide.GuideTo(Controller, cleanup.CollectionTarget, cleanup.CollectionTarget.position, null,
                    "海底垃圾回收 / Collect litter in the basket");
            return;
        }
        Transform[] targets = { navigation.start, navigation.act1, navigation.act2, null, navigation.act3, navigation.act4, navigation.act4 };
        string[] names = { "介绍区域 / Introduction", "头盔展台 / Helmet exhibit", "细节观察区 / Detail inspection", "", "潜水员装备区 / Dress the diver", "下潜平台 / Descent platform", "上浮返回陆地平台 / Return to the land platform" };
        Transform target = targets[(int)stage];
        if (!target) { IntegrationStatus = "Route guidance unavailable: destination is missing."; return; }
        Vector3 arrival = target.position + (stage == ExperienceStage.TheCase || stage == ExperienceStage.ScaleUp ? Vector3.left * 2.8f : Vector3.back * 3f);
        arrival.y = 0;
        if ((stage == ExperienceStage.Descent || stage == ExperienceStage.SeabedCleanup) && rope) arrival = rope.SurfaceDock;
        routeGuide.GuideTo(Controller, target, arrival, stages[(int)stage].arrivalArea, names[(int)stage]);
    }
    void OnDestroy()
    {
        if (routeGuide && Audio) routeGuide.ArrivalConfirmed -= Audio.ConfirmArrival;
        if (divers != null) foreach (var diver in divers) if (diver) diver.EscortStarted -= OnEscortStarted;
        if (Controller) Controller.Session.Changed -= Synchronize;
        if (rope) rope.DescentCompleted -= OnDescentCompleted;
        if (ready) LeaveStage();
        if (Audio) Audio.StopAll();
        SetLocomotionPaused(false);
        if (Current == this) Current = null;
    }
}
