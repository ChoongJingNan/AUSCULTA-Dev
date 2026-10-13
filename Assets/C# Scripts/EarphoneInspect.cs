using UnityEngine;
using TMPro;

public class EarphoneInspect : MonoBehaviour
{
    [Header("Data")]
    public EarphoneData earphoneData;
    public ExitPuzzleManager puzzleManager;

    [Header("Interaction Settings")]
    public float interactDistance = 3.0f;
    public Transform playerCamera;

    private bool isInspecting = false;
    private static bool anyEarphoneInspected = false;

    private void Update()
    {
        if (puzzleManager != null && puzzleManager.isPuzzleSolved) return;

        // Check distance to player camera
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;

        if (playerCamera != null && !anyEarphoneInspected)
        {
            float dist = Vector3.Distance(transform.position, playerCamera.position);
            if (dist <= interactDistance && Input.GetKeyDown(KeyCode.E))
            {
                OpenInspectionUI();
            }
        }

        // Handle UI input when inspecting this specific item
        if (isInspecting)
        {
            HandleInspectionInput();
        }
    }

    private void OpenInspectionUI()
    {
        isInspecting = true;
        anyEarphoneInspected = true;
        
        // Disable player movement/look if desired, or show UI Canvas
        PuzzleUIManager.Instance?.ShowOverlay(earphoneData, this);
    }

    private void HandleInspectionInput()
    {
        // Arrow keys or A/D to select Confirm (0) vs Cancel (1)
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            PuzzleUIManager.Instance?.SetSelectedIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            PuzzleUIManager.Instance?.SetSelectedIndex(1);
        }

        // Confirm selection
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            int choice = PuzzleUIManager.Instance.selectedIndex;
            CloseInspectionUI(confirmed: (choice == 0));
        }
    }

    public void CloseInspectionUI(bool confirmed)
    {
        isInspecting = false;
        anyEarphoneInspected = false;
        PuzzleUIManager.Instance?.HideOverlay();

        // No matter what choice they pick, trigger puzzle completion!
        if (puzzleManager != null)
        {
            puzzleManager.SolvePuzzle();
        }
    }
}