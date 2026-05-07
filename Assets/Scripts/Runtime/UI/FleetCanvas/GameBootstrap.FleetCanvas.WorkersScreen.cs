using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void SetupDriversScreenUi()
    {
        if (driversScreenUi != null) return;
        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        driversScreenUi = new DriversScreenUiRefs();

        // Canvas
        GameObject canvasObject = new("DriversScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        driversScreenUi.CanvasRoot = canvasObject;

        // Window root — two-panel layout (980×560)
        GameObject windowRoot = CreateUiObject("DriversWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        SetCenteredWindow(windowRect, 1080f, 620f, -16f);
        driversScreenUi.WindowRoot = windowRect;
        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = DriversScreenTint;
        Outline windowOutline = windowRoot.AddComponent<Outline>();
        windowOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        windowOutline.effectDistance = new Vector2(2f, -2f);
        HorizontalLayoutGroup rootLayout = windowRoot.AddComponent<HorizontalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 16;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = true;

        // ── LEFT PANEL ────────────────────────────────────────────────────────
        RectTransform leftPanel = CreateStyledPanel("WorkersLeftPanel", windowRoot.transform, FleetPanelColor);
        driversScreenUi.LeftPanel = leftPanel;
        LayoutElement leftPanelLE = leftPanel.gameObject.AddComponent<LayoutElement>();
        leftPanelLE.preferredWidth = 360f;
        leftPanelLE.minWidth      = 360f;
        leftPanelLE.flexibleWidth  = 0f;
        leftPanelLE.flexibleHeight = 1f;
        VerticalLayoutGroup leftLayout = leftPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        leftLayout.padding = new RectOffset(16, 16, 16, 16);
        leftLayout.spacing = 16;
        leftLayout.childControlWidth = true;
        leftLayout.childControlHeight = true;
        leftLayout.childForceExpandWidth = true;
        leftLayout.childForceExpandHeight = false;

        // Header
        RectTransform headerRow = CreateLayoutRow("DriversHeaderRow", leftPanel, 40f, 0f);
        Text titleText = CreateHeaderText("DriversTitle", headerRow, font, "Workers", 24, TextAnchor.MiddleLeft, Color.white);
        titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        driversScreenUi.HeaderCountText = CreateHeaderText("DriversCount", headerRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        // Scrollable compact worker rows
        RectTransform listFrame = CreateStyledPanel("WorkersListFrame", leftPanel, FleetInsetColor);
        listFrame.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        FleetCanvasUiFactory.ScrollPanelRefs workerScroll = CreateVerticalScrollPanel(
            "WorkersScrollView",
            listFrame,
            new Color(0f, 0f, 0f, 0.04f),
            8f,
            8f);
        StretchRect(workerScroll.Root, 8f, 8f, 22f, 8f);
        workerScroll.ScrollRect.vertical = true;
        workerScroll.ScrollRect.movementType = ScrollRect.MovementType.Clamped;
        workerScroll.ScrollRect.inertia = false;
        workerScroll.ScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        workerScroll.ScrollRect.verticalScrollbar = CreatePanelScrollbar("WorkersScrollbar", listFrame);
        RectTransform contentRect = workerScroll.Content;
        driversScreenUi.WorkerListContent = contentRect;
        driversScreenUi.WorkerListScrollRect = workerScroll.ScrollRect;

        for (int i = 0; i < InitialWorkerRowSlots; i++)
        {
            driversScreenUi.WorkerRows.Add(CreateWorkerRow(contentRect, font, i));
        }

        // Hire section
        RectTransform hireSection = CreateStyledPanel("HireSection", leftPanel, FleetInsetColor);
        hireSection.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
        VerticalLayoutGroup hireLayout = hireSection.gameObject.AddComponent<VerticalLayoutGroup>();
        hireLayout.padding = new RectOffset(18, 18, 16, 14);
        hireLayout.spacing = 10;
        hireLayout.childAlignment = TextAnchor.MiddleCenter;
        hireLayout.childControlWidth = true;
        hireLayout.childControlHeight = true;
        hireLayout.childForceExpandWidth = true;
        hireLayout.childForceExpandHeight = false;
        driversScreenUi.HireButton = CreateButton("HireDriverButton", hireSection, font, out driversScreenUi.HireButtonText, "Workers arrive automatically", 16, FleetPrimaryButtonColor, Color.white);
        driversScreenUi.HireButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        driversScreenUi.HireButton.onClick.AddListener(() =>
        {
            LogUiInput("Drivers Canvas: clicked disabled direct worker hire info");
            isDriversScreenDirty = true;
        });
        driversScreenUi.HireStatusText = CreateBodyText("HireStatus", hireSection, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetSecondaryTextColor);

        // ── RIGHT PANEL ───────────────────────────────────────────────────────
        RectTransform rightPanel = CreateStyledPanel("WorkersDetailPanel", windowRoot.transform, FleetPanelColor);
        driversScreenUi.RightPanel = rightPanel;
        LayoutElement rightPanelLE = rightPanel.gameObject.AddComponent<LayoutElement>();
        rightPanelLE.flexibleWidth  = 1f;
        rightPanelLE.flexibleHeight = 1f;
        VerticalLayoutGroup rightLayout = rightPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        rightLayout.padding = new RectOffset(18, 18, 18, 18);
        rightLayout.spacing = 14;
        rightLayout.childControlWidth = true;
        rightLayout.childControlHeight = true;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;

        // Placeholder card (shown when nothing selected)
        RectTransform placeholderCard = CreateSectionCard(rightPanel, font, string.Empty, out RectTransform placeholderBody, false);
        placeholderCard.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        VerticalLayoutGroup plBodyLayout = placeholderBody.GetComponent<VerticalLayoutGroup>();
        plBodyLayout.childAlignment = TextAnchor.MiddleCenter;
        plBodyLayout.childForceExpandHeight = true;
        CreateBodyText("DetailPlaceholder", placeholderBody, font, "Select a worker from the list to view details.", 15, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
        driversScreenUi.DetailPlaceholderCard = placeholderCard.gameObject;

        // Detail content (hidden until a worker is selected)
        GameObject detailRoot = CreateUiObject("WorkerDetailRoot", rightPanel);
        driversScreenUi.DetailContentRoot = detailRoot;
        detailRoot.AddComponent<LayoutElement>().flexibleHeight = 1f;
        VerticalLayoutGroup detailGroup = detailRoot.AddComponent<VerticalLayoutGroup>();
        detailGroup.spacing = 14;
        detailGroup.childControlWidth = true;
        detailGroup.childControlHeight = true;
        detailGroup.childForceExpandWidth = true;
        detailGroup.childForceExpandHeight = false;

        // Name + status badge header row
        RectTransform detailHeaderRow = CreateLayoutRow("DetailHeaderRow", detailRoot.transform, 36f, 12f);
        driversScreenUi.DetailNameText = CreateHeaderText("DetailName", detailHeaderRow, font, string.Empty, 22, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailNameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        RectTransform statusBadge = CreateStyledPanel("DetailStatusBadge", detailHeaderRow, new Color(0.24f, 0.29f, 0.36f, 1f));
        driversScreenUi.DetailStatusBadge = statusBadge.GetComponent<Image>();
        statusBadge.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;
        driversScreenUi.DetailStatusText = CreateBodyText("DetailStatus", statusBadge, font, string.Empty, 12, TextAnchor.MiddleCenter, Color.white);
        driversScreenUi.DetailStatusText.fontStyle = FontStyle.Bold;

        RectTransform detailTabRow = CreateTabRow("WorkerDetailTabRow", detailRoot.transform, 36f, 8f);
        driversScreenUi.DetailProfileTabButton = CreateButton("WorkerProfileTabBtn", detailTabRow, font, out driversScreenUi.DetailProfileTabText, "Profile", 13, FleetPrimaryButtonColor, Color.white);
        driversScreenUi.DetailProfileTabText.fontStyle = FontStyle.Bold;
        driversScreenUi.DetailProfileTabButton.transition = Selectable.Transition.None;
        LayoutElement profileTabLayout = driversScreenUi.DetailProfileTabButton.gameObject.AddComponent<LayoutElement>();
        profileTabLayout.preferredHeight = 36f;
        profileTabLayout.flexibleWidth = 1f;
        driversScreenUi.DetailProfileTabButton.onClick.AddListener(() =>
        {
            activeWorkerDetailTab = WorkerDetailTab.Profile;
            isDriversScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.7f);
        });

        driversScreenUi.DetailSocialTabButton = CreateButton("WorkerSocialTabBtn", detailTabRow, font, out driversScreenUi.DetailSocialTabText, "Social Links", 13, new Color(0.08f, 0.10f, 0.14f, 1f), Color.white);
        driversScreenUi.DetailSocialTabText.fontStyle = FontStyle.Bold;
        driversScreenUi.DetailSocialTabButton.transition = Selectable.Transition.None;
        LayoutElement socialTabLayout = driversScreenUi.DetailSocialTabButton.gameObject.AddComponent<LayoutElement>();
        socialTabLayout.preferredHeight = 36f;
        socialTabLayout.flexibleWidth = 1f;
        driversScreenUi.DetailSocialTabButton.onClick.AddListener(() =>
        {
            activeWorkerDetailTab = WorkerDetailTab.Social;
            isDriversScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.7f);
        });

        driversScreenUi.DetailThoughtsTabButton = CreateButton("WorkerThoughtsTabBtn", detailTabRow, font, out driversScreenUi.DetailThoughtsTabText, "Thoughts", 13, new Color(0.08f, 0.10f, 0.14f, 1f), Color.white);
        driversScreenUi.DetailThoughtsTabText.fontStyle = FontStyle.Bold;
        driversScreenUi.DetailThoughtsTabButton.transition = Selectable.Transition.None;
        LayoutElement thoughtsTabLayout = driversScreenUi.DetailThoughtsTabButton.gameObject.AddComponent<LayoutElement>();
        thoughtsTabLayout.preferredHeight = 36f;
        thoughtsTabLayout.flexibleWidth = 1f;
        driversScreenUi.DetailThoughtsTabButton.onClick.AddListener(() =>
        {
            activeWorkerDetailTab = WorkerDetailTab.Thoughts;
            isDriversScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.7f);
        });

        driversScreenUi.DetailInventoryTabButton = CreateButton("WorkerInventoryTabBtn", detailTabRow, font, out driversScreenUi.DetailInventoryTabText, "Inventory", 13, new Color(0.08f, 0.10f, 0.14f, 1f), Color.white);
        driversScreenUi.DetailInventoryTabText.fontStyle = FontStyle.Bold;
        driversScreenUi.DetailInventoryTabButton.transition = Selectable.Transition.None;
        LayoutElement inventoryTabLayout = driversScreenUi.DetailInventoryTabButton.gameObject.AddComponent<LayoutElement>();
        inventoryTabLayout.preferredHeight = 36f;
        inventoryTabLayout.flexibleWidth = 1f;
        driversScreenUi.DetailInventoryTabButton.onClick.AddListener(() =>
        {
            activeWorkerDetailTab = WorkerDetailTab.Inventory;
            isDriversScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.7f);
        });

        RectTransform profileTabRoot = CreateUiObject("WorkerProfileTabRoot", detailRoot.transform).GetComponent<RectTransform>();
        driversScreenUi.DetailProfileTabRoot = profileTabRoot.gameObject;
        VerticalLayoutGroup profileTabLayoutGroup = profileTabRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        profileTabLayoutGroup.spacing = 14f;
        profileTabLayoutGroup.childControlWidth = true;
        profileTabLayoutGroup.childControlHeight = true;
        profileTabLayoutGroup.childForceExpandWidth = true;
        profileTabLayoutGroup.childForceExpandHeight = false;

        RectTransform socialTabRoot = CreateUiObject("WorkerSocialTabRoot", detailRoot.transform).GetComponent<RectTransform>();
        driversScreenUi.DetailSocialTabRoot = socialTabRoot.gameObject;
        socialTabRoot.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        VerticalLayoutGroup socialTabLayoutGroup = socialTabRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        socialTabLayoutGroup.spacing = 14f;
        socialTabLayoutGroup.childControlWidth = true;
        socialTabLayoutGroup.childControlHeight = true;
        socialTabLayoutGroup.childForceExpandWidth = true;
        socialTabLayoutGroup.childForceExpandHeight = false;
        driversScreenUi.DetailSocialTabRoot.SetActive(false);

        RectTransform thoughtsTabRoot = CreateUiObject("WorkerThoughtsTabRoot", detailRoot.transform).GetComponent<RectTransform>();
        driversScreenUi.DetailThoughtsTabRoot = thoughtsTabRoot.gameObject;
        thoughtsTabRoot.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        VerticalLayoutGroup thoughtsTabLayoutGroup = thoughtsTabRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        thoughtsTabLayoutGroup.spacing = 14f;
        thoughtsTabLayoutGroup.childControlWidth = true;
        thoughtsTabLayoutGroup.childControlHeight = true;
        thoughtsTabLayoutGroup.childForceExpandWidth = true;
        thoughtsTabLayoutGroup.childForceExpandHeight = false;
        driversScreenUi.DetailThoughtsTabRoot.SetActive(false);

        RectTransform inventoryTabRoot = CreateUiObject("WorkerInventoryTabRoot", detailRoot.transform).GetComponent<RectTransform>();
        driversScreenUi.DetailInventoryTabRoot = inventoryTabRoot.gameObject;
        inventoryTabRoot.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        VerticalLayoutGroup inventoryTabLayoutGroup = inventoryTabRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        inventoryTabLayoutGroup.spacing = 14f;
        inventoryTabLayoutGroup.childControlWidth = true;
        inventoryTabLayoutGroup.childControlHeight = true;
        inventoryTabLayoutGroup.childForceExpandWidth = true;
        inventoryTabLayoutGroup.childForceExpandHeight = false;
        driversScreenUi.DetailInventoryTabRoot.SetActive(false);

        // Profile card
        RectTransform profileCard = CreateStyledPanel("WorkerProfileCard", profileTabRoot, FleetInsetColor);
        profileCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 104f;
        HorizontalLayoutGroup profileLayout = profileCard.gameObject.AddComponent<HorizontalLayoutGroup>();
        profileLayout.padding = new RectOffset(16, 16, 10, 10);
        profileLayout.spacing = 14f;
        profileLayout.childAlignment = TextAnchor.MiddleLeft;
        profileLayout.childControlWidth = true;
        profileLayout.childControlHeight = true;
        profileLayout.childForceExpandWidth = false;
        profileLayout.childForceExpandHeight = false;

        driversScreenUi.DetailPortraitRoot = CreateUiObject("WorkerPortraitRoot", profileCard).GetComponent<RectTransform>();
        LayoutElement portraitRootLayout = driversScreenUi.DetailPortraitRoot.gameObject.AddComponent<LayoutElement>();
        portraitRootLayout.preferredWidth = 112f;
        portraitRootLayout.preferredHeight = 98f;
        portraitRootLayout.minWidth = 112f;
        portraitRootLayout.minHeight = 98f;

        RectTransform profileInfoColumn = CreateUiObject("WorkerProfileInfoColumn", profileCard).GetComponent<RectTransform>();
        LayoutElement profileInfoLayoutElement = profileInfoColumn.gameObject.AddComponent<LayoutElement>();
        profileInfoLayoutElement.flexibleWidth = 1f;
        VerticalLayoutGroup profileInfoLayout = profileInfoColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        profileInfoLayout.spacing = 6f;
        profileInfoLayout.childControlWidth = true;
        profileInfoLayout.childControlHeight = true;
        profileInfoLayout.childForceExpandWidth = true;
        profileInfoLayout.childForceExpandHeight = false;

        driversScreenUi.DetailProfileTitleText = CreateHeaderText("WorkerProfileTitle", profileInfoColumn, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailProfileTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        driversScreenUi.DetailRoleText = CreateHeaderText("WorkerRole", profileInfoColumn, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailRoleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        RectTransform conditionRow = CreateLayoutRow("WorkerConditionRow", profileTabRoot, 142f, 14f);

        RectTransform needsCard = CreateStyledPanel("WorkerNeedsCard", conditionRow, FleetInsetColor);
        needsCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup needsCardLayout = needsCard.gameObject.AddComponent<VerticalLayoutGroup>();
        needsCardLayout.padding = new RectOffset(16, 16, 8, 8);
        needsCardLayout.spacing = 4f;
        needsCardLayout.childControlWidth = true;
        needsCardLayout.childControlHeight = true;
        needsCardLayout.childForceExpandWidth = true;
        needsCardLayout.childForceExpandHeight = false;

        RectTransform needsColumn = CreateUiObject("WorkerNeedsColumn", needsCard).GetComponent<RectTransform>();
        needsColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup needsLayout = needsColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        needsLayout.spacing = 4f;
        needsLayout.childControlWidth = true;
        needsLayout.childControlHeight = true;
        needsLayout.childForceExpandWidth = true;
        needsLayout.childForceExpandHeight = false;
        driversScreenUi.DetailNeedsTitleText = CreateHeaderText("WorkerNeedsTitle", needsColumn, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailNeedsTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        driversScreenUi.DetailMealNeedText = CreateBodyText("WorkerMealNeed", needsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailMealNeedText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        driversScreenUi.DetailSleepNeedText = CreateBodyText("WorkerSleepNeed", needsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailSleepNeedText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        driversScreenUi.DetailLeisureNeedText = CreateBodyText("WorkerLeisureNeed", needsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailLeisureNeedText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        RectTransform perksCard = CreateStyledPanel("WorkerPerksCard", conditionRow, FleetInsetColor);
        perksCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup perksCardLayout = perksCard.gameObject.AddComponent<VerticalLayoutGroup>();
        perksCardLayout.padding = new RectOffset(16, 16, 8, 8);
        perksCardLayout.spacing = 4f;
        perksCardLayout.childControlWidth = true;
        perksCardLayout.childControlHeight = true;
        perksCardLayout.childForceExpandWidth = true;
        perksCardLayout.childForceExpandHeight = false;

        RectTransform perksColumn = CreateUiObject("WorkerPerksColumn", perksCard).GetComponent<RectTransform>();
        perksColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup perksLayout = perksColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        perksLayout.spacing = 4f;
        perksLayout.childControlWidth = true;
        perksLayout.childControlHeight = true;
        perksLayout.childForceExpandWidth = true;
        perksLayout.childForceExpandHeight = false;
        driversScreenUi.DetailPerksTitleText = CreateHeaderText("WorkerPerksTitle", perksColumn, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailPerksTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        driversScreenUi.DetailPerksEmptyText = CreateBodyText("WorkerPerksEmpty", perksColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailPerksEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        for (int i = 0; i < WorkerPerkHudRowCount; i++)
        {
            Text perkText = CreateBodyText($"WorkerPerkRow{i + 1}", perksColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
            perkText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
            perkText.gameObject.SetActive(false);
            driversScreenUi.DetailPerkTexts.Add(perkText);
        }

        // Assignment info card — compact rows, label refs stored for post-localization update
        RectTransform assignCard = CreateSectionCard(profileTabRoot, font, string.Empty, out RectTransform assignBody, false);
        assignCard.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 10, 10);
        assignCard.GetComponent<VerticalLayoutGroup>().spacing = 4;
        assignBody.GetComponent<VerticalLayoutGroup>().spacing = 4;
        driversScreenUi.DetailWorkTitleText = CreateHeaderText("WorkerWorkTitle", assignBody, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailWorkTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform assignRow = CreateLayoutRow("AssignRow", assignBody, 20f, 12f);
        driversScreenUi.DetailAssignmentLabel = CreateBodyText("AL", assignRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailAssignmentLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailAssignmentValue = CreateHeaderText("AV", assignRow, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailAssignmentValue.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform shiftRow = CreateLayoutRow("ShiftRow", assignBody, 20f, 12f);
        driversScreenUi.DetailShiftLabel = CreateBodyText("SL", shiftRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailShiftLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailShiftText = CreateHeaderText("SV", shiftRow, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailShiftText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform dutyRow = CreateLayoutRow("DutyRow", assignBody, 20f, 12f);
        driversScreenUi.DetailDutyLabel = CreateBodyText("DL", dutyRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailDutyLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailDutyStateText = CreateBodyText("DV", dutyRow, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        driversScreenUi.DetailDutyStateText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform homeRow = CreateLayoutRow("HomeRow", assignBody, 20f, 12f);
        driversScreenUi.DetailHomeLabel = CreateBodyText("HomeL", homeRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailHomeLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailHomeText = CreateBodyText("HomeV", homeRow, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailHomeText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform carRow = CreateLayoutRow("CarRow", assignBody, 20f, 12f);
        driversScreenUi.DetailCarLabel = CreateBodyText("CarL", carRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailCarLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailCarText = CreateBodyText("CarV", carRow, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailCarText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform ageRow = CreateLayoutRow("AgeRow", assignBody, 20f, 12f);
        driversScreenUi.DetailAgeLabel = CreateBodyText("AgeL", ageRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailAgeLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailAgeText = CreateBodyText("AgeV", ageRow, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailAgeText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Contract card (salary, balance) — compact
        RectTransform statsCard = CreateSectionCard(profileTabRoot, font, string.Empty, out RectTransform statsBody, false);
        statsCard.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 10, 10);
        statsCard.GetComponent<VerticalLayoutGroup>().spacing = 4;
        statsBody.GetComponent<VerticalLayoutGroup>().spacing = 6;
        driversScreenUi.DetailContractTitleText = CreateHeaderText("WorkerContractTitle", statsBody, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailContractTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform salaryRow = CreateLayoutRow("SalaryEditRow", statsBody, 28f, 6f);
        driversScreenUi.DetailSalaryLabel = CreateBodyText("SRL", salaryRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailSalaryLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailSalaryMinusBtn = CreateButton("SalaryMinus", salaryRow, font, out Text minusTxt, "-", 14, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
        minusTxt.fontStyle = FontStyle.Bold;
        LayoutElement minusLE = driversScreenUi.DetailSalaryMinusBtn.gameObject.AddComponent<LayoutElement>();
        minusLE.preferredWidth = 28f; minusLE.preferredHeight = 24f;
        RectTransform salaryValPanel = CreateStyledPanel("SalaryValPanel", salaryRow, new Color(0.09f, 0.13f, 0.19f, 1f));
        LayoutElement salaryValLE = salaryValPanel.gameObject.AddComponent<LayoutElement>();
        salaryValLE.preferredWidth = 230f; salaryValLE.preferredHeight = 24f;
        driversScreenUi.DetailSalaryText = CreateHeaderText("SalaryVal", salaryValPanel, font, string.Empty, 13, TextAnchor.MiddleCenter, Color.white);
        driversScreenUi.DetailSalaryPlusBtn = CreateButton("SalaryPlus", salaryRow, font, out Text plusTxt, "+", 14, new Color(0.24f, 0.38f, 0.24f, 1f), Color.white);
        plusTxt.fontStyle = FontStyle.Bold;
        LayoutElement plusLE = driversScreenUi.DetailSalaryPlusBtn.gameObject.AddComponent<LayoutElement>();
        plusLE.preferredWidth = 28f; plusLE.preferredHeight = 24f;
        CreateBodyText("PerShift", salaryRow, font, "/ contract", 12, TextAnchor.MiddleLeft, FleetMutedTextColor).gameObject.AddComponent<LayoutElement>().preferredWidth = 70f;
        driversScreenUi.DetailSalaryMinusBtn.onClick.RemoveAllListeners();
        driversScreenUi.DetailSalaryPlusBtn.onClick.RemoveAllListeners();
        driversScreenUi.DetailSalaryMinusBtn.gameObject.SetActive(false);
        driversScreenUi.DetailSalaryPlusBtn.gameObject.SetActive(false);

        RectTransform balanceRow = CreateLayoutRow("BalanceRow", statsBody, 20f, 12f);
        driversScreenUi.DetailBalanceLabel = CreateBodyText("BL", balanceRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailBalanceLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailBalanceText = CreateHeaderText("BV", balanceRow, font, string.Empty, 14, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailBalanceText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform socialCard = CreateSectionCard(socialTabRoot, font, string.Empty, out RectTransform socialBody, false);
        LayoutElement socialCardLayout = socialCard.gameObject.AddComponent<LayoutElement>();
        socialCardLayout.preferredHeight = 260f;
        socialCardLayout.flexibleHeight = 1f;
        socialCard.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 10, 10);
        socialCard.GetComponent<VerticalLayoutGroup>().spacing = 4;
        socialBody.GetComponent<VerticalLayoutGroup>().spacing = 4;
        driversScreenUi.DetailSocialTitleText = CreateHeaderText("WorkerSocialTitle", socialBody, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailSocialTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform socialHeader = CreateLayoutRow("WorkerSocialHeader", socialBody, 16f, 8f);
        driversScreenUi.DetailSocialNameHeaderText = CreateBodyText("SocialNameHeader", socialHeader, font, string.Empty, 10, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailSocialNameHeaderText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        driversScreenUi.DetailSocialRelationHeaderText = CreateBodyText("SocialRelationHeader", socialHeader, font, string.Empty, 10, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailSocialRelationHeaderText.gameObject.AddComponent<LayoutElement>().preferredWidth = 86f;
        driversScreenUi.DetailSocialFamiliarityHeaderText = CreateBodyText("SocialFamiliarityHeader", socialHeader, font, string.Empty, 10, TextAnchor.MiddleRight, FleetMutedTextColor);
        driversScreenUi.DetailSocialFamiliarityHeaderText.gameObject.AddComponent<LayoutElement>().preferredWidth = 42f;
        driversScreenUi.DetailSocialContextHeaderText = CreateBodyText("SocialContextHeader", socialHeader, font, string.Empty, 10, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailSocialContextHeaderText.gameObject.AddComponent<LayoutElement>().preferredWidth = 126f;

        driversScreenUi.DetailSocialEmptyText = CreateBodyText("WorkerSocialEmpty", socialBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailSocialEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        for (int i = 0; i < WorkerSocialHudRowCount; i++)
        {
            driversScreenUi.DetailSocialRows.Add(CreateWorkerSocialRow(socialBody, font, i));
        }

        RectTransform thoughtsCard = CreateSectionCard(thoughtsTabRoot, font, string.Empty, out RectTransform thoughtsBody, false);
        LayoutElement thoughtsCardLayout = thoughtsCard.gameObject.AddComponent<LayoutElement>();
        thoughtsCardLayout.preferredHeight = 336f;
        thoughtsCardLayout.flexibleHeight = 1f;
        thoughtsCard.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 10, 10);
        thoughtsCard.GetComponent<VerticalLayoutGroup>().spacing = 6;
        thoughtsBody.GetComponent<VerticalLayoutGroup>().spacing = 6;
        driversScreenUi.DetailThoughtsTitleText = CreateHeaderText("WorkerThoughtsTitle", thoughtsBody, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailThoughtsTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform opinionRow = CreateLayoutRow("WorkerOpinionRow", thoughtsBody, 28f, 8f);
        for (int i = 0; i < WorkerOpinionHudChipCount; i++)
        {
            Text chipText = CreateBodyText($"WorkerOpinionChip{i + 1}", opinionRow, font, string.Empty, 11, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
            chipText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            chipText.gameObject.SetActive(false);
            driversScreenUi.DetailOpinionChipTexts.Add(chipText);
        }

        driversScreenUi.DetailThoughtsEmptyText = CreateBodyText("WorkerThoughtsEmpty", thoughtsBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailThoughtsEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        for (int i = 0; i < WorkerThoughtHudRowCount; i++)
        {
            driversScreenUi.DetailThoughtRows.Add(CreateWorkerThoughtRow(thoughtsBody, font, i));
        }

        SetupWorkerInventoryUi(inventoryTabRoot, font);

        // Focus button
        driversScreenUi.DetailFocusButton = CreateButton("FocusWorkerBtn", detailRoot.transform, font, out driversScreenUi.DetailFocusButtonText, "Focus on Worker", 14, new Color(0.25f, 0.33f, 0.46f, 1f), Color.white);
        driversScreenUi.DetailFocusButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        driversScreenUi.DetailFocusButton.onClick.AddListener(() =>
        {
            DriverAgent d = driverAgents.Find(x => x.DriverId == selectedWorkerPanelDriverId);
            if (d == null) return;
            isDriversPanelOpen = false;
            isDriversScreenDirty = true;
            FocusDriver(d.DriverId);
        });

        detailRoot.SetActive(false);
        SetupWorkerTraitTooltip(windowRect, font);

        AddOverlayCloseButton(windowRect, font);
        driversScreenUi.CanvasRoot.SetActive(false);
        UpdateDriversScreenUi();
    }

    private void EnsureWorkerRows(int targetCount)
    {
        if (driversScreenUi?.WorkerListContent == null || targetCount <= driversScreenUi.WorkerRows.Count)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        while (driversScreenUi.WorkerRows.Count < targetCount)
        {
            int rowIndex = driversScreenUi.WorkerRows.Count;
            driversScreenUi.WorkerRows.Add(CreateWorkerRow(driversScreenUi.WorkerListContent, font, rowIndex));
        }
    }

    private WorkerSocialRowUi CreateWorkerSocialRow(RectTransform parent, Font font, int index)
    {
        WorkerSocialRowUi row = new();
        row.Root = CreateLayoutRow($"WorkerSocialRow{index + 1}", parent, 17f, 8f);
        row.NameText = CreateBodyText("Name", row.Root, font, string.Empty, 11, TextAnchor.MiddleLeft, Color.white);
        row.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        row.RelationText = CreateBodyText("Relation", row.Root, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        row.RelationText.gameObject.AddComponent<LayoutElement>().preferredWidth = 86f;
        row.FamiliarityText = CreateHeaderText("Familiarity", row.Root, font, string.Empty, 11, TextAnchor.MiddleRight, FleetAccentColor);
        row.FamiliarityText.gameObject.AddComponent<LayoutElement>().preferredWidth = 42f;
        row.ContextText = CreateBodyText("Context", row.Root, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        row.ContextText.gameObject.AddComponent<LayoutElement>().preferredWidth = 126f;
        row.Root.gameObject.SetActive(false);
        return row;
    }

    private WorkerRowUi CreateWorkerRow(RectTransform parent, Font font, int index)
    {
        WorkerRowUi row = new();
        GameObject rowObj = CreateUiObject($"WorkerRow{index + 1}", parent);
        row.Root = rowObj.GetComponent<RectTransform>();
        rowObj.AddComponent<LayoutElement>().preferredHeight = 80f;
        row.Background = rowObj.AddComponent<Image>();
        row.Background.color = DriversCardColor;
        Outline outline = rowObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        outline.effectDistance = new Vector2(1f, -1f);
        VerticalLayoutGroup rowLayout = rowObj.AddComponent<VerticalLayoutGroup>();
        rowLayout.padding = new RectOffset(14, 14, 10, 10);
        rowLayout.spacing = 5;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;

        RectTransform nameRow = CreateLayoutRow($"WorkerNameRow{index}", rowObj.transform, 22f, 8f);
        row.NameText = CreateHeaderText("Name", nameRow, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
        row.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        RectTransform badge = CreateStyledPanel($"WorkerStatusBadge{index}", nameRow, new Color(0.24f, 0.29f, 0.36f, 1f));
        row.StatusBadgeBg = badge.GetComponent<Image>();
        badge.gameObject.AddComponent<LayoutElement>().preferredWidth = 112f;
        row.StatusText = CreateBodyText("Status", badge, font, string.Empty, 10, TextAnchor.MiddleCenter, Color.white);
        row.StatusText.fontStyle = FontStyle.Bold;

        RectTransform subRow = CreateLayoutRow($"WorkerSubRow{index}", rowObj.transform, 18f, 6f);
        row.SubText = CreateBodyText("SubText", subRow, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        row.SubText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        row.BalanceText = CreateHeaderText("Balance", subRow, font, string.Empty, 12, TextAnchor.MiddleRight, FleetAccentColor);
        row.BalanceText.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;

        RectTransform needsRow = CreateUiObject($"NeedsRow{index}", rowObj.transform).GetComponent<RectTransform>();
        HorizontalLayoutGroup needsLayout = needsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        needsLayout.spacing = 6f;
        needsLayout.childAlignment = TextAnchor.MiddleLeft;
        needsLayout.childControlWidth = false;
        needsLayout.childControlHeight = false;
        needsLayout.childForceExpandWidth = false;
        needsLayout.childForceExpandHeight = false;
        needsRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 10f;
        row.NeedsMealBarFill    = CreateNeedsMiniBar(needsRow, GetNeedsMealIcon(),    $"Meal{index}",    60f);
        row.NeedsSleepBarFill   = CreateNeedsMiniBar(needsRow, GetNeedsSleepIcon(),   $"Sleep{index}",   60f);
        row.NeedsLeisureBarFill = CreateNeedsMiniBar(needsRow, GetNeedsLeisureIcon(), $"Leisure{index}", 60f);

        row.SelectButton = rowObj.AddComponent<Button>();
        ColorBlock rowColors = row.SelectButton.colors;
        rowColors.normalColor = Color.white;
        rowColors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        rowColors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        rowColors.selectedColor = Color.white;
        rowColors.fadeDuration = 0.08f;
        row.SelectButton.colors = rowColors;

        row.SelectButton.onClick.AddListener(() =>
        {
            if (row.DriverId <= 0 || driverAgents.Find(d => d.DriverId == row.DriverId) == null)
            {
                return;
            }

            selectedWorkerPanelDriverId = selectedWorkerPanelDriverId == row.DriverId ? 0 : row.DriverId;
            PlayUiSound(uiSelectClip, 0.8f);
            isDriversScreenDirty = true;
        });

        return row;
    }

    private DriverCardUi CreateDriverCard(RectTransform parent, Font font, int cardIndex)
    {
        DriverCardUi card = new();
        GameObject cardObj = CreateUiObject($"DriverCard{cardIndex + 1}", parent);
        card.Root = cardObj.GetComponent<RectTransform>();
        card.Root.anchorMin = new Vector2(0f, 1f);
        card.Root.anchorMax = new Vector2(1f, 1f);
        card.Root.pivot = new Vector2(0.5f, 1f);
        card.Root.sizeDelta = new Vector2(0f, 128f);
        card.Root.anchoredPosition = Vector2.zero;
        LayoutElement cardLayoutElement = cardObj.AddComponent<LayoutElement>();
        cardLayoutElement.preferredHeight = 128f;
        cardLayoutElement.flexibleWidth = 1f;
        card.Background = cardObj.AddComponent<Image>();
        card.Background.color = DriversCardColor;
        Outline cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        cardOutline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup cardLayout = cardObj.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(16, 16, 12, 12);
        cardLayout.spacing = 6;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        // Row 1: Name + Status
        RectTransform nameRow = CreateLayoutRow($"NameRow{cardIndex}", cardObj.transform, 22f, 8f);
        card.NameText = CreateHeaderText("Name", nameRow, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        card.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        card.StatusText = CreateBodyText("Status", nameRow, font, string.Empty, 12, TextAnchor.MiddleRight, FleetMutedTextColor);
        card.StatusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 180f;

        // Row 2: Truck
        card.TruckText = CreateBodyText("Truck", cardObj.transform, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        card.TruckText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        // Row 3: Salary + Balance
        RectTransform salaryRow = CreateLayoutRow($"SalaryRow{cardIndex}", cardObj.transform, 28f, 6f);

        CreateBodyText("SalaryLabel", salaryRow, font, "Salary:", 13, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 56f;

        card.SalaryText = CreateHeaderText("SalaryValue", salaryRow, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        card.SalaryText.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;

        Button minusBtn = CreateButton($"SalaryMinus{cardIndex}", salaryRow, font, out Text minusTxt, "−", 14, new Color(0.32f, 0.26f, 0.18f, 1f), Color.white);
        minusTxt.fontStyle = FontStyle.Bold;
        LayoutElement minusLayout = minusBtn.gameObject.AddComponent<LayoutElement>();
        minusLayout.preferredWidth = 26f;
        minusLayout.preferredHeight = 26f;
        Button plusBtn = CreateButton($"SalaryPlus{cardIndex}", salaryRow, font, out Text plusTxt, "+", 14, new Color(0.24f, 0.38f, 0.24f, 1f), Color.white);
        plusTxt.fontStyle = FontStyle.Bold;
        LayoutElement plusLayout = plusBtn.gameObject.AddComponent<LayoutElement>();
        plusLayout.preferredWidth = 26f;
        plusLayout.preferredHeight = 26f;
        minusBtn.onClick.RemoveAllListeners();
        plusBtn.onClick.RemoveAllListeners();
        minusBtn.gameObject.SetActive(false);
        plusBtn.gameObject.SetActive(false);

        CreateBodyText("PerShift", salaryRow, font, "contract", 12, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 62f;

        // Spacer
        GameObject spacer = CreateUiObject($"SalarySpacer{cardIndex}", salaryRow);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;

        card.BalanceText = CreateBodyText("Balance", salaryRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetAccentColor);
        card.BalanceText.gameObject.AddComponent<LayoutElement>().preferredWidth = 140f;

        return card;
    }

    private DriverCardUi CreateDriverCardV2(RectTransform parent, Font font, int cardIndex)
    {
        DriverCardUi card = new();
        GameObject cardObj = CreateUiObject($"DriverCardV2_{cardIndex + 1}", parent);
        card.Root = cardObj.GetComponent<RectTransform>();
        card.Root.anchorMin = new Vector2(0f, 1f);
        card.Root.anchorMax = new Vector2(1f, 1f);
        card.Root.pivot = new Vector2(0.5f, 1f);
        card.Root.sizeDelta = new Vector2(0f, 170f);
        card.Root.anchoredPosition = Vector2.zero;

        LayoutElement cardLayoutElement = cardObj.AddComponent<LayoutElement>();
        cardLayoutElement.preferredHeight = 170f;
        cardLayoutElement.flexibleWidth = 1f;

        card.Background = cardObj.AddComponent<Image>();
        card.Background.color = DriversCardColor;
        Outline cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        cardOutline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup cardLayout = cardObj.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(16, 16, 14, 14);
        cardLayout.spacing = 12;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow($"DriverCardHeader{cardIndex}", cardObj.transform, 26f, 10f);
        card.NameText = CreateHeaderText("Name", headerRow, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        card.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform statusBadge = CreateStyledPanel($"DriverStatusBadge{cardIndex}", headerRow, new Color(0.24f, 0.29f, 0.36f, 1f));
        card.StatusBadgeBackground = statusBadge.GetComponent<Image>();
        LayoutElement statusBadgeLayout = statusBadge.gameObject.AddComponent<LayoutElement>();
        statusBadgeLayout.preferredWidth = 90f;
        statusBadgeLayout.preferredHeight = 24f;
        card.StatusText = CreateBodyText("Status", statusBadge, font, string.Empty, 11, TextAnchor.MiddleCenter, Color.white);
        card.StatusText.fontStyle = FontStyle.Bold;

        int cii = cardIndex;
        card.SelectButton = CreateButton($"DriverSelectBtn{cardIndex}", headerRow, font, out Text selectTxt, "Select", 11, new Color(0.25f, 0.33f, 0.46f, 1f), Color.white);
        selectTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        LayoutElement selectLayout = card.SelectButton.gameObject.AddComponent<LayoutElement>();
        selectLayout.preferredWidth = 64f;
        selectLayout.preferredHeight = 24f;
        card.SelectButton.onClick.AddListener(() =>
        {
            if (cii >= driverAgents.Count) return;
            DriverAgent d = driverAgents[cii];
            isDriversPanelOpen = false;
            isDriversScreenDirty = true;
            FocusDriver(d.DriverId);
        });

        RectTransform infoRow = CreateLayoutRow($"DriverCardInfo{cardIndex}", cardObj.transform, 52f, 12f);

        RectTransform truckPanel = CreateStyledPanel($"DriverTruckPanel{cardIndex}", infoRow, FleetCardMutedColor);
        truckPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup truckPanelLayout = truckPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        truckPanelLayout.padding = new RectOffset(12, 12, 8, 8);
        truckPanelLayout.spacing = 4;
        truckPanelLayout.childControlWidth = true;
        truckPanelLayout.childControlHeight = true;
        truckPanelLayout.childForceExpandWidth = true;
        truckPanelLayout.childForceExpandHeight = false;
        card.TruckLabelText = CreateBodyText("TruckLabel", truckPanel, font, "Truck", 11, TextAnchor.MiddleLeft, FleetMutedTextColor);
        card.TruckLabelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;
        card.TruckText = CreateBodyText("TruckValue", truckPanel, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        card.TruckText.fontStyle = FontStyle.Bold;
        card.TruckText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform footerRow = CreateLayoutRow($"DriverCardFooter{cardIndex}", cardObj.transform, 52f, 12f);

        RectTransform salaryPanel = CreateStyledPanel($"DriverSalaryPanel{cardIndex}", footerRow, FleetCardMutedColor);
        salaryPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup salaryPanelLayout = salaryPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        salaryPanelLayout.padding = new RectOffset(12, 12, 8, 8);
        salaryPanelLayout.spacing = 6;
        salaryPanelLayout.childControlWidth = true;
        salaryPanelLayout.childControlHeight = true;
        salaryPanelLayout.childForceExpandWidth = true;
        salaryPanelLayout.childForceExpandHeight = false;
        CreateBodyText("SalaryLabel", salaryPanel, font, "Salary", 11, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

        RectTransform salaryRow = CreateLayoutRow($"DriverSalaryRow{cardIndex}", salaryPanel, 28f, 8f);
        Button minusBtn = CreateButton($"DriverSalaryMinus{cardIndex}", salaryRow, font, out Text minusTxt, "-", 13, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
        minusTxt.fontStyle = FontStyle.Bold;
        LayoutElement minusLayout = minusBtn.gameObject.AddComponent<LayoutElement>();
        minusLayout.preferredWidth = 30f;
        minusLayout.preferredHeight = 26f;

        RectTransform salaryValuePanel = CreateStyledPanel($"DriverSalaryValuePanel{cardIndex}", salaryRow, new Color(0.09f, 0.13f, 0.19f, 1f));
        LayoutElement salaryValueLayout = salaryValuePanel.gameObject.AddComponent<LayoutElement>();
        salaryValueLayout.flexibleWidth = 1f;
        salaryValueLayout.preferredHeight = 26f;
        card.SalaryText = CreateHeaderText("SalaryValue", salaryValuePanel, font, string.Empty, 12, TextAnchor.MiddleCenter, Color.white);

        Button plusBtn = CreateButton($"DriverSalaryPlus{cardIndex}", salaryRow, font, out Text plusTxt, "+", 13, new Color(0.24f, 0.38f, 0.24f, 1f), Color.white);
        plusTxt.fontStyle = FontStyle.Bold;
        LayoutElement plusLayout = plusBtn.gameObject.AddComponent<LayoutElement>();
        plusLayout.preferredWidth = 30f;
        plusLayout.preferredHeight = 26f;
        minusBtn.onClick.RemoveAllListeners();
        plusBtn.onClick.RemoveAllListeners();
        minusBtn.gameObject.SetActive(false);
        plusBtn.gameObject.SetActive(false);

        RectTransform balancePanel = CreateStyledPanel($"DriverBalancePanel{cardIndex}", footerRow, FleetCardMutedColor);
        LayoutElement balanceLayout = balancePanel.gameObject.AddComponent<LayoutElement>();
        balanceLayout.preferredWidth = 120f;
        VerticalLayoutGroup balancePanelLayout = balancePanel.gameObject.AddComponent<VerticalLayoutGroup>();
        balancePanelLayout.padding = new RectOffset(12, 12, 8, 8);
        balancePanelLayout.spacing = 4;
        balancePanelLayout.childControlWidth = true;
        balancePanelLayout.childControlHeight = true;
        balancePanelLayout.childForceExpandWidth = true;
        balancePanelLayout.childForceExpandHeight = false;
        CreateBodyText("BalanceLabel", balancePanel, font, "Balance", 11, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;
        card.BalanceText = CreateHeaderText("BalanceValue", balancePanel, font, string.Empty, 14, TextAnchor.MiddleLeft, FleetAccentColor);
        card.BalanceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        return card;
    }

    private static string Gend(DriverAgent d, string male, string female) =>
        d?.Gender == WorkerGender.Female ? female : male;

    private static string GetWorkerGenderLabel(DriverAgent driver, bool ru)
    {
        if (driver == null)
        {
            return "—";
        }

        return driver.Gender == WorkerGender.Female
            ? (ru ? "Женщина" : "Female")
            : (ru ? "Мужчина" : "Male");
    }

    private string GetWorkerListStatusLabel(DriverAgent driver, bool ru)
    {
        if (driver == null) return ru ? "Свободен" : "Idle";

        TruckAgent truck = GetAssignedTruckForDriver(driver);
        bool isProduction = driver.DutyMode == DriverDutyMode.Logistics;
        bool isService = isProduction && driver.AssignedBuildingType.HasValue && HasServiceWorkerSlot(driver.AssignedBuildingType.Value);
        if (driver.WalkPhase == DriverRescuePhase.ToLaborExchangeForJob ||
            driver.WalkPhase == DriverRescuePhase.AtLaborExchange)
        {
            return ru ? "\u0418\u0449\u0435\u0442 \u0440\u0430\u0431\u043e\u0442\u0443" : "Job search";
        }
        return driver.IsArrivingByBus ? (ru ? "В пути" : "Arriving")
            : IsDriverOnActiveTradeRun(driver) ? (ru ? "Торговый рейс" : "Trade Run")
            : IsDriverIntercity(driver) ? (ru ? "Межгород" : "Intercity")
            : IsDriverBusDriver(driver) ? (ru ? "Логистика" : "Logistics")
            : isService ? (ru ? "На сервисе" : "Service")
            : isProduction ? (ru ? "На производстве" : "Production")
            : truck != null ? (ru ? "На логистике" : "Logistics")
            : driver.RestPhase != DriverRestPhase.None ? (ru ? "Отдыхает" : "Resting")
            : (ru ? Gend(driver, "Свободен", "Свободна") : "Idle");
    }

    private string GetWorkerDutySummaryLabel(DriverAgent driver, bool ru)
    {
        if (driver == null) return "—";

        return driver.WalkPhase == DriverRescuePhase.ToLaborExchangeForJob || driver.WalkPhase == DriverRescuePhase.AtLaborExchange
            ? (ru ? "\u041d\u0430 \u0411\u0438\u0440\u0436\u0435 \u0442\u0440\u0443\u0434\u0430" : "At Labor Exchange")
            : driver.IsInsideBuilding ? (ru ? "Работает в здании" : "Inside building")
            : IsBusDriverOnActiveRoute(driver) ? (ru ? "На автобусном маршруте" : "On bus route")
            : driver.IsOnActiveShift ? (ru ? "На смене" : "On shift")
            : driver.RestPhase != DriverRestPhase.None ? (ru ? "Отдыхает" : "Resting")
            : IsDriverBusyWalkPhase(driver) ? (ru ? "В пути" : "Commuting")
            : IsDriverIntercity(driver) ? (ru ? "Ожидает межгород" : "Waiting for intercity")
            : IsDriverBusDriver(driver) ? (ru ? "Ожидает автобусный маршрут" : "Waiting for bus route")
            : driver.AssignedPersonalHouseIndex >= 0
                ? (ru ? Gend(driver, "Свободен у дома", "Свободна у дома") : "Idle at home")
                : (ru ? Gend(driver, "Свободен в мотеле", "Свободна в мотеле") : "Idle at motel");
    }


}

