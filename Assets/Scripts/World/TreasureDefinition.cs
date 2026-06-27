using UnityEngine;

[System.Serializable]
public class TreasureDefinition
{
    public string treasureName = "Old Coin";
    public int value = 10;
    public TreasureRarity rarity = TreasureRarity.Common;
    public Sprite icon;
    [Range(1, 100)]
    public int spawnWeight = 10;
    [Range(1, 3)]
    public int width = 1;
    [Range(1, 3)]
    public int height = 1;
    public int minDigHits = 3;
    public int maxDigHits = 4;
}
