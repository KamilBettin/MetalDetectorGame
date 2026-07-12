using UnityEngine;

public static class GameUIState
{
    public static bool IsStartMenuOpen { get; private set; }
    public static bool IsPauseMenuOpen { get; private set; }
    public static bool IsInventoryOpen { get; private set; }
    public static bool IsTraderMenuOpen { get; private set; }
    public static bool IsQuestMenuOpen { get; private set; }
    public static bool IsHomeMenuOpen { get; private set; }
    public static bool IsIntroLetterOpen { get; private set; }
    public static bool IsConfirmationOpen { get; private set; }
    public static bool IsSettingsMenuOpen { get; private set; }
    public static bool IsCharacterSelectionOpen { get; private set; }
    private static int lastMenuClosedFrame = -1;

    public static bool AnyMenuOpen => IsStartMenuOpen
        || IsPauseMenuOpen
        || IsInventoryOpen
        || IsTraderMenuOpen
        || IsQuestMenuOpen
        || IsHomeMenuOpen
        || IsIntroLetterOpen
        || IsSettingsMenuOpen
        || IsCharacterSelectionOpen;
    public static bool AnyBlockingUIOpen => AnyMenuOpen || IsConfirmationOpen;
    public static bool MenuClosedThisFrame => lastMenuClosedFrame == Time.frameCount;
    public static bool CanProcessGameplayInput => !AnyBlockingUIOpen && !MenuClosedThisFrame;

    public static void SetStartMenuOpen(bool isOpen)
    {
        RecordMenuClose(IsStartMenuOpen, isOpen);
        IsStartMenuOpen = isOpen;
        RefreshCursor();
    }

    public static void SetPauseMenuOpen(bool isOpen)
    {
        RecordMenuClose(IsPauseMenuOpen, isOpen);
        IsPauseMenuOpen = isOpen;
        RefreshCursor();
    }

    public static void SetInventoryOpen(bool isOpen)
    {
        RecordMenuClose(IsInventoryOpen, isOpen);
        IsInventoryOpen = isOpen;
        RefreshCursor();
    }

    public static void SetTraderMenuOpen(bool isOpen)
    {
        RecordMenuClose(IsTraderMenuOpen, isOpen);
        IsTraderMenuOpen = isOpen;
        RefreshCursor();
    }

    public static void SetQuestMenuOpen(bool isOpen)
    {
        RecordMenuClose(IsQuestMenuOpen, isOpen);
        IsQuestMenuOpen = isOpen;
        RefreshCursor();
    }

    public static void SetHomeMenuOpen(bool isOpen)
    {
        RecordMenuClose(IsHomeMenuOpen, isOpen);
        IsHomeMenuOpen = isOpen;
        RefreshCursor();
    }

    public static void SetIntroLetterOpen(bool isOpen)
    {
        RecordMenuClose(IsIntroLetterOpen, isOpen);
        IsIntroLetterOpen = isOpen;
        RefreshCursor();
    }

    public static void SetConfirmationOpen(bool isOpen)
    {
        RecordMenuClose(IsConfirmationOpen, isOpen);
        IsConfirmationOpen = isOpen;
        RefreshCursor();
    }

    public static void SetSettingsMenuOpen(bool isOpen)
    {
        RecordMenuClose(IsSettingsMenuOpen, isOpen);
        IsSettingsMenuOpen = isOpen;
        RefreshCursor();
    }

    public static void SetCharacterSelectionOpen(bool isOpen)
    {
        RecordMenuClose(IsCharacterSelectionOpen, isOpen);
        IsCharacterSelectionOpen = isOpen;
        RefreshCursor();
    }

    private static void RecordMenuClose(bool wasOpen, bool isOpen)
    {
        if (wasOpen && !isOpen)
        {
            lastMenuClosedFrame = Time.frameCount;
        }
    }

    private static void RefreshCursor()
    {
        if (AnyBlockingUIOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
