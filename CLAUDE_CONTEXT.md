# Rhythm and Lines - Project Context

This file maintains context for Claude Code sessions. Read this first when starting a new terminal.

## Project Overview

**Type:** Skateboarding trick game
**Engine:** Unity 6000.0.28f1
**Render Pipeline:** Universal Render Pipeline (URP) v17.0.3
**Input System:** Unity Input System 1.11.2

## Key Systems

### Input System
- **InputReader.cs** - Main input handler, converts gamepad/keyboard to game events
- **InputBuffer.cs** - Rolling buffer of recent inputs for pattern matching
- **RecordedInput.cs** - Struct representing a single recorded input event
- **InputStep.cs** - Defines input steps for trick definitions

**Input Types** (InputType enum):
- `Tap` - Quick directional tap (0.08s - 0.2s)
- `Hold` - Hold direction for duration (> 0.2s)
- `Flick` - Very quick input (< 0.08s)
- `Drag` - Hold and drag from one direction to another

**Stick Directions** (8-way): None, Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft

**Controls:**
- Right Stick (RS) - Back foot / Arrow keys
- Left Stick (LS) - Front foot / WASD
- Triggers (LT/RT) - Grabs / Q, E
- Face buttons (A/B/X/Y) - Z, X, C, V
- Shoulders (LB/RB) - 1, 2

### Trick System
- **TrickDefinition.cs** - ScriptableObject defining a trick's input sequence
- **TrickMatcher.cs** - Pattern matches inputs against trick database
- **TrickInputSystem.cs** - Main orchestrator, manages detection and matching

**Trick Database Location:** `Assets/TrickS/`
Available tricks: Ollie, Kickflip, Heelflip, FS/BS Pop-Shuvit, 360 Flip, Varial Kickflip, 50-50 Grind, Boardslide

### Debug/Test System
- **TrickInputSystem.cs:199-229** - OnGUI debug display (top-left corner)
- **TrickDisplayUI.cs** - TextMeshPro display for matched tricks
- **TestSceneSetup.cs** - Editor tool to setup test scene (Tools/Skate/Setup Test Scene)

**Test Scene:** `Assets/Scenes/Test.unity`

### Physics/Movement
- **BoardController.cs** - Dual-rigidbody board movement
- **GroundDetector.cs** - Ground collision detection

## Directory Structure

```
Assets/
├── Scripts/
│   ├── Board/
│   │   ├── SkateboardBuilder.cs      # Builds 3D board from primitives
│   │   ├── BoardVisualController.cs  # Steering/tilt from stick input
│   │   ├── TrickAnimator.cs          # Procedural trick animations
│   │   └── TrickAnimationData.cs     # Animation parameter struct
│   ├── World/
│   │   └── WorldMover.cs             # Endless runner movement
│   ├── Editor/
│   │   ├── TestSceneSetup.cs
│   │   └── TrickDatabaseCreator.cs
│   ├── InputReader.cs
│   ├── InputBuffer.cs
│   ├── InputStep.cs
│   ├── RecordedInput.cs
│   ├── TrickDefinition.cs
│   ├── TrickMatcher.cs
│   ├── TrickInputSystem.cs
│   ├── TrickTimingUI.cs
│   ├── TrickDisplayUI.cs
│   ├── BoardController.cs            # (Legacy dual-rigidbody system)
│   ├── GroundDetector.cs
│   ├── StickDirection.cs
│   ├── StickType.cs
│   └── TriggerButton.cs (also contains FaceButton, ShoulderButton enums)
├── Scenes/
│   ├── SampleScene.unity
│   └── Test.unity
├── TrickS/
│   └── [Trick .asset files]
└── TextMesh Pro/
```

## Recent Changes

### 2026-01-27: Input Type Display in Debug Text
**Files Modified:**
- `RecordedInput.cs` - Added `inputType` field, `ClassifyInputType()` method, updated `ToString()` to show input type
- `InputBuffer.cs` - Updated `RecordRelease()` to classify input type on release

**Result:** Debug text now shows input type classification:
```
[1.25s] RS ↑ (Flick 0.05s)
[1.50s] RS → (Tap 0.12s)
[2.00s] RS ↓ (Hold 0.45s)
```

**Thresholds:**
- Flick: < 0.08s
- Tap: 0.08s - 0.2s
- Hold: > 0.2s

