using UnityEngine;

public class VinylPlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionDistance = 3.0f;

    [Header("Audio (Optional)")]
    [Tooltip("Assign an AudioSource if you want music to play when it spins")]
    public AudioSource recordMusic;

    private Transform playerTransform;
    private Animator animator;
    private bool isPlaying = false;

    private void Start()
    {
        // Finds the animator whether it sits on this parent object or a child sub-mesh
        animator = GetComponentInChildren<Animator>();

        // Force disable the Animator component on load so Unity CANNOT auto-play on frame 1
        if (animator != null)
        {
            animator.enabled = false;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Check if the player is close enough and presses 'E'
        if (distance <= interactionDistance && KeyBindings.GetKeyDown(GameAction.Interact))
        {
            // We only want to trigger the animation once
            if (!isPlaying)
            {
                PlayRecord();
            }
        }
    }

    private void PlayRecord()
    {
        isPlaying = true;

        if (animator != null)
        {
            // Re-enable the Animator component
            animator.enabled = true;

            // Direct call to play the "Playing" state instantly, bypassing state machine transition bugs
            animator.Play("Playing");
        }

        // Play the music if an AudioSource is attached
        if (recordMusic != null)
        {
            recordMusic.Play();
        }
    }
}