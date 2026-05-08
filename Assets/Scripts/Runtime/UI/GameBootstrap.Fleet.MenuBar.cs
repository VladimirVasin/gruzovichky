public partial class GameBootstrap
{
    private int GetVisibleMenuButtonCount()
    {
        return DefaultMenuBtnCount + (ShouldShowTutorialStaffingMenuButton() ? 1 : 0);
    }

    private bool ShouldShowTutorialStaffingMenuButton()
    {
        if (isShiftsPanelOpen)
        {
            return true;
        }

        if (selectedGameStartMode != GameStartMode.Tutorial ||
            isTutorialSkipped ||
            !isTutorialGoalsActive)
        {
            return false;
        }

        return tutorialGoalsMode is TutorialGoalsMode.LumberjackCamp
            or TutorialGoalsMode.BuyTruck
            or TutorialGoalsMode.WarehouseLoaders
            or TutorialGoalsMode.LocalTransport
            or TutorialGoalsMode.Docks;
    }
}
