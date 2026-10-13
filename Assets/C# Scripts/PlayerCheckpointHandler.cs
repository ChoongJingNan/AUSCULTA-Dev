using System.Collections;
using UnityEngine;

public class PlayerCheckpointHandler : MonoBehaviour
{
    private void Start()
    {
        if (CheckpointData.hasCheckpoint)
        {
            StartCoroutine(ForceRespawnRoutine());
        }
    }

    private IEnumerator ForceRespawnRoutine()
    {
        // Wait for 1 frame to let other scripts finish setting up the player
        yield return new WaitForEndOfFrame(); 

        // Use CheckpointData's built-in teleport method which handles physics and transforms safely
        CheckpointData.TryLoadAndTeleportPlayer(gameObject);
    }
}