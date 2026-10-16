using UnityEngine;

/// <summary>
/// Put ONE of these on an empty root object in each map (R1, SaveRoom, Poolrooms).
/// It marks where the main-menu camera should sit when that map is shown behind the menu.
/// Place it with GameObject > Align With View. The yellow gizmo shows what the camera will see.
/// </summary>
public class MenuCameraAnchor : MonoBehaviour
{
    [Header("Main menu music (optional)")]
    [Tooltip("Track to play behind the menu when this map is the backdrop. Leave empty to use the map's own level music automatically.")]
    public AudioClip menuMusic;

    [Header("Gizmo preview only (does not affect gameplay)")]
    [Tooltip("Match the menu camera's Field of View")]
    public float fieldOfView = 60f;

    [Tooltip("Roughly how far in front of the camera the menu panel sits. Keep this volume clear of walls.")]
    public float previewDistance = 6f;

    public float aspect = 16f / 9f;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 1f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, fieldOfView, previewDistance, 0.1f, aspect);
        Gizmos.DrawSphere(Vector3.zero, 0.1f);
    }
}