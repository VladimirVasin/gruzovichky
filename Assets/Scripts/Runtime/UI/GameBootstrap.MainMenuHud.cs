using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class MainMenuButtonFx
    {
        public Button Button;
        public RectTransform RectTransform;
        public Image Image;
        public Color BaseColor;
        public Color HoverColor;
        public Color PressedColor;
        public float BaseScale;
        public float HoverScale;
        public float PressedScale;
        public bool IsHovered;
        public bool IsPressed;
    }

    private sealed class MainMenuHudRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Button ContinueButton;
        public Text ContinueButtonText;
        public Button NewGameButton;
        public Text NewGameButtonText;
        public Button NewGameUserButton;
        public Text NewGameUserButtonText;
        public Button PatchNotesButton;
        public Text PatchNotesButtonText;
        public Button ExitButton;
        public Text ExitButtonText;
        public Text LanguageLabelText;
        public Button EnglishButton;
        public Text EnglishButtonText;
        public Button RussianButton;
        public Text RussianButtonText;
        public Text GameVersionLabelText;
        public GameObject PatchNotesRoot;
        public Text PatchNotesTitleText;
        public RectTransform PatchNotesContentRoot;
        public Button PatchNotesCloseButton;
        public Text PatchNotesCloseButtonText;
        public MainMenuButtonFx ContinueButtonFx;
        public MainMenuButtonFx NewGameButtonFx;
        public MainMenuButtonFx NewGameUserButtonFx;
        public MainMenuButtonFx PatchNotesButtonFx;
        public MainMenuButtonFx ExitButtonFx;
        public MainMenuButtonFx EnglishButtonFx;
        public MainMenuButtonFx RussianButtonFx;
    }

    private MainMenuHudRefs mainMenuHud;
    private GameObject loadingOverlayCanvas;
    private Image loadingBarFill;
    private Text loadingStatusText;
    private static readonly bool IsUserModeTemporarilyDisabled = true;
    private const string UserModeWorkInProgressLabel = "Work in progress";
    private const string MainMenuVersionLabel = "Lo-fi Delivery Co. v.0.0.1";
    private const string PatchNotesButtonLabel = "Patch Notes";
    private bool isGameStarted;
    private GameStartMode selectedGameStartMode = GameStartMode.Debug;
    private bool isWorldBuilt;
    private bool isTutorialOpen;
    private bool isTutorialSkipped;
    private bool hasShownWelcomeTutorial;
    private bool hasShownFirstMotelTutorial;
    private bool hasShownWorkersPanelTutorial;
    private bool isBuildHighlightPersistent;
    private bool isWorkersHighlightPersistent;
    private bool isHireWorkerHighlightPersistent;
    private TutorialTrigger? pendingTutorialTrigger;
    private float pendingTutorialDelay;
    private bool hasShownFirstDriverHiredTutorial;
    private bool hasShownForestIntroTutorial;
    private bool hasShownForestWorkerStartedTutorial;
    private bool hasShownNeedSawmillTutorial;
    private bool hasShownSawmillBuiltTutorial;
    private bool hasShownFleetIntroTutorial;
    private bool hasShownFleetSelectTruckTutorial;
    private bool hasShownFleetAssignDriverTutorial;
    private bool hasShownFleetPickDriverTutorial;
    private bool hasShownAssignSawmillWorkerTutorial;
    private bool hasShownSawmillWorkerAssignedTutorial;
    private bool isShiftsHighlightPersistent;
    private bool isFleetHighlightPersistent;
    private enum TutorialCinematicPhase { None, TrackingBus, TrackingWorker, TrackingWorkerBackCloseup, Returning }
    private TutorialCinematicPhase tutorialCinematicPhase;
    private DriverAgent             tutorialCinematicDriver;
    private bool tutorialCinematicShouldShowForestIntro;
    private bool isLoadingWorld;
    private static GameStartMode? pendingAutoStartMode;
    // mainMenuMusicLoadCoroutine removed (music disabled)

    private void SetupMainMenuHud()
    {
        if (mainMenuHud != null)
        {
            return;
        }

        EnsureFleetEventSystem(); // buttons require an EventSystem — create it eagerly
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mainMenuHud = new MainMenuHudRefs();

        GameObject canvasObject = new("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;   // above tutorial canvas (80) so ESC → main menu covers it

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        mainMenuHud.CanvasRoot = canvasObject;
        // mainMenuMusicSource disabled

        RectTransform backgroundRoot = CreateUiObject("MainMenuBackground", canvasObject.transform).GetComponent<RectTransform>();
        // Anchor top edge to top of canvas so anchoredPosition.y controls vertical shift cleanly
        backgroundRoot.anchorMin = new Vector2(0f, 1f);
        backgroundRoot.anchorMax = new Vector2(1f, 1f);
        backgroundRoot.pivot = new Vector2(0.5f, 1f);
        backgroundRoot.sizeDelta = new Vector2(0f, 900f);
        backgroundRoot.anchoredPosition = new Vector2(0f, 20f);
        Image backgroundImage = backgroundRoot.gameObject.AddComponent<Image>();
        Sprite menuBackgroundSprite = LoadMainMenuBackgroundSprite();
        if (menuBackgroundSprite != null)
        {
            backgroundImage.sprite = menuBackgroundSprite;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = true;
            backgroundImage.color = Color.white;
            AspectRatioFitter aspectFitter = backgroundRoot.gameObject.AddComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            aspectFitter.aspectRatio = menuBackgroundSprite.rect.width / menuBackgroundSprite.rect.height;
        }
        else
        {
            backgroundImage.color = new Color(0.03f, 0.05f, 0.08f, 1f);
        }

        RectTransform screenTint = CreateStyledPanel("MainMenuTint", canvasObject.transform, new Color(0.03f, 0.05f, 0.08f, 0f));
        StretchRect(screenTint, 0f, 0f, 0f, 0f);

        mainMenuHud.GameVersionLabelText = CreateBodyText("MainMenuVersionLabel", screenTint, uiFont, MainMenuVersionLabel, 14, TextAnchor.MiddleRight, new Color(0.78f, 0.84f, 0.88f, 0.88f));
        RectTransform versionRect = mainMenuHud.GameVersionLabelText.GetComponent<RectTransform>();
        versionRect.anchorMin = new Vector2(1f, 0f);
        versionRect.anchorMax = new Vector2(1f, 0f);
        versionRect.pivot = new Vector2(1f, 0f);
        versionRect.anchoredPosition = new Vector2(-26f, 18f);
        versionRect.sizeDelta = new Vector2(360f, 28f);

        RectTransform window = CreateStyledPanel("MainMenuWindow", screenTint, FleetPanelColor);
        window.anchorMin = new Vector2(0f, 0f);
        window.anchorMax = new Vector2(0f, 0f);
        window.pivot = new Vector2(0f, 0f);
        window.anchoredPosition = new Vector2(48f, 58f);
        window.sizeDelta = new Vector2(360f, 326f);
        mainMenuHud.WindowRoot = window;

        VerticalLayoutGroup windowLayout = window.gameObject.AddComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(20, 20, 20, 20);
        windowLayout.spacing = 12;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = true;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;

        RectTransform buttonStack = CreateUiObject("ButtonStack", window).GetComponent<RectTransform>();
        VerticalLayoutGroup buttonLayout = buttonStack.gameObject.AddComponent<VerticalLayoutGroup>();
        buttonLayout.spacing = 12;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = true;
        buttonLayout.childForceExpandHeight = false;
        buttonStack.gameObject.AddComponent<LayoutElement>().preferredHeight = 208f;

        mainMenuHud.ContinueButton = CreateButton("ContinueButton", buttonStack, uiFont, out mainMenuHud.ContinueButtonText, "Continue", 18, new Color(0.22f, 0.54f, 0.30f, 1f), Color.white);
        mainMenuHud.ContinueButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
        mainMenuHud.ContinueButtonFx = SetupMainMenuButtonFx(mainMenuHud.ContinueButton, new Color(0.22f, 0.54f, 0.30f, 1f), new Color(0.30f, 0.72f, 0.40f, 1f));
        mainMenuHud.ContinueButton.onClick.AddListener(ContinueGameFromMainMenu);

        mainMenuHud.NewGameButton = CreateButton("NewGameDebugButton", buttonStack, uiFont, out mainMenuHud.NewGameButtonText, "New Game Debug", 18, FleetPrimaryButtonColor, Color.white);
        mainMenuHud.NewGameButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
        mainMenuHud.NewGameButtonFx = SetupMainMenuButtonFx(mainMenuHud.NewGameButton, FleetPrimaryButtonColor, new Color(0.90f, 0.65f, 0.20f, 1f));
        mainMenuHud.NewGameButton.onClick.AddListener(() => StartGameFromMainMenu(GameStartMode.Debug));

        mainMenuHud.NewGameUserButton = CreateButton("NewGameUserButton", buttonStack, uiFont, out mainMenuHud.NewGameUserButtonText, "New Game User", 18, new Color(0.22f, 0.46f, 0.58f, 1f), Color.white);
        mainMenuHud.NewGameUserButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
        mainMenuHud.NewGameUserButton.interactable = !IsUserModeTemporarilyDisabled;
        mainMenuHud.NewGameUserButtonFx = SetupMainMenuButtonFx(mainMenuHud.NewGameUserButton, new Color(0.22f, 0.46f, 0.58f, 1f), new Color(0.30f, 0.62f, 0.78f, 1f));
        if (!IsUserModeTemporarilyDisabled)
        {
            mainMenuHud.NewGameUserButton.onClick.AddListener(() => StartGameFromMainMenu(GameStartMode.User));
        }

        mainMenuHud.PatchNotesButton = CreateButton("PatchNotesButton", buttonStack, uiFont, out mainMenuHud.PatchNotesButtonText, PatchNotesButtonLabel, 18, new Color(0.27f, 0.33f, 0.43f, 1f), Color.white);
        mainMenuHud.PatchNotesButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        mainMenuHud.PatchNotesButtonFx = SetupMainMenuButtonFx(mainMenuHud.PatchNotesButton, new Color(0.27f, 0.33f, 0.43f, 1f), new Color(0.40f, 0.48f, 0.62f, 1f));
        mainMenuHud.PatchNotesButton.onClick.AddListener(OpenMainMenuPatchNotes);

        mainMenuHud.ExitButton = CreateButton("ExitButton", buttonStack, uiFont, out mainMenuHud.ExitButtonText, "Exit", 18, new Color(0.31f, 0.35f, 0.43f, 1f), Color.white);
        mainMenuHud.ExitButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        mainMenuHud.ExitButtonFx = SetupMainMenuButtonFx(mainMenuHud.ExitButton, new Color(0.31f, 0.35f, 0.43f, 1f), new Color(0.42f, 0.47f, 0.57f, 1f));
        mainMenuHud.ExitButton.onClick.AddListener(ExitGameFromMainMenu);

        RectTransform languageRow = CreateLayoutRow("LanguageRow", window, 34f, 8f);
        mainMenuHud.LanguageLabelText = CreateBodyText("LanguageLabel", languageRow, uiFont, "Language:", 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        LayoutElement languageLabelLayout = mainMenuHud.LanguageLabelText.gameObject.AddComponent<LayoutElement>();
        languageLabelLayout.preferredWidth = 98f;
        languageLabelLayout.preferredHeight = 30f;

        mainMenuHud.EnglishButton = CreateButton("EnglishLanguageButton", languageRow, uiFont, out mainMenuHud.EnglishButtonText, "Eng", 12, new Color(0.24f, 0.30f, 0.38f, 1f), Color.white);
        LayoutElement englishLayout = mainMenuHud.EnglishButton.gameObject.AddComponent<LayoutElement>();
        englishLayout.preferredWidth = 72f;
        englishLayout.preferredHeight = 30f;
        mainMenuHud.EnglishButtonFx = SetupMainMenuButtonFx(mainMenuHud.EnglishButton, new Color(0.24f, 0.30f, 0.38f, 1f), new Color(0.36f, 0.45f, 0.56f, 1f));
        mainMenuHud.EnglishButton.onClick.AddListener(() => SetLanguage(GameLanguage.English));

        mainMenuHud.RussianButton = CreateButton("RussianLanguageButton", languageRow, uiFont, out mainMenuHud.RussianButtonText, "Rus", 12, new Color(0.24f, 0.30f, 0.38f, 1f), Color.white);
        LayoutElement russianLayout = mainMenuHud.RussianButton.gameObject.AddComponent<LayoutElement>();
        russianLayout.preferredWidth = 72f;
        russianLayout.preferredHeight = 30f;
        mainMenuHud.RussianButtonFx = SetupMainMenuButtonFx(mainMenuHud.RussianButton, new Color(0.24f, 0.30f, 0.38f, 1f), new Color(0.36f, 0.45f, 0.56f, 1f));
        mainMenuHud.RussianButton.onClick.AddListener(() => SetLanguage(GameLanguage.Russian));

        SetupMainMenuPatchNotesWindow(screenTint, uiFont);
        UpdateMainMenuHud();
    }

    private Sprite LoadMainMenuBackgroundSprite()
    {
        // Load from Resources/ (works in Editor and Build)
        Sprite sprite = Resources.Load<Sprite>("MenuPic");
        if (sprite != null)
        {
            return sprite;
        }

        // Fallback for Editor: direct AssetDatabase path
#if UNITY_EDITOR
        Sprite editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/MenuPic.png");
        if (editorSprite != null)
        {
            return editorSprite;
        }
#endif

        return null;
    }

    private void UpdateMainMenuHud()
    {
        if (mainMenuHud == null)
        {
            return;
        }

        if (mainMenuHud.CanvasRoot.activeSelf != isMainMenuOpen)
        {
            mainMenuHud.CanvasRoot.SetActive(isMainMenuOpen);
        }

        if (!isMainMenuOpen)
        {
            return;
        }

        bool showContinue = isGameStarted;
        if (mainMenuHud.ContinueButton.gameObject.activeSelf != showContinue)
            mainMenuHud.ContinueButton.gameObject.SetActive(showContinue);

        UpdateMainMenuButtonFx(mainMenuHud.ContinueButtonFx);
        UpdateMainMenuButtonFx(mainMenuHud.NewGameButtonFx);
        UpdateMainMenuButtonFx(mainMenuHud.NewGameUserButtonFx);
        UpdateMainMenuButtonFx(mainMenuHud.PatchNotesButtonFx);
        UpdateMainMenuButtonFx(mainMenuHud.ExitButtonFx);
        UpdateMainMenuButtonFx(mainMenuHud.EnglishButtonFx);
        UpdateMainMenuButtonFx(mainMenuHud.RussianButtonFx);
        UpdateMainMenuTexts();
    }

    private void UpdateMainMenuTexts()
    {
        if (mainMenuHud == null)
        {
            return;
        }

        if (mainMenuHud.ContinueButtonText != null) mainMenuHud.ContinueButtonText.text = L("Continue");
        if (mainMenuHud.NewGameButtonText != null) mainMenuHud.NewGameButtonText.text = L("New Game Debug");
        if (mainMenuHud.NewGameUserButtonText != null) mainMenuHud.NewGameUserButtonText.text = IsUserModeTemporarilyDisabled ? UserModeWorkInProgressLabel : L("New Game User");
        if (mainMenuHud.PatchNotesButtonText != null) mainMenuHud.PatchNotesButtonText.text = PatchNotesButtonLabel;
        if (mainMenuHud.ExitButtonText != null) mainMenuHud.ExitButtonText.text = L("Exit");
        if (mainMenuHud.LanguageLabelText != null) mainMenuHud.LanguageLabelText.text = L("Language:");
        if (mainMenuHud.EnglishButtonText != null) mainMenuHud.EnglishButtonText.text = selectedLanguage == GameLanguage.English ? "[Eng]" : "Eng";
        if (mainMenuHud.RussianButtonText != null) mainMenuHud.RussianButtonText.text = selectedLanguage == GameLanguage.Russian ? "[Rus]" : "Rus";
        if (mainMenuHud.GameVersionLabelText != null) mainMenuHud.GameVersionLabelText.text = MainMenuVersionLabel;
        if (mainMenuHud.PatchNotesTitleText != null) mainMenuHud.PatchNotesTitleText.text = IsRussianLanguage() ? "\u041e\u0431\u043d\u043e\u0432\u043b\u0435\u043d\u0438\u044f" : "Patch Notes";
        RebuildMainMenuPatchNotesContent();
        if (mainMenuHud.PatchNotesCloseButtonText != null) mainMenuHud.PatchNotesCloseButtonText.text = L("Close");
    }

    private void SetupMainMenuPatchNotesWindow(RectTransform parent, Font uiFont)
    {
        RectTransform root = CreateStyledPanel("PatchNotesWindow", parent, new Color(0.07f, 0.10f, 0.15f, 0.96f));
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(860f, 640f);
        mainMenuHud.PatchNotesRoot = root.gameObject;

        VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 22, 22);
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("PatchNotesHeader", root, 42f, 12f);
        mainMenuHud.PatchNotesTitleText = CreateHeaderText("PatchNotesTitle", headerRow, uiFont, "Patch Notes", 24, TextAnchor.MiddleLeft, Color.white);
        mainMenuHud.PatchNotesTitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        mainMenuHud.PatchNotesCloseButton = CreateButton("PatchNotesCloseButton", headerRow, uiFont, out mainMenuHud.PatchNotesCloseButtonText, "Close", 14, new Color(0.52f, 0.16f, 0.17f, 1f), Color.white);
        LayoutElement closeLayout = mainMenuHud.PatchNotesCloseButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 96f;
        closeLayout.preferredHeight = 36f;
        mainMenuHud.PatchNotesCloseButton.onClick.AddListener(CloseMainMenuPatchNotes);

        RectTransform scrollRoot = CreateStyledPanel("PatchNotesScrollRoot", root, new Color(0.10f, 0.15f, 0.22f, 0.96f));
        scrollRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 530f;
        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        GameObject viewportObject = CreateUiObject("PatchNotesViewport", scrollRoot);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        StretchRect(viewport, 16f, 16f, 16f, 16f);
        viewportObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        viewportObject.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport;

        RectTransform content = CreateUiObject("PatchNotesContent", viewport).GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 3f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = content;
        mainMenuHud.PatchNotesContentRoot = content;

        root.gameObject.SetActive(false);
    }

    private void OpenMainMenuPatchNotes()
    {
        LogUiInput("Main Menu: clicked Patch Notes");
        if (mainMenuHud?.PatchNotesRoot == null)
        {
            return;
        }

        mainMenuHud.PatchNotesRoot.SetActive(true);
        UpdateMainMenuTexts();
        Canvas.ForceUpdateCanvases();
        ScrollRect scrollRect = mainMenuHud.PatchNotesRoot.GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
        PlayUiSound(uiPanelOpenClip, 0.8f);
    }

    private void CloseMainMenuPatchNotes()
    {
        if (mainMenuHud?.PatchNotesRoot == null)
        {
            return;
        }

        mainMenuHud.PatchNotesRoot.SetActive(false);
        PlayUiSound(uiPanelCloseClip, 0.8f);
    }

    private void RebuildMainMenuPatchNotesContent()
    {
        if (mainMenuHud?.PatchNotesContentRoot == null)
        {
            return;
        }

        for (int i = mainMenuHud.PatchNotesContentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(mainMenuHud.PatchNotesContentRoot.GetChild(i).gameObject);
        }

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bool ru = IsRussianLanguage();
        AddPatchNotesParagraph(uiFont, MainMenuVersionLabel, 15, Color.white, FontStyle.Bold);
        AddPatchNotesParagraph(
            uiFont,
            ru
                ? "\u0417\u0430\u043c\u0435\u0442\u043a\u0438 \u043e \u0432\u0435\u0440\u0441\u0438\u044f\u0445 \u0441\u0433\u0440\u0443\u043f\u043f\u0438\u0440\u043e\u0432\u0430\u043d\u044b \u043f\u043e \u0440\u0435\u043b\u0438\u0437\u0430\u043c. \u041f\u043e\u0437\u0436\u0435 \u0441\u044e\u0434\u0430 \u043c\u043e\u0436\u043d\u043e \u0431\u0443\u0434\u0435\u0442 \u0434\u043e\u0431\u0430\u0432\u043b\u044f\u0442\u044c \u043d\u043e\u0432\u044b\u0435 \u043f\u0430\u0442\u0447\u043d\u043e\u0443\u0442\u044b."
                : "Release notes are grouped by version so this screen can grow into a small in-game changelog.",
            13,
            FleetSecondaryTextColor,
            FontStyle.Normal);

        AddPatchNotesSection(uiFont, ru ? "v.0.0.1 - \u0442\u0435\u043a\u0443\u0449\u0438\u0439 \u0438\u0433\u0440\u0430\u0435\u043c\u044b\u0439 \u043f\u0440\u043e\u0442\u043e\u0442\u0438\u043f" : "v.0.0.1 - Current playable prototype");
        AddPatchNotesSection(uiFont, ru ? "\u041e\u0441\u043d\u043e\u0432\u043d\u043e\u0439 \u0446\u0438\u043a\u043b" : "Core loop");
        AddPatchNotesParagraph(uiFont, ru
            ? "- \u0421\u0442\u0440\u043e\u0439 \u0438 \u0441\u043e\u0435\u0434\u0438\u043d\u044f\u0439 \u043d\u0435\u0431\u043e\u043b\u044c\u0448\u043e\u0439 \u043b\u043e\u0433\u0438\u0441\u0442\u0438\u0447\u0435\u0441\u043a\u0438\u0439 \u0433\u043e\u0440\u043e\u0434 \u043d\u0430 \u0441\u0435\u0442\u043a\u0435.\n- \u0421\u0435\u0439\u0447\u0430\u0441 \u0434\u043e\u0441\u0442\u0443\u043f\u0435\u043d New Game Debug. New Game User \u0432\u0440\u0435\u043c\u0435\u043d\u043d\u043e \u043f\u043e\u043c\u0435\u0447\u0435\u043d \u043a\u0430\u043a Work in progress.\n- \u0412\u0435\u0440\u0445\u043d\u0438\u0435 HUD-\u0432\u043a\u043b\u0430\u0434\u043a\u0438 \u043e\u0442\u043a\u0440\u044b\u0432\u0430\u044e\u0442 \u0430\u0432\u0442\u043e\u043f\u0430\u0440\u043a, \u0440\u0430\u0431\u043e\u0447\u0438\u0445, \u0441\u043c\u0435\u043d\u044b, \u0440\u0435\u0441\u0443\u0440\u0441\u044b, \u044d\u043a\u043e\u043d\u043e\u043c\u0438\u043a\u0443, \u0441\u0442\u0440\u043e\u0439\u043a\u0443, \u0442\u043e\u0440\u0433\u043e\u0432\u043b\u044e \u0438 \u043a\u0430\u0440\u0442\u0443."
            : "- Build and connect a small logistics town on a generated grid.\n- Use New Game Debug for the currently available sandbox start. New Game User is temporarily marked Work in progress.\n- Use the top HUD tabs to open Fleet, Workers, Shifts, Resources, Economy, Build, Trade, and Map.");

        AddPatchNotesSection(uiFont, ru ? "\u0420\u0430\u0431\u043e\u0447\u0438\u0435" : "Workers");
        AddPatchNotesParagraph(uiFont, ru
            ? "- \u041d\u0430\u043d\u0438\u043c\u0430\u0439 \u0440\u0430\u0431\u043e\u0447\u0438\u0445 \u0432 \u043c\u0435\u043d\u044e \u0420\u0430\u0431\u043e\u0447\u0438\u0435. \u041d\u043e\u0432\u044b\u0435 \u043d\u0430\u0451\u043c\u043d\u0438\u043a\u0438 \u043f\u0440\u0438\u0435\u0437\u0436\u0430\u044e\u0442 \u043d\u0430 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0435.\n- \u0423 \u0440\u0430\u0431\u043e\u0447\u0438\u0445 \u0435\u0441\u0442\u044c \u043f\u043e\u0440\u0442\u0440\u0435\u0442\u044b, \u0431\u0430\u0437\u043e\u0432\u044b\u0435 \u043d\u0430\u0432\u044b\u043a\u0438 \u0438 \u0442\u0430\u0439\u043c\u0435\u0440\u044b \u043f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u0435\u0439: \u0435\u0434\u0430, \u0441\u043e\u043d, \u0440\u0430\u0437\u0432\u043b\u0435\u0447\u0435\u043d\u0438\u0435.\n- \u041d\u0430\u0437\u043d\u0430\u0447\u0430\u0439 \u0440\u0430\u0431\u043e\u0447\u0438\u0445 \u0432 \u043f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u043e \u0447\u0435\u0440\u0435\u0437 \u0421\u043c\u0435\u043d\u044b > \u041f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u043e."
            : "- Hire workers from the Workers menu. New hires arrive by bus before checking in.\n- Workers have generated portraits, base skills, and need timers for Food, Sleep, and Leisure.\n- Assign workers to production buildings from Shifts > Production.");

        AddPatchNotesSection(uiFont, ru ? "\u041f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u043e \u0438 \u0440\u0435\u0441\u0443\u0440\u0441\u044b" : "Production and resources");
        AddPatchNotesParagraph(uiFont, ru
            ? "- \u041b\u0435\u0441 \u0434\u0430\u0451\u0442 Logs. \u041b\u0435\u0441\u043e\u043f\u0438\u043b\u043a\u0430 \u0434\u0435\u043b\u0430\u0435\u0442 Boards. \u041c\u0435\u0431\u0435\u043b\u044c\u043d\u0430\u044f \u0444\u0430\u0431\u0440\u0438\u043a\u0430 \u0434\u0435\u043b\u0430\u0435\u0442 Furniture \u0438\u0437 Boards \u0438 Textile.\n- \u0421\u043a\u043b\u0430\u0434 \u0445\u0440\u0430\u043d\u0438\u0442 \u0433\u043e\u0442\u043e\u0432\u044b\u0435 \u0440\u0435\u0441\u0443\u0440\u0441\u044b.\n- Canteen, Bar \u0438 Motel \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u044e\u0442\u0441\u044f \u0432 life-cycle \u043f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u0435\u0439."
            : "- Forest produces Logs. Sawmill turns Logs into Boards. Furniture Factory turns Boards and Textile into Furniture.\n- Warehouse stores finished resources and supports the local resource loop.\n- Canteen, Bar, and Motel are service buildings used by worker life-cycle needs.");

        AddPatchNotesSection(uiFont, ru ? "\u041b\u043e\u0433\u0438\u0441\u0442\u0438\u043a\u0430 \u0438 \u0442\u043e\u0440\u0433\u043e\u0432\u043b\u044f" : "Logistics and trade");
        AddPatchNotesParagraph(uiFont, ru
            ? "- \u041d\u0430\u0437\u043d\u0430\u0447\u0430\u0439 \u0432\u043e\u0434\u0438\u0442\u0435\u043b\u0435\u0439 \u043d\u0430 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438 \u0447\u0435\u0440\u0435\u0437 \u0410\u0432\u0442\u043e\u043f\u0430\u0440\u043a.\n- \u0412 Trade \u043c\u043e\u0436\u043d\u043e \u0441\u043e\u0437\u0434\u0430\u0432\u0430\u0442\u044c Buy/Sell \u0437\u0430\u043a\u0430\u0437\u044b \u0438 \u043e\u0442\u043f\u0440\u0430\u0432\u043b\u044f\u0442\u044c intercity-\u0440\u0435\u0439\u0441\u044b \u0447\u0435\u0440\u0435\u0437 \u043c\u0430\u0433\u0438\u0441\u0442\u0440\u0430\u043b\u044c.\n- \u0413\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438 \u0432 trade-\u0440\u0435\u0439\u0441\u0430\u0445 \u043c\u043e\u0433\u0443\u0442 \u043f\u043e\u043f\u0430\u0441\u0442\u044c \u0432 race-\u043c\u0438\u043d\u0438\u0438\u0433\u0440\u0443."
            : "- Assign drivers to trucks from Fleet.\n- Use Trade to create Buy/Sell orders and send intercity trade runs through the highway.\n- Trade trucks can enter the race minigame when available.");

        AddPatchNotesSection(uiFont, ru ? "\u041c\u0438\u0440 \u0438 \u0430\u0442\u043c\u043e\u0441\u0444\u0435\u0440\u0430" : "World and atmosphere");
        AddPatchNotesParagraph(uiFont, ru
            ? "- \u0415\u0441\u0442\u044c \u043f\u0440\u043e\u0446\u0435\u0434\u0443\u0440\u043d\u044b\u0435 \u0434\u043e\u0440\u043e\u0433\u0438, \u0437\u0434\u0430\u043d\u0438\u044f, \u0440\u0435\u043a\u0430, \u043c\u0430\u0433\u0438\u0441\u0442\u0440\u0430\u043b\u044c, \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u044b, \u043b\u0430\u0432\u043e\u0447\u043a\u0438, \u0446\u0432\u0435\u0442\u044b, \u043f\u0447\u0451\u043b\u044b, \u043a\u043e\u0442\u044b, \u043f\u0442\u0438\u0446\u044b, \u0440\u044b\u0431\u044b, \u0447\u0430\u0441\u0442\u0438\u0446\u044b \u0438 \u043d\u043e\u0447\u043d\u044b\u0435 \u043c\u043e\u0448\u043a\u0438.\n- \u0420\u0435\u0433\u0438\u043e\u043d\u0430\u043b\u044c\u043d\u0430\u044f \u043a\u0430\u0440\u0442\u0430 \u043f\u043e\u043a\u0430\u0437\u044b\u0432\u0430\u0435\u0442 \u0442\u0435\u043a\u0443\u0449\u0438\u0439 \u0433\u043e\u0440\u043e\u0434 \u0438 \u0441\u043e\u0441\u0435\u0434\u043d\u0438\u0435 \u0440\u0435\u0433\u0438\u043e\u043d\u044b-\u0437\u0430\u0433\u043b\u0443\u0448\u043a\u0438."
            : "- Procedural roads, buildings, river, highway traffic, buses, benches, flowers, bees, cats, birds, fish, particles, and night moths are implemented as visual ambience.\n- Regional Map shows the current town and placeholder neighboring regions for future expansion.");
    }

    private void AddPatchNotesSection(Font uiFont, string text)
    {
        AddPatchNotesParagraph(uiFont, text, 15, FleetAccentColor, FontStyle.Bold, 22f);
    }

    private void AddPatchNotesParagraph(Font uiFont, string text, int fontSize = 14, Color? color = null, FontStyle style = FontStyle.Normal, float minHeight = 0f)
    {
        Text paragraph = CreateBodyText("PatchNotesParagraph", mainMenuHud.PatchNotesContentRoot, uiFont, text, fontSize, TextAnchor.UpperLeft, color ?? new Color(0.86f, 0.90f, 0.94f, 1f));
        paragraph.fontStyle = style;
        paragraph.horizontalOverflow = HorizontalWrapMode.Wrap;
        paragraph.verticalOverflow = VerticalWrapMode.Overflow;
        paragraph.lineSpacing = 1f;
        ContentSizeFitter fitter = paragraph.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        LayoutElement layout = paragraph.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = minHeight;
    }

    private void StartCityMusic()
    {
        if (cityMusicSource != null)
        {
            if (!cityMusicSource.isPlaying) cityMusicSource.UnPause();
            ResumeDayNightMusic();
            return;
        }
        AudioClip clip = Resources.Load<AudioClip>("City1");
        if (clip != null)
        {
            cityMusicSource = CreateAudioSource("CityMusic", null, true, 0f, 0f, false);
            cityMusicSource.clip = clip;
            cityMusicSource.Play();
        }
        SetupDayNightMusic();
    }

    private void StartMainMenuMusic()
    {
        if (mainMenuMusicSource == null)
        {
            AudioClip clip = Resources.Load<AudioClip>("MainMenu1");
            if (clip == null) return;
            mainMenuMusicSource = CreateAudioSource("MainMenuMusic", null, true, 0.20f, 0f, false);
            mainMenuMusicSource.ignoreListenerPause = true;
            mainMenuMusicSource.clip = clip;
            mainMenuMusicSource.Play();
        }
        else if (!mainMenuMusicSource.isPlaying)
        {
            mainMenuMusicSource.UnPause();
        }
    }

    private void StartGameFromMainMenu(GameStartMode mode)
    {
        LogUiInput($"Main Menu: clicked New Game {mode}");
        if (isLoadingWorld) return;

        if (isGameStarted || isWorldBuilt)
        {
            RestartGameFromMainMenu(mode);
            return;
        }

        selectedGameStartMode = mode;
        if (!isWorldBuilt)
        {
            SetupLoadingOverlay();
            loadingOverlayCanvas.SetActive(true);
            isLoadingWorld = true;
            StartCoroutine(BuildPrototypeSceneAsync());
        }
        else
        {
            FinishGameStart();
        }
    }

    private void RestartGameFromMainMenu(GameStartMode mode)
    {
        pendingAutoStartMode = mode;
        SessionDebugLogger.Log("BOOT", $"Restart requested from main menu. Pending mode={mode}.");
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        AudioListener.pause = false;
        Scene activeScene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                activeScene.path,
                new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    private void TryConsumePendingAutoStartMode()
    {
        if (!pendingAutoStartMode.HasValue)
        {
            return;
        }

        GameStartMode mode = pendingAutoStartMode.Value;
        pendingAutoStartMode = null;
        StartCoroutine(AutoStartGameAfterSceneRestart(mode));
    }

    private IEnumerator AutoStartGameAfterSceneRestart(GameStartMode mode)
    {
        yield return null;
        SessionDebugLogger.Log("BOOT", $"Auto-starting restarted world. Mode={mode}.");
        StartGameFromMainMenu(mode);
    }

    private void FinishGameStart()
    {
        isMainMenuOpen = false;
        isGameStarted = true;
        gameSpeedMultiplier = 1;
        lastActiveGameSpeedMultiplier = 1;
        InitUnlockedBuildTools();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        AudioListener.pause = false;
        mainMenuMusicSource?.Pause();
        StartCityMusic();
        UpdateDayNightCycle(0f);
        UpdateMainMenuHud();
        PlayUiSound(uiPanelOpenClip, 0.9f);
        TryShowTutorial(TutorialTrigger.GameStarted);
        if (selectedGameStartMode == GameStartMode.Debug)
        {
            foreach (TradeResourceType res in System.Enum.GetValues(typeof(TradeResourceType)))
                AddStoredTradeResource(res, 5);
            if (locations.TryGetValue(LocationType.Forest, out LocationData debugForest))
                debugForest.LogsStored += 5;
        }
    }

    private void SetupLoadingOverlay()
    {
        if (loadingOverlayCanvas != null) return;

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        loadingOverlayCanvas = new GameObject("LoadingOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = loadingOverlayCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;  // above main menu (90) so bar shows on top of it

        CanvasScaler scaler = loadingOverlayCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // No full-screen overlay — main menu stays visible underneath
        Transform canvasRoot = loadingOverlayCanvas.transform;

        // Bottom bar background (full width, 14px tall)
        RectTransform barBg = CreateUiObject("LoadingBarBg", canvasRoot).GetComponent<RectTransform>();
        barBg.anchorMin = new Vector2(0f, 0f);
        barBg.anchorMax = new Vector2(1f, 0f);
        barBg.pivot = new Vector2(0.5f, 0f);
        barBg.anchoredPosition = new Vector2(0f, 0f);
        barBg.sizeDelta = new Vector2(0f, 14f);
        barBg.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 1f);

        // Fill bar (starts at 0 width, grows left-to-right)
        RectTransform barFillRect = CreateUiObject("LoadingBarFill", barBg).GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = new Vector2(0f, 1f);
        barFillRect.pivot = new Vector2(0f, 0.5f);
        barFillRect.anchoredPosition = Vector2.zero;
        barFillRect.sizeDelta = new Vector2(0f, 0f);
        loadingBarFill = barFillRect.gameObject.AddComponent<Image>();
        loadingBarFill.color = FleetPrimaryButtonColor;

        // "Loading..." label just above the bar
        RectTransform labelRect = CreateUiObject("LoadingLabel", canvasRoot).GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 18f);
        labelRect.sizeDelta = new Vector2(0f, 22f);
        loadingStatusText = labelRect.gameObject.AddComponent<Text>();
        loadingStatusText.font = uiFont;
        loadingStatusText.fontSize = 14;
        loadingStatusText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        loadingStatusText.alignment = TextAnchor.MiddleCenter;
        loadingStatusText.text = L("Loading...");
    }

    private void SetLoadingProgress(float t, string status)
    {
        if (loadingBarFill == null) return;
        // barBg is full width (anchored 0→1 horizontally, sizeDelta.x=0)
        // We drive fill via anchorMax.x on the fill rect
        RectTransform rt = loadingBarFill.rectTransform;
        rt.anchorMax = new Vector2(Mathf.Clamp01(t), 1f);
        if (loadingStatusText != null) loadingStatusText.text = L(status);
    }

    private IEnumerator BuildPrototypeSceneAsync()
    {
        SessionDebugLogger.Log("BOOT", $"Building world for {selectedGameStartMode} mode.");

        worldRoot = new GameObject("PrototypeWorld").transform;
        roadsRoot = new GameObject("Roads").transform;
        roadsRoot.SetParent(worldRoot, false);
        lanternsRoot = new GameObject("RoadLanterns").transform;
        lanternsRoot.SetParent(worldRoot, false);
        roadsidePropsRoot = new GameObject("RoadsideProps").transform;
        roadsidePropsRoot.SetParent(worldRoot, false);
        miscRoot = new GameObject("Misc").transform;
        miscRoot.SetParent(worldRoot, false);

        const int totalSteps = 24;
        int step = 0;

        SetLoadingProgress(++step / (float)totalSteps, "Camera & lighting..."); yield return null;
        SetupCamera(); SetupLighting(); SetupDioramaPostProcessing(); SetupSurfaceMaterials();

        SetLoadingProgress(++step / (float)totalSteps, "Populating water..."); yield return null;
        PopulateWaterCells();

        SetLoadingProgress(++step / (float)totalSteps, "Setting up locations..."); yield return null;
        SetupLocations();

        SetLoadingProgress(++step / (float)totalSteps, "Building road network..."); yield return null;
        GenerateInitialRoadNetwork();

        SetLoadingProgress(++step / (float)totalSteps, "Generating terrain..."); yield return null;
        GenerateTerrainHeights();

        SetLoadingProgress(++step / (float)totalSteps, "Smoothing terrain..."); yield return null;
        FlattenTerrainNearWater();

        SetLoadingProgress(++step / (float)totalSteps, "Applying terrain..."); yield return null;
        ApplyTerrainHeightsToWorld();

        SetLoadingProgress(++step / (float)totalSteps, "Setting up ground..."); yield return null;
        yield return StartCoroutine(SetupGroundAsync()); CreateWaterLayer();

        SetLoadingProgress(++step / (float)totalSteps, "Building grid..."); yield return null;
        yield return StartCoroutine(SetupGridAsync());

        SetLoadingProgress(++step / (float)totalSteps, "Edge highways..."); yield return null;
        SetupEdgeHighway(); SetupEdgeHighwayBuses(); SetupRiverBoats();

        SetLoadingProgress(++step / (float)totalSteps, "Atmosphere..."); yield return null;
        SetupDistantClouds(); SetupAmbientAirParticles();

        SetLoadingProgress(++step / (float)totalSteps, "Road lanterns..."); yield return null;
        RebuildRoadLanterns();

        SetLoadingProgress(++step / (float)totalSteps, "Planting trees..."); yield return null;
        PopulateMiscTrees();

        SetLoadingProgress(++step / (float)totalSteps, "Placing benches..."); yield return null;
        RebuildRoadsideBenches();

        SetLoadingProgress(++step / (float)totalSteps, "Wildlife..."); yield return null;
        SetupMiscBirds(); SetupAmbientCats(); SetupAmbientBees(); SetupAmbientLanternMoths(); SetupRiverFish();

        SetLoadingProgress(++step / (float)totalSteps, "Water effects..."); yield return null;
        waterVisualLodLevel = -1; UpdateWaterVisualLod();

        SetLoadingProgress(++step / (float)totalSteps, "Visual tools..."); yield return null;
        SetupBuildHoverHighlight(); SetupForestWorkers(); SetupSelectionVisuals();

        SetLoadingProgress(++step / (float)totalSteps, "Vehicles..."); yield return null;
        SetupTruck();
        money = StartingTreasury;
        SetupCargoTransferVisual();

        SetLoadingProgress(++step / (float)totalSteps, "Audio..."); yield return null;
        SetupAudio();

        SetLoadingProgress(++step / (float)totalSteps, "Fleet UI..."); yield return null;
        SetupFleetScreenUi(); SetupDriversScreenUi(); SetupShiftsScreenUi();

        SetLoadingProgress(++step / (float)totalSteps, "Economy UI..."); yield return null;
        SetupResourcesScreenUi(); SetupEconomyScreenUi();

        SetLoadingProgress(++step / (float)totalSteps, "Build UI..."); yield return null;
        SetupBuildScreenUi(); SetupWorldMapScreenUi();

        SetLoadingProgress(++step / (float)totalSteps, "HUD..."); yield return null;
        SetupTruckQuickHud(); SetupDriverQuickHud(); SetupBuildingQuickHud(); SetupCellQuickHud();

        SetLoadingProgress(++step / (float)totalSteps, "Finishing up..."); yield return null;
        SetupMainMenuHud(); SetupJoinRaceButton();

        SetLoadingProgress(1f, "Done!"); yield return null;

        isWorldBuilt = true;
        isLoadingWorld = false;
        loadingOverlayCanvas.SetActive(false);

        SessionDebugLogger.Log("BOOT", $"Scene bootstrap complete. Mode={selectedGameStartMode}, Locations={locations.Count}, Roads={roadCells.Count}, Trucks={truckAgents.Count}.");
        FinishGameStart();
    }

    private void ContinueGameFromMainMenu()
    {
        LogUiInput("Main Menu: clicked Continue");
        isMainMenuOpen = false;
        gameSpeedMultiplier = lastActiveGameSpeedMultiplier > 0 ? lastActiveGameSpeedMultiplier : 1;
        Time.timeScale = gameSpeedMultiplier;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        AudioListener.pause = false;
        mainMenuMusicSource?.Pause();
        StartCityMusic();
        UpdateMainMenuHud();
        PlayUiSound(uiPanelCloseClip, 0.85f);
    }

    private void OpenPauseMenu()
    {
        if (!isGameStarted) return;
        lastActiveGameSpeedMultiplier = gameSpeedMultiplier > 0 ? gameSpeedMultiplier : lastActiveGameSpeedMultiplier;
        gameSpeedMultiplier = 0;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        AudioListener.pause = true;
        cityMusicSource?.Pause();
        PauseDayNightMusic();
        StartMainMenuMusic();
        isMainMenuOpen = true;
        UpdateMainMenuHud();
        PlayUiSound(uiPanelOpenClip, 0.8f);
    }

    private void ExitGameFromMainMenu()
    {
        LogUiInput("Main Menu: clicked Exit");
        SessionDebugLogger.EndSession("Exited from main menu");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private MainMenuButtonFx SetupMainMenuButtonFx(Button button, Color baseColor, Color hoverColor)
    {
        if (button == null || !button.TryGetComponent(out Image image))
        {
            return null;
        }

        MainMenuButtonFx fx = new()
        {
            Button = button,
            RectTransform = button.GetComponent<RectTransform>(),
            Image = image,
            BaseColor = baseColor,
            HoverColor = hoverColor,
            PressedColor = Color.Lerp(hoverColor, Color.black, 0.18f),
            BaseScale = 1f,
            HoverScale = 1.02f,
            PressedScale = 0.975f
        };

        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        AddEventTrigger(trigger, EventTriggerType.PointerEnter, () =>
        {
            if (!button.interactable) return;
            fx.IsHovered = true;
            PlayUiSound(menuHoverClip != null ? menuHoverClip : uiSelectClip, 0.84f);
        });
        AddEventTrigger(trigger, EventTriggerType.PointerExit, () =>
        {
            fx.IsHovered = false;
            fx.IsPressed = false;
        });
        AddEventTrigger(trigger, EventTriggerType.PointerDown, () =>
        {
            if (!button.interactable) return;
            fx.IsPressed = true;
            PlayUiSound(uiPanelOpenClip, 0.72f);
        });
        AddEventTrigger(trigger, EventTriggerType.PointerUp, () =>
        {
            fx.IsPressed = false;
        });

        return fx;
    }

    // Menu music disabled
    private void EnsureMainMenuMusic() { }

    private static void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction callback)
    {
        EventTrigger.Entry entry = new() { eventID = eventType };
        entry.callback.AddListener(_ => callback());
        trigger.triggers.Add(entry);
    }

    private static void UpdateMainMenuButtonFx(MainMenuButtonFx fx)
    {
        if (fx == null || fx.RectTransform == null || fx.Image == null)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        bool isInteractive = fx.Button == null || fx.Button.interactable;
        if (!isInteractive)
        {
            fx.IsHovered = false;
            fx.IsPressed = false;
        }

        Color disabledColor = Color.Lerp(fx.BaseColor, Color.black, 0.38f);
        Color targetColor = !isInteractive ? disabledColor : fx.IsPressed ? fx.PressedColor : fx.IsHovered ? fx.HoverColor : fx.BaseColor;
        float targetScale = !isInteractive ? fx.BaseScale : fx.IsPressed ? fx.PressedScale : fx.IsHovered ? fx.HoverScale : fx.BaseScale;

        fx.Image.color = Color.Lerp(fx.Image.color, targetColor, 12f * deltaTime);
        Vector3 currentScale = fx.RectTransform.localScale;
        Vector3 desiredScale = Vector3.one * targetScale;
        fx.RectTransform.localScale = Vector3.Lerp(currentScale, desiredScale, 14f * deltaTime);
    }
}
