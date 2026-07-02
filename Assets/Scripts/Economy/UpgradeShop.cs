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
            ShowMessage("This trader handles upgrades.");
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
            ShowMessage("You have nothing to sell.");
            return;
        }

        ShowMessage("Sold treasures for $" + earnedMoney + ".");
        GameEvents.ReportTreasuresSold(soldCount);
        LocalCoopManager.Instance?.ReportTeamStateChanged();
    }

    public void TrySellItem(PlayerInventory.InventorySlot item)
    {
        if (!allowSelling)
        {
            ShowMessage("This trader handles upgrades.");
            return;
        }

        if (!CanUseShop())
        {
            return;
        }

        if (item == null)
        {
            ShowMessage("Choose an item to sell.");
            return;
        }

        if (!playerInventory.SellItem(item))
        {
            ShowMessage("That item is no longer in your backpack.");
            return;
        }

        ShowMessage("Sold " + item.itemName + " for $" + item.value + ".");
        GameEvents.ReportTreasuresSold(1);
        LocalCoopManager.Instance?.ReportTeamStateChanged();
    }

    public void TryBuyRangeUpgrade()
    {
        if (!allowUpgrades)
        {
            ShowMessage("This trader only buys treasures.");
            return;
        }

        if (!CanUseShop())
        {
            return;
        }

        if (metalDetector == null)
        {
            ShowMessage("Shop is not connected.");
            return;
        }

        if (metalDetector.DetectionRange >= maxDetectionRange)
        {
            ShowMessage("Detector range is maxed.");
            return;
        }

        if (!playerInventory.TrySpendMoney(rangeUpgradeCost))
        {
            ShowMessage("Not enough money. Need $" + rangeUpgradeCost + ".");
            return;
        }

        metalDetector.IncreaseRange(rangeIncrease);
        ShowMessage("Range upgraded to " + metalDetector.DetectionRange.ToString("0.#") + "m.");

        rangeUpgradeCost += costIncrease;
        LocalCoopManager.Instance?.ReportTeamStateChanged();
    }

    public void TryBuyDetectorUpgrade()
    {
        if (!allowUpgrades)
        {
            ShowMessage("This trader only buys treasures.");
            return;
        }

        if (!CanUseShop())
        {
            return;
        }

        if (metalDetector == null)
        {
            ShowMessage("Shop is not connected.");
            return;
        }

        if (!metalDetector.CanUpgradeDetector)
        {
            ShowMessage("Detector model is maxed.");
            return;
        }

        if (!playerInventory.TrySpendMoney(detectorUpgradeCost))
        {
            ShowMessage("Not enough money. Need $" + detectorUpgradeCost + ".");
            return;
        }

        metalDetector.TryUpgradeDetector();
        ShowMessage("Detector upgraded: " + metalDetector.CurrentDetectorName + " (" + metalDetector.CurrentScanCells + "x" + metalDetector.CurrentScanCells + ").");
        detectorUpgradeCost += costIncrease;
        LocalCoopManager.Instance?.ReportTeamStateChanged();
    }

    public void TryBuyInventoryUpgrade()
    {
        if (!allowUpgrades)
        {
            ShowMessage("This trader only buys treasures.");
            return;
        }

        if (!CanUseShop())
        {
            return;
        }

        if (!playerInventory.CanUpgradeGrid())
        {
            ShowMessage("Backpack size is maxed.");
            return;
        }

        if (!playerInventory.TrySpendMoney(inventoryUpgradeCost))
        {
            ShowMessage("Not enough money. Need $" + inventoryUpgradeCost + ".");
            return;
        }

        playerInventory.TryUpgradeGrid();
        ShowMessage("Backpack upgraded to " + playerInventory.gridSize + "x" + playerInventory.gridSize + ".");

        inventoryUpgradeCost += inventoryCostIncrease;
        LocalCoopManager.Instance?.ReportTeamStateChanged();
    }

    public void TryBuyShovelUpgrade()
    {
        if (!allowUpgrades)
        {
            ShowMessage("This trader only buys treasures.");
            return;
        }

        if (!CanUseShop())
        {
            return;
        }

        if (shovelUpgraded)
        {
            ShowMessage("Shovel is already upgraded.");
            return;
        }

        if (!playerInventory.TrySpendMoney(shovelUpgradeCost))
        {
            ShowMessage("Not enough money. Need $" + shovelUpgradeCost + ".");
            return;
        }

        shovelUpgraded = true;
        ShowMessage("Clean shovel equipped.");
        LocalCoopManager.Instance?.ReportTeamStateChanged();
    }

    public void TryBuyLocation()
    {
        if (!allowUpgrades)
        {
            ShowMessage("This trader only buys treasures.");
            return;
        }

        ShowLocationPurchaseHint();
    }

    private bool CanUseShop()
    {
        if (playerInventory == null || shopNpc == null)
        {
            ShowMessage("Shop is not connected.");
            return false;
        }

        if (Vector3.Distance(transform.position, shopNpc.position) > interactionDistance)
        {
            ShowMessage("Go to the trader first.");
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
        ShowMessage("Buy land at the sign next to each search area.");
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
                GameGui.DrawToast(new Rect(Screen.width * 0.5f - 150f, Screen.height - 178f, 300f, 40f), "Press E to talk to " + shopDisplayName);
            }

            if (messageTimer > 0f)
            {
                GameGui.DrawToast(new Rect(Screen.width * 0.5f - 180f, Screen.height - 226f, 360f, 38f), shopMessage);
            }

            return;
        }

        string rangeText = metalDetector != null
            ? metalDetector.DetectionRange.ToString("0.#") + "m"
            : "not connected";
        string detectorUpgradeText = metalDetector != null && metalDetector.CanUpgradeDetector
            ? "Upgrade detector to " + metalDetector.GetDetectorName(metalDetector.DetectorTier + 1) + " " + metalDetector.GetScanCellsForTier(metalDetector.DetectorTier + 1) + "x" + metalDetector.GetScanCellsForTier(metalDetector.DetectorTier + 1) + " ($" + detectorUpgradeCost + ")"
            : "Detector model maxed";
        string inventoryUpgradeText = playerInventory.CanUpgradeGrid()
            ? "Upgrade backpack " + playerInventory.gridSize + "x" + playerInventory.gridSize + " -> " + (playerInventory.gridSize + 1) + "x" + (playerInventory.gridSize + 1) + " ($" + inventoryUpgradeCost + ")"
            : "Backpack size maxed";
        string shovelUpgradeText = shovelUpgraded
            ? "Clean shovel equipped"
            : "Upgrade shovel ($" + shovelUpgradeCost + ")";

        Rect panelRect = new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 190f, 460f, 380f);
        GameGui.DrawPanel(panelRect, shopDisplayName);
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 44f, 390f, 22f), "Money: $" + playerInventory.money + " | Cargo value: $" + playerInventory.GetInventoryValue(), GameGui.LabelStyle);
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 68f, 390f, 22f), "Detector range: " + rangeText, GameGui.SmallLabelStyle);
        GUI.Label(new Rect(panelRect.x + 24f, panelRect.y + 90f, 390f, 22f), "Backpack: " + playerInventory.gridSize + "x" + playerInventory.gridSize, GameGui.SmallLabelStyle);

        if (allowSelling && GameGui.Button(new Rect(panelRect.x + 24f, panelRect.y + 128f, 412f, 34f), "Sell all treasures ($" + playerInventory.GetInventoryValue() + ")"))
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

        if (allowUpgrades && GameGui.Button(new Rect(panelRect.x + 24f, panelRect.y + 296f, 412f, 34f), "Buy land at plot signs"))
        {
            TryBuyLocation();
        }

        if (messageTimer > 0f)
        {
            GameGui.DrawToast(new Rect(panelRect.x, panelRect.y + panelRect.height + 10f, panelRect.width, 38f), shopMessage);
        }
    }
}
