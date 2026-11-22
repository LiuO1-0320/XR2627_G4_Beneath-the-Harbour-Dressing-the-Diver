using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Grab, release near the HMD to wear, then B/Y to return to the exhibit.</summary>
public class DivingHelmetWearable : MonoBehaviour, IResettable, IAccessibleActionProvider
{
    public int ResetOrder => 0;
    public void PrepareReset() { PrepareSessionReset(); }
    public void RestoreReset() { ReturnToStand(); previousButton=false; if(IsWorn || (grab && grab.isSelected)) throw new System.InvalidOperationException("Helmet reset did not release the visitor."); }

    public Transform helmet;
    public Transform head;
    [Min(0.1f)] public float wearDistance = 0.4f;
    public Vector3 wornCenterOffset = new Vector3(0, 0.05f, 0);
    public Vector3 wornEulerOffset = new Vector3(0, -90, 0);
    public bool hideShellWhileWorn = true;
    public bool returnWithSecondaryButton = true;
    [Min(0.1f)] public float standSnapDistance = 0.7f;
    internal const string FitDiverAction = "fit-diver-helmet";
    internal System.Guid FittedActivityId { get; private set; }
    public bool IsWorn { get; private set; }
    public bool ReadyToWear => helmet && head && grab && grab.isSelected && !IsWorn
        && StageCoordinator.AllowsHelmet(this)
        && Vector3.Distance(helmet.TransformPoint(center), head.position) <= wearDistance;
    bool fittingHighlight;
    MaterialPropertyBlock[] originalProperties;
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
    System.Guid grabActivity;
    DiverNpcInteraction diver;

