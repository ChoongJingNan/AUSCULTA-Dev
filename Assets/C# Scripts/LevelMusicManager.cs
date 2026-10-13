using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMusicManager : MonoBehaviour
{
    public static LevelMusicManager Instance { get; private set; }

    [Header("Audio Settings")]
    public AudioSource musicSource;
    public AudioClip levelMusic; // Starting level background music
    public AudioClip postCheckpointMusic; // Optional: Specific track to play when continuing from a save
    [Range(0f, 1f)] public float defaultVolume = 0.5f;
    public float fadeDuration = 1.5f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (levelMusic != null && !CheckpointData.hasCheckpoint)
            {
                Instance.PlayMusic(levelMusic, fadeDuration);
            }
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null) musicSource = GetComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = defaultVolume;
    }

    private void Start()
    {
        // If loading from a valid checkpoint, do NOT force starting level music
        if (CheckpointData.hasCheckpoint)
        {
            string savedScene = CheckpointData.GetSavedSceneName();
            if (!string.IsNullOrEmpty(savedScene) && savedScene == SceneManager.GetActiveScene().name)
            {
                if (postCheckpointMusic != null)
                {
                    PlayMusic(postCheckpointMusic, fadeDuration);
                }
                return; // Hands music control to EncounterManager or postCheckpointMusic
            }
        }

        if (levelMusic != null)
        {
            PlayMusic(levelMusic, fadeDuration);
        }
    }

    public void PlayMusic(AudioClip newTrack, float duration = 1.5f)
    {
        if (musicSource.clip == newTrack && musicSource.isPlaying) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToTrack(newTrack, duration));
    }

    public void StopMusic()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void SetVolume(float targetVolume)
    {
        // Stop any active fade coroutine so it doesn't overwrite manual volume changes
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(targetVolume);
        }
    }

    private IEnumerator FadeToTrack(AudioClip newTrack, float duration)
    {
        float startVolume = musicSource.volume;

        // Fade out
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newTrack;

        if (newTrack != null)
        {
            musicSource.Play();
            // Fade in
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, defaultVolume, t / duration);
                yield return null;
            }
        }

        fadeCoroutine = null;
    }
}