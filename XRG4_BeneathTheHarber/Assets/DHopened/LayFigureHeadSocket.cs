using UnityEngine;

/// <summary>
/// Finds the lay figure head bone and wears a released helmet on it.
/// </summary>
[DisallowMultipleComponent]
public class LayFigureHeadSocket : MonoBehaviour
{
    const float HelmetHeightInHeads = 2.15f;
    const float ReachInHeads = 1.45f;

    static bool s_MissingWarned;

    Transform m_Head;
    Transform m_HeadEnd;
    Transform m_LeftShoulder;
    Transform m_RightShoulder;
    HelmetHeadWear m_Worn;

    public static LayFigureHeadSocket Instance { get; private set; }

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        var head = FindHeadBone();
        if (head == null)
        {
            if (!s_MissingWarned)
            {
                s_MissingWarned = true;
                Debug.LogWarning("lay_figure head bone was not found, so a helmet cannot be worn.");
            }

            return;
        }

        var host = head.root != null ? head.root.gameObject : head.gameObject;
        Instance = host.GetComponent<LayFigureHeadSocket>();
        if (Instance == null)
            Instance = host.AddComponent<LayFigureHeadSocket>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
        CacheBones();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsClose(Transform helmet)
    {
        if (!CacheBones() || helmet == null)
            return false;

        var reach = Mathf.Max(HeadLength() * ReachInHeads, 0.16f);
        return (RenderCenter(helmet) - AnchorPosition()).sqrMagnitude <= reach * reach;
    }

    public void Wear(HelmetHeadWear wear)
    {
        if (wear == null || !CacheBones())
            return;

        if (m_Worn != null && m_Worn != wear)
            m_Worn.ForceRemove();

        var rotation = WearRotation();
        wear.PrepareMeasure(rotation);
        wear.ApplyWearPose(AnchorPosition(), rotation, FittedScale(wear.transform), m_Head);
        m_Worn = wear;
    }

    public void NotifyRemoved(HelmetHeadWear wear)
    {
        if (m_Worn == wear)
            m_Worn = null;
    }

    Vector3 AnchorPosition()
    {
        return Vector3.Lerp(m_Head.position, m_HeadEnd.position, 0.42f);
    }

    Quaternion WearRotation()
    {
        var up = HeadUp();
        return Quaternion.LookRotation(FaceDirection(up), up);
    }

    float FittedScale(Transform helmet)
    {
        var height = AlignedHeight(helmet, HeadUp());
        if (height < 0.0001f)
            return 1f;

        return HeadLength() * HelmetHeightInHeads / height;
    }

    bool CacheBones()
    {
        if (m_Head != null && m_HeadEnd != null)
            return true;

        if (m_Head == null)
            m_Head = FindHeadBone(transform);
        if (m_Head == null)
            return false;

        m_HeadEnd = FindNamed(m_Head, "head_end");
        if (m_HeadEnd == null)
            return false;

        var root = m_Head.root != null ? m_Head.root : m_Head;
        m_LeftShoulder = FindNamed(root, "l_shoulder");
        m_RightShoulder = FindNamed(root, "r_shoulder");
        return true;
    }

    float HeadLength()
    {
        return Mathf.Max(Vector3.Distance(m_Head.position, m_HeadEnd.position), 0.05f);
    }

    Vector3 HeadUp()
    {
        var up = m_HeadEnd.position - m_Head.position;
        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.up;
        return up.normalized;
    }

    Vector3 FaceDirection(Vector3 up)
    {
        if (m_LeftShoulder != null && m_RightShoulder != null)
        {
            var shoulderRight = m_RightShoulder.position - m_LeftShoulder.position;
            var face = Vector3.Cross(shoulderRight, up);
            if (face.sqrMagnitude > 0.0001f)
                return face.normalized;
        }

        var fallback = Vector3.ProjectOnPlane(m_Head.forward, up);
        if (fallback.sqrMagnitude < 0.0001f)
            fallback = Vector3.ProjectOnPlane(Vector3.forward, up);
        return fallback.normalized;
    }

    static float AlignedHeight(Transform helmet, Vector3 up)
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

        if (!found)
            return 0f;

        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;
        var center = bounds.center;
        var extents = bounds.extents;
        for (var x = -1; x <= 1; x += 2)
        {
            for (var y = -1; y <= 1; y += 2)
            {
                for (var z = -1; z <= 1; z += 2)
                {
                    var corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    var projected = Vector3.Dot(corner, up);
                    if (projected < min)
                        min = projected;
                    if (projected > max)
                        max = projected;
                }
            }
        }

        return max - min;
    }

    public static Vector3 RenderCenter(Transform helmet)
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

        return found ? bounds.center : helmet.position;
    }

    static Transform FindHeadBone()
    {
        var transforms = FindObjectsOfType<Transform>();
        for (var i = 0; i < transforms.Length; i++)
        {
            if (IsLayFigureHead(transforms[i]))
                return transforms[i];
        }

        return null;
    }

    static Transform FindHeadBone(Transform preferredRoot)
    {
        if (preferredRoot != null)
        {
            var named = FindNamed(preferredRoot, "head");
            if (named != null && FindNamed(named, "head_end") != null)
                return named;
        }

        return FindHeadBone();
    }

    static bool IsLayFigureHead(Transform candidate)
    {
        if (candidate == null || candidate.name != "head")
            return false;

        var parent = candidate;
        while (parent != null)
        {
            if (parent.name.IndexOf("lay_figure", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return FindNamed(candidate, "head_end") != null;
            parent = parent.parent;
        }

        return false;
    }

    static Transform FindNamed(Transform root, string boneName)
    {
        if (root == null)
            return null;

        var transforms = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == boneName)
                return transforms[i];
        }

        return null;
    }
}
