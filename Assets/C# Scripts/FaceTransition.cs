using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FaceTransition : MonoBehaviour
{
    [Header("Scene Settings")]
    public string targetSceneName = "Level2"; 

    [Header("Startup Settings (For new scene loads)")]
    [Tooltip("Check this if the scene should start black and fade in (e.g., in Save Rooms)")]
    public bool fadeInOnStart = false;
    public float fadeInDuration = 2.0f;

    [Header("Transition Timings")]
    public float fadeToBlackDuration = 3.0f;     
    public float blackHoldDuration = 2.0f;       
    public float heartbeat1Duration = 2.0f;      
    public float imageFadeTotalDuration = 7.0f;  
    public float imageCutoffTime = 6.0f;         

    [Header("Visual Settings")]
    [Tooltip("Maximum opacity the face image will reach (0.4 = 40%)")]
    [Range(0f, 1f)]
    public float maxFaceOpacity = 0.4f;

    [Header("Level-Specific Image")]
    public Sprite levelTransitionSprite; 

    [Header("UI References")]
    public Image blackScreenImage;       
    public Image faceRevealImage;        
    
    [Header("Script References")]
    public PlayerHealth playerHealth;         
    public EncounterManager encounterManager; 

    private bool sequenceStarted = false;

    private void Start()
    {
        if (faceRevealImage != null) faceRevealImage.gameObject.SetActive(false);

        if (fadeInOnStart && blackScreenImage != null)
        {
            StartCoroutine(FadeInSequence());
        }
        else if (blackScreenImage != null)
        {
            blackScreenImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeInSequence()
    {
        blackScreenImage.gameObject.SetActive(true);
        blackScreenImage.color = Color.black;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / fadeInDuration);
            blackScreenImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        blackScreenImage.gameObject.SetActive(false);
    }

    public void BeginTransition()
    {
        if (sequenceStarted) return;
        sequenceStarted = true; 

        EntitySpot[] allEntities = Object.FindObjectsByType<EntitySpot>(FindObjectsSortMode.None);
        foreach (EntitySpot entity in allEntities)
        {
            if (entity.isChaseEntity)
            {
                entity.CompleteChaseEscape(true); 
            }
        }

        GameObject player = GameObject.FindWithTag("Player");

        if (playerHealth == null && player != null) 
            playerHealth = player.GetComponentInParent<PlayerHealth>();

        if (encounterManager == null) 
            encounterManager = Object.FindFirstObjectByType<EncounterManager>();

        if (player != null)
        {
            var fpc = player.GetComponentInParent<FirstPersonController>();
            if (fpc != null) fpc.enabled = false; 
        }

        bool isSafeRoom = targetSceneName.ToLower().Contains("save");
        float actualFadeOutTime = isSafeRoom ? 2.0f : fadeToBlackDuration;
        
        LevelMusicManager.Instance?.PlayMusic(null, actualFadeOutTime);

        StartCoroutine(TransitionSequenceCoroutine(isSafeRoom, actualFadeOutTime));
    }

    private IEnumerator TransitionSequenceCoroutine(bool isSafeRoom, float fadeOutTime)
    {
        if (blackScreenImage == null)
        {
            Debug.LogError("FaceTransition: Missing Black Screen Image! Loading scene directly.");
            SceneManager.LoadScene(targetSceneName);
            yield break;
        }

        if (playerHealth != null)
        {
            playerHealth.enabled = false;
        }

        blackScreenImage.sprite = null; 
        blackScreenImage.color = new Color(0f, 0f, 0f, 0f);
        blackScreenImage.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeOutTime);
            blackScreenImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        blackScreenImage.color = Color.black;

        float holdTime = isSafeRoom ? 0.5f : blackHoldDuration;
        yield return new WaitForSeconds(holdTime);

        if (!isSafeRoom)
        {
            AudioSource heartbeatSource = playerHealth != null ? playerHealth.heartbeatAudioSource : null;
            if (heartbeatSource != null && playerHealth.heartbeatStage1 != null)
            {
                heartbeatSource.clip = playerHealth.heartbeatStage1;
                heartbeatSource.loop = true;
                heartbeatSource.volume = 1f;
                heartbeatSource.Play();
            }

            yield return new WaitForSeconds(heartbeat1Duration);

            if (heartbeatSource != null && playerHealth.heartbeatStage2 != null)
            {
                heartbeatSource.clip = playerHealth.heartbeatStage2;
                heartbeatSource.loop = true;
                heartbeatSource.Play();
            }

            if (faceRevealImage != null)
            {
                if (levelTransitionSprite != null)
                {
                    faceRevealImage.sprite = levelTransitionSprite;
                }
                
                faceRevealImage.color = new Color(1f, 1f, 1f, 0f);
                faceRevealImage.gameObject.SetActive(true);

                elapsed = 0f;
                while (elapsed < imageCutoffTime)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / imageFadeTotalDuration);
                    float currentAlpha = progress * maxFaceOpacity;
                    
                    faceRevealImage.color = new Color(1f, 1f, 1f, currentAlpha);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(imageCutoffTime);
            }

            if (heartbeatSource != null)
            {
                heartbeatSource.Stop();
            }
        }

        // Deletes checkpoint data before transitioning to the next level
        CheckpointData.DeleteSavedData();
        SceneManager.LoadScene(targetSceneName);
    }
}