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
        ApplyLighting();
    }

    private void Update()
    {
        phaseTimer += Time.deltaTime;

        if (!isNight && phaseTimer >= dayDurationSeconds)
        {
            StartNight();
        }
        else if (isNight && phaseTimer >= nightDurationSeconds)
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
        ShowStatus(message);
    }

    private void ResetSearchDay()
    {
        TreasureSpawner treasureSpawner = FindAnyObjectByType<TreasureSpawner>();

        if (treasureSpawner != null)
        {
            treasureSpawner.ClearSpawnedTreasures();
        }

        GroundScanner[] scanners = FindObjectsByType<GroundScanner>();

        foreach (GroundScanner scanner in scanners)
        {
            if (scanner != null)
            {
                scanner.ClearScannedArea();
            }
        }

        SearchArea[] searchAreas = FindObjectsByType<SearchArea>();

        foreach (SearchArea searchArea in searchAreas)
        {
            if (searchArea != null)
            {
                searchArea.ResetForNewDay();
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
        ResolveSunLight();

        float t = Phase01;

        if (isNight)
        {
            sunLight.intensity = Mathf.Lerp(0.12f, 0.06f, Mathf.Sin(t * Mathf.PI));
            sunLight.color = nightLightColor;
            sunLight.transform.rotation = Quaternion.Euler(Mathf.Lerp(195f, 330f, t), -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = nightAmbientColor;
            RenderSettings.ambientIntensity = 0.22f;
            return;
        }

        float sunArc = Mathf.Sin(t * Mathf.PI);
        sunLight.intensity = Mathf.Lerp(0.45f, 1.1f, sunArc);
        sunLight.color = Color.Lerp(new Color(1f, 0.72f, 0.48f, 1f), dayLightColor, sunArc);
        sunLight.transform.rotation = Quaternion.Euler(Mathf.Lerp(25f, 155f, t), -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(new Color(0.38f, 0.36f, 0.34f, 1f), dayAmbientColor, sunArc);
        RenderSettings.ambientIntensity = Mathf.Lerp(0.72f, 1f, sunArc);
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
