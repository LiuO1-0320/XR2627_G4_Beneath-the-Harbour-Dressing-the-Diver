using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4AccessibilityUI")]
public sealed class AccessibilityUI : MonoBehaviour, IResettable
{
    public int ResetOrder => 100;
    public void PrepareReset() { pending=null; confirming=false; }
    public void RestoreReset() { pending=null; confirming=false; open=false; placed=false; actionPage=0; shownSession=Guid.Empty; shownRevision=-1; if(panel) panel.SetActive(false); }

    SessionController controller;
    GazeInput input;
    GameObject panel;
    Font font;
    Text heading, help;
    AccessibleButton standard, accessible, start, pause, skip, confirm, cancel, menu, slower, faster, nextActions;
    readonly List<AccessibleButton> actions = new List<AccessibleButton>();
    readonly List<AccessibleTaskAction> endpoints = new List<AccessibleTaskAction>();
    bool open, confirming;
    Guid confirmSession, shownSession;
    int confirmRevision, shownRevision;
    float scanAt;
    Vector3 previousHead;
    bool placed;
    int actionPage;
    Action pending;
    void Start()
    {
        controller = GetComponent<SessionController>(); input = GetComponent<GazeInput>();
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" },28);
        panel = new GameObject("Accessibility controls",typeof(RectTransform),typeof(Canvas),typeof(TrackedDeviceGraphicRaycaster));
        panel.transform.SetParent(transform,false); panel.transform.localScale = Vector3.one*.0013f;
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(720,1040);
        heading = Label("HEAD GAZE / 头部凝视",new Vector2(0,450),new Vector2(700,70));
        menu = Button("辅助菜单 / Accessibility",0,360,()=>open=!open,true);
        standard = Button("Standard / 手柄",-175,260,()=>input.SetAccessible(false),true,330);
        accessible = Button("Accessible / 凝视",175,260,()=>input.SetAccessible(true),true,330);
        standard.gazeInStandardMode = accessible.gazeInStandardMode = true;
        start = Button("开始体验 / Start",0,160,()=>{ controller.StartSession(); open=false; });
        pause = Button("暂停 / Pause",0,60,()=>{ if (controller.Session.Status == SessionStatus.Paused) controller.ResumeSession(); else controller.PauseSession(); },true);
        skip = Button("跳过当前任务… / Skip…",0,-40,()=>
        {
            confirming=true; confirmSession=controller.Session.SessionId; confirmRevision=controller.Session.Revision;
        },true);
        confirm = Button("确认跳过 / Confirm",-175,-140,()=>
        {
            confirming=false;
            if (controller.Session.SessionId == confirmSession && controller.Session.Revision == confirmRevision) controller.SkipStage();
        },true,330);
        cancel = Button("取消 / Cancel",175,-140,()=>confirming=false,true,330);
        slower = Button("停留 +0.5 秒",-175,-240,()=>input.dwellSeconds=Mathf.Min(5,input.dwellSeconds+.5f),true,330);
        faster = Button("停留 −0.5 秒",175,-240,()=>input.dwellSeconds=Mathf.Max(.5f,input.dwellSeconds-.5f),true,330);
        help = Label("",new Vector2(0,-355),new Vector2(690,160)); help.fontSize=23;
        nextActions = Button("更多辅助操作 / More actions",0,-765,()=>actionPage++);
    }
    void Update()
    {
        if (!controller || !panel) return;
        var state = controller.Session;
        if (pending != null)
        {
            var command=pending; pending=null;
            if (state.SessionId==shownSession && state.Revision==shownRevision) command();
        }
        shownSession=state.SessionId; shownRevision=state.Revision;
        if (confirming && (state.SessionId!=confirmSession || state.Revision!=confirmRevision)) confirming=false;
        bool idle=state.Status==SessionStatus.Idle;
        bool active=state.Status==SessionStatus.Running || state.Status==SessionStatus.Paused;
        panel.SetActive(idle || active && input.Accessible);
        bool expanded=idle || open;
        heading.gameObject.SetActive(expanded); help.gameObject.SetActive(expanded);
        menu.gameObject.SetActive(!idle && active);
        standard.gameObject.SetActive(idle); accessible.gameObject.SetActive(idle);
        start.gameObject.SetActive(idle);
        start.interactable=controller.Telemetry && controller.Telemetry.Fault.Length==0;
        pause.gameObject.SetActive(expanded && active); skip.gameObject.SetActive(expanded && active);
        pause.GetComponentInChildren<Text>().text=state.Status==SessionStatus.Paused ? "继续 / Resume" : "暂停 / Pause";
        skip.interactable=state.Stage!=ExperienceStage.ExperienceComplete && state.Stage!=ExperienceStage.FreeExploration
            && !(state.Stage==ExperienceStage.SeabedCleanup && state.CleanupResolved);
        confirm.gameObject.SetActive(expanded && confirming); cancel.gameObject.SetActive(expanded && confirming);
        slower.gameObject.SetActive(expanded); faster.gameObject.SetActive(expanded);
        help.text=(input.Accessible ? "Accessible" : "Standard")+" · "+input.dwellSeconds.ToString("0.0")+" s\n"
            +"看向按钮，圆环填满后选择；移开取消。\nLook away to cancel or re-arm. Controllers remain available.";
        if (Time.unscaledTime>=scanAt)
        {
            scanAt=Time.unscaledTime+1;
            foreach (var endpoint in FindObjectsOfType<AccessibleTaskAction>(true))
                if (!endpoints.Contains(endpoint))
                {
                    var captured=endpoint; endpoints.Add(endpoint);
                    actions.Add(Button(endpoint.caption,0,-470-(actions.Count*95),()=>captured.Execute(controller.Session,shownSession,shownRevision)));
                }
        }
        int matching=0;
        foreach (var endpoint in endpoints)
            if (endpoint && (endpoint.stage==state.Stage || state.Stage==ExperienceStage.FreeExploration)) matching++;
        int pages=Mathf.Max(1,(matching+2)/3); actionPage%=pages;
        bool any=false; int row=0;
        for (int i=0;i<actions.Count;i++)
        {
            var endpoint=endpoints[i];
            bool matches=endpoint && (endpoint.stage==state.Stage || state.Stage==ExperienceStage.FreeExploration);
            int index=matches ? row++ : -1;
            bool show=matches && expanded && active && index/3==actionPage;
            actions[i].gameObject.SetActive(show);
            if (!show) continue;
            actions[i].GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-470-(index%3)*95);
            any=true; actions[i].interactable=endpoint.Available(state,out string reason);
            actions[i].GetComponentInChildren<Text>().text=actions[i].interactable ? endpoint.caption : endpoint.caption+"\n"+reason;
        }
        if (expanded && active && !any) help.text += "\n物理辅助操作待 M3 接入 / Assisted tasks awaiting M3";
        nextActions.gameObject.SetActive(expanded && active && pages>1);
        if (idle && state.Revision!=0 && !placed) confirming=false;
    }
    void LateUpdate()
    {
        if (!panel || !panel.activeSelf || !Camera.main) { placed=false; return; }
        var viewer=Camera.main.transform;
        // Recenter only when the menu has fallen well outside view and no dwell is active.
        // Never chase small head turns while the visitor is trying to point at a button.
        if (placed && !confirming && input.TargetName.Length==0
            && Vector3.Dot(viewer.forward,(panel.transform.position-viewer.position).normalized)<.15f) placed=false;
        if (!placed)
        {
            var forward=Vector3.ProjectOnPlane(viewer.forward,Vector3.up).normalized;
            if (forward.sqrMagnitude<.01f) forward=Vector3.forward;
            var offset=controller.Session.Status==SessionStatus.Idle ? Vector3.zero : -Vector3.Cross(Vector3.up,forward)*1.05f;
            panel.transform.SetPositionAndRotation(viewer.position+forward*2.2f+offset,Quaternion.LookRotation(forward));
            previousHead=viewer.position; placed=true;
        }
        else { panel.transform.position+=viewer.position-previousHead; previousHead=viewer.position; }
    }
    AccessibleButton Button(string caption,float x,float y,Action action,bool paused=false,float width=650)
    {
        var obj=new GameObject(caption,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(AccessibleButton));
        obj.transform.SetParent(panel.transform,false);
        var rect=obj.GetComponent<RectTransform>(); rect.anchoredPosition=new Vector2(x,y); rect.sizeDelta=new Vector2(width,80);
        var image=obj.GetComponent<Image>(); image.color=new Color(.04f,.28f,.32f,.97f);
        var button=obj.GetComponent<AccessibleButton>(); button.targetGraphic=image; button.allowWhilePaused=paused;
        button.onClick.AddListener(()=>{ if (pending==null) pending=action; });
        var text=Label(caption,Vector2.zero,new Vector2(width-15,75)); text.transform.SetParent(obj.transform,false);
        return button;
    }
    Text Label(string caption,Vector2 position,Vector2 size)
    {
        var obj=new GameObject("Caption",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)); obj.transform.SetParent(panel.transform,false);
        var text=obj.GetComponent<Text>(); text.rectTransform.anchoredPosition=position; text.rectTransform.sizeDelta=size;
        text.font=font; text.fontSize=27; text.color=Color.white; text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false; text.text=caption; return text;
    }
    void OnDestroy() { if(panel)Destroy(panel); if(font)Destroy(font); }
}
