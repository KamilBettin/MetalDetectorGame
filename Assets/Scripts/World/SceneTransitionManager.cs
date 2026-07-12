using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class SceneTransitionManager : MonoBehaviour
{
    public const string MainWorldSceneName = "SampleScene";
    public const string HomeInteriorSceneName = "Scene";
    public const string HomeInteriorScenePath = "Assets/BK_AlchemistHouse/Scenes/Scene.unity";

    private static readonly Vector3 InteriorSpawnPosition = new Vector3(-0.9f, 1f, 5.55f);
    private static readonly Quaternion InteriorSpawnRotation = Quaternion.Euler(0f, -90f, 0f);
    private static readonly Vector3 InteriorExitPosition = new Vector3(0f, 1f, -4.45f);

    private static SceneTransitionManager instance;
    private static GameObject persistentPlayer;
    private static Vector3 worldReturnPosition;
    private static Quaternion worldReturnRotation = Quaternion.identity;
    private static bool hasWorldReturnPosition;
    private static bool isTransitioning;
    private static bool isPlayerInsideHomeInterior;
    private static ulong preparedInteriorSceneHandle;
    private static AsyncOperation interiorLoadOperation;
    private static readonly Dictionary<Material, Material> repairedMaterialCache = new Dictionary<Material, Material>();
    private static bool hasSavedWorldLighting;
    private static UnityEngine.Rendering.AmbientMode savedAmbientMode;
    private static Color savedAmbientLight;
    private static float savedAmbientIntensity;
    private static float savedReflectionIntensity;
    private static LightmapData[] savedLightmaps;
    private static LightmapsMode savedLightmapsMode;
    private static LightProbes savedLightProbes;
    private static readonly List<DirectionalLightState> savedDirectionalLights = new List<DirectionalLightState>();

    private const float FadeOutDuration = 0.28f;
    private const float FadeInDuration = 0.42f;
    private Canvas fadeCanvas;
    private CanvasGroup fadeCanvasGroup;

    private struct DirectionalLightState
    {
        public Light light;
        public float intensity;
        public float shadowStrength;
        public LightShadows shadows;

        public DirectionalLightState(Light light)
        {
            this.light = light;
            intensity = light != null ? light.intensity : 0f;
            shadowStrength = light != null ? light.shadowStrength : 1f;
            shadows = light != null ? light.shadows : LightShadows.None;
        }
    }

    public static bool IsHomeInteriorActive => isPlayerInsideHomeInterior || IsHomeInteriorScene(SceneManager.GetActiveScene());

    public static void EnsureExists()
    {
        if (instance != null)
        {
            return;
        }

        SceneTransitionManager existingManager = Object.FindAnyObjectByType<SceneTransitionManager>();

        if (existingManager != null)
        {
            instance = existingManager;
            Object.DontDestroyOnLoad(existingManager.gameObject);
            return;
        }

        GameObject managerObject = new GameObject("Scene Transition Manager");
        instance = managerObject.AddComponent<SceneTransitionManager>();
        Object.DontDestroyOnLoad(managerObject);
    }

    public static bool IsHomeInteriorScene(Scene scene)
    {
        return scene.IsValid() && scene.name == HomeInteriorSceneName;
    }

    public static bool IsHomeInteriorSceneName(string sceneName)
    {
        return sceneName == HomeInteriorSceneName;
    }

    public static void EnterHomeInterior(Vector3 returnPosition, Quaternion returnRotation)
    {
        EnsureExists();
        instance.BeginEnterHomeInterior(returnPosition, returnRotation);
    }

    public static void ExitHomeInterior()
    {
        EnsureExists();
        instance.BeginExitHomeInterior();
    }

    public static void EnsureInteriorExitPortal()
    {
        Scene interiorScene = GetHomeInteriorScene();

        if (!interiorScene.IsValid() || !interiorScene.isLoaded)
        {
            return;
        }

        PrepareHomeInteriorScene();

        if (Object.FindAnyObjectByType<ScenePortal>() != null)
        {
            return;
        }

        Transform exitDoor = FindInteriorExitDoor(interiorScene);

        if (exitDoor != null)
        {
            CreateDoorExitPortal(exitDoor);
            return;
        }

        GameObject portalObject = new GameObject("Home Interior Exit Portal");
        portalObject.transform.position = InteriorExitPosition;

        ScenePortal portal = portalObject.AddComponent<ScenePortal>();
        portal.mode = ScenePortal.PortalMode.ExitHomeInterior;
        portal.interactionDistance = 2.4f;
        portal.promptText = "E - Leave house";
        SceneManager.MoveGameObjectToScene(portalObject, interiorScene);
    }

    public static void PrepareHomeInteriorScene()
    {
        Scene interiorScene = GetHomeInteriorScene();

        if (!interiorScene.IsValid() || !interiorScene.isLoaded)
        {
            return;
        }

        ulong interiorSceneHandle = interiorScene.handle.GetRawData();

        if (preparedInteriorSceneHandle == interiorSceneHandle)
        {
            return;
        }

        RepairInteriorMaterialsForUrp(interiorScene);
        StabilizeInteriorScene(interiorScene);
        HomeInteractionStationBootstrapper.EnsureHomeInteriorStations();
        preparedInteriorSceneHandle = interiorSceneHandle;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildFadeOverlay();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        PreloadHomeInteriorIfNeeded();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void BeginEnterHomeInterior(Vector3 returnPosition, Quaternion returnRotation)
    {
        if (isTransitioning || isPlayerInsideHomeInterior)
        {
            return;
        }

        if (!CapturePersistentPlayer())
        {
            Debug.LogWarning("Cannot enter home interior because no local player was found.");
            return;
        }

        worldReturnPosition = returnPosition;
        worldReturnRotation = returnRotation;
        hasWorldReturnPosition = true;
        CloseTransientMenus();
        isTransitioning = true;
        StartCoroutine(EnterHomeInteriorTransition());
    }

    private void BeginExitHomeInterior()
    {
        if (isTransitioning)
        {
            return;
        }

        CapturePersistentPlayer();
        CloseTransientMenus();
        isTransitioning = true;
        StartCoroutine(ExitHomeInteriorTransition());
    }

    private void CompleteExitHomeInterior()
    {

        if (hasWorldReturnPosition)
        {
            MovePersistentPlayer(worldReturnPosition, worldReturnRotation);
        }

        isPlayerInsideHomeInterior = false;
        hasWorldReturnPosition = false;
        Scene mainScene = SceneManager.GetSceneByName(MainWorldSceneName);

        if (mainScene.IsValid() && mainScene.isLoaded)
        {
            SceneManager.SetActiveScene(mainScene);
        }

        SetHomeInteriorVisible(false);
        RestoreWorldLightingEnvironment();
    }

    private bool CapturePersistentPlayer()
    {
        if (persistentPlayer != null)
        {
            return true;
        }

        Transform player = PlayerRigReferences.FindLocalPlayerTransform();

        if (player == null)
        {
            return false;
        }

        persistentPlayer = player.gameObject;
        DontDestroyOnLoad(persistentPlayer);
        return true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CapturePersistentPlayer();
        DestroyDuplicatePlayers();

        if (IsHomeInteriorScene(scene))
        {
            PrepareHomeInteriorScene();
            EnsureInteriorExitPortal();

            if (mode == LoadSceneMode.Single)
            {
                EnterLoadedHomeInterior(scene);
            }
            else
            {
                SetHomeInteriorVisible(isPlayerInsideHomeInterior);
            }

            return;
        }

        if (scene.name == MainWorldSceneName && hasWorldReturnPosition)
        {
            MovePersistentPlayer(worldReturnPosition, worldReturnRotation);
            DisableNonPlayerCameras();
        }

        if (scene.name == MainWorldSceneName)
        {
            PreloadHomeInteriorIfNeeded();
        }
    }

    private IEnumerator EnterHomeInteriorTransition()
    {
        yield return FadeScreen(1f, FadeOutDuration);

        Scene interiorScene = GetHomeInteriorScene();

        if (!interiorScene.IsValid() || !interiorScene.isLoaded)
        {
            AsyncOperation loadOperation = PreloadHomeInteriorIfNeeded();

            while (loadOperation != null && !loadOperation.isDone)
            {
                yield return null;
            }

            interiorScene = GetHomeInteriorScene();
        }

        if (interiorScene.IsValid() && interiorScene.isLoaded)
        {
            EnterLoadedHomeInterior(interiorScene);
            yield return null;
        }
        else
        {
            Debug.LogWarning("Cannot enter home interior because the interior scene did not load.");
        }

        yield return FadeScreen(0f, FadeInDuration);
        isTransitioning = false;
    }

    private IEnumerator ExitHomeInteriorTransition()
    {
        yield return FadeScreen(1f, FadeOutDuration);
        CompleteExitHomeInterior();
        yield return null;
        yield return FadeScreen(0f, FadeInDuration);
        isTransitioning = false;
    }

    private AsyncOperation PreloadHomeInteriorIfNeeded()
    {
        if (!SceneManager.GetSceneByName(MainWorldSceneName).isLoaded || GetHomeInteriorScene().isLoaded)
        {
            return null;
        }

        if (interiorLoadOperation != null && !interiorLoadOperation.isDone)
        {
            return interiorLoadOperation;
        }

        interiorLoadOperation = SceneManager.LoadSceneAsync(HomeInteriorScenePath, LoadSceneMode.Additive);

        if (interiorLoadOperation == null)
        {
            Debug.LogWarning("Could not start additive load for home interior scene.");
        }

        return interiorLoadOperation;
    }

    private void EnterLoadedHomeInterior(Scene interiorScene)
    {
        isPlayerInsideHomeInterior = true;
        SetHomeInteriorVisible(true);
        PrepareHomeInteriorScene();
        EnsureInteriorExitPortal();
        ApplyInteriorLightingEnvironment();
        MovePersistentPlayer(InteriorSpawnPosition, InteriorSpawnRotation);
        DisableNonPlayerCameras();

        if (interiorScene.IsValid() && interiorScene.isLoaded)
        {
            Scene mainScene = SceneManager.GetSceneByName(MainWorldSceneName);

            if (!mainScene.IsValid() || !mainScene.isLoaded)
            {
                SceneManager.SetActiveScene(interiorScene);
            }
        }
    }

    private void BuildFadeOverlay()
    {
        if (fadeCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("Scene Fade Canvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        fadeCanvas = canvasObject.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 20000;
        fadeCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;

        GameObject fadeObject = new GameObject("Scene Fade", typeof(RectTransform));
        fadeObject.transform.SetParent(canvasObject.transform, false);
        RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        Image fadeImage = fadeObject.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;
        fadeCanvas.enabled = false;
    }

    private IEnumerator FadeScreen(float targetAlpha, float duration)
    {
        if (fadeCanvas == null || fadeCanvasGroup == null)
        {
            BuildFadeOverlay();
        }

        fadeCanvas.enabled = true;
        fadeCanvasGroup.blocksRaycasts = true;
        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            progress = progress * progress * (3f - 2f * progress);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvas.enabled = false;
        }
    }

    private static Scene GetHomeInteriorScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (IsHomeInteriorScene(activeScene))
        {
            return activeScene;
        }

        return SceneManager.GetSceneByName(HomeInteriorSceneName);
    }

    private static void SetHomeInteriorVisible(bool visible)
    {
        Scene interiorScene = GetHomeInteriorScene();

        if (!interiorScene.IsValid() || !interiorScene.isLoaded)
        {
            return;
        }

        GameObject[] rootObjects = interiorScene.GetRootGameObjects();

        foreach (GameObject rootObject in rootObjects)
        {
            if (rootObject != null)
            {
                rootObject.SetActive(visible);
            }
        }
    }

    private static void ApplyInteriorLightingEnvironment()
    {
        if (!hasSavedWorldLighting)
        {
            savedAmbientMode = RenderSettings.ambientMode;
            savedAmbientLight = RenderSettings.ambientLight;
            savedAmbientIntensity = RenderSettings.ambientIntensity;
            savedReflectionIntensity = RenderSettings.reflectionIntensity;
            savedLightmaps = LightmapSettings.lightmaps;
            savedLightmapsMode = LightmapSettings.lightmapsMode;
            savedLightProbes = LightmapSettings.lightProbes;
            savedDirectionalLights.Clear();

            Light[] lights = Object.FindObjectsByType<Light>();

            foreach (Light light in lights)
            {
                if (light != null && light.type == LightType.Directional)
                {
                    savedDirectionalLights.Add(new DirectionalLightState(light));
                }
            }

            hasSavedWorldLighting = true;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.13f, 0.12f, 0.105f, 1f);
        RenderSettings.ambientIntensity = 0.40f;
        RenderSettings.reflectionIntensity = 0.10f;
        LightmapSettings.lightmaps = new LightmapData[0];
        LightmapSettings.lightProbes = null;

        for (int i = 0; i < savedDirectionalLights.Count; i++)
        {
            Light light = savedDirectionalLights[i].light;

            if (light == null)
            {
                continue;
            }

            light.intensity = 0.18f;
            light.shadowStrength = 0.2f;
            light.shadows = LightShadows.None;
        }
    }

    private static void RestoreWorldLightingEnvironment()
    {
        if (!hasSavedWorldLighting)
        {
            return;
        }

        RenderSettings.ambientMode = savedAmbientMode;
        RenderSettings.ambientLight = savedAmbientLight;
        RenderSettings.ambientIntensity = savedAmbientIntensity;
        RenderSettings.reflectionIntensity = savedReflectionIntensity;
        LightmapSettings.lightmaps = savedLightmaps;
        LightmapSettings.lightmapsMode = savedLightmapsMode;
        LightmapSettings.lightProbes = savedLightProbes;

        for (int i = 0; i < savedDirectionalLights.Count; i++)
        {
            DirectionalLightState state = savedDirectionalLights[i];

            if (state.light == null)
            {
                continue;
            }

            state.light.intensity = state.intensity;
            state.light.shadowStrength = state.shadowStrength;
            state.light.shadows = state.shadows;
        }

        savedDirectionalLights.Clear();
        hasSavedWorldLighting = false;

        DayNightCycle dayNightCycle = DayNightCycle.Instance;

        if (dayNightCycle != null)
        {
            dayNightCycle.RefreshLighting();
        }
    }

    private static Transform FindInteriorExitDoor(Scene interiorScene)
    {
        Transform fallbackDoor = null;
        GameObject[] rootObjects = interiorScene.GetRootGameObjects();

        foreach (GameObject rootObject in rootObjects)
        {
            if (rootObject == null)
            {
                continue;
            }

            Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(true);

            foreach (Transform transform in transforms)
            {
                if (transform == null || transform.GetComponentInChildren<Renderer>(true) == null)
                {
                    continue;
                }

                string objectName = transform.name;

                if (objectName == "Door (1)")
                {
                    return transform;
                }

                if (objectName == "Door")
                {
                    fallbackDoor = transform;
                }
            }
        }

        return fallbackDoor;
    }

    private static void CreateDoorExitPortal(Transform doorTransform)
    {
        Renderer[] doorRenderers = doorTransform.GetComponentsInChildren<Renderer>(true);
        GameObject portalObject = new GameObject("Home Interior Door Exit Portal");
        portalObject.transform.SetParent(doorTransform, false);
        portalObject.transform.position = GetRendererCenter(doorRenderers, doorTransform.position);

        ScenePortal portal = portalObject.AddComponent<ScenePortal>();
        portal.mode = ScenePortal.PortalMode.ExitHomeInterior;
        portal.interactionDistance = 2.25f;
        portal.promptText = "E - Leave house";
        portal.promptAnchor = portalObject.transform;
        portal.highlightRenderers = doorRenderers;
        portal.highlightColor = new Color(0.2f, 1f, 0.35f, 1f);
    }

    private static Vector3 GetRendererCenter(Renderer[] renderers, Vector3 fallbackPosition)
    {
        bool hasBounds = false;
        Bounds combinedBounds = new Bounds(fallbackPosition, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            combinedBounds.Encapsulate(renderer.bounds);
        }

        return hasBounds ? combinedBounds.center : fallbackPosition;
    }

    private static void DestroyDuplicatePlayers()
    {
        if (persistentPlayer == null)
        {
            return;
        }

        FirstPersonController[] controllers = Object.FindObjectsByType<FirstPersonController>();

        foreach (FirstPersonController controller in controllers)
        {
            if (controller != null && controller.gameObject != persistentPlayer)
            {
                Destroy(controller.gameObject);
            }
        }
    }

    private static void MovePersistentPlayer(Vector3 position, Quaternion rotation)
    {
        if (persistentPlayer == null)
        {
            return;
        }

        CharacterController controller = persistentPlayer.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;

        if (controller != null)
        {
            controller.enabled = false;
        }

        persistentPlayer.transform.SetPositionAndRotation(position, rotation);

        if (controller != null)
        {
            controller.enabled = controllerWasEnabled;
        }
    }

    private static void DisableNonPlayerCameras()
    {
        if (persistentPlayer == null)
        {
            return;
        }

        Camera playerCamera = persistentPlayer.GetComponentInChildren<Camera>(true);
        Camera[] cameras = Object.FindObjectsByType<Camera>();

        foreach (Camera camera in cameras)
        {
            if (camera == null || camera == playerCamera || camera.transform.IsChildOf(persistentPlayer.transform))
            {
                continue;
            }

            camera.enabled = false;

            AudioListener listener = camera.GetComponent<AudioListener>();

            if (listener != null)
            {
                listener.enabled = false;
            }
        }
    }

    private static void CloseTransientMenus()
    {
        GameUIState.SetHomeMenuOpen(false);
        GameUIState.SetTraderMenuOpen(false);
        GameUIState.SetQuestMenuOpen(false);
        GameUIState.SetInventoryOpen(false);
    }

    private static void RepairInteriorMaterialsForUrp(Scene interiorScene)
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");

        if (litShader == null)
        {
            return;
        }

        Renderer[] renderers = Object.FindObjectsByType<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.gameObject.scene != interiorScene)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (!NeedsUrpRepair(material))
                {
                    continue;
                }

                materials[i] = GetRepairedMaterial(material, litShader);
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }
        }
    }

    private static bool NeedsUrpRepair(Material material)
    {
        if (material == null)
        {
            return false;
        }

        Shader shader = material.shader;

        if (shader == null || !shader.isSupported)
        {
            return true;
        }

        string shaderName = shader.name;

        return shaderName == "Standard"
            || shaderName.StartsWith("Custom/")
            || shaderName.Contains("Refraction")
            || shaderName.Contains("Volumetric")
            || shaderName.StartsWith("Hidden/");
    }

    private static Material GetRepairedMaterial(Material source, Shader litShader)
    {
        if (repairedMaterialCache.TryGetValue(source, out Material repairedMaterial) && repairedMaterial != null)
        {
            return repairedMaterial;
        }

        repairedMaterial = new Material(litShader)
        {
            name = source.name + " (URP Runtime)",
        };

        Color baseColor = GetColor(source, Color.white, "_BaseColor", "_Color", "_TintColor");
        Texture baseTexture = GetTexture(source, "_BaseMap", "_MainTex", "_Albedo", "_TextureSample0");
        Texture normalTexture = GetTexture(source, "_BumpMap", "_NormalMap");
        Texture metallicTexture = GetTexture(source, "_MetallicGlossMap", "_MetallicMapGlossA", "_MetallicMap");

        SetColor(repairedMaterial, baseColor, "_BaseColor", "_Color");
        SetTexture(repairedMaterial, baseTexture, "_BaseMap", "_MainTex");
        SetTexture(repairedMaterial, normalTexture, "_BumpMap");
        SetTexture(repairedMaterial, metallicTexture, "_MetallicGlossMap");

        if (normalTexture != null)
        {
            repairedMaterial.EnableKeyword("_NORMALMAP");
        }

        if (metallicTexture != null)
        {
            repairedMaterial.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        SetFloat(repairedMaterial, 0f, "_Metallic");
        SetFloat(repairedMaterial, Mathf.Min(GetFloat(source, 0.12f, "_Smoothness", "_Glossiness", "_Gloss"), 0.18f), "_Smoothness", "_Glossiness");
        SetFloat(repairedMaterial, 0f, "_SpecularHighlights");
        SetFloat(repairedMaterial, 0f, "_EnvironmentReflections");

        if (ShouldBeTransparent(source, baseColor))
        {
            ConfigureTransparentMaterial(repairedMaterial);
        }
        else
        {
            ConfigureOpaqueMaterial(repairedMaterial);
        }

        repairedMaterialCache[source] = repairedMaterial;
        return repairedMaterial;
    }

    private static bool ShouldBeTransparent(Material source, Color baseColor)
    {
        string materialName = source.name.ToLowerInvariant();

        return baseColor.a < 0.95f
            || source.renderQueue >= 3000
            || materialName.Contains("glass")
            || materialName.Contains("liquid")
            || materialName.Contains("window")
            || source.IsKeywordEnabled("_ALPHABLEND_ON")
            || source.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        SetFloat(material, 1f, "_Surface");
        SetFloat(material, 0f, "_Blend");
        SetFloat(material, 0f, "_ZWrite");
        SetFloat(material, 0f, "_AlphaClip");
        SetFloat(material, (float)UnityEngine.Rendering.BlendMode.SrcAlpha, "_SrcBlend");
        SetFloat(material, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, "_DstBlend");
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = 3000;
    }

    private static void ConfigureOpaqueMaterial(Material material)
    {
        SetFloat(material, 0f, "_Surface");
        SetFloat(material, 1f, "_ZWrite");
        material.SetOverrideTag("RenderType", "Opaque");
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = -1;
    }

    private static Color GetColor(Material material, Color fallback, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetColor(propertyName);
            }
        }

        return fallback;
    }

    private static Texture GetTexture(Material material, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
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

    private static float GetFloat(Material material, float fallback, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetFloat(propertyName);
            }
        }

        return fallback;
    }

    private static void SetColor(Material material, Color value, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }
    }

    private static void SetTexture(Material material, Texture value, params string[] propertyNames)
    {
        if (value == null)
        {
            return;
        }

        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, value);
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

    private static void StabilizeInteriorScene(Scene interiorScene)
    {
        DisableInteriorDemoCameras(interiorScene);
        DisableInteriorAudio(interiorScene);
        DisableUnsupportedInteriorEffects(interiorScene);
        DisableInteriorPostProcessing(interiorScene);
        DisableInteriorReflectionProbes(interiorScene);
        SimplifyInteriorLights(interiorScene);
    }

    private static void DisableInteriorDemoCameras(Scene interiorScene)
    {
        Camera playerCamera = persistentPlayer != null ? persistentPlayer.GetComponentInChildren<Camera>(true) : null;
        Camera[] cameras = Object.FindObjectsByType<Camera>();

        foreach (Camera camera in cameras)
        {
            if (camera == null || camera.gameObject.scene != interiorScene || camera == playerCamera)
            {
                continue;
            }

            camera.enabled = false;

            AudioListener listener = camera.GetComponent<AudioListener>();

            if (listener != null)
            {
                listener.enabled = false;
            }
        }
    }

    private static void DisableInteriorAudio(Scene interiorScene)
    {
        AudioSource[] audioSources = Object.FindObjectsByType<AudioSource>();

        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource != null && audioSource.gameObject.scene == interiorScene)
            {
                audioSource.enabled = false;
            }
        }
    }

    private static void DisableUnsupportedInteriorEffects(Scene interiorScene)
    {
        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour.gameObject.scene != interiorScene)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;

            if (typeName == "FreeCamera"
                || typeName == "VolumetricLight"
                || typeName == "VolumetricLightRenderer"
                || typeName == "EventSystem"
                || typeName == "StandaloneInputModule"
                || typeName == "InputSystemUIInputModule")
            {
                behaviour.enabled = false;
            }
        }
    }

    private static void DisableInteriorPostProcessing(Scene interiorScene)
    {
        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour.gameObject.scene != interiorScene)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;

            if (typeName == "Volume" || typeName.Contains("PostProcess"))
            {
                behaviour.enabled = false;
            }
        }
    }

    private static void DisableInteriorReflectionProbes(Scene interiorScene)
    {
        ReflectionProbe[] probes = Object.FindObjectsByType<ReflectionProbe>();

        foreach (ReflectionProbe probe in probes)
        {
            if (probe != null && probe.gameObject.scene == interiorScene)
            {
                probe.enabled = false;
            }
        }
    }

    private static void SimplifyInteriorLights(Scene interiorScene)
    {
        Light[] lights = Object.FindObjectsByType<Light>();

        foreach (Light light in lights)
        {
            if (light == null || light.gameObject.scene != interiorScene)
            {
                continue;
            }

            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForceVertex;
        }
    }
}

