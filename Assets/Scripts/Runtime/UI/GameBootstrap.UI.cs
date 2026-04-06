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
        truckTargetWorld = GetTruckWorldPosition(nextCell);
        truckSegmentProgress = 0f;
        float distance = Vector3.Distance(truckSegmentStartWorld, truckTargetWorld);
        truckSegmentDuration = Mathf.Max(0.38f, distance / TruckCruiseSpeed);
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
        if (driverObject == null)
        {
            return;
        }

        isDriverRescueActive = true;
        currentDriverRescuePhase = DriverRescuePhase.ToGasStation;
        driverObject.SetActive(true);
        if (driverFuelCanTransform != null)
        {
            driverFuelCanTransform.gameObject.SetActive(false);
        }

        driverObject.transform.position = GetDriverStandPointNearTruck();
        driverObject.transform.rotation = truckObject.transform.rotation;
        driverWalkAnimationTime = 0f;
        ApplyDriverPose(0f, 0f);
        driverRescueTargetWorld = GetDriverStandPointNearLocation(LocationType.GasStation);
        BuildDriverRescuePath(driverObject.transform.position, driverRescueTargetWorld);
        SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} ran out of fuel at cell ({truckCell.x},{truckCell.y}); driver started rescue walk.");
        PlayUiSound(uiSelectClip, 0.9f);
    }

    private void BuildDriverRescuePath(Vector3 startWorld, Vector3 targetWorld)
    {
        driverRescuePath.Clear();
        driverRescueWaypointIndex = 0;

        Vector2Int startCell = WorldToCell(startWorld);
        Vector2Int goalCell = WorldToCell(targetWorld);
        List<Vector2Int> cellPath = FindDriverWalkPath(startCell, goalCell);
        if (cellPath == null || cellPath.Count == 0)
        {
            driverRescueTargetWorld = targetWorld;
            return;
        }

        for (int i = 1; i < cellPath.Count; i++)
        {
            driverRescuePath.Add(GetCellCenter(cellPath[i]));
        }

        driverRescuePath.Add(targetWorld);
        driverRescueTargetWorld = targetWorld;
    }

    private List<Vector2Int> FindDriverWalkPath(Vector2Int start, Vector2Int goal)
    {
        LocationType? startLocation = GetContainingLocation(start);
        LocationType? goalLocation = GetContainingLocation(goal);
        return GridPathService.FindPath(
                   start,
                   goal,
                   GridPathService.GetCardinalNeighbors,
                   neighbor => IsWalkableDriverCell(neighbor, startLocation, goalLocation))
               ?? new List<Vector2Int> { start, goal };
    }

    private bool IsWalkableDriverCell(Vector2Int cell, LocationType? startLocation, LocationType? goalLocation)
    {
        if (!IsInsideGrid(cell))
        {
            return false;
        }

        LocationType? containingLocation = GetContainingLocation(cell);
        if (!containingLocation.HasValue)
        {
            return true;
        }

        return containingLocation == startLocation || containingLocation == goalLocation;
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
        bool loadingIntoTruck = activeTruckInteraction == TruckInteractionType.LoadAtForest || activeTruckInteraction == TruckInteractionType.LoadAtWarehouse;
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

    private string GetTruckStatusLabel()
    {
        if (isTruckInteracting)
        {
            return activeTruckInteraction switch
            {
                TruckInteractionType.LoadAtForest => "Loading at Forest...",
                TruckInteractionType.UnloadAtWarehouse => "Unloading at Warehouse...",
                TruckInteractionType.LoadAtWarehouse => "Loading at Warehouse...",
                TruckInteractionType.UnloadAtTown => "Unloading at Town...",
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
            return currentDriverRescuePhase == DriverRescuePhase.ToGasStation
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

        bool canHireTruck = GetOwnedTruckCount() < MaxTruckCount && money >= HireTruckCost;
        GUI.enabled = canHireTruck;
        if (GUI.Button(new Rect(panelRect.x + 12, panelRect.y + panelRect.height - 42f, panelRect.width - 24, 28), $"Hire New Truck  ${HireTruckCost}"))
        {
            HireNewTruck();
        }

        GUI.enabled = true;
        if (GetOwnedTruckCount() >= MaxTruckCount)
        {
            GUI.Label(new Rect(panelRect.x + 12, panelRect.y + panelRect.height - 66f, 240, 20), "Parking is full.");
        }
        else if (money < HireTruckCost)
        {
            GUI.Label(new Rect(panelRect.x + 12, panelRect.y + panelRect.height - 66f, 240, 20), $"Need ${HireTruckCost} to hire a truck.");
        }
    }

    private void HireNewTruck()
    {
        if (GetOwnedTruckCount() >= MaxTruckCount || money < HireTruckCost)
        {
            return;
        }

        TruckAgent hiredTruck = CreateAndRegisterTruckAgent(nextHireTruckNumber, truckAgents.Count);
        SetupTruckAudio(hiredTruck);
        nextHireTruckNumber++;
        money -= HireTruckCost;
        SessionDebugLogger.Log("TRUCK", $"Hired {hiredTruck.DisplayName} for ${HireTruckCost}. Money now ${money}.");
        TruckAgent selectedTruck = GetTruckAgent(selectedTruckNumber) ?? GetTruckAgent(1);
        if (selectedTruck != null)
        {
            LoadTruckState(selectedTruck);
        }

        PlayUiSound(uiSelectClip, 1f);
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
        Rect panelRect = GetTruckDetailsHudRect();
        GUI.Box(panelRect, "Truck HUD");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 30, 220, 22), $"Truck: {GetTruckDisplayName(selectedTruck.TruckNumber)}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 56, 220, 22), $"State: {GetTruckDetailStatus()}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 82, 220, 22), $"Fuel: {Mathf.CeilToInt(truckFuel)}/{Mathf.CeilToInt(TruckFuelCapacity)}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 108, 220, 22), $"Cargo: {truckCargoWood}/1 ({truckCargoSource})");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 134, 220, 22), $"Grid cell: {truckCell.x}, {truckCell.y}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 160, 240, 22), $"Assigned route: {GetTripTitle(currentAssignedTrip)}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 182, 240, 22), $"Trip payout: ${currentAssignedTripReward}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 204, 240, 22), isDriverRescueActive ? "Driver: On foot fuel rescue" : "Driver: In truck");
        if (GUI.Button(new Rect(panelRect.x + 12, panelRect.y + 230, panelRect.width - 24, 26), selectedTruck.IsTruckAutoModeEnabled ? "Auto Mode: ON" : "Auto Mode: OFF"))
        {
            SetTruckAutoMode(selectedTruck, !selectedTruck.IsTruckAutoModeEnabled);
            LoadTruckState(selectedTruck);
            PlayUiSound(uiSelectClip, 0.9f);
        }

        List<TripOption> trips = GetAvailableTrips();
        bool truckAvailable = CanIssueOrdersToTruck(selectedTruck);
        float y = panelRect.y + 266f;
        if (!truckAvailable)
        {
            GUI.Label(new Rect(panelRect.x + 12, y - 18f, panelRect.width - 24, 18), GetTruckCommandBlockReason(selectedTruck));
        }

        if (GUI.Button(new Rect(panelRect.x + 12, y, panelRect.width - 24, 26), "Refuel At Gas Station"))
        {
            StartRefuelOrderForTruck(selectedTruck);
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
               !truckAgent.IsTruckMoving &&
               !truckAgent.IsTruckInteracting &&
               !truckAgent.IsDriverRescueActive &&
               IsTruckInsideParking(truckAgent);
    }

    private string GetTruckCommandBlockReason(TruckAgent truckAgent)
    {
        if (truckAgent == null)
        {
            return "No truck selected.";
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

    private string GetTruckDetailStatus()
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

        if (isDriverRescueActive)
        {
            return currentDriverRescuePhase == DriverRescuePhase.ToGasStation ? "Driver fetching fuel" : "Driver returning with fuel";
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
            return "Night";
        }

        if (normalizedTime < 0.5f)
        {
            return "Morning";
        }

        if (normalizedTime < 0.75f)
        {
            return "Day";
        }

        return "Evening";
    }

    private string GetDayNightClockLabel()
    {
        float normalizedTime = dayNightCycleTimer / DayNightCycleDuration;
        int totalMinutes = Mathf.FloorToInt(normalizedTime * 24f * 60f);
        int hours = (totalMinutes / 60) % 24;
        int minutes = totalMinutes % 60;
        return $"{hours:00}:{minutes:00}";
    }

    private void DrawMoneyHud()
    {
        Rect panelRect = GetMoneyHudRect();
        GUI.Box(panelRect, "Treasury");
        GUI.Label(new Rect(panelRect.x + 14, panelRect.y + 22, 160, 26), $"Money: ${money}");

        if (moneyPopupTimer <= 0f || moneyPopupAmount <= 0)
        {
            return;
        }

        float normalized = 1f - Mathf.Clamp01(moneyPopupTimer / MoneyPopupDuration);
        float rise = Mathf.Lerp(0f, 26f, normalized);
        float alpha = 1f - normalized;
        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 0.95f, 0.55f, alpha);
        GUI.Label(new Rect(panelRect.x + 92, panelRect.y - 8f - rise, 120, 24), $"+${moneyPopupAmount}");
        GUI.color = previousColor;
    }

    private void DrawTimeHud()
    {
        Rect panelRect = GetTimeHudRect();
        GUI.Box(panelRect, "Time");
        GUI.Label(new Rect(panelRect.x + 14, panelRect.y + 22, 160, 26), $"{GetDayNightClockLabel()}  {GetTimeOfDayLabel()}");
    }

    private void DrawSpeedHud()
    {
        Rect panelRect = GetSpeedHudRect();
        GUI.Box(panelRect, "Speed");
        GUI.Label(new Rect(panelRect.x + 14, panelRect.y + 22, 120, 26), $"{gameSpeedMultiplier}x");
    }

    private void DrawTruckFleetHud()
    {
        Rect panelRect = GetTruckFleetHudRect();
        GUI.Box(panelRect, "Fleet");

        float y = panelRect.y + 30f;
        foreach (TruckAgent truckAgent in truckAgents)
        {
            LoadTruckState(truckAgent);
            bool isSelected = selectedTruckNumber == truckAgent.TruckNumber && isTruckDetailsOpen;
            Rect cardRect = new Rect(panelRect.x + 10f, y, panelRect.width - 20f, 54f);
            GUI.Box(cardRect, string.Empty);

            string iconLabel = isSelected ? "[TRUCK]" : "TRUCK";
            if (GUI.Button(new Rect(cardRect.x + 8f, cardRect.y + 10f, 66f, 34f), iconLabel))
            {
                FocusTruck(truckAgent.TruckNumber);
            }

            GUI.Label(new Rect(cardRect.x + 84f, cardRect.y + 8f, 120f, 20f), truckAgent.DisplayName);
            GUI.Label(new Rect(cardRect.x + 84f, cardRect.y + 28f, 120f, 18f), GetTruckFleetStatusLabel());
            SaveTruckState(truckAgent);
            y += 60f;
        }
    }

    private void DrawCameraLegendHud()
    {
        Rect panelRect = GetCameraLegendHudRect();
        GUI.Box(panelRect, "Camera");
        string focusLabel = isTruckDetailsOpen
            ? (isTruckCameraFocused ? "F: Exit truck camera" : "F: Follow selected truck")
            : "F: Select a truck first";
        GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 22f, panelRect.width - 24f, 20f), "Q/E: Rotate map view");
        GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 42f, panelRect.width - 24f, 20f), focusLabel);
    }

    private void DrawSelectedBuildingHud(LocationType locationType)
    {
        Rect panelRect = GetSelectedBuildingHudRect();
        GUI.Box(panelRect, $"{locations[locationType].Label} HUD");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 32, 220, 22), $"Selected building: {locations[locationType].Label}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 58, 220, 22), GetBuildingResourceLabel(locationType));
    }

    private string GetBuildingResourceLabel(LocationType locationType)
    {
        return locationType switch
        {
            LocationType.Parking => $"Trucks parked: {GetParkingTruckCount()}/{MaxTruckCount}",
            LocationType.GasStation => "Fuel service: Ready",
            LocationType.Forest => $"Logs stored: {locations[LocationType.Forest].WoodStored}",
            LocationType.Warehouse => $"Logs stored: {locations[LocationType.Warehouse].WoodStored}",
            LocationType.Town => $"Logs received: {locations[LocationType.Town].WoodStored}",
            _ => string.Empty
        };
    }

    private static string GetSelectedLocationDisplayName(LocationType locationType)
    {
        return locationType switch
        {
            LocationType.Parking => "Parking",
            LocationType.GasStation => "Fuel Stop",
            LocationType.Forest => "Forest",
            LocationType.Warehouse => "Warehouse",
            LocationType.Town => "Town Hall",
            _ => "Location"
        };
    }

}

