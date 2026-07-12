using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NpcQuestGiver : MonoBehaviour
{
    [Serializable]
    public class QuestDefinition
    {
        public string questId;
        public string title;
        public string requiredItemName;
        public string[] acceptedItemNames;
        public ItemRequirement[] itemRequirements;
        public int requiredCount = 1;
        public int requiredMoney;
        public int rewardMoney = 50;
        public string flavorText;
        public string completionText;
        public bool endsGame;
        public bool completed;

        public string[] AllAcceptedItemNames
        {
            get
            {
                if (acceptedItemNames == null || acceptedItemNames.Length == 0)
                {
                    return new[] { requiredItemName };
                }

                string[] names = new string[acceptedItemNames.Length + 1];
                names[0] = requiredItemName;

                for (int i = 0; i < acceptedItemNames.Length; i++)
                {
                    names[i + 1] = acceptedItemNames[i];
                }

                return names;
            }
        }
    }

    [Serializable]
    public class ItemRequirement
    {
        public string itemName;
        public string[] acceptedItemNames;
        public int count = 1;

        public string[] AllAcceptedItemNames
        {
            get
            {
                if (acceptedItemNames == null || acceptedItemNames.Length == 0)
                {
                    return new[] { itemName };
                }

                string[] names = new string[acceptedItemNames.Length + 1];
                names[0] = itemName;

                for (int i = 0; i < acceptedItemNames.Length; i++)
                {
                    names[i + 1] = acceptedItemNames[i];
                }

                return names;
            }
        }
    }

    public string npcDisplayName = "Mira";
    public Transform interactionPoint;
    public Transform promptAnchor;
    public Transform player;
    public PlayerInventory playerInventory;
    public float interactionDistance = 4.2f;
    public QuestDefinition[] quests;

    private bool isMenuOpen;
    private bool endingOpen;
    private QuestDefinition pendingQuestConfirmation;
    private string message = "";
    private float messageTimer;
    private Vector2 questScrollPosition;

    public bool IsMenuOpen => isMenuOpen;
    public Vector3 PromptPosition => promptAnchor != null ? promptAnchor.position : transform.position + Vector3.up * 2f;

    private void Awake()
    {
        EnsureDefaultQuests();
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
        }

        if (endingOpen)
        {
            if (Keyboard.current != null
                && (Keyboard.current.escapeKey.wasPressedThisFrame
                    || Keyboard.current.enterKey.wasPressedThisFrame
                    || Keyboard.current.spaceKey.wasPressedThisFrame))
            {
                CloseEnding();
            }

            return;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && IsPlayerInRange())
        {
            if (isMenuOpen || GameUIState.CanProcessGameplayInput)
            {
                SetMenuOpen(!isMenuOpen);
            }
        }

        if (isMenuOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (pendingQuestConfirmation != null)
            {
                pendingQuestConfirmation = null;
                return;
            }

            SetMenuOpen(false);
        }

        if (isMenuOpen && !IsPlayerInRange())
        {
            SetMenuOpen(false);
        }
    }

    public bool IsPlayerInRange()
    {
        ResolveReferences();

        if (player == null)
        {
            return false;
        }

        Vector3 targetPosition = interactionPoint != null ? interactionPoint.position : transform.position;
        return Vector3.Distance(player.position, targetPosition) <= interactionDistance;
    }

    public void SetMenuOpen(bool open)
    {
        isMenuOpen = open;
        if (!isMenuOpen)
        {
            pendingQuestConfirmation = null;
        }

        GameUIState.SetQuestMenuOpen(isMenuOpen);
    }

    public List<string> GetCompletedQuestIds()
    {
        List<string> completedQuestIds = new List<string>();

        EnsureDefaultQuests();

        foreach (QuestDefinition quest in quests)
        {
            if (quest != null && quest.completed && !string.IsNullOrEmpty(quest.questId))
            {
                completedQuestIds.Add(quest.questId);
            }
        }

        return completedQuestIds;
    }

    public void ApplyCompletedQuestIds(IList<string> completedQuestIds)
    {
        EnsureDefaultQuests();
        HashSet<string> completedLookup = completedQuestIds != null
            ? new HashSet<string>(completedQuestIds)
            : new HashSet<string>();

        foreach (QuestDefinition quest in quests)
        {
            if (quest == null)
            {
                continue;
            }

            quest.completed = !string.IsNullOrEmpty(quest.questId) && completedLookup.Contains(quest.questId);
        }
    }

    public static NpcQuestGiver FindClosestQuestGiverInRange()
    {
        NpcQuestGiver[] questGivers = FindObjectsByType<NpcQuestGiver>();
        NpcQuestGiver closest = null;
        float closestDistance = float.MaxValue;

        foreach (NpcQuestGiver questGiver in questGivers)
        {
            if (questGiver == null || !questGiver.IsPlayerInRange())
            {
                continue;
            }

            Transform playerTransform = questGiver.player != null ? questGiver.player : PlayerRigReferences.FindLocalPlayerTransform();
            Vector3 targetPosition = questGiver.interactionPoint != null ? questGiver.interactionPoint.position : questGiver.transform.position;
            float distance = playerTransform != null ? Vector3.Distance(playerTransform.position, targetPosition) : 0f;

            if (distance < closestDistance)
            {
                closest = questGiver;
                closestDistance = distance;
            }
        }

        return closest;
    }

    public static bool AnyQuestGiverInteractionInRange()
    {
        return FindClosestQuestGiverInRange() != null;
    }

    private void CompleteQuest(QuestDefinition quest)
    {
        if (quest == null)
        {
            return;
        }

        if (quest.completed)
        {
            ShowMessage(GameLocalization.T("quest.already_done"));
            return;
        }

        if (!IsQuestUnlocked(quest))
        {
            ShowMessage(GameLocalization.T("quest.locked"));
            return;
        }

        if (playerInventory == null)
        {
            ShowMessage(GameLocalization.T("quest.no_backpack"));
            return;
        }

        if (!HasQuestRequirements(quest, out string missingMessage))
        {
            ShowMessage(missingMessage);
            return;
        }

        if (!RemoveQuestItems(quest))
        {
            ShowMessage(GameLocalization.T("quest.items_missing"));
            return;
        }

        if (quest.requiredMoney > 0 && !playerInventory.TrySpendMoney(quest.requiredMoney))
        {
            ShowMessage(GameLocalization.TFormat("quest.need_money", quest.requiredMoney));
            return;
        }

        quest.completed = true;
        playerInventory.money += Mathf.Max(0, quest.rewardMoney);
        ShowMessage(quest.rewardMoney > 0
            ? GameLocalization.TFormat("quest.complete_paid", quest.rewardMoney)
            : GameLocalization.T("quest.complete_story"));
        GameEvents.ReportTreasuresSold(GetRequiredItemCount(quest));
        LocalCoopManager.Instance?.ReportTeamStateChanged();

        if (quest.endsGame)
        {
            OpenEnding();
        }
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            player = PlayerRigReferences.FindLocalPlayerTransform();
        }

        if (playerInventory == null)
        {
            playerInventory = PlayerRigReferences.FindLocalInventory();
        }
    }

    private void EnsureDefaultQuests()
    {
        if (quests != null && quests.Length > 0 && !UsesLegacyDefaultQuests())
        {
            return;
        }

        quests = new[]
        {
            CreateQuest(
                "escape_beacon_timer",
                "The Beacon Needs a Heart",
                new[] { Require("Watch Fragment", 1), Require("Watch Case", 1) },
                0,
                90,
                "Mira can turn watch parts into a timing coil for the emergency beacon.",
                "The beacon ticks again. Somewhere beyond the fog, a receiver can hear it."),
            CreateQuest(
                "escape_compass",
                "A Needle for North",
                new[] { Require("Compass", 1) },
                0,
                160,
                "The island bends every path. A working compass gives the boat a direction.",
                "The compass steadies. North finally means something again."),
            CreateQuest(
                "escape_hull_patch",
                "Patch the Old Skiff",
                new[] { Require("Horseshoe", 1), Require("Bracelet", 1) },
                0,
                260,
                "Mira's hidden skiff needs a curved brace and a chain clamp before it can float.",
                "The hull holds. It is ugly, but it will not sink immediately."),
            CreateQuest(
                "escape_harbor_debt",
                "Fuel and Favors",
                new[] { Require("Silver Ring", 1, "Plain Silver Wedding Band", "Silver Earring") },
                700,
                0,
                "The cove keeper wants silver and cash before he risks lighting the pier.",
                "The keeper is paid. At dawn, the cove gate will be open."),
            CreateQuest(
                "escape_final_fare",
                "One Last Fare",
                new[] { Require("Pocket Watch", 1, "Working Pocket Watch"), Require("Old Gold Coin", 1) },
                1000,
                0,
                "Mira needs one valuable fare for the smuggler and a watch to time the tide.",
                "The tide turns. The engine coughs awake, and the island finally starts shrinking behind you.",
                true)
        };
    }

    private bool UsesLegacyDefaultQuests()
    {
        if (quests == null || quests.Length == 0)
        {
            return true;
        }

        foreach (QuestDefinition quest in quests)
        {
            if (quest == null || string.IsNullOrEmpty(quest.questId))
            {
                continue;
            }

            if (quest.questId == "watch_parts"
                || quest.questId == "fishing_bits"
                || quest.questId == "silver_trinkets")
            {
                return true;
            }
        }

        return false;
    }

    private static ItemRequirement Require(string itemName, int count, params string[] acceptedItemNames)
    {
        return new ItemRequirement
        {
            itemName = itemName,
            acceptedItemNames = acceptedItemNames,
            count = Mathf.Max(1, count)
        };
    }

    private static QuestDefinition CreateQuest(string questId, string title, ItemRequirement[] itemRequirements, int requiredMoney, int rewardMoney, string flavorText, string completionText, bool endsGame = false)
    {
        return new QuestDefinition
        {
            questId = questId,
            title = title,
            requiredItemName = itemRequirements != null && itemRequirements.Length > 0 ? itemRequirements[0].itemName : "",
            itemRequirements = itemRequirements,
            requiredCount = itemRequirements != null && itemRequirements.Length > 0 ? itemRequirements[0].count : 0,
            requiredMoney = requiredMoney,
            rewardMoney = rewardMoney,
            flavorText = flavorText,
            completionText = completionText,
            endsGame = endsGame
        };
    }

    private void ShowMessage(string value)
    {
        message = value;
        messageTimer = 3f;
    }

    private void OnGUI()
    {
        if (endingOpen)
        {
            DrawEndingScreen();
            return;
        }

        if (isMenuOpen)
        {
            DrawQuestMenu();
            return;
        }

        if (IsPlayerInRange() && GameUIState.CanProcessGameplayInput)
        {
            GameGui.DrawToast(new Rect(Screen.width * 0.5f - 175f, Screen.height - 178f, 350f, 40f), GameLocalization.TFormat("quest.talk_to", npcDisplayName));
        }
    }

    private void DrawQuestMenu()
    {
        EnsureDefaultQuests();
        ResolveReferences();

        Rect panel = new Rect(Screen.width * 0.5f - 320f, Screen.height * 0.5f - 280f, 640f, 560f);
        GameGui.DrawPanel(panel, npcDisplayName + " - " + GameLocalization.T("quest.jobs"));

        GUI.Label(new Rect(panel.x + 20f, panel.y + 44f, panel.width - 40f, 42f), GameLocalization.T("quest.help"), GameGui.SmallLabelStyle);

        const float rowHeight = 124f;
        Rect listRect = new Rect(panel.x + 18f, panel.y + 94f, panel.width - 36f, panel.height - 146f);
        Rect viewRect = new Rect(0f, 0f, listRect.width - 18f, quests.Length * rowHeight);
        questScrollPosition = GUI.BeginScrollView(listRect, questScrollPosition, viewRect, false, true);

        for (int i = 0; i < quests.Length; i++)
        {
            DrawQuestRow(quests[i], i, new Rect(0f, i * rowHeight, viewRect.width, rowHeight - 8f));
        }

        GUI.EndScrollView();

        GUI.Label(new Rect(panel.x + 18f, panel.y + panel.height - 34f, panel.width - 36f, 22f), messageTimer > 0f ? message : GameLocalization.T("quest.close"), GameGui.HintStyle);
        DrawQuestConfirmationDialog();
    }

    private void DrawQuestRow(QuestDefinition quest, int questIndex, Rect rect)
    {
        if (quest == null)
        {
            return;
        }

        bool unlocked = IsQuestUnlocked(questIndex);
        bool confirmationOpen = pendingQuestConfirmation != null;
        bool canComplete = unlocked && !quest.completed && HasQuestRequirements(quest, out _);

        GameGui.DrawRect(rect, new Color(0f, 0f, 0f, 0.22f));
        Color iconTint = quest.completed
            ? new Color(1f, 1f, 1f, 0.48f)
            : !unlocked ? new Color(1f, 1f, 1f, 0.32f) : canComplete ? Color.white : new Color(1f, 1f, 1f, 0.72f);
        GameGui.DrawIcon(new Rect(rect.x + 12f, rect.y + 16f, 46f, 46f), "quest", iconTint);

        float textX = rect.x + 68f;
        float textWidth = rect.width - 224f;
        string titlePrefix = quest.completed ? "[DONE] " : !unlocked ? "[LOCKED] " : "";
        GUI.Label(new Rect(textX, rect.y + 6f, textWidth, 22f), titlePrefix + quest.title, GameGui.LabelStyle);
        GUI.Label(new Rect(textX, rect.y + 30f, textWidth, 36f), unlocked ? quest.flavorText : GameLocalization.T("quest.finish_previous"), GameGui.SmallLabelStyle);
        GUI.Label(new Rect(textX, rect.y + 68f, textWidth, 38f), GetQuestRequirementsText(quest), GameGui.SmallLabelStyle);

        Rect rewardRect = new Rect(rect.xMax - 132f, rect.y + 14f, 112f, 30f);
        GameGui.DrawRect(rewardRect, quest.completed ? new Color(0.35f, 0.92f, 0.62f, 0.16f) : new Color(1f, 0.82f, 0.24f, 0.18f));
        GUI.Label(new Rect(rewardRect.x + 6f, rewardRect.y + 6f, rewardRect.width - 12f, 18f), GetQuestRewardText(quest), GameGui.HintStyle);

        GUI.enabled = canComplete && !confirmationOpen;
        string buttonText = quest.completed ? GameLocalization.T("quest.done") : !unlocked ? GameLocalization.T("quest.locked_short") : canComplete ? GameLocalization.T("quest.deliver") : GameLocalization.T("quest.missing");

        if (GameGui.Button(new Rect(rect.xMax - 126f, rect.y + 58f, 106f, 34f), buttonText))
        {
            pendingQuestConfirmation = quest;
        }

        GUI.enabled = true;
    }

    private bool IsQuestUnlocked(QuestDefinition quest)
    {
        if (quests == null)
        {
            return false;
        }

        for (int i = 0; i < quests.Length; i++)
        {
            if (quests[i] == quest)
            {
                return IsQuestUnlocked(i);
            }
        }

        return false;
    }

    private bool IsQuestUnlocked(int questIndex)
    {
        if (questIndex <= 0)
        {
            return true;
        }

        return quests != null
            && questIndex < quests.Length
            && quests[questIndex - 1] != null
            && quests[questIndex - 1].completed;
    }

    private bool HasQuestRequirements(QuestDefinition quest, out string missingMessage)
    {
        List<string> missingParts = new List<string>();

        foreach (ItemRequirement requirement in GetQuestItemRequirements(quest))
        {
            int ownedCount = playerInventory != null ? playerInventory.CountItemsNamed(requirement.AllAcceptedItemNames) : 0;

            if (ownedCount < requirement.count)
            {
                missingParts.Add(requirement.itemName + " " + ownedCount + "/" + requirement.count);
            }
        }

        if (quest != null && quest.requiredMoney > 0)
        {
            int ownedMoney = playerInventory != null ? playerInventory.money : 0;

            if (ownedMoney < quest.requiredMoney)
            {
                missingParts.Add("$" + ownedMoney + "/$" + quest.requiredMoney);
            }
        }

        missingMessage = missingParts.Count > 0
            ? GameLocalization.T("quest.missing_prefix") + " " + string.Join(", ", missingParts)
            : "";
        return missingParts.Count == 0;
    }

    private bool RemoveQuestItems(QuestDefinition quest)
    {
        foreach (ItemRequirement requirement in GetQuestItemRequirements(quest))
        {
            if (!playerInventory.RemoveItemsNamed(requirement.AllAcceptedItemNames, requirement.count))
            {
                return false;
            }
        }

        return true;
    }

    private ItemRequirement[] GetQuestItemRequirements(QuestDefinition quest)
    {
        if (quest == null)
        {
            return Array.Empty<ItemRequirement>();
        }

        if (quest.itemRequirements != null && quest.itemRequirements.Length > 0)
        {
            return quest.itemRequirements;
        }

        if (!string.IsNullOrWhiteSpace(quest.requiredItemName) && quest.requiredCount > 0)
        {
            return new[] { Require(quest.requiredItemName, quest.requiredCount, quest.acceptedItemNames ?? Array.Empty<string>()) };
        }

        return Array.Empty<ItemRequirement>();
    }

    private int GetRequiredItemCount(QuestDefinition quest)
    {
        int count = 0;

        foreach (ItemRequirement requirement in GetQuestItemRequirements(quest))
        {
            count += Mathf.Max(0, requirement.count);
        }

        return count;
    }

    private string GetQuestRequirementsText(QuestDefinition quest)
    {
        if (quest == null)
        {
            return "";
        }

        List<string> parts = new List<string>();

        foreach (ItemRequirement requirement in GetQuestItemRequirements(quest))
        {
            int ownedCount = playerInventory != null ? playerInventory.CountItemsNamed(requirement.AllAcceptedItemNames) : 0;
            parts.Add(requirement.count + "x " + requirement.itemName + " (" + ownedCount + "/" + requirement.count + ")");
        }

        if (quest.requiredMoney > 0)
        {
            int ownedMoney = playerInventory != null ? playerInventory.money : 0;
            parts.Add("$" + quest.requiredMoney + " (" + ownedMoney + "/$" + quest.requiredMoney + ")");
        }
        return GameLocalization.T("quest.requires") + " " + string.Join(", ", parts);
    }

    private string GetQuestRewardText(QuestDefinition quest)
    {
        if (quest == null)
        {
            return "";
        }

        if (quest.endsGame)
        {
            return GameLocalization.T("quest.reward_escape_short");
        }

        return quest.rewardMoney > 0
            ? GameLocalization.TFormat("quest.reward_cash_short", quest.rewardMoney)
            : GameLocalization.T("quest.reward_story_short");
    }

    private void OpenEnding()
    {
        endingOpen = true;
        isMenuOpen = false;
        GameUIState.SetQuestMenuOpen(true);
    }

    private void CloseEnding()
    {
        endingOpen = false;
        GameUIState.SetQuestMenuOpen(false);
    }

    private void DrawEndingScreen()
    {
        Rect panel = new Rect(Screen.width * 0.5f - 330f, Screen.height * 0.5f - 190f, 660f, 380f);
        GameGui.DrawPanel(panel, GameLocalization.T("quest.ending_title"));
        GameGui.DrawIcon(new Rect(panel.x + panel.width * 0.5f - 34f, panel.y + 54f, 68f, 68f), "quest", new Color(0.35f, 0.92f, 0.62f, 1f));
        GUI.Label(new Rect(panel.x + 42f, panel.y + 140f, panel.width - 84f, 122f), GameLocalization.T("quest.ending_body"), GameGui.LabelStyle);

        if (GameGui.Button(new Rect(panel.x + panel.width * 0.5f - 120f, panel.yMax - 72f, 240f, 38f), GameLocalization.T("quest.ending_close")))
        {
            CloseEnding();
        }
    }

    private void DrawQuestConfirmationDialog()
    {
        if (pendingQuestConfirmation == null)
        {
            return;
        }

        Rect overlay = new Rect(0f, 0f, Screen.width, Screen.height);
        GameGui.DrawRect(overlay, new Color(0f, 0f, 0f, 0.48f));

        Rect panel = new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 150f, 460f, 300f);
        GameGui.DrawPanel(panel, GameLocalization.T("quest.confirm_title"));
        GUI.Label(new Rect(panel.x + 28f, panel.y + 48f, panel.width - 56f, 42f), GameLocalization.TFormat("quest.confirm_body", pendingQuestConfirmation.title), GameGui.LabelStyle);
        GUI.Label(new Rect(panel.x + 28f, panel.y + 94f, panel.width - 56f, 48f), GetQuestRequirementsText(pendingQuestConfirmation), GameGui.SmallLabelStyle);
        GUI.Label(new Rect(panel.x + 28f, panel.y + 142f, panel.width - 56f, 28f), GameLocalization.TFormat("quest.confirm_reward", GetQuestRewardText(pendingQuestConfirmation)), GameGui.HintStyle);

        Rect acceptRect = new Rect(panel.x + 112f, panel.y + 188f, 72f, 72f);
        Rect rejectRect = new Rect(panel.xMax - 184f, panel.y + 188f, 72f, 72f);

        if (GUI.Button(acceptRect, GUIContent.none, GUIStyle.none))
        {
            QuestDefinition confirmedQuest = pendingQuestConfirmation;
            pendingQuestConfirmation = null;
            CompleteQuest(confirmedQuest);
        }

        if (GUI.Button(rejectRect, GUIContent.none, GUIStyle.none))
        {
            pendingQuestConfirmation = null;
        }

        GameGui.DrawIcon(acceptRect, "confirm", Color.white);
        GameGui.DrawIcon(rejectRect, "reject", Color.white);
        GUI.Label(new Rect(acceptRect.x - 16f, acceptRect.yMax + 4f, acceptRect.width + 32f, 20f), GameLocalization.T("quest.confirm_accept"), GameGui.HintStyle);
        GUI.Label(new Rect(rejectRect.x - 16f, rejectRect.yMax + 4f, rejectRect.width + 32f, 20f), GameLocalization.T("quest.confirm_cancel"), GameGui.HintStyle);
    }
}
