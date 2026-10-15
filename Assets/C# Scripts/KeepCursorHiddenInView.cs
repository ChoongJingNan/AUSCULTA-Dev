using UnityEngine;

public class KeepCursorHiddenInView : MonoBehaviour
{
    private void OnEnable()
    {
        // Whenever the earphone selection panel opens, hide and lock the cursor 
        // so it only rotates the 3D model instead of flying around the UI
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Continuously enforce it while this panel is active just in case another script tries to unlock it
        if (gameObject.activeSelf)
        {
            if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible == true)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}