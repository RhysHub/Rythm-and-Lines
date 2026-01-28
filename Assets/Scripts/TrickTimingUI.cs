using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rhythm-game style trick timing system
/// Tricks scroll down the screen and must be performed when they reach the hit window
/// </summary>
public class TrickTimingUI : MonoBehaviour
{
    [Header("Timing Settings")]
    [Tooltip("Seconds between trick spawns")]
    [Range(1f, 10f)]
    public float trickInterval = 3f;

    [Tooltip("How long it takes for a trick to scroll from top to bottom")]
    [Range(1f, 5f)]
    public float scrollDuration = 2f;

    [Header("Hit Window")]
    [Tooltip("Perfect hit window (seconds before/after center)")]
    [Range(0.05f, 0.3f)]
    public float perfectWindow = 0.1f;

    [Tooltip("Good hit window (seconds before/after center)")]
    [Range(0.1f, 0.5f)]
    public float goodWindow = 0.2f;

    [Tooltip("OK hit window (seconds before/after center)")]
    [Range(0.2f, 0.8f)]
    public float okWindow = 0.35f;

    [Header("UI Settings")]
    [Tooltip("Width of the timing track")]
    public float trackWidth = 300f;

    [Tooltip("Y position of hit window from bottom (percentage of screen height)")]
    [Range(0.1f, 0.5f)]
    public float hitWindowPosition = 0.2f;

    [Header("References")]
    [Tooltip("Reference to the TrickInputSystem")]
    public TrickInputSystem trickInputSystem;

    [Header("Debug")]
    public bool showDebugInfo = true;

    // Internal state
    private List<ScrollingTrick> activeTricks = new List<ScrollingTrick>();
    private float nextSpawnTime;
    private int score = 0;
    private int combo = 0;
    private int maxCombo = 0;
    private string lastResult = "";
    private string lastTrickName = "";
    private float lastResultTime;

    // Track statistics
    private int perfectCount = 0;
    private int goodCount = 0;
    private int okCount = 0;
    private int missCount = 0;
    private int earlyCount = 0;

    private class ScrollingTrick
    {
        public TrickDefinition trick;
        public float spawnTime;
        public float targetTime; // When it should be hit (center of window)
        public bool completed;
        public bool missed;
    }

    private void Start()
    {
        if (trickInputSystem == null)
        {
            trickInputSystem = FindObjectOfType<TrickInputSystem>();
        }

        if (trickInputSystem != null)
        {
            trickInputSystem.OnTrickMatched += OnTrickPerformed;
        }

        nextSpawnTime = Time.time + 1f; // Start spawning after 1 second
    }

    private void OnDestroy()
    {
        if (trickInputSystem != null)
        {
            trickInputSystem.OnTrickMatched -= OnTrickPerformed;
        }
    }

    private void Update()
    {
        // Spawn new tricks
        if (Time.time >= nextSpawnTime)
        {
            SpawnRandomTrick();
            nextSpawnTime = Time.time + trickInterval;
        }

        // Check for missed tricks
        CheckMissedTricks();

        // Clean up old tricks
        CleanupTricks();
    }

    private void SpawnRandomTrick()
    {
        if (trickInputSystem == null || trickInputSystem.trickDatabase.Count == 0)
            return;

        // Pick a random trick
        int index = Random.Range(0, trickInputSystem.trickDatabase.Count);
        TrickDefinition trick = trickInputSystem.trickDatabase[index];

        var scrollingTrick = new ScrollingTrick
        {
            trick = trick,
            spawnTime = Time.time,
            targetTime = Time.time + scrollDuration,
            completed = false,
            missed = false
        };

        activeTricks.Add(scrollingTrick);
    }

