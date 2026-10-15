using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every action the player can rebind.
/// </summary>
public enum GameAction
{
    MoveForward,
    MoveBack,
    MoveLeft,
    MoveRight,
    Sprint,
    Crouch,
    Jump,
    Zoom,
    Swim,
    Interact,
    MenuLeft,   // Left choice in the earphone inspection prompt
    MenuRight   // Right choice in the earphone inspection prompt
}

/// <summary>
/// Central place for all rebindable keys. Saved in PlayerPrefs, so bindings
/// set in the menu scene are live in every gameplay scene.
/// Each action has two slots (primary + optional secondary).
/// </summary>
public static class KeyBindings
{
    public const int SlotCount = 2;

    /// <summary>Raised whenever any binding changes (UI listens to refresh labels).</summary>
    public static event Action OnBindingsChanged;

    // Context flags decide which actions can't share a key.
    // Interact is in both contexts because the inspection prompt opens from it.
    private const int Gameplay = 1;
    private const int Menu = 2;

    private struct Def
    {
        public string label;
        public int context;
        public KeyCode primary;
        public KeyCode secondary;
    }

    private static readonly Dictionary<GameAction, Def> defs = new Dictionary<GameAction, Def>
    {
        { GameAction.MoveForward, new Def { label = "Move Forward", context = Gameplay, primary = KeyCode.W,           secondary = KeyCode.UpArrow } },
        { GameAction.MoveBack,    new Def { label = "Move Back",    context = Gameplay, primary = KeyCode.S,           secondary = KeyCode.DownArrow } },
        { GameAction.MoveLeft,    new Def { label = "Move Left",    context = Gameplay, primary = KeyCode.A,           secondary = KeyCode.LeftArrow } },
        { GameAction.MoveRight,   new Def { label = "Move Right",   context = Gameplay, primary = KeyCode.D,           secondary = KeyCode.RightArrow } },
        { GameAction.Sprint,      new Def { label = "Sprint",       context = Gameplay, primary = KeyCode.LeftShift,   secondary = KeyCode.None } },
        { GameAction.Crouch,      new Def { label = "Crouch",       context = Gameplay, primary = KeyCode.LeftControl, secondary = KeyCode.None } },
        { GameAction.Jump,        new Def { label = "Jump",         context = Gameplay, primary = KeyCode.Space,       secondary = KeyCode.None } },
        { GameAction.Zoom,        new Def { label = "Zoom",         context = Gameplay, primary = KeyCode.Mouse1,      secondary = KeyCode.None } },
        { GameAction.Swim,        new Def { label = "Swim Up",      context = Gameplay, primary = KeyCode.Space,       secondary = KeyCode.None } },
        { GameAction.Interact,    new Def { label = "Interact",     context = Gameplay | Menu, primary = KeyCode.E,    secondary = KeyCode.None } },
        { GameAction.MenuLeft,    new Def { label = "Prompt: Left Choice",  context = Menu, primary = KeyCode.A,       secondary = KeyCode.LeftArrow } },
        { GameAction.MenuRight,   new Def { label = "Prompt: Right Choice", context = Menu, primary = KeyCode.D,       secondary = KeyCode.RightArrow } },
    };

    // Keys that can never be assigned (they have fixed jobs elsewhere in the game).
    private static readonly HashSet<KeyCode> reserved = new HashSet<KeyCode>
    {
        KeyCode.Escape, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Delete, KeyCode.Mouse0,
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3
    };

