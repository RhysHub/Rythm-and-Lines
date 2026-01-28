using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Editor utility to set up the 3D board system with endless runner movement
/// </summary>
public class BoardSceneSetup : EditorWindow
{
    [MenuItem("Tools/Skate/Setup 3D Board Scene")]
    public static void Setup3DBoardScene()
    {
        Debug.Log("<color=cyan>Setting up 3D Board Scene...</color>");

        // Step 1: Find or create TrickInputSystem
        TrickInputSystem trickInputSystem = Object.FindObjectOfType<TrickInputSystem>();
        InputReader inputReader = null;

        if (trickInputSystem == null)
        {
            Debug.Log("Creating TrickInputSystem...");
            GameObject inputSystemObj = new GameObject("TrickInputSystem");
            inputReader = inputSystemObj.AddComponent<InputReader>();
            trickInputSystem = inputSystemObj.AddComponent<TrickInputSystem>();

            // Load tricks
            LoadTricksIntoSystem(trickInputSystem);

            inputReader.enableKeyboard = true;
            trickInputSystem.debugMode = true;
            trickInputSystem.showDebugUI = true;
            trickInputSystem.showCenterPopup = false; // TrickAnimator handles visuals now
        }
        else
        {
            inputReader = trickInputSystem.GetComponent<InputReader>();
            Debug.Log("Found existing TrickInputSystem");
        }

        // Step 2: Create Skateboard root
        GameObject skateboard = new GameObject("Skateboard");
        skateboard.transform.position = new Vector3(0, 0.1f, 0);

        // Step 3: Add SkateboardBuilder and build the board
        SkateboardBuilder builder = skateboard.AddComponent<SkateboardBuilder>();
        GameObject boardVisuals = builder.BuildSkateboard();
        Debug.Log("Built skateboard visuals");

        // Step 4: Create Wheel Colliders for ground detection
        GameObject wheelColliders = new GameObject("WheelColliders");
        wheelColliders.transform.SetParent(skateboard.transform);
        wheelColliders.transform.localPosition = Vector3.zero;

        // Add Rigidbody for trigger detection (kinematic so it doesn't fall)
        Rigidbody wheelRb = wheelColliders.AddComponent<Rigidbody>();
        wheelRb.isKinematic = true;
        wheelRb.useGravity = false;

        // Front wheel collider - positioned to intersect ground at y=0
        GameObject frontWheelCollider = new GameObject("FrontWheelCollider");
        frontWheelCollider.transform.SetParent(wheelColliders.transform);
        frontWheelCollider.transform.localPosition = new Vector3(0, -0.1f, 0.28f);
        BoxCollider frontBox = frontWheelCollider.AddComponent<BoxCollider>();
        frontBox.isTrigger = true;
        frontBox.size = new Vector3(0.3f, 0.15f, 0.1f); // Taller to ensure ground intersection
        GroundDetector frontDetector = frontWheelCollider.AddComponent<GroundDetector>();

        // Back wheel collider - positioned to intersect ground at y=0
        GameObject backWheelCollider = new GameObject("BackWheelCollider");
        backWheelCollider.transform.SetParent(wheelColliders.transform);
        backWheelCollider.transform.localPosition = new Vector3(0, -0.1f, -0.28f);
        BoxCollider backBox = backWheelCollider.AddComponent<BoxCollider>();
        backBox.isTrigger = true;
        backBox.size = new Vector3(0.3f, 0.15f, 0.1f); // Taller to ensure ground intersection
        GroundDetector backDetector = backWheelCollider.AddComponent<GroundDetector>();

        Debug.Log("Created wheel colliders with GroundDetector");

        // Step 5: Create or find WorldContainer
        GameObject worldContainer = GameObject.Find("WorldContainer");
        if (worldContainer == null)
        {
            worldContainer = new GameObject("WorldContainer");
            worldContainer.transform.position = Vector3.zero;
        }

        // Add WorldMover
        WorldMover worldMover = worldContainer.GetComponent<WorldMover>();
        if (worldMover == null)
        {
            worldMover = worldContainer.AddComponent<WorldMover>();
        }
        worldMover.worldContainer = worldContainer.transform;
        worldMover.baseSpeed = 8f;
        worldMover.maxSteerAngle = 30f;

        // Step 6: Find or create Ground and parent to WorldContainer
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(50f, 0.1f, 200f);
            ground.transform.position = new Vector3(0, -0.05f, 50f);

            // Set material color
            var renderer = ground.GetComponent<MeshRenderer>();
            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.color = new Color(0.3f, 0.3f, 0.3f);
            renderer.material = groundMat;
        }

        // Ensure ground is tagged
        if (!TagExists("Ground"))
        {
            Debug.LogWarning("'Ground' tag doesn't exist. Please create it manually: Edit → Project Settings → Tags and Layers");
        }
        else
        {
            ground.tag = "Ground";
        }

        // Parent ground to WorldContainer
        if (ground.transform.parent != worldContainer.transform)
        {
            ground.transform.SetParent(worldContainer.transform);
        }

