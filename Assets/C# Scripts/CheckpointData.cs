using UnityEngine;
using UnityEngine.SceneManagement;
using BayatGames.SaveGameFree;
using System;

public static class CheckpointData
{
    private const string SCENE_KEY = "Saved_Scene_Name";
    private const string POS_KEY = "Saved_Player_Position";
    private const string ROT_KEY = "Saved_Player_Rotation";
    private const string INDEX_KEY = "Saved_Checkpoint_Index";

    public static bool hasCheckpoint => SaveGame.Exists(POS_KEY) && SaveGame.Exists(SCENE_KEY);

    public static void SaveCheckpoint(Vector3 position, Quaternion rotation, string sceneName, int checkpointIndex)
    {
        if (sceneName.Equals("Main_Menu", StringComparison.OrdinalIgnoreCase)) return;

        SaveGame.Save(POS_KEY, position);
        SaveGame.Save(ROT_KEY, rotation);
        SaveGame.Save(SCENE_KEY, sceneName);
        SaveGame.Save(INDEX_KEY, checkpointIndex);

        Debug.Log($"<color=green>[CheckpointData] Saved Checkpoint #{checkpointIndex} at {position} in scene: {sceneName}</color>");
    }

    public static int GetSavedCheckpointIndex()
    {
        return SaveGame.Load<int>(INDEX_KEY, 1);
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
        Debug.Log("<color=yellow>[CheckpointData] Save data deleted.</color>");
    }

    public static bool TryLoadAndTeleportPlayer(GameObject player)
    {
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
}