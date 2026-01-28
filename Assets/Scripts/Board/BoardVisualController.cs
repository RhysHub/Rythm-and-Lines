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

    [Header("Lateral Movement")]
    [Tooltip("Maximum Y rotation (yaw) when steering")]
    public float maxYawAngle = 20f;

    [Tooltip("Maximum lateral distance from center")]
    public float maxLateralDistance = 3f;

    [Tooltip("Speed of lateral movement")]
    public float lateralSpeed = 5f;

    [Tooltip("How quickly board returns to center when not steering")]
    public float centeringSpeed = 2f;

    [Header("State")]
    [SerializeField] private bool isAnimatingTrick = false;

    [Header("Debug")]
    public bool debugMode = true;
    [SerializeField] private bool debugIsGrounded;
    [SerializeField] private Vector2 debugLeftStick;

    // Internal state
    private Quaternion targetRotation = Quaternion.identity;
    private Quaternion currentRotation = Quaternion.identity;
    private Vector3 basePosition;
    private float currentLateralPosition = 0f;
    private Vector3 skateboardStartPosition;
    private bool initialized = false;

    /// <summary>
    /// Whether a trick animation is currently playing.
    /// When true, steering input is ignored.
    /// </summary>
    public bool IsAnimatingTrick => isAnimatingTrick;

    /// <summary>
    /// Whether either set of wheels is touching the ground.
    /// Defaults to true if no ground detectors are assigned (endless runner default).
    /// </summary>
    public bool IsGrounded
    {
        get
        {
            // If no ground detectors assigned, assume grounded (endless runner default)
            if (frontWheels == null && backWheels == null)
                return !isAnimatingTrick;

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
        Initialize();
    }

    private void Initialize()
    {
        if (initialized) return;

        // Auto-find references if missing
        AutoFindReferences();

        if (boardVisuals != null)
        {
            basePosition = boardVisuals.localPosition;
            currentRotation = boardVisuals.localRotation;
        }

        // Store the skateboard's starting position for lateral bounds
        // Only store X=0 to ensure proper centering
        skateboardStartPosition = transform.position;
        skateboardStartPosition.x = 0f; // Force center start

        initialized = true;

        if (debugMode) Debug.Log($"[BoardVisualController] Initialized. Start pos: {skateboardStartPosition}");
    }

    private void AutoFindReferences()
    {
        // Auto-find boardVisuals
        if (boardVisuals == null)
        {
            // Try to find BoardVisuals child
            Transform found = transform.Find("BoardVisuals");
            if (found != null)
            {
                boardVisuals = found;
                if (debugMode) Debug.Log("[BoardVisualController] Auto-found BoardVisuals child");
            }
            else
            {
                // Try to find SkateboardBuilder and get its visuals
                SkateboardBuilder builder = GetComponentInChildren<SkateboardBuilder>();
                if (builder != null && builder.transform.childCount > 0)
                {
                    boardVisuals = builder.transform.GetChild(0);
                    if (debugMode) Debug.Log("[BoardVisualController] Auto-found BoardVisuals from SkateboardBuilder");
                }
            }
        }

        // Auto-find inputReader
        if (inputReader == null)
        {
            inputReader = FindObjectOfType<InputReader>();
            if (inputReader != null && debugMode)
                Debug.Log("[BoardVisualController] Auto-found InputReader");
        }

        // Auto-find worldMover
        if (worldMover == null)
        {
            worldMover = FindObjectOfType<WorldMover>();
            if (worldMover != null && debugMode)
                Debug.Log("[BoardVisualController] Auto-found WorldMover");
        }

        // Auto-find ground detectors
        if (frontWheels == null || backWheels == null)
        {
            GroundDetector[] detectors = GetComponentsInChildren<GroundDetector>();
            foreach (var detector in detectors)
            {
                if (detector.name.Contains("Front") && frontWheels == null)
                    frontWheels = detector;
                else if (detector.name.Contains("Back") && backWheels == null)
                    backWheels = detector;
            }
            if (debugMode && detectors.Length > 0)
                Debug.Log($"[BoardVisualController] Auto-found {detectors.Length} GroundDetectors");
        }
    }

    private void Update()
    {
        // Ensure initialized
        if (!initialized) Initialize();

        // Re-find references if they become null
        if (boardVisuals == null || inputReader == null)
        {
            AutoFindReferences();
            if (boardVisuals == null || inputReader == null)
            {
                if (debugMode) Debug.LogWarning($"[BoardVisualController] Missing ref: boardVisuals={boardVisuals}, inputReader={inputReader}");
                return;
            }
        }

        // Debug info
        if (debugMode)
        {
            debugIsGrounded = IsGrounded;
            debugLeftStick = inputReader.GetRawLeftStick();
        }

        // Skip all input processing if animating a trick
        if (isAnimatingTrick)
        {
            // Still maintain lateral position during tricks
            ApplyLateralPosition();
            return;
        }

        // Always process steering (endless runner - always on ground unless tricking)
        ProcessGroundedInput();

        // Apply lateral position
        ApplyLateralPosition();

        // Smoothly interpolate to target rotation
        currentRotation = Quaternion.Lerp(currentRotation, targetRotation, tiltSpeed * Time.deltaTime);
        boardVisuals.localRotation = currentRotation;
    }

    private void ApplyLateralPosition()
    {
        Vector3 newPosition = skateboardStartPosition;
        newPosition.x = skateboardStartPosition.x + currentLateralPosition;
        newPosition.y = transform.position.y; // Keep current Y (for tricks)
        newPosition.z = transform.position.z; // Keep current Z
        transform.position = newPosition;
    }

    private void ProcessGroundedInput()
    {
        // Get left stick input for steering
        Vector2 leftStick = inputReader.GetRawLeftStick();

        // Calculate tilt angles
        // X-axis input (left/right) causes:
        // - Z rotation (side tilt/lean)
        // - Y rotation (yaw/steering look)
        float rollAngle = -leftStick.x * maxTiltAngle;
        float yawAngle = leftStick.x * maxYawAngle;

        // Y-axis input (forward/back) causes:
        // - X rotation (pitch/nose up-down)
        float pitchAngle = leftStick.y * maxPitchAngle;

        // Set target rotation with yaw for steering appearance
        targetRotation = Quaternion.Euler(pitchAngle, yawAngle, rollAngle);

        // Handle lateral movement
        if (Mathf.Abs(leftStick.x) > 0.1f)
        {
            // Move in steering direction
            float targetLateral = leftStick.x * maxLateralDistance;
            currentLateralPosition = Mathf.MoveTowards(
                currentLateralPosition,
                targetLateral,
                lateralSpeed * Time.deltaTime);

            if (debugMode && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[BoardVisualController] Steering: stick={leftStick.x:F2}, lateral={currentLateralPosition:F2}, target={targetLateral:F2}");
            }
        }
        else
        {
            // Return to center when not steering
            currentLateralPosition = Mathf.MoveTowards(
                currentLateralPosition,
                0f,
                centeringSpeed * Time.deltaTime);
        }

        // Clamp lateral position to bounds (position applied in ApplyLateralPosition)
        currentLateralPosition = Mathf.Clamp(currentLateralPosition, -maxLateralDistance, maxLateralDistance);

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

        // Lateral position is maintained in ApplyLateralPosition()

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

        // Draw lateral movement bounds
        Vector3 center = Application.isPlaying ? skateboardStartPosition : transform.position;
        Gizmos.color = Color.yellow;
        Vector3 leftBound = center + Vector3.left * maxLateralDistance;
        Vector3 rightBound = center + Vector3.right * maxLateralDistance;
        Gizmos.DrawLine(leftBound + Vector3.forward * 2f, leftBound + Vector3.back * 2f);
        Gizmos.DrawLine(rightBound + Vector3.forward * 2f, rightBound + Vector3.back * 2f);
        Gizmos.DrawLine(leftBound, rightBound);
    }
}
