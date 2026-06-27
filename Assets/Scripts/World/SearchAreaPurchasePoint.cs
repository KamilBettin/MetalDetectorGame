using UnityEngine;
using UnityEngine.InputSystem;

public class SearchAreaPurchasePoint : MonoBehaviour
{
    public SearchArea targetArea;
    public PlayerInventory playerInventory;
    public Transform player;
    public float interactionDistance = 3f;
    public Vector3 promptWorldOffset = new Vector3(0f, 1.4f, 0f);

    private string message = "";
    private float messageTimer;

    private void Awake()
    {
        if (targetArea == null)
        {
            targetArea = GetComponentInParent<SearchArea>();
        }
    }

    private void Update()
    {
        ResolveReferences();

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
        }

        if (!CanInteract())
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryBuyArea();
        }
    }

    public bool IsPlayerInRange()
    {
        return player != null && Vector3.Distance(player.position, transform.position) <= interactionDistance;
    }

    private void TryBuyArea()
    {
        if (targetArea == null)
        {
            ShowMessage("This sign is not connected to a search area.");
            return;
        }

        if (targetArea.TryUnlock(playerInventory, out string resultMessage))
        {
            ShowMessage(resultMessage);
            return;
        }

        ShowMessage(resultMessage);
    }

    private bool CanInteract()
    {
        return targetArea != null
            && !targetArea.isUnlocked
            && playerInventory != null
            && IsPlayerInRange()
            && !GameUIState.AnyMenuOpen;
    }

    private void ResolveReferences()
    {
        if (playerInventory == null)
        {
            playerInventory = FindAnyObjectByType<PlayerInventory>();
        }

        if (player == null && playerInventory != null)
        {
            player = playerInventory.transform;
        }
    }

    private void ShowMessage(string value)
    {
        message = value;
        messageTimer = 2.6f;
    }

    private void OnGUI()
    {
        if (targetArea == null || playerInventory == null)
        {
            return;
        }

        if (targetArea.isUnlocked)
        {
            return;
        }

        if (IsPlayerInRange() && !GameUIState.AnyMenuOpen)
        {
            string action = targetArea.unlockCost <= 0 ? "Claim" : "Buy";
            string price = targetArea.unlockCost <= 0 ? "Free" : "$" + targetArea.unlockCost;
            string prompt = "E - " + action + " " + targetArea.areaName + " (" + price + ")";
            GameGui.DrawToast(new Rect(Screen.width * 0.5f - 190f, Screen.height - 178f, 380f, 40f), prompt);
        }

        if (messageTimer > 0f)
        {
            GameGui.DrawToast(new Rect(Screen.width * 0.5f - 190f, Screen.height - 226f, 380f, 40f), message);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.76f, 0.2f, 0.42f);
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
