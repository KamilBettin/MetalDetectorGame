using UnityEngine;

public static class TraderBootstrapper
{
    private const string LegacyTraderName = "TraderNPC";
    private const string UpgradeTraderName = "UpgradeNPC";
    private const string SellTraderName = "SellNPC";
    private const string VisualRootName = "UMA Visual";
    private const string FallbackVisualName = "Fallback Human";
    private const float TraderInteractionDistance = 4.2f;
    private const float GroundOffset = 0.03f;
    private const int SellTraderAppearanceSeed = 672145;

    private static readonly Vector3 UpgradeTraderPosition = new Vector3(-664.7f, 66.270f, -723.3f);
    private static readonly Vector3 SellTraderPosition = new Vector3(-668.7f, 67.311f, -732.5f);

    public static void EnsureTraderAtHome()
    {
        Transform upgradeTrader = FindOrCreateTrader(UpgradeTraderName, LegacyTraderName);
        Transform sellTrader = FindOrCreateTrader(SellTraderName, null);

        PlaceTrader(upgradeTrader, UpgradeTraderPosition, SellTraderPosition);
        PlaceTrader(sellTrader, SellTraderPosition, UpgradeTraderPosition);
        EnsureTraderVisuals(upgradeTrader, "UPGRADES", true, 381704);
        EnsureTraderVisuals(sellTrader, "SELL", false, SellTraderAppearanceSeed);
        ConnectShops(upgradeTrader, sellTrader);
    }

    private static Transform FindOrCreateTrader(string traderName, string legacyName)
    {
        GameObject traderObject = GameObject.Find(traderName);

        if (traderObject == null && !string.IsNullOrEmpty(legacyName))
        {
            traderObject = GameObject.Find(legacyName);

            if (traderObject != null)
            {
                traderObject.name = traderName;
            }
        }

        if (traderObject == null)
        {
            traderObject = new GameObject(traderName);
        }

        CapsuleCollider collider = traderObject.GetComponent<CapsuleCollider>();

        if (collider == null)
        {
            collider = traderObject.AddComponent<CapsuleCollider>();
        }

        collider.height = 2f;
        collider.radius = 0.45f;
        collider.center = new Vector3(0f, 1f, 0f);
        collider.isTrigger = true;

        Renderer renderer = traderObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.enabled = false;
        }

