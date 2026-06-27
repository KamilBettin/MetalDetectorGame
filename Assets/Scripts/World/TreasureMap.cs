using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TreasureMap : MonoBehaviour
{
    public Transform player;
    public Vector2 worldSize = new Vector2(1000f, 1000f);
    public bool showSmallMap;
    public bool showLargeMap;

    private readonly List<Vector3> checkedPositions = new List<Vector3>();
    public IReadOnlyList<Vector3> CheckedPositions => checkedPositions;

    private void Awake()
    {
        if (player == null)
        {
            player = transform;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            showLargeMap = !showLargeMap;
        }
    }

    public void RegisterFoundTreasure(Vector3 worldPosition)
    {
        RegisterCheckedSpot(worldPosition);
    }

    public void RegisterCheckedSpot(Vector3 worldPosition)
    {
        checkedPositions.Add(worldPosition);
    }

    private void OnGUI()
    {
        if (RuntimeGameUI.IsActive)
        {
            return;
        }

        if (showLargeMap)
        {
            DrawMap(new Rect(20, 80, 420, 420), true);
            return;
        }

        if (showSmallMap)
        {
            DrawMap(new Rect(Screen.width - 250, Screen.height - 250, 230, 230), false);
        }
    }

    private void DrawMap(Rect rect, bool large)
    {
        GameGui.DrawPanel(rect, "Search Map " + Mathf.RoundToInt(worldSize.x) + "x" + Mathf.RoundToInt(worldSize.y));

        Rect mapRect = new Rect(rect.x + 14f, rect.y + 28f, rect.width - 28f, rect.height - 42f);
        GameGui.DrawRect(mapRect, new Color(0.1f, 0.12f, 0.1f, 0.8f));

        DrawGrid(mapRect, large ? 10 : 5);

        foreach (Vector3 position in checkedPositions)
        {
            Vector2 mapPosition = WorldToMapPosition(position, mapRect);
            GameGui.DrawRect(new Rect(mapPosition.x - 3f, mapPosition.y - 3f, 6f, 6f), new Color(0.7f, 0.66f, 0.56f, 0.9f));
        }

        if (player != null)
        {
            Vector2 playerPosition = WorldToMapPosition(player.position, mapRect);
            GameGui.DrawRect(new Rect(playerPosition.x - 5f, playerPosition.y - 5f, 10f, 10f), GameGui.AccentColor);
        }

        if (large)
        {
            GUI.Label(new Rect(rect.x + 16f, rect.y + rect.height - 24f, rect.width - 32f, 20f), "Yellow = you | Gray = checked/found spot | M = close", GameGui.SmallLabelStyle);
        }
    }

    private void DrawGrid(Rect mapRect, int divisions)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 0.84f, 0.42f, 0.24f);

        for (int i = 1; i < divisions; i++)
        {
            float x = mapRect.x + mapRect.width * i / divisions;
            float y = mapRect.y + mapRect.height * i / divisions;
            GUI.Box(new Rect(x, mapRect.y, 1f, mapRect.height), "");
            GUI.Box(new Rect(mapRect.x, y, mapRect.width, 1f), "");
        }

        GUI.color = oldColor;
    }

    private Vector2 WorldToMapPosition(Vector3 worldPosition, Rect mapRect)
    {
        float normalizedX = Mathf.InverseLerp(-worldSize.x * 0.5f, worldSize.x * 0.5f, worldPosition.x);
        float normalizedY = Mathf.InverseLerp(-worldSize.y * 0.5f, worldSize.y * 0.5f, worldPosition.z);

        float x = mapRect.x + normalizedX * mapRect.width;
        float y = mapRect.y + (1f - normalizedY) * mapRect.height;

        return new Vector2(x, y);
    }
}
