using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class DriverQuickHudRefs
    {
        public GameObject CanvasRoot;
        public RectTransform Root;
        public Text HeaderText;
        public Text OccupationText;
        public RectTransform PortraitRoot;
        public Text ProfileText;
        public Text PlaceText;
        public Text StatusText;
        public RectTransform NeedsMealBarFill;
        public RectTransform NeedsSleepBarFill;
        public RectTransform NeedsLeisureBarFill;
        public Text NeedsText;
        public GameObject TruckRow;
        public Text TruckText;
        public Text ShiftText;
        public Text HomeText;
        public Text CarText;
        public Text BalanceText;
        public Text PerksText;
        public Button OpenDriversButton;
        public Text OpenDriversButtonText;
        public Button CloseButton;
        public int PortraitDriverId = -1;
    }

    private DriverQuickHudRefs driverQuickHud;
    private int selectedDriverId;
    private bool isDriverDetailsOpen;

    private void SetupDriverQuickHud()
    {
        if (driverQuickHud != null) return;

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        driverQuickHud = new DriverQuickHudRefs();

        GameObject canvasObject = new("DriverQuickHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        driverQuickHud.CanvasRoot = canvasObject;

        RectTransform root = CreateStyledPanel("DriverQuickHudRoot", canvasObject.transform, FleetPanelColor);
        root.anchorMin = new Vector2(1f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-18f, 104f);
        root.sizeDelta = new Vector2(390f, 420f);
        root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(16, 16, 16, 16);
        rootLayout.spacing = 12;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        driverQuickHud.Root = root;

        RectTransform headerRow = CreateLayoutRow("DriverQuickHudHeader", root, 30f, 10f);
        driverQuickHud.HeaderText = CreateHeaderText("DriverName", headerRow, uiFont, "Driver", 21, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.HeaderText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        driverQuickHud.CloseButton = CreateButton("CloseBtn", headerRow, uiFont, out Text closeTxt, "X", 12, new Color(0.26f, 0.30f, 0.36f, 1f), Color.white);
        LayoutElement closeLayout = driverQuickHud.CloseButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 28f;
        closeLayout.preferredHeight = 28f;
        driverQuickHud.CloseButton.onClick.AddListener(ClearDriverFocus);

        driverQuickHud.OccupationText = CreateBodyText("OccupationText", root, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, new Color(0.55f, 0.65f, 0.80f, 1f));
        driverQuickHud.OccupationText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform profileRow = CreateLayoutRow("DriverQuickHudProfile", root, 58f, 10f);
        driverQuickHud.PortraitRoot = CreateUiObject("DriverPortrait", profileRow).GetComponent<RectTransform>();
        driverQuickHud.PortraitRoot.gameObject.AddComponent<RectMask2D>();
        driverQuickHud.PortraitRoot.gameObject.AddComponent<LayoutElement>().preferredWidth = 52f;

        RectTransform profileColumn = CreateUiObject("DriverProfileColumn", profileRow).GetComponent<RectTransform>();
        profileColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup profileLayout = profileColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        profileLayout.spacing = 4f;
        profileLayout.childControlWidth = true;
        profileLayout.childControlHeight = true;
        profileLayout.childForceExpandWidth = true;
        profileLayout.childForceExpandHeight = false;
        driverQuickHud.ProfileText = CreateBodyText("ProfileText", profileColumn, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        driverQuickHud.ProfileText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
        driverQuickHud.PlaceText = CreateBodyText("PlaceText", profileColumn, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.PlaceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        RectTransform summaryCard = CreateSectionCard(root, uiFont, string.Empty, out RectTransform summaryBody, false);
        summaryCard.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        driverQuickHud.StatusText = CreateBodyText("Status", summaryBody, uiFont, string.Empty, 17, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.StatusText.fontStyle = FontStyle.Bold;
        driverQuickHud.StatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        RectTransform needsBlock = CreateUiObject("NeedsBlock", summaryBody).GetComponent<RectTransform>();
        HorizontalLayoutGroup needsBlockLayout = needsBlock.gameObject.AddComponent<HorizontalLayoutGroup>();
        needsBlockLayout.spacing = 8f;
        needsBlockLayout.childAlignment = TextAnchor.MiddleLeft;
        needsBlockLayout.childControlWidth = false;
        needsBlockLayout.childControlHeight = false;
        needsBlockLayout.childForceExpandWidth = false;
        needsBlockLayout.childForceExpandHeight = false;
        needsBlock.gameObject.AddComponent<LayoutElement>().preferredHeight = 12f;
        driverQuickHud.NeedsMealBarFill    = CreateNeedsMiniBar(needsBlock, GetNeedsMealIcon(),    "DQHMeal",    80f);
        driverQuickHud.NeedsSleepBarFill   = CreateNeedsMiniBar(needsBlock, GetNeedsSleepIcon(),   "DQHSleep",   80f);
        driverQuickHud.NeedsLeisureBarFill = CreateNeedsMiniBar(needsBlock, GetNeedsLeisureIcon(), "DQHLeisure", 80f);
        driverQuickHud.NeedsText = CreateBodyText("NeedsText", summaryBody, uiFont, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        driverQuickHud.NeedsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        RectTransform statsGrid = CreateUiObject("StatsGrid", summaryBody).GetComponent<RectTransform>();
        VerticalLayoutGroup statsLayout = statsGrid.gameObject.AddComponent<VerticalLayoutGroup>();
        statsLayout.spacing = 5f;
        statsLayout.childControlWidth = true;
        statsLayout.childControlHeight = true;
        statsLayout.childForceExpandWidth = true;
        statsLayout.childForceExpandHeight = false;
        statsGrid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject truckRow = CreateUiObject("TruckRow", statsGrid);
        truckRow.AddComponent<LayoutElement>().preferredHeight = 24f;
        driverQuickHud.TruckRow = truckRow;
        driverQuickHud.TruckText = CreateBodyText("TruckText", truckRow.GetComponent<RectTransform>(), uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.TruckText.rectTransform.anchorMin = Vector2.zero;
        driverQuickHud.TruckText.rectTransform.anchorMax = Vector2.one;
        driverQuickHud.TruckText.rectTransform.offsetMin = Vector2.zero;
        driverQuickHud.TruckText.rectTransform.offsetMax = Vector2.zero;

        driverQuickHud.ShiftText = CreateBodyText("ShiftText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.ShiftText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        driverQuickHud.HomeText = CreateBodyText("HomeText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.HomeText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        driverQuickHud.CarText = CreateBodyText("CarText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.CarText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        driverQuickHud.BalanceText = CreateBodyText("BalanceText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, FleetAccentColor);
        driverQuickHud.BalanceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        driverQuickHud.PerksText = CreateBodyText("PerksText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        driverQuickHud.PerksText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        RectTransform actionRow = CreateLayoutRow("DriverQuickHudActions", root, 34f, 0f);
        driverQuickHud.OpenDriversButton = CreateButton("OpenDriversBtn", actionRow, uiFont, out driverQuickHud.OpenDriversButtonText, "Open Drivers", 13, FleetPrimaryButtonColor, Color.white);
        LayoutElement openDriversLayout = driverQuickHud.OpenDriversButton.gameObject.AddComponent<LayoutElement>();
        openDriversLayout.preferredHeight = 34f;
        openDriversLayout.flexibleWidth = 1f;
        driverQuickHud.OpenDriversButton.onClick.AddListener(() =>
        {
            if (selectedDriverId <= 0) return;
            LogUiInput($"Driver Quick HUD: opened Drivers for Driver #{selectedDriverId}");
            OpenDriversPanelForDriver(selectedDriverId);
            isDriversScreenDirty = true;
        });

        driverQuickHud.CanvasRoot.SetActive(false);
        UpdateDriverQuickHud();
    }

    private void UpdateDriverQuickHud()
    {
        if (driverQuickHud == null) return;

        bool shouldShow =
            isDriverDetailsOpen &&
            !HasBlockingHudOpenForQuickHuds();

        if (driverQuickHud.CanvasRoot.activeSelf != shouldShow)
            driverQuickHud.CanvasRoot.SetActive(shouldShow);

        if (!shouldShow) return;

        DriverAgent driver = driverAgents.Find(d => d.DriverId == selectedDriverId);
        if (driver == null)
        {
            driverQuickHud.CanvasRoot.SetActive(false);
            return;
        }

        TruckAgent truck = GetAssignedTruckForDriver(driver);
        bool ru = IsRussianLanguage();

        driverQuickHud.HeaderText.text = driver.DriverName;
        string occupationLabel = GetWorkerOccupationLabel(driver);
        driverQuickHud.OccupationText.text = L(occupationLabel);
        if (driverQuickHud.PortraitDriverId != driver.DriverId)
        {
            driverQuickHud.PortraitDriverId = driver.DriverId;
            DrawWorkerPortraitScaled(driver, driverQuickHud.PortraitRoot, 0.50f);
        }
        driverQuickHud.ProfileText.text = $"{GetWorkerGenderLabel(driver, ru)} | {GetWorkerEducationDisplayName(driver.Education, ru)} | {GetWorkerQuickHudAgeLabel(driver, ru)}";
        driverQuickHud.PlaceText.text = FormatValueLine(ru ? "\u0421\u0435\u0439\u0447\u0430\u0441" : "Now", GetWorkerQuickHudPlaceLabel(driver, ru));

        string migrationLabel = GetWorkerMigrationStatusLabel(driver, ru);
        driverQuickHud.StatusText.text = string.IsNullOrEmpty(migrationLabel)
            ? GetDriverQuickHudStatusLabel(driver)
            : $"{GetDriverQuickHudStatusLabel(driver)} | {migrationLabel} {driver.Satisfaction}";

        bool hasTruck = truck != null;
        driverQuickHud.TruckRow.SetActive(hasTruck);
        if (hasTruck)
            driverQuickHud.TruckText.text = FormatValueLine("Truck", truck.DisplayName);

        string shiftLabel = driver.DutyMode == DriverDutyMode.Logistics
            ? GetProductionWorkRangeLabel()
            : driver.ShiftStartHour >= 0 ? GetShiftRangeLabel(driver.ShiftStartHour) : "—";
        driverQuickHud.ShiftText.text = FormatValueLine("Shift", shiftLabel);
        driverQuickHud.HomeText.text = FormatValueLine(ru ? "\u0414\u043e\u043c" : "Home", GetWorkerQuickHudHomeLabel(driver));
        bool hasCar = driver.OwnedCarModelIndex >= 0 && driver.OwnedCarModelIndex < CarModelNames.Length;
        driverQuickHud.CarText.text = FormatValueLine(ru ? "\u0410\u0432\u0442\u043e" : "Car", hasCar ? CarModelNames[driver.OwnedCarModelIndex] : "—");
        driverQuickHud.BalanceText.text = FormatValueLine("Balance", $"${driver.Money}");
        driverQuickHud.PerksText.text =
            $"{FormatValueLine(ru ? "\u041f\u0440\u043e\u0444." : "Prof.", FormatWorkerProfessionalSummary(driver, ru))}\n" +
            $"{FormatValueLine(ru ? "\u041f\u0435\u0440\u043a\u0438" : "Perks", FormatWorkerPerksInline(driver, ru, 4))}";
        driverQuickHud.NeedsText.text = GetWorkerQuickHudNeedsLine(driver, ru);
        if (driverQuickHud.OpenDriversButtonText != null)
        {
            driverQuickHud.OpenDriversButtonText.text = ru ? "\u041e\u0442\u043a\u0440\u044b\u0442\u044c \u0432 \u0420\u0430\u0431\u043e\u0447\u0438\u0435" : "Open in Workers";
        }

        if (driverQuickHud.NeedsMealBarFill != null)
        {
            float mealPct    = Mathf.Clamp01(1f - driver.HoursSinceMeal    / WorkerMealCriticalHours);
            float sleepPct   = Mathf.Clamp01(1f - driver.HoursSinceSleep   / WorkerSleepCriticalHours);
            float leisurePct = Mathf.Clamp01(1f - driver.HoursSinceLeisure / WorkerLeisureCriticalHours);
            driverQuickHud.NeedsMealBarFill.sizeDelta    = new Vector2(mealPct    * 80f, 0f);
            driverQuickHud.NeedsSleepBarFill.sizeDelta   = new Vector2(sleepPct   * 80f, 0f);
            driverQuickHud.NeedsLeisureBarFill.sizeDelta = new Vector2(leisurePct * 80f, 0f);
            driverQuickHud.NeedsMealBarFill.GetComponent<Image>().color    = GetNeedBarColor(mealPct);
            driverQuickHud.NeedsSleepBarFill.GetComponent<Image>().color   = GetNeedBarColor(sleepPct);
            driverQuickHud.NeedsLeisureBarFill.GetComponent<Image>().color = GetNeedBarColor(leisurePct);
        }

    }

    private string GetDriverQuickHudStatusLabel(DriverAgent driver)
    {
        if (IsDriverOnActiveTradeRun(driver))
            return L("On Trade Run");
        if (driver.IsLeavingTown || driver.WalkPhase == DriverRescuePhase.ToIntercityStopForDeparture)
            return IsRussianLanguage() ? "\u0423\u0435\u0437\u0436\u0430\u0435\u0442" : "Leaving town";
        if (driver.IsArrivingByBus)
            return L("Arriving by Bus");
        if (driver.RestPhase == DriverRestPhase.Sleeping || driver.RestPhase == DriverRestPhase.SleepingAtHome)
            return L("Sleeping");
        if (driver.RestPhase != DriverRestPhase.None)
            return L("Walking");
        if (IsBusDriverOnActiveRoute(driver))
            return IsRussianLanguage()
                ? $"{L("On Bus Route")} • Пассажиры {GetLocalBusPassengerCount(driver)}/{GetLocalBusPassengerCapacity()}"
                : $"{L("On Bus Route")} • Passengers {GetLocalBusPassengerCount(driver)}/{GetLocalBusPassengerCapacity()}";
        if (IsDriverBusDriver(driver) && driver.ShiftStartHour < 0)
            return L("Bus Driver: no shift");
        if (IsDriverIntercity(driver))
            return L("Intercity");
        if (driver.IsOnActiveShift)
            return L("On Shift");
        if (driver.WaitingForShiftAtParking)
            return L("At Parking");
        if (driver.WalkPhase == DriverRescuePhase.WalkToLocalBusStop)
            return IsRussianLanguage() ? $"Идёт к остановке #{driver.BusOriginStopNumber}" : $"Walking to Stop #{driver.BusOriginStopNumber}";
        if (driver.WalkPhase == DriverRescuePhase.WaitingAtLocalBusStop)
            return IsRussianLanguage() ? $"Ждёт автобус на остановке #{driver.BusOriginStopNumber}" : $"Waiting at Stop #{driver.BusOriginStopNumber}";
        if (driver.WalkPhase == DriverRescuePhase.RidingLocalBus)
            return IsRussianLanguage() ? $"Едет до остановки #{driver.BusDestinationStopNumber}" : $"Riding to Stop #{driver.BusDestinationStopNumber}";
        if (driver.WalkPhase == DriverRescuePhase.ToMotelFromBusStop)
            return L("Walking from Bus Stop");
        if (driver.WalkPhase == DriverRescuePhase.IdleWander)
            return L("Wandering");
        if (driver.WalkPhase == DriverRescuePhase.ToPersonalHouseMeal || driver.WalkPhase == DriverRescuePhase.IdleAtPersonalHouseMeal)
            return IsRussianLanguage() ? "Ест дома" : "Eating at home";
        if (driver.WalkPhase == DriverRescuePhase.IdleWalkToCanteen || driver.WalkPhase == DriverRescuePhase.IdleAtCanteen)
            return L("At Canteen");
        if (driver.WalkPhase == DriverRescuePhase.IdleWalkToTrashCan || driver.WalkPhase == DriverRescuePhase.IdleAtTrashCan)
            return IsRussianLanguage() ? "Ищет еду в мусорке" : "Eating from trash";
        if (driver.WalkPhase == DriverRescuePhase.IdleWalkToGamblingHall || driver.WalkPhase == DriverRescuePhase.IdleAtGamblingHall)
            return L("At Gambling Hall");
        if (driver.ShiftStartHour >= 0)
            return string.Format(L("Shift at {0}:00"), driver.ShiftStartHour.ToString("00"));

        return IsRussianLanguage() ? "Бездельничает" : "Idling";
    }

    private string GetWorkerQuickHudNeedsLine(DriverAgent driver, bool ru)
    {
        string meal = FormatWorkerNeedShort(ru ? "\u0415\u0434\u0430" : "Food", driver.HoursSinceMeal, WorkerMealCriticalHours);
        string sleep = FormatWorkerNeedShort(ru ? "\u0421\u043e\u043d" : "Sleep", driver.HoursSinceSleep, WorkerSleepCriticalHours);
        string leisure = FormatWorkerNeedShort(ru ? "\u0414\u043e\u0441\u0443\u0433" : "Leisure", driver.HoursSinceLeisure, WorkerLeisureCriticalHours);
        return $"{meal}\n{sleep}  |  {leisure}";
    }

    private static string FormatWorkerNeedShort(string label, float hoursSince, float criticalHours)
    {
        float remaining = Mathf.Max(0f, criticalHours - hoursSince);
        return $"{label}: {hoursSince:0.0}h / {criticalHours:0.0}h ({remaining:0.0}h left)";
    }

    private string GetWorkerQuickHudPlaceLabel(DriverAgent driver, bool ru)
    {
        if (driver.RestPhase == DriverRestPhase.SleepingAtHome)
        {
            return ru ? "\u0421\u043f\u0438\u0442 \u0434\u043e\u043c\u0430" : "Sleeping at home";
        }

        if (driver.RestPhase == DriverRestPhase.Sleeping)
        {
            return ru ? "\u0421\u043f\u0438\u0442 \u0432 Motel" : "Sleeping at Motel";
        }

        if (driver.WalkPhase == DriverRescuePhase.RidingLocalBus)
        {
            return driver.BusDestinationStopNumber > 0
                ? (ru ? $"\u0412 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0435 -> #{driver.BusDestinationStopNumber}" : $"On bus -> #{driver.BusDestinationStopNumber}")
                : (ru ? "\u0412 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0435" : "On bus");
        }

        LocationType? serviceLocation = GetDriverServiceLocation(driver.WalkPhase);
        if (serviceLocation.HasValue)
        {
            return GetSelectedLocationDisplayName(serviceLocation.Value);
        }

        if (driver.IsInsideBuilding && driver.AssignedBuildingType.HasValue)
        {
            return GetSelectedLocationDisplayName(driver.AssignedBuildingType.Value);
        }

        if (driver.WaitingForShiftAtParking)
        {
            return ru ? "\u041f\u0430\u0440\u043a\u043e\u0432\u043a\u0430" : "Parking";
        }

        if (driver.AssignedBuildingType.HasValue && driver.IsOnActiveShift)
        {
            return GetSelectedLocationDisplayName(driver.AssignedBuildingType.Value);
        }

        TruckAgent truck = GetAssignedTruckForDriver(driver);
        if (truck != null)
        {
            return truck.DisplayName;
        }

        return GetDriverQuickHudStatusLabel(driver);
    }

    private string GetWorkerQuickHudHomeLabel(DriverAgent driver)
    {
        return driver.AssignedPersonalHouseIndex >= 0 && driver.AssignedPersonalHouseIndex < personalHouses.Count
            ? personalHouses[driver.AssignedPersonalHouseIndex].Label
            : "\u2014";
    }

    private static string GetWorkerQuickHudAgeLabel(DriverAgent driver, bool ru)
    {
        if (driver.Age <= 0)
        {
            return "\u2014";
        }

        return ru ? $"{driver.Age} \u043b\u0435\u0442" : $"{driver.Age} y.o.";
    }

    private void FocusDriver(int driverId)
    {
        selectedDriverId = driverId;
        isDriverDetailsOpen = true;
        isTruckDetailsOpen = false;
        isLocalBusDetailsOpen = false;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        DriverAgent driver = driverAgents.Find(d => d.DriverId == driverId);
        if (driver != null)
        {
            if (TryFocusCameraOnDriver(driver, out string targetLabel))
            {
                LogUiInput($"Selection: focused {driver.DriverName} at {targetLabel}");
            }
            else
            {
                LogUiInput($"Selection: focused {driver.DriverName}");
            }
        }
        RefreshSelectionVisuals();
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }

    private string GetWorkerOccupationLabel(DriverAgent driver)
    {
        if (driver != null && driver.LifeGoal == WorkerLifeGoal.FindJob)
            return "Job Seeker";

        if (IsDriverBusDriver(driver))
            return "Bus Driver";

        if (driver.DutyMode == DriverDutyMode.Intercity)
            return "Intercity Driver";

        if (driver.DutyMode == DriverDutyMode.Logistics && driver.AssignedBuildingType.HasValue)
        {
            return GetBuildingWorkerRoleLabel(driver.AssignedBuildingType.Value);
        }

        if (driver.AssignedTruckNumber > 0)
            return "Truck Driver";

        return "Unemployed";
    }

    private void ClearDriverFocus()
    {
        if (isDriverDetailsOpen)
        {
            DriverAgent driver = driverAgents.Find(d => d.DriverId == selectedDriverId);
            if (driver != null)
                LogUiInput($"Selection: cleared {driver.DriverName}");
        }
        isDriverDetailsOpen = false;
        selectedDriverId = 0;
        isFleetScreenDirty = true;
        RefreshSelectionVisuals();
        PlayUiSound(uiPanelCloseClip, 0.82f);
    }
}


