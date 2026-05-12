using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private enum CityTrustFormulaEvent
    {
        PromiseCompleted,
        PromiseFailed,
        PromiseRejected,
        PositiveCitySignals,
        MassNegativeSignals
    }

    private int lastCityTrustSocialFormulaDay = -1;

    private void ApplyCityTrustPromiseCompleted(int complaintId)
    {
        ApplyCityTrustFormulaEvent(CityTrustFormulaEvent.PromiseCompleted, 100, $"citizen request #{complaintId} completed");
    }

    private void ApplyCityTrustPromiseFailed(int complaintId)
    {
        ApplyCityTrustFormulaEvent(CityTrustFormulaEvent.PromiseFailed, 100, $"citizen request #{complaintId} expired");
    }

    private int ApplyCityTrustRequestRejected(int complaintId)
    {
        return ApplyCityTrustFormulaEvent(CityTrustFormulaEvent.PromiseRejected, 100, $"citizen request #{complaintId} rejected");
    }

    private int ApplyCityTrustFormulaEvent(CityTrustFormulaEvent formulaEvent, int strength, string reason)
    {
        int delta = CalculateCityTrustFormulaDelta(formulaEvent, strength);
        ApplyCityTrustDelta(delta, $"formula:{formulaEvent}; {reason}");
        RecordCityTrustFormulaSocialSignal(formulaEvent, delta, reason);
        return delta;
    }

    private int CalculateCityTrustFormulaDelta(CityTrustFormulaEvent formulaEvent, int strength)
    {
        int clampedStrength = Mathf.Clamp(strength, 0, 500);
        return formulaEvent switch
        {
            CityTrustFormulaEvent.PromiseCompleted => CityTrustCitizenRequestCompletedReward,
            CityTrustFormulaEvent.PromiseFailed => GetCityTrustComplaintExpiredPenalty(),
            CityTrustFormulaEvent.PromiseRejected => GetCityTrustCitizenRequestRejectedPenalty(),
            CityTrustFormulaEvent.PositiveCitySignals => Mathf.Clamp(Mathf.RoundToInt(clampedStrength / 70f), 0, 8),
            CityTrustFormulaEvent.MassNegativeSignals => -Mathf.Clamp(Mathf.RoundToInt(clampedStrength / 90f), 0, 10),
            _ => 0
        };
    }

    private void ApplyDailyCityTrustFormulaFromSocialSignals(int endedDay)
    {
        if (endedDay <= 0 || lastCityTrustSocialFormulaDay >= endedDay)
        {
            return;
        }

        lastCityTrustSocialFormulaDay = endedDay;
        int positiveStrength = 0;
        int negativeStrength = 0;
        int positiveCount = 0;
        int negativeCount = 0;
        HashSet<int> negativeWorkers = new();
        for (int i = 0; i < socialSignals.Count; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null)
            {
                continue;
            }

            if (signal.Day < endedDay)
            {
                break;
            }

            if (signal.Day != endedDay ||
                !signal.PublicForNoosphere ||
                signal.SourceKind is SocialSignalSourceKind.CityTrust or SocialSignalSourceKind.CityHallDecision)
            {
                continue;
            }

            int strength = signal.DailyScoreHint != 0
                ? Mathf.Abs(signal.DailyScoreHint)
                : Mathf.Clamp(signal.Strength, 1, 100);
            if (signal.Tone == SocialSignalTone.Positive && IsCityTrustPositiveSignal(signal))
            {
                positiveStrength += strength;
                positiveCount++;
            }
            else if (signal.Tone == SocialSignalTone.Negative)
            {
                negativeStrength += strength;
                negativeCount++;
                if (signal.WorkerId > 0)
                {
                    negativeWorkers.Add(signal.WorkerId);
                }
            }
        }

        int activeResidents = CountActiveCityKnowledgeResidents();
        bool massNegative = negativeCount >= 4 || negativeWorkers.Count >= Mathf.Max(2, Mathf.CeilToInt(activeResidents * 0.22f));
        int positiveDelta = positiveStrength >= 40
            ? CalculateCityTrustFormulaDelta(CityTrustFormulaEvent.PositiveCitySignals, positiveStrength)
            : 0;
        int negativeDelta = massNegative
            ? CalculateCityTrustFormulaDelta(CityTrustFormulaEvent.MassNegativeSignals, negativeStrength)
            : 0;
        int delta = Mathf.Clamp(positiveDelta + negativeDelta, -10, 8);
        if (delta == 0)
        {
            return;
        }

        string reason = $"daily signals day {endedDay}: positive={positiveCount}/{positiveStrength}, negative={negativeCount}/{negativeStrength}, residents={negativeWorkers.Count}";
        ApplyCityTrustDelta(delta, $"formula:daily social signals; {reason}");
        RecordCityTrustFormulaSocialSignal(
            delta > 0 ? CityTrustFormulaEvent.PositiveCitySignals : CityTrustFormulaEvent.MassNegativeSignals,
            delta,
            reason);
    }

    private static bool IsCityTrustPositiveSignal(SocialSignal signal)
    {
        return signal != null &&
               signal.Category is SocialSignalCategory.City or
                   SocialSignalCategory.Governance or
                   SocialSignalCategory.Housing or
                   SocialSignalCategory.Transport or
                   SocialSignalCategory.Need;
    }

    private void RecordCityTrustFormulaSocialSignal(CityTrustFormulaEvent formulaEvent, int delta, string reason)
    {
        if (delta == 0)
        {
            return;
        }

        RecordSocialSignal(
            null,
            SocialSignalCategory.Governance,
            SocialSignalSourceKind.CityTrust,
            delta > 0 ? SocialSignalTone.Positive : SocialSignalTone.Negative,
            Mathf.Clamp(Mathf.Abs(delta) * 4, 8, 100),
            90,
            "city_trust",
            "\u0434\u043e\u0432\u0435\u0440\u0438\u0435 \u0433\u043e\u0440\u043e\u0434\u0430",
            "city trust",
            FormatCityTrustFormulaReason(formulaEvent, delta, true),
            FormatCityTrustFormulaReason(formulaEvent, delta, false),
            sourceKey: $"trust:{formulaEvent}:{currentDay}:{reason}",
            locationType: LocationType.CityHall,
            includeInDailyExperience: false,
            publicForNoosphere: true,
            dedupeHours: 0f);
    }

    private static string FormatCityTrustFormulaReason(CityTrustFormulaEvent formulaEvent, int delta, bool ru)
    {
        return formulaEvent switch
        {
            CityTrustFormulaEvent.PromiseCompleted => ru
                ? $"\u0433\u043e\u0440\u043e\u0434 \u0432\u044b\u043f\u043e\u043b\u043d\u0438\u043b \u043e\u0431\u0435\u0449\u0430\u043d\u0438\u0435, \u0434\u043e\u0432\u0435\u0440\u0438\u0435 {delta:+#;-#;0}"
                : $"the city kept a promise, trust {delta:+#;-#;0}",
            CityTrustFormulaEvent.PromiseFailed => ru
                ? $"\u0433\u043e\u0440\u043e\u0434 \u043f\u0440\u043e\u0432\u0430\u043b\u0438\u043b \u043e\u0431\u0435\u0449\u0430\u043d\u0438\u0435, \u0434\u043e\u0432\u0435\u0440\u0438\u0435 {delta:+#;-#;0}"
                : $"the city failed a promise, trust {delta:+#;-#;0}",
            CityTrustFormulaEvent.PromiseRejected => ru
                ? $"\u0433\u043e\u0440\u043e\u0434 \u043e\u0442\u043a\u0430\u0437\u0430\u043b \u0432 \u043e\u0431\u0440\u0430\u0449\u0435\u043d\u0438\u0438, \u0434\u043e\u0432\u0435\u0440\u0438\u0435 {delta:+#;-#;0}"
                : $"the city rejected a request, trust {delta:+#;-#;0}",
            CityTrustFormulaEvent.PositiveCitySignals => ru
                ? $"\u043f\u043e\u0437\u0438\u0442\u0438\u0432\u043d\u044b\u0435 \u0433\u043e\u0440\u043e\u0434\u0441\u043a\u0438\u0435 \u0441\u0438\u0433\u043d\u0430\u043b\u044b, \u0434\u043e\u0432\u0435\u0440\u0438\u0435 {delta:+#;-#;0}"
                : $"positive city signals, trust {delta:+#;-#;0}",
            _ => ru
                ? $"\u043c\u0430\u0441\u0441\u043e\u0432\u044b\u0439 \u043d\u0435\u0433\u0430\u0442\u0438\u0432, \u0434\u043e\u0432\u0435\u0440\u0438\u0435 {delta:+#;-#;0}"
                : $"mass negative signals, trust {delta:+#;-#;0}"
        };
    }
}
