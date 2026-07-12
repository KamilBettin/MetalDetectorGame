using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HomeInteractionStation : MonoBehaviour
{
    public enum StationMode
    {
        Sleep,
        Crafting,
        Storage
    }

    public StationMode mode = StationMode.Crafting;
    public PlayerHome home;
    public Transform player;
    public float interactionDistance = 2.6f;
    public string promptText = "E - Use";

    private InteractionTargetHighlight interactionHighlight;
    private string stationMessage = "";
    private float stationMessageTimer;

    private void Update()
    {
        ResolveReferences();
        UpdateHighlight();

        if (stationMessageTimer > 0f)
        {
            stationMessageTimer -= Time.deltaTime;
        }

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame || !IsPlayerInRange() || !GameUIState.CanProcessGameplayInput)
        {
            return;
        }

        Activate();
    }

    private void OnGUI()
    {
        if (!IsPlayerInRange() || !GameUIState.CanProcessGameplayInput)
        {
            return;
        }

        GameGui.DrawToast(new Rect(Screen.width * 0.5f - 190f, Screen.height - 178f, 380f, 40f), GetPromptText());

        if (stationMessageTimer > 0f && !string.IsNullOrEmpty(stationMessage))
        {
            GameGui.DrawToast(new Rect(Screen.width * 0.5f - 190f, Screen.height - 226f, 380f, 40f), stationMessage);
        }
    }

    public bool IsPlayerInRange()
    {
        ResolveReferences();
        return player != null && Vector3.Distance(player.position, transform.position) <= interactionDistance;
    }

    private void OnDisable()
    {
        if (interactionHighlight != null)
        {
            interactionHighlight.SetTarget(null);
        }
    }

    public static bool AnyStationInteractionInRange()
    {
        HomeInteractionStation[] stations = FindObjectsByType<HomeInteractionStation>();

        foreach (HomeInteractionStation station in stations)
        {
            if (station != null && station.IsPlayerInRange())
            {
                return true;
            }
        }

        return false;
    }

    private void Activate()
    {
        ResolveReferences();

        if (home == null)
        {
            ShowStationMessage(GameLocalization.T("home.no_home"));
            return;
        }

        if (mode == StationMode.Sleep)
        {
            ShowStationMessage(home.SleepFromStation());
            return;
        }

        if (mode == StationMode.Storage)
        {
            ShowStationMessage(home.OpenStorageFromStation(transform, interactionDistance + 0.75f));
            return;
        }

        ShowStationMessage(home.OpenCraftingFromStation(transform, interactionDistance + 0.75f));
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            player = PlayerRigReferences.FindLocalPlayerTransform();
        }

        if (home == null)
        {
            home = FindAnyObjectByType<PlayerHome>();
        }
    }

    private void ShowStationMessage(string value)
    {
        stationMessage = value;
        stationMessageTimer = 2.8f;
    }

    private string GetPromptText()
    {
        if (mode == StationMode.Crafting && home != null && !home.IsCraftingUnlocked())
        {
            return GameLocalization.T("home.crafting_locked_prompt");
        }

        if (mode == StationMode.Sleep)
        {
            return GameLocalization.T("home.sleep_prompt");
        }

        if (mode == StationMode.Storage)
        {
            return GameLocalization.T("home.storage_prompt");
        }

        return mode == StationMode.Crafting
            ? GameLocalization.T("home.crafting_prompt")
            : promptText;
    }

    private void UpdateHighlight()
    {
        if (interactionHighlight == null)
        {
            interactionHighlight = gameObject.GetComponent<InteractionTargetHighlight>();

            if (interactionHighlight == null)
            {
                interactionHighlight = gameObject.AddComponent<InteractionTargetHighlight>();
            }
        }

        interactionHighlight.SetTarget(IsPlayerInRange() && GameUIState.CanProcessGameplayInput ? transform : null);
    }
}

public static class HomeInteractionStationBootstrapper
{
    private const string ChestBasePath = "Assets/Treasure chest closed/Materials and textures/treasure_chest_albedo.tga";
    private const string ChestNormalPath = "Assets/Treasure chest closed/Materials and textures/treasure_chest_normal.tga";
    private const string ChestMetalSmoothPath = "Assets/Treasure chest closed/Materials and textures/treasure_chest_metal+smoo.tga";
    private const string ChestOcclusionPath = "Assets/Treasure chest closed/Materials and textures/treasure_chest_occlusion.tga";
    private const string ChestBaseResourcePath = "TreasureChest/Textures/treasure_chest_albedo";
    private const string ChestNormalResourcePath = "TreasureChest/Textures/treasure_chest_normal";
    private const string ChestMetalSmoothResourcePath = "TreasureChest/Textures/treasure_chest_metal+smoo";
    private const string ChestOcclusionResourcePath = "TreasureChest/Textures/treasure_chest_occlusion";
    private static readonly string[] BedNames = { "BedDouble", "Bed", "bed" };
    private static readonly string[] WorkbenchNames = { "Workbench", "Work Bench", "workbench" };
    private static readonly string[] StorageChestNames = { "Treasure Chest", "TreasureChest", "Treasure chest closed", "treasure_chest_closed", "Storage Chest", "Chest", "treasure chest", "chest" };
    private static Material storageChestMaterial;

