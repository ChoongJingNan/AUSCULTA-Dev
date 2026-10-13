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

        CharacterController cc = GetComponent<CharacterController>();
        Rigidbody rb = GetComponent<Rigidbody>();

        // 1. Disable physics
        if (cc != null) cc.enabled = false;
        if (rb != null) rb.isKinematic = true;

        // 2. Teleport the player
        transform.position = CheckpointData.checkpointPosition;
        transform.rotation = CheckpointData.checkpointRotation;

        // 3. Re-enable physics
        if (cc != null) cc.enabled = true;
        if (rb != null) rb.isKinematic = false;
    }
}