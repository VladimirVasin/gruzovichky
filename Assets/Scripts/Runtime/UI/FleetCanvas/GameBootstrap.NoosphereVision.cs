using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int NoosphereVisionMaxInsights = 5;
    private const int NoosphereVisionMaxSourceDots = 160;
    private const int NoosphereVisionMaxLines = 192;
    private const float NoosphereVisionEnterSeconds = 0.72f;
    private const float NoosphereVisionLeaveSeconds = 0.42f;
    private const float NoosphereVisionSlowTimeScale = 0.10f;

    private enum NoosphereVisionMode
    {
        Closed,
        Entering,
        Open,
        Leaving
    }

    private enum NoosphereVisionTone
    {
        Positive,
        Neutral,
        Negative,
        Split
    }

    private sealed class NoosphereVisionInsight
    {
        public string Key = string.Empty;
        public string TitleRu = string.Empty;
        public string TitleEn = string.Empty;
        public string SummaryRu = string.Empty;
        public string SummaryEn = string.Empty;
        public string SourceRu = string.Empty;
        public string SourceEn = string.Empty;
        public string EffectRu = string.Empty;
        public string EffectEn = string.Empty;
        public string ActionRu = string.Empty;
        public string ActionEn = string.Empty;
        public NoosphereVisionTone Tone = NoosphereVisionTone.Neutral;
        public SocialSignalCategory Category = SocialSignalCategory.City;
        public int Score;
        public int Strength;
        public int SourceCount;
        public readonly List<Vector3> SourceWorldPositions = new();
    }

    private sealed class NoosphereVisionInsightUi
    {
        public RectTransform Root;
        public Image Background;
        public Button Button;
        public Text TitleText;
        public Text MetaText;
        public Vector2 AnchoredPosition;
    }

    private sealed class NoosphereVisionSourceDotUi
    {
        public RectTransform Root;
        public Image Image;
        public int InsightIndex;
    }

    private sealed class NoosphereVisionLineUi
    {
        public RectTransform Rect;
        public Image Image;
    }

    private sealed class NoosphereVisionUiRefs
    {
        public GameObject CanvasRoot;
        public CanvasGroup CanvasGroup;
        public RectTransform FieldRoot;
        public Image DimImage;
        public RectTransform CoreRoot;
        public Image CoreImage;
        public Image CorePulseImage;
        public Image CoreInnerImage;
        public Text TitleText;
        public Text LeadText;
        public Text StatsText;
        public Text FooterText;
        public Button CloseButton;
        public Button JournalButton;
        public RectTransform DetailPanel;
        public Text DetailTitleText;
        public Text DetailBodyText;
        public Text DetailActionText;
        public Button DetailCloseButton;
        public readonly List<NoosphereVisionInsightUi> Insights = new();
        public readonly List<NoosphereVisionSourceDotUi> SourceDots = new();
        public readonly List<NoosphereVisionLineUi> Lines = new();
    }

    private NoosphereVisionMode noosphereVisionMode = NoosphereVisionMode.Closed;
    private NoosphereVisionUiRefs noosphereVisionUi;
    private readonly List<NoosphereVisionInsight> noosphereVisionInsights = new();
    private float noosphereVisionTransitionTimer;
    private float noosphereVisionAnimationTime;
    private int noosphereVisionSelectedInsightIndex = -1;
    private int noosphereVisionRestoreGameSpeed;
    private int noosphereVisionRestoreLastActiveSpeed;
    private float noosphereVisionRestoreTimeScale;
    private float noosphereVisionRestoreFixedDeltaTime;
    private Vector3 noosphereVisionSavedCameraFocus;
    private Vector3 noosphereVisionSavedCameraOffset;
    private Vector3 noosphereVisionSavedCameraTargetOffset;
    private Vector3 noosphereVisionSavedCameraPosition;
    private Quaternion noosphereVisionSavedCameraRotation;
    private bool noosphereVisionSavedTruckCameraFocused;
    private bool noosphereVisionSavedCameraReturning;
    private bool noosphereVisionSavedCameraRotating;
    private static Sprite noosphereVisionRingSprite;
    private static Sprite noosphereVisionSoftDotSprite;

    private bool IsNoosphereVisionInputBlocking()
    {
        return noosphereVisionMode != NoosphereVisionMode.Closed;
    }

    private void BeginNoosphereVision()
    {
        if (noosphereVisionMode != NoosphereVisionMode.Closed)
        {
            BeginNoosphereVisionExit();
            return;
        }

        SetupNoosphereVisionUi();
        CloseRegularPanelsForNoosphereVision();
        SaveNoosphereVisionCameraState();
        ApplyNoosphereVisionSlowdown();
        BuildNoosphereVisionSnapshot();
        noosphereVisionSelectedInsightIndex = -1;
        noosphereVisionMode = NoosphereVisionMode.Entering;
        noosphereVisionTransitionTimer = 0f;
        noosphereVisionAnimationTime = 0f;
        noosphereVisionUi.CanvasRoot.SetActive(true);
        noosphereVisionUi.CanvasGroup.alpha = 0f;
        noosphereVisionUi.CanvasGroup.blocksRaycasts = true;
        noosphereVisionUi.CanvasGroup.interactable = true;
        ApplyNoosphereVisionUi(0f);
        ApplyNoosphereVisionCamera(0f);
        PlayUiSound(uiPanelOpenClip, 0.72f);
        LogUiInput("Noosphere Vision: opened city meaning view");
    }

    private void BeginNoosphereVisionExit()
    {
        if (noosphereVisionMode == NoosphereVisionMode.Closed ||
            noosphereVisionMode == NoosphereVisionMode.Leaving)
        {
            return;
        }

        noosphereVisionMode = NoosphereVisionMode.Leaving;
        noosphereVisionTransitionTimer = 0f;
        PlayUiSound(uiPanelCloseClip, 0.62f);
    }

    private void CloseNoosphereVisionImmediate()
    {
        if (noosphereVisionMode == NoosphereVisionMode.Closed)
        {
            return;
        }

        noosphereVisionMode = NoosphereVisionMode.Closed;
        noosphereVisionTransitionTimer = 0f;
        noosphereVisionSelectedInsightIndex = -1;
        RestoreNoosphereVisionTimeScale();
        RestoreNoosphereVisionCameraState();

        if (noosphereVisionUi?.CanvasRoot != null)
        {
            noosphereVisionUi.CanvasRoot.SetActive(false);
            noosphereVisionUi.CanvasGroup.alpha = 0f;
            noosphereVisionUi.CanvasGroup.blocksRaycasts = false;
            noosphereVisionUi.CanvasGroup.interactable = false;
        }
    }

    private void UpdateNoosphereVisionRuntime()
    {
        if (noosphereVisionMode == NoosphereVisionMode.Closed)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            OpenNoosphereJournalFromVision();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame ||
            Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            BeginNoosphereVisionExit();
        }

        float dt = Time.unscaledDeltaTime;
        noosphereVisionAnimationTime += dt;

        if (noosphereVisionMode == NoosphereVisionMode.Entering)
        {
            noosphereVisionTransitionTimer += dt;
            float progress = Mathf.Clamp01(noosphereVisionTransitionTimer / NoosphereVisionEnterSeconds);
            float eased = SmoothNoosphereVisionProgress(progress);
            ApplyNoosphereVisionCamera(eased);
            ApplyNoosphereVisionUi(eased);
            if (progress >= 1f)
            {
                noosphereVisionMode = NoosphereVisionMode.Open;
                noosphereVisionTransitionTimer = 0f;
            }
        }
        else if (noosphereVisionMode == NoosphereVisionMode.Leaving)
        {
            noosphereVisionTransitionTimer += dt;
            float progress = Mathf.Clamp01(noosphereVisionTransitionTimer / NoosphereVisionLeaveSeconds);
            float eased = 1f - SmoothNoosphereVisionProgress(progress);
            ApplyNoosphereVisionCamera(eased);
            ApplyNoosphereVisionUi(eased);
            if (progress >= 1f)
            {
                CloseNoosphereVisionImmediate();
                return;
            }
        }
        else
        {
            ApplyNoosphereVisionCamera(1f);
            ApplyNoosphereVisionUi(1f);
        }

        AnimateNoosphereVisionUi();
    }

    private void ToggleNoosphereJournalPanel()
    {
        if (IsNoosphereVisionInputBlocking())
        {
            CloseNoosphereVisionImmediate();
        }

        bool wasOpen = isNoospherePanelOpen;
        if (isWorldMapPanelOpen)
        {
            CloseWorldMapPanel();
        }

        isFleetPanelOpen = false;
        isShiftsPanelOpen = false;
        isDriversPanelOpen = false;
        isResourcesPanelOpen = false;
        isEconomyPanelOpen = false;
        isTradePanelOpen = false;
        isBuildPanelOpen = false;
        isStatesPanelOpen = false;
        isSocialGraphPanelOpen = false;
        isCityHallPanelOpen = false;
        isNoospherePanelOpen = !wasOpen;
        activeBuildTool = BuildTool.None;
        hoveredBuildCell = null;
        HideBuildingQuickHudSubmenuImmediate();
        CancelRoadPathMode();
        CloseNoosphereDiveImmediate();
        isNoosphereScreenDirty = true;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
        isEconomyScreenDirty = true;
        isTradeScreenDirty = true;
        isBuildScreenDirty = true;
        isWorldMapScreenDirty = true;
        isStatesScreenDirty = true;
        isSocialGraphScreenDirty = true;
        isCityHallScreenDirty = true;
        RefreshSelectionVisuals();
        PlayUiSound(isNoospherePanelOpen ? uiPanelOpenClip : uiPanelCloseClip, 0.85f);
        LogUiInput(isNoospherePanelOpen ? "Noosphere Journal: opened with F9" : "Noosphere Journal: closed with F9");
    }

    private void OpenNoosphereJournalFromVision()
    {
        CloseNoosphereVisionImmediate();
        if (!isNoospherePanelOpen)
        {
            ToggleNoosphereJournalPanel();
        }
    }

    private void CloseRegularPanelsForNoosphereVision()
    {
        if (isWorldMapPanelOpen)
        {
            CloseWorldMapPanel();
        }

        isFleetPanelOpen = false;
        isShiftsPanelOpen = false;
        isDriversPanelOpen = false;
        isResourcesPanelOpen = false;
        isEconomyPanelOpen = false;
        isTradePanelOpen = false;
        isBuildPanelOpen = false;
        isStatesPanelOpen = false;
        isSocialGraphPanelOpen = false;
        isCityHallPanelOpen = false;
        isNoospherePanelOpen = false;
        isTruckDetailsOpen = false;
        isLocalBusDetailsOpen = false;
        isDriverDetailsOpen = false;
        activeBuildTool = BuildTool.None;
        hoveredBuildCell = null;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        HideBuildingQuickHudSubmenuImmediate();
        CancelRoadPathMode();
        CloseNoosphereDiveImmediate();
        isNoosphereScreenDirty = true;
        RefreshSelectionVisuals();
    }

    private void SaveNoosphereVisionCameraState()
    {
        noosphereVisionSavedCameraFocus = cameraFocusPoint;
        noosphereVisionSavedCameraOffset = cameraOffset;
        noosphereVisionSavedCameraTargetOffset = cameraTargetOffset;
        noosphereVisionSavedTruckCameraFocused = isTruckCameraFocused;
        noosphereVisionSavedCameraReturning = isCameraReturningToDiorama;
        noosphereVisionSavedCameraRotating = isCameraRotatingToTarget;
        if (mainCamera != null)
        {
            noosphereVisionSavedCameraPosition = mainCamera.transform.position;
            noosphereVisionSavedCameraRotation = mainCamera.transform.rotation;
        }

        isTruckCameraFocused = false;
        isCameraReturningToDiorama = false;
        isCameraRotatingToTarget = false;
    }

    private void RestoreNoosphereVisionCameraState()
    {
        cameraFocusPoint = noosphereVisionSavedCameraFocus;
        cameraOffset = noosphereVisionSavedCameraOffset;
        cameraTargetOffset = noosphereVisionSavedCameraTargetOffset;
        isTruckCameraFocused = noosphereVisionSavedTruckCameraFocused;
        isCameraReturningToDiorama = noosphereVisionSavedCameraReturning;
        isCameraRotatingToTarget = noosphereVisionSavedCameraRotating;
        if (mainCamera != null)
        {
            mainCamera.transform.position = noosphereVisionSavedCameraPosition;
            mainCamera.transform.rotation = noosphereVisionSavedCameraRotation;
        }
    }

    private void ApplyNoosphereVisionSlowdown()
    {
        noosphereVisionRestoreGameSpeed = gameSpeedMultiplier;
        noosphereVisionRestoreLastActiveSpeed = lastActiveGameSpeedMultiplier;
        noosphereVisionRestoreTimeScale = Time.timeScale;
        noosphereVisionRestoreFixedDeltaTime = Time.fixedDeltaTime;

        if (gameSpeedMultiplier > 0)
        {
            gameSpeedMultiplier = 1;
            Time.timeScale = NoosphereVisionSlowTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
    }

    private void PauseNoosphereVisionForFocusedInsight()
    {
        if (noosphereVisionRestoreGameSpeed <= 0)
        {
            return;
        }

        gameSpeedMultiplier = 0;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
    }

    private void ResumeNoosphereVisionSlowdown()
    {
        if (noosphereVisionRestoreGameSpeed <= 0)
        {
            return;
        }

        gameSpeedMultiplier = 1;
        Time.timeScale = NoosphereVisionSlowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void RestoreNoosphereVisionTimeScale()
    {
        gameSpeedMultiplier = noosphereVisionRestoreGameSpeed;
        lastActiveGameSpeedMultiplier = noosphereVisionRestoreLastActiveSpeed;
        Time.timeScale = noosphereVisionRestoreTimeScale;
        Time.fixedDeltaTime = noosphereVisionRestoreFixedDeltaTime;
    }

    private void ApplyNoosphereVisionCamera(float progress)
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector3 targetFocus = GetNoosphereVisionTargetFocus();
        Vector3 targetOffset = GetNoosphereVisionTargetOffset();
        cameraFocusPoint = Vector3.Lerp(noosphereVisionSavedCameraFocus, targetFocus, progress);
        cameraOffset = Vector3.Lerp(noosphereVisionSavedCameraOffset, targetOffset, progress);
        cameraTargetOffset = cameraOffset;
        mainCamera.transform.position = cameraFocusPoint + cameraOffset;
        mainCamera.transform.rotation = GetDioramaCameraRotation();
    }

    private static float SmoothNoosphereVisionProgress(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private void SetupNoosphereVisionUi()
    {
        if (noosphereVisionUi != null)
        {
            return;
        }

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        noosphereVisionUi = new NoosphereVisionUiRefs();

        GameObject canvasObject = new("NoosphereVisionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 44;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        noosphereVisionUi.CanvasRoot = canvasObject;
        noosphereVisionUi.CanvasGroup = canvasGroup;

        RectTransform field = CreateUiObject("NoosphereVisionField", canvasObject.transform).GetComponent<RectTransform>();
        StretchRect(field, 0f, 0f, 0f, 0f);
        noosphereVisionUi.FieldRoot = field;
        noosphereVisionUi.DimImage = field.gameObject.AddComponent<Image>();
        noosphereVisionUi.DimImage.color = new Color(0.01f, 0.015f, 0.030f, 0.46f);
        noosphereVisionUi.DimImage.raycastTarget = true;

        RectTransform header = CreateStyledPanel("NoosphereVisionHeader", field, new Color(0.025f, 0.035f, 0.055f, 0.84f));
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(0f, 1f);
        header.pivot = new Vector2(0f, 1f);
        header.anchoredPosition = new Vector2(28f, -28f);
        header.sizeDelta = new Vector2(560f, 128f);
        VerticalLayoutGroup headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
        headerLayout.padding = new RectOffset(18, 18, 12, 12);
        headerLayout.spacing = 5;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = false;
        noosphereVisionUi.TitleText = CreateHeaderText("Title", header, font, string.Empty, 24, TextAnchor.MiddleLeft, Color.white);
        noosphereVisionUi.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        noosphereVisionUi.LeadText = CreateBodyText("Lead", header, font, string.Empty, 14, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        noosphereVisionUi.LeadText.horizontalOverflow = HorizontalWrapMode.Wrap;
        noosphereVisionUi.LeadText.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
        noosphereVisionUi.StatsText = CreateBodyText("Stats", header, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        noosphereVisionUi.StatsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        CreateNoosphereVisionCore(field);

        CreateNoosphereVisionTopButtons(field, font);
        CreateNoosphereVisionDetailPanel(field, font);
        CreateNoosphereVisionFooter(field, font);
        CreateNoosphereVisionClarityUi(field, font);

        for (int i = 0; i < NoosphereVisionMaxInsights; i++)
        {
            noosphereVisionUi.Insights.Add(CreateNoosphereVisionInsightUi(field, font, i));
        }

        for (int i = 0; i < NoosphereVisionMaxSourceDots; i++)
        {
            noosphereVisionUi.SourceDots.Add(CreateNoosphereVisionSourceDotUi(field));
        }

        for (int i = 0; i < NoosphereVisionMaxLines; i++)
        {
            noosphereVisionUi.Lines.Add(CreateNoosphereVisionLineUi(field, $"VisionLine{i + 1}"));
        }

        canvasObject.SetActive(false);
    }

    private void CreateNoosphereVisionCore(RectTransform field)
    {
        noosphereVisionUi.CoreRoot = CreateUiObject("NoosphereVisionCore", field).GetComponent<RectTransform>();
        noosphereVisionUi.CoreRoot.anchorMin = new Vector2(0.5f, 0.5f);
        noosphereVisionUi.CoreRoot.anchorMax = new Vector2(0.5f, 0.5f);
        noosphereVisionUi.CoreRoot.pivot = new Vector2(0.5f, 0.5f);
        noosphereVisionUi.CoreRoot.sizeDelta = new Vector2(172f, 172f);

        noosphereVisionUi.CorePulseImage = CreateNoosphereVisionCoreSprite(
            "CorePulse",
            noosphereVisionUi.CoreRoot,
            172f,
            GetNoosphereVisionSoftDotSprite(),
            new Color(0.38f, 0.74f, 1f, 0.08f));

        noosphereVisionUi.CoreImage = CreateNoosphereVisionCoreSprite(
            "CoreRing",
            noosphereVisionUi.CoreRoot,
            136f,
            GetNoosphereVisionRingSprite(),
            new Color(0.62f, 0.86f, 1f, 0.50f));

        noosphereVisionUi.CoreInnerImage = CreateNoosphereVisionCoreSprite(
            "CoreInner",
            noosphereVisionUi.CoreRoot,
            28f,
            GetNoosphereVisionSoftDotSprite(),
            new Color(0.74f, 0.94f, 1f, 0.70f));
    }

    private static Image CreateNoosphereVisionCoreSprite(string name, RectTransform parent, float size, Sprite sprite, Color color)
    {
        RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite GetNoosphereVisionRingSprite()
    {
        if (noosphereVisionRingSprite != null)
        {
            return noosphereVisionRingSprite;
        }

        noosphereVisionRingSprite = CreateNoosphereVisionSprite(128, dist =>
        {
            float outer = 1f - Mathf.SmoothStep(0.86f, 0.96f, dist);
            float inner = Mathf.SmoothStep(0.68f, 0.78f, dist);
            float mainRing = Mathf.Clamp01(outer * inner);
            float echoRing = Mathf.Clamp01((1f - Mathf.SmoothStep(0.50f, 0.56f, dist)) * Mathf.SmoothStep(0.43f, 0.50f, dist)) * 0.45f;
            return Mathf.Max(mainRing, echoRing);
        });
        return noosphereVisionRingSprite;
    }

    private static Sprite GetNoosphereVisionSoftDotSprite()
    {
        if (noosphereVisionSoftDotSprite != null)
        {
            return noosphereVisionSoftDotSprite;
        }

        noosphereVisionSoftDotSprite = CreateNoosphereVisionSprite(128, dist =>
        {
            float core = 1f - Mathf.SmoothStep(0.0f, 0.22f, dist);
            float haze = (1f - Mathf.SmoothStep(0.0f, 1.0f, dist)) * 0.55f;
            return Mathf.Clamp01(core + haze);
        });
        return noosphereVisionSoftDotSprite;
    }

    private static Sprite CreateNoosphereVisionSprite(int size, System.Func<float, float> alphaForDistance)
    {
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color[] pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = dist <= 1f ? Mathf.Clamp01(alphaForDistance(dist)) : 0f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void CreateNoosphereVisionTopButtons(RectTransform field, Font font)
    {
        RectTransform actions = CreateLayoutRow("NoosphereVisionActions", field, 36f, 8f);
        actions.anchorMin = new Vector2(1f, 1f);
        actions.anchorMax = new Vector2(1f, 1f);
        actions.pivot = new Vector2(1f, 1f);
        actions.anchoredPosition = new Vector2(-28f, -28f);
        actions.sizeDelta = new Vector2(260f, 36f);

        noosphereVisionUi.JournalButton = CreateButton("JournalButton", actions, font, out Text journalText, string.Empty, 13, new Color(0.16f, 0.22f, 0.32f, 0.96f), Color.white);
        noosphereVisionUi.JournalButton.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        noosphereVisionUi.JournalButton.onClick.AddListener(OpenNoosphereJournalFromVision);
        journalText.text = IsRussianLanguage() ? "F9 \u0416\u0443\u0440\u043d\u0430\u043b" : "F9 Journal";

        noosphereVisionUi.CloseButton = CreateButton("CloseButton", actions, font, out Text closeText, "X", 14, new Color(0.36f, 0.12f, 0.12f, 0.96f), Color.white);
        noosphereVisionUi.CloseButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 44f;
        noosphereVisionUi.CloseButton.onClick.AddListener(BeginNoosphereVisionExit);
        closeText.text = "X";
    }

    private void CreateNoosphereVisionDetailPanel(RectTransform field, Font font)
    {
        RectTransform panel = CreateStyledPanel("NoosphereVisionDetail", field, new Color(0.035f, 0.048f, 0.074f, 0.94f));
        panel.anchorMin = new Vector2(1f, 0.5f);
        panel.anchorMax = new Vector2(1f, 0.5f);
        panel.pivot = new Vector2(1f, 0.5f);
        panel.anchoredPosition = new Vector2(-34f, -32f);
        panel.sizeDelta = new Vector2(430f, 342f);
        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 16, 16);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        noosphereVisionUi.DetailPanel = panel;
        noosphereVisionUi.DetailTitleText = CreateHeaderText("DetailTitle", panel, font, string.Empty, 19, TextAnchor.MiddleLeft, Color.white);
        noosphereVisionUi.DetailTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        noosphereVisionUi.DetailBodyText = CreateBodyText("DetailBody", panel, font, string.Empty, 13, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        noosphereVisionUi.DetailBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        noosphereVisionUi.DetailBodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 198f;
        noosphereVisionUi.DetailActionText = CreateBodyText("DetailAction", panel, font, string.Empty, 14, TextAnchor.UpperLeft, Color.white);
        noosphereVisionUi.DetailActionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        noosphereVisionUi.DetailActionText.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        noosphereVisionUi.DetailCloseButton = CreateButton("DetailClose", panel, font, out Text closeText, string.Empty, 13, new Color(0.18f, 0.23f, 0.30f, 1f), Color.white);
        noosphereVisionUi.DetailCloseButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        noosphereVisionUi.DetailCloseButton.onClick.AddListener(() =>
        {
            noosphereVisionSelectedInsightIndex = -1;
            ResumeNoosphereVisionSlowdown();
        });
        closeText.text = IsRussianLanguage() ? "\u0421\u043d\u044f\u0442\u044c \u0444\u043e\u043a\u0443\u0441" : "Clear focus";
    }

    private void CreateNoosphereVisionFooter(RectTransform field, Font font)
    {
        noosphereVisionUi.FooterText = CreateBodyText("NoosphereVisionFooter", field, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetMutedTextColor);
        RectTransform footer = noosphereVisionUi.FooterText.rectTransform;
        footer.anchorMin = new Vector2(0.5f, 0f);
        footer.anchorMax = new Vector2(0.5f, 0f);
        footer.pivot = new Vector2(0.5f, 0f);
        footer.anchoredPosition = new Vector2(0f, 24f);
        footer.sizeDelta = new Vector2(880f, 26f);
        noosphereVisionUi.FooterText.raycastTarget = false;
    }

    private NoosphereVisionInsightUi CreateNoosphereVisionInsightUi(RectTransform parent, Font font, int index)
    {
        NoosphereVisionInsightUi ui = new();
        RectTransform root = CreateStyledPanel($"NoosphereVisionInsight{index + 1}", parent, new Color(0.10f, 0.13f, 0.18f, 0.94f));
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(230f, 76f);
        VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 9, 8);
        layout.spacing = 2;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ui.Root = root;
        ui.Background = root.GetComponent<Image>();
        ui.Button = root.gameObject.AddComponent<Button>();
        ui.Button.targetGraphic = ui.Background;
        int insightIndex = index;
        ui.Button.onClick.AddListener(() => SelectNoosphereVisionInsight(insightIndex));
        ui.TitleText = CreateHeaderText("Title", root, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
        ui.TitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        ui.TitleText.verticalOverflow = VerticalWrapMode.Truncate;
        ui.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
        ui.MetaText = CreateBodyText("Meta", root, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        ui.MetaText.gameObject.AddComponent<LayoutElement>().preferredHeight = 17f;
        root.gameObject.SetActive(false);
        return ui;
    }

    private NoosphereVisionSourceDotUi CreateNoosphereVisionSourceDotUi(RectTransform parent)
    {
        NoosphereVisionSourceDotUi ui = new();
        RectTransform root = CreateUiObject("NoosphereVisionSourceDot", parent).GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(12f, 12f);
        Image image = root.gameObject.AddComponent<Image>();
        image.sprite = GetNoosphereVisionSoftDotSprite();
        image.color = Color.white;
        image.raycastTarget = false;
        Outline outline = root.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        outline.effectDistance = new Vector2(1f, -1f);
        root.gameObject.SetActive(false);
        ui.Root = root;
        ui.Image = image;
        return ui;
    }

    private NoosphereVisionLineUi CreateNoosphereVisionLineUi(RectTransform parent, string name)
    {
        RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
        rect.gameObject.SetActive(false);
        return new NoosphereVisionLineUi { Rect = rect, Image = image };
    }

    private void SelectNoosphereVisionInsight(int index)
    {
        if (index < 0 || index >= noosphereVisionInsights.Count)
        {
            return;
        }

        noosphereVisionSelectedInsightIndex = index;
        PauseNoosphereVisionForFocusedInsight();
        ApplyNoosphereVisionUi(1f);
        PlayUiSound(uiSelectClip, 0.7f);
    }

}