### 2026-01-27: Drag Input Detection Fix
**Problem:** Drags weren't working - doing Down→Left was triggering wrong tricks because each direction was recorded as a separate input.

**Files Modified:**
- `RecordedInput.cs` - Added `dragEndDirection` field to track where drag ends, updated `ClassifyInputType()` to detect drags when direction changes, updated `ToString()` to show drag notation (e.g., `↓→←`)
- `InputBuffer.cs` - Updated `RecordInput()` to detect direction changes while holding and update `dragEndDirection` instead of creating new input
- `TrickMatcher.cs` - Implemented drag matching logic (checks `inputType == Drag` and `dragEndDirection` matches)

**Result:** Drags now properly tracked as single input:
```
[1.25s] RS ↓→← (Drag 0.30s)
```

**Drag Detection Logic:**
- When stick direction changes while still held, updates `dragEndDirection` on current input
- On release, if `dragEndDirection` differs from start `direction`, classified as Drag

### 2026-01-27: Trick Display Separator Change
**File Modified:** `TrickDefinition.cs`

**Change:** In `GetInputSequenceString()`, changed separator between trick inputs from `→` to `>` to avoid confusion with directional arrows.

Before: `RS ↓ → RS ←`
After: `RS ↓ > RS ←`

### 2026-01-27: Required Held Stick Directions
**Problem:** Needed ability to require a specific direction be held on left/right stick during an input step (e.g., hold LS left while doing RS trick).

**Files Modified:**
- `InputStep.cs` - Added `requiredLeftStickHeld` and `requiredRightStickHeld` fields with Header, updated `ToString()` to show held requirements
- `RecordedInput.cs` - Added `leftStickHeld` and `rightStickHeld` fields, updated constructor and `ToString()`
- `InputBuffer.cs` - Updated `RecordInput()` to accept and pass held stick directions
- `TrickInputSystem.cs` - Updated `HandleStickInput()` to get current stick directions from InputReader and pass to buffer
- `TrickMatcher.cs` - Added checks for `requiredLeftStickHeld` and `requiredRightStickHeld` in `MatchInputStep()`

**Result:** In Unity Inspector, each InputStep now has dropdowns for:
- Required Left Stick Held (StickDirection)
- Required Right Stick Held (StickDirection)

Debug text shows held sticks: `[1.25s] RS ↑ [LS:←] (Tap 0.15s)`

### 2026-01-27: Drag Detection Bug Fix
**File Modified:** `InputBuffer.cs`

**Problem:** Drag detection checked `isHeld == true`, but `isHeld` was never true at moment of direction change because `OnStickInput` only fires on direction changes.

**Fix:** Changed drag condition to check `direction != currentDirection` instead of `isHeld`.

### 2026-01-27: Longer Trick Priority
**File Modified:** `TrickMatcher.cs`

**Problem:** When multiple tricks match (e.g., Kickflip and 360 Flip), the system picked by accuracy alone. A 1-input trick could beat a 2-input trick.

**Fix:** Modified `MatchTrick()` to prioritize longer input sequences. Accuracy is only used as tiebreaker for equal-length tricks.

**Priority order:**
1. More input steps wins (360 Flip 2-step > Kickflip 1-step)
2. If equal steps, higher accuracy wins

### 2026-01-27: Pending Match System (Confirmation Delay Fix)
**File Modified:** `TrickInputSystem.cs`

**Problem:** Confirmation delay reset on every input, causing tricks to fail if you bumped the stick during the delay period.

**Solution:** Implemented pending match system:
1. Matches are detected immediately and stored as "pending"
2. If a longer trick is detected during delay, it replaces the pending match and resets timer
3. Once delay passes without a better match, pending match is confirmed
4. Extra noise inputs don't break the match

**New state variables:**
- `pendingMatch` - Currently pending trick match
- `pendingMatchTime` - When the pending match was detected

**Debug display now shows:** `Pending: TrickName (0.05s)` countdown

### 2026-01-27: Rhythm-Game Trick Timing UI
**File Created:** `TrickTimingUI.cs`

**Features:**
- Scrolling trick indicators on right side of screen
- Hit window with Perfect/Great/OK zones
- Random tricks spawn at configurable intervals
- Score and combo system
- Statistics tracking (Perfect, Great, OK, Miss, Early counts)

