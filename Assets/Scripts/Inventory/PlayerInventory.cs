using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
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
    public int maxGridSize = 6;
    public List<InventorySlot> items = new List<InventorySlot>();
    public bool isOpen;

    public int Capacity => gridSize * gridSize;
    public int OccupiedSlots => GetOccupiedSlotCount();
    public bool IsFull => !HasFreeSpace(1, 1);

    public bool AddTreasure(DetectableTreasure treasure)
    {
        return AddItem(treasure.treasureName, treasure.value, treasure.inventoryWidth, treasure.inventoryHeight, treasure.icon);
    }

    public bool AddItem(string itemName, int value, int width = 1, int height = 1, Sprite icon = null)
    {
        width = Mathf.Clamp(width, 1, gridSize);
        height = Mathf.Clamp(height, 1, gridSize);

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
        if (RuntimeGameUI.IsActive)
        {
            return;
        }

        if (!isOpen)
        {
            DrawCompactStatus();
            return;
        }

        float cellSize = 58f;
        float cellGap = 5f;
        float headerHeight = 110f;
        float panelWidth = gridSize * cellSize + (gridSize - 1) * cellGap + 30f;
        float panelHeight = headerHeight + gridSize * cellSize + (gridSize - 1) * cellGap + 20f;
        float panelX = Screen.width - panelWidth - 20f;

        GameGui.DrawPanel(new Rect(panelX, 20, panelWidth, panelHeight), "Backpack " + gridSize + "x" + gridSize);
        GUI.Label(new Rect(panelX + 15, 50, panelWidth - 30, 22), "Money: $" + money, GameGui.LabelStyle);
        GUI.Label(new Rect(panelX + 15, 73, panelWidth - 30, 22), "Cargo value: $" + GetInventoryValue(), GameGui.LabelStyle);
        GameGui.DrawProgressBar(
            new Rect(panelX + 15, 100, panelWidth - 30, 16),
            OccupiedSlots / (float)Capacity,
            IsFull ? GameGui.DangerColor : GameGui.AccentColor,
            OccupiedSlots + "/" + Capacity + " slots"
        );

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                float cellX = panelX + 15f + x * (cellSize + cellGap);
                float cellY = 20f + headerHeight + y * (cellSize + cellGap);
                Rect cellRect = new Rect(cellX, cellY, cellSize, cellSize);
                GUI.Box(cellRect, "", GameGui.SlotStyle);
            }
        }

        foreach (InventorySlot item in items)
        {
            float itemX = panelX + 15f + item.gridX * (cellSize + cellGap);
            float itemY = 20f + headerHeight + item.gridY * (cellSize + cellGap);
            float itemWidth = item.width * cellSize + (item.width - 1) * cellGap;
            float itemHeight = item.height * cellSize + (item.height - 1) * cellGap;
            Rect itemRect = new Rect(itemX, itemY, itemWidth, itemHeight);

            GameGui.DrawPanel(itemRect, "");
            GUI.Label(new Rect(itemX + 6f, itemY + 6f, itemWidth - 12f, 34f), ShortenName(item.itemName), GameGui.SmallLabelStyle);
            GUI.Label(new Rect(itemX + 6f, itemY + itemHeight - 22f, itemWidth - 12f, 18f), "$" + item.value + " | " + item.width + "x" + item.height, GameGui.LabelStyle);
        }
    }

    private void DrawCompactStatus()
    {
        Rect rect = new Rect(Screen.width - 230f, 20f, 210f, 76f);
        GameGui.DrawPanel(rect, "");
        GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f), "$" + money + " cash", GameGui.LabelStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 31f, rect.width - 24f, 18f), "Cargo $" + GetInventoryValue(), GameGui.SmallLabelStyle);
        GameGui.DrawProgressBar(
            new Rect(rect.x + 12f, rect.y + 54f, rect.width - 24f, 12f),
            OccupiedSlots / (float)Capacity,
            IsFull ? GameGui.DangerColor : GameGui.AccentColor,
            ""
        );
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

    private int GetOccupiedSlotCount()
    {
        int occupiedSlots = 0;

        foreach (InventorySlot item in items)
        {
            occupiedSlots += Mathf.Max(1, item.width) * Mathf.Max(1, item.height);
        }

        return occupiedSlots;
    }

    private string ShortenName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            return "Item";
        }

        if (itemName.Length <= 10)
        {
            return itemName;
        }

        return itemName.Substring(0, 9) + ".";
    }
}
