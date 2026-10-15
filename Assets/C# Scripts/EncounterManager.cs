using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class EncounterManager : MonoBehaviour
{
    private HashSet<string> triggeredCheckpointIds = new HashSet<string>();
    private int globalCheckpointSequence = 0;

    [Header("Audio Sources")]
    [Tooltip("AudioSource dedicated to playing event overlays, stage loops, and chase tracks")]
    public AudioSource eventMusicSource;        

    [Header("Audio Clips")]
    [Tooltip("Main ambient level music track (e.g. One_Loop) to restore when starting or resetting the Backrooms")]
    public AudioClip defaultLevelMusic; 
    public AudioClip inClip;      
    public AudioClip loop1Clip;   
    public AudioClip loop2Clip;   
    public AudioClip loop3Clip;   
    public AudioClip loop4Clip;   
    public AudioClip chaseClip;                 
    public AudioClip outClip;

    [Header("Background Music Settings")]
    [Tooltip("Volume level background music ducks to when an event begins")]
    public float backgroundMusicDuckedVolume = 0.2f;

    public bool introStarted = false; 
    private Coroutine introCoroutine;

    // Volume the event source was authored at in the inspector. The user's saved
    // MusicVolume preference is multiplied on top of this every time a clip starts.
    private float eventBaseVolume = 1f;

    /// <summary>
    /// True when a saved checkpoint exists AND it belongs to the scene that is currently loaded.
    /// </summary>
    private static bool HasCheckpointForThisScene()
    {
        if (!CheckpointData.hasCheckpoint) return false;
        string savedScene = CheckpointData.GetSavedSceneName();
        return !string.IsNullOrEmpty(savedScene) && savedScene == SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// Reloads the saved stage + triggered IDs from disk and plays that stage's loop (no intro).
    /// Safe to call more than once: if the right loop is already playing it is left alone.
    /// </summary>
    private void RestoreFromSavedCheckpoint()
    {
        globalCheckpointSequence = CheckpointData.GetSavedCheckpointIndex();
        triggeredCheckpointIds = CheckpointData.LoadTriggeredIds();
        PlayCheckpointMusic(globalCheckpointSequence, isInitialSave: false);
    }

    /// <summary>
    /// Resets encounter tracking, stage state, and audio routing.
    /// If a checkpoint is saved for this scene, the saved stage is restored instead of wiped,
    /// so a scene reload / save room return never throws away checkpoint progress.
    /// </summary>
    public void ResetEncounterState()
    {
        Debug.Log($"[EncounterManager] ResetEncounterState() in scene '{SceneManager.GetActiveScene().name}' | checkpointForThisScene={HasCheckpointForThisScene()}", this);

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        if (HasCheckpointForThisScene())
        {
            RestoreFromSavedCheckpoint();
            return;
        }

        // ---- No checkpoint for this scene: full wipe back to the default level music ----
        introStarted = false;
        globalCheckpointSequence = 0;
        triggeredCheckpointIds.Clear();

        if (eventMusicSource != null)
        {
            eventMusicSource.Stop();
            eventMusicSource.clip = null;
        }

        if (LevelMusicManager.Instance != null)
        {
            // Un-suppress, otherwise PlayMusic(defaultLevelMusic) is silently ignored
            LevelMusicManager.Instance.suppressLevelMusic = false;
            LevelMusicManager.Instance.SetVolume(1f);

            if (defaultLevelMusic != null)
            {
                LevelMusicManager.Instance.PlayMusic(defaultLevelMusic, 1.0f);
            }
        }
    }

    private void Awake()
    {
        if (eventMusicSource != null)
        {
            eventMusicSource.spatialBlend = 0f;
            eventBaseVolume = eventMusicSource.volume;
        }
    }

    private void Start()
    {
        if (HasCheckpointForThisScene())
        {
            if (LevelMusicManager.Instance != null)
            {
                LevelMusicManager.Instance.StopMusic();
            }

            RestoreFromSavedCheckpoint();
        }
        else
        {
            globalCheckpointSequence = 0;
            triggeredCheckpointIds.Clear();

            if (LevelMusicManager.Instance != null)
            {
                LevelMusicManager.Instance.suppressLevelMusic = false;
            }
        }
    }

    // ------------------------------------------------------------------
    // Event music helpers
    // ------------------------------------------------------------------

    /// <summary>Starts a clip on the event source at (authored volume x user music preference).</summary>
    private void PlayEventClip(AudioClip clip, bool loop)
    {
        if (eventMusicSource == null || clip == null) return;

        eventMusicSource.Stop();
        eventMusicSource.volume = eventBaseVolume * PlayerPrefs.GetFloat("MusicVolume", 1f);
        eventMusicSource.clip = clip;
        eventMusicSource.loop = loop;
        eventMusicSource.Play();
    }

    private AudioClip GetLoopClip(int stage)
    {
        switch (stage)
        {
            case 1: return loop1Clip;
            case 2: return loop2Clip;
            case 3: return loop3Clip;
            default: return loop4Clip;
        }
    }

    public void StartEncounter()
    {
        if (introStarted) return;
        introStarted = true;
        
        introCoroutine = StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        LevelMusicManager.Instance?.SetVolume(backgroundMusicDuckedVolume);

        if (eventMusicSource != null && inClip != null)
        {
            PlayEventClip(inClip, false);

            yield return new WaitForSeconds(inClip.length);

            // Default level music (One_Loop) hands over to the stage loop
            LevelMusicManager.Instance?.StopMusic();

            if (globalCheckpointSequence <= 1 && loop1Clip != null)
            {
                PlayEventClip(loop1Clip, true);
            }
        }

        introCoroutine = null;
    }

    public void TriggerNextDynamicCheckpoint(string checkpointId, Vector3 position, Quaternion rotation, string sceneName)
    {
        Debug.Log($"[Checkpoint] id={checkpointId} | stage now={globalCheckpointSequence} | already used={triggeredCheckpointIds.Contains(checkpointId)}");
        // Already reached (respawned inside it, or walked back through it)
        if (!triggeredCheckpointIds.Add(checkpointId)) return;

        int previousStage = globalCheckpointSequence;
        int targetStage = Mathf.Clamp(previousStage + 1, 1, 4);
        globalCheckpointSequence = targetStage;

        CheckpointData.SaveCheckpoint(position, rotation, sceneName, targetStage);
        CheckpointData.SaveTriggeredIds(triggeredCheckpointIds);

        // Already at the last stage: save the new position, keep the music running
        if (targetStage == previousStage) return;

        // Stage 1 after an entity spot already started In -> Loop 1: don't restart it
        bool introAlreadyHandlingMusic = (targetStage == 1 && introStarted);
        if (!introAlreadyHandlingMusic)
        {
            PlayCheckpointMusic(targetStage, isInitialSave: true);
        }
    }

    public void PlayCheckpointMusic(int checkpointIndex, bool isInitialSave = false)
    {
        globalCheckpointSequence = checkpointIndex;

        PlayerPrefs.SetInt("SavedCheckpointIndex", checkpointIndex);
        PlayerPrefs.Save();

        LevelMusicManager.Instance?.StopMusic();
        if (LevelMusicManager.Instance != null) LevelMusicManager.Instance.suppressLevelMusic = true;

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        introStarted = true;

        if (eventMusicSource == null) return;

        AudioClip targetLoop = GetLoopClip(checkpointIndex);

        // Restoring a saved stage and the right loop is already playing: leave it alone
        if (!isInitialSave && targetLoop != null &&
            eventMusicSource.isPlaying && eventMusicSource.clip == targetLoop && eventMusicSource.loop)
        {
            return;
        }

        eventMusicSource.Stop();

        if (checkpointIndex == 1 && isInitialSave && inClip != null)
        {
            introCoroutine = StartCoroutine(PlayIntroThenLoop(inClip, loop1Clip));
        }
        else if (targetLoop != null)
        {
            PlayEventClip(targetLoop, true);
        }
    }

    private IEnumerator PlayIntroThenLoop(AudioClip intro, AudioClip loop)
    {
        PlayEventClip(intro, false);

        yield return new WaitForSeconds(intro.length);

        if (loop != null)
        {
            PlayEventClip(loop, true);
        }

        introCoroutine = null;
    }

    public void SkipToPostCheckpointState()
    {
        // Restore the SAVED stage instead of forcing stage 1
        PlayCheckpointMusic(CheckpointData.GetSavedCheckpointIndex(), isInitialSave: false);
    }

    public void AdvanceMusicStage() { }

    public void StartChaseMusic()
    {
        if (introCoroutine != null) StopCoroutine(introCoroutine);

        if (eventMusicSource != null && chaseClip != null)
        {
            PlayEventClip(chaseClip, true);
        }
    }

    public void TriggerFinalStalkerMusic()
    {
        StartChaseMusic();
    }

    public void StopChaseAndPlayOut()
    {
        float originalVolume = 1f;
        UnityEngine.Audio.AudioMixerGroup originalMixerGroup = null;

        if (eventMusicSource != null)
        {
            originalVolume = eventMusicSource.volume;
            originalMixerGroup = eventMusicSource.outputAudioMixerGroup;

            eventMusicSource.Stop();
        }

        if (outClip != null)
        {
            GameObject tempAudioObj = new GameObject("OutroMusicPlayer");
            AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
            
            tempSource.clip = outClip;
            tempSource.loop = false;
            tempSource.volume = originalVolume; 
            tempSource.outputAudioMixerGroup = originalMixerGroup;
            tempSource.spatialBlend = 0f;
            tempSource.Play();

            DontDestroyOnLoad(tempAudioObj);
            Destroy(tempAudioObj, outClip.length);
        }
        
        LevelMusicManager.Instance?.SetVolume(0f);
    }
}