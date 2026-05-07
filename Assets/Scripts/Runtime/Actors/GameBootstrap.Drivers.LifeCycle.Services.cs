using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private const int WorkerVendorPurchasePrice = 4;
    private const float WorkerVendorPurchaseDuration = 2.4f;

    private bool TryStartWeightedLeisureGoal(DriverAgent driver, Vector3 startPosition)
    {
        bool isAlcoholic = HasWorkerPerk(driver, WorkerPerkKind.Alcoholism);
        bool isGambler = HasWorkerPerk(driver, WorkerPerkKind.Gambler);

        // Alcoholism remains a strong behavioral rule: if a working Bar exists, it wins first.
        if (isAlcoholic &&
            TryStartWorkerServiceVisit(driver, LocationType.Bar, WorkerLifeGoal.Leisure, DriverRescuePhase.IdleWalkToBar, WorkerLeisureDuration, startPosition))
        {
            SessionDebugLogger.Log("LIFE", $"{driver.DriverName} selected Bar for Leisure due to Alcoholism.");
            return true;
        }

        List<LocationType> weightedChoices = new();
        if (CanWorkerConsiderLeisureService(driver, LocationType.CityPark))
        {
            AddWeightedLeisureChoice(weightedChoices, LocationType.CityPark, isGambler ? 3 : 5);
        }
        if (CanWorkerConsiderLeisureService(driver, LocationType.GamblingHall))
        {
            AddWeightedLeisureChoice(weightedChoices, LocationType.GamblingHall, isGambler ? 7 : 3);
        }
        if (CanWorkerConsiderLeisureService(driver, LocationType.Bar))
        {
            AddWeightedLeisureChoice(weightedChoices, LocationType.Bar, isGambler ? 1 : 2);
        }

        if (weightedChoices.Count == 0)
        {
            LogWorkerDecision(driver, "leisure-no-candidates", "no available Bar/GamblingHall/CityPark candidates", true);
            return false;
        }

        int pickedIndex = Random.Range(0, weightedChoices.Count);
        LocationType picked = weightedChoices[pickedIndex];
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} rolled leisure target {picked}; candidates={weightedChoices.Count}, gambler={isGambler}, alcoholic={isAlcoholic}.");
        if (TryStartLeisureServiceVisit(driver, picked, startPosition))
        {
            return true;
        }

        LocationType[] fallbackOrder = { LocationType.CityPark, LocationType.GamblingHall, LocationType.Bar };
        foreach (LocationType fallback in fallbackOrder)
        {
            if (fallback == picked || !CanWorkerConsiderLeisureService(driver, fallback))
            {
                continue;
            }

            if (TryStartLeisureServiceVisit(driver, fallback, startPosition))
            {
                SessionDebugLogger.Log("LIFE", $"{driver.DriverName} switched leisure target from {picked} to {fallback}.");
                return true;
            }
        }

        return false;
    }

    private static void AddWeightedLeisureChoice(List<LocationType> choices, LocationType type, int weight)
    {
        for (int i = 0; i < weight; i++)
        {
            choices.Add(type);
        }
    }

    private bool CanWorkerConsiderLeisureService(DriverAgent driver, LocationType type)
    {
        if (driver == null || !locations.TryGetValue(type, out LocationData service))
        {
            return false;
        }

        if (driver.Money < service.ServiceFee)
        {
            return false;
        }

        if (type == LocationType.GamblingHall && !HasWorkerPerk(driver, WorkerPerkKind.Gambler) && driver.Money < WorkerGamblingMinBalance)
        {
            return false;
        }

        return true;
    }

    private bool TryStartLeisureServiceVisit(DriverAgent driver, LocationType type, Vector3 startPosition)
    {
        return type switch
        {
            LocationType.Bar => TryStartWorkerServiceVisit(driver, LocationType.Bar, WorkerLifeGoal.Leisure, DriverRescuePhase.IdleWalkToBar, WorkerLeisureDuration, startPosition),
            LocationType.GamblingHall => TryStartWorkerServiceVisit(driver, LocationType.GamblingHall, WorkerLifeGoal.Leisure, DriverRescuePhase.IdleWalkToGamblingHall, WorkerGamblingHallDuration, startPosition),
            LocationType.CityPark => TryStartWorkerServiceVisit(driver, LocationType.CityPark, WorkerLifeGoal.Leisure, DriverRescuePhase.IdleWalkToCityPark, WorkerCityParkDuration, startPosition),
            _ => false
        };
    }

    private bool TryStartWorkerIdleVendorPurchase(DriverAgent driver, Vector3 startPosition)
    {
        if (driver == null || driver.Money < WorkerVendorPurchasePrice)
        {
            return false;
        }

        List<(LocationType Type, string ItemId, DriverRescuePhase WalkPhase)> choices = new();
        if (CanWorkerConsiderVendorPurchase(driver, LocationType.Kiosk, WorkerSnackItemId))
        {
            choices.Add((LocationType.Kiosk, WorkerSnackItemId, DriverRescuePhase.IdleWalkToKiosk));
        }

        if (CanWorkerConsiderVendorPurchase(driver, LocationType.Kiosk, WorkerCoffeeItemId))
        {
            choices.Add((LocationType.Kiosk, WorkerCoffeeItemId, DriverRescuePhase.IdleWalkToKiosk));
        }

        if (choices.Count == 0)
        {
            return false;
        }

        int startIndex = Random.Range(0, choices.Count);
        for (int i = 0; i < choices.Count; i++)
        {
            var choice = choices[(startIndex + i) % choices.Count];
            if (TryStartWorkerVendorPurchase(driver, choice.Type, choice.ItemId, choice.WalkPhase, startPosition))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanWorkerConsiderVendorPurchase(DriverAgent driver, LocationType vendorType, string itemId)
    {
        if (driver == null ||
            driver.Money < WorkerVendorPurchasePrice ||
            !CanWorkerReceiveVendorInventoryItem(driver, itemId))
        {
            return false;
        }

        foreach (LocationData vendor in EnumerateLocationsOfType(vendorType))
        {
            if (vendor != null && driver.Money >= Mathf.Max(WorkerVendorPurchasePrice, vendor.ServiceFee))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryStartWorkerCriticalNeedVendorPurchase(DriverAgent driver, WorkerNeedKind need, Vector3 startPosition)
    {
        if (driver == null || GetWorkerNeedLastStatus(driver, need) != WorkerNeedStatus.Critical)
        {
            return false;
        }

        string itemId = need switch
        {
            WorkerNeedKind.Meal => WorkerSnackItemId,
            WorkerNeedKind.Sleep => WorkerCoffeeItemId,
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(itemId) ||
            HasWorkerInventoryItem(driver, itemId) ||
            !CanWorkerConsiderVendorPurchase(driver, LocationType.Kiosk, itemId))
        {
            return false;
        }

        if (TryStartWorkerVendorPurchase(driver, LocationType.Kiosk, itemId, DriverRescuePhase.IdleWalkToKiosk, startPosition, GetWorkerGoalForNeed(need)))
        {
            LogWorkerDecision(driver, "critical-need-kiosk", $"{need}: buying {itemId} before fallback; need={FormatWorkerNeedDebug(driver, need)}", true);
            return true;
        }

        return false;
    }

    private static WorkerLifeGoal GetWorkerGoalForNeed(WorkerNeedKind need)
    {
        return need switch
        {
            WorkerNeedKind.Meal => WorkerLifeGoal.Eat,
            WorkerNeedKind.Sleep => WorkerLifeGoal.Sleep,
            WorkerNeedKind.Leisure => WorkerLifeGoal.Leisure,
            _ => WorkerLifeGoal.Idle
        };
    }

    private bool TryStartWorkerVendorPurchase(DriverAgent driver, LocationType vendorType, string itemId, DriverRescuePhase walkPhase, Vector3 startPosition, WorkerLifeGoal purchaseGoal = WorkerLifeGoal.Idle)
    {
        if (!TryGetNearestVendorPurchaseTarget(driver, vendorType, walkPhase, startPosition, out LocationData vendor, out Vector3 target))
        {
            LogWorkerDecision(driver, "vendor-purchase-blocked", $"{vendorType}: no reachable stand for {itemId}", true);
            return false;
        }

        driver.LifeGoal = purchaseGoal;
        driver.IdleActivityTimer = WorkerVendorPurchaseDuration;
        driver.PendingVendorLocationInstanceId = vendor.InstanceId;
        driver.PendingVendorItemId = itemId;
        driver.WalkTargetWorld = target;
        driver.WalkPhase = walkPhase;
        driver.WalkAnimationTime = 0f;
        ResetWorkerLocalBusTripState(driver);
        if (!BuildDriverWalkPath(driver, startPosition, target))
        {
            driver.WalkPhase = DriverRescuePhase.None;
            driver.LifeGoal = WorkerLifeGoal.None;
            driver.IdleActivityTimer = 0f;
            driver.PendingVendorLocationInstanceId = 0;
            driver.PendingVendorItemId = string.Empty;
            LogWorkerDecision(driver, "vendor-purchase-path-blocked", $"{vendorType}: no safe walk path for {itemId}", true);
            return false;
        }

        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} heading to {vendor.Label}#{vendor.InstanceId} to buy {itemId}.");
        LogWorkerDecision(driver, "vendor-purchase-walk", $"{vendorType}#{vendor.InstanceId}; item={itemId}; price=${WorkerVendorPurchasePrice}", true);
        return true;
    }

    private bool TryGetNearestVendorPurchaseTarget(DriverAgent driver, LocationType vendorType, DriverRescuePhase walkPhase, Vector3 startPosition, out LocationData vendor, out Vector3 target)
    {
        vendor = null;
        target = Vector3.zero;
        if (driver == null)
        {
            return false;
        }

        float bestScore = float.PositiveInfinity;
        Vector2Int startCell = WorldToCell(startPosition);
        foreach (LocationData candidate in EnumerateLocationsOfType(vendorType))
        {
            if (candidate == null || driver.Money < Mathf.Max(WorkerVendorPurchasePrice, candidate.ServiceFee))
            {
                continue;
            }

            Vector3 candidateTarget = GetVendorStandPoint(candidate);
            Vector2Int goalCell = WorldToCell(candidateTarget);
            List<Vector2Int> path = FindDriverWalkPath(startCell, goalCell, walkPhase);
            if (path == null || path.Count == 0)
            {
                continue;
            }

            float score = path.Count + (candidateTarget - startPosition).sqrMagnitude * 0.01f;
            if (score < bestScore)
            {
                bestScore = score;
                vendor = candidate;
                target = candidateTarget;
            }
        }

        return vendor != null;
    }

    private Vector3 GetVendorStandPoint(LocationData vendor)
    {
        if (vendor == null)
        {
            return Vector3.zero;
        }

        Vector3 target = GetCellCenter(vendor.Anchor);
        Vector3 center = GetLocationCenter(vendor);
        Vector3 facing = target - center;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.001f)
        {
            target -= facing.normalized * 0.08f;
        }

        target.x += Random.Range(-0.08f, 0.08f);
        target.z += Random.Range(-0.08f, 0.08f);
        target.y = SampleTerrainHeight(target.x, target.z);
        return target;
    }

    private static bool TryGetVendorPurchaseForPhase(DriverAgent driver, DriverRescuePhase phase, out LocationType vendorType, out string itemId, out DriverRescuePhase atPhase)
    {
        switch (phase)
        {
            case DriverRescuePhase.IdleWalkToKiosk:
            case DriverRescuePhase.IdleAtKiosk:
                vendorType = LocationType.Kiosk;
                itemId = IsWorkerVendorInventoryItem(driver?.PendingVendorItemId)
                    ? driver.PendingVendorItemId
                    : WorkerSnackItemId;
                atPhase = DriverRescuePhase.IdleAtKiosk;
                return true;
        }

        vendorType = default;
        itemId = string.Empty;
        atPhase = DriverRescuePhase.None;
        return false;
    }

    private bool CompleteWorkerVendorPurchase(DriverAgent driver, DriverRescuePhase completedPhase, Vector3 purchasePosition)
    {
        if (driver == null ||
            !TryGetVendorPurchaseForPhase(driver, completedPhase, out LocationType vendorType, out string itemId, out _))
        {
            return false;
        }

        LocationData vendor = FindLocationByInstanceId(driver.PendingVendorLocationInstanceId);
        driver.PendingVendorLocationInstanceId = 0;
        driver.PendingVendorItemId = string.Empty;
        if (vendor == null || vendor.Type != vendorType)
        {
            locations.TryGetValue(vendorType, out vendor);
        }

        if (vendor == null)
        {
            SessionDebugLogger.Log("LIFE", $"{driver.DriverName} could not complete {itemId} purchase: {vendorType} was removed.");
            return false;
        }

        int price = Mathf.Max(WorkerVendorPurchasePrice, vendor.ServiceFee);
        if (driver.Money < price)
        {
            LogWorkerDecision(driver, "vendor-purchase-unaffordable", $"{vendorType}: ${driver.Money}/${price} for {itemId}", true);
            SessionDebugLogger.Log("LIFE", $"{driver.DriverName} could not afford {itemId} at {vendor.Label}; balance=${driver.Money}, price=${price}.");
            return false;
        }

        if (!CanWorkerReceiveVendorInventoryItem(driver, itemId))
        {
            LogWorkerDecision(driver, "vendor-purchase-skipped", $"{vendorType}: already has max {itemId}", true);
            return false;
        }

        int moneyBefore = driver.Money;
        int bankBefore = vendor.BuildingBank;
        if (!TryAddWorkerInventoryItem(driver, itemId, 1, $"vendor:{vendor.Type}:{vendor.InstanceId}"))
        {
            LogWorkerDecision(driver, "vendor-purchase-failed", $"{vendorType}: inventory rejected {itemId}", true);
            return false;
        }

        driver.Money -= price;
        vendor.BuildingBank += price;
        SpawnMoneySpendPopup(purchasePosition, price);
        LogBuildingBankTransaction(vendor, driver, price, $"{GetWorkerInventoryItemTitle(itemId, false)} purchase", moneyBefore, bankBefore);
        TryAutoUseNeedConsumables(driver);
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} bought {itemId} at {vendor.Label}#{vendor.InstanceId} for ${price}; balance=${driver.Money}.");
        isDriversScreenDirty = true;
        return true;
    }

    private bool TryStartWorkerServiceVisit(DriverAgent driver, LocationType type, WorkerLifeGoal goal, DriverRescuePhase walkPhase, float duration, Vector3 startPosition)
    {
        if (driver == null || !locations.TryGetValue(type, out LocationData service))
        {
            WorkerNeedKind missingNeed = goal == WorkerLifeGoal.Eat
                ? WorkerNeedKind.Meal
                : goal == WorkerLifeGoal.Sleep
                    ? WorkerNeedKind.Sleep
                    : WorkerNeedKind.Leisure;
            RecordWorkerServiceMissingThought(driver, type, missingNeed, $"{type} not built");
            LogWorkerDecision(driver, "service-unavailable", $"{type} not built", true);
            return false;
        }

        if (driver.Money < service.ServiceFee)
        {
            RecordWorkerThought(
                driver,
                WorkerThoughtKind.Money,
                WorkerThoughtTone.Negative,
                74,
                "service_unaffordable",
                new[]
                {
                    ThoughtBuilding("service", type),
                    ThoughtText("balance", $"${driver.Money}")
                },
                WorkerThoughtSubjectType.BuildingType,
                0,
                type.ToString(),
                GetSelectedLocationDisplayName(type),
                -7,
                $"service_unaffordable|{type}",
                5f);
            LogWorkerDecision(driver, "service-unavailable", $"{type}: {GetWorkerServiceUnavailableReason(driver, type)}", true);
            return false;
        }

        Vector3 target;
        if (type == LocationType.CityPark)
        {
            target = GetNearestCityParkEntranceTarget(service, startPosition);
        }
        else
        {
            target = GetCellCenter(service.RoadAccess == default ? service.Anchor : service.RoadAccess);
            target.x += Random.Range(-0.18f, 0.18f);
            target.z += Random.Range(-0.18f, 0.18f);
            target.y = SampleTerrainHeight(target.x, target.z);
        }
        driver.LifeGoal = goal;
        driver.IdleActivityTimer = duration;
        ResetWorkerLocalBusTripState(driver);
        if (TryStartWorkerPersonalCarTrip(driver, startPosition, target, walkPhase, $"{type} visit"))
        {
            LogWorkerDecision(driver, "service-visit-by-car", $"{type} for {goal}; fee=${service.ServiceFee}; duration={duration:0.0}s", true);
            return true;
        }
        if (CanWorkerUsePersonalCar(driver))
        {
            LogWorkerDecision(driver, "service-visit-car-blocked", $"{type} for {goal}: no personal car route", true);
            return false;
        }

        if (TryStartWorkerLocalBusTrip(driver, startPosition, target, walkPhase, $"{type} visit"))
        {
            LogWorkerDecision(driver, "service-visit-via-bus", $"{type} for {goal}; fee=${service.ServiceFee}; duration={duration:0.0}s", true);
            return true;
        }

        driver.WalkTargetWorld = target;
        driver.WalkPhase = walkPhase;
        driver.WalkAnimationTime = 0f;
        if (!BuildDriverWalkPath(driver, startPosition, target))
        {
            driver.WalkPhase = DriverRescuePhase.None;
            driver.LifeGoal = WorkerLifeGoal.None;
            driver.IdleActivityTimer = 0f;
            LogWorkerDecision(driver, "service-visit-path-blocked", $"{type} for {goal}; no safe walk path", true);
            return false;
        }
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} heading to {type} for {goal}; serviceFee=${service.ServiceFee}, need={FormatWorkerNeedDebug(driver, goal == WorkerLifeGoal.Eat ? WorkerNeedKind.Meal : WorkerNeedKind.Leisure)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
        LogWorkerDecision(driver, "service-visit-walk", $"{type} for {goal}; fee=${service.ServiceFee}; duration={duration:0.0}s", true);
        return true;
    }

    private Vector3 GetNearestCityParkEntranceTarget(LocationData park, Vector3 startPosition)
    {
        float centerX = (park.Min.x + park.Max.x + 1) * 0.5f;
        float centerZ = (park.Min.y + park.Max.y + 1) * 0.5f;
        float jitter = Random.Range(-1.15f, 1.15f);
        Vector3[] candidates =
        {
            new(centerX + jitter, 0f, park.Min.y - 0.55f),
            new(centerX - jitter, 0f, park.Max.y + 1.55f),
            new(park.Min.x - 0.55f, 0f, centerZ - jitter),
            new(park.Max.x + 1.55f, 0f, centerZ + jitter)
        };

        Vector3 best = candidates[0];
        float bestScore = float.PositiveInfinity;
        Vector2Int startCell = WorldToCell(startPosition);
        for (int i = 0; i < candidates.Length; i++)
        {
            Vector3 candidate = candidates[i];
            candidate.y = SampleTerrainHeight(candidate.x, candidate.z);
            Vector2Int goalCell = WorldToCell(candidate);
            List<Vector2Int> path = FindDriverWalkPath(startCell, goalCell, DriverRescuePhase.IdleWalkToCityPark);
            float pathPenalty = path == null || path.Count == 0 ? 10000f : path.Count;
            float score = pathPenalty + (candidate - startPosition).sqrMagnitude * 0.01f;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }
}