**Inspector Settings:**
- `trickInterval` - Seconds between spawns (default 3s)
- `scrollDuration` - Time for trick to scroll down (default 2s)
- `perfectWindow` - Perfect timing window (default 0.1s)
- `goodWindow` - Great timing window (default 0.2s)
- `okWindow` - OK timing window (default 0.35s)
- Track position/size settings

**Setup:** Add `TrickTimingUI` component to a GameObject, assign `TrickInputSystem` reference

### 2026-01-27: Input History Persistence & Copy Button
**Files Modified:** `InputBuffer.cs`, `TrickInputSystem.cs`

**Changes:**
- Input history no longer clears after trick confirmation
- Trick markers added to show where tricks were confirmed in history (e.g., `=== 360 Flip ===`)
- Added "Copy Input History" button to debug UI - copies full history to clipboard for debugging

### 2026-01-27: Visual Stick Indicators
**File Modified:** `TrickInputSystem.cs`, `InputReader.cs`

**Features:**
- Two circular stick displays (LS and RS) at bottom-center of screen
- **Yellow dot** - Raw/live stick position (exact X/Y, moves smoothly)
- **Green arrow** - Detected 8-way direction (snapped to direction)
- **Gray inner circle** - Deadzone visualization
- **X/Y values** - Raw values displayed below each stick

**Inspector Settings:**
- `showStickIndicators` - Toggle on/off
- `stickIndicatorSize` - Size of circles (default 80px)

**New InputReader methods:**
- `GetRawLeftStick()` - Returns raw Vector2 (-1 to 1)
- `GetRawRightStick()` - Returns raw Vector2 (-1 to 1)

### 2026-01-27: TrickTimingUI Centered Full Screen
**File Modified:** `TrickTimingUI.cs`

**Changes:**
- Track now centered horizontally on screen
- Full screen height - tricks scroll from top to bottom
- Hit window position adjustable via `hitWindowPosition` (percentage from bottom)
- Score moved to top-right corner
- Stats moved to bottom-left corner
- Result text (PERFECT/GREAT/OK/MISS) shows in center of screen with trick name

### 2026-01-27: Trick Definitions Updated (Correct Directions)
**Current trick directions (regular stance facing right):**

| Trick | Input | Description |
|-------|-------|-------------|
| BS Pop-Shuvit | RS ↓→← | Down to Left (clockwise spin) |
| FS Pop-Shuvit | RS ↓→→ | Down to Right (counter-clockwise) |
| 360 Flip | RS ↓→← > LS ↗ | BS shuvit + kickflip |

**Direction reference:**
- Backside (BS) = board spins clockwise (viewed from above) = tail goes left
- Frontside (FS) = board spins counter-clockwise = tail goes right

---

## Notes for Future Sessions

- Debug UI toggle: `TrickInputSystem.showDebugUI`
- Debug console logging: `TrickInputSystem.debugMode`
- Input buffer duration configurable via `TrickInputSystem.bufferDuration`
- Trick matching window: `TrickInputSystem.matchingTimeWindow` (default 0.5s)
- **Input Classification Thresholds** (adjustable in Inspector):
  - `TrickInputSystem.flickThreshold` - Max duration for Flick (default 0.08s)
  - `TrickInputSystem.tapThreshold` - Max duration for Tap, above = Hold (default 0.2s)
  - `TrickInputSystem.confirmationDelay` - Wait time after last input before confirming trick (default 0.1s)
- **Display Toggles:**
  - `TrickInputSystem.showCenterPopup` - Toggle center trick popup (disable when using TrickTimingUI)
  - `TrickInputSystem.showStickIndicators` - Toggle visual stick position indicators
  - `TrickInputSystem.stickIndicatorSize` - Size of stick indicator circles

---

## 3D Board Visual System (NEW)

### Overview
Endless runner style skateboard system where:
- World moves backward, board stays at origin
- Left stick steers (visual tilt + world direction change)
- Tricks have procedural animations generated from TrickDefinition input sequences

### New Scripts

**Location:** `Assets/Scripts/Board/` and `Assets/Scripts/World/`

| File | Purpose |
|------|---------|
| `SkateboardBuilder.cs` | Builds 3D skateboard from Unity primitives |
| `BoardVisualController.cs` | Handles board tilt/steering from left stick |
| `TrickAnimator.cs` | Procedural trick animations |
| `TrickAnimationData.cs` | Animation parameter data structure |
| `WorldMover.cs` | Endless runner world movement |

