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
        "TwinSweep Pro",
        "Auric Titan"
    };
    public int[] detectorScanSizes = { 2, 3, 4, 5, 6 };

    public float DetectionRange => detectionRange;
    public float CurrentSignal => currentSignal;
    public DetectableTreasure NearestTreasure => nearestTreasure;
    public int CurrentSignalRangeCells => GetCurrentSignalRangeCells();
    public int CurrentSignalCellDistance => currentSignalCellDistance;
    public float CurrentSignalDistanceMeters => currentSignalCellDistance < 0 ? -1f : currentSignalCellDistance * GetSignalCellSize();
    public bool RevealSignalActive => nearestTreasure != null && nearestTreasure.isRevealed && !nearestTreasure.isFound;
    public int DetectorTier => Mathf.Clamp(detectorTier, 0, MaxDetectorTier);
    public int MaxDetectorTier => detectorScanSizes != null && detectorScanSizes.Length > 0 ? detectorScanSizes.Length - 1 : 0;
    public int CurrentScanCells => GetScanCellsForTier(DetectorTier);
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
        currentSignalCellDistance = 0;
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
        return true;
    }

    public int GetScanCellsForTier(int tier)
    {
        if (detectorScanSizes == null || detectorScanSizes.Length == 0)
        {
            return 2;
        }

        int safeTier = Mathf.Clamp(tier, 0, detectorScanSizes.Length - 1);
        return Mathf.Max(1, detectorScanSizes[safeTier]);
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
    private int currentSignalCellDistance = -1;
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
    }

    private void Update()
    {
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
        int maxSignalCells = CurrentSignalRangeCells;
        int closestCellDistance = maxSignalCells + 1;

        foreach (DetectableTreasure treasure in treasures)
        {
            if (treasure.isFound)
            {
                continue;
            }

            int cellDistance = GetCellDistanceToTreasure(treasure);

            if (cellDistance > maxSignalCells || cellDistance >= closestCellDistance)
            {
                continue;
            }

            if (groundScanner != null && !groundScanner.CanScanAtWorldPosition(treasure.transform.position))
            {
                continue;
            }

            closestCellDistance = cellDistance;
            closestTreasure = treasure;
        }

        if (closestTreasure == null)
        {
            ClearSignal();
            return;
        }

        SetSignal(closestTreasure, closestCellDistance, GetSignalForCellDistance(closestCellDistance, maxSignalCells));
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
        return !GameUIState.AnyMenuOpen && Mouse.current != null && Mouse.current.leftButton.isPressed;
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
        currentSignalCellDistance = -1;
    }

    private void SetSignal(DetectableTreasure treasure, int cellDistance, float signal)
    {
        nearestTreasure = treasure;
        currentSignalCellDistance = cellDistance;
        currentSignal = Mathf.Clamp01(signal);
    }

    private int GetCellDistanceToTreasure(DetectableTreasure treasure)
    {
        if (groundScanner != null)
        {
            return groundScanner.GetCellDistanceFromCurrentScan(treasure.transform.position);
        }

        Vector3 scanPosition = detectorHead != null ? detectorHead.position : transform.position;
        return Mathf.CeilToInt(GetGroundDistance(scanPosition, treasure.transform.position));
    }

    private float GetSignalForCellDistance(int cellDistance, int maxSignalCells)
    {
        return Mathf.Clamp01((maxSignalCells - cellDistance + 1f) / (maxSignalCells + 1f));
    }

    private int GetCurrentSignalRangeCells()
    {
        return Mathf.Max(1, Mathf.CeilToInt(detectionRange / GetSignalCellSize()));
    }

    private float GetSignalCellSize()
    {
        CacheGroundScanner();
        return groundScanner != null && groundScanner.gridCellSize > 0f ? groundScanner.gridCellSize : 1f;
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
        if (RuntimeGameUI.IsActive)
        {
            return;
        }

        Rect panelRect = new Rect(20, 20, 300, 112);
        string stateText = nearestTreasure == null
            ? "No buried signal nearby"
            : RevealSignalActive ? "Target marked"
            : currentSignalCellDistance <= 0 ? "Scan here to mark target" : "Buried ping: " + CurrentSignalDistanceMeters.ToString("0.0") + "m";
        Color signalColor = currentSignal > 0.8f
            ? GameGui.GoodColor
            : Color.Lerp(GameGui.MutedTextColor, GameGui.AccentColor, currentSignal);

        GameGui.DrawPanel(panelRect, "Detector");
        GUI.Label(new Rect(36, 54, 248, 20), stateText, GameGui.SmallLabelStyle);
        GameGui.DrawProgressBar(
            new Rect(36, 80, 248, 18),
            currentSignal,
            signalColor,
            "Signal " + Mathf.RoundToInt(currentSignal * 100f) + "%"
        );
        GUI.Label(new Rect(36, 104, 248, 18), "LMB scan | E dig | TAB backpack | M map", GameGui.SmallLabelStyle);

        if (nearestTreasure != null && RevealSignalActive)
        {
            GameGui.DrawToast(new Rect(20, 144, 300, 38), "Target marked. Dig under the arrow.");
        }
    }
}
