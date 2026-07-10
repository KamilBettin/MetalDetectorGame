using UnityEngine;

public class TreasureSpawner : MonoBehaviour
{
    [System.Serializable]
    public class TreasureOption
    {
        public string treasureName;
        public int value;
    }

    public int treasureCount = 80;
    public int treasuresPerUnlockedArea = 120;
    public int generalTerrainTreasureCount = 180;
    public bool scaleTreasureCountToFindableItems = true;
    public int minimumGeneralTerrainTreasureCount = 180;
    public int generalTerrainTreasureMultiplier = 5;
    public bool focusGeneralTerrainAroundPlayer = true;
    public float generalTerrainSpawnRadius = 460f;
    public bool requireGeneralTerrainOnDryLand = true;
    public float maxGeneralTerrainWaterDepth = 0.35f;
    public int maxGeneralSpawnAttemptsPerTreasure = 24;
    public int deterministicSeed = 420777;
    public Vector2 mapSize = new Vector2(1000f, 1000f);
    public float buriedYPosition = -0.25f;
    public bool showDebugTreasures;
    public bool showSearchAreaBounds = true;
    public float searchAreaMarkerYPosition = 0.025f;
    public float searchAreaLineThickness = 0.35f;
    public float searchAreaCornerSize = 1.8f;
    public bool snapTreasuresToTerrain = true;
    public float buriedDepth = 0.22f;
    public TreasureLootPool generalTerrainLootPool = TreasureLootPool.Default;

    private GameObject searchAreaMarkerParent;
    private Material searchAreaLineMaterial;
    private Material searchAreaCornerMaterial;
    private int nextTreasureId;
    private MetalDetector metalDetector;
    private OceanWaterSurface waterSurface;

    public TreasureDatabase treasureDatabase;

    public TreasureOption[] treasureOptions =
    {
        new TreasureOption { treasureName = "Rusty Bottle Cap", value = 3 },
        new TreasureOption { treasureName = "Old Nail", value = 3 },
        new TreasureOption { treasureName = "Small Coin", value = 8 },
        new TreasureOption { treasureName = "Silver Ring", value = 60 },
        new TreasureOption { treasureName = "Gold Ring", value = 210 }
    };

    private void Start()
    {
        DefaultSearchAreaBootstrapper.EnsureDefaultSearchAreas();

        if (treasureDatabase == null)
        {
            treasureDatabase = TreasureDatabase.CreateRuntimeDefault();
        }

        CreateSearchAreaMarker();
        SpawnTreasures();
    }

    public void SpawnTreasuresForArea(SearchArea area)
    {
        if (area == null || !area.CanSpawnTreasures())
        {
            return;
        }

        EnsureTreasureDatabase();
        SpawnTreasuresInArea(area, GetTreasureCountForArea(area));
        area.MarkTreasuresSpawned();
    }

    public void ClearSpawnedTreasures()
    {
        DetectableTreasure[] treasures = GetComponentsInChildren<DetectableTreasure>();

        foreach (DetectableTreasure treasure in treasures)
        {
            if (treasure == null)
            {
                continue;
            }

            if (treasure.revealMarker != null)
            {
                Destroy(treasure.revealMarker.gameObject);
            }

            Destroy(treasure.gameObject);
        }
    }

    public void RegenerateTreasures()
    {
        RegenerateTreasures(0);
    }

    public void RegenerateTreasures(int daySeedOffset)
    {
        ClearSpawnedTreasures();

        SearchArea[] searchAreas = FindObjectsByType<SearchArea>();

        foreach (SearchArea searchArea in searchAreas)
        {
            if (searchArea != null)
            {
                searchArea.ClearTreasureSpawnState();
            }
        }

        SpawnTreasures(daySeedOffset);
    }

