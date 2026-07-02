using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHome : MonoBehaviour
{
    private const string WatchFragmentName = "Watch Fragment";
    private const string WorkingWatchName = "Working Pocket Watch";
    private const int WorkingWatchValue = 95;
    private const string CraftingBoardResourcePath = "UI/CraftingBoard";

    public Transform interactionPoint;
    public Transform promptAnchor;
    public float interactionDistance = 4.5f;
    public PlayerInventory playerInventory;
    public Transform player;
    public DetectorBattery detectorBattery;

    private readonly List<PlayerInventory.InventorySlot> storedItems = new List<PlayerInventory.InventorySlot>();
    private readonly PlayerInventory.InventorySlot[] craftingSlots = new PlayerInventory.InventorySlot[9];
    private bool isMenuOpen;
    private bool isCraftingOpen;
    private string message = "";
    private float messageTimer;
    private Texture2D craftingBoardTexture;
    private CraftingDragSource craftingDragSource = CraftingDragSource.None;
    private PlayerInventory.InventorySlot draggedCraftingItem;
    private PlayerInventory.InventorySlot draggedBackpackItem;
    private int draggedCraftingSlotIndex = -1;

    private enum CraftingDragSource
    {
        None,
        Backpack,
        CraftingSlot,
        Result
    }

    public bool IsMenuOpen => isMenuOpen;
    public int StoredItemCount => storedItems.Count;
    public int StoredValue => GetStoredValue();
    public Vector3 PromptPosition => promptAnchor != null ? promptAnchor.position : transform.position + Vector3.up * 2f;

    private void Update()
    {
        ResolveReferences();

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && IsPlayerInRange())
        {
            if (!GameUIState.AnyMenuOpen || isMenuOpen)
            {
                SetMenuOpen(!isMenuOpen);
            }
        }

        if (isMenuOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetMenuOpen(false);
        }

        if (isMenuOpen && !IsPlayerInRange())
        {
            SetMenuOpen(false);
        }
    }

    public bool IsPlayerInRange()
    {
        ResolveReferences();

        if (player == null)
        {
            return false;
        }

        Vector3 targetPosition = interactionPoint != null ? interactionPoint.position : transform.position;
        return Vector3.Distance(player.position, targetPosition) <= interactionDistance;
    }

    public void SetMenuOpen(bool open)
    {
        if (!open)
        {
            ReturnCraftingItemsToBackpack();
            isCraftingOpen = false;
        }

        isMenuOpen = open;
        GameUIState.SetHomeMenuOpen(isMenuOpen);
    }

    public void StoreBackpack()
    {
        if (playerInventory == null || playerInventory.items.Count == 0)
        {
            ShowMessage("Backpack is empty.");
            return;
        }

        int storedCount = playerInventory.items.Count;
        int storedValue = playerInventory.GetInventoryValue();

        foreach (PlayerInventory.InventorySlot item in playerInventory.items)
        {
            storedItems.Add(CloneItem(item));
        }

        playerInventory.items.Clear();
        ShowMessage("Stored " + storedCount + " item(s), value $" + storedValue + ".");
        LocalCoopManager.Instance?.ReportHomeStorageChanged(this);
    }

    public void TakeStoredItems()
    {
        if (playerInventory == null)
        {
            ShowMessage("No backpack found.");
            return;
        }

        if (storedItems.Count == 0)
        {
            ShowMessage("Storage is empty.");
            return;
        }

        int movedCount = 0;
        int movedValue = 0;

        for (int i = storedItems.Count - 1; i >= 0; i--)
        {
            PlayerInventory.InventorySlot item = storedItems[i];

            if (!playerInventory.AddItem(item.itemName, item.value, item.width, item.height, item.icon))
            {
                continue;
            }

            movedCount++;
            movedValue += item.value;
            storedItems.RemoveAt(i);
        }

        if (movedCount == 0)
        {
            ShowMessage("Backpack is full.");
            return;
        }

        ShowMessage("Took " + movedCount + " item(s), value $" + movedValue + ".");
        LocalCoopManager.Instance?.ReportHomeStorageChanged(this);
    }

    public void Sleep()
    {
        ResolveReferences();

        LocalCoopManager coop = LocalCoopManager.Instance;

        if (DayNightCycle.Instance != null && !DayNightCycle.Instance.CanSleep)
        {
            ShowMessage("You can sleep after 20:00.");
            return;
        }

        if (coop != null && coop.IsRunning)
        {
            if (coop.RequestTeamSleep(out string teamSleepMessage))
            {
                ShowMessage(teamSleepMessage);
                return;
            }
        }

        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.SleepUntilMorning();
            coop?.ReportTeamStateChanged();
            ShowMessage("You slept until morning. Treasures reset.");
            return;
        }

        if (detectorBattery != null)
        {
            detectorBattery.charge = detectorBattery.maxCharge;
        }

        ShowMessage("You slept. Detector battery refilled.");
    }

    public List<PlayerInventory.InventorySlot> ExportStoredItems()
    {
        List<PlayerInventory.InventorySlot> exportedItems = new List<PlayerInventory.InventorySlot>();

        foreach (PlayerInventory.InventorySlot item in storedItems)
        {
            exportedItems.Add(CloneItem(item));
        }

        return exportedItems;
    }

    public void ImportStoredItems(IEnumerable<PlayerInventory.InventorySlot> importedItems)
    {
        storedItems.Clear();

        if (importedItems == null)
        {
            return;
        }

        foreach (PlayerInventory.InventorySlot item in importedItems)
        {
            storedItems.Add(CloneItem(item));
        }
    }

    public void OpenCrafting()
    {
        isCraftingOpen = true;
        ShowMessage("");
    }

    public void CloseCrafting()
    {
        ClearCraftingDrag();
        ReturnCraftingItemsToBackpack();
        isCraftingOpen = false;
    }

    public void CraftSelectedRecipe()
    {
        TryClaimCraftingResult();
    }

    public static PlayerHome FindClosestHomeInRange()
    {
        PlayerHome[] homes = FindObjectsByType<PlayerHome>();
        PlayerHome closestHome = null;
        float closestDistance = float.MaxValue;

        foreach (PlayerHome home in homes)
        {
            if (home == null || !home.IsPlayerInRange())
            {
                continue;
            }

            Transform playerTransform = home.player;
            Vector3 targetPosition = home.interactionPoint != null ? home.interactionPoint.position : home.transform.position;
            float distance = playerTransform != null ? Vector3.Distance(playerTransform.position, targetPosition) : 0f;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHome = home;
            }
        }

        return closestHome;
    }

    public static bool AnyHomeInteractionInRange()
    {
        return FindClosestHomeInRange() != null;
    }

    private void ResolveReferences()
    {
        if (playerInventory == null)
        {
            playerInventory = FindAnyObjectByType<PlayerInventory>();
        }

        if (player == null && playerInventory != null)
        {
            player = playerInventory.transform;
        }

        if (detectorBattery == null)
        {
            detectorBattery = FindAnyObjectByType<DetectorBattery>();
        }
    }

    private bool TryPlaceItemInCrafting(PlayerInventory.InventorySlot item)
    {
        if (playerInventory == null || item == null)
        {
            return false;
        }

        int emptySlot = GetFirstEmptyCraftingSlot();

        if (emptySlot < 0)
        {
            ShowMessage("Crafting grid is full.");
            return false;
        }

        if (!playerInventory.RemoveItem(item))
        {
            return false;
        }

        craftingSlots[emptySlot] = CloneItem(item);
        ShowMessage("Placed " + item.itemName + ".");
        return true;
    }

    private void ReturnCraftingSlotToBackpack(int slotIndex)
    {
        if (playerInventory == null || slotIndex < 0 || slotIndex >= craftingSlots.Length)
        {
            return;
        }

        PlayerInventory.InventorySlot item = craftingSlots[slotIndex];

        if (item == null)
        {
            return;
        }

        if (!playerInventory.AddItem(item.itemName, item.value, item.width, item.height, item.icon))
        {
            ShowMessage("Backpack is full.");
            return;
        }

        craftingSlots[slotIndex] = null;
        ShowMessage("Returned " + item.itemName + ".");
    }

    private void ReturnCraftingItemsToBackpack()
    {
        if (playerInventory == null)
        {
            return;
        }

        for (int i = 0; i < craftingSlots.Length; i++)
        {
            PlayerInventory.InventorySlot item = craftingSlots[i];

            if (item == null)
            {
                continue;
            }

            if (!playerInventory.AddItem(item.itemName, item.value, item.width, item.height, item.icon))
            {
                continue;
            }

            craftingSlots[i] = null;
        }
    }

    private int GetFirstEmptyCraftingSlot()
    {
        for (int i = 0; i < craftingSlots.Length; i++)
        {
            if (craftingSlots[i] == null)
            {
                return i;
            }
        }

        return -1;
    }

    private bool TryGetWatchRecipeRow(out int rowStart)
    {
        for (int row = 0; row < 3; row++)
        {
            int start = row * 3;

            if (IsWatchFragment(craftingSlots[start])
                && IsWatchFragment(craftingSlots[start + 1])
                && IsWatchFragment(craftingSlots[start + 2]))
            {
                rowStart = start;
                return true;
            }
        }

        rowStart = -1;
        return false;
    }

    private bool IsWatchFragment(PlayerInventory.InventorySlot item)
    {
        return item != null && item.itemName == WatchFragmentName;
    }

    private Texture2D GetCraftingBoardTexture()
    {
        if (craftingBoardTexture == null)
        {
            craftingBoardTexture = Resources.Load<Texture2D>(CraftingBoardResourcePath);
        }

        return craftingBoardTexture;
    }

    private int GetStoredValue()
    {
        int value = 0;

        foreach (PlayerInventory.InventorySlot item in storedItems)
        {
            value += item.value;
        }

        return value;
    }

    private PlayerInventory.InventorySlot CloneItem(PlayerInventory.InventorySlot item)
    {
        return new PlayerInventory.InventorySlot
        {
            itemName = item.itemName,
            value = item.value,
            icon = item.icon,
            width = item.width,
            height = item.height,
            gridX = item.gridX,
            gridY = item.gridY
        };
    }

    private void ShowMessage(string value)
    {
        message = value;
        messageTimer = 2.8f;
    }

    private void OnGUI()
    {
        if (isMenuOpen)
        {
            DrawHomeMenu();
            return;
        }

        if (IsPlayerInRange() && !GameUIState.AnyMenuOpen)
        {
            GameGui.DrawToast(new Rect(Screen.width * 0.5f - 190f, Screen.height - 178f, 380f, 40f), "E - Use home");
        }
    }

    private void DrawHomeMenu()
    {
        if (isCraftingOpen)
        {
            DrawCraftingMenu();
            return;
        }

        Rect panel = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 163f, 440f, 326f);
        GameGui.DrawPanel(panel, "Home");

        int backpackCount = playerInventory != null ? playerInventory.items.Count : 0;
        int backpackValue = playerInventory != null ? playerInventory.GetInventoryValue() : 0;
        GUI.Label(new Rect(panel.x + 18f, panel.y + 46f, panel.width - 36f, 24f), "Backpack: " + backpackCount + " item(s), $" + backpackValue, GameGui.LabelStyle);
        GUI.Label(new Rect(panel.x + 18f, panel.y + 72f, panel.width - 36f, 24f), "Storage: " + StoredItemCount + " item(s), $" + StoredValue, GameGui.LabelStyle);

        if (GameGui.Button(new Rect(panel.x + 18f, panel.y + 112f, panel.width - 36f, 38f), "Store backpack"))
        {
            StoreBackpack();
        }

        if (GameGui.Button(new Rect(panel.x + 18f, panel.y + 158f, panel.width - 36f, 38f), "Take stored items"))
        {
            TakeStoredItems();
        }

        LocalCoopManager coop = LocalCoopManager.Instance;
        bool teamSleepActive = coop != null && coop.IsRunning;
        string sleepButtonText = teamSleepActive && coop.IsLocalPlayerSleepReady ? "Waiting for team..." : "Sleep";

        if (GameGui.Button(new Rect(panel.x + 18f, panel.y + 204f, panel.width - 36f, 38f), sleepButtonText))
        {
            Sleep();
        }

        if (GameGui.Button(new Rect(panel.x + 18f, panel.y + 250f, panel.width - 36f, 38f), "Crafting"))
        {
            OpenCrafting();
        }

        string footerText = messageTimer > 0f ? message : "ESC - Close";

        if (teamSleepActive && coop.HasTeamSleepVote && !string.IsNullOrEmpty(coop.TeamSleepStatusText))
        {
            footerText = coop.TeamSleepStatusText;
        }

        GUI.Label(new Rect(panel.x + 18f, panel.y + 294f, panel.width - 36f, 22f), footerText, GameGui.HintStyle);
    }

    private void DrawCraftingMenu()
    {
        float availableWidth = Mathf.Max(720f, Screen.width - 40f);
        float gap = 18f;
        float backpackWidth = Mathf.Clamp(Screen.width * 0.34f, 420f, 620f);
        float boardWidth = Mathf.Clamp(availableWidth - backpackWidth - gap, 620f, 980f);

        if (boardWidth + backpackWidth + gap > availableWidth)
        {
            backpackWidth = Mathf.Clamp(availableWidth * 0.34f, 340f, 520f);
            boardWidth = Mathf.Max(520f, availableWidth - backpackWidth - gap);
        }

        float boardHeight = boardWidth / PlayerInventory.BoardAspect;
        float backpackHeight = backpackWidth / PlayerInventory.BoardAspect;
        float maxBoardHeight = Mathf.Max(420f, Screen.height - 70f);

        if (boardHeight > maxBoardHeight)
        {
            boardHeight = maxBoardHeight;
            boardWidth = boardHeight * PlayerInventory.BoardAspect;
            backpackWidth = Mathf.Min(backpackWidth, Mathf.Max(320f, availableWidth - boardWidth - gap));
            backpackHeight = backpackWidth / PlayerInventory.BoardAspect;
        }

        float totalWidth = boardWidth + backpackWidth + gap;
        Rect boardRect = new Rect(Screen.width * 0.5f - totalWidth * 0.5f, Screen.height * 0.5f - boardHeight * 0.5f - 8f, boardWidth, boardHeight);
        Texture2D boardTexture = GetCraftingBoardTexture();
        Rect contentRect = boardRect;
        Rect backpackRect = new Rect(contentRect.x + contentRect.width + gap, contentRect.y + contentRect.height * 0.5f - backpackHeight * 0.5f, backpackWidth, backpackHeight);

        if (boardTexture != null)
        {
            contentRect = PlayerInventory.GetFittedTextureRect(boardRect, boardTexture);
            GUI.DrawTexture(contentRect, boardTexture, ScaleMode.StretchToFill, true);
            backpackRect = new Rect(contentRect.x + contentRect.width + gap, contentRect.y + contentRect.height * 0.5f - backpackHeight * 0.5f, backpackWidth, backpackHeight);
        }
        else
        {
            GameGui.DrawPanel(boardRect, "Crafting");
        }

        DrawCraftingGrid(contentRect);
        DrawCraftingResult(contentRect);
        DrawCraftingBackpack(backpackRect);

        GUI.Label(new Rect(contentRect.x + 42f, contentRect.y + contentRect.height - 48f, contentRect.width - 250f, 28f), messageTimer > 0f ? message : "ESC - Close", GameGui.HintStyle);
        HandleCraftingDragRelease(contentRect, backpackRect);
        DrawCraftingDragGhost();
    }

    private void DrawCraftingGrid(Rect boardRect)
    {
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                int index = y * 3 + x;
                Rect slotRect = GetCraftingSlotRect(boardRect, x, y);
                PlayerInventory.InventorySlot item = craftingSlots[index];

                if (item != null)
                {
                    PlayerInventory.DrawInventoryItem(slotRect, item);
                    HandleCraftingSlotDragStart(index, slotRect, item);
                }
            }
        }
    }

    private void DrawCraftingResult(Rect boardRect)
    {
        Rect resultRect = GetCraftingResultRect(boardRect);

        if (TryGetWatchRecipeRow(out _))
        {
            PlayerInventory.InventorySlot resultItem = CreateWorkingWatchItem();
            PlayerInventory.DrawInventoryItem(resultRect, resultItem);
            HandleCraftingResultDragStart(resultRect, resultItem);
        }
    }

    private void HandleCraftingResultDragStart(Rect resultRect, PlayerInventory.InventorySlot resultItem)
    {
        Event currentEvent = Event.current;

        if (currentEvent == null
            || currentEvent.type != EventType.MouseDown
            || currentEvent.button != 0
            || craftingDragSource != CraftingDragSource.None
            || resultItem == null
            || !resultRect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        craftingDragSource = CraftingDragSource.Result;
        draggedCraftingItem = CloneItem(resultItem);
        draggedBackpackItem = null;
        draggedCraftingSlotIndex = -1;
        currentEvent.Use();
    }

    private PlayerInventory.InventorySlot CreateWorkingWatchItem()
    {
        return new PlayerInventory.InventorySlot
            {
                itemName = WorkingWatchName,
                value = WorkingWatchValue,
                width = 2,
                height = 1
            };
    }

    private void DrawCraftingBackpack(Rect backpackRect)
    {
        if (playerInventory == null)
        {
            return;
        }

        playerInventory.DrawInventoryBoard(backpackRect, false, null);
        HandleBackpackDragStart(backpackRect);
    }

    private void HandleBackpackDragStart(Rect backpackRect)
    {
        Event currentEvent = Event.current;

        if (currentEvent == null
            || currentEvent.type != EventType.MouseDown
            || currentEvent.button != 0
            || craftingDragSource != CraftingDragSource.None
            || playerInventory == null)
        {
            return;
        }

        PlayerInventory.InventorySlot item = GetBackpackItemAt(backpackRect, currentEvent.mousePosition);

        if (item == null)
        {
            return;
        }

        craftingDragSource = CraftingDragSource.Backpack;
        draggedBackpackItem = item;
        draggedCraftingItem = CloneItem(item);
        draggedCraftingSlotIndex = -1;
        currentEvent.Use();
    }

    private void HandleCraftingSlotDragStart(int slotIndex, Rect slotRect, PlayerInventory.InventorySlot item)
    {
        Event currentEvent = Event.current;

        if (currentEvent == null
            || currentEvent.type != EventType.MouseDown
            || currentEvent.button != 0
            || craftingDragSource != CraftingDragSource.None
            || !slotRect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        craftingDragSource = CraftingDragSource.CraftingSlot;
        draggedCraftingSlotIndex = slotIndex;
        draggedCraftingItem = CloneItem(item);
        draggedBackpackItem = null;
        currentEvent.Use();
    }

    private void HandleCraftingDragRelease(Rect boardRect, Rect backpackRect)
    {
        Event currentEvent = Event.current;

        if (currentEvent == null
            || currentEvent.type != EventType.MouseUp
            || currentEvent.button != 0
            || craftingDragSource == CraftingDragSource.None)
        {
            return;
        }

        Vector2 mousePosition = currentEvent.mousePosition;
        int targetSlot = GetCraftingSlotIndexAt(boardRect, mousePosition);

        if (craftingDragSource == CraftingDragSource.Backpack)
        {
            DropBackpackItemIntoCrafting(targetSlot);
        }
        else if (craftingDragSource == CraftingDragSource.CraftingSlot)
        {
            DropCraftingSlotItem(targetSlot, backpackRect.Contains(mousePosition));
        }
        else if (craftingDragSource == CraftingDragSource.Result && backpackRect.Contains(mousePosition))
        {
            TryClaimCraftingResult();
        }

        ClearCraftingDrag();
        currentEvent.Use();
    }

    private void DropBackpackItemIntoCrafting(int targetSlot)
    {
        if (playerInventory == null || draggedBackpackItem == null || targetSlot < 0)
        {
            return;
        }

        if (craftingSlots[targetSlot] != null)
        {
            ShowMessage("Crafting slot is occupied.");
            return;
        }

        if (!playerInventory.RemoveItem(draggedBackpackItem))
        {
            return;
        }

        craftingSlots[targetSlot] = CloneItem(draggedBackpackItem);
        ShowMessage("Placed " + draggedBackpackItem.itemName + ".");
    }

    private bool TryClaimCraftingResult()
    {
        if (playerInventory == null)
        {
            ShowMessage("No backpack found.");
            return false;
        }

        if (!TryGetWatchRecipeRow(out int rowStart))
        {
            return false;
        }

        if (!playerInventory.AddItem(WorkingWatchName, WorkingWatchValue, 2, 1))
        {
            ShowMessage("Need 2x1 space in backpack.");
            return false;
        }

        for (int i = rowStart; i < rowStart + 3; i++)
        {
            craftingSlots[i] = null;
        }

        ShowMessage("Crafted " + WorkingWatchName + "!");
        return true;
    }

    private void DropCraftingSlotItem(int targetSlot, bool droppedOnBackpack)
    {
        if (draggedCraftingSlotIndex < 0 || draggedCraftingSlotIndex >= craftingSlots.Length)
        {
            return;
        }

        if (droppedOnBackpack)
        {
            ReturnCraftingSlotToBackpack(draggedCraftingSlotIndex);
            return;
        }

        if (targetSlot < 0 || targetSlot == draggedCraftingSlotIndex)
        {
            return;
        }

        PlayerInventory.InventorySlot sourceItem = craftingSlots[draggedCraftingSlotIndex];
        craftingSlots[draggedCraftingSlotIndex] = craftingSlots[targetSlot];
        craftingSlots[targetSlot] = sourceItem;
    }

    private PlayerInventory.InventorySlot GetBackpackItemAt(Rect backpackRect, Vector2 mousePosition)
    {
        if (playerInventory == null)
        {
            return null;
        }

        for (int i = playerInventory.items.Count - 1; i >= 0; i--)
        {
            PlayerInventory.InventorySlot item = playerInventory.items[i];
            Rect itemRect = PlayerInventory.GetInventoryItemRect(backpackRect, playerInventory.gridSize, item);

            if (itemRect.Contains(mousePosition))
            {
                return item;
            }
        }

        return null;
    }

    private int GetCraftingSlotIndexAt(Rect boardRect, Vector2 mousePosition)
    {
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                Rect slotRect = GetCraftingSlotRect(boardRect, x, y);

                if (slotRect.Contains(mousePosition))
                {
                    return y * 3 + x;
                }
            }
        }

        return -1;
    }

    private void DrawCraftingDragGhost()
    {
        if (craftingDragSource == CraftingDragSource.None || draggedCraftingItem == null)
        {
            return;
        }

        Vector2 mousePosition = Event.current != null ? Event.current.mousePosition : Vector2.zero;
        float size = Mathf.Clamp(Screen.height * 0.08f, 56f, 88f);
        Rect ghostRect = new Rect(mousePosition.x - size * 0.5f, mousePosition.y - size * 0.5f, size, size);
        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.86f);
        PlayerInventory.DrawInventoryItem(ghostRect, draggedCraftingItem);
        GUI.color = previousColor;
    }

    private void ClearCraftingDrag()
    {
        craftingDragSource = CraftingDragSource.None;
        draggedCraftingItem = null;
        draggedBackpackItem = null;
        draggedCraftingSlotIndex = -1;
    }

    private static Rect GetCraftingSlotRect(Rect boardRect, int x, int y)
    {
        float slotWidth = boardRect.width * (108f / 1448f);
        float slotHeight = boardRect.height * (112f / 1086f);
        float stepX = boardRect.width * (131.5f / 1448f);
        float stepY = boardRect.height * (136.5f / 1086f);
        float startX = boardRect.x + boardRect.width * (362f / 1448f);
        float startY = boardRect.y + boardRect.height * (378f / 1086f);
        return new Rect(startX + x * stepX, startY + y * stepY, slotWidth, slotHeight);
    }

    private static Rect GetCraftingResultRect(Rect boardRect)
    {
        return new Rect(
            boardRect.x + boardRect.width * (848f / 1448f),
            boardRect.y + boardRect.height * (501f / 1086f),
            boardRect.width * (206f / 1448f),
            boardRect.height * (202f / 1086f));
    }
}

