using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Remembers the last gameplay scene the player was in, so the main menu can show it as the backdrop.
/// Needs no scene setup: it hooks into scene loading by itself when the game starts.
/// </summary>
public static class LastMapTracker
{
    public const string PrefKey = "LastSavedScene"; // same key MenuBackgroundLoader already reads

    // Scenes that must never be recorded as "the last map". Add your menu scene's name if it differs.
    private static readonly string[] MenuScenes = { "Main_Menu", "MainMenu" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // The very first scene loaded before we subscribed (e.g. pressing Play inside a map in the editor)
        Record(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Additive loads are ignored, so the menu backdrop never overwrites the value
        if (mode != LoadSceneMode.Single) return;
        Record(scene);
    }

    private static void Record(Scene scene)
    {
        if (!scene.IsValid() || IsMenuScene(scene.name)) return;

        PlayerPrefs.SetString(PrefKey, scene.name);
        PlayerPrefs.Save();
    }

    public static bool IsMenuScene(string sceneName)
    {
        foreach (string menu in MenuScenes)
            if (string.Equals(menu, sceneName, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>Last gameplay scene, or the fallback if none is stored or it isn't in the build.</summary>
    public static string GetLastMap(string fallback)
    {
        string scene = PlayerPrefs.GetString(PrefKey, fallback);
        if (string.IsNullOrEmpty(scene) || IsMenuScene(scene) || !Application.CanStreamedLevelBeLoaded(scene))
            return fallback;
        return scene;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(PrefKey);
        PlayerPrefs.Save();
    }
}