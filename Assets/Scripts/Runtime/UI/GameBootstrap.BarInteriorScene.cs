using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const float BarInteriorOpenSeconds = 0.82f;
    private const float BarInteriorCloseSeconds = 0.48f;

    private enum BarInteriorScenePhase
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    private sealed class BarInteriorSceneHudRefs
    {
        public GameObject CanvasRoot;
        public CanvasGroup RootGroup;
        public CanvasGroup SceneGroup;
        public Image DimImage;
        public RectTransform SceneRoot;
        public RawImage SceneImage;
        public Text TitleText;
        public Button ExitButton;
        public Text ExitButtonText;
    }

    private BarInteriorSceneHudRefs barInteriorSceneHud;
    private BarInteriorScenePhase barInteriorScenePhase = BarInteriorScenePhase.Closed;
    private bool isBarInteriorSceneOpen;
    private bool barInteriorScenePausedSimulation;
    private bool barInteriorSceneEnteredWhilePaused;
    private int barInteriorScenePreviousSpeed = 1;
    private float barInteriorSceneTimer;
    private float barInteriorLightTimer;
    private GameObject barInteriorRoot;
    private Camera barInteriorCamera;
    private RenderTexture barInteriorRenderTexture;
    private Light[] barInteriorPulseLights;

    private void StartBarInteriorScene(LocationData barLocation)
    {
        EnsureBarInteriorSceneHud();
        EnsureBarInteriorSceneWorld();
        PauseBarInteriorSceneSimulation();

        isBarInteriorSceneOpen = true;
        barInteriorScenePhase = BarInteriorScenePhase.Opening;
        barInteriorSceneTimer = 0f;
        barInteriorLightTimer = 0f;
        PrepareBarInteriorSceneAnimation();

        if (buildingQuickHud?.CanvasRoot != null)
        {
            buildingQuickHud.CanvasRoot.SetActive(false);
        }

        selectedLocation = null;
        selectedLocationInstanceId = 0;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        RefreshSelectionVisuals();

        string title = IsRussianLanguage() ? "\u0411\u0430\u0440" : "Bar";
        if (barLocation != null && !string.IsNullOrWhiteSpace(barLocation.Label))
        {
            title = IsRussianLanguage() ? "\u0412\u043d\u0443\u0442\u0440\u0438: \u0411\u0430\u0440" : $"Inside: {barLocation.Label}";
        }

        barInteriorSceneHud.TitleText.text = title;
        barInteriorSceneHud.ExitButtonText.text = IsRussianLanguage() ? "\u0412\u044b\u0439\u0442\u0438" : "Exit";
        barInteriorSceneHud.CanvasRoot.SetActive(true);
        barInteriorSceneHud.RootGroup.alpha = 1f;
        barInteriorSceneHud.RootGroup.blocksRaycasts = true;
        barInteriorSceneHud.RootGroup.interactable = true;
        barInteriorSceneHud.SceneGroup.alpha = 0f;
        barInteriorSceneHud.SceneRoot.localScale = Vector3.one * 0.94f;
        barInteriorSceneHud.DimImage.color = new Color(0f, 0f, 0f, 0f);

        barInteriorRoot.SetActive(true);
        barInteriorCamera.enabled = true;
        barInteriorCamera.Render();

        ValidateBarInteriorSceneClickTargets();
        LogUiInput("Quick HUD: entered Bar interior scene");
        PlayUiSound(uiPanelOpenClip != null ? uiPanelOpenClip : uiSelectClip, 0.8f);
    }

    private void EnsureBarInteriorSceneHud()
    {
        if (barInteriorSceneHud != null)
        {
            return;
        }

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        barInteriorSceneHud = new BarInteriorSceneHudRefs();

        GameObject canvasObject = new("BarInteriorSceneCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 92;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup rootGroup = canvasObject.AddComponent<CanvasGroup>();
        barInteriorSceneHud.CanvasRoot = canvasObject;
        barInteriorSceneHud.RootGroup = rootGroup;

        RectTransform dim = CreateUiObject("Dim", canvasObject.transform).GetComponent<RectTransform>();
        StretchRect(dim, 0f, 0f, 0f, 0f);
        Image dimImage = dim.gameObject.AddComponent<Image>();
        dimImage.color = Color.clear;
        dimImage.raycastTarget = true;
        barInteriorSceneHud.DimImage = dimImage;

        RectTransform sceneRoot = CreateUiObject("SceneRoot", canvasObject.transform).GetComponent<RectTransform>();
        StretchRect(sceneRoot, 0f, 0f, 0f, 0f);
        CanvasGroup sceneGroup = sceneRoot.gameObject.AddComponent<CanvasGroup>();
        barInteriorSceneHud.SceneRoot = sceneRoot;
        barInteriorSceneHud.SceneGroup = sceneGroup;

        RawImage sceneImage = sceneRoot.gameObject.AddComponent<RawImage>();
        sceneImage.color = Color.white;
        sceneImage.raycastTarget = false;
        barInteriorSceneHud.SceneImage = sceneImage;

        RectTransform topBar = CreateStyledPanel("TopBar", canvasObject.transform, new Color(0.02f, 0.03f, 0.04f, 0.74f));
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.anchoredPosition = Vector2.zero;
        topBar.sizeDelta = new Vector2(0f, 78f);

        RectTransform titleRect = CreateUiObject("Title", topBar).GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(36f, 0f);
        titleRect.offsetMax = new Vector2(-220f, 0f);
        Text title = titleRect.gameObject.AddComponent<Text>();
        title.font = font;
        title.fontSize = 26;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleLeft;
        title.color = new Color(1f, 0.86f, 0.42f, 1f);
        title.raycastTarget = false;
        barInteriorSceneHud.TitleText = title;

        Button exitButton = CreateButton("ExitButton", topBar, font, out Text exitText, "Exit", 15, new Color(0.45f, 0.16f, 0.13f, 1f), Color.white);
        RectTransform exitRect = exitButton.GetComponent<RectTransform>();
        exitRect.anchorMin = new Vector2(1f, 0.5f);
        exitRect.anchorMax = new Vector2(1f, 0.5f);
        exitRect.pivot = new Vector2(1f, 0.5f);
        exitRect.anchoredPosition = new Vector2(-34f, 0f);
        exitRect.sizeDelta = new Vector2(154f, 42f);
        exitText.fontStyle = FontStyle.Bold;
        exitText.raycastTarget = false;
        exitButton.onClick.AddListener(BeginCloseBarInteriorScene);
        barInteriorSceneHud.ExitButton = exitButton;
        barInteriorSceneHud.ExitButtonText = exitText;

        canvasObject.SetActive(false);
    }

    private void EnsureBarInteriorSceneWorld()
    {
        if (barInteriorRoot != null)
        {
            return;
        }

        barInteriorRenderTexture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32)
        {
            name = "BarInteriorSceneTexture",
            antiAliasing = 2
        };
        barInteriorRenderTexture.Create();
        if (barInteriorSceneHud?.SceneImage != null)
        {
            barInteriorSceneHud.SceneImage.texture = barInteriorRenderTexture;
        }

        barInteriorRoot = new GameObject("BarInteriorSceneWorld");
        barInteriorRoot.transform.position = new Vector3(6400f, 0f, 6400f);

        Transform room = new GameObject("LargeBarRoom").transform;
        room.SetParent(barInteriorRoot.transform, false);
        ResetBarInteriorAnimationState();

        CreateBarInteriorBox(room, "Floor", new Vector3(0f, -0.04f, 0f), new Vector3(15.8f, 0.08f, 10.8f), new Color(0.31f, 0.22f, 0.15f), VisualSmoothnessWood);
        CreateBarInteriorBox(room, "BackWall", new Vector3(0f, 1.55f, 4.95f), new Vector3(15.9f, 3.2f, 0.14f), new Color(0.20f, 0.09f, 0.08f), VisualSmoothnessBuildingWall);
        CreateBarInteriorBox(room, "LeftWall", new Vector3(-7.85f, 1.55f, 0f), new Vector3(0.14f, 3.2f, 10.8f), new Color(0.16f, 0.08f, 0.08f), VisualSmoothnessBuildingWall);
        CreateBarInteriorBox(room, "RightWall", new Vector3(7.85f, 1.55f, 0f), new Vector3(0.14f, 3.2f, 10.8f), new Color(0.16f, 0.08f, 0.08f), VisualSmoothnessBuildingWall);
        CreateBarInteriorBox(room, "StagePlatform", new Vector3(4.85f, 0.08f, 3.2f), new Vector3(3.4f, 0.18f, 2.05f), new Color(0.18f, 0.13f, 0.14f), VisualSmoothnessWood);
        CreateBarInteriorBox(room, "EntranceGlow", new Vector3(-6.8f, 1f, -4.85f), new Vector3(1.4f, 2f, 0.08f), new Color(0.98f, 0.70f, 0.32f), VisualSmoothnessGlass);

        CreateBarInteriorCozyDecor(room);
        CreateBarInteriorBarCounter(room);
        CreateBarInteriorTables(room);
        CreateBarInteriorPatrons(room);
        CreateBarInteriorLights(room);

        GameObject cameraObject = new("BarInteriorCamera");
        cameraObject.transform.SetParent(barInteriorRoot.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 2.55f, -4.65f);
        cameraObject.transform.LookAt(barInteriorRoot.transform.TransformPoint(new Vector3(0f, 1.02f, 1.35f)));
        barInteriorCamera = cameraObject.AddComponent<Camera>();
        barInteriorCamera.clearFlags = CameraClearFlags.SolidColor;
        barInteriorCamera.backgroundColor = new Color(0.045f, 0.035f, 0.035f, 1f);
        barInteriorCamera.fieldOfView = 60f;
        barInteriorCamera.nearClipPlane = 0.1f;
        barInteriorCamera.farClipPlane = 60f;
        barInteriorCamera.targetTexture = barInteriorRenderTexture;
        barInteriorCamera.enabled = false;

        barInteriorRoot.SetActive(false);
    }

    private void CreateBarInteriorBarCounter(Transform room)
    {
        Color wood = new(0.39f, 0.20f, 0.10f);
        Color trim = new(0.82f, 0.58f, 0.25f);
        CreateBarInteriorBox(room, "CounterBody", new Vector3(-2.1f, 0.48f, 3.62f), new Vector3(8.6f, 0.86f, 0.76f), wood, VisualSmoothnessWood);
        CreateBarInteriorBox(room, "CounterTop", new Vector3(-2.1f, 0.96f, 3.54f), new Vector3(8.9f, 0.16f, 0.94f), Color.Lerp(wood, Color.white, 0.08f), VisualSmoothnessWood);
        CreateBarInteriorBox(room, "CounterTrim", new Vector3(-2.1f, 0.82f, 3.08f), new Vector3(8.95f, 0.10f, 0.08f), trim, VisualSmoothnessVehicleMetal);
        CreateBarInteriorBox(room, "BackShelf", new Vector3(-2.2f, 1.42f, 4.78f), new Vector3(8.8f, 0.10f, 0.28f), new Color(0.22f, 0.11f, 0.06f), VisualSmoothnessWood);
        CreateBarInteriorBox(room, "BackShelfHigh", new Vector3(-2.2f, 2.03f, 4.78f), new Vector3(8.8f, 0.10f, 0.28f), new Color(0.22f, 0.11f, 0.06f), VisualSmoothnessWood);

        for (int i = 0; i < 14; i++)
        {
            float x = -6f + i * 0.58f;
            Color bottleColor = i % 3 == 0 ? new Color(0.18f, 0.55f, 0.38f) : i % 3 == 1 ? new Color(0.72f, 0.42f, 0.18f) : new Color(0.46f, 0.18f, 0.58f);
            CreateBarInteriorCylinder(room, $"Bottle{i}", new Vector3(x, 1.62f + (i % 2) * 0.60f, 4.62f), new Vector3(0.07f, 0.26f, 0.07f), bottleColor, VisualSmoothnessGlass);
            CreateBarInteriorCylinder(room, $"BottleCap{i}", new Vector3(x, 1.90f + (i % 2) * 0.60f, 4.62f), new Vector3(0.045f, 0.045f, 0.045f), new Color(0.92f, 0.78f, 0.36f), VisualSmoothnessVehicleMetal);
        }

        for (int i = 0; i < 5; i++)
        {
            float x = -5.2f + i * 0.92f;
            CreateBarInteriorStool(room, new Vector3(x, 0f, 2.48f), 180f);
        }

    }

    private void CreateBarInteriorTables(Transform room)
    {
        Vector3[] tablePositions =
        {
            new(-4.9f, 0f, -1.35f),
            new(-1.4f, 0f, -1.85f),
            new(2.45f, 0f, -1.25f),
            new(5.55f, 0f, 0.85f)
        };

        for (int i = 0; i < tablePositions.Length; i++)
        {
            CreateBarInteriorTableGroup(room, tablePositions[i], i * 18f);
        }

        CreateBarInteriorBox(room, "WideWalkway", new Vector3(0f, 0.012f, -4.15f), new Vector3(13.4f, 0.03f, 0.16f), new Color(0.72f, 0.50f, 0.20f), VisualSmoothnessWood);
        CreateBarInteriorBox(room, "DanceFloor", new Vector3(2.6f, 0.018f, 1.34f), new Vector3(3.4f, 0.035f, 1.9f), new Color(0.10f, 0.13f, 0.17f), VisualSmoothnessAsphalt);
    }

    private void CreateBarInteriorTableGroup(Transform room, Vector3 position, float yaw)
    {
        Transform group = new GameObject("TableGroup").transform;
        group.SetParent(room, false);
        group.localPosition = position;
        group.localRotation = Quaternion.Euler(0f, yaw, 0f);

        CreateBarInteriorCylinder(group, "TableTop", new Vector3(0f, 0.54f, 0f), new Vector3(0.64f, 0.08f, 0.64f), new Color(0.36f, 0.20f, 0.11f), VisualSmoothnessWood);
        CreateBarInteriorCylinder(group, "TableLeg", new Vector3(0f, 0.28f, 0f), new Vector3(0.08f, 0.28f, 0.08f), new Color(0.18f, 0.12f, 0.08f), VisualSmoothnessWood);
        CreateBarInteriorCylinder(group, "GlassA", new Vector3(-0.18f, 0.68f, 0.12f), new Vector3(0.06f, 0.10f, 0.06f), new Color(0.78f, 0.90f, 0.96f, 0.82f), VisualSmoothnessGlass);
        CreateBarInteriorCylinder(group, "GlassB", new Vector3(0.24f, 0.68f, -0.10f), new Vector3(0.06f, 0.10f, 0.06f), new Color(0.92f, 0.62f, 0.26f, 0.88f), VisualSmoothnessGlass);
        CreateBarInteriorTableCandle(group, new Vector3(0.08f, 0.74f, 0.05f), position.x + position.z);

        CreateBarInteriorStool(group, new Vector3(-0.92f, 0f, 0f), 90f);
        CreateBarInteriorStool(group, new Vector3(0.92f, 0f, 0f), -90f);
        CreateBarInteriorStool(group, new Vector3(0f, 0f, -0.92f), 0f);
        CreateBarInteriorStool(group, new Vector3(0f, 0f, 0.92f), 180f);
    }

    private void CreateBarInteriorPatrons(Transform room)
    {
        CreateBarInteriorPatron(room, "PatronCounterA", new Vector3(-4.4f, 0f, 1.98f), 188f, new Color(0.26f, 0.44f, 0.62f), new Color(0.20f, 0.12f, 0.06f), false, BarInteriorPatronRole.CounterDrinker, 1, 0.1f);
        CreateBarInteriorPatron(room, "PatronCounterB", new Vector3(-2.7f, 0f, 1.96f), 178f, new Color(0.70f, 0.28f, 0.22f), new Color(0.74f, 0.50f, 0.22f), false, BarInteriorPatronRole.CounterDrinker, 1, 1.4f);
        CreateBarInteriorPatron(room, "PatronTableA", new Vector3(-5.75f, 0f, -1.35f), 80f, new Color(0.26f, 0.58f, 0.34f), new Color(0.08f, 0.06f, 0.04f), true, BarInteriorPatronRole.TableTalker, 2, 2.2f);
        CreateBarInteriorPatron(room, "PatronTableB", new Vector3(-0.48f, 0f, -1.85f), -82f, new Color(0.62f, 0.34f, 0.68f), new Color(0.36f, 0.20f, 0.12f), true, BarInteriorPatronRole.TableTalker, 2, 3.1f);
        CreateBarInteriorPatron(room, "PatronDanceA", new Vector3(2.05f, 0f, 1.28f), -18f, new Color(0.92f, 0.66f, 0.20f), new Color(0.24f, 0.18f, 0.10f), false, BarInteriorPatronRole.Dancer, 3, 4.0f);
        CreateBarInteriorPatron(room, "PatronDanceB", new Vector3(3.08f, 0f, 1.06f), 28f, new Color(0.22f, 0.62f, 0.66f), new Color(0.58f, 0.38f, 0.18f), false, BarInteriorPatronRole.Dancer, 3, 5.4f);
        CreateBarInteriorPatron(room, "Bartender", new Vector3(-6.15f, 0f, 4.05f), -8f, new Color(0.92f, 0.86f, 0.70f), new Color(0.16f, 0.12f, 0.08f), false, BarInteriorPatronRole.Bartender, 0, 0.8f);
    }

    private BarInteriorPatronRefs CreateBarInteriorPatron(Transform room, string name, Vector3 position, float yaw, Color shirt, Color hair, bool seated, BarInteriorPatronRole role, int conversationGroup, float phase)
    {
        Transform root = new GameObject(name).transform;
        root.SetParent(room, false);
        root.localPosition = position;
        root.localRotation = Quaternion.Euler(0f, yaw, 0f);

        float bodyY = seated ? 0.68f : 0.88f;
        float legY = seated ? 0.34f : 0.34f;
        Transform body = CreateBarInteriorBox(root, "Body", new Vector3(0f, bodyY, 0f), new Vector3(0.34f, 0.58f, 0.22f), shirt, VisualSmoothnessFabric).transform;
        Transform head = CreateBarInteriorSphere(root, "Head", new Vector3(0f, bodyY + 0.46f, 0f), new Vector3(0.24f, 0.24f, 0.24f), new Color(0.96f, 0.78f, 0.62f), VisualSmoothnessSkin).transform;
        Transform hairVisual = CreateBarInteriorSphere(root, "Hair", new Vector3(0f, bodyY + 0.60f, -0.02f), new Vector3(0.25f, 0.13f, 0.24f), hair, VisualSmoothnessFabric).transform;
        Transform leftArm = CreateBarInteriorBox(root, "LeftArm", new Vector3(-0.26f, bodyY + 0.08f, 0.03f), new Vector3(0.09f, 0.36f, 0.09f), shirt * 0.92f, VisualSmoothnessFabric).transform;
        Transform rightArm = CreateBarInteriorBox(root, "RightArm", new Vector3(0.26f, bodyY + 0.08f, 0.03f), new Vector3(0.09f, 0.36f, 0.09f), shirt * 0.92f, VisualSmoothnessFabric).transform;
        Transform leftLeg = CreateBarInteriorBox(root, "LeftLeg", new Vector3(-0.09f, legY, 0f), new Vector3(0.10f, seated ? 0.30f : 0.56f, 0.10f), new Color(0.12f, 0.16f, 0.22f), VisualSmoothnessFabric).transform;
        Transform rightLeg = CreateBarInteriorBox(root, "RightLeg", new Vector3(0.09f, legY, 0f), new Vector3(0.10f, seated ? 0.30f : 0.56f, 0.10f), new Color(0.12f, 0.16f, 0.22f), VisualSmoothnessFabric).transform;

        BarInteriorPatronRefs patron = new()
        {
            Name = name,
            Root = root,
            Body = body,
            Head = head,
            Hair = hairVisual,
            LeftArm = leftArm,
            RightArm = rightArm,
            LeftLeg = leftLeg,
            RightLeg = rightLeg,
            Role = role,
            ConversationGroup = conversationGroup,
            Phase = phase,
            Seated = seated,
            RootBaseLocalPosition = root.localPosition,
            RootBaseLocalRotation = root.localRotation,
            BodyBaseLocalPosition = body.localPosition,
            HeadBaseLocalPosition = head.localPosition,
            HairBaseLocalPosition = hairVisual.localPosition,
            LeftArmBaseLocalPosition = leftArm.localPosition,
            RightArmBaseLocalPosition = rightArm.localPosition,
            BodyBaseLocalRotation = body.localRotation,
            HeadBaseLocalRotation = head.localRotation,
            HairBaseLocalRotation = hairVisual.localRotation,
            LeftArmBaseLocalRotation = leftArm.localRotation,
            RightArmBaseLocalRotation = rightArm.localRotation
        };
        CreateBarInteriorPatronGlass(patron);
        CreateBarInteriorSpeechBubble(patron);
        RegisterBarInteriorPatron(patron);
        return patron;
    }

    private void CreateBarInteriorLights(Transform room)
    {
        Color warm = new(1f, 0.64f, 0.28f);
        Vector3[] hangingLights =
        {
            new(-5.2f, 2.82f, -1.4f),
            new(-1.2f, 2.82f, -2.0f),
            new(2.8f, 2.82f, -0.9f),
            new(5.4f, 2.82f, 1.0f),
            new(-3.6f, 2.82f, 3.3f)
        };

        barInteriorPulseLights = new Light[hangingLights.Length + 1];
        for (int i = 0; i < hangingLights.Length; i++)
        {
            CreateBarInteriorCylinder(room, $"PendantCord{i}", hangingLights[i] + new Vector3(0f, 0.16f, 0f), new Vector3(0.025f, 0.24f, 0.025f), new Color(0.06f, 0.05f, 0.04f), VisualSmoothnessDefault);
            CreateBarInteriorSphere(room, $"PendantLamp{i}", hangingLights[i], new Vector3(0.20f, 0.12f, 0.20f), warm, VisualSmoothnessGlass);
            GameObject lightObject = new($"WarmLight{i}");
            lightObject.transform.SetParent(room, false);
            lightObject.transform.localPosition = hangingLights[i] + new Vector3(0f, -0.12f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = warm;
            light.range = 5.2f;
            light.intensity = 1.20f;
            light.shadows = LightShadows.None;
            barInteriorPulseLights[i] = light;
        }

        GameObject stageLightObject = new("StageLight");
        stageLightObject.transform.SetParent(room, false);
        stageLightObject.transform.localPosition = new Vector3(4.8f, 2.85f, 1.6f);
        stageLightObject.transform.localRotation = Quaternion.Euler(65f, -18f, 0f);
        Light stageLight = stageLightObject.AddComponent<Light>();
        stageLight.type = LightType.Spot;
        stageLight.color = new Color(0.62f, 0.76f, 1f);
        stageLight.range = 7f;
        stageLight.spotAngle = 42f;
        stageLight.intensity = 1.35f;
        stageLight.shadows = LightShadows.None;
        barInteriorPulseLights[barInteriorPulseLights.Length - 1] = stageLight;
    }

    private void UpdateBarInteriorSceneRuntime()
    {
        if (!isBarInteriorSceneOpen || barInteriorSceneHud == null)
        {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        UpdateBarInteriorLighting(dt);
        UpdateBarInteriorAmbientAnimations(dt);

        switch (barInteriorScenePhase)
        {
            case BarInteriorScenePhase.Opening:
                UpdateBarInteriorSceneOpening(dt);
                break;
            case BarInteriorScenePhase.Closing:
                UpdateBarInteriorSceneClosing(dt);
                break;
        }
    }

    private void UpdateBarInteriorSceneOpening(float dt)
    {
        barInteriorSceneTimer += dt;
        float t = Mathf.Clamp01(barInteriorSceneTimer / BarInteriorOpenSeconds);
        float eased = SmootherStep01(t);
        barInteriorSceneHud.DimImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.88f, eased));
        barInteriorSceneHud.SceneGroup.alpha = SmootherStep01(Mathf.Clamp01((t - 0.12f) / 0.88f));
        barInteriorSceneHud.SceneRoot.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);

        if (t < 1f)
        {
            return;
        }

        barInteriorScenePhase = BarInteriorScenePhase.Open;
    }

    private void UpdateBarInteriorSceneClosing(float dt)
    {
        barInteriorSceneTimer += dt;
        float t = Mathf.Clamp01(barInteriorSceneTimer / BarInteriorCloseSeconds);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        float alpha = 1f - eased;
        barInteriorSceneHud.RootGroup.alpha = alpha;
        barInteriorSceneHud.DimImage.color = new Color(0f, 0f, 0f, 0.88f * alpha);

        if (t < 1f)
        {
            return;
        }

        barInteriorSceneHud.CanvasRoot.SetActive(false);
        barInteriorSceneHud.RootGroup.alpha = 1f;
        barInteriorSceneHud.RootGroup.blocksRaycasts = false;
        barInteriorSceneHud.RootGroup.interactable = false;
        barInteriorSceneHud.SceneGroup.alpha = 0f;
        if (barInteriorCamera != null)
        {
            barInteriorCamera.enabled = false;
        }

        if (barInteriorRoot != null)
        {
            barInteriorRoot.SetActive(false);
        }

        isBarInteriorSceneOpen = false;
        barInteriorScenePhase = BarInteriorScenePhase.Closed;
        ResumeBarInteriorSceneSimulation();
    }

    private void BeginCloseBarInteriorScene()
    {
        if (!isBarInteriorSceneOpen || barInteriorScenePhase == BarInteriorScenePhase.Closing)
        {
            return;
        }

        barInteriorScenePhase = BarInteriorScenePhase.Closing;
        barInteriorSceneTimer = 0f;
        PlayUiSound(uiPanelCloseClip != null ? uiPanelCloseClip : uiSelectClip, 0.76f);
    }

    private void PauseBarInteriorSceneSimulation()
    {
        if (barInteriorScenePausedSimulation)
        {
            return;
        }

        barInteriorSceneEnteredWhilePaused = gameSpeedMultiplier <= 0;
        barInteriorScenePreviousSpeed = gameSpeedMultiplier > 0
            ? gameSpeedMultiplier
            : Mathf.Max(1, lastActiveGameSpeedMultiplier);
        if (!barInteriorSceneEnteredWhilePaused)
        {
            lastActiveGameSpeedMultiplier = barInteriorScenePreviousSpeed;
        }

        gameSpeedMultiplier = 0;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        barInteriorScenePausedSimulation = true;
    }

    private void ResumeBarInteriorSceneSimulation()
    {
        if (!barInteriorScenePausedSimulation)
        {
            return;
        }

        if (barInteriorSceneEnteredWhilePaused)
        {
            gameSpeedMultiplier = 0;
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;
        }
        else
        {
            int speed = Mathf.Max(1, barInteriorScenePreviousSpeed);
            gameSpeedMultiplier = speed;
            Time.timeScale = speed;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }

        barInteriorScenePausedSimulation = false;
    }

    private void UpdateBarInteriorLighting(float dt)
    {
        barInteriorLightTimer += dt;
        if (barInteriorPulseLights == null)
        {
            return;
        }

        for (int i = 0; i < barInteriorPulseLights.Length; i++)
        {
            Light light = barInteriorPulseLights[i];
            if (light == null)
            {
                continue;
            }

            light.intensity = i == barInteriorPulseLights.Length - 1
                ? 1.15f + Mathf.Sin(barInteriorLightTimer * 1.5f) * 0.18f
                : 1.10f + Mathf.Sin(barInteriorLightTimer * 1.2f + i * 0.73f) * 0.12f;
        }
    }

    private GameObject CreateBarInteriorBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, float smoothness)
    {
        return CreateBarInteriorPrimitive(parent, PrimitiveType.Cube, name, localPosition, localScale, color, smoothness);
    }

    private GameObject CreateBarInteriorCylinder(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, float smoothness)
    {
        return CreateBarInteriorPrimitive(parent, PrimitiveType.Cylinder, name, localPosition, localScale, color, smoothness);
    }

    private GameObject CreateBarInteriorSphere(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, float smoothness)
    {
        return CreateBarInteriorPrimitive(parent, PrimitiveType.Sphere, name, localPosition, localScale, color, smoothness);
    }

    private GameObject CreateBarInteriorPrimitive(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Color color, float smoothness)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        ApplyColor(go, color, smoothness);
        ConfigureStaticVisual(go, smoothness);
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        return go;
    }

    private void CreateBarInteriorStool(Transform parent, Vector3 localPosition, float yaw)
    {
        Transform stool = new GameObject("Stool").transform;
        stool.SetParent(parent, false);
        stool.localPosition = localPosition;
        stool.localRotation = Quaternion.Euler(0f, yaw, 0f);
        CreateBarInteriorCylinder(stool, "Seat", new Vector3(0f, 0.42f, 0f), new Vector3(0.24f, 0.07f, 0.24f), new Color(0.22f, 0.13f, 0.08f), VisualSmoothnessWood);
        CreateBarInteriorCylinder(stool, "Pedestal", new Vector3(0f, 0.22f, 0f), new Vector3(0.055f, 0.22f, 0.055f), new Color(0.72f, 0.58f, 0.32f), VisualSmoothnessVehicleMetal);
        CreateBarInteriorBox(stool, "FootRail", new Vector3(0f, 0.18f, 0.19f), new Vector3(0.42f, 0.035f, 0.035f), new Color(0.72f, 0.58f, 0.32f), VisualSmoothnessVehicleMetal);
    }

    private TextMesh CreateBarInteriorTextSign(Transform parent, string name, string text, Vector3 localPosition, float characterSize, Color color)
    {
        GameObject signObject = new(name);
        signObject.transform.SetParent(parent, false);
        signObject.transform.localPosition = localPosition;
        signObject.transform.localRotation = Quaternion.identity;
        TextMesh textMesh = signObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = 72;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = color;
        Renderer renderer = signObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        return textMesh;
    }

    private void ValidateBarInteriorSceneClickTargets()
    {
        bool ok = barInteriorSceneHud?.CanvasRoot != null &&
                  barInteriorSceneHud.CanvasRoot.GetComponent<GraphicRaycaster>() != null &&
                  IsButtonClickTargetReady(barInteriorSceneHud.ExitButton);
        if (!ok)
        {
            SessionDebugLogger.Log("UI_INPUT", "Bar interior scene click-target validation failed: check GraphicRaycaster and exit button.");
        }
    }

    private void ReleaseBarInteriorSceneResources()
    {
        if (barInteriorCamera != null && barInteriorCamera.targetTexture == barInteriorRenderTexture)
        {
            barInteriorCamera.targetTexture = null;
        }

        if (barInteriorRenderTexture != null)
        {
            barInteriorRenderTexture.Release();
            Destroy(barInteriorRenderTexture);
            barInteriorRenderTexture = null;
        }

        ResetBarInteriorAnimationState();
    }
}
