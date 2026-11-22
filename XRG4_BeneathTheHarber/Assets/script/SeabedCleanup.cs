using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Hand and head-gaze inputs share the physical seabed litter and basket.</summary>
[ExecuteAlways]
public class SeabedCleanup : MonoBehaviour, IResettable
{
    public int ResetOrder => 30;
    public void PrepareReset() { CancelCarry(); }
    public void RestoreReset() { ResetForSession(); if(collected!=0) throw new System.InvalidOperationException("Cleanup count was not cleared."); }

    public Transform seabed;
    [Tooltip("Maximum distance from the headset to a piece of litter or the collection basket, in metres.")]
    [Min(.5f)] public float gazeReach = 3f;
    class Litter
    {
        public Transform root;
        public Vector3 start;
        public Quaternion rotation;
        public Rigidbody body;
        public XRGrabInteractable grab;
        public bool collected, handSelected;
        public Vector3? releasePosition;
        public int lastReleaseFrame = -10;
        public System.Guid pickupActivity;
        public ExperienceStage pickupStage;
        public Renderer[] renderers;
        public MaterialPropertyBlock[] originalProperties;
        public bool highlighted;
    }
    readonly List<Litter> litter = new List<Litter>();
    readonly List<Material> materials = new List<Material>();
    GameObject generated;
    Transform basket, panel;
    Font font;
    Text progress;
    Button resetButton;
    int collected;
    Litter carried;
    bool carriedGravity, carriedCollisions;
    internal Transform InteractionRoot => generated ? generated.transform : null;
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
                item.renderers = root.GetComponentsInChildren<Renderer>();
                item.originalProperties = new MaterialPropertyBlock[item.renderers.Length];
                for (int j = 0; j < item.renderers.Length; j++)
                {
                    item.originalProperties[j] = new MaterialPropertyBlock();
                    item.renderers[j].GetPropertyBlock(item.originalProperties[j]);
                }
                item.grab.hoverEntered.AddListener(args => RefreshHighlight(item));
                item.grab.hoverExited.AddListener(args => RefreshHighlight(item));
                item.grab.selectFilters.Add(new XRSelectFilterDelegate((interactor, target) => carried == null));
                item.grab.selectEntered.AddListener(args =>
                {
                    item.handSelected = true;
                    item.releasePosition = null;
                    item.pickupActivity = CurrentActivity;
                    item.pickupStage = CurrentStage;
                    SetHighlight(item, false);
                });
                item.grab.selectExited.AddListener(args =>
                {
                    if (!item.handSelected) return;
                    item.handSelected = false;
                    item.lastReleaseFrame = Time.frameCount;
                    // Count only a matching, normal release. A late exit after cancellation owns no item.
                    if (!args.isCanceled && CollectionOpen() && SamePickup(item))
                        item.releasePosition = item.body.position;
                    else
                    {
                        item.releasePosition = null;
                        item.pickupActivity = System.Guid.Empty;
                    }
                    RefreshHighlight(item);
                });
                root.AddComponent<GazeWorldTarget>().Bind(() => CanPickUp(item), () => PickUp(item));
            }
            litter.Add(item);
        }
        if (Application.isPlaying) basket.gameObject.AddComponent<GazeWorldTarget>().Bind(CanDeposit, DepositCarried);
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
        bool canReset = !current || (current.Controller.Session.Status == SessionStatus.Running &&
            current.Controller.Session.Stage == ExperienceStage.FreeExploration);
        if (resetButton && resetButton.interactable != canReset)
        {
            resetButton.interactable = canReset;
            if (canReset) RefreshProgress();
        }
        UpdateCarry();
        if (!StageCoordinator.Allows(ExperienceStage.SeabedCleanup)) return;
        if (current && current.Controller.Session.Stage == ExperienceStage.SeabedCleanup && current.Controller.Session.CleanupResolved)
        {
            if (!current.Controller.Session.CleanupCompleted)
                progress.text = "工作人员已跳过回收，请沿绳上浮返回平台\nCOLLECTION SKIPPED - ASCEND THE ROPE TO THE PLATFORM";
            return;
        }
        foreach (var item in litter)
        {
            if (item.collected || !item.grab) continue;
            RefreshHighlight(item);
            if (item == carried || item.grab.isSelected) continue;
            if (item.releasePosition.HasValue)
            {
                Vector3 positionAtRelease = item.releasePosition.Value;
                item.releasePosition = null;
                if (CollectReleasedItem(item, positionAtRelease)) continue;
            }
            Vector3 position = generated.transform.InverseTransformPoint(item.root.position);
            if (position.y < Bottom - 1 || Vector3.Distance(position, item.start) > 35) ReturnItem(item);
        }
        if (current && current.Controller.Session.CleanupTotal == 0)
            ReportProgress(CurrentActivity);
    }

    System.Guid CurrentActivity => StageCoordinator.Current
        ? StageCoordinator.Current.Controller.Session.ActivityId : System.Guid.Empty;

    ExperienceStage CurrentStage => StageCoordinator.Current
        ? StageCoordinator.Current.Controller.Session.Stage : ExperienceStage.SeabedCleanup;

    bool SamePickup(Litter item) => item.pickupActivity == CurrentActivity && item.pickupStage == CurrentStage;

    bool CollectionOpen()
    {
        var coordinator = StageCoordinator.Current;
        return StageCoordinator.Allows(ExperienceStage.SeabedCleanup) &&
            (!coordinator || CurrentStage == ExperienceStage.FreeExploration || !coordinator.Controller.Session.CleanupResolved);
    }

    bool CanUseGaze() => Application.isPlaying && isActiveAndEnabled && StageCoordinator.Current && Camera.main &&
        GazeInput.Current && GazeInput.Current.Accessible && !DiveRopeStation.IsPlayerTravelling && CollectionOpen();

    bool WithinReach(Vector3 point) => (Camera.main.transform.position - point).sqrMagnitude <= gazeReach * gazeReach;

    bool CanPickUp(Litter item)
    {
        return CanUseGaze() && carried == null && !item.collected && item.grab.isActiveAndEnabled &&
            WithinReach(item.root.position) && !litter.Exists(other => other.grab.isSelected ||
                Time.frameCount - other.lastReleaseFrame <= 1);
    }

    void PickUp(Litter item)
    {
        if (!CanPickUp(item)) return;
        carried = item;
        item.pickupActivity = CurrentActivity;
        item.pickupStage = CurrentStage;
        item.releasePosition = null;
        carriedGravity = item.body.useGravity;
        carriedCollisions = item.body.detectCollisions;
        if (!item.body.isKinematic) { item.body.velocity = Vector3.zero; item.body.angularVelocity = Vector3.zero; }
        item.body.useGravity = false;
        item.body.detectCollisions = false;
        SetHighlight(item, false);
        FollowViewer();
        SessionTelemetry.Report("LitterPickedUp", item.root, "Head gaze");
        RefreshProgress();
    }

    void FollowViewer()
    {
        // Keep the model visible below the gaze ray and within its original reset hierarchy.
        carried.body.position = Camera.main.transform.TransformPoint(new Vector3(.32f, -.3f, .65f));
    }

    void UpdateCarry()
    {
        if (carried == null) return;
        var coordinator = StageCoordinator.Current;
        if (coordinator && coordinator.Controller.Session.Status == SessionStatus.Paused) return;
        if (!CanUseGaze() || !SamePickup(carried))
            CancelCarry();
        else FollowViewer();
    }

    bool CanDeposit() => carried != null && CanUseGaze() && SamePickup(carried) && WithinReach(basket.TransformPoint(Vector3.up * .5f));

    void DepositCarried()
    {
        if (!CanDeposit()) return;
        var item = ReleaseCarry();
        item.body.position = basket.TransformPoint(Vector3.up * .65f);
        CollectReleasedItem(item, item.body.position);
    }

    Litter ReleaseCarry()
    {
        var item = carried;
        carried = null;
        item.body.useGravity = carriedGravity;
        item.body.detectCollisions = carriedCollisions;
        return item;
    }

    void CancelCarry()
    {
        if (carried == null) return;
        ReturnItem(ReleaseCarry());
        RefreshProgress();
    }

    bool IsInsideBasket(Vector3 position)
    {
        Vector3 local = basket.InverseTransformPoint(position);
        return Mathf.Abs(local.x) < .65f && Mathf.Abs(local.z) < .55f && local.y > .2f && local.y < 1.5f;
    }

    bool CollectReleasedItem(Litter item, Vector3 positionAtRelease)
    {
        if (item.collected || item == carried || item.grab.isSelected ||
            !SamePickup(item) || !IsInsideBasket(positionAtRelease)) return false;
        item.collected = true;
        collected++;
        SessionTelemetry.Report("LitterCollected", item.root, collected.ToString());
        item.root.gameObject.SetActive(false);
        RefreshProgress();
        ReportProgress(item.pickupActivity);
        return true;
    }

    void ReportProgress(System.Guid activity)
    {
        var coordinator = StageCoordinator.Current;
        if (coordinator && coordinator.Controller.Session.Stage == ExperienceStage.SeabedCleanup)
            coordinator.Controller.Session.ReportCleanupProgress(activity, collected, litter.Count);
    }

    void RefreshHighlight(Litter item)
    {
        SetHighlight(item, item.grab && item.grab.isActiveAndEnabled && item.grab.isHovered
            && !item.grab.isSelected && !item.collected && StageCoordinator.Allows(ExperienceStage.SeabedCleanup));
    }
    void SetHighlight(Litter item, bool active)
    {
        if (item.highlighted == active || item.renderers == null) return;
        item.highlighted = active;
        for (int i = 0; i < item.renderers.Length; i++)
        {
            if (!item.renderers[i]) continue;
            if (!active) { item.renderers[i].SetPropertyBlock(item.originalProperties[i]); continue; }
            var properties = new MaterialPropertyBlock();
            item.renderers[i].GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", new Color(1f, .85f, .15f, 1f));
            properties.SetColor("_Color", new Color(1f, .85f, .15f, 1f));
            item.renderers[i].SetPropertyBlock(properties);
        }
    }
    internal void CancelHandSelections()
    {
        // XRI temporarily unparents a held object. Cancel it before gating the generated subtree.
        foreach (var item in litter)
            if (item.grab && item.grab.isSelected && item.grab.interactionManager)
                item.grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)item.grab);
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
        RestoreLitter();
    }
    void RestoreLitter()
    {
        collected = 0;
        if (carried != null) ReleaseCarry();
        foreach (var item in litter) { item.collected = false; ReturnItem(item); }
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
            RestoreLitter();
        }
        finally { foreach (var grab in enabledGrabs) if (grab) grab.enabled = true; }
    }
    void ReturnItem(Litter item)
    {
        item.handSelected = false;
        item.releasePosition = null;
        item.lastReleaseFrame = -10;
        item.pickupActivity = System.Guid.Empty;
        item.root.gameObject.SetActive(true);
        item.root.SetParent(generated.transform, false);
        item.root.localPosition = item.start; item.root.localRotation = item.rotation;
        if (item.body)
        {
            item.body.position = item.root.position;
            item.body.rotation = item.root.rotation;
            if (!item.body.isKinematic) { item.body.velocity = Vector3.zero; item.body.angularVelocity = Vector3.zero; }
        }
    }
    void RefreshProgress()
    {
        if (collected == litter.Count)
        {
            progress.text = CurrentStage == ExperienceStage.FreeExploration
                ? "全部回收完成！可以重新收集 / All collected! You can reset the basket."
                : $"{collected} / {litter.Count} 全部回收完成！沿绳上浮返回平台\nALL COLLECTED - ASCEND THE ROPE TO THE PLATFORM";
            return;
        }
        progress.text = $"已回收 / Collected: {collected} / {litter.Count}" +
            (carried != null ? "\n带回篮边，凝视篮壁或篮底 / Look at the basket to deposit" : "");
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
        Label(panel, "凝视垃圾拾起，再到篮边凝视篮壁或篮底。也可以用手柄抓取后在篮内松手。\nLook at litter to pick it up, then at the basket to deposit. Or use grip and release inside.", new Vector2(0, 110), 30);
        progress = Label(panel, "", new Vector2(0, -45), 38);
        var image = Graphic<Image>(panel, "Reset collection", new Vector2(0, -225), new Vector2(700, 90));
        image.color = Color.clear; image.raycastTarget = true;
        var button = image.gameObject.AddComponent<QuickTravelPressButton>();
        var resetFrame = OptionFrame.Apply(image.gameObject);
        if (resetFrame != null) button.targetGraphic = resetFrame; resetButton = button;
        button.onClick.AddListener(ResetCleanup);
        var resetLabel = Label(image.transform, "重新收集 / Reset cleanup", Vector2.zero, 32);
        resetLabel.rectTransform.anchoredPosition = new Vector2(700f * 0.044f, 0f);
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
        carried = null;
        Dispose(generated); Dispose(font); foreach (var material in materials) Dispose(material);
        materials.Clear(); litter.Clear(); generated = null; basket = panel = null; collected = 0;
    }
    static void Dispose(Object item)
    { if (!item) return; if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
}
