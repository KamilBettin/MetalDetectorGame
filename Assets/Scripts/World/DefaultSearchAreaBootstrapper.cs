using UnityEngine;

public static class DefaultSearchAreaBootstrapper
{
    private const string BasicAreaName = "Basic Ground";
    private const string BasicAreaRootName = "Search Area - Basic Ground";
    private const float SignOffsetOutsideArea = 2f;
    private const float PlayerSpawnOffsetInsideArea = 6f;
    private const float PlayerSpawnHeightOffset = 1.1f;
    private const float BoundarySegmentLength = 1.25f;
    private const float BoundaryThickness = 0.42f;
    private const float BoundaryHeight = 0.035f;
    private const float BoundaryGroundOffset = 0.035f;

    private static readonly Vector2 BasicAreaMin = new Vector2(-770f, -730f);
    private static readonly Vector2 BasicAreaMax = new Vector2(-720f, -680f);
    private static readonly Vector2 BeachSpawnPosition = new Vector2(-920f, -720f);
    private static bool movedPlayerToBasicArea;

    public static void EnsureDefaultSearchAreas()
    {
        bool hasBasicArea = HasSearchArea(BasicAreaName) || GameObject.Find(BasicAreaRootName) != null;

        if (!hasBasicArea)
        {
            CreateBasicGroundArea();
        }

        MovePlayerToBasicAreaSpawn();
    }

    private static bool HasSearchArea(string areaName)
    {
        SearchArea[] searchAreas = Object.FindObjectsByType<SearchArea>();

        foreach (SearchArea area in searchAreas)
        {
            if (area != null && area.areaName == areaName)
            {
                return true;
            }
        }

        return false;
    }

    private static void CreateBasicGroundArea()
    {
        Vector2 size = BasicAreaMax - BasicAreaMin;
        Vector2 center = (BasicAreaMin + BasicAreaMax) * 0.5f;
        Vector3 centerPosition = new Vector3(center.x, GetGroundY(center.x, center.y), center.y);

        GameObject areaObject = new GameObject(BasicAreaRootName);
        areaObject.transform.position = centerPosition;

        SearchArea area = areaObject.AddComponent<SearchArea>();
        area.areaName = BasicAreaName;
        area.unlockCost = 0;
        area.isUnlocked = false;
        area.size = size;
        area.areaColor = new Color(0.35f, 0.9f, 0.45f, 0.28f);

        Material lockedMaterial = CreateMaterial(new Color(1f, 0.72f, 0.18f, 0.62f), true);
        Material unlockedMaterial = CreateMaterial(new Color(0.25f, 0.95f, 0.42f, 0.5f), true);
        Material postMaterial = CreateMaterial(new Color(0.28f, 0.16f, 0.08f, 1f), false);
        Material boardMaterial = CreateMaterial(new Color(0.55f, 0.34f, 0.16f, 1f), false);

        GameObject lockedMarker = CreateAreaBoundary("Basic Ground Locked Boundary", areaObject.transform, center, size, lockedMaterial);
        GameObject unlockedMarker = CreateAreaBoundary("Basic Ground Unlocked Boundary", areaObject.transform, center, size, unlockedMaterial);
        unlockedMarker.SetActive(false);

        Vector3 signPosition = new Vector3(center.x, GetGroundY(center.x, BasicAreaMax.y + SignOffsetOutsideArea), BasicAreaMax.y + SignOffsetOutsideArea);
        GameObject purchaseSign = CreateSign(
            "Basic Ground Purchase Sign",
            areaObject.transform,
            signPosition,
            centerPosition,
            "BASIC\nGROUND\nFREE",
            postMaterial,
            boardMaterial
        );

        SearchAreaPurchasePoint purchasePoint = purchaseSign.AddComponent<SearchAreaPurchasePoint>();
        purchasePoint.targetArea = area;
        purchasePoint.interactionDistance = 8.5f;

        GameObject ownedSign = CreateSign(
            "Basic Ground Owned Sign",
            areaObject.transform,
            signPosition,
            centerPosition,
            "BASIC\nGROUND\nOWNED",
            postMaterial,
            boardMaterial
        );
        ownedSign.SetActive(false);

        area.lockedObjects = new[] { lockedMarker, purchaseSign };
        area.unlockedObjects = new[] { unlockedMarker, ownedSign };

        Debug.Log("Created Basic Ground search area at " + centerPosition + ".");
    }

