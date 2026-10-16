using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Only a fresh press on the visible tutorial button can become a click.</summary>
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4TutorialButton")]
public sealed class TutorialButton : AccessibleButton
{
    readonly HashSet<int> pressed = new HashSet<int>();
    public override void OnPointerDown(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable())
            pressed.Add(data.pointerId);
        base.OnPointerDown(data);
    }
    public override void OnPointerClick(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left && pressed.Remove(data.pointerId))
            base.OnPointerClick(data);
    }
    public override void OnPointerExit(PointerEventData data)
    { pressed.Remove(data.pointerId); base.OnPointerExit(data); }
    // Simulator keyboard/gamepad navigation is not a substitute for pointing practice.
    public override void OnSubmit(BaseEventData data) { }
    protected override void OnDisable() { pressed.Clear(); base.OnDisable(); }
}

/// <summary>Tracked devices use their 3D raycaster, never their stale 2D cursor position.</summary>
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4TutorialMouseRaycaster")]
public sealed class TutorialMouseRaycaster : GraphicRaycaster
{
    public override void Raycast(PointerEventData data, List<RaycastResult> results)
    {
        if (data is TrackedDeviceEventData) return;
        base.Raycast(data, results);
    }
}
