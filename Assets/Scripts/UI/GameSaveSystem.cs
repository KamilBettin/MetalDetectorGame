using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameSaveSystem
{
    private const string LegacySaveKey = "MetalDetector.Save.Json";
    private const string SaveSlotKeyPrefix = "MetalDetector.Save.Slot.";
    public const int SaveSlotCount = 3;
    private const int DefaultDetectorUpgradeCost = 250;
    private const int DetectorUpgradeCostIncrease = 350;
    private const int DefaultRangeUpgradeCost = 140;
    private const int RangeUpgradeCostIncrease = 90;
    private const int DefaultInventoryUpgradeCost = 220;
    private const int InventoryUpgradeCostIncrease = 180;
    private const int DefaultShovelUpgradeCost = 550;
    private const int DefaultCraftingUnlockCost = 100;
    private const int TrailerMinimumMoney = 5000;
    private const float DefaultDetectorRange = 6f;
    private const float RangeUpgradeStep = 2f;

    private static GameSaveData initialDefaults;
    private static GameSaveData pauseResumeSnapshot;
    private static int activeSlot = -1;

    public static bool HasSavedGame
    {
        get
        {
            MigrateLegacySave();

            for (int slotIndex = 0; slotIndex < SaveSlotCount; slotIndex++)
            {
                if (PlayerPrefs.HasKey(GetSaveSlotKey(slotIndex)))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static int ActiveSlot => activeSlot;
    public static bool HasPauseResumeSnapshot => pauseResumeSnapshot != null;

    public sealed class SaveSlotInfo
    {
        public int slotIndex;
        public bool isOccupied;
        public bool isValid;
        public string savedAt = "";
        public int dayNumber = 1;
        public int money;
    }

    public static void CaptureInitialDefaults()
    {
        if (initialDefaults != null)
        {
            return;
        }

        initialDefaults = CaptureCurrentData();
    }

    public static void StartNewGame()
    {
        pauseResumeSnapshot = null;
        activeSlot = -1;
        ResetRuntimeToDefaults();
    }

    public static bool ContinueGame(out string message)
    {
        int slotIndex = FindFirstOccupiedSlot();

        if (slotIndex < 0)
        {
            message = "No save file yet. Starting fresh.";
            ResetRuntimeToDefaults();
            return false;
        }

        return ContinueGame(slotIndex, out message);
    }

    public static bool ContinueGame(int slotIndex, out string message)
    {
        MigrateLegacySave();

        if (!IsValidSlot(slotIndex))
        {
            message = "Invalid save slot.";
            return false;
        }

        string saveKey = GetSaveSlotKey(slotIndex);

        if (!PlayerPrefs.HasKey(saveKey))
        {
            message = "This save slot is empty.";
            return false;
        }

        string json = PlayerPrefs.GetString(saveKey, "");

        if (string.IsNullOrWhiteSpace(json))
        {
            message = "This save slot is empty.";
            return false;
        }

        GameSaveData data;

        try
        {
            data = JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (Exception)
        {
            data = null;
        }

        if (data == null)
        {
            message = "Save slot " + (slotIndex + 1) + " could not be loaded.";
            return false;
        }

        ApplyData(data, true);
        activeSlot = slotIndex;
        pauseResumeSnapshot = null;
        message = string.IsNullOrEmpty(data.savedAt)
            ? "Loaded save slot " + (slotIndex + 1) + "."
            : "Loaded slot " + (slotIndex + 1) + " from " + data.savedAt + ".";
        return true;
    }

    public static bool SaveCurrentGame(out string message)
    {
        int slotIndex = IsValidSlot(activeSlot) ? activeSlot : 0;
        return SaveCurrentGame(slotIndex, out message);
    }

    public static bool SaveCurrentGame(int slotIndex, out string message)
    {
        if (!IsValidSlot(slotIndex))
        {
            message = "Invalid save slot.";
            return false;
        }

        LocalCoopManager coopManager = LocalCoopManager.Instance;

        GameSaveData data = CaptureCurrentData();
        data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        PlayerPrefs.SetString(GetSaveSlotKey(slotIndex), JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        activeSlot = slotIndex;
        message = coopManager != null && (coopManager.IsRunning || coopManager.RemotePlayerCount > 0)
            ? "Multiplayer game saved in slot " + (slotIndex + 1) + "."
            : "Game saved in slot " + (slotIndex + 1) + ".";
        return true;
    }

    public static SaveSlotInfo GetSaveSlotInfo(int slotIndex)
    {
        MigrateLegacySave();

        SaveSlotInfo info = new SaveSlotInfo
        {
            slotIndex = slotIndex,
            isOccupied = false,
            isValid = false
        };

        if (!IsValidSlot(slotIndex))
        {
            return info;
        }

        string saveKey = GetSaveSlotKey(slotIndex);

        if (!PlayerPrefs.HasKey(saveKey))
        {
            return info;
        }

        info.isOccupied = true;
        string json = PlayerPrefs.GetString(saveKey, "");

        try
        {
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

            if (data != null)
            {
                info.isValid = true;
                info.savedAt = data.savedAt ?? "";
                info.dayNumber = Mathf.Max(1, data.dayNumber);
                info.money = Mathf.Max(0, data.money);
            }
        }
        catch (Exception)
        {
            info.isValid = false;
        }

        return info;
    }

    public static void CapturePauseResumeSnapshotIfSolo()
    {
        LocalCoopManager coopManager = LocalCoopManager.Instance;

        if (coopManager != null && coopManager.IsRunning)
        {
            pauseResumeSnapshot = null;
            return;
        }

        pauseResumeSnapshot = CaptureCurrentData();
    }

    public static void ClearPauseResumeSnapshot()
    {
        pauseResumeSnapshot = null;
    }

    public static void ClearSavedGame()
    {
        bool changed = false;

        for (int slotIndex = 0; slotIndex < SaveSlotCount; slotIndex++)
        {
            string saveKey = GetSaveSlotKey(slotIndex);

            if (!PlayerPrefs.HasKey(saveKey))
            {
                continue;
            }

            PlayerPrefs.DeleteKey(saveKey);
            changed = true;
        }

        if (PlayerPrefs.HasKey(LegacySaveKey))
        {
            PlayerPrefs.DeleteKey(LegacySaveKey);
            changed = true;
        }

        if (changed)
        {
            PlayerPrefs.Save();
        }

        activeSlot = -1;
    }

    private static int FindFirstOccupiedSlot()
    {
        MigrateLegacySave();

        for (int slotIndex = 0; slotIndex < SaveSlotCount; slotIndex++)
        {
            SaveSlotInfo info = GetSaveSlotInfo(slotIndex);

            if (info.isOccupied && info.isValid)
            {
                return slotIndex;
            }
        }

        return -1;
    }

    private static void MigrateLegacySave()
    {
        if (!PlayerPrefs.HasKey(LegacySaveKey))
        {
            return;
        }

        string firstSlotKey = GetSaveSlotKey(0);

        if (!PlayerPrefs.HasKey(firstSlotKey))
        {
            PlayerPrefs.SetString(firstSlotKey, PlayerPrefs.GetString(LegacySaveKey, ""));
        }

        PlayerPrefs.DeleteKey(LegacySaveKey);
        PlayerPrefs.Save();
    }

    private static bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < SaveSlotCount;
    }

    private static string GetSaveSlotKey(int slotIndex)
    {
        return SaveSlotKeyPrefix + slotIndex;
    }

    private static void ResetRuntimeToDefaults()
    {
        GameSaveData defaults = initialDefaults ?? CaptureCurrentData();
        GameSaveData resetData = defaults.Clone();
        resetData.savedAt = "";
        resetData.inventoryItems.Clear();
        resetData.homeStoredItems.Clear();
        resetData.completedNpcQuestIds.Clear();
        resetData.unlockedSearchAreaIds.Clear();
        resetData.treasures.Clear();
        resetData.remotePlayers.Clear();
        resetData.wasMultiplayer = false;
        ApplyData(resetData, false);

        TreasureSpawner spawner = UnityEngine.Object.FindAnyObjectByType<TreasureSpawner>();

        if (spawner != null)
        {
            spawner.RegenerateTreasures();
        }
    }

    private static GameSaveData CaptureCurrentData()
    {
        GameSaveData data = new GameSaveData();
        CapturePlayer(data);
        CaptureInventory(data);
        CaptureDetector(data);
        CaptureShop(data);
        CaptureDayNight(data);
        CaptureSearchAreas(data);
        CaptureTreasures(data);
        CaptureHome(data);
        CaptureNpcQuests(data);
        CaptureMultiplayer(data);
        return data;
    }

    private static void CapturePlayer(GameSaveData data)
    {
        Transform player = PlayerRigReferences.FindLocalPlayerTransform();

        if (player == null)
        {
            return;
        }

        data.hasPlayerTransform = true;
        data.playerPosition = SerializableVector3.FromVector3(player.position);
        data.playerYaw = player.eulerAngles.y;
    }

    private static void CaptureInventory(GameSaveData data)
    {
        PlayerInventory inventory = PlayerRigReferences.FindLocalInventory();

        if (inventory == null)
        {
            return;
        }

        data.money = inventory.money;
        data.gridSize = inventory.gridSize;
        data.maxGridSize = inventory.maxGridSize;
        data.inventoryItems = CaptureItems(inventory.items);
    }

    private static void CaptureDetector(GameSaveData data)
    {
        MetalDetector detector = PlayerRigReferences.FindLocalMetalDetector();

        if (detector != null)
        {
            data.detectorRange = detector.DetectionRange;
            data.detectorTier = detector.detectorTier;
        }

    }

    private static void CaptureShop(GameSaveData data)
    {
        UpgradeShop shop = FindUpgradeShop();

        if (shop == null)
        {
            return;
        }

        data.detectorUpgradeCost = shop.detectorUpgradeCost;
        data.rangeUpgradeCost = shop.rangeUpgradeCost;
        data.inventoryUpgradeCost = shop.inventoryUpgradeCost;
        data.shovelUpgradeCost = shop.shovelUpgradeCost;
        data.shovelUpgraded = shop.shovelUpgraded;
        data.craftingUnlockCost = shop.craftingUnlockCost;
        data.craftingUnlocked = shop.craftingUnlocked;
    }

    private static void CaptureDayNight(GameSaveData data)
    {
        DayNightCycle cycle = DayNightCycle.Instance;

        if (cycle == null)
        {
            return;
        }

        data.dayNumber = cycle.DayNumber;
        data.isNight = cycle.IsNight;
        data.phase01 = cycle.Phase01;
    }

    private static void CaptureSearchAreas(GameSaveData data)
    {
        SearchArea[] areas = UnityEngine.Object.FindObjectsByType<SearchArea>();

        foreach (SearchArea area in areas)
        {
            if (area != null && area.isUnlocked)
            {
                data.unlockedSearchAreaIds.Add(area.MultiplayerId);
            }
        }

        data.unlockedSearchAreaIds.Sort(StringComparer.Ordinal);
    }

    private static void CaptureTreasures(GameSaveData data)
    {
        DetectableTreasure[] treasures = UnityEngine.Object.FindObjectsByType<DetectableTreasure>();

        foreach (DetectableTreasure treasure in treasures)
        {
            if (treasure == null || string.IsNullOrEmpty(treasure.multiplayerId))
            {
                continue;
            }

            data.treasures.Add(new SavedTreasure
            {
                id = treasure.multiplayerId,
                itemName = treasure.treasureName,
                value = treasure.value,
                rarity = treasure.rarity,
                position = SerializableVector3.FromVector3(treasure.transform.position),
                width = 1,
                height = 1,
                requiredDigHits = treasure.requiredDigHits,
                currentDigHits = treasure.currentDigHits,
                isRevealed = treasure.isRevealed,
                isFound = treasure.isFound
            });
        }
    }

    private static void CaptureHome(GameSaveData data)
    {
        PlayerHome home = UnityEngine.Object.FindAnyObjectByType<PlayerHome>();

        if (home != null)
        {
            data.homeStoredItems = CaptureItems(home.ExportStoredItems());
        }
    }

    private static void CaptureNpcQuests(GameSaveData data)
    {
        NpcQuestGiver questGiver = UnityEngine.Object.FindAnyObjectByType<NpcQuestGiver>();

        if (questGiver == null)
        {
            return;
        }

        data.completedNpcQuestIds = questGiver.GetCompletedQuestIds();
    }

    private static void CaptureMultiplayer(GameSaveData data)
    {
        LocalCoopManager coopManager = LocalCoopManager.Instance;

        if (coopManager == null)
        {
            return;
        }

        data.wasMultiplayer = coopManager.IsRunning || coopManager.RemotePlayerCount > 0;
        data.localPlayerId = coopManager.LocalPlayerId;
        data.coopRole = (int)coopManager.Role;
        data.remotePlayers.Clear();

        foreach (LocalCoopManager.SavedRemotePlayerState remotePlayer in coopManager.CaptureRemotePlayerStates())
        {
            data.remotePlayers.Add(new SavedRemotePlayer
            {
                playerId = remotePlayer.playerId,
                playerName = remotePlayer.playerName,
                position = SerializableVector3.FromVector3(remotePlayer.position),
                rotation = SerializableQuaternion.FromQuaternion(remotePlayer.rotation),
                characterIndex = remotePlayer.characterIndex
            });
        }
    }

    private static List<SavedInventoryItem> CaptureItems(IList<PlayerInventory.InventorySlot> items)
    {
        List<SavedInventoryItem> savedItems = new List<SavedInventoryItem>();

        if (items == null)
        {
            return savedItems;
        }

        foreach (PlayerInventory.InventorySlot item in items)
        {
            if (item == null)
            {
                continue;
            }

            savedItems.Add(new SavedInventoryItem
            {
                itemName = item.itemName,
                value = item.value,
                width = item.width,
                height = item.height,
                gridX = item.gridX,
                gridY = item.gridY
            });
        }

        return savedItems;
    }

    private static void ApplyData(GameSaveData data, bool restoreSavedTreasures)
    {
        if (data == null)
        {
            return;
        }

        ApplyPlayer(data);
        ApplyInventory(data);
        ApplyDetector(data);
        ApplyShop(data);
        ApplyDayNight(data);
        ApplySearchAreas(data);

        if (restoreSavedTreasures)
        {
            RestoreTreasures(data);
        }

        ApplyHome(data);
        ApplyNpcQuests(data);
        ApplyMultiplayer(data);
    }

    private static void ApplyPlayer(GameSaveData data)
    {
        if (!data.hasPlayerTransform)
        {
            return;
        }

        Transform player = PlayerRigReferences.FindLocalPlayerTransform();

        if (player == null)
        {
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;

        if (wasEnabled)
        {
            controller.enabled = false;
        }

        player.position = data.playerPosition.ToVector3();
        player.rotation = Quaternion.Euler(0f, data.playerYaw, 0f);

        if (wasEnabled)
        {
            controller.enabled = true;
        }
    }

    private static void ApplyInventory(GameSaveData data)
    {
        PlayerInventory inventory = PlayerRigReferences.FindLocalInventory();

        if (inventory == null)
        {
            return;
        }

        inventory.SetOpen(false);
        inventory.money = Mathf.Max(TrailerMinimumMoney, data.money);
        inventory.maxGridSize = Mathf.Max(3, data.maxGridSize);
        inventory.gridSize = Mathf.Clamp(data.gridSize <= 0 ? 3 : data.gridSize, 3, inventory.maxGridSize);
        inventory.items.Clear();

        foreach (SavedInventoryItem item in data.inventoryItems)
        {
            inventory.items.Add(CreateInventorySlot(item));
        }
    }

    private static void ApplyDetector(GameSaveData data)
    {
        MetalDetector detector = PlayerRigReferences.FindLocalMetalDetector();

        if (detector != null)
        {
            detector.detectorTier = Mathf.Clamp(data.detectorTier, 0, detector.MaxDetectorTier);
            float tierRange = detector.GetSignalRangeForTier(detector.DetectorTier);
            float savedEffectiveRange = data.detectorRange > 0f ? data.detectorRange : tierRange;
            float purchasedRangeBonus = Mathf.Max(0f, savedEffectiveRange - tierRange);
            detector.detectionRange = detector.GetSignalRangeForTier(0) + purchasedRangeBonus;
        }

    }

    private static void ApplyShop(GameSaveData data)
    {
        UpgradeShop shop = FindUpgradeShop();

        if (shop == null)
        {
            return;
        }

        shop.detectorUpgradeCost = Mathf.Max(data.detectorUpgradeCost, GetExpectedDetectorUpgradeCost());
        shop.rangeUpgradeCost = Mathf.Max(data.rangeUpgradeCost, GetExpectedRangeUpgradeCost());
        shop.inventoryUpgradeCost = Mathf.Max(data.inventoryUpgradeCost, GetExpectedInventoryUpgradeCost());
        shop.shovelUpgradeCost = Mathf.Max(data.shovelUpgradeCost, DefaultShovelUpgradeCost);
        shop.shovelUpgraded = data.shovelUpgraded;
        shop.craftingUnlockCost = DefaultCraftingUnlockCost;
        shop.craftingUnlocked = data.craftingUnlocked;
        shop.SetMenuOpen(false);
    }

    private static int GetExpectedDetectorUpgradeCost()
    {
        MetalDetector detector = PlayerRigReferences.FindLocalMetalDetector();
        int tier = detector != null ? detector.DetectorTier : 0;
        return DefaultDetectorUpgradeCost + (tier * DetectorUpgradeCostIncrease);
    }

    private static int GetExpectedRangeUpgradeCost()
    {
        MetalDetector detector = PlayerRigReferences.FindLocalMetalDetector();
        float range = detector != null ? detector.DetectionRange : DefaultDetectorRange;
        int upgrades = Mathf.Max(0, Mathf.RoundToInt((range - DefaultDetectorRange) / RangeUpgradeStep));
        return DefaultRangeUpgradeCost + (upgrades * RangeUpgradeCostIncrease);
    }

    private static int GetExpectedInventoryUpgradeCost()
    {
        PlayerInventory inventory = PlayerRigReferences.FindLocalInventory();
        int gridSize = inventory != null ? inventory.gridSize : 3;
        int upgrades = Mathf.Max(0, gridSize - 3);
        return DefaultInventoryUpgradeCost + (upgrades * InventoryUpgradeCostIncrease);
    }

    private static UpgradeShop FindUpgradeShop()
    {
        UpgradeShop[] shops = UnityEngine.Object.FindObjectsByType<UpgradeShop>(FindObjectsInactive.Include);

        foreach (UpgradeShop shop in shops)
        {
            if (shop != null && shop.CanUpgradeHere)
            {
                return shop;
            }
        }

        return shops.Length > 0 ? shops[0] : null;
    }

    private static void ApplyDayNight(GameSaveData data)
    {
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.ApplySavedState(Mathf.Max(1, data.dayNumber), data.isNight, data.phase01);
        }
    }

    private static void ApplySearchAreas(GameSaveData data)
    {
        HashSet<string> unlockedIds = new HashSet<string>(data.unlockedSearchAreaIds ?? new List<string>());
        SearchArea[] areas = UnityEngine.Object.FindObjectsByType<SearchArea>();

        foreach (SearchArea area in areas)
        {
            if (area == null)
            {
                continue;
            }

            area.ClearTreasureSpawnState();
            area.SetUnlocked(unlockedIds.Contains(area.MultiplayerId), false);
        }
    }

    private static void RestoreTreasures(GameSaveData data)
    {
        TreasureSpawner spawner = UnityEngine.Object.FindAnyObjectByType<TreasureSpawner>();

        if (spawner == null)
        {
            return;
        }

        spawner.ClearSpawnedTreasures();

        foreach (SavedTreasure treasure in data.treasures)
        {
            if (IsRemovedLootItem(treasure.itemName))
            {
                continue;
            }

            RestoreTreasure(spawner, treasure);
        }

        SearchArea[] areas = UnityEngine.Object.FindObjectsByType<SearchArea>();

        foreach (SearchArea area in areas)
        {
            if (area != null && area.isUnlocked)
            {
                area.MarkTreasuresSpawned();
            }
        }
    }

    private static void RestoreTreasure(TreasureSpawner spawner, SavedTreasure savedTreasure)
    {
        if (savedTreasure == null || string.IsNullOrEmpty(savedTreasure.id))
        {
            return;
        }

        GameObject treasureObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        treasureObject.name = "Treasure - " + savedTreasure.itemName;
        treasureObject.transform.SetParent(spawner.transform);
        treasureObject.transform.position = savedTreasure.position.ToVector3();
        treasureObject.transform.localScale = Vector3.one * 0.25f;

        DetectableTreasure treasure = treasureObject.AddComponent<DetectableTreasure>();
        treasure.multiplayerId = savedTreasure.id;
        treasure.treasureName = savedTreasure.itemName;
        treasure.value = savedTreasure.value;
        treasure.rarity = savedTreasure.rarity;
        treasure.inventoryWidth = 1;
        treasure.inventoryHeight = 1;
        treasure.requiredDigHits = Mathf.Max(1, savedTreasure.requiredDigHits);
        treasure.currentDigHits = Mathf.Clamp(savedTreasure.currentDigHits, 0, treasure.requiredDigHits);
        treasure.isRevealed = savedTreasure.isRevealed;
        treasure.isFound = savedTreasure.isFound;
        treasure.icon = PlayerInventory.ResolveItemIcon(savedTreasure.itemName, FindTreasureIcon(savedTreasure.itemName));

        Renderer treasureRenderer = treasureObject.GetComponent<Renderer>();

        if (treasureRenderer != null)
        {
            treasureRenderer.enabled = spawner.showDebugTreasures && !treasure.isFound;
        }

        Collider treasureCollider = treasureObject.GetComponent<Collider>();

        if (treasureCollider != null && treasure.isFound)
        {
            treasureCollider.enabled = false;
        }
    }

    private static void ApplyHome(GameSaveData data)
    {
        PlayerHome home = UnityEngine.Object.FindAnyObjectByType<PlayerHome>();

        if (home == null)
        {
            return;
        }

        List<PlayerInventory.InventorySlot> storedItems = new List<PlayerInventory.InventorySlot>();

        foreach (SavedInventoryItem item in data.homeStoredItems)
        {
            storedItems.Add(CreateInventorySlot(item));
        }

        home.ImportStoredItems(storedItems);
        home.SetMenuOpen(false);
    }

    private static void ApplyNpcQuests(GameSaveData data)
    {
        NpcQuestGiver questGiver = UnityEngine.Object.FindAnyObjectByType<NpcQuestGiver>();

        if (questGiver == null)
        {
            return;
        }

        questGiver.SetMenuOpen(false);
        questGiver.ApplyCompletedQuestIds(data.completedNpcQuestIds);
    }

    private static void ApplyMultiplayer(GameSaveData data)
    {
        LocalCoopManager coopManager = LocalCoopManager.Instance;

        if (coopManager == null)
        {
            return;
        }

        List<LocalCoopManager.SavedRemotePlayerState> remotePlayerStates = new List<LocalCoopManager.SavedRemotePlayerState>();
        List<SavedRemotePlayer> savedRemotePlayers = data.remotePlayers ?? new List<SavedRemotePlayer>();

        foreach (SavedRemotePlayer remotePlayer in savedRemotePlayers)
        {
            if (remotePlayer == null)
            {
                continue;
            }

            remotePlayerStates.Add(new LocalCoopManager.SavedRemotePlayerState
            {
                playerId = remotePlayer.playerId,
                playerName = remotePlayer.playerName,
                position = remotePlayer.position.ToVector3(),
                rotation = remotePlayer.rotation.ToQuaternion(),
                characterIndex = remotePlayer.characterIndex
            });
        }

        if (data.wasMultiplayer || remotePlayerStates.Count > 0)
        {
            coopManager.RestoreSavedRemotePlayerStates(remotePlayerStates);
            return;
        }

        if (!coopManager.IsRunning && coopManager.RemotePlayerCount > 0)
        {
            coopManager.ClearRemotePlayerVisuals();
        }
    }

    private static PlayerInventory.InventorySlot CreateInventorySlot(SavedInventoryItem item)
    {
        bool isTreasureItem = IsKnownTreasureItem(item.itemName);

        return new PlayerInventory.InventorySlot
        {
            itemName = item.itemName,
            value = item.value,
            icon = PlayerInventory.ResolveItemIcon(item.itemName, FindTreasureIcon(item.itemName)),
            width = isTreasureItem ? 1 : Mathf.Max(1, item.width),
            height = isTreasureItem ? 1 : Mathf.Max(1, item.height),
            gridX = Mathf.Max(0, item.gridX),
            gridY = Mathf.Max(0, item.gridY)
        };
    }

    private static Sprite FindTreasureIcon(string itemName)
    {
        TreasureDatabase database = FindTreasureDatabase();

        if (database == null)
        {
            return null;
        }

        return FindIconInDefinitions(database.GetAllIconDefinitions(), itemName);
    }

    private static bool IsKnownTreasureItem(string itemName)
    {
        TreasureDatabase database = FindTreasureDatabase();

        if (database == null)
        {
            return IsRuntimeDefaultTreasureName(itemName);
        }

        return HasTreasureDefinition(database.GetAllTreasures(), itemName);
    }

    private static TreasureDatabase FindTreasureDatabase()
    {
        TreasureSpawner spawner = UnityEngine.Object.FindAnyObjectByType<TreasureSpawner>();
        return spawner != null ? spawner.treasureDatabase : null;
    }

    private static bool HasTreasureDefinition(TreasureDefinition[] definitions, string itemName)
    {
        if (definitions == null)
        {
            return false;
        }

        string lookupName = GetTreasureLookupAlias(itemName);

        foreach (TreasureDefinition definition in definitions)
        {
            if (definition != null && (definition.treasureName == itemName || definition.treasureName == lookupName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRuntimeDefaultTreasureName(string itemName)
    {
        switch (itemName)
        {
            case "Rusty Bottle Cap":
            case "Old Nail":
            case "Pull Tab":
            case "Crushed Can":
            case "Small Coin":
            case "Bent Spoon":
            case "Watch Fragment":
            case "Pocket Watch":
            case "Silver Ring":
            case "Old Dagger":
            case "Gold Ring":
            case "Jeweled Compass":
            case "Ancient Relic":
                return true;
            default:
                return false;
        }
    }

    private static Sprite FindIconInDefinitions(TreasureDefinition[] definitions, string itemName)
    {
        if (definitions == null)
        {
            return null;
        }

        string lookupName = GetTreasureLookupAlias(itemName);

        foreach (TreasureDefinition definition in definitions)
        {
            if (definition != null && (definition.treasureName == itemName || definition.treasureName == lookupName))
            {
                return definition.icon;
            }
        }

        return null;
    }

    private static string GetTreasureLookupAlias(string itemName)
    {
        return itemName == "Gold Ring" ? "Plain Gold Wedding Band" : itemName;
    }

    private static bool IsRemovedLootItem(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return false;
        }

        if (itemName.IndexOf("key", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        switch (itemName.Trim().ToLowerInvariant())
        {
            case "metal plate":
            case "metal lighter":
            case "old lighter":
            case "small screwdriver":
            case "old hammer":
            case "small chisel":
            case "saw fragment":
            case "metal file":
            case "old forged nail":
            case "small sheathed fishing knife":
            case "old toy car":
            case "rusty open end wrench":
            case "small adjustable wrench":
                return true;
            default:
                return false;
        }
    }

    [Serializable]
    private class GameSaveData
    {
        public string savedAt = "";
        public bool hasPlayerTransform;
        public SerializableVector3 playerPosition;
        public float playerYaw;
        public int money;
        public int gridSize = 3;
        public int maxGridSize = 5;
        public List<SavedInventoryItem> inventoryItems = new List<SavedInventoryItem>();
        public float detectorRange = DefaultDetectorRange;
        public int detectorTier;
        public int detectorUpgradeCost = DefaultDetectorUpgradeCost;
        public int rangeUpgradeCost = DefaultRangeUpgradeCost;
        public int inventoryUpgradeCost = DefaultInventoryUpgradeCost;
        public int shovelUpgradeCost = DefaultShovelUpgradeCost;
        public bool shovelUpgraded;
        public int craftingUnlockCost = DefaultCraftingUnlockCost;
        public bool craftingUnlocked;
        public int dayNumber = 1;
        public bool isNight;
        public float phase01;
        public List<string> unlockedSearchAreaIds = new List<string>();
        public List<SavedTreasure> treasures = new List<SavedTreasure>();
        public List<SavedInventoryItem> homeStoredItems = new List<SavedInventoryItem>();
        public List<string> completedNpcQuestIds = new List<string>();
        public bool wasMultiplayer;
        public int localPlayerId;
        public int coopRole;
        public List<SavedRemotePlayer> remotePlayers = new List<SavedRemotePlayer>();

        public GameSaveData Clone()
        {
            return JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(this));
        }
    }

    [Serializable]
    private class SavedInventoryItem
    {
        public string itemName;
        public int value;
        public int width = 1;
        public int height = 1;
        public int gridX;
        public int gridY;
    }

    [Serializable]
    private class SavedTreasure
    {
        public string id;
        public string itemName;
        public int value;
        public TreasureRarity rarity;
        public SerializableVector3 position;
        public int width = 1;
        public int height = 1;
        public int requiredDigHits = 3;
        public int currentDigHits;
        public bool isRevealed;
        public bool isFound;
    }

    [Serializable]
    private class SavedRemotePlayer
    {
        public int playerId;
        public string playerName;
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        public int characterIndex;
    }

    [Serializable]
    private struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public static SerializableVector3 FromVector3(Vector3 value)
        {
            return new SerializableVector3 { x = value.x, y = value.y, z = value.z };
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    private struct SerializableQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static SerializableQuaternion FromQuaternion(Quaternion value)
        {
            return new SerializableQuaternion { x = value.x, y = value.y, z = value.z, w = value.w };
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w == 0f ? 1f : w);
        }
    }
}
