using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DefaultExecutionOrder(1001)] // Read RegionGuide visibility after its LateUpdate.
public class ActDirectionSign : MonoBehaviour
{
    public Transform destination;
    public string message = "下一站：ACT 2\n请沿箭头方向前往 / Follow the arrow";
    public Font font;
    [Min(1)] public int fontSize = 42;
    public Color arrowColor = new Color(0.2f, 0.85f, 0.72f);
    GameObject visuals;
    Transform arrow, label, continuation;
    Renderer[] carpetSurfaces;
    Text text;
    Mesh mesh;
    Material material;
    Font fallback;

    // BasicScene carpet centres measured from the prefab transforms, including their parents.
    // Turns follow the arrival areas (ACT2 x141.54,z-1.99; ACT3 x160.18,z9.17),
    // not the older navigation prefab anchors. Headings also describe carpet-end turns.
    struct RouteMarker
    {
        public Vector3 point, forward;
        public RouteMarker(float x, float z, float dx, float dz)
        { point = new Vector3(x, 0, z); forward = new Vector3(dx, 0, dz).normalized; }
    }
    static readonly RouteMarker[] tutorialRoute = {
        new RouteMarker(139, -14.662f, 1, 0)
    };
    static readonly RouteMarker[] caseRoute = {
        new RouteMarker(146.5f, -14.662f, 1, 0),
        new RouteMarker(150.098f, -11.5f, 0, 1),
        new RouteMarker(150.098f, -7.5f, 0, 1),
        new RouteMarker(150.098f, -5f, 0, 1),
        new RouteMarker(149.5f, -2.792f, -1, 0),
        new RouteMarker(146f, -2.792f, -1, 0),
        new RouteMarker(143.5f, -2.792f, -1, 0)
    };
    static readonly RouteMarker[] dressRoute = {
        new RouteMarker(140.5f, -2.792f, -1, 0),
        new RouteMarker(139.388f, 0.5f, 0, 1),
        new RouteMarker(139.388f, 3.5f, 0, 1),
        new RouteMarker(139.388f, 6.768f, 1, 0),
        new RouteMarker(143.5f, 6.768f, 1, 0),
        new RouteMarker(148f, 6.768f, 1, 0),
        new RouteMarker(152.5f, 6.768f, 1, 0),
        new RouteMarker(157f, 6.768f, 1, 0),
        new RouteMarker(160.18f, 6.768f, 0, 1)
    };
    readonly System.Collections.Generic.List<Transform> routeArrows = new System.Collections.Generic.List<Transform>();
    void LateUpdate()
    {
        if (!visuals) Build();
        // One owner draws the active route. The old second sign must not advertise ACT3 early.
        bool owner = gameObject.name == "guidance1";
        var coordinator = StageCoordinator.Current;
        RouteMarker[] route = null;
        if (Application.isPlaying && owner && coordinator && coordinator.Controller.Session.Status == SessionStatus.Running)
        {
            var stage = coordinator.Controller.Session.Stage;
            if (stage == ExperienceStage.TheCase) route = tutorialRoute;
            else if (stage == ExperienceStage.ScaleUp) route = caseRoute;
            else if (stage == ExperienceStage.DressTheDiver) route = dressRoute;
            var guide = coordinator.GetComponent<RegionGuide>();
            if (!guide || !guide.IsGuiding) route = null;
        }
        arrow.gameObject.SetActive(false); continuation.gameObject.SetActive(false); label.gameObject.SetActive(false);
        while (route != null && routeArrows.Count < route.Length)
        {
            var marker = new GameObject("Active route arrow", typeof(MeshFilter), typeof(MeshRenderer));
            marker.hideFlags = HideFlags.HideAndDontSave; marker.transform.SetParent(visuals.transform, false);
            marker.GetComponent<MeshFilter>().sharedMesh = mesh; marker.GetComponent<MeshRenderer>().sharedMaterial = material;
            routeArrows.Add(marker.transform);
        }
        for (int i = 0; i < routeArrows.Count; i++)
        {
            bool visible = route != null && i < route.Length;
            routeArrows[i].gameObject.SetActive(visible);
            if (!visible) continue;
            Vector3 tangent; float width;
            Vector3 point = CarpetPoint(route[i].point, out tangent, out width);
            if (width <= 0) { routeArrows[i].gameObject.SetActive(false); continue; }
            Vector3 heading = route[i].forward;
            // The final arrow shares the task card's actual standing-space target.
            // Prefab navigation anchors are not the exhibit / arrival positions in BasicScene.
            var activeGuide = coordinator.GetComponent<RegionGuide>();
            if (i == route.Length - 1 && activeGuide && activeGuide.HasDestination)
            {
                Vector3 toArrival = Vector3.ProjectOnPlane(activeGuide.DestinationPoint - point, Vector3.up);
                if (toArrival.sqrMagnitude > .04f) heading = toArrival.normalized;
            }
            routeArrows[i].SetPositionAndRotation(point, Quaternion.LookRotation(heading, Vector3.up));
            float size = Mathf.Min(.65f, width * .7f / 2f);
            var parentScale = visuals.transform.lossyScale;
            routeArrows[i].localScale = new Vector3(size / Mathf.Abs(parentScale.x), size / Mathf.Abs(parentScale.y), size / Mathf.Abs(parentScale.z));
        }
        material.color = arrowColor;
    }

