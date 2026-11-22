using UnityEngine;
using UnityEngine.UI;

public sealed class OptionFrame : MaskableGraphic
{
    public static OptionFrame Apply(GameObject button)
    {
        var child = new GameObject("Option Frame", typeof(RectTransform), typeof(OptionFrame));
        child.layer = button.layer;
        var frame = child.GetComponent<OptionFrame>();
        var rect = frame.rectTransform;
        rect.SetParent(button.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        frame.color = new Color32(22, 60, 91, 255);
        frame.raycastTarget = false;
        return frame;
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        const int segments = 24;
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float x = Mathf.Lerp(rect.xMin, rect.xMax, t);
                float y = rect.center.y + side * rect.height * (0.31f + 0.16f * Mathf.Sin(Mathf.PI * t));
                int index = mesh.currentVertCount;
                mesh.AddVert(new Vector3(x, y - 1), color, Vector2.zero);
                mesh.AddVert(new Vector3(x, y + 1), color, Vector2.zero);
                if (i == 0) continue;
                mesh.AddTriangle(index - 2, index - 1, index);
                mesh.AddTriangle(index, index - 1, index + 1);
            }
        }
    }
}
