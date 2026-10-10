using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Act 3 diver, controller-operated dialogue and helmet dressing station.</summary>
[ExecuteAlways]
public class DiverNpcInteraction : MonoBehaviour
{
    public Transform helmet;
    public Font font;
    public Transform instructionAnchor;
    [Tooltip("Speech bubble position relative to the diver, in metres.")]
    public Vector3 dialogueOffset = new Vector3(1.25f, 2.05f, 0.05f);
    [Range(0.2f, 1f)] public float dialogueBackgroundOpacity = 0.65f;
    [Min(0.0001f)] public float dialogueScale = 0.00105f;
    public Vector3 helmetEulerOffset = Vector3.zero;
    [Min(0.1f)] public float wearDistance = 0.4f;
    public Transform act4Destination;
    public DiveRopeStation diveRope;
    [Tooltip("Optional clear walking route, in order, before the Act 4 arrival point.")]
    public Transform[] escortWaypoints = new Transform[0];
    [Min(0.5f)] public float greetingDistance = 3.5f;
    [Min(0.1f)] public float walkSpeed = 1.15f;
    [Min(1f)] public float waitForPlayerDistance = 4.5f;
    public Vector3 act4ArrivalOffset = new Vector3(-3, 0, -3);
    [Tooltip("Step away from the helmet stand in the diver's starting forward direction before leading the player.")]
    public float departureDistance = 2.2f;
    GameObject visuals;
    Transform head, dialoguePanel, instructionPanel, uiParent;
    Text dialogue, nextCaption;
    Image dialogueBackground;
    DiverSpeechBubbleTail speechTail;
    Font fallback;
    Material suit, skin, boots;
    Material brass, white, hair;
    Transform bodyRoot, headPivot, leftArm, rightArm, leftElbow, rightElbow, leftLeg, rightLeg, mouth;
    enum Behaviour { Idle, Greeting, Talking, Thanking, Inviting, Leading, RopeBriefing, Descending, Arrived }
    Behaviour behaviour;
    float stateTime, gestureTime, stride;
    bool greeted, waitingForPlayer;
    int routeIndex;
    Vector3 homePosition;
    Quaternion homeRotation;
    Vector3[] route;
    Vector3 diveStart;
    DivingHelmetWearable wearable;
    int page;
    bool previouslyWorn;
    readonly string[] lines = {
        "你好，我是这里的潜水员。准备下潜前，请帮我检查并戴好头盔。\n\nHello! I am the diver at this station. Help me check and put on my helmet before the dive.",
        "先观察头盔的视窗和连接结构。清晰的视野与牢固的连接对潜水员很重要。\n\nLook at the helmet's viewports and connections. A clear view and secure connections are important to a diver.",
        "请按住握把抓起展台上的 helmet3，移到我的头部附近，再松开握把。\n\nHold the grip to pick up helmet3 from the stand. Bring it close to my head, then release the grip.",
        "谢谢你帮我戴好头盔！现在准备好了。跟我来，我们一起前往 ACT 4。\n\nThank you for fitting my helmet! I'm ready now. Follow me to ACT 4."
    };

