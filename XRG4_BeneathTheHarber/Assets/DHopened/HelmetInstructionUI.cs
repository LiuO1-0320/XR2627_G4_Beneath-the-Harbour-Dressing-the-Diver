using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space hint in front of the headset. Uses a system Chinese font because the
/// project TextMeshPro fonts do not include Chinese glyphs.
/// </summary>
[DisallowMultipleComponent]
public class HelmetInstructionUI : MonoBehaviour
{
    const float CanvasScale = 0.00055f;

    static readonly string[] FontNames =
    {
        "Microsoft YaHei",
        "Microsoft YaHei UI",
        "SimHei",
        "DengXian",
        "NSimSun",
    };

    static HelmetInstructionUI s_Owner;

    Canvas m_Canvas;
    Text m_Status;
    string m_StatusText;

    void Start()
    {
        if (s_Owner != null && s_Owner != this)
            return;

        s_Owner = this;
        CreatePanel();
    }

    void LateUpdate()
    {
        if (m_Canvas == null)
            return;

        var camera = Camera.main;
        if (camera == null)
            return;

        var canvasTransform = m_Canvas.transform;
        if (canvasTransform.parent != camera.transform)
        {
            canvasTransform.SetParent(camera.transform, false);
            canvasTransform.localPosition = new Vector3(-0.02f, -0.42f, 1.15f);
            canvasTransform.localRotation = Quaternion.identity;
            canvasTransform.localScale = Vector3.one * CanvasScale;
        }

        var focus = FindFocus();
        var title = m_Canvas.GetComponentInChildren<Text>();
        if (title != null && title.gameObject.name == "Title")
            title.text = focus != null && focus.IsInspecting ? "正在观察" : "操作说明";

        if (m_Status == null)
            return;

        var status = BuildStatus(focus);
        if (status == m_StatusText)
            return;

        m_StatusText = status;
        m_Status.text = status;
    }

    static HelmetInspect FindFocus()
    {
        HelmetInspect held = null;
        var helmets = HelmetInspect.All;
        for (var i = 0; i < helmets.Count; i++)
        {
            var helmet = helmets[i];
            if (helmet == null)
                continue;

            if (helmet.IsInspecting)
                return helmet;

            if (held == null && helmet.IsHeld)
                held = helmet;
        }

        return held;
    }

    static HelmetHeadWear FindWorn()
    {
        var helmets = HelmetInspect.All;
        for (var i = 0; i < helmets.Count; i++)
        {
            var helmet = helmets[i];
            if (helmet == null)
                continue;

            var wear = helmet.GetComponent<HelmetHeadWear>();
            if (wear != null && wear.IsWorn)
                return wear;
        }

        return null;
    }

    static HelmetStand FindSeated()
    {
        var helmets = HelmetInspect.All;
        for (var i = 0; i < helmets.Count; i++)
        {
            var helmet = helmets[i];
            if (helmet == null)
                continue;

            var stand = helmet.GetComponent<HelmetStand>();
            if (stand != null && stand.IsSeated)
                return stand;
        }

        return null;
    }

    string BuildStatus(HelmetInspect focus)
    {
        if (focus == null)
        {
            if (FindWorn() != null)
                return "头盔已经戴在模特头上。抓住它可以取下来。";

            if (FindSeated() != null)
                return "头盔已经竖直放在方块上。抓住它可以拿走。";

            return "先抓住一个头盔，再按副键放大手里的那一个。只有 2 号头盔可以拆下螺栓。";
        }

        var bolt = focus.GetComponent<HelmetBoltRelease>();
        if (bolt != null && bolt.isActiveAndEnabled && bolt.IsRemoved)
            return "螺栓已拆下。拿在手里转动观察，松开后会停在空中。放回孔位附近再松开，就会装回。";

        if (bolt != null && bolt.isActiveAndEnabled && bolt.CanRemove)
            return "螺栓可以拆了。握住正在轻微起伏的螺栓，把它从头盔上拿下来观察。";

        if (focus.IsInspecting && focus.AllowsPartInspection)
            return "继续双手拉开。放到足够大之后，可以握住螺栓拆下来。";

        if (focus.IsInspecting)
            return "正在放大这个头盔。双手拉开或靠拢调整大小，再按副键放下。";

        var wear = focus.GetComponent<HelmetHeadWear>();
        if (wear != null && wear.IsReadyToWear)
            return "松手，头盔就会戴到模特头上。之后再抓住它可以取下来。";

        var stand = focus.GetComponent<HelmetStand>();
        if (stand != null && stand.IsReadyToSeat)
            return "松手后，方块会把头盔吸正，竖直放稳。";

        if (focus.AllowsPartInspection)
            return "已抓住 2 号头盔。按副键把它拿到面前，放大后可以拆下螺栓。";

        return "已抓住这个头盔。按副键把它拿到面前放大。这个头盔不能拆零件。";
    }

