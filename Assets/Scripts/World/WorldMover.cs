using UnityEngine;

/// <summary>
/// Moves the world backward to create an endless runner effect.
/// The skateboard stays stationary while the world scrolls past.
/// </summary>
public class WorldMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Base forward speed (world moves backward at this speed)")]
    public float baseSpeed = 10f;

    [Tooltip("Current speed multiplier (for speed boosts/slowdowns)")]
    [Range(0f, 3f)]
    public float speedMultiplier = 1f;

    [Tooltip("Whether the world is currently moving")]
    public bool isMoving = true;

    [Header("Steering Settings")]
    [Tooltip("Maximum steering angle in degrees")]
    public float maxSteerAngle = 45f;

    [Tooltip("How quickly steering responds (higher = snappier)")]
    public float steerSmoothing = 5f;

    [Tooltip("Current steering input (-1 to 1)")]
    [Range(-1f, 1f)]
    public float steerInput = 0f;

    [Header("References")]
    [Tooltip("Container holding all world objects that should move")]
    public Transform worldContainer;

    // Internal state
    private float currentSteerAngle = 0f;
    private float currentSpeed;

    // Events
    public System.Action<float> OnSpeedChanged;
    public System.Action<float> OnSteerAngleChanged;

    /// <summary>
    /// Current actual speed after multiplier
    /// </summary>
    public float CurrentSpeed => currentSpeed;

    /// <summary>
    /// Current steering angle in degrees
    /// </summary>
    public float CurrentSteerAngle => currentSteerAngle;

    private void Update()
    {
        if (!isMoving || worldContainer == null)
            return;

        // Calculate current speed
        float newSpeed = baseSpeed * speedMultiplier;
        if (!Mathf.Approximately(newSpeed, currentSpeed))
        {
            currentSpeed = newSpeed;
            OnSpeedChanged?.Invoke(currentSpeed);
        }

        // Smooth steering angle
        float targetSteerAngle = steerInput * maxSteerAngle;
        float previousSteerAngle = currentSteerAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, steerSmoothing * Time.deltaTime);

        if (!Mathf.Approximately(previousSteerAngle, currentSteerAngle))
        {
            OnSteerAngleChanged?.Invoke(currentSteerAngle);
        }

        // Calculate movement direction based on steering
        // World moves backward (negative Z), steering rotates the direction
        Vector3 direction = Quaternion.Euler(0, currentSteerAngle, 0) * Vector3.back;

        // Move the world container
        worldContainer.position += direction * currentSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Set the steering input value (-1 to 1).
    /// Typically driven by left stick X-axis.
    /// </summary>
    public void SetSteerInput(float input)
    {
        steerInput = Mathf.Clamp(input, -1f, 1f);
    }

    /// <summary>
    /// Set the speed multiplier (1 = normal, 2 = double speed, etc.)
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0f, multiplier);
    }

    /// <summary>
    /// Pause world movement
    /// </summary>
    public void Pause()
    {
        isMoving = false;
    }

    /// <summary>
    /// Resume world movement
    /// </summary>
    public void Resume()
    {
        isMoving = true;
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        isMoving = !isMoving;
    }

    /// <summary>
    /// Reset world container position to origin
    /// </summary>
    public void ResetPosition()
    {
        if (worldContainer != null)
        {
            worldContainer.position = Vector3.zero;
        }
        currentSteerAngle = 0f;
        steerInput = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (worldContainer == null)
            return;

        // Draw movement direction
        Gizmos.color = Color.green;
        Vector3 direction = Quaternion.Euler(0, currentSteerAngle, 0) * Vector3.back;
        Gizmos.DrawRay(worldContainer.position, direction * 5f);

        // Draw steering arc
        Gizmos.color = Color.yellow;
        Vector3 leftDir = Quaternion.Euler(0, -maxSteerAngle, 0) * Vector3.back;
        Vector3 rightDir = Quaternion.Euler(0, maxSteerAngle, 0) * Vector3.back;
        Gizmos.DrawRay(worldContainer.position, leftDir * 3f);
        Gizmos.DrawRay(worldContainer.position, rightDir * 3f);
    }
}
