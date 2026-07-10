using UnityEngine;
using UnityEngine.InputSystem;

public class GroundScanner : MonoBehaviour
{
    private const float SearchAreaCacheRefreshInterval = 0.5f;
    private const float TreasureCacheRefreshInterval = 0.35f;
    private static readonly bool ScanPreviewEnabled = false;

    public Transform scanOrigin;
    public float scanRadius = 0.14f;
    public float scanDistanceForward = 0f;
    public float scanInterval = 0.04f;
    public float markerYPosition = 0.024f;
    public bool revealTreasures = true;
    public bool alignMarkersToTerrain = true;
    public float terrainMarkerYOffset = 0.04f;
    public bool requireUnlockedSearchArea = true;
    public bool showScanCircle = true;
    public Color scanCircleColor = new Color(0.25f, 0.88f, 1f, 0.18f);

    private float scanTimer;
    private int scanCount;
    private MetalDetector metalDetector;
    private GameObject scanPreviewCircle;
    private Renderer scanPreviewRenderer;
    private SearchArea[] cachedSearchAreas = new SearchArea[0];
    private DetectableTreasure[] cachedTreasures = new DetectableTreasure[0];
    private float nextSearchAreaCacheRefreshTime = -1f;
    private float nextTreasureCacheRefreshTime = -1f;

    private void Awake()
    {
        metalDetector = GetComponent<MetalDetector>();
        ResolveScanOrigin();

        if (ScanPreviewEnabled)
        {
            EnsureScanPreviewCircle();
        }
    }

    private void OnDestroy()
    {
        if (scanPreviewCircle != null)
        {
            Destroy(scanPreviewCircle);
        }
    }

    private void Update()
    {
        ResolveScanOrigin();

        if (GameUIState.AnyBlockingUIOpen || Mouse.current == null || !Mouse.current.leftButton.isPressed)
        {
            scanTimer = 0f;
            SetScanPreviewVisible(false);
            return;
        }

        Vector3 scanPosition = GetScanPosition();

        if (DayNightCycle.IsNightNow || !CanScanAt(scanPosition))
        {
            scanTimer = 0f;
            SetScanPreviewVisible(false);
            return;
        }

        float currentScanRadius = GetCurrentScanRadius();
        UpdateScanPreviewCircle(scanPosition, currentScanRadius);

        scanTimer -= Time.deltaTime;

        if (scanTimer > 0f)
        {
            return;
        }

        scanTimer = scanInterval;
        scanCount++;
        GameEvents.ReportGroundScans(scanCount);
        RevealTreasuresInScan(scanPosition, currentScanRadius);
    }

    public void ClearScannedArea()
    {
        scanCount = 0;
        scanTimer = 0f;
        SetScanPreviewVisible(false);
    }

    public Vector3 GetCurrentScanPosition()
    {
        return GetScanPosition();
    }

    public float GetCurrentScanRadius()
    {
        return metalDetector != null ? metalDetector.CurrentScanRadius : Mathf.Max(0.05f, scanRadius);
    }

    public bool CanSignalAtCurrentScan()
    {
        return CanScanAt(GetScanPosition());
    }

    public bool CanScanAtWorldPosition(Vector3 worldPosition)
    {
        return CanScanAt(worldPosition);
    }

    public float GetDistanceFromCurrentScan(Vector3 worldPosition)
    {
        return GetGroundDistance(GetScanPosition(), worldPosition);
    }

    private Vector3 GetScanPosition()
    {
        Transform origin = GetEffectiveScanOrigin();
        Vector3 forward = new Vector3(origin.forward.x, 0f, origin.forward.z).normalized;

        if (forward.sqrMagnitude < 0.01f)
        {
            forward = transform.forward;
        }

        Vector3 scanPosition = origin.position + forward * scanDistanceForward;
        return new Vector3(scanPosition.x, markerYPosition, scanPosition.z);
    }

    private Transform GetEffectiveScanOrigin()
    {
        ResolveScanOrigin();
        return scanOrigin != null ? scanOrigin : transform;
    }

    private void ResolveScanOrigin()
    {
        if (metalDetector == null)
        {
            metalDetector = GetComponent<MetalDetector>();
        }

        if (metalDetector == null || metalDetector.detectorHead == null)
        {
            return;
        }

        if (scanOrigin == null || scanOrigin == transform)
        {
            scanOrigin = metalDetector.detectorHead;
        }
    }

    private bool CanScanAt(Vector3 worldPosition)
    {
        if (DayNightCycle.IsNightNow)
        {
            return false;
        }

        if (!requireUnlockedSearchArea)
        {
            return true;
        }

        SearchArea searchArea = FindSearchAreaAt(worldPosition);

        if (searchArea == null)
        {
            return true;
        }

        return searchArea.isUnlocked;
    }

    private SearchArea FindSearchAreaAt(Vector3 worldPosition)
    {
        SearchArea[] searchAreas = GetSearchAreas();

        foreach (SearchArea searchArea in searchAreas)
        {
            if (searchArea != null && searchArea.Contains(worldPosition))
            {
                return searchArea;
            }
        }

        return null;
    }

