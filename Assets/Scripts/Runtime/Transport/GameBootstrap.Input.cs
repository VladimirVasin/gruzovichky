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

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseAllMenus();
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
                RotateDioramaCamera(-1);
                return;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
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
        gameSpeedMultiplier = Mathf.Clamp(multiplier, 1, 3);
        lastActiveGameSpeedMultiplier = gameSpeedMultiplier;
        Time.timeScale = gameSpeedMultiplier;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        PlayUiSound(uiSelectClip, 0.8f);
    }

    private void TogglePauseSpeed()
    {
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

    private void FocusTruck(int truckNumber)
    {
        if (!IsTruckNumberOwned(truckNumber))
        {
            return;
        }

        bool wasTruckCameraFocused = isTruckCameraFocused;

        selectedTruckNumber = truckNumber;
        selectedLocation = null;
        isTruckDetailsOpen = true;
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
            isBuildPanelOpen ||
            isTruckDetailsOpen ||
            activeBuildTool != BuildTool.None;

        isFleetPanelOpen = false;
        isShiftsPanelOpen = false;
        isDriversPanelOpen = false;
        isResourcesPanelOpen = false;
        isBuildPanelOpen = false;
        isTruckDetailsOpen = false;
        activeBuildTool = BuildTool.None;
        hoveredBuildCell = null;
        selectedLocation = null;
        isFleetScreenDirty = true;
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
            cameraFocusPoint += pan.normalized * (CameraPanSpeed * cameraDeltaTime);
            isCameraRotatingToTarget = false;
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
                }

                lastMousePosition = mousePosition;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float currentDistance = cameraOffset.magnitude;
                // Normalize scroll to ±1 per tick regardless of platform scroll magnitude, then apply a fixed step
                float zoomStep = Mathf.Sign(scroll) * CameraZoomSpeed * 0.6f;
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
        return Quaternion.Euler(DioramaCameraPitch, yaw, 0f);
    }

    private void ClearTruckFocus()
    {
        if (isTruckDetailsOpen && IsTruckNumberOwned(selectedTruckNumber))
        {
            LogUiInput($"Selection: cleared {GetTruckDisplayName(selectedTruckNumber)}");
        }
        isTruckDetailsOpen = false;
        selectedLocation = null;
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

    private void HandleRoadRemovalInput()
    {
        if (mainCamera == null ||
            Mouse.current == null ||
            activeBuildTool != BuildTool.Road ||
            !Mouse.current.rightButton.wasReleasedThisFrame ||
            isRightMouseDragging)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if ((mousePosition - rightMousePressPosition).sqrMagnitude > 16f || IsPointerOverHud(mousePosition))
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane plane = new(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
        {
            return;
        }

        Vector2Int cell = WorldToCell(ray.GetPoint(distance));
        if (!roadCells.Contains(cell))
        {
            return;
        }

        selectedLocation = null;
        isTruckDetailsOpen = false;
        DisableTruckCameraFocus();
        RefreshSelectionVisuals();
        RemoveRoad(cell);
    }

    private void HandleRoadPlacementInput()
    {
        if (mainCamera == null ||
            Mouse.current == null ||
            Mouse.current.rightButton.isPressed ||
            !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (IsPointerOverHud(mousePosition))
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane plane = new(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
        {
            return;
        }

        if (TryHandleTruckSelection(ray))
        {
            return;
        }

        Vector2Int cell = WorldToCell(ray.GetPoint(distance));
        if (TryHandleLocationSelection(cell))
        {
            return;
        }

        if (activeBuildTool != BuildTool.Road)
        {
            selectedLocation = null;
            isTruckDetailsOpen = false;
            DisableTruckCameraFocus();
            RefreshSelectionVisuals();
            return;
        }

        if (IsRoadBuildCellBlocked(cell))
        {
            selectedLocation = null;
            isTruckDetailsOpen = false;
            DisableTruckCameraFocus();
            RefreshSelectionVisuals();
            return;
        }

        selectedLocation = null;
        isTruckDetailsOpen = false;
        DisableTruckCameraFocus();
        RefreshSelectionVisuals();
        AddRoad(cell);
    }

    private void SetupBuildHoverHighlight()
    {
        if (worldRoot == null)
        {
            return;
        }

        buildHoverHighlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buildHoverHighlight.name = "BuildHoverHighlight";
        buildHoverHighlight.transform.SetParent(worldRoot, false);
        buildHoverHighlight.transform.localScale = new Vector3(0.92f, 0.04f, 0.92f);
        buildHoverHighlight.GetComponent<Collider>().enabled = false;
        ApplyColor(buildHoverHighlight, new Color(0.22f, 0.9f, 0.32f));
        ConfigureStaticVisual(buildHoverHighlight);
        buildHoverHighlight.SetActive(false);
    }

    private void UpdateBuildHoverHighlight()
    {
        if (buildHoverHighlight == null)
        {
            return;
        }

        hoveredBuildCell = null;
        if (activeBuildTool != BuildTool.Road || mainCamera == null || Mouse.current == null || isTruckCameraFocused || isRightMouseDragging)
        {
            buildHoverHighlight.SetActive(false);
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (IsPointerOverHud(mousePosition))
        {
            buildHoverHighlight.SetActive(false);
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane plane = new(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
        {
            buildHoverHighlight.SetActive(false);
            return;
        }

        Vector2Int cell = WorldToCell(ray.GetPoint(distance));
        if (!IsInsideGrid(cell))
        {
            buildHoverHighlight.SetActive(false);
            return;
        }

        hoveredBuildCell = cell;
        bool canBuild = !IsRoadBuildCellBlocked(cell);
        buildHoverHighlight.SetActive(true);
        buildHoverHighlight.transform.position = GetCellCenter(cell) + new Vector3(0f, RoadHeight + 0.03f, 0f);
        buildHoverHighlight.transform.localScale = new Vector3(canBuild ? 0.92f : 0.98f, 0.04f, canBuild ? 0.92f : 0.98f);

        Renderer renderer = buildHoverHighlight.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial.color = canBuild
                ? new Color(0.22f, 0.9f, 0.32f)
                : new Color(0.92f, 0.28f, 0.22f);
        }
    }

}

