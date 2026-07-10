using UnityEngine;
using UnityEngine.InputSystem;

public class MetalDetector : MonoBehaviour
{
    private const float SignalRefreshInterval = 0.08f;
    private const float TreasureCacheRefreshInterval = 0.35f;

    public float detectionRange = 6f;
    public float strongSignalDistance = 1.5f;
    public float farBeepDelay = 1.15f;
    public float closeBeepDelay = 0.08f;
    public Transform detectorHead;
    public bool useRealBeepSound = true;
    public float beepVolume = 0.25f;
    public int detectorTier;
    public string[] detectorModelNames =
    {
        "Scout Plate",
        "Pocket Viper",
        "Standard Arc",
        "Auric Titan"
    };
    public float[] detectorScanRadii = { 0.14f, 0.17f, 0.2f, 0.23f };
    public float[] detectorSignalRanges = { 6f, 8f, 10f, 12f };
    public Transform[] detectorTierVisuals;

    public float DetectionRange => GetSignalRangeForTier(DetectorTier);
    public float CurrentSignal => currentSignal;
    public DetectableTreasure NearestTreasure => nearestTreasure;
    public float CurrentSignalDistanceMeters => currentSignalDistanceMeters;
    public bool RevealSignalActive => nearestTreasure != null && nearestTreasure.isRevealed && !nearestTreasure.isFound;
    public int DetectorTier => Mathf.Clamp(detectorTier, 0, MaxDetectorTier);
    public int MaxDetectorTier => Mathf.Max(GetMaxIndex(detectorScanRadii), GetMaxIndex(detectorSignalRanges), GetMaxIndex(detectorTierVisuals));
    public float CurrentScanRadius => GetScanRadiusForTier(DetectorTier);
    public string CurrentDetectorName => GetDetectorName(DetectorTier);
    public bool CanUpgradeDetector => DetectorTier < MaxDetectorTier;

    public void IncreaseRange(float amount)
    {
        detectionRange = Mathf.Max(0.1f, detectionRange + amount);
    }

    public void ReportTreasureRevealed(DetectableTreasure treasure)
    {
        if (treasure == null)
        {
            return;
        }

        nearestTreasure = treasure;
        currentSignal = 1f;
        currentSignalDistanceMeters = 0f;
        beepTimer = 0f;
        lastBeepTarget = null;
    }

    public bool TryUpgradeDetector()
    {
        if (!CanUpgradeDetector)
        {
            return false;
        }

        detectorTier++;
        ApplyDetectorTierVisuals();
        return true;
    }

    public float GetScanRadiusForTier(int tier)
    {
        if (detectorScanRadii == null || detectorScanRadii.Length == 0)
        {
            return 0.14f;
        }

        int safeTier = Mathf.Clamp(tier, 0, detectorScanRadii.Length - 1);
        return Mathf.Max(0.05f, detectorScanRadii[safeTier]);
    }

    public float GetSignalRangeForTier(int tier)
    {
        if (detectorSignalRanges == null || detectorSignalRanges.Length == 0)
        {
            return Mathf.Max(0.1f, detectionRange);
        }

        int safeTier = Mathf.Clamp(tier, 0, detectorSignalRanges.Length - 1);
        return Mathf.Max(0.1f, detectorSignalRanges[safeTier]);
    }

    public string GetDetectorName(int tier)
    {
        if (detectorModelNames == null || detectorModelNames.Length == 0)
        {
            return "Detector";
        }

        int safeTier = Mathf.Clamp(tier, 0, detectorModelNames.Length - 1);
        return detectorModelNames[safeTier];
    }

