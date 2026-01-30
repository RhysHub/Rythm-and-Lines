using UnityEngine;

/// <summary>
/// Predefined drag turn types based on direction and rotation amount.
/// Clockwise (CW) = Right/Frontside direction
/// Counter-Clockwise (CCW) = Left/Backside direction
/// </summary>
public enum DragTurnType
{
    None = 0,

    // Counter-Clockwise (Left/Backside) turns
    CCW_Quarter = 1,    // 90 degrees left (e.g., Down -> Left)
    CCW_Half = 2,       // 180 degrees left (e.g., Down -> Up via Left)
    CCW_ThreeQuarter = 3, // 270 degrees left
    CCW_Full = 4,       // 360 degrees left (full rotation)

    // Clockwise (Right/Frontside) turns
    CW_Quarter = 5,     // 90 degrees right (e.g., Down -> Right)
    CW_Half = 6,        // 180 degrees right (e.g., Down -> Up via Right)
    CW_ThreeQuarter = 7, // 270 degrees right
    CW_Full = 8         // 360 degrees right (full rotation)
}

/// <summary>
/// Extension methods for DragTurnType
/// </summary>
public static class DragTurnTypeExtensions
{
    /// <summary>
    /// Returns a readable string for the turn type
    /// </summary>
    public static string ToReadableString(this DragTurnType turnType)
    {
        return turnType switch
        {
            DragTurnType.CCW_Quarter => "CCW 90",
            DragTurnType.CCW_Half => "CCW 180",
            DragTurnType.CCW_ThreeQuarter => "CCW 270",
            DragTurnType.CCW_Full => "CCW 360",
            DragTurnType.CW_Quarter => "CW 90",
            DragTurnType.CW_Half => "CW 180",
            DragTurnType.CW_ThreeQuarter => "CW 270",
            DragTurnType.CW_Full => "CW 360",
            _ => "None"
        };
    }

    /// <summary>
    /// Returns the rotation in degrees (positive = CCW/BS, negative = CW/FS)
    /// </summary>
    public static float GetRotationDegrees(this DragTurnType turnType)
    {
        return turnType switch
        {
            DragTurnType.CCW_Quarter => 90f,
            DragTurnType.CCW_Half => 180f,
            DragTurnType.CCW_ThreeQuarter => 270f,
            DragTurnType.CCW_Full => 360f,
            DragTurnType.CW_Quarter => -90f,
            DragTurnType.CW_Half => -180f,
            DragTurnType.CW_ThreeQuarter => -270f,
            DragTurnType.CW_Full => -360f,
            _ => 0f
        };
    }

    /// <summary>
    /// Returns whether this is a backside (CCW) turn
    /// </summary>
    public static bool IsBackside(this DragTurnType turnType)
    {
        return turnType >= DragTurnType.CCW_Quarter && turnType <= DragTurnType.CCW_Full;
    }

    /// <summary>
    /// Returns whether this is a frontside (CW) turn
    /// </summary>
    public static bool IsFrontside(this DragTurnType turnType)
    {
        return turnType >= DragTurnType.CW_Quarter && turnType <= DragTurnType.CW_Full;
    }

    /// <summary>
    /// Gets the shuvit rotation amount based on turn type.
    /// Quarter turn (90° stick) = Pop shuvit (180° board)
    /// Half turn (180° stick) = 360 shuvit (360° board)
    /// </summary>
    public static float GetShuvitRotation(this DragTurnType turnType)
    {
        return turnType switch
        {
            DragTurnType.CCW_Quarter => 180f,       // Pop shuvit (BS)
            DragTurnType.CCW_Half => 360f,          // 360 shuvit (BS)
            DragTurnType.CCW_ThreeQuarter => 540f,  // 540 shuvit (BS)
            DragTurnType.CCW_Full => 720f,          // 720 shuvit (BS)
            DragTurnType.CW_Quarter => -180f,       // Pop shuvit (FS)
            DragTurnType.CW_Half => -360f,          // 360 shuvit (FS)
            DragTurnType.CW_ThreeQuarter => -540f,  // 540 shuvit (FS)
            DragTurnType.CW_Full => -720f,          // 720 shuvit (FS)
            _ => 0f
        };
    }
}
