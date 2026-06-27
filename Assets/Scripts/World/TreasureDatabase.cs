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
        new TreasureDefinition { treasureName = "Bent Spoon", value = 9, rarity = TreasureRarity.Common, spawnWeight = 13, width = 1, height = 2, minDigHits = 3, maxDigHits = 4 },
        new TreasureDefinition { treasureName = "Lost Key", value = 14, rarity = TreasureRarity.Rare, spawnWeight = 10, width = 1, height = 1, minDigHits = 3, maxDigHits = 5 },
        new TreasureDefinition { treasureName = "Pocket Watch", value = 32, rarity = TreasureRarity.Rare, spawnWeight = 7, width = 2, height = 1, minDigHits = 4, maxDigHits = 5 },
        new TreasureDefinition { treasureName = "Silver Ring", value = 45, rarity = TreasureRarity.Rare, spawnWeight = 6, width = 1, height = 1, minDigHits = 4, maxDigHits = 6 },
        new TreasureDefinition { treasureName = "Old Dagger", value = 70, rarity = TreasureRarity.Rare, spawnWeight = 4, width = 1, height = 2, minDigHits = 4, maxDigHits = 6 },
        new TreasureDefinition { treasureName = "Gold Ring", value = 120, rarity = TreasureRarity.Epic, spawnWeight = 3, width = 1, height = 1, minDigHits = 5, maxDigHits = 7 },
        new TreasureDefinition { treasureName = "Jeweled Compass", value = 180, rarity = TreasureRarity.Epic, spawnWeight = 2, width = 2, height = 2, minDigHits = 5, maxDigHits = 7 },
        new TreasureDefinition { treasureName = "Ancient Relic", value = 260, rarity = TreasureRarity.Epic, spawnWeight = 1, width = 2, height = 2, minDigHits = 6, maxDigHits = 8 }
    };

    public TreasureDefinition GetRandomTreasure()
    {
        if (treasures == null || treasures.Length == 0)
        {
            return null;
        }

        int totalWeight = 0;

        foreach (TreasureDefinition treasure in treasures)
        {
            if (treasure != null)
            {
                totalWeight += Mathf.Max(0, treasure.spawnWeight);
            }
        }

        if (totalWeight <= 0)
        {
            return treasures[Random.Range(0, treasures.Length)];
        }

        int roll = Random.Range(0, totalWeight);

        foreach (TreasureDefinition treasure in treasures)
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

        return treasures[treasures.Length - 1];
    }

    public static TreasureDatabase CreateRuntimeDefault()
    {
        return CreateInstance<TreasureDatabase>();
    }
}