    private void CreateSearchAreaMarker()
    {
        if (!showSearchAreaBounds)
        {
            return;
        }

        searchAreaMarkerParent = new GameObject("Search Area Bounds");
        searchAreaMarkerParent.transform.SetParent(transform);
        searchAreaLineMaterial = CreateTransparentMaterial(new Color(1f, 0.78f, 0.28f, 0.42f));
        searchAreaCornerMaterial = CreateTransparentMaterial(new Color(1f, 0.9f, 0.46f, 0.62f));

        float halfWidth = mapSize.x * 0.5f;
        float halfDepth = mapSize.y * 0.5f;

        CreateBoundsLine("Search Area North Edge", new Vector3(0f, searchAreaMarkerYPosition, halfDepth), new Vector3(mapSize.x, 0.018f, searchAreaLineThickness));
        CreateBoundsLine("Search Area South Edge", new Vector3(0f, searchAreaMarkerYPosition, -halfDepth), new Vector3(mapSize.x, 0.018f, searchAreaLineThickness));
        CreateBoundsLine("Search Area East Edge", new Vector3(halfWidth, searchAreaMarkerYPosition, 0f), new Vector3(searchAreaLineThickness, 0.018f, mapSize.y));
        CreateBoundsLine("Search Area West Edge", new Vector3(-halfWidth, searchAreaMarkerYPosition, 0f), new Vector3(searchAreaLineThickness, 0.018f, mapSize.y));

        CreateCornerMarker(new Vector3(-halfWidth, searchAreaMarkerYPosition + 0.018f, -halfDepth));
        CreateCornerMarker(new Vector3(-halfWidth, searchAreaMarkerYPosition + 0.018f, halfDepth));
        CreateCornerMarker(new Vector3(halfWidth, searchAreaMarkerYPosition + 0.018f, -halfDepth));
        CreateCornerMarker(new Vector3(halfWidth, searchAreaMarkerYPosition + 0.018f, halfDepth));
    }

    private void CreateBoundsLine(string lineName, Vector3 position, Vector3 scale)
    {
        GameObject lineObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lineObject.name = lineName;
        lineObject.transform.SetParent(searchAreaMarkerParent.transform);
        lineObject.transform.position = position;
        lineObject.transform.localScale = scale;

        DisableCollider(lineObject);

        Renderer lineRenderer = lineObject.GetComponent<Renderer>();

        if (lineRenderer != null)
        {
            lineRenderer.material = searchAreaLineMaterial;
        }
    }

    private void CreateCornerMarker(Vector3 position)
    {
        GameObject cornerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cornerObject.name = "Search Area Corner Marker";
        cornerObject.transform.SetParent(searchAreaMarkerParent.transform);
        cornerObject.transform.position = position;
        cornerObject.transform.localScale = new Vector3(searchAreaCornerSize, 0.035f, searchAreaCornerSize);

        DisableCollider(cornerObject);

        Renderer cornerRenderer = cornerObject.GetComponent<Renderer>();

        if (cornerRenderer != null)
        {
            cornerRenderer.material = searchAreaCornerMaterial;
        }
    }

    private void SpawnTreasures()
    {
        SpawnTreasures(0);
    }

    private void SpawnTreasures(int seedOffset)
    {
        EnsureTreasureDatabase();

        if ((treasureDatabase == null || !treasureDatabase.HasAnyTreasures())
            && (treasureOptions == null || treasureOptions.Length == 0))
        {
            Debug.LogWarning("TreasureSpawner has no treasure options.");
            return;
        }

        Random.State previousRandomState = Random.state;
        Random.InitState(deterministicSeed + seedOffset * 9973);
        nextTreasureId = 0;

        try
        {
            SearchArea[] searchAreas = FindObjectsByType<SearchArea>();
            System.Array.Sort(searchAreas, CompareSearchAreas);
            SpawnGeneralTerrainTreasures(searchAreas);

            if (searchAreas.Length > 0)
            {
                foreach (SearchArea area in searchAreas)
                {
                    SpawnTreasuresForArea(area);
                }

                return;
            }
        }
        finally
        {
            Random.state = previousRandomState;
        }
    }

    private void EnsureTreasureDatabase()
    {
        if (treasureDatabase == null)
        {
            treasureDatabase = TreasureDatabase.CreateRuntimeDefault();
        }
    }

    private int CompareSearchAreas(SearchArea first, SearchArea second)
    {
        if (first == second)
        {
            return 0;
        }

        if (first == null)
        {
            return -1;
        }

        if (second == null)
        {
            return 1;
        }

        int nameCompare = string.CompareOrdinal(first.areaName, second.areaName);

        if (nameCompare != 0)
        {
            return nameCompare;
        }

        int xCompare = first.transform.position.x.CompareTo(second.transform.position.x);
        return xCompare != 0 ? xCompare : first.transform.position.z.CompareTo(second.transform.position.z);
    }

