using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DiggingController : MonoBehaviour
{
    public float digRange = 1.8f;
    public float digCooldownPadding = 0.05f;
    public PlayerInventory playerInventory;
    public UpgradeShop upgradeShop;
    public GameObject rustyShovelPrefab;
    public GameObject upgradedShovelPrefab;
    public GameObject buriedChestPrefab;
    public Material dugSandMaterial;
    public Material dugSoilMaterial;
    public Vector3 rustyShovelLocalPosition = new Vector3(0f, 0.12f, 0f);
    public Vector3 rustyShovelLocalEulerAngles = new Vector3(-8f, 0f, 0f);
    public Vector3 rustyShovelLocalScale = Vector3.one * 0.24f;
    public Vector3 cleanShovelLocalPosition = Vector3.zero;
    public Vector3 cleanShovelLocalEulerAngles = new Vector3(-8f, 0f, 0f);
    public Vector3 cleanShovelLocalScale = Vector3.one * 1.16f;
    public float chestSearchRange = 2f;

    private string lastFoundMessage = "";
    private float messageTimer;
    private Sprite lastFoundIcon;
    private bool lastFoundWasTreasure;
    private string lastFoundItemName = "";
    private int lastFoundValue;
    private int lastDigCurrentHits;
    private int lastDigRequiredHits;
    private float nextDigAllowedTime;
    private bool chestSearchInProgress;

    public string LastFoundMessage => lastFoundMessage;
    public float MessageTimer => messageTimer;
    public Sprite LastFoundIcon => lastFoundIcon;
    public bool LastFoundWasTreasure => lastFoundWasTreasure;
    public string LastFoundItemName => lastFoundItemName;
    public int LastFoundValue => lastFoundValue;
    public int LastDigCurrentHits => lastDigCurrentHits;
    public int LastDigRequiredHits => lastDigRequiredHits;
    public float LastDigProgress01 => lastDigRequiredHits > 0 ? Mathf.Clamp01(lastDigCurrentHits / (float)lastDigRequiredHits) : 0f;

    public bool HasDigTargetInRange => FindClosestTreasureInRange() != null;
    public bool HasSearchableChestInRange => FindClosestSearchableChestInRange() != null;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();

        if (playerInventory == null)
        {
            playerInventory = gameObject.AddComponent<PlayerInventory>();
        }

        upgradeShop = FindBestShop();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (chestSearchInProgress)
            {
                return;
            }

            if (GameUIState.AnyMenuOpen || (upgradeShop != null && upgradeShop.IsPlayerNearShop()) || PlayerHome.AnyHomeInteractionInRange() || NpcQuestGiver.AnyQuestGiverInteractionInRange())
            {
                return;
            }

            if (!TrySearchReadyChest())
            {
                TryDig();
            }
        }

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
        }
    }

    private void TryDig()
    {
        if (IsDigCooldownActive())
        {
            return;
        }

        if (DayNightCycle.IsNightNow)
        {
            lastFoundMessage = "Too dark to search. Sleep at home until morning.";
            lastFoundIcon = null;
            lastFoundWasTreasure = false;
            lastFoundItemName = "";
            lastFoundValue = 0;
            ClearDigProgress();
            messageTimer = 2.4f;
            return;
        }

        DetectableTreasure treasure = FindClosestTreasureInRange();

        if (treasure == null)
        {
            lastFoundMessage = "No marked target here. Scan with LMB first.";
            lastFoundIcon = null;
            lastFoundWasTreasure = false;
            lastFoundItemName = "";
            lastFoundValue = 0;
            ClearDigProgress();
            messageTimer = 2f;
            return;
        }

        bool digComplete = treasure.DigOnce();
        DigSiteVisual digSite = UpdateDigSite(treasure);

        if (!digComplete)
        {
            StartDigCooldown(PlayDigVisual(treasure, false));
            lastFoundMessage = "Digging: " + treasure.currentDigHits + "/" + treasure.requiredDigHits;
            lastFoundIcon = null;
            lastFoundWasTreasure = false;
            lastFoundItemName = "";
            lastFoundValue = 0;
            lastDigCurrentHits = treasure.currentDigHits;
            lastDigRequiredHits = treasure.requiredDigHits;
            messageTimer = 1.5f;
            return;
        }

        StartDigCooldown(PlayDigVisual(treasure, true));
        PrepareSearchableChest(treasure, digSite);
    }

    private bool TrySearchReadyChest()
    {
        if (chestSearchInProgress)
        {
            return true;
        }

        if (IsDigCooldownActive())
        {
            return false;
        }

        DetectableTreasure treasure = FindClosestSearchableChestInRange();

        if (treasure == null)
        {
            return false;
        }

        if (playerInventory == null || !playerInventory.HasFreeSpace(1, 1))
        {
            ShowBackpackFullMessage();
            return true;
        }

        DigSiteVisual digSite = DigSiteVisual.FindForTreasure(treasure);
        StartCoroutine(SearchChestRoutine(treasure, digSite));
        return true;
    }

    private IEnumerator SearchChestRoutine(DetectableTreasure treasure, DigSiteVisual digSite)
    {
        chestSearchInProgress = true;
        ClearDigProgress();
        lastFoundMessage = "Searching chest...";
        lastFoundIcon = null;
        lastFoundWasTreasure = false;
        lastFoundItemName = "";
        lastFoundValue = 0;
        messageTimer = 1.5f;

        float duration = digSite != null ? digSite.PlaySearchAnimation() : 0.85f;
        nextDigAllowedTime = Time.time + duration + digCooldownPadding;
        yield return new WaitForSeconds(duration);

        if (treasure != null && !treasure.isFound)
        {
            SearchChest(treasure);
        }

        chestSearchInProgress = false;
    }

    private void SearchChest(DetectableTreasure treasure)
    {
        if (!playerInventory.AddTreasure(treasure))
        {
            ShowBackpackFullMessage();
            return;
        }

        treasure.isFound = true;

        DigSiteVisual digSite = DigSiteVisual.FindForTreasure(treasure);

        if (digSite != null)
        {
            digSite.MarkSearched();
        }

        lastFoundMessage = "Found: " + treasure.treasureName + " ($" + treasure.value + ")!";
        lastFoundIcon = treasure.icon;
        lastFoundWasTreasure = true;
        lastFoundItemName = treasure.treasureName;
        lastFoundValue = treasure.value;
        ClearDigProgress();
        messageTimer = GetFoundMessageDuration(treasure.value);
        GameEvents.ReportTreasureFound(treasure);

        if (treasure.revealMarker != null)
        {
            Destroy(treasure.revealMarker.gameObject);
        }

        Renderer treasureRenderer = treasure.GetComponent<Renderer>();

        if (treasureRenderer != null)
        {
            treasureRenderer.enabled = false;
        }

        Collider treasureCollider = treasure.GetComponent<Collider>();

        if (treasureCollider != null)
        {
            treasureCollider.enabled = false;
        }
    }

    private void PrepareSearchableChest(DetectableTreasure treasure, DigSiteVisual digSite)
    {
        if (digSite != null)
        {
            digSite.SetProgress(1f);
        }

        if (treasure.revealMarker != null)
        {
            Destroy(treasure.revealMarker.gameObject);
        }

        Renderer treasureRenderer = treasure.GetComponent<Renderer>();

        if (treasureRenderer != null)
        {
            treasureRenderer.enabled = false;
        }

        Collider treasureCollider = treasure.GetComponent<Collider>();

        if (treasureCollider != null)
        {
            treasureCollider.enabled = false;
        }

        lastFoundMessage = "Chest exposed. Press E to search.";
        lastFoundIcon = null;
        lastFoundWasTreasure = false;
        lastFoundItemName = "";
        lastFoundValue = 0;
        ClearDigProgress();
        messageTimer = 2.4f;
    }

    private void ClearDigProgress()
    {
        lastDigCurrentHits = 0;
        lastDigRequiredHits = 0;
    }

    public DetectableTreasure FindClosestTreasureInRange()
    {
        return FindClosestTreasureInRange(digRange, false);
    }

    public DetectableTreasure FindClosestSearchableChestInRange()
    {
        return FindClosestTreasureInRange(chestSearchRange, true);
    }

    public Vector3 GetChestPromptPosition(DetectableTreasure treasure)
    {
        DigSiteVisual digSite = DigSiteVisual.FindForTreasure(treasure);

        if (digSite != null)
        {
            return digSite.PromptPosition;
        }

        return GetDigSitePosition(treasure) + Vector3.up * 0.85f;
    }

    public Transform GetChestHighlightTarget(DetectableTreasure treasure)
    {
        DigSiteVisual digSite = DigSiteVisual.FindForTreasure(treasure);
        return digSite != null ? digSite.HighlightTarget : treasure != null ? treasure.transform : null;
    }

    private DetectableTreasure FindClosestTreasureInRange(float range, bool requireSearchableChest)
    {
        if (DayNightCycle.IsNightNow)
        {
            return null;
        }

        DetectableTreasure[] treasures = FindObjectsByType<DetectableTreasure>();
        DetectableTreasure closestTreasure = null;
        float closestDistance = range;

        foreach (DetectableTreasure treasure in treasures)
        {
            if (treasure.isFound)
            {
                continue;
            }

            if (!treasure.isRevealed)
            {
                continue;
            }

            bool searchableChest = IsSearchableChest(treasure);

            if (searchableChest != requireSearchableChest)
            {
                continue;
            }

            float distance = GetGroundDistance(transform.position, treasure.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTreasure = treasure;
            }
        }

        if (closestTreasure != null && closestTreasure.currentDigHits > 0)
        {
            UpdateDigSite(closestTreasure);
        }

        return closestTreasure;
    }

    private bool IsSearchableChest(DetectableTreasure treasure)
    {
        if (treasure == null || !treasure.isRevealed || treasure.isFound || !treasure.IsDugUp)
        {
            return false;
        }

        DigSiteVisual digSite = DigSiteVisual.FindForTreasure(treasure);
        return digSite == null || !digSite.IsSearchAnimationPlaying;
    }

    private void ShowBackpackFullMessage()
    {
        lastFoundMessage = "Backpack is full. Make room, then search the chest.";
        lastFoundIcon = null;
        lastFoundWasTreasure = false;
        lastFoundItemName = "";
        lastFoundValue = 0;
        ClearDigProgress();
        messageTimer = 3f;
    }

    private DigSiteVisual UpdateDigSite(DetectableTreasure treasure)
    {
        if (treasure == null)
        {
            return null;
        }

        DigSiteVisual digSite = DigSiteVisual.EnsureForTreasure(treasure, GetDigSitePosition(treasure), buriedChestPrefab, dugSandMaterial, dugSoilMaterial);
        digSite.SetProgress(treasure.DigProgress01);
        return digSite;
    }

    private Vector3 GetDigSitePosition(DetectableTreasure treasure)
    {
        Vector3 markerPosition = treasure.transform.position;

        if (treasure.revealMarker != null)
        {
            markerPosition = treasure.revealMarker.transform.position;
        }

        return new Vector3(markerPosition.x, GetSurfaceY(markerPosition), markerPosition.z);
    }

    private float GetGroundDistance(Vector3 firstPosition, Vector3 secondPosition)
    {
        Vector2 first = new Vector2(firstPosition.x, firstPosition.z);
        Vector2 second = new Vector2(secondPosition.x, secondPosition.z);
        return Vector2.Distance(first, second);
    }

    private bool IsDigCooldownActive()
    {
        return Time.time < nextDigAllowedTime;
    }

    private void StartDigCooldown(float effectDuration)
    {
        nextDigAllowedTime = Time.time + Mathf.Max(0.05f, effectDuration + digCooldownPadding);
    }

    private float GetFoundMessageDuration(int value)
    {
        if (value > 100)
        {
            return 5.6f;
        }

        if (value >= 50)
        {
            return 4.8f;
        }

        if (value >= 10)
        {
            return 4.2f;
        }

        return 3.2f;
    }

    private float GetSurfaceY(Vector3 worldPosition)
    {
        Terrain terrain = GetTerrainAt(worldPosition);

        if (terrain == null)
        {
            return worldPosition.y;
        }

        return GetTerrainWorldHeight(terrain, worldPosition.x, worldPosition.z) + 0.02f;
    }

    private Terrain GetTerrainAt(Vector3 worldPosition)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain != null && IsInsideTerrain(terrain, worldPosition))
            {
                return terrain;
            }
        }

        Terrain activeTerrain = Terrain.activeTerrain;
        return activeTerrain != null && IsInsideTerrain(activeTerrain, worldPosition) ? activeTerrain : null;
    }

    private bool IsInsideTerrain(Terrain terrain, Vector3 worldPosition)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        return worldPosition.x >= terrainPosition.x
            && worldPosition.x <= terrainPosition.x + terrainSize.x
            && worldPosition.z >= terrainPosition.z
            && worldPosition.z <= terrainPosition.z + terrainSize.z;
    }

    private float GetTerrainWorldHeight(Terrain terrain, float worldX, float worldZ)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        float normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldX);
        float normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldZ);
        return terrainPosition.y + terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
    }

    private float PlayDigVisual(DetectableTreasure treasure, bool finalReveal)
    {
        Vector3 effectPosition = treasure.transform.position;

        if (treasure.revealMarker != null)
        {
            effectPosition = treasure.revealMarker.transform.position;
        }

        bool hasUpgradedShovel = upgradeShop != null && upgradeShop.shovelUpgraded;
        GameObject shovelPrefab = hasUpgradedShovel && upgradedShovelPrefab != null
            ? upgradedShovelPrefab
            : rustyShovelPrefab;
        Vector3 shovelPosition = hasUpgradedShovel ? cleanShovelLocalPosition : rustyShovelLocalPosition;
        Vector3 shovelEulerAngles = hasUpgradedShovel ? cleanShovelLocalEulerAngles : rustyShovelLocalEulerAngles;
        Vector3 shovelScale = hasUpgradedShovel ? cleanShovelLocalScale : rustyShovelLocalScale;
        return DiggingVisualEffect.Play(effectPosition, finalReveal, treasure.value, shovelPrefab, shovelPosition, shovelEulerAngles, shovelScale);
    }

    private UpgradeShop FindBestShop()
    {
        UpgradeShop[] shops = GetComponents<UpgradeShop>();

        foreach (UpgradeShop candidate in shops)
        {
            if (candidate != null && candidate.shopNpc != null && candidate.CanUpgradeHere)
            {
                return candidate;
            }
        }

        return shops.Length > 0 ? shops[0] : null;
    }

    private void OnGUI()
    {
        if (RuntimeGameUI.IsActive)
        {
            return;
        }

        if (!GameUIState.AnyMenuOpen)
        {
            string prompt = HasSearchableChestInRange
                ? "E - Search"
                : HasDigTargetInRange
                    ? "E - Dig"
                    : "Hold LMB to scan sand.";
            GameGui.DrawToast(new Rect(Screen.width * 0.5f - 190f, Screen.height - 74f, 380f, 42f), prompt);
        }

        if (messageTimer > 0f)
        {
            GameGui.DrawToast(new Rect(Screen.width * 0.5f - 180f, Screen.height - 126f, 360f, 40f), lastFoundMessage);
        }
    }
}
