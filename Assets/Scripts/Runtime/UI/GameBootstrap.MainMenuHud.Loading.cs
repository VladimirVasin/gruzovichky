using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void SetupLoadingOverlay()
    {
        if (loadingOverlayCanvas != null) return;

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        loadingOverlayCanvas = new GameObject("LoadingOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = loadingOverlayCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;  // above main menu (90) so bar shows on top of it

        CanvasScaler scaler = loadingOverlayCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // No full-screen overlay - main menu stays visible underneath.
        Transform canvasRoot = loadingOverlayCanvas.transform;

        RectTransform barBg = CreateUiObject("LoadingBarBg", canvasRoot).GetComponent<RectTransform>();
        barBg.anchorMin = new Vector2(0f, 0f);
        barBg.anchorMax = new Vector2(1f, 0f);
        barBg.pivot = new Vector2(0.5f, 0f);
        barBg.anchoredPosition = new Vector2(0f, 0f);
        barBg.sizeDelta = new Vector2(0f, 14f);
        barBg.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 1f);

        RectTransform barFillRect = CreateUiObject("LoadingBarFill", barBg).GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = new Vector2(0f, 1f);
        barFillRect.pivot = new Vector2(0f, 0.5f);
        barFillRect.anchoredPosition = Vector2.zero;
        barFillRect.sizeDelta = new Vector2(0f, 0f);
        loadingBarFill = barFillRect.gameObject.AddComponent<Image>();
        loadingBarFill.color = FleetPrimaryButtonColor;

        RectTransform labelRect = CreateUiObject("LoadingLabel", canvasRoot).GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 18f);
        labelRect.sizeDelta = new Vector2(0f, 22f);
        loadingStatusText = labelRect.gameObject.AddComponent<Text>();
        loadingStatusText.font = uiFont;
        loadingStatusText.fontSize = 14;
        loadingStatusText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        loadingStatusText.alignment = TextAnchor.MiddleCenter;
        loadingStatusText.text = L("Loading...");
    }

    private void SetLoadingProgress(float t, string status)
    {
        if (loadingBarFill == null) return;

        RectTransform rt = loadingBarFill.rectTransform;
        rt.anchorMax = new Vector2(Mathf.Clamp01(t), 1f);
        if (loadingStatusText != null) loadingStatusText.text = L(status);
    }

    private IEnumerator BuildPrototypeSceneAsync()
    {
        SessionDebugLogger.Log("BOOT", $"Building world for {selectedGameStartMode} mode.");

        isFarZoomVisualLodActive = false;
        shadowLodRenderers.Clear();
        gridLinesRoot = null;
        ResetSquirrelMemorialWorldState();
        ResetFootpathSystem();
        ResetStreetLitterSystem();
        ResetCityUpgrades();
        worldRoot = new GameObject("PrototypeWorld").transform;
        roadsRoot = new GameObject("Roads").transform;
        roadsRoot.SetParent(worldRoot, false);
        lanternsRoot = new GameObject("RoadLanterns").transform;
        lanternsRoot.SetParent(worldRoot, false);
        roadsidePropsRoot = new GameObject("RoadsideProps").transform;
        roadsidePropsRoot.SetParent(worldRoot, false);
        roadsideSignsRoot = new GameObject("RoadsideSigns").transform;
        roadsideSignsRoot.SetParent(worldRoot, false);
        miscRoot = new GameObject("Misc").transform;
        miscRoot.SetParent(worldRoot, false);

        const int totalSteps = 26;
        int step = 0;

        SetLoadingProgress(++step / (float)totalSteps, "Camera & lighting..."); yield return null;
        SetupCamera(); SetupLighting(); SetupDioramaPostProcessing(); SetupSurfaceMaterials();

        SetLoadingProgress(++step / (float)totalSteps, "Populating water..."); yield return null;
        GenerateNaturalZones(); PopulateWaterCells();

        SetLoadingProgress(++step / (float)totalSteps, "Regional map..."); yield return null;
        GenerateRegionalMapState();

        SetLoadingProgress(++step / (float)totalSteps, "Setting up locations..."); yield return null;
        SetupLocations();

        SetLoadingProgress(++step / (float)totalSteps, "Building road network..."); yield return null;
        GenerateInitialRoadNetwork();

        SetLoadingProgress(++step / (float)totalSteps, "Generating terrain..."); yield return null;
        GenerateTerrainHeights();

        SetLoadingProgress(++step / (float)totalSteps, "Smoothing terrain..."); yield return null;
        FlattenTerrainNearWater();

        SetLoadingProgress(++step / (float)totalSteps, "Applying terrain..."); yield return null;
        ApplyTerrainHeightsToWorld();
        RebuildUnifiedRoadVisuals();

        SetLoadingProgress(++step / (float)totalSteps, "Setting up ground..."); yield return null;
        yield return StartCoroutine(SetupGroundAsync()); CreateWaterLayer();

        SetLoadingProgress(++step / (float)totalSteps, "Building grid..."); yield return null;
        yield return StartCoroutine(SetupGridAsync());

        SetLoadingProgress(++step / (float)totalSteps, "Edge highways..."); yield return null;
        SetupEdgeHighway(); SetupEdgeHighwayBuses(); SetupLocalBusRuntime(); SetupRiverBoats();

        SetLoadingProgress(++step / (float)totalSteps, "Atmosphere..."); yield return null;
        SetupDistantClouds(); SetupAmbientAirParticles(); SetupExhaustSmoke();

        SetLoadingProgress(++step / (float)totalSteps, "Road lanterns..."); yield return null;
        RebuildRoadLanterns();

        SetLoadingProgress(++step / (float)totalSteps, "Planting trees..."); yield return null;
        PopulateMiscTrees(); PopulateLakeDecorations();

        SetLoadingProgress(++step / (float)totalSteps, "Placing benches..."); yield return null;
        RebuildRoadsideBenches();

        SetLoadingProgress(++step / (float)totalSteps, "Road signs..."); yield return null;
        RebuildRoadSigns();

        SetLoadingProgress(++step / (float)totalSteps, "Wildlife..."); yield return null;
        SetupMiscBirds(); SetupAmbientCats(); SetupAmbientSquirrels(); SetupAmbientBees(); SetupAmbientLanternMoths(); SetupAmbientFallingLeaves(); SetupAmbientFireflies(); SetupAmbientFrogs(); SetupRiverFish(); SetupLakeFish(); SetupNightSky(); SetupWeatherSystem();

        SetLoadingProgress(++step / (float)totalSteps, "Water effects..."); yield return null;
        waterVisualLodLevel = -1; UpdateWaterVisualLod();

        SetLoadingProgress(++step / (float)totalSteps, "Visual tools..."); yield return null;
        SetupBuildHoverHighlight(); SetupForestWorkers(); SetupSelectionVisuals();

        SetLoadingProgress(++step / (float)totalSteps, "Vehicles..."); yield return null;
        SetupTruck();
        money = StartingTreasury;
        SetupCargoTransferVisual();

        SetLoadingProgress(++step / (float)totalSteps, "Audio..."); yield return null;
        SetupAudio();

        SetLoadingProgress(++step / (float)totalSteps, "Fleet UI..."); yield return null;
        SetupFleetScreenUi(); SetupDriversScreenUi(); SetupShiftsScreenUi();

        SetLoadingProgress(++step / (float)totalSteps, "Economy UI..."); yield return null;
        SetupResourcesScreenUi(); SetupEconomyScreenUi(); SetupTradeScreenUi();

        SetLoadingProgress(++step / (float)totalSteps, "Build UI..."); yield return null;
        SetupBuildScreenUi(); SetupWorldMapScreenUi(); SetupStatesScreenUi(); SetupSocialGraphScreenUi(); SetupCityHallScreenUi();

        SetLoadingProgress(++step / (float)totalSteps, "HUD..."); yield return null;
        SetupTruckQuickHud(); SetupLocalBusQuickHud(); SetupDriverQuickHud(); SetupBuildingQuickHud(); SetupCellQuickHud();

        SetLoadingProgress(++step / (float)totalSteps, "Finishing up..."); yield return null;
        SetupMainMenuHud(); SetupJoinRaceButton();

        SetLoadingProgress(1f, "Done!"); yield return null;

        isWorldBuilt = true;
        isLoadingWorld = false;
        loadingOverlayCanvas.SetActive(false);

        SessionDebugLogger.Log("BOOT", $"Scene bootstrap complete. Mode={selectedGameStartMode}, Locations={locations.Count}, Roads={roadCells.Count}, Trucks={truckAgents.Count}.");
        FinishGameStart();
    }
}
