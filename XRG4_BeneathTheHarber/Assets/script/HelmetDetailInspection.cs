using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

/// <summary>Independent exhibit for helmet 2. Exact part keys exclude nail2 through nail12.</summary>
[ExecuteAlways]
public class HelmetDetailInspection : MonoBehaviour, IResettable, IStageActivity
{
    public int ResetOrder => 10;
    public void PrepareReset() { PrepareSessionReset(); }
    public void RestoreReset() { ResetExhibit(); if(Held()) throw new System.InvalidOperationException("Inspection parts are still selected."); }

    [Serializable] public class PartDescription
    {
        public string key, title;
        [TextArea(3, 8)] public string description;
        [TextArea(3, 8)] public string englishDescription;
        public PartDescription(string k, string t, string d)
        { key = k; title = t; description = d; englishDescription = EnglishDescription(k); }
    }
    public Transform helmet;
    public Font font;
    [Min(0.03f)] public float reattachDistance = 0.15f;
    public PartDescription[] descriptions = {
        new PartDescription("forward", "前部视窗 / Front viewport", "传统铜制潜水头盔配有玻璃视窗。观察前部零件的边框、开口和连接结构，再比较它与两侧部件的形状。"),
        new PartDescription("left", "左侧视窗 / Left viewport", "观察左侧部件的轮廓、边框和与头盔外壳连接的位置，比较它与右侧部件的异同。"),
        new PartDescription("right", "右侧视窗 / Right viewport", "将零件转到不同角度，观察内外两面以及边缘的连接结构，并与左侧部件比较。"),
        new PartDescription("up", "顶部部件 / Top component", "观察顶部部件的开口、边框及安装位置，思考不同方向的视窗如何影响观察范围。"),
        new PartDescription("fixing", "固定连接件 / Fixing component", "观察固定件与其他零件接触的表面、孔位和轮廓。此处展示模型的连接结构，具体功能需结合实物型号确认。"),
        new PartDescription("nameplate", "铭牌 / Nameplate", "仔细查看铭牌表面是否有文字、标记或编号。它们可能为识别设备提供线索；本模型的具体制造信息尚未确认。"),
        new PartDescription("nail1", "紧固件 nail1 / Fastener", "观察这枚紧固件的头部、杆身和安装位置。为了避免重复操作，本展项仅开放 nail1，其他 nail 保持固定。")
    };
    class Part
    {
        public Transform transform, parent;
        public Vector3 position, scale;
        public Quaternion rotation;
        public XRGrabInteractable grab;
        public Rigidbody body;
        public bool detached;
        public Renderer renderer;
        public MaterialPropertyBlock original;
        public LineRenderer outline, socket;
        public Bounds meshBounds;
        public int releaseFrame = -1;
        public PartDescription info;
        public Action<SelectEnterEventArgs> entered;
        public Action<SelectExitEventArgs> exited;
    }
    Guid taskSession;
    bool taskActive, taskPaused, completing;
    readonly HashSet<string> inspected = new HashSet<string>();
    public int InspectedCount => inspected.Count;
    public int RequiredCount => parts.Count;
    public void BeginStage(Guid sessionId, ExperienceStage stage)
    { taskSession = sessionId; taskActive = stage == ExperienceStage.ScaleUp; taskPaused = completing = false; inspected.Clear(); UpdateStatus(); }
    public void SetStagePaused(bool paused) { taskPaused = paused; }
    public void EndStage() { taskActive = false; }
    public void ResetActivity() { taskActive = taskPaused = completing = false; inspected.Clear(); ResetExhibit(); }
    readonly List<Part> parts = new List<Part>();
    Vector3 baseScale, basePosition, localCenter;
    Quaternion baseRotation;
    Text details, status, checklist, actionHint;
    GameObject hintPanel;
    Material outlineMaterial;
    bool panelPlaced;
    string lastFeedback;
    float feedbackUntil;
    ScrollRect detailsScroll;
    GameObject panel;
    Font fallback;
    XRGrabInteractable wholeGrab;
    XRGeneralGrabTransformer wholeTransformer;
    readonly string instructions = "ACT 2 · 头盔细节观察 / Helmet details\n\n观察潜水头盔的视窗、连接件与铭牌。\n抓取头盔固定部分移动、旋转。\n松开整盔后，抓取零件查看详情。\n零件放回原位附近并松手可装回；Reset 恢复展项。";
    const string defaultDetails = "可拆：前、左、右、顶部、固定件、铭牌、nail1。\n抓取一个零件，即可在这里查看介绍。\n\nRemovable parts: front, left, right, top, fixing component, nameplate and nail1.\nGrab a part to read its description here.\n\n拖动文字或右侧滚动条查看全部内容。\nDrag the text or the scrollbar to read more.";

