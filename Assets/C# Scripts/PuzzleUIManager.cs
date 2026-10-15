using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Audio;

public class PuzzleUIManager : MonoBehaviour
{
    [Header("Selection Highlighting")]
    public Color normalColor = Color.white;
    public Color confirmSelectedColor = Color.green; // Color when Confirm is selected
    public Color cancelSelectedColor = Color.red;   // Color when Cancel is selected

    [Header("Button Backgrounds")]
    public UnityEngine.UI.Image confirmButtonBackground; // Drag the Confirm button's Image here
    public UnityEngine.UI.Image cancelButtonBackground;   // Drag the Cancel button's Image here

    [Header("HUD / Camera UI")]
    public GameObject cameraUIPanel; // Drag your camera UI container/canvas here
    public static PuzzleUIManager Instance;

    [Header("Player Movement Control")]
    public MonoBehaviour playerMovementScript; // Drag your FirstPersonController here
    public MonoBehaviour mouseLookScript;

    [Header("UI References")]
    public GameObject overlayPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    
    // Split into two separate text components for clean label and bar alignment
    public TextMeshProUGUI statsLabelsText; 
    public TextMeshProUGUI statsBarsText; 

    public TextMeshProUGUI confirmButtonText;
    public TextMeshProUGUI cancelButtonText;
    public UnityEngine.UI.Image earphoneDisplayImage; // Drag your UI Image component here

    [HideInInspector] public int selectedIndex = 0; // 0 = Confirm, 1 = Cancel

