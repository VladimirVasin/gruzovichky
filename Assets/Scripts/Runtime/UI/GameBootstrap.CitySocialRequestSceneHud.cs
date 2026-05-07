using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const float CitySocialSceneDimSeconds = 1.28f;
    private const float CitySocialSceneRevealSeconds = 1.02f;
    private const float CitySocialSceneCloseSeconds = 0.58f;
    private const float CitySocialSceneWordSeconds = 0.055f;
    private const float CitySocialConversationGoodChance = 0.70f;
    private const float CitySocialResultRevealSeconds = 0.82f;

    private enum CitySocialRequestScenePhase
    {
        Closed,
        Opening,
        Input,
        TargetReveal,
        TargetIntro,
        Dialogue,
        Result,
        Closing
    }

    private enum CitySocialConversationOutcome
    {
        Success,
        Failure
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
        public RectTransform ResultPanel;
        public CanvasGroup ResultGroup;
        public Text ResultTitleText;
        public Text ResultBodyText;
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
    private string citySocialTypewriterFullText = string.Empty;
    private int citySocialTypewriterVisibleWords;
    private int citySocialTypewriterWordCount;
    private float citySocialTypewriterTimer;
    private float citySocialTypewriterWordPulse;
    private bool citySocialTypewriterActive;
    private Text citySocialTypewriterTargetText;
    private Vector2 citySocialRequesterBasePosition;
    private Vector2 citySocialTargetBasePosition;
    private float citySocialPortraitAnimTime;
    private int citySocialSpeakingSide = -1;
    private CitySocialConversationOutcome citySocialConversationOutcome = CitySocialConversationOutcome.Success;

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
        citySocialPortraitAnimTime = 0f;
        ResetCitySocialTopicInputFeedback();
        citySocialSpeakingSide = -1;
        citySocialConversationOutcome = Random.value <= CitySocialConversationGoodChance
            ? CitySocialConversationOutcome.Success
            : CitySocialConversationOutcome.Failure;
        SelectCitySocialDialogueVariant();
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
        citySocialRequestSceneHud.RequesterCard.localRotation = Quaternion.identity;
        citySocialRequestSceneHud.TargetCard.anchoredPosition = new Vector2(150f, 54f);
        citySocialRequestSceneHud.TargetCard.localScale = Vector3.one * 0.82f;
        citySocialRequestSceneHud.TargetCard.localRotation = Quaternion.identity;
        citySocialRequesterBasePosition = new Vector2(0f, 92f);
        citySocialTargetBasePosition = new Vector2(150f, 92f);
        citySocialRequestSceneHud.RequesterNameText.text = request.RequesterName;
        citySocialRequestSceneHud.TargetNameText.text = request.TargetName;
        citySocialRequestSceneHud.TopicInput.SetTextWithoutNotify(string.Empty);
        citySocialRequestSceneHud.TopicInput.gameObject.SetActive(false);
        citySocialRequestSceneHud.ActionButton.gameObject.SetActive(false);
        citySocialRequestSceneHud.ResultPanel.gameObject.SetActive(false);
        citySocialRequestSceneHud.ResultGroup.alpha = 0f;
        citySocialRequestSceneHud.ResultPanel.localScale = Vector3.one * 0.78f;
        ResetCitySocialVoice();

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

        CreateCitySocialResultPanel(sceneRoot, font);
        canvasObject.SetActive(false);
    }

    private void CreateCitySocialResultPanel(Transform parent, Font font)
    {
        RectTransform panel = CreateStyledPanel("ResultPanel", parent, new Color(0.06f, 0.10f, 0.16f, 0.96f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(620f, 178f);
        panel.anchoredPosition = new Vector2(0f, 16f);
        panel.localScale = Vector3.one * 0.78f;
        CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 22, 22);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        Text title = CreateHeaderText("ResultTitle", panel, font, string.Empty, 34, TextAnchor.MiddleCenter, Color.white);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        Text body = CreateBodyText("ResultBody", panel, font, string.Empty, 17, TextAnchor.UpperCenter, new Color(0.88f, 0.92f, 0.98f, 1f));
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.gameObject.AddComponent<LayoutElement>().preferredHeight = 82f;
        citySocialRequestSceneHud.ResultPanel = panel;
        citySocialRequestSceneHud.ResultGroup = group;
        citySocialRequestSceneHud.ResultTitleText = title;
        citySocialRequestSceneHud.ResultBodyText = body;
        panel.gameObject.SetActive(false);
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
        citySocialRequestSceneHud.TitleText.text = "Нужна тема для разговора";
        citySocialSpeakingSide = 0;
        SetCitySocialBodyText(
            $"{request.RequesterName} хочет заговорить с {request.TargetName}, но застрял на самом опасном месте: первой фразе.\nПодскажите тему, за которую можно зацепиться. Что-нибудь живое, странное или хотя бы не смертельно неловкое.");
        citySocialRequestSceneHud.ActionButtonText.text = "Подсказать";
    }

    private void UpdateCitySocialRequestSceneHudRuntime()
    {
        if (!isCitySocialRequestSceneOpen || citySocialRequestSceneHud == null)
        {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        UpdateCitySocialTypewriter(dt);
        UpdateCitySocialTopicRejectShake(dt);
        switch (citySocialRequestScenePhase)
        {
            case CitySocialRequestScenePhase.Opening:
                UpdateCitySocialSceneOpening(dt);
                break;
            case CitySocialRequestScenePhase.Input:
                UpdateCitySocialPortraitMotion(dt);
                break;
            case CitySocialRequestScenePhase.TargetReveal:
                UpdateCitySocialSceneTargetReveal(dt);
                break;
            case CitySocialRequestScenePhase.TargetIntro:
            case CitySocialRequestScenePhase.Dialogue:
                UpdateCitySocialPortraitMotion(dt);
                break;
            case CitySocialRequestScenePhase.Result:
                UpdateCitySocialSceneResult(dt);
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
        float eased = SmootherStep01(t);
        citySocialRequestSceneHud.DimImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.94f, eased));
        float portraitT = SmootherStep01(Mathf.Clamp01((t - 0.10f) / 0.84f));
        citySocialRequestSceneHud.RequesterGroup.alpha = portraitT;
        float wordPulse = citySocialSpeakingSide == 0 ? citySocialTypewriterWordPulse : 0f;
        float portraitScale = Mathf.Lerp(0.72f, 1f, portraitT) + wordPulse * 0.035f * portraitT;
        float portraitY = Mathf.Lerp(-8f, 92f, portraitT) + wordPulse * 4.5f * portraitT;
        citySocialRequestSceneHud.RequesterCard.localScale = Vector3.one * portraitScale;
        citySocialRequestSceneHud.RequesterCard.anchoredPosition = new Vector2(0f, portraitY);

        if (t < 1f)
        {
            return;
        }

        PauseCitySocialRequestSceneSimulation();
        citySocialRequestScenePhase = CitySocialRequestScenePhase.Input;
        citySocialRequesterBasePosition = new Vector2(0f, 92f);
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
        float eased = SmootherStep01(t);
        citySocialRequestSceneHud.RequesterCard.anchoredPosition = new Vector2(Mathf.Lerp(0f, -150f, eased), 92f);
        citySocialRequestSceneHud.TargetCard.anchoredPosition = new Vector2(Mathf.Lerp(360f, 150f, eased), 92f);
        citySocialRequestSceneHud.TargetCard.localScale = Vector3.one * Mathf.Lerp(0.74f, 1f, eased);
        citySocialRequestSceneHud.TargetGroup.alpha = eased;

        if (t < 1f)
        {
            return;
        }

        citySocialRequesterBasePosition = new Vector2(-150f, 92f);
        citySocialTargetBasePosition = new Vector2(150f, 92f);
        citySocialRequestSceneHud.RequesterCard.anchoredPosition = citySocialRequesterBasePosition;
        citySocialRequestSceneHud.TargetCard.anchoredPosition = citySocialTargetBasePosition;
        citySocialRequestSceneHud.TargetCard.localScale = Vector3.one;
        citySocialRequestScenePhase = CitySocialRequestScenePhase.TargetIntro;
    }

    private void UpdateCitySocialSceneResult(float dt)
    {
        citySocialRequestSceneTimer += dt;
        UpdateCitySocialPortraitMotion(dt);

        float t = Mathf.Clamp01(citySocialRequestSceneTimer / CitySocialResultRevealSeconds);
        float eased = SmootherStep01(t);
        float punchT = Mathf.Clamp01(citySocialRequestSceneTimer / 1.15f);
        float punch = Mathf.Sin(punchT * Mathf.PI * 4f) * Mathf.Pow(1f - punchT, 1.6f) * 0.075f;
        float typedPulse = citySocialTypewriterTargetText == citySocialRequestSceneHud.ResultBodyText
            ? citySocialTypewriterWordPulse * 0.032f
            : 0f;
        citySocialRequestSceneHud.ResultGroup.alpha = eased;
        citySocialRequestSceneHud.ResultPanel.localScale = Vector3.one * (Mathf.Lerp(0.72f, 1f, eased) + punch + typedPulse);
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
        citySocialRequestSceneHud.ResultPanel.gameObject.SetActive(false);
        citySocialRequestSceneHud.ResultGroup.alpha = 0f;
        isCitySocialRequestSceneOpen = false;
        citySocialRequestScenePhase = CitySocialRequestScenePhase.Closed;
        citySocialSpeakingSide = -1;
        CompleteCitySocialTypewriter();
        ResetCitySocialVoice();
        ResumeCitySocialRequestSceneSimulation();
    }

    private void AdvanceCitySocialRequestScene()
    {
        if (citySocialRequestScenePhase == CitySocialRequestScenePhase.Input)
        {
            if (citySocialTypewriterActive)
            {
                CompleteCitySocialTypewriter();
                return;
            }

            SubmitCitySocialTopic();
            return;
        }

        if (citySocialRequestScenePhase == CitySocialRequestScenePhase.Result)
        {
            if (citySocialTypewriterActive)
            {
                CompleteCitySocialTypewriter();
                return;
            }

            citySocialRequestScenePhase = CitySocialRequestScenePhase.Closing;
            citySocialRequestSceneTimer = 0f;
            PlayUiSound(uiPanelCloseClip != null ? uiPanelCloseClip : uiSelectClip, 0.72f);
            return;
        }

        if (citySocialRequestScenePhase == CitySocialRequestScenePhase.TargetIntro)
        {
            if (citySocialTypewriterActive)
            {
                CompleteCitySocialTypewriter();
                return;
            }

            citySocialRequestScenePhase = CitySocialRequestScenePhase.Dialogue;
            citySocialRequestDialogueIndex = 0;
            ShowCitySocialDialogueLine();
            return;
        }

        if (citySocialRequestScenePhase != CitySocialRequestScenePhase.Dialogue)
        {
            return;
        }

        if (citySocialTypewriterActive)
        {
            CompleteCitySocialTypewriter();
            return;
        }

        citySocialRequestDialogueIndex++;
        if (citySocialRequestDialogueIndex >= GetCitySocialDialogueLineCount())
        {
            ShowCitySocialConversationResult();
            return;
        }

        ShowCitySocialDialogueLine();
    }

    private void ShowCitySocialConversationResult()
    {
        CitySocialIntroductionRequest request = activeCitySocialIntroductionRequest;
        string requester = request?.RequesterName ?? "Житель";
        string target = request?.TargetName ?? "Житель";
        string topic = string.IsNullOrWhiteSpace(citySocialRequestTopic)
            ? SanitizeCitySocialTopic(string.Empty)
            : citySocialRequestTopic;
        bool success = citySocialConversationOutcome == CitySocialConversationOutcome.Success;
        DriverAgent requesterAgent = request != null ? GetDriverAgentById(request.RequesterId) : null;
        DriverAgent targetAgent = request != null ? GetDriverAgentById(request.TargetId) : null;
        GetCitySocialPairScores(requesterAgent, targetAgent, out int familiarityBefore, out int relationshipBefore);

        citySocialSpeakingSide = -1;
        citySocialRequestScenePhase = CitySocialRequestScenePhase.Result;
        citySocialRequestSceneTimer = 0f;
        citySocialRequestSceneHud.ResultPanel.gameObject.SetActive(true);
        citySocialRequestSceneHud.ResultGroup.alpha = 0f;
        citySocialRequestSceneHud.ResultPanel.localScale = Vector3.one * 0.72f;
        citySocialRequestSceneHud.TitleText.text = success ? "Разговор удался" : "Разговор не сложился";
        citySocialRequestSceneHud.BodyText.text = string.Empty;
        citySocialRequestSceneHud.ActionButtonText.text = "Закрыть";

        Image resultImage = citySocialRequestSceneHud.ResultPanel.GetComponent<Image>();
        if (resultImage != null)
        {
            resultImage.color = success
                ? new Color(0.06f, 0.16f, 0.11f, 0.96f)
                : new Color(0.18f, 0.08f, 0.07f, 0.96f);
        }

        Outline resultOutline = citySocialRequestSceneHud.ResultPanel.GetComponent<Outline>();
        if (resultOutline != null)
        {
            resultOutline.effectColor = success
                ? new Color(0.45f, 0.86f, 0.36f, 0.46f)
                : new Color(0.94f, 0.35f, 0.28f, 0.52f);
        }

        citySocialRequestSceneHud.ResultTitleText.text = success ? "Успех" : "Провал";
        citySocialRequestSceneHud.ResultTitleText.color = success
            ? new Color(0.58f, 0.91f, 0.42f, 1f)
            : new Color(1f, 0.36f, 0.28f, 1f);

        CompleteCitySocialIntroductionRequest(citySocialRequestTopic, success);

        GetCitySocialPairScores(requesterAgent, targetAgent, out int familiarityAfter, out int relationshipAfter);
        int familiarityDelta = Mathf.Max(0, familiarityAfter - familiarityBefore);
        int relationshipDeltaValue = relationshipAfter - relationshipBefore;
        if (requesterAgent == null || targetAgent == null)
        {
            familiarityDelta = success
                ? CitySocialIntroSuccessFamiliarityDelta
                : CitySocialIntroFailureFamiliarityDelta;
            relationshipDeltaValue = success
                ? CitySocialIntroSuccessRelationshipDelta
                : CitySocialIntroFailureRelationshipDelta;
        }

        string relationshipDelta = FormatCitySocialSignedDelta(relationshipDeltaValue);
        string resultText = BuildCitySocialConversationResultText(
            requester,
            target,
            topic,
            familiarityDelta,
            relationshipDelta,
            success);
        SetCitySocialTypedText(citySocialRequestSceneHud.ResultBodyText, resultText);
        PlayUiSound(success ? (slotWinClip != null ? slotWinClip : uiSelectClip) : (slotLoseClip != null ? slotLoseClip : uiSelectClip), success ? 0.76f : 0.70f);
    }

    private static string FormatCitySocialSignedDelta(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }

    private void GetCitySocialPairScores(DriverAgent requester, DriverAgent target, out int familiarity, out int relationship)
    {
        WorkerSocialMemory requesterMemory = requester != null && target != null
            ? FindWorkerSocialMemory(requester, target.DriverId)
            : null;
        WorkerSocialMemory targetMemory = requester != null && target != null
            ? FindWorkerSocialMemory(target, requester.DriverId)
            : null;
        familiarity = GetWorkerSocialPairAverageFamiliarity(requesterMemory, targetMemory);
        relationship = GetWorkerSocialPairAverageRelationship(requesterMemory, targetMemory);
    }

    private void SetCitySocialBodyText(string text, bool instant = false)
    {
        SetCitySocialTypedText(citySocialRequestSceneHud?.BodyText, text, instant);
    }

    private void SetCitySocialTypedText(Text target, string text, bool instant = false)
    {
        citySocialTypewriterFullText = text ?? string.Empty;
        citySocialTypewriterTargetText = target;
        citySocialTypewriterWordCount = CountCitySocialWords(citySocialTypewriterFullText);
        citySocialTypewriterVisibleWords = instant || citySocialTypewriterWordCount <= 1
            ? citySocialTypewriterWordCount
            : 1;
        citySocialTypewriterTimer = 0f;
        citySocialTypewriterWordPulse = citySocialTypewriterVisibleWords > 0 ? 1f : 0f;
        citySocialTypewriterActive = !instant && citySocialTypewriterVisibleWords < citySocialTypewriterWordCount;
        UpdateCitySocialTypedBodyText();
        if (!instant && citySocialTypewriterVisibleWords > 0) PlayCitySocialVoiceWord(citySocialTypewriterVisibleWords);
    }

    private void UpdateCitySocialTypewriter(float dt)
    {
        citySocialTypewriterWordPulse = Mathf.Max(0f, citySocialTypewriterWordPulse - dt * 7.5f);
        if (!citySocialTypewriterActive)
        {
            return;
        }

        citySocialTypewriterTimer += dt;
        while (citySocialTypewriterTimer >= CitySocialSceneWordSeconds &&
               citySocialTypewriterVisibleWords < citySocialTypewriterWordCount)
        {
            citySocialTypewriterTimer -= CitySocialSceneWordSeconds;
            citySocialTypewriterVisibleWords++;
            citySocialTypewriterWordPulse = 1f;
            UpdateCitySocialTypedBodyText();
            PlayCitySocialVoiceWord(citySocialTypewriterVisibleWords);
        }

        if (citySocialTypewriterVisibleWords >= citySocialTypewriterWordCount)
        {
            citySocialTypewriterActive = false;
            if (citySocialTypewriterTargetText != null)
            {
                citySocialTypewriterTargetText.text = citySocialTypewriterFullText;
            }
        }
    }

    private void CompleteCitySocialTypewriter()
    {
        citySocialTypewriterActive = false;
        citySocialTypewriterVisibleWords = citySocialTypewriterWordCount;
        citySocialTypewriterWordPulse = 0f;
        if (citySocialTypewriterTargetText != null)
        {
            citySocialTypewriterTargetText.text = citySocialTypewriterFullText;
        }
    }

    private void UpdateCitySocialTypedBodyText()
    {
        if (citySocialTypewriterTargetText == null)
        {
            return;
        }

        if (citySocialTypewriterWordCount <= 0 ||
            citySocialTypewriterVisibleWords >= citySocialTypewriterWordCount)
        {
            citySocialTypewriterTargetText.text = citySocialTypewriterFullText;
            return;
        }

        int visibleLength = GetCitySocialWordRevealLength(citySocialTypewriterFullText, citySocialTypewriterVisibleWords);
        citySocialTypewriterTargetText.text = citySocialTypewriterFullText.Substring(0, visibleLength);
    }

    private static int CountCitySocialWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        int count = 0;
        bool inWord = false;
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                inWord = false;
                continue;
            }

            if (!inWord)
            {
                count++;
                inWord = true;
            }
        }

        return count;
    }

    private static int GetCitySocialWordRevealLength(string text, int visibleWords)
    {
        if (string.IsNullOrEmpty(text) || visibleWords <= 0)
        {
            return 0;
        }

        int seen = 0;
        bool inWord = false;
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                inWord = false;
                if (seen >= visibleWords)
                {
                    return i;
                }

                continue;
            }

            if (!inWord)
            {
                seen++;
                inWord = true;
            }

            if (seen > visibleWords)
            {
                return i;
            }
        }

        return text.Length;
    }

    private void UpdateCitySocialPortraitMotion(float dt)
    {
        if (citySocialRequestSceneHud == null)
        {
            return;
        }

        citySocialPortraitAnimTime += dt;
        ApplyCitySocialPortraitMotion(citySocialRequestSceneHud.RequesterCard, citySocialRequesterBasePosition, citySocialSpeakingSide == 0, 0f);
        if (citySocialRequestSceneHud.TargetCard != null && citySocialRequestSceneHud.TargetCard.gameObject.activeSelf)
        {
            ApplyCitySocialPortraitMotion(citySocialRequestSceneHud.TargetCard, citySocialTargetBasePosition, citySocialSpeakingSide == 1, 1.7f);
        }
    }

    private void ApplyCitySocialPortraitMotion(RectTransform card, Vector2 basePosition, bool speaking, float phaseOffset)
    {
        if (card == null)
        {
            return;
        }

        float t = citySocialPortraitAnimTime + phaseOffset;
        float idleX = Mathf.Sin(t * 1.25f) * 1.6f;
        float idleY = Mathf.Cos(t * 1.55f) * 2.4f;
        float idleRot = Mathf.Sin(t * 1.05f) * 0.65f;
        float scale = 1f;
        float wordPulse = speaking && (citySocialTypewriterActive || citySocialTypewriterWordPulse > 0.02f)
            ? citySocialTypewriterWordPulse
            : 0f;

        if (wordPulse > 0f)
        {
            float emphatic = Mathf.Sin(t * 10.5f);
            float sway = Mathf.Sin(t * 4.2f);
            idleX += (sway * 5.2f + Mathf.Sin(t * 15.3f) * 1.4f) * wordPulse;
            idleY += (Mathf.Abs(emphatic) * 8.5f + Mathf.Sin(t * 6.3f) * 2.0f) * wordPulse;
            idleRot += (sway * 4.1f + Mathf.Sin(t * 8.0f) * 1.2f) * wordPulse;
            scale = 1f + (Mathf.Abs(emphatic) * 0.055f + Mathf.Max(0f, Mathf.Sin(t * 3.0f)) * 0.018f) * wordPulse;
        }

        card.anchoredPosition = basePosition + new Vector2(idleX, idleY);
        card.localRotation = Quaternion.Euler(0f, 0f, idleRot);
        card.localScale = Vector3.one * scale;
    }

    private static float SmootherStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (6f * t - 15f) + 10f);
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
