using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private void BuildNoosphereVisionSnapshot()
    {
        noosphereVisionInsights.Clear();
        BuildNoosphereVisionInsights(noosphereVisionInsights);
    }

    private void BuildNoosphereVisionInsights(List<NoosphereVisionInsight> target)
    {
        if (target == null)
        {
            return;
        }

        target.Clear();
        AddNoosphereVisionEducationInsight(target, LocationType.Kindergarten);
        AddNoosphereVisionEducationInsight(target, LocationType.PrimarySchool);
        AddNoosphereVisionEducationInsight(target, LocationType.SecondarySchool);
        AddNoosphereVisionFamilyReadinessInsight(target);
        AddNoosphereVisionAffectInsight(target);
        AddNoosphereVisionCityExperienceInsight(target);
        AddNoosphereVisionSocialSignalInsights(target);
        AddNoosphereVisionCanonInsight(target);

        target.Sort((a, b) =>
        {
            int strengthCompare = b.Strength.CompareTo(a.Strength);
            if (strengthCompare != 0)
            {
                return strengthCompare;
            }

            return Mathf.Abs(b.Score).CompareTo(Mathf.Abs(a.Score));
        });

        while (target.Count > NoosphereVisionMaxInsights)
        {
            target.RemoveAt(target.Count - 1);
        }

        if (target.Count == 0)
        {
            NoosphereVisionInsight quiet = new()
            {
                Key = "quiet_city",
                TitleRu = "\u0413\u043e\u0440\u043e\u0434 \u043f\u0440\u0438\u0441\u043b\u0443\u0448\u0438\u0432\u0430\u0435\u0442\u0441\u044f",
                TitleEn = "The city is listening",
                SummaryRu = "\u0421\u0438\u043b\u044c\u043d\u044b\u0445 \u043c\u044b\u0441\u043b\u0435\u0439 \u043f\u043e\u043a\u0430 \u043d\u0435\u0442.",
                SummaryEn = "No strong shared thought has formed yet.",
                SourceRu = "\u0416\u0438\u0442\u0435\u043b\u0438 \u0435\u0449\u0435 \u043d\u0435 \u0441\u043e\u0431\u0440\u0430\u043b\u0438 \u043e\u0431\u0449\u0438\u0439 \u043e\u043f\u044b\u0442.",
                SourceEn = "Residents have not gathered enough shared experience yet.",
                EffectRu = "\u041d\u043e\u043e\u0441\u0444\u0435\u0440\u0430 \u0436\u0434\u0435\u0442 \u0441\u043e\u0431\u044b\u0442\u0438\u0439.",
                EffectEn = "The Noosphere is waiting for events.",
                ActionRu = "\u0414\u0430\u0439 \u0433\u043e\u0440\u043e\u0434\u0443 \u0434\u0435\u043d\u044c \u0436\u0438\u0437\u043d\u0438: \u0440\u0430\u0431\u043e\u0442\u0430, \u0441\u0435\u043c\u044c\u044f, \u0434\u043e\u0440\u043e\u0433\u0438.",
                ActionEn = "Let the city live: work, families, roads.",
                Tone = NoosphereVisionTone.Neutral,
                Strength = 12,
                SourceCount = 0
            };
            target.Add(quiet);
        }
    }

    private void AddNoosphereVisionEducationInsight(List<NoosphereVisionInsight> target, LocationType educationType)
    {
        int need = CountWorkerChildrenNeedingEducation(educationType);
        int capacity = GetEducationCapacity(educationType);
        int shortfall = Mathf.Max(0, need - capacity);
        if (need <= 0 || shortfall <= 0)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        string educationRu = FormatEducationLocationName(educationType, true);
        string educationEn = FormatEducationLocationName(educationType, false);
        string titleRu = educationType switch
        {
            LocationType.Kindergarten => "\u041c\u0430\u043b\u044b\u0448\u0430\u043c \u043d\u0443\u0436\u0435\u043d \u0434\u0435\u0442\u0441\u0430\u0434",
            LocationType.PrimarySchool => "\u0414\u0435\u0442\u044f\u043c \u043d\u0443\u0436\u043d\u0430 \u043d\u0430\u0447\u0430\u043b\u044c\u043d\u0430\u044f \u0448\u043a\u043e\u043b\u0430",
            LocationType.SecondarySchool => "\u041f\u043e\u0434\u0440\u043e\u0441\u0442\u043a\u0430\u043c \u043d\u0443\u0436\u043d\u0430 \u0441\u0440\u0435\u0434\u043d\u044f\u044f \u0448\u043a\u043e\u043b\u0430",
            _ => "\u0414\u0435\u0442\u044f\u043c \u043d\u0443\u0436\u043d\u044b \u043c\u0435\u0441\u0442\u0430"
        };
        string titleEn = educationType switch
        {
            LocationType.Kindergarten => "Toddlers need kindergarten",
            LocationType.PrimarySchool => "Children need primary school",
            LocationType.SecondarySchool => "Teens need secondary school",
            _ => "Children need seats"
        };

        NoosphereVisionInsight insight = new()
        {
            Key = $"education_{educationType}",
            TitleRu = titleRu,
            TitleEn = titleEn,
            SummaryRu = $"{shortfall} \u0438\u0437 {need} \u0434\u0435\u0442\u0435\u0439 \u0431\u0435\u0437 \u043c\u0435\u0441\u0442\u0430.",
            SummaryEn = $"{shortfall} of {need} children have no seat.",
            SourceRu = $"{need} \u0434\u0435\u0442\u0435\u0439 \u0436\u0434\u0443\u0442 {educationRu}; \u043c\u0435\u0441\u0442 {capacity}.",
            SourceEn = $"{need} children need {educationEn}; capacity is {capacity}.",
            EffectRu = "\u0421\u0435\u043c\u044c\u0438 \u0442\u0440\u0435\u0432\u043e\u0436\u0430\u0442\u0441\u044f, Family Readiness \u043f\u0430\u0434\u0430\u0435\u0442.",
            EffectEn = "Families worry, Family Readiness falls.",
            ActionRu = CountBuiltEducationLocations(educationType) <= 0
                ? $"\u041f\u043e\u0441\u0442\u0440\u043e\u0439 {educationRu}."
                : $"\u041d\u0430\u0437\u043d\u0430\u0447\u044c \u043f\u0435\u0440\u0441\u043e\u043d\u0430\u043b \u0432 {educationRu}.",
            ActionEn = CountBuiltEducationLocations(educationType) <= 0
                ? $"Build {educationEn}."
                : $"Assign staff to {educationEn}.",
            Tone = NoosphereVisionTone.Negative,
            Category = SocialSignalCategory.Family,
            Score = -Mathf.Clamp(shortfall * 22, 18, 100),
            Strength = Mathf.Clamp(56 + shortfall * 16, 56, 100),
            SourceCount = need
        };

        AddNoosphereVisionChildSourcePositions(insight, educationType);
        target.Add(insight);
        _ = ru;
    }

    private void AddNoosphereVisionFamilyReadinessInsight(List<NoosphereVisionInsight> target)
    {
        int blocked = 0;
        int total = 0;
        int lowest = 101;
        string lowestReason = string.Empty;
        NoosphereVisionInsight insight = new()
        {
            Key = "family_readiness",
            TitleRu = "\u0421\u0435\u043c\u044c\u0438 \u0434\u0443\u043c\u0430\u044e\u0442 \u043e \u0434\u0435\u0442\u044f\u0445",
            TitleEn = "Families are thinking about children",
            Tone = NoosphereVisionTone.Split,
            Category = SocialSignalCategory.Family
        };

        for (int i = 0; i < workerFamilies.Count; i++)
        {
            WorkerFamily family = workerFamilies[i];
            if (family == null || !IsWorkerFamilyLivingInHouse(family))
            {
                continue;
            }

            int childCount = CountWorkerFamilyChildren(family.Id);
            if (childCount >= MaxWorkerFamilyChildren)
            {
                continue;
            }

            total++;
            int readiness = CalculateWorkerFamilyNextChildReadiness(family, out string reason);
            if (readiness < WorkerFamilyNextChildReadinessThreshold)
            {
                blocked++;
                if (readiness < lowest)
                {
                    lowest = readiness;
                    lowestReason = reason;
                }
            }

            if (IsValidPersonalHouseIndex(family.HouseIndex))
            {
                insight.SourceWorldPositions.Add(GetLocationCenter(personalHouses[family.HouseIndex]));
            }
        }

        if (total <= 0 || blocked <= 0)
        {
            return;
        }

        insight.SummaryRu = $"{blocked} \u0438\u0437 {total} \u0441\u0435\u043c\u0435\u0439 \u043d\u0435 \u0433\u043e\u0442\u043e\u0432\u044b \u043a \u0441\u043b\u0435\u0434\u0443\u044e\u0449\u0435\u043c\u0443 \u0440\u0435\u0431\u0451\u043d\u043a\u0443.";
        insight.SummaryEn = $"{blocked} of {total} families are not ready for another child.";
        insight.SourceRu = string.IsNullOrWhiteSpace(lowestReason)
            ? "\u041f\u0440\u0438\u0447\u0438\u043d\u044b \u0441\u043c\u0435\u0448\u0430\u043d\u043d\u044b\u0435: \u0440\u0430\u0431\u043e\u0442\u0430, \u0434\u0435\u043d\u044c\u0433\u0438, \u0443\u0445\u043e\u0434, \u0448\u043a\u043e\u043b\u044b."
            : $"\u0421\u043b\u0430\u0431\u043e\u0435 \u043c\u0435\u0441\u0442\u043e: {lowestReason}.";
        insight.SourceEn = string.IsNullOrWhiteSpace(lowestReason)
            ? "Mixed causes: work, money, care, schools."
            : $"Weakest reason: {lowestReason}.";
        insight.EffectRu = "\u0428\u0430\u043d\u0441 \u0441\u0435\u043c\u0435\u0439\u043d\u043e\u0433\u043e \u0441\u043e\u0431\u044b\u0442\u0438\u044f \u043d\u0438\u0436\u0435, \u0440\u043e\u0434\u0438\u0442\u0435\u043b\u0438 \u043e\u0441\u0442\u043e\u0440\u043e\u0436\u043d\u0435\u0435.";
        insight.EffectEn = "Family events become less likely; parents act cautiously.";
        insight.ActionRu = "\u0423\u0441\u0438\u043b\u044c \u0441\u0442\u0430\u0431\u0438\u043b\u044c\u043d\u043e\u0441\u0442\u044c: \u0440\u0430\u0431\u043e\u0442\u0430, \u0434\u0435\u0442\u0441\u0430\u0434, \u0448\u043a\u043e\u043b\u044b, \u0434\u043e\u0445\u043e\u0434.";
        insight.ActionEn = "Improve stability: jobs, child care, schools, income.";
        insight.Score = -Mathf.Clamp(blocked * 18, 14, 100);
        insight.Strength = Mathf.Clamp(48 + blocked * 12, 48, 96);
        insight.SourceCount = blocked;
        target.Add(insight);
    }

    private void AddNoosphereVisionCityExperienceInsight(List<NoosphereVisionInsight> target)
    {
        CityDailyExperience experience = GetLatestCityDailyExperience();
        if (experience == null)
        {
            return;
        }

        NoosphereVisionInsight insight = new()
        {
            Key = $"city_experience_{experience.Day}",
            TitleRu = experience.Score >= 0 ? "\u0413\u043e\u0440\u043e\u0434 \u0434\u0435\u0440\u0436\u0438\u0442\u0441\u044f" : "\u0413\u043e\u0440\u043e\u0434 \u043d\u0430\u043f\u0440\u044f\u0436\u0451\u043d",
            TitleEn = experience.Score >= 0 ? "The city feels steady" : "The city feels strained",
            SummaryRu = experience.SummaryRu,
            SummaryEn = experience.SummaryEn,
            SourceRu = string.IsNullOrWhiteSpace(experience.MainReasonRu) ? "\u041e\u0431\u0449\u0438\u0439 \u043e\u043f\u044b\u0442 \u0436\u0438\u0442\u0435\u043b\u0435\u0439." : experience.MainReasonRu,
            SourceEn = string.IsNullOrWhiteSpace(experience.MainReasonEn) ? "Shared resident experience." : experience.MainReasonEn,
            EffectRu = $"\u041e\u0431\u0449\u0438\u0439 \u0442\u043e\u043d {experience.Score:+#;-#;0}, \u0441\u043e\u0433\u043b\u0430\u0441\u0438\u0435 {experience.Consensus}%, \u0440\u0430\u0441\u043a\u043e\u043b {experience.Tension}%.",
            EffectEn = $"Tone {experience.Score:+#;-#;0}, consensus {experience.Consensus}%, tension {experience.Tension}%.",
            ActionRu = experience.Score >= 0
                ? "\u0417\u0430\u043a\u0440\u0435\u043f\u0438 \u0443\u0441\u043f\u0435\u0445: \u0443\u0431\u0435\u0440\u0438 \u0441\u043b\u0430\u0431\u044b\u0435 \u043c\u0435\u0441\u0442\u0430, \u043f\u043e\u043a\u0430 \u0433\u043e\u0440\u043e\u0434 \u0441\u043f\u043e\u043a\u043e\u0435\u043d."
                : "\u041d\u0430\u0439\u0434\u0438 \u0441\u0430\u043c\u044b\u0439 \u0441\u0438\u043b\u044c\u043d\u044b\u0439 \u0438\u0441\u0442\u043e\u0447\u043d\u0438\u043a \u0442\u0440\u0435\u0432\u043e\u0433\u0438 \u0438 \u0437\u0430\u043a\u0440\u043e\u0439 \u0435\u0433\u043e.",
            ActionEn = experience.Score >= 0
                ? "Lock in the win: fix weak spots while the city is calm."
                : "Find the strongest source of worry and resolve it.",
            Tone = experience.Tension >= 60 ? NoosphereVisionTone.Split : experience.Score >= 0 ? NoosphereVisionTone.Positive : NoosphereVisionTone.Negative,
            Category = SocialSignalCategory.City,
            Score = experience.Score,
            Strength = Mathf.Clamp(Mathf.Abs(experience.Score) + experience.Confidence / 2 + experience.Tension / 3, 24, 94),
            SourceCount = experience.ResidentCount
        };
        AddNoosphereVisionResidentSourcePositions(insight, 10);
        target.Add(insight);
    }

    private void AddNoosphereVisionSocialSignalInsights(List<NoosphereVisionInsight> target)
    {
        int latestDay = GetLatestSocialSignalDay();
        if (latestDay <= 0)
        {
            return;
        }

        List<SocialSignalTopicAggregate> aggregates = BuildSocialSignalTopicAggregates(latestDay, publicOnly: true);
        int added = 0;
        for (int i = 0; i < aggregates.Count && added < 3; i++)
        {
            SocialSignalTopicAggregate aggregate = aggregates[i];
            if (aggregate.Count <= 0 || aggregate.Strength < 12)
            {
                continue;
            }

            int averageConfidence = Mathf.Clamp(aggregate.ConfidenceTotal / Mathf.Max(1, aggregate.Count), 0, 100);
            NoosphereVisionInsight insight = new()
            {
                Key = $"signal_{aggregate.Key}",
                TitleRu = FormatNoosphereVisionSignalTitle(aggregate, true),
                TitleEn = FormatNoosphereVisionSignalTitle(aggregate, false),
                SummaryRu = aggregate.Score >= 0
                    ? "\u042d\u0442\u0430 \u0442\u0435\u043c\u0430 \u0434\u0430\u0451\u0442 \u0433\u043e\u0440\u043e\u0434\u0443 \u043d\u0430\u0434\u0435\u0436\u0434\u0443."
                    : "\u042d\u0442\u0430 \u0442\u0435\u043c\u0430 \u0442\u044f\u043d\u0435\u0442 \u0433\u043e\u0440\u043e\u0434 \u0432 \u0442\u0440\u0435\u0432\u043e\u0433\u0443.",
                SummaryEn = aggregate.Score >= 0
                    ? "This topic gives the city hope."
                    : "This topic pulls the city into worry.",
                SourceRu = $"{aggregate.Count} \u0441\u0438\u0433\u043d\u0430\u043b\u043e\u0432, \u0441\u0438\u043b\u0430 {aggregate.Strength}, \u0443\u0432\u0435\u0440\u0435\u043d\u043d\u043e\u0441\u0442\u044c {averageConfidence}%.",
                SourceEn = $"{aggregate.Count} signals, strength {aggregate.Strength}, confidence {averageConfidence}%.",
                EffectRu = "\u0412\u043b\u0438\u044f\u0435\u0442 \u043d\u0430 \u043e\u0431\u0449\u0438\u0439 \u043e\u043f\u044b\u0442, \u0434\u043e\u0432\u0435\u0440\u0438\u0435 \u0438 \u0436\u0430\u043b\u043e\u0431\u044b.",
                EffectEn = "Feeds shared experience, trust, and complaints.",
                ActionRu = aggregate.Score >= 0
                    ? "\u0423\u0441\u0438\u043b\u044c \u0438\u0441\u0442\u043e\u0447\u043d\u0438\u043a \u0445\u043e\u0440\u043e\u0448\u0435\u0433\u043e \u0441\u0438\u0433\u043d\u0430\u043b\u0430."
                    : "\u041f\u043e\u043a\u0430\u0436\u0438 \u0438\u0441\u0442\u043e\u0447\u043d\u0438\u043a \u043d\u0430 \u043a\u0430\u0440\u0442\u0435 \u0438 \u0443\u0431\u0435\u0440\u0438 \u043f\u0440\u0438\u0447\u0438\u043d\u0443.",
                ActionEn = aggregate.Score >= 0
                    ? "Reinforce the source of this good signal."
                    : "Find the map source and remove the cause.",
                Tone = GetNoosphereVisionToneFromScore(aggregate.Score),
                Category = aggregate.Category,
                Score = Mathf.Clamp(aggregate.Score, -100, 100),
                Strength = Mathf.Clamp(aggregate.Strength, 16, 100),
                SourceCount = aggregate.Count
            };
            AddNoosphereVisionSocialSignalSourcePositions(insight, aggregate.Key, latestDay, 10);
            target.Add(insight);
            added++;
        }
    }

    private void AddNoosphereVisionAffectInsight(List<NoosphereVisionInsight> target)
    {
        int[] counts = new int[WorkerAffectCatalog.Length];
        int[] intensityTotals = new int[WorkerAffectCatalog.Length];
        DriverAgent[] sampleWorkers = new DriverAgent[WorkerAffectCatalog.Length];
        WorkerAffect[] sampleAffects = new WorkerAffect[WorkerAffectCatalog.Length];

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker == null || worker.HasDepartedTown || worker.IsLeavingTown)
            {
                continue;
            }

            UpdateWorkerAffects(worker);
            WorkerAffect affect = GetStrongestWorkerAffect(worker);
            if (affect == null || affect.Intensity < WorkerAffectStrongIntensity)
            {
                continue;
            }

            int index = GetWorkerAffectCatalogIndex(affect.Kind);
            if (index < 0)
            {
                continue;
            }

            counts[index]++;
            intensityTotals[index] += affect.Intensity;
            if (sampleAffects[index] == null || affect.Intensity > sampleAffects[index].Intensity)
            {
                sampleAffects[index] = affect;
                sampleWorkers[index] = worker;
            }
        }

        int bestIndex = -1;
        int bestStrength = 0;
        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] <= 0)
            {
                continue;
            }

            int average = intensityTotals[i] / Mathf.Max(1, counts[i]);
            int strength = counts[i] * 18 + average;
            if (strength > bestStrength)
            {
                bestStrength = strength;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
        {
            return;
        }

        WorkerAffectKind kind = WorkerAffectCatalog[bestIndex];
        WorkerAffect sample = sampleAffects[bestIndex];
        DriverAgent sampleWorker = sampleWorkers[bestIndex];
        int count = counts[bestIndex];
        int averageIntensity = intensityTotals[bestIndex] / Mathf.Max(1, count);
        WorkerThought thought = sampleWorker != null ? FindActiveWorkerThought(sampleWorker, GetWorkerAffectThoughtKey(kind)) : null;
        string thoughtRu = thought != null
            ? RenderWorkerThought(thought, true)
            : GetWorkerAffectDisplayName(kind, true);
        string thoughtEn = thought != null
            ? RenderWorkerThought(thought, false)
            : GetWorkerAffectDisplayName(kind, false);
        string reasonRu = sample?.ReasonRu;
        string reasonEn = sample?.ReasonEn;
        if (string.IsNullOrWhiteSpace(reasonRu))
        {
            reasonRu = "\u0441\u043e\u0441\u0442\u043e\u044f\u043d\u0438\u0435 \u0443\u0441\u0438\u043b\u0438\u043b\u043e\u0441\u044c";
        }

        if (string.IsNullOrWhiteSpace(reasonEn))
        {
            reasonEn = "the state grew stronger";
        }

        int scoreSign = GetWorkerAffectThoughtTone(kind) == WorkerThoughtTone.Positive ? 1 :
            GetWorkerAffectThoughtTone(kind) == WorkerThoughtTone.Negative ? -1 : 0;
        NoosphereVisionInsight insight = new()
        {
            Key = $"affect_{kind}",
            TitleRu = $"\u0421\u043e\u0441\u0442\u043e\u044f\u043d\u0438\u0435: {GetWorkerAffectDisplayName(kind, true)}",
            TitleEn = $"State: {GetWorkerAffectDisplayName(kind, false)}",
            SummaryRu = $"{count} \u0436\u0438\u0442. \u0434\u0435\u0440\u0436\u0430\u0442 \u044d\u0442\u043e \u0441\u043e\u0441\u0442\u043e\u044f\u043d\u0438\u0435, \u0441\u0440\u0435\u0434\u043d\u044f\u044f \u0441\u0438\u043b\u0430 {averageIntensity}.",
            SummaryEn = $"{count} residents carry this state, average intensity {averageIntensity}.",
            SourceRu = $"\u041f\u0440\u0438\u0447\u0438\u043d\u0430: {reasonRu}.",
            SourceEn = $"Cause: {reasonEn}.",
            EffectRu = $"\u0426\u0435\u043f\u043e\u0447\u043a\u0430: \u043f\u0440\u0438\u0447\u0438\u043d\u0430 -> \u0441\u043e\u0441\u0442\u043e\u044f\u043d\u0438\u0435 -> \u043c\u044b\u0441\u043b\u044c «{thoughtRu}» -> \u0442\u0435\u043c\u0430 {GetSocialSignalCategoryLabel(GetWorkerAffectSocialCategory(kind), true)}.",
            EffectEn = $"Chain: cause -> state -> thought \"{thoughtEn}\" -> topic {GetSocialSignalCategoryLabel(GetWorkerAffectSocialCategory(kind), false)}.",
            ActionRu = "\u0423\u0431\u0435\u0440\u0438 \u043f\u0440\u0438\u0447\u0438\u043d\u0443, \u0435\u0441\u043b\u0438 \u044d\u0442\u043e \u0442\u0440\u0435\u0432\u043e\u0433\u0430, \u0438\u043b\u0438 \u0443\u0441\u0438\u043b\u044c \u0438\u0441\u0442\u043e\u0447\u043d\u0438\u043a, \u0435\u0441\u043b\u0438 \u044d\u0442\u043e \u043d\u0430\u0434\u0435\u0436\u0434\u0430.",
            ActionEn = "Remove the cause if this is worry, or reinforce the source if this is hope.",
            Tone = GetNoosphereVisionToneFromScore(scoreSign * averageIntensity),
            Category = GetWorkerAffectSocialCategory(kind),
            Score = scoreSign * Mathf.Clamp(averageIntensity, 0, 100),
            Strength = Mathf.Clamp(bestStrength, 24, 100),
            SourceCount = count
        };

        AddNoosphereVisionAffectSourcePositions(insight, kind, 14);
        target.Add(insight);
    }

    private void AddNoosphereVisionCanonInsight(List<NoosphereVisionInsight> target)
    {
        int canonCount = GetCityKnowledgeCanonMemoryCount();
        if (canonCount <= 0)
        {
            return;
        }

        NoosphereVisionInsight insight = new()
        {
            Key = "city_canon",
            TitleRu = "\u0412 \u0433\u043e\u0440\u043e\u0434\u0435 \u0435\u0441\u0442\u044c \u043e\u0431\u0449\u0430\u044f \u043f\u0430\u043c\u044f\u0442\u044c",
            TitleEn = "The city has shared memory",
            SummaryRu = $"{canonCount} \u0437\u043d\u0430\u043d\u0438\u0439 \u0441\u0442\u0430\u043b\u0438 \u0432\u0435\u0447\u043d\u044b\u043c\u0438.",
            SummaryEn = $"{canonCount} memories became permanent.",
            SourceRu = "\u0414\u043e\u0441\u0442\u0430\u0442\u043e\u0447\u043d\u043e \u0436\u0438\u0442\u0435\u043b\u0435\u0439 \u0443\u0437\u043d\u0430\u043b\u0438 \u043e\u0434\u043d\u043e \u0438 \u0442\u043e \u0436\u0435.",
            SourceEn = "Enough residents accepted the same knowledge.",
            EffectRu = "\u042d\u0442\u043e \u0437\u043d\u0430\u043d\u0438\u0435 \u0431\u043e\u043b\u044c\u0448\u0435 \u043d\u0435 \u0441\u0433\u043e\u0440\u0438\u0442 \u0438 \u0432\u043b\u0438\u044f\u0435\u0442 \u043d\u0430 \u0432\u0441\u0435\u0445.",
            EffectEn = "This knowledge no longer expires and affects everyone.",
            ActionRu = "\u041e\u0442\u043a\u0440\u043e\u0439 F9, \u0447\u0442\u043e\u0431\u044b \u0443\u0432\u0438\u0434\u0435\u0442\u044c \u0441\u044b\u0440\u043e\u0439 \u0436\u0443\u0440\u043d\u0430\u043b.",
            ActionEn = "Open F9 to inspect the raw journal.",
            Tone = NoosphereVisionTone.Positive,
            Category = SocialSignalCategory.Knowledge,
            Score = 30,
            Strength = Mathf.Clamp(32 + canonCount * 10, 32, 78),
            SourceCount = canonCount
        };
        AddNoosphereVisionResidentSourcePositions(insight, 8);
        target.Add(insight);
    }

    private void AddNoosphereVisionChildSourcePositions(NoosphereVisionInsight insight, LocationType educationType)
    {
        for (int i = 0; i < workerChildren.Count && insight.SourceWorldPositions.Count < 14; i++)
        {
            WorkerChild child = workerChildren[i];
            if (!IsWorkerChildNeedingEducation(child, educationType))
            {
                continue;
            }

            WorkerFamily family = GetWorkerFamilyById(child.FamilyId);
            if (family != null && IsValidPersonalHouseIndex(family.HouseIndex))
            {
                insight.SourceWorldPositions.Add(GetLocationCenter(personalHouses[family.HouseIndex]));
            }
        }
    }

    private void AddNoosphereVisionResidentSourcePositions(NoosphereVisionInsight insight, int limit)
    {
        for (int i = 0; i < driverAgents.Count && insight.SourceWorldPositions.Count < limit; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker?.DriverObject != null && !worker.HasDepartedTown && !worker.IsLeavingTown)
            {
                insight.SourceWorldPositions.Add(worker.DriverObject.transform.position + Vector3.up * 0.45f);
            }
        }
    }

    private void AddNoosphereVisionSocialSignalSourcePositions(NoosphereVisionInsight insight, string topicKey, int day, int limit)
    {
        for (int i = 0; i < socialSignals.Count && insight.SourceWorldPositions.Count < limit; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null || signal.Day < day)
            {
                break;
            }

            if (signal.Day != day || !signal.PublicForNoosphere)
            {
                continue;
            }

            string key = string.IsNullOrWhiteSpace(signal.TopicKey) ? signal.Category.ToString() : signal.TopicKey;
            if (!string.Equals(key, topicKey, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (signal.HasCell)
            {
                insight.SourceWorldPositions.Add(GetCellCenter(signal.Cell) + Vector3.up * 0.28f);
            }
            else if (signal.LocationInstanceId > 0)
            {
                LocationData location = FindLocationByInstanceId(signal.LocationInstanceId);
                if (location != null)
                {
                    insight.SourceWorldPositions.Add(GetLocationCenter(location));
                }
            }
            else if (signal.LocationType.HasValue && locations.TryGetValue(signal.LocationType.Value, out LocationData typeLocation))
            {
                insight.SourceWorldPositions.Add(GetLocationCenter(typeLocation));
            }
            else
            {
                DriverAgent worker = GetDriverAgentById(signal.WorkerId);
                if (worker?.DriverObject != null)
                {
                    insight.SourceWorldPositions.Add(worker.DriverObject.transform.position + Vector3.up * 0.45f);
                }
            }
        }
    }

    private void AddNoosphereVisionAffectSourcePositions(NoosphereVisionInsight insight, WorkerAffectKind kind, int limit)
    {
        for (int i = 0; i < driverAgents.Count && insight.SourceWorldPositions.Count < limit; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker == null || worker.HasDepartedTown || worker.IsLeavingTown || !HasWorkerAffect(worker, kind))
            {
                continue;
            }

            if (TryGetNoosphereVisionResidentPosition(worker, out Vector3 position))
            {
                insight.SourceWorldPositions.Add(position + Vector3.up * 0.45f);
            }
        }
    }

    private static int GetWorkerAffectCatalogIndex(WorkerAffectKind kind)
    {
        for (int i = 0; i < WorkerAffectCatalog.Length; i++)
        {
            if (WorkerAffectCatalog[i] == kind)
            {
                return i;
            }
        }

        return -1;
    }

    private static NoosphereVisionTone GetNoosphereVisionToneFromScore(int score)
    {
        if (score >= 18)
        {
            return NoosphereVisionTone.Positive;
        }

        if (score <= -18)
        {
            return NoosphereVisionTone.Negative;
        }

        return NoosphereVisionTone.Neutral;
    }

    private Vector3 GetNoosphereVisionTargetFocus()
    {
        Vector3 total = Vector3.zero;
        int count = 0;
        for (int i = 0; i < noosphereVisionInsights.Count; i++)
        {
            NoosphereVisionInsight insight = noosphereVisionInsights[i];
            for (int j = 0; j < insight.SourceWorldPositions.Count; j++)
            {
                total += insight.SourceWorldPositions[j];
                count++;
            }
        }

        if (count <= 0)
        {
            return new Vector3(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
        }

        Vector3 average = total / count;
        average.x = Mathf.Clamp(average.x, 12f, GridWidth - 12f);
        average.y = 0f;
        average.z = Mathf.Clamp(average.z, 12f, GridHeight - 12f);
        return average;
    }

    private Vector3 GetNoosphereVisionTargetOffset()
    {
        return new Vector3(-62f, 84f, -62f);
    }
}
