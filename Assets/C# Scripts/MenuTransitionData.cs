public static class MenuTransitionData
{
    public static bool openSettingsOnLoad = false;
    public static string previousSceneName = "";
    
    // New fields to preserve state when returning to gameplay
    public static bool openPauseMenuOnLoad = false;
    public static UnityEngine.Vector3 savedPlayerPosition;
    public static bool hasSavedPosition = false;
}