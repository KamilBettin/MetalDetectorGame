using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class EnvironmentScatterBootstrapper
{
    private const string ScatterRootName = "Island Environment Scatter";
    private const string VegetationAlphaClipTemplateResourcePath = "EnvironmentScatter/Materials/VegetationAlphaClipTemplate";
    private const int RandomSeed = 71342;
    private static readonly Vector2 HomeClearCenter = new Vector2(-700f, -710f);
    private static readonly Vector2 StarterPlotCenter = new Vector2(-745f, -705f);
    private static readonly Dictionary<Material, Material> fallbackMaterialCache = new Dictionary<Material, Material>();
    private static Material vegetationAlphaClipTemplate;

    private static readonly string[] TreePrefabPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_green.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_apples.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_pears.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_plums.prefab"
    };

    private static readonly string[] NatureTreePrefabPaths =
    {
        "Assets/NatureStarterKit2/Nature/tree01.prefab",
        "Assets/NatureStarterKit2/Nature/tree02.prefab",
        "Assets/NatureStarterKit2/Nature/tree03.prefab",
        "Assets/NatureStarterKit2/Nature/tree04.prefab"
    };

    private static readonly string[] ConiferMeshAssetPaths =
    {
        "Assets/Forst/Conifers [BOTD]/Sources/Conifer Small/SM Conifer Small LOD0.asset",
        "Assets/Forst/Conifers [BOTD]/Sources/Conifer Medium/SM Conifer Medium LOD0.asset",
        "Assets/Forst/Conifers [BOTD]/Sources/Conifer Tall/SM Conifer Tall LOD0.asset",
        "Assets/Forst/Conifers [BOTD]/Sources/Conifer Bare/SM Conifer Bare LOD0.asset"
    };

    private static readonly string[] ConiferPrefabPaths =
    {
        "Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Small BOTD URP.prefab",
        "Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Medium BOTD URP.prefab",
        "Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Tall BOTD URP.prefab",
        "Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Bare BOTD URP.prefab"
    };

    private static readonly string[] RockyTreePrefabPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_dead.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_stump.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_logs.prefab"
    };

    private static readonly string[] ShrubPrefabPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab"
    };

    private static readonly string[] NatureShrubPrefabPaths =
    {
        "Assets/NatureStarterKit2/Nature/bush01.prefab",
        "Assets/NatureStarterKit2/Nature/bush02.prefab",
        "Assets/NatureStarterKit2/Nature/bush03.prefab",
        "Assets/NatureStarterKit2/Nature/bush04.prefab",
        "Assets/NatureStarterKit2/Nature/bush05.prefab",
        "Assets/NatureStarterKit2/Nature/bush06.prefab"
    };

    private static readonly string[] AnimalPrefabPaths =
    {
        "Assets/ithappy/Animals_FREE/Prefabs/Deer_001.prefab",
        "Assets/ithappy/Animals_FREE/Prefabs/Horse_001.prefab",
        "Assets/ithappy/Animals_FREE/Prefabs/Dog_001.prefab",
        "Assets/ithappy/Animals_FREE/Prefabs/Kitty_001.prefab",
        "Assets/ithappy/Animals_FREE/Prefabs/Chicken_001.prefab"
    };

    private static readonly string[] ShorePlantPrefabPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_dead.prefab"
    };

    private static readonly string[] FlowerPrefabPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Flowers/PT_Poppy_02.prefab"
    };

    private static readonly string[] MushroomPrefabPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Mushrooms/PT_Caesars_Mushroom_01.prefab"
    };

    private static readonly string[] RockPrefabPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Generic_Rock_01.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Menhir_Rock_02.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Ore_Rock_01.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Ore_Rock_01_split.prefab"
    };

    private static readonly string[] ShoreRockPrefabPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_River_Rock_Pile_02.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Generic_Rock_01.prefab"
    };

    public static void EnsureIslandEnvironment()
    {
        GameObject existingRoot = GameObject.Find(ScatterRootName);

        if (existingRoot != null)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(existingRoot);
            }
            else
            {
                Object.DestroyImmediate(existingRoot);
            }
        }

        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null)
        {
            Terrain[] terrains = Terrain.activeTerrains;
            terrain = terrains.Length > 0 ? terrains[0] : null;
        }

        if (terrain == null || terrain.terrainData == null)
        {
            return;
        }

        float waterLevel = GetWaterLevel();
        GameObject root = new GameObject(ScatterRootName);
        Random.State previousRandomState = Random.state;
        Random.InitState(RandomSeed);

        ScatterGroup(terrain, waterLevel, root.transform, "Shore Rocks", ShoreRockPrefabPaths, 170, 0.25f, 4.6f, 42f, 0.75f, 1.65f, true, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Shore Plants", ShorePlantPrefabPaths, 240, 0.35f, 5.8f, 32f, 0.75f, 1.35f, false, false);
        ScatterGroup(terrain, waterLevel, root.transform, "Inland Trees", TreePrefabPaths, 65, 4.2f, 55f, 34f, 1.15f, 1.85f, false, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Mixed Island Trees", NatureTreePrefabPaths, 36, 4.8f, 46f, 28f, 0.55f, 1.05f, false, true);
        ScatterConiferGroves(terrain, waterLevel, root.transform, "Conifer Groves", ConiferPrefabPaths, 9, 7, 13, 7.5f, 68f, 27f, 8f, 18f, 0.78f, 1.45f);
        ScatterMeshGroup(terrain, waterLevel, root.transform, "Sparse Conifer Outliers", ConiferMeshAssetPaths, 14, 5.5f, 62f, 30f, 0.6f, 1.1f, false, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Shrubs", ShrubPrefabPaths, 230, 3.2f, 45f, 36f, 0.75f, 1.45f, false, false);
        ScatterGroup(terrain, waterLevel, root.transform, "Mixed Bushes", NatureShrubPrefabPaths, 125, 3.8f, 38f, 30f, 0.45f, 0.95f, false, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Flowers", FlowerPrefabPaths, 260, 4.4f, 35f, 24f, 0.8f, 1.25f, false, false);
        ScatterGroup(terrain, waterLevel, root.transform, "Mushrooms", MushroomPrefabPaths, 90, 5.2f, 42f, 28f, 0.75f, 1.2f, false, false);
        ScatterGroup(terrain, waterLevel, root.transform, "Rocky Slopes", RockPrefabPaths, 260, 7f, 90f, 58f, 0.9f, 2.35f, true, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Dead Wood", RockyTreePrefabPaths, 22, 11f, 80f, 46f, 1f, 1.7f, false, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Island Animals", AnimalPrefabPaths, 18, 4.4f, 34f, 18f, 0.7f, 1.15f, false, true, false, true);

        Random.state = previousRandomState;
    }

    private static void ScatterGroup(
        Terrain terrain,
        float waterLevel,
        Transform root,
        string groupName,
        string[] prefabPaths,
        int targetCount,
        float minHeightAboveWater,
        float maxHeightAboveWater,
        float maxSteepness,
        float minScale,
        float maxScale,
        bool alignToSlope,
        bool keepClearOfHome,
        bool applyFallbackMaterials = true,
        bool disableAmbientControlScripts = false)
    {
        GameObject[] prefabs = LoadPrefabs(prefabPaths);

        if (prefabs.Length == 0)
        {
            return;
        }

        GameObject groupObject = new GameObject(groupName);
        groupObject.transform.SetParent(root.transform, false);
        int placedCount = 0;
        int maxAttempts = targetCount * 34;

        for (int attempt = 0; attempt < maxAttempts && placedCount < targetCount; attempt++)
        {
            if (!TryGetScatterPoint(terrain, waterLevel, minHeightAboveWater, maxHeightAboveWater, maxSteepness, keepClearOfHome, out Vector3 position, out Vector3 normal))
            {
                continue;
            }

            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            GameObject instance = InstantiatePrefab(prefab);

            if (instance == null)
            {
                continue;
            }

            instance.name = prefab.name;
            instance.transform.SetParent(groupObject.transform, true);
            instance.transform.position = position;
            instance.transform.rotation = GetScatterRotation(normal, alignToSlope);
            float scale = Random.Range(minScale, maxScale);
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, Vector3.one * scale);

            if (applyFallbackMaterials)
            {
                ApplyFallbackMaterials(instance);
            }

            if (disableAmbientControlScripts)
            {
                DisableAmbientControlScripts(instance);
            }

            DisableColliders(instance);
            placedCount++;
        }

        if (placedCount == 0)
        {
            Object.Destroy(groupObject);
        }
    }

    private static void ScatterConiferGroves(
        Terrain terrain,
        float waterLevel,
        Transform root,
        string groupName,
        string[] prefabPaths,
        int groveCount,
        int minTreesPerGrove,
        int maxTreesPerGrove,
        float minHeightAboveWater,
        float maxHeightAboveWater,
        float maxSteepness,
        float minGroveRadius,
        float maxGroveRadius,
        float minScale,
        float maxScale)
    {
        GameObject[] prefabs = LoadPrefabs(prefabPaths);

        if (prefabs.Length == 0)
        {
            return;
        }

        GameObject groupObject = new GameObject(groupName);
        groupObject.transform.SetParent(root.transform, false);
        int placedGroveCount = 0;
        int maxCenterAttempts = groveCount * 42;

        for (int centerAttempt = 0; centerAttempt < maxCenterAttempts && placedGroveCount < groveCount; centerAttempt++)
        {
            if (!TryGetScatterPoint(terrain, waterLevel, minHeightAboveWater, maxHeightAboveWater, maxSteepness, true, out Vector3 center, out _))
            {
                continue;
            }

            float groveRadius = Random.Range(minGroveRadius, maxGroveRadius);
            int targetTreeCount = Random.Range(minTreesPerGrove, maxTreesPerGrove + 1);
            GameObject groveObject = new GameObject("Conifer Grove " + (placedGroveCount + 1).ToString("00"));
            groveObject.transform.SetParent(groupObject.transform, false);
            int placedTreeCount = 0;
            int maxTreeAttempts = targetTreeCount * 12;

            for (int treeAttempt = 0; treeAttempt < maxTreeAttempts && placedTreeCount < targetTreeCount; treeAttempt++)
            {
                Vector2 offset = Random.insideUnitCircle * groveRadius;
                Vector3 candidate = new Vector3(center.x + offset.x, center.y, center.z + offset.y);

                if (!TryGetScatterPointAt(terrain, waterLevel, candidate.x, candidate.z, minHeightAboveWater, maxHeightAboveWater, maxSteepness, true, out Vector3 position, out Vector3 normal))
                {
                    continue;
                }

                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                GameObject instance = InstantiatePrefab(prefab);

                if (instance == null)
                {
                    continue;
                }

                instance.name = prefab.name;
                instance.transform.SetParent(groveObject.transform, true);
                instance.transform.position = position;
                instance.transform.rotation = GetScatterRotation(normal, false);

                float centerDistance = Vector2.Distance(new Vector2(center.x, center.z), new Vector2(position.x, position.z));
                float edgeBlend = Mathf.InverseLerp(groveRadius, 0f, centerDistance);
                float scale = Random.Range(minScale, maxScale) * Mathf.Lerp(0.82f, 1.12f, edgeBlend);
                instance.transform.localScale = Vector3.Scale(instance.transform.localScale, Vector3.one * scale);

                DisableColliders(instance);
                placedTreeCount++;
            }

            if (placedTreeCount == 0)
            {
                Object.Destroy(groveObject);
                continue;
            }

            placedGroveCount++;
        }

        if (placedGroveCount == 0)
        {
            Object.Destroy(groupObject);
        }
    }

    private static void ScatterMeshGroup(
        Terrain terrain,
        float waterLevel,
        Transform root,
        string groupName,
        string[] meshAssetPaths,
        int targetCount,
        float minHeightAboveWater,
        float maxHeightAboveWater,
        float maxSteepness,
        float minScale,
        float maxScale,
        bool alignToSlope,
        bool keepClearOfHome)
    {
        Mesh[] meshes = LoadMeshes(meshAssetPaths);

        if (meshes.Length == 0)
        {
            return;
        }

        GameObject groupObject = new GameObject(groupName);
        groupObject.transform.SetParent(root.transform, false);
        int placedCount = 0;
        int maxAttempts = targetCount * 34;

        for (int attempt = 0; attempt < maxAttempts && placedCount < targetCount; attempt++)
        {
            if (!TryGetScatterPoint(terrain, waterLevel, minHeightAboveWater, maxHeightAboveWater, maxSteepness, keepClearOfHome, out Vector3 position, out Vector3 normal))
            {
                continue;
            }

            Mesh mesh = meshes[Random.Range(0, meshes.Length)];
            GameObject instance = CreateMeshInstance(mesh);

            if (instance == null)
            {
                continue;
            }

            instance.transform.SetParent(groupObject.transform, true);
            instance.transform.position = position;
            instance.transform.rotation = GetScatterRotation(normal, alignToSlope);
            float scale = Random.Range(minScale, maxScale);
            instance.transform.localScale = Vector3.one * scale;
            placedCount++;
        }

        if (placedCount == 0)
        {
            Object.Destroy(groupObject);
        }
    }

    private static Mesh[] LoadMeshes(string[] meshAssetPaths)
    {
        List<Mesh> meshes = new List<Mesh>();

#if UNITY_EDITOR
        foreach (string meshAssetPath in meshAssetPaths)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);

            if (mesh != null)
            {
                meshes.Add(mesh);
            }
        }
#endif

        return meshes.ToArray();
    }

    private static GameObject CreateMeshInstance(Mesh mesh)
    {
        if (mesh == null)
        {
            return null;
        }

        GameObject instance = new GameObject(mesh.name);
        MeshFilter meshFilter = instance.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = instance.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterials = CreateConiferMaterials(mesh.subMeshCount);
        return instance;
    }

    private static Material[] CreateConiferMaterials(int subMeshCount)
    {
        int materialCount = Mathf.Max(1, subMeshCount);
        Material[] materials = new Material[materialCount];

        for (int i = 0; i < materialCount; i++)
        {
            bool trunkMaterial = i == 0 && materialCount > 1;
            Color color = trunkMaterial
                ? new Color(0.42f, 0.28f, 0.17f, 1f)
                : new Color(0.18f, 0.43f, 0.18f, 1f);
            materials[i] = CreateFallbackMaterial(color, null, !trunkMaterial, trunkMaterial ? "ConiferTrunk_Fallback" : "ConiferNeedles_Fallback");
        }

        return materials;
    }

    private static bool TryGetScatterPoint(
        Terrain terrain,
        float waterLevel,
        float minHeightAboveWater,
        float maxHeightAboveWater,
        float maxSteepness,
        bool keepClearOfHome,
        out Vector3 position,
        out Vector3 normal)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        float worldX = Random.Range(terrainPosition.x, terrainPosition.x + terrainSize.x);
        float worldZ = Random.Range(terrainPosition.z, terrainPosition.z + terrainSize.z);
        float normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldX);
        float normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldZ);
        float groundY = terrainPosition.y + terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
        float heightAboveWater = groundY - waterLevel;
        float steepness = terrainData.GetSteepness(normalizedX, normalizedZ);

        position = new Vector3(worldX, groundY, worldZ);
        normal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);

        if (heightAboveWater < minHeightAboveWater || heightAboveWater > maxHeightAboveWater)
        {
            return false;
        }

        if (steepness > maxSteepness)
        {
            return false;
        }

        if (keepClearOfHome && IsInsideClearZone(position))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetScatterPointAt(
        Terrain terrain,
        float waterLevel,
        float worldX,
        float worldZ,
        float minHeightAboveWater,
        float maxHeightAboveWater,
        float maxSteepness,
        bool keepClearOfHome,
        out Vector3 position,
        out Vector3 normal)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        position = Vector3.zero;
        normal = Vector3.up;

        if (worldX < terrainPosition.x || worldX > terrainPosition.x + terrainSize.x || worldZ < terrainPosition.z || worldZ > terrainPosition.z + terrainSize.z)
        {
            return false;
        }

        float normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldX);
        float normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldZ);
        float groundY = terrainPosition.y + terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
        float heightAboveWater = groundY - waterLevel;
        float steepness = terrainData.GetSteepness(normalizedX, normalizedZ);

        position = new Vector3(worldX, groundY, worldZ);
        normal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);

        if (heightAboveWater < minHeightAboveWater || heightAboveWater > maxHeightAboveWater)
        {
            return false;
        }

        if (steepness > maxSteepness)
        {
            return false;
        }

        if (keepClearOfHome && IsInsideClearZone(position))
        {
            return false;
        }

        return true;
    }

    private static bool IsInsideClearZone(Vector3 position)
    {
        Vector2 groundPosition = new Vector2(position.x, position.z);

        if (Vector2.Distance(groundPosition, HomeClearCenter) < 22f)
        {
            return true;
        }

        return Vector2.Distance(groundPosition, StarterPlotCenter) < 16f;
    }

    private static Quaternion GetScatterRotation(Vector3 normal, bool alignToSlope)
    {
        Quaternion yaw = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        if (!alignToSlope)
        {
            return yaw;
        }

        return Quaternion.FromToRotation(Vector3.up, normal) * yaw;
    }

    private static float GetWaterLevel()
    {
        OceanWaterSurface waterSurface = Object.FindAnyObjectByType<OceanWaterSurface>();
        return waterSurface != null ? waterSurface.waterLevel : 50f;
    }

    private static GameObject[] LoadPrefabs(string[] prefabPaths)
    {
        List<GameObject> prefabs = new List<GameObject>();

        foreach (string prefabPath in prefabPaths)
        {
            GameObject resourcePrefab = Resources.Load<GameObject>(GetResourcePath(prefabPath));

            if (resourcePrefab != null)
            {
                prefabs.Add(resourcePrefab);
                continue;
            }

#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null)
            {
                prefabs.Add(prefab);
            }
#endif
        }

        return prefabs.ToArray();
    }

    private static string GetResourcePath(string assetPath)
    {
        const string prefabRoot = "/Prefabs/";
        int rootIndex = assetPath.IndexOf(prefabRoot);

        if (rootIndex < 0)
        {
            return "";
        }

        string relativePath = assetPath.Substring(rootIndex + prefabRoot.Length);
        const string prefabExtension = ".prefab";

        if (relativePath.EndsWith(prefabExtension))
        {
            relativePath = relativePath.Substring(0, relativePath.Length - prefabExtension.Length);
        }

        return "EnvironmentScatter/" + relativePath;
    }

    private static GameObject InstantiatePrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

