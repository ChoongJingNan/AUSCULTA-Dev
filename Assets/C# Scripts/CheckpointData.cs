using UnityEngine;
using UnityEngine.SceneManagement;
using BayatGames.SaveGameFree;
using System;
using System.Collections.Generic;
        #if UNITY_EDITOR
    using UnityEditor;

    public static class CheckpointDebugMenu
    {
        [MenuItem("Tools/Clear Checkpoint Save")]
        public static void Clear() => CheckpointData.DeleteSavedData();
    }
    #endif
public static class CheckpointData
{
    private const string IDS_KEY = "Saved_Triggered_Checkpoints";

    public static void SaveTriggeredIds(HashSet<string> ids)
    {
        SaveGame.Save(IDS_KEY, string.Join("|", ids));
    }

    public static HashSet<string> LoadTriggeredIds()
    {
        if (!SaveGame.Exists(IDS_KEY)) return new HashSet<string>();
        string raw = SaveGame.Load<string>(IDS_KEY, "");
        return new HashSet<string>(raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
    }
    private const string SCENE_KEY = "Saved_Scene_Name";
    private const string POS_KEY = "Saved_Player_Position";
    private const string ROT_KEY = "Saved_Player_Rotation";
    private const string INDEX_KEY = "Saved_Checkpoint_Index";
    private const string TIME_KEY = "Saved_Session_Time";

    public static bool hasCheckpoint => SaveGame.Exists(POS_KEY) && SaveGame.Exists(SCENE_KEY);

    public static void SaveCheckpoint(Vector3 position, Quaternion rotation, string sceneName, int checkpointIndex)
    {
        if (sceneName.Equals("Main_Menu", StringComparison.OrdinalIgnoreCase)) return;

        SaveGame.Save(POS_KEY, position);
        SaveGame.Save(ROT_KEY, rotation);
        SaveGame.Save(SCENE_KEY, sceneName);
        SaveGame.Save(INDEX_KEY, checkpointIndex);
        SaveGame.Save(TIME_KEY, ModernCameraHUD.SessionTime);

        // Force sync to PlayerPrefs so EncounterManager grabs the correct stage index instantly
        PlayerPrefs.SetInt("SavedCheckpointIndex", checkpointIndex);
        PlayerPrefs.Save();

        Debug.Log($"<color=green>[CheckpointData] Saved Checkpoint #{checkpointIndex} at {position} in scene: {sceneName}</color>");
    }

    public static int GetSavedCheckpointIndex()
    {
        int index = SaveGame.Load<int>(INDEX_KEY, 1);
        // Ensure PlayerPrefs matches just in case
        PlayerPrefs.SetInt("SavedCheckpointIndex", index);
        return index;
    }

    public static string GetSavedSceneName()
    {
        if (!SaveGame.Exists(SCENE_KEY)) return null;

        string sceneName = SaveGame.Load<string>(SCENE_KEY, "");
        if (string.IsNullOrEmpty(sceneName) || sceneName.Equals("Main_Menu", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return sceneName;
    }

    public static void DeleteSavedData()
    {
        SaveGame.Delete(POS_KEY);
        SaveGame.Delete(ROT_KEY);
        SaveGame.Delete(SCENE_KEY);
        SaveGame.Delete(INDEX_KEY);
        
        // Also clear out PlayerPrefs progression keys
        PlayerPrefs.DeleteKey("SavedCheckpointIndex");
        PlayerPrefs.Save();
        SaveGame.Delete(IDS_KEY);
        SaveGame.Delete(TIME_KEY);
        LastMapTracker.Clear();

        Debug.Log("<color=yellow>[CheckpointData] Save data deleted.</color>");
    }

    public static bool TryLoadAndTeleportPlayer(GameObject player)
    {
        // If we just returned from the settings menu, do NOT load the checkpoint!
        if (MenuTransitionData.hasSavedPosition) return false;

        if (!hasCheckpoint || player == null) return false;

        string savedScene = GetSavedSceneName();
        if (savedScene == null || savedScene != SceneManager.GetActiveScene().name) return false;

        Vector3 savedPos = SaveGame.Load<Vector3>(POS_KEY);
        Quaternion savedRot = SaveGame.Load<Quaternion>(ROT_KEY);
        
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        player.transform.position = savedPos;
        player.transform.rotation = savedRot;

        if (controller != null) controller.enabled = true;

        return true;
    }
    public static void RestoreSavedTimer()
    {
        if (SaveGame.Exists(TIME_KEY))
            ModernCameraHUD.SessionTime = SaveGame.Load<float>(TIME_KEY, 0f);
    }
    public static bool TryGetSavedPose(out Vector3 pos, out Quaternion rot)
{
    pos = Vector3.zero; rot = Quaternion.identity;
    if (!hasCheckpoint) return false;
    pos = SaveGame.Load<Vector3>(POS_KEY);
    rot = SaveGame.Load<Quaternion>(ROT_KEY);
    return true;
}

    public static void ApplySlot(SaveSlotData d)
    {
        DeleteSavedData();
        SaveRoomData.ClearReturnPoint();

        // Clear leftover pause/settings transition flags so they can't block the teleport
        MenuTransitionData.hasSavedPosition = false;
        MenuTransitionData.openPauseMenuOnLoad = false;
        MenuTransitionData.openSettingsOnLoad = false;
        MenuTransitionData.previousSceneName = "";

        if (d.hasCheckpoint)
        {
            SaveGame.Save(POS_KEY, d.position);
            SaveGame.Save(ROT_KEY, d.rotation);
            SaveGame.Save(SCENE_KEY, d.sceneName);
            SaveGame.Save(INDEX_KEY, d.checkpointIndex);
            PlayerPrefs.SetInt("SavedCheckpointIndex", d.checkpointIndex);
            PlayerPrefs.Save();
            SaveTriggeredIds(new HashSet<string>(d.triggeredIds ?? new string[0]));
            SaveGame.Save(TIME_KEY, d.playTimeSeconds);
        }
        else
        {
            SaveRoomData.SaveReturnPoint(d.sceneName, d.position, d.rotation);
        }
    }
}