public static class MenuTransitionData
{
    public static bool openSettingsOnLoad = false;
    public static string previousSceneName = "";
    
    // New fields to preserve state when returning to gameplay
    public static bool openPauseMenuOnLoad = false;

    // True for the first frame of a scene that was just reloaded after visiting Settings from the pause menu.
    // Scene-start black fades (FaceTransition) check this and skip themselves.
    public static bool skipSceneFadeIn = false;
    public static UnityEngine.Vector3 savedPlayerPosition;
    public static bool hasSavedPosition = false;
}