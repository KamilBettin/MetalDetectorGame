using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class EnvironmentScatterBootstrapper
{
    private const string ScatterRootName = "Island Environment Scatter";
    private const int RandomSeed = 71342;
    private static readonly Vector2 HomeClearCenter = new Vector2(-700f, -710f);
    private static readonly Vector2 StarterPlotCenter = new Vector2(-745f, -705f);
    private static readonly Dictionary<Material, Material> fallbackMaterialCache = new Dictionary<Material, Material>();

    private static readonly string[] TreePrefabPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_green.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_apples.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_pears.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_plums.prefab"
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

        ScatterGroup(terrain, waterLevel, root.transform, "Shore Rocks", ShoreRockPrefabPaths, 190, 0.25f, 4.6f, 42f, 0.75f, 1.65f, true, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Shore Plants", ShorePlantPrefabPaths, 360, 0.35f, 5.8f, 32f, 0.75f, 1.35f, false, false);
        ScatterGroup(terrain, waterLevel, root.transform, "Inland Trees", TreePrefabPaths, 145, 4.2f, 55f, 34f, 1.25f, 2.1f, false, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Shrubs", ShrubPrefabPaths, 420, 3.2f, 45f, 36f, 0.75f, 1.45f, false, false);
        ScatterGroup(terrain, waterLevel, root.transform, "Flowers", FlowerPrefabPaths, 360, 4.4f, 35f, 24f, 0.8f, 1.25f, false, false);
        ScatterGroup(terrain, waterLevel, root.transform, "Mushrooms", MushroomPrefabPaths, 140, 5.2f, 42f, 28f, 0.75f, 1.2f, false, false);
        ScatterGroup(terrain, waterLevel, root.transform, "Rocky Slopes", RockPrefabPaths, 260, 7f, 90f, 58f, 0.9f, 2.35f, true, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Dead Wood", RockyTreePrefabPaths, 28, 11f, 80f, 46f, 1f, 1.7f, false, true);

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
        bool keepClearOfHome)
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
            ApplyFallbackMaterials(instance);
            DisableColliders(instance);
            placedCount++;
        }

        if (placedCount == 0)
        {
            Object.Destroy(groupObject);
        }
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

    private static void ApplyFallbackMaterials(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
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

        Texture baseTexture = GetMaterialTexture(sourceMaterial, "_BaseMap", "_BaseTexture", "_BASETEXTURE", "_MainTex");
        bool alphaClipped = IsVegetationMaterial(sourceMaterial.name);
        Material material = CreateFallbackMaterial(GetReadableTint(sourceMaterial.name), baseTexture, alphaClipped, sourceMaterial.name + "_Fallback");
        fallbackMaterialCache[sourceMaterial] = material;
        return material;
    }

    private static Material CreateFallbackMaterial(Color color, Texture baseTexture, bool alphaClipped, string materialName)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader)
        {
            name = materialName,
            color = color
        };

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
            SetMaterialFloat(material, 0.42f, "_Cutoff");
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = 2450;
        }

        return material;
    }

    private static bool IsVegetationMaterial(string materialName)
    {
        string lowerName = materialName.ToLowerInvariant();
        return lowerName.Contains("leaf")
            || lowerName.Contains("leaves")
            || lowerName.Contains("foliage")
            || lowerName.Contains("grass")
            || lowerName.Contains("poppy")
            || lowerName.Contains("mushroom");
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
