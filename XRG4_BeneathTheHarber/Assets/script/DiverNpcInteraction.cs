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
    public Vector3 helmetEulerOffset = Vector3.zero;
    [Min(0.1f)] public float wearDistance = 0.4f;
    GameObject visuals;
    Transform head, dialoguePanel, instructionPanel, uiParent;
    Text dialogue, nextCaption;
    Font fallback;
    Material suit, skin, boots;
    DivingHelmetWearable wearable;
    int page;
    bool previouslyWorn;
    readonly string[] lines = {
        "你好，我是这里的潜水员。准备下潜前，请帮我检查并戴好头盔。\n\nHello! I am the diver at this station. Help me check and put on my helmet before the dive.",
        "先观察头盔的视窗和连接结构。清晰的视野与牢固的连接对潜水员很重要。\n\nLook at the helmet's viewports and connections. A clear view and secure connections are important to a diver.",
        "请按住握把抓起展台上的 helmet3，移到我的头部附近，再松开握把。\n\nHold the grip to pick up helmet3 from the stand. Bring it close to my head, then release the grip.",
        "谢谢，头盔已经戴好了！你完成了为潜水员戴头盔的体验。\n\nThank you, my helmet is on! You have completed the helmet dressing activity."
    };

    void OnEnable() { Build(); SetupWearable(); }
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
            if (dialoguePanel.gameObject.activeSelf) FaceViewer(dialoguePanel, Camera.main.transform);
        }
        if (!Application.isPlaying || !wearable) return;
        if (wearable.IsWorn != previouslyWorn)
        {
            previouslyWorn = wearable.IsWorn;
            page = wearable.IsWorn ? 3 : 2;
            RefreshDialogue();
            if (wearable.IsWorn) ShowDialoguePopup();
        }
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
    }
    void ShowDialoguePopup()
    {
        var viewer = Camera.main;
        if (viewer)
        {
            Vector3 forward = Vector3.ProjectOnPlane(viewer.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            dialoguePanel.position = viewer.transform.position + forward * 2f + right * 1.3f - Vector3.up * 0.1f;
            FaceViewer(dialoguePanel, viewer.transform);
        }
        dialoguePanel.gameObject.SetActive(true);
    }
    public void CloseDialogue() { if (dialoguePanel) dialoguePanel.gameObject.SetActive(false); }
    public void NextDialogue()
    {
        if (!Application.isPlaying) return;
        page = (page + 1) % ((wearable && wearable.IsWorn) ? 4 : 3);
        RefreshDialogue();
    }
    public void ResetActivity()
    {
        if (!Application.isPlaying || !wearable) return;
        wearable.ReturnToStand();
        // A held helmet cannot be reset; keep its current dialogue until released.
        if (wearable.IsWorn) return;
        previouslyWorn = false;
        page = 0;
        RefreshDialogue();
        CloseDialogue();
    }
    void RefreshDialogue()
    {
        if (dialogue) dialogue.text = lines[page];
        if (nextCaption) nextCaption.text = page == 3 ? "再聊一次 / Talk again" : "下一句 / Next";
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
        Shape("Diving suit torso", PrimitiveType.Capsule, new Vector3(0, 1.08f, 0), new Vector3(0.52f, 0.39f, 0.32f), suit);
        head = Shape("NPC Head", PrimitiveType.Sphere, new Vector3(0, 1.65f, 0), Vector3.one * 0.25f, skin);
        Shape("Neck", PrimitiveType.Cylinder, new Vector3(0, 1.47f, 0), new Vector3(0.12f, 0.08f, 0.12f), skin);
        for (int side = -1; side <= 1; side += 2)
        {
            Shape("Suit leg", PrimitiveType.Capsule, new Vector3(side * 0.14f, 0.48f, 0), new Vector3(0.22f, 0.26f, 0.24f), suit);
            Shape("Weighted boot", PrimitiveType.Cube, new Vector3(side * 0.14f, 0.1f, 0.06f), new Vector3(0.24f, 0.2f, 0.38f), boots);
            Shape("Suit arm", PrimitiveType.Capsule, new Vector3(side * 0.36f, 1.06f, 0), new Vector3(0.17f, 0.27f, 0.17f), suit);
            Shape("Glove", PrimitiveType.Sphere, new Vector3(side * 0.36f, 0.74f, 0), new Vector3(0.18f, 0.2f, 0.18f), boots);
        }
        Shape("Suit chest plate", PrimitiveType.Cube, new Vector3(0, 1.16f, 0.17f), new Vector3(0.32f, 0.28f, 0.05f), boots);
        fallback = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 36);
        instructionPanel = CreatePanel("Act 3 Instructions (generated)", instructionAnchor ? instructionAnchor : transform,
            instructionAnchor ? Vector3.zero : new Vector3(-2.5f, 1.6f, 0), new Vector2(1300, 620));
        uiParent = instructionPanel;
        Element<Image>("Background", Vector2.zero, new Vector2(1300, 620)).color = new Color(0.025f, 0.09f, 0.12f, 0.97f);
        Label("Title", "ACT 3 · 为潜水员戴头盔 / Dress the diver", new Vector2(0, 235), new Vector2(1200, 80), 38);
        Label("Instructions", "手柄射线指向“开始对话”，按扳机打开对话弹窗。\n按住握把抓起 helmet3，移到潜水员头部附近松手。\n\nPoint at Start dialogue and press the trigger to open the conversation.\nGrip helmet3, bring it close to the diver's head, then release.",
            new Vector2(0, 20), new Vector2(1180, 300), 32);
        MakeButton("开始对话 / Talk", new Vector2(-280, -225), OpenDialogue);
        MakeButton("重置 / Reset", new Vector2(280, -225), ResetActivity);

        dialoguePanel = CreatePanel("Diver Dialogue Popup (generated)", transform, new Vector3(2.5f, 1.6f, 0), new Vector2(1300, 720));
        uiParent = dialoguePanel;
        Element<Image>("Background", Vector2.zero, new Vector2(1300, 720)).color = new Color(0.025f, 0.09f, 0.12f, 0.98f);
        Label("Title", "潜水员 / Diver", new Vector2(0, 280), new Vector2(1200, 80), 42);
        dialogue = Label("Dialogue", "", new Vector2(0, 30), new Vector2(1180, 380), 34);
        dialogue.alignment = TextAnchor.UpperLeft;
        nextCaption = MakeButton("下一句 / Next", new Vector2(-280, -270), NextDialogue);
        MakeButton("关闭 / Close", new Vector2(280, -270), CloseDialogue);
        RefreshDialogue();
        CloseDialogue();
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
        Dispose(visuals); Dispose(fallback); Dispose(suit); Dispose(skin); Dispose(boots);
        visuals = null;
    }
    static void Dispose(Object item)
    {
        if (!item) return;
        if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
    }
}
