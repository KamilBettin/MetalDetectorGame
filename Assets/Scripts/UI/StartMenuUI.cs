using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StartMenuUI : MonoBehaviour
{
    private const string BackgroundResourcePath = "UI/StartMenuBackground";
    private static bool hasShownThisSession;
    private static StartMenuUI instance;

    private Canvas canvas;
    private RectTransform menuRoot;
    private RectTransform selectionFrame;
    private RectTransform multiplayerPanel;
    private Text messageText;
    private InputField playerNameInput;
    private InputField addressInput;
    private InputField portInput;
    private int selectedIndex;
    private int hoveredIndex = -1;
    private float messageTimer;
    private bool isLaunchingGame;
    private readonly string[] menuItems = { "New Game", "Continue", "Multiplayer", "Settings", "Quit" };

    public static void AllowShowingAgain()
    {
        hasShownThisSession = false;
    }

    public static void CloseForSteamJoin()
    {
        if (instance != null)
        {
            instance.CloseMenu();
        }
    }

    private void Awake()
    {
        if (hasShownThisSession)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        hasShownThisSession = true;
        LoadingScreenUI.ForceHide();
        BuildCanvas();
        SetMenuOpen(true);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (GameUIState.IsStartMenuOpen)
        {
            SetMenuOpen(false);
        }
    }

    private void Update()
    {
        if (!GameUIState.IsStartMenuOpen)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (GameUIState.IsConfirmationOpen)
        {
            UpdateMessage();
            return;
        }

        if (GameUIState.IsSettingsMenuOpen || GameUIState.IsCharacterSelectionOpen || GameUIState.MenuClosedThisFrame)
        {
            UpdateMessage();
            return;
        }

        if (multiplayerPanel != null && multiplayerPanel.gameObject.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                multiplayerPanel.gameObject.SetActive(false);
            }

            UpdateMessage();
            return;
        }

        HandleKeyboardInput();
        UpdateMessage();
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Start Menu Canvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        menuRoot = CreateUiObject("Start Menu Root", canvas.transform);
        menuRoot.anchorMin = Vector2.zero;
        menuRoot.anchorMax = Vector2.one;
        menuRoot.offsetMin = Vector2.zero;
        menuRoot.offsetMax = Vector2.zero;

        CreateBackground(menuRoot);
        CreateMenuButtons(menuRoot);
        CreateSelectionFrame(menuRoot);
        CreateMultiplayerPanel(menuRoot);
        CreateMessageText(menuRoot);
    }

    private void CreateBackground(RectTransform parent)
    {
        Texture2D backgroundTexture = Resources.Load<Texture2D>(BackgroundResourcePath);
        Image background = CreateUiObject("Start Menu Background", parent).gameObject.AddComponent<Image>();
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = Vector2.zero;
        background.rectTransform.offsetMax = Vector2.zero;
        background.raycastTarget = false;

        if (backgroundTexture == null)
        {
            background.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            return;
        }

        background.sprite = Sprite.Create(
            backgroundTexture,
            new Rect(0f, 0f, backgroundTexture.width, backgroundTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        background.color = Color.white;
    }

    private void CreateMenuButtons(RectTransform parent)
    {
        Vector2 buttonSize = new Vector2(452f, 118f);
        float left = 70f;
        float[] topPositions = { 252f, 384f, 516f, 649f, 781f };

        for (int i = 0; i < menuItems.Length; i++)
        {
            int itemIndex = i;
            RectTransform buttonTransform = CreateUiObject(menuItems[i] + " Click Area", parent);
            buttonTransform.anchorMin = new Vector2(0f, 1f);
            buttonTransform.anchorMax = new Vector2(0f, 1f);
            buttonTransform.pivot = new Vector2(0f, 1f);
            buttonTransform.anchoredPosition = new Vector2(left, -topPositions[i]);
            buttonTransform.sizeDelta = buttonSize;

            Image image = buttonTransform.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 0.78f, 0.24f, 0f);

            Button button = buttonTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => ActivateItem(itemIndex));

            EventTrigger trigger = buttonTransform.gameObject.AddComponent<EventTrigger>();
            AddEventTrigger(trigger, EventTriggerType.PointerEnter, () => HandlePointerEnter(itemIndex));
            AddEventTrigger(trigger, EventTriggerType.PointerExit, () => HandlePointerExit(itemIndex));
        }
    }

    private void CreateSelectionFrame(RectTransform parent)
    {
        selectionFrame = CreateUiObject("Selected Menu Frame", parent);
        selectionFrame.anchorMin = new Vector2(0f, 1f);
        selectionFrame.anchorMax = new Vector2(0f, 1f);
        selectionFrame.pivot = new Vector2(0f, 1f);
        selectionFrame.sizeDelta = new Vector2(452f, 118f);

        Image frameImage = selectionFrame.gameObject.AddComponent<Image>();
        frameImage.color = new Color(1f, 0.82f, 0.32f, 0.08f);
        frameImage.raycastTarget = false;
        SetSelected(0);
    }

    private void CreateMessageText(RectTransform parent)
    {
        RectTransform panel = CreateUiObject("Start Menu Message", parent);
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 78f);
        panel.sizeDelta = new Vector2(760f, 42f);

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.04f, 0.025f, 0.72f);
        panelImage.raycastTarget = false;

        messageText = CreateText("", panel, Vector2.zero, panel.sizeDelta, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        messageText.color = new Color(1f, 0.86f, 0.46f, 1f);
        panel.gameObject.SetActive(false);
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
        {
            SetSelected((selectedIndex + menuItems.Length - 1) % menuItems.Length);
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
        {
            SetSelected((selectedIndex + 1) % menuItems.Length);
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ActivateItem(selectedIndex);
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ShowMessage("Choose New Game or Continue to enter the island.");
        }
    }

    private void ActivateItem(int itemIndex)
    {
        if (isLaunchingGame)
        {
            return;
        }

        SetSelected(itemIndex);

        switch (itemIndex)
        {
            case 0:
                StartCoroutine(LaunchNewGameRoutine());
                break;
            case 1:
                ShowContinueSlots();
                break;
            case 2:
                ShowMultiplayerPanel();
                break;
            case 3:
                SettingsMenuUI.Open();
                break;
            case 4:
                QuitGame();
                break;
        }
    }

    private IEnumerator LaunchNewGameRoutine()
    {
        isLaunchingGame = true;
        LoadingScreenUI.Show("Loading island", 2.85f);
        yield return null;

        GameSaveSystem.StartNewGame();
        LoadingScreenUI.HideAfterMinimum(0.25f);
        CloseMenu();
        IntroLetterUI.Show();
    }

    private void ShowContinueSlots()
    {
        SaveSlotDialogUI.ShowLoad(
            slotIndex => StartCoroutine(LaunchContinueRoutine(slotIndex)),
            () => isLaunchingGame = false);
    }

    private IEnumerator LaunchContinueRoutine(int slotIndex)
    {
        isLaunchingGame = true;
        LoadingScreenUI.Show("Loading save", 2.75f);
        yield return null;

        bool loaded = GameSaveSystem.ContinueGame(slotIndex, out string continueMessage);

        if (!loaded)
        {
            isLaunchingGame = false;
            LoadingScreenUI.HideAfterMinimum(0.1f);
            ShowMessage(continueMessage);
            yield break;
        }

        LoadingScreenUI.HideAfterMinimum(0.25f);
        CloseMenu();
    }

    private void CloseMenu()
    {
        SetMenuOpen(false);
        Destroy(gameObject);
    }

    private void ShowMultiplayerPanel()
    {
        if (multiplayerPanel == null)
        {
            ShowMessage("Multiplayer UI is not ready.");
            return;
        }

        multiplayerPanel.gameObject.SetActive(true);
        ShowMessage("Steam is recommended. LAN remains available as a backup connection mode.");
    }

    private void StartHostFromPanel()
    {
        ShowHostSaveSlots(false);
    }

    private void StartClientFromPanel()
    {
        LocalCoopManager coopManager = LocalCoopManager.Instance;

        if (coopManager == null)
        {
            ShowMessage("Local co-op manager is missing.");
            return;
        }

        string address = addressInput != null && !string.IsNullOrWhiteSpace(addressInput.text)
            ? addressInput.text.Trim()
            : "127.0.0.1";
        int port = ReadPort();
        bool started = coopManager.StartClient(address, port, playerNameInput != null ? playerNameInput.text : "Client");
        ShowMessage(coopManager.StatusText);

        if (started)
        {
            CloseMenu();
        }
    }

    private int ReadPort()
    {
        if (portInput != null && int.TryParse(portInput.text, out int port))
        {
            return Mathf.Clamp(port, 1, 65535);
        }

        return LocalCoopManager.Instance != null ? LocalCoopManager.Instance.defaultPort : 7777;
    }

    private void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        ShowMessage("Quit works in a built game. In Editor, the menu stays open.");
#endif
    }

    private void SetSelected(int itemIndex)
    {
        SetSelected(itemIndex, true);
    }

    private void SetSelected(int itemIndex, bool showFrame)
    {
        selectedIndex = Mathf.Clamp(itemIndex, 0, menuItems.Length - 1);

        if (selectionFrame == null)
        {
            return;
        }

        selectionFrame.gameObject.SetActive(showFrame);

        if (!showFrame)
        {
            return;
        }

        float[] topPositions = { 252f, 384f, 516f, 649f, 781f };
        selectionFrame.anchoredPosition = new Vector2(80f, -topPositions[selectedIndex] - 14f);
        selectionFrame.sizeDelta = new Vector2(432f, 98f);
    }

    private void HandlePointerEnter(int itemIndex)
    {
        hoveredIndex = itemIndex;
        SetSelected(itemIndex);
    }

    private void HandlePointerExit(int itemIndex)
    {
        if (hoveredIndex != itemIndex)
        {
            return;
        }

        hoveredIndex = -1;
        SetSelected(selectedIndex, false);
    }

    private void CreateMultiplayerPanel(RectTransform parent)
    {
        multiplayerPanel = CreatePanel("Multiplayer Panel", parent, new Vector2(590f, -120f), new Vector2(700f, 800f), new Vector2(0f, 1f));
        Image panelImage = multiplayerPanel.GetComponent<Image>();
        panelImage.color = new Color(0.045f, 0.032f, 0.022f, 0.97f);
        Shadow panelShadow = multiplayerPanel.gameObject.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0.01f, 0.008f, 0.006f, 0.68f);
        panelShadow.effectDistance = new Vector2(10f, -10f);
        AddOutline(multiplayerPanel.gameObject, new Color(0.86f, 0.64f, 0.28f, 0.36f), new Vector2(2f, -2f));

        RectTransform topAccent = CreatePanel("Top Accent", multiplayerPanel, Vector2.zero, new Vector2(700f, 5f), new Vector2(0f, 1f));
        topAccent.GetComponent<Image>().color = new Color(0.96f, 0.68f, 0.24f, 1f);

        CreateAnchoredText("MULTIPLAYER", multiplayerPanel, new Vector2(28f, -21f), new Vector2(400f, 40f), 32, FontStyle.Bold, TextAnchor.MiddleLeft);
        Text headerHint = CreateAnchoredText("HOST A WORLD OR JOIN YOUR FRIENDS", multiplayerPanel, new Vector2(30f, -60f), new Vector2(500f, 24f), 14, FontStyle.Bold, TextAnchor.MiddleLeft);
        headerHint.color = new Color(0.75f, 0.68f, 0.57f, 1f);

        RectTransform playerCard = CreatePanel("Player Setup", multiplayerPanel, new Vector2(28f, -94f), new Vector2(644f, 62f), new Vector2(0f, 1f));
        playerCard.GetComponent<Image>().color = new Color(0.11f, 0.075f, 0.044f, 0.95f);
        CreateAnchoredText("PLAYER NAME", playerCard, new Vector2(18f, -18f), new Vector2(150f, 26f), 15, FontStyle.Bold, TextAnchor.MiddleLeft);
        playerNameInput = CreateInputField(playerCard, new Vector2(164f, -11f), new Vector2(300f, 40f), "Hunter");
        Text playerLimit = CreateAnchoredText("UP TO 4 PLAYERS", playerCard, new Vector2(480f, -18f), new Vector2(146f, 26f), 13, FontStyle.Bold, TextAnchor.MiddleCenter);
        playerLimit.color = new Color(0.95f, 0.72f, 0.3f, 1f);

        RectTransform steamCard = CreatePanel("Steam Recommended", multiplayerPanel, new Vector2(28f, -174f), new Vector2(644f, 224f), new Vector2(0f, 1f));
        steamCard.GetComponent<Image>().color = new Color(0.09f, 0.066f, 0.045f, 0.98f);
        AddOutline(steamCard.gameObject, new Color(0.58f, 0.44f, 0.25f, 0.56f), new Vector2(2f, -2f));

        Text steamTitle = CreateAnchoredText("PLAY WITH STEAM FRIENDS", steamCard, new Vector2(24f, -24f), new Vector2(440f, 31f), 22, FontStyle.Bold, TextAnchor.MiddleLeft);
        steamTitle.color = new Color(0.3f, 0.72f, 1f, 1f);
        Text steamDescription = CreateAnchoredText("Friends-only lobby  |  invites open automatically", steamCard, new Vector2(25f, -57f), new Vector2(470f, 25f), 14, FontStyle.Normal, TextAnchor.MiddleLeft);
        steamDescription.color = new Color(0.75f, 0.7f, 0.62f, 1f);
        CreateBadge("ONLINE MODE", steamCard, new Vector2(497f, -25f), new Vector2(124f, 30f), new Color(0.15f, 0.12f, 0.085f, 1f), new Color(0.3f, 0.72f, 1f, 1f));

        CreateStyledButton(
            "HOST ON STEAM + INVITE FRIENDS",
            steamCard,
            new Vector2(24f, -111f),
            new Vector2(596f, 64f),
            StartSteamHostFromPanel,
            new Color(0.72f, 0.52f, 0.25f, 1f),
            new Color(0.86f, 0.64f, 0.31f, 1f),
            new Color(0.1f, 0.065f, 0.03f, 1f),
            20);
        Text steamNote = CreateAnchoredText("Start a new world or load a save, then invite friends through Steam.", steamCard, new Vector2(25f, -184f), new Vector2(594f, 24f), 14, FontStyle.Normal, TextAnchor.MiddleCenter);
        steamNote.color = new Color(0.68f, 0.62f, 0.53f, 1f);

        RectTransform lanCard = CreatePanel("LAN Backup", multiplayerPanel, new Vector2(28f, -420f), new Vector2(644f, 292f), new Vector2(0f, 1f));
        lanCard.GetComponent<Image>().color = new Color(0.09f, 0.066f, 0.045f, 0.96f);
        AddOutline(lanCard.gameObject, new Color(0.52f, 0.41f, 0.26f, 0.48f), new Vector2(1f, -1f));

        CreateAnchoredText("LAN / DIRECT IP", lanCard, new Vector2(22f, -19f), new Vector2(300f, 30f), 20, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateBadge("LOCAL / LAN", lanCard, new Vector2(484f, -18f), new Vector2(136f, 29f), new Color(0.2f, 0.16f, 0.11f, 1f), new Color(0.9f, 0.72f, 0.4f, 1f));
        Text lanDescription = CreateAnchoredText("Use this when Steam is unavailable or for local network play.", lanCard, new Vector2(23f, -52f), new Vector2(570f, 24f), 14, FontStyle.Normal, TextAnchor.MiddleLeft);
        lanDescription.color = new Color(0.73f, 0.68f, 0.6f, 1f);

        CreateAnchoredText("HOST ADDRESS", lanCard, new Vector2(23f, -88f), new Vector2(250f, 22f), 13, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateAnchoredText("PORT", lanCard, new Vector2(433f, -88f), new Vector2(100f, 22f), 13, FontStyle.Bold, TextAnchor.MiddleLeft);
        addressInput = CreateInputField(lanCard, new Vector2(23f, -113f), new Vector2(390f, 42f), "127.0.0.1");
        int defaultPort = LocalCoopManager.Instance != null ? LocalCoopManager.Instance.defaultPort : 7777;
        portInput = CreateInputField(lanCard, new Vector2(433f, -113f), new Vector2(187f, 42f), defaultPort.ToString());
        portInput.contentType = InputField.ContentType.IntegerNumber;

        CreateStyledButton("HOST VIA LAN", lanCard, new Vector2(23f, -177f), new Vector2(286f, 54f), StartHostFromPanel, new Color(0.68f, 0.49f, 0.24f, 1f), new Color(0.82f, 0.61f, 0.3f, 1f), new Color(0.1f, 0.065f, 0.03f, 1f), 17);
        CreateStyledButton("JOIN VIA LAN", lanCard, new Vector2(329f, -177f), new Vector2(291f, 54f), StartClientFromPanel, new Color(0.29f, 0.25f, 0.19f, 1f), new Color(0.42f, 0.35f, 0.25f, 1f), new Color(0.94f, 0.87f, 0.73f, 1f), 17);
        Text lanNote = CreateAnchoredText("LAN host can start a new world or load a saved one.", lanCard, new Vector2(24f, -244f), new Vector2(596f, 24f), 13, FontStyle.Normal, TextAnchor.MiddleCenter);
        lanNote.color = new Color(0.66f, 0.6f, 0.51f, 1f);

        CreateStyledButton("BACK", multiplayerPanel, new Vector2(248f, -733f), new Vector2(204f, 46f), () => multiplayerPanel.gameObject.SetActive(false), new Color(0.2f, 0.15f, 0.1f, 1f), new Color(0.32f, 0.23f, 0.14f, 1f), new Color(0.92f, 0.83f, 0.67f, 1f), 16);
        multiplayerPanel.gameObject.SetActive(false);
    }

    private RectTransform CreatePanel(string panelName, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
    {
        RectTransform panel = CreateUiObject(panelName, parent);
        panel.anchorMin = anchor;
        panel.anchorMax = anchor;
        panel.pivot = anchor;
        panel.anchoredPosition = anchoredPosition;
        panel.sizeDelta = size;

        Image image = panel.gameObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.052f, 0.026f, 0.9f);
        return panel;
    }

    private Button CreatePanelButton(string label, RectTransform parent, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        RectTransform buttonTransform = CreatePanel(label + " Button", parent, anchoredPosition, size, new Vector2(0f, 1f));
        Image image = buttonTransform.GetComponent<Image>();
        image.color = new Color(0.78f, 0.55f, 0.22f, 0.94f);

        Button button = buttonTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text text = CreateAnchoredText(label, buttonTransform, Vector2.zero, size, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.color = new Color(0.09f, 0.055f, 0.022f, 1f);
        return button;
    }

    private Button CreateStyledButton(
        string label,
        RectTransform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        UnityEngine.Events.UnityAction action,
        Color normalColor,
        Color highlightedColor,
        Color textColor,
        int fontSize)
    {
        RectTransform buttonTransform = CreatePanel(label + " Button", parent, anchoredPosition, size, new Vector2(0f, 1f));
        Image image = buttonTransform.GetComponent<Image>();
        image.color = Color.white;
        AddOutline(buttonTransform.gameObject, new Color(1f, 1f, 1f, 0.16f), new Vector2(1f, -1f));

        Button button = buttonTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = Color.Lerp(highlightedColor, Color.black, 0.16f);
        colors.selectedColor = highlightedColor;
        colors.disabledColor = new Color(normalColor.r * 0.55f, normalColor.g * 0.55f, normalColor.b * 0.55f, 0.65f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(action);

        Text text = CreateAnchoredText(label, buttonTransform, Vector2.zero, size, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.color = textColor;
        return button;
    }

    private void CreateBadge(string label, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, Color textColor)
    {
        RectTransform badge = CreatePanel(label + " Badge", parent, anchoredPosition, size, new Vector2(0f, 1f));
        badge.GetComponent<Image>().color = backgroundColor;
        Text badgeText = CreateAnchoredText(label, badge, Vector2.zero, size, 12, FontStyle.Bold, TextAnchor.MiddleCenter);
        badgeText.color = textColor;
    }

    private void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private InputField CreateInputField(RectTransform parent, Vector2 anchoredPosition, Vector2 size, string value)
    {
        RectTransform inputTransform = CreatePanel("Input Field", parent, anchoredPosition, size, new Vector2(0f, 1f));
        Image image = inputTransform.GetComponent<Image>();
        image.color = new Color(0.95f, 0.82f, 0.58f, 0.92f);

        InputField inputField = inputTransform.gameObject.AddComponent<InputField>();
        inputField.targetGraphic = image;

        Text text = CreateAnchoredText(value, inputTransform, new Vector2(10f, 0f), new Vector2(size.x - 20f, size.y), 17, FontStyle.Bold, TextAnchor.MiddleLeft);
        text.color = new Color(0.09f, 0.055f, 0.022f, 1f);
        inputField.textComponent = text;
        inputField.text = value;

        Text placeholder = CreateAnchoredText(value, inputTransform, new Vector2(10f, 0f), new Vector2(size.x - 20f, size.y), 17, FontStyle.Normal, TextAnchor.MiddleLeft);
        placeholder.color = new Color(0.28f, 0.2f, 0.12f, 0.55f);
        inputField.placeholder = placeholder;
        return inputField;
    }

    private void StartSteamHostFromPanel()
    {
        ShowHostSaveSlots(true);
    }

    private void ShowHostSaveSlots(bool useSteam)
    {
        LocalCoopManager coopManager = LocalCoopManager.Instance;

        if (coopManager == null)
        {
            ShowMessage("Local co-op manager is missing.");
            return;
        }

        string playerName = playerNameInput != null ? playerNameInput.text : "Host";
        int port = ReadPort();
        SaveSlotDialogUI.ShowHost(
            slotIndex => StartCoroutine(LaunchHostRoutine(slotIndex, false, useSteam, port, playerName)),
            () => StartCoroutine(LaunchHostRoutine(-1, true, useSteam, port, playerName)),
            () => isLaunchingGame = false);
    }

    private IEnumerator LaunchHostRoutine(int slotIndex, bool startNewGame, bool useSteam, int port, string playerName)
    {
        isLaunchingGame = true;
        string loadingMessage = startNewGame
            ? (useSteam ? "Creating new Steam world" : "Creating new LAN world")
            : (useSteam ? "Loading Steam host save" : "Loading LAN host save");
        LoadingScreenUI.Show(loadingMessage, 2.75f);
        yield return null;

        if (startNewGame)
        {
            GameSaveSystem.StartNewGame();
        }
        else if (!GameSaveSystem.ContinueGame(slotIndex, out string loadMessage))
        {
            isLaunchingGame = false;
            LoadingScreenUI.HideAfterMinimum(0.1f);
            ShowMessage(loadMessage);
            yield break;
        }

        LocalCoopManager coopManager = LocalCoopManager.Instance;

        if (coopManager == null)
        {
            isLaunchingGame = false;
            LoadingScreenUI.HideAfterMinimum(0.1f);
            ShowMessage("Local co-op manager is missing.");
            yield break;
        }

        bool started = useSteam
            ? coopManager.StartSteamHost(playerName)
            : coopManager.StartHost(port, playerName);

        if (started)
        {
            LoadingScreenUI.HideAfterMinimum(0.25f);
            CloseMenu();

            if (startNewGame)
            {
                IntroLetterUI.Show();
            }

            yield break;
        }

        isLaunchingGame = false;
        LoadingScreenUI.HideAfterMinimum(0.1f);
        ShowMessage(coopManager.StatusText);
    }

    private void StartSteamClientFromPanel()
    {
        LocalCoopManager coopManager = LocalCoopManager.Instance;

        if (coopManager == null)
        {
            ShowMessage("Local co-op manager is missing.");
            return;
        }

        if (addressInput == null || !ulong.TryParse(addressInput.text.Trim(), out ulong hostSteamId))
        {
            ShowMessage("Enter host Steam ID in Address.");
            return;
        }

        bool started = coopManager.StartSteamClient(hostSteamId, playerNameInput != null ? playerNameInput.text : "Client");
        ShowMessage(coopManager.StatusText);

        if (started)
        {
            CloseMenu();
        }
    }

    private void ShowMessage(string message)
    {
        if (messageText == null)
        {
            return;
        }

        messageText.transform.parent.gameObject.SetActive(true);
        messageText.text = message;
        messageTimer = 2.4f;
    }

    private void UpdateMessage()
    {
        if (messageText == null || !messageText.transform.parent.gameObject.activeSelf)
        {
            return;
        }

        messageTimer -= Time.unscaledDeltaTime;

        if (messageTimer <= 0f)
        {
            messageText.transform.parent.gameObject.SetActive(false);
        }
    }

    private void SetMenuOpen(bool isOpen)
    {
        GameUIState.SetStartMenuOpen(isOpen);
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;
    }

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    private Text CreateText(string value, RectTransform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        RectTransform rectTransform = CreateUiObject("Text", parent);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Text text = rectTransform.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Text CreateAnchoredText(string value, RectTransform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        RectTransform rectTransform = CreateUiObject("Text", parent);
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Text text = rectTransform.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = new Color(0.98f, 0.94f, 0.82f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private RectTransform CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        return gameObject.AddComponent<RectTransform>();
    }
}
