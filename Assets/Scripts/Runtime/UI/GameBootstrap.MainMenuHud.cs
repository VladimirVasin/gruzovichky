using System.Collections;
using System.Globalization;
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
        public Button NewGameClearButton;
        public Text NewGameClearButtonText;
        public MainMenuButtonFx ContinueButtonFx;
        public MainMenuButtonFx NewGameButtonFx;
        public MainMenuButtonFx NewGameUserButtonFx;
        public MainMenuButtonFx NewGameClearButtonFx;
        public MainMenuButtonFx PatchNotesButtonFx;
        public MainMenuButtonFx ExitButtonFx;
        public MainMenuButtonFx EnglishButtonFx;
        public MainMenuButtonFx RussianButtonFx;
        public Button           GraphicsButton;
        public Text             GraphicsButtonText;
        public MainMenuButtonFx GraphicsButtonFx;
        public GameObject       GraphicsOptionsRoot;
        public Button[]         GfxToggleButtons = new Button[4];
        public Text[]           GfxToggleTexts   = new Text[4];
        public Button[]         GfxMinusButtons  = new Button[9];
        public Button[]         GfxPlusButtons   = new Button[9];
        public InputField[]     GfxValueFields   = new InputField[9];
    }

    private MainMenuHudRefs mainMenuHud;
    private GameObject loadingOverlayCanvas;
    private Image loadingBarFill;
    private Text loadingStatusText;
    private static readonly bool IsUserModeTemporarilyDisabled = false;
    private const string UserModeWorkInProgressLabel = "Work in progress";
    private const string MainMenuVersionLabel = "Lo-fi Delivery Co. v.0.0.3";
    private const string PatchNotesButtonLabel = "Patch Notes";
    private bool isGameStarted;
    private GameStartMode selectedGameStartMode = GameStartMode.Debug;
    private bool isWorldBuilt;
    private bool isTutorialOpen;
    private bool isTutorialSkipped;
    private bool hasShownWelcomeTutorial;
    private TutorialTrigger? pendingTutorialTrigger;
    private float pendingTutorialDelay;
    private bool hasShownUserCoreBuildingsTutorial;
    private bool hasShownUserWarehouseBuiltTutorial;
    private bool hasShownUserMotelBuiltTutorial;
    private bool hasShownUserParkingBuiltTutorial;
    private int nextUserCoreBuildingInfoTutorialStep = 4;
    private bool hasShownUserBuildLumberjackCampTutorial;
    private bool hasShownUserLumberjackCampBuiltTutorial;
    private bool hasShownUserLumberjackWorkerAssignedTutorial;
    private bool hasShownUserWorkerShiftInfoTutorial;
    private bool hasShownUserLogisticsSetupTutorial;
    private bool hasShownUserBuyTruckTutorial;
    private bool hasShownUserTruckArrivalTutorial;
    private bool hasShownUserTruckFreightTutorial;
    private bool hasShownUserWorkersLeisureTutorial;
    private bool hasShownUserBuildServiceBuildingsTutorial;
    private bool hasShownUserBarBuiltTutorial;
    private bool hasShownUserCanteenBuiltTutorial;
    private bool hasShownUserGasStationBuiltTutorial;
    private bool hasShownUserGamblingHallBuiltTutorial;
    private bool hasShownUserCityParkBuiltTutorial;
    private bool hasShownUserWorkersOverviewTutorial;
    private bool hasShownUserWorkerHiringBusTutorial;
    private bool hasShownUserWarehouseLoadersTutorial;
    private bool hasShownUserLocalTransportTutorial;
    private bool hasShownUserLocalBusRoutesTutorial;
    private bool hasShownUserEconomyTaxesTutorial;
    private bool hasShownUserTradeIntroTutorial;
    private bool hasShownUserTradeRaceTutorial;
    private bool hasShownUserDemoCompleteTutorial;
    private int nextUserServiceBuildingInfoTutorialStep = 15;
    private bool isLoadingWorld;
    private static GameStartMode? pendingAutoStartMode;
    // mainMenuMusicLoadCoroutine removed (music disabled)

    private void SetupMainMenuHud()
    {
        if (mainMenuHud != null)
        {
            return;
        }

        EnsureFleetEventSystem(); // buttons require an EventSystem - create it eagerly
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mainMenuHud = new MainMenuHudRefs();

        GameObject canvasObject = new("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;   // above tutorial canvas (80) so ESC в†’ main menu covers it

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
        window.sizeDelta = new Vector2(360f, 436f);
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
        buttonStack.gameObject.AddComponent<LayoutElement>().preferredHeight = 318f;

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

        mainMenuHud.NewGameClearButton = CreateButton("NewGameClearButton", buttonStack, uiFont, out mainMenuHud.NewGameClearButtonText, "New Game Clear", 18, new Color(0.34f, 0.28f, 0.52f, 1f), Color.white);
        mainMenuHud.NewGameClearButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
        mainMenuHud.NewGameClearButtonFx = SetupMainMenuButtonFx(mainMenuHud.NewGameClearButton, new Color(0.34f, 0.28f, 0.52f, 1f), new Color(0.48f, 0.38f, 0.70f, 1f));
        mainMenuHud.NewGameClearButton.onClick.AddListener(() => StartGameFromMainMenu(GameStartMode.Clear));

        mainMenuHud.PatchNotesButton = CreateButton("PatchNotesButton", buttonStack, uiFont, out mainMenuHud.PatchNotesButtonText, PatchNotesButtonLabel, 18, new Color(0.27f, 0.33f, 0.43f, 1f), Color.white);
        mainMenuHud.PatchNotesButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        mainMenuHud.PatchNotesButtonFx = SetupMainMenuButtonFx(mainMenuHud.PatchNotesButton, new Color(0.27f, 0.33f, 0.43f, 1f), new Color(0.40f, 0.48f, 0.62f, 1f));
        mainMenuHud.PatchNotesButton.onClick.AddListener(OpenMainMenuPatchNotes);

        bool ru = IsRussianLanguage();
        mainMenuHud.GraphicsButton = CreateButton("GraphicsButton", buttonStack, uiFont, out mainMenuHud.GraphicsButtonText,
            ru ? "Графика" : "Graphics", 18, new Color(0.20f, 0.30f, 0.42f, 1f), Color.white);
        mainMenuHud.GraphicsButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        mainMenuHud.GraphicsButtonFx = SetupMainMenuButtonFx(mainMenuHud.GraphicsButton,
            new Color(0.20f, 0.30f, 0.42f, 1f), new Color(0.30f, 0.44f, 0.60f, 1f));
        mainMenuHud.GraphicsButton.onClick.AddListener(ToggleGraphicsOptionsPanel);

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
        LoadGraphicsPrefs();
        CreateGraphicsOptionsPanel(screenTint, uiFont);
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
        UpdateMainMenuButtonFx(mainMenuHud.NewGameClearButtonFx);
        UpdateMainMenuButtonFx(mainMenuHud.PatchNotesButtonFx);
        UpdateMainMenuButtonFx(mainMenuHud.GraphicsButtonFx);
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
        if (mainMenuHud.NewGameClearButtonText != null) mainMenuHud.NewGameClearButtonText.text = L("New Game Clear");
        if (mainMenuHud.PatchNotesButtonText != null) mainMenuHud.PatchNotesButtonText.text = PatchNotesButtonLabel;
        if (mainMenuHud.GraphicsButtonText != null) mainMenuHud.GraphicsButtonText.text = IsRussianLanguage() ? "Графика" : "Graphics";
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
                ? "Заметки о версиях сгруппированы по релизам. Здесь перечислены только игровые изменения, которые важны игроку."
                : "Release notes are grouped by version. This screen lists player-facing gameplay changes only.",
            13,
            FleetSecondaryTextColor,
            FontStyle.Normal);

        AddPatchNotesSection(uiFont, ru ? "v.0.0.2 - новые системы города" : "v.0.0.2 - New town systems");
        AddPatchNotesParagraph(uiFont, ru
            ? "- ДОБАВЛЕНЫ ЖЕНЩИНЫ!!!\n\nРабочие\n- Рабочие получили портреты, пол, навыки, потребности, эффекты и перки.\n- Еда, сон и досуг теперь являются частью жизненного цикла рабочих.\n- Перк Алкоголизм получил отдельное поведение через опьянение и похмелье.\n\nПроизводство\n- Лесозаготовка стала активной производственной цепочкой: рабочие рубят деревья, переносят брёвна и высаживают новые саженцы.\n- Деревья в лесных зонах растут по дням, создавая возобновляемый источник древесины.\n\nТранспорт\n- Добавлена городская автобусная система: остановки, водители автобусов, маршруты и пассажирские поездки рабочих.\n- Автобусы берут плату за проезд, копят выручку и передают её в Parking.\n- Грузовики перевозят ресурсы между зданиями и могут отправляться в междугородние торговые рейсы.\n\nЭкономика и торговля\n- Окно Экономика разделено на Налоги и Торговлю.\n- Добавлен ежедневный сбор налогов с касс зданий в городскую казну.\n- Торговые заказы позволяют покупать и продавать ресурсы через междугородние рейсы.\n\nГород и сервисы\n- Bar, Canteen, Gambling Hall, Gas Station и City Park получили понятные роли в жизни рабочих и транспорта.\n- Сервисные здания стали заметнее визуально и лучше различаются ночью.\n- Добавлен Event Feed для важных городских событий.\n\nМир и атмосфера\n- Карта стала крупнее и плотнее: больше деревьев, кустов, лесных зон, воды, холмов и декоративной жизни.\n- Улучшены освещение, туман, вода, дороги, фонари, облака и ambient-детали.\n- Добавлен новый обучающий User-flow и свободный режим New Game Clear."
            : "- WOMEN ADDED!!!\n\nWorkers\n- Workers now have portraits, gender, skills, needs, effects, and perks.\n- Food, sleep, and leisure are now part of the worker life cycle.\n- The Alcoholism perk now has distinct Drunk and Hangover behavior.\n\nProduction\n- Lumberyard is now an active production chain: workers chop trees, carry logs, and plant new saplings.\n- Trees in forest zones grow over days, creating a renewable wood source.\n\nTransport\n- Added a local bus system with stops, bus drivers, routes, and worker passenger trips.\n- Buses charge fares, store route revenue, and transfer it into Parking.\n- Trucks move resources between buildings and can run intercity trade routes.\n\nEconomy and trade\n- Economy is split into Taxes and Trade tabs.\n- Daily taxes now collect money from building banks into the town Treasury.\n- Trade orders let the town buy and sell resources through intercity runs.\n\nTown services\n- Bar, Canteen, Gambling Hall, Gas Station, and City Park now have clearer roles in worker and vehicle life.\n- Service buildings are more visually distinct, especially at night.\n- Added an Event Feed for important town events.\n\nWorld and atmosphere\n- The map is larger and denser, with more trees, bushes, forest zones, water, hills, and ambient life.\n- Lighting, fog, water, roads, lanterns, clouds, and ambient details were improved.\n- Added a rebuilt User tutorial flow and the free New Game Clear mode.");

        AddPatchNotesSection(uiFont, ru ? "v.0.0.1 - базовый играемый прототип" : "v.0.0.1 - Base playable prototype");
        AddPatchNotesSection(uiFont, ru ? "Основной цикл" : "Core loop");
        AddPatchNotesParagraph(uiFont, ru
            ? "- Строй и соединяй небольшой логистический город на сетке.\n- Верхние HUD-вкладки открывают автопарк, рабочих, роли, ресурсы, экономику, стройку, торговлю и карту."
            : "- Build and connect a small logistics town on a generated grid.\n- Use the top HUD tabs to open Fleet, Workers, Roles, Resources, Economy, Build, Trade, and Map.");

        AddPatchNotesSection(uiFont, ru ? "Рабочие" : "Workers");
        AddPatchNotesParagraph(uiFont, ru
            ? "- Нанимай рабочих в меню Рабочие. Новые наёмники приезжают на автобусе.\n- У рабочих есть портреты, базовые навыки и таймеры потребностей: еда, сон, развлечение.\n- Назначай рабочих на производство через Роли."
            : "- Hire workers from the Workers menu. New hires arrive by bus before checking in.\n- Workers have generated portraits, base skills, and need timers for Food, Sleep, and Leisure.\n- Assign workers to jobs from Roles.");

        AddPatchNotesSection(uiFont, ru ? "Производство и ресурсы" : "Production and resources");
        AddPatchNotesParagraph(uiFont, ru
            ? "- Лес даёт Logs. Лесопилка делает Boards. Мебельная фабрика делает Furniture из Boards и Textile.\n- Склад хранит готовые ресурсы и поддерживает локальный ресурсный цикл.\n- Canteen, Bar и Motel используются в жизненном цикле рабочих."
            : "- Forest produces Logs. Sawmill turns Logs into Boards. Furniture Factory turns Boards and Textile into Furniture.\n- Warehouse stores finished resources and supports the local resource loop.\n- Canteen, Bar, and Motel are service buildings used by worker life-cycle needs.");

        AddPatchNotesSection(uiFont, ru ? "Логистика и торговля" : "Logistics and trade");
        AddPatchNotesParagraph(uiFont, ru
            ? "- Назначай водителей на грузовики и смены через Роли.\n- Создавай торговые заказы и отправляй междугородние рейсы через магистраль.\n- Торговые рейсы могут открывать режим Join the Race."
            : "- Assign drivers to trucks and shifts from Roles.\n- Create trade orders and send intercity runs through the highway.\n- Trade runs can open the Join the Race mode.");

        AddPatchNotesSection(uiFont, ru ? "Мир и атмосфера" : "World and atmosphere");
        AddPatchNotesParagraph(uiFont, ru
            ? "- Процедурный мир включает дороги, здания, реку, магистраль, автобусы и декоративную жизнь.\n- На карте есть лавочки, цветы, пчёлы, коты, птицы, рыбы, частицы, фонари и ночные мошки.\n- Региональная карта показывает текущий город и соседние регионы-заглушки."
            : "- The procedural world includes roads, buildings, river, highway traffic, buses, and visual ambience.\n- Benches, flowers, bees, cats, birds, fish, particles, lanterns, and night moths bring the map to life.\n- Regional Map shows the current town and placeholder neighboring regions.");
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
        ResetTutorialFlowForNewGame();
        ResetTutorialGoalsForNewGame();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        AudioListener.pause = false;
        mainMenuMusicSource?.Pause();
        StartCityMusic();
        UpdateDayNightCycle(0f);
        UpdateMainMenuHud();
        PlayUiSound(uiPanelOpenClip, 0.9f);
        if (selectedGameStartMode == GameStartMode.User)
        {
            ScheduleTutorial(TutorialTrigger.UserWelcome, 2f);
        }

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

        // No full-screen overlay - main menu stays visible underneath
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
        // barBg is full width (anchored 0в†’1 horizontally, sizeDelta.x=0)
        // We drive fill via anchorMax.x on the fill rect
        RectTransform rt = loadingBarFill.rectTransform;
        rt.anchorMax = new Vector2(Mathf.Clamp01(t), 1f);
        if (loadingStatusText != null) loadingStatusText.text = L(status);
    }

    private IEnumerator BuildPrototypeSceneAsync()
    {
        SessionDebugLogger.Log("BOOT", $"Building world for {selectedGameStartMode} mode.");

        isFarZoomVisualLodActive = false;
        shadowLodRenderers.Clear();
        gridLinesRoot = null;
        ResetSquirrelMemorialWorldState();
        worldRoot = new GameObject("PrototypeWorld").transform;
        roadsRoot = new GameObject("Roads").transform;
        roadsRoot.SetParent(worldRoot, false);
        lanternsRoot = new GameObject("RoadLanterns").transform;
        lanternsRoot.SetParent(worldRoot, false);
        roadsidePropsRoot = new GameObject("RoadsideProps").transform;
        roadsidePropsRoot.SetParent(worldRoot, false);
        roadsideSignsRoot = new GameObject("RoadsideSigns").transform;
        roadsideSignsRoot.SetParent(worldRoot, false);
        miscRoot = new GameObject("Misc").transform;
        miscRoot.SetParent(worldRoot, false);

        const int totalSteps = 25;
        int step = 0;

        SetLoadingProgress(++step / (float)totalSteps, "Camera & lighting..."); yield return null;
        SetupCamera(); SetupLighting(); SetupDioramaPostProcessing(); SetupSurfaceMaterials();

        SetLoadingProgress(++step / (float)totalSteps, "Populating water..."); yield return null;
        GenerateNaturalZones(); PopulateWaterCells();

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
        RebuildUnifiedRoadVisuals();

        SetLoadingProgress(++step / (float)totalSteps, "Setting up ground..."); yield return null;
        yield return StartCoroutine(SetupGroundAsync()); CreateWaterLayer();

        SetLoadingProgress(++step / (float)totalSteps, "Building grid..."); yield return null;
        yield return StartCoroutine(SetupGridAsync());

        SetLoadingProgress(++step / (float)totalSteps, "Edge highways..."); yield return null;
        SetupEdgeHighway(); SetupEdgeHighwayBuses(); SetupLocalBusRuntime(); SetupRiverBoats();

        SetLoadingProgress(++step / (float)totalSteps, "Atmosphere..."); yield return null;
        SetupDistantClouds(); SetupAmbientAirParticles(); SetupExhaustSmoke();

        SetLoadingProgress(++step / (float)totalSteps, "Road lanterns..."); yield return null;
        RebuildRoadLanterns();

        SetLoadingProgress(++step / (float)totalSteps, "Planting trees..."); yield return null;
        PopulateMiscTrees(); PopulateLakeDecorations();

        SetLoadingProgress(++step / (float)totalSteps, "Placing benches..."); yield return null;
        RebuildRoadsideBenches();

        SetLoadingProgress(++step / (float)totalSteps, "Road signs..."); yield return null;
        RebuildRoadSigns();

        SetLoadingProgress(++step / (float)totalSteps, "Wildlife..."); yield return null;
        SetupMiscBirds(); SetupAmbientCats(); SetupAmbientSquirrels(); SetupAmbientBees(); SetupAmbientLanternMoths(); SetupAmbientFallingLeaves(); SetupAmbientFireflies(); SetupAmbientFrogs(); SetupRiverFish(); SetupLakeFish(); SetupNightSky(); SetupWeatherSystem();

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
        SetupTruckQuickHud(); SetupLocalBusQuickHud(); SetupDriverQuickHud(); SetupBuildingQuickHud(); SetupCellQuickHud();

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

    private void CreateGraphicsOptionsPanel(RectTransform screenTint, Font uiFont)
    {
        bool ru = IsRussianLanguage();
        RectTransform panel = CreateStyledPanel("GraphicsOptionsPanel", screenTint, FleetPanelColor);
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 0f);
        panel.pivot     = new Vector2(0f, 0f);
        panel.anchoredPosition = new Vector2(428f, 34f);
        panel.sizeDelta = new Vector2(690f, 548f);
        mainMenuHud.GraphicsOptionsRoot = panel.gameObject;
        mainMenuHud.GraphicsOptionsRoot.SetActive(false);

        VerticalLayoutGroup vl = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(18, 18, 14, 14);
        vl.spacing = 8f;
        vl.childControlWidth  = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        Text title = CreateBodyText("GfxTitle", panel, uiFont,
            ru ? "Настройки графики" : "Graphics Settings",
            18, TextAnchor.MiddleLeft, new Color(0.94f, 0.97f, 1f));
        title.fontStyle = FontStyle.Bold;
        title.raycastTarget = false;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        Text hint = CreateBodyText("GfxHint", panel, uiFont,
            ru
                ? "Значения применяются сразу. Дефолт — обычный вид, 100 — усиленный максимум."
                : "Values apply immediately. Defaults are the normal look; 100 is boosted maximum.",
            12, TextAnchor.MiddleLeft, new Color(0.70f, 0.78f, 0.86f));
        hint.raycastTarget = false;
        hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        CreateGfxSectionLabel("GfxPostFxSection", panel, uiFont, ru ? "Постэффекты" : "Post Effects");
        CreateGfxOptionRow("GfxBloomRow",      panel, uiFont, ru ? "Bloom: сила свечения" : "Bloom intensity", -1, 0);
        CreateGfxOptionRow("GfxBloomReachRow", panel, uiFont, ru ? "Bloom: дальность"       : "Bloom reach",     -1, 1);
        CreateGfxOptionRow("GfxSatRow",        panel, uiFont, ru ? "Насыщенность"           : "Saturation",      -1, 2);
        CreateGfxOptionRow("GfxContrastRow",   panel, uiFont, ru ? "Контраст"                : "Contrast",        -1, 3);
        CreateGfxOptionRow("GfxWarmthRow",     panel, uiFont, ru ? "Теплота цвета"           : "Color warmth",    -1, 4);
        CreateGfxOptionRow("GfxVignetteRow",   panel, uiFont, ru ? "Виньетка"                : "Vignette",        -1, 5);

        CreateGfxSectionLabel("GfxToggleSection", panel, uiFont, ru ? "Включение эффектов" : "Effect Toggles");
        CreateGfxOptionRow("GfxDofRow",   panel, uiFont, ru ? "Глубина резкости"        : "Depth of Field",        3, 6);
        CreateGfxOptionRow("GfxGrainRow", panel, uiFont, ru ? "Зерно плёнки"            : "Film Grain",            0, 7);
        CreateGfxOptionRow("GfxSmhRow",   panel, uiFont, ru ? "Цветокоррекция"          : "Color Grading",         1, -1);
        CreateGfxOptionRow("GfxChromRow", panel, uiFont, ru ? "Хроматическая аберрация" : "Chromatic Aberration",  2, 8);

        Button resetButton = CreateButton("GfxResetDefaultsButton", panel, uiFont, out Text resetText,
            ru ? "Сбросить на дефолт" : "Reset to Defaults",
            14, new Color(0.36f, 0.20f, 0.08f, 1f), Color.white);
        resetButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
        resetText.fontStyle = FontStyle.Bold;
        resetText.raycastTarget = false;
        resetButton.onClick.AddListener(ResetGraphicsOptionsToDefaults);

        RefreshGraphicsOptionsPanelUI();
    }

    private static void CreateGfxSectionLabel(string name, Transform parent, Font uiFont, string labelText)
    {
        Text label = CreateBodyText(name, parent, uiFont, labelText, 13, TextAnchor.MiddleLeft, new Color(1f, 0.80f, 0.25f));
        label.fontStyle = FontStyle.Bold;
        label.raycastTarget = false;
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
    }

    private void CreateGfxOptionRow(string name, Transform parent, Font uiFont, string labelText, int toggleIndex, int valueIndex)
    {
        RectTransform row = CreateLayoutRow(name, parent, 38f, 8f);
        Image rowImage = row.gameObject.AddComponent<Image>();
        rowImage.color = new Color(0.10f, 0.14f, 0.20f, 0.92f);
        rowImage.raycastTarget = false;

        if (toggleIndex >= 0)
        {
            int idx = toggleIndex;
            mainMenuHud.GfxToggleButtons[idx] = CreateGfxToggleButton($"{name}Toggle", row, uiFont, out mainMenuHud.GfxToggleTexts[idx]);
            mainMenuHud.GfxToggleButtons[idx].onClick.AddListener(() => OnGfxToggle(idx));
        }
        else
        {
            CreateGfxSpacer(row, 104f, 32f);
        }

        Text label = CreateBodyText($"{name}Label", row, uiFont, labelText, 13, TextAnchor.MiddleLeft, new Color(0.86f, 0.91f, 0.97f));
        label.raycastTarget = false;
        label.gameObject.AddComponent<LayoutElement>().preferredWidth = 285f;

        if (valueIndex >= 0)
        {
            CreateGfxNumberStepper($"{name}Stepper", row, uiFont, valueIndex);
        }
        else
        {
            CreateGfxSpacer(row, 188f, 32f);
        }
    }

    private static void CreateGfxSpacer(Transform parent, float width, float height)
    {
        GameObject go = new("GfxSpacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        layout.flexibleWidth = 0f;
    }

    private static Button CreateGfxToggleButton(string name, Transform parent, Font uiFont, out Text label)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = 104f;
        layout.preferredHeight = 32f;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.14f, 0.42f, 0.22f, 1f);
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.22f, 0.58f, 0.32f, 1f);
        colors.pressedColor = new Color(0.08f, 0.20f, 0.12f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.15f, 0.15f, 0.16f, 0.75f);
        button.colors = colors;
        button.interactable = true;

        label = CreateBodyText("Label", go.transform, uiFont, "ON", 13, TextAnchor.MiddleCenter, Color.white);
        StretchRect(label.rectTransform, 0f, 0f, 0f, 0f);
        label.fontStyle = FontStyle.Bold;
        label.raycastTarget = false;
        return button;
    }

    private void CreateGfxNumberStepper(string name, Transform parent, Font uiFont, int valueIndex)
    {
        RectTransform root = CreateLayoutRow(name, parent, 32f, 5f);
        root.gameObject.AddComponent<LayoutElement>().preferredWidth = 188f;

        Button minus = CreateButton($"{name}Minus", root, uiFont, out Text minusText, "-", 13, new Color(0.16f, 0.19f, 0.26f, 1f), Color.white);
        minus.gameObject.AddComponent<LayoutElement>().preferredWidth = 40f;
        minusText.raycastTarget = false;
        minus.onClick.AddListener(() => StepGfxValue(valueIndex, -5f));
        mainMenuHud.GfxMinusButtons[valueIndex] = minus;

        InputField field = CreateGfxNumberInput($"{name}Value", root, uiFont);
        field.onEndEdit.AddListener(value => SetGfxValueFromText(valueIndex, value));
        mainMenuHud.GfxValueFields[valueIndex] = field;

        Button plus = CreateButton($"{name}Plus", root, uiFont, out Text plusText, "+", 13, new Color(0.16f, 0.19f, 0.26f, 1f), Color.white);
        plus.gameObject.AddComponent<LayoutElement>().preferredWidth = 40f;
        plusText.raycastTarget = false;
        plus.onClick.AddListener(() => StepGfxValue(valueIndex, 5f));
        mainMenuHud.GfxPlusButtons[valueIndex] = plus;
    }

    private static InputField CreateGfxNumberInput(string name, Transform parent, Font uiFont)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 88f;
        le.preferredHeight = 32f;

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.07f, 0.09f, 0.13f, 1f);
        bg.raycastTarget = true;

        Text text = CreateBodyText("Text", go.transform, uiFont, "100", 13, TextAnchor.MiddleCenter, Color.white);
        StretchRect(text.rectTransform, 4f, 4f, 4f, 4f);
        text.supportRichText = false;
        text.raycastTarget = false;

        InputField input = go.GetComponent<InputField>();
        input.textComponent = text;
        input.targetGraphic = bg;
        input.contentType = InputField.ContentType.IntegerNumber;
        input.lineType = InputField.LineType.SingleLine;
        input.characterValidation = InputField.CharacterValidation.Integer;
        input.caretColor = Color.white;
        input.selectionColor = new Color(0.40f, 0.58f, 0.90f, 0.45f);
        return input;
    }

    private void OnGfxToggle(int index)
    {
        switch (index)
        {
            case 0: gfxFilmGrainEnabled  = !gfxFilmGrainEnabled;  break;
            case 1: gfxSmhEnabled        = !gfxSmhEnabled;        break;
            case 2: gfxChromAberrEnabled = !gfxChromAberrEnabled; break;
            case 3: gfxDepthOfFieldEnabled = !gfxDepthOfFieldEnabled; break;
        }
        SessionDebugLogger.Log("UI", $"Graphics toggle {index} clicked. Grain={gfxFilmGrainEnabled}, Color={gfxSmhEnabled}, Aberration={gfxChromAberrEnabled}, DoF={gfxDepthOfFieldEnabled}.");
        SaveGraphicsPrefs();
        ApplyGraphicsOptionsNow();
        RefreshGraphicsOptionsPanelUI();
    }

    private void ToggleGraphicsOptionsPanel()
    {
        if (mainMenuHud?.GraphicsOptionsRoot == null) return;
        bool show = !mainMenuHud.GraphicsOptionsRoot.activeSelf;
        mainMenuHud.GraphicsOptionsRoot.SetActive(show);
        if (show)
        {
            mainMenuHud.GraphicsOptionsRoot.transform.SetAsLastSibling();
            RefreshGraphicsOptionsPanelUI();
        }
        PlayUiSound(show ? uiPanelOpenClip : uiPanelCloseClip, 0.9f);
    }

    private void RefreshGraphicsOptionsPanelUI()
    {
        if (mainMenuHud?.GfxToggleButtons == null) return;
        bool[] enabled = { gfxFilmGrainEnabled, gfxSmhEnabled, gfxChromAberrEnabled, gfxDepthOfFieldEnabled };
        Color onColor  = new Color(0.14f, 0.42f, 0.22f, 1f);
        Color offColor = new Color(0.28f, 0.28f, 0.32f, 1f);
        for (int i = 0; i < mainMenuHud.GfxToggleButtons.Length; i++)
        {
            if (mainMenuHud.GfxToggleButtons[i] == null) continue;
            mainMenuHud.GfxToggleTexts[i].text = enabled[i] ? "ON" : "OFF";
            mainMenuHud.GfxToggleButtons[i].interactable = true;
            ColorBlock cb = mainMenuHud.GfxToggleButtons[i].colors;
            cb.normalColor = enabled[i] ? onColor : offColor;
            cb.highlightedColor = enabled[i] ? new Color(0.18f, 0.52f, 0.28f, 1f) : new Color(0.36f, 0.36f, 0.42f, 1f);
            cb.pressedColor = new Color(0.10f, 0.12f, 0.16f, 1f);
            cb.selectedColor = cb.highlightedColor;
            mainMenuHud.GfxToggleButtons[i].colors = cb;
            if (mainMenuHud.GfxToggleButtons[i].targetGraphic is Image image)
            {
                image.raycastTarget = true;
                image.color = enabled[i] ? onColor : offColor;
            }
        }
        for (int i = 0; i < mainMenuHud.GfxValueFields.Length; i++)
        {
            SetGfxInputText(i, GetGfxValue(i));
        }
    }

    private void SetGfxInputText(int valueIndex, float value)
    {
        if (mainMenuHud?.GfxValueFields == null || valueIndex < 0 || valueIndex >= mainMenuHud.GfxValueFields.Length)
        {
            return;
        }

        InputField input = mainMenuHud.GfxValueFields[valueIndex];
        if (input == null) return;
        input.SetTextWithoutNotify(Mathf.RoundToInt(Mathf.Clamp01(value) * 100f).ToString(CultureInfo.InvariantCulture));
    }

    private void StepGfxValue(int valueIndex, float delta)
    {
        SetGfxPercentValue(valueIndex, Mathf.RoundToInt(GetGfxValue(valueIndex) * 100f + delta));
    }

    private void SetGfxValueFromText(int valueIndex, string rawValue)
    {
        if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) &&
            !int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
        {
            RefreshGraphicsOptionsPanelUI();
            return;
        }

        SetGfxPercentValue(valueIndex, value);
    }

    private float GetGfxValue(int valueIndex)
    {
        return valueIndex switch
        {
            0 => gfxBloomIntensity,
            1 => gfxBloomReach,
            2 => gfxSaturation,
            3 => gfxContrast,
            4 => gfxWarmth,
            5 => gfxVignette,
            6 => gfxDepthOfFieldAmount,
            7 => gfxFilmGrainIntensity,
            8 => gfxChromAberrIntensity,
            _ => 1f,
        };
    }

    private void SetGfxPercentValue(int valueIndex, int percent)
    {
        SetGfxValue(valueIndex, Mathf.Clamp(percent, 0, 100) / 100f);
    }

    private void SetGfxValue(int valueIndex, float value)
    {
        value = Mathf.Clamp01(value);
        switch (valueIndex)
        {
            case 0: gfxBloomIntensity = value; break;
            case 1: gfxBloomReach = value; break;
            case 2: gfxSaturation = value; break;
            case 3: gfxContrast = value; break;
            case 4: gfxWarmth = value; break;
            case 5: gfxVignette = value; break;
            case 6: gfxDepthOfFieldAmount = value; break;
            case 7: gfxFilmGrainIntensity = value; break;
            case 8: gfxChromAberrIntensity = value; break;
        }

        SaveGraphicsPrefs();
        ApplyGraphicsOptionsNow();
        RefreshGraphicsOptionsPanelUI();
    }

    private void ResetGraphicsOptionsToDefaults()
    {
        gfxFilmGrainEnabled = true;
        gfxSmhEnabled = true;
        gfxChromAberrEnabled = true;
        gfxDepthOfFieldEnabled = true;
        gfxBloomIntensity = GfxDefaultBloomIntensity;
        gfxBloomReach = GfxDefaultBloomReach;
        gfxSaturation = GfxDefaultSaturation;
        gfxContrast = GfxDefaultContrast;
        gfxWarmth = GfxDefaultWarmth;
        gfxVignette = GfxDefaultVignette;
        gfxDepthOfFieldAmount = GfxDefaultDepthOfField;
        gfxFilmGrainIntensity = GfxDefaultFilmGrain;
        gfxChromAberrIntensity = GfxDefaultChromAberr;

        SessionDebugLogger.Log("UI", "Graphics settings reset to defaults.");
        SaveGraphicsPrefs();
        ApplyGraphicsOptionsNow();
        RefreshGraphicsOptionsPanelUI();
    }

    private void LoadGraphicsPrefs()
    {
        gfxFilmGrainEnabled   = PlayerPrefs.GetInt("gfx_grain_on", 1) == 1;
        gfxSmhEnabled         = PlayerPrefs.GetInt("gfx_smh_on",   1) == 1;
        gfxChromAberrEnabled  = PlayerPrefs.GetInt("gfx_chrom_on", 1) == 1;
        gfxDepthOfFieldEnabled= PlayerPrefs.GetInt("gfx_dof_on",   1) == 1;
        gfxBloomIntensity     = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_bloom_intensity_v2", GfxDefaultBloomIntensity));
        gfxBloomReach         = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_bloom_reach_v2",     GfxDefaultBloomReach));
        gfxSaturation         = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_saturation_v2",      GfxDefaultSaturation));
        gfxContrast           = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_contrast_v2",        GfxDefaultContrast));
        gfxWarmth             = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_warmth_v2",          GfxDefaultWarmth));
        gfxVignette           = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_vignette_v2",        GfxDefaultVignette));
        gfxDepthOfFieldAmount = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_dof_amount_v2",      GfxDefaultDepthOfField));
        gfxFilmGrainIntensity = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_grain_percent_v2",   GfxDefaultFilmGrain));
        gfxChromAberrIntensity= Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_chrom_percent_v2",   GfxDefaultChromAberr));
    }

    private void SaveGraphicsPrefs()
    {
        PlayerPrefs.SetInt("gfx_grain_on",  gfxFilmGrainEnabled  ? 1 : 0);
        PlayerPrefs.SetInt("gfx_smh_on",    gfxSmhEnabled        ? 1 : 0);
        PlayerPrefs.SetInt("gfx_chrom_on",  gfxChromAberrEnabled ? 1 : 0);
        PlayerPrefs.SetInt("gfx_dof_on",    gfxDepthOfFieldEnabled ? 1 : 0);
        PlayerPrefs.SetFloat("gfx_bloom_intensity_v2", gfxBloomIntensity);
        PlayerPrefs.SetFloat("gfx_bloom_reach_v2",     gfxBloomReach);
        PlayerPrefs.SetFloat("gfx_saturation_v2",      gfxSaturation);
        PlayerPrefs.SetFloat("gfx_contrast_v2",        gfxContrast);
        PlayerPrefs.SetFloat("gfx_warmth_v2",          gfxWarmth);
        PlayerPrefs.SetFloat("gfx_vignette_v2",        gfxVignette);
        PlayerPrefs.SetFloat("gfx_dof_amount_v2",      gfxDepthOfFieldAmount);
        PlayerPrefs.SetFloat("gfx_grain_percent_v2",   gfxFilmGrainIntensity);
        PlayerPrefs.SetFloat("gfx_chrom_percent_v2",   gfxChromAberrIntensity);
        PlayerPrefs.Save();
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

