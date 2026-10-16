using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1200)]
[DisallowMultipleComponent]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4GazeInput")]
public sealed class GazeInput : MonoBehaviour, IResettable
{
    public int ResetOrder => -800;
    public void PrepareReset() { dwell.Reset(); TargetName=""; if(feedback) feedback.Hide(); }
    public void RestoreReset() { Accessible=false; dwell.Reset(); TargetName=""; revision=controller ? controller.Session.Revision : -1; }

    public static GazeInput Current { get; private set; }
    [Range(.5f,5)] public float dwellSeconds = 1.8f;
    [Min(.5f)] public float maxDistance = 8;
    public LayerMask occlusionMask = ~0;
    public bool Accessible { get; private set; }
    public string TargetName { get; private set; } = "";
    public float Progress => dwell.Progress;
    readonly GazeDwell dwell = new GazeDwell();
    readonly List<Canvas> canvases = new List<Canvas>();
    GazeFeedback feedback;
    SessionController controller;
    float nextScan;
    int revision = -1;
    bool focused = true;
    void Awake()
    {
        Current = this; controller = GetComponent<SessionController>();
        var obj = new GameObject("Head gaze progress",typeof(RectTransform)); obj.transform.SetParent(transform,false);
        feedback = obj.AddComponent<GazeFeedback>();
    }
    public void SetAccessible(bool value)
    {
        if (Accessible == value) return;
        Accessible = value; dwell.CancelProgress();
        SessionTelemetry.Report("AccessibilityModeChanged",this,value ? "Accessible" : "Standard");
    }
    public bool Claim(AccessibleButton button,bool gaze)
    {
        if (!isActiveAndEnabled) return !gaze;
        if (!focused || (gaze && !Accessible && !button.gazeInStandardMode)) return false;
        if (controller && controller.Session.Status == SessionStatus.Completed) return false;
        if (controller && controller.Session.Status == SessionStatus.Paused && !button.allowWhilePaused) return false;
        if ((Accessible || gaze || button.gazeInStandardMode) && !dwell.Claim(button.GetInstanceID(),Time.unscaledTimeAsDouble)) return false;
        SessionTelemetry.Report(gaze ? "GazeActivated" : "UIActivated",button,button.name);
        return true;
    }
    bool Eligible(AccessibleButton button)
    {
        return button && button.IsActive() && button.IsInteractable() && (Accessible || button.gazeInStandardMode)
            && (!controller || controller.Session.Status != SessionStatus.Completed)
            && (!controller || controller.Session.Status != SessionStatus.Paused || button.allowWhilePaused);
    }
    void LateUpdate()
    {
        if (!focused || !Camera.main) { dwell.CancelProgress(); feedback.Hide(); return; }
        if (controller && revision != controller.Session.Revision)
        {
            revision = controller.Session.Revision; dwell.CancelProgress();
            if (controller.Session.Status == SessionStatus.Idle) { SetAccessible(false); dwell.Reset(); }
        }
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime+1; canvases.Clear();
            foreach (var canvas in Resources.FindObjectsOfTypeAll<Canvas>())
                if (canvas.gameObject.scene.IsValid() && canvas.gameObject.scene.isLoaded && canvas.renderMode == RenderMode.WorldSpace)
                    canvases.Add(canvas);
        }
        var camera = Camera.main;
        var ray = new Ray(camera.transform.position,camera.transform.forward);
        float distance = maxDistance;
        // Ignore the visitor's own body; environmental meshes still occlude buttons.
        var origin = camera.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
        foreach (var hit in Physics.RaycastAll(ray,maxDistance,occlusionMask,QueryTriggerInteraction.Ignore))
            if (!origin || !hit.transform.IsChildOf(origin.transform)) distance = Mathf.Min(distance,hit.distance);
        Graphic winner = null;
        foreach (var canvas in canvases)
        {
            if (!canvas || !canvas.isActiveAndEnabled) continue;
            // uGUI 1.0 returns IndexedSet through IList; its enumerator throws.
            // Use Count/index access, as Unity's own graphic raycasters do.
            var graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
            for (int i = 0; i < graphics.Count; i++)
            {
                var graphic = graphics[i];
                if (!graphic || !graphic.isActiveAndEnabled || !graphic.raycastTarget || !graphic.canvasRenderer || graphic.canvasRenderer.cull) continue;
                var rect = graphic.rectTransform;
                if (Vector3.Dot(ray.direction,rect.forward) <= 0) continue;
                if (!new Plane(rect.forward,rect.position).Raycast(ray,out float t) || t < 0 || t > distance+.001f) continue;
                var point = ray.GetPoint(t);
                if (!rect.rect.Contains(rect.InverseTransformPoint(point)) || !graphic.Raycast(camera.WorldToScreenPoint(point),camera)) continue;
                if (winner && Mathf.Abs(t-distance) < .001f)
                {
                    if (canvas == winner.canvas && graphic.depth <= winner.depth) continue;
                    if (canvas != winner.canvas)
                    {
                        int layer=SortingLayer.GetLayerValueFromID(canvas.sortingLayerID), previous=SortingLayer.GetLayerValueFromID(winner.canvas.sortingLayerID);
                        if (layer<previous || (layer==previous && canvas.sortingOrder<winner.canvas.sortingOrder)) continue;
                    }
                }
                winner = graphic; distance = t;
            }
        }
        var target = winner ? winner.GetComponentInParent<AccessibleButton>() : null;
        if (!Eligible(target)) target = null;
        TargetName = target ? target.name : "";
        bool complete = dwell.Step(target ? target.GetInstanceID() : 0,Time.unscaledDeltaTime,dwellSeconds);
        if (Accessible || target) feedback.Show(camera,distance,dwell.Progress,target);
        else feedback.Hide();
        // Native XRI UI dispatch runs in Update; dwell submits afterwards through the same gate.
        if (complete && target) target.SubmitOnce(true);
    }
    void OnApplicationFocus(bool value) { focused = value; dwell.CancelProgress(); }
    void OnDisable() { Accessible=false; dwell.Reset(); if (feedback) feedback.Hide(); }
    void OnDestroy() { if (Current == this) Current = null; if (feedback) Destroy(feedback.gameObject); }
}
