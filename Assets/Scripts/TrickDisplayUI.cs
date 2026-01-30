using UnityEngine;
using TMPro;

/// <summary>
/// Simple UI display for showing matched tricks
/// </summary>
public class TrickDisplayUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshPro text component to display trick name")]
    public TextMeshProUGUI trickNameText;

    [Tooltip("TextMeshPro text component for input instructions")]
    public TextMeshProUGUI instructionsText;

    [Header("Display Settings")]
    [Tooltip("How long to show the trick name before fading")]
    public float displayDuration = 2.0f;

    [Tooltip("Show input instructions")]
    public bool showInstructions = true;

    private TrickInputSystem inputSystem;
    private float displayTimer = 0f;
    private string currentTrickName = "";

    private void Start()
    {
        // Find the TrickInputSystem in the scene
        inputSystem = FindObjectOfType<TrickInputSystem>();

        if (inputSystem == null)
        {
            Debug.LogError("TrickDisplayUI: Could not find TrickInputSystem in scene!");
            return;
        }

        // Subscribe to trick matched event
        inputSystem.OnTrickMatched += OnTrickPerformed;

        // Setup instructions
        if (showInstructions && instructionsText != null)
        {
            instructionsText.text = "GAMEPAD:\n" +
                                   "  Right Stick (RS) = Back Foot\n" +
                                   "  Left Stick (LS) = Front Foot\n" +
                                   "KEYBOARD:\n" +
                                   "  Arrow Keys = Right Stick\n" +
                                   "  WASD = Left Stick\n\n" +
                                   "Try these:\n" +
                                   "Ollie: RS D > LS U\n" +
                                   "Kickflip: RS D > LS UL\n" +
                                   "Heelflip: RS D > LS UR\n" +
                                   "360 Flip: RS D (CCW half) > LS UL\n" +
                                   "Nollie: LS U > RS D";
        }

        // Clear trick name initially
        if (trickNameText != null)
        {
            trickNameText.text = "";
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from event
        if (inputSystem != null)
        {
            inputSystem.OnTrickMatched -= OnTrickPerformed;
        }
    }

    private void Update()
    {
        // Handle fade out timer
        if (displayTimer > 0f)
        {
            displayTimer -= Time.deltaTime;

            if (displayTimer <= 0f)
            {
                // Clear the text
                if (trickNameText != null)
                {
                    trickNameText.text = "";
                }
            }
        }
    }

    /// <summary>
    /// Called when a trick is successfully performed
    /// </summary>
    private void OnTrickPerformed(TrickMatchResult result)
    {
        if (result == null || !result.matched || result.trick == null)
            return;

        currentTrickName = result.trick.trickName;

        // Display trick name with accuracy
        if (trickNameText != null)
        {
            string accuracyText = result.accuracy >= 0.9f ? "PERFECT!" :
                                 result.accuracy >= 0.7f ? "GREAT!" : "GOOD!";

            trickNameText.text = $"{currentTrickName}\n{accuracyText}";
        }

        // Reset timer
        displayTimer = displayDuration;

        Debug.Log($"Trick Performed: {currentTrickName} ({result.accuracy:P0})");
    }
}
