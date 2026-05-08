using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerIdleDialogueMaxVisible = 3;
    private const float WorkerIdleDialogueWordSeconds = 0.13f;
    private const float WorkerIdleDialogueLineHoldSeconds = 1.0f;
    private const float WorkerIdleDialogueMinLineSeconds = 2.1f;
    private const float WorkerIdleDialogueBubbleHeight = 1.95f;
    private const float WorkerIdleDialogueVoiceVolume = 0.075f;

    private readonly List<WorkerIdleDialogueRuntime> activeWorkerIdleDialogues = new();

    private enum WorkerIdleDialogueContext
    {
        NewFace,
        Familiar,
        Warm,
        Bar,
        Kiosk,
        Work,
        Socialite
    }

    private readonly struct WorkerIdleDialogueLine
    {
        public readonly int SpeakerSide;
        public readonly string Text;

        public WorkerIdleDialogueLine(int speakerSide, string text)
        {
            SpeakerSide = speakerSide;
            Text = text;
        }
    }

    private readonly struct WorkerIdleDialogueTemplate
    {
        public readonly WorkerIdleDialogueContext Context;
        public readonly int MinRelationship;
        public readonly WorkerIdleDialogueLine[] Lines;

        public WorkerIdleDialogueTemplate(WorkerIdleDialogueContext context, int minRelationship, WorkerIdleDialogueLine[] lines)
        {
            Context = context;
            MinRelationship = minRelationship;
            Lines = lines;
        }
    }

    private sealed class WorkerIdleDialogueRuntime
    {
        public int FirstDriverId;
        public int SecondDriverId;
        public WorkerIdleDialogueLine[] Lines;
        public string[][] LineWords;
        public int LineIndex;
        public int VisibleWords;
        public int LastVoicedWord;
        public float LineTimer;
        public float LineDuration;
        public float TotalDuration;
        public GameObject Root;
        public TextMesh Text;
        public TextMesh ShadowText;
        public AudioSource VoiceSource;
    }

    private static readonly WorkerIdleDialogueTemplate[] WorkerIdleDialogueTemplates =
    {
        new WorkerIdleDialogueTemplate(
            WorkerIdleDialogueContext.NewFace,
            -100,
            new[]
            {
                new WorkerIdleDialogueLine(0, "Ты здесь давно?"),
                new WorkerIdleDialogueLine(1, "Достаточно, чтобы подозревать тротуар."),
                new WorkerIdleDialogueLine(0, "Хорошо. Начну с тротуара осторожно.")
            }),
        new WorkerIdleDialogueTemplate(
            WorkerIdleDialogueContext.NewFace,
            -100,
            new[]
            {
                new WorkerIdleDialogueLine(0, "Я пытаюсь выглядеть местным."),
                new WorkerIdleDialogueLine(1, "Пока похоже на собеседование с улицей."),
                new WorkerIdleDialogueLine(0, "Значит, улица хотя бы слушает.")
            }),
        new WorkerIdleDialogueTemplate(
            WorkerIdleDialogueContext.Familiar,
            0,
            new[]
            {
                new WorkerIdleDialogueLine(0, "Как день?"),
                new WorkerIdleDialogueLine(1, "Идёт. Делает вид, что это план."),
                new WorkerIdleDialogueLine(0, "Классический городской стиль.")
            }),
        new WorkerIdleDialogueTemplate(
            WorkerIdleDialogueContext.Familiar,
            0,
            new[]
            {
                new WorkerIdleDialogueLine(0, "Ты тоже слышал этот гул?"),
                new WorkerIdleDialogueLine(1, "Это город думает. Медленно."),
                new WorkerIdleDialogueLine(0, "Надеюсь, не вслух про нас.")
            }),
        new WorkerIdleDialogueTemplate(
            WorkerIdleDialogueContext.Warm,
            20,
            new[]
            {
                new WorkerIdleDialogueLine(0, "Рад тебя видеть."),
                new WorkerIdleDialogueLine(1, "Осторожнее. Вдруг это станет привычкой."),
                new WorkerIdleDialogueLine(0, "Некоторые привычки город заслужил.")
            }),
        new WorkerIdleDialogueTemplate(
            WorkerIdleDialogueContext.Warm,
            20,
            new[]
            {
                new WorkerIdleDialogueLine(0, "Если город опять начнёт шуметь, я буду рядом."),
                new WorkerIdleDialogueLine(1, "Очень административно, но приятно."),
                new WorkerIdleDialogueLine(0, "Я тренирую душевную бюрократию.")
            }),
        new WorkerIdleDialogueTemplate(
            WorkerIdleDialogueContext.Bar,
            -100,
            new[]
            {
                new WorkerIdleDialogueLine(0, "После бара город мягче?"),
                new WorkerIdleDialogueLine(1, "Нет. Просто углы временно согласны молчать."),
                new WorkerIdleDialogueLine(0, "Тоже метод городской терапии.")
            }),
        new WorkerIdleDialogueTemplate(
            WorkerIdleDialogueContext.Kiosk,
            -100,
            new[]
            {
                new WorkerIdleDialogueLine(0, "Снэк за пять долларов видел?"),
                new WorkerIdleDialogueLine(1, "Видел. Экономика в хрустящей обёртке."),
                new WorkerIdleDialogueLine(0, "И ведь спорить неудобно.")
            }),
        new WorkerIdleDialogueTemplate(
            WorkerIdleDialogueContext.Work,
            -100,
            new[]
            {
                new WorkerIdleDialogueLine(0, "Как смена?"),
                new WorkerIdleDialogueLine(1, "Смена смотрела первой. Я не моргнул."),
                new WorkerIdleDialogueLine(0, "Значит, рабочая победа засчитана.")
            }),
        new WorkerIdleDialogueTemplate(
            WorkerIdleDialogueContext.Socialite,
            -100,
            new[]
            {
                new WorkerIdleDialogueLine(0, "С тобой легко начать разговор."),
                new WorkerIdleDialogueLine(1, "Это не я. Это город устал от тишины."),
                new WorkerIdleDialogueLine(0, "Передай городу спасибо.")
            })
    };

    private float StartWorkerIdleDialogue(DriverAgent first, DriverAgent second)
    {
        WorkerIdleDialogueLine[] lines = BuildWorkerIdleDialogueLines(first, second);
        float duration = GetWorkerIdleDialogueDuration(lines);
        if (activeWorkerIdleDialogues.Count >= WorkerIdleDialogueMaxVisible ||
            first?.DriverObject == null ||
            second?.DriverObject == null)
        {
            return duration;
        }

        StopWorkerIdleDialogue(first.DriverId, second.DriverId);
        WorkerIdleDialogueRuntime runtime = new()
        {
            FirstDriverId = first.DriverId,
            SecondDriverId = second.DriverId,
            Lines = lines,
            LineWords = BuildWorkerIdleDialogueWordCache(lines),
            TotalDuration = duration
        };

        CreateWorkerIdleDialogueVisual(runtime);
        StartWorkerIdleDialogueLine(runtime, 0);
        activeWorkerIdleDialogues.Add(runtime);
        return duration;
    }

    private WorkerIdleDialogueLine[] BuildWorkerIdleDialogueLines(DriverAgent first, DriverAgent second)
    {
        WorkerIdleDialogueContext context = GetWorkerIdleDialogueContext(first, second);
        int relationship = GetWorkerIdleDialogueRelationship(first, second);
        List<int> candidates = new();
        for (int i = 0; i < WorkerIdleDialogueTemplates.Length; i++)
        {
            WorkerIdleDialogueTemplate template = WorkerIdleDialogueTemplates[i];
            if (template.Context == context && relationship >= template.MinRelationship)
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
        {
            for (int i = 0; i < WorkerIdleDialogueTemplates.Length; i++)
            {
                WorkerIdleDialogueTemplate template = WorkerIdleDialogueTemplates[i];
                if (template.Context == WorkerIdleDialogueContext.Familiar && relationship >= template.MinRelationship)
                {
                    candidates.Add(i);
                }
            }
        }

        int selectedIndex = candidates.Count > 0
            ? candidates[Mathf.Abs((first?.DriverId ?? 0) * 31 + (second?.DriverId ?? 0) * 17 + currentDay * 7 + GetCurrentHour()) % candidates.Count]
            : 0;
        return WorkerIdleDialogueTemplates[Mathf.Clamp(selectedIndex, 0, WorkerIdleDialogueTemplates.Length - 1)].Lines;
    }

    private WorkerIdleDialogueContext GetWorkerIdleDialogueContext(DriverAgent first, DriverAgent second)
    {
        if (HasWorkerPerk(first, WorkerPerkKind.Socialite) || HasWorkerPerk(second, WorkerPerkKind.Socialite))
        {
            return WorkerIdleDialogueContext.Socialite;
        }

        if (IsWorkerIdleDialogueNearLocation(first, second, LocationType.Bar, 5.5f))
        {
            return WorkerIdleDialogueContext.Bar;
        }

        if (IsWorkerIdleDialogueNearLocation(first, second, LocationType.Kiosk, 5.5f))
        {
            return WorkerIdleDialogueContext.Kiosk;
        }

        if (first != null &&
            second != null &&
            first.AssignedBuildingType.HasValue &&
            second.AssignedBuildingType.HasValue &&
            first.AssignedBuildingType.Value == second.AssignedBuildingType.Value &&
            first.AssignedBuildingInstanceId == second.AssignedBuildingInstanceId)
        {
            return WorkerIdleDialogueContext.Work;
        }

        int relationship = GetWorkerIdleDialogueRelationship(first, second);
        if (relationship >= 20)
        {
            return WorkerIdleDialogueContext.Warm;
        }

        return relationship <= 0 ? WorkerIdleDialogueContext.NewFace : WorkerIdleDialogueContext.Familiar;
    }

    private bool IsWorkerIdleDialogueNearLocation(DriverAgent first, DriverAgent second, LocationType type, float radius)
    {
        if (first?.DriverObject == null || second?.DriverObject == null || !locations.TryGetValue(type, out LocationData location))
        {
            return false;
        }

        Vector3 center = GetLocationCenter(location);
        float radiusSqr = radius * radius;
        Vector3 firstDelta = first.DriverObject.transform.position - center;
        Vector3 secondDelta = second.DriverObject.transform.position - center;
        firstDelta.y = 0f;
        secondDelta.y = 0f;
        return firstDelta.sqrMagnitude <= radiusSqr && secondDelta.sqrMagnitude <= radiusSqr;
    }

    private int GetWorkerIdleDialogueRelationship(DriverAgent first, DriverAgent second)
    {
        WorkerSocialMemory firstMemory = first != null && second != null ? FindWorkerSocialMemory(first, second.DriverId) : null;
        WorkerSocialMemory secondMemory = first != null && second != null ? FindWorkerSocialMemory(second, first.DriverId) : null;
        return GetWorkerSocialPairAverageRelationship(firstMemory, secondMemory);
    }

    private static string[][] BuildWorkerIdleDialogueWordCache(WorkerIdleDialogueLine[] lines)
    {
        if (lines == null)
        {
            return System.Array.Empty<string[]>();
        }

        string[][] result = new string[lines.Length][];
        for (int i = 0; i < lines.Length; i++)
        {
            result[i] = SplitWorkerIdleDialogueWords(lines[i].Text);
        }

        return result;
    }

    private static string[] SplitWorkerIdleDialogueWords(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? System.Array.Empty<string>()
            : text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
    }

    private static float GetWorkerIdleDialogueDuration(WorkerIdleDialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return DriverIdleConversationDurationMax;
        }

        float total = 0f;
        for (int i = 0; i < lines.Length; i++)
        {
            total += GetWorkerIdleDialogueLineDuration(SplitWorkerIdleDialogueWords(lines[i].Text));
        }

        return Mathf.Max(total + 0.45f, DriverIdleConversationDurationMax);
    }

    private static float GetWorkerIdleDialogueLineDuration(string[] words)
    {
        int wordCount = words?.Length ?? 0;
        return Mathf.Max(WorkerIdleDialogueMinLineSeconds, wordCount * WorkerIdleDialogueWordSeconds + WorkerIdleDialogueLineHoldSeconds);
    }

    private void CreateWorkerIdleDialogueVisual(WorkerIdleDialogueRuntime runtime)
    {
        GameObject root = new("WorkerIdleDialogue");
        root.transform.SetParent(worldRoot, false);
        runtime.Root = root;

        TextMesh shadow = CreateWorkerIdleDialogueTextMesh("Shadow", root.transform, new Color(0f, 0f, 0f, 0.82f));
        shadow.transform.localPosition = new Vector3(0.035f, -0.035f, 0.01f);
        runtime.ShadowText = shadow;

        TextMesh text = CreateWorkerIdleDialogueTextMesh("Text", root.transform, new Color(0.96f, 0.91f, 0.78f, 1f));
        text.transform.localPosition = Vector3.zero;
        runtime.Text = text;

        runtime.VoiceSource = CreateAudioSource("WorkerIdleDialogueVoice", root.transform, false, 0.7f, 1f, false);
        runtime.VoiceSource.priority = 155;
        runtime.VoiceSource.minDistance = 2.5f;
        runtime.VoiceSource.maxDistance = 13f;
    }

    private TextMesh CreateWorkerIdleDialogueTextMesh(string name, Transform parent, Color color)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        TextMesh mesh = textObject.AddComponent<TextMesh>();
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 54;
        mesh.characterSize = 0.033f;
        mesh.lineSpacing = 0.84f;
        mesh.richText = false;
        mesh.color = color;
        return mesh;
    }

    private void StartWorkerIdleDialogueLine(WorkerIdleDialogueRuntime runtime, int lineIndex)
    {
        if (runtime == null || runtime.Lines == null || lineIndex < 0 || lineIndex >= runtime.Lines.Length)
        {
            HideWorkerIdleDialogue(runtime);
            return;
        }

        runtime.LineIndex = lineIndex;
        runtime.VisibleWords = 0;
        runtime.LastVoicedWord = 0;
        runtime.LineTimer = 0f;
        runtime.LineDuration = GetWorkerIdleDialogueLineDuration(runtime.LineWords[lineIndex]);
        SetWorkerIdleDialogueText(runtime, string.Empty);
    }

    private void UpdateWorkerIdleDialogueRuntime()
    {
        float dt = Time.deltaTime * Mathf.Max(0.1f, gameSpeedMultiplier);
        for (int i = activeWorkerIdleDialogues.Count - 1; i >= 0; i--)
        {
            WorkerIdleDialogueRuntime runtime = activeWorkerIdleDialogues[i];
            if (!IsWorkerIdleDialogueValid(runtime))
            {
                DestroyWorkerIdleDialogue(runtime);
                activeWorkerIdleDialogues.RemoveAt(i);
                continue;
            }

            runtime.LineTimer += dt;
            UpdateWorkerIdleDialogueLine(runtime);
            UpdateWorkerIdleDialogueTransform(runtime);
            if (runtime.LineTimer >= runtime.LineDuration)
            {
                StartWorkerIdleDialogueLine(runtime, runtime.LineIndex + 1);
            }
        }
    }

    private bool IsWorkerIdleDialogueValid(WorkerIdleDialogueRuntime runtime)
    {
        DriverAgent first = GetDriverAgentById(runtime?.FirstDriverId ?? -1);
        DriverAgent second = GetDriverAgentById(runtime?.SecondDriverId ?? -1);
        return runtime != null &&
               runtime.Root != null &&
               first?.DriverObject != null &&
               second?.DriverObject != null &&
               first.DriverObject.activeSelf &&
               second.DriverObject.activeSelf &&
               first.IdleConversationPartnerId == second.DriverId &&
               second.IdleConversationPartnerId == first.DriverId &&
               first.IdleConversationTimer > 0f &&
               second.IdleConversationTimer > 0f;
    }

    private void UpdateWorkerIdleDialogueLine(WorkerIdleDialogueRuntime runtime)
    {
        if (runtime.LineIndex < 0 || runtime.LineIndex >= runtime.LineWords.Length)
        {
            HideWorkerIdleDialogue(runtime);
            return;
        }

        string[] words = runtime.LineWords[runtime.LineIndex];
        int visible = Mathf.Clamp(Mathf.FloorToInt(runtime.LineTimer / WorkerIdleDialogueWordSeconds), 0, words.Length);
        if (visible == runtime.VisibleWords)
        {
            return;
        }

        runtime.VisibleWords = visible;
        SetWorkerIdleDialogueText(runtime, BuildWorkerIdleDialogueVisibleText(words, visible));
        DriverAgent speaker = GetWorkerIdleDialogueCurrentSpeaker(runtime);
        while (runtime.LastVoicedWord < visible)
        {
            runtime.LastVoicedWord++;
            PlayWorkerIdleDialogueVoiceWord(runtime, speaker, runtime.LastVoicedWord);
        }
    }

    private void UpdateWorkerIdleDialogueTransform(WorkerIdleDialogueRuntime runtime)
    {
        DriverAgent first = GetDriverAgentById(runtime.FirstDriverId);
        DriverAgent second = GetDriverAgentById(runtime.SecondDriverId);
        DriverAgent speaker = GetWorkerIdleDialogueCurrentSpeaker(runtime);
        if (speaker?.DriverObject == null || first?.DriverObject == null || second?.DriverObject == null)
        {
            return;
        }

        Vector3 speakerPosition = speaker.DriverObject.transform.position + Vector3.up * WorkerIdleDialogueBubbleHeight;
        float floatOffset = Mathf.Sin(Time.time * 3.6f + speaker.DriverId) * 0.035f;
        runtime.Root.transform.position = speakerPosition + Vector3.up * floatOffset;
        if (mainCamera != null)
        {
            runtime.Root.transform.rotation = Quaternion.LookRotation(runtime.Root.transform.position - mainCamera.transform.position, Vector3.up);
        }

        if (runtime.VoiceSource != null)
        {
            Vector3 midpoint = (first.DriverObject.transform.position + second.DriverObject.transform.position) * 0.5f + Vector3.up * 1.2f;
            runtime.VoiceSource.transform.position = midpoint;
        }
    }

    private DriverAgent GetWorkerIdleDialogueCurrentSpeaker(WorkerIdleDialogueRuntime runtime)
    {
        if (runtime == null || runtime.LineIndex < 0 || runtime.LineIndex >= runtime.Lines.Length)
        {
            return null;
        }

        int speakerId = runtime.Lines[runtime.LineIndex].SpeakerSide == 0
            ? runtime.FirstDriverId
            : runtime.SecondDriverId;
        return GetDriverAgentById(speakerId);
    }

    private float GetWorkerIdleDialogueSpeechWeight(DriverAgent driver)
    {
        if (driver == null)
        {
            return 0f;
        }

        for (int i = 0; i < activeWorkerIdleDialogues.Count; i++)
        {
            WorkerIdleDialogueRuntime runtime = activeWorkerIdleDialogues[i];
            if (runtime == null || runtime.LineIndex < 0 || runtime.LineIndex >= runtime.Lines.Length)
            {
                continue;
            }

            int speakerId = runtime.Lines[runtime.LineIndex].SpeakerSide == 0
                ? runtime.FirstDriverId
                : runtime.SecondDriverId;
            if (speakerId == driver.DriverId && runtime.VisibleWords > 0)
            {
                return 1f;
            }
        }

        return 0f;
    }

    private void StopWorkerIdleDialogue(int firstDriverId, int secondDriverId)
    {
        for (int i = activeWorkerIdleDialogues.Count - 1; i >= 0; i--)
        {
            WorkerIdleDialogueRuntime runtime = activeWorkerIdleDialogues[i];
            if (runtime == null ||
                (runtime.FirstDriverId != firstDriverId && runtime.FirstDriverId != secondDriverId &&
                 runtime.SecondDriverId != firstDriverId && runtime.SecondDriverId != secondDriverId))
            {
                continue;
            }

            DestroyWorkerIdleDialogue(runtime);
            activeWorkerIdleDialogues.RemoveAt(i);
        }
    }

    private void HideWorkerIdleDialogue(WorkerIdleDialogueRuntime runtime)
    {
        if (runtime?.Root != null)
        {
            runtime.Root.SetActive(false);
        }
    }

    private void DestroyWorkerIdleDialogue(WorkerIdleDialogueRuntime runtime)
    {
        if (runtime?.VoiceSource != null)
        {
            runtime.VoiceSource.Stop();
        }

        if (runtime?.Root != null)
        {
            Destroy(runtime.Root);
        }
    }

    private void SetWorkerIdleDialogueText(WorkerIdleDialogueRuntime runtime, string text)
    {
        string wrapped = WrapWorkerIdleDialogueText(text);
        if (runtime.Text != null)
        {
            runtime.Text.text = wrapped;
        }

        if (runtime.ShadowText != null)
        {
            runtime.ShadowText.text = wrapped;
        }
    }

    private static string BuildWorkerIdleDialogueVisibleText(string[] words, int visibleWords)
    {
        if (words == null || visibleWords <= 0)
        {
            return string.Empty;
        }

        int count = Mathf.Clamp(visibleWords, 0, words.Length);
        return string.Join(" ", words, 0, count);
    }

    private static string WrapWorkerIdleDialogueText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        const int maxLineLength = 27;
        string[] words = SplitWorkerIdleDialogueWords(text);
        List<string> lines = new();
        string current = string.Empty;
        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            string candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
            if (candidate.Length > maxLineLength && !string.IsNullOrEmpty(current))
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = candidate;
            }
        }

        if (!string.IsNullOrEmpty(current))
        {
            lines.Add(current);
        }

        if (lines.Count <= 2)
        {
            return string.Join("\n", lines);
        }

        string second = lines[1];
        if (second.Length > maxLineLength - 1)
        {
            second = second.Substring(0, maxLineLength - 1);
        }

        return $"{lines[0]}\n{second}…";
    }

    private void PlayWorkerIdleDialogueVoiceWord(WorkerIdleDialogueRuntime runtime, DriverAgent speaker, int visibleWordIndex)
    {
        if (runtime?.VoiceSource == null || speaker == null || visibleWordIndex <= 0)
        {
            return;
        }

        CitySocialVoiceProfile profile = GetCitySocialVoiceProfile(speaker.DriverId, false);
        AudioClip[] clips = GetCitySocialVoiceClips(profile);
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        AudioClip clip = clips[Mathf.Abs(speaker.DriverId * 23 + visibleWordIndex * 19) % clips.Length];
        if (clip == null)
        {
            return;
        }

        float pitch = 0.95f + (speaker.DriverId % 7) * 0.025f + Mathf.Sin(visibleWordIndex * 0.71f) * 0.035f;
        runtime.VoiceSource.pitch = Mathf.Clamp(pitch, 0.78f, 1.28f);
        runtime.VoiceSource.PlayOneShot(clip, WorkerIdleDialogueVoiceVolume * GetAudioClipVolumeMultiplier(clip));
    }
}
