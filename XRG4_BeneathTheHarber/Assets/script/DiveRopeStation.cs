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
public class DiveRopeStation : MonoBehaviour, IResettable
{
    public int ResetOrder => -1000;
    public void PrepareReset() { CancelSessionTravel(); }
    public void RestoreReset() { elapsed=0; travelSession=System.Guid.Empty; ascending=false; headUnderwater=false; currentWater=null; panelFollowBlend=0; clearanceDuration=0;
        if(surfaceProgress) surfaceProgress.fillAmount=0; if(bottomProgress) bottomProgress.fillAmount=0;
        if(surfacePanel) { GazePanelAnchor.Clear(surfacePanel); surfacePanel.gameObject.SetActive(false); }
        if(bottomPanel) { GazePanelAnchor.Clear(bottomPanel); bottomPanel.gameObject.SetActive(false); } }

    public event System.Action DescentCompleted;
    public event System.Action<System.Guid> PlatformReturnCompleted;
    System.Guid travelSession;
    public void CancelSessionTravel() { FinishTravel(false); }
    public void StopAtCurrentPosition() { FinishTravel(false, true); }
    public Transform seabed;
    public Font font;
    [Min(2)] public float travelDuration = 9f;
    public static bool IsPlayerTravelling { get; private set; }
    public bool IsDescending => travelling && !ascending;
    public bool IsVisitorOnSurfacePlatform
    {
        get
        {
            if (travelling || !Camera.main) return false;
            var rig = Camera.main.GetComponentInParent<XROrigin>();
            if (!rig) return false;
            var head = Camera.main.transform.position;
            return Vector2.Distance(new Vector2(head.x, head.z), new Vector2(SurfaceDock.x, SurfaceDock.z)) <= 1.4f
                && Mathf.Abs(rig.transform.position.y - SurfaceDock.y) <= 0.35f;
        }
    }
    public Vector3 SurfaceDock => transform.TransformPoint(new Vector3(-1.5f, 0, 0));
    public Vector3 NpcSurfaceDock => SurfaceDock + transform.forward * 0.65f;
    public Vector3 NpcRopeTop => transform.TransformPoint(new Vector3(-0.45f, 0, 0.3f));
    public Vector3 NpcRopeBottom => NpcRopeTop + Vector3.up * BottomLocalY;
    float BottomLocalY => seabed ? transform.InverseTransformPoint(seabed.TransformPoint(Vector3.up * 0.5f)).y : -6.41f;
    Vector3 BottomDock => transform.TransformPoint(new Vector3(DiveShaftGeometry.AxisX, BottomLocalY, 0));
    GameObject generated;
    Material ropeMaterial, braidMaterial, woodMaterial, metalMaterial;
    Font fallback;
    Transform surfacePanel, bottomPanel;
    Vector3 surfacePanelAnchor, bottomPanelAnchor;
    Text surfaceStatus, bottomStatus;
    Button downButton, upButton;
    XRSimpleInteractable surfaceGrip, bottomGrip;
    XROrigin origin;
    CharacterController controller;
    bool controllerWasEnabled, travelling, ascending;
    float elapsed;
    float clearanceDuration;
    Vector3 clearanceFeet;
    Vector3 startFeet, horizontalHeadOffset;
    readonly List<Behaviour> suspended = new List<Behaviour>();
    WaterVolumeSwimming[] waters;
    WaterVolumeSwimming currentWater;
    bool headUnderwater, overWater;
    Image surfaceProgress, bottomProgress;
    float panelFollowBlend;
    Sprite panelSprite;
    Texture2D panelTexture;
    // Runtime colliders must remain in the physics scene; editor-only previews are disposable.
    HideFlags GeneratedFlags => Application.isPlaying ? HideFlags.None : HideFlags.HideAndDontSave;

