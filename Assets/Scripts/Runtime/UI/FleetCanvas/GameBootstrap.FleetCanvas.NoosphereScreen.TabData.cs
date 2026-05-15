using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private void RebuildNoosphereOverviewTab(
        bool ru,
        float now,
        int activeKnowledgeCount,
        int receivedCount,
        int burnedCount,
        int canonizedCount,
        int socialSignalCount)
    {
        int pinnedCount = GetCityKnowledgeCanonMemoryCount();
        List<NoosphereKnowledgeStateRow> knowledgeRows = BuildNoosphereKnowledgeStateRows(now);
        int formingCount = CountNoosphereKnowledgeStates(knowledgeRows, NoosphereKnowledgeStateKind.Forming);
        int contestedCount = CountNoosphereKnowledgeStates(knowledgeRows, NoosphereKnowledgeStateKind.Contested);

        noosphereScreenUi.OverviewLeadText.text = ru
            ? "Городская память разложена по слоям: текущее знание, сигналы, архив событий и схема."
            : "City memory is split into current knowledge, signals, event history, and map.";

        ApplyNoosphereTextRow(
            noosphereScreenUi.OverviewRows[0],
            ru ? "ПУЛЬС" : "PULSE",
            new Color(0.54f, 0.86f, 1f, 1f),
            ru ? "Состояние памяти" : "Memory state",
            ru
                ? $"Активных знаний {activeKnowledgeCount}; событий в архиве {noosphereKnowledgeLog.Count}; закреплённых сейчас {pinnedCount}."
                : $"Active knowledge {activeKnowledgeCount}; archived events {noosphereKnowledgeLog.Count}; currently pinned {pinnedCount}.",
            ru ? $"Получено {receivedCount}; закреплено событий {canonizedCount}; сгорело {burnedCount}." : $"Received {receivedCount}; canonized events {canonizedCount}; burned {burnedCount}.",
            new Color(0.050f, 0.092f, 0.128f, 0.94f));

        ApplyNoosphereTextRow(
            noosphereScreenUi.OverviewRows[1],
            ru ? "ЗНАН" : "KNOW",
            new Color(0.60f, 0.78f, 1f, 1f),
            ru ? "Городское знание" : "City knowledge",
            ru
                ? $"Закреплено {pinnedCount}; формируется {formingCount}; спорных тем {contestedCount}."
                : $"Pinned {pinnedCount}; forming {formingCount}; contested {contestedCount}.",
            ru ? "Открой вкладку «Знания», чтобы видеть состояние, а не поток событий." : "Open Knowledge to inspect state instead of the event stream.",
            new Color(0.070f, 0.085f, 0.132f, 0.94f));

        ApplyNoosphereTextRow(
            noosphereScreenUi.OverviewRows[2],
            ru ? "СИГН" : "SIG",
            new Color(0.58f, 0.94f, 0.54f, 1f),
            ru ? "Социальные сигналы" : "Social signals",
            BuildNoosphereSignalHeadline(ru),
            ru ? $"Публичных сигналов: {socialSignalCount}." : $"Public signals: {socialSignalCount}.",
            new Color(0.055f, 0.112f, 0.096f, 0.94f));

        ApplyNoosphereTextRow(
            noosphereScreenUi.OverviewRows[3],
            ru ? "ОПЫТ" : "EXP",
            new Color(1f, 0.76f, 0.34f, 1f),
            ru ? "Опыт дня" : "Daily experience",
            FormatLatestCityDailyExperienceNoosphereSummary(ru),
            ru ? "Это общий фон города, отдельно от конкретных знаний и слухов." : "This is the city mood layer, separate from concrete knowledge and rumors.",
            new Color(0.118f, 0.092f, 0.050f, 0.94f));
    }

    private static int CountNoosphereKnowledgeStates(List<NoosphereKnowledgeStateRow> rows, NoosphereKnowledgeStateKind kind)
    {
        int count = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i]?.Kind == kind)
            {
                count++;
            }
        }

        return count;
    }

    private string BuildNoosphereSignalHeadline(bool ru)
    {
        int latestDay = GetLatestSocialSignalDay();
        if (latestDay <= 0)
        {
            return ru ? "Сигналы ещё не собрались." : "No signals have gathered yet.";
        }

        List<SocialSignalTopicAggregate> aggregates = BuildSocialSignalTopicAggregates(latestDay, publicOnly: true);
        if (aggregates.Count == 0)
        {
            return ru ? "Сигналы дня тихие." : "Today signals are quiet.";
        }

        SocialSignalTopicAggregate top = aggregates[0];
        string label = ru ? top.LabelRu : top.LabelEn;
        return ru
            ? $"Главная тема Д{latestDay}: {label}; тон {top.Score:+#;-#;0}; сила {top.Strength}; сигналов {top.Count}."
            : $"Top D{latestDay} topic: {label}; tone {top.Score:+#;-#;0}; strength {top.Strength}; signals {top.Count}.";
    }

    private void RebuildNoosphereKnowledgeTab(bool ru, float now)
    {
        List<NoosphereKnowledgeStateRow> rows = BuildNoosphereKnowledgeStateRows(now);
        noosphereScreenUi.KnowledgeTitleText.text = ru ? "Городское знание" : "City knowledge";
        noosphereScreenUi.KnowledgeEmptyText.gameObject.SetActive(rows.Count == 0);
        noosphereScreenUi.KnowledgeEmptyText.text = ru
            ? "Пока нет повторяющихся знаний. Сначала жители должны узнать и принять одну и ту же тему."
            : "No repeated knowledge yet. Residents need to learn and accept the same topic first.";

        int visibleCount = Mathf.Min(rows.Count, noosphereScreenUi.KnowledgeRows.Count);
        for (int i = 0; i < visibleCount; i++)
        {
            ApplyNoosphereKnowledgeStateRow(noosphereScreenUi.KnowledgeRows[i], rows[i], ru);
        }

        HideNoosphereTextRows(noosphereScreenUi.KnowledgeRows, visibleCount);
    }

    private List<NoosphereKnowledgeStateRow> BuildNoosphereKnowledgeStateRows(float now)
    {
        List<NoosphereKnowledgeStateRow> rows = new();
        int activeResidents = CountActiveCityKnowledgeResidents();
        int required = GetCityKnowledgeCanonRequiredAdopters(activeResidents);

        for (int i = 0; i < cityKnowledgeCanon.Count; i++)
        {
            CityKnowledgeCanonEntry entry = cityKnowledgeCanon[i];
            WorkerMemory memory = CreateWorkerMemoryFromCityKnowledgeCanon(entry);
            if (!IsWorkerMemoryDisplayable(memory))
            {
                continue;
            }

            rows.Add(new NoosphereKnowledgeStateRow
            {
                Kind = NoosphereKnowledgeStateKind.Canonized,
                Memory = memory,
                CanonEntry = entry,
                Count = entry.AdoptionCount,
                Required = entry.AdoptionRequired,
                PositiveCount = entry.Positive ? entry.AdoptionCount : 0,
                NegativeCount = entry.Positive ? 0 : entry.AdoptionCount,
                Score = entry.OpinionScore,
                Confidence = entry.OpinionConfidence,
                LastDay = entry.CanonizedDay,
                LastWorldHour = entry.CanonizedWorldHour
            });
        }

        Dictionary<string, NoosphereKnowledgeStateRow> forming = new();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (!IsActiveCityKnowledgeResident(worker))
            {
                continue;
            }

            HashSet<string> seenWorkerKeys = new();
            for (int j = 0; j < worker.Memories.Count; j++)
            {
                WorkerMemory memory = worker.Memories[j];
                if (!IsWorkerMemoryDisplayable(memory) ||
                    ShouldExpireWorkerMemory(memory, now) ||
                    HasCityKnowledgeCanonEquivalent(memory))
                {
                    continue;
                }

                string key = BuildNoosphereKnowledgeStateKey(memory);
                if (string.IsNullOrWhiteSpace(key) || !seenWorkerKeys.Add(key))
                {
                    continue;
                }

                if (!forming.TryGetValue(key, out NoosphereKnowledgeStateRow state))
                {
                    state = new NoosphereKnowledgeStateRow
                    {
                        Kind = NoosphereKnowledgeStateKind.Forming,
                        Memory = memory,
                        Required = required,
                        LastDay = memory.CreatedDay,
                        LastWorldHour = memory.CreatedWorldHour
                    };
                    forming.Add(key, state);
                }

                state.Count++;
                if (memory.Positive)
                {
                    state.PositiveCount++;
                }
                else
                {
                    state.NegativeCount++;
                }

                state.ScoreTotal += memory.OpinionScore;
                state.ConfidenceTotal += memory.OpinionConfidence;
                if (memory.CreatedWorldHour > state.LastWorldHour)
                {
                    state.LastDay = memory.CreatedDay;
                    state.LastWorldHour = memory.CreatedWorldHour;
                    state.Memory = memory;
                }
            }
        }

        foreach (NoosphereKnowledgeStateRow state in forming.Values)
        {
            state.Score = Mathf.RoundToInt(state.ScoreTotal / (float)Mathf.Max(1, state.Count));
            state.Confidence = Mathf.RoundToInt(state.ConfidenceTotal / (float)Mathf.Max(1, state.Count));
            if (state.PositiveCount > 0 && state.NegativeCount > 0)
            {
                state.Kind = NoosphereKnowledgeStateKind.Contested;
            }

            rows.Add(state);
        }

        rows.Sort(CompareNoosphereKnowledgeStateRows);
        return rows;
    }

    private static int CompareNoosphereKnowledgeStateRows(NoosphereKnowledgeStateRow first, NoosphereKnowledgeStateRow second)
    {
        int kind = GetNoosphereKnowledgeStateSortOrder(first.Kind).CompareTo(GetNoosphereKnowledgeStateSortOrder(second.Kind));
        if (kind != 0)
        {
            return kind;
        }

        int count = second.Count.CompareTo(first.Count);
        if (count != 0)
        {
            return count;
        }

        return second.LastWorldHour.CompareTo(first.LastWorldHour);
    }

    private static int GetNoosphereKnowledgeStateSortOrder(NoosphereKnowledgeStateKind kind)
    {
        return kind switch
        {
            NoosphereKnowledgeStateKind.Canonized => 0,
            NoosphereKnowledgeStateKind.Contested => 1,
            _ => 2
        };
    }

    private static string BuildNoosphereKnowledgeStateKey(WorkerMemory memory)
    {
        if (memory == null)
        {
            return string.Empty;
        }

        return memory.Kind switch
        {
            WorkerMemoryKind.BuildingExistence => $"building:{memory.BuildingType}:{memory.BuildingInstanceId}",
            WorkerMemoryKind.ConversationTopic => memory.RumorRootId > 0
                ? $"topic-root:{memory.RumorRootId}"
                : $"topic:{NormalizeWorkerKnowledgeTopicKey(GetWorkerRumorOriginalTopic(memory))}",
            _ => string.Empty
        };
    }

    private void ApplyNoosphereKnowledgeStateRow(NoosphereTextRowUi ui, NoosphereKnowledgeStateRow row, bool ru)
    {
        Color accent = GetNoosphereKnowledgeStateColor(row.Kind);
        ApplyNoosphereTextRow(
            ui,
            GetNoosphereKnowledgeStateBadge(row.Kind, ru),
            accent,
            BuildNoosphereKnowledgeStateTitle(row, ru),
            BuildNoosphereKnowledgeStateBody(row, ru),
            BuildNoosphereKnowledgeStateMeta(row, ru),
            GetNoosphereKnowledgeStateBackground(row.Kind));
    }

    private string BuildNoosphereKnowledgeStateTitle(NoosphereKnowledgeStateRow row, bool ru)
    {
        WorkerMemory memory = row.Memory;
        if (memory?.Kind == WorkerMemoryKind.BuildingExistence)
        {
            string building = GetWorkerKnowledgeBuildingDisplayName(memory, ru);
            return row.Kind == NoosphereKnowledgeStateKind.Canonized
                ? ru ? $"Закреплено: {building}" : $"Pinned: {building}"
                : ru ? $"Формируется: {building}" : $"Forming: {building}";
        }

        string topic = GetWorkerRumorTopic(memory);
        return row.Kind switch
        {
            NoosphereKnowledgeStateKind.Canonized => ru ? $"Закреплённый слух: «{topic}»" : $"Pinned rumor: \"{topic}\"",
            NoosphereKnowledgeStateKind.Contested => ru ? $"Спорная тема: «{topic}»" : $"Contested topic: \"{topic}\"",
            _ => ru ? $"Формируется тема: «{topic}»" : $"Forming topic: \"{topic}\""
        };
    }

    private static string BuildNoosphereKnowledgeStateBody(NoosphereKnowledgeStateRow row, bool ru)
    {
        int required = Mathf.Max(1, row.Required);
        string type = row.Memory?.Kind == WorkerMemoryKind.BuildingExistence
            ? (ru ? "факт о месте" : "place fact")
            : (ru ? "слух / тема" : "rumor / topic");
        if (row.Kind == NoosphereKnowledgeStateKind.Canonized)
        {
            return ru
                ? $"Приняли {row.Count}/{required}; тип: {type}; уверенность {row.Confidence}%."
                : $"Accepted {row.Count}/{required}; type: {type}; confidence {row.Confidence}%.";
        }

        if (row.Kind == NoosphereKnowledgeStateKind.Contested)
        {
            return ru
                ? $"Знают {row.Count}/{required}; раскол +{row.PositiveCount}/-{row.NegativeCount}; уверенность {row.Confidence}%."
                : $"Known by {row.Count}/{required}; split +{row.PositiveCount}/-{row.NegativeCount}; confidence {row.Confidence}%.";
        }

        return ru
            ? $"Знают {row.Count}/{required}; тон {row.Score:+#;-#;0}; уверенность {row.Confidence}%."
            : $"Known by {row.Count}/{required}; tone {row.Score:+#;-#;0}; confidence {row.Confidence}%.";
    }

    private static string BuildNoosphereKnowledgeStateMeta(NoosphereKnowledgeStateRow row, bool ru)
    {
        string time = FormatNoosphereKnowledgeStateTime(row.LastDay, row.LastWorldHour, ru);
        return row.Kind == NoosphereKnowledgeStateKind.Canonized
            ? (ru ? $"Статус: навсегда; закреплено {time}" : $"Status: forever; pinned {time}")
            : (ru ? $"Последнее подтверждение: {time}" : $"Last confirmation: {time}");
    }

    private static string FormatNoosphereKnowledgeStateTime(int day, float worldHour, bool ru)
    {
        int displayDay = day > 0 ? day : Mathf.FloorToInt(worldHour / 24f) + 1;
        int hour = Mathf.FloorToInt(Mathf.Repeat(worldHour, 24f));
        return ru ? $"Д{displayDay} {hour:00}:00" : $"D{displayDay} {hour:00}:00";
    }

    private static string GetNoosphereKnowledgeStateBadge(NoosphereKnowledgeStateKind kind, bool ru)
    {
        return kind switch
        {
            NoosphereKnowledgeStateKind.Canonized => ru ? "ВЕЧ" : "PERM",
            NoosphereKnowledgeStateKind.Contested => ru ? "СПОР" : "SPLIT",
            _ => ru ? "РОСТ" : "FORM"
        };
    }

    private static Color GetNoosphereKnowledgeStateColor(NoosphereKnowledgeStateKind kind)
    {
        return kind switch
        {
            NoosphereKnowledgeStateKind.Canonized => new Color(0.60f, 0.78f, 1f, 1f),
            NoosphereKnowledgeStateKind.Contested => new Color(1f, 0.62f, 0.28f, 1f),
            _ => new Color(0.58f, 0.94f, 0.54f, 1f)
        };
    }

    private static Color GetNoosphereKnowledgeStateBackground(NoosphereKnowledgeStateKind kind)
    {
        return kind switch
        {
            NoosphereKnowledgeStateKind.Canonized => new Color(0.075f, 0.090f, 0.145f, 0.94f),
            NoosphereKnowledgeStateKind.Contested => new Color(0.128f, 0.092f, 0.050f, 0.94f),
            _ => new Color(0.050f, 0.112f, 0.096f, 0.94f)
        };
    }

    private void RebuildNoosphereSignalsTab(bool ru, int socialSignalCount)
    {
        int latestDay = GetLatestSocialSignalDay();
        List<SocialSignalTopicAggregate> aggregates = latestDay > 0
            ? BuildSocialSignalTopicAggregates(latestDay, publicOnly: true)
            : new List<SocialSignalTopicAggregate>();

        noosphereScreenUi.SignalsTitleText.text = latestDay > 0
            ? ru ? $"Сигналы дня {latestDay}" : $"Day {latestDay} signals"
            : ru ? "Сигналы дня" : "Daily signals";
        noosphereScreenUi.SignalsEmptyText.gameObject.SetActive(aggregates.Count == 0);
        noosphereScreenUi.SignalsEmptyText.text = ru
            ? "Пока нет публичных социальных сигналов."
            : "No public social signals yet.";

        int visibleCount = Mathf.Min(aggregates.Count, noosphereScreenUi.SignalsRows.Count);
        for (int i = 0; i < visibleCount; i++)
        {
            ApplyNoosphereSignalRow(noosphereScreenUi.SignalsRows[i], aggregates[i], socialSignalCount, ru);
        }

        HideNoosphereTextRows(noosphereScreenUi.SignalsRows, visibleCount);
    }

    private void ApplyNoosphereSignalRow(NoosphereTextRowUi row, SocialSignalTopicAggregate aggregate, int totalSignals, bool ru)
    {
        int averageConfidence = Mathf.Clamp(aggregate.ConfidenceTotal / Mathf.Max(1, aggregate.Count), 0, 100);
        bool positive = aggregate.Score >= 0;
        Color accent = positive ? new Color(0.58f, 0.94f, 0.54f, 1f) : new Color(1f, 0.34f, 0.26f, 1f);
        string label = ru ? aggregate.LabelRu : aggregate.LabelEn;
        ApplyNoosphereTextRow(
            row,
            positive ? (ru ? "ПЛЮС" : "POS") : (ru ? "МИН" : "NEG"),
            accent,
            label,
            ru
                ? $"Тон {aggregate.Score:+#;-#;0}; сила {aggregate.Strength}; сигналов по теме {aggregate.Count}."
                : $"Tone {aggregate.Score:+#;-#;0}; strength {aggregate.Strength}; topic signals {aggregate.Count}.",
            ru
                ? $"{GetSocialSignalCategoryLabel(aggregate.Category, true)}; уверенность {averageConfidence}%; всего сигналов {totalSignals}."
                : $"{GetSocialSignalCategoryLabel(aggregate.Category, false)}; confidence {averageConfidence}%; total signals {totalSignals}.",
            positive ? new Color(0.050f, 0.112f, 0.096f, 0.94f) : new Color(0.128f, 0.065f, 0.060f, 0.94f));
    }

    private void RebuildNoosphereJournalTab(bool ru, int socialSignalCount)
    {
        noosphereScreenUi.JournalTitleText.text = ru ? "Журнал знаний" : "Knowledge log";
        noosphereScreenUi.EmptyText.gameObject.SetActive(noosphereKnowledgeLog.Count == 0 && socialSignalCount == 0);
        noosphereScreenUi.EmptyText.text = ru
            ? "Пока нет событий. Ноосфера молчит, как архив до первой папки."
            : "No knowledge events yet.";

        for (int i = 0; i < noosphereScreenUi.Rows.Count; i++)
        {
            NoosphereLogRowUi row = noosphereScreenUi.Rows[i];
            bool visible = i < noosphereKnowledgeLog.Count;
            row.Root.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            ApplyNoosphereLogRow(row, noosphereKnowledgeLog[i], ru);
        }
    }
}
