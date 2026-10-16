using UnityEngine;
using UnityEngine.SceneManagement;
using SlimUI.ModernMenu; // Added SlimUI namespace

public class SlimUIBridge : MonoBehaviour
{
    [Header("Default New Game Scene")]
    public string defaultNewGameScene = "R1_BackroomsBuild";

    [Header("UI References")]
    [Tooltip("Drag the UIMenuManager component object here (optional, auto-finds if left empty)")]
    public UIMenuManager uiMenuManager;

    [Tooltip("Drag SlimUI's 'Continue' or 'Load' button here to disable it if no save exists")]
    public GameObject loadGameButton;

    [Tooltip("A SaveSlotPanel set to Mode = Load. The Load Game button should call OpenLoadMenu().")]
    public SaveSlotPanel loadSlotPanel;

    private void Awake()
    {
        UnlockCursor();
    }

    private void Start()
    {
        UnlockCursor();

        if (uiMenuManager == null)
        {
            uiMenuManager = FindFirstObjectByType<UIMenuManager>();
        }

        // Disable Continue button if no save exists
        if (!SaveSlotManager.AnySlotExists() && loadGameButton != null)
        loadGameButton.SetActive(false);
    }

    private void Update()
    {
        // Keep mouse visible while in main menu
        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
        {
            UnlockCursor();
        }
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartNewGame()
    {
        Time.timeScale = 1.0f;
        CheckpointData.DeleteSavedData();
        ModernCameraHUD.SessionTime = 0f;

        if (uiMenuManager != null)
        {
            uiMenuManager.LoadScene(defaultNewGameScene);
        }
        else
        {
            SceneManager.LoadScene(defaultNewGameScene);
        }
    }

    // Hook the main menu's "Load Game" button to this
    public void OpenLoadMenu()
    {
        if (loadSlotPanel != null) loadSlotPanel.Open();
        else LoadLastSave();
    }

    public void LoadLastSave()
    {
        Time.timeScale = 1.0f;
        CheckpointData.RestoreSavedTimer();
        string lastSavedScene = CheckpointData.GetSavedSceneName();
        if (string.IsNullOrEmpty(lastSavedScene))
        {
            lastSavedScene = defaultNewGameScene;
        }

        if (uiMenuManager != null)
        {
            uiMenuManager.LoadScene(lastSavedScene);
        }
        else
        {
            SceneManager.LoadScene(lastSavedScene);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}