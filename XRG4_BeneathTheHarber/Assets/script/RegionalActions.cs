using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>Untimed briefing practice on a visitor-height, world-anchored card.</summary>
[DisallowMultipleComponent]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "M4RegionalActions")]
public sealed class RegionalActions : MonoBehaviour, IResettable
{
    public Font font;
    public int ResetOrder => 90;
    readonly BriefingPractice practice = new BriefingPractice();
    GameObject panel, ball;
    Text instruction, english, heading, reassurance, actionCaption;
    readonly Image[] steps = new Image[3];
    Sprite rounded;
    Texture2D roundedTexture;
    bool placed;
    AccessibleButton left, right, action, relocate;
    XRGrabInteractable grab;
    Rigidbody ballBody;
    Vector3 positionBeforeGazeGrab;
    bool releaseNeedsLookAway;
    Material ballMaterial;
    Guid session;
    bool submitted, wasRunning;
    XRBaseInputInteractor holdingController;
    Guid holdingSession;
    int holdingRevision;
    bool GazeHolding => practice.HeldBy == BriefingPractice.Input.HeadGaze;
    int pendingTarget = -1;
    int lastTransitionFrame = -1;
    bool Accessible => GazeInput.Current && GazeInput.Current.Accessible;
    bool Running
    {
        get
        {
            var c = StageCoordinator.Current;
            return c && c.Controller.Session.ActivityId == session &&
                c.Controller.Session.Stage == ExperienceStage.Briefing && c.Controller.Session.Status == SessionStatus.Running;
        }
    }
    public void PrepareReset() { ClearBall(); }
    public void RestoreReset()
    {
        practice.Reset(); session = Guid.Empty; submitted = wasRunning = placed = false;
        pendingTarget = -1;
        if (panel) panel.SetActive(false);
    }
    void Start()
    {
        BuildRoundedSprite();
        panel = new GameObject("Briefing practice card", typeof(RectTransform), typeof(Canvas), typeof(TutorialMouseRaycaster), typeof(TrackedDeviceGraphicRaycaster));
        // The gaze scanner discovers world canvases in loaded scenes. OnDestroy owns this runtime card.
        panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        panel.GetComponent<Canvas>().worldCamera = Camera.main;
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(1100,660);
        panel.transform.localScale = Vector3.one*.0013f;
        var backing = Element<Image>("Rounded card", panel.transform, Vector2.zero, new Vector2(1100,660));
        backing.sprite = rounded; backing.type = Image.Type.Sliced;
        backing.raycastTarget = true;
        backing.color = new Color(.025f,.075f,.1f,.98f);
        heading = Label("Encouragement", new Vector2(0,240), new Vector2(1000,80), 38, Color.white);
        for (int i=0;i<3;i++)
        {
            steps[i] = Element<Image>("Practice progress " + (i+1), panel.transform, new Vector2((i-1)*300,169), new Vector2(260,8));
            steps[i].sprite = rounded; steps[i].type = Image.Type.Sliced;
        }
        instruction = Label("Current action", new Vector2(0,75), new Vector2(990,135), 32, Color.white);
        english = Label("English instruction", new Vector2(0,-28), new Vector2(990,85), 25, new Color(.78f,.88f,.9f));
        reassurance = Label("Comfort reminder", new Vector2(0,-245), new Vector2(1000,85), 22, new Color(.69f,.81f,.84f));
        left = Button("试着选我\nTry this button", -250, () => SelectTarget(0));
        right = Button("也来试试这边\nTry this side", 250, () => SelectTarget(1));
        action = Button("Practice action", 0, Act);
        var actionRect = action.GetComponent<RectTransform>();
        actionRect.sizeDelta = new Vector2(760,110);
        actionCaption = action.GetComponentInChildren<Text>(true);
        actionCaption.rectTransform.sizeDelta = new Vector2(730,100);
        relocate = Button("把球带回我面前\nBring the ball back", 250, Reposition);
        Refresh();
    }
    void Update()
    {
        if (pendingTarget >= 0)
        {
            int target = pendingTarget; pendingTarget = -1;
            if (Running && !submitted && practice.Select(target))
            {
                lastTransitionFrame = Time.frameCount;
                StageCoordinator.Current.Audio.PlayFeedback(false);
                SessionTelemetry.Report("TutorialTargetSelected", this, target.ToString());
            }
        }
        Refresh();
    }
    void Refresh()
    {
        if (!panel) return;
        var c = StageCoordinator.Current;
        if (!c) { panel.SetActive(false); ClearBall(); return; }
        var state = c.Controller.Session;
        if (session != state.ActivityId)
        { ClearBall(); practice.Reset(); pendingTarget = -1; submitted = placed = false; session = state.ActivityId; }
        bool briefing = state.Stage == ExperienceStage.Briefing &&
            (state.Status == SessionStatus.Running || state.Status == SessionStatus.Paused);
        if (briefing && !placed) PlacePanel();
        panel.SetActive(briefing && placed);
        bool running = Running;
        if (!running) submitted = false;
        if (!briefing) { ClearBall(); wasRunning = false; return; }
        if (grab && ((wasRunning && !running) || (practice.Holding && holdingRevision != state.Revision) ||
            (GazeHolding && !Accessible)))
            CancelHold();
        wasRunning = running;
        if (practice.Ready && ball) ClearBall();
        if (grab) grab.enabled = running && !practice.Ready && !GazeHolding;
        if (GazeHolding && running && Camera.main) FollowViewer();
        if (practice.Step == 2 && running && !ball) SpawnBall();
        left.gameObject.SetActive(practice.Step == 0);
        right.gameObject.SetActive(practice.Step == 1);
        action.gameObject.SetActive(practice.Ready || GazeHolding);
        relocate.gameObject.SetActive(practice.Step == 2 && !practice.Holding);
        bool canAct = running && !submitted;
        left.interactable = canAct && practice.Step == 0 && pendingTarget < 0;
        right.interactable = canAct && practice.Step == 1 && pendingTarget < 0;
        relocate.interactable = canAct;
        if (GazeHolding && releaseNeedsLookAway) releaseNeedsLookAway = LookingAtAction();
        action.interactable = canAct && !releaseNeedsLookAway;
        for (int i=0;i<3;i++) steps[i].color = i < practice.Step ? new Color(.35f,.84f,.65f) :
            i == practice.Step ? new Color(.95f,.76f,.37f) : new Color(.17f,.26f,.3f);
        reassurance.text = "不用赶时间，按你的节奏来。\nTake your time. We're here to help.";
        if (!running)
        {
            heading.text = "先歇一会儿，也没关系";
            instruction.text = "进度已经为你保留。\n准备好后，请工作人员帮你继续。";
            english.text = "Your progress is safe. Ask staff to resume whenever you're ready.";
        }
        else if (practice.Step == 0)
        {
            heading.text = "欢迎！我们先轻松试一试 · 1 / 3";
            instruction.text = Accessible ? "看向下方亮起的按钮。\n保持凝视，等小圆环填满就好。" : "把手柄的射线指向下方按钮，\n轻按一下食指处的扳机。";
            english.text = Accessible ? "Look at the button below. Let the gaze ring fill." : "Point your controller ray at the button below. Press the trigger once.";
        }
        else if (practice.Step == 1)
        {
            heading.text = "很好，你已经会选择了！ · 2 / 3";
            instruction.text = Accessible ? "先移开视线，\n再看看另一边亮起的按钮。" : "松开扳机，把射线移到另一边，\n再轻轻点一次。";
            english.text = Accessible ? "Look away, then try the button on the other side." : "Release the trigger, point at the other button, and try one more click.";
        }
        else if (practice.Step == 2)
        {
            heading.text = practice.Holding ? "拿到了！做得很好 · 3 / 3" : "最后，试着拿起小球 · 3 / 3";
            if (GazeHolding)
            {
                instruction.text = releaseNeedsLookAway ? "先看一眼旁边，再看「放开小球」，等圆环填满。" :
                    "想放手时，看向下方的「放开小球」，等圆环填满。";
                english.text = releaseNeedsLookAway ? "Look to the side, then at Let go of the ball." :
                    "When you're ready, look at Let go of the ball.";
            }
            else if (Accessible && !practice.Holding)
            {
                instruction.text = "看向面前的蓝色小球，等圆环填满就能拿起。\n也可以用手柄握把拿球。";
                english.text = "Look at the blue ball until the ring fills, or pick it up with your controller.";
            }
            else
            {
                instruction.text = practice.Holding ? "拿稳啦！现在松开手柄侧面的握把，\n放开小球就完成这一步。" :
                    "用手柄射线指向蓝色小球，\n按住侧面握把拿起，再松开握把。";
                english.text = practice.Holding ? "You've got it! Release the side grip to let go of the ball." :
                    "Point your controller at the blue ball. Hold the side grip to pick it up, then release.";
            }
            reassurance.text = practice.Holding ? "准备好了再放手。\nYou can hold it for as long as you like." :
                "没拿到也没关系，可以再试一次。\nNo rush — you can always try again.";
        }
        else
        {
            heading.text = "太好了，你准备好探索啦！";
            instruction.text = "你已经学会了选择、拿起和放下。\n向右转，看看旅程简介；准备好再开始探索吧。";
            english.text = "Well done! Turn right to read the introduction. Choose Let's explore whenever you're ready.";
            reassurance.text = "随时可以休息，或请工作人员帮忙。\nYou can take a break or ask for help at any time.";
        }
        // Keep one button identity: dwelling on release cannot submit the subsequent journey confirmation.
        actionCaption.text = GazeHolding ? "放开小球\nLet go of the ball" :
            "我准备好了，开始探索\nI'm ready — let's explore";
    }
    void SelectTarget(int target)
    {
        if (!Running || submitted || pendingTarget >= 0 || practice.Step != target ||
            target > 1 || Time.frameCount == lastTransitionFrame) return;
        // Commit outside input dispatch so a mouse/controller event cannot cascade
        // through a newly revealed button in the same frame.
        pendingTarget = target;
        left.interactable = right.interactable = false;
    }
    void Act()
    {
        if (!Running || submitted || lastTransitionFrame == Time.frameCount) return;
        if (GazeHolding)
        {
            if (!Accessible || releaseNeedsLookAway || holdingSession != session ||
                holdingRevision != StageCoordinator.Current.Controller.Session.Revision) return;
            if (practice.Release(false, BriefingPractice.Input.HeadGaze))
            {
                holdingSession = Guid.Empty;
                Succeeded();
            }
            return;
        }
        if (practice.Ready)
        {
            submitted = true;
            SessionTelemetry.Report("TutorialCompleted", this);
            StageCoordinator.Current.QueueInteraction(session, ExperienceStage.Briefing, VisitorAction.ExploreHelmet);
            return;
        }
    }
    void SpawnBall()
    {
        if (!Camera.main || !Running || practice.Step != 2 || ball) return;
        ball = GameObject.CreatePrimitive(PrimitiveType.Sphere); ball.name = "Tutorial practice ball";
        ball.transform.localScale = Vector3.one*.18f;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader)
        {
            ballMaterial = new Material(shader); ballMaterial.color = new Color(.1f,.7f,.95f);
            ball.GetComponent<Renderer>().sharedMaterial = ballMaterial;
        }
        ballBody = ball.AddComponent<Rigidbody>(); ballBody.useGravity = false; ballBody.isKinematic = true;
        grab = ball.AddComponent<XRGrabInteractable>(); grab.throwOnDetach = false;
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.selectEntered.AddListener(Grabbed); grab.selectExited.AddListener(Released);
        var targetBall = ball;
        var attempt = session;
        ball.AddComponent<GazeWorldTarget>().Bind(() => CanPickUpByGaze(targetBall, attempt),
            () => PickUpByGaze(targetBall, attempt));
        Reposition();
    }
    bool CanPickUpByGaze(GameObject targetBall, Guid attempt) =>
        Running && Accessible && !submitted && session == attempt && targetBall && targetBall == ball &&
        practice.Step == 2 && !practice.Holding && grab && grab.isActiveAndEnabled && !grab.isSelected;

    void PickUpByGaze(GameObject targetBall, Guid attempt)
    {
        if (!CanPickUpByGaze(targetBall, attempt) || !Camera.main || !practice.Grab(BriefingPractice.Input.HeadGaze)) return;
        holdingSession = attempt;
        holdingRevision = StageCoordinator.Current.Controller.Session.Revision;
        positionBeforeGazeGrab = ballBody.position;
        releaseNeedsLookAway = LookingAtAction();
        grab.enabled = false;
        ballBody.detectCollisions = false;
        FollowViewer();
        SessionTelemetry.Report("TutorialGrabbed", this, "Head gaze");
    }

    bool LookingAtAction()
    {
        var camera = Camera.main;
        if (!camera) return false;
        var rect = (RectTransform)action.transform;
        var ray = new Ray(camera.transform.position, camera.transform.forward);
        return new Plane(rect.forward, rect.position).Raycast(ray, out float distance) &&
            rect.rect.Contains(rect.InverseTransformPoint(ray.GetPoint(distance)));
    }

    void FollowViewer()
    {
        ballBody.position = Camera.main.transform.TransformPoint(new Vector3(.30f, -.24f, .55f));
    }

    void CancelHold()
    {
        if (GazeHolding && ballBody)
        {
            ballBody.position = positionBeforeGazeGrab;
            ballBody.detectCollisions = true;
        }
        practice.CancelHold();
        releaseNeedsLookAway = false;
        holdingController = null;
        holdingSession = Guid.Empty;
        if (grab.isSelected && grab.interactionManager)
            grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
    }

    void Reposition()
    {
        if (!Running || practice.Holding || !ball || !Camera.main) return;
        var forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
        ballBody.position = Camera.main.transform.position + forward*.65f - Vector3.up*.25f;
    }
    void Grabbed(SelectEnterEventArgs args)
    {
        var input = args.interactorObject as XRBaseInputInteractor;
        // Only the controller that physically holds the ball can complete the grip exercise.
        if (!Running || !input || input is XRGazeInteractor || !input.selectInput.ReadIsPerformed()) return;
        if (!practice.Grab()) return;
        holdingController = input; holdingSession = session;
        holdingRevision = StageCoordinator.Current.Controller.Session.Revision;
        SessionTelemetry.Report("TutorialGrabbed", this, input.name);
    }
    void Released(SelectExitEventArgs args)
    {
        if (!ReferenceEquals(args.interactorObject, holdingController) || holdingSession != session) return;
        bool cancelled = args.isCanceled || !Running || !holdingController ||
            holdingRevision != StageCoordinator.Current.Controller.Session.Revision || holdingController.selectInput.ReadIsPerformed();
        holdingController = null; holdingSession = Guid.Empty;
        if (practice.Release(cancelled)) Succeeded();
    }
    void Succeeded()
    {
        lastTransitionFrame = Time.frameCount;
        StageCoordinator.Current.Audio.PlayFeedback(true);
        SessionTelemetry.Report("TutorialPracticeSucceeded", this);
        // Deactivate on the following Update, outside XRI selection callbacks.
    }
    void ClearBall()
    {
        if (grab)
        {
            grab.selectEntered.RemoveListener(Grabbed); grab.selectExited.RemoveListener(Released);
            if (grab.isSelected && grab.interactionManager)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        }
        if (ball) { ball.SetActive(false); Destroy(ball); }
        if (ballMaterial) Destroy(ballMaterial);
        ball = null; grab = null; ballBody = null; ballMaterial = null;
        holdingController = null; holdingSession = Guid.Empty; releaseNeedsLookAway = false;
        practice.CancelHold();
    }
    AccessibleButton Button(string caption, float x, UnityEngine.Events.UnityAction callback)
    {
        var image = Element<Image>(caption, panel.transform, new Vector2(x,-140), new Vector2(460,110));
        image.color = new Color(.08f,.36f,.4f); image.raycastTarget = true;
        image.sprite = rounded; image.type = Image.Type.Sliced;
        var button = image.gameObject.AddComponent<TutorialButton>(); button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = button.colors;
        colors.normalColor = Color.white; colors.highlightedColor = new Color(1.25f,1.4f,1.4f);
        colors.pressedColor = new Color(.7f,1f,.85f); colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(.5f,.55f,.6f,.7f); colors.fadeDuration = .15f; button.colors = colors;
        button.onClick.AddListener(callback);
        var label = Element<Text>("Label", image.transform, Vector2.zero, new Vector2(430,100));
        label.font = font; label.fontSize = 27; label.color = Color.white; label.alignment = TextAnchor.MiddleCenter; label.text = caption;
        return button;
    }
    T Element<T>(string label, Transform parent, Vector2 position, Vector2 size) where T : Graphic
    {
        var item = new GameObject(label, typeof(RectTransform)); item.transform.SetParent(parent,false);
        var rect = item.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
        var graphic = item.AddComponent<T>(); graphic.raycastTarget = false; return graphic;
    }
    Text Label(string title, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var label = Element<Text>(title, panel.transform, position, size);
        label.font = font; label.fontSize = fontSize; label.alignment = TextAnchor.MiddleCenter;
        label.color = color; label.supportRichText = false; return label;
    }
    void PlacePanel()
    {
        if (!Camera.main || practice.Holding) return;
        var head = Camera.main.transform;
        var origin = head.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
        float floorY = origin ? origin.transform.position.y : head.position.y - 1.6f;
        var forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
        // First placement matches the welcome/reset orientation even if the visitor
        // looks around before staff press Start. Placement stays fixed for this tutorial.
        var coordinator = StageCoordinator.Current;
        if (!placed && coordinator && coordinator.navigation)
            forward = coordinator.navigation.BriefingTutorialForward(Camera.main.transform.position);
        // Stay in front of the original board if the visitor started close to it.
        var board = coordinator && coordinator.navigation ? coordinator.navigation.start : null;
        float boardDistance = board ? Vector3.Dot(board.position - Camera.main.transform.position, forward) : 0;
        float distance = boardDistance > .5f ? Mathf.Min(1.8f, Mathf.Max(.65f, boardDistance-.25f)) : 1.8f;
        panel.transform.localScale = Vector3.one * (.0013f * distance / 1.8f);
        var position = head.position + forward*distance - Vector3.up*(.12f*distance/1.8f);
        // The card is world-anchored: use standing height while the simulator is
        // still starting, so it does not stay low after the eye-height correction.
        if (origin && head.position.y - floorY < .5f)
            position.y = floorY + 1.72f - .12f*distance/1.8f;
        float halfHeight = panel.GetComponent<RectTransform>().rect.height * panel.transform.localScale.y * .5f;
        position.y = Mathf.Max(position.y, floorY + halfHeight + .15f);
        panel.transform.SetPositionAndRotation(position,
            Quaternion.LookRotation(forward,Vector3.up));
        placed = true;
    }
    void BuildRoundedSprite()
    {
        const int size=64;
        roundedTexture = new Texture2D(size,size,TextureFormat.RGBA32,false) { hideFlags=HideFlags.HideAndDontSave, wrapMode=TextureWrapMode.Clamp };
        for(int y=0;y<size;y++) for(int x=0;x<size;x++)
        {
            float dx=Mathf.Max(12-x,0,x-51), dy=Mathf.Max(12-y,0,y-51);
            roundedTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(12.5f-Mathf.Sqrt(dx*dx+dy*dy))));
        }
        roundedTexture.Apply();
        rounded = Sprite.Create(roundedTexture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(14,14,14,14));
    }
    void OnDisable() { ClearBall(); wasRunning = false; if(panel) panel.SetActive(false); }
    void OnDestroy()
    {
        if(panel) Destroy(panel);
        if(rounded) Destroy(rounded);
        if(roundedTexture) Destroy(roundedTexture);
    }
}