public static class PlayerHomeBootstrapper
{
    private const string HomeRootName = "Player Home";
    private static readonly Vector2 HomePosition = new Vector2(-700f, -710f);

    public static void EnsurePlayerHome()
    {
        if (Object.FindAnyObjectByType<PlayerHome>() != null || GameObject.Find(HomeRootName) != null)
        {
            return;
        }

        CreateHome();
    }

    private static void CreateHome()
    {
        float groundY = GetGroundY(HomePosition.x, HomePosition.y);
        GameObject homeObject = new GameObject(HomeRootName);
        homeObject.transform.position = new Vector3(HomePosition.x, groundY, HomePosition.y);
        homeObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        Material wallMaterial = CreateMaterial(new Color(0.46f, 0.28f, 0.14f, 1f));
        Material trimMaterial = CreateMaterial(new Color(0.28f, 0.16f, 0.08f, 1f));
        Material roofMaterial = CreateMaterial(new Color(0.22f, 0.09f, 0.055f, 1f));
        Material floorMaterial = CreateMaterial(new Color(0.36f, 0.29f, 0.2f, 1f));
        Material bedMaterial = CreateMaterial(new Color(0.58f, 0.18f, 0.15f, 1f));
        Material chestMaterial = CreateMaterial(new Color(0.52f, 0.32f, 0.13f, 1f));

        CreateCube(homeObject.transform, "Foundation", new Vector3(0f, 0.18f, 0f), Vector3.zero, new Vector3(8.6f, 0.36f, 6.3f), floorMaterial);
        CreateCube(homeObject.transform, "Back Wall", new Vector3(0f, 1.85f, 3f), Vector3.zero, new Vector3(8.4f, 3.25f, 0.28f), wallMaterial);
        CreateCube(homeObject.transform, "Left Wall", new Vector3(-4.05f, 1.85f, 0f), Vector3.zero, new Vector3(0.28f, 3.25f, 6f), wallMaterial);
        CreateCube(homeObject.transform, "Right Wall", new Vector3(4.05f, 1.85f, 0f), Vector3.zero, new Vector3(0.28f, 3.25f, 6f), wallMaterial);
        CreateCube(homeObject.transform, "Front Wall Left", new Vector3(-2.45f, 1.85f, -3f), Vector3.zero, new Vector3(3.2f, 3.25f, 0.28f), wallMaterial);
        CreateCube(homeObject.transform, "Front Wall Right", new Vector3(2.45f, 1.85f, -3f), Vector3.zero, new Vector3(3.2f, 3.25f, 0.28f), wallMaterial);
        CreateCube(homeObject.transform, "Door Frame Top", new Vector3(0f, 3.25f, -3f), Vector3.zero, new Vector3(1.65f, 0.45f, 0.3f), trimMaterial);
        CreateCube(homeObject.transform, "Door", new Vector3(0f, 1.38f, -3.16f), Vector3.zero, new Vector3(1.25f, 2.25f, 0.12f), trimMaterial);

        CreateCube(homeObject.transform, "Roof Left", new Vector3(-2.05f, 3.75f, 0f), new Vector3(0f, 0f, -24f), new Vector3(4.9f, 0.3f, 6.9f), roofMaterial);
        CreateCube(homeObject.transform, "Roof Right", new Vector3(2.05f, 3.75f, 0f), new Vector3(0f, 0f, 24f), new Vector3(4.9f, 0.3f, 6.9f), roofMaterial);
        CreateCube(homeObject.transform, "Roof Ridge", new Vector3(0f, 4.72f, 0f), Vector3.zero, new Vector3(0.35f, 0.25f, 6.95f), roofMaterial);

        CreateCube(homeObject.transform, "Storage Chest", new Vector3(2.35f, 0.82f, -3.85f), Vector3.zero, new Vector3(1.6f, 0.9f, 0.95f), chestMaterial);
        CreateCube(homeObject.transform, "Chest Lid", new Vector3(2.35f, 1.34f, -3.85f), Vector3.zero, new Vector3(1.7f, 0.18f, 1.05f), trimMaterial);
        CreateCube(homeObject.transform, "Bed", new Vector3(-2.35f, 0.72f, -3.82f), Vector3.zero, new Vector3(1.75f, 0.45f, 2.45f), bedMaterial);
        CreateCube(homeObject.transform, "Pillow", new Vector3(-2.35f, 1.05f, -4.55f), Vector3.zero, new Vector3(1.35f, 0.24f, 0.46f), CreateMaterial(new Color(0.86f, 0.78f, 0.62f, 1f)));

        Transform promptAnchor = CreateMarker(homeObject.transform, "Prompt Anchor", new Vector3(0f, 2.2f, -4.2f));
        Transform interactionPoint = CreateMarker(homeObject.transform, "Interaction Point", new Vector3(0f, 1f, -4.2f));

        CreateText(homeObject.transform, "HOME", new Vector3(0f, 3.25f, -3.25f), Quaternion.Euler(0f, 180f, 0f));

        PlayerHome home = homeObject.AddComponent<PlayerHome>();
        home.interactionPoint = interactionPoint;
        home.promptAnchor = promptAnchor;
        home.interactionDistance = 5.2f;

        Debug.Log("Created provisional player home at " + homeObject.transform.position + ".");
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.Euler(localEulerAngles);
        cube.transform.localScale = localScale;
        SetMaterial(cube, material);
        DisableCollider(cube);
        return cube;
    }

