using UnityEngine;
using UnityEngine.InputSystem;

public class DiggingController : MonoBehaviour
{
    public float digRange = 1.8f;
    public float digCooldownPadding = 0.05f;
    public PlayerInventory playerInventory;
    public UpgradeShop upgradeShop;
    public TreasureMap treasureMap;
    public GameObject rustyShovelPrefab;
    public GameObject upgradedShovelPrefab;
    public Vector3 rustyShovelLocalPosition = new Vector3(0f, 0.12f, 0f);
    public Vector3 rustyShovelLocalEulerAngles = new Vector3(-8f, 0f, 0f);
    public Vector3 rustyShovelLocalScale = Vector3.one * 0.24f;
    public Vector3 cleanShovelLocalPosition = Vector3.zero;
    public Vector3 cleanShovelLocalEulerAngles = new Vector3(-8f, 0f, 0f);
    public Vector3 cleanShovelLocalScale = Vector3.one * 1.16f;

    private string lastFoundMessage = "";
    private float messageTimer;
    private Sprite lastFoundIcon;
    private bool lastFoundWasTreasure;
    private string lastFoundItemName = "";
    private int lastFoundValue;
    private float nextDigAllowedTime;

    public string LastFoundMessage => lastFoundMessage;
    public float MessageTimer => messageTimer;
    public Sprite LastFoundIcon => lastFoundIcon;
    public bool LastFoundWasTreasure => lastFoundWasTreasure;
    public string LastFoundItemName => lastFoundItemName;
    public int LastFoundValue => lastFoundValue;

    public bool HasDigTargetInRange => FindClosestTreasureInRange() != null;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();

        if (playerInventory == null)
        {
            playerInventory = gameObject.AddComponent<PlayerInventory>();
        }

        upgradeShop = FindBestShop();
        treasureMap = GetComponent<TreasureMap>();

        if (treasureMap == null)
        {
            treasureMap = gameObject.AddComponent<TreasureMap>();
        }

        treasureMap.player = transform;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (GameUIState.AnyMenuOpen || (upgradeShop != null && upgradeShop.IsPlayerNearShop()) || PlayerHome.AnyHomeInteractionInRange())
            {
                return;
            }

