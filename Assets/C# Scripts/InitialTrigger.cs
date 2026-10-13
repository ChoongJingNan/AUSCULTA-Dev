using UnityEngine;
using UnityEngine.SceneManagement;
using BayatGames.SaveGameFree;

public class InitialTrigger : MonoBehaviour
{
    [Header("References")]
    public EncounterManager encounterManager;

    [Tooltip("Drag the Entity/Monster GameObject here to despawn when loading a checkpoint.")]
    public GameObject entityToDespawn;

    [Header("Settings")]
    [Tooltip("Distance in meters from the checkpoint to consider this encounter already cleared.")]
    public float triggerCheckRadius = 10.0f;

    private bool triggered = false;
    private bool isReady = false;

    private void Start()
    {
        // 1. CHECK IF CONTINUING FROM A SAVED CHECKPOINT
        if (CheckpointData.hasCheckpoint)
        {
            string savedScene = CheckpointData.GetSavedSceneName();
            if (!string.IsNullOrEmpty(savedScene) && savedScene == SceneManager.GetActiveScene().name)
            {
                if (SaveGame.Exists("Saved_Player_Position"))
                {
                    Vector3 savedPos = SaveGame.Load<Vector3>("Saved_Player_Position");
                    float distance = Vector3.Distance(transform.position, savedPos);

                    // If player loaded near this checkpoint spot, bypass encounter & update music via EncounterManager
                    if (distance <= triggerCheckRadius)
                    {
                        Debug.Log("<color=yellow>INTRO TRIGGER:</color> Loaded checkpoint. Despawning entity & setting post-encounter audio state.");

                        if (encounterManager != null)
                        {
                            encounterManager.SkipToPostCheckpointState();
                        }

                        if (entityToDespawn != null)
                        {
                            entityToDespawn.SetActive(false);
                        }

                        gameObject.SetActive(false);
                        return;
                    }
                }
            }
        }

        // 2. DELAY TRIGGER ACTIVATION
        Invoke(nameof(EnableTrigger), 0.2f);
    }

    private void EnableTrigger()
    {
        isReady = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isReady || triggered) return;

        bool isPlayer = other.CompareTag("Player") || other.transform.root.CompareTag("Player");

        if (isPlayer)
        {
            triggered = true;
            Debug.Log("<color=cyan>INTRO TRIGGER:</color> Walked through intro trigger! Starting music sequence...");

            if (encounterManager != null)
            {
                encounterManager.StartEncounter();
            }

            gameObject.SetActive(false);
        }
    }
}