using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Grab, release near the HMD to wear, then B/Y to return to the exhibit.</summary>
public class DivingHelmetWearable : MonoBehaviour
{
    public Transform helmet;
    public Transform head;
    [Min(0.1f)] public float wearDistance = 0.4f;
    public Vector3 wornCenterOffset = new Vector3(0, 0.05f, 0);
    public Vector3 wornEulerOffset = new Vector3(0, -90, 0);
    public bool hideShellWhileWorn = true;
    public bool returnWithSecondaryButton = true;
    [Min(0.1f)] public float standSnapDistance = 0.7f;
    public bool IsWorn { get; private set; }
    XRGrabInteractable grab;
    Rigidbody body;
    Renderer[] renderers;
    ShadowCastingMode[] shadows;
    Vector3 center, initialPosition;
    Quaternion initialRotation;
    Transform stand;
    Vector3 standLocalPosition;
    Quaternion standLocalRotation;
    bool docked = true;
    int wearFrame = -1;
    bool previousButton;

    void Start()
    {
        if (!helmet) { Debug.LogError("Assign the helmet Transform.", this); enabled = false; return; }
        if (!head && Camera.main) head = Camera.main.transform;
        initialPosition = helmet.position; initialRotation = helmet.rotation;
        stand = helmet.parent;
        standLocalPosition = helmet.localPosition; standLocalRotation = helmet.localRotation;
        renderers = helmet.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { Debug.LogError("Helmet has no visible model.", this); enabled = false; return; }
        var bounds = renderers[0].bounds;
        shadows = new ShadowCastingMode[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        { bounds.Encapsulate(renderers[i].bounds); shadows[i] = renderers[i].shadowCastingMode; }
        center = helmet.InverseTransformPoint(bounds.center);
        body = helmet.GetComponent<Rigidbody>();
        if (!body) body = helmet.gameObject.AddComponent<Rigidbody>();
        body.mass = 3; body.useGravity = false; body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        // A simple convex grab volume works with the imported model, including a hollow shell.
        var collider = helmet.gameObject.AddComponent<BoxCollider>();
        var local = new Bounds(center, Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            local.Encapsulate(helmet.InverseTransformPoint(corner));
        }
        collider.center = local.center; collider.size = local.size;
        grab = helmet.GetComponent<XRGrabInteractable>();
        if (!grab) grab = helmet.gameObject.AddComponent<XRGrabInteractable>();
        grab.colliders.Clear(); grab.colliders.Add(collider);
        grab.useDynamicAttach = true;
        grab.retainTransformParent = true;
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.throwOnDetach = false;
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        wearFrame = -1;
        docked = false;
        body.detectCollisions = true;
        body.useGravity = false;
    }
    void OnRelease(SelectExitEventArgs args)
    {
        // Resolve every release after XR has restored its cached Rigidbody state.
        // The cached state is kinematic while docked, so gravity alone is insufficient.
        if (!IsWorn) wearFrame = Time.frameCount + 2;
    }
    void LateUpdate()
    {
        if (!helmet || !grab) return;
        if (!head && Camera.main) head = Camera.main.transform;
        if (wearFrame >= 0 && Time.frameCount >= wearFrame)
        {
            wearFrame = -1;
            if (!grab.isSelected && head && Vector3.Distance(helmet.TransformPoint(center), head.position) <= wearDistance)
            {
                IsWorn = true; grab.enabled = false;
                body.isKinematic = true; body.useGravity = false; body.detectCollisions = false;
                if (hideShellWhileWorn)
                    foreach (var renderer in renderers) renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            else if (!grab.isSelected && !IsWorn)
            {
                if (NearStand()) ReturnToStand();
                else
                {
                    docked = false;
                    body.isKinematic = false;
                    body.detectCollisions = true;
                    body.useGravity = true;
                    body.WakeUp();
                }
            }
        }
        bool button = SecondaryPressed(XRNode.LeftHand) || SecondaryPressed(XRNode.RightHand);
        if (returnWithSecondaryButton && IsWorn && button && !previousButton) ReturnToStand();
        previousButton = button;
        if (IsWorn && head)
        {
            helmet.rotation = head.rotation * Quaternion.Euler(wornEulerOffset);
            helmet.position += head.TransformPoint(wornCenterOffset) - helmet.TransformPoint(center);
        }
        if (!IsWorn && !grab.isSelected && helmet.position.y < initialPosition.y - 10) ReturnToStand();
    }
    void FixedUpdate()
    {
        if (!helmet || !grab || IsWorn || docked || grab.isSelected || wearFrame >= 0) return;
        if (NearStand()) ReturnToStand();
    }
    Vector3 StandPosition() { return stand ? stand.TransformPoint(standLocalPosition) : initialPosition; }
    Quaternion StandRotation() { return stand ? stand.rotation * standLocalRotation : initialRotation; }
    bool NearStand()
    {
        Vector3 standCenter = StandPosition() + StandRotation() * Vector3.Scale(helmet.lossyScale, center);
        return Vector3.Distance(helmet.TransformPoint(center), standCenter) <= standSnapDistance;
    }
    static bool SecondaryPressed(XRNode node)
    {
        return InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed) && pressed;
    }
    [ContextMenu("Return helmet to stand")]
    public void ReturnToStand()
    {
        if (!body || grab.isSelected) return;
        IsWorn = false; wearFrame = -1;
        if (!body.isKinematic) { body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        docked = true;
        body.isKinematic = true; body.useGravity = false; body.detectCollisions = true;
        helmet.SetPositionAndRotation(StandPosition(), StandRotation());
        for (int i = 0; i < renderers.Length; i++) renderers[i].shadowCastingMode = shadows[i];
        grab.enabled = true;
    }
    void OnDisable()
    {
        if (!grab) return;
        if (IsWorn) ReturnToStand();
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }
}
