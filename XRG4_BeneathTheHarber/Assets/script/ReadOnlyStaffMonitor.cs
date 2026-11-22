using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

/// <summary>Publishes the current visit and routes paired staff requests to the Unity thread.</summary>
[DisallowMultipleComponent]
public sealed class ReadOnlyStaffMonitor : MonoBehaviour
{
    [Serializable]
    sealed class View
    {
        public MonitorSnapshot snapshot;
        public ActivityRecord[] events;
    }

    // Only these snapshot fields are exposed over the LAN.
#pragma warning disable 0649 // JsonUtility populates this wire projection.
    [Serializable]
    sealed class MonitorSnapshot
    {
        public string runtimeId, sessionId, utc, sessionStartedUtc, status, stage;
        public string observedRegion, integrationStatus, routeDestination, locationEvidence;
        public int revision, cleanupCollected, cleanupTotal, partsObserved, partsRefitted, partsTotal;
        public long eventSequence;
        public bool cleanupCompleted, firstPlatformReturnCompleted, ropeTravelling, accessibleMode;
        public bool ready, resetInProgress, lastResetSucceeded, visitorPresent;
        public StageSnapshot[] stages;
        public string[] availableCommands;
    }

#pragma warning restore 0649

    static readonly HashSet<string> visibleKinds = new HashSet<string>
    {
        "TutorialTargetSelected", "TutorialGrabbed", "TutorialPracticeSucceeded", "TutorialCompleted",
        "HelmetGrabbed", "HelmetReleased", "HelmetGrabCancelled", "HelmetFitMissed", "HelmetFitted",
        "PartDetached", "PartInspected", "PartRestored", "LoosePartRecovered", "PartReset",
        "InformationViewed", "DiverBehaviourChanged", "RopeTravelStarted", "RopeTravelCompleted",
        "RopeTravelCancelled", "LitterPickedUp", "LitterCollected", "VisitorEnteredWater", "VisitorExitedWater",
        "VisitorEndingChoice", "RouteArrived", "NarrationStarted", "AccessibilityModeChanged",
        "VisitorAction", "CommandResult", "RouteStarted", "RegionFirstNarration", "NarrationReplayRequested",
        "TaskProgress", "TaskCompletionReported"
    };
    const int RecentLimit = 30;
    public string AddressText { get; private set; }
    public string[] Urls => server != null ? server.Urls : Array.Empty<string>();
    StaffMonitorServer server;
    SessionTelemetry telemetry;
    StaffWebCommandQueue commands;
    string controlKey;

    // Only the local staff panel copies this link. The logged and published URLs omit the key.
    public string ControlLink(string baseUrl)
    {
        if (controlKey == null || Array.IndexOf(Urls, baseUrl) < 0) return null;
        var visit = GetComponent<SessionController>().Session;
        return baseUrl + "?runtime=" + telemetry.RuntimeId + "&visit=" + visit.SessionId +
            "&rev=" + visit.Revision + "#key=" + controlKey;
    }
    bool initialized;

    void OnEnable() { if (initialized) Open(); }
    public void Open()
    {
        if (server != null) return;
        var owner = GetComponent<StageCoordinator>();
        if (!owner || owner != StageCoordinator.Current || !owner.IsReady) return;
        initialized = true;
        telemetry = owner.Controller.Telemetry;
        var page = Resources.Load<TextAsset>("StaffMonitor/index");
        if (!page) { ShowError("Missing Resources/StaffMonitor/index."); return; }
        server = new StaffMonitorServer();
        var secret = new byte[32];
        using (var random = RandomNumberGenerator.Create()) random.GetBytes(secret);
        controlKey = BitConverter.ToString(secret).Replace("-", "").ToLowerInvariant();
        commands = new StaffWebCommandQueue();
        if (!server.TryStart(owner.staffMonitorPort, page.text, controlKey, commands.Submit, out string error))
        {
            commands.Close(); commands = null; controlKey = null;
            server.Dispose(); server = null;
            ShowError(error); return;
        }
        var links = Array.ConvertAll(server.Urls, url => url + "?runtime=" + telemetry.RuntimeId);
        AddressText = string.Join("\n", links);
        telemetry.SnapshotJson += Publish;
        Publish(telemetry.GetSnapshotJson());
        Debug.Log("Staff monitor (view links)\n" + AddressText, this);
    }

    void Update()
    {
        commands?.Drain(ExecuteStaffCommand);
    }

    string ExecuteStaffCommand(string json)
    {
        var reply = telemetry.ExecuteStaffWeb(json);
        if (reply.accepted) Publish(telemetry.GetSnapshotJson());
        // Keep internal exception messages in the local audit log.
        if (reply.code == "ExecutionFailed")
            return JsonUtility.ToJson(new CommandReply { accepted = false, code = reply.code,
                commandId = reply.commandId, runtimeId = reply.runtimeId, sessionId = reply.sessionId,
                revision = reply.revision, message = "Check the Unity staff panel." });
        return JsonUtility.ToJson(reply);
    }

    void ShowError(string error)
    {
        AddressText = "网页未启动 / Offline: " + error;
        Debug.LogWarning(AddressText, this);
    }

    void Publish(string json)
    {
        var snapshot = JsonUtility.FromJson<MonitorSnapshot>(json);
        snapshot.availableCommands = Array.FindAll(snapshot.availableCommands ?? Array.Empty<string>(),
            CommandDispatcher.IsWebAction);
        var events = new List<ActivityRecord>(RecentLimit);
        if (snapshot.sessionId != Guid.Empty.ToString() && snapshot.status != "Idle")
        {
            var history = telemetry.EventsAfter(0).events;
            for (int i = history.Length - 1; i >= 0 && events.Count < RecentLimit; i--)
            {
                var record = history[i];
                if (record.runtimeId != snapshot.runtimeId || record.sessionId != snapshot.sessionId ||
                    record.sequence > snapshot.eventSequence || !visibleKinds.Contains(record.kind)) continue;
                if (record.kind == "CommandResult")
                {
                    var reply = JsonUtility.FromJson<CommandReply>(record.detail);
                    // A rejected command from an older visitor can be logged during the next visit.
                    if (reply == null || !reply.accepted || reply.sessionId != snapshot.sessionId) continue;
                    events.Add(new ActivityRecord { sequence = record.sequence, revision = record.revision,
                        runtimeId = record.runtimeId, sessionId = record.sessionId, utc = record.utc,
                        stage = record.stage, kind = record.kind, subject = record.subject, detail = reply.code, source = record.source });
                }
                else events.Add(record);
            }
            events.Reverse();
        }
        server.Publish(JsonUtility.ToJson(new View { snapshot = snapshot, events = events.ToArray() }));
    }

    void OnDisable()
    {
        if (telemetry) telemetry.SnapshotJson -= Publish;
        commands?.Close(); commands = null;
        controlKey = null;
        if (server != null) server.Dispose();
        server = null;
        AddressText = null;
    }
}
