using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ActDirectionSign : MonoBehaviour
{
    public Transform destination;
    public string message = "下一站：ACT 2\n请沿箭头方向前往 / Follow the arrow";
    public Font font;
    [Min(1)] public int fontSize = 42;
    public Color arrowColor = new Color(0.2f, 0.85f, 0.72f);
    GameObject visuals;
    Transform arrow, label;
    Text text;
    Mesh mesh;
    Material material;
    Font fallback;

    void Update()
    {
        if (!visuals) Build();
        Vector3 direction = destination ? destination.position - transform.position : transform.forward;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.001f) arrow.rotation = Quaternion.LookRotation(direction);
        material.color = arrowColor;
        text.text = message;
        text.fontSize = Mathf.Max(1, fontSize);
        text.font = font ? font : fallback;
        // Rotate the text only around the vertical axis, keeping it comfortable in VR.
        var viewer = Camera.main;
        if (viewer)
        {
            Vector3 facing = label.position - viewer.transform.position;
            facing.y = 0;
            if (facing.sqrMagnitude > 0.001f) label.rotation = Quaternion.LookRotation(facing);
        }
    }

    void Build()
    {
        visuals = new GameObject("Direction Sign (generated)");
        visuals.hideFlags = HideFlags.HideAndDontSave;
        visuals.transform.SetParent(transform, false);
        var marker = new GameObject("Arrow", typeof(MeshFilter), typeof(MeshRenderer));
        marker.hideFlags = HideFlags.HideAndDontSave;
        marker.transform.SetParent(visuals.transform, false);
        arrow = marker.transform;
        mesh = new Mesh { name = "Direction Arrow", hideFlags = HideFlags.HideAndDontSave };
        mesh.vertices = new[] {
            new Vector3(-0.2f, 0, -0.9f), new Vector3(0.2f, 0, -0.9f),
            new Vector3(-0.2f, 0, 0.3f), new Vector3(0.2f, 0, 0.3f),
            new Vector3(-0.65f, 0, 0.3f), new Vector3(0.65f, 0, 0.3f),
            new Vector3(0, 0, 1.1f) };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3, 4, 6, 5 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        marker.GetComponent<MeshFilter>().sharedMesh = mesh;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) shader = Shader.Find("Unlit/Color");
        material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        marker.GetComponent<MeshRenderer>().sharedMaterial = material;
        var panel = new GameObject("Next Station", typeof(RectTransform), typeof(Canvas));
        panel.hideFlags = HideFlags.HideAndDontSave;
        panel.transform.SetParent(visuals.transform, false);
        panel.transform.localPosition = new Vector3(0, 1.6f, 0);
        panel.transform.localScale = Vector3.one * 0.002f;
        label = panel.transform;
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(1100, 180);
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var words = new GameObject("Instruction", typeof(RectTransform), typeof(Text));
        words.hideFlags = HideFlags.HideAndDontSave;
        words.transform.SetParent(label, false);
        var rect = words.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1100, 180);
        text = words.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.supportRichText = false;
        var shadow = words.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.9f);
        shadow.effectDistance = new Vector2(2, -2);
        fallback = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 42);
    }

    void OnDisable()
    {
        Dispose(visuals); Dispose(mesh); Dispose(material); Dispose(fallback);
    }
    static void Dispose(Object item)
    {
        if (!item) return;
        if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
    }
}
