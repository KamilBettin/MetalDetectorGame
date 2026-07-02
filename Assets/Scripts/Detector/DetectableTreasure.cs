using UnityEngine;

public class DetectableTreasure : MonoBehaviour
{
    public string multiplayerId;
    public string treasureName = "Old Coin";
    public int value = 10;
    public TreasureRarity rarity = TreasureRarity.Common;
    public Sprite icon;
    public int inventoryWidth = 1;
    public int inventoryHeight = 1;
    public int requiredDigHits = 3;
    public int currentDigHits;
    public bool isRevealed;
    public bool isFound;
    public SearchMarker revealMarker;

    public float DigProgress01 => requiredDigHits > 0 ? Mathf.Clamp01(currentDigHits / (float)requiredDigHits) : 0f;
    public bool IsDugUp => currentDigHits >= Mathf.Max(1, requiredDigHits);

    public void Reveal(SearchMarker marker)
    {
        if (isFound || isRevealed)
        {
            return;
        }

        isRevealed = true;
        revealMarker = marker;
    }

    public bool DigOnce()
    {
        currentDigHits = Mathf.Min(currentDigHits + 1, Mathf.Max(1, requiredDigHits));
        return IsDugUp;
    }
}