    private void OnTrickPerformed(TrickMatchResult result)
    {
        // Find if this trick matches any active scrolling trick
        for (int i = 0; i < activeTricks.Count; i++)
        {
            var scrollingTrick = activeTricks[i];
            if (scrollingTrick.completed || scrollingTrick.missed)
                continue;

            if (scrollingTrick.trick == result.trick)
            {
                // Calculate timing accuracy
                float timeDiff = Mathf.Abs(Time.time - scrollingTrick.targetTime);

                string trickName = scrollingTrick.trick.trickName;

                if (Time.time < scrollingTrick.targetTime - okWindow)
                {
                    // Too early
                    SetResult("EARLY!", Color.red, trickName);
                    earlyCount++;
                    combo = 0;
                }
                else if (timeDiff <= perfectWindow)
                {
                    SetResult("PERFECT!", Color.cyan, trickName);
                    score += 100 * (combo + 1);
                    combo++;
                    perfectCount++;
                }
                else if (timeDiff <= goodWindow)
                {
                    SetResult("GREAT!", Color.green, trickName);
                    score += 75 * (combo + 1);
                    combo++;
                    goodCount++;
                }
                else if (timeDiff <= okWindow)
                {
                    SetResult("OK", Color.yellow, trickName);
                    score += 50 * (combo + 1);
                    combo++;
                    okCount++;
                }
                else
                {
                    // Too late but still hit
                    SetResult("LATE", Color.yellow, trickName);
                    score += 25;
                    combo = 0;
                }

                if (combo > maxCombo)
                    maxCombo = combo;

                scrollingTrick.completed = true;
                activeTricks[i] = scrollingTrick;
                return;
            }
        }
    }

    private void CheckMissedTricks()
    {
        for (int i = 0; i < activeTricks.Count; i++)
        {
            var scrollingTrick = activeTricks[i];
            if (scrollingTrick.completed || scrollingTrick.missed)
                continue;

            // If trick has passed the window
            if (Time.time > scrollingTrick.targetTime + okWindow)
            {
                scrollingTrick.missed = true;
                activeTricks[i] = scrollingTrick;
                SetResult("MISS", Color.red, scrollingTrick.trick.trickName);
                missCount++;
                combo = 0;
            }
        }
    }

    private void CleanupTricks()
    {
        // Remove tricks that are done and off screen
        activeTricks.RemoveAll(t =>
            (t.completed || t.missed) &&
            Time.time > t.targetTime + 1f);
    }

    private void SetResult(string text, Color color, string trickName = "")
    {
        lastResult = text;
        lastTrickName = trickName;
        lastResultTime = Time.time;
    }

    private void OnGUI()
    {
        // Center the track horizontally, full screen height
        float trackX = (Screen.width - trackWidth) / 2f;
        float trackTop = 0f;
        float trackHeight = Screen.height;
        float trackBottom = trackHeight;
        float hitWindowCenterY = Screen.height * (1f - hitWindowPosition);

        // Draw track background
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(new Rect(trackX, trackTop, trackWidth, trackHeight), Texture2D.whiteTexture);

        // Draw hit window zones
        float scrollDistance = hitWindowCenterY + 60f; // Distance from spawn to hit line
        float pixelsPerSecond = scrollDistance / scrollDuration;

        // OK zone (largest)
        float okZoneHeight = okWindow * 2 * pixelsPerSecond;
        GUI.color = new Color(1, 1, 0, 0.2f);
        GUI.DrawTexture(new Rect(trackX, hitWindowCenterY - okZoneHeight / 2, trackWidth, okZoneHeight), Texture2D.whiteTexture);

        // Good zone
        float goodZoneHeight = goodWindow * 2 * pixelsPerSecond;
        GUI.color = new Color(0, 1, 0, 0.3f);
        GUI.DrawTexture(new Rect(trackX, hitWindowCenterY - goodZoneHeight / 2, trackWidth, goodZoneHeight), Texture2D.whiteTexture);

        // Perfect zone (smallest)
        float perfectZoneHeight = perfectWindow * 2 * pixelsPerSecond;
        GUI.color = new Color(0, 1, 1, 0.4f);
        GUI.DrawTexture(new Rect(trackX, hitWindowCenterY - perfectZoneHeight / 2, trackWidth, perfectZoneHeight), Texture2D.whiteTexture);

        // Draw hit line
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(trackX, hitWindowCenterY - 2, trackWidth, 4), Texture2D.whiteTexture);

        // Draw scrolling tricks
        GUIStyle trickStyle = new GUIStyle(GUI.skin.box);
        trickStyle.alignment = TextAnchor.MiddleCenter;
        trickStyle.fontSize = 14;
        trickStyle.wordWrap = true;

