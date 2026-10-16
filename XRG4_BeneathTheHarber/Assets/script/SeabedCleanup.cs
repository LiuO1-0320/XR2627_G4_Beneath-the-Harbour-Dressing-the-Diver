using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Pick up litter with either hand and release it into the seabed collection basket.</summary>
[ExecuteAlways]
public class SeabedCleanup : MonoBehaviour, IResettable
{
    public int ResetOrder => 30;
    public void PrepareReset() {  }
    public void RestoreReset() { ResetForSession(); if(collected!=0) throw new System.InvalidOperationException("Cleanup count was not cleared."); }

    public Transform seabed;
    class Litter
    {
        public Transform root;
        public Vector3 start;
        public Quaternion rotation;
        public Rigidbody body;
        public XRGrabInteractable grab;
        public bool held, collected;
    }
    readonly List<Litter> litter = new List<Litter>();
    readonly List<Material> materials = new List<Material>();
    GameObject generated;
    Transform basket, panel;
    Font font;
    Text progress;
    Button resetButton;
    int collected;
    public Transform CollectionTarget => basket;
    public int CollectedCount => collected;
    public int TotalCount => litter.Count;
    float Bottom => seabed ? transform.InverseTransformPoint(seabed.TransformPoint(Vector3.up * .5f)).y : -6.41f;
    HideFlags Flags => Application.isPlaying ? HideFlags.None : HideFlags.HideAndDontSave;
    void OnEnable() { Build(); }
    void Build()
    {
        if (generated) return;
        generated = new GameObject("Seabed cleanup activity (generated)") { hideFlags = Flags };
        generated.transform.SetParent(transform, false);
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 40);
        var metal = Mat(new Color(.22f, .3f, .31f));
        var teal = Mat(new Color(.06f, .48f, .43f));
        var plastic = Mat(new Color(.38f, .68f, .73f));
        var cap = Mat(new Color(.92f, .49f, .12f));
        var red = Mat(new Color(.67f, .19f, .08f));
        var label = Mat(new Color(.84f, .79f, .62f));
        var g = new GameObject("海底回收篮 / Collection basket") { hideFlags = Flags };
        g.transform.SetParent(generated.transform, false); basket = g.transform;
        basket.localPosition = new Vector3(4, Bottom, 1.5f);
        Shape(basket, "Basket floor", PrimitiveType.Cube, new Vector3(0, .12f, 0), new Vector3(1.5f, .2f, 1.3f), teal, true);
        for (int side = -1; side <= 1; side += 2)
        {
            Shape(basket, "Basket side", PrimitiveType.Cube, new Vector3(side * .72f, .5f, 0), new Vector3(.07f, .8f, 1.3f), teal, true);
            Shape(basket, "Basket end", PrimitiveType.Cube, new Vector3(0, .5f, side * .62f), new Vector3(1.5f, .8f, .07f), teal, true);
        }
        for (int i = 0; i < 7; i++)
            Shape(basket, "Basket metal rib", PrimitiveType.Cube, new Vector3(-.6f + i * .2f, .52f, -.663f), new Vector3(.025f, .65f, .025f), metal);
        Vector2[] spots = { new Vector2(6, -2), new Vector2(8, 0), new Vector2(10, -3),
            new Vector2(12, -1), new Vector2(7, -4), new Vector2(11, 1),
            new Vector2(14, -3), new Vector2(9, -4.5f), new Vector2(13, 0) };
        for (int i = 0; i < spots.Length; i++)
        {
            var root = new GameObject(i % 3 == 0 ? "废弃塑料瓶 / Plastic bottle" : i % 3 == 1 ? "废弃饮料罐 / Can" : "废弃包装 / Wrapper") { hideFlags = Flags };
            root.transform.SetParent(generated.transform, false);
            root.transform.localPosition = new Vector3(spots[i].x, Bottom + .26f, spots[i].y);
            if (i % 3 == 0)
            {
                Shape(root.transform, "Bottle body", PrimitiveType.Capsule, Vector3.zero, new Vector3(.19f, .23f, .19f), plastic);
                Shape(root.transform, "Bottle neck", PrimitiveType.Cylinder, new Vector3(0, .25f, 0), new Vector3(.1f, .05f, .1f), plastic);
                Shape(root.transform, "Bottle cap", PrimitiveType.Cylinder, new Vector3(0, .31f, 0), new Vector3(.11f, .02f, .11f), cap);
                root.transform.localRotation = Quaternion.Euler(0, i * 37, 90);
            }
            else if (i % 3 == 1)
            {
                Shape(root.transform, "Can", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.22f, .15f, .22f), red);
                Shape(root.transform, "Can lid", PrimitiveType.Cylinder, new Vector3(0, .154f, 0), new Vector3(.23f, .008f, .23f), metal);
                root.transform.localRotation = Quaternion.Euler(90, i * 31, 0);
            }
            else
                Shape(root.transform, "Discarded packaging", PrimitiveType.Cube, Vector3.zero, new Vector3(.38f, .1f, .27f), label);
            // One root collider keeps compound visuals easy to grab and prevents counting child meshes twice.
            var collider = root.AddComponent<BoxCollider>();
            collider.size = i % 3 == 0 ? new Vector3(.21f, .68f, .21f) : i % 3 == 1 ? new Vector3(.24f, .33f, .24f) : new Vector3(.38f, .1f, .27f);
            var item = new Litter { root = root.transform, start = root.transform.localPosition, rotation = root.transform.localRotation };
            if (Application.isPlaying)
            {
                item.body = root.AddComponent<Rigidbody>(); item.body.mass = .15f; item.body.drag = 3;
                item.body.angularDrag = 3; item.body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                item.grab = root.AddComponent<XRGrabInteractable>();
                item.grab.colliders.Clear(); item.grab.colliders.Add(collider);
                item.grab.useDynamicAttach = true; item.grab.throwOnDetach = false;
                item.grab.retainTransformParent = true; item.grab.movementType = XRBaseInteractable.MovementType.Kinematic;
                item.grab.selectEntered.AddListener(args => item.held = true);
            }
            litter.Add(item);
        }
        BuildPanel(); RefreshProgress();
    }
    void Update()
    {
        if (!generated) Build();
        if (panel && Camera.main)
        {
            Vector3 facing = panel.position - Camera.main.transform.position; facing.y = 0;
            if (facing.sqrMagnitude > .001f) panel.rotation = Quaternion.LookRotation(facing);
        }
        if (!Application.isPlaying) return;
        var current = StageCoordinator.Current;
        if (resetButton) resetButton.interactable = !current ||
            (current.Controller.Session.Status == SessionStatus.Running && current.Controller.Session.Stage == ExperienceStage.FreeExploration);
        if (!StageCoordinator.Allows(ExperienceStage.SeabedCleanup)) return;
        if (current && current.Controller.Session.Stage == ExperienceStage.SeabedCleanup && current.Controller.Session.CleanupResolved)
        {
            progress.text = current.Controller.Session.CleanupCompleted
                ? "全部回收完成！请上浮返回陆地平台\nALL COLLECTED - RETURN TO THE LAND PLATFORM"
                : "工作人员已跳过回收，请上浮返回陆地平台\nCOLLECTION SKIPPED - RETURN TO THE LAND PLATFORM";
            return;
        }
        foreach (var item in litter)
        {
            if (item.collected || !item.grab) continue;
            if (item.grab.isSelected) { item.held = true; continue; }
            Vector3 inBasket = basket.InverseTransformPoint(item.root.position);
            // Release inside the open basket, including a small area above its rim.
            if (item.held && Mathf.Abs(inBasket.x) < .65f && Mathf.Abs(inBasket.z) < .55f && inBasket.y > .2f && inBasket.y < 1.5f)
            {
                item.collected = true; collected++;
                SessionTelemetry.Report("LitterCollected", item.root, collected.ToString());
                item.root.gameObject.SetActive(false); RefreshProgress(); continue;
            }
            Vector3 position = generated.transform.InverseTransformPoint(item.root.position);
            if (position.y < Bottom - 1 || Vector3.Distance(position, item.start) > 35) ReturnItem(item);
        }
        var coordinator = StageCoordinator.Current;
        if (coordinator)
            coordinator.Controller.Session.ReportCleanupProgress(coordinator.Controller.Session.SessionId, collected, litter.Count);
    }
    public void ResetCleanup()
    {
        // Guided progress cannot be erased mid-task. Replay is available in free exploration.
        var coordinator = StageCoordinator.Current;
        if (Application.isPlaying && coordinator && coordinator.Controller.Session.Status != SessionStatus.Idle
            && !(coordinator.Controller.Session.Status == SessionStatus.Running
                && coordinator.Controller.Session.Stage == ExperienceStage.FreeExploration)) return;
        foreach (var item in litter)
            if (item.grab && item.grab.isSelected) { progress.text = "请先松开手里的垃圾，再重置\nRelease the item before resetting"; return; }
        collected = 0;
        foreach (var item in litter) { item.collected = item.held = false; ReturnItem(item); }
        RefreshProgress();
    }
    public void ResetForSession()
    {
        if (!generated) return;
        // Cancel selection before restoring transforms; the public visitor reset still asks for release.
        var enabledGrabs = new List<XRGrabInteractable>();
        foreach (var item in litter)
            if (item.grab && item.grab.enabled) { enabledGrabs.Add(item.grab); item.grab.enabled = false; }
        try
        {
            foreach (var item in litter)
                if (item.grab && item.grab.isSelected) throw new System.InvalidOperationException("Cleanup item is still selected after cancellation.");
            ResetCleanup();
        }
        finally { foreach (var grab in enabledGrabs) if (grab) grab.enabled = true; }
    }
    void ReturnItem(Litter item)
    {
        item.root.gameObject.SetActive(true);
        item.root.SetParent(generated.transform, false);
        item.root.localPosition = item.start; item.root.localRotation = item.rotation;
        if (item.body) { item.body.velocity = Vector3.zero; item.body.angularVelocity = Vector3.zero; }
    }
    void RefreshProgress()
    {
        progress.text = collected == litter.Count ? "全部回收完成！请上浮到水面\nALL LITTER COLLECTED - ASCEND TO THE SURFACE" :
            "已回收 / Collected: " + collected + " / " + litter.Count;
    }
    void BuildPanel()
    {
        var ui = new GameObject("海底清理说明 / Cleanup instructions", typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster)) { hideFlags = Flags };
        ui.transform.SetParent(generated.transform, false); panel = ui.transform;
        panel.localPosition = basket.localPosition + Vector3.up * 2;
        panel.localScale = Vector3.one * .0014f;
        ui.GetComponent<RectTransform>().sizeDelta = new Vector2(1300, 660);
        ui.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        Graphic<Image>(panel, "Background", Vector2.zero, new Vector2(1300, 660)).color = new Color(.015f, .1f, .12f, .75f);
        Label(panel, "海底捡垃圾 / SEABED CLEANUP", new Vector2(0, 245), 42);
        Label(panel, "握住垃圾捡起，放到回收篮内松手\nHold grip to pick up litter; release it inside the basket", new Vector2(0, 110), 32);
        progress = Label(panel, "", new Vector2(0, -45), 38);
        var image = Graphic<Image>(panel, "Reset collection", new Vector2(0, -225), new Vector2(700, 90));
        image.color = new Color(.05f, .38f, .34f); image.raycastTarget = true;
        var button = image.gameObject.AddComponent<QuickTravelPressButton>(); button.targetGraphic = image; resetButton = button;
        button.onClick.AddListener(ResetCleanup);
        Label(image.transform, "重新收集 / Reset cleanup", Vector2.zero, 32);
    }
    T Graphic<T>(Transform parent, string name, Vector2 p, Vector2 size) where T : Graphic
    {
        var g = new GameObject(name, typeof(RectTransform)) { hideFlags = Flags }; g.transform.SetParent(parent, false);
        var rect = g.GetComponent<RectTransform>(); rect.anchoredPosition = p; rect.sizeDelta = size;
        var graphic = g.AddComponent<T>(); graphic.raycastTarget = false; return graphic;
    }
    Text Label(Transform parent, string words, Vector2 p, int size)
    {
        var text = Graphic<Text>(parent, "Caption", p, new Vector2(1200, 140));
        text.text = words; text.font = font; text.fontSize = size; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; return text;
    }
    void Shape(Transform parent, string name, PrimitiveType type, Vector3 p, Vector3 scale, Material material, bool solid = false)
    {
        var g = GameObject.CreatePrimitive(type); g.name = name; g.hideFlags = Flags;
        g.transform.SetParent(parent, false); g.transform.localPosition = p; g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = material; g.GetComponent<Collider>().enabled = solid;
    }
    Material Mat(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit"); if (!shader) shader = Shader.Find("Standard");
        var material = new Material(shader) { color = color, hideFlags = HideFlags.HideAndDontSave };
        materials.Add(material); return material;
    }
    void OnDisable()
    {
        Dispose(generated); Dispose(font); foreach (var material in materials) Dispose(material);
        materials.Clear(); litter.Clear(); generated = null; basket = panel = null; collected = 0;
    }
    static void Dispose(Object item)
    { if (!item) return; if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
}
