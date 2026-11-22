using UnityEngine;
using UnityEngine.UI;

/// <summary>Decorative frame for exhibit buttons. The parent keeps its original hit area.</summary>
public sealed class OptionFrame : MaskableGraphic
{
    public static OptionFrame Apply(GameObject button)
    {
        var child = new GameObject("Option frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(OptionFrame));
        var rect = (RectTransform)child.transform;
        rect.SetParent(button.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
        var frame = child.GetComponent<OptionFrame>();
        frame.color = new Color(.10f, .32f, .48f);
        frame.raycastTarget = false;
        return frame;
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        if (r.width <= 0 || r.height <= 0) return;
        float slant = Mathf.Min(r.width * .12f, r.height * .35f);
        Vector2[] corners = {
            new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + slant, r.yMax),
            new Vector2(r.xMax, r.yMax), new Vector2(r.xMax - slant, r.yMin)
        };
        const int steps = 4;
        const int vertices = 4 * (steps + 1);
        Vector2 inset = new Vector2(1f - Mathf.Min(.15f, 6f / r.width), 1f - Mathf.Min(.18f, 6f / r.height));
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 corner = corners[i];
            float radius = Mathf.Min(10f, Mathf.Min(r.width, r.height) * .15f);
            Vector2 start = Vector2.MoveTowards(corner, corners[(i + 3) % 4], radius);
            Vector2 end = Vector2.MoveTowards(corner, corners[(i + 1) % 4], radius);
            for (int step = 0; step <= steps; step++)
            {
                float t = (float)step / steps;
                Vector2 edge = Vector2.Lerp(Vector2.Lerp(start, corner, t), Vector2.Lerp(corner, end, t), t);
                mesh.AddVert(edge, color, Vector2.zero);
                mesh.AddVert(r.center + Vector2.Scale(edge - r.center, inset), color, Vector2.zero);
            }
        }
        for (int i = 0; i < vertices; i++)
        {
            int a = i * 2, b = ((i + 1) % vertices) * 2;
            mesh.AddTriangle(a, b, a + 1);
            mesh.AddTriangle(b, b + 1, a + 1);
        }
    }
}
