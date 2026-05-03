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
        RectTransform contentRect = workerScroll.Content;
        driversScreenUi.WorkerListContent = contentRect;

        for (int i = 0; i < MaxDriverCardSlots; i++)
            driversScreenUi.WorkerRows.Add(CreateWorkerRow(contentRect, font, i));

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
        driversScreenUi.HireButton = CreateButton("HireDriverButton", hireSection, font, out driversScreenUi.HireButtonText, "Hire New Worker", 16, FleetPrimaryButtonColor, Color.white);
        driversScreenUi.HireButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        driversScreenUi.HireButton.onClick.AddListener(() =>
        {
            LogUiInput("Drivers Canvas: clicked Hire New Driver");
            HireNewDriver();
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

        // Profile card
        RectTransform profileCard = CreateStyledPanel("WorkerProfileCard", detailRoot.transform, FleetInsetColor);
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

        // Condition row 1: Skills | Effects
        RectTransform conditionTopRow = CreateLayoutRow("WorkerConditionTopRow", detailRoot.transform, 118f, 14f);

        RectTransform skillsCard = CreateStyledPanel("WorkerSkillsCard", conditionTopRow, FleetInsetColor);
        skillsCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup skillsCardLayout = skillsCard.gameObject.AddComponent<VerticalLayoutGroup>();
        skillsCardLayout.padding = new RectOffset(16, 16, 10, 10);
        skillsCardLayout.spacing = 4f;
        skillsCardLayout.childControlWidth = true;
        skillsCardLayout.childControlHeight = true;
        skillsCardLayout.childForceExpandWidth = true;
        skillsCardLayout.childForceExpandHeight = false;

        RectTransform skillsColumn = CreateUiObject("WorkerStatsColumn", skillsCard).GetComponent<RectTransform>();
        LayoutElement skillsColumnLayoutElement = skillsColumn.gameObject.AddComponent<LayoutElement>();
        skillsColumnLayoutElement.flexibleWidth = 1f;
        VerticalLayoutGroup skillsLayout = skillsColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        skillsLayout.spacing = 4f;
        skillsLayout.childControlWidth = true;
        skillsLayout.childControlHeight = true;
        skillsLayout.childForceExpandWidth = true;
        skillsLayout.childForceExpandHeight = false;
        driversScreenUi.DetailSkillsTitleText = CreateHeaderText("WorkerStatsTitle", skillsColumn, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailSkillsTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        driversScreenUi.DetailDrivingSkillText = CreateBodyText("WorkerDrivingSkill", skillsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailDrivingSkillText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        ConfigureWorkerSkillTooltip(driversScreenUi.DetailDrivingSkillText, WorkerSkillKind.Driving);
        driversScreenUi.DetailStaminaSkillText = CreateBodyText("WorkerStaminaSkill", skillsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailStaminaSkillText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        ConfigureWorkerSkillTooltip(driversScreenUi.DetailStaminaSkillText, WorkerSkillKind.Stamina);
        driversScreenUi.DetailProductionSkillText = CreateBodyText("WorkerProductionSkill", skillsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailProductionSkillText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        ConfigureWorkerSkillTooltip(driversScreenUi.DetailProductionSkillText, WorkerSkillKind.Production);
        driversScreenUi.DetailLogisticsSkillText = CreateBodyText("WorkerLogisticsSkill", skillsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailLogisticsSkillText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        ConfigureWorkerSkillTooltip(driversScreenUi.DetailLogisticsSkillText, WorkerSkillKind.Logistics);

        RectTransform effectsCard = CreateStyledPanel("WorkerEffectsCard", conditionTopRow, FleetInsetColor);
        effectsCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup effectsCardLayout = effectsCard.gameObject.AddComponent<VerticalLayoutGroup>();
        effectsCardLayout.padding = new RectOffset(16, 16, 10, 10);
        effectsCardLayout.spacing = 4f;
        effectsCardLayout.childControlWidth = true;
        effectsCardLayout.childControlHeight = true;
        effectsCardLayout.childForceExpandWidth = true;
        effectsCardLayout.childForceExpandHeight = false;

        RectTransform effectsColumn = CreateUiObject("WorkerEffectsColumn", effectsCard).GetComponent<RectTransform>();
        effectsColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup effectsLayout = effectsColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        effectsLayout.spacing = 4f;
        effectsLayout.childControlWidth = true;
        effectsLayout.childControlHeight = true;
        effectsLayout.childForceExpandWidth = true;
        effectsLayout.childForceExpandHeight = false;
        driversScreenUi.DetailEffectsTitleText = CreateHeaderText("WorkerEffectsTitle", effectsColumn, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailEffectsTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        driversScreenUi.DetailEffectsListRoot = CreateUiObject("WorkerEffectsList", effectsColumn).GetComponent<RectTransform>();
        LayoutElement effectsListLayout = driversScreenUi.DetailEffectsListRoot.gameObject.AddComponent<LayoutElement>();
        effectsListLayout.preferredHeight = 64f;
        effectsListLayout.flexibleHeight = 1f;
        VerticalLayoutGroup effectsListGroup = driversScreenUi.DetailEffectsListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        effectsListGroup.spacing = 1f;
        effectsListGroup.childControlWidth = true;
        effectsListGroup.childControlHeight = true;
        effectsListGroup.childForceExpandWidth = true;
        effectsListGroup.childForceExpandHeight = false;

        driversScreenUi.DetailEffectsEmptyText = CreateBodyText("WorkerEffectsEmpty", driversScreenUi.DetailEffectsListRoot, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailEffectsEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        for (int i = 0; i < WorkerEffectHudRowCount; i++)
        {
            Text effectText = CreateBodyText($"WorkerEffectRow{i + 1}", driversScreenUi.DetailEffectsListRoot, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
            effectText.gameObject.AddComponent<LayoutElement>().preferredHeight = 15f;
            effectText.gameObject.SetActive(false);
            driversScreenUi.DetailEffectTexts.Add(effectText);
        }

        // Condition row 2: Needs | Perks
        RectTransform conditionBottomRow = CreateLayoutRow("WorkerConditionBottomRow", detailRoot.transform, 104f, 14f);

        RectTransform needsCard = CreateStyledPanel("WorkerNeedsCard", conditionBottomRow, FleetInsetColor);
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

        RectTransform perksCard = CreateStyledPanel("WorkerPerksCard", conditionBottomRow, FleetInsetColor);
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
            perkText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
            perkText.gameObject.SetActive(false);
            driversScreenUi.DetailPerkTexts.Add(perkText);
        }

        // Assignment info card — compact rows, label refs stored for post-localization update
        RectTransform assignCard = CreateSectionCard(detailRoot.transform, font, string.Empty, out RectTransform assignBody, false);
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

        RectTransform educationRow = CreateLayoutRow("EducationRow", assignBody, 20f, 12f);
        driversScreenUi.DetailEducationLabel = CreateBodyText("EduL", educationRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailEducationLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailEducationText = CreateBodyText("EduV", educationRow, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailEducationText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform ageRow = CreateLayoutRow("AgeRow", assignBody, 20f, 12f);
        driversScreenUi.DetailAgeLabel = CreateBodyText("AgeL", ageRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailAgeLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailAgeText = CreateBodyText("AgeV", ageRow, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailAgeText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Stats card (salary, balance) — compact
        RectTransform statsCard = CreateSectionCard(detailRoot.transform, font, string.Empty, out RectTransform statsBody, false);
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
        salaryValLE.preferredWidth = 80f; salaryValLE.preferredHeight = 24f;
        driversScreenUi.DetailSalaryText = CreateHeaderText("SalaryVal", salaryValPanel, font, string.Empty, 13, TextAnchor.MiddleCenter, Color.white);
        driversScreenUi.DetailSalaryPlusBtn = CreateButton("SalaryPlus", salaryRow, font, out Text plusTxt, "+", 14, new Color(0.24f, 0.38f, 0.24f, 1f), Color.white);
        plusTxt.fontStyle = FontStyle.Bold;
        LayoutElement plusLE = driversScreenUi.DetailSalaryPlusBtn.gameObject.AddComponent<LayoutElement>();
        plusLE.preferredWidth = 28f; plusLE.preferredHeight = 24f;
        CreateBodyText("PerShift", salaryRow, font, "/ shift", 12, TextAnchor.MiddleLeft, FleetMutedTextColor).gameObject.AddComponent<LayoutElement>().preferredWidth = 44f;
        driversScreenUi.DetailSalaryMinusBtn.onClick.AddListener(() =>
        {
            DriverAgent d = driverAgents.Find(x => x.DriverId == selectedWorkerPanelDriverId);
            if (d == null) return;
            d.Salary = Mathf.Max(0, d.Salary - 25);
            isDriversScreenDirty = true;
        });
        driversScreenUi.DetailSalaryPlusBtn.onClick.AddListener(() =>
        {
            DriverAgent d = driverAgents.Find(x => x.DriverId == selectedWorkerPanelDriverId);
            if (d == null) return;
            d.Salary += 25;
            isDriversScreenDirty = true;
        });

        RectTransform balanceRow = CreateLayoutRow("BalanceRow", statsBody, 20f, 12f);
        driversScreenUi.DetailBalanceLabel = CreateBodyText("BL", balanceRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailBalanceLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailBalanceText = CreateHeaderText("BV", balanceRow, font, string.Empty, 14, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailBalanceText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

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
            if (d.DriverObject != null && d.DriverObject.activeSelf)
            {
                Vector3 pos = d.DriverObject.transform.position;
                cameraFocusPoint = new Vector3(pos.x, 0f, pos.z);
            }
        });

        detailRoot.SetActive(false);
        SetupWorkerSkillTooltip(windowRect, font);

        AddOverlayCloseButton(windowRect, font);
        driversScreenUi.CanvasRoot.SetActive(false);
        UpdateDriversScreenUi();
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

        row.SubText = CreateBodyText("SubText", rowObj.transform, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        row.SubText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

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

        int rowIndex = index;
        row.SelectButton.onClick.AddListener(() =>
        {
            if (rowIndex >= driverAgents.Count) return;
            DriverAgent d = driverAgents[rowIndex];
            selectedWorkerPanelDriverId = selectedWorkerPanelDriverId == d.DriverId ? 0 : d.DriverId;
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
        card.SalaryText.gameObject.AddComponent<LayoutElement>().preferredWidth = 46f;

        int ci = cardIndex;
        Button minusBtn = CreateButton($"SalaryMinus{cardIndex}", salaryRow, font, out Text minusTxt, "−", 14, new Color(0.32f, 0.26f, 0.18f, 1f), Color.white);
        minusTxt.fontStyle = FontStyle.Bold;
        LayoutElement minusLayout = minusBtn.gameObject.AddComponent<LayoutElement>();
        minusLayout.preferredWidth = 26f;
        minusLayout.preferredHeight = 26f;
        minusBtn.onClick.AddListener(() =>
        {
            if (ci >= driverAgents.Count) return;
            driverAgents[ci].Salary = Mathf.Max(0, driverAgents[ci].Salary - 25);
            LogUiInput($"Drivers Canvas: {driverAgents[ci].DriverName} salary decreased to ${driverAgents[ci].Salary}");
            isDriversScreenDirty = true;
        });

        Button plusBtn = CreateButton($"SalaryPlus{cardIndex}", salaryRow, font, out Text plusTxt, "+", 14, new Color(0.24f, 0.38f, 0.24f, 1f), Color.white);
        plusTxt.fontStyle = FontStyle.Bold;
        LayoutElement plusLayout = plusBtn.gameObject.AddComponent<LayoutElement>();
        plusLayout.preferredWidth = 26f;
        plusLayout.preferredHeight = 26f;
        plusBtn.onClick.AddListener(() =>
        {
            if (ci >= driverAgents.Count) return;
            driverAgents[ci].Salary += 25;
            LogUiInput($"Drivers Canvas: {driverAgents[ci].DriverName} salary increased to ${driverAgents[ci].Salary}");
            isDriversScreenDirty = true;
        });

        CreateBodyText("PerShift", salaryRow, font, "/shift", 12, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 42f;

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
            if (d.DriverObject != null && d.DriverObject.activeSelf)
            {
                Vector3 pos = d.DriverObject.transform.position;
                cameraFocusPoint = new Vector3(pos.x, 0f, pos.z);
            }
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
        int ci = cardIndex;

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

        minusBtn.onClick.AddListener(() =>
        {
            if (ci >= driverAgents.Count) return;
            driverAgents[ci].Salary = Mathf.Max(0, driverAgents[ci].Salary - 25);
            LogUiInput($"Drivers Canvas: {driverAgents[ci].DriverName} salary decreased to ${driverAgents[ci].Salary}");
            isDriversScreenDirty = true;
        });

        Button plusBtn = CreateButton($"DriverSalaryPlus{cardIndex}", salaryRow, font, out Text plusTxt, "+", 13, new Color(0.24f, 0.38f, 0.24f, 1f), Color.white);
        plusTxt.fontStyle = FontStyle.Bold;
        LayoutElement plusLayout = plusBtn.gameObject.AddComponent<LayoutElement>();
        plusLayout.preferredWidth = 30f;
        plusLayout.preferredHeight = 26f;
        plusBtn.onClick.AddListener(() =>
        {
            if (ci >= driverAgents.Count) return;
            driverAgents[ci].Salary += 25;
            LogUiInput($"Drivers Canvas: {driverAgents[ci].DriverName} salary increased to ${driverAgents[ci].Salary}");
            isDriversScreenDirty = true;
        });

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
        return driver.IsArrivingByBus ? (ru ? "В пути" : "Arriving")
            : IsDriverOnActiveTradeRun(driver) ? (ru ? "Торговый рейс" : "Trade Run")
            : IsDriverIntercity(driver) ? (ru ? "Межгород" : "Intercity")
            : IsDriverBusDriver(driver) ? (ru ? "Логистика" : "Logistics")
            : isProduction ? (ru ? "На производстве" : "Production")
            : truck != null ? (ru ? "На логистике" : "Logistics")
            : driver.RestPhase != DriverRestPhase.None ? (ru ? "Отдыхает" : "Resting")
            : (ru ? Gend(driver, "Свободен", "Свободна") : "Idle");
    }

    private string GetWorkerDutySummaryLabel(DriverAgent driver, bool ru)
    {
        if (driver == null) return "—";

        return driver.IsInsideBuilding ? (ru ? "Работает в здании" : "Inside building")
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

