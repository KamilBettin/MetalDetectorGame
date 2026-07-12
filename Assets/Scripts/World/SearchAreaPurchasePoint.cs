using TMPro;
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
    private bool pendingUnlockConfirmation;
    public Vector3 PromptPosition => transform.position + promptWorldOffset;
    public string PromptActionText => targetArea != null && targetArea.unlockCost <= 0 ? GameLocalization.T("area.claim_area") : GameLocalization.T("area.buy_area");

    private void Awake()
    {
        if (targetArea == null)
        {
            targetArea = GetComponentInParent<SearchArea>();
        }

        RefreshSignVisual();
    }

    private void OnEnable()
    {
        GameLocalization.LanguageChanged += RefreshSignVisual;
        RefreshSignVisual();
    }

    private void Update()
    {
        ResolveReferences();

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
        }

        if (pendingUnlockConfirmation)
        {
            if (!CanKeepConfirmationOpen())
            {
                SetUnlockConfirmationOpen(false);
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetUnlockConfirmationOpen(false);
            }

            return;
        }

        if (!CanInteract())
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenUnlockConfirmation();
        }
    }

    public bool IsPlayerInRange()
    {
        return player != null && Vector3.Distance(player.position, transform.position) <= interactionDistance;
    }

    private void OpenUnlockConfirmation()
    {
        if (targetArea == null)
        {
            ShowMessage(GameLocalization.T("area.sign_not_connected"));
            return;
        }

        SetUnlockConfirmationOpen(true);
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
            GameSfx.PlayUpgrade();
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
            && GameUIState.CanProcessGameplayInput
            && !pendingUnlockConfirmation;
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

    private void OnDisable()
    {
        GameLocalization.LanguageChanged -= RefreshSignVisual;
        SetUnlockConfirmationOpen(false);
    }

    public void RefreshSignVisual()
    {
        if (targetArea == null)
        {
            targetArea = GetComponentInParent<SearchArea>();
        }

        if (targetArea == null || !targetArea.isUnlocked)
        {
            return;
        }

        TextMeshPro[] signTexts = GetComponentsInChildren<TextMeshPro>(true);

        foreach (TextMeshPro signText in signTexts)
        {
            if (signText != null && (signText.name == "Sign Text Front" || signText.name == "Sign Text Back"))
            {
                signText.text = GameLocalization.T("area.purchased");
                signText.ForceMeshUpdate();
            }
        }
    }

    private void SetUnlockConfirmationOpen(bool isOpen)
    {
        if (pendingUnlockConfirmation == isOpen)
        {
            return;
        }

        pendingUnlockConfirmation = isOpen;
        GameUIState.SetConfirmationOpen(isOpen);
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

        if (IsPlayerInRange() && GameUIState.CanProcessGameplayInput && !pendingUnlockConfirmation)
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

        DrawUnlockConfirmationDialog();
    }

    private bool CanKeepConfirmationOpen()
    {
        ResolveReferences();

        return targetArea != null
            && !targetArea.isUnlocked
            && playerInventory != null
            && IsPlayerInRange()
            && !GameUIState.AnyMenuOpen;
    }

    private void DrawUnlockConfirmationDialog()
    {
        if (!pendingUnlockConfirmation || targetArea == null)
        {
            return;
        }

        Rect overlay = new Rect(0f, 0f, Screen.width, Screen.height);
        GameGui.DrawRect(overlay, new Color(0f, 0f, 0f, 0.48f));

        Rect panel = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 132f, 440f, 264f);
        bool isFree = targetArea.unlockCost <= 0;
        string body = isFree
            ? GameLocalization.TFormat("area.confirm_body_claim", targetArea.areaName)
            : GameLocalization.TFormat("area.confirm_body_buy", targetArea.areaName, targetArea.unlockCost);

        GameGui.DrawPanel(panel, GameLocalization.T("area.confirm_title"));
        GUI.Label(new Rect(panel.x + 28f, panel.y + 54f, panel.width - 56f, 58f), body, GameGui.LabelStyle);

        Rect acceptRect = new Rect(panel.x + 102f, panel.y + 138f, 72f, 72f);
        Rect rejectRect = new Rect(panel.xMax - 174f, panel.y + 138f, 72f, 72f);

        if (GUI.Button(acceptRect, GUIContent.none, GUIStyle.none))
        {
            SetUnlockConfirmationOpen(false);
            TryBuyArea();
        }

        if (GUI.Button(rejectRect, GUIContent.none, GUIStyle.none))
        {
            SetUnlockConfirmationOpen(false);
        }

        GameGui.DrawIcon(acceptRect, "confirm", Color.white);
        GameGui.DrawIcon(rejectRect, "reject", Color.white);
        GUI.Label(new Rect(acceptRect.x - 16f, acceptRect.yMax + 4f, acceptRect.width + 32f, 20f), GameLocalization.T("area.confirm_accept"), GameGui.HintStyle);
        GUI.Label(new Rect(rejectRect.x - 16f, rejectRect.yMax + 4f, rejectRect.width + 32f, 20f), GameLocalization.T("area.confirm_cancel"), GameGui.HintStyle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.76f, 0.2f, 0.42f);
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
