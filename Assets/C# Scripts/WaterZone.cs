using UnityEngine;

/// <summary>
/// Place on water trigger volumes matching shallow or deep pool areas.
/// </summary>
public class WaterZone : MonoBehaviour
{
    public enum WaterDepth { Shallow, Deep }
    public WaterDepth depth = WaterDepth.Shallow;

    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerWaterMovement player = other.GetComponentInParent<PlayerWaterMovement>();
        if (player != null)
        {
            player.EnterWaterZone(this, zoneCollider);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerWaterMovement player = other.GetComponentInParent<PlayerWaterMovement>();
        if (player != null)
        {
            player.ExitWaterZone(this);
        }
    }
}