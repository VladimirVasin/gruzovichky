public partial class GameBootstrap
{
    private void SetTutorialClockToProductionStart()
    {
    }

    private void NotifyTutorialProductionWorkerEntered(LocationType locationType)
    {
    }

    private void NotifyTutorialSawmillBuilt()
    {
    }

    private void StartTutorialBusStopWorkerArrival()
    {
    }

    private void ShowTutorialOrbitHud(
        DriverAgent worker,
        string message,
        string stepLabel = "",
        System.Action onOk = null,
        string speakerProfessionOverrideKey = null)
    {
        onOk?.Invoke();
    }

    private void UpdateTutorialOrbitHud(float dt)
    {
    }

    private void HideTutorialOrbitHud()
    {
    }
}