    private static Transform CreateMarker(Transform parent, string name, Vector3 localPosition)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        return marker.transform;
    }

    private static void CreateText(Transform parent, string label, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject textObject = new GameObject("Home Label");
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = localRotation;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = label;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.26f;
        text.fontSize = 80;
        text.color = new Color(0.98f, 0.9f, 0.62f, 1f);
    }

    private static float GetGroundY(float worldX, float worldZ)
    {
        Terrain terrain = GetTerrainAt(worldX, worldZ);

        if (terrain == null)
        {
            return 51.08f;
        }

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        float normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldX);
        float normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldZ);
        return terrainPosition.y + terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
    }

    private static Terrain GetTerrainAt(float worldX, float worldZ)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain != null && IsInsideTerrain(terrain, worldX, worldZ))
            {
                return terrain;
            }
        }

        Terrain activeTerrain = Terrain.activeTerrain;
        return activeTerrain != null && IsInsideTerrain(activeTerrain, worldX, worldZ) ? activeTerrain : null;
    }

    private static bool IsInsideTerrain(Terrain terrain, float worldX, float worldZ)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        return worldX >= terrainPosition.x
            && worldX <= terrainPosition.x + terrainSize.x
            && worldZ >= terrainPosition.z
            && worldZ <= terrainPosition.z + terrainSize.z;
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader)
        {
            color = color
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.22f);
        }

        return material;
    }

    private static void SetMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = material;
        }
    }

    private static void DisableCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = false;
        }
    }
}
