using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const float CitySocialSceneDimSeconds = 0.72f;
    private const float CitySocialSceneRevealSeconds = 0.45f;
    private const float CitySocialSceneCloseSeconds = 0.38f;

    private enum CitySocialRequestScenePhase
    {
        Closed,
        Opening,
        Input,
        TargetReveal,
        Dialogue,
        Closing
    }

    private sealed class CitySocialRequestSceneHudRefs
    {
        public GameObject CanvasRoot;
        public CanvasGroup RootGroup;
        public Image DimImage;
        public RectTransform SceneRoot;
        public RectTransform RequesterCard;
        public RectTransform TargetCard;
        public CanvasGroup RequesterGroup;
        public CanvasGroup TargetGroup;
        public RectTransform RequesterPortraitRoot;
        public RectTransform TargetPortraitRoot;
        public Text RequesterNameText;
        public Text TargetNameText;
        public Text TitleText;
        public Text BodyText;
        public InputField TopicInput;
        public Text TopicInputText;
        public Button ActionButton;
        public Text ActionButtonText;
    }

    private CitySocialRequestSceneHudRefs citySocialRequestSceneHud;
    private CitySocialRequestScenePhase citySocialRequestScenePhase = CitySocialRequestScenePhase.Closed;
    private bool isCitySocialRequestSceneOpen;
    private bool citySocialRequestScenePausedSimulation;
    private int citySocialRequestScenePreviousSpeed = 1;
    private float citySocialRequestSceneTimer;
    private int citySocialRequestDialogueIndex;
    private string citySocialRequestTopic = string.Empty;

    private void StartCitySocialIntroductionScene(CitySocialIntroductionRequest request)
    {
        if (request == null)
        {
            return;
        }

        EnsureCitySocialRequestSceneHud();
        isCitySocialRequestSceneOpen = true;
        citySocialRequestScenePhase = CitySocialRequestScenePhase.Opening;
        citySocialRequestSceneTimer = 0f;
        citySocialRequestDialogueIndex = 0;
        citySocialRequestTopic = string.Empty;
        citySocialRequestScenePausedSimulation = false;
        citySocialRequestScenePreviousSpeed = Mathf.Max(1, gameSpeedMultiplier);

        citySocialRequestSceneHud.CanvasRoot.SetActive(true);
        citySocialRequestSceneHud.RootGroup.alpha = 1f;
        citySocialRequestSceneHud.RootGroup.blocksRaycasts = true;
        citySocialRequestSceneHud.RootGroup.interactable = true;
        citySocialRequestSceneHud.DimImage.color = new Color(0f, 0f, 0f, 0f);
        citySocialRequestSceneHud.RequesterGroup.alpha = 0f;
        citySocialRequestSceneHud.TargetGroup.alpha = 0f;
        citySocialRequestSceneHud.TargetCard.gameObject.SetActive(false);
        citySocialRequestSceneHud.RequesterCard.anchoredPosition = new Vector2(0f, 54f);
        citySocialRequestSceneHud.RequesterCard.localScale = Vector3.one * 0.82f;
        citySocialRequestSceneHud.TargetCard.anchoredPosition = new Vector2(150f, 54f);
        citySocialRequestSceneHud.TargetCard.localScale = Vector3.one * 0.82f;
        citySocialRequestSceneHud.RequesterNameText.text = request.RequesterName;
        citySocialRequestSceneHud.TargetNameText.text = request.TargetName;
        citySocialRequestSceneHud.TopicInput.SetTextWithoutNotify(string.Empty);
        citySocialRequestSceneHud.TopicInput.gameObject.SetActive(false);
        citySocialRequestSceneHud.ActionButton.gameObject.SetActive(false);

        DriverAgent requester = GetDriverAgentById(request.RequesterId);
        DriverAgent target = GetDriverAgentById(request.TargetId);
        DrawWorkerPortraitScaled(requester, citySocialRequestSceneHud.RequesterPortraitRoot, 1.10f);
        DrawWorkerPortraitScaled(target, citySocialRequestSceneHud.TargetPortraitRoot, 1.10f);
        SetCitySocialSceneRequestText(request);
        ValidateCitySocialRequestSceneClickTargets();
        PlayUiSound(uiPanelOpenClip != null ? uiPanelOpenClip : uiSelectClip, 0.72f);
    }

    private void EnsureCitySocialRequestSceneHud()
    {
        if (citySocialRequestSceneHud != null)
        {
            return;
        }

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        citySocialRequestSceneHud = new CitySocialRequestSceneHudRefs();

        GameObject canvasObject = new("CitySocialRequestSceneCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup rootGroup = canvasObject.AddComponent<CanvasGroup>();
        citySocialRequestSceneHud.CanvasRoot = canvasObject;
        citySocialRequestSceneHud.RootGroup = rootGroup;

        RectTransform dim = CreateUiObject("Dim", canvasObject.transform).GetComponent<RectTransform>();
        StretchRect(dim, 0f, 0f, 0f, 0f);
        Image dimImage = dim.gameObject.AddComponent<Image>();
        dimImage.color = Color.clear;
        dimImage.raycastTarget = true;
        citySocialRequestSceneHud.DimImage = dimImage;

        RectTransform sceneRoot = CreateUiObject("SceneRoot", canvasObject.transform).GetComponent<RectTransform>();
        sceneRoot.anchorMin = new Vector2(0.5f, 0.5f);
        sceneRoot.anchorMax = new Vector2(0.5f, 0.5f);
        sceneRoot.pivot = new Vector2(0.5f, 0.5f);
        sceneRoot.sizeDelta = new Vector2(920f, 620f);
        sceneRoot.anchoredPosition = Vector2.zero;
        citySocialRequestSceneHud.SceneRoot = sceneRoot;

        CreateCitySocialPortraitCard("Requester", sceneRoot, font, new Vector2(0f, 54f), out citySocialRequestSceneHud.RequesterCard, out citySocialRequestSceneHud.RequesterGroup, out citySocialRequestSceneHud.RequesterPortraitRoot, out citySocialRequestSceneHud.RequesterNameText);
        CreateCitySocialPortraitCard("Target", sceneRoot, font, new Vector2(150f, 54f), out citySocialRequestSceneHud.TargetCard, out citySocialRequestSceneHud.TargetGroup, out citySocialRequestSceneHud.TargetPortraitRoot, out citySocialRequestSceneHud.TargetNameText);

        RectTransform textPanel = CreateStyledPanel("TextPanel", sceneRoot, new Color(0.04f, 0.07f, 0.11f, 0.92f));
        textPanel.anchorMin = new Vector2(0.5f, 0f);
        textPanel.anchorMax = new Vector2(0.5f, 0f);
        textPanel.pivot = new Vector2(0.5f, 0f);
        textPanel.anchoredPosition = new Vector2(0f, 18f);
        textPanel.sizeDelta = new Vector2(820f, 238f);
        VerticalLayoutGroup textLayout = textPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.padding = new RectOffset(24, 24, 18, 18);
        textLayout.spacing = 10f;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;

        citySocialRequestSceneHud.TitleText = CreateHeaderText("Title", textPanel, font, string.Empty, 25, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.28f, 1f));
        citySocialRequestSceneHud.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        citySocialRequestSceneHud.BodyText = CreateBodyText("Body", textPanel, font, string.Empty, 17, TextAnchor.UpperLeft, new Color(0.88f, 0.92f, 0.98f, 1f));
        citySocialRequestSceneHud.BodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;

        citySocialRequestSceneHud.TopicInput = CreateCitySocialTopicInput("TopicInput", textPanel, font, out citySocialRequestSceneHud.TopicInputText);
        citySocialRequestSceneHud.TopicInput.gameObject.SetActive(false);
        citySocialRequestSceneHud.TopicInput.onEndEdit.AddListener(_ =>
        {
            if (citySocialRequestScenePhase == CitySocialRequestScenePhase.Input &&
                Keyboard.current != null &&
                Keyboard.current.enterKey.wasPressedThisFrame)
            {
                SubmitCitySocialTopic();
            }
        });

        citySocialRequestSceneHud.ActionButton = CreateButton("ActionButton", textPanel, font, out citySocialRequestSceneHud.ActionButtonText, string.Empty, 15, new Color(0.63f, 0.36f, 0.06f, 1f), Color.white);
        citySocialRequestSceneHud.ActionButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
        citySocialRequestSceneHud.ActionButtonText.fontStyle = FontStyle.Bold;
        citySocialRequestSceneHud.ActionButtonText.raycastTarget = false;
        citySocialRequestSceneHud.ActionButton.onClick.AddListener(AdvanceCitySocialRequestScene);

        canvasObject.SetActive(false);
    }

    private void CreateCitySocialPortraitCard(
        string name,
        Transform parent,
        Font font,
        Vector2 position,
        out RectTransform card,
        out CanvasGroup group,
        out RectTransform portraitRoot,
        out Text nameText)
    {
        card = CreateStyledPanel($"{name}PortraitCard", parent, new Color(0.06f, 0.10f, 0.16f, 0.96f));
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(230f, 246f);
        card.anchoredPosition = position;
        group = card.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 15, 15);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        portraitRoot = CreateStyledPanel("Portrait", card, new Color(0.08f, 0.12f, 0.18f, 1f));
        LayoutElement portraitLayout = portraitRoot.gameObject.AddComponent<LayoutElement>();
        portraitLayout.preferredHeight = 164f;
        portraitLayout.flexibleHeight = 0f;

        nameText = CreateHeaderText("Name", card, font, string.Empty, 18, TextAnchor.MiddleCenter, Color.white);
        nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
    }

    private InputField CreateCitySocialTopicInput(string name, Transform parent, Font font, out Text inputText)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = 42f;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.02f, 0.04f, 0.07f, 0.98f);
        image.raycastTarget = true;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.64f, 0.16f, 0.38f);
        outline.effectDistance = new Vector2(1f, -1f);

        Text placeholder = CreateBodyText("Placeholder", go.transform, font, "Введите тему разговора", 15, TextAnchor.MiddleLeft, new Color(0.50f, 0.57f, 0.66f, 1f));
        StretchRect(placeholder.rectTransform, 14f, 2f, 14f, 2f);
        placeholder.raycastTarget = false;
        placeholder.supportRichText = false;

        inputText = CreateBodyText("Text", go.transform, font, string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        StretchRect(inputText.rectTransform, 14f, 2f, 14f, 2f);
        inputText.raycastTarget = false;
        inputText.supportRichText = false;

        InputField input = go.GetComponent<InputField>();
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.targetGraphic = image;
        input.contentType = InputField.ContentType.Standard;
        input.lineType = InputField.LineType.SingleLine;
        input.characterLimit = 64;
        input.caretColor = Color.white;
        input.selectionColor = new Color(0.95f, 0.64f, 0.16f, 0.35f);
        return input;
    }

    private void SetCitySocialSceneRequestText(CitySocialIntroductionRequest request)
    {
        citySocialRequestSceneHud.TitleText.text = "Требуется тема для разговора";
        citySocialRequestSceneHud.BodyText.text =
            $"{request.RequesterName} хочет поговорить с {request.TargetName}, но внутренний отдел светской дипломатии ушёл обедать и оставил на столе только скрепку.\nПодскажите тему. Любую. Город переживал и более странные документы.";
        citySocialRequestSceneHud.ActionButtonText.text = "Подсказать";
    }

    private void UpdateCitySocialRequestSceneHudRuntime()
    {
        if (!isCitySocialRequestSceneOpen || citySocialRequestSceneHud == null)
        {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        switch (citySocialRequestScenePhase)
        {
            case CitySocialRequestScenePhase.Opening:
                UpdateCitySocialSceneOpening(dt);
                break;
            case CitySocialRequestScenePhase.TargetReveal:
                UpdateCitySocialSceneTargetReveal(dt);
                break;
            case CitySocialRequestScenePhase.Closing:
                UpdateCitySocialSceneClosing(dt);
                break;
        }
    }

    private void UpdateCitySocialSceneOpening(float dt)
    {
        citySocialRequestSceneTimer += dt;
        float t = Mathf.Clamp01(citySocialRequestSceneTimer / CitySocialSceneDimSeconds);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        citySocialRequestSceneHud.DimImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.94f, eased));
        citySocialRequestSceneHud.RequesterGroup.alpha = Mathf.Clamp01((t - 0.12f) / 0.58f);
        citySocialRequestSceneHud.RequesterCard.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, eased);
        citySocialRequestSceneHud.RequesterCard.anchoredPosition = new Vector2(0f, Mathf.Lerp(26f, 92f, eased));

        if (t < 1f)
        {
            return;
        }

        PauseCitySocialRequestSceneSimulation();
        citySocialRequestScenePhase = CitySocialRequestScenePhase.Input;
        citySocialRequestSceneHud.TopicInput.gameObject.SetActive(true);
        citySocialRequestSceneHud.ActionButton.gameObject.SetActive(true);
        citySocialRequestSceneHud.ActionButtonText.text = "Подсказать";
        citySocialRequestSceneHud.TopicInput.ActivateInputField();
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(citySocialRequestSceneHud.TopicInput.gameObject);
        }
    }

    private void UpdateCitySocialSceneTargetReveal(float dt)
    {
        citySocialRequestSceneTimer += dt;
        float t = Mathf.Clamp01(citySocialRequestSceneTimer / CitySocialSceneRevealSeconds);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        citySocialRequestSceneHud.RequesterCard.anchoredPosition = new Vector2(Mathf.Lerp(0f, -150f, eased), 92f);
        citySocialRequestSceneHud.TargetCard.anchoredPosition = new Vector2(Mathf.Lerp(300f, 150f, eased), 92f);
        citySocialRequestSceneHud.TargetCard.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, eased);
        citySocialRequestSceneHud.TargetGroup.alpha = eased;

        if (t < 1f)
        {
            return;
        }

        citySocialRequestScenePhase = CitySocialRequestScenePhase.Dialogue;
        citySocialRequestDialogueIndex = 0;
        ShowCitySocialDialogueLine();
    }

    private void UpdateCitySocialSceneClosing(float dt)
    {
        citySocialRequestSceneTimer += dt;
        float t = Mathf.Clamp01(citySocialRequestSceneTimer / CitySocialSceneCloseSeconds);
        float alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
        citySocialRequestSceneHud.RootGroup.alpha = alpha;
        citySocialRequestSceneHud.DimImage.color = new Color(0f, 0f, 0f, 0.94f * alpha);

        if (t < 1f)
        {
            return;
        }

        citySocialRequestSceneHud.CanvasRoot.SetActive(false);
        citySocialRequestSceneHud.RootGroup.alpha = 1f;
        citySocialRequestSceneHud.RootGroup.blocksRaycasts = false;
        citySocialRequestSceneHud.RootGroup.interactable = false;
        isCitySocialRequestSceneOpen = false;
        citySocialRequestScenePhase = CitySocialRequestScenePhase.Closed;
        ResumeCitySocialRequestSceneSimulation();
    }

    private void AdvanceCitySocialRequestScene()
    {
        if (citySocialRequestScenePhase == CitySocialRequestScenePhase.Input)
        {
            SubmitCitySocialTopic();
            return;
        }

        if (citySocialRequestScenePhase != CitySocialRequestScenePhase.Dialogue)
        {
            return;
        }

        citySocialRequestDialogueIndex++;
        if (citySocialRequestDialogueIndex >= 3)
        {
            CompleteCitySocialIntroductionRequest(citySocialRequestTopic);
            citySocialRequestScenePhase = CitySocialRequestScenePhase.Closing;
            citySocialRequestSceneTimer = 0f;
            PlayUiSound(uiPanelCloseClip != null ? uiPanelCloseClip : uiSelectClip, 0.72f);
            return;
        }

        ShowCitySocialDialogueLine();
    }

    private void SubmitCitySocialTopic()
    {
        citySocialRequestTopic = SanitizeCitySocialTopic(citySocialRequestSceneHud.TopicInput.text);
        citySocialRequestSceneHud.TopicInput.DeactivateInputField();
        citySocialRequestSceneHud.TopicInput.gameObject.SetActive(false);
        citySocialRequestSceneHud.TargetCard.gameObject.SetActive(true);
        citySocialRequestSceneHud.TargetGroup.alpha = 0f;
        citySocialRequestSceneHud.TargetCard.anchoredPosition = new Vector2(300f, 92f);
        citySocialRequestSceneHud.TargetCard.localScale = Vector3.one * 0.82f;
        citySocialRequestSceneHud.TitleText.text = "Социальный эксперимент начат";
        citySocialRequestSceneHud.BodyText.text = "Второй участник входит в поле гуманитарного поражения. Держитесь спокойно.";
        citySocialRequestSceneHud.ActionButtonText.text = "Дальше";
        citySocialRequestScenePhase = CitySocialRequestScenePhase.TargetReveal;
        citySocialRequestSceneTimer = 0f;
        PlayUiSound(uiSelectClip, 0.78f);
    }

    private void ShowCitySocialDialogueLine()
    {
        CitySocialIntroductionRequest request = activeCitySocialIntroductionRequest;
        string requester = request?.RequesterName ?? "Житель";
        string target = request?.TargetName ?? "Житель";
        string topic = string.IsNullOrWhiteSpace(citySocialRequestTopic)
            ? SanitizeCitySocialTopic(string.Empty)
            : citySocialRequestTopic;

        switch (citySocialRequestDialogueIndex)
        {
            case 0:
                citySocialRequestSceneHud.TitleText.text = requester;
                citySocialRequestSceneHud.BodyText.text = $"Привет. Как ты относишься к «{topic}»? Я спрашиваю без протокола, но с серьёзным лицом.";
                citySocialRequestSceneHud.ActionButtonText.text = "Дальше";
                break;
            case 1:
                citySocialRequestSceneHud.TitleText.text = target;
                citySocialRequestSceneHud.BodyText.text = $"Наконец-то вопрос, у которого есть запах странной папки и надежда на смысл. Расскажи ещё.";
                citySocialRequestSceneHud.ActionButtonText.text = "Дальше";
                break;
            default:
                citySocialRequestSceneHud.TitleText.text = requester;
                citySocialRequestSceneHud.BodyText.text = $"Вот оно. Маленький мост между двумя людьми, построенный из «{topic}» и административного отчаяния.";
                citySocialRequestSceneHud.ActionButtonText.text = "Завершить";
                break;
        }
    }

    private void PauseCitySocialRequestSceneSimulation()
    {
        if (citySocialRequestScenePausedSimulation)
        {
            return;
        }

        citySocialRequestScenePreviousSpeed = gameSpeedMultiplier > 0
            ? gameSpeedMultiplier
            : Mathf.Max(1, lastActiveGameSpeedMultiplier);
        lastActiveGameSpeedMultiplier = citySocialRequestScenePreviousSpeed;
        gameSpeedMultiplier = 0;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        citySocialRequestScenePausedSimulation = true;
    }

    private void ResumeCitySocialRequestSceneSimulation()
    {
        if (!citySocialRequestScenePausedSimulation)
        {
            return;
        }

        int speed = Mathf.Max(1, citySocialRequestScenePreviousSpeed);
        gameSpeedMultiplier = speed;
        Time.timeScale = speed;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        citySocialRequestScenePausedSimulation = false;
    }

    private void ValidateCitySocialRequestSceneClickTargets()
    {
        bool ok = citySocialRequestSceneHud?.CanvasRoot != null &&
                  citySocialRequestSceneHud.CanvasRoot.GetComponent<GraphicRaycaster>() != null &&
                  IsButtonClickTargetReady(citySocialRequestSceneHud.ActionButton) &&
                  citySocialRequestSceneHud.TopicInput != null &&
                  citySocialRequestSceneHud.TopicInput.targetGraphic != null &&
                  citySocialRequestSceneHud.TopicInput.targetGraphic.raycastTarget;

        if (!ok)
        {
            SessionDebugLogger.Log("UI_INPUT", "City social request scene click-target validation failed: check GraphicRaycaster, topic input, and action button.");
        }
    }
}
