using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private bool TryStartWorkerBuyHouseBeforeMotelSleep(DriverAgent driver, Vector3 startPosition)
    {
        if (driver == null || driver.AssignedPersonalHouseIndex >= 0)
            return false;

        if (!TryStartWorkerBuyHouse(driver, startPosition))
            return false;

        LogWorkerDecision(driver, "sleep-buy-house-first", $"buying house before Motel sleep; fee=${HousePurchasePrice}", true);
        return true;
    }

    private bool TryStartWorkerBuyHouse(DriverAgent driver, Vector3 startPosition)
    {
        if (!TryFindAvailablePersonalHouse(driver, out int targetIndex, out string unavailableReason))
        {
            LogWorkerDecision(driver, "buy-house-skip", unavailableReason, true);
            return false;
        }

        driver.AssignedPersonalHouseIndex = targetIndex;
        driver.LifeGoal = WorkerLifeGoal.BuyHouse;
        Vector3 target = GetDriverStandPointNearPersonalHouse(targetIndex);
        ResetWorkerLocalBusTripState(driver);
        if (TryStartWorkerLocalBusTrip(driver, startPosition, target, DriverRescuePhase.ToPersonalHouseForPurchase, "House purchase"))
        {
            LogWorkerDecision(driver, "buy-house-via-bus", $"House #{targetIndex}, fee=${HousePurchasePrice}", true);
            return true;
        }

        driver.WalkTargetWorld = target;
        driver.WalkPhase = DriverRescuePhase.ToPersonalHouseForPurchase;
        driver.WalkAnimationTime = 0f;
        if (!BuildDriverWalkPath(driver, startPosition, target))
        {
            driver.AssignedPersonalHouseIndex = -1;
            driver.WalkPhase = DriverRescuePhase.None;
            driver.LifeGoal = WorkerLifeGoal.None;
            LogWorkerDecision(driver, "buy-house-path-blocked", $"House #{targetIndex}: no safe path", true);
            return false;
        }
        LogWorkerDecision(driver, "buy-house-walk", $"House #{targetIndex}, fee=${HousePurchasePrice}", true);
        return true;
    }

    private bool TryFindAvailablePersonalHouse(DriverAgent driver, out int houseIndex, out string unavailableReason)
    {
        houseIndex = -1;
        unavailableReason = "worker missing";
        if (driver == null)
            return false;

        if (driver.Money < HousePurchasePrice)
        {
            unavailableReason = $"not enough money (${driver.Money} < ${HousePurchasePrice})";
            return false;
        }

        if (personalHouses.Count == 0)
        {
            unavailableReason = "no Personal House built";
            return false;
        }

        for (int i = 0; i < personalHouses.Count; i++)
        {
            if (CountPersonalHouseResidents(i) == 0)
            {
                houseIndex = i;
                unavailableReason = string.Empty;
                return true;
            }
        }

        unavailableReason = "no empty Personal House available";
        return false;
    }

    private int CountPersonalHouseResidents(int houseIndex)
    {
        int residents = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            if (driverAgents[i] != null && driverAgents[i].AssignedPersonalHouseIndex == houseIndex)
                residents++;
        }

        return residents;
    }
}
