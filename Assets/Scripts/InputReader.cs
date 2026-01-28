using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads input from both gamepad and keyboard, converting to StickDirection
/// </summary>
public class InputReader : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("Deadzone for stick input")]
    [Range(0.1f, 0.9f)]
    public float deadzone = 0.3f;

    [Tooltip("Threshold for detecting a flick (magnitude must exceed this)")]
    [Range(0.5f, 1.0f)]
    public float flickThreshold = 0.8f;

    [Header("Keyboard Settings")]
    [Tooltip("Enable keyboard input for testing")]
    public bool enableKeyboard = true;

    // Current input state
    private Vector2 rightStickInput;
    private Vector2 leftStickInput;
    private bool leftTriggerHeld;
    private bool rightTriggerHeld;
    private bool southButtonHeld;
    private bool eastButtonHeld;
    private bool westButtonHeld;
    private bool northButtonHeld;
    private bool leftShoulderHeld;
    private bool rightShoulderHeld;

    // Previous frame state for detecting changes
    private StickDirection previousRightDirection = StickDirection.None;
    private StickDirection previousLeftDirection = StickDirection.None;

    // Events
    public System.Action<StickType, StickDirection> OnStickInput;
    public System.Action<StickType> OnStickReleased;
    public System.Action<TriggerButton> OnTriggerPressed;
    public System.Action<TriggerButton> OnTriggerReleased;
    public System.Action<FaceButton> OnFaceButtonPressed;
    public System.Action<FaceButton> OnFaceButtonReleased;
    public System.Action<ShoulderButton> OnShoulderPressed;
    public System.Action<ShoulderButton> OnShoulderReleased;

    private void Update()
    {
        ReadInputs();
        ProcessStickInput();
        ProcessButtons();
    }

    private void ReadInputs()
    {
        // Read gamepad if available
        if (Gamepad.current != null)
        {
            rightStickInput = Gamepad.current.rightStick.ReadValue();
            leftStickInput = Gamepad.current.leftStick.ReadValue();

            leftTriggerHeld = Gamepad.current.leftTrigger.ReadValue() > 0.5f;
            rightTriggerHeld = Gamepad.current.rightTrigger.ReadValue() > 0.5f;

            southButtonHeld = Gamepad.current.buttonSouth.isPressed;
            eastButtonHeld = Gamepad.current.buttonEast.isPressed;
            westButtonHeld = Gamepad.current.buttonWest.isPressed;
            northButtonHeld = Gamepad.current.buttonNorth.isPressed;

            leftShoulderHeld = Gamepad.current.leftShoulder.isPressed;
            rightShoulderHeld = Gamepad.current.rightShoulder.isPressed;
        }

        // Read keyboard input if enabled
        if (enableKeyboard && Keyboard.current != null)
        {
            Vector2 keyboardRightStick = Vector2.zero;
            Vector2 keyboardLeftStick = Vector2.zero;

            // Arrow keys for right stick simulation
            if (Keyboard.current.upArrowKey.isPressed) keyboardRightStick.y = 1f;
            if (Keyboard.current.downArrowKey.isPressed) keyboardRightStick.y = -1f;
            if (Keyboard.current.leftArrowKey.isPressed) keyboardRightStick.x = -1f;
            if (Keyboard.current.rightArrowKey.isPressed) keyboardRightStick.x = 1f;

            // WASD for left stick simulation
            if (Keyboard.current.wKey.isPressed) keyboardLeftStick.y = 1f;
            if (Keyboard.current.sKey.isPressed) keyboardLeftStick.y = -1f;
            if (Keyboard.current.aKey.isPressed) keyboardLeftStick.x = -1f;
            if (Keyboard.current.dKey.isPressed) keyboardLeftStick.x = 1f;

            // Use keyboard input if gamepad isn't connected or not providing input
            if (Gamepad.current == null || rightStickInput.magnitude < deadzone)
            {
                rightStickInput = keyboardRightStick;
            }
            if (Gamepad.current == null || leftStickInput.magnitude < deadzone)
            {
                leftStickInput = keyboardLeftStick;
            }

            // Q/E for triggers
            if (Keyboard.current.qKey.isPressed) leftTriggerHeld = true;
            if (Keyboard.current.eKey.isPressed) rightTriggerHeld = true;

            // ZXCV for face buttons
            if (Keyboard.current.zKey.isPressed) southButtonHeld = true;
            if (Keyboard.current.xKey.isPressed) eastButtonHeld = true;
            if (Keyboard.current.cKey.isPressed) westButtonHeld = true;
            if (Keyboard.current.vKey.isPressed) northButtonHeld = true;

            // 1/2 for shoulders
            if (Keyboard.current.digit1Key.isPressed) leftShoulderHeld = true;
            if (Keyboard.current.digit2Key.isPressed) rightShoulderHeld = true;
        }
    }

    private void ProcessStickInput()
    {
        // Process Right Stick
        StickDirection currentRightDirection = rightStickInput.ToStickDirection(deadzone);
        if (currentRightDirection != previousRightDirection)
        {
            if (currentRightDirection != StickDirection.None)
            {
                OnStickInput?.Invoke(StickType.RightStick, currentRightDirection);
            }
            else
            {
                OnStickReleased?.Invoke(StickType.RightStick);
            }
            previousRightDirection = currentRightDirection;
        }

        // Process Left Stick
        StickDirection currentLeftDirection = leftStickInput.ToStickDirection(deadzone);
        if (currentLeftDirection != previousLeftDirection)
        {
            if (currentLeftDirection != StickDirection.None)
            {
                OnStickInput?.Invoke(StickType.LeftStick, currentLeftDirection);
            }
            else
            {
                OnStickReleased?.Invoke(StickType.LeftStick);
            }
            previousLeftDirection = currentLeftDirection;
        }
    }

    private void ProcessButtons()
    {
        // Trigger events for button presses/releases
        // (This is simplified - a full implementation would track previous state)
    }

    /// <summary>
    /// Gets the current stick direction for a specific stick
    /// </summary>
    public StickDirection GetCurrentStickDirection(StickType stickType)
    {
        if (stickType == StickType.RightStick)
            return rightStickInput.ToStickDirection(deadzone);
        else
            return leftStickInput.ToStickDirection(deadzone);
    }

    /// <summary>
    /// Gets the current right stick direction
    /// </summary>
    public StickDirection GetCurrentRightStickDirection()
    {
        return rightStickInput.ToStickDirection(deadzone);
    }

    /// <summary>
    /// Gets the current left stick direction
    /// </summary>
    public StickDirection GetCurrentLeftStickDirection()
    {
        return leftStickInput.ToStickDirection(deadzone);
    }

    /// <summary>
    /// Gets the raw left stick Vector2 input (-1 to 1)
    /// </summary>
    public Vector2 GetRawLeftStick()
    {
        return leftStickInput;
    }

    /// <summary>
    /// Gets the raw right stick Vector2 input (-1 to 1)
    /// </summary>
    public Vector2 GetRawRightStick()
    {
        return rightStickInput;
    }

    /// <summary>
    /// Gets the current trigger state
    /// </summary>
    public TriggerButton GetCurrentTrigger()
    {
        if (leftTriggerHeld && rightTriggerHeld)
            return TriggerButton.Both;
        else if (leftTriggerHeld)
            return TriggerButton.LeftTrigger;
        else if (rightTriggerHeld)
            return TriggerButton.RightTrigger;
        else
            return TriggerButton.None;
    }

    /// <summary>
    /// Gets the current face button state
    /// </summary>
    public FaceButton GetCurrentFaceButton()
    {
        if (southButtonHeld) return FaceButton.South;
        if (eastButtonHeld) return FaceButton.East;
        if (westButtonHeld) return FaceButton.West;
        if (northButtonHeld) return FaceButton.North;
        return FaceButton.None;
    }

    /// <summary>
    /// Gets the current shoulder button state
    /// </summary>
    public ShoulderButton GetCurrentShoulder()
    {
        if (leftShoulderHeld && rightShoulderHeld)
            return ShoulderButton.Both;
        else if (leftShoulderHeld)
            return ShoulderButton.LeftShoulder;
        else if (rightShoulderHeld)
            return ShoulderButton.RightShoulder;
        else
            return ShoulderButton.None;
    }

    /// <summary>
    /// Check if input magnitude exceeds flick threshold for a specific stick
    /// </summary>
    public bool IsFlickMagnitude(StickType stickType)
    {
        if (stickType == StickType.RightStick)
            return rightStickInput.magnitude >= flickThreshold;
        else
            return leftStickInput.magnitude >= flickThreshold;
    }
}
