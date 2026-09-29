using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Settles a helmet upright on Cube1 or Cube2 so the uneven bottom does not tip it.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(70)]
public class HelmetStand : MonoBehaviour
{
    const float PullDuration = 0.28f;

    XRGrabInteractable m_Grab;
    HelmetInspect m_Inspect;
    HelmetHeadWear m_Wear;
    Rigidbody m_Body;
    HelmetPedestal m_Pedestal;
    Quaternion m_FromRotation;
    Quaternion m_ToRotation;
    float m_Pull;
    bool m_Pulling;

    public bool IsSeated { get; private set; }
    public bool IsReadyToSeat { get; private set; }

    void Awake()
    {
        m_Grab = GetComponent<XRGrabInteractable>();
        m_Inspect = GetComponent<HelmetInspect>();
        m_Wear = GetComponent<HelmetHeadWear>();
        m_Body = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (m_Grab == null)
            return;

        m_Grab.selectEntered.AddListener(OnGrabbed);
    }

    void OnDisable()
    {
        if (m_Grab != null)
            m_Grab.selectEntered.RemoveListener(OnGrabbed);

        ReleasePedestal();
    }

    void Start()
    {
        HelmetPedestal.EnsureCubes();
    }

    void LateUpdate()
    {
        HelmetPedestal.EnsureCubes();

        if (m_Pulling)
        {
            AdvancePull();
            return;
        }

        var held = m_Grab != null && m_Grab.isSelected;
        var inspecting = m_Inspect != null && m_Inspect.IsInspecting;
        var worn = m_Wear != null && m_Wear.IsWorn;
        var pedestal = inspecting || worn ? null : HelmetPedestal.Closest(transform, this);
        IsReadyToSeat = held && pedestal != null;
        if (!CanAttract() || pedestal == null || !pedestal.TryClaim(this))
            return;

        BeginPull(pedestal);
    }

    public static Vector3 RenderCenter(Transform helmet)
    {
        return RenderBounds(helmet).center;
    }

    public static Bounds RenderBounds(Transform helmet)
    {
        var renderers = helmet.GetComponentsInChildren<Renderer>();
        var found = false;
        var bounds = new Bounds(helmet.position, Vector3.zero);
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

        return found ? bounds : new Bounds(helmet.position, Vector3.zero);
    }

    public static float ExtentAlong(Bounds bounds, Vector3 up)
    {
        var extents = bounds.extents;
        return Mathf.Abs(extents.x * up.x) + Mathf.Abs(extents.y * up.y) + Mathf.Abs(extents.z * up.z);
    }

    bool CanAttract()
    {
        if (m_Pulling || IsSeated)
            return false;

        if (m_Grab != null && m_Grab.isSelected)
            return false;

        if (m_Inspect != null && m_Inspect.IsInspecting)
            return false;

        if (m_Wear != null && m_Wear.IsWorn)
            return false;

        return true;
    }

    void BeginPull(HelmetPedestal pedestal)
    {
        m_Pedestal = pedestal;
        m_Pulling = true;
        IsSeated = false;
        IsReadyToSeat = false;
        m_Pull = 0f;
        m_FromRotation = transform.rotation;
        m_ToRotation = UprightRotation(pedestal);
        SettleBody();
        PlaceOnTop();
    }

    void AdvancePull()
    {
        if (m_Pedestal == null)
        {
            m_Pulling = false;
            return;
        }

        m_Pull += Time.deltaTime / PullDuration;
        var blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_Pull));
        transform.rotation = Quaternion.Slerp(m_FromRotation, m_ToRotation, blend);
        PlaceOnTop();
        SettleBody();

        if (blend < 1f)
            return;

        m_Pulling = false;
        IsSeated = true;
    }

    void PlaceOnTop()
    {
        var up = m_Pedestal.Up;
        var bounds = RenderBounds(transform);
        var bottom = bounds.center - up * ExtentAlong(bounds, up);
        var top = m_Pedestal.TopCenter;
        transform.position += top - bottom;

        bounds = RenderBounds(transform);
        var horizontal = Vector3.ProjectOnPlane(bounds.center - m_Pedestal.transform.position, up);
        transform.position -= horizontal;
    }

    Quaternion UprightRotation(HelmetPedestal pedestal)
    {
        var up = pedestal.Up;
        var forward = Vector3.ProjectOnPlane(transform.forward, up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.ProjectOnPlane(pedestal.transform.forward, up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.ProjectOnPlane(Vector3.forward, up);

        return Quaternion.LookRotation(forward.normalized, up);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        var fromSeat = IsSeated || m_Pulling;
        m_Pulling = false;
        IsSeated = false;
        IsReadyToSeat = false;
        ReleasePedestal();
        if (fromSeat && m_Grab != null)
            m_Grab.throwOnDetach = false;
    }

    void ReleasePedestal()
    {
        if (m_Pedestal == null)
            return;

        m_Pedestal.Clear(this);
        m_Pedestal = null;
    }

    void SettleBody()
    {
        if (m_Body == null)
            return;

        m_Body.velocity = Vector3.zero;
        m_Body.angularVelocity = Vector3.zero;
        m_Body.useGravity = false;
        m_Body.isKinematic = true;
    }
}
