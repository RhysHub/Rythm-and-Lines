using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all game UI elements using Canvas-based UI.
/// Can be seen and edited in the Unity Editor.
/// </summary>
[ExecuteInEditMode]
public class GameUIManager : MonoBehaviour
{
    [Header("References")]
    public Canvas mainCanvas;
    public TrickInputSystem trickInputSystem;
    public TrickAnimator trickAnimator;

    [Header("Trick Track UI")]
    public RectTransform trickTrackPanel;
    public RectTransform hitLine;
    public RectTransform perfectZone;
    public RectTransform greatZone;
    public RectTransform okZone;

    [Header("Stick Indicators")]
    public RectTransform leftStickPanel;
    public RectTransform rightStickPanel;
    public RectTransform leftStickIndicator;
    public RectTransform rightStickIndicator;
    public RectTransform leftStickDirection;
    public RectTransform rightStickDirection;
    public TextMeshProUGUI leftStickLabel;
    public TextMeshProUGUI rightStickLabel;

    [Header("Score UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;

    [Header("Result Display")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI trickNameText;

    [Header("Debug Panel")]
    public RectTransform debugPanel;
    public TextMeshProUGUI debugText;

    [Header("Settings")]
    public float stickIndicatorSize = 80f;
    public float trackWidth = 200f;

    [ContextMenu("Create All UI")]
    public void CreateAllUI()
    {
        CreateCanvas();
        CreateTrickTrack();
        CreateStickIndicators();
        CreateScoreUI();
        CreateResultDisplay();
        CreateDebugPanel();
    }

    [ContextMenu("Create Canvas")]
    public void CreateCanvas()
    {
        if (mainCanvas == null)
        {
            GameObject canvasObj = new GameObject("GameUICanvas");
            canvasObj.transform.SetParent(transform);
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
    }

    [ContextMenu("Create Trick Track")]
    public void CreateTrickTrack()
    {
        if (mainCanvas == null) CreateCanvas();

        // Create track panel on the right side
        if (trickTrackPanel == null)
        {
            GameObject trackObj = CreateUIPanel("TrickTrack", mainCanvas.transform);
            trickTrackPanel = trackObj.GetComponent<RectTransform>();
            trickTrackPanel.anchorMin = new Vector2(1, 0);
            trickTrackPanel.anchorMax = new Vector2(1, 1);
            trickTrackPanel.pivot = new Vector2(1, 0.5f);
            trickTrackPanel.anchoredPosition = new Vector2(-20, 0);
            trickTrackPanel.sizeDelta = new Vector2(trackWidth, 0);

            Image bg = trackObj.GetComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);
        }

        // Create hit zones
        if (okZone == null)
        {
            okZone = CreateZone("OKZone", trickTrackPanel, new Color(1, 1, 0, 0.2f), 140);
        }
        if (greatZone == null)
        {
            greatZone = CreateZone("GreatZone", trickTrackPanel, new Color(0, 1, 0, 0.3f), 80);
        }
        if (perfectZone == null)
        {
            perfectZone = CreateZone("PerfectZone", trickTrackPanel, new Color(0, 1, 1, 0.4f), 40);
        }

        // Create hit line
        if (hitLine == null)
        {
            GameObject lineObj = CreateUIPanel("HitLine", trickTrackPanel);
            hitLine = lineObj.GetComponent<RectTransform>();
            hitLine.anchorMin = new Vector2(0, 0.2f);
            hitLine.anchorMax = new Vector2(1, 0.2f);
            hitLine.sizeDelta = new Vector2(0, 4);
            lineObj.GetComponent<Image>().color = Color.white;
        }
    }

    private RectTransform CreateZone(string name, Transform parent, Color color, float height)
    {
        GameObject obj = CreateUIPanel(name, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.2f);
        rt.anchorMax = new Vector2(1, 0.2f);
        rt.sizeDelta = new Vector2(0, height);
        obj.GetComponent<Image>().color = color;
        return rt;
    }

    [ContextMenu("Create Stick Indicators")]
    public void CreateStickIndicators()
    {
        if (mainCanvas == null) CreateCanvas();

        float spacing = 20f;
        float totalWidth = stickIndicatorSize * 2 + spacing;

        // Left stick
        if (leftStickPanel == null)
        {
            leftStickPanel = CreateStickPanel("LeftStickPanel", -totalWidth / 2 - stickIndicatorSize / 2);
            leftStickIndicator = CreateStickDot("Indicator", leftStickPanel, Color.yellow);
            leftStickDirection = CreateStickDot("Direction", leftStickPanel, Color.green);
            leftStickLabel = CreateStickLabel("Label", leftStickPanel, "LS");
        }

        // Right stick
        if (rightStickPanel == null)
        {
            rightStickPanel = CreateStickPanel("RightStickPanel", totalWidth / 2 - stickIndicatorSize / 2);
            rightStickIndicator = CreateStickDot("Indicator", rightStickPanel, Color.yellow);
            rightStickDirection = CreateStickDot("Direction", rightStickPanel, Color.green);
            rightStickLabel = CreateStickLabel("Label", rightStickPanel, "RS");
        }
    }

    private RectTransform CreateStickPanel(string name, float xOffset)
    {
        GameObject obj = CreateUIPanel(name, mainCanvas.transform);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(xOffset, 50);
        rt.sizeDelta = new Vector2(stickIndicatorSize, stickIndicatorSize);

        Image img = obj.GetComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Make it circular by adding a mask or just leave as square for now
        return rt;
    }

    private RectTransform CreateStickDot(string name, Transform parent, Color color)
    {
        GameObject obj = CreateUIPanel(name, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(16, 16);
        rt.anchoredPosition = Vector2.zero;

        Image img = obj.GetComponent<Image>();
        img.color = color;

        return rt;
    }

    private TextMeshProUGUI CreateStickLabel(string name, Transform parent, string text)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -5);
        rt.sizeDelta = new Vector2(60, 25);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return tmp;
    }

    [ContextMenu("Create Score UI")]
    public void CreateScoreUI()
    {
        if (mainCanvas == null) CreateCanvas();

        if (scoreText == null)
        {
            scoreText = CreateText("ScoreText", mainCanvas.transform, "Score: 0",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), 24);
            scoreText.alignment = TextAlignmentOptions.TopRight;
        }

        if (comboText == null)
        {
            comboText = CreateText("ComboText", mainCanvas.transform, "Combo: 0x",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -50), 20);
            comboText.alignment = TextAlignmentOptions.TopRight;
        }
    }

