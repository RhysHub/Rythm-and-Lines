using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main system that manages trick input detection and matching
/// </summary>
[RequireComponent(typeof(InputReader))]
public class TrickInputSystem : MonoBehaviour
{
    [Header("Trick Database")]
    [Tooltip("All available trick definitions")]
    public List<TrickDefinition> trickDatabase = new List<TrickDefinition>();

    [Header("Timing Settings")]
    [Tooltip("Time window for matching trick sequences (seconds)")]
    [Range(0.01f, 1.0f)]
    public float matchingTimeWindow = 0.5f;

    [Tooltip("Input buffer duration (how long to keep inputs)")]
    [Range(0.01f, 1.0f)]
    public float bufferDuration = 1.0f;

    [Header("Input Classification")]
    [Tooltip("Max duration (seconds) for input to be classified as Flick")]
    [Range(0.01f, 0.2f)]
    public float flickThreshold = 0.08f;

    [Tooltip("Max duration (seconds) for input to be classified as Tap (above this = Hold)")]
    [Range(0.1f, 0.5f)]
    public float tapThreshold = 0.2f;

    [Tooltip("Delay after last input before confirming a trick (allows time for longer sequences)")]
    [Range(0f, 0.5f)]
    public float confirmationDelay = 0.1f;

    [Header("Debug")]
    [Tooltip("Show debug info in console")]
    public bool debugMode = true;

    [Tooltip("Show debug UI on screen")]
    public bool showDebugUI = true;

    [Tooltip("Show trick result popup in center of screen (disable if using TrickTimingUI)")]
    public bool showCenterPopup = true;

    [Tooltip("Show visual stick position indicators")]
    public bool showStickIndicators = true;

    [Tooltip("Size of stick indicator circles")]
    public float stickIndicatorSize = 80f;

    // Components
    private InputReader inputReader;
    private InputBuffer inputBuffer;
    private TrickMatcher trickMatcher;

    // State
    private TrickMatchResult lastMatchedTrick;
    private float lastMatchTime;
    private float lastInputTime;

    // Pending match state (for confirmation delay)
    private TrickMatchResult pendingMatch;
    private float pendingMatchTime;

    // Events
    public System.Action<TrickMatchResult> OnTrickMatched;
    public System.Action<TrickDefinition> OnTrickFailed;

    private void Awake()
    {
        inputReader = GetComponent<InputReader>();
        inputBuffer = new InputBuffer(bufferDuration);
        inputBuffer.SetThresholds(flickThreshold, tapThreshold);
        trickMatcher = new TrickMatcher();

        // Load trick database
        if (trickDatabase.Count > 0)
        {
            trickMatcher.SetTrickDatabase(trickDatabase);
            if (debugMode)
                Debug.Log($"Loaded {trickDatabase.Count} tricks into database");
        }
        else
        {
            Debug.LogWarning("No tricks in database! Add TrickDefinition assets to the trickDatabase list.");
        }
    }

    private void OnEnable()
    {
        // Subscribe to input events
        inputReader.OnStickInput += HandleStickInput;
        inputReader.OnStickReleased += HandleStickReleased;
    }

    private void OnDisable()
    {
        // Unsubscribe from input events
        inputReader.OnStickInput -= HandleStickInput;
        inputReader.OnStickReleased -= HandleStickReleased;
    }

    private void Update()
    {
        // Sync thresholds in case they changed in Inspector at runtime
        inputBuffer.SetThresholds(flickThreshold, tapThreshold);

        // Continuously try to match tricks as inputs come in
        TryMatchCurrentInputs();
    }

    /// <summary>
    /// Called when a stick input is detected
    /// </summary>
    private void HandleStickInput(StickType stickType, StickDirection direction)
    {
        // Record the input with current button states and held stick directions
        TriggerButton trigger = inputReader.GetCurrentTrigger();
        FaceButton faceButton = inputReader.GetCurrentFaceButton();
        ShoulderButton shoulder = inputReader.GetCurrentShoulder();
        StickDirection leftStickHeld = inputReader.GetCurrentLeftStickDirection();
        StickDirection rightStickHeld = inputReader.GetCurrentRightStickDirection();

        inputBuffer.RecordInput(stickType, direction, trigger, faceButton, shoulder, leftStickHeld, rightStickHeld);
        lastInputTime = Time.time;

        if (debugMode)
        {
            Debug.Log($"Input: {stickType.ToReadableString()} {direction.ToReadableString()} " +
                     $"(Trig: {trigger}, Face: {faceButton}, Shoulder: {shoulder}, " +
                     $"LS: {leftStickHeld.ToReadableString()}, RS: {rightStickHeld.ToReadableString()})");
        }
    }

