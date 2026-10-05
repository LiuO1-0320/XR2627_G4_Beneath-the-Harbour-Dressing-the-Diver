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
public class HelmetDetailInspection : MonoBehaviour
{
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
    [Range(1, 4)] public float maximumMagnification = 3;
    [Range(0.25f, 1)] public float minimumMagnification = 1;
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
        public int releaseFrame = -1;
        public PartDescription info;
        public Action<SelectEnterEventArgs> entered;
        public Action<SelectExitEventArgs> exited;
    }
    readonly List<Part> parts = new List<Part>();
    Vector3 baseScale, basePosition, localCenter;
    Quaternion baseRotation;
    float magnification = 1;
    Text details, status;
    ScrollRect detailsScroll;
    GameObject panel;
    Font fallback;
    XRGrabInteractable wholeGrab;
    XRGeneralGrabTransformer wholeTransformer;
    Material handleMaterial;
    readonly string instructions = "ACT 2 · 头盔细节观察 / Helmet details\n\n观察潜水头盔的视窗、连接件与铭牌。\n握住青色抓取点移动、旋转；双手拉开放大。\n松开整盔后，抓取零件查看详情。\n零件放回原位附近并松手可装回；Reset 恢复展项。";
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
            parts.Add(part);
        }
        SetupWholeGrab(bounds);
        if (!panel) BuildPanel();
        ShowDetails(defaultDetails);
        UpdateStatus();
    }
    void Detach(Part part)
    {
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
        if (wholeGrab)
        {
            float current = Mathf.Abs(helmet.localScale.x / baseScale.x);
            if (!Mathf.Approximately(current, magnification))
            { magnification = current; UpdateStatus(); }
        }
        foreach (var part in parts)
        {
            if (part.releaseFrame < 0 || Time.frameCount < part.releaseFrame) continue;
            part.releaseFrame = -1;
            if (!part.grab.isSelected && Vector3.Distance(part.transform.position, part.parent.TransformPoint(part.position)) <= reattachDistance)
                Restore(part);
        }
    }
    bool Held() { return (wholeGrab && wholeGrab.isSelected) || parts.Exists(p => p.grab.isSelected); }
    void SetupWholeGrab(Bounds bounds)
    {
        var body = helmet.GetComponent<Rigidbody>();
        if (!body) body = helmet.gameObject.AddComponent<Rigidbody>();
        body.useGravity = false; body.isKinematic = true;
        wholeTransformer = helmet.gameObject.AddComponent<XRGeneralGrabTransformer>();
        wholeTransformer.allowOneHandedScaling = false;
        wholeTransformer.allowTwoHandedScaling = true;
        wholeTransformer.allowTwoHandedRotation = XRGeneralGrabTransformer.TwoHandedRotationMode.TwoHandedAverage;
        wholeTransformer.thresholdMoveRatioForScale = 0.03f;
        wholeTransformer.clampScaling = true;
        wholeTransformer.minimumScaleRatio = minimumMagnification;
        wholeTransformer.maximumScaleRatio = maximumMagnification;
        wholeGrab = helmet.gameObject.AddComponent<XRGrabInteractable>();
        // Re-register only after the handle collider list is complete. OnEnable
        // initially sees the imported hierarchy and its separate part colliders.
        wholeGrab.enabled = false;
        wholeGrab.addDefaultGrabTransformers = false;
        wholeGrab.selectMode = InteractableSelectMode.Multiple;
        wholeGrab.useDynamicAttach = true;
        wholeGrab.reinitializeDynamicAttachEverySingleGrab = true;
        wholeGrab.trackScale = true;
        wholeGrab.throwOnDetach = false;
        wholeGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        wholeGrab.AddSingleGrabTransformer(wholeTransformer);
        wholeGrab.AddMultipleGrabTransformer(wholeTransformer);
        wholeGrab.selectFilters.Add(new XRSelectFilterDelegate((interactor, interactable) =>
            !parts.Exists(p => !p.detached && p.grab.isSelected)));
        // Restrict whole-object selection to separate handles. A large root collider
        // would cover detachable windows and compete with their grab interactions.
        wholeGrab.colliders.Clear();
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) shader = Shader.Find("Unlit/Color");
        handleMaterial = new Material(shader);
        handleMaterial.color = new Color(0.1f, 0.95f, 0.85f);
        for (int side = -1; side <= 1; side += 2)
        {
            var handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = side < 0 ? "Left Whole Helmet Grip" : "Right Whole Helmet Grip";
            handle.transform.SetParent(helmet, false);
            handle.transform.position = bounds.center + Vector3.right * side * (bounds.extents.x + 0.12f);
            Vector3 worldScale = helmet.lossyScale;
            handle.transform.localScale = new Vector3(0.12f / Mathf.Abs(worldScale.x),
                0.12f / Mathf.Abs(worldScale.y), 0.12f / Mathf.Abs(worldScale.z));
            handle.GetComponent<Renderer>().sharedMaterial = handleMaterial;
            wholeGrab.colliders.Add(handle.GetComponent<SphereCollider>());
        }
        wholeGrab.enabled = true;
    }
    void Restore(Part part)
    {
        part.body.isKinematic = true;
        part.transform.SetParent(part.parent, false);
        part.transform.localPosition = part.position; part.transform.localRotation = part.rotation; part.transform.localScale = part.scale;
        part.detached = false; part.releaseFrame = -1;
        UpdateStatus();
    }
    public void ChangeScale(float amount)
    {
        if (Held()) { status.text = "请先松开零件，再调整 / Release part first"; return; }
        Vector3 center = helmet.TransformPoint(localCenter);
        magnification = Mathf.Clamp(magnification + amount, minimumMagnification, maximumMagnification);
        helmet.localScale = baseScale * magnification;
        helmet.position += center - helmet.TransformPoint(localCenter);
        UpdateStatus();
    }
    public void Rotate(float angle)
    {
        if (Held()) { status.text = "请先松开零件，再旋转 / Release part first"; return; }
        helmet.RotateAround(helmet.TransformPoint(localCenter), Vector3.up, angle);
        UpdateStatus();
    }
    public void ResetExhibit()
    {
        if (!Application.isPlaying) return;
        if (Held()) { status.text = "请先松开零件，再复位 / Release part first"; return; }
        helmet.localScale = baseScale; helmet.localPosition = basePosition; helmet.localRotation = baseRotation;
        magnification = 1;
        foreach (var part in parts) Restore(part);
        ShowDetails(defaultDetails); UpdateStatus();
    }
    void UpdateStatus()
    {
        if (status) status.text = $"放大 {magnification:0.0}× · 已拆下 {parts.FindAll(p => p.detached).Count}/{parts.Count}";
    }
    void BuildPanel()
    {
        fallback = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 40);
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
        Button("复位 / Reset", 0, ResetExhibit);
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
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
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
        panel = null;
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
        if (panel) Destroy(panel);
        if (fallback) Destroy(fallback);
        if (handleMaterial) Destroy(handleMaterial);
    }
}
