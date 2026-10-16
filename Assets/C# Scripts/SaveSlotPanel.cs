using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SlimUI.ModernMenu;

/// <summary>
/// One panel with 4 slot buttons. Use it twice:
///  - Mode = Save: inside the Save Room menu (clicking a slot saves into it)
///  - Mode = Load: in the Main Menu (clicking a slot loads it)
/// </summary>
public class SaveSlotPanel : MonoBehaviour
{
    public enum Mode { Save, Load }

    [Header("Mode")]
    public Mode mode = Mode.Save;

    [Header("Slots (exactly 4, in order)")]
    public Button[] slotButtons = new Button[SaveSlotManager.SlotCount];
    [Tooltip("The TMP text inside each slot button, same order as the buttons")]
    public TMP_Text[] slotLabels = new TMP_Text[SaveSlotManager.SlotCount];

    [Header("Optional")]
    public TMP_Text statusText;

    [Header("Load mode only")]
    [Tooltip("Auto-finds if empty. Used so the loading screen shows.")]
    public UIMenuManager uiMenuManager;
    [Tooltip("Hidden while this panel is open (e.g. the main menu buttons). Must NOT contain this panel.")]
    public GameObject[] hideWhileOpen;

    [Header("Save mode only")]
    public UnityEvent onSaved;

    private int armedSlot = -1;
    private float armedUntil;
    private const float OverwriteWindow = 3f;

    private void Awake()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null) continue;
            int index = i; // capture a copy for the lambda
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(index));
        }
    }

    private void OnEnable()
    {
        armedSlot = -1;
        SetStatus("");
        Refresh();
    }

    private void Update()
    {
        // Escape closes the slot list in Load mode.
        // Inside the pause menu, PauseMenuManager already handles Escape, so skip it there
        // (otherwise one key press would close the list AND resume the game).
        if (mode != Mode.Load) return;
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (GetComponentInParent<PauseMenuManager>() != null) return;

        Close();
    }

    // ---- Load-mode open/close (hook these to the Load Save / Back buttons) ----

    public void Open()
    {
        foreach (GameObject go in hideWhileOpen)
            if (go != null) go.SetActive(false);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        foreach (GameObject go in hideWhileOpen)
            if (go != null) go.SetActive(true);
        gameObject.SetActive(false);
    }

    // ---- UI ----

    public void Refresh()
    {
        for (int i = 0; i < SaveSlotManager.SlotCount; i++)
        {
            bool filled = SaveSlotManager.TryRead(i, out SaveSlotData data);

            if (i < slotLabels.Length && slotLabels[i] != null)
                slotLabels[i].text = BuildLabel(i, filled ? data : null);

            // In Load mode an empty slot can't be clicked
            if (i < slotButtons.Length && slotButtons[i] != null)
                slotButtons[i].interactable = mode == Mode.Save || filled;
        }
    }

    private static string BuildLabel(int index, SaveSlotData d)
    {
        string head = "SLOT " + (index + 1);
        if (d == null) return head + "\nEmpty";

        string where = d.sceneName.Replace('_', ' ');
        string stage = d.hasCheckpoint && d.checkpointIndex > 0 ? "Checkpoint " + d.checkpointIndex : "Start of area";
        TimeSpan t = TimeSpan.FromSeconds(d.playTimeSeconds);
        string time = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)t.TotalHours, t.Minutes, t.Seconds);

        return head + "\n" + where + " - " + stage + "\n" + time + "    " + d.savedAt;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    // ---- Clicks ----

    private void OnSlotClicked(int slot)
    {
        if (mode == Mode.Save) HandleSave(slot);
        else HandleLoad(slot);
    }

    private void HandleSave(int slot)
    {
        // Overwriting needs a second click within a few seconds
        bool occupied = SaveSlotManager.TryRead(slot, out _);
        bool confirmed = armedSlot == slot && Time.unscaledTime < armedUntil;

        if (occupied && !confirmed)
        {
            armedSlot = slot;
            armedUntil = Time.unscaledTime + OverwriteWindow;
            SetStatus("Slot " + (slot + 1) + " already has a save. Click it again to overwrite.");
            return;
        }

        armedSlot = -1;

        if (SaveSlotManager.SaveToSlot(slot, out string error))
        {
            SetStatus("Saved to Slot " + (slot + 1) + ".");
            Refresh();
            onSaved?.Invoke();
        }
        else
        {
            SetStatus(error);
        }
    }

    private void HandleLoad(int slot)
    {
        if (!SaveSlotManager.PrepareLoad(slot, out string scene, out string error))
        {
            SetStatus(error);
            return;
        }

        if (uiMenuManager == null) uiMenuManager = FindFirstObjectByType<UIMenuManager>();

        if (uiMenuManager != null) uiMenuManager.LoadScene(scene);   // shows the loading screen
        else SceneManager.LoadScene(scene);
    }
}