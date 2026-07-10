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

    public static bool IsActive { get; private set; }

    private Canvas canvas;
    private MetalDetector metalDetector;
    private PlayerInventory inventory;
    private UpgradeShop shop;
    private DiggingController diggingController;
    private InteractionTargetHighlight interactionHighlight;

    private Text moneyText;
    private RectTransform worldPromptPanel;
    private Image worldPromptIcon;
    private Text worldPromptKeyText;
    private Text worldPromptActionText;
    private CanvasGroup worldPromptCanvasGroup;
    private RectTransform foundToastPanel;
    private CanvasGroup foundToastCanvasGroup;
    private Image foundToastIcon;
    private Text foundToastTitleText;
    private Text foundToastText;
    private Text foundToastValueText;
    private Image foundToastProgressFill;
    private readonly List<RectTransform> foundToastSparkles = new List<RectTransform>();
    private string currentFoundToastMessage = "";
    private int currentFoundToastValue = -1;
    private bool foundToastShouldShow;
    private bool foundToastIsReward;
    private bool foundToastIsDigProgress;
    private RewardTier foundToastTier = RewardTier.Info;
    private Color foundToastAccentColor = Color.white;
    private float foundToastAnimationTimer;
    private Vector2 worldPromptTargetPosition;
    private bool worldPromptVisible;
    private bool worldPromptHasPosition;
    private float worldPromptBobTimer;
    private Text inventoryTitleText;
    private Text shopMessageText;

    private RectTransform inventoryPanel;
    private RectTransform inventoryGrid;
    private RectTransform shopPanel;
    private RectTransform shopContent;
    private RectTransform shopDropZone;
    private Text shopTitleText;
    private Text shopDropText;
    private Image shopDropImage;
    private RectTransform shopDragGhost;

    private readonly List<GameObject> inventoryCells = new List<GameObject>();
    private readonly List<GameObject> shopContentObjects = new List<GameObject>();
    private readonly Dictionary<string, Sprite> iconSprites = new Dictionary<string, Sprite>();
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
        interactionHighlight = gameObject.AddComponent<InteractionTargetHighlight>();
        GameLocalization.LanguageChanged += HandleLanguageChanged;
    }

    private void OnDestroy()
    {
        GameLocalization.LanguageChanged -= HandleLanguageChanged;
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
            metalDetector = PlayerRigReferences.FindLocalMetalDetector();
        }

        if (inventory == null)
        {
            inventory = PlayerRigReferences.FindLocalInventory();
        }

        if (diggingController == null)
        {
            diggingController = FindAnyObjectByType<DiggingController>();
        }

        UpgradeShop bestShop = FindBestShop();

        if (bestShop != null && bestShop != shop && (shop == null || !shop.IsMenuOpen))
        {
            shop = bestShop;
            shopDirty = true;
        }
    }

    private UpgradeShop FindBestShop()
    {
        UpgradeShop[] shops = FindObjectsByType<UpgradeShop>();
        UpgradeShop nearestShop = null;
        float nearestDistance = float.MaxValue;

        foreach (UpgradeShop candidate in shops)
        {
            if (candidate == null || candidate.shopNpc == null)
            {
                continue;
            }

            if (candidate.IsMenuOpen)
            {
                return candidate;
            }

            if (!candidate.IsPlayerNearShop())
            {
                continue;
            }

            float distance = Vector3.Distance(candidate.transform.position, candidate.shopNpc.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestShop = candidate;
            }
        }

        if (nearestShop != null)
        {
            return nearestShop;
        }

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
    }

    private void BuildHud()
    {
        RectTransform moneyPanel = CreatePanel("Money Panel", new Vector2(-24f, -22f), new Vector2(250f, 68f), new Vector2(1f, 1f), canvas.transform);
        moneyPanel.GetComponent<Image>().color = new Color(0.045f, 0.042f, 0.032f, 0.9f);
        CreateIconBadge(moneyPanel, new Vector2(14f, -12f), new Vector2(48f, 44f), "coin", new Color(1f, 0.76f, 0.2f, 1f));
        moneyText = CreateText("$0", moneyPanel, new Vector2(76f, -12f), new Vector2(152f, 44f), 28, FontStyle.Bold, TextAnchor.MiddleRight);
        moneyText.color = new Color(1f, 0.86f, 0.38f, 1f);

        foundToastPanel = CreatePanel("Found Treasure Toast", new Vector2(0f, -185f), new Vector2(520f, 150f), new Vector2(0.5f, 0.5f), canvas.transform);
        foundToastPanel.pivot = new Vector2(0.5f, 0.5f);
        foundToastPanel.GetComponent<Image>().color = new Color(0.045f, 0.038f, 0.028f, 0.94f);
        foundToastCanvasGroup = foundToastPanel.gameObject.AddComponent<CanvasGroup>();
        foundToastCanvasGroup.alpha = 0f;
        foundToastIcon = CreateSpriteIcon(foundToastPanel, new Vector2(24f, -24f), new Vector2(104f, 104f), null, true);
        foundToastTitleText = CreateText("", foundToastPanel, new Vector2(150f, -22f), new Vector2(340f, 30f), 24, FontStyle.Bold, TextAnchor.MiddleLeft);
        foundToastText = CreateText("", foundToastPanel, new Vector2(150f, -58f), new Vector2(340f, 34f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        foundToastValueText = CreateText("", foundToastPanel, new Vector2(150f, -96f), new Vector2(340f, 28f), 16, FontStyle.Normal, TextAnchor.MiddleLeft);
        foundToastProgressFill = CreateBar(foundToastPanel, new Vector2(72f, -62f), new Vector2(260f, 10f), new Color(0.25f, 0.88f, 1f, 1f));
        foundToastProgressFill.transform.parent.SetAsLastSibling();
        BuildFoundToastSparkles();
        foundToastPanel.gameObject.SetActive(false);

        worldPromptPanel = CreatePanel("World Interaction Prompt", Vector2.zero, new Vector2(270f, 54f), new Vector2(0.5f, 0.5f), canvas.transform);
        worldPromptPanel.pivot = new Vector2(0.5f, 0.5f);
        worldPromptCanvasGroup = worldPromptPanel.gameObject.AddComponent<CanvasGroup>();
        worldPromptCanvasGroup.alpha = 0f;
        Image promptImage = worldPromptPanel.GetComponent<Image>();
        promptImage.color = new Color(0.03f, 0.035f, 0.03f, 0.88f);
        RectTransform keyBox = CreatePanel("World Prompt Key", new Vector2(12f, -8f), new Vector2(44f, 38f), new Vector2(0f, 1f), worldPromptPanel);
        keyBox.GetComponent<Image>().color = new Color(1f, 0.76f, 0.2f, 0.95f);
        worldPromptKeyText = CreateText("E", keyBox, Vector2.zero, new Vector2(44f, 38f), 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        worldPromptKeyText.color = new Color(0.06f, 0.045f, 0.02f, 1f);
        worldPromptIcon = CreateSpriteIcon(worldPromptPanel, new Vector2(64f, -12f), new Vector2(30f, 30f), GetIconSprite("quest"), true);
        worldPromptActionText = CreateText("", worldPromptPanel, new Vector2(104f, -11f), new Vector2(146f, 32f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        worldPromptPanel.gameObject.SetActive(false);
    }

    private void BuildInventory()
    {
        inventoryPanel = CreatePanel("Inventory Panel", new Vector2(-20f, -140f), new Vector2(420f, 520f), new Vector2(1f, 1f), canvas.transform);
        inventoryTitleText = CreateText(GameLocalization.T("inventory.backpack"), inventoryPanel, new Vector2(18f, -14f), new Vector2(360f, 26f), 22, FontStyle.Bold, TextAnchor.MiddleLeft);
        inventoryGrid = CreatePanel("Inventory Grid", new Vector2(18f, -82f), new Vector2(370f, 390f), new Vector2(0f, 1f), inventoryPanel);
        inventoryGrid.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        inventoryPanel.gameObject.SetActive(false);
    }

    private void BuildShop()
    {
        shopPanel = CreatePanel("Trader Panel", Vector2.zero, new Vector2(760f, 520f), new Vector2(0.5f, 0.5f), canvas.transform);
        shopPanel.GetComponent<Image>().color = new Color(0.045f, 0.038f, 0.028f, 0.95f);
        shopTitleText = CreateText(GameLocalization.T("shop.trader"), shopPanel, new Vector2(24f, -18f), new Vector2(600f, 34f), 28, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateIconBadge(shopPanel, new Vector2(676f, -18f), new Vector2(54f, 34f), "coin", new Color(1f, 0.76f, 0.2f, 1f));

        shopContent = CreatePanel("Trader Content", new Vector2(24f, -72f), new Vector2(712f, 376f), new Vector2(0f, 1f), shopPanel);
        shopContent.GetComponent<Image>().color = new Color(0.025f, 0.028f, 0.024f, 0.72f);

        shopMessageText = CreateText("", shopPanel, new Vector2(24f, -464f), new Vector2(712f, 34f), 16, FontStyle.Bold, TextAnchor.MiddleCenter);
        shopPanel.gameObject.SetActive(false);
    }

    private void Refresh()
    {
        RefreshLocalizedLabels();
        RefreshHud();
        RefreshInventory();
        RefreshShop();
    }

    private void HandleLanguageChanged()
    {
        shopDirty = true;
        lastShopItemCount = -1;
        lastShopMoney = -1;
        Refresh();
    }

    private void RefreshLocalizedLabels()
    {
        if (inventoryTitleText != null)
        {
            inventoryTitleText.text = GameLocalization.T("inventory.backpack");
        }

    }

    private void RefreshHud()
    {
        if (inventory != null)
        {
            moneyText.text = "$" + inventory.money;
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
        foundToastIsDigProgress = !foundToastIsReward && diggingController.LastDigRequiredHits > 0;
        foundToastTier = foundToastIsReward ? GetRewardTier(diggingController.LastFoundValue) : RewardTier.Info;
        foundToastAccentColor = foundToastIsDigProgress ? new Color(0.25f, 0.88f, 1f, 1f) : GetRewardAccentColor(foundToastTier);
        foundToastPanel.GetComponent<Image>().color = GetRewardPanelColor(foundToastTier);
        ApplyFoundToastLayout(foundToastTier);
        foundToastTitleText.text = GetToastTitle();
        foundToastTitleText.color = foundToastAccentColor;
        foundToastText.text = GetToastBody();
        foundToastValueText.text = foundToastIsReward
            ? diggingController.LastFoundItemName + "  $" + diggingController.LastFoundValue
            : "";
        foundToastValueText.gameObject.SetActive(foundToastIsReward);
        foundToastValueText.color = foundToastIsReward ? Color.Lerp(foundToastAccentColor, Color.white, 0.35f) : Color.white;
        foundToastIcon.gameObject.SetActive(foundToastIsReward || foundToastIsDigProgress);
        foundToastIcon.sprite = GetFoundToastIcon();
        foundToastIcon.preserveAspect = true;

        if (foundToastProgressFill != null)
        {
            foundToastProgressFill.transform.parent.gameObject.SetActive(foundToastIsDigProgress);
            SetFoundToastProgressFill(foundToastIsDigProgress ? diggingController.LastDigProgress01 : 0f);
            foundToastProgressFill.color = foundToastAccentColor;
        }
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
        float targetScale = foundToastIsReward ? Mathf.Lerp(startScale, 1f, easedAppear) + rewardPop + idlePulse : Mathf.Lerp(0.92f, 1f, easedAppear);
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
            SetHintText(GameLocalization.T("hud.hint_close_trader"));
            HideWorldPrompt();
            return;
        }

        if (GameUIState.IsQuestMenuOpen)
        {
            SetHintText(GameLocalization.T("hud.hint_close_jobs"));
            HideWorldPrompt();
            return;
        }

        if (GameUIState.IsInventoryOpen)
        {
            SetHintText(GameLocalization.T("hud.hint_close_backpack"));
            HideWorldPrompt();
            return;
        }

        if (GameUIState.IsHomeMenuOpen)
        {
            SetHintText(GameLocalization.T("hud.hint_close_home"));
            HideWorldPrompt();
            return;
        }

        NpcQuestGiver questGiver = NpcQuestGiver.FindClosestQuestGiverInRange();

        if (questGiver != null)
        {
            SetHintText(GameLocalization.T("hud.hint_talk_jobs"));
            ShowWorldPrompt(GameLocalization.T("hud.action_jobs"), questGiver.PromptPosition, new Color(0.35f, 0.92f, 0.62f, 0.95f), "quest", questGiver.transform);
            return;
        }

        PlayerHome home = PlayerHome.FindClosestHomeInRange();

        if (home != null)
        {
            SetHintText(GameLocalization.T("hud.hint_use_home"));
            ShowWorldPrompt(GameLocalization.T("hud.action_use_home"), home.PromptPosition, new Color(0.35f, 0.92f, 0.62f, 0.95f), "bag", home.transform);
            return;
        }

        SearchAreaPurchasePoint purchasePoint = SearchAreaPurchasePoint.FindClosestInteractableInRange();

        if (purchasePoint != null)
        {
            SetHintText("E - " + purchasePoint.PromptActionText);
            ShowWorldPrompt(purchasePoint.PromptActionText, purchasePoint.PromptPosition, new Color(0.35f, 0.92f, 0.62f, 0.95f), "map", purchasePoint.transform);
            return;
        }

        if (DayNightCycle.IsNightNow)
        {
            SetHintText(GameLocalization.T("hud.hint_night"));
            HideWorldPrompt();
            return;
        }

        UpgradeShop promptShop = FindBestShop();

        if (promptShop != null && promptShop.IsPlayerNearShop())
        {
            shop = promptShop;
            string actionText = promptShop.CanSellHere && !promptShop.CanUpgradeHere
                ? GameLocalization.T("hud.action_sell")
                : promptShop.CanUpgradeHere && !promptShop.CanSellHere ? GameLocalization.T("hud.action_upgrade") : GameLocalization.T("hud.action_trade");
            string icon = promptShop.CanSellHere && !promptShop.CanUpgradeHere ? "sell" : "upgrade";
            SetHintText("E - " + actionText);
            ShowWorldPrompt(actionText, promptShop.shopNpc.position + Vector3.up * 1.35f, new Color(1f, 0.76f, 0.2f, 0.95f), icon, promptShop.shopNpc);
            return;
        }

        DetectableTreasure searchableChest = diggingController != null ? diggingController.FindClosestSearchableChestInRange() : null;

        if (searchableChest != null)
        {
            string actionText = GameLocalization.T("hud.action_search");
            SetHintText("E - " + actionText);
            ShowWorldPrompt(actionText, diggingController.GetChestPromptPosition(searchableChest), new Color(1f, 0.76f, 0.2f, 0.95f), "treasure", diggingController.GetChestHighlightTarget(searchableChest));
            return;
        }

        DetectableTreasure digTarget = diggingController != null ? diggingController.FindClosestTreasureInRange() : null;

        if (digTarget != null)
        {
            string actionText = GameLocalization.T("hud.action_dig");
            SetHintText("E - " + actionText);
            Vector3 promptPosition = digTarget.revealMarker != null
                ? digTarget.revealMarker.transform.position + Vector3.up * 0.75f
                : digTarget.transform.position + Vector3.up * 0.75f;
            ShowWorldPrompt(actionText, promptPosition, new Color(0.25f, 0.88f, 1f, 0.95f), "treasure");
            return;
        }

        SetHintText(GameLocalization.T("hud.hint_default"));
        HideWorldPrompt();
    }

    private void SetHintText(string text)
    {
    }

    private void ShowWorldPrompt(string actionText, Vector3 worldPosition, Color keyColor, string iconName, Transform highlightTarget = null)
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
        SetInteractionHighlight(highlightTarget);

        if (!worldPromptHasPosition)
        {
            worldPromptPanel.anchoredPosition = localPosition;
            worldPromptHasPosition = true;
        }

        worldPromptActionText.text = actionText;
        worldPromptKeyText.transform.parent.GetComponent<Image>().color = keyColor;

        if (worldPromptIcon != null)
        {
            worldPromptIcon.sprite = GetIconSprite(iconName);
        }

        worldPromptPanel.gameObject.SetActive(true);
    }

    private void HideWorldPrompt()
    {
        worldPromptVisible = false;
        SetInteractionHighlight(null);
    }

    private void SetInteractionHighlight(Transform target)
    {
        if (interactionHighlight != null)
        {
            interactionHighlight.SetTarget(target);
        }
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
        // The backpack is drawn by PlayerInventory.OnGUI so it can use the illustrated board assets.
        inventoryPanel.gameObject.SetActive(false);
    }

    private void RefreshShop()
    {
        UpgradeShop openShop = FindBestShop();

        if (openShop != null && openShop.IsMenuOpen && openShop != shop)
        {
            shop = openShop;
            shopDirty = true;
        }

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
            ? GameLocalization.TFormat("shop.money_cargo", inventory.money, inventory.GetInventoryValue()) + (shop.MessageTimer > 0f ? " | " + shop.ShopMessage : "")
            : "";
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

        if (shop == null)
        {
            return;
        }

        if (shop.CanSellHere && shop.CanUpgradeHere)
        {
            BuildCombinedShop();
            return;
        }

        if (shop.CanSellHere)
        {
            BuildSellTab();
            return;
        }

        if (shop.CanUpgradeHere)
        {
            BuildUpgradesTab();
            return;
        }

        shopTitleText.text = GameLocalization.T("shop.trader");
        AddShopObject(CreateText(GameLocalization.T("toast.notice"), shopContent, new Vector2(18f, -20f), new Vector2(676f, 26f), 16, FontStyle.Bold, TextAnchor.MiddleCenter).gameObject);
    }

    private void BuildSellTab()
    {
        shopTitleText.text = GameLocalization.T("shop.sell");
        BuildSellSection(18f, -14f, 676f, 3, true);
    }

    private void BuildCombinedShop()
    {
        shopTitleText.text = GameLocalization.T("shop.trader");
        AddShopObject(CreateText(GameLocalization.T("shop.sell"), shopContent, new Vector2(18f, -14f), new Vector2(318f, 24f), 18, FontStyle.Bold, TextAnchor.MiddleLeft).gameObject);
        AddShopObject(CreateText(GameLocalization.T("shop.upgrades"), shopContent, new Vector2(370f, -14f), new Vector2(324f, 24f), 18, FontStyle.Bold, TextAnchor.MiddleLeft).gameObject);
        BuildSellSection(18f, -46f, 318f, 1, false);
        BuildUpgradeSection(370f, -46f, 324f, false);
    }

    private void BuildSellSection(float x, float y, float width, int columns, bool showHelp)
    {
        if (showHelp)
        {
            AddShopObject(CreateText(GameLocalization.T("shop.sell_help"), shopContent, new Vector2(x, y), new Vector2(width, 22f), 14, FontStyle.Normal, TextAnchor.MiddleLeft).gameObject);
            y -= 34f;
        }

        shopDropZone = CreatePanel("Sell Drop Zone", new Vector2(x, y), new Vector2(width, 74f), new Vector2(0f, 1f), shopContent);
        shopDropImage = shopDropZone.GetComponent<Image>();
        shopDropImage.color = new Color(0.08f, 0.18f, 0.13f, 0.92f);
        AddShopObject(shopDropZone.gameObject);
        CreateIconBadge(shopDropZone, new Vector2(14f, -15f), new Vector2(44f, 44f), "sell", new Color(0.35f, 0.92f, 0.62f, 1f));
        shopDropText = CreateText(GameLocalization.T("shop.drop_to_sell"), shopDropZone, new Vector2(72f, -18f), new Vector2(width - 92f, 34f), 18, FontStyle.Bold, TextAnchor.MiddleLeft);

        float itemStartY = y - 98f;

        if (inventory == null || inventory.items.Count == 0)
        {
            AddShopObject(CreateText(GameLocalization.T("shop.empty_backpack"), shopContent, new Vector2(x, itemStartY + 2f), new Vector2(width, 26f), 16, FontStyle.Bold, TextAnchor.MiddleCenter).gameObject);
            return;
        }

        int safeColumns = Mathf.Max(1, columns);
        float gap = 10f;
        float cardWidth = (width - (safeColumns - 1) * gap) / safeColumns;
        float cardHeight = 56f;

        for (int i = 0; i < inventory.items.Count; i++)
        {
            PlayerInventory.InventorySlot item = inventory.items[i];
            int column = i % safeColumns;
            int row = i / safeColumns;
            Vector2 position = new Vector2(x + column * (cardWidth + gap), itemStartY - row * (cardHeight + gap));
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
        shopTitleText.text = GameLocalization.T("shop.upgrades");
        BuildUpgradeSection(18f, -14f, 676f, true);
    }

    private void BuildUpgradeSection(float x, float y, float width, bool showHelp)
    {
        if (showHelp)
        {
            AddShopObject(CreateText(GameLocalization.T("shop.upgrade_help"), shopContent, new Vector2(x, y), new Vector2(width, 22f), 14, FontStyle.Normal, TextAnchor.MiddleLeft).gameObject);
            y -= 34f;
        }

        string detectorTitle = metalDetector != null ? metalDetector.CurrentDetectorName : GameLocalization.T("shop.metal_detector");
        string detectorPrice = shop != null && metalDetector != null && metalDetector.CanUpgradeDetector ? "$" + shop.detectorUpgradeCost : GameLocalization.T("shop.maxed");
        string detectorDescription = metalDetector != null && metalDetector.CanUpgradeDetector
            ? GameLocalization.TFormat("shop.detector_next", metalDetector.GetDetectorName(metalDetector.DetectorTier + 1))
            : GameLocalization.T("shop.detector_maxed");
        float cardHeight = width < 400f ? 54f : 60f;
        float gap = width < 400f ? 8f : 10f;

        AddShopObject(CreateUpgradeCard("detector", detectorTitle, detectorDescription, detectorPrice, new Vector2(x, y), width, cardHeight, () =>
        {
            shop?.TryBuyDetectorUpgrade();
            shopDirty = true;
        }).gameObject);

        AddShopObject(CreateUpgradeCard("bag", GameLocalization.T("shop.backpack_size"), GameLocalization.T("shop.backpack_description"), "$" + (shop != null ? shop.inventoryUpgradeCost : 0), new Vector2(x, y - cardHeight - gap), width, cardHeight, () =>
        {
            shop?.TryBuyInventoryUpgrade();
            shopDirty = true;
        }).gameObject);

        string shovelCost = shop != null && shop.shovelUpgraded ? GameLocalization.T("shop.equipped") : "$" + (shop != null ? shop.shovelUpgradeCost : 0);
        AddShopObject(CreateUpgradeCard("shovel", GameLocalization.T("shop.clean_shovel"), GameLocalization.T("shop.clean_shovel_description"), shovelCost, new Vector2(x, y - (cardHeight + gap) * 2f), width, cardHeight, () =>
        {
            shop?.TryBuyShovelUpgrade();
            shopDirty = true;
        }).gameObject);

        string craftingCost = shop != null && shop.craftingUnlocked ? GameLocalization.T("shop.crafting_unlocked") : "$" + (shop != null ? shop.craftingUnlockCost : 0);
        AddShopObject(CreateUpgradeCard("crafting", GameLocalization.T("shop.crafting_station"), GameLocalization.T("shop.crafting_description"), craftingCost, new Vector2(x, y - (cardHeight + gap) * 3f), width, cardHeight, () =>
        {
            shop?.TryBuyCraftingUnlock();
            shopDirty = true;
        }).gameObject);

        AddShopObject(CreateUpgradeCard("map", GameLocalization.T("shop.search_areas"), GameLocalization.T("shop.search_areas_description"), GameLocalization.T("shop.visit"), new Vector2(x, y - (cardHeight + gap) * 4f), width, cardHeight, () =>
        {
            shop?.ShowLocationPurchaseHint();
            shopDirty = true;
        }).gameObject);
    }

    private RectTransform CreateUpgradeCard(string iconText, string title, string description, string price, Vector2 anchoredPosition, float width, float height, UnityEngine.Events.UnityAction action)
    {
        RectTransform card = CreatePanel(title + " Upgrade", anchoredPosition, new Vector2(width, height), new Vector2(0f, 1f), shopContent);
        Image image = card.GetComponent<Image>();
        image.color = new Color(0.08f, 0.11f, 0.14f, 0.96f);

        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        AddHoverEffect(card, new Color(0.08f, 0.11f, 0.14f, 0.96f), new Color(0.12f, 0.2f, 0.27f, 0.98f));

        float priceWidth = width < 400f ? 74f : 104f;
        float priceX = width - priceWidth - 14f;
        float textWidth = Mathf.Max(90f, priceX - 66f - 10f);
        int titleLength = width < 400f ? 20 : 34;
        int titleSize = width < 400f ? 14 : 16;
        int descriptionSize = width < 400f ? 10 : 11;

        CreateIconBadge(card, new Vector2(12f, -8f), new Vector2(38f, 38f), iconText, new Color(0.18f, 0.72f, 1f, 1f));
        CreateText(Shorten(title, titleLength), card, new Vector2(62f, -7f), new Vector2(textWidth, 19f), titleSize, FontStyle.Bold, TextAnchor.MiddleLeft);
        CreateText(Shorten(description, width < 400f ? 34 : 54), card, new Vector2(62f, -29f), new Vector2(textWidth, 17f), descriptionSize, FontStyle.Normal, TextAnchor.MiddleLeft);
        CreateText(price, card, new Vector2(priceX, -14f), new Vector2(priceWidth, 28f), titleSize, FontStyle.Bold, TextAnchor.MiddleCenter);
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
            shopDropText.text = activeShopDragItem != null ? GameLocalization.T("shop.release_to_sell") : GameLocalization.T("shop.drop_to_sell");
        }
    }

    private void AddShopObject(GameObject contentObject)
    {
        shopContentObjects.Add(contentObject);
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
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(0f, 1f);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.anchoredPosition = Vector2.zero;
        fill.sizeDelta = new Vector2(size.x, 0f);

        Image image = fill.gameObject.AddComponent<Image>();
        image.color = color;
        image.type = Image.Type.Simple;
        return image;
    }

    private void SetFoundToastProgressFill(float progress)
    {
        if (foundToastProgressFill == null)
        {
            return;
        }

        RectTransform fillRect = foundToastProgressFill.rectTransform;
        RectTransform background = foundToastProgressFill.transform.parent as RectTransform;
        float width = background != null ? background.sizeDelta.x : fillRect.sizeDelta.x;
        fillRect.sizeDelta = new Vector2(Mathf.Max(0f, width * Mathf.Clamp01(progress)), fillRect.sizeDelta.y);
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
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;

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
                return GameLocalization.T("toast.reward_jackpot");
            case RewardTier.Valuable:
                return GameLocalization.T("toast.reward_valuable");
            case RewardTier.Okay:
                return GameLocalization.T("toast.reward_okay");
            case RewardTier.Trash:
                return GameLocalization.T("toast.reward_trash");
            default:
                return GameLocalization.T("toast.info");
        }
    }

    private string GetToastTitle()
    {
        if (foundToastIsReward)
        {
            return GetRewardTitle(foundToastTier);
        }

        if (foundToastIsDigProgress)
        {
            return GameLocalization.T("toast.digging");
        }

        string message = currentFoundToastMessage ?? "";

        if (message.Contains("Too dark"))
        {
            return GameLocalization.T("toast.too_dark");
        }

        if (message.Contains("Backpack is full"))
        {
            return GameLocalization.T("toast.backpack_full");
        }

        if (message.Contains("No marked target"))
        {
            return GameLocalization.T("toast.no_target");
        }

        if (message.Contains("Searching chest"))
        {
            return GameLocalization.T("hud.action_search");
        }

        return GameLocalization.T("toast.notice");
    }

    private string GetToastBody()
    {
        if (foundToastIsReward)
        {
            return GetRewardSubtitle(foundToastTier);
        }

        if (foundToastIsDigProgress && diggingController != null)
        {
            string progressMessage = currentFoundToastMessage ?? "";

            if (progressMessage.Contains("Chest exposed"))
            {
                return GameLocalization.T("toast.chest_exposed");
            }

            return GameLocalization.TFormat("toast.digging_progress", diggingController.LastDigCurrentHits, diggingController.LastDigRequiredHits);
        }

        string message = currentFoundToastMessage ?? "";

        if (message.Contains("No marked target"))
        {
            return GameLocalization.T("toast.scan_first");
        }

        if (message.Contains("Backpack is full"))
        {
            return GameLocalization.T("toast.backpack_full_body");
        }

        if (message.Contains("Searching chest"))
        {
            return GameLocalization.T("toast.searching_chest");
        }

        if (message.Contains("Chest exposed"))
        {
            return GameLocalization.T("toast.chest_exposed");
        }

        if (message.Contains("Too dark"))
        {
            return GameLocalization.T("toast.sleep_morning");
        }

        return message;
    }

    private string GetRewardSubtitle(RewardTier tier)
    {
        switch (tier)
        {
            case RewardTier.Jackpot:
                return GameLocalization.T("toast.subtitle_jackpot");
            case RewardTier.Valuable:
                return GameLocalization.T("toast.subtitle_valuable");
            case RewardTier.Okay:
                return GameLocalization.T("toast.subtitle_okay");
            case RewardTier.Trash:
                return GameLocalization.T("toast.subtitle_trash");
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
        Vector2 panelSize = new Vector2(580f, 168f);
        Vector2 panelPosition = new Vector2(0f, -95f);
        Vector2 iconPosition = new Vector2(26f, -25f);
        Vector2 iconSize = new Vector2(118f, 118f);
        Vector2 textStart = new Vector2(166f, -24f);
        Vector2 textWidth = new Vector2(382f, 34f);
        Vector2 progressPosition = new Vector2(textStart.x, -62f);
        Vector2 progressSize = new Vector2(textWidth.x, 10f);
        float bodyHeight = 34f;
        int titleSize = 28;
        int subtitleSize = 20;
        int valueSize = 18;

        if (tier == RewardTier.Trash)
        {
            panelSize = new Vector2(510f, 142f);
            iconPosition = new Vector2(24f, -23f);
            iconSize = new Vector2(94f, 94f);
            textStart = new Vector2(142f, -20f);
            textWidth = new Vector2(332f, 30f);
            titleSize = 23;
            subtitleSize = 17;
            valueSize = 15;
        }
        else if (!foundToastIsReward)
        {
            panelSize = foundToastIsDigProgress ? new Vector2(420f, 108f) : new Vector2(390f, 78f);
            panelPosition = new Vector2(0f, -405f);
            iconPosition = foundToastIsDigProgress ? new Vector2(18f, -18f) : new Vector2(0f, 0f);
            iconSize = foundToastIsDigProgress ? new Vector2(50f, 50f) : Vector2.zero;
            textStart = foundToastIsDigProgress ? new Vector2(86f, -14f) : new Vector2(22f, -13f);
            textWidth = foundToastIsDigProgress ? new Vector2(304f, 24f) : new Vector2(346f, 24f);
            progressPosition = foundToastIsDigProgress ? new Vector2(textStart.x, -78f) : new Vector2(textStart.x, -62f);
            progressSize = foundToastIsDigProgress ? new Vector2(textWidth.x, 10f) : new Vector2(textWidth.x, 0f);
            bodyHeight = foundToastIsDigProgress ? 24f : 34f;
            titleSize = 17;
            subtitleSize = 14;
            valueSize = 12;
        }
        else if (tier == RewardTier.Valuable)
        {
            panelSize = new Vector2(680f, 198f);
            iconPosition = new Vector2(28f, -26f);
            iconSize = new Vector2(142f, 142f);
            textStart = new Vector2(196f, -26f);
            textWidth = new Vector2(448f, 38f);
            titleSize = 34;
            subtitleSize = 23;
            valueSize = 20;
        }
        else if (tier == RewardTier.Jackpot)
        {
            panelSize = new Vector2(780f, 232f);
            iconPosition = new Vector2(32f, -30f);
            iconSize = new Vector2(170f, 170f);
            textStart = new Vector2(236f, -28f);
            textWidth = new Vector2(506f, 46f);
            titleSize = 43;
            subtitleSize = 27;
            valueSize = 24;
        }

        foundToastPanel.anchoredPosition = panelPosition;
        foundToastPanel.sizeDelta = panelSize;
        foundToastIcon.rectTransform.anchoredPosition = iconPosition;
        foundToastIcon.rectTransform.sizeDelta = iconSize;
        foundToastTitleText.rectTransform.anchoredPosition = textStart;
        foundToastTitleText.rectTransform.sizeDelta = textWidth;
        foundToastText.rectTransform.anchoredPosition = new Vector2(textStart.x, textStart.y - textWidth.y - 8f);
        foundToastText.rectTransform.sizeDelta = new Vector2(textWidth.x, bodyHeight);
        foundToastValueText.rectTransform.anchoredPosition = new Vector2(textStart.x, textStart.y - textWidth.y - 48f);
        foundToastValueText.rectTransform.sizeDelta = new Vector2(textWidth.x, 34f);
        foundToastTitleText.fontSize = titleSize;
        foundToastText.fontSize = subtitleSize;
        foundToastValueText.fontSize = valueSize;

        if (foundToastProgressFill != null)
        {
            RectTransform progressBackground = foundToastProgressFill.transform.parent as RectTransform;

            if (progressBackground != null)
            {
                progressBackground.SetAsLastSibling();
                progressBackground.anchoredPosition = progressPosition;
                progressBackground.sizeDelta = progressSize;
            }
        }
    }

    private Sprite GetFoundToastIcon()
    {
        if (foundToastIsReward && diggingController != null && diggingController.LastFoundIcon != null)
        {
            return diggingController.LastFoundIcon;
        }

        if (foundToastIsDigProgress)
        {
            string message = currentFoundToastMessage ?? "";
            return GetIconSprite(message.Contains("Chest exposed") ? "treasure" : "shovel");
        }

        return GetIconSprite("treasure");
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
        if (string.IsNullOrEmpty(iconName))
        {
            iconName = "treasure";
        }

        if (iconSprites.TryGetValue(iconName, out Sprite sprite))
        {
            return sprite;
        }

        Texture2D resourceTexture = Resources.Load<Texture2D>("UI/Icons/" + iconName);

        if (resourceTexture != null)
        {
            resourceTexture.filterMode = FilterMode.Bilinear;
            resourceTexture.wrapMode = TextureWrapMode.Clamp;
            sprite = Sprite.Create(resourceTexture, new Rect(0f, 0f, resourceTexture.width, resourceTexture.height), new Vector2(0.5f, 0.5f), 100f);
            iconSprites.Add(iconName, sprite);
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
        else if (iconName == "quest")
        {
            DrawRect(texture, 13, 8, 21, 32, ink);
            DrawRect(texture, 17, 30, 13, 3, clear);
            DrawRect(texture, 17, 23, 12, 3, clear);
            DrawRect(texture, 17, 16, 14, 3, clear);
            DrawCircle(texture, 35, 34, 9, ink);
            DrawRect(texture, 34, 31, 2, 8, clear);
            DrawRect(texture, 34, 26, 2, 2, clear);
        }
        else if (iconName == "detector")
        {
            DrawCircleOutline(texture, 13, 15, 9, ink, 3);
            DrawLine(texture, 20, 20, 34, 32, ink, 4);
            DrawRect(texture, 31, 30, 12, 8, ink);
            DrawRect(texture, 35, 33, 5, 2, clear);
            DrawLine(texture, 40, 38, 45, 43, ink, 3);
        }
        else if (iconName == "shovel")
        {
            DrawLine(texture, 15, 11, 33, 31, ink, 4);
            DrawCircleOutline(texture, 12, 10, 5, ink, 3);
            DrawRect(texture, 31, 29, 9, 8, ink);
            DrawLine(texture, 34, 28, 44, 18, ink, 5);
        }
        else if (iconName == "crafting")
        {
            DrawLine(texture, 13, 11, 31, 29, ink, 4);
            DrawRect(texture, 8, 8, 12, 8, ink);
            DrawRect(texture, 28, 25, 12, 8, ink);
            DrawRect(texture, 11, 34, 26, 5, ink);
            DrawRect(texture, 17, 24, 14, 10, ink);
            DrawRect(texture, 14, 39, 20, 4, ink);
        }
        else if (iconName == "treasure")
        {
            DrawRect(texture, 9, 17, 30, 17, ink);
            DrawRect(texture, 12, 29, 24, 8, ink);
            DrawRect(texture, 22, 22, 5, 8, clear);
            DrawRect(texture, 23, 23, 3, 5, ink);
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
