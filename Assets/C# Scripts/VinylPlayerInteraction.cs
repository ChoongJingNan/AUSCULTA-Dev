using System.Collections;
using UnityEngine;

/// <summary>
/// The save station. Press Interact near it: the screen dims, the record comes out and
/// settles on the player, then only the spin loops. Closing the menu lifts the record
/// off and puts it back (the intro played in reverse).
/// </summary>
public class VinylPlayerInteraction : MonoBehaviour
{
    // ---- Lets the pause menu ignore Escape while this menu is open ----
    public static bool IsMenuOpen { get; private set; }
    private static int lastCloseFrame = -1;
    public static bool BlocksPause => IsMenuOpen || Time.frameCount == lastCloseFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { IsMenuOpen = false; lastCloseFrame = -1; }

    [Header("Interaction")]
    public float interactionDistance = 3.0f;

    [Header("Save Menu")]
    [Tooltip("Root of the save menu (dim background + slots). Needs a CanvasGroup.")]
    public CanvasGroup saveMenuGroup;
    public SaveSlotPanel slotPanel;
    public float menuFadeTime = 0.35f;
    [Tooltip("Optional: the camera HUD, hidden while the menu is open")]
    public GameObject cameraHud;

    [Header("Animation")]
    [Tooltip("Optional. Leave empty to use the clip found in the Animator's controller. The clip is played by code; the Animator itself is never run.")]
    public AnimationClip clip;
    [Tooltip("No longer used (kept so existing scenes don't lose the value)")]
    public string stateName = "Playing";
    [Tooltip("Seconds into the clip where the record has landed and starts spinning")]
    public float spinStartTime = 0f;
    [Tooltip("Seconds where one spin cycle ends. 0 = end of the clip")]
    public float spinEndTime = 0f;
    [Tooltip("On = finish the current spin cycle before lifting the record. Off = lift immediately.")]
    public bool finishSpinBeforeLifting = true;
    [Tooltip("Speed of the reverse (record going back into the sleeve). 1 = same speed as the intro.")]
    public float outroSpeed = 1f;

    [Header("Audio (Optional)")]
    public AudioSource recordMusic;
    public float musicFadeTime = 0.6f;

    private enum AnimState { Idle, Intro, Spinning, Outro }
    private AnimState animState = AnimState.Idle;
    private bool closeRequested;

    private Transform playerTransform;
    private Animator animator;
    private GameObject animRoot;
    private FirstPersonController fpc;
    private Coroutine menuRoutine;
    private Coroutine musicRoutine;
    private float musicBaseVolume = 1f;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animRoot = animator.gameObject;

            // The clip is sampled directly from code (see ApplyClipTime), so the Animator must never run
            animator.enabled = false;

