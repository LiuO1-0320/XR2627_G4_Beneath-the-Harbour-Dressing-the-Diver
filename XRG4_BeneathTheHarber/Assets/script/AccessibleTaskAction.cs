using System;
using UnityEngine;

/// <summary>M3 implements real equivalent actions, validates its own preconditions and emits
/// its existing success event. Experience never calls CompleteTask on behalf of an assisted action.</summary>
public interface IAccessibleActionProvider
{
    bool CanExecuteAccessibleAction(string actionId, Guid sessionId, ExperienceStage stage, out string reason);
    void ExecuteAccessibleAction(string actionId, Guid capturedSessionId, ExperienceStage stage);
}

[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4AccessibleTaskAction")]
public sealed class AccessibleTaskAction : MonoBehaviour
{
    public MonoBehaviour provider;
    public ExperienceStage stage;
    public string actionId;
    public string caption = "辅助操作 / Assisted action";
    public bool Available(SessionState state, out string reason)
    {
        reason = "等待 M3 接口 / Awaiting M3 provider";
        if (!isActiveAndEnabled || state.Status != SessionStatus.Running || (state.Stage != stage && state.Stage != ExperienceStage.FreeExploration)) return false;
        if (!provider || !provider.isActiveAndEnabled || !(provider is IAccessibleActionProvider adapter)) return false;
        return adapter.CanExecuteAccessibleAction(actionId,state.SessionId,stage,out reason);
    }
    public bool Execute(SessionState state, Guid capturedSession, int capturedRevision)
    {
        if (state.SessionId != capturedSession || state.Revision != capturedRevision || !Available(state,out _)) return false;
        ((IAccessibleActionProvider)provider).ExecuteAccessibleAction(actionId,capturedSession,stage);
        SessionTelemetry.Report("AccessibleTaskRequested",this,actionId); return true;
    }
}