    private SearchArea[] GetSearchAreas()
    {
        if (cachedSearchAreas == null || Time.unscaledTime >= nextSearchAreaCacheRefreshTime)
        {
            cachedSearchAreas = FindObjectsByType<SearchArea>();
            nextSearchAreaCacheRefreshTime = Time.unscaledTime + SearchAreaCacheRefreshInterval;
        }

        return cachedSearchAreas;
    }

    private void RevealTreasuresInScan(Vector3 scanPosition, float radius)
    {
        if (!revealTreasures)
        {
            return;
        }

        DetectableTreasure[] treasures = GetDetectableTreasures();

        foreach (DetectableTreasure treasure in treasures)
        {
            if (treasure.isFound || treasure.isRevealed)
            {
                continue;
            }

            if (GetGroundDistanceSqr(scanPosition, treasure.transform.position) > radius * radius)
            {
                continue;
            }

            SearchMarker marker = CreateTreasureRevealMarker(treasure);
            treasure.Reveal(marker);
            LocalCoopManager.Instance?.ReportTreasureRevealed(treasure);

            if (metalDetector != null)
            {
                metalDetector.ReportTreasureRevealed(treasure);
            }
        }
    }

    private DetectableTreasure[] GetDetectableTreasures()
    {
        if (cachedTreasures == null || Time.unscaledTime >= nextTreasureCacheRefreshTime)
        {
            cachedTreasures = FindObjectsByType<DetectableTreasure>();
            nextTreasureCacheRefreshTime = Time.unscaledTime + TreasureCacheRefreshInterval;
        }

        return cachedTreasures;
    }

    private SearchMarker CreateTreasureRevealMarker(DetectableTreasure treasure)
    {
        Vector3 treasurePosition = treasure.transform.position;
        float markerY = GetMarkerYAt(treasurePosition, markerYPosition) + 0.02f;

        GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        markerObject.name = "Dig Target Marker";
        markerObject.transform.position = new Vector3(treasurePosition.x, markerY, treasurePosition.z);
        float markerRadius = Mathf.Max(0.12f, GetCurrentScanRadius() * 0.35f);
        markerObject.transform.localScale = new Vector3(markerRadius * 2f, 0.004f, markerRadius * 2f);

        SearchMarker marker = markerObject.AddComponent<SearchMarker>();
        marker.markerType = SearchMarker.MarkerType.FoundTreasure;
        marker.treasureRarity = treasure.rarity;
        marker.pulseAmount = 0.08f;
        return marker;
    }

    private void EnsureScanPreviewCircle()
    {
        if (scanPreviewCircle != null)
        {
            return;
        }

        scanPreviewCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        scanPreviewCircle.name = "Detector Scan Circle";
        scanPreviewCircle.SetActive(false);

        Collider scanCollider = scanPreviewCircle.GetComponent<Collider>();

        if (scanCollider != null)
        {
            scanCollider.enabled = false;
        }

        scanPreviewRenderer = scanPreviewCircle.GetComponent<Renderer>();

        if (scanPreviewRenderer != null)
        {
            scanPreviewRenderer.material = CreateTransparentMaterial(scanCircleColor);
            scanPreviewRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            scanPreviewRenderer.receiveShadows = false;
        }
    }

    private void UpdateScanPreviewCircle(Vector3 scanPosition, float radius)
    {
        if (!ScanPreviewEnabled || !showScanCircle)
        {
            SetScanPreviewVisible(false);
            return;
        }

        EnsureScanPreviewCircle();

        if (scanPreviewCircle == null)
        {
            return;
        }

        float scanY = GetMarkerYAt(scanPosition, markerYPosition) + 0.018f;
        scanPreviewCircle.transform.position = new Vector3(scanPosition.x, scanY, scanPosition.z);
        scanPreviewCircle.transform.rotation = Quaternion.identity;
        scanPreviewCircle.transform.localScale = new Vector3(radius * 2f, 0.0025f, radius * 2f);
        SetScanPreviewVisible(true);
    }

    private void SetScanPreviewVisible(bool isVisible)
    {
        if (scanPreviewCircle != null && scanPreviewCircle.activeSelf != isVisible)
        {
            scanPreviewCircle.SetActive(isVisible);
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

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
        }

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;
        return material;
    }

    private float GetMarkerYAt(Vector3 worldPosition, float fallbackY)
    {
        if (!alignMarkersToTerrain)
        {
            return fallbackY;
        }

        Terrain terrain = GetTerrainAt(worldPosition);

        if (terrain == null)
        {
            return fallbackY;
        }

        return GetTerrainWorldHeight(terrain, worldPosition.x, worldPosition.z) + terrainMarkerYOffset;
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

    private float GetGroundDistance(Vector3 first, Vector3 second)
    {
        Vector2 firstGround = new Vector2(first.x, first.z);
        Vector2 secondGround = new Vector2(second.x, second.z);
        return Vector2.Distance(firstGround, secondGround);
    }

    private float GetGroundDistanceSqr(Vector3 first, Vector3 second)
    {
        Vector2 firstGround = new Vector2(first.x, first.z);
        Vector2 secondGround = new Vector2(second.x, second.z);
        return (firstGround - secondGround).sqrMagnitude;
    }
}
