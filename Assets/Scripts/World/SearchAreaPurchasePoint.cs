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
    public Vector3 PromptPosition => transform.position + promptWorldOffset;
    public string PromptActionText => targetArea != null && targetArea.unlockCost <= 0 ? GameLocalization.T("area.claim_area") : GameLocalization.T("area.buy_area");

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
            ShowMessage(GameLocalization.T("area.sign_not_connected"));
            return;
        }

        if (targetArea.TryUnlock(playerInventory, out string resultMessage))
        {
            ShowMessage(resultMessage);
            return;
        }

        ShowMessage(resultMessage);
    }

    public bool CanInteract()
    {
        ResolveReferences();

        return targetArea != null
            && !targetArea.isUnlocked
            && playerInventory != null
            && IsPlayerInRange()
            && !GameUIState.AnyMenuOpen;
    }

    public static SearchAreaPurchasePoint FindClosestInteractableInRange()
    {
        SearchAreaPurchasePoint[] purchasePoints = FindObjectsByType<SearchAreaPurchasePoint>();
        SearchAreaPurchasePoint closest = null;
        float closestDistance = float.MaxValue;

        foreach (SearchAreaPurchasePoint purchasePoint in purchasePoints)
        {
            if (purchasePoint == null || !purchasePoint.CanInteract())
            {
                continue;
            }

            float distance = purchasePoint.player != null
                ? Vector3.Distance(purchasePoint.player.position, purchasePoint.transform.position)
                : 0f;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = purchasePoint;
            }
        }

        return closest;
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
            string action = targetArea.unlockCost <= 0 ? GameLocalization.T("area.claim") : GameLocalization.T("area.buy");
            string price = targetArea.unlockCost <= 0 ? GameLocalization.T("area.free") : "$" + targetArea.unlockCost;
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
