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
        CityComplaint top = GetHighestPriorityOpenCityComplaint();
        string topLine = top != null
            ? FormatCityComplaintTitle(top, ru)
            : (ru ? "Нет активных обращений" : "No active requests");

        return FormatValueLine(ru ? "Доверие" : "Trust", $"{cityTrust}/{CityTrustMax}") + "\n" +
               FormatValueLine(ru ? "Активно" : "Active", CountOpenCityComplaints().ToString()) + "\n" +
               FormatValueLine(ru ? "Срочных" : "Urgent", CountCriticalCityComplaints().ToString()) + "\n" +
               FormatValueLine(ru ? "Просрочено" : "Expired", CountExpiredCityComplaints().ToString()) + "\n" +
               FormatValueLine(ru ? "Выполнено сегодня" : "Resolved today", CountResolvedCityComplaintsToday().ToString()) + "\n" +
               FormatValueLine(ru ? "Главное" : "Top request", topLine);
    }

    private string FormatCityComplaintTargetName(CityComplaint complaint)
    {
        if (complaint != null && complaint.Category == CityComplaintCategory.SocialIntroduction)
        {
            return string.IsNullOrWhiteSpace(complaint.SocialTargetWorkerName)
                ? (IsRussianLanguage() ? "житель" : "resident")
                : complaint.SocialTargetWorkerName;
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
            CityComplaintCategory.FamilyStress => ru ? "Низкое довольство и критические нужды давят на семьи подписавших жителей." : "Low satisfaction and critical needs are stressing signed families.",
            CityComplaintCategory.SocialIntroduction => ru
                ? $"{complaint.WorkerName} хочет поговорить с {FormatCityComplaintTargetName(complaint)}, но разговор, как и всякое городское дело, требует бумажки, печати и человека, который скажет первую странную фразу.\n\nЕсли принять обращение, Ратуша попросит вас подсказать тему."
                : $"{complaint.WorkerName} wants to talk to {FormatCityComplaintTargetName(complaint)} and needs a topic. Accepting will open the conversation scene.",
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
            _ => "Городу нужно новое здание. Не героическое, не судьбоносное, просто то самое, отсутствие которого начинает скрипеть в каждом дне."
        };
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
            "social introduction completed" => ru ? "разговор состоялся" : "conversation happened",
            "social participant unavailable" => ru ? "участник недоступен" : "participant unavailable",
            _ => string.IsNullOrWhiteSpace(reason) ? (ru ? "без заметки" : "no note") : reason
        };
    }
}
