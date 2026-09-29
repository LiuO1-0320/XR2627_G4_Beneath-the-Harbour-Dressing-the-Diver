using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Grab a helmet, then press the controller secondary button (Y or B) to bring that helmet
/// in front of the headset. Hand distance scales it. Only a helmet named as number 2 can
/// have DivingHelmet_bolt removed. Press the secondary button again to put it down.
/// </summary>
[DisallowMultipleComponent]
public class HelmetInspect : MonoBehaviour
{
    const float FaceClearance = 0.2f;
    const float FlySmoothTime = 0.28f;
    const float MinMultiplier = 0.4f;
    const float MaxMultiplier = 4f;
    const float MinHandDistance = 0.05f;

    public float Magnification => m_Multiplier;

    static readonly List<HelmetInspect> s_Helmets = new List<HelmetInspect>();
    static HelmetInspect s_Active;
    static int s_DecisionFrame = -1;
    static HelmetInspect s_PressTarget;

    public static List<HelmetInspect> All => s_Helmets;

    public bool IsHeld
    {
        get
        {
            if (m_Grab == null)
                m_Grab = GetComponent<XRGrabInteractable>();
            return m_Grab != null && m_Grab.isSelected;
        }
    }

    public bool AllowsPartInspection => AllowsParts(name);

    public static bool AllowsParts(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        return objectName.IndexOf("DHopened2", StringComparison.Ordinal) >= 0
            || objectName.EndsWith("2", StringComparison.Ordinal)
            || objectName.IndexOf("(2)", StringComparison.Ordinal) >= 0
            || objectName.IndexOf(" 2", StringComparison.Ordinal) >= 0;
    }

    enum Phase
    {
        Idle,
        Flying,
        Inspecting,
    }

    InputAction m_SummonAction;
    Camera m_Camera;
    Transform m_LeftController;
    Transform m_RightController;
    Rigidbody m_Body;
    XRGrabInteractable m_Grab;

    public bool IsInspecting => m_Phase != Phase.Idle;

    Phase m_Phase;
    Vector3 m_Velocity;
    Vector3 m_BaseScale;
    Vector3 m_ViewOrigin;
    Vector3 m_ViewForward;
    float m_HandReference;
    float m_Multiplier = 1f;
    bool m_HasHandReference;
    bool m_BodyWasKinematic;
    bool m_BodyUsedGravity;
    readonly List<Collider> m_DisabledColliders = new List<Collider>();

    void OnEnable()
    {
        if (!s_Helmets.Contains(this))
            s_Helmets.Add(this);

        m_SummonAction = new InputAction("HelmetInspectSummon", InputActionType.Button);
        m_SummonAction.AddBinding("<XRController>/{SecondaryButton}");
        m_SummonAction.Enable();
    }

    void OnDisable()
    {
        s_Helmets.Remove(this);
        if (s_Active == this)
            s_Active = null;
        if (s_PressTarget == this)
            s_PressTarget = null;

        if (m_Phase != Phase.Idle)
            EndInspect();

        if (m_SummonAction == null)
            return;

        m_SummonAction.Disable();
        m_SummonAction.Dispose();
        m_SummonAction = null;
    }

    void LateUpdate()
    {
        if (m_SummonAction != null && m_SummonAction.WasPressedThisFrame())
            DecidePress();

        if (s_DecisionFrame == Time.frameCount)
            ApplyPress();

        if (m_Phase == Phase.Flying)
            FlyTowardView();
        else if (m_Phase == Phase.Inspecting)
            ScaleWithControllers();
    }

    void DecidePress()
    {
        if (s_DecisionFrame == Time.frameCount)
            return;

        s_DecisionFrame = Time.frameCount;
        s_PressTarget = null;
        for (var i = 0; i < s_Helmets.Count; i++)
        {
            var helmet = s_Helmets[i];
            if (helmet != null && helmet.IsHeld)
            {
                s_PressTarget = helmet;
                return;
            }
        }
    }

    void ApplyPress()
    {
        if (s_PressTarget == this)
        {
            if (m_Phase != Phase.Idle)
            {
                EndInspect();
                return;
            }

            if (s_Active != null && s_Active != this)
                s_Active.EndInspect();
            BeginInspect();
            return;
        }

        if (s_PressTarget == null && m_Phase != Phase.Idle)
            EndInspect();
    }

    void BeginInspect()
    {
        if (!TryGetCamera())
            return;

        m_Body = GetComponent<Rigidbody>();
        m_Grab = GetComponent<XRGrabInteractable>();
        m_BaseScale = transform.localScale;
        m_Multiplier = 1f;
        m_HasHandReference = false;
        m_Velocity = Vector3.zero;

        ReleaseGrab();
        SetCollidersEnabled(false);

        if (m_Body != null)
        {
            m_BodyWasKinematic = m_Body.isKinematic;
            m_BodyUsedGravity = m_Body.useGravity;
            m_Body.velocity = Vector3.zero;
            m_Body.angularVelocity = Vector3.zero;
            m_Body.useGravity = false;
            m_Body.isKinematic = true;
        }

        CacheControllers();
        s_Active = this;
        m_Phase = Phase.Flying;
    }

