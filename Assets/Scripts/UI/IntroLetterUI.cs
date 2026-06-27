using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class IntroLetterUI : MonoBehaviour
{
    private const string PaperResourcePath = "UI/IntroLetterPaper";

    private Canvas canvas;

    public static void Show()
    {
        if (FindAnyObjectByType<IntroLetterUI>() != null)
        {
            return;
        }

        new GameObject("Intro Letter UI").AddComponent<IntroLetterUI>();
    }

    private void Awake()
    {
        BuildCanvas();
        GameUIState.SetIntroLetterOpen(true);
    }

    private void Update()
    {
        if (Keyboard.current != null
            && (Keyboard.current.spaceKey.wasPressedThisFrame
                || Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.eKey.wasPressedThisFrame
                || Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            Close();
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Close();
        }
    }

    private void OnDestroy()
    {
        GameUIState.SetIntroLetterOpen(false);
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Intro Letter Canvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform root = CreateUiObject("Intro Letter Root", canvas.transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Image shade = root.gameObject.AddComponent<Image>();
        shade.color = new Color(0f, 0f, 0f, 0.68f);

        RectTransform paper = CreateUiObject("Shipwreck Letter", root);
        paper.anchorMin = new Vector2(0.5f, 0.5f);
        paper.anchorMax = new Vector2(0.5f, 0.5f);
        paper.pivot = new Vector2(0.5f, 0.5f);
        paper.anchoredPosition = Vector2.zero;
        paper.sizeDelta = new Vector2(840f, 1020f);

        Image paperImage = paper.gameObject.AddComponent<Image>();
        Texture2D paperTexture = Resources.Load<Texture2D>(PaperResourcePath);

        if (paperTexture != null)
        {
            paperImage.sprite = Sprite.Create(
                paperTexture,
                new Rect(0f, 0f, paperTexture.width, paperTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            paperImage.preserveAspect = true;
        }
        else
        {
            paperImage.color = new Color(0.77f, 0.57f, 0.31f, 1f);
        }

        CreateLetterText(paper);
        CreateDismissHint(paper);
        Button closeButton = paper.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = paperImage;
        closeButton.onClick.AddListener(Close);
    }

    private void CreateLetterText(RectTransform paper)
    {
        RectTransform textRect = CreateUiObject("Letter Text", paper);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 58f);
        textRect.sizeDelta = new Vector2(540f, 700f);

        Text text = textRect.gameObject.AddComponent<Text>();
        text.text =
            "If you are reading this,\n"
            + "you survived the wreck.\n\n"
            + "The storm tore the ship apart and the tide dragged you onto this beach.\n\n"
            + "A metal detector was lying beside this note. Use it. Search the sand. Sell what you find. Upgrade what you can.\n\n"
            + "There may be more buried under this island than scrap and old coins.";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 25;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.16f, 0.075f, 0.025f, 1f);
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 1.05f;

        CreateSignature(paper);
    }

    private void CreateSignature(RectTransform paper)
    {
        RectTransform signatureRect = CreateUiObject("Letter Signature", paper);
        signatureRect.anchorMin = new Vector2(0.5f, 0.5f);
        signatureRect.anchorMax = new Vector2(0.5f, 0.5f);
        signatureRect.pivot = new Vector2(0.5f, 0.5f);
        signatureRect.anchoredPosition = new Vector2(120f, -266f);
        signatureRect.sizeDelta = new Vector2(310f, 44f);

        Text signature = signatureRect.gameObject.AddComponent<Text>();
        signature.text = "- unknown survivor";
        signature.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        signature.fontSize = 24;
        signature.fontStyle = FontStyle.Italic;
        signature.alignment = TextAnchor.MiddleRight;
        signature.color = new Color(0.16f, 0.075f, 0.025f, 1f);
        signature.raycastTarget = false;
    }

    private void CreateDismissHint(RectTransform paper)
    {
        RectTransform hintRect = CreateUiObject("Dismiss Hint", paper);
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 148f);
        hintRect.sizeDelta = new Vector2(430f, 42f);

        Text hint = hintRect.gameObject.AddComponent<Text>();
        hint.text = "CLICK E, ENTER OR SPACE";
        hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hint.fontSize = 18;
        hint.fontStyle = FontStyle.Bold;
        hint.alignment = TextAnchor.MiddleCenter;
        hint.color = new Color(0.30f, 0.15f, 0.055f, 0.82f);
        hint.raycastTarget = false;
    }

    private void Close()
    {
        Destroy(gameObject);
    }

    private static RectTransform CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        return gameObject.AddComponent<RectTransform>();
    }
}
