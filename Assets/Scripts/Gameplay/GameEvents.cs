using UnityEngine;

public static class GameEvents
{
    public static event System.Action<int> ScannedCellsChanged;
    public static event System.Action<DetectableTreasure> TreasureFound;
    public static event System.Action<int> TreasuresSold;

    public static void ReportScannedCells(int totalScannedCells)
    {
        ScannedCellsChanged?.Invoke(totalScannedCells);
    }

    public static void ReportTreasureFound(DetectableTreasure treasure)
    {
        TreasureFound?.Invoke(treasure);
    }

    public static void ReportTreasuresSold(int soldCount)
    {
        TreasuresSold?.Invoke(soldCount);
    }
}
