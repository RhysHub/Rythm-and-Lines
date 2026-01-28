using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Result of a trick match attempt
/// </summary>
public class TrickMatchResult
{
    public bool matched;
    public TrickDefinition trick;
    public float accuracy;  // 0-1, how accurate the timing was
    public List<RecordedInput> matchedInputs;

    public TrickMatchResult()
    {
        matched = false;
        trick = null;
        accuracy = 0f;
        matchedInputs = new List<RecordedInput>();
    }
}

/// <summary>
/// Matches input sequences against trick definitions
/// </summary>
public class TrickMatcher
{
    private List<TrickDefinition> trickDatabase = new List<TrickDefinition>();

    // Tolerance for matching inputs
    private float directionTolerance = 15f; // Degrees of tolerance for diagonal inputs
    private float timingTolerance = 0.05f; // Timing tolerance for sequence steps

    public TrickMatcher()
    {
    }

    /// <summary>
    /// Sets the trick database to match against
    /// </summary>
    public void SetTrickDatabase(List<TrickDefinition> tricks)
    {
        trickDatabase = tricks;
    }

    /// <summary>
    /// Attempts to match recent inputs against all tricks in the database
    /// Returns the best matching trick, or null if no match
    /// Prioritizes longer input sequences over shorter ones (e.g., 360 flip over kickflip)
    /// </summary>
    public TrickMatchResult MatchTrick(List<RecordedInput> recentInputs, float timeWindow)
    {
        if (recentInputs == null || recentInputs.Count == 0)
            return new TrickMatchResult();

        TrickMatchResult bestMatch = new TrickMatchResult();
        int bestInputCount = 0;
        float bestScore = 0f;

        // Try to match against each trick
        foreach (var trick in trickDatabase)
        {
            if (!trick.IsValid())
                continue;

            var result = TryMatchTrick(trick, recentInputs, timeWindow);

            if (result.matched)
            {
                int inputCount = trick.inputSequence.Count;

                // Prefer longer input sequences (more complex tricks)
                // Only use accuracy as tiebreaker for equal-length sequences
                if (inputCount > bestInputCount ||
                    (inputCount == bestInputCount && result.accuracy > bestScore))
                {
                    bestInputCount = inputCount;
                    bestScore = result.accuracy;
                    bestMatch = result;
                }
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// Attempts to match a specific trick against the input sequence
    /// </summary>
    private TrickMatchResult TryMatchTrick(TrickDefinition trick, List<RecordedInput> inputs, float timeWindow)
    {
        var result = new TrickMatchResult();
        result.trick = trick;

        // Need at least as many inputs as the trick requires
        if (inputs.Count < trick.inputSequence.Count)
        {
            return result;
        }

        // Get the most recent inputs matching the trick's step count
        int startIndex = inputs.Count - trick.inputSequence.Count;
        List<RecordedInput> candidateInputs = new List<RecordedInput>();

        for (int i = startIndex; i < inputs.Count; i++)
        {
            candidateInputs.Add(inputs[i]);
        }

        // Check if the entire sequence happened within the allowed time
        float sequenceTime = candidateInputs[candidateInputs.Count - 1].timestamp - candidateInputs[0].timestamp;
        if (sequenceTime > trick.maxSequenceTime)
        {
            return result;
        }

        // Try to match each step
        float totalAccuracy = 0f;
        int matchedSteps = 0;

        for (int i = 0; i < trick.inputSequence.Count; i++)
        {
            InputStep requiredStep = trick.inputSequence[i];
            RecordedInput actualInput = candidateInputs[i];

            float stepAccuracy = MatchInputStep(requiredStep, actualInput);

            if (stepAccuracy > 0.5f) // Threshold for matching
            {
                matchedSteps++;
                totalAccuracy += stepAccuracy;
                result.matchedInputs.Add(actualInput);
            }
            else
            {
                // Step didn't match, trick failed
                return result;
            }
        }

        // All steps matched!
        if (matchedSteps == trick.inputSequence.Count)
        {
            result.matched = true;
            result.accuracy = totalAccuracy / matchedSteps;
        }

        return result;
    }

    /// <summary>
    /// Matches a single input step, returns accuracy score 0-1
    /// </summary>
    private float MatchInputStep(InputStep required, RecordedInput actual)
    {
        float score = 1.0f;

        // Check stick type match
        if (required.stickType != actual.stickType)
        {
            return 0f; // Wrong stick used
        }

        // Check direction match
        if (required.direction != actual.direction)
        {
            // For now, require exact direction match
            // Could add fuzzy matching for diagonals later
            return 0f;
        }

        // Check trigger match
        if (required.requiredTrigger != TriggerButton.None &&
            required.requiredTrigger != actual.trigger)
        {
            return 0f;
        }

        // Check face button match
        if (required.requiredFaceButton != FaceButton.None &&
            required.requiredFaceButton != actual.faceButton)
        {
            return 0f;
        }

        // Check shoulder button match
        if (required.requiredShoulder != ShoulderButton.None &&
            required.requiredShoulder != actual.shoulder)
        {
            return 0f;
        }

        // Check required held left stick direction
        if (required.requiredLeftStickHeld != StickDirection.None &&
            required.requiredLeftStickHeld != actual.leftStickHeld)
        {
            return 0f;
        }

        // Check required held right stick direction
        if (required.requiredRightStickHeld != StickDirection.None &&
            required.requiredRightStickHeld != actual.rightStickHeld)
        {
            return 0f;
        }

        // Check input type matching
        if (required.inputType == InputType.Hold)
        {
            if (actual.inputType != InputType.Hold || actual.holdDuration < required.minHoldDuration)
            {
                score *= 0.5f; // Penalize if hold wasn't long enough
            }
        }
        else if (required.inputType == InputType.Drag)
        {
            // For drags, verify the input was classified as a drag and end direction matches
            if (actual.inputType != InputType.Drag)
            {
                return 0f; // Not a drag input
            }
            if (actual.dragEndDirection != required.dragEndDirection)
            {
                return 0f; // Drag ended in wrong direction
            }
        }
        else if (required.inputType == InputType.Flick)
        {
            // For flicks, the input should be quick (Flick or Tap are acceptable)
            if (actual.inputType == InputType.Hold || actual.inputType == InputType.Drag)
            {
                score *= 0.5f; // Penalize if it was held too long
            }
        }
        // For Tap, any quick input (Tap or Flick) is acceptable

        return score;
    }

    /// <summary>
    /// Finds all tricks matching a specific category
    /// </summary>
    public List<TrickDefinition> GetTricksByCategory(TrickCategory category)
    {
        List<TrickDefinition> matches = new List<TrickDefinition>();

        foreach (var trick in trickDatabase)
        {
            if (trick.category == category)
            {
                matches.Add(trick);
            }
        }

        return matches;
    }

    /// <summary>
    /// Gets a trick by name
    /// </summary>
    public TrickDefinition GetTrickByName(string trickName)
    {
        foreach (var trick in trickDatabase)
        {
            if (trick.trickName.Equals(trickName, System.StringComparison.OrdinalIgnoreCase))
            {
                return trick;
            }
        }

        return null;
    }
}
