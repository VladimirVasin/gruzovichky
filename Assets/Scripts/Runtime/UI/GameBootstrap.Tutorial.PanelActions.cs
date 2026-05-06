public partial class GameBootstrap
{
    private void OpenBuildPanelFromTutorial()
    {
        if (isWorldMapPanelOpen)
        {
            CloseWorldMapPanel();
        }

        isFleetPanelOpen = false;
        isShiftsPanelOpen = false;
        isDriversPanelOpen = false;
        isResourcesPanelOpen = false;
        isEconomyPanelOpen = false;
        isTradePanelOpen = false;
        isWorldMapPanelOpen = false;
        isBuildPanelOpen = true;
        isBuildScreenDirty = true;
        LogUiInput("Tutorial: auto-opened Build panel after tutorial 2 OK");
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }

    private void OpenWorldMapPanelFromTutorial()
    {
        OpenWorldMapPanel();
        selectedWorldMapRegionIndex = GetTutorialRiverTradeRegionIndex();
        isWorldMapScreenDirty = true;
        NotifyTutorialWorldMapOpened();
        LogUiInput("Tutorial: auto-opened Regional Map panel");
    }

    private void OpenTradePanelFromTutorial()
    {
        if (isWorldMapPanelOpen)
        {
            CloseWorldMapPanel();
        }

        isFleetPanelOpen = false;
        isShiftsPanelOpen = false;
        isDriversPanelOpen = false;
        isResourcesPanelOpen = false;
        isEconomyPanelOpen = false;
        isWorldMapPanelOpen = false;
        isBuildPanelOpen = false;
        isStatesPanelOpen = false;
        isTruckDetailsOpen = false;
        isLocalBusDetailsOpen = false;
        isDriverDetailsOpen = false;
        activeBuildTool = BuildTool.None;
        hoveredBuildCell = null;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        CancelRoadPathMode();
        DisableTruckCameraFocus();
        RefreshSelectionVisuals();

        isTradePanelOpen = true;
        isTradeScreenDirty = true;
        LogUiInput("Tutorial: auto-opened Trade panel");
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }
}
