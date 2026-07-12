using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }
    public static bool IsNightNow => Instance != null && Instance.isNight;

    public float dayDurationSeconds = 240f;
    public float nightDurationSeconds = 75f;
    public Light sunLight;
    public Color dayLightColor = new Color(1f, 0.94f, 0.82f, 1f);
    public Color nightLightColor = new Color(0.3f, 0.42f, 0.68f, 1f);
    public Color dayAmbientColor = new Color(0.55f, 0.62f, 0.66f, 1f);
    public Color nightAmbientColor = new Color(0.055f, 0.065f, 0.11f, 1f);

    private bool isNight;
    private float phaseTimer;
    private int dayNumber = 1;
    private string statusMessage = "";
    private float statusMessageTimer;
    private Material sourceSkyMaterial;
    private Material runtimeSkyMaterial;
    private Material celestialMaterial;
    private GameObject celestialBody;
    private Renderer celestialRenderer;
    private Camera playerCamera;
    private OceanWaterSurface waterSurface;
    private float initialSkyRotation;

    public int DayNumber => dayNumber;
    public bool IsNight => isNight;
    public float Phase01 => Mathf.Clamp01(phaseTimer / Mathf.Max(0.01f, isNight ? nightDurationSeconds : dayDurationSeconds));
    public string ClockText => GetClockText();
    public bool CanSleep => isNight || GetCurrentHour() >= 20f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ResolveSunLight();
        EnsureRuntimeSky();
        EnsureCelestialBody();
        ApplyLighting();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (runtimeSkyMaterial != null && RenderSettings.skybox == runtimeSkyMaterial)
        {
            RenderSettings.skybox = sourceSkyMaterial;
        }

        if (runtimeSkyMaterial != null)
        {
            Destroy(runtimeSkyMaterial);
        }

        if (celestialMaterial != null)
        {
            Destroy(celestialMaterial);
        }
    }

    private void Update()
    {
        phaseTimer += Time.deltaTime;

        if (!IsRemoteClientClock() && !isNight && phaseTimer >= dayDurationSeconds)
        {
            StartNight();
        }
        else if (!IsRemoteClientClock() && isNight && phaseTimer >= nightDurationSeconds)
        {
            FinishNightAndStartMorning("Morning came. Treasures reset.");
        }

        if (statusMessageTimer > 0f)
        {
            statusMessageTimer -= Time.deltaTime;
        }

        ApplyLighting();
    }

    public void SleepUntilMorning()
    {
        FinishNightAndStartMorning("You slept until morning. Treasures reset.");
    }

    public void ApplySavedState(int savedDayNumber, bool savedIsNight, float savedPhase01)
    {
        bool dayChanged = savedDayNumber > dayNumber;
        dayNumber = Mathf.Max(1, savedDayNumber);
        isNight = savedIsNight;
        phaseTimer = Mathf.Clamp01(savedPhase01) * Mathf.Max(0.01f, isNight ? nightDurationSeconds : dayDurationSeconds);

        if (dayChanged)
        {
            ResetSearchDay();
        }

        statusMessage = "";
        statusMessageTimer = 0f;
        ApplyLighting();
    }

    public void RefreshLighting()
    {
        ApplyLighting();
    }

    private void StartNight()
    {
        isNight = true;
        phaseTimer = 0f;
        ShowStatus("Night started. Searching is disabled.");
    }

    private void FinishNightAndStartMorning(string message)
    {
        isNight = false;
        phaseTimer = 0f;
        dayNumber++;
        ResetSearchDay();
        BroadcastDayStateIfHost();
        ShowStatus(message);
    }

    private void ResetSearchDay()
    {
        TreasureSpawner treasureSpawner = FindAnyObjectByType<TreasureSpawner>();

        if (treasureSpawner != null)
        {
            treasureSpawner.RegenerateTreasures(dayNumber);
        }

        GroundScanner[] scanners = FindObjectsByType<GroundScanner>();

        foreach (GroundScanner scanner in scanners)
        {
            if (scanner != null)
            {
                scanner.ClearScannedArea();
            }
        }

        if (treasureSpawner == null)
        {
            SearchArea[] searchAreas = FindObjectsByType<SearchArea>();

            foreach (SearchArea searchArea in searchAreas)
            {
                if (searchArea != null)
                {
                    searchArea.ResetForNewDay();
                }
            }
        }

        SearchMarker[] markers = FindObjectsByType<SearchMarker>();

        foreach (SearchMarker marker in markers)
        {
            if (marker != null)
            {
                Destroy(marker.gameObject);
            }
        }

        Transform[] transforms = FindObjectsByType<Transform>();

        foreach (Transform target in transforms)
        {
            if (target != null && target.name == "Dug Ground")
            {
                Destroy(target.gameObject);
            }
        }
    }

    private bool IsRemoteClientClock()
    {
        LocalCoopManager coop = LocalCoopManager.Instance;
        return coop != null && coop.Role == LocalCoopManager.CoopRole.Client;
    }

    private void BroadcastDayStateIfHost()
    {
        LocalCoopManager coop = LocalCoopManager.Instance;

        if (coop != null && coop.Role == LocalCoopManager.CoopRole.Host)
        {
            coop.ReportTeamStateChanged();
        }
    }

    private void ResolveSunLight()
    {
        if (sunLight != null)
        {
            return;
        }

        Light[] lights = FindObjectsByType<Light>();

        foreach (Light light in lights)
        {
            if (light != null && light.type == LightType.Directional)
            {
                sunLight = light;
                return;
            }
        }

        GameObject lightObject = new GameObject("Directional Light");
        sunLight = lightObject.AddComponent<Light>();
        sunLight.type = LightType.Directional;
    }

    private void ApplyLighting()
    {
        if (SceneTransitionManager.IsHomeInteriorActive)
        {
            if (celestialBody != null)
            {
                celestialBody.SetActive(false);
            }

            return;
        }

        ResolveSunLight();
        EnsureRuntimeSky();
        EnsureCelestialBody();
        RenderSettings.sun = sunLight;

        float t = Phase01;

        if (isNight)
        {
            float moonArc = Mathf.Sin(t * Mathf.PI);
            sunLight.intensity = Mathf.Lerp(0.16f, 0.28f, Mathf.Pow(moonArc, 0.65f));
            sunLight.color = nightLightColor;
            sunLight.transform.rotation = GetCelestialLightRotation(true, t);
            ApplyWorldShadowSettings(Mathf.Lerp(0.38f, 0.46f, moonArc));
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.045f, 0.065f, 0.13f), new Color(0.075f, 0.11f, 0.22f), moonArc);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.035f, 0.045f, 0.085f), new Color(0.055f, 0.075f, 0.14f), moonArc);
            RenderSettings.ambientGroundColor = new Color(0.018f, 0.023f, 0.045f, 1f);
            RenderSettings.ambientLight = RenderSettings.ambientEquatorColor;
            RenderSettings.ambientIntensity = Mathf.Lerp(0.28f, 0.38f, moonArc);
            RenderSettings.reflectionIntensity = Mathf.Lerp(0.16f, 0.25f, moonArc);
            ApplySkyAppearance(true, moonArc);
            UpdateCelestialBody(true, moonArc, t);
            ApplyWaterAppearance(true, moonArc);
            PostProcessingBootstrapper.ApplyIslandAtmosphere(t, true, moonArc);
            return;
        }

        float sunArc = Mathf.Sin(t * Mathf.PI);
        sunLight.intensity = Mathf.Lerp(0.58f, 1.08f, Mathf.Pow(sunArc, 0.58f));
        sunLight.color = Color.Lerp(new Color(1f, 0.7f, 0.43f, 1f), new Color(1f, 0.95f, 0.84f, 1f), sunArc);
        sunLight.transform.rotation = GetCelestialLightRotation(false, t);
        ApplyWorldShadowSettings(Mathf.Lerp(0.45f, 0.55f, sunArc));
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.55f, 0.44f, 0.36f), new Color(0.48f, 0.62f, 0.71f), sunArc);
        RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.46f, 0.39f, 0.34f), new Color(0.46f, 0.51f, 0.49f), sunArc);
        RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.24f, 0.2f, 0.17f), new Color(0.27f, 0.26f, 0.23f), sunArc);
        RenderSettings.ambientLight = RenderSettings.ambientEquatorColor;
        RenderSettings.ambientIntensity = Mathf.Lerp(0.8f, 0.92f, sunArc);
        RenderSettings.reflectionIntensity = Mathf.Lerp(0.38f, 0.58f, sunArc);
        ApplySkyAppearance(false, sunArc);
        UpdateCelestialBody(false, sunArc, t);
        ApplyWaterAppearance(false, sunArc);
        PostProcessingBootstrapper.ApplyIslandAtmosphere(t, false, sunArc);
    }

    private void ApplyWorldShadowSettings(float shadowStrength)
    {
        if (sunLight == null)
        {
            return;
        }

        sunLight.shadows = LightShadows.Soft;
        sunLight.shadowStrength = shadowStrength;
        sunLight.shadowBias = 0.04f;
        sunLight.shadowNormalBias = 0.24f;
        sunLight.shadowNearPlane = 0.2f;
#if UNITY_EDITOR
        sunLight.shadowAngle = 0.55f;
#endif
    }

    private void EnsureRuntimeSky()
    {
        if (runtimeSkyMaterial == null)
        {
            sourceSkyMaterial = RenderSettings.skybox;

            if (sourceSkyMaterial == null)
            {
                return;
            }

            runtimeSkyMaterial = new Material(sourceSkyMaterial)
            {
                name = sourceSkyMaterial.name + " (Runtime Atmosphere)",
                hideFlags = HideFlags.HideAndDontSave
            };

            initialSkyRotation = runtimeSkyMaterial.HasProperty("_Rotation")
                ? runtimeSkyMaterial.GetFloat("_Rotation")
                : 0f;
        }

        RenderSettings.skybox = runtimeSkyMaterial;
    }

    private void ApplySkyAppearance(bool night, float arc)
    {
        if (runtimeSkyMaterial == null)
        {
            return;
        }

        Color tint = night
            ? Color.Lerp(new Color(0.13f, 0.17f, 0.3f), new Color(0.18f, 0.24f, 0.4f), arc)
            : Color.Lerp(new Color(0.58f, 0.45f, 0.37f), new Color(0.52f, 0.57f, 0.64f), arc);
        float exposure = night ? Mathf.Lerp(0.28f, 0.42f, arc) : Mathf.Lerp(0.9f, 1.12f, arc);

        if (runtimeSkyMaterial.HasProperty("_Tint"))
        {
            runtimeSkyMaterial.SetColor("_Tint", tint);
        }

        if (runtimeSkyMaterial.HasProperty("_Exposure"))
        {
            runtimeSkyMaterial.SetFloat("_Exposure", exposure);
        }

        if (runtimeSkyMaterial.HasProperty("_Rotation"))
        {
            runtimeSkyMaterial.SetFloat("_Rotation", initialSkyRotation + Time.time * 0.12f);
        }
    }

    private void EnsureCelestialBody()
    {
        if (celestialBody != null)
        {
            return;
        }

        celestialBody = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        celestialBody.name = "Moving Sun And Moon";
        celestialBody.transform.SetParent(transform, true);

        Collider bodyCollider = celestialBody.GetComponent<Collider>();

        if (bodyCollider != null)
        {
            Destroy(bodyCollider);
        }

        celestialRenderer = celestialBody.GetComponent<Renderer>();

        if (celestialRenderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        celestialMaterial = new Material(shader)
        {
            name = "Island Celestial Body Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        celestialRenderer.sharedMaterial = celestialMaterial;
        celestialRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        celestialRenderer.receiveShadows = false;
        celestialRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        celestialRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private void UpdateCelestialBody(bool night, float arc, float phase)
    {
        if (celestialBody == null || celestialMaterial == null || sunLight == null)
        {
            return;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;

            if (playerCamera == null)
            {
                playerCamera = FindAnyObjectByType<Camera>();
            }
        }

        if (playerCamera == null)
        {
            celestialBody.SetActive(false);
            return;
        }

        celestialBody.SetActive(true);
        float distance = night ? 420f : 500f;
        float diameter = night ? 10f : Mathf.Lerp(14f, 17f, arc);
        Vector3 skyDirection = GetCelestialDirection(night, phase);
        celestialBody.transform.position = playerCamera.transform.position + skyDirection * distance;
        celestialBody.transform.localScale = Vector3.one * diameter;

        Color baseColor = night
            ? new Color(0.58f, 0.72f, 1f, 1f)
            : Color.Lerp(new Color(1f, 0.58f, 0.25f, 1f), new Color(1f, 0.92f, 0.72f, 1f), arc);
        Color hdrColor = MultiplyRgb(baseColor, night ? 2.5f : Mathf.Lerp(4.5f, 3.4f, arc));

        if (celestialMaterial.HasProperty("_BaseColor"))
        {
            celestialMaterial.SetColor("_BaseColor", hdrColor);
        }

        if (celestialMaterial.HasProperty("_Color"))
        {
            celestialMaterial.SetColor("_Color", hdrColor);
        }

        if (celestialMaterial.HasProperty("_EmissionColor"))
        {
            celestialMaterial.EnableKeyword("_EMISSION");
            celestialMaterial.SetColor("_EmissionColor", hdrColor);
        }
    }

    private static Quaternion GetCelestialLightRotation(bool night, float phase)
    {
        Vector3 lightDirection = -GetCelestialDirection(night, phase);
        return Quaternion.LookRotation(lightDirection, Vector3.forward);
    }

    private static Vector3 GetCelestialDirection(bool night, float phase)
    {
        Quaternion visualOrbit = night
            ? Quaternion.Euler(Mathf.Lerp(28f, 152f, phase), Mathf.Lerp(118f, 158f, phase), 0f)
            : Quaternion.Euler(Mathf.Lerp(18f, 162f, phase), Mathf.Lerp(-68f, -18f, phase), 0f);
        return -(visualOrbit * Vector3.forward).normalized;
    }

    private void ApplyWaterAppearance(bool night, float arc)
    {
        if (waterSurface == null)
        {
            waterSurface = FindAnyObjectByType<OceanWaterSurface>();
        }

        waterSurface?.ApplyAtmosphere(night, arc);
    }

    private static Color MultiplyRgb(Color color, float multiplier)
    {
        return new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);
    }

    private void ShowStatus(string message)
    {
        statusMessage = message;
        statusMessageTimer = 4.5f;
    }

    private string GetClockText()
    {
        float hour = GetCurrentHour();
        int wholeHour = Mathf.FloorToInt(hour);
        int minute = Mathf.FloorToInt((hour - wholeHour) * 60f);
        return wholeHour.ToString("00") + ":" + minute.ToString("00");
    }

    private float GetCurrentHour()
    {
        float hour = isNight
            ? Mathf.Lerp(20f, 30f, Phase01)
            : Mathf.Lerp(6f, 20f, Phase01);

        if (hour >= 24f)
        {
            hour -= 24f;
        }

        return hour;
    }

    private void OnGUI()
    {
        if (GameUIState.AnyMenuOpen)
        {
            return;
        }

        string label = "Day " + dayNumber + " | " + (isNight ? "Night" : "Day") + " " + ClockText;
        GameGui.DrawToast(new Rect(Screen.width * 0.5f - 160f, 16f, 320f, 38f), label);

        if (statusMessageTimer > 0f)
        {
            GameGui.DrawToast(new Rect(Screen.width * 0.5f - 210f, 60f, 420f, 40f), statusMessage);
        }
    }
}

public static class DayNightCycleBootstrapper
{
    public static void EnsureDayNightCycle()
    {
        if (Object.FindAnyObjectByType<DayNightCycle>() != null)
        {
            return;
        }

        new GameObject("Day Night Cycle").AddComponent<DayNightCycle>();
    }
}
