using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

/// <summary>
/// Sprint: while the sprint key is held (default Left Shift on keyboard, or the right
/// controller Secondary button / Quest "B" in VR), multiplies both:
/// - ContinuousMoveProvider.moveSpeed (thumbstick / VR walking path), and
/// - XRInteractionSimulator.bodyTranslateMultiplier (keyboard WASD head-move path),
/// restoring the original values on release. Values are re-captured at each press.
/// </summary>
[AddComponentMenu("XR/Locomotion/Shift Sprint")]
public class ShiftSprint : MonoBehaviour
{
    [Tooltip("Keyboard sprint binding path")]
    [SerializeField] string sprintBinding = "<Keyboard>/leftShift";

    [Tooltip("VR controller sprint binding path (empty to disable). Free button: right controller secondary / Quest B")]
    [SerializeField] string sprintVrBinding = "<XRController>{RightHand}/{SecondaryButton}";

    [Min(1f)] public float sprintMultiplier = 2f;

    ContinuousMoveProvider moveProvider;
    XRInteractionSimulator simulator;
    InputAction sprintAction;
    float originalSpeed;
    float originalBodyMultiplier;
    bool sprinting;

    void Start()
    {
        moveProvider = FindFirstObjectByType<ContinuousMoveProvider>();
        // Simulator is only active in keyboard mode; stays null when testing in VR
        simulator = FindFirstObjectByType<XRInteractionSimulator>();
        if (moveProvider == null)
            Debug.LogWarning("ShiftSprint: no ContinuousMoveProvider found in scene.", this);
    }

    void OnEnable()
    {
        sprintAction = new InputAction("Sprint", InputActionType.Button, sprintBinding);
        if (!string.IsNullOrEmpty(sprintVrBinding))
            sprintAction.AddBinding(sprintVrBinding);
        sprintAction.Enable();
    }

    void OnDisable()
    {
        sprintAction?.Disable();
        sprintAction?.Dispose();
        sprintAction = null;
        RestoreSpeed();
    }

    void Update()
    {
        if (moveProvider == null)
            return;

        bool held = sprintAction != null && sprintAction.IsPressed();
        if (held && !sprinting)
        {
            originalSpeed = moveProvider.moveSpeed;
            moveProvider.moveSpeed = originalSpeed * sprintMultiplier;
            if (simulator != null)
            {
                originalBodyMultiplier = simulator.bodyTranslateMultiplier;
                simulator.bodyTranslateMultiplier = originalBodyMultiplier * sprintMultiplier;
            }
            sprinting = true;
        }
        else if (!held && sprinting)
        {
            RestoreSpeed();
        }
    }

    void RestoreSpeed()
    {
        if (!sprinting)
            return;
        if (moveProvider != null)
            moveProvider.moveSpeed = originalSpeed;
        if (simulator != null)
            simulator.bodyTranslateMultiplier = originalBodyMultiplier;
        sprinting = false;
    }
}
