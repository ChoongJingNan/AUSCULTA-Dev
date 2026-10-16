using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Master + SFX volume. Needs no scene setup: it applies the saved values by itself when the game starts
/// and after every scene load.
///   Master -> AudioListener.volume (scales everything, including music)
///   SFX    -> the "SFXVolume" exposed parameter of the mixer at Assets/Resources/GameAudio.mixer
/// Music keeps its own slider (MusicVolume), handled by LevelMusicManager / EncounterManager.
/// </summary>
public static class GameAudioSettings
{
    public const string MasterKey = "MasterVolume";
    public const string SfxKey = "SFXVolume";

    private const string MixerResourceName = "GameAudio"; // Assets/Resources/GameAudio.mixer
    private const string SfxParam = "SFXVolume";          // exposed parameter name inside that mixer

    private static AudioMixer mixer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyAll();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyAll();

    public static float Master => PlayerPrefs.GetFloat(MasterKey, 1f);
    public static float Sfx => PlayerPrefs.GetFloat(SfxKey, 1f);

    public static void SetMaster(float value)
    {
        PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
        ApplyAll();
    }

    public static void SetSfx(float value)
    {
        PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
        ApplyAll();
    }

    public static void ApplyAll()
    {
        AudioListener.volume = Master;

        if (mixer == null) mixer = Resources.Load<AudioMixer>(MixerResourceName);
        if (mixer != null) mixer.SetFloat(SfxParam, ToDecibels(Sfx));
    }

    private static float ToDecibels(float linear)
    {
        return linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }
}