    private EarphoneInspect currentInspectedItem;
    public bool isInspectionActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Forces the UI overlay to be hidden when the game starts
        if (overlayPanel != null) overlayPanel.SetActive(false);
    }

    private Color confirmOriginalColor;
    private Color cancelOriginalColor;
    private void Start()
    {
        // Grab the colors directly from the image components on startup
        if (confirmButtonBackground != null) confirmOriginalColor = confirmButtonBackground.color;
        if (cancelButtonBackground != null) cancelOriginalColor = cancelButtonBackground.color;
    }

    private void Update()
    {
        if (!isInspectionActive) return;

        // Rebindable left/right choice keys
        if (KeyBindings.GetKeyDown(GameAction.MenuLeft))
        {
            SetSelectedIndex(0); // Highlight Confirm (Left)
        }
        else if (KeyBindings.GetKeyDown(GameAction.MenuRight))
        {
            SetSelectedIndex(1); // Highlight Cancel (Right)
        }

        // Enter Key to Select
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (selectedIndex == 0)
            {
                ConfirmSelection();
            }
            else
            {
                CancelSelection();
            }
        }
    }
    [Header("Earphone Preview Audio")]
        public AudioSource previewSource;            // 2D source: Loop ON, Play On Awake OFF
        public float previewDelay = 1f;              // seconds after pickup before music starts
        public float previewFadeTime = 0.75f;
        [Range(0f, 1f)] public float levelMusicDuck = 0.1f;  // level music volume while previewing

        [Header("Optional: stat-based sound (Audio Mixer)")]
        public AudioMixer previewMixer;
        public string bassParam = "BassGain";
        public string midParam = "MidGain";
        public string trebleParam = "TrebleGain";
        public string distortionParam = "Distortion";

        private Coroutine previewRoutine;
        private float savedMusicVolume;
        private bool musicDucked;
    public void ShowOverlay(EarphoneData data, EarphoneInspect inspectItem)
    {
        currentInspectedItem = inspectItem;
        isInspectionActive = true;
        selectedIndex = 0;
        UpdateHighlight();
        

        if (overlayPanel != null) overlayPanel.SetActive(true);

        // Populate Text Data
        if (data != null)
        {
            if (nameText != null) nameText.text = data.earphoneName;
            if (descriptionText != null) descriptionText.text = data.description;

            // Populate clean labels without stretching
            if (statsLabelsText != null)
            {
                statsLabelsText.text = 
                    "Bass Score      :\n" +
                    "Midrange Score  :\n" +
                    "Treble Score    :\n" +
                    "Technical Score :";
            }

            // Populate visual bars separately
            if (statsBarsText != null)
            {
                statsBarsText.text = 
                    $"{GetVisualBar(data.bassBars)}\n" +
                    $"{GetVisualBar(data.midrangeBars)}\n" +
                    $"{GetVisualBar(data.trebleBars)}\n" +
                    $"{GetVisualBar(data.technicalPerformance)}";
            }

            // Populate 2D Sprite Image based on the current EarphoneData
            if (earphoneDisplayImage != null)
            {
                if (data.earphoneSprite != null)
                {
                    earphoneDisplayImage.gameObject.SetActive(true);
                    earphoneDisplayImage.sprite = data.earphoneSprite;
                }
                else
                {
                    earphoneDisplayImage.gameObject.SetActive(false); // Hide if no sprite is provided
                }
            }

            if (cameraUIPanel != null) cameraUIPanel.SetActive(false);
        }
        StartPreview(data);
        // Freeze Player Movement & Camera
        if (playerMovementScript != null) 
        {
            playerMovementScript.SendMessage("StopMovement", SendMessageOptions.DontRequireReceiver);
            playerMovementScript.enabled = false;
        }
        if (mouseLookScript != null) mouseLookScript.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private string GetVisualBar(int value)
    {
        int max = 10;
        int filled = Mathf.Clamp(value, 0, max);
        int empty = max - filled;
        
        return new string('■', filled) + new string('□', empty);
    }

    public void HideOverlay()
    {
        StopPreview();
        // Re-enable player movement & camera
        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (mouseLookScript != null) mouseLookScript.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (overlayPanel != null) overlayPanel.SetActive(false);
        
        currentInspectedItem = null; 
        isInspectionActive = false;

        // Only turn the camera HUD back on if the player's HUD setting is turned ON (1)
        if (cameraUIPanel != null)
        {
            bool showHudPref = PlayerPrefs.GetInt("ShowHUD", 1) == 1;
            cameraUIPanel.SetActive(showHudPref);
        }
    }

    public void SetSelectedIndex(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, 1);
        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        if (confirmButtonText != null) confirmButtonText.color = Color.white; 
        if (confirmButtonBackground != null)
        {
            confirmButtonBackground.color = (selectedIndex == 0) ? confirmOriginalColor : Color.black; 
        }

        if (cancelButtonText != null) cancelButtonText.color = Color.white; 
        if (cancelButtonBackground != null)
        {
            cancelButtonBackground.color = (selectedIndex == 1) ? cancelOriginalColor : Color.black; 
        }
    }

    public void ConfirmSelection()
    {
        Debug.Log("Confirmed selection!");
        if (currentInspectedItem != null)
        {
            currentInspectedItem.CompletePuzzle();
        }
        HideOverlay();
    }

    public void CancelSelection()
    {
        Debug.Log("Cancelled inspection.");
        if (currentInspectedItem != null)
        {
            currentInspectedItem.CancelInspection();
            currentInspectedItem = null; 
        }
        HideOverlay();
    }
    private AudioSource LevelMusic =>
    LevelMusicManager.Instance != null ? LevelMusicManager.Instance.musicSource : null;

    private void StartPreview(EarphoneData data)
    {
        if (previewSource == null || data == null || data.previewTrack == null) return;
        if (previewRoutine != null) StopCoroutine(previewRoutine);
        ApplyStatSound(data);
        previewRoutine = StartCoroutine(PreviewIn(data));
    }

    private IEnumerator PreviewIn(EarphoneData data)
    {
        yield return new WaitForSecondsRealtime(previewDelay);

        previewSource.clip = data.previewTrack;
        previewSource.time = Mathf.Clamp(data.previewStartTime, 0f, Mathf.Max(0f, data.previewTrack.length - 0.1f));
        previewSource.volume = 0f;
        previewSource.Play();

        AudioSource bg = LevelMusic;
        if (bg != null && !musicDucked) { savedMusicVolume = bg.volume; musicDucked = true; }

        float target = data.previewVolume * PlayerPrefs.GetFloat("MusicVolume", 1f);
        float dur = Mathf.Max(0.01f, previewFadeTime);
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = t / dur;
            previewSource.volume = Mathf.Lerp(0f, target, k);
            if (bg != null && musicDucked) bg.volume = Mathf.Lerp(savedMusicVolume, savedMusicVolume * levelMusicDuck, k);
            yield return null;
        }
        previewSource.volume = target;
        if (bg != null && musicDucked) bg.volume = savedMusicVolume * levelMusicDuck;
        previewRoutine = null;
    }
    private void StopPreview()
    {
        if (previewRoutine != null) { StopCoroutine(previewRoutine); previewRoutine = null; }
        bool playing = previewSource != null && previewSource.isPlaying;
        if (playing || musicDucked) previewRoutine = StartCoroutine(PreviewOut());
    }

    private IEnumerator PreviewOut()
    {
        AudioSource bg = LevelMusic;
        float startPrev = previewSource != null ? previewSource.volume : 0f;
        float startBg = bg != null ? bg.volume : 0f;
        float dur = Mathf.Max(0.01f, previewFadeTime);

        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = t / dur;
            if (previewSource != null) previewSource.volume = Mathf.Lerp(startPrev, 0f, k);
            if (bg != null && musicDucked) bg.volume = Mathf.Lerp(startBg, savedMusicVolume, k);
            yield return null;
        }
        if (previewSource != null) previewSource.Stop();
        if (bg != null && musicDucked) bg.volume = savedMusicVolume;
        musicDucked = false;
        previewRoutine = null;
    }

    private void ApplyStatSound(EarphoneData data)
    {
        if (previewMixer == null) return;   // optional feature
        previewMixer.SetFloat(bassParam,   BarsToGain(data.bassBars));
        previewMixer.SetFloat(midParam,    BarsToGain(data.midrangeBars));
        previewMixer.SetFloat(trebleParam, BarsToGain(data.trebleBars));
        // Low technical score = slightly rough sound
        previewMixer.SetFloat(distortionParam, Mathf.Lerp(0.2f, 0f, data.technicalPerformance / 10f));
    }

    private static float BarsToGain(int bars)
    {
        // 5 bars = neutral (1.0), 0 = band mostly cut, 10 = strong boost
        return bars <= 5 ? Mathf.Lerp(0.25f, 1f, bars / 5f)
                        : Mathf.Lerp(1f, 2.5f, (bars - 5) / 5f);
    }
}