using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class CityRequestGoalHudRefs
    {
        public GameObject CanvasRoot;
        public RectTransform PanelRoot;
        public Vector2 OriginalPosition;
        public Text TitleText;
        public Text SubtitleText;
        public GameObject GoalRowRoot;
        public Image CheckBox;
        public Text CheckMarkText;
        public Text GoalLabelText;
        public Text TimerText;
        public Image FlashOverlay;
    }

    private CityRequestGoalHudRefs cityRequestGoalHud;
    private float cityRequestGoalFeedbackTimer;
    private float cityRequestGoalFeedbackDuration;
    private Color cityRequestGoalFeedbackColor;
    private bool cityRequestGoalFeedbackHideWhenDone;
    private GameObject cityHallRequestMarkerRoot;
    private TextMesh cityHallRequestMarkerText;

    private void NotifyCityHallNewRequest(CityComplaint complaint)
    {
        if (complaint == null)
        {
            return;
        }

        complaint.IsUnread = true;
        isCityHallScreenDirty = true;
        PushFeedEvent(
            "City Hall received a citizen request.",
            $"В Ратушу пришло обращение: {FormatCityComplaintTitle(complaint, IsRussianLanguage())}.",
            complaint.Severity >= 3 ? FeedEventType.Warning : FeedEventType.Info);
        PlayUiSound(uiPanelOpenClip != null ? uiPanelOpenClip : uiSelectClip, 0.55f);
    }

    private void ClearUnreadCityHallRequests()
    {
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            if (cityComplaints[i] != null)
            {
                cityComplaints[i].IsUnread = false;
            }
        }
    }

    private int CountUnreadCityHallRequests()
    {
        int count = 0;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (complaint != null && complaint.IsUnread && complaint.State == CityComplaintState.Open)
            {
                count++;
            }
        }

        return count;
    }

    private CityComplaint GetActiveAcceptedCityServiceRequest()
    {
        CityComplaint best = null;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (complaint == null ||
                complaint.State != CityComplaintState.Accepted ||
                complaint.Category != CityComplaintCategory.ServiceMissing)
            {
                continue;
            }

            if (best == null || complaint.DueWorldHour < best.DueWorldHour)
            {
                best = complaint;
            }
        }

        return best;
    }

    private void EnsureCityRequestGoalHud()
    {
        if (cityRequestGoalHud != null)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cityRequestGoalHud = new CityRequestGoalHudRefs();

        GameObject canvasObject = new("CityRequestGoalCanvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        cityRequestGoalHud.CanvasRoot = canvasObject;

        RectTransform panel = CreateStyledPanel("CityRequestGoalPanel", canvasObject.transform, new Color(0.08f, 0.11f, 0.16f, 0.86f));
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 1f);
        panel.sizeDelta = new Vector2(390f, 156f);
        panel.anchoredPosition = new Vector2(12f, -58f);
        cityRequestGoalHud.PanelRoot = panel;
        cityRequestGoalHud.OriginalPosition = panel.anchoredPosition;
        SetGraphicRaycast(panel.gameObject, false);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        cityRequestGoalHud.TitleText = CreateGoalText("Title", panel, font, 19, FontStyle.Bold, new Color(1f, 0.86f, 0.32f), TextAnchor.MiddleLeft);
        cityRequestGoalHud.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        cityRequestGoalHud.SubtitleText = CreateGoalText("Subtitle", panel, font, 12, FontStyle.Normal, new Color(0.78f, 0.84f, 0.92f), TextAnchor.MiddleLeft);
        cityRequestGoalHud.SubtitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        CreateCityRequestGoalRow(panel, font);
        cityRequestGoalHud.TimerText = CreateGoalText("Timer", panel, font, 12, FontStyle.Normal, new Color(0.70f, 0.78f, 0.88f, 1f), TextAnchor.MiddleLeft);
        cityRequestGoalHud.TimerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        RectTransform flash = CreateUiObject("Flash", panel).GetComponent<RectTransform>();
        StretchRect(flash, 0f, 0f, 0f, 0f);
        Image flashImage = flash.gameObject.AddComponent<Image>();
        flashImage.color = Color.clear;
        flashImage.raycastTarget = false;
        flash.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        flash.gameObject.SetActive(false);
        cityRequestGoalHud.FlashOverlay = flashImage;
        flash.SetAsLastSibling();

        SetGraphicRaycast(canvasObject, false);
        canvasObject.SetActive(false);
    }

    private void CreateCityRequestGoalRow(RectTransform parent, Font font)
    {
        RectTransform row = CreateLayoutRow("CityRequestGoalRow", parent, 30f, 9f);
        SetGraphicRaycast(row.gameObject, false);
        cityRequestGoalHud.GoalRowRoot = row.gameObject;

        GameObject boxObject = CreateUiObject("CheckBox", row);
        Image box = boxObject.AddComponent<Image>();
        box.color = new Color(0.03f, 0.05f, 0.08f, 0.95f);
        box.raycastTarget = false;
        Outline outline = boxObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.78f, 0.25f, 0.62f);
        outline.effectDistance = new Vector2(1f, -1f);
        LayoutElement boxLayout = boxObject.AddComponent<LayoutElement>();
        boxLayout.preferredWidth = 22f;
        boxLayout.preferredHeight = 22f;
        cityRequestGoalHud.CheckBox = box;

        Text mark = CreateGoalText("CheckMark", boxObject.transform, font, 15, FontStyle.Bold, new Color(1f, 0.88f, 0.32f), TextAnchor.MiddleCenter);
        RectTransform markRect = mark.rectTransform;
        markRect.anchorMin = Vector2.zero;
        markRect.anchorMax = Vector2.one;
        markRect.offsetMin = Vector2.zero;
        markRect.offsetMax = Vector2.zero;
        cityRequestGoalHud.CheckMarkText = mark;

        Text label = CreateGoalText("Label", row, font, 12, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        cityRequestGoalHud.GoalLabelText = label;
    }

    private void UpdateCityRequestGoalHudRuntime()
    {
        if (cityRequestGoalHud == null && cityRequestGoalFeedbackTimer <= 0f && GetActiveAcceptedCityServiceRequest() == null)
        {
            return;
        }

        EnsureCityRequestGoalHud();
        CityComplaint activeRequest = GetActiveAcceptedCityServiceRequest();
        bool shouldShow = activeRequest != null || cityRequestGoalFeedbackTimer > 0f;
        if (cityRequestGoalHud.CanvasRoot.activeSelf != shouldShow)
        {
            cityRequestGoalHud.CanvasRoot.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            return;
        }

        if (cityRequestGoalFeedbackTimer <= 0f && activeRequest != null)
        {
            UpdateCityRequestGoalHudTexts(activeRequest, false, false);
        }

        if (cityRequestGoalFeedbackTimer > 0f)
        {
            cityRequestGoalFeedbackTimer -= Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(cityRequestGoalFeedbackTimer / Mathf.Max(cityRequestGoalFeedbackDuration, 0.01f));
            float pulse = Mathf.Sin((1f - progress) * Mathf.PI * 5f) * 0.025f * progress;
            float shake = Mathf.Sin(cityRequestGoalFeedbackTimer * 38f) * 4f * progress;
            cityRequestGoalHud.PanelRoot.localScale = Vector3.one * (1f + pulse);
            cityRequestGoalHud.PanelRoot.anchoredPosition = cityRequestGoalHud.OriginalPosition + new Vector2(shake, 0f);
            cityRequestGoalHud.FlashOverlay.gameObject.SetActive(true);
            cityRequestGoalHud.FlashOverlay.color = new Color(
                cityRequestGoalFeedbackColor.r,
                cityRequestGoalFeedbackColor.g,
                cityRequestGoalFeedbackColor.b,
                0.32f * progress);

            if (cityRequestGoalFeedbackTimer <= 0f)
            {
                cityRequestGoalHud.PanelRoot.localScale = Vector3.one;
                cityRequestGoalHud.PanelRoot.anchoredPosition = cityRequestGoalHud.OriginalPosition;
                cityRequestGoalHud.FlashOverlay.gameObject.SetActive(false);
                if (cityRequestGoalFeedbackHideWhenDone && GetActiveAcceptedCityServiceRequest() == null)
                {
                    cityRequestGoalHud.CanvasRoot.SetActive(false);
                }
            }
        }
    }

    private void StartCityRequestGoalFeedback(bool success, CityComplaint complaint, bool previewOnly = false)
    {
        EnsureCityRequestGoalHud();
        if (previewOnly)
        {
            cityRequestGoalHud.CanvasRoot.SetActive(true);
            UpdateCityRequestGoalHudTexts(complaint, false, false);
            return;
        }

        UpdateCityRequestGoalHudTexts(complaint, true, success);
        cityRequestGoalFeedbackDuration = success ? 1.15f : 1.55f;
        cityRequestGoalFeedbackTimer = cityRequestGoalFeedbackDuration;
        cityRequestGoalFeedbackColor = success
            ? new Color(0.10f, 0.80f, 0.25f, 1f)
            : new Color(0.86f, 0.18f, 0.12f, 1f);
        cityRequestGoalFeedbackHideWhenDone = true;
        cityRequestGoalHud.CanvasRoot.SetActive(true);
        PlayUiSound(success ? tutorialGoalSuccessClip : slotLoseClip, success ? 0.72f : 0.82f);
    }

    private void UpdateCityRequestGoalHudTexts(CityComplaint complaint, bool feedback, bool success)
    {
        bool ru = IsRussianLanguage();
        string target = FormatCityComplaintTargetName(complaint);
        if (feedback)
        {
            cityRequestGoalHud.TitleText.text = success
                ? (ru ? "Обращение выполнено" : "Request completed")
                : (ru ? "Обращение просрочено" : "Request expired");
            cityRequestGoalHud.SubtitleText.text = success
                ? (ru ? "Доверие +25." : "Trust +25.")
                : (ru ? "Доверие снизилось на 25." : "Trust -25.");
            cityRequestGoalHud.GoalLabelText.text = success
                ? (ru ? $"Построено: {target}" : $"Built: {target}")
                : (ru ? $"Не успели: {target}" : $"Missed: {target}");
            cityRequestGoalHud.TimerText.text = success
                ? (ru ? "Цель закрыта." : "Goal closed.")
                : (ru ? "Срок вышел." : "Time is up.");
            cityRequestGoalHud.CheckMarkText.text = success ? "X" : string.Empty;
            cityRequestGoalHud.CheckBox.color = success
                ? new Color(0.12f, 0.55f, 0.20f, 0.96f)
                : new Color(0.45f, 0.10f, 0.08f, 0.95f);
            cityRequestGoalHud.GoalLabelText.color = success
                ? new Color(0.65f, 1f, 0.72f, 1f)
                : new Color(1f, 0.72f, 0.68f, 1f);
            return;
        }

        cityRequestGoalHud.TitleText.text = ru ? "Обращение принято" : "Request accepted";
        cityRequestGoalHud.SubtitleText.text = ru ? "Городская цель" : "City goal";
        cityRequestGoalHud.GoalLabelText.text = ru ? $"Построить: {target}" : $"Build: {target}";
        cityRequestGoalHud.CheckMarkText.text = string.Empty;
        cityRequestGoalHud.CheckBox.color = new Color(0.03f, 0.05f, 0.08f, 0.95f);
        cityRequestGoalHud.GoalLabelText.color = Color.white;
        cityRequestGoalHud.TimerText.text = FormatCityRequestGoalTimerText(complaint, ru);
    }

    private string FormatCityRequestGoalTimerText(CityComplaint complaint, bool ru)
    {
        if (complaint == null || complaint.DueWorldHour <= 0f)
        {
            return ru ? "Осталось: 24ч" : "Time left: 24h";
        }

        float remaining = Mathf.Max(0f, complaint.DueWorldHour - GetCurrentWorldHour());
        int hours = Mathf.FloorToInt(remaining);
        int minutes = Mathf.FloorToInt((remaining - hours) * 60f);
        return ru ? $"Осталось: {hours:00}ч {minutes:00}м" : $"Time left: {hours:00}h {minutes:00}m";
    }

    private void UpdateCityHallRequestMarkerRuntime()
    {
        int unread = CountUnreadCityHallRequests();
        bool shouldShow = unread > 0 && locations.ContainsKey(LocationType.CityHall);
        if (!shouldShow)
        {
            if (cityHallRequestMarkerRoot != null && cityHallRequestMarkerRoot.activeSelf)
            {
                cityHallRequestMarkerRoot.SetActive(false);
            }

            return;
        }

        EnsureCityHallRequestMarker();
        cityHallRequestMarkerRoot.SetActive(true);
        cityHallRequestMarkerText.text = unread > 1 ? unread.ToString() : "!";
        Vector3 center = GetLocationCenter(LocationType.CityHall);
        float bob = Mathf.Sin(Time.unscaledTime * 4.2f) * 0.18f;
        cityHallRequestMarkerRoot.transform.position = center + new Vector3(0f, 2.8f + bob, 0f);
        cityHallRequestMarkerRoot.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5.6f) * 0.08f);
        if (mainCamera != null)
        {
            cityHallRequestMarkerRoot.transform.rotation = mainCamera.transform.rotation;
        }
    }

    private void EnsureCityHallRequestMarker()
    {
        if (cityHallRequestMarkerRoot != null)
        {
            return;
        }

        cityHallRequestMarkerRoot = new GameObject("CityHallRequestMarker");
        if (worldRoot != null)
        {
            cityHallRequestMarkerRoot.transform.SetParent(worldRoot, false);
        }

        cityHallRequestMarkerText = cityHallRequestMarkerRoot.AddComponent<TextMesh>();
        cityHallRequestMarkerText.anchor = TextAnchor.MiddleCenter;
        cityHallRequestMarkerText.alignment = TextAlignment.Center;
        cityHallRequestMarkerText.fontSize = 86;
        cityHallRequestMarkerText.characterSize = 0.22f;
        cityHallRequestMarkerText.color = new Color(1f, 0.72f, 0.12f, 1f);
    }
}
