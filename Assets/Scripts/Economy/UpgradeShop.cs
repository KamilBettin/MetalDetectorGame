using UnityEngine;
using UnityEngine.InputSystem;

public class UpgradeShop : MonoBehaviour
{
    public PlayerInventory playerInventory;
    public MetalDetector metalDetector;
    public Transform shopNpc;
    public bool allowSelling = true;
    public bool allowUpgrades = true;
    public string shopDisplayName = "Trader";

    public int detectorUpgradeCost = 75;
    public int rangeUpgradeCost = 75;
    public int costIncrease = 50;
    public float rangeIncrease = 2f;
    public float maxDetectionRange = 18f;
    public int inventoryUpgradeCost = 120;
    public int inventoryCostIncrease = 100;
    public int shovelUpgradeCost = 160;
    public bool shovelUpgraded;
    public float interactionDistance = 3f;
    public string locationName = "Forest";
    public int locationCost = 350;
    public bool locationUnlocked;

    private string shopMessage = "";
    private float messageTimer;
    private bool isMenuOpen;
    public bool IsMenuOpen => isMenuOpen;
    public string ShopMessage => shopMessage;
    public float MessageTimer => messageTimer;
    public bool CanSellHere => allowSelling;
    public bool CanUpgradeHere => allowUpgrades;

