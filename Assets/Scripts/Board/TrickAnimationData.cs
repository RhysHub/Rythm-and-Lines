using UnityEngine;

/// <summary>
/// Data structure holding procedurally generated animation parameters for a trick.
/// </summary>
[System.Serializable]
public class TrickAnimationData
{
    [Header("Timing")]
    [Tooltip("Total animation duration in seconds")]
    public float duration = 0.5f;

    [Header("Pop/Height")]
    [Tooltip("Maximum height of the pop")]
    public float popHeight = 0.3f;

    [Tooltip("Whether this trick has a pop (airborne trick)")]
    public bool hasPop = true;

    [Header("Rotation (degrees)")]
    [Tooltip("X-axis rotation (kickflip/heelflip axis - board flips heel-to-toe)")]
    public float xRotation = 0f;

    [Tooltip("Y-axis rotation (shuvit axis - board spins horizontally)")]
    public float yRotation = 0f;

    [Tooltip("Z-axis rotation (roll axis - rarely used)")]
    public float zRotation = 0f;

    [Header("Metadata")]
    [Tooltip("Name of the trick being animated")]
    public string trickName;

    [Tooltip("Category of the trick")]
    public TrickCategory category;

    /// <summary>
    /// Creates a default animation data (simple ollie)
    /// </summary>
    public TrickAnimationData()
    {
        duration = 0.5f;
        popHeight = 0.3f;
        hasPop = true;
        xRotation = 0f;
        yRotation = 0f;
        zRotation = 0f;
        trickName = "";
        category = TrickCategory.Flip;
    }

    /// <summary>
    /// Creates animation data with specified rotation values
    /// </summary>
    public TrickAnimationData(float xRot, float yRot, float zRot = 0f)
    {
        duration = 0.5f;
        popHeight = 0.3f;
        hasPop = true;
        xRotation = xRot;
        yRotation = yRot;
        zRotation = zRot;
        trickName = "";
        category = TrickCategory.Flip;
    }

    /// <summary>
    /// Returns whether this trick has any rotation
    /// </summary>
    public bool HasRotation => !Mathf.Approximately(xRotation, 0f) ||
                               !Mathf.Approximately(yRotation, 0f) ||
                               !Mathf.Approximately(zRotation, 0f);

    /// <summary>
    /// Returns the total rotation as a Vector3 (in degrees)
    /// </summary>
    public Vector3 TotalRotation => new Vector3(xRotation, yRotation, zRotation);

    public override string ToString()
    {
        return $"{trickName}: Pop={hasPop}, Height={popHeight:F2}m, Rot=({xRotation:F0}, {yRotation:F0}, {zRotation:F0})";
    }
}
