using UnityEngine;
using UnityEngine.SceneManagement;
using SlimUI.ModernMenu;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Scene Navigation")]
    [SerializeField] private string menuSceneName = "Main_Menu"; // Scene used for SlimUI Settings
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // Scene used when exiting to main menu entirely
    [SerializeField] private GameObject playerObject; // Drag your Player object here in the Inspector

    [Header("UI Panels & HUD")]
    [SerializeField] private GameObject pauseRootPanel; // The main pause overlay container
    [SerializeField] private ModernCameraHUD cameraHUD; // Drag your CameraHUD GameObject here

    [Header("SlimUI Integration")]
    [Tooltip("Reference to SlimUI's menu manager if you are using its built-in panel switching for Options/Controls")]
    [SerializeField] private UIMenuManager uiMenuManager;

    [Header("Load Game (save slots)")]
    [Tooltip("A SaveSlotPanel set to Mode = Load, placed inside the pause menu canvas. Leave empty to disable.")]
    [SerializeField] private SaveSlotPanel loadSlotPanel;

    [Header("Player Controllers to Freeze")]
    [SerializeField] private MonoBehaviour playerMovementScript; // Drag your FirstPersonController here
    [SerializeField] private MonoBehaviour mouseLookScript;      // Drag your MouseLook here

    [Header("Entity / Enemy to Freeze")]
    [SerializeField] private MonoBehaviour entityChaseScript;   // Drag your EntityChase01 script here

    private bool isPaused = false;

    private static bool cachedHasSavedPosition = false;
    private static Vector3 cachedSavedPosition;
    private static bool cachedOpenPauseMenu = false;

    private void Awake()
    {
        if (MenuTransitionData.hasSavedPosition)
        {
            cachedHasSavedPosition = true;
            cachedSavedPosition = MenuTransitionData.savedPlayerPosition;
        }

        if (MenuTransitionData.openPauseMenuOnLoad)
        {
            cachedOpenPauseMenu = true;
            MenuTransitionData.openPauseMenuOnLoad = false;

            // We are coming back from Settings: tell scene-start fades (FaceTransition) to skip themselves
            MenuTransitionData.skipSceneFadeIn = true;
        }

        if (playerObject == null)
        {
            GameObject foundPlayer = GameObject.FindWithTag("Player");
            if (foundPlayer != null)
            {
                playerObject = foundPlayer;
            }
        }

        if (cachedHasSavedPosition)
        {
            if (playerObject != null)
            {
                Debug.Log($"[PauseMenuManager] SUCCESS: Teleporting player to {cachedSavedPosition}");

                CharacterController cc = playerObject.GetComponent<CharacterController>();
                Rigidbody rb = playerObject.GetComponent<Rigidbody>();
                
                if (cc != null) cc.enabled = false;
                if (rb != null) rb.isKinematic = true;

                playerObject.transform.position = cachedSavedPosition;

                if (cc != null) cc.enabled = true;
                if (rb != null) rb.isKinematic = false;
            }
            cachedHasSavedPosition = false;
        }
    }
    private System.Collections.IEnumerator ClearSkipFadeFlagNextFrame()
    {
        // By the next frame every Start() in the scene has run, so the flag has been used
        yield return null;
        MenuTransitionData.skipSceneFadeIn = false;
    }

    private void Start()
    {
        StartCoroutine(ClearSkipFadeFlagNextFrame());

        if (uiMenuManager == null)
        {
            uiMenuManager = FindFirstObjectByType<UIMenuManager>();
        }

        if (cachedOpenPauseMenu)
        {
            cachedOpenPauseMenu = false;
            PauseGame(); 
        }
        else
        {
            if (pauseRootPanel != null)
            {
                pauseRootPanel.SetActive(false);
            }
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        // Block the pause menu while the save (vinyl) menu is open, and on the frame it closes.
        // This must come BEFORE the Escape check, otherwise Escape closes the save menu AND pauses the game.
        if (VinylPlayerInteraction.BlocksPause) return;

        // Block the pause menu entirely if the earphone inspection UI is active
        if (PuzzleUIManager.Instance != null && PuzzleUIManager.Instance.isInspectionActive)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Escape closes the load-slot list first instead of resuming the game
            if (isPaused && loadSlotPanel != null && loadSlotPanel.gameObject.activeSelf)
            {
                CloseLoadMenu();
                return;
            }

            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseRootPanel != null) pauseRootPanel.SetActive(true);

        // Completely hide and pause the camera HUD visuals
        if (cameraHUD != null)
        {
            cameraHUD.isPaused = true;
            cameraHUD.gameObject.SetActive(false); //[cite: 7]
        }

        Time.timeScale = 0f;

        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (mouseLookScript != null) mouseLookScript.enabled = false;
        if (entityChaseScript != null) entityChaseScript.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        // Make sure the slot list is closed so the pause buttons are back next time
        if (loadSlotPanel != null && loadSlotPanel.gameObject.activeSelf) loadSlotPanel.Close();

        isPaused = false;
        if (pauseRootPanel != null) pauseRootPanel.SetActive(false);

        // Re-enable the camera HUD if player settings allow it
        if (cameraHUD != null)
        {
            cameraHUD.isPaused = false;
            bool showHudPref = PlayerPrefs.GetInt("ShowHUD", 1) == 1;
            cameraHUD.gameObject.SetActive(showHudPref); //[cite: 7, 8]
        }

        Time.timeScale = 1f;

        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (mouseLookScript != null) mouseLookScript.enabled = true;
        if (entityChaseScript != null) entityChaseScript.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        MenuTransitionData.hasSavedPosition = false;
        MenuTransitionData.openPauseMenuOnLoad = false;
    }

    // Called when clicking 'Settings' from the Pause Menu
    public void OpenSettingsFromPause()
    {
        Time.timeScale = 1f;

        // Ensure the camera HUD is turned off before loading the settings scene
        if (cameraHUD != null)
        {
            cameraHUD.gameObject.SetActive(false); //[cite: 7]
        }

        MenuTransitionData.openSettingsOnLoad = true;
        MenuTransitionData.openPauseMenuOnLoad = true; 
        MenuTransitionData.previousSceneName = SceneManager.GetActiveScene().name; 

        if (playerObject != null)
        {
            MenuTransitionData.savedPlayerPosition = playerObject.transform.position;
            MenuTransitionData.hasSavedPosition = true;
        }

        SceneManager.LoadScene(menuSceneName);
    }

    // Hook the pause menu's "Load Game" button to this
    public void OpenLoadMenu()
    {
        if (loadSlotPanel == null)
        {
            Debug.LogWarning("[PauseMenuManager] No Load Slot Panel assigned, falling back to the latest save.");
            LoadLatestSave();
            return;
        }
        loadSlotPanel.Open();
    }

    // Hook the slot panel's Back button to this (or to the panel's own Close())
    public void CloseLoadMenu()
    {
        if (loadSlotPanel != null) loadSlotPanel.Close();
    }

    public void LoadCheckpoint()
    {
        Time.timeScale = 1f;
        CheckpointData.RestoreSavedTimer();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadLatestSave()
    {
        Time.timeScale = 1f;
        CheckpointData.RestoreSavedTimer();
        string savedScene = CheckpointData.GetSavedSceneName();
        if (!string.IsNullOrEmpty(savedScene))
        {
            SceneManager.LoadScene(savedScene);
        }
        else
        {
            Debug.LogWarning("No save file found, reloading current scene instead.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}