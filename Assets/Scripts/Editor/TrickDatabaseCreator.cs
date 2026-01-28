using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor utility to quickly create trick definitions
/// </summary>
public class TrickDatabaseCreator : EditorWindow
{
    [MenuItem("Tools/Skate/Create Basic Tricks")]
    public static void CreateBasicTricks()
    {
        string folderPath = "Assets/Tricks";

        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Tricks");
        }

        // Create basic tricks
        CreateOllie(folderPath);
        CreateKickflip(folderPath);
        CreateHeelflip(folderPath);
        CreateFSPopShuvit(folderPath);
        CreateBSPopShuvit(folderPath);
        Create360Flip(folderPath);
        CreateVarialKickflip(folderPath);

        // Create basic grinds
        Create5050Grind(folderPath);
        CreateBoardslide(folderPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created basic trick definitions in Assets/Tricks/");
    }

    private static void CreateOllie(string folder)
    {
        var trick = ScriptableObject.CreateInstance<TrickDefinition>();
        trick.trickName = "Ollie";
        trick.category = TrickCategory.Flip;
        trick.difficulty = 1;
        trick.maxSequenceTime = 0.3f;

        trick.inputSequence = new List<InputStep>
        {
            new InputStep(StickDirection.Up, InputType.Tap)
        };

        AssetDatabase.CreateAsset(trick, $"{folder}/Ollie.asset");
    }

    private static void CreateKickflip(string folder)
    {
        var trick = ScriptableObject.CreateInstance<TrickDefinition>();
        trick.trickName = "Kickflip";
        trick.category = TrickCategory.Flip;
        trick.difficulty = 2;
        trick.maxSequenceTime = 0.3f;

        trick.inputSequence = new List<InputStep>
        {
            new InputStep(StickDirection.UpRight, InputType.Tap)
        };

        AssetDatabase.CreateAsset(trick, $"{folder}/Kickflip.asset");
    }

    private static void CreateHeelflip(string folder)
    {
        var trick = ScriptableObject.CreateInstance<TrickDefinition>();
        trick.trickName = "Heelflip";
        trick.category = TrickCategory.Flip;
        trick.difficulty = 2;
        trick.maxSequenceTime = 0.3f;

        trick.inputSequence = new List<InputStep>
        {
            new InputStep(StickDirection.UpLeft, InputType.Tap)
        };

        AssetDatabase.CreateAsset(trick, $"{folder}/Heelflip.asset");
    }

    private static void CreateFSPopShuvit(string folder)
    {
        var trick = ScriptableObject.CreateInstance<TrickDefinition>();
        trick.trickName = "FS Pop-Shuvit";
        trick.category = TrickCategory.Shuvit;
        trick.difficulty = 2;
        trick.maxSequenceTime = 0.3f;

        trick.inputSequence = new List<InputStep>
        {
            new InputStep(StickDirection.Left, InputType.Tap)
        };

        AssetDatabase.CreateAsset(trick, $"{folder}/FS_PopShuvit.asset");
    }

    private static void CreateBSPopShuvit(string folder)
    {
        var trick = ScriptableObject.CreateInstance<TrickDefinition>();
        trick.trickName = "BS Pop-Shuvit";
        trick.category = TrickCategory.Shuvit;
        trick.difficulty = 2;
        trick.maxSequenceTime = 0.3f;

        trick.inputSequence = new List<InputStep>
        {
            new InputStep(StickDirection.Right, InputType.Tap)
        };

        AssetDatabase.CreateAsset(trick, $"{folder}/BS_PopShuvit.asset");
    }

    private static void Create360Flip(string folder)
    {
        var trick = ScriptableObject.CreateInstance<TrickDefinition>();
        trick.trickName = "360 Flip";
        trick.category = TrickCategory.Flip;
        trick.difficulty = 5;
        trick.maxSequenceTime = 0.5f;

        trick.inputSequence = new List<InputStep>
        {
            new InputStep(StickDirection.Left, InputType.Tap),
            new InputStep(StickDirection.Down, InputType.Tap),
            new InputStep(StickDirection.UpRight, InputType.Tap)
        };

        AssetDatabase.CreateAsset(trick, $"{folder}/360Flip.asset");
    }

    private static void CreateVarialKickflip(string folder)
    {
        var trick = ScriptableObject.CreateInstance<TrickDefinition>();
        trick.trickName = "Varial Kickflip";
        trick.category = TrickCategory.Flip;
        trick.difficulty = 4;
        trick.maxSequenceTime = 0.4f;

        trick.inputSequence = new List<InputStep>
        {
            new InputStep(StickDirection.DownLeft, InputType.Flick),
            new InputStep(StickDirection.UpRight, InputType.Tap)
        };

        AssetDatabase.CreateAsset(trick, $"{folder}/VarialKickflip.asset");
    }

    private static void Create5050Grind(string folder)
    {
        var trick = ScriptableObject.CreateInstance<TrickDefinition>();
        trick.trickName = "50-50 Grind";
        trick.category = TrickCategory.Grind;
        trick.difficulty = 2;
        trick.maxSequenceTime = 0.4f;
        trick.requiresGrindSurface = true;

        trick.inputSequence = new List<InputStep>
        {
            new InputStep(StickDirection.Down, InputType.Tap),
            new InputStep(StickDirection.Up, InputType.Tap)
        };

        AssetDatabase.CreateAsset(trick, $"{folder}/5050Grind.asset");
    }

    private static void CreateBoardslide(string folder)
    {
        var trick = ScriptableObject.CreateInstance<TrickDefinition>();
        trick.trickName = "Boardslide";
        trick.category = TrickCategory.Grind;
        trick.difficulty = 3;
        trick.maxSequenceTime = 0.3f;
        trick.requiresGrindSurface = true;

        trick.inputSequence = new List<InputStep>
        {
            new InputStep(StickDirection.Left, InputType.Hold) { minHoldDuration = 0.2f }
        };

        AssetDatabase.CreateAsset(trick, $"{folder}/Boardslide.asset");
    }
}
