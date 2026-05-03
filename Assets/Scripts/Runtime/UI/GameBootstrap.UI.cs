using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void ClampCameraFocus()
    {
        cameraFocusPoint.x = Mathf.Clamp(cameraFocusPoint.x, -2f, GridWidth + 2f);
        cameraFocusPoint.y = 0f;
        cameraFocusPoint.z = Mathf.Clamp(cameraFocusPoint.z, -2f, GridHeight + 2f);
    }

    private void BeginNextTruckSegment(Vector2Int nextCell)
    {
        truckSegmentStartWorld = truckObject.transform.position;
        Vector3 baseTargetWorld = GetTruckWorldPosition(nextCell);
        baseTargetWorld.y = SampleRoadSurfaceHeight(baseTargetWorld.x, baseTargetWorld.z) + TruckSegmentStartLift;
        Vector3 laneOffset = GetTruckTargetLaneOffset(truckCell, nextCell, out string laneReason);
        truckTargetWorld = baseTargetWorld + laneOffset;
        LogTruckLaneSegment(truckCell, nextCell, laneOffset, laneReason, baseTargetWorld, truckTargetWorld);
        truckSegmentProgress = 0f;
        float distance = Vector3.Distance(truckSegmentStartWorld, truckTargetWorld);
        truckSegmentDuration = Mathf.Max(0.38f, distance / TruckCruiseSpeed);
    }

    private Vector3 GetRoadLaneOffset(Vector2Int fromCell, Vector2Int toCell)
    {
        return GetRoadLaneOffset(fromCell, toCell, out _);
    }

    private Vector3 GetRoadLaneOffset(Vector2Int fromCell, Vector2Int toCell, out string reason)
    {
        if (IsAnchorCell(fromCell))
        {
            reason = "from-anchor";
            return Vector3.zero;
        }

        if (IsAnchorCell(toCell))
        {
            reason = "to-anchor";
            return Vector3.zero;
        }

        if (!roadCells.Contains(toCell))
        {
            reason = "target-not-road";
            return Vector3.zero;
        }

        if (IsRoadDeadEnd(toCell))
        {
            reason = "road-dead-end";
            return Vector3.zero;
        }

        Vector2Int dir = NormalizeRoadDirection(toCell - fromCell);
        Vector2Int physicalRightLaneOffset = TwoLaneRoadGeometry.GetRightLaneOffset(dir);
        Vector3 offset = GetRightHandLaneOffset(dir);
        if (IsContinuousPhysicalRightLane(fromCell, toCell, dir, physicalRightLaneOffset))
        {
            offset += new Vector3(physicalRightLaneOffset.x, 0f, physicalRightLaneOffset.y);
            reason = "physical-right-lane";
            return offset;
        }

        reason = offset.sqrMagnitude > 0.0001f ? "right-lane-in-cell" : "zero-direction";
        return offset;
    }

    private Vector3 GetTruckTargetLaneOffset(Vector2Int fromCell, Vector2Int toCell, out string reason)
    {
        if (!IsAnchorCell(fromCell) &&
            !IsAnchorCell(toCell) &&
            !IsNearAnchorCell(fromCell) &&
            !IsNearAnchorCell(toCell) &&
            activePath != null &&
            activePath.Count > 1 &&
            activePath[0] == toCell)
        {
            Vector2Int incomingRawDir = toCell - fromCell;
            Vector2Int outgoingRawDir = activePath[1] - toCell;
            Vector2Int incomingDir = NormalizeRoadDirection(incomingRawDir);
            Vector2Int outgoingDir = NormalizeRoadDirection(outgoingRawDir);
            if (incomingRawDir != Vector2Int.zero && outgoingRawDir != Vector2Int.zero && incomingDir != outgoingDir)
            {
                Vector3 outgoingOffset = GetRoadLaneOffset(toCell, activePath[1], out string outgoingReason);
                if (outgoingOffset.sqrMagnitude > 0.0001f)
                {
                    reason = $"corner-outgoing-{outgoingReason}; next=({activePath[1].x},{activePath[1].y})";
                    return outgoingOffset;
                }
            }
        }

        return GetRoadLaneOffset(fromCell, toCell, out reason);
    }

    private bool IsContinuousPhysicalRightLane(Vector2Int fromCell, Vector2Int toCell, Vector2Int direction, Vector2Int rightLaneOffset)
    {
        if (rightLaneOffset == Vector2Int.zero)
        {
            return false;
        }

        Vector2Int sideCell = toCell + rightLaneOffset;
        if (!roadCells.Contains(sideCell))
        {
            return false;
        }

        Vector2Int sideFrom = fromCell + rightLaneOffset;
        Vector2Int sideForward = sideCell + direction;
        return roadCells.Contains(sideFrom) || roadCells.Contains(sideForward);
    }

    private bool IsNearAnchorCell(Vector2Int cell)
    {
        foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(cell))
        {
            if (IsAnchorCell(neighbor))
            {
                return true;
            }
        }

        return false;
    }

    private void LogTruckLaneSegment(Vector2Int fromCell, Vector2Int toCell, Vector3 laneOffset, string laneReason, Vector3 baseTargetWorld, Vector3 targetWorld)
    {
        if (!SessionDebugLogger.IsVerboseEnabled("TRUCK_LANE"))
        {
            return;
        }

        Vector2Int rawDir = toCell - fromCell;
        Vector2Int dir = NormalizeRoadDirection(rawDir);
        SessionDebugLogger.LogVerbose(
            "TRUCK_LANE",
            $"{GetLoadedTruckDisplayName()} from=({fromCell.x},{fromCell.y}) to=({toCell.x},{toCell.y}) rawDir=({rawDir.x},{rawDir.y}) dir=({dir.x},{dir.y}) reason={laneReason} offset=({laneOffset.x:F2},{laneOffset.z:F2}) base=({baseTargetWorld.x:F2},{baseTargetWorld.z:F2}) target=({targetWorld.x:F2},{targetWorld.z:F2}) roadTarget={roadCells.Contains(toCell)} fromAnchor={IsAnchorCell(fromCell)} toAnchor={IsAnchorCell(toCell)}.");
    }

    private Vector3 GetRightHandLaneOffset(Vector2Int direction)
    {
        Vector2Int dir = NormalizeRoadDirection(direction);
        // Match two-lane road geometry: travel +X uses the south lane, travel +Y uses the east lane.
        return new Vector3(dir.y, 0f, -dir.x) * RoadLaneOffset;
    }

    private bool IsRoadDeadEnd(Vector2Int cell)
    {
        int n = 0;
        foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(cell))
            if (roadCells.Contains(neighbor) || IsAnchorCell(neighbor)) n++;
        return n <= 1;
    }

    private void CompleteTruckSegment()
    {
        truckObject.transform.position = truckTargetWorld;
        truckCell = activePath[0];
        truckFuel = Mathf.Max(0f, truckFuel - TruckFuelPerCell);
        activePath.RemoveAt(0);

        if (truckFuel <= 0f)
        {
            isTruckMoving = false;
            truckSegmentDuration = 0f;
            truckSegmentProgress = 0f;
            UpdateTruckVisuals(0f, false);

            if (!isDriverRescueActive)
            {
                StartDriverRescue();
            }

            return;
        }

        if (activePath.Count == 0)
        {
            isTruckMoving = false;
            truckSegmentDuration = 0f;
            truckSegmentProgress = 0f;
            UpdateTruckVisuals(0f, false);
            return;
        }

        BeginNextTruckSegment(activePath[0]);
    }

    private void StartDriverRescue()
    {
        // Find the driver for the currently loaded truck
        DriverAgent driver = null;
        foreach (TruckAgent ta in truckAgents)
        {
            if (ta.TruckObject == truckObject) { driver = ta.Driver; break; }
        }
        if (driver == null || driver.DriverObject == null) return;

        // If driver is already on a rest journey, silently refuel and continue.
        if (locations.TryGetValue(LocationType.GasStation, out LocationData gsCheck))
        {
            gsCheck.FuelStored = GasStationMaxFuelStorage;
        }

        if (driver.RestPhase != DriverRestPhase.None)
        {
            truckFuel = TruckFuelCapacity;
            SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} refueled silently (rest journey) at ({truckCell.x},{truckCell.y}).");
            if (activePath.Count > 0)
            {
                isTruckMoving = true;
                BeginNextTruckSegment(activePath[0]);
            }
            return;
        }

        isDriverRescueActive = true;
        driver.WalkPhase = DriverRescuePhase.ToGasStation;
        driver.DriverObject.SetActive(true);
        if (driver.DriverFuelCanTransform != null)
        {
            driver.DriverFuelCanTransform.gameObject.SetActive(false);
        }

        driver.DriverObject.transform.position = GetDriverStandPointNearTruck();
        driver.DriverObject.transform.rotation = truckObject.transform.rotation;
        driver.WalkAnimationTime = 0f;
        ApplyDriverPose(driver, 0f, 0f);
        driver.WalkTargetWorld = GetDriverStandPointNearLocation(LocationType.GasStation);
        BuildDriverWalkPath(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld);
        SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} ran out of fuel at cell ({truckCell.x},{truckCell.y}); driver started rescue walk.");
        PlayUiSound(uiSelectClip, 0.9f);
    }

    private void BuildDriverWalkPath(DriverAgent driver, Vector3 startWorld, Vector3 targetWorld)
    {
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;

        Vector2Int startCell = WorldToCell(startWorld);
        Vector2Int goalCell = WorldToCell(targetWorld);
        List<Vector2Int> cellPath = FindDriverWalkPath(startCell, goalCell, driver.WalkPhase);
        if (cellPath == null || cellPath.Count == 0)
        {
            if (startCell == goalCell)
            {
                driver.WalkPath.Add(targetWorld);
                driver.WalkTargetWorld = targetWorld;
                SessionDebugLogger.Log(
                    "DRIVER",
                    $"{driver.DriverName} built a same-cell walk target for {driver.WalkPhase} at ({goalCell.x},{goalCell.y}).");
                return;
            }

            bool startInLocation = IsLocationCell(startCell);
            bool goalInLocation = IsLocationCell(goalCell);
            bool isIdleWander = driver.WalkPhase == DriverRescuePhase.IdleWander;
            if (!isIdleWander && !startInLocation && !goalInLocation && !DoesWalkSegmentCrossWater(startWorld, targetWorld))
            {
                driver.WalkPath.Add(targetWorld);
                driver.WalkTargetWorld = targetWorld;
                SessionDebugLogger.Log(
                    "DRIVER",
                    $"{driver.DriverName} could not build a grid walk path from ({startCell.x},{startCell.y}) to ({goalCell.x},{goalCell.y}); using direct open-ground target.");
                return;
            }

            driver.WalkTargetWorld = startWorld;
            SessionDebugLogger.Log(
                "DRIVER",
                $"{driver.DriverName} could not build a safe walk path from ({startCell.x},{startCell.y}) to ({goalCell.x},{goalCell.y}); blocking direct fallback through buildings.");
            return;
        }

        for (int i = 1; i < cellPath.Count; i++)
        {
            driver.WalkPath.Add(GetCellCenter(cellPath[i]));
        }

        driver.WalkPath.Add(targetWorld);
        driver.WalkTargetWorld = targetWorld;
        SessionDebugLogger.Log(
            "DRIVER",
            $"{driver.DriverName} built walk path for {driver.WalkPhase}: {cellPath.Count - 1} cell steps, {driver.WalkPath.Count} world waypoints, from ({startCell.x},{startCell.y}) to ({goalCell.x},{goalCell.y}).");
    }

    private List<Vector2Int> FindDriverWalkPath(Vector2Int start, Vector2Int goal, DriverRescuePhase walkPhase)
    {
        return GridPathService.FindPath(
                   start,
                   goal,
                   GridPathService.GetCardinalNeighbors,
                   neighbor => IsWalkableDriverCell(neighbor, start, goal, walkPhase));
    }

    private bool IsWalkableDriverCell(Vector2Int cell, Vector2Int start, Vector2Int goal, DriverRescuePhase walkPhase)
    {
        if (!IsInsideGrid(cell))
        {
            return false;
        }

        if (waterCells.Contains(cell))
        {
            return cell == start;
        }

        if (cell == start || cell == goal || IsAnchorCell(cell))
        {
            return true;
        }

        if (IsLocationCell(cell))
        {
            return false;
        }

        if (edgeHighwayCells.Contains(cell))
        {
            return false;
        }

        return true;
    }

    private bool DoesWalkSegmentCrossWater(Vector3 startWorld, Vector3 targetWorld)
    {
        Vector3 delta = targetWorld - startWorld;
        int steps = Mathf.Max(2, Mathf.CeilToInt(delta.magnitude / 0.25f));
        for (int i = 1; i <= steps; i++)
        {
            Vector3 sample = Vector3.Lerp(startWorld, targetWorld, i / (float)steps);
            if (waterCells.Contains(WorldToCell(sample)))
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 GetDriverStandPointNearTruck()
    {
        Vector3 truckPosition = truckObject.transform.position;
        Vector3 standPoint = new Vector3(truckPosition.x + 0.32f, 0f, truckPosition.z - 0.32f);
        standPoint.y = SampleTerrainHeight(standPoint.x, standPoint.z);
        return standPoint;
    }

    private Vector3 GetDriverStandPointNearLocation(LocationType locationType)
    {
        if (!locations.TryGetValue(locationType, out LocationData location))
        {
            return Vector3.zero;
        }

        Vector3 anchorPoint = GetCellCenter(location.Anchor);
        Vector3 locationCenter = GetLocationCenter(locationType);
        Vector3 outwardDirection = (anchorPoint - locationCenter);
        outwardDirection.y = 0f;
        if (outwardDirection.sqrMagnitude < 0.0001f)
        {
            outwardDirection = Vector3.forward;
        }

        outwardDirection.Normalize();
        Vector3 standPoint = anchorPoint - outwardDirection * 0.12f;
        standPoint.y = SampleTerrainHeight(standPoint.x, standPoint.z);
        return standPoint;
    }

    private Vector3 GetDriverStandPointNearPersonalHouse(int houseIndex)
    {
        if (houseIndex < 0 || houseIndex >= personalHouses.Count) return Vector3.zero;
        LocationData house = personalHouses[houseIndex];
        Vector3 anchorPoint = GetCellCenter(house.Anchor);
        Vector3 center = GetLocationCenter(house);
        Vector3 outward = anchorPoint - center;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.0001f) outward = Vector3.forward;
        outward.Normalize();
        Vector3 standPoint = anchorPoint - outward * 0.12f;
        standPoint.y = SampleTerrainHeight(standPoint.x, standPoint.z);
        return standPoint;
    }

    private void UpdateTruckVisuals(float speed, bool moving)
    {
        if (truckVisualRoot == null)
        {
            return;
        }

        float normalizedSpeed = Mathf.Clamp01(speed / TruckCruiseSpeed);
        float idleServiceBob = isTruckInteracting ? Mathf.Sin(Time.time * 8f) * 0.02f : 0f;
        float bob = moving ? Mathf.Sin(Time.time * 10f) * TruckSuspensionBobAmount * normalizedSpeed : idleServiceBob;
        float pitch = moving ? -Mathf.Sin(Mathf.Clamp01(truckSegmentProgress) * Mathf.PI) * 2.2f * normalizedSpeed : 0f;

        truckVisualRoot.localPosition = new Vector3(0f, bob, 0f);

        float targetSteer = 0f;
        if (moving && activePath.Count > 1)
        {
            Vector3 currentDir = (GetTruckWorldPosition(activePath[0]) - truckObject.transform.position).normalized;
            Vector3 nextDir = (GetTruckWorldPosition(activePath[1]) - GetTruckWorldPosition(activePath[0])).normalized;
            targetSteer = Mathf.Clamp(Vector3.SignedAngle(currentDir, nextDir, Vector3.up), -18f, 18f);
        }

        truckSteerAngle = Mathf.Lerp(truckSteerAngle, targetSteer, 8f * Time.deltaTime);
        float roll = moving ? -truckSteerAngle * 0.18f * normalizedSpeed : 0f;

        truckBodyTransform.localRotation = Quaternion.Euler(pitch, 0f, roll);
        truckCabinTransform.localRotation = Quaternion.Euler(pitch * 0.6f, 0f, roll * 0.55f);
        truckWheelSpinAngle += moving ? speed / Mathf.Max(TruckWheelRadius, 0.01f) * Mathf.Rad2Deg * Time.deltaTime : 0f;

        foreach (Transform wheel in truckWheels)
        {
            bool isFrontWheel = truckFrontWheels.Contains(wheel);
            float steer = isFrontWheel ? truckSteerAngle : 0f;
            wheel.localRotation = Quaternion.Euler(90f + truckWheelSpinAngle, steer, 0f);
        }
    }

    private void UpdateCargoTransferVisual(float progress)
    {
        if (cargoTransferCrate == null || !cargoTransferCrate.activeSelf)
        {
            return;
        }

        Vector3 truckRearPoint = truckObject.transform.position - truckObject.transform.forward * 0.52f + Vector3.up * 0.18f;
        bool loadingIntoTruck =
            activeTruckInteraction == TruckInteractionType.LoadAtForest ||
            activeTruckInteraction == TruckInteractionType.LoadAtSawmill ||
            activeTruckInteraction == TruckInteractionType.LoadBoardsAtWarehouse ||
            activeTruckInteraction == TruckInteractionType.LoadTextileAtWarehouse ||
            activeTruckInteraction == TruckInteractionType.LoadAtFurnitureFactory;
        Vector3 from = loadingIntoTruck ? truckInteractionBuildingPoint : truckRearPoint;
        Vector3 to = loadingIntoTruck ? truckRearPoint : truckInteractionBuildingPoint;

        float arc = Mathf.Sin(progress * Mathf.PI) * 0.45f;
        cargoTransferCrate.transform.position = Vector3.Lerp(from, to, progress) + Vector3.up * arc;
        cargoTransferCrate.transform.rotation = Quaternion.Euler(0f, Time.time * 140f, 0f);
    }

    private Vector3 GetLocationCenter(LocationType locationType)
    {
        LocationData location = locations[locationType];
        return new Vector3((location.Min.x + location.Max.x + 1) * 0.5f, GetLocationBaseHeight(locationType), (location.Min.y + location.Max.y + 1) * 0.5f);
    }

    private Vector3 GetLocationCenter(LocationData location)
    {
        return new Vector3((location.Min.x + location.Max.x + 1) * 0.5f, GetLocationBaseHeight(location), (location.Min.y + location.Max.y + 1) * 0.5f);
    }

    private string GetTruckStatusLabel()
    {
        if (isTruckInteracting)
        {
            return activeTruckInteraction switch
            {
                TruckInteractionType.LoadAtForest => "Loading at Forest...",
                TruckInteractionType.UnloadAtSawmill => "Unloading at Sawmill...",
                TruckInteractionType.LoadAtSawmill => "Loading at Sawmill...",
                TruckInteractionType.UnloadAtWarehouse => "Unloading cargo at Warehouse...",
                TruckInteractionType.LoadBoardsAtWarehouse => "Loading boards at Warehouse...",
                TruckInteractionType.LoadTextileAtWarehouse => "Loading textile at Warehouse...",
                TruckInteractionType.UnloadBoardsAtFurnitureFactory => "Unloading boards at Furniture Factory...",
                TruckInteractionType.UnloadTextileAtFurnitureFactory => "Unloading textile at Furniture Factory...",
                TruckInteractionType.LoadAtFurnitureFactory => "Loading furniture at Furniture Factory...",
                TruckInteractionType.UnloadFurnitureAtWarehouse => "Unloading furniture at Warehouse...",
                TruckInteractionType.TradeUnloadAtWarehouse => "Unloading trade goods at Warehouse...",
                TruckInteractionType.RefuelAtGasStation => "Refueling at Gas Station...",
                _ => "Truck servicing cargo..."
            };
        }

        if (isTruckWaitingForService && queuedServiceLocation.HasValue)
        {
            return $"Waiting for service slot at {locations[queuedServiceLocation.Value].Label}.";
        }

        if (isTruckMoving)
        {
            return "Truck is moving.";
        }

        if (currentAssignedTrip != TripType.None)
        {
            return $"Assigned route: {GetTripTitle(currentAssignedTrip)}";
        }

        if (isDriverRescueActive)
        {
            DriverAgent activeDriver = null;
            foreach (TruckAgent ta in truckAgents)
            {
                if (ta.TruckObject == truckObject) { activeDriver = ta.Driver; break; }
            }
            bool goingToStation = activeDriver == null || activeDriver.WalkPhase == DriverRescuePhase.ToGasStation;
            return goingToStation
                ? "Out of fuel. Driver is walking to Gas Station."
                : "Driver is bringing fuel back to the truck.";
        }

        if (currentRefuelPhase != RefuelPhase.None)
        {
            return "Refuel order in progress.";
        }

        if (isTruckAutoModeEnabled)
        {
            return "Auto mode is waiting for the next task.";
        }

        return "Truck is awaiting manual orders.";
    }


}
