using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class GameBootstrap
{
    // Menu bar

    private const float MenuBtnMaxW = 90f, MenuBtnMinW = 56f, MenuBtnH = 40f, MenuBtnGap = 5f;
    private const int DefaultMenuBtnCount = 9;

    private Rect GetMenuBarRect()
    {
        float buttonWidth = GetMenuButtonWidth();
        int buttonCount = GetVisibleMenuButtonCount();
        float w = buttonCount * buttonWidth + (buttonCount + 1) * MenuBtnGap;
        return new Rect(12f, 12f, w, MenuBtnH + 10f);
    }

    private float GetMenuButtonWidth()
    {
        float safeRight = Mathf.Max(620f, GetWeatherHudRect().x - 18f);
        int buttonCount = GetVisibleMenuButtonCount();
        float available = Mathf.Max(0f, safeRight - 12f - (buttonCount + 1) * MenuBtnGap);
        return Mathf.Clamp(available / buttonCount, MenuBtnMinW, MenuBtnMaxW);
    }

    private Rect GetFleetPanelRect()
    {
        float height = 44f + truckAgents.Count * 122f;
        return new Rect(12f, 68f, 320f, height);
    }

    private Rect GetShiftsPanelRect()
    {
        const float leftW = 210f, rightW = 476f, pad = 8f, gap = 8f;
        float leftH = 34f + driverAgents.Count * 52f + pad;
        float rightH = pad;
        foreach (int hour in ShiftPresetHours)
        {
            int n = 0;
            foreach (DriverAgent driver in driverAgents) if (driver.ShiftStartHour == hour) n++;
            rightH += Mathf.Max(32f + n * 30f + 44f + pad, 112f) + gap;
        }
        float h = Mathf.Max(leftH, rightH) + 48f;
        return new Rect(12f, 68f, leftW + rightW + gap + pad * 2, h);
    }

    private Rect GetDriversPanelRect() => Rect.zero; // Drivers panel is now Canvas-based

    private Rect GetResourcesPanelRect() => new Rect(12f, 68f, 300f, 210f);

    private Rect GetBuildPanelRect() => new Rect(12f, 68f, 300f, 140f);

    private void ToggleMenuPanel(string panelName, ref bool target)
    {
        if (panelName == "Map")
        {
            ToggleWorldMapPanel();
            return;
        }

        bool wasOpen = target;
        if (isWorldMapPanelOpen)
        {
            CloseWorldMapPanel();
        }

        isFleetPanelOpen = false;
        isShiftsPanelOpen = false;
        isDriversPanelOpen = false;
        isResourcesPanelOpen = false;
        isEconomyPanelOpen = false;
        isTradePanelOpen = false;
        isBuildPanelOpen = false;
        isStatesPanelOpen = false;
        isSocialGraphPanelOpen = false;
        isCityHallPanelOpen = false;
        isNoospherePanelOpen = false;
        target = !wasOpen;
        if (panelName == "Economy")
        {
            isEconomyTaxesTabActive = true;
        }
        if (panelName == "Workers")
        {
            MarkTutorialGoalComplete(TutorialGoalKind.OpenWorkersCard);
            NotifyTutorialWorkersPanelOpened();
        }
        if (panelName == "Shifts")
        {
            selectedLocation = null;
            selectedLocalStopIndex = -1;
            selectedPersonalHouseIndex = -1;   // close building microhud (Forest was selected by tutorial 6)
        }
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
        isEconomyScreenDirty = true;
        isTradeScreenDirty = true;
        isBuildScreenDirty = true;
        isWorldMapScreenDirty = true;
        isStatesScreenDirty = true;
        isSocialGraphScreenDirty = true;
        isCityHallScreenDirty = true;
        isNoosphereScreenDirty = true;
        if (panelName == "Stats")
        {
            EnsureStatesScreenUiReady();
            UpdateStatesScreenUi();
        }
        if (panelName == "Social")
        {
            if (target) ResetSocialGraphScreenSelection();
            EnsureSocialGraphScreenUiReady();
            UpdateSocialGraphScreenUi();
        }
        LogUiInput($"MenuBar: {(target ? "opened" : "closed")} {panelName}");
        PlayUiSound(target ? uiPanelOpenClip : uiPanelCloseClip, 0.9f);
    }

    private void DrawMenuBar()
    {
        Rect bar = GetMenuBarRect();
        GUI.Box(bar, string.Empty);

        float buttonWidth = GetMenuButtonWidth();
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = buttonWidth < 62f ? 9 : buttonWidth < 74f ? 10 : buttonWidth < 84f ? 12 : 14,
            fontStyle = FontStyle.Bold
        };

        float btnY = bar.y + 5f;
        Color prevColor = GUI.color;
        bool prevEnabled = GUI.enabled;

        try
        {
            void MenuBtn(string panelName, string displayLabel, ref bool state, float x, bool highlight = false)
            {
                GUI.color = state ? Color.yellow : Color.white;
                Rect buttonRect = new Rect(x, btnY, buttonWidth, MenuBtnH);
                if (GUI.Button(buttonRect, L(displayLabel), btnStyle))
                    ToggleMenuPanel(panelName, ref state);

                if (highlight)
                {
                    Color outlineColor = GUI.color;
                    GUI.color = new Color(1f, 0.12f, 0.08f, 1f);
                    GUI.Box(new Rect(buttonRect.x - 3f, buttonRect.y - 3f, buttonRect.width + 6f, 3f), string.Empty);
                    GUI.Box(new Rect(buttonRect.x - 3f, buttonRect.yMax, buttonRect.width + 6f, 3f), string.Empty);
                    GUI.Box(new Rect(buttonRect.x - 3f, buttonRect.y - 3f, 3f, buttonRect.height + 6f), string.Empty);
                    GUI.Box(new Rect(buttonRect.xMax, buttonRect.y - 3f, 3f, buttonRect.height + 6f), string.Empty);
                    GUI.color = outlineColor;
                }
            }

            float x = bar.x + MenuBtnGap;
            MenuBtn("Workers", "Residents", ref isDriversPanelOpen, x); x += buttonWidth + MenuBtnGap;
            MenuBtn("Social", "Social", ref isSocialGraphPanelOpen, x); x += buttonWidth + MenuBtnGap;
            MenuBtn("Noosphere", IsRussianLanguage() ? "\u041d\u043e\u043e\u0441" : "Noos", ref isNoospherePanelOpen, x); x += buttonWidth + MenuBtnGap;
            if (ShouldShowTutorialStaffingMenuButton())
            {
                MenuBtn("Vacancies", "Staffing", ref isShiftsPanelOpen, x, highlight: isTutorialGoalsActive);
                x += buttonWidth + MenuBtnGap;
            }
            MenuBtn("Resources", "Resources", ref isResourcesPanelOpen, x); x += buttonWidth + MenuBtnGap;
            MenuBtn("Economy", "Economy", ref isEconomyPanelOpen, x); x += buttonWidth + MenuBtnGap;
            MenuBtn("Trade", "Trade", ref isTradePanelOpen, x); x += buttonWidth + MenuBtnGap;
            MenuBtn("Stats", "Stats", ref isStatesPanelOpen, x); x += buttonWidth + MenuBtnGap;
            MenuBtn("Building", "Building", ref isBuildPanelOpen, x); x += buttonWidth + MenuBtnGap;
            MenuBtn("Map", "Map", ref isWorldMapPanelOpen, x); x += buttonWidth + MenuBtnGap;
        }
        finally
        {
            GUI.color = prevColor;
            GUI.enabled = prevEnabled;
        }
    }

    // Fleet panel

    private void DrawFleetPanel()
    {
    }

    // Shifts panel

    private static readonly int[]    ShiftPresetHours = { 6, 14, 22 };
    private static readonly string[] ShiftNames       = { "Morning", "Evening", "Night" };

    private void DrawShiftsPanel()
    {
        if (!hasLoggedLegacyShiftsHudDraw)
        {
            hasLoggedLegacyShiftsHudDraw = true;
            SessionDebugLogger.LogVerbose("SHIFTS_HUD", "Legacy OnGUI DrawShiftsPanel was called. This should be inactive while ShiftsScreenCanvas owns the HUD.");
        }

        const float leftW = 210f, rightW = 476f, pad = 8f, gap = 8f;

        Rect panelRect = GetShiftsPanelRect();

        // Styles
        GUIStyle titleStyle  = new GUIStyle(GUI.skin.box)   { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
        GUIStyle secStyle    = new GUIStyle(GUI.skin.box)   { fontSize = 13, fontStyle = FontStyle.Bold };
        GUIStyle labelBold   = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
        GUIStyle labelMid    = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        GUIStyle labelSm     = new GUIStyle(GUI.skin.label) { fontSize = 11 };
        GUIStyle btnStyle    = new GUIStyle(GUI.skin.button) { fontSize = 12 };
        GUIStyle btnSmall    = new GUIStyle(GUI.skin.button) { fontSize = 11 };
        GUIStyle idleColor   = new GUIStyle(labelSm)  { normal = { textColor = new Color(0.65f, 0.65f, 0.65f) } };
        GUIStyle assignColor = new GUIStyle(labelSm)  { normal = { textColor = new Color(0.45f, 0.9f,  0.45f) } };

        GUI.Box(panelRect, L("Shift Management"), titleStyle);

        // в”Ђв”Ђ Left column: driver list в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        Rect leftRect = new Rect(panelRect.x + pad, panelRect.y + 38f, leftW, panelRect.height - 46f);
        GUI.Box(leftRect, L("Drivers"), secStyle);

        float dy = leftRect.y + 32f;
        foreach (DriverAgent d in driverAgents)
        {
            bool isSelected = selectedShiftDriverId == d.DriverId;

            Rect rowRect = new Rect(leftRect.x + 4f, dy, leftRect.width - 8f, 48f);

            Color prev = GUI.color;
            GUI.color = isSelected ? new Color(1f, 0.88f, 0.25f) : Color.white;
            GUI.Box(rowRect, string.Empty);
            GUI.color = prev;

            GUI.Label(new Rect(rowRect.x + 8f, rowRect.y + 6f,  rowRect.width - 16f, 20f), d.DriverName, labelBold);

            bool isAssigned = d.ShiftStartHour >= 0;
            string statusText = isAssigned ? $"{L("Assigned")}: {GetShiftRangeLabel(d.ShiftStartHour)}" : L("Idle");
            GUI.Label(new Rect(rowRect.x + 8f, rowRect.y + 27f, rowRect.width - 16f, 16f), statusText,
                isAssigned ? assignColor : idleColor);

            // Invisible full-row button for selection
            if (GUI.Button(rowRect, string.Empty, GUIStyle.none))
            {
                selectedShiftDriverId = isSelected ? 0 : d.DriverId;
                LogUiInput($"Shifts: {(isSelected ? $"deselected {d.DriverName}" : $"selected {d.DriverName}")}");
                PlayUiSound(uiSelectClip, 0.8f);
            }

            dy += 52f;
        }

        // в”Ђв”Ђ Right column: shift cards stacked в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        float rx = panelRect.x + pad + leftW + gap;
        float cy = panelRect.y + 38f;

        // Find currently selected driver
        DriverAgent selDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);

        for (int c = 0; c < 3; c++)
        {
            // Compute card height
            int assignedCount = 0;
            foreach (DriverAgent driver in driverAgents)
                if (driver.ShiftStartHour == ShiftPresetHours[c]) assignedCount++;
            float cardH = Mathf.Max(32f + assignedCount * 30f + 44f + pad, 112f);

            Rect card = new Rect(rx, cy, rightW, cardH);
            GUI.Box(card, string.Empty);

            // Card header
            string header = $"{L(ShiftNames[c])}   {GetShiftRangeLabel(ShiftPresetHours[c])}";
            GUI.Label(new Rect(card.x + 10f, card.y + 7f, card.width - 20f, 22f), header, labelBold);

            // Separator line (thin box)
            GUI.Box(new Rect(card.x + 6f, card.y + 30f, card.width - 12f, 2f), string.Empty);

            float ry = card.y + 36f;
            bool hasDrivers = false;
            foreach (DriverAgent d in driverAgents)
            {
                if (d.ShiftStartHour != ShiftPresetHours[c]) continue;
                hasDrivers = true;

                GUI.Box(new Rect(card.x + 6f, ry, card.width - 12f, 26f), string.Empty);
                GUI.Label(new Rect(card.x + 12f, ry + 4f, card.width - 80f, 18f), d.DriverName, labelMid);
                if (GUI.Button(new Rect(card.x + card.width - 68f, ry + 3f, 60f, 20f), "Remove", btnSmall))
                {
                    LogUiInput($"Shifts: removed {d.DriverName} from {ShiftNames[c]}");
                    LogCommand($"RemoveShift({d.DriverName})");
                    d.ShiftStartHour = -1;
                    d.IsOnActiveShift = false;
                    d.WaitingForShiftAtParking = false;
                    d.NeedsShiftEndReturn = false;
                    TruckAgent assignedTruck = GetAssignedTruckForDriver(d);
                    if (assignedTruck != null)
                    {
                        assignedTruck.IsTruckAutoModeEnabled = false;
                    }
                    if (selectedShiftDriverId == d.DriverId) selectedShiftDriverId = 0;
                    PlayUiSound(uiSelectClip, 0.85f);
                    SessionDebugLogger.Log("SHIFT", $"{d.DriverName} removed from shift - now Idle.");
                    LogDriverReaction(d, "shift removed; now idle");
                }
                ry += 30f;
            }

            if (!hasDrivers)
            {
                GUI.Label(new Rect(card.x + 12f, ry, card.width - 24f, 18f), "No workers assigned", idleColor);
            }

            // Assign button
            bool alreadyHere = selDriver != null && selDriver.ShiftStartHour == ShiftPresetHours[c];
            bool canAssign   = selDriver != null && !alreadyHere;
            string assignLabel = selDriver == null      ? "Select a worker to assign"
                               : alreadyHere            ? $"{selDriver.DriverName} already assigned"
                               :                          $"Assign  {selDriver.DriverName}  в†’  {ShiftNames[c]}";

            GUI.enabled = canAssign;
            if (GUI.Button(new Rect(card.x + 6f, card.y + cardH - 38f, card.width - 12f, 30f), assignLabel, btnStyle))
            {
                LogUiInput($"Shifts: assigned {selDriver.DriverName} to {ShiftNames[c]}");
                LogCommand($"AssignShift({selDriver.DriverName}, {ShiftNames[c]})");
                selDriver.ShiftStartHour = ShiftPresetHours[c];
                selDriver.IsOnActiveShift = false;
                selDriver.WaitingForShiftAtParking = false;
                bool inWindow = IsHourInShiftWindow(GetCurrentHour(), ShiftPresetHours[c]);
                if (inWindow && selDriver.RestPhase == DriverRestPhase.None)
                {
                    if (IsDriverBusDriver(selDriver))
                    {
                        StartBusDriverShiftCommute(selDriver);
                    }
                    else if (!IsDriverBusyWalkPhase(selDriver))
                    {
                        StartDriverShiftCommute(selDriver);
                    }
                }
                PlayUiSound(uiSelectClip, 0.85f);
                SessionDebugLogger.Log("SHIFT", $"{selDriver.DriverName} assigned to {ShiftNames[c]} ({GetShiftRangeLabel(ShiftPresetHours[c])}).");
                LogDriverReaction(selDriver, $"assigned to {ShiftNames[c]} ({GetShiftRangeLabel(ShiftPresetHours[c])})");
            }
            GUI.enabled = true;

            cy += cardH + gap;
        }
    }

    // в”Ђв”Ђ Drivers panel в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void DrawDriversPanel()
    {
        Rect panelRect = GetDriversPanelRect();
        GUIStyle labelBold = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
        GUIStyle labelMid  = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        GUIStyle btnStyle  = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold };
        GUIStyle btnSmall  = new GUIStyle(GUI.skin.button) { fontSize = 11 };

        GUI.Box(panelRect, "Residents");

        float y = panelRect.y + 34f;
        foreach (DriverAgent d in driverAgents)
        {
            bool isSelected = selectedShiftDriverId == d.DriverId;
            TruckAgent assignedTruck = GetAssignedTruckForDriver(d);

            Rect cardRect = new Rect(panelRect.x + 8f, y, panelRect.width - 16f, 106f);
            Color previousColor = GUI.color;
            GUI.color = isSelected ? new Color(1f, 0.88f, 0.25f) : Color.white;
            GUI.Box(cardRect, string.Empty);
            GUI.color = previousColor;

            GUI.Label(new Rect(cardRect.x + 8f, cardRect.y + 6f,  180f, 20f), d.DriverName, labelBold);
            GUI.Label(new Rect(cardRect.x + 8f, cardRect.y + 26f, cardRect.width - 16f, 18f), $"Assigned truck: {(assignedTruck != null ? assignedTruck.DisplayName : "None")}", labelMid);

            string status = GetDriverWorkforceStatus(assignedTruck, d);
            GUI.Label(new Rect(cardRect.x + 8f, cardRect.y + 44f, cardRect.width - 120f, 18f), $"Status: {status}", labelMid);

            GUIStyle balanceStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(cardRect.x + cardRect.width - 120f, cardRect.y + 82f, 112f, 18f), $"Balance: ${d.Money}", balanceStyle);

            GUI.enabled = assignedTruck != null;
            if (GUI.Button(new Rect(cardRect.x + cardRect.width - 96f, cardRect.y + 54f, 84f, 24f), "Open Fleet", btnStyle))
            {
                LogUiInput($"Drivers: Open Fleet for {assignedTruck.DisplayName} via {d.DriverName}");
                FocusTruck(assignedTruck.TruckNumber);
            }
            GUI.enabled = true;

            if (GUI.Button(cardRect, string.Empty, GUIStyle.none))
            {
                selectedShiftDriverId = isSelected ? 0 : d.DriverId;
                LogUiInput($"Drivers: {(isSelected ? $"deselected {d.DriverName}" : $"selected {d.DriverName}")}");
                PlayUiSound(uiSelectClip, 0.8f);
            }

            y += 114f;
        }
    }

    private string GetDriverWorkforceStatus(TruckAgent truckAgent, DriverAgent driver)
    {
        if (driver == null)
        {
            return "Idle";
        }

        if (driver.IsArrivingByBus)
        {
            return "Arriving by bus";
        }

        if (driver.RestPhase == DriverRestPhase.Sleeping || driver.RestPhase == DriverRestPhase.SleepingAtHome)
        {
            return "Sleeping";
        }

        if (driver.RestPhase != DriverRestPhase.None)
        {
            return "Resting";
        }

        if (driver.WalkPhase == DriverRescuePhase.ToParkingForShift)
        {
            return "Walking to parking";
        }

        if (driver.WalkPhase == DriverRescuePhase.ToMotelFromBusStop)
        {
            return "Walking from bus stop";
        }

        if (driver.WalkPhase == DriverRescuePhase.IdleWander)
        {
            return "Walking near motel";
        }

        if (driver.WaitingForShiftAtParking)
        {
            return "Waiting for shift";
        }

        if (IsDriverBusyWalkPhase(driver) || (truckAgent != null && truckAgent.IsDriverRescueActive))
        {
            return "Working";
        }

        if (truckAgent != null && (truckAgent.IsTruckMoving || truckAgent.IsTruckInteracting || truckAgent.CurrentAssignedTrip != TripType.None || truckAgent.CurrentRefuelPhase != RefuelPhase.None))
        {
            return "Working";
        }

        return driver.AssignedTruckNumber > 0 ? "Assigned" : "Idle";
    }

    // в”Ђв”Ђ Resources panel в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void DrawResourcesPanel()
    {
        Rect panelRect = GetResourcesPanelRect();
        GUIStyle labelName = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
        GUIStyle labelVal  = new GUIStyle(GUI.skin.label) { fontSize = 12 };

        GUI.Box(panelRect, "Resources");

        float y = panelRect.y + 34f;

        if (locations.TryGetValue(LocationType.Forest, out LocationData forest))
        {
            DrawResourceRow(ref y, panelRect, forest,
                $"Logs ready: {forest.LogsStored} / {ForestMaxLogsStorage}", labelName, labelVal);
        }

        if (locations.TryGetValue(LocationType.Sawmill, out LocationData sawmill))
        {
            DrawResourceRow(ref y, panelRect, sawmill,
                $"Boards ready: {sawmill.BoardsStored}", labelName, labelVal);
        }

        if (locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
        {
            DrawResourceRow(ref y, panelRect, warehouse,
                $"Boards stored: {warehouse.BoardsStored}", labelName, labelVal);
        }
    }

    private void DrawResourceRow(ref float y, Rect panelRect, LocationData location, string resourceText, GUIStyle nameStyle, GUIStyle valStyle)
    {
        GUI.Box(new Rect(panelRect.x + 8f, y, panelRect.width - 16f, 52f), string.Empty);
        GUI.Label(new Rect(panelRect.x + 12f, y + 6f,  panelRect.width - 24f, 20f), location.Label, nameStyle);
        GUI.Label(new Rect(panelRect.x + 12f, y + 28f, panelRect.width - 24f, 18f), resourceText, valStyle);
        y += 58f;
    }

    // в”Ђв”Ђ Build panel в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void DrawBuildPanel()
    {
        Rect panelRect = GetBuildPanelRect();
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        GUIStyle smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        GUI.Box(panelRect, "Build");

        GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 32f, panelRect.width - 24f, 20f), "Select a building tool.", labelStyle);

        Color previousColor = GUI.color;
        bool previousEnabled = GUI.enabled;
        GUI.color = new Color(0.55f, 0.58f, 0.62f, 0.8f);
        GUI.enabled = false;

        if (GUI.Button(new Rect(panelRect.x + 12f, panelRect.y + 58f, 72f, 56f), "2-WAY"))
        {
        }

        GUI.enabled = previousEnabled;
        GUI.color = previousColor;
        GUI.Label(new Rect(panelRect.x + 96f, panelRect.y + 66f, panelRect.width - 108f, 20f), "Two-Way Road", labelStyle);
        GUI.Label(
            new Rect(panelRect.x + 96f, panelRect.y + 88f, panelRect.width - 108f, 34f),
            "Temporarily under rework. Use 1-cell roads for now.",
            smallStyle);
    }

    // в”Ђв”Ђ Helpers в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private string GetTruckFleetStatusLabel()
    {
        if (IsTruckOnActiveTradeRun(currentLoadedTruckAgent)) return "Trade";
        if (isTruckInteracting) return "Busy";
        if (isTruckWaitingForService) return "Queue";
        if (isDriverRescueActive) return "Rescue";
        if (isTruckMoving) return "Moving";
        if (currentRefuelPhase != RefuelPhase.None) return "Refuel";
        if (currentAssignedTrip != TripType.None) return "Assigned";
        return IsTruckInsideParking() ? "Parked" : "Idle";
    }

    private List<TripOption> GetAvailableTrips()
    {
        List<TripOption> trips = new();

        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking) ||
            !locations.TryGetValue(LocationType.Forest, out LocationData forest))
        {
            return trips;
        }

        bool hasSawmill = locations.TryGetValue(LocationType.Sawmill, out LocationData sawmill);
        bool hasWarehouse = locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse);
        bool canReachForestTrip = hasSawmill &&
            HasPath(parking.Anchor, forest.Anchor) &&
            HasPath(forest.Anchor, sawmill.Anchor);
        if (hasSawmill && forest.LogsStored > 0 && canReachForestTrip)
        {
            trips.Add(new TripOption
            {
                Type = TripType.ForestToSawmill,
                Title = "Deliver Logs: Forest -> Sawmill",
                Description = "Pick up logs in Forest and deliver them to Sawmill.",
                Reward = GetTripReward(TripType.ForestToSawmill),
                Priority = GetTruckTripPriority(TripType.ForestToSawmill)
            });
        }

        bool canReachForestWarehouseFallback = hasWarehouse &&
            HasPath(parking.Anchor, forest.Anchor) &&
            HasPath(forest.Anchor, warehouse.Anchor);
        if (!canReachForestTrip && hasWarehouse && forest.LogsStored > 0 && canReachForestWarehouseFallback)
        {
            trips.Add(new TripOption
            {
                Type = TripType.ForestToWarehouse,
                Title = "Deliver Logs: Forest -> Warehouse",
                Description = "No sawmill route is available, so store logs at the Warehouse.",
                Reward = GetTripReward(TripType.ForestToWarehouse),
                Priority = GetTruckTripPriority(TripType.ForestToWarehouse)
            });
        }

        bool canReachWarehouseTrip = hasSawmill && hasWarehouse &&
            HasPath(parking.Anchor, sawmill.Anchor) &&
            HasPath(sawmill.Anchor, warehouse.Anchor);
        if (hasSawmill && sawmill.BoardsStored > 0 && canReachWarehouseTrip)
        {
            trips.Add(new TripOption
            {
                Type = TripType.SawmillToWarehouse,
                Title = "Deliver Boards: Sawmill -> Warehouse",
                Description = "Take processed boards from Sawmill to Warehouse.",
                Reward = GetTripReward(TripType.SawmillToWarehouse),
                Priority = GetTruckTripPriority(TripType.SawmillToWarehouse)
            });
        }

        if (locations.TryGetValue(LocationType.FurnitureFactory, out LocationData furnitureFactory))
        {
            bool canReachFactoryFromWarehouse =
                hasWarehouse &&
                HasPath(parking.Anchor, warehouse.Anchor) &&
                HasPath(warehouse.Anchor, furnitureFactory.Anchor);
            bool canReachWarehouseFromFactory =
                hasWarehouse &&
                HasPath(parking.Anchor, furnitureFactory.Anchor) &&
                HasPath(furnitureFactory.Anchor, warehouse.Anchor);

            if (hasWarehouse &&
                warehouse.BoardsStored > 0 &&
                furnitureFactory.BoardsStored < FurnitureFactoryMaxBoardsStorage &&
                canReachFactoryFromWarehouse)
            {
                trips.Add(new TripOption
                {
                    Type = TripType.WarehouseToFurnitureFactoryBoards,
                    Title = "Deliver Boards: Warehouse -> Factory",
                    Description = "Take boards from Warehouse to the Furniture Factory.",
                    Reward = GetTripReward(TripType.WarehouseToFurnitureFactoryBoards),
                    Priority = GetTruckTripPriority(TripType.WarehouseToFurnitureFactoryBoards)
                });
            }

            if (textileStored > 0 &&
                furnitureFactory.TextileStored < FurnitureFactoryMaxTextileStorage &&
                canReachFactoryFromWarehouse)
            {
                trips.Add(new TripOption
                {
                    Type = TripType.WarehouseToFurnitureFactoryTextile,
                    Title = "Deliver Textile: Warehouse -> Factory",
                    Description = "Take textile stock from Warehouse to the Furniture Factory.",
                    Reward = GetTripReward(TripType.WarehouseToFurnitureFactoryTextile),
                    Priority = GetTruckTripPriority(TripType.WarehouseToFurnitureFactoryTextile)
                });
            }

            if (furnitureFactory.FurnitureStored > 0 && canReachWarehouseFromFactory)
            {
                trips.Add(new TripOption
                {
                    Type = TripType.FurnitureFactoryToWarehouse,
                    Title = "Deliver Furniture: Factory -> Warehouse",
                    Description = "Pick up finished furniture and return it to Warehouse storage.",
                    Reward = GetTripReward(TripType.FurnitureFactoryToWarehouse),
                    Priority = GetTruckTripPriority(TripType.FurnitureFactoryToWarehouse)
                });
            }
        }

        if (hasWarehouse && locations.TryGetValue(LocationType.Docks, out LocationData docks))
        {
            bool canReachDocksFromWarehouse =
                HasPath(parking.Anchor, warehouse.Anchor) &&
                HasPath(warehouse.Anchor, docks.Anchor);
            bool canReachWarehouseFromDocks =
                HasPath(parking.Anchor, docks.Anchor) &&
                HasPath(docks.Anchor, warehouse.Anchor);

            if (canReachDocksFromWarehouse)
            {
                AddDocksExportTripIfAvailable(trips, docks, TradeResourceType.Logs, TripType.WarehouseToDocksLogs, "Logs");
                AddDocksExportTripIfAvailable(trips, docks, TradeResourceType.Boards, TripType.WarehouseToDocksBoards, "Boards");
                AddDocksExportTripIfAvailable(trips, docks, TradeResourceType.Furniture, TripType.WarehouseToDocksFurniture, "Furniture");
            }

            if (canReachWarehouseFromDocks)
            {
                AddDocksImportTripIfAvailable(trips, docks, TradeResourceType.Cotton, TripType.DocksToWarehouseCotton, "Cotton");
                AddDocksImportTripIfAvailable(trips, docks, TradeResourceType.Textile, TripType.DocksToWarehouseTextile, "Textile");
                AddDocksImportTripIfAvailable(trips, docks, TradeResourceType.Furniture, TripType.DocksToWarehouseFurniture, "Furniture");
            }
        }

        trips.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        return trips;
    }

    private void AddDocksExportTripIfAvailable(List<TripOption> trips, LocationData docks, TradeResourceType resource, TripType tripType, string label)
    {
        if (GetTradePolicyMode(resource) != TradePolicyMode.SellAbove ||
            !HasBuiltRegionalTradeRoute(resource, TradeOrderType.Sell, RegionalTradeRouteMode.River) ||
            GetDocksExportTripLoadLimit(resource) <= 0 ||
            GetDocksExportStoredResource(docks, resource) >= DocksResourceCapacity)
        {
            return;
        }

        trips.Add(new TripOption
        {
            Type = tripType,
            Title = $"Export {label}: Warehouse -> Docks",
            Description = $"Move assigned export {label} to the Docks before the next ship.",
            Reward = GetTripReward(tripType),
            Priority = GetTruckTripPriority(tripType)
        });
    }

    private void AddDocksImportTripIfAvailable(List<TripOption> trips, LocationData docks, TradeResourceType resource, TripType tripType, string label)
    {
        if (GetDocksImportStoredResource(docks, resource) <= 0)
        {
            return;
        }

        trips.Add(new TripOption
        {
            Type = tripType,
            Title = $"Import {label}: Docks -> Warehouse",
            Description = $"Move imported {label} from the Docks to town storage.",
            Reward = GetTripReward(tripType),
            Priority = GetTruckTripPriority(tripType)
        });
    }

    private int GetTruckTripPriority(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToSawmill => 300,
            TripType.WarehouseToFurnitureFactoryBoards => 300,
            TripType.WarehouseToFurnitureFactoryTextile => 300,
            TripType.DocksToWarehouseTextile when IsTradeResourceNeededByProduction(TradeResourceType.Textile) => 280,
            TripType.DocksToWarehouseFurniture when IsTradeResourceNeededByProduction(TradeResourceType.Furniture) => 280,
            TripType.SawmillToWarehouse when IsTradeResourceNeededByProduction(TradeResourceType.Boards) => 260,
            TripType.FurnitureFactoryToWarehouse => 360,
            TripType.DocksToWarehouseCotton => 210,
            TripType.DocksToWarehouseTextile => 210,
            TripType.DocksToWarehouseFurniture => 210,
            TripType.SawmillToWarehouse => 170,
            TripType.ForestToWarehouse => 130,
            TripType.WarehouseToDocksLogs => 80,
            TripType.WarehouseToDocksBoards => 80,
            TripType.WarehouseToDocksFurniture => 80,
            _ => 0
        };
    }

    private bool IsTradeResourceNeededByProduction(TradeResourceType resourceType)
    {
        return GetProductionReserveForWarehouseExport(resourceType) > 0;
    }

    private void AssignTrip(TripOption trip)
    {
        if (trip == null || trip.Type == TripType.None || currentAssignedTrip != TripType.None || currentRefuelPhase != RefuelPhase.None)
        {
            return;
        }

        currentAssignedTrip = trip.Type;
        currentTripPhase = TripPhase.ToPickup;
        currentAssignedTripReward = trip.Reward;
        PlayUiSound(uiSelectClip, 0.82f);
    }

    private void StartRefuelOrder()
    {
        if (currentAssignedTrip != TripType.None || currentRefuelPhase != RefuelPhase.None)
        {
            return;
        }

        if (!locations.ContainsKey(LocationType.GasStation))
        {
            SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} cannot start refuel order: Gas Station is not built.");
            return;
        }

        currentRefuelPhase = RefuelPhase.ToGasStation;
        PlayUiSound(uiSelectClip, 0.82f);
    }

    private string GetTripTitle(TripType tripType)
    {
        return L(tripType switch
        {
            TripType.ForestToSawmill => "Forest -> Sawmill",
            TripType.ForestToWarehouse => "Forest -> Warehouse",
            TripType.SawmillToWarehouse => "Sawmill -> Warehouse",
            TripType.WarehouseToFurnitureFactoryBoards => "Warehouse -> Furniture Factory (Boards)",
            TripType.WarehouseToFurnitureFactoryTextile => "Warehouse -> Furniture Factory (Textile)",
            TripType.FurnitureFactoryToWarehouse => "Furniture Factory -> Warehouse",
            TripType.WarehouseToDocksLogs => "Warehouse -> Docks (Logs)",
            TripType.WarehouseToDocksBoards => "Warehouse -> Docks (Boards)",
            TripType.WarehouseToDocksFurniture => "Warehouse -> Docks (Furniture)",
            TripType.DocksToWarehouseCotton => "Docks -> Warehouse (Cotton)",
            TripType.DocksToWarehouseTextile => "Docks -> Warehouse (Textile)",
            TripType.DocksToWarehouseFurniture => "Docks -> Warehouse (Furniture)",
            _ => "None"
        });
    }

    private bool IsPointerOverHud(Vector2 screenPosition)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);

        if (GetMoneyHudRect().Contains(guiPosition) ||
            GetCityTrustHudRect().Contains(guiPosition) ||
            GetPopulationHudRect().Contains(guiPosition) ||
            GetTimeHudRect().Contains(guiPosition) ||
            GetSpeedHudRect().Contains(guiPosition) ||
            GetWeatherHudRect().Contains(guiPosition) ||
            GetMenuBarRect().Contains(guiPosition))
        {
            return true;
        }

        if (isFleetPanelOpen && GetFleetPanelRect().Contains(guiPosition)) return true;
        // Shifts / Drivers / Resources / Economy / Build panels are Canvas-based; handled by EventSystem.IsPointerOverGameObject above.
        if (isTruckDetailsOpen && GetTruckDetailsHudRect().Contains(guiPosition)) return true;

        return false;
    }

    private void UpdateMoneyPopup()
    {
        if (moneyPopupTimer <= 0f)
        {
            return;
        }

        moneyPopupTimer = Mathf.Max(0f, moneyPopupTimer - Time.deltaTime);
        if (moneyPopupTimer <= 0f)
        {
            moneyPopupAmount = 0;
        }
    }

    private void AwardMoney(int amount, string fromLabel, string reason)
    {
        if (amount <= 0)
        {
            return;
        }

        money += amount;
        RecordMoneyMovement(
            amount,
            fromLabel,
            "Treasury",
            reason,
            money,
            recipientBalanceAfter: null,
            fromAccountKind: MoneyAccountKind.External,
            toAccountKind: MoneyAccountKind.CityBudget,
            reasonKind: MoneyTransactionReasonKind.Other);
        moneyPopupAmount = amount;
        moneyPopupTimer = MoneyPopupDuration;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isEconomyScreenDirty = true;
    }
}
