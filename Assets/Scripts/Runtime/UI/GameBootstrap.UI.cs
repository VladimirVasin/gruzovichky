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
        Vector2Int rawDir = toCell - fromCell;
        Vector2Int dir = NormalizeRoadDirection(rawDir);
        SessionDebugLogger.Log(
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
            if (!isIdleWander && !startInLocation && !goalInLocation)
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

    private void DrawParkingHud()
    {
        Rect panelRect = GetParkingHudRect();
        GUI.Box(panelRect, "Parking HUD");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 30, 250, 22), "Selected building: Parking");
        int trucksInsideCount = GetParkingTruckCount();
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 54, 250, 22), $"Trucks inside parking: {trucksInsideCount}/{MaxTruckCount}");

        float y = panelRect.y + 82f;
        TruckAgent firstTruck = GetTruckAgent(1);
        if (IsTruckInsideParking(firstTruck))
        {
            GUI.Label(new Rect(panelRect.x + 12, y, 250, 22), "Fleet in parking:");

            Rect iconRect = new Rect(panelRect.x + 16, y + 28f, 76, 60);
            bool iconPressed = GUI.Button(iconRect, "TRUCK");

            if (iconPressed)
            {
                isTruckDetailsOpen = !isTruckDetailsOpen;
                PlayUiSound(isTruckDetailsOpen ? uiPanelOpenClip : uiPanelCloseClip, 0.9f);
            }

            GUI.Label(new Rect(panelRect.x + 106, y + 32f, 150, 22), GetTruckDisplayName(1));
            GUI.Label(new Rect(panelRect.x + 106, y + 54f, 150, 22), "Status: Parked");
            GUI.Label(new Rect(panelRect.x + 106, y + 76f, 150, 22), "Operational truck");
            y += 96f;
        }

        foreach (TruckAgent truckAgent in truckAgents)
        {
            if (truckAgent.TruckNumber == 1)
            {
                continue;
            }

            if (!IsTruckInsideParking(truckAgent))
            {
                continue;
            }

            GUI.Box(new Rect(panelRect.x + 12, y, panelRect.width - 24, 32), string.Empty);
            if (GUI.Button(new Rect(panelRect.x + 16, y + 4f, 82, 24), truckAgent.DisplayName))
            {
                FocusTruck(truckAgent.TruckNumber);
            }

            GUI.Label(new Rect(panelRect.x + 106, y + 6f, 90, 20), "Parked");
            y += 36f;
        }

        if (GetParkingTruckCount() == 0)
        {
            GUI.Box(new Rect(panelRect.x + 12, y, 252, 58), "No trucks inside");
            GUI.Label(new Rect(panelRect.x + 24, y + 24f, 220, 22), "The fleet is currently out on routes.");
            y += 66f;
            isTruckDetailsOpen = false;
        }

        y += 8f;
        bool canHireTruck = locations.ContainsKey(LocationType.Parking) && GetOwnedTruckCount() < MaxTruckCount && money >= HireTruckCost;
        GUI.enabled = canHireTruck;
        if (GUI.Button(new Rect(panelRect.x + 12, y, panelRect.width - 24, 28), $"Hire New Truck  ${HireTruckCost}"))
        {
            HireNewTruck();
        }

        GUI.enabled = true;
        if (!locations.ContainsKey(LocationType.Parking))
        {
            GUI.Label(new Rect(panelRect.x + 12, y + 32f, 240, 20), "Build Parking first.");
        }
        else if (GetOwnedTruckCount() >= MaxTruckCount)
        {
            GUI.Label(new Rect(panelRect.x + 12, y + 32f, 240, 20), "Parking is full.");
        }
        else if (money < HireTruckCost)
        {
            GUI.Label(new Rect(panelRect.x + 12, y + 32f, 240, 20), $"Need ${HireTruckCost} to hire a truck.");
        }
    }

    private void HireNewTruck()
    {
        LogUiInput("Fleet/Parking: clicked Buy New Truck");
        LogCommand($"HireNewTruck(cost=${HireTruckCost})");
        if (!locations.ContainsKey(LocationType.Parking) || GetOwnedTruckCount() >= MaxTruckCount || money < HireTruckCost)
        {
            SessionDebugLogger.Log("TRUCK_REACTION", $"Hire new truck rejected: {GetFleetBuyStatusLabel()}");
            return;
        }

        TruckAgent hiredTruck = CreateAndRegisterTruckAgent(nextHireTruckNumber, truckAgents.Count);
        SetupTruckAudio(hiredTruck);
        nextHireTruckNumber++;
        money -= HireTruckCost;
        RecordMoneyMovement(-HireTruckCost, "Treasury", "Fleet Expansion", $"Hire {hiredTruck.DisplayName}", money);
        SessionDebugLogger.Log("TRUCK", $"Hired {hiredTruck.DisplayName} for ${HireTruckCost}. Money now ${money}.");
        StartPurchasedTruckArrival(hiredTruck);
        NotifyTutorialTruckPurchased(hiredTruck);
        LogTruckReaction(hiredTruck, $"purchased for ${HireTruckCost}; arriving from edge highway");
        TruckAgent selectedTruck = GetTruckAgent(selectedTruckNumber) ?? GetTruckAgent(1);
        if (selectedTruck != null)
        {
            LoadTruckState(selectedTruck);
        }

        isFleetScreenDirty = true;
        PlayUiSound(uiSelectClip, 1f);
    }

    private void HireNewDriver()
    {
        LogUiInput("Drivers: clicked Hire New Driver");
        LogCommand($"HireNewDriver(cost=${HireDriverCost})");
        if (money < HireDriverCost)
        {
            SessionDebugLogger.Log("DRIVER_REACTION", $"Hire new driver rejected: need ${HireDriverCost}.");
            return;
        }

        if (!locations.ContainsKey(LocationType.Motel))
        {
            SessionDebugLogger.Log("DRIVER_REACTION", "Hire new driver rejected: build a Motel first.");
            return;
        }

        if (hiringDriverArrival != null)
        {
            SessionDebugLogger.Log("DRIVER_REACTION", "Hire new driver rejected: another driver is already arriving by bus.");
            return;
        }

        DriverAgent hiredDriver = CreateAndRegisterDriverAgent(spawnInMotel: false);
        money -= HireDriverCost;
        RecordMoneyMovement(-HireDriverCost, "Treasury", "Hiring", $"Hire {hiredDriver.DriverName}", money);
        hiringDriverArrival = new HiringDriverArrivalData
        {
            Driver = hiredDriver,
            Phase = HiringDriverArrivalPhase.WaitingLaneClear
        };
        SessionDebugLogger.Log("DRIVER", $"Hired {hiredDriver.DriverName} for ${HireDriverCost}. Money now ${money}.");
        LogDriverReaction(hiredDriver, $"hired for ${HireDriverCost} and arriving by bus");
        PushFeedEvent(
            $"Hired {hiredDriver.DriverName}. Arrival bus is on the way.",
            $"Нанят {hiredDriver.DriverName}. Автобус с новым рабочим уже в пути.",
            FeedEventType.Success);
        isDriversPanelOpen = false;
        isDriversScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.FirstDriverHired);
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isEconomyScreenDirty = true;
        PlayUiSound(uiSelectClip, 0.96f);
    }

    private void DrawAvailableTripsHud()
    {
        Rect panelRect = GetAvailableTripsHudRect();
        GUI.Box(panelRect, "Available Routes");

        List<TripOption> trips = GetAvailableTrips();
        if (trips.Count == 0)
        {
            GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 34, 240, 22), "No routes available right now.");
            GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 58, 240, 22), "Routes appear when cargo and roads exist.");
            return;
        }

        float y = panelRect.y + 32f;
        int shown = 0;
        foreach (TripOption trip in trips)
        {
            if (shown >= 5)
            {
                break;
            }

            GUI.Box(new Rect(panelRect.x + 10, y, panelRect.width - 20, 52), string.Empty);
            GUI.Label(new Rect(panelRect.x + 18, y + 8, panelRect.width - 36, 20), trip.Title);
            GUI.Label(new Rect(panelRect.x + 18, y + 26, panelRect.width - 120, 18), trip.Description);
            GUI.Label(new Rect(panelRect.x + panelRect.width - 88, y + 26, 60, 18), $"${trip.Reward}");
            y += 58f;
            shown++;
        }
    }

    private void DrawTruckDetailsHud()
    {
        TruckAgent selectedTruck = GetTruckAgent(selectedTruckNumber);
        if (selectedTruck == null)
        {
            isTruckDetailsOpen = false;
            DisableTruckCameraFocus();
            return;
        }

        LoadTruckState(selectedTruck);
        DriverAgent driver = selectedTruck.Driver;
        Rect panelRect = GetTruckDetailsHudRect();
        GUI.Box(panelRect, "Truck HUD");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 30, 220, 22), $"Truck: {GetTruckDisplayName(selectedTruck.TruckNumber)}");
        string shiftStatus = driver.ShiftStartHour < 0 ? "Idle" : GetShiftRangeLabel(driver.ShiftStartHour);
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 52, 260, 18), $"{driver.DriverName}  |  Shift: {shiftStatus}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 74, 220, 22), $"State: {GetTruckDetailStatus(driver)}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 98, 220, 22), $"Fuel: {Mathf.CeilToInt(truckFuel)}/{Mathf.CeilToInt(TruckFuelCapacity)}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 122, 220, 22), $"{L("Cargo")}: {FormatTruckCargoValue(truckCargoAmount, truckCargoType)}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 146, 220, 22), $"Grid cell: {truckCell.x}, {truckCell.y}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 170, 240, 22), $"Assigned route: {GetTripTitle(currentAssignedTrip)}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 192, 240, 22), $"Trip payout: ${currentAssignedTripReward}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 214, 240, 22), isDriverRescueActive ? "Driver: On foot fuel rescue" : "Driver: In truck");
        if (GUI.Button(new Rect(panelRect.x + 12, panelRect.y + 238, panelRect.width - 24, 26), selectedTruck.IsTruckAutoModeEnabled ? "Auto Mode: ON" : "Auto Mode: OFF"))
        {
            SetTruckAutoMode(selectedTruck, !selectedTruck.IsTruckAutoModeEnabled);
            LoadTruckState(selectedTruck);
            PlayUiSound(uiSelectClip, 0.9f);
        }

        List<TripOption> trips = GetAvailableTrips();
        bool truckAvailable = CanIssueOrdersToTruck(selectedTruck);
        float y = panelRect.y + 272f;
        if (!truckAvailable)
        {
            GUI.Label(new Rect(panelRect.x + 12, y - 18f, panelRect.width - 24, 18), GetTruckCommandBlockReason(selectedTruck));
        }

        if (GUI.Button(new Rect(panelRect.x + 12, y, panelRect.width - 24, 26), "Refuel At Gas Station"))
        {
            if (locations.ContainsKey(LocationType.GasStation))
            {
                StartRefuelOrderForTruck(selectedTruck);
            }
            else
            {
                SessionDebugLogger.Log("FUEL", $"{selectedTruck.DisplayName} manual refuel ignored: Gas Station is not built.");
            }

            LoadTruckState(selectedTruck);
        }

        y += 32f;

        GUI.Label(new Rect(panelRect.x + 12, y, 220, 20), "Assign route:");
        y += 24f;

        if (trips.Count == 0)
        {
            GUI.Label(new Rect(panelRect.x + 12, y, 220, 20), "No trips available.");
            y += 26f;
        }
        else
        {
            foreach (TripOption trip in trips)
            {
                if (GUI.Button(new Rect(panelRect.x + 12, y, panelRect.width - 24, 26), $"{trip.Title}  ${trip.Reward}"))
                {
                    AssignTripToTruck(selectedTruck, trip);
                    LoadTruckState(selectedTruck);
                }

                y += 30f;
            }
        }

        if (GUI.Button(new Rect(panelRect.x + 12, panelRect.y + panelRect.height - 32, 120, 24), "Close"))
        {
            ClearTruckFocus();
        }

        SaveTruckState(selectedTruck);
    }

    private bool CanIssueOrdersToTruck(TruckAgent truckAgent)
    {
        if (truckAgent == null)
        {
            return false;
        }

        return truckAgent.CurrentAssignedTrip == TripType.None &&
               truckAgent.CurrentRefuelPhase == RefuelPhase.None &&
               truckAgent.Driver != null &&
               truckAgent.Driver.RestPhase == DriverRestPhase.None &&
               !truckAgent.IsTruckMoving &&
               !truckAgent.IsTruckInteracting &&
               !truckAgent.IsDriverRescueActive &&
               IsDriverOnShift(truckAgent.Driver) &&
               IsTruckInsideParking(truckAgent);
    }

    private string GetTruckCommandBlockReason(TruckAgent truckAgent)
    {
        if (truckAgent == null)
        {
            return "No truck selected.";
        }

        if (truckAgent.Driver == null)
        {
            return "Commands blocked: no boarded driver.";
        }

        if (truckAgent.Driver.RestPhase != DriverRestPhase.None)
        {
            return "Commands blocked: driver is resting at motel.";
        }

        if (!IsDriverOnShift(truckAgent.Driver))
        {
            return "Commands blocked: no active driver shift.";
        }

        if (truckAgent.IsDriverRescueActive)
        {
            return "Commands blocked: driver is on fuel rescue.";
        }

        if (truckAgent.IsTruckInteracting)
        {
            return "Commands blocked: truck is servicing.";
        }

        if (truckAgent.IsTruckMoving)
        {
            return "Commands blocked: truck is moving.";
        }

        if (truckAgent.CurrentRefuelPhase != RefuelPhase.None)
        {
            return "Commands blocked: refuel order already active.";
        }

        if (truckAgent.CurrentAssignedTrip != TripType.None)
        {
            return "Commands blocked: route already assigned.";
        }

        if (!IsTruckInsideParking(truckAgent))
        {
            return "Commands blocked: truck must be in parking.";
        }

        return string.Empty;
    }

    private string GetTruckDetailStatus(DriverAgent driver)
    {
        if (isTruckInteracting)
        {
            return "Servicing cargo";
        }

        if (isTruckWaitingForService && queuedServiceLocation.HasValue)
        {
            return $"Waiting at {locations[queuedServiceLocation.Value].Label}";
        }

        if (isTruckMoving)
        {
            return "Moving";
        }

        if (driver.RestPhase != DriverRestPhase.None)
        {
            return driver.RestPhase switch
            {
                DriverRestPhase.ToMotel => "Driving to Motel",
                DriverRestPhase.ParkAtMotel => "Parking at Motel",
                DriverRestPhase.DriverWalkToMotel => "Driver walking to Motel",
                DriverRestPhase.Sleeping => $"Driver sleeping ({Mathf.CeilToInt(driver.SleepTimer)}s)",
                DriverRestPhase.SleepingAtHome => $"Sleeping at home ({Mathf.CeilToInt(driver.SleepTimer)}s)",
                DriverRestPhase.DriverWalkToTruck => "Driver returning to truck",
                DriverRestPhase.ReturnToParking => "Returning from Motel",
                _ => "Resting"
            };
        }

        if (isDriverRescueActive)
        {
            return driver.WalkPhase == DriverRescuePhase.ToGasStation ? "Driver fetching fuel" : "Driver returning with fuel";
        }

        if (currentRefuelPhase != RefuelPhase.None)
        {
            return currentRefuelPhase == RefuelPhase.ReturnToParking ? "Returning from refuel" : "Refuel order";
        }

        if (currentAssignedTrip != TripType.None)
        {
            return $"Queued: {GetTripTitle(currentAssignedTrip)}";
        }

        return IsTruckInsideParking() ? "Parked in parking" : "Idle in world";
    }

    private string GetTimeOfDayLabel()
    {
        float normalizedTime = dayNightCycleTimer / DayNightCycleDuration;
        if (normalizedTime < 0.25f)
        {
            return L("Night");
        }

        if (normalizedTime < 0.5f)
        {
            return L("Morning");
        }

        if (normalizedTime < 0.75f)
        {
            return L("Day");
        }

        return L("Evening");
    }

    private string GetDayNightClockLabel()
    {
        float normalizedTime = dayNightCycleTimer / DayNightCycleDuration;
        int totalMinutes = Mathf.FloorToInt(normalizedTime * 24f * 60f);
        int hours = (totalMinutes / 60) % 24;
        int minutes = totalMinutes % 60;
        return $"{hours:00}:{minutes:00}";
    }

    private string GetWeekDayLabel()
    {
        int dow = (currentDay - 1) % 7;
        bool ru = IsRussianLanguage();
        return dow switch
        {
            0 => ru ? "Пн" : "Mon",
            1 => ru ? "Вт" : "Tue",
            2 => ru ? "Ср" : "Wed",
            3 => ru ? "Чт" : "Thu",
            4 => ru ? "Пт" : "Fri",
            5 => ru ? "Сб" : "Sat",
            _ => ru ? "Вс" : "Sun",
        };
    }

    private void DrawMoneyHud()
    {
        Rect panelRect = GetMoneyHudRect();
        GUI.Box(panelRect, L("Treasury"));
        GUIStyle centeredHudValueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        GUI.Label(new Rect(panelRect.x, panelRect.y + 22, panelRect.width, 26), $"${money}", centeredHudValueStyle);

        if (moneyPopupTimer <= 0f || moneyPopupAmount <= 0)
        {
            return;
        }

        float normalized = 1f - Mathf.Clamp01(moneyPopupTimer / MoneyPopupDuration);
        float rise = Mathf.Lerp(0f, 26f, normalized);
        float alpha = 1f - normalized;
        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 0.95f, 0.55f, alpha);
        GUI.Label(new Rect(panelRect.x, panelRect.y - 8f - rise, panelRect.width, 24), $"+${moneyPopupAmount}", centeredHudValueStyle);
        GUI.color = previousColor;
    }

    private void DrawTimeHud()
    {
        Rect panelRect = GetTimeHudRect();
        GUI.Box(panelRect, $"{(IsRussianLanguage() ? "День" : "Day")} {currentDay} ({GetWeekDayLabel()})");
        GUIStyle centeredHudValueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        GUI.Label(new Rect(panelRect.x, panelRect.y + 22, panelRect.width, 26), $"{GetDayNightClockLabel()}  {GetTimeOfDayLabel()}", centeredHudValueStyle);
    }

    private void DrawSpeedHud()
    {
        Rect panelRect = GetSpeedHudRect();
        GUI.Box(panelRect, L("Speed"));
        string speedLabel = gameSpeedMultiplier == 0 ? L("Paused") : $"{gameSpeedMultiplier}x";
        GUIStyle centeredHudValueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        GUI.Label(new Rect(panelRect.x, panelRect.y + 22, panelRect.width, 26), speedLabel, centeredHudValueStyle);
    }

    private void DrawPauseOverlay()
    {
        if (gameSpeedMultiplier != 0)
        {
            return;
        }

        Rect overlayRect = new Rect(Screen.width * 0.5f - 120f, 78f, 240f, 44f);
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.58f);
        GUI.Box(overlayRect, string.Empty);
        GUI.color = new Color(1f, 0.93f, 0.42f, 1f);

        GUIStyle pausedStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };

        GUI.Label(overlayRect, L("PAUSE"), pausedStyle);
        GUI.color = previousColor;
    }

    private void DrawSelectedBuildingHud(LocationType locationType)
    {
        Rect panelRect = GetSelectedBuildingHudRect();
        GUI.Box(panelRect, $"{L(locations[locationType].Label)} HUD");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 32, 220, 22), $"{L("Selected building")}: {L(locations[locationType].Label)}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 58, 220, 22), L(GetBuildingResourceLabel(locationType)));
    }

    private string GetBuildingResourceLabel(LocationType locationType)
    {
        return locationType switch
        {
            LocationType.Parking => $"Trucks parked: {GetParkingTruckCount()}/{MaxTruckCount}",
            LocationType.GasStation => "Fuel service: Ready",
            LocationType.Forest => $"Logs stored: {locations[LocationType.Forest].LogsStored}/{ForestMaxLogsStorage}",
            LocationType.Warehouse => $"Boards stored: {locations[LocationType.Warehouse].BoardsStored}",
            LocationType.Sawmill => $"Logs: {locations[LocationType.Sawmill].LogsStored} | Boards: {locations[LocationType.Sawmill].BoardsStored}",
            LocationType.FurnitureFactory => $"Boards: {locations[LocationType.FurnitureFactory].BoardsStored} | Textile: {locations[LocationType.FurnitureFactory].TextileStored} | Furniture: {locations[LocationType.FurnitureFactory].FurnitureStored}",
            LocationType.Motel => "Roadside stop",
            LocationType.IntercityStop => "Intercity stop by the highway",
            LocationType.Stop => "Local bus stop",
            LocationType.Bar => "Service fee: $10",
            LocationType.Canteen => "Service fee: $10",
            _ => string.Empty
        };
    }

    private static string GetSelectedLocationDisplayName(LocationType locationType)
    {
        return locationType switch
        {
            LocationType.Parking => L("Parking"),
            LocationType.GasStation => L("Fuel Stop"),
            LocationType.Forest => IsRussianLanguage() ? "Лесозаготовка" : "Lumberyard",
            LocationType.Warehouse => L("Warehouse"),
            LocationType.Sawmill => L("Sawmill"),
            LocationType.FurnitureFactory => L("Furniture Factory"),
            LocationType.Motel => L("Motel"),
            LocationType.IntercityStop => IsRussianLanguage() ? "Междугородняя остановка" : "Intercity Stop",
            LocationType.Stop => IsRussianLanguage() ? "Автобусная остановка" : "Bus Stop",
            LocationType.Bar => L("Bar"),
            LocationType.Canteen => L("Canteen"),
            LocationType.GamblingHall  => L("Gambling Hall"),
            LocationType.CityPark      => IsRussianLanguage() ? "Городской парк" : "City Park",
            LocationType.PersonalHouse => IsRussianLanguage() ? "Жилой дом" : "Personal House",
            LocationType.CarMarket     => IsRussianLanguage() ? "\u0410\u0432\u0442\u043e\u0440\u044b\u043d\u043e\u043a" : "Car Market",
            _ => L("Location")
        };
    }

    private void LogUiInput(string message)
    {
        SessionDebugLogger.Log("UI_INPUT", message);
    }

    private void LogCommand(string message)
    {
        SessionDebugLogger.Log("COMMAND", message);
    }

    private void LogTruckReaction(TruckAgent truckAgent, string message)
    {
        string truckName = truckAgent != null ? truckAgent.DisplayName : "Truck";
        SessionDebugLogger.Log("TRUCK_REACTION", $"{truckName}: {message}");
    }

    private void LogDriverReaction(DriverAgent driver, string message)
    {
        string driverName = driver != null ? driver.DriverName : "Driver";
        SessionDebugLogger.Log("DRIVER_REACTION", $"{driverName}: {message}");
    }

}
