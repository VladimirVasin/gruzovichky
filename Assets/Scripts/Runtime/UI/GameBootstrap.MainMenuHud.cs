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
        public Button ExitButton;
        public Text ExitButtonText;
        public Text LanguageLabelText;
        public Button EnglishButton;
        public Text EnglishButtonText;
        public Button RussianButton;
        public Text RussianButtonText;
        public MainMenuButtonFx ContinueButtonFx;
        public MainMenuButtonFx NewGameButtonFx;
        public MainMenuButtonFx NewGameUserButtonFx;
        public MainMenuButtonFx ExitButtonFx;
        public MainMenuButtonFx EnglishButtonFx;
        public MainMenuButtonFx RussianButtonFx;
    }

    private MainMenuHudRefs mainMenuHud;
    private GameObject loadingOverlayCanvas;
    private Image loadingBarFill;
    private Text loadingStatusText;
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
    private bool isShiftsHighlightPersistent;
    private enum TutorialCinematicPhase { None, TrackingBus, TrackingWorker, Returning }
    private TutorialCinematicPhase tutorialCinematicPhase;
    private DriverAgent             tutorialCinematicDriver;
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

        RectTransform window = CreateStyledPanel("MainMenuWindow", screenTint, FleetPanelColor);
        window.anchorMin = new Vector2(0f, 0f);
        window.anchorMax = new Vector2(0f, 0f);
        window.pivot = new Vector2(0f, 0f);
        window.anchoredPosition = new Vector2(48f, 58f);
        window.sizeDelta = new Vector2(360f, 270f);
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
        buttonStack.gameObject.AddComponent<LayoutElement>().preferredHeight = 156f;

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
        mainMenuHud.NewGameUserButtonFx = SetupMainMenuButtonFx(mainMenuHud.NewGameUserButton, new Color(0.22f, 0.46f, 0.58f, 1f), new Color(0.30f, 0.62f, 0.78f, 1f));
        mainMenuHud.NewGameUserButton.onClick.AddListener(() => StartGameFromMainMenu(GameStartMode.User));

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
        if (mainMenuHud.NewGameUserButtonText != null) mainMenuHud.NewGameUserButtonText.text = L("New Game User");
        if (mainMenuHud.ExitButtonText != null) mainMenuHud.ExitButtonText.text = L("Exit");
        if (mainMenuHud.LanguageLabelText != null) mainMenuHud.LanguageLabelText.text = L("Language:");
        if (mainMenuHud.EnglishButtonText != null) mainMenuHud.EnglishButtonText.text = selectedLanguage == GameLanguage.English ? "[Eng]" : "Eng";
        if (mainMenuHud.RussianButtonText != null) mainMenuHud.RussianButtonText.text = selectedLanguage == GameLanguage.Russian ? "[Rus]" : "Rus";
    }

    private void StartCityMusic()
    {
        if (cityMusicSource != null)
        {
            if (!cityMusicSource.isPlaying) cityMusicSource.UnPause();
            return;
        }
        AudioClip clip = Resources.Load<AudioClip>("City1");
        if (clip == null) return;
        cityMusicSource = CreateAudioSource("CityMusic", null, true, 0.20f, 0f, false);
        cityMusicSource.clip = clip;
        cityMusicSource.Play();
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
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        AudioListener.pause = false;
        mainMenuMusicSource?.Pause();
        StartCityMusic();
        UpdateDayNightCycle(0f);
        UpdateMainMenuHud();
        PlayUiSound(uiPanelOpenClip, 0.9f);
        TryShowTutorial(TutorialTrigger.GameStarted);
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
        SetupGround(); CreateWaterLayer();

        SetLoadingProgress(++step / (float)totalSteps, "Building grid..."); yield return null;
        SetupGrid();

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
        Color targetColor = fx.IsPressed ? fx.PressedColor : fx.IsHovered ? fx.HoverColor : fx.BaseColor;
        float targetScale = fx.IsPressed ? fx.PressedScale : fx.IsHovered ? fx.HoverScale : fx.BaseScale;

        fx.Image.color = Color.Lerp(fx.Image.color, targetColor, 12f * deltaTime);
        Vector3 currentScale = fx.RectTransform.localScale;
        Vector3 desiredScale = Vector3.one * targetScale;
        fx.RectTransform.localScale = Vector3.Lerp(currentScale, desiredScale, 14f * deltaTime);
    }
}
