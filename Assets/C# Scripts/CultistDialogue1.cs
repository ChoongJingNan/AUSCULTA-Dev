using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CultistDialogue1 : MonoBehaviour
{
    [Header("NPC Info")]
    public string npcName = "Cultist #1";
    public TMP_Text npcNameText;

    [Header("Manager References")]
    public EncounterManager encounterManager;
    public Transform playerTransform;
    public ModernCameraHUD cameraHUD;
    public float interactionDistance = 3.5f;

    [Header("Level Music Settings")]
    [Tooltip("Volume level background music drops to while talking to this NPC")]
    [Range(0f, 1f)] public float dialogueMusicVolume = 0.15f;
    [Tooltip("Volume level background music restores to when dialogue ends")]
    [Range(0f, 1f)] public float normalMusicVolume = 0.5f;

    [Header("Camera Look Settings")]
    [Tooltip("Height offset so the camera looks at the sitting NPC's head/face instead of their floor pivot")]
    public float lookHeightOffset = 1.2f;

    [Header("Typewriter Settings")]
    public float typingSpeed = 0.03f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip greetingVoice;
    public AudioClip option1Voice;
    public AudioClip option2Voice;
    public AudioClip option3Voice;

    [Header("UI Panels")]
    public GameObject dialoguePanel;
    public TMP_Text npcDialogueText;

    [Header("Choice Buttons")]
    public Button option1Button;
    public Button option2Button;
    public Button option3Button;

    [Header("Button Labels")]
    public TMP_Text option1Text;
    public TMP_Text option2Text;
    public TMP_Text option3Text;

    private bool isDialogueOpen = false;
    private bool isPlayerNearby = false;
    
    // State Tracking
    private bool isTyping = false;
    private bool isWaitingForEnter = false;
    private bool isShowingOptions = false;
    private bool isInitialGreeting = false;

    private string currentFullText = "";
    private Coroutine typingCoroutine;

    // Mouse Look Script Reference
    private MonoBehaviour playerMouseLookScript;

    private void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (cameraHUD == null)
        {
            cameraHUD = Object.FindFirstObjectByType<ModernCameraHUD>();
        }

        // Cache player's mouse look script
        if (playerTransform != null)
        {
            foreach (var mb in playerTransform.GetComponentsInChildren<MonoBehaviour>())
            {
                string scriptName = mb.GetType().Name.ToLower();
                if (scriptName.Contains("look") || scriptName.Contains("mouselook"))
                {
                    playerMouseLookScript = mb;
                    break;
                }
            }
        }

        // Disable mouse interaction on choice buttons (Keybinds only)
        ConfigureButtonForKeybindOnly(option1Button);
        ConfigureButtonForKeybindOnly(option2Button);
        ConfigureButtonForKeybindOnly(option3Button);

        // Labels setup
        if (option1Text != null) option1Text.text = "[1] Where am I?";
        if (option2Text != null) option2Text.text = "[2] Who are you?";
        if (option3Text != null) option3Text.text = "[3] What was that thing?";
    }

    private void ConfigureButtonForKeybindOnly(Button button)
    {
        if (button != null)
        {
            button.interactable = false; // Prevents mouse clicks and hover highlighting
            if (button.TryGetComponent<Image>(out var img))
            {
                img.raycastTarget = false; // Disables UI raycasting entirely
            }
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNearby = distance <= interactionDistance;

        // Open Dialogue
        if (isPlayerNearby && !isDialogueOpen)
        {
            if (KeyBindings.GetKeyDown(GameAction.Interact))
            {
                OpenDialogue();
                return;
            }
        }
        else if (isDialogueOpen)
        {
            // Close if player moves out of range
            if (distance > interactionDistance)
            {
                CloseDialogue();
                return;
            }

            // Smoothly look at the NPC's head position
            Vector3 targetPosition = transform.position + (Vector3.up * lookHeightOffset);
            Vector3 direction = (targetPosition - playerTransform.position).normalized;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, Time.deltaTime * 12f);
            }

            // 1. Fast forward typewriter effect without stopping NPC audio
            if (isTyping && KeyBindings.GetKeyDown(GameAction.Interact))
            {
                FinishTypingInstantly();
                return;
            }

            // 2. Transition from initial greeting to response choices
            if (isWaitingForEnter && KeyBindings.GetKeyDown(GameAction.Interact))
            {
                isWaitingForEnter = false;
                ShowOptions();
                return;
            }

            // 3. Keybind choices (Keyboard triggers only)
            if (isShowingOptions)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SelectOption1();
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SelectOption2();
                else if (option3Button != null && option3Button.gameObject.activeSelf && (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))) SelectOption3();
            }

            // Exit dialogue via Escape key
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseDialogue();
            }
        }
    }

    public void OpenDialogue()
    {
        isDialogueOpen = true;
        isWaitingForEnter = false;
        isShowingOptions = false;
        isInitialGreeting = true;

        LevelMusicManager.Instance?.SetVolume(dialogueMusicVolume);

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        if (npcNameText != null) npcNameText.text = npcName;

        // Hide Camera HUD
        if (cameraHUD != null) cameraHUD.SetHUDActive(false);

        // Keep cursor locked, disable free mouse look script
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerMouseLookScript != null) playerMouseLookScript.enabled = false;

        string greetingText = "O? Welcome, o' lost lamb. Lo, this sacred refuge standeth as witness, for our faith burneth eternal, and our words have been hearkened unto in the unseen realm. Wander forth, if thou must, for thy convictions shall be weighed and thy beliefs laid bare. May fortune attend thee, lost lamb.";

        StartSpeech(greetingText, greetingVoice);
    }

    private void StartSpeech(string speechText, AudioClip voiceClip)
    {
        HideOptionButtons();
        isShowingOptions = false;
        isWaitingForEnter = false;

        // Stop previous voice track if choosing a new option
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeTextRoutine(speechText, voiceClip));
    }

    private IEnumerator TypeTextRoutine(string textToType, AudioClip voiceClip)
    {
        isTyping = true;
        currentFullText = textToType;
        npcDialogueText.text = "";

        if (audioSource != null && voiceClip != null)
        {
            audioSource.clip = voiceClip;
            audioSource.Play();
        }

        foreach (char letter in textToType.ToCharArray())
        {
            npcDialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        FinishTypingInstantly();
    }

    private void FinishTypingInstantly()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        // NOTE: audioSource.Stop() intentionally removed so audio continues playing
        isTyping = false;
        npcDialogueText.text = currentFullText;

        if (isInitialGreeting)
        {
            isWaitingForEnter = true;
        }
        else
        {
            isWaitingForEnter = false;
            ShowOptions();
        }
    }

    public void ShowOptions()
    {
        isShowingOptions = true;
        isInitialGreeting = false;

        if (option1Button != null) option1Button.gameObject.SetActive(true);
        if (option2Button != null) option2Button.gameObject.SetActive(true);

        bool entityEncountered = (encounterManager != null && encounterManager.introStarted);
        if (option3Button != null) option3Button.gameObject.SetActive(entityEncountered);
    }

    private void SelectOption1()
    {
        string answer = "Thou standest within the threshold of trial. A sacred expanse where fleeting shadows strip away all deceit, and thy very faith shall be weighed.";
        StartSpeech(answer, option1Voice);
    }

    private void SelectOption2()
    {
        string answer = "I am but a humble follower of His divine will, waiting upon the quiet grace of our Lord, who shall soon deliver holy judgment unto this realm.";
        StartSpeech(answer, option2Voice);
    }

    private void SelectOption3()
    {
        string answer = "Thou speakest in riddles, wanderer. Naught walketh these hallowed halls save thee and I. Perchance thy mind wavereth in the dark?";
        StartSpeech(answer, option3Voice);
    }

    private void HideOptionButtons()
    {
        if (option1Button != null) option1Button.gameObject.SetActive(false);
        if (option2Button != null) option2Button.gameObject.SetActive(false);
        if (option3Button != null) option3Button.gameObject.SetActive(false);
    }

public void CloseDialogue()
{
    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
    
    if (audioSource != null && audioSource.isPlaying)
    {
        audioSource.Stop();
    }

    isTyping = false;
    isWaitingForEnter = false;
    isShowingOptions = false;
    isDialogueOpen = false;
    isInitialGreeting = false;

    if (dialoguePanel != null) dialoguePanel.SetActive(false);

    // Re-enable Camera HUD when exiting dialogue
    if (cameraHUD != null) cameraHUD.SetHUDActive(true);

    if (playerMouseLookScript != null) playerMouseLookScript.enabled = true;

    // --- FIX HERE: Re-lock and hide the cursor when exiting via Escape ---
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;

    LevelMusicManager.Instance?.SetVolume(normalMusicVolume);
    }
}