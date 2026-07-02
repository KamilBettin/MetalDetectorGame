using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DigSiteVisual : MonoBehaviour
{
    private const float SurfaceOffset = 0.0035f;
    private const float MaxSlopeAlignDegrees = 28f;
    private const string ChestPrefabPath = "Assets/Treasure chest closed/treasure_chest_closed.prefab";
    private const string SoilBasePath = "Assets/TerrainSampleAssets/Textures/Terrain/Soil_Rocks_BaseColor.tif";
    private const string SoilNormalPath = "Assets/TerrainSampleAssets/Textures/Terrain/Soil_Rocks_Normal.tif";
    private const string SoilMaskPath = "Assets/TerrainSampleAssets/Textures/Terrain/Soil_Rocks_MaskMap.tif";
    private const string ChestBasePath = "Assets/Treasure chest closed/Materials and textures/treasure_chest_albedo.tga";
    private const string ChestNormalPath = "Assets/Treasure chest closed/Materials and textures/treasure_chest_normal.tga";
    private const string ChestMetalSmoothPath = "Assets/Treasure chest closed/Materials and textures/treasure_chest_metal+smoo.tga";
    private const string ChestOcclusionPath = "Assets/Treasure chest closed/Materials and textures/treasure_chest_occlusion.tga";

    private static Material cachedSoilMaterial;
    private static Material cachedChestMaterial;
    private static GameObject cachedEditorChestPrefab;

    private class DirtPiece
    {
        public Transform transform;
        public Vector3 basePosition;
        public Vector3 baseScale;
        public float revealAt;
    }

    private readonly List<DirtPiece> rimPieces = new List<DirtPiece>();
    private readonly List<DirtPiece> loosePieces = new List<DirtPiece>();
    private DetectableTreasure treasure;
    private Transform darkCenter;
    private Transform innerShadow;
    private Transform chestRoot;
    private Material soilMaterial;
    private Material soilDarkMaterial;
    private Material soilPieceMaterial;
    private Material configuredRuntimeSoilMaterial;
    private Material configuredSoilSource;
    private Vector3 chestBaseScale = Vector3.one;
    private bool searched;
    private float progress;
    private float readyPulse;

    public DetectableTreasure Treasure => treasure;
    public bool IsReadyToSearch => treasure != null && !treasure.isFound && treasure.currentDigHits >= Mathf.Max(1, treasure.requiredDigHits);
    public Vector3 PromptPosition => chestRoot != null ? chestRoot.position + Vector3.up * 0.55f : transform.position + Vector3.up * 0.85f;
    public Transform HighlightTarget => chestRoot != null ? chestRoot : transform;

    public static DigSiteVisual EnsureForTreasure(DetectableTreasure treasure, Vector3 worldPosition, GameObject chestPrefab, Material sandMaterial, Material soilMaterial)
    {
        if (treasure == null)
        {
            return null;
        }

        DigSiteVisual visual = FindForTreasure(treasure);

        if (visual == null)
        {
            GameObject visualObject = new GameObject("Dig Site - " + treasure.treasureName);
            visual = visualObject.AddComponent<DigSiteVisual>();
            visual.Initialize(treasure, worldPosition, chestPrefab, sandMaterial, soilMaterial);
        }
        else
        {
            visual.transform.position = worldPosition;
            visual.AlignToGround();
            visual.ApplyConfiguredMaterials(sandMaterial, soilMaterial);
            visual.EnsureChest(chestPrefab);
        }

        return visual;
    }

    public static DigSiteVisual FindForTreasure(DetectableTreasure treasure)
    {
        if (treasure == null)
        {
            return null;
        }

        DigSiteVisual[] visuals = FindObjectsByType<DigSiteVisual>();

        foreach (DigSiteVisual visual in visuals)
        {
            if (visual != null && visual.treasure == treasure)
            {
                return visual;
            }
        }

        return null;
    }

    public static void RemoveForTreasure(DetectableTreasure treasure)
    {
        DigSiteVisual visual = FindForTreasure(treasure);

        if (visual != null)
        {
            Destroy(visual.gameObject);
        }
    }

    public void SetProgress(float newProgress)
    {
        progress = Mathf.Clamp01(newProgress);
        float eased = Mathf.SmoothStep(0f, 1f, progress);

        if (darkCenter != null)
        {
            float thickness = Mathf.Lerp(0.001f, 0.0022f, eased);
            darkCenter.localPosition = new Vector3(0f, thickness + 0.0006f, 0f);
            darkCenter.localScale = new Vector3(Mathf.Lerp(0.22f, 0.88f, eased), thickness, Mathf.Lerp(0.16f, 0.61f, eased));
        }

        if (innerShadow != null)
        {
            float thickness = Mathf.Lerp(0.0008f, 0.0016f, eased);
            innerShadow.localPosition = new Vector3(0f, thickness + 0.0012f, 0f);
            innerShadow.localScale = new Vector3(Mathf.Lerp(0.12f, 0.49f, eased), thickness, Mathf.Lerp(0.09f, 0.34f, eased));
        }

        UpdatePieces(rimPieces, eased, 0.72f);
        UpdatePieces(loosePieces, eased, 0.9f);
        UpdateChest(eased);
    }

    public void MarkSearched()
    {
        searched = true;

        if (chestRoot != null)
        {
            Destroy(chestRoot.gameObject);
            chestRoot = null;
        }
    }

    private void Initialize(DetectableTreasure targetTreasure, Vector3 worldPosition, GameObject chestPrefab, Material configuredSandMaterial, Material configuredSoilMaterial)
    {
        treasure = targetTreasure;
        transform.position = worldPosition;
        AlignToGround();
        ApplyConfiguredMaterials(configuredSandMaterial, configuredSoilMaterial);
        BuildHole();
        EnsureChest(chestPrefab);
        SetProgress(treasure != null ? treasure.DigProgress01 : 0f);
    }

    private void ApplyConfiguredMaterials(Material configuredSandMaterial, Material configuredSoilMaterial)
    {
        soilMaterial = GetRuntimeDigMaterial(
            configuredSoilMaterial,
            ref configuredSoilSource,
            ref configuredRuntimeSoilMaterial,
            GetSoilMaterial(),
            "Runtime Configured Dug Soil",
            new Color(0.28f, 0.19f, 0.12f, 1f),
            SoilBasePath,
            SoilNormalPath,
            SoilMaskPath);

        BuildDigMaterialVariants();
        ReapplyHoleMaterials();
    }

    private void BuildHole()
    {
        darkCenter = CreatePrimitive("Dark Dug Soil", PrimitiveType.Cylinder, soilDarkMaterial).transform;
        darkCenter.SetParent(transform, false);
        darkCenter.localPosition = new Vector3(0f, 0.0016f, 0f);

        innerShadow = CreatePrimitive("Wet Inner Soil", PrimitiveType.Cylinder, CreateTintedMaterial(soilMaterial, new Color(0.34f, 0.24f, 0.16f, 1f))).transform;
        innerShadow.SetParent(transform, false);
        innerShadow.localPosition = new Vector3(0f, 0.0024f, 0f);

        CreateRimPieces();
        CreateLoosePieces();
    }

    private void CreateRimPieces()
    {
        int count = 36;

        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f;
            float wobble = i % 2 == 0 ? 1.08f : 0.92f;
            GameObject piece = CreateDirtClump("Dug Soil Rim Clump", GetRimMaterial(i), i);
            piece.transform.SetParent(transform, false);

            Vector3 basePosition = new Vector3(Mathf.Cos(angle) * 0.52f * wobble, 0f, Mathf.Sin(angle) * 0.39f * wobble);
            Vector3 baseScale = new Vector3(0.052f + (i % 4) * 0.006f, 0.011f + (i % 3) * 0.002f, 0.035f + (i % 5) * 0.004f);
            piece.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + (i % 5) * 7f, 0f);

            rimPieces.Add(new DirtPiece
            {
                transform = piece.transform,
                basePosition = basePosition,
                baseScale = baseScale,
                revealAt = 0.06f + i / (float)count * 0.5f
            });
        }
    }

    private void CreateLoosePieces()
    {
        int count = 26;

        for (int i = 0; i < count; i++)
        {
            float angle = (i * 1.37f + 0.21f) * Mathf.PI;
            GameObject piece = CreateDirtClump("Scattered Dirt Clump", GetLooseMaterial(i), i + 37);
            piece.transform.SetParent(transform, false);
            float radiusX = 0.58f + (i % 9) * 0.025f;
            float radiusZ = 0.46f + (i % 7) * 0.022f;

            loosePieces.Add(new DirtPiece
            {
                transform = piece.transform,
                basePosition = new Vector3(Mathf.Cos(angle) * radiusX, 0f, Mathf.Sin(angle) * radiusZ),
                baseScale = new Vector3(0.026f + (i % 5) * 0.003f, 0.01f + (i % 3) * 0.002f, 0.02f + (i % 4) * 0.003f),
                revealAt = 0.16f + i / (float)count * 0.55f
            });

            piece.transform.localRotation = Quaternion.Euler(8f + i * 17f, i * 53f, 4f + i * 29f);
        }
    }

    private void BuildDigMaterialVariants()
    {
        soilDarkMaterial = CreateTintedMaterial(soilMaterial, new Color(0.2f, 0.13f, 0.08f, 1f));
        soilPieceMaterial = CreateTintedMaterial(soilMaterial, new Color(0.2f, 0.13f, 0.08f, 1f));
        SetTextureScale(soilPieceMaterial, new Vector2(12f, 12f), "_BaseColorMap", "_BaseMap", "_MainTex");
        SetFloat(soilPieceMaterial, 0.08f, "_Smoothness", "_Glossiness");
    }

    private void ReapplyHoleMaterials()
    {
        AssignRendererMaterial(darkCenter, soilDarkMaterial);
        AssignRendererMaterial(innerShadow, soilDarkMaterial);

        for (int i = 0; i < rimPieces.Count; i++)
        {
            AssignRendererMaterial(rimPieces[i].transform, GetRimMaterial(i));
        }

        for (int i = 0; i < loosePieces.Count; i++)
        {
            AssignRendererMaterial(loosePieces[i].transform, GetLooseMaterial(i));
        }
    }

    private Material GetRimMaterial(int index)
    {
        return soilPieceMaterial != null ? soilPieceMaterial : soilDarkMaterial != null ? soilDarkMaterial : soilMaterial;
    }

    private Material GetLooseMaterial(int index)
    {
        return soilPieceMaterial != null ? soilPieceMaterial : soilDarkMaterial != null ? soilDarkMaterial : soilMaterial;
    }

    private static void AssignRendererMaterial(Transform target, Material material)
    {
        if (target == null || material == null)
        {
            return;
        }

        Renderer targetRenderer = target.GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            targetRenderer.material = material;
        }
    }

    private void EnsureChest(GameObject configuredChestPrefab)
    {
        if (chestRoot != null)
        {
            return;
        }

        GameObject chestPrefab = configuredChestPrefab != null ? configuredChestPrefab : LoadEditorChestPrefab();
        GameObject chestObject = chestPrefab != null ? Instantiate(chestPrefab) : CreateProceduralChest();
        chestObject.name = "Unearthed Treasure Chest";
        chestRoot = chestObject.transform;
        chestRoot.SetParent(transform, false);
        chestRoot.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        NormalizeChestScale(chestObject, new Vector3(0.48f, 0.3f, 0.34f));
        chestBaseScale = chestRoot.localScale;
        DisableCollidersInChildren(chestObject);
        ApplyChestMaterials(chestObject);
    }

    private void UpdatePieces(List<DirtPiece> pieces, float easedProgress, float spread)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            DirtPiece piece = pieces[i];

            if (piece.transform == null)
            {
                continue;
            }

            float reveal = Mathf.Clamp01((easedProgress - piece.revealAt) / 0.22f);
            piece.transform.gameObject.SetActive(reveal > 0.01f);
            piece.transform.localScale = piece.baseScale * reveal;

            float planarSpread = Mathf.Lerp(0.42f, spread, easedProgress);
            Vector3 position = new Vector3(piece.basePosition.x * planarSpread, 0f, piece.basePosition.z * planarSpread);
            position.y = Mathf.Max(0.0015f, piece.baseScale.y * reveal * 0.5f + 0.0008f);
            piece.transform.localPosition = position;
        }
    }

    private void AlignToGround()
    {
        if (!TryGetGroundSurface(transform.position, out float groundY, out Vector3 groundNormal))
        {
            return;
        }

        Vector3 position = transform.position;
        transform.position = new Vector3(position.x, groundY + SurfaceOffset, position.z);
        Vector3 alignedNormal = Vector3.RotateTowards(Vector3.up, groundNormal.normalized, MaxSlopeAlignDegrees * Mathf.Deg2Rad, 0f);
        transform.rotation = Quaternion.FromToRotation(Vector3.up, alignedNormal);
    }

    private static bool TryGetGroundSurface(Vector3 worldPosition, out float groundY, out Vector3 groundNormal)
    {
        Terrain terrain = GetTerrainAt(worldPosition);

        if (terrain != null)
        {
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            float normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldPosition.x);
            float normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldPosition.z);
            groundY = terrainPosition.y + terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
            groundNormal = terrain.transform.TransformDirection(terrain.terrainData.GetInterpolatedNormal(normalizedX, normalizedZ));
            return true;
        }

        if (Physics.Raycast(worldPosition + Vector3.up * 4f, Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore))
        {
            groundY = hit.point.y;
            groundNormal = hit.normal;
            return true;
        }

        groundY = worldPosition.y;
        groundNormal = Vector3.up;
        return false;
    }

    private static Terrain GetTerrainAt(Vector3 worldPosition)
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

    private static bool IsInsideTerrain(Terrain terrain, Vector3 worldPosition)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        return worldPosition.x >= terrainPosition.x
            && worldPosition.x <= terrainPosition.x + terrainSize.x
            && worldPosition.z >= terrainPosition.z
            && worldPosition.z <= terrainPosition.z + terrainSize.z;
    }

    private void UpdateChest(float easedProgress)
    {
        if (chestRoot == null)
        {
            return;
        }

        float reveal = Mathf.Clamp01(Mathf.InverseLerp(0.18f, 1f, easedProgress));
        chestRoot.gameObject.SetActive(reveal > 0.02f || searched);

        if (searched)
        {
            return;
        }

        float y = Mathf.Lerp(-0.34f, 0.09f, Mathf.SmoothStep(0f, 1f, reveal));
        chestRoot.localPosition = new Vector3(0f, y, 0f);
        chestRoot.localScale = chestBaseScale * Mathf.Lerp(0.82f, 1f, reveal);

        if (IsReadyToSearch)
        {
            readyPulse += Time.deltaTime * 5.5f;
            chestRoot.localPosition += Vector3.up * (Mathf.Sin(readyPulse) * 0.016f);
        }
    }

    private void Update()
    {
        if (treasure == null)
        {
            Destroy(gameObject);
        }
    }

    private static GameObject CreatePrimitive(string objectName, PrimitiveType primitiveType, Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = objectName;

        Renderer primitiveRenderer = primitive.GetComponent<Renderer>();

        if (primitiveRenderer != null)
        {
            primitiveRenderer.material = material;
        }

        Collider primitiveCollider = primitive.GetComponent<Collider>();

        if (primitiveCollider != null)
        {
            primitiveCollider.enabled = false;
        }

        return primitive;
    }

    private static GameObject CreateDirtClump(string objectName, Material material, int seed)
    {
        GameObject clump = new GameObject(objectName);
        MeshFilter meshFilter = clump.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = clump.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = CreateDirtClumpMesh(seed);
        meshRenderer.material = material;
        return clump;
    }

    private static Mesh CreateDirtClumpMesh(int seed)
    {
        const int rings = 8;
        const int segments = 14;
        List<Vector3> vertices = new List<Vector3>((rings + 1) * (segments + 1));
        List<Vector2> uvs = new List<Vector2>((rings + 1) * (segments + 1));
        List<int> triangles = new List<int>(rings * segments * 6);

        for (int ring = 0; ring <= rings; ring++)
        {
            float v = ring / (float)rings;
            float latitude = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, v);
            float y = Mathf.Sin(latitude);
            float radius = Mathf.Cos(latitude);

            for (int segment = 0; segment <= segments; segment++)
            {
                float u = segment / (float)segments;
                float angle = u * Mathf.PI * 2f;
                float noiseA = Mathf.PerlinNoise(seed * 0.173f + u * 3.7f, seed * 0.241f + v * 4.1f);
                float noiseB = Mathf.PerlinNoise(seed * 0.317f + u * 9.2f, seed * 0.421f + v * 8.4f);
                float ridge = Mathf.Sin((u * 5.5f + v * 3.2f + seed * 0.19f) * Mathf.PI * 2f) * 0.07f;
                float lump = 0.78f + noiseA * 0.26f + noiseB * 0.14f + ridge;

                float x = Mathf.Cos(angle) * radius * lump * 0.5f;
                float z = Mathf.Sin(angle) * radius * lump * 0.5f;
                float yLump = y * (0.48f + noiseB * 0.12f);
                vertices.Add(new Vector3(x, yLump, z));
                uvs.Add(new Vector2(u * 2.4f, v * 2.4f));
            }
        }

        for (int ring = 0; ring < rings; ring++)
        {
            for (int segment = 0; segment < segments; segment++)
            {
                int current = ring * (segments + 1) + segment;
                int next = current + segments + 1;
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(current + 1);
                triangles.Add(current + 1);
                triangles.Add(next);
                triangles.Add(next + 1);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Runtime Dirt Clump Mesh";
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material GetSoilMaterial()
    {
        if (cachedSoilMaterial == null)
        {
            cachedSoilMaterial = CreateTexturedMaterial("Runtime Dug Soil", new Color(0.28f, 0.19f, 0.12f, 1f), SoilBasePath, SoilNormalPath, SoilMaskPath);
        }

        return cachedSoilMaterial;
    }

    private static Material GetRuntimeDigMaterial(
        Material configuredMaterial,
        ref Material configuredSource,
        ref Material configuredRuntimeMaterial,
        Material fallbackMaterial,
        string materialName,
        Color fallbackTint,
        string basePath,
        string normalPath,
        string maskPath)
    {
        if (configuredMaterial == null)
        {
            return fallbackMaterial;
        }

        if (configuredRuntimeMaterial != null && configuredSource == configuredMaterial)
        {
            return configuredRuntimeMaterial;
        }

        configuredSource = configuredMaterial;
        configuredRuntimeMaterial = CreateRuntimeDigMaterialFromConfigured(configuredMaterial, materialName, fallbackTint, basePath, normalPath, maskPath);
        return configuredRuntimeMaterial;
    }

    private static Material CreateRuntimeDigMaterialFromConfigured(Material configuredMaterial, string materialName, Color fallbackTint, string basePath, string normalPath, string maskPath)
    {
        Color tint = GetMaterialColor(configuredMaterial, fallbackTint);
        Material material = CreateTexturedMaterial(materialName, tint, basePath, normalPath, maskPath);

        Texture baseTexture = GetMaterialTexture(configuredMaterial, "_BaseColorMap", "_BaseMap", "_MainTex");
        Texture normalTexture = GetMaterialTexture(configuredMaterial, "_NormalMap", "_BumpMap");
        Texture maskTexture = GetMaterialTexture(configuredMaterial, "_MaskMap", "_MetallicGlossMap", "_OcclusionMap");

        if (baseTexture != null)
        {
            SetTexture(material, baseTexture, "_BaseColorMap", "_BaseMap", "_MainTex");
        }

        if (normalTexture != null)
        {
            SetTexture(material, normalTexture, "_NormalMap", "_BumpMap");
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
        }

        if (maskTexture != null)
        {
            SetTexture(material, maskTexture, "_MaskMap", "_MetallicGlossMap", "_OcclusionMap");
        }

        CopyFloat(configuredMaterial, material, "_Metallic");
        CopyFloat(configuredMaterial, material, "_Smoothness", "_Glossiness");
        return material;
    }

    private static Material CreateTexturedMaterial(string materialName, Color tint, string basePath, string normalPath, string maskPath)
    {
        Shader shader = GetLitShader();

        Material material = new Material(shader);
        material.name = materialName;
        SetMaterialColor(material, tint);
        Texture2D baseTexture = LoadEditorTexture(basePath);
        Texture2D normalTexture = LoadEditorTexture(normalPath);
        Texture2D maskTexture = LoadEditorTexture(maskPath);

        if (baseTexture == null)
        {
            baseTexture = CreateNoiseTexture(tint);
        }

        SetTexture(material, baseTexture, "_BaseColorMap", "_BaseMap", "_MainTex");
        SetTextureScale(material, new Vector2(3.5f, 3.5f), "_BaseColorMap", "_BaseMap", "_MainTex");

        if (normalTexture != null)
        {
            SetTexture(material, normalTexture, "_NormalMap", "_BumpMap");
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
        }

        if (maskTexture != null)
        {
            SetTexture(material, maskTexture, "_MaskMap", "_MetallicGlossMap", "_OcclusionMap");
        }

        SetFloat(material, 0f, "_Metallic");
        SetFloat(material, 0.32f, "_Smoothness", "_Glossiness");
        return material;
    }

    private static Material CreateTintedMaterial(Material source, Color tint)
    {
        Material material = source != null ? new Material(source) : CreateTexturedMaterial("Runtime Tinted Soil", tint, SoilBasePath, SoilNormalPath, SoilMaskPath);
        SetMaterialColor(material, tint);
        return material;
    }

    private static Material CreateTransparentMaterial(Color color)
    {
        Shader shader = GetLitShader();

        Material material = new Material(shader);
        SetMaterialColor(material, color);

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_SurfaceType"))
        {
            material.SetFloat("_SurfaceType", 1f);
        }

        if (material.HasProperty("_BlendMode"))
        {
            material.SetFloat("_BlendMode", 0f);
        }

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        return material;
    }

    private static Texture2D LoadEditorTexture(string path)
    {
#if UNITY_EDITOR
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (texture != null)
        {
            return texture;
        }
#endif

        return null;
    }

    private static GameObject LoadEditorChestPrefab()
    {
#if UNITY_EDITOR
        if (cachedEditorChestPrefab == null)
        {
            cachedEditorChestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChestPrefabPath);
        }

        return cachedEditorChestPrefab;
#else
        return null;
#endif
    }

    private static Texture2D CreateNoiseTexture(Color baseColor)
    {
        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float broad = Mathf.PerlinNoise(x * 0.055f, y * 0.055f);
                float fine = Mathf.PerlinNoise((x + 31f) * 0.28f, (y + 17f) * 0.28f);
                float grain = Mathf.Clamp01(broad * 0.65f + fine * 0.35f);
                texture.SetPixel(x, y, Color.Lerp(baseColor * 0.72f, Color.Lerp(baseColor, Color.white, 0.24f), grain));
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }

    private static void NormalizeChestScale(GameObject chestObject, Vector3 targetSize)
    {
        Bounds bounds = CalculateRendererBounds(chestObject);
        float largestSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float targetLargestSize = Mathf.Max(targetSize.x, targetSize.y, targetSize.z);

        if (largestSize > 0.001f)
        {
            chestObject.transform.localScale = Vector3.one * (targetLargestSize / largestSize);
        }
    }

    private static Bounds CalculateRendererBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return new Bounds(target.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static GameObject CreateProceduralChest()
    {
        GameObject root = new GameObject("Procedural Treasure Chest");
        Material wood = CreateSimpleMaterial("Runtime Chest Wood", new Color(0.34f, 0.18f, 0.08f, 1f), 0.22f);
        Material metal = CreateSimpleMaterial("Runtime Chest Metal", new Color(0.74f, 0.56f, 0.22f, 1f), 0.55f);

        GameObject baseBox = CreatePrimitive("Chest Base", PrimitiveType.Cube, wood);
        baseBox.transform.SetParent(root.transform, false);
        baseBox.transform.localScale = new Vector3(0.68f, 0.28f, 0.42f);
        baseBox.transform.localPosition = new Vector3(0f, 0.12f, 0f);

        GameObject lid = CreatePrimitive("Chest Lid", PrimitiveType.Cube, wood);
        lid.transform.SetParent(root.transform, false);
        lid.transform.localScale = new Vector3(0.72f, 0.16f, 0.46f);
        lid.transform.localPosition = new Vector3(0f, 0.35f, 0f);

        for (int i = -1; i <= 1; i += 2)
        {
            GameObject band = CreatePrimitive("Chest Metal Band", PrimitiveType.Cube, metal);
            band.transform.SetParent(root.transform, false);
            band.transform.localScale = new Vector3(0.055f, 0.5f, 0.48f);
            band.transform.localPosition = new Vector3(i * 0.22f, 0.24f, 0f);
        }

        GameObject lockPlate = CreatePrimitive("Chest Lock", PrimitiveType.Cube, metal);
        lockPlate.transform.SetParent(root.transform, false);
        lockPlate.transform.localScale = new Vector3(0.13f, 0.13f, 0.035f);
        lockPlate.transform.localPosition = new Vector3(0f, 0.24f, -0.235f);
        return root;
    }

    private static Material CreateSimpleMaterial(string materialName, Color color, float smoothness)
    {
        Shader shader = GetLitShader();

        Material material = new Material(shader);
        material.name = materialName;
        SetMaterialColor(material, color);
        SetFloat(material, smoothness, "_Smoothness", "_Glossiness");
        return material;
    }

    private static void ApplyChestMaterials(GameObject chestObject)
    {
        Renderer[] renderers = chestObject.GetComponentsInChildren<Renderer>(true);
        Material chestMaterial = GetChestMaterial();

        foreach (Renderer targetRenderer in renderers)
        {
            Material[] materials = targetRenderer.materials;

            if (materials == null || materials.Length == 0)
            {
                targetRenderer.material = chestMaterial;
                continue;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = chestMaterial;
            }

            targetRenderer.materials = materials;
        }
    }

    private static Material GetChestMaterial()
    {
        if (cachedChestMaterial == null)
        {
            cachedChestMaterial = CreateChestMaterial();
        }

        return cachedChestMaterial;
    }

    private static Material CreateChestMaterial()
    {
        Material material = CreateSimpleMaterial("Runtime Textured Treasure Chest", Color.white, 0.48f);
        Texture2D baseTexture = LoadEditorTexture(ChestBasePath);
        Texture2D normalTexture = LoadEditorTexture(ChestNormalPath);
        Texture2D metalSmoothTexture = LoadEditorTexture(ChestMetalSmoothPath);
        Texture2D occlusionTexture = LoadEditorTexture(ChestOcclusionPath);

        if (baseTexture != null)
        {
            SetTexture(material, baseTexture, "_BaseColorMap", "_BaseMap", "_MainTex");
        }
        else
        {
            SetMaterialColor(material, new Color(0.38f, 0.2f, 0.09f, 1f));
        }

        if (normalTexture != null)
        {
            SetTexture(material, normalTexture, "_NormalMap", "_BumpMap");
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
        }

        if (metalSmoothTexture != null)
        {
            SetTexture(material, metalSmoothTexture, "_MaskMap", "_MetallicGlossMap");
        }

        if (occlusionTexture != null)
        {
            SetTexture(material, occlusionTexture, "_OcclusionMap");
        }

        SetFloat(material, 0.22f, "_Metallic");
        SetFloat(material, 0.42f, "_Smoothness", "_Glossiness");
        return material;
    }

    private static void DisableCollidersInChildren(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);

        foreach (Collider targetCollider in colliders)
        {
            targetCollider.enabled = false;
        }
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (!material.HasProperty("_BaseColor") && !material.HasProperty("_Color"))
        {
            material.color = color;
        }
    }

    private static Color GetMaterialColor(Material material, Color fallback)
    {
        if (material == null)
        {
            return fallback;
        }

        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        return fallback;
    }

    private static Texture GetMaterialTexture(Material material, params string[] names)
    {
        if (material == null)
        {
            return null;
        }

        foreach (string propertyName in names)
        {
            if (!material.HasProperty(propertyName))
            {
                continue;
            }

            Texture texture = material.GetTexture(propertyName);

            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }

    private static Shader GetLitShader()
    {
        string pipelineName = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
            ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().FullName
            : "";

        if (pipelineName.Contains("HighDefinition"))
        {
            Shader shader = Shader.Find("HDRP/Lit");

            if (shader != null)
            {
                return shader;
            }
        }

        if (pipelineName.Contains("Universal"))
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader != null)
            {
                return shader;
            }
        }

        return Shader.Find("HDRP/Lit")
            ?? Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");
    }

    private static void SetTexture(Material material, Texture texture, params string[] names)
    {
        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }
    }

    private static void SetTextureScale(Material material, Vector2 scale, params string[] names)
    {
        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTextureScale(propertyName, scale);
            }
        }
    }

    private static void SetFloat(Material material, float value, params string[] names)
    {
        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }
    }

    private static void CopyFloat(Material source, Material target, params string[] names)
    {
        if (source == null || target == null)
        {
            return;
        }

        foreach (string propertyName in names)
        {
            if (!source.HasProperty(propertyName) || !target.HasProperty(propertyName))
            {
                continue;
            }

            target.SetFloat(propertyName, source.GetFloat(propertyName));
        }
    }
}