    private void SpawnTreasuresInArea(SearchArea area, int count)
    {
        TreasureLootPool areaLootPool = area != null ? area.lootPool : TreasureLootPool.SpecialField;
        int detectorTier = GetCurrentDetectorTier();

        for (int i = 0; i < count; i++)
        {
            SpawnTreasureAt(area.GetRandomPoint(buriedYPosition), areaLootPool, detectorTier);
        }
    }

    private void SpawnGeneralTerrainTreasures(SearchArea[] searchAreas)
    {
        int targetCount = GetGeneralTerrainTreasureCount();
        int maxAttempts = Mathf.Max(requireGeneralTerrainOnDryLand || focusGeneralTerrainAroundPlayer ? 64 : 1, maxGeneralSpawnAttemptsPerTreasure);

        for (int i = 0; i < targetCount; i++)
        {
            bool spawned = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector3 position = GetRandomGeneralTerrainPosition();

                if (IsInsideAnySearchArea(position, searchAreas))
                {
                    continue;
                }

                if (!IsValidGeneralTerrainSpawnPosition(position))
                {
                    continue;
                }

                SpawnTreasureAt(position, generalTerrainLootPool);
                spawned = true;
                break;
            }

            if (!spawned && (searchAreas == null || searchAreas.Length == 0))
            {
                Vector3 fallbackPosition = GetRandomGeneralTerrainPosition();

                if (IsValidGeneralTerrainSpawnPosition(fallbackPosition))
                {
                    SpawnTreasureAt(fallbackPosition, generalTerrainLootPool);
                }
            }
        }
    }

    private Vector3 GetRandomGeneralTerrainPosition()
    {
        if (focusGeneralTerrainAroundPlayer && TryGetGeneralTerrainSpawnCenter(out Vector3 center))
        {
            Vector2 offset = Random.insideUnitCircle * Mathf.Max(1f, generalTerrainSpawnRadius);
            return new Vector3(center.x + offset.x, buriedYPosition, center.z + offset.y);
        }

        return GetRandomLegacyPosition();
    }

    private Vector3 GetRandomLegacyPosition()
    {
        return new Vector3(
            Random.Range(-mapSize.x * 0.5f, mapSize.x * 0.5f),
            buriedYPosition,
            Random.Range(-mapSize.y * 0.5f, mapSize.y * 0.5f)
        );
    }

    private bool IsInsideAnySearchArea(Vector3 position, SearchArea[] searchAreas)
    {
        if (searchAreas == null)
        {
            return false;
        }

        foreach (SearchArea searchArea in searchAreas)
        {
            if (searchArea != null && searchArea.Contains(position))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValidGeneralTerrainSpawnPosition(Vector3 position)
    {
        if (!requireGeneralTerrainOnDryLand)
        {
            return true;
        }

        Terrain terrain = GetTerrainAt(position);

        if (terrain == null)
        {
            return false;
        }

        float groundHeight = GetTerrainWorldHeight(terrain, position.x, position.z);
        float waterDepth = Mathf.Max(0f, GetCurrentWaterLevel() - groundHeight);
        return waterDepth <= Mathf.Max(0f, maxGeneralTerrainWaterDepth);
    }

    private void SpawnTreasureAt(Vector3 position, TreasureLootPool lootPool)
    {
        SpawnTreasureAt(position, lootPool, null);
    }

    private void SpawnTreasureAt(Vector3 position, TreasureLootPool lootPool, int? detectorTier)
    {
        position = GetBuriedPosition(position);

        TreasureDefinition definition = null;

        if (treasureDatabase != null)
        {
            definition = detectorTier.HasValue
                ? treasureDatabase.GetRandomTreasureForSearchArea(lootPool, detectorTier.Value)
                : treasureDatabase.GetRandomTreasure(lootPool);
        }

        TreasureOption option = definition == null && treasureOptions != null && treasureOptions.Length > 0
            ? treasureOptions[Random.Range(0, treasureOptions.Length)]
            : null;

        if (definition == null && option == null)
        {
            return;
        }

        GameObject treasureObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        string treasureName = definition != null ? definition.treasureName : option.treasureName;
        int treasureValue = definition != null ? definition.value : option.value;
        TreasureRarity rarity = definition != null ? definition.rarity : TreasureRarity.Common;
        Sprite icon = definition != null ? definition.icon : null;
        int inventoryWidth = definition != null ? definition.width : 1;
        int inventoryHeight = definition != null ? definition.height : 1;

        treasureObject.name = "Treasure - " + treasureName;
        treasureObject.transform.SetParent(transform);
        treasureObject.transform.position = position;
        treasureObject.transform.localScale = Vector3.one * 0.25f;

        DetectableTreasure treasure = treasureObject.AddComponent<DetectableTreasure>();
        treasure.multiplayerId = "treasure_" + nextTreasureId.ToString();
        nextTreasureId++;
        treasure.treasureName = treasureName;
        treasure.value = treasureValue;
        treasure.rarity = rarity;
        treasure.icon = icon;
        treasure.inventoryWidth = Mathf.Max(1, inventoryWidth);
        treasure.inventoryHeight = Mathf.Max(1, inventoryHeight);
        treasure.requiredDigHits = definition != null
            ? Random.Range(definition.minDigHits, definition.maxDigHits + 1)
            : Random.Range(3, 5);

        Renderer treasureRenderer = treasureObject.GetComponent<Renderer>();

        if (treasureRenderer != null)
        {
            treasureRenderer.enabled = showDebugTreasures;
        }
    }

    private int GetCurrentDetectorTier()
    {
        if (metalDetector == null)
        {
            metalDetector = FindAnyObjectByType<MetalDetector>();
        }

        return metalDetector != null ? metalDetector.DetectorTier : 0;
    }

    private int GetTreasureCountForArea(SearchArea area)
    {
        if (!scaleTreasureCountToFindableItems || treasureDatabase == null)
        {
            return Mathf.Max(0, treasuresPerUnlockedArea);
        }

        TreasureLootPool areaLootPool = area != null ? area.lootPool : TreasureLootPool.SpecialField;
        int findableCount = treasureDatabase.GetFindableTreasureCountForSearchArea(areaLootPool, GetCurrentDetectorTier());
        return findableCount > 0 ? findableCount : Mathf.Max(0, treasuresPerUnlockedArea);
    }

    private int GetGeneralTerrainTreasureCount()
    {
        if (!scaleTreasureCountToFindableItems || treasureDatabase == null)
        {
            return Mathf.Max(0, generalTerrainTreasureCount > 0 ? generalTerrainTreasureCount : treasureCount);
        }

        int findableCount = treasureDatabase.GetFindableTreasureCount(generalTerrainLootPool);
        int scaledCount = findableCount * Mathf.Max(1, generalTerrainTreasureMultiplier);
        int minimumCount = Mathf.Max(0, minimumGeneralTerrainTreasureCount);
        int fallbackCount = Mathf.Max(0, generalTerrainTreasureCount > 0 ? generalTerrainTreasureCount : treasureCount);
        return findableCount > 0 ? Mathf.Max(minimumCount, scaledCount) : fallbackCount;
    }

    private bool TryGetGeneralTerrainSpawnCenter(out Vector3 center)
    {
        Transform playerTransform = PlayerRigReferences.FindLocalPlayerTransform();

        if (playerTransform != null)
        {
            center = playerTransform.position;
            return true;
        }

        center = Vector3.zero;
        return false;
    }

    private float GetCurrentWaterLevel()
    {
        if (waterSurface == null)
        {
            waterSurface = FindAnyObjectByType<OceanWaterSurface>();
        }

        return waterSurface != null ? waterSurface.waterLevel : 0f;
    }

    private Vector3 GetBuriedPosition(Vector3 position)
    {
        if (!snapTreasuresToTerrain)
        {
            return position;
        }

        Terrain terrain = GetTerrainAt(position);

        if (terrain == null)
        {
            return position;
        }

        position.y = GetTerrainWorldHeight(terrain, position.x, position.z) - buriedDepth;
        return position;
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

    private void DisableCollider(GameObject markerObject)
    {
        Collider markerCollider = markerObject.GetComponent<Collider>();

        if (markerCollider != null)
        {
            markerCollider.enabled = false;
        }
    }

    private Material CreateTransparentMaterial(Color color)
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

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", 0f);
        }

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;

        return material;
    }
}
