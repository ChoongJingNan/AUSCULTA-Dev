using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointTrigger : MonoBehaviour
{
    [Header("Checkpoint Config")]
    [Tooltip("Set to 1 for First Checkpoint, 2 for Second Checkpoint, etc.")]
    public int checkpointIndex = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            // Save location along with checkpoint index number
            CheckpointData.SaveCheckpoint(transform.position, SceneManager.GetActiveScene().name, checkpointIndex);

            // Change music immediately in-game
            EncounterManager encounterMgr = FindFirstObjectByType<EncounterManager>();
            if (encounterMgr != null)
            {
                encounterMgr.PlayCheckpointMusic(checkpointIndex, isInitialSave: true);
            }
        }
    }
}