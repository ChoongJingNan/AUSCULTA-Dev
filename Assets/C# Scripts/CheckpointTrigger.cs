using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointTrigger : MonoBehaviour
{
    private bool hasBeenTriggered = false;
    private string checkpointId;

    private void Awake()
    {
        checkpointId = $"{SceneManager.GetActiveScene().name}_{name}_{transform.position}";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenTriggered) return;

        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            hasBeenTriggered = true;

            EncounterManager encounterMgr = FindFirstObjectByType<EncounterManager>();
            if (encounterMgr != null)
            {
                encounterMgr.TriggerNextDynamicCheckpoint(checkpointId, transform.position, transform.rotation, SceneManager.GetActiveScene().name);
            }
        }
    }
}