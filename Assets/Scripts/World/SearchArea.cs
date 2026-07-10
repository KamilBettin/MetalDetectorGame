using UnityEngine;

public class SearchArea : MonoBehaviour
{
    public string areaName = "Search Area";
    public int unlockCost = 650;
    public bool isUnlocked;
    public Vector2 size = new Vector2(24f, 24f);
    public Color areaColor = new Color(0.25f, 0.8f, 0.45f, 0.28f);
    public TreasureLootPool lootPool = TreasureLootPool.SpecialField;
    public GameObject[] lockedObjects;
    public GameObject[] unlockedObjects;

    private bool hasSpawnedTreasures;

    public Vector3 Center => transform.position;
    public string MultiplayerId => GetMultiplayerId();

    private void Start()
    {
        RefreshVisualState();
    }

    public bool TryUnlock(PlayerInventory playerInventory, out string message)
    {
        if (isUnlocked)
        {
            message = GameLocalization.TFormat("area.already_unlocked", areaName);
            return false;
        }

        if (playerInventory == null)
        {
            message = GameLocalization.T("area.no_inventory");
            return false;
        }

        if (!playerInventory.TrySpendMoney(unlockCost))
        {
            message = GameLocalization.TFormat("area.need_money", unlockCost, areaName);
            return false;
        }

        SetUnlocked(true);
        message = GameLocalization.TFormat("area.unlocked", areaName);
        return true;
    }

    public void SetUnlocked(bool unlocked)
    {
        SetUnlocked(unlocked, true);
    }

    public void SetUnlocked(bool unlocked, bool notifyMultiplayer)
    {
        isUnlocked = unlocked;
        RefreshVisualState();

        if (isUnlocked)
        {
            SpawnTreasuresIfNeeded();

            if (notifyMultiplayer)
            {
                LocalCoopManager.Instance?.ReportAreaUnlocked(this);
            }
        }
    }

    public bool Contains(Vector3 worldPosition)
    {
        Vector3 center = Center;
        float halfWidth = size.x * 0.5f;
        float halfDepth = size.y * 0.5f;
        return worldPosition.x >= center.x - halfWidth
            && worldPosition.x <= center.x + halfWidth
            && worldPosition.z >= center.z - halfDepth
            && worldPosition.z <= center.z + halfDepth;
    }

    public Vector3 GetRandomPoint(float yPosition)
    {
        return new Vector3(
            Random.Range(Center.x - size.x * 0.5f, Center.x + size.x * 0.5f),
            yPosition,
            Random.Range(Center.z - size.y * 0.5f, Center.z + size.y * 0.5f)
        );
    }

    public void MarkTreasuresSpawned()
    {
        hasSpawnedTreasures = true;
    }

    public void ClearTreasureSpawnState()
    {
        hasSpawnedTreasures = false;
        RefreshVisualState();
    }

    public bool CanSpawnTreasures()
    {
        return isUnlocked && !hasSpawnedTreasures;
    }

    public void ResetForNewDay()
    {
        hasSpawnedTreasures = false;
        RefreshVisualState();

        if (isUnlocked)
        {
            SpawnTreasuresIfNeeded();
        }
    }

    private void SpawnTreasuresIfNeeded()
    {
        if (hasSpawnedTreasures)
        {
            return;
        }

        TreasureSpawner treasureSpawner = FindAnyObjectByType<TreasureSpawner>();

        if (treasureSpawner != null)
        {
            treasureSpawner.SpawnTreasuresForArea(this);
        }
    }

    private string GetMultiplayerId()
    {
        Vector3 center = Center;
        return areaName + "@" + Mathf.RoundToInt(center.x) + "," + Mathf.RoundToInt(center.z);
    }

    private void RefreshVisualState()
    {
        SetObjectsActive(lockedObjects, !isUnlocked);
        SetObjectsActive(unlockedObjects, isUnlocked);
    }

    private void SetObjectsActive(GameObject[] objects, bool isActive)
    {
        if (objects == null)
        {
            return;
        }

        foreach (GameObject target in objects)
        {
            if (target != null)
            {
                target.SetActive(isActive);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isUnlocked ? areaColor : new Color(1f, 0.45f, 0.1f, 0.35f);
        Gizmos.DrawCube(Center + Vector3.up * 0.08f, new Vector3(size.x, 0.16f, size.y));
    }
}