public class ScenePortal : MonoBehaviour
{
    public enum PortalMode
    {
        ExitHomeInterior
    }

    public PortalMode mode = PortalMode.ExitHomeInterior;
    public float interactionDistance = 2.4f;
    public string promptText = "E - Leave home";
    public Transform promptAnchor;
    public Renderer[] highlightRenderers;
    public Color highlightColor = new Color(0.2f, 1f, 0.35f, 1f);

    private Transform player;
    private readonly List<LineRenderer> outlineLines = new List<LineRenderer>();
    private Material outlineMaterial;
    private bool isHighlighted;

    private Vector3 PromptPosition => promptAnchor != null ? promptAnchor.position : transform.position + Vector3.up * 1.4f;

    private void Update()
    {
        ResolvePlayer();
        UpdateHighlight();

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame || !IsPlayerInRange() || !GameUIState.CanProcessGameplayInput)
        {
            return;
        }

        if (mode == PortalMode.ExitHomeInterior)
        {
            SceneTransitionManager.ExitHomeInterior();
        }
    }

    private void OnDisable()
    {
        SetHighlighted(false);
    }

    private void OnGUI()
    {
        if (IsPlayerInRange() && GameUIState.CanProcessGameplayInput)
        {
            GameGui.DrawToast(new Rect(Screen.width * 0.5f - 190f, Screen.height - 178f, 380f, 40f), GetPromptText());
        }
    }

    private string GetPromptText()
    {
        return mode == PortalMode.ExitHomeInterior
            ? GameLocalization.T("home.exit_prompt")
            : promptText;
    }

    private bool IsPlayerInRange()
    {
        ResolvePlayer();

        if (player == null)
        {
            return false;
        }

        return Vector3.Distance(player.position, PromptPosition) <= interactionDistance;
    }

    private void ResolvePlayer()
    {
        if (player == null)
        {
            player = PlayerRigReferences.FindLocalPlayerTransform();
        }
    }

    private void UpdateHighlight()
    {
        SetHighlighted(IsPlayerInRange() && GameUIState.CanProcessGameplayInput);
    }

    private void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;

        if (highlightRenderers == null || highlightRenderers.Length == 0)
        {
            return;
        }

        EnsureOutline();

        foreach (LineRenderer outlineLine in outlineLines)
        {
            if (outlineLine != null)
            {
                outlineLine.enabled = highlighted;
            }
        }
    }

    private void EnsureOutline()
    {
        if (outlineLines.Count > 0)
        {
            return;
        }

        Bounds bounds = GetHighlightBounds();
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, max.z),
            new Vector3(min.x, max.y, max.z),
        };

        int[,] edges =
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 },
        };

        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");

        if (shader != null)
        {
            outlineMaterial = new Material(shader)
            {
                name = "Door Exit Outline Material",
            };
        }

        for (int i = 0; i < edges.GetLength(0); i++)
        {
            GameObject lineObject = new GameObject("Door Exit Outline");
            lineObject.transform.SetParent(transform, true);

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.enabled = false;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, corners[edges[i, 0]]);
            lineRenderer.SetPosition(1, corners[edges[i, 1]]);
            lineRenderer.startColor = highlightColor;
            lineRenderer.endColor = highlightColor;
            lineRenderer.startWidth = 0.035f;
            lineRenderer.endWidth = 0.035f;
            lineRenderer.numCapVertices = 2;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            if (outlineMaterial != null)
            {
                lineRenderer.sharedMaterial = outlineMaterial;
            }

            outlineLines.Add(lineRenderer);
        }
    }

    private Bounds GetHighlightBounds()
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.one);

        foreach (Renderer renderer in highlightRenderers)
        {
            if (renderer == null)
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

        bounds.Expand(0.06f);
        return bounds;
    }
}