    /// <summary>
    /// Called when stick returns to neutral
    /// </summary>
    private void HandleStickReleased(StickType stickType)
    {
        inputBuffer.RecordRelease();

        if (debugMode)
        {
            Debug.Log($"{stickType.ToReadableString()} released");
        }
    }

    /// <summary>
    /// Attempts to match current inputs against trick database
    /// Uses a pending match system to allow longer tricks to override shorter ones
    /// </summary>
    private void TryMatchCurrentInputs()
    {
        // Don't match too frequently after a confirmed match
        if (Time.time - lastMatchTime < 0.1f)
            return;

        var recentInputs = inputBuffer.GetRecentInputs(matchingTimeWindow);

        if (recentInputs.Count == 0)
        {
            // No inputs - check if we should confirm pending match
            ConfirmPendingMatchIfReady();
            return;
        }

        var result = trickMatcher.MatchTrick(recentInputs, matchingTimeWindow);

        if (result.matched)
        {
            // Check if this is a better match than pending (more inputs = better)
            bool shouldUpdatePending = false;

            if (pendingMatch == null || !pendingMatch.matched)
            {
                // No pending match, store this one
                shouldUpdatePending = true;
            }
            else if (result.trick.inputSequence.Count > pendingMatch.trick.inputSequence.Count)
            {
                // New match has more inputs (longer trick), replace pending
                shouldUpdatePending = true;

                if (debugMode)
                {
                    Debug.Log($"<color=yellow>Upgrading pending match: {pendingMatch.trick.trickName} -> {result.trick.trickName}</color>");
                }
            }
            else if (result.trick == pendingMatch.trick && result.accuracy > pendingMatch.accuracy)
            {
                // Same trick but better accuracy, update it
                shouldUpdatePending = true;
            }

            if (shouldUpdatePending)
            {
                pendingMatch = result;
                pendingMatchTime = Time.time;

                if (debugMode)
                {
                    Debug.Log($"<color=cyan>Pending match: {result.trick.trickName}</color> (waiting {confirmationDelay}s to confirm)");
                }
            }
        }

        // Check if we should confirm the pending match
        ConfirmPendingMatchIfReady();
    }

    /// <summary>
    /// Confirms the pending match if the confirmation delay has passed
    /// </summary>
    private void ConfirmPendingMatchIfReady()
    {
        if (pendingMatch == null || !pendingMatch.matched)
            return;

        // Check if confirmation delay has passed since the pending match was found
        if (Time.time - pendingMatchTime < confirmationDelay)
            return;

        // Avoid matching the same trick multiple times in quick succession
        if (lastMatchedTrick != null &&
            lastMatchedTrick.trick == pendingMatch.trick &&
            Time.time - lastMatchTime < 0.5f)
        {
            pendingMatch = null;
            return;
        }

        // Confirm the pending match
        lastMatchedTrick = pendingMatch;
        lastMatchTime = Time.time;

        if (debugMode)
        {
            Debug.Log($"<color=green>TRICK CONFIRMED: {pendingMatch.trick.trickName}</color> " +
                     $"(Accuracy: {pendingMatch.accuracy:P0})");
        }

        OnTrickMatched?.Invoke(pendingMatch);

        // Mark the trick in the buffer (don't clear - keep history visible)
        inputBuffer.MarkTrickConfirmed(pendingMatch.trick.trickName, pendingMatch.trick.inputSequence.Count);
        pendingMatch = null;
    }

    /// <summary>
    /// Manually check if a specific trick can be performed with current inputs
    /// </summary>
    public bool CanPerformTrick(TrickDefinition trick)
    {
        var recentInputs = inputBuffer.GetRecentInputs(matchingTimeWindow);
        var result = trickMatcher.MatchTrick(recentInputs, matchingTimeWindow);
        return result.matched && result.trick == trick;
    }

    /// <summary>
    /// Get all tricks in a specific category
    /// </summary>
    public List<TrickDefinition> GetTricksByCategory(TrickCategory category)
    {
        return trickMatcher.GetTricksByCategory(category);
    }

    /// <summary>
    /// Clears the input buffer
    /// </summary>
    public void ClearInputBuffer()
    {
        inputBuffer.Clear();
    }

