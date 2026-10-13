using UnityEngine;

public class ExitPuzzleManager : MonoBehaviour
{
    [Header("Blocking Environment")]
    public GameObject[] exitWalls; // The 2 walls blocking the exit
    public GameObject finalChaseTrigger; // Activates Entity #5 / Chase script when walls drop

    [HideInInspector] public bool isPuzzleSolved = false;

    public void SolvePuzzle()
    {
        if (isPuzzleSolved) return;
        isPuzzleSolved = true;

        // Remove the 2 blocking walls
        foreach (GameObject wall in exitWalls)
        {
            if (wall != null) wall.SetActive(false);
        }

        // Enable final chase trigger / entity encounter
        if (finalChaseTrigger != null)
        {
            finalChaseTrigger.SetActive(true);
        }

        Debug.Log("Puzzle solved! Walls removed, opening path to exit/chase.");
    }
}