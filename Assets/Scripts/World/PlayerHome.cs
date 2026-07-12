using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerHome : MonoBehaviour
{
    private const string CraftingBoardResourcePath = "UI/CraftingBoard";
    private const int StorageGridSize = 10;
    private static readonly CraftingRecipe[] CraftingRecipes =
    {
        new CraftingRecipe("Working Pocket Watch", 120, 2, 1, "Watch Fragment", "Watch Case"),
        new CraftingRecipe("Horseshoe", 45, 2, 1, "Horseshoe Fragment", "Metal Rod"),
        new CraftingRecipe("Compass", 85, 1, 1, "Old Cracked Compass", "Metal Rod"),
        new CraftingRecipe("Bracelet", 55, 2, 1, "Broken Chain", "Chain Piece", "Chain Link"),
        new CraftingRecipe("Closed Portrait Locket", 110, 1, 1, "Cracked Glass Locket", "Metal Photo Frame")
    };

    public Transform interactionPoint;
    public Transform promptAnchor;
    public float interactionDistance = 4.5f;
    public PlayerInventory playerInventory;
    public Transform player;
    public Renderer[] highlightRenderers;
    public Color highlightColor = new Color(0.2f, 1f, 0.35f, 1f);

    private readonly List<PlayerInventory.InventorySlot> storedItems = new List<PlayerInventory.InventorySlot>();
    private readonly PlayerInventory.InventorySlot[] craftingSlots = new PlayerInventory.InventorySlot[9];
    private readonly List<LineRenderer> outlineLines = new List<LineRenderer>();
    private bool isMenuOpen;
    private bool isCraftingOpen;
    private bool isStorageOpen;
    private bool isHighlighted;
    private HomeConfirmation pendingHomeConfirmation = HomeConfirmation.None;
    private string message = "";
    private float messageTimer;
    private Texture2D craftingBoardTexture;
    private Material outlineMaterial;
    private Transform temporaryInteractionPoint;
    private float temporaryInteractionDistance;
    private CraftingDragSource craftingDragSource = CraftingDragSource.None;
    private PlayerInventory.InventorySlot draggedCraftingItem;
    private PlayerInventory.InventorySlot draggedBackpackItem;
    private Vector2 craftingRecipeScrollPosition;
    private int draggedCraftingSlotIndex = -1;
    private int menuOpenedFrame = -1;

    private enum CraftingDragSource
    {
        None,
        Backpack,
        CraftingSlot,
        Result
    }

    private enum HomeConfirmation
    {
        None,
        EnterHouse
    }

    private class CraftingRecipe
    {
        public readonly string resultName;
        public readonly int resultValue;
        public readonly int resultWidth;
        public readonly int resultHeight;
        public readonly string[] ingredients;

        public CraftingRecipe(string resultName, int resultValue, int resultWidth, int resultHeight, params string[] ingredients)
        {
            this.resultName = resultName;
            this.resultValue = resultValue;
            this.resultWidth = resultWidth;
            this.resultHeight = resultHeight;
            this.ingredients = ingredients;
        }
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

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (isMenuOpen)
            {
                if (Time.frameCount > menuOpenedFrame)
                {
                    SetMenuOpen(false);
                }
            }
            else if (GameUIState.CanProcessGameplayInput && IsPlayerNearPrimaryInteraction())
            {
                pendingHomeConfirmation = HomeConfirmation.EnterHouse;
                SetMenuOpen(true);
            }
        }

        if (isMenuOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (pendingHomeConfirmation != HomeConfirmation.None)
            {
                pendingHomeConfirmation = HomeConfirmation.None;
                return;
            }

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

        if (IsPlayerNear(targetPosition, interactionDistance))
        {
            return true;
        }

        return temporaryInteractionPoint != null && IsPlayerNear(temporaryInteractionPoint.position, temporaryInteractionDistance);
    }

    private bool IsPlayerNearPrimaryInteraction()
    {
        ResolveReferences();

        if (player == null)
        {
            return false;
        }

        Vector3 targetPosition = interactionPoint != null ? interactionPoint.position : transform.position;
        return IsPlayerNear(targetPosition, interactionDistance);
    }

    public void SetMenuOpen(bool open)
    {
        if (open && !isMenuOpen)
        {
            menuOpenedFrame = Time.frameCount;
        }

        if (!open)
        {
            ReturnCraftingItemsToBackpack();
            isCraftingOpen = false;
            isStorageOpen = false;
            temporaryInteractionPoint = null;
            temporaryInteractionDistance = 0f;
            pendingHomeConfirmation = HomeConfirmation.None;
        }

        isMenuOpen = open;
        GameUIState.SetHomeMenuOpen(isMenuOpen);
    }

    public void StoreBackpack()
    {
        if (playerInventory == null || playerInventory.items.Count == 0)
        {
            ShowMessage(GameLocalization.T("home.backpack_empty"));
            return;
        }

        int storedCount = playerInventory.items.Count;
        int storedValue = playerInventory.GetInventoryValue();

        for (int i = playerInventory.items.Count - 1; i >= 0; i--)
        {
            StoreItemFromBackpack(playerInventory.items[i], false);
        }
        ShowMessage(GameLocalization.TFormat("home.stored_summary", storedCount, storedValue));
        LocalCoopManager.Instance?.ReportHomeStorageChanged(this);
    }

    public void TakeStoredItems()
    {
        if (playerInventory == null)
        {
            ShowMessage(GameLocalization.T("home.no_backpack"));
            return;
        }

        if (storedItems.Count == 0)
        {
            ShowMessage(GameLocalization.T("home.storage_empty"));
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
            ShowMessage(GameLocalization.T("home.backpack_full"));
            return;
        }

        ShowMessage(GameLocalization.TFormat("home.took_summary", movedCount, movedValue));
        LocalCoopManager.Instance?.ReportHomeStorageChanged(this);
    }

    public void Sleep()
    {
        SleepInternal();
    }

    public string SleepFromStation()
    {
        return SleepInternal();
    }

    private string SleepInternal()
    {
        ResolveReferences();

        LocalCoopManager coop = LocalCoopManager.Instance;

        if (DayNightCycle.Instance != null && !DayNightCycle.Instance.CanSleep)
        {
            return ShowMessageAndReturn(GameLocalization.T("home.sleep_after"));
        }

        if (coop != null && coop.IsRunning)
        {
            if (coop.RequestTeamSleep(out string teamSleepMessage))
            {
                return ShowMessageAndReturn(teamSleepMessage);
            }
        }

        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.SleepUntilMorning();
            coop?.ReportTeamStateChanged();
            return ShowMessageAndReturn(GameLocalization.T("home.slept_reset"));
        }

        return ShowMessageAndReturn(GameLocalization.T("home.slept"));
    }

    public void EnterHomeInterior()
    {
        ResolveReferences();

        if (player == null)
        {
            ShowMessage(GameLocalization.T("home.no_player"));
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
            PlayerInventory.InventorySlot storedItem = CloneItem(item);

            if (CanPlaceStoredItemAt(storedItem.gridX, storedItem.gridY, storedItem.width, storedItem.height, null))
            {
                storedItems.Add(storedItem);
                continue;
            }

            if (TryFindStorageSpace(storedItem.width, storedItem.height, out int gridX, out int gridY))
            {
                storedItem.gridX = gridX;
                storedItem.gridY = gridY;
                storedItems.Add(storedItem);
            }
        }
    }

    public bool OpenCrafting()
    {
        if (!IsCraftingUnlocked())
        {
            ShowMessage(GameLocalization.T("home.crafting_locked"));
            return false;
        }

        isCraftingOpen = true;
        ShowMessage("");
        return true;
    }

    public string OpenCraftingFromStation(Transform stationPoint, float stationInteractionDistance)
    {
        if (!IsCraftingUnlocked())
        {
            return ShowMessageAndReturn(GameLocalization.T("home.crafting_locked"));
        }

        temporaryInteractionPoint = stationPoint;
        temporaryInteractionDistance = Mathf.Max(0.5f, stationInteractionDistance);
        isStorageOpen = false;
        SetMenuOpen(true);
        OpenCrafting();
        return "";
    }

    public string OpenStorageFromStation(Transform stationPoint, float stationInteractionDistance)
    {
        temporaryInteractionPoint = stationPoint;
        temporaryInteractionDistance = Mathf.Max(0.5f, stationInteractionDistance);
        isCraftingOpen = false;
        isStorageOpen = true;
        pendingHomeConfirmation = HomeConfirmation.None;
        SetMenuOpen(true);
        ShowMessage("");
        return "";
    }

    public void CloseCrafting()
    {
        ClearCraftingDrag();
        ReturnCraftingItemsToBackpack();
        isCraftingOpen = false;
    }

    public void CloseStorage()
    {
        isStorageOpen = false;
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
            if (home == null || !home.IsPlayerNearPrimaryInteraction())
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
        return FindClosestHomeInRange() != null || HomeInteractionStation.AnyStationInteractionInRange();
    }

    public bool IsCraftingUnlocked()
    {
        UpgradeShop shop = FindUpgradeShop();
        return shop != null && shop.craftingUnlocked;
    }

    private bool IsPlayerNear(Vector3 targetPosition, float distance)
    {
        return player != null && Vector3.Distance(player.position, targetPosition) <= distance;
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

    }

    private UpgradeShop FindUpgradeShop()
    {
        UpgradeShop[] shops = FindObjectsByType<UpgradeShop>(FindObjectsInactive.Include);

        foreach (UpgradeShop shop in shops)
        {
            if (shop != null && shop.CanUpgradeHere)
            {
                return shop;
            }
        }

        return shops.Length > 0 ? shops[0] : null;
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
            ShowMessage(GameLocalization.T("home.crafting_grid_full"));
            return false;
        }

        if (!playerInventory.RemoveItem(item))
        {
            return false;
        }

        craftingSlots[emptySlot] = CloneItem(item);
        ShowMessage(GameLocalization.TFormat("home.placed_item", item.itemName));
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
            ShowMessage(GameLocalization.T("home.backpack_full"));
            return;
        }

        craftingSlots[slotIndex] = null;
        ShowMessage(GameLocalization.TFormat("home.returned_item", item.itemName));
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

    private CraftingRecipe GetMatchingCraftingRecipe()
    {
        foreach (CraftingRecipe recipe in CraftingRecipes)
        {
            if (CraftingGridMatches(recipe))
            {
                return recipe;
            }
        }

        return null;
    }

    private bool CraftingGridMatches(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.ingredients == null || recipe.ingredients.Length == 0)
        {
            return false;
        }

        Dictionary<string, int> requiredItems = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        foreach (string ingredient in recipe.ingredients)
        {
            if (string.IsNullOrWhiteSpace(ingredient))
            {
                continue;
            }

            if (!requiredItems.ContainsKey(ingredient))
            {
                requiredItems[ingredient] = 0;
            }

            requiredItems[ingredient]++;
        }

        int placedItemCount = 0;

        foreach (PlayerInventory.InventorySlot item in craftingSlots)
        {
            if (item == null)
            {
                continue;
            }

            placedItemCount++;

            if (!requiredItems.ContainsKey(item.itemName) || requiredItems[item.itemName] <= 0)
            {
                return false;
            }

            requiredItems[item.itemName]--;
        }

        if (placedItemCount != recipe.ingredients.Length)
        {
            return false;
        }

        foreach (int remainingCount in requiredItems.Values)
        {
            if (remainingCount != 0)
            {
                return false;
            }
        }

        return true;
    }

    private Texture2D GetCraftingBoardTexture()
    {
        if (craftingBoardTexture == null)
        {
            craftingBoardTexture = Resources.Load<Texture2D>(CraftingBoardResourcePath);
        }

        return craftingBoardTexture;
    }

    private bool StoreItemFromBackpack(PlayerInventory.InventorySlot item, bool showFeedback)
    {
        if (playerInventory == null || item == null)
        {
            return false;
        }

        if (!TryFindStorageSpace(item.width, item.height, out int gridX, out int gridY))
        {
            if (showFeedback)
            {
                ShowMessage(GameLocalization.T("home.storage_full"));
            }

            return false;
        }

        if (!playerInventory.RemoveItem(item))
        {
            return false;
        }

        PlayerInventory.InventorySlot storedItem = CloneItem(item);
        storedItem.gridX = gridX;
        storedItem.gridY = gridY;
        storedItems.Add(storedItem);

        if (showFeedback)
        {
            ShowMessage(GameLocalization.TFormat("home.stored_item", storedItem.itemName));
        }

        LocalCoopManager.Instance?.ReportHomeStorageChanged(this);
        return true;
    }

    private bool TakeStoredItem(PlayerInventory.InventorySlot item)
    {
        if (playerInventory == null || item == null || !storedItems.Contains(item))
        {
            return false;
        }

        if (!playerInventory.AddItem(item.itemName, item.value, item.width, item.height, item.icon))
        {
            ShowMessage(GameLocalization.T("home.backpack_full"));
            return false;
        }

        storedItems.Remove(item);
        ShowMessage(GameLocalization.TFormat("home.took_item", item.itemName));
        LocalCoopManager.Instance?.ReportHomeStorageChanged(this);
        return true;
    }

    private bool TryFindStorageSpace(int width, int height, out int gridX, out int gridY)
    {
        width = Mathf.Clamp(width, 1, StorageGridSize);
        height = Mathf.Clamp(height, 1, StorageGridSize);

        for (int y = 0; y <= StorageGridSize - height; y++)
        {
            for (int x = 0; x <= StorageGridSize - width; x++)
            {
                if (CanPlaceStoredItemAt(x, y, width, height, null))
                {
                    gridX = x;
                    gridY = y;
                    return true;
                }
            }
        }

        gridX = 0;
        gridY = 0;
        return false;
    }

    private bool CanPlaceStoredItemAt(int gridX, int gridY, int width, int height, PlayerInventory.InventorySlot ignoredItem)
    {
        if (gridX < 0 || gridY < 0 || width <= 0 || height <= 0 || gridX + width > StorageGridSize || gridY + height > StorageGridSize)
        {
            return false;
        }

        foreach (PlayerInventory.InventorySlot item in storedItems)
        {
            if (item == null || item == ignoredItem)
            {
                continue;
            }

            bool overlaps = gridX < item.gridX + item.width
                && gridX + width > item.gridX
                && gridY < item.gridY + item.height
                && gridY + height > item.gridY;

            if (overlaps)
            {
                return false;
            }
        }

        return true;
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

    private string ShowMessageAndReturn(string value)
    {
        ShowMessage(value);
        return value;
    }

    private void OnGUI()
    {
        if (isMenuOpen)
        {
            DrawHomeMenu();
            return;
        }

        if (IsPlayerNearPrimaryInteraction() && GameUIState.CanProcessGameplayInput)
        {
            GameGui.DrawToast(new Rect(Screen.width * 0.5f - 190f, Screen.height - 178f, 380f, 40f), GameLocalization.T("hud.hint_use_home"));
        }
    }

    private void UpdateHighlight()
    {
        SetHighlighted(IsPlayerNearPrimaryInteraction() && GameUIState.CanProcessGameplayInput);
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

        if (isStorageOpen)
        {
            DrawStorageMenu();
            return;
        }

        if (pendingHomeConfirmation == HomeConfirmation.None)
        {
            pendingHomeConfirmation = HomeConfirmation.EnterHouse;
        }

        DrawHomeConfirmationDialog();
    }

    private void DrawHomeConfirmationDialog()
    {
        if (pendingHomeConfirmation == HomeConfirmation.None)
        {
            return;
        }

        Rect overlay = new Rect(0f, 0f, Screen.width, Screen.height);
        GameGui.DrawRect(overlay, new Color(0f, 0f, 0f, 0.48f));

        Rect panel = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 132f, 440f, 264f);
        GameGui.DrawPanel(panel, GameLocalization.T("home.confirm_title"));
        string body = GameLocalization.T("home.confirm_body");
        GUI.Label(new Rect(panel.x + 28f, panel.y + 54f, panel.width - 56f, 58f), body, GameGui.LabelStyle);

        Rect acceptRect = new Rect(panel.x + 102f, panel.y + 138f, 72f, 72f);
        Rect rejectRect = new Rect(panel.xMax - 174f, panel.y + 138f, 72f, 72f);

        if (GUI.Button(acceptRect, GUIContent.none, GUIStyle.none))
        {
            pendingHomeConfirmation = HomeConfirmation.None;
            EnterHomeInterior();
        }

        if (GUI.Button(rejectRect, GUIContent.none, GUIStyle.none))
        {
            SetMenuOpen(false);
        }

        GameGui.DrawIcon(acceptRect, "confirm", Color.white);
        GameGui.DrawIcon(rejectRect, "reject", Color.white);
        GUI.Label(new Rect(acceptRect.x - 16f, acceptRect.yMax + 4f, acceptRect.width + 32f, 20f), GameLocalization.T("home.confirm_accept"), GameGui.HintStyle);
        GUI.Label(new Rect(rejectRect.x - 16f, rejectRect.yMax + 4f, rejectRect.width + 32f, 20f), GameLocalization.T("home.confirm_cancel"), GameGui.HintStyle);
    }

    private void DrawStorageMenu()
    {
        float availableWidth = Mathf.Max(760f, Screen.width - 40f);
        float availableHeight = Mathf.Max(520f, Screen.height - 70f);
        float gap = 22f;
        float backpackWidth = Mathf.Clamp(availableWidth * 0.38f, 360f, 560f);
        float storageSize = Mathf.Min(availableHeight - 92f, availableWidth - backpackWidth - gap - 42f);
        storageSize = Mathf.Clamp(storageSize, 420f, 720f);
        float totalWidth = backpackWidth + gap + storageSize;
        float startX = Screen.width * 0.5f - totalWidth * 0.5f;
        float startY = Screen.height * 0.5f - Mathf.Max(backpackWidth / PlayerInventory.BoardAspect, storageSize) * 0.5f - 12f;

        Rect backpackRect = PlayerInventory.GetInventoryBoardRect(
            startX + backpackWidth * 0.5f,
            startY + storageSize * 0.5f,
            backpackWidth,
            Mathf.Min(storageSize, backpackWidth / PlayerInventory.BoardAspect));
        Rect storageRect = new Rect(startX + backpackWidth + gap, startY, storageSize, storageSize);
        Rect titleRect = new Rect(storageRect.x, storageRect.y - 40f, storageRect.width, 28f);
        Rect footerRect = new Rect(startX, startY + storageSize + 12f, totalWidth, 28f);

        GUI.Label(new Rect(backpackRect.x, backpackRect.y - 40f, backpackRect.width, 28f), GameLocalization.T("home.backpack_title"), GameGui.TitleStyle);
        GUI.Label(titleRect, GameLocalization.T("home.storage_title"), GameGui.TitleStyle);

        if (playerInventory != null)
        {
            playerInventory.DrawInventoryBoard(backpackRect, false, null);
            HandleStorageBackpackClick(backpackRect);
        }

        DrawStorageGrid(storageRect);
        GUI.Label(footerRect, messageTimer > 0f ? message : GameLocalization.T("home.storage_help"), GameGui.HintStyle);
    }

    private void DrawStorageGrid(Rect gridRect)
    {
        GameGui.DrawPanel(gridRect, "");
        float gap = 2f;
        float cellSize = (gridRect.width - gap * (StorageGridSize + 1)) / StorageGridSize;

        for (int y = 0; y < StorageGridSize; y++)
        {
            for (int x = 0; x < StorageGridSize; x++)
            {
                Rect cellRect = new Rect(
                    gridRect.x + gap + x * (cellSize + gap),
                    gridRect.y + gap + y * (cellSize + gap),
                    cellSize,
                    cellSize);
                GameGui.DrawRect(cellRect, new Color(0.04f, 0.035f, 0.025f, 0.58f));
            }
        }

        PlayerInventory.InventorySlot clickedItem = null;

        foreach (PlayerInventory.InventorySlot item in storedItems)
        {
            Rect itemRect = GetStorageItemRect(gridRect, item, cellSize, gap);

            if (GUI.Button(itemRect, "", GUIStyle.none))
            {
                clickedItem = item;
            }

            PlayerInventory.DrawInventoryItem(itemRect, item);
        }

        if (clickedItem != null)
        {
            TakeStoredItem(clickedItem);
        }
    }

    private Rect GetStorageItemRect(Rect gridRect, PlayerInventory.InventorySlot item, float cellSize, float gap)
    {
        float x = gridRect.x + gap + item.gridX * (cellSize + gap);
        float y = gridRect.y + gap + item.gridY * (cellSize + gap);
        float width = item.width * cellSize + Mathf.Max(0, item.width - 1) * gap;
        float height = item.height * cellSize + Mathf.Max(0, item.height - 1) * gap;
        return new Rect(x, y, width, height);
    }

    private void HandleStorageBackpackClick(Rect backpackRect)
    {
        Event currentEvent = Event.current;

        if (currentEvent == null || currentEvent.type != EventType.MouseDown || currentEvent.button != 0 || !backpackRect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        PlayerInventory.InventorySlot item = GetBackpackItemAt(backpackRect, currentEvent.mousePosition);

        if (item != null && StoreItemFromBackpack(item, true))
        {
            currentEvent.Use();
        }
    }

    private void DrawCraftingMenu()
    {
        float availableWidth = Mathf.Max(720f, Screen.width - 40f);
        float gap = 18f;
        float recipeWidth = Mathf.Clamp(Screen.width * 0.22f, 310f, 380f);
        bool useRecipeSidePanel = availableWidth >= 1180f;
        float backpackWidth = useRecipeSidePanel
            ? Mathf.Clamp(Screen.width * 0.25f, 340f, 520f)
            : Mathf.Clamp(Screen.width * 0.34f, 420f, 620f);
        float boardWidth = Mathf.Clamp(availableWidth - backpackWidth - gap - (useRecipeSidePanel ? recipeWidth + gap : 0f), 620f, 980f);

        if (useRecipeSidePanel && boardWidth < 560f)
        {
            useRecipeSidePanel = false;
            backpackWidth = Mathf.Clamp(Screen.width * 0.34f, 420f, 620f);
            boardWidth = Mathf.Clamp(availableWidth - backpackWidth - gap, 620f, 980f);
        }

        if (boardWidth + backpackWidth + gap + (useRecipeSidePanel ? recipeWidth + gap : 0f) > availableWidth)
        {
            backpackWidth = Mathf.Clamp(availableWidth * (useRecipeSidePanel ? 0.27f : 0.34f), 320f, 520f);
            boardWidth = Mathf.Max(520f, availableWidth - backpackWidth - gap - (useRecipeSidePanel ? recipeWidth + gap : 0f));
        }

        float boardHeight = boardWidth / PlayerInventory.BoardAspect;
        float backpackHeight = backpackWidth / PlayerInventory.BoardAspect;
        float maxBoardHeight = Mathf.Max(420f, Screen.height - 70f);

        if (boardHeight > maxBoardHeight)
        {
            boardHeight = maxBoardHeight;
            boardWidth = boardHeight * PlayerInventory.BoardAspect;
            backpackWidth = Mathf.Min(backpackWidth, Mathf.Max(320f, availableWidth - boardWidth - gap - (useRecipeSidePanel ? recipeWidth + gap : 0f)));
            backpackHeight = backpackWidth / PlayerInventory.BoardAspect;
        }

        float totalWidth = boardWidth + backpackWidth + gap + (useRecipeSidePanel ? recipeWidth + gap : 0f);
        Rect boardRect = new Rect(Screen.width * 0.5f - totalWidth * 0.5f, Screen.height * 0.5f - boardHeight * 0.5f - 8f, boardWidth, boardHeight);
        Texture2D boardTexture = GetCraftingBoardTexture();
        Rect contentRect = boardRect;
        Rect backpackRect = new Rect(contentRect.x + contentRect.width + gap, contentRect.y + contentRect.height * 0.5f - backpackHeight * 0.5f, backpackWidth, backpackHeight);
        Rect recipeRect = useRecipeSidePanel
            ? new Rect(backpackRect.xMax + gap, contentRect.y, recipeWidth, contentRect.height)
            : Rect.zero;

        if (boardTexture != null)
        {
            contentRect = PlayerInventory.GetFittedTextureRect(boardRect, boardTexture);
            GUI.DrawTexture(contentRect, boardTexture, ScaleMode.StretchToFill, true);
            backpackRect = new Rect(contentRect.x + contentRect.width + gap, contentRect.y + contentRect.height * 0.5f - backpackHeight * 0.5f, backpackWidth, backpackHeight);
            recipeRect = useRecipeSidePanel
                ? new Rect(backpackRect.xMax + gap, contentRect.y, recipeWidth, contentRect.height)
                : Rect.zero;
        }
        else
        {
            GameGui.DrawPanel(boardRect, GameLocalization.T("home.crafting_title"));
        }

        if (!useRecipeSidePanel)
        {
            float remainingHeight = Screen.height - backpackRect.yMax - 24f;
            float recipeHeight = Mathf.Clamp(remainingHeight, 190f, 280f);
            recipeRect = new Rect(backpackRect.x, backpackRect.yMax + 12f, backpackRect.width, recipeHeight);

            if (recipeRect.yMax > Screen.height - 12f)
            {
                recipeRect.y = Mathf.Max(12f, contentRect.y + contentRect.height - recipeRect.height);
            }
        }

        DrawCraftingGrid(contentRect);
        DrawCraftingResult(contentRect);
        DrawCraftingBackpack(backpackRect);
        DrawCraftingRecipeList(recipeRect);

        GUI.Label(new Rect(contentRect.x + 42f, contentRect.y + contentRect.height - 48f, contentRect.width - 250f, 28f), messageTimer > 0f ? message : GameLocalization.T("home.close_hint"), GameGui.HintStyle);
        HandleCraftingDragRelease(contentRect, backpackRect);
        DrawCraftingDragGhost();
    }

    private void DrawCraftingRecipeList(Rect recipeRect)
    {
        if (recipeRect.width <= 0f || recipeRect.height <= 0f)
        {
            return;
        }

        GameGui.DrawPanel(recipeRect, GameLocalization.T("home.recipes_title"));

        Rect listRect = new Rect(recipeRect.x + 12f, recipeRect.y + 42f, recipeRect.width - 24f, recipeRect.height - 54f);
        float rowHeight = 78f;
        Rect viewRect = new Rect(0f, 0f, Mathf.Max(1f, listRect.width - 18f), CraftingRecipes.Length * rowHeight);
        craftingRecipeScrollPosition = GUI.BeginScrollView(listRect, craftingRecipeScrollPosition, viewRect, false, true);

        for (int i = 0; i < CraftingRecipes.Length; i++)
        {
            DrawCraftingRecipeRow(new Rect(0f, i * rowHeight, viewRect.width, rowHeight - 8f), CraftingRecipes[i], i);
        }

        GUI.EndScrollView();
    }

    private void DrawCraftingRecipeRow(Rect rowRect, CraftingRecipe recipe, int index)
    {
        if (recipe == null)
        {
            return;
        }

        bool hasIngredients = HasIngredientsForRecipe(recipe, out string missingText);
        Color rowColor = index % 2 == 0
            ? new Color(0.06f, 0.055f, 0.04f, 0.74f)
            : new Color(0.10f, 0.085f, 0.055f, 0.66f);

        GameGui.DrawRect(rowRect, rowColor);
        GameGui.DrawRect(new Rect(rowRect.x, rowRect.y, 3f, rowRect.height), hasIngredients ? GameGui.GoodColor : GameGui.PanelBorderColor);

        Rect iconRect = new Rect(rowRect.x + 8f, rowRect.y + 9f, 52f, 52f);
        PlayerInventory.DrawInventoryItem(iconRect, CreateCraftingResultItem(recipe));

        float textX = iconRect.xMax + 9f;
        float textWidth = Mathf.Max(1f, rowRect.width - textX - 8f);
        GUI.Label(new Rect(textX, rowRect.y + 7f, textWidth, 20f), recipe.resultName + "  $" + recipe.resultValue, GameGui.LabelStyle);
        GUI.Label(new Rect(textX, rowRect.y + 29f, textWidth, 20f), string.Join(" + ", recipe.ingredients), GameGui.SmallLabelStyle);

        Color oldColor = GUI.color;
        GUI.color = hasIngredients ? GameGui.GoodColor : GameGui.MutedTextColor;
        GUI.Label(new Rect(textX, rowRect.y + 51f, textWidth, 18f), hasIngredients ? GameLocalization.T("home.ready") : GameLocalization.TFormat("home.missing", missingText), GameGui.SmallLabelStyle);
        GUI.color = oldColor;
    }

    private bool HasIngredientsForRecipe(CraftingRecipe recipe, out string missingText)
    {
        Dictionary<string, int> requiredItems = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> availableItems = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        foreach (string ingredient in recipe.ingredients)
        {
            if (string.IsNullOrWhiteSpace(ingredient))
            {
                continue;
            }

            if (!requiredItems.ContainsKey(ingredient))
            {
                requiredItems[ingredient] = 0;
            }

            requiredItems[ingredient]++;
        }

        if (playerInventory != null)
        {
            foreach (PlayerInventory.InventorySlot item in playerInventory.items)
            {
                AddAvailableCraftingItem(availableItems, item);
            }
        }

        foreach (PlayerInventory.InventorySlot item in craftingSlots)
        {
            AddAvailableCraftingItem(availableItems, item);
        }

        List<string> missingItems = new List<string>();

        foreach (KeyValuePair<string, int> requiredItem in requiredItems)
        {
            availableItems.TryGetValue(requiredItem.Key, out int availableCount);
            int missingCount = requiredItem.Value - availableCount;

            if (missingCount <= 0)
            {
                continue;
            }

            missingItems.Add(requiredItem.Key + (missingCount > 1 ? " x" + missingCount : ""));
        }

        missingText = missingItems.Count > 0 ? string.Join(", ", missingItems.ToArray()) : "";
        return missingItems.Count == 0;
    }

    private static void AddAvailableCraftingItem(Dictionary<string, int> availableItems, PlayerInventory.InventorySlot item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.itemName))
        {
            return;
        }

        if (!availableItems.ContainsKey(item.itemName))
        {
            availableItems[item.itemName] = 0;
        }

        availableItems[item.itemName]++;
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
        CraftingRecipe recipe = GetMatchingCraftingRecipe();

        if (recipe != null)
        {
            PlayerInventory.InventorySlot resultItem = CreateCraftingResultItem(recipe);
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

    private PlayerInventory.InventorySlot CreateCraftingResultItem(CraftingRecipe recipe)
    {
        return new PlayerInventory.InventorySlot
            {
                itemName = recipe.resultName,
                value = recipe.resultValue,
                icon = PlayerInventory.ResolveItemIcon(recipe.resultName),
                width = recipe.resultWidth,
                height = recipe.resultHeight
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
            ShowMessage(GameLocalization.T("home.slot_occupied"));
            return;
        }

        if (!playerInventory.RemoveItem(draggedBackpackItem))
        {
            return;
        }

        craftingSlots[targetSlot] = CloneItem(draggedBackpackItem);
        ShowMessage(GameLocalization.TFormat("home.placed_item", draggedBackpackItem.itemName));
    }

    private bool TryClaimCraftingResult()
    {
        if (playerInventory == null)
        {
            ShowMessage(GameLocalization.T("home.no_backpack"));
            return false;
        }

        CraftingRecipe recipe = GetMatchingCraftingRecipe();

        if (recipe == null)
        {
            return false;
        }

        PlayerInventory.InventorySlot resultItem = CreateCraftingResultItem(recipe);

        if (!playerInventory.AddItem(resultItem.itemName, resultItem.value, resultItem.width, resultItem.height, resultItem.icon))
        {
            ShowMessage(GameLocalization.TFormat("home.need_space", resultItem.width, resultItem.height));
            return false;
        }

        for (int i = 0; i < craftingSlots.Length; i++)
        {
            craftingSlots[i] = null;
        }

        ShowMessage(GameLocalization.TFormat("home.crafted_item", resultItem.itemName));
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
