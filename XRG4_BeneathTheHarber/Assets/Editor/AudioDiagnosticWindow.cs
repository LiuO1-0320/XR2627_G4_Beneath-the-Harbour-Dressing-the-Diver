using UnityEditor;
using UnityEngine;

public sealed class AudioDiagnosticWindow : EditorWindow
{
    static readonly string[] Labels = {
        "正常播放", "禁止环境循环音", "禁止旁白与视频声音", "禁止全部播放（底层音频系统保留）", "仅禁止旁白（视频保留）", "仅禁止视频声音（旁白保留）"
    };
    [MenuItem("Tools/Experience/Audio/Editor audio test panel")]
    public static void ShowPanel()
    {
        var window = GetWindow<AudioDiagnosticWindow>("音频排查");
        window.minSize = new Vector2(460, 330); window.Show();
    }
    void OnInspectorUpdate() { Repaint(); }
    void OnGUI()
    {
        EditorGUILayout.LabelField("编辑器 Play 模式音频隔离", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("退出 Play 后选择模式，再进入 Play。开关阻止播放，不是音量设为零。仅当前编辑器会话生效，不写入场景或项目设置，也不影响构建菜单。", MessageType.Info);
        bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
        int selected = Mathf.Clamp(UnityEditor.SessionState.GetInt(AudioDiagnostics.EditorModeKey, 0), 0, Labels.Length - 1);
        using (new EditorGUI.DisabledScope(playing))
        {
            int next = EditorGUILayout.Popup("下次 Play 模式", selected, Labels);
            if (next != selected) UnityEditor.SessionState.SetInt(AudioDiagnostics.EditorModeKey, next);
            if (GUILayout.Button("恢复正常播放模式")) UnityEditor.SessionState.SetInt(AudioDiagnostics.EditorModeKey, 0);
        }
        if (playing) EditorGUILayout.LabelField("本轮生效模式：", AudioDiagnostics.Mode);
        else EditorGUILayout.LabelField("当前选择：", Labels[Mathf.Clamp(UnityEditor.SessionState.GetInt(AudioDiagnostics.EditorModeKey, 0), 0, Labels.Length - 1)]);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("禁止全部播放：跳过项目音源创建和播放，关闭视频音轨；引擎仍可初始化音频设备。它不等同于 AUDIO DISABLED 独立诊断包。", MessageType.Warning);
        EditorGUILayout.LabelField("环境音：博物馆音乐、港口及水下循环音", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("旁白与视频：含头盔内部旁白；保留 NPC 文字、动作和任务交互。分类隔离仍保留反馈音。", EditorStyles.wordWrappedLabel);
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset");
        if (assets.Length > 0)
        {
            var audio = new SerializedObject(assets[0]);
            var disabled = audio.FindProperty("m_DisableAudio");
            if (disabled != null && disabled.boolValue)
                EditorGUILayout.HelpBox("项目 Disable Unity Audio 当前已勾选。请记录该设置；本面板不会修改它，其作用也不能代替独立包验证。", MessageType.Warning);
        }
    }
}
