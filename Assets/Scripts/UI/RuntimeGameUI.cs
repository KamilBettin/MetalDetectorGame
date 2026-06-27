using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeGameUI : MonoBehaviour
{
    private enum RewardTier
    {
        Info,
        Trash,
        Okay,
        Valuable,
        Jackpot
    }

    private enum ShopTab
    {
        Sell,
        Upgrades
    }

    public static bool IsActive { get; private set; }

    private Canvas canvas;
    private MetalDetector metalDetector;
    private DetectorBattery detectorBattery;
    private PlayerInventory inventory;
    private UpgradeShop shop;
    private TreasureMap treasureMap;
    private TutorialQuestSystem questSystem;
    private DiggingController diggingController;

    private Text signalText;
    private Image signalFill;
    private Text batteryText;
    private Image batteryFill;
    private Text moneyText;
    private Text cargoText;
    private Image cargoFill;
    private Text questText;
    private Image questFill;
    private Text hintText;
    private RectTransform worldPromptPanel;
    private Text worldPromptKeyText;
    private Text worldPromptActionText;
    private CanvasGroup worldPromptCanvasGroup;
    private RectTransform foundToastPanel;
    private CanvasGroup foundToastCanvasGroup;
    private Image foundToastIcon;
    private Text foundToastTitleText;
    private Text foundToastText;
    private Text foundToastValueText;
    private readonly List<RectTransform> foundToastSparkles = new List<RectTransform>();
    private string currentFoundToastMessage = "";
    private int currentFoundToastValue = -1;
    private bool foundToastShouldShow;
    private bool foundToastIsReward;
    private RewardTier foundToastTier = RewardTier.Info;
    private Color foundToastAccentColor = Color.white;
    private float foundToastAnimationTimer;
    private Vector2 worldPromptTargetPosition;
    private bool worldPromptVisible;
    private bool worldPromptHasPosition;
    private float worldPromptBobTimer;
    private Text inventoryTitleText;
    private Text shopMessageText;
    private Text mapTitleText;

    private RectTransform inventoryPanel;
    private RectTransform inventoryGrid;
    private RectTransform shopPanel;
    private RectTransform shopContent;
    private RectTransform shopDropZone;
    private Text shopTitleText;
    private Text shopDropText;
    private Image shopDropImage;
    private RectTransform shopDragGhost;
    private RectTransform mapPanel;
    private RectTransform mapContent;

    private readonly List<GameObject> inventoryCells = new List<GameObject>();
    private readonly List<GameObject> shopContentObjects = new List<GameObject>();
    private readonly List<GameObject> mapDots = new List<GameObject>();
    private readonly Dictionary<string, Sprite> iconSprites = new Dictionary<string, Sprite>();
    private ShopTab activeShopTab = ShopTab.Sell;
    private ShopDragItem activeShopDragItem;
    private bool shopDirty = true;
    private int lastShopItemCount = -1;
    private int lastShopMoney = -1;
    private float shopPulseTimer;
    private float refreshTimer;

    private void Awake()
    {
        IsActive = true;
        FindReferences();
        BuildCanvas();
    }

    private void OnDestroy()
    {
        IsActive = false;
    }

    private void Update()
    {
        FindReferences();
        AnimateWorldPrompt();
        AnimateFoundToast();
        refreshTimer -= Time.unscaledDeltaTime;

        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.12f;
            Refresh();
        }
    }

    private void FindReferences()
    {
        if (metalDetector == null)
        {
            metalDetector = FindAnyObjectByType<MetalDetector>();
        }

        if (detectorBattery == null)
        {
            detectorBattery = FindAnyObjectByType<DetectorBattery>();
        }

        if (inventory == null)
        {
            inventory = FindAnyObjectByType<PlayerInventory>();
        }

        if (diggingController == null)
        {
            diggingController = FindAnyObjectByType<DiggingController>();
        }

        if (shop == null)
        {
            shop = FindBestShop();
        }

        if (treasureMap == null)
        {
            treasureMap = FindAnyObjectByType<TreasureMap>();
        }

        if (questSystem == null)
        {
            questSystem = FindAnyObjectByType<TutorialQuestSystem>();
        }
    }

    private UpgradeShop FindBestShop()
    {
        UpgradeShop[] shops = FindObjectsByType<UpgradeShop>();

        foreach (UpgradeShop candidate in shops)
        {
            if (candidate != null && candidate.shopNpc != null)
            {
                return candidate;
            }
        }

        return shops.Length > 0 ? shops[0] : null;
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Runtime Canvas UI");
        canvasObject.transform.SetParent(transform);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        BuildHud();
        BuildInventory();
        BuildShop();
        BuildMap();
    }

    private void BuildHud()
    {
        RectTransform detectorPanel = CreatePanel("Detector Panel", new Vector2(20f, -20f), new Vector2(340f, 150f), new Vector2(0f, 1f), canvas.transform);
        CreateText("Detector", detectorPanel, new Vector2(16f, -12f), new Vector2(300f, 24f), 22, FontStyle.Bold, TextAnchor.MiddleLeft);
        signalText = CreateText("Signal 0%", detectorPanel, new Vector2(16f, -48f), new Vector2(300f, 22f), 16, FontStyle.Normal, TextAnchor.MiddleLeft);
        signalFill = CreateBar(detectorPanel, new Vector2(16f, -78f), new Vector2(300f, 18f), new Color(1f, 0.76f, 0.2f, 1f));
        batteryText = CreateText("Battery 100%", detectorPanel, new Vector2(16f, -104f), new Vector2(300f, 22f), 14, FontStyle.Normal, TextAnchor.MiddleLeft);
        batteryFill = CreateBar(detectorPanel, new Vector2(16f, -130f), new Vector2(300f, 12f), new Color(0.35f, 0.92f, 0.62f, 1f));

        RectTransform statusPanel = CreatePanel("Status Panel", new Vector2(-20f, -20f), new Vector2(270f, 104f), new Vector2(1f, 1f), canvas.transform);
        moneyText = CreateText("$0 cash", statusPanel, new Vector2(14f, -12f), new Vector2(238f, 24f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        cargoText = CreateText("Cargo $0", statusPanel, new Vector2(14f, -42f), new Vector2(238f, 20f), 14, FontStyle.Normal, TextAnchor.MiddleLeft);
        cargoFill = CreateBar(statusPanel, new Vector2(14f, -72f), new Vector2(238f, 14f), new Color(1f, 0.76f, 0.2f, 1f));

        RectTransform questPanel = CreatePanel("Quest Panel", new Vector2(20f, -190f), new Vector2(380f, 92f), new Vector2(0f, 1f), canvas.transform);
        CreateText("Tutorial", questPanel, new Vector2(14f, -10f), new Vector2(340f, 22f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        questText = CreateText("", questPanel, new Vector2(14f, -38f), new Vector2(340f, 20f), 14, FontStyle.Normal, TextAnchor.MiddleLeft);
        questFill = CreateBar(questPanel, new Vector2(14f, -66f), new Vector2(340f, 12f), new Color(0.18f, 0.72f, 1f, 1f));

        hintText = CreateText("Hold LMB scan | E dig/talk | TAB backpack | M map", canvas.transform as RectTransform, new Vector2(0f, 34f), new Vector2(620f, 38f), 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        hintText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        hintText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        hintText.rectTransform.pivot = new Vector2(0.5f, 0f);

        foundToastPanel = CreatePanel("Found Treasure Toast", new Vector2(0f, -185f), new Vector2(520f, 150f), new Vector2(0.5f, 0.5f), canvas.transform);
        foundToastPanel.pivot = new Vector2(0.5f, 0.5f);
        foundToastPanel.GetComponent<Image>().color = new Color(0.045f, 0.038f, 0.028f, 0.94f);
        foundToastCanvasGroup = foundToastPanel.gameObject.AddComponent<CanvasGroup>();
        foundToastCanvasGroup.alpha = 0f;
        foundToastIcon = CreateSpriteIcon(foundToastPanel, new Vector2(24f, -24f), new Vector2(104f, 104f), null, true);
        foundToastTitleText = CreateText("", foundToastPanel, new Vector2(150f, -22f), new Vector2(340f, 30f), 24, FontStyle.Bold, TextAnchor.MiddleLeft);
        foundToastText = CreateText("", foundToastPanel, new Vector2(150f, -58f), new Vector2(340f, 34f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        foundToastValueText = CreateText("", foundToastPanel, new Vector2(150f, -96f), new Vector2(340f, 28f), 16, FontStyle.Normal, TextAnchor.MiddleLeft);
        BuildFoundToastSparkles();
        foundToastPanel.gameObject.SetActive(false);

        worldPromptPanel = CreatePanel("World Interaction Prompt", Vector2.zero, new Vector2(220f, 54f), new Vector2(0.5f, 0.5f), canvas.transform);
        worldPromptPanel.pivot = new Vector2(0.5f, 0.5f);
        worldPromptCanvasGroup = worldPromptPanel.gameObject.AddComponent<CanvasGroup>();
        worldPromptCanvasGroup.alpha = 0f;
        Image promptImage = worldPromptPanel.GetComponent<Image>();
        promptImage.color = new Color(0.03f, 0.035f, 0.03f, 0.88f);
        RectTransform keyBox = CreatePanel("World Prompt Key", new Vector2(12f, -8f), new Vector2(44f, 38f), new Vector2(0f, 1f), worldPromptPanel);
        keyBox.GetComponent<Image>().color = new Color(1f, 0.76f, 0.2f, 0.95f);
        worldPromptKeyText = CreateText("E", keyBox, Vector2.zero, new Vector2(44f, 38f), 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        worldPromptKeyText.color = new Color(0.06f, 0.045f, 0.02f, 1f);
        worldPromptActionText = CreateText("Use", worldPromptPanel, new Vector2(64f, -11f), new Vector2(142f, 32f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        worldPromptPanel.gameObject.SetActive(false);
    }

    private void BuildInventory()
    {
        inventoryPanel = CreatePanel("Inventory Panel", new Vector2(-20f, -140f), new Vector2(420f, 520f), new Vector2(1f, 1f), canvas.transform);
        inventoryTitleText = CreateText("Backpack", inventoryPanel, new Vector2(18f, -14f), new Vector2(360f, 26f), 22, FontStyle.Bold, TextAnchor.MiddleLeft);
        inventoryGrid = CreatePanel("Inventory Grid", new Vector2(18f, -82f), new Vector2(370f, 390f), new Vector2(0f, 1f), inventoryPanel);
        inventoryGrid.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        inventoryPanel.gameObject.SetActive(false);
    }

    private void BuildShop()
    {
        shopPanel = CreatePanel("Trader Panel", Vector2.zero, new Vector2(760f, 520f), new Vector2(0.5f, 0.5f), canvas.transform);
        shopTitleText = CreateText("Trader", shopPanel, new Vector2(24f, -18f), new Vector2(480f, 34f), 28, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateIconBadge(shopPanel, new Vector2(676f, -18f), new Vector2(54f, 34f), "coin", new Color(1f, 0.76f, 0.2f, 1f));

        RectTransform tabsPanel = CreatePanel("Trader Tabs", new Vector2(24f, -70f), new Vector2(170f, 374f), new Vector2(0f, 1f), shopPanel);
        tabsPanel.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.04f, 0.84f);
        CreateShopTabButton(tabsPanel, "sell", "Sell Items", new Vector2(12f, -14f), ShopTab.Sell, new Color(0.35f, 0.92f, 0.62f, 1f));
        CreateShopTabButton(tabsPanel, "upgrade", "Upgrades", new Vector2(12f, -76f), ShopTab.Upgrades, new Color(0.18f, 0.72f, 1f, 1f));

        shopContent = CreatePanel("Trader Content", new Vector2(212f, -70f), new Vector2(524f, 374f), new Vector2(0f, 1f), shopPanel);
        shopContent.GetComponent<Image>().color = new Color(0.03f, 0.035f, 0.03f, 0.78f);

        shopMessageText = CreateText("", shopPanel, new Vector2(24f, -464f), new Vector2(712f, 34f), 16, FontStyle.Bold, TextAnchor.MiddleCenter);
        shopPanel.gameObject.SetActive(false);
    }

    private void BuildMap()
    {
        mapPanel = CreatePanel("Map Panel", new Vector2(20f, -300f), new Vector2(420f, 420f), new Vector2(0f, 1f), canvas.transform);
        mapTitleText = CreateText("Search Map", mapPanel, new Vector2(16f, -12f), new Vector2(360f, 24f), 20, FontStyle.Bold, TextAnchor.MiddleLeft);
        mapContent = CreatePanel("Map Content", new Vector2(18f, -50f), new Vector2(384f, 330f), new Vector2(0f, 1f), mapPanel);
        mapPanel.gameObject.SetActive(false);
    }

    private void Refresh()
    {
        RefreshHud();
        RefreshInventory();
        RefreshShop();
        RefreshMap();
    }

    private void RefreshHud()
    {
        float signal = metalDetector != null ? metalDetector.CurrentSignal : 0f;
        if (metalDetector != null && metalDetector.NearestTreasure != null)
        {
            string signalState = metalDetector.RevealSignalActive
                ? "target marked"
                : metalDetector.CurrentSignalCellDistance <= 0 ? "scan now" : metalDetector.CurrentSignalCellDistance + " cells";
            signalText.text = "Signal " + Mathf.RoundToInt(signal * 100f) + "% | " + signalState;
        }
        else
        {
            signalText.text = "Signal 0% | " + (metalDetector != null ? metalDetector.CurrentDetectorName + " " + metalDetector.CurrentScanCells + "x" + metalDetector.CurrentScanCells : "No detector");
        }

        signalFill.fillAmount = signal;

        float battery = detectorBattery != null ? detectorBattery.Charge01 : 1f;
        batteryText.text = "Battery " + Mathf.RoundToInt(battery * 100f) + "%";
        batteryFill.fillAmount = battery;
        batteryFill.color = battery < 0.2f ? new Color(1f, 0.35f, 0.28f, 1f) : new Color(0.35f, 0.92f, 0.62f, 1f);

        if (inventory != null)
        {
            moneyText.text = "$" + inventory.money + " cash";
            cargoText.text = "Cargo $" + inventory.GetInventoryValue() + " | " + inventory.OccupiedSlots + "/" + inventory.Capacity;
            cargoFill.fillAmount = inventory.OccupiedSlots / (float)inventory.Capacity;
        }

        if (questSystem != null)
        {
            questText.text = questSystem.CurrentObjective;
            questFill.fillAmount = questSystem.Progress01;
        }

        RefreshFoundToast();
        RefreshInteractionPrompt();
    }

    private void RefreshFoundToast()
    {
        if (foundToastPanel == null || foundToastCanvasGroup == null || diggingController == null)
        {
            return;
        }

        foundToastShouldShow = diggingController.MessageTimer > 0f && !GameUIState.AnyMenuOpen;

        if (!foundToastShouldShow && foundToastCanvasGroup.alpha <= 0.01f)
        {
            foundToastPanel.gameObject.SetActive(false);
            return;
        }

        if (currentFoundToastMessage != diggingController.LastFoundMessage
            || foundToastIsReward != diggingController.LastFoundWasTreasure
            || currentFoundToastValue != diggingController.LastFoundValue)
        {
            currentFoundToastMessage = diggingController.LastFoundMessage;
            currentFoundToastValue = diggingController.LastFoundValue;
            foundToastAnimationTimer = 0f;
        }

        foundToastIsReward = diggingController.LastFoundWasTreasure;
        foundToastTier = foundToastIsReward ? GetRewardTier(diggingController.LastFoundValue) : RewardTier.Info;
        foundToastAccentColor = GetRewardAccentColor(foundToastTier);
        foundToastPanel.GetComponent<Image>().color = GetRewardPanelColor(foundToastTier);
        ApplyFoundToastLayout(foundToastTier);
        foundToastTitleText.text = GetRewardTitle(foundToastTier);
        foundToastTitleText.color = foundToastAccentColor;
        foundToastText.text = foundToastIsReward ? GetRewardSubtitle(foundToastTier) : diggingController.LastFoundMessage;
        foundToastValueText.text = foundToastIsReward
            ? diggingController.LastFoundItemName + "  $" + diggingController.LastFoundValue
            : "";
        foundToastValueText.gameObject.SetActive(foundToastIsReward);
        foundToastValueText.color = foundToastIsReward ? Color.Lerp(foundToastAccentColor, Color.white, 0.35f) : Color.white;
        foundToastIcon.sprite = diggingController.LastFoundIcon != null ? diggingController.LastFoundIcon : GetIconSprite("treasure");
        foundToastIcon.preserveAspect = true;
    }

    private void AnimateFoundToast()
    {
        if (foundToastPanel == null || foundToastCanvasGroup == null)
        {
            return;
        }

        float targetAlpha = foundToastShouldShow ? 1f : 0f;
        foundToastCanvasGroup.alpha = Mathf.MoveTowards(foundToastCanvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 7f);

        if (!foundToastShouldShow && foundToastCanvasGroup.alpha <= 0.01f)
        {
            foundToastPanel.gameObject.SetActive(false);
            return;
        }

        foundToastPanel.gameObject.SetActive(true);
        foundToastAnimationTimer += Time.unscaledDeltaTime;

        float appearDuration = foundToastTier == RewardTier.Jackpot ? 0.38f : foundToastTier == RewardTier.Trash ? 0.18f : 0.24f;
        float appear = Mathf.Clamp01(foundToastAnimationTimer / appearDuration);
        float easedAppear = 1f - Mathf.Pow(1f - appear, 3f);
        float popStrength = GetRewardPopStrength(foundToastTier);
        float rewardPop = foundToastIsReward ? Mathf.Sin(Mathf.Clamp01(foundToastAnimationTimer / 0.34f) * Mathf.PI) * popStrength : 0f;
        float idlePulse = foundToastTier == RewardTier.Jackpot ? Mathf.Sin(foundToastAnimationTimer * 8f) * 0.025f : 0f;
        float startScale = foundToastTier == RewardTier.Trash ? 0.94f : foundToastTier == RewardTier.Jackpot ? 0.72f : 0.84f;
        float targetScale = foundToastIsReward ? Mathf.Lerp(startScale, 1f, easedAppear) + rewardPop + idlePulse : Mathf.Lerp(0.96f, 1f, easedAppear);
        foundToastPanel.localScale = Vector3.one * targetScale;

        for (int i = 0; i < foundToastSparkles.Count; i++)
        {
            RectTransform sparkle = foundToastSparkles[i];
            Image sparkleImage = sparkle.GetComponent<Image>();
            bool showSparkle = foundToastIsReward && foundToastShouldShow && i < GetRewardSparkleCount(foundToastTier);
            sparkle.gameObject.SetActive(showSparkle);

            if (!showSparkle)
            {
                continue;
            }

            float sparkleCount = Mathf.Max(1, GetRewardSparkleCount(foundToastTier));
            float spinSpeed = foundToastTier == RewardTier.Jackpot ? 5.2f : foundToastTier == RewardTier.Valuable ? 3.8f : 2.2f;
            float angle = i / sparkleCount * Mathf.PI * 2f + foundToastAnimationTimer * spinSpeed;
            Vector2 sparkleCenter = GetFoundToastIconCenter();
            float baseRadius = foundToastTier == RewardTier.Jackpot ? 112f : foundToastTier == RewardTier.Valuable ? 88f : 58f;
            float radius = baseRadius + Mathf.Sin(foundToastAnimationTimer * 5f + i) * 7f;
            sparkle.anchoredPosition = new Vector2(sparkleCenter.x + Mathf.Cos(angle) * radius, sparkleCenter.y + Mathf.Sin(angle) * radius);
            sparkle.localScale = Vector3.one * GetRewardSparkleScale(foundToastTier);
            sparkle.localRotation = Quaternion.Euler(0f, 0f, foundToastAnimationTimer * 190f + i * 33f);
            float alpha = Mathf.Clamp01(Mathf.Sin(foundToastAnimationTimer * 7f + i * 0.7f) * 0.45f + 0.55f);
            sparkleImage.color = new Color(foundToastAccentColor.r, foundToastAccentColor.g, foundToastAccentColor.b, alpha);
        }
    }

    private void RefreshInteractionPrompt()
    {
        if (GameUIState.IsTraderMenuOpen)
        {
            hintText.text = "ESC - Close trader";
            HideWorldPrompt();
            return;
        }

        if (GameUIState.IsInventoryOpen)
        {
            hintText.text = "TAB / ESC - Close backpack";
            HideWorldPrompt();
            return;
        }

        if (GameUIState.IsHomeMenuOpen)
        {
            hintText.text = "ESC - Close home";
            HideWorldPrompt();
            return;
        }

        PlayerHome home = PlayerHome.FindClosestHomeInRange();

        if (home != null)
        {
            hintText.text = "E - Use home";
            ShowWorldPrompt("Use Home", home.PromptPosition, new Color(0.35f, 0.92f, 0.62f, 0.95f));
            return;
        }

        if (DayNightCycle.IsNightNow)
        {
            hintText.text = "Night - searching disabled. Sleep at home.";
            HideWorldPrompt();
            return;
        }

        if (shop != null && shop.IsPlayerNearShop())
        {
            hintText.text = "E - Talk / Use";
            ShowWorldPrompt("Talk / Use", shop.shopNpc.position + Vector3.up * 1.35f, new Color(1f, 0.76f, 0.2f, 0.95f));
            return;
        }

        DetectableTreasure digTarget = diggingController != null ? diggingController.FindClosestTreasureInRange() : null;

        if (digTarget != null)
        {
            hintText.text = "E - Dig";
            Vector3 promptPosition = digTarget.revealMarker != null
                ? digTarget.revealMarker.transform.position + Vector3.up * 0.75f
                : digTarget.transform.position + Vector3.up * 0.75f;
            ShowWorldPrompt("Dig", promptPosition, new Color(0.25f, 0.88f, 1f, 0.95f));
            return;
        }

        hintText.text = "Hold LMB - Scan sand | TAB backpack | M map";
        HideWorldPrompt();
    }

    private void ShowWorldPrompt(string actionText, Vector3 worldPosition, Color keyColor)
    {
        Camera camera = Camera.main;

        if (camera == null || worldPromptPanel == null)
        {
            return;
        }

        Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);

        if (screenPosition.z <= 0f)
        {
            HideWorldPrompt();
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 localPosition))
        {
            HideWorldPrompt();
            return;
        }

        worldPromptTargetPosition = localPosition;
        worldPromptVisible = true;

        if (!worldPromptHasPosition)
        {
            worldPromptPanel.anchoredPosition = localPosition;
            worldPromptHasPosition = true;
        }

        worldPromptActionText.text = actionText;
        worldPromptKeyText.transform.parent.GetComponent<Image>().color = keyColor;
        worldPromptPanel.gameObject.SetActive(true);
    }

    private void HideWorldPrompt()
    {
        worldPromptVisible = false;
    }

    private void AnimateWorldPrompt()
    {
        if (worldPromptPanel == null || worldPromptCanvasGroup == null)
        {
            return;
        }

        float targetAlpha = worldPromptVisible ? 1f : 0f;
        worldPromptCanvasGroup.alpha = Mathf.MoveTowards(worldPromptCanvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 8f);

        if (!worldPromptVisible && worldPromptCanvasGroup.alpha <= 0.01f)
        {
            worldPromptPanel.gameObject.SetActive(false);
            worldPromptHasPosition = false;
            return;
        }

        worldPromptPanel.gameObject.SetActive(true);
        worldPromptBobTimer += Time.unscaledDeltaTime * 4f;
        Vector2 bob = new Vector2(0f, Mathf.Sin(worldPromptBobTimer) * 4f);
        worldPromptPanel.anchoredPosition = Vector2.Lerp(worldPromptPanel.anchoredPosition, worldPromptTargetPosition + bob, Time.unscaledDeltaTime * 14f);

        float targetScale = worldPromptVisible ? 1f : 0.94f;
        worldPromptPanel.localScale = Vector3.Lerp(worldPromptPanel.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * 12f);
    }

    private void RefreshInventory()
    {
        bool isOpen = inventory != null && inventory.isOpen;
        inventoryPanel.gameObject.SetActive(isOpen);

        if (!isOpen)
        {
            return;
        }

        inventoryTitleText.text = "Backpack " + inventory.gridSize + "x" + inventory.gridSize;
        RebuildInventoryGrid();
    }

    private void RefreshShop()
    {
        bool isOpen = shop != null && shop.IsMenuOpen;
        shopPanel.gameObject.SetActive(isOpen);

        if (!isOpen)
        {
            return;
        }

        if (inventory != null && (lastShopItemCount != inventory.items.Count || lastShopMoney != inventory.money))
        {
            shopDirty = true;
            lastShopItemCount = inventory.items.Count;
            lastShopMoney = inventory.money;
        }

        if (shopDirty)
        {
            RebuildShopContent();
            shopDirty = false;
        }

        AnimateShopSellZone();

        shopMessageText.text = inventory != null
            ? "Cash $" + inventory.money + " | Cargo $" + inventory.GetInventoryValue() + (shop.MessageTimer > 0f ? " | " + shop.ShopMessage : "")
            : "";
    }

    private void CreateShopTabButton(RectTransform parent, string iconText, string label, Vector2 anchoredPosition, ShopTab tab, Color iconColor)
    {
        RectTransform buttonRect = CreatePanel(label + " Tab", anchoredPosition, new Vector2(146f, 48f), new Vector2(0f, 1f), parent);
        buttonRect.GetComponent<Image>().color = new Color(0.13f, 0.11f, 0.08f, 0.94f);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonRect.GetComponent<Image>();
        button.onClick.AddListener(() =>
        {
            activeShopTab = tab;
            shopDirty = true;
        });
        AddHoverEffect(buttonRect, new Color(0.13f, 0.11f, 0.08f, 0.94f), new Color(0.22f, 0.17f, 0.1f, 0.98f));

        CreateIconBadge(buttonRect, new Vector2(8f, -7f), new Vector2(36f, 34f), iconText, iconColor);
        CreateText(label, buttonRect, new Vector2(52f, -10f), new Vector2(86f, 28f), 15, FontStyle.Bold, TextAnchor.MiddleLeft);
    }

    private void RebuildShopContent()
    {
        foreach (GameObject contentObject in shopContentObjects)
        {
            Destroy(contentObject);
        }

        shopContentObjects.Clear();
        shopDropZone = null;
        shopDropImage = null;
        shopDropText = null;

        if (activeShopTab == ShopTab.Sell)
        {
            BuildSellTab();
            return;
        }

        BuildUpgradesTab();
    }

    private void BuildSellTab()
    {
        shopTitleText.text = "Trader - Sell";
        AddShopObject(CreateText("Drag treasures into the sell box or click an item card.", shopContent, new Vector2(18f, -14f), new Vector2(486f, 22f), 14, FontStyle.Normal, TextAnchor.MiddleLeft).gameObject);

        shopDropZone = CreatePanel("Sell Drop Zone", new Vector2(18f, -48f), new Vector2(488f, 74f), new Vector2(0f, 1f), shopContent);
        shopDropImage = shopDropZone.GetComponent<Image>();
        shopDropImage.color = new Color(0.08f, 0.18f, 0.13f, 0.92f);
        AddShopObject(shopDropZone.gameObject);
        CreateIconBadge(shopDropZone, new Vector2(14f, -15f), new Vector2(44f, 44f), "sell", new Color(0.35f, 0.92f, 0.62f, 1f));
        shopDropText = CreateText("DROP TO SELL", shopDropZone, new Vector2(72f, -18f), new Vector2(380f, 34f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);

        if (inventory == null || inventory.items.Count == 0)
        {
            AddShopObject(CreateText("Backpack is empty. Go find something shiny.", shopContent, new Vector2(18f, -146f), new Vector2(486f, 26f), 16, FontStyle.Bold, TextAnchor.MiddleCenter).gameObject);
            return;
        }

        float cardWidth = 238f;
        float cardHeight = 56f;
        float gap = 10f;

        for (int i = 0; i < inventory.items.Count; i++)
        {
            PlayerInventory.InventorySlot item = inventory.items[i];
            int column = i % 2;
            int row = i / 2;
            Vector2 position = new Vector2(18f + column * (cardWidth + gap), -142f - row * (cardHeight + gap));
            RectTransform card = CreateShopItemCard(item, position, new Vector2(cardWidth, cardHeight));
            AddShopObject(card.gameObject);
        }
    }

    private RectTransform CreateShopItemCard(PlayerInventory.InventorySlot item, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform card = CreatePanel("Sell Item - " + item.itemName, anchoredPosition, size, new Vector2(0f, 1f), shopContent);
        Image image = card.GetComponent<Image>();
        image.color = new Color(0.16f, 0.12f, 0.08f, 0.96f);

        ShopDragItem dragItem = card.gameObject.AddComponent<ShopDragItem>();
        dragItem.owner = this;
        dragItem.item = item;

        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() =>
        {
            shop?.TrySellItem(item);
            shopDirty = true;
        });
        AddHoverEffect(card, new Color(0.16f, 0.12f, 0.08f, 0.96f), new Color(0.26f, 0.19f, 0.1f, 0.98f));

        CreateSpriteIcon(card, new Vector2(7f, -7f), new Vector2(42f, 42f), item.icon, true);
        CreateText(Shorten(item.itemName, 24), card, new Vector2(54f, -7f), new Vector2(size.x - 64f, 22f), 13, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateText("$" + item.value, card, new Vector2(54f, -30f), new Vector2(size.x - 64f, 18f), 12, FontStyle.Normal, TextAnchor.MiddleLeft);
        return card;
    }

    private void BuildUpgradesTab()
    {
        shopTitleText.text = "Trader - Upgrades";
        AddShopObject(CreateText("Spend cash to expand your search loop.", shopContent, new Vector2(18f, -14f), new Vector2(486f, 22f), 14, FontStyle.Normal, TextAnchor.MiddleLeft).gameObject);

        string detectorTitle = metalDetector != null ? metalDetector.CurrentDetectorName + " " + metalDetector.CurrentScanCells + "x" + metalDetector.CurrentScanCells : "Metal Detector";
        string detectorPrice = shop != null && metalDetector != null && metalDetector.CanUpgradeDetector ? "$" + shop.detectorUpgradeCost : "Maxed";
        string detectorDescription = metalDetector != null && metalDetector.CanUpgradeDetector
            ? "Next: " + metalDetector.GetDetectorName(metalDetector.DetectorTier + 1) + " scans " + metalDetector.GetScanCellsForTier(metalDetector.DetectorTier + 1) + "x" + metalDetector.GetScanCellsForTier(metalDetector.DetectorTier + 1) + " cells."
            : "Your detector scans the maximum 6x6 grid.";
        AddShopObject(CreateUpgradeCard("range", detectorTitle, detectorDescription, detectorPrice, new Vector2(18f, -50f), () =>
        {
            shop?.TryBuyDetectorUpgrade();
            shopDirty = true;
        }).gameObject);

        AddShopObject(CreateUpgradeCard("bag", "Backpack Size", "Adds another row and column to your backpack.", "$" + (shop != null ? shop.inventoryUpgradeCost : 0), new Vector2(18f, -128f), () =>
        {
            shop?.TryBuyInventoryUpgrade();
            shopDirty = true;
        }).gameObject);

        string shovelCost = shop != null && shop.shovelUpgraded ? "Equipped" : "$" + (shop != null ? shop.shovelUpgradeCost : 0);
        AddShopObject(CreateUpgradeCard("upgrade", "Clean Shovel", "Replaces the rusty shovel animation with the clean silver shovel.", shovelCost, new Vector2(18f, -206f), () =>
        {
            shop?.TryBuyShovelUpgrade();
            shopDirty = true;
        }).gameObject);

        AddShopObject(CreateUpgradeCard("map", "Search Areas", "Buy land at the sign next to each plot.", "Visit", new Vector2(18f, -284f), () =>
        {
            shop?.ShowLocationPurchaseHint();
            shopDirty = true;
        }).gameObject);
    }

    private RectTransform CreateUpgradeCard(string iconText, string title, string description, string price, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        RectTransform card = CreatePanel(title + " Upgrade", anchoredPosition, new Vector2(488f, 72f), new Vector2(0f, 1f), shopContent);
        Image image = card.GetComponent<Image>();
        image.color = new Color(0.08f, 0.11f, 0.14f, 0.96f);

        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        AddHoverEffect(card, new Color(0.08f, 0.11f, 0.14f, 0.96f), new Color(0.12f, 0.2f, 0.27f, 0.98f));

        CreateIconBadge(card, new Vector2(12f, -12f), new Vector2(48f, 48f), iconText, new Color(0.18f, 0.72f, 1f, 1f));
        CreateText(title, card, new Vector2(74f, -10f), new Vector2(260f, 22f), 17, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateText(description, card, new Vector2(74f, -35f), new Vector2(300f, 18f), 12, FontStyle.Normal, TextAnchor.MiddleLeft);
        CreateText(price, card, new Vector2(382f, -20f), new Vector2(88f, 30f), 17, FontStyle.Bold, TextAnchor.MiddleCenter);
        return card;
    }

    public void BeginShopItemDrag(ShopDragItem dragItem)
    {
        activeShopDragItem = dragItem;

        if (shopDragGhost != null)
        {
            Destroy(shopDragGhost.gameObject);
        }

        shopDragGhost = CreatePanel("Dragged Shop Item", Vector2.zero, new Vector2(190f, 48f), new Vector2(0.5f, 0.5f), canvas.transform);
        shopDragGhost.pivot = new Vector2(0.5f, 0.5f);
        shopDragGhost.GetComponent<Image>().color = new Color(0.22f, 0.16f, 0.08f, 0.88f);
        CreateSpriteIcon(shopDragGhost, new Vector2(7f, -6f), new Vector2(36f, 36f), dragItem.item.icon, true);
        CreateText(dragItem.item.itemName, shopDragGhost, new Vector2(50f, -10f), new Vector2(128f, 26f), 13, FontStyle.Bold, TextAnchor.MiddleLeft);
    }

    public void DragShopItem(Vector2 screenPosition)
    {
        if (shopDragGhost == null)
        {
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 localPosition))
        {
            shopDragGhost.anchoredPosition = localPosition;
        }
    }

    public void EndShopItemDrag(ShopDragItem dragItem, Vector2 screenPosition)
    {
        bool isSellDrop = shopDropZone != null && RectTransformUtility.RectangleContainsScreenPoint(shopDropZone, screenPosition, null);

        if (isSellDrop && dragItem != null && dragItem.item != null)
        {
            shop?.TrySellItem(dragItem.item);
            shopDirty = true;
        }

        activeShopDragItem = null;

        if (shopDragGhost != null)
        {
            Destroy(shopDragGhost.gameObject);
            shopDragGhost = null;
        }
    }

    private void AnimateShopSellZone()
    {
        if (shopDropImage == null)
        {
            return;
        }

        shopPulseTimer += Time.unscaledDeltaTime * 4f;
        float pulse = activeShopDragItem != null ? Mathf.Abs(Mathf.Sin(shopPulseTimer)) : 0f;
        Color baseColor = new Color(0.08f, 0.18f, 0.13f, 0.92f);
        Color activeColor = new Color(0.18f, 0.5f, 0.32f, 0.98f);
        shopDropImage.color = Color.Lerp(baseColor, activeColor, pulse);

        if (shopDropText != null)
        {
            shopDropText.text = activeShopDragItem != null ? "RELEASE TO SELL" : "DROP TO SELL";
        }
    }

    private void AddShopObject(GameObject contentObject)
    {
        shopContentObjects.Add(contentObject);
    }

    private void RefreshMap()
    {
        bool isOpen = treasureMap != null && (treasureMap.showSmallMap || treasureMap.showLargeMap);
        mapPanel.gameObject.SetActive(isOpen);

        if (!isOpen)
        {
            return;
        }

        mapTitleText.text = "Search Map";
        RebuildMapDots();
    }

    private void RebuildInventoryGrid()
    {
        foreach (GameObject cell in inventoryCells)
        {
            Destroy(cell);
        }

        inventoryCells.Clear();

        float cellSize = 52f;
        float gap = 7f;

        for (int y = 0; y < inventory.gridSize; y++)
        {
            for (int x = 0; x < inventory.gridSize; x++)
            {
                RectTransform cell = CreatePanel("Inventory Cell", new Vector2(x * (cellSize + gap), -y * (cellSize + gap)), new Vector2(cellSize, cellSize), new Vector2(0f, 1f), inventoryGrid);
                inventoryCells.Add(cell.gameObject);
            }
        }

        foreach (PlayerInventory.InventorySlot item in inventory.items)
        {
            float itemWidth = item.width * cellSize + (item.width - 1) * gap;
            float itemHeight = item.height * cellSize + (item.height - 1) * gap;
            RectTransform itemRect = CreatePanel(
                "Inventory Item",
                new Vector2(item.gridX * (cellSize + gap), -item.gridY * (cellSize + gap)),
                new Vector2(itemWidth, itemHeight),
                new Vector2(0f, 1f),
                inventoryGrid
            );

            itemRect.GetComponent<Image>().color = new Color(0.22f, 0.17f, 0.1f, 0.96f);
            inventoryCells.Add(itemRect.gameObject);
            Image iconImage = CreateSpriteIcon(itemRect, new Vector2(5f, -5f), new Vector2(itemWidth - 10f, itemHeight - 23f), item.icon, true);
            iconImage.transform.SetAsFirstSibling();
            CreateText("$" + item.value, itemRect, new Vector2(4f, -itemHeight + 18f), new Vector2(itemWidth - 8f, 16f), 11, FontStyle.Bold, TextAnchor.MiddleCenter);
        }
    }

    private void RebuildMapDots()
    {
        foreach (GameObject dot in mapDots)
        {
            Destroy(dot);
        }

        mapDots.Clear();

        if (treasureMap == null)
        {
            return;
        }

        foreach (Vector3 position in treasureMap.CheckedPositions)
        {
            Vector2 mapPosition = WorldToMapPosition(position);
            mapDots.Add(CreateDot("Checked Dot", mapPosition, new Color(0.7f, 0.66f, 0.56f, 1f), 8f).gameObject);
        }

        if (treasureMap.player != null)
        {
            Vector2 playerPosition = WorldToMapPosition(treasureMap.player.position);
            mapDots.Add(CreateDot("Player Dot", playerPosition, new Color(1f, 0.76f, 0.2f, 1f), 12f).gameObject);
        }
    }

    private Vector2 WorldToMapPosition(Vector3 worldPosition)
    {
        Vector2 worldSize = treasureMap != null ? treasureMap.worldSize : new Vector2(1000f, 1000f);
        RectTransform rectTransform = mapContent;
        float normalizedX = Mathf.InverseLerp(-worldSize.x * 0.5f, worldSize.x * 0.5f, worldPosition.x);
        float normalizedY = Mathf.InverseLerp(-worldSize.y * 0.5f, worldSize.y * 0.5f, worldPosition.z);
        return new Vector2(normalizedX * rectTransform.rect.width, -(1f - normalizedY) * rectTransform.rect.height);
    }

    private RectTransform CreateDot(string dotName, Vector2 anchoredPosition, Color color, float size)
    {
        RectTransform dot = CreateUiObject(dotName, mapContent);
        dot.anchorMin = new Vector2(0f, 1f);
        dot.anchorMax = new Vector2(0f, 1f);
        dot.pivot = new Vector2(0.5f, 0.5f);
        dot.anchoredPosition = anchoredPosition;
        dot.sizeDelta = new Vector2(size, size);
        dot.gameObject.AddComponent<Image>().color = color;
        return dot;
    }

    private RectTransform CreatePanel(string panelName, Vector2 anchoredPosition, Vector2 size, Vector2 anchor, Transform parent)
    {
        RectTransform rectTransform = CreateUiObject(panelName, parent);
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = anchor;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.color = new Color(0.07f, 0.08f, 0.07f, 0.78f);

        return rectTransform;
    }

    private Image CreateBar(RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        RectTransform background = CreatePanel("Bar Background", anchoredPosition, size, new Vector2(0f, 1f), parent);
        background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        RectTransform fill = CreateUiObject("Bar Fill", background);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        Image image = fill.gameObject.AddComponent<Image>();
        image.color = color;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = 0;
        image.fillAmount = 1f;
        return image;
    }

    private Text CreateText(string value, RectTransform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
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

    private Button CreateButton(string label, RectTransform parent, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        RectTransform rectTransform = CreatePanel(label + " Button", anchoredPosition, size, new Vector2(0f, 1f), parent);
        Image image = rectTransform.GetComponent<Image>();
        image.color = new Color(0.21f, 0.17f, 0.1f, 0.95f);

        Button button = rectTransform.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        AddHoverEffect(rectTransform, new Color(0.21f, 0.17f, 0.1f, 0.95f), new Color(0.3f, 0.23f, 0.12f, 0.98f));
        CreateText(label, rectTransform, new Vector2(0f, 0f), size, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
        return button;
    }

    private RectTransform CreateIconBadge(RectTransform parent, Vector2 anchoredPosition, Vector2 size, string label, Color color)
    {
        RectTransform badge = CreatePanel("Icon Badge " + label, anchoredPosition, size, new Vector2(0f, 1f), parent);
        Image image = badge.GetComponent<Image>();
        image.color = color;

        RectTransform icon = CreateUiObject("Icon " + label, badge);
        icon.anchorMin = new Vector2(0.5f, 0.5f);
        icon.anchorMax = new Vector2(0.5f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.anchoredPosition = Vector2.zero;
        float iconSize = Mathf.Min(size.x, size.y) * 0.78f;
        icon.sizeDelta = new Vector2(iconSize, iconSize);

        RectTransform shadow = CreateUiObject("Icon Shadow " + label, badge);
        shadow.anchorMin = new Vector2(0.5f, 0.5f);
        shadow.anchorMax = new Vector2(0.5f, 0.5f);
        shadow.pivot = new Vector2(0.5f, 0.5f);
        shadow.anchoredPosition = new Vector2(1.5f, -1.5f);
        shadow.sizeDelta = new Vector2(iconSize, iconSize);

        Image shadowImage = shadow.gameObject.AddComponent<Image>();
        shadowImage.sprite = GetIconSprite(label);
        shadowImage.color = new Color(0f, 0f, 0f, 0.38f);
        shadowImage.raycastTarget = false;

        Image iconImage = icon.gameObject.AddComponent<Image>();
        iconImage.sprite = GetIconSprite(label);
        iconImage.color = Color.white;
        iconImage.raycastTarget = false;
        icon.SetAsLastSibling();
        return badge;
    }

    private Image CreateSpriteIcon(RectTransform parent, Vector2 anchoredPosition, Vector2 size, Sprite sprite, bool preserveAspect)
    {
        RectTransform icon = CreateUiObject("Item Icon", parent);
        icon.anchorMin = new Vector2(0f, 1f);
        icon.anchorMax = new Vector2(0f, 1f);
        icon.pivot = new Vector2(0f, 1f);
        icon.anchoredPosition = anchoredPosition;
        icon.sizeDelta = size;

        Image image = icon.gameObject.AddComponent<Image>();
        image.sprite = sprite != null ? sprite : GetIconSprite("treasure");
        image.color = Color.white;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return image;
    }

    private RewardTier GetRewardTier(int value)
    {
        if (value > 100)
        {
            return RewardTier.Jackpot;
        }

        if (value >= 50)
        {
            return RewardTier.Valuable;
        }

        if (value >= 10)
        {
            return RewardTier.Okay;
        }

        return RewardTier.Trash;
    }

    private string GetRewardTitle(RewardTier tier)
    {
        switch (tier)
        {
            case RewardTier.Jackpot:
                return "JACKPOT!";
            case RewardTier.Valuable:
                return "Great Find!";
            case RewardTier.Okay:
                return "Nice Find";
            case RewardTier.Trash:
                return "Found Something";
            default:
                return "Info";
        }
    }

    private string GetRewardSubtitle(RewardTier tier)
    {
        switch (tier)
        {
            case RewardTier.Jackpot:
                return "Huge treasure found:";
            case RewardTier.Valuable:
                return "You found something valuable:";
            case RewardTier.Okay:
                return "You found:";
            case RewardTier.Trash:
                return "A tiny find:";
            default:
                return "";
        }
    }

    private Color GetRewardAccentColor(RewardTier tier)
    {
        switch (tier)
        {
            case RewardTier.Jackpot:
                return new Color(1f, 0.46f, 0.12f, 1f);
            case RewardTier.Valuable:
                return new Color(0.6f, 0.84f, 1f, 1f);
            case RewardTier.Okay:
                return new Color(0.35f, 0.92f, 0.62f, 1f);
            case RewardTier.Trash:
                return new Color(0.72f, 0.66f, 0.56f, 1f);
            default:
                return new Color(0.98f, 0.94f, 0.82f, 1f);
        }
    }

    private Color GetRewardPanelColor(RewardTier tier)
    {
        switch (tier)
        {
            case RewardTier.Jackpot:
                return new Color(0.16f, 0.055f, 0.025f, 0.97f);
            case RewardTier.Valuable:
                return new Color(0.035f, 0.075f, 0.11f, 0.96f);
            case RewardTier.Okay:
                return new Color(0.035f, 0.085f, 0.055f, 0.95f);
            case RewardTier.Trash:
                return new Color(0.07f, 0.062f, 0.05f, 0.92f);
            default:
                return new Color(0.045f, 0.038f, 0.028f, 0.94f);
        }
    }

    private float GetRewardPopStrength(RewardTier tier)
    {
        switch (tier)
        {
            case RewardTier.Jackpot:
                return 0.23f;
            case RewardTier.Valuable:
                return 0.16f;
            case RewardTier.Okay:
                return 0.1f;
            case RewardTier.Trash:
                return 0.035f;
            default:
                return 0f;
        }
    }

    private int GetRewardSparkleCount(RewardTier tier)
    {
        switch (tier)
        {
            case RewardTier.Jackpot:
                return 16;
            case RewardTier.Valuable:
                return 10;
            case RewardTier.Okay:
                return 5;
            case RewardTier.Trash:
                return 1;
            default:
                return 0;
        }
    }

    private float GetRewardSparkleScale(RewardTier tier)
    {
        switch (tier)
        {
            case RewardTier.Jackpot:
                return 1.65f;
            case RewardTier.Valuable:
                return 1.28f;
            case RewardTier.Okay:
                return 0.9f;
            case RewardTier.Trash:
                return 0.65f;
            default:
                return 1f;
        }
    }

    private void ApplyFoundToastLayout(RewardTier tier)
    {
        Vector2 panelSize = new Vector2(520f, 150f);
        Vector2 iconPosition = new Vector2(24f, -24f);
        Vector2 iconSize = new Vector2(104f, 104f);
        Vector2 textStart = new Vector2(150f, -22f);
        Vector2 textWidth = new Vector2(340f, 30f);
        int titleSize = 24;
        int subtitleSize = 18;
        int valueSize = 16;

        if (tier == RewardTier.Trash)
        {
            panelSize = new Vector2(460f, 126f);
            iconPosition = new Vector2(22f, -22f);
            iconSize = new Vector2(82f, 82f);
            textStart = new Vector2(126f, -18f);
            textWidth = new Vector2(304f, 28f);
            titleSize = 21;
            subtitleSize = 15;
            valueSize = 14;
        }
        else if (tier == RewardTier.Valuable)
        {
            panelSize = new Vector2(610f, 178f);
            iconPosition = new Vector2(26f, -24f);
            iconSize = new Vector2(126f, 126f);
            textStart = new Vector2(176f, -24f);
            textWidth = new Vector2(404f, 34f);
            titleSize = 30;
            subtitleSize = 20;
            valueSize = 18;
        }
        else if (tier == RewardTier.Jackpot)
        {
            panelSize = new Vector2(700f, 208f);
            iconPosition = new Vector2(30f, -28f);
            iconSize = new Vector2(150f, 150f);
            textStart = new Vector2(208f, -26f);
            textWidth = new Vector2(456f, 42f);
            titleSize = 38;
            subtitleSize = 24;
            valueSize = 22;
        }

        foundToastPanel.sizeDelta = panelSize;
        foundToastIcon.rectTransform.anchoredPosition = iconPosition;
        foundToastIcon.rectTransform.sizeDelta = iconSize;
        foundToastTitleText.rectTransform.anchoredPosition = textStart;
        foundToastTitleText.rectTransform.sizeDelta = textWidth;
        foundToastText.rectTransform.anchoredPosition = new Vector2(textStart.x, textStart.y - textWidth.y - 8f);
        foundToastText.rectTransform.sizeDelta = new Vector2(textWidth.x, 34f);
        foundToastValueText.rectTransform.anchoredPosition = new Vector2(textStart.x, textStart.y - textWidth.y - 48f);
        foundToastValueText.rectTransform.sizeDelta = new Vector2(textWidth.x, 34f);
        foundToastTitleText.fontSize = titleSize;
        foundToastText.fontSize = subtitleSize;
        foundToastValueText.fontSize = valueSize;
    }

    private Vector2 GetFoundToastIconCenter()
    {
        RectTransform rectTransform = foundToastIcon.rectTransform;
        return rectTransform.anchoredPosition + new Vector2(rectTransform.sizeDelta.x * 0.5f, -rectTransform.sizeDelta.y * 0.5f);
    }

    private void BuildFoundToastSparkles()
    {
        foundToastSparkles.Clear();

        for (int i = 0; i < 16; i++)
        {
            RectTransform sparkle = CreateUiObject("Found Sparkle", foundToastPanel);
            sparkle.anchorMin = new Vector2(0f, 1f);
            sparkle.anchorMax = new Vector2(0f, 1f);
            sparkle.pivot = new Vector2(0.5f, 0.5f);
            sparkle.sizeDelta = new Vector2(10f, 10f);

            Image image = sparkle.gameObject.AddComponent<Image>();
            image.sprite = GetIconSprite("treasure");
            image.color = new Color(1f, 0.78f, 0.24f, 0f);
            image.raycastTarget = false;
            sparkle.gameObject.SetActive(false);
            foundToastSparkles.Add(sparkle);
        }
    }

    private void AddHoverEffect(RectTransform target, Color normalColor, Color hoverColor)
    {
        ShopUiHover hover = target.gameObject.AddComponent<ShopUiHover>();
        hover.normalColor = normalColor;
        hover.hoverColor = hoverColor;
        hover.pressedColor = Color.Lerp(hoverColor, Color.white, 0.16f);
    }

    private Sprite GetIconSprite(string iconName)
    {
        if (iconSprites.TryGetValue(iconName, out Sprite sprite))
        {
            return sprite;
        }

        Texture2D texture = CreateIconTexture(iconName);
        sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        iconSprites.Add(iconName, sprite);
        return sprite;
    }

    private Texture2D CreateIconTexture(string iconName)
    {
        const int size = 48;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color ink = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        if (iconName == "coin")
        {
            DrawCircle(texture, 24, 24, 16, ink);
            DrawCircle(texture, 24, 24, 9, clear);
            DrawRect(texture, 22, 13, 4, 22, ink);
            DrawRect(texture, 17, 17, 14, 4, ink);
            DrawRect(texture, 17, 27, 14, 4, ink);
        }
        else if (iconName == "sell")
        {
            DrawRect(texture, 7, 28, 21, 4, ink);
            DrawRect(texture, 24, 20, 4, 12, ink);
            DrawLine(texture, 25, 20, 38, 24, ink, 3);
            DrawLine(texture, 25, 20, 38, 16, ink, 3);
            DrawRect(texture, 36, 14, 4, 12, ink);
            DrawRect(texture, 10, 12, 16, 10, ink);
        }
        else if (iconName == "upgrade")
        {
            DrawLine(texture, 24, 10, 24, 36, ink, 4);
            DrawLine(texture, 24, 10, 12, 22, ink, 4);
            DrawLine(texture, 24, 10, 36, 22, ink, 4);
            DrawRect(texture, 14, 34, 20, 4, ink);
        }
        else if (iconName == "range")
        {
            DrawCircleOutline(texture, 24, 24, 6, ink, 3);
            DrawCircleOutline(texture, 24, 24, 14, ink, 3);
            DrawCircleOutline(texture, 24, 24, 21, ink, 2);
        }
        else if (iconName == "bag")
        {
            DrawRect(texture, 12, 18, 24, 22, ink);
            DrawRect(texture, 17, 11, 14, 5, ink);
            DrawRect(texture, 19, 8, 10, 4, ink);
            DrawRect(texture, 17, 18, 14, 8, clear);
        }
        else if (iconName == "map")
        {
            DrawRect(texture, 8, 12, 10, 26, ink);
            DrawRect(texture, 20, 10, 10, 26, ink);
            DrawRect(texture, 32, 12, 8, 26, ink);
            DrawLine(texture, 18, 13, 18, 38, clear, 2);
            DrawLine(texture, 30, 10, 30, 36, clear, 2);
        }
        else
        {
            DrawCircle(texture, 24, 24, 14, ink);
            DrawRect(texture, 17, 21, 14, 6, clear);
        }

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }

    private void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int py = y; py < y + height; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                SetIconPixel(texture, px, py, color);
            }
        }
    }

    private void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        int radiusSquared = radius * radius;

        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;

                if (dx * dx + dy * dy <= radiusSquared)
                {
                    SetIconPixel(texture, x, y, color);
                }
            }
        }
    }

    private void DrawCircleOutline(Texture2D texture, int centerX, int centerY, int radius, Color color, int thickness)
    {
        int outer = radius * radius;
        int innerRadius = Mathf.Max(0, radius - thickness);
        int inner = innerRadius * innerRadius;

        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                int distance = dx * dx + dy * dy;

                if (distance <= outer && distance >= inner)
                {
                    SetIconPixel(texture, x, y, color);
                }
            }
        }
    }

    private void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int error = dx - dy;

        while (true)
        {
            DrawCircle(texture, x0, y0, Mathf.Max(1, thickness / 2), color);

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int error2 = error * 2;

            if (error2 > -dy)
            {
                error -= dy;
                x0 += sx;
            }

            if (error2 < dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private void SetIconPixel(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
        {
            return;
        }

        texture.SetPixel(x, y, color);
    }

    private RectTransform CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        return gameObject.AddComponent<RectTransform>();
    }

    private string Shorten(string itemName, int maxLength = 10)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return "Item";
        }

        if (itemName.Length <= maxLength)
        {
            return itemName;
        }

        return itemName.Substring(0, Mathf.Max(1, maxLength - 1)) + ".";
    }
}