    private static void MovePlayerToBasicAreaSpawn()
    {
        if (movedPlayerToBasicArea)
        {
            return;
        }

        FirstPersonController firstPersonController = Object.FindAnyObjectByType<FirstPersonController>();
        Transform player = firstPersonController != null ? firstPersonController.transform : null;

        if (player == null)
        {
            PlayerInventory playerInventory = Object.FindAnyObjectByType<PlayerInventory>();
            player = playerInventory != null ? playerInventory.transform : null;
        }

        if (player == null)
        {
            return;
        }

        Vector2 center = (BasicAreaMin + BasicAreaMax) * 0.5f;
        Vector3 signFlatPosition = new Vector3(center.x, 0f, center.y);
        Vector3 spawnPosition = new Vector3(BeachSpawnPosition.x, 0f, BeachSpawnPosition.y);
        spawnPosition.y = GetGroundY(spawnPosition.x, spawnPosition.z) + PlayerSpawnHeightOffset;

        CharacterController characterController = player.GetComponent<CharacterController>();
        bool wasControllerEnabled = characterController != null && characterController.enabled;

        if (wasControllerEnabled)
        {
            characterController.enabled = false;
        }

        player.position = spawnPosition;

        Vector3 lookDirection = signFlatPosition - spawnPosition;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            player.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        if (firstPersonController != null && firstPersonController.playerCamera != null)
        {
            firstPersonController.playerCamera.localRotation = Quaternion.identity;
        }

        if (wasControllerEnabled)
        {
            characterController.enabled = true;
        }

        movedPlayerToBasicArea = true;
        Debug.Log("Moved player next to Basic Ground purchase sign at " + spawnPosition + ".");
    }

    private static GameObject CreateAreaBoundary(string name, Transform parent, Vector2 center, Vector2 size, Material material)
    {
        GameObject boundary = new GameObject(name);
        boundary.transform.SetParent(parent);

        float xMin = center.x - size.x * 0.5f;
        float xMax = center.x + size.x * 0.5f;
        float zMin = center.y - size.y * 0.5f;
        float zMax = center.y + size.y * 0.5f;

        CreateBoundaryEdge(boundary.transform, "North Edge", new Vector3(xMin, 0f, zMax), new Vector3(xMax, 0f, zMax), material);
        CreateBoundaryEdge(boundary.transform, "South Edge", new Vector3(xMin, 0f, zMin), new Vector3(xMax, 0f, zMin), material);
        CreateBoundaryEdge(boundary.transform, "East Edge", new Vector3(xMax, 0f, zMin), new Vector3(xMax, 0f, zMax), material);
        CreateBoundaryEdge(boundary.transform, "West Edge", new Vector3(xMin, 0f, zMin), new Vector3(xMin, 0f, zMax), material);

        CreateCornerPost(boundary.transform, new Vector3(xMin, 0f, zMin), material);
        CreateCornerPost(boundary.transform, new Vector3(xMin, 0f, zMax), material);
        CreateCornerPost(boundary.transform, new Vector3(xMax, 0f, zMin), material);
        CreateCornerPost(boundary.transform, new Vector3(xMax, 0f, zMax), material);

        return boundary;
    }

