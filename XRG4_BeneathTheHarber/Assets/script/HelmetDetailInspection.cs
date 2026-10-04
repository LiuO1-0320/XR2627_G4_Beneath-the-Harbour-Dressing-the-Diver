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
public class HelmetDetailInspection : MonoBehaviour
{
    [Serializable] public class PartDescription
    {
        public string key, title;
        [TextArea(3, 8)] public string description;
        public PartDescription(string k, string t, string d) { key = k; title = t; description = d; }
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
    GameObject panel;
    Font fallback;
    XRGrabInteractable wholeGrab;
    XRGeneralGrabTransformer wholeTransformer;
    Material handleMaterial;
    readonly string instructions = "头盔细节观察 / Helmet details\n\n靠近两侧青色抓取点，按住手柄握把抓住头盔。\n单手移动、旋转；双手各抓一侧，拉开放大、聚拢缩小。\n松开后保留当前位置和大小，再抓取可继续调整。\n\n先松开整盔，再抓住零件拆下，查看介绍。\n移回原位附近并松手可装回；复位恢复全部零件。\n可拆：前、左、右、顶部、固定件、铭牌、nail1。";

    void Start()
    {
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
        BuildPanel();
        details.text = instructions;
        UpdateStatus();
    }
    void Detach(Part part)
    {
        part.releaseFrame = -1; part.detached = true;
        part.transform.SetParent(null, true);
        details.text = part.info.title + "\n\n" + part.info.description + "\n\n旋转手柄观察零件。\n放回原位附近并松手可装回。";
        UpdateStatus();
    }
    void LateUpdate()
    {
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
        if (Held()) { status.text = "请先松开零件，再复位 / Release part first"; return; }
        helmet.localScale = baseScale; helmet.localPosition = basePosition; helmet.localRotation = baseRotation;
        magnification = 1;
        foreach (var part in parts) Restore(part);
        details.text = instructions; UpdateStatus();
    }
    void UpdateStatus()
    {
        if (status) status.text = $"放大 {magnification:0.0}× · 已拆下 {parts.FindAll(p => p.detached).Count}/{parts.Count}";
    }
    void BuildPanel()
    {
        fallback = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 40);
        panel = new GameObject("Helmet 2 Detail Controls", typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
        panel.transform.SetParent(transform, false); panel.transform.localScale = Vector3.one * 0.002f;
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 950);
        Element<Image>("Background", Vector2.zero, new Vector2(1200, 950)).color = new Color(0.025f, 0.09f, 0.12f, 0.97f);
        details = TextLabel("Details", new Vector2(0, 85), new Vector2(1100, 670), 34);
        details.alignment = TextAnchor.UpperLeft;
        status = TextLabel("Status", new Vector2(0, -295), new Vector2(1100, 65), 30);
        Button("复位 / Reset", 0, ResetExhibit);
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
        var rect = item.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
        return item.AddComponent<T>();
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