    void EndInspect()
    {
        if (s_Active == this)
            s_Active = null;

        m_Phase = Phase.Idle;

        var bolt = GetComponent<HelmetBoltRelease>();
        if (bolt != null)
            bolt.ReturnToHelmet();
        m_HasHandReference = false;
        transform.localScale = m_BaseScale;

        if (m_Camera != null)
        {
            var view = m_Camera.transform;
            transform.position = view.position + view.forward * 0.55f + Vector3.down * 0.1f;
        }

        SetCollidersEnabled(true);

        if (m_Grab != null)
            m_Grab.enabled = true;

        if (m_Body != null)
        {
            m_Body.isKinematic = m_BodyWasKinematic;
            m_Body.useGravity = m_BodyUsedGravity;
            m_Body.velocity = Vector3.zero;
            m_Body.angularVelocity = Vector3.zero;
        }
    }

    void FlyTowardView()
    {
        if (!TryGetGoal(m_Camera.transform, out var goalPosition, out var goalRotation))
            return;

        transform.position = Vector3.SmoothDamp(transform.position, goalPosition, ref m_Velocity, FlySmoothTime);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            goalRotation,
            1f - Mathf.Exp(-8f * Time.deltaTime));

        var arrived = (transform.position - goalPosition).sqrMagnitude < 0.0008f
            && Quaternion.Angle(transform.rotation, goalRotation) < 4f;
        if (!arrived)
            return;

        var view = m_Camera.transform;
        m_ViewOrigin = view.position;
        m_ViewForward = view.forward;
        m_Phase = Phase.Inspecting;
        HoldInFrontOfView();
    }

    void ScaleWithControllers()
    {
        if (!TryGetControllerDistance(out var distance))
            return;

        if (!m_HasHandReference)
        {
            m_HandReference = distance;
            m_HasHandReference = true;
            m_Multiplier = 1f;
            return;
        }

        var target = Mathf.Clamp(distance / m_HandReference, MinMultiplier, MaxMultiplier);
        var next = Mathf.Lerp(m_Multiplier, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
        if (Mathf.Abs(next - m_Multiplier) < 0.0001f)
            return;

        m_Multiplier = next;
        transform.localScale = m_BaseScale * m_Multiplier;
        HoldInFrontOfView();
    }

    void HoldInFrontOfView()
    {
        if (!TryGetBounds(out var bounds))
            return;

        var radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        var desiredCenter = m_ViewOrigin + m_ViewForward * (FaceClearance + radius);
        transform.position += desiredCenter - bounds.center;
    }

    bool TryGetGoal(Transform view, out Vector3 goalPosition, out Quaternion goalRotation)
    {
        goalPosition = transform.position;
        goalRotation = transform.rotation;
        if (!TryGetBounds(out var bounds))
            return false;

        var radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        var desiredCenter = view.position + view.forward * (FaceClearance + radius);
        goalPosition = transform.position + (desiredCenter - bounds.center);
        var toViewer = view.position - desiredCenter;
        if (toViewer.sqrMagnitude < 0.0001f)
            toViewer = -view.forward;
        var up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(toViewer.normalized, up)) > 0.98f)
            up = view.up;
        goalRotation = Quaternion.LookRotation(toViewer, up);
        return true;
    }

    bool TryGetBounds(out Bounds bounds)
    {
        var renderers = GetComponentsInChildren<Renderer>();
        bounds = new Bounds(transform.position, Vector3.zero);
        var found = false;
        for (var i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i].enabled)
                continue;

            if (!found)
            {
                bounds = renderers[i].bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return found;
    }

    bool TryGetControllerDistance(out float distance)
    {
        distance = 0f;
        CacheControllers();
        if (m_LeftController == null || m_RightController == null)
            return false;

        distance = Vector3.Distance(m_LeftController.position, m_RightController.position);
        return distance >= MinHandDistance;
    }

    void CacheControllers()
    {
        if (m_LeftController != null && m_RightController != null)
            return;

        var transforms = FindObjectsOfType<Transform>();
        for (var i = 0; i < transforms.Length; i++)
        {
            var current = transforms[i];
            if (current.name == "Left Controller")
                m_LeftController = current;
            else if (current.name == "Right Controller")
                m_RightController = current;
        }
    }

    bool TryGetCamera()
    {
        if (m_Camera != null)
            return true;

        m_Camera = Camera.main;
        return m_Camera != null;
    }

    void ReleaseGrab()
    {
        if (m_Grab == null)
            return;

        var manager = m_Grab.interactionManager;
        if (manager != null && m_Grab.isSelected)
        {
            var selecting = m_Grab.interactorsSelecting;
            for (var i = selecting.Count - 1; i >= 0; i--)
                manager.SelectCancel(selecting[i], m_Grab);
        }

        m_Grab.enabled = false;
    }

    void SetCollidersEnabled(bool enabled)
    {
        if (enabled)
        {
            for (var i = 0; i < m_DisabledColliders.Count; i++)
            {
                if (m_DisabledColliders[i] != null)
                    m_DisabledColliders[i].enabled = true;
            }

            m_DisabledColliders.Clear();
            return;
        }

        var colliders = GetComponentsInChildren<Collider>(true);
        for (var i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].enabled)
                continue;

            colliders[i].enabled = false;
            m_DisabledColliders.Add(colliders[i]);
        }
    }
}
