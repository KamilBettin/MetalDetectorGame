using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    private static LoadingScreenUI instance;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Text titleText;
    private Text hintText;
    private float hideAtTime;
    private float visibleUntilTime;
    private bool hideRequested;
    private float dotTimer;
    private int dotCount;
    private string baseMessage = "Loading";

    public static LoadingScreenUI Instance
    {
        get
        {
            EnsureExists();
            return instance;
        }
    }

    public static void EnsureExists()
    {
        if (instance != null)
        {
            instance.HideImmediate();
            return;
        }

        instance = FindAnyObjectByType<LoadingScreenUI>();

        if (instance != null)
        {
            instance.HideImmediate();
            return;
        }

        GameObject loadingObject = new GameObject("Loading Screen UI");
        instance = loadingObject.AddComponent<LoadingScreenUI>();
    }

    public static void Show(string message = "Loading island", float minimumVisibleSeconds = 0.75f)
    {
        Instance.ShowInternal(message, minimumVisibleSeconds);
    }

    public static void HideAfterMinimum(float extraDelaySeconds = 0.15f)
    {
        if (instance == null)
        {
            return;
        }

        instance.hideAtTime = Mathf.Max(instance.visibleUntilTime, Time.unscaledTime + Mathf.Max(0f, extraDelaySeconds));
        instance.hideRequested = true;
    }

    public static void ForceHide()
    {
        if (instance == null)
        {
            instance = FindAnyObjectByType<LoadingScreenUI>();
        }

        if (instance == null)
        {
            return;
        }

        instance.HideImmediate();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildCanvas();
        HideImmediate();
    }

    private void Update()
    {
        if (canvas == null || !canvas.enabled)
        {
            return;
        }

        dotTimer += Time.unscaledDeltaTime;

        if (dotTimer >= 0.28f)
        {
            dotTimer = 0f;
            dotCount = (dotCount + 1) % 4;
            titleText.text = baseMessage + new string('.', dotCount);
        }

        if (hideRequested && Time.unscaledTime >= hideAtTime)
        {
            HideImmediate();
        }
    }

    private void ShowInternal(string message, float minimumVisibleSeconds)
    {
        if (canvas == null)
        {
            BuildCanvas();
        }

        baseMessage = string.IsNullOrWhiteSpace(message) ? "Loading" : message;
        dotCount = 0;
        dotTimer = 0f;
        visibleUntilTime = Time.unscaledTime + Mathf.Max(0f, minimumVisibleSeconds);
        hideRequested = false;

        titleText.text = baseMessage;
        hintText.text = "Please wait";
        canvas.enabled = true;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void HideImmediate()
    {
        if (canvas == null)
        {
            return;
        }

        canvas.enabled = false;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        hideRequested = false;
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Loading Screen Canvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        RectTransform root = CreateUiObject("Loading Screen Root", canvas.transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.025f, 0.025f, 0.03f, 1f);
        background.raycastTarget = false;

        RectTransform title = CreateUiObject("Loading Title", root);
        title.anchorMin = new Vector2(0.5f, 0.5f);
        title.anchorMax = new Vector2(0.5f, 0.5f);
        title.pivot = new Vector2(0.5f, 0.5f);
        title.anchoredPosition = new Vector2(0f, 34f);
        title.sizeDelta = new Vector2(820f, 86f);
        titleText = CreateText(title, 44, FontStyle.Bold, new Color(1f, 0.83f, 0.34f, 1f));

        RectTransform hint = CreateUiObject("Loading Hint", root);
        hint.anchorMin = new Vector2(0.5f, 0.5f);
        hint.anchorMax = new Vector2(0.5f, 0.5f);
        hint.pivot = new Vector2(0.5f, 0.5f);
        hint.anchoredPosition = new Vector2(0f, -32f);
        hint.sizeDelta = new Vector2(760f, 42f);
        hintText = CreateText(hint, 20, FontStyle.Normal, new Color(0.86f, 0.88f, 0.86f, 1f));
    }

    private static Text CreateText(RectTransform parent, int fontSize, FontStyle fontStyle, Color color)
    {
        Text text = parent.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rectTransform = uiObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }
}
