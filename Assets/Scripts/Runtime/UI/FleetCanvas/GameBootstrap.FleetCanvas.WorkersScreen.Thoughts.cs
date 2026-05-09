using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static Sprite s_workerThoughtSpeechIcon;
    private static Sprite s_workerThoughtBriefcaseIcon;
    private static Sprite s_workerThoughtClockIcon;
    private static Sprite s_workerThoughtCityIcon;
    private static Sprite s_workerThoughtCoinsIcon;
    private static Sprite s_workerThoughtHouseIcon;
    private static Sprite s_workerThoughtPeopleIcon;
    private static Sprite s_workerThoughtDotIcon;

    private readonly struct WorkerThoughtDisplayData
    {
        public WorkerThoughtDisplayData(string title, string description, Sprite icon, Color accentColor)
        {
            Title = title;
            Description = description;
            Icon = icon;
            AccentColor = accentColor;
        }

        public readonly string Title;
        public readonly string Description;
        public readonly Sprite Icon;
        public readonly Color AccentColor;
    }

    private readonly struct WorkerLifeOpinionDisplayData
    {
        public WorkerLifeOpinionDisplayData(string label, Color color)
        {
            Label = label;
            Color = color;
        }

        public readonly string Label;
        public readonly Color Color;
    }

    private void SetupWorkerThoughtsUi(RectTransform thoughtsTabRoot, Font font)
    {
        RectTransform content = CreateVerticalStack(
            "WorkerThoughtsContent",
            thoughtsTabRoot,
            new RectOffset(4, 4, 0, 0),
            14f,
            flexibleHeight: 1f);

        driversScreenUi.DetailCurrentThoughtTitleText = CreateHeaderText(
            "WorkerCurrentThoughtSectionTitle",
            content,
            font,
            "\u0421\u0435\u0439\u0447\u0430\u0441 \u0432\u0430\u0436\u043d\u043e",
            18,
            TextAnchor.MiddleLeft,
            FleetAccentColor);
        driversScreenUi.DetailCurrentThoughtTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        RectTransform currentCard = CreateResidentHudPanel(
            "WorkerCurrentThoughtCard",
            content,
            new Color(0.15f, 0.075f, 0.055f, 0.72f),
            new Color(0.94f, 0.34f, 0.24f, 0.46f));
        currentCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 160f;
        driversScreenUi.DetailCurrentThoughtBackground = currentCard.GetComponent<Image>();
        driversScreenUi.DetailCurrentThoughtOutline = currentCard.GetComponent<Outline>();

        HorizontalLayoutGroup currentLayout = currentCard.gameObject.AddComponent<HorizontalLayoutGroup>();
        currentLayout.padding = new RectOffset(22, 22, 20, 18);
        currentLayout.spacing = 18f;
        currentLayout.childAlignment = TextAnchor.MiddleLeft;
        currentLayout.childControlWidth = true;
        currentLayout.childControlHeight = true;
        currentLayout.childForceExpandWidth = false;
        currentLayout.childForceExpandHeight = true;

        driversScreenUi.DetailCurrentThoughtIcon = CreateWorkerThoughtIconImage(
            "CurrentThoughtIcon",
            currentCard,
            GetWorkerInventoryWarningIcon(),
            60f,
            new Color(0.94f, 0.34f, 0.24f, 1f));

        RectTransform currentTextColumn = CreateVerticalStack(
            "CurrentThoughtTextColumn",
            currentCard,
            new RectOffset(0, 0, 0, 0),
            7f,
            flexibleWidth: 1f);
        driversScreenUi.DetailCurrentThoughtHeadlineText = CreateHeaderText(
            "CurrentThoughtTitle",
            currentTextColumn,
            font,
            string.Empty,
            24,
            TextAnchor.MiddleLeft,
            Color.white);
        driversScreenUi.DetailCurrentThoughtHeadlineText.horizontalOverflow = HorizontalWrapMode.Wrap;
        driversScreenUi.DetailCurrentThoughtHeadlineText.verticalOverflow = VerticalWrapMode.Truncate;
        driversScreenUi.DetailCurrentThoughtHeadlineText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        driversScreenUi.DetailCurrentThoughtDescriptionText = CreateBodyText(
            "CurrentThoughtDescription",
            currentTextColumn,
            font,
            string.Empty,
            16,
            TextAnchor.UpperLeft,
            FleetSecondaryTextColor);
        driversScreenUi.DetailCurrentThoughtDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        driversScreenUi.DetailCurrentThoughtDescriptionText.verticalOverflow = VerticalWrapMode.Truncate;
        driversScreenUi.DetailCurrentThoughtDescriptionText.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;

        driversScreenUi.DetailCurrentThoughtTimeRow = CreateLayoutRow("CurrentThoughtTimeRow", currentTextColumn, 22f, 8f);
        driversScreenUi.DetailCurrentThoughtTimeRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
        driversScreenUi.DetailCurrentThoughtTimeIcon = CreateWorkerThoughtIconImage(
            "ClockIcon",
            driversScreenUi.DetailCurrentThoughtTimeRow,
            GetWorkerThoughtClockIcon(),
            16f,
            FleetMutedTextColor);
        driversScreenUi.DetailCurrentThoughtTimeText = CreateBodyText(
            "CurrentThoughtTime",
            driversScreenUi.DetailCurrentThoughtTimeRow,
            font,
            string.Empty,
            13,
            TextAnchor.MiddleLeft,
            FleetMutedTextColor);
        driversScreenUi.DetailCurrentThoughtTimeText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform lowerRow = CreateUiObject("WorkerThoughtsLowerRow", content).GetComponent<RectTransform>();
        lowerRow.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        HorizontalLayoutGroup lowerLayout = lowerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        lowerLayout.spacing = 14f;
        lowerLayout.childAlignment = TextAnchor.UpperLeft;
        lowerLayout.childControlWidth = true;
        lowerLayout.childControlHeight = true;
        lowerLayout.childForceExpandWidth = true;
        lowerLayout.childForceExpandHeight = true;

        RectTransform recentSection = CreateWorkerThoughtsSection("WorkerRecentThoughtsSection", lowerRow, font, "\u041d\u0435\u0434\u0430\u0432\u043d\u0438\u0435 \u043c\u044b\u0441\u043b\u0438", out driversScreenUi.DetailRecentThoughtsTitleText);
        recentSection.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.25f;
        driversScreenUi.DetailRecentThoughtsEmptyText = CreateBodyText(
            "WorkerRecentThoughtsEmpty",
            recentSection,
            font,
            string.Empty,
            14,
            TextAnchor.MiddleLeft,
            FleetMutedTextColor);
        driversScreenUi.DetailRecentThoughtsEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        for (int i = 0; i < WorkerThoughtHudRowCount; i++)
        {
            driversScreenUi.DetailThoughtRows.Add(CreateWorkerThoughtRow(recentSection, font, i));
        }

        RectTransform opinionsSection = CreateWorkerThoughtsSection("WorkerLifeOpinionsSection", lowerRow, font, "\u041c\u043d\u0435\u043d\u0438\u044f", out driversScreenUi.DetailLifeOpinionsTitleText);
        opinionsSection.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        driversScreenUi.DetailLifeOpinionRows.Add(CreateWorkerLifeOpinionRow("WorkerOpinionWork", opinionsSection, font, GetWorkerThoughtBriefcaseIcon()));
        driversScreenUi.DetailLifeOpinionRows.Add(CreateWorkerLifeOpinionRow("WorkerOpinionCity", opinionsSection, font, GetWorkerThoughtCityIcon()));
        driversScreenUi.DetailLifeOpinionRows.Add(CreateWorkerLifeOpinionRow("WorkerOpinionMoney", opinionsSection, font, GetWorkerThoughtCoinsIcon()));
        driversScreenUi.DetailLifeOpinionRows.Add(CreateWorkerLifeOpinionRow("WorkerOpinionHousing", opinionsSection, font, GetWorkerThoughtHouseIcon()));
        driversScreenUi.DetailLifeOpinionRows.Add(CreateWorkerLifeOpinionRow("WorkerOpinionSocial", opinionsSection, font, GetWorkerThoughtPeopleIcon()));
    }

    private RectTransform CreateWorkerThoughtsSection(string name, Transform parent, Font font, string title, out Text titleText)
    {
        RectTransform section = CreateResidentHudPanel(name, parent, ResidentHudCardColor, ResidentHudBorderColor);
        VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 16);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        titleText = CreateHeaderText("Title", section, font, title, 17, TextAnchor.MiddleLeft, FleetAccentColor);
        titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        CreateWorkerThoughtDivider("HeaderDivider", section);
        return section;
    }

    private WorkerThoughtRowUi CreateWorkerThoughtRow(RectTransform parent, Font font, int index)
    {
        WorkerThoughtRowUi row = new();
        row.Root = CreateResidentHudPanel($"WorkerThoughtRow{index + 1}", parent, new Color(0.055f, 0.11f, 0.17f, 0.88f), new Color(0.47f, 0.63f, 0.78f, 0.14f));
        row.Root.gameObject.AddComponent<LayoutElement>().preferredHeight = 82f;
        row.Background = row.Root.GetComponent<Image>();
        HorizontalLayoutGroup rowLayout = row.Root.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(12, 12, 10, 10);
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        row.TimeText = CreateBodyText("Time", row.Root, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        row.TimeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 82f;

        row.IconImage = CreateWorkerThoughtIconImage("Icon", row.Root, GetWorkerThoughtSpeechIcon(), 34f, FleetMutedTextColor);

        RectTransform textStack = CreateVerticalStack("ThoughtTextStack", row.Root, new RectOffset(), 3f, flexibleWidth: 1f);
        row.TitleText = CreateHeaderText("Title", textStack, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        row.TitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.TitleText.verticalOverflow = VerticalWrapMode.Truncate;
        row.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        row.DescriptionText = CreateBodyText("Description", textStack, font, string.Empty, 13, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        row.DescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.DescriptionText.verticalOverflow = VerticalWrapMode.Truncate;
        row.DescriptionText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        row.Root.gameObject.SetActive(false);
        return row;
    }

    private WorkerLifeOpinionRowUi CreateWorkerLifeOpinionRow(string name, Transform parent, Font font, Sprite icon)
    {
        WorkerLifeOpinionRowUi row = new();
        row.Root = CreateResidentHudPanel(name, parent, new Color(0.055f, 0.11f, 0.17f, 0.72f), new Color(0.47f, 0.63f, 0.78f, 0.10f));
        row.Root.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
        HorizontalLayoutGroup layout = row.Root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 8, 8);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        row.IconImage = CreateWorkerThoughtIconImage("Icon", row.Root, icon, 26f, FleetMutedTextColor);
        row.CategoryText = CreateBodyText("Category", row.Root, font, string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        row.CategoryText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        row.StatusDot = CreateWorkerThoughtIconImage("StatusDot", row.Root, GetWorkerThoughtDotIcon(), 10f, FleetMutedTextColor);
        row.StatusText = CreateBodyText("Status", row.Root, font, string.Empty, 14, TextAnchor.MiddleLeft, FleetMutedTextColor);
        row.StatusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 110f;
        return row;
    }

    private Image CreateWorkerThoughtIconImage(string name, Transform parent, Sprite sprite, float size, Color color)
    {
        RectTransform icon = CreateUiObject(name, parent).GetComponent<RectTransform>();
        LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = size;
        iconLayout.preferredHeight = size;
        iconLayout.minWidth = size;
        iconLayout.minHeight = size;
        Image image = icon.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private void CreateWorkerThoughtDivider(string name, Transform parent)
    {
        RectTransform divider = CreateUiObject(name, parent).GetComponent<RectTransform>();
        divider.gameObject.AddComponent<LayoutElement>().preferredHeight = 1f;
        Image image = divider.gameObject.AddComponent<Image>();
        image.color = new Color(FleetAccentColor.r, FleetAccentColor.g, FleetAccentColor.b, 0.22f);
        image.raycastTarget = false;
    }

    private void UpdateWorkerThoughtsUi(DriverAgent worker, bool ru)
    {
        if (driversScreenUi == null)
        {
            return;
        }

        if (worker != null)
        {
            EvaluateWorkerActiveThoughtRules(worker);
        }

        UpdateWorkerCurrentImportantThoughtUi(worker);
        UpdateWorkerRecentThoughtsUi(worker);
        UpdateWorkerLifeOpinionsUi(worker);
    }

    private void UpdateWorkerCurrentImportantThoughtUi(DriverAgent worker)
    {
        WorkerThought thought = GetMostImportantWorkerThought(worker);
        bool hasThought = thought != null;
        WorkerThoughtDisplayData display = hasThought
            ? GetWorkerThoughtDisplayData(thought)
            : new WorkerThoughtDisplayData(
                worker == null ? "\u0416\u0438\u0442\u0435\u043b\u044c \u043d\u0435 \u0432\u044b\u0431\u0440\u0430\u043d" : "\u0421\u0435\u0439\u0447\u0430\u0441 \u0441\u043f\u043e\u043a\u043e\u0439\u043d\u043e",
                worker == null ? "\u0412\u044b\u0431\u0435\u0440\u0438\u0442\u0435 \u0436\u0438\u0442\u0435\u043b\u044f \u0441\u043b\u0435\u0432\u0430." : "\u0423 \u0436\u0438\u0442\u0435\u043b\u044f \u043d\u0435\u0442 \u0441\u0440\u043e\u0447\u043d\u044b\u0445 \u043c\u044b\u0441\u043b\u0435\u0439.",
                GetWorkerThoughtSpeechIcon(),
                FleetMutedTextColor);

        bool warning = hasThought && thought.Tone == WorkerThoughtTone.Negative;
        if (driversScreenUi.DetailCurrentThoughtBackground != null)
        {
            driversScreenUi.DetailCurrentThoughtBackground.color = warning
                ? new Color(0.15f, 0.075f, 0.055f, 0.72f)
                : new Color(0.045f, 0.095f, 0.15f, 0.92f);
        }

        if (driversScreenUi.DetailCurrentThoughtOutline != null)
        {
            driversScreenUi.DetailCurrentThoughtOutline.effectColor = warning
                ? new Color(0.94f, 0.34f, 0.24f, 0.46f)
                : ResidentHudBorderColor;
        }

        if (driversScreenUi.DetailCurrentThoughtIcon != null)
        {
            driversScreenUi.DetailCurrentThoughtIcon.sprite = display.Icon;
            driversScreenUi.DetailCurrentThoughtIcon.color = display.AccentColor;
        }

        if (driversScreenUi.DetailCurrentThoughtHeadlineText != null)
        {
            driversScreenUi.DetailCurrentThoughtHeadlineText.text = display.Title;
        }

        if (driversScreenUi.DetailCurrentThoughtDescriptionText != null)
        {
            driversScreenUi.DetailCurrentThoughtDescriptionText.text = display.Description;
        }

        if (driversScreenUi.DetailCurrentThoughtTimeRow != null)
        {
            driversScreenUi.DetailCurrentThoughtTimeRow.gameObject.SetActive(hasThought);
        }

        if (driversScreenUi.DetailCurrentThoughtTimeText != null)
        {
            driversScreenUi.DetailCurrentThoughtTimeText.text = hasThought ? FormatWorkerThoughtTime(thought, true) : string.Empty;
        }
    }

    private void UpdateWorkerRecentThoughtsUi(DriverAgent worker)
    {
        List<WorkerThought> thoughts = GetWorkerRecentThoughtsForHud(worker);
        bool hasThoughts = thoughts.Count > 0;
        if (driversScreenUi.DetailRecentThoughtsEmptyText != null)
        {
            driversScreenUi.DetailRecentThoughtsEmptyText.gameObject.SetActive(!hasThoughts);
            driversScreenUi.DetailRecentThoughtsEmptyText.text = worker == null
                ? "\u0416\u0438\u0442\u0435\u043b\u044c \u043d\u0435 \u0432\u044b\u0431\u0440\u0430\u043d."
                : "\u041d\u0435\u0434\u0430\u0432\u043d\u0438\u0445 \u043c\u044b\u0441\u043b\u0435\u0439 \u043f\u043e\u043a\u0430 \u043d\u0435\u0442\n\u0416\u0438\u0442\u0435\u043b\u044c \u043f\u043e\u043a\u0430 \u043d\u0438 \u043d\u0430 \u0447\u0442\u043e \u043d\u0435 \u043e\u0442\u0440\u0435\u0430\u0433\u0438\u0440\u043e\u0432\u0430\u043b.";
        }

        for (int i = 0; i < driversScreenUi.DetailThoughtRows.Count; i++)
        {
            WorkerThoughtRowUi row = driversScreenUi.DetailThoughtRows[i];
            bool visible = i < thoughts.Count;
            row.Root.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            WorkerThought thought = thoughts[i];
            WorkerThoughtDisplayData display = GetWorkerThoughtDisplayData(thought);
            row.TimeText.text = FormatWorkerThoughtTime(thought, true);
            row.IconImage.sprite = display.Icon;
            row.IconImage.color = display.AccentColor;
            row.TitleText.text = display.Title;
            row.DescriptionText.text = display.Description;
            row.DescriptionText.color = FleetSecondaryTextColor;
            if (row.Background != null)
            {
                row.Background.color = thought.Active
                    ? new Color(0.060f, 0.120f, 0.185f, 0.94f)
                    : new Color(0.050f, 0.105f, 0.160f, 0.84f);
            }
        }
    }

    private void UpdateWorkerLifeOpinionsUi(DriverAgent worker)
    {
        if (worker != null)
        {
            UpdateWorkerLifeOpinionsSnapshot(worker);
        }

        WorkerLifeOpinionCategory[] categories =
        {
            WorkerLifeOpinionCategory.Work,
            WorkerLifeOpinionCategory.City,
            WorkerLifeOpinionCategory.Money,
            WorkerLifeOpinionCategory.Housing,
            WorkerLifeOpinionCategory.Social
        };

        for (int i = 0; i < driversScreenUi.DetailLifeOpinionRows.Count && i < categories.Length; i++)
        {
            WorkerLifeOpinionCategory category = categories[i];
            WorkerLifeOpinion opinion = FindWorkerLifeOpinion(worker, category);
            WorkerLifeOpinionDisplayData display = GetWorkerLifeOpinionDisplayData(opinion);
            WorkerLifeOpinionRowUi row = driversScreenUi.DetailLifeOpinionRows[i];
            row.CategoryText.text = GetWorkerLifeOpinionCategoryLabel(category);
            row.IconImage.sprite = GetWorkerLifeOpinionCategoryIcon(category);
            row.IconImage.color = category == WorkerLifeOpinionCategory.Money ? FleetAccentColor : FleetMutedTextColor;
            row.StatusDot.color = display.Color;
            row.StatusText.text = display.Label;
            row.StatusText.color = display.Color;
        }
    }

    private List<WorkerThought> GetWorkerRecentThoughtsForHud(DriverAgent worker)
    {
        List<WorkerThought> result = new();
        if (worker == null)
        {
            return result;
        }

        for (int i = 0; i < worker.Thoughts.Count && result.Count < WorkerThoughtHudRowCount; i++)
        {
            WorkerThought thought = worker.Thoughts[i];
            if (ShouldShowWorkerThoughtInHud(thought))
            {
                result.Add(thought);
            }
        }

        return result;
    }

    private static bool ShouldShowWorkerThoughtInHud(WorkerThought thought)
    {
        if (thought == null)
        {
            return false;
        }

        return !string.Equals(thought.Key, "starter_job_resolved", System.StringComparison.Ordinal) &&
               !string.Equals(thought.TemplateKey, "starter_job_resolved", System.StringComparison.Ordinal);
    }

    private WorkerThoughtDisplayData GetWorkerThoughtDisplayData(WorkerThought thought)
    {
        if (thought == null)
        {
            return new WorkerThoughtDisplayData("\u0421\u0435\u0439\u0447\u0430\u0441 \u0441\u043f\u043e\u043a\u043e\u0439\u043d\u043e", "\u0423 \u0436\u0438\u0442\u0435\u043b\u044f \u043d\u0435\u0442 \u0441\u0440\u043e\u0447\u043d\u044b\u0445 \u043c\u044b\u0441\u043b\u0435\u0439.", GetWorkerThoughtSpeechIcon(), FleetMutedTextColor);
        }

        string key = string.IsNullOrWhiteSpace(thought.Key) ? thought.TemplateKey : thought.Key;
        Color warning = new(0.94f, 0.34f, 0.24f, 1f);
        Color neutral = new(0.42f, 0.62f, 0.82f, 1f);
        Color positive = ResidentHudPositiveColor;
        Color muted = FleetMutedTextColor;

        return key switch
        {
            "no_job_warning" or "no_job_today" => new WorkerThoughtDisplayData("\u0420\u0430\u0431\u043e\u0442\u044b \u043f\u043e\u043a\u0430 \u043d\u0435\u0442", "\u0415\u0441\u043b\u0438 \u044d\u0442\u043e \u0437\u0430\u0442\u044f\u043d\u0435\u0442\u0441\u044f, \u0434\u0435\u043d\u044c\u0433\u0438 \u0431\u044b\u0441\u0442\u0440\u043e \u0440\u0430\u0441\u0442\u0430\u044e\u0442.", GetWorkerInventoryWarningIcon(), warning),
            "worker_arrived" => new WorkerThoughtDisplayData("\u042f \u0432 \u0433\u043e\u0440\u043e\u0434\u0435", "\u041f\u043e\u0440\u0430 \u043f\u043e\u043d\u044f\u0442\u044c, \u043a\u0430\u043a \u0442\u0443\u0442 \u0436\u0438\u0442\u044c.", GetWorkerThoughtSpeechIcon(), neutral),
            "starter_job_suggestion" => new WorkerThoughtDisplayData("\u041d\u0443\u0436\u043d\u043e \u043d\u0430\u0447\u0430\u0442\u044c \u0441 \u043f\u0440\u043e\u0441\u0442\u043e\u0439 \u0440\u0430\u0431\u043e\u0442\u044b", "\u041f\u043e\u0434\u043e\u0439\u0434\u0435\u0442 \u0434\u0430\u0436\u0435 \u0441\u0442\u0430\u0440\u0442\u043e\u0432\u0430\u044f \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u044f.", GetWorkerThoughtBriefcaseIcon(), muted),
            "low_money" => new WorkerThoughtDisplayData("\u0414\u0435\u043d\u0435\u0433 \u043f\u043e\u0447\u0442\u0438 \u043d\u0435\u0442", "\u041b\u044e\u0431\u0430\u044f \u043c\u0435\u043b\u043e\u0447\u044c \u0441\u0442\u0430\u043d\u043e\u0432\u0438\u0442\u0441\u044f \u043f\u0440\u043e\u0431\u043b\u0435\u043c\u043e\u0439.", GetWorkerThoughtCoinsIcon(), warning),
            "job_found" => new WorkerThoughtDisplayData("\u0420\u0430\u0431\u043e\u0442\u0430 \u043d\u0430\u0439\u0434\u0435\u043d\u0430", "\u0422\u0435\u043f\u0435\u0440\u044c \u0435\u0441\u0442\u044c \u043f\u043e\u043d\u044f\u0442\u043d\u044b\u0439 \u0441\u043b\u0435\u0434\u0443\u044e\u0449\u0438\u0439 \u0448\u0430\u0433.", GetWorkerThoughtBriefcaseIcon(), positive),
            "salary_paid" => new WorkerThoughtDisplayData("\u041f\u043e\u043b\u0443\u0447\u0438\u043b \u0437\u0430\u0440\u043f\u043b\u0430\u0442\u0443", RenderWorkerThought(thought, true), GetWorkerThoughtCoinsIcon(), positive),
            "need_meal_warning" => new WorkerThoughtDisplayData("\u041f\u043e\u0440\u0430 \u043f\u0435\u0440\u0435\u043a\u0443\u0441\u0438\u0442\u044c", "\u041b\u0443\u0447\u0448\u0435 \u043d\u0435 \u0434\u043e\u0432\u043e\u0434\u0438\u0442\u044c \u0435\u0434\u0443 \u0434\u043e \u043a\u0440\u0438\u0442\u0438\u0447\u0435\u0441\u043a\u043e\u0433\u043e \u0443\u0440\u043e\u0432\u043d\u044f.", GetNeedsMealIcon(), warning),
            "need_meal_critical" => new WorkerThoughtDisplayData("\u041a\u0440\u0438\u0442\u0438\u0447\u0435\u0441\u043a\u0438\u0439 \u0433\u043e\u043b\u043e\u0434", "\u041d\u0443\u0436\u043d\u043e \u0441\u0440\u043e\u0447\u043d\u043e \u043d\u0430\u0439\u0442\u0438 \u0435\u0434\u0443.", GetWorkerInventoryWarningIcon(), warning),
            "need_sleep_warning" => new WorkerThoughtDisplayData("\u0421\u0438\u043b \u043f\u043e\u0447\u0442\u0438 \u043d\u0435 \u043e\u0441\u0442\u0430\u043b\u043e\u0441\u044c", "\u0421\u043a\u043e\u0440\u043e \u043d\u0443\u0436\u0435\u043d \u043e\u0442\u0434\u044b\u0445.", GetNeedsSleepIcon(), warning),
            "need_sleep_critical" => new WorkerThoughtDisplayData("\u041a\u0440\u0438\u0442\u0438\u0447\u0435\u0441\u043a\u0430\u044f \u0443\u0441\u0442\u0430\u043b\u043e\u0441\u0442\u044c", "\u041d\u0443\u0436\u043d\u043e \u0441\u0440\u043e\u0447\u043d\u043e \u0432\u0435\u0440\u043d\u0443\u0442\u044c \u0431\u043e\u0434\u0440\u043e\u0441\u0442\u044c.", GetWorkerInventoryWarningIcon(), warning),
            "need_leisure_warning" => new WorkerThoughtDisplayData("\u041d\u0443\u0436\u043d\u043e \u0432\u044b\u0434\u043e\u0445\u043d\u0443\u0442\u044c", "\u0411\u0435\u0437 \u043e\u0442\u0434\u044b\u0445\u0430 \u043d\u0430\u0441\u0442\u0440\u043e\u0435\u043d\u0438\u0435 \u0431\u044b\u0441\u0442\u0440\u043e \u043f\u0440\u043e\u0441\u044f\u0434\u0435\u0442.", GetNeedsLeisureIcon(), warning),
            "need_leisure_critical" => new WorkerThoughtDisplayData("\u041e\u0442\u0434\u044b\u0445 \u043d\u0430 \u043f\u0440\u0435\u0434\u0435\u043b\u0435", "\u041d\u0443\u0436\u043d\u043e \u0441\u0440\u043e\u0447\u043d\u043e \u043e\u0442\u0432\u043b\u0435\u0447\u044c\u0441\u044f.", GetWorkerInventoryWarningIcon(), warning),
            "used_snack" => new WorkerThoughtDisplayData("\u0421\u044a\u0435\u043b Snack", "\u0410\u0432\u0442\u043e\u0440\u0430\u0441\u0445\u043e\u0434\u043d\u0438\u043a \u043f\u043e\u043c\u043e\u0433 \u043d\u0435 \u0434\u043e\u0432\u0435\u0441\u0442\u0438 \u0433\u043e\u043b\u043e\u0434 \u0434\u043e \u043a\u0440\u0438\u0437\u0438\u0441\u0430.", GetWorkerInventorySnackIcon(), positive),
            "used_coffee" => new WorkerThoughtDisplayData("\u0412\u044b\u043f\u0438\u043b Coffee", "\u0410\u0432\u0442\u043e\u0440\u0430\u0441\u0445\u043e\u0434\u043d\u0438\u043a \u0432\u0435\u0440\u043d\u0443\u043b \u043d\u0435\u043c\u043d\u043e\u0433\u043e \u0431\u043e\u0434\u0440\u043e\u0441\u0442\u0438.", GetWorkerInventoryCoffeeIcon(), positive),
            "service_missing" => new WorkerThoughtDisplayData("\u041d\u0435 \u0445\u0432\u0430\u0442\u0430\u0435\u0442 \u0441\u0435\u0440\u0432\u0438\u0441\u0430", "\u0413\u043e\u0440\u043e\u0434 \u043f\u043e\u043a\u0430 \u043d\u0435 \u0437\u0430\u043a\u0440\u044b\u0432\u0430\u0435\u0442 \u044d\u0442\u0443 \u043f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u044c.", GetWorkerInventoryWarningIcon(), warning),
            "service_unaffordable" => new WorkerThoughtDisplayData("\u041d\u0435 \u0445\u0432\u0430\u0442\u0438\u043b\u043e \u0434\u0435\u043d\u0435\u0433", "\u0416\u0438\u0442\u0435\u043b\u044c \u0445\u043e\u0442\u0435\u043b \u0432 \u0441\u0435\u0440\u0432\u0438\u0441, \u043d\u043e \u043d\u0435 \u0441\u043c\u043e\u0433 \u043e\u043f\u043b\u0430\u0442\u0438\u0442\u044c.", GetWorkerThoughtCoinsIcon(), warning),
            "need_fallback_bad" => new WorkerThoughtDisplayData("\u041f\u0440\u0438\u0448\u043b\u043e\u0441\u044c \u0432\u044b\u043a\u0440\u0443\u0447\u0438\u0432\u0430\u0442\u044c\u0441\u044f", "\u041f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u044c \u0437\u0430\u043a\u0440\u044b\u0442\u0430 \u043d\u0435 \u043b\u0443\u0447\u0448\u0438\u043c \u0441\u043f\u043e\u0441\u043e\u0431\u043e\u043c.", GetWorkerInventoryWarningIcon(), warning),
            "meal_service_good" => new WorkerThoughtDisplayData("\u041d\u043e\u0440\u043c\u0430\u043b\u044c\u043d\u043e \u043f\u043e\u0435\u043b", "\u0415\u0434\u0430 \u0431\u043e\u043b\u044c\u0448\u0435 \u043d\u0435 \u0434\u0430\u0432\u0438\u0442.", GetNeedsMealIcon(), positive),
            "sleep_service_good" or "home_sleep_good" => new WorkerThoughtDisplayData("\u0412\u044b\u0441\u043f\u0430\u043b\u0441\u044f", "\u0417\u0430\u0432\u0442\u0440\u0430 \u0431\u0443\u0434\u0435\u0442 \u043b\u0435\u0433\u0447\u0435.", GetNeedsSleepIcon(), positive),
            "leisure_service_good" => new WorkerThoughtDisplayData("\u0412\u044b\u0434\u043e\u0445\u043d\u0443\u043b", "\u041e\u0442\u0434\u044b\u0445 \u043f\u043e\u043c\u043e\u0433 \u0441\u0431\u0440\u043e\u0441\u0438\u0442\u044c \u043d\u0430\u043f\u0440\u044f\u0436\u0435\u043d\u0438\u0435.", GetNeedsLeisureIcon(), positive),
            "social_talk_good" => new WorkerThoughtDisplayData("\u041f\u0440\u0438\u044f\u0442\u043d\u044b\u0439 \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440", RenderWorkerThought(thought, true), GetWorkerThoughtPeopleIcon(), positive),
            "social_shared_place" => new WorkerThoughtDisplayData("\u041d\u043e\u0432\u043e\u0435 \u0437\u043d\u0430\u043a\u043e\u043c\u0441\u0442\u0432\u043e", RenderWorkerThought(thought, true), GetWorkerThoughtPeopleIcon(), neutral),
            "social_learned_new_topic" => new WorkerThoughtDisplayData("\u042f \u0443\u0437\u043d\u0430\u043b \u0447\u0442\u043e-\u0442\u043e \u043d\u043e\u0432\u043e\u0435", RenderWorkerThought(thought, true), GetWorkerThoughtSpeechIcon(), thought.Tone == WorkerThoughtTone.Positive ? positive : neutral),
            "family_formed" => new WorkerThoughtDisplayData("\u041f\u043e\u044f\u0432\u0438\u043b\u0430\u0441\u044c \u0441\u0435\u043c\u044c\u044f", RenderWorkerThought(thought, true), GetWorkerThoughtHouseIcon(), positive),
            "child_born" => new WorkerThoughtDisplayData("\u0420\u043e\u0434\u0438\u043b\u0441\u044f \u0440\u0435\u0431\u0435\u043d\u043e\u043a", RenderWorkerThought(thought, true), GetWorkerThoughtPeopleIcon(), positive),
            "house_bought" => new WorkerThoughtDisplayData("\u0415\u0441\u0442\u044c \u0441\u0432\u043e\u0439 \u0434\u043e\u043c", RenderWorkerThought(thought, true), GetWorkerThoughtHouseIcon(), positive),
            "bus_chosen" => new WorkerThoughtDisplayData("\u041f\u043e\u0435\u0434\u0435\u0442 \u043d\u0430 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0435", "\u041f\u0435\u0448\u043a\u043e\u043c \u0431\u044b\u043b\u043e \u0431\u044b \u0441\u043b\u0438\u0448\u043a\u043e\u043c \u0434\u043e\u043b\u0433\u043e.", GetWorkerThoughtCityIcon(), neutral),
            "bus_unavailable" => new WorkerThoughtDisplayData("\u0410\u0432\u0442\u043e\u0431\u0443\u0441 \u043d\u0435 \u0432\u044b\u0440\u0443\u0447\u0438\u043b", "\u041f\u0440\u0438\u0448\u043b\u043e\u0441\u044c \u0438\u0441\u043a\u0430\u0442\u044c \u0434\u0440\u0443\u0433\u043e\u0439 \u043f\u0443\u0442\u044c.", GetWorkerInventoryWarningIcon(), warning),
            "stable_life" => new WorkerThoughtDisplayData("\u0414\u0435\u043d\u044c \u0441\u0442\u0430\u0431\u0438\u043b\u0435\u043d", "\u0421\u0435\u0433\u043e\u0434\u043d\u044f \u0432\u0441\u0435 \u0431\u043e\u043b\u0435\u0435-\u043c\u0435\u043d\u0435\u0435 \u0440\u043e\u0432\u043d\u043e.", GetWorkerThoughtSpeechIcon(), positive),
            _ => BuildWorkerThoughtFallbackDisplay(thought)
        };
    }

    private WorkerThoughtDisplayData BuildWorkerThoughtFallbackDisplay(WorkerThought thought)
    {
        string title = thought.Kind switch
        {
            WorkerThoughtKind.Work => "\u041c\u044b\u0441\u043b\u044c \u043e \u0440\u0430\u0431\u043e\u0442\u0435",
            WorkerThoughtKind.Money => "\u041c\u044b\u0441\u043b\u044c \u043e \u0434\u0435\u043d\u044c\u0433\u0430\u0445",
            WorkerThoughtKind.Need => "\u041f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u044c \u0434\u0430\u0435\u0442 \u043e \u0441\u0435\u0431\u0435 \u0437\u043d\u0430\u0442\u044c",
            WorkerThoughtKind.Social => "\u041c\u044b\u0441\u043b\u044c \u043e \u043b\u044e\u0434\u044f\u0445",
            WorkerThoughtKind.Family => "\u0421\u0435\u043c\u0435\u0439\u043d\u043e\u0435 \u0441\u043e\u0431\u044b\u0442\u0438\u0435",
            WorkerThoughtKind.Transport => "\u041c\u044b\u0441\u043b\u044c \u043e \u0434\u043e\u0440\u043e\u0433\u0435",
            _ => "\u041c\u044b\u0441\u043b\u044c"
        };
        string description = RenderWorkerThought(thought, true);
        if (string.IsNullOrWhiteSpace(description) ||
            string.Equals(description, thought.Key, System.StringComparison.Ordinal) ||
            string.Equals(description, thought.TemplateKey, System.StringComparison.Ordinal))
        {
            description = "\u0416\u0438\u0442\u0435\u043b\u044c \u043f\u043e\u043a\u0430 \u0444\u043e\u0440\u043c\u0443\u043b\u0438\u0440\u0443\u0435\u0442 \u044d\u0442\u0443 \u043c\u044b\u0441\u043b\u044c.";
        }

        Color color = thought.Tone switch
        {
            WorkerThoughtTone.Positive => ResidentHudPositiveColor,
            WorkerThoughtTone.Negative => new Color(0.94f, 0.34f, 0.24f, 1f),
            _ => new Color(0.42f, 0.62f, 0.82f, 1f)
        };
        Sprite icon = thought.Kind switch
        {
            WorkerThoughtKind.Work => GetWorkerThoughtBriefcaseIcon(),
            WorkerThoughtKind.Money => GetWorkerThoughtCoinsIcon(),
            WorkerThoughtKind.Social => GetWorkerThoughtPeopleIcon(),
            WorkerThoughtKind.Family => GetWorkerThoughtHouseIcon(),
            WorkerThoughtKind.Need when thought.Tone == WorkerThoughtTone.Negative => GetWorkerInventoryWarningIcon(),
            _ => GetWorkerThoughtSpeechIcon()
        };
        return new WorkerThoughtDisplayData(title, description, icon, color);
    }

    private static WorkerLifeOpinionDisplayData GetWorkerLifeOpinionDisplayData(WorkerLifeOpinion opinion)
    {
        if (opinion == null || !opinion.HasScore)
        {
            return new WorkerLifeOpinionDisplayData("\u041d\u0435\u0442 \u043c\u043d\u0435\u043d\u0438\u044f", FleetMutedTextColor);
        }

        int score = opinion.Score;
        if (score <= -50) return new WorkerLifeOpinionDisplayData("\u0422\u0440\u0435\u0432\u043e\u0436\u043d\u043e", new Color(0.94f, 0.34f, 0.24f, 1f));
        if (score <= -15) return new WorkerLifeOpinionDisplayData("\u041e\u0441\u0442\u043e\u0440\u043e\u0436\u043d\u043e", new Color(0.95f, 0.62f, 0.10f, 1f));
        if (score < 15) return new WorkerLifeOpinionDisplayData("\u041d\u0435\u0439\u0442\u0440\u0430\u043b\u044c\u043d\u043e", new Color(0.38f, 0.58f, 0.82f, 1f));
        if (score < 50) return new WorkerLifeOpinionDisplayData("\u0425\u043e\u0440\u043e\u0448\u043e", ResidentHudPositiveColor);
        return new WorkerLifeOpinionDisplayData("\u041e\u0442\u043b\u0438\u0447\u043d\u043e", new Color(0.62f, 0.92f, 0.52f, 1f));
    }

    private static string GetWorkerLifeOpinionCategoryLabel(WorkerLifeOpinionCategory category)
    {
        return category switch
        {
            WorkerLifeOpinionCategory.Work => "\u0420\u0430\u0431\u043e\u0442\u0430",
            WorkerLifeOpinionCategory.City => "\u0413\u043e\u0440\u043e\u0434",
            WorkerLifeOpinionCategory.Money => "\u0414\u0435\u043d\u044c\u0433\u0438",
            WorkerLifeOpinionCategory.Housing => "\u0416\u0438\u043b\u044c\u0435",
            WorkerLifeOpinionCategory.Social => "\u041e\u0431\u0449\u0435\u043d\u0438\u0435",
            _ => "\u0422\u0435\u043c\u0430"
        };
    }

    private static Sprite GetWorkerLifeOpinionCategoryIcon(WorkerLifeOpinionCategory category)
    {
        return category switch
        {
            WorkerLifeOpinionCategory.Work => GetWorkerThoughtBriefcaseIcon(),
            WorkerLifeOpinionCategory.City => GetWorkerThoughtCityIcon(),
            WorkerLifeOpinionCategory.Money => GetWorkerThoughtCoinsIcon(),
            WorkerLifeOpinionCategory.Housing => GetWorkerThoughtHouseIcon(),
            WorkerLifeOpinionCategory.Social => GetWorkerThoughtPeopleIcon(),
            _ => GetWorkerThoughtSpeechIcon()
        };
    }

    private static string FormatWorkerThoughtTime(WorkerThought thought, bool ru)
    {
        if (thought == null)
        {
            return string.Empty;
        }

        int hour = Mathf.FloorToInt(Mathf.Repeat(thought.CreatedWorldHour, 24f));
        return $"\u0414{thought.CreatedDay} {hour:00}:00";
    }

    private static Sprite GetWorkerThoughtSpeechIcon() =>
        s_workerThoughtSpeechIcon ??= BuildWorkerInventorySprite(24, PaintWorkerThoughtSpeechIcon);

    private static Sprite GetWorkerThoughtBriefcaseIcon() =>
        s_workerThoughtBriefcaseIcon ??= BuildWorkerInventorySprite(24, PaintWorkerThoughtBriefcaseIcon);

    private static Sprite GetWorkerThoughtClockIcon() =>
        s_workerThoughtClockIcon ??= BuildWorkerInventorySprite(16, PaintWorkerThoughtClockIcon);

    private static Sprite GetWorkerThoughtCityIcon() =>
        s_workerThoughtCityIcon ??= BuildWorkerInventorySprite(24, PaintWorkerThoughtCityIcon);

    private static Sprite GetWorkerThoughtCoinsIcon() =>
        s_workerThoughtCoinsIcon ??= BuildWorkerInventorySprite(24, PaintWorkerThoughtCoinsIcon);

    private static Sprite GetWorkerThoughtHouseIcon() =>
        s_workerThoughtHouseIcon ??= BuildWorkerInventorySprite(24, PaintWorkerThoughtHouseIcon);

    private static Sprite GetWorkerThoughtPeopleIcon() =>
        s_workerThoughtPeopleIcon ??= BuildWorkerInventorySprite(24, PaintWorkerThoughtPeopleIcon);

    private static Sprite GetWorkerThoughtDotIcon() =>
        s_workerThoughtDotIcon ??= BuildWorkerInventorySprite(16, PaintWorkerThoughtDotIcon);

    private static void PaintWorkerThoughtDotIcon(Color[] pixels, int size)
    {
        Color c = Color.white;
        float center = (size - 1) * 0.5f;
        float radius = size * 0.34f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                if (dx * dx + dy * dy <= radius * radius)
                {
                    WorkerInventoryIconSet(pixels, size, x, y, c);
                }
            }
        }
    }

    private static void PaintWorkerThoughtSpeechIcon(Color[] pixels, int size)
    {
        Color fill = new(0.58f, 0.68f, 0.80f, 1f);
        Color dark = new(0.23f, 0.32f, 0.44f, 1f);
        WorkerInventoryIconRect(pixels, size, 4, 6, 16, 10, fill);
        WorkerInventoryIconRect(pixels, size, 6, 16, 5, 3, fill);
        WorkerInventoryIconRect(pixels, size, 4, 6, 16, 2, dark);
        WorkerInventoryIconRect(pixels, size, 4, 14, 16, 2, dark);
        WorkerInventoryIconSet(pixels, size, 8, 11, dark);
        WorkerInventoryIconSet(pixels, size, 12, 11, dark);
        WorkerInventoryIconSet(pixels, size, 16, 11, dark);
    }

    private static void PaintWorkerThoughtBriefcaseIcon(Color[] pixels, int size)
    {
        Color body = new(0.45f, 0.43f, 0.39f, 1f);
        Color dark = new(0.20f, 0.20f, 0.19f, 1f);
        Color light = new(0.66f, 0.62f, 0.54f, 1f);
        WorkerInventoryIconRect(pixels, size, 5, 9, 14, 10, body);
        WorkerInventoryIconRect(pixels, size, 8, 6, 8, 3, dark);
        WorkerInventoryIconRect(pixels, size, 10, 5, 4, 2, body);
        WorkerInventoryIconRect(pixels, size, 5, 9, 14, 2, light);
        WorkerInventoryIconRect(pixels, size, 11, 12, 2, 3, dark);
    }

    private static void PaintWorkerThoughtClockIcon(Color[] pixels, int size)
    {
        Color c = new(0.56f, 0.63f, 0.74f, 1f);
        float center = (size - 1) * 0.5f;
        float radius = size * 0.38f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = dx * dx + dy * dy;
                if (d <= radius * radius && d >= (radius - 1.7f) * (radius - 1.7f))
                {
                    WorkerInventoryIconSet(pixels, size, x, y, c);
                }
            }
        }

        WorkerInventoryIconRect(pixels, size, size / 2, size / 2, 1, 5, c);
        WorkerInventoryIconRect(pixels, size, size / 2, size / 2, 4, 1, c);
    }

    private static void PaintWorkerThoughtCityIcon(Color[] pixels, int size)
    {
        Color c = new(0.45f, 0.58f, 0.72f, 1f);
        Color dark = new(0.18f, 0.25f, 0.34f, 1f);
        WorkerInventoryIconRect(pixels, size, 5, 9, 5, 11, c);
        WorkerInventoryIconRect(pixels, size, 13, 5, 6, 15, c);
        for (int y = 11; y <= 17; y += 3)
        {
            WorkerInventoryIconSet(pixels, size, 7, y, dark);
            WorkerInventoryIconSet(pixels, size, 15, y, dark);
            WorkerInventoryIconSet(pixels, size, 17, y, dark);
        }
    }

    private static void PaintWorkerThoughtCoinsIcon(Color[] pixels, int size)
    {
        Color coin = new(0.96f, 0.67f, 0.08f, 1f);
        Color dark = new(0.62f, 0.38f, 0.04f, 1f);
        WorkerInventoryIconRect(pixels, size, 5, 13, 8, 4, coin);
        WorkerInventoryIconRect(pixels, size, 4, 12, 10, 2, dark);
        WorkerInventoryIconRect(pixels, size, 11, 8, 8, 4, coin);
        WorkerInventoryIconRect(pixels, size, 10, 7, 10, 2, dark);
        WorkerInventoryIconRect(pixels, size, 9, 16, 10, 4, coin);
        WorkerInventoryIconRect(pixels, size, 8, 15, 12, 2, dark);
    }

    private static void PaintWorkerThoughtHouseIcon(Color[] pixels, int size)
    {
        Color wall = new(0.55f, 0.56f, 0.56f, 1f);
        Color roof = new(0.39f, 0.42f, 0.45f, 1f);
        WorkerInventoryIconRect(pixels, size, 7, 11, 11, 9, wall);
        for (int y = 5; y <= 11; y++)
        {
            int half = y - 5;
            WorkerInventoryIconRect(pixels, size, 12 - half, y, half * 2 + 1, 1, roof);
        }
        WorkerInventoryIconRect(pixels, size, 11, 15, 4, 5, roof);
    }

    private static void PaintWorkerThoughtPeopleIcon(Color[] pixels, int size)
    {
        Color c = new(0.58f, 0.62f, 0.68f, 1f);
        WorkerInventoryIconRect(pixels, size, 6, 6, 4, 4, c);
        WorkerInventoryIconRect(pixels, size, 14, 6, 4, 4, c);
        WorkerInventoryIconRect(pixels, size, 4, 12, 8, 7, c);
        WorkerInventoryIconRect(pixels, size, 12, 12, 8, 7, c);
        WorkerInventoryIconRect(pixels, size, 9, 10, 6, 8, c * 0.82f);
    }
}
