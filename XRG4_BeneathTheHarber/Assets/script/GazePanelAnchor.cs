using UnityEngine;

/// <summary>Translate with the visitor but do not rotate away from their head-gaze ray.</summary>
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4GazePanelAnchor")]
public sealed class GazePanelAnchor : MonoBehaviour
{
    bool anchored;
    Vector3 previousHead;
    public static bool KeepPose(Transform panel)
    {
        var anchor = panel.GetComponent<GazePanelAnchor>();
        if (!GazeInput.Current || !GazeInput.Current.Accessible || !Camera.main)
        { if (anchor) anchor.anchored = false; return false; }
        if (!anchor) anchor = panel.gameObject.AddComponent<GazePanelAnchor>();
        var head = Camera.main.transform.position;
        if (!anchor.anchored) { anchor.anchored = true; anchor.previousHead = head; return false; }
        panel.position += head-anchor.previousHead; anchor.previousHead = head;
        return true;
    }
    public static void Clear(Transform panel)
    { var anchor = panel.GetComponent<GazePanelAnchor>(); if (anchor) anchor.anchored = false; }
    void OnDisable() { anchored = false; }
}
