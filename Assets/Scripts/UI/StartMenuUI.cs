using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StartMenuUI : MonoBehaviour
{
    private const string BackgroundResourcePath = "UI/StartMenuBackground";
    private static bool hasShownThisSession;

    private Canvas canvas;
    private RectTransform menuRoot;
    private RectTransform selectionFrame;
    private RectTransform multiplayerPanel;
    private Text messageText;
    private InputField playerNameInput;
    private InputField addressInput;
    private InputField portInput;
    private int selectedIndex;
    private float messageTimer;
    private readonly string[] menuItems = { "New Game", "Continue", "Multiplayer", "Settings", "Quit" };

    public static void AllowShowingAgain()
    {
        hasShownThisSession = false;
    }

    private void Awake()
    {
        if (hasShownThisSession)
        {
            Destroy(gameObject);
            return;
        }

        hasShownThisSession = true;
        BuildCanvas();
        SetMenuOpen(true);
    }

    private void OnDestroy()
    {
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
            AddEventTrigger(trigger, EventTriggerType.PointerEnter, () => SetSelected(itemIndex));
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
        frameImage.color = new Color(1f, 0.78f, 0.24f, 0.16f);
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
        SetSelected(itemIndex);

        switch (itemIndex)
        {
            case 0:
                CloseMenu();
                IntroLetterUI.Show();
                break;
            case 1:
                ShowMessage("No save file yet. Starting the current island.");
                CloseMenu();
                break;
            case 2:
                ShowMultiplayerPanel();
                break;
            case 3:
                CharacterSelectionUI.Open();
                ShowMessage("Choose your co-op character.");
                break;
            case 4:
                QuitGame();
                break;
        }
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
        ShowMessage("Host creates a local co-op session. Join uses LAN IP or 127.0.0.1.");
    }

    private void StartHostFromPanel()
    {
        LocalCoopManager coopManager = LocalCoopManager.Instance;

        if (coopManager == null)
        {
            ShowMessage("Local co-op manager is missing.");
            return;
        }

        int port = ReadPort();
        bool started = coopManager.StartHost(port, playerNameInput != null ? playerNameInput.text : "Host");
        ShowMessage(coopManager.StatusText);

        if (started)
        {
            CloseMenu();
        }
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
        selectedIndex = Mathf.Clamp(itemIndex, 0, menuItems.Length - 1);

        if (selectionFrame == null)
        {
            return;
        }

        float[] topPositions = { 252f, 384f, 516f, 649f, 781f };
        selectionFrame.anchoredPosition = new Vector2(70f, -topPositions[selectedIndex]);
    }

    private void CreateMultiplayerPanel(RectTransform parent)
    {
        multiplayerPanel = CreatePanel("Multiplayer Panel", parent, new Vector2(590f, -210f), new Vector2(560f, 520f), new Vector2(0f, 1f));
        Image panelImage = multiplayerPanel.GetComponent<Image>();
        panelImage.color = new Color(0.07f, 0.045f, 0.025f, 0.92f);

        CreateAnchoredText("LOCAL CO-OP", multiplayerPanel, new Vector2(24f, -22f), new Vector2(492f, 36f), 28, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateAnchoredText("Player", multiplayerPanel, new Vector2(28f, -82f), new Vector2(150f, 26f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        playerNameInput = CreateInputField(multiplayerPanel, new Vector2(176f, -76f), new Vector2(300f, 38f), "Hunter");

        CreateAnchoredText("Address / Steam ID", multiplayerPanel, new Vector2(28f, -136f), new Vector2(180f, 26f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        addressInput = CreateInputField(multiplayerPanel, new Vector2(216f, -130f), new Vector2(300f, 38f), "127.0.0.1");

        CreateAnchoredText("Port", multiplayerPanel, new Vector2(28f, -190f), new Vector2(150f, 26f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        int defaultPort = LocalCoopManager.Instance != null ? LocalCoopManager.Instance.defaultPort : 7777;
        portInput = CreateInputField(multiplayerPanel, new Vector2(216f, -184f), new Vector2(140f, 38f), defaultPort.ToString());
        portInput.contentType = InputField.ContentType.IntegerNumber;

        CreatePanelButton("LAN HOST", multiplayerPanel, new Vector2(28f, -250f), new Vector2(154f, 50f), StartHostFromPanel);
        CreatePanelButton("LAN JOIN", multiplayerPanel, new Vector2(202f, -250f), new Vector2(154f, 50f), StartClientFromPanel);
        CreatePanelButton("BACK", multiplayerPanel, new Vector2(376f, -250f), new Vector2(112f, 50f), () => multiplayerPanel.gameObject.SetActive(false));

        CreatePanelButton("STEAM HOST", multiplayerPanel, new Vector2(28f, -322f), new Vector2(206f, 54f), StartSteamHostFromPanel);
        CreatePanelButton("STEAM JOIN", multiplayerPanel, new Vector2(258f, -322f), new Vector2(206f, 54f), StartSteamClientFromPanel);

        CreateAnchoredText("Steam join: paste host Steam ID into Address. LAN join: use IP.", multiplayerPanel, new Vector2(28f, -410f), new Vector2(488f, 48f), 15, FontStyle.Bold, TextAnchor.MiddleCenter);
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
        LocalCoopManager coopManager = LocalCoopManager.Instance;

        if (coopManager == null)
        {
            ShowMessage("Local co-op manager is missing.");
            return;
        }

        bool started = coopManager.StartSteamHost(playerNameInput != null ? playerNameInput.text : "Host");
        ShowMessage(coopManager.StatusText);

        if (started)
        {
            CloseMenu();
        }
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
