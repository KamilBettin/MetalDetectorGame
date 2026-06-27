using UnityEngine;

public static class TraderBootstrapper
{
    private const string TraderName = "TraderNPC";
    private const float NpcHeightOffset = 1f;
    private const float TraderInteractionDistance = 4.2f;
    private static readonly Vector2 FallbackHomePosition = new Vector2(-700f, -710f);
    private static readonly Vector3 TraderOffsetFromHome = new Vector3(6.8f, 0f, 6.2f);

    public static void EnsureTraderAtHome()
    {
        PlayerHome home = Object.FindAnyObjectByType<PlayerHome>();
        Transform trader = FindOrCreateTrader();

        if (trader == null)
        {
            return;
        }

        PlaceTraderNearHome(trader, home);
        EnsureTraderVisuals(trader);
        ConnectPrimaryShop(trader);
    }

    private static Transform FindOrCreateTrader()
    {
        GameObject traderObject = GameObject.Find(TraderName);

        if (traderObject != null)
        {
            return traderObject.transform;
        }

        traderObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        traderObject.name = TraderName;
        traderObject.transform.localScale = Vector3.one;

        Renderer renderer = traderObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = CreateMaterial(new Color(0.18f, 0.42f, 0.72f, 1f));
        }

        return traderObject.transform;
    }

    private static void PlaceTraderNearHome(Transform trader, PlayerHome home)
    {
        Vector3 homePosition = home != null
            ? home.transform.position
            : new Vector3(FallbackHomePosition.x, GetGroundY(FallbackHomePosition.x, FallbackHomePosition.y), FallbackHomePosition.y);
        Vector3 targetPosition = homePosition + TraderOffsetFromHome;
        targetPosition.y = GetGroundY(targetPosition.x, targetPosition.z) + NpcHeightOffset;
        trader.position = targetPosition;

        Vector3 lookDirection = homePosition - targetPosition;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            trader.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    private static void ConnectPrimaryShop(Transform trader)
    {
        UpgradeShop[] shops = Object.FindObjectsByType<UpgradeShop>();
        UpgradeShop primaryShop = FindPrimaryShop(shops);

        if (primaryShop == null)
        {
            PlayerInventory inventory = Object.FindAnyObjectByType<PlayerInventory>();

            if (inventory == null)
            {
                return;
            }

            primaryShop = inventory.gameObject.AddComponent<UpgradeShop>();
        }

        AssignShopReferences(primaryShop, trader);

        foreach (UpgradeShop shop in shops)
        {
            if (shop != null && shop != primaryShop && shop.gameObject == primaryShop.gameObject)
            {
                shop.shopNpc = null;
            }
        }
    }

    private static UpgradeShop FindPrimaryShop(UpgradeShop[] shops)
    {
        if (shops == null || shops.Length == 0)
        {
            return null;
        }

        foreach (UpgradeShop shop in shops)
        {
            if (shop != null && shop.shopNpc != null)
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

    private static void AssignShopReferences(UpgradeShop shop, Transform trader)
    {
        shop.shopNpc = trader;
        shop.interactionDistance = TraderInteractionDistance;

        if (shop.playerInventory == null)
        {
            shop.playerInventory = Object.FindAnyObjectByType<PlayerInventory>();
        }

        if (shop.metalDetector == null)
        {
            shop.metalDetector = Object.FindAnyObjectByType<MetalDetector>();
        }
    }

    private static void EnsureTraderVisuals(Transform trader)
    {
        Renderer renderer = trader.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = CreateMaterial(new Color(0.18f, 0.42f, 0.72f, 1f));
        }

        if (trader.Find("Trader Label") == null)
        {
            CreateTraderLabel(trader);
        }

        if (trader.Find("Upgrade Crate") == null)
        {
            CreateUpgradeCrate(trader);
        }
    }

    private static void CreateTraderLabel(Transform trader)
    {
        GameObject labelObject = new GameObject("Trader Label");
        labelObject.transform.SetParent(trader, false);
        labelObject.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        labelObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = "UPGRADES";
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = 0.18f;
        label.fontSize = 72;
        label.color = new Color(1f, 0.86f, 0.28f, 1f);
    }

    private static void CreateUpgradeCrate(Transform trader)
    {
        GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crate.name = "Upgrade Crate";
        crate.transform.SetParent(trader, false);
        crate.transform.localPosition = new Vector3(-0.95f, -0.45f, 0.25f);
        crate.transform.localScale = new Vector3(0.65f, 0.42f, 0.65f);

        Renderer renderer = crate.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material = CreateMaterial(new Color(0.45f, 0.26f, 0.11f, 1f));
        }

        Collider collider = crate.GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = false;
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
