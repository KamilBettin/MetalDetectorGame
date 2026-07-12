using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class GameBootstrapper
{
    private static BootstrapRetryRunner retryRunner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureGlobalObjects();
        EnsureSceneObjects();
        EnsureRetryRunner();

        GameSaveSystem.CaptureInitialDefaults();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureGlobalObjects();
        EnsureSceneObjects();
        EnsureRetryRunner();
    }

    private static void EnsureGlobalObjects()
    {
        if (Object.FindAnyObjectByType<TutorialQuestSystem>() == null)
        {
            new GameObject("Tutorial Quest System").AddComponent<TutorialQuestSystem>();
        }

        if (Object.FindAnyObjectByType<RuntimeGameUI>() == null)
        {
            new GameObject("Runtime Game UI").AddComponent<RuntimeGameUI>();
        }

        if (Object.FindAnyObjectByType<LocalCoopManager>() == null)
        {
            new GameObject("Local Coop Manager").AddComponent<LocalCoopManager>();
        }

        LoadingScreenUI.EnsureExists();
        EnsureEventSystem();

        if (Object.FindAnyObjectByType<PauseMenuUI>() == null)
        {
            new GameObject("Pause Menu UI").AddComponent<PauseMenuUI>();
        }

        if (Object.FindAnyObjectByType<CharacterSelectionUI>() == null)
        {
            new GameObject("Character Selection UI").AddComponent<CharacterSelectionUI>();
        }

        if (Object.FindAnyObjectByType<StartMenuUI>() == null)
        {
            new GameObject("Start Menu UI").AddComponent<StartMenuUI>();
        }
    }

    private static void EnsureSceneObjects()
    {
        RunBootstrapStep("scene transition manager", SceneTransitionManager.EnsureExists);

        if (SceneTransitionManager.IsHomeInteriorActive)
        {
            RunBootstrapStep("home interior scene repair", SceneTransitionManager.PrepareHomeInteriorScene);
            RunBootstrapStep("local player avatar", EnsureLocalPlayerAvatarVisual);
            RunBootstrapStep("event system", EnsureEventSystem);
            RunBootstrapStep("home interior exit", SceneTransitionManager.EnsureInteriorExitPortal);
            RunBootstrapStep("home interior stations", HomeInteractionStationBootstrapper.EnsureHomeInteriorStations);
            RunBootstrapStep("post processing", PostProcessingBootstrapper.EnsurePostProcessing);
            return;
        }

        RunBootstrapStep("UMA runtime", () => UmaCharacterFactory.EnsureRuntime());
        RunBootstrapStep("day night cycle", DayNightCycleBootstrapper.EnsureDayNightCycle);
        RunBootstrapStep("default search areas", DefaultSearchAreaBootstrapper.EnsureDefaultSearchAreas);
        RunBootstrapStep("player home", PlayerHomeBootstrapper.EnsurePlayerHome);
        RunBootstrapStep("trader NPCs", TraderBootstrapper.EnsureTraderAtHome);
        RunBootstrapStep("quest NPC", QuestGiverBootstrapper.EnsureQuestGiverAtHome);
        RunBootstrapStep("scanner", EnsureScanner);
        RunBootstrapStep("detector visual", EnsureDetectorVisual);
        RunBootstrapStep("local player avatar", EnsureLocalPlayerAvatarVisual);
        RunBootstrapStep("event system", EnsureEventSystem);
        RunBootstrapStep("sand appearance", EnsureSandAppearance);
        RunBootstrapStep("island environment", EnvironmentScatterBootstrapper.EnsureIslandEnvironment);
        RunBootstrapStep("post processing", PostProcessingBootstrapper.EnsurePostProcessing);
    }

    private static void RunBootstrapStep(string stepName, Action step)
    {
        try
        {
            step?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogError("Game bootstrap step failed: " + stepName);
            Debug.LogException(exception);
        }
    }

    private static void EnsureRetryRunner()
    {
        if (retryRunner == null)
        {
            retryRunner = Object.FindAnyObjectByType<BootstrapRetryRunner>();
        }

        if (retryRunner == null)
        {
            GameObject runnerObject = new GameObject("Game Bootstrapper Runner");
            retryRunner = runnerObject.AddComponent<BootstrapRetryRunner>();
            Object.DontDestroyOnLoad(runnerObject);
        }

        retryRunner.Restart();
    }

    private class BootstrapRetryRunner : MonoBehaviour
    {
        private const float RetryDuration = 6f;
        private const float RetryInterval = 0.5f;
        private float stopTime;
        private float nextRunTime;

        public void Restart()
        {
            stopTime = Time.unscaledTime + RetryDuration;
            nextRunTime = 0f;
            enabled = true;
        }

        private void Update()
        {
            if (Time.unscaledTime >= stopTime)
            {
                enabled = false;
                LoadingScreenUI.HideAfterMinimum(0.1f);
                return;
            }

            if (Time.unscaledTime < nextRunTime)
            {
                return;
            }

            nextRunTime = Time.unscaledTime + RetryInterval;
            EnsureSceneObjects();

            LoadingScreenUI.HideAfterMinimum(0.2f);
        }
    }

    private static void EnsureScanner()
    {
        MetalDetector metalDetector = PlayerRigReferences.FindLocalMetalDetector();

        if (metalDetector == null)
        {
            return;
        }

        EnsureDetectorVisual();

        GroundScanner scanner = metalDetector.GetComponent<GroundScanner>();

        if (scanner == null)
        {
            scanner = Object.FindAnyObjectByType<GroundScanner>();
        }

        if (scanner == null)
        {
            scanner = metalDetector.gameObject.AddComponent<GroundScanner>();
        }

        scanner.scanOrigin = metalDetector.detectorHead != null ? metalDetector.detectorHead : metalDetector.transform;
    }

    private static void EnsureDetectorVisual()
    {
        MetalDetector metalDetector = PlayerRigReferences.FindLocalMetalDetector();

        if (metalDetector == null || metalDetector.GetComponent<DetectorVisualBuilder>() != null)
        {
            return;
        }

        DetectorVisualBuilder visualBuilder = metalDetector.gameObject.AddComponent<DetectorVisualBuilder>();
        visualBuilder.detectorHead = metalDetector.detectorHead;
    }

    private static void EnsureLocalPlayerAvatarVisual()
    {
        FirstPersonController controller = Object.FindAnyObjectByType<FirstPersonController>();

        if (controller == null || controller.GetComponent<LocalPlayerAvatarVisual>() != null)
        {
            return;
        }

        controller.gameObject.AddComponent<LocalPlayerAvatarVisual>();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static void EnsureSandAppearance()
    {
        if (Object.FindAnyObjectByType<SandGroundAppearance>() != null)
        {
            return;
        }

        new GameObject("Sand Ground Appearance").AddComponent<SandGroundAppearance>();
    }
}

