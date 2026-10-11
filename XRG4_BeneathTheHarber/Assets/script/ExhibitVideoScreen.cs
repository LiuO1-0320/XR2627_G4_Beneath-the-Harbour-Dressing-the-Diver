using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>A video-player placeholder fitted to the existing vedio screen blocks.</summary>
[ExecuteAlways]
public class ExhibitVideoScreen : MonoBehaviour
{
    public string screenTitle = "展览影像 / EXHIBIT FILM";
    public VideoClip videoClip;
    [Tooltip("Optional video URL; a supplied clip takes priority.")] public string videoUrl;
    public bool loop = true;
    [Range(0, 1)] public float volume = .65f;
    GameObject generated;
    Font font;
    VideoPlayer player;
    AudioSource audioSource;
    RenderTexture texture;
    readonly List<Text> statuses = new List<Text>(), timers = new List<Text>(), playLabels = new List<Text>();
    readonly List<Slider> sliders = new List<Slider>();
    readonly List<GameObject> overlays = new List<GameObject>();
    bool pendingPlay;
    VideoClip loadedClip;
    string loadedUrl;

    void OnEnable() { Build(); }
    void Update()
    {
        if (!generated) Build();
        if (!Application.isPlaying) return;
        if (!player) SetupPlayer();
        if (videoClip != loadedClip || videoUrl != loadedUrl) ConfigureSource();
        player.isLooping = loop; audioSource.volume = volume;
        foreach (var picture in generated.GetComponentsInChildren<RawImage>()) picture.color = player.isPrepared ? Color.white : Color.black;
        foreach (var overlay in overlays) overlay.SetActive(!player.isPrepared);
        foreach (var label in playLabels) label.text = player.isPlaying ? "Ⅱ 暂停 / Pause" : "▶ 播放 / Play";
        foreach (var label in timers) label.text = Format(player.time) + " / " + Format(player.isPrepared ? player.length : 0);
        foreach (var slider in sliders)
        {
            slider.interactable = player.isPrepared && player.canSetTime;
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
        foreach (var picture in generated.GetComponentsInChildren<RawImage>()) { picture.texture = texture; picture.color = Color.white; }
        ConfigureSource();
    }
    void ConfigureSource()
    {
        player.Stop(); pendingPlay = false; loadedClip = videoClip; loadedUrl = videoUrl;
        if (videoClip) { player.source = VideoSource.VideoClip; player.clip = videoClip; }
        else if (!string.IsNullOrWhiteSpace(videoUrl)) { player.source = VideoSource.Url; player.url = videoUrl; }
        else { SetStatus("视频占位 · 待添加影片\nVIDEO PLACEHOLDER"); return; }
        SetStatus("视频加载中… / Loading video…"); player.Prepare();
    }
    public void TogglePlayback()
    {
        if (!Application.isPlaying) return;
        if (!player) SetupPlayer();
        if (!videoClip && string.IsNullOrWhiteSpace(videoUrl))
        { SetStatus("暂未添加视频\nNO VIDEO ADDED YET"); return; }
        if (!player.isPrepared) { pendingPlay = !pendingPlay; return; }
        if (player.isPlaying) player.Pause(); else player.Play();
    }
    void Prepared(VideoPlayer source) { if (pendingPlay) { pendingPlay = false; source.Play(); } }
    void VideoError(VideoPlayer source, string message)
    { pendingPlay = false; SetStatus("视频加载失败，请检查影片\nVIDEO COULD NOT BE LOADED"); Debug.LogWarning(name + ": " + message, this); }
    void Seek(float value) { if (player && player.isPrepared && player.canSetTime) player.time = value * player.length; }
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
        if (player) { player.prepareCompleted -= Prepared; player.errorReceived -= VideoError; player.Stop(); }
        if (texture) texture.Release(); Dispose(texture); Dispose(generated); Dispose(font);
        generated = null; player = null; audioSource = null; texture = null;
        statuses.Clear(); timers.Clear(); playLabels.Clear(); sliders.Clear(); overlays.Clear(); pendingPlay = false;
    }
    static void Dispose(Object item)
    { if (!item) return; if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
}