    void OnEnable()
    {
        homePosition = transform.position; homeRotation = transform.rotation;
        behaviour = Behaviour.Idle; greeted = previouslyWorn = false;
        stateTime = gestureTime = stride = 0;
        Build(); SetupWearable();
    }
    void SetupWearable()
    {
        if (!Application.isPlaying || wearable) return;
        if (!helmet) { Debug.LogError("Assign helmet3 to the Act 3 diver.", this); return; }
        wearable = gameObject.AddComponent<DivingHelmetWearable>();
        wearable.helmet = helmet;
        wearable.head = head;
        wearable.wearDistance = wearDistance;
        wearable.wornCenterOffset = new Vector3(0, 0.02f, 0);
        wearable.wornEulerOffset = helmetEulerOffset;
        wearable.hideShellWhileWorn = false;
        wearable.returnWithSecondaryButton = false;
    }
    void Update()
    {
        if (!visuals) Build();
        if (Application.isPlaying && !wearable && helmet) SetupWearable();
        if (Camera.main)
        {
            FaceViewer(instructionPanel, Camera.main.transform);
        }
        if (!Application.isPlaying) return;
        if (wearable && wearable.IsWorn != previouslyWorn)
        {
            previouslyWorn = wearable.IsWorn;
            page = wearable.IsWorn ? 3 : 2;
            RefreshDialogue();
            if (wearable.IsWorn) { SetBehaviour(Behaviour.Thanking); ShowDialoguePopup(); }
            else { RestoreStation(); }
        }
        UpdateBehaviour();
    }
    static void FaceViewer(Transform panel, Transform viewer)
    {
        Vector3 facing = panel.position - viewer.position;
        facing.y = 0;
        if (facing.sqrMagnitude > 0.001f) panel.rotation = Quaternion.LookRotation(facing);
    }
    public void OpenDialogue()
    {
        if (!Application.isPlaying) return;
        page = wearable && wearable.IsWorn ? 3 : 0;
        RefreshDialogue();
        ShowDialoguePopup();
        greeted = true;
        if (behaviour == Behaviour.Idle || behaviour == Behaviour.Talking) SetBehaviour(Behaviour.Greeting);
    }
    void ShowDialoguePopup()
    {
        dialoguePanel.gameObject.SetActive(true);
        PositionSpeechBubble();
    }
    void LateUpdate()
    {
        if (dialoguePanel && dialoguePanel.gameObject.activeSelf) PositionSpeechBubble();
    }
    void PositionSpeechBubble()
    {
        // The diver supplies the position; the viewer only supplies the readable orientation.
        dialoguePanel.position = transform.TransformPoint(dialogueOffset);
        dialoguePanel.localScale = Vector3.one * dialogueScale;
        if (Camera.main) FaceViewer(dialoguePanel, Camera.main.transform);
        var color = new Color(0.025f, 0.09f, 0.12f, dialogueBackgroundOpacity);
        dialogueBackground.color = color;
        speechTail.color = color;
        Vector3 speaker = dialoguePanel.InverseTransformPoint(head.position);
        Vector2 direction = new Vector2(speaker.x, speaker.y);
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.down;
        // Intersect the bubble boundary, then draw a small tail toward the speaker.
        float edgeDistance = 1f / Mathf.Max(Mathf.Abs(direction.x) / 650f, Mathf.Abs(direction.y) / 360f);
        Vector2 edge = direction * edgeDistance;
        speechTail.SetTail(edge, edge + direction.normalized * 150f);
    }
    public void CloseDialogue()
    {
        if (dialoguePanel) dialoguePanel.gameObject.SetActive(false);
        if (behaviour == Behaviour.Talking) SetBehaviour(Behaviour.Idle);
    }
    public void NextDialogue()
    {
        if (!Application.isPlaying) return;
        if (wearable && wearable.IsWorn) { CloseDialogue(); return; }
        page = (page + 1) % 3;
        gestureTime = 0;
        if (behaviour == Behaviour.Idle) SetBehaviour(Behaviour.Talking);
        RefreshDialogue();
    }
    public void ResetActivity()
    {
        if (!Application.isPlaying || !wearable) return;
        wearable.ReturnToStand();
        // A held helmet cannot be reset; keep its current dialogue until released.
        if (wearable.IsWorn) return;
        previouslyWorn = false;
        RestoreStation();
        page = 0;
        RefreshDialogue();
        CloseDialogue();
    }
    void RefreshDialogue()
    {
        if (dialogue) dialogue.text = lines[page];
        if (nextCaption) nextCaption.text = page == 3 ? "跟上潜水员 / Follow" : "下一句 / Next";
    }
    void Build()
    {
        if (visuals) return;
        visuals = new GameObject("Diver NPC (generated)");
        visuals.hideFlags = HideFlags.HideAndDontSave;
        visuals.transform.SetParent(transform, false);
        suit = MakeMaterial(new Color(0.13f, 0.24f, 0.23f));
        skin = MakeMaterial(new Color(0.72f, 0.49f, 0.32f));
        boots = MakeMaterial(new Color(0.09f, 0.10f, 0.10f));
        brass = MakeMaterial(new Color(0.66f, 0.43f, 0.18f));
        if (brass.HasProperty("_Metallic")) brass.SetFloat("_Metallic", 0.7f);
        white = MakeMaterial(new Color(0.92f, 0.9f, 0.8f));
        hair = MakeMaterial(new Color(0.12f, 0.065f, 0.035f));
        BuildDiver();
        fallback = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 36);
        instructionPanel = CreatePanel("Act 3 Instructions (generated)", instructionAnchor ? instructionAnchor : transform,
            instructionAnchor ? Vector3.zero : new Vector3(-2.5f, 1.6f, 0), new Vector2(1300, 620));
        uiParent = instructionPanel;
        Element<Image>("Background", Vector2.zero, new Vector2(1300, 620)).color = new Color(0.025f, 0.09f, 0.12f, 0.97f);
        Label("Title", "ACT 3 · 为潜水员戴头盔 / Dress the diver", new Vector2(0, 235), new Vector2(1200, 80), 38);
        Label("Instructions", "靠近潜水员，他会挥手向你打招呼。也可以按“开始对话”。\n握住展台上的头盔，移到他头部附近松手。\n戴好后，他会点头感谢，带你前往 ACT 4。\n\nApproach the diver to say hello. Grip and fit his helmet.\nHe will thank you and lead the way to ACT 4.",
            new Vector2(0, 20), new Vector2(1180, 300), 32);
        MakeButton("开始对话 / Talk", new Vector2(-280, -225), OpenDialogue);
        MakeButton("重置 / Reset", new Vector2(280, -225), ResetActivity);

