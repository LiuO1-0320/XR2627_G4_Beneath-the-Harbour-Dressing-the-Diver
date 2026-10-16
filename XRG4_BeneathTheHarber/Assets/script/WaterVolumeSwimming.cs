using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

/// <summary>
/// Input-driven buoyancy for a water body: while the player's head is inside this
/// trigger volume, the GravityProvider is locked off and depth is held without input;
/// left thumbstick up/down (or the jump button) swims up/down. Exiting the volume
/// restores normal gravity. Land behavior is untouched.
/// Setup: put this on a trigger collider covering the water body; the volume's top
/// face defines the surface. Input readers are wired to the same XRI actions used
/// by the rig's existing Jump and Move providers.
/// </summary>
[AddComponentMenu("XR/Locomotion/Water Volume Swimming")]
public class WaterVolumeSwimming : LocomotionProvider, IGravityController, IResettable
{
    public int ResetOrder => 50;
    public void PrepareReset() { ExitWater(); }
    public void RestoreReset() { ExitWater(); verticalSpeed=0; if(gravityProvider) gravityProvider.ResetFallForce(); }

    [Header("References (auto-found if empty)")]
    [SerializeField] XROrigin origin;
    [SerializeField] GravityProvider gravityProvider;
    [SerializeField] Collider waterVolume;

    [Header("Input (same actions as Jump / Move providers)")]
    [SerializeField] XRInputValueReader<Vector2> leftThumbstick = new XRInputValueReader<Vector2>("Left Thumbstick");
    [SerializeField] XRInputButtonReader jumpButton = new XRInputButtonReader("Jump");

    [Header("Dedicated keyboard swim keys (keyboard-simulator friendly, avoids IJKL)")]
    [SerializeField] string swimUpBinding = "<Keyboard>/f";
    [SerializeField] string swimDownBinding = "<Keyboard>/b";
    InputAction swimUpAction, swimDownAction;

    [Header("Feel")]
    [Min(0f)] public float sinkAcceleration = 0.6f;
    [Min(0f)] public float maxSinkSpeed = 1.2f;
    [Min(0f)] public float swimUpSpeed = 1.8f;
    [Min(0f)] public float swimDownSpeed = 1.2f;
    [Min(0f)] public float surfaceHysteresis = 0.15f;

    readonly XROriginMovement transformation = new XROriginMovement();
    float verticalSpeed;
    bool submerged;
    bool gravityLocked;
    DiveRopeStation diveStation;

    public bool canProcess => isActiveAndEnabled;
    public bool gravityPaused => submerged;
    public bool IsSubmerged => submerged;
    public float DistanceToShore(Vector3 position)
    {
        var volume = waterVolume ? waterVolume : GetComponent<Collider>();
        if (!volume || !volume.enabled || !volume.gameObject.activeInHierarchy) return float.PositiveInfinity;
        return AudioDirector.HorizontalDistance(position, volume.bounds);
    }
    // Audio must query geometry even while rope travel disables this locomotion provider.
    public bool ContainsHead(Vector3 position, bool wasInWater = false)
    {
        var volume = waterVolume ? waterVolume : GetComponent<Collider>();
        if (!volume || !volume.enabled || !volume.gameObject.activeInHierarchy) return false;
        var bounds = volume.bounds;
        return position.x >= bounds.min.x && position.x <= bounds.max.x
            && position.z >= bounds.min.z && position.z <= bounds.max.z && position.y >= bounds.min.y
            && position.y < bounds.max.y + (wasInWater ? 0.05f : -0.05f);
    }
    public bool IsAboveSurface(Vector3 position)
    {
        var volume = waterVolume ? waterVolume : GetComponent<Collider>();
        return volume && position.y >= volume.bounds.max.y;
    }
    public bool IsOverWater(Vector3 position)
    {
        var volume = waterVolume ? waterVolume : GetComponent<Collider>();
        if (!volume || !volume.enabled || !volume.gameObject.activeInHierarchy) return false;
        var bounds = volume.bounds;
        return position.x >= bounds.min.x && position.x <= bounds.max.x
            && position.z >= bounds.min.z && position.z <= bounds.max.z;
    }

    protected override void Awake()
    {
        base.Awake();
        if (origin == null) origin = FindFirstObjectByType<XROrigin>();
        if (gravityProvider == null) gravityProvider = FindFirstObjectByType<GravityProvider>();
        if (waterVolume == null) waterVolume = GetComponent<Collider>();
        diveStation = FindFirstObjectByType<DiveRopeStation>();
    }

