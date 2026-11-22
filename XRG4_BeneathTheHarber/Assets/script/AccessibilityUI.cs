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
    public void RestoreReset() { pending=null; confirming=false; open=false; placed=false; actionPage=0; continuingStage=null; shownSession=Guid.Empty; shownRevision=-1; if(panel) panel.SetActive(false); }

    SessionController controller;
    GazeInput input;
    GameObject panel, backing;
    RectTransform backingRect;
    SessionControlMode shownMode;
    bool wasIdle;
    Font font;
    Text heading, help;
    AccessibleButton standard, accessible, start, pause, skip, confirm, cancel, menu, slower, faster, nextActions;
    readonly List<AccessibleButton> actions = new List<AccessibleButton>();
    readonly List<AccessibleTaskAction> endpoints = new List<AccessibleTaskAction>();
    bool open, confirming;
    Guid confirmSession, shownSession;
    int confirmRevision, shownRevision;
    float scanAt;
    const float PanelScale = .0013f;
    const float SurfaceClearance = .12f;
    readonly List<AccessibleButton> visibleButtons = new List<AccessibleButton>();
    Vector3 menuOffset;
    Transform visitorOrigin;
    float checkClearanceAt, clearFraction = 1f, displayFraction = 1f;
    bool placed;
    int actionPage;
    ExperienceStage? continuingStage;
    Action pending;
    void Start()
    {
        controller = GetComponent<SessionController>(); input = GetComponent<GazeInput>();
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" },28);
        panel = new GameObject("Accessibility controls",typeof(RectTransform),typeof(Canvas),typeof(TrackedDeviceGraphicRaycaster));
        panel.transform.SetParent(transform,false); panel.transform.localScale = Vector3.one * PanelScale;
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(720,1040);
        // Same black backing as the other text panels, so the floating controls stay readable.
        backing = new GameObject("Backing",typeof(RectTransform),typeof(Image));
        backing.transform.SetParent(panel.transform,false);
        backingRect = backing.GetComponent<RectTransform>();
        backingRect.anchorMin = backingRect.anchorMax = backingRect.pivot = new Vector2(.5f, 1f);
        backingRect.anchoredPosition = Vector2.zero;
        backingRect.sizeDelta = new Vector2(720,1040);
        backing.GetComponent<Image>().color = new Color(.025f,.09f,.12f,.96f);
        backing.GetComponent<Image>().raycastTarget = false;
        backing.transform.SetAsFirstSibling();
        heading = Label("HEAD GAZE / 头部凝视",new Vector2(0,450),new Vector2(700,70));
        menu = Button("体验菜单 / Menu",0,360,()=>{ open=!open; confirming=false; if (!open) continuingStage=null; },true);
        standard = Button("Standard / 手柄",-175,260,()=>input.SetAccessible(false),true,330);
        accessible = Button("Accessible / 凝视",175,260,()=>input.SetAccessible(true),true,330);
        standard.gazeInStandardMode = accessible.gazeInStandardMode = true;
        start = Button("开始体验 / Start",0,160,()=>{ if (controller.Telemetry.ExecuteVisitor("Start").accepted) open=false; });
        pause = Button("暂停 / Pause",0,60,()=>controller.Telemetry.ExecuteVisitor(controller.Session.Status == SessionStatus.Paused ? "Resume" : "Pause"),true);
        skip = Button("跳过当前任务… / Skip…",0,-40,()=>
        {
            confirming=true; confirmSession=controller.Session.SessionId; confirmRevision=controller.Session.Revision;
        },true);
        confirm = Button("确认跳过 / Confirm",-175,-140,()=>
        {
            confirming=false;
            if (controller.Session.SessionId == confirmSession && controller.Session.Revision == confirmRevision) controller.Telemetry.ExecuteVisitor("Skip");
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
        var mode = controller.Telemetry.ControlMode;
        if (mode != shownMode)
        {
            pending = null;
            confirming = false;
            shownMode = mode;
        }
        bool test = mode == SessionControlMode.TeamTest;
        if (pending != null)
        {
            var command=pending; pending=null;
            if (state.SessionId==shownSession && state.Revision==shownRevision) command();
        }
        if (continuingStage.HasValue && state.Stage != continuingStage.Value)
        {
            open = false;
            actionPage = 0;
            continuingStage = null;
        }
        shownSession=state.SessionId; shownRevision=state.Revision;
        if (confirming && (state.SessionId!=confirmSession || state.Revision!=confirmRevision)) confirming=false;
        bool idle=state.Status==SessionStatus.Idle;
        bool active=state.Status==SessionStatus.Running || state.Status==SessionStatus.Paused;
        panel.SetActive(idle || active);
        if (idle != wasIdle) { placed=false; open=false; wasIdle=idle; }
        bool expanded=idle || open;
        backing.SetActive(expanded);
        heading.gameObject.SetActive(expanded); help.gameObject.SetActive(expanded);
        menu.gameObject.SetActive(!idle && active);
        standard.gameObject.SetActive(idle); accessible.gameObject.SetActive(idle);
        start.gameObject.SetActive(idle && test);
        start.interactable=controller.Telemetry && controller.Telemetry.Fault.Length==0;
        pause.gameObject.SetActive(expanded && active); skip.gameObject.SetActive(expanded && active && test);
        pause.GetComponentInChildren<Text>(true).text=state.Status==SessionStatus.Paused ? "继续 / Resume" : "暂停 / Pause";
        skip.interactable=state.Stage!=ExperienceStage.ExperienceComplete && state.Stage!=ExperienceStage.FreeExploration
            && !(state.Stage==ExperienceStage.SeabedCleanup && state.CleanupResolved);
        confirm.gameObject.SetActive(test && expanded && confirming); cancel.gameObject.SetActive(test && expanded && confirming);
        bool gazeSettings = expanded && (idle || input.Accessible);
        slower.gameObject.SetActive(gazeSettings); faster.gameObject.SetActive(gazeSettings);
        help.text=(input.Accessible ? "Accessible" : "Standard")+" · "+input.dwellSeconds.ToString("0.0")+" s\n"
            +"看向按钮，圆环填满后选择；移开取消。\nLook away to cancel or re-arm. Controllers remain available.";
        if (idle && !test) help.text = "选好操作方式后，请等待工作人员开始。\nChoose your controls, then wait for staff to start.";
        else if (active && !input.Accessible) help.text = "随时可以暂停，准备好后点继续。\nPause whenever you like. Select Resume when ready.";
        if (Time.unscaledTime>=scanAt)
        {
            scanAt=Time.unscaledTime+1;
            foreach (var endpoint in FindObjectsOfType<AccessibleTaskAction>(true))
                if (!endpoints.Contains(endpoint))
                    RegisterAction(endpoint);
        }
        // In free exploration, keep the actions for the stand the visitor is using together.
        var menuStage = state.Stage;
        if (menuStage == ExperienceStage.FreeExploration && expanded && input.Accessible && StageCoordinator.Current)
        {
            if (StageCoordinator.Current.IsVisitorInNarrationRegion(ExperienceStage.SeabedCleanup))
                menuStage = ExperienceStage.SeabedCleanup;
            else
            {
                foreach (var endpoint in endpoints)
                    if (endpoint && StageCoordinator.Current.IsVisitorInNarrationRegion(endpoint.stage))
                    {
                        menuStage = endpoint.stage;
                        break;
                    }
            }
        }
        int matching=0;
        foreach (var endpoint in endpoints)
            if (endpoint && (endpoint.stage==menuStage || menuStage==ExperienceStage.FreeExploration)) matching++;
        int pages=Mathf.Max(1,(matching+2)/3); actionPage%=pages;
        int row=0, visibleCount=0;
        for (int i=0;i<actions.Count;i++)
        {
            var endpoint=endpoints[i];
            bool matches=endpoint && (endpoint.stage==menuStage || menuStage==ExperienceStage.FreeExploration);
            int index=matches ? row++ : -1;
            bool show=matches && expanded && active && input.Accessible && index/3==actionPage;
            actions[i].gameObject.SetActive(show);
            if (!show) continue;
            actions[i].GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-470-(index%3)*95);
            visibleCount++;
            actions[i].interactable=endpoint.Available(state,out string reason);
            actions[i].GetComponentInChildren<Text>().text=actions[i].interactable ? endpoint.caption : endpoint.caption+"\n"+reason;
        }
        if (expanded && active && input.Accessible && menuStage == ExperienceStage.SeabedCleanup)
            help.text = state.Stage == ExperienceStage.SeabedCleanup && state.CleanupResolved
                ? "沿绳上浮返回平台 / Ascend the rope to the platform"
                : "靠近并凝视垃圾，带回篮边凝视篮子投放。\nLook at a piece of litter, carry it back, then look at the basket.";
        else if (expanded && active && input.Accessible && state.Stage == ExperienceStage.Briefing)
            help.text = "跟随教程卡片练习选择、拿球和放手。\nFollow the practice card to try selecting, picking up and letting go.";
        else if (expanded && active && input.Accessible && visibleCount==0) help.text += "\n物理辅助操作待 M3 接入 / Assisted tasks awaiting M3";
        bool showPages = expanded && active && input.Accessible && pages > 1;
        nextActions.gameObject.SetActive(showPages);
        // Extend downwards only. Every dwell target keeps its original position.
        float extraHeight = showPages ? 295f : 95f * Mathf.Max(0, visibleCount - 1);
        var size = new Vector2(720f, 1040f + extraHeight);
        if (backingRect.sizeDelta != size) backingRect.sizeDelta = size;
        if (idle && state.Revision!=0 && !placed) confirming=false;
    }
    void RegisterAction(AccessibleTaskAction endpoint)
    {
        int index = endpoints.FindIndex(other => other && (other.stage > endpoint.stage ||
            (other.stage == endpoint.stage && other.menuOrder > endpoint.menuOrder)));
        if (index < 0) index = endpoints.Count;
        var button = Button(endpoint.caption, 0, -470, () =>
        {
            var stage = controller.Session.Stage;
            if (!endpoint || !endpoint.Execute(controller.Session, shownSession, shownRevision)) return;
            if (endpoint.keepMenuOpen) continuingStage = stage;
            else open = false;
        });
        var caption = button.GetComponentInChildren<Text>();
        caption.resizeTextForBestFit = true;
        caption.resizeTextMinSize = 20;
        caption.resizeTextMaxSize = caption.fontSize;
        endpoints.Insert(index, endpoint);
        actions.Insert(index, button);
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
            menuOffset = forward * 2.2f + offset;
            panel.transform.rotation = Quaternion.LookRotation(forward);
            var origin = viewer.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
            visitorOrigin = origin ? origin.transform : null;
            checkClearanceAt = 0;
            placed = true;
        }
        if (Time.unscaledTime >= checkClearanceAt)
        {
            checkClearanceAt = Time.unscaledTime + .1f;
            clearFraction = 1f;
            panel.GetComponentsInChildren(false, visibleButtons);
            foreach (var button in visibleButtons)
            {
                var rect = (RectTransform)button.transform;
                var centre = menuOffset + panel.transform.rotation * (rect.localPosition * PanelScale);
                var edge = panel.transform.right * (rect.rect.width * .45f * PanelScale);
                CheckSightline(viewer.position, centre);
                CheckSightline(viewer.position, centre - edge);
                CheckSightline(viewer.position, centre + edge);
            }
        }
        // Move the whole menu towards the eye on the same sightlines. A button keeps
        // its angular position and size while the visitor dwells in front of an exhibit.
        displayFraction = Mathf.Min(clearFraction, displayFraction + Time.unscaledDeltaTime);
        panel.transform.position = viewer.position + menuOffset * displayFraction;
        panel.transform.localScale = Vector3.one * (PanelScale * displayFraction);
    }

    void CheckSightline(Vector3 eye, Vector3 offset)
    {
        float distance = offset.magnitude;
        var ray = new Ray(eye, offset);
        if (!Physics.Raycast(ray, out var hit, distance, input.occlusionMask, QueryTriggerInteraction.Ignore)) return;
        if (visitorOrigin && hit.collider.transform.IsChildOf(visitorOrigin))
        {
            // Match the head-gaze raycaster's exclusion of the visitor's own rig.
            foreach (var other in Physics.RaycastAll(ray, distance, input.occlusionMask, QueryTriggerInteraction.Ignore))
                if (!other.collider.transform.IsChildOf(visitorOrigin)) FitBefore(other.distance, distance);
        }
        else FitBefore(hit.distance, distance);
    }

    void FitBefore(float obstacleDistance, float buttonDistance)
    {
        clearFraction = Mathf.Min(clearFraction, Mathf.Max(.01f, (obstacleDistance - SurfaceClearance) / buttonDistance));
    }
    AccessibleButton Button(string caption,float x,float y,Action action,bool paused=false,float width=650)
    {
        var obj=new GameObject(caption,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(AccessibleButton));
        obj.transform.SetParent(panel.transform,false);
        var rect=obj.GetComponent<RectTransform>(); rect.anchoredPosition=new Vector2(x,y); rect.sizeDelta=new Vector2(width,80);
        var image=obj.GetComponent<Image>(); image.color=Color.clear;
        var frame=OptionFrame.Apply(obj);
        var button=obj.GetComponent<AccessibleButton>(); if (frame != null) button.targetGraphic=frame; button.allowWhilePaused=paused;
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
