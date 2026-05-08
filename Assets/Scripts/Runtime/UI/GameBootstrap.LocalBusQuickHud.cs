using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class LocalBusPassengerRowUi
    {
        public RectTransform Root;
        public Button Button;
        public Text LabelText;
        public int CurrentDriverId = -1;
    }

    private sealed class LocalBusQuickHudRefs
    {
        public GameObject CanvasRoot;
        public RectTransform Root;
        public Text HeaderText;
        public Text StatusText;
        public Button DriverButton;
        public Text DriverButtonText;
        public Text PassengersText;
        public Text BankText;
        public Text StopText;
        public Text NextStopText;
        public Text RouteText;
        public Text ManifestText;
        public ScrollRect PassengerScroll;
        public RectTransform PassengerContent;
        public LocalBusPassengerRowUi[] PassengerRows;
        public Button CloseButton;
        public Text CloseButtonText;
    }

    private LocalBusQuickHudRefs localBusQuickHud;

    private void SetupLocalBusQuickHud()
    {
        if (localBusQuickHud != null)
        {
            return;
        }

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        localBusQuickHud = new LocalBusQuickHudRefs();

        GameObject canvasObject = new("LocalBusQuickHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        localBusQuickHud.CanvasRoot = canvasObject;

        RectTransform root = CreateStyledPanel("LocalBusQuickHudRoot", canvasObject.transform, FleetPanelColor);
        root.anchorMin = new Vector2(1f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-18f, 104f);
        root.sizeDelta = new Vector2(360f, 336f);
        root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(16, 16, 16, 16);
        rootLayout.spacing = 12f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        localBusQuickHud.Root = root;

        RectTransform headerRow = CreateLayoutRow("LocalBusQuickHudHeader", root, 30f, 10f);
        localBusQuickHud.HeaderText = CreateHeaderText("Header", headerRow, uiFont, "Local Bus", 21, TextAnchor.MiddleLeft, Color.white);
        localBusQuickHud.HeaderText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        localBusQuickHud.CloseButton = CreateButton("CloseButton", headerRow, uiFont, out Text closeTxt, "X", 12, new Color(0.26f, 0.30f, 0.36f, 1f), Color.white);
        localBusQuickHud.CloseButtonText = closeTxt;
        LayoutElement closeLayout = localBusQuickHud.CloseButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 28f;
        closeLayout.preferredHeight = 28f;
        localBusQuickHud.CloseButton.onClick.AddListener(ClearLocalBusFocus);

        RectTransform summaryCard = CreateSectionCard(root, uiFont, string.Empty, out RectTransform summaryBody, false);
        summaryCard.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        localBusQuickHud.StatusText = CreateBodyText("Status", summaryBody, uiFont, string.Empty, 17, TextAnchor.MiddleLeft, Color.white);
        localBusQuickHud.StatusText.fontStyle = FontStyle.Bold;
        localBusQuickHud.StatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        RectTransform statsGrid = CreateUiObject("StatsGrid", summaryBody).GetComponent<RectTransform>();
        VerticalLayoutGroup statsLayout = statsGrid.gameObject.AddComponent<VerticalLayoutGroup>();
        statsLayout.spacing = 5f;
        statsLayout.childControlWidth = true;
        statsLayout.childControlHeight = true;
        statsLayout.childForceExpandWidth = true;
        statsLayout.childForceExpandHeight = false;
        statsGrid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        localBusQuickHud.DriverButton = CreateButton("DriverButton", statsGrid, uiFont, out localBusQuickHud.DriverButtonText, string.Empty, 12, new Color(0.23f, 0.29f, 0.36f, 1f), Color.white);
        localBusQuickHud.DriverButtonText.alignment = TextAnchor.MiddleLeft;
        localBusQuickHud.DriverButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        localBusQuickHud.DriverButton.onClick.AddListener(() =>
        {
            if (localBusRoute?.Driver == null)
            {
                return;
            }

            FocusWorkerFromQuickHud(localBusRoute.Driver.DriverId, "local bus HUD");
        });
        localBusQuickHud.PassengersText = CreateBodyText("PassengersText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, FleetAccentColor);
        localBusQuickHud.PassengersText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        localBusQuickHud.BankText = CreateBodyText("BankText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, FleetAccentColor);
        localBusQuickHud.BankText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        localBusQuickHud.StopText = CreateBodyText("StopText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        localBusQuickHud.StopText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        localBusQuickHud.NextStopText = CreateBodyText("NextStopText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        localBusQuickHud.NextStopText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        localBusQuickHud.RouteText = CreateBodyText("RouteText", statsGrid, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        localBusQuickHud.RouteText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        localBusQuickHud.ManifestText = CreateBodyText("ManifestText", statsGrid, uiFont, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        LayoutElement manifestLayout = localBusQuickHud.ManifestText.gameObject.AddComponent<LayoutElement>();
        manifestLayout.preferredHeight = 24f;

        FleetCanvasUiFactory.ScrollPanelRefs passengerScroll = CreateVerticalScrollList("BusPassengerScroll", statsGrid, "BusPassengerContent", 4f, preferredHeight: 104f);
        localBusQuickHud.PassengerScroll = passengerScroll.ScrollRect;
        localBusQuickHud.PassengerContent = passengerScroll.Content;
        localBusQuickHud.PassengerRows = new LocalBusPassengerRowUi[0];

        localBusQuickHud.CanvasRoot.SetActive(false);
        UpdateLocalBusQuickHud();
    }

    private void UpdateLocalBusQuickHud()
    {
        if (localBusQuickHud == null)
        {
            return;
        }

        bool shouldShow =
            isLocalBusDetailsOpen &&
            !isFleetPanelOpen &&
            !isDriversPanelOpen &&
            !isShiftsPanelOpen &&
            !isResourcesPanelOpen &&
            !isEconomyPanelOpen &&
            !isTradePanelOpen &&
            !isBuildPanelOpen &&
            !isWorldMapPanelOpen &&
            !isStatesPanelOpen &&
            !isSocialGraphPanelOpen;

        if (localBusQuickHud.CanvasRoot.activeSelf != shouldShow)
        {
            localBusQuickHud.CanvasRoot.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            return;
        }

        if (localBusRoute?.RootTransform == null)
        {
            localBusQuickHud.CanvasRoot.SetActive(false);
            isLocalBusDetailsOpen = false;
            return;
        }

        bool ru = IsRussianLanguage();
        localBusQuickHud.HeaderText.text = ru ? "Городской автобус" : "Local Bus";
        localBusQuickHud.StatusText.text = GetLocalBusQuickHudStatusLabel();
        localBusQuickHud.DriverButtonText.text = FormatValueLine(ru ? "Водитель" : "Driver", localBusRoute.Driver != null ? localBusRoute.Driver.DriverName : "-");
        localBusQuickHud.DriverButton.interactable = localBusRoute.Driver != null;
        localBusQuickHud.PassengersText.text = FormatValueLine(ru ? "Пассажиры" : "Passengers", $"{localBusRoute.PassengerCount} / {localBusRoute.PassengerCapacity}");
        localBusQuickHud.BankText.text = FormatValueLine(ru ? "Касса автобуса" : "Bus Bank", $"${localBusRoute.Bank}");
        localBusQuickHud.StopText.text = FormatValueLine(ru ? "Остановка" : "Stop", GetLocalBusQuickHudStopLabel());
        localBusQuickHud.NextStopText.text = FormatValueLine(ru ? "Следующая" : "Next Stop", GetLocalBusQuickHudNextStopLabel());
        localBusQuickHud.RouteText.text = FormatValueLine(ru ? "Маршрут" : "Route", GetLocalBusQuickHudRouteLabel());
        localBusQuickHud.ManifestText.text = FormatValueLine(ru ? "В салоне" : "On Board", GetLocalBusQuickHudManifestLabel());
        UpdateLocalBusPassengerRows(ru);
    }

    private string GetLocalBusQuickHudStatusLabel()
    {
        if (localBusRoute == null)
        {
            return IsRussianLanguage() ? "Неактивен" : "Inactive";
        }

        return localBusRoute.Phase switch
        {
            LocalBusPhase.ParkedAwaitingShiftStart => IsRussianLanguage() ? "Ожидает старт смены" : "Awaiting shift start",
            LocalBusPhase.DrivingRoute => IsRussianLanguage() ? "На маршруте" : "On Route",
            LocalBusPhase.WaitingAtStop => IsRussianLanguage() ? "Стоит на остановке" : "Waiting at Stop",
            LocalBusPhase.ReturningToParking => IsRussianLanguage() ? "Возвращается на парковку" : "Returning to Parking",
            _ => IsRussianLanguage() ? "Неактивен" : "Inactive"
        };
    }

    private string GetLocalBusQuickHudStopLabel()
    {
        if (localBusRoute == null)
        {
            return "-";
        }

        List<LocationData> orderedStops = GetOrderedLocalStops();
        if (orderedStops.Count == 0)
        {
            return "-";
        }

        if (localBusRoute.CurrentStopIndex >= 0 && localBusRoute.CurrentStopIndex < orderedStops.Count)
        {
            return $"#{orderedStops[localBusRoute.CurrentStopIndex].StopNumber}";
        }

        if (localBusRoute.Phase == LocalBusPhase.ParkedAwaitingShiftStart || localBusRoute.Phase == LocalBusPhase.ReturningToParking)
        {
            return IsRussianLanguage() ? "Парковка" : "Parking";
        }

        return "-";
    }

    private string GetLocalBusQuickHudNextStopLabel()
    {
        if (localBusRoute == null)
        {
            return "-";
        }

        List<LocationData> orderedStops = GetOrderedLocalStops();
        if (orderedStops.Count == 0)
        {
            return localBusRoute.Phase == LocalBusPhase.ReturningToParking || localBusRoute.Phase == LocalBusPhase.ParkedAwaitingShiftStart
                ? (IsRussianLanguage() ? "Парковка" : "Parking")
                : "-";
        }

        if (localBusRoute.Phase == LocalBusPhase.ReturningToParking)
        {
            return IsRussianLanguage() ? "Парковка" : "Parking";
        }

        if (localBusRoute.Phase == LocalBusPhase.ParkedAwaitingShiftStart)
        {
            return $"#{orderedStops[0].StopNumber}";
        }

        if (localBusRoute.Phase == LocalBusPhase.DrivingRoute)
        {
            int destinationIndex = Mathf.Clamp(localBusRoute.CurrentStopIndex, 0, orderedStops.Count - 1);
            return $"#{orderedStops[destinationIndex].StopNumber}";
        }

        if (localBusRoute.Phase != LocalBusPhase.WaitingAtStop)
        {
            return "-";
        }

        if (orderedStops.Count == 1)
        {
            return IsRussianLanguage() ? "Парковка" : "Parking";
        }

        int currentIndex = Mathf.Clamp(localBusRoute.CurrentStopIndex, 0, orderedStops.Count - 1);
        int nextIndex;
        if (localBusRoute.TravelDirection > 0)
        {
            nextIndex = currentIndex >= orderedStops.Count - 1
                ? currentIndex - 1
                : currentIndex + 1;
        }
        else
        {
            nextIndex = currentIndex <= 0
                ? 1
                : currentIndex - 1;
        }

        if (nextIndex < 0 || nextIndex >= orderedStops.Count)
        {
            return IsRussianLanguage() ? "Парковка" : "Parking";
        }

        return $"#{orderedStops[nextIndex].StopNumber}";
    }

    private string GetLocalBusQuickHudRouteLabel()
    {
        if (localBusRoute == null)
        {
            return "-";
        }

        List<LocationData> orderedStops = GetOrderedLocalStops();
        if (orderedStops.Count == 0)
        {
            return "-";
        }

        string leg = localBusRoute.TravelDirection > 0
            ? (IsRussianLanguage() ? "вверх по линии" : "ascending")
            : (IsRussianLanguage() ? "обратно по линии" : "descending");
        return $"{orderedStops.Count} {(IsRussianLanguage() ? "ост." : "stops")} - {leg}";
    }

    private string GetLocalBusQuickHudManifestLabel()
    {
        if (localBusRoute == null || localBusRoute.PassengerCount <= 0)
        {
            return IsRussianLanguage() ? "никого" : "nobody";
        }

        int visiblePassengers = GetLocalBusPassengerDrivers().Count;
        if (visiblePassengers == 0)
        {
            return IsRussianLanguage() ? "пассажиры скрыты" : "passengers hidden";
        }

        return IsRussianLanguage()
            ? $"{visiblePassengers} \u0447\u0435\u043b."
            : $"{visiblePassengers} worker(s)";
    }

    private List<DriverAgent> GetLocalBusPassengerDrivers()
    {
        List<DriverAgent> passengers = new();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver == null || driver.WalkPhase != DriverRescuePhase.RidingLocalBus)
            {
                continue;
            }

            passengers.Add(driver);
        }

        return passengers;
    }

    private void UpdateLocalBusPassengerRows(bool ru)
    {
        if (localBusQuickHud?.PassengerRows == null)
        {
            return;
        }

        List<DriverAgent> passengers = GetLocalBusPassengerDrivers();
        EnsureLocalBusPassengerRowCount(passengers.Count);
        if (localBusQuickHud.PassengerScroll != null)
        {
            localBusQuickHud.PassengerScroll.vertical = passengers.Count > 3;
        }

        for (int i = 0; i < localBusQuickHud.PassengerRows.Length; i++)
        {
            LocalBusPassengerRowUi row = localBusQuickHud.PassengerRows[i];
            if (i >= passengers.Count)
            {
                row.Root.gameObject.SetActive(false);
                row.CurrentDriverId = -1;
                continue;
            }

            DriverAgent driver = passengers[i];
            row.Root.gameObject.SetActive(true);
            row.CurrentDriverId = driver.DriverId;
            string destinationLabel = driver.BusDestinationStopNumber > 0 ? $"#{driver.BusDestinationStopNumber}" : "-";
            row.LabelText.text = ru
                ? $"{driver.DriverName} -> \u043e\u0441\u0442. {destinationLabel}"
                : $"{driver.DriverName} -> stop {destinationLabel}";
        }
    }

    private void EnsureLocalBusPassengerRowCount(int requiredCount)
    {
        if (requiredCount <= localBusQuickHud.PassengerRows.Length)
        {
            return;
        }

        int oldCount = localBusQuickHud.PassengerRows.Length;
        System.Array.Resize(ref localBusQuickHud.PassengerRows, requiredCount);
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        for (int i = oldCount; i < requiredCount; i++)
        {
            localBusQuickHud.PassengerRows[i] = CreateLocalBusPassengerRow(i, uiFont);
        }
    }

    private LocalBusPassengerRowUi CreateLocalBusPassengerRow(int index, Font uiFont)
    {
        LocalBusPassengerRowUi row = new();
        row.Button = CreateButton($"BusPassenger{index}", localBusQuickHud.PassengerContent, uiFont, out row.LabelText, string.Empty, 11, new Color(0.14f, 0.18f, 0.25f, 1f), Color.white);
        row.LabelText.alignment = TextAnchor.MiddleLeft;
        row.Root = row.Button.GetComponent<RectTransform>();
        row.Root.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;
        int capturedIndex = index;
        row.Button.onClick.AddListener(() => OnLocalBusPassengerRowClick(capturedIndex));
        return row;
    }

    private void OnLocalBusPassengerRowClick(int rowIndex)
    {
        if (localBusQuickHud?.PassengerRows == null ||
            rowIndex < 0 ||
            rowIndex >= localBusQuickHud.PassengerRows.Length)
        {
            return;
        }

        FocusWorkerFromQuickHud(localBusQuickHud.PassengerRows[rowIndex].CurrentDriverId, "local bus passenger list");
    }

    private void FocusLocalBus()
    {
        if (localBusRoute?.RootTransform == null)
        {
            return;
        }

        isLocalBusDetailsOpen = true;
        isTruckDetailsOpen = false;
        isDriverDetailsOpen = false;
        selectedDriverId = 0;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        HideBuildingQuickHudSubmenuImmediate();
        LogUiInput("Selection: focused Local Bus");
        RefreshSelectionVisuals();
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }

    private void ClearLocalBusFocus()
    {
        if (isLocalBusDetailsOpen)
        {
            LogUiInput("Selection: cleared Local Bus");
        }

        isLocalBusDetailsOpen = false;
        RefreshSelectionVisuals();
        PlayUiSound(uiPanelCloseClip, 0.82f);
    }
}