    protected virtual void OnEnable()
    {
        leftThumbstick.EnableDirectActionIfModeUsed();
        jumpButton.EnableDirectActionIfModeUsed();
        swimUpAction = new InputAction("Swim Up", InputActionType.Button, swimUpBinding);
        swimDownAction = new InputAction("Swim Down", InputActionType.Button, swimDownBinding);
        swimUpAction.Enable();
        swimDownAction.Enable();
    }

    protected virtual void OnDisable()
    {
        if (submerged) ExitWater();
        leftThumbstick.DisableDirectActionIfModeUsed();
        jumpButton.DisableDirectActionIfModeUsed();
        swimUpAction?.Disable();
        swimDownAction?.Disable();
        swimUpAction?.Dispose();
        swimDownAction?.Dispose();
        swimUpAction = null;
        swimDownAction = null;
    }

    protected virtual void Update()
    {
        if (StageCoordinator.Current && StageCoordinator.Current.Controller.Session.Status != SessionStatus.Running) return;
        if (origin == null || gravityProvider == null || waterVolume == null)
            return;

        var bounds = waterVolume.bounds;
        var head = origin.Camera != null ? origin.Camera.transform : origin.transform;
        bool inWater = bounds.Contains(head.position);
        bool wasSubmerged = submerged;
        // Hysteresis: needs to rise a bit above the surface plane before leaving the water
        submerged = inWater && (wasSubmerged || head.position.y < bounds.max.y - surfaceHysteresis)
            || (!inWater && wasSubmerged && head.position.y < bounds.max.y - surfaceHysteresis);

        if (submerged && !wasSubmerged) EnterWater();
        else if (!submerged && wasSubmerged) ExitWater();

        if (!submerged)
            return;

        float swimAxis = leftThumbstick.ReadValue().y;
        bool jumpHeld = jumpButton.ReadIsPerformed();
        bool keyUp = swimUpAction != null && swimUpAction.IsPressed();
        bool keyDown = swimDownAction != null && swimDownAction.IsPressed();
        bool wantUp = swimAxis > 0.25f || jumpHeld || keyUp;
        bool wantDown = swimAxis < -0.25f || keyDown;

        if (wantUp)
        {
            // Rise quickly toward swim speed; also cancels any residual sink speed
            verticalSpeed = Mathf.MoveTowards(Mathf.Max(verticalSpeed, 0f), swimUpSpeed, swimUpSpeed * 4f * Time.deltaTime);
        }
        else if (wantDown)
        {
            verticalSpeed = Mathf.MoveTowards(Mathf.Min(verticalSpeed, 0f), -swimDownSpeed, swimDownSpeed * 4f * Time.deltaTime);
        }
        else
        {
            // Releasing input must not start/continue an unintended dive near the rope.
            verticalSpeed = 0;
        }

        // Use the same opening for keyboard/controller swimming as for the rope buttons.
        // Horizontal exploration remains available; vertical movement outside the shaft stops.
        if (diveStation && IsOverWater(diveStation.transform.position) && !diveStation.IsInsideShaft(head.position))
            verticalSpeed = 0;

        TryStartLocomotionImmediately();
        if (locomotionState != LocomotionState.Moving)
            return;

        transformation.motion = Vector3.up * (verticalSpeed * Time.deltaTime);
        TryQueueTransformation(transformation);
    }

    void EnterWater()
    {
        verticalSpeed = 0;
        if (gravityProvider != null)
            gravityLocked = gravityProvider.TryLockGravity(this, GravityOverride.ForcedOff);
    }

    void ExitWater()
    {
        verticalSpeed = 0f;
        if (gravityLocked && gravityProvider != null)
        {
            gravityProvider.UnlockGravity(this);
            gravityLocked = false;
        }
    }

    /// <inheritdoc />
    public bool TryLockGravity(GravityOverride gravityOverride)
    {
        if (gravityProvider != null)
            return gravityProvider.TryLockGravity(this, gravityOverride);
        return false;
    }

    /// <inheritdoc />
    public void RemoveGravityLock()
    {
        if (gravityProvider != null)
            gravityProvider.UnlockGravity(this);
        gravityLocked = false;
    }

    void IGravityController.OnGroundedChanged(bool isGrounded) { }
    void IGravityController.OnGravityLockChanged(GravityOverride gravityOverride) { }
}
