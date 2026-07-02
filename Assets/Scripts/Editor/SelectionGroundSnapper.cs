using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SelectionGroundSnapper
{
    private const float RaycastHeight = 1000f;
    private const float RaycastDistance = 2500f;
    private const float EarthPadPadding = 0f;
    private const float EarthPadTopOverlap = 0.05f;
    private const float EarthPadMinimumHeight = 0.25f;

    [MenuItem("Tools/Metal Detector Game/Snap Selected To Ground/By Object Center")]
    private static void SnapSelectedToGround()
    {
        int snappedCount = 0;

        foreach (Transform transform in Selection.transforms)
        {
            if (transform == null || !TryGetRendererBounds(transform, out Bounds bounds))
            {
                continue;
            }

            Vector3 samplePosition = bounds.center;

            if (!TryGetGroundHeight(samplePosition, transform, out float groundY))
            {
                groundY = 0f;
            }

            float deltaY = groundY - bounds.min.y;

            if (Mathf.Approximately(deltaY, 0f))
            {
                continue;
            }

            Undo.RecordObject(transform, "Snap selected to ground");
            transform.position += Vector3.up * deltaY;
            EditorUtility.SetDirty(transform);
            snappedCount++;
        }

        if (snappedCount > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        Debug.Log("Snapped " + snappedCount + " selected object(s) to ground.");
    }

    [MenuItem("Tools/Metal Detector Game/Snap Selected To Ground/By Highest Footprint Point")]
    private static void SnapSelectedToHighestFootprintPoint()
    {
        int snappedCount = 0;

        foreach (Transform transform in Selection.transforms)
        {
            if (transform == null || !TryGetRendererBounds(transform, out Bounds bounds))
            {
                continue;
            }

            if (!TryGetHighestGroundUnderBounds(bounds, transform, out float groundY))
            {
                groundY = 0f;
            }

            float deltaY = groundY - bounds.min.y;

            if (Mathf.Approximately(deltaY, 0f))
            {
                continue;
            }

            Undo.RecordObject(transform, "Snap selected to highest ground point");
            transform.position += Vector3.up * deltaY;
            EditorUtility.SetDirty(transform);
            snappedCount++;
        }

        MarkSceneDirtyIfNeeded(snappedCount);
        Debug.Log("Snapped " + snappedCount + " selected object(s) to the highest ground point under their footprint.");
    }

    [MenuItem("Tools/Metal Detector Game/Snap Selected To Ground/By Individual Parts")]
    private static void SnapSelectedPartsToGround()
    {
        int snappedCount = 0;

        foreach (Transform selectedRoot in Selection.transforms)
        {
            if (selectedRoot == null)
            {
                continue;
            }

            Renderer[] renderers = selectedRoot.GetComponentsInChildren<Renderer>();
            Dictionary<Transform, Bounds> boundsByPart = new Dictionary<Transform, Bounds>();

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || renderer.transform == selectedRoot)
                {
                    continue;
                }

                Transform partTransform = renderer.transform;

                if (boundsByPart.TryGetValue(partTransform, out Bounds existingBounds))
                {
                    existingBounds.Encapsulate(renderer.bounds);
                    boundsByPart[partTransform] = existingBounds;
                    continue;
                }

                boundsByPart.Add(partTransform, renderer.bounds);
            }

            foreach (KeyValuePair<Transform, Bounds> partBounds in boundsByPart)
            {
                Transform partTransform = partBounds.Key;
                Bounds bounds = partBounds.Value;

                if (!TryGetGroundHeight(bounds.center, selectedRoot, out float groundY))
                {
                    groundY = 0f;
                }

                float deltaY = groundY - bounds.min.y;

                if (Mathf.Approximately(deltaY, 0f))
                {
                    continue;
                }

                Undo.RecordObject(partTransform, "Snap selected part to ground");
                partTransform.position += Vector3.up * deltaY;
                EditorUtility.SetDirty(partTransform);
                snappedCount++;
            }
        }

        MarkSceneDirtyIfNeeded(snappedCount);
        Debug.Log("Snapped " + snappedCount + " child part(s) to ground.");
    }

    [MenuItem("Tools/Metal Detector Game/Create Earth Pad Under Selected")]
    private static void CreateEarthPadUnderSelected()
    {
        int createdCount = 0;

        foreach (Transform transform in Selection.transforms)
        {
            if (transform == null || !TryGetRendererBounds(transform, out Bounds bounds))
            {
                continue;
            }

            if (!TryGetLowestGroundUnderBounds(bounds, transform, out float groundY))
            {
                groundY = 0f;
            }

            float topY = bounds.min.y + EarthPadTopOverlap;
            float height = Mathf.Max(EarthPadMinimumHeight, topY - groundY);
            float centerY = topY - height * 0.5f;

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(pad, "Create earth pad under selected");
            pad.name = transform.name + " Earth Pad";
            pad.transform.position = new Vector3(bounds.center.x, centerY, bounds.center.z);
            pad.transform.localScale = new Vector3(
                Mathf.Max(0.01f, bounds.size.x + EarthPadPadding * 2f),
                height,
                Mathf.Max(0.01f, bounds.size.z + EarthPadPadding * 2f));

            if (transform.parent != null)
            {
                pad.transform.SetParent(transform.parent, true);
            }

            Renderer padRenderer = pad.GetComponent<Renderer>();

            if (padRenderer != null)
            {
                padRenderer.sharedMaterial = CreateEarthPadMaterial();
            }

            createdCount++;
        }

        MarkSceneDirtyIfNeeded(createdCount);
        Debug.Log("Created " + createdCount + " earth pad(s) under selected object(s).");
    }

    [MenuItem("Tools/Metal Detector Game/Snap Selected To Ground/By Object Center", true)]
    [MenuItem("Tools/Metal Detector Game/Snap Selected To Ground/By Highest Footprint Point", true)]
    [MenuItem("Tools/Metal Detector Game/Snap Selected To Ground/By Individual Parts", true)]
    [MenuItem("Tools/Metal Detector Game/Create Earth Pad Under Selected", true)]
    private static bool CanSnapSelectedToGround()
    {
        return Selection.transforms.Length > 0;
    }

    private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        bounds = default;
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return hasBounds;
    }

    private static bool TryGetHighestGroundUnderBounds(Bounds bounds, Transform ignoredRoot, out float groundY)
    {
        float highestGround = float.NegativeInfinity;
        bool foundGround = false;

        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                Vector3 samplePosition = new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, x * 0.5f),
                    bounds.center.y,
                    Mathf.Lerp(bounds.min.z, bounds.max.z, z * 0.5f));

                if (!TryGetGroundHeight(samplePosition, ignoredRoot, out float sampleGroundY))
                {
                    continue;
                }

                if (sampleGroundY > highestGround)
                {
                    highestGround = sampleGroundY;
                    foundGround = true;
                }
            }
        }

        groundY = highestGround;
        return foundGround;
    }

    private static bool TryGetLowestGroundUnderBounds(Bounds bounds, Transform ignoredRoot, out float groundY)
    {
        float lowestGround = float.PositiveInfinity;
        bool foundGround = false;

        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                Vector3 samplePosition = new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, x * 0.5f),
                    bounds.center.y,
                    Mathf.Lerp(bounds.min.z, bounds.max.z, z * 0.5f));

                if (!TryGetGroundHeight(samplePosition, ignoredRoot, out float sampleGroundY))
                {
                    continue;
                }

                if (sampleGroundY < lowestGround)
                {
                    lowestGround = sampleGroundY;
                    foundGround = true;
                }
            }
        }

        groundY = lowestGround;
        return foundGround;
    }

    private static bool TryGetGroundHeight(Vector3 worldPosition, Transform ignoredRoot, out float groundY)
    {
        if (TryGetTerrainHeight(worldPosition, out groundY))
        {
            return true;
        }

        Vector3 rayStart = new Vector3(worldPosition.x, worldPosition.y + RaycastHeight, worldPosition.z);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, RaycastDistance);
        float highestGround = float.NegativeInfinity;
        bool foundGround = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(ignoredRoot))
            {
                continue;
            }

            if (hit.point.y > highestGround)
            {
                highestGround = hit.point.y;
                foundGround = true;
            }
        }

        groundY = highestGround;
        return foundGround;
    }

    private static bool TryGetTerrainHeight(Vector3 worldPosition, out float groundY)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            bool insideX = worldPosition.x >= terrainPosition.x && worldPosition.x <= terrainPosition.x + terrainSize.x;
            bool insideZ = worldPosition.z >= terrainPosition.z && worldPosition.z <= terrainPosition.z + terrainSize.z;

            if (!insideX || !insideZ)
            {
                continue;
            }

            groundY = terrain.SampleHeight(worldPosition) + terrainPosition.y;
            return true;
        }

        groundY = 0f;
        return false;
    }

    private static void MarkSceneDirtyIfNeeded(int changedCount)
    {
        if (changedCount > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    private static Material CreateEarthPadMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = "Generated Earth Pad Material";

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", new Color(0.33f, 0.25f, 0.16f));
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", new Color(0.33f, 0.25f, 0.16f));
        }

        return material;
    }
}
