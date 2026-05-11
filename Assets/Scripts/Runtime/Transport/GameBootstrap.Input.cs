using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void HandleHotkeys()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame && IsDemolishConfirmOpen())
        {
            CloseDemolishConfirm(false);
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (TryHandleBuildMenuEscape())
            {
                return;
            }

            bool anyMenuOpen =
                isFleetPanelOpen || isShiftsPanelOpen || isDriversPanelOpen ||
                isResourcesPanelOpen || isEconomyPanelOpen || isTradePanelOpen || isBuildPanelOpen || isWorldMapPanelOpen ||
                isStatesPanelOpen || isSocialGraphPanelOpen || isNoospherePanelOpen || isTruckDetailsOpen || isDriverDetailsOpen || activeBuildTool != BuildTool.None;

            if (anyMenuOpen)
            {
                CloseAllMenus();
            }
            else if (isGameStarted)
            {
                OpenPauseMenu();
            }
            return;
        }

        if (Keyboard.current.deleteKey.wasPressedThisFrame && TryOpenSelectedBuildingDemolishConfirm())
        {
            return;
        }

        if (Keyboard.current.fKey.wasPressedThisFrame && isTruckDetailsOpen && IsTruckNumberOwned(selectedTruckNumber))
        {
            ToggleTruckCameraFocus();
            return;
        }

        if (!isTruckCameraFocused)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                MarkTutorialGoalComplete(TutorialGoalKind.CameraRotate);
                RotateDioramaCamera(-1);
                return;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                MarkTutorialGoalComplete(TutorialGoalKind.CameraRotate);
                RotateDioramaCamera(1);
                return;
            }
        }

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            SetGameSpeed(1);
        }
        else if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            SetGameSpeed(2);
        }
        else if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            SetGameSpeed(3);
        }
        else if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            TogglePauseSpeed();
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            ToggleDebugServicePanel();
        }

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (isWorldMapPanelOpen)
            {
                CloseWorldMapPanel();
            }

            isBuildPanelOpen = !isBuildPanelOpen;
            if (isBuildPanelOpen)
            {
                isFleetPanelOpen = false;
                isShiftsPanelOpen = false;
                isDriversPanelOpen = false;
                isResourcesPanelOpen = false;
                isEconomyPanelOpen = false;
                isTradePanelOpen = false;
                isSocialGraphPanelOpen = false;
                isNoospherePanelOpen = false;
            }
            isBuildScreenDirty = true;
            isSocialGraphScreenDirty = true;
            isNoosphereScreenDirty = true;
            PlayUiSound(isBuildPanelOpen ? uiPanelOpenClip : uiPanelCloseClip, 0.85f);
            return;
        }

        if (isBuildPanelOpen && TryHandleBuildMenuHotkey())
        {
            return;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame && (IsBuildingBuildTool(activeBuildTool) || IsRoadBuildTool(activeBuildTool)))
        {
            buildPlacementRotationIndex = (buildPlacementRotationIndex + 1) % 4;
            isBuildScreenDirty = true;
            SessionDebugLogger.Log("BUILD", $"Build placement rotation changed to {GetBuildRotationLabel()}.");
            PlayUiSound(uiSelectClip, 0.72f);
            return;
        }

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            ToggleWorldMapPanel();
            return;
        }

        for (int truckNumber = 1; truckNumber <= MaxTruckCount; truckNumber++)
        {
            if (WasTruckHotkeyPressed(truckNumber))
            {
                LogUiInput($"Hotkey: selected Truck #{truckNumber}");
                if (isTruckCameraFocused && isTruckDetailsOpen && selectedTruckNumber == truckNumber)
                {
                    ClearTruckFocus();
                    return;
                }

                FocusTruck(truckNumber);
                return;
            }
        }
    }

    private void SetGameSpeed(int multiplier)
    {
        if (isWorldMapPanelOpen)
        {
            worldMapPauseRestoreSpeed = Mathf.Clamp(multiplier, 1, 3);
            lastActiveGameSpeedMultiplier = worldMapPauseRestoreSpeed;
            PlayUiSound(uiSelectClip, 0.8f);
            return;
        }

        gameSpeedMultiplier = Mathf.Clamp(multiplier, 1, 3);
        lastActiveGameSpeedMultiplier = gameSpeedMultiplier;
        Time.timeScale = gameSpeedMultiplier;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        PlayUiSound(uiSelectClip, 0.8f);
    }

    private void TogglePauseSpeed()
    {
        if (isWorldMapPanelOpen)
        {
            PlayUiSound(uiSelectClip, 0.8f);
            return;
        }

        if (gameSpeedMultiplier == 0)
        {
            int resumedSpeed = Mathf.Clamp(lastActiveGameSpeedMultiplier, 1, 3);
            gameSpeedMultiplier = resumedSpeed;
            Time.timeScale = resumedSpeed;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
        else
        {
            lastActiveGameSpeedMultiplier = Mathf.Clamp(gameSpeedMultiplier, 1, 3);
            gameSpeedMultiplier = 0;
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;
        }

        PlayUiSound(uiSelectClip, 0.8f);
    }

    private void ToggleWorldMapPanel()
    {
        if (isWorldMapPanelOpen)
        {
            CloseWorldMapPanel();
            PlayUiSound(uiPanelCloseClip, 0.85f);
        }
        else
        {
            OpenWorldMapPanel();
            PlayUiSound(uiPanelOpenClip, 0.85f);
        }
    }

    private void OpenWorldMapPanel()
    {
        if (isWorldMapPanelOpen)
        {
            return;
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
        isTruckDetailsOpen = false;
        isLocalBusDetailsOpen = false;
        isDriverDetailsOpen = false;
        activeBuildTool = BuildTool.None;
        hoveredBuildCell = null;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        CancelRoadPathMode();
        DisableTruckCameraFocus();
        RefreshSelectionVisuals();

        selectedWorldMapRegionIndex = -1;
        worldMapPauseRestoreSpeed = gameSpeedMultiplier > 0
            ? Mathf.Clamp(gameSpeedMultiplier, 1, 3)
            : Mathf.Clamp(lastActiveGameSpeedMultiplier, 1, 3);
        isWorldMapSimulationPauseActive = gameSpeedMultiplier > 0;
        gameSpeedMultiplier = 0;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;

        isWorldMapPanelOpen = true;
        isWorldMapScreenDirty = true;
        isSocialGraphScreenDirty = true;
        isCityHallScreenDirty = true;
        isNoosphereScreenDirty = true;
        isFleetScreenDirty = true;
        isBuildScreenDirty = true;
        NotifyTutorialWorldMapOpened();
    }

    private void CloseWorldMapPanel()
    {
        if (!isWorldMapPanelOpen)
        {
            return;
        }

        isWorldMapPanelOpen = false;
        isWorldMapScreenDirty = true;

        if (isWorldMapSimulationPauseActive)
        {
            int restoredSpeed = Mathf.Clamp(worldMapPauseRestoreSpeed, 1, 3);
            gameSpeedMultiplier = restoredSpeed;
            lastActiveGameSpeedMultiplier = restoredSpeed;
            Time.timeScale = restoredSpeed;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }

        isWorldMapSimulationPauseActive = false;
    }

    private bool WasTruckHotkeyPressed(int truckNumber)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        return truckNumber switch
        {
            1 => (Keyboard.current.digit1Key != null && Keyboard.current.digit1Key.wasPressedThisFrame) ||
                 (Keyboard.current.numpad1Key != null && Keyboard.current.numpad1Key.wasPressedThisFrame),
            2 => (Keyboard.current.digit2Key != null && Keyboard.current.digit2Key.wasPressedThisFrame) ||
                 (Keyboard.current.numpad2Key != null && Keyboard.current.numpad2Key.wasPressedThisFrame),
            3 => (Keyboard.current.digit3Key != null && Keyboard.current.digit3Key.wasPressedThisFrame) ||
                 (Keyboard.current.numpad3Key != null && Keyboard.current.numpad3Key.wasPressedThisFrame),
            4 => (Keyboard.current.digit4Key != null && Keyboard.current.digit4Key.wasPressedThisFrame) ||
                 (Keyboard.current.numpad4Key != null && Keyboard.current.numpad4Key.wasPressedThisFrame),
            5 => (Keyboard.current.digit5Key != null && Keyboard.current.digit5Key.wasPressedThisFrame) ||
                 (Keyboard.current.numpad5Key != null && Keyboard.current.numpad5Key.wasPressedThisFrame),
            _ => false
        };
    }

    private static bool IsBuildingBuildTool(BuildTool tool)
    {
        return tool == BuildTool.Parking ||
               tool == BuildTool.Warehouse ||
               tool == BuildTool.Stop ||
               tool == BuildTool.Forest ||
               tool == BuildTool.FurnitureFactory ||
               tool == BuildTool.Sawmill ||
               tool == BuildTool.Motel ||
               tool == BuildTool.Bar ||
               tool == BuildTool.Canteen ||
               tool == BuildTool.Kiosk ||
               tool == BuildTool.GasStation ||
               tool == BuildTool.GamblingHall ||
               tool == BuildTool.CityPark ||
               tool == BuildTool.PersonalHouse ||
               tool == BuildTool.Kindergarten ||
               tool == BuildTool.CarMarket ||
               tool == BuildTool.LaborExchange ||
               tool == BuildTool.CityHall ||
               tool == BuildTool.Docks;
    }

    private static bool IsRoadlessBuildTool(BuildTool tool)
    {
        return IsBuildingBuildTool(tool) && !DoesBuildToolRequireRoadAccess(tool);
    }

    private static bool DoesBuildToolRequireRoadAccess(BuildTool tool) => tool switch
    {
        BuildTool.Parking          => true,
        BuildTool.Warehouse        => true,
        BuildTool.Stop             => true,
        BuildTool.Forest           => true,
        BuildTool.FurnitureFactory => true,
        BuildTool.Sawmill          => true,
        BuildTool.GasStation       => true,
        BuildTool.Docks            => true,
        _                          => false
    };

    private static bool IsRoadBuildTool(BuildTool tool)
    {
        return tool == BuildTool.Road || tool == BuildTool.SingleRoad;
    }

    private string GetBuildRotationLabel()
    {
        return (buildPlacementRotationIndex % 4) switch
        {
            1 => "East",
            2 => "South",
            3 => "West",
            _ => "North"
        };
    }

    private void FocusTruck(int truckNumber)
    {
        if (!IsTruckNumberOwned(truckNumber))
        {
            return;
        }

        bool wasTruckCameraFocused = isTruckCameraFocused;

        selectedTruckNumber = truckNumber;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        isTruckDetailsOpen = true;
        isLocalBusDetailsOpen = false;
        isDriverDetailsOpen = false;
        selectedDriverId = 0;
        HideBuildingQuickHudSubmenuImmediate();
        isTruckCameraFocused = false;
        isCameraReturningToDiorama = wasTruckCameraFocused;
        isFleetScreenDirty = true;
        LogUiInput($"Selection: focused {GetTruckDisplayName(truckNumber)}");
        LogTruckReaction(GetTruckAgent(truckNumber), "became the active player-selected truck");
        RefreshSelectionVisuals();
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }

    private void CloseAllMenus()
    {
        bool hadOpenUi =
            isFleetPanelOpen ||
            isShiftsPanelOpen ||
            isDriversPanelOpen ||
            isResourcesPanelOpen ||
            isEconomyPanelOpen ||
            isTradePanelOpen ||
            isWorldMapPanelOpen ||
            isBuildPanelOpen ||
            isStatesPanelOpen ||
            isSocialGraphPanelOpen ||
            isCityHallPanelOpen ||
            isNoospherePanelOpen ||
            isTruckDetailsOpen ||
            isLocalBusDetailsOpen ||
            isDriverDetailsOpen ||
            activeBuildTool != BuildTool.None;

        isFleetPanelOpen = false;
        isShiftsPanelOpen = false;
        isDriversPanelOpen = false;
        isResourcesPanelOpen = false;
        isEconomyPanelOpen = false;
        isTradePanelOpen = false;
        CloseWorldMapPanel();
        isBuildPanelOpen = false;
        isStatesPanelOpen = false;
        isSocialGraphPanelOpen = false;
        isCityHallPanelOpen = false;
        isNoospherePanelOpen = false;
        isTruckDetailsOpen = false;
        isLocalBusDetailsOpen = false;
        isDriverDetailsOpen = false;
        selectedDriverId = 0;
        HideBuildingQuickHudSubmenuImmediate();
        CancelRoadPathMode();
        activeBuildTool = BuildTool.None;
        hoveredBuildCell = null;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        isFleetScreenDirty = true;
        isEconomyScreenDirty = true;
        isTradeScreenDirty = true;
        isBuildScreenDirty = true;
        isWorldMapScreenDirty = true;
        isStatesScreenDirty = true;
        isSocialGraphScreenDirty = true;
        isCityHallScreenDirty = true;
        isNoosphereScreenDirty = true;
        DisableTruckCameraFocus();
        RefreshSelectionVisuals();

        if (hadOpenUi)
        {
            PlayUiSound(uiPanelCloseClip, 0.82f);
        }
    }

    private void HandleCameraInput()
    {
        if (mainCamera == null || Keyboard.current == null)
        {
            return;
        }

        float cameraDeltaTime = Time.unscaledDeltaTime;

        if (isTruckCameraFocused)
        {
            UpdateTruckFollowCamera(cameraDeltaTime);
            return;
        }

        if (isCameraReturningToDiorama || isCameraRotatingToTarget)
        {
            if (isCameraRotatingToTarget)
            {
                cameraOffset = Vector3.Lerp(cameraOffset, cameraTargetOffset, 6.5f * cameraDeltaTime);
                if ((cameraOffset - cameraTargetOffset).sqrMagnitude < 0.0025f)
                {
                    cameraOffset = cameraTargetOffset;
                    isCameraRotatingToTarget = false;
                }
            }

            Vector3 defaultPosition = cameraFocusPoint + cameraOffset;
            Quaternion defaultRotation = GetDioramaCameraRotation();
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, defaultPosition, 5.5f * cameraDeltaTime);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, defaultRotation, 6.5f * cameraDeltaTime);

            if ((mainCamera.transform.position - defaultPosition).sqrMagnitude < 0.01f &&
                Quaternion.Angle(mainCamera.transform.rotation, defaultRotation) < 0.75f &&
                !isCameraRotatingToTarget)
            {
                mainCamera.transform.position = defaultPosition;
                mainCamera.transform.rotation = defaultRotation;
                isCameraReturningToDiorama = false;
            }

            return;
        }

        Vector3 forward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized;
        Vector3 pan = Vector3.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            pan += forward;
        }

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            pan -= forward;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            pan += right;
        }

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            pan -= right;
        }

        if (pan.sqrMagnitude > 0.0001f)
        {
            float zoomT = Mathf.InverseLerp(CameraMinHeight, CameraMaxHeight, cameraOffset.y);
            float panSpeed = CameraPanSpeed * Mathf.Lerp(1f, 6f, zoomT);
            cameraFocusPoint += pan.normalized * (panSpeed * cameraDeltaTime);
            isCameraRotatingToTarget = false;
            MarkTutorialGoalComplete(TutorialGoalKind.CameraPan);
        }

        if (Mouse.current != null)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                lastMousePosition = mousePosition;
                rightMousePressPosition = mousePosition;
                isRightMouseDragging = false;
            }

            if (Mouse.current.rightButton.isPressed)
            {
                Vector2 mouseDelta = mousePosition - lastMousePosition;
                if (mouseDelta.sqrMagnitude > 0.01f)
                {
                    isRightMouseDragging = true;
                }

                if (isRightMouseDragging)
                {
                    cameraFocusPoint -= right * (mouseDelta.x * CameraDragPanMultiplier);
                    cameraFocusPoint -= forward * (mouseDelta.y * CameraDragPanMultiplier);
                    isCameraRotatingToTarget = false;
                    MarkTutorialGoalComplete(TutorialGoalKind.CameraPan);
                }

                lastMousePosition = mousePosition;
            }

            if (Mouse.current.middleButton.wasPressedThisFrame)
            {
                lastMiddleMousePosition = mousePosition;
            }

            if (Mouse.current.middleButton.isPressed)
            {
                Vector2 middleDelta = mousePosition - lastMiddleMousePosition;
                if (middleDelta.sqrMagnitude > 0.01f)
                {
                    float zoomT = Mathf.InverseLerp(CameraMinHeight, CameraMaxHeight, cameraOffset.y);
                    float dragMultiplier = CameraDragPanMultiplier * Mathf.Lerp(1f, 4f, zoomT);
                    cameraFocusPoint -= right   * (middleDelta.x * dragMultiplier);
                    cameraFocusPoint -= forward * (middleDelta.y * dragMultiplier);
                    isCameraRotatingToTarget = false;
                    MarkTutorialGoalComplete(TutorialGoalKind.CameraPan);
                }
                lastMiddleMousePosition = mousePosition;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;
            bool blockingHudOpen = isFleetPanelOpen || isShiftsPanelOpen || isDriversPanelOpen ||
                isResourcesPanelOpen || isEconomyPanelOpen || isTradePanelOpen || isWorldMapPanelOpen || isStatesPanelOpen || isSocialGraphPanelOpen || isNoospherePanelOpen;
            if (Mathf.Abs(scroll) > 0.01f && !blockingHudOpen && !IsPointerOverHud(mousePosition))
            {
                float currentDistance = cameraOffset.magnitude;
                float zoomDistanceT = Mathf.InverseLerp(CameraMinDistance, CameraMaxDistance, currentDistance);
                bool isZoomingOut = scroll < 0f;
                float zoomSpeedScale = isZoomingOut
                    ? Mathf.Lerp(0.55f, 6.2f, Mathf.Pow(zoomDistanceT, 1.55f))
                    : Mathf.Lerp(0.45f, 2.35f, Mathf.SmoothStep(0f, 1f, zoomDistanceT));
                // Normalize scroll to one step per wheel tick, then scale it by current camera distance.
                float zoomStep = Mathf.Sign(scroll) * CameraZoomSpeed * 0.6f * zoomSpeedScale;
                float targetDistance = Mathf.Clamp(currentDistance - zoomStep, CameraMinDistance, CameraMaxDistance);
                Vector3 nextOffset = cameraOffset.normalized * targetDistance;
                float clampedHeight = Mathf.Clamp(cameraFocusPoint.y + nextOffset.y, CameraMinHeight, CameraMaxHeight);
                if (Mathf.Abs(nextOffset.y) > 0.0001f)
                {
                    float heightScale = (clampedHeight - cameraFocusPoint.y) / nextOffset.y;
                    nextOffset *= heightScale;
                }

                cameraOffset = nextOffset;
                cameraTargetOffset = cameraOffset;
                isCameraRotatingToTarget = false;
                MarkTutorialGoalComplete(isZoomingOut ? TutorialGoalKind.CameraZoomOut : TutorialGoalKind.CameraZoomIn);
            }
        }

        ClampCameraFocus();
        if (cameraOffset.sqrMagnitude < 0.0001f)
        {
            cameraOffset = new Vector3(-8f, 10f, -8f);
        }

        mainCamera.transform.position = cameraFocusPoint + cameraOffset;
        mainCamera.transform.rotation = GetDioramaCameraRotation();
    }

    private void UpdateTruckFollowCamera(float cameraDeltaTime)
    {
        TruckAgent focusedTruck = GetTruckAgent(selectedTruckNumber);
        if (focusedTruck?.TruckObject == null)
        {
            ClearTruckFocus();
            return;
        }

        Vector3 truckPosition = focusedTruck.TruckObject.transform.position;
        Vector3 truckForward = Vector3.ProjectOnPlane(focusedTruck.TruckObject.transform.forward, Vector3.up).normalized;
        if (truckForward.sqrMagnitude < 0.0001f)
        {
            truckForward = Vector3.forward;
        }

        Vector3 desiredPosition = truckPosition - truckForward * TruckFollowDistance + Vector3.up * TruckFollowHeight;
        Vector3 lookTarget = truckPosition + Vector3.up * TruckFollowLookHeight + truckForward * 1.2f;

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredPosition, 8f * cameraDeltaTime);
        Quaternion desiredRotation = Quaternion.LookRotation((lookTarget - mainCamera.transform.position).normalized, Vector3.up);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, desiredRotation, 10f * cameraDeltaTime);
    }

    private void ToggleTruckCameraFocus()
    {
        if (isTruckCameraFocused)
        {
            LogUiInput($"Quick HUD: exit follow camera for {GetTruckDisplayName(selectedTruckNumber)}");
            DisableTruckCameraFocus();
            PlayUiSound(uiPanelCloseClip, 0.82f);
            return;
        }

        if (!isTruckDetailsOpen || !IsTruckNumberOwned(selectedTruckNumber))
        {
            return;
        }

        LogUiInput($"Quick HUD: follow camera for {GetTruckDisplayName(selectedTruckNumber)}");
        isTruckCameraFocused = true;
        isCameraReturningToDiorama = false;
        isCameraRotatingToTarget = false;
        PlayUiSound(uiPanelOpenClip, 0.82f);
    }

    private void DisableTruckCameraFocus()
    {
        if (!isTruckCameraFocused)
        {
            return;
        }

        isTruckCameraFocused = false;
        isCameraReturningToDiorama = true;
        isCameraRotatingToTarget = false;
    }

    private void RotateDioramaCamera(int direction)
    {
        Vector3 horizontalOffset = new Vector3(cameraOffset.x, 0f, cameraOffset.z);
        if (horizontalOffset.sqrMagnitude < 0.0001f)
        {
            horizontalOffset = new Vector3(DioramaCameraOffset.x, 0f, DioramaCameraOffset.z);
        }

        horizontalOffset = Quaternion.Euler(0f, 90f * Mathf.Clamp(direction, -1, 1), 0f) * horizontalOffset;
        cameraTargetOffset = new Vector3(horizontalOffset.x, cameraOffset.y, horizontalOffset.z);
        isCameraRotatingToTarget = true;
        isCameraReturningToDiorama = true;
        PlayUiSound(uiSelectClip, 0.75f);
    }

    private Quaternion GetDioramaCameraRotation()
    {
        Vector3 lookDirection = new Vector3(-cameraOffset.x, 0f, -cameraOffset.z);
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            lookDirection = new Vector3(-DioramaCameraOffset.x, 0f, -DioramaCameraOffset.z);
        }

        float yaw = Mathf.Atan2(lookDirection.x, lookDirection.z) * Mathf.Rad2Deg;
        float zoomT = Mathf.InverseLerp(CameraMinHeight, CameraMaxHeight, cameraOffset.y);
        float pitchT = zoomT >= 0.2f
            ? 1f
            : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.2f, zoomT));
        float pitch = Mathf.Lerp(DioramaCameraMinPitch, DioramaCameraPitch, pitchT);
        return Quaternion.Euler(pitch, yaw, 0f);
    }

    private void ClearTruckFocus()
    {
        if (isTruckDetailsOpen && IsTruckNumberOwned(selectedTruckNumber))
        {
            LogUiInput($"Selection: cleared {GetTruckDisplayName(selectedTruckNumber)}");
        }
        isTruckDetailsOpen = false;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        DisableTruckCameraFocus();
        cameraFocusPoint = new Vector3(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
        if (cameraOffset.sqrMagnitude < 0.0001f)
        {
            cameraOffset = DioramaCameraOffset;
        }
        cameraTargetOffset = cameraOffset;
        isFleetScreenDirty = true;
        RefreshSelectionVisuals();
        PlayUiSound(uiPanelCloseClip, 0.82f);
    }

}
