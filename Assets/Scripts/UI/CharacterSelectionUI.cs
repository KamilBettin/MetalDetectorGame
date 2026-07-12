using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform root;
    private Text titleText;
    private Text hintText;
    private Text maleGenderText;
    private Text femaleGenderText;
    private Text closeButtonText;
    private Button maleGenderButton;
    private Button femaleGenderButton;
    private Image maleGenderImage;
    private Image femaleGenderImage;
    private PlayerCharacterSelection.CharacterGender selectedGender;

    public static CharacterSelectionUI Instance { get; private set; }
    public bool IsOpen => root != null && root.gameObject.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildCanvas();
        GameLocalization.LanguageChanged += RefreshSelection;
        SetOpen(false);
    }

    private void OnDestroy()
    {
        if (GameUIState.IsCharacterSelectionOpen)
        {
            GameUIState.SetCharacterSelectionOpen(false);
        }

        if (Instance == this)
        {
            Instance = null;
        }

        GameLocalization.LanguageChanged -= RefreshSelection;
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
        CharacterSelectionUI selectionUi = Instance;

        if (selectionUi == null)
        {
            selectionUi = new GameObject("Character Selection UI").AddComponent<CharacterSelectionUI>();
        }

        selectionUi.SetOpen(true);
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Character Selection Canvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1400;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        root = CreateUiObject("Character Selection Root", canvas.transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Image shade = root.gameObject.AddComponent<Image>();
        shade.color = new Color(0f, 0f, 0f, 0.58f);

        RectTransform panel = CreatePanel("Character Panel", root, new Vector2(0f, 0f), new Vector2(680f, 360f), new Vector2(0.5f, 0.5f));
        titleText = CreateText("CHARACTER", panel, new Vector2(0f, -40f), new Vector2(600f, 58f), 34, FontStyle.Bold, TextAnchor.MiddleCenter);
        hintText = CreateText("", panel, new Vector2(0f, -96f), new Vector2(600f, 38f), 18, FontStyle.Bold, TextAnchor.MiddleCenter);

        maleGenderButton = CreateButton("MALE", panel, new Vector2(-145f, -178f), new Vector2(240f, 72f), () => SelectGender(PlayerCharacterSelection.CharacterGender.Male));
        femaleGenderButton = CreateButton("FEMALE", panel, new Vector2(145f, -178f), new Vector2(240f, 72f), () => SelectGender(PlayerCharacterSelection.CharacterGender.Female));
        maleGenderImage = maleGenderButton.targetGraphic as Image;
        femaleGenderImage = femaleGenderButton.targetGraphic as Image;
        maleGenderText = maleGenderButton.GetComponentInChildren<Text>();
        femaleGenderText = femaleGenderButton.GetComponentInChildren<Text>();

        Button closeButton = CreateButton("CLOSE", panel, new Vector2(0f, -290f), new Vector2(220f, 52f), () => SetOpen(false));
        closeButtonText = closeButton.GetComponentInChildren<Text>();
    }

    private void SelectGender(PlayerCharacterSelection.CharacterGender gender)
    {
        selectedGender = gender;
        PlayerCharacterSelection.SetSelectedGender(selectedGender);
        RefreshNetworkCharacter();
        RefreshSelection();
    }

    private void SetOpen(bool open)
    {
        if (root == null)
        {
            return;
        }

        root.gameObject.SetActive(open);
        GameUIState.SetCharacterSelectionOpen(open);

        if (open)
        {
            selectedGender = PlayerCharacterSelection.SelectedGender;
            RefreshSelection();
        }
    }

    private void RefreshSelection()
    {
        if (maleGenderImage != null)
        {
            maleGenderImage.color = selectedGender == PlayerCharacterSelection.CharacterGender.Male
                ? new Color(0.33f, 0.58f, 0.82f, 0.98f)
                : new Color(0.36f, 0.28f, 0.18f, 0.96f);
        }

        if (femaleGenderImage != null)
        {
            femaleGenderImage.color = selectedGender == PlayerCharacterSelection.CharacterGender.Female
                ? new Color(0.70f, 0.42f, 0.70f, 0.98f)
                : new Color(0.36f, 0.28f, 0.18f, 0.96f);
        }

        titleText.text = GameLocalization.TFormat("character.title", GetLocalizedGenderLabel(selectedGender).ToUpperInvariant());
        hintText.text = GameLocalization.T("character.hint");

        if (maleGenderText != null)
        {
            maleGenderText.text = GameLocalization.T("character.male");
        }

        if (femaleGenderText != null)
        {
            femaleGenderText.text = GameLocalization.T("character.female");
        }

        if (closeButtonText != null)
        {
            closeButtonText.text = GameLocalization.T("settings.close").ToUpperInvariant();
        }
    }

    private static string GetLocalizedGenderLabel(PlayerCharacterSelection.CharacterGender gender)
    {
        return gender == PlayerCharacterSelection.CharacterGender.Female
            ? GameLocalization.T("character.female")
            : GameLocalization.T("character.male");
    }

    private void RefreshNetworkCharacter()
    {
        LocalCoopManager.Instance?.RequestImmediateStateSend();
    }

    private RectTransform CreatePanel(string panelName, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
    {
        RectTransform panel = CreateUiObject(panelName, parent);
        panel.anchorMin = anchor;
        panel.anchorMax = anchor;
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = anchoredPosition;
        panel.sizeDelta = size;

        Image image = panel.gameObject.AddComponent<Image>();
        image.color = new Color(0.24f, 0.14f, 0.07f, 0.96f);

        return panel;
    }

    private Button CreateButton(string label, RectTransform parent, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform buttonRect = CreateUiObject(label + " Button", parent);
        buttonRect.anchorMin = new Vector2(0.5f, 1f);
        buttonRect.anchorMax = new Vector2(0.5f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;

        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.color = new Color(0.82f, 0.63f, 0.38f, 0.96f);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Text text = CreateText(label, buttonRect, Vector2.zero, size, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.color = new Color(0.10f, 0.06f, 0.025f, 1f);

        return button;
    }

    private Text CreateText(string value, RectTransform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        RectTransform textRect = CreateUiObject(value + " Text", parent);
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
        text.color = new Color(0.96f, 0.82f, 0.56f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        return gameObject.AddComponent<RectTransform>();
    }
}