        dialoguePanel = CreatePanel("Diver Speech Bubble (generated)", transform, dialogueOffset, new Vector2(1300, 720));
        dialoguePanel.localScale = Vector3.one * dialogueScale;
        uiParent = dialoguePanel;
        speechTail = Element<DiverSpeechBubbleTail>("Tail toward diver", Vector2.zero, new Vector2(1300, 720));
        speechTail.raycastTarget = false;
        dialogueBackground = Element<Image>("Background", Vector2.zero, new Vector2(1300, 720));
        dialogueBackground.color = new Color(0.025f, 0.09f, 0.12f, dialogueBackgroundOpacity);
        Label("Title", "潜水员说 / Diver says", new Vector2(0, 280), new Vector2(1200, 80), 42);
        dialogue = Label("Dialogue", "", new Vector2(0, 30), new Vector2(1180, 380), 34);
        dialogue.alignment = TextAnchor.UpperLeft;
        nextCaption = MakeButton("下一句 / Next", new Vector2(-280, -270), NextDialogue);
        MakeButton("关闭 / Close", new Vector2(280, -270), CloseDialogue);
        RefreshDialogue();
        CloseDialogue();
    }
    Transform Pivot(string name, Transform parent, Vector3 position)
    {
        var item = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        return item.transform;
    }
    Transform Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var part = Shape(name, type, Vector3.zero, scale, material);
        part.SetParent(parent, false);
        part.localPosition = position;
        return part;
    }
    void BuildDiver()
    {
        bodyRoot = Pivot("Animated diver", visuals.transform, Vector3.zero);
        Part("Canvas diving suit", PrimitiveType.Capsule, bodyRoot, new Vector3(0, 1.08f, 0), new Vector3(0.53f, 0.34f, 0.34f), suit);
        Part("Suit waist", PrimitiveType.Sphere, bodyRoot, new Vector3(0, 0.84f, 0), new Vector3(0.46f, 0.3f, 0.32f), suit);
        Part("Brass helmet collar", PrimitiveType.Cylinder, bodyRoot, new Vector3(0, 1.43f, 0), new Vector3(0.4f, 0.035f, 0.38f), brass);
        Part("Neck", PrimitiveType.Cylinder, bodyRoot, new Vector3(0, 1.49f, 0), new Vector3(0.12f, 0.05f, 0.12f), skin);
        headPivot = Pivot("Head nod joint", bodyRoot, new Vector3(0, 1.49f, 0));
        // Keep the helmet's original fit centre; facial features are nested under the nod joint.
        head = Pivot("Helmet fit anchor", headPivot, new Vector3(0, 0.16f, 0));
        Part("Face", PrimitiveType.Sphere, head, Vector3.zero, new Vector3(0.25f, 0.3f, 0.25f), skin);
        Part("Hair", PrimitiveType.Sphere, head, new Vector3(0, 0.09f, -0.022f), new Vector3(0.255f, 0.15f, 0.23f), hair);
        Part("Nose bridge", PrimitiveType.Sphere, head, new Vector3(0, 0.01f, 0.123f), new Vector3(0.043f, 0.074f, 0.045f), skin);
        Part("Nose tip", PrimitiveType.Sphere, head, new Vector3(0, -0.007f, 0.145f), new Vector3(0.052f, 0.038f, 0.047f), skin);
        mouth = Part("Mouth", PrimitiveType.Sphere, head, new Vector3(0, -0.065f, 0.109f), new Vector3(0.067f, 0.013f, 0.012f), hair);
        for (int side = -1; side <= 1; side += 2)
        {
            Part("Ear", PrimitiveType.Sphere, head, new Vector3(side * 0.122f, -0.005f, 0), new Vector3(0.04f, 0.074f, 0.043f), skin);
            Part("Eye white", PrimitiveType.Sphere, head, new Vector3(side * 0.047f, 0.033f, 0.11f), new Vector3(0.045f, 0.031f, 0.022f), white);
            Part("Pupil", PrimitiveType.Sphere, head, new Vector3(side * 0.047f, 0.033f, 0.123f), new Vector3(0.018f, 0.022f, 0.009f), boots);
            Part("Eyebrow", PrimitiveType.Cube, head, new Vector3(side * 0.047f, 0.057f, 0.11f), new Vector3(0.051f, 0.01f, 0.012f), hair);
            Part("Shoulder harness", PrimitiveType.Cube, bodyRoot, new Vector3(side * 0.155f, 1.15f, 0.172f), new Vector3(0.055f, 0.44f, 0.025f), boots);
            Part("Harness buckle", PrimitiveType.Cube, bodyRoot, new Vector3(side * 0.155f, 1.18f, 0.192f), new Vector3(0.075f, 0.065f, 0.02f), brass);
            Part("Belt weight", PrimitiveType.Cube, bodyRoot, new Vector3(side * 0.15f, 0.86f, 0.16f), new Vector3(0.12f, 0.12f, 0.07f), boots);
            var arm = Pivot(side < 0 ? "Left shoulder" : "Right shoulder", bodyRoot, new Vector3(side * 0.31f, 1.34f, 0));
            Part("Upper sleeve", PrimitiveType.Capsule, arm, new Vector3(0, -0.17f, 0), new Vector3(0.17f, 0.17f, 0.18f), suit);
            var elbow = Pivot("Elbow", arm, new Vector3(0, -0.32f, 0));
            Part("Forearm sleeve", PrimitiveType.Capsule, elbow, new Vector3(0, -0.13f, 0), new Vector3(0.15f, 0.135f, 0.16f), suit);
            Part("Copper cuff", PrimitiveType.Cylinder, elbow, new Vector3(0, -0.245f, 0), new Vector3(0.16f, 0.025f, 0.16f), brass);
            Part("Gloved hand", PrimitiveType.Sphere, elbow, new Vector3(0, -0.31f, 0.015f), new Vector3(0.15f, 0.18f, 0.11f), boots);
            Part("Glove thumb", PrimitiveType.Sphere, elbow, new Vector3(-side * 0.068f, -0.29f, 0.035f), new Vector3(0.06f, 0.09f, 0.06f), boots);
            var leg = Pivot(side < 0 ? "Left hip" : "Right hip", bodyRoot, new Vector3(side * 0.14f, 0.82f, 0));
            Part("Suit leg", PrimitiveType.Capsule, leg, new Vector3(0, -0.32f, 0), new Vector3(0.22f, 0.27f, 0.24f), suit);
            Part("Weighted boot", PrimitiveType.Cube, leg, new Vector3(0, -0.72f, 0.07f), new Vector3(0.25f, 0.2f, 0.39f), boots);
            Part("Copper boot sole", PrimitiveType.Cube, leg, new Vector3(0, -0.795f, 0.07f), new Vector3(0.26f, 0.045f, 0.4f), brass);
            if (side < 0) { leftArm = arm; leftElbow = elbow; leftLeg = leg; }
            else { rightArm = arm; rightElbow = elbow; rightLeg = leg; }
        }
        Part("Chest ballast", PrimitiveType.Cube, bodyRoot, new Vector3(0, 1.08f, 0.185f), new Vector3(0.23f, 0.2f, 0.07f), boots);
        Part("Ballast fastening", PrimitiveType.Cube, bodyRoot, new Vector3(0, 1.08f, 0.225f), new Vector3(0.15f, 0.035f, 0.012f), brass);
        var valve = Part("Air supply connector", PrimitiveType.Cylinder, bodyRoot, new Vector3(0.23f, 1.32f, 0.13f), new Vector3(0.075f, 0.055f, 0.075f), brass);
        valve.localRotation = Quaternion.Euler(90, 0, 0);
        Part("Rear air coupling", PrimitiveType.Cylinder, bodyRoot, new Vector3(0, 1.35f, -0.19f), new Vector3(0.09f, 0.05f, 0.09f), brass).localRotation = Quaternion.Euler(90, 0, 0);
    }
    void SetBehaviour(Behaviour next) { behaviour = next; stateTime = 0; gestureTime = 0; }
    static float PlanarDistance(Vector3 a, Vector3 b) { return Vector3.ProjectOnPlane(a - b, Vector3.up).magnitude; }
    void TurnToward(Vector3 target)
    {
        Vector3 direction = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 100 * Time.deltaTime);
    }
    void UpdateBehaviour()
    {
        stateTime += Time.deltaTime; gestureTime += Time.deltaTime;
        var viewer = Camera.main;
        bool moving = false;
        if (viewer)
        {
            float distance = PlanarDistance(viewer.transform.position, transform.position);
            if (!greeted && behaviour == Behaviour.Idle && distance <= greetingDistance)
            { OpenDialogue(); SetBehaviour(Behaviour.Greeting); }
            if (behaviour == Behaviour.Idle || behaviour == Behaviour.Greeting || behaviour == Behaviour.Talking || behaviour == Behaviour.Thanking || behaviour == Behaviour.Inviting || behaviour == Behaviour.RopeBriefing || behaviour == Behaviour.Arrived)
            {
                if (distance <= waitForPlayerDistance + 2) TurnToward(viewer.transform.position);
            }
            if (behaviour == Behaviour.Greeting && stateTime >= 2.5f)
                SetBehaviour(dialoguePanel.gameObject.activeSelf ? Behaviour.Talking : Behaviour.Idle);
            if (behaviour == Behaviour.Talking && distance > greetingDistance + 2)
            { CloseDialogue(); SetBehaviour(Behaviour.Idle); }
            if (behaviour == Behaviour.Thanking && stateTime >= 3.5f) SetBehaviour(Behaviour.Inviting);
            if (behaviour == Behaviour.Inviting && stateTime >= 2.5f) BeginEscort();
            if (behaviour == Behaviour.Leading) moving = WalkRoute(viewer.transform);
            if (behaviour == Behaviour.RopeBriefing && stateTime >= 5 && diveRope)
            {
                diveStart = transform.position;
                CloseDialogue(); SetBehaviour(Behaviour.Descending);
            }
            if (behaviour == Behaviour.Descending) UpdateRopeDescent();
        }
        AnimateBody(moving);
    }
    void BeginEscort()
    {
        if (!act4Destination)
        {
            dialogue.text = "谢谢！请通过导航前往 ACT 4。\n\nThank you! Use the navigation menu to visit ACT 4.";
            SetBehaviour(Behaviour.Arrived);
            Debug.LogWarning("Assign an Act 4 destination to enable the diver's escort.", this);
            return;
        }
        var points = new System.Collections.Generic.List<Vector3>();
        if (escortWaypoints != null)
            foreach (var waypoint in escortWaypoints)
                if (waypoint) points.Add(new Vector3(waypoint.position.x, homePosition.y, waypoint.position.z));
        Vector3 arrival = diveRope ? diveRope.NpcSurfaceDock : act4Destination.TransformPoint(act4ArrivalOffset);
        arrival.y = homePosition.y;
        // Use a baked navigation route when available, otherwise the authored clear route.
        if (points.Count == 0)
        {
            Vector3 departure = homePosition + homeRotation * Vector3.forward * Mathf.Max(0, departureDistance);
            departure.y = homePosition.y;
            points.Add(departure);
            var path = new UnityEngine.AI.NavMeshPath();
            if (UnityEngine.AI.NavMesh.SamplePosition(departure, out var start, 2f, UnityEngine.AI.NavMesh.AllAreas) &&
                UnityEngine.AI.NavMesh.SamplePosition(arrival, out var end, 2f, UnityEngine.AI.NavMesh.AllAreas) &&
                UnityEngine.AI.NavMesh.CalculatePath(start.position, end.position, UnityEngine.AI.NavMesh.AllAreas, path) &&
                path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                foreach (var corner in path.corners) points.Add(new Vector3(corner.x, homePosition.y, corner.z));
        }
        points.Add(arrival); route = points.ToArray(); routeIndex = 0;
        // The instruction panel stays at the station; speech remains attached to the diver.
        CloseDialogue(); SetBehaviour(Behaviour.Leading);
    }
    bool WalkRoute(Transform viewer)
    {
        float distance = PlanarDistance(viewer.position, transform.position);
        // Hysteresis keeps the diver from switching between walking and waiting every frame.
        if (distance > waitForPlayerDistance) waitingForPlayer = true;
        else if (distance < Mathf.Max(0.75f, waitForPlayerDistance - 1)) waitingForPlayer = false;
        if (waitingForPlayer) { TurnToward(viewer.position); return false; }
        while (routeIndex < route.Length && PlanarDistance(transform.position, route[routeIndex]) < 0.2f) routeIndex++;
        if (routeIndex >= route.Length)
        {
            SetBehaviour(diveRope ? Behaviour.RopeBriefing : Behaviour.Arrived);
            dialogue.text = diveRope ?
                "这里是 ACT 4。我先沿绳下去，在海底等你。\n握住绳索手柄，或点“下潜”跟上我；海底的绳索可以带你上浮。\n\nI'll descend first and wait below. Grip the rope or press Descend to follow. Use the underwater rope to return." :
                "我们到了！这里就是 ACT 4，接下来一起探索水下港口。\n\nWe've arrived at ACT 4! Let's explore the underwater harbour.";
            ShowDialoguePopup(); return false;
        }
        Vector3 direction = Vector3.ProjectOnPlane(route[routeIndex] - transform.position, Vector3.up).normalized;
        float step = Mathf.Min(walkSpeed * Time.deltaTime, PlanarDistance(transform.position, route[routeIndex]));
        TurnToward(route[routeIndex]);
        // Stop before scenery instead of walking through an exhibit or wall.
        if (Physics.SphereCast(transform.position + Vector3.up * 0.9f, 0.25f, direction, out _, step + 0.15f, ~0, QueryTriggerInteraction.Ignore))
            return false;
        Vector3 next = transform.position + direction * step;
        if (!Physics.Raycast(next + Vector3.up * 0.45f, Vector3.down, out var floor, 0.9f, ~0, QueryTriggerInteraction.Ignore))
            return false;
        next.y = floor.point.y;
        if (floor.normal.y < 0.7f) return false;
        transform.position = next;
        stride += step * 7;
        return true;
    }
    void UpdateRopeDescent()
    {
        if (!diveRope) { SetBehaviour(Behaviour.Arrived); return; }
        const float approachDuration = 1.5f;
        float duration = Mathf.Max(2, diveRope.travelDuration);
        if (stateTime < approachDuration)
            transform.position = Vector3.Lerp(diveStart, diveRope.NpcRopeTop, DiveRopeStation.Ease(stateTime / approachDuration));
        else
            transform.position = Vector3.Lerp(diveRope.NpcRopeTop, diveRope.NpcRopeBottom,
                DiveRopeStation.Ease((stateTime - approachDuration) / duration));
        TurnToward(transform.position + diveRope.transform.right);
        if (stateTime >= approachDuration + duration)
        {
            SetBehaviour(Behaviour.Arrived);
            dialogue.text = "欢迎来到海底！现在可以自由探索。\n想回平台时，握住这里的绳索手柄，或点“上浮”。\n\nWelcome to the seabed! Explore freely. Grip the rope here or press Ascend to return to the platform.";
            ShowDialoguePopup();
        }
    }
    void AnimateBody(bool moving)
    {
        Vector3 l = new Vector3(0, 0, -8), r = new Vector3(0, 0, 8);
        Vector3 le = new Vector3(-8, 0, 0), re = le;
        float nod = 0, legSwing = moving ? Mathf.Sin(stride) * 24 : 0;
        float breathe = Mathf.Sin(Time.time * 1.7f) * 0.004f;
        if (behaviour == Behaviour.Greeting)
        {
            float blend = Mathf.Sin(Mathf.Clamp01(stateTime / 2.5f) * Mathf.PI);
            r = new Vector3(-12, 0, 95) * blend + r * (1 - blend);
            re = new Vector3(0, 0, 60 + Mathf.Sin(stateTime * 12) * 18) * blend;
        }
        else if (behaviour == Behaviour.Descending)
        {
            float stroke = Mathf.Sin(stateTime * 3);
            l = new Vector3(-70 + stroke * 15, -10, -12);
            r = new Vector3(-70 - stroke * 15, 10, 12);
            le = new Vector3(-20 - stroke * 12, 0, 0);
            re = new Vector3(-20 + stroke * 12, 0, 0);
            legSwing = stroke * 9;
        }
        else if (behaviour == Behaviour.Talking || behaviour == Behaviour.RopeBriefing)
        {
            float beat = Mathf.Sin(gestureTime * 2.8f);
            l = new Vector3(-18 - beat * 6, -10, -18);
            r = new Vector3(-25 + beat * 8, 10, 22);
            le = new Vector3(-35 - beat * 10, 0, 0);
            re = new Vector3(-48 + beat * 10, 0, 0);
            nod = Mathf.Sin(gestureTime * 2.2f) * 3;
        }
        else if (behaviour == Behaviour.Thanking)
        {
            float envelope = Mathf.Sin(Mathf.Clamp01(stateTime / 3.5f) * Mathf.PI);
            nod = (12 + Mathf.Sin(stateTime * 5.5f) * 10) * envelope;
            r = new Vector3(-35, -20, 20) * envelope + r * (1 - envelope);
            re = new Vector3(-85, 0, 0) * envelope;
        }
        else if (behaviour == Behaviour.Inviting || (behaviour == Behaviour.Leading && waitingForPlayer))
        {
            r = new Vector3(-55, 0, 30);
            re = new Vector3(-35 + Mathf.Sin(gestureTime * 4) * 18, 0, 0);
        }
        else if (moving) { l.x = -legSwing * 0.7f; r.x = legSwing * 0.7f; }
        float blendSpeed = 1 - Mathf.Exp(-8 * Time.deltaTime);
        leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, Quaternion.Euler(l), blendSpeed);
        rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(r), blendSpeed);
        leftElbow.localRotation = Quaternion.Slerp(leftElbow.localRotation, Quaternion.Euler(le), blendSpeed);
        rightElbow.localRotation = Quaternion.Slerp(rightElbow.localRotation, Quaternion.Euler(re), blendSpeed);
        headPivot.localRotation = Quaternion.Slerp(headPivot.localRotation, Quaternion.Euler(nod, 0, 0), blendSpeed);
        leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, Quaternion.Euler(legSwing, 0, 0), blendSpeed);
        rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(-legSwing, 0, 0), blendSpeed);
        bodyRoot.localPosition = Vector3.up * (breathe + (moving ? Mathf.Abs(Mathf.Sin(stride)) * 0.018f : 0));
        bool speaking = behaviour == Behaviour.Talking || behaviour == Behaviour.Greeting || behaviour == Behaviour.Thanking || behaviour == Behaviour.RopeBriefing;
        mouth.localScale = new Vector3(0.067f, speaking ? 0.013f + Mathf.Abs(Mathf.Sin(gestureTime * 9)) * 0.016f : 0.013f, 0.012f);
    }
    void RestoreStation()
    {
        transform.SetPositionAndRotation(homePosition, homeRotation);
        greeted = waitingForPlayer = false; route = null; routeIndex = 0;
        SetBehaviour(Behaviour.Idle);
        if (dialoguePanel) dialoguePanel.SetParent(transform, true);
    }
    Transform CreatePanel(string name, Transform parent, Vector3 position, Vector2 size)
    {
        var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
        canvasObject.hideFlags = HideFlags.HideAndDontSave;
        canvasObject.transform.SetParent(parent, false);
        canvasObject.transform.localPosition = position;
        canvasObject.transform.localScale = Vector3.one * 0.0015f;
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        canvasObject.GetComponent<RectTransform>().sizeDelta = size;
        return canvasObject.transform;
    }
    Transform Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var item = GameObject.CreatePrimitive(type);
        item.name = name;
        item.hideFlags = HideFlags.HideAndDontSave;
        item.transform.SetParent(visuals.transform, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        item.GetComponent<Collider>().enabled = false;
        return item.transform;
    }
    Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        return new Material(shader) { color = color, hideFlags = HideFlags.HideAndDontSave };
    }
    T Element<T>(string name, Vector2 position, Vector2 size) where T : Graphic
    {
        var item = new GameObject(name, typeof(RectTransform));
        item.hideFlags = HideFlags.HideAndDontSave;
        item.transform.SetParent(uiParent, false);
        var rect = item.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return item.AddComponent<T>();
    }
    Text Label(string name, string message, Vector2 position, Vector2 size, int fontSize)
    {
        var text = Element<Text>(name, position, size);
        text.font = font ? font : fallback;
        text.fontSize = fontSize;
        text.text = message;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.supportRichText = false;
        text.raycastTarget = false;
        return text;
    }
    Text MakeButton(string caption, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var image = Element<Image>(caption, position, new Vector2(480, 100));
        image.color = new Color(0.1f, 0.42f, 0.42f);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        var label = Label(caption + " Label", caption, Vector2.zero, new Vector2(460, 90), 34);
        label.transform.SetParent(image.transform, false);
        return label;
    }
    void OnDisable()
    {
        if (wearable && Application.isPlaying)
        {
            wearable.ReturnToStand();
            Destroy(wearable);
            wearable = null;
        }
        if (instructionPanel) Dispose(instructionPanel.gameObject);
        if (dialoguePanel) Dispose(dialoguePanel.gameObject);
        instructionPanel = dialoguePanel = uiParent = null;
        if (Application.isPlaying) transform.SetPositionAndRotation(homePosition, homeRotation);
        Dispose(visuals); Dispose(fallback); Dispose(suit); Dispose(skin); Dispose(boots);
        Dispose(brass); Dispose(white); Dispose(hair);
        visuals = null;
    }
    static void Dispose(Object item)
    {
        if (!item) return;
        if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
    }
}

/// <summary>Small non-interactive speech bubble tail that points toward its speaker.</summary>
public class DiverSpeechBubbleTail : MaskableGraphic
{
    Vector2 edge, tip;
    public void SetTail(Vector2 edgePoint, Vector2 tipPoint)
    {
        if (edge == edgePoint && tip == tipPoint) return;
        edge = edgePoint; tip = tipPoint;
        SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Vector2 direction = (tip - edge).normalized;
        Vector2 halfBase = new Vector2(-direction.y, direction.x) * 55f;
        mesh.AddVert(edge - halfBase, color, Vector2.zero);
        mesh.AddVert(edge + halfBase, color, Vector2.zero);
        mesh.AddVert(tip, color, Vector2.zero);
        mesh.AddTriangle(0, 1, 2);
    }
}
