using UnityEngine;

public class SaveRoomReturnHandler : MonoBehaviour
{
    private void Start()
    {
        Debug.Log($"[SaveRoomReturnHandler] Start() in scene '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}' | hasReturnPoint={SaveRoomData.HasReturnPoint()}", this);

        // 1. Reset Backrooms lighting, atmosphere, exposure, and EncounterManager music/state
        EntitySpot.ResetBackroomsZone();

        // 2. Teleport player back to the saved return point
        if (SaveRoomData.HasReturnPoint())
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                SaveRoomData.ConsumeReturnPoint(out Vector3 spawnPos, out Quaternion spawnRot);

                CharacterController controller = player.GetComponentInParent<CharacterController>();
                if (controller != null) controller.enabled = false;

                player.transform.position = spawnPos;
                player.transform.rotation = spawnRot;

                if (controller != null) controller.enabled = true;
            }
        }
    }
}