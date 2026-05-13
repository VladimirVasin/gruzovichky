using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private void RecordNoosphereDayStartSnapshot(NoosphereDayStartSnapshotTrigger trigger)
    {
        if (currentDay <= 0 || noosphereDayStartSnapshotDays.Contains(currentDay))
        {
            return;
        }

        float now = GetCurrentWorldHour();
        NoosphereDayStartSnapshot snapshot = BuildNoosphereDayStartSnapshot(currentDay, now, trigger);
        noosphereDayStartSnapshots.Insert(0, snapshot);
        noosphereDayStartSnapshotDays.Add(snapshot.Day);
        while (noosphereDayStartSnapshots.Count > NoosphereDayStartSnapshotHistoryCap)
        {
            NoosphereDayStartSnapshot removed = noosphereDayStartSnapshots[noosphereDayStartSnapshots.Count - 1];
            noosphereDayStartSnapshotDays.Remove(removed.Day);
            noosphereDayStartSnapshots.RemoveAt(noosphereDayStartSnapshots.Count - 1);
        }
    }

    private NoosphereDayStartSnapshot BuildNoosphereDayStartSnapshot(
        int day,
        float now,
        NoosphereDayStartSnapshotTrigger trigger)
    {
        NoosphereDayStartSnapshot snapshot = new()
        {
            Day = day,
            WorldHour = now,
            ClockLabel = GetDayNightClockLabel(),
            Trigger = trigger,
            ActiveResidentCount = CountActiveNoosphereResidents(),
            ActiveKnowledgeCount = CountActiveNoosphereKnowledge(now),
            PendingKnowledgeCount = CountPendingNoosphereKnowledge(),
            KnowledgeEventCount = noosphereKnowledgeLog.Count,
            CityCanonCount = GetCityKnowledgeCanonMemoryCount(),
            PublicSocialSignalCount = CountPublicNoosphereSocialSignals(),
            CityExperienceCount = cityDailyExperiences.Count,
            ConversationTopicCount = conversationTopics.Count
        };

        CopyNoosphereKnowledgeEvents(snapshot);
        CopyNoosphereSocialSignals(snapshot);
        CopyNoosphereSocialInsights(snapshot);
        CopyNoosphereCityExperiences(snapshot);
        CopyNoosphereCityCanon(snapshot);
        CopyNoosphereConversationTopics(snapshot);
        CopyNoosphereWorkerLayers(snapshot, now);
        CopyNoosphereDiveMeanings(snapshot);
        CopyNoosphereVisionInsights(snapshot);
        CopyNoosphereVisualNodes(snapshot, now);
        return snapshot;
    }

    private int CountActiveNoosphereResidents()
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker != null && !worker.HasDepartedTown && !worker.IsLeavingTown)
            {
                count++;
            }
        }

        return count;
    }

    private int CountPendingNoosphereKnowledge()
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            count += driverAgents[i]?.PendingKnowledge.Count ?? 0;
        }

        return count;
    }

    private void CopyNoosphereKnowledgeEvents(NoosphereDayStartSnapshot snapshot)
    {
        for (int i = 0; i < noosphereKnowledgeLog.Count; i++)
        {
            NoosphereKnowledgeLogEntry entry = noosphereKnowledgeLog[i];
            if (entry == null)
            {
                continue;
            }

            snapshot.KnowledgeEvents.Add(new NoosphereKnowledgeEventSnapshot
            {
                EventKind = entry.EventKind,
                CognitionKind = entry.CognitionKind,
                MemoryKind = entry.MemoryKind,
                ConversationTopicKey = entry.ConversationTopicKey,
                OwnerWorkerId = entry.OwnerWorkerId,
                OtherWorkerId = entry.OtherWorkerId,
                OwnerName = entry.OwnerName,
                OtherName = entry.OtherName,
                Topic = entry.Topic,
                BuildingType = entry.BuildingType,
                BuildingInstanceId = entry.BuildingInstanceId,
                BuildingLabel = entry.BuildingLabel,
                Positive = entry.Positive,
                ReasonRu = entry.ReasonRu,
                ReasonEn = entry.ReasonEn,
                KnowledgeIteration = entry.KnowledgeIteration,
                SourceAttitude = entry.SourceAttitude,
                RumorRootId = entry.RumorRootId,
                OriginalTopic = entry.OriginalTopic,
                RumorTopic = entry.RumorTopic,
                RumorDistortionPercent = entry.RumorDistortionPercent,
                RumorConnotationScore = entry.RumorConnotationScore,
                RumorConnotationConfidence = entry.RumorConnotationConfidence,
                OpinionTone = entry.OpinionTone,
                OpinionScore = entry.OpinionScore,
                OpinionConfidence = entry.OpinionConfidence,
                OpinionReasonRu = entry.OpinionReasonRu,
                OpinionReasonEn = entry.OpinionReasonEn,
                IsCityCanonKnowledge = entry.IsCityCanonKnowledge,
                CityCanonAdoptionCount = entry.CityCanonAdoptionCount,
                CityCanonAdoptionRequired = entry.CityCanonAdoptionRequired,
                EventDay = entry.EventDay,
                EventWorldHour = entry.EventWorldHour,
                MemoryCreatedWorldHour = entry.MemoryCreatedWorldHour,
                MemoryExpiresWorldHour = entry.MemoryExpiresWorldHour
            });
        }
    }

    private void CopyNoosphereSocialSignals(NoosphereDayStartSnapshot snapshot)
    {
        for (int i = 0; i < socialSignals.Count; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null)
            {
                continue;
            }

            snapshot.SocialSignals.Add(new NoosphereSocialSignalSnapshot
            {
                CognitionKind = signal.CognitionKind,
                Id = signal.Id,
                WorkerId = signal.WorkerId,
                WorkerName = signal.WorkerName,
                Day = signal.Day,
                WorldHour = signal.WorldHour,
                TopicKey = signal.TopicKey,
                TopicLabelRu = signal.TopicLabelRu,
                TopicLabelEn = signal.TopicLabelEn,
                Tone = signal.Tone,
                Strength = signal.Strength,
                Confidence = signal.Confidence,
                Category = signal.Category,
                SourceKind = signal.SourceKind,
                SourceKey = signal.SourceKey,
                LocationType = signal.LocationType,
                LocationInstanceId = signal.LocationInstanceId,
                HasCell = signal.HasCell,
                Cell = signal.Cell,
                ReasonRu = signal.ReasonRu,
                ReasonEn = signal.ReasonEn,
                DailyScoreHint = signal.DailyScoreHint,
                IncludeInDailyExperience = signal.IncludeInDailyExperience,
                PublicForNoosphere = signal.PublicForNoosphere
            });
        }
    }

    private void CopyNoosphereSocialInsights(NoosphereDayStartSnapshot snapshot)
    {
        int latestDay = GetLatestSocialSignalDay();
        if (latestDay <= 0)
        {
            return;
        }

        NoosphereSocialSignalInsight insight = BuildNoosphereSocialSignalInsight(latestDay);
        NoosphereSocialInsightSnapshot copy = new()
        {
            Day = insight.Day,
            Count = insight.Count,
            PositiveCount = insight.PositiveCount,
            NegativeCount = insight.NegativeCount,
            NeutralCount = insight.NeutralCount,
            Score = insight.Score,
            Strength = insight.Strength
        };

        for (int i = 0; i < insight.Topics.Count; i++)
        {
            NoosphereSocialSignalTopicInsight topic = insight.Topics[i];
            copy.Topics.Add(new NoosphereSocialTopicSnapshot
            {
                Key = topic.Key,
                LabelRu = topic.LabelRu,
                LabelEn = topic.LabelEn,
                Category = topic.Category,
                Count = topic.Count,
                PositiveCount = topic.PositiveCount,
                NegativeCount = topic.NegativeCount,
                NeutralCount = topic.NeutralCount,
                Score = topic.Score,
                Strength = topic.Strength,
                ConfidenceTotal = topic.ConfidenceTotal
            });
        }

        for (int i = 0; i < insight.Reasons.Count; i++)
        {
            NoosphereSocialSignalReasonInsight reason = insight.Reasons[i];
            copy.Reasons.Add(new NoosphereSocialReasonSnapshot
            {
                Key = reason.Key,
                TextRu = reason.TextRu,
                TextEn = reason.TextEn,
                Count = reason.Count,
                Strength = reason.Strength,
                Score = reason.Score
            });
        }

        snapshot.SocialInsights.Add(copy);
    }

    private void CopyNoosphereCityExperiences(NoosphereDayStartSnapshot snapshot)
    {
        for (int i = 0; i < cityDailyExperiences.Count; i++)
        {
            CityDailyExperience experience = cityDailyExperiences[i];
            if (experience == null)
            {
                continue;
            }

            NoosphereCityExperienceSnapshot copy = new()
            {
                Day = experience.Day,
                FinalTone = experience.FinalTone,
                Score = experience.Score,
                Confidence = experience.Confidence,
                Consensus = experience.Consensus,
                Tension = experience.Tension,
                ResidentCount = experience.ResidentCount,
                PositiveResidentCount = experience.PositiveResidentCount,
                NegativeResidentCount = experience.NegativeResidentCount,
                DominantKind = experience.DominantKind,
                SummaryRu = experience.SummaryRu,
                SummaryEn = experience.SummaryEn,
                MainReasonRu = experience.MainReasonRu,
                MainReasonEn = experience.MainReasonEn,
                CounterpointRu = experience.CounterpointRu,
                CounterpointEn = experience.CounterpointEn,
                CreatedWorldHour = experience.CreatedWorldHour
            };

            for (int j = 0; j < experience.Factors.Count; j++)
            {
                CityDailyExperienceFactor factor = experience.Factors[j];
                copy.Factors.Add(new NoosphereCityExperienceFactorSnapshot
                {
                    Kind = factor.Kind,
                    Score = factor.Score,
                    ResidentCount = factor.ResidentCount,
                    PositiveCount = factor.PositiveCount,
                    NegativeCount = factor.NegativeCount,
                    RepresentativeScore = factor.RepresentativeScore,
                    RepresentativeReasonRu = factor.RepresentativeReasonRu,
                    RepresentativeReasonEn = factor.RepresentativeReasonEn
                });
            }

            snapshot.CityExperiences.Add(copy);
        }
    }

    private void CopyNoosphereCityCanon(NoosphereDayStartSnapshot snapshot)
    {
        for (int i = 0; i < cityKnowledgeCanon.Count; i++)
        {
            CityKnowledgeCanonEntry entry = cityKnowledgeCanon[i];
            if (entry == null)
            {
                continue;
            }

            snapshot.CityCanon.Add(new NoosphereCityCanonSnapshot
            {
                CognitionKind = entry.CognitionKind,
                Kind = entry.Kind,
                ConversationTopicKey = entry.ConversationTopicKey,
                OtherWorkerId = entry.OtherWorkerId,
                Topic = entry.Topic,
                BuildingType = entry.BuildingType,
                BuildingInstanceId = entry.BuildingInstanceId,
                BuildingLabel = entry.BuildingLabel,
                Positive = entry.Positive,
                KnowledgeIteration = entry.KnowledgeIteration,
                SourceAttitude = entry.SourceAttitude,
                RumorRootId = entry.RumorRootId,
                OriginalTopic = entry.OriginalTopic,
                RumorTopic = entry.RumorTopic,
                RumorDistortionPercent = entry.RumorDistortionPercent,
                RumorConnotationScore = entry.RumorConnotationScore,
                RumorConnotationConfidence = entry.RumorConnotationConfidence,
                OpinionTone = entry.OpinionTone,
                OpinionScore = entry.OpinionScore,
                OpinionConfidence = entry.OpinionConfidence,
                OpinionReasonRu = entry.OpinionReasonRu,
                OpinionReasonEn = entry.OpinionReasonEn,
                SourceWorkerId = entry.SourceWorkerId,
                AdoptionCount = entry.AdoptionCount,
                AdoptionRequired = entry.AdoptionRequired,
                CanonizedDay = entry.CanonizedDay,
                CanonizedWorldHour = entry.CanonizedWorldHour
            });
        }
    }

    private void CopyNoosphereConversationTopics(NoosphereDayStartSnapshot snapshot)
    {
        for (int i = 0; i < conversationTopics.Count; i++)
        {
            ConversationTopic topic = conversationTopics[i];
            if (topic == null)
            {
                continue;
            }

            snapshot.ConversationTopics.Add(new NoosphereConversationTopicSnapshot
            {
                Key = topic.Key,
                OriginalText = topic.OriginalText,
                DisplayText = topic.DisplayText,
                FirstDay = topic.FirstDay,
                FirstWorldHour = topic.FirstWorldHour,
                LastMentionedDay = topic.LastMentionedDay,
                LastMentionedWorldHour = topic.LastMentionedWorldHour,
                MentionCount = topic.MentionCount,
                PositiveConversationCount = topic.PositiveConversationCount,
                NegativeConversationCount = topic.NegativeConversationCount,
                PositiveOpinionCount = topic.PositiveOpinionCount,
                NegativeOpinionCount = topic.NegativeOpinionCount
            });
        }
    }

    private void CopyNoosphereWorkerLayers(NoosphereDayStartSnapshot snapshot, float now)
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker == null || worker.HasDepartedTown || worker.IsLeavingTown)
            {
                continue;
            }

            UpdateWorkerAffects(worker);
            NoosphereWorkerLayerSnapshot copy = new()
            {
                WorkerId = worker.DriverId,
                CitizenId = worker.CitizenId,
                WorkerName = worker.DriverName,
                CitizenProfession = worker.CitizenProfession,
                Education = worker.Education,
                Weakness = worker.Weakness,
                Satisfaction = worker.Satisfaction,
                Money = worker.Money,
                IsInsideBuilding = worker.IsInsideBuilding,
                InsideBuildingType = worker.InsideBuildingType,
                InsideBuildingInstanceId = worker.InsideBuildingInstanceId,
                FamilyId = worker.FamilyId,
                SocialMemoryCount = worker.SocialMemories.Count,
                ActiveThoughtCount = CountActiveNoosphereThoughts(worker),
                ActiveAffectCount = CountActiveWorkerAffects(worker),
                MemoryCount = CountActiveNoosphereMemories(worker, now),
                PendingKnowledgeCount = worker.PendingKnowledge.Count,
                TopicOpinionCount = worker.TopicOpinions.Count,
                DailyOpinionCount = worker.DailyOpinions.Count
            };

            CopyNoosphereWorkerTraits(worker, copy);
            CopyNoosphereWorkerThoughts(worker, copy);
            CopyNoosphereWorkerAffects(worker, copy, now);
            CopyNoosphereWorkerPendingThoughts(worker, copy);
            CopyNoosphereWorkerPendingKnowledge(worker, copy);
            CopyNoosphereWorkerMemories(worker, copy, now);
            CopyNoosphereWorkerTopicOpinions(worker, copy);
            CopyNoosphereWorkerDailyOpinions(worker, copy);
            snapshot.Workers.Add(copy);
        }
    }

    private int CountActiveNoosphereThoughts(DriverAgent worker)
    {
        int count = 0;
        for (int i = 0; i < worker.Thoughts.Count; i++)
        {
            if (worker.Thoughts[i]?.Active == true)
            {
                count++;
            }
        }

        return count;
    }

    private int CountActiveNoosphereMemories(DriverAgent worker, float now)
    {
        int count = 0;
        for (int i = 0; i < worker.Memories.Count; i++)
        {
            WorkerMemory memory = worker.Memories[i];
            if (IsWorkerMemoryDisplayable(memory) && !ShouldExpireWorkerMemory(memory, now))
            {
                count++;
            }
        }

        return count;
    }

    private void CopyNoosphereWorkerAffects(DriverAgent worker, NoosphereWorkerLayerSnapshot copy, float now)
    {
        for (int i = 0; i < worker.Affects.Count; i++)
        {
            WorkerAffect affect = worker.Affects[i];
            if (affect == null || affect.ExpiresWorldHour > 0f && now >= affect.ExpiresWorldHour)
            {
                continue;
            }

            copy.Affects.Add(new NoosphereWorkerAffectSnapshot
            {
                Kind = affect.Kind,
                Intensity = affect.Intensity,
                StartedDay = affect.StartedDay,
                StartedWorldHour = affect.StartedWorldHour,
                ExpiresWorldHour = affect.ExpiresWorldHour,
                SourceLocationType = affect.SourceLocationType,
                SourceInstanceId = affect.SourceInstanceId,
                SourceKey = affect.SourceKey ?? string.Empty,
                ReasonRu = affect.ReasonRu ?? string.Empty,
                ReasonEn = affect.ReasonEn ?? string.Empty
            });
        }
    }

    private static void CopyNoosphereWorkerTraits(DriverAgent worker, NoosphereWorkerLayerSnapshot copy)
    {
        for (int i = 0; i < worker.Traits.Count; i++)
        {
            copy.Traits.Add(worker.Traits[i]);
        }
    }

    private void CopyNoosphereWorkerThoughts(DriverAgent worker, NoosphereWorkerLayerSnapshot copy)
    {
        for (int i = 0; i < worker.Thoughts.Count; i++)
        {
            WorkerThought thought = worker.Thoughts[i];
            if (thought == null)
            {
                continue;
            }

            copy.Thoughts.Add(new NoosphereWorkerThoughtSnapshot
            {
                Key = thought.Key ?? string.Empty,
                Kind = thought.Kind,
                Tone = thought.Tone,
                Priority = thought.Priority,
                Intensity = thought.Intensity,
                TemplateKey = thought.TemplateKey ?? string.Empty,
                CreatedDay = thought.CreatedDay,
                CreatedWorldHour = thought.CreatedWorldHour,
                Active = thought.Active,
                ExpiresWorldHour = thought.ExpiresWorldHour
            });
        }
    }

    private void CopyNoosphereWorkerPendingThoughts(DriverAgent worker, NoosphereWorkerLayerSnapshot copy)
    {
        for (int i = 0; i < worker.PendingThoughts.Count; i++)
        {
            PendingWorkerThought thought = worker.PendingThoughts[i];
            if (thought == null)
            {
                continue;
            }

            copy.PendingThoughts.Add(new NoospherePendingThoughtSnapshot
            {
                FormationKey = thought.FormationKey,
                ThoughtKey = thought.ThoughtKey,
                Kind = thought.Kind,
                Tone = thought.Tone,
                Priority = thought.Priority,
                Intensity = thought.Intensity,
                TemplateKey = thought.TemplateKey,
                StartedDay = thought.StartedDay,
                StartedWorldHour = thought.StartedWorldHour,
                ReadyWorldHour = thought.ReadyWorldHour,
                FormationReason = thought.FormationReason
            });
        }
    }

    private void CopyNoosphereWorkerPendingKnowledge(DriverAgent worker, NoosphereWorkerLayerSnapshot copy)
    {
        for (int i = 0; i < worker.PendingKnowledge.Count; i++)
        {
            PendingWorkerKnowledge knowledge = worker.PendingKnowledge[i];
            if (knowledge == null)
            {
                continue;
            }

            copy.PendingKnowledge.Add(new NoospherePendingKnowledgeSnapshot
            {
                FormationKey = knowledge.FormationKey,
                CognitionKind = knowledge.CognitionKind,
                Kind = knowledge.Kind,
                ConversationTopicKey = knowledge.ConversationTopicKey,
                OtherWorkerId = knowledge.OtherWorkerId,
                Topic = knowledge.Topic,
                BuildingType = knowledge.BuildingType,
                BuildingInstanceId = knowledge.BuildingInstanceId,
                BuildingLabel = knowledge.BuildingLabel,
                SourceRu = knowledge.SourceRu,
                SourceEn = knowledge.SourceEn,
                Positive = knowledge.Positive,
                Stage = knowledge.Stage,
                StartedDay = knowledge.StartedDay,
                StartedWorldHour = knowledge.StartedWorldHour,
                NextStageWorldHour = knowledge.NextStageWorldHour,
                OpinionTone = knowledge.OpinionTone,
                OpinionScore = knowledge.OpinionScore,
                OpinionConfidence = knowledge.OpinionConfidence,
                OpinionReasonRu = knowledge.OpinionReasonRu,
                OpinionReasonEn = knowledge.OpinionReasonEn
            });
        }
    }

    private void CopyNoosphereWorkerMemories(DriverAgent worker, NoosphereWorkerLayerSnapshot copy, float now)
    {
        for (int i = 0; i < worker.Memories.Count; i++)
        {
            WorkerMemory memory = worker.Memories[i];
            if (!IsWorkerMemoryDisplayable(memory) || ShouldExpireWorkerMemory(memory, now))
            {
                continue;
            }

            copy.Memories.Add(new NoosphereWorkerMemorySnapshot
            {
                CognitionKind = memory.CognitionKind,
                Kind = memory.Kind,
                ConversationTopicKey = memory.ConversationTopicKey,
                OtherWorkerId = memory.OtherWorkerId,
                Topic = memory.Topic,
                BuildingType = memory.BuildingType,
                BuildingInstanceId = memory.BuildingInstanceId,
                BuildingLabel = memory.BuildingLabel,
                SourceRu = memory.SourceRu,
                SourceEn = memory.SourceEn,
                Positive = memory.Positive,
                KnowledgeIteration = memory.KnowledgeIteration,
                RumorRootId = memory.RumorRootId,
                OriginalTopic = memory.OriginalTopic,
                RumorTopic = memory.RumorTopic,
                OpinionTone = memory.OpinionTone,
                OpinionScore = memory.OpinionScore,
                OpinionConfidence = memory.OpinionConfidence,
                IsCityCanonKnowledge = memory.IsCityCanonKnowledge,
                CityCanonAdoptionCount = memory.CityCanonAdoptionCount,
                CityCanonAdoptionRequired = memory.CityCanonAdoptionRequired,
                CreatedDay = memory.CreatedDay,
                CreatedWorldHour = memory.CreatedWorldHour,
                ExpiresWorldHour = memory.ExpiresWorldHour
            });
        }
    }

    private void CopyNoosphereWorkerTopicOpinions(DriverAgent worker, NoosphereWorkerLayerSnapshot copy)
    {
        for (int i = 0; i < worker.TopicOpinions.Count; i++)
        {
            WorkerTopicOpinion opinion = worker.TopicOpinions[i];
            if (opinion == null)
            {
                continue;
            }

            copy.TopicOpinions.Add(new NoosphereWorkerTopicOpinionSnapshot
            {
                TopicKey = opinion.TopicKey,
                ConversationTopicKey = opinion.ConversationTopicKey,
                RumorRootId = opinion.RumorRootId,
                OriginalTopic = opinion.OriginalTopic,
                CurrentTopic = opinion.CurrentTopic,
                Tone = opinion.Tone,
                Score = opinion.Score,
                Confidence = opinion.Confidence,
                ReasonRu = opinion.ReasonRu,
                ReasonEn = opinion.ReasonEn,
                TimesHeard = opinion.TimesHeard,
                PositiveSignalCount = opinion.PositiveSignalCount,
                NegativeSignalCount = opinion.NegativeSignalCount,
                ContradictionCount = opinion.ContradictionCount,
                LastUpdatedDay = opinion.LastUpdatedDay,
                LastUpdatedWorldHour = opinion.LastUpdatedWorldHour
            });
        }
    }

    private void CopyNoosphereWorkerDailyOpinions(DriverAgent worker, NoosphereWorkerLayerSnapshot copy)
    {
        for (int i = 0; i < worker.DailyOpinions.Count; i++)
        {
            WorkerDailyOpinion opinion = worker.DailyOpinions[i];
            if (opinion == null)
            {
                continue;
            }

            NoosphereWorkerDailyOpinionSnapshot opinionCopy = new()
            {
                Day = opinion.Day,
                FinalTone = opinion.FinalTone,
                Score = opinion.Score,
                Confidence = opinion.Confidence,
                SummaryRu = opinion.SummaryRu,
                SummaryEn = opinion.SummaryEn,
                MainReasonRu = opinion.MainReasonRu,
                MainReasonEn = opinion.MainReasonEn,
                PositiveThoughtCount = opinion.PositiveThoughtCount,
                NegativeThoughtCount = opinion.NegativeThoughtCount,
                CriticalActiveThoughtCount = opinion.CriticalActiveThoughtCount,
                EmittedSocialSignalCount = opinion.EmittedSocialSignalCount,
                DominantKind = opinion.DominantKind
            };

            for (int j = 0; j < opinion.Factors.Count; j++)
            {
                WorkerDailyOpinionFactor factor = opinion.Factors[j];
                opinionCopy.Factors.Add(new NoosphereWorkerDailyOpinionFactorSnapshot
                {
                    Kind = factor.Kind,
                    Score = factor.Score,
                    ReasonRu = factor.ReasonRu,
                    ReasonEn = factor.ReasonEn
                });
            }

            copy.DailyOpinions.Add(opinionCopy);
        }
    }

    private void CopyNoosphereDiveMeanings(NoosphereDayStartSnapshot snapshot)
    {
        List<NoosphereDiveMeaningModel> meanings = new();
        BuildNoosphereDiveMeaningModels(meanings);
        for (int i = 0; i < meanings.Count; i++)
        {
            NoosphereDiveMeaningModel meaning = meanings[i];
            snapshot.DiveMeanings.Add(new NoosphereDiveMeaningSnapshot
            {
                Key = meaning.Key,
                Text = meaning.Text,
                Kind = meaning.Kind,
                Score = meaning.Score,
                Confidence = meaning.Confidence,
                Weight = meaning.Weight,
                IsCanon = meaning.IsCanon,
                IsBurned = meaning.IsBurned,
                Radius = meaning.Radius,
                Height = meaning.Height,
                Phase = meaning.Phase,
                Speed = meaning.Speed,
                Size = meaning.Size,
                Wobble = meaning.Wobble,
                Color = meaning.Color
            });
        }
    }

    private void CopyNoosphereVisionInsights(NoosphereDayStartSnapshot snapshot)
    {
        List<NoosphereVisionInsight> insights = new();
        BuildNoosphereVisionInsights(insights);
        for (int i = 0; i < insights.Count; i++)
        {
            NoosphereVisionInsight insight = insights[i];
            NoosphereVisionInsightSnapshot copy = new()
            {
                Key = insight.Key,
                TitleRu = insight.TitleRu,
                TitleEn = insight.TitleEn,
                SummaryRu = insight.SummaryRu,
                SummaryEn = insight.SummaryEn,
                SourceRu = insight.SourceRu,
                SourceEn = insight.SourceEn,
                EffectRu = insight.EffectRu,
                EffectEn = insight.EffectEn,
                ActionRu = insight.ActionRu,
                ActionEn = insight.ActionEn,
                Tone = insight.Tone,
                Category = insight.Category,
                Score = insight.Score,
                Strength = insight.Strength,
                SourceCount = insight.SourceCount
            };
            copy.SourceWorldPositions.AddRange(insight.SourceWorldPositions);
            snapshot.VisionInsights.Add(copy);
        }
    }

    private void CopyNoosphereVisualNodes(NoosphereDayStartSnapshot snapshot, float now)
    {
        Vector2 fieldSize = GetNoosphereVisualFieldSize();
        Vector2 core = GetNoosphereVisualCorePosition(fieldSize);
        List<NoosphereVisualNodeState> nodeStates = new();
        BuildNoosphereVisualNodeStates(now, fieldSize, core, nodeStates);
        for (int i = 0; i < nodeStates.Count; i++)
        {
            NoosphereVisualNodeState node = nodeStates[i];
            DriverAgent worker = node.Worker;
            snapshot.VisualNodes.Add(new NoosphereVisualNodeSnapshot
            {
                WorkerId = node.WorkerId,
                WorkerName = worker?.DriverName ?? string.Empty,
                Position = node.Position,
                Tone = node.Tone,
                HasPending = node.HasPending,
                HasCanon = node.HasCanon,
                ActiveMemoryCount = worker != null ? CountActiveNoosphereMemories(worker, now) : 0,
                PendingKnowledgeCount = worker?.PendingKnowledge.Count ?? 0,
                Color = node.Color
            });
        }
    }
}
