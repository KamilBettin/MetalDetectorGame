using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsMenuUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform root;
    private Text titleText;
    private Text languageLabelText;
    private Text genderLabelText;
    private Text currentLanguageText;
    private Text maleGenderText;
    private Text femaleGenderText;
    private Text maleGenderSelectedText;
    private Text femaleGenderSelectedText;
    private Text closeButtonText;
    private Image maleGenderCardImage;
    private Image femaleGenderCardImage;
    private Image maleGenderAccentImage;
    private Image femaleGenderAccentImage;
    private readonly List<LanguageRow> languageRows = new List<LanguageRow>();

    private static readonly Color PanelColor = new Color(0.12f, 0.085f, 0.055f, 0.98f);
    private static readonly Color PanelTrimColor = new Color(0.78f, 0.55f, 0.25f, 0.94f);
    private static readonly Color CardColor = new Color(0.20f, 0.15f, 0.10f, 0.96f);
    private static readonly Color SelectedCardColor = new Color(0.34f, 0.25f, 0.13f, 0.98f);
    private static readonly Color TextColor = new Color(0.96f, 0.82f, 0.56f, 1f);
    private static readonly Color MutedTextColor = new Color(0.78f, 0.66f, 0.47f, 1f);
    private static readonly Color DarkTextColor = new Color(0.10f, 0.06f, 0.025f, 1f);
    private static readonly Color AccentColor = new Color(1f, 0.76f, 0.28f, 1f);
    private static readonly Color GreenAccentColor = new Color(0.35f, 0.92f, 0.62f, 1f);

    public static SettingsMenuUI Instance { get; private set; }
    public bool IsOpen => root != null && root.gameObject.activeSelf;

    private sealed class LanguageRow
    {
        public GameLanguage language;
        public string labelKey;
        public Image cardImage;
        public Image accentImage;
        public Text labelText;
        public Text selectedText;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildCanvas();
        GameLocalization.LanguageChanged += RefreshTexts;
        SetOpen(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        GameLocalization.LanguageChanged -= RefreshTexts;
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetOpen(false);
        }
    }

    public static void Open()
    {
        SettingsMenuUI settingsUi = Instance;

        if (settingsUi == null)
        {
            settingsUi = new GameObject("Settings Menu UI").AddComponent<SettingsMenuUI>();
        }

        settingsUi.SetOpen(true);
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Settings Menu Canvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1450;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        root = CreateUiObject("Settings Root", canvas.transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Image shade = root.gameObject.AddComponent<Image>();
        shade.color = new Color(0f, 0f, 0f, 0.66f);

        languageRows.Clear();

        RectTransform trim = CreatePanel("Settings Trim", root, Vector2.zero, new Vector2(792f, 846f), new Vector2(0.5f, 0.5f), PanelTrimColor);
        RectTransform panel = CreatePanel("Settings Panel", trim, Vector2.zero, new Vector2(774f, 828f), new Vector2(0.5f, 0.5f), PanelColor);
        CreateRect("Top Accent", panel, new Vector2(0f, -8f), new Vector2(710f, 4f), AccentColor, new Vector2(0.5f, 1f));

        titleText = CreateText("", panel, new Vector2(0f, -40f), new Vector2(650f, 42f), 32, FontStyle.Bold, TextAnchor.MiddleCenter);
        currentLanguageText = CreateText("", panel, new Vector2(0f, -74f), new Vector2(560f, 26f), 17, FontStyle.Bold, TextAnchor.MiddleCenter);
        currentLanguageText.color = MutedTextColor;

        languageLabelText = CreateText("", panel, new Vector2(-236f, -110f), new Vector2(260f, 28f), 21, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateRect("Language Rule", panel, new Vector2(110f, -110f), new Vector2(400f, 2f), new Color(0.78f, 0.55f, 0.25f, 0.45f), new Vector2(0.5f, 1f));

        CreateLanguageButton(panel, GameLanguage.English, "EN", "language.english", new Vector2(0f, -156f));
        CreateLanguageButton(panel, GameLanguage.Polish, "PL", "language.polish", new Vector2(0f, -214f));
        CreateLanguageButton(panel, GameLanguage.Norwegian, "NO", "language.norwegian", new Vector2(0f, -272f));
        CreateLanguageButton(panel, GameLanguage.German, "DE", "language.german", new Vector2(0f, -330f));
        CreateLanguageButton(panel, GameLanguage.Spanish, "ES", "language.spanish", new Vector2(0f, -388f));
        CreateLanguageButton(panel, GameLanguage.Swedish, "SE", "language.swedish", new Vector2(0f, -446f));
        CreateLanguageButton(panel, GameLanguage.Danish, "DK", "language.danish", new Vector2(0f, -504f));

        genderLabelText = CreateText("", panel, new Vector2(-236f, -572f), new Vector2(260f, 28f), 21, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateRect("Gender Rule", panel, new Vector2(110f, -572f), new Vector2(400f, 2f), new Color(0.78f, 0.55f, 0.25f, 0.45f), new Vector2(0.5f, 1f));

        CreateGenderButton(panel, PlayerCharacterSelection.CharacterGender.Male, new Vector2(0f, -620f), out maleGenderCardImage, out maleGenderAccentImage, out maleGenderText, out maleGenderSelectedText);
        CreateGenderButton(panel, PlayerCharacterSelection.CharacterGender.Female, new Vector2(0f, -678f), out femaleGenderCardImage, out femaleGenderAccentImage, out femaleGenderText, out femaleGenderSelectedText);

        Button closeButton = CreateActionButton("Close Button", panel, new Vector2(0f, -764f), new Vector2(240f, 50f), () => SetOpen(false));
        closeButtonText = closeButton.GetComponentInChildren<Text>();

        RefreshTexts();
    }

    private void SetOpen(bool open)
    {
        if (root == null)
        {
            return;
        }

        root.gameObject.SetActive(open);

        if (open)
        {
            RefreshTexts();
        }
    }

    private void RefreshTexts()
    {
        if (titleText != null)
        {
            titleText.text = GameLocalization.T("settings.title");
        }

        if (languageLabelText != null)
        {
            languageLabelText.text = GameLocalization.T("settings.language");
        }

        if (currentLanguageText != null)
        {
            currentLanguageText.text = GameLocalization.T("settings.current") + ": " + GameLocalization.GetLanguageName(GameLocalization.CurrentLanguage);
        }

        foreach (LanguageRow row in languageRows)
        {
            RefreshLanguageButton(row);
        }

        if (genderLabelText != null)
        {
            genderLabelText.text = GameLocalization.T("settings.gender");
        }

        RefreshGenderButton(PlayerCharacterSelection.CharacterGender.Male, maleGenderCardImage, maleGenderAccentImage, maleGenderText, maleGenderSelectedText);
        RefreshGenderButton(PlayerCharacterSelection.CharacterGender.Female, femaleGenderCardImage, femaleGenderAccentImage, femaleGenderText, femaleGenderSelectedText);

        if (closeButtonText != null)
        {
            closeButtonText.text = GameLocalization.T("settings.close");
        }
    }

    private void RefreshGenderButton(PlayerCharacterSelection.CharacterGender gender, Image cardImage, Image accentImage, Text labelText, Text selectedText)
    {
        bool isSelected = PlayerCharacterSelection.SelectedGender == gender;

        if (cardImage != null)
        {
            cardImage.color = isSelected ? SelectedCardColor : CardColor;
        }

        if (accentImage != null)
        {
            accentImage.color = isSelected ? GreenAccentColor : new Color(0.78f, 0.55f, 0.25f, 0.35f);
        }

        if (labelText != null)
        {
            labelText.text = gender == PlayerCharacterSelection.CharacterGender.Female
                ? GameLocalization.T("character.female")
                : GameLocalization.T("character.male");
            labelText.color = isSelected ? Color.white : TextColor;
        }

        if (selectedText != null)
        {
            selectedText.text = isSelected ? GameLocalization.T("settings.selected") : "";
            selectedText.color = isSelected ? GreenAccentColor : MutedTextColor;
        }
    }

    private void RefreshLanguageButton(LanguageRow row)
    {
        bool isSelected = GameLocalization.CurrentLanguage == row.language;

        if (row.cardImage != null)
        {
            row.cardImage.color = isSelected ? SelectedCardColor : CardColor;
        }

        if (row.accentImage != null)
        {
            row.accentImage.color = isSelected ? GreenAccentColor : new Color(0.78f, 0.55f, 0.25f, 0.35f);
        }

        if (row.labelText != null)
        {
            row.labelText.text = GameLocalization.T(row.labelKey);
            row.labelText.color = isSelected ? Color.white : TextColor;
        }

        if (row.selectedText != null)
        {
            row.selectedText.text = isSelected ? GameLocalization.T("settings.selected") : "";
            row.selectedText.color = isSelected ? GreenAccentColor : MutedTextColor;
        }
    }

    private void CreateLanguageButton(
        RectTransform parent,
        GameLanguage language,
        string languageCode,
        string labelKey,
        Vector2 anchoredPosition)
    {
        LanguageRow row = new LanguageRow
        {
            language = language,
            labelKey = labelKey
        };

        RectTransform card = CreatePanel(languageCode + " Language Card", parent, anchoredPosition, new Vector2(650f, 52f), new Vector2(0.5f, 1f), CardColor);
        row.cardImage = card.GetComponent<Image>();

        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = row.cardImage;
        button.onClick.AddListener(() => GameLocalization.SetLanguage(language));

        row.accentImage = CreateRect(languageCode + " Selected Accent", card, new Vector2(-318f, 0f), new Vector2(6f, 40f), PanelTrimColor, new Vector2(0.5f, 0.5f)).GetComponent<Image>();
        CreateFlag(card, language, new Vector2(-256f, 0f));

        row.labelText = CreateText("", card, new Vector2(-42f, -17f), new Vector2(280f, 24f), 20, FontStyle.Bold, TextAnchor.MiddleLeft);
        Text codeText = CreateText(languageCode, card, new Vector2(-42f, -37f), new Vector2(280f, 16f), 12, FontStyle.Bold, TextAnchor.MiddleLeft);
        codeText.color = MutedTextColor;

        row.selectedText = CreateText("", card, new Vector2(236f, -26f), new Vector2(150f, 28f), 14, FontStyle.Bold, TextAnchor.MiddleCenter);
        languageRows.Add(row);
    }

    private void CreateGenderButton(
        RectTransform parent,
        PlayerCharacterSelection.CharacterGender gender,
        Vector2 anchoredPosition,
        out Image cardImage,
        out Image accentImage,
        out Text labelText,
        out Text selectedText)
    {
        RectTransform card = CreatePanel(gender + " Gender Card", parent, anchoredPosition, new Vector2(650f, 52f), new Vector2(0.5f, 1f), CardColor);
        cardImage = card.GetComponent<Image>();

        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = cardImage;
        button.onClick.AddListener(() => SelectGender(gender));

        accentImage = CreateRect(gender + " Selected Accent", card, new Vector2(-318f, 0f), new Vector2(6f, 40f), PanelTrimColor, new Vector2(0.5f, 0.5f)).GetComponent<Image>();
        CreateGenderIcon(card, gender, new Vector2(-256f, 0f));

        labelText = CreateText("", card, new Vector2(-42f, -26f), new Vector2(280f, 28f), 20, FontStyle.Bold, TextAnchor.MiddleLeft);
        selectedText = CreateText("", card, new Vector2(236f, -26f), new Vector2(150f, 28f), 14, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    private void SelectGender(PlayerCharacterSelection.CharacterGender gender)
    {
        PlayerCharacterSelection.SetSelectedGender(gender);

        if (LocalCoopManager.Instance != null)
        {
            LocalCoopManager.Instance.RequestImmediateStateSend();
        }

        RefreshTexts();
    }

    private Button CreateActionButton(string buttonName, RectTransform parent, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform buttonRect = CreatePanel(buttonName, parent, anchoredPosition, size, new Vector2(0.5f, 1f), PanelTrimColor);
        Image image = buttonRect.GetComponent<Image>();

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Text text = CreateText("", buttonRect, Vector2.zero, size, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
        text.color = DarkTextColor;
        return button;
    }

    private void CreateFlag(RectTransform parent, GameLanguage language, Vector2 anchoredPosition)
    {
        RectTransform frame = CreatePanel("Flag Frame", parent, anchoredPosition, new Vector2(78f, 48f), new Vector2(0.5f, 0.5f), new Color(0.055f, 0.04f, 0.028f, 1f));
        RectTransform flag = CreateRect("Flag", frame, Vector2.zero, new Vector2(68f, 38f), Color.white, new Vector2(0.5f, 0.5f));

        switch (language)
        {
            case GameLanguage.Polish:
                DrawPolishFlag(flag);
                break;
            case GameLanguage.Norwegian:
                DrawNorwegianFlag(flag);
                break;
            case GameLanguage.German:
                DrawGermanFlag(flag);
                break;
            case GameLanguage.Spanish:
                DrawSpanishFlag(flag);
                break;
            case GameLanguage.Swedish:
                DrawSwedishFlag(flag);
                break;
            case GameLanguage.Danish:
                DrawDanishFlag(flag);
                break;
            default:
                DrawEnglishFlag(flag);
                break;
        }
    }

    private void CreateGenderIcon(RectTransform parent, PlayerCharacterSelection.CharacterGender gender, Vector2 anchoredPosition)
    {
        Color iconColor = gender == PlayerCharacterSelection.CharacterGender.Female
            ? new Color(0.88f, 0.36f, 0.72f, 1f)
            : new Color(0.32f, 0.60f, 0.95f, 1f);
        RectTransform frame = CreatePanel("Gender Icon Frame", parent, anchoredPosition, new Vector2(46f, 46f), new Vector2(0.5f, 0.5f), new Color(0.055f, 0.04f, 0.028f, 1f));
        CreateRect("Gender Icon Fill", frame, Vector2.zero, new Vector2(38f, 38f), iconColor, new Vector2(0.5f, 0.5f));

        Text symbolText = CreateText(gender == PlayerCharacterSelection.CharacterGender.Female ? "\u2640" : "\u2642", frame, Vector2.zero, new Vector2(46f, 46f), 26, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform symbolRect = symbolText.rectTransform;
        symbolRect.anchorMin = Vector2.zero;
        symbolRect.anchorMax = Vector2.one;
        symbolRect.offsetMin = Vector2.zero;
        symbolRect.offsetMax = Vector2.zero;
        symbolRect.anchoredPosition = Vector2.zero;
        symbolRect.sizeDelta = Vector2.zero;
        symbolText.color = Color.white;
    }

    private void DrawEnglishFlag(RectTransform flag)
    {
        flag.GetComponent<Image>().color = new Color(0.04f, 0.16f, 0.44f, 1f);
        CreateRect("White Diagonal A", flag, Vector2.zero, new Vector2(86f, 8f), Color.white, new Vector2(0.5f, 0.5f), 28f);
        CreateRect("White Diagonal B", flag, Vector2.zero, new Vector2(86f, 8f), Color.white, new Vector2(0.5f, 0.5f), -28f);
        CreateRect("Red Diagonal A", flag, Vector2.zero, new Vector2(86f, 4f), new Color(0.78f, 0.02f, 0.08f, 1f), new Vector2(0.5f, 0.5f), 28f);
        CreateRect("Red Diagonal B", flag, Vector2.zero, new Vector2(86f, 4f), new Color(0.78f, 0.02f, 0.08f, 1f), new Vector2(0.5f, 0.5f), -28f);
        CreateRect("White Horizontal", flag, Vector2.zero, new Vector2(68f, 12f), Color.white, new Vector2(0.5f, 0.5f));
        CreateRect("White Vertical", flag, Vector2.zero, new Vector2(14f, 38f), Color.white, new Vector2(0.5f, 0.5f));
        CreateRect("Red Horizontal", flag, Vector2.zero, new Vector2(68f, 7f), new Color(0.78f, 0.02f, 0.08f, 1f), new Vector2(0.5f, 0.5f));
        CreateRect("Red Vertical", flag, Vector2.zero, new Vector2(8f, 38f), new Color(0.78f, 0.02f, 0.08f, 1f), new Vector2(0.5f, 0.5f));
    }

    private void DrawPolishFlag(RectTransform flag)
    {
        flag.GetComponent<Image>().color = Color.white;
        CreateRect("Polish Red", flag, new Vector2(0f, -9.5f), new Vector2(68f, 19f), new Color(0.82f, 0.03f, 0.12f, 1f), new Vector2(0.5f, 0.5f));
    }

    private void DrawGermanFlag(RectTransform flag)
    {
        flag.GetComponent<Image>().color = new Color(0.02f, 0.018f, 0.015f, 1f);
        CreateRect("German Red", flag, Vector2.zero, new Vector2(68f, 13f), new Color(0.82f, 0.02f, 0.05f, 1f), new Vector2(0.5f, 0.5f));
        CreateRect("German Gold", flag, new Vector2(0f, -12.5f), new Vector2(68f, 13f), new Color(1f, 0.80f, 0.08f, 1f), new Vector2(0.5f, 0.5f));
    }

    private void DrawSpanishFlag(RectTransform flag)
    {
        flag.GetComponent<Image>().color = new Color(0.78f, 0.02f, 0.08f, 1f);
        CreateRect("Spanish Gold", flag, Vector2.zero, new Vector2(68f, 20f), new Color(1f, 0.78f, 0.10f, 1f), new Vector2(0.5f, 0.5f));
    }

    private void DrawSwedishFlag(RectTransform flag)
    {
        flag.GetComponent<Image>().color = new Color(0.02f, 0.27f, 0.58f, 1f);
        CreateRect("Sweden Yellow Horizontal", flag, Vector2.zero, new Vector2(68f, 8f), new Color(1f, 0.82f, 0.10f, 1f), new Vector2(0.5f, 0.5f));
        CreateRect("Sweden Yellow Vertical", flag, new Vector2(-14f, 0f), new Vector2(8f, 38f), new Color(1f, 0.82f, 0.10f, 1f), new Vector2(0.5f, 0.5f));
    }

    private void DrawDanishFlag(RectTransform flag)
    {
        flag.GetComponent<Image>().color = new Color(0.78f, 0.02f, 0.08f, 1f);
        CreateRect("Denmark White Horizontal", flag, Vector2.zero, new Vector2(68f, 8f), Color.white, new Vector2(0.5f, 0.5f));
        CreateRect("Denmark White Vertical", flag, new Vector2(-14f, 0f), new Vector2(8f, 38f), Color.white, new Vector2(0.5f, 0.5f));
    }

    private void DrawNorwegianFlag(RectTransform flag)
    {
        flag.GetComponent<Image>().color = new Color(0.74f, 0.03f, 0.10f, 1f);
        CreateRect("Norway White Horizontal", flag, Vector2.zero, new Vector2(68f, 12f), Color.white, new Vector2(0.5f, 0.5f));
        CreateRect("Norway White Vertical", flag, new Vector2(-14f, 0f), new Vector2(12f, 38f), Color.white, new Vector2(0.5f, 0.5f));
        CreateRect("Norway Blue Horizontal", flag, Vector2.zero, new Vector2(68f, 6f), new Color(0.02f, 0.13f, 0.42f, 1f), new Vector2(0.5f, 0.5f));
        CreateRect("Norway Blue Vertical", flag, new Vector2(-14f, 0f), new Vector2(6f, 38f), new Color(0.02f, 0.13f, 0.42f, 1f), new Vector2(0.5f, 0.5f));
    }

    private RectTransform CreatePanel(string panelName, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchor, Color color)
    {
        RectTransform panel = CreateUiObject(panelName, parent);
        panel.anchorMin = anchor;
        panel.anchorMax = anchor;
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = anchoredPosition;
        panel.sizeDelta = size;

        Image image = panel.gameObject.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private Text CreateText(string value, RectTransform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        RectTransform textRect = CreateUiObject("Text", parent);
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = size;

        Text text = textRect.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = TextColor;
        text.raycastTarget = false;
        return text;
    }

    private RectTransform CreateRect(string objectName, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchor, float zRotation = 0f)
    {
        RectTransform rectTransform = CreateUiObject(objectName, parent);
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, zRotation);

        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rectTransform;
    }

    private static RectTransform CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        return gameObject.AddComponent<RectTransform>();
    }
}
