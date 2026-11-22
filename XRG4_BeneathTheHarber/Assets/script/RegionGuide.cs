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
    public bool HasDestination => destination;
    public Vector3 DestinationPoint => arrivalArea && arrivalArea.enabled && arrivalArea.gameObject.activeInHierarchy
        ? arrivalArea.bounds.center : destination ? destination.TransformPoint(localArrivalPoint) : transform.position;
    public string LocationEvidence { get; private set; } = "Unknown";
    public bool ArrivalFinished => LocationEvidence == "TargetArrivalObserved" && arrivalReported;
    public event System.Action<System.Guid, ExperienceStage> ArrivalConfirmed;
    System.Guid routeSession;
    ExperienceStage routeStage;
    SessionController controller;
    Transform destination;
    Collider arrivalArea;
    Vector3 localArrivalPoint;
    string destinationName;
    GameObject panel, targetMarker;
    Text markerText;
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
        if (targetMarker) targetMarker.SetActive(false);
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
        Vector3 direction = Vector3.ProjectOnPlane(viewer.transform.forward, Vector3.up).normalized;
        // Keep the card below the sightline, within the central view of both eyes.
        const float cardDepth = 1.2f;
        const float cardViewportY = .35f;
        var eye = viewer.stereoEnabled ? Camera.MonoOrStereoscopicEye.Left : Camera.MonoOrStereoscopicEye.Mono;
        var left = viewer.ViewportToWorldPoint(new Vector3(.28f, cardViewportY, cardDepth), eye);
        var right = viewer.ViewportToWorldPoint(new Vector3(.72f, cardViewportY, cardDepth), eye);
        float cardWidth = Mathf.Min(Vector3.Distance(left, right), .85f);
        panel.transform.localScale = Vector3.one * (cardWidth / 900f);
        Vector3 position = (left + right) * .5f;
        if (viewer.stereoEnabled)
        {
            var otherEye = viewer.ViewportToWorldPoint(new Vector3(.5f, cardViewportY, cardDepth), Camera.MonoOrStereoscopicEye.Right);
            position = (position + otherEye) * .5f;
        }
        panel.transform.SetPositionAndRotation(position, viewer.transform.rotation);
        // Tracking continues during pause, but routing/fading does not advance.
        if (state == SessionStatus.Paused) { if (targetMarker) targetMarker.SetActive(false); return; }
        Vector3 target = DestinationPoint;
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
        if (targetMarker)
        {
            targetMarker.SetActive(state == SessionStatus.Running && !arrived);
            targetMarker.transform.position = target + Vector3.up * 1.8f;
            var facing = targetMarker.transform.position - viewer.transform.position;
            if (facing.sqrMagnitude > .001f) targetMarker.transform.rotation = Quaternion.LookRotation(facing);
            markerText.text = "▼ 当前目标 / Current target\n" + destinationName;
        }
        LocationEvidence = arrived ? "TargetArrivalObserved" : "TravellingToTarget";
        IsGuiding = !arrived;
        panel.SetActive(true);
        if (IsGuiding) panel.SetActive(true);
        group.alpha = Mathf.MoveTowards(group.alpha, 1,
            Time.unscaledDeltaTime / Mathf.Max(0.1f, IsGuiding ? fadeInSeconds : fadeOutSeconds));
        Vector3 toward = Vector3.ProjectOnPlane(target - viewer.transform.position, Vector3.up);
        float angle = Vector3.SignedAngle(direction, toward, Vector3.up);
        string bearing = Mathf.Abs(angle) > 135 ? "后方 / Behind" : angle > 30 ? "右侧 / Right" : angle < -30 ? "左侧 / Left" : "前方 / Ahead";
        float distance = Vector2.Distance(new Vector2(feet.x, feet.z), new Vector2(target.x, target.z));
        string arrow = Mathf.Abs(angle) > 135 ? "↶" : angle > 30 ? "→" : angle < -30 ? "←" : "↑";
        label.text = arrived ? "已到达：" + destinationName + "\n" + TaskInstruction(routeStage)
            : "当前任务：" + destinationName + "\n" + arrow + " " + bearing + " · " + distance.ToString("0.0") + " m\n请完成当前站点后再前往下一站";
        if (arrived && !arrivalReported)
        {
            arrivalReported = true;
            SessionTelemetry.Report("RouteArrived", destination, destinationName);
            ArrivalConfirmed?.Invoke(routeSession, routeStage);
        }
    }
    string TaskInstruction(ExperienceStage stage)
    {
        switch (stage)
        {
            case ExperienceStage.Briefing: return "完成操作练习，选择探索头盔 / Complete practice";
            case ExperienceStage.TheCase: return "抓起头盔，靠近头部，松开握把佩戴 / Fit helmet";
            case ExperienceStage.ScaleUp: return "逐一抓取可拆零件观察并装回 / Inspect and return each part";
            case ExperienceStage.DressTheDiver: return "给潜水员戴上头盔，然后跟随前往下潜平台";
            case ExperienceStage.Descent: return "操作绳索下潜 / Use the rope to descend";
            case ExperienceStage.SeabedCleanup: return controller.Session.CleanupResolved
                ? "沿绳上浮，返回陆地平台" : "抓取垃圾，松手放入回收篮";
            default: return "完成当前任务后自动显示下一站";
        }
    }
    void Build()
    {
        panel = new GameObject("Route guidance (temporary)", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        panel.transform.SetParent(transform, false);
        panel.transform.localScale = Vector3.one * 0.0013f;
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 230);
        group = panel.GetComponent<CanvasGroup>(); group.alpha = 0;
        group.interactable = false; group.blocksRaycasts = false;
        var background = panel.AddComponent<Image>(); background.color = new Color(0.02f, 0.09f, 0.12f, 0.8f); background.raycastTarget = false;
        var words = new GameObject("Destination", typeof(RectTransform), typeof(Text));
        words.transform.SetParent(panel.transform, false);
        words.GetComponent<RectTransform>().sizeDelta = new Vector2(860, 220);
        label = words.GetComponent<Text>(); label.fontSize = 28; label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false; label.color = Color.white;
        if (!font) fallback = Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 28);
        label.font = font ? font : fallback ? fallback : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        targetMarker = new GameObject("Current task destination marker", typeof(RectTransform), typeof(Canvas));
        targetMarker.transform.SetParent(transform, false);
        targetMarker.transform.localScale = Vector3.one * .002f;
        targetMarker.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 150);
        targetMarker.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var markerWords = new GameObject("Destination", typeof(RectTransform), typeof(Text));
        markerWords.transform.SetParent(targetMarker.transform, false);
        markerWords.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 150);
        markerText = markerWords.GetComponent<Text>();
        markerText.font = label.font; markerText.fontSize = 32;
        markerText.alignment = TextAnchor.MiddleCenter;
        markerText.color = new Color(.3f, 1f, .75f); markerText.raycastTarget = false;
        targetMarker.SetActive(false);
    }
    void OnDisable() { Clear(); }
    void OnDestroy() { if (panel) Destroy(panel); if (targetMarker) Destroy(targetMarker); if (fallback) Destroy(fallback); }
}
