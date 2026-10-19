using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Builds the conveyor settings panel and applies spacing and speed changes immediately.
/// </summary>
[ExecuteAlways]
public sealed class ConveyorSettingsPanel : MonoBehaviour
{
    [Header("Conveyor")]
    [SerializeField] private Transform productsRoot;
    [SerializeField] private GameObject productPrefab;
    [SerializeField] private TMP_FontAsset koreanFont;
    [SerializeField] private Sprite roundedSpriteAsset;
    [SerializeField] private Sprite dotSpriteAsset;
    [SerializeField] private float conveyorLength = 26f;
    [SerializeField] private float productSpacing = 2.6f;
    [SerializeField] private float conveyorSpeed = 0.65f;

    private TMP_InputField spacingInput;
    private TMP_InputField speedInput;
    private Slider spacingSlider;
    private Slider speedSlider;
    private static Sprite roundedSprite;
    private static Sprite dotSprite;

    private const float MinSpacing = 0.6f;
    private const float MaxSpacing = 2.6f;
    private const float MinSpeed = 0f;
    private const float MaxSpeed = 3f;

    private void OnEnable()
    {
        EnsurePanel();
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        if (spacingInput != null)
        {
            spacingInput.onValueChanged.AddListener(OnSpacingChanged);
            spacingInput.onEndEdit.AddListener(OnSpacingEndEdit);
        }
        if (speedInput != null)
        {
            speedInput.onValueChanged.AddListener(OnSpeedChanged);
            speedInput.onEndEdit.AddListener(OnSpeedEndEdit);
        }
        if (spacingSlider != null) spacingSlider.onValueChanged.AddListener(OnSpacingSliderChanged);
        if (speedSlider != null) speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
        ApplySettings();
    }

