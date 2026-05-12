using UnityEngine;

public partial class GameBootstrap
{
    private static int CompareCityComplaintsForDisplay(CityComplaint a, CityComplaint b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        int stateCompare = GetCityComplaintDisplayStateRank(a.State).CompareTo(GetCityComplaintDisplayStateRank(b.State));
        if (stateCompare != 0) return stateCompare;
        int severityCompare = b.Severity.CompareTo(a.Severity);
        if (severityCompare != 0) return severityCompare;
        return b.CreatedWorldHour.CompareTo(a.CreatedWorldHour);
    }

    private string GetCityHallQuickResourceText()
    {
        bool ru = IsRussianLanguage();
        int activeCount = CountOpenCityComplaints();
        CityComplaint acceptedGoal = GetActiveAcceptedCityServiceRequest();
        CityComplaint top = GetHighestPriorityOpenCityComplaint();
        string activeLine = activeCount > 0
            ? (ru ? $"{activeCount} активных" : $"{activeCount} active")
            : (ru ? "нет активных" : "none active");
        string goalLine = acceptedGoal != null
            ? FormatCityComplaintTitle(acceptedGoal, ru)
            : (ru ? "нет принятой цели" : "no accepted goal");
        string topLine = top != null
            ? FormatCityComplaintTitle(top, ru)
            : (ru ? "нет активных обращений" : "no active requests");
        int dueHours = Mathf.RoundToInt(GetCityComplaintDueWorldHours());
        string description = ru
            ? $"Жители приносят сюда городские обращения.\nПринятое обращение становится целью на {dueHours} ч.\nВыполнение повышает доверие; отказ или просрочка снижают."
            : $"Citizens file city requests here.\nAccepted requests become {dueHours}h goals.\nCompletion raises trust; rejection or expiry lowers it.";

        if (acceptedGoal != null)
        {
            string timer = FormatCityComplaintTimeLeft(acceptedGoal, ru);
            if (!string.IsNullOrWhiteSpace(timer))
            {
                goalLine = $"{goalLine} · {timer}";
            }
        }

        return description + "\n" +
               FormatValueLine(ru ? "Обращения" : "Requests", activeLine) + "\n" +
               FormatValueLine(ru ? "Городская цель" : "City goal", goalLine) + "\n" +
               FormatValueLine(ru ? "Главное сейчас" : "Focus", topLine);
    }

    private string FormatCityComplaintTargetName(CityComplaint complaint)
    {
        if (complaint != null && complaint.Category == CityComplaintCategory.SocialIntroduction)
        {
            return string.IsNullOrWhiteSpace(complaint.SocialTargetWorkerName)
                ? (IsRussianLanguage() ? "житель" : "resident")
                : complaint.SocialTargetWorkerName;
        }

        if (complaint != null && complaint.Category == CityComplaintCategory.PublicConcern)
        {
            bool ru = IsRussianLanguage();
            string title = ru ? complaint.IssueTitleRu : complaint.IssueTitleEn;
            return string.IsNullOrWhiteSpace(title)
                ? GetSocialSignalCategoryLabel(complaint.IssueSignalCategory, ru)
                : title;
        }

        string targetName = complaint != null && complaint.LinkedLocationType.HasValue
            ? GetSelectedLocationDisplayName(complaint.LinkedLocationType.Value)
            : (IsRussianLanguage() ? "сервис" : "service");
        return complaint != null && complaint.RequiredLocationCount > 1
            ? $"{targetName} x{complaint.RequiredLocationCount}"
            : targetName;
    }

    private string FormatCityComplaintTitle(CityComplaint complaint, bool ru)
    {
        if (complaint == null)
        {
            return ru ? "Обращение" : "Request";
        }

        int signerCount = GetCityComplaintSignerCount(complaint);
        string countSuffix = signerCount > 1 ? $" ({signerCount})" : string.Empty;
        return complaint.Category switch
        {
            CityComplaintCategory.NeedPressure => $"{GetCityComplaintIssueTitle(complaint, ru)}{countSuffix}",
            CityComplaintCategory.NoJob => ru ? $"Нужна работа{countSuffix}" : $"Need jobs{countSuffix}",
            CityComplaintCategory.LowMoney => ru ? $"Не хватает денег{countSuffix}" : $"Low money{countSuffix}",
            CityComplaintCategory.ServiceMissing => ru ? $"Нужен: {FormatCityComplaintTargetName(complaint)}{countSuffix}" : $"Build: {FormatCityComplaintTargetName(complaint)}{countSuffix}",
            CityComplaintCategory.FamilyStress => ru ? $"Семейный стресс{countSuffix}" : $"Family stress{countSuffix}",
            CityComplaintCategory.SocialIntroduction => ru ? $"Тема для разговора{countSuffix}" : $"Conversation topic{countSuffix}",
            CityComplaintCategory.PublicConcern => ru ? $"Общая проблема: {FormatCityComplaintTargetName(complaint)}{countSuffix}" : $"Public concern: {FormatCityComplaintTargetName(complaint)}{countSuffix}",
            _ => ru ? $"Городское обращение{countSuffix}" : $"City request{countSuffix}"
        };
    }

