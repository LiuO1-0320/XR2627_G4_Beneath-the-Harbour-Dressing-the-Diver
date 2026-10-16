using UnityEngine;
using UnityEngine.UI;

/// <summary>Route UI is visible only between experience regions; destination tracking remains active.</summary>
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4RegionGuide")]
public sealed class RegionGuide : MonoBehaviour, IResettable
{
    public int ResetOrder => 100;
    public void PrepareReset() { Clear(); }
    public void RestoreReset() { Clear(); }

    [Min(0.1f)] public float fadeInSeconds = 1.5f;
    [Min(0.1f)] public float fadeOutSeconds = 0.4f;
    [Min(0.1f)] public float arrivalRadius = 3f;
    [Min(0.1f)] public float exhibitArrivalRadius = 1.25f;
    [Min(0.1f)] public float arrivalHeightTolerance = 2f;
    public Font font;
    public bool IsGuiding { get; private set; }
    public string DestinationName => destinationName ?? "";
    public string LocationEvidence { get; private set; } = "Unknown";
    public bool ArrivalFinished => LocationEvidence == "TargetArrivalObserved" && panel && !panel.activeSelf;
    public event System.Action<System.Guid, ExperienceStage> ArrivalConfirmed;
    System.Guid routeSession;
    ExperienceStage routeStage;
    SessionController controller;
    Transform destination;
    Collider arrivalArea;
    Vector3 localArrivalPoint;
    string destinationName;
    GameObject panel;
    CanvasGroup group;
    Text label;
    Font fallback;
    StageCoordinator coordinator;
    bool arrivalReported;

    public void GuideTo(SessionController sessionController, Transform target, Vector3 arrivalPoint, Collider area, string title)
    {
        Clear();
        if (!target) return;
        controller = sessionController; destination = target; arrivalArea = area;
        coordinator = controller.GetComponent<StageCoordinator>();
        routeSession = controller.Session.SessionId; routeStage = controller.Session.Stage;
        localArrivalPoint = target.InverseTransformPoint(arrivalPoint); destinationName = title;
        IsGuiding = false;
        LocationEvidence = "TravellingToTarget";
        SessionTelemetry.Report("RouteStarted", target, title);
        if (!panel) Build();
        panel.SetActive(false);
    }
    public void Clear()
    {
        IsGuiding = false; destination = null; arrivalArea = null;
        destinationName = "";
        LocationEvidence = "Unknown";
        routeSession = System.Guid.Empty;
        arrivalReported = false;
        if (group) group.alpha = 0;
        if (panel) panel.SetActive(false);
    }
    void LateUpdate()
    {
        // Continue tracking while hidden so leaving a region can show guidance again.
        if (!panel || !controller || !destination) return;
        var state = controller.Session.Status;
        if (controller.Session.SessionId != routeSession || controller.Session.Stage != routeStage) { Clear(); return; }
        if (state == SessionStatus.Idle || state == SessionStatus.Completed || !destination) { Clear(); return; }
        var viewer = Camera.main;
        if (!viewer) return;
        bool insideAnyRegion = coordinator && coordinator.IsVisitorInAnyExperienceRegion();
        if (insideAnyRegion)
        {
            group.alpha = 0; panel.SetActive(false); IsGuiding = false;
        }
        Vector3 direction = Vector3.ProjectOnPlane(viewer.transform.forward, Vector3.up).normalized;
        if (direction.sqrMagnitude > 0.001f)
            panel.transform.SetPositionAndRotation(viewer.transform.position + direction * 2f + Vector3.down * 0.55f, Quaternion.LookRotation(direction));
        // Tracking continues during pause, but routing/fading does not advance.
        if (state == SessionStatus.Paused) return;
        Vector3 target = destination.TransformPoint(localArrivalPoint);
        if (arrivalArea && arrivalArea.enabled && arrivalArea.gameObject.activeInHierarchy)
            target = arrivalArea.bounds.center;
        var origin = viewer.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
        Vector3 feet = viewer.transform.position;
        feet.y = origin ? origin.transform.position.y : feet.y - 1.6f;
        bool arrived;
        if (arrivalArea)
        {
            // An authored volume covers standing space, not just the exhibit mesh.
            var probe = feet + Vector3.up * 0.25f;
            arrived = arrivalArea.enabled && arrivalArea.gameObject.activeInHierarchy
                && (arrivalArea.ClosestPoint(probe) - probe).sqrMagnitude < 0.0025f;
        }
        else
            arrived = Vector2.Distance(new Vector2(feet.x, feet.z), new Vector2(target.x, target.z)) <=
                (routeStage == ExperienceStage.TheCase || routeStage == ExperienceStage.ScaleUp || routeStage == ExperienceStage.DressTheDiver ? exhibitArrivalRadius : arrivalRadius)
                && Mathf.Abs(feet.y - target.y) <= arrivalHeightTolerance;
        LocationEvidence = arrived ? "TargetArrivalObserved" : "TravellingToTarget";
        IsGuiding = !arrived && !insideAnyRegion;
        if (IsGuiding) panel.SetActive(true);
        group.alpha = Mathf.MoveTowards(group.alpha, IsGuiding ? 1 : 0,
            Time.unscaledDeltaTime / Mathf.Max(0.1f, IsGuiding ? fadeInSeconds : fadeOutSeconds));
        if (!IsGuiding && group.alpha <= 0)
        {
            panel.SetActive(false);
            if (arrived && !arrivalReported)
            {
                arrivalReported = true;
                SessionTelemetry.Report("RouteArrived", destination, destinationName);
                ArrivalConfirmed?.Invoke(routeSession, routeStage);
            }
            return;
        }
        Vector3 toward = Vector3.ProjectOnPlane(target - viewer.transform.position, Vector3.up);
        float angle = Vector3.SignedAngle(direction, toward, Vector3.up);
        string bearing = Mathf.Abs(angle) > 135 ? "后方 / Behind" : angle > 30 ? "右侧 / Right" : angle < -30 ? "左侧 / Left" : "前方 / Ahead";
        label.text = "请前往：" + destinationName + "\nPlease make your way to the next area\n" + bearing;
    }
    void Build()
    {
        panel = new GameObject("Route guidance (temporary)", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        panel.transform.SetParent(transform, false);
        panel.transform.localScale = Vector3.one * 0.0013f;
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 170);
        group = panel.GetComponent<CanvasGroup>(); group.alpha = 0;
        group.interactable = false; group.blocksRaycasts = false;
        var background = panel.AddComponent<Image>(); background.color = new Color(0.02f, 0.09f, 0.12f, 0.8f); background.raycastTarget = false;
        var words = new GameObject("Destination", typeof(RectTransform), typeof(Text));
        words.transform.SetParent(panel.transform, false);
        words.GetComponent<RectTransform>().sizeDelta = new Vector2(860, 160);
        label = words.GetComponent<Text>(); label.fontSize = 28; label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false; label.color = Color.white;
        if (!font) fallback = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 28);
        label.font = font ? font : fallback ? fallback : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
    void OnDisable() { Clear(); }
    void OnDestroy() { if (panel) Destroy(panel); if (fallback) Destroy(fallback); }
}