    private void EnsurePanel()
    {
        Transform existingCanvas = transform.Find("ConveyorSettingsCanvas");
        if (existingCanvas != null)
        {
            TMP_InputField[] inputs = existingCanvas.GetComponentsInChildren<TMP_InputField>(true);
            Slider[] sliders = existingCanvas.GetComponentsInChildren<Slider>(true);
            if (inputs.Length > 0) spacingInput = inputs[0];
            if (inputs.Length > 1) speedInput = inputs[1];
            if (sliders.Length > 0) spacingSlider = sliders[0];
            if (sliders.Length > 1) speedSlider = sliders[1];
            EnsureSprites();
            return;
        }

        BuildPanel();
#if UNITY_EDITOR
        if (!Application.isPlaying && gameObject.scene.IsValid())
        {
            EditorUtility.SetDirty(gameObject);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private void OnDestroy()
    {
        if (spacingInput != null)
        {
            spacingInput.onValueChanged.RemoveListener(OnSpacingChanged);
            spacingInput.onEndEdit.RemoveListener(OnSpacingEndEdit);
        }
        if (speedInput != null)
        {
            speedInput.onValueChanged.RemoveListener(OnSpeedChanged);
            speedInput.onEndEdit.RemoveListener(OnSpeedEndEdit);
        }
        if (spacingSlider != null) spacingSlider.onValueChanged.RemoveListener(OnSpacingSliderChanged);
        if (speedSlider != null) speedSlider.onValueChanged.RemoveListener(OnSpeedSliderChanged);
    }

    private void OnSpacingChanged(string value)
    {
        if (TryRead(value, out float parsed))
        {
            productSpacing = Mathf.Clamp(parsed, MinSpacing, MaxSpacing);
            if (spacingSlider != null) spacingSlider.SetValueWithoutNotify(productSpacing);
            ApplySettings();
        }
    }

    private void OnSpeedChanged(string value)
    {
        if (TryRead(value, out float parsed))
        {
            conveyorSpeed = Mathf.Clamp(parsed, MinSpeed, MaxSpeed);
            if (speedSlider != null) speedSlider.SetValueWithoutNotify(conveyorSpeed);
            ApplySettings();
        }
    }

    private void OnSpacingEndEdit(string value)
    {
        if (TryRead(value, out float parsed)) productSpacing = Mathf.Clamp(parsed, MinSpacing, MaxSpacing);
        if (spacingInput != null) spacingInput.SetTextWithoutNotify(productSpacing.ToString("0.00", CultureInfo.InvariantCulture));
        if (spacingSlider != null) spacingSlider.SetValueWithoutNotify(productSpacing);
        ApplySettings();
    }

    private void OnSpeedEndEdit(string value)
    {
        if (TryRead(value, out float parsed)) conveyorSpeed = Mathf.Clamp(parsed, MinSpeed, MaxSpeed);
        if (speedInput != null) speedInput.SetTextWithoutNotify(conveyorSpeed.ToString("0.00", CultureInfo.InvariantCulture));
        if (speedSlider != null) speedSlider.SetValueWithoutNotify(conveyorSpeed);
        ApplySettings();
    }

    private void OnSpacingSliderChanged(float value)
    {
        productSpacing = value;
        if (spacingInput != null) spacingInput.SetTextWithoutNotify(value.ToString("0.00", CultureInfo.InvariantCulture));
        ApplySettings();
    }

    private void OnSpeedSliderChanged(float value)
    {
        conveyorSpeed = value;
        if (speedInput != null) speedInput.SetTextWithoutNotify(value.ToString("0.00", CultureInfo.InvariantCulture));
        ApplySettings();
    }

    private static bool TryRead(string value, out float parsed)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
            && !float.IsNaN(parsed) && !float.IsInfinity(parsed);
    }

    private void ApplySettings()
    {
        if (productsRoot == null || productPrefab == null) return;

        int desiredCount = Mathf.Max(1, Mathf.FloorToInt(conveyorLength / productSpacing));
        while (productsRoot.childCount < desiredCount)
        {
            GameObject product = Instantiate(productPrefab, productsRoot);
            product.name = $"ProductBox_{productsRoot.childCount:00}";
        }

        float occupiedLength = desiredCount * productSpacing;
        float startZ = -occupiedLength * 0.5f;
        float endZ = occupiedLength * 0.5f;
        for (int i = 0; i < productsRoot.childCount; i++)
        {
            Transform product = productsRoot.GetChild(i);
            bool active = i < desiredCount;
            if (product.gameObject.activeSelf != active) product.gameObject.SetActive(active);
            if (!active) continue;

            product.position = new Vector3(0f, 0.85f, startZ + productSpacing * (i + 0.5f));
            ConveyorProductMover mover = product.GetComponent<ConveyorProductMover>();
            if (mover != null) mover.ConfigureMotion(conveyorSpeed, startZ, endZ);
        }

    }

    private void BuildPanel()
    {
        GameObject canvasObject = new GameObject("ConveyorSettingsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        TMP_FontAsset font = koreanFont != null ? koreanFont : TMP_Settings.defaultFontAsset;
        EnsureSprites();

        Color ink = new Color32(237, 243, 243, 255);
        Color secondary = new Color32(146, 163, 166, 255);
        Color accent = new Color32(190, 239, 112, 255);
        RectTransform panel = CreateRect("SettingsPanel", canvasObject.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 0f), new Vector2(318f, 390f));
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.sprite = roundedSprite;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color32(17, 27, 32, 248);
        Shadow shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.34f);
        shadow.effectDistance = new Vector2(0f, -7f);
        shadow.useGraphicAlpha = true;

        CreateText("PanelEyebrow", panel, "SMART FACTORY  /  LINE 01", font, 11, FontStyles.Bold, new Vector2(22f, -17f), new Vector2(205f, 20f), TextAlignmentOptions.MidlineLeft, accent);
        CreateLiveBadge(panel, font, accent);
        CreateText("PanelTitle", panel, "컨베이어 제어", font, 22, FontStyles.Bold, new Vector2(22f, -44f), new Vector2(274f, 32f), TextAlignmentOptions.MidlineLeft, ink);
        CreateText("PanelSubtitle", panel, "상품 흐름과 속도를 실시간으로 조정합니다", font, 12, FontStyles.Normal, new Vector2(22f, -76f), new Vector2(274f, 22f), TextAlignmentOptions.MidlineLeft, secondary);
        CreateRule("HeaderDivider", panel, new Vector2(22f, -111f), new Vector2(274f, 1f), new Color32(46, 64, 69, 255));

        CreateText("SpacingLabel", panel, "상품 드롭 간격", font, 15, FontStyles.Bold, new Vector2(22f, -130f), new Vector2(210f, 24f), TextAlignmentOptions.MidlineLeft, ink);
        CreateText("SpacingCaption", panel, "상자 중심 사이 거리", font, 11, FontStyles.Normal, new Vector2(22f, -154f), new Vector2(270f, 18f), TextAlignmentOptions.MidlineLeft, secondary);
        spacingSlider = CreateSlider("SpacingSlider", panel, new Vector2(22f, -176f), new Vector2(166f, 28f), MinSpacing, MaxSpacing, productSpacing);
        spacingInput = CreateInput("SpacingInput", panel, font, productSpacing.ToString("0.00", CultureInfo.InvariantCulture), "m", new Vector2(202f, -169f), new Vector2(94f, 42f));
        CreateText("SpacingRange", panel, "0.60 m", font, 10, FontStyles.Normal, new Vector2(22f, -207f), new Vector2(74f, 17f), TextAlignmentOptions.MidlineLeft, secondary);
        CreateText("SpacingRangeMax", panel, "2.60 m", font, 10, FontStyles.Normal, new Vector2(114f, -207f), new Vector2(74f, 17f), TextAlignmentOptions.MidlineRight, secondary);

        CreateRule("SectionDivider", panel, new Vector2(22f, -238f), new Vector2(274f, 1f), new Color32(46, 64, 69, 255));
        CreateText("SpeedLabel", panel, "컨베이어 이동 속도", font, 15, FontStyles.Bold, new Vector2(22f, -255f), new Vector2(250f, 24f), TextAlignmentOptions.MidlineLeft, ink);
        CreateText("SpeedCaption", panel, "현재 라인에 즉시 반영", font, 11, FontStyles.Normal, new Vector2(22f, -279f), new Vector2(270f, 18f), TextAlignmentOptions.MidlineLeft, secondary);
        speedSlider = CreateSlider("SpeedSlider", panel, new Vector2(22f, -301f), new Vector2(166f, 28f), MinSpeed, MaxSpeed, conveyorSpeed);
        speedInput = CreateInput("SpeedInput", panel, font, conveyorSpeed.ToString("0.00", CultureInfo.InvariantCulture), "m/s", new Vector2(202f, -294f), new Vector2(94f, 42f));
        CreateText("SpeedRange", panel, "0.00 m/s", font, 10, FontStyles.Normal, new Vector2(22f, -332f), new Vector2(74f, 17f), TextAlignmentOptions.MidlineLeft, secondary);
        CreateText("SpeedRangeMax", panel, "3.00 m/s", font, 10, FontStyles.Normal, new Vector2(114f, -332f), new Vector2(74f, 17f), TextAlignmentOptions.MidlineRight, secondary);

        CreateText("PanelFooter", panel, "슬라이더 또는 숫자 입력으로 조정", font, 10, FontStyles.Normal, new Vector2(22f, -365f), new Vector2(274f, 18f), TextAlignmentOptions.MidlineLeft, secondary);
    }

