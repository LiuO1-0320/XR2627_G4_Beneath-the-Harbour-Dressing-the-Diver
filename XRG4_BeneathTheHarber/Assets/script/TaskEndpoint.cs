using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>M3 owns completion criteria. Capture the supplied session ID at Begin;
/// report that captured ID, never the controller's latest ID from a delayed callback.</summary>
public interface IStageActivity
{
    void BeginStage(Guid sessionId, ExperienceStage stage);
    void SetStagePaused(bool paused);
    void EndStage();
    void ResetActivity();
}

[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4TaskEndpoint")]
public sealed class TaskEndpoint : MonoBehaviour, IStageActivity
{
    public SessionController controller;
    public ExperienceStage stage = ExperienceStage.DressTheDiver;
    [Serializable] public sealed class BeginEvent : UnityEvent<string> { }
    public BeginEvent onBegin = new BeginEvent();
    public UnityEvent onPause = new UnityEvent(), onResume = new UnityEvent();
    public UnityEvent onEnd = new UnityEvent(), onReset = new UnityEvent();
    Guid activeSession;
    bool active, paused, pendingCompletion;
    public void BeginStage(Guid sessionId, ExperienceStage enteredStage)
    {
        activeSession = sessionId; active = enteredStage == stage;
        paused = pendingCompletion = false;
        if (active) onBegin.Invoke(sessionId.ToString());
    }
    // M3 calls this only after its own validated success event. Safe to retry.
    public void ReportCompleted(string capturedSessionId)
    {
        if (!active || !Guid.TryParse(capturedSessionId, out var id) || id != activeSession) return;
        pendingCompletion = true;
        SessionTelemetry.Report("TaskCompletionReported", this, stage.ToString());
    }
    // Optional semantic progress from M3. Reporting progress never completes a stage.
    public void ReportProgress(string capturedSessionId, string detail)
    {
        if (!active || paused || !controller || !Guid.TryParse(capturedSessionId, out var id) || id != activeSession
            || controller.Session.SessionId != id || controller.Session.Stage != stage
            || controller.Session.Status != SessionStatus.Running) return;
        SessionTelemetry.Report("TaskProgress", this, detail == null ? "" : detail.Substring(0, Math.Min(detail.Length, 1024)));
    }
    void Update()
    {
        if (pendingCompletion && !paused && controller && controller.Session.CompleteTask(activeSession, stage))
            pendingCompletion = false;
    }
    public void SetStagePaused(bool value)
    {
        if (!active || paused == value) return;
        paused = value;
        if (value) onPause.Invoke(); else onResume.Invoke();
    }
    public void EndStage() { if (active) onEnd.Invoke(); active = pendingCompletion = false; }
    public void ResetActivity() { active = paused = pendingCompletion = false; activeSession = Guid.Empty; onReset.Invoke(); }
}
