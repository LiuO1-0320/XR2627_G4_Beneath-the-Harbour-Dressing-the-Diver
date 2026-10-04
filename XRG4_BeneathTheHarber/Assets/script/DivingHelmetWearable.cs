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
    public bool IsWorn { get; private set; }
    XRGrabInteractable grab;
    Rigidbody body;
    Renderer[] renderers;
    ShadowCastingMode[] shadows;
    Vector3 center, initialPosition;
    Quaternion initialRotation;
    int wearFrame = -1;
    bool previousButton;

    void Start()
    {
        if (!helmet) { Debug.LogError("Assign the divinghelmet1 Transform.", this); enabled = false; return; }
        if (!head && Camera.main) head = Camera.main.transform;
        initialPosition = helmet.position; initialRotation = helmet.rotation;
        renderers = helmet.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { Debug.LogError("Helmet has no visible model.", this); enabled = false; return; }
        var bounds = renderers[0].bounds;
        shadows = new ShadowCastingMode[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        { bounds.Encapsulate(renderers[i].bounds); shadows[i] = renderers[i].shadowCastingMode; }
        center = helmet.InverseTransformPoint(bounds.center);
        body = helmet.GetComponent<Rigidbody>();
        if (!body) body = helmet.gameObject.AddComponent<Rigidbody>();
        body.mass = 3; body.useGravity = false; body.isKinematic = false;
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
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.throwOnDetach = false;
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        wearFrame = -1;
        body.useGravity = true;
    }
    void OnRelease(SelectExitEventArgs args)
    {
        if (args.isCanceled || !head) return;
        if (Vector3.Distance(helmet.TransformPoint(center), head.position) <= wearDistance)
            wearFrame = Time.frameCount + 2; // Wait for XR detach physics to finish.
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
        }
        bool button = SecondaryPressed(XRNode.LeftHand) || SecondaryPressed(XRNode.RightHand);
        if (IsWorn && button && !previousButton) ReturnToStand();
        previousButton = button;
        if (IsWorn && head)
        {
            helmet.rotation = head.rotation * Quaternion.Euler(wornEulerOffset);
            helmet.position += head.TransformPoint(wornCenterOffset) - helmet.TransformPoint(center);
        }
        if (!IsWorn && !grab.isSelected && helmet.position.y < initialPosition.y - 10) ReturnToStand();
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
        body.isKinematic = false; body.useGravity = false; body.detectCollisions = true;
        body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        helmet.SetPositionAndRotation(initialPosition, initialRotation);
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
