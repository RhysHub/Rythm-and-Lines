using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Categories for grouping tricks
/// </summary>
public enum TrickCategory
{
    Flip,          // Kickflips, heelflips, etc.
    Shuvit,        // Pop-shuvits and variations
    Nollie,        // Nollie tricks
    Grind,         // Grinds and slides
    Grab,          // Grab tricks
    Special        // Handplants, footplants, etc.
}

/// <summary>
/// Stance requirement for trick execution
/// </summary>
public enum TrickStance
{
    Any,           // Can be performed in any stance
    Regular,       // Must be in regular stance
    Nollie,        // Must pop from nose
    Switch,        // Switch stance
    Fakie          // Fakie stance
}

/// <summary>
/// Defines a complete skateboard trick with its input sequence
/// </summary>
[CreateAssetMenu(fileName = "New Trick", menuName = "Skate/Trick Definition")]
public class TrickDefinition : ScriptableObject
{
    [Header("Trick Info")]
    [Tooltip("Name of the trick")]
    public string trickName;

    [Tooltip("Category this trick belongs to")]
    public TrickCategory category;

    [Tooltip("Required stance to perform this trick")]
    public TrickStance requiredStance = TrickStance.Any;

    [Tooltip("Difficulty score (used for points)")]
    [Range(1, 10)]
    public int difficulty = 1;

    [Header("Input Sequence")]
    [Tooltip("The sequence of inputs required to perform this trick")]
    public List<InputStep> inputSequence = new List<InputStep>();

    [Tooltip("Maximum time to complete entire sequence (seconds)")]
    public float maxSequenceTime = 0.4f;

    [Header("Execution Requirements")]
    [Tooltip("Can only be performed in the air")]
    public bool requiresAirborne = false;

    [Tooltip("Can only be performed when grounded")]
    public bool requiresGrounded = false;

    [Tooltip("Requires contact with a rail/ledge")]
    public bool requiresGrindSurface = false;

    /// <summary>
    /// Returns a readable string of the input sequence
    /// </summary>
    public string GetInputSequenceString()
    {
        if (inputSequence == null || inputSequence.Count == 0)
            return "No inputs defined";

        string result = "";
        for (int i = 0; i < inputSequence.Count; i++)
        {
            result += inputSequence[i].ToString();
            if (i < inputSequence.Count - 1)
                result += " > ";
        }
        return result;
    }

    /// <summary>
    /// Validates that the trick definition is properly set up
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(trickName))
        {
            Debug.LogWarning($"Trick definition has no name: {name}");
            return false;
        }

        if (inputSequence == null || inputSequence.Count == 0)
        {
            Debug.LogWarning($"Trick '{trickName}' has no input sequence defined");
            return false;
        }

        return true;
    }

    private void OnValidate()
    {
        // Auto-validation in editor
        IsValid();
    }
}
