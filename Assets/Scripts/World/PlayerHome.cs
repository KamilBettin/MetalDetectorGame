using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHome : MonoBehaviour
{
    public Transform interactionPoint;
    public Transform promptAnchor;
    public float interactionDistance = 4.5f;
    public PlayerInventory playerInventory;
    public Transform player;
    public DetectorBattery detectorBattery;

    private readonly List<PlayerInventory.InventorySlot> storedItems = new List<PlayerInventory.InventorySlot>();
    private bool isMenuOpen;
    private string message = "";
    private float messageTimer;

    public bool IsMenuOpen => isMenuOpen;
    public int StoredItemCount => storedItems.Count;
    public int StoredValue => GetStoredValue();
    public Vector3 PromptPosition => promptAnchor != null ? promptAnchor.position : transform.position + Vector3.up * 2f;

    private void Update()
    {
        ResolveReferences();

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && IsPlayerInRange())
        {
            if (!GameUIState.AnyMenuOpen || isMenuOpen)
            {
                SetMenuOpen(!isMenuOpen);
            }
        }

        if (isMenuOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetMenuOpen(false);
        }

        if (isMenuOpen && !IsPlayerInRange())
        {
            SetMenuOpen(false);
        }
    }

    public bool IsPlayerInRange()
    {
        ResolveReferences();

        if (player == null)
        {
            return false;
        }

        Vector3 targetPosition = interactionPoint != null ? interactionPoint.position : transform.position;
        return Vector3.Distance(player.position, targetPosition) <= interactionDistance;
    }

    public void SetMenuOpen(bool open)
    {
        isMenuOpen = open;
        GameUIState.SetHomeMenuOpen(isMenuOpen);
    }

    public void StoreBackpack()
    {
        if (playerInventory == null || playerInventory.items.Count == 0)
        {
            ShowMessage("Backpack is empty.");
            return;
        }

        int storedCount = playerInventory.items.Count;
        int storedValue = playerInventory.GetInventoryValue();

        foreach (PlayerInventory.InventorySlot item in playerInventory.items)
        {
            storedItems.Add(CloneItem(item));
        }

        playerInventory.items.Clear();
        ShowMessage("Stored " + storedCount + " item(s), value $" + storedValue + ".");
    }

    public void TakeStoredItems()
    {
        if (playerInventory == null)
        {
            ShowMessage("No backpack found.");
            return;
        }

        if (storedItems.Count == 0)
        {
            ShowMessage("Storage is empty.");
            return;
        }

        int movedCount = 0;
        int movedValue = 0;

        for (int i = storedItems.Count - 1; i >= 0; i--)
        {
            PlayerInventory.InventorySlot item = storedItems[i];

            if (!playerInventory.AddItem(item.itemName, item.value, item.width, item.height, item.icon))
            {
                continue;
            }

            movedCount++;
            movedValue += item.value;
            storedItems.RemoveAt(i);
        }

        if (movedCount == 0)
        {
            ShowMessage("Backpack is full.");
            return;
        }

        ShowMessage("Took " + movedCount + " item(s), value $" + movedValue + ".");
    }

    public void Sleep()
    {
        ResolveReferences();

        if (detectorBattery != null)
        {
            detectorBattery.charge = detectorBattery.maxCharge;
        }

        if (DayNightCycle.Instance != null)
        {
            if (!DayNightCycle.Instance.CanSleep)
            {
                ShowMessage("You can sleep after 20:00.");
                return;
            }

            DayNightCycle.Instance.SleepUntilMorning();
            ShowMessage("You slept until morning. Treasures reset.");
            return;
        }

        ShowMessage("You slept. Detector battery refilled.");
    }

    public static PlayerHome FindClosestHomeInRange()
    {
        PlayerHome[] homes = FindObjectsByType<PlayerHome>();
        PlayerHome closestHome = null;
        float closestDistance = float.MaxValue;

        foreach (PlayerHome home in homes)
        {
            if (home == null || !home.IsPlayerInRange())
            {
                continue;
            }

            Transform playerTransform = home.player;
            Vector3 targetPosition = home.interactionPoint != null ? home.interactionPoint.position : home.transform.position;
            float distance = playerTransform != null ? Vector3.Distance(playerTransform.position, targetPosition) : 0f;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHome = home;
            }
        }

        return closestHome;
    }

    public static bool AnyHomeInteractionInRange()
    {
        return FindClosestHomeInRange() != null;
    }

    private void ResolveReferences()
    {
        if (playerInventory == null)
        {
            playerInventory = FindAnyObjectByType<PlayerInventory>();
        }

        if (player == null && playerInventory != null)
        {
            player = playerInventory.transform;
        }

        if (detectorBattery == null)
        {
            detectorBattery = FindAnyObjectByType<DetectorBattery>();
        }
    }

    private int GetStoredValue()
    {
        int value = 0;

        foreach (PlayerInventory.InventorySlot item in storedItems)
        {
            value += item.value;
        }

        return value;
    }

    private PlayerInventory.InventorySlot CloneItem(PlayerInventory.InventorySlot item)
    {
        return new PlayerInventory.InventorySlot
        {
            itemName = item.itemName,
            value = item.value,
            icon = item.icon,
            width = item.width,
            height = item.height,
            gridX = item.gridX,
            gridY = item.gridY
        };
    }

    private void ShowMessage(string value)
    {
        message = value;
        messageTimer = 2.8f;
    }

    private void OnGUI()
    {
        if (isMenuOpen)
        {
            DrawHomeMenu();
            return;
        }

        if (IsPlayerInRange() && !GameUIState.AnyMenuOpen)
        {
            GameGui.DrawToast(new Rect(Screen.width * 0.5f - 190f, Screen.height - 178f, 380f, 40f), "E - Use home");
        }
    }

    private void DrawHomeMenu()
    {
        Rect panel = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 140f, 440f, 280f);
        GameGui.DrawPanel(panel, "Home");

        int backpackCount = playerInventory != null ? playerInventory.items.Count : 0;
        int backpackValue = playerInventory != null ? playerInventory.GetInventoryValue() : 0;
        GUI.Label(new Rect(panel.x + 18f, panel.y + 46f, panel.width - 36f, 24f), "Backpack: " + backpackCount + " item(s), $" + backpackValue, GameGui.LabelStyle);
        GUI.Label(new Rect(panel.x + 18f, panel.y + 72f, panel.width - 36f, 24f), "Storage: " + StoredItemCount + " item(s), $" + StoredValue, GameGui.LabelStyle);

        if (GameGui.Button(new Rect(panel.x + 18f, panel.y + 112f, panel.width - 36f, 38f), "Store backpack"))
        {
            StoreBackpack();
        }

        if (GameGui.Button(new Rect(panel.x + 18f, panel.y + 158f, panel.width - 36f, 38f), "Take stored items"))
        {
            TakeStoredItems();
        }

        if (GameGui.Button(new Rect(panel.x + 18f, panel.y + 204f, panel.width - 36f, 38f), "Sleep"))
        {
            Sleep();
        }

        GUI.Label(new Rect(panel.x + 18f, panel.y + 248f, panel.width - 36f, 22f), messageTimer > 0f ? message : "ESC - Close", GameGui.HintStyle);
    }
}

