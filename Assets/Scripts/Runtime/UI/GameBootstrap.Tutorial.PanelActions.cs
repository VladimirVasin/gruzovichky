public partial class GameBootstrap
{
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
