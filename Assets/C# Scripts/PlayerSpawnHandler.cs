using UnityEngine;

public class PlayerSpawnHandler : MonoBehaviour
{
    private void Start()
    {
        // Teleports player to the saved coordinates on scene start
        CheckpointData.TryLoadAndTeleportPlayer(gameObject);
    }
}