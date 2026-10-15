using UnityEngine;

/// <summary>
/// Put this on any object (e.g. the "Reset to Defaults" button) and hook
/// ResetToDefaults() to the Button's OnClick.
/// </summary>
public class KeybindResetButton : MonoBehaviour
{
    public void ResetToDefaults()
    {
        KeyBindings.ResetToDefaults();
    }
}
