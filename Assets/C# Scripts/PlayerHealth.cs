using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 4; 
    public float invulnerabilityDuration = 1.2f;

    [Header("Overlay Fade Settings")]
    public float fadeStartDelay = 8.0f; 
    public float fadeDuration = 4.0f;   

    [Header("UI Overlays")]
    public Image bloodOverlay1; 
    public Image bloodOverlay2; 

    [Header("Audio Settings")]
    public AudioSource heartbeatAudioSource;
    public AudioClip heartbeatStage1; 
    public AudioClip heartbeatStage2; 

    private int currentHealth;
    private bool isInvulnerable = false;
    private float lastHitTime = -999f;

    private float currentOverlay1Alpha = 0f;
    private float currentOverlay2Alpha = 0f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private CharacterController characterController;

    private void Start()
    {
        currentHealth = maxHealth;
        startPosition = transform.position;
        startRotation = transform.rotation;
        characterController = GetComponent<CharacterController>();

        if (heartbeatAudioSource == null)
        {
            heartbeatAudioSource = GetComponent<AudioSource>();
        }

        SetImageAlpha(bloodOverlay1, 0f);
        SetImageAlpha(bloodOverlay2, 0f);
        UpdateHeartbeatAudio();
    }

    private void Update()
    {
        float timeSinceHit = Time.time - lastHitTime;
        UpdateOverlayAlphas(timeSinceHit);
    }

    public void TakeDamage(int damageAmount)
    {
        if (isInvulnerable) return;

        lastHitTime = Time.time;
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);
        Debug.Log($"<color=red>Player Hit!</color> Remaining Health: {currentHealth}");

        if (currentHealth == 3)
        {
            currentOverlay1Alpha = 0.5f;
        }
        else if (currentHealth == 2)
        {
            currentOverlay1Alpha = 1.0f;
        }
        else if (currentHealth == 1)
        {
            currentOverlay1Alpha = 1.0f;
            currentOverlay2Alpha = 1.0f;
        }

        UpdateHeartbeatAudio();

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    public int GetCurrentHealth()
    {
       return currentHealth;
    }

    private void UpdateHeartbeatAudio()
    {
        if (heartbeatAudioSource == null) return;

        if (currentHealth == 2 || currentHealth == 3)
        {
            if (heartbeatAudioSource.clip != heartbeatStage1 || !heartbeatAudioSource.isPlaying)
            {
                heartbeatAudioSource.clip = heartbeatStage1;
                heartbeatAudioSource.loop = true;
                heartbeatAudioSource.Play();
            }
        }
        else if (currentHealth == 1)
        {
            if (heartbeatAudioSource.clip != heartbeatStage2 || !heartbeatAudioSource.isPlaying)
            {
                heartbeatAudioSource.clip = heartbeatStage2;
                heartbeatAudioSource.loop = true;
                heartbeatAudioSource.Play();
            }
        }
        else
        {
            if (heartbeatAudioSource.isPlaying)
            {
                heartbeatAudioSource.Stop();
            }
            heartbeatAudioSource.clip = null;
        }
    }

    private void UpdateOverlayAlphas(float timeSinceHit)
    {
        float target1Alpha = 0f;
        float target2Alpha = 0f;

        float fadeAlpha = 1.0f;
        if (timeSinceHit >= fadeStartDelay)
        {
            float fadeProgress = (timeSinceHit - fadeStartDelay) / fadeDuration;
            fadeAlpha = Mathf.Clamp01(1.0f - fadeProgress);
        }

        if (currentHealth == 1)
        {
            target1Alpha = 1.0f;      
            target2Alpha = fadeAlpha;  
        }
        else if (currentHealth == 2)
        {
            target1Alpha = fadeAlpha;  
            target2Alpha = 0.0f;       
        }
        else if (currentHealth == 3)
        {
            target1Alpha = fadeAlpha * 0.5f;
            target2Alpha = 0.0f;
        }
        else if (currentHealth >= 4)
        {
            target1Alpha = 0.0f;
            target2Alpha = 0.0f;
        }

        float step = (1.0f / fadeDuration) * Time.deltaTime;
        currentOverlay1Alpha = Mathf.MoveTowards(currentOverlay1Alpha, target1Alpha, step);
        currentOverlay2Alpha = Mathf.MoveTowards(currentOverlay2Alpha, target2Alpha, step);

        SetImageAlpha(bloodOverlay1, currentOverlay1Alpha);
        SetImageAlpha(bloodOverlay2, currentOverlay2Alpha);
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }

    public void Die()
    {
        Debug.Log("<color=red>Player Died!</color> Respawning...");

        if (CheckpointData.TryLoadAndTeleportPlayer(gameObject))
        {
            Debug.Log("Teleported player to saved Checkpoint!");
        }
        else
        {
            if (characterController != null) characterController.enabled = false;
            transform.position = startPosition;
            transform.rotation = startRotation;
            if (characterController != null) characterController.enabled = true;
            
            Debug.Log("No checkpoint found. Teleported to level start.");
        }

        currentHealth = maxHealth;
        isInvulnerable = false;

        currentOverlay1Alpha = 0f;
        currentOverlay2Alpha = 0f;
        SetImageAlpha(bloodOverlay1, 0f);
        SetImageAlpha(bloodOverlay2, 0f);

        UpdateHeartbeatAudio();
    }

    public void TriggerFinalTransitionEffects()
    {
        currentHealth = 1;
        lastHitTime = Time.time; 

        UpdateOverlayAlphas(0f); 
        UpdateHeartbeatAudio();
        
        Debug.Log("<color=magenta>PlayerHealth: Final transition effects triggered!</color>");
    }
}