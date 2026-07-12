using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SaveSlotDialogUI : MonoBehaviour
{
    private enum DialogMode
    {
        Save,
        Load,
        Host,
        ExitConfirmation
    }

    private static SaveSlotDialogUI instance;

    private Canvas canvas;
    private RectTransform panel;
    private RectTransform slotContainer;
    private RectTransform exitContainer;
    private Text titleText;
    private Text hintText;
    private readonly Button[] slotButtons = new Button[GameSaveSystem.SaveSlotCount];
    private readonly Text[] slotLabels = new Text[GameSaveSystem.SaveSlotCount];
    private Button hostNewGameButton;
    private Button cancelButton;

    private DialogMode mode;
    private Action<int> slotSelected;
    private Action newGameSelected;
    private Action exitWithoutSaving;
    private Action canceled;

    public static void ShowSave(Action<int> onSlotSelected, Action onCanceled = null)
    {
        EnsureInstance();
        instance.OpenSlotSelection(DialogMode.Save, onSlotSelected, onCanceled);
    }

    public static void ShowLoad(Action<int> onSlotSelected, Action onCanceled = null)
    {
        EnsureInstance();
        instance.OpenSlotSelection(DialogMode.Load, onSlotSelected, onCanceled);
    }

    public static void ShowHost(Action<int> onSlotSelected, Action onNewGameSelected, Action onCanceled = null)
    {
        EnsureInstance();
        instance.OpenSlotSelection(DialogMode.Host, onSlotSelected, onCanceled, onNewGameSelected);
    }

    public static void ShowExitConfirmation(Action onExitWithoutSaving, Action<int> onSaveThenExit, Action onCanceled = null)
    {
        EnsureInstance();
        instance.OpenExitConfirmation(onExitWithoutSaving, onSaveThenExit, onCanceled);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindAnyObjectByType<SaveSlotDialogUI>();

        if (instance == null)
        {
            GameObject dialogObject = new GameObject("Save Slot Dialog UI");
            instance = dialogObject.AddComponent<SaveSlotDialogUI>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildCanvas();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (GameUIState.IsConfirmationOpen)
        {
            GameUIState.SetConfirmationOpen(false);
        }
    }

    private void Update()
    {
        if (canvas == null || !canvas.enabled || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cancel();
            return;
        }

        if (mode == DialogMode.ExitConfirmation)
        {
            if (Keyboard.current.yKey.wasPressedThisFrame)
            {
                OpenSlotSelection(DialogMode.Save, slotSelected, canceled);
            }
            else if (Keyboard.current.nKey.wasPressedThisFrame)
            {
                ExitWithoutSaving();
            }

            return;
        }

        if (mode == DialogMode.Host && Keyboard.current.nKey.wasPressedThisFrame)
        {
            SelectNewGame();
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            SelectSlot(0);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            SelectSlot(1);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            SelectSlot(2);
        }
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Save Slot Dialog Canvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2600;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform overlay = CreateUiObject("Dialog Overlay", canvas.transform);
        StretchToParent(overlay);
        Image overlayImage = overlay.gameObject.AddComponent<Image>();
        overlayImage.color = new Color(0.015f, 0.01f, 0.006f, 0.82f);

        panel = CreateUiObject("Dialog Panel", overlay);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(820f, 690f);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.075f, 0.035f, 0.98f);

        RectTransform innerBorder = CreateUiObject("Inner Border", panel);
        innerBorder.anchorMin = Vector2.zero;
        innerBorder.anchorMax = Vector2.one;
        innerBorder.offsetMin = new Vector2(12f, 12f);
        innerBorder.offsetMax = new Vector2(-12f, -12f);
        Image borderImage = innerBorder.gameObject.AddComponent<Image>();
        borderImage.color = new Color(0.78f, 0.57f, 0.25f, 0.16f);
        borderImage.raycastTarget = false;

        titleText = CreateText("", panel, new Vector2(0f, 280f), new Vector2(740f, 56f), 34, FontStyle.Bold, TextAnchor.MiddleCenter);
        titleText.color = new Color(1f, 0.84f, 0.44f, 1f);
        hintText = CreateText("", panel, new Vector2(0f, 230f), new Vector2(720f, 44f), 18, FontStyle.Normal, TextAnchor.MiddleCenter);
        hintText.color = new Color(0.92f, 0.85f, 0.7f, 1f);

        BuildSlotContainer();
        BuildExitContainer();
    }

    private void BuildSlotContainer()
    {
        slotContainer = CreateUiObject("Save Slots", panel);
        slotContainer.anchorMin = new Vector2(0.5f, 0.5f);
        slotContainer.anchorMax = new Vector2(0.5f, 0.5f);
        slotContainer.pivot = new Vector2(0.5f, 0.5f);
        slotContainer.anchoredPosition = new Vector2(0f, -18f);
        slotContainer.sizeDelta = new Vector2(700f, 430f);

        for (int slotIndex = 0; slotIndex < GameSaveSystem.SaveSlotCount; slotIndex++)
        {
            int capturedSlot = slotIndex;
            RectTransform buttonTransform = CreateUiObject("Save Slot " + (slotIndex + 1), slotContainer);
            buttonTransform.anchorMin = new Vector2(0.5f, 1f);
            buttonTransform.anchorMax = new Vector2(0.5f, 1f);
            buttonTransform.pivot = new Vector2(0.5f, 1f);
            buttonTransform.anchoredPosition = new Vector2(0f, -(slotIndex * 126f));
            buttonTransform.sizeDelta = new Vector2(660f, 108f);

            Image image = buttonTransform.gameObject.AddComponent<Image>();
            image.color = new Color(0.25f, 0.16f, 0.075f, 0.98f);

            Button button = buttonTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => SelectSlot(capturedSlot));
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.87f, 0.58f, 1f);
            colors.pressedColor = new Color(0.82f, 0.66f, 0.36f, 1f);
            colors.disabledColor = new Color(0.48f, 0.45f, 0.4f, 0.72f);
            button.colors = colors;

            Text label = CreateText("", buttonTransform, Vector2.zero, new Vector2(620f, 94f), 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            label.color = new Color(1f, 0.93f, 0.76f, 1f);

            slotButtons[slotIndex] = button;
            slotLabels[slotIndex] = label;
        }

        hostNewGameButton = CreateButton(
            GameLocalization.T("menu.new_game").ToUpperInvariant(),
            slotContainer,
            new Vector2(0f, 175f),
            new Vector2(660f, 72f),
            SelectNewGame);
        hostNewGameButton.gameObject.SetActive(false);

        cancelButton = CreateButton(
            GameLocalization.T("save_slots.cancel"),
            slotContainer,
            new Vector2(0f, -194f),
            new Vector2(240f, 52f),
            Cancel);
    }

    private void BuildExitContainer()
    {
        exitContainer = CreateUiObject("Exit Confirmation", panel);
        exitContainer.anchorMin = new Vector2(0.5f, 0.5f);
        exitContainer.anchorMax = new Vector2(0.5f, 0.5f);
        exitContainer.pivot = new Vector2(0.5f, 0.5f);
        exitContainer.anchoredPosition = new Vector2(0f, -35f);
        exitContainer.sizeDelta = new Vector2(700f, 330f);

        CreateButton(
            GameLocalization.T("save_slots.save"),
            exitContainer,
            new Vector2(0f, 95f),
            new Vector2(520f, 74f),
            () => OpenSlotSelection(DialogMode.Save, slotSelected, canceled));
        CreateButton(
            GameLocalization.T("save_slots.dont_save"),
            exitContainer,
            new Vector2(0f, 5f),
            new Vector2(520f, 74f),
            ExitWithoutSaving);
        CreateButton(
            GameLocalization.T("save_slots.cancel"),
            exitContainer,
            new Vector2(0f, -85f),
            new Vector2(520f, 64f),
            Cancel);
    }

    private void OpenSlotSelection(DialogMode slotMode, Action<int> onSlotSelected, Action onCanceled, Action onNewGameSelected = null)
    {
        mode = slotMode;
        slotSelected = onSlotSelected;
        newGameSelected = onNewGameSelected;
        canceled = onCanceled;
        exitWithoutSaving = null;

        string titleKey = slotMode == DialogMode.Host
            ? "save_slots.choose_host"
            : slotMode == DialogMode.Load
                ? "save_slots.choose_load"
                : "save_slots.choose_save";
        string hintKey = slotMode == DialogMode.Host
            ? "save_slots.host_hint"
            : slotMode == DialogMode.Load
                ? "save_slots.load_hint"
                : "save_slots.save_hint";
        titleText.text = GameLocalization.T(titleKey);
        hintText.text = GameLocalization.T(hintKey);

        slotContainer.gameObject.SetActive(true);
        exitContainer.gameObject.SetActive(false);
        ConfigureSlotLayout(slotMode == DialogMode.Host);
        RefreshSlots();
        SetVisible(true);
    }

    private void ConfigureSlotLayout(bool isHostMode)
    {
        if (hostNewGameButton != null)
        {
            hostNewGameButton.gameObject.SetActive(isHostMode);
        }

        for (int slotIndex = 0; slotIndex < slotButtons.Length; slotIndex++)
        {
            RectTransform slotTransform = slotButtons[slotIndex] != null
                ? slotButtons[slotIndex].GetComponent<RectTransform>()
                : null;

            if (slotTransform == null)
            {
                continue;
            }

            slotTransform.anchoredPosition = isHostMode
                ? new Vector2(0f, -(84f + slotIndex * 100f))
                : new Vector2(0f, -(slotIndex * 126f));
            slotTransform.sizeDelta = isHostMode
                ? new Vector2(660f, 88f)
                : new Vector2(660f, 108f);
        }

        if (cancelButton != null)
        {
            cancelButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -194f);
        }
    }

    private void OpenExitConfirmation(Action onExitWithoutSaving, Action<int> onSaveThenExit, Action onCanceled)
    {
        mode = DialogMode.ExitConfirmation;
        exitWithoutSaving = onExitWithoutSaving;
        slotSelected = onSaveThenExit;
        canceled = onCanceled;

        titleText.text = GameLocalization.T("save_slots.save_before_exit");
        hintText.text = GameLocalization.T("save_slots.exit_hint");
        slotContainer.gameObject.SetActive(false);
        exitContainer.gameObject.SetActive(true);
        SetVisible(true);
    }

    private void RefreshSlots()
    {
        for (int slotIndex = 0; slotIndex < GameSaveSystem.SaveSlotCount; slotIndex++)
        {
            GameSaveSystem.SaveSlotInfo info = GameSaveSystem.GetSaveSlotInfo(slotIndex);
            string slotName = GameLocalization.TFormat("save_slots.slot", slotIndex + 1);

            if (!info.isOccupied)
            {
                slotLabels[slotIndex].text = slotName + "\n" + GameLocalization.T("save_slots.empty");
            }
            else if (!info.isValid)
            {
                slotLabels[slotIndex].text = slotName + "\n" + GameLocalization.T("save_slots.damaged");
            }
            else
            {
                string savedAt = string.IsNullOrWhiteSpace(info.savedAt) ? "—" : info.savedAt;
                slotLabels[slotIndex].text = slotName + "\n"
                    + GameLocalization.TFormat("save_slots.summary", info.dayNumber, info.money, savedAt);
            }

            slotButtons[slotIndex].interactable = mode == DialogMode.Save || (info.isOccupied && info.isValid);
        }
    }

    private void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotButtons.Length || !slotButtons[slotIndex].interactable)
        {
            return;
        }

        Action<int> callback = slotSelected;
        Close();
        callback?.Invoke(slotIndex);
    }

    private void SelectNewGame()
    {
        if (mode != DialogMode.Host)
        {
            return;
        }

        Action callback = newGameSelected;
        Close();
        callback?.Invoke();
    }

    private void ExitWithoutSaving()
    {
        Action callback = exitWithoutSaving;
        Close();
        callback?.Invoke();
    }

    private void Cancel()
    {
        Action callback = canceled;
        Close();
        callback?.Invoke();
    }

    private void Close()
    {
        slotSelected = null;
        newGameSelected = null;
        exitWithoutSaving = null;
        canceled = null;
        SetVisible(false);
        Destroy(gameObject);
    }

    private void SetVisible(bool visible)
    {
        if (canvas != null)
        {
            canvas.enabled = visible;
        }

        GameUIState.SetConfirmationOpen(visible);
    }

    private Button CreateButton(string label, RectTransform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        RectTransform buttonTransform = CreateUiObject(label + " Button", parent);
        buttonTransform.anchorMin = new Vector2(0.5f, 0.5f);
        buttonTransform.anchorMax = new Vector2(0.5f, 0.5f);
        buttonTransform.pivot = new Vector2(0.5f, 0.5f);
        buttonTransform.anchoredPosition = position;
        buttonTransform.sizeDelta = size;

        Image image = buttonTransform.gameObject.AddComponent<Image>();
        image.color = new Color(0.36f, 0.22f, 0.085f, 1f);

        Button button = buttonTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text text = CreateText(label, buttonTransform, Vector2.zero, size, 21, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.color = new Color(1f, 0.91f, 0.68f, 1f);
        return button;
    }

    private Text CreateText(string value, RectTransform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        RectTransform textTransform = CreateUiObject("Text", parent);
        textTransform.anchorMin = new Vector2(0.5f, 0.5f);
        textTransform.anchorMax = new Vector2(0.5f, 0.5f);
        textTransform.pivot = new Vector2(0.5f, 0.5f);
        textTransform.anchoredPosition = anchoredPosition;
        textTransform.sizeDelta = size;

        Text text = textTransform.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName);
        uiObject.transform.SetParent(parent, false);
        return uiObject.AddComponent<RectTransform>();
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
