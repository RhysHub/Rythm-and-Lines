using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Canvas-based stick position indicator UI.
/// Matches the OnGUI debug visuals with circles, deadzone, 8-way markers, and direction arrows.
/// </summary>
public class StickIndicatorUI : MonoBehaviour
{
    [Header("References")]
    public InputReader inputReader;

    [Header("Left Stick UI")]
    public RectTransform leftStickPanel;
    public Image leftStickBackground;
    public Image leftStickDeadzone;
    public RectTransform leftStickRawIndicator;
    public Image leftStickLine;
    public RectTransform leftStickDirectionIndicator;
    public TextMeshProUGUI leftStickDirectionText;
    public TextMeshProUGUI leftStickValueText;
    public RectTransform[] leftStick8WayMarkers;

    [Header("Right Stick UI")]
    public RectTransform rightStickPanel;
    public Image rightStickBackground;
    public Image rightStickDeadzone;
    public RectTransform rightStickRawIndicator;
    public Image rightStickLine;
    public RectTransform rightStickDirectionIndicator;
    public TextMeshProUGUI rightStickDirectionText;
    public TextMeshProUGUI rightStickValueText;
    public RectTransform[] rightStick8WayMarkers;

    [Header("Settings")]
    public float panelSize = 100f;
    public float indicatorRadius = 42f;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);
    public Color outerRingColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color deadzoneColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);
    public Color markerColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Color rawIndicatorColor = Color.yellow;
    public Color lineColor = new Color(1f, 1f, 0f, 0.5f);
    public Color directionRingColor = new Color(0f, 1f, 0f, 0.5f);
    public Color directionTextColor = Color.green;

    private void Update()
    {
        if (inputReader == null)
        {
            inputReader = FindObjectOfType<InputReader>();
            if (inputReader == null) return;
        }

        UpdateStick(
            leftStickPanel, leftStickRawIndicator, leftStickLine,
            leftStickDirectionIndicator, leftStickDirectionText, leftStickValueText,
            inputReader.GetRawLeftStick(), inputReader.GetCurrentLeftStickDirection());

        UpdateStick(
            rightStickPanel, rightStickRawIndicator, rightStickLine,
            rightStickDirectionIndicator, rightStickDirectionText, rightStickValueText,
            inputReader.GetRawRightStick(), inputReader.GetCurrentRightStickDirection());
    }

    private void UpdateStick(RectTransform panel, RectTransform rawIndicator, Image line,
        RectTransform dirIndicator, TextMeshProUGUI dirText, TextMeshProUGUI valueText,
        Vector2 rawInput, StickDirection direction)
    {
        if (panel == null) return;

        // Update raw position indicator
        if (rawIndicator != null)
        {
            Vector2 rawPos = rawInput * indicatorRadius;
            rawIndicator.anchoredPosition = rawPos;
        }

        // Update line from center to raw position
        if (line != null)
        {
            if (rawInput.magnitude > 0.01f)
            {
                line.gameObject.SetActive(true);
                Vector2 rawPos = rawInput * indicatorRadius;
                float length = rawPos.magnitude;
                float angle = Mathf.Atan2(rawPos.y, rawPos.x) * Mathf.Rad2Deg;

                line.rectTransform.sizeDelta = new Vector2(length, 2f);
                line.rectTransform.anchoredPosition = rawPos / 2f;
                line.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                line.gameObject.SetActive(false);
            }
        }

        // Update direction indicator
        if (dirIndicator != null)
        {
            if (direction != StickDirection.None)
            {
                dirIndicator.gameObject.SetActive(true);
                Vector2 dirVec = direction.ToVector2() * (indicatorRadius - 10f);
                dirIndicator.anchoredPosition = dirVec;

                if (dirText != null)
                {
                    dirText.gameObject.SetActive(true);
                    dirText.text = direction.ToReadableString();
                    dirText.rectTransform.anchoredPosition = dirVec;
                }
            }
            else
            {
                dirIndicator.gameObject.SetActive(false);
                if (dirText != null) dirText.gameObject.SetActive(false);
            }
        }

        // Update value text
        if (valueText != null)
        {
            valueText.text = $"X:{rawInput.x:F2} Y:{rawInput.y:F2}";
        }
    }

    [ContextMenu("Auto Setup UI")]
    public void AutoSetupUI()
    {
        // Find or create canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("StickIndicatorCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvasObj.transform);
        }

        float spacing = 20f;

        // Create left stick panel
        CreateFullStickPanel("LeftStickPanel", canvas.transform, -spacing / 2 - panelSize / 2, "LS",
            out leftStickPanel, out leftStickBackground, out leftStickDeadzone,
            out leftStickRawIndicator, out leftStickLine,
            out leftStickDirectionIndicator, out leftStickDirectionText,
            out leftStickValueText, out leftStick8WayMarkers);

        // Create right stick panel
        CreateFullStickPanel("RightStickPanel", canvas.transform, spacing / 2 + panelSize / 2, "RS",
            out rightStickPanel, out rightStickBackground, out rightStickDeadzone,
            out rightStickRawIndicator, out rightStickLine,
            out rightStickDirectionIndicator, out rightStickDirectionText,
            out rightStickValueText, out rightStick8WayMarkers);
    }

    private void CreateFullStickPanel(string name, Transform parent, float xOffset, string label,
        out RectTransform panel, out Image background, out Image deadzone,
        out RectTransform rawIndicator, out Image line,
        out RectTransform dirIndicator, out TextMeshProUGUI dirText,
        out TextMeshProUGUI valueText, out RectTransform[] markers)
    {
        // Main panel
        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent);
        panel = panelObj.AddComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0);
        panel.anchorMax = new Vector2(0.5f, 0);
        panel.pivot = new Vector2(0.5f, 0);
        panel.anchoredPosition = new Vector2(xOffset, 60);
        panel.sizeDelta = new Vector2(panelSize, panelSize);

        // Background circle
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(panelObj.transform);
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = new Vector2(panelSize, panelSize);
        bgRt.anchoredPosition = Vector2.zero;
        background = bgObj.AddComponent<Image>();
        background.color = backgroundColor;
        // Make it circular
        background.type = Image.Type.Simple;

        // Outer ring
        GameObject ringObj = new GameObject("OuterRing");
        ringObj.transform.SetParent(panelObj.transform);
        RectTransform ringRt = ringObj.AddComponent<RectTransform>();
        ringRt.anchorMin = new Vector2(0.5f, 0.5f);
        ringRt.anchorMax = new Vector2(0.5f, 0.5f);
        ringRt.sizeDelta = new Vector2(indicatorRadius * 2, indicatorRadius * 2);
        ringRt.anchoredPosition = Vector2.zero;
        Image ringImg = ringObj.AddComponent<Image>();
        ringImg.color = outerRingColor;

        // Deadzone circle
        float deadzoneRadius = indicatorRadius * (inputReader != null ? inputReader.deadzone : 0.2f);
        GameObject dzObj = new GameObject("Deadzone");
        dzObj.transform.SetParent(panelObj.transform);
        RectTransform dzRt = dzObj.AddComponent<RectTransform>();
        dzRt.anchorMin = new Vector2(0.5f, 0.5f);
        dzRt.anchorMax = new Vector2(0.5f, 0.5f);
        dzRt.sizeDelta = new Vector2(deadzoneRadius * 2, deadzoneRadius * 2);
        dzRt.anchoredPosition = Vector2.zero;
        deadzone = dzObj.AddComponent<Image>();
        deadzone.color = deadzoneColor;

        // 8-way direction markers
        markers = new RectTransform[8];
        for (int i = 0; i < 8; i++)
        {
            StickDirection dir = (StickDirection)(i + 1);
            Vector2 dirVec = dir.ToVector2();

            GameObject markerObj = new GameObject($"Marker_{dir}");
            markerObj.transform.SetParent(panelObj.transform);
            RectTransform markerRt = markerObj.AddComponent<RectTransform>();
            markerRt.anchorMin = new Vector2(0.5f, 0.5f);
            markerRt.anchorMax = new Vector2(0.5f, 0.5f);
            markerRt.sizeDelta = new Vector2(8, 8);
            markerRt.anchoredPosition = dirVec * (indicatorRadius - 10f);
            Image markerImg = markerObj.AddComponent<Image>();
            markerImg.color = markerColor;
            markers[i] = markerRt;
        }

        // Center dot
        GameObject centerObj = new GameObject("CenterDot");
        centerObj.transform.SetParent(panelObj.transform);
        RectTransform centerRt = centerObj.AddComponent<RectTransform>();
        centerRt.anchorMin = new Vector2(0.5f, 0.5f);
        centerRt.anchorMax = new Vector2(0.5f, 0.5f);
        centerRt.sizeDelta = new Vector2(12, 12);
        centerRt.anchoredPosition = Vector2.zero;
        Image centerImg = centerObj.AddComponent<Image>();
        centerImg.color = new Color(0.4f, 0.4f, 0.4f, 1f);

        // Line from center to raw position
        GameObject lineObj = new GameObject("Line");
        lineObj.transform.SetParent(panelObj.transform);
        RectTransform lineRt = lineObj.AddComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(0.5f, 0.5f);
        lineRt.anchorMax = new Vector2(0.5f, 0.5f);
        lineRt.pivot = new Vector2(0, 0.5f);
        lineRt.sizeDelta = new Vector2(0, 2);
        lineRt.anchoredPosition = Vector2.zero;
        line = lineObj.AddComponent<Image>();
        line.color = lineColor;
        lineObj.SetActive(false);

        // Raw position indicator (yellow dot)
        GameObject rawObj = new GameObject("RawIndicator");
        rawObj.transform.SetParent(panelObj.transform);
        rawIndicator = rawObj.AddComponent<RectTransform>();
        rawIndicator.anchorMin = new Vector2(0.5f, 0.5f);
        rawIndicator.anchorMax = new Vector2(0.5f, 0.5f);
        rawIndicator.sizeDelta = new Vector2(16, 16);
        rawIndicator.anchoredPosition = Vector2.zero;
        Image rawImg = rawObj.AddComponent<Image>();
        rawImg.color = rawIndicatorColor;

        // Direction indicator (green ring)
        GameObject dirObj = new GameObject("DirectionIndicator");
        dirObj.transform.SetParent(panelObj.transform);
        dirIndicator = dirObj.AddComponent<RectTransform>();
        dirIndicator.anchorMin = new Vector2(0.5f, 0.5f);
        dirIndicator.anchorMax = new Vector2(0.5f, 0.5f);
        dirIndicator.sizeDelta = new Vector2(28, 28);
        dirIndicator.anchoredPosition = Vector2.zero;
        Image dirImg = dirObj.AddComponent<Image>();
        dirImg.color = directionRingColor;
        dirObj.SetActive(false);

        // Direction text (arrow symbol)
        GameObject dirTextObj = new GameObject("DirectionText");
        dirTextObj.transform.SetParent(panelObj.transform);
        RectTransform dirTextRt = dirTextObj.AddComponent<RectTransform>();
        dirTextRt.anchorMin = new Vector2(0.5f, 0.5f);
        dirTextRt.anchorMax = new Vector2(0.5f, 0.5f);
        dirTextRt.sizeDelta = new Vector2(30, 24);
        dirTextRt.anchoredPosition = Vector2.zero;
        dirText = dirTextObj.AddComponent<TextMeshProUGUI>();
        dirText.fontSize = 18;
        dirText.fontStyle = FontStyles.Bold;
        dirText.alignment = TextAlignmentOptions.Center;
        dirText.color = directionTextColor;
        dirTextObj.SetActive(false);

        // Value text below panel
        GameObject valObj = new GameObject("ValueText");
        valObj.transform.SetParent(panelObj.transform);
        RectTransform valRt = valObj.AddComponent<RectTransform>();
        valRt.anchorMin = new Vector2(0.5f, 0);
        valRt.anchorMax = new Vector2(0.5f, 0);
        valRt.pivot = new Vector2(0.5f, 1);
        valRt.anchoredPosition = new Vector2(0, -30);
        valRt.sizeDelta = new Vector2(100, 20);
        valueText = valObj.AddComponent<TextMeshProUGUI>();
        valueText.fontSize = 10;
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.color = Color.yellow;

        // Label (LS/RS)
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(panelObj.transform);
        RectTransform labelRt = labelObj.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0);
        labelRt.anchorMax = new Vector2(0.5f, 0);
        labelRt.pivot = new Vector2(0.5f, 1);
        labelRt.anchoredPosition = new Vector2(0, -10);
        labelRt.sizeDelta = new Vector2(50, 20);
        TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 14;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = Color.white;
    }
}
