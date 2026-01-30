using UnityEngine;

/// <summary>
/// Represents a single input event recorded in the buffer
/// </summary>
public struct RecordedInput
{
    public float timestamp;
    public StickType stickType;
    public StickDirection direction;         // Starting direction
    public StickDirection dragEndDirection;  // Ending direction (for drags)
    public StickDirection previousDirection; // Previous direction during drag (for tracking rotation)
    public float accumulatedRotation;        // Total rotation accumulated during drag (degrees, + = CCW, - = CW)
    public DragTurnType dragTurnType;        // Calculated turn type based on accumulated rotation
    public TriggerButton trigger;
    public FaceButton faceButton;
    public ShoulderButton shoulder;
    public StickDirection leftStickHeld;     // Left stick direction held during input
    public StickDirection rightStickHeld;    // Right stick direction held during input
    public bool isHeld;        // Is this a hold or just a tap?
    public float holdDuration;  // How long has it been held?
    public InputType inputType; // The classified input type (Tap, Hold, Flick, Drag)

    // Default thresholds for input type classification (can be overridden via TrickInputSystem)
    public const float DEFAULT_FLICK_THRESHOLD = 0.08f;  // Under this = Flick
    public const float DEFAULT_TAP_THRESHOLD = 0.2f;     // Under this = Tap, over = Hold

    public RecordedInput(float time, StickType stick, StickDirection dir,
                         TriggerButton trig = TriggerButton.None,
                         FaceButton face = FaceButton.None,
                         ShoulderButton sh = ShoulderButton.None,
                         StickDirection leftHeld = StickDirection.None,
                         StickDirection rightHeld = StickDirection.None)
    {
        timestamp = time;
        stickType = stick;
        direction = dir;
        dragEndDirection = StickDirection.None;
        previousDirection = dir;
        accumulatedRotation = 0f;
        dragTurnType = DragTurnType.None;
        trigger = trig;
        faceButton = face;
        shoulder = sh;
        leftStickHeld = leftHeld;
        rightStickHeld = rightHeld;
        isHeld = false;
        holdDuration = 0f;
        inputType = InputType.Tap; // Default, will be updated on release
    }

    /// <summary>
    /// Updates the drag with a new direction, tracking rotation
    /// </summary>
    public void UpdateDragDirection(StickDirection newDirection)
    {
        if (newDirection == StickDirection.None || newDirection == previousDirection)
            return;

        // Calculate rotation from previous to new direction
        float rotation = CalculateRotation(previousDirection, newDirection);
        accumulatedRotation += rotation;

        previousDirection = newDirection;
        dragEndDirection = newDirection;

        // Update turn type based on accumulated rotation
        UpdateDragTurnType();
    }

    /// <summary>
    /// Calculates the shortest rotation between two directions
    /// Returns positive for CCW (left), negative for CW (right)
    /// </summary>
    private float CalculateRotation(StickDirection from, StickDirection to)
    {
        if (from == StickDirection.None || to == StickDirection.None)
            return 0f;

        // Direction values are 1-8, representing 45 degree increments
        // 1=Up, 2=UpRight, 3=Right, 4=DownRight, 5=Down, 6=DownLeft, 7=Left, 8=UpLeft
        int fromVal = (int)from;
        int toVal = (int)to;

        // Calculate the difference (each step is 45 degrees)
        int diff = toVal - fromVal;

        // Normalize to -4 to +4 range (shortest path)
        if (diff > 4) diff -= 8;
        if (diff < -4) diff += 8;

        // Positive diff = CW (right), we want CCW to be positive
        // So negate: CCW (left) = positive, CW (right) = negative
        return -diff * 45f;
    }

    /// <summary>
    /// Updates the drag turn type based on accumulated rotation
    /// </summary>
    private void UpdateDragTurnType()
    {
        // Round to nearest 90 degree increment for classification
        float absRotation = Mathf.Abs(accumulatedRotation);

        if (absRotation < 45f)
        {
            dragTurnType = DragTurnType.None;
        }
        else if (accumulatedRotation > 0) // CCW (Backside)
        {
            if (absRotation >= 315f)
                dragTurnType = DragTurnType.CCW_Full;
            else if (absRotation >= 225f)
                dragTurnType = DragTurnType.CCW_ThreeQuarter;
            else if (absRotation >= 135f)
                dragTurnType = DragTurnType.CCW_Half;
            else
                dragTurnType = DragTurnType.CCW_Quarter;
        }
        else // CW (Frontside)
        {
            if (absRotation >= 315f)
                dragTurnType = DragTurnType.CW_Full;
            else if (absRotation >= 225f)
                dragTurnType = DragTurnType.CW_ThreeQuarter;
            else if (absRotation >= 135f)
                dragTurnType = DragTurnType.CW_Half;
            else
                dragTurnType = DragTurnType.CW_Quarter;
        }
    }

    /// <summary>
    /// Determines the input type based on hold duration and direction changes
    /// </summary>
    public void ClassifyInputType(float flickThreshold = DEFAULT_FLICK_THRESHOLD, float tapThreshold = DEFAULT_TAP_THRESHOLD)
    {
        // If direction changed during input, it's a drag
        if (dragEndDirection != StickDirection.None && dragEndDirection != direction)
        {
            inputType = InputType.Drag;
        }
        else if (holdDuration < flickThreshold)
        {
            inputType = InputType.Flick;
        }
        else if (holdDuration < tapThreshold)
        {
            inputType = InputType.Tap;
        }
        else
        {
            inputType = InputType.Hold;
        }
    }

    public override string ToString()
    {
        string result = $"[{timestamp:F2}s] {stickType.ToReadableString()} {direction.ToReadableString()}";

        // Show drag info
        if (inputType == InputType.Drag || (dragEndDirection != StickDirection.None && dragEndDirection != direction))
        {
            if (dragTurnType != DragTurnType.None)
                result += $" ({dragTurnType.ToReadableString()})";
            else
                result += $"->{dragEndDirection.ToReadableString()}";
        }

        if (trigger != TriggerButton.None)
            result += $" + {trigger}";
        if (faceButton != FaceButton.None)
            result += $" + {faceButton}";
        if (shoulder != ShoulderButton.None)
            result += $" + {shoulder}";
        if (leftStickHeld != StickDirection.None)
            result += $" [LS:{leftStickHeld.ToReadableString()}]";
        if (rightStickHeld != StickDirection.None)
            result += $" [RS:{rightStickHeld.ToReadableString()}]";

        // Show input type
        string typeStr = inputType switch
        {
            InputType.Flick => "Flick",
            InputType.Tap => "Tap",
            InputType.Hold => "Hold",
            InputType.Drag => "Drag",
            _ => "?"
        };

        if (isHeld)
            result += $" ({typeStr}ing {holdDuration:F2}s)";
        else
            result += $" ({typeStr} {holdDuration:F2}s)";

        return result;
    }
}