    void OnDestroy()
    {
        if (s_Owner == this)
            s_Owner = null;

        if (m_Canvas != null)
            Destroy(m_Canvas.gameObject);
    }

    void CreatePanel()
    {
        var font = Font.CreateDynamicFontFromOSFont(FontNames, 64);
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var canvasObject = new GameObject("Helmet Instruction UI");
        m_Canvas = canvasObject.AddComponent<Canvas>();
        m_Canvas.renderMode = RenderMode.WorldSpace;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 3f;

        var canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(980f, 980f);

        var panel = CreateUiObject("Panel", canvasObject.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var background = panel.AddComponent<Image>();
        background.color = new Color(0.06f, 0.09f, 0.12f, 0.86f);
        background.raycastTarget = false;

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 28, 28);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddLabel(panel.transform, font, "Title", "操作说明", 52, FontStyle.Bold, new Color(1f, 0.86f, 0.45f), TextAnchor.MiddleLeft, 64f, -1f, false);
        m_Status = AddLabel(panel.transform, font, "Status", "先抓住一个头盔，再按副键放大手里的那一个。只有 2 号头盔可以拆下螺栓。", 36, FontStyle.Normal, new Color(1f, 0.9f, 0.55f), TextAnchor.UpperLeft, 120f, -1f, true);
        AddRow(panel.transform, font, "握把", "拿起头盔");
        AddRow(panel.transform, font, "靠近头上松开", "给模特戴上");
        AddRow(panel.transform, font, "靠近方块松开", "吸正立住");
        AddRow(panel.transform, font, "副键", "放大手里这个");
        AddRow(panel.transform, font, "双手拉开", "放大");
        AddRow(panel.transform, font, "双手靠拢", "缩小");
        AddRow(panel.transform, font, "握住螺栓", "仅 2 号头盔");
        AddRow(panel.transform, font, "靠近孔位松开", "装回螺栓");
        AddRow(panel.transform, font, "再按副键", "放下");
    }

    static void AddRow(Transform parent, Font font, string key, string action)
    {
        var row = CreateUiObject("Row " + key, parent);
        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 20f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        var rowElement = row.AddComponent<LayoutElement>();
        rowElement.minHeight = 62f;
        rowElement.preferredHeight = 62f;

        AddLabel(row.transform, font, "Key", key, 42, FontStyle.Normal, new Color(0.65f, 0.88f, 1f), TextAnchor.MiddleLeft, 62f, 430f, false);
        AddLabel(row.transform, font, "Action", action, 42, FontStyle.Bold, Color.white, TextAnchor.MiddleRight, 62f, 460f, false);
    }

    static Text AddLabel(Transform parent, Font font, string objectName, string content, int size, FontStyle style, Color color, TextAnchor alignment, float height, float width, bool wrap)
    {
        var label = CreateUiObject(objectName, parent);
        var element = label.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        if (width > 0f)
        {
            element.minWidth = width;
            element.preferredWidth = width;
        }
        else
        {
            element.flexibleWidth = 1f;
        }

        var text = label.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = false;
        return text;
    }

    static GameObject CreateUiObject(string objectName, Transform parent)
    {
        var uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }
}
