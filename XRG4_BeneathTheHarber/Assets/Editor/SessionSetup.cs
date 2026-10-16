using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SessionSetup
{
    [MenuItem("Tools/Experience/Create editable session configuration")]
    static void Create()
    {
        var existing = Object.FindObjectOfType<StageCoordinator>();
        if (existing) { Selection.activeGameObject = existing.gameObject; return; }
        var host = new GameObject("Experience Session");
        Undo.RegisterCreatedObjectUndo(host, "Create Experience session");
        host.AddComponent<SessionController>();
        var coordinator = host.AddComponent<StageCoordinator>();
        coordinator.navigation = Object.FindObjectOfType<PlayerQuickTravel>();
        Selection.activeGameObject = host;
        EditorSceneManager.MarkSceneDirty(host.scene);
    }
}
