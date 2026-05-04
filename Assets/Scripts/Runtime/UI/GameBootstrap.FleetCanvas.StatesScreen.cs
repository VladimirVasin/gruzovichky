using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private bool isStatesScreenDirty = true;
    private StatesScreenUiRefs statesScreenUi;

    private sealed class StatesScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text TitleText;
        public Text SubtitleText;
        public RectTransform ContentRoot;
    }

    private void SetupStatesScreenUi()
    {
        if (statesScreenUi != null)
        {
            return;
        }

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statesScreenUi = new StatesScreenUiRefs();

        GameObject canvasObject = new("StatesScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        statesScreenUi.CanvasRoot = canvasObject;

        RectTransform window = CreateStyledPanel("StatesWindowRoot", canvasObject.transform, FleetPanelColor);
        SetCenteredWindow(window, 980f, 620f, -16f);
        statesScreenUi.WindowRoot = window;

        VerticalLayoutGroup windowLayout = window.gameObject.AddComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(18, 18, 18, 18);
        windowLayout.spacing = 12f;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = true;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;

        statesScreenUi.TitleText = CreateHeaderText("StatesTitle", window, font, string.Empty, 24, TextAnchor.MiddleLeft, Color.white);
        statesScreenUi.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        statesScreenUi.SubtitleText = CreateBodyText("StatesSubtitle", window, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        statesScreenUi.SubtitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        RectTransform listFrame = CreateStyledPanel("StatesListFrame", window, FleetInsetColor);
        listFrame.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        FleetCanvasUiFactory.ScrollPanelRefs scroll = CreateVerticalScrollPanel(
            "StatesScroll",
            listFrame,
            new Color(0f, 0f, 0f, 0.04f),
            10f,
            10f);
        StretchRect(scroll.Root, 8f, 8f, 22f, 8f);
        scroll.ScrollRect.vertical = true;
        scroll.ScrollRect.movementType = ScrollRect.MovementType.Clamped;
        scroll.ScrollRect.inertia = false;
        scroll.ScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scroll.ScrollRect.verticalScrollbar = CreatePanelScrollbar("StatesScrollbar", listFrame);
        statesScreenUi.ContentRoot = scroll.Content;

        AddOverlayCloseButton(window, font);
        statesScreenUi.CanvasRoot.SetActive(false);
        isStatesScreenDirty = true;
        UpdateStatesScreenUi();
    }

    private void EnsureStatesScreenUiReady()
    {
        if (statesScreenUi == null)
        {
            SetupStatesScreenUi();
        }
    }

    private void UpdateStatesScreenUi()
    {
        if (statesScreenUi == null)
        {
            if (isStatesPanelOpen)
            {
                EnsureStatesScreenUiReady();
            }
            return;
        }

        bool shouldShow = isStatesPanelOpen;
        if (statesScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            statesScreenUi.CanvasRoot.SetActive(shouldShow);
            isStatesScreenDirty = true;
        }

        if (!shouldShow || !isStatesScreenDirty)
        {
            return;
        }

        RebuildStatesScreenContent();
        isStatesScreenDirty = false;
    }

    private void RebuildStatesScreenContent()
    {
        bool ru = IsRussianLanguage();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statesScreenUi.TitleText.text = ru ? "\u0421\u043f\u0440\u0430\u0432\u043a\u0430: \u043f\u0435\u0440\u043a\u0438 \u0438 \u0441\u0442\u0430\u0442\u0443\u0441\u044b" : "Stats: perks and statuses";
        statesScreenUi.SubtitleText.text = ru
            ? "\u0427\u0438\u0441\u043b\u043e\u0432\u044b\u0435 \u0441\u0442\u0430\u0442\u044b \u0443\u0434\u0430\u043b\u0435\u043d\u044b. \u042d\u0442\u043e\u0442 \u044d\u043a\u0440\u0430\u043d \u043e\u0431\u044a\u044f\u0441\u043d\u044f\u0435\u0442 \u043f\u043e\u0441\u0442\u043e\u044f\u043d\u043d\u044b\u0435 \u043f\u0435\u0440\u043a\u0438, \u043f\u0440\u043e\u0444\u0443\u0440\u043e\u0432\u043d\u0438 \u0438 \u0441\u043e\u0441\u0442\u043e\u044f\u043d\u0438\u044f \u0440\u0430\u0431\u043e\u0447\u0438\u0445."
            : "Numeric stats were removed. This screen explains permanent perks, professionalism levels, and worker statuses.";

        ClearUiChildren(statesScreenUi.ContentRoot);
        AddStatesSectionHeader(font, ru ? "\u041f\u0435\u0440\u043a\u0438" : "Perks");
        for (int i = 0; i < NegativePerks.Length; i++)
        {
            AddStatesReferenceRow(font, GetWorkerPerkDisplayName(NegativePerks[i], ru), GetWorkerPerkDescription(NegativePerks[i], ru), GetWorkerPerkTypeColor(WorkerPerkType.Negative));
        }

        for (int i = 0; i < PositivePerks.Length; i++)
        {
            AddStatesReferenceRow(font, GetWorkerPerkDisplayName(PositivePerks[i], ru), GetWorkerPerkDescription(PositivePerks[i], ru), GetWorkerPerkTypeColor(WorkerPerkType.Positive));
        }

        AddStatesSectionHeader(font, ru ? "\u041f\u0440\u043e\u0444\u0435\u0441\u0441\u0438\u043e\u043d\u0430\u043b\u0438\u0437\u043c" : "Professionalism");
        AddStatesReferenceRow(font, ru ? "\u0423\u0440\u043e\u0432\u0435\u043d\u044c 1" : "Level 1", ru ? "\u0411\u0430\u0437\u043e\u0432\u044b\u0439 \u0440\u0430\u0431\u043e\u0447\u0438\u0439. \u0414\u043e\u0441\u0442\u0443\u043f\u0435\u043d \u0434\u043b\u044f \u043e\u0431\u044b\u0447\u043d\u044b\u0445 \u043a\u043e\u043d\u0442\u0440\u0430\u043a\u0442\u043e\u0432." : "Base worker level. Eligible for regular contracts.", FleetMutedTextColor);
        AddStatesReferenceRow(font, ru ? "\u0423\u0440\u043e\u0432\u0435\u043d\u044c 2" : "Level 2", ru ? "\u041f\u043e\u0441\u043b\u0435 3 \u043e\u043f\u043b\u0430\u0447\u0435\u043d\u043d\u044b\u0445 \u0440\u0430\u0431\u043e\u0447\u0438\u0445 \u0434\u043d\u0435\u0439 \u0432 \u043d\u0430\u043f\u0440\u0430\u0432\u043b\u0435\u043d\u0438\u0438. \u0414\u0430\u0451\u0442 \u0434\u043e\u0441\u0442\u0443\u043f \u043a \u0431\u043e\u043b\u0435\u0435 \u0434\u043e\u0440\u043e\u0433\u0438\u043c \u043a\u043e\u043d\u0442\u0440\u0430\u043a\u0442\u0430\u043c." : "After 3 paid workdays in a track. Unlocks stronger contracts.", FleetAccentColor);
        AddStatesReferenceRow(font, ru ? "\u0423\u0440\u043e\u0432\u0435\u043d\u044c 3" : "Level 3", ru ? "\u041f\u043e\u0441\u043b\u0435 9 \u043e\u043f\u043b\u0430\u0447\u0435\u043d\u043d\u044b\u0445 \u0440\u0430\u0431\u043e\u0447\u0438\u0445 \u0434\u043d\u0435\u0439. \u0420\u0435\u0434\u043a\u0438\u0439 \u0432\u0435\u0440\u0445\u043d\u0438\u0439 \u0441\u043b\u043e\u0439 \u0434\u043b\u044f premium-\u0432\u0430\u043a\u0430\u043d\u0441\u0438\u0439." : "After 9 paid workdays. Rare high-end layer for premium vacancies.", new Color(0.55f, 0.88f, 0.44f, 1f));
        AddStatesReferenceRow(font, ru ? "\u0421\u043c\u044b\u0448\u043b\u0451\u043d\u044b\u0439" : "Quick Learner", ru ? "\u0421 \u044d\u0442\u0438\u043c \u043f\u0435\u0440\u043a\u043e\u043c: \u0443\u0440. 2 \u043f\u043e\u0441\u043b\u0435 2 \u0440\u0430\u0431. \u0434\u043d\u0435\u0439, \u0443\u0440. 3 \u043f\u043e\u0441\u043b\u0435 7." : "With this perk: level 2 after 2 workdays, level 3 after 7.", GetWorkerPerkTypeColor(WorkerPerkType.Positive));
        AddStatesReferenceRow(font, ru ? "\u041d\u0430\u043f\u0440\u0430\u0432\u043b\u0435\u043d\u0438\u044f" : "Tracks", ru ? "\u041e\u043f\u044b\u0442 \u043a\u043e\u043f\u0438\u0442\u0441\u044f \u043e\u0442\u0434\u0435\u043b\u044c\u043d\u043e: \u041b\u043e\u0433\u0438\u0441\u0442\u0438\u043a\u0430, \u041f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u043e, \u0421\u0435\u0440\u0432\u0438\u0441." : "Experience is tracked separately: Logistics, Production, Service.", FleetSecondaryTextColor);

        AddStatesSectionHeader(font, ru ? "\u041f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u0438 \u0438 \u0441\u0442\u0430\u0442\u0443\u0441\u044b" : "Needs and statuses");
        AddStatesReferenceRow(font, ru ? "\u0415\u0434\u0430" : "Meal", ru ? "\u0415\u0441\u043b\u0438 \u0433\u043e\u043b\u043e\u0434 \u0440\u0430\u0441\u0442\u0451\u0442, \u0440\u0430\u0431\u043e\u0447\u0438\u0439 \u0438\u0434\u0451\u0442 \u0435\u0441\u0442\u044c. \u0416\u0438\u0442\u0435\u043b\u0438 Personal House \u0435\u0434\u044f\u0442 \u0434\u043e\u043c\u0430." : "When hunger rises, the worker seeks food. Personal House residents eat at home.", FleetSecondaryTextColor);
        AddStatesReferenceRow(font, ru ? "\u0421\u043e\u043d" : "Sleep", ru ? "\u0411\u0435\u0437 \u0434\u043e\u043c\u0430 \u0440\u0430\u0431\u043e\u0447\u0438\u0439 \u0441\u043f\u0438\u0442 \u0432 \u041c\u043e\u0442\u0435\u043b\u0435 \u0438\u043b\u0438 \u0438\u0449\u0435\u0442 \u0441\u043a\u0430\u043c\u0435\u0439\u043a\u0443 \u0431\u0435\u0437 \u0434\u0435\u043d\u0435\u0433. \u0414\u043e\u043c \u0442\u0430\u043a\u0436\u0435 \u0432\u043e\u0441\u043f\u043e\u043b\u043d\u044f\u0435\u0442 \u0435\u0434\u0443." : "Without a home, workers sleep at the Motel or use a bench when broke. Home sleep also restores meal need.", FleetSecondaryTextColor);
        AddStatesReferenceRow(font, ru ? "\u0414\u043e\u0441\u0443\u0433" : "Leisure", ru ? "\u041f\u0430\u0440\u043a, \u0411\u0430\u0440 \u0438 \u0418\u0433\u0440\u043e\u0432\u044b\u0435 \u0437\u0430\u043a\u0440\u044b\u0432\u0430\u044e\u0442 \u0434\u043e\u0441\u0443\u0433. \u041f\u0435\u0440\u043a\u0438 \u043c\u043e\u0433\u0443\u0442 \u0441\u0438\u043b\u044c\u043d\u043e \u0441\u0434\u0432\u0438\u0433\u0430\u0442\u044c \u0432\u044b\u0431\u043e\u0440." : "Park, Bar, and Gambling Hall satisfy leisure. Perks can strongly shift the choice.", FleetSecondaryTextColor);
        AddStatesReferenceRow(font, ru ? "\u0418\u0449\u0435\u0442 \u0440\u0430\u0431\u043e\u0442\u0443" : "Job search", ru ? "\u0421\u0432\u043e\u0431\u043e\u0434\u043d\u044b\u0439 \u0440\u0430\u0431\u043e\u0447\u0438\u0439 \u043c\u043e\u0436\u0435\u0442 \u0437\u0430\u0440\u0435\u0437\u0435\u0440\u0432\u0438\u0440\u043e\u0432\u0430\u0442\u044c \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u044e \u0438 \u043f\u0440\u0438\u0439\u0442\u0438 \u043d\u0430 \u0411\u0438\u0440\u0436\u0443 \u0442\u0440\u0443\u0434\u0430." : "A free worker can reserve a vacancy and travel to the Labor Exchange.", FleetAccentColor);
        AddStatesReferenceRow(font, ru ? "\u041d\u0430 \u0441\u043c\u0435\u043d\u0435" : "On shift", ru ? "\u0420\u0430\u0431\u043e\u0447\u0438\u0439 \u043f\u043e\u043b\u0443\u0447\u0430\u0435\u0442 \u0437\u0430\u0440\u043f\u043b\u0430\u0442\u0443 \u0437\u0430 \u043e\u043f\u043b\u0430\u0447\u0435\u043d\u043d\u044b\u0439 \u0440\u0430\u0431\u043e\u0447\u0438\u0439 \u0434\u0435\u043d\u044c; \u0442\u0430\u043a\u043e\u0439 \u0434\u0435\u043d\u044c \u0442\u0430\u043a\u0436\u0435 \u0434\u0430\u0451\u0442 \u043f\u0440\u043e\u0444\u043e\u043f\u044b\u0442." : "A paid workday grants salary and professional experience.", FleetAccentColor);
        AddStatesReferenceRow(font, ru ? "\u0412 \u043f\u0443\u0442\u0438 / \u0432\u043d\u0443\u0442\u0440\u0438" : "Travel / inside", ru ? "\u041c\u043e\u0434\u0435\u043b\u044c \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e \u043c\u043e\u0436\u0435\u0442 \u043f\u0440\u044f\u0442\u0430\u0442\u044c\u0441\u044f \u0432\u043d\u0443\u0442\u0440\u0438 \u0437\u0434\u0430\u043d\u0438\u044f, \u0432 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0435 \u0438\u043b\u0438 \u0432 \u043b\u0438\u0447\u043d\u043e\u0439 \u043c\u0430\u0448\u0438\u043d\u0435." : "A worker model can hide inside buildings, buses, or personal cars.", FleetSecondaryTextColor);

        LayoutRebuilder.ForceRebuildLayoutImmediate(statesScreenUi.ContentRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(statesScreenUi.WindowRoot);
    }

    private void AddStatesSectionHeader(Font font, string title)
    {
        Text header = CreateHeaderText($"StatesSection{statesScreenUi.ContentRoot.childCount}", statesScreenUi.ContentRoot, font, title, 18, TextAnchor.MiddleLeft, FleetAccentColor);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
    }

    private void AddStatesReferenceRow(Font font, string title, string body, Color accent)
    {
        RectTransform row = CreateStyledPanel($"StatesRow{statesScreenUi.ContentRoot.childCount}", statesScreenUi.ContentRoot, FleetCardMutedColor);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
        HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(12, 14, 8, 8);
        rowLayout.spacing = 12f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        RectTransform accentBar = CreateUiObject("Accent", row).GetComponent<RectTransform>();
        accentBar.gameObject.AddComponent<LayoutElement>().preferredWidth = 4f;
        Image accentImage = accentBar.gameObject.AddComponent<Image>();
        accentImage.color = accent;

        RectTransform textColumn = CreateUiObject("Text", row).GetComponent<RectTransform>();
        textColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup textLayout = textColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 4f;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;

        Text titleText = CreateHeaderText("Title", textColumn, font, title, 13, TextAnchor.MiddleLeft, Color.white);
        titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        Text bodyText = CreateBodyText("Body", textColumn, font, body, 11, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        bodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
    }

    private static void ClearUiChildren(RectTransform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
            {
                Object.Destroy(child.gameObject);
            }
            else
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}