public static class PostProcessingBootstrapper
{
    private const string VolumeObjectName = "Metal Detector Post Processing";
    private const float VolumePriority = 80f;

    private static GameObject volumeObject;
    private static Volume volume;
    private static VolumeProfile islandProfile;
    private static VolumeProfile interiorProfile;

    public static void EnsurePostProcessing()
    {
        if (!IsUniversalRenderPipelineActive())
        {
            return;
        }

        Camera camera = FindPlayerCamera();

        if (camera == null)
        {
            return;
        }

        ConfigurePostProcessingCameras(camera);
        EnsureVolumeObject();
        DisableConflictingVolumes();

        bool isInterior = SceneTransitionManager.IsHomeInteriorActive;
        volume.isGlobal = true;
        volume.priority = VolumePriority;
        volume.weight = 1f;
        volume.sharedProfile = isInterior ? GetInteriorProfile() : GetIslandProfile();
        volume.enabled = true;

        if (isInterior)
        {
            DisableSceneFog();
            return;
        }

        DayNightCycle cycle = DayNightCycle.Instance;
        float phase = cycle != null ? cycle.Phase01 : 0.45f;
        bool isNight = cycle != null && cycle.IsNight;
        ApplyIslandAtmosphere(phase, isNight, Mathf.Sin(phase * Mathf.PI));
    }