    private void EnsureSprites()
    {
        if (roundedSpriteAsset != null) roundedSprite = roundedSpriteAsset;
        if (dotSpriteAsset != null) dotSprite = dotSpriteAsset;
        if (roundedSprite == null) roundedSprite = CreateRoundedSprite("RuntimeRoundedSprite", 64, 18f, true);
        if (dotSprite == null) dotSprite = CreateRoundedSprite("RuntimeDotSprite", 64, 32f, false);
    }

    private static Sprite CreateRoundedSprite(string name, int size, float radius, bool sliced)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = name + "Texture";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[size * size];
        float center = size * 0.5f;
        float innerExtent = center - radius;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - center) - innerExtent, 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - center) - innerExtent, 0f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + 0.5f - distance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        Vector4 border = sliced ? new Vector4(18f, 18f, 18f, 18f) : Vector4.zero;
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, border);
        sprite.name = name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static void CreateLiveBadge(Transform parent, TMP_FontAsset font, Color accent)
    {
        RectTransform badge = CreateRect("LiveBadge", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(231f, -16f), new Vector2(65f, 23f));
        Image background = badge.gameObject.AddComponent<Image>();
        background.sprite = roundedSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color32(38, 57, 47, 255);
        RectTransform dot = CreateRect("LiveDot", badge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(9f, 0f), new Vector2(7f, 7f));
        Image dotImage = dot.gameObject.AddComponent<Image>();
        dotImage.sprite = dotSprite;
        dotImage.color = accent;
        CreateText("LiveText", badge, "LIVE", font, 10, FontStyles.Bold, new Vector2(22f, 0f), new Vector2(37f, 23f), TextAlignmentOptions.MidlineLeft, accent);
    }

    private static void CreateRule(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, TMP_FontAsset font, int size, FontStyles style, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, dimensions);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.text = value;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static TMP_InputField CreateInput(string name, Transform parent, TMP_FontAsset font, string initialValue, string unit, Vector2 position, Vector2 dimensions)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, dimensions);
        Image frame = rect.gameObject.AddComponent<Image>();
        frame.sprite = roundedSprite;
        frame.type = Image.Type.Sliced;
        frame.color = new Color32(55, 76, 81, 255);
        frame.raycastTarget = true;

        TMP_DefaultControls.Resources resources = new TMP_DefaultControls.Resources { inputField = roundedSprite };
        GameObject fieldObject = TMP_DefaultControls.CreateInputField(resources);
        fieldObject.name = "NumericInputField";
        fieldObject.transform.SetParent(rect, false);
        RectTransform field = fieldObject.GetComponent<RectTransform>();
        field.anchorMin = Vector2.zero;
        field.anchorMax = Vector2.one;
        field.pivot = new Vector2(0.5f, 0.5f);
        field.anchoredPosition = Vector2.zero;
        field.sizeDelta = new Vector2(-2f, -2f);

        Image background = fieldObject.GetComponent<Image>();
        background.sprite = roundedSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color32(25, 39, 45, 255);
        background.raycastTarget = true;

        TMP_InputField input = fieldObject.GetComponent<TMP_InputField>();
        TMP_Text text = input.textComponent;
        TMP_Text placeholder = input.placeholder as TMP_Text;
        text.font = font;
        text.fontSize = 15f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color32(242, 247, 244, 255);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.extraPadding = true;
        placeholder.font = font;
        placeholder.fontSize = 12f;
        placeholder.fontStyle = FontStyles.Normal;
        placeholder.text = "숫자 입력";
        placeholder.color = new Color32(115, 133, 136, 255);
        placeholder.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform textArea = input.textViewport;
        textArea.offsetMin = new Vector2(9f, 5f);
        textArea.offsetMax = new Vector2(-31f, -5f);
        CreateText("Unit", field, unit, font, 10, FontStyles.Bold, new Vector2(dimensions.x - 43f, 0f), new Vector2(36f, dimensions.y - 4f), TextAlignmentOptions.Center, new Color32(154, 177, 169, 255));

        input.textComponent = text;
        input.placeholder = placeholder;
        input.contentType = TMP_InputField.ContentType.DecimalNumber;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        input.targetGraphic = background;
        input.fontAsset = font;
        input.text = initialValue;
        input.readOnly = false;
        input.shouldHideMobileInput = false;
        input.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
        input.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = input.colors;
        colors.normalColor = new Color32(25, 39, 45, 255);
        colors.highlightedColor = new Color32(31, 49, 54, 255);
        colors.selectedColor = new Color32(31, 49, 54, 255);
        colors.pressedColor = new Color32(38, 57, 61, 255);
        colors.colorMultiplier = 1f;
        input.colors = colors;
        return input;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 position, Vector2 dimensions, float minimum, float maximum, float initialValue)
    {
        RectTransform root = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, dimensions);

        RectTransform track = CreateRect("Track", root, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 6f));
        Image trackImage = track.gameObject.AddComponent<Image>();
        trackImage.sprite = roundedSprite;
        trackImage.type = Image.Type.Sliced;
        trackImage.color = new Color32(49, 66, 71, 255);

        RectTransform fill = CreateRect("Fill", track, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.sprite = roundedSprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = new Color32(190, 239, 112, 255);
        fillImage.raycastTarget = false;

        RectTransform handle = CreateRect("Handle", root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f));
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.sprite = dotSprite;
        handleImage.color = new Color32(223, 248, 185, 255);

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = minimum;
        slider.maxValue = maximum;
        slider.wholeNumbers = false;
        slider.value = Mathf.Clamp(initialValue, minimum, maximum);
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        return slider;
    }
}
