using System;
using System.IO;
using UnityEngine;

/// <summary>
/// What gets written to disk for one save slot.
/// Stored as JSON at: <persistentDataPath>/Saves/Slot_N/save.json
/// </summary>
[Serializable]
public class SaveSlotData
{
    public int version = 1;
    public string sceneName;
    public bool hasCheckpoint;          // false = saved in the Save Room before reaching a checkpoint
    public int checkpointIndex;
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public string[] triggeredIds = new string[0];
    public float playTimeSeconds;
    public string savedAt;              // display only
}

/// <summary>
/// The 4 manual save slots. Each slot lives in its own folder on the PC.
/// Slot indices are 0-3 in code (shown as Slot 1-4 in the UI).
/// </summary>
public static class SaveSlotManager
{
    public const int SlotCount = 4;

    public static string RootFolder => Path.Combine(Application.persistentDataPath, "Saves");
    public static string SlotFolder(int slot) => Path.Combine(RootFolder, "Slot_" + (slot + 1));
    private static string SlotFile(int slot) => Path.Combine(SlotFolder(slot), "save.json");

    public static bool IsValidSlot(int slot) => slot >= 0 && slot < SlotCount;

    // ------------------------------------------------------------------
    // Reading
    // ------------------------------------------------------------------

    public static bool TryRead(int slot, out SaveSlotData data)
    {
        data = null;
        if (!IsValidSlot(slot)) return false;

        string path = SlotFile(slot);
        if (!File.Exists(path)) return false;

        try
        {
            data = JsonUtility.FromJson<SaveSlotData>(File.ReadAllText(path));
            return data != null && !string.IsNullOrEmpty(data.sceneName);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSlots] Slot {slot + 1} could not be read: {e.Message}");
            data = null;
            return false;
        }
    }

    public static bool AnySlotExists()
    {
        for (int i = 0; i < SlotCount; i++)
            if (TryRead(i, out _)) return true;
        return false;
    }

    // ------------------------------------------------------------------
    // Saving
    // ------------------------------------------------------------------

    /// <summary>Builds a snapshot of the player's current progress.</summary>
    public static bool TryCapture(out SaveSlotData data, out string error)
    {
        data = new SaveSlotData();
        error = null;

        bool inSaveRoom = SaveRoomData.HasReturnPoint();
        string returnScene = inSaveRoom ? SaveRoomData.GetReturnScene() : null;
        string checkpointScene = CheckpointData.hasCheckpoint ? CheckpointData.GetSavedSceneName() : null;

        if (!string.IsNullOrEmpty(checkpointScene)
            && (!inSaveRoom || checkpointScene == returnScene)
            && CheckpointData.TryGetSavedPose(out Vector3 cpPos, out Quaternion cpRot))
        {
            // Normal case: save the last checkpoint reached (same data "Continue" used before)
            data.sceneName = checkpointScene;
            data.hasCheckpoint = true;
            data.checkpointIndex = CheckpointData.GetSavedCheckpointIndex();
            data.position = cpPos;
            data.rotation = cpRot;

            var ids = CheckpointData.LoadTriggeredIds();
            data.triggeredIds = new string[ids.Count];
            ids.CopyTo(data.triggeredIds);
        }
        else if (inSaveRoom && !string.IsNullOrEmpty(returnScene))
        {
            // No checkpoint yet in this level: save the spot the player left the level from
            SaveRoomData.PeekReturnPoint(out Vector3 p, out Quaternion r);
            data.sceneName = returnScene;
            data.hasCheckpoint = false;
            data.checkpointIndex = 0;
            data.position = p;
            data.rotation = r;
        }
        else
        {
            error = "Nothing to save yet.";
            return false;
        }

        data.playTimeSeconds = ModernCameraHUD.SessionTime;
        data.savedAt = DateTime.Now.ToString("dd MMM yyyy  HH:mm");
        return true;
    }

    public static bool SaveToSlot(int slot, out string error)
    {
        error = null;
        if (!IsValidSlot(slot)) { error = "Invalid slot."; return false; }
        if (!TryCapture(out SaveSlotData data, out error)) return false;

        try
        {
            Directory.CreateDirectory(SlotFolder(slot));

            // Write to a temp file first so a crash mid-save can't corrupt an existing save
            string path = SlotFile(slot);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);

            Debug.Log($"<color=green>[SaveSlots] Saved slot {slot + 1} -> {path}</color>");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSlots] Failed to write slot {slot + 1}: {e}");
            error = "Couldn't write the save file.";
            return false;
        }
    }

    // ------------------------------------------------------------------
    // Loading
    // ------------------------------------------------------------------

    /// <summary>
    /// Copies a slot into the live checkpoint system and restores the HUD timer.
    /// The caller then loads <paramref name="sceneName"/>.
    /// </summary>
    public static bool PrepareLoad(int slot, out string sceneName, out string error)
    {
        sceneName = null;
        error = null;

        if (!TryRead(slot, out SaveSlotData d))
        {
            error = "That slot is empty or unreadable.";
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(d.sceneName))
        {
            error = $"Scene '{d.sceneName}' isn't in Build Settings.";
            return false;
        }

        Time.timeScale = 1f;
        CheckpointData.ApplySlot(d);
        ModernCameraHUD.SessionTime = d.playTimeSeconds;

        sceneName = d.sceneName;
        return true;
    }

    public static void DeleteSlot(int slot)
    {
        if (!IsValidSlot(slot)) return;
        try
        {
            if (Directory.Exists(SlotFolder(slot))) Directory.Delete(SlotFolder(slot), true);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSlots] Couldn't delete slot {slot + 1}: {e.Message}");
        }
    }
}

#if UNITY_EDITOR
public static class SaveSlotEditorMenu
{
    [UnityEditor.MenuItem("Tools/Saves/Open Saves Folder")]
    private static void OpenFolder()
    {
        Directory.CreateDirectory(SaveSlotManager.RootFolder);
        UnityEditor.EditorUtility.RevealInFinder(SaveSlotManager.RootFolder);
    }

    [UnityEditor.MenuItem("Tools/Saves/Delete All Slots")]
    private static void DeleteAll()
    {
        for (int i = 0; i < SaveSlotManager.SlotCount; i++) SaveSlotManager.DeleteSlot(i);
        Debug.Log("[SaveSlots] All slots deleted.");
    }
}
#endif