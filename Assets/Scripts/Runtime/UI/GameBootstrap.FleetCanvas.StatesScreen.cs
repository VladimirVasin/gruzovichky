public partial class GameBootstrap
{
    private bool isStatesScreenDirty = true;

    private void SetupStatesScreenUi()
    {
        isStatesScreenDirty = false;
    }

    private void UpdateStatesScreenUi()
    {
        if (!isStatesScreenDirty && !isStatesPanelOpen)
        {
            return;
        }

        if (isStatesPanelOpen)
        {
            isStatesPanelOpen = false;
        }

        isStatesScreenDirty = false;
    }
}