    private static KeyCode[,] cache;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cache = null;
        OnBindingsChanged = null;
    }

    // ------------------------------------------------------------------
    // Reading
    // ------------------------------------------------------------------

    private static string PrefKey(GameAction a, int slot) => $"Key_{a}_{slot}";

    private static KeyCode Default(GameAction a, int slot) => slot == 0 ? defs[a].primary : defs[a].secondary;

    private static void EnsureLoaded()
    {
        if (cache != null) return;

        int count = Enum.GetValues(typeof(GameAction)).Length;
        cache = new KeyCode[count, SlotCount];

        foreach (GameAction a in Enum.GetValues(typeof(GameAction)))
        {
            for (int s = 0; s < SlotCount; s++)
            {
                string key = PrefKey(a, s);
                cache[(int)a, s] = PlayerPrefs.HasKey(key)
                    ? (KeyCode)PlayerPrefs.GetInt(key)
                    : Default(a, s);
            }
        }
    }

    public static KeyCode Get(GameAction a, int slot = 0)
    {
        EnsureLoaded();
        return cache[(int)a, slot];
    }

    public static string GetLabel(GameAction a) => defs[a].label;

    public static bool GetKeyDown(GameAction a)
    {
        EnsureLoaded();
        for (int s = 0; s < SlotCount; s++)
        {
            KeyCode k = cache[(int)a, s];
            if (k != KeyCode.None && Input.GetKeyDown(k)) return true;
        }
        return false;
    }

    public static bool GetKey(GameAction a)
    {
        EnsureLoaded();
        for (int s = 0; s < SlotCount; s++)
        {
            KeyCode k = cache[(int)a, s];
            if (k != KeyCode.None && Input.GetKey(k)) return true;
        }
        return false;
    }

    public static bool GetKeyUp(GameAction a)
    {
        EnsureLoaded();
        for (int s = 0; s < SlotCount; s++)
        {
            KeyCode k = cache[(int)a, s];
            if (k != KeyCode.None && Input.GetKeyUp(k)) return true;
        }
        return false;
    }

    /// <summary>Replacement for Input.GetAxis: -1, 0 or +1.</summary>
    public static float GetAxis(GameAction negative, GameAction positive)
    {
        float v = 0f;
        if (GetKey(positive)) v += 1f;
        if (GetKey(negative)) v -= 1f;
        return v;
    }

    // ------------------------------------------------------------------
    // Writing
    // ------------------------------------------------------------------

    public static bool IsReserved(KeyCode key) => reserved.Contains(key);

    private static bool Conflicts(GameAction a, GameAction b)
    {
        // Jump and Swim intentionally share Space by default.
        if ((a == GameAction.Jump && b == GameAction.Swim) || (a == GameAction.Swim && b == GameAction.Jump))
            return false;
        return (defs[a].context & defs[b].context) != 0;
    }

    private static void Store(GameAction a, int slot, KeyCode key)
    {
        cache[(int)a, slot] = key;
        PlayerPrefs.SetInt(PrefKey(a, slot), (int)key);
    }

    /// <summary>
    /// Assigns a key. If another action in the same context already uses it,
    /// the two swap keys. Returns false (with a message) if it can't be done.
    /// </summary>
    public static bool TrySet(GameAction a, int slot, KeyCode key, out string message)
    {
        EnsureLoaded();
        message = null;

        if (key == KeyCode.None) return false;
        if (reserved.Contains(key))
        {
            message = "That key is reserved.";
            return false;
        }

        KeyCode oldKey = cache[(int)a, slot];
        if (oldKey == key) return true;

        // Find the conflicting slot (if any)
        bool hasConflict = false;
        GameAction other = a;
        int otherSlot = 0;

        foreach (GameAction b in Enum.GetValues(typeof(GameAction)))
        {
            if (!Conflicts(a, b)) continue;
            for (int s = 0; s < SlotCount; s++)
            {
                if (b == a && s == slot) continue;
                if (cache[(int)b, s] == key)
                {
                    hasConflict = true;
                    other = b;
                    otherSlot = s;
                }
            }
        }

        if (hasConflict)
        {
            // The displaced slot takes our old key. If we had none and the other
            // slot is a primary, we'd leave it empty, so refuse that case.
            if (oldKey == KeyCode.None && otherSlot == 0)
            {
                message = $"Already used by {defs[other].label}.";
                return false;
            }
            Store(other, otherSlot, oldKey);
        }

        Store(a, slot, key);
        PlayerPrefs.Save();
        OnBindingsChanged?.Invoke();
        return true;
    }

    /// <summary>Empties a secondary slot. Primary slots can't be cleared.</summary>
    public static void ClearSlot(GameAction a, int slot)
    {
        if (slot == 0) return;
        EnsureLoaded();
        Store(a, slot, KeyCode.None);
        PlayerPrefs.Save();
        OnBindingsChanged?.Invoke();
    }

    public static void ResetToDefaults()
    {
        EnsureLoaded();
        foreach (GameAction a in Enum.GetValues(typeof(GameAction)))
        {
            for (int s = 0; s < SlotCount; s++)
            {
                PlayerPrefs.DeleteKey(PrefKey(a, s));
                cache[(int)a, s] = Default(a, s);
            }
        }
        PlayerPrefs.Save();
        OnBindingsChanged?.Invoke();
    }

    // ------------------------------------------------------------------
    // Display
    // ------------------------------------------------------------------

    public static string DisplayName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.None: return "—";
            case KeyCode.Mouse0: return "Left Mouse";
            case KeyCode.Mouse1: return "Right Mouse";
            case KeyCode.Mouse2: return "Middle Mouse";
            case KeyCode.Mouse3: return "Mouse 4";
            case KeyCode.Mouse4: return "Mouse 5";
            case KeyCode.UpArrow: return "Up Arrow";
            case KeyCode.DownArrow: return "Down Arrow";
            case KeyCode.LeftArrow: return "Left Arrow";
            case KeyCode.RightArrow: return "Right Arrow";
        }

        string s = key.ToString();
        if (s.StartsWith("Alpha")) return s.Substring(5);

        // "LeftShift" -> "Left Shift"
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (i > 0 && char.IsUpper(s[i]) && !char.IsUpper(s[i - 1])) sb.Append(' ');
            sb.Append(s[i]);
        }
        return sb.ToString();
    }
}
