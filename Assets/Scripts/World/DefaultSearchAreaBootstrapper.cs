using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class DefaultSearchAreaBootstrapper
{
    private const string BasicAreaName = "Basic Ground";
    private const string BasicAreaRootName = "Search Area - Basic Ground";
    private const string StylizedSignTextResourcePath = "StylizedWoodenSign/source/StylizedWoodenSign_Text 1";
    private const string StylizedSignResourcePath = "StylizedWoodenSign/source/wooden sign";
    private const string StylizedSignMaterialResourcePath = "StylizedWoodenSign/Materials/StylizedWoodenSign";
    private const string AreaSignLabel = "Darmowa\nDupcia";
    private const float StylizedSignUprightPitch = 90f;
    private const float PlacedStylizedSignYaw = 155f;
    private const float StylizedSignGroundClearance = 0.03f;
    private const float SignOffsetOutsideArea = 2f;
    private const float SignTextSurfaceOffset = 0.024f;
    private const float AnchoredSignTextHeightRatio = 0.72f;
    private const float AnchoredSignTextMinHeight = 0.52f;
    private const float AnchoredSignTextMaxHeight = 0.9f;
    private const float BoundarySegmentLength = 1.25f;
    private const float BoundaryThickness = 0.42f;
    private const float BoundaryHeight = 0.035f;
    private const float BoundaryGroundOffset = 0.035f;

    private static readonly Vector2 BasicAreaMin = new Vector2(-770f, -730f);
    private static readonly Vector2 BasicAreaMax = new Vector2(-720f, -680f);

    public static void EnsureDefaultSearchAreas()
    {
        EnsurePlacedStylizedSignText();
        ConvertExistingSignTextsToTmp();

        bool hasBasicArea = HasSearchArea(BasicAreaName) || GameObject.Find(BasicAreaRootName) != null;

        if (!hasBasicArea)
        {
            CreateBasicGroundArea();
        }
    }

    public static int ConvertExistingSignTextsToTmp()
    {
        int convertedCount = 0;
        TextMesh[] legacyTexts = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include);

        foreach (TextMesh legacyText in legacyTexts)
        {
            if (legacyText == null || !IsSignTextObject(legacyText.gameObject))
            {
                continue;
            }

            GameObject textObject = legacyText.gameObject;
            Transform textTransform = textObject.transform;
            string label = NormalizeSignLabel(legacyText.text);

            if (!TryApplyAnchoredTextLayout(textTransform, out Vector2 textBoxSize))
            {
                float textScale = Mathf.Clamp(legacyText.characterSize * 1.8f, 0.075f, 0.18f);
                textTransform.localScale = Vector3.one * textScale;
                textTransform.position += textTransform.forward * SignTextSurfaceOffset;
                textBoxSize = new Vector2(8f, 3f);
            }

            DestroyComponent(legacyText);

            TextMeshPro text = EnsureTextMeshPro(textObject);

            ConfigureSignText(text, label, textBoxSize, 1f, true);
            convertedCount++;
        }

        TextMeshPro[] tmpTexts = Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include);

        foreach (TextMeshPro tmpText in tmpTexts)
        {
            if (tmpText == null || !IsSignTextObject(tmpText.gameObject))
            {
                continue;
            }

            if (TryApplyAnchoredTextLayout(tmpText.transform, out Vector2 textBoxSize))
            {
                ConfigureSignText(tmpText, tmpText.text, textBoxSize, GetAnchoredFontSizeMax(textBoxSize), true);
            }
        }

        return convertedCount;
    }

    private static void EnsurePlacedStylizedSignText()
    {
        GameObject placedSign = GameObject.Find("StylizedWoodenSign_Scene");

        if (placedSign == null)
        {
            return;
        }

        if (!HasTextAnchors(placedSign.transform))
        {
            placedSign.transform.rotation = Quaternion.Euler(StylizedSignUprightPitch, PlacedStylizedSignYaw, 0f);
        }

        Transform placedBackText = placedSign.transform.Find("Sign Text Back");

        if (placedBackText != null)
        {
            DestroyGameObject(placedBackText.gameObject);
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
            AreaSignLabel,
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
            AreaSignLabel,
            postMaterial,
            boardMaterial
        );
        ownedSign.SetActive(false);

        area.lockedObjects = new[] { lockedMarker, purchaseSign };
        area.unlockedObjects = new[] { unlockedMarker, ownedSign };

        Debug.Log("Created Basic Ground search area at " + centerPosition + ".");
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
        textObject.transform.localScale = Vector3.one * 0.16f;

        TextMeshPro text = EnsureTextMeshPro(textObject);
        ConfigureSignText(text, label, new Vector2(8f, 3f), 1f);
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
        text.enableAutoSizing = fitToBox;
        text.fontSizeMin = fitToBox ? Mathf.Max(0.05f, fontSize * 0.25f) : fontSize;
        text.fontSizeMax = fitToBox ? fontSize : fontSize;
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
        return Mathf.Clamp(Mathf.Min(textBoxSize.x * 0.42f, textBoxSize.y * 0.52f), 0.22f, 0.58f);
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
