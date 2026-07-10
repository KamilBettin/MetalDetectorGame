using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Metal Detector/Treasure Database")]
public class TreasureDatabase : ScriptableObject
{
    [Header("Default")]
    public TreasureDefinition[] defaultTreasures =
    {
        new TreasureDefinition { treasureName = "Rusty Bottle Cap", value = 3, rarity = TreasureRarity.Common, spawnWeight = 30, width = 1, height = 1, minDigHits = 2, maxDigHits = 3 },
        new TreasureDefinition { treasureName = "Old Nail", value = 3, rarity = TreasureRarity.Common, spawnWeight = 24, width = 1, height = 1, minDigHits = 2, maxDigHits = 3 },
        new TreasureDefinition { treasureName = "Small Coin", value = 8, rarity = TreasureRarity.Common, spawnWeight = 18, width = 1, height = 1, minDigHits = 3, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Bent Spoon", value = 9, rarity = TreasureRarity.Common, spawnWeight = 13, width = 1, height = 1, minDigHits = 3, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Watch Fragment", value = 12, rarity = TreasureRarity.Common, spawnWeight = 34, width = 1, height = 1, minDigHits = 2, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Watch Case", value = 16, rarity = TreasureRarity.Common, spawnWeight = 30, width = 1, height = 1, minDigHits = 2, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Horseshoe Fragment", value = 11, rarity = TreasureRarity.Common, spawnWeight = 32, width = 1, height = 1, minDigHits = 2, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Metal Rod", value = 8, rarity = TreasureRarity.Common, spawnWeight = 34, width = 1, height = 1, minDigHits = 2, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Old Cracked Compass", value = 16, rarity = TreasureRarity.Common, spawnWeight = 28, width = 1, height = 1, minDigHits = 2, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Broken Chain", value = 9, rarity = TreasureRarity.Common, spawnWeight = 34, width = 1, height = 1, minDigHits = 2, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Chain Piece", value = 9, rarity = TreasureRarity.Common, spawnWeight = 34, width = 1, height = 1, minDigHits = 2, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Chain Link", value = 9, rarity = TreasureRarity.Common, spawnWeight = 34, width = 1, height = 1, minDigHits = 2, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Cracked Glass Locket", value = 16, rarity = TreasureRarity.Common, spawnWeight = 28, width = 1, height = 1, minDigHits = 2, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Metal Photo Frame", value = 12, rarity = TreasureRarity.Common, spawnWeight = 30, width = 1, height = 1, minDigHits = 2, maxDigHits = 4 }
    };

    [Header("Default + 1 Upgrade")]
    public TreasureDefinition[] defaultPlusOneUpgradeTreasures =
    {
        new TreasureDefinition { treasureName = "Silver Ring", value = 60, rarity = TreasureRarity.Rare, spawnWeight = 6, width = 1, height = 1, minDigHits = 4, maxDigHits = 6 }
    };

    [Header("Default + 2 Upgrades")]
    public TreasureDefinition[] defaultPlusTwoUpgradeTreasures =
    {
        new TreasureDefinition { treasureName = "Old Dagger", value = 110, rarity = TreasureRarity.Rare, spawnWeight = 4, width = 1, height = 1, minDigHits = 4, maxDigHits = 6 },
        new TreasureDefinition { treasureName = "Gold Ring", value = 210, rarity = TreasureRarity.Epic, spawnWeight = 3, width = 1, height = 1, minDigHits = 5, maxDigHits = 7 }
    };

    [Header("Default + 3 Upgrades")]
    public TreasureDefinition[] defaultPlusThreeUpgradeTreasures =
    {
        new TreasureDefinition { treasureName = "Ancient Relic", value = 300, rarity = TreasureRarity.Epic, spawnWeight = 1, width = 1, height = 1, minDigHits = 6, maxDigHits = 8 }
    };

    [Header("Special Field")]
    public TreasureDefinition[] specialFieldTreasures =
    {
        new TreasureDefinition { treasureName = "Medallion", value = 75, rarity = TreasureRarity.Rare, spawnWeight = 10, width = 1, height = 1, minDigHits = 4, maxDigHits = 6 },
        new TreasureDefinition { treasureName = "Cross Pendant", value = 80, rarity = TreasureRarity.Rare, spawnWeight = 10, width = 1, height = 1, minDigHits = 4, maxDigHits = 6 },
        new TreasureDefinition { treasureName = "Heart Pendant", value = 85, rarity = TreasureRarity.Rare, spawnWeight = 8, width = 1, height = 1, minDigHits = 4, maxDigHits = 6 },
        new TreasureDefinition { treasureName = "Time Capsule", value = 70, rarity = TreasureRarity.Rare, spawnWeight = 5, width = 1, height = 1, minDigHits = 4, maxDigHits = 7 }
    };

    [Header("Special Tree Field")]
    public TreasureDefinition[] specialTreeFieldTreasures =
    {
        new TreasureDefinition { treasureName = "Ancient Relic", value = 300, rarity = TreasureRarity.Epic, spawnWeight = 2, width = 1, height = 1, minDigHits = 6, maxDigHits = 8 }
    };

    [Header("Icon Only")]
    public TreasureDefinition[] iconOnlyTreasures =
    {
        new TreasureDefinition { treasureName = "Pocket Watch", value = 120, rarity = TreasureRarity.Rare, spawnWeight = 0, width = 2, height = 1, minDigHits = 0, maxDigHits = 0 },
        new TreasureDefinition { treasureName = "Horseshoe", value = 45, rarity = TreasureRarity.Common, spawnWeight = 0, width = 2, height = 1, minDigHits = 0, maxDigHits = 0 },
        new TreasureDefinition { treasureName = "Compass", value = 85, rarity = TreasureRarity.Rare, spawnWeight = 0, width = 1, height = 1, minDigHits = 0, maxDigHits = 0 },
        new TreasureDefinition { treasureName = "Bracelet", value = 55, rarity = TreasureRarity.Common, spawnWeight = 0, width = 2, height = 1, minDigHits = 0, maxDigHits = 0 },
        new TreasureDefinition { treasureName = "Closed Portrait Locket", value = 110, rarity = TreasureRarity.Rare, spawnWeight = 0, width = 1, height = 1, minDigHits = 0, maxDigHits = 0 }
    };

    [HideInInspector]
    public TreasureDefinition[] treasures;

    [HideInInspector]
    public TreasureDefinition[] generalTerrainTreasures;

    [HideInInspector]
    public TreasureDefinition[] searchAreaTreasures;

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

    public TreasureDefinition GetRandomTreasureForSearchArea(TreasureLootPool areaLootPool, int detectorTier)
    {
        TreasureDefinition[] mixedTreasures = GetTreasuresForSearchArea(areaLootPool, detectorTier);
        TreasureDefinition chosenTreasure = GetRandomTreasureFrom(mixedTreasures);

        if (chosenTreasure != null)
        {
            return chosenTreasure;
        }

        return GetRandomTreasure(areaLootPool);
    }

    public int GetFindableTreasureCount(TreasureLootPool lootPool)
    {
        return CountFindableTreasures(GetTreasuresForPool(lootPool));
    }

    public int GetFindableTreasureCountForSearchArea(TreasureLootPool areaLootPool, int detectorTier)
    {
        return CountFindableTreasures(GetTreasuresForSearchArea(areaLootPool, detectorTier));
    }

    private TreasureDefinition[] GetTreasuresForPool(TreasureLootPool lootPool)
    {
        return lootPool switch
        {
            TreasureLootPool.Default => GetDefaultTreasures(),
            TreasureLootPool.DefaultPlusOneUpgrade => CombinePools(GetDefaultTreasures(), GetUpgradeOneTreasures()),
            TreasureLootPool.DefaultPlusTwoUpgrades => CombinePools(GetDefaultTreasures(), GetUpgradeOneTreasures(), GetUpgradeTwoTreasures()),
            TreasureLootPool.DefaultPlusThreeUpgrades => CombinePools(GetDefaultTreasures(), GetUpgradeOneTreasures(), GetUpgradeTwoTreasures(), GetUpgradeThreeTreasures()),
            TreasureLootPool.SpecialField => GetSpecialFieldTreasures(),
            TreasureLootPool.SpecialTreeField => GetSpecialTreeFieldTreasures(),
            _ => GetAllTreasures()
        };
    }

    private TreasureDefinition[] GetTreasuresForDetectorTier(int detectorTier)
    {
        int safeTier = Mathf.Max(0, detectorTier);

        if (safeTier >= 3)
        {
            return GetTreasuresForPool(TreasureLootPool.DefaultPlusThreeUpgrades);
        }

        if (safeTier == 2)
        {
            return GetTreasuresForPool(TreasureLootPool.DefaultPlusTwoUpgrades);
        }

        if (safeTier == 1)
        {
            return GetTreasuresForPool(TreasureLootPool.DefaultPlusOneUpgrade);
        }

        return GetTreasuresForPool(TreasureLootPool.Default);
    }

    private TreasureDefinition[] GetTreasuresForSearchArea(TreasureLootPool areaLootPool, int detectorTier)
    {
        TreasureDefinition[] progressionTreasures = GetTreasuresForDetectorTier(detectorTier);
        TreasureDefinition[] areaTreasures = GetSearchAreaBonusTreasures(areaLootPool);
        return CombinePools(progressionTreasures, areaTreasures);
    }

    private TreasureDefinition[] GetSearchAreaBonusTreasures(TreasureLootPool areaLootPool)
    {
        return areaLootPool switch
        {
            TreasureLootPool.SpecialTreeField => GetSpecialTreeFieldTreasures(),
            TreasureLootPool.SpecialField => GetSpecialFieldTreasures(),
            _ => GetTreasuresForPool(areaLootPool)
        };
    }

    public TreasureDefinition[] GetAllTreasures()
    {
        TreasureDefinition[] allCategorizedTreasures = CombinePools(
            GetDefaultTreasures(),
            GetUpgradeOneTreasures(),
            GetUpgradeTwoTreasures(),
            GetUpgradeThreeTreasures(),
            GetSpecialFieldTreasures(),
            GetSpecialTreeFieldTreasures()
        );

        return HasTreasures(allCategorizedTreasures) ? allCategorizedTreasures : treasures;
    }

    public TreasureDefinition[] GetAllIconDefinitions()
    {
        return CombinePools(GetAllTreasures(), iconOnlyTreasures);
    }

    public bool HasAnyTreasures()
    {
        return HasTreasures(GetAllTreasures());
    }

    private TreasureDefinition[] GetDefaultTreasures()
    {
        if (HasTreasures(defaultTreasures))
        {
            return defaultTreasures;
        }

        return HasTreasures(generalTerrainTreasures) ? generalTerrainTreasures : GetFallbackDefaultTreasures();
    }

    private TreasureDefinition[] GetUpgradeOneTreasures()
    {
        return HasTreasures(defaultPlusOneUpgradeTreasures) ? defaultPlusOneUpgradeTreasures : null;
    }

    private TreasureDefinition[] GetUpgradeTwoTreasures()
    {
        return HasTreasures(defaultPlusTwoUpgradeTreasures) ? defaultPlusTwoUpgradeTreasures : null;
    }

    private TreasureDefinition[] GetUpgradeThreeTreasures()
    {
        return HasTreasures(defaultPlusThreeUpgradeTreasures) ? defaultPlusThreeUpgradeTreasures : null;
    }

    private TreasureDefinition[] GetSpecialFieldTreasures()
    {
        if (HasTreasures(specialFieldTreasures))
        {
            return specialFieldTreasures;
        }

        return HasTreasures(searchAreaTreasures) ? searchAreaTreasures : GetFallbackSpecialTreasures();
    }

    private TreasureDefinition[] GetSpecialTreeFieldTreasures()
    {
        if (HasTreasures(specialTreeFieldTreasures))
        {
            return specialTreeFieldTreasures;
        }

        return GetSpecialFieldTreasures();
    }

    private TreasureDefinition[] GetFallbackDefaultTreasures()
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

    private TreasureDefinition[] GetFallbackSpecialTreasures()
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

    private TreasureDefinition[] CombinePools(params TreasureDefinition[][] pools)
    {
        List<TreasureDefinition> combined = new List<TreasureDefinition>();

        foreach (TreasureDefinition[] pool in pools)
        {
            if (!HasTreasures(pool))
            {
                continue;
            }

            foreach (TreasureDefinition treasure in pool)
            {
                if (treasure != null)
                {
                    combined.Add(treasure);
                }
            }
        }

        return combined.ToArray();
    }

    private int CountFindableTreasures(TreasureDefinition[] treasureList)
    {
        if (!HasTreasures(treasureList))
        {
            return 0;
        }

        int count = 0;

        foreach (TreasureDefinition treasure in treasureList)
        {
            if (treasure == null
                || treasure.spawnWeight <= 0
                || string.IsNullOrWhiteSpace(treasure.treasureName))
            {
                continue;
            }

            count++;
        }

        return count;
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
    Default,
    DefaultPlusOneUpgrade,
    DefaultPlusTwoUpgrades,
    DefaultPlusThreeUpgrades,
    SpecialField,
    SpecialTreeField
}
