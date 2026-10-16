using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable] public sealed class StageSnapshot { public string stage, result; }
[Serializable] public sealed class SessionSnapshot
{
    public int protocolVersion = 1, revision;
    public long eventSequence;
    public string runtimeId, sessionId, utc, status, stage, integrationStatus, fault, logError;
    public string routeDestination, locationEvidence;
    public string observedRegion, lastRuntimeError;
    public string sessionStartedUtc;
    public int cleanupCollected, cleanupTotal;
    public bool cleanupCompleted;
    public string soundscape, audioStatus, narrationClip;
    public float narrationPositionSeconds;
    public int fittedHelmets, heldObjects;
    public bool ropeTravelling;
    public bool travelling, ready;
    public bool firstPlatformReturnCompleted;
    public bool accessibleMode;
    public float gazeDwellSeconds, gazeProgress;
    public string gazeTarget;
    public bool resetInProgress, lastResetSucceeded;
    public double lastResetDurationSeconds;
    public int completedResets;
    public string resetError;
    public StageSnapshot[] stages;
    public string[] availableCommands;
}
[Serializable] public sealed class ActivityRecord
{
    public int protocolVersion = 1, revision;
    public long sequence;
    public string runtimeId, sessionId, utc, stage, kind, subject, detail;
}
[Serializable] public sealed class EventBatch
{
    public bool gap;
    public long latestSequence;
    public ActivityRecord[] events;
}

/// <summary>Transport-neutral backend. All events and snapshot APIs run on Unity's main thread.
/// Only SubmitJson is safe to call from a transport worker. No network listener is opened.</summary>
[DisallowMultipleComponent]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4SessionTelemetry")]
public sealed class SessionTelemetry : MonoBehaviour
{
    public event Action<string> SnapshotJson;
    public event Action<string> EventJson;
    public event Action<string> ReplyJson;
    public string RuntimeId { get; private set; }
    public string LogDirectory { get; private set; }
    public string LogError { get; private set; } = "";
    public string Fault { get; private set; } = "";
    readonly ConcurrentQueue<string> incoming = new ConcurrentQueue<string>();
    readonly Queue<ActivityRecord> history = new Queue<ActivityRecord>();
    readonly object queueLock = new object();
    SessionController controller;
    CommandDispatcher dispatcher;
    StreamWriter writer;
    long sequence, fileBytes;
    int segment;
    float nextSnapshot;
    string lastSession, lastState;
    string lastRuntimeError = "";
    string sessionStartedUtc = "";
    bool dirty = true;

