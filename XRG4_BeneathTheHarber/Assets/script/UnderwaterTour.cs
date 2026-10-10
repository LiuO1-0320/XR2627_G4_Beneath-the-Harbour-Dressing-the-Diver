using System.Collections.Generic;
using UnityEngine;

/// <summary>Local harbour atmosphere and gentle fish movement; no forced XR camera motion.</summary>
public class UnderwaterTour : MonoBehaviour
{
    readonly List<Transform> fish = new List<Transform>();
    readonly List<Vector3> origins = new List<Vector3>();
    Transform waterSurface, seabed;
    Camera viewer;
    bool submerged, oldFog;
    Color oldColor;
    FogMode oldMode;
    float oldDensity, oldStart, oldEnd;

    void Start()
    {
        waterSurface = transform.Find("Water_Surface");
        seabed = transform.Find("Seabed_Teleport");
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
        // Derive the water volume from its authored geometry so pool resizing also resizes the atmosphere.
        if (!waterSurface || !seabed) return;
        Vector3 waterCenter = waterSurface.localPosition;
        Vector3 waterHalfSize = waterSurface.localScale * 0.5f;
        float surfaceY = waterCenter.y - waterHalfSize.y;
        float bottomY = seabed.localPosition.y - seabed.localScale.y * 0.5f;
        bool inside = Mathf.Abs(p.x - waterCenter.x) < waterHalfSize.x &&
            Mathf.Abs(p.z - waterCenter.z) < waterHalfSize.z && p.y < surfaceY && p.y > bottomY;
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
