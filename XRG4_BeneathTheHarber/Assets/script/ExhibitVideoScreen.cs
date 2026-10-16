using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>A video-player placeholder fitted to the existing vedio screen blocks.</summary>
[ExecuteAlways]
public class ExhibitVideoScreen : MonoBehaviour, IResettable
{
    public int ResetOrder => 40;
    public void PrepareReset() { ResetSessionPlayback(); }
    public void RestoreReset() { ResetSessionPlayback(); sessionPaused=true; }

    public string screenTitle = "展览影像 / EXHIBIT FILM";
    public VideoClip videoClip;
    [Tooltip("Optional video URL; a supplied clip takes priority.")] public string videoUrl;
    public bool loop = true;
    [Range(0, 1)] public float volume = .65f;
    [Tooltip("Pause when the visitor's head is farther than this distance from the screen (metres).")]
    [Min(1)] public float viewingDistance = 4f;
    [Tooltip("Optional centre of the viewing area; defaults to this screen.")]
    public Transform viewingAnchor;
    double savedTime;
    bool restoringPosition;
    bool InViewingRange => Camera.main && Vector3.Distance(Camera.main.transform.position,
        viewingAnchor ? viewingAnchor.position : transform.position) <= Mathf.Max(1, viewingDistance);
    GameObject generated;
    Font font;
    VideoPlayer player;
    AudioSource audioSource;
    RenderTexture texture;
    readonly List<Text> statuses = new List<Text>(), timers = new List<Text>(), playLabels = new List<Text>();
    readonly List<Slider> sliders = new List<Slider>();
    readonly List<GameObject> overlays = new List<GameObject>();
    readonly List<Button> playButtons = new List<Button>();
    bool preparing, loadFailed;
    float prepareElapsed;
    bool HasSource => videoClip || !string.IsNullOrWhiteSpace(videoUrl);
    bool CanPlay => !sessionPaused && (!StageCoordinator.Current ||
        StageCoordinator.Current.Controller.Session.Status == SessionStatus.Running);
    bool pendingPlay;
    bool sessionPaused, resumePlayback;
    public void SetSessionPaused(bool paused)
    {
        if (sessionPaused == paused) return;
        sessionPaused = paused;
        if (!player) return;
        if (paused) { resumePlayback = player.isPlaying; if (player.isPlaying) player.Pause(); }
        else if (InViewingRange && !restoringPosition && (resumePlayback || (pendingPlay && player.isPrepared)))
        { resumePlayback = pendingPlay = false; player.Play(); }
        else if (!InViewingRange) PausePreservingProgress();
    }
    void SavePosition()
    {
        if (player && player.isPrepared && !restoringPosition && player.time >= 0)
            savedTime = player.time;
    }
    public void PausePreservingProgress()
    {
        SavePosition();
        pendingPlay = resumePlayback = false;
        if (player && player.isPlaying) player.Pause();
    }
    public void ResetSessionPlayback()
    {
        pendingPlay = resumePlayback = false;
        if (player) player.Stop();
        savedTime = 0; restoringPosition = false;
        preparing = loadFailed = false; prepareElapsed = 0;
        SetStatus(HasSource ? "点击播放观看影片\nPRESS PLAY TO WATCH" : "暂未添加视频\nNO VIDEO ADDED YET");
    }
    VideoClip loadedClip;
    string loadedUrl;