    void Awake()
    {
        controller = GetComponent<SessionController>();
        RuntimeId = Guid.NewGuid().ToString();
        dispatcher = new CommandDispatcher(controller.Session, RuntimeId, Execute);
        LogDirectory = Path.Combine(Application.persistentDataPath, "SessionLogs");
        lastSession = controller.Session.SessionId.ToString();
        controller.Session.Changed += StateChanged;
        Application.logMessageReceived += OnUnityLog;
        Record("RuntimeStarted", "Unity", Application.version);
    }
    // A false return means no command was queued. The adapter must return Busy/InvalidPayload.
    public bool SubmitJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > 4096) return false;
        lock (queueLock) { if (incoming.Count >= 64) return false; incoming.Enqueue(json); return true; }
    }
    public CommandReply ExecuteLocal(string action)
    {
        var state = controller.Session;
        return Dispatch(new ControlCommand { commandId = Guid.NewGuid().ToString(), runtimeId = RuntimeId,
            sessionId = state.SessionId.ToString(), expectedRevision = state.Revision, action = action });
    }
    CommandReply Dispatch(ControlCommand command)
    {
        Record("CommandRequested", command?.action, command == null ? "null" : JsonUtility.ToJson(command));
        var reply = dispatcher.Dispatch(command);
        Record("CommandResult", command?.action, JsonUtility.ToJson(reply));
        Emit(ReplyJson, JsonUtility.ToJson(reply)); dirty = true; return reply;
    }
    bool Execute(string action)
    {
        var state = controller.Session;
        var coordinator = GetComponent<StageCoordinator>();
        if (!coordinator || !coordinator.IsReady) return false;
        if (coordinator.GlobalReset && coordinator.GlobalReset.IsResetting) return false;
        if (Fault.Length != 0 && action != "Reset") return false;
        try
        {
            switch (action)
            {
                case "Start": return state.Start();
                case "Pause": return state.Pause();
                case "Resume": return state.Resume();
                case "Skip": return state.Skip();
                case "Reset":
                    Record("ResetStarted", "Session", "");
                    if (!coordinator.GlobalReset || !coordinator.GlobalReset.ResetAll()) return false;
                    Fault = ""; Record("ResetCompleted", "Session", "All registered reset participants restored."); return true;
                default: return false;
            }
        }
        catch (Exception error) { Fault = error.Message; Record("ControlFault", action, Fault); throw; }
    }
    void StateChanged()
    {
        var state = controller.Session;
        string current = state.SessionId.ToString();
        if (lastSession != Guid.Empty.ToString() && current != lastSession)
            Record("SessionClosed", lastSession, lastState ?? "", lastSession);
        if (current != lastSession) sessionStartedUtc = current == Guid.Empty.ToString() ? "" : DateTime.UtcNow.ToString("O");
        lastSession = current;
        var results = new List<string>();
        foreach (ExperienceStage stage in Enum.GetValues(typeof(ExperienceStage))) results.Add(stage + "=" + state.Result(stage));
        lastState = state.Status + ";" + string.Join(",", results) + ";cleanup=" + state.CleanupCollected + "/" + state.CleanupTotal;
        Record("SessionStateChanged", state.Stage.ToString(), lastState); dirty = true;
    }
    void Update()
    {
        for (int i = 0; i < 8 && incoming.TryDequeue(out var json); i++)
        {
            ControlCommand command;
            try { command = JsonUtility.FromJson<ControlCommand>(json); }
            catch (Exception) { command = null; }
            Dispatch(command);
        }
    }
    void LateUpdate()
    {
        if (!dirty && Time.unscaledTime < nextSnapshot) return;
        dirty = false; nextSnapshot = Time.unscaledTime + 1;
        Emit(SnapshotJson, GetSnapshotJson());
    }
    public string GetSnapshotJson() => JsonUtility.ToJson(GetSnapshot());
    public SessionSnapshot GetSnapshot()
    {
        var state = controller.Session;
        var coordinator = GetComponent<StageCoordinator>();
        var guide = GetComponent<RegionGuide>();
        var results = new List<StageSnapshot>();
        int fitted = 0, held = 0;
        foreach (var helmet in FindObjectsOfType<DivingHelmetWearable>()) if (helmet.IsWorn) fitted++;
        foreach (var grab in FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>()) if (grab.isSelected) held++;
        string region = "Unknown";
        if (coordinator && Camera.main)
        {
            var head = Camera.main.transform;
            for (int i = 0; i < coordinator.stages.Length; i++)
            {
                var area = coordinator.stages[i]?.arrivalArea;
                if (area && area.enabled && area.gameObject.activeInHierarchy && (area.ClosestPoint(head.position) - head.position).sqrMagnitude < 0.0025f)
                { region = region == "Unknown" ? ((ExperienceStage)i).ToString() : "OverlappingRegions"; }
            }
        }
        foreach (ExperienceStage stage in Enum.GetValues(typeof(ExperienceStage)))
            results.Add(new StageSnapshot { stage = stage.ToString(), result = state.Result(stage).ToString() });
        bool ready = coordinator && coordinator.IsReady && Fault.Length == 0;
        var commands = new List<string>();
        if (coordinator && coordinator.IsReady) commands.Add("Reset");
        if (ready)
        {
            if (state.Status == SessionStatus.Idle) commands.Add("Start");
            if (state.Status == SessionStatus.Running) commands.Add("Pause");
            if (state.Status == SessionStatus.Paused) commands.Add("Resume");
            if ((state.Status == SessionStatus.Running || state.Status == SessionStatus.Paused)
                && !(state.Stage == ExperienceStage.SeabedCleanup && state.CleanupResolved)) commands.Add("Skip");
        }
        return new SessionSnapshot { runtimeId = RuntimeId, sessionId = state.SessionId.ToString(), utc = DateTime.UtcNow.ToString("O"),
            revision = state.Revision, eventSequence = sequence, status = state.Status.ToString(), stage = state.Stage.ToString(),
            stages = results.ToArray(), integrationStatus = coordinator ? coordinator.IntegrationStatus : "Coordinator missing",
            ready = ready, fault = Fault, logError = LogError, availableCommands = commands.ToArray(),
            observedRegion = region, fittedHelmets = fitted, heldObjects = held, ropeTravelling = DiveRopeStation.IsPlayerTravelling,
            lastRuntimeError = lastRuntimeError,
            cleanupCollected = state.CleanupCollected, cleanupTotal = state.CleanupTotal,
            cleanupCompleted = state.CleanupCompleted, sessionStartedUtc = sessionStartedUtc,
            firstPlatformReturnCompleted = state.HasReturnedAfterCleanup,
            accessibleMode = GazeInput.Current && GazeInput.Current.Accessible,
            gazeDwellSeconds = GazeInput.Current ? GazeInput.Current.dwellSeconds : 0,
            gazeProgress = GazeInput.Current ? GazeInput.Current.Progress : 0,
            gazeTarget = GazeInput.Current ? GazeInput.Current.TargetName : "",
            resetInProgress = coordinator && coordinator.GlobalReset && coordinator.GlobalReset.IsResetting,
            lastResetSucceeded = coordinator && coordinator.GlobalReset && coordinator.GlobalReset.LastSucceeded,
            lastResetDurationSeconds = coordinator && coordinator.GlobalReset ? coordinator.GlobalReset.LastDurationSeconds : 0,
            completedResets = coordinator && coordinator.GlobalReset ? coordinator.GlobalReset.CompletedResets : 0,
            resetError = coordinator && coordinator.GlobalReset ? coordinator.GlobalReset.LastError : "",
            soundscape = coordinator && coordinator.Audio ? coordinator.Audio.Soundscape : "Unavailable",
            audioStatus = coordinator && coordinator.Audio ? coordinator.Audio.Status : "Unavailable",
            narrationClip = coordinator && coordinator.Audio ? coordinator.Audio.CurrentNarration : "",
            narrationPositionSeconds = coordinator && coordinator.Audio ? coordinator.Audio.NarrationPosition : 0,
            travelling = guide && guide.IsGuiding, routeDestination = guide ? guide.DestinationName : "",
            locationEvidence = guide ? guide.LocationEvidence : "Unknown" };
    }
    public EventBatch EventsAfter(long after)
    {
        var records = new List<ActivityRecord>();
        foreach (var record in history) if (record.sequence > after) records.Add(record);
        return new EventBatch { gap = after > sequence || (history.Count > 0 && after < history.Peek().sequence - 1),
            latestSequence = sequence, events = records.ToArray() };
    }
    public static void Report(string kind, UnityEngine.Object subject, string detail = "")
    {
        var current = StageCoordinator.Current;
        if (Application.isPlaying && current && current.Controller && current.Controller.Telemetry)
            current.Controller.Telemetry.Record(kind, subject ? subject.name : "", detail);
    }
    public void Record(string kind, string subject, string detail, string capturedSessionId = null)
    {
        var state = controller.Session;
        var record = new ActivityRecord { sequence = ++sequence, runtimeId = RuntimeId, sessionId = capturedSessionId ?? state.SessionId.ToString(),
            utc = DateTime.UtcNow.ToString("O"), revision = state.Revision, stage = state.Stage.ToString(), kind = kind, subject = subject, detail = detail };
        history.Enqueue(record); if (history.Count > 512) history.Dequeue();
        string json = JsonUtility.ToJson(record);
        try
        {
            if (writer == null || fileBytes >= 4 * 1024 * 1024)
            {
                writer?.Dispose(); writer = null;
                Directory.CreateDirectory(LogDirectory);
                writer = new StreamWriter(Path.Combine(LogDirectory, RuntimeId + "-" + segment++ + ".jsonl"), false) { AutoFlush = true };
                fileBytes = 0;
            }
            writer.WriteLine(json); fileBytes += System.Text.Encoding.UTF8.GetByteCount(json) + 2;
            LogError = "";
        }
        catch (Exception error)
        {
            LogError = error.Message;
            try { writer?.Dispose(); } catch (Exception) { }
            writer = null;
        }
        Emit(EventJson, json);
    }
    static void Emit(Action<string> handlers, string json)
    {
        if (handlers == null) return;
        foreach (Action<string> handler in handlers.GetInvocationList())
            try { handler(json); } catch (Exception error) { Debug.LogException(error); }
    }
    void OnUnityLog(string message, string trace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        // Expose the latest error without recursively forwarding log callbacks into event subscribers.
        lastRuntimeError = message.Length > 2048 ? message.Substring(0, 2048) : message;
        dirty = true;
    }
    void OnDestroy()
    {
        controller.Session.Changed -= StateChanged; Application.logMessageReceived -= OnUnityLog;
        try { writer?.Dispose(); } catch (Exception) { }
    }
}
