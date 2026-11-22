using UnityEngine;

/// <summary>Desktop staff controls only. Visitors progress through exhibit interaction events.</summary>
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4SessionUI")]
public sealed class StaffSessionUI : MonoBehaviour
{
    public StageCoordinator coordinator;
    bool expanded, confirmBack, confirmSkip;
    int backRevision, skipRevision;
    static readonly string[] modeLabels = { "展览 / Exhibition", "小组测试 / Team test" };
    string commandMessage;
    int messageRevision;
    Vector2 panelScroll;

    void RunCommand(string action)
    {
        var control = coordinator.Controller;
        var reply = control.Telemetry.ExecuteLocal(action);
        messageRevision = reply.revision;
        commandMessage = null;
        if (!reply.accepted) commandMessage = action + " failed: " + reply.message;
        else if (action == "Back") commandMessage = "Returned to " + control.Session.Stage + " (restarted)";
    }

    void OnGUI()
    {
        if (!coordinator) return;
        var control = coordinator.Controller;
        var session = control.Session;
        if (GUI.Button(new Rect(15, 15, 190, 30), expanded ? "收起工作人员控制" : "工作人员控制 / Staff")) expanded = !expanded;
        if (!expanded) { confirmBack = confirmSkip = false; return; }
        GUILayout.BeginArea(new Rect(15, 50, Mathf.Min(440, Screen.width - 30), Mathf.Max(60, Screen.height - 60)), GUI.skin.box);
        panelScroll = GUILayout.BeginScrollView(panelScroll);
        GUILayout.Label("Experience - Staff session controls");
        GUILayout.Label(session.Status + " | " + coordinator.Title);
        GUILayout.Label("Session: " + session.SessionId);
        var telemetry = control.Telemetry;
        var monitor = coordinator.StaffMonitor;
        if (monitor)
        {
            var urls = monitor.Urls;
            if (urls.Length == 0) GUILayout.Label(monitor.AddressText);
            else
            {
                GUILayout.Label("iPad 工作人员控制 / Staff web · 点击地址复制控制链接");
                foreach (var url in urls)
                    if (GUILayout.Button(url))
                        GUIUtility.systemCopyBuffer = monitor.ControlLink(url);
            }
        }
        GUI.enabled = telemetry.CanExecute("Start");
        var mode = (SessionControlMode)GUILayout.Toolbar((int)telemetry.ControlMode, modeLabels);
        if (mode != telemetry.ControlMode) telemetry.SetStaffControlMode(mode);
        GUI.enabled = true;
        var gaze = coordinator.GetComponent<GazeInput>();
        if (gaze)
        {
            GUI.enabled = telemetry.CanExecute("Start");
            bool accessible = GUILayout.Toggle(gaze.Accessible, "Accessible Mode / Head gaze (controllers retained)");
            if (accessible != gaze.Accessible) gaze.SetAccessible(accessible);
            GUI.enabled = telemetry.ControlsReady;
            GUILayout.Label("Gaze dwell: " + gaze.dwellSeconds.ToString("0.0") + " s");
            gaze.dwellSeconds = GUILayout.HorizontalSlider(gaze.dwellSeconds,.5f,5);
            GUI.enabled = true;
        }
        GUILayout.Label(coordinator.IntegrationStatus);
        if (coordinator.Audio) GUILayout.Label("Audio: " + coordinator.Audio.Soundscape + " | " + coordinator.Audio.Status);
        if (control.Telemetry.Fault.Length > 0) GUILayout.Label("Control fault (Reset required): " + control.Telemetry.Fault);
        if (control.Telemetry.LogError.Length > 0) GUILayout.Label("Log unavailable: " + control.Telemetry.LogError);
        if (session.CleanupTotal > 0) GUILayout.Label("Cleanup: " + session.CleanupCollected + " / " + session.CleanupTotal + (session.CleanupCompleted ? " - Complete; return to land platform" : ""));
        GUI.enabled = telemetry.CanExecute("Start");
        if (GUILayout.Button("Start", GUILayout.Height(32))) RunCommand("Start");
        GUI.enabled = telemetry.CanExecute("Pause");
        if (GUILayout.Button("Pause", GUILayout.Height(32))) RunCommand("Pause");
        GUI.enabled = telemetry.CanExecute("Resume");
        if (GUILayout.Button("Resume", GUILayout.Height(32))) RunCommand("Resume");
        GUI.enabled = telemetry.CanExecute("Back");
        if (GUILayout.Button("Back to previous task", GUILayout.Height(32)))
        { confirmBack = true; confirmSkip = false; backRevision = session.Revision; }
        if (confirmBack && (!GUI.enabled || backRevision != session.Revision)) confirmBack = false;
        if (confirmBack)
        {
            GUILayout.Label("Return to " + session.PreviousStage + "? This task and all later progress will be reset.");
            if (GUILayout.Button("Confirm Back"))
            {
                RunCommand("Back");
                confirmBack = false;
            }
            if (GUILayout.Button("Cancel")) confirmBack = false;
        }
        GUI.enabled = telemetry.CanExecute("Skip");
        if (GUILayout.Button("Skip current stage (records Skipped)", GUILayout.Height(32)))
        { confirmSkip = true; confirmBack = false; skipRevision = session.Revision; }
        if (confirmSkip && (!GUI.enabled || skipRevision != session.Revision)) confirmSkip = false;
        if (confirmSkip)
        {
            GUILayout.Label("Skip " + session.Stage + "? It will be recorded as Skipped, not Completed."
                + (session.Stage == ExperienceStage.SeabedCleanup ? " You must still return to the land platform." : " The experience will advance to the next stage."));
            if (GUILayout.Button("Confirm Skip"))
            { RunCommand("Skip"); confirmSkip = false; }
            if (GUILayout.Button("Cancel")) confirmSkip = false;
        }
        GUI.enabled = telemetry.CanExecute("Reset");
        if (GUILayout.Button("End / Reset to waiting", GUILayout.Height(32))) RunCommand("Reset");
        GUI.enabled = true;
        if (messageRevision == session.Revision && !string.IsNullOrEmpty(commandMessage))
            GUILayout.Label(commandMessage);
        var reset=coordinator.GlobalReset;
        if(reset)
        {
            string resetStatus = "Ready";
            if (reset.IsResetting) resetStatus = "In progress";
            else if (!reset.LastSucceeded) resetStatus = "FAILED — retry required";
            else if (telemetry.Fault.Length > 0) resetStatus = "Required";
            GUILayout.Label("Reset: "+resetStatus
                +" | "+reset.LastDurationSeconds.ToString("F2")+"s | successful resets: "+reset.CompletedResets);
            if(!reset.LastSucceeded) GUILayout.Label(reset.LastError);
        }
        if (telemetry.CanExecute("Start"))
            GUILayout.Label("交接 / Handover: 确认 Waiting → 协助摘下并清洁头显与手柄 →\n为下一位调整佩戴和模式 → Start。两分钟目标需现场计时验证。");
        foreach (ExperienceStage stage in System.Enum.GetValues(typeof(ExperienceStage)))
            if (stage != ExperienceStage.Inside) GUILayout.Label(stage + ": " + session.Result(stage));
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
