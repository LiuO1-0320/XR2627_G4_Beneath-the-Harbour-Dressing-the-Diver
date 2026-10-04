using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class HelmetInteractionGuidance : MonoBehaviour
{
    public DivingHelmetWearable wearable;
    public Font font;
    [TextArea(4, 10)] public string instructions = "试戴潜水头盔 / Try the diving helmet\n按住握把抓取 → 移到头部附近 → 松开佩戴\nHold grip → bring to your head → release\n按 B / Y 取下并放回展台 / Return to stand";
    [TextArea(2, 5)] public string successMessage = "佩戴成功！ / Helmet equipped!\n按 B / Y 可取下头盔";
    [Min(1)] public int fontSize = 34;
    [Min(0.5f)] public float successDuration = 3f;
    GameObject station, notification;
    Text stationText, notificationText;
    CanvasGroup notificationGroup;
    Font fallback;
    bool previousWorn;
    float remaining;

    void Update()
    {
        if (!station) Build();
        Font selected = font ? font : fallback;
        stationText.font = notificationText.font = selected;
        stationText.fontSize = notificationText.fontSize = Mathf.Max(1, fontSize);
        stationText.text = Application.isPlaying && wearable && wearable.IsWorn
            ? successMessage : instructions;
        notificationText.text = successMessage;
        Transform viewer = wearable && wearable.head ? wearable.head : Camera.main ? Camera.main.transform : null;
        if (viewer)
        {
            Vector3 direction = station.transform.position - viewer.position;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.001f) station.transform.rotation = Quaternion.LookRotation(direction);
        }
        if (!Application.isPlaying) return;
        bool worn = wearable && wearable.IsWorn;
        if (worn && !previousWorn) remaining = successDuration;
        if (!worn) remaining = 0;
        previousWorn = worn;
        if (remaining > 0 && viewer)
        {
            notification.transform.SetPositionAndRotation(viewer.TransformPoint(new Vector3(0, -0.18f, 1.3f)), viewer.rotation);
            notificationGroup.alpha = Mathf.Clamp01(remaining / 0.4f);
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
        }
        else notificationGroup.alpha = 0;
    }

    void Build()
    {
        fallback = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 42);
        station = Panel("Helmet instructions (generated)", new Vector2(1100, 320), out stationText);
        notification = Panel("Equipped notification (generated)", new Vector2(900, 160), out notificationText);
        notification.transform.localScale = Vector3.one * 0.001f;
        notificationText.color = new Color(0.45f, 1, 0.75f);
        notificationGroup = notification.AddComponent<CanvasGroup>();
        notificationGroup.alpha = 0;
        notificationGroup.interactable = false;
        notificationGroup.blocksRaycasts = false;
    }

    GameObject Panel(string label, Vector2 dimensions, out Text text)
    {
        var panel = new GameObject(label, typeof(RectTransform), typeof(Canvas), typeof(Image));
        panel.hideFlags = HideFlags.HideAndDontSave;
        panel.transform.SetParent(transform, false);
        panel.transform.localScale = Vector3.one * 0.002f;
        panel.GetComponent<RectTransform>().sizeDelta = dimensions;
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        panel.GetComponent<Image>().color = new Color(0.025f, 0.09f, 0.12f, 0.95f);
        panel.GetComponent<Image>().raycastTarget = false;
        var words = new GameObject("Message", typeof(RectTransform), typeof(Text));
        words.hideFlags = HideFlags.HideAndDontSave;
        words.transform.SetParent(panel.transform, false);
        words.GetComponent<RectTransform>().sizeDelta = dimensions - new Vector2(60, 30);
        text = words.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.supportRichText = false;
        text.raycastTarget = false;
        return panel;
    }

    void OnDisable()
    {
        Dispose(station); Dispose(notification); Dispose(fallback);
        previousWorn = false; remaining = 0;
    }
    static void Dispose(Object item)
    {
        if (!item) return;
        if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
    }
}