        foreach (var scrollingTrick in activeTricks)
        {
            float progress = (Time.time - scrollingTrick.spawnTime) / scrollDuration;
            float trickY = Mathf.Lerp(-60f, hitWindowCenterY, progress); // Start above screen

            // Set color based on state
            if (scrollingTrick.completed)
            {
                GUI.color = new Color(0, 1, 0, 0.5f);
            }
            else if (scrollingTrick.missed)
            {
                GUI.color = new Color(1, 0, 0, 0.5f);
            }
            else
            {
                GUI.color = Color.white;
            }

            trickStyle.normal.textColor = GUI.color;

            // Draw trick box
            float boxHeight = 60f;
            GUI.Box(new Rect(trackX + 10, trickY - boxHeight / 2, trackWidth - 20, boxHeight),
                scrollingTrick.trick.trickName + "\n" + scrollingTrick.trick.GetInputSequenceString(),
                trickStyle);
        }

        // Draw result text in CENTER of screen
        if (Time.time - lastResultTime < 1f)
        {
            GUIStyle resultStyle = new GUIStyle(GUI.skin.label);
            resultStyle.fontSize = 48;
            resultStyle.alignment = TextAnchor.MiddleCenter;
            resultStyle.fontStyle = FontStyle.Bold;

            // Color based on result
            if (lastResult == "PERFECT!")
                GUI.color = Color.cyan;
            else if (lastResult == "GREAT!")
                GUI.color = Color.green;
            else if (lastResult == "OK")
                GUI.color = Color.yellow;
            else
                GUI.color = Color.red;

            // Center of screen
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;

            // Draw shadow for readability
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.Label(new Rect(centerX - 202, centerY - 52, 400, 100), lastResult, resultStyle);

            // Draw main text
            if (lastResult == "PERFECT!")
                GUI.color = Color.cyan;
            else if (lastResult == "GREAT!")
                GUI.color = Color.green;
            else if (lastResult == "OK")
                GUI.color = Color.yellow;
            else
                GUI.color = Color.red;

            GUI.Label(new Rect(centerX - 200, centerY - 50, 400, 100), lastResult, resultStyle);

            // Also show the trick name below
            GUIStyle trickNameStyle = new GUIStyle(GUI.skin.label);
            trickNameStyle.fontSize = 24;
            trickNameStyle.alignment = TextAnchor.MiddleCenter;
            trickNameStyle.normal.textColor = Color.white;

            GUI.color = Color.white;
            GUI.Label(new Rect(centerX - 200, centerY + 10, 400, 50), lastTrickName, trickNameStyle);
        }

        // Draw score and combo (top right corner)
        GUI.color = Color.white;
        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label);
        scoreStyle.fontSize = 24;
        scoreStyle.alignment = TextAnchor.UpperRight;

        GUI.Label(new Rect(Screen.width - 250, 20, 230, 30), $"Score: {score}", scoreStyle);
        GUI.Label(new Rect(Screen.width - 250, 50, 230, 30), $"Combo: {combo}x (Max: {maxCombo})", scoreStyle);

        // Draw stats if debug enabled (bottom left corner)
        if (showDebugInfo)
        {
            GUIStyle statsStyle = new GUIStyle(GUI.skin.box);
            statsStyle.fontSize = 14;
            statsStyle.alignment = TextAnchor.UpperLeft;
            statsStyle.normal.textColor = Color.white;

            string stats = $"Stats:\n";
            stats += $"Perfect: {perfectCount}\n";
            stats += $"Great: {goodCount}\n";
            stats += $"OK: {okCount}\n";
            stats += $"Miss: {missCount}\n";
            stats += $"Early: {earlyCount}";

            GUI.Box(new Rect(10, Screen.height - 140, 120, 130), stats, statsStyle);
        }
    }

    /// <summary>
    /// Resets the score and statistics
    /// </summary>
    public void ResetScore()
    {
        score = 0;
        combo = 0;
        maxCombo = 0;
        perfectCount = 0;
        goodCount = 0;
        okCount = 0;
        missCount = 0;
        earlyCount = 0;
        activeTricks.Clear();
    }
}