    void OnEnable()
    {
        Build();
        if (Application.isPlaying) Application.onBeforeRender += PositionPanels;
    }
    void Update()
    {
        if (!generated) Build();
        if (!Application.isPlaying || !Camera.main) return;
        if (!surfaceGrip) BindGrips();
        UpdateWaterPosition();
        var session = StageCoordinator.Current ? StageCoordinator.Current.Controller.Session : null;
        bool visible = session == null || ((session.Status == SessionStatus.Running || session.Status == SessionStatus.Paused)
            && (session.Stage == ExperienceStage.Descent || session.Stage == ExperienceStage.SeabedCleanup || session.Stage == ExperienceStage.FreeExploration));
        // Keep the journey's panel through the surface crossing, instead of swapping it mid-trip.
        bool showAscent = travelling ? ascending : headUnderwater;
        surfacePanel.gameObject.SetActive(visible && !showAscent);
        bottomPanel.gameObject.SetActive(visible && showAscent);
        bool nearRope = NearRope();
        downButton.interactable = CanDescend && !IsPlayerTravelling && !headUnderwater && nearRope;
        upButton.interactable = CanAscend && !IsPlayerTravelling && headUnderwater && nearRope;
        if (!travelling) SetStatus(!nearRope ? "靠近绳索以启用\nApproach the rope to activate"
            : headUnderwater ? "沿安全通道返回平台\nReturn to the platform" : "沿绳索探索水下\nExplore beneath the harbour");
        if (surfaceProgress) surfaceProgress.fillAmount = travelling ? Mathf.Clamp01(elapsed / (clearanceDuration + Mathf.Max(2, travelDuration) + 1.25f + (ascending ? 2.5f : 0))) : 0;
        if (bottomProgress) bottomProgress.fillAmount = surfaceProgress ? surfaceProgress.fillAmount : 0;
    }
    void UpdateWaterPosition()
    {
        if (waters == null) waters = FindObjectsOfType<WaterVolumeSwimming>(true);
        var head = Camera.main.transform.position;
        WaterVolumeSwimming found = null; overWater = false;
        foreach (var water in waters)
        {
            if (!water) continue;
            overWater |= water.IsOverWater(head);
            if (water.ContainsHead(head, water == currentWater && headUnderwater)) found = water;
        }
        currentWater = found; headUnderwater = found;
    }
    public bool IsInsideShaft(Vector3 worldPoint)
    {
        var local = transform.InverseTransformPoint(worldPoint);
        return DiveShaftGeometry.Contains(local.x, local.z);
    }
    bool NearRope()
    {
        if (!Camera.main) return false;
        var eye = Camera.main.transform.position;
        var local = transform.InverseTransformPoint(eye);
        return DiveShaftGeometry.CanInteract(local.x, local.z)
            && eye.y >= BottomDock.y - .3f && eye.y <= SurfaceDock.y + 2.8f;
    }
    public void Descend() { BeginTravel(false); }
    public void Ascend() { BeginTravel(true); }
    bool CanAscend => StageCoordinator.Allows(ExperienceStage.Descent) || StageCoordinator.Allows(ExperienceStage.SeabedCleanup);
    bool CanDescend => CanAscend;
    void BeginTravel(bool goingUp)
    {
        if (goingUp ? !CanAscend : !CanDescend) return;
        if (!Application.isPlaying || IsPlayerTravelling || !Camera.main) return;
        UpdateWaterPosition();
        if (!NearRope()) return;
        if (goingUp ? !headUnderwater : headUnderwater) return;
        origin = Camera.main.GetComponentInParent<XROrigin>();
        if (!origin) { SetStatus("未找到玩家 / Player unavailable"); return; }
        // Preserve tracking and rotation; suspend only locomotion that would fight the rope journey.
        horizontalHeadOffset = Camera.main.transform.position - origin.transform.position;
        horizontalHeadOffset.y = 0;
        startFeet = origin.transform.position + horizontalHeadOffset;
        // At shallow depth, lower the head below the rim before aligning horizontally.
        // On land, clear the platform top before moving over the covered opening.
        clearanceFeet = startFeet;
        float headHeight = Mathf.Max(.5f, Camera.main.transform.position.y - startFeet.y);
        clearanceFeet.y = goingUp ? Mathf.Min(startFeet.y, SurfaceDock.y - headHeight - .45f)
            : Mathf.Max(startFeet.y, SurfaceDock.y + .1f);
        clearanceDuration = Mathf.Abs(clearanceFeet.y - startFeet.y) > .01f ? .75f : 0;
        suspended.Clear();
        foreach (var provider in origin.GetComponentsInChildren<LocomotionProvider>())
            if (provider.enabled) { suspended.Add(provider); provider.enabled = false; }
        foreach (var bodyTransformer in origin.GetComponentsInChildren<XRBodyTransformer>())
            if (bodyTransformer.enabled) { suspended.Add(bodyTransformer); bodyTransformer.enabled = false; }
        controller = origin.GetComponent<CharacterController>();
        controllerWasEnabled = controller && controller.enabled;
        if (controllerWasEnabled) controller.enabled = false;
        ascending = goingUp; elapsed = 0; travelling = IsPlayerTravelling = true;
        travelSession = StageCoordinator.Current ? StageCoordinator.Current.Controller.Session.SessionId : System.Guid.Empty;
        SessionTelemetry.Report("RopeTravelStarted", this, ascending ? "Ascent" : "Descent");
        SetStatus("正在对齐绳索位置… / Aligning with the rope…");
    }
    void LateUpdate()
    {
        UpdateTravel();
        if (Application.isPlaying && Camera.main)
            panelFollowBlend = Mathf.MoveTowards(panelFollowBlend, travelling || NearRope() ? 1 : 0, Time.unscaledDeltaTime / .35f);
        PositionPanels();
    }
    void UpdateTravel()
    {
        if (ascending ? !CanAscend : !CanDescend) return;
        if (!travelling) return;
        if (!origin) { FinishTravel(false); return; }
        elapsed += Time.deltaTime;
        // Align horizontally at the current depth, then travel vertically inside the opening.
        // Only step onto land once the feet have cleared the platform's upper surface.
        Vector3 first = BottomDock; first.y = clearanceFeet.y;
        Vector3 last = BottomDock;
        if (ascending) last.y = SurfaceDock.y + .1f;
        float alignDuration = 1.25f;
        float exitDuration = ascending ? 2.5f : 0;
        float verticalDuration = Mathf.Max(2, travelDuration);
        float journeyTime = elapsed - clearanceDuration;
        Vector3 feet;
        if (elapsed < clearanceDuration)
            feet = Vector3.Lerp(startFeet, clearanceFeet, Ease(elapsed / clearanceDuration));
        else if (journeyTime < alignDuration)
            feet = Vector3.Lerp(clearanceFeet, first, Ease(journeyTime / alignDuration));
        else if (journeyTime < alignDuration + verticalDuration)
            feet = Vector3.Lerp(first, last, Ease((journeyTime - alignDuration) / verticalDuration));
        else
            feet = ascending ? Vector3.Lerp(last, SurfaceDock,
                Ease((journeyTime - alignDuration - verticalDuration) / exitDuration)) : BottomDock;
        SetStatus(journeyTime < alignDuration ? "正在对齐绳索位置… / Aligning with the rope…"
            : ascending ? "正在上浮返回平台… / Ascending to the platform…" : "正在下潜… / Descending…");
        horizontalHeadOffset = Camera.main.transform.position - origin.transform.position;
        horizontalHeadOffset.y = 0;
        origin.transform.position = feet - horizontalHeadOffset;
        if (journeyTime >= alignDuration + verticalDuration + exitDuration) FinishTravel(true);
    }
    public static float Ease(float value)
    {
        float t = Mathf.Clamp01(value);
        return t * t * t * (t * (6 * t - 15) + 10);
    }
    void FinishTravel(bool completed, bool keepPosition = false)
    {
        if (!travelling) return;
        // If interrupted, place the player on the supported surface dock before restoring gravity.
        SessionTelemetry.Report(completed ? "RopeTravelCompleted" : "RopeTravelCancelled", this, ascending ? "Ascent" : "Descent");
        if (origin && !keepPosition) origin.transform.position = (completed && !ascending ? BottomDock : SurfaceDock) - horizontalHeadOffset;
        if (controller && controllerWasEnabled) controller.enabled = true;
        foreach (var component in suspended) if (component) component.enabled = true;
        suspended.Clear(); travelling = IsPlayerTravelling = false;
        SetStatus(completed && !ascending ? "已到海底，可自由探索 / Explore the seabed" : "已回到平台 / Back on the platform");
        if (completed && !ascending) DescentCompleted?.Invoke();
        if (completed && ascending) PlatformReturnCompleted?.Invoke(travelSession);
    }
    void SetStatus(string message)
    {
        if (surfaceStatus) surfaceStatus.text = message;
        if (bottomStatus) bottomStatus.text = message;
    }
    void PositionPanels()
    {
        if (!Application.isPlaying || !Camera.main || !surfacePanel || !bottomPanel) return;
        PlacePanel(surfacePanel, surfacePanelAnchor);
        PlacePanel(bottomPanel, bottomPanelAnchor);
    }
    void PlacePanel(Transform panel, Vector3 anchor)
    {
        if (!panel || !panel.gameObject.activeSelf) return;
        var viewer = Camera.main.transform;
        // Camera-local pose includes pitch/roll: the panel stays at the same place in
        // the view while travelling, including the headset's final before-render pose.
        float blend = travelling ? 1 : Mathf.SmoothStep(0, 1, panelFollowBlend);
        if (blend >= .99f) { if (GazePanelAnchor.KeepPose(panel)) return; }
        else GazePanelAnchor.Clear(panel);
        panel.SetPositionAndRotation(Vector3.Lerp(generated.transform.TransformPoint(anchor),
            viewer.TransformPoint(new Vector3(.4f, .25f, 1.7f)), blend),
            Quaternion.Slerp(generated.transform.rotation, viewer.rotation, blend));
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
        // Walking onto the opening must never count as permission to dive. The rope
        // journey disables the player's CharacterController, so this support can stay
        // enabled throughout travel, pause, cancellation and reset without a race.
        var support = new GameObject("Shaft opening - walking support", typeof(BoxCollider)) { hideFlags = GeneratedFlags };
        support.transform.SetParent(generated.transform, false);
        var supportCollider = support.GetComponent<BoxCollider>();
        supportCollider.center = new Vector3(0, -.15f, 0);
        supportCollider.size = new Vector3(2, .3f, 1.6f);
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
        BuildPanelSprite();
        surfacePanel = Panel("Surface descent controls", new Vector3(-1.5f, 1.8f, 1.4f), false, out surfaceStatus, out downButton);
        bottomPanel = Panel("Underwater ascent controls", new Vector3(1.6f, bottom + 1.8f, 1.2f), true, out bottomStatus, out upButton);
        surfacePanelAnchor = surfacePanel.localPosition;
        bottomPanelAnchor = bottomPanel.localPosition;
        surfaceProgress = surfacePanel.Find("Journey progress").GetComponent<Image>();
        bottomProgress = bottomPanel.Find("Journey progress").GetComponent<Image>();
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
        Card(item.transform, "Shadow", new Vector2(0,-7), new Vector2(776,426), new Color(0,0,0,.22f));
        Card(item.transform, "Background", Vector2.zero, new Vector2(760,410), new Color(.025f,.075f,.105f,.96f));
        var badge = Label(item.transform, "Section", "BENEATH THE HARBOUR  /  DIVE STATION", new Vector2(0,165), new Vector2(700,35), 19);
        badge.color = new Color(.45f,.85f,.8f);
        Label(item.transform, "Title", underwater ? "返回水面 / Ascend" : "探索水下 / Descend", new Vector2(0,110), new Vector2(700,65), 38);
        status = Label(item.transform, "Hint", "靠近绳索以启用\nApproach the rope to activate", new Vector2(0,20), new Vector2(690,100), 26);
        status.color = new Color(.75f,.84f,.87f);
        var image = Card(item.transform, "Travel button", new Vector2(0, -95), new Vector2(620, 88), Color.white);
        image.raycastTarget = true;
        button = image.gameObject.AddComponent<QuickTravelPressButton>(); button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(.08f,.44f,.44f);
        colors.highlightedColor = new Color(.16f,.66f,.6f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.04f,.3f,.33f);
        colors.disabledColor = new Color(.15f,.23f,.28f,.8f);
        colors.fadeDuration = .15f; button.colors = colors;
        if (underwater) button.onClick.AddListener(Ascend); else button.onClick.AddListener(Descend);
        Label(image.transform, "Button label", underwater ? "↑  上浮返回平台 / Ascend" : "↓  开始下潜 / Descend", Vector2.zero, new Vector2(600, 75), 30);
        Card(item.transform, "Progress track", new Vector2(0,-170), new Vector2(620,7), new Color(.12f,.23f,.27f));
        var fill = Card(item.transform, "Journey progress", new Vector2(0,-170), new Vector2(620,7), new Color(.4f,.87f,.75f));
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 0;
        return item.transform;
    }
    Image Card(Transform parent, string label, Vector2 position, Vector2 size, Color color)
    {
        var image = Graphic<Image>(parent, label, position, size);
        image.sprite = panelSprite; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = false;
        return image;
    }
    void BuildPanelSprite()
    {
        const int size = 64;
        panelTexture = new Texture2D(size,size,TextureFormat.RGBA32,false) { hideFlags = GeneratedFlags, wrapMode = TextureWrapMode.Clamp };
        for (int y=0; y<size; y++) for (int x=0; x<size; x++)
        {
            float dx = Mathf.Max(Mathf.Abs(x-31.5f)-19.5f,0), dy = Mathf.Max(Mathf.Abs(y-31.5f)-19.5f,0);
            panelTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(12-Mathf.Sqrt(dx*dx+dy*dy))));
        }
        panelTexture.Apply();
        panelSprite = Sprite.Create(panelTexture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(13,13,13,13));
        panelSprite.hideFlags = GeneratedFlags;
    }
    void OnDrawGizmosSelected()
    {
        var previous = Gizmos.matrix; Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(.2f,.9f,.7f,.7f);
        Gizmos.DrawWireCube(new Vector3(0,BottomLocalY/2,0), new Vector3(DiveShaftGeometry.HalfX*2,-BottomLocalY,DiveShaftGeometry.HalfZ*2));
        Gizmos.color = new Color(.35f,.65f,1,.65f);
        Gizmos.DrawWireCube(new Vector3(0,BottomLocalY/2,0), new Vector3(DiveShaftGeometry.InteractionHalfX*2,-BottomLocalY,DiveShaftGeometry.InteractionHalfZ*2));
        Gizmos.matrix = previous;
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
        Application.onBeforeRender -= PositionPanels;
        FinishTravel(false);
        if (surfaceGrip) surfaceGrip.selectEntered.RemoveListener(OnSurfaceGrip);
        if (bottomGrip) bottomGrip.selectEntered.RemoveListener(OnBottomGrip);
        Dispose(generated); Dispose(ropeMaterial); Dispose(braidMaterial); Dispose(woodMaterial); Dispose(metalMaterial); Dispose(fallback);
        Dispose(panelSprite); Dispose(panelTexture); panelFollowBlend = 0;
        generated = null; surfaceGrip = bottomGrip = null;
    }
    static void Dispose(Object item)
    {
        if (!item) return;
        if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
    }
}
