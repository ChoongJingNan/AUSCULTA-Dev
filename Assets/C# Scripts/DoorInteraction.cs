using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteraction : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Type the EXACT name of the scene file you want to load. (Ignored if 'Is Save Room Exit' is checked)")]
    public string targetSceneName = "SaveRoomBuild";

    [Header("Save Room Settings")]
    [Tooltip("Check this box ONLY on the door INSIDE the Save Room to exit back to the previous level.")]
    public bool isSaveRoomExit = false;

    [Tooltip("Place an empty GameObject behind this door in your level and assign it here. When leaving the Save Room, the player spawns at this point.")]
    public Transform returnSpawnPoint;

    [Header("Transition Settings")]
    public FaceTransition transitionScript;
    public float interactionDistance = 3.0f;

    private Transform playerTransform;
    private bool isOpenTriggered = false;

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;

        if (transitionScript == null)
        {
            transitionScript = GetComponent<FaceTransition>();
        }
    }

    private void Update()
    {
        if (!isOpenTriggered && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= interactionDistance && KeyBindings.GetKeyDown(GameAction.Interact))
            {
                isOpenTriggered = true;
                InteractWithDoor();
            }
        }
    }

    private void InteractWithDoor()
    {
        string destinationScene = targetSceneName;

        if (isSaveRoomExit)
        {
            // Exit door logic: Load the level the player came from
            destinationScene = SaveRoomData.GetReturnScene();

            if (string.IsNullOrEmpty(destinationScene))
            {
                Debug.LogWarning($"DoorInteraction: No return scene stored. Fallback to default {targetSceneName}");
                destinationScene = targetSceneName;
            }
        }
        else if (returnSpawnPoint != null)
        {
            // Entrance door logic: Save current scene & spawn position before entering Save Room
            SaveRoomData.SaveReturnPoint(
                SceneManager.GetActiveScene().name,
                returnSpawnPoint.position,
                returnSpawnPoint.rotation
            );
        }

        // Trigger Scene Loading / Transition
        if (transitionScript != null)
        {
            transitionScript.targetSceneName = destinationScene;
            transitionScript.BeginTransition();
        }
        else
        {
            Debug.LogWarning($"DoorInteraction: FaceTransition missing on {gameObject.name}. Loading {destinationScene} directly.");
            SceneManager.LoadScene(destinationScene);
        }
    }
}