    Vector3 CarpetPoint(Vector3 desired, out Vector3 tangent, out float availableWidth)
    {
        if (carpetSurfaces == null)
        {
            var found = new System.Collections.Generic.List<Renderer>();
            foreach (var renderer in FindObjectsOfType<Renderer>())
                for (var parent = renderer.transform; parent; parent = parent.parent)
                    if (parent.name.ToLowerInvariant().Contains("carpet")) { found.Add(renderer); break; }
            carpetSurfaces = found.ToArray();
        }
        float best = float.PositiveInfinity; Vector3 result = desired;
        tangent = Vector3.forward; availableWidth = 0f;
        foreach (var surface in carpetSurfaces)
        {
            if (!surface || !surface.enabled || !surface.gameObject.activeInHierarchy) continue;
            var filter = surface.GetComponent<MeshFilter>();
            if (!filter || !filter.sharedMesh) continue;
            // Work in mesh space: world AABBs do not describe the edges of a rotated carpet.
            var bounds = filter.sharedMesh.bounds;
            var frame = surface.transform;
            if (Mathf.Abs(Vector3.Dot(frame.up.normalized, Vector3.up)) < .95f) continue;
            float xSize = frame.TransformVector(Vector3.right * bounds.size.x).magnitude;
            float zSize = frame.TransformVector(Vector3.forward * bounds.size.z).magnitude;
            bool alongX = xSize > zSize;
            float worldLength = alongX ? xSize : zSize;
            float worldWidth = alongX ? zSize : xSize;
            if (worldWidth < .3f || worldLength < 1f) continue;
            Vector3 local = frame.InverseTransformPoint(desired);
            Vector3 point = bounds.center;
            float scale = alongX ? frame.TransformVector(Vector3.right).magnitude : frame.TransformVector(Vector3.forward).magnitude;
            float inset = .8f / Mathf.Max(.001f, scale);
            if (alongX)
                point.x = Mathf.Clamp(local.x, bounds.min.x + Mathf.Min(inset,bounds.extents.x), bounds.max.x - Mathf.Min(inset,bounds.extents.x));
            else point.z = Mathf.Clamp(local.z, bounds.min.z + Mathf.Min(inset,bounds.extents.z), bounds.max.z - Mathf.Min(inset,bounds.extents.z));
            point.y = bounds.max.y;
            Vector3 world = frame.TransformPoint(point) + Vector3.up * .025f;
            float distance = new Vector2(world.x-desired.x,world.z-desired.z).sqrMagnitude;
            if (distance < best)
            {
                best = distance; result = world; availableWidth = worldWidth;
                tangent = Vector3.ProjectOnPlane(frame.TransformDirection(alongX ? Vector3.right : Vector3.forward),Vector3.up).normalized;
            }
        }
        return result;
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
        var nextArrow = new GameObject("Act 3 approach arrow", typeof(MeshFilter), typeof(MeshRenderer));
        nextArrow.hideFlags = HideFlags.HideAndDontSave;
        nextArrow.transform.SetParent(visuals.transform, false);
        nextArrow.GetComponent<MeshFilter>().sharedMesh = mesh;
        nextArrow.GetComponent<MeshRenderer>().sharedMaterial = material;
        continuation = nextArrow.transform;
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
        routeArrows.Clear(); carpetSurfaces = null;
    }
    static void Dispose(Object item)
    {
        if (!item) return;
        if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
    }
}