            if (clip == null) clip = FindClip(animator);
        }

        if (clip != null && animRoot != null)
        {
            clipLength = Mathf.Max(0.01f, clip.length);
            ApplyClipTime(0f); // show the record in its sleeve
        }
        else
        {
            Debug.LogError("[VinylPlayer] No animation clip found. Assign one in the 'Clip' field, or make sure the Animator has a controller with the clip in it.", this);
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;

        if (recordMusic != null)
        {
            musicBaseVolume = recordMusic.volume;
            recordMusic.loop = true;
            recordMusic.Stop(); // in case 'Play On Awake' is ticked: music should only play while the record spins
        }

        if (saveMenuGroup != null)
        {
            saveMenuGroup.alpha = 0f;
            saveMenuGroup.interactable = false;
            saveMenuGroup.blocksRaycasts = false;
            saveMenuGroup.gameObject.SetActive(false);
        }

        if (spinStartTime <= 0f)
            Debug.LogWarning("[VinylPlayer] Spin Start Time is 0, so the whole clip will loop. Set it to the second where the record has landed.", this);
        else if (clip != null && spinStartTime >= clipLength)
            Debug.LogWarning($"[VinylPlayer] Spin Start Time ({spinStartTime}s) is past the end of the clip ({clipLength:0.00}s).", this);
    }

    private static AnimationClip FindClip(Animator a)
    {
        if (a.runtimeAnimatorController == null) return null;

        AnimationClip best = null;
        foreach (AnimationClip c in a.runtimeAnimatorController.animationClips)
            if (c != null && (best == null || c.length > best.length)) best = c;
        return best;
    }

    private void OnDisable()
    {
        if (IsMenuOpen) IsMenuOpen = false;
    }

    private void Update()
    {
        if (IsMenuOpen)
        {
            // Keep the cursor free while the menu is up
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (Input.GetKeyDown(KeyCode.Escape)) CloseMenu();
        }
        else if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= interactionDistance && KeyBindings.GetKeyDown(GameAction.Interact))
            {
                OpenMenu();
            }
        }

        UpdateAnimation();
    }

    // ------------------------------------------------------------------
    // Menu
    // ------------------------------------------------------------------

    private void OpenMenu()
    {
        IsMenuOpen = true;
        closeRequested = false;

        FreezePlayer(true);
        if (cameraHud != null) cameraHud.SetActive(false);

        if (animState == AnimState.Idle)
        {
            BeginIntro();
        }
        else if (animState == AnimState.Outro)
        {
            // Reopened while the record was on its way back: bring it out again from where it is
            animState = AnimState.Intro;
        }
        // Intro / Spinning: closeRequested is now false, so it simply carries on

        if (menuRoutine != null) StopCoroutine(menuRoutine);
        menuRoutine = StartCoroutine(FadeMenu(true));
    }

    /// <summary>Hook this to the menu's Close button. Escape also calls it.</summary>
    public void CloseMenu()
    {
        if (!IsMenuOpen) return;

        IsMenuOpen = false;
        lastCloseFrame = Time.frameCount;
        closeRequested = true;

        FreezePlayer(false);
        if (cameraHud != null) cameraHud.SetActive(PlayerPrefs.GetInt("ShowHUD", 1) == 1);

        if (menuRoutine != null) StopCoroutine(menuRoutine);
        menuRoutine = StartCoroutine(FadeMenu(false));
    }

    private void FreezePlayer(bool freeze)
    {
        if (fpc == null) fpc = FindFirstObjectByType<FirstPersonController>();

        if (fpc != null)
        {
            if (freeze)
            {
                Rigidbody rb = fpc.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }
            fpc.enabled = !freeze;
        }

        Cursor.lockState = freeze ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = freeze;
    }

    private IEnumerator FadeMenu(bool show)
    {
        if (saveMenuGroup == null) yield break;

        if (show) saveMenuGroup.gameObject.SetActive(true);
        saveMenuGroup.interactable = show;
        saveMenuGroup.blocksRaycasts = show;

        float start = saveMenuGroup.alpha;
        float end = show ? 1f : 0f;
        float duration = Mathf.Max(0.01f, menuFadeTime);

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            saveMenuGroup.alpha = Mathf.Lerp(start, end, t / duration);
            yield return null;
        }

        saveMenuGroup.alpha = end;
        if (!show) saveMenuGroup.gameObject.SetActive(false);
        menuRoutine = null;
    }

    // ------------------------------------------------------------------
    // Animation: intro once -> spin loop -> intro played backwards
    //
    // The clip time is set directly from code every frame (Animator speed stays 0),
    // so the reverse never depends on negative Animator speed or on reading the
    // Animator's state back, and it cannot get stuck.
    // ------------------------------------------------------------------

    private float clipTime;
    private float clipLength = 1f;

    private bool HasClip => clip != null && animRoot != null;

    /// <summary>Poses the model exactly as the clip looks at this time (in seconds).</summary>
    private void ApplyClipTime(float seconds)
    {
        clipTime = seconds;
        if (HasClip) clip.SampleAnimation(animRoot, Mathf.Clamp(seconds, 0f, clipLength));
    }

    private void BeginIntro()
    {
        if (!HasClip)
        {
            animState = AnimState.Spinning;
            StartMusic();
            return;
        }

        Debug.Log($"[VinylPlayer] Opening: clip '{clip.name}' is {clipLength:0.00}s, spin starts at {spinStartTime:0.00}s", this);
        ApplyClipTime(0f);
        animState = AnimState.Intro;
    }

    private void UpdateAnimation()
    {
        if (animState == AnimState.Idle) return;

        // No clip: nothing to play, just finish when the menu closes
        if (!HasClip)
        {
            if (closeRequested) { StopMusic(); animState = AnimState.Idle; closeRequested = false; }
            return;
        }

        float dt = Time.unscaledDeltaTime;
        float spinStart = Mathf.Clamp(spinStartTime, 0f, clipLength);
        float spinEnd = (spinEndTime > spinStart && spinEndTime <= clipLength) ? spinEndTime : clipLength;

        switch (animState)
        {
            case AnimState.Intro:
                if (closeRequested)
                {
                    // Closed before the record landed: run the intro backwards from here
                    animState = AnimState.Outro;
                    break;
                }

                clipTime += dt;
                if (clipTime >= spinStart)
                {
                    clipTime = spinStart;
                    animState = AnimState.Spinning;
                    StartMusic();
                }
                ApplyClipTime(clipTime);
                break;

            case AnimState.Spinning:
                if (closeRequested && !finishSpinBeforeLifting)
                {
                    BeginOutro(spinStart);
                    break;
                }

                clipTime += dt;
                if (clipTime >= spinEnd)
                {
                    if (closeRequested)
                    {
                        BeginOutro(spinStart); // same pose as the loop point, so it looks seamless
                        break;
                    }

                    // Loop: back to where the spin starts
                    clipTime = spinStart + (clipTime - spinEnd);
                    if (clipTime >= spinEnd) clipTime = spinStart;
                }
                ApplyClipTime(clipTime);
                break;

            case AnimState.Outro:
                clipTime -= dt * Mathf.Max(0.05f, outroSpeed);
                if (clipTime <= 0f)
                {
                    EndOutro();
                    break;
                }
                ApplyClipTime(clipTime);
                break;
        }
    }

    private void BeginOutro(float fromTime)
    {
        ApplyClipTime(fromTime);
        animState = AnimState.Outro;
        StopMusic();
    }

    private void EndOutro()
    {
        // Back in the sleeve: first frame of the clip
        ApplyClipTime(0f);
        animState = AnimState.Idle;
        closeRequested = false;
        Debug.Log("[VinylPlayer] Record is back in the sleeve. Ready to open again.", this);
    }

    // ------------------------------------------------------------------
    // Music
    // ------------------------------------------------------------------

    private void StartMusic()
    {
        if (recordMusic == null) return;
        if (musicRoutine != null) { StopCoroutine(musicRoutine); musicRoutine = null; }
        recordMusic.volume = musicBaseVolume;
        if (!recordMusic.isPlaying) recordMusic.Play();
    }

    private void StopMusic()
    {
        if (recordMusic == null || !recordMusic.isPlaying) return;
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(FadeOutMusic());
    }

    private IEnumerator FadeOutMusic()
    {
        float start = recordMusic.volume;
        float duration = Mathf.Max(0.01f, musicFadeTime);
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            recordMusic.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        recordMusic.Stop();
        recordMusic.volume = musicBaseVolume;
        musicRoutine = null;
    }
}