    private DetectableTreasure nearestTreasure;
    private float currentSignal;
    private float currentSignalDistanceMeters = -1f;
    private float beepTimer;
    private DetectableTreasure lastBeepTarget;
    private GroundScanner groundScanner;
    private AudioSource audioSource;
    private AudioClip commonBeepClip;
    private AudioClip rareBeepClip;
    private AudioClip epicBeepClip;
    private DetectableTreasure[] cachedTreasures = new DetectableTreasure[0];
    private float nextSignalRefreshTime;
    private float nextTreasureCacheRefreshTime = -1f;
    private int appliedVisualTier = -1;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        groundScanner = GetComponent<GroundScanner>();
        commonBeepClip = CreateBeepClip(900f);
        rareBeepClip = CreateBeepClip(1150f);
        epicBeepClip = CreateBeepClip(1420f);
        ApplyDetectorTierVisuals();
    }

    private void Update()
    {
        ApplyDetectorTierVisuals();
        UpdateNearestTreasure();
        UpdateBeep();
    }

    private void UpdateNearestTreasure()
    {
        if (DayNightCycle.IsNightNow || !IsScanInputHeld())
        {
            FindNearestTreasure();
            return;
        }

        if (Time.unscaledTime < nextSignalRefreshTime)
        {
            return;
        }

        nextSignalRefreshTime = Time.unscaledTime + SignalRefreshInterval;
        FindNearestTreasure();
    }

    private void FindNearestTreasure()
    {
        if (DayNightCycle.IsNightNow || !IsScanInputHeld())
        {
            ClearSignal();
            return;
        }

        CacheGroundScanner();

        if (groundScanner != null && !groundScanner.CanSignalAtCurrentScan())
        {
            ClearSignal();
            return;
        }

        DetectableTreasure[] treasures = GetDetectableTreasures();
        DetectableTreasure closestTreasure = null;
        float maxSignalDistance = DetectionRange;
        float closestDistance = maxSignalDistance + 1f;

        foreach (DetectableTreasure treasure in treasures)
        {
            if (treasure.isFound)
            {
                continue;
            }

            float distance = GetDistanceToTreasure(treasure);

            if (distance > maxSignalDistance || distance >= closestDistance)
            {
                continue;
            }

            if (groundScanner != null && !groundScanner.CanScanAtWorldPosition(treasure.transform.position))
            {
                continue;
            }

            closestDistance = distance;
            closestTreasure = treasure;
        }

        if (closestTreasure == null)
        {
            ClearSignal();
            return;
        }

        SetSignal(closestTreasure, closestDistance, GetSignalForDistance(closestDistance, maxSignalDistance));
    }

    private DetectableTreasure[] GetDetectableTreasures()
    {
        if (cachedTreasures == null || Time.unscaledTime >= nextTreasureCacheRefreshTime)
        {
            cachedTreasures = FindObjectsByType<DetectableTreasure>();
            nextTreasureCacheRefreshTime = Time.unscaledTime + TreasureCacheRefreshInterval;
        }

        return cachedTreasures;
    }

    private void UpdateBeep()
    {
        if (DayNightCycle.IsNightNow || !IsScanInputHeld() || nearestTreasure == null || currentSignal <= 0f)
        {
            beepTimer = 0f;
            lastBeepTarget = null;
            return;
        }

        if (nearestTreasure != lastBeepTarget)
        {
            beepTimer = 0f;
            lastBeepTarget = nearestTreasure;
        }

        float beepDelay = Mathf.Lerp(farBeepDelay, closeBeepDelay, currentSignal);
        beepTimer -= Time.deltaTime;

        if (beepTimer <= 0f)
        {
            AudioClip beepClip = GetBeepClipForNearestTreasure();

            if (useRealBeepSound && audioSource != null && beepClip != null)
            {
                audioSource.PlayOneShot(beepClip, beepVolume);
            }

            beepTimer = beepDelay;
        }
    }

    private bool IsScanInputHeld()
    {
        return !GameUIState.AnyBlockingUIOpen && Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private void CacheGroundScanner()
    {
        if (groundScanner == null)
        {
            groundScanner = GetComponent<GroundScanner>();
        }
    }

    private void ClearSignal()
    {
        nearestTreasure = null;
        currentSignal = 0f;
        currentSignalDistanceMeters = -1f;
    }

    private void SetSignal(DetectableTreasure treasure, float distanceMeters, float signal)
    {
        nearestTreasure = treasure;
        currentSignalDistanceMeters = distanceMeters;
        currentSignal = Mathf.Clamp01(signal);
    }

    private void ApplyDetectorTierVisuals()
    {
        int tier = DetectorTier;

        if (appliedVisualTier == tier || detectorTierVisuals == null || detectorTierVisuals.Length == 0)
        {
            return;
        }

        for (int i = 0; i < detectorTierVisuals.Length; i++)
        {
            if (detectorTierVisuals[i] == null)
            {
                continue;
            }

            detectorTierVisuals[i].gameObject.SetActive(i == tier);
        }

        appliedVisualTier = tier;
    }

    private static int GetMaxIndex<T>(T[] values)
    {
        return values != null && values.Length > 0 ? values.Length - 1 : 0;
    }

    private float GetDistanceToTreasure(DetectableTreasure treasure)
    {
        if (groundScanner != null)
        {
            return groundScanner.GetDistanceFromCurrentScan(treasure.transform.position);
        }

        Vector3 scanPosition = detectorHead != null ? detectorHead.position : transform.position;
        return GetGroundDistance(scanPosition, treasure.transform.position);
    }

    private float GetSignalForDistance(float distanceMeters, float maxSignalDistance)
    {
        return Mathf.Clamp01(1f - (distanceMeters / Mathf.Max(0.01f, maxSignalDistance)));
    }

    private float GetGroundDistance(Vector3 firstPosition, Vector3 secondPosition)
    {
        Vector2 first = new Vector2(firstPosition.x, firstPosition.z);
        Vector2 second = new Vector2(secondPosition.x, secondPosition.z);
        return Vector2.Distance(first, second);
    }

    private AudioClip GetBeepClipForNearestTreasure()
    {
        if (nearestTreasure == null || !nearestTreasure.isRevealed)
        {
            return commonBeepClip;
        }

        if (nearestTreasure.rarity == TreasureRarity.Epic)
        {
            return epicBeepClip;
        }

        if (nearestTreasure.rarity == TreasureRarity.Rare)
        {
            return rareBeepClip;
        }

        return commonBeepClip;
    }

    private AudioClip CreateBeepClip(float frequency)
    {
        int sampleRate = 44100;
        float duration = 0.08f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float fade = 1f - i / (float)sampleCount;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * fade;
        }

        AudioClip clip = AudioClip.Create("DetectorBeep" + Mathf.RoundToInt(frequency), sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnGUI()
    {
        return;
    }
}
