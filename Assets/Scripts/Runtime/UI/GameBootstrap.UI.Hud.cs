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
        return selectedGameStartMode == GameStartMode.Tutorial &&
               !isTutorialSkipped &&
               isTutorialGoalsActive &&
               tutorialGoalsMode == TutorialGoalsMode.WorkerCard &&
               activeTutorialGoals.Contains(TutorialGoalKind.WaitForWorkerArrival) &&
               !completedTutorialGoals.Contains(TutorialGoalKind.WaitForWorkerArrival);
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
        centeredHudValueStyle.normal.textColor = GetTreasuryDisplayColor();

        GUI.Label(new Rect(panelRect.x, panelRect.y + 22, panelRect.width, 26), FormatTreasuryAmount(), centeredHudValueStyle);

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

    private void DrawPopulationHud()
    {
        Rect panelRect = GetPopulationHudRect();
        GUI.Box(panelRect, L("Population"));
        GUIStyle centeredHudValueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        GUI.Label(new Rect(panelRect.x, panelRect.y + 22, panelRect.width, 26), driverAgents.Count.ToString(), centeredHudValueStyle);
    }

    private string FormatTreasuryAmount()
    {
        return $"${money}";
    }

    private Color GetTreasuryDisplayColor()
    {
        return money < 0
            ? new Color(1f, 0.32f, 0.28f, 1f)
            : Color.white;
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

    private void DrawTradePolicyHud()
    {
        Color prevColor = GUI.color;
        bool prevEnabled = GUI.enabled;

        GUI.color = new Color(0.02f, 0.03f, 0.05f, 0.86f);
        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), string.Empty);
        GUI.color = prevColor;

        Rect window = new Rect(28f, 24f, Screen.width - 56f, Screen.height - 48f);
        GUI.Box(window, string.Empty);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        GUI.Label(new Rect(window.x + 18f, window.y + 8f, 420f, 42f), L("Trade"), titleStyle);

        if (GUI.Button(new Rect(window.xMax - 46f, window.y + 8f, 34f, 34f), "X"))
        {
            SessionDebugLogger.Log("TRADE_UI", "Closed Trade HUD via OnGUI close button.");
            CloseAllMenus();
            GUI.enabled = prevEnabled;
            GUI.color = prevColor;
            return;
        }

        GUIStyle topRightStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontStyle = FontStyle.Bold
        };
        topRightStyle.normal.textColor = GetTreasuryDisplayColor();
        GUI.Label(new Rect(window.xMax - 230f, window.y + 42f, 176f, 22f), $"{L("Treasury")}: {FormatTreasuryAmount()}", topRightStyle);

        Rect summary = new Rect(window.x + 18f, window.y + 62f, window.width - 36f, 70f);
        GUI.Box(summary, string.Empty);
        GUI.Label(new Rect(summary.x + 14f, summary.y + 8f, summary.width - 28f, 22f), IsRussianLanguage() ? "Политики торговли склада" : "Warehouse Trade Policies", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 16 });
        GUI.Label(new Rect(summary.x + 14f, summary.y + 34f, summary.width - 28f, 24f), BuildTradePoliciesSummaryText());

        Rect table = new Rect(window.x + 18f, summary.yMax + 14f, window.width - 36f, window.height - summary.yMax + window.y - 32f);
        GUI.Box(table, string.Empty);
        DrawTradePolicyTable(table);
        if (isTradeScreenDirty)
        {
            isTradeScreenDirty = false;
        }

        GUI.enabled = prevEnabled;
        GUI.color = prevColor;
    }

    private void DrawTradePolicyTable(Rect table)
    {
        bool ru = IsRussianLanguage();
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        GUIStyle valueStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        GUIStyle statusStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };

        float x = table.x + 16f;
        float y = table.y + 14f;
        float nameW = 190f;
        float stockW = 86f;
        float modeW = 245f;
        float targetW = 132f;
        float priceW = 170f;
        float gap = 12f;

        GUI.Label(new Rect(x, y, nameW, 24f), L("Resource"), headerStyle);
        GUI.Label(new Rect(x + nameW + gap, y, stockW, 24f), ru ? "Склад" : "Stock", headerStyle);
        GUI.Label(new Rect(x + nameW + stockW + gap * 2f, y, modeW, 24f), ru ? "Режим" : "Mode", headerStyle);
        GUI.Label(new Rect(x + nameW + stockW + modeW + gap * 3f, y, targetW, 24f), ru ? "Цель" : "Target", headerStyle);
        GUI.Label(new Rect(x + nameW + stockW + modeW + targetW + gap * 4f, y, priceW, 24f), ru ? "Цена" : "Price", headerStyle);
        GUI.Label(new Rect(x + nameW + stockW + modeW + targetW + priceW + gap * 5f, y, table.width - nameW - stockW - modeW - targetW - priceW - 84f, 24f), L("Status"), headerStyle);

        y += 34f;
        for (int i = 0; i < TradeHudResources.Length; i++)
        {
            TradeResourceType resourceType = TradeHudResources[i];
            Rect rowRect = new Rect(table.x + 12f, y - 5f, table.width - 24f, 52f);
            GUI.Box(rowRect, string.Empty);

            DrawTradeResourceIcon(new Rect(x, y + 4f, 30f, 30f), GetTradeResourceVisualKind(resourceType));
            GUI.Label(new Rect(x + 38f, y + 8f, nameW - 38f, 26f), L(GetTradeResourceShortLabel(resourceType)), valueStyle);
            GUI.Label(new Rect(x + nameW + gap, y + 8f, stockW, 26f), GetWarehouseTradeResourceAmount(resourceType).ToString(), valueStyle);

            Rect modeRect = new Rect(x + nameW + stockW + gap * 2f, y + 6f, modeW, 30f);
            if (GUI.Button(modeRect, GetTradePolicyModeButtonLabel(resourceType)))
            {
                CycleTradePolicyMode(resourceType);
            }

            DrawTradeTargetControls(resourceType, new Rect(x + nameW + stockW + modeW + gap * 3f, y + 6f, targetW, 30f));
            GUI.Label(new Rect(x + nameW + stockW + modeW + targetW + gap * 4f, y + 8f, priceW, 26f), GetTradePolicyPriceText(resourceType), statusStyle);
            GUI.Label(new Rect(x + nameW + stockW + modeW + targetW + priceW + gap * 5f, y + 8f, table.width - nameW - stockW - modeW - targetW - priceW - 84f, 26f), GetTradePolicyStatusText(resourceType), statusStyle);
            y += 62f;
        }
    }

    private string GetTradePolicyPriceText(TradeResourceType resourceType)
    {
        bool ru = IsRussianLanguage();
        int sell = GetTradeResourceUnitPrice(resourceType, TradeOrderType.Sell);
        int buy = GetTradeResourceUnitPrice(resourceType, TradeOrderType.Buy);
        return ru ? $"Прод. ${sell} / Пок. ${buy}" : $"Sell ${sell} / Buy ${buy}";
    }

    private static ResourceVisualKind GetTradeResourceVisualKind(TradeResourceType resourceType)
    {
        return resourceType switch
        {
            TradeResourceType.Logs => ResourceVisualKind.Logs,
            TradeResourceType.Boards => ResourceVisualKind.Boards,
            TradeResourceType.Cotton => ResourceVisualKind.Cotton,
            TradeResourceType.Textile => ResourceVisualKind.Textile,
            TradeResourceType.Furniture => ResourceVisualKind.Furniture,
            _ => ResourceVisualKind.Logs
        };
    }

    private void DrawTradeResourceIcon(Rect rect, ResourceVisualKind iconKind)
    {
        Color prevColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.08f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        switch (iconKind)
        {
            case ResourceVisualKind.Logs:
                DrawTradeIconBar(rect, new Rect(5f, 7f, 19f, 5f), new Color(0.63f, 0.43f, 0.20f, 1f));
                DrawTradeIconBar(rect, new Rect(7f, 13f, 20f, 5f), new Color(0.74f, 0.52f, 0.26f, 1f));
                DrawTradeIconBar(rect, new Rect(4f, 19f, 19f, 5f), new Color(0.56f, 0.36f, 0.16f, 1f));
                break;
            case ResourceVisualKind.Boards:
                DrawTradeIconBar(rect, new Rect(5f, 8f, 21f, 4f), new Color(0.86f, 0.72f, 0.45f, 1f));
                DrawTradeIconBar(rect, new Rect(5f, 14f, 21f, 4f), new Color(0.81f, 0.67f, 0.40f, 1f));
                DrawTradeIconBar(rect, new Rect(5f, 20f, 21f, 4f), new Color(0.76f, 0.61f, 0.35f, 1f));
                break;
            case ResourceVisualKind.Cotton:
                DrawTradeIconBar(rect, new Rect(6f, 9f, 11f, 11f), new Color(0.97f, 0.97f, 0.95f, 1f));
                DrawTradeIconBar(rect, new Rect(14f, 6f, 10f, 10f), new Color(0.98f, 0.98f, 0.96f, 1f));
                DrawTradeIconBar(rect, new Rect(12f, 16f, 9f, 9f), new Color(0.95f, 0.95f, 0.93f, 1f));
                break;
            case ResourceVisualKind.Textile:
                DrawTradeIconBar(rect, new Rect(5f, 7f, 22f, 18f), new Color(0.72f, 0.84f, 0.95f, 1f));
                DrawTradeIconBar(rect, new Rect(9f, 7f, 3f, 18f), new Color(0.55f, 0.74f, 0.90f, 1f));
                DrawTradeIconBar(rect, new Rect(17f, 7f, 3f, 18f), new Color(0.55f, 0.74f, 0.90f, 1f));
                break;
            case ResourceVisualKind.Furniture:
                DrawTradeIconBar(rect, new Rect(6f, 9f, 19f, 4f), new Color(0.78f, 0.56f, 0.30f, 1f));
                DrawTradeIconBar(rect, new Rect(8f, 16f, 15f, 4f), new Color(0.72f, 0.50f, 0.25f, 1f));
                DrawTradeIconBar(rect, new Rect(9f, 20f, 3f, 8f), new Color(0.58f, 0.39f, 0.18f, 1f));
                DrawTradeIconBar(rect, new Rect(20f, 20f, 3f, 8f), new Color(0.58f, 0.39f, 0.18f, 1f));
                break;
        }

        GUI.color = prevColor;
    }

    private static void DrawTradeIconBar(Rect iconRect, Rect localRect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(
            new Rect(iconRect.x + localRect.x, iconRect.y + localRect.y, localRect.width, localRect.height),
            Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void DrawTradeTargetControls(TradeResourceType resourceType, Rect rect)
    {
        int target = GetTradePolicyTarget(resourceType);
        GUI.enabled = target > 0;
        if (GUI.Button(new Rect(rect.x, rect.y, 32f, rect.height), "-"))
        {
            AdjustTradePolicyTarget(resourceType, -1);
            SessionDebugLogger.Log("TRADE_UI", $"Clicked target minus for {resourceType}.");
        }

        GUI.enabled = true;
        GUI.Label(new Rect(rect.x + 38f, rect.y + 4f, 44f, rect.height - 8f), target.ToString(), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
        GUI.enabled = target < 99;
        if (GUI.Button(new Rect(rect.x + 88f, rect.y, 32f, rect.height), "+"))
        {
            AdjustTradePolicyTarget(resourceType, 1);
            SessionDebugLogger.Log("TRADE_UI", $"Clicked target plus for {resourceType}.");
        }

        GUI.enabled = true;
    }

    private string GetTradePolicyModeButtonLabel(TradeResourceType resourceType)
    {
        string suffix = IsRussianLanguage() ? " (клик)" : " (click)";
        return GetTradePolicyModeLabel(GetTradePolicyMode(resourceType)) + suffix;
    }

    private void CycleTradePolicyMode(TradeResourceType resourceType)
    {
        TradePolicyMode current = GetTradePolicyMode(resourceType);
        TradePolicyMode[] order = { TradePolicyMode.None, TradePolicyMode.BuyUpTo, TradePolicyMode.SellAbove };
        int currentIndex = System.Array.IndexOf(order, current);
        for (int step = 1; step <= order.Length; step++)
        {
            TradePolicyMode next = order[(currentIndex + step + order.Length) % order.Length];
            if (IsTradePolicyModeSupported(resourceType, next))
            {
                SessionDebugLogger.Log("TRADE_UI", $"Clicked mode cycle for {resourceType}: {current} -> {next}.");
                SetTradePolicyMode(resourceType, next);
                return;
            }
        }
    }

    private void DrawSelectedBuildingHud(LocationType locationType)
    {
        if (!locations.TryGetValue(locationType, out LocationData location))
        {
            return;
        }

        Rect panelRect = GetSelectedBuildingHudRect();
        GUI.Box(panelRect, $"{L(location.Label)} HUD");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 32, 220, 22), $"{L("Selected building")}: {L(location.Label)}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 58, 220, 22), L(GetBuildingResourceLabel(locationType)));
    }

    private string GetBuildingResourceLabel(LocationType locationType)
    {
        locations.TryGetValue(locationType, out LocationData location);
        return locationType switch
        {
            LocationType.Parking => $"Truck slots: {GetOwnedTruckCount()}/{GetTruckParkingCapacity()} | Bus slots: {GetOwnedBusCount()}/{GetBusParkingCapacity()}",
            LocationType.GasStation => "Fuel service: Ready",
            LocationType.Forest => $"Logs stored: {location?.LogsStored ?? 0}/{ForestMaxLogsStorage}",
            LocationType.Warehouse => $"Boards stored: {location?.BoardsStored ?? 0}",
            LocationType.Docks => location != null ? $"Dock cargo: {GetDocksQuickCargoSummary(location)}" : "Dock cargo: none",
            LocationType.Sawmill => $"Logs: {location?.LogsStored ?? 0} | Boards: {location?.BoardsStored ?? 0}",
            LocationType.FurnitureFactory => $"Boards: {location?.BoardsStored ?? 0} | Textile: {location?.TextileStored ?? 0} | Furniture: {location?.FurnitureStored ?? 0}",
            LocationType.Motel => "Roadside stop",
            LocationType.IntercityStop => "Intercity stop by the highway",
            LocationType.Stop => "Local bus stop",
            LocationType.Bar => "Service fee: $10",
            LocationType.Canteen => "Service fee: $10",
            LocationType.Kiosk => "Snack: $5",
            LocationType.CoffeeShop => "Coffee: $5",
            LocationType.Kindergarten => $"Child care: {FormatKindergartenCoverageLabel(false)}",
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
            LocationType.Docks => IsRussianLanguage() ? "\u0414\u043e\u043a\u0438" : "Docks",
            LocationType.Sawmill => L("Sawmill"),
            LocationType.FurnitureFactory => L("Furniture Factory"),
            LocationType.Motel => L("Motel"),
            LocationType.IntercityStop => IsRussianLanguage() ? "Междугородняя остановка" : "Intercity Stop",
            LocationType.Stop => IsRussianLanguage() ? "Автобусная остановка" : "Bus Stop",
            LocationType.Bar => L("Bar"),
            LocationType.Canteen => L("Canteen"),
            LocationType.Kiosk => IsRussianLanguage() ? "\u041a\u0438\u043e\u0441\u043a" : "Kiosk",
            LocationType.CoffeeShop => IsRussianLanguage() ? "\u041a\u043e\u0444\u0435\u0439\u043d\u044f" : "Coffee Shop",
            LocationType.GamblingHall  => L("Gambling Hall"),
            LocationType.CityPark      => IsRussianLanguage() ? "Городской парк" : "City Park",
            LocationType.PersonalHouse => IsRussianLanguage() ? "Жилой дом" : "Personal House",
            LocationType.Kindergarten  => IsRussianLanguage() ? "\u0414\u0435\u0442\u0441\u043a\u0438\u0439 \u0441\u0430\u0434" : "Kindergarten",
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
