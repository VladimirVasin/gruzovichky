using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const float WorkerKnowledgeExpiryBarWidth = 76f;

    private readonly struct WorkerKnowledgeHudEntry
    {
        public WorkerKnowledgeHudEntry(WorkerMemory memory)
        {
            Memory = memory;
            Pending = null;
        }

        public WorkerKnowledgeHudEntry(PendingWorkerKnowledge pending)
        {
            Memory = null;
            Pending = pending;
        }

        public readonly WorkerMemory Memory;
        public readonly PendingWorkerKnowledge Pending;
        public bool IsPending => Pending != null;
    }

    private void SetupWorkerKnowledgeUi(RectTransform knowledgeTabRoot, Font font)
    {
        RectTransform card = CreateResidentHudPanel("WorkerKnowledgeCard", knowledgeTabRoot, ResidentHudCardColor, ResidentHudBorderColor);
        card.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 16);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        driversScreenUi.DetailKnowledgeTitleText = CreateHeaderText(
            "WorkerKnowledgeTitle",
            card,
            font,
            string.Empty,
            18,
            TextAnchor.MiddleLeft,
            FleetAccentColor);
        driversScreenUi.DetailKnowledgeTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        CreateWorkerThoughtDivider("WorkerKnowledgeDivider", card);

        driversScreenUi.DetailKnowledgeEmptyText = CreateBodyText(
            "WorkerKnowledgeEmpty",
            card,
            font,
            string.Empty,
            15,
            TextAnchor.MiddleCenter,
            FleetMutedTextColor);
        driversScreenUi.DetailKnowledgeEmptyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        driversScreenUi.DetailKnowledgeEmptyText.verticalOverflow = VerticalWrapMode.Truncate;
        driversScreenUi.DetailKnowledgeEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;

        FleetCanvasUiFactory.ScrollPanelRefs scroll = CreateVerticalScrollList(
            "WorkerKnowledgeScroll",
            card,
            "WorkerKnowledgeRows",
            10f,
            30f,
            flexibleHeight: 1f);
        Image scrollRaycast = scroll.Root.gameObject.AddComponent<Image>();
        scrollRaycast.color = new Color(0f, 0f, 0f, 0f);
        scrollRaycast.raycastTarget = true;

        for (int i = 0; i < WorkerPersonalMemoryCap; i++)
        {
            driversScreenUi.DetailKnowledgeRows.Add(CreateWorkerKnowledgeRow(scroll.Content, font, i));
        }
    }

    private WorkerKnowledgeRowUi CreateWorkerKnowledgeRow(RectTransform parent, Font font, int index)
    {
        WorkerKnowledgeRowUi row = new();
        row.Root = CreateResidentHudPanel($"WorkerKnowledgeRow{index + 1}", parent, new Color(0.050f, 0.105f, 0.165f, 0.88f), new Color(0.47f, 0.63f, 0.78f, 0.14f));
        row.Root.gameObject.AddComponent<LayoutElement>().preferredHeight = 118f;
        row.Background = row.Root.GetComponent<Image>();
        if (row.Background != null)
        {
            row.Background.raycastTarget = false;
        }

        HorizontalLayoutGroup rowLayout = row.Root.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(14, 14, 12, 12);
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        row.TimeText = CreateBodyText("Time", row.Root, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        row.TimeText.raycastTarget = false;
        row.TimeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 82f;

        row.IconImage = CreateWorkerThoughtIconImage("Icon", row.Root, GetWorkerThoughtSpeechIcon(), 34f, FleetMutedTextColor);

        RectTransform textStack = CreateVerticalStack("KnowledgeTextStack", row.Root, new RectOffset(), 4f, flexibleWidth: 1f);
        row.TitleText = CreateHeaderText("Title", textStack, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        row.TitleText.raycastTarget = false;
        row.TitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.TitleText.verticalOverflow = VerticalWrapMode.Truncate;
        row.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 23f;

        row.DescriptionText = CreateBodyText("Description", textStack, font, string.Empty, 13, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        row.DescriptionText.raycastTarget = false;
        row.DescriptionText.supportRichText = true;
        row.DescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.DescriptionText.verticalOverflow = VerticalWrapMode.Truncate;
        row.DescriptionText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        RectTransform metaRow = CreateLayoutRow("KnowledgeMetaRow", textStack, 19f, 8f);
        row.MetaText = CreateBodyText("Meta", metaRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        row.MetaText.raycastTarget = false;
        row.MetaText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.MetaText.verticalOverflow = VerticalWrapMode.Truncate;
        row.MetaText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform expiryBar = CreateUiObject("ExpiryBar", metaRow).GetComponent<RectTransform>();
        LayoutElement expiryBarLayout = expiryBar.gameObject.AddComponent<LayoutElement>();
        expiryBarLayout.preferredWidth = WorkerKnowledgeExpiryBarWidth;
        expiryBarLayout.preferredHeight = 7f;
        expiryBarLayout.minWidth = WorkerKnowledgeExpiryBarWidth;
        expiryBarLayout.minHeight = 7f;
        Image expiryBarBackground = expiryBar.gameObject.AddComponent<Image>();
        expiryBarBackground.color = new Color(0.08f, 0.10f, 0.14f, 1f);
        expiryBarBackground.raycastTarget = false;

        row.ExpiryFillRect = CreateUiObject("ExpiryFill", expiryBar).GetComponent<RectTransform>();
        row.ExpiryFillRect.anchorMin = new Vector2(0f, 0f);
        row.ExpiryFillRect.anchorMax = new Vector2(0f, 1f);
        row.ExpiryFillRect.pivot = new Vector2(0f, 0.5f);
        row.ExpiryFillRect.anchoredPosition = Vector2.zero;
        row.ExpiryFillRect.sizeDelta = new Vector2(WorkerKnowledgeExpiryBarWidth, 0f);
        row.ExpiryFillImage = row.ExpiryFillRect.gameObject.AddComponent<Image>();
        row.ExpiryFillImage.color = FleetAccentColor;
        row.ExpiryFillImage.raycastTarget = false;

        row.ExpiryText = CreateBodyText("ExpiryText", metaRow, font, string.Empty, 12, TextAnchor.MiddleRight, FleetMutedTextColor);
        row.ExpiryText.raycastTarget = false;
        row.ExpiryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.ExpiryText.verticalOverflow = VerticalWrapMode.Truncate;
        row.ExpiryText.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;

        row.Root.gameObject.SetActive(false);
        return row;
    }

    private void UpdateWorkerKnowledgeUi(DriverAgent worker, bool ru)
    {
        if (driversScreenUi == null)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        if (worker != null && PruneExpiredWorkerMemories(worker, now))
        {
            isDriversScreenDirty = true;
        }

        List<WorkerKnowledgeHudEntry> entries = GetWorkerKnowledgeEntriesForKnowledgeHud(worker, now);
        int count = entries.Count;
        if (driversScreenUi.DetailKnowledgeTitleText != null)
        {
            driversScreenUi.DetailKnowledgeTitleText.text = ru
                ? $"\u0417\u043d\u0430\u043d\u0438\u044f ({count}/{WorkerPersonalMemoryCap})"
                : $"Knowledge ({count}/{WorkerPersonalMemoryCap})";
        }

        bool hasMemories = count > 0;
        if (driversScreenUi.DetailKnowledgeEmptyText != null)
        {
            driversScreenUi.DetailKnowledgeEmptyText.gameObject.SetActive(!hasMemories);
            driversScreenUi.DetailKnowledgeEmptyText.text = worker == null
                ? (ru ? "\u0416\u0438\u0442\u0435\u043b\u044c \u043d\u0435 \u0432\u044b\u0431\u0440\u0430\u043d." : "No resident selected.")
                : (ru ? "\u041f\u043e\u043a\u0430 \u044d\u0442\u043e\u0442 \u0436\u0438\u0442\u0435\u043b\u044c \u043d\u0438\u0447\u0435\u0433\u043e \u043e\u0441\u043e\u0431\u0435\u043d\u043d\u043e\u0433\u043e \u043d\u0435 \u0437\u0430\u043f\u043e\u043c\u043d\u0438\u043b." : "This resident has not remembered anything notable yet.");
        }

        for (int i = 0; i < driversScreenUi.DetailKnowledgeRows.Count; i++)
        {
            WorkerKnowledgeRowUi row = driversScreenUi.DetailKnowledgeRows[i];
            bool visible = i < count;
            row.Root.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            ApplyWorkerKnowledgeRow(row, entries[i], ru, now);
        }
    }

    private List<WorkerKnowledgeHudEntry> GetWorkerKnowledgeEntriesForKnowledgeHud(DriverAgent worker, float now)
    {
        List<WorkerKnowledgeHudEntry> result = new();
        if (worker == null)
        {
            return result;
        }

        for (int i = 0; i < worker.Memories.Count && result.Count < WorkerPersonalMemoryCap; i++)
        {
            WorkerMemory memory = worker.Memories[i];
            if (ShouldShowWorkerMemoryInKnowledgeHud(memory, now))
            {
                result.Add(new WorkerKnowledgeHudEntry(memory));
            }
        }

        for (int i = 0; i < worker.PendingKnowledge.Count && result.Count < WorkerPersonalMemoryCap; i++)
        {
            PendingWorkerKnowledge pending = worker.PendingKnowledge[i];
            if (IsPendingWorkerKnowledgeDisplayable(pending))
            {
                result.Add(new WorkerKnowledgeHudEntry(pending));
            }
        }

        return result;
    }

    private static bool ShouldShowWorkerMemoryInKnowledgeHud(WorkerMemory memory, float now)
    {
        return IsWorkerMemoryDisplayable(memory) &&
               !ShouldExpireWorkerMemory(memory, now);
    }

    private void ApplyWorkerKnowledgeRow(WorkerKnowledgeRowUi row, WorkerKnowledgeHudEntry entry, bool ru, float now)
    {
        if (entry.IsPending)
        {
            ApplyPendingWorkerKnowledgeRow(row, entry.Pending, ru, now);
            return;
        }

        ApplyWorkerKnowledgeRow(row, entry.Memory, ru, now);
    }

    private void ApplyPendingWorkerKnowledgeRow(WorkerKnowledgeRowUi row, PendingWorkerKnowledge pending, bool ru, float now)
    {
        if (pending == null)
        {
            return;
        }

        bool buildingKnowledge = pending.Kind == WorkerMemoryKind.BuildingExistence;
        WorkerMemory probe = CreateWorkerMemoryProbe(pending);
        string buildingName = buildingKnowledge
            ? GetWorkerKnowledgeBuildingDisplayName(probe, ru)
            : string.Empty;

        if (row.TimeText != null)
        {
            row.TimeText.text = FormatPendingWorkerKnowledgeTime(pending, ru);
        }

        if (row.IconImage != null)
        {
            row.IconImage.sprite = buildingKnowledge
                ? GetWorkerBuildingKnowledgeIcon(probe)
                : GetWorkerThoughtSpeechIcon();
            row.IconImage.color = GetWorkerKnowledgeOpinionColor(pending.OpinionTone);
        }

        if (row.TitleText != null)
        {
            row.TitleText.text = ru ? "\u0424\u043e\u0440\u043c\u0438\u0440\u0443\u0435\u0442\u0441\u044f \u043c\u043d\u0435\u043d\u0438\u0435" : "Opinion forming";
        }

        if (row.DescriptionText != null)
        {
            if (buildingKnowledge)
            {
                string building = $"<color=#74D7FF><b>{SanitizeRichTextLiteral(buildingName)}</b></color>";
                row.DescriptionText.text = ru
                    ? $"\u041e\u0431\u0434\u0443\u043c\u044b\u0432\u0430\u0435\u0442, \u0447\u0442\u043e \u0432 \u0433\u043e\u0440\u043e\u0434\u0435 \u0435\u0441\u0442\u044c {building}."
                    : $"Thinking through that {building} exists in town.";
            }
            else
            {
                DriverAgent other = GetDriverAgentById(pending.OtherWorkerId);
                string otherName = other != null && !other.HasDepartedTown
                    ? other.DriverName
                    : (ru ? "\u0437\u043d\u0430\u043a\u043e\u043c\u044b\u0439" : "contact");
                string topic = FormatCitySocialTopicRichText(GetWorkerRumorTopic(pending));
                row.DescriptionText.text = ru
                    ? $"\u041f\u0440\u043e\u043a\u0440\u0443\u0447\u0438\u0432\u0430\u0435\u0442 \u0442\u0435\u043c\u0443 \u00ab{topic}\u00bb, \u0441\u0432\u044f\u0437\u0430\u043d\u043d\u0443\u044e \u0441 {otherName}."
                    : $"Thinking through \"{topic}\" tied to {otherName}.";
            }
        }

        if (row.MetaText != null)
        {
            string stage = FormatPendingWorkerKnowledgeStage(pending.Stage, ru);
            string opinion = FormatPendingWorkerKnowledgeOpinionMeta(pending, ru);
            string attitude = pending.Kind == WorkerMemoryKind.ConversationTopic
                ? $"; {FormatWorkerKnowledgeSourceAttitudeMeta(pending.SourceAttitude, ru)}"
                : string.Empty;
            string rumorState = pending.Kind == WorkerMemoryKind.ConversationTopic
                ? $"; {FormatWorkerRumorStateMeta(pending, ru)}"
                : string.Empty;
            row.MetaText.text = $"{stage}{rumorState}{attitude}; {opinion}";
            row.MetaText.color = GetWorkerKnowledgeOpinionColor(pending.OpinionTone);
        }

        ApplyPendingWorkerKnowledgeProgressIndicator(row, pending, now, ru);

        if (row.Background != null)
        {
            float progress = GetPendingWorkerKnowledgeProgress01(pending, now);
            row.Background.color = Color.Lerp(
                new Color(0.070f, 0.075f, 0.115f, 0.90f),
                new Color(0.060f, 0.125f, 0.145f, 0.90f),
                progress);
        }
    }

    private void ApplyWorkerKnowledgeRow(WorkerKnowledgeRowUi row, WorkerMemory memory, bool ru, float now)
    {
        if (memory != null && memory.Kind == WorkerMemoryKind.BuildingExistence)
        {
            ApplyWorkerBuildingKnowledgeRow(row, memory, ru, now);
            return;
        }

        DriverAgent other = GetDriverAgentById(memory.OtherWorkerId);
        string otherName = other != null && !other.HasDepartedTown
            ? other.DriverName
            : (ru ? "\u0431\u044b\u0432\u0448\u0438\u0439 \u0437\u043d\u0430\u043a\u043e\u043c\u044b\u0439" : "former contact");

        if (row.TimeText != null)
        {
            row.TimeText.text = FormatWorkerMemoryTime(memory, ru);
        }

        if (row.IconImage != null)
        {
            row.IconImage.sprite = GetWorkerThoughtSpeechIcon();
            row.IconImage.color = GetWorkerKnowledgeOpinionColor(memory.OpinionTone);
        }

        if (row.TitleText != null)
        {
            row.TitleText.text = ru ? "\u0422\u0435\u043c\u0430 \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u0430" : "Conversation topic";
        }

        if (row.DescriptionText != null)
        {
            string topic = FormatCitySocialTopicRichText(GetWorkerRumorTopic(memory));
            string originalTopic = GetWorkerRumorOriginalTopic(memory);
            string originalSuffix = !string.IsNullOrWhiteSpace(originalTopic) &&
                                    NormalizeWorkerKnowledgeTopicKey(originalTopic) != NormalizeWorkerKnowledgeTopicKey(GetWorkerRumorTopic(memory))
                ? ru ? $" \u0418\u0441\u0445\u043e\u0434\u043d\u043e: \u00ab{FormatCitySocialTopicRichText(originalTopic)}\u00bb." : $" Original: \"{FormatCitySocialTopicRichText(originalTopic)}\"."
                : string.Empty;
            row.DescriptionText.text = ru
                ? $"\u041f\u043e\u0441\u043b\u0435 \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u0430 \u0441 {otherName} \u0437\u0430\u043f\u043e\u043c\u043d\u0438\u043b \u0441\u043b\u0443\u0445: \u00ab{topic}\u00bb.{originalSuffix}"
                : $"After talking with {otherName}, remembered rumor: \"{topic}\".{originalSuffix}";
        }

        if (row.MetaText != null)
        {
            string outcome = memory.Positive
                ? (ru ? "\u0418\u0441\u0445\u043e\u0434: \u0442\u0435\u043c\u0430 \u0441\u0440\u0430\u0431\u043e\u0442\u0430\u043b\u0430" : "Outcome: topic worked")
                : (ru ? "\u0418\u0441\u0445\u043e\u0434: \u0442\u0435\u043c\u0430 \u0431\u044b\u043b\u0430 \u043d\u0435\u043b\u043e\u0432\u043a\u043e\u0439" : "Outcome: topic felt awkward");
            string source = ru ? memory.SourceRu : memory.SourceEn;
            string iteration = FormatWorkerKnowledgeIteration(memory, ru);
            string opinion = FormatWorkerKnowledgeOpinionMeta(memory, ru);
            string attitude = FormatWorkerKnowledgeSourceAttitudeMeta(memory.SourceAttitude, ru);
            string rumorState = FormatWorkerRumorStateMeta(memory, ru);
            row.MetaText.text = string.IsNullOrWhiteSpace(source)
                ? $"{iteration}; {rumorState}; {attitude}; {outcome}; {opinion}"
                : ru ? $"{iteration}; {rumorState}; {attitude}; \u0418\u0441\u0442\u043e\u0447\u043d\u0438\u043a: {source}; {outcome}; {opinion}" : $"{iteration}; {rumorState}; {attitude}; Source: {source}; {outcome}; {opinion}";
            row.MetaText.color = GetWorkerKnowledgeOpinionColor(memory.OpinionTone);
        }

        ApplyWorkerKnowledgeExpiryIndicator(row, memory, now, ru);

        if (row.Background != null)
        {
            float freshness = GetWorkerMemoryFreshness01(memory, now);
            row.Background.color = memory.Positive
                ? Color.Lerp(new Color(0.16f, 0.095f, 0.055f, 0.90f), new Color(0.050f, 0.135f, 0.120f, 0.88f), freshness)
                : Color.Lerp(new Color(0.16f, 0.075f, 0.070f, 0.90f), new Color(0.050f, 0.105f, 0.165f, 0.88f), freshness);
        }
    }

    private void ApplyWorkerBuildingKnowledgeRow(WorkerKnowledgeRowUi row, WorkerMemory memory, bool ru, float now)
    {
        string buildingName = GetWorkerKnowledgeBuildingDisplayName(memory, ru);
        string reason = ru ? memory.SourceRu : memory.SourceEn;
        if (string.IsNullOrWhiteSpace(reason))
        {
            reason = ru ? "\u0438\u0441\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u043b \u043f\u043e\u0441\u0442\u0440\u043e\u0439\u043a\u0443" : "used the building";
        }

        if (row.TimeText != null)
        {
            row.TimeText.text = FormatWorkerMemoryTime(memory, ru);
        }

        if (row.IconImage != null)
        {
            row.IconImage.sprite = GetWorkerBuildingKnowledgeIcon(memory);
            row.IconImage.color = GetWorkerKnowledgeOpinionColor(memory.OpinionTone);
        }

        if (row.TitleText != null)
        {
            row.TitleText.text = ru ? "\u0417\u043d\u0430\u043d\u0438\u0435 \u043e \u043f\u043e\u0441\u0442\u0440\u043e\u0439\u043a\u0435" : "Building knowledge";
        }

        if (row.DescriptionText != null)
        {
            string building = $"<color=#74D7FF><b>{SanitizeRichTextLiteral(buildingName)}</b></color>";
            row.DescriptionText.text = ru
                ? $"\u041f\u043e\u043d\u044f\u043b, \u0447\u0442\u043e \u0432 \u0433\u043e\u0440\u043e\u0434\u0435 \u0435\u0441\u0442\u044c {building}."
                : $"Understood that {building} exists in town.";
        }

        if (row.MetaText != null)
        {
            string opinion = FormatWorkerKnowledgeOpinionMeta(memory, ru);
            row.MetaText.text = ru
                ? $"{FormatWorkerKnowledgeIteration(memory, ru)}; \u041f\u0440\u0438\u0447\u0438\u043d\u0430: {reason}; {opinion}"
                : $"{FormatWorkerKnowledgeIteration(memory, ru)}; Reason: {reason}; {opinion}";
            row.MetaText.color = GetWorkerKnowledgeOpinionColor(memory.OpinionTone);
        }

        ApplyWorkerKnowledgeExpiryIndicator(row, memory, now, ru);

        if (row.Background != null)
        {
            float freshness = GetWorkerMemoryFreshness01(memory, now);
            row.Background.color = Color.Lerp(
                new Color(0.085f, 0.075f, 0.145f, 0.92f),
                new Color(0.045f, 0.115f, 0.155f, 0.88f),
                freshness);
        }
    }

    private static Sprite GetWorkerBuildingKnowledgeIcon(WorkerMemory memory)
    {
        return memory?.BuildingType switch
        {
            LocationType.PersonalHouse => GetWorkerThoughtHouseIcon(),
            LocationType.LaborExchange => GetWorkerThoughtBriefcaseIcon(),
            LocationType.Warehouse or LocationType.Sawmill or LocationType.Forest or LocationType.FurnitureFactory or LocationType.Docks => GetWorkerThoughtBriefcaseIcon(),
            _ => GetWorkerThoughtCityIcon()
        };
    }

    private void ApplyPendingWorkerKnowledgeProgressIndicator(WorkerKnowledgeRowUi row, PendingWorkerKnowledge pending, float now, bool ru)
    {
        float progress = GetPendingWorkerKnowledgeProgress01(pending, now);
        Color progressColor = GetWorkerKnowledgeOpinionColor(pending?.OpinionTone ?? WorkerKnowledgeOpinionTone.Neutral);

        if (row.ExpiryFillRect != null)
        {
            row.ExpiryFillRect.sizeDelta = new Vector2(WorkerKnowledgeExpiryBarWidth * progress, 0f);
        }

        if (row.ExpiryFillImage != null)
        {
            row.ExpiryFillImage.color = progressColor;
        }

        if (row.ExpiryText != null)
        {
            row.ExpiryText.text = ru
                ? $"{Mathf.RoundToInt(progress * 100f)}%"
                : $"{Mathf.RoundToInt(progress * 100f)}%";
            row.ExpiryText.color = progressColor;
        }
    }

    private static float GetPendingWorkerKnowledgeProgress01(PendingWorkerKnowledge pending, float now)
    {
        if (pending == null)
        {
            return 0f;
        }

        float stageTotal = Mathf.Max(0.01f, pending.NextStageWorldHour - pending.StageStartedWorldHour);
        float stageProgress = Mathf.Clamp01((now - pending.StageStartedWorldHour) / stageTotal);
        int stageIndex = pending.Stage switch
        {
            WorkerKnowledgeFormationStage.Comparing => 1,
            WorkerKnowledgeFormationStage.Judging => 2,
            _ => 0
        };
        return Mathf.Clamp01((stageIndex + stageProgress) / 3f);
    }

    private static string FormatPendingWorkerKnowledgeStage(WorkerKnowledgeFormationStage stage, bool ru)
    {
        return stage switch
        {
            WorkerKnowledgeFormationStage.Comparing => ru ? "\u0421\u0432\u0435\u0440\u044f\u0435\u0442 \u0441 \u043e\u043f\u044b\u0442\u043e\u043c" : "Comparing with experience",
            WorkerKnowledgeFormationStage.Judging => ru ? "\u0421\u043e\u0431\u0438\u0440\u0430\u0435\u0442 \u043c\u043d\u0435\u043d\u0438\u0435" : "Judging the value",
            _ => ru ? "\u0423\u0441\u043b\u044b\u0448\u0430\u043b \u0438 \u0434\u0443\u043c\u0430\u0435\u0442" : "Heard and thinking"
        };
    }

    private static string FormatWorkerKnowledgeOpinionMeta(WorkerMemory memory, bool ru)
    {
        if (memory == null)
        {
            return string.Empty;
        }

        return FormatWorkerKnowledgeOpinionMeta(
            memory.OpinionTone,
            memory.OpinionConfidence,
            ru ? memory.OpinionReasonRu : memory.OpinionReasonEn,
            ru);
    }

    private static string FormatPendingWorkerKnowledgeOpinionMeta(PendingWorkerKnowledge pending, bool ru)
    {
        if (pending == null)
        {
            return string.Empty;
        }

        return FormatWorkerKnowledgeOpinionMeta(
            pending.OpinionTone,
            pending.OpinionConfidence,
            ru ? pending.OpinionReasonRu : pending.OpinionReasonEn,
            ru);
    }

    private static string FormatWorkerKnowledgeOpinionMeta(
        WorkerKnowledgeOpinionTone tone,
        int confidence,
        string reason,
        bool ru)
    {
        string label = FormatWorkerKnowledgeOpinionLabel(tone, ru);
        int clampedConfidence = Mathf.Clamp(confidence, 0, 100);
        string prefix = ru ? "\u041c\u043d\u0435\u043d\u0438\u0435" : "Opinion";
        if (string.IsNullOrWhiteSpace(reason))
        {
            return clampedConfidence > 0
                ? $"{prefix}: {label}, {clampedConfidence}%"
                : $"{prefix}: {label}";
        }

        return clampedConfidence > 0
            ? $"{prefix}: {label}, {clampedConfidence}% ({reason})"
            : $"{prefix}: {label} ({reason})";
    }

    private static string FormatWorkerKnowledgeOpinionLabel(WorkerKnowledgeOpinionTone tone, bool ru)
    {
        return tone switch
        {
            WorkerKnowledgeOpinionTone.Positive => ru ? "\u043f\u043e\u043b\u0435\u0437\u043d\u043e" : "useful",
            WorkerKnowledgeOpinionTone.Negative => ru ? "\u0441\u043e\u043c\u043d\u0438\u0442\u0435\u043b\u044c\u043d\u043e" : "doubtful",
            _ => ru ? "\u043d\u0435\u0439\u0442\u0440\u0430\u043b\u044c\u043d\u043e" : "neutral"
        };
    }

    private static Color GetWorkerKnowledgeOpinionColor(WorkerKnowledgeOpinionTone tone)
    {
        return tone switch
        {
            WorkerKnowledgeOpinionTone.Positive => ResidentHudPositiveColor,
            WorkerKnowledgeOpinionTone.Negative => new Color(0.95f, 0.42f, 0.30f, 1f),
            _ => new Color(0.50f, 0.72f, 0.92f, 1f)
        };
    }

    private void ApplyWorkerKnowledgeExpiryIndicator(WorkerKnowledgeRowUi row, WorkerMemory memory, float now, bool ru)
    {
        float freshness = GetWorkerMemoryFreshness01(memory, now);
        float remainingHours = GetWorkerMemoryRemainingHours(memory, now);
        Color expiryColor = Color.Lerp(new Color(0.95f, 0.34f, 0.24f, 1f), new Color(1f, 0.82f, 0.29f, 1f), freshness);

        if (row.ExpiryFillRect != null)
        {
            row.ExpiryFillRect.sizeDelta = new Vector2(WorkerKnowledgeExpiryBarWidth * freshness, 0f);
        }

        if (row.ExpiryFillImage != null)
        {
            row.ExpiryFillImage.color = expiryColor;
        }

        if (row.ExpiryText != null)
        {
            row.ExpiryText.text = FormatWorkerMemoryRemainingTime(remainingHours, ru);
            row.ExpiryText.color = freshness <= 0.25f ? expiryColor : FleetMutedTextColor;
        }
    }

    private static float GetWorkerMemoryFreshness01(WorkerMemory memory, float now)
    {
        float remainingHours = GetWorkerMemoryRemainingHours(memory, now);
        return Mathf.Clamp01(remainingHours / WorkerPersonalMemoryLifetimeHours);
    }

    private static float GetWorkerMemoryRemainingHours(WorkerMemory memory, float now)
    {
        float expiresWorldHour = GetWorkerMemoryExpiresWorldHour(memory);
        return expiresWorldHour > 0f ? Mathf.Max(0f, expiresWorldHour - now) : 0f;
    }

    private static string FormatWorkerMemoryRemainingTime(float remainingHours, bool ru)
    {
        int hours = Mathf.CeilToInt(Mathf.Max(0f, remainingHours));
        if (hours <= 0)
        {
            return ru ? "\u0441\u0433\u043e\u0440\u0430\u0435\u0442" : "fading";
        }

        return ru ? $"{hours}\u0447 \u043e\u0441\u0442." : $"{hours}h left";
    }

    private static string FormatWorkerMemoryTime(WorkerMemory memory, bool ru)
    {
        if (memory == null)
        {
            return string.Empty;
        }

        int hour = Mathf.FloorToInt(Mathf.Repeat(memory.CreatedWorldHour, 24f));
        return ru ? $"\u0414{memory.CreatedDay} {hour:00}:00" : $"D{memory.CreatedDay} {hour:00}:00";
    }

    private static string FormatPendingWorkerKnowledgeTime(PendingWorkerKnowledge pending, bool ru)
    {
        if (pending == null)
        {
            return string.Empty;
        }

        int day = pending.StartedDay > 0 ? pending.StartedDay : Mathf.FloorToInt(pending.StartedWorldHour / 24f) + 1;
        int hour = Mathf.FloorToInt(Mathf.Repeat(pending.StartedWorldHour, 24f));
        return ru ? $"\u0414{day} {hour:00}:00" : $"D{day} {hour:00}:00";
    }

    private static string FormatWorkerKnowledgeIteration(WorkerMemory memory, bool ru)
    {
        int iteration = GetWorkerKnowledgeIteration(memory);
        return ru ? $"\u0418\u0442\u0435\u0440\u0430\u0446\u0438\u044f {iteration}" : $"Iteration {iteration}";
    }
}
