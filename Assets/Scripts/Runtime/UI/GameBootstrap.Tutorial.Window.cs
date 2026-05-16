using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void ShowTutorialWindow(TutorialTrigger trigger, int stepNumber, string title, string body)
    {
        SetupTutorialUi();
        activeTutorialTrigger = trigger;
        isTutorialSideMode = isDriversPanelOpen;
        tutorialHud.BodyText.fontSize = 15;   // default; callers may override after this returns
        ApplyTutorialWindowLayout();
        tutorialHud.TitleText.text = L(title);
        bool isEasterEgg = trigger == TutorialTrigger.BeeEasterEgg;
        int stepCount = NewUserTutorialStepCount;
        tutorialHud.StepText.text = isEasterEgg ? string.Empty : $"{stepNumber}/{stepCount}";
        tutorialWindowFullText = L(body);
        tutorialWindowTypeTime = 0f;
        tutorialHud.BodyText.text = string.Empty;
        tutorialHud.SkipToggle.isOn = false;
        tutorialHud.SkipToggle.gameObject.SetActive(!isEasterEgg);
        isTutorialOpen = true;
        tutorialHud.CanvasRoot.SetActive(true);
        PlayUiSound(uiPanelOpenClip, 0.82f);
        SessionDebugLogger.Log("TUTORIAL", $"Shown tutorial window: {title} (side={isTutorialSideMode}).");
    }

    private void ShowBeeEasterEggHud()
    {
        isTutorialSideMode = false;
        ShowTutorialWindow(
            TutorialTrigger.BeeEasterEgg,
            0,
            "Bees",
            "\u0414\u0443\u0440\u0430\u0447\u043e\u043a, \u043d\u0435 \u043c\u0435\u0448\u0430\u0439 \u043f\u0447\u0435\u043b\u043a\u0430\u043c");
        if (tutorialHud?.WindowRect != null)
        {
            tutorialHud.WindowRect.sizeDelta = new Vector2(500f, 260f);
        }
        if (tutorialHud?.BodyPanelLayout != null)
        {
            tutorialHud.BodyPanelLayout.preferredHeight = 100f;
        }
    }

    private static readonly Color OverlayColorFull        = new(0.02f, 0.03f, 0.05f, 0.56f);
    private static readonly Color OverlayColorTransparent = new(0f, 0f, 0f, 0f);

    private void ApplyTutorialWindowLayout()
    {
        if (tutorialHud?.WindowRect == null) return;
        if (activeTutorialTrigger is TutorialTrigger.UserTruckPurchasedArrivalInfo
            or TutorialTrigger.UserWorkerHiringBusInfo
            or TutorialTrigger.UserTruckAssignedFreightInfo
            or TutorialTrigger.UserWorkersLeisureInfo
            or TutorialTrigger.UserBuildServiceBuildingsPrompt
            or TutorialTrigger.UserBarBuiltInfo
            or TutorialTrigger.UserCanteenBuiltInfo
            or TutorialTrigger.UserGasStationBuiltInfo
            or TutorialTrigger.UserGamblingHallBuiltInfo
            or TutorialTrigger.UserCityParkBuiltInfo
            or TutorialTrigger.UserWorkersOverviewInfo
            or TutorialTrigger.UserLocalTransportInfo
            or TutorialTrigger.UserLocalBusRoutesInfo
            or TutorialTrigger.UserEconomyTaxesInfo
            or TutorialTrigger.UserDemoCompleteInfo)
        {
            tutorialHud.WindowRect.sizeDelta        = new Vector2(520f, 330f);
            tutorialHud.WindowRect.anchoredPosition = activeTutorialTrigger == TutorialTrigger.UserWorkerHiringBusInfo
                ? new Vector2(350f, 126f)
                : new Vector2(260f, 92f);
            if (tutorialHud.BodyPanelLayout != null) tutorialHud.BodyPanelLayout.preferredHeight = 150f;
            if (tutorialHud.OverlayImage    != null) tutorialHud.OverlayImage.color = OverlayColorTransparent;
            tutorialBobTime = 0f;
            return;
        }

        if (isTutorialSideMode)
        {
            tutorialHud.WindowRect.sizeDelta        = new Vector2(360f, 290f);
            tutorialHud.WindowRect.anchoredPosition = new Vector2(TutorialSidePanelBaseX, 0f);
            if (tutorialHud.BodyPanelLayout != null) tutorialHud.BodyPanelLayout.preferredHeight = 148f;
            if (tutorialHud.OverlayImage    != null) tutorialHud.OverlayImage.color = OverlayColorTransparent;
            tutorialBobTime = 0f;
        }
        else
        {
            tutorialHud.WindowRect.sizeDelta        = new Vector2(580f, 370f);
            tutorialHud.WindowRect.anchoredPosition = Vector2.zero;
            if (tutorialHud.BodyPanelLayout != null) tutorialHud.BodyPanelLayout.preferredHeight = 190f;
            if (tutorialHud.OverlayImage    != null) tutorialHud.OverlayImage.color = OverlayColorFull;
        }
    }

    private void SetupTutorialUi()
    {
        if (tutorialHud != null)
        {
            return;
        }

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tutorialHud = new TutorialHudRefs();

        GameObject canvasObject = new("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        tutorialHud.CanvasRoot = canvasObject;

        RectTransform overlay = CreateStyledPanel("TutorialOverlay", canvasObject.transform, new Color(0.02f, 0.03f, 0.05f, 0.56f));
        StretchRect(overlay, 0f, 0f, 0f, 0f);
        tutorialHud.OverlayRoot  = overlay.gameObject;
        tutorialHud.OverlayImage = overlay.GetComponent<Image>();
        tutorialHud.OverlayImage.raycastTarget = false;   // window button handles its own input

        // Window stays a child of the overlay (same coordinate space = full canvas)
        RectTransform window = CreateStyledPanel("TutorialWindow", overlay, FleetPanelColor);
        SetCenteredWindow(window, 580f, 370f, 0f);
        tutorialHud.WindowRect = window;

        VerticalLayoutGroup layout = window.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 22, 22);
        layout.spacing = 14;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("TutorialHeaderRow", window, 34f, 12f);
        tutorialHud.TitleText = CreateHeaderText("TutorialTitle", headerRow, font, string.Empty, 24, TextAnchor.MiddleLeft, Color.white);
        LayoutElement titleLayout = tutorialHud.TitleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;
        titleLayout.preferredHeight = 34f;
        tutorialHud.StepText = CreateHeaderText("TutorialStep", headerRow, font, string.Empty, 14, TextAnchor.MiddleRight, FleetAccentColor);
        LayoutElement stepLayout = tutorialHud.StepText.gameObject.AddComponent<LayoutElement>();
        stepLayout.preferredWidth = 64f;
        stepLayout.preferredHeight = 34f;

        RectTransform bodyPanel = CreateStyledPanel("TutorialBodyPanel", window, FleetInsetColor);
        LayoutElement bodyLayoutElem = bodyPanel.gameObject.AddComponent<LayoutElement>();
        bodyLayoutElem.preferredHeight = 190f;
        tutorialHud.BodyPanelLayout = bodyLayoutElem;
        VerticalLayoutGroup bodyLayout = bodyPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.padding = new RectOffset(14, 14, 12, 12);
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = true;
        tutorialHud.BodyText = CreateBodyText("TutorialBody", bodyPanel, font, string.Empty, 15, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        tutorialHud.BodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        tutorialHud.BodyText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform actionRow = CreateLayoutRow("TutorialActionRow", window, 42f, 12f);
        tutorialHud.SkipToggle = CreateTutorialSkipToggle(actionRow, font);
        LayoutElement skipToggleLayout = tutorialHud.SkipToggle.gameObject.AddComponent<LayoutElement>();
        skipToggleLayout.flexibleWidth = 1f;
        skipToggleLayout.preferredHeight = 28f;

        tutorialHud.OkButton = CreateButton("TutorialOkButton", actionRow, font, out tutorialHud.OkButtonText, "OK", 14, FleetPrimaryButtonColor, Color.white);
        LayoutElement okLayout = tutorialHud.OkButton.gameObject.AddComponent<LayoutElement>();
        okLayout.preferredWidth = 120f;
        okLayout.preferredHeight = 34f;
        okLayout.flexibleWidth = 0f;
        tutorialHud.OkButtonText.raycastTarget = false;
        tutorialHud.OkButton.onClick.AddListener(CloseCurrentTutorialWindow);

        tutorialHud.CanvasRoot.SetActive(false);
    }

    private Toggle CreateTutorialSkipToggle(RectTransform parent, Font font)
    {
        RectTransform root = CreateUiObject("TutorialSkipToggle", parent).GetComponent<RectTransform>();
        HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Toggle toggle = root.gameObject.AddComponent<Toggle>();

        RectTransform box = CreateStyledPanel("CheckBox", root, new Color(0.12f, 0.15f, 0.20f, 1f));
        box.sizeDelta = new Vector2(18f, 18f);
        LayoutElement boxLayout = box.gameObject.AddComponent<LayoutElement>();
        boxLayout.preferredWidth = 18f;
        boxLayout.preferredHeight = 18f;
        boxLayout.minWidth = 18f;
        boxLayout.minHeight = 18f;
        boxLayout.flexibleWidth = 0f;
        boxLayout.flexibleHeight = 0f;
        Image boxImage = box.GetComponent<Image>();

        RectTransform check = CreateStyledPanel("CheckMark", box, FleetAccentColor);
        check.anchorMin = new Vector2(0.22f, 0.22f);
        check.anchorMax = new Vector2(0.78f, 0.78f);
        check.offsetMin = Vector2.zero;
        check.offsetMax = Vector2.zero;
        Image checkImage = check.GetComponent<Image>();
        checkImage.raycastTarget = false;

        tutorialHud.SkipToggleText = CreateBodyText("SkipLabel", root, font, "Skip tutorial", 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        tutorialHud.SkipToggleText.raycastTarget = false;
        LayoutElement labelLayout = tutorialHud.SkipToggleText.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredHeight = 24f;
        labelLayout.preferredWidth = 128f;
        labelLayout.flexibleWidth = 0f;

        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        toggle.isOn = false;
        return toggle;
    }

    private void CenterCameraOnWorldForTutorial()
    {
        if (mainCamera == null)
        {
            return;
        }

        isTruckCameraFocused = false;
        isCameraReturningToDiorama = false;
        isCameraRotatingToTarget = false;
        cameraFocusPoint = new Vector3(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
        cameraOffset = DioramaCameraOffset;
        cameraTargetOffset = TutorialForestZoomOffset;
        isCameraWheelZoomSmoothing = false;
        mainCamera.transform.position = cameraFocusPoint + cameraOffset;
        mainCamera.transform.rotation = GetDioramaCameraRotation();
    }

    private void FocusCameraOnUserStartStopForTutorial()
    {
        if (mainCamera == null)
        {
            return;
        }

        isTruckCameraFocused = false;
        isCameraReturningToDiorama = false;
        isCameraRotatingToTarget = false;

        Vector3 focus = new Vector3(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
        if (locations.TryGetValue(LocationType.IntercityStop, out LocationData stop))
        {
            focus = GetLocationCenter(stop);
            focus += new Vector3(-1.6f, 0f, -1.0f);
        }

        focus.y = 0f;
        tutorialCameraFocusTarget = focus;
        tutorialCameraFocusOffset = UserWelcomeCameraOffset;
        isTutorialCameraFocusActive = true;
        cameraTargetOffset = UserWelcomeCameraOffset;
        isCameraWheelZoomSmoothing = false;
        SessionDebugLogger.Log("TUTORIAL", $"Started smooth User welcome camera focus on start stop at ({focus.x:F1},{focus.z:F1}).");
    }

    private void CloseCurrentTutorialWindow()
    {
        if (tutorialHud == null)
        {
            return;
        }

        if (tutorialHud.SkipToggle != null && tutorialHud.SkipToggle.isOn)
        {
            ApplyTutorialSkippedState("player skip");
        }

        isTutorialOpen     = false;
        isTutorialSideMode = false;
        tutorialWindowFullText = string.Empty;
        tutorialWindowTypeTime = 0f;
        if (tutorialHud.SkipToggle != null)
        {
            tutorialHud.SkipToggle.gameObject.SetActive(true);
        }
        PlayUiSound(uiPanelCloseClip, 0.82f);

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserWelcome)
        {
            BeginCameraControlTutorialGoals();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserBuildRoadPrompt)
        {
            BeginRoadBuildTutorialGoals();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserCoreBuildingsPrompt)
        {
            BeginCoreBuildingsTutorialGoals();
            OpenBuildPanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserBuildLumberjackCampPrompt)
        {
            BeginLumberjackCampTutorialGoals();
            OpenBuildPanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserLumberjackCampBuiltInfo)
        {
            isBuildPanelOpen = false;
            isShiftsScreenDirty = true;
            LogUiInput("Tutorial: Lumberjack Camp assignment prompt closed; waiting for player to open Staffing.");
        }

        if (activeTutorialTrigger == TutorialTrigger.UserTruckPurchasedArrivalInfo)
        {
            tutorialCameraFollowTruck = null;
            isTutorialCameraFocusActive = false;
            isCameraReturningToDiorama = true;
        }

        if (activeTutorialTrigger == TutorialTrigger.UserWorkerHiringBusInfo)
        {
            tutorialCameraFollowHiringBus = false;
            isTutorialCameraFocusActive = false;
            isCameraReturningToDiorama = true;
            UnlockAllTutorialVacancies();
            SessionDebugLogger.Log("TUTORIAL", "Unlocked all vacancies after tutorial hiring bus info.");
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserBuildLaborExchangePrompt)
        {
            BeginLaborExchangeTutorialGoals();
            OpenBuildPanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserMigrationInfo)
        {
            BeginWorkerCardTutorialGoals();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserWarehouseLoadersInfo)
        {
            BeginWarehouseLoadersTutorialGoals();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserLocalTransportInfo)
        {
            BeginLocalTransportTutorialGoals();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserLocalBusRoutesInfo)
        {
            ScheduleTutorial(TutorialTrigger.UserEconomyTaxesInfo, 0.35f);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserEconomyTaxesInfo)
        {
            BeginEconomyTaxesTutorialGoals();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserTradeIntroInfo)
        {
            BeginRegionalMapTutorialGoals();
            OpenWorldMapPanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserTruckAssignedFreightInfo)
        {
            tutorialCameraFollowTruck = null;
            isTutorialCameraFocusActive = false;
            isCameraReturningToDiorama = true;
            ScheduleTutorial(TutorialTrigger.UserBuildLaborExchangePrompt, 0.35f);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserWorkersLeisureInfo)
        {
            ScheduleTutorial(TutorialTrigger.UserBuildServiceBuildingsPrompt, 0.25f);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserBuildServiceBuildingsPrompt)
        {
            BeginServiceBuildingsTutorialGoals();
            OpenBuildPanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserWorkersOverviewInfo)
        {
            ScheduleTutorial(TutorialTrigger.UserWarehouseLoadersInfo, 0.35f);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserDocksPrompt)
        {
            BeginDocksTutorialGoals();
            OpenBuildPanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserTradePolicyInfo)
        {
            BeginTradeSetupTutorialGoals();
            OpenTradePanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserLumberjackWorkerAssignedInfo)
        {
            ScheduleTutorial(TutorialTrigger.UserWorkerShiftInfo, 0.8f);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserWorkerShiftInfo)
        {
            ScheduleTutorial(TutorialTrigger.UserLogisticsSetupInfo, 0.2f);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserLogisticsSetupInfo)
        {
            UnlockTutorialTruckDriverVacancy();
            BeginBuyTruckTutorialGoals();
        }
    }

    private void ApplyTutorialSkippedState(string reason, bool unlockBuildTools = true)
    {
        isTutorialSkipped = true;
        pendingTutorialTrigger = null;
        pendingTutorialDelay = 0f;
        if (unlockBuildTools)
        {
            UnlockAllBuildTools();
        }
        UnlockAllTutorialVacancies();
        isTutorialCameraFocusActive = false;
        tutorialCameraFollowTruck = null;
        tutorialCameraFollowHiringBus = false;
        ResetTutorialGoalsForNewGame();
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        RefreshSelectionVisuals();
        SessionDebugLogger.Log("TUTORIAL", $"Tutorial skipped: {reason}.");
    }


}