        Debug.Log("Set up WorldContainer with Ground");

        // Step 7: Add BoardVisualController
        BoardVisualController boardController = skateboard.AddComponent<BoardVisualController>();
        boardController.inputReader = inputReader;
        boardController.frontWheels = frontDetector;
        boardController.backWheels = backDetector;
        boardController.boardVisuals = boardVisuals.transform;
        boardController.worldMover = worldMover;
        boardController.debugMode = true;

        Debug.Log("Added BoardVisualController");

        // Step 8: Add TrickAnimator
        TrickAnimator trickAnimator = skateboard.AddComponent<TrickAnimator>();
        trickAnimator.trickInputSystem = trickInputSystem;
        trickAnimator.boardController = boardController;
        trickAnimator.boardVisuals = boardVisuals.transform;

        // Set up animation curves
        trickAnimator.popCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4f),
            new Keyframe(0.4f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -4f, 0f)
        );
        trickAnimator.rotationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        Debug.Log("Added TrickAnimator");

        // Step 9: Set up Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        // Position camera behind and above the board
        mainCam.transform.SetParent(skateboard.transform);
        mainCam.transform.localPosition = new Vector3(0, 1.5f, -3f);
        mainCam.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);

        Debug.Log("Set up Camera");

        // Step 10: Add directional light if none exists
        if (Object.FindObjectOfType<Light>() == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 1f;
        }

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        // Step 11: Create Canvas UI
        CreateCanvasUI(trickInputSystem, inputReader);

        Debug.Log("<color=green>✓ 3D Board Scene setup complete!</color>");
        Debug.Log("Controls:");
        Debug.Log("  - WASD / Left Stick: Steer and tilt board");
        Debug.Log("  - Arrow Keys / Right Stick: Trick inputs");
        Debug.Log("  - Perform tricks to see procedural animations!");

        // Select the skateboard
        Selection.activeGameObject = skateboard;
    }

    private static void CreateCanvasUI(TrickInputSystem trickInputSystem, InputReader inputReader)
    {
        Debug.Log("Creating Canvas UI...");

        // Create main UI Canvas
        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Create Stick Indicator UI
        GameObject stickUIObj = new GameObject("StickIndicatorUI");
        stickUIObj.transform.SetParent(canvasObj.transform);
        StickIndicatorUI stickUI = stickUIObj.AddComponent<StickIndicatorUI>();
        stickUI.inputReader = inputReader;
        stickUI.AutoSetupUI();

        // Create Trick Track UI
        GameObject trackUIObj = new GameObject("TrickTrackUI");
        trackUIObj.transform.SetParent(canvasObj.transform);
        TrickTrackUI trackUI = trackUIObj.AddComponent<TrickTrackUI>();
        trackUI.trickInputSystem = trickInputSystem;
        trackUI.canvas = canvas;
        trackUI.SetupUI();

        // Disable old OnGUI in TrickInputSystem
        trickInputSystem.showDebugUI = false;
        trickInputSystem.showStickIndicators = false;

        Debug.Log("Canvas UI created");
    }

    [MenuItem("Tools/Skate/Clear 3D Board Scene")]
    public static void Clear3DBoardScene()
    {
        // Find and destroy created objects
        string[] objectsToRemove = {
            "Skateboard",
            "WorldContainer",
            "TrickInputSystem"
        };

        foreach (string name in objectsToRemove)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                DestroyImmediate(obj);
                Debug.Log($"Removed {name}");
            }
        }

        Debug.Log("3D Board scene cleared");
    }

    /// <summary>
    /// Loads all TrickDefinition assets into the system
    /// </summary>
    private static void LoadTricksIntoSystem(TrickInputSystem system)
    {
        // Try multiple folders where tricks might be
        string[] searchPaths = { "Assets/TrickS", "Assets/Tricks" };

        foreach (string path in searchPaths)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                string[] trickGuids = AssetDatabase.FindAssets("t:TrickDefinition", new[] { path });
                foreach (string guid in trickGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    TrickDefinition trick = AssetDatabase.LoadAssetAtPath<TrickDefinition>(assetPath);
                    if (trick != null && !system.trickDatabase.Contains(trick))
                    {
                        system.trickDatabase.Add(trick);
                    }
                }
            }
        }

        Debug.Log($"Loaded {system.trickDatabase.Count} tricks");
    }

    /// <summary>
    /// Checks if a tag exists in the project
    /// </summary>
    private static bool TagExists(string tag)
    {
        try
        {
            GameObject temp = new GameObject();
            temp.tag = tag;
            DestroyImmediate(temp);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [MenuItem("Tools/Skate/Add Ground Tag")]
    public static void AddGroundTag()
    {
        // Open the TagManager
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // Check if tag already exists
        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == "Ground")
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            tagsProp.GetArrayElementAtIndex(0).stringValue = "Ground";
            tagManager.ApplyModifiedProperties();
            Debug.Log("<color=green>Added 'Ground' tag</color>");
        }
        else
        {
            Debug.Log("'Ground' tag already exists");
        }
    }
}
