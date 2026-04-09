using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class DriverQuickHudRefs
    {
        public GameObject CanvasRoot;
        public RectTransform Root;
        public Text HeaderText;
        public Text StatusText;
        public Text TruckText;
        public Text ShiftText;
        public Text EnergyText;
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
        root.sizeDelta = new Vector2(300f, 220f);
        VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(16, 16, 16, 16);
        rootLayout.spacing = 12;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        driverQuickHud.Root = root;

        // Header row: name + close button
        RectTransform headerRow = CreateLayoutRow("DriverQuickHudHeader", root, 30f, 10f);
        driverQuickHud.HeaderText = CreateHeaderText("DriverName", headerRow, uiFont, "Driver", 21, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.HeaderText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        driverQuickHud.CloseButton = CreateButton("CloseBtn", headerRow, uiFont, out Text closeTxt, "X", 12, new Color(0.26f, 0.30f, 0.36f, 1f), Color.white);
        LayoutElement closeLayout = driverQuickHud.CloseButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 28f;
        closeLayout.preferredHeight = 28f;
        driverQuickHud.CloseButton.onClick.AddListener(ClearDriverFocus);

        // Info card
        RectTransform summaryCard = CreateSectionCard(root, uiFont, string.Empty, out RectTransform summaryBody, false);
        summaryCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 110f;

        driverQuickHud.StatusText = CreateBodyText("Status", summaryBody, uiFont, string.Empty, 17, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.StatusText.fontStyle = FontStyle.Bold;
        driverQuickHud.StatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        RectTransform statsGrid = CreateUiObject("StatsGrid", summaryBody).GetComponent<RectTransform>();
        GridLayoutGroup statsLayout = statsGrid.gameObject.AddComponent<GridLayoutGroup>();
        statsLayout.cellSize = new Vector2(120f, 26f);
        statsLayout.spacing = new Vector2(8f, 6f);
        statsLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        statsLayout.constraintCount = 2;
        statsGrid.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        driverQuickHud.TruckText   = CreateBodyText("TruckText",   statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.ShiftText   = CreateBodyText("ShiftText",   statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.EnergyText  = CreateBodyText("EnergyText",  statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.BalanceText = CreateBodyText("BalanceText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, FleetAccentColor);

        // Action row
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

        string statusLabel;
        if (driver.IsArrivingByBus)
            statusLabel = "Arriving by Bus";
        else if (driver.RestPhase == DriverRestPhase.Sleeping)
            statusLabel = "Sleeping";
        else if (driver.RestPhase != DriverRestPhase.None)
            statusLabel = "Walking";
        else if (driver.IsOnActiveShift)
            statusLabel = "On Shift";
        else if (driver.WaitingForShiftAtParking)
            statusLabel = "At Parking";
        else if (driver.WalkPhase == DriverRescuePhase.ToMotelFromBusStop)
            statusLabel = "Walking from Bus Stop";
        else if (driver.WalkPhase == DriverRescuePhase.IdleWander)
            statusLabel = "Wandering";
        else if (driver.ShiftStartHour >= 0)
            statusLabel = $"Shift at {driver.ShiftStartHour:00}:00";
        else
            statusLabel = "Idle";

        driverQuickHud.StatusText.text = statusLabel;
        driverQuickHud.TruckText.text  = FormatValueLine("Truck",   truck != null ? truck.DisplayName : "None");
        driverQuickHud.ShiftText.text  = FormatValueLine("Shift",   driver.ShiftStartHour >= 0 ? GetShiftRangeLabel(driver.ShiftStartHour) : "—");
        driverQuickHud.EnergyText.text = FormatValueLine("Energy",  $"{Mathf.CeilToInt(driver.Energy)} / {Mathf.CeilToInt(DriverEnergyMax)}");
        driverQuickHud.BalanceText.text = FormatValueLine("Balance", $"${driver.Money}");
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
