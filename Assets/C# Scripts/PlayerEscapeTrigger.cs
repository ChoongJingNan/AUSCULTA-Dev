using UnityEngine;

public class PlayerEscapeTrigger : MonoBehaviour
{
    public EntitySpot entity5SpotScript;

    private void OnTriggerEnter(Collider other)
    {
        bool isPlayer = other.CompareTag("Player") || 
                        (other.transform.root != null && other.transform.root.CompareTag("Player")) ||
                        other.GetComponentInParent<FirstPersonController>() != null;

        if (isPlayer && entity5SpotScript != null)
        {
            entity5SpotScript.CompleteChaseEscape();
        }
    }
}