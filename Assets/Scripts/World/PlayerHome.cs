using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
    public Renderer[] highlightRenderers;
    public Color highlightColor = new Color(0.2f, 1f, 0.35f, 1f);

    private readonly List<PlayerInventory.InventorySlot> storedItems = new List<PlayerInventory.InventorySlot>();
    private readonly PlayerInventory.InventorySlot[] craftingSlots = new PlayerInventory.InventorySlot[9];
    private readonly List<LineRenderer> outlineLines = new List<LineRenderer>();
    private bool isMenuOpen;
    private bool isCraftingOpen;
    private bool isHighlighted;
    private string message = "";
    private float messageTimer;
    private Texture2D craftingBoardTexture;
    private Material outlineMaterial;
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
        UpdateHighlight();

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

    private void OnDisable()
    {
        SetHighlighted(false);
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

    public void EnterHomeInterior()
    {
        ResolveReferences();

        if (player == null)
        {
            ShowMessage("No player found.");
            return;
        }

        Vector3 returnPosition = player.position;
        Quaternion returnRotation = player.rotation;
        SetMenuOpen(false);
        SceneTransitionManager.EnterHomeInterior(returnPosition, returnRotation);
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

    private void UpdateHighlight()
    {
        SetHighlighted(IsPlayerInRange() && !GameUIState.AnyMenuOpen);
    }

    private void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;

        if (highlightRenderers == null || highlightRenderers.Length == 0)
        {
            return;
        }

        EnsureOutline();

        foreach (LineRenderer outlineLine in outlineLines)
        {
            if (outlineLine != null)
            {
                outlineLine.enabled = highlighted;
            }
        }
    }

    private void EnsureOutline()
    {
        if (outlineLines.Count > 0)
        {
            return;
        }

        Bounds bounds = GetHighlightBounds();
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, max.z),
            new Vector3(min.x, max.y, max.z),
        };

        int[,] edges =
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 },
        };

        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");

        if (shader != null)
        {
            outlineMaterial = new Material(shader)
            {
                name = "Home Door Outline Material",
            };
        }

        for (int i = 0; i < edges.GetLength(0); i++)
        {
            GameObject lineObject = new GameObject("Home Door Outline");
            lineObject.transform.SetParent(transform, true);

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.enabled = false;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, corners[edges[i, 0]]);
            lineRenderer.SetPosition(1, corners[edges[i, 1]]);
            lineRenderer.startColor = highlightColor;
            lineRenderer.endColor = highlightColor;
            lineRenderer.startWidth = 0.045f;
            lineRenderer.endWidth = 0.045f;
            lineRenderer.numCapVertices = 2;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            if (outlineMaterial != null)
            {
                lineRenderer.sharedMaterial = outlineMaterial;
            }

            outlineLines.Add(lineRenderer);
        }
    }

    private Bounds GetHighlightBounds()
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.one);

        foreach (Renderer renderer in highlightRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        bounds.Expand(0.08f);
        return bounds;
    }

    private void DrawHomeMenu()
    {
        if (isCraftingOpen)
        {
            DrawCraftingMenu();
            return;
        }

        Rect panel = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 186f, 440f, 372f);
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

        if (GameGui.Button(new Rect(panel.x + 18f, panel.y + 296f, panel.width - 36f, 38f), "Enter house"))
        {
            EnterHomeInterior();
        }

        string footerText = messageTimer > 0f ? message : "ESC - Close";

        if (teamSleepActive && coop.HasTeamSleepVote && !string.IsNullOrEmpty(coop.TeamSleepStatusText))
        {
            footerText = coop.TeamSleepStatusText;
        }

        GUI.Label(new Rect(panel.x + 18f, panel.y + 340f, panel.width - 36f, 22f), footerText, GameGui.HintStyle);
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
    private const string InteractionPointName = "Home Door Interaction Point";
    private const string PromptAnchorName = "Home Door Prompt Anchor";
    private static readonly string[] PreferredDoorNames = { "Door (1)", "house door", "House Door", "Door" };

    public static void EnsurePlayerHome()
    {
        Transform homeDoor = FindHomeDoor();

        if (homeDoor == null)
        {
            DestroyProceduralHomeIfPresent(null);
            return;
        }

        PlayerHome existingHome = Object.FindAnyObjectByType<PlayerHome>();
        List<PlayerInventory.InventorySlot> existingStoredItems = existingHome != null ? existingHome.ExportStoredItems() : null;
        PlayerHome home = homeDoor.GetComponent<PlayerHome>();

        if (home == null)
        {
            home = homeDoor.gameObject.AddComponent<PlayerHome>();
        }

        ConfigureDoorHome(home, homeDoor);

        if (existingStoredItems != null && existingHome != home)
        {
            home.ImportStoredItems(existingStoredItems);
        }

        DestroyProceduralHomeIfPresent(home);
    }

    private static Transform FindHomeDoor()
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);

        foreach (string preferredDoorName in PreferredDoorNames)
        {
            foreach (Transform transform in transforms)
            {
                if (transform == null || !IsMainWorldSceneObject(transform.gameObject) || transform.name != preferredDoorName)
                {
                    continue;
                }

                return transform;
            }
        }

        foreach (Transform transform in transforms)
        {
            if (transform == null || !IsMainWorldSceneObject(transform.gameObject))
            {
                continue;
            }

            if (string.Equals(transform.name, "house door", System.StringComparison.OrdinalIgnoreCase))
            {
                return transform;
            }
        }

        return null;
    }

    private static bool IsMainWorldSceneObject(GameObject gameObject)
    {
        Scene scene = gameObject.scene;
        return scene.IsValid() && scene.name == SceneTransitionManager.MainWorldSceneName;
    }

    private static void ConfigureDoorHome(PlayerHome home, Transform homeDoor)
    {
        Renderer[] renderers = homeDoor.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = GetRendererBounds(renderers, homeDoor.position);
        home.interactionPoint = EnsureMarker(homeDoor, InteractionPointName, homeDoor.InverseTransformPoint(bounds.center));
        home.promptAnchor = EnsureMarker(homeDoor, PromptAnchorName, homeDoor.InverseTransformPoint(bounds.center + Vector3.up * 0.9f));
        home.interactionDistance = 4.5f;
        home.highlightRenderers = renderers;
        home.highlightColor = new Color(0.2f, 1f, 0.35f, 1f);
    }

    private static Transform EnsureMarker(Transform parent, string name, Vector3 localPosition)
    {
        Transform marker = parent.Find(name);

        if (marker == null)
        {
            GameObject markerObject = new GameObject(name);
            markerObject.transform.SetParent(parent, false);
            marker = markerObject.transform;
        }

        marker.localPosition = localPosition;
        marker.localRotation = Quaternion.identity;
        marker.localScale = Vector3.one;
        return marker;
    }

    private static Bounds GetRendererBounds(Renderer[] renderers, Vector3 fallbackPosition)
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(fallbackPosition, Vector3.one);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    private static void DestroyProceduralHomeIfPresent(PlayerHome keepHome)
    {
        GameObject proceduralHome = GameObject.Find(HomeRootName);

        if (proceduralHome == null || (keepHome != null && proceduralHome == keepHome.gameObject))
        {
            return;
        }

        DestroyGameObject(proceduralHome);
    }

    private static void DestroyGameObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
            return;
        }

        Object.DestroyImmediate(target);
    }
}