    void Start()
    {
        if (!helmet) { Debug.LogError("Assign the helmet Transform.", this); enabled = false; return; }
        diver = GetComponent<DiverNpcInteraction>();
        if (!head && !diver && Camera.main) head = Camera.main.transform;
        initialPosition = helmet.position; initialRotation = helmet.rotation;
        stand = helmet.parent;
        standLocalPosition = helmet.localPosition; standLocalRotation = helmet.localRotation;
        renderers = helmet.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { Debug.LogError("Helmet has no visible model.", this); enabled = false; return; }
        var bounds = renderers[0].bounds;
        shadows = new ShadowCastingMode[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        { bounds.Encapsulate(renderers[i].bounds); shadows[i] = renderers[i].shadowCastingMode; }
        originalProperties = new MaterialPropertyBlock[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalProperties[i] = new MaterialPropertyBlock();
            renderers[i].GetPropertyBlock(originalProperties[i]);
        }
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
        SessionTelemetry.Report("HelmetGrabbed", this);
        if (StageCoordinator.Current && StageCoordinator.Current.Audio) StageCoordinator.Current.Audio.PlayFeedback(false);
        var fittingStage = diver ? ExperienceStage.DressTheDiver : ExperienceStage.TheCase;
        grabActivity = StageCoordinator.Current && StageCoordinator.Allows(fittingStage)
            ? StageCoordinator.Current.Controller.Session.ActivityId : System.Guid.Empty;
        wearFrame = -1;
        docked = false;
        body.detectCollisions = true;
        body.useGravity = false;
    }
    void OnRelease(SelectExitEventArgs args)
    {
        SessionTelemetry.Report(args.isCanceled ? "HelmetGrabCancelled" : "HelmetReleased", this);
        if (args.isCanceled) { wearFrame = -1; grabActivity = System.Guid.Empty; return; }
        // Resolve every release after XR has restored its cached Rigidbody state.
        // The cached state is kinematic while docked, so gravity alone is insufficient.
        if (!IsWorn) wearFrame = Time.frameCount + 2;
    }
    void LateUpdate()
    {
        if (!helmet || !grab) return;
        if (!head && !diver && Camera.main) head = Camera.main.transform;
        SetFittingHighlight(ReadyToWear);
        // A worn helmet must keep following tracking, including during pause and routing.
        FollowHead();
        bool canRemoveWorn = IsWorn && (!StageCoordinator.Current ||
            StageCoordinator.Current.Controller.Session.Status == SessionStatus.Running);
        if (!StageCoordinator.AllowsHelmet(this) && !canRemoveWorn) return;
        if (wearFrame >= 0 && Time.frameCount >= wearFrame)
        {
            wearFrame = -1;
            if (!grab.isSelected && head && Vector3.Distance(helmet.TransformPoint(center), head.position) <= wearDistance)
            {
                FitToHead(grabActivity);
            }
            else if (!grab.isSelected && !IsWorn)
            {
                SessionTelemetry.Report("HelmetFitMissed", this, "Released outside fitting distance");
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
        bool editorRemove = false;
#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        editorRemove = keyboard != null && (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame);
#endif
        if (editorRemove && !diver)
        {
            if (!IsWorn) Debug.Log("Helmet removal: helmet is not fitted. Bring it close to the head and release the grip first.", this);
            else if (!canRemoveWorn) Debug.Log("Helmet removal: resume the session first.", this);
            else
            {
                ReturnToStand();
                Debug.Log(IsWorn ? "Helmet removal blocked: release the grip first." : "Helmet returned to stand.", this);
            }
        }
        else if (returnWithSecondaryButton && IsWorn && button && !previousButton)
        {
            ReturnToStand();
        }
        previousButton = button;
        FollowHead();
        if (!IsWorn && !grab.isSelected && helmet.position.y < initialPosition.y - 10) ReturnToStand();
    }
    public bool CanExecuteAccessibleAction(string actionId, System.Guid attempt, ExperienceStage stage, out string reason)
    {
        var coordinator = StageCoordinator.Current;
        reason = "当前无法佩戴 / Fitting unavailable";
        var fittingStage = diver ? ExperienceStage.DressTheDiver : ExperienceStage.TheCase;
        if (stage != fittingStage || actionId != (diver ? FitDiverAction : "fit-visitor-helmet") || !isActiveAndEnabled ||
            !coordinator || !GazeInput.Current || !GazeInput.Current.Accessible) return false;
        if (diver && (!diver.isActiveAndEnabled || diver.helmet != helmet)) return false;
        var state = coordinator.Controller.Session;
        if (state.ActivityId != attempt || !StageCoordinator.Allows(stage)) return false;
        if (!coordinator.IsVisitorInNarrationRegion(stage))
        {
            reason = diver ? "请到潜水员展台前 / Approach the diver" : "请走到头盔展台前 / Approach the helmet stand";
            return false;
        }
        if (IsWorn) { reason = "已佩戴 / Already wearing"; return false; }
        if (!helmet || !head || !body || !grab || !grab.isActiveAndEnabled) return false;
        if (grab.isSelected || wearFrame >= 0)
        {
            reason = "请先松开头盔 / Release the helmet first";
            return false;
        }
        reason = "";
        return true;
    }
    public void ExecuteAccessibleAction(string actionId, System.Guid attempt, ExperienceStage stage)
    {
        if (!CanExecuteAccessibleAction(actionId, attempt, stage, out _)) return;
        FitToHead(attempt);
    }

    void FitToHead(System.Guid attempt)
    {
        IsWorn = true;
        FittedActivityId = attempt;
        docked = false;
        wearFrame = -1;
        grabActivity = System.Guid.Empty;
        grab.enabled = false;
        body.isKinematic = true;
        body.useGravity = false;
        body.detectCollisions = false;
        if (hideShellWhileWorn)
            foreach (var renderer in renderers) renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        FollowHead();

        var coordinator = StageCoordinator.Current;
        SessionTelemetry.Report("HelmetFitted", this, diver ? "Diver" : "Visitor");
        if (coordinator && coordinator.Audio) coordinator.Audio.PlayFeedback(true);
        if (!diver && attempt != System.Guid.Empty && coordinator && coordinator.Controller.Session.Stage == ExperienceStage.TheCase)
            coordinator.QueueInteraction(attempt, ExperienceStage.TheCase, VisitorAction.HelmetEquipped);
    }

    void SetFittingHighlight(bool active)
    {
        if (fittingHighlight == active || originalProperties == null) return;
        fittingHighlight = active;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i]) continue;
            if (!active) { renderers[i].SetPropertyBlock(originalProperties[i]); continue; }
            var properties = new MaterialPropertyBlock();
            renderers[i].GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", new Color(0.25f, 1f, 0.65f, 1f));
            properties.SetColor("_Color", new Color(0.25f, 1f, 0.65f, 1f));
            renderers[i].SetPropertyBlock(properties);
        }
    }
    void FollowHead()
    {
        if (!IsWorn || !head || !helmet) return;
        helmet.rotation = head.rotation * Quaternion.Euler(wornEulerOffset);
        helmet.position += head.TransformPoint(wornCenterOffset) - helmet.TransformPoint(center);
    }
    void FixedUpdate()
    {
        if (!StageCoordinator.AllowsHelmet(this)) return;
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
        if (InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed) && pressed)
            return true;
#if ENABLE_INPUT_SYSTEM
        // XR simulators publish controller state through Input System, not the legacy XR device API.
        var usage = node == XRNode.LeftHand
            ? UnityEngine.InputSystem.CommonUsages.LeftHand
            : UnityEngine.InputSystem.CommonUsages.RightHand;
        var controller = UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>(usage);
        var button = controller?.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("secondaryButton");
        return button != null && button.isPressed;
#else
        return false;
#endif
    }
    public void PrepareSessionReset()
    {
        grabActivity = System.Guid.Empty;
        wearFrame = -1;
        if (grab) grab.enabled = false;
    }
    [ContextMenu("Return helmet to stand")]
    public void ReturnToStand()
    {
        if (!body || grab.isSelected) return;
        SetFittingHighlight(false);
        IsWorn = false; wearFrame = -1;
        FittedActivityId = System.Guid.Empty;
        grabActivity = System.Guid.Empty;
        if (!body.isKinematic) { body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        docked = true;
        body.isKinematic = true; body.useGravity = false; body.detectCollisions = true;
        helmet.SetPositionAndRotation(StandPosition(), StandRotation());
        for (int i = 0; i < renderers.Length; i++) renderers[i].shadowCastingMode = shadows[i];
        grab.enabled = true;
    }
    void OnDisable()
    {
        SetFittingHighlight(false);
        if (!grab) return;
        if (IsWorn) ReturnToStand();
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }
}
