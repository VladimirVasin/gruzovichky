using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const string WorkerKnowledgeBuildingHighlightColorHex = "#74D7FF";

    private readonly struct WorkerKnowledgeShareDialogueTemplate
    {
        public readonly string SharerLine;
        public readonly string ListenerLine;
        public readonly string ClosingLine;

        public WorkerKnowledgeShareDialogueTemplate(string sharerLine, string listenerLine, string closingLine)
        {
            SharerLine = sharerLine;
            ListenerLine = listenerLine;
            ClosingLine = closingLine;
        }
    }

    private sealed class WorkerKnowledgeShareTransfer
    {
        public DriverAgent Sharer;
        public DriverAgent Receiver;
        public WorkerMemory SourceMemory;
        public WorkerMemory ReceivedMemory;
        public int SharerSide;
        public WorkerSocialInteractionKind ShareKind;
        public LocationType? ShareLocationType;
    }

    private static readonly WorkerKnowledgeShareDialogueTemplate[] WorkerBuildingKnowledgeShareTemplates =
    {
        new(
            "\u042f \u0432\u0438\u0434\u0435\u043b {building}.",
            "\u0417\u0430\u043f\u043e\u043c\u043d\u044e \u043e\u0440\u0438\u0435\u043d\u0442\u0438\u0440.",
            "\u041f\u0435\u0440\u0435\u0434\u0430\u0439 \u0434\u0430\u043b\u044c\u0448\u0435, \u0435\u0441\u043b\u0438 \u0441\u043f\u0440\u043e\u0441\u044f\u0442."),
        new(
            "\u0415\u0441\u0442\u044c \u043c\u0435\u0441\u0442\u043e: {building}.",
            "\u0425\u043e\u0440\u043e\u0448\u043e. \u041f\u0443\u0441\u0442\u044c \u043f\u043e\u0436\u0438\u0432\u0435\u0442 \u0432 \u043f\u0430\u043c\u044f\u0442\u0438.",
            "\u0417\u043d\u0430\u043d\u0438\u044f \u0442\u0443\u0442 \u0431\u044b\u0441\u0442\u0440\u043e \u0441\u0433\u043e\u0440\u0430\u044e\u0442."),
        new(
            "\u0421\u043b\u044b\u0448\u0430\u043b? \u0412 \u0433\u043e\u0440\u043e\u0434\u0435 \u0435\u0441\u0442\u044c {building}.",
            "\u0422\u0435\u043f\u0435\u0440\u044c \u0441\u043b\u044b\u0448\u0430\u043b.",
            "\u041d\u043e\u043e\u0441\u0444\u0435\u0440\u0430 \u043b\u044e\u0431\u0438\u0442 \u0443\u0448\u0438.")
    };

    private static readonly WorkerKnowledgeShareDialogueTemplate[] WorkerTopicKnowledgeShareTemplates =
    {
        new(
            "\u041c\u043d\u0435 \u0437\u0430\u043f\u043e\u043c\u043d\u0438\u043b\u0430\u0441\u044c \u0442\u0435\u043c\u0430: {topic}.",
            "\u0417\u0430\u043f\u043e\u043c\u043d\u044e. \u041c\u043e\u0436\u0435\u0442 \u043f\u0440\u0438\u0433\u043e\u0434\u0438\u0442\u044c\u0441\u044f.",
            "\u0423 \u0437\u043d\u0430\u043d\u0438\u0439 \u043a\u043e\u0440\u043e\u0442\u043a\u0438\u0439 \u0441\u0440\u043e\u043a."),
        new(
            "\u0415\u0441\u0442\u044c \u0442\u0435\u043c\u0430: {topic}.",
            "\u041f\u0440\u0438\u043d\u044f\u043b. \u0422\u0435\u043f\u0435\u0440\u044c \u0438 \u0443 \u043c\u0435\u043d\u044f \u0435\u0441\u0442\u044c \u043d\u0438\u0442\u043e\u0447\u043a\u0430.",
            "\u041d\u0435 \u0434\u0430\u0439 \u0435\u0439 \u0441\u0433\u043e\u0440\u0435\u0442\u044c \u043c\u043e\u043b\u0447\u0430."),
        new(
            "\u0421\u043b\u0443\u0445 \u0434\u043d\u044f: {knowledge}.",
            "\u041b\u0430\u0434\u043d\u043e, \u0437\u0430\u043d\u0435\u0441 \u0432 \u0433\u043e\u043b\u043e\u0432\u0443.",
            "\u0415\u0441\u043b\u0438 \u0441\u0433\u043e\u0440\u0438\u0442 - \u0441\u0433\u043e\u0440\u0438\u0442.")
    };

    private bool TryBuildWorkerKnowledgeShareDialogueLines(DriverAgent first, DriverAgent second, out WorkerIdleDialogueLine[] lines)
    {
        lines = null;
        float now = GetCurrentWorldHour();
        if (!TrySelectWorkerKnowledgeShare(first, second, now, WorkerSocialInteractionKind.IdleConversation, null, out WorkerKnowledgeShareTransfer transfer) ||
            !TryRecordSharedWorkerKnowledge(transfer, now))
        {
            return false;
        }

        lines = BuildWorkerKnowledgeShareDialogueLines(transfer);
        return lines != null && lines.Length > 0;
    }

    private bool TryShareWorkerKnowledgeFromSocialInteraction(
        DriverAgent first,
        DriverAgent second,
        WorkerSocialInteractionKind shareKind,
        LocationType? locationType)
    {
        if (!ShouldShareWorkerKnowledgeForSocialInteraction(shareKind))
        {
            return false;
        }

        float now = GetCurrentWorldHour();
        return TrySelectWorkerKnowledgeShare(first, second, now, shareKind, locationType, out WorkerKnowledgeShareTransfer transfer) &&
               TryRecordSharedWorkerKnowledge(transfer, now);
    }

    private static bool ShouldShareWorkerKnowledgeForSocialInteraction(WorkerSocialInteractionKind shareKind)
    {
        return shareKind switch
        {
            WorkerSocialInteractionKind.ServiceCoPresence => true,
            WorkerSocialInteractionKind.CoworkerShift => true,
            WorkerSocialInteractionKind.PlayerPromptedConversation => true,
            WorkerSocialInteractionKind.PlayerPromptedConversationFailed => true,
            WorkerSocialInteractionKind.FamilyFormation => true,
            _ => false
        };
    }

    private bool TrySelectWorkerKnowledgeShare(
        DriverAgent first,
        DriverAgent second,
        float now,
        WorkerSocialInteractionKind shareKind,
        LocationType? locationType,
        out WorkerKnowledgeShareTransfer transfer)
    {
        transfer = null;
        if (first == null ||
            second == null ||
            first == second ||
            first.HasDepartedTown ||
            second.HasDepartedTown)
        {
            return false;
        }

        bool pruned = PruneExpiredWorkerMemories(first, now) | PruneExpiredWorkerMemories(second, now);
        if (pruned)
        {
            isDriversScreenDirty = true;
        }

        List<WorkerKnowledgeShareTransfer> candidates = new();
        AddWorkerKnowledgeShareCandidates(first, second, 0, now, shareKind, locationType, candidates);
        AddWorkerKnowledgeShareCandidates(second, first, 1, now, shareKind, locationType, candidates);
        if (candidates.Count == 0)
        {
            return false;
        }

        int seed = first.DriverId * 41 + second.DriverId * 59 + currentDay * 17 + Mathf.FloorToInt(now * 3f);
        transfer = SelectWorkerKnowledgeShareCandidate(candidates, seed);
        return transfer != null;
    }

    private static WorkerKnowledgeShareTransfer SelectWorkerKnowledgeShareCandidate(List<WorkerKnowledgeShareTransfer> candidates, int seed)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        int topicCount = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i]?.SourceMemory?.Kind == WorkerMemoryKind.ConversationTopic)
            {
                topicCount++;
            }
        }

        if (topicCount > 0)
        {
            int selectedTopicIndex = Mathf.Abs(seed) % topicCount;
            int seenTopics = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i]?.SourceMemory?.Kind != WorkerMemoryKind.ConversationTopic)
                {
                    continue;
                }

                if (seenTopics == selectedTopicIndex)
                {
                    return candidates[i];
                }

                seenTopics++;
            }
        }

        int selectedIndex = Mathf.Abs(seed) % candidates.Count;
        return candidates[selectedIndex];
    }

    private void AddWorkerKnowledgeShareCandidates(
        DriverAgent sharer,
        DriverAgent receiver,
        int sharerSide,
        float now,
        WorkerSocialInteractionKind shareKind,
        LocationType? locationType,
        List<WorkerKnowledgeShareTransfer> candidates)
    {
        if (sharer == null || receiver == null || candidates == null)
        {
            return;
        }

        for (int i = 0; i < sharer.Memories.Count; i++)
        {
            WorkerMemory memory = sharer.Memories[i];
            if (!CanShareWorkerKnowledge(sharer, receiver, memory, now))
            {
                continue;
            }

            candidates.Add(new WorkerKnowledgeShareTransfer
            {
                Sharer = sharer,
                Receiver = receiver,
                SourceMemory = memory,
                SharerSide = sharerSide,
                ShareKind = shareKind,
                ShareLocationType = locationType
            });
        }
    }

    private bool CanShareWorkerKnowledge(DriverAgent sharer, DriverAgent receiver, WorkerMemory memory, float now)
    {
        return sharer != null &&
               receiver != null &&
               memory != null &&
               IsWorkerMemoryDisplayable(memory) &&
               !ShouldExpireWorkerMemory(memory, now) &&
               !WorkerHasEquivalentKnowledge(receiver, memory, now);
    }

    private bool TryShareSpecificWorkerKnowledge(
        DriverAgent sharer,
        DriverAgent receiver,
        WorkerMemory sourceMemory,
        int sharerSide,
        WorkerSocialInteractionKind shareKind,
        LocationType? locationType,
        float now)
    {
        if (!CanShareWorkerKnowledge(sharer, receiver, sourceMemory, now))
        {
            return false;
        }

        WorkerKnowledgeShareTransfer transfer = new()
        {
            Sharer = sharer,
            Receiver = receiver,
            SourceMemory = sourceMemory,
            SharerSide = sharerSide,
            ShareKind = shareKind,
            ShareLocationType = locationType
        };
        return TryRecordSharedWorkerKnowledge(transfer, now);
    }

    private bool WorkerHasEquivalentKnowledge(DriverAgent worker, WorkerMemory source, float now)
    {
        if (worker == null || source == null)
        {
            return true;
        }

        for (int i = 0; i < worker.Memories.Count; i++)
        {
            WorkerMemory existing = worker.Memories[i];
            if (existing == null ||
                !IsWorkerMemoryDisplayable(existing) ||
                ShouldExpireWorkerMemory(existing, now))
            {
                continue;
            }

            if (AreWorkerKnowledgeEquivalent(existing, source))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AreWorkerKnowledgeEquivalent(WorkerMemory first, WorkerMemory second)
    {
        if (first == null || second == null || first.Kind != second.Kind)
        {
            return false;
        }

        return first.Kind switch
        {
            WorkerMemoryKind.ConversationTopic =>
                NormalizeWorkerKnowledgeTopicKey(first.Topic) == NormalizeWorkerKnowledgeTopicKey(second.Topic),
            WorkerMemoryKind.BuildingExistence =>
                first.BuildingType == second.BuildingType &&
                first.BuildingInstanceId == second.BuildingInstanceId,
            _ => false
        };
    }

    private static string NormalizeWorkerKnowledgeTopicKey(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return string.Empty;
        }

        string normalized = topic.Trim();
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized.ToUpperInvariant();
    }

    private bool TryRecordSharedWorkerKnowledge(WorkerKnowledgeShareTransfer transfer, float now)
    {
        if (transfer?.Sharer == null ||
            transfer.Receiver == null ||
            transfer.SourceMemory == null ||
            WorkerHasEquivalentKnowledge(transfer.Receiver, transfer.SourceMemory, now))
        {
            return false;
        }

        WorkerMemory received = CreateSharedWorkerMemory(transfer, now);
        if (!IsWorkerMemoryDisplayable(received))
        {
            return false;
        }

        transfer.Receiver.Memories.Insert(0, received);
        transfer.ReceivedMemory = received;
        RecordNoosphereKnowledgeReceived(transfer.Receiver, transfer.Sharer, received, now);
        TrimWorkerMemories(transfer.Receiver, now);
        isDriversScreenDirty = true;

        SessionDebugLogger.Log(
            "KNOWLEDGE",
            $"{GetWorkerDisplayNameSafe(transfer.Sharer)} shared {FormatWorkerKnowledgeShareDebugLabel(received)} with {GetWorkerDisplayNameSafe(transfer.Receiver)} {FormatWorkerKnowledgeShareReason(transfer.ShareKind, transfer.ShareLocationType, false)}.");
        return true;
    }

    private WorkerMemory CreateSharedWorkerMemory(WorkerKnowledgeShareTransfer transfer, float now)
    {
        DriverAgent sharer = transfer?.Sharer;
        WorkerMemory source = transfer?.SourceMemory;
        WorkerMemory received = new()
        {
            Kind = source?.Kind ?? WorkerMemoryKind.ConversationTopic,
            OtherWorkerId = sharer?.DriverId ?? 0,
            Topic = source?.Topic ?? string.Empty,
            BuildingType = source?.BuildingType,
            BuildingInstanceId = source?.BuildingInstanceId ?? 0,
            BuildingLabel = source?.BuildingLabel ?? string.Empty,
            SourceRu = FormatWorkerKnowledgeShareSource(sharer, transfer.ShareKind, transfer.ShareLocationType, true),
            SourceEn = FormatWorkerKnowledgeShareSource(sharer, transfer.ShareKind, transfer.ShareLocationType, false),
            Positive = source?.Positive ?? true,
            KnowledgeIteration = GetWorkerKnowledgeIteration(source) + 1,
            CreatedDay = currentDay,
            CreatedWorldHour = now,
            ExpiresWorldHour = now + WorkerPersonalMemoryLifetimeHours
        };

        if (received.Kind == WorkerMemoryKind.BuildingExistence &&
            received.BuildingType.HasValue &&
            string.IsNullOrWhiteSpace(received.BuildingLabel))
        {
            received.BuildingLabel = GetWorkerKnowledgeBuildingDisplayName(received.BuildingType.Value, received.BuildingInstanceId, IsRussianLanguage());
        }

        return received;
    }

    private string FormatWorkerKnowledgeShareSource(DriverAgent sharer, WorkerSocialInteractionKind shareKind, LocationType? locationType, bool ru)
    {
        string sharerName = GetWorkerDisplayNameSafe(sharer);
        return ru
            ? $"\u0443\u0441\u043b\u044b\u0448\u0430\u043b \u043e\u0442 {sharerName} {FormatWorkerKnowledgeShareReason(shareKind, locationType, true)}"
            : $"heard from {sharerName} {FormatWorkerKnowledgeShareReason(shareKind, locationType, false)}";
    }

    private static string FormatWorkerKnowledgeShareReason(WorkerSocialInteractionKind shareKind, LocationType? locationType, bool ru)
    {
        string place = locationType.HasValue
            ? GetWorkerKnowledgeBuildingTypeLabel(locationType.Value, ru)
            : ru ? "\u0433\u043e\u0440\u043e\u0434\u0435" : "the city";

        return shareKind switch
        {
            WorkerSocialInteractionKind.IdleConversation =>
                ru ? "\u0432\u043e \u0432\u0440\u0435\u043c\u044f idle-\u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u0430" : "during an idle conversation",
            WorkerSocialInteractionKind.ServiceCoPresence =>
                ru ? $"\u043f\u0440\u0438 \u0441\u043e\u0432\u043c\u0435\u0441\u0442\u043d\u043e\u043c \u043f\u043e\u0441\u0435\u0449\u0435\u043d\u0438\u0438 \u0441\u0435\u0440\u0432\u0438\u0441\u0430 {place}" : $"during a shared service visit to {place}",
            WorkerSocialInteractionKind.CoworkerShift =>
                ru ? $"\u043d\u0430 \u0441\u043e\u0432\u043c\u0435\u0441\u0442\u043d\u043e\u0439 \u0441\u043c\u0435\u043d\u0435 \u0432 {place}" : $"during a shared shift at {place}",
            WorkerSocialInteractionKind.PlayerPromptedConversation =>
                ru ? "\u0432 \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u0435, \u0443\u0441\u0442\u0440\u043e\u0435\u043d\u043d\u043e\u043c \u0447\u0435\u0440\u0435\u0437 \u0440\u0430\u0442\u0443\u0448\u0443" : "during a City Hall introduction",
            WorkerSocialInteractionKind.PlayerPromptedConversationFailed =>
                ru ? "\u0432 \u043d\u0435\u043b\u043e\u0432\u043a\u043e\u043c \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u0435 \u0447\u0435\u0440\u0435\u0437 \u0440\u0430\u0442\u0443\u0448\u0443" : "during an awkward City Hall introduction",
            WorkerSocialInteractionKind.FamilyFormation =>
                ru ? "\u043f\u0440\u0438 \u0441\u043e\u0437\u0434\u0430\u043d\u0438\u0438 \u0441\u0435\u043c\u044c\u0438" : "while forming a family",
            _ =>
                ru ? "\u043f\u0440\u0438 \u0441\u043e\u0446\u0438\u0430\u043b\u044c\u043d\u043e\u043c \u0432\u0437\u0430\u0438\u043c\u043e\u0434\u0435\u0439\u0441\u0442\u0432\u0438\u0438" : "during a social interaction"
        };
    }

    private WorkerIdleDialogueLine[] BuildWorkerKnowledgeShareDialogueLines(WorkerKnowledgeShareTransfer transfer)
    {
        if (transfer?.SourceMemory == null)
        {
            return System.Array.Empty<WorkerIdleDialogueLine>();
        }

        WorkerKnowledgeShareDialogueTemplate template = SelectWorkerKnowledgeShareDialogueTemplate(transfer);
        string sharerLine = ReplaceWorkerKnowledgeSharePlaceholders(template.SharerLine, transfer);
        string listenerLine = ReplaceWorkerKnowledgeSharePlaceholders(template.ListenerLine, transfer);
        string closingLine = ReplaceWorkerKnowledgeSharePlaceholders(template.ClosingLine, transfer);

        int listenerSide = transfer.SharerSide == 0 ? 1 : 0;
        return string.IsNullOrWhiteSpace(closingLine)
            ? new[]
            {
                new WorkerIdleDialogueLine(transfer.SharerSide, sharerLine),
                new WorkerIdleDialogueLine(listenerSide, listenerLine)
            }
            : new[]
            {
                new WorkerIdleDialogueLine(transfer.SharerSide, sharerLine),
                new WorkerIdleDialogueLine(listenerSide, listenerLine),
                new WorkerIdleDialogueLine(transfer.SharerSide, closingLine)
            };
    }

    private static WorkerKnowledgeShareDialogueTemplate SelectWorkerKnowledgeShareDialogueTemplate(WorkerKnowledgeShareTransfer transfer)
    {
        WorkerKnowledgeShareDialogueTemplate[] templates = transfer?.SourceMemory?.Kind == WorkerMemoryKind.BuildingExistence
            ? WorkerBuildingKnowledgeShareTemplates
            : WorkerTopicKnowledgeShareTemplates;
        if (templates.Length == 0)
        {
            return default;
        }

        int seed = (transfer?.Sharer?.DriverId ?? 0) * 29 +
                   (transfer?.Receiver?.DriverId ?? 0) * 37 +
                   (transfer?.SourceMemory?.BuildingInstanceId ?? 0) * 11 +
                   NormalizeWorkerKnowledgeTopicKey(transfer?.SourceMemory?.Topic).Length * 5;
        return templates[Mathf.Abs(seed) % templates.Length];
    }

    private string ReplaceWorkerKnowledgeSharePlaceholders(string template, WorkerKnowledgeShareTransfer transfer)
    {
        WorkerMemory memory = transfer?.SourceMemory;
        return (template ?? string.Empty)
            .Replace("{sharer}", GetWorkerDisplayNameSafe(transfer?.Sharer))
            .Replace("{listener}", GetWorkerDisplayNameSafe(transfer?.Receiver))
            .Replace("{building}", FormatWorkerKnowledgeShareBuildingHighlightedLabel(memory))
            .Replace("{topic}", FormatWorkerKnowledgeShareTopicHighlightedLabel(memory))
            .Replace("{knowledge}", FormatWorkerKnowledgeShareHighlightedLabel(memory));
    }

    private string FormatWorkerKnowledgeShareBuildingLabel(WorkerMemory memory)
    {
        return SanitizeWorkerKnowledgeBubbleText(GetWorkerKnowledgeBuildingDisplayName(memory, true), 44);
    }

    private static string FormatWorkerKnowledgeShareTopicLabel(WorkerMemory memory)
    {
        return SanitizeWorkerKnowledgeBubbleText(memory?.Topic, 48);
    }

    private string FormatWorkerKnowledgeShareBuildingHighlightedLabel(WorkerMemory memory)
    {
        return FormatWorkerKnowledgeBubbleHighlight(FormatWorkerKnowledgeShareBuildingLabel(memory), WorkerKnowledgeBuildingHighlightColorHex);
    }

    private static string FormatWorkerKnowledgeShareTopicHighlightedLabel(WorkerMemory memory)
    {
        return FormatWorkerKnowledgeBubbleHighlight(FormatWorkerKnowledgeShareTopicLabel(memory), CitySocialTopicHighlightColorHex);
    }

    private string FormatWorkerKnowledgeShareHighlightedLabel(WorkerMemory memory)
    {
        if (memory == null)
        {
            return string.Empty;
        }

        return memory.Kind == WorkerMemoryKind.BuildingExistence
            ? $"\u043f\u043e\u0441\u0442\u0440\u043e\u0439\u043a\u0430 {FormatWorkerKnowledgeShareBuildingHighlightedLabel(memory)}"
            : $"\u0442\u0435\u043c\u0430 {FormatWorkerKnowledgeShareTopicHighlightedLabel(memory)}";
    }

    private static string FormatWorkerKnowledgeBubbleHighlight(string value, string colorHex)
    {
        string[] words = SplitBubbleRichTextWords(value);
        if (words.Length == 0)
        {
            return string.Empty;
        }

        for (int i = 0; i < words.Length; i++)
        {
            string word = SanitizeRichTextLiteral(words[i]);
            words[i] = $"<color={colorHex}><b>{word}</b></color>";
        }

        return string.Join(" ", words);
    }

    private static string FormatWorkerKnowledgeShareDebugLabel(WorkerMemory memory)
    {
        if (memory == null)
        {
            return "knowledge";
        }

        return memory.Kind == WorkerMemoryKind.BuildingExistence
            ? $"building {memory.BuildingType}/{memory.BuildingInstanceId}"
            : $"topic '{memory.Topic}'";
    }

    private static string SanitizeWorkerKnowledgeBubbleText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string result = value.Trim()
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace('<', '[')
            .Replace('>', ']');
        while (result.Contains("  "))
        {
            result = result.Replace("  ", " ");
        }

        if (maxLength > 3 && result.Length > maxLength)
        {
            result = result.Substring(0, maxLength - 3).TrimEnd() + "...";
        }

        return result;
    }
}
