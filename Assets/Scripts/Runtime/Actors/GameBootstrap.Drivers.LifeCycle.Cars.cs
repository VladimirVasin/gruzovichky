using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private bool TryStartWorkerBuyCar(DriverAgent driver, Vector3 startPosition)
    {
        if (!locations.ContainsKey(LocationType.CarMarket) ||
            driver.AssignedPersonalHouseIndex < 0 ||
            driver.AssignedPersonalHouseIndex >= personalHouses.Count)
        {
            return false;
        }

        driver.LifeGoal = WorkerLifeGoal.BuyCar;
        Vector3 target = GetDriverStandPointNearLocation(LocationType.CarMarket);
        ResetWorkerLocalBusTripState(driver);
        if (TryStartWorkerLocalBusTrip(driver, startPosition, target, DriverRescuePhase.ToCarMarketForPurchase, "Car purchase"))
        {
            LogWorkerDecision(driver, "buy-car-via-bus", $"Car Market, fee=${CarPurchasePrice}", true);
            return true;
        }

        driver.WalkTargetWorld = target;
        driver.WalkPhase = DriverRescuePhase.ToCarMarketForPurchase;
        driver.WalkAnimationTime = 0f;
        BuildDriverWalkPath(driver, startPosition, target);
        LogWorkerDecision(driver, "buy-car-walk", $"Car Market, fee=${CarPurchasePrice}", true);
        return true;
    }
}
