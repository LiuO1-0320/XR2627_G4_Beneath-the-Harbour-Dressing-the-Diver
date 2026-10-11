using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(HarbourSceneDressing))]
public class HarbourSceneryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("展开 Static Scenery (editable)，选择提示板、展墙、工作台等对象，直接调整 Transform。保存场景后会保留调整。运行时才进行静态合批。", MessageType.Info);
        var dressing = (HarbourSceneDressing)target;
        if (GUILayout.Button("选中可编辑布景"))
        {
            dressing.EnsureBuilt();
            Selection.activeTransform = dressing.transform.Find("Static Scenery (editable)");
        }
        if (GUILayout.Button("按头盔前部视窗重新对齐提示板"))
        { dressing.AlignStationBoards(); dressing.AlignExhibitBoards(); }
    }

    // Headless preparation and save/reload checks. Also available from the editor menu.
    public static void PrepareBasicScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/BasicScene.unity");
        var dressing = Object.FindObjectOfType<HarbourSceneDressing>();
        if (!dressing) throw new System.InvalidOperationException("Scene dressing component missing");
        dressing.EnsureBuilt();
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene)) throw new System.InvalidOperationException("Scene save failed");
        scene = EditorSceneManager.OpenScene("Assets/Scenes/BasicScene.unity");
        dressing = Object.FindObjectOfType<HarbourSceneDressing>();
        var root = dressing.transform.Find("Static Scenery (editable)");
        if (!root || root.childCount < 20) throw new System.InvalidOperationException("Editable scenery missing after reload");
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
            if (!renderer.enabled || !EditorUtility.IsPersistent(renderer.sharedMaterial))
                throw new System.InvalidOperationException("Unsaved or combined editable geometry: " + renderer.name);
        foreach (var line in root.GetComponentsInChildren<LineRenderer>())
            if (!EditorUtility.IsPersistent(line.sharedMaterial)) throw new System.InvalidOperationException("Unsaved hose material");
        CheckFacing(dressing.introductionBoard, dressing.helmet1);
        CheckFacing(dressing.helmetInstructionsBoard, dressing.helmet1);
        CheckFacing(dressing.detailsBoard, dressing.helmet2);
        CheckFacing(dressing.diverInstructionsBoard, dressing.helmet3);
        // A save/reload must keep manual edits rather than re-create the layout.
        var board = root.Find("01 提示板 · Exhibit information");
        Vector3 original = board.localPosition, test = original + Vector3.right * .123f;
        board.localPosition = test; EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        scene = EditorSceneManager.OpenScene("Assets/Scenes/BasicScene.unity");
        dressing = Object.FindObjectOfType<HarbourSceneDressing>(); dressing.EnsureBuilt();
        board = dressing.transform.Find("Static Scenery (editable)").Find("01 提示板 · Exhibit information");
        if (Vector3.Distance(board.localPosition, test) > .0001f) throw new System.InvalidOperationException("Manual position was reset");
        board.localPosition = original; EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("HARBOUR_SCENERY_OK: independent geometry, saved materials, helmet-facing panels, manual edits survive reload.");
    }
    static void CheckFacing(Transform board, Transform helmet)
    {
        if (!board || !helmet || Vector3.Dot(-board.forward, HarbourSceneDressing.HelmetFront(helmet)) < .999f)
            throw new System.InvalidOperationException("Board is not facing the helmet front");
    }
}
