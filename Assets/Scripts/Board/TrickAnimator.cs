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
    [Tooltip("Degrees for a kickflip/heelflip (Z-axis, around long axis of board)")]
    public float flipRotation = 360f;

    [Tooltip("Degrees for a 180 shuvit (Y-axis, horizontal spin)")]
    public float shuvit180Rotation = 180f;

    [Tooltip("Degrees for a 360 shuvit (Y-axis, tre flip)")]
    public float shuvit360Rotation = 360f;

    [Tooltip("Small tilt for tricks with no explicit rotation (like Ollie)")]
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
        // Auto-find references if not assigned
        AutoFindReferences();

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

    private void AutoFindReferences()
    {
        // Find TrickInputSystem if not assigned
        if (trickInputSystem == null)
        {
            trickInputSystem = FindObjectOfType<TrickInputSystem>();
            if (trickInputSystem != null && debugMode)
                Debug.Log("[TrickAnimator] Auto-found TrickInputSystem");
        }

        // Find BoardVisualController if not assigned
        if (boardController == null)
        {
            boardController = GetComponent<BoardVisualController>();
            if (boardController == null)
                boardController = FindObjectOfType<BoardVisualController>();
            if (boardController != null && debugMode)
                Debug.Log("[TrickAnimator] Auto-found BoardVisualController");
        }

        // Find board visuals if not assigned
        if (boardVisuals == null)
        {
            // Try to find BoardVisuals child
            Transform found = transform.Find("BoardVisuals");
            if (found == null)
            {
                // Search in children
                foreach (Transform child in transform)
                {
                    if (child.name.Contains("BoardVisuals") || child.name.Contains("Deck"))
                    {
                        found = child;
                        break;
                    }
                }
            }

            // Try SkateboardBuilder
            if (found == null)
            {
                var builder = GetComponent<SkateboardBuilder>();
                if (builder != null && builder.BoardVisuals != null)
                {
                    found = builder.BoardVisuals;
                }
            }

            // Try to find anywhere in scene
            if (found == null)
            {
                var allBuilders = FindObjectsOfType<SkateboardBuilder>();
                foreach (var builder in allBuilders)
                {
                    if (builder.BoardVisuals != null)
                    {
                        found = builder.BoardVisuals;
                        break;
                    }
                }
            }

            // Last resort: find any object named BoardVisuals
            if (found == null)
            {
                var go = GameObject.Find("BoardVisuals");
                if (go != null)
                    found = go.transform;
            }

            if (found != null)
            {
                boardVisuals = found;
                if (debugMode)
                    Debug.Log($"[TrickAnimator] Auto-found boardVisuals: {found.name}");
            }
            else
            {
                Debug.LogError("[TrickAnimator] Could not find boardVisuals! Please assign manually or ensure SkateboardBuilder has built the board.");
            }
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

        // Track if we've seen a nollie pop (LS Up) - this changes how RS diagonals are interpreted
        bool hasNolliePop = false;

        // Analyze each input step to determine rotation
        foreach (InputStep step in trick.inputSequence)
        {
            // Check for nollie pop (LS Up)
            if (step.stickType == StickType.LeftStick && step.direction == StickDirection.Up)
            {
                hasNolliePop = true;
            }

            AnalyzeInputStep(step, ref anim, hasNolliePop);
        }

        // If no rotation was added, add a small X rotation for visual feedback (nose lifts)
        if (!anim.HasRotation)
        {
            anim.xRotation = minVisibleRotation;
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
    private void AnalyzeInputStep(InputStep step, ref TrickAnimationData anim, bool hasNolliePop = false)
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
            AnalyzeRightStickInput(step, ref anim, hasNolliePop);
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
        // If dragTurnType is specified, use it directly for rotation
        if (step.dragTurnType != DragTurnType.None)
        {
            // Get shuvit rotation from turn type (positive = BS/CCW, negative = FS/CW)
            anim.yRotation += step.dragTurnType.GetShuvitRotation();
            return;
        }

        // Legacy: fallback to dragEndDirection-based logic
        // Down->Left = BS (backside) shuvit = positive Y rotation
        // Down->Right = FS (frontside) shuvit = negative Y rotation
        if (step.direction == StickDirection.Down)
        {
            // BS 360 Shuvit - full arc through left to up
            if (step.dragEndDirection == StickDirection.UpLeft ||
                step.dragEndDirection == StickDirection.Up && anim.yRotation > 0)
            {
                anim.yRotation = shuvit360Rotation;
            }
            // FS 360 Shuvit - full arc through right to up
            else if (step.dragEndDirection == StickDirection.UpRight ||
                     step.dragEndDirection == StickDirection.Up && anim.yRotation < 0)
            {
                anim.yRotation = -shuvit360Rotation;
            }
            // BS Pop Shuvit (180)
            else if (step.dragEndDirection == StickDirection.Left ||
                step.dragEndDirection == StickDirection.DownLeft)
            {
                if (anim.yRotation >= shuvit180Rotation)
                    anim.yRotation = shuvit360Rotation;
                else
                    anim.yRotation += shuvit180Rotation;
            }
            // FS Pop Shuvit (180)
            else if (step.dragEndDirection == StickDirection.Right ||
                     step.dragEndDirection == StickDirection.DownRight)
            {
                if (anim.yRotation <= -shuvit180Rotation)
                    anim.yRotation = -shuvit360Rotation;
                else
                    anim.yRotation -= shuvit180Rotation;
            }
        }
    }

    /// <summary>
    /// Analyzes right stick (back foot) input
    /// Right stick typically handles pop and some flip/shuvit initiation
    /// For nollie tricks (hasNolliePop=true), DownLeft/DownRight produce flip instead of shuvit
    /// </summary>
    private void AnalyzeRightStickInput(InputStep step, ref TrickAnimationData anim, bool hasNolliePop = false)
    {
        switch (step.direction)
        {
            case StickDirection.Up:
                // Ollie pop - just height, no rotation
                // (Pop is already handled by hasPop flag)
                break;

            case StickDirection.UpLeft:
                // Kickflip from back foot - Z-axis rotation (barrel roll)
                anim.zRotation += flipRotation;
                break;

            case StickDirection.UpRight:
                // Heelflip from back foot - Z-axis rotation (barrel roll opposite)
                anim.zRotation -= flipRotation;
                break;

            case StickDirection.Down:
                // Pop setup (part of trick sequence)
                break;

            case StickDirection.Left:
                // BS shuvit direction (if not a drag) - Y-axis rotation
                if (step.inputType != InputType.Drag)
                {
                    anim.yRotation += shuvit180Rotation;
                }
                break;

            case StickDirection.Right:
                // FS shuvit direction (if not a drag) - Y-axis rotation
                if (step.inputType != InputType.Drag)
                {
                    anim.yRotation -= shuvit180Rotation;
                }
                break;

            case StickDirection.DownLeft:
                if (hasNolliePop)
                {
                    // Nollie kickflip - RS DownLeft = kickflip (Z-axis rotation)
                    anim.zRotation += flipRotation;
                }
                else
                {
                    // Varial kickflip setup - BS shuvit component (180 rotation)
                    anim.yRotation += shuvit180Rotation;
                }
                break;

            case StickDirection.DownRight:
                if (hasNolliePop)
                {
                    // Nollie heelflip - RS DownRight = heelflip (Z-axis rotation)
                    anim.zRotation -= flipRotation;
                }
                else
                {
                    // Varial heelflip setup - FS shuvit component (180 rotation)
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

            case StickDirection.UpLeft:
            case StickDirection.Left:
                // Kickflip - board flips toward heel side
                // Z-axis rotation (barrel roll along length of board)
                anim.zRotation += flipRotation;
                break;

            case StickDirection.UpRight:
            case StickDirection.Right:
                // Heelflip - board flips toward toe side
                // Z-axis rotation opposite direction
                anim.zRotation -= flipRotation;
                break;

            case StickDirection.Down:
                // Setup position
                break;

            case StickDirection.DownLeft:
                // Varial kickflip component - kickflip direction
                anim.zRotation += flipRotation;
                break;

            case StickDirection.DownRight:
                // Varial heelflip component - heelflip direction
                anim.zRotation -= flipRotation;
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
