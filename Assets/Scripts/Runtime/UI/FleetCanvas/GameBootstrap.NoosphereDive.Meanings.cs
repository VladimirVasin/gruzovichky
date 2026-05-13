using System;
using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private void RebuildNoosphereDiveMeanings()
    {
        BuildNoosphereDiveMeaningModels(noosphereDiveMeanings);
        ApplyNoosphereDiveStats();
    }

    private void BuildNoosphereDiveMeaningModels(List<NoosphereDiveMeaningModel> target)
    {
        if (target == null)
        {
            return;
        }

        target.Clear();
        Dictionary<string, NoosphereDiveMeaningModel> meanings = new();
        bool ru = IsRussianLanguage();

        CityDailyExperience cityExperience = GetLatestCityDailyExperience();
        if (cityExperience != null)
        {
            AddNoosphereDiveMeaning(
                meanings,
                ru ? "Пережитый опыт" : "Lived experience",
                cityExperience.Score,
                cityExperience.Confidence,
                8,
                NoosphereDiveMeaningKind.CityExperience,
                canon: false,
                burned: false);
            AddNoosphereDiveMeaning(
                meanings,
                GetCityDailyExperienceFactorLabel(cityExperience.DominantKind, ru),
                cityExperience.Score,
                cityExperience.Confidence,
                5,
                NoosphereDiveMeaningKind.CityExperience,
                canon: false,
                burned: false);
        }

        AddNoosphereDiveSocialSignalMeanings(meanings, ru);

        int logLimit = Mathf.Min(noosphereKnowledgeLog.Count, 56);
        for (int i = 0; i < logLimit; i++)
        {
            NoosphereKnowledgeLogEntry entry = noosphereKnowledgeLog[i];
            string text = GetNoosphereDiveMeaningText(entry, ru);
            int score = entry.OpinionScore != 0 ? entry.OpinionScore : entry.RumorConnotationScore;
            if (score == 0)
            {
                score = entry.Positive ? 18 : -18;
            }

            bool burned = entry.EventKind == NoosphereKnowledgeEventKind.Burned;
            bool canon = entry.EventKind == NoosphereKnowledgeEventKind.Canonized || entry.IsCityCanonKnowledge;
            int confidence = Mathf.Max(entry.OpinionConfidence, entry.RumorConnotationConfidence);
            int weight = canon ? 8 : burned ? 2 : 4;
            AddNoosphereDiveMeaning(meanings, text, burned ? -Mathf.Abs(score) : score, confidence, weight, NoosphereDiveMeaningKind.Knowledge, canon, burned);
        }

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker == null || worker.HasDepartedTown || worker.IsLeavingTown)
            {
                continue;
            }

            int opinionLimit = Mathf.Min(worker.TopicOpinions.Count, 3);
            for (int j = 0; j < opinionLimit; j++)
            {
                WorkerTopicOpinion opinion = worker.TopicOpinions[j];
                string text = string.IsNullOrWhiteSpace(opinion.OriginalTopic) ? opinion.CurrentTopic : opinion.OriginalTopic;
                AddNoosphereDiveMeaning(
                    meanings,
                    text,
                    opinion.Score,
                    opinion.Confidence,
                    Mathf.Clamp(opinion.TimesHeard + 2, 2, 9),
                    NoosphereDiveMeaningKind.TopicOpinion,
                    canon: false,
                    burned: false);
            }
        }

        if (meanings.Count == 0)
        {
            AddNoosphereDiveMeaning(meanings, ru ? "ожидание" : "waiting", 0, 30, 3, NoosphereDiveMeaningKind.Knowledge, false, false);
            AddNoosphereDiveMeaning(meanings, ru ? "тишина" : "silence", 0, 30, 3, NoosphereDiveMeaningKind.Knowledge, false, false);
            AddNoosphereDiveMeaning(meanings, ru ? "город" : "city", 0, 30, 3, NoosphereDiveMeaningKind.CityExperience, false, false);
        }

        foreach (NoosphereDiveMeaningModel meaning in meanings.Values)
        {
            FinalizeNoosphereDiveMeaningOrbit(meaning);
            target.Add(meaning);
        }

        target.Sort((a, b) => b.Weight.CompareTo(a.Weight));
        while (target.Count > NoosphereDiveMaxMeanings)
        {
            target.RemoveAt(target.Count - 1);
        }
    }

    private void AddNoosphereDiveMeaning(
        Dictionary<string, NoosphereDiveMeaningModel> meanings,
        string rawText,
        int score,
        int confidence,
        int weight,
        NoosphereDiveMeaningKind kind,
        bool canon,
        bool burned)
    {
        string text = NormalizeNoosphereDiveMeaningText(rawText);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        string key = NormalizeWorkerKnowledgeTopicKey(text);
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (!meanings.TryGetValue(key, out NoosphereDiveMeaningModel meaning))
        {
            meaning = new NoosphereDiveMeaningModel
            {
                Key = key,
                Text = text,
                Kind = kind,
                Score = Mathf.Clamp(score, -100, 100),
                Confidence = Mathf.Clamp(confidence, 0, 100),
                Weight = Mathf.Max(1, weight),
                IsCanon = canon,
                IsBurned = burned
            };
            meanings.Add(key, meaning);
            return;
        }

        int totalWeight = meaning.Weight + Mathf.Max(1, weight);
        meaning.Score = Mathf.Clamp(Mathf.RoundToInt((meaning.Score * meaning.Weight + score * weight) / (float)totalWeight), -100, 100);
        meaning.Confidence = Mathf.Clamp(Mathf.Max(meaning.Confidence, confidence), 0, 100);
        meaning.Weight = Mathf.Clamp(totalWeight, 1, 99);
        meaning.IsCanon |= canon;
        meaning.IsBurned = meaning.IsBurned && burned;
        if (kind == NoosphereDiveMeaningKind.CityExperience)
        {
            meaning.Kind = kind;
        }
    }

    private static string NormalizeNoosphereDiveMeaningText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        string text = rawText.Replace('\n', ' ').Replace('\r', ' ').Trim();
        text = text.Trim('"', '\'', '«', '»');
        while (text.Contains("  ", StringComparison.Ordinal))
        {
            text = text.Replace("  ", " ", StringComparison.Ordinal);
        }

        return ShortenNoosphereDiveMeaningText(text, 34);
    }

    private static string ShortenNoosphereDiveMeaningText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
        {
            return text;
        }

        int cut = maxLength;
        for (int i = maxLength - 1; i >= 12; i--)
        {
            if (char.IsWhiteSpace(text[i]) || text[i] == ':' || text[i] == ';')
            {
                cut = i;
                break;
            }
        }

        return text.Substring(0, cut).TrimEnd('.', ',', ':', ';', ' ') + "...";
    }

    private string GetNoosphereDiveMeaningText(NoosphereKnowledgeLogEntry entry, bool ru)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        if (entry.MemoryKind == WorkerMemoryKind.BuildingExistence)
        {
            if (!string.IsNullOrWhiteSpace(entry.BuildingLabel))
            {
                return entry.BuildingLabel;
            }

            if (entry.BuildingType.HasValue)
            {
                return GetSelectedLocationDisplayName(entry.BuildingType.Value);
            }

            return ru ? "здание" : "building";
        }

        string topic = GetWorkerRumorTopic(entry);
        return string.IsNullOrWhiteSpace(topic) ? entry.Topic : topic;
    }

    private void FinalizeNoosphereDiveMeaningOrbit(NoosphereDiveMeaningModel meaning)
    {
        int hash = GetNoosphereDiveStableHash(meaning.Key);
        float importance = Mathf.Clamp01((meaning.Weight + meaning.Confidence * 0.08f) / 18f);
        meaning.Radius = Mathf.Lerp(6.8f, 2.35f, importance);
        if (meaning.IsCanon)
        {
            meaning.Radius *= 0.72f;
        }

        meaning.Height = Mathf.Lerp(-2.35f, 2.35f, Hash01(hash, 11));
        meaning.Phase = Hash01(hash, 23) * Mathf.PI * 2f;
        meaning.Speed = Mathf.Lerp(0.13f, 0.42f, Hash01(hash, 37));
        if (meaning.Score < 0)
        {
            meaning.Speed *= -1f;
        }

        meaning.Size = Mathf.Lerp(0.70f, 1.26f, importance);
        meaning.Wobble = Mathf.Lerp(0.18f, 0.78f, Hash01(hash, 51));
        meaning.Color = GetNoosphereDiveMeaningColor(meaning);
    }

    private static int GetNoosphereDiveStableHash(string text)
    {
        unchecked
        {
            int hash = 23;
            for (int i = 0; i < text.Length; i++)
            {
                hash = hash * 31 + text[i];
            }

            return hash;
        }
    }

    private static float Hash01(int hash, int salt)
    {
        unchecked
        {
            uint value = (uint)(hash * 1103515245 + salt * 12345);
            value ^= value >> 16;
            value *= 2246822519u;
            value ^= value >> 13;
            return (value & 0xFFFFFF) / 16777215f;
        }
    }

    private static Color GetNoosphereDiveMeaningColor(NoosphereDiveMeaningModel meaning)
    {
        if (meaning.IsBurned)
        {
            return new Color(1f, 0.42f, 0.24f, 0.78f);
        }

        if (meaning.IsCanon)
        {
            return new Color(0.82f, 0.95f, 1f, 0.96f);
        }

        if (meaning.Kind == NoosphereDiveMeaningKind.CityExperience)
        {
            return meaning.Score >= 0
                ? new Color(0.78f, 1f, 0.44f, 0.92f)
                : new Color(1f, 0.47f, 0.32f, 0.92f);
        }

        if (meaning.Kind == NoosphereDiveMeaningKind.SocialSignal)
        {
            return meaning.Score >= 0
                ? new Color(0.42f, 1f, 0.76f, 0.92f)
                : new Color(1f, 0.48f, 0.22f, 0.92f);
        }

        float amount = Mathf.Clamp01(Mathf.Abs(meaning.Score) / 100f);
        Color neutral = new(0.58f, 0.84f, 1f, 0.84f);
        Color signed = meaning.Score >= 0
            ? new Color(0.48f, 1f, 0.62f, 0.92f)
            : new Color(1f, 0.34f, 0.28f, 0.92f);
        return Color.Lerp(neutral, signed, amount);
    }

    private void ApplyNoosphereDiveMeaningViews()
    {
        for (int i = 0; i < noosphereDiveMeaningViews.Count; i++)
        {
            NoosphereDiveMeaningView view = noosphereDiveMeaningViews[i];
            bool active = i < noosphereDiveMeanings.Count;
            view.Root.gameObject.SetActive(active);
            if (!active)
            {
                view.Model = null;
                continue;
            }

            NoosphereDiveMeaningModel model = noosphereDiveMeanings[i];
            view.Model = model;
            view.Text.text = model.Text;
            view.Text.color = model.Color;
            view.Text.fontSize = Mathf.RoundToInt(Mathf.Lerp(36f, 54f, model.Size - 0.70f));
            view.Text.characterSize = Mathf.Lerp(0.062f, 0.086f, Mathf.Clamp01(model.Size - 0.70f));
            view.Spoke.startColor = new Color(model.Color.r, model.Color.g, model.Color.b, 0.28f);
            view.Spoke.endColor = new Color(model.Color.r, model.Color.g, model.Color.b, 0.04f);
        }
    }

    private void ApplyNoosphereDiveStats()
    {
        if (noosphereDiveUi?.TitleText == null)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        int pending = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            pending += driverAgents[i]?.PendingKnowledge.Count ?? 0;
        }

        CityDailyExperience experience = GetLatestCityDailyExperience();
        string experienceScore = experience == null ? "0" : $"{experience.Score:+#;-#;0}";
        noosphereDiveUi.TitleText.text = ru ? "Ноосфера" : "Noosphere";
        noosphereDiveUi.StatsText.text = ru
            ? $"смыслы {noosphereDiveMeanings.Count} / вечные {GetCityKnowledgeCanonMemoryCount()} / раздумья {pending} / опыт {experienceScore}"
            : $"meanings {noosphereDiveMeanings.Count} / permanent {GetCityKnowledgeCanonMemoryCount()} / forming {pending} / exp {experienceScore}";
    }
}