    private void Awake()
    {
        if (playerInventory == null)
        {
            playerInventory = GetComponent<PlayerInventory>();
        }

        if (playerInventory == null)
        {
            playerInventory = gameObject.AddComponent<PlayerInventory>();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && IsPlayerNearShop())
        {
            SetMenuOpen(!isMenuOpen);
        }

        if (isMenuOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetMenuOpen(false);
        }

        if (isMenuOpen && !IsPlayerNearShop())
        {
            SetMenuOpen(false);
        }

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
        }
    }

    public bool IsPlayerNearShop()
    {
        return shopNpc != null && Vector3.Distance(transform.position, shopNpc.position) <= interactionDistance;
    }

    public void SetMenuOpen(bool open)
    {
        isMenuOpen = open;
        GameUIState.SetTraderMenuOpen(isMenuOpen);
    }

    public void TrySellAll()
    {
        if (!allowSelling)
        {
            ShowMessage(GameLocalization.T("shop.msg_trader_upgrades"));
            return;
        }

        if (!CanUseShop())
        {
            return;
        }

        int soldCount = playerInventory.items.Count;
        int earnedMoney = playerInventory.SellAll();

        if (earnedMoney <= 0)
        {
            ShowMessage(GameLocalization.T("shop.msg_nothing_sell"));
            return;
        }

        ShowMessage(GameLocalization.TFormat("shop.msg_sold_treasures", earnedMoney));
        GameEvents.ReportTreasuresSold(soldCount);
        LocalCoopManager.Instance?.ReportTeamStateChanged();
    }

    public void TrySellItem(PlayerInventory.InventorySlot item)
    {
        if (!allowSelling)
        {
            ShowMessage(GameLocalization.T("shop.msg_trader_upgrades"));
            return;
        }

        if (!CanUseShop())
        {
            return;
        }

        if (item == null)
        {
            ShowMessage(GameLocalization.T("shop.msg_choose_item"));
            return;
        }

        if (!playerInventory.SellItem(item))
        {
            ShowMessage(GameLocalization.T("shop.msg_item_missing"));
            return;
        }

        ShowMessage(GameLocalization.TFormat("shop.msg_sold_item", item.itemName, item.value));
        GameEvents.ReportTreasuresSold(1);
        LocalCoopManager.Instance?.ReportTeamStateChanged();
    }

    public void TryBuyRangeUpgrade()
    {
        if (!allowUpgrades)
        {
            ShowMessage(GameLocalization.T("shop.msg_only_buys"));
            return;
        }

        if (!CanUseShop())
        {
            return;
        }

        if (metalDetector == null)
        {
            ShowMessage(GameLocalization.T("shop.msg_not_connected"));
            return;
        }

        if (metalDetector.DetectionRange >= maxDetectionRange)
        {
            ShowMessage(GameLocalization.T("shop.msg_range_maxed"));
            return;
        }

        if (!playerInventory.TrySpendMoney(rangeUpgradeCost))
        {
            ShowMessage(GameLocalization.TFormat("shop.msg_not_enough", rangeUpgradeCost));
            return;
        }

        metalDetector.IncreaseRange(rangeIncrease);
        ShowMessage(GameLocalization.TFormat("shop.msg_range_upgraded", metalDetector.DetectionRange.ToString("0.#")));

        rangeUpgradeCost += costIncrease;
        LocalCoopManager.Instance?.ReportTeamStateChanged();
    }

    public void TryBuyDetectorUpgrade()
    {
        if (!allowUpgrades)
        {
            ShowMessage(GameLocalization.T("shop.msg_only_buys"));
            return;
        }

        if (!CanUseShop())
        {
            return;
        }

        if (metalDetector == null)
        {
            ShowMessage(GameLocalization.T("shop.msg_not_connected"));
            return;
        }

        if (!metalDetector.CanUpgradeDetector)
        {
            ShowMessage(GameLocalization.T("shop.msg_detector_maxed"));
            return;
        }

        if (!playerInventory.TrySpendMoney(detectorUpgradeCost))
        {
            ShowMessage(GameLocalization.TFormat("shop.msg_not_enough", detectorUpgradeCost));
            return;
        }

        metalDetector.TryUpgradeDetector();
        ShowMessage(GameLocalization.TFormat("shop.msg_detector_upgraded", metalDetector.CurrentDetectorName, metalDetector.CurrentScanCells));
        detectorUpgradeCost += costIncrease;
        LocalCoopManager.Instance?.ReportTeamStateChanged();
    }

    public void TryBuyInventoryUpgrade()
    {
        if (!allowUpgrades)
        {
            ShowMessage(GameLocalization.T("shop.msg_only_buys"));
            return;
        }

        if (!CanUseShop())
        {
            return;
        }

        if (!playerInventory.CanUpgradeGrid())
        {
            ShowMessage(GameLocalization.T("shop.msg_backpack_maxed"));
            return;
        }

        if (!playerInventory.TrySpendMoney(inventoryUpgradeCost))
        {
            ShowMessage(GameLocalization.TFormat("shop.msg_not_enough", inventoryUpgradeCost));
            return;
        }

        playerInventory.TryUpgradeGrid();
        ShowMessage(GameLocalization.TFormat("shop.msg_backpack_upgraded", playerInventory.gridSize));

        inventoryUpgradeCost += inventoryCostIncrease;
        LocalCoopManager.Instance?.ReportTeamStateChanged();
    }

    public void TryBuyShovelUpgrade()
    {
        if (!allowUpgrades)
        {
            ShowMessage(GameLocalization.T("shop.msg_only_buys"));
            return;
        }

        if (!CanUseShop())
        {
            return;
        }

        if (shovelUpgraded)
        {
            ShowMessage(GameLocalization.T("shop.msg_shovel_already"));
            return;
        }

        if (!playerInventory.TrySpendMoney(shovelUpgradeCost))
        {
            ShowMessage(GameLocalization.TFormat("shop.msg_not_enough", shovelUpgradeCost));
            return;
        }

        shovelUpgraded = true;
        ShowMessage(GameLocalization.T("shop.msg_shovel_equipped"));
        LocalCoopManager.Instance?.ReportTeamStateChanged();
    }

    public void TryBuyLocation()
    {
        if (!allowUpgrades)
        {
            ShowMessage(GameLocalization.T("shop.msg_only_buys"));
            return;
        }

        ShowLocationPurchaseHint();
    }

    private bool CanUseShop()
    {
        if (playerInventory == null || shopNpc == null)
        {
            ShowMessage(GameLocalization.T("shop.msg_not_connected"));
            return false;
        }

        if (Vector3.Distance(transform.position, shopNpc.position) > interactionDistance)
        {
            ShowMessage(GameLocalization.T("shop.msg_go_trader"));
            return false;
        }

        return true;
    }

    private void ShowMessage(string message)
    {
        shopMessage = message;
        messageTimer = 3f;
    }

    public void ShowLocationPurchaseHint()
    {
        ShowMessage(GameLocalization.T("shop.location_hint"));
    }

    private void OnGUI()
    {
        if (RuntimeGameUI.IsActive)
        {
            return;
        }

        if (playerInventory == null)
        {
            return;
        }

        bool isNearShop = IsPlayerNearShop();

        if (!isMenuOpen)
        {
            if (isNearShop)
            {
                GameGui.DrawToast(new Rect(Screen.width * 0.5f - 150f, Screen.height - 178f, 300f, 40f), GameLocalization.TFormat("shop.prompt_talk", shopDisplayName));
            }

            if (messageTimer > 0f)
            {
                GameGui.DrawToast(new Rect(Screen.width * 0.5f - 180f, Screen.height - 226f, 360f, 38f), shopMessage);
            }

            return;
        }

        string rangeText = metalDetector != null
            ? metalDetector.DetectionRange.ToString("0.#") + "m"
            : GameLocalization.T("shop.msg_not_connected");
        string detectorUpgradeText = metalDetector != null && metalDetector.CanUpgradeDetector
            ? GameLocalization.TFormat("shop.upgrade_detector_to", metalDetector.GetDetectorName(metalDetector.DetectorTier + 1), metalDetector.GetScanCellsForTier(metalDetector.DetectorTier + 1), detectorUpgradeCost)
            : GameLocalization.T("shop.detector_model_maxed");
        string inventoryUpgradeText = playerInventory.CanUpgradeGrid()
            ? GameLocalization.TFormat("shop.upgrade_backpack_to", playerInventory.gridSize, playerInventory.gridSize + 1, inventoryUpgradeCost)
            : GameLocalization.T("shop.backpack_size_maxed");
        string shovelUpgradeText = shovelUpgraded
            ? GameLocalization.T("shop.msg_shovel_equipped")
            : GameLocalization.TFormat("shop.upgrade_shovel", shovelUpgradeCost);

        Rect panelRect = new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 190f, 460f, 380f);
        GameGui.DrawPanel(panelRect, shopDisplayName);
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 44f, 390f, 22f), GameLocalization.TFormat("shop.money_cargo", playerInventory.money, playerInventory.GetInventoryValue()), GameGui.LabelStyle);
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 68f, 390f, 22f), GameLocalization.TFormat("shop.detector_range", rangeText), GameGui.SmallLabelStyle);
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 90f, 390f, 22f), GameLocalization.T("inventory.backpack") + ": " + playerInventory.gridSize + "x" + playerInventory.gridSize, GameGui.SmallLabelStyle);

        if (allowSelling && GameGui.Button(new Rect(panelRect.x + 24f, panelRect.y + 128f, 412f, 34f), GameLocalization.TFormat("shop.sell_all", playerInventory.GetInventoryValue())))
        {
            TrySellAll();
        }

        if (allowUpgrades && GameGui.Button(new Rect(panelRect.x + 24f, panelRect.y + 170f, 412f, 34f), detectorUpgradeText))
        {
            TryBuyDetectorUpgrade();
        }

        if (allowUpgrades && GameGui.Button(new Rect(panelRect.x + 24f, panelRect.y + 212f, 412f, 34f), inventoryUpgradeText))
        {
            TryBuyInventoryUpgrade();
        }

        if (allowUpgrades && GameGui.Button(new Rect(panelRect.x + 24f, panelRect.y + 254f, 412f, 34f), shovelUpgradeText))
        {
            TryBuyShovelUpgrade();
        }

        if (allowUpgrades && GameGui.Button(new Rect(panelRect.x + 24f, panelRect.y + 296f, 412f, 34f), GameLocalization.T("shop.buy_land_signs")))
        {
            TryBuyLocation();
        }

        if (messageTimer > 0f)
        {
            GameGui.DrawToast(new Rect(panelRect.x, panelRect.y + panelRect.height + 10f, panelRect.width, 38f), shopMessage);
        }
    }
}
