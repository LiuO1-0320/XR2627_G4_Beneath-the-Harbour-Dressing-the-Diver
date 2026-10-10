using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Authored rope station with reversible, player-initiated vertical travel.</summary>
[ExecuteAlways]
public class DiveRopeStation : MonoBehaviour
{
    public Transform seabed;
    public Font font;
    [Min(2)] public float travelDuration = 9f;
    [Min(0.5f)] public float interactionDistance = 3.5f;
    public static bool IsPlayerTravelling { get; private set; }
    public Vector3 SurfaceDock => transform.TransformPoint(new Vector3(-1.5f, 0, 0));
    public Vector3 NpcSurfaceDock => SurfaceDock + transform.forward * 0.65f;
    public Vector3 NpcRopeTop => transform.TransformPoint(new Vector3(-0.45f, 0, 0.3f));
    public Vector3 NpcRopeBottom => NpcRopeTop + Vector3.up * BottomLocalY;
    float BottomLocalY => seabed ? transform.InverseTransformPoint(seabed.TransformPoint(Vector3.up * 0.5f)).y : -6.41f;
    Vector3 BottomDock => transform.TransformPoint(new Vector3(0.55f, BottomLocalY, 0));
    GameObject generated;
    Material ropeMaterial, braidMaterial, woodMaterial, metalMaterial;
    Font fallback;
    Transform surfacePanel, bottomPanel;
    Text surfaceStatus, bottomStatus;
    Button downButton, upButton;
    XRSimpleInteractable surfaceGrip, bottomGrip;
    XROrigin origin;
    CharacterController controller;
    bool controllerWasEnabled, travelling, ascending;
    float elapsed;
    Vector3 startFeet, horizontalHeadOffset;
    readonly List<Behaviour> suspended = new List<Behaviour>();
    // Runtime colliders must remain in the physics scene; editor-only previews are disposable.
    HideFlags GeneratedFlags => Application.isPlaying ? HideFlags.None : HideFlags.HideAndDontSave;

