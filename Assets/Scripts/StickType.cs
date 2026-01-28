/// <summary>
/// Which stick is being used for input
/// </summary>
public enum StickType
{
    RightStick = 0,   // Back foot (default, tail of board)
    LeftStick = 1     // Front foot (nose of board)
}

/// <summary>
/// Extension methods for StickType
/// </summary>
public static class StickTypeExtensions
{
    /// <summary>
    /// Returns a readable string for the stick type
    /// </summary>
    public static string ToReadableString(this StickType stickType)
    {
        return stickType switch
        {
            StickType.RightStick => "RS",
            StickType.LeftStick => "LS",
            _ => "??"
        };
    }

    /// <summary>
    /// Returns a descriptive name
    /// </summary>
    public static string ToDescriptiveName(this StickType stickType)
    {
        return stickType switch
        {
            StickType.RightStick => "Right Stick (Back Foot)",
            StickType.LeftStick => "Left Stick (Front Foot)",
            _ => "Unknown"
        };
    }
}
