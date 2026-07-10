using UnityEngine;
using UnityEngine.Serialization;

public class TutorialQuestSystem : MonoBehaviour
{
    [FormerlySerializedAs("scannedCellsGoal")]
    public int groundScansGoal = 5;

    [FormerlySerializedAs("scannedCells")]
    public int groundScans;
    public bool foundFirstTreasure;
    public bool soldFirstLoot;

    public string CurrentObjective
    {
        get
        {
            if (groundScans < groundScansGoal)
            {
                return GameLocalization.TFormat("tutorial.scan_sand", groundScans, groundScansGoal);
            }

            if (!foundFirstTreasure)
            {
                return GameLocalization.T("tutorial.find_first");
            }

            if (!soldFirstLoot)
            {
                return GameLocalization.T("tutorial.sell_loot");
            }

            return GameLocalization.T("tutorial.complete");
        }
    }

    public float Progress01
    {
        get
        {
            if (groundScans < groundScansGoal)
            {
                return groundScansGoal <= 0 ? 1f : Mathf.Clamp01(groundScans / (float)groundScansGoal);
            }

            if (!foundFirstTreasure)
            {
                return 0.5f;
            }

            return soldFirstLoot ? 1f : 0.75f;
        }
    }

    private void OnEnable()
    {
        GameEvents.GroundScansChanged += HandleGroundScansChanged;
        GameEvents.TreasureFound += HandleTreasureFound;
        GameEvents.TreasuresSold += HandleTreasuresSold;
    }

    private void OnDisable()
    {
        GameEvents.GroundScansChanged -= HandleGroundScansChanged;
        GameEvents.TreasureFound -= HandleTreasureFound;
        GameEvents.TreasuresSold -= HandleTreasuresSold;
    }

    private void HandleGroundScansChanged(int totalScans)
    {
        groundScans = Mathf.Max(groundScans, totalScans);
    }

    private void HandleTreasureFound(DetectableTreasure treasure)
    {
        foundFirstTreasure = true;
    }

    private void HandleTreasuresSold(int soldCount)
    {
        if (soldCount > 0)
        {
            soldFirstLoot = true;
        }
    }
}
