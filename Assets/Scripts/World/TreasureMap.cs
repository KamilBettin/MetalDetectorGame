using System.Collections.Generic;
using UnityEngine;

public class TreasureMap : MonoBehaviour
{
    public Transform player;
    public Vector2 worldSize = new Vector2(1000f, 1000f);
    public bool showSmallMap;
    public bool showLargeMap;

    private readonly List<Vector3> checkedPositions = new List<Vector3>();
    public IReadOnlyList<Vector3> CheckedPositions => checkedPositions;

    private void OnEnable()
    {
        showSmallMap = false;
        showLargeMap = false;
    }

    public void RegisterFoundTreasure(Vector3 worldPosition)
    {
    }

    public void RegisterCheckedSpot(Vector3 worldPosition)
    {
    }
}
