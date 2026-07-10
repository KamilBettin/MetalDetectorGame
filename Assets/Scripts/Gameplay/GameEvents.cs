using UnityEngine;

public static class GameEvents
{
    public static event System.Action<int> GroundScansChanged;
    public static event System.Action<DetectableTreasure> TreasureFound;
    public static event System.Action<int> TreasuresSold;

    public static void ReportGroundScans(int totalScans)
    {
        GroundScansChanged?.Invoke(totalScans);
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
