using UnityEngine;

/// <summary>
/// Attach to FirstPersonController. Handles speed resistance and Minecraft-style 
/// waist-height buoyancy when submerged in deep water.
/// </summary>
public class PlayerWaterMovement : MonoBehaviour
{
    [Header("References")]
    public FirstPersonController controller;
    public Rigidbody rb;

    [Header("Speed Resistance")]
    [Range(0f, 1f)]
    public float shallowSpeedMultiplier = 0.5f;
    [Range(0f, 1f)]
    public float deepSpeedMultiplier = 0.6f;

    [Header("Minecraft Buoyancy (Deep Water)")]
    [Tooltip("Height from player feet to waist (e.g., 0.9 = float at waist height)")]
    public float waistOffset = 0.9f;
    [Tooltip("Upward floating force when submerged")]
    public float buoyancyForce = 15f;
    [Tooltip("Upward impulse when tapping Space to swim up")]
    public float swimImpulse = 4f;
    [Tooltip("Damping force so player floats smoothly at surface without bouncing endlessly")]
    public float waterDrag = 3f;
    public KeyCode swimKey = KeyCode.Space;

    private float originalWalkSpeed;
    private float originalSprintSpeed;
    private bool speedCached = false;

    private WaterZone currentZone;
    private Collider currentWaterCollider;

    public void EnterWaterZone(WaterZone zone, Collider zoneCollider)
    {
        CacheOriginalSpeeds();
        currentZone = zone;
        currentWaterCollider = zoneCollider;

        // Apply speed modifiers
        float multiplier = (zone.depth == WaterZone.WaterDepth.Shallow) ? shallowSpeedMultiplier : deepSpeedMultiplier;
        controller.walkSpeed = originalWalkSpeed * multiplier;
        controller.sprintSpeed = originalSprintSpeed * multiplier;
    }

    public void ExitWaterZone(WaterZone zone)
    {
        if (currentZone == zone)
        {
            currentZone = null;
            currentWaterCollider = null;
            RestoreOriginalSpeeds();
        }
    }

    private void CacheOriginalSpeeds()
    {
        if (speedCached) return;
        originalWalkSpeed = controller.walkSpeed;
        originalSprintSpeed = controller.sprintSpeed;
        speedCached = true;
    }

    private void RestoreOriginalSpeeds()
    {
        if (!speedCached) return;
        controller.walkSpeed = originalWalkSpeed;
        controller.sprintSpeed = originalSprintSpeed;
        speedCached = false;
    }

    private void Update()
    {
        // Spacebar upward swim kick
        if (currentZone != null && currentZone.depth == WaterZone.WaterDepth.Deep && Input.GetKeyDown(swimKey))
        {
            if (rb != null)
            {
                // Reset downward linear velocity before applying impulse for immediate response
                Vector3 vel = rb.linearVelocity;
                if (vel.y < 0) vel.y = 0;
                rb.linearVelocity = vel;

                rb.AddForce(Vector3.up * swimImpulse, ForceMode.Impulse);
            }
        }
    }

    private void FixedUpdate()
    {
        if (currentZone == null || currentWaterCollider == null || rb == null) return;

        // Only process floating mechanics in deep water
        if (currentZone.depth == WaterZone.WaterDepth.Deep)
        {
            float waterSurfaceY = currentWaterCollider.bounds.max.y;
            float playerWaistY = transform.position.y + waistOffset;

            // If submerged below waist height, push player up
            if (playerWaistY < waterSurfaceY)
            {
                float submergenceDepth = waterSurfaceY - playerWaistY;
                
                // Lift player upward relative to how far below surface they are
                rb.AddForce(Vector3.up * (buoyancyForce * submergenceDepth), ForceMode.Acceleration);

                // Smooth out upward speed so player stays steadily at surface level
                Vector3 vel = rb.linearVelocity;
                vel.y = Mathf.Lerp(vel.y, 0f, Time.fixedDeltaTime * waterDrag);
                rb.linearVelocity = vel;
            }
        }
    }
}