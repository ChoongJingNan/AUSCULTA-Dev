using System.Collections;
using UnityEngine;

public class EncounterManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("AudioSource dedicated to playing event overlays, stage loops, and chase tracks")]
    public AudioSource eventMusicSource;        

    [Header("Audio Clips")]
    [Tooltip("Main ambient level music track (e.g. One_Loop) to restore when starting or resetting the Backrooms")]
    public AudioClip defaultLevelMusic; 
    public AudioClip inClip;      // Plays during first checkpoint initial transition ("Something's Wrong In")
    public AudioClip loop1Clip;   // Plays during first checkpoint loop ("Something's Wrong Loop1")
    public AudioClip loop2Clip;   // Plays for Checkpoint 2
    public AudioClip loop3Clip;   // Plays for Checkpoint 3
    public AudioClip loop4Clip;   // Plays for Checkpoint 4
    public AudioClip chaseClip;                 
    public AudioClip outClip;

    [Header("Background Music Settings")]
    [Tooltip("Volume level background music ducks to when an event begins")]
    public float backgroundMusicDuckedVolume = 0.2f;

    private int encounterStage = 0; 
    private int vanishedEntitiesCount = 0; 
    public bool introStarted = false; 

    private Coroutine introCoroutine;

    /// <summary>
    /// Resets all encounter tracking, stage state, and audio routing to clean default settings.
    /// </summary>
    public void ResetEncounterState()
    {
        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        introStarted = false;
        encounterStage = 0;
        vanishedEntitiesCount = 0;

        if (eventMusicSource != null)
        {
            eventMusicSource.Stop();
            eventMusicSource.clip = null;
        }

        // Restore central level background music track & volume
        if (LevelMusicManager.Instance != null)
        {
            LevelMusicManager.Instance.SetVolume(1f);

            if (defaultLevelMusic != null)
            {
                LevelMusicManager.Instance.PlayMusic(defaultLevelMusic, 1.0f);
            }
        }
    }

    public void StartEncounter()
    {
        if (introStarted) return;
        introStarted = true;
        encounterStage = 0;
        vanishedEntitiesCount = 0;
        
        introCoroutine = StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        LevelMusicManager.Instance?.SetVolume(backgroundMusicDuckedVolume);

        if (eventMusicSource != null)
        {
            eventMusicSource.clip = inClip;
            eventMusicSource.loop = false;
            eventMusicSource.Play();

            yield return new WaitForSeconds(inClip.length);

            if (encounterStage == 0 && loop1Clip != null)
            {
                eventMusicSource.clip = loop1Clip;
                eventMusicSource.loop = true;
                eventMusicSource.Play();
            }
        }

        introCoroutine = null;
    }

    public void PlayCheckpointMusic(int checkpointIndex, bool isInitialSave = false)
    {
        LevelMusicManager.Instance?.StopMusic();

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        introStarted = true;

        if (eventMusicSource == null) return;

        eventMusicSource.Stop();

        switch (checkpointIndex)
        {
            case 1:
                if (isInitialSave && inClip != null)
                {
                    introCoroutine = StartCoroutine(PlayIntroThenLoop(inClip, loop1Clip));
                }
                else if (loop1Clip != null)
                {
                    eventMusicSource.clip = loop1Clip;
                    eventMusicSource.loop = true;
                    eventMusicSource.Play();
                }
                break;

            case 2:
                if (loop2Clip != null)
                {
                    eventMusicSource.clip = loop2Clip;
                    eventMusicSource.loop = true;
                    eventMusicSource.Play();
                }
                break;

            case 3:
                if (loop3Clip != null)
                {
                    eventMusicSource.clip = loop3Clip;
                    eventMusicSource.loop = true;
                    eventMusicSource.Play();
                }
                break;

            default:
                if (loop4Clip != null)
                {
                    eventMusicSource.clip = loop4Clip;
                    eventMusicSource.loop = true;
                    eventMusicSource.Play();
                }
                break;
        }
    }

    private IEnumerator PlayIntroThenLoop(AudioClip intro, AudioClip loop)
    {
        eventMusicSource.clip = intro;
        eventMusicSource.loop = false;
        eventMusicSource.Play();

        yield return new WaitForSeconds(intro.length);

        if (loop != null)
        {
            eventMusicSource.clip = loop;
            eventMusicSource.loop = true;
            eventMusicSource.Play();
        }

        introCoroutine = null;
    }

    public void SkipToPostCheckpointState()
    {
        PlayCheckpointMusic(1, isInitialSave: false);
    }

    public void AdvanceMusicStage()
    {
        vanishedEntitiesCount++;

        if (vanishedEntitiesCount <= 1) return;

        encounterStage++;

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        if (eventMusicSource == null) return;

        if (encounterStage == 1 && loop2Clip != null)
        {
            eventMusicSource.clip = loop2Clip;
            eventMusicSource.loop = true;
            eventMusicSource.Play();
        }
        else if (encounterStage == 2 && loop3Clip != null)
        {
            eventMusicSource.clip = loop3Clip;
            eventMusicSource.loop = true;
            eventMusicSource.Play();
        }
        else if (encounterStage >= 3 && loop4Clip != null)
        {
            eventMusicSource.clip = loop4Clip;
            eventMusicSource.loop = true;
            eventMusicSource.Play();
        }
    }

    public void StartChaseMusic()
    {
        if (introCoroutine != null) StopCoroutine(introCoroutine);

        if (eventMusicSource != null && chaseClip != null)
        {
            eventMusicSource.Stop();
            eventMusicSource.clip = chaseClip;
            eventMusicSource.loop = true;
            eventMusicSource.Play();
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