    private string FormatCityComplaintDetail(CityComplaint complaint, bool ru)
    {
        if (complaint == null)
        {
            return string.Empty;
        }

        string target = FormatCityComplaintTargetName(complaint);
        string state = GetCityComplaintStateLabel(complaint.State, ru);
        string signers = FormatCityComplaintSignerNames(complaint, ru);
        string timer = complaint.State == CityComplaintState.Accepted
            ? FormatCityComplaintTimeLeft(complaint, ru)
            : string.Empty;

        string reason = complaint.Category switch
        {
            CityComplaintCategory.NeedPressure => complaint.LinkedNeed.HasValue
                ? (ru
                    ? $"Проблема: {GetCityComplaintNeedLabel(complaint.LinkedNeed.Value, ru)} повторяется у подписавших жителей."
                    : $"Problem: repeated {GetCityComplaintNeedLabel(complaint.LinkedNeed.Value, ru)} pressure among signed citizens.")
                : (ru ? "Повторяющаяся проблема с нуждами." : "Repeated needs pressure."),
            CityComplaintCategory.NoJob => ru ? "Подписавшие жители долго остаются без назначения." : "Signed citizens have remained without assignments.",
            CityComplaintCategory.LowMoney => ru ? "У подписавших жителей мало денег, а нужды уже давят." : "Signed citizens have very low money while needs are pressing.",
            CityComplaintCategory.ServiceMissing => ru
                ? $"{GetCityServiceRequestLiteraryBody(complaint.LinkedLocationType)}\n\nПросьба простая, почти официальная: построить {target}."
                : $"Suggested building: {target}.",
            CityComplaintCategory.FamilyStress => FormatFamilyStressComplaintReason(complaint, ru),
            CityComplaintCategory.SocialIntroduction => ru
                ? $"{complaint.WorkerName} хочет поговорить с {FormatCityComplaintTargetName(complaint)}, но разговор, как и всякое городское дело, требует бумажки, печати и человека, который скажет первую странную фразу.\n\nЕсли принять обращение, Ратуша попросит вас подсказать тему."
                : $"{complaint.WorkerName} wants to talk to {FormatCityComplaintTargetName(complaint)} and needs a topic. Accepting will open the conversation scene.",
            CityComplaintCategory.PublicConcern => ru
                ? $"Ноосфера и жители поймали повторяющийся негативный сигнал: {FormatCityComplaintPublicConcernReason(complaint, true)}\n\nЭто обращение появилось не случайно, а из похожих переживаний нескольких жителей."
                : $"The Noosphere and citizens detected a repeated negative signal: {FormatCityComplaintPublicConcernReason(complaint, false)}\n\nThis request came from a cluster of similar lived experiences.",
            _ => string.Empty
        };

        if (!IsCityComplaintActive(complaint) && !string.IsNullOrWhiteSpace(complaint.ResolveReason))
        {
            reason += ru ? $"\nИтог: {FormatCityComplaintResolveReason(complaint.ResolveReason, ru)}." : $"\nResult: {FormatCityComplaintResolveReason(complaint.ResolveReason, ru)}.";
        }

        string timerLine = string.IsNullOrWhiteSpace(timer)
            ? string.Empty
            : (ru ? $"\nСрок: {timer}" : $"\nDue: {timer}");

        return ru
            ? $"Статус: {state}\nПодписали: {signers}\n{reason}{timerLine}"
            : $"State: {state}\nSigned: {signers}\n{reason}{timerLine}";
    }

