# Dual-Stick Trick System Guide

## Overview

The trick input system now supports **both sticks** for authentic Skate-style controls:

- **Right Stick (RS)** = Back Foot (tail of board) - Most flip tricks
- **Left Stick (LS)** = Front Foot (nose of board) - Nollie tricks, front foot moves

This allows you to create tricks that use either stick, or even combinations of both sticks for complex maneuvers.

---

## Control Mapping

### Gamepad (Primary - Steam Deck Compatible):
- **Right Stick** → Back foot controls (flip tricks, shuvits)
- **Left Stick** → Front foot controls (nollies, footplants)
- **LT/L2** → Left trigger (frontside grabs)
- **RT/R2** → Right trigger (backside grabs)
- **Face Buttons** → Foot modifiers
- **Shoulders** → Handplants/special tricks

### Keyboard (Testing/Development):
- **Arrow Keys** → Right Stick simulation
- **WASD** → Left Stick simulation
- **Q** → Left trigger
- **E** → Right trigger
- **Z/X/C/V** → Face buttons
- **1/2** → Shoulders

---

## Creating Tricks with Specific Sticks

### In Unity Inspector:

When creating a TrickDefinition asset, each InputStep now has a **Stick Type** dropdown:

1. Create a new Trick Definition asset
2. Add an Input Step
3. Select **Stick Type**:
   - **Right Stick** (default) - Back foot
   - **Left Stick** - Front foot
4. Choose direction, input type, etc.

### Example: Ollie (Right Stick)
```
Stick Type: Right Stick
Direction: Up
Input Type: Tap
```

### Example: Nollie (Left Stick)
```
Stick Type: Left Stick
Direction: Up
Input Type: Tap
```

---

## Trick Examples by Stick Type

### Right Stick Tricks (Back Foot):

**Basic Flips:**
- **Ollie** → RS ↑
- **Kickflip** → RS ↗
- **Heelflip** → RS ↖
- **Pop Shuvit FS** → RS ←
- **Pop Shuvit BS** → RS →

**Complex Flips:**
- **360 Flip** → RS ← → ↓ → ↗ (sequence)
- **Varial Kickflip** → RS ↙ → ↗ (flick)
- **Hardflip** → RS ↓ → ↘ → ↑

### Left Stick Tricks (Front Foot):

**Nollie Basics:**
- **Nollie** → LS ↑
- **Nollie Kickflip** → LS ↗
- **Nollie Heelflip** → LS ↖

**Nollie Shuvits:**
- **Nollie FS Shuvit** → LS →
- **Nollie BS Shuvit** → LS ←

**Footplants:**
- **Fastplant** → LS ↑ + RT/LT

### Combination Tricks (Both Sticks):

You can create tricks that require **sequential inputs** from both sticks:

**Example: Switch to Nollie Transition**
```
Step 1: LS ↓ (Left Stick Down) - Switch stance
Step 2: RS ↑ (Right Stick Up) - Pop
Step 3: LS ↗ (Left Stick Up-Right) - Nollie flip
```

---

## Code Example: Creating a Nollie Trick

```csharp
// Create a Nollie trick (front foot pop)
var nollie = ScriptableObject.CreateInstance<TrickDefinition>();
nollie.trickName = "Nollie";
nollie.category = TrickCategory.Nollie;
nollie.difficulty = 2;
nollie.maxSequenceTime = 0.3f;

// Use LEFT stick for front foot pop
nollie.inputSequence = new List<InputStep>
{
    new InputStep(StickDirection.Up, InputType.Tap, StickType.LeftStick)
};
```

```csharp
// Create a Nollie Kickflip
var nollieKickflip = ScriptableObject.CreateInstance<TrickDefinition>();
nollieKickflip.trickName = "Nollie Kickflip";
nollieKickflip.category = TrickCategory.Nollie;
nollieKickflip.difficulty = 4;
nollieKickflip.maxSequenceTime = 0.3f;

// Use LEFT stick with diagonal up-right
nollieKickflip.inputSequence = new List<InputStep>
{
    new InputStep(StickDirection.UpRight, InputType.Tap, StickType.LeftStick)
};
```

---

## Testing Both Sticks

### Quick Test:

1. Run your test scene
2. **Right Stick Test** (or Arrow Keys):
   - Flick up → Should detect "RS ↑"
   - Flick diagonal → Should detect "RS ↗" or "RS ↖"

