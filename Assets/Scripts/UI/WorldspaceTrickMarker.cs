using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Spawns 3D worldspace trick markers on the road that scroll toward the player.
/// Works alongside TrickTrackUI to show tricks in the 3D scene.
/// </summary>
public class WorldspaceTrickMarker : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The TrickTrackUI to sync with (optional - can work standalone)")]
    public TrickTrackUI trickTrackUI;

    [Tooltip("The trick input system for trick database")]
    public TrickInputSystem trickInputSystem;

    [Tooltip("Icon helper for input icons")]
    public TrickIconHelper iconHelper;

    [Header("Spawn Settings")]
    [Tooltip("How far ahead markers spawn (Z distance from player)")]
    public float spawnDistance = 50f;

    [Tooltip("Z position where markers reach the player (hit line)")]
    public float hitLineZ = 0f;

    [Tooltip("Time it takes for marker to travel from spawn to hit line")]
    public float travelDuration = 2f;

    [Tooltip("Time between spawning new markers")]
    public float spawnInterval = 3f;

    [Header("Marker Appearance")]
    [Tooltip("Width of the marker (road width)")]
    public float markerWidth = 10f;

    [Tooltip("Height/thickness of the marker")]
    public float markerHeight = 0.5f;

    [Tooltip("Depth of the marker along Z")]
    public float markerDepth = 2f;

    [Tooltip("Color for RS-first tricks (ollie)")]
    public Color rightStickColor = new Color(1f, 0.8f, 0.5f, 0.3f);

    [Tooltip("Color for LS-first tricks (nollie)")]
    public Color leftStickColor = new Color(0.5f, 0.8f, 1f, 0.3f);

    [Header("Text Settings")]
    [Tooltip("Font size for trick name")]
    public float fontSize = 10f;

    [Tooltip("Height offset for text above marker")]
    public float textHeightOffset = 1f;

    [Tooltip("Starting scale for text when far away")]
    public float textStartScale = 3f;

    // Runtime state
    private List<WorldspaceMarker> activeMarkers = new List<WorldspaceMarker>();
    private float nextSpawnTime;
    private Material markerMaterial;

    private class WorldspaceMarker
    {
        public TrickDefinition trick;
        public GameObject markerObject;
        public GameObject textObject;
        public TextMeshProUGUI textMesh;
        public float spawnTime;
        public float targetTime;
        public bool completed;
        public bool missed;
    }

    private void Start()
    {
        // Find references if not assigned
        if (trickTrackUI == null)
            trickTrackUI = FindObjectOfType<TrickTrackUI>();

        if (trickInputSystem == null)
            trickInputSystem = FindObjectOfType<TrickInputSystem>();

        if (iconHelper == null)
            iconHelper = TrickIconHelper.Instance;

        // Subscribe to trick events
        if (trickInputSystem != null)
            trickInputSystem.OnTrickMatched += OnTrickPerformed;

        // Sync timing with TrickTrackUI if available
        if (trickTrackUI != null)
        {
            travelDuration = trickTrackUI.scrollDuration;
            spawnInterval = trickTrackUI.trickInterval;
        }

        // Create shared material
        CreateMarkerMaterial();

        nextSpawnTime = Time.time + 1f;
    }

    private void OnDestroy()
    {
        if (trickInputSystem != null)
            trickInputSystem.OnTrickMatched -= OnTrickPerformed;
    }

    private void CreateMarkerMaterial()
    {
        // Create a simple unlit material with transparency
        markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (markerMaterial == null)
        {
            // Fallback for built-in render pipeline
            markerMaterial = new Material(Shader.Find("Unlit/Color"));
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        // Spawn new markers (only if not syncing with TrickTrackUI, otherwise it handles spawning)
        if (trickTrackUI == null && Time.time >= nextSpawnTime)
        {
            SpawnMarker(null);
            nextSpawnTime = Time.time + spawnInterval;
        }

        // Update marker positions
        UpdateMarkers();

        // Check for missed markers
        CheckMissedMarkers();
    }

    /// <summary>
    /// Spawns a marker for a trick. Called by TrickTrackUI when synced, or internally when standalone.
    /// </summary>
    public void SpawnMarker(TrickDefinition trick)
    {
        if (trick == null && trickInputSystem != null && trickInputSystem.trickDatabase.Count > 0)
        {
            int index = Random.Range(0, trickInputSystem.trickDatabase.Count);
            trick = trickInputSystem.trickDatabase[index];
        }

        if (trick == null) return;

        // Create empty parent transform that handles movement
        GameObject markerRoot = new GameObject($"TrickMarker_{trick.trickName}");
        markerRoot.transform.position = new Vector3(0, markerHeight / 2f, spawnDistance);

        // Create cube as child of root (scaling won't affect siblings)
        GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeObj.name = "Cube";
        cubeObj.transform.SetParent(markerRoot.transform, false);
        cubeObj.transform.localPosition = Vector3.zero;
        cubeObj.transform.localScale = new Vector3(markerWidth, markerHeight, markerDepth);

        // Set color based on first stick
        Renderer renderer = cubeObj.GetComponent<Renderer>();
        Material mat = new Material(markerMaterial);

        bool isLeftStickFirst = trick.inputSequence != null &&
                                trick.inputSequence.Count > 0 &&
                                trick.inputSequence[0].stickType == StickType.LeftStick;

        mat.color = isLeftStickFirst ? leftStickColor : rightStickColor;
        renderer.material = mat;

        // Remove collider (we don't need physics)
        Destroy(cubeObj.GetComponent<Collider>());

        // Create World Space Canvas as sibling to cube (both under markerRoot)
        GameObject canvasObj = new GameObject("TrickCanvas");
        canvasObj.transform.SetParent(markerRoot.transform, false);
        canvasObj.transform.localPosition = new Vector3(0, textHeightOffset, 0);
        // Face toward camera/player: rotate 180 on Y, then flip X scale to un-mirror text
        canvasObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        canvasObj.transform.localScale = new Vector3(-1f, 1f, 1f) * textStartScale * 0.01f;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 150);
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.anchoredPosition = Vector2.zero;

        // Create trick name text
        GameObject nameObj = new GameObject("TrickName");
        nameObj.transform.SetParent(canvasObj.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.6f);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.pivot = new Vector2(0.5f, 0.5f);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = trick.trickName;
        nameTmp.fontSize = 36;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.color = Color.white;

        // Create icons container
        GameObject iconsObj = new GameObject("Icons");
        iconsObj.transform.SetParent(canvasObj.transform, false);
        RectTransform iconsRect = iconsObj.AddComponent<RectTransform>();
        iconsRect.anchorMin = new Vector2(0, 0);
        iconsRect.anchorMax = new Vector2(1, 0.5f);
        iconsRect.pivot = new Vector2(0.5f, 0.5f);
        iconsRect.anchoredPosition = Vector2.zero;
        iconsRect.offsetMin = Vector2.zero;
        iconsRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup layout = iconsObj.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 5;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Create input icons
        CreateWorldspaceInputIcons(iconsObj.transform, trick);

        // Create marker data (markerObject is the root that we move)
        var marker = new WorldspaceMarker
        {
            trick = trick,
            markerObject = markerRoot,
            textObject = canvasObj,
            textMesh = nameTmp,
            spawnTime = Time.time,
            targetTime = Time.time + travelDuration,
            completed = false,
            missed = false
        };

        activeMarkers.Add(marker);
    }

    private void CreateWorldspaceInputIcons(Transform container, TrickDefinition trick)
    {
        if (iconHelper == null || trick.inputSequence == null) return;

        float iconSize = 40f;

        for (int i = 0; i < trick.inputSequence.Count; i++)
        {
            InputStep step = trick.inputSequence[i];

            // Create step container
            GameObject stepObj = new GameObject($"Step_{i}");
            stepObj.transform.SetParent(container, false);
            stepObj.transform.localPosition = Vector3.zero;
            RectTransform stepRt = stepObj.AddComponent<RectTransform>();
            stepRt.pivot = new Vector2(0.5f, 0.5f);
            stepRt.anchoredPosition3D = Vector3.zero;
            stepRt.sizeDelta = new Vector2(iconSize + 8, iconSize + 24);

            // Create stick label (LS/RS)
            GameObject labelObj = new GameObject("StickLabel");
            labelObj.transform.SetParent(stepObj.transform, false);
            labelObj.transform.localPosition = Vector3.zero;
            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.anchorMin = new Vector2(0.5f, 1);
            labelRt.anchorMax = new Vector2(0.5f, 1);
            labelRt.anchoredPosition3D = new Vector3(0, -8, 0);
            labelRt.sizeDelta = new Vector2(iconSize, 20);

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = step.stickType == StickType.LeftStick ? "LS" : "RS";
            labelTmp.fontSize = 16;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = step.stickType == StickType.LeftStick ?
                new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.8f, 0.5f);

            // Create icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(stepObj.transform, false);
            iconObj.transform.localPosition = Vector3.zero;
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition3D = new Vector3(0, -4, 0);
            iconRt.sizeDelta = new Vector2(iconSize, iconSize);

            RawImage iconImg = iconObj.AddComponent<RawImage>();
            iconImg.color = Color.white;

            // Set texture and rotation
            if (step.inputType == InputType.Drag && step.dragTurnType != DragTurnType.None)
            {
                iconImg.texture = iconHelper.GetTurnTexture(step.dragTurnType);
                if (TrickIconHelper.ShouldFlipHorizontal(step.dragTurnType))
                {
                    iconRt.localScale = new Vector3(-1, 1, 1);
                }
            }
            else if (step.direction != StickDirection.None && iconHelper.arrowUp != null)
            {
                iconImg.texture = iconHelper.arrowUp;
                float rotation = TrickIconHelper.GetDirectionRotation(step.direction);
                iconRt.localRotation = Quaternion.Euler(0, 0, rotation);
            }

            // Add arrow between steps
            if (i < trick.inputSequence.Count - 1)
            {
                GameObject arrowObj = new GameObject("Arrow");
                arrowObj.transform.SetParent(container, false);
                arrowObj.transform.localPosition = Vector3.zero;
                RectTransform arrowRt = arrowObj.AddComponent<RectTransform>();
                arrowRt.pivot = new Vector2(0.5f, 0.5f);
                arrowRt.anchoredPosition3D = Vector3.zero;
                arrowRt.sizeDelta = new Vector2(20, iconSize);

                TextMeshProUGUI arrowTmp = arrowObj.AddComponent<TextMeshProUGUI>();
                arrowTmp.text = ">";
                arrowTmp.fontSize = 24;
                arrowTmp.alignment = TextAlignmentOptions.Center;
                arrowTmp.color = new Color(0.6f, 0.6f, 0.6f);
            }
        }
    }

    private void UpdateMarkers()
    {
        foreach (var marker in activeMarkers)
        {
            if (marker.markerObject == null) continue;

            // Calculate progress (0 = spawn, 1 = hit line)
            float progress = (Time.time - marker.spawnTime) / travelDuration;

            // Interpolate Z position
            float currentZ = Mathf.Lerp(spawnDistance, hitLineZ, progress);

            // Move the root transform (children move with it)
            Vector3 pos = marker.markerObject.transform.position;
            pos.z = currentZ;
            marker.markerObject.transform.position = pos;

            // Update text scale with distance (larger when far, smaller when close)
            // Preserve negative X scale for proper text orientation
            if (marker.textObject != null)
            {
                float scale = Mathf.Lerp(textStartScale, 1f, progress) * 0.01f;
                marker.textObject.transform.localScale = new Vector3(-scale, scale, scale);
            }

            // Update visual state (renderer is on child cube)
            Renderer renderer = marker.markerObject.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Color baseColor = marker.trick.inputSequence != null &&
                                  marker.trick.inputSequence.Count > 0 &&
                                  marker.trick.inputSequence[0].stickType == StickType.LeftStick
                                  ? leftStickColor : rightStickColor;

                if (marker.completed)
                {
                    renderer.material.color = new Color(0, 1, 0, 0.5f); // Green for completed
                }
                else if (marker.missed)
                {
                    renderer.material.color = new Color(1, 0, 0, 0.5f); // Red for missed
                }
                else
                {
                    renderer.material.color = baseColor;
                }
            }
        }

        // Cleanup old markers (destroying root destroys all children)
        activeMarkers.RemoveAll(m =>
        {
            if ((m.completed || m.missed) && Time.time > m.targetTime + 1f)
            {
                if (m.markerObject != null) Destroy(m.markerObject);
                return true;
            }
            return false;
        });
    }

    private void OnTrickPerformed(TrickMatchResult result)
    {
        // Find matching marker
        foreach (var marker in activeMarkers)
        {
            if (marker.completed || marker.missed) continue;
            if (marker.trick != result.trick) continue;

            // Check if within timing window
            float timeDiff = Mathf.Abs(Time.time - marker.targetTime);
            if (timeDiff <= 0.5f) // Use a generous window
            {
                marker.completed = true;

                // Update text to show result
                if (marker.textMesh != null)
                {
                    marker.textMesh.color = Color.green;
                }
                return;
            }
        }
    }

    private void CheckMissedMarkers()
    {
        foreach (var marker in activeMarkers)
        {
            if (marker.completed || marker.missed) continue;

            // Check if past the hit line
            if (Time.time > marker.targetTime + 0.35f)
            {
                marker.missed = true;

                // Update text to show missed
                if (marker.textMesh != null)
                {
                    marker.textMesh.color = Color.red;
                }
            }
        }
    }

    /// <summary>
    /// Gets the list of active markers (for syncing with UI)
    /// </summary>
    public List<TrickDefinition> GetActiveMarkerTricks()
    {
        List<TrickDefinition> tricks = new List<TrickDefinition>();
        foreach (var marker in activeMarkers)
        {
            if (!marker.completed && !marker.missed)
                tricks.Add(marker.trick);
        }
        return tricks;
    }
}
