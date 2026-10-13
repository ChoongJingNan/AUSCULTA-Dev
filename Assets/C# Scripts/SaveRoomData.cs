using UnityEngine;
using BayatGames.SaveGameFree;

public static class SaveRoomData
{
    private const string RETURN_SCENE_KEY = "SaveRoom_ReturnScene";
    private const string RETURN_POS_KEY = "SaveRoom_ReturnPos";
    private const string RETURN_ROT_KEY = "SaveRoom_ReturnRot";
    private const string WAS_IN_SAVEROOM_KEY = "SaveRoom_WasInSaveRoom";

    public static void SaveReturnPoint(string currentScene, Vector3 position, Quaternion rotation)
    {
        SaveGame.Save(RETURN_SCENE_KEY, currentScene);
        SaveGame.Save(RETURN_POS_KEY, position);
        SaveGame.Save(RETURN_ROT_KEY, rotation);
        SaveGame.Save(WAS_IN_SAVEROOM_KEY, true);
    }

    public static bool HasReturnPoint()
    {
        return SaveGame.Load<bool>(WAS_IN_SAVEROOM_KEY, false);
    }

    public static string GetReturnScene()
    {
        return SaveGame.Load<string>(RETURN_SCENE_KEY, "");
    }

    public static void ConsumeReturnPoint(out Vector3 position, out Quaternion rotation)
    {
        position = SaveGame.Load<Vector3>(RETURN_POS_KEY, Vector3.zero);
        rotation = SaveGame.Load<Quaternion>(RETURN_ROT_KEY, Quaternion.identity);
        SaveGame.Save(WAS_IN_SAVEROOM_KEY, false);
    }
}