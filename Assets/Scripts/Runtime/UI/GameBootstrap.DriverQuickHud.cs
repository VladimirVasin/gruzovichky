using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class DriverQuickHudRefs
    {
        public GameObject CanvasRoot;
        public RectTransform Root;
        public Text HeaderText;
        public Text OccupationText;
        public Text ActivityText;
        public Text ConditionText;
        public RectTransform ConditionBarFill;
        public Image ConditionBarFillImage;
        public Text CriticalEmptyText;
        public DriverQuickHudNeedRow[] NeedRows;
        public Text BalanceText;
        public Button OpenDriversButton;
        public Text OpenDriversButtonText;
        public Button CloseButton;
        public Text CloseButtonText;
        public RectTransform LinkLine;
        public Image LinkLineImage;
    }

    private sealed class DriverQuickHudNeedRow
    {
        public GameObject Root;
        public Image IconImage;
        public Text LabelText;
        public RectTransform BarFill;
        public Image BarFillImage;
        public Text PercentText;
    }

    private struct DriverQuickHudNeedSnapshot
    {
        public WorkerNeedKind Kind;
        public string Label;
        public Sprite Icon;
        public float Percent;
        public WorkerNeedStatus Status;
    }

    private static Sprite s_driverQuickHudShieldIcon;
    private static readonly Color DriverQuickHudLinkLineColor = new(1f, 0.74f, 0.25f, 0.76f);
    private const float DriverQuickHudLinkLineThickness = 3f;
    private readonly Vector3[] driverQuickHudLinkCorners = new Vector3[4];
    private DriverQuickHudRefs driverQuickHud;
    private int selectedDriverId;
    private bool isDriverDetailsOpen;

    private void SetupDriverQuickHud()
    {
        if (driverQuickHud != null) return;

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        driverQuickHud = new DriverQuickHudRefs();

        GameObject canvasObject = new("DriverQuickHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        driverQuickHud.CanvasRoot = canvasObject;
        CreateDriverQuickHudLinkLine(canvasObject.transform);

        RectTransform root = CreateStyledPanel("DriverQuickHudRoot", canvasObject.transform, new Color(0.035f, 0.055f, 0.080f, 0.96f));
        root.anchorMin = new Vector2(1f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-14f, 92f);
        root.sizeDelta = new Vector2(330f, 0f);
        root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(12, 12, 12, 12);
        rootLayout.spacing = 7;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        driverQuickHud.Root = root;

        RectTransform headerRow = CreateLayoutRow("DriverQuickHudHeader", root, 42f, 8f);
        HorizontalLayoutGroup headerLayout = headerRow.GetComponent<HorizontalLayoutGroup>();
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        driverQuickHud.HeaderText = CreateHeaderText("DriverName", headerRow, uiFont, "Житель", 22, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.HeaderText.resizeTextForBestFit = true;
        driverQuickHud.HeaderText.resizeTextMinSize = 15;
        driverQuickHud.HeaderText.resizeTextMaxSize = 22;
        driverQuickHud.HeaderText.verticalOverflow = VerticalWrapMode.Truncate;
        driverQuickHud.HeaderText.lineSpacing = 0.82f;
        LayoutElement headerTextLayout = driverQuickHud.HeaderText.gameObject.AddComponent<LayoutElement>();
        headerTextLayout.flexibleWidth = 1f;
        headerTextLayout.preferredHeight = 42f;

        driverQuickHud.CloseButton = CreateButton("CloseBtn", headerRow, uiFont, out driverQuickHud.CloseButtonText, "X", 16, new Color(0.08f, 0.12f, 0.17f, 1f), Color.white);
        driverQuickHud.CloseButtonText.fontStyle = FontStyle.Bold;
        LayoutElement closeLayout = driverQuickHud.CloseButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 30f;
        closeLayout.preferredHeight = 30f;
        driverQuickHud.CloseButton.onClick.AddListener(ClearDriverFocus);

        driverQuickHud.OccupationText = CreateBodyText("OccupationText", root, uiFont, string.Empty, 15, TextAnchor.MiddleLeft, new Color(0.64f, 0.74f, 0.90f, 1f));
        driverQuickHud.OccupationText.fontStyle = FontStyle.Bold;
        driverQuickHud.OccupationText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        driverQuickHud.ActivityText = CreateBodyText("ActivityText", root, uiFont, string.Empty, 14, TextAnchor.MiddleLeft, new Color(0.78f, 0.84f, 0.92f, 1f));
        driverQuickHud.ActivityText.fontStyle = FontStyle.Bold;
        driverQuickHud.ActivityText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        RectTransform conditionCard = CreateStyledPanel("DriverQuickHudConditionCard", root, new Color(0.06f, 0.09f, 0.13f, 0.94f));
        conditionCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 82f;
        VerticalLayoutGroup conditionLayout = conditionCard.gameObject.AddComponent<VerticalLayoutGroup>();
        conditionLayout.padding = new RectOffset(10, 10, 9, 10);
        conditionLayout.spacing = 8f;
        conditionLayout.childControlWidth = true;
        conditionLayout.childControlHeight = true;
        conditionLayout.childForceExpandWidth = true;
        conditionLayout.childForceExpandHeight = false;

        RectTransform conditionRow = CreateLayoutRow("DriverQuickHudConditionRow", conditionCard, 32f, 9f);
        Image shieldIcon = CreateDriverQuickHudIconImage("ConditionShield", conditionRow, GetDriverQuickHudShieldIcon(), new Vector2(30f, 30f));
        shieldIcon.color = Color.white;
        driverQuickHud.ConditionText = CreateHeaderText("ConditionText", conditionRow, uiFont, string.Empty, 22, TextAnchor.MiddleLeft, Color.white);
        driverQuickHud.ConditionText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        driverQuickHud.ConditionBarFill = CreateDriverQuickHudProgressBar(
            conditionCard,
            "ConditionBar",
            10f,
            out driverQuickHud.ConditionBarFillImage);

        Text criticalHeader = CreateHeaderText("CriticalHeader", root, uiFont, "Нужды", 17, TextAnchor.MiddleLeft, new Color(0.78f, 0.84f, 0.92f, 1f));
        criticalHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        RectTransform needsCard = CreateStyledPanel("DriverQuickHudNeedsCard", root, new Color(0.06f, 0.09f, 0.13f, 0.94f));
        needsCard.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup needsLayout = needsCard.gameObject.AddComponent<VerticalLayoutGroup>();
        needsLayout.padding = new RectOffset(8, 8, 8, 8);
        needsLayout.spacing = 5f;
        needsLayout.childControlWidth = true;
        needsLayout.childControlHeight = true;
        needsLayout.childForceExpandWidth = true;
        needsLayout.childForceExpandHeight = false;

        driverQuickHud.CriticalEmptyText = CreateBodyText("CriticalEmptyText", needsCard, uiFont, "Все нужды в норме", 14, TextAnchor.MiddleCenter, new Color(0.72f, 0.80f, 0.88f, 1f));
        driverQuickHud.CriticalEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        driverQuickHud.NeedRows = new[]
        {
            CreateDriverQuickHudNeedRow("Meal", needsCard, uiFont),
            CreateDriverQuickHudNeedRow("Sleep", needsCard, uiFont),
            CreateDriverQuickHudNeedRow("Leisure", needsCard, uiFont)
        };

        driverQuickHud.BalanceText = CreateBodyText("BalanceText", root, uiFont, string.Empty, 16, TextAnchor.MiddleLeft, new Color(0.62f, 0.90f, 0.54f, 1f));
        driverQuickHud.BalanceText.fontStyle = FontStyle.Bold;
        driverQuickHud.BalanceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        RectTransform actionRow = CreateLayoutRow("DriverQuickHudActions", root, 34f, 0f);
        driverQuickHud.OpenDriversButton = CreateButton("OpenDriversBtn", actionRow, uiFont, out driverQuickHud.OpenDriversButtonText, "Открыть профиль", 15, new Color(0.80f, 0.36f, 0.03f, 1f), Color.white);
        driverQuickHud.OpenDriversButtonText.fontStyle = FontStyle.Bold;
        LayoutElement openDriversLayout = driverQuickHud.OpenDriversButton.gameObject.AddComponent<LayoutElement>();
        openDriversLayout.preferredHeight = 34f;
        openDriversLayout.flexibleWidth = 1f;
        driverQuickHud.OpenDriversButton.onClick.AddListener(() =>
        {
            if (selectedDriverId <= 0) return;
            LogUiInput($"Driver Quick HUD: opened profile for Driver #{selectedDriverId}");
            OpenDriversPanelForDriver(selectedDriverId);
            isDriversScreenDirty = true;
        });

        driverQuickHud.CanvasRoot.SetActive(false);
        UpdateDriverQuickHud();
    }

    private void UpdateDriverQuickHud()
    {
        if (driverQuickHud == null) return;

        bool shouldShow =
            isDriverDetailsOpen &&
            !HasBlockingHudOpenForQuickHuds();

        if (driverQuickHud.CanvasRoot.activeSelf != shouldShow)
            driverQuickHud.CanvasRoot.SetActive(shouldShow);

        if (!shouldShow)
        {
            SetDriverQuickHudLinkLineVisible(false);
            return;
        }

        DriverAgent driver = driverAgents.Find(d => d.DriverId == selectedDriverId);
        if (driver == null)
        {
            SetDriverQuickHudLinkLineVisible(false);
            driverQuickHud.CanvasRoot.SetActive(false);
            return;
        }

        driverQuickHud.HeaderText.text = driver.DriverName;
        driverQuickHud.OccupationText.text = GetWorkerQuickHudOccupationLabelRu(driver);
        driverQuickHud.ActivityText.text = $"Сейчас: {GetWorkerQuickHudActivityLabelRu(driver)}";

        int score = GetDriverQuickHudConditionScore(driver);
        Color conditionColor = GetDriverQuickHudConditionColor(score);
        driverQuickHud.ConditionText.text = $"{GetDriverQuickHudConditionLabel(score)} · {score}";
        SetDriverQuickHudBar(driverQuickHud.ConditionBarFill, driverQuickHud.ConditionBarFillImage, score / 100f, conditionColor);

        ApplyDriverQuickHudNeeds(driver);
        driverQuickHud.BalanceText.text = $"Баланс: ${driver.Money}";
        if (driverQuickHud.OpenDriversButtonText != null)
        {
            driverQuickHud.OpenDriversButtonText.text = "Открыть профиль";
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(driverQuickHud.Root);
        UpdateDriverQuickHudLinkLine();
    }

    private void CreateDriverQuickHudLinkLine(Transform canvasTransform)
    {
        GameObject lineObject = CreateUiObject("DriverQuickHudLinkLine", canvasTransform);
        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.sizeDelta = new Vector2(0f, DriverQuickHudLinkLineThickness);

        Image lineImage = lineObject.AddComponent<Image>();
        lineImage.color = DriverQuickHudLinkLineColor;
        lineImage.raycastTarget = false;

        lineObject.SetActive(false);
        driverQuickHud.LinkLine = lineRect;
        driverQuickHud.LinkLineImage = lineImage;
    }

    private void UpdateDriverQuickHudLinkLine()
    {
        if (driverQuickHud?.LinkLine == null ||
            driverQuickHud.Root == null ||
            driverQuickHud.CanvasRoot == null ||
            mainCamera == null ||
            !driverQuickHud.CanvasRoot.activeInHierarchy ||
            !isDriverDetailsOpen ||
            selectedDriverId <= 0)
        {
            SetDriverQuickHudLinkLineVisible(false);
            return;
        }

        if (!TryGetSelectedEntityHighlightTarget(out Vector3 targetPosition, out _))
        {
            SetDriverQuickHudLinkLineVisible(false);
            return;
        }

        Vector3 markerPosition = new(
            targetPosition.x,
            SampleTerrainHeight(targetPosition.x, targetPosition.z) + 0.08f,
            targetPosition.z);
        Vector3 targetScreen3 = mainCamera.WorldToScreenPoint(markerPosition);
        if (targetScreen3.z <= 0f ||
            targetScreen3.x < -32f ||
            targetScreen3.x > Screen.width + 32f ||
            targetScreen3.y < -32f ||
            targetScreen3.y > Screen.height + 32f)
        {
            SetDriverQuickHudLinkLineVisible(false);
            return;
        }

        Vector2 targetScreen = new(targetScreen3.x, targetScreen3.y);
        Vector2 hudScreenPoint = GetClosestDriverQuickHudEdgePoint(targetScreen);
        RectTransform canvasRect = driverQuickHud.CanvasRoot.GetComponent<RectTransform>();
        if (canvasRect == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, hudScreenPoint, null, out Vector2 hudLocalPoint) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreen, null, out Vector2 targetLocalPoint))
        {
            SetDriverQuickHudLinkLineVisible(false);
            return;
        }

        Vector2 delta = targetLocalPoint - hudLocalPoint;
        float length = delta.magnitude;
        if (length < 8f)
        {
            SetDriverQuickHudLinkLineVisible(false);
            return;
        }

        RectTransform line = driverQuickHud.LinkLine;
        line.anchoredPosition = hudLocalPoint;
        line.sizeDelta = new Vector2(length, DriverQuickHudLinkLineThickness);
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        if (driverQuickHud.LinkLineImage != null)
        {
            driverQuickHud.LinkLineImage.color = DriverQuickHudLinkLineColor;
        }

        SetDriverQuickHudLinkLineVisible(true);
        line.SetAsFirstSibling();
    }

    private Vector2 GetClosestDriverQuickHudEdgePoint(Vector2 targetScreen)
    {
        driverQuickHud.Root.GetWorldCorners(driverQuickHudLinkCorners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, driverQuickHudLinkCorners[0]);
        Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(null, driverQuickHudLinkCorners[1]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, driverQuickHudLinkCorners[2]);
        Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(null, driverQuickHudLinkCorners[3]);

        Vector2 bestPoint = GetClosestPointOnDriverQuickHudSegment(targetScreen, bottomLeft, topLeft);
        float bestDistance = (targetScreen - bestPoint).sqrMagnitude;
        TryUseCloserDriverQuickHudSegment(targetScreen, topLeft, topRight, ref bestPoint, ref bestDistance);
        TryUseCloserDriverQuickHudSegment(targetScreen, topRight, bottomRight, ref bestPoint, ref bestDistance);
        TryUseCloserDriverQuickHudSegment(targetScreen, bottomRight, bottomLeft, ref bestPoint, ref bestDistance);
        return bestPoint;
    }

    private static void TryUseCloserDriverQuickHudSegment(
        Vector2 target,
        Vector2 segmentStart,
        Vector2 segmentEnd,
        ref Vector2 bestPoint,
        ref float bestDistance)
    {
        Vector2 candidate = GetClosestPointOnDriverQuickHudSegment(target, segmentStart, segmentEnd);
        float distance = (target - candidate).sqrMagnitude;
        if (distance < bestDistance)
        {
            bestDistance = distance;
            bestPoint = candidate;
        }
    }

    private static Vector2 GetClosestPointOnDriverQuickHudSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        Vector2 segment = segmentEnd - segmentStart;
        float lengthSq = segment.sqrMagnitude;
        if (lengthSq <= 0.0001f)
        {
            return segmentStart;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / lengthSq);
        return segmentStart + segment * t;
    }

    private void SetDriverQuickHudLinkLineVisible(bool visible)
    {
        if (driverQuickHud?.LinkLine != null &&
            driverQuickHud.LinkLine.gameObject.activeSelf != visible)
        {
            driverQuickHud.LinkLine.gameObject.SetActive(visible);
        }
    }

    private DriverQuickHudNeedRow CreateDriverQuickHudNeedRow(string name, Transform parent, Font uiFont)
    {
        RectTransform rowRoot = CreateStyledPanel($"DriverQuickHudNeed{name}", parent, new Color(0.075f, 0.105f, 0.150f, 0.96f));
        rowRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
        HorizontalLayoutGroup rowLayout = rowRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(8, 8, 5, 5);
        rowLayout.spacing = 6f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        Image icon = CreateDriverQuickHudIconImage($"Need{name}Icon", rowRoot, null, new Vector2(20f, 20f));

        Text label = CreateBodyText($"Need{name}Label", rowRoot, uiFont, string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        label.fontStyle = FontStyle.Bold;
        LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 58f;
        labelLayout.preferredHeight = 24f;

        RectTransform barFill = CreateDriverQuickHudProgressBar(rowRoot, $"Need{name}Bar", 10f, out Image fillImage);

        Text percent = CreateBodyText($"Need{name}Percent", rowRoot, uiFont, string.Empty, 14, TextAnchor.MiddleRight, new Color(0.88f, 0.92f, 0.98f, 1f));
        percent.fontStyle = FontStyle.Bold;
        LayoutElement percentLayout = percent.gameObject.AddComponent<LayoutElement>();
        percentLayout.preferredWidth = 42f;
        percentLayout.preferredHeight = 24f;

        rowRoot.gameObject.SetActive(false);
        return new DriverQuickHudNeedRow
        {
            Root = rowRoot.gameObject,
            IconImage = icon,
            LabelText = label,
            BarFill = barFill,
            BarFillImage = fillImage,
            PercentText = percent
        };
    }

    private static Image CreateDriverQuickHudIconImage(string name, Transform parent, Sprite sprite, Vector2 size)
    {
        GameObject iconObject = CreateUiObject(name, parent);
        Image image = iconObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = false;
        LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = size.x;
        iconLayout.preferredHeight = size.y;
        iconLayout.minWidth = size.x;
        iconLayout.minHeight = size.y;
        return image;
    }

    private static RectTransform CreateDriverQuickHudProgressBar(Transform parent, string name, float height, out Image fillImage)
    {
        GameObject bgObject = CreateUiObject($"{name}Bg", parent);
        RectTransform bgRect = bgObject.GetComponent<RectTransform>();
        Image bgImage = bgObject.AddComponent<Image>();
        bgImage.color = new Color(0.018f, 0.030f, 0.045f, 1f);
        bgImage.raycastTarget = false;
        LayoutElement bgLayout = bgObject.AddComponent<LayoutElement>();
        bgLayout.preferredHeight = height;
        bgLayout.minHeight = height;
        bgLayout.flexibleWidth = 1f;

        GameObject fillObject = CreateUiObject($"{name}Fill", bgRect);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillImage = fillObject.AddComponent<Image>();
        fillImage.color = new Color(0.58f, 0.88f, 0.54f, 1f);
        fillImage.raycastTarget = false;
        return fillRect;
    }

    private void ApplyDriverQuickHudNeeds(DriverAgent driver)
    {
        DriverQuickHudNeedSnapshot[] needs =
        {
            CreateDriverQuickHudNeedSnapshot(driver, WorkerNeedKind.Meal),
            CreateDriverQuickHudNeedSnapshot(driver, WorkerNeedKind.Sleep),
            CreateDriverQuickHudNeedSnapshot(driver, WorkerNeedKind.Leisure)
        };

        for (int i = 0; i < needs.Length - 1; i++)
        {
            for (int j = i + 1; j < needs.Length; j++)
            {
                if (needs[j].Percent < needs[i].Percent)
                {
                    DriverQuickHudNeedSnapshot tmp = needs[i];
                    needs[i] = needs[j];
                    needs[j] = tmp;
                }
            }
        }

        int visibleCount = 0;
        for (int i = 0; i < needs.Length && visibleCount < driverQuickHud.NeedRows.Length; i++)
        {
            if (needs[i].Status == WorkerNeedStatus.Ok)
            {
                continue;
            }

            ApplyDriverQuickHudNeedRow(driverQuickHud.NeedRows[visibleCount], needs[i]);
            visibleCount++;
        }

        for (int i = visibleCount; i < driverQuickHud.NeedRows.Length; i++)
        {
            driverQuickHud.NeedRows[i].Root.SetActive(false);
        }

        driverQuickHud.CriticalEmptyText.gameObject.SetActive(visibleCount == 0);
    }

    private DriverQuickHudNeedSnapshot CreateDriverQuickHudNeedSnapshot(DriverAgent driver, WorkerNeedKind need)
    {
        return new DriverQuickHudNeedSnapshot
        {
            Kind = need,
            Label = GetDriverQuickHudNeedLabel(need),
            Icon = GetDriverQuickHudNeedIcon(need),
            Percent = GetDriverQuickHudNeedPercent(driver, need),
            Status = GetWorkerNeedLastStatus(driver, need)
        };
    }

    private void ApplyDriverQuickHudNeedRow(DriverQuickHudNeedRow row, DriverQuickHudNeedSnapshot need)
    {
        int percent = Mathf.RoundToInt(need.Percent * 100f);
        row.Root.SetActive(true);
        row.IconImage.sprite = need.Icon;
        row.LabelText.text = need.Label;
        row.PercentText.text = $"{percent}%";
        SetDriverQuickHudBar(row.BarFill, row.BarFillImage, need.Percent, GetDriverQuickHudNeedColor(need.Percent));
    }

    private int GetDriverQuickHudConditionScore(DriverAgent driver)
    {
        if (driver == null)
        {
            return 0;
        }

        float meal = GetDriverQuickHudNeedPercent(driver, WorkerNeedKind.Meal);
        float sleep = GetDriverQuickHudNeedPercent(driver, WorkerNeedKind.Sleep);
        float leisure = GetDriverQuickHudNeedPercent(driver, WorkerNeedKind.Leisure);
        float worstNeed = Mathf.Min(meal, sleep, leisure);
        return Mathf.RoundToInt(Mathf.Clamp01(worstNeed) * 100f);
    }

    private static void SetDriverQuickHudBar(RectTransform fill, Image fillImage, float progress, Color color)
    {
        if (fill == null || fillImage == null)
        {
            return;
        }

        fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fillImage.color = color;
    }

    private static string GetDriverQuickHudConditionLabel(int score)
    {
        if (score >= 70) return "Хорош";
        if (score >= 45) return "Стабилен";
        if (score >= 25) return "Плохо";
        return "Критично";
    }

    private static Color GetDriverQuickHudConditionColor(int score)
    {
        if (score >= 45) return new Color(0.50f, 0.86f, 0.38f, 1f);
        if (score >= 25) return new Color(0.96f, 0.62f, 0.20f, 1f);
        return new Color(0.92f, 0.28f, 0.20f, 1f);
    }

    private static Color GetDriverQuickHudNeedColor(float percent)
    {
        if (percent >= 0.50f) return new Color(0.50f, 0.86f, 0.38f, 1f);
        if (percent >= 0.20f) return new Color(0.96f, 0.72f, 0.30f, 1f);
        if (percent > 0f) return new Color(0.96f, 0.45f, 0.14f, 1f);
        return new Color(0.92f, 0.28f, 0.20f, 1f);
    }

    private float GetDriverQuickHudNeedPercent(DriverAgent driver, WorkerNeedKind need)
    {
        float criticalHours = need switch
        {
            WorkerNeedKind.Meal => WorkerMealCriticalHours,
            WorkerNeedKind.Sleep => WorkerSleepCriticalHours,
            WorkerNeedKind.Leisure => WorkerLeisureCriticalHours,
            _ => 1f
        };

        return Mathf.Clamp01(1f - GetWorkerNeedHours(driver, need) / Mathf.Max(0.01f, criticalHours));
    }

    private static string GetDriverQuickHudNeedLabel(WorkerNeedKind need)
    {
        return need switch
        {
            WorkerNeedKind.Meal => "Еда",
            WorkerNeedKind.Sleep => "Сон",
            WorkerNeedKind.Leisure => "Досуг",
            _ => "Нужда"
        };
    }

    private static Sprite GetDriverQuickHudNeedIcon(WorkerNeedKind need)
    {
        return need switch
        {
            WorkerNeedKind.Meal => GetNeedsMealIcon(),
            WorkerNeedKind.Sleep => GetNeedsSleepIcon(),
            WorkerNeedKind.Leisure => GetNeedsLeisureIcon(),
            _ => null
        };
    }

    private static Sprite GetDriverQuickHudShieldIcon()
    {
        return s_driverQuickHudShieldIcon ??= BuildNeedIcon(PaintDriverQuickHudShieldIcon);
    }

    private static void PaintDriverQuickHudShieldIcon(Color[] px, int sz)
    {
        Color gold = new(1.00f, 0.78f, 0.20f, 1f);
        Color star = new(1.00f, 0.92f, 0.38f, 1f);
        void S(int x, int y, Color c) => NeedIconSet(px, sz, x, y, c);

        for (int y = 2; y <= 12; y++)
        {
            int left = y < 6 ? 4 - y / 3 : 2 + (y - 6) / 3;
            int right = sz - 1 - left;
            S(left, y, gold);
            S(right, y, gold);
        }

        for (int x = 4; x <= 11; x++) S(x, 13, gold);
        for (int x = 5; x <= 10; x++) S(x, 2, gold);
        S(8, 4, star);
        S(7, 6, star); S(8, 6, star); S(9, 6, star);
        S(6, 7, star); S(7, 7, star); S(8, 7, star); S(9, 7, star); S(10, 7, star);
        S(7, 8, star); S(8, 8, star); S(9, 8, star);
        S(7, 9, star); S(9, 9, star);
    }

    private string GetWorkerQuickHudOccupationLabelRu(DriverAgent driver)
    {
        if (driver != null && driver.LifeGoal == WorkerLifeGoal.FindJob)
        {
            return "Ищет работу";
        }

        if (IsDriverBusDriver(driver))
        {
            return "Водитель автобуса";
        }

        if (driver != null && driver.DutyMode == DriverDutyMode.Intercity)
        {
            return "Межгородный водитель";
        }

        if (driver != null && driver.DutyMode == DriverDutyMode.Logistics && driver.AssignedBuildingType.HasValue)
        {
            return GetWorkerQuickHudBuildingRoleLabelRu(driver.AssignedBuildingType.Value);
        }

        if (driver != null && driver.AssignedTruckNumber > 0)
        {
            return "Водитель грузовика";
        }

        return "Безработный";
    }

    private static string GetWorkerQuickHudBuildingRoleLabelRu(LocationType type)
    {
        return type switch
        {
            LocationType.Forest => "Лесоруб",
            LocationType.Sawmill => "Рабочий лесопилки",
            LocationType.FurnitureFactory => "Столяр",
            LocationType.Warehouse => "Грузчик склада",
            LocationType.Docks => "Работник доков",
            LocationType.Kindergarten => "Воспитатель",
            LocationType.PrimarySchool => "Учитель",
            LocationType.SecondarySchool => "Учитель",
            LocationType.LaborExchange => "Сотрудник биржи труда",
            _ when HasServiceWorkerSlot(type) => "Сервисный работник",
            _ => "Работник"
        };
    }

    private string GetWorkerQuickHudActivityLabelRu(DriverAgent driver)
    {
        if (IsDriverOnActiveTradeRun(driver))
            return "В торговом рейсе";
        if (driver.IsLeavingTown || driver.WalkPhase == DriverRescuePhase.ToIntercityStopForDeparture)
            return "Уезжает из города";
        if (driver.IsArrivingByBus)
            return "Приезжает";
        if (driver.RestPhase == DriverRestPhase.SleepingAtHome)
            return "Спит дома";
        if (driver.RestPhase == DriverRestPhase.Sleeping)
            return "Спит в мотеле";
        if (driver.RestPhase != DriverRestPhase.None)
            return "Идёт";
        if (IsBusDriverOnActiveRoute(driver))
            return $"Ведёт автобус · {GetLocalBusPassengerCount(driver)}/{GetLocalBusPassengerCapacity()}";
        if (IsDriverBusDriver(driver) && driver.ShiftStartHour < 0)
            return "Ждёт расписание";
        if (IsDriverIntercity(driver))
            return "Межгород";
        if (driver.IsOnActiveShift)
            return "На смене";
        if (driver.WaitingForShiftAtParking)
            return "Ждёт смену";
        if (driver.WalkPhase == DriverRescuePhase.WalkToLocalBusStop)
            return $"Идёт к остановке #{driver.BusOriginStopNumber}";
        if (driver.WalkPhase == DriverRescuePhase.WaitingAtLocalBusStop)
            return $"Ждёт автобус #{driver.BusOriginStopNumber}";
        if (driver.WalkPhase == DriverRescuePhase.RidingLocalBus)
            return driver.BusDestinationStopNumber > 0
                ? $"Едет до остановки #{driver.BusDestinationStopNumber}"
                : "Едет в автобусе";
        if (driver.WalkPhase == DriverRescuePhase.ToMotelFromBusStop)
            return "Идёт от остановки";
        if (driver.WalkPhase == DriverRescuePhase.IdleWander)
            return "Гуляет";
        if (driver.WalkPhase == DriverRescuePhase.ToPersonalHouseMeal || driver.WalkPhase == DriverRescuePhase.IdleAtPersonalHouseMeal)
            return "Ест дома";
        if (driver.WalkPhase == DriverRescuePhase.IdleWalkToCanteen || driver.WalkPhase == DriverRescuePhase.IdleAtCanteen)
            return "В столовой";
        if (driver.WalkPhase == DriverRescuePhase.IdleWalkToTrashCan || driver.WalkPhase == DriverRescuePhase.IdleAtTrashCan)
            return "Ищет еду";
        if (driver.WalkPhase == DriverRescuePhase.IdleWalkToGamblingHall || driver.WalkPhase == DriverRescuePhase.IdleAtGamblingHall)
            return "В зале игр";

        LocationType? serviceLocation = GetDriverServiceLocation(driver.WalkPhase);
        if (serviceLocation.HasValue)
        {
            return GetSelectedLocationDisplayName(serviceLocation.Value);
        }

        if (driver.ShiftStartHour >= 0)
            return $"Смена в {driver.ShiftStartHour:00}:00";

        return "Бездельничает";
    }

    private void FocusDriver(int driverId)
    {
        selectedDriverId = driverId;
        isDriverDetailsOpen = true;
        isTruckDetailsOpen = false;
        isLocalBusDetailsOpen = false;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        HideBuildingQuickHudSubmenuImmediate();
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        DriverAgent driver = driverAgents.Find(d => d.DriverId == driverId);
        if (driver != null)
        {
            if (TryFocusCameraOnDriver(driver, out string targetLabel))
            {
                LogUiInput($"Selection: focused {driver.DriverName} at {targetLabel}");
            }
            else
            {
                LogUiInput($"Selection: focused {driver.DriverName}");
            }
        }
        RefreshSelectionVisuals();
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }

    private string GetWorkerOccupationLabel(DriverAgent driver)
    {
        if (driver != null && driver.LifeGoal == WorkerLifeGoal.FindJob)
            return "Job Seeker";

        if (IsDriverBusDriver(driver))
            return "Bus Driver";

        if (driver.DutyMode == DriverDutyMode.Intercity)
            return "Intercity Driver";

        if (driver.DutyMode == DriverDutyMode.Logistics && driver.AssignedBuildingType.HasValue)
        {
            return GetBuildingWorkerRoleLabel(driver.AssignedBuildingType.Value);
        }

        if (driver.AssignedTruckNumber > 0)
            return "Truck Driver";

        return "Unemployed";
    }

    private void ClearDriverFocus()
    {
        if (isDriverDetailsOpen)
        {
            DriverAgent driver = driverAgents.Find(d => d.DriverId == selectedDriverId);
            if (driver != null)
                LogUiInput($"Selection: cleared {driver.DriverName}");
        }
        isDriverDetailsOpen = false;
        selectedDriverId = 0;
        isFleetScreenDirty = true;
        RefreshSelectionVisuals();
        PlayUiSound(uiPanelCloseClip, 0.82f);
    }
}
