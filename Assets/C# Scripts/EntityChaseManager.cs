using System.Collections;
using UnityEngine;
using TMPro;

public class EntityChaseManager : MonoBehaviour
{
    [Header("Player Settings")]
    public FirstPersonController playerController;

    [Header("UI Settings")]
    public GameObject runUIObject;     // UI GameObject containing TextMeshPro
    public TMP_Text runTextComponent;  // TMP_Text component for updating text

    [Header("Chase Countdown Settings")]
    public float chaseDuration = 10f;   // Duration of chase in seconds
    public bool showCountdown = true;   // Toggles numerical countdown display

    private Coroutine chaseCoroutine;

    /// <summary>
    /// Call this function when the Bacteria Entity begins the chase sequence.
    /// </summary>
    public void StartChase()
    {
        if (chaseCoroutine != null)
        {
            StopCoroutine(chaseCoroutine);
        }
        chaseCoroutine = StartCoroutine(ChaseRoutine());
    }

    private IEnumerator ChaseRoutine()
    {
        // 1. Enable Sprint on FirstPersonController
        if (playerController != null)
        {
            playerController.enableSprint = true;
        }

        // 2. Display "RUN" UI Text
        if (runUIObject != null)
        {
            runUIObject.SetActive(true);
        }

        float timeRemaining = chaseDuration;

        // 3. Countdown timer loop
        while (timeRemaining > 0)
        {
            if (runTextComponent != null)
            {
                if (showCountdown)
                {
                    runTextComponent.text = $"<color=red>RUN!</color> ({Mathf.CeilToInt(timeRemaining)})";
                }
                else
                {
                    runTextComponent.text = "<color=red>RUN!</color>";
                }
            }

            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        // 4. Timer finished -> End Chase
        EndChase();
    }

    /// <summary>
    /// Call this function when the chase ends, player escapes, or dies.
    /// </summary>
    public void EndChase()
    {
        if (chaseCoroutine != null)
        {
            StopCoroutine(chaseCoroutine);
            chaseCoroutine = null;
        }

        // 1. Disable Sprint on FirstPersonController
        if (playerController != null)
        {
            playerController.enableSprint = false;
        }

        // 2. Hide UI Text
        if (runUIObject != null)
        {
            runUIObject.SetActive(false);
        }
    }
}