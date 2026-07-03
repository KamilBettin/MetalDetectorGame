using UnityEngine;

public class TutorialQuestSystem : MonoBehaviour
{
    public int scannedCellsGoal = 5;

    public int scannedCells;
    public bool foundFirstTreasure;
    public bool soldFirstLoot;

    public string CurrentObjective
    {
        get
        {
            if (scannedCells < scannedCellsGoal)
            {
                return GameLocalization.TFormat("tutorial.scan_sand", scannedCells, scannedCellsGoal);
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
            if (scannedCells < scannedCellsGoal)
            {
                return scannedCellsGoal <= 0 ? 1f : Mathf.Clamp01(scannedCells / (float)scannedCellsGoal);
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
        GameEvents.ScannedCellsChanged += HandleScannedCellsChanged;
        GameEvents.TreasureFound += HandleTreasureFound;
        GameEvents.TreasuresSold += HandleTreasuresSold;
    }

    private void OnDisable()
    {
        GameEvents.ScannedCellsChanged -= HandleScannedCellsChanged;
        GameEvents.TreasureFound -= HandleTreasureFound;
        GameEvents.TreasuresSold -= HandleTreasuresSold;
    }

    private void HandleScannedCellsChanged(int totalScannedCells)
    {
        scannedCells = Mathf.Max(scannedCells, totalScannedCells);
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
