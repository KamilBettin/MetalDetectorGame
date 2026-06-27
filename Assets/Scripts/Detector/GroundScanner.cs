using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GroundScanner : MonoBehaviour
{
    public Transform scanOrigin;
    public float scanRadius = 0.16f;
    public float scanDistanceForward = 0f;
    public float scanInterval = 0.04f;
    public float gridCellSize = 0.16f;
    public int scanCellsWide = 2;
    public int scanCellsDeep = 2;
    public float markerYPosition = 0.024f;
    public float gridLineThickness = 0.012f;
    public Vector2 fullGridWorldSize = new Vector2(100f, 100f);
    public float scannedMarksClearDelay = 1f;
    public bool showCellFill = true;
    public bool revealTreasures = true;
    public bool alignMarkersToTerrain = true;
    public float terrainMarkerYOffset = 0.04f;
    public bool requireUnlockedSearchArea = true;
    public DetectorBattery detectorBattery;

    private float scanTimer;
    private float timeSinceLastScan;
    private bool wasScanningLastFrame;
    private bool hasPreviousScannedCell;
    private Vector2Int previousScannedCell;
    private GameObject markerParent;
    private GameObject previewGridParent;
    private Vector2Int previewGridMinCell;
    private Vector2Int previewGridCellCount;
    private Material permanentScannedCellMaterial;
    private Material scannedCellFillMaterial;
    private Material previewGridMaterial;
    private readonly HashSet<Vector2Int> scannedCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> visibleScannedCells = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> currentScanCells = new HashSet<Vector2Int>();
    private readonly List<GameObject> temporaryScannedCellMarkers = new List<GameObject>();
    private MetalDetector metalDetector;

    private void Awake()
    {
        markerParent = new GameObject("Scan Markers");
        previewGridParent = new GameObject("Scan Preview Grid");
        permanentScannedCellMaterial = CreateTransparentMaterial(new Color(0.95f, 0.86f, 0.58f, 0.045f));
        scannedCellFillMaterial = CreateTransparentMaterial(new Color(1f, 0.92f, 0.62f, 0.105f));
        previewGridMaterial = CreateTransparentMaterial(new Color(0.82f, 0.96f, 1f, 0.09f));
        CreatePreviewGrid();
        SetPreviewGridVisible(false);

        if (detectorBattery == null)
        {
            detectorBattery = GetComponent<DetectorBattery>();
        }

        if (detectorBattery == null)
        {
            detectorBattery = gameObject.AddComponent<DetectorBattery>();
        }

        metalDetector = GetComponent<MetalDetector>();
    }

    private void Update()
    {
        if (GameUIState.AnyMenuOpen || Mouse.current == null || !Mouse.current.leftButton.isPressed)
        {
            wasScanningLastFrame = false;
            TickScannedMarkerClear();
            detectorBattery.Recharge(Time.deltaTime);
            return;
        }

        Vector3 scanPosition = GetScanPosition();

        if (DayNightCycle.IsNightNow)
        {
            BlockScan();
            detectorBattery.Recharge(Time.deltaTime);
            return;
        }

        if (!CanScanAt(scanPosition))
        {
            BlockScan();
            detectorBattery.Recharge(Time.deltaTime);
            return;
        }

        if (!detectorBattery.ConsumeForScan(Time.deltaTime))
        {
            BlockScan();
            detectorBattery.Recharge(Time.deltaTime * 0.35f);
            return;
        }

        if (!wasScanningLastFrame)
        {
            hasPreviousScannedCell = false;
        }

        wasScanningLastFrame = true;
        timeSinceLastScan = 0f;
        SetPreviewGridVisible(true);

        scanTimer -= Time.deltaTime;

        if (scanTimer > 0f)
        {
            return;
        }

        scanTimer = scanInterval;
        MarkScannedSpot(scanPosition);
        RevealTreasuresInScan();
    }

    private Vector3 GetScanPosition()
    {
        Transform origin = scanOrigin != null ? scanOrigin : transform;
        Vector3 forward = new Vector3(origin.forward.x, 0f, origin.forward.z).normalized;

        if (forward.sqrMagnitude < 0.01f)
        {
            forward = transform.forward;
        }

        Vector3 scanPosition = origin.position + forward * scanDistanceForward;
        return new Vector3(scanPosition.x, markerYPosition, scanPosition.z);
    }

    private void MarkScannedSpot(Vector3 scanPosition)
    {
        currentScanCells.Clear();
        Vector2Int centerCell = WorldToCell(scanPosition);
        int width = GetCurrentScanCellsWide();
        int depth = GetCurrentScanCellsDeep();
        int minX = centerCell.x - width / 2;
        int minY = centerCell.y - depth / 2;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < depth; y++)
            {
                MarkCurrentScanCell(new Vector2Int(minX + x, minY + y));
            }
        }

        if (hasPreviousScannedCell)
        {
            FillCellsBetween(previousScannedCell, centerCell, width, depth);
        }

        previousScannedCell = centerCell;
        hasPreviousScannedCell = true;

        GameEvents.ReportScannedCells(scannedCells.Count);
    }

    private void MarkCurrentScanCell(Vector2Int cell)
    {
        if (!CanScanCell(cell))
        {
            return;
        }

        currentScanCells.Add(cell);
        MarkScannedCell(cell);
    }

    private void MarkScannedCell(Vector2Int cell)
    {
        bool alreadyScanned = scannedCells.Contains(cell);

        if (!alreadyScanned)
        {
            scannedCells.Add(cell);
            CreateScannedCellMarker(CellToWorld(cell), permanentScannedCellMaterial, "Scanned Grid Cell Permanent", markerYPosition, false);
        }

        if (visibleScannedCells.Contains(cell))
        {
            return;
        }

        visibleScannedCells.Add(cell);
        CreateScannedCellMarker(CellToWorld(cell), scannedCellFillMaterial, "Scanned Grid Cell Fresh", markerYPosition + 0.003f, true);
    }

    private void FillCellsBetween(Vector2Int fromCell, Vector2Int toCell, int width, int depth)
    {
        int steps = Mathf.Max(Mathf.Abs(toCell.x - fromCell.x), Mathf.Abs(toCell.y - fromCell.y));

        if (steps <= 1)
        {
            return;
        }

        for (int i = 1; i < steps; i++)
        {
            float t = i / (float)steps;
            Vector2 interpolated = Vector2.Lerp(fromCell, toCell, t);
            Vector2Int centerCell = new Vector2Int(Mathf.RoundToInt(interpolated.x), Mathf.RoundToInt(interpolated.y));
            int minX = centerCell.x - width / 2;
            int minY = centerCell.y - depth / 2;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < depth; y++)
                {
                    MarkCurrentScanCell(new Vector2Int(minX + x, minY + y));
                }
            }
        }
    }

    private void CreateScannedCellMarker(Vector3 position, Material material, string markerName, float yPosition, bool isTemporary)
    {
        if (!showCellFill)
        {
            return;
        }

        GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        markerObject.name = markerName;
        markerObject.transform.SetParent(markerParent.transform);
        Vector2Int cell = WorldToCell(position);
        Vector3 cellCenter = CellToWorld(cell);
        float markerY = GetMarkerYAt(cellCenter, yPosition);
        markerObject.transform.position = new Vector3(cellCenter.x, markerY, cellCenter.z);
        markerObject.transform.localScale = new Vector3(gridCellSize * 0.92f, 0.003f, gridCellSize * 0.92f);

        Collider markerCollider = markerObject.GetComponent<Collider>();

        if (markerCollider != null)
        {
            markerCollider.enabled = false;
        }

        Renderer markerRenderer = markerObject.GetComponent<Renderer>();

        if (markerRenderer != null)
        {
            markerRenderer.material = material;
        }

        if (isTemporary)
        {
            temporaryScannedCellMarkers.Add(markerObject);
        }
    }

    private void CreatePreviewGrid()
    {
        UpdatePreviewGridBounds();

        GameObject gridObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gridObject.name = "Full Scan Grid Overlay";
        gridObject.transform.SetParent(previewGridParent.transform);
        gridObject.transform.position = GetPreviewGridCenter();
        gridObject.transform.localScale = GetPreviewGridScale();

        Collider gridCollider = gridObject.GetComponent<Collider>();

        if (gridCollider != null)
        {
            gridCollider.enabled = false;
        }

        Renderer gridRenderer = gridObject.GetComponent<Renderer>();

        if (gridRenderer != null)
        {
            gridRenderer.material = previewGridMaterial;
            Texture2D gridTexture = CreateGridTexture();
            Vector2 textureScale = new Vector2(previewGridCellCount.x, previewGridCellCount.y);
            gridRenderer.material.mainTexture = gridTexture;
            gridRenderer.material.mainTextureScale = textureScale;

            if (gridRenderer.material.HasProperty("_BaseMap"))
            {
                gridRenderer.material.SetTexture("_BaseMap", gridTexture);
                gridRenderer.material.SetTextureScale("_BaseMap", textureScale);
            }
        }
    }

    private void UpdatePreviewGridBounds()
    {
        Vector2 gridSize = GetPreviewGridWorldSize();
        float safeCellSize = Mathf.Max(0.001f, gridCellSize);
        float halfWidth = gridSize.x * 0.5f;
        float halfDepth = gridSize.y * 0.5f;

        previewGridMinCell = new Vector2Int(
            Mathf.FloorToInt(-halfWidth / safeCellSize),
            Mathf.FloorToInt(-halfDepth / safeCellSize)
        );

        Vector2Int maxCell = new Vector2Int(
            Mathf.CeilToInt(halfWidth / safeCellSize),
            Mathf.CeilToInt(halfDepth / safeCellSize)
        );

        previewGridCellCount = new Vector2Int(
            Mathf.Max(1, maxCell.x - previewGridMinCell.x),
            Mathf.Max(1, maxCell.y - previewGridMinCell.y)
        );
    }

    private Vector2 GetPreviewGridWorldSize()
    {
        TreasureSpawner treasureSpawner = FindAnyObjectByType<TreasureSpawner>();

        if (treasureSpawner != null)
        {
            return new Vector2(
                Mathf.Max(gridCellSize, treasureSpawner.mapSize.x),
                Mathf.Max(gridCellSize, treasureSpawner.mapSize.y)
            );
        }

        return new Vector2(
            Mathf.Max(gridCellSize, fullGridWorldSize.x),
            Mathf.Max(gridCellSize, fullGridWorldSize.y)
        );
    }

    private Vector3 GetPreviewGridCenter()
    {
        Vector3 minWorld = CellCornerToWorld(previewGridMinCell);
        Vector3 maxWorld = CellCornerToWorld(previewGridMinCell + previewGridCellCount);
        return new Vector3(
            (minWorld.x + maxWorld.x) * 0.5f,
            markerYPosition + 0.006f,
            (minWorld.z + maxWorld.z) * 0.5f
        );
    }

    private Vector3 GetPreviewGridScale()
    {
        return new Vector3(
            previewGridCellCount.x * gridCellSize,
            0.002f,
            previewGridCellCount.y * gridCellSize
        );
    }

    private Texture2D CreateGridTexture()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color line = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isLine = x == 0 || y == 0;
                texture.SetPixel(x, y, isLine ? line : clear);
            }
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;
        texture.Apply();
        return texture;
    }

    private void SetPreviewGridVisible(bool isVisible)
    {
        if (previewGridParent != null && previewGridParent.activeSelf != isVisible)
        {
            previewGridParent.SetActive(isVisible);
        }
    }

    private void BlockScan()
    {
        wasScanningLastFrame = false;
        currentScanCells.Clear();
        TickScannedMarkerClear();
        SetPreviewGridVisible(false);
    }

    private void TickScannedMarkerClear()
    {
        timeSinceLastScan += Time.deltaTime;

        if (timeSinceLastScan < scannedMarksClearDelay)
        {
            return;
        }

        SetPreviewGridVisible(false);

        foreach (GameObject marker in temporaryScannedCellMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }

        temporaryScannedCellMarkers.Clear();
        visibleScannedCells.Clear();
        hasPreviousScannedCell = false;
    }

    public void ClearScannedArea()
    {
        scannedCells.Clear();
        visibleScannedCells.Clear();
        currentScanCells.Clear();
        temporaryScannedCellMarkers.Clear();
        hasPreviousScannedCell = false;
        wasScanningLastFrame = false;
        scanTimer = 0f;
        timeSinceLastScan = scannedMarksClearDelay;

        ClearChildren(markerParent);
        SetPreviewGridVisible(false);
    }

    public Vector3 GetCurrentScanPosition()
    {
        return GetScanPosition();
    }

    public bool CanSignalAtCurrentScan()
    {
        return CanScanAt(GetScanPosition());
    }

    public int GetCellDistanceFromCurrentScan(Vector3 worldPosition)
    {
        Vector2Int centerCell = WorldToCell(GetScanPosition());
        Vector2Int targetCell = WorldToCell(worldPosition);
        return GetCellDistanceFromScanFootprint(centerCell, targetCell);
    }

    private int GetCellDistanceFromScanFootprint(Vector2Int centerCell, Vector2Int targetCell)
    {
        int width = GetCurrentScanCellsWide();
        int depth = GetCurrentScanCellsDeep();
        int minX = centerCell.x - width / 2;
        int minY = centerCell.y - depth / 2;
        int maxX = minX + width - 1;
        int maxY = minY + depth - 1;
        int xDistance = targetCell.x < minX ? minX - targetCell.x : targetCell.x > maxX ? targetCell.x - maxX : 0;
        int yDistance = targetCell.y < minY ? minY - targetCell.y : targetCell.y > maxY ? targetCell.y - maxY : 0;
        return Mathf.Max(xDistance, yDistance);
    }

    private int GetCurrentScanCellsWide()
    {
        return metalDetector != null ? metalDetector.CurrentScanCells : Mathf.Max(1, scanCellsWide);
    }

    private int GetCurrentScanCellsDeep()
    {
        return metalDetector != null ? metalDetector.CurrentScanCells : Mathf.Max(1, scanCellsDeep);
    }

    private bool CanScanCell(Vector2Int cell)
    {
        return CanScanAt(CellToWorld(cell));
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

        SearchArea[] searchAreas = FindObjectsByType<SearchArea>();

        if (searchAreas.Length == 0)
        {
            return true;
        }

        foreach (SearchArea searchArea in searchAreas)
        {
            if (searchArea != null && searchArea.isUnlocked && searchArea.Contains(worldPosition))
            {
                return true;
            }
        }

        return false;
    }

    private void ClearChildren(GameObject parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.transform.GetChild(i).gameObject);
        }
    }

    private void RevealTreasuresInScan()
    {
        if (!revealTreasures)
        {
            return;
        }

        DetectableTreasure[] treasures = FindObjectsByType<DetectableTreasure>();

        foreach (DetectableTreasure treasure in treasures)
        {
            if (treasure.isFound || treasure.isRevealed)
            {
                continue;
            }

            Vector2Int treasureCell = WorldToCell(treasure.transform.position);

            if (!currentScanCells.Contains(treasureCell))
            {
                continue;
            }

            SearchMarker marker = CreateTreasureRevealMarker(treasure, treasureCell);
            treasure.Reveal(marker);
            LocalCoopManager.Instance?.ReportTreasureRevealed(treasure);

            if (metalDetector != null)
            {
                metalDetector.ReportTreasureRevealed(treasure);
            }
        }
    }

    private SearchMarker CreateTreasureRevealMarker(DetectableTreasure treasure, Vector2Int treasureCell)
    {
        Vector3 position = CellToWorld(treasureCell);
        GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        markerObject.name = "Dig Target Marker";
        float markerY = GetMarkerYAt(position, markerYPosition) + 0.02f;
        markerObject.transform.position = new Vector3(position.x, markerY, position.z);
        markerObject.transform.localScale = new Vector3(gridCellSize * 0.92f, 0.025f, gridCellSize * 0.92f);

        SearchMarker marker = markerObject.AddComponent<SearchMarker>();
        marker.markerType = SearchMarker.MarkerType.FoundTreasure;
        marker.treasureRarity = treasure.rarity;
        marker.pulseAmount = 0.08f;
        return marker;
    }

    private Vector2Int WorldToCell(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / gridCellSize),
            Mathf.FloorToInt(worldPosition.z / gridCellSize)
        );
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(
            (cell.x + 0.5f) * gridCellSize,
            markerYPosition,
            (cell.y + 0.5f) * gridCellSize
        );
    }

    private Vector3 CellCornerToWorld(Vector2Int cell)
    {
        return new Vector3(
            cell.x * gridCellSize,
            markerYPosition,
            cell.y * gridCellSize
        );
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