    void OnEnable() { Build(); }
    void Update()
    {
        if (!generated) Build();
        if (!Application.isPlaying) return;
        if (!player) SetupPlayer();
        if (videoClip != loadedClip || videoUrl != loadedUrl) ConfigureSource();
        if (!InViewingRange) PausePreservingProgress();
        if (preparing && !sessionPaused)
        {
            prepareElapsed += Time.unscaledDeltaTime;
            if (prepareElapsed > 30)
            {
                player.Stop();
                VideoError(player, "Preparation timed out after 30 seconds.");
            }
        }
        player.isLooping = loop;
        var director = StageCoordinator.Current ? StageCoordinator.Current.Audio : null;
        audioSource.volume = volume * (director && director.NarrationPlaying ? 0.25f : 1f);
        foreach (var picture in generated.GetComponentsInChildren<RawImage>()) picture.color = player.isPrepared ? Color.white : Color.black;
        foreach (var overlay in overlays) overlay.SetActive(!player.isPrepared);
        foreach (var button in playButtons) button.interactable = CanPlay && HasSource && InViewingRange;
        foreach (var label in playLabels) label.text = !CanPlay ? "等待开始或继续" : player.isPlaying ? "Ⅱ 暂停 / Pause" :
            !InViewingRange ? "靠近观看 / Come closer" : loadFailed ? "重试 / Retry" : pendingPlay ? "取消 / Cancel" : "▶ 播放 / Play";
        foreach (var label in timers) label.text = Format(player.isPrepared && !restoringPosition ? player.time : savedTime) + " / " + Format(player.isPrepared ? player.length : 0);
        foreach (var slider in sliders)
        {
            slider.interactable = CanPlay && InViewingRange && player.isPrepared && player.canSetTime;
            slider.SetValueWithoutNotify(player.isPrepared && player.length > 0 ? (float)(player.time / player.length) : 0);
        }
    }
    void Build()
    {
        if (generated) return;
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 40);
        generated = new GameObject("Video player display (generated)") { hideFlags = HideFlags.HideAndDontSave };
        generated.transform.SetParent(transform, false);
        // Both faces are readable, so the existing cube's orientation need not be changed.
        Face(false); Face(true);
    }
    void Face(bool back)
    {
        var canvas = new GameObject(back ? "Rear video display" : "Front video display",
            typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
        canvas.hideFlags = HideFlags.HideAndDontSave; canvas.transform.SetParent(generated.transform, false);
        canvas.transform.localPosition = new Vector3(0, 0, back ? .506f : -.506f);
        canvas.transform.localRotation = back ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        canvas.transform.localScale = new Vector3(.001f, 1f / 630, 1);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        // Mouse input uses the ordinary raycaster; tracked XR rays use the existing one.
        canvas.AddComponent<GraphicRaycaster>();
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 630);
        Graphic<Image>(canvas.transform, "Screen surround", Vector2.zero, new Vector2(975, 610)).color = new Color(.018f, .028f, .034f);
        Label(canvas.transform, screenTitle, new Vector2(0, 276), new Vector2(920, 42), 27);
        float desiredHeight = 900 * Mathf.Abs(transform.lossyScale.x / transform.lossyScale.y) * .63f / (16f / 9);
        float fit = Mathf.Min(1, 480 / Mathf.Max(1, desiredHeight));
        var picture = Graphic<RawImage>(canvas.transform, "Video picture", new Vector2(0, 12), new Vector2(900 * fit, desiredHeight * fit));
        picture.color = Color.black;
        var overlay = new GameObject("No video placeholder", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        overlay.transform.SetParent(canvas.transform, false); overlay.GetComponent<RectTransform>().sizeDelta = new Vector2(850, 380);
        overlays.Add(overlay);
        Label(overlay.transform, "▶", new Vector2(0, 68), new Vector2(180, 140), 100);
        statuses.Add(Label(overlay.transform, "视频占位 · 待添加影片\nVIDEO PLACEHOLDER", new Vector2(0, -52), new Vector2(800, 100), 34));
        var play = Graphic<Image>(canvas.transform, "Play pause button", new Vector2(-315, -260), new Vector2(260, 60));
        play.color = new Color(.08f, .27f, .3f); play.raycastTarget = true;
        var button = play.gameObject.AddComponent<QuickTravelPressButton>(); button.targetGraphic = play;
        playButtons.Add(button);
        button.onClick.AddListener(TogglePlayback);
        playLabels.Add(Label(play.transform, "▶ 播放 / Play", Vector2.zero, new Vector2(250, 55), 27));
        var track = Graphic<Image>(canvas.transform, "Progress track", new Vector2(10, -260), new Vector2(330, 14));
        track.color = new Color(.2f, .26f, .28f); track.raycastTarget = true;
        var fill = Graphic<Image>(track.transform, "Progress fill", Vector2.zero, new Vector2(330, 14));
        fill.rectTransform.sizeDelta = Vector2.zero;
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
        fill.color = new Color(.43f, .78f, .73f);
        var slider = track.gameObject.AddComponent<Slider>(); slider.fillRect = fill.rectTransform;
        slider.minValue = 0; slider.maxValue = 1;
        slider.onValueChanged.AddListener(Seek); sliders.Add(slider);
        timers.Add(Label(canvas.transform, "00:00 / 00:00", new Vector2(320, -260), new Vector2(270, 55), 26));
    }
    void SetupPlayer()
    {
        player = generated.AddComponent<VideoPlayer>(); player.playOnAwake = false;
        player.renderMode = VideoRenderMode.RenderTexture; player.aspectRatio = VideoAspectRatio.FitInside;
        texture = new RenderTexture(1280, 720, 0) { name = name + " video", hideFlags = HideFlags.HideAndDontSave };
        texture.Create(); player.targetTexture = texture;
        audioSource = generated.AddComponent<AudioSource>(); audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1; audioSource.minDistance = 2; audioSource.maxDistance = 15;
        player.audioOutputMode = VideoAudioOutputMode.AudioSource; player.controlledAudioTrackCount = 1;
        player.SetTargetAudioSource(0, audioSource);
        player.prepareCompleted += Prepared; player.errorReceived += VideoError;
        player.seekCompleted += SeekCompleted;
        foreach (var picture in generated.GetComponentsInChildren<RawImage>()) { picture.texture = texture; picture.color = Color.white; }
        ConfigureSource();
    }
    void ConfigureSource()
    {
        if (loadedClip != videoClip || loadedUrl != videoUrl) savedTime = 0;
        player.Stop(); restoringPosition = false;
        pendingPlay = preparing = loadFailed = false; loadedClip = videoClip; loadedUrl = videoUrl;
        if (videoClip) { player.source = VideoSource.VideoClip; player.clip = videoClip; }
        else if (!string.IsNullOrWhiteSpace(videoUrl)) { player.source = VideoSource.Url; player.url = videoUrl; }
        else { SetStatus("视频占位 · 待添加影片\nVIDEO PLACEHOLDER"); return; }
        SetStatus("点击播放观看影片\nPRESS PLAY TO WATCH");
    }
    void BeginPrepare()
    {
        preparing = true; loadFailed = false; prepareElapsed = 0;
        SetStatus("视频加载中…可点击取消\nLOADING VIDEO — CANCEL AVAILABLE");
        player.Prepare();
    }
    public void TogglePlayback()
    {
        if (!Application.isPlaying || !CanPlay || !InViewingRange || restoringPosition) return;
        if (!player) SetupPlayer();
        if (videoClip != loadedClip || videoUrl != loadedUrl) ConfigureSource();
        if (!videoClip && string.IsNullOrWhiteSpace(videoUrl))
        { SetStatus("暂未添加视频\nNO VIDEO ADDED YET"); return; }
        if (!player.isPrepared)
        {
            if (preparing && pendingPlay) { pendingPlay = false; return; }
            pendingPlay = true;
            if (!preparing) BeginPrepare();
            return;
        }
        if (player.isPlaying) player.Pause(); else player.Play();
    }
    void Prepared(VideoPlayer source)
    {
        if (!preparing) return;
        preparing = false; loadFailed = false;
        if (savedTime > 0 && source.canSetTime)
        {
            restoringPosition = true;
            source.time = System.Math.Min(savedTime, System.Math.Max(0, source.length - .05));
            return;
        }
        if (pendingPlay && CanPlay && InViewingRange) { pendingPlay = false; source.Play(); }
    }
    void SeekCompleted(VideoPlayer source)
    {
        if (!restoringPosition) return;
        restoringPosition = false;
        if (pendingPlay && CanPlay && InViewingRange) { pendingPlay = false; source.Play(); }
    }
    void VideoError(VideoPlayer source, string message)
    { pendingPlay = preparing = restoringPosition = false; loadFailed = true; SetStatus("视频加载失败，请点击重试\nVIDEO COULD NOT LOAD — PRESS RETRY"); Debug.LogWarning(name + ": " + message, this); }
    void Seek(float value) { if (CanPlay && InViewingRange && player && player.isPrepared && player.canSetTime) { savedTime = value * player.length; player.time = savedTime; } }
    void SetStatus(string message) { foreach (var label in statuses) label.text = message; }
    static string Format(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) seconds = 0;
        int time = (int)seconds; return (time / 60).ToString("00") + ":" + (time % 60).ToString("00");
    }
    T Graphic<T>(Transform parent, string label, Vector2 p, Vector2 size) where T : Graphic
    {
        var item = new GameObject(label, typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        item.transform.SetParent(parent, false); var rect = item.GetComponent<RectTransform>();
        rect.sizeDelta = size; rect.anchoredPosition = p;
        var graphic = item.AddComponent<T>(); graphic.raycastTarget = false; return graphic;
    }
    Text Label(Transform parent, string words, Vector2 p, Vector2 size, int fontSize)
    {
        var text = Graphic<Text>(parent, "Player label", p, size); text.font = font; text.text = words;
        text.fontSize = fontSize; text.alignment = TextAnchor.MiddleCenter; text.color = new Color(.87f, .92f, .93f); return text;
    }
    void OnDisable()
    {
        SavePosition();
        if (player) player.seekCompleted -= SeekCompleted;
        if (player) { player.prepareCompleted -= Prepared; player.errorReceived -= VideoError; player.Stop(); }
        if (texture) texture.Release(); Dispose(texture); Dispose(generated); Dispose(font);
        generated = null; player = null; audioSource = null; texture = null;
        statuses.Clear(); timers.Clear(); playLabels.Clear(); playButtons.Clear(); sliders.Clear(); overlays.Clear();
        pendingPlay = preparing = loadFailed = resumePlayback = false;
    }
    static void Dispose(Object item)
    { if (!item) return; if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
}