            TryDig();
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
            messageTimer = 2.4f;
            return;
        }

        DetectableTreasure treasure = FindClosestTreasureInRange();

        if (treasure == null)
        {
            treasureMap.RegisterCheckedSpot(transform.position);
            lastFoundMessage = "No marked target here. Scan with LMB first.";
            lastFoundIcon = null;
            lastFoundWasTreasure = false;
            lastFoundItemName = "";
            lastFoundValue = 0;
            messageTimer = 2f;
            return;
        }

        bool digComplete = treasure.DigOnce();

        if (!digComplete)
        {
            StartDigCooldown(PlayDigVisual(treasure, false));
            lastFoundMessage = "Digging: " + treasure.currentDigHits + "/" + treasure.requiredDigHits;
            lastFoundIcon = treasure.icon;
            lastFoundWasTreasure = false;
            lastFoundItemName = treasure.treasureName;
            lastFoundValue = treasure.value;
            messageTimer = 1.5f;
            return;
        }

        if (!playerInventory.AddTreasure(treasure))
        {
            lastFoundMessage = "Backpack is full. Sell items or buy an upgrade.";
            lastFoundIcon = treasure.icon;
            lastFoundWasTreasure = false;
            lastFoundItemName = treasure.treasureName;
            lastFoundValue = treasure.value;
            messageTimer = 3f;
            return;
        }

        treasure.isFound = true;
        treasureMap.RegisterFoundTreasure(treasure.transform.position);
        StartDigCooldown(PlayDigVisual(treasure, true));
        ReplaceRevealMarkerWithDugSpot(treasure);
        lastFoundMessage = "Found: " + treasure.treasureName + " ($" + treasure.value + ")!";
        lastFoundIcon = treasure.icon;
        lastFoundWasTreasure = true;
        lastFoundItemName = treasure.treasureName;
        lastFoundValue = treasure.value;
        messageTimer = GetFoundMessageDuration(treasure.value);
        GameEvents.ReportTreasureFound(treasure);

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

    public DetectableTreasure FindClosestTreasureInRange()
    {
        if (DayNightCycle.IsNightNow)
        {
            return null;
        }

        DetectableTreasure[] treasures = FindObjectsByType<DetectableTreasure>();
        DetectableTreasure closestTreasure = null;
        float closestDistance = digRange;

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

            float distance = GetGroundDistance(transform.position, treasure.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTreasure = treasure;
            }
        }

        return closestTreasure;
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

    private void ReplaceRevealMarkerWithDugSpot(DetectableTreasure treasure)
    {
        Vector3 markerPosition = treasure.transform.position;

        if (treasure.revealMarker != null)
        {
            markerPosition = treasure.revealMarker.transform.position;
            Destroy(treasure.revealMarker.gameObject);
        }

        GameObject dugSpotObject = new GameObject("Dug Ground");
        dugSpotObject.transform.position = new Vector3(markerPosition.x, GetSurfaceY(markerPosition), markerPosition.z);

        GameObject darkCenter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        darkCenter.name = "Dug Hole Dark Center";
        darkCenter.transform.SetParent(dugSpotObject.transform, false);
        darkCenter.transform.localPosition = new Vector3(0f, 0.012f, 0f);
        darkCenter.transform.localScale = new Vector3(0.78f, 0.026f, 0.66f);
        AssignDugSpotMaterial(darkCenter, new Color(0.13f, 0.075f, 0.035f, 0.92f));
        DisableCollider(darkCenter);

        GameObject innerShadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        innerShadow.name = "Dug Hole Inner Shadow";
        innerShadow.transform.SetParent(dugSpotObject.transform, false);
        innerShadow.transform.localPosition = new Vector3(0f, 0.028f, 0f);
        innerShadow.transform.localScale = new Vector3(0.52f, 0.018f, 0.42f);
        AssignDugSpotMaterial(innerShadow, new Color(0.045f, 0.028f, 0.018f, 0.95f));
        DisableCollider(innerShadow);

        CreateDugSandRim(dugSpotObject.transform);

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
            if (candidate != null && candidate.shopNpc != null)
            {
                return candidate;
            }
        }

        return shops.Length > 0 ? shops[0] : null;
    }

    private void CreateDugSandRim(Transform parent)
    {
        Color rimColor = new Color(0.63f, 0.48f, 0.25f, 1f);
        Color highlightColor = new Color(0.82f, 0.68f, 0.38f, 1f);
        int count = 14;

        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f;
            float wobble = i % 2 == 0 ? 1.04f : 0.92f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * 0.58f * wobble, 0.052f, Mathf.Sin(angle) * 0.47f * wobble);

            GameObject rimChunk = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere);
            rimChunk.name = "Dug Sand Rim";
            rimChunk.transform.SetParent(parent, false);
            rimChunk.transform.localPosition = position;
            rimChunk.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            rimChunk.transform.localScale = new Vector3(0.18f + (i % 4) * 0.018f, 0.045f, 0.12f + (i % 5) * 0.012f);
            AssignDugSpotMaterial(rimChunk, i % 4 == 0 ? highlightColor : rimColor);
            DisableCollider(rimChunk);
        }

        for (int i = 0; i < 5; i++)
        {
            float angle = (i * 1.37f + 0.4f) * Mathf.PI;
            GameObject looseChunk = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            looseChunk.name = "Loose Dug Sand";
            looseChunk.transform.SetParent(parent, false);
            looseChunk.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.86f, 0.055f, Mathf.Sin(angle) * 0.72f);
            looseChunk.transform.localScale = Vector3.one * (0.055f + i * 0.007f);
            AssignDugSpotMaterial(looseChunk, rimColor);
            DisableCollider(looseChunk);
        }
    }

    private void AssignDugSpotMaterial(GameObject target, Color color)
    {
        Renderer targetRenderer = target.GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            targetRenderer.material = CreateDugSpotMaterial(color);
        }
    }

    private Material CreateDugSpotMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
    }

    private void DisableCollider(GameObject target)
    {
        Collider targetCollider = target.GetComponent<Collider>();

        if (targetCollider != null)
        {
            targetCollider.enabled = false;
        }
    }

    private void OnGUI()
    {
        if (RuntimeGameUI.IsActive)
        {
            return;
        }

        if (!GameUIState.AnyMenuOpen)
        {
            string prompt = HasDigTargetInRange
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