    void OnEnable() { Build(); }
    void Update()
    {
        if (!generated) Build();
        if (!Application.isPlaying || !Camera.main) return;
        if (!surfaceGrip) BindGrips();
        FaceViewer(surfacePanel); FaceViewer(bottomPanel);
        downButton.interactable = !IsPlayerTravelling && Nearby(SurfaceDock);
        upButton.interactable = !IsPlayerTravelling && Nearby(BottomDock);
    }
    bool Nearby(Vector3 feet)
    {
        Vector3 eye = Camera.main.transform.position;
        return Vector2.Distance(new Vector2(eye.x, eye.z), new Vector2(feet.x, feet.z)) <= interactionDistance &&
            eye.y >= feet.y - 0.3f && eye.y <= feet.y + 2.8f;
    }
    public void Descend() { BeginTravel(false); }
    public void Ascend() { BeginTravel(true); }
    void BeginTravel(bool goingUp)
    {
        if (!Application.isPlaying || IsPlayerTravelling || !Camera.main) return;
        if (!Nearby(goingUp ? BottomDock : SurfaceDock)) return;
        origin = Camera.main.GetComponentInParent<XROrigin>();
        if (!origin) { SetStatus("未找到玩家 / Player unavailable"); return; }
        // Preserve tracking and rotation; suspend only locomotion that would fight the rope journey.
        horizontalHeadOffset = Camera.main.transform.position - origin.transform.position;
        horizontalHeadOffset.y = 0;
        startFeet = origin.transform.position + horizontalHeadOffset;
        suspended.Clear();
        foreach (var provider in origin.GetComponentsInChildren<LocomotionProvider>())
            if (provider.enabled) { suspended.Add(provider); provider.enabled = false; }
        foreach (var bodyTransformer in origin.GetComponentsInChildren<XRBodyTransformer>())
            if (bodyTransformer.enabled) { suspended.Add(bodyTransformer); bodyTransformer.enabled = false; }
        controller = origin.GetComponent<CharacterController>();
        controllerWasEnabled = controller && controller.enabled;
        if (controllerWasEnabled) controller.enabled = false;
        ascending = goingUp; elapsed = 0; travelling = IsPlayerTravelling = true;
        SetStatus(ascending ? "正在上浮… / Ascending…" : "沿绳下潜中… / Descending…");
    }
    void LateUpdate()
    {
        if (!travelling) return;
        if (!origin) { FinishTravel(false); return; }
        elapsed += Time.deltaTime;
        Vector3 ropeSurface = BottomDock; ropeSurface.y = SurfaceDock.y;
        Vector3 first = ascending ? BottomDock : ropeSurface;
        Vector3 last = ascending ? ropeSurface : BottomDock;
        float alignDuration = ascending ? 0.75f : 2.5f;
        float exitDuration = ascending ? 2.5f : 0;
        float verticalDuration = Mathf.Max(2, travelDuration);
        Vector3 feet;
        if (elapsed < alignDuration)
            feet = Vector3.Lerp(startFeet, first, Ease(elapsed / alignDuration));
        else if (elapsed < alignDuration + verticalDuration)
            feet = Vector3.Lerp(first, last, Ease((elapsed - alignDuration) / verticalDuration));
        else
            feet = ascending ? Vector3.Lerp(last, SurfaceDock,
                Ease((elapsed - alignDuration - verticalDuration) / exitDuration)) : BottomDock;
        origin.transform.position = feet - horizontalHeadOffset;
        if (elapsed >= alignDuration + verticalDuration + exitDuration) FinishTravel(true);
    }
    public static float Ease(float value)
    {
        float t = Mathf.Clamp01(value);
        return t * t * t * (t * (6 * t - 15) + 10);
    }
    void FinishTravel(bool completed)
    {
        if (!travelling) return;
        // If interrupted, place the player on the supported surface dock before restoring gravity.
        if (origin) origin.transform.position = (completed && !ascending ? BottomDock : SurfaceDock) - horizontalHeadOffset;
        if (controller && controllerWasEnabled) controller.enabled = true;
        foreach (var component in suspended) if (component) component.enabled = true;
        suspended.Clear(); travelling = IsPlayerTravelling = false;
        SetStatus(completed && !ascending ? "已到海底，可自由探索 / Explore the seabed" : "已回到平台 / Back on the platform");
    }
    void SetStatus(string message)
    {
        if (surfaceStatus) surfaceStatus.text = message;
        if (bottomStatus) bottomStatus.text = message;
    }
    void FaceViewer(Transform panel)
    {
        Vector3 facing = panel.position - Camera.main.transform.position;
        facing.y = 0;
        if (facing.sqrMagnitude > 0.001f) panel.rotation = Quaternion.LookRotation(facing);
    }
    void Build()
    {
        if (generated) return;
        generated = new GameObject("Dive rope and landing docks (generated)") { hideFlags = GeneratedFlags };
        generated.transform.SetParent(transform, false);
        ropeMaterial = Material(new Color(0.58f, 0.4f, 0.17f));
        braidMaterial = Material(new Color(0.84f, 0.67f, 0.32f));
        woodMaterial = Material(new Color(0.22f, 0.28f, 0.25f));
        metalMaterial = Material(new Color(0.12f, 0.38f, 0.42f));
        float bottom = BottomLocalY;
        // Four slabs form a solid dock with an open shaft around the rope.
        Shape("Platform bridge", PrimitiveType.Cube, new Vector3(-2.1f, -0.15f, 0), new Vector3(2.2f, 0.3f, 3), woodMaterial, true);
        Shape("Outer shaft rim", PrimitiveType.Cube, new Vector3(1.3f, -0.15f, 0), new Vector3(0.6f, 0.3f, 3), woodMaterial, true);
        for (int side = -1; side <= 1; side += 2)
            Shape("Shaft side rim", PrimitiveType.Cube, new Vector3(0, -0.15f, side * 1.15f), new Vector3(2, 0.3f, 0.7f), woodMaterial, true);
        Shape("Rope support post", PrimitiveType.Cylinder, new Vector3(-0.7f, 1.1f, 1.05f), new Vector3(0.13f, 1.1f, 0.13f), metalMaterial);
        Shape("Rope support beam", PrimitiveType.Cube, new Vector3(-0.35f, 2.2f, 0.5f), new Vector3(0.95f, 0.1f, 1.2f), metalMaterial);
        Shape("Descent rope", PrimitiveType.Cylinder, new Vector3(0, (2.2f + bottom) / 2, 0), new Vector3(0.065f, (2.2f - bottom) / 2, 0.065f), ropeMaterial);
        for (int strand = 0; strand < 2; strand++)
        {
            var braid = new GameObject("Braided rope strand") { hideFlags = GeneratedFlags };
            braid.transform.SetParent(generated.transform, false);
            var line = braid.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.sharedMaterial = braidMaterial; line.widthMultiplier = 0.012f;
            line.positionCount = 260; line.numCapVertices = 2;
            for (int i = 0; i < line.positionCount; i++)
            {
                float fraction = i / (float)(line.positionCount - 1);
                float angle = fraction * 70 * Mathf.PI + strand * Mathf.PI;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.033f, Mathf.Lerp(bottom, 2.2f, fraction), Mathf.Sin(angle) * 0.033f));
            }
        }
        Shape("Seabed rope weight", PrimitiveType.Cylinder, new Vector3(0, bottom + 0.1f, 0), new Vector3(0.45f, 0.1f, 0.45f), metalMaterial);
        Shape("Surface rope grip", PrimitiveType.Capsule, new Vector3(0, 1, 0), new Vector3(0.22f, 0.18f, 0.22f), metalMaterial, true);
        Shape("Seabed rope grip", PrimitiveType.Capsule, new Vector3(0, bottom + 1, 0), new Vector3(0.22f, 0.18f, 0.22f), metalMaterial, true);
        fallback = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 32);
        surfacePanel = Panel("Surface descent controls", new Vector3(-1.5f, 1.8f, 1.4f), false, out surfaceStatus, out downButton);
        bottomPanel = Panel("Underwater ascent controls", new Vector3(1.6f, bottom + 1.8f, 1.2f), true, out bottomStatus, out upButton);
        if (Application.isPlaying) BindGrips();
    }
    void BindGrips()
    {
        if (surfaceGrip) return;
        surfaceGrip = generated.transform.Find("Surface rope grip").gameObject.AddComponent<XRSimpleInteractable>();
        bottomGrip = generated.transform.Find("Seabed rope grip").gameObject.AddComponent<XRSimpleInteractable>();
        surfaceGrip.selectEntered.AddListener(OnSurfaceGrip);
        bottomGrip.selectEntered.AddListener(OnBottomGrip);
    }
    void OnSurfaceGrip(SelectEnterEventArgs args) { Descend(); }
    void OnBottomGrip(SelectEnterEventArgs args) { Ascend(); }
    Transform Panel(string name, Vector3 position, bool underwater, out Text status, out Button button)
    {
        var item = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster)) { hideFlags = GeneratedFlags };
        item.transform.SetParent(generated.transform, false); item.transform.localPosition = position;
        item.transform.localScale = Vector3.one * 0.0012f;
        item.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        item.GetComponent<RectTransform>().sizeDelta = new Vector2(760, 410);
        Graphic<Image>(item.transform, "Background", Vector2.zero, new Vector2(760, 410)).color = new Color(0.02f, 0.1f, 0.14f, 0.65f);
        Label(item.transform, "Title", underwater ? "沿绳上浮 / Rope ascent" : "沿绳下潜 / Rope descent", new Vector2(0, 135), new Vector2(720, 60), 34);
        status = Label(item.transform, "Hint", "射线点按钮，或握住绳索手柄\nPoint + trigger, or grip the rope handle", new Vector2(0, 35), new Vector2(700, 130), 28);
        var image = Graphic<Image>(item.transform, "Travel button", new Vector2(0, -125), new Vector2(600, 85));
        image.color = new Color(0.1f, 0.42f, 0.42f, 0.85f);
        button = image.gameObject.AddComponent<QuickTravelPressButton>(); button.targetGraphic = image;
        if (underwater) button.onClick.AddListener(Ascend); else button.onClick.AddListener(Descend);
        Label(image.transform, "Button label", underwater ? "上浮 / Ascend" : "下潜 / Descend", Vector2.zero, new Vector2(580, 75), 32);
        return item.transform;
    }
    T Graphic<T>(Transform parent, string name, Vector2 position, Vector2 size) where T : UnityEngine.UI.Graphic
    {
        var item = new GameObject(name, typeof(RectTransform)) { hideFlags = GeneratedFlags };
        item.transform.SetParent(parent, false);
        var rect = item.GetComponent<RectTransform>(); rect.sizeDelta = size; rect.anchoredPosition = position;
        return item.AddComponent<T>();
    }
    Text Label(Transform parent, string name, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var label = Graphic<Text>(parent, name, position, size);
        label.font = font ? font : fallback; label.text = text; label.fontSize = fontSize;
        label.color = Color.white; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        return label;
    }
    GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool solid = false)
    {
        var item = GameObject.CreatePrimitive(type); item.name = name; item.hideFlags = GeneratedFlags;
        item.transform.SetParent(generated.transform, false); item.transform.localPosition = position; item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material; item.GetComponent<Collider>().enabled = solid;
        return item;
    }
    Material Material(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        return new Material(shader) { color = color, hideFlags = HideFlags.HideAndDontSave };
    }
    void OnDisable()
    {
        FinishTravel(false);
        if (surfaceGrip) surfaceGrip.selectEntered.RemoveListener(OnSurfaceGrip);
        if (bottomGrip) bottomGrip.selectEntered.RemoveListener(OnBottomGrip);
        Dispose(generated); Dispose(ropeMaterial); Dispose(braidMaterial); Dispose(woodMaterial); Dispose(metalMaterial); Dispose(fallback);
        generated = null; surfaceGrip = bottomGrip = null;
    }
    static void Dispose(Object item)
    {
        if (!item) return;
        if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
    }
}
