using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private bool CanWorkerUsePersonalCar(DriverAgent driver)
    {
        return driver != null &&
               driver.OwnedCarModelIndex >= 0 &&
               driver.AssignedPersonalHouseIndex >= 0 &&
               driver.AssignedPersonalHouseIndex < personalHouses.Count &&
               !driver.IsDrivingPersonalCar &&
               !driver.HasDepartedTown;
    }

    private bool TryStartWorkerPersonalCarTrip(
        DriverAgent driver,
        Vector3 startPosition,
        Vector3 finalTargetWorld,
        DriverRescuePhase finalPhase,
        string reason)
    {
        if (!CanWorkerUsePersonalCar(driver) || driver.DriverObject == null)
        {
            return false;
        }

        if (!TryResolvePersonalCarStartDriveCell(driver, out Vector2Int startDriveCell) ||
            !TryResolvePersonalCarDestinationDriveCell(driver, finalTargetWorld, finalPhase, out Vector2Int destinationDriveCell))
        {
            SessionDebugLogger.Log("PERSONAL_CAR", $"{driver.DriverName} could not start car trip for {reason}: no driveable start/destination cell.");
            return false;
        }

        List<Vector2Int> cellPath = FindPath(startDriveCell, destinationDriveCell);
        if (cellPath == null || cellPath.Count == 0)
        {
            SessionDebugLogger.Log(
                "PERSONAL_CAR",
                $"{driver.DriverName} could not start car trip for {reason}: no road path from ({startDriveCell.x},{startDriveCell.y}) to ({destinationDriveCell.x},{destinationDriveCell.y}).");
            return false;
        }

        EnsureWorkerPersonalCarObject(driver, startPosition);
        if (driver.OwnedCarObject == null)
        {
            return false;
        }

        driver.PersonalCarPath.Clear();
        Vector3 roadStart = GetBusRoadWorldPosition(startDriveCell);
        if ((driver.OwnedCarObject.transform.position - roadStart).sqrMagnitude > 0.04f)
        {
            driver.PersonalCarPath.Add(roadStart);
        }

        for (int i = 1; i < cellPath.Count; i++)
        {
            driver.PersonalCarPath.Add(GetBusRoadWorldPosition(cellPath[i - 1], cellPath[i]));
        }

        if (driver.PersonalCarPath.Count == 0)
        {
            driver.PersonalCarPath.Add(GetBusRoadWorldPosition(destinationDriveCell));
        }

        Vector3 parkedWorld = GetPersonalCarParkingWorld(driver, finalTargetWorld, finalPhase, destinationDriveCell);
        driver.PersonalCarPath.Add(parkedWorld);
        driver.PersonalCarWaypointIndex = 0;
        driver.PersonalCarFinalPhase = finalPhase;
        driver.PersonalCarFinalTargetWorld = finalTargetWorld;
        driver.PersonalCarTravelReason = reason ?? string.Empty;
        driver.IsDrivingPersonalCar = true;
        driver.CompletedPersonalCarTrip = false;
        driver.WalkPhase = finalPhase;
        driver.WalkTargetWorld = finalTargetWorld;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        driver.DriverObject.SetActive(false);

        SessionDebugLogger.Log(
            "PERSONAL_CAR",
            $"{driver.DriverName} started personal car trip for {reason}: from ({startDriveCell.x},{startDriveCell.y}) to ({destinationDriveCell.x},{destinationDriveCell.y}), waypoints={driver.PersonalCarPath.Count}.");
        return true;
    }

    private void UpdateDriverPersonalCarTrip(DriverAgent driver)
    {
        if (driver == null || !driver.IsDrivingPersonalCar)
        {
            return;
        }

        if (driver.OwnedCarObject == null || driver.PersonalCarPath.Count == 0)
        {
            CompleteWorkerPersonalCarTrip(driver, driver.PersonalCarFinalTargetWorld);
            return;
        }

        int waypointIndex = Mathf.Clamp(driver.PersonalCarWaypointIndex, 0, driver.PersonalCarPath.Count - 1);
        Vector3 current = driver.OwnedCarObject.transform.position;
        Vector3 target = driver.PersonalCarPath[waypointIndex];
        Vector3 flatDirection = target - current;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            float stepDistance = PersonalCarSpeed * Time.deltaTime * Mathf.Max(0.1f, gameSpeedMultiplier);
            Vector3 step = flatDirection.normalized * stepDistance;
            current = step.sqrMagnitude >= flatDirection.sqrMagnitude
                ? target
                : current + step;
            current = WithRoadVehicleHeight(current, LocalBusRoadSurfaceLift);
            driver.OwnedCarObject.transform.position = current;
            driver.OwnedCarObject.transform.rotation = Quaternion.Slerp(
                driver.OwnedCarObject.transform.rotation,
                GetPersonalCarFacingRotation(flatDirection),
                10f * Time.deltaTime);
        }

        Vector3 flatDelta = driver.OwnedCarObject.transform.position - target;
        flatDelta.y = 0f;
        if (flatDelta.sqrMagnitude > 0.0025f)
        {
            return;
        }

        if (driver.PersonalCarWaypointIndex < driver.PersonalCarPath.Count - 1)
        {
            driver.PersonalCarWaypointIndex++;
            return;
        }

        CompleteWorkerPersonalCarTrip(driver, driver.PersonalCarFinalTargetWorld);
    }

    private void CompleteWorkerPersonalCarTrip(DriverAgent driver, Vector3 workerTarget)
    {
        Vector3 parkedWorld = driver.PersonalCarPath.Count > 0
            ? driver.PersonalCarPath[driver.PersonalCarPath.Count - 1]
            : driver.OwnedCarObject != null ? driver.OwnedCarObject.transform.position : workerTarget;

        if (driver.OwnedCarObject != null)
        {
            driver.OwnedCarObject.transform.position = parkedWorld;
        }

        driver.HasOwnedCarParking = true;
        driver.OwnedCarParkedWorld = parkedWorld;
        driver.OwnedCarParkedDriveCell = WorldToCell(parkedWorld);
        TryResolvePersonalCarDestinationDriveCell(driver, workerTarget, driver.PersonalCarFinalPhase, out driver.OwnedCarParkedDriveCell);
        driver.IsDrivingPersonalCar = false;
        driver.CompletedPersonalCarTrip = true;
        driver.PersonalCarPath.Clear();
        driver.PersonalCarWaypointIndex = 0;
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = workerTarget;
        driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        SessionDebugLogger.Log(
            "PERSONAL_CAR",
            $"{driver.DriverName} parked personal car after {driver.PersonalCarTravelReason}; finalPhase={driver.PersonalCarFinalPhase}, parkedCell=({driver.OwnedCarParkedDriveCell.x},{driver.OwnedCarParkedDriveCell.y}).");
    }

    private void EnsureWorkerPersonalCarObject(DriverAgent driver, Vector3 fallbackPosition)
    {
        if (driver == null || driver.OwnedCarModelIndex < 0)
        {
            return;
        }

        if (driver.OwnedCarObject != null)
        {
            return;
        }

        Vector3 spawnPosition = driver.HasOwnedCarParking
            ? driver.OwnedCarParkedWorld
            : GetPersonalHouseCarParkingWorld(driver);
        if (!driver.HasOwnedCarParking && spawnPosition == Vector3.zero)
        {
            spawnPosition = fallbackPosition;
        }

        GameObject car = CreateCarModel(driver.OwnedCarModelIndex, worldRoot);
        car.transform.position = spawnPosition;
        car.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        car.transform.localScale = Vector3.one;
        driver.OwnedCarObject = car;
        driver.OwnedCarParkedWorld = spawnPosition;
        driver.OwnedCarParkedDriveCell = WorldToCell(spawnPosition);
        driver.HasOwnedCarParking = true;
    }

    private void SpawnWorkerCarAtCurrentParking(DriverAgent driver, Vector3 parkedWorld, Vector2Int driveCell)
    {
        if (driver == null || driver.OwnedCarModelIndex < 0)
        {
            return;
        }

        if (driver.OwnedCarObject != null)
        {
            Object.Destroy(driver.OwnedCarObject);
            driver.OwnedCarObject = null;
        }

        GameObject car = CreateCarModel(driver.OwnedCarModelIndex, worldRoot);
        car.transform.position = parkedWorld;
        car.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        car.transform.localScale = Vector3.one;
        driver.OwnedCarObject = car;
        driver.HasOwnedCarParking = true;
        driver.OwnedCarParkedWorld = parkedWorld;
        driver.OwnedCarParkedDriveCell = driveCell;
    }

    private void ParkNewlyPurchasedWorkerCarAtMarket(DriverAgent driver)
    {
        if (driver == null || driver.OwnedCarModelIndex < 0)
        {
            return;
        }

        Vector2Int driveCell = ResolvePersonalCarLocationDriveCell(LocationType.CarMarket);
        Vector3 parkedWorld = locations.TryGetValue(LocationType.CarMarket, out LocationData market)
            ? GetLocationCarParkingWorld(market, driver.DriverId, driveCell)
            : GetBusRoadWorldPosition(driveCell);
        SpawnWorkerCarAtCurrentParking(driver, parkedWorld, driveCell);
    }

    private bool TryResolvePersonalCarStartDriveCell(DriverAgent driver, out Vector2Int driveCell)
    {
        if (driver.HasOwnedCarParking)
        {
            driveCell = driver.OwnedCarParkedDriveCell;
            if (IsDriveable(driveCell))
            {
                return true;
            }
        }

        if (driver.OwnedCarObject != null)
        {
            Vector2Int objectCell = WorldToCell(driver.OwnedCarObject.transform.position);
            if (TryFindNearestDriveableCell(objectCell, 5, out driveCell))
            {
                return true;
            }
        }

        if (driver.AssignedPersonalHouseIndex >= 0 && driver.AssignedPersonalHouseIndex < personalHouses.Count)
        {
            driveCell = GetLocationDriveCell(personalHouses[driver.AssignedPersonalHouseIndex]);
            return IsDriveable(driveCell);
        }

        driveCell = Vector2Int.zero;
        return false;
    }

    private bool TryResolvePersonalCarDestinationDriveCell(
        DriverAgent driver,
        Vector3 targetWorld,
        DriverRescuePhase finalPhase,
        out Vector2Int driveCell)
    {
        if ((finalPhase == DriverRescuePhase.ToPersonalHouseEntrance ||
             finalPhase == DriverRescuePhase.ToPersonalHouseMeal ||
             finalPhase == DriverRescuePhase.ToPersonalHouseParking) &&
            driver.AssignedPersonalHouseIndex >= 0 &&
            driver.AssignedPersonalHouseIndex < personalHouses.Count)
        {
            driveCell = GetLocationDriveCell(personalHouses[driver.AssignedPersonalHouseIndex]);
            return IsDriveable(driveCell);
        }

        Vector2Int targetCell = WorldToCell(targetWorld);
        foreach (LocationData location in locations.Values)
        {
            if (location.Contains(targetCell) || location.Anchor == targetCell || location.RoadAccess == targetCell)
            {
                driveCell = GetLocationDriveCell(location);
                return IsDriveable(driveCell);
            }
        }

        if (TryFindNearestDriveableCell(targetCell, 5, out driveCell))
        {
            return true;
        }

        driveCell = Vector2Int.zero;
        return false;
    }

    private Vector2Int ResolvePersonalCarLocationDriveCell(LocationType type)
    {
        if (locations.TryGetValue(type, out LocationData location))
        {
            return GetLocationDriveCell(location);
        }

        return locations.TryGetValue(LocationType.Parking, out LocationData parking)
            ? GetLocationDriveCell(parking)
            : Vector2Int.zero;
    }

    private Vector2Int GetLocationDriveCell(LocationData location)
    {
        if (location != null && IsDriveable(location.RoadAccess))
        {
            return location.RoadAccess;
        }

        if (location != null && IsDriveable(location.Anchor))
        {
            return location.Anchor;
        }

        return location != null ? location.Anchor : Vector2Int.zero;
    }

    private bool TryFindNearestDriveableCell(Vector2Int origin, int maxRadius, out Vector2Int driveCell)
    {
        if (IsDriveable(origin))
        {
            driveCell = origin;
            return true;
        }

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) != radius)
                    {
                        continue;
                    }

                    Vector2Int candidate = origin + new Vector2Int(dx, dy);
                    if (IsDriveable(candidate))
                    {
                        driveCell = candidate;
                        return true;
                    }
                }
            }
        }

        driveCell = Vector2Int.zero;
        return false;
    }

    private Vector3 GetPersonalCarParkingWorld(
        DriverAgent driver,
        Vector3 targetWorld,
        DriverRescuePhase finalPhase,
        Vector2Int destinationDriveCell)
    {
        if (finalPhase == DriverRescuePhase.ToPersonalHouseEntrance ||
            finalPhase == DriverRescuePhase.ToPersonalHouseMeal ||
            finalPhase == DriverRescuePhase.ToPersonalHouseParking)
        {
            Vector3 homeParking = GetPersonalHouseCarParkingWorld(driver);
            if (homeParking != Vector3.zero)
            {
                return homeParking;
            }
        }

        Vector2Int targetCell = WorldToCell(targetWorld);
        foreach (LocationData location in locations.Values)
        {
            if (location.Contains(targetCell) || location.Anchor == targetCell || location.RoadAccess == targetCell)
            {
                return GetLocationCarParkingWorld(location, driver.DriverId, destinationDriveCell);
            }
        }

        return GetBusRoadWorldPosition(destinationDriveCell);
    }

    private Vector3 GetPersonalHouseCarParkingWorld(DriverAgent driver)
    {
        if (driver == null ||
            driver.AssignedPersonalHouseIndex < 0 ||
            driver.AssignedPersonalHouseIndex >= personalHouses.Count)
        {
            return Vector3.zero;
        }

        LocationData house = personalHouses[driver.AssignedPersonalHouseIndex];
        Vector3 center = GetLocationCenter(house);
        Vector3 anchor = GetCellCenter(house.Anchor);
        Vector3 front = anchor - center;
        front.y = 0f;
        if (front.sqrMagnitude < 0.0001f)
        {
            front = Vector3.forward;
        }
        front.Normalize();
        Vector3 side = new(front.z, 0f, -front.x);
        float residentOffset = (driver.DriverId % 2 == 0) ? -0.72f : 0.72f;
        Vector3 parked = center + front * 1.35f + side * residentOffset;
        return WithRoadVehicleHeight(parked, LocalBusRoadSurfaceLift);
    }

    private Vector3 GetLocationCarParkingWorld(LocationData location, int driverId, Vector2Int driveCell)
    {
        if (location == null)
        {
            return GetBusRoadWorldPosition(driveCell);
        }

        Vector3 access = GetBusRoadWorldPosition(driveCell);
        Vector3 center = GetLocationCenter(location);
        Vector3 awayFromBuilding = access - center;
        awayFromBuilding.y = 0f;
        if (awayFromBuilding.sqrMagnitude < 0.0001f)
        {
            awayFromBuilding = Vector3.forward;
        }
        awayFromBuilding.Normalize();
        Vector3 side = new(awayFromBuilding.z, 0f, -awayFromBuilding.x);
        float sideOffset = ((driverId % 3) - 1) * 0.42f;
        Vector3 parked = access + awayFromBuilding * 0.35f + side * sideOffset;
        return WithRoadVehicleHeight(parked, LocalBusRoadSurfaceLift);
    }

    private static Quaternion GetPersonalCarFacingRotation(Vector3 flatDirection)
    {
        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.FromToRotation(Vector3.right, flatDirection.normalized);
    }
}
