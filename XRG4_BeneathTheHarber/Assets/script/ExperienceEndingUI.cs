using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Visitor-facing completion choices and a persistent exit button during free exploration.</summary>
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4ExperienceEndingUI")]
public sealed class ExperienceEndingUI : MonoBehaviour, IResettable
{
    public int ResetOrder => 100;
    public void PrepareReset() { queuedFree=queuedExit=false; }
    public void RestoreReset() { queuedFree=queuedExit=false; shownSession=Guid.Empty; if(panel) { GazePanelAnchor.Clear(panel.transform); panel.SetActive(false); } }

    SessionController controller;
    GameObject panel, explore, exit;
    RectTransform rect;
    Text message;
    Font font;
    Guid shownSession;
    ExperienceStage shownStage;
    bool queuedFree, queuedExit;
    void Start()
    {
        controller = GetComponent<SessionController>();
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 30);
        panel = new GameObject("Experience completion and exit", typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
        panel.transform.SetParent(transform, false); panel.transform.localScale = Vector3.one * .0015f;
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        panel.GetComponent<Canvas>().sortingOrder = 100;
        rect = panel.GetComponent<RectTransform>();
        var background = panel.AddComponent<Image>(); background.color = new Color(.02f,.07f,.09f,.98f);
        message = Label(panel.transform, "", new Vector2(0,65), new Vector2(880,240));
        explore = Button("自由探索 / Explore freely", -225, () => queuedFree = true);
        exit = Button("退出体验 / Exit experience", 225, () => queuedExit = true);
        exit.GetComponent<AccessibleButton>().allowWhilePaused = true;
        panel.SetActive(false);
    }
    void Update()
    {
        if (!controller) return;
        var state = controller.Session;
        // Execute outside XR button dispatch so stage cleanup cannot invalidate the active UI event.
        if (queuedFree || queuedExit)
        {
            bool free = queuedFree && !queuedExit; queuedFree = queuedExit = false;
            if (state.SessionId == shownSession && state.Stage == shownStage)
            {
                SessionTelemetry.Report("VisitorEndingChoice", this, free ? "FreeExploration" : "Exit");
                if (free) state.ChooseFreeExploration(shownSession); else state.ExitExperience(shownSession);
            }
        }
        bool finished = state.Status == SessionStatus.Completed;
        bool choose = state.Stage == ExperienceStage.ExperienceComplete;
        bool exploring = state.Stage == ExperienceStage.FreeExploration;
        bool visible = state.Status != SessionStatus.Idle && (finished || choose || exploring);
        panel.SetActive(visible); if (!visible) return;
        if (shownSession != state.SessionId || shownStage != state.Stage) GazePanelAnchor.Clear(panel.transform);
        shownSession = state.SessionId; shownStage = state.Stage;
        rect.sizeDelta = exploring && !finished ? new Vector2(440,110) : new Vector2(960,470);
        message.gameObject.SetActive(!exploring || finished);
        message.text = finished
            ? "体验已结束，感谢参与！\n请停留原地，等待工作人员协助摘下头显。\n\nThank you for visiting!\nStay still and ask staff to help remove the headset."
            : "你已完成本次体验！\n可以继续自由探索，也可以现在退出。\n\nYour guided experience is complete.\nExplore freely, or finish your visit.";
        explore.SetActive(choose && !finished); exit.SetActive(!finished);
        exit.GetComponent<RectTransform>().anchoredPosition = exploring ? Vector2.zero : new Vector2(225,-160);
        bool enabled = state.Status == SessionStatus.Running;
        explore.GetComponent<Button>().interactable = enabled;
        exit.GetComponent<Button>().interactable = enabled || state.Status == SessionStatus.Paused;
    }
    void LateUpdate()
    {
        if (!panel || !panel.activeSelf || !Camera.main) return;
        if (GazePanelAnchor.KeepPose(panel.transform)) return;
        var viewer = Camera.main.transform;
        bool compact = controller.Session.Stage == ExperienceStage.FreeExploration && controller.Session.Status != SessionStatus.Completed;
        var forward = Vector3.ProjectOnPlane(viewer.forward,Vector3.up).normalized;
        if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
        var right = Vector3.Cross(Vector3.up,forward);
        panel.transform.SetPositionAndRotation(viewer.position + forward*1.6f + (compact ? right*.55f + Vector3.down*.55f : Vector3.down*.12f), Quaternion.LookRotation(forward));
    }
    GameObject Button(string caption, float x, UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject(caption,typeof(RectTransform),typeof(Image),typeof(AccessibleButton));
        obj.transform.SetParent(panel.transform,false);
        var r = obj.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(420,95); r.anchoredPosition = new Vector2(x,-160);
        var image = obj.GetComponent<Image>(); image.color = new Color(.08f,.35f,.38f);
        var button = obj.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        Label(obj.transform,caption,Vector2.zero,new Vector2(400,85)); return obj;
    }
    Text Label(Transform parent,string caption,Vector2 position,Vector2 size)
    {
        var obj = new GameObject("Text",typeof(RectTransform),typeof(Text)); obj.transform.SetParent(parent,false);
        var r = obj.GetComponent<RectTransform>(); r.sizeDelta=size; r.anchoredPosition=position;
        var text=obj.GetComponent<Text>(); text.font=font; text.fontSize=29; text.alignment=TextAnchor.MiddleCenter;
        text.color=Color.white; text.text=caption; text.raycastTarget=false; return text;
    }
    void OnDestroy() { if (panel) Destroy(panel); if (font) Destroy(font); }
}
