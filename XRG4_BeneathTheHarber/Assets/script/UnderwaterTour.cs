using System.Collections.Generic;
using UnityEngine;

/// <summary>Local harbour atmosphere and gentle fish movement; no forced XR camera motion.</summary>
public class UnderwaterTour : MonoBehaviour
{
    readonly List<Transform> fish = new List<Transform>();
    readonly List<Vector3> origins = new List<Vector3>();
    Camera viewer;
    bool submerged, oldFog;
    Color oldColor;
    FogMode oldMode;
    float oldDensity, oldStart, oldEnd;

    void Start()
    {
        foreach (Transform child in transform)
            if (child.name.StartsWith("Fish_"))
            { fish.Add(child); origins.Add(child.localPosition); }
    }

    void Update()
    {
        for (int i = 0; i < fish.Count; i++)
        {
            float phase = Time.time * 0.22f + i * 0.7f;
            fish[i].localPosition = origins[i] + new Vector3(Mathf.Sin(phase) * 2.5f,
                Mathf.Sin(phase * 1.7f) * 0.25f, Mathf.Cos(phase) * 1.5f);
            fish[i].localRotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(phase) * 2.5f, 0, -Mathf.Sin(phase) * 1.5f));
        }
        if (!viewer) viewer = Camera.main;
        if (!viewer) return;
        Vector3 p = transform.InverseTransformPoint(viewer.transform.position);
        bool inside = p.x > -28 && p.x < 16 && Mathf.Abs(p.z) < 16 && p.y < -0.3f && p.y > -9;
        if (inside == submerged) return;
        if (inside)
        {
            oldFog = RenderSettings.fog; oldColor = RenderSettings.fogColor;
            oldMode = RenderSettings.fogMode; oldDensity = RenderSettings.fogDensity;
            oldStart = RenderSettings.fogStartDistance; oldEnd = RenderSettings.fogEndDistance;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.025f, 0.22f, 0.28f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.045f;
            submerged = true;
        }
        else RestoreFog();
    }

    void RestoreFog()
    {
        if (!submerged) return;
        RenderSettings.fog = oldFog; RenderSettings.fogColor = oldColor;
        RenderSettings.fogMode = oldMode; RenderSettings.fogDensity = oldDensity;
        RenderSettings.fogStartDistance = oldStart; RenderSettings.fogEndDistance = oldEnd;
        submerged = false;
    }
    void OnDisable() { RestoreFog(); }
}
