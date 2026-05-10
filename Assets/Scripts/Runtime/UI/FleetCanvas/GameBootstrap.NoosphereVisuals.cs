using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int NoosphereVisualMaxResidentNodes = 30;
    private const int NoosphereVisualMaxLinkSegments = 150;
    private const int NoosphereVisualMaxPulses = 24;
    private const float NoosphereVisualRefreshSeconds = 0.22f;

    private NoosphereVisualUiRefs noosphereVisualUi;
    private bool noosphereVisualDirty = true;
    private float noosphereVisualRefreshTimer;
    private readonly List<NoosphereVisualNodeState> noosphereVisualNodeStates = new();
    private readonly List<NoosphereVisualPulseState> noosphereVisualPulses = new();

    private sealed class NoosphereVisualUiRefs
    {
        public RectTransform Root;
        public RectTransform Field;
        public RectTransform CoreRoot;
        public Text CoreGlyph;
        public Text CoreLabel;
        public Text StatsText;
        public readonly List<NoosphereVisualNodeUi> Nodes = new();
        public readonly List<NoosphereVisualLineUi> Links = new();
    }

    private sealed class NoosphereVisualNodeUi
    {
        public RectTransform Root;
        public Text GlyphText;
        public Text LabelText;
        public Text SparkText;
        public int WorkerId;
        public Vector2 BasePosition;
        public float Phase;
    }

    private sealed class NoosphereVisualLineUi
    {
        public RectTransform Rect;
        public Image Image;
    }

    private sealed class NoosphereVisualNodeState
    {
        public DriverAgent Worker;
        public int WorkerId;
        public Vector2 Position;
        public Color Color;
        public bool HasPending;
        public bool HasCanon;
        public WorkerKnowledgeOpinionTone Tone;
    }

    private sealed class NoosphereVisualPulseState
    {
        public RectTransform Root;
        public Text GlyphText;
        public Vector2 From;
        public Vector2 To;
        public Color Color;
        public float Age;
        public float Duration;
        public float ArcHeight;
    }

    private void SetupNoosphereVisualPanelUi(RectTransform panel, Font font)
    {
        if (panel == null)
        {
            return;
        }

        noosphereVisualUi = new NoosphereVisualUiRefs { Root = panel };

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("NoosphereVisualHeader", panel, 28f, 8f);
        Text title = CreateHeaderText("NoosphereVisualTitle", headerRow, font, "NOOSPHERE", 15, TextAnchor.MiddleLeft, Color.white);
        title.raycastTarget = false;
        title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        noosphereVisualUi.StatsText = CreateBodyText("NoosphereVisualStats", headerRow, font, string.Empty, 11, TextAnchor.MiddleRight, FleetMutedTextColor);
        noosphereVisualUi.StatsText.raycastTarget = false;
        noosphereVisualUi.StatsText.gameObject.AddComponent<LayoutElement>().preferredWidth = 158f;

        RectTransform field = CreateStyledPanel("NoosphereVisualField", panel, new Color(0.020f, 0.032f, 0.052f, 0.94f));
        field.gameObject.AddComponent<RectMask2D>();
        field.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        noosphereVisualUi.Field = field;

        CreateNoosphereVisualCore(field, font);
        noosphereVisualDirty = true;
    }

    private void CreateNoosphereVisualCore(RectTransform field, Font font)
    {
        RectTransform root = CreateUiObject("NoosphereVisualCore", field).GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.zero;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(118f, 104f);
        noosphereVisualUi.CoreRoot = root;

        noosphereVisualUi.CoreGlyph = CreateBodyText("CoreGlyph", root, font, "\u25ce", 66, TextAnchor.MiddleCenter, new Color(0.60f, 0.86f, 1f, 0.92f));
        noosphereVisualUi.CoreGlyph.raycastTarget = false;
        RectTransform glyphRect = noosphereVisualUi.CoreGlyph.rectTransform;
        glyphRect.anchorMin = Vector2.zero;
        glyphRect.anchorMax = Vector2.one;
        glyphRect.offsetMin = new Vector2(0f, 10f);
        glyphRect.offsetMax = new Vector2(0f, 10f);

        noosphereVisualUi.CoreLabel = CreateBodyText("CoreLabel", root, font, "NOOS", 11, TextAnchor.MiddleCenter, new Color(0.80f, 0.92f, 1f, 0.86f));
        noosphereVisualUi.CoreLabel.raycastTarget = false;
        RectTransform labelRect = noosphereVisualUi.CoreLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 10f);
        labelRect.sizeDelta = new Vector2(0f, 22f);
    }

    private void UpdateNoosphereVisualsRuntime()
    {
        if (!isNoospherePanelOpen || noosphereVisualUi?.Field == null)
        {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        noosphereVisualRefreshTimer -= dt;
        if (noosphereVisualDirty || noosphereVisualRefreshTimer <= 0f)
        {
            noosphereVisualRefreshTimer = NoosphereVisualRefreshSeconds;
            RebuildNoosphereVisualUi();
            noosphereVisualDirty = false;
        }

        AnimateNoosphereVisualUi(dt);
    }

    private void RebuildNoosphereVisualUi()
    {
        if (noosphereVisualUi?.Field == null)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        Vector2 fieldSize = GetNoosphereVisualFieldSize();
        Vector2 core = GetNoosphereVisualCorePosition(fieldSize);
        BuildNoosphereVisualNodeStates(now, fieldSize, core);
        ApplyNoosphereVisualNodes();
        ApplyNoosphereVisualLinks(fieldSize, core);
        ApplyNoosphereVisualStats(now);
    }

    private void BuildNoosphereVisualNodeStates(float now, Vector2 fieldSize, Vector2 core)
    {
        noosphereVisualNodeStates.Clear();
        int canonCount = GetCityKnowledgeCanonMemoryCount();
        List<DriverAgent> workers = new();
        for (int i = 0; i < driverAgents.Count && workers.Count < NoosphereVisualMaxResidentNodes; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (ShouldShowNoosphereVisualWorker(worker, now, canonCount))
            {
                workers.Add(worker);
            }
        }

        int count = workers.Count;
        float radiusX = Mathf.Max(120f, fieldSize.x * 0.36f);
        float radiusY = Mathf.Max(100f, fieldSize.y * 0.31f);
        for (int i = 0; i < count; i++)
        {
            DriverAgent worker = workers[i];
            float angle = -Mathf.PI * 0.5f + i / Mathf.Max(1f, count) * Mathf.PI * 2f;
            Vector2 position = core + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
            position.x = Mathf.Clamp(position.x, 34f, fieldSize.x - 34f);
            position.y = Mathf.Clamp(position.y, 34f, fieldSize.y - 34f);

            WorkerKnowledgeOpinionTone tone = GetNoosphereVisualWorkerTone(worker, now);
            bool hasCanon = canonCount > 0;
            Color color = hasCanon
                ? Color.Lerp(GetNoosphereVisualToneColor(tone), new Color(0.72f, 0.91f, 1f, 0.95f), 0.50f)
                : GetNoosphereVisualToneColor(tone);
            noosphereVisualNodeStates.Add(new NoosphereVisualNodeState
            {
                Worker = worker,
                WorkerId = worker.DriverId,
                Position = position,
                Color = color,
                HasPending = worker.PendingKnowledge.Count > 0,
                HasCanon = hasCanon,
                Tone = tone
            });
        }
    }

    private bool ShouldShowNoosphereVisualWorker(DriverAgent worker, float now, int canonCount)
    {
        if (worker == null || worker.HasDepartedTown || worker.IsLeavingTown)
        {
            return false;
        }

        if (canonCount > 0 || worker.PendingKnowledge.Count > 0)
        {
            return true;
        }

        for (int i = 0; i < worker.Memories.Count; i++)
        {
            WorkerMemory memory = worker.Memories[i];
            if (IsWorkerMemoryDisplayable(memory) && !ShouldExpireWorkerMemory(memory, now))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyNoosphereVisualNodes()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        for (int i = 0; i < noosphereVisualNodeStates.Count; i++)
        {
            NoosphereVisualNodeState state = noosphereVisualNodeStates[i];
            NoosphereVisualNodeUi node = GetOrCreateNoosphereVisualNode(i, font);
            node.Root.gameObject.SetActive(true);
            node.WorkerId = state.WorkerId;
            node.BasePosition = state.Position;
            node.Root.anchoredPosition = state.Position;
            node.GlyphText.text = state.HasCanon ? "*" : state.Tone switch
            {
                WorkerKnowledgeOpinionTone.Positive => "+",
                WorkerKnowledgeOpinionTone.Negative => "-",
                _ => "?"
            };
            node.GlyphText.color = state.Color;
            node.LabelText.text = GetNoosphereVisualWorkerShortLabel(state.Worker);
            node.LabelText.color = new Color(state.Color.r, state.Color.g, state.Color.b, 0.82f);
            node.SparkText.gameObject.SetActive(state.HasPending);
            node.SparkText.color = new Color(1f, 0.72f, 0.24f, 0.88f);
        }

        for (int i = noosphereVisualNodeStates.Count; i < noosphereVisualUi.Nodes.Count; i++)
        {
            noosphereVisualUi.Nodes[i].Root.gameObject.SetActive(false);
        }
    }

    private NoosphereVisualNodeUi GetOrCreateNoosphereVisualNode(int index, Font font)
    {
        while (noosphereVisualUi.Nodes.Count <= index)
        {
            noosphereVisualUi.Nodes.Add(CreateNoosphereVisualNode(noosphereVisualUi.Field, font, noosphereVisualUi.Nodes.Count));
        }

        return noosphereVisualUi.Nodes[index];
    }

    private NoosphereVisualNodeUi CreateNoosphereVisualNode(RectTransform parent, Font font, int index)
    {
        RectTransform root = CreateUiObject($"NoosphereVisualNode{index + 1}", parent).GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.zero;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(58f, 58f);

        Text glyph = CreateBodyText("Glyph", root, font, "?", 32, TextAnchor.MiddleCenter, Color.white);
        glyph.raycastTarget = false;
        glyph.rectTransform.anchorMin = Vector2.zero;
        glyph.rectTransform.anchorMax = Vector2.one;
        glyph.rectTransform.offsetMin = Vector2.zero;
        glyph.rectTransform.offsetMax = Vector2.zero;

        Text label = CreateBodyText("Label", root, font, string.Empty, 9, TextAnchor.UpperCenter, FleetMutedTextColor);
        label.raycastTarget = false;
        label.rectTransform.anchorMin = new Vector2(0f, 0f);
        label.rectTransform.anchorMax = new Vector2(1f, 0f);
        label.rectTransform.anchoredPosition = new Vector2(0f, -9f);
        label.rectTransform.sizeDelta = new Vector2(0f, 18f);

        Text spark = CreateBodyText("Spark", root, font, "\u2022", 22, TextAnchor.MiddleCenter, Color.white);
        spark.raycastTarget = false;
        spark.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        spark.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        spark.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        spark.rectTransform.sizeDelta = new Vector2(22f, 22f);

        return new NoosphereVisualNodeUi
        {
            Root = root,
            GlyphText = glyph,
            LabelText = label,
            SparkText = spark,
            Phase = index * 0.73f
        };
    }

    private void ApplyNoosphereVisualLinks(Vector2 fieldSize, Vector2 core)
    {
        int segmentIndex = 0;
        float time = Time.unscaledTime;
        DrawNoosphereVisualCoreRings(ref segmentIndex, core, fieldSize, time);
        DrawNoosphereVisualCanonLinks(ref segmentIndex, core);
        DrawNoosphereVisualRecentEventLinks(ref segmentIndex, fieldSize, core, time);

        for (int i = segmentIndex; i < noosphereVisualUi.Links.Count; i++)
        {
            noosphereVisualUi.Links[i].Image.gameObject.SetActive(false);
        }
    }

    private void DrawNoosphereVisualCoreRings(ref int segmentIndex, Vector2 core, Vector2 fieldSize, float time)
    {
        DrawNoosphereVisualRing(ref segmentIndex, core, Mathf.Min(fieldSize.x, fieldSize.y) * 0.16f, new Color(0.55f, 0.82f, 1f, 0.42f), time * 0.7f);
        DrawNoosphereVisualRing(ref segmentIndex, core, Mathf.Min(fieldSize.x, fieldSize.y) * 0.24f, new Color(0.86f, 0.68f, 1f, 0.23f), -time * 0.48f);
    }

    private void DrawNoosphereVisualCanonLinks(ref int segmentIndex, Vector2 core)
    {
        if (GetCityKnowledgeCanonMemoryCount() <= 0)
        {
            return;
        }

        for (int i = 0; i < noosphereVisualNodeStates.Count && i < 18; i++)
        {
            Color color = new(0.64f, 0.86f, 1f, 0.34f);
            DrawNoosphereVisualArc(ref segmentIndex, core, noosphereVisualNodeStates[i].Position, color, 3.0f, 0.16f, i * 0.21f);
        }
    }

    private void DrawNoosphereVisualRecentEventLinks(ref int segmentIndex, Vector2 fieldSize, Vector2 core, float time)
    {
        int drawn = 0;
        for (int i = 0; i < noosphereKnowledgeLog.Count && drawn < 16; i++)
        {
            NoosphereKnowledgeLogEntry entry = noosphereKnowledgeLog[i];
            if (!TryGetNoosphereVisualEventEndpoints(entry, fieldSize, core, out Vector2 from, out Vector2 to))
            {
                continue;
            }

            Color color = GetNoosphereVisualEventColor(entry);
            float wobble = entry.MemoryKind == WorkerMemoryKind.ConversationTopic
                ? Mathf.Clamp01(entry.RumorDistortionPercent / 100f) * 0.38f
                : 0.08f;
            float width = entry.EventKind == NoosphereKnowledgeEventKind.Canonized ? 4.2f : 2.6f;
            DrawNoosphereVisualArc(ref segmentIndex, from, to, color, width, wobble, time * 0.32f + i * 0.33f);
            drawn++;
        }
    }

    private void DrawNoosphereVisualRing(ref int segmentIndex, Vector2 center, float radius, Color color, float phase)
    {
        const int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i / (float)segments * Mathf.PI * 2f + phase;
            float a1 = (i + 1) / (float)segments * Mathf.PI * 2f + phase;
            Vector2 from = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
            Vector2 to = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            ApplyNoosphereVisualLine(ref segmentIndex, from, to, color, 2.2f);
        }
    }

    private void DrawNoosphereVisualArc(ref int segmentIndex, Vector2 from, Vector2 to, Color color, float width, float wobble, float phase)
    {
        const int parts = 7;
        Vector2 previous = from;
        Vector2 delta = to - from;
        Vector2 normal = delta.sqrMagnitude > 0.01f
            ? new Vector2(-delta.y, delta.x).normalized
            : Vector2.up;
        for (int i = 1; i <= parts; i++)
        {
            float p = i / (float)parts;
            Vector2 point = Vector2.Lerp(from, to, p);
            point += normal * (Mathf.Sin(p * Mathf.PI) * 26f);
            point += normal * (Mathf.Sin(p * Mathf.PI * 5f + phase) * wobble * 18f);
            ApplyNoosphereVisualLine(ref segmentIndex, previous, point, color, width);
            previous = point;
        }
    }

    private void ApplyNoosphereVisualLine(ref int index, Vector2 from, Vector2 to, Color color, float width)
    {
        if (index >= NoosphereVisualMaxLinkSegments)
        {
            return;
        }

        NoosphereVisualLineUi line = GetOrCreateNoosphereVisualLine(index++);
        Vector2 delta = to - from;
        line.Image.gameObject.SetActive(delta.sqrMagnitude > 0.01f);
        if (delta.sqrMagnitude <= 0.01f)
        {
            return;
        }

        line.Rect.anchoredPosition = (from + to) * 0.5f;
        line.Rect.sizeDelta = new Vector2(delta.magnitude, width);
        line.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        line.Image.color = color;
    }

    private NoosphereVisualLineUi GetOrCreateNoosphereVisualLine(int index)
    {
        while (noosphereVisualUi.Links.Count <= index)
        {
            noosphereVisualUi.Links.Add(CreateNoosphereVisualLine(noosphereVisualUi.Field, $"NoosphereVisualLink{noosphereVisualUi.Links.Count + 1}"));
        }

        return noosphereVisualUi.Links[index];
    }

    private NoosphereVisualLineUi CreateNoosphereVisualLine(RectTransform parent, string name)
    {
        RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;
        rect.SetAsFirstSibling();
        return new NoosphereVisualLineUi { Rect = rect, Image = image };
    }

    private void NotifyNoosphereVisualKnowledgeEvent(NoosphereKnowledgeLogEntry entry)
    {
        noosphereVisualDirty = true;
        if (!isNoospherePanelOpen || entry == null || noosphereVisualUi?.Field == null)
        {
            return;
        }

        Vector2 fieldSize = GetNoosphereVisualFieldSize();
        Vector2 core = GetNoosphereVisualCorePosition(fieldSize);
        BuildNoosphereVisualNodeStates(GetCurrentWorldHour(), fieldSize, core);

        if (entry.EventKind == NoosphereKnowledgeEventKind.Canonized)
        {
            Color color = GetNoosphereVisualEventColor(entry);
            for (int i = 0; i < noosphereVisualNodeStates.Count && i < NoosphereVisualMaxPulses; i++)
            {
                SpawnNoosphereVisualPulse(core, noosphereVisualNodeStates[i].Position, color, 1.35f, 34f);
            }

            return;
        }

        if (TryGetNoosphereVisualEventEndpoints(entry, fieldSize, core, out Vector2 from, out Vector2 to))
        {
            SpawnNoosphereVisualPulse(from, to, GetNoosphereVisualEventColor(entry), 1.05f, 26f);
        }
    }

    private void SpawnNoosphereVisualPulse(Vector2 from, Vector2 to, Color color, float duration, float arcHeight)
    {
        while (noosphereVisualPulses.Count >= NoosphereVisualMaxPulses)
        {
            DestroyNoosphereVisualPulse(noosphereVisualPulses[0]);
            noosphereVisualPulses.RemoveAt(0);
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform root = CreateUiObject("NoosphereVisualPulse", noosphereVisualUi.Field).GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.zero;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(32f, 32f);
        Text glyph = CreateBodyText("Glyph", root, font, "\u2022", 32, TextAnchor.MiddleCenter, color);
        glyph.raycastTarget = false;
        glyph.rectTransform.anchorMin = Vector2.zero;
        glyph.rectTransform.anchorMax = Vector2.one;
        glyph.rectTransform.offsetMin = Vector2.zero;
        glyph.rectTransform.offsetMax = Vector2.zero;

        noosphereVisualPulses.Add(new NoosphereVisualPulseState
        {
            Root = root,
            GlyphText = glyph,
            From = from,
            To = to,
            Color = color,
            Duration = Mathf.Max(0.1f, duration),
            ArcHeight = arcHeight
        });
    }

    private void AnimateNoosphereVisualUi(float dt)
    {
        if (noosphereVisualUi?.CoreRoot == null)
        {
            return;
        }

        float time = Time.unscaledTime;
        Vector2 fieldSize = GetNoosphereVisualFieldSize();
        Vector2 core = GetNoosphereVisualCorePosition(fieldSize);
        noosphereVisualUi.CoreRoot.anchoredPosition = core + Vector2.up * (Mathf.Sin(time * 1.8f) * 4f);
        noosphereVisualUi.CoreRoot.localScale = Vector3.one * (1f + Mathf.Sin(time * 2.1f) * 0.045f);
        noosphereVisualUi.CoreRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.9f) * 4f);
        if (noosphereVisualUi.CoreGlyph != null)
        {
            noosphereVisualUi.CoreGlyph.color = new Color(0.60f, 0.86f, 1f, 0.84f + Mathf.Sin(time * 2.6f) * 0.08f);
        }

        for (int i = 0; i < noosphereVisualUi.Nodes.Count; i++)
        {
            NoosphereVisualNodeUi node = noosphereVisualUi.Nodes[i];
            if (node?.Root == null || !node.Root.gameObject.activeSelf)
            {
                continue;
            }

            float pulse = 1f + Mathf.Sin(time * 2.8f + node.Phase) * 0.10f;
            node.Root.localScale = Vector3.one * pulse;
            node.Root.anchoredPosition = node.BasePosition + Vector2.up * (Mathf.Sin(time * 1.6f + node.Phase) * 2.2f);
            if (node.SparkText != null && node.SparkText.gameObject.activeSelf)
            {
                float angle = time * 3.6f + node.Phase;
                node.SparkText.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle) * 17f, Mathf.Sin(angle) * 17f);
            }
        }

        for (int i = noosphereVisualPulses.Count - 1; i >= 0; i--)
        {
            NoosphereVisualPulseState pulse = noosphereVisualPulses[i];
            pulse.Age += dt;
            float progress = Mathf.Clamp01(pulse.Age / pulse.Duration);
            Vector2 position = Vector2.Lerp(pulse.From, pulse.To, progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * pulse.ArcHeight;
            pulse.Root.anchoredPosition = position;
            pulse.Root.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.55f, progress);
            Color color = pulse.Color;
            color.a *= 1f - progress;
            pulse.GlyphText.color = color;
            if (progress >= 1f)
            {
                DestroyNoosphereVisualPulse(pulse);
                noosphereVisualPulses.RemoveAt(i);
            }
        }
    }

    private void ApplyNoosphereVisualStats(float now)
    {
        if (noosphereVisualUi?.StatsText == null)
        {
            return;
        }

        int pending = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            pending += driverAgents[i]?.PendingKnowledge.Count ?? 0;
        }

        noosphereVisualUi.StatsText.text = IsRussianLanguage()
            ? $"\u0430\u043a\u0442 {CountActiveNoosphereKnowledge(now)} / \u0440\u0430\u0437\u0434 {pending} / \u0432\u0435\u0447 {GetCityKnowledgeCanonMemoryCount()}"
            : $"act {CountActiveNoosphereKnowledge(now)} / form {pending} / perm {GetCityKnowledgeCanonMemoryCount()}";
    }

    private bool TryGetNoosphereVisualEventEndpoints(NoosphereKnowledgeLogEntry entry, Vector2 fieldSize, Vector2 core, out Vector2 from, out Vector2 to)
    {
        from = core;
        to = core;
        if (entry == null)
        {
            return false;
        }

        if (entry.EventKind != NoosphereKnowledgeEventKind.Canonized &&
            TryGetNoosphereVisualNodePosition(entry.OwnerWorkerId, out Vector2 owner))
        {
            to = owner;
        }

        if (entry.MemoryKind == WorkerMemoryKind.BuildingExistence)
        {
            from = GetNoosphereVisualBuildingAnchor(entry, fieldSize);
            return true;
        }

        if (TryGetNoosphereVisualNodePosition(entry.OtherWorkerId, out Vector2 other))
        {
            from = other;
            return true;
        }

        from = core + new Vector2(Mathf.Sin(entry.KnowledgeIteration * 1.7f) * 62f, -fieldSize.y * 0.24f);
        return true;
    }

    private bool TryGetNoosphereVisualNodePosition(int workerId, out Vector2 position)
    {
        for (int i = 0; i < noosphereVisualNodeStates.Count; i++)
        {
            if (noosphereVisualNodeStates[i].WorkerId == workerId)
            {
                position = noosphereVisualNodeStates[i].Position;
                return true;
            }
        }

        position = Vector2.zero;
        return false;
    }

    private static Vector2 GetNoosphereVisualBuildingAnchor(NoosphereKnowledgeLogEntry entry, Vector2 fieldSize)
    {
        int seed = ((int)(entry?.BuildingType ?? LocationType.Parking) * 37) ^ ((entry?.BuildingInstanceId ?? 0) * 19);
        float x = Mathf.Lerp(fieldSize.x * 0.16f, fieldSize.x * 0.84f, Mathf.Repeat(seed * 0.071f, 1f));
        float y = fieldSize.y * (0.13f + Mathf.Repeat(seed * 0.037f, 1f) * 0.12f);
        return new Vector2(x, y);
    }

    private Vector2 GetNoosphereVisualFieldSize()
    {
        Vector2 size = noosphereVisualUi?.Field != null ? noosphereVisualUi.Field.rect.size : Vector2.zero;
        if (size.x < 1f || size.y < 1f)
        {
            size = new Vector2(366f, 508f);
        }

        return size;
    }

    private static Vector2 GetNoosphereVisualCorePosition(Vector2 fieldSize)
    {
        return new Vector2(fieldSize.x * 0.50f, fieldSize.y * 0.55f);
    }

    private WorkerKnowledgeOpinionTone GetNoosphereVisualWorkerTone(DriverAgent worker, float now)
    {
        int score = 0;
        int count = 0;
        if (worker == null)
        {
            return WorkerKnowledgeOpinionTone.Neutral;
        }

        for (int i = 0; i < worker.Memories.Count; i++)
        {
            WorkerMemory memory = worker.Memories[i];
            if (!IsWorkerMemoryDisplayable(memory) || ShouldExpireWorkerMemory(memory, now))
            {
                continue;
            }

            score += memory.OpinionScore;
            if (memory.Kind == WorkerMemoryKind.ConversationTopic)
            {
                score += Mathf.RoundToInt(memory.RumorConnotationScore * 0.35f);
            }

            count++;
        }

        if (count <= 0)
        {
            return WorkerKnowledgeOpinionTone.Neutral;
        }

        return GetWorkerKnowledgeOpinionTone(Mathf.RoundToInt(score / (float)count));
    }

    private static Color GetNoosphereVisualToneColor(WorkerKnowledgeOpinionTone tone)
    {
        return tone switch
        {
            WorkerKnowledgeOpinionTone.Positive => new Color(0.80f, 1f, 0.36f, 0.92f),
            WorkerKnowledgeOpinionTone.Negative => new Color(1f, 0.30f, 0.20f, 0.92f),
            _ => new Color(0.50f, 0.78f, 1f, 0.86f)
        };
    }

    private static Color GetNoosphereVisualEventColor(NoosphereKnowledgeLogEntry entry)
    {
        if (entry?.EventKind == NoosphereKnowledgeEventKind.Canonized)
        {
            return new Color(0.78f, 0.92f, 1f, 0.86f);
        }

        if (entry?.EventKind == NoosphereKnowledgeEventKind.Burned)
        {
            return new Color(1f, 0.25f, 0.18f, 0.48f);
        }

        if (entry?.MemoryKind == WorkerMemoryKind.ConversationTopic)
        {
            int connotation = Mathf.Clamp(entry.RumorConnotationScore, -100, 100);
            if (connotation >= 12)
            {
                return new Color(0.88f, 1f, 0.34f, 0.76f);
            }

            if (connotation <= -12)
            {
                return new Color(1f, 0.30f, 0.22f, 0.76f);
            }
        }

        return new Color(0.48f, 0.84f, 1f, 0.62f);
    }

    private static string GetNoosphereVisualWorkerShortLabel(DriverAgent worker)
    {
        if (worker == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(worker.DriverName)
            ? $"#{worker.DriverId}"
            : worker.DriverName.Length <= 7
                ? worker.DriverName
                : worker.DriverName.Substring(0, 7);
    }

    private void DestroyNoosphereVisualPulse(NoosphereVisualPulseState pulse)
    {
        if (pulse?.Root != null)
        {
            Destroy(pulse.Root.gameObject);
        }
    }
}
