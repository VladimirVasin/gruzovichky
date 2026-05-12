using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerShiftCommutePathStartsPerFrame = 2;

    private int workerShiftCommutePathBudgetFrame = -1;
    private int workerShiftCommutePathStartsThisFrame;

    private bool TryConsumeWorkerShiftCommutePathBudget(DriverAgent driver)
    {
        if (workerShiftCommutePathBudgetFrame != Time.frameCount)
        {
            workerShiftCommutePathBudgetFrame = Time.frameCount;
            workerShiftCommutePathStartsThisFrame = 0;
        }

        if (workerShiftCommutePathStartsThisFrame < WorkerShiftCommutePathStartsPerFrame)
        {
            workerShiftCommutePathStartsThisFrame++;
            return true;
        }

        if (driver != null)
        {
            driver.IdleWanderPauseTimer = Mathf.Max(driver.IdleWanderPauseTimer, 0.15f);
        }

        return false;
    }
}
