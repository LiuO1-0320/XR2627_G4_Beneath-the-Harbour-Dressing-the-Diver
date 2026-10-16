using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>One submission path shared by native XRI/keyboard and head gaze.</summary>
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4AccessibleButton")]
public class AccessibleButton : Button
{
    public bool gazeInStandardMode;
    public bool allowWhilePaused;
    public bool SubmitOnce(bool gaze)
    {
        if (!IsActive() || !IsInteractable()) return false;
        var input = GazeInput.Current;
        if (input && !input.Claim(this, gaze)) return false;
        onClick.Invoke();
        return true;
    }
    public override void OnPointerClick(PointerEventData data)
    { if (data.button == PointerEventData.InputButton.Left) SubmitOnce(false); }
    public override void OnSubmit(BaseEventData data) { SubmitOnce(false); }
}
