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

        // Show drag end direction if it's a drag
        if (inputType == InputType.Drag || (dragEndDirection != StickDirection.None && dragEndDirection != direction))
        {
            result += $"{dragEndDirection.ToReadableString()}";
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
