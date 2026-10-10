using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Static gallery and working diver's room, leaving the interactive exhibits accessible.</summary>
[ExecuteAlways]
public class HarbourSceneDressing : MonoBehaviour
{
    public Transform act1, act2, act3;
    public Font font;
    GameObject scenery;
    Font fallback;
    readonly List<Material> materials = new List<Material>();
    readonly List<Mesh> combinedMeshes = new List<Mesh>();
    Material ivory, ink, brass, wood, steel, blue, canvas, glass, glow;
    HideFlags Flags => Application.isPlaying ? HideFlags.None : HideFlags.HideAndDontSave;

    void OnEnable() { Build(); }
    void Update() { if (!scenery) Build(); }
    [ContextMenu("Rebuild museum and workshop")]
    public void Rebuild() { Clear(); Build(); }
    void Build()
    {
        if (scenery) return;
        scenery = new GameObject("Museum and diver workshop (generated)") { hideFlags = Flags };
        scenery.transform.SetParent(transform, false);
        fallback = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 48);
        ivory = Mat(new Color(.8f, .77f, .66f));
        ink = Mat(new Color(.035f, .085f, .105f));
        brass = Mat(new Color(.58f, .36f, .12f), .65f);
        wood = Mat(new Color(.28f, .14f, .065f));
        steel = Mat(new Color(.19f, .23f, .24f), .6f);
        blue = Mat(new Color(.07f, .24f, .28f));
        canvas = Mat(new Color(.55f, .48f, .3f));
        glass = Mat(new Color(.13f, .29f, .32f));
        glow = Mat(new Color(.95f, .75f, .4f));
        glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(.65f, .42f, .17f));
        Museum(); Workshop(); CombineStaticGeometry();
    }

    void Museum()
    {
        // Low, visual-only overlays keep the original teleportable ground and entrances intact.
        Box("Gallery stone floor", new Vector3(144.6f, .012f, -7.8f), new Vector3(18.2f, .015f, 32.8f), ivory);
        Box("Second gallery floor", new Vector3(144.6f, .012f, 14), new Vector3(18.2f, .015f, 10.5f), ivory);
        WallPanel(new Vector3(153.94f, 2, -16), new Vector3(.08f, 3.8f, 15), 270,
            "01   铜盔与海港\nTHE HELMET & THE HARBOUR", "从岸上开始，认识水下工作的装备。\nExplore the equipment before entering the water.");
        WallPanel(new Vector3(135.47f, 2, 9.5f), new Vector3(.08f, 3.8f, 17), 90,
            "02   装备的语言\nREADING THE EQUIPMENT", "观察视窗、连接与配重。\nLook closely at viewports, connections and weights.");
        WallPanel(new Vector3(145.5f, 2, -24.72f), new Vector3(15, 3.8f, .08f), 0,
            "BENEATH THE HARBOUR", "海港之下 · 潜水装备探索馆\nA journey from the gallery to the seabed");
        // Gold seams and lower wall courses bring the very tall existing walls down to human scale.
        Box("Gallery east lower course", new Vector3(153.84f, .28f, -16), new Vector3(.13f, .5f, 15), ink);
        Box("Gallery west lower course", new Vector3(135.57f, .28f, 9.5f), new Vector3(.13f, .5f, 17), ink);
        for (int i = 0; i < 7; i++)
            Box("Stone joint", new Vector3(144.6f, .025f, -23 + i * 6), new Vector3(18.1f, .004f, .025f), brass);
        Sign("Museum entrance", new Vector3(132.6f, 2.25f, -6.6f), 270, 2.8f, 1.1f,
            "海港潜水装备馆\nHARBOUR DIVING GALLERY", ink, 38);
        Sign("Entrance route", new Vector3(133.5f, 1.1f, -6.6f), 270, 1.7f, .55f,
            "01 展品探索 →\nBegin your visit", ivory, 32);
        Box("Entrance sign post", new Vector3(132.7f, 1, -6.6f), new Vector3(.1f, 2, .1f), brass, true);
        Bench(new Vector3(139, 0, -22.5f), 0);
        Bench(new Vector3(137.3f, 0, 13), 90);
        Case(new Vector3(137.3f, 0, -15), 90, "连接与配重 / CONNECTIONS & WEIGHTS", false);
        Case(new Vector3(145, 0, 17.8f), 180, "绳索与信号 / ROPES & SIGNALS", true);
        if (act1) Exhibit(act1.position, "01", "铜制潜水头盔\nDIVING HELMET", "靠近观察 · 抓取探索\nApproach, inspect and handle");
        if (act2) Exhibit(act2.position, "02", "潜水装备探索\nEQUIPMENT STUDY", "观察部件 · 尝试互动\nInspect the parts and interact");
        // Wall-mounted route marks, avoiding freestanding barriers across the doorway.
        Sign("Workshop transition", new Vector3(151, 2.5f, 19.39f), 180, 4, .8f,
            "展览之后：走进潜水员的工作室 →\nNEXT: THE DIVER'S WORKSHOP", ink, 32);
        for (int i = 0; i < 4; i++)
        {
            float z = -21 + i * 7;
            Box("Gallery brass wall rail", new Vector3(153.78f, 3.85f, z), new Vector3(.15f, .08f, 3.8f), brass);
            Box("Warm gallery wall light", new Vector3(153.69f, 3.8f, z), new Vector3(.035f, .035f, 3.6f), glow);
        }
    }

    void Workshop()
    {
        // Props sit along the back wall; the diver leads through the open space in front.
        Box("Workshop rear timber lining", new Vector3(178, 1.5f, 19.38f), new Vector3(25, 2.9f, .1f), wood);
        for (int i = 0; i < 26; i++)
            Box("Timber lining seam", new Vector3(165.5f + i, 1.5f, 19.3f), new Vector3(.025f, 2.9f, .02f), steel);
        Box("Workshop upper metal rail", new Vector3(178, 3, 19.2f), new Vector3(25, .12f, .12f), steel);
        Sign("Workshop identity", new Vector3(172, 3.9f, 19.1f), 180, 6, 1,
            "潜水员工作室\nDIVER'S WORKSHOP", ink, 44);
        Workbench(new Vector3(171, 0, 18.2f));
        EquipmentRack(new Vector3(178, 0, 18.4f));
        Pump(new Vector3(184.7f, 0, 18.1f));
        Crate(new Vector3(188.2f, 0, 18.2f));
        Crate(new Vector3(189.3f, 0, 18.2f));
        Coil(new Vector3(187.9f, .85f, 18.2f), .32f, 5, canvas);
        Sign("Equipment checklist", new Vector3(171, 2.15f, 19.14f), 180, 3, 1.35f,
            "下潜前检查 / BEFORE THE DIVE\n01 头盔与视窗 / Helmet & viewports\n02 连接与供气 / Connections & air\n03 配重与绳索 / Weights & rope", ivory, 32);
        Sign("Air supply board", new Vector3(184.8f, 2.55f, 19.14f), 180, 3.2f, .8f,
            "供气与软管 / AIR & HOSES\n收好工具，准备出发 / Stow tools, prepare to dive", ivory, 30);
        if (act3)
        {
            Vector3 p = act3.position; p.y = .03f;
            Box("Dressing station mat", p + new Vector3(0, 0, -1.2f), new Vector3(5, .02f, 3.8f), blue);
            Sign("Dressing station wall sign", new Vector3(p.x, 2.7f, 19.1f), 180, 3.3f, .75f,
                "03 装备准备 / SUITING UP\n帮助潜水员戴好头盔", ivory, 34);
        }
        // The route is suggested by flat brass markers, without physical rails in the escort corridor.
        for (int i = 0; i < 6; i++)
        {
            Vector3 p = new Vector3(169 + i * 5, .025f, 14.8f - i * 1.35f);
            Box("Dive route dash", p, new Vector3(.7f, .007f, .12f), brass);
        }
        Sign("Dive departure marker", new Vector3(195.8f, 2.25f, 16.7f), 180, 3.1f, .8f,
            "04 下潜平台 →\nDIVE DEPARTURE", ink, 38);
        Box("Departure sign upright", new Vector3(195.8f, 1.05f, 16.8f), new Vector3(.12f, 2.1f, .12f), steel, true);
    }

    void WallPanel(Vector3 p, Vector3 size, float yaw, string title, string caption)
    {
        Box("Museum exhibit wall", p, size, ivory);
        Vector3 outward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        Sign("Gallery wall title", p + Vector3.up * .9f + outward * .08f, yaw, 5, .9f, title, ink, 46);
        Sign("Gallery wall introduction", p - Vector3.up * .2f + outward * .08f, yaw, 4.8f, .8f, caption, ivory, 34);
    }
    void Exhibit(Vector3 p, string number, string title, string caption)
    {
        // Existing helmets stay uncovered and retain their original colliders and grab interaction.
        Vector3 label = new Vector3(p.x + 1.75f, 1.25f, p.z - 1.15f);
        Box("Exhibit label pedestal", label - Vector3.up * .62f, new Vector3(.1f, 1.2f, .1f), brass, true);
        Sign("Exhibit caption " + number, label, 180, 1.35f, .9f, number + "  " + title + "\n" + caption, ink, 29);
        Box("Exhibit label base", new Vector3(label.x, .05f, label.z), new Vector3(.55f, .1f, .55f), ink, true);
    }
    void Bench(Vector3 p, float yaw)
    {
        var group = Group("Gallery visitor bench", p, yaw);
        Shape(group, "Timber seat", PrimitiveType.Cube, new Vector3(0, .48f, 0), new Vector3(2.6f, .12f, .65f), wood, true);
        foreach (float x in new[] { -.95f, .95f })
            Shape(group, "Brass bench leg", PrimitiveType.Cube, new Vector3(x, .23f, 0), new Vector3(.12f, .46f, .5f), brass, true);
    }
    void Case(Vector3 p, float yaw, string caption, bool rope)
    {
        var g = Group("Museum open display cabinet", p, yaw);
        Shape(g, "Dark display plinth", PrimitiveType.Cube, new Vector3(0, .45f, 0), new Vector3(2.5f, .9f, 1), ink, true);
        Shape(g, "Brass display lip", PrimitiveType.Cube, new Vector3(0, .92f, 0), new Vector3(2.55f, .06f, 1.05f), brass);
        Shape(g, "Linen display bed", PrimitiveType.Cube, new Vector3(0, .96f, 0), new Vector3(2.42f, .02f, .92f), canvas);
        // A framed blue back makes a readable cabinet silhouette without opaque glass over the exhibits.
        Shape(g, "Cabinet back", PrimitiveType.Cube, new Vector3(0, 1.28f, .46f), new Vector3(2.5f, .65f, .04f), glass);
        foreach (float x in new[] { -1.24f, 1.24f })
            Shape(g, "Cabinet frame", PrimitiveType.Cube, new Vector3(x, 1.28f, .46f), new Vector3(.035f, .7f, .05f), brass);
        if (rope)
        {
            Coil(scenery.transform.InverseTransformPoint(g.TransformPoint(new Vector3(-.45f, 1, 0))), .3f, 4, canvas);
            Shape(g, "Signal block", PrimitiveType.Cube, new Vector3(.65f, 1.08f, 0), new Vector3(.45f, .22f, .35f), wood);
        }
        else
        {
            foreach (float x in new[] { -.7f, 0, .7f })
            {
                Shape(g, "Display weight", PrimitiveType.Cube, new Vector3(x, 1.06f, 0), new Vector3(.4f, .19f, .3f), steel);
                Shape(g, "Brass connection", PrimitiveType.Cylinder, new Vector3(x, 1.2f, 0), new Vector3(.17f, .045f, .17f), brass);
            }
        }
        Sign("Cabinet explanation", g.TransformPoint(new Vector3(0, .64f, -.515f)), yaw + 180, 2.3f, .4f, caption, ink, 27);
    }
    void Workbench(Vector3 p)
    {
        Box("Heavy timber worktop", p + Vector3.up * .95f, new Vector3(5, .16f, 1.25f), wood, true);
        foreach (float x in new[] { -2.1f, 2.1f })
            Box("Workbench trestle", p + new Vector3(x, .45f, 0), new Vector3(.18f, .9f, 1), steel, true);
        Box("Workbench shelf", p + Vector3.up * .26f, new Vector3(4.5f, .1f, 1), wood);
        for (int i = 0; i < 3; i++)
        {
            Vector3 drawer = p + new Vector3(-1.4f + i * 1.4f, .65f, -.45f);
            Box("Tool drawer", drawer, new Vector3(1.2f, .4f, .32f), blue);
            Box("Drawer brass handle", drawer + new Vector3(0, 0, -.18f), new Vector3(.25f, .05f, .05f), brass);
        }
        Box("Folded equipment cloth", p + new Vector3(-1.5f, 1.09f, 0), new Vector3(.8f, .1f, .55f), canvas);
        Box("Inspection chart", p + new Vector3(.1f, 1.04f, -.1f), new Vector3(.8f, .012f, .55f), ivory);
        for (int i = 0; i < 3; i++)
            Box("Chart lines", p + new Vector3(.1f, 1.052f, -.25f + i * .14f), new Vector3(.65f, .006f, .015f), blue);
        Box("Spanner shaft", p + new Vector3(1.2f, 1.05f, 0), new Vector3(.6f, .05f, .09f), steel);
        Box("Spanner jaw upper", p + new Vector3(1.48f, 1.05f, .08f), new Vector3(.16f, .05f, .09f), steel);
        Box("Spanner jaw lower", p + new Vector3(1.48f, 1.05f, -.08f), new Vector3(.16f, .05f, .09f), steel);
        Coil(p + new Vector3(1.8f, 1.05f, .1f), .24f, 3, ink);
    }
    void EquipmentRack(Vector3 p)
    {
        foreach (float x in new[] { -1.7f, 1.7f })
            Box("Equipment rack upright", p + new Vector3(x, 1.3f, 0), new Vector3(.09f, 2.6f, .09f), steel, true);
        Box("Equipment hanging rail", p + Vector3.up * 2.55f, new Vector3(3.5f, .09f, .1f), brass);
        Box("Equipment rack base", p + Vector3.up * .1f, new Vector3(3.6f, .2f, .85f), steel, true);
        for (int i = 0; i < 3; i++)
        {
            Vector3 tank = p + new Vector3(-1.05f + i * 1.05f, .92f, -.1f);
            Shape(scenery.transform, "Air cylinder", PrimitiveType.Capsule, tank, new Vector3(.38f, .75f, .38f), blue, true);
            Box("Tank retaining band", tank + Vector3.up * .2f, new Vector3(.42f, .08f, .42f), steel);
            Shape(scenery.transform, "Cylinder valve", PrimitiveType.Cylinder, tank + Vector3.up * .79f, new Vector3(.12f, .06f, .12f), brass);
        }
        Sign("Rack label", p + new Vector3(0, 2.1f, -.12f), 180, 3.1f, .4f, "装备归位 / EQUIPMENT STORAGE", ink, 29);
    }
    void Pump(Vector3 p)
    {
        Box("Air supply machine base", p + Vector3.up * .35f, new Vector3(2.2f, .7f, 1.1f), blue, true);
        Box("Pump top plate", p + Vector3.up * .75f, new Vector3(2.3f, .12f, 1.2f), steel);
        foreach (float x in new[] { -.6f, .6f })
        {
            Shape(scenery.transform, "Pump piston", PrimitiveType.Cylinder, p + new Vector3(x, 1.16f, 0), new Vector3(.32f, .38f, .32f), brass);
            Shape(scenery.transform, "Piston cap", PrimitiveType.Cylinder, p + new Vector3(x, 1.57f, 0), new Vector3(.4f, .04f, .4f), steel);
        }
        var gauge = Shape(scenery.transform, "Round pressure gauge", PrimitiveType.Cylinder, p + new Vector3(0, 1.24f, -.32f), new Vector3(.44f, .035f, .44f), ivory);
        gauge.transform.localRotation = Quaternion.Euler(90, 0, 0);
        Box("Pressure gauge needle", p + new Vector3(.04f, 1.28f, -.36f), new Vector3(.025f, .2f, .018f), ink);
        Coil(p + new Vector3(0, .045f, -1), .65f, 5, ink);
    }
    void Crate(Vector3 p)
    {
        Box("Supply crate", p + Vector3.up * .4f, new Vector3(.95f, .8f, .85f), wood, true);
        foreach (float x in new[] { -.31f, .31f })
            Box("Crate metal band", p + new Vector3(x, .405f, 0), new Vector3(.045f, .82f, .88f), steel);
        Sign("Crate stencil", p + new Vector3(0, .4f, -.431f), 180, .52f, .25f, "DIVE\nTOOLS", ivory, 28);
    }
    void Coil(Vector3 p, float radius, int turns, Material material)
    {
        var item = new GameObject("Coiled rope or hose") { hideFlags = Flags };
        item.transform.SetParent(scenery.transform, false);
        var line = item.AddComponent<LineRenderer>(); line.useWorldSpace = false;
        line.sharedMaterial = material; line.widthMultiplier = .035f; line.numCornerVertices = 2;
        line.positionCount = turns * 40 + 1;
        for (int i = 0; i < line.positionCount; i++)
        {
            float t = (float)i / (line.positionCount - 1);
            float angle = t * turns * Mathf.PI * 2, r = Mathf.Lerp(radius * .45f, radius, t);
            line.SetPosition(i, p + new Vector3(Mathf.Cos(angle) * r, .02f, Mathf.Sin(angle) * r));
        }
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    Transform Group(string name, Vector3 p, float yaw)
    {
        var g = new GameObject(name) { hideFlags = Flags }; g.transform.SetParent(scenery.transform, false);
        g.transform.localPosition = p; g.transform.localRotation = Quaternion.Euler(0, yaw, 0); return g.transform;
    }
    void Box(string name, Vector3 p, Vector3 size, Material material, bool solid = false)
    { Shape(scenery.transform, name, PrimitiveType.Cube, p, size, material, solid); }
    GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 p, Vector3 size, Material material, bool solid = false)
    {
        var g = GameObject.CreatePrimitive(type); g.name = name; g.hideFlags = Flags;
        g.transform.SetParent(parent, false); g.transform.localPosition = p; g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = material; g.GetComponent<Collider>().enabled = solid;
        return g;
    }
    void Sign(string name, Vector3 p, float yaw, float width, float height, string words, Material backing, int fontSize)
    {
        var g = Group(name, p, yaw);
        Shape(g, "Sign frame", PrimitiveType.Cube, Vector3.zero, new Vector3(width + .08f, height + .08f, .045f), brass);
        Shape(g, "Sign face", PrimitiveType.Cube, new Vector3(0, 0, .026f), new Vector3(width, height, .016f), backing);
        var ui = new GameObject("Fixed exhibit text", typeof(RectTransform), typeof(Canvas)) { hideFlags = Flags };
        ui.transform.SetParent(g, false); ui.transform.localPosition = new Vector3(0, 0, .04f);
        // UI's readable front faces local -Z; rotate it to the sign's outward +Z face.
        ui.transform.localRotation = Quaternion.Euler(0, 180, 0);
        ui.transform.localScale = Vector3.one * .002f;
        ui.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ui.GetComponent<RectTransform>().sizeDelta = new Vector2(width * 500, height * 500);
        var textObject = new GameObject("Caption", typeof(RectTransform), typeof(Text)) { hideFlags = Flags };
        textObject.transform.SetParent(ui.transform, false);
        var rect = textObject.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20, 12); rect.offsetMax = new Vector2(-20, -12);
        var label = textObject.GetComponent<Text>(); label.font = font ? font : fallback; label.text = words;
        label.fontSize = fontSize; label.alignment = TextAnchor.MiddleCenter;
        label.color = backing == ivory ? new Color(.035f, .085f, .105f) : new Color(.96f, .9f, .74f);
        label.raycastTarget = false;
    }
    Material Mat(Color color, float metallic = 0)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit"); if (!shader) shader = Shader.Find("Standard");
        var mat = new Material(shader) { color = color, hideFlags = HideFlags.HideAndDontSave };
        mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Smoothness", .25f); materials.Add(mat); return mat;
    }
    void CombineStaticGeometry()
    {
        // Merge only decorative meshes. Original exhibits, UI, hoses and prop colliders remain independent.
        var groups = new Dictionary<Material, List<CombineInstance>>();
        foreach (var renderer in scenery.GetComponentsInChildren<MeshRenderer>())
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (!filter || !filter.sharedMesh || !renderer.sharedMaterial) continue;
            if (!groups.TryGetValue(renderer.sharedMaterial, out var instances))
            { instances = new List<CombineInstance>(); groups.Add(renderer.sharedMaterial, instances); }
            instances.Add(new CombineInstance {
                mesh = filter.sharedMesh,
                transform = scenery.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix
            });
            renderer.enabled = false;
        }
        foreach (var pair in groups)
        {
            var mesh = new Mesh { name = "Static scenery batch", hideFlags = HideFlags.HideAndDontSave,
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(pair.Value.ToArray(), true, true); combinedMeshes.Add(mesh);
            var batch = new GameObject("Batched " + pair.Key.color, typeof(MeshFilter), typeof(MeshRenderer)) { hideFlags = Flags };
            batch.transform.SetParent(scenery.transform, false);
            batch.GetComponent<MeshFilter>().sharedMesh = mesh;
            batch.GetComponent<MeshRenderer>().sharedMaterial = pair.Key;
        }
    }
    void OnDisable() { Clear(); }
    void Clear()
    {
        Dispose(scenery); scenery = null;
        foreach (var mesh in combinedMeshes) Dispose(mesh); combinedMeshes.Clear();
        foreach (var material in materials) Dispose(material); materials.Clear(); Dispose(fallback); fallback = null;
    }
    static void Dispose(Object item)
    { if (!item) return; if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
}