    void Update()
    {
        if (!panel)
        {
            BuildPanel();
            ShowDetails(defaultDetails);
            if (Application.isPlaying) UpdateStatus();
            else status.text = "操作介绍常驻显示 / Instructions always visible";
        }
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        if (!helmet) { Debug.LogError("Assign helmet 2.", this); enabled = false; return; }
        baseScale = helmet.localScale; basePosition = helmet.localPosition; baseRotation = helmet.localRotation;
        var renderers = helmet.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { enabled = false; return; }
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        localCenter = helmet.InverseTransformPoint(bounds.center);
        foreach (var child in helmet.GetComponentsInChildren<Transform>())
        {
            // Full token comparison is intentional: nail1 must never match nail10/11/12.
            string key = child.name.Substring(child.name.LastIndexOf('.') + 1).ToLowerInvariant();
            PartDescription info = Array.Find(descriptions, d => d != null && d.key == key);
            if (info == null || !child.GetComponent<Renderer>()) continue;
            var part = new Part { transform = child, parent = child.parent, position = child.localPosition,
                rotation = child.localRotation, scale = child.localScale, info = info };
            var rb = child.GetComponent<Rigidbody>();
            if (!rb) rb = child.gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false; rb.isKinematic = true;
            part.body = rb;
            var box = child.gameObject.AddComponent<BoxCollider>();
            var mesh = child.GetComponent<MeshFilter>();
            if (mesh && mesh.sharedMesh) { box.center = mesh.sharedMesh.bounds.center; box.size = mesh.sharedMesh.bounds.size; }
            // Give small fittings a usable ray target, protruding beyond the shell surface.
            Vector3 scale = child.lossyScale;
            Vector3 padding = new Vector3(.025f / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                .025f / Mathf.Max(.001f, Mathf.Abs(scale.y)), .025f / Mathf.Max(.001f, Mathf.Abs(scale.z)));
            box.size += padding;
            var grab = child.GetComponent<XRGrabInteractable>();
            if (!grab) grab = child.gameObject.AddComponent<XRGrabInteractable>();
            grab.colliders.Clear(); grab.colliders.Add(box);
            grab.useDynamicAttach = true; grab.retainTransformParent = false;
            grab.throwOnDetach = false; grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            part.grab = grab;
            grab.selectFilters.Add(new XRSelectFilterDelegate((interactor, interactable) =>
                part.detached || !wholeGrab || !wholeGrab.isSelected));
            part.entered = args => Detach(part);
            part.exited = args => { if (!args.isCanceled) part.releaseFrame = Time.frameCount + 2; };
            grab.selectEntered.AddListener(part.entered.Invoke);
            grab.selectExited.AddListener(part.exited.Invoke);
            part.renderer = child.GetComponent<Renderer>();
            part.original = new MaterialPropertyBlock(); part.renderer.GetPropertyBlock(part.original);
            part.meshBounds = mesh && mesh.sharedMesh ? mesh.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one * .1f);
            part.outline = MakeOutline("可拆部件 " + info.key);
            part.socket = MakeOutline("装回位置 " + info.key);
            parts.Add(part);
        }
        SetupWholeGrab();
        if (!panel) BuildPanel();
        ShowDetails(defaultDetails);
        UpdateStatus();
    }
    void Detach(Part part)
    {
        if (taskActive && !taskPaused && StageCoordinator.Allows(ExperienceStage.ScaleUp)) inspected.Add(part.info.key);
        SessionTelemetry.Report("PartDetached", part.transform, part.info.key);
        // Keep ACT2 part grabs silent while investigating the native audio resampler crash.
        part.releaseFrame = -1; part.detached = true;
        part.transform.SetParent(null, true);
        string english = string.IsNullOrWhiteSpace(part.info.englishDescription)
            ? EnglishDescription(part.info.key) : part.info.englishDescription;
        ShowDetails(part.info.title + "\n\n" + part.info.description + "\n\n" + english
            + "\n\n旋转手柄观察零件。放回原位附近并松手可装回。\nRotate your controller to inspect the part. Bring it close to its original position and release to reattach.");
        UpdateStatus();
    }
    void LateUpdate()
    {
        if (!Application.isPlaying) return;
        UpdateInteractionFeedback();
        if (!StageCoordinator.Allows(ExperienceStage.ScaleUp)) return;
        foreach (var part in parts)
        {
            if (part.releaseFrame < 0 || Time.frameCount < part.releaseFrame) continue;
            part.releaseFrame = -1;
            if (!part.grab.isSelected && Vector3.Distance(part.transform.position, part.parent.TransformPoint(part.position)) <= reattachDistance)
                Restore(part);
        }
        if (taskActive && !taskPaused && !completing && parts.Count > 0
            && parts.TrueForAll(p => inspected.Contains(p.info.key) && !p.detached) && !Held()
            && StageCoordinator.Current)
        {
            completing = true;
            StageCoordinator.Current.Controller.Session.CompleteTask(taskSession, ExperienceStage.ScaleUp);
        }
    }
    bool Held() { return (wholeGrab && wholeGrab.isSelected) || parts.Exists(p => p.grab.isSelected); }
    void SetupWholeGrab()
    {
        var body = helmet.GetComponent<Rigidbody>();
        if (!body) body = helmet.gameObject.AddComponent<Rigidbody>();
        body.useGravity = false; body.isKinematic = true;
        wholeTransformer = helmet.gameObject.AddComponent<XRGeneralGrabTransformer>();
        wholeTransformer.allowOneHandedScaling = false;
        wholeTransformer.allowTwoHandedScaling = false;
        wholeTransformer.allowTwoHandedRotation = XRGeneralGrabTransformer.TwoHandedRotationMode.TwoHandedAverage;
        wholeGrab = helmet.gameObject.AddComponent<XRGrabInteractable>();
        // Re-register only after the fixed-part collider list is complete. OnEnable
        // initially sees the imported hierarchy and its separate part colliders.
        wholeGrab.enabled = false;
        // Non-convex shell surfaces support raycasts, but not Physics.ClosestPoint.
        wholeGrab.distanceCalculationMode = XRBaseInteractable.DistanceCalculationMode.TransformPosition;
        wholeGrab.addDefaultGrabTransformers = false;
        wholeGrab.selectMode = InteractableSelectMode.Multiple;
        wholeGrab.useDynamicAttach = true;
        wholeGrab.snapToColliderVolume = false;
        wholeGrab.reinitializeDynamicAttachEverySingleGrab = true;
        wholeGrab.trackScale = false;
        wholeGrab.throwOnDetach = false;
        wholeGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        wholeGrab.AddSingleGrabTransformer(wholeTransformer);
        wholeGrab.AddMultipleGrabTransformer(wholeTransformer);
        wholeGrab.selectFilters.Add(new XRSelectFilterDelegate((interactor, interactable) =>
            !parts.Exists(p => !p.detached && p.grab.isSelected)));
        // Use the existing fixed model parts instead of generated grip spheres.
        // Detachable parts keep their own grab interactables and colliders.
        wholeGrab.colliders.Clear();
        foreach (var mesh in helmet.GetComponentsInChildren<MeshFilter>())
        {
            if (!mesh.sharedMesh || !mesh.GetComponent<Renderer>()) continue;
            if (parts.Exists(part => mesh.transform == part.transform || mesh.transform.IsChildOf(part.transform))) continue;
            // A shell's bounding box fills its openings and can occlude removable viewports.
            // The whole exhibit is kinematic, so use its actual mesh surface for ray targeting.
            var collider = mesh.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh.sharedMesh;
            collider.convex = false;
            wholeGrab.colliders.Add(collider);
        }
        wholeGrab.enabled = true;
    }
    void Restore(Part part)
    {
        if (part.detached)
        {
            SessionTelemetry.Report("PartRestored", part.transform, part.info.key);
            lastFeedback = part.info.title + " · 已装回 ✓"; feedbackUntil = Time.time + 2;
            // Keep reattachment silent too: preserve model-testing audio crash mitigation.
        }
        part.body.isKinematic = true;
        part.transform.SetParent(part.parent, false);
        part.transform.localPosition = part.position; part.transform.localRotation = part.rotation; part.transform.localScale = part.scale;
        part.detached = false; part.releaseFrame = -1;
        UpdateStatus();
    }
    public void Rotate(float angle)
    {
        if (!StageCoordinator.Allows(ExperienceStage.ScaleUp)) return;
        if (Held()) { status.text = "请先松开零件，再旋转 / Release part first"; return; }
        helmet.RotateAround(helmet.TransformPoint(localCenter), Vector3.up, angle);
        UpdateStatus();
    }
    public void ResetExhibit()
    {
        if (!Application.isPlaying || !helmet || !wholeGrab) return;
        if (Held()) { status.text = "请先松开零件，再复位 / Release part first"; return; }
        inspected.Clear();
        helmet.localScale = baseScale; helmet.localPosition = basePosition; helmet.localRotation = baseRotation;
        foreach (var part in parts) Restore(part);
        ShowDetails(defaultDetails); UpdateStatus();
    }
    public void SetSessionInteraction(bool allowed)
    {
        if (wholeGrab) wholeGrab.enabled = allowed;
        foreach (var part in parts) if (part.grab) part.grab.enabled = allowed;
    }
    public void PrepareSessionReset()
    {
        SetSessionInteraction(false);
        foreach (var part in parts) part.releaseFrame = -1;
    }
    void UpdateStatus()
    {
        if (checklist)
        {
            var lines = new System.Text.StringBuilder("部件清单 / Parts\n\n");
            foreach (var part in parts) lines.Append(part.detached ? "待装回 ○ " : inspected.Contains(part.info.key) ? "已完成 ✓ " : "未观察 · ").Append(part.info.title).Append("\n\n");
            checklist.text = lines.ToString();
        }
        if (status) status.text = $"已观察 {inspected.Count}/{parts.Count} · 未归位 {parts.FindAll(p => p.detached).Count}\n逐一抓取所有可拆零件并装回，完成后前往潜水员装备区";
    }
    public void RecoverLooseParts()
    {
        if (!StageCoordinator.Allows(ExperienceStage.ScaleUp) || Held()) return;
        // Recovery is not a successful manual reattachment: recovered parts must be inspected again.
        foreach (var part in parts) if (part.detached) { inspected.Remove(part.info.key); Restore(part); }
        lastFeedback = "散落零件已归位，其他完成进度保留"; feedbackUntil = Time.time + 3;
        UpdateStatus();
    }
    LineRenderer MakeOutline(string name)
    {
        if (!outlineMaterial)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) shader = Shader.Find("Sprites/Default");
            outlineMaterial = new Material(shader);
        }
        var obj = new GameObject(name); obj.transform.SetParent(transform, false);
        var line = obj.AddComponent<LineRenderer>(); line.sharedMaterial = outlineMaterial;
        line.useWorldSpace = true; line.widthMultiplier = .003f; line.positionCount = 16;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false; line.enabled = false;
        return line;
    }
    void DrawOutline(LineRenderer line, Part part, bool socket, Color color)
    {
        // Bounding outlines have no colliders and cannot intercept the controller ray.
        int[] edges = {0,1,3,2,0,4,5,1,5,7,3,7,6,2,6,4};
        for (int i = 0; i < edges.Length; i++)
        {
            int corner = edges[i];
            Vector3 local = part.meshBounds.center + Vector3.Scale(part.meshBounds.extents * 1.04f,
                new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
            Vector3 world = socket ? part.parent.TransformPoint(part.position + part.rotation * Vector3.Scale(part.scale, local))
                : part.transform.TransformPoint(local);
            line.SetPosition(i, world);
        }
        line.startColor = line.endColor = color; line.enabled = true;
    }
    void UpdateInteractionFeedback()
    {
        bool allowed = StageCoordinator.Allows(ExperienceStage.ScaleUp);
        if (allowed && !panelPlaced && panel && helmet && Camera.main
            && Vector3.Distance(helmet.position, Camera.main.transform.position) < 4f)
        {
            var viewer = Camera.main.transform;
            // Place once on arrival: stand to the right of the exhibit in the visitor's frontal view.
            Vector3 forward = Vector3.ProjectOnPlane(helmet.position - viewer.position, Vector3.up).normalized;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            // Canvas fronts face opposite their forward vector. Choose the visitor-facing side once.
            if (Vector3.Dot(forward, helmet.position - viewer.position) < 0) forward = -forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 position = helmet.position + right * 1.6f;
            position.y = helmet.position.y + .45f;
            panel.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            panel.transform.localScale = Vector3.one * .001f;
            panelPlaced = true;
        }
        string message = "指向标记部件，按住握把抓取 / Point at a part and hold grip";
        if (wholeGrab && (wholeGrab.isSelected || wholeGrab.isHovered))
            message = wholeGrab.isSelected ? "移动或旋转整盔；松开整盔后再拆零件" : "固定外壳：按住握把可移动整盔";
        foreach (var part in parts)
        {
            bool near = part.detached && Vector3.Distance(part.transform.position, part.parent.TransformPoint(part.position)) <= reattachDistance;
            bool highlighted = allowed && (part.grab.isHovered || part.grab.isSelected);
            var color = near ? new Color(.25f, 1, .55f) : highlighted ? Color.yellow : new Color(.25f,.8f,1);
            if (allowed && (!inspected.Contains(part.info.key) || highlighted)) DrawOutline(part.outline, part, false, color);
            else part.outline.enabled = false;
            if (allowed && part.detached) DrawOutline(part.socket, part, true, near ? Color.green : new Color(.3f,.8f,1));
            else part.socket.enabled = false;
            if (highlighted)
            {
                var block = new MaterialPropertyBlock(); part.renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color); block.SetColor("_Color", color); part.renderer.SetPropertyBlock(block);
                message = part.info.title + "\n" + (part.grab.isSelected
                    ? near ? "松开握把即可装回 / Release grip to reattach"
                        : "转动手柄观察；移向蓝色原位轮廓装回"
                    : "按住握把抓取 / Hold grip to grab");
#if UNITY_EDITOR
                message += " · 键盘 G";
#endif
            }
            else part.renderer.SetPropertyBlock(part.original);
        }
        if (Time.time < feedbackUntil && !Held()) message = lastFeedback;
        if (hintPanel)
        {
            bool interacting = parts.Exists(p => p.grab.isHovered || p.grab.isSelected)
                || (wholeGrab && (wholeGrab.isHovered || wholeGrab.isSelected));
            hintPanel.SetActive(allowed && interacting && Camera.main
                && Vector3.Distance(Camera.main.transform.position, helmet.position) < 3f);
            if (allowed && Camera.main)
            {
                var viewer = Camera.main.transform;
                var camera = Camera.main;
                const float depth = 2f;
                float height = 2f * depth * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
                float width = height * camera.aspect;
                float cardWidth = Mathf.Min(width * .36f, height * .65f);
                hintPanel.transform.localScale = Vector3.one * (cardWidth / 1100f);
                hintPanel.transform.SetPositionAndRotation(viewer.TransformPoint(new Vector3(
                    width * .5f - cardWidth * .5f - width * .03f,
                    height * .5f - height * .06f - (cardWidth * 230f/900f) - .12f, depth)), viewer.rotation);
                actionHint.text = message;
            }
        }
        UpdateStatus();
    }
    void BuildPanel()
    {
        fallback = Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 40);
        panel = new GameObject("Helmet 2 Detail Controls", typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
        panel.hideFlags = HideFlags.HideAndDontSave;
        panel.transform.SetParent(transform, false); panel.transform.localScale = Vector3.one * 0.002f;
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 950);
        Element<Image>("Background", Vector2.zero, new Vector2(1200, 950)).color = new Color(0.025f, 0.09f, 0.12f, 0.97f);
        var introduction = TextLabel("Permanent Introduction", new Vector2(0, 245), new Vector2(1100, 400), 34);
        introduction.alignment = TextAnchor.UpperLeft;
        introduction.text = instructions;
        introduction.raycastTarget = false;
        var viewport = Element<Image>("Details Viewport", new Vector2(-20, -105), new Vector2(1060, 280));
        viewport.color = new Color(0.04f, 0.14f, 0.18f, 1);
        viewport.gameObject.AddComponent<RectMask2D>();
        detailsScroll = viewport.gameObject.AddComponent<ScrollRect>();
        detailsScroll.viewport = viewport.rectTransform;
        detailsScroll.horizontal = false;
        detailsScroll.movementType = ScrollRect.MovementType.Clamped;
        detailsScroll.scrollSensitivity = 45;
        details = TextLabel("Details", Vector2.zero, new Vector2(1020, 280), 30);
        details.transform.SetParent(viewport.transform, false);
        var content = details.rectTransform;
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = new Vector2(-40, 0);
        content.anchoredPosition = Vector2.zero;
        details.horizontalOverflow = HorizontalWrapMode.Wrap;
        details.verticalOverflow = VerticalWrapMode.Overflow;
        details.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        detailsScroll.content = content;
        details.alignment = TextAnchor.UpperLeft;
        details.raycastTarget = true;
        var track = Element<Image>("Details Scrollbar", new Vector2(535, -105), new Vector2(30, 280));
        track.color = new Color(0.1f, 0.25f, 0.28f);
        var thumb = Element<Image>("Scroll Handle", Vector2.zero, Vector2.zero);
        thumb.transform.SetParent(track.transform, false);
        thumb.rectTransform.anchorMin = Vector2.zero;
        thumb.rectTransform.anchorMax = Vector2.one;
        thumb.rectTransform.sizeDelta = Vector2.zero;
        thumb.color = new Color(0.3f, 0.85f, 0.8f);
        var scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = thumb.rectTransform;
        scrollbar.targetGraphic = thumb;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        detailsScroll.verticalScrollbar = scrollbar;
        status = TextLabel("Status", new Vector2(0, -295), new Vector2(1100, 65), 30);
        Button("重新开始 / Restart", 0, ResetExhibit);
        InformationButton("散落零件归位（保留其他进度）", new Vector2(850, -640), RecoverLooseParts);
        checklist = TextLabel("Part checklist", new Vector2(-850, 0), new Vector2(450, 850), 28);
        checklist.alignment = TextAnchor.UpperLeft; checklist.raycastTarget = false;
        var checklistBackground = Element<Image>("Checklist background", new Vector2(-850,0), new Vector2(500,900));
        checklistBackground.color = new Color(.02f,.09f,.12f,.97f); checklistBackground.raycastTarget = false;
        checklistBackground.transform.SetAsFirstSibling();
        hintPanel = new GameObject("Act 2 action hint", typeof(RectTransform), typeof(Canvas), typeof(Image));
        hintPanel.transform.SetParent(transform, false); hintPanel.transform.localScale = Vector3.one * .001f;
        hintPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(1100, 150);
        hintPanel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        hintPanel.GetComponent<Image>().color = new Color(.02f,.09f,.12f,.94f);
        hintPanel.GetComponent<Image>().raycastTarget = false;
        actionHint = TextLabel("Action", Vector2.zero, new Vector2(1060,140), 30);
        actionHint.transform.SetParent(hintPanel.transform, false); actionHint.raycastTarget = false;
        hintPanel.SetActive(false);
        // Information-only alternatives; viewing a description never reports M3 task success.
        for (int i=0;i<descriptions.Length;i++)
        {
            var info=descriptions[i]; if (info==null) continue;
            InformationButton(info.title,new Vector2(850,350-i*105),()=>
            {
                if (!StageCoordinator.Allows(ExperienceStage.ScaleUp)) return;
                ShowDetails(info.title+"\n\n"+info.description+"\n\n"+info.englishDescription);
                SessionTelemetry.Report("InformationViewed",this,info.key);
            });
        }
        InformationButton("文字上移 / Scroll up",new Vector2(850,-440),()=>ScrollInformation(.35f));
        InformationButton("文字下移 / Scroll down",new Vector2(850,-535),()=>ScrollInformation(-.35f));
    }
    void ScrollInformation(float delta)
    {
        if (!StageCoordinator.Allows(ExperienceStage.ScaleUp)) return;
        detailsScroll.StopMovement(); detailsScroll.verticalNormalizedPosition=Mathf.Clamp01(detailsScroll.verticalNormalizedPosition+delta);
    }
    void InformationButton(string caption,Vector2 position,UnityEngine.Events.UnityAction action)
    {
        var image=Element<Image>(caption,position,new Vector2(440,85)); image.color=new Color(.06f,.28f,.32f);
        var button=image.gameObject.AddComponent<AccessibleButton>(); button.targetGraphic=image; button.onClick.AddListener(action);
        var label=TextLabel(caption+" label",Vector2.zero,new Vector2(425,80),27);
        label.text=caption; label.transform.SetParent(image.transform,false); label.raycastTarget=false;
    }
    void ShowDetails(string message)
    {
        details.text = message;
        LayoutRebuilder.ForceRebuildLayoutImmediate(details.rectTransform);
        detailsScroll.StopMovement();
        detailsScroll.verticalNormalizedPosition = 1;
    }
    public static string EnglishDescription(string key)
    {
        switch (key)
        {
            case "forward": return "Traditional copper diving helmets have glass viewports. Examine the frame, opening and connections of the front part, then compare its shape with the side parts.";
            case "left": return "Examine the outline, frame and attachment to the helmet shell. Compare this part with the right viewport.";
            case "right": return "Turn the part to examine its inner and outer surfaces and the connections around its edge. Compare it with the left viewport.";
            case "up": return "Examine the opening, frame and mounting position of the top component. Consider how viewports facing different directions affect the field of view.";
            case "fixing": return "Examine the contact surfaces, holes and outline of the fixing component. This model shows its connection structure; its exact function needs confirmation against the actual helmet model.";
            case "nameplate": return "Look closely for lettering, marks or numbers on the nameplate. These may help identify the equipment. The specific manufacturer of this model has not been confirmed.";
            case "nail1": return "Examine the head, shaft and mounting position of this fastener. Only nail1 can be removed in this exhibit to avoid repetitive interactions; the other nail parts remain fixed.";
            default: return "";
        }
    }
    void Button(string caption, float x, UnityEngine.Events.UnityAction action)
    {
        var image = Element<Image>(caption, new Vector2(x, -390), new Vector2(320, 95));
        image.color = new Color(0.1f, 0.42f, 0.42f);
        var button = image.gameObject.AddComponent<AccessibleButton>(); button.targetGraphic = image;
        button.onClick.AddListener(action);
        var text = TextLabel(caption + " Label", Vector2.zero, new Vector2(310, 85), 34);
        text.text = caption;
        text.transform.SetParent(image.transform, false); text.raycastTarget = false;
    }
    Text TextLabel(string name, Vector2 position, Vector2 size, int fontSize)
    {
        var text = Element<Text>(name, position, size);
        text.font = font ? font : fallback; text.fontSize = fontSize;
        text.color = Color.white; text.alignment = TextAnchor.MiddleCenter;
        text.supportRichText = false; return text;
    }
    T Element<T>(string name, Vector2 position, Vector2 size) where T : Graphic
    {
        var item = new GameObject(name, typeof(RectTransform)); item.transform.SetParent(panel.transform, false);
        item.hideFlags = HideFlags.HideAndDontSave;
        var rect = item.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
        return item.AddComponent<T>();
    }
    void OnDisable()
    {
        if (panel) { if (Application.isPlaying) Destroy(panel); else DestroyImmediate(panel); }
        if (fallback) { if (Application.isPlaying) Destroy(fallback); else DestroyImmediate(fallback); }
        if (hintPanel) { if (Application.isPlaying) Destroy(hintPanel); else DestroyImmediate(hintPanel); }
        foreach (var part in parts)
        {
            if (part.outline) part.outline.enabled = false;
            if (part.socket) part.socket.enabled = false;
            if (part.renderer) part.renderer.SetPropertyBlock(part.original);
        }
        panel = null;
        panelPlaced = false;
        fallback = null;
    }
    void OnDestroy()
    {
        foreach (var part in parts)
        {
            if (!part.grab) continue;
            part.grab.selectEntered.RemoveListener(part.entered.Invoke);
            part.grab.selectExited.RemoveListener(part.exited.Invoke);
            if (part.detached && part.transform) Destroy(part.transform.gameObject);
        }
        foreach (var part in parts) { if (part.outline) Destroy(part.outline.gameObject); if (part.socket) Destroy(part.socket.gameObject); }
        if (outlineMaterial) Destroy(outlineMaterial);
        if (hintPanel) Destroy(hintPanel);
        if (panel) Destroy(panel);
        if (fallback) Destroy(fallback);
    }
}