    private static string GetCityServiceRequestLiteraryBody(LocationType? target)
    {
        if (!target.HasValue)
        {
            return "Городу снова нужно что-то полезное. Это звучит подозрительно здраво, поэтому, конечно, требует печати Ратуши.";
        }

        return target.Value switch
        {
            LocationType.Kiosk => "Город дозрел до великого института маленьких покупок. Людям нужно место, где можно купить что-то почти неважное ровно в тот момент, когда без этого уже никак.",
            LocationType.Canteen => "На пустом месте уже мысленно дымится кастрюля. Горожане проходят мимо и делают вид, что это не надежда, а просто ветер пахнет супом.",
            LocationType.Motel => "Ночью город звучит прилично, пока не прислушаться: это люди пытаются спать стоя и сохранять достоинство.",
            LocationType.Bar => "Вечерам нужно место, где можно официально ничего не решать. Иначе люди делают это прямо на улице, что выглядит менее культурно.",
            LocationType.CityPark => "Городу нужна зелень, которую не надо оправдывать производственной пользой. Просто место, где можно посидеть и не быть частью механизма.",
            LocationType.GamblingHall => "Некоторым нужен зал, где удачу можно обвинить официально. Это дешевле, чем спорить с небом на перекрестке.",
            LocationType.GasStation => "Техника тоже хочет пить, только громче и с запахом бензина. Люди называют это инфраструктурой, чтобы звучало менее тревожно.",
            LocationType.PrimarySchool => "Дети растут быстрее, чем город успевает делать вид, что это потом. Нужна начальная школа, пока семейная тревога не стала расписанием.",
            LocationType.SecondarySchool => "Подростки уже слишком взрослые для детских ответов и слишком юные для взрослых проблем. Городу нужна средняя школа.",
            _ => "Городу нужно новое здание. Не героическое, не судьбоносное, просто то самое, отсутствие которого начинает скрипеть в каждом дне."
        };
    }

    private string FormatFamilyStressComplaintReason(CityComplaint complaint, bool ru)
    {
        DriverAgent worker = GetDriverAgentById(complaint?.WorkerId ?? 0);
        WorkerFamily family = GetWorkerFamilyById(worker?.FamilyId ?? -1);
        if (family == null)
        {
            return ru
                ? "\u0421\u0435\u043c\u044c\u0438 \u0436\u0430\u043b\u0443\u044e\u0442\u0441\u044f \u043d\u0430 \u043d\u0430\u043a\u043e\u043f\u0438\u0432\u0448\u0438\u0439\u0441\u044f \u0441\u0442\u0440\u0435\u0441\u0441."
                : "Families report accumulated stress.";
        }

        int totalChildren = CountWorkerFamilyChildren(family.Id);
        int needingCare = CountWorkerFamilyChildrenNeedingChildCare(family);
        int covered = CountWorkerFamilyChildCareCovered(family);
        int shortfall = Mathf.Max(0, needingCare - covered);
        if (shortfall > 0)
        {
            return ru
                ? $"\u041d\u0435 \u0445\u0432\u0430\u0442\u0430\u0435\u0442 \u043c\u0435\u0441\u0442 \u0432 \u0434\u0435\u0442\u0441\u0430\u0434\u0443: {covered}/{needingCare}. \u0412 \u0441\u0435\u043c\u044c\u0435 \u0434\u0435\u0442\u0435\u0439: {totalChildren}."
                : $"Child-care capacity is short: {covered}/{needingCare}. Children in the family: {totalChildren}.";
        }

        LocationType? schoolShortage = GetWorkerFamilyMostNeededSchoolLocation(family);
        if (schoolShortage.HasValue)
        {
            int need = CountWorkerFamilyEducationNeed(family, schoolShortage.Value);
            int schoolCovered = CountWorkerFamilyEducationCovered(family, schoolShortage.Value);
            string schoolName = FormatEducationLocationName(schoolShortage.Value, ru);
            return ru
                ? $"\u041d\u0435 \u0445\u0432\u0430\u0442\u0430\u0435\u0442 \u043c\u0435\u0441\u0442: {schoolName}, {schoolCovered}/{need}. \u0412 \u0441\u0435\u043c\u044c\u0435 \u0434\u0435\u0442\u0435\u0439: {totalChildren}."
                : $"School seats are short: {schoolName}, {schoolCovered}/{need}. Children in the family: {totalChildren}.";
        }

        return ru
            ? $"\u0421\u0435\u043c\u0435\u0439\u043d\u0430\u044f \u043d\u0430\u0433\u0440\u0443\u0437\u043a\u0430 \u0440\u0430\u0441\u0442\u0435\u0442: \u0434\u0435\u0442\u0435\u0439 {totalChildren}, \u0441\u0447\u0430\u0441\u0442\u044c\u0435 \u0441\u0435\u043c\u044c\u0438 {family.Happiness}/100."
            : $"Family load is rising: {totalChildren} children, family happiness {family.Happiness}/100.";
    }

