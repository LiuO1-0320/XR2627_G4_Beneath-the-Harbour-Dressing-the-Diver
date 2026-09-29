using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// After the helmet is magnified, the DivingHelmet_bolt can be grabbed off and held for inspection.
/// Releasing it near its socket seats it again. Ending helmet inspection always seats it.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class HelmetBoltRelease : MonoBehaviour
{
    const string BoltName = "DivingHelmet_bolt";
    const float ArmMagnification = 1.7f;
    const float DisarmMagnification = 1.4f;

    HelmetInspect m_Inspect;
    XRGrabInteractable m_HelmetGrab;
    Transform m_Bolt;
    Transform m_HomeParent;
    Vector3 m_HomeLocalPosition;
    Quaternion m_HomeLocalRotation;
    Vector3 m_HomeLocalScale;
    Renderer m_Renderer;
    Collider m_BoltCollider;
    Rigidbody m_BoltBody;
    XRGrabInteractable m_BoltGrab;
    MaterialPropertyBlock m_Highlight;
    bool m_EmissionReady;
    bool m_Removed;
    bool m_CheckReturn;
    bool m_MissingWarned;

    public bool IsRemoved => m_Removed;
    public bool CanRemove => m_BoltGrab != null && !m_Removed;

    void Awake()
    {
        m_Inspect = GetComponent<HelmetInspect>();
        if (m_Inspect == null || !m_Inspect.AllowsPartInspection)
        {
            enabled = false;
            return;
        }

        m_HelmetGrab = GetComponent<XRGrabInteractable>();
        m_Highlight = new MaterialPropertyBlock();

        var transforms = GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name != BoltName)
                continue;

            m_Bolt = transforms[i];
            break;
        }

        if (m_Bolt == null)
        {
            WarnMissing();
            return;
        }

        m_HomeParent = m_Bolt.parent;
        m_HomeLocalPosition = m_Bolt.localPosition;
        m_HomeLocalRotation = m_Bolt.localRotation;
        m_HomeLocalScale = m_Bolt.localScale;
        m_Renderer = m_Bolt.GetComponent<Renderer>();
        if (m_Renderer == null)
            m_Renderer = m_Bolt.GetComponentInChildren<Renderer>(true);
        m_BoltCollider = m_Bolt.GetComponent<Collider>();
        if (m_BoltCollider == null)
            m_BoltCollider = m_Bolt.GetComponentInChildren<Collider>(true);
    }

    void LateUpdate()
    {
        if (m_Bolt == null || m_Inspect == null)
            return;

        if (!m_Inspect.IsInspecting)
        {
            if (m_Removed || m_BoltGrab != null)
                ReturnToHelmet();
            return;
        }

        if (m_CheckReturn && m_Removed && !IsHeld())
        {
            m_CheckReturn = false;
            if (IsNearHome())
                ReturnToHelmet();
        }

        if (m_Removed)
            return;

        if (m_Inspect.Magnification >= ArmMagnification)
            ArmBolt();
        else if (m_BoltGrab != null && m_Inspect.Magnification < DisarmMagnification)
            DisarmBolt();

        if (CanRemove)
            PulseBolt();
    }

    public void ReturnToHelmet()
    {
        if (m_Bolt == null)
            return;

        ReleaseHolders();
        TearDownInteractable();
        SeatBolt();
        m_Removed = false;
        m_CheckReturn = false;
        ClearHighlight();
    }

    void ArmBolt()
    {
        if (m_BoltGrab != null || m_Removed)
            return;

        if (!EnsureCollider())
            return;

        m_BoltCollider.enabled = true;
        BorrowCollider();

        m_BoltBody = m_Bolt.gameObject.AddComponent<Rigidbody>();
        m_BoltBody.useGravity = false;
        m_BoltBody.isKinematic = true;
        m_BoltBody.interpolation = RigidbodyInterpolation.None;
        m_BoltBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        m_BoltGrab = m_Bolt.gameObject.AddComponent<XRGrabInteractable>();
        m_BoltGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        m_BoltGrab.useDynamicAttach = true;
        m_BoltGrab.matchAttachPosition = true;
        m_BoltGrab.matchAttachRotation = true;
        m_BoltGrab.snapToColliderVolume = false;
        m_BoltGrab.throwOnDetach = false;
        m_BoltGrab.forceGravityOnDetach = false;
        m_BoltGrab.retainTransformParent = false;
        m_BoltGrab.trackScale = false;
        m_BoltGrab.farAttachMode = InteractableFarAttachMode.Near;
        m_BoltGrab.attachEaseInTime = 0.12f;
        m_BoltGrab.selectEntered.AddListener(OnBoltGrabbed);
        m_BoltGrab.selectExited.AddListener(OnBoltReleased);
    }

    void DisarmBolt()
    {
        if (m_Removed)
            return;

        ReleaseHolders();
        TearDownInteractable();
        if (!m_Removed)
            SeatBolt();
        ClearHighlight();
    }

    void OnBoltGrabbed(SelectEnterEventArgs args)
    {
        m_Removed = true;
        ClearHighlight();
        if (m_HomeParent == null)
            return;

        m_Bolt.localScale = Vector3.Scale(m_HomeParent.lossyScale, m_HomeLocalScale);
    }

    void OnBoltReleased(SelectExitEventArgs args)
    {
        m_CheckReturn = true;
        if (m_BoltBody == null)
            return;

        m_BoltBody.isKinematic = true;
        m_BoltBody.useGravity = false;
    }

    void ReleaseHolders()
    {
        if (m_BoltGrab == null || !m_BoltGrab.isSelected)
            return;

        var manager = m_BoltGrab.interactionManager;
        if (manager == null)
            return;

        var selecting = m_BoltGrab.interactorsSelecting;
        for (var i = selecting.Count - 1; i >= 0; i--)
            manager.SelectCancel(selecting[i], m_BoltGrab);
    }

    void TearDownInteractable()
    {
        if (m_BoltGrab != null)
        {
            m_BoltGrab.selectEntered.RemoveListener(OnBoltGrabbed);
            m_BoltGrab.selectExited.RemoveListener(OnBoltReleased);
            DestroyImmediate(m_BoltGrab);
            m_BoltGrab = null;
        }

        if (m_BoltBody != null)
        {
            DestroyImmediate(m_BoltBody);
            m_BoltBody = null;
        }

        ReturnCollider();
    }

    void SeatBolt()
    {
        if (m_HomeParent != null)
            m_Bolt.SetParent(m_HomeParent, false);

        m_Bolt.localPosition = m_HomeLocalPosition;
        m_Bolt.localRotation = m_HomeLocalRotation;
        m_Bolt.localScale = m_HomeLocalScale;

        if (m_BoltCollider != null && (m_Inspect == null || !m_Inspect.IsInspecting))
            m_BoltCollider.enabled = true;
        else if (m_BoltCollider != null)
            m_BoltCollider.enabled = false;
    }

    bool EnsureCollider()
    {
        if (m_BoltCollider != null)
            return true;

        var filter = m_Bolt.GetComponent<MeshFilter>();
        if (filter == null)
            filter = m_Bolt.GetComponentInChildren<MeshFilter>(true);
        if (filter == null || filter.sharedMesh == null)
        {
            WarnMissing();
            return false;
        }

        var meshCollider = filter.gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = filter.sharedMesh;
        meshCollider.convex = true;
        m_BoltCollider = meshCollider;
        if (m_HelmetGrab != null && !m_HelmetGrab.colliders.Contains(meshCollider))
            m_HelmetGrab.colliders.Add(meshCollider);
        return true;
    }

    void BorrowCollider()
    {
        if (m_HelmetGrab == null || m_BoltCollider == null)
            return;

        m_HelmetGrab.colliders.Remove(m_BoltCollider);
    }

    void ReturnCollider()
    {
        if (m_HelmetGrab == null || m_BoltCollider == null)
            return;

        if (!m_HelmetGrab.colliders.Contains(m_BoltCollider))
            m_HelmetGrab.colliders.Add(m_BoltCollider);
    }

    bool IsHeld()
    {
        return m_BoltGrab != null && m_BoltGrab.isSelected;
    }

    bool IsNearHome()
    {
        if (m_HomeParent == null)
            return false;

        var home = m_HomeParent.TransformPoint(m_HomeLocalPosition);
        var radius = 0.08f * Mathf.Max(m_HomeParent.lossyScale.x, 0.01f);
        if (m_Renderer != null)
            radius = Mathf.Max(radius, m_Renderer.bounds.extents.magnitude * 0.4f);

        return (m_Bolt.position - home).sqrMagnitude <= radius * radius;
    }

    void PulseBolt()
    {
        var wave = Mathf.Sin(Time.time * 5.5f);
        m_Bolt.localScale = m_HomeLocalScale * (1f + 0.07f * wave);
        PrepareEmission();
        if (m_Renderer == null)
            return;

        m_Renderer.GetPropertyBlock(m_Highlight);
        var glow = new Color(1f, 0.62f, 0.12f) * (0.45f + 0.7f * (wave * 0.5f + 0.5f));
        m_Highlight.SetColor("_EmissionColor", glow);
        m_Renderer.SetPropertyBlock(m_Highlight);
    }

    void PrepareEmission()
    {
        if (m_EmissionReady || m_Renderer == null)
            return;

        var material = m_Renderer.material;
        material.EnableKeyword("_EMISSION");
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", Color.black);
        m_EmissionReady = true;
    }

    void ClearHighlight()
    {
        if (m_Renderer != null)
            m_Renderer.SetPropertyBlock(null);
    }

    void WarnMissing()
    {
        if (m_MissingWarned)
            return;

        m_MissingWarned = true;
        Debug.LogWarning("DivingHelmet_bolt was not found on " + name + ", so it cannot be removed for inspection.", this);
    }
}
