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
    private const float HomeClearRadius = 36f;
    private const float StarterClearRadius = 82f;
    private const float FruitTreeScaleMultiplier = 1.5f;
    private const float ForestFringeWidth = 42f;
    private static readonly Vector2 HomeClearCenter = new Vector2(-700f, -710f);
    private static readonly Vector2 StarterPlotCenter = new Vector2(-745f, -705f);
    private static readonly Vector2 ForestAreaMin = new Vector2(-630f, -770f);
    private static readonly Vector2 ForestAreaMax = new Vector2(-520f, -690f);
    private static readonly Dictionary<Material, Material> fallbackMaterialCache = new Dictionary<Material, Material>();
    private static Material vegetationAlphaClipTemplate;

    private static readonly string[] PineTreePrefabPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green.prefab"
    };

    private static readonly string[] FruitTreePrefabPaths =
    {
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

    private static readonly string[] MediumConiferPrefabPaths =
    {
        "Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/PF Conifer Medium BOTD URP.prefab"
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

    private static readonly string[] HomeDogPrefabPaths =
    {
        "Assets/ithappy/Animals_FREE/Prefabs/Dog_001.prefab"
    };

    private static readonly string[] HomeCatPrefabPaths =
    {
        "Assets/ithappy/Animals_FREE/Prefabs/Kitty_001.prefab"
    };

    private static readonly string[] HomeChickenPrefabPaths =
    {
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
        fallbackMaterialCache.Clear();
        Random.State previousRandomState = Random.state;
        Random.InitState(RandomSeed);

        ScatterGroup(terrain, waterLevel, root.transform, "Shore Rocks", ShoreRockPrefabPaths, 170, 0.25f, 4.6f, 42f, 0.75f, 1.65f, true, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Shore Plants", ShorePlantPrefabPaths, 310, 0.35f, 5.8f, 32f, 0.75f, 1.35f, false, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Inland Pine Trees", PineTreePrefabPaths, 150, 9.5f, 55f, 34f, 1.55f, 3.05f, false, true, enableTreeCollisions: true);
        ScatterGroup(terrain, waterLevel, root.transform, "Island Fruit Trees", FruitTreePrefabPaths, 26, 9.5f, 55f, 34f, 1.55f, 2.65f, false, true, enableTreeCollisions: true);
        ScatterGroup(terrain, waterLevel, root.transform, "Mixed Island Trees", NatureTreePrefabPaths, 125, 9f, 46f, 28f, 1.1f, 2.35f, false, true, enableTreeCollisions: true);
        ScatterConiferGroves(terrain, waterLevel, root.transform, "Conifer Groves", ConiferPrefabPaths, 22, 12, 24, 12f, 68f, 27f, 12f, 26f, 0.95f, 1.9f);
        ScatterGroup(terrain, waterLevel, root.transform, "Medium Conifer Landmarks", MediumConiferPrefabPaths, 95, 13f, 74f, 27f, 1.05f, 2.15f, false, true, enableTreeCollisions: true);
        ScatterMeshGroup(terrain, waterLevel, root.transform, "Sparse Conifer Outliers", ConiferMeshAssetPaths, 60, 11f, 62f, 30f, 0.8f, 1.5f, false, true);
        ScatterHomeForest(terrain, waterLevel, root.transform);
        ScatterGroup(terrain, waterLevel, root.transform, "Shrubs", ShrubPrefabPaths, 340, 3.2f, 45f, 36f, 0.75f, 1.45f, false, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Mixed Bushes", NatureShrubPrefabPaths, 190, 3.8f, 38f, 30f, 0.45f, 0.95f, false, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Flowers", FlowerPrefabPaths, 330, 4.4f, 35f, 24f, 0.8f, 1.25f, false, false);
        ScatterGroup(terrain, waterLevel, root.transform, "Mushrooms", MushroomPrefabPaths, 125, 5.2f, 42f, 28f, 0.75f, 1.2f, false, false);
        ScatterGroup(terrain, waterLevel, root.transform, "Rocky Slopes", RockPrefabPaths, 260, 7f, 90f, 58f, 0.9f, 2.35f, true, true);
        ScatterGroup(terrain, waterLevel, root.transform, "Dead Wood", RockyTreePrefabPaths, 36, 11f, 80f, 46f, 1f, 1.7f, false, true);
        ScatterHomeAnimals(terrain, root.transform);
        ScatterGroup(terrain, waterLevel, root.transform, "Island Animals", AnimalPrefabPaths, 10, 4.4f, 46f, 22f, 0.7f, 1.15f, false, true, false, true, 68f);

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
        bool disableAmbientControlScripts = false,
        float minDistanceBetween = 0f,
        bool enableTreeCollisions = false)
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
        List<Vector3> placedPositions = minDistanceBetween > 0f ? new List<Vector3>() : null;

        for (int attempt = 0; attempt < maxAttempts && placedCount < targetCount; attempt++)
        {
            if (!TryGetScatterPoint(terrain, waterLevel, minHeightAboveWater, maxHeightAboveWater, maxSteepness, keepClearOfHome, out Vector3 position, out Vector3 normal))
            {
                continue;
            }

            if (!IsFarEnoughFromPlaced(position, placedPositions, minDistanceBetween))
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
            float scale = GetVariedScale(minScale, maxScale) * GetPrefabScaleMultiplier(prefab);
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, Vector3.one * scale);

            if (applyFallbackMaterials)
            {
                ApplyFallbackMaterials(instance);
            }

            if (disableAmbientControlScripts)
            {
                DisableAmbientControlScripts(instance);
            }

            if (enableTreeCollisions)
            {
                EnableTreeCollisions(instance);
            }
            else
            {
                DisableColliders(instance);
            }
            placedPositions?.Add(position);
            placedCount++;
        }

        if (placedCount == 0)
        {
            Object.Destroy(groupObject);
        }
    }

    private static bool IsFarEnoughFromPlaced(Vector3 position, List<Vector3> placedPositions, float minDistanceBetween)
    {
        if (placedPositions == null || minDistanceBetween <= 0f)
        {
            return true;
        }

        float minSqrDistance = minDistanceBetween * minDistanceBetween;

        foreach (Vector3 placedPosition in placedPositions)
        {
            Vector2 current = new Vector2(position.x, position.z);
            Vector2 placed = new Vector2(placedPosition.x, placedPosition.z);

            if ((current - placed).sqrMagnitude < minSqrDistance)
            {
                return false;
            }
        }

        return true;
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
                float scale = GetVariedScale(minScale, maxScale) * Mathf.Lerp(0.82f, 1.12f, edgeBlend);
                instance.transform.localScale = Vector3.Scale(instance.transform.localScale, Vector3.one * scale);

                EnableTreeCollisions(instance);
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
            float scale = GetVariedScale(minScale, maxScale);
            instance.transform.localScale = Vector3.one * scale;
            EnableTreeCollisions(instance);
            placedCount++;
        }

        if (placedCount == 0)
        {
            Object.Destroy(groupObject);
        }
    }

    private static void ScatterHomeForest(Terrain terrain, float waterLevel, Transform root)
    {
        List<GameObject> treePrefabs = new List<GameObject>();
        GameObject[] coniferPrefabs = LoadPrefabs(ConiferPrefabPaths);
        GameObject[] pinePrefabs = LoadPrefabs(PineTreePrefabPaths);

        for (int i = 0; i < 3; i++)
        {
            treePrefabs.AddRange(coniferPrefabs);
        }

        treePrefabs.AddRange(LoadPrefabs(MediumConiferPrefabPaths));
        treePrefabs.AddRange(LoadPrefabs(MediumConiferPrefabPaths));
        treePrefabs.AddRange(pinePrefabs);
        treePrefabs.AddRange(pinePrefabs);
        treePrefabs.AddRange(LoadPrefabs(NatureTreePrefabPaths));

        if (treePrefabs.Count == 0)
        {
            return;
        }

        GameObject groupObject = new GameObject("Forest Behind Home");
        groupObject.transform.SetParent(root.transform, false);
        List<Vector3> placedTreePositions = new List<Vector3>();

        int placedTreeCount = ScatterForestAreaPrefabs(
            terrain,
            waterLevel,
            groupObject.transform,
            treePrefabs,
            targetCount: 125,
            minHeightAboveWater: 8f,
            maxHeightAboveWater: 70f,
            maxSteepness: 29f,
            minScale: 1.05f,
            maxScale: 2.25f,
            edgePadding: 4f,
            minDistanceBetween: 4f,
            applyFallbackMaterials: true,
            enableTreeCollisions: true,
            placedPositions: placedTreePositions);

        placedTreeCount += ScatterForestAreaPrefabs(
            terrain,
            waterLevel,
            groupObject.transform,
            treePrefabs,
            targetCount: 70,
            minHeightAboveWater: 8f,
            maxHeightAboveWater: 70f,
            maxSteepness: 29f,
            minScale: 0.9f,
            maxScale: 1.85f,
            edgePadding: 0f,
            minDistanceBetween: 5f,
            applyFallbackMaterials: true,
            enableTreeCollisions: true,
            placedPositions: placedTreePositions,
            outerSpread: ForestFringeWidth,
            outsideCoreOnly: true);

        List<GameObject> fruitTreePrefabs = new List<GameObject>(LoadPrefabs(FruitTreePrefabPaths));

        if (fruitTreePrefabs.Count > 0)
        {
            placedTreeCount += ScatterForestAreaPrefabs(
                terrain,
                waterLevel,
                groupObject.transform,
                fruitTreePrefabs,
                targetCount: 6,
                minHeightAboveWater: 8f,
                maxHeightAboveWater: 70f,
                maxSteepness: 29f,
                minScale: 1.05f,
                maxScale: 1.75f,
                edgePadding: 5f,
                minDistanceBetween: 4f,
                applyFallbackMaterials: true,
                enableTreeCollisions: true,
                placedPositions: placedTreePositions);

            placedTreeCount += ScatterForestAreaPrefabs(
                terrain,
                waterLevel,
                groupObject.transform,
                fruitTreePrefabs,
                targetCount: 2,
                minHeightAboveWater: 8f,
                maxHeightAboveWater: 70f,
                maxSteepness: 29f,
                minScale: 0.9f,
                maxScale: 1.5f,
                edgePadding: 0f,
                minDistanceBetween: 5f,
                applyFallbackMaterials: true,
                enableTreeCollisions: true,
                placedPositions: placedTreePositions,
                outerSpread: ForestFringeWidth,
                outsideCoreOnly: true);
        }

        List<GameObject> shrubPrefabs = new List<GameObject>();
        shrubPrefabs.AddRange(LoadPrefabs(ShrubPrefabPaths));
        shrubPrefabs.AddRange(LoadPrefabs(NatureShrubPrefabPaths));

        if (shrubPrefabs.Count > 0)
        {
            List<Vector3> placedShrubPositions = new List<Vector3>();

            ScatterForestAreaPrefabs(
                terrain,
                waterLevel,
                groupObject.transform,
                shrubPrefabs,
                targetCount: 75,
                minHeightAboveWater: 6f,
                maxHeightAboveWater: 65f,
                maxSteepness: 32f,
                minScale: 0.55f,
                maxScale: 1.2f,
                edgePadding: 2f,
                minDistanceBetween: 2f,
                applyFallbackMaterials: true,
                enableTreeCollisions: false,
                placedPositions: placedShrubPositions);

            ScatterForestAreaPrefabs(
                terrain,
                waterLevel,
                groupObject.transform,
                shrubPrefabs,
                targetCount: 55,
                minHeightAboveWater: 6f,
                maxHeightAboveWater: 65f,
                maxSteepness: 32f,
                minScale: 0.42f,
                maxScale: 0.95f,
                edgePadding: 0f,
                minDistanceBetween: 2.5f,
                applyFallbackMaterials: true,
                enableTreeCollisions: false,
                placedPositions: placedShrubPositions,
                outerSpread: ForestFringeWidth,
                outsideCoreOnly: true);
        }

        if (placedTreeCount == 0)
        {
            Object.Destroy(groupObject);
        }
    }

    private static int ScatterForestAreaPrefabs(
        Terrain terrain,
        float waterLevel,
        Transform root,
        List<GameObject> prefabs,
        int targetCount,
        float minHeightAboveWater,
        float maxHeightAboveWater,
        float maxSteepness,
        float minScale,
        float maxScale,
        float edgePadding,
        float minDistanceBetween,
        bool applyFallbackMaterials,
        bool enableTreeCollisions,
        List<Vector3> placedPositions,
        float outerSpread = 0f,
        bool outsideCoreOnly = false)
    {
        int placedCount = 0;
        int maxAttempts = targetCount * 30;
        float xMin = outerSpread > 0f ? ForestAreaMin.x - outerSpread : ForestAreaMin.x + edgePadding;
        float xMax = outerSpread > 0f ? ForestAreaMax.x + outerSpread : ForestAreaMax.x - edgePadding;
        float zMin = outerSpread > 0f ? ForestAreaMin.y - outerSpread : ForestAreaMin.y + edgePadding;
        float zMax = outerSpread > 0f ? ForestAreaMax.y + outerSpread : ForestAreaMax.y - edgePadding;

        for (int attempt = 0; attempt < maxAttempts && placedCount < targetCount; attempt++)
        {
            Vector2 ground = new Vector2(Random.Range(xMin, xMax), Random.Range(zMin, zMax));
            float placementBlend = 1f;

            if (outsideCoreOnly)
            {
                if (IsInsideForestArea(ground))
                {
                    continue;
                }

                float outsideDistance = GetDistanceOutsideForestArea(ground);

                if (outsideDistance > outerSpread)
                {
                    continue;
                }

                placementBlend = 1f - Mathf.Clamp01(outsideDistance / Mathf.Max(0.01f, outerSpread));
                float placementChance = Mathf.Lerp(0.06f, 1f, placementBlend * placementBlend);

                if (Random.value > placementChance)
                {
                    continue;
                }
            }

            if (!TryGetScatterPointAt(terrain, waterLevel, ground.x, ground.y, minHeightAboveWater, maxHeightAboveWater, maxSteepness, true, out Vector3 position, out Vector3 normal))
            {
                continue;
            }

            if (!IsFarEnoughFromPlaced(position, placedPositions, minDistanceBetween))
            {
                continue;
            }

            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            GameObject instance = InstantiatePrefab(prefab);

            if (instance == null)
            {
                continue;
            }

            instance.name = prefab.name;
            instance.transform.SetParent(root, true);
            instance.transform.position = position;
            instance.transform.rotation = GetScatterRotation(normal, false);

            float edgeDistance = Mathf.Min(position.x - xMin, xMax - position.x, position.z - zMin, zMax - position.z);
            float edgeBlend = outsideCoreOnly ? placementBlend : Mathf.InverseLerp(0f, 12f, edgeDistance);
            float scale = GetVariedScale(minScale, maxScale) * GetPrefabScaleMultiplier(prefab) * Mathf.Lerp(0.78f, 1.08f, edgeBlend);
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, Vector3.one * scale);

            if (applyFallbackMaterials)
            {
                ApplyFallbackMaterials(instance);
            }

            if (enableTreeCollisions)
            {
                EnableTreeCollisions(instance);
            }
            else
            {
                DisableColliders(instance);
            }

            placedPositions?.Add(position);
            placedCount++;
        }

        return placedCount;
    }

    private static bool IsInsideForestArea(Vector2 position)
    {
        return position.x >= ForestAreaMin.x
            && position.x <= ForestAreaMax.x
            && position.y >= ForestAreaMin.y
            && position.y <= ForestAreaMax.y;
    }

    private static float GetDistanceOutsideForestArea(Vector2 position)
    {
        float xDistance = Mathf.Max(ForestAreaMin.x - position.x, 0f, position.x - ForestAreaMax.x);
        float zDistance = Mathf.Max(ForestAreaMin.y - position.y, 0f, position.y - ForestAreaMax.y);
        return Mathf.Sqrt(xDistance * xDistance + zDistance * zDistance);
    }

    private static float GetVariedScale(float minScale, float maxScale)
    {
        float t = Random.value;
        float variantRoll = Random.value;

        if (variantRoll < 0.28f)
        {
            t = t * t;
        }
        else if (variantRoll > 0.6f)
        {
            t = 1f - (1f - t) * (1f - t);
        }

        return Mathf.Lerp(minScale, maxScale, t);
    }

    private static float GetPrefabScaleMultiplier(GameObject prefab)
    {
        if (prefab == null)
        {
            return 1f;
        }

        string prefabName = prefab.name;

        if (prefabName.Contains("PT_Fruit_Tree_01") && !prefabName.Contains("logs"))
        {
            return FruitTreeScaleMultiplier;
        }

        return 1f;
    }

    private static void ScatterHomeAnimals(Terrain terrain, Transform root)
    {
        GameObject groupObject = new GameObject("Home Animals");
        groupObject.transform.SetParent(root.transform, false);

        GameObject[] dogPrefabs = LoadPrefabs(HomeDogPrefabPaths);
        GameObject[] catPrefabs = LoadPrefabs(HomeCatPrefabPaths);
        GameObject[] chickenPrefabs = LoadPrefabs(HomeChickenPrefabPaths);
        int placedCount = 0;

        placedCount += PlaceHomeAnimals(
            terrain,
            groupObject.transform,
            dogPrefabs,
            new[]
            {
                new Vector2(-11f, -8f)
            },
            minScale: 1.8f,
            maxScale: 2.16f,
            wanderRadius: 18f,
            pauseChance: 0.36f,
            runChance: 0.28f,
            groundYOffset: -0.12f,
            walkSpeed: 1.45f,
            runSpeed: 3.4f);

        placedCount += PlaceHomeAnimals(
            terrain,
            groupObject.transform,
            catPrefabs,
            new[]
            {
                new Vector2(12f, 7f)
            },
            minScale: 1.56f,
            maxScale: 1.9f,
            wanderRadius: 10f,
            pauseChance: 0.34f,
            runChance: 0.28f,
            groundYOffset: -0.06f,
            walkSpeed: 1.5f,
            runSpeed: 3.4f);

        placedCount += PlaceHomeAnimals(
            terrain,
            groupObject.transform,
            chickenPrefabs,
            new[]
            {
                new Vector2(-18f, 12f),
                new Vector2(18f, -12f),
                new Vector2(2f, 18f),
                new Vector2(-20f, -4f),
                new Vector2(15f, 9f)
            },
            minScale: 1.16f,
            maxScale: 1.52f,
            wanderRadius: 14f,
            pauseChance: 0.12f,
            runChance: 0.48f,
            groundYOffset: 0f,
            walkSpeed: 1.2f,
            runSpeed: 2.8f);

        if (placedCount == 0)
        {
            Object.Destroy(groupObject);
        }
    }

    private static int PlaceHomeAnimals(
        Terrain terrain,
        Transform root,
        GameObject[] prefabs,
        Vector2[] offsets,
        float minScale,
        float maxScale,
        float wanderRadius,
        float pauseChance,
        float runChance,
        float groundYOffset,
        float walkSpeed,
        float runSpeed)
    {
        if (prefabs == null || prefabs.Length == 0 || offsets == null || offsets.Length == 0)
        {
            return 0;
        }

        int placedCount = 0;

        foreach (Vector2 offset in offsets)
        {
            Vector2 ground = HomeClearCenter + offset;

            if (!TryGetTerrainSurfaceAt(terrain, ground.x, ground.y, out Vector3 position, out Vector3 normal))
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
            instance.transform.SetParent(root, true);
            instance.transform.position = position + Vector3.up * groundYOffset;
            instance.transform.rotation = GetScatterRotation(normal, false);
            float scale = GetVariedScale(minScale, maxScale);
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, Vector3.one * scale);
            DisableAnimalPlayerInputScripts(instance);

            AnimalWanderController wanderController = instance.AddComponent<AnimalWanderController>();
            bool isDog = prefab.name.Contains("Dog");
            float minDecisionTime = isDog ? 6f : 1.2f;
            float maxDecisionTime = isDog ? 11f : 4.4f;
            float minimumTargetDistance = isDog ? Mathf.Min(8f, wanderRadius * 0.6f) : 0f;
            wanderController.Configure(
                instance.transform.position,
                wanderRadius,
                minDecisionTime,
                maxDecisionTime,
                pauseChance,
                runChance,
                groundYOffset,
                walkSpeed,
                runSpeed,
                minimumTargetDistance);
            placedCount++;
        }

        return placedCount;
    }

    private static bool TryGetTerrainSurfaceAt(Terrain terrain, float worldX, float worldZ, out Vector3 position, out Vector3 normal)
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

        position = new Vector3(worldX, groundY, worldZ);
        normal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
        return true;
    }

    private static void DisableAnimalPlayerInputScripts(GameObject instance)
    {
        MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;

            if (typeName == "MovePlayerInput" || typeName == "PlayerCamera")
            {
                behaviour.enabled = false;
            }
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

        if (Vector2.Distance(groundPosition, HomeClearCenter) < HomeClearRadius)
        {
            return true;
        }

        return Vector2.Distance(groundPosition, StarterPlotCenter) < StarterClearRadius;
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
        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private static void EnableTreeCollisions(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
        bool hasTrunkCollider = false;

        foreach (Collider collider in colliders)
        {
            bool isTrunkCollider = collider is CapsuleCollider;
            collider.enabled = isTrunkCollider;

            if (isTrunkCollider)
            {
                collider.isTrigger = false;
                hasTrunkCollider = true;
            }
        }

        if (hasTrunkCollider || !TryGetLocalRendererBounds(instance, out Bounds localBounds))
        {
            return;
        }

        float radius = Mathf.Clamp(localBounds.size.y * 0.04f, 0.2f, 0.65f);
        float height = Mathf.Max(radius * 2f, localBounds.size.y * 0.68f);
        CapsuleCollider trunkCollider = instance.AddComponent<CapsuleCollider>();
        trunkCollider.direction = 1;
        trunkCollider.radius = radius;
        trunkCollider.height = height;
        trunkCollider.center = new Vector3(
            localBounds.center.x,
            localBounds.min.y + height * 0.5f,
            localBounds.center.z);
        trunkCollider.isTrigger = false;
    }

    private static bool TryGetLocalRendererBounds(GameObject instance, out Bounds localBounds)
    {
        localBounds = new Bounds();
        bool hasBounds = false;
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is BillboardRenderer)
            {
                continue;
            }

            Bounds worldBounds = renderer.bounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 worldCorner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 localCorner = instance.transform.InverseTransformPoint(worldCorner);

                        if (!hasBounds)
                        {
                            localBounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        return hasBounds && localBounds.size.y > 0.01f;
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
        bool vegetationMaterial = IsVegetationMaterial(alphaContext);
        bool alphaClipped = ShouldAlphaClipMaterial(sourceMaterial, alphaContext);
        float cutoff = GetVegetationCutoff(sourceMaterial, vegetationMaterial);
        Color tint = GetFallbackTint(sourceMaterial, materialContext, vegetationMaterial);
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
        SetMaterialFloat(material, 0.08f, "_Smoothness", "_Glossiness");
        SetMaterialFloat(material, 0f, "_SpecularHighlights");
        SetMaterialFloat(material, 0f, "_EnvironmentReflections");

        if (alphaClipped)
        {
            SetMaterialFloat(material, 1f, "_AlphaClip");
            SetMaterialFloat(material, 0f, "_AlphaToMask");
            SetMaterialFloat(material, 0f, "_Surface");
            SetMaterialFloat(material, Mathf.Clamp(cutoff, 0.05f, 0.95f), "_Cutoff");
            SetMaterialFloat(material, 0f, "_Cull", "_CullMode");
            SetMaterialFloat(material, 1f, "_ZWrite");
            SetMaterialFloat(material, 0f, "_ReceiveShadows");
            material.EnableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
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

    private static float GetVegetationCutoff(Material material, bool vegetationMaterial)
    {
        float cutoff = GetMaterialFloat(material, 0.5f, "_Cutoff", "_MaskClipValue");
        return vegetationMaterial ? Mathf.Max(cutoff, 0.52f) : cutoff;
    }

    private static Color GetFallbackTint(Material material, string materialContext, bool vegetationMaterial)
    {
        Color fallback = GetReadableTint(materialContext);
        Color tint = GetMaterialColor(material, fallback, "_BaseColor", "_Color");

        if (vegetationMaterial && IsNearWhite(tint))
        {
            return fallback;
        }

        return tint;
    }

    private static bool IsNearWhite(Color color)
    {
        return color.r > 0.88f && color.g > 0.88f && color.b > 0.88f;
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
