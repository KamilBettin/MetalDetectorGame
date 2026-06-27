using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    private const string BackgroundResourcePath = "UI/PauseMenuBackground";
    private const string SavePrefix = "MetalDetector.Save.";

    private readonly string[] menuItems =
    {
        "Resume",
        "Invite",
        "Save",
        "Settings",
        "Quit",
        "Main Menu"
    };

    private Canvas canvas;
    private RectTransform menuRoot;
    private RectTransform selectionFrame;
    private Text messageText;
    private int selectedIndex;
    private float messageTimer;
    private bool isOpen;

    private void Awake()
    {
        BuildCanvas();
        SetOpen(false);
    }

    private void OnDestroy()
    {
        if (GameUIState.IsPauseMenuOpen)
        {
            GameUIState.SetPauseMenuOpen(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameUIState.IsStartMenuOpen)
            {
                return;
            }

            if (isOpen)
            {
                ResumeGame();
                return;
            }

            if (!GameUIState.IsInventoryOpen && !GameUIState.IsTraderMenuOpen && !GameUIState.IsHomeMenuOpen)
            {
                SetOpen(true);
            }
        }

        if (!isOpen)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        HandleKeyboardInput();
        UpdateMessage();
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Pause Menu Canvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        menuRoot = CreateUiObject("Pause Menu Root", canvas.transform);
        menuRoot.anchorMin = Vector2.zero;
        menuRoot.anchorMax = Vector2.one;
        menuRoot.offsetMin = Vector2.zero;
        menuRoot.offsetMax = Vector2.zero;

        CreateBackground(menuRoot);
        CreateMenuButtons(menuRoot);
        CreateSelectionFrame(menuRoot);
        CreateMessageText(menuRoot);
    }

    private void CreateBackground(RectTransform parent)
    {
        Texture2D backgroundTexture = Resources.Load<Texture2D>(BackgroundResourcePath);
        Image background = CreateUiObject("Pause Menu Background", parent).gameObject.AddComponent<Image>();
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = Vector2.zero;
        background.rectTransform.offsetMax = Vector2.zero;
        background.raycastTarget = false;

        if (backgroundTexture == null)
        {
            background.color = new Color(0f, 0f, 0f, 0.82f);
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
        Vector2[] positions =
        {
            new Vector2(700f, -302f),
            new Vector2(700f, -414f),
            new Vector2(700f, -520f),
            new Vector2(700f, -627f),
            new Vector2(700f, -733f),
            new Vector2(738f, -848f)
        };

        Vector2[] sizes =
        {
            new Vector2(510f, 94f),
            new Vector2(510f, 88f),
            new Vector2(510f, 88f),
            new Vector2(510f, 88f),
            new Vector2(510f, 88f),
            new Vector2(430f, 70f)
        };

        for (int i = 0; i < menuItems.Length; i++)
        {
            int itemIndex = i;
            RectTransform buttonTransform = CreateUiObject(menuItems[i] + " Click Area", parent);
            buttonTransform.anchorMin = new Vector2(0f, 1f);
            buttonTransform.anchorMax = new Vector2(0f, 1f);
            buttonTransform.pivot = new Vector2(0f, 1f);
            buttonTransform.anchoredPosition = positions[i];
            buttonTransform.sizeDelta = sizes[i];

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
        selectionFrame = CreateUiObject("Pause Selected Frame", parent);
        selectionFrame.anchorMin = new Vector2(0f, 1f);
        selectionFrame.anchorMax = new Vector2(0f, 1f);
        selectionFrame.pivot = new Vector2(0f, 1f);

        Image frameImage = selectionFrame.gameObject.AddComponent<Image>();
        frameImage.color = new Color(1f, 0.78f, 0.24f, 0.16f);
        frameImage.raycastTarget = false;
        SetSelected(0);
    }

    private void CreateMessageText(RectTransform parent)
    {
        RectTransform panel = CreateUiObject("Pause Menu Message", parent);
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 96f);
        panel.sizeDelta = new Vector2(780f, 42f);

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.04f, 0.025f, 0.76f);
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
    }

    private void ActivateItem(int itemIndex)
    {
        SetSelected(itemIndex);

        switch (itemIndex)
        {
            case 0:
                ResumeGame();
                break;
            case 1:
                ShowInviteInfo();
                break;
            case 2:
                SaveGame();
                break;
            case 3:
                CharacterSelectionUI.Open();
                ShowMessage("Choose your co-op character.");
                break;
            case 4:
                QuitGame();
                break;
            case 5:
                ReturnToMainMenu();
                break;
        }
    }

    private void ResumeGame()
    {
        SetOpen(false);
    }

    private void ShowInviteInfo()
    {
        LocalCoopManager coopManager = LocalCoopManager.Instance;

        if (coopManager != null && coopManager.IsRunning)
        {
            ShowMessage("Invite: share your Steam ID/status with a friend from the multiplayer panel.");
            return;
        }

        ShowMessage("Start Steam Host from Multiplayer, then invite a friend with your Steam ID.");
    }

    private void SaveGame()
    {
        Transform player = FindLocalPlayer();
        PlayerInventory inventory = player != null ? player.GetComponent<PlayerInventory>() : FindAnyObjectByType<PlayerInventory>();

        if (player != null)
        {
            PlayerPrefs.SetFloat(SavePrefix + "PlayerX", player.position.x);
            PlayerPrefs.SetFloat(SavePrefix + "PlayerY", player.position.y);
            PlayerPrefs.SetFloat(SavePrefix + "PlayerZ", player.position.z);
            PlayerPrefs.SetFloat(SavePrefix + "PlayerRotY", player.eulerAngles.y);
        }

        if (inventory != null)
        {
            PlayerPrefs.SetInt(SavePrefix + "Money", inventory.money);
            PlayerPrefs.SetInt(SavePrefix + "GridSize", inventory.gridSize);
        }

        PlayerPrefs.SetString(SavePrefix + "SavedAt", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        PlayerPrefs.Save();
        ShowMessage("Game saved.");
    }

    private void ReturnToMainMenu()
    {
        LocalCoopManager.Instance?.StopSession();
        SetOpen(false);

        if (FindAnyObjectByType<StartMenuUI>() == null)
        {
            StartMenuUI.AllowShowingAgain();
            new GameObject("Start Menu UI").AddComponent<StartMenuUI>();
        }
    }

    private void QuitGame()
    {
        SaveGame();
        Application.Quit();

#if UNITY_EDITOR
        ShowMessage("Quit works in a built game. In Editor, the game stays open.");
#endif
    }

    private Transform FindLocalPlayer()
    {
        FirstPersonController controller = FindAnyObjectByType<FirstPersonController>();
        return controller != null ? controller.transform : null;
    }

    private void SetOpen(bool open)
    {
        isOpen = open;
        menuRoot.gameObject.SetActive(isOpen);
        GameUIState.SetPauseMenuOpen(isOpen);
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;

        if (isOpen)
        {
            SetSelected(0);
        }
    }

    private void SetSelected(int itemIndex)
    {
        selectedIndex = Mathf.Clamp(itemIndex, 0, menuItems.Length - 1);

        if (selectionFrame == null)
        {
            return;
        }

        Vector2[] positions =
        {
            new Vector2(700f, -302f),
            new Vector2(700f, -414f),
            new Vector2(700f, -520f),
            new Vector2(700f, -627f),
            new Vector2(700f, -733f),
            new Vector2(738f, -848f)
        };

        Vector2[] sizes =
        {
            new Vector2(510f, 94f),
            new Vector2(510f, 88f),
            new Vector2(510f, 88f),
            new Vector2(510f, 88f),
            new Vector2(510f, 88f),
            new Vector2(430f, 70f)
        };

        selectionFrame.anchoredPosition = positions[selectedIndex];
        selectionFrame.sizeDelta = sizes[selectedIndex];
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

    private RectTransform CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        return gameObject.AddComponent<RectTransform>();
    }
}