        return traderObject.transform;
    }

    private static void PlaceTrader(Transform trader, Vector3 position, Vector3 lookTarget)
    {
        if (trader == null)
        {
            return;
        }

        Vector3 groundedPosition = GetGroundedPosition(position);
        trader.position = groundedPosition;

        Vector3 lookDirection = lookTarget - groundedPosition;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            trader.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    private static void ConnectShops(Transform upgradeTrader, Transform sellTrader)
    {
        PlayerInventory inventory = PlayerRigReferences.FindLocalInventory();

        if (inventory == null)
        {
            return;
        }

        UpgradeShop upgradeShop = FindShop(false, true);
        UpgradeShop sellShop = FindShop(true, false);

        if (upgradeShop == null)
        {
            upgradeShop = FindLegacyCombinedShop();
        }

        if (upgradeShop == null)
        {
            upgradeShop = inventory.gameObject.AddComponent<UpgradeShop>();
        }

        ConfigureShop(upgradeShop, upgradeTrader, false, true, "Upgrade Trader");

        if (sellShop == null)
        {
            sellShop = inventory.gameObject.AddComponent<UpgradeShop>();
        }

        ConfigureShop(sellShop, sellTrader, true, false, "Sell Trader");
    }

    private static UpgradeShop FindShop(bool allowSelling, bool allowUpgrades)
    {
        UpgradeShop[] shops = Object.FindObjectsByType<UpgradeShop>(FindObjectsInactive.Include);

        foreach (UpgradeShop shop in shops)
        {
            if (shop != null && shop.allowSelling == allowSelling && shop.allowUpgrades == allowUpgrades)
            {
                return shop;
            }
        }

        return null;
    }

    private static UpgradeShop FindLegacyCombinedShop()
    {
        UpgradeShop[] shops = Object.FindObjectsByType<UpgradeShop>(FindObjectsInactive.Include);

        foreach (UpgradeShop shop in shops)
        {
            if (shop != null && shop.allowSelling && shop.allowUpgrades)
            {
                return shop;
            }
        }

        foreach (UpgradeShop shop in shops)
        {
            if (shop != null)
            {
                return shop;
            }
        }

        return null;
    }

    private static void ConfigureShop(UpgradeShop shop, Transform npc, bool allowSelling, bool allowUpgrades, string displayName)
    {
        if (shop == null)
        {
            return;
        }

        shop.shopNpc = npc;
        shop.allowSelling = allowSelling;
        shop.allowUpgrades = allowUpgrades;
        shop.shopDisplayName = displayName;
        shop.interactionDistance = TraderInteractionDistance;

        if (shop.playerInventory == null)
        {
            shop.playerInventory = PlayerRigReferences.FindLocalInventory();
        }

        if (shop.metalDetector == null)
        {
            shop.metalDetector = PlayerRigReferences.FindLocalMetalDetector();
        }
    }

    private static void EnsureTraderVisuals(Transform trader, string labelText, bool upgradeTrader, int randomSeed)
    {
        if (trader == null)
        {
            return;
        }

        Transform visualRoot = trader.Find(VisualRootName);

        if (visualRoot == null)
        {
            GameObject visualObject = new GameObject(VisualRootName);
            visualObject.transform.SetParent(trader, false);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = Vector3.one;
            visualRoot = visualObject.transform;
        }

        if (!HasRenderableVisual(visualRoot))
        {
            PlayerCharacterSelection.CharacterProfile profile = new PlayerCharacterSelection.CharacterProfile(
                PlayerCharacterSelection.CharacterGender.Male,
                randomSeed
            );
            profile.displayName = upgradeTrader ? "Upgrade Trader" : "Sell Trader";
            UmaCharacterFactory.TryCreateCharacter(visualRoot, profile, out _);
        }

        if (!HasRenderableVisual(visualRoot))
        {
            EnsureFallbackHuman(visualRoot, upgradeTrader);
        }

        ConfigureRenderers(visualRoot);
        RemoveLabels(trader);
        RemoveBlockProps(trader);
    }

    private static void EnsureFallbackHuman(Transform visualRoot, bool upgradeTrader)
    {
        if (visualRoot.Find(FallbackVisualName) != null)
        {
            return;
        }

        GameObject fallback = new GameObject(FallbackVisualName);
        fallback.transform.SetParent(visualRoot, false);
        fallback.transform.localPosition = Vector3.zero;
        fallback.transform.localRotation = Quaternion.identity;
        fallback.transform.localScale = Vector3.one;

        Material skinMaterial = CreateMaterial(upgradeTrader
            ? new Color(0.72f, 0.54f, 0.39f, 1f)
            : new Color(0.78f, 0.58f, 0.42f, 1f));
        Material clothesMaterial = CreateMaterial(upgradeTrader
            ? new Color(0.1f, 0.22f, 0.42f, 1f)
            : new Color(0.34f, 0.14f, 0.32f, 1f));

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(fallback.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        body.transform.localScale = new Vector3(0.48f, 0.72f, 0.48f);
        body.GetComponent<Renderer>().material = clothesMaterial;
        DisableCollider(body);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(fallback.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.82f, 0f);
        head.transform.localScale = new Vector3(0.34f, 0.34f, 0.34f);
        head.GetComponent<Renderer>().material = skinMaterial;
        DisableCollider(head);
    }

    private static void ConfigureRenderers(Transform visualRoot)
    {
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private static bool HasRenderableVisual(Transform root)
    {
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;

            if (skinnedMeshRenderer != null)
            {
                if (skinnedMeshRenderer.sharedMesh != null)
                {
                    return true;
                }

                continue;
            }

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();

            if (meshFilter == null || meshFilter.sharedMesh != null)
            {
                return true;
            }
        }

        return false;
    }

    private static void RemoveLabels(Transform trader)
    {
        DestroyChild(trader, "NPC Label");
        DestroyChild(trader, "Trader Label");
    }

    private static void DestroyObject(GameObject target)
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

    private static void RemoveBlockProps(Transform trader)
    {
        DestroyChild(trader, "Upgrade Crate");
        DestroyChild(trader, "Sell Crate");
    }

    private static void DestroyChild(Transform parent, string childName)
    {
        Transform child = parent != null ? parent.Find(childName) : null;

        if (child != null)
        {
            DestroyObject(child.gameObject);
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

    private static Vector3 GetGroundedPosition(Vector3 position)
    {
        Terrain terrain = GetTerrainAt(position.x, position.z);

        if (terrain != null)
        {
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            float normalizedX = Mathf.InverseLerp(terrainPosition.x, terrainPosition.x + terrainSize.x, position.x);
            float normalizedZ = Mathf.InverseLerp(terrainPosition.z, terrainPosition.z + terrainSize.z, position.z);
            position.y = terrainPosition.y + terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ) + GroundOffset;
        }

        return position;
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

    private static Material CreateMaterial(Color color)
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
            material.SetFloat("_Smoothness", 0.28f);
        }

        return material;
    }
}
