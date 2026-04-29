using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void StartHireArrivalCinematic()
    {
        if (hiringDriverArrival == null) return;   // bus already gone, nothing to track
        tutorialCinematicDriver = hiringDriverArrival.Driver;
        tutorialCinematicPhase  = TutorialCinematicPhase.TrackingBus;
        tutorialCinematicShouldShowForestIntro = true;
        isTruckCameraFocused        = false;
        isCameraReturningToDiorama  = false;
        isCameraRotatingToTarget    = false;
        SessionDebugLogger.Log("TUTORIAL", "Started hire-arrival cinematic.");
    }

    private void UpdateTutorialCinematic(float dt)
    {
        Vector3 worldCenter = new(GridWidth * 0.5f, 0f, GridHeight * 0.5f);

        switch (tutorialCinematicPhase)
        {
            case TutorialCinematicPhase.TrackingBus:
            {
                if (hiringDriverArrival?.BusRootTransform != null)
                {
                    Vector3 busPos = hiringDriverArrival.BusRootTransform.position;
                    busPos.y = 0f;
                    // Faster lock-on once bus has stopped for dropoff
                    float busLerp = hiringDriverArrival.Phase == HiringDriverArrivalPhase.StoppedForDropoff ? 8f : 3.5f;
                    cameraFocusPoint = Vector3.Lerp(cameraFocusPoint, busPos, busLerp * dt);
                }
                // Zoom in toward bus/worker
                cameraOffset       = Vector3.Lerp(cameraOffset,       TutorialCinematicZoomOffset, 2.5f * dt);
                cameraTargetOffset = cameraOffset;

                bool driverSpawned = hiringDriverArrival == null
                    || hiringDriverArrival.Phase == HiringDriverArrivalPhase.DriverWalkingToMotel
                    || hiringDriverArrival.Phase == HiringDriverArrivalPhase.Departing;
                if (driverSpawned)
                {
                    if (tutorialCinematicDriver?.DriverObject != null)
                    {
                        // Snap focus to worker spawn position for a seamless cut
                        Vector3 spawnPos = tutorialCinematicDriver.DriverObject.transform.position;
                        spawnPos.y = 0f;
                        cameraFocusPoint = spawnPos;
                        tutorialCinematicPhase = TutorialCinematicPhase.TrackingWorker;
                    }
                    else
                    {
                        tutorialCinematicPhase = TutorialCinematicPhase.Returning;
                    }
                }
                break;
            }

            case TutorialCinematicPhase.TrackingWorker:
            {
                if (tutorialCinematicDriver?.DriverObject != null && tutorialCinematicDriver.DriverObject.activeSelf)
                {
                    Vector3 workerPos = tutorialCinematicDriver.DriverObject.transform.position;
                    workerPos.y = 0f;
                    cameraFocusPoint = Vector3.Lerp(cameraFocusPoint, workerPos, 4f * dt);
                }
                // Keep zoom in while following worker
                cameraOffset       = Vector3.Lerp(cameraOffset,       TutorialCinematicZoomOffset, 2.5f * dt);
                cameraTargetOffset = cameraOffset;

                if (tutorialCinematicDriver == null || !tutorialCinematicDriver.IsArrivingByBus)
                {
                    HideTutorialOrbitHud();
                    tutorialCinematicPhase = TutorialCinematicPhase.Returning;
                }
                break;
            }

            case TutorialCinematicPhase.TrackingWorkerBackCloseup:
            {
                if (tutorialCinematicDriver?.DriverObject != null && tutorialCinematicDriver.DriverObject.activeSelf)
                {
                    Vector3 workerPos = tutorialCinematicDriver.DriverObject.transform.position;
                    Vector3 forward = Vector3.ProjectOnPlane(tutorialCinematicDriver.DriverObject.transform.forward, Vector3.up);
                    if (forward.sqrMagnitude < 0.001f)
                    {
                        forward = Vector3.forward;
                    }

                    forward.Normalize();
                    Vector3 desiredPosition = workerPos - forward * 5.5f + Vector3.up * 3.0f;
                    Vector3 lookTarget = workerPos + Vector3.up * 1.05f + forward * 1.1f;
                    float positionLerp = 1f - Mathf.Exp(-1.8f * dt);
                    float rotationLerp = 1f - Mathf.Exp(-3.5f * dt);
                    mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredPosition, positionLerp);
                    mainCamera.transform.rotation = Quaternion.Slerp(
                        mainCamera.transform.rotation,
                        Quaternion.LookRotation((lookTarget - mainCamera.transform.position).normalized, Vector3.up),
                        rotationLerp);
                    cameraFocusPoint = workerPos;
                    cameraOffset = mainCamera.transform.position - cameraFocusPoint;
                    cameraTargetOffset = cameraOffset;
                }

                tutorialCinematicPhase = TutorialCinematicPhase.Returning;

                return;
            }

            case TutorialCinematicPhase.Returning:
            {
                cameraFocusPoint   = Vector3.Lerp(cameraFocusPoint,   worldCenter,        2f   * dt);
                cameraOffset       = Vector3.Lerp(cameraOffset,       DioramaCameraOffset, 2.5f * dt);
                cameraTargetOffset = cameraOffset;
                bool focusDone  = (cameraFocusPoint - worldCenter).sqrMagnitude        < 0.5f;
                bool offsetDone = (cameraOffset     - DioramaCameraOffset).sqrMagnitude < 0.01f;
                if (focusDone && offsetDone)
                {
                    cameraFocusPoint        = worldCenter;
                    cameraOffset            = DioramaCameraOffset;
                    cameraTargetOffset      = DioramaCameraOffset;
                    tutorialCinematicDriver = null;
                    tutorialCinematicPhase  = TutorialCinematicPhase.None;
                    // Show tutorial 6 immediately; camera pan to Forest happens inside TryShowTutorial.
                    if (tutorialCinematicShouldShowForestIntro)
                    {
                        ScheduleTutorial(TutorialTrigger.ForestIntroduction);
                    }
                    tutorialCinematicShouldShowForestIntro = false;
                    SessionDebugLogger.Log("TUTORIAL", "Hire-arrival cinematic ended.");
                }
                break;
            }
        }

        mainCamera.transform.position = cameraFocusPoint + cameraOffset;
        mainCamera.transform.rotation = GetDioramaCameraRotation();
    }

    private void StartTutorialCameraFocus(LocationType locationType, bool placeLocationOnRight = false)
    {
        if (mainCamera == null || !locations.ContainsKey(locationType))
        {
            return;
        }

        tutorialCameraFocusTarget = GetLocationCenter(locationType);
        tutorialCameraFocusOffset = TutorialForestZoomOffset;
        tutorialCameraFocusTarget.y = 0f;
        if (placeLocationOnRight)
        {
            Vector3 cameraRight = GetDioramaCameraRotation() * Vector3.right;
            cameraRight.y = 0f;
            if (cameraRight.sqrMagnitude > 0.001f)
            {
                tutorialCameraFocusTarget -= cameraRight.normalized * 3.2f;
            }
        }
        isTutorialCameraFocusActive = true;
        tutorialCameraWanderTime = 0f;
        isTruckCameraFocused = false;
        isCameraReturningToDiorama = false;
        isCameraRotatingToTarget = false;
        cameraTargetOffset = DioramaCameraOffset;
        SessionDebugLogger.Log("TUTORIAL", $"Started smooth tutorial camera focus on {locationType}.");
    }

    private void UpdateTutorialCameraFocus(float dt)
    {
        if (!isTutorialCameraFocusActive || mainCamera == null)
        {
            return;
        }

        bool isTruckFollowMode = tutorialCameraFollowTruck?.TruckObject != null;
        if (isTruckFollowMode)
        {
            Vector3 truckPosition = tutorialCameraFollowTruck.TruckObject.transform.position;
            tutorialCameraFocusTarget = new Vector3(truckPosition.x, 0f, truckPosition.z);
        }
        else if (tutorialCameraFollowHiringBus)
        {
            if (hiringDriverArrival?.BusRootTransform != null)
            {
                Vector3 busPosition = hiringDriverArrival.BusRootTransform.position;
                tutorialCameraFocusTarget = new Vector3(busPosition.x, 0f, busPosition.z);
            }
            else
            {
                tutorialCameraFocusTarget = new Vector3(-1f, 0f, GetEdgeHighwayBusLaneWorldZ(isCitySideLane: false));
            }
        }

        // Wander mode: Tutorial 9 (ForestWorkerStarted) keeps the camera drifting gently around Forest while open.
        bool isWanderMode = isTutorialOpen && activeTutorialTrigger == TutorialTrigger.ForestWorkerStarted;
        if (isWanderMode)
            tutorialCameraWanderTime += dt;

        // Compute target focus point (with sinusoidal wander in wander mode)
        Vector3 focusTarget = tutorialCameraFocusTarget;
        if (isWanderMode)
        {
            float t = tutorialCameraWanderTime;
            focusTarget += new Vector3(Mathf.Sin(t * 0.22f) * 2.0f, 0f, Mathf.Cos(t * 0.17f) * 1.5f);
        }

        float focusLerp = 1f - Mathf.Exp(-TutorialCameraFocusSpeed * dt);
        float offsetLerp = 1f - Mathf.Exp(-(TutorialCameraFocusSpeed * 1.15f) * dt);
        cameraFocusPoint = Vector3.Lerp(cameraFocusPoint, focusTarget, focusLerp);
        cameraOffset = Vector3.Lerp(cameraOffset, tutorialCameraFocusOffset, offsetLerp);
        cameraTargetOffset = cameraOffset;

        mainCamera.transform.position = cameraFocusPoint + cameraOffset;
        mainCamera.transform.rotation = GetDioramaCameraRotation();

        // In wander mode the focus never completes; it stays active until OK is pressed.
        if (!isWanderMode && !isTruckFollowMode && !tutorialCameraFollowHiringBus)
        {
            bool focusDone = (cameraFocusPoint - focusTarget).sqrMagnitude < 0.05f;
            bool offsetDone = (cameraOffset - tutorialCameraFocusOffset).sqrMagnitude < 0.01f;
            if (focusDone && offsetDone)
            {
                cameraFocusPoint = tutorialCameraFocusTarget;
                cameraOffset = tutorialCameraFocusOffset;
                cameraTargetOffset = tutorialCameraFocusOffset;
                mainCamera.transform.position = cameraFocusPoint + cameraOffset;
                mainCamera.transform.rotation = GetDioramaCameraRotation();
                isTutorialCameraFocusActive = false;
                SessionDebugLogger.Log("TUTORIAL", "Completed smooth tutorial camera focus.");
            }
        }
    }

    private bool IsBuildMenuTutorialHighlightActive()
    {
        return false;
    }

    private bool IsWorkersTutorialHighlightActive()
    {
        return false;
    }

    private bool IsHireWorkerTutorialHighlightActive()
    {
        return false;
    }

    private bool IsFleetTutorialHighlightActive()
    {
        return false;
    }

    private bool IsShiftsTutorialHighlightActive()
    {
        return false;
    }

    private bool IsFirstWorkerTutorialHighlightActive()
    {
        return false;
    }

    private bool IsForestAssignTutorialHighlightActive()
    {
        return false;
    }

    private bool IsFleetTruckTutorialHighlightActive()
    {
        return false;
    }

    private bool IsFleetAssignDriverTutorialHighlightActive()
    {
        return false;
    }

    private bool IsFleetDriverPickerTutorialHighlightActive()
    {
        return false;
    }

    private void UpdateTutorialUi()
    {
        float dt = Time.unscaledDeltaTime;
        UpdateTutorialCameraFocus(dt);
        if (pendingTutorialTrigger.HasValue)
        {
            pendingTutorialDelay -= dt;
            if (pendingTutorialDelay <= 0f && !isTutorialOpen)
            {
                TutorialTrigger trigger = pendingTutorialTrigger.Value;
                pendingTutorialTrigger = null;
                TryShowTutorial(trigger);
            }
        }

        if (tutorialHud == null || tutorialHud.CanvasRoot == null)
        {
            return;
        }

        // Hide the window card when no tutorial is actively showing
        if (tutorialHud.WindowRect != null)
            tutorialHud.WindowRect.gameObject.SetActive(isTutorialOpen);

        if (isTutorialOpen && tutorialHud.BodyText != null)
        {
            tutorialWindowTypeTime += dt;
            int visibleChars = Mathf.Clamp(
                Mathf.FloorToInt(tutorialWindowTypeTime * TutorialWindowTypeSpeed),
                0,
                tutorialWindowFullText.Length);
            tutorialHud.BodyText.text = tutorialWindowFullText.Substring(0, visibleChars);
        }

        bool canvasNeeded = isTutorialOpen;
        if (tutorialHud.CanvasRoot.activeSelf != canvasNeeded)
            tutorialHud.CanvasRoot.SetActive(canvasNeeded);

        if (!isTutorialOpen && tutorialHud.OverlayImage != null)
            tutorialHud.OverlayImage.color = OverlayColorTransparent;

        // Bob animation for the floating side card
        if (isTutorialOpen && isTutorialSideMode && tutorialHud.WindowRect != null)
        {
            tutorialBobTime += Time.unscaledDeltaTime;
            float xBase = activeTutorialTrigger switch
            {
                TutorialTrigger.SelectProductionWorker => -640f,
                TutorialTrigger.AssignForestProductionWorker => 640f,
                TutorialTrigger.FleetSelectTruck => -640f,
                TutorialTrigger.FleetAssignDriver => -640f,
                TutorialTrigger.FleetPickDriver => -500f,
                _ => tutorialSideOnLeft ? TutorialSidePanelLeftX : TutorialSidePanelBaseX
            };
            float bobX  = Mathf.Sin(tutorialBobTime * 0.65f) * 3.5f;
            float bobY  = Mathf.Sin(tutorialBobTime * 1.15f) * 5f;
            tutorialHud.WindowRect.anchoredPosition = new Vector2(xBase + bobX, bobY);
        }

        if (tutorialHud.BuildMenuOutlineRoot != null)
            tutorialHud.BuildMenuOutlineRoot.SetActive(IsBuildMenuTutorialHighlightActive());

        if (tutorialHud.WorkersMenuOutlineRoot != null)
            tutorialHud.WorkersMenuOutlineRoot.SetActive(IsWorkersTutorialHighlightActive());

        if (tutorialHud.HireWorkerOutlineRoot != null)
            tutorialHud.HireWorkerOutlineRoot.SetActive(IsHireWorkerTutorialHighlightActive());

        if (tutorialHud.ShiftsMenuOutlineRoot != null)
            tutorialHud.ShiftsMenuOutlineRoot.SetActive(IsShiftsTutorialHighlightActive());

        if (tutorialHud.FleetMenuOutlineRoot != null)
            tutorialHud.FleetMenuOutlineRoot.SetActive(IsFleetTutorialHighlightActive());

        UpdateTutorialElementOutlines();

        // Shift fleet window right when the side tutorial card sits on the left
        if (fleetScreenUi?.ScreenRoot != null)
        {
            RectTransform fleetRect = fleetScreenUi.ScreenRoot.GetComponent<RectTransform>();
            bool shiftRight = isTutorialOpen && isFleetPanelOpen &&
                (activeTutorialTrigger == TutorialTrigger.FleetSelectTruck ||
                 activeTutorialTrigger == TutorialTrigger.FleetAssignDriver ||
                 activeTutorialTrigger == TutorialTrigger.FleetPickDriver);
            float targetX = shiftRight ? 180f : 0f;
            Vector2 fp = fleetRect.anchoredPosition;
            fp.x = Mathf.Lerp(fp.x, targetX, Time.unscaledDeltaTime * 8f);
            fleetRect.anchoredPosition = fp;
        }
    }

    private void UpdateTutorialElementOutlines()
    {
        HideAllTutorialOutlines();
        return;

#pragma warning disable CS0162
        UpdateTutorialOutlineFromTarget(
            tutorialHud.FirstWorkerOutlineRoot,
            IsFirstWorkerTutorialHighlightActive() && shiftsScreenUi != null && shiftsScreenUi.DriverRows.Count > 0
                ? shiftsScreenUi.DriverRows[0].Root
                : null,
            5f);

        RectTransform forestAssignTarget = null;
        if (IsForestAssignTutorialHighlightActive() &&
            logisticsSlots.Length > 0 &&
            logisticsSlots[0]?.AssignButton != null)
        {
            forestAssignTarget = logisticsSlots[0].AssignButton.GetComponent<RectTransform>();
        }

        UpdateTutorialOutlineFromTarget(tutorialHud.ForestAssignOutlineRoot, forestAssignTarget, 5f);

        RectTransform fleetTruckTarget = null;
        if (IsFleetTruckTutorialHighlightActive() &&
            fleetScreenUi != null &&
            fleetScreenUi.TruckRows.Count > 0 &&
            fleetScreenUi.TruckRows[0]?.Root != null)
        {
            fleetTruckTarget = fleetScreenUi.TruckRows[0].Root;
        }

        UpdateTutorialOutlineFromTarget(tutorialHud.FleetTruckOutlineRoot, fleetTruckTarget, 5f);

        RectTransform fleetAssignTarget = null;
        if (IsFleetAssignDriverTutorialHighlightActive() &&
            fleetScreenUi != null &&
            fleetScreenUi.AssignDriverButtons.Count > 0 &&
            fleetScreenUi.AssignDriverButtons[0] != null)
        {
            fleetAssignTarget = fleetScreenUi.AssignDriverButtons[0].GetComponent<RectTransform>();
        }

        UpdateTutorialOutlineFromTarget(tutorialHud.FleetAssignDriverOutlineRoot, fleetAssignTarget, 5f);

        RectTransform fleetPickerTarget = null;
        if (IsFleetDriverPickerTutorialHighlightActive() &&
            fleetScreenUi != null &&
            fleetScreenUi.DriverPickerButtons.Count > 0 &&
            fleetScreenUi.DriverPickerButtons[0] != null &&
            fleetScreenUi.DriverPickerButtons[0].gameObject.activeInHierarchy)
        {
            fleetPickerTarget = fleetScreenUi.DriverPickerButtons[0].GetComponent<RectTransform>();
        }

        UpdateTutorialOutlineFromTarget(tutorialHud.FleetDriverPickerOutlineRoot, fleetPickerTarget, 5f);
#pragma warning restore CS0162
    }

    private void HideAllTutorialOutlines()
    {
        if (tutorialHud == null)
        {
            return;
        }

        tutorialHud.BuildMenuOutlineRoot?.SetActive(false);
        tutorialHud.WorkersMenuOutlineRoot?.SetActive(false);
        tutorialHud.HireWorkerOutlineRoot?.SetActive(false);
        tutorialHud.ShiftsMenuOutlineRoot?.SetActive(false);
        tutorialHud.FleetMenuOutlineRoot?.SetActive(false);
        tutorialHud.FirstWorkerOutlineRoot?.SetActive(false);
        tutorialHud.ForestAssignOutlineRoot?.SetActive(false);
        tutorialHud.FleetTruckOutlineRoot?.SetActive(false);
        tutorialHud.FleetAssignDriverOutlineRoot?.SetActive(false);
        tutorialHud.FleetDriverPickerOutlineRoot?.SetActive(false);
    }

    private DriverAgent GetFirstAssignableProductionWorker()
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver != null &&
                driver.DutyMode == DriverDutyMode.Local &&
                driver.AssignedTruckNumber == 0 &&
                driver.ShiftStartHour < 0 &&
                !driver.IsArrivingByBus)
            {
                return driver;
            }
        }

        return null;
    }

    private void SelectFirstWorkerForProductionTutorial()
    {
        DriverAgent worker = GetFirstAssignableProductionWorker();
        if (worker == null)
        {
            return;
        }

        selectedShiftDriverId = worker.DriverId;
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.AssignForestProductionWorker);
        LogUiInput($"Tutorial: selected {worker.DriverName} for Forest production assignment.");
        PlayUiSound(uiSelectClip, 0.85f);
    }

    private void CompleteProductionWorkerSelectionTutorial(DriverAgent worker)
    {
        if (!isTutorialOpen || activeTutorialTrigger != TutorialTrigger.SelectProductionWorker || worker == null)
        {
            return;
        }

        selectedShiftDriverId = worker.DriverId;
        isTutorialOpen = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.AssignForestProductionWorker);
        LogUiInput($"Tutorial: player selected {worker.DriverName} for production assignment.");
        PlayUiSound(uiPanelCloseClip, 0.82f);
    }

    private void AssignSelectedWorkerToForestFromTutorial()
    {
        DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);
        if (selectedDriver == null)
        {
            selectedDriver = GetFirstAssignableProductionWorker();
            if (selectedDriver != null)
            {
                selectedShiftDriverId = selectedDriver.DriverId;
            }
        }

        if (selectedDriver != null && logisticsSlots.Length > 0 && logisticsSlots[0] != null)
        {
            AssignWorkerToBuilding(selectedDriver, logisticsSlots[0]);
            LogUiInput($"Tutorial: assigned {selectedDriver.DriverName} to Forest production.");
            PlayUiSound(uiSelectClip, 0.85f);
            StartForestWorkerTutorialCommute(selectedDriver);
        }

        isShiftsPanelOpen = false;
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void CompleteForestAssignmentTutorial()
    {
        if (!isTutorialOpen || activeTutorialTrigger != TutorialTrigger.AssignForestProductionWorker)
        {
            return;
        }

        isTutorialOpen = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        isShiftsPanelOpen = false;
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
        LogUiInput("Tutorial: Forest production assignment complete; closed Shifts panel.");
        PlayUiSound(uiPanelCloseClip, 0.82f);

        DriverAgent assignedDriver = driverAgents.Find(d => d.DriverId == selectedShiftDriverId);
        StartForestWorkerTutorialCommute(assignedDriver);
    }

    private void StartForestWorkerTutorialCommute(DriverAgent driver)
    {
        if (driver == null || driver.IsInsideBuilding) return;

        bool shiftAlreadyActive = IsProductionWorkHour(GetCurrentHour());

        if (shiftAlreadyActive)
        {
            // Shift is already active: force commute immediately and focus on Forest so player sees it.
            if (!IsDriverBusyWalkPhase(driver))
                StartDriverBuildingCommute(driver);
            if (locations.ContainsKey(LocationType.Forest))
                StartTutorialCameraFocus(LocationType.Forest, placeLocationOnRight: true);
            return;
        }

        // Shift has not started yet: force early commute and follow with camera + orbit HUD.
        SetTutorialClockToProductionStart();
        driver.RestPhase = DriverRestPhase.None;
        driver.SleepTimer = 0f;
        driver.IsOnActiveShift = false;
        driver.IsInsideBuilding = false;
        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        if (IsDriverBusyWalkPhase(driver)) return;
        StartDriverBuildingCommute(driver);
        SetupTutorialUi();
        tutorialCinematicDriver                = driver;
        tutorialCinematicPhase                 = TutorialCinematicPhase.TrackingWorkerBackCloseup;
        tutorialCinematicShouldShowForestIntro  = false;
        isTutorialCameraFocusActive            = false;
        isTruckCameraFocused                   = false;
        isCameraReturningToDiorama              = false;
        isCameraRotatingToTarget                = false;
        string commuteMessage = IsRussianLanguage()
            ? "Я иду к Лесозаготовке.\n\nКогда рабочий входит в производственное здание в рабочее время 08:00-18:00, производство запускается автоматически."
            : "I am walking to the Forest.\n\nWhen a worker enters a production building during 08:00-18:00, production starts automatically.";
        ShowTutorialOrbitHud(driver, commuteMessage, $"8/{TutorialStepCount}", onOk: null);
    }

}
