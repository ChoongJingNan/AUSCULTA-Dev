using UnityEngine;

public class EarphoneInspect : MonoBehaviour
{
    [Header("Puzzle Completion")]
    public GameObject temporaryWalls; // Drag your temporary walls GameObject here
    
    [Header("Data")]
    public EarphoneData earphoneData;
    public ExitPuzzleManager puzzleManager;

    [Header("Interaction Settings")]
    public float interactDistance = 3.0f;
    public Transform playerCamera;

    private static bool anyEarphoneInspected = false;

    private void Update()
    {
        if (puzzleManager != null && puzzleManager.isPuzzleSolved) return;

        // Check distance to player camera
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;

        if (playerCamera != null && !anyEarphoneInspected)
        {
            float dist = Vector3.Distance(transform.position, playerCamera.position);
            if (dist <= interactDistance && KeyBindings.GetKeyDown(GameAction.Interact))
            {
                OpenInspectionUI();
            }
        }
    }

    private void OpenInspectionUI()
    {
        anyEarphoneInspected = true;
        
        // Open the UI overlay via the Singleton manager
        PuzzleUIManager.Instance?.ShowOverlay(earphoneData, this);
    }

    // This is called by PuzzleUIManager when the player hits "Confirm"
    public void CompletePuzzle()
    {
        Debug.Log("Puzzle confirmed! Removing walls.");
        
        anyEarphoneInspected = false;

        // Disable or destroy the temporary walls so the player can pass
        if (temporaryWalls != null)
        {
            temporaryWalls.SetActive(false);
        }

        // Trigger puzzle completion manager if assigned
        if (puzzleManager != null)
        {
            puzzleManager.SolvePuzzle();
        }
        
        // Disable this interaction script so they can't inspect it again
        this.enabled = false;
    }

    // This can be called if they hit "Cancel" (resets the inspection lock)
    public void CancelInspection()
    {
        anyEarphoneInspected = false;
    }
}