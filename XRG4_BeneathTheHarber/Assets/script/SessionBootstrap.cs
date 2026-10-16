using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Existing exhibit scenes opt in through their PlayerQuickTravel component.
/// An authored coordinator takes precedence over these runtime defaults.</summary>
public static class SessionBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= Install;
        SceneManager.sceneLoaded += Install;
    }
    static void Install(Scene scene, LoadSceneMode mode)
    {
        if (Object.FindObjectOfType<StageCoordinator>()) return;
        var navigation = Object.FindObjectOfType<PlayerQuickTravel>();
        if (!navigation || navigation.gameObject.scene != scene) return;
        var host = new GameObject("Experience Session");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<SessionController>();
        host.AddComponent<StageCoordinator>().navigation = navigation;
    }
}
