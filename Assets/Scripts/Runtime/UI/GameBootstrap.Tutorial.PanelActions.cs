public partial class GameBootstrap
{
    private void OpenWorkersPanelFromTutorial()
    {
        isFleetPanelOpen = false;
        isShiftsPanelOpen = false;
        isResourcesPanelOpen = false;
        isEconomyPanelOpen = false;
        isBuildPanelOpen = false;
        isWorldMapPanelOpen = false;
        isDriversPanelOpen = true;
        isDriversScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.WorkersPanelOpened);
        LogUiInput("Tutorial: auto-opened Workers panel after tutorial 3 OK");
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }

    private void OpenBuildPanelFromTutorial()
    {
        isFleetPanelOpen = false;
        isShiftsPanelOpen = false;
        isDriversPanelOpen = false;
        isResourcesPanelOpen = false;
        isEconomyPanelOpen = false;
        isWorldMapPanelOpen = false;
        isBuildPanelOpen = true;
        isBuildScreenDirty = true;
        LogUiInput("Tutorial: auto-opened Build panel after tutorial 2 OK");
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }
}
