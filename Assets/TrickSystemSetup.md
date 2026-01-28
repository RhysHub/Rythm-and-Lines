# Skate-Style Trick Input System - Setup Guide

## What's Been Built

A complete trick input detection system based on Skate game mechanics:

### Core Components

1. **StickDirection.cs** - 8-way directional input detection with helper methods
2. **TriggerButton.cs** - Trigger, face button, and shoulder button enums
3. **InputStep.cs** - Defines individual steps in a trick sequence
4. **TrickDefinition.cs** - ScriptableObject for defining complete tricks
5. **RecordedInput.cs** - Records input events with timestamps
6. **InputBuffer.cs** - Maintains rolling buffer of recent inputs
7. **InputReader.cs** - Reads from gamepad + keyboard (Steam Deck compatible)
8. **TrickMatcher.cs** - Pattern matching engine for trick recognition
9. **TrickInputSystem.cs** - Main system that ties everything together

### Editor Tools

10. **TrickDatabaseCreator.cs** - Editor utility to quickly create basic tricks

## Setup Instructions

### 1. Create Trick Definitions

In Unity Editor:
1. Go to **Tools > Skate > Create Basic Tricks**
2. This creates 9 basic tricks in `Assets/Tricks/`:
   - Ollie (RS Up)
   - Kickflip (RS Up/Right)
   - Heelflip (RS Up/Left)
   - FS Pop-Shuvit (RS Left)
   - BS Pop-Shuvit (RS Right)
   - 360 Flip (RS Left → Down → Up/Right)
   - Varial Kickflip (RS Down/Left → Up/Right)
   - 50-50 Grind (RS Down → Up)
   - Boardslide (Hold RS Left)

### 2. Setup Scene

1. Create an empty GameObject in your scene
2. Add the **TrickInputSystem** component
3. In the Inspector, add your trick assets to the **Trick Database** list
4. Enable **Debug Mode** and **Show Debug UI** for testing

### 3. Test Controls

#### Gamepad (Primary - Steam Deck compatible):
- **Right Stick** - Main trick input
- **LT/L2** - Left trigger (grabs)
- **RT/R2** - Right trigger (grabs)
- **Face Buttons** - A/X, B/Circle, X/Square, Y/Triangle
- **LB/L1, RB/R1** - Shoulder buttons (handplants)

#### Keyboard (Testing/Accessibility):
- **Arrow Keys** - Right stick simulation
- **Q** - Left trigger
- **E** - Right trigger
- **Z** - South button (A/X)
- **X** - East button (B/Circle)
- **C** - West button (X/Square)
- **V** - North button (Y/Triangle)
- **1** - Left shoulder
- **2** - Right shoulder

### 4. Testing Tricks

Run the game and try these inputs:

1. **Ollie**: Flick RS Up quickly
2. **Kickflip**: Flick RS Up-Right (diagonal)
3. **Heelflip**: Flick RS Up-Left (diagonal)
4. **FS Pop-Shuvit**: Tap RS Left
5. **BS Pop-Shuvit**: Tap RS Right
6. **360 Flip**: Tap RS Left → Down → Up-Right (smooth sequence)

### 5. Debug UI

The debug UI (top-left) shows:
- Number of tricks loaded
- Recent inputs with timestamps
- Last matched trick (appears in center of screen in green)

## Creating Custom Tricks

### Method 1: Using the Inspector

1. Right-click in Project window
2. Create > Skate > Trick Definition
3. Fill in:
   - **Trick Name**: Display name
   - **Category**: Flip, Shuvit, Nollie, Grind, Grab, Special
   - **Difficulty**: 1-10 (affects scoring)
   - **Input Sequence**: Add steps with + button
     - Direction (8-way + None)
     - Input Type (Tap, Hold, Flick, Drag)
     - Optional triggers/buttons
     - Timing windows
   - **Max Sequence Time**: How fast the entire trick must be completed

### Method 2: Using Code

```csharp
var hardflip = ScriptableObject.CreateInstance<TrickDefinition>();
hardflip.trickName = "Hardflip";
hardflip.category = TrickCategory.Flip;
hardflip.difficulty = 6;
hardflip.maxSequenceTime = 0.5f;

hardflip.inputSequence = new List<InputStep>
{
    new InputStep(StickDirection.Down, InputType.Tap),
    new InputStep(StickDirection.DownRight, InputType.Tap),
    new InputStep(StickDirection.Up, InputType.Tap)
};

AssetDatabase.CreateAsset(hardflip, "Assets/Tricks/Hardflip.asset");
```

## Integration with Rhythm Game

The system is designed to work with your 3D obstacle-based rhythm game:

### Timing Windows

The `TrickInputSystem` has a `matchingTimeWindow` parameter (default 0.5s). This is the window where the entire trick gesture must be completed.

For your rhythm game, you can:
1. **Trigger timing window when approaching obstacle**
2. **Call `CanPerformTrick(TrickDefinition)` to check if player performed correct trick**
3. **Use the accuracy score (0-1) for timing feedback** (Perfect/Great/Good/Miss)

### Example Integration:

```csharp
public class ObstacleTrickZone : MonoBehaviour
{
    public TrickDefinition requiredTrick;  // Or TrickCategory for flexibility
    private TrickInputSystem inputSystem;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Player entered trick zone
            StartCoroutine(EvaluateTrick());
        }
    }

    IEnumerator EvaluateTrick()
    {
        // Wait for timing window
        yield return new WaitForSeconds(0.2f);

        // Check if trick was performed
        if (inputSystem.CanPerformTrick(requiredTrick))
        {
            // SUCCESS!
            OnTrickSuccess();
        }
        else
        {
            // MISS!
            OnTrickFail();
        }
    }
}
```

## Next Steps

1. **Create more tricks from TrickList.txt**
   - Copy the patterns from the Skate 4 list
   - Focus on the tricks you want in your game first

2. **Add grab tricks**
   - These use trigger buttons (LT/RT) + stick directions

3. **Add timing evaluation**
   - Integrate with your rhythm game beat system
   - Score based on timing accuracy

4. **Add visual feedback**
   - Show stick input visually (like Skate games)
   - Show trick name and score on success

5. **Create trick categories for obstacles**
   - "Any Flip" - accepts any flip trick
   - "Any Grind" - accepts any grind
   - Player choice routes

## Adjustable Parameters

In TrickInputSystem Inspector:
- **Matching Time Window**: How long inputs are valid for matching
- **Buffer Duration**: How long to remember inputs
- **Debug Mode**: Console logging
- **Show Debug UI**: On-screen display

In InputReader Inspector:
- **Deadzone**: Stick sensitivity (lower = more sensitive)
- **Flick Threshold**: How hard stick must be pushed for flicks
- **Enable Keyboard**: Toggle keyboard controls

## Troubleshooting

**No tricks detected:**
- Check trick assets are in the Trick Database list
- Ensure Debug Mode is on to see input logging
- Try simpler tricks first (Ollie, Kickflip)
- Check deadzone isn't too high

**Wrong tricks detected:**
- Reduce Matching Time Window for stricter timing
- Check similar tricks aren't conflicting (order matters in database)
- Make trick sequences more distinct

**Keyboard not working:**
- Enable Keyboard option in InputReader
- Make sure no Input Action Asset is blocking keyboard

**Gamepad not detected:**
- Check gamepad is connected before starting game
- Steam Deck: Should work automatically as generic gamepad
- Try keyboard controls first to verify system works
