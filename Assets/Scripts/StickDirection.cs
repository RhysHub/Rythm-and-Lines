using UnityEngine;

/// <summary>
/// 8-way directional input for right stick (like Skate games)
/// </summary>
public enum StickDirection
{
    None = 0,
    Up = 1,          // North
    UpRight = 2,     // Northeast
    Right = 3,       // East
    DownRight = 4,   // Southeast
    Down = 5,        // South
    DownLeft = 6,    // Southwest
    Left = 7,        // West
    UpLeft = 8       // Northwest
}

/// <summary>
/// Helper methods for stick direction detection and conversion
/// </summary>
public static class StickDirectionExtensions
{
    /// <summary>
    /// Converts a Vector2 input to a StickDirection using 8-way detection
    /// </summary>
    public static StickDirection ToStickDirection(this Vector2 input, float deadzone = 0.3f)
    {
        // Check if input is within deadzone
        if (input.magnitude < deadzone)
            return StickDirection.None;

        // Get angle in degrees (0 = right, 90 = up)
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;

        // Normalize to 0-360
        if (angle < 0) angle += 360;

        // Determine 8-way direction (45 degree segments)
        // Up = 67.5 to 112.5 degrees
        if (angle >= 67.5f && angle < 112.5f)
            return StickDirection.Up;
        // UpRight = 22.5 to 67.5
        else if (angle >= 22.5f && angle < 67.5f)
            return StickDirection.UpRight;
        // Right = 337.5 to 22.5
        else if (angle >= 337.5f || angle < 22.5f)
            return StickDirection.Right;
        // DownRight = 292.5 to 337.5
        else if (angle >= 292.5f && angle < 337.5f)
            return StickDirection.DownRight;
        // Down = 247.5 to 292.5
        else if (angle >= 247.5f && angle < 292.5f)
            return StickDirection.Down;
        // DownLeft = 202.5 to 247.5
        else if (angle >= 202.5f && angle < 247.5f)
            return StickDirection.DownLeft;
        // Left = 157.5 to 202.5
        else if (angle >= 157.5f && angle < 202.5f)
            return StickDirection.Left;
        // UpLeft = 112.5 to 157.5
        else
            return StickDirection.UpLeft;
    }

    /// <summary>
    /// Returns a readable string for the direction
    /// </summary>
    public static string ToReadableString(this StickDirection direction)
    {
        return direction switch
        {
            StickDirection.Up => "U",
            StickDirection.UpRight => "UR",
            StickDirection.Right => "R",
            StickDirection.DownRight => "DR",
            StickDirection.Down => "D",
            StickDirection.DownLeft => "DL",
            StickDirection.Left => "L",
            StickDirection.UpLeft => "UL",
            _ => "O"
        };
    }

    /// <summary>
    /// Returns the Vector2 representation of the direction
    /// </summary>
    public static Vector2 ToVector2(this StickDirection direction)
    {
        return direction switch
        {
            StickDirection.Up => Vector2.up,
            StickDirection.UpRight => new Vector2(0.707f, 0.707f),
            StickDirection.Right => Vector2.right,
            StickDirection.DownRight => new Vector2(0.707f, -0.707f),
            StickDirection.Down => Vector2.down,
            StickDirection.DownLeft => new Vector2(-0.707f, -0.707f),
            StickDirection.Left => Vector2.left,
            StickDirection.UpLeft => new Vector2(-0.707f, 0.707f),
            _ => Vector2.zero
        };
    }
}
