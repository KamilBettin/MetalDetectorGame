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
        public int requiredCount = 1;
        public int rewardMoney = 50;
        public string flavorText;
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

    public string npcDisplayName = "Mira";
    public Transform interactionPoint;
    public Transform promptAnchor;
    public Transform player;
    public PlayerInventory playerInventory;
    public float interactionDistance = 4.2f;
    public QuestDefinition[] quests;

    private bool isMenuOpen;
    private string message = "";
    private float messageTimer;

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

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && IsPlayerInRange())
        {
            if (!GameUIState.AnyMenuOpen || isMenuOpen)
            {
                SetMenuOpen(!isMenuOpen);
            }
        }

        if (isMenuOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
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

        if (playerInventory == null)
        {
            ShowMessage(GameLocalization.T("quest.no_backpack"));
            return;
        }

        int ownedCount = playerInventory.CountItemsNamed(quest.AllAcceptedItemNames);

        if (ownedCount < quest.requiredCount)
        {
            ShowMessage(GameLocalization.TFormat("quest.need_items", quest.requiredCount, quest.requiredItemName));
            return;
        }

        if (!playerInventory.RemoveItemsNamed(quest.AllAcceptedItemNames, quest.requiredCount))
        {
            ShowMessage(GameLocalization.T("quest.items_missing"));
            return;
        }

        quest.completed = true;
        playerInventory.money += Mathf.Max(0, quest.rewardMoney);
        ShowMessage(GameLocalization.TFormat("quest.complete_paid", quest.rewardMoney));
        GameEvents.ReportTreasuresSold(quest.requiredCount);
        LocalCoopManager.Instance?.ReportTeamStateChanged();
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
        if (quests != null && quests.Length > 0)
        {
            return;
        }

        quests = new[]
        {
            CreateQuest("keys_for_cellar", "Cellar Keys", "Rusty/old keys", new[] { "Old Key", "Lost Key", "Broken Key", "Car Key", "Bicycle Key", "Keyring With Keys" }, 3, 100, "Bring me any three old keys."),
            CreateQuest("watch_parts", "Watch Repair", "Watch Fragment", new[] { "Old Watch Fragment", "Watch Dial", "Watch Hands", "Watch Crown", "Clock Gear", "Small Cog", "Watch Spring" }, 3, 140, "I can rebuild a watch from spare parts."),
            CreateQuest("fishing_bits", "Tackle Box", "Fishing Hook", new[] { "Treble Fishing Hook", "Fishing Sinker", "Fishing Swivel", "Spinner Lure", "Fishing Bell" }, 4, 120, "Old fishing metal still has buyers."),
            CreateQuest("silver_trinkets", "Silver Trinkets", "Silver Ring", new[] { "Plain Silver Wedding Band", "Silver Earring", "Silver Fork", "Silver Teaspoon" }, 2, 180, "Clean silver is worth the effort.")
        };
    }

    private static QuestDefinition CreateQuest(string questId, string title, string requiredItemName, string[] acceptedItemNames, int requiredCount, int rewardMoney, string flavorText)
    {
        return new QuestDefinition
        {
            questId = questId,
            title = title,
            requiredItemName = requiredItemName,
            acceptedItemNames = acceptedItemNames,
            requiredCount = requiredCount,
            rewardMoney = rewardMoney,
            flavorText = flavorText
        };
    }

    private void ShowMessage(string value)
    {
        message = value;
        messageTimer = 3f;
    }

    private void OnGUI()
    {
        if (isMenuOpen)
        {
            DrawQuestMenu();
            return;
        }

        if (IsPlayerInRange() && !GameUIState.AnyMenuOpen)
        {
            GameGui.DrawToast(new Rect(Screen.width * 0.5f - 175f, Screen.height - 178f, 350f, 40f), GameLocalization.TFormat("quest.talk_to", npcDisplayName));
        }
    }

    private void DrawQuestMenu()
    {
        EnsureDefaultQuests();
        ResolveReferences();

        Rect panel = new Rect(Screen.width * 0.5f - 280f, Screen.height * 0.5f - 238f, 560f, 476f);
        GameGui.DrawPanel(panel, npcDisplayName + " - " + GameLocalization.T("quest.jobs"));

        GUI.Label(new Rect(panel.x + 20f, panel.y + 44f, panel.width - 40f, 24f), GameLocalization.T("quest.help"), GameGui.LabelStyle);

        float rowY = panel.y + 82f;

        for (int i = 0; i < quests.Length; i++)
        {
            DrawQuestRow(quests[i], new Rect(panel.x + 18f, rowY, panel.width - 36f, 78f));
            rowY += 84f;
        }

        GUI.Label(new Rect(panel.x + 18f, panel.y + panel.height - 34f, panel.width - 36f, 22f), messageTimer > 0f ? message : GameLocalization.T("quest.close"), GameGui.HintStyle);
    }

    private void DrawQuestRow(QuestDefinition quest, Rect rect)
    {
        if (quest == null)
        {
            return;
        }

        int ownedCount = playerInventory != null ? playerInventory.CountItemsNamed(quest.AllAcceptedItemNames) : 0;
        bool canComplete = !quest.completed && ownedCount >= quest.requiredCount;

        GameGui.DrawRect(rect, new Color(0f, 0f, 0f, 0.22f));
        Color iconTint = quest.completed
            ? new Color(1f, 1f, 1f, 0.48f)
            : canComplete ? Color.white : new Color(1f, 1f, 1f, 0.72f);
        GameGui.DrawIcon(new Rect(rect.x + 12f, rect.y + 16f, 46f, 46f), "quest", iconTint);

        float textX = rect.x + 68f;
        float textWidth = rect.width - 210f;
        GUI.Label(new Rect(textX, rect.y + 6f, textWidth, 22f), quest.title, GameGui.LabelStyle);
        GUI.Label(new Rect(textX, rect.y + 29f, textWidth, 18f), quest.flavorText, GameGui.SmallLabelStyle);
        GUI.Label(new Rect(textX, rect.y + 52f, textWidth, 18f), GameLocalization.TFormat("quest.need_reward", quest.requiredCount, quest.requiredItemName, ownedCount, quest.rewardMoney), GameGui.SmallLabelStyle);

        GUI.enabled = canComplete;
        string buttonText = quest.completed ? GameLocalization.T("quest.done") : canComplete ? GameLocalization.T("quest.deliver") : GameLocalization.T("quest.missing");

        if (GameGui.Button(new Rect(rect.xMax - 112f, rect.y + 22f, 92f, 34f), buttonText))
        {
            CompleteQuest(quest);
        }

        GUI.enabled = true;
    }
}