public static class PlayerHomeBootstrapper
{
    private const string HomeRootName = "Player Home";
    private static readonly Vector2 HomePosition = new Vector2(-700f, -710f);

    public static void EnsurePlayerHome()
    {
        if (Object.FindAnyObjectByType<PlayerHome>() != null || GameObject.Find(HomeRootName) != null)
        {
            return;
        }

        CreateHome();
    }

    private static void CreateHome()
    {
        float groundY = GetGroundY(HomePosition.x, HomePosition.y);
        GameObject homeObject = new GameObject(HomeRootName);
        homeObject.transform.position = new Vector3(HomePosition.x, groundY, HomePosition.y);
        homeObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        Material wallMaterial = CreateMaterial(new Color(0.46f, 0.28f, 0.14f, 1f));
        Material trimMaterial = CreateMaterial(new Color(0.28f, 0.16f, 0.08f, 1f));
        Material roofMaterial = CreateMaterial(new Color(0.22f, 0.09f, 0.055f, 1f));
        Material floorMaterial = CreateMaterial(new Color(0.36f, 0.29f, 0.2f, 1f));
        Material bedMaterial = CreateMaterial(new Color(0.58f, 0.18f, 0.15f, 1f));
        Material chestMaterial = CreateMaterial(new Color(0.52f, 0.32f, 0.13f, 1f));

        CreateCube(homeObject.transform, "Foundation", new Vector3(0f, 0.18f, 0f), Vector3.zero, new Vector3(8.6f, 0.36f, 6.3f), floorMaterial);
        CreateCube(homeObject.transform, "Back Wall", new Vector3(0f, 1.85f, 3f), Vector3.zero, new Vector3(8.4f, 3.25f, 0.28f), wallMaterial);
        CreateCube(homeObject.transform, "Left Wall", new Vector3(-4.05f, 1.85f, 0f), Vector3.zero, new Vector3(0.28f, 3.25f, 6f), wallMaterial);
        CreateCube(homeObject.transform, "Right Wall", new Vector3(4.05f, 1.85f, 0f), Vector3.zero, new Vector3(0.28f, 3.25f, 6f), wallMaterial);
        CreateCube(homeObject.transform, "Front Wall Left", new Vector3(-2.45f, 1.85f, -3f), Vector3.zero, new Vector3(3.2f, 3.25f, 0.28f), wallMaterial);
        CreateCube(homeObject.transform, "Front Wall Right", new Vector3(2.45f, 1.85f, -3f), Vector3.zero, new Vector3(3.2f, 3.25f, 0.28f), wallMaterial);
        CreateCube(homeObject.transform, "Door Frame Top", new Vector3(0f, 3.25f, -3f), Vector3.zero, new Vector3(1.65f, 0.45f, 0.3f), trimMaterial);
        CreateCube(homeObject.transform, "Door", new Vector3(0f, 1.38f, -3.16f), Vector3.zero, new Vector3(1.25f, 2.25f, 0.12f), trimMaterial);

        CreateCube(homeObject.transform, "Roof Left", new Vector3(-2.05f, 3.75f, 0f), new Vector3(0f, 0f, -24f), new Vector3(4.9f, 0.3f, 6.9f), roofMaterial);
        CreateCube(homeObject.transform, "Roof Right", new Vector3(2.05f, 3.75f, 0f), new Vector3(0f, 0f, 24f), new Vector3(4.9f, 0.3f, 6.9f), roofMaterial);
        CreateCube(homeObject.transform, "Roof Ridge", new Vector3(0f, 4.72f, 0f), Vector3.zero, new Vector3(0.35f, 0.25f, 6.95f), roofMaterial);

        CreateCube(homeObject.transform, "Storage Chest", new Vector3(2.35f, 0.82f, -3.85f), Vector3.zero, new Vector3(1.6f, 0.9f, 0.95f), chestMaterial);
        CreateCube(homeObject.transform, "Chest Lid", new Vector3(2.35f, 1.34f, -3.85f), Vector3.zero, new Vector3(1.7f, 0.18f, 1.05f), trimMaterial);
        CreateCube(homeObject.transform, "Bed", new Vector3(-2.35f, 0.72f, -3.82f), Vector3.zero, new Vector3(1.75f, 0.45f, 2.45f), bedMaterial);
        CreateCube(homeObject.transform, "Pillow", new Vector3(-2.35f, 1.05f, -4.55f), Vector3.zero, new Vector3(1.35f, 0.24f, 0.46f), CreateMaterial(new Color(0.86f, 0.78f, 0.62f, 1f)));

        Transform promptAnchor = CreateMarker(homeObject.transform, "Prompt Anchor", new Vector3(0f, 2.2f, -4.2f));
        Transform interactionPoint = CreateMarker(homeObject.transform, "Interaction Point", new Vector3(0f, 1f, -4.2f));

        CreateText(homeObject.transform, "HOME", new Vector3(0f, 3.25f, -3.25f), Quaternion.Euler(0f, 180f, 0f));

        PlayerHome home = homeObject.AddComponent<PlayerHome>();
        home.interactionPoint = interactionPoint;
        home.promptAnchor = promptAnchor;
        home.interactionDistance = 5.2f;

        Debug.Log("Created provisional player home at " + homeObject.transform.position + ".");
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.Euler(localEulerAngles);
        cube.transform.localScale = localScale;
        SetMaterial(cube, material);
        DisableCollider(cube);
        return cube;
    }

    private static Transform CreateMarker(Transform parent, string name, Vector3 localPosition)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        return marker.transform;
    }

    private static void CreateText(Transform parent, string label, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject textObject = new GameObject("Home Label");
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = localRotation;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.text = label;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.26f;
        text.fontSize = 80;
        text.color = new Color(0.98f, 0.9f, 0.62f, 1f);
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
            material.SetFloat("_Smoothness", 0.22f);
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
