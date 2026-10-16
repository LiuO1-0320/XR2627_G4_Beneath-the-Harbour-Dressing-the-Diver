using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

[DisallowMultipleComponent]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4GlobalReset")]
public sealed class GlobalReset : MonoBehaviour
{
    public bool IsResetting => sequence.IsRunning;
    public bool LastSucceeded { get; private set; } = true;
    public string LastError { get; private set; } = "";
    public double LastDurationSeconds { get; private set; }
    public int CompletedResets { get; private set; }
    readonly ResetSequence sequence=new ResetSequence();
    sealed class Step : IResettable
    {
        public int ResetOrder { get; set; }
        public Action prepare=()=>{}, restore=()=>{};
        public void PrepareReset()=>prepare(); public void RestoreReset()=>restore();
    }
    sealed class ActivityReset : IResettable
    {
        readonly IStageActivity activity;
        public ActivityReset(IStageActivity value) { activity=value; }
        public int ResetOrder=>100;
        public void PrepareReset()=>activity.EndStage();
        public void RestoreReset()=>activity.ResetActivity();
    }
    public bool ResetAll()
    {
        if (IsResetting) return false;
        var controller=GetComponent<SessionController>();
        var coordinator=GetComponent<StageCoordinator>();
        if(!controller || !coordinator || !coordinator.IsReady) throw new InvalidOperationException("Session coordinator is not ready.");
        double start=Time.realtimeSinceStartupAsDouble;
        var modules=new List<IResettable>();
        foreach(var component in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
        {
            if(!component || !component.gameObject.scene.IsValid() || !component.gameObject.scene.isLoaded) continue;
            if(component is IResettable resettable) modules.Add(resettable);
            else if(component is IStageActivity activity) modules.Add(new ActivityReset(activity));
        }
        modules.Add(new Step { ResetOrder=-950,prepare=coordinator.PrepareGlobalReset });
        // Use XRI's cancellation API, not normal release/fit events. Cancel every selected
        // object even if an earlier object has a faulty callback.
        foreach(var interactable in Resources.FindObjectsOfTypeAll<XRBaseInteractable>())
        {
            if(!interactable || !interactable.gameObject.scene.IsValid() || !interactable.gameObject.scene.isLoaded) continue;
            var target=interactable;
            modules.Add(new Step { ResetOrder=-900,prepare=()=>
            {
                if(!target) return;
                if(target.isSelected && target.interactionManager) target.interactionManager.CancelInteractableSelection((IXRSelectInteractable)target);
                if(target.isSelected) throw new InvalidOperationException(target.name+" is still selected.");
                if(target is XRGrabInteractable)
                {
                    var detach=typeof(XRGrabInteractable).GetField("m_DetachInLateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
                    if(detach==null) throw new InvalidOperationException("XRI deferred release adapter requires review.");
                    detach.SetValue(target,false);
                }
            }});
        }
        modules.Add(new Step { ResetOrder=900,prepare=ClearLocomotionQueues,restore=()=>
        {
            ClearLocomotionQueues();
            if(!Camera.main || !Camera.main.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() || !coordinator.navigation || !coordinator.navigation.start)
                throw new InvalidOperationException("Assign the XR Origin camera and navigation.start before Reset.");
            coordinator.navigation.SessionTravelTo(0);
            Physics.SyncTransforms();
        }});
        SessionTelemetry.Report("GlobalResetStarted",this,controller.Session.SessionId.ToString());
        var failures=sequence.Run(modules,()=>
        {
            if(!controller.Session.Reset()) throw new InvalidOperationException("Session rejected Reset.");
        },()=>
        {
            if(EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            coordinator.FinishGlobalReset();
            if(controller.Session.Status!=SessionStatus.Idle || controller.Session.Stage!=ExperienceStage.Briefing || DiveRopeStation.IsPlayerTravelling)
                throw new InvalidOperationException("Session has not returned to Waiting.");
        });
        LastDurationSeconds=Time.realtimeSinceStartupAsDouble-start;
        LastSucceeded=failures.Count==0; LastError=string.Join("\n",failures);
        coordinator.ReportResetResult(LastSucceeded);
        if(LastSucceeded) CompletedResets++;
        SessionTelemetry.Report(LastSucceeded ? "GlobalResetCompleted" : "GlobalResetFailed",this,
            LastDurationSeconds.ToString("F3")+"s; "+LastError);
        if(!LastSucceeded) throw new InvalidOperationException(LastError);
        return true;
    }
    // XRI 3.1.2 exposes no public queue-clear API. Keep this small version-specific bridge
    // isolated, preserve members in link.xml, and fail visibly if a package upgrade changes it.
    static void ClearLocomotionQueues()
    {
        var camera=Camera.main;
        var origin=camera ? camera.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() : null;
        if(!origin) return;
        var valid=typeof(TeleportationProvider).GetProperty("validRequest",BindingFlags.Instance|BindingFlags.NonPublic);
        var queue=typeof(XRBodyTransformer).GetField("m_TransformationsQueue",BindingFlags.Instance|BindingFlags.NonPublic);
        var end=typeof(LocomotionProvider).GetMethod("TryEndLocomotion",BindingFlags.Instance|BindingFlags.NonPublic);
        if(valid==null || queue==null || end==null) throw new InvalidOperationException("XRI reset adapter requires review for this package version.");
        foreach(var provider in origin.GetComponentsInChildren<LocomotionProvider>(true)) end.Invoke(provider,null);
        foreach(var provider in origin.GetComponentsInChildren<TeleportationProvider>(true)) valid.SetValue(provider,false);
        foreach(var gravity in origin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity.GravityProvider>(true)) gravity.ResetFallForce();
        foreach(var body in origin.GetComponentsInChildren<XRBodyTransformer>(true))
        {
            var pending=queue.GetValue(body);
            var clear=pending?.GetType().GetMethod("Clear");
            if(clear==null) throw new InvalidOperationException("Cannot clear XR movement queue.");
            clear.Invoke(pending,null);
        }
    }
}
