using System;
using UnityEngine;

/// <summary>
/// Type of input action required for a trick step
/// </summary>
public enum InputType
{
    Tap,        // Quick directional tap (released quickly)
    Hold,       // Hold direction for duration
    Flick,      // Quick flick in direction then release
    Drag        // Hold and drag from one direction to another
}

/// <summary>
/// Represents a single input step in a trick sequence
/// </summary>
[Serializable]
public class InputStep
{
    [Tooltip("Which stick to use for this input")]
    public StickType stickType = StickType.RightStick;

    [Tooltip("The stick direction for this input")]
    public StickDirection direction;

    [Tooltip("Type of input (tap, hold, flick, drag)")]
    public InputType inputType;

    [Tooltip("For drags: the ending direction")]
    public StickDirection dragEndDirection;

    [Tooltip("For holds: minimum duration in seconds")]
    public float minHoldDuration = 0.1f;

    [Tooltip("Maximum time allowed to complete this step (seconds)")]
    public float maxStepTime = 0.15f;

    [Tooltip("Optional trigger button required with this input")]
    public TriggerButton requiredTrigger = TriggerButton.None;

    [Tooltip("Optional face button required with this input")]
    public FaceButton requiredFaceButton = FaceButton.None;

    [Tooltip("Optional shoulder button required with this input")]
    public ShoulderButton requiredShoulder = ShoulderButton.None;

    [Header("Required Held Directions")]
    [Tooltip("Required direction to be held on left stick during this input")]
    public StickDirection requiredLeftStickHeld = StickDirection.None;

    [Tooltip("Required direction to be held on right stick during this input")]
    public StickDirection requiredRightStickHeld = StickDirection.None;

    public InputStep() { }

    public InputStep(StickDirection dir, InputType type = InputType.Tap, StickType stick = StickType.RightStick)
    {
        stickType = stick;
        direction = dir;
        inputType = type;
    }

    public InputStep(StickDirection dir, TriggerButton trigger, StickType stick = StickType.RightStick)
    {
        stickType = stick;
        direction = dir;
        inputType = InputType.Hold;
        requiredTrigger = trigger;
    }

    /// <summary>
    /// Returns a readable string representation of this input step
    /// </summary>
    public override string ToString()
    {
        string result = $"{stickType.ToReadableString()} {direction.ToReadableString()}";

        if (requiredTrigger != TriggerButton.None)
            result += $" + {requiredTrigger}";
        if (requiredFaceButton != FaceButton.None)
            result += $" + {requiredFaceButton}";
        if (requiredShoulder != ShoulderButton.None)
            result += $" + {requiredShoulder}";
        if (requiredLeftStickHeld != StickDirection.None)
            result += $" + LS:{requiredLeftStickHeld.ToReadableString()}";
        if (requiredRightStickHeld != StickDirection.None)
            result += $" + RS:{requiredRightStickHeld.ToReadableString()}";

        if (inputType == InputType.Hold)
            result += " (hold)";
        else if (inputType == InputType.Drag)
            result += $"{dragEndDirection.ToReadableString()}";

        return result;
    }
}
