using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Put this on each rebind button in the Key Bindings panel.
/// Pick the action and slot (0 = primary, 1 = secondary) in the Inspector.
/// Click it, then press the key you want. Escape cancels, Delete clears a secondary slot.
/// </summary>
[RequireComponent(typeof(Button))]
public class KeybindButton : MonoBehaviour
{
    [Header("Binding")]
    public GameAction action;
    [Tooltip("0 = primary, 1 = secondary")]
    [Range(0, 1)] public int slot = 0;

    [Header("UI")]
    [Tooltip("Text that shows the current key. Auto-finds a TMP_Text child if left empty.")]
    public TMP_Text label;
    public string listeningText = "Press a key...";

    private Button button;
    private bool listening;
    private string errorMessage;
    private float errorUntil;

    private static KeybindButton active;
    private static readonly KeyCode[] scanKeys = BuildScanList();

    private static KeyCode[] BuildScanList()
    {
        var list = new System.Collections.Generic.List<KeyCode>();
        foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
        {
            // Skip None and joystick buttons; keyboard + mouse only
            if (k == KeyCode.None || k >= KeyCode.JoystickButton0) continue;
            list.Add(k);
        }
        return list.ToArray();
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        button.onClick.AddListener(BeginListening);
    }

    private void OnEnable()
    {
        KeyBindings.OnBindingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        KeyBindings.OnBindingsChanged -= Refresh;
        if (active == this) active = null;
        listening = false;
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(BeginListening);
    }

    private void BeginListening()
    {
        if (active != null && active != this) active.CancelListening();
        active = this;
        listening = true;
        errorMessage = null;
        if (label != null) label.text = listeningText;
    }

    private void CancelListening()
    {
        listening = false;
        if (active == this) active = null;
        Refresh();
    }

    private void Refresh()
    {
        if (label == null || listening) return;
        label.text = KeyBindings.DisplayName(KeyBindings.Get(action, slot));
    }

    private void Update()
    {
        // Show a short error (e.g. "Key is reserved") then go back to the key name
        if (errorMessage != null && Time.unscaledTime > errorUntil)
        {
            errorMessage = null;
            Refresh();
        }

        if (!listening) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelListening();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (slot == 1) KeyBindings.ClearSlot(action, slot);
            CancelListening();
            return;
        }

        for (int i = 0; i < scanKeys.Length; i++)
        {
            KeyCode k = scanKeys[i];
            if (!Input.GetKeyDown(k)) continue;

            // Ignore the click used to press this button
            if (k == KeyCode.Mouse0) continue;

            if (KeyBindings.TrySet(action, slot, k, out string msg))
            {
                CancelListening();
            }
            else
            {
                listening = false;
                if (active == this) active = null;
                errorMessage = msg;
                errorUntil = Time.unscaledTime + 1.5f;
                if (label != null) label.text = msg;
            }
            return;
        }
    }
}
