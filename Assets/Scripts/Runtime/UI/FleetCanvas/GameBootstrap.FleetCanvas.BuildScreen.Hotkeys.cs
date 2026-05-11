using UnityEngine.InputSystem;

public partial class GameBootstrap
{
    private bool TryHandleBuildMenuEscape()
    {
        if (!isBuildPanelOpen)
        {
            return false;
        }

        if (selectedBuildCategoryIndex >= 0 || activeBuildTool != BuildTool.None)
        {
            selectedBuildCategoryIndex = -1;
            activeBuildTool = BuildTool.None;
            hoveredBuildCell = null;
            CancelRoadPathMode();
            isBuildScreenDirty = true;
            LogUiInput("Build Canvas: escaped to category layer");
            SessionDebugLogger.Log("BUILD", "Build menu escaped to category layer.");
            PlayUiSound(uiPanelCloseClip, 0.66f);
            RefreshSelectionVisuals();
            return true;
        }

        isBuildPanelOpen = false;
        selectedBuildCategoryIndex = -1;
        isBuildScreenDirty = true;
        LogUiInput("Build Canvas: closed by Escape");
        SessionDebugLogger.Log("BUILD", "Build menu closed by Escape.");
        PlayUiSound(uiPanelCloseClip, 0.82f);
        return true;
    }

    private bool TryHandleBuildMenuHotkey()
    {
        if (!isBuildPanelOpen || buildScreenUi?.Categories == null)
        {
            return false;
        }

        int hotkeyNumber = GetPressedBuildMenuHotkeyNumber();
        if (hotkeyNumber <= 0)
        {
            return false;
        }

        EnsureSelectedBuildCategory();
        if (selectedBuildCategoryIndex >= 0 && TryActivateVisibleBuildItemHotkey(hotkeyNumber))
        {
            return true;
        }

        return TrySelectVisibleBuildCategoryHotkey(hotkeyNumber);
    }

    private bool TrySelectVisibleBuildCategoryHotkey(int hotkeyNumber)
    {
        int visibleNumber = 0;
        foreach (BuildCategoryUi category in buildScreenUi.Categories)
        {
            if (!HasUnlockedBuildItems(category))
            {
                continue;
            }

            visibleNumber++;
            if (visibleNumber != hotkeyNumber)
            {
                continue;
            }

            if (selectedBuildCategoryIndex == category.Index)
            {
                UnfocusBuildCategoryFromHotkey(hotkeyNumber, category);
                return true;
            }

            SelectBuildCategoryFromMenu(category, false);
            LogUiInput($"Build Canvas hotkey {hotkeyNumber}: selected category {category.LabelEn}");
            SessionDebugLogger.Log("BUILD", $"Build hotkey {hotkeyNumber} selected category {category.LabelEn}.");
            return true;
        }

        return true;
    }

    private void UnfocusBuildCategoryFromHotkey(int hotkeyNumber, BuildCategoryUi category)
    {
        selectedBuildCategoryIndex = -1;
        activeBuildTool = BuildTool.None;
        hoveredBuildCell = null;
        CancelRoadPathMode();
        isBuildScreenDirty = true;
        LogUiInput($"Build Canvas hotkey {hotkeyNumber}: unfocused category {category.LabelEn}");
        SessionDebugLogger.Log("BUILD", $"Build hotkey {hotkeyNumber} unfocused category {category.LabelEn}.");
        PlayUiSound(uiPanelCloseClip, 0.64f);
        RefreshSelectionVisuals();
    }

    private bool TryActivateVisibleBuildItemHotkey(int hotkeyNumber)
    {
        if (selectedBuildCategoryIndex < 0 || selectedBuildCategoryIndex >= buildScreenUi.Categories.Length)
        {
            return false;
        }

        BuildCategoryUi category = buildScreenUi.Categories[selectedBuildCategoryIndex];
        int visibleNumber = 0;
        foreach (BuildItemUi item in category.Items)
        {
            if (!IsBuildToolUnlocked(item.Tool))
            {
                continue;
            }

            visibleNumber++;
            if (visibleNumber != hotkeyNumber)
            {
                continue;
            }

            TryToggleBuildToolFromBuildMenu(item.Tool, $"Build Canvas hotkey {hotkeyNumber}");
            return true;
        }

        return false;
    }

    private void SelectBuildCategoryFromMenu(BuildCategoryUi category, bool allowToggle)
    {
        if (category == null)
        {
            return;
        }

        bool wasSelected = selectedBuildCategoryIndex == category.Index;
        bool shouldClose = allowToggle && wasSelected;
        selectedBuildCategoryIndex = shouldClose ? -1 : category.Index;
        if (!wasSelected)
        {
            buildScreenTrayAnimation = 0f;
        }

        isBuildScreenDirty = true;
        PlayUiSound(shouldClose ? uiPanelCloseClip : uiSelectClip, shouldClose ? 0.64f : 0.70f);
    }

    private bool TryToggleBuildToolFromBuildMenu(BuildTool tool, string source)
    {
        if (!IsBuildToolUnlocked(tool) || IsBuildToolTemporarilyUnavailable(tool))
        {
            return false;
        }

        activeBuildTool = activeBuildTool == tool ? BuildTool.None : tool;
        LogUiInput($"{source}: switched tool to {activeBuildTool}");
        PlayUiSound(uiSelectClip, 0.85f);
        SessionDebugLogger.Log("BUILD", $"Build tool switched to {activeBuildTool}.");
        isBuildScreenDirty = true;
        return true;
    }

    private static string FormatBuildMenuHotkeyLabel(int hotkeyNumber, string label)
    {
        return hotkeyNumber > 0
            ? hotkeyNumber + "." + (label ?? string.Empty)
            : label ?? string.Empty;
    }

    private static int GetPressedBuildMenuHotkeyNumber()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return 0;
        }

        if (WasPressed(keyboard.digit1Key) || WasPressed(keyboard.numpad1Key)) return 1;
        if (WasPressed(keyboard.digit2Key) || WasPressed(keyboard.numpad2Key)) return 2;
        if (WasPressed(keyboard.digit3Key) || WasPressed(keyboard.numpad3Key)) return 3;
        if (WasPressed(keyboard.digit4Key) || WasPressed(keyboard.numpad4Key)) return 4;
        if (WasPressed(keyboard.digit5Key) || WasPressed(keyboard.numpad5Key)) return 5;
        if (WasPressed(keyboard.digit6Key) || WasPressed(keyboard.numpad6Key)) return 6;
        if (WasPressed(keyboard.digit7Key) || WasPressed(keyboard.numpad7Key)) return 7;
        if (WasPressed(keyboard.digit8Key) || WasPressed(keyboard.numpad8Key)) return 8;
        if (WasPressed(keyboard.digit9Key) || WasPressed(keyboard.numpad9Key)) return 9;
        return 0;
    }

    private static bool WasPressed(UnityEngine.InputSystem.Controls.KeyControl key)
    {
        return key != null && key.wasPressedThisFrame;
    }
}
