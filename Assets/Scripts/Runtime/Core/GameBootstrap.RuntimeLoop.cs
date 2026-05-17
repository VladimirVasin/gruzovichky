using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public partial class GameBootstrap
{
    private int GetCurrentHour()
    {
        float normalized = dayNightCycleTimer / DayNightCycleDuration;
        return Mathf.FloorToInt(normalized * 24f) % 24;
    }

    private bool IsNightTime()
    {
        float normalizedTime = dayNightCycleTimer / DayNightCycleDuration;
        return normalizedTime < 0.25f;
    }

    private int GetCurrentTotalMinutes()
    {
        float normalized = dayNightCycleTimer / DayNightCycleDuration;
        return Mathf.FloorToInt(normalized * 24f * 60f) % (24 * 60);
    }

    private int GetMinutesUntilShiftStart(DriverAgent driver)
    {
        if (driver == null || driver.ShiftStartHour < 0)
        {
            return int.MaxValue;
        }

        int currentMinutes = GetCurrentTotalMinutes();
        int shiftStartMinutes = driver.ShiftStartHour * 60;
        return (shiftStartMinutes - currentMinutes + 24 * 60) % (24 * 60);
    }

    private bool IsWeekend() => (currentDay - 1) % 7 >= 5;

    // Returns true if 'hour' falls within the 8-hour transport/service window starting at 'shiftStart'.
    private bool IsHourInShiftWindow(int hour, int shiftStart)
    {
        return (hour - shiftStart + 24) % 24 < 8;
    }

    private bool IsShiftWindowActive(int hour, int shiftStart, bool worksWeekends)
    {
        return (worksWeekends || !IsWeekend()) && IsHourInShiftWindow(hour, shiftStart);
    }

    private bool IsProductionWorkHour(int hour)
    {
        if (IsWeekend()) return false;
        return hour >= ProductionWorkStartHour && hour < ProductionWorkEndHour;
    }

    private static string GetProductionWorkRangeLabel()
    {
        return $"{ProductionWorkStartHour:00}:00 - {ProductionWorkEndHour:00}:00";
    }

    private bool IsBuildingWorkerWorkHour(LocationType buildingType, int slotIndex, int hour)
    {
        BuildingWorkScheduleKind scheduleKind = GetBuildingWorkScheduleKind(buildingType);
        if (slotIndex < 0 || slotIndex >= GetBuildingWorkerScheduleSlotCount(buildingType))
        {
            return false;
        }

        if (IsDayWorkSchedule(scheduleKind))
        {
            return IsProductionWorkHour(hour);
        }

        int shiftStart = GetBuildingWorkerShiftStartHour(buildingType, slotIndex);
        return shiftStart >= 0 &&
               IsShiftWindowActive(hour, shiftStart, DoesBuildingScheduleUseWeekends(scheduleKind));
    }

    private bool CanBuildingWorkerWorkToday(LocationType buildingType)
    {
        BuildingWorkScheduleKind scheduleKind = GetBuildingWorkScheduleKind(buildingType);
        return DoesBuildingScheduleUseWeekends(scheduleKind) || !IsWeekend();
    }

    private bool IsLogisticsWorkerWorkHour(DriverAgent driver)
    {
        return driver != null &&
               driver.DutyMode == DriverDutyMode.Logistics &&
               driver.AssignedBuildingType.HasValue &&
               IsBuildingWorkerWorkHour(driver.AssignedBuildingType.Value, GetLogisticsWorkerSlotIndex(driver), GetCurrentHour());
    }

    private static int GetBuildingWorkerShiftPresetIndex(LocationType buildingType, int slotIndex)
    {
        BuildingWorkScheduleKind scheduleKind = GetBuildingWorkScheduleKind(buildingType);
        int normalizedSlot = Mathf.Clamp(slotIndex, 0, Mathf.Max(0, GetBuildingWorkerScheduleSlotCount(buildingType) - 1));
        if (scheduleKind == BuildingWorkScheduleKind.ServiceEveningNight)
        {
            return normalizedSlot == 0 ? 1 : 2;
        }

        if (scheduleKind == BuildingWorkScheduleKind.ServiceDayEvening)
        {
            return normalizedSlot == 0 ? 0 : 1;
        }

        return -1;
    }

    private static int GetBuildingWorkerShiftStartHour(LocationType buildingType, int slotIndex)
    {
        BuildingWorkScheduleKind scheduleKind = GetBuildingWorkScheduleKind(buildingType);
        if (IsDayWorkSchedule(scheduleKind))
        {
            return ProductionWorkStartHour;
        }

        int shiftIndex = GetBuildingWorkerShiftPresetIndex(buildingType, slotIndex);
        return shiftIndex >= 0 && shiftIndex < ShiftPresetHours.Length ? ShiftPresetHours[shiftIndex] : -1;
    }

    private int GetLogisticsWorkerSlotIndex(DriverAgent driver)
    {
        if (driver == null)
        {
            return 0;
        }

        return driver.AssignedBuildingSlotIndex >= 0
            ? driver.AssignedBuildingSlotIndex
            : Mathf.Max(0, driver.ContractSlotIndex);
    }

    private string GetBuildingWorkerWorkRangeLabel(LocationType buildingType, int slotIndex)
    {
        if (IsDayWorkSchedule(GetBuildingWorkScheduleKind(buildingType)))
        {
            return GetProductionWorkRangeLabel();
        }

        int shiftStart = GetBuildingWorkerShiftStartHour(buildingType, slotIndex);
        return shiftStart >= 0 ? GetShiftRangeLabel(shiftStart) : GetProductionWorkRangeLabel();
    }

    // Shift display string: "06:00 \u2013 14:00"
    private static string GetShiftRangeLabel(int shiftStart)
    {
        int end = (shiftStart + 8) % 24;
        return $"{shiftStart:00}:00 \u2013 {end:00}:00";
    }

    private void Awake()
    {
        SessionDebugLogger.SetGameTimeProvider(() => GetDayNightClockLabel());
        SessionDebugLogger.StartNewSession($"{nameof(GameBootstrap)} on {gameObject.scene.name}");
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        gameSpeedMultiplier = 0;
        lastActiveGameSpeedMultiplier = 1;
        AudioListener.pause = true;
        SessionDebugLogger.Log("BOOT", "Initializing main menu bootstrap.");
        SetupMainMenuHud();
        SessionDebugLogger.Log("BOOT", "Main menu ready. World generation waits for selected new game mode.");
        StartMainMenuMusic();
        TryConsumePendingAutoStartMode();
    }

    private void OnApplicationQuit()
    {
        SessionDebugLogger.EndSession("Application quit");
    }

    private void OnDestroy()
    {
        ReleaseBarInteriorSceneResources();
        ClearVisualMaterialCache();
        SessionDebugLogger.EndSession("Play mode object destroyed");
    }

    private void Update()
    {
        bool shouldUpdateWaterEffects = ConsumeThrottledUpdate(ref waterEffectsUpdateTimer, GetWaterEffectsUpdateInterval());
        UpdateMainMenuHud(); UpdateGameStartAudioFade();
        if (isLoadingWorld || isMainMenuOpen)
        {
            SetEventFeedVisible(false);
            ClearEventFeedEntries();
            return;
        }

        float runtimeUpdateStartedRealtime = Time.realtimeSinceStartup;
        TrackRuntimeFrameGap(runtimeUpdateStartedRealtime);

        UpdateTutorialUi();
        if (isTutorialOpen && ShouldPauseSimulationForTutorial())
        {
            return;
        }
        if (isRacingActive)
        {
            SetEventFeedVisible(false);
            UpdateRacingMinigame();
            return;
        }
        UpdateJoinRaceButton();
        if (UpdateJoinRaceButtonInputFallback())
        {
            return;
        }

        bool blockPlayerInputForOverlay = isTutorialOpen || isCitySocialRequestSceneOpen || isBarInteriorSceneOpen || IsNoosphereDiveInputBlocking() || IsNoosphereVisionInputBlocking();
        if (!blockPlayerInputForOverlay)
        {
            HandleHotkeys();
            bool buildCancelledByRightClick = TryHandleBuildMenuRightClickCancel();
            if (!buildCancelledByRightClick)
            {
                HandleCameraInput();
                HandleRoadRemovalInput();
                HandleRoadPlacementInput();
            }
            UpdateBuildHoverHighlight();
        }

        UpdateGridLineVisualState();
        UpdateWaterVisualLod();
        UpdateCameraVisualLod();
        ProduceForestWood();
        UpdateSawmillProcessing();
        UpdateFurnitureFactoryProcessing();
        UpdateWeather(Time.deltaTime);
        UpdateDayNightCycle();
        UpdateNightSky();
        UpdateSelectedLocationLabel();
        UpdateSelectedEntityHighlight();
        UpdateRoadAccessWarningMarkerRuntime();
        if (!isFarZoomVisualLodActive)
        {
            UpdateForestTreeWobbles();
            UpdateMiscTreeSways();
            UpdateMiscBirds();
            UpdateAmbientCats();
            UpdateAmbientDogs();
            UpdateAmbientSquirrels();
            UpdateAmbientBees();
            UpdateAmbientLanternMoths();
            UpdateAmbientFallingLeaves();
            UpdateAmbientFireflies();
            UpdateAmbientFrogs();
            UpdateRiverFish();
            UpdateLakeFish();
        }
        if (shouldUpdateWaterEffects)
        {
            UpdateWaterEffects();
        }
        UpdateHiringDriverArrival();
        for (int i = 0; i < busAgents.Count; i++)
        {
            UpdatePurchasedBusArrival(busAgents[i], Time.deltaTime);
        }

        UpdateLocalBusRoute();
        UpdateEdgeHighwayBuses();
        UpdateRiverBoats();
        UpdateDistantClouds();
        if (!isFarZoomVisualLodActive)
        {
            UpdateAmbientAirParticles();
            UpdateExhaustSmoke();
        }
        UpdateMoneyPopups();
        UpdateForestWorkers();
        UpdateTradeSimulation();
        UpdateLaborExchangeRuntime();
        foreach (DriverAgent driver in driverAgents)
        {
            UpdateWorkerNeedsClock(driver);
        }
        UpdateHourlyNeedsEconomyTelemetry();
        UpdateWorkerSocialDecay();
        UpdateWorkerPersonalMemoryExpiry();
        UpdateWorkerKnowledgeFormationRuntime();
        UpdateWorkerThoughtFormationRuntime();
        UpdateWorkerFamilyRuntime();
        UpdateWorkerMigrationRuntime();
        UpdateImportedBuildingInteractions(Time.deltaTime * gameSpeedMultiplier);

        for (int i = 0; i < truckAgents.Count; i++)
        {
            TruckAgent ta = truckAgents[i];
            if (UpdatePurchasedTruckArrival(ta, Time.deltaTime))
            {
                UpdateTruckCargoVisual(ta);
                continue;
            }

            if (ShouldSkipTruckRuntimeForTrade(ta))
            {
                continue;
            }

            DriverAgent da = ta.Driver;
            LoadTruckState(ta);
            UpdateTruckMovement();
            UpdateTruckInteraction();
            UpdateAssignedTrip(da);   // award trip money BEFORE salary deduction
            UpdateRefuelOrder(da);
            UpdateDriverShiftEnd(ta, da);
            UpdateDriverShiftActivation(da);
            UpdateIdleRecall(da);
            UpdateDriverPersonalCarTrip(da);
            UpdateDriverWalk(da);
            UpdateDriverRest(da);
            UpdateDriverVisualAnimation(da);
            UpdateTruckAutoMode();

            if (!isTruckMoving &&
                !isTruckInteracting &&
                !isDriverRescueActive &&
                (da == null || da.RestPhase == DriverRestPhase.None) &&
                locations.TryGetValue(LocationType.Parking, out LocationData parking) &&
                truckCell == parking.Anchor)
            {
                Vector3 parkedPosition = GetParkingSlotWorldPosition(ta.ParkingSlotIndex);
                truckObject.transform.position = parkedPosition;
                truckTargetWorld = parkedPosition;
                truckSegmentStartWorld = parkedPosition;
                truckObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }

            SaveTruckState(ta);
            UpdateTruckCargoVisual(ta);
        }

        UpdateAudio();

        foreach (DriverAgent driver in driverAgents)
        {
            if (GetCurrentTruckForDriver(driver) != null)
            {
                continue;
            }

            UpdateDriverShiftPreparation(driver);
            UpdateDriverShiftActivation(driver);
            UpdateBusDriverShiftEnd(driver);
            UpdateLogisticsShiftEnd(driver);
            UpdateStreetCleaningWorker(driver);
            UpdateWarehouseDelivery(driver);
            UpdateDriverRest(driver);
            UpdateDriverIdleWander(driver);
            UpdateDriverPersonalCarTrip(driver);
            UpdateDriverWalk(driver);
            UpdateDriverVisualAnimation(driver);
            UpdateDriverFlashlight(driver, currentStylizedDaylight);
        }

        SeparateOverlappingDrivers();
        UpdateStreetLitterRuntime();
        UpdateCityHallRuntime();
        UpdateCityHallRequestMarkerRuntime();
        UpdateWorkerIdleDialogueRuntime();
        UpdateMoneyPopup();
        UpdateFleetScreenUi();
        UpdateDriversScreenUi();
        UpdateShiftsScreenUi();
        UpdateResourcesScreenUi();
        UpdateEconomyScreenUi();
        UpdateTradeScreenUi();
        UpdateBuildScreenUi();
        UpdateWorldMapScreenUi();
        UpdateStatesScreenUi();
        UpdateSocialGraphScreenUi();
        UpdateCityHallScreenUi();
        UpdateNoosphereScreenUi();
        UpdateNoosphereDiveRuntime();
        UpdateNoosphereVisualsRuntime();
        UpdateNoosphereVisionRuntime();
        UpdateEventFeedUi();
        UpdateTutorialGoalsRuntime();
        UpdateCityRequestGoalHudRuntime();
        UpdateCitySocialRequestSceneHudRuntime();
        UpdateBarInteriorSceneRuntime();
        CloseQuickHudsWhenBlockingHudIsOpen();
        UpdateTruckQuickHud();
        UpdateLocalBusQuickHud();
        UpdateDriverQuickHud();
        UpdateBuildingQuickHud();
        UpdateCellQuickHud(); UpdateWorkerPortraitAnimationExpressions();
        UpdateRuntimeLocalizationTick();
        TrackRuntimeUpdateDuration(runtimeUpdateStartedRealtime);
        SessionDebugLogger.FlushIfIntervalElapsed();
    }

    private static bool ConsumeThrottledUpdate(ref float accumulator, float interval)
    {
        accumulator += Time.deltaTime;
        if (accumulator < interval)
        {
            return false;
        }

        accumulator = Mathf.Repeat(accumulator, interval);
        return true;
    }

    private void UpdateWaterVisualLod()
    {
        int targetLod = GetWaterVisualLodLevel();
        if (targetLod == waterVisualLodLevel)
        {
            return;
        }

        waterVisualLodLevel = targetLod;
        ApplyWaterVisualLod(targetLod);
    }

    private float GetWaterEffectsUpdateInterval()
    {
        return waterVisualLodLevel >= 2 || isFarZoomVisualLodActive
            ? WaterEffectsFarUpdateInterval
            : WaterEffectsUpdateInterval;
    }

    private int GetWaterVisualLodLevel()
    {
        float cameraHeight = truckObject != null && isTruckCameraFocused
            ? mainCamera != null ? mainCamera.transform.position.y : CameraMinHeight
            : cameraOffset.y;

        if (cameraHeight >= WaterLodFarCameraHeight)
        {
            return 2;
        }

        if (cameraHeight >= WaterLodMediumCameraHeight)
        {
            return 1;
        }

        return 0;
    }

    private void UpdateCameraVisualLod()
    {
        if (isRacingActive || isTruckCameraFocused)
        {
            SetFarZoomVisualLod(false);
            return;
        }

        float cameraHeight = mainCamera != null
            ? mainCamera.transform.position.y
            : cameraOffset.y;
        bool shouldUseFarLod = isFarZoomVisualLodActive
            ? cameraHeight >= FarZoomVisualLodExitHeight
            : cameraHeight >= FarZoomVisualLodEnterHeight;

        SetFarZoomVisualLod(shouldUseFarLod);
    }

    private void SetFarZoomVisualLod(bool active)
    {
        if (isFarZoomVisualLodActive == active)
        {
            return;
        }

        isFarZoomVisualLodActive = active;
        SetRootActive(gridLinesRoot, !active && (isBuildPanelOpen || IsBuildingBuildTool(activeBuildTool) || IsRoadBuildTool(activeBuildTool)));
        SetRootActive(ambientAirRoot, !active);
        SetRootActive(ambientFallingLeafRoot, !active);
        SetRootActive(miscBirdRoot, !active);
        SetRootActive(ambientCatRoot, !active);
        SetRootActive(ambientDogRoot, !active);
        SetRootActive(ambientSquirrelRoot, !active);
        SetRootActive(ambientBeeRoot, !active);
        SetRootActive(ambientLanternMothRoot, !active);
        SetRootActive(ambientFireflyRoot, !active);
        SetRootActive(ambientFrogRoot, !active);
        SetRootActive(riverFishRoot, !active);
        SetRootActive(lakeFishRoot, !active);
        SetRootActive(exhaustSmokeRoot, !active);
        SetRootActive(truckDirtDustRoot, !active);

        ApplyShadowVisualLod(active);
        ApplyRoadLanternVisualLod(active);
    }

    private static void SetRootActive(Transform root, bool active)
    {
        if (root != null && root.gameObject.activeSelf != active)
        {
            root.gameObject.SetActive(active);
        }
    }

    private void RegisterShadowLodRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        shadowLodRenderers.Add(new ShadowLodRendererData
        {
            Renderer = renderer,
            OriginalShadowMode = renderer.shadowCastingMode
        });

        if (isFarZoomVisualLodActive)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    private void ApplyShadowVisualLod(bool farLodActive)
    {
        for (int i = shadowLodRenderers.Count - 1; i >= 0; i--)
        {
            ShadowLodRendererData entry = shadowLodRenderers[i];
            if (entry?.Renderer == null)
            {
                shadowLodRenderers.RemoveAt(i);
                continue;
            }

            entry.Renderer.shadowCastingMode = farLodActive
                ? ShadowCastingMode.Off
                : entry.OriginalShadowMode;
        }
    }

    private void ApplyRoadLanternVisualLod(bool farLodActive)
    {
        // Road lanterns are a core night readability cue. Keep their real
        // lights under far-zoom LOD; other expensive ambient layers are
        // still culled by SetFarZoomVisualLod().
    }

    private void OnGUI()
    {
        if (isMainMenuOpen)
        {
            return;
        }

        if (isCitySocialRequestSceneOpen || isBarInteriorSceneOpen) { DrawDayTitleCinematic(); return; }

        try
        {
            Color prevColor = GUI.color;
            bool prevEnabled = GUI.enabled;

            if (isTradePanelOpen)
            {
                DrawTradePolicyHud();
                DrawDayTitleCinematic();
                GUI.color = prevColor;
                GUI.enabled = prevEnabled;
                return;
            }

            if (!isRacingActive && !isWorldMapPanelOpen && !IsNoosphereVisionInputBlocking())
            {
                DrawMoneyHud();
                DrawCityTrustHud();
                DrawPopulationHud();
                DrawTimeHud();
                DrawSpeedHud();
                DrawWeatherHud();
                DrawPauseOverlay();
                DrawMenuBar();
                DrawBuildModeLegend();
                if (isFleetPanelOpen) DrawFleetPanel();
            }
            // Shifts panel is now Canvas-based (ShiftsScreenCanvas)
            // Drivers panel is now Canvas-based (DriversScreenCanvas)
            // Resources panel is now Canvas-based (ResourcesScreenCanvas)
            // Economy panel is now Canvas-based (EconomyScreenCanvas)
            // Build panel is now Canvas-based (BuildScreenCanvas)

            DrawDebugServicePanel();
            DrawDayTitleCinematic();

            GUI.color = prevColor;
            GUI.enabled = prevEnabled;
        }
        catch (System.Exception ex)
        {
            SessionDebugLogger.Log("GUI", $"OnGUI exception: {ex.Message}");
            GUI.color = Color.white;
            GUI.enabled = true;
        }
    }

    private void DrawBuildModeLegend()
    {
        if (!IsBuildingBuildTool(activeBuildTool) && !IsRoadBuildTool(activeBuildTool))
        {
            return;
        }

        bool isRoadTool = IsRoadBuildTool(activeBuildTool);
        Rect rect = isRoadTool
            ? new Rect(Screen.width - 302f, Screen.height - 112f, 272f, 82f)
            : new Rect(Screen.width - 242f, Screen.height - 86f, 212f, 56f);
        Color prevColor = GUI.color;
        Color prevContentColor = GUI.contentColor;

        try
        {
            GUI.color = new Color(0.05f, 0.07f, 0.1f, 0.82f);
            GUI.Box(rect, string.Empty);

            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            GUIStyle legendStyle = new(GUI.skin.label)
            {
                fontSize = isRoadTool ? 15 : 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            legendStyle.normal.textColor = Color.white;
            legendStyle.hover.textColor = Color.white;
            legendStyle.active.textColor = Color.white;
            legendStyle.focused.textColor = Color.white;

            string text = isRoadTool
                ? $"{L("R - rotate")}\n{L("Shift - drag road")}"
                : L("R - rotate");

            GUI.Label(
                new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, rect.height - 24f),
                text,
                legendStyle);
        }
        finally
        {
            GUI.color = prevColor;
            GUI.contentColor = prevContentColor;
        }
    }

    private void SetupCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.transform.position = DioramaCameraOffset;
        mainCamera.transform.rotation = GetDioramaCameraRotation();
        mainCamera.fieldOfView = 30f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 220f;
        mainCamera.backgroundColor = new Color(0.82f, 0.9f, 0.97f);
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        cameraFocusPoint = new Vector3(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
        cameraOffset = DioramaCameraOffset;
        cameraTargetOffset = DioramaCameraOffset;
        isCameraWheelZoomSmoothing = false;
    }

    private void SetupLighting()
    {
        Light keyLight = FindAnyObjectByType<Light>();
        if (keyLight == null)
        {
            keyLight = new GameObject("Directional Light").AddComponent<Light>();
        }

        mainDirectionalLight = keyLight;
        keyLight.type = LightType.Directional;
        keyLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        keyLight.color = new Color(1f, 0.90f, 0.72f);
        keyLight.intensity = 1.28f;
        keyLight.shadows = LightShadows.Soft;
        keyLight.shadowStrength = 0.92f;
        keyLight.shadowBias = 0.038f;
        keyLight.shadowNormalBias = 0.28f;
        keyLight.shadowNearPlane = 0.2f;

        Light[] allLights = FindObjectsByType<Light>();
        foreach (Light lightComponent in allLights)
        {
            lightComponent.enabled = lightComponent == keyLight;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.82f, 0.86f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.64f, 0.63f, 0.58f);
        RenderSettings.ambientGroundColor = new Color(0.33f, 0.3f, 0.26f);
        RenderSettings.ambientLight = new Color(0.72f, 0.72f, 0.68f);
    }

    private void UpdateDayNightCycle(float deltaTime = -1f)
    {
        if (mainDirectionalLight == null || mainCamera == null)
        {
            return;
        }

        float cycleDeltaTime = deltaTime >= 0f ? deltaTime : Time.deltaTime;
        int endedDay = currentDay;
        bool didWrapDay = dayNightCycleTimer + cycleDeltaTime >= DayNightCycleDuration;
        if (didWrapDay) currentDay++;
        dayNightCycleTimer = Mathf.Repeat(dayNightCycleTimer + cycleDeltaTime, DayNightCycleDuration);
        if (didWrapDay)
        {
            FinalizeWorkerDailyOpinionsForDay(endedDay);
            CollectDailyBuildingTaxes();
            TickWorkerAging();
            RecordNoosphereDayStartSnapshot(NoosphereDayStartSnapshotTrigger.DayStart);
            ShowDayTitleCinematic(currentDay);
        }
        float normalizedTime = dayNightCycleTimer / DayNightCycleDuration;
        float dayHour = normalizedTime * 24f;
        float sunriseBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.8f, 7.2f, dayHour));
        float sunsetBlend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(18f, 20.6f, dayHour));
        float daylight = Mathf.Clamp01(sunriseBlend * sunsetBlend);
        float stylizedDaylight = Mathf.SmoothStep(0.04f, 1f, daylight);
        currentStylizedDaylight = stylizedDaylight;

        float sunTravel = Mathf.Clamp01(Mathf.InverseLerp(4.5f, 20.5f, dayHour));
        float sunArc = Mathf.Sin(sunTravel * Mathf.PI);
        float lowSun = 1f - Mathf.SmoothStep(0.18f, 0.72f, sunArc);
        float sunPitch = Mathf.Lerp(14f, 68f, sunArc);
        float sunYaw = Mathf.Lerp(-62f, -8f, sunTravel);
        mainDirectionalLight.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0f);
        mainDirectionalLight.intensity = Mathf.Lerp(0.15f, 1.46f, stylizedDaylight) * Mathf.Lerp(0.8f, 1.05f, sunArc);
        mainDirectionalLight.color = Color.Lerp(
            new Color(0.56f, 0.53f, 0.66f),
            Color.Lerp(
                new Color(1f, 0.66f, 0.4f),
                new Color(1f, 0.90f, 0.76f),
                Mathf.SmoothStep(0f, 1f, sunArc)),
            stylizedDaylight);
        mainDirectionalLight.shadowStrength = Mathf.Lerp(0.82f, 0.97f, stylizedDaylight);
        mainDirectionalLight.shadowBias = Mathf.Lerp(0.065f, 0.028f, sunArc);
        mainDirectionalLight.shadowNormalBias = Mathf.Lerp(0.54f, 0.21f, sunArc);

        Color ambientSky = Color.Lerp(
            Color.Lerp(new Color(0.1f, 0.12f, 0.18f), new Color(0.56f, 0.34f, 0.28f), lowSun * daylight),
            new Color(0.8f, 0.88f, 0.96f),
            stylizedDaylight);
        Color ambientEquator = Color.Lerp(
            Color.Lerp(new Color(0.09f, 0.08f, 0.11f), new Color(0.34f, 0.24f, 0.2f), lowSun * daylight),
            new Color(0.68f, 0.66f, 0.58f),
            stylizedDaylight);
        Color ambientGround = Color.Lerp(
            new Color(0.06f, 0.055f, 0.07f),
            new Color(0.36f, 0.32f, 0.24f),
            Mathf.SmoothStep(0f, 1f, stylizedDaylight));

        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;
        RenderSettings.ambientLight = Color.Lerp(ambientEquator, ambientSky, 0.42f);

        Color horizonWarmth = Color.Lerp(new Color(0.18f, 0.20f, 0.28f), new Color(0.96f, 0.70f, 0.50f), lowSun * daylight);
        Color daytimeSky = Color.Lerp(new Color(0.52f, 0.72f, 0.92f), new Color(0.66f, 0.84f, 0.98f), sunArc);
        Color backgroundColor = Color.Lerp(horizonWarmth, daytimeSky, stylizedDaylight);
        mainCamera.backgroundColor = backgroundColor;

        float zoomT = Mathf.InverseLerp(CameraMinHeight, CameraMaxHeight, cameraOffset.y);
        float fogZoom = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 0.96f, zoomT));
        float weatherFogMult = Mathf.Clamp(activeWeatherParams.FogMult, 0.58f, 1.35f);
        RenderSettings.fog = fogZoom > 0.001f || weatherFogMult < 0.95f;
        RenderSettings.fogMode = FogMode.Linear;
        float hazeStrength = Mathf.Lerp(0.1f, 0.24f, fogZoom) * Mathf.Lerp(0.82f, 1.12f, lowSun);
        Color hazeColor = Color.Lerp(
            new Color(0.44f, 0.56f, 0.70f),
            Color.Lerp(new Color(0.98f, 0.82f, 0.66f), new Color(0.82f, 0.90f, 0.98f), sunArc),
            stylizedDaylight);
        Color fogColor = Color.Lerp(backgroundColor, hazeColor, hazeStrength);
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = Mathf.Max(46f, Mathf.Lerp(126f, 64f, fogZoom) * weatherFogMult);
        RenderSettings.fogEndDistance   = Mathf.Max(92f, Mathf.Lerp(186f, 98f, fogZoom) * weatherFogMult);

        UpdateDioramaPostProcessing(stylizedDaylight, lowSun, sunArc, backgroundColor);
        ApplyWeatherToPostProcessing(stylizedDaylight);
        UpdateLocationNightLights(stylizedDaylight);
        UpdateCellLightingRuntime(stylizedDaylight);

        for (int i = 0; i < truckAgents.Count; i++)
        {
            LoadTruckState(truckAgents[i]);
            UpdateTruckHeadlights(stylizedDaylight, truckAgents[i].Driver);
            SaveTruckState(truckAgents[i]);
        }
    }

    private void TickWorkerAging()
    {
        foreach (DriverAgent driver in driverAgents)
        {
            driver.DaysOnMap++;
            if (driver.DaysOnMap % 7 == 0)
            {
                driver.Age++;
                isDriversScreenDirty = true;
            }
        }
    }

}
