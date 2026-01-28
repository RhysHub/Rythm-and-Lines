using UnityEngine;

/// <summary>
/// Controls the visual rotation and tilt of the skateboard based on stick input.
/// Handles steering while grounded and hands off control to TrickAnimator during tricks.
/// </summary>
public class BoardVisualController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Input reader for stick input")]
    public InputReader inputReader;

    [Tooltip("Front wheel ground detector")]
    public GroundDetector frontWheels;

    [Tooltip("Back wheel ground detector")]
    public GroundDetector backWheels;

    [Tooltip("The board visuals transform (animation target)")]
    public Transform boardVisuals;

    [Tooltip("World mover for steering direction changes")]
    public WorldMover worldMover;

    [Header("Tilt Settings")]
    [Tooltip("Maximum side-to-side tilt angle (Z rotation)")]
    public float maxTiltAngle = 15f;

    [Tooltip("Maximum nose up/down angle (X rotation)")]
    public float maxPitchAngle = 10f;

    [Tooltip("How quickly the board tilts to target rotation")]
    public float tiltSpeed = 8f;

    [Header("Steering Settings")]
    [Tooltip("Maximum steering angle for world direction change")]
    public float maxSteerAngle = 30f;

    [Tooltip("How quickly steering responds")]
    public float steerSpeed = 5f;

    [Header("State")]
    [SerializeField] private bool isAnimatingTrick = false;

    // Internal state
    private Quaternion targetRotation = Quaternion.identity;
    private Quaternion currentRotation = Quaternion.identity;
    private Vector3 basePosition;

    /// <summary>
    /// Whether a trick animation is currently playing.
    /// When true, steering input is ignored.
    /// </summary>
    public bool IsAnimatingTrick => isAnimatingTrick;

    /// <summary>
    /// Whether either set of wheels is touching the ground.
    /// </summary>
    public bool IsGrounded
    {
        get
        {
            bool frontGrounded = frontWheels != null && frontWheels.isGrounded;
            bool backGrounded = backWheels != null && backWheels.isGrounded;
            return frontGrounded || backGrounded;
        }
    }

    /// <summary>
    /// Whether both sets of wheels are off the ground.
    /// </summary>
    public bool IsAirborne
    {
        get
        {
            bool frontGrounded = frontWheels != null && frontWheels.isGrounded;
            bool backGrounded = backWheels != null && backWheels.isGrounded;
            return !frontGrounded && !backGrounded;
        }
    }

    private void Start()
    {
        if (boardVisuals != null)
        {
            basePosition = boardVisuals.localPosition;
            currentRotation = boardVisuals.localRotation;
        }
    }

    private void Update()
    {
        if (boardVisuals == null || inputReader == null)
            return;

        // Skip input processing if animating a trick
        if (isAnimatingTrick)
            return;

        // Process steering input when grounded
        if (IsGrounded)
        {
            ProcessGroundedInput();
        }
        else
        {
            // In air without trick animation - maintain current rotation
            // or apply slight air control if desired
            ProcessAirborneInput();
        }

        // Smoothly interpolate to target rotation
        currentRotation = Quaternion.Lerp(currentRotation, targetRotation, tiltSpeed * Time.deltaTime);
        boardVisuals.localRotation = currentRotation;
    }

    private void ProcessGroundedInput()
    {
        // Get left stick input for steering
        Vector2 leftStick = inputReader.GetRawLeftStick();

        // Calculate tilt angles
        // X-axis input (left/right) causes:
        // - Z rotation (side tilt/roll)
        // - Y rotation (steering direction via WorldMover)
        float rollAngle = -leftStick.x * maxTiltAngle;

        // Y-axis input (forward/back) causes:
        // - X rotation (pitch/nose up-down)
        float pitchAngle = leftStick.y * maxPitchAngle;

        // Set target rotation (no Y rotation on the board itself - that's handled by WorldMover)
        targetRotation = Quaternion.Euler(pitchAngle, 0f, rollAngle);

        // Send steering input to world mover
        if (worldMover != null)
        {
            worldMover.SetSteerInput(leftStick.x);
        }
    }

    private void ProcessAirborneInput()
    {
        // Optional: Allow slight air control
        // For now, just return to neutral rotation
        targetRotation = Quaternion.identity;

        // Stop steering when airborne
        if (worldMover != null)
        {
            worldMover.SetSteerInput(0f);
        }
    }

    /// <summary>
    /// Called by TrickAnimator to take control of board rotation.
    /// </summary>
    public void SetAnimationMode(bool animating)
    {
        isAnimatingTrick = animating;

        if (!animating)
        {
            // Restore to current visual rotation when animation ends
            if (boardVisuals != null)
            {
                currentRotation = boardVisuals.localRotation;
                targetRotation = currentRotation;
            }
        }
    }

    /// <summary>
    /// Sets the board rotation directly (used by TrickAnimator).
    /// </summary>
    public void SetRotation(Quaternion rotation)
    {
        if (boardVisuals != null)
        {
            boardVisuals.localRotation = rotation;
            currentRotation = rotation;
        }
    }

    /// <summary>
    /// Sets the board position directly (used by TrickAnimator for pop height).
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        if (boardVisuals != null)
        {
            boardVisuals.localPosition = position;
        }
    }

    /// <summary>
    /// Gets the base/resting position of the board visuals.
    /// </summary>
    public Vector3 GetBasePosition()
    {
        return basePosition;
    }

    /// <summary>
    /// Resets the board to its base position and neutral rotation.
    /// </summary>
    public void ResetToBase()
    {
        if (boardVisuals != null)
        {
            boardVisuals.localPosition = basePosition;
            boardVisuals.localRotation = Quaternion.identity;
            currentRotation = Quaternion.identity;
            targetRotation = Quaternion.identity;
        }
        isAnimatingTrick = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (boardVisuals == null)
            return;

        // Draw current rotation axes
        Gizmos.color = Color.red;
        Gizmos.DrawRay(boardVisuals.position, boardVisuals.right * 0.3f);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(boardVisuals.position, boardVisuals.up * 0.3f);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(boardVisuals.position, boardVisuals.forward * 0.3f);
    }
}
