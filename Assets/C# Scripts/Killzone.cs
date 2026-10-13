using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Killzone : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("Drag your black UI screen here")]
    public Image blackScreen; 
    public float fadeDuration = 1.0f;

    private bool isFading = false;

    private void OnTriggerEnter(Collider other)
    {
        // If the player falls in, and we aren't already fading
        if (other.CompareTag("Player") && !isFading)
        {
            StartCoroutine(FallAndRespawn(other.gameObject));
        }
    }

    private IEnumerator FallAndRespawn(GameObject player)
    {
        isFading = true;

        // 1. Fade the screen to Black
        float timer = 0;
        if (blackScreen != null)
        {
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                SetAlpha(timer / fadeDuration);
                yield return null;
            }
            SetAlpha(1); // Ensure it's fully black
        }

        // 2. Kill and Teleport the player (happens invisibly behind the black screen!)
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Die(); 
        }

        // Wait a tiny moment in the dark for cinematic effect
        yield return new WaitForSeconds(0.5f);

        // 3. Fade the screen back to Clear
        timer = 0;
        if (blackScreen != null)
        {
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                SetAlpha(1f - (timer / fadeDuration));
                yield return null;
            }
            SetAlpha(0); // Ensure it's fully clear
        }

        isFading = false;
    }

    private void SetAlpha(float alphaValue)
    {
        if (blackScreen == null) return;
        Color c = blackScreen.color;
        c.a = alphaValue;
        blackScreen.color = c;
    }
}