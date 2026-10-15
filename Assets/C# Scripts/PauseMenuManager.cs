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
    private void Start()
    {
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
        // Block the pause menu entirely if the earphone inspection UI is active
        if (PuzzleUIManager.Instance != null && PuzzleUIManager.Instance.isInspectionActive)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
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

    public void LoadCheckpoint()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadLatestSave()
    {
        Time.timeScale = 1f;

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