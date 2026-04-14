using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
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
        public Button ExitButton;
        public Text ExitButtonText;
        public MainMenuButtonFx ContinueButtonFx;
        public MainMenuButtonFx NewGameButtonFx;
        public MainMenuButtonFx ExitButtonFx;
    }

    private MainMenuHudRefs mainMenuHud;
    private bool isGameStarted;
    // mainMenuMusicLoadCoroutine removed (music disabled)

    private void SetupMainMenuHud()
    {
        if (mainMenuHud != null)
        {
            return;
        }

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mainMenuHud = new MainMenuHudRefs();

        GameObject canvasObject = new("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

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
        window.sizeDelta = new Vector2(320f, 150f);
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
        buttonStack.gameObject.AddComponent<LayoutElement>().preferredHeight = 98f;

        mainMenuHud.ContinueButton = CreateButton("ContinueButton", buttonStack, uiFont, out mainMenuHud.ContinueButtonText, "Continue", 18, new Color(0.22f, 0.54f, 0.30f, 1f), Color.white);
        mainMenuHud.ContinueButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
        mainMenuHud.ContinueButtonFx = SetupMainMenuButtonFx(mainMenuHud.ContinueButton, new Color(0.22f, 0.54f, 0.30f, 1f), new Color(0.30f, 0.72f, 0.40f, 1f));
        mainMenuHud.ContinueButton.onClick.AddListener(ContinueGameFromMainMenu);

        mainMenuHud.NewGameButton = CreateButton("NewGameButton", buttonStack, uiFont, out mainMenuHud.NewGameButtonText, "New Game", 18, FleetPrimaryButtonColor, Color.white);
        mainMenuHud.NewGameButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
        mainMenuHud.NewGameButtonFx = SetupMainMenuButtonFx(mainMenuHud.NewGameButton, FleetPrimaryButtonColor, new Color(0.90f, 0.65f, 0.20f, 1f));
        mainMenuHud.NewGameButton.onClick.AddListener(StartGameFromMainMenu);

        mainMenuHud.ExitButton = CreateButton("ExitButton", buttonStack, uiFont, out mainMenuHud.ExitButtonText, "Exit", 18, new Color(0.31f, 0.35f, 0.43f, 1f), Color.white);
        mainMenuHud.ExitButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        mainMenuHud.ExitButtonFx = SetupMainMenuButtonFx(mainMenuHud.ExitButton, new Color(0.31f, 0.35f, 0.43f, 1f), new Color(0.42f, 0.47f, 0.57f, 1f));
        mainMenuHud.ExitButton.onClick.AddListener(ExitGameFromMainMenu);

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
        UpdateMainMenuButtonFx(mainMenuHud.ExitButtonFx);
    }

    private void StartGameFromMainMenu()
    {
        LogUiInput("Main Menu: clicked New Game");
        isMainMenuOpen = false;
        isGameStarted = true;
        gameSpeedMultiplier = 1;
        lastActiveGameSpeedMultiplier = 1;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        AudioListener.pause = false;
        UpdateMainMenuHud();
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }

    private void ContinueGameFromMainMenu()
    {
        LogUiInput("Main Menu: clicked Continue");
        isMainMenuOpen = false;
        gameSpeedMultiplier = lastActiveGameSpeedMultiplier > 0 ? lastActiveGameSpeedMultiplier : 1;
        Time.timeScale = gameSpeedMultiplier;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        AudioListener.pause = false;
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
