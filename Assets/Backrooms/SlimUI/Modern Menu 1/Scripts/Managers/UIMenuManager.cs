using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace SlimUI.ModernMenu{
    public class UIMenuManager : MonoBehaviour {
        // Clear previous progress and start a fresh game session
    public void StartNewGame(string gameSceneName)
    {
        // Wipe all BayatGames save files and progression keys
        CheckpointData.DeleteSavedData();
        ModernCameraHUD.SessionTime = 0f;

        // Load the starting level
        LoadScene(gameSceneName);
    }

        public void ResumeGame(string fallbackSceneName = "Level1")
        {
            CheckpointData.RestoreSavedTimer();
            string savedScene = CheckpointData.GetSavedSceneName();
            
            if (!string.IsNullOrEmpty(savedScene))
            {
                LoadScene(savedScene);
            }
            else
            {
                // Default scene to load if no saved checkpoint exists yet
                LoadScene(fallbackSceneName);
            }
        }
        private Animator CameraObject;

        // campaign button sub menu
        [Header("MENUS")]
        [Tooltip("The Menu for when the MAIN menu buttons")]
        public GameObject mainMenu;
        [Tooltip("THe first list of buttons")]
        public GameObject firstMenu;
        [Tooltip("The Menu for when the PLAY button is clicked")]
        public GameObject playMenu;
        [Tooltip("The Menu for when the EXIT button is clicked")]
        public GameObject exitMenu;
        [Tooltip("Optional 4th Menu")]
        public GameObject extrasMenu;

        public enum Theme {custom1, custom2, custom3};
        [Header("THEME SETTINGS")]
        public Theme theme;
        private int themeIndex;
        public ThemedUIData themeController;

        [Header("PANELS")]
        [Tooltip("The UI Panel parenting all main sub menus")]
        public GameObject mainCanvas;
        [Tooltip("The UI Panel for Settings/Options")]
        public GameObject optionsCanvas;
        [Tooltip("The UI Panel that holds the CONTROLS window tab")]
        public GameObject PanelControls;
        [Tooltip("The UI Panel that holds the VIDEO window tab")]
        public GameObject PanelVideo;
        [Tooltip("The UI Panel that holds the GAME window tab")]
        public GameObject PanelGame;
        [Tooltip("The UI Panel that holds the KEY BINDINGS window tab")]
        public GameObject PanelKeyBindings;
        [Tooltip("The UI Sub-Panel under KEY BINDINGS for MOVEMENT")]
        public GameObject PanelMovement;
        [Tooltip("The UI Sub-Panel under KEY BINDINGS for GENERAL")]
        public GameObject PanelGeneral;

        // highlights in settings screen
        [Header("SETTINGS SCREEN")]
        [Tooltip("Highlight Image for when GAME Tab is selected in Settings")]
        public GameObject lineGame;
        [Tooltip("Highlight Image for when VIDEO Tab is selected in Settings")]
        public GameObject lineVideo;
        [Tooltip("Highlight Image for when CONTROLS Tab is selected in Settings")]
        public GameObject lineControls;
        [Tooltip("Highlight Image for when KEY BINDINGS Tab is selected in Settings")]
        public GameObject lineKeyBindings;
        [Tooltip("Highlight Image for when MOVEMENT Sub-Tab is selected in KEY BINDINGS")]
        public GameObject lineMovement;
        [Tooltip("Highlight Image for when GENERAL Sub-Tab is selected in KEY BINDINGS")]
        public GameObject lineGeneral;

        [Header("LOADING SCREEN")]
        [Tooltip("If this is true, the loaded scene won't load until receiving user input")]
        public bool waitForInput = true;
        public GameObject loadingMenu;
        [Tooltip("The loading bar Slider UI element in the Loading Screen")]
        public Slider loadingBar;
        public TMP_Text loadPromptText;
        public KeyCode userPromptKey;

        [Header("SFX")]
        [Tooltip("The GameObject holding the Audio Source component for the HOVER SOUND")]
        public AudioSource hoverSound;
        [Tooltip("The GameObject holding the Audio Source component for the AUDIO SLIDER")]
        public AudioSource sliderSound;
        [Tooltip("The GameObject holding the Audio Source component for the SWOOSH SOUND when switching to the Settings Screen")]
        public AudioSource swooshSound;

        private void Update()
        {
            // Continuously ensure the cursor is free and visible while in the menu scene
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        void Start(){
            // Ensure the cursor is unlocked and visible for menu navigation
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            CameraObject = transform.GetComponent<Animator>();

            playMenu.SetActive(false);
            exitMenu.SetActive(false);
            if(extrasMenu) extrasMenu.SetActive(false);
            firstMenu.SetActive(true);
            mainMenu.SetActive(true);

            // Check if we arrived here specifically to open settings from the pause menu
            if (MenuTransitionData.openSettingsOnLoad)
            {
                MenuTransitionData.openSettingsOnLoad = false; // Reset flag
                Position2(); // Opens the options canvas and triggers the camera animation
            }
            else
            {
                // Normal start behavior: Hide Options Canvas
                if(optionsCanvas != null) optionsCanvas.SetActive(false);
                if(mainCanvas != null) mainCanvas.SetActive(true);
            }

            SetThemeColors();
        }

        void SetThemeColors()
        {
            switch (theme)
            {
                case Theme.custom1:
                    themeController.currentColor = themeController.custom1.graphic1;
                    themeController.textColor = themeController.custom1.text1;
                    themeIndex = 0;
                    break;
                case Theme.custom2:
                    themeController.currentColor = themeController.custom2.graphic2;
                    themeController.textColor = themeController.custom2.text2;
                    themeIndex = 1;
                    break;
                case Theme.custom3:
                    themeController.currentColor = themeController.custom3.graphic3;
                    themeController.textColor = themeController.custom3.text3;
                    themeIndex = 2;
                    break;
                default:
                    Debug.Log("Invalid theme selected.");
                    break;
            }
        }

        public void PlayCampaign(){
            exitMenu.SetActive(false);
            if(extrasMenu) extrasMenu.SetActive(false);
            playMenu.SetActive(true);
        }
        
        public void PlayCampaignMobile(){
            exitMenu.SetActive(false);
            if(extrasMenu) extrasMenu.SetActive(false);
            playMenu.SetActive(true);
            mainMenu.SetActive(false);
        }

        public void ReturnMenu(){
            playMenu.SetActive(false);
            if(extrasMenu) extrasMenu.SetActive(false);
            exitMenu.SetActive(false);
            mainMenu.SetActive(true);
        }

        public void LoadScene(string scene){
            if(scene != ""){
                StartCoroutine(LoadAsynchronously(scene));
            }
        }

        public void DisablePlayCampaign(){
            playMenu.SetActive(false);
        }

        // Called when clicking 'Settings'
        public void Position2(){
            // Ensure cursor is free when opening settings
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            DisablePlayCampaign();

            if(mainCanvas != null) mainCanvas.SetActive(false);
            if(optionsCanvas != null) optionsCanvas.SetActive(true);

            GamePanel();

            if(CameraObject != null && CameraObject.runtimeAnimatorController != null){
                CameraObject.SetFloat("Animate", 1);
            }
        }

        public void Position1(){
            // If we came from the pause menu during gameplay, return back to the game with state restored
            if (!string.IsNullOrEmpty(MenuTransitionData.previousSceneName))
            {
                string gameplayScene = MenuTransitionData.previousSceneName;
                MenuTransitionData.previousSceneName = ""; 
                
                // Tell the gameplay scene to open the pause menu upon loading
                MenuTransitionData.openPauseMenuOnLoad = true;

                LoadScene(gameplayScene);
                return;
            }

            // Normal Main Menu behavior: Just close options and show main canvas[cite: 10]
            if(optionsCanvas != null) optionsCanvas.SetActive(false);
            if(mainCanvas != null) mainCanvas.SetActive(true);

            if(CameraObject != null && CameraObject.runtimeAnimatorController != null){
                CameraObject.SetFloat("Animate", 0);
            }
        }

        // Null-safe helpers so an unassigned (or deleted) panel/line never throws
        static void Show(GameObject go){ if(go != null) go.SetActive(true); }
        static void Hide(GameObject go){ if(go != null) go.SetActive(false); }

        void DisablePanels(){
            Hide(PanelControls);
            Hide(PanelVideo);
            Hide(PanelGame);
            Hide(PanelKeyBindings);

            Hide(lineGame);
            Hide(lineControls);
            Hide(lineVideo);
            Hide(lineKeyBindings);

            Hide(PanelMovement);
            Hide(lineMovement);
            Hide(PanelGeneral);
            Hide(lineGeneral);
        }

        public void GamePanel(){
            DisablePanels();
            Show(PanelGame);
            Show(lineGame);
        }

        public void VideoPanel(){
            DisablePanels();
            Show(PanelVideo);
            Show(lineVideo);
        }

        public void ControlsPanel(){
            DisablePanels();
            Show(PanelControls);
            Show(lineControls);
        }

        public void KeyBindingsPanel(){
            DisablePanels();
            MovementPanel();
            Show(PanelKeyBindings);
            Show(lineKeyBindings);
        }

        public void MovementPanel(){
            DisablePanels();
            Show(PanelKeyBindings);
            Show(PanelMovement);
            Show(lineMovement);
        }

        public void GeneralPanel(){
            DisablePanels();
            Show(PanelKeyBindings);
            Show(PanelGeneral);
            Show(lineGeneral);
        }

        public void PlayHover(){
            if(hoverSound != null) hoverSound.Play();
        }

        public void PlaySFXHover(){
            if(sliderSound != null) sliderSound.Play();
        }

        public void PlaySwoosh(){
            if(swooshSound != null) swooshSound.Play();
        }

        public void AreYouSure(){
            exitMenu.SetActive(true);
            if(extrasMenu) extrasMenu.SetActive(false);
            DisablePlayCampaign();
        }

        public void AreYouSureMobile(){
            exitMenu.SetActive(true);
            if(extrasMenu) extrasMenu.SetActive(false);
            mainMenu.SetActive(false);
            DisablePlayCampaign();
        }

        public void ExtrasMenu(){
            playMenu.SetActive(false);
            if(extrasMenu) extrasMenu.SetActive(true);
            exitMenu.SetActive(false);
        }

        public void QuitGame(){
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        IEnumerator LoadAsynchronously(string sceneName){
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;
            if(mainCanvas != null) mainCanvas.SetActive(false);
            if(optionsCanvas != null) optionsCanvas.SetActive(false);
            loadingMenu.SetActive(true);

            while (!operation.isDone){
                float progress = Mathf.Clamp01(operation.progress / .95f);
                loadingBar.value = progress;

                if (operation.progress >= 0.9f && waitForInput){
                    loadPromptText.text = "Press " + userPromptKey.ToString().ToUpper() + " to continue";
                    loadingBar.value = 1;

                    if (Input.GetKeyDown(userPromptKey)){
                        operation.allowSceneActivation = true;
                    }
                }else if(operation.progress >= 0.9f && !waitForInput){
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }
        }
    }
}