    private static bool IsUniversalRenderPipelineActive()
    {
        RenderPipelineAsset pipelineAsset = GraphicsSettings.currentRenderPipeline;

        if (pipelineAsset == null)
        {
            pipelineAsset = GraphicsSettings.defaultRenderPipeline;
        }

        return pipelineAsset != null && pipelineAsset.GetType().FullName.Contains("Universal");
    }

    private static Camera FindPlayerCamera()
    {
        FirstPersonController controller = Object.FindAnyObjectByType<FirstPersonController>();

        if (controller != null && controller.playerCamera != null)
        {
            Camera controllerCamera = controller.playerCamera.GetComponent<Camera>();

            if (controllerCamera != null)
            {
                return controllerCamera;
            }
        }

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            return mainCamera;
        }

        Camera[] cameras = Object.FindObjectsByType<Camera>();

        foreach (Camera camera in cameras)
        {
            if (camera != null && camera.enabled)
            {
                return camera;
            }
        }

        return null;
    }

    private static void DisableCameraPostProcessing(Camera camera)
    {
        UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();

        if (cameraData == null)
        {
            cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }

        cameraData.renderPostProcessing = false;
        cameraData.antialiasing = AntialiasingMode.None;
        cameraData.antialiasingQuality = AntialiasingQuality.Low;
        cameraData.stopNaN = true;
        cameraData.dithering = false;
        cameraData.volumeTrigger = camera.transform;
        cameraData.volumeLayerMask = 0;
    }

    private static void EnableCameraPostProcessing(Camera camera)
    {
        UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();

        if (cameraData == null)
        {
            cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }

        int volumeLayer = GetVolumeLayer();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.High;
        cameraData.stopNaN = true;
        cameraData.dithering = true;
        cameraData.volumeTrigger = camera.transform;
        cameraData.volumeLayerMask = volumeLayer >= 0 ? 1 << volumeLayer : ~0;
    }

    private static void ConfigurePostProcessingCameras(Camera primaryCamera)
    {
        EnableCameraPostProcessing(primaryCamera);
        DisableLegacyCameraEffects(primaryCamera);

        Camera[] cameras = Object.FindObjectsByType<Camera>();

        foreach (Camera camera in cameras)
        {
            if (camera == null || camera == primaryCamera)
            {
                continue;
            }

            DisableCameraPostProcessing(camera);
            DisableLegacyCameraEffects(camera);
        }
    }

    private static void DisableLegacyCameraEffects(Camera camera)
    {
        MonoBehaviour[] behaviours = camera.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;

            if (typeName == "GlobalFog"
                || typeName == "Tonemapping"
                || typeName == "Bloom"
                || typeName == "BloomAndFlares"
                || typeName == "BloomOptimized"
                || typeName == "VignetteAndChromaticAberration"
                || typeName == "NoiseAndGrain"
                || typeName == "NoiseAndScratches"
                || typeName == "ScreenOverlay"
                || typeName == "ColorCorrectionCurves"
                || typeName == "ColorCorrectionLookup"
                || typeName == "DepthOfField"
                || typeName == "DepthOfFieldDeprecated"
                || typeName == "MotionBlur"
                || typeName == "CameraMotionBlur"
                || typeName == "SunShafts"
                || typeName == "Antialiasing"
                || typeName == "VolumetricLightRenderer")
            {
                behaviour.enabled = false;
            }
        }
    }

    private static void DisableSceneFog()
    {
        RenderSettings.fog = false;
        RenderSettings.fogDensity = 0f;
        RenderSettings.fogStartDistance = 1000f;
        RenderSettings.fogEndDistance = 10000f;
    }

    private static void EnsureVolumeObject()
    {
        if (volumeObject == null)
        {
            GameObject existing = GameObject.Find(VolumeObjectName)
                ?? GameObject.Find(VolumeObjectName + " (Island)")
                ?? GameObject.Find(VolumeObjectName + " (Home)");

            volumeObject = existing != null ? existing : new GameObject(VolumeObjectName);
            Object.DontDestroyOnLoad(volumeObject);
        }

        volumeObject.layer = GetVolumeLayer();

        if (volume == null)
        {
            volume = volumeObject.GetComponent<Volume>();

            if (volume == null)
            {
                volume = volumeObject.AddComponent<Volume>();
            }
        }
    }

    private static void DisableConflictingVolumes()
    {
        Volume[] sceneVolumes = Object.FindObjectsByType<Volume>();

        foreach (Volume sceneVolume in sceneVolumes)
        {
            if (sceneVolume == null || sceneVolume == volume)
            {
                continue;
            }

            sceneVolume.enabled = false;
            sceneVolume.weight = 0f;
        }
    }

    private static int GetVolumeLayer()
    {
        return LayerMask.NameToLayer("Default");
    }

    private static VolumeProfile GetIslandProfile()
    {
        if (islandProfile == null)
        {
            islandProfile = CreateProfile("Island Post Processing");
            ConfigureIslandProfile(islandProfile);
        }

        return islandProfile;
    }

    private static VolumeProfile GetInteriorProfile()
    {
        if (interiorProfile == null)
        {
            interiorProfile = CreateProfile("Home Interior Post Processing");
            ConfigureInteriorProfile(interiorProfile);
        }

        return interiorProfile;
    }

    private static VolumeProfile CreateProfile(string profileName)
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = profileName;
        profile.hideFlags = HideFlags.HideAndDontSave;
        return profile;
    }

    private static void ConfigureIslandProfile(VolumeProfile profile)
    {
        ConfigureCommonProfile(
            profile,
            postExposure: 0.08f,
            contrast: 7f,
            saturation: 7f,
            colorFilter: new Color(1f, 0.985f, 0.94f),
            temperature: 5f,
            tint: 0f,
            bloomIntensity: 0.16f,
            bloomThreshold: 1.05f,
            bloomScatter: 0.52f,
            vignetteIntensity: 0.09f,
            vignetteSmoothness: 0.44f,
            grainIntensity: 0f);
    }

    private static void ConfigureInteriorProfile(VolumeProfile profile)
    {
        ConfigureCommonProfile(
            profile,
            postExposure: 0.22f,
            contrast: 5f,
            saturation: 3f,
            colorFilter: new Color(1f, 0.96f, 0.88f),
            temperature: 12f,
            tint: 1f,
            bloomIntensity: 0.12f,
            bloomThreshold: 1.08f,
            bloomScatter: 0.45f,
            vignetteIntensity: 0.1f,
            vignetteSmoothness: 0.42f,
            grainIntensity: 0f);
    }

    private static void ConfigureCommonProfile(
        VolumeProfile profile,
        float postExposure,
        float contrast,
        float saturation,
        Color colorFilter,
        float temperature,
        float tint,
        float bloomIntensity,
        float bloomThreshold,
        float bloomScatter,
        float vignetteIntensity,
        float vignetteSmoothness,
        float grainIntensity)
    {
        Tonemapping tonemapping = Add<Tonemapping>(profile);
        tonemapping.mode.Override(TonemappingMode.ACES);

        ColorAdjustments colorAdjustments = Add<ColorAdjustments>(profile);
        colorAdjustments.postExposure.Override(postExposure);
        colorAdjustments.contrast.Override(contrast);
        colorAdjustments.saturation.Override(saturation);
        colorAdjustments.colorFilter.Override(colorFilter);
        colorAdjustments.hueShift.Override(0f);

        WhiteBalance whiteBalance = Add<WhiteBalance>(profile);
        whiteBalance.temperature.Override(temperature);
        whiteBalance.tint.Override(tint);

        Bloom bloom = Add<Bloom>(profile);
        bloom.threshold.Override(bloomThreshold);
        bloom.intensity.Override(bloomIntensity);
        bloom.scatter.Override(bloomScatter);
        bloom.tint.Override(Color.white);
        bloom.highQualityFiltering.Override(true);
        bloom.downscale.Override(BloomDownscaleMode.Half);
        bloom.maxIterations.Override(6);

        Vignette vignette = Add<Vignette>(profile);
        vignette.color.Override(Color.black);
        vignette.center.Override(new Vector2(0.5f, 0.5f));
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(vignetteSmoothness);
        vignette.rounded.Override(false);

        FilmGrain filmGrain = Add<FilmGrain>(profile);
        filmGrain.type.Override(FilmGrainLookup.Thin1);
        filmGrain.intensity.Override(grainIntensity);
        filmGrain.response.Override(0.82f);
        filmGrain.active = grainIntensity > 0f;

        ShadowsMidtonesHighlights colorWheels = Add<ShadowsMidtonesHighlights>(profile);
        colorWheels.shadows.Override(new Vector4(0.96f, 1.01f, 1.06f, 0f));
        colorWheels.midtones.Override(new Vector4(1f, 1f, 1f, 0f));
        colorWheels.highlights.Override(new Vector4(1.04f, 1.015f, 0.97f, 0f));
        colorWheels.shadowsStart.Override(0f);
        colorWheels.shadowsEnd.Override(0.34f);
        colorWheels.highlightsStart.Override(0.58f);
        colorWheels.highlightsEnd.Override(1f);
    }

    public static void ApplyIslandAtmosphere(float phase01, bool isNight, float skyArc)
    {
        if (!IsUniversalRenderPipelineActive() || SceneTransitionManager.IsHomeInteriorActive)
        {
            return;
        }

        float arc = Mathf.Clamp01(skyArc);
        VolumeProfile profile = GetIslandProfile();

        if (profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            if (isNight)
            {
                colorAdjustments.postExposure.Override(Mathf.Lerp(-0.42f, -0.28f, arc));
                colorAdjustments.contrast.Override(11f);
                colorAdjustments.saturation.Override(-7f);
                colorAdjustments.colorFilter.Override(Color.Lerp(new Color(0.72f, 0.8f, 1f), new Color(0.82f, 0.88f, 1f), arc));
            }
            else
            {
                colorAdjustments.postExposure.Override(Mathf.Lerp(0.055f, 0.14f, arc));
                colorAdjustments.contrast.Override(Mathf.Lerp(4f, 6f, arc));
                colorAdjustments.saturation.Override(Mathf.Lerp(5f, 8f, arc));
                colorAdjustments.colorFilter.Override(Color.Lerp(new Color(1f, 0.91f, 0.82f), new Color(1f, 0.99f, 0.95f), arc));
            }
        }

        if (profile.TryGet(out WhiteBalance whiteBalance))
        {
            whiteBalance.temperature.Override(isNight ? Mathf.Lerp(-22f, -14f, arc) : Mathf.Lerp(13f, 3f, arc));
            whiteBalance.tint.Override(isNight ? -2f : 0f);
        }

        if (profile.TryGet(out Bloom bloom))
        {
            bloom.intensity.Override(isNight ? Mathf.Lerp(0.23f, 0.3f, arc) : Mathf.Lerp(0.23f, 0.17f, arc));
            bloom.threshold.Override(isNight ? 0.82f : Mathf.Lerp(0.92f, 1.08f, arc));
            bloom.tint.Override(isNight ? new Color(0.72f, 0.82f, 1f) : Color.Lerp(new Color(1f, 0.76f, 0.5f), Color.white, arc));
        }

        if (profile.TryGet(out Vignette vignette))
        {
            vignette.intensity.Override(isNight ? 0.16f : Mathf.Lerp(0.11f, 0.075f, arc));
        }

        if (profile.TryGet(out ShadowsMidtonesHighlights colorWheels))
        {
            if (isNight)
            {
                colorWheels.shadows.Override(new Vector4(0.82f, 0.9f, 1.12f, 0f));
                colorWheels.midtones.Override(new Vector4(0.93f, 0.97f, 1.045f, 0f));
                colorWheels.highlights.Override(new Vector4(0.92f, 0.98f, 1.1f, 0f));
            }
            else
            {
                colorWheels.shadows.Override(Vector4.Lerp(new Vector4(0.96f, 1.01f, 1.08f, 0.012f), new Vector4(0.98f, 1.02f, 1.055f, 0.008f), arc));
                colorWheels.midtones.Override(new Vector4(1f, 1f, 1f, 0.004f));
                colorWheels.highlights.Override(Vector4.Lerp(new Vector4(1.08f, 1.015f, 0.92f, 0f), new Vector4(1.035f, 1.01f, 0.975f, 0f), arc));
            }
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        if (isNight)
        {
            RenderSettings.fogColor = Color.Lerp(new Color(0.035f, 0.055f, 0.11f), new Color(0.055f, 0.085f, 0.16f), arc);
            RenderSettings.fogDensity = Mathf.Lerp(0.0027f, 0.00215f, arc);
            RenderSettings.fogStartDistance = Mathf.Lerp(32f, 52f, arc);
            RenderSettings.fogEndDistance = Mathf.Lerp(280f, 390f, arc);
            RenderSettings.subtractiveShadowColor = new Color(0.12f, 0.18f, 0.32f, 1f);
        }
        else
        {
            RenderSettings.fogColor = Color.Lerp(new Color(0.7f, 0.58f, 0.47f), new Color(0.55f, 0.72f, 0.8f), arc);
            RenderSettings.fogDensity = Mathf.Lerp(0.00215f, 0.0017f, arc);
            RenderSettings.fogStartDistance = Mathf.Lerp(45f, 78f, arc);
            RenderSettings.fogEndDistance = Mathf.Lerp(330f, 480f, arc);
            RenderSettings.subtractiveShadowColor = Color.Lerp(new Color(0.34f, 0.38f, 0.48f), new Color(0.3f, 0.39f, 0.5f), arc);
        }
    }

    private static T Add<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
        }

        component.active = true;
        return component;
    }
}

