using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuBackgroundLoader : MonoBehaviour
{
    [Header("Default Fallback Map")]
    [Tooltip("Used when there is no last-visited map yet (fresh install / new game)")]
    public string defaultScene = "R1_BackroomsBuild";

    [Header("Menu Rig (camera + 3D menu UI)")]
    [Tooltip("Empty parent (at position 0, rotation 0) holding the menu camera and the world-space menu UI. It gets moved so the camera lands on the map's MenuCameraAnchor.")]
    public Transform menuRig;

    [Tooltip("The camera that looks at the menu. Auto-found under the rig if left empty.")]
    public Camera menuCamera;

    [Header("Menu Music")]
    [Tooltip("Play the backdrop map's music behind the menu.")]
    public bool playMapMusic = true;
    [Range(0f, 1f)] public float menuMusicVolume = 0.5f;
    public float musicFadeIn = 1.5f;

    [Header("Lighting")]
    [Tooltip("Make the backdrop map the active scene so ITS skybox, ambient light, fog and reflections are used instead of the menu scene's.")]
    public bool useMapLighting = true;

    /// <summary>Name of the map currently loaded behind the menu ("" when none).</summary>
    public static string BackdropSceneName { get; private set; } = "";

    // Handle of the loaded backdrop scene (-1 until the load has finished)
    private static int backdropHandle = -1;

    /// <summary>
    /// True when this component lives in the map loaded behind the menu rather than in a map being played.
    /// Gameplay scripts that change saved state or global settings should return early when this is true.
    /// </summary>
    public static bool IsBackdrop(Component c)
    {
        if (c == null || string.IsNullOrEmpty(BackdropSceneName)) return false;
        Scene s = c.gameObject.scene;
        if (s.name != BackdropSceneName) return false;
        return backdropHandle == -1 || s.handle == backdropHandle;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        BackdropSceneName = "";
        backdropHandle = -1;
    }

    // Rest pose of the rig and camera, captured before anything can animate them
    private Vector3 rigStartPos, camStartPos;
    private Quaternion rigStartRot, camStartRot;
    private bool poseCaptured;

    private static MenuBackgroundLoader current;
    private AudioSource musicSource;
    private Coroutine musicFade;

    private float TargetMusicVolume => Mathf.Clamp01(menuMusicVolume * PlayerPrefs.GetFloat("MusicVolume", 1f));

    /// <summary>Call after the music slider moves so the menu track follows it live.</summary>
    public static void RefreshMusicVolume()
    {
        if (current != null && current.musicSource != null && current.musicFade == null)
            current.musicSource.volume = current.TargetMusicVolume;
    }

    private void StartMapMusic(GameObject[] roots, MenuCameraAnchor anchor)
    {
        // 1) clip set on the map's anchor, 2) the map's LevelMusicManager track, 3) EncounterManager default music
        AudioClip clip = anchor != null ? anchor.menuMusic : null;

        if (clip == null)
            foreach (GameObject r in roots)
            {
                var lm = r.GetComponentInChildren<LevelMusicManager>(true);
                if (lm != null && lm.levelMusic != null) { clip = lm.levelMusic; break; }
            }

        if (clip == null)
            foreach (GameObject r in roots)
            {
                var em = r.GetComponentInChildren<EncounterManager>(true);
                if (em != null && em.defaultLevelMusic != null) { clip = em.defaultLevelMusic; break; }
            }

        // The map's own gameplay music systems would double up with this, so silence them
        foreach (var lm in FindObjectsByType<LevelMusicManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (lm.musicSource != null) lm.musicSource.Stop();
        foreach (var em in FindObjectsByType<EncounterManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (em.eventMusicSource != null) em.eventMusicSource.Stop();

        if (clip == null)
        {
            Debug.LogWarning("[MenuBackgroundLoader] No music found for this map. Assign 'Menu Music' on its MenuCameraAnchor.", this);
            return;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
        }

        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(FadeInMusic());
    }

    private IEnumerator FadeInMusic()
    {
        float duration = Mathf.Max(0.01f, musicFadeIn);
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, TargetMusicVolume, t / duration);
            yield return null;
        }
        musicSource.volume = TargetMusicVolume;
        musicFade = null;
    }

    private void Start()
    {
        current = this;
        if (menuCamera == null && menuRig != null) menuCamera = menuRig.GetComponentInChildren<Camera>(true);

        if (menuRig != null && menuCamera != null)
        {
            rigStartPos = menuRig.position;
            rigStartRot = menuRig.rotation;
            camStartPos = menuCamera.transform.position;
            camStartRot = menuCamera.transform.rotation;
            poseCaptured = true;
        }

        // Last map the player was actually in (R1, Save Room, Poolrooms...), or the default
        string sceneToLoad = LastMapTracker.GetLastMap(defaultScene);
        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad)) sceneToLoad = defaultScene;

        StartCoroutine(LoadSceneInBackground(sceneToLoad));
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
        BackdropSceneName = "";
        backdropHandle = -1;
    }

    private IEnumerator LoadSceneInBackground(string sceneName)
    {
        // Must be set BEFORE loading so Awake/Start in the backdrop map can already detect it
        BackdropSceneName = sceneName;
        backdropHandle = -1;

        // Load the map additively behind the menu UI
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (!loadedScene.IsValid()) yield break;

        backdropHandle = loadedScene.handle;

        GameObject[] roots = loadedScene.GetRootGameObjects();

        // Only switch off the backdrop map's EventSystem if the menu already has its own active one.
        // (Switching it off when the menu has none would leave every button dead.)
        bool menuHasEventSystem = false;
        foreach (var es in FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None))
        {
            if (es.gameObject.scene != loadedScene) { menuHasEventSystem = true; break; }
        }

        if (menuHasEventSystem)
        {
            foreach (GameObject rootObj in roots)
            {
                foreach (var es in rootObj.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
                    es.gameObject.SetActive(false);
            }
        }

        // Find this map's camera anchor (searched before anything is deactivated)
        MenuCameraAnchor anchor = null;
        foreach (GameObject rootObj in roots)
        {
            anchor = rootObj.GetComponentInChildren<MenuCameraAnchor>(true);
            if (anchor != null) break;
        }

        if (anchor != null) MoveRigSoCameraSitsAt(anchor.transform);

        if (playMapMusic) StartMapMusic(roots, anchor);

        // Skybox, ambient, fog and reflection settings come from the ACTIVE scene only
        if (useMapLighting) SceneManager.SetActiveScene(loadedScene);

        // Disable gameplay elements (player controller, HUD canvas) from the background scene
        foreach (GameObject rootObj in roots)
        {
            if (rootObj.CompareTag("Player") || rootObj.GetComponentInChildren<Canvas>() != null)
            {
                rootObj.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Moves the whole menu rig (camera + UI together) rigidly, so the camera's rest pose lands exactly on the anchor.
    /// The map itself is never moved.
    /// </summary>
    private void MoveRigSoCameraSitsAt(Transform anchor)
    {
        if (!poseCaptured)
        {
            Debug.LogWarning("[MenuBackgroundLoader] Assign Menu Rig (and Menu Camera) to place the camera at the map's anchor.", this);
            return;
        }

        // Rotation that turns the camera's rest facing into the anchor's facing
        Quaternion delta = anchor.rotation * Quaternion.Inverse(camStartRot);

        menuRig.rotation = delta * rigStartRot;
        menuRig.position = anchor.position + delta * (rigStartPos - camStartPos);
    }
}