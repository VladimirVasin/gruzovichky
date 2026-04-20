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
        public Text StatusText;
        public Text TruckText;
        public Text ModeText;
        public Text ShiftText;
        public Text BalanceText;
        public Button OpenDriversButton;
        public Text OpenDriversButtonText;
        public Button CloseButton;
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
        root.sizeDelta = new Vector2(360f, 310f);
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

        RectTransform summaryCard = CreateSectionCard(root, uiFont, string.Empty, out RectTransform summaryBody, false);
        summaryCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 152f;

        driverQuickHud.StatusText = CreateBodyText("Status", summaryBody, uiFont, string.Empty, 17, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.StatusText.fontStyle = FontStyle.Bold;
        driverQuickHud.StatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        RectTransform statsGrid = CreateUiObject("StatsGrid", summaryBody).GetComponent<RectTransform>();
        GridLayoutGroup statsLayout = statsGrid.gameObject.AddComponent<GridLayoutGroup>();
        statsLayout.cellSize = new Vector2(288f, 24f);
        statsLayout.spacing = new Vector2(0f, 5f);
        statsLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        statsLayout.constraintCount = 1;
        statsGrid.gameObject.AddComponent<LayoutElement>().preferredHeight = 111f;
        driverQuickHud.TruckText = CreateBodyText("TruckText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.ModeText = CreateBodyText("ModeText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.ShiftText = CreateBodyText("ShiftText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.BalanceText = CreateBodyText("BalanceText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, FleetAccentColor);

        RectTransform actionRow = CreateLayoutRow("DriverQuickHudActions", root, 34f, 0f);
        driverQuickHud.OpenDriversButton = CreateButton("OpenDriversBtn", actionRow, uiFont, out driverQuickHud.OpenDriversButtonText, "Open Drivers", 13, FleetPrimaryButtonColor, Color.white);
        driverQuickHud.OpenDriversButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
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
            !isFleetPanelOpen &&
            !isDriversPanelOpen &&
            !isShiftsPanelOpen &&
            !isResourcesPanelOpen &&
            !isBuildPanelOpen;

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

        driverQuickHud.HeaderText.text = driver.DriverName;
        string occupationLabel = GetWorkerOccupationLabel(driver);
        driverQuickHud.OccupationText.text = L(occupationLabel);

        driverQuickHud.StatusText.text = GetDriverQuickHudStatusLabel(driver);
        driverQuickHud.TruckText.text = FormatValueLine("Truck", truck != null ? truck.DisplayName : "None");
        driverQuickHud.ModeText.text = FormatValueLine("Role", occupationLabel);
        driverQuickHud.ShiftText.text = FormatValueLine("Shift", driver.ShiftStartHour >= 0 ? GetShiftRangeLabel(driver.ShiftStartHour) : "—");
        driverQuickHud.BalanceText.text = FormatValueLine("Balance", $"${driver.Money}");
    }

    private string GetDriverQuickHudStatusLabel(DriverAgent driver)
    {
        if (IsDriverOnActiveTradeRun(driver))
            return L("On Trade Run");
        if (driver.IsArrivingByBus)
            return L("Arriving by Bus");
        if (driver.RestPhase == DriverRestPhase.Sleeping)
            return L("Sleeping");
        if (driver.RestPhase != DriverRestPhase.None)
            return L("Walking");
        if (IsDriverIntercity(driver))
            return L("Intercity");
        if (driver.IsOnActiveShift)
            return L("On Shift");
        if (driver.WaitingForShiftAtParking)
            return L("At Parking");
        if (driver.WalkPhase == DriverRescuePhase.ToMotelFromBusStop)
            return L("Walking from Bus Stop");
        if (driver.WalkPhase == DriverRescuePhase.IdleWander)
            return L("Wandering");
        if (driver.WalkPhase == DriverRescuePhase.IdleWalkToCanteen || driver.WalkPhase == DriverRescuePhase.IdleAtCanteen)
            return L("At Canteen");
        if (driver.ShiftStartHour >= 0)
            return string.Format(L("Shift at {0}:00"), driver.ShiftStartHour.ToString("00"));

        return L("Idle");
    }

    private void FocusDriver(int driverId)
    {
        selectedDriverId = driverId;
        isDriverDetailsOpen = true;
        isTruckDetailsOpen = false;
        selectedLocation = null;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        DriverAgent driver = driverAgents.Find(d => d.DriverId == driverId);
        if (driver != null)
            LogUiInput($"Selection: focused {driver.DriverName}");
        RefreshSelectionVisuals();
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }

    private string GetWorkerOccupationLabel(DriverAgent driver)
    {
        if (driver.DutyMode == DriverDutyMode.Intercity)
            return "Intercity Driver";

        if (driver.DutyMode == DriverDutyMode.Logistics && driver.AssignedBuildingType.HasValue)
        {
            return driver.AssignedBuildingType.Value switch
            {
                LocationType.Forest           => "Lumberjack",
                LocationType.Sawmill          => "Sawmill Worker",
                LocationType.FurnitureFactory => "Carpenter",
                LocationType.Warehouse        => "Warehouse Loader",
                _                             => "Production Worker"
            };
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
