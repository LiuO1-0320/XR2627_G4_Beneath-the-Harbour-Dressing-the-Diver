using UnityEngine;

/// <summary>Desktop staff controls only. Visitors progress through exhibit interaction events.</summary>
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4SessionUI")]
public sealed class StaffSessionUI : MonoBehaviour
{
    public StageCoordinator coordinator;
    bool expanded, confirmBack, confirmSkip;
    int backRevision, skipRevision;
    string backMessage;
    void OnGUI()
    {
        if (!coordinator) return;
        var control = coordinator.Controller;
        var session = control.Session;
        if (GUI.Button(new Rect(15, 15, 190, 30), expanded ? "收起工作人员控制" : "工作人员控制 / Staff")) expanded = !expanded;
        if (!expanded) { confirmBack = confirmSkip = false; return; }
        GUILayout.BeginArea(new Rect(15, 50, 440, 780), GUI.skin.box);
        GUILayout.Label("Experience - Staff session controls");
        GUILayout.Label(session.Status + " | " + coordinator.Title);
        GUILayout.Label("Session: " + session.SessionId);
        var gaze = coordinator.GetComponent<GazeInput>();
        if (gaze)
        {
            GUI.enabled = session.Status == SessionStatus.Idle;
            bool accessible = GUILayout.Toggle(gaze.Accessible, "Accessible Mode / Head gaze (controllers retained)");
            if (accessible != gaze.Accessible) gaze.SetAccessible(accessible);
            GUI.enabled = true;
            GUILayout.Label("Gaze dwell: " + gaze.dwellSeconds.ToString("0.0") + " s");
            gaze.dwellSeconds = GUILayout.HorizontalSlider(gaze.dwellSeconds,.5f,5);
        }
        GUILayout.Label(coordinator.IntegrationStatus);
        if (coordinator.Audio) GUILayout.Label("Audio: " + coordinator.Audio.Soundscape + " | " + coordinator.Audio.Status);
        if (control.Telemetry.Fault.Length > 0) GUILayout.Label("Control fault (Reset required): " + control.Telemetry.Fault);
        if (control.Telemetry.LogError.Length > 0) GUILayout.Label("Log unavailable: " + control.Telemetry.LogError);
        if (session.CleanupTotal > 0) GUILayout.Label("Cleanup: " + session.CleanupCollected + " / " + session.CleanupTotal + (session.CleanupCompleted ? " - Complete; return to land platform" : ""));
        GUI.enabled = session.Status == SessionStatus.Idle && control.Telemetry.Fault.Length == 0;
        if (GUILayout.Button("Start", GUILayout.Height(32))) control.StartSession();
        GUI.enabled = session.Status == SessionStatus.Running;
        if (GUILayout.Button("Pause", GUILayout.Height(32))) control.PauseSession();
        GUI.enabled = session.Status == SessionStatus.Paused;
        if (GUILayout.Button("Resume", GUILayout.Height(32))) control.ResumeSession();
        GUI.enabled = session.CanGoBack;
        if (GUILayout.Button("Back to previous task", GUILayout.Height(32)))
        { confirmBack = true; confirmSkip = false; backRevision = session.Revision; }
        if (confirmBack && backRevision != session.Revision) confirmBack = false;
        if (confirmBack)
        {
            GUILayout.Label("Return to " + session.PreviousStage + "? This task and all later progress will be reset.");
            if (GUILayout.Button("Confirm Back"))
            {
                var reply = control.BackStage();
                backMessage = reply.accepted ? "Returned to " + session.Stage + " (restarted)" : "Back failed: " + reply.message;
                confirmBack = false;
            }
            if (GUILayout.Button("Cancel")) confirmBack = false;
        }
        if (!string.IsNullOrEmpty(backMessage)) GUILayout.Label(backMessage);
        GUI.enabled = (session.Status == SessionStatus.Running || session.Status == SessionStatus.Paused)
            && !(session.Stage == ExperienceStage.SeabedCleanup && session.CleanupResolved);
        if (GUILayout.Button("Skip current stage (records Skipped)", GUILayout.Height(32)))
        { confirmSkip = true; confirmBack = false; skipRevision = session.Revision; }
        if (confirmSkip && skipRevision != session.Revision) confirmSkip = false;
        if (confirmSkip)
        {
            GUILayout.Label("Skip " + session.Stage + "? It will be recorded as Skipped, not Completed."
                + (session.Stage == ExperienceStage.SeabedCleanup ? " You must still return to the land platform." : " The experience will advance to the next stage."));
            if (GUILayout.Button("Confirm Skip"))
            { control.SkipStage(); confirmSkip = false; }
            if (GUILayout.Button("Cancel")) confirmSkip = false;
        }
        GUI.enabled = true;
        if (GUILayout.Button("End / Reset to waiting", GUILayout.Height(32))) control.ResetSession();
        var reset=coordinator.GlobalReset;
        if(reset)
        {
            GUILayout.Label("Reset: "+(reset.LastSucceeded ? "Ready" : "FAILED — retry required")
                +" | "+reset.LastDurationSeconds.ToString("F2")+"s | successful resets: "+reset.CompletedResets);
            if(!reset.LastSucceeded) GUILayout.Label(reset.LastError);
        }
        if(session.Status==SessionStatus.Idle)
            GUILayout.Label("交接 / Handover: 确认 Waiting → 协助摘下并清洁头显与手柄 →\n为下一位调整佩戴和模式 → Start。两分钟目标需现场计时验证。");
        foreach (ExperienceStage stage in System.Enum.GetValues(typeof(ExperienceStage)))
            if (stage != ExperienceStage.Inside) GUILayout.Label(stage + ": " + session.Result(stage));
        GUILayout.EndArea();
    }
}
