using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class DefaultSearchAreaBootstrapper
{
    private const string BasicAreaName = "Basic Ground";
    private const string BasicAreaRootName = "Search Area - Basic Ground";
    private const string ForestAreaName = "Forest Grove";
    private const string ForestAreaRootName = "Search Area - Forest Grove";
    private const string StylizedSignTextResourcePath = "StylizedWoodenSign/source/StylizedWoodenSign_Text 1";
    private const string StylizedSignResourcePath = "StylizedWoodenSign/source/wooden sign";
    private const string StylizedSignMaterialResourcePath = "StylizedWoodenSign/Materials/StylizedWoodenSign";
    private const string AreaSignLabel = "Start\n$300";
    private const int BasicUnlockCost = 300;
    private const int ForestUnlockCost = 1000;
    private const float StylizedSignUprightPitch = 90f;
    private const float PlacedStylizedSignYaw = 155f;
    private const float BasicSignYaw = -60f;
    private const float ForestSignYaw = -240f;
    private const float StylizedSignGroundClearance = 0.03f;
    private const float SignOffsetOutsideArea = 2f;
    private const float SignTextSurfaceOffset = 0.024f;
    private const float AnchoredSignTextHeightRatio = 0.92f;
    private const float AnchoredSignTextMinHeight = 0.68f;
    private const float AnchoredSignTextMaxHeight = 1.18f;
    private const float BoundarySegmentLength = 1.25f;
    private const float BoundaryThickness = 0.42f;
    private const float BoundaryHeight = 0.035f;
    private const float BoundaryGroundOffset = 0.035f;

    private static readonly Vector2 BasicAreaCenter = new Vector2(-740f, -705f);
    private static readonly Vector2 BasicAreaSize = new Vector2(80f, 80f);
    private static readonly Vector2 BasicAreaMin = BasicAreaCenter - BasicAreaSize * 0.5f;
    private static readonly Vector2 BasicAreaMax = BasicAreaCenter + BasicAreaSize * 0.5f;
    private static readonly Vector2 BasicSignPosition = new Vector2(-699f, -711f);
    private static readonly Vector2 ForestAreaCenter = new Vector2(-575f, -730f);
    private static readonly Vector2 ForestAreaSize = new Vector2(110f, 80f);
    private static readonly Vector2 ForestSignPosition = new Vector2(-631f, -730f);
    private static bool isListeningForLanguageChanges;

    private struct TransformSnapshot
    {
        public Transform transform;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
    }

    public static void EnsureDefaultSearchAreas()
    {
        EnsureLanguageChangeListener();

        SearchArea basicArea = FindSearchArea(BasicAreaName);
        bool hasBasicArea = basicArea != null || GameObject.Find(BasicAreaRootName) != null;

        if (!hasBasicArea)
        {
            CreateBasicGroundArea();
        }
        else if (basicArea != null)
        {
            ConfigureExistingArea(
                basicArea,
                BasicAreaCenter,
                BasicAreaSize,
                BasicUnlockCost,
                TreasureLootPool.SpecialField,
                AreaSignLabel,
                new Color(1f, 0.72f, 0.18f, 0.62f),
                new Color(0.25f, 0.95f, 0.42f, 0.5f),
                BasicSignPosition);
        }

        SearchArea forestArea = FindSearchArea(ForestAreaName);
        bool hasForestArea = forestArea != null || GameObject.Find(ForestAreaRootName) != null;

        if (!hasForestArea)
        {
            CreateForestGroveArea();
        }
        else if (forestArea != null)
        {
            ConfigureExistingArea(
                forestArea,
                ForestAreaCenter,
                ForestAreaSize,
                ForestUnlockCost,
                TreasureLootPool.SpecialTreeField,
                GetForestSignLabel(),
                new Color(0.1f, 0.52f, 0.22f, 0.58f),
                new Color(0.22f, 0.95f, 0.42f, 0.46f),
                ForestSignPosition);
        }

        RefreshForestSignTexts();
    }

    public static int ConvertExistingSignTextsToTmp()
    {
        return 0;
    }

    private static void EnsurePlacedStylizedSignText()
    {
        GameObject placedSign = GameObject.Find("StylizedWoodenSign_Scene");

        if (placedSign == null)
        {
            return;
        }

        if (HasSignText(placedSign.transform))
        {
            return;
        }

        if (!HasTextAnchors(placedSign.transform))
        {
            placedSign.transform.rotation = Quaternion.Euler(StylizedSignUprightPitch, PlacedStylizedSignYaw, 0f);
        }

        if (ApplyAnchoredSignText(placedSign.transform, AreaSignLabel))
        {
            return;
        }

        if (placedSign.transform.Find("Sign Text Front") != null)
        {
            return;
        }

        CreateSignText(placedSign.transform, AreaSignLabel, new Vector3(0f, 0.09f, 1.55f), Quaternion.Euler(-StylizedSignUprightPitch, 0f, 0f));
    }

    private static SearchArea FindSearchArea(string areaName)
    {
        SearchArea[] searchAreas = Object.FindObjectsByType<SearchArea>();

        foreach (SearchArea area in searchAreas)
        {
            if (area != null && area.areaName == areaName)
            {
                return area;
            }
        }

        return null;
    }

    private static void EnsureLanguageChangeListener()
    {
        if (isListeningForLanguageChanges)
        {
            return;
        }

        GameLocalization.LanguageChanged += RefreshForestSignTexts;
        isListeningForLanguageChanges = true;
    }

    private static string GetForestSignLabel()
    {
        return GameLocalization.TFormat("area.forest_sign", ForestUnlockCost);
    }

    private static void RefreshForestSignTexts()
    {
        SearchArea forestArea = FindSearchArea(ForestAreaName);

        if (forestArea == null)
        {
            return;
        }

        SearchArea basicArea = FindSearchArea(BasicAreaName);
        TextMeshPro fieldTextStyle = basicArea != null
            ? FindSignText(FindDirectChildContaining(basicArea.transform, "Purchase Sign"))
                ?? FindSignText(FindDirectChildContaining(basicArea.transform, "Owned Sign"))
            : null;
        string label = GetForestSignLabel();
        ApplySignLabel(FindDirectChildContaining(forestArea.transform, "Purchase Sign"), label, fieldTextStyle);
        ApplySignLabel(FindDirectChildContaining(forestArea.transform, "Owned Sign"), label, fieldTextStyle);
    }

    private static TextMeshPro FindSignText(GameObject sign)
    {
        if (sign == null)
        {
            return null;
        }

        TextMeshPro[] signTexts = sign.GetComponentsInChildren<TextMeshPro>(true);

        foreach (TextMeshPro signText in signTexts)
        {
            if (signText != null && IsSignTextObject(signText.gameObject))
            {
                return signText;
            }
        }

        return null;
    }

    private static void ApplySignLabel(GameObject sign, string label, TextMeshPro styleSource)
    {
        if (sign == null)
        {
            return;
        }

        string normalizedLabel = NormalizeSignLabel(label);
        TextMeshPro[] signTexts = sign.GetComponentsInChildren<TextMeshPro>(true);

        foreach (TextMeshPro signText in signTexts)
        {
            if (signText != null && IsSignTextObject(signText.gameObject))
            {
                CopySignTextStyle(styleSource, signText);
                signText.text = normalizedLabel;
                signText.ForceMeshUpdate();
            }
        }
    }

    private static void CopySignTextStyle(TextMeshPro source, TextMeshPro target)
    {
        if (source == null || target == null || source == target)
        {
            return;
        }

        target.font = source.font;

        Material sourceMaterial = GetSafeSharedMaterial(source);

        if (sourceMaterial != null)
        {
            target.fontSharedMaterial = sourceMaterial;
        }

        target.alignment = source.alignment;
        target.fontStyle = source.fontStyle;
        target.fontSize = source.fontSize;
        target.enableAutoSizing = source.enableAutoSizing;
        target.fontSizeMin = source.fontSizeMin;
        target.fontSizeMax = source.fontSizeMax;
        target.textWrappingMode = source.textWrappingMode;
        target.overflowMode = source.overflowMode;
        target.lineSpacing = source.lineSpacing;
        target.characterSpacing = source.characterSpacing;
        target.extraPadding = source.extraPadding;
        target.enableCulling = source.enableCulling;
        target.color = source.color;
        target.margin = source.margin;
        target.rectTransform.sizeDelta = source.rectTransform.sizeDelta;
        target.transform.localPosition = source.transform.localPosition;
        target.transform.localRotation = source.transform.localRotation;
        target.transform.localScale = source.transform.localScale;

        MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
        MeshRenderer targetRenderer = target.GetComponent<MeshRenderer>();

        if (sourceRenderer != null && targetRenderer != null)
        {
            targetRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            targetRenderer.receiveShadows = sourceRenderer.receiveShadows;
            targetRenderer.sortingOrder = sourceRenderer.sortingOrder;
        }
    }

    private static void ConfigureExistingArea(
        SearchArea area,
        Vector2 center,
        Vector2 size,
        int unlockCost,
        TreasureLootPool lootPool,
        string signLabel,
        Color lockedColor,
        Color unlockedColor,
        Vector2? signPositionOverride = null)
    {
        bool wasUnlocked = area.isUnlocked;
        bool hasExistingVisuals = area.transform.childCount > 0;
        List<TransformSnapshot> preservedVisuals = hasExistingVisuals ? CaptureNonBoundaryChildTransforms(area.transform) : null;

        area.transform.position = new Vector3(center.x, GetGroundY(center.x, center.y), center.y);
        RestoreTransformSnapshots(preservedVisuals);

        area.size = size;
        area.unlockCost = unlockCost;
        area.lootPool = lootPool;

        if (hasExistingVisuals)
        {
            RepairExistingAreaVisuals(area, center, size, lockedColor, unlockedColor, wasUnlocked, signPositionOverride);
        }
        else if (!HasConfiguredAreaVisuals(area))
        {
            RebuildAreaVisuals(area, center, size, signLabel, lockedColor, unlockedColor, wasUnlocked, signPositionOverride);
        }
    }

    private static void CreateBasicGroundArea()
    {
        CreateSearchArea(
            BasicAreaRootName,
            BasicAreaName,
            BasicAreaMin,
            BasicAreaMax,
            BasicUnlockCost,
            TreasureLootPool.SpecialField,
            AreaSignLabel,
            new Color(0.35f, 0.9f, 0.45f, 0.28f),
            new Color(1f, 0.72f, 0.18f, 0.62f),
            new Color(0.25f, 0.95f, 0.42f, 0.5f),
            BasicSignPosition);
    }

    private static void CreateForestGroveArea()
    {
        Vector2 halfSize = ForestAreaSize * 0.5f;
        CreateSearchArea(
            ForestAreaRootName,
            ForestAreaName,
            ForestAreaCenter - halfSize,
            ForestAreaCenter + halfSize,
            ForestUnlockCost,
            TreasureLootPool.SpecialTreeField,
            GetForestSignLabel(),
            new Color(0.12f, 0.55f, 0.24f, 0.26f),
            new Color(0.1f, 0.52f, 0.22f, 0.58f),
            new Color(0.22f, 0.95f, 0.42f, 0.46f),
            ForestSignPosition);
    }

    private static void CreateSearchArea(
        string rootName,
        string areaName,
        Vector2 areaMin,
        Vector2 areaMax,
        int unlockCost,
        TreasureLootPool lootPool,
        string signLabel,
        Color areaColor,
        Color lockedColor,
        Color unlockedColor,
        Vector2? signPositionOverride = null)
    {
        Vector2 size = areaMax - areaMin;
        Vector2 center = (areaMin + areaMax) * 0.5f;
        Vector3 centerPosition = new Vector3(center.x, GetGroundY(center.x, center.y), center.y);

        GameObject areaObject = new GameObject(rootName);
        areaObject.transform.position = centerPosition;

        SearchArea area = areaObject.AddComponent<SearchArea>();
        area.areaName = areaName;
        area.unlockCost = unlockCost;
        area.isUnlocked = false;
        area.size = size;
        area.areaColor = areaColor;
        area.lootPool = lootPool;

        Material lockedMaterial = CreateMaterial(lockedColor, true);
        Material unlockedMaterial = CreateMaterial(unlockedColor, true);
        Material postMaterial = CreateMaterial(new Color(0.28f, 0.16f, 0.08f, 1f), false);
        Material boardMaterial = CreateMaterial(new Color(0.55f, 0.34f, 0.16f, 1f), false);

        GameObject lockedMarker = CreateAreaBoundary(areaName + " Locked Boundary", areaObject.transform, center, size, lockedMaterial);
        GameObject unlockedMarker = CreateAreaBoundary(areaName + " Unlocked Boundary", areaObject.transform, center, size, unlockedMaterial);
        unlockedMarker.SetActive(false);

        Vector3 signPosition = GetAreaSignPosition(center, size, signPositionOverride);
        GameObject purchaseSign = CreateSign(
            areaName + " Purchase Sign",
            areaObject.transform,
            signPosition,
            signLabel,
            postMaterial,
            boardMaterial,
            GetAreaSignYaw(areaName)
        );

        SearchAreaPurchasePoint purchasePoint = purchaseSign.AddComponent<SearchAreaPurchasePoint>();
        purchasePoint.targetArea = area;
        purchasePoint.interactionDistance = 8.5f;

        GameObject ownedSign = CreateSign(
            areaName + " Owned Sign",
            areaObject.transform,
            signPosition,
            signLabel,
            postMaterial,
            boardMaterial,
            GetAreaSignYaw(areaName)
        );
        ownedSign.SetActive(false);

        area.lockedObjects = new[] { lockedMarker, purchaseSign };
        area.unlockedObjects = new[] { unlockedMarker, ownedSign };

        Debug.Log("Created " + areaName + " search area at " + centerPosition + ".");
    }

    private static void RebuildAreaVisuals(
        SearchArea area,
        Vector2 center,
        Vector2 size,
        string signLabel,
        Color lockedColor,
        Color unlockedColor,
        bool isUnlocked,
        Vector2? signPositionOverride = null)
    {
        if (area == null)
        {
            return;
        }

        for (int i = area.transform.childCount - 1; i >= 0; i--)
        {
            DestroyGameObject(area.transform.GetChild(i).gameObject);
        }

        Material lockedMaterial = CreateMaterial(lockedColor, true);
        Material unlockedMaterial = CreateMaterial(unlockedColor, true);
        Material postMaterial = CreateMaterial(new Color(0.28f, 0.16f, 0.08f, 1f), false);
        Material boardMaterial = CreateMaterial(new Color(0.55f, 0.34f, 0.16f, 1f), false);

        GameObject lockedMarker = CreateAreaBoundary(area.areaName + " Locked Boundary", area.transform, center, size, lockedMaterial);
        GameObject unlockedMarker = CreateAreaBoundary(area.areaName + " Unlocked Boundary", area.transform, center, size, unlockedMaterial);

        Vector3 signPosition = GetAreaSignPosition(center, size, signPositionOverride);
        GameObject purchaseSign = CreateSign(
            area.areaName + " Purchase Sign",
            area.transform,
            signPosition,
            signLabel,
            postMaterial,
            boardMaterial,
            GetAreaSignYaw(area.areaName)
        );

        SearchAreaPurchasePoint purchasePoint = purchaseSign.AddComponent<SearchAreaPurchasePoint>();
        purchasePoint.targetArea = area;
        purchasePoint.interactionDistance = 8.5f;

        GameObject ownedSign = CreateSign(
            area.areaName + " Owned Sign",
            area.transform,
            signPosition,
            signLabel,
            postMaterial,
            boardMaterial,
            GetAreaSignYaw(area.areaName)
        );

        area.lockedObjects = new[] { lockedMarker, purchaseSign };
        area.unlockedObjects = new[] { unlockedMarker, ownedSign };

        lockedMarker.SetActive(!isUnlocked);
        purchaseSign.SetActive(!isUnlocked);
        unlockedMarker.SetActive(isUnlocked);
        ownedSign.SetActive(isUnlocked);
    }

    private static void RepairExistingAreaVisuals(
        SearchArea area,
        Vector2 center,
        Vector2 size,
        Color lockedColor,
        Color unlockedColor,
        bool isUnlocked,
        Vector2? signPositionOverride = null)
    {
        if (area == null)
        {
            return;
        }

        Material lockedMaterial = CreateMaterial(lockedColor, true);
        Material unlockedMaterial = CreateMaterial(unlockedColor, true);

        GameObject lockedBoundary = ReplaceAreaBoundary(area.transform, area.areaName + " Locked Boundary", center, size, lockedMaterial);
        GameObject unlockedBoundary = ReplaceAreaBoundary(area.transform, area.areaName + " Unlocked Boundary", center, size, unlockedMaterial);
        GameObject purchaseSign = FindDirectChildContaining(area.transform, "Purchase Sign");
        GameObject ownedSign = FindDirectChildContaining(area.transform, "Owned Sign");
        Vector3 signPosition = GetAreaSignPosition(center, size, signPositionOverride);
        bool shouldMoveExistingSigns = signPositionOverride.HasValue;

        if (purchaseSign != null)
        {
            SearchAreaPurchasePoint purchasePoint = purchaseSign.GetComponent<SearchAreaPurchasePoint>();

            if (purchasePoint == null)
            {
                purchasePoint = purchaseSign.AddComponent<SearchAreaPurchasePoint>();
            }

            purchasePoint.targetArea = area;
            purchasePoint.interactionDistance = 8.5f;

            if (shouldMoveExistingSigns)
            {
                PlaceExistingSign(purchaseSign.transform, signPosition, GetAreaSignYaw(area.areaName));
            }
        }

        if (ownedSign != null && shouldMoveExistingSigns)
        {
            PlaceExistingSign(ownedSign.transform, signPosition, GetAreaSignYaw(area.areaName));
        }

        area.lockedObjects = CompactObjects(lockedBoundary, purchaseSign);
        area.unlockedObjects = CompactObjects(unlockedBoundary, ownedSign);

        SetObjectsActive(area.lockedObjects, !isUnlocked);
        SetObjectsActive(area.unlockedObjects, isUnlocked);
    }

    private static GameObject ReplaceAreaBoundary(Transform parent, string boundaryName, Vector2 center, Vector2 size, Material material)
    {
        GameObject existingBoundary = FindDirectChildContaining(parent, boundaryName);

        if (existingBoundary == null)
        {
            string boundaryKind = boundaryName.Contains("Unlocked") ? "Unlocked Boundary" : "Locked Boundary";
            existingBoundary = FindDirectChildContaining(parent, boundaryKind);
        }

        if (existingBoundary != null)
        {
            DestroyGameObject(existingBoundary);
        }

        return CreateAreaBoundary(boundaryName, parent, center, size, material);
    }

    private static Vector3 GetAreaSignPosition(Vector2 center, Vector2 size, Vector2? signPositionOverride)
    {
        if (signPositionOverride.HasValue)
        {
            Vector2 signPosition = signPositionOverride.Value;
            return new Vector3(signPosition.x, GetGroundY(signPosition.x, signPosition.y), signPosition.y);
        }

        float z = center.y + size.y * 0.5f + SignOffsetOutsideArea;
        return new Vector3(center.x, GetGroundY(center.x, z), z);
    }

    private static float GetAreaSignYaw(string areaName)
    {
        return areaName == ForestAreaName ? ForestSignYaw : BasicSignYaw;
    }

    private static void PlaceExistingSign(Transform sign, Vector3 signPosition, float signYaw)
    {
        if (sign == null)
        {
            return;
        }

        sign.position = signPosition;
        sign.rotation = Quaternion.Euler(0f, signYaw, 0f);
    }

    private static GameObject[] CompactObjects(params GameObject[] objects)
    {
        List<GameObject> compactedObjects = new List<GameObject>();

        foreach (GameObject target in objects)
        {
            if (target != null)
            {
                compactedObjects.Add(target);
            }
        }

        return compactedObjects.ToArray();
    }

    private static void SetObjectsActive(GameObject[] objects, bool isActive)
    {
        if (objects == null)
        {
            return;
        }

        foreach (GameObject target in objects)
        {
            if (target != null)
            {
                target.SetActive(isActive);
            }
        }
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
        string label,
        Material postMaterial,
        Material boardMaterial,
        float signYaw)
    {
        GameObject sign = new GameObject(name);
        sign.transform.SetParent(parent);
        sign.transform.position = position;
        sign.transform.rotation = Quaternion.Euler(0f, signYaw, 0f);

        if (CreateStylizedSign(sign.transform, label))
        {
            return sign;
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

        return sign;
    }

    private static bool CreateStylizedSign(Transform parent, string label)
    {
        GameObject signPrefab = Resources.Load<GameObject>(StylizedSignTextResourcePath);
        bool usesTextAnchors = signPrefab != null;

        if (signPrefab == null)
        {
            signPrefab = Resources.Load<GameObject>(StylizedSignResourcePath);
        }

        if (signPrefab == null)
        {
            return false;
        }

        GameObject signModel = Object.Instantiate(signPrefab, parent);
        signModel.name = "Stylized Sign Model";
        signModel.transform.localPosition = Vector3.zero;
        signModel.transform.localScale = Vector3.one;

        if (!usesTextAnchors)
        {
            signModel.transform.localRotation = Quaternion.Euler(StylizedSignUprightPitch, 0f, 0f);
        }

        LiftRenderableToGround(signModel.transform, parent.position.y + StylizedSignGroundClearance);

        Material signMaterial = usesTextAnchors ? null : Resources.Load<Material>(StylizedSignMaterialResourcePath);

        if (!usesTextAnchors && signMaterial != null)
        {
            foreach (Renderer renderer in signModel.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = signMaterial;
                }

                renderer.sharedMaterials = materials;
            }
        }

        foreach (Collider collider in signModel.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        if (!ApplyAnchoredSignText(signModel.transform, label))
        {
            CreateSignText(parent, label, new Vector3(0f, 1.55f, 0.09f), Quaternion.identity);
        }

        return true;
    }

    private static void CreateSignText(Transform parent, string label, Vector3 localPosition, Quaternion localRotation)
    {
        bool facesBackward = Mathf.Abs(Mathf.DeltaAngle(localRotation.eulerAngles.y, 180f)) < 1f;
        GameObject textObject = new GameObject(facesBackward ? "Sign Text Back" : "Sign Text Front");
        textObject.transform.SetParent(parent);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = localRotation;
        textObject.transform.localScale = Vector3.one * 0.19f;

        TextMeshPro text = EnsureTextMeshPro(textObject);
        ConfigureSignText(text, label, new Vector2(8f, 3f), 1.35f);
    }

    private static bool ApplyAnchoredSignText(Transform signRoot, string label)
    {
        Transform textStart = FindDeepChild(signRoot, "Text");
        Transform textEnd = FindDeepChild(signRoot, "TextEnd");

        if (textStart == null || textEnd == null || textStart.parent == null || textStart.parent != textEnd.parent)
        {
            return false;
        }

        DestroyChild(signRoot, "Sign Text Front");
        DestroyChild(signRoot, "Sign Text Back");

        Transform textParent = textStart.parent;
        Vector3 delta = textEnd.localPosition - textStart.localPosition;

        if (delta.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        GameObject textObject = new GameObject("Sign Text Front");
        textObject.transform.SetParent(textParent);
        Vector2 textBoxSize = GetAnchoredTextBoxSize(delta);
        ApplyAnchoredTextTransform(textObject.transform, textStart, textEnd);

        TextMeshPro text = EnsureTextMeshPro(textObject);
        ConfigureSignText(text, label, textBoxSize, GetAnchoredFontSizeMax(textBoxSize), true);

        return true;
    }

    private static TextMeshPro EnsureTextMeshPro(GameObject textObject)
    {
        if (textObject == null)
        {
            return null;
        }

        if (textObject.GetComponent<MeshRenderer>() == null)
        {
            textObject.AddComponent<MeshRenderer>();
        }

        TextMeshPro text = textObject.GetComponent<TextMeshPro>();

        if (text == null)
        {
            text = textObject.AddComponent<TextMeshPro>();
        }

        EnsureTextFont(text);
        return text;
    }

    private static void EnsureTextFont(TextMeshPro text)
    {
        if (text == null)
        {
            return;
        }

        if (text.font == null)
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

            if (defaultFont == null)
            {
                defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            if (defaultFont != null)
            {
                text.font = defaultFont;
            }
        }

        if (GetSafeSharedMaterial(text) == null && text.font != null && text.font.material != null)
        {
            text.fontSharedMaterial = text.font.material;
        }
    }

    private static void ConfigureSignText(TextMeshPro text, string label, Vector2 textBoxSize, float fontSize, bool fitToBox = false)
    {
        if (text == null)
        {
            return;
        }

        text.text = NormalizeSignLabel(label);
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.fontSizeMin = fontSize;
        text.fontSizeMax = fontSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.lineSpacing = -8f;
        text.characterSpacing = 0f;
        text.extraPadding = true;
        text.enableCulling = false;
        text.color = new Color(1f, 0.86f, 0.42f, 1f);
        text.margin = Vector4.zero;
        text.rectTransform.sizeDelta = textBoxSize;
        ApplyReadableTmpMaterial(text);

        MeshRenderer renderer = text.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 30;
        }

        text.ForceMeshUpdate();
    }

    private static bool IsSignTextObject(GameObject textObject)
    {
        if (textObject == null)
        {
            return false;
        }

        return textObject.name == "Sign Text Front" || textObject.name == "Sign Text Back";
    }

    private static bool HasSignText(Transform root)
    {
        return FindDeepChild(root, "Sign Text Front") != null || FindDeepChild(root, "Sign Text Back") != null;
    }

    private static bool HasConfiguredAreaVisuals(SearchArea area)
    {
        if (area == null)
        {
            return false;
        }

        if (area.transform.childCount > 0)
        {
            return true;
        }

        if (area.lockedObjects == null || area.unlockedObjects == null)
        {
            return false;
        }

        if (area.lockedObjects.Length == 0 || area.unlockedObjects.Length == 0)
        {
            return false;
        }

        foreach (GameObject lockedObject in area.lockedObjects)
        {
            if (lockedObject == null)
            {
                return false;
            }
        }

        foreach (GameObject unlockedObject in area.unlockedObjects)
        {
            if (unlockedObject == null)
            {
                return false;
            }
        }

        return true;
    }

    private static List<TransformSnapshot> CaptureNonBoundaryChildTransforms(Transform parent)
    {
        if (parent == null)
        {
            return null;
        }

        List<TransformSnapshot> snapshots = new List<TransformSnapshot>();

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child == null || IsAreaBoundaryRoot(child))
            {
                continue;
            }

            snapshots.Add(new TransformSnapshot
            {
                transform = child,
                position = child.position,
                rotation = child.rotation,
                localScale = child.localScale
            });
        }

        return snapshots;
    }

    private static void RestoreTransformSnapshots(List<TransformSnapshot> snapshots)
    {
        if (snapshots == null)
        {
            return;
        }

        foreach (TransformSnapshot snapshot in snapshots)
        {
            if (snapshot.transform == null)
            {
                continue;
            }

            snapshot.transform.position = snapshot.position;
            snapshot.transform.rotation = snapshot.rotation;
            snapshot.transform.localScale = snapshot.localScale;
        }
    }

    private static bool IsAreaBoundaryRoot(Transform candidate)
    {
        return candidate != null && candidate.name.Contains("Boundary");
    }

    private static GameObject FindDirectChildContaining(Transform parent, string namePart)
    {
        if (parent == null || string.IsNullOrEmpty(namePart))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child != null && child.name.Contains(namePart))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static string NormalizeSignLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return AreaSignLabel;
        }

        string[] rawLines = label.Replace("\r", "\n").Split('\n');
        List<string> lines = new List<string>();

        foreach (string rawLine in rawLines)
        {
            string trimmedLine = rawLine.Trim();

            if (!string.IsNullOrEmpty(trimmedLine))
            {
                lines.Add(trimmedLine);
            }
        }

        return lines.Count > 0 ? string.Join("\n", lines) : AreaSignLabel;
    }

    private static bool TryApplyAnchoredTextLayout(Transform textTransform, out Vector2 textBoxSize)
    {
        textBoxSize = new Vector2(8f, 3f);

        if (textTransform == null || textTransform.parent == null)
        {
            return false;
        }

        Transform textStart = FindDeepChild(textTransform.parent, "Text");
        Transform textEnd = FindDeepChild(textTransform.parent, "TextEnd");

        if (textStart == null || textEnd == null || textStart.parent != textEnd.parent)
        {
            return false;
        }

        Vector3 delta = textEnd.localPosition - textStart.localPosition;

        if (delta.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        ApplyAnchoredTextTransform(textTransform, textStart, textEnd);
        textBoxSize = GetAnchoredTextBoxSize(delta);
        return true;
    }

    private static void ApplyAnchoredTextTransform(Transform textTransform, Transform textStart, Transform textEnd)
    {
        textTransform.localPosition = (textStart.localPosition + textEnd.localPosition) * 0.5f;
        textTransform.rotation = Quaternion.Euler(0f, textStart.rotation.eulerAngles.y, 0f);
        textTransform.localScale = Vector3.one;
        textTransform.position += textTransform.forward * SignTextSurfaceOffset;
    }

    private static Vector2 GetAnchoredTextBoxSize(Vector3 anchorDelta)
    {
        float width = Mathf.Max(0.01f, anchorDelta.magnitude);
        float height = Mathf.Clamp(width * AnchoredSignTextHeightRatio, AnchoredSignTextMinHeight, AnchoredSignTextMaxHeight);
        return new Vector2(width, height);
    }

    private static float GetAnchoredFontSizeMax(Vector2 textBoxSize)
    {
        return Mathf.Clamp(Mathf.Min(textBoxSize.x * 0.58f, textBoxSize.y * 0.72f), 0.32f, 0.82f);
    }

    private static void ApplyReadableTmpMaterial(TextMeshPro text)
    {
        if (text == null)
        {
            return;
        }

        EnsureTextFont(text);
        Material sourceMaterial = GetSafeSharedMaterial(text);

        if (sourceMaterial == null)
        {
            return;
        }

        Material readableMaterial = new Material(sourceMaterial);
        readableMaterial.name = "Readable Sign Text TMP";
        SetMaterialColor(readableMaterial, new Color(1f, 0.86f, 0.32f, 1f), "_FaceColor", "_Color");
        SetMaterialColor(readableMaterial, Color.black, "_OutlineColor", "_UnderlayColor");
        SetMaterialFloat(readableMaterial, 0.34f, "_OutlineWidth");
        SetMaterialFloat(readableMaterial, 0.62f, "_FaceDilate");
        SetMaterialFloat(readableMaterial, 0.18f, "_UnderlayOffsetX");
        SetMaterialFloat(readableMaterial, -0.18f, "_UnderlayOffsetY");
        SetMaterialFloat(readableMaterial, 0.08f, "_UnderlaySoftness");
        readableMaterial.EnableKeyword("OUTLINE_ON");
        readableMaterial.EnableKeyword("UNDERLAY_ON");
        text.fontSharedMaterial = readableMaterial;
    }

    private static Material GetSafeSharedMaterial(TextMeshPro text)
    {
        if (text == null)
        {
            return null;
        }

        try
        {
            if (text.fontSharedMaterial != null)
            {
                return text.fontSharedMaterial;
            }
        }
        catch (UnassignedReferenceException)
        {
        }

        return text.font != null ? text.font.material : null;
    }

    private static void SetMaterialColor(Material material, Color color, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }
    }

    private static void SetMaterialFloat(Material material, float value, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }
    }

    private static void LiftRenderableToGround(Transform root, float targetBottomY)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            return;
        }

        float lift = targetBottomY - bounds.min.y;
        root.position += Vector3.up * lift;
    }

    private static int GetLongestLineLength(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 1;
        }

        int longest = 0;
        int current = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                longest = Mathf.Max(longest, current);
                current = 0;
            }
            else
            {
                current++;
            }
        }

        return Mathf.Max(longest, current, 1);
    }

    private static bool HasTextAnchors(Transform signRoot)
    {
        return FindDeepChild(signRoot, "Text") != null && FindDeepChild(signRoot, "TextEnd") != null;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void DestroyChild(Transform root, string childName)
    {
        Transform child = FindDeepChild(root, childName);

        if (child != null)
        {
            DestroyGameObject(child.gameObject);
        }
    }

    private static void DestroyGameObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }

    private static void DestroyComponent(Component target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
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
