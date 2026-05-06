using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
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

    private bool TryStartWorkerServiceVisit(DriverAgent driver, LocationType type, WorkerLifeGoal goal, DriverRescuePhase walkPhase, float duration, Vector3 startPosition)
    {
        if (driver == null || !locations.TryGetValue(type, out LocationData service))
        {
            LogWorkerDecision(driver, "service-unavailable", $"{type} not built", true);
            return false;
        }

        if (driver.Money < service.ServiceFee)
        {
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