    /// <summary>
    /// Gets debug info string
    /// </summary>
    public string GetDebugInfo()
    {
        string info = $"Trick Input System\n";
        info += $"Tricks Loaded: {trickDatabase.Count}\n";
        info += $"Matching Window: {matchingTimeWindow}s\n\n";
        info += inputBuffer.GetDebugString(5);

        if (pendingMatch != null && pendingMatch.matched)
        {
            float timeLeft = confirmationDelay - (Time.time - pendingMatchTime);
            info += $"\nPending: {pendingMatch.trick.trickName} ({timeLeft:F2}s)";
        }

        if (lastMatchedTrick != null && lastMatchedTrick.matched)
        {
            info += $"\nLast Matched: {lastMatchedTrick.trick.trickName} " +
                   $"({lastMatchedTrick.accuracy:P0})";
        }

        return info;
    }

    // Debug UI
    private void OnGUI()
    {
        if (!showDebugUI)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 14;
        style.normal.textColor = Color.white;

        string debugInfo = GetDebugInfo();
        GUI.Box(new Rect(10, 10, 400, 250), debugInfo, style);

        // Copy to clipboard button
        if (GUI.Button(new Rect(10, 265, 150, 30), "Copy Input History"))
        {
            string fullHistory = GetFullInputHistory();
            GUIUtility.systemCopyBuffer = fullHistory;
            Debug.Log("Input history copied to clipboard!");
        }

        // Show last matched trick in green (can be disabled when using TrickTimingUI)
        if (showCenterPopup && lastMatchedTrick != null && lastMatchedTrick.matched)
        {
            if (Time.time - lastMatchTime < 2.0f) // Show for 2 seconds
            {
                GUIStyle trickStyle = new GUIStyle(GUI.skin.box);
                trickStyle.fontSize = 24;
                trickStyle.alignment = TextAnchor.MiddleCenter;
                trickStyle.normal.textColor = Color.green;

                string trickText = $"{lastMatchedTrick.trick.trickName}\n" +
                                  $"{lastMatchedTrick.trick.GetInputSequenceString()}";

                GUI.Box(new Rect(Screen.width / 2 - 200, 100, 400, 100), trickText, trickStyle);
            }
        }

        // Draw stick position indicators
        if (showStickIndicators && inputReader != null)
        {
            DrawStickIndicators();
        }
    }

    /// <summary>
    /// Draws visual stick position indicators
    /// </summary>
    private void DrawStickIndicators()
    {
        float spacing = 20f;
        float bottomMargin = 150f;
        float size = stickIndicatorSize;

        // Position sticks at bottom of screen, centered
        float totalWidth = size * 2 + spacing;
        float startX = (Screen.width - totalWidth) / 2f;
        float posY = Screen.height - bottomMargin - size;

        // Get current stick directions and raw positions
        StickDirection leftDir = inputReader.GetCurrentLeftStickDirection();
        StickDirection rightDir = inputReader.GetCurrentRightStickDirection();
        Vector2 leftRaw = inputReader.GetRawLeftStick();
        Vector2 rightRaw = inputReader.GetRawRightStick();

        // Draw left stick
        DrawSingleStick(startX, posY, size, leftDir, leftRaw, "LS");

        // Draw right stick
        DrawSingleStick(startX + size + spacing, posY, size, rightDir, rightRaw, "RS");
    }

    /// <summary>
    /// Draws a single stick indicator
    /// </summary>
    private void DrawSingleStick(float x, float y, float size, StickDirection direction, Vector2 rawInput, string label)
    {
        float centerX = x + size / 2f;
        float centerY = y + size / 2f;
        float radius = size / 2f - 5f;

        // Draw background circle
        GUI.color = new Color(0, 0, 0, 0.7f);
        DrawCircle(centerX, centerY, radius + 5f);

        // Draw outer ring
        GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        DrawCircle(centerX, centerY, radius);

        // Draw deadzone circle
        GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.3f);
        DrawCircle(centerX, centerY, radius * inputReader.deadzone);

        // Draw 8-way direction markers
        GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        for (int i = 1; i <= 8; i++)
        {
            Vector2 dir = ((StickDirection)i).ToVector2();
            float markerX = centerX + dir.x * (radius - 10f);
            float markerY = centerY - dir.y * (radius - 10f); // Y is flipped in GUI
            DrawCircle(markerX, markerY, 4f);
        }

        // Draw center point
        GUI.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        DrawCircle(centerX, centerY, 6f);

        // Draw RAW position indicator (yellow dot that follows exact stick position)
        float rawX = centerX + rawInput.x * (radius - 5f);
        float rawY = centerY - rawInput.y * (radius - 5f); // Y is flipped in GUI