    private static void CreateBoundaryEdge(Transform parent, string name, Vector3 start, Vector3 end, Material material)
    {
        Vector3 flatDelta = new Vector3(end.x - start.x, 0f, end.z - start.z);
        float edgeLength = flatDelta.magnitude;

        if (edgeLength <= 0.001f)
        {
            return;
        }

        int segmentCount = Mathf.Max(1, Mathf.CeilToInt(edgeLength / BoundarySegmentLength));
        float segmentLength = edgeLength / segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (i + 0.5f) / segmentCount;
            Vector3 position = Vector3.Lerp(start, end, t);
            CreateBoundarySegment(parent, name + " Segment", position, flatDelta.normalized, segmentLength, material);
        }
    }

    private static void CreateBoundarySegment(Transform parent, string name, Vector3 position, Vector3 direction, float segmentLength, Material material)
    {
        Vector3 normal = GetGroundNormal(position.x, position.z);
        position.y = GetGroundY(position.x, position.z) + BoundaryGroundOffset;

        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = name;
        segment.transform.SetParent(parent);
        segment.transform.position = position;
        segment.transform.rotation = GetGroundAlignedRotation(direction, normal);
        segment.transform.localScale = new Vector3(BoundaryThickness, BoundaryHeight, segmentLength * 0.94f);
        SetMaterial(segment, material);
        DisableCollider(segment);
    }

    private static void CreateCornerPost(Transform parent, Vector3 position, Material material)
    {
        Vector3 normal = GetGroundNormal(position.x, position.z);
        position.y = GetGroundY(position.x, position.z) + BoundaryGroundOffset;

        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Corner Marker";
        post.transform.SetParent(parent);
        post.transform.position = position;
        post.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
        post.transform.localScale = new Vector3(1.15f, BoundaryHeight * 2f, 1.15f);
        SetMaterial(post, material);
        DisableCollider(post);
    }

    private static GameObject CreateSign(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 faceTarget,
        string label,
        Material postMaterial,
        Material boardMaterial)
    {
        GameObject sign = new GameObject(name);
        sign.transform.SetParent(parent);
        sign.transform.position = position;

        Vector3 forward = faceTarget - position;
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.01f)
        {
            sign.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "Post";
        post.transform.SetParent(sign.transform);
        post.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        post.transform.localRotation = Quaternion.identity;
        post.transform.localScale = new Vector3(0.16f, 1.44f, 0.16f);
        SetMaterial(post, postMaterial);
        DisableCollider(post);

        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Board";
        board.transform.SetParent(sign.transform);
        board.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        board.transform.localRotation = Quaternion.identity;
        board.transform.localScale = new Vector3(2.35f, 0.88f, 0.14f);
        SetMaterial(board, boardMaterial);
        DisableCollider(board);

        CreateSignText(sign.transform, label, new Vector3(0f, 1.55f, 0.076f), Quaternion.Euler(0f, 180f, 0f));
        CreateSignText(sign.transform, label, new Vector3(0f, 1.55f, -0.076f), Quaternion.identity);

        return sign;
    }

    private static void CreateSignText(Transform parent, string label, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = localRotation;
        textObject.transform.localScale = Vector3.one;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = label;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.115f;
        text.fontSize = 52;
        text.lineSpacing = 0.86f;
        text.color = new Color(0.08f, 0.055f, 0.025f, 1f);
    }

    private static float GetGroundY(float worldX, float worldZ)
    {
        Terrain terrain = GetTerrainAt(worldX, worldZ);

        if (terrain == null)
        {
            return 51.08f;
        }

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        float normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldX);
        float normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldZ);
        return terrainPosition.y + terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
    }

    private static Vector3 GetGroundNormal(float worldX, float worldZ)
    {
        Terrain terrain = GetTerrainAt(worldX, worldZ);

        if (terrain == null)
        {
            return Vector3.up;
        }

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        float normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, worldX);
        float normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, worldZ);
        Vector3 localNormal = terrain.terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
        return terrain.transform.TransformDirection(localNormal).normalized;
    }

    private static Quaternion GetGroundAlignedRotation(Vector3 forward, Vector3 normal)
    {
        Vector3 projectedForward = Vector3.ProjectOnPlane(forward, normal);

        if (projectedForward.sqrMagnitude < 0.001f)
        {
            projectedForward = Vector3.ProjectOnPlane(Vector3.forward, normal);
        }

        return Quaternion.LookRotation(projectedForward.normalized, normal);
    }

    private static Terrain GetTerrainAt(float worldX, float worldZ)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain != null && IsInsideTerrain(terrain, worldX, worldZ))
            {
                return terrain;
            }
        }

        Terrain activeTerrain = Terrain.activeTerrain;
        return activeTerrain != null && IsInsideTerrain(activeTerrain, worldX, worldZ) ? activeTerrain : null;
    }

    private static bool IsInsideTerrain(Terrain terrain, float worldX, float worldZ)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        return worldX >= terrainPosition.x
            && worldX <= terrainPosition.x + terrainSize.x
            && worldZ >= terrainPosition.z
            && worldZ <= terrainPosition.z + terrainSize.z;
    }

    private static Material CreateMaterial(Color color, bool transparent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader)
        {
            color = color
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.18f);
        }

        if (transparent)
        {
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
        }

        return material;
    }

    private static void SetMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = material;
        }
    }

    private static void DisableCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = false;
        }
    }
}
