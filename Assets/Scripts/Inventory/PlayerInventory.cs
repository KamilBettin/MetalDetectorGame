using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    private const int MinimumGridSize = 3;
    private const int MaximumArtGridSize = 5;
    private const int TrailerStartingMoney = 1000;
    public const float BoardAspect = 4f / 3f;
    private static readonly Dictionary<int, Texture2D> BoardTextures = new Dictionary<int, Texture2D>();
    private static readonly Dictionary<string, Sprite> GeneratedItemIcons = new Dictionary<string, Sprite>();

    [System.Serializable]
    public class InventorySlot
    {
        public string itemName;
        public int value;
        public Sprite icon;
        public int width = 1;
        public int height = 1;
        public int gridX;
        public int gridY;
    }

    public int money;
    public int gridSize = 3;
    public int maxGridSize = 5;
    public List<InventorySlot> items = new List<InventorySlot>();
    public bool isOpen;

    public int Capacity => gridSize * gridSize;
    public int OccupiedSlots => GetOccupiedSlotCount();
    public bool IsFull => !HasFreeSpace(1, 1);

    private void Awake()
    {
        money = Mathf.Max(money, TrailerStartingMoney);
        ClampGridSizeToAvailableArt();
    }

    public bool AddTreasure(DetectableTreasure treasure)
    {
        treasure.icon = ResolveItemIcon(treasure.treasureName, treasure.icon);
        return AddItem(treasure.treasureName, treasure.value, 1, 1, treasure.icon);
    }

    public bool AddItem(string itemName, int value, int width = 1, int height = 1, Sprite icon = null)
    {
        width = Mathf.Clamp(width, 1, gridSize);
        height = Mathf.Clamp(height, 1, gridSize);
        icon = ResolveItemIcon(itemName, icon);

        if (!TryFindFreeSpace(width, height, out int gridX, out int gridY))
        {
            return false;
        }

        items.Add(new InventorySlot
        {
            itemName = itemName,
            value = value,
            icon = icon,
            width = width,
            height = height,
            gridX = gridX,
            gridY = gridY
        });

        return true;
    }

    public bool HasFreeSpace(int width, int height)
    {
        return TryFindFreeSpace(width, height, out _, out _);
    }

    public int GetInventoryValue()
    {
        int totalValue = 0;

        foreach (InventorySlot item in items)
        {
            totalValue += item.value;
        }

        return totalValue;
    }

    public int SellAll()
    {
        int earnedMoney = GetInventoryValue();
        money += earnedMoney;
        items.Clear();
        return earnedMoney;
    }

    public bool SellItem(InventorySlot item)
    {
        if (item == null || !items.Contains(item))
        {
            return false;
        }

        money += item.value;
        items.Remove(item);
        return true;
    }

    public bool RemoveItem(InventorySlot item)
    {
        if (item == null || !items.Contains(item))
        {
            return false;
        }

        items.Remove(item);
        return true;
    }

    public int CountItemsNamed(string itemName)
    {
        return CountItemsNamed(new[] { itemName });
    }

    public int CountItemsNamed(string[] itemNames)
    {
        int count = 0;

        foreach (InventorySlot item in items)
        {
            if (ItemNameMatches(item, itemNames))
            {
                count++;
            }
        }

        return count;
    }

    public bool RemoveItemsNamed(string itemName, int count)
    {
        return RemoveItemsNamed(new[] { itemName }, count);
    }

    public bool RemoveItemsNamed(string[] itemNames, int count)
    {
        count = Mathf.Max(0, count);

        if (count == 0)
        {
            return true;
        }

        if (CountItemsNamed(itemNames) < count)
        {
            return false;
        }

        for (int i = items.Count - 1; i >= 0 && count > 0; i--)
        {
            if (!ItemNameMatches(items[i], itemNames))
            {
                continue;
            }

            items.RemoveAt(i);
            count--;
        }

        return count == 0;
    }

    public bool TrySpendMoney(int amount)
    {
        if (money < amount)
        {
            return false;
        }

        money -= amount;
        return true;
    }

    public bool CanUpgradeGrid()
    {
        return gridSize < maxGridSize;
    }

    public bool TryUpgradeGrid()
    {
        if (!CanUpgradeGrid())
        {
            return false;
        }

        gridSize++;
        return true;
    }

    private void Update()
    {
        ClampGridSizeToAvailableArt();

        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            SetOpen(!isOpen);
        }

        if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetOpen(false);
        }
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        GameUIState.SetInventoryOpen(isOpen);
    }

    private void OnGUI()
    {
        if (RuntimeGameUI.IsActive && !isOpen)
        {
            return;
        }

        if (!isOpen)
        {
            DrawCompactStatus();
            return;
        }

        Rect boardRect = GetInventoryBoardRect(
            Screen.width * 0.5f,
            Screen.height * 0.5f,
            Mathf.Min(Screen.width - 40f, 1280f),
            Mathf.Min(Screen.height - 30f, 960f));
        DrawInventoryBoard(boardRect, false, null);
    }

    private void DrawCompactStatus()
    {
        Rect rect = new Rect(Screen.width - 190f, 20f, 170f, 48f);
        GameGui.DrawPanel(rect, "");
        GUI.Label(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, 24f), "$" + money, GameGui.LabelStyle);
    }

    private bool TryFindFreeSpace(int width, int height, out int gridX, out int gridY)
    {
        for (int y = 0; y <= gridSize - height; y++)
        {
            for (int x = 0; x <= gridSize - width; x++)
            {
                if (CanPlaceAt(x, y, width, height))
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

    private bool CanPlaceAt(int gridX, int gridY, int width, int height)
    {
        if (gridX < 0 || gridY < 0 || gridX + width > gridSize || gridY + height > gridSize)
        {
            return false;
        }

        foreach (InventorySlot item in items)
        {
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

    private static bool ItemNameMatches(InventorySlot item, string[] itemNames)
    {
        if (item == null || itemNames == null)
        {
            return false;
        }

        for (int i = 0; i < itemNames.Length; i++)
        {
            if (NamesMatch(item.itemName, itemNames[i]))
            {
                return true;
            }
        }

        return false;
    }

    private int GetOccupiedSlotCount()
    {
        int occupiedSlots = 0;

        foreach (InventorySlot item in items)
        {
            occupiedSlots += Mathf.Max(1, item.width) * Mathf.Max(1, item.height);
        }

        return occupiedSlots;
    }

    public void DrawInventoryBoard(Rect boardRect, bool showStats, Action<InventorySlot> onItemClicked)
    {
        Texture2D boardTexture = GetBoardTexture(gridSize);
        Rect contentRect = boardRect;

        if (boardTexture != null)
        {
            contentRect = GetFittedTextureRect(boardRect, boardTexture);
            GUI.DrawTexture(contentRect, boardTexture, ScaleMode.StretchToFill, true);
        }
        else
        {
            GameGui.DrawPanel(boardRect, "Backpack " + gridSize + "x" + gridSize);
        }

        if (showStats)
        {
            Rect statsRect = new Rect(contentRect.x + contentRect.width * 0.17f, contentRect.y + contentRect.height * 0.20f, contentRect.width * 0.24f, 64f);
            GUI.Label(new Rect(statsRect.x, statsRect.y, statsRect.width, 22f), "$" + money + " cash", GameGui.LabelStyle);
            GUI.Label(new Rect(statsRect.x, statsRect.y + 22f, statsRect.width, 18f), "Cargo $" + GetInventoryValue(), GameGui.SmallLabelStyle);
            GameGui.DrawProgressBar(
                new Rect(statsRect.x, statsRect.y + 44f, statsRect.width, 12f),
                OccupiedSlots / (float)Capacity,
                IsFull ? GameGui.DangerColor : GameGui.AccentColor,
                OccupiedSlots + "/" + Capacity);
        }

        InventorySlot clickedItem = null;

        for (int i = 0; i < items.Count; i++)
        {
            InventorySlot item = items[i];
            Rect itemRect = GetInventoryItemRect(contentRect, gridSize, item);

            if (onItemClicked != null && GUI.Button(itemRect, "", GUIStyle.none))
            {
                clickedItem = item;
            }

            DrawInventoryItem(itemRect, item);
        }

        if (clickedItem != null)
        {
            onItemClicked(clickedItem);
        }
    }

    public static Rect GetInventoryBoardRect(float centerX, float centerY, float maxWidth, float maxHeight)
    {
        float width = Mathf.Min(maxWidth, maxHeight * BoardAspect);
        float height = width / BoardAspect;
        return new Rect(centerX - width * 0.5f, centerY - height * 0.5f, width, height);
    }

    public static Rect GetFittedTextureRect(Rect outerRect, Texture texture)
    {
        if (texture == null || texture.width <= 0 || texture.height <= 0 || outerRect.width <= 0f || outerRect.height <= 0f)
        {
            return outerRect;
        }

        float textureAspect = texture.width / (float)texture.height;
        float rectAspect = outerRect.width / outerRect.height;

        if (rectAspect > textureAspect)
        {
            float width = outerRect.height * textureAspect;
            return new Rect(outerRect.x + (outerRect.width - width) * 0.5f, outerRect.y, width, outerRect.height);
        }

        float height = outerRect.width / textureAspect;
        return new Rect(outerRect.x, outerRect.y + (outerRect.height - height) * 0.5f, outerRect.width, height);
    }

    public static Rect GetInventoryItemRect(Rect boardRect, int gridSize, InventorySlot item)
    {
        BoardGridLayout layout = GetBoardGridLayout(gridSize);
        float slotWidth = boardRect.width * layout.slotWidth;
        float slotHeight = boardRect.height * layout.slotHeight;
        float gapX = boardRect.width * layout.gapX;
        float gapY = boardRect.height * layout.gapY;
        float x = boardRect.x + boardRect.width * layout.startX + item.gridX * (slotWidth + gapX);
        float y = boardRect.y + boardRect.height * layout.startY + item.gridY * (slotHeight + gapY);
        float width = item.width * slotWidth + Mathf.Max(0, item.width - 1) * gapX;
        float height = item.height * slotHeight + Mathf.Max(0, item.height - 1) * gapY;
        return new Rect(x, y, width, height);
    }

    private static Texture2D GetBoardTexture(int gridSize)
    {
        int visualGridSize = GetVisualGridSize(gridSize);

        if (BoardTextures.TryGetValue(visualGridSize, out Texture2D cachedTexture))
        {
            return cachedTexture;
        }

        Texture2D texture = Resources.Load<Texture2D>("UI/InventoryBoard" + visualGridSize);
        BoardTextures[visualGridSize] = texture;
        return texture;
    }

    private static int GetVisualGridSize(int gridSize)
    {
        return Mathf.Clamp(gridSize, MinimumGridSize, MaximumArtGridSize);
    }

    private static BoardGridLayout GetBoardGridLayout(int gridSize)
    {
        switch (GetVisualGridSize(gridSize))
        {
            case 4:
                return new BoardGridLayout(0.2935f, 0.2624f, 0.0891f, 0.1206f, 0.0175f, 0.0226f);
            case 5:
                return new BoardGridLayout(0.2548f, 0.2339f, 0.0822f, 0.1031f, 0.0200f, 0.0230f);
            default:
                return new BoardGridLayout(0.3198f, 0.2680f, 0.1077f, 0.1473f, 0.0185f, 0.0254f);
        }
    }

    public static void DrawInventoryItem(Rect itemRect, InventorySlot item)
    {
        item.icon = ResolveItemIcon(item.itemName, item.icon);
        float padding = Mathf.Clamp(Mathf.Min(itemRect.width, itemRect.height) * 0.025f, 2f, 5f);
        Rect innerRect = InsetRect(itemRect, padding);
        GameGui.DrawRect(innerRect, new Color(0.04f, 0.035f, 0.025f, 0.58f));

        if (item.icon != null)
        {
            Rect iconArea = InsetRect(new Rect(innerRect.x, innerRect.y, innerRect.width, innerRect.height - 18f), Mathf.Clamp(Mathf.Min(innerRect.width, innerRect.height) * 0.08f, 4f, 10f));
            DrawSprite(iconArea, item.icon);
            GUI.Label(new Rect(innerRect.x + 4f, innerRect.yMax - 21f, innerRect.width - 8f, 18f), "$" + item.value, GameGui.HintStyle);
            return;
        }

        GUI.Label(new Rect(innerRect.x + 6f, innerRect.y + 8f, innerRect.width - 12f, innerRect.height - 34f), ShortenNameStatic(item.itemName, 18), GameGui.HintStyle);
        GUI.Label(new Rect(innerRect.x + 6f, innerRect.yMax - 24f, innerRect.width - 12f, 18f), "$" + item.value, GameGui.HintStyle);
    }

    private static Rect InsetRect(Rect rect, float padding)
    {
        return new Rect(rect.x + padding, rect.y + padding, Mathf.Max(1f, rect.width - padding * 2f), Mathf.Max(1f, rect.height - padding * 2f));
    }

    private static Rect GetContainedRect(Rect rect, float contentAspect)
    {
        if (contentAspect <= 0f)
        {
            return rect;
        }

        float rectAspect = rect.width / Mathf.Max(1f, rect.height);

        if (rectAspect > contentAspect)
        {
            float width = rect.height * contentAspect;
            return new Rect(rect.x + (rect.width - width) * 0.5f, rect.y, width, rect.height);
        }

        float height = rect.width / contentAspect;
        return new Rect(rect.x, rect.y + (rect.height - height) * 0.5f, rect.width, height);
    }

    private static void DrawSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
        {
            return;
        }

        Rect textureRect = sprite.textureRect;
        rect = GetContainedRect(rect, textureRect.width / Mathf.Max(1f, textureRect.height));
        Rect texCoords = new Rect(
            textureRect.x / sprite.texture.width,
            textureRect.y / sprite.texture.height,
            textureRect.width / sprite.texture.width,
            textureRect.height / sprite.texture.height);
        GUI.DrawTextureWithTexCoords(rect, sprite.texture, texCoords, true);
    }

    public static Sprite ResolveItemIcon(string itemName, Sprite currentIcon = null)
    {
        if (currentIcon != null)
        {
            return currentIcon;
        }

        string lookupName = string.IsNullOrWhiteSpace(itemName) ? "Item" : itemName;
        TreasureSpawner spawner = UnityEngine.Object.FindAnyObjectByType<TreasureSpawner>();
        TreasureDatabase database = spawner != null ? spawner.treasureDatabase : null;

        string alias = GetIconLookupAlias(lookupName);
        Sprite databaseIcon = database != null
            ? FindIconInDefinitions(database.GetAllIconDefinitions(), lookupName, alias)
            : null;

        return databaseIcon != null ? databaseIcon : GetGeneratedItemIcon(lookupName);
    }

    private static Sprite FindIconInDefinitions(TreasureDefinition[] definitions, string itemName, string alias)
    {
        if (definitions == null)
        {
            return null;
        }

        foreach (TreasureDefinition definition in definitions)
        {
            if (definition == null || definition.icon == null)
            {
                continue;
            }

            if (NamesMatch(definition.treasureName, itemName) || NamesMatch(definition.treasureName, alias))
            {
                return definition.icon;
            }
        }

        return null;
    }

    private static bool NamesMatch(string first, string second)
    {
        return !string.IsNullOrEmpty(first)
            && !string.IsNullOrEmpty(second)
            && string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string GetIconLookupAlias(string itemName)
    {
        switch (itemName)
        {
            case "Rusty Bottle Cap":
                return "Bottle Cap";
            case "Old Nail":
                return "Nail";
            case "Pull Tab":
                return "Soda Can Pull Tab";
            case "Small Coin":
                return "Old Coin";
            case "Bent Spoon":
                return "Silver Teaspoon";
            case "Gold Ring":
                return "Plain Gold Wedding Band";
            case "Jeweled Compass":
                return "Compass";
            case "Working Pocket Watch":
                return "Pocket Watch";
            default:
                return itemName;
        }
    }

    private static Sprite GetGeneratedItemIcon(string itemName)
    {
        string cacheKey = string.IsNullOrEmpty(itemName) ? "Item" : itemName;

        if (GeneratedItemIcons.TryGetValue(cacheKey, out Sprite icon))
        {
            return icon;
        }

        Texture2D texture = CreateGeneratedItemIconTexture(cacheKey);
        icon = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        GeneratedItemIcons[cacheKey] = icon;
        return icon;
    }

    private static Texture2D CreateGeneratedItemIconTexture(string itemName)
    {
        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        Color accent = GetGeneratedIconColor(itemName);
        Color shadow = new Color(0.06f, 0.05f, 0.04f, 0.72f);
        Color shine = Color.Lerp(accent, Color.white, 0.35f);
        DrawIconCircle(texture, 48, 50, 32, shadow);
        DrawIconCircle(texture, 48, 48, 30, accent);
        DrawIconCircle(texture, 39, 59, 8, shine);
        DrawIconRing(texture, 48, 48, 30, Color.Lerp(accent, Color.black, 0.32f), 4);
        DrawIconRect(texture, 34, 43, 28, 10, Color.Lerp(accent, Color.black, 0.22f));
        DrawIconRect(texture, 39, 36, 18, 24, new Color(0.98f, 0.9f, 0.55f, 0.28f));

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private static Color GetGeneratedIconColor(string itemName)
    {
        int hash = 17;

        if (!string.IsNullOrEmpty(itemName))
        {
            for (int i = 0; i < itemName.Length; i++)
            {
                hash = hash * 31 + itemName[i];
            }
        }

        float hue = Mathf.Abs(hash % 360) / 360f;
        return Color.HSVToRGB(hue, 0.42f, 0.9f);
    }

    private static void DrawIconCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
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

    private static void DrawIconRing(Texture2D texture, int centerX, int centerY, int radius, Color color, int thickness)
    {
        int outerSquared = radius * radius;
        int innerRadius = Mathf.Max(0, radius - thickness);
        int innerSquared = innerRadius * innerRadius;

        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                int distanceSquared = dx * dx + dy * dy;

                if (distanceSquared <= outerSquared && distanceSquared >= innerSquared)
                {
                    SetIconPixel(texture, x, y, color);
                }
            }
        }
    }

    private static void DrawIconRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int py = y; py < y + height; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                SetIconPixel(texture, px, py, color);
            }
        }
    }

    private static void SetIconPixel(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
        {
            return;
        }

        texture.SetPixel(x, y, color);
    }

    private void ClampGridSizeToAvailableArt()
    {
        maxGridSize = Mathf.Clamp(maxGridSize, MinimumGridSize, MaximumArtGridSize);
        gridSize = Mathf.Clamp(gridSize, MinimumGridSize, maxGridSize);
    }

    private struct BoardGridLayout
    {
        public readonly float startX;
        public readonly float startY;
        public readonly float slotWidth;
        public readonly float slotHeight;
        public readonly float gapX;
        public readonly float gapY;

        public BoardGridLayout(float startX, float startY, float slotWidth, float slotHeight, float gapX, float gapY)
        {
            this.startX = startX;
            this.startY = startY;
            this.slotWidth = slotWidth;
            this.slotHeight = slotHeight;
            this.gapX = gapX;
            this.gapY = gapY;
        }
    }

    private static string ShortenNameStatic(string itemName, int maxLength)
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
