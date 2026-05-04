using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void DrawParkingHud()
    {
        Rect panelRect = GetParkingHudRect();
        GUI.Box(panelRect, "Parking HUD");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 30, 250, 22), "Selected building: Parking");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 54, 250, 22), $"Truck slots: {GetOwnedTruckCount()}/{GetTruckParkingCapacity()}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 74, 250, 22), $"Bus slots: {GetOwnedBusCount()}/{GetBusParkingCapacity()}");

        float y = panelRect.y + 104f;
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

        if (!locations.ContainsKey(LocationType.Parking))
        {
            GUI.Label(new Rect(panelRect.x + 12, y + 8f, 240, 20), "Build Parking first.");
        }
        else
        {
            GUI.Label(new Rect(panelRect.x + 12, y + 8f, panelRect.width - 24, 36), "Vehicles appear automatically from Parking slots when drivers need them.");
        }
    }

    private void HireNewTruck()
    {
        LogUiInput("Fleet/Parking: requested Parking truck slot");
        LogCommand("ProvisionTruckFromParkingSlot()");
        if (!TryProvisionTruckFromParkingCapacity(out TruckAgent truckAgent, "manual Parking slot request"))
        {
            SessionDebugLogger.Log("TRUCK_REACTION", $"Truck slot request rejected: {GetFleetBuyStatusLabel()}");
            return;
        }

        SessionDebugLogger.Log("TRUCK", $"{truckAgent.DisplayName} is ready in Parking; no separate purchase was required.");
        LogTruckReaction(truckAgent, "provisioned from Parking infrastructure slot");
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
        LogUiInput("Drivers: clicked removed direct worker hire action");
        LogCommand("HireNewDriver(disabled)");
        SessionDebugLogger.Log("DRIVER_REACTION", "Direct worker purchase is disabled; workers now arrive through city migration.");
        PushFeedEvent(
            "Direct worker hiring is disabled. Keep vacancies open and workers will arrive by bus.",
            "\u041f\u0440\u044f\u043c\u043e\u0439 \u043d\u0430\u0439\u043c \u0440\u0430\u0431\u043e\u0447\u0438\u0445 \u043e\u0442\u043a\u043b\u044e\u0447\u0435\u043d. \u041e\u0441\u0442\u0430\u0432\u044c \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u0438, \u0438 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u043f\u0440\u0438\u0435\u0434\u0443\u0442 \u043d\u0430 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0435.",
            FeedEventType.Info);
    }
    private bool ShouldStartTutorialWorkerHireWave()
    {
        return selectedGameStartMode == GameStartMode.User &&
               !isTutorialSkipped &&
               isTutorialGoalsActive &&
               tutorialGoalsMode == TutorialGoalsMode.WorkerCard &&
               activeTutorialGoals.Contains(TutorialGoalKind.HireNewWorker) &&
               !completedTutorialGoals.Contains(TutorialGoalKind.HireNewWorker);
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
            LocationType.Parking => $"Truck slots: {GetOwnedTruckCount()}/{GetTruckParkingCapacity()} | Bus slots: {GetOwnedBusCount()}/{GetBusParkingCapacity()}",
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
            LocationType.LaborExchange => $"Vacancies: {CountAvailableLaborExchangePostings()}",
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
            LocationType.LaborExchange => IsRussianLanguage() ? "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430" : "Labor Exchange",
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
