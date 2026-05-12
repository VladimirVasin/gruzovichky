using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int CityHallComplaintRowCount = 10;
    private const float CityHallRejectedRowDismissSeconds = 0.32f;

    private bool isCityHallPanelOpen;
    private bool isCityHallScreenDirty = true;
    private int selectedCityComplaintId;
    private bool isCityHallUpgradesTabActive;
    private int cityHallDismissingRejectedComplaintId;
    private float cityHallRejectedRowDismissTimer;
    private CityHallScreenUiRefs cityHallScreenUi;

    private sealed class CityHallComplaintRowUi
    {
        public Button Button;
        public CanvasGroup CanvasGroup;
        public LayoutElement LayoutElement;
        public Image Background;
        public Image Accent;
        public Text TitleText;
        public Text MetaText;
        public int ComplaintId;
    }

    private sealed partial class CityHallScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text TitleText;
        public Text SummaryText;
        public RectTransform RequestsRoot;
        public Button RequestsTabButton;
        public Text RequestsTabText;
        public Button UpgradesTabButton;
        public Text UpgradesTabText;
        public Text EmptyText;
        public CityHallComplaintRowUi[] Rows;
        public Text DetailTitleText;
        public Text DetailMetaText;
        public Text DetailBodyText;
        public RectTransform DecisionRow;
        public Button AcceptButton;
        public Text AcceptButtonText;
        public Button RejectButton;
        public Text RejectButtonText;
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
        cityHallScreenUi.SummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;

        RectTransform tabRow = CreateTabRow("CityHallTabRow", window, 38f, 8f);
        cityHallScreenUi.RequestsTabButton = CreateButton("CityHallRequestsTab", tabRow, font, out cityHallScreenUi.RequestsTabText, string.Empty, 13, FleetPrimaryButtonColor, Color.white);
        ConfigureCityHallTabButton(cityHallScreenUi.RequestsTabButton, cityHallScreenUi.RequestsTabText);
        cityHallScreenUi.RequestsTabButton.onClick.AddListener(() =>
        {
            isCityHallUpgradesTabActive = false;
            isCityHallScreenDirty = true;
            PlayUiSound(uiPanelOpenClip, 0.48f);
            UpdateCityHallScreenUi();
        });

        cityHallScreenUi.UpgradesTabButton = CreateButton("CityHallUpgradesTab", tabRow, font, out cityHallScreenUi.UpgradesTabText, string.Empty, 13, new Color(0.22f, 0.26f, 0.32f, 1f), Color.white);
        ConfigureCityHallTabButton(cityHallScreenUi.UpgradesTabButton, cityHallScreenUi.UpgradesTabText);
        cityHallScreenUi.UpgradesTabButton.onClick.AddListener(() =>
        {
            isCityHallUpgradesTabActive = true;
            isCityHallScreenDirty = true;
            PlayUiSound(uiPanelOpenClip, 0.48f);
            UpdateCityHallScreenUi();
        });

        RectTransform bodyRow = CreateUiObject("CityHallBody", window).GetComponent<RectTransform>();
        cityHallScreenUi.RequestsRoot = bodyRow;
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
        listTitle.text = IsRussianLanguage() ? "Обращения граждан" : "Citizen requests";
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

        RectTransform decisionRow = CreateLayoutRow("CityHallDecisionActions", detailPanel, 42f, 10f);
        cityHallScreenUi.DecisionRow = decisionRow;
        cityHallScreenUi.AcceptButton = CreateButton("CityHallAccept", decisionRow, font, out cityHallScreenUi.AcceptButtonText, string.Empty, 14, new Color(0.24f, 0.50f, 0.28f, 1f), Color.white);
        ConfigureCityHallDecisionButton(cityHallScreenUi.AcceptButton, cityHallScreenUi.AcceptButtonText);
        cityHallScreenUi.AcceptButton.onClick.AddListener(AcceptSelectedCityComplaint);

        cityHallScreenUi.RejectButton = CreateButton("CityHallReject", decisionRow, font, out cityHallScreenUi.RejectButtonText, string.Empty, 14, new Color(0.50f, 0.18f, 0.14f, 1f), Color.white);
        ConfigureCityHallDecisionButton(cityHallScreenUi.RejectButton, cityHallScreenUi.RejectButtonText);
        cityHallScreenUi.RejectButton.onClick.AddListener(RejectSelectedCityComplaint);

        SetupCityHallUpgradeTreeUi(window, font);
        AddOverlayCloseButton(window, font);
        EnsureCityHallDecisionButtonsClickable();
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
                  IsButtonClickTargetReady(cityHallScreenUi.AcceptButton) &&
                  IsButtonClickTargetReady(cityHallScreenUi.RejectButton) &&
                  IsButtonClickTargetReady(cityHallScreenUi.RequestsTabButton) &&
                  IsButtonClickTargetReady(cityHallScreenUi.UpgradesTabButton) &&
                  AreCityHallUpgradeClickTargetsReady();

        if (cityHallScreenUi.Rows != null)
        {
            for (int i = 0; i < cityHallScreenUi.Rows.Length; i++)
            {
                ok &= IsButtonClickTargetReady(cityHallScreenUi.Rows[i]?.Button);
            }
        }

        if (!ok)
        {
            SessionDebugLogger.Log("UI_INPUT", "City Hall click-target validation failed: check GraphicRaycaster, decision buttons, and row buttons.");
        }
    }

    private static bool IsButtonClickTargetReady(Button button)
    {
        return button != null &&
               button.targetGraphic != null &&
               button.targetGraphic.raycastTarget &&
               button.onClick != null;
    }

    private void EnsureCityHallDecisionButtonsClickable()
    {
        if (cityHallScreenUi == null)
        {
            return;
        }

        ConfigureCityHallDecisionButton(cityHallScreenUi.AcceptButton, cityHallScreenUi.AcceptButtonText);
        ConfigureCityHallDecisionButton(cityHallScreenUi.RejectButton, cityHallScreenUi.RejectButtonText);
        ConfigureCityHallTabButton(cityHallScreenUi.RequestsTabButton, cityHallScreenUi.RequestsTabText);
        ConfigureCityHallTabButton(cityHallScreenUi.UpgradesTabButton, cityHallScreenUi.UpgradesTabText);
        if (cityHallScreenUi.DecisionRow != null)
        {
            cityHallScreenUi.DecisionRow.SetAsLastSibling();
            LayoutElement layout = cityHallScreenUi.DecisionRow.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = 42f;
                layout.preferredHeight = 42f;
                layout.flexibleHeight = 0f;
            }
        }
    }

    private static void ConfigureCityHallDecisionButton(Button button, Text label)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
        }

        if (image != null)
        {
            image.raycastTarget = true;
            button.targetGraphic = image;
        }

        if (label != null)
        {
            label.raycastTarget = false;
        }

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        layout.minWidth = 170f;
        layout.preferredWidth = 220f;
        layout.flexibleWidth = 1f;
        layout.minHeight = 38f;
        layout.preferredHeight = 42f;
        layout.flexibleHeight = 0f;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
    }

    private static void ConfigureCityHallTabButton(Button button, Text label)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
        }

        if (image != null)
        {
            image.raycastTarget = true;
            button.targetGraphic = image;
        }

        if (label != null)
        {
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
        }

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        layout.minHeight = 38f;
        layout.preferredHeight = 38f;
        layout.flexibleWidth = 1f;
        layout.flexibleHeight = 0f;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
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
        CanvasGroup canvasGroup = rowRoot.gameObject.AddComponent<CanvasGroup>();
        LayoutElement rowLayout = rowRoot.GetComponent<LayoutElement>();
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
            CanvasGroup = canvasGroup,
            LayoutElement = rowLayout,
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
                ClearUnreadCityHallRequests();
                isCityHallScreenDirty = true;
            }
        }

        UpdateCityHallRejectedRowDismissAnimation();

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
        ClearUnreadCityHallRequests();
        List<CityComplaintRowViewModel> rows = isCityHallUpgradesTabActive ? null : BuildCityHallComplaintRows(ru);
        if (rows != null && (selectedCityComplaintId <= 0 || !DoesCityHallRowsContain(rows, selectedCityComplaintId)))
        {
            selectedCityComplaintId = rows.Count > 0 ? rows[0].Id : 0;
        }

        cityHallScreenUi.TitleText.text = ru ? "\u0420\u0430\u0442\u0443\u0448\u0430" : "City Hall";
        int dueHours = Mathf.RoundToInt(GetCityComplaintDueWorldHours());
        int requestPenalty = GetCityTrustCitizenRequestRejectedPenalty();
        cityHallScreenUi.SummaryText.text = ru
            ? $"{FormatCityTrustSummary(ru)}.\nОбращения: активно {CountOpenCityComplaints()}, срочных {CountCriticalCityComplaints()}, просрочено {CountExpiredCityComplaints()}, выполнено сегодня {CountResolvedCityComplaintsToday()}.\nПринятое обращение становится городской целью на {dueHours} ч. Отказ или просрочка: доверие {requestPenalty}.\nАпдейты: куплено {CountPurchasedCityUpgrades()} / {CityUpgradeDefinitions.Length}."
            : $"{FormatCityTrustSummary(ru)}.\nCitizen requests: active {CountOpenCityComplaints()}, urgent {CountCriticalCityComplaints()}, expired {CountExpiredCityComplaints()}, resolved today {CountResolvedCityComplaintsToday()}.\nAccepted requests become {dueHours}h city goals. Rejection or expiry: {requestPenalty} trust.\nUpgrades: purchased {CountPurchasedCityUpgrades()} / {CityUpgradeDefinitions.Length}.";
        cityHallScreenUi.SummaryText.color = cityTrust <= -20 ? GetCityTrustColor() : FleetSecondaryTextColor;
        ApplyCityHallTabVisuals(ru);

        if (cityHallScreenUi.RequestsRoot != null)
        {
            cityHallScreenUi.RequestsRoot.gameObject.SetActive(!isCityHallUpgradesTabActive);
        }

        if (cityHallScreenUi.UpgradesRoot != null)
        {
            cityHallScreenUi.UpgradesRoot.gameObject.SetActive(isCityHallUpgradesTabActive);
        }

        if (isCityHallUpgradesTabActive)
        {
            RebuildCityHallUpgradeTree(ru);
            LocalizeCanvas(cityHallScreenUi.CanvasRoot);
            LayoutRebuilder.ForceRebuildLayoutImmediate(cityHallScreenUi.WindowRoot);
            return;
        }

        cityHallScreenUi.EmptyText.gameObject.SetActive(rows.Count == 0);
        cityHallScreenUi.EmptyText.text = ru
            ? "Пока обращений нет. Ратуша молчит так уверенно, будто это тоже вид управления."
            : "No requests yet. City Hall is quiet.";

        for (int i = 0; i < cityHallScreenUi.Rows.Length; i++)
        {
            CityHallComplaintRowUi row = cityHallScreenUi.Rows[i];
            bool active = i < rows.Count;
            row.Button.gameObject.SetActive(active);
            if (!active)
            {
                row.ComplaintId = 0;
                if (row.CanvasGroup != null)
                {
                    row.CanvasGroup.alpha = 1f;
                    row.CanvasGroup.interactable = true;
                    row.CanvasGroup.blocksRaycasts = true;
                }

                if (row.LayoutElement != null)
                {
                    row.LayoutElement.preferredHeight = 52f;
                    row.LayoutElement.minHeight = 0f;
                }

                continue;
            }

            CityComplaintRowViewModel data = rows[i];
            CityComplaint complaint = GetCityComplaintById(data.Id);
            bool isDismissingRejected = IsCityHallRejectedComplaintDismissing(complaint);
            float dismissT = isDismissingRejected
                ? Mathf.Clamp01(cityHallRejectedRowDismissTimer / CityHallRejectedRowDismissSeconds)
                : 1f;

            row.ComplaintId = data.Id;
            row.TitleText.text = data.Title;
            row.MetaText.text = data.Meta;
            row.Accent.color = data.AccentColor;
            row.Background.color = data.Id == selectedCityComplaintId
                ? new Color(0.24f, 0.30f, 0.40f, 1f)
                : !IsCityComplaintActive(complaint)
                    ? new Color(0.12f, 0.17f, 0.15f, 1f)
                    : FleetCardMutedColor;
            row.TitleText.color = !IsCityComplaintActive(complaint) ? FleetSecondaryTextColor : Color.white;
            row.Button.interactable = !isDismissingRejected;
            if (row.CanvasGroup != null)
            {
                row.CanvasGroup.alpha = dismissT;
                row.CanvasGroup.interactable = !isDismissingRejected;
                row.CanvasGroup.blocksRaycasts = !isDismissingRejected;
            }

            if (row.LayoutElement != null)
            {
                row.LayoutElement.preferredHeight = Mathf.Lerp(0f, 52f, dismissT);
                row.LayoutElement.minHeight = 0f;
            }
        }

        UpdateCityHallComplaintDetail(rows, ru);
        LocalizeCanvas(cityHallScreenUi.CanvasRoot);
        EnsureCityHallDecisionButtonsClickable();
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
            cityHallScreenUi.DetailTitleText.text = ru ? "Нет обращений" : "No request selected";
            cityHallScreenUi.DetailMetaText.text = string.Empty;
            cityHallScreenUi.DetailBodyText.text = ru
                ? "Выбери обращение слева: там обычно спрятана маленькая катастрофа с официальной шапкой."
                : "Select a request on the left to see the reason.";
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
        bool isPendingDecision = hasComplaint && complaint.State == CityComplaintState.Open;

        cityHallScreenUi.AcceptButtonText.text = ru ? "Принять" : "Accept";
        cityHallScreenUi.AcceptButton.interactable = isPendingDecision;
        cityHallScreenUi.RejectButtonText.text = ru ? "Отклонить" : "Reject";
        cityHallScreenUi.RejectButton.interactable = isPendingDecision;
        EnsureCityHallDecisionButtonsClickable();
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

    private void AcceptSelectedCityComplaint()
    {
        int acceptedComplaintId = selectedCityComplaintId;
        if (TryAcceptCityHallRequestCommand(acceptedComplaintId, out bool startsSocialScene))
        {
            PlayUiSound(uiSelectClip, 0.75f);
            isCityHallPanelOpen = false;
            isCityHallScreenDirty = true;
            cityHallScreenUi.CanvasRoot.SetActive(false);
            if (startsSocialScene)
            {
                StartAcceptedCitySocialIntroductionRequest(acceptedComplaintId);
            }
        }
    }

    private void RejectSelectedCityComplaint()
    {
        int rejectedComplaintId = selectedCityComplaintId;
        if (TryRejectCityHallRequestCommand(selectedCityComplaintId))
        {
            PlayUiSound(slotLoseClip != null ? slotLoseClip : uiSelectClip, 0.62f);
            BeginCityHallRejectedRowDismiss(rejectedComplaintId);
            CityComplaint next = GetHighestPriorityOpenCityComplaint();
            selectedCityComplaintId = next?.Id ?? 0;
            isCityHallScreenDirty = true;
        }
    }

    private static bool DoesCityHallRowsContain(List<CityComplaintRowViewModel> rows, int complaintId)
    {
        if (rows == null || complaintId <= 0)
        {
            return false;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Id == complaintId)
            {
                return true;
            }
        }

        return false;
    }

    private void BeginCityHallRejectedRowDismiss(int complaintId)
    {
        if (complaintId <= 0)
        {
            return;
        }

        cityHallDismissingRejectedComplaintId = complaintId;
        cityHallRejectedRowDismissTimer = CityHallRejectedRowDismissSeconds;
    }

    private void UpdateCityHallRejectedRowDismissAnimation()
    {
        if (cityHallRejectedRowDismissTimer <= 0f)
        {
            return;
        }

        cityHallRejectedRowDismissTimer = Mathf.Max(0f, cityHallRejectedRowDismissTimer - Time.unscaledDeltaTime);
        if (cityHallRejectedRowDismissTimer <= 0f)
        {
            cityHallDismissingRejectedComplaintId = 0;
        }

        isCityHallScreenDirty = true;
    }

    private bool IsCityHallRejectedComplaintDismissing(CityComplaint complaint)
    {
        return complaint != null &&
               complaint.Id == cityHallDismissingRejectedComplaintId &&
               complaint.State == CityComplaintState.Rejected &&
               cityHallRejectedRowDismissTimer > 0f;
    }
}
