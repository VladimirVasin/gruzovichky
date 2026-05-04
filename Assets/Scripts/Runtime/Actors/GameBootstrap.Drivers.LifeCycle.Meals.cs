using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private bool TryStartWorkerMealAtHome(DriverAgent driver, Vector3 startPosition)
    {
        if (driver == null ||
            driver.AssignedPersonalHouseIndex < 0 ||
            driver.AssignedPersonalHouseIndex >= personalHouses.Count)
        {
            return false;
        }

        driver.LifeGoal = WorkerLifeGoal.Eat;
        driver.IdleActivityTimer = WorkerCanteenDuration;
        Vector3 target = GetDriverStandPointNearPersonalHouse(driver.AssignedPersonalHouseIndex);
        ResetWorkerLocalBusTripState(driver);
        if (TryStartWorkerPersonalCarTrip(driver, startPosition, target, DriverRescuePhase.ToPersonalHouseMeal, "Home meal"))
        {
            LogWorkerDecision(driver, "meal-home-by-car", $"House #{driver.AssignedPersonalHouseIndex}; duration={WorkerCanteenDuration:0.0}s", true);
            return true;
        }

        if (CanWorkerUsePersonalCar(driver))
        {
            LogWorkerDecision(driver, "meal-home-car-blocked", $"House #{driver.AssignedPersonalHouseIndex}: no personal car route", true);
            driver.LifeGoal = WorkerLifeGoal.None;
            return false;
        }

        if (TryStartWorkerLocalBusTrip(driver, startPosition, target, DriverRescuePhase.ToPersonalHouseMeal, "Home meal"))
        {
            LogWorkerDecision(driver, "meal-home-via-bus", $"House #{driver.AssignedPersonalHouseIndex}; duration={WorkerCanteenDuration:0.0}s", true);
            return true;
        }

        driver.WalkTargetWorld = target;
        driver.WalkPhase = DriverRescuePhase.ToPersonalHouseMeal;
        driver.WalkAnimationTime = 0f;
        if (!BuildDriverWalkPath(driver, startPosition, target))
        {
            driver.WalkPhase = DriverRescuePhase.None;
            driver.WalkTargetWorld = startPosition;
            driver.LifeGoal = WorkerLifeGoal.None;
            LogWorkerDecision(driver, "meal-home-path-blocked", $"House #{driver.AssignedPersonalHouseIndex}: no safe path", true);
            return false;
        }

        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} heading home for Meal; house=#{driver.AssignedPersonalHouseIndex}, need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
        LogWorkerDecision(driver, "meal-home-walk", $"House #{driver.AssignedPersonalHouseIndex}; duration={WorkerCanteenDuration:0.0}s", true);
        return true;
    }
}
