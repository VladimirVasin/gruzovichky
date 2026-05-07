using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int CityHallComplaintRowCount = 10;

    private bool isCityHallPanelOpen;
    private bool isCityHallScreenDirty = true;
    private int selectedCityComplaintId;
    private CityHallScreenUiRefs cityHallScreenUi;

    private sealed class CityHallComplaintRowUi
    {
        public Button Button;
        public Image Background;
        public Image Accent;
        public Text TitleText;
        public Text MetaText;
        public int ComplaintId;
    }

    private sealed class CityHallScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text TitleText;
        public Text SummaryText;
        public Text EmptyText;
        public CityHallComplaintRowUi[] Rows;
        public Text DetailTitleText;
        public Text DetailMetaText;
        public Text DetailBodyText;
        public Button FocusWorkerButton;
        public Text FocusWorkerButtonText;
        public Button FocusTargetButton;
        public Text FocusTargetButtonText;
        public Button ResolveButton;
        public Text ResolveButtonText;
    }

    private void SetupCityHallScreenUi()
    {
        if (cityHallScreenUi != null)
        {
            return;
        }

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cityHallScreenUi = new CityHallScreenUiRefs();

        GameObject canvasObject = new("CityHallScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        cityHallScreenUi.CanvasRoot = canvasObject;

        RectTransform window = CreateStyledPanel("CityHallWindowRoot", canvasObject.transform, FleetPanelColor);
        SetCenteredWindow(window, 1040f, 650f, -12f);
        cityHallScreenUi.WindowRoot = window;

        VerticalLayoutGroup windowLayout = window.gameObject.AddComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(18, 18, 18, 18);
        windowLayout.spacing = 12f;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = true;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;

        cityHallScreenUi.TitleText = CreateHeaderText("CityHallTitle", window, font, string.Empty, 24, TextAnchor.MiddleLeft, Color.white);
        cityHallScreenUi.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        cityHallScreenUi.SummaryText = CreateBodyText("CityHallSummary", window, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        cityHallScreenUi.SummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

        RectTransform bodyRow = CreateUiObject("CityHallBody", window).GetComponent<RectTransform>();
        bodyRow.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        HorizontalLayoutGroup bodyLayout = bodyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 14f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = true;

        RectTransform listPanel = CreateStyledPanel("CityHallListPanel", bodyRow, FleetInsetColor);
        LayoutElement listLayout = listPanel.gameObject.AddComponent<LayoutElement>();
        listLayout.preferredWidth = 520f;
        listLayout.minWidth = 520f;
        listLayout.flexibleHeight = 1f;
        VerticalLayoutGroup listGroup = listPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        listGroup.padding = new RectOffset(12, 12, 12, 12);
        listGroup.spacing = 8f;
        listGroup.childControlWidth = true;
        listGroup.childControlHeight = true;
        listGroup.childForceExpandWidth = true;
        listGroup.childForceExpandHeight = false;

        Text listTitle = CreateHeaderText("CityHallListTitle", listPanel, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        listTitle.text = IsRussianLanguage() ? "\u0416\u0430\u043b\u043e\u0431\u044b \u0433\u043e\u0440\u043e\u0436\u0430\u043d" : "Citizen complaints";
        listTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        cityHallScreenUi.EmptyText = CreateBodyText("CityHallEmpty", listPanel, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetMutedTextColor);
        cityHallScreenUi.EmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

        cityHallScreenUi.Rows = new CityHallComplaintRowUi[CityHallComplaintRowCount];
        for (int i = 0; i < CityHallComplaintRowCount; i++)
        {
            cityHallScreenUi.Rows[i] = CreateCityHallComplaintRow(listPanel, font, i);
        }

        RectTransform detailPanel = CreateStyledPanel("CityHallDetailPanel", bodyRow, FleetPanelColor);
        LayoutElement detailLayout = detailPanel.gameObject.AddComponent<LayoutElement>();
        detailLayout.flexibleWidth = 1f;
        detailLayout.flexibleHeight = 1f;
        VerticalLayoutGroup detailGroup = detailPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        detailGroup.padding = new RectOffset(16, 16, 16, 16);
        detailGroup.spacing = 10f;
        detailGroup.childControlWidth = true;
        detailGroup.childControlHeight = true;
        detailGroup.childForceExpandWidth = true;
        detailGroup.childForceExpandHeight = false;

        cityHallScreenUi.DetailTitleText = CreateHeaderText("CityHallDetailTitle", detailPanel, font, string.Empty, 20, TextAnchor.MiddleLeft, Color.white);
        cityHallScreenUi.DetailTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        cityHallScreenUi.DetailMetaText = CreateBodyText("CityHallDetailMeta", detailPanel, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetAccentColor);
        cityHallScreenUi.DetailMetaText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        cityHallScreenUi.DetailBodyText = CreateBodyText("CityHallDetailBody", detailPanel, font, string.Empty, 13, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        cityHallScreenUi.DetailBodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 250f;

        RectTransform actionRow = CreateLayoutRow("CityHallActions", detailPanel, 38f, 8f);
        cityHallScreenUi.FocusWorkerButton = CreateButton("CityHallFocusWorker", actionRow, font, out cityHallScreenUi.FocusWorkerButtonText, string.Empty, 12, new Color(0.25f, 0.33f, 0.46f, 1f), Color.white);
        cityHallScreenUi.FocusWorkerButton.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        cityHallScreenUi.FocusWorkerButton.onClick.AddListener(FocusSelectedCityComplaintWorker);

        cityHallScreenUi.FocusTargetButton = CreateButton("CityHallFocusTarget", actionRow, font, out cityHallScreenUi.FocusTargetButtonText, string.Empty, 12, new Color(0.24f, 0.38f, 0.24f, 1f), Color.white);
        cityHallScreenUi.FocusTargetButton.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        cityHallScreenUi.FocusTargetButton.onClick.AddListener(FocusSelectedCityComplaintTarget);

        cityHallScreenUi.ResolveButton = CreateButton("CityHallResolve", detailPanel, font, out cityHallScreenUi.ResolveButtonText, string.Empty, 13, FleetPrimaryButtonColor, Color.white);
        cityHallScreenUi.ResolveButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
        cityHallScreenUi.ResolveButton.onClick.AddListener(ResolveSelectedCityComplaint);

        AddOverlayCloseButton(window, font);
        ValidateCityHallScreenClickTargets();
        cityHallScreenUi.CanvasRoot.SetActive(false);
        UpdateCityHallScreenUi();
    }

    private void ValidateCityHallScreenClickTargets()
    {
        if (cityHallScreenUi?.CanvasRoot == null)
        {
            return;
        }

        bool ok = cityHallScreenUi.CanvasRoot.GetComponent<GraphicRaycaster>() != null &&
                  IsButtonClickTargetReady(cityHallScreenUi.FocusWorkerButton) &&
                  IsButtonClickTargetReady(cityHallScreenUi.FocusTargetButton) &&
                  IsButtonClickTargetReady(cityHallScreenUi.ResolveButton);

        if (cityHallScreenUi.Rows != null)
        {
            for (int i = 0; i < cityHallScreenUi.Rows.Length; i++)
            {
                ok &= IsButtonClickTargetReady(cityHallScreenUi.Rows[i]?.Button);
            }
        }

        if (!ok)
        {
            SessionDebugLogger.Log("UI_INPUT", "City Hall click-target validation failed: check GraphicRaycaster, Button targetGraphic, and row buttons.");
        }
    }

    private static bool IsButtonClickTargetReady(Button button)
    {
        return button != null &&
               button.targetGraphic != null &&
               button.onClick != null;
    }

    private CityHallComplaintRowUi CreateCityHallComplaintRow(RectTransform parent, Font font, int rowIndex)
    {
        RectTransform rowRoot = CreateHorizontalLayoutPanel(
            $"CityComplaintRow{rowIndex}",
            parent,
            FleetCardMutedColor,
            new RectOffset(0, 10, 0, 0),
            8f,
            preferredHeight: 52f,
            flexibleHeight: 0f,
            childForceExpandHeight: true,
            addOutline: false);

        Image bg = rowRoot.GetComponent<Image>();
        Button button = rowRoot.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        int capturedIndex = rowIndex;
        button.onClick.AddListener(() => SelectCityHallComplaintRow(capturedIndex));

        RectTransform accent = CreateUiObject("Accent", rowRoot).GetComponent<RectTransform>();
        Image accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = FleetAccentColor;
        accentImage.raycastTarget = false;
        accent.gameObject.AddComponent<LayoutElement>().preferredWidth = 8f;

        RectTransform texts = CreateVerticalStack("Texts", rowRoot, new RectOffset(0, 0, 6, 5), 2f, flexibleWidth: 1f);
        Text title = CreateBodyText("Title", texts, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        title.fontStyle = FontStyle.Bold;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
        Text meta = CreateBodyText("Meta", texts, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        meta.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        return new CityHallComplaintRowUi
        {
            Button = button,
            Background = bg,
            Accent = accentImage,
            TitleText = title,
            MetaText = meta,
            ComplaintId = 0
        };
    }

    private void UpdateCityHallScreenUi()
    {
        if (cityHallScreenUi == null)
        {
            if (isCityHallPanelOpen)
            {
                SetupCityHallScreenUi();
            }

            return;
        }

        bool shouldShow = isCityHallPanelOpen;
        if (cityHallScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            cityHallScreenUi.CanvasRoot.SetActive(shouldShow);
            if (shouldShow)
            {
                isCityHallScreenDirty = true;
            }
        }

        if (!shouldShow || !isCityHallScreenDirty)
        {
            return;
        }

        RebuildCityHallScreen();
        isCityHallScreenDirty = false;
    }

    private void RebuildCityHallScreen()
    {
        bool ru = IsRussianLanguage();
        List<CityComplaintRowViewModel> rows = BuildCityHallComplaintRows(ru);
        if (selectedCityComplaintId <= 0 || GetCityComplaintById(selectedCityComplaintId) == null)
        {
            selectedCityComplaintId = rows.Count > 0 ? rows[0].Id : 0;
        }

        cityHallScreenUi.TitleText.text = ru ? "\u0420\u0430\u0442\u0443\u0448\u0430" : "City Hall";
        cityHallScreenUi.SummaryText.text = ru
            ? $"\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u0438\u0435 \u0436\u0430\u043b\u043e\u0431\u044b: \u043e\u0442\u043a\u0440\u044b\u0442\u043e {CountOpenCityComplaints()}, \u043a\u0440\u0438\u0442\u0438\u0447\u043d\u044b\u0445 {CountCriticalCityComplaints()}, \u043f\u0440\u043e\u0441\u0440\u043e\u0447\u0435\u043d\u043e {CountExpiredCityComplaints()}, \u0440\u0435\u0448\u0435\u043d\u043e \u0441\u0435\u0433\u043e\u0434\u043d\u044f {CountResolvedCityComplaintsToday()}.\n\u041f\u0440\u043e\u0431\u043b\u0435\u043c\u044b \u0441\u043d\u0430\u0447\u0430\u043b\u0430 \u043a\u043e\u043f\u044f\u0442\u0441\u044f, \u0437\u0430\u0442\u0435\u043c \u043f\u043e\u0434\u0430\u044e\u0442\u0441\u044f \u043a\u0430\u043a \u043e\u0434\u043d\u0430 \u0436\u0430\u043b\u043e\u0431\u0430 \u0441\u043e \u0441\u043f\u0438\u0441\u043a\u043e\u043c \u043f\u043e\u0434\u043f\u0438\u0441\u0430\u0432\u0448\u0438\u0445."
            : $"City complaints: open {CountOpenCityComplaints()}, critical {CountCriticalCityComplaints()}, expired {CountExpiredCityComplaints()}, resolved today {CountResolvedCityComplaintsToday()}.\nProblems accumulate first, then become one complaint with a signer list.";

        cityHallScreenUi.EmptyText.gameObject.SetActive(rows.Count == 0);
        cityHallScreenUi.EmptyText.text = ru
            ? "\u041f\u043e\u043a\u0430 \u0436\u0430\u043b\u043e\u0431 \u043d\u0435\u0442. \u0413\u043e\u0440\u043e\u0436\u0430\u043d\u0435 \u043d\u0430\u0447\u043d\u0443\u0442 \u043f\u043e\u0434\u0430\u0432\u0430\u0442\u044c \u0438\u0445, \u043a\u043e\u0433\u0434\u0430 \u0438\u0445 \u043d\u0443\u0436\u0434\u044b \u0441\u0442\u0430\u043d\u0443\u0442 \u0437\u0430\u043c\u0435\u0442\u043d\u044b\u043c\u0438."
            : "No complaints yet. Citizens will file them when their needs become visible.";

        for (int i = 0; i < cityHallScreenUi.Rows.Length; i++)
        {
            CityHallComplaintRowUi row = cityHallScreenUi.Rows[i];
            bool active = i < rows.Count;
            row.Button.gameObject.SetActive(active);
            if (!active)
            {
                row.ComplaintId = 0;
                continue;
            }

            CityComplaintRowViewModel data = rows[i];
            row.ComplaintId = data.Id;
            row.TitleText.text = data.Title;
            row.MetaText.text = data.Meta;
            row.Accent.color = data.AccentColor;
            row.Background.color = data.Id == selectedCityComplaintId
                ? new Color(0.24f, 0.30f, 0.40f, 1f)
                : data.State == CityComplaintState.Resolved
                    ? new Color(0.12f, 0.17f, 0.15f, 1f)
                    : FleetCardMutedColor;
            row.TitleText.color = data.State == CityComplaintState.Resolved ? FleetSecondaryTextColor : Color.white;
        }

        UpdateCityHallComplaintDetail(rows, ru);
        LocalizeCanvas(cityHallScreenUi.CanvasRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(cityHallScreenUi.WindowRoot);
    }

    private void UpdateCityHallComplaintDetail(List<CityComplaintRowViewModel> rows, bool ru)
    {
        CityComplaint complaint = GetCityComplaintById(selectedCityComplaintId);
        CityComplaintRowViewModel row = null;
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Id == selectedCityComplaintId)
            {
                row = rows[i];
                break;
            }
        }

        if (complaint == null || row == null)
        {
            cityHallScreenUi.DetailTitleText.text = ru ? "\u041d\u0435\u0442 \u0436\u0430\u043b\u043e\u0431" : "No complaint selected";
            cityHallScreenUi.DetailMetaText.text = string.Empty;
            cityHallScreenUi.DetailBodyText.text = ru
                ? "\u0412\u044b\u0431\u0435\u0440\u0438 \u0436\u0430\u043b\u043e\u0431\u0443 \u0441\u043b\u0435\u0432\u0430, \u0447\u0442\u043e\u0431\u044b \u0443\u0432\u0438\u0434\u0435\u0442\u044c \u043f\u0440\u0438\u0447\u0438\u043d\u0443."
                : "Select a complaint on the left to see the reason.";
            SetCityHallDetailButtons(null, ru);
            return;
        }

        cityHallScreenUi.DetailTitleText.text = row.Title;
        cityHallScreenUi.DetailMetaText.text = row.Meta;
        cityHallScreenUi.DetailBodyText.text = row.Detail;
        SetCityHallDetailButtons(complaint, ru);
    }

    private void SetCityHallDetailButtons(CityComplaint complaint, bool ru)
    {
        bool hasComplaint = complaint != null;
        bool isOpen = hasComplaint && complaint.State == CityComplaintState.Open;
        DriverAgent worker = hasComplaint ? GetDriverAgentById(complaint.WorkerId) : null;
        bool canFocusTarget = hasComplaint &&
                              ((complaint.LinkedLocationType.HasValue && locations.ContainsKey(complaint.LinkedLocationType.Value)) ||
                               locations.ContainsKey(LocationType.CityHall));

        cityHallScreenUi.FocusWorkerButtonText.text = ru ? "\u041a \u043f\u043e\u0434\u043f\u0438\u0441\u0430\u043d\u0442\u0443" : "Focus signer";
        cityHallScreenUi.FocusWorkerButton.interactable = worker != null;
        cityHallScreenUi.FocusTargetButtonText.text = ru ? "\u041a \u0446\u0435\u043b\u0438" : "Focus target";
        cityHallScreenUi.FocusTargetButton.interactable = canFocusTarget;
        cityHallScreenUi.ResolveButtonText.text = ru ? "\u041e\u0442\u043c\u0435\u0442\u0438\u0442\u044c \u0440\u0430\u0437\u043e\u0431\u0440\u0430\u043d\u043d\u043e\u0439" : "Mark reviewed";
        cityHallScreenUi.ResolveButton.interactable = isOpen;
    }

    private void SelectCityHallComplaintRow(int rowIndex)
    {
        if (cityHallScreenUi?.Rows == null || rowIndex < 0 || rowIndex >= cityHallScreenUi.Rows.Length)
        {
            return;
        }

        int complaintId = cityHallScreenUi.Rows[rowIndex].ComplaintId;
        if (complaintId <= 0)
        {
            return;
        }

        selectedCityComplaintId = complaintId;
        isCityHallScreenDirty = true;
        PlayUiSound(uiSelectClip, 0.72f);
    }

    private void FocusSelectedCityComplaintWorker()
    {
        CityComplaint complaint = GetCityComplaintById(selectedCityComplaintId);
        if (complaint == null)
        {
            return;
        }

        isCityHallPanelOpen = false;
        isCityHallScreenDirty = true;
        FocusWorkerFromQuickHud(complaint.WorkerId, "city hall complaint");
    }

    private void FocusSelectedCityComplaintTarget()
    {
        CityComplaint complaint = GetCityComplaintById(selectedCityComplaintId);
        if (complaint == null)
        {
            return;
        }

        LocationType target = complaint.LinkedLocationType.HasValue && locations.ContainsKey(complaint.LinkedLocationType.Value)
            ? complaint.LinkedLocationType.Value
            : LocationType.CityHall;
        if (!locations.ContainsKey(target))
        {
            return;
        }

        isCityHallPanelOpen = false;
        isCityHallScreenDirty = true;
        selectedLocation = target;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        isTruckDetailsOpen = false;
        isLocalBusDetailsOpen = false;
        isDriverDetailsOpen = false;
        FocusCameraOnWorldPosition(GetLocationCenter(locations[target]));
        RefreshSelectionVisuals();
        PlayUiSound(uiSelectClip, 0.75f);
    }

    private void ResolveSelectedCityComplaint()
    {
        if (ResolveCityComplaintManually(selectedCityComplaintId))
        {
            PlayUiSound(uiSelectClip, 0.75f);
            RebuildCityHallScreen();
        }
    }
}