#if UNITY_EDITOR
        Object instance = PrefabUtility.InstantiatePrefab(prefab);
        return instance as GameObject;
#else
        return Object.Instantiate(prefab);
#endif
    }

    private static void DisableColliders(GameObject instance)
    {
        Collider[] colliders = instance.GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private static void DisableAmbientControlScripts(GameObject instance)
    {
        MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;

            if (typeName == "MovePlayerInput" || typeName == "CreatureMover" || typeName == "PlayerCamera")
            {
                behaviour.enabled = false;
            }
        }
    }

    private static void ApplyFallbackMaterials(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer is BillboardRenderer)
            {
                continue;
            }

            Material[] sourceMaterials = renderer.sharedMaterials;

            if (sourceMaterials == null || sourceMaterials.Length == 0)
            {
                continue;
            }

            Material[] runtimeMaterials = new Material[sourceMaterials.Length];

            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                runtimeMaterials[i] = GetFallbackMaterial(sourceMaterials[i], renderer.name, instance.name);
            }

            renderer.sharedMaterials = runtimeMaterials;
        }
    }

    private static Material GetFallbackMaterial(Material sourceMaterial, string rendererName, string instanceName)
    {
        if (sourceMaterial == null)
        {
            return CreateFallbackMaterial(GetReadableTint(rendererName + " " + instanceName), null, false, "ScatterFallback");
        }

        if (fallbackMaterialCache.TryGetValue(sourceMaterial, out Material cachedMaterial) && cachedMaterial != null)
        {
            return cachedMaterial;
        }

        string materialContext = sourceMaterial.name + " " + rendererName + " " + instanceName;
        string alphaContext = sourceMaterial.name + " " + rendererName;
        Texture baseTexture = GetMaterialTexture(sourceMaterial, "_BaseMap", "_BaseTexture", "_BASETEXTURE", "_BaseColorMap", "_MainTex");
        bool alphaClipped = ShouldAlphaClipMaterial(sourceMaterial, alphaContext);
        float cutoff = GetMaterialFloat(sourceMaterial, 0.5f, "_Cutoff", "_MaskClipValue");
        Color tint = GetMaterialColor(sourceMaterial, GetReadableTint(materialContext), "_BaseColor", "_Color");
        Material material = CreateFallbackMaterial(tint, baseTexture, alphaClipped, sourceMaterial.name + "_Fallback", cutoff);
        fallbackMaterialCache[sourceMaterial] = material;
        return material;
    }

    private static Material CreateFallbackMaterial(Color color, Texture baseTexture, bool alphaClipped, string materialName, float cutoff = 0.5f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = alphaClipped && GetVegetationAlphaClipTemplate() != null
            ? new Material(GetVegetationAlphaClipTemplate())
            : new Material(shader);

        material.name = materialName;
        material.color = color;

        if (shader != null && material.shader == null)
        {
            material.shader = shader;
        }

        SetMaterialColor(material, color, "_BaseColor", "_Color");

        if (baseTexture != null)
        {
            SetMaterialTexture(material, baseTexture, "_BaseMap", "_MainTex");
        }

        SetMaterialFloat(material, 0f, "_Metallic");
        SetMaterialFloat(material, 0.25f, "_Smoothness", "_Glossiness");

        if (alphaClipped)
        {
            SetMaterialFloat(material, 1f, "_AlphaClip");
            SetMaterialFloat(material, 1f, "_AlphaToMask");
            SetMaterialFloat(material, 0f, "_Surface");
            SetMaterialFloat(material, Mathf.Clamp(cutoff, 0.05f, 0.95f), "_Cutoff");
            SetMaterialFloat(material, 0f, "_Cull", "_CullMode");
            SetMaterialFloat(material, 1f, "_ZWrite");
            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.doubleSidedGI = true;
            material.renderQueue = 2450;
        }

        return material;
    }

    private static Material GetVegetationAlphaClipTemplate()
    {
        if (vegetationAlphaClipTemplate == null)
        {
            vegetationAlphaClipTemplate = Resources.Load<Material>(VegetationAlphaClipTemplateResourcePath);
        }

        return vegetationAlphaClipTemplate;
    }

    private static bool IsVegetationMaterial(string materialName)
    {
        string lowerName = materialName.ToLowerInvariant();
        return lowerName.Contains("leaf")
            || lowerName.Contains("leaves")
            || lowerName.Contains("branch")
            || lowerName.Contains("branches")
            || lowerName.Contains("foliage")
            || lowerName.Contains("needle")
            || lowerName.Contains("needles")
            || lowerName.Contains("grass")
            || lowerName.Contains("bush")
            || lowerName.Contains("shrub")
            || lowerName.Contains("poppy")
            || lowerName.Contains("mushroom");
    }

    private static bool ShouldAlphaClipMaterial(Material material, string materialContext)
    {
        return IsVegetationMaterial(materialContext)
            || HasAlphaKeyword(material);
    }

    private static bool HasAlphaKeyword(Material material)
    {
        if (material == null || material.shaderKeywords == null)
        {
            return false;
        }

        foreach (string keyword in material.shaderKeywords)
        {
            if (keyword == "_ALPHATEST_ON" || keyword == "_ALPHAPREMULTIPLY_ON" || keyword == "_ALPHABLEND_ON")
            {
                return true;
            }
        }

        return false;
    }

    private static Color GetReadableTint(string objectName)
    {
        string lowerName = objectName.ToLowerInvariant();

        if (lowerName.Contains("rock") || lowerName.Contains("ore") || lowerName.Contains("menhir"))
        {
            return new Color(0.72f, 0.68f, 0.6f, 1f);
        }

        if (lowerName.Contains("trunk") || lowerName.Contains("wood") || lowerName.Contains("log") || lowerName.Contains("dead") || lowerName.Contains("stump"))
        {
            return new Color(0.62f, 0.4f, 0.22f, 1f);
        }

        if (lowerName.Contains("poppy") || lowerName.Contains("flower"))
        {
            return new Color(1f, 0.24f, 0.16f, 1f);
        }

        if (lowerName.Contains("mushroom"))
        {
            return new Color(1f, 0.54f, 0.22f, 1f);
        }

        if (lowerName.Contains("leaf") || lowerName.Contains("leaves") || lowerName.Contains("foliage"))
        {
            return new Color(0.42f, 0.76f, 0.28f, 1f);
        }

        if (lowerName.Contains("grass") || lowerName.Contains("shrub"))
        {
            return new Color(0.48f, 0.78f, 0.28f, 1f);
        }

        return new Color(0.62f, 0.78f, 0.32f, 1f);
    }

    private static Texture GetMaterialTexture(Material material, params string[] names)
    {
        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                Texture texture = material.GetTexture(propertyName);

                if (texture != null)
                {
                    return texture;
                }
            }
        }

        return null;
    }

    private static Color GetMaterialColor(Material material, Color fallback, params string[] names)
    {
        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetColor(propertyName);
            }
        }

        return fallback;
    }

    private static float GetMaterialFloat(Material material, float fallback, params string[] names)
    {
        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetFloat(propertyName);
            }
        }

        return fallback;
    }

    private static void SetMaterialTexture(Material material, Texture texture, params string[] names)
    {
        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }
    }

    private static void SetMaterialColor(Material material, Color color, params string[] names)
    {
        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }
    }

    private static void SetMaterialFloat(Material material, float value, params string[] names)
    {
        foreach (string propertyName in names)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }
    }
}
