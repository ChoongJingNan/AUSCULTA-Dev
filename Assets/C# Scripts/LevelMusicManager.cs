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
        
        // Pull saved volume from PlayerPrefs, falling back to defaultVolume if it doesn't exist yet
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", defaultVolume);
        musicSource.volume = savedVolume;
    }

    private void Start()
        {
            // If loading from a valid checkpoint, do NOT play default or postCheckpointMusic here;
            // let EncounterManager fully handle the dynamic checkpoint music loops.
            if (CheckpointData.hasCheckpoint)
            {
                string savedScene = CheckpointData.GetSavedSceneName();
                if (!string.IsNullOrEmpty(savedScene) && savedScene == SceneManager.GetActiveScene().name)
                {
                    // Stop any default background music and hand control entirely to EncounterManager
                    StopMusic();
                    return; 
                }
            }

            if (levelMusic != null)
            {
                PlayMusic(levelMusic, fadeDuration);
            }
        }

    public bool suppressLevelMusic = false;
    public void PlayMusic(AudioClip newTrack, float duration = 1.5f)
    {
        string trackName = newTrack != null ? newTrack.name : "null";

        // Logged only after the early-outs so the console reflects what actually happens.
        // (Select the log entry and read the bottom pane of the Console for the real call stack.)
        if (suppressLevelMusic && newTrack == levelMusic)
        {
            Debug.Log($"[LevelMusic] PlayMusic({trackName}) IGNORED: level music is suppressed by an active checkpoint stage", this);
            return;
        }
        if (musicSource.clip == newTrack && musicSource.isPlaying) return;

        Debug.Log($"[LevelMusic] PlayMusic({trackName}) -> fading in", this);

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
            // Scale the requested volume by the user's master music slider preference
            float userMusicPref = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicSource.volume = Mathf.Clamp01(targetVolume * userMusicPref);
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
            
            // Get the current user preference for the fade-in ceiling
            float userMusicPref = PlayerPrefs.GetFloat("MusicVolume", defaultVolume);
            float targetPlayVolume = defaultVolume * userMusicPref;

            // Fade in matching the user's saved volume setting
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, targetPlayVolume, t / duration);
                yield return null;
            }
            
            musicSource.volume = targetPlayVolume;
        }

        fadeCoroutine = null;
    }
}