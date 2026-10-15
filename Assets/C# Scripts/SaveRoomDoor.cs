using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SaveRoomDoor : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The exact name of the save room scene")]
    public string targetSceneName = "SaveRoomBuild";

    [Header("Interaction Settings")]
    public float interactionDistance = 3.0f;

    [Header("UI Settings")]
    [Tooltip("Drag a simple UI Image here (color set to black)")]
    public Image blackFadeImage;
    public float fadeDuration = 2.0f;

    private Transform playerTransform;
    private bool isTransitioning = false;

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // Ensure the black screen is hidden when the level starts
        if (blackFadeImage != null)
        {
            blackFadeImage.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // Stop checking for input if we are already fading
        if (isTransitioning || playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distance <= interactionDistance && KeyBindings.GetKeyDown(GameAction.Interact))
        {
            StartCoroutine(TransitionToSaveRoom());
        }
    }

    private IEnumerator TransitionToSaveRoom()
    {
        isTransitioning = true;

        // Smoothly fade out the level music
        LevelMusicManager.Instance?.PlayMusic(null, fadeDuration);

        // Perform the Fade to Black
        if (blackFadeImage != null)
        {
            blackFadeImage.gameObject.SetActive(true);
            blackFadeImage.color = new Color(0f, 0f, 0f, 0f);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                blackFadeImage.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }
            
            // Ensure it is completely black at the end
            blackFadeImage.color = Color.black;
            
            // Optional: Hold pure black for half a second so it doesn't feel abrupt
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            Debug.LogWarning("SaveRoomDoor: No Black Fade Image assigned! Skipping visual fade.");
        }

        // Load the Save Room
        SceneManager.LoadScene(targetSceneName);
    }
}