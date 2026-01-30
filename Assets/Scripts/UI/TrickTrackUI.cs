using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Canvas-based trick timing track UI.
/// Tricks scroll down and must be hit at the right time.
/// </summary>
public class TrickTrackUI : MonoBehaviour
{
    [Header("References")]
    public TrickInputSystem trickInputSystem;
    public Canvas canvas;
    public TrickIconHelper iconHelper;

    [Header("Track Panel")]
    public RectTransform trackPanel;
    public Image trackBackground;
    public RectTransform hitLine;
    public RectTransform perfectZone;
    public RectTransform greatZone;
    public RectTransform okZone;

    [Header("Score UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;

    [Header("Result UI")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI resultTrickName;

    [Header("Stats Panel")]
    public RectTransform statsPanel;
    public TextMeshProUGUI statsText;

    [Header("Trick Card Prefab")]
    public GameObject trickCardPrefab;

    [Header("Timing Settings")]
    [Range(1f, 10f)]
    public float trickInterval = 3f;
    [Range(1f, 5f)]
    public float scrollDuration = 2f;
    [Range(0.05f, 0.3f)]
    public float perfectWindow = 0.1f;
    [Range(0.1f, 0.5f)]
    public float greatWindow = 0.2f;
    [Range(0.2f, 0.8f)]
    public float okWindow = 0.35f;

    [Header("Track Settings")]
    public float trackWidth = 200f;
    [Range(0.1f, 0.5f)]
    public float hitLinePosition = 0.2f;

    // Runtime state
    private List<ScrollingTrickCard> activeCards = new List<ScrollingTrickCard>();
    private float nextSpawnTime;
    private int score = 0;
    private int combo = 0;
    private int maxCombo = 0;
    private float lastResultTime;

    // Stats
    private int perfectCount, greatCount, okCount, missCount, earlyCount;

    private class ScrollingTrickCard
    {
        public TrickDefinition trick;
        public GameObject cardObject;
        public RectTransform rectTransform;
        public float spawnTime;
        public float targetTime;
        public bool completed;
        public bool missed;
    }

    private void Start()
    {
        if (trickInputSystem == null)
            trickInputSystem = FindObjectOfType<TrickInputSystem>();

        if (trickInputSystem != null)
            trickInputSystem.OnTrickMatched += OnTrickPerformed;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (iconHelper == null)
            iconHelper = TrickIconHelper.Instance;

        nextSpawnTime = Time.time + 1f;

        // Create prefab if not assigned
        if (trickCardPrefab == null)
            CreateTrickCardPrefab();
    }

    private void OnDestroy()
    {
        if (trickInputSystem != null)
            trickInputSystem.OnTrickMatched -= OnTrickPerformed;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        // Spawn new tricks
        if (Time.time >= nextSpawnTime)
        {
            SpawnTrick();
            nextSpawnTime = Time.time + trickInterval;
        }

        // Update card positions
        UpdateCards();

        // Check for misses
        CheckMissedTricks();

        // Update UI
        UpdateScoreUI();
        UpdateResultUI();
        UpdateStatsUI();
    }

    private void SpawnTrick()
    {
        if (trickInputSystem == null || trickInputSystem.trickDatabase.Count == 0)
            return;

        int index = Random.Range(0, trickInputSystem.trickDatabase.Count);
        TrickDefinition trick = trickInputSystem.trickDatabase[index];

        GameObject cardObj = Instantiate(trickCardPrefab, trackPanel);
        cardObj.SetActive(true); // Prefab is inactive, activate the instance
        RectTransform rt = cardObj.GetComponent<RectTransform>();

        // Position at top of track
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(0, 30);

        // Set border color based on first stick used
        Outline outline = cardObj.GetComponent<Outline>();
        if (outline != null && trick.inputSequence != null && trick.inputSequence.Count > 0)
        {
            StickType firstStick = trick.inputSequence[0].stickType;
            if (firstStick == StickType.LeftStick)
            {
                // Blue for nollie tricks (LS first)
                outline.effectColor = new Color(0.5f, 0.8f, 1f);
            }
            else
            {
                // Orange/beige for ollie tricks (RS first)
                outline.effectColor = new Color(1f, 0.8f, 0.5f);
            }
        }

        // Set trick name
        TextMeshProUGUI[] texts = cardObj.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length > 0) texts[0].text = trick.trickName;

        // Create input icons instead of text
        if (iconHelper != null && trick.inputSequence != null && trick.inputSequence.Count > 0)
        {
            // Find or create icons container
            Transform iconsContainer = cardObj.transform.Find("IconsContainer");
            if (iconsContainer == null)
            {
                GameObject iconsObj = new GameObject("IconsContainer");
                iconsObj.transform.SetParent(cardObj.transform);
                RectTransform iconsRt = iconsObj.AddComponent<RectTransform>();
                iconsRt.anchorMin = new Vector2(0, 0);
                iconsRt.anchorMax = new Vector2(1, 0.5f);
                iconsRt.offsetMin = new Vector2(5, 5);
                iconsRt.offsetMax = new Vector2(-5, 0);
                iconsContainer = iconsObj.transform;

                // Add horizontal layout
                UnityEngine.UI.HorizontalLayoutGroup layout = iconsObj.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                layout.spacing = 4f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
            }

            // Clear existing icons
            foreach (Transform child in iconsContainer)
            {
                Destroy(child.gameObject);
            }

            // Create icons for each step
            CreateInputIcons(iconsContainer, trick);

            // Hide the text input sequence if it exists
            if (texts.Length > 1) texts[1].gameObject.SetActive(false);
        }
        else if (texts.Length > 1)
        {
            // Fallback to text
            texts[1].text = trick.GetInputSequenceString();
        }

        var card = new ScrollingTrickCard
        {
            trick = trick,
            cardObject = cardObj,
            rectTransform = rt,
            spawnTime = Time.time,
            targetTime = Time.time + scrollDuration,
            completed = false,
            missed = false
        };

        activeCards.Add(card);
    }

    private void CreateInputIcons(Transform container, TrickDefinition trick)
    {
        float iconSize = 20f;

        for (int i = 0; i < trick.inputSequence.Count; i++)
        {
            InputStep step = trick.inputSequence[i];

            // Create step container
            GameObject stepObj = new GameObject($"Step_{i}");
            stepObj.transform.SetParent(container);
            RectTransform stepRt = stepObj.AddComponent<RectTransform>();
            stepRt.sizeDelta = new Vector2(iconSize + 4, iconSize + 12);

            // Create stick label (LS/RS)
            GameObject labelObj = new GameObject("StickLabel");
            labelObj.transform.SetParent(stepObj.transform);
            RectTransform labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.5f, 1);
            labelRt.anchorMax = new Vector2(0.5f, 1);
            labelRt.anchoredPosition = new Vector2(0, -5);
            labelRt.sizeDelta = new Vector2(iconSize, 10);

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = step.stickType == StickType.LeftStick ? "LS" : "RS";
            labelTmp.fontSize = 8;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = step.stickType == StickType.LeftStick ?
                new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.8f, 0.5f);

            // Create icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(stepObj.transform);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(0, -2);
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
                arrowObj.transform.SetParent(container);
                RectTransform arrowRt = arrowObj.AddComponent<RectTransform>();
                arrowRt.sizeDelta = new Vector2(10, iconSize);

                TextMeshProUGUI arrowTmp = arrowObj.AddComponent<TextMeshProUGUI>();
                arrowTmp.text = ">";
                arrowTmp.fontSize = 12;
                arrowTmp.alignment = TextAlignmentOptions.Center;
                arrowTmp.color = new Color(0.6f, 0.6f, 0.6f);
            }
        }
    }

    private void UpdateCards()
    {
        float trackHeight = trackPanel.rect.height;
        float hitY = trackHeight * hitLinePosition;

        foreach (var card in activeCards)
        {
            if (card.cardObject == null) continue;

            float progress = (Time.time - card.spawnTime) / scrollDuration;
            float startY = trackHeight + 30;
            float endY = hitY;
            float currentY = Mathf.Lerp(startY, endY, progress);

            card.rectTransform.anchoredPosition = new Vector2(0, -trackHeight + currentY);

            // Update visual state
            Image bg = card.cardObject.GetComponent<Image>();
            if (bg != null)
            {
                if (card.completed)
                    bg.color = new Color(0, 1, 0, 0.5f);
                else if (card.missed)
                    bg.color = new Color(1, 0, 0, 0.5f);
                else
                    bg.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);
            }
        }

        // Cleanup old cards
        activeCards.RemoveAll(c =>
        {
            if ((c.completed || c.missed) && Time.time > c.targetTime + 1f)
            {
                if (c.cardObject != null) Destroy(c.cardObject);
                return true;
            }
            return false;
        });
    }

    private void OnTrickPerformed(TrickMatchResult result)
    {
        foreach (var card in activeCards)
        {
            if (card.completed || card.missed) continue;
            if (card.trick != result.trick) continue;

            float timeDiff = Mathf.Abs(Time.time - card.targetTime);

            if (Time.time < card.targetTime - okWindow)
            {
                SetResult("EARLY!", Color.red, card.trick.trickName);
                earlyCount++;
                combo = 0;
            }
            else if (timeDiff <= perfectWindow)
            {
                SetResult("PERFECT!", Color.cyan, card.trick.trickName);
                score += 100 * (combo + 1);
                combo++;
                perfectCount++;
            }
            else if (timeDiff <= greatWindow)
            {
                SetResult("GREAT!", Color.green, card.trick.trickName);
                score += 75 * (combo + 1);
                combo++;
                greatCount++;
            }
            else if (timeDiff <= okWindow)
            {
                SetResult("OK", Color.yellow, card.trick.trickName);
                score += 50 * (combo + 1);
                combo++;
                okCount++;
            }
            else
            {
                SetResult("LATE", Color.yellow, card.trick.trickName);
                score += 25;
                combo = 0;
            }

            if (combo > maxCombo) maxCombo = combo;
            card.completed = true;
            return;
        }
    }

    private void CheckMissedTricks()
    {
        foreach (var card in activeCards)
        {
            if (card.completed || card.missed) continue;

            if (Time.time > card.targetTime + okWindow)
            {
                card.missed = true;
                SetResult("MISS", Color.red, card.trick.trickName);
                missCount++;
                combo = 0;
            }
        }
    }

    private void SetResult(string text, Color color, string trickName)
    {
        if (resultText != null)
        {
            resultText.text = text;
            resultText.color = color;
        }
        if (resultTrickName != null)
        {
            resultTrickName.text = trickName;
        }
        lastResultTime = Time.time;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
        if (comboText != null)
            comboText.text = $"Combo: {combo}x (Max: {maxCombo})";
    }

    private void UpdateResultUI()
    {
        if (resultText != null)
        {
            float alpha = Mathf.Clamp01(1f - (Time.time - lastResultTime) / 1f);
            Color c = resultText.color;
            c.a = alpha;
            resultText.color = c;
        }
        if (resultTrickName != null)
        {
            float alpha = Mathf.Clamp01(1f - (Time.time - lastResultTime) / 1f);
            Color c = resultTrickName.color;
            c.a = alpha;
            resultTrickName.color = c;
        }
    }

    private void UpdateStatsUI()
    {
        if (statsText != null)
        {
            statsText.text = $"Perfect: {perfectCount}\nGreat: {greatCount}\nOK: {okCount}\nMiss: {missCount}\nEarly: {earlyCount}";
        }
    }

    [ContextMenu("Setup UI")]
    public void SetupUI()
    {
        // Find or create canvas
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("TrickTrackCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
                transform.SetParent(canvasObj.transform);
            }
        }

        // Create track panel
        if (trackPanel == null)
        {
            GameObject trackObj = new GameObject("TrackPanel");
            trackObj.transform.SetParent(canvas.transform);
            trackPanel = trackObj.AddComponent<RectTransform>();
            trackPanel.anchorMin = new Vector2(1, 0);
            trackPanel.anchorMax = new Vector2(1, 1);
            trackPanel.pivot = new Vector2(1, 0.5f);
            trackPanel.anchoredPosition = new Vector2(-20, 0);
            trackPanel.sizeDelta = new Vector2(trackWidth, 0);

            trackBackground = trackObj.AddComponent<Image>();
            trackBackground.color = new Color(0, 0, 0, 0.6f);

            // Mask for clipping
            trackObj.AddComponent<RectMask2D>();
        }

        // Create zones
        CreateZone(ref okZone, "OKZone", new Color(1, 1, 0, 0.2f), 140);
        CreateZone(ref greatZone, "GreatZone", new Color(0, 1, 0, 0.3f), 80);
        CreateZone(ref perfectZone, "PerfectZone", new Color(0, 1, 1, 0.4f), 40);

        // Create hit line
        if (hitLine == null)
        {
            GameObject lineObj = new GameObject("HitLine");
            lineObj.transform.SetParent(trackPanel);
            hitLine = lineObj.AddComponent<RectTransform>();
            hitLine.anchorMin = new Vector2(0, hitLinePosition);
            hitLine.anchorMax = new Vector2(1, hitLinePosition);
            hitLine.sizeDelta = new Vector2(0, 4);
            hitLine.anchoredPosition = Vector2.zero;

            Image lineImg = lineObj.AddComponent<Image>();
            lineImg.color = Color.white;
        }

        // Create score texts
        CreateScoreUI();

        // Create result texts
        CreateResultUI();

        // Create stats panel
        CreateStatsUI();

        // Create prefab
        CreateTrickCardPrefab();
    }

    private void CreateZone(ref RectTransform zone, string name, Color color, float height)
    {
        if (zone != null) return;

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(trackPanel);
        zone = obj.AddComponent<RectTransform>();
        zone.anchorMin = new Vector2(0, hitLinePosition);
        zone.anchorMax = new Vector2(1, hitLinePosition);
        zone.sizeDelta = new Vector2(0, height);
        zone.anchoredPosition = Vector2.zero;

        Image img = obj.AddComponent<Image>();
        img.color = color;

        // Send to back
        zone.SetAsFirstSibling();
    }

    private void CreateScoreUI()
    {
        if (scoreText == null)
        {
            scoreText = CreateTextElement("ScoreText", canvas.transform, "Score: 0", 24,
                new Vector2(1, 1), new Vector2(-120, -30));
            scoreText.alignment = TextAlignmentOptions.Right;
        }
        if (comboText == null)
        {
            comboText = CreateTextElement("ComboText", canvas.transform, "Combo: 0x", 18,
                new Vector2(1, 1), new Vector2(-120, -60));
            comboText.alignment = TextAlignmentOptions.Right;
        }
    }

    private void CreateResultUI()
    {
        if (resultText == null)
        {
            resultText = CreateTextElement("ResultText", canvas.transform, "", 48,
                new Vector2(0.5f, 0.5f), new Vector2(0, 50));
            resultText.alignment = TextAlignmentOptions.Center;
            resultText.fontStyle = FontStyles.Bold;
        }
        if (resultTrickName == null)
        {
            resultTrickName = CreateTextElement("ResultTrickName", canvas.transform, "", 24,
                new Vector2(0.5f, 0.5f), new Vector2(0, 0));
            resultTrickName.alignment = TextAlignmentOptions.Center;
        }
    }

    private void CreateStatsUI()
    {
        if (statsPanel == null)
        {
            GameObject obj = new GameObject("StatsPanel");
            obj.transform.SetParent(canvas.transform);
            statsPanel = obj.AddComponent<RectTransform>();
            statsPanel.anchorMin = new Vector2(0, 0);
            statsPanel.anchorMax = new Vector2(0, 0);
            statsPanel.pivot = new Vector2(0, 0);
            statsPanel.anchoredPosition = new Vector2(10, 10);
            statsPanel.sizeDelta = new Vector2(120, 110);

            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.6f);

            statsText = CreateTextElement("StatsText", statsPanel, "Stats", 12,
                new Vector2(0, 1), new Vector2(10, -10));
            statsText.alignment = TextAlignmentOptions.TopLeft;
            statsText.rectTransform.sizeDelta = new Vector2(100, 100);
        }
    }

    private TextMeshProUGUI CreateTextElement(string name, Transform parent, string text, int fontSize,
        Vector2 anchor, Vector2 position)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(200, 50);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;

        return tmp;
    }

    private void CreateTrickCardPrefab()
    {
        if (trickCardPrefab != null) return;

        GameObject card = new GameObject("TrickCard");
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(-20, 60);

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

        // Add outline for stick-type border (color set at spawn time)
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(3, 3);

        // Trick name
        GameObject nameObj = new GameObject("TrickName");
        nameObj.transform.SetParent(card.transform);
        RectTransform nameRt = nameObj.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0.5f);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.offsetMin = new Vector2(5, 0);
        nameRt.offsetMax = new Vector2(-5, -5);

        TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "Trick Name";
        nameTmp.fontSize = 14;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.color = Color.white;

        // Input sequence
        GameObject inputObj = new GameObject("InputSequence");
        inputObj.transform.SetParent(card.transform);
        RectTransform inputRt = inputObj.AddComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0, 0);
        inputRt.anchorMax = new Vector2(1, 0.5f);
        inputRt.offsetMin = new Vector2(5, 5);
        inputRt.offsetMax = new Vector2(-5, 0);

        TextMeshProUGUI inputTmp = inputObj.AddComponent<TextMeshProUGUI>();
        inputTmp.text = "RS ↓ > LS ↗";
        inputTmp.fontSize = 11;
        inputTmp.alignment = TextAlignmentOptions.Center;
        inputTmp.color = new Color(0.8f, 0.8f, 0.8f);

        trickCardPrefab = card;
        card.SetActive(false);
    }

    public void ResetScore()
    {
        score = 0;
        combo = 0;
        maxCombo = 0;
        perfectCount = greatCount = okCount = missCount = earlyCount = 0;

        foreach (var card in activeCards)
        {
            if (card.cardObject != null)
                Destroy(card.cardObject);
        }
        activeCards.Clear();
    }
}