    private string FormatCityComplaintMeta(CityComplaint complaint, bool ru)
    {
        if (complaint == null)
        {
            return string.Empty;
        }

        int hour = Mathf.FloorToInt(Mathf.Repeat(complaint.CreatedWorldHour, 24f));
        string created = ru ? $"Д{complaint.CreatedDay} {hour:00}:00" : $"D{complaint.CreatedDay} {hour:00}:00";
        string severity = ru ? $"важность {complaint.Severity}" : $"severity {complaint.Severity}";
        if (complaint.State == CityComplaintState.Accepted && complaint.DueWorldHour > 0f)
        {
            return $"{created} | {severity} | {FormatCityComplaintTimeLeft(complaint, ru)}";
        }

        return $"{created} | {GetCityComplaintStateLabel(complaint.State, ru)}";
    }

    private static string GetCityComplaintNeedLabel(WorkerNeedKind need, bool ru)
    {
        return need switch
        {
            WorkerNeedKind.Meal => ru ? "голод" : "food",
            WorkerNeedKind.Sleep => ru ? "сон" : "sleep",
            WorkerNeedKind.Leisure => ru ? "досуг" : "leisure",
            _ => ru ? "нужда" : "need"
        };
    }

    private static Color GetCityComplaintAccentColor(CityComplaint complaint)
    {
        if (complaint == null)
        {
            return new Color(0.45f, 0.52f, 0.62f, 1f);
        }

        if (complaint.State == CityComplaintState.Resolved)
        {
            return new Color(0.32f, 0.56f, 0.38f, 1f);
        }

        if (complaint.State == CityComplaintState.Expired || complaint.State == CityComplaintState.Rejected)
        {
            return new Color(0.76f, 0.22f, 0.18f, 1f);
        }

        if (complaint.State == CityComplaintState.Accepted)
        {
            return new Color(0.22f, 0.58f, 0.32f, 1f);
        }

        return complaint.Severity >= 4
            ? new Color(0.84f, 0.24f, 0.18f, 1f)
            : complaint.Severity >= 3
                ? new Color(0.90f, 0.58f, 0.20f, 1f)
                : new Color(0.46f, 0.58f, 0.78f, 1f);
    }

    private static string FormatCityComplaintResolveReason(string reason, bool ru)
    {
        return reason switch
        {
            "deadline expired" => ru ? "срок истек" : "deadline expired",
            "rejected by player" => ru ? "отклонено" : "rejected",
            "requested service exists" => ru ? "здание построено" : "requested service exists",
            "already satisfied" => ru ? "уже выполнено" : "already satisfied",
            "public concern eased" => ru ? "напряжение спало" : "public concern eased",
            "social introduction completed" => ru ? "разговор состоялся" : "conversation happened",
            "social participant unavailable" => ru ? "участник недоступен" : "participant unavailable",
            _ => string.IsNullOrWhiteSpace(reason) ? (ru ? "без заметки" : "no note") : reason
        };
    }

    private static string FormatCityComplaintPublicConcernReason(CityComplaint complaint, bool ru)
    {
        if (complaint == null)
        {
            return ru ? "городская проблема" : "city issue";
        }

        string reason = ru ? complaint.IssueReasonRu : complaint.IssueReasonEn;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            return reason;
        }

        string title = ru ? complaint.IssueTitleRu : complaint.IssueTitleEn;
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        return GetSocialSignalCategoryLabel(complaint.IssueSignalCategory, ru);
    }

    private string FormatAcceptedCityComplaintGoalText(CityComplaint complaint, int dueHours, bool ru)
    {
        if (complaint == null)
        {
            return ru ? $"Обращение принято: срок {dueHours} ч." : $"Request accepted: due in {dueHours}h.";
        }

        if (complaint.Category == CityComplaintCategory.PublicConcern)
        {
            string target = FormatCityComplaintTargetName(complaint);
            return ru
                ? $"Обращение принято: снизить напряжение вокруг темы \"{target}\" за {dueHours} ч."
                : $"Request accepted: ease public concern around \"{target}\" within {dueHours}h.";
        }

        if (complaint.Category == CityComplaintCategory.ServiceMissing)
        {
            return ru
                ? $"Обращение принято: построить {FormatCityComplaintTargetName(complaint)} за {dueHours} ч."
                : $"Request accepted: build {FormatCityComplaintTargetName(complaint)} within {dueHours}h.";
        }

        return ru
            ? $"Обращение принято: решить \"{FormatCityComplaintTitle(complaint, ru)}\" за {dueHours} ч."
            : $"Request accepted: resolve \"{FormatCityComplaintTitle(complaint, ru)}\" within {dueHours}h.";
    }
}