3. **Left Stick Test** (or WASD):
   - Tap W → Should detect "LS ↑"
   - Tap W+D → Should detect "LS ↗"

### Debug Output:

When `debugMode` is enabled, console will show:
```
Input: RS ↑ (Trig: None, Face: None, Shoulder: None)
Input: LS ↗ (Trig: None, Face: None, Shoulder: None)
```

Notice the **RS** (Right Stick) and **LS** (Left Stick) prefixes!

---

## Trick Recognition

The TrickMatcher now checks:
1. **Stick Type** - Must match exactly (RS vs LS)
2. **Direction** - Must match the specified direction
3. **Timing** - Must complete within time window
4. **Buttons** - Any required triggers/buttons must be pressed

**Example:** If a trick requires "RS ↑" and you input "LS ↑", it will **NOT match** (wrong stick).

---

## Best Practices

### For Regular Tricks:
- Use **Right Stick** (back foot) for most flip tricks
- This is the default and matches Skate game conventions

### For Nollie Tricks:
- Use **Left Stick** (front foot) for all nollie variations
- Helps distinguish nollie vs regular tricks
- More authentic to real skateboarding

### For Special Tricks:
- Combine both sticks for unique tricks
- Example: One stick for setup, other for execution
- Good for teaching players complex inputs

### For Grinds/Grabs:
- Typically use **Right Stick** + triggers/buttons
- Left stick can be used for balance/adjustments
- Can create tricks requiring both for variety

---

## Creating a Full Trick Set

Here's a template for creating all basic tricks from your TrickList.txt:

### Ollie Tricks (Right Stick):
```csharp
Ollie          → RS ↑
Kickflip       → RS ↗
Heelflip       → RS ↖
FS Pop-Shuvit  → RS ←
BS Pop-Shuvit  → RS →
```

### Nollie Tricks (Left Stick):
```csharp
Nollie         → LS ↑
Nollie Kickflip → LS ↗
Nollie Heelflip → LS ↖
Nollie FS Shuv  → LS →
Nollie BS Shuv  → LS ←
```

### Grinds (Right Stick + Hold):
```csharp
50-50 Grind    → RS ↓ → ↑
5-0 Grind      → RS ↓ (hold)
Nose Grind     → RS ↑ (hold)
Boardslide     → RS ← (hold)
```

---

## Integration with Rhythm Game

For your 3D obstacle system:

### Obstacle Types:

**1. Specific Trick Required:**
```csharp
public TrickDefinition requiredTrick; // Exact trick (e.g., "Kickflip")
```

**2. Stick Type Required:**
```csharp
public StickType requiredStick; // Any trick using RS or LS
```

**3. Category Required:**
```csharp
public TrickCategory requiredCategory; // Any flip, grind, or nollie trick
```

### Example Obstacle Logic:
```csharp
// Check if player used the correct stick type
bool correctStick = matchResult.trick.inputSequence[0].stickType == requiredStick;

// Check if it's the right trick category
bool correctCategory = matchResult.trick.category == requiredCategory;

if (correctStick && correctCategory)
{
    // Success!
    AwardPoints();
}
```

---

## Troubleshooting

**Q: My trick isn't being recognized**
- Check the **Stick Type** field in your TrickDefinition
- Verify you're using the correct stick (RS vs LS)
- Enable `debugMode` to see what inputs are being recorded

**Q: Left stick (WASD) not working**
- Ensure `enableKeyboard` is true in InputReader
- Check that no other Input Action Asset is consuming WASD
- Test with gamepad left stick to verify it's not a control issue

**Q: Both sticks detecting same trick**
- Make sure your tricks have **different stick types** set
- Ollie should be RS ↑, Nollie should be LS ↑
- Check the InputStep's `stickType` field in Inspector

**Q: Trick shows "RS" but I want "LS"**
- Edit the TrickDefinition asset
- Select each InputStep
- Change **Stick Type** dropdown to "Left Stick"

---

## Summary

✓ **Right Stick** = Back foot (most tricks)
✓ **Left Stick** = Front foot (nollies, special)
✓ **Keyboard**: Arrows = RS, WASD = LS
✓ Each InputStep has a **Stick Type** selector
✓ TrickMatcher checks stick type automatically
✓ Debug output shows RS/LS prefix

Now you can create authentic Skate-style tricks using both feet!
