using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintains a rolling buffer of recent inputs for pattern matching
/// </summary>
public class InputBuffer
{
    private List<RecordedInput> buffer = new List<RecordedInput>();
    private List<TrickMarker> trickMarkers = new List<TrickMarker>(); // Track confirmed tricks
    private float bufferDuration = 1.0f; // Keep inputs for 1 second
    private RecordedInput? currentHeldInput = null;

    // Configurable thresholds for input classification
    public float flickThreshold = RecordedInput.DEFAULT_FLICK_THRESHOLD;
    public float tapThreshold = RecordedInput.DEFAULT_TAP_THRESHOLD;

    // Struct to track where tricks were confirmed in the buffer
    private struct TrickMarker
    {
        public float timestamp;
        public string trickName;
        public int inputCount; // How many inputs were part of this trick
    }

    public InputBuffer(float duration = 1.0f)
    {
        bufferDuration = duration;
    }

    /// <summary>
    /// Sets the input classification thresholds
    /// </summary>
    public void SetThresholds(float flick, float tap)
    {
        flickThreshold = flick;
        tapThreshold = tap;
    }

    /// <summary>
    /// Marks that a trick was confirmed at this point in the buffer
    /// </summary>
    public void MarkTrickConfirmed(string trickName, int inputCount)
    {
        trickMarkers.Add(new TrickMarker
        {
            timestamp = Time.time,
            trickName = trickName,
            inputCount = inputCount
        });
    }

    /// <summary>
    /// Records a new input event
    /// </summary>
    public void RecordInput(StickType stickType,
                           StickDirection direction,
                           TriggerButton trigger = TriggerButton.None,
                           FaceButton faceButton = FaceButton.None,
                           ShoulderButton shoulder = ShoulderButton.None,
                           StickDirection leftStickHeld = StickDirection.None,
                           StickDirection rightStickHeld = StickDirection.None)
    {
        float time = Time.time;

        // If this is the same input as the current held input, update hold duration
        if (currentHeldInput.HasValue &&
            currentHeldInput.Value.stickType == stickType &&
            currentHeldInput.Value.direction == direction &&
            currentHeldInput.Value.trigger == trigger &&
            currentHeldInput.Value.faceButton == faceButton &&
            currentHeldInput.Value.shoulder == shoulder)
        {
            var held = currentHeldInput.Value;
            held.holdDuration = time - held.timestamp;
            held.isHeld = true;
            currentHeldInput = held;

            // Update the last entry in buffer
            if (buffer.Count > 0)
            {
                buffer[buffer.Count - 1] = held;
            }
        }
        // If same stick but different direction, this is a drag
        // (Note: we don't check isHeld because OnStickInput only fires on direction changes,
        // so isHeld would never be true at the moment of direction change)
        else if (currentHeldInput.HasValue &&
                 currentHeldInput.Value.stickType == stickType &&
                 currentHeldInput.Value.direction != direction &&
                 direction != StickDirection.None)
        {
            var held = currentHeldInput.Value;
            held.holdDuration = time - held.timestamp;
            held.dragEndDirection = direction; // Track the new direction as drag end
            held.isHeld = true;
            currentHeldInput = held;

            // Update the last entry in buffer
            if (buffer.Count > 0)
            {
                buffer[buffer.Count - 1] = held;
            }
        }
        else
        {
            // New input
            var input = new RecordedInput(time, stickType, direction, trigger, faceButton, shoulder, leftStickHeld, rightStickHeld);
            buffer.Add(input);
            currentHeldInput = input;
        }

        CleanOldInputs();
    }

    /// <summary>
    /// Records that the stick/button was released
    /// </summary>
    public void RecordRelease()
    {
        if (currentHeldInput.HasValue)
        {
            var released = currentHeldInput.Value;
            released.holdDuration = Time.time - released.timestamp;
            released.isHeld = false;
            released.ClassifyInputType(flickThreshold, tapThreshold);

            if (buffer.Count > 0)
            {
                buffer[buffer.Count - 1] = released;
            }

            currentHeldInput = null;
        }
    }

    /// <summary>
    /// Gets all inputs within the specified time window
    /// </summary>
    public List<RecordedInput> GetRecentInputs(float timeWindow)
    {
        float cutoffTime = Time.time - timeWindow;
        List<RecordedInput> recent = new List<RecordedInput>();

        foreach (var input in buffer)
        {
            if (input.timestamp >= cutoffTime)
            {
                recent.Add(input);
            }
        }

        return recent;
    }

    /// <summary>
    /// Gets the most recent N inputs
    /// </summary>
    public List<RecordedInput> GetLastNInputs(int count)
    {
        int startIndex = Mathf.Max(0, buffer.Count - count);
        List<RecordedInput> recent = new List<RecordedInput>();

        for (int i = startIndex; i < buffer.Count; i++)
        {
            recent.Add(buffer[i]);
        }

        return recent;
    }

    /// <summary>
    /// Gets the current held input if any
    /// </summary>
    public RecordedInput? GetCurrentHeldInput()
    {
        return currentHeldInput;
    }

    /// <summary>
    /// Clears all recorded inputs
    /// </summary>
    public void Clear()
    {
        buffer.Clear();
        currentHeldInput = null;
    }

    /// <summary>
    /// Removes inputs older than the buffer duration
    /// </summary>
    private void CleanOldInputs()
    {
        float cutoffTime = Time.time - bufferDuration;
        buffer.RemoveAll(input => input.timestamp < cutoffTime);
        trickMarkers.RemoveAll(marker => marker.timestamp < cutoffTime);
    }

    /// <summary>
    /// Gets debug string showing recent inputs with trick markers
    /// </summary>
    public string GetDebugString(int maxInputs = 5)
    {
        var recent = GetLastNInputs(maxInputs);
        if (recent.Count == 0 && trickMarkers.Count == 0)
            return "No recent inputs";

        string result = "Recent inputs:\n";

        // Combine inputs and markers, sorted by timestamp
        List<(float time, string text, bool isMarker)> entries = new List<(float, string, bool)>();

        foreach (var input in recent)
        {
            entries.Add((input.timestamp, input.ToString(), false));
        }

        foreach (var marker in trickMarkers)
        {
            entries.Add((marker.timestamp, $"=== {marker.trickName} ===", true));
        }

        // Sort by timestamp
        entries.Sort((a, b) => a.time.CompareTo(b.time));

        // Take the last maxInputs entries (but always include markers)
        int startIndex = Mathf.Max(0, entries.Count - maxInputs - trickMarkers.Count);
        for (int i = startIndex; i < entries.Count; i++)
        {
            result += entries[i].text + "\n";
        }

        return result;
    }
}
