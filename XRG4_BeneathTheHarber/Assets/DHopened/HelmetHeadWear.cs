using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Release a held helmet near the lay figure head to wear it. Grab it again to take it off.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(60)]
public class HelmetHeadWear : MonoBehaviour
{
    XRGrabInteractable m_Grab;
    HelmetInspect m_Inspect;
    Rigidbody m_Body;
    Vector3 m_OriginalLocalScale;
    bool m_WearQueued;
    bool m_IsWorn;

    public bool IsWorn => m_IsWorn;
    public bool IsReadyToWear { get; private set; }

    void Awake()
    {
        m_OriginalLocalScale = transform.localScale;
        m_Grab = GetComponent<XRGrabInteractable>();
        m_Inspect = GetComponent<HelmetInspect>();
        m_Body = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (m_Grab == null)
            return;

        m_Grab.selectEntered.AddListener(OnGrabbed);
        m_Grab.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        if (m_Grab == null)
            return;

        m_Grab.selectEntered.RemoveListener(OnGrabbed);
        m_Grab.selectExited.RemoveListener(OnReleased);
    }

    void Start()
    {
        LayFigureHeadSocket.EnsureExists();
    }

    void LateUpdate()
    {
        if (LayFigureHeadSocket.Instance == null)
            LayFigureHeadSocket.EnsureExists();

        var held = m_Grab != null && m_Grab.isSelected;
        var inspecting = m_Inspect != null && m_Inspect.IsInspecting;
        var socket = LayFigureHeadSocket.Instance;
        IsReadyToWear = held && !inspecting && socket != null && socket.IsClose(transform);

        if (!m_WearQueued)
            return;

        m_WearQueued = false;
        if (m_Grab != null)
            m_Grab.throwOnDetach = true;

        if (held || inspecting || socket == null || !socket.IsClose(transform))
        {
            RestoreLooseBody();
            return;
        }

        socket.Wear(this);
    }

    public void PrepareMeasure(Quaternion rotation)
    {
        transform.SetParent(null, true);
        transform.rotation = rotation;
        transform.localScale = m_OriginalLocalScale;
    }

    public void ApplyWearPose(Vector3 anchor, Quaternion rotation, float scaleFactor, Transform headBone)
    {
        transform.SetParent(null, true);
        transform.rotation = rotation;
        transform.localScale = m_OriginalLocalScale * Mathf.Max(scaleFactor, 0.01f);
        transform.position += anchor - LayFigureHeadSocket.RenderCenter(transform);
        if (headBone != null)
            transform.SetParent(headBone, true);

        SettleBody();
        m_IsWorn = true;
    }

    public void ForceRemove()
    {
        if (!m_IsWorn)
            return;

        m_IsWorn = false;
        m_WearQueued = false;
        var drop = transform.forward;
        transform.SetParent(null, true);
        transform.localScale = m_OriginalLocalScale;
        transform.position += drop * 0.35f;
        if (LayFigureHeadSocket.Instance != null)
            LayFigureHeadSocket.Instance.NotifyRemoved(this);

        RestoreLooseBody();
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        m_WearQueued = false;
        IsReadyToWear = false;
        if (!m_IsWorn)
            return;

        m_IsWorn = false;
        if (m_Grab != null)
            m_Grab.throwOnDetach = false;
        if (LayFigureHeadSocket.Instance != null)
            LayFigureHeadSocket.Instance.NotifyRemoved(this);
        transform.localScale = m_OriginalLocalScale;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (args.isCanceled || (m_Grab != null && m_Grab.isSelected))
            return;

        if (m_Inspect != null && m_Inspect.IsInspecting)
            return;

        m_WearQueued = true;
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

    void RestoreLooseBody()
    {
        if (m_Body == null || m_IsWorn)
            return;

        var velocity = m_Body.velocity;
        var angularVelocity = m_Body.angularVelocity;
        m_Body.isKinematic = false;
        m_Body.useGravity = true;
        m_Body.velocity = velocity;
        m_Body.angularVelocity = angularVelocity;
    }
}
