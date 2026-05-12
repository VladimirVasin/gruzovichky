using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int NoosphereDiveMaxMeanings = 72;
    private const int NoosphereDiveRingSegments = 96;
    private const float NoosphereDiveEnterSeconds = 1.18f;
    private const float NoosphereDiveLeaveSeconds = 0.62f;
    private const float NoosphereDiveRefreshSeconds = 0.85f;
    private static readonly Vector3 NoosphereDiveSceneOrigin = new(0f, -2200f, 0f);

    private enum NoosphereDiveMode
    {
        Closed,
        Entering,
        Open,
        Leaving
    }

    private enum NoosphereDiveMeaningKind
    {
        Knowledge,
        TopicOpinion,
        CityExperience,
        SocialSignal
    }

    private sealed class NoosphereDiveUiRefs
    {
        public GameObject CanvasRoot;
        public CanvasGroup CanvasGroup;
        public RawImage ViewImage;
        public Text TitleText;
        public Text StatsText;
        public Button CloseButton;
        public Camera Camera;
        public Transform SceneRoot;
        public Transform CoreRoot;
        public Renderer CoreRenderer;
        public Renderer ShellRenderer;
    }

    private sealed class NoosphereDiveMeaningModel
    {
        public string Key = string.Empty;
        public string Text = string.Empty;
        public NoosphereDiveMeaningKind Kind;
        public int Score;
        public int Confidence;
        public int Weight;
        public bool IsCanon;
        public bool IsBurned;
        public float Radius;
        public float Height;
        public float Phase;
        public float Speed;
        public float Size;
        public float Wobble;
        public Color Color;
    }

    private sealed class NoosphereDiveMeaningView
    {
        public Transform Root;
        public TextMesh Text;
        public LineRenderer Spoke;
        public NoosphereDiveMeaningModel Model;
    }

    private NoosphereDiveMode noosphereDiveMode = NoosphereDiveMode.Closed;
    private NoosphereDiveUiRefs noosphereDiveUi;
    private RenderTexture noosphereDiveTexture;
    private CanvasGroup noosphereScreenFadeGroup;
    private Material noosphereDiveLineMaterial;
    private Material noosphereDiveCoreMaterial;
    private Material noosphereDiveShellMaterial;
    private float noosphereDiveTransitionTimer;
    private float noosphereDiveRefreshTimer;
    private bool noosphereDiveDirty = true;
    private readonly List<NoosphereDiveMeaningModel> noosphereDiveMeanings = new();
    private readonly List<NoosphereDiveMeaningView> noosphereDiveMeaningViews = new();
    private readonly List<LineRenderer> noosphereDiveRings = new();

    private bool IsNoosphereDiveInputBlocking()
    {
        return noosphereDiveMode != NoosphereDiveMode.Closed;
    }

    private void MarkNoosphereDiveDirty()
    {
        noosphereDiveDirty = true;
    }

    private void BeginNoosphereDive()
    {
        if (!isNoospherePanelOpen)
        {
            return;
        }

        SetupNoosphereDiveUi();
        if (noosphereDiveUi == null)
        {
            return;
        }

        EnsureNoosphereScreenFadeGroup();
        noosphereDiveMode = NoosphereDiveMode.Entering;
        noosphereDiveTransitionTimer = 0f;
        noosphereDiveRefreshTimer = 0f;
        noosphereDiveDirty = true;
        noosphereDiveUi.CanvasRoot.SetActive(true);
        noosphereDiveUi.SceneRoot.gameObject.SetActive(true);
        noosphereDiveUi.CanvasGroup.blocksRaycasts = true;
        noosphereDiveUi.CanvasGroup.interactable = true;
        RebuildNoosphereDiveMeanings();
        ApplyNoosphereDiveMeaningViews();
        ApplyNoosphereDiveTransition(0f);
        PlayUiSound(uiPanelOpenClip, 0.58f);
        LogUiInput("Noosphere: entered 3D dive");
    }

    private void BeginNoosphereDiveExit()
    {
        if (noosphereDiveMode == NoosphereDiveMode.Closed || noosphereDiveMode == NoosphereDiveMode.Leaving)
        {
            return;
        }

        noosphereDiveMode = NoosphereDiveMode.Leaving;
        noosphereDiveTransitionTimer = 0f;
        PlayUiSound(uiPanelCloseClip, 0.48f);
        LogUiInput("Noosphere: leaving 3D dive");
    }

    private void CloseNoosphereDiveImmediate()
    {
        noosphereDiveMode = NoosphereDiveMode.Closed;
        noosphereDiveTransitionTimer = 0f;
        if (noosphereDiveUi?.CanvasRoot != null)
        {
            noosphereDiveUi.CanvasRoot.SetActive(false);
            noosphereDiveUi.CanvasGroup.alpha = 0f;
            noosphereDiveUi.CanvasGroup.blocksRaycasts = false;
            noosphereDiveUi.CanvasGroup.interactable = false;
        }

        if (noosphereDiveUi?.SceneRoot != null)
        {
            noosphereDiveUi.SceneRoot.gameObject.SetActive(false);
        }

        SetNoosphereScreenDiveFade(0f);
    }

    private void UpdateNoosphereDiveRuntime()
    {
        if (noosphereDiveMode == NoosphereDiveMode.Closed)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame ||
            Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            BeginNoosphereDiveExit();
        }

        float dt = Time.unscaledDeltaTime;
        if (noosphereDiveMode == NoosphereDiveMode.Entering)
        {
            noosphereDiveTransitionTimer += dt;
            float progress = Mathf.Clamp01(noosphereDiveTransitionTimer / NoosphereDiveEnterSeconds);
            ApplyNoosphereDiveTransition(SmoothNoosphereDiveProgress(progress));
            if (progress >= 1f)
            {
                noosphereDiveMode = NoosphereDiveMode.Open;
                noosphereDiveTransitionTimer = 0f;
            }
        }
        else if (noosphereDiveMode == NoosphereDiveMode.Leaving)
        {
            noosphereDiveTransitionTimer += dt;
            float progress = Mathf.Clamp01(noosphereDiveTransitionTimer / NoosphereDiveLeaveSeconds);
            ApplyNoosphereDiveTransition(1f - SmoothNoosphereDiveProgress(progress));
            if (progress >= 1f)
            {
                CloseNoosphereDiveImmediate();
                return;
            }
        }
        else
        {
            ApplyNoosphereDiveTransition(1f);
        }

        noosphereDiveRefreshTimer -= dt;
        if (noosphereDiveDirty || noosphereDiveRefreshTimer <= 0f)
        {
            noosphereDiveRefreshTimer = NoosphereDiveRefreshSeconds;
            RebuildNoosphereDiveMeanings();
            ApplyNoosphereDiveMeaningViews();
            noosphereDiveDirty = false;
        }

        AnimateNoosphereDive(dt);
    }

    private void SetupNoosphereDiveUi()
    {
        if (noosphereDiveUi != null)
        {
            return;
        }

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        noosphereDiveTexture = CreateNoosphereDiveTexture();
        noosphereDiveLineMaterial = CreateNoosphereDiveMaterial("NoosphereDiveLineMaterial", new Color(0.44f, 0.82f, 1f, 0.52f));
        noosphereDiveCoreMaterial = CreateNoosphereDiveMaterial("NoosphereDiveCoreMaterial", new Color(0.45f, 0.82f, 1f, 1f));
        noosphereDiveShellMaterial = CreateNoosphereDiveMaterial("NoosphereDiveShellMaterial", new Color(0.14f, 0.22f, 0.36f, 0.34f));

        GameObject canvasObject = new("NoosphereDiveCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 48;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        RectTransform root = CreateUiObject("NoosphereDiveRoot", canvasObject.transform).GetComponent<RectTransform>();
        StretchRect(root, 0f, 0f, 0f, 0f);
        Image blocker = root.gameObject.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 1f);
        blocker.raycastTarget = true;

        RectTransform viewRoot = CreateUiObject("NoosphereDiveView", root).GetComponent<RectTransform>();
        StretchRect(viewRoot, 0f, 0f, 0f, 0f);
        RawImage viewImage = viewRoot.gameObject.AddComponent<RawImage>();
        viewImage.texture = noosphereDiveTexture;
        viewImage.color = Color.white;
        viewImage.raycastTarget = true;

        RectTransform topBar = CreateUiObject("NoosphereDiveTopBar", root).GetComponent<RectTransform>();
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.anchoredPosition = new Vector2(0f, -24f);
        topBar.sizeDelta = new Vector2(0f, 58f);

        Text title = CreateHeaderText("NoosphereDiveTitle", topBar, font, string.Empty, 24, TextAnchor.MiddleLeft, Color.white);
        title.rectTransform.anchorMin = new Vector2(0f, 0f);
        title.rectTransform.anchorMax = new Vector2(0.45f, 1f);
        title.rectTransform.offsetMin = new Vector2(36f, 0f);
        title.rectTransform.offsetMax = Vector2.zero;
        title.raycastTarget = false;

        Text stats = CreateBodyText("NoosphereDiveStats", topBar, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);
        stats.rectTransform.anchorMin = new Vector2(0.45f, 0f);
        stats.rectTransform.anchorMax = new Vector2(1f, 1f);
        stats.rectTransform.offsetMin = Vector2.zero;
        stats.rectTransform.offsetMax = new Vector2(-88f, 0f);
        stats.raycastTarget = false;

        Button closeButton = CreateNoosphereDiveCloseButton(root, font);
        SetupNoosphereDiveScene(font);

        noosphereDiveUi = new NoosphereDiveUiRefs
        {
            CanvasRoot = canvasObject,
            CanvasGroup = canvasGroup,
            ViewImage = viewImage,
            TitleText = title,
            StatsText = stats,
            CloseButton = closeButton,
            Camera = noosphereDiveUi?.Camera,
            SceneRoot = noosphereDiveUi?.SceneRoot,
            CoreRoot = noosphereDiveUi?.CoreRoot,
            CoreRenderer = noosphereDiveUi?.CoreRenderer,
            ShellRenderer = noosphereDiveUi?.ShellRenderer
        };

        noosphereDiveUi.CanvasRoot.SetActive(false);
    }

    private Button CreateNoosphereDiveCloseButton(RectTransform parent, Font font)
    {
        RectTransform buttonRoot = CreateStyledPanel("NoosphereDiveCloseButton", parent, new Color(0.18f, 0.12f, 0.12f, 0.76f));
        buttonRoot.anchorMin = new Vector2(1f, 1f);
        buttonRoot.anchorMax = new Vector2(1f, 1f);
        buttonRoot.pivot = new Vector2(1f, 1f);
        buttonRoot.anchoredPosition = new Vector2(-24f, -24f);
        buttonRoot.sizeDelta = new Vector2(42f, 42f);

        Button button = buttonRoot.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonRoot.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.12f, 0.12f, 0.76f);
        colors.highlightedColor = new Color(0.48f, 0.16f, 0.18f, 0.96f);
        colors.pressedColor = new Color(0.78f, 0.20f, 0.18f, 1f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
        button.onClick.AddListener(BeginNoosphereDiveExit);

        Text text = CreateBodyText("X", buttonRoot, font, "X", 18, TextAnchor.MiddleCenter, Color.white);
        StretchRect(text.rectTransform, 0f, 0f, 0f, 0f);
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;
        return button;
    }

    private void SetupNoosphereDiveScene(Font font)
    {
        Transform sceneRoot = new GameObject("NoosphereDiveSceneRoot").transform;
        sceneRoot.position = NoosphereDiveSceneOrigin;

        GameObject cameraObject = new("NoosphereDiveCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.004f, 0.008f, 0.018f, 1f);
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 90f;
        camera.fieldOfView = 58f;
        camera.targetTexture = noosphereDiveTexture;
        camera.transform.position = NoosphereDiveSceneOrigin + new Vector3(0f, 0.15f, -10.5f);
        camera.transform.LookAt(NoosphereDiveSceneOrigin);

        Transform coreRoot = new GameObject("NoosphereDiveCoreRoot").transform;
        coreRoot.SetParent(sceneRoot, false);

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "NoosphereDiveCore";
        core.transform.SetParent(coreRoot, false);
        core.transform.localScale = Vector3.one * 0.92f;
        DestroyNoosphereDiveCollider(core);
        Renderer coreRenderer = core.GetComponent<Renderer>();
        coreRenderer.sharedMaterial = noosphereDiveCoreMaterial;

        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "NoosphereDiveShell";
        shell.transform.SetParent(coreRoot, false);
        shell.transform.localScale = Vector3.one * 1.42f;
        DestroyNoosphereDiveCollider(shell);
        Renderer shellRenderer = shell.GetComponent<Renderer>();
        shellRenderer.sharedMaterial = noosphereDiveShellMaterial;

        noosphereDiveUi = new NoosphereDiveUiRefs
        {
            Camera = camera,
            SceneRoot = sceneRoot,
            CoreRoot = coreRoot,
            CoreRenderer = coreRenderer,
            ShellRenderer = shellRenderer
        };

        noosphereDiveRings.Add(CreateNoosphereDiveRing("NoosphereDiveRingA", sceneRoot, 2.15f, 0.018f, new Color(0.40f, 0.80f, 1f, 0.42f), Quaternion.Euler(64f, 0f, 0f)));
        noosphereDiveRings.Add(CreateNoosphereDiveRing("NoosphereDiveRingB", sceneRoot, 3.45f, 0.014f, new Color(0.88f, 0.64f, 1f, 0.28f), Quaternion.Euler(18f, 58f, 0f)));
        noosphereDiveRings.Add(CreateNoosphereDiveRing("NoosphereDiveRingC", sceneRoot, 5.55f, 0.010f, new Color(0.52f, 1f, 0.68f, 0.18f), Quaternion.Euler(82f, 0f, 34f)));

        for (int i = 0; i < NoosphereDiveMaxMeanings; i++)
        {
            noosphereDiveMeaningViews.Add(CreateNoosphereDiveMeaningView(sceneRoot, font, i));
        }

        sceneRoot.gameObject.SetActive(false);
    }

    private RenderTexture CreateNoosphereDiveTexture()
    {
        int width = Mathf.Clamp(Screen.width > 0 ? Screen.width : 1600, 960, 1920);
        int height = Mathf.Clamp(Screen.height > 0 ? Screen.height : 900, 540, 1080);
        RenderTexture texture = new(width, height, 24, RenderTextureFormat.ARGB32)
        {
            name = "NoosphereDiveRenderTexture",
            antiAliasing = 2
        };
        texture.Create();
        return texture;
    }

    private static Material CreateNoosphereDiveMaterial(string name, Color color)
    {
        Shader shader =
            Shader.Find("Sprites/Default") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Standard");
        Material material = new(shader)
        {
            name = name,
            color = color
        };
        return material;
    }

    private LineRenderer CreateNoosphereDiveRing(string name, Transform parent, float radius, float width, Color color, Quaternion rotation)
    {
        GameObject ringObject = new(name);
        ringObject.transform.SetParent(parent, false);
        ringObject.transform.localRotation = rotation;
        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.sharedMaterial = noosphereDiveLineMaterial;
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = NoosphereDiveRingSegments;
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 2;
        for (int i = 0; i < NoosphereDiveRingSegments; i++)
        {
            float angle = i / (float)NoosphereDiveRingSegments * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        return line;
    }

    private NoosphereDiveMeaningView CreateNoosphereDiveMeaningView(Transform parent, Font font, int index)
    {
        GameObject root = new($"NoosphereDiveMeaning{index + 1}");
        root.transform.SetParent(parent, false);

        TextMesh text = root.AddComponent<TextMesh>();
        text.font = font;
        text.text = string.Empty;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 44;
        text.characterSize = 0.072f;
        text.richText = false;
        text.color = Color.white;
        MeshRenderer renderer = root.GetComponent<MeshRenderer>();
        if (renderer != null && font != null)
        {
            renderer.sharedMaterial = font.material;
        }

        GameObject spokeObject = new("Spoke");
        spokeObject.transform.SetParent(root.transform, false);
        LineRenderer spoke = spokeObject.AddComponent<LineRenderer>();
        spoke.sharedMaterial = noosphereDiveLineMaterial;
        spoke.useWorldSpace = true;
        spoke.positionCount = 2;
        spoke.startWidth = 0.010f;
        spoke.endWidth = 0.003f;
        spoke.numCapVertices = 2;

        root.SetActive(false);
        return new NoosphereDiveMeaningView
        {
            Root = root.transform,
            Text = text,
            Spoke = spoke
        };
    }

    private static void DestroyNoosphereDiveCollider(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private void AnimateNoosphereDive(float dt)
    {
        if (noosphereDiveUi?.SceneRoot == null || noosphereDiveUi.Camera == null)
        {
            return;
        }

        float time = Time.unscaledTime;
        float openProgress = noosphereDiveUi.CanvasGroup != null ? noosphereDiveUi.CanvasGroup.alpha : 1f;
        Vector3 cameraFrom = NoosphereDiveSceneOrigin + new Vector3(0f, 0.20f, -15.2f);
        Vector3 cameraTo = NoosphereDiveSceneOrigin + new Vector3(0f, 0.55f, -8.4f);
        noosphereDiveUi.Camera.transform.position = Vector3.Lerp(cameraFrom, cameraTo, openProgress);
        noosphereDiveUi.Camera.fieldOfView = Mathf.Lerp(78f, 49f, openProgress);
        noosphereDiveUi.Camera.transform.LookAt(NoosphereDiveSceneOrigin + new Vector3(0f, 0.12f, 0f));

        Color coreColor = GetNoosphereCityExperienceCoreColor();
        if (noosphereDiveUi.CoreRenderer != null)
        {
            noosphereDiveUi.CoreRenderer.sharedMaterial.color = Color.Lerp(new Color(0.34f, 0.74f, 1f, 1f), coreColor, 0.72f);
        }

        if (noosphereDiveUi.ShellRenderer != null)
        {
            noosphereDiveUi.ShellRenderer.sharedMaterial.color = new Color(coreColor.r, coreColor.g, coreColor.b, 0.24f + Mathf.Sin(time * 1.4f) * 0.05f);
        }

        noosphereDiveUi.CoreRoot.localScale = Vector3.one * (Mathf.Lerp(0.35f, 1f, openProgress) * (1f + Mathf.Sin(time * 2.2f) * 0.045f));
        noosphereDiveUi.CoreRoot.localRotation = Quaternion.Euler(time * 8f, time * 15f, time * 5f);

        for (int i = 0; i < noosphereDiveRings.Count; i++)
        {
            Transform ring = noosphereDiveRings[i].transform;
            ring.Rotate(new Vector3(0.08f + i * 0.04f, 0.12f, 0.05f) * (dt * 46f), Space.Self);
        }

        for (int i = 0; i < noosphereDiveMeaningViews.Count; i++)
        {
            NoosphereDiveMeaningView view = noosphereDiveMeaningViews[i];
            NoosphereDiveMeaningModel model = view.Model;
            if (model == null || !view.Root.gameObject.activeSelf)
            {
                continue;
            }

            float angle = model.Phase + time * model.Speed;
            float radius = model.Radius * Mathf.Lerp(0.42f, 1f, openProgress);
            Vector3 localPosition = new(
                Mathf.Cos(angle) * radius,
                model.Height + Mathf.Sin(time * 0.72f + model.Phase) * model.Wobble,
                Mathf.Sin(angle) * radius * 0.72f);
            localPosition += new Vector3(
                Mathf.Sin(time * 0.39f + model.Phase * 0.7f) * 0.18f,
                0f,
                Mathf.Cos(time * 0.33f + model.Phase) * 0.15f);

            view.Root.localPosition = localPosition;
            view.Root.localScale = Vector3.one * (model.Size * Mathf.Lerp(0.45f, 1f, openProgress));
            view.Root.rotation = Quaternion.LookRotation(noosphereDiveUi.Camera.transform.forward, noosphereDiveUi.Camera.transform.up);

            Color color = model.Color;
            color.a *= openProgress * (0.82f + Mathf.Sin(time * 1.2f + model.Phase) * 0.12f);
            view.Text.color = color;

            Vector3 worldPosition = view.Root.position;
            view.Spoke.SetPosition(0, NoosphereDiveSceneOrigin);
            view.Spoke.SetPosition(1, worldPosition);
            Color spokeStart = new(color.r, color.g, color.b, Mathf.Clamp01(color.a * 0.28f));
            Color spokeEnd = new(color.r, color.g, color.b, Mathf.Clamp01(color.a * 0.03f));
            view.Spoke.startColor = spokeStart;
            view.Spoke.endColor = spokeEnd;
        }
    }

    private void ApplyNoosphereDiveTransition(float progress)
    {
        if (noosphereDiveUi?.CanvasGroup == null)
        {
            return;
        }

        noosphereDiveUi.CanvasGroup.alpha = progress;
        noosphereDiveUi.CanvasGroup.blocksRaycasts = progress > 0.05f;
        noosphereDiveUi.CanvasGroup.interactable = progress > 0.80f;
        SetNoosphereScreenDiveFade(progress);
    }

    private static float SmoothNoosphereDiveProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        return progress * progress * (3f - 2f * progress);
    }

    private void EnsureNoosphereScreenFadeGroup()
    {
        if (noosphereScreenFadeGroup != null || noosphereScreenUi?.WindowRoot == null)
        {
            return;
        }

        noosphereScreenFadeGroup = noosphereScreenUi.WindowRoot.GetComponent<CanvasGroup>();
        if (noosphereScreenFadeGroup == null)
        {
            noosphereScreenFadeGroup = noosphereScreenUi.WindowRoot.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void SetNoosphereScreenDiveFade(float progress)
    {
        EnsureNoosphereScreenFadeGroup();
        if (noosphereScreenFadeGroup == null)
        {
            return;
        }

        float screenAlpha = 1f - Mathf.Clamp01(progress);
        noosphereScreenFadeGroup.alpha = screenAlpha;
        noosphereScreenFadeGroup.blocksRaycasts = screenAlpha > 0.20f;
        noosphereScreenFadeGroup.interactable = screenAlpha > 0.20f;
    }
}
