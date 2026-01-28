using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// Editor utility to automatically setup a test scene for the trick system
/// </summary>
public class TestSceneSetup : EditorWindow
{
    [MenuItem("Tools/Skate/Setup Test Scene")]
    public static void SetupTestScene()
    {
        // Step 1: Create tricks if they don't exist
        if (!AssetDatabase.IsValidFolder("Assets/Tricks"))
        {
            Debug.Log("Creating trick definitions...");
            TrickDatabaseCreator.CreateBasicTricks();
        }

        // Step 2: Create TrickInputSystem GameObject
        GameObject inputSystemObj = new GameObject("TrickInputSystem");
        var inputReader = inputSystemObj.AddComponent<InputReader>();
        var trickSystem = inputSystemObj.AddComponent<TrickInputSystem>();

        // Configure input reader
        inputReader.enableKeyboard = true;
        inputReader.deadzone = 0.3f;

        // Load all tricks from the Tricks folder
        string[] trickGuids = AssetDatabase.FindAssets("t:TrickDefinition", new[] { "Assets/Tricks" });
        foreach (string guid in trickGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TrickDefinition trick = AssetDatabase.LoadAssetAtPath<TrickDefinition>(path);
            if (trick != null)
            {
                trickSystem.trickDatabase.Add(trick);
            }
        }

        // Configure trick system
        trickSystem.debugMode = true;
        trickSystem.showDebugUI = false; // We'll use our custom UI instead

        Debug.Log($"Loaded {trickSystem.trickDatabase.Count} tricks into system");

        // Step 3: Create Canvas for UI
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Step 4: Create background panel (black)
        GameObject bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = Color.black;
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Step 5: Create Trick Name Text (center, large)
        GameObject trickTextObj = new GameObject("TrickNameText");
        trickTextObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI trickText = trickTextObj.AddComponent<TextMeshProUGUI>();
        trickText.text = "";
        trickText.fontSize = 72;
        trickText.color = Color.green;
        trickText.alignment = TextAlignmentOptions.Center;
        trickText.fontStyle = FontStyles.Bold;

        RectTransform trickRect = trickTextObj.GetComponent<RectTransform>();
        trickRect.anchorMin = new Vector2(0.5f, 0.5f);
        trickRect.anchorMax = new Vector2(0.5f, 0.5f);
        trickRect.sizeDelta = new Vector2(800, 200);
        trickRect.anchoredPosition = Vector2.zero;

        // Step 6: Create Instructions Text (bottom left, small)
        GameObject instructionsObj = new GameObject("InstructionsText");
        instructionsObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI instructionsText = instructionsObj.AddComponent<TextMeshProUGUI>();
        instructionsText.text = "";
        instructionsText.fontSize = 18;
        instructionsText.color = new Color(0.8f, 0.8f, 0.8f);
        instructionsText.alignment = TextAlignmentOptions.BottomLeft;

        RectTransform instructionsRect = instructionsObj.GetComponent<RectTransform>();
        instructionsRect.anchorMin = Vector2.zero;
        instructionsRect.anchorMax = new Vector2(0, 0);
        instructionsRect.pivot = new Vector2(0, 0);
        instructionsRect.sizeDelta = new Vector2(500, 300);
        instructionsRect.anchoredPosition = new Vector2(20, 20);

        // Step 7: Add TrickDisplayUI component
        GameObject uiControllerObj = new GameObject("TrickDisplayUI");
        var displayUI = uiControllerObj.AddComponent<TrickDisplayUI>();
        displayUI.trickNameText = trickText;
        displayUI.instructionsText = instructionsText;
        displayUI.displayDuration = 2.0f;
        displayUI.showInstructions = true;

        // Step 8: Remove Main Camera clutter (optional)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
        }

        Debug.Log("<color=green>✓ Test scene setup complete!</color>");
        Debug.Log("Press Play and use Right Stick (or Arrow Keys) to perform tricks!");

        // Select the TrickInputSystem so user can see what was created
        Selection.activeGameObject = inputSystemObj;
    }

    [MenuItem("Tools/Skate/Clear Test Scene")]
    public static void ClearTestScene()
    {
        // Clean up test objects
        GameObject inputSystem = GameObject.Find("TrickInputSystem");
        GameObject canvas = GameObject.Find("Canvas");
        GameObject uiController = GameObject.Find("TrickDisplayUI");

        if (inputSystem != null) DestroyImmediate(inputSystem);
        if (canvas != null) DestroyImmediate(canvas);
        if (uiController != null) DestroyImmediate(uiController);

        Debug.Log("Test scene cleared");
    }
}
