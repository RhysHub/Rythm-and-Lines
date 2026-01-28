/// <summary>
/// Trigger buttons used for grab tricks and modifiers
/// </summary>
public enum TriggerButton
{
    None = 0,
    LeftTrigger = 1,   // LT/L2 - Frontside grabs
    RightTrigger = 2,  // RT/R2 - Backside grabs
    Both = 3           // Both triggers - Double grabs
}

/// <summary>
/// Face buttons used for special trick modifiers
/// </summary>
public enum FaceButton
{
    None = 0,
    South = 1,   // A/X - Front foot tricks
    East = 2,    // B/Circle - No-foot tricks
    West = 3,    // X/Square - Back foot tricks
    North = 4    // Y/Triangle - Special tricks
}

/// <summary>
/// Shoulder buttons for handplants and special tricks
/// </summary>
public enum ShoulderButton
{
    None = 0,
    LeftShoulder = 1,   // LB/L1
    RightShoulder = 2,  // RB/R1
    Both = 3
}
