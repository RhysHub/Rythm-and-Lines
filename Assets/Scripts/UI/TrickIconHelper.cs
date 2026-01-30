using UnityEngine;

/// <summary>
/// Helper class for managing trick indicator icons.
/// Provides textures and rotation values for directions and drag turn types.
/// </summary>
public class TrickIconHelper : MonoBehaviour
{
    [Header("Direction Arrow")]
    [Tooltip("Arrow pointing up - will be rotated for other directions")]
    public Texture2D arrowUp;

    [Header("CCW Turn Icons (starting from bottom middle)")]
    public Texture2D quarterCCW;
    public Texture2D halfCCW;
    public Texture2D threeQuarterCCW;
    public Texture2D fullCCW;

    [Header("Stick Icons")]
    public Texture2D leftStickIcon;
    public Texture2D rightStickIcon;

    private static TrickIconHelper _instance;
    public static TrickIconHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TrickIconHelper>();
                if (_instance == null)
                {
                    // Auto-create if not found
                    GameObject go = new GameObject("TrickIconHelper");
                    _instance = go.AddComponent<TrickIconHelper>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
        LoadTexturesFromResources();
    }

    private void Start()
    {
        // Ensure textures are loaded
        LoadTexturesFromResources();
    }

    /// <summary>
    /// Attempts to load textures from Resources/textures folder if not assigned
    /// </summary>
    private void LoadTexturesFromResources()
    {
        if (arrowUp == null)
            arrowUp = Resources.Load<Texture2D>("textures/arrow_up");
        if (quarterCCW == null)
            quarterCCW = Resources.Load<Texture2D>("textures/quarter_CCW");
        if (halfCCW == null)
            halfCCW = Resources.Load<Texture2D>("textures/half_CCW");
        if (threeQuarterCCW == null)
            threeQuarterCCW = Resources.Load<Texture2D>("textures/3quarter_CCW");
        if (fullCCW == null)
            fullCCW = Resources.Load<Texture2D>("textures/full_CCW");

        // Debug output
        if (arrowUp == null)
            Debug.LogWarning("TrickIconHelper: Failed to load arrow_up texture from Resources/textures/");
        else
            Debug.Log("TrickIconHelper: Loaded arrow_up texture successfully");
    }

    /// <summary>
    /// Gets the rotation angle in degrees for a given stick direction.
    /// Arrow starts pointing up (0 degrees).
    /// </summary>
    public static float GetDirectionRotation(StickDirection direction)
    {
        switch (direction)
        {
            case StickDirection.Up: return 0f;
            case StickDirection.UpRight: return -45f;
            case StickDirection.Right: return -90f;
            case StickDirection.DownRight: return -135f;
            case StickDirection.Down: return 180f;
            case StickDirection.DownLeft: return 135f;
            case StickDirection.Left: return 90f;
            case StickDirection.UpLeft: return 45f;
            default: return 0f;
        }
    }

    /// <summary>
    /// Gets the appropriate turn texture for a drag turn type.
    /// Returns null if no turn texture needed.
    /// </summary>
    public Texture2D GetTurnTexture(DragTurnType turnType)
    {
        switch (turnType)
        {
            case DragTurnType.CCW_Quarter:
            case DragTurnType.CW_Quarter:
                return quarterCCW;
            case DragTurnType.CCW_Half:
            case DragTurnType.CW_Half:
                return halfCCW;
            case DragTurnType.CCW_ThreeQuarter:
            case DragTurnType.CW_ThreeQuarter:
                return threeQuarterCCW;
            case DragTurnType.CCW_Full:
            case DragTurnType.CW_Full:
                return fullCCW;
            default:
                return null;
        }
    }

    /// <summary>
    /// Returns true if the turn type should be flipped horizontally (CW turns)
    /// </summary>
    public static bool ShouldFlipHorizontal(DragTurnType turnType)
    {
        return turnType == DragTurnType.CW_Quarter ||
               turnType == DragTurnType.CW_Half ||
               turnType == DragTurnType.CW_ThreeQuarter ||
               turnType == DragTurnType.CW_Full;
    }

    /// <summary>
    /// Draws a direction arrow at the specified position with proper rotation.
    /// </summary>
    public void DrawDirectionArrow(Rect rect, StickDirection direction, Color color)
    {
        if (arrowUp == null || direction == StickDirection.None)
            return;

        float rotation = GetDirectionRotation(direction);
        DrawRotatedTexture(rect, arrowUp, rotation, color);
    }

    /// <summary>
    /// Draws a drag turn icon at the specified position, flipped for CW turns.
    /// </summary>
    public void DrawTurnIcon(Rect rect, DragTurnType turnType, Color color)
    {
        Texture2D tex = GetTurnTexture(turnType);
        if (tex == null)
            return;

        bool flip = ShouldFlipHorizontal(turnType);
        DrawRotatedTexture(rect, tex, 0f, color, flip);
    }

    /// <summary>
    /// Draws a texture rotated around its center.
    /// </summary>
    public static void DrawRotatedTexture(Rect rect, Texture2D texture, float angle, Color color, bool flipHorizontal = false)
    {
        if (texture == null)
            return;

        Matrix4x4 matrixBackup = GUI.matrix;
        Color colorBackup = GUI.color;

        GUI.color = color;

        // Calculate pivot point (center of rect)
        Vector2 pivot = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);

        // Apply rotation around pivot
        GUIUtility.RotateAroundPivot(angle, pivot);

        // Apply horizontal flip if needed
        if (flipHorizontal)
        {
            GUIUtility.ScaleAroundPivot(new Vector2(-1, 1), pivot);
        }

        GUI.DrawTexture(rect, texture);

        GUI.matrix = matrixBackup;
        GUI.color = colorBackup;
    }

    /// <summary>
    /// Draws a complete input step icon (stick label + direction/turn)
    /// </summary>
    public void DrawInputStepIcon(Rect rect, InputStep step, Color color)
    {
        float iconSize = Mathf.Min(rect.width, rect.height) * 0.8f;
        Rect iconRect = new Rect(
            rect.x + (rect.width - iconSize) / 2,
            rect.y + (rect.height - iconSize) / 2,
            iconSize,
            iconSize
        );

        // For drag inputs with turn type, draw the turn icon
        if (step.inputType == InputType.Drag && step.dragTurnType != DragTurnType.None)
        {
            DrawTurnIcon(iconRect, step.dragTurnType, color);
        }
        else
        {
            // Draw direction arrow
            DrawDirectionArrow(iconRect, step.direction, color);
        }
    }
}
