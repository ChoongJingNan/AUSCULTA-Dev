using UnityEngine;

public class NPCConversationController : MonoBehaviour
{
    [Header("Conversation Settings")]
    [Tooltip("How close the player needs to be to stay in conversation")]
    public float maxInteractionDistance = 5f;
    [Tooltip("Optional: Point this to the NPC's head/eyes. If left empty, it uses the NPC's center.")]
    public Transform lookTarget;

    private bool isInConversation = false;
    private GameObject currentPlayer;
    private MonoBehaviour playerMouseLookScript; // Reference to your camera/mouse look script

    private void Start()
    {
        if (lookTarget == null)
        {
            lookTarget = transform; // Default to the object itself
        }
    }

    private void Update()
    {
        if (isInConversation && currentPlayer != null)
        {
            // 1. Force the camera to look at the NPC
            Vector3 direction = (lookTarget.position - currentPlayer.transform.position).normalized;
            // Keep the rotation level (ignore steep up/down head snapping if preferred, or look straight at target)
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            // Smoothly snap player's view to the NPC
            currentPlayer.transform.rotation = Quaternion.Slerp(currentPlayer.transform.rotation, targetRotation, Time.deltaTime * 10f);

            // 2. Check if the player walked away to disengage
            float distance = Vector3.Distance(transform.position, currentPlayer.transform.position);
            if (distance > maxInteractionDistance)
            {
                EndConversation();
            }
        }
    }

    // Call this method when the player interacts/talks to the NPC
    public void StartConversation(GameObject player)
    {
        if (isInConversation) return;

        isInConversation = true;
        currentPlayer = player;

        // Disable the player's mouse look script so they can't move the cursor/camera around
        // (Replace 'FirstPersonLook' with whatever your camera script is named, e.g., MouseLook, PlayerCam, etc.)
        playerMouseLookScript = currentPlayer.GetComponentInChildren<MonoBehaviour>(); // Or target your specific camera script component
        
        // Alternative: If your FirstPersonController handles mouse look on the player GameObject:
        var fpc = currentPlayer.GetComponent<FirstPersonController>();
        if (fpc != null)
        {
            // If you have a variable or method to disable look, or you can disable look input here
        }

        Debug.Log("Conversation started. Camera locked to NPC.");
    }

    public void EndConversation()
    {
        if (!isInConversation) return;

        isInConversation = false;
        currentPlayer = null;

        // Re-enable mouse look so the player has full freedom again
        if (playerMouseLookScript != null)
        {
            playerMouseLookScript.enabled = true;
        }

        Debug.Log("Conversation ended. Player walked away.");
    }
}