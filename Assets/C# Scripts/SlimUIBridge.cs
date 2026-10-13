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
        string savedScene = CheckpointData.GetSavedSceneName();
        if (string.IsNullOrEmpty(savedScene) && loadGameButton != null)
        {
            loadGameButton.SetActive(false);
        }
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

        if (uiMenuManager != null)
        {
            uiMenuManager.LoadScene(defaultNewGameScene);
        }
        else
        {
            SceneManager.LoadScene(defaultNewGameScene);
        }
    }

    public void LoadLastSave()
    {
        Time.timeScale = 1.0f;

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