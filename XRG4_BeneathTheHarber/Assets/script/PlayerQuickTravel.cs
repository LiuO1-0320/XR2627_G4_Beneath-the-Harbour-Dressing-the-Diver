using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;

[ExecuteAlways]
public class PlayerQuickTravel : MonoBehaviour
{
    public Transform start, act1, act2, act3;
    public Font font;
    [Tooltip("Menu position relative to the player's headset, in metres.")]
    public Vector3 viewOffset = new Vector3(-0.65f, -0.05f, 1.4f);
    GameObject panel;
    GameObject drawer;
    Transform uiParent;
    RectTransform toggleRect;
    Text toggleCaption;
    bool expanded = true;
    Font fallback;
    Text status;
    bool previousA;

    void Update()
    {
        if (!panel) Build();
        if (!Application.isPlaying) return;
        bool pressed = InputDevices.GetDeviceAtXRNode(XRNode.RightHand)
            .TryGetFeatureValue(CommonUsages.primaryButton, out bool value) && value;
        if (pressed && !previousA) ToggleMenu();
        previousA = pressed;
    }
    void LateUpdate()
    {
        if (!Application.isPlaying || !panel || !Camera.main) return;
        var viewer = Camera.main.transform;
        Vector3 position = viewer.TransformPoint(viewOffset);
        panel.transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - viewer.position, viewer.up));
    }
    public void ToggleMenu()
    {
        expanded = !expanded;
        ApplyMenuState();
    }
    void ApplyMenuState()
    {
        drawer.SetActive(expanded);
        toggleCaption.text = expanded ? "收起 / Close" : "打开导航\nOpen menu";
        toggleRect.anchoredPosition = expanded ? new Vector2(0, -450) : Vector2.zero;
    }
    public void TravelTo(int index)
    {
        if (!Application.isPlaying) return;
        Transform destination = index == 0 ? start : index == 1 ? act1 : index == 2 ? act2 : act3;
        if (!destination) { status.text = "目的地未设置 / Destination missing"; return; }
        var viewer = Camera.main;
        var origin = viewer ? viewer.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() : null;
        var teleport = origin ? origin.GetComponentInChildren<TeleportationProvider>() : null;
        if (!teleport || !teleport.isActiveAndEnabled)
        { status.text = "无法找到玩家传送组件 / Teleport unavailable"; return; }
        // Arrive beside each exhibit, facing it, with space clear of the stand.
        Vector3 offset = index == 1 || index == 2 ? Vector3.left * 2.8f : Vector3.back * 3f;
        Vector3 position = destination.position + offset;
        position.y = 0;
        if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore))
            position.y = hit.point.y;
        Vector3 facing = Vector3.ProjectOnPlane(destination.position - position, Vector3.up).normalized;
        var request = new TeleportRequest {
            destinationPosition = position,
            destinationRotation = Quaternion.LookRotation(facing),
            matchOrientation = MatchOrientation.TargetUpAndForward
        };
        if (teleport.QueueTeleportRequest(request)) status.text = "目的地 / Destination\n" + (index == 0 ? "Start" : "ACT " + index);
        else status.text = "暂时无法跳转 / Please try again";
    }
    void Build()
    {
        fallback = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 36);
        panel = new GameObject("Quick Travel UI (generated)", typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
        panel.hideFlags = HideFlags.HideAndDontSave;
        panel.transform.SetParent(transform, false);
        panel.transform.localScale = Vector3.one * 0.001f;
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 1000);
        drawer = new GameObject("Navigation Drawer", typeof(RectTransform));
        drawer.hideFlags = HideFlags.HideAndDontSave;
        drawer.transform.SetParent(panel.transform, false);
        drawer.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 1000);
        uiParent = drawer.transform;
        Element<Image>("Background", Vector2.zero, new Vector2(420, 1000)).color = new Color(0.025f, 0.09f, 0.12f, 0.97f);
        Label("Title", "快捷跳转\nQuick travel", new Vector2(0, 400), new Vector2(380, 120), 36);
        string[] captions = { "起点 / Start", "ACT 1", "ACT 2", "ACT 3" };
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            MakeButton(captions[i], new Vector2(0, 250 - i * 140), () => TravelTo(index), out _);
        }
        status = Label("Hint", "指向按钮，按扳机\nPoint + trigger\nA 键展开 / 收起", new Vector2(0, -310), new Vector2(380, 120), 26);
        // Keep this button outside the drawer so it remains interactive when collapsed.
        uiParent = panel.transform;
        toggleCaption = MakeButton("收起 / Close", new Vector2(0, -450), ToggleMenu, out toggleRect);
        ApplyMenuState();
    }
    Text MakeButton(string caption, Vector2 position, UnityEngine.Events.UnityAction action, out RectTransform rect)
    {
        var image = Element<Image>(caption, position, new Vector2(360, 110));
        rect = image.rectTransform;
        image.color = new Color(0.1f, 0.42f, 0.42f);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        var text = Label(caption + " Label", caption, Vector2.zero, new Vector2(340, 100), 32);
        text.transform.SetParent(image.transform, false);
        return text;
    }
    T Element<T>(string name, Vector2 position, Vector2 size) where T : Graphic
    {
        var item = new GameObject(name, typeof(RectTransform));
        item.hideFlags = HideFlags.HideAndDontSave;
        item.transform.SetParent(uiParent, false);
        var rect = item.GetComponent<RectTransform>();
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return item.AddComponent<T>();
    }
    Text Label(string name, string message, Vector2 position, Vector2 size, int fontSize)
    {
        var text = Element<Text>(name, position, size);
        text.font = font ? font : fallback; text.fontSize = fontSize;
        text.text = message; text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        return text;
    }
    void OnDisable()
    {
        Dispose(panel); Dispose(fallback); panel = null; previousA = false;
    }
    static void Dispose(Object item)
    {
        if (!item) return;
        if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
    }
}
