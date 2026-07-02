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
    public static bool AnyMenuOpen => IsStartMenuOpen || IsPauseMenuOpen || IsInventoryOpen || IsTraderMenuOpen || IsQuestMenuOpen || IsHomeMenuOpen || IsIntroLetterOpen;

    public static void SetStartMenuOpen(bool isOpen)
    {
        IsStartMenuOpen = isOpen;
        RefreshCursor();
    }

    public static void SetPauseMenuOpen(bool isOpen)
    {
        IsPauseMenuOpen = isOpen;
        RefreshCursor();
    }

    public static void SetInventoryOpen(bool isOpen)
    {
        IsInventoryOpen = isOpen;
        RefreshCursor();
    }

    public static void SetTraderMenuOpen(bool isOpen)
    {
        IsTraderMenuOpen = isOpen;
        RefreshCursor();
    }

    public static void SetQuestMenuOpen(bool isOpen)
    {
        IsQuestMenuOpen = isOpen;
        RefreshCursor();
    }

    public static void SetHomeMenuOpen(bool isOpen)
    {
        IsHomeMenuOpen = isOpen;
        RefreshCursor();
    }

    public static void SetIntroLetterOpen(bool isOpen)
    {
        IsIntroLetterOpen = isOpen;
        RefreshCursor();
    }

    private static void RefreshCursor()
    {
        if (AnyMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