    public static void EnsureHomeInteriorStations()
    {
        Scene interiorScene = SceneManager.GetSceneByName(SceneTransitionManager.HomeInteriorSceneName);

        if (!interiorScene.IsValid() || !interiorScene.isLoaded)
        {
            return;
        }

        PlayerHome home = Object.FindAnyObjectByType<PlayerHome>();
        ConfigureStation(FindSceneTransform(interiorScene, BedNames), HomeInteractionStation.StationMode.Sleep, "E - Sleep", 2.8f, home);
        ConfigureStation(FindSceneTransform(interiorScene, WorkbenchNames), HomeInteractionStation.StationMode.Crafting, "E - Crafting", 2.8f, home);
        ConfigureStation(FindSceneTransform(interiorScene, StorageChestNames), HomeInteractionStation.StationMode.Storage, "E - Storage", 2.8f, home);
    }

    private static void ConfigureStation(Transform target, HomeInteractionStation.StationMode mode, string promptText, float interactionDistance, PlayerHome home)
    {
        if (target == null)
        {
            return;
        }

        HomeInteractionStation station = target.GetComponent<HomeInteractionStation>();

        if (station == null)
        {
            station = target.gameObject.AddComponent<HomeInteractionStation>();
        }

        station.mode = mode;
        station.promptText = promptText;
        station.interactionDistance = interactionDistance;
        station.home = home;

        if (mode == HomeInteractionStation.StationMode.Storage)
        {
            RepairStorageChestRenderers(target);
        }
    }

    private static Transform FindSceneTransform(Scene scene, string[] preferredNames)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);

        foreach (string preferredName in preferredNames)
        {
            foreach (Transform transform in transforms)
            {
                if (transform != null && transform.gameObject.scene == scene && transform.name == preferredName)
                {
                    return transform;
                }
            }
        }

        foreach (string preferredName in preferredNames)
        {
            foreach (Transform transform in transforms)
            {
                if (transform != null
                    && transform.gameObject.scene == scene
                    && transform.name.IndexOf(preferredName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return transform;
                }
            }
        }

        return null;
    }

    private static void RepairStorageChestRenderers(Transform chestRoot)
    {
        if (chestRoot == null)
        {
            return;
        }

        Renderer[] renderers = chestRoot.GetComponentsInChildren<Renderer>(true);
        Material material = GetStorageChestMaterial();

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.enabled = true;
            targetRenderer.forceRenderingOff = false;
            targetRenderer.allowOcclusionWhenDynamic = false;
            targetRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            targetRenderer.receiveShadows = true;

            Material[] materials = targetRenderer.sharedMaterials;

            if (materials == null || materials.Length == 0)
            {
                targetRenderer.sharedMaterial = material;
                continue;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            targetRenderer.sharedMaterials = materials;
        }
    }

    private static Material GetStorageChestMaterial()
    {
        if (storageChestMaterial != null)
        {
            return storageChestMaterial;
        }

        storageChestMaterial = new Material(GetLitShader())
        {
            name = "Runtime Home Storage Chest",
        };

        SetMaterialColor(storageChestMaterial, Color.white);
        SetFloat(storageChestMaterial, 0.7f, "_Metallic");
        SetFloat(storageChestMaterial, 0.5f, "_Smoothness", "_Glossiness");
        SetFloat(storageChestMaterial, 1f, "_OcclusionStrength");

        Texture2D baseTexture = LoadRuntimeTexture(ChestBaseResourcePath, ChestBasePath);
        Texture2D normalTexture = LoadRuntimeTexture(ChestNormalResourcePath, ChestNormalPath);
        Texture2D metalSmoothTexture = LoadRuntimeTexture(ChestMetalSmoothResourcePath, ChestMetalSmoothPath);
        Texture2D occlusionTexture = LoadRuntimeTexture(ChestOcclusionResourcePath, ChestOcclusionPath);

        if (baseTexture != null)
        {
            SetTexture(storageChestMaterial, baseTexture, "_BaseColorMap", "_BaseMap", "_MainTex");
        }

        if (normalTexture != null)
        {
            SetTexture(storageChestMaterial, normalTexture, "_NormalMap", "_BumpMap");
            storageChestMaterial.EnableKeyword("_NORMALMAP");
            storageChestMaterial.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
        }

        if (metalSmoothTexture != null)
        {
            SetTexture(storageChestMaterial, metalSmoothTexture, "_MaskMap", "_MetallicGlossMap");
            storageChestMaterial.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        if (occlusionTexture != null)
        {
            SetTexture(storageChestMaterial, occlusionTexture, "_OcclusionMap");
        }

        ConfigureOpaqueMaterial(storageChestMaterial);
        return storageChestMaterial;
    }

    private static Shader GetLitShader()
    {
        string pipelineName = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
            ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().FullName
            : "";

        if (pipelineName.Contains("HighDefinition"))
        {
            Shader hdrpShader = Shader.Find("HDRP/Lit");

            if (hdrpShader != null)
            {
                return hdrpShader;
            }
        }

        if (pipelineName.Contains("Universal"))
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");

            if (urpShader != null)
            {
                return urpShader;
            }
        }

        return Shader.Find("Standard")
            ?? Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("HDRP/Lit")
            ?? Shader.Find("Unlit/Color");
    }

    private static Texture2D LoadEditorTexture(string path)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#else
        return null;
#endif
    }

    private static Texture2D LoadRuntimeTexture(string resourcePath, string editorPath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);

        if (texture != null)
        {
            return texture;
        }

        return LoadEditorTexture(editorPath);
    }

    private static void ConfigureOpaqueMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 0f);
        }

        if (material.HasProperty("_SurfaceType"))
        {
            material.SetFloat("_SurfaceType", 0f);
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 0f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", 0f);
        }

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = -1;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private static void SetTexture(Material material, Texture texture, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }
    }

    private static void SetFloat(Material material, float value, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }
    }
}
