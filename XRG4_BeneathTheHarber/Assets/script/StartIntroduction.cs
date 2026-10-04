using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>An editable world-space introduction, visible in the scene and in VR.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class StartIntroduction : MonoBehaviour
{
    [Header("Introduction / 引言")]
    public string title = "Beneath the Harbour";
    [TextArea(6, 20)]
    public string introduction = "Welcome to Dressing the Diver.\n\nDiscover the equipment that helped divers work beneath the harbour, then explore the underwater environment.\n\nTake your time to look around before beginning your journey.";
    [Tooltip("Assign a font containing your language's characters. Desktop preview also tries installed Chinese fonts.")]
    public Font font;
    [Header("Appearance / 外观")]
    public Color backgroundColor = new Color(0.025f, 0.09f, 0.12f, 0.97f);
    public Color textColor = new Color(0.9f, 0.96f, 0.96f);
    public Color accentColor = new Color(0.25f, 0.8f, 0.75f);
    [Tooltip("Exact title font size. Text does not automatically shrink to fit.")]
    [Min(1)] public int titleFontSize = 58;
    [Tooltip("Exact body font size. Enlarge the panel or shorten the text if it does not fit.")]
    [Min(1)] public int bodyFontSize = 34;
    [Tooltip("Panel size in metres. Move and rotate this object's Transform to place it.")]
    public Vector2 size = new Vector2(2.4f, 1.8f);
    [Header("Scrolling / 滚动")]
    [Min(1)] public float scrollSensitivity = 45f;

    GameObject display;
    Text heading, body, hint;
    Image background, accent;
    ScrollRect scroll;
    Scrollbar scrollbar;
    RectTransform content;
    Font fallback;
    bool refresh = true;

    void OnEnable() { refresh = true; }
    void OnValidate() { refresh = true; }
    void Update()
    {
        if (!display) Build();
        if (!refresh) return;
        refresh = false;
        display.transform.localScale = new Vector3(Mathf.Max(0.5f, size.x) / 1200f,
            Mathf.Max(0.5f, size.y) / 900f, 0.002f);
        Font selected = font ? font : GetFallback();
        heading.font = body.font = selected;
        heading.text = title;
        body.text = introduction;
        heading.resizeTextForBestFit = body.resizeTextForBestFit = false;
        heading.fontSize = Mathf.Max(1, titleFontSize);
        body.fontSize = Mathf.Max(1, bodyFontSize);
        hint.font = selected;
        hint.color = textColor;
        scrollbar.handleRect.GetComponent<Image>().color = accentColor;
        scroll.scrollSensitivity = Mathf.Max(1f, scrollSensitivity);
        // Measure with the exact selected font and width, so long text can scroll
        // without reducing the user's font size.
        content.sizeDelta = new Vector2(980, Mathf.Max(510, body.preferredHeight + 24));
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = 1;
        heading.color = body.color = textColor;
        background.color = backgroundColor;
        accent.color = accentColor;
    }

    Font GetFallback()
    {
        if (!fallback)
        {
            // For portable builds, assign a bundled font in the Inspector.
            fallback = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "PingFang SC", "Arial" }, 48);
            if (!fallback) fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return fallback;
    }

    void Build()
    {
        display = new GameObject("Introduction Display (generated)", typeof(RectTransform), typeof(Canvas));
        display.hideFlags = HideFlags.HideAndDontSave;
        display.transform.SetParent(transform, false);
        var canvas = display.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        display.AddComponent<TrackedDeviceGraphicRaycaster>();
        display.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 900);
        background = Element<Image>("Background", new Vector2(0, 0), new Vector2(1200, 900));
        accent = Element<Image>("Accent", new Vector2(0, 375), new Vector2(1050, 8));
        heading = Element<Text>("Title", new Vector2(0, 275), new Vector2(1050, 150));
        heading.alignment = TextAnchor.MiddleLeft;
        heading.horizontalOverflow = HorizontalWrapMode.Wrap;
        heading.verticalOverflow = VerticalWrapMode.Truncate;
        var viewportImage = Element<Image>("Scroll Viewport", new Vector2(-35, -70), new Vector2(980, 510));
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;
        viewportImage.gameObject.AddComponent<RectMask2D>();
        var viewport = viewportImage.rectTransform;
        scroll = viewportImage.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.decelerationRate = 0.12f;
        body = Element<Text>("Introduction", Vector2.zero, new Vector2(980, 510));
        content = body.rectTransform;
        content.SetParent(viewport, false);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        body.raycastTarget = true;
        scroll.content = content;
        body.alignment = TextAnchor.UpperLeft;
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        body.lineSpacing = 1.2f;
        heading.supportRichText = body.supportRichText = false;
        var track = Element<Image>("Scrollbar", new Vector2(510, -70), new Vector2(44, 510));
        track.color = new Color(1, 1, 1, 0.12f);
        track.raycastTarget = true;
        scrollbar = track.gameObject.AddComponent<Scrollbar>();
        var handle = Element<Image>("Scroll Handle", Vector2.zero, Vector2.zero);
        handle.transform.SetParent(track.transform, false);
        handle.raycastTarget = true;
        handle.rectTransform.anchorMin = Vector2.zero;
        handle.rectTransform.anchorMax = Vector2.one;
        handle.rectTransform.offsetMin = new Vector2(5, 5);
        handle.rectTransform.offsetMax = new Vector2(-5, -5);
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        hint = Element<Text>("Controller Hint", new Vector2(0, -385), new Vector2(1050, 60));
        hint.fontSize = 22;
        hint.alignment = TextAnchor.MiddleCenter;
        hint.text = "指向正文并拨动摇杆滚动 · 按住扳机拖动滚动条";
        refresh = true;
    }

    T Element<T>(string label, Vector2 position, Vector2 dimensions) where T : Graphic
    {
        var item = new GameObject(label, typeof(RectTransform));
        item.hideFlags = HideFlags.HideAndDontSave;
        item.transform.SetParent(display.transform, false);
        var rect = item.GetComponent<RectTransform>();
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = position;
        var graphic = item.AddComponent<T>();
        graphic.raycastTarget = false;
        return graphic;
    }

    void OnDisable()
    {
        if (display) { if (Application.isPlaying) Destroy(display); else DestroyImmediate(display); }
        if (fallback) { if (Application.isPlaying) Destroy(fallback); else DestroyImmediate(fallback); }
    }
}