    [ContextMenu("Create Result Display")]
    public void CreateResultDisplay()
    {
        if (mainCanvas == null) CreateCanvas();

        if (resultText == null)
        {
            resultText = CreateText("ResultText", mainCanvas.transform, "",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), 48);
            resultText.alignment = TextAlignmentOptions.Center;
            resultText.fontStyle = FontStyles.Bold;
        }

        if (trickNameText == null)
        {
            trickNameText = CreateText("TrickNameText", mainCanvas.transform, "",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), 24);
            trickNameText.alignment = TextAlignmentOptions.Center;
        }
    }

    [ContextMenu("Create Debug Panel")]
    public void CreateDebugPanel()
    {
        if (mainCanvas == null) CreateCanvas();

        if (debugPanel == null)
        {
            GameObject obj = CreateUIPanel("DebugPanel", mainCanvas.transform);
            debugPanel = obj.GetComponent<RectTransform>();
            debugPanel.anchorMin = new Vector2(1, 1);
            debugPanel.anchorMax = new Vector2(1, 1);
            debugPanel.pivot = new Vector2(1, 1);
            debugPanel.anchoredPosition = new Vector2(-20, -100);
            debugPanel.sizeDelta = new Vector2(220, 150);

            obj.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);

            debugText = CreateText("DebugText", debugPanel, "[Debug Info]",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -10), 12);
            debugText.alignment = TextAlignmentOptions.TopLeft;
        }
    }

    private GameObject CreateUIPanel(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.AddComponent<RectTransform>();
        obj.AddComponent<CanvasRenderer>();
        obj.AddComponent<Image>();
        return obj;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, int fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(300, 50);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;

        return tmp;
    }

    // Auto-find references
    private void Awake()
    {
        if (Application.isPlaying)
        {
            if (trickInputSystem == null)
                trickInputSystem = FindObjectOfType<TrickInputSystem>();
            if (trickAnimator == null)
                trickAnimator = FindObjectOfType<TrickAnimator>();
        }
    }
}
