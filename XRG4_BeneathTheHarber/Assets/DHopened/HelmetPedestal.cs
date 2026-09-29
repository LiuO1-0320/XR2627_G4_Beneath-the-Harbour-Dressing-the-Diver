using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cube1 and Cube2 pull a loose helmet onto the top face and hold it upright.
/// </summary>
[DisallowMultipleComponent]
public class HelmetPedestal : MonoBehaviour
{
    const float ExtraReach = 0.18f;
    const float AboveSlack = 0.32f;
    const float BelowSlack = 0.28f;

    static readonly List<HelmetPedestal> s_All = new List<HelmetPedestal>();
    static bool s_Searched;

    HelmetStand m_Occupied;
    BoxCollider m_Box;

    public static void EnsureCubes()
    {
        if (s_Searched && s_All.Count > 0)
            return;

        s_Searched = true;
        var transforms = FindObjectsOfType<Transform>();
        for (var i = 0; i < transforms.Length; i++)
        {
            var current = transforms[i];
            if (!IsPedestalName(current.name))
                continue;

            if (current.GetComponent<HelmetPedestal>() == null)
                current.gameObject.AddComponent<HelmetPedestal>();
        }
    }

    public static HelmetPedestal Closest(Transform helmet, HelmetStand requester)
    {
        EnsureCubes();
        HelmetPedestal best = null;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < s_All.Count; i++)
        {
            var pedestal = s_All[i];
            if (pedestal == null || !pedestal.CanAccept(requester))
                continue;

            if (!pedestal.IsInRange(helmet, out var distance))
                continue;

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = pedestal;
        }

        return best;
    }

    void Awake()
    {
        m_Box = GetComponent<BoxCollider>();
    }

    void OnEnable()
    {
        if (!s_All.Contains(this))
            s_All.Add(this);
    }

    void OnDisable()
    {
        s_All.Remove(this);
        m_Occupied = null;
    }

    public bool TryClaim(HelmetStand stand)
    {
        if (stand == null)
            return false;

        if (m_Occupied != null && m_Occupied != stand)
            return false;

        m_Occupied = stand;
        return true;
    }

    public void Clear(HelmetStand stand)
    {
        if (m_Occupied == stand)
            m_Occupied = null;
    }

    public Vector3 Up => transform.up;

    public Vector3 TopCenter
    {
        get
        {
            if (m_Box == null)
                m_Box = GetComponent<BoxCollider>();

            if (m_Box == null)
                return transform.position + transform.up * (transform.lossyScale.y * 0.5f);

            return transform.TransformPoint(m_Box.center + Vector3.up * (m_Box.size.y * 0.5f));
        }
    }

    bool CanAccept(HelmetStand requester)
    {
        return m_Occupied == null || m_Occupied == requester;
    }

    bool IsInRange(Transform helmet, out float horizontalDistance)
    {
        horizontalDistance = float.MaxValue;
        if (helmet == null)
            return false;

        var center = HelmetStand.RenderCenter(helmet);
        var up = transform.up;
        var offset = center - transform.position;
        horizontalDistance = Vector3.ProjectOnPlane(offset, up).magnitude;
        if (horizontalDistance > HorizontalReach())
            return false;

        var bottom = center - up * HelmetStand.ExtentAlong(HelmetStand.RenderBounds(helmet), up);
        var top = Vector3.Dot(TopCenter, up);
        var bottomHeight = Vector3.Dot(bottom, up);
        return bottomHeight < top + AboveSlack && bottomHeight > top - BelowSlack;
    }

    float HorizontalReach()
    {
        var scale = transform.lossyScale;
        var size = m_Box != null ? m_Box.size : Vector3.one;
        var half = Mathf.Max(Mathf.Abs(scale.x * size.x), Mathf.Abs(scale.z * size.z)) * 0.5f;
        return half + ExtraReach;
    }

    static bool IsPedestalName(string objectName)
    {
        return objectName == "Cube1"
            || objectName == "Cube2"
            || objectName == "cube1"
            || objectName == "cube2";
    }
}
