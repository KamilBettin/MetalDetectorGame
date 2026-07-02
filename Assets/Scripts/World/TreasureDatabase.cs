using UnityEngine;

[CreateAssetMenu(menuName = "Metal Detector/Treasure Database")]
public class TreasureDatabase : ScriptableObject
{
    public TreasureDefinition[] treasures =
    {
        new TreasureDefinition { treasureName = "Rusty Bottle Cap", value = 1, rarity = TreasureRarity.Common, spawnWeight = 30, width = 1, height = 1, minDigHits = 2, maxDigHits = 3 },
        new TreasureDefinition { treasureName = "Old Nail", value = 2, rarity = TreasureRarity.Common, spawnWeight = 24, width = 1, height = 1, minDigHits = 2, maxDigHits = 3 },
        new TreasureDefinition { treasureName = "Pull Tab", value = 3, rarity = TreasureRarity.Common, spawnWeight = 22, width = 1, height = 1, minDigHits = 2, maxDigHits = 3 },
        new TreasureDefinition { treasureName = "Small Coin", value = 8, rarity = TreasureRarity.Common, spawnWeight = 18, width = 1, height = 1, minDigHits = 3, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Bent Spoon", value = 9, rarity = TreasureRarity.Common, spawnWeight = 13, width = 1, height = 1, minDigHits = 3, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Lost Key", value = 14, rarity = TreasureRarity.Rare, spawnWeight = 10, width = 1, height = 1, minDigHits = 3, maxDigHits = 5 },
        new TreasureDefinition { treasureName = "Watch Fragment", value = 14, rarity = TreasureRarity.Common, spawnWeight = 16, width = 1, height = 1, minDigHits = 3, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Pocket Watch", value = 32, rarity = TreasureRarity.Rare, spawnWeight = 7, width = 1, height = 1, minDigHits = 4, maxDigHits = 5 },
        new TreasureDefinition { treasureName = "Silver Ring", value = 45, rarity = TreasureRarity.Rare, spawnWeight = 6, width = 1, height = 1, minDigHits = 4, maxDigHits = 6 },
        new TreasureDefinition { treasureName = "Old Dagger", value = 70, rarity = TreasureRarity.Rare, spawnWeight = 4, width = 1, height = 1, minDigHits = 4, maxDigHits = 6 },
        new TreasureDefinition { treasureName = "Gold Ring", value = 120, rarity = TreasureRarity.Epic, spawnWeight = 3, width = 1, height = 1, minDigHits = 5, maxDigHits = 7 },
        new TreasureDefinition { treasureName = "Jeweled Compass", value = 180, rarity = TreasureRarity.Epic, spawnWeight = 2, width = 1, height = 1, minDigHits = 5, maxDigHits = 7 },
        new TreasureDefinition { treasureName = "Ancient Relic", value = 260, rarity = TreasureRarity.Epic, spawnWeight = 1, width = 1, height = 1, minDigHits = 6, maxDigHits = 8 }
    };

    public TreasureDefinition[] generalTerrainTreasures =
    {
        new TreasureDefinition { treasureName = "Rusty Bottle Cap", value = 1, rarity = TreasureRarity.Common, spawnWeight = 34, width = 1, height = 1, minDigHits = 2, maxDigHits = 3 },
        new TreasureDefinition { treasureName = "Old Nail", value = 2, rarity = TreasureRarity.Common, spawnWeight = 30, width = 1, height = 1, minDigHits = 2, maxDigHits = 3 },
        new TreasureDefinition { treasureName = "Pull Tab", value = 3, rarity = TreasureRarity.Common, spawnWeight = 28, width = 1, height = 1, minDigHits = 2, maxDigHits = 3 },
        new TreasureDefinition { treasureName = "Crushed Can", value = 4, rarity = TreasureRarity.Common, spawnWeight = 22, width = 1, height = 1, minDigHits = 3, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Small Coin", value = 8, rarity = TreasureRarity.Common, spawnWeight = 15, width = 1, height = 1, minDigHits = 3, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Bent Spoon", value = 9, rarity = TreasureRarity.Common, spawnWeight = 11, width = 1, height = 1, minDigHits = 3, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Watch Fragment", value = 14, rarity = TreasureRarity.Common, spawnWeight = 9, width = 1, height = 1, minDigHits = 3, maxDigHits = 4 }
    };

    public TreasureDefinition[] searchAreaTreasures =
    {
        new TreasureDefinition { treasureName = "Lost Key", value = 14, rarity = TreasureRarity.Rare, spawnWeight = 16, width = 1, height = 1, minDigHits = 3, maxDigHits = 5 },
        new TreasureDefinition { treasureName = "Watch Fragment", value = 14, rarity = TreasureRarity.Common, spawnWeight = 18, width = 1, height = 1, minDigHits = 3, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Pocket Watch", value = 32, rarity = TreasureRarity.Rare, spawnWeight = 12, width = 1, height = 1, minDigHits = 4, maxDigHits = 5 },
        new TreasureDefinition { treasureName = "Silver Ring", value = 45, rarity = TreasureRarity.Rare, spawnWeight = 10, width = 1, height = 1, minDigHits = 4, maxDigHits = 6 },
        new TreasureDefinition { treasureName = "Old Dagger", value = 70, rarity = TreasureRarity.Rare, spawnWeight = 7, width = 1, height = 1, minDigHits = 4, maxDigHits = 6 },
        new TreasureDefinition { treasureName = "Gold Ring", value = 120, rarity = TreasureRarity.Epic, spawnWeight = 4, width = 1, height = 1, minDigHits = 5, maxDigHits = 7 },
        new TreasureDefinition { treasureName = "Jeweled Compass", value = 180, rarity = TreasureRarity.Epic, spawnWeight = 2, width = 1, height = 1, minDigHits = 5, maxDigHits = 7 },
        new TreasureDefinition { treasureName = "Ancient Relic", value = 260, rarity = TreasureRarity.Epic, spawnWeight = 1, width = 1, height = 1, minDigHits = 6, maxDigHits = 8 }
    };

    public TreasureDefinition GetRandomTreasure()
    {
        return GetRandomTreasure(TreasureLootPool.Any);
    }

    public TreasureDefinition GetRandomTreasure(TreasureLootPool lootPool)
    {
        TreasureDefinition[] poolTreasures = GetTreasuresForPool(lootPool);
        TreasureDefinition chosenTreasure = GetRandomTreasureFrom(poolTreasures);

        if (chosenTreasure != null)
        {
            return chosenTreasure;
        }

        return GetRandomTreasureFrom(treasures);
    }

    private TreasureDefinition[] GetTreasuresForPool(TreasureLootPool lootPool)
    {
        if (lootPool == TreasureLootPool.GeneralTerrain)
        {
            return HasTreasures(generalTerrainTreasures) ? generalTerrainTreasures : GetFallbackGeneralTreasures();
        }

        if (lootPool == TreasureLootPool.SearchArea)
        {
            return HasTreasures(searchAreaTreasures) ? searchAreaTreasures : GetFallbackSearchAreaTreasures();
        }

        return treasures;
    }

    private TreasureDefinition[] GetFallbackGeneralTreasures()
    {
        if (!HasTreasures(treasures))
        {
            return null;
        }

        System.Collections.Generic.List<TreasureDefinition> fallbackTreasures = new System.Collections.Generic.List<TreasureDefinition>();

        foreach (TreasureDefinition treasure in treasures)
        {
            if (treasure != null && treasure.rarity == TreasureRarity.Common && treasure.value <= 20)
            {
                fallbackTreasures.Add(treasure);
            }
        }

        return fallbackTreasures.ToArray();
    }

    private TreasureDefinition[] GetFallbackSearchAreaTreasures()
    {
        if (!HasTreasures(treasures))
        {
            return null;
        }

        System.Collections.Generic.List<TreasureDefinition> fallbackTreasures = new System.Collections.Generic.List<TreasureDefinition>();

        foreach (TreasureDefinition treasure in treasures)
        {
            if (treasure != null && (treasure.rarity != TreasureRarity.Common || treasure.value >= 10))
            {
                fallbackTreasures.Add(treasure);
            }
        }

        return fallbackTreasures.ToArray();
    }

    private bool HasTreasures(TreasureDefinition[] treasureList)
    {
        return treasureList != null && treasureList.Length > 0;
    }

    private TreasureDefinition GetRandomTreasureFrom(TreasureDefinition[] treasureList)
    {
        if (treasureList == null || treasureList.Length == 0)
        {
            return null;
        }

        int totalWeight = 0;

        foreach (TreasureDefinition treasure in treasureList)
        {
            if (treasure != null)
            {
                totalWeight += Mathf.Max(0, treasure.spawnWeight);
            }
        }

        if (totalWeight <= 0)
        {
            return treasureList[Random.Range(0, treasureList.Length)];
        }

        int roll = Random.Range(0, totalWeight);

        foreach (TreasureDefinition treasure in treasureList)
        {
            if (treasure == null)
            {
                continue;
            }

            roll -= Mathf.Max(0, treasure.spawnWeight);

            if (roll < 0)
            {
                return treasure;
            }
        }

        return treasureList[treasureList.Length - 1];
    }

    public static TreasureDatabase CreateRuntimeDefault()
    {
        return CreateInstance<TreasureDatabase>();
    }
}

public enum TreasureLootPool
{
    Any,
    GeneralTerrain,
    SearchArea
}