public static class PlayerRigReferences
{
    public static Transform FindLocalPlayerTransform()
    {
        FirstPersonController controller = Object.FindAnyObjectByType<FirstPersonController>();

        if (controller != null)
        {
            return controller.transform;
        }

        PlayerInventory inventory = Object.FindAnyObjectByType<PlayerInventory>();
        return inventory != null ? inventory.transform : null;
    }

    public static PlayerInventory FindLocalInventory()
    {
        Transform player = FindLocalPlayerTransform();

        if (player != null && player.TryGetComponent(out PlayerInventory inventory))
        {
            return inventory;
        }

        return Object.FindAnyObjectByType<PlayerInventory>();
    }

    public static MetalDetector FindLocalMetalDetector()
    {
        Transform player = FindLocalPlayerTransform();

        if (player != null)
        {
            MetalDetector detectorInPlayer = player.GetComponentInChildren<MetalDetector>(true);

            if (detectorInPlayer != null)
            {
                return detectorInPlayer;
            }
        }

        FirstPersonController controller = Object.FindAnyObjectByType<FirstPersonController>();

        if (controller != null && controller.playerCamera != null)
        {
            MetalDetector detectorInCamera = controller.playerCamera.GetComponentInChildren<MetalDetector>(true);

            if (detectorInCamera != null)
            {
                return detectorInCamera;
            }
        }

        return Object.FindAnyObjectByType<MetalDetector>();
    }

}
