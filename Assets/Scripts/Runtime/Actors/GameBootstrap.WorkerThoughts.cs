using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerThoughtMemoryCap = 20;
    private const int WorkerOpinionMemoryCap = 12;
    private const int WorkerThoughtHudRowCount = 4;
    private const int WorkerOpinionHudChipCount = 4;
    private const float WorkerThoughtDefaultCooldownHours = 4f;

    private WorkerThought RecordWorkerThought(
        DriverAgent worker,
        WorkerThoughtKind kind,
        WorkerThoughtTone tone,
        int intensity,
        string templateKey,
        IReadOnlyList<WorkerThoughtPlaceholder> placeholders,
        WorkerThoughtSubjectType opinionSubjectType = WorkerThoughtSubjectType.None,
        int opinionSubjectId = 0,
        string opinionSubjectKey = null,
        string opinionFallbackLabel = null,
        int opinionDelta = 0,
        string cooldownKey = null,
        float cooldownHours = WorkerThoughtDefaultCooldownHours,
        string thoughtKey = null,
        WorkerThoughtPriority priority = WorkerThoughtPriority.Normal,
        bool active = false,
        float expiresWorldHour = 0f)
    {
        if (worker == null || string.IsNullOrWhiteSpace(templateKey) || worker.HasDepartedTown)
        {
            return null;
        }

        float now = GetCurrentWorldHour();
        string resolvedThoughtKey = string.IsNullOrWhiteSpace(thoughtKey) ? templateKey : thoughtKey;
        string resolvedCooldownKey = string.IsNullOrWhiteSpace(cooldownKey)
            ? $"{templateKey}|{opinionSubjectType}|{opinionSubjectId}|{opinionSubjectKey}"
            : cooldownKey;
        if (cooldownHours > 0f &&
            worker.WorkerThoughtCooldownWorldHours.TryGetValue(resolvedCooldownKey, out float nextAllowedHour) &&
            now < nextAllowedHour)
        {
            return null;
        }

        if (cooldownHours > 0f)
        {
            worker.WorkerThoughtCooldownWorldHours[resolvedCooldownKey] = now + cooldownHours;
        }

        WorkerThought thought = new()
        {
            Key = resolvedThoughtKey,
            Kind = kind,
            Tone = tone,
            Priority = priority,
            Intensity = Mathf.Clamp(intensity, 0, 100),
            TemplateKey = templateKey,
            CreatedDay = currentDay,
            CreatedWorldHour = now,
            Active = active,
            ExpiresWorldHour = expiresWorldHour
        };

        if (placeholders != null)
        {
            for (int i = 0; i < placeholders.Count; i++)
            {
                if (placeholders[i] != null)
                {
                    thought.Placeholders.Add(placeholders[i]);
                }
            }
        }

        worker.Thoughts.Insert(0, thought);
        while (worker.Thoughts.Count > WorkerThoughtMemoryCap)
        {
            worker.Thoughts.RemoveAt(worker.Thoughts.Count - 1);
        }

        if (opinionDelta != 0 && opinionSubjectType != WorkerThoughtSubjectType.None)
        {
            UpdateWorkerOpinion(worker, opinionSubjectType, opinionSubjectId, opinionSubjectKey, opinionFallbackLabel, opinionDelta, now);
        }

        RecordSocialSignalFromWorkerThought(worker, thought, opinionSubjectType, opinionSubjectId, opinionSubjectKey, opinionFallbackLabel);
        isDriversScreenDirty = true;
        SessionDebugLogger.Log(
            "THOUGHT",
            $"{worker.DriverName}: {RenderWorkerThought(thought, false)}; key={resolvedThoughtKey}; template={templateKey}; active={active}; priority={priority}; tone={tone}; intensity={intensity}; opinion={opinionSubjectType}/{opinionSubjectId}/{opinionSubjectKey}; delta={opinionDelta:+#;-#;0}.");
        return thought;
    }

    private void UpdateWorkerOpinion(
        DriverAgent worker,
        WorkerThoughtSubjectType subjectType,
        int subjectId,
        string subjectKey,
        string fallbackLabel,
        int delta,
        float now)
    {
        WorkerOpinion opinion = FindWorkerOpinion(worker, subjectType, subjectId, subjectKey);
        if (opinion == null)
        {
            opinion = new WorkerOpinion
            {
                SubjectType = subjectType,
                SubjectId = subjectId,
                SubjectKey = subjectKey ?? string.Empty,
                FallbackLabel = fallbackLabel ?? string.Empty
            };
            worker.Opinions.Add(opinion);
        }

        opinion.Score = Mathf.Clamp(opinion.Score + delta, -100, 100);
        opinion.Confidence = Mathf.Clamp(opinion.Confidence + Mathf.Abs(delta), 0, 100);
        opinion.LastUpdatedWorldHour = now;
        if (!string.IsNullOrWhiteSpace(fallbackLabel))
        {
            opinion.FallbackLabel = fallbackLabel;
        }

        TrimWorkerOpinions(worker);
    }

    private static WorkerOpinion FindWorkerOpinion(DriverAgent worker, WorkerThoughtSubjectType subjectType, int subjectId, string subjectKey)
    {
        if (worker == null)
        {
            return null;
        }

        string normalizedKey = subjectKey ?? string.Empty;
        for (int i = 0; i < worker.Opinions.Count; i++)
        {
            WorkerOpinion opinion = worker.Opinions[i];
            if (opinion != null &&
                opinion.SubjectType == subjectType &&
                opinion.SubjectId == subjectId &&
                opinion.SubjectKey == normalizedKey)
            {
                return opinion;
            }
        }

        return null;
    }

    private static void TrimWorkerOpinions(DriverAgent worker)
    {
        while (worker.Opinions.Count > WorkerOpinionMemoryCap)
        {
            int removeIndex = 0;
            int removeScore = int.MaxValue;
            for (int i = 0; i < worker.Opinions.Count; i++)
            {
                WorkerOpinion opinion = worker.Opinions[i];
                int score = opinion == null
                    ? int.MinValue
                    : Mathf.Abs(opinion.Score) + opinion.Confidence;
                if (score < removeScore)
                {
                    removeScore = score;
                    removeIndex = i;
                }
            }

            worker.Opinions.RemoveAt(removeIndex);
        }
    }

    private WorkerThoughtPlaceholder ThoughtText(string key, string label)
    {
        return new WorkerThoughtPlaceholder
        {
            Key = key,
            SubjectType = WorkerThoughtSubjectType.Text,
            FallbackLabel = label ?? string.Empty
        };
    }

    private WorkerThoughtPlaceholder ThoughtWorker(string key, DriverAgent worker)
    {
        return new WorkerThoughtPlaceholder
        {
            Key = key,
            SubjectType = WorkerThoughtSubjectType.Worker,
            SubjectId = worker?.DriverId ?? 0,
            FallbackLabel = worker?.DriverName ?? string.Empty
        };
    }

    private WorkerThoughtPlaceholder ThoughtBuilding(string key, LocationType buildingType)
    {
        return new WorkerThoughtPlaceholder
        {
            Key = key,
            SubjectType = WorkerThoughtSubjectType.BuildingType,
            SubjectKey = buildingType.ToString(),
            FallbackLabel = GetSelectedLocationDisplayName(buildingType)
        };
    }

    private WorkerThoughtPlaceholder ThoughtNeed(string key, WorkerNeedKind need)
    {
        return new WorkerThoughtPlaceholder
        {
            Key = key,
            SubjectType = WorkerThoughtSubjectType.Need,
            SubjectKey = need.ToString(),
            FallbackLabel = need.ToString()
        };
    }

    private WorkerThoughtPlaceholder ThoughtFamily(string key, WorkerFamily family)
    {
        return new WorkerThoughtPlaceholder
        {
            Key = key,
            SubjectType = WorkerThoughtSubjectType.Family,
            SubjectId = family?.Id ?? -1,
            FallbackLabel = family != null ? $"family #{family.Id}" : string.Empty
        };
    }

    private WorkerThoughtPlaceholder ThoughtChild(string key, WorkerChild child)
    {
        return new WorkerThoughtPlaceholder
        {
            Key = key,
            SubjectType = WorkerThoughtSubjectType.Child,
            SubjectId = child?.Id ?? -1,
            FallbackLabel = child?.Name ?? string.Empty
        };
    }

    private void RecordWorkerServiceThought(DriverAgent worker, LocationType service, WorkerNeedKind need, string templateKey, WorkerThoughtTone tone, int intensity, int opinionDelta)
    {
        RecordWorkerThought(
            worker,
            WorkerThoughtKind.Need,
            tone,
            intensity,
            templateKey,
            new[]
            {
                ThoughtBuilding("service", service),
                ThoughtNeed("need", need)
            },
            WorkerThoughtSubjectType.BuildingType,
            0,
            service.ToString(),
            GetSelectedLocationDisplayName(service),
            opinionDelta,
            $"{templateKey}|{service}",
            3f);
    }

    private void RecordWorkerServiceMissingThought(DriverAgent worker, LocationType service, WorkerNeedKind need, string reason)
    {
        RecordWorkerThought(
            worker,
            WorkerThoughtKind.Need,
            WorkerThoughtTone.Negative,
            62,
            "service_missing",
            new[]
            {
                ThoughtBuilding("service", service),
                ThoughtNeed("need", need),
                ThoughtText("reason", reason)
            },
            WorkerThoughtSubjectType.BuildingType,
            0,
            service.ToString(),
            GetSelectedLocationDisplayName(service),
            -5,
            $"service_missing|{service}",
            8f);
    }

    private void RecordWorkerArrivalThought(DriverAgent worker, string source)
    {
        RecordWorkerThought(
            worker,
            WorkerThoughtKind.City,
            WorkerThoughtTone.Neutral,
            38,
            "worker_arrived",
            new[]
            {
                ThoughtText("source", source)
            },
            WorkerThoughtSubjectType.Text,
            0,
            "town_arrival",
            "town arrival",
            1,
            "worker_arrived",
            72f);
    }

    private void RecordWorkerNoJobThought(DriverAgent worker, string reason)
    {
        AddOrKeepActiveWorkerThought(
            worker,
            "no_job_warning",
            WorkerThoughtKind.Work,
            WorkerThoughtTone.Negative,
            worker != null && worker.Money < 50 ? 70 : 52,
            "no_job_warning",
            new[]
            {
                ThoughtText("reason", reason)
            },
            worker != null && worker.Money < 80 ? WorkerThoughtPriority.High : WorkerThoughtPriority.Normal,
            WorkerThoughtSubjectType.Text,
            0,
            "city_work",
            "work in town",
            -3);
    }

    private string RenderWorkerThought(WorkerThought thought, bool ru)
    {
        if (thought == null)
        {
            return string.Empty;
        }

        string text = GetWorkerThoughtTemplate(thought.TemplateKey, ru);
        for (int i = 0; i < thought.Placeholders.Count; i++)
        {
            WorkerThoughtPlaceholder placeholder = thought.Placeholders[i];
            if (placeholder == null || string.IsNullOrWhiteSpace(placeholder.Key))
            {
                continue;
            }

            text = text.Replace("{" + placeholder.Key + "}", ResolveWorkerThoughtPlaceholder(placeholder, ru));
        }

        return text;
    }

    private string ResolveWorkerThoughtPlaceholder(WorkerThoughtPlaceholder placeholder, bool ru)
    {
        if (placeholder == null)
        {
            return string.Empty;
        }

        switch (placeholder.SubjectType)
        {
            case WorkerThoughtSubjectType.Worker:
                DriverAgent worker = GetDriverAgentById(placeholder.SubjectId);
                return worker != null && !worker.HasDepartedTown
                    ? worker.DriverName
                    : Fallback(placeholder, ru ? "\u0431\u044b\u0432\u0448\u0438\u0439 \u0437\u043d\u0430\u043a\u043e\u043c\u044b\u0439" : "former contact");
            case WorkerThoughtSubjectType.BuildingType:
                return System.Enum.TryParse(placeholder.SubjectKey, out LocationType locationType)
                    ? GetSelectedLocationDisplayName(locationType)
                    : Fallback(placeholder, ru ? "\u043c\u0435\u0441\u0442\u043e" : "place");
            case WorkerThoughtSubjectType.Need:
                return FormatWorkerThoughtNeed(placeholder.SubjectKey, ru);
            case WorkerThoughtSubjectType.Family:
                WorkerFamily family = GetWorkerFamilyById(placeholder.SubjectId);
                return family != null
                    ? (ru ? $"\u0441\u0435\u043c\u044c\u044f #{family.Id}" : $"family #{family.Id}")
                    : Fallback(placeholder, ru ? "\u0441\u0435\u043c\u044c\u044f" : "family");
            case WorkerThoughtSubjectType.Child:
                WorkerChild child = GetWorkerChildById(placeholder.SubjectId);
                return child != null
                    ? child.Name
                    : Fallback(placeholder, ru ? "\u0440\u0435\u0431\u0435\u043d\u043e\u043a" : "child");
            default:
                return Fallback(placeholder, string.Empty);
        }
    }

    private static string Fallback(WorkerThoughtPlaceholder placeholder, string defaultLabel)
    {
        return !string.IsNullOrWhiteSpace(placeholder.FallbackLabel)
            ? placeholder.FallbackLabel
            : defaultLabel;
    }

    private static string GetWorkerThoughtTemplate(string key, bool ru)
    {
        return key switch
        {
            "meal_service_good" => ru ? "\u0412 {service} \u043d\u0430\u043a\u043e\u043d\u0435\u0446 \u043d\u043e\u0440\u043c\u0430\u043b\u044c\u043d\u043e \u043f\u043e\u0435\u043b. {need} \u0431\u043e\u043b\u044c\u0448\u0435 \u043d\u0435 \u0434\u0430\u0432\u0438\u0442." : "A real meal at {service} helped. {need} is not pressing now.",
            "leisure_service_good" => ru ? "{service} \u043f\u043e\u043c\u043e\u0433 \u0432\u044b\u0434\u043e\u0445\u043d\u0443\u0442\u044c. \u0418\u043d\u043e\u0433\u0434\u0430 \u044d\u0442\u043e\u0433\u043e \u0445\u0432\u0430\u0442\u0430\u0435\u0442." : "{service} helped me breathe a little. Sometimes that is enough.",
            "sleep_service_good" => ru ? "{service} \u0434\u0430\u043b \u0432\u044b\u0441\u043f\u0430\u0442\u044c\u0441\u044f. \u0417\u0430\u0432\u0442\u0440\u0430 \u0431\u0443\u0434\u0435\u0442 \u043b\u0435\u0433\u0447\u0435." : "{service} gave me a proper sleep. Tomorrow should be easier.",
            "home_sleep_good" => ru ? "\u0414\u043e\u043c\u0430 \u0441\u043f\u0438\u0442\u0441\u044f \u0441\u043f\u043e\u043a\u043e\u0439\u043d\u0435\u0435. {home} \u0443\u0436\u0435 \u043f\u043e\u0445\u043e\u0436 \u043d\u0430 \u043c\u043e\u0435 \u043c\u0435\u0441\u0442\u043e." : "Sleeping at home feels calmer. {home} is starting to feel like my place.",
            "service_missing" => ru ? "\u0414\u043b\u044f {need} \u043d\u0443\u0436\u0435\u043d {service}, \u043d\u043e \u0435\u0433\u043e \u043d\u0435\u0442. \u041f\u0440\u0438\u0447\u0438\u043d\u0430: {reason}." : "I need {service} for {need}, but it is missing. Reason: {reason}.",
            "service_unaffordable" => ru ? "\u0425\u043e\u0442\u0435\u043b\u043e\u0441\u044c \u0432 {service}, \u043d\u043e \u0434\u0435\u043d\u0435\u0433 \u043d\u0435 \u0445\u0432\u0430\u0442\u0438\u043b\u043e. \u041d\u0430 \u0441\u0447\u0435\u0442\u0443 {balance}." : "I wanted {service}, but I could not afford it. Balance: {balance}.",
            "need_fallback_bad" => ru ? "{need} \u043f\u0440\u0438\u0448\u043b\u043e\u0441\u044c \u0437\u0430\u043a\u0440\u044b\u0432\u0430\u0442\u044c \u043a\u0430\u043a \u043f\u043e\u043f\u0430\u043b\u043e. \u041f\u0440\u0438\u0447\u0438\u043d\u0430: {reason}." : "I had to handle {need} however I could. Reason: {reason}.",
            "salary_paid" => ru ? "\u041f\u043e\u043b\u0443\u0447\u0438\u043b {amount}. \u041d\u0430 \u0441\u0447\u0435\u0442\u0443 {balance} - \u043c\u043e\u0436\u043d\u043e \u0432\u044b\u0434\u043e\u0445\u043d\u0443\u0442\u044c." : "Got {amount}. Balance is {balance}, so I can breathe for a moment.",
            "worker_arrived" => ru ? "\u042f \u0432 \u0433\u043e\u0440\u043e\u0434\u0435. \u041f\u043e\u0440\u0430 \u043f\u043e\u043d\u044f\u0442\u044c, \u043a\u0430\u043a \u0442\u0443\u0442 \u0436\u0438\u0442\u044c: {source}." : "I am in town now. Time to see how life works here: {source}.",
            "no_job_today" => ru ? "\u0420\u0430\u0431\u043e\u0442\u044b \u043f\u043e\u043a\u0430 \u043d\u0435\u0442. \u0415\u0441\u043b\u0438 \u044d\u0442\u043e \u0437\u0430\u0442\u044f\u043d\u0435\u0442\u0441\u044f, \u0434\u0435\u043d\u044c\u0433\u0438 \u0431\u044b\u0441\u0442\u0440\u043e \u0440\u0430\u0441\u0442\u0430\u044e\u0442." : "No job yet. If this drags on, the money will melt fast.",
            "no_job_warning" => ru ? "Работы пока нет. Если это затянется, деньги быстро растают." : "No job yet. If this drags on, the money will melt fast.",
            "no_job_warning_known_place" => ru ? "Работы пока нет. В голове всплывает {knownPlace}: возможно, там помогут с занятостью." : "No job yet. I keep thinking about {knownPlace}; maybe it can help with work.",
            "starter_job_suggestion" => ru ? "Нужно начать с простой работы. Подойдет даже стартовая вакансия." : "I should start with simple work. Even a starter vacancy would help.",
            "starter_job_resolved" => ru ? "Вопрос первой работы закрыт." : "The starter job worry is settled.",
            "job_found" => ru ? "Работа найдена: {job}. Теперь есть понятный следующий шаг." : "Found work: {job}. There is a clear next step now.",
            "need_meal_warning" => ru ? "Пора перекусить. Лучше не доводить еду до критического уровня." : "I should eat soon. Better not let food become critical.",
            "need_meal_critical" => ru ? "Критический голод. Нужно срочно найти еду." : "Critical hunger. I need food urgently.",
            "need_meal_critical_known_place" => ru ? "Критический голод. Я помню про {knownPlace}; нужно добраться туда." : "Critical hunger. I remember {knownPlace}; I should get there.",
            "need_sleep_warning" => ru ? "Сил почти не осталось. Скоро нужен отдых." : "Energy is running low. I will need rest soon.",
            "need_sleep_critical" => ru ? "Критическая усталость. Нужно срочно восстановить бодрость." : "Critical fatigue. I need to recover energy urgently.",
            "need_sleep_critical_known_place" => ru ? "Критическая усталость. В памяти держится {knownPlace}; там можно восстановиться." : "Critical fatigue. I remember {knownPlace}; I can recover there.",
            "need_leisure_warning" => ru ? "Нужно выдохнуть. Без отдыха настроение быстро просядет." : "I need a break. Without leisure, mood will drop fast.",
            "need_leisure_critical" => ru ? "Отдых на пределе. Нужно срочно отвлечься." : "Leisure is at the limit. I need a break urgently.",
            "need_leisure_critical_known_place" => ru ? "Отдых на пределе. Я знаю про {knownPlace}; возможно, там получится выдохнуть." : "Leisure is at the limit. I know about {knownPlace}; maybe I can breathe there.",
            "used_snack" => ru ? "Автоматически съел Snack, чтобы не сорваться по еде." : "Auto-used a Snack before food became a crisis.",
            "used_coffee" => ru ? "Автоматически выпил Coffee, чтобы вернуть бодрость." : "Auto-used Coffee to recover some energy.",
            "house_bought" => ru ? "\u0422\u0435\u043f\u0435\u0440\u044c {home} - \u043c\u043e\u0439 \u0434\u043e\u043c. \u0418\u043d\u0442\u0435\u0440\u0435\u0441\u043d\u043e, \u0447\u0442\u043e \u043f\u043e\u043b\u0443\u0447\u0438\u0442\u0441\u044f \u0434\u0430\u043b\u044c\u0448\u0435." : "{home} is mine now. I wonder what comes next.",
            "social_talk_good" => ru ? "{otherWorker} \u043e\u043a\u0430\u0437\u0430\u043b\u0441\u044f \u043f\u0440\u0438\u044f\u0442\u043d\u044b\u043c \u0441\u043e\u0431\u0435\u0441\u0435\u0434\u043d\u0438\u043a\u043e\u043c." : "{otherWorker} turned out to be easy to talk to.",
            "social_shared_place" => ru ? "\u0412 {place} \u044f \u0437\u0430\u043c\u0435\u0442\u0438\u043b {otherWorker}. \u041a\u0430\u0436\u0435\u0442\u0441\u044f, \u043c\u044b \u0441\u0442\u0430\u043d\u043e\u0432\u0438\u043c\u0441\u044f \u0437\u043d\u0430\u043a\u043e\u043c\u044b\u043c\u0438." : "I noticed {otherWorker} at {place}. We are starting to know each other.",
            "social_learned_new_topic" => ru ? "\u0412 \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u0435 \u0441 {otherWorker} \u0432 \u0433\u043e\u043b\u043e\u0432\u0435 \u043e\u0441\u0442\u0430\u043b\u0430\u0441\u044c \u0442\u0435\u043c\u0430: \u00ab{topic}\u00bb." : "After talking with {otherWorker}, one topic stayed with me: \"{topic}\".",
            "knowledge_reflection_building" => ru ? "Запоминаю ориентир: {knownPlace}. Может пригодиться, если день снова пойдет наперекосяк." : "I am memorizing {knownPlace}. It may help if the day goes sideways again.",
            "family_formed" => ru ? "{otherWorker} \u0442\u0435\u043f\u0435\u0440\u044c \u0441\u0435\u043c\u044c\u044f. \u041d\u0443\u0436\u043d\u043e \u043e\u0431\u0436\u0438\u0442\u044c\u0441\u044f \u0432 {home}." : "{otherWorker} is family now. We need to settle into {home}.",
            "child_born" => ru ? "\u0423 \u043d\u0430\u0441 \u0440\u043e\u0434\u0438\u043b\u0441\u044f {child}. \u0414\u043e\u043c \u0441\u0440\u0430\u0437\u0443 \u0441\u0442\u0430\u043b \u0434\u0440\u0443\u0433\u0438\u043c." : "{child} was born. The house feels different already.",
            "bus_chosen" => ru ? "\u041f\u043e\u0435\u0434\u0443 \u043d\u0430 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0435: {reason}. \u041f\u0435\u0448\u043a\u043e\u043c \u0441\u043b\u0438\u0448\u043a\u043e\u043c \u0434\u043e\u043b\u0433\u043e." : "I will take the bus for {reason}. Walking would take too long.",
            "bus_unavailable" => ru ? "\u0410\u0432\u0442\u043e\u0431\u0443\u0441 \u043d\u0435 \u0432\u044b\u0440\u0443\u0447\u0438\u043b: {reason}." : "The bus did not help: {reason}.",
            "low_money" => ru ? "\u0414\u0435\u043d\u0435\u0433 \u043f\u043e\u0447\u0442\u0438 \u043d\u0435\u0442. \u041b\u044e\u0431\u0430\u044f \u043c\u0435\u043b\u043e\u0447\u044c \u0441\u0442\u0430\u043d\u043e\u0432\u0438\u0442\u0441\u044f \u043f\u0440\u043e\u0431\u043b\u0435\u043c\u043e\u0439." : "Money is almost gone. Every little thing becomes a problem.",
            "street_litter_low" => ru ? "\u041d\u0430 \u0443\u043b\u0438\u0446\u0435 \u043f\u043e\u043f\u0430\u0434\u0430\u0435\u0442\u0441\u044f \u043c\u0443\u0441\u043e\u0440. \u041d\u0435 \u043a\u0440\u0438\u0442\u0438\u0447\u043d\u043e, \u043d\u043e \u043d\u0435\u043f\u0440\u0438\u044f\u0442\u043d\u043e." : "There is some litter on the street. Not a crisis, but unpleasant.",
            "street_litter_medium" => ru ? "\u0423\u043b\u0438\u0446\u044b \u043c\u0435\u0441\u0442\u0430\u043c\u0438 \u0437\u0430\u043c\u0435\u0442\u043d\u043e \u0437\u0430\u043c\u0443\u0441\u043e\u0440\u0435\u043d\u044b." : "Some streets are noticeably littered.",
            "street_litter_high" => ru ? "\u0413\u0440\u044f\u0437\u044c \u043d\u0430 \u0443\u043b\u0438\u0446\u0430\u0445 \u0443\u0436\u0435 \u0440\u0430\u0437\u0434\u0440\u0430\u0436\u0430\u0435\u0442." : "The street litter is getting irritating.",
            "affect_financial_pressure" => ru ? "\u0414\u0435\u043d\u044c\u0433\u0438 \u0436\u043c\u0443\u0442: {reason}." : "Money is pressing: {reason}.",
            "affect_family_anxiety" => ru ? "\u0417\u0430 \u0441\u0435\u043c\u044c\u044e \u0442\u0440\u0435\u0432\u043e\u0436\u043d\u043e: {reason}." : "Family feels worrying: {reason}.",
            "affect_relief_after_rest" => ru ? "\u041e\u0442\u0434\u044b\u0445 \u0441\u043d\u044f\u043b \u0447\u0430\u0441\u0442\u044c \u043d\u0430\u043f\u0440\u044f\u0436\u0435\u043d\u0438\u044f: {reason}." : "Rest eased some pressure: {reason}.",
            "affect_hangover" => ru ? "\u041f\u043e\u0441\u043b\u0435 \u043e\u0442\u0434\u044b\u0445\u0430 \u0442\u044f\u0436\u0435\u043b\u043e: {reason}." : "The rest left a heavy aftertaste: {reason}.",
            "affect_loneliness" => ru ? "\u041d\u0435 \u0445\u0432\u0430\u0442\u0430\u0435\u0442 \u0436\u0438\u0432\u044b\u0445 \u0441\u0432\u044f\u0437\u0435\u0439: {reason}." : "Real ties are missing: {reason}.",
            "affect_inspired_by_nature" => ru ? "\u041f\u0440\u0438\u0440\u043e\u0434\u0430 \u043f\u043e\u043c\u043e\u0433\u0430\u0435\u0442 \u0434\u0435\u0440\u0436\u0430\u0442\u044c\u0441\u044f: {reason}." : "Nature helps them hold together: {reason}.",
            "affect_litter_irritation" => ru ? "\u041c\u0443\u0441\u043e\u0440 \u0446\u0435\u043f\u043b\u044f\u0435\u0442 \u0432\u0437\u0433\u043b\u044f\u0434: {reason}." : "Litter keeps catching the eye: {reason}.",
            "affect_gambling_excitement" => ru ? "\u0410\u0437\u0430\u0440\u0442 \u0435\u0449\u0435 \u0437\u0432\u0435\u043d\u0438\u0442 \u0432 \u0433\u043e\u043b\u043e\u0432\u0435: {reason}." : "The thrill is still ringing: {reason}.",
            "affect_gambling_regret" => ru ? "\u041f\u043e\u0441\u043b\u0435 \u0441\u0442\u0430\u0432\u043a\u0438 \u0442\u044f\u043d\u0435\u0442 \u043a \u0434\u0435\u043d\u044c\u0433\u0430\u043c: {reason}." : "The bet pulls attention back to money: {reason}.",
            "affect_stable_routine" => ru ? "\u0414\u0435\u043d\u044c \u0434\u0435\u0440\u0436\u0438\u0442\u0441\u044f \u0440\u043e\u0432\u043d\u043e: {reason}." : "The day feels steady: {reason}.",
            "stable_life" => ru ? "\u0421\u0435\u0433\u043e\u0434\u043d\u044f \u0432\u0441\u0435 \u0431\u043e\u043b\u0435\u0435-\u043c\u0435\u043d\u0435\u0435 \u0441\u0442\u0430\u0431\u0438\u043b\u044c\u043d\u043e." : "Today feels more or less stable.",
            _ => key ?? string.Empty
        };
    }

    private static string FormatWorkerThoughtNeed(string key, bool ru)
    {
        return key switch
        {
            nameof(WorkerNeedKind.Meal) => ru ? "\u0435\u0434\u0430" : "food",
            nameof(WorkerNeedKind.Sleep) => ru ? "\u0441\u043e\u043d" : "sleep",
            nameof(WorkerNeedKind.Leisure) => ru ? "\u043e\u0442\u0434\u044b\u0445" : "leisure",
            _ => string.IsNullOrWhiteSpace(key) ? (ru ? "\u043f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u044c" : "need") : key
        };
    }

    private WorkerChild GetWorkerChildById(int childId)
    {
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child != null && child.Id == childId)
            {
                return child;
            }
        }

        return null;
    }
}
