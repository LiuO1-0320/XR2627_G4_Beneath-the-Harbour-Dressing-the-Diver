using UnityEngine;

[DisallowMultipleComponent]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4SessionController")]
public sealed class SessionController : MonoBehaviour
{
    public SessionState Session { get; } = new SessionState();
    public SessionTelemetry Telemetry { get; private set; }
    void Awake() { Telemetry = GetComponent<SessionTelemetry>(); if (!Telemetry) Telemetry = gameObject.AddComponent<SessionTelemetry>(); }
    public void StartSession() => Telemetry.ExecuteLocal("Start");
    public void PauseSession() => Telemetry.ExecuteLocal("Pause");
    public void ResumeSession() => Telemetry.ExecuteLocal("Resume");
    public void SkipStage() => Telemetry.ExecuteLocal("Skip");
    public void ResetSession() => Telemetry.ExecuteLocal("Reset");
}
