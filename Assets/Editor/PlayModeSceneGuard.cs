#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayModeSceneGuard
{
    private const string MainScenePath = "Assets/Scenes/SampleScene.unity";

    static PlayModeSceneGuard()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChangedInEditMode;
        EditorApplication.delayCall += EnsureMainSceneOpenIfUnityBackupIsActive;
    }

    [MenuItem("Tools/Lo-Fi Delivery/Open Main Scene")]
    public static void OpenMainScene()
    {
        EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
        {
            return;
        }

        if (!IsUnityBackupScene(SceneManager.GetActiveScene()))
        {
            return;
        }

        Debug.Log("PlayModeSceneGuard: Play was requested from a Unity backup scene. Reopening the main scene and retrying Play Mode.");
        EditorApplication.isPlaying = false;
        OpenMainScene();
        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }

    private static void OnActiveSceneChangedInEditMode(Scene previousScene, Scene newScene)
    {
        if (IsUnityBackupScene(newScene))
        {
            EditorApplication.delayCall += EnsureMainSceneOpenIfUnityBackupIsActive;
        }
    }

    private static void EnsureMainSceneOpenIfUnityBackupIsActive()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!IsUnityBackupScene(activeScene))
        {
            return;
        }

        Debug.Log($"PlayModeSceneGuard: active scene was Unity backup scene '{FormatScene(activeScene)}'. Opening '{MainScenePath}'.");
        OpenMainScene();
    }

    private static bool IsUnityBackupScene(Scene scene)
    {
        string path = scene.path?.Replace('\\', '/') ?? string.Empty;
        string name = scene.name ?? string.Empty;
        return path.StartsWith("Temp/__Backupscenes/")
            || path.Contains("/__Backupscenes/")
            || name.EndsWith(".backup");
    }

    private static string FormatScene(Scene scene)
    {
        return string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
    }
}
#endif
