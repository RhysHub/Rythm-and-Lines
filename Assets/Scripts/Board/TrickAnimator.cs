using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates and plays procedural trick animations based on TrickDefinition input sequences.
/// Subscribes to TrickInputSystem.OnTrickMatched to automatically animate confirmed tricks.
/// </summary>
public class TrickAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The trick input system to subscribe to")]
    public TrickInputSystem trickInputSystem;

    [Tooltip("The board visual controller")]
    public BoardVisualController boardController;

    [Tooltip("The board visuals transform (animation target)")]
    public Transform boardVisuals;

    [Header("Animation Settings")]
    [Tooltip("Default animation duration")]
    public float defaultDuration = 0.6f;

    [Tooltip("Default pop height")]
    public float defaultPopHeight = 0.5f;

    [Tooltip("Always pop even for grounded tricks")]
    public bool alwaysPop = true;

    [Tooltip("Pop height curve (0-1 normalized time, should peak in middle)")]
    public AnimationCurve popCurve;

    [Tooltip("Rotation curve (0-1 normalized time)")]
    public AnimationCurve rotationCurve;

    [Header("Rotation Amounts (degrees)")]
    [Tooltip("Degrees for a kickflip/heelflip")]
    public float flipRotation = 360f;

    [Tooltip("Degrees for a 180 shuvit")]
    public float shuvit180Rotation = 180f;

    [Tooltip("Degrees for a 360 shuvit (tre flip)")]
    public float shuvit360Rotation = 360f;

    [Tooltip("Small rotation for tricks with no explicit rotation (like Ollie)")]
    public float minVisibleRotation = 15f;

    [Header("Debug")]
    public bool debugMode = true;

    // Animation state
    private bool isAnimating = false;
    private float animationTime = 0f;
    private TrickAnimationData currentAnimation;
    private Vector3 startPosition;
    private Quaternion startRotation;

    /// <summary>
    /// Whether an animation is currently playing
    /// </summary>
    public bool IsAnimating => isAnimating;

    /// <summary>
    /// Current animation progress (0-1)
    /// </summary>
    public float AnimationProgress => isAnimating ? animationTime / currentAnimation.duration : 0f;

    private void Awake()
    {
        // Setup default curves if not configured or invalid
        if (popCurve == null || popCurve.keys.Length < 2)
        {
            SetupDefaultPopCurve();
        }
        else
        {
            // Check if curve is flat (broken default)
            float midValue = popCurve.Evaluate(0.5f);
            if (midValue < 0.1f)
            {
                Debug.LogWarning("[TrickAnimator] Pop curve appears flat, resetting to default");
                SetupDefaultPopCurve();
            }
        }

        if (rotationCurve == null || rotationCurve.keys.Length < 2)
        {
            rotationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }

    private void SetupDefaultPopCurve()
    {
        // Parabolic pop curve - starts at 0, peaks at 1 around 40%, ends at 0
        popCurve = new AnimationCurve();
        popCurve.AddKey(new Keyframe(0f, 0f, 0f, 3f));      // Start at ground
        popCurve.AddKey(new Keyframe(0.4f, 1f, 0f, 0f));    // Peak in air
        popCurve.AddKey(new Keyframe(1f, 0f, -3f, 0f));     // Land back down

        if (debugMode)
            Debug.Log("[TrickAnimator] Set up default pop curve");
    }

    private void OnEnable()
    {
        if (trickInputSystem != null)
        {
            trickInputSystem.OnTrickMatched += OnTrickMatched;
        }
    }

    private void OnDisable()
    {
        if (trickInputSystem != null)
        {
            trickInputSystem.OnTrickMatched -= OnTrickMatched;
        }
    }

    private void Update()
    {
        if (!isAnimating || currentAnimation == null)
            return;

        animationTime += Time.deltaTime;
        float t = Mathf.Clamp01(animationTime / currentAnimation.duration);

        ApplyAnimationFrame(t);

        // Check if animation is complete
        if (t >= 1f)
        {
            FinishAnimation();
        }
    }

    /// <summary>
    /// Called when a trick is matched by the input system
    /// </summary>
    private void OnTrickMatched(TrickMatchResult result)
    {
        if (result == null || !result.matched || result.trick == null)
        {
            if (debugMode)
                Debug.Log("[TrickAnimator] OnTrickMatched called but result invalid");
            return;
        }

        if (debugMode)
        {
            Debug.Log($"<color=yellow>[TrickAnimator] TRICK MATCHED: {result.trick.trickName}</color>");
        }

        // Generate animation from the trick definition
        TrickAnimationData animData = GenerateAnimationFromTrick(result.trick, result.accuracy);

        StartAnimation(animData);
    }

    /// <summary>
    /// Generates animation parameters from a trick definition
    /// </summary>
    public TrickAnimationData GenerateAnimationFromTrick(TrickDefinition trick, float accuracy = 1f)
    {
        TrickAnimationData anim = new TrickAnimationData();
        anim.trickName = trick.trickName;
        anim.category = trick.category;
        anim.duration = defaultDuration;
        anim.popHeight = defaultPopHeight;

        // Use the trick's pops setting, or override with alwaysPop if enabled
        anim.hasPop = trick.pops || alwaysPop;

        // Analyze each input step to determine rotation
        foreach (InputStep step in trick.inputSequence)
        {
            AnalyzeInputStep(step, ref anim);
        }

        // If no rotation was added, add a small Z rotation for visual feedback (board tilts)
        if (!anim.HasRotation)
        {
            anim.zRotation = minVisibleRotation;
        }

        if (debugMode)
        {
            Debug.Log($"[TrickAnimator] Generated animation for {trick.trickName}: " +
                     $"Pop={anim.hasPop} (trick.pops={trick.pops}), Height={anim.popHeight}, " +
                     $"Rot=({anim.xRotation}, {anim.yRotation}, {anim.zRotation})");
        }

        return anim;
    }

    /// <summary>
    /// Analyzes an input step and adds appropriate rotation to the animation
    /// </summary>
    private void AnalyzeInputStep(InputStep step, ref TrickAnimationData anim)
    {
        // Handle drags (shuvits)
        if (step.inputType == InputType.Drag)
        {
            AnalyzeDragInput(step, ref anim);
            return;
        }

        // Handle directional inputs based on stick type
        if (step.stickType == StickType.RightStick)
        {
            AnalyzeRightStickInput(step, ref anim);
        }
        else // LeftStick
        {
            AnalyzeLeftStickInput(step, ref anim);
        }
    }

    /// <summary>
    /// Analyzes drag input for shuvit rotations
    /// </summary>
    private void AnalyzeDragInput(InputStep step, ref TrickAnimationData anim)
    {
        // Drag inputs typically indicate shuvits (Y-axis rotation)
        // Down->Left = BS (backside) shuvit = positive Y rotation
        // Down->Right = FS (frontside) shuvit = negative Y rotation

        if (step.direction == StickDirection.Down)
        {
            if (step.dragEndDirection == StickDirection.Left ||
                step.dragEndDirection == StickDirection.DownLeft)
            {
                // BS Shuvit - check if it's part of a tre flip (360)
                if (anim.yRotation >= shuvit180Rotation)
                {
                    // Already has shuvit rotation, this might be a tre flip
                    anim.yRotation = shuvit360Rotation;
                }
                else
                {
                    anim.yRotation += shuvit180Rotation;
                }
            }
            else if (step.dragEndDirection == StickDirection.Right ||
                     step.dragEndDirection == StickDirection.DownRight)
            {
                // FS Shuvit
                if (anim.yRotation <= -shuvit180Rotation)
                {
                    anim.yRotation = -shuvit360Rotation;
                }
                else
                {
                    anim.yRotation -= shuvit180Rotation;
                }
            }
        }
    }

    /// <summary>
    /// Analyzes right stick (back foot) input
    /// Right stick typically handles pop and some flip/shuvit initiation
    /// </summary>
    private void AnalyzeRightStickInput(InputStep step, ref TrickAnimationData anim)
    {
        switch (step.direction)
        {
            case StickDirection.Up:
                // Ollie pop - just height, no rotation
                // (Pop is already handled by hasPop flag)
                break;

            case StickDirection.UpRight:
                // Could be kickflip initiation from back foot
                // (Usually kickflips use left stick in this system)
                anim.xRotation += flipRotation;
                break;

            case StickDirection.UpLeft:
                // Could be heelflip initiation from back foot
                anim.xRotation -= flipRotation;
                break;

            case StickDirection.Down:
                // Pop setup (part of trick sequence)
                break;

            case StickDirection.Left:
                // BS shuvit direction (if not a drag)
                if (step.inputType != InputType.Drag)
                {
                    anim.yRotation += shuvit180Rotation;
                }
                break;

            case StickDirection.Right:
                // FS shuvit direction (if not a drag)
                if (step.inputType != InputType.Drag)
                {
                    anim.yRotation -= shuvit180Rotation;
                }
                break;
        }
    }

    /// <summary>
    /// Analyzes left stick (front foot) input
    /// Left stick controls flip direction (kickflip/heelflip)
    /// </summary>
    private void AnalyzeLeftStickInput(InputStep step, ref TrickAnimationData anim)
    {
        switch (step.direction)
        {
            case StickDirection.Up:
                // Nollie pop
                break;

            case StickDirection.UpRight:
            case StickDirection.Right:
                // Kickflip - board flips toward heel side
                // Positive X rotation (rolls right when viewed from behind)
                anim.xRotation += flipRotation;
                break;

            case StickDirection.UpLeft:
            case StickDirection.Left:
                // Heelflip - board flips toward toe side
                // Negative X rotation (rolls left when viewed from behind)
                anim.xRotation -= flipRotation;
                break;

            case StickDirection.Down:
                // Setup position
                break;

            case StickDirection.DownRight:
                // Varial kickflip component?
                anim.xRotation += flipRotation;
                break;

            case StickDirection.DownLeft:
                // Varial heelflip component?
                anim.xRotation -= flipRotation;
                break;
        }
    }

    /// <summary>
    /// Starts playing an animation
    /// </summary>
    public void StartAnimation(TrickAnimationData animData)
    {
        if (animData == null)
        {
            if (debugMode)
                Debug.LogWarning("[TrickAnimator] StartAnimation called with null data");
            return;
        }

        if (boardVisuals == null)
        {
            Debug.LogError("[TrickAnimator] boardVisuals is null! Cannot animate.");
            return;
        }

        currentAnimation = animData;
        animationTime = 0f;
        isAnimating = true;

        // Store starting state
        startPosition = boardVisuals.localPosition;
        startRotation = boardVisuals.localRotation;

        if (debugMode)
        {
            Debug.Log($"<color=green>[TrickAnimator] STARTING ANIMATION: {animData.trickName}</color>\n" +
                     $"  Duration: {animData.duration}s, Pop Height: {animData.popHeight}m\n" +
                     $"  Rotation: X={animData.xRotation}, Y={animData.yRotation}, Z={animData.zRotation}\n" +
                     $"  Start Pos: {startPosition}, Start Rot: {startRotation.eulerAngles}");
        }

        // Notify board controller
        if (boardController != null)
        {
            boardController.SetAnimationMode(true);
        }
    }

    /// <summary>
    /// Applies the current animation frame
    /// </summary>
    private void ApplyAnimationFrame(float t)
    {
        if (boardVisuals == null || currentAnimation == null)
            return;

        Vector3 newPos = startPosition;
        Quaternion newRot = startRotation;

        // Apply pop height using curve
        if (currentAnimation.hasPop)
        {
            float heightMultiplier = popCurve.Evaluate(t);
            newPos.y += currentAnimation.popHeight * heightMultiplier;
        }

        // Always apply rotation (even if small)
        float rotProgress = rotationCurve.Evaluate(t);

        // Calculate current rotation
        Quaternion animRotation = Quaternion.Euler(
            currentAnimation.xRotation * rotProgress,
            currentAnimation.yRotation * rotProgress,
            currentAnimation.zRotation * rotProgress
        );

        newRot = startRotation * animRotation;

        // Apply to board
        boardVisuals.localPosition = newPos;
        boardVisuals.localRotation = newRot;
    }

    /// <summary>
    /// Finishes the current animation
    /// </summary>
    private void FinishAnimation()
    {
        isAnimating = false;

        // Snap to final position/rotation
        if (boardVisuals != null)
        {
            // Return to ground level
            boardVisuals.localPosition = startPosition;

            // Always return to original rotation for clean landing
            // (In a real game, 180 shuvits would change stance, but for now keep it simple)
            boardVisuals.localRotation = startRotation;
        }

        if (debugMode)
        {
            Debug.Log($"<color=cyan>[TrickAnimator] Animation finished: {currentAnimation?.trickName}</color>");
        }

        // Notify board controller
        if (boardController != null)
        {
            boardController.SetAnimationMode(false);
        }

        currentAnimation = null;
    }

    /// <summary>
    /// Cancels the current animation immediately
    /// </summary>
    public void CancelAnimation()
    {
        if (!isAnimating)
            return;

        isAnimating = false;

        // Return to start position
        if (boardVisuals != null)
        {
            boardVisuals.localPosition = startPosition;
            boardVisuals.localRotation = startRotation;
        }

        // Notify board controller
        if (boardController != null)
        {
            boardController.SetAnimationMode(false);
        }

        currentAnimation = null;
    }

    /// <summary>
    /// Manually triggers an animation for a specific trick
    /// </summary>
    public void PlayTrick(TrickDefinition trick, float accuracy = 1f)
    {
        if (trick == null)
            return;

        TrickAnimationData animData = GenerateAnimationFromTrick(trick, accuracy);
        StartAnimation(animData);
    }

    /// <summary>
    /// Debug UI to show animation state
    /// </summary>
    private void OnGUI()
    {
        if (!debugMode)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 12;
        style.normal.textColor = Color.white;

        string info = "[TrickAnimator]\n";
        info += $"Board Visuals: {(boardVisuals != null ? "OK" : "MISSING!")}\n";
        info += $"Trick System: {(trickInputSystem != null ? "OK" : "MISSING!")}\n";
        info += $"Is Animating: {isAnimating}\n";

        if (isAnimating && currentAnimation != null)
        {
            float progress = animationTime / currentAnimation.duration;
            info += $"Current: {currentAnimation.trickName}\n";
            info += $"Progress: {progress:P0}\n";
            info += $"Rot: ({currentAnimation.xRotation:F0}, {currentAnimation.yRotation:F0}, {currentAnimation.zRotation:F0})";
        }

        GUI.Box(new Rect(Screen.width - 220, 10, 210, 130), info, style);
    }
}