        // Draw line from center to raw position
        if (rawInput.magnitude > 0.01f)
        {
            GUI.color = new Color(1f, 1f, 0f, 0.5f);
            DrawLine(centerX, centerY, rawX, rawY, 2f);
        }

        // Draw raw position dot
        GUI.color = Color.yellow;
        DrawCircle(rawX, rawY, 8f);

        // Draw detected 8-way direction indicator (green, larger)
        if (direction != StickDirection.None)
        {
            Vector2 dir = direction.ToVector2();
            float indicatorX = centerX + dir.x * (radius - 15f);
            float indicatorY = centerY - dir.y * (radius - 15f); // Y is flipped in GUI

            // Draw direction indicator ring
            GUI.color = new Color(0f, 1f, 0f, 0.5f);
            DrawCircle(indicatorX, indicatorY, 14f);

            // Draw direction arrow/symbol
            GUI.color = Color.green;
            GUIStyle dirStyle = new GUIStyle(GUI.skin.label);
            dirStyle.fontSize = 18;
            dirStyle.alignment = TextAnchor.MiddleCenter;
            dirStyle.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(indicatorX - 15, indicatorY - 12, 30, 24), direction.ToReadableString(), dirStyle);
        }

        // Draw label and raw values
        GUI.color = Color.white;
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(x, y + size + 5, size, 20), label, labelStyle);

        // Show raw X,Y values
        GUIStyle valueStyle = new GUIStyle(GUI.skin.label);
        valueStyle.fontSize = 11;
        valueStyle.alignment = TextAnchor.MiddleCenter;
        valueStyle.normal.textColor = Color.yellow;

        GUI.Label(new Rect(x, y + size + 22, size, 16), $"X:{rawInput.x:F2} Y:{rawInput.y:F2}", valueStyle);
    }

    /// <summary>
    /// Draws a filled circle using GUI
    /// </summary>
    private void DrawCircle(float x, float y, float radius)
    {
        // Use a texture for the circle (simple box approximation for now)
        GUI.DrawTexture(new Rect(x - radius, y - radius, radius * 2, radius * 2), GetCircleTexture(), ScaleMode.StretchToFill);
    }

    /// <summary>
    /// Draws a line between two points
    /// </summary>
    private void DrawLine(float x1, float y1, float x2, float y2, float width)
    {
        Vector2 start = new Vector2(x1, y1);
        Vector2 end = new Vector2(x2, y2);
        Vector2 dir = (end - start).normalized;
        float length = Vector2.Distance(start, end);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GUIUtility.RotateAroundPivot(angle, start);
        GUI.DrawTexture(new Rect(x1, y1 - width / 2, length, width), Texture2D.whiteTexture);
        GUIUtility.RotateAroundPivot(-angle, start);
    }

    // Cached circle texture
    private Texture2D circleTexture;

    /// <summary>
    /// Gets or creates a circle texture
    /// </summary>
    private Texture2D GetCircleTexture()
    {
        if (circleTexture == null)
        {
            int size = 64;
            circleTexture = new Texture2D(size, size);
            float radius = size / 2f;
            Color transparent = new Color(1, 1, 1, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    if (dist < radius - 1)
                        circleTexture.SetPixel(x, y, Color.white);
                    else if (dist < radius)
                        circleTexture.SetPixel(x, y, new Color(1, 1, 1, radius - dist));
                    else
                        circleTexture.SetPixel(x, y, transparent);
                }
            }
            circleTexture.Apply();
        }
        return circleTexture;
    }

    /// <summary>
    /// Gets the full input history for copying to clipboard
    /// </summary>
    public string GetFullInputHistory()
    {
        string history = "=== Input History ===\n";
        history += $"Time: {System.DateTime.Now}\n";
        history += $"Flick Threshold: {flickThreshold}s\n";
        history += $"Tap Threshold: {tapThreshold}s\n";
        history += $"Confirmation Delay: {confirmationDelay}s\n";
        history += $"Matching Window: {matchingTimeWindow}s\n\n";
        history += inputBuffer.GetDebugString(20); // Get more inputs for clipboard

        if (pendingMatch != null && pendingMatch.matched)
        {
            history += $"\nPending: {pendingMatch.trick.trickName}";
        }

        if (lastMatchedTrick != null && lastMatchedTrick.matched)
        {
            history += $"\nLast Confirmed: {lastMatchedTrick.trick.trickName} ({lastMatchedTrick.accuracy:P0})";
        }

        return history;
    }
}