### SkateboardBuilder.cs
Generates a 3D skateboard from Unity primitives at runtime.

**Hierarchy created:**
```
BoardVisuals (empty parent - animation target)
├── Deck (cube)
├── NoseKick (cube, angled)
├── TailKick (cube, angled)
├── FrontTruck
│   ├── Hanger (cube)
│   ├── LeftWheel (cylinder)
│   └── RightWheel (cylinder)
└── BackTruck
    ├── Hanger (cube)
    ├── LeftWheel (cylinder)
    └── RightWheel (cylinder)
```

**Key Inspector Settings:**
- Deck dimensions (length, width, thickness)
- Truck/wheel dimensions
- Colors (if no materials assigned)

**Usage:** Add `SkateboardBuilder` component, call `BuildSkateboard()` or use Context Menu "Build Skateboard"

### BoardVisualController.cs
Controls board visual rotation based on left stick input while grounded.

**Grounded behavior:**
- Left stick X → Roll (side tilt) + Steering direction
- Left stick Y → Pitch (nose up/down)
- Sends steer input to WorldMover

**Airborne behavior:**
- Disabled during trick animations
- Returns to neutral rotation

**Key Inspector Settings:**
- `maxTiltAngle` - Maximum side tilt (default 15°)
- `maxPitchAngle` - Maximum nose pitch (default 10°)
- `maxSteerAngle` - Steering angle for world direction (default 30°)
- `tiltSpeed` - Rotation lerp speed

### TrickAnimator.cs
Generates procedural animations from TrickDefinition input sequences.

**Animation Mapping:**

| Input | Animation Effect |
|-------|------------------|
| Pop (any Up direction) | Y translation (pop height) |
| LS UpRight / Right | Kickflip: +360° X rotation |
| LS UpLeft / Left | Heelflip: -360° X rotation |
| RS Drag Down→Left | BS Shuvit: +180° Y rotation |
| RS Drag Down→Right | FS Shuvit: -180° Y rotation |
| Combined | Compound rotation (e.g., 360 flip) |

**Key Inspector Settings:**
- `defaultDuration` - Animation duration (default 0.5s)
- `defaultPopHeight` - Pop height (default 0.3m)
- `popCurve` - AnimationCurve for height
- `rotationCurve` - AnimationCurve for rotation progress
- `flipRotation` - Degrees for kickflip (default 360°)
- `shuvit180Rotation` - Degrees for shuvit (default 180°)

**Events:** Subscribes to `TrickInputSystem.OnTrickMatched`

### WorldMover.cs
Moves the world backward for endless runner effect.

**Key Inspector Settings:**
- `baseSpeed` - Forward speed (world moves backward)
- `speedMultiplier` - For speed boosts
- `maxSteerAngle` - Max world turn angle
- `steerSmoothing` - Steering response speed
- `worldContainer` - Transform holding all world objects

**Methods:**
- `SetSteerInput(float)` - Set steering (-1 to 1)
- `Pause()` / `Resume()` / `TogglePause()`
- `ResetPosition()` - Reset world to origin

### Scene Setup

**Recommended hierarchy:**
```
Skateboard (origin, stationary)
├── BoardVisuals (from SkateboardBuilder)
├── WheelColliders
│   ├── FrontWheelCollider (GroundDetector)
│   └── BackWheelCollider (GroundDetector)
└── Camera

WorldContainer (moves backward)
├── Ground (tagged "Ground")
└── Obstacles...

Systems (empty GameObject)
├── InputReader
├── TrickInputSystem
├── SkateboardBuilder
├── BoardVisualController
├── TrickAnimator
└── WorldMover
```

### 2026-01-28: 3D Board System Created
**Files Created:**
- `Assets/Scripts/Board/SkateboardBuilder.cs`
- `Assets/Scripts/Board/BoardVisualController.cs`
- `Assets/Scripts/Board/TrickAnimator.cs`
- `Assets/Scripts/Board/TrickAnimationData.cs`
- `Assets/Scripts/World/WorldMover.cs`

**Features:**
- Procedural 3D skateboard from primitives (deck, trucks, wheels)
- Left stick steering with visual tilt
- Endless runner world movement with steering curves
- Procedural trick animations generated from TrickDefinition data
- Animation curves for pop height and rotation timing
