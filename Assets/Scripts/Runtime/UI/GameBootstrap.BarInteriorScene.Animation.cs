using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float BarInteriorConversationCycleSeconds = 13f;
    private const float BarInteriorConversationVisibleSeconds = 3.2f;
    private const float BarInteriorDrinkCycleSeconds = 6.4f;
    private const float BarInteriorDialogueVoiceVolume = 0.095f;

    private enum BarInteriorPatronRole
    {
        CounterDrinker,
        TableTalker,
        Dancer,
        Bartender
    }

    private sealed class BarInteriorPatronRefs
    {
        public string Name;
        public Transform Root;
        public Transform Body;
        public Transform Head;
        public Transform Hair;
        public Transform LeftArm;
        public Transform RightArm;
        public Transform LeftLeg;
        public Transform RightLeg;
        public Transform Glass;
        public Transform BubbleRoot;
        public TextMesh BubbleText;
        public TextMesh BubbleShadowText;
        public AudioSource VoiceSource;
        public BarInteriorPatronRole Role;
        public int ConversationGroup;
        public int VoiceTurnIndex = int.MinValue;
        public int VoiceWordCount;
        public float Phase;
        public bool Seated;
        public Vector3 RootBaseLocalPosition;
        public Quaternion RootBaseLocalRotation;
        public Vector3 BodyBaseLocalPosition;
        public Vector3 HeadBaseLocalPosition;
        public Vector3 HairBaseLocalPosition;
        public Vector3 LeftArmBaseLocalPosition;
        public Vector3 RightArmBaseLocalPosition;
        public Quaternion BodyBaseLocalRotation;
        public Quaternion HeadBaseLocalRotation;
        public Quaternion HairBaseLocalRotation;
        public Quaternion LeftArmBaseLocalRotation;
        public Quaternion RightArmBaseLocalRotation;
    }

    private sealed class BarInteriorGlowRefs
    {
        public Transform Transform;
        public TextMesh Text;
        public Color BaseColor;
        public Vector3 BaseScale;
        public float Phase;
        public float PulseStrength;
    }

    private readonly List<BarInteriorPatronRefs> barInteriorPatrons = new();
    private readonly List<BarInteriorGlowRefs> barInteriorGlowProps = new();
    private float barInteriorAnimationTimer;

    private static readonly string[][] BarInteriorConversationLines =
    {
        System.Array.Empty<string>(),
        new[]
        {
            "\u0417\u0430 \u0441\u043c\u0435\u043d\u0443.",
            "\u0415\u0449\u0435 \u043f\u043e \u043e\u0434\u043d\u043e\u0439?",
            "\u0413\u043e\u0440\u043e\u0434 \u0441\u0435\u0433\u043e\u0434\u043d\u044f \u043c\u044f\u0433\u0447\u0435."
        },
        new[]
        {
            "\u041a\u0430\u043a \u0434\u043e\u0440\u043e\u0433\u0430?",
            "\u041f\u043e\u0441\u043b\u0435 \u0431\u0430\u0440\u0430 \u043b\u0443\u0447\u0448\u0435.",
            "\u0417\u0430\u0432\u0442\u0440\u0430 \u0442\u043e\u0447\u043d\u043e \u0440\u0430\u0437\u0431\u0435\u0440\u0435\u043c\u0441\u044f."
        },
        new[]
        {
            "\u0421\u043b\u044b\u0448\u0438\u0448\u044c \u043c\u0443\u0437\u044b\u043a\u0443?",
            "\u042d\u0442\u043e \u043d\u0430\u0448 \u0440\u0438\u0442\u043c.",
            "\u0415\u0449\u0435 \u043a\u0440\u0443\u0433."
        }
    };

    private static readonly string[] BarInteriorTopicKnowledgeConversationTemplates =
    {
        "\u0421\u043b\u044b\u0448\u0430\u043b\u0438 \u0442\u0435\u043c\u0443: {topic}.",
        "\u0412 \u0440\u0430\u0442\u0443\u0448\u0435 \u0432\u0441\u043f\u043b\u044b\u043b\u0430 \u0442\u0435\u043c\u0430 {topic}.",
        "\u042f \u0437\u0430\u043f\u043e\u043c\u043d\u0438\u043b: {topic}."
    };

    private static readonly string[] BarInteriorBuildingKnowledgeConversationTemplates =
    {
        "\u0413\u043e\u0432\u043e\u0440\u044f\u0442, \u0435\u0441\u0442\u044c {building}.",
        "\u042f \u0432\u0438\u0434\u0435\u043b {building}.",
        "\u0422\u0435\u043f\u0435\u0440\u044c \u0432 \u0433\u043e\u0440\u043e\u0434\u0435 \u0435\u0441\u0442\u044c {building}."
    };

    private void ResetBarInteriorAnimationState()
    {
        barInteriorPatrons.Clear();
        barInteriorGlowProps.Clear();
        barInteriorAnimationTimer = 0f;
    }

    private void PrepareBarInteriorSceneAnimation()
    {
        barInteriorAnimationTimer = 0f;
        for (int i = 0; i < barInteriorPatrons.Count; i++)
        {
            BarInteriorPatronRefs patron = barInteriorPatrons[i];
            ResetBarInteriorPatronPose(patron);
            if (patron?.BubbleRoot != null)
            {
                patron.BubbleRoot.gameObject.SetActive(false);
            }

            ResetBarInteriorPatronVoice(patron);
        }
    }

    private void RegisterBarInteriorPatron(BarInteriorPatronRefs patron)
    {
        if (patron != null)
        {
            barInteriorPatrons.Add(patron);
        }
    }

    private void RegisterBarInteriorTextGlow(TextMesh textMesh, Color baseColor, float phase, float pulseStrength)
    {
        if (textMesh == null)
        {
            return;
        }

        barInteriorGlowProps.Add(new BarInteriorGlowRefs
        {
            Transform = textMesh.transform,
            Text = textMesh,
            BaseColor = baseColor,
            BaseScale = textMesh.transform.localScale,
            Phase = phase,
            PulseStrength = pulseStrength
        });
    }

    private void CreateBarInteriorCozyDecor(Transform room)
    {
        CreateBarInteriorBox(room, "AmberRug", new Vector3(-1.4f, 0.024f, -2.42f), new Vector3(4.8f, 0.035f, 1.85f), new Color(0.36f, 0.08f, 0.07f), VisualSmoothnessFabric);
        CreateBarInteriorBox(room, "RugTrimA", new Vector3(-1.4f, 0.048f, -3.30f), new Vector3(4.35f, 0.025f, 0.08f), new Color(0.88f, 0.58f, 0.22f), VisualSmoothnessFabric);
        CreateBarInteriorBox(room, "RugTrimB", new Vector3(-1.4f, 0.048f, -1.54f), new Vector3(4.35f, 0.025f, 0.08f), new Color(0.88f, 0.58f, 0.22f), VisualSmoothnessFabric);

        CreateBarInteriorBox(room, "MenuBoard", new Vector3(4.6f, 1.78f, 4.83f), new Vector3(1.65f, 0.92f, 0.055f), new Color(0.07f, 0.055f, 0.045f), VisualSmoothnessWood);
        CreateBarInteriorTextSign(room, "MenuText", "MENU", new Vector3(4.6f, 1.88f, 4.78f), 0.075f, new Color(0.95f, 0.78f, 0.48f));

        for (int i = 0; i < 4; i++)
        {
            float x = -6.7f + i * 1.25f;
            CreateBarInteriorBox(room, $"BackWallFrame{i}", new Vector3(x, 1.95f, 4.82f), new Vector3(0.72f, 0.52f, 0.05f), new Color(0.52f, 0.30f, 0.12f), VisualSmoothnessWood);
            CreateBarInteriorBox(room, $"BackWallPicture{i}", new Vector3(x, 1.95f, 4.77f), new Vector3(0.55f, 0.36f, 0.045f), i % 2 == 0 ? new Color(0.20f, 0.30f, 0.30f) : new Color(0.30f, 0.20f, 0.26f), VisualSmoothnessFabric);
        }

        for (int i = 0; i < 3; i++)
        {
            float z = -2.6f + i * 2.15f;
            CreateBarInteriorBox(room, $"LeftWallSconceBase{i}", new Vector3(-7.76f, 1.72f, z), new Vector3(0.045f, 0.42f, 0.22f), new Color(0.40f, 0.23f, 0.10f), VisualSmoothnessWood);
            Transform flame = CreateBarInteriorSphere(room, $"LeftWallSconceGlow{i}", new Vector3(-7.70f, 1.76f, z), new Vector3(0.10f, 0.14f, 0.10f), new Color(1f, 0.56f, 0.20f), VisualSmoothnessGlass).transform;
            RegisterBarInteriorGlowTransform(flame, new Color(1f, 0.56f, 0.20f), i * 0.47f, 0.18f);
        }
    }

    private void CreateBarInteriorTableCandle(Transform parent, Vector3 localPosition, float phase)
    {
        Transform candle = new GameObject("TableCandle").transform;
        candle.SetParent(parent, false);
        candle.localPosition = localPosition;

        CreateBarInteriorCylinder(candle, "CandleBody", new Vector3(0f, 0f, 0f), new Vector3(0.055f, 0.13f, 0.055f), new Color(0.94f, 0.84f, 0.62f), VisualSmoothnessDefault);
        CreateBarInteriorCylinder(candle, "CandleWick", new Vector3(0f, 0.12f, 0f), new Vector3(0.012f, 0.045f, 0.012f), new Color(0.06f, 0.04f, 0.025f), VisualSmoothnessDefault);
        Transform flame = CreateBarInteriorSphere(candle, "CandleFlame", new Vector3(0f, 0.18f, 0f), new Vector3(0.05f, 0.08f, 0.05f), new Color(1f, 0.58f, 0.20f), VisualSmoothnessGlass).transform;
        RegisterBarInteriorGlowTransform(flame, new Color(1f, 0.58f, 0.20f), phase, 0.22f);
    }

    private void CreateBarInteriorPatronGlass(BarInteriorPatronRefs patron)
    {
        if (patron?.RightArm == null || patron.Role == BarInteriorPatronRole.Bartender)
        {
            return;
        }

        Color drinkColor = patron.Role == BarInteriorPatronRole.Dancer
            ? new Color(0.90f, 0.62f, 0.28f, 0.88f)
            : new Color(0.82f, 0.58f, 0.26f, 0.90f);
        Transform glass = CreateBarInteriorCylinder(patron.RightArm, "HandGlass", new Vector3(0f, -0.27f, 0.075f), new Vector3(0.055f, 0.095f, 0.055f), drinkColor, VisualSmoothnessGlass).transform;
        patron.Glass = glass;
    }

    private void CreateBarInteriorSpeechBubble(BarInteriorPatronRefs patron)
    {
        if (patron?.Root == null || patron.ConversationGroup <= 0)
        {
            return;
        }

        GameObject bubbleObject = new($"{patron.Name}Bubble");
        bubbleObject.transform.SetParent(patron.Root, false);
        bubbleObject.transform.localPosition = new Vector3(0f, patron.Seated ? 1.55f : 1.86f, -0.02f);

        GameObject back = CreateBarInteriorBox(bubbleObject.transform, "BubbleBack", new Vector3(0f, 0f, -0.02f), new Vector3(1.28f, 0.36f, 0.025f), new Color(0.055f, 0.035f, 0.03f), VisualSmoothnessDefault);
        ApplyUnlitColor(back, new Color(0.055f, 0.035f, 0.03f));

        TextMesh shadow = CreateBarInteriorBubbleText("BubbleShadow", bubbleObject.transform, new Color(0f, 0f, 0f, 0.88f));
        shadow.transform.localPosition = new Vector3(0.015f, -0.015f, 0.02f);
        TextMesh text = CreateBarInteriorBubbleText("BubbleText", bubbleObject.transform, new Color(1f, 0.88f, 0.58f, 1f));
        text.transform.localPosition = new Vector3(0f, 0f, 0.035f);

        patron.BubbleRoot = bubbleObject.transform;
        patron.BubbleText = text;
        patron.BubbleShadowText = shadow;
        bubbleObject.SetActive(false);

        AudioSource voiceSource = CreateAudioSource($"{patron.Name}Voice", patron.Root, false, 0.7f, 1f, false);
        voiceSource.ignoreListenerPause = true;
        voiceSource.priority = 135;
        voiceSource.minDistance = 1.6f;
        voiceSource.maxDistance = 10.5f;
        voiceSource.transform.localPosition = new Vector3(0f, patron.Seated ? 1.12f : 1.38f, 0f);
        patron.VoiceSource = voiceSource;
    }

    private TextMesh CreateBarInteriorBubbleText(string name, Transform parent, Color color)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        TextMesh mesh = textObject.AddComponent<TextMesh>();
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 46;
        mesh.characterSize = 0.034f;
        mesh.lineSpacing = 0.82f;
        mesh.richText = true;
        mesh.color = color;
        return mesh;
    }

    private void RegisterBarInteriorGlowTransform(Transform transform, Color baseColor, float phase, float pulseStrength)
    {
        if (transform == null)
        {
            return;
        }

        barInteriorGlowProps.Add(new BarInteriorGlowRefs
        {
            Transform = transform,
            BaseColor = baseColor,
            BaseScale = transform.localScale,
            Phase = phase,
            PulseStrength = pulseStrength
        });
    }

    private void UpdateBarInteriorAmbientAnimations(float dt)
    {
        if (barInteriorRoot == null || !barInteriorRoot.activeSelf)
        {
            return;
        }

        barInteriorAnimationTimer += dt;
        UpdateBarInteriorPatronAnimations();
        UpdateBarInteriorConversationBubbles();
        UpdateBarInteriorGlowProps();
    }

    private void UpdateBarInteriorPatronAnimations()
    {
        for (int i = 0; i < barInteriorPatrons.Count; i++)
        {
            BarInteriorPatronRefs patron = barInteriorPatrons[i];
            if (patron?.Root == null)
            {
                continue;
            }

            ResetBarInteriorPatronPose(patron);
            float t = barInteriorAnimationTimer + patron.Phase;
            float talkWeight = GetBarInteriorConversationTalkWeight(patron);
            float drinkWeight = GetBarInteriorDrinkWeight(patron, t);
            ApplyBarInteriorBaseIdle(patron, t, talkWeight);

            switch (patron.Role)
            {
                case BarInteriorPatronRole.Dancer:
                    ApplyBarInteriorDancePose(patron, t);
                    break;
                case BarInteriorPatronRole.Bartender:
                    ApplyBarInteriorBartenderPose(patron, t);
                    break;
                default:
                    ApplyBarInteriorDrinkPose(patron, t, drinkWeight, talkWeight);
                    break;
            }
        }
    }

    private void ResetBarInteriorPatronPose(BarInteriorPatronRefs patron)
    {
        if (patron == null)
        {
            return;
        }

        SetLocalPose(patron.Root, patron.RootBaseLocalPosition, patron.RootBaseLocalRotation);
        SetLocalPose(patron.Body, patron.BodyBaseLocalPosition, patron.BodyBaseLocalRotation);
        SetLocalPose(patron.Head, patron.HeadBaseLocalPosition, patron.HeadBaseLocalRotation);
        SetLocalPose(patron.Hair, patron.HairBaseLocalPosition, patron.HairBaseLocalRotation);
        SetLocalPose(patron.LeftArm, patron.LeftArmBaseLocalPosition, patron.LeftArmBaseLocalRotation);
        SetLocalPose(patron.RightArm, patron.RightArmBaseLocalPosition, patron.RightArmBaseLocalRotation);
    }

    private static void SetLocalPose(Transform transform, Vector3 position, Quaternion rotation)
    {
        if (transform == null)
        {
            return;
        }

        transform.localPosition = position;
        transform.localRotation = rotation;
    }

    private void ApplyBarInteriorBaseIdle(BarInteriorPatronRefs patron, float t, float talkWeight)
    {
        float breath = Mathf.Sin(t * 2.15f) * 0.018f;
        float nod = Mathf.Sin(t * 5.2f) * 3.4f * talkWeight;
        float look = Mathf.Sin(t * 2.7f) * (2.8f + talkWeight * 4.5f);

        if (patron.Body != null)
        {
            patron.Body.localPosition = patron.BodyBaseLocalPosition + Vector3.up * breath;
            patron.Body.localRotation = patron.BodyBaseLocalRotation * Quaternion.Euler(talkWeight * Mathf.Sin(t * 4.1f) * 2.6f, 0f, Mathf.Sin(t * 1.7f) * 1.2f);
        }

        if (patron.Head != null)
        {
            patron.Head.localPosition = patron.HeadBaseLocalPosition + Vector3.up * breath * 1.15f;
            patron.Head.localRotation = patron.HeadBaseLocalRotation * Quaternion.Euler(nod, look * talkWeight, 0f);
        }

        if (patron.Hair != null)
        {
            patron.Hair.localPosition = patron.HairBaseLocalPosition + Vector3.up * breath * 1.15f;
            patron.Hair.localRotation = patron.Head != null ? patron.Head.localRotation : patron.HairBaseLocalRotation;
        }
    }

    private void ApplyBarInteriorDrinkPose(BarInteriorPatronRefs patron, float t, float drinkWeight, float talkWeight)
    {
        float gesture = Mathf.Sin(t * 5.8f) * 12f * talkWeight;
        if (patron.LeftArm != null)
        {
            patron.LeftArm.localRotation = patron.LeftArmBaseLocalRotation * Quaternion.Euler(0f, 0f, gesture);
            patron.LeftArm.localPosition = patron.LeftArmBaseLocalPosition + Vector3.up * Mathf.Abs(gesture) * 0.0025f;
        }

        if (patron.RightArm != null)
        {
            float sipAngle = Mathf.Lerp(0f, -58f, drinkWeight);
            float elbowLift = drinkWeight * 0.13f;
            patron.RightArm.localPosition = patron.RightArmBaseLocalPosition + new Vector3(-drinkWeight * 0.035f, elbowLift, drinkWeight * 0.055f);
            patron.RightArm.localRotation = patron.RightArmBaseLocalRotation * Quaternion.Euler(sipAngle, 0f, -talkWeight * 8f);
        }

        if (drinkWeight > 0.55f && patron.Head != null)
        {
            patron.Head.localRotation *= Quaternion.Euler(-drinkWeight * 5f, 0f, 0f);
            if (patron.Hair != null)
            {
                patron.Hair.localRotation = patron.Head.localRotation;
            }
        }
    }

    private void ApplyBarInteriorDancePose(BarInteriorPatronRefs patron, float t)
    {
        float side = Mathf.Sin(t * 1.85f);
        float step = Mathf.Sin(t * 3.7f);
        patron.Root.localPosition = patron.RootBaseLocalPosition + new Vector3(side * 0.12f, 0f, step * 0.035f);
        patron.Root.localRotation = patron.RootBaseLocalRotation * Quaternion.Euler(0f, side * 10f, 0f);

        if (patron.Body != null)
        {
            patron.Body.localRotation = patron.BodyBaseLocalRotation * Quaternion.Euler(Mathf.Abs(step) * 3f, 0f, side * -4.5f);
        }

        if (patron.LeftArm != null)
        {
            patron.LeftArm.localRotation = patron.LeftArmBaseLocalRotation * Quaternion.Euler(0f, 0f, 18f + side * 12f);
        }

        if (patron.RightArm != null)
        {
            patron.RightArm.localRotation = patron.RightArmBaseLocalRotation * Quaternion.Euler(-12f, 0f, -18f + side * 10f);
        }
    }

    private void ApplyBarInteriorBartenderPose(BarInteriorPatronRefs patron, float t)
    {
        float wipe = Mathf.Sin(t * 2.2f);
        if (patron.Root != null)
        {
            patron.Root.localPosition = patron.RootBaseLocalPosition + new Vector3(wipe * 0.10f, 0f, 0f);
        }

        if (patron.RightArm != null)
        {
            patron.RightArm.localPosition = patron.RightArmBaseLocalPosition + new Vector3(wipe * 0.09f, -0.03f, 0.04f);
            patron.RightArm.localRotation = patron.RightArmBaseLocalRotation * Quaternion.Euler(-26f, 0f, wipe * 18f);
        }

        if (patron.LeftArm != null)
        {
            patron.LeftArm.localRotation = patron.LeftArmBaseLocalRotation * Quaternion.Euler(-10f, 0f, -8f);
        }
    }

    private float GetBarInteriorDrinkWeight(BarInteriorPatronRefs patron, float t)
    {
        if (patron == null || patron.Glass == null || patron.Role == BarInteriorPatronRole.TableTalker && GetBarInteriorConversationTalkWeight(patron) > 0.35f)
        {
            return 0f;
        }

        float cycle = Mathf.Repeat(t, BarInteriorDrinkCycleSeconds);
        if (cycle < 0.85f)
        {
            return 0f;
        }

        if (cycle < 1.55f)
        {
            return SmootherStep01((cycle - 0.85f) / 0.70f);
        }

        if (cycle < 2.18f)
        {
            return 1f;
        }

        if (cycle < 3.05f)
        {
            return 1f - SmootherStep01((cycle - 2.18f) / 0.87f);
        }

        return 0f;
    }

    private float GetBarInteriorConversationTalkWeight(BarInteriorPatronRefs patron)
    {
        if (!TryGetBarInteriorConversationState(patron, out bool isSpeaker, out float normalizedVisibleTime, out _, out _))
        {
            return 0f;
        }

        if (!isSpeaker)
        {
            return 0.28f;
        }

        float inWeight = SmootherStep01(Mathf.Clamp01(normalizedVisibleTime / 0.16f));
        float outWeight = 1f - SmootherStep01(Mathf.Clamp01((normalizedVisibleTime - 0.84f) / 0.16f));
        return Mathf.Clamp01(inWeight * outWeight);
    }

    private void UpdateBarInteriorConversationBubbles()
    {
        for (int i = 0; i < barInteriorPatrons.Count; i++)
        {
            BarInteriorPatronRefs patron = barInteriorPatrons[i];
            if (patron?.BubbleRoot == null)
            {
                continue;
            }

            if (!TryGetBarInteriorConversationState(patron, out bool isSpeaker, out float normalizedVisibleTime, out string line, out int turnIndex) || !isSpeaker)
            {
                patron.BubbleRoot.gameObject.SetActive(false);
                ResetBarInteriorPatronVoice(patron);
                continue;
            }

            string visibleLine = BuildBarInteriorVisibleConversationText(line, normalizedVisibleTime);
            SetBarInteriorBubbleText(patron, visibleLine);
            UpdateBarInteriorConversationVoice(patron, line, normalizedVisibleTime, turnIndex);
            patron.BubbleRoot.gameObject.SetActive(true);
            float scale = Mathf.Lerp(0.86f, 1f, SmootherStep01(Mathf.Clamp01(normalizedVisibleTime / 0.22f)));
            patron.BubbleRoot.localScale = Vector3.one * scale;

            if (barInteriorCamera != null)
            {
                Vector3 fromCamera = patron.BubbleRoot.position - barInteriorCamera.transform.position;
                if (fromCamera.sqrMagnitude > 0.001f)
                {
                    patron.BubbleRoot.rotation = Quaternion.LookRotation(fromCamera.normalized, Vector3.up);
                }
            }
        }
    }

    private bool TryGetBarInteriorConversationState(BarInteriorPatronRefs patron, out bool isSpeaker, out float normalizedVisibleTime, out string line, out int turnIndex)
    {
        isSpeaker = false;
        normalizedVisibleTime = 0f;
        line = string.Empty;
        turnIndex = -1;
        if (patron == null || patron.ConversationGroup <= 0)
        {
            return false;
        }

        int memberCount = GetBarInteriorConversationMemberCount(patron.ConversationGroup);
        int memberIndex = GetBarInteriorConversationMemberIndex(patron);
        if (memberCount <= 0 || memberIndex < 0)
        {
            return false;
        }

        float groupTime = barInteriorAnimationTimer + patron.ConversationGroup * 1.31f;
        turnIndex = Mathf.FloorToInt(groupTime / BarInteriorConversationCycleSeconds);
        float turnTime = groupTime - turnIndex * BarInteriorConversationCycleSeconds;
        if (turnTime > BarInteriorConversationVisibleSeconds)
        {
            return false;
        }

        int speakerIndex = Mathf.Abs(turnIndex) % memberCount;
        isSpeaker = memberIndex == speakerIndex;
        normalizedVisibleTime = Mathf.Clamp01(turnTime / BarInteriorConversationVisibleSeconds);
        line = GetBarInteriorConversationLine(patron.ConversationGroup, turnIndex);
        return true;
    }

    private void UpdateBarInteriorConversationVoice(BarInteriorPatronRefs patron, string line, float normalizedVisibleTime, int turnIndex)
    {
        if (patron?.VoiceSource == null || string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        if (patron.VoiceTurnIndex != turnIndex)
        {
            patron.VoiceTurnIndex = turnIndex;
            patron.VoiceWordCount = 0;
        }

        int visibleWords = CountBarInteriorVisibleConversationWords(line, normalizedVisibleTime);
        while (patron.VoiceWordCount < visibleWords)
        {
            patron.VoiceWordCount++;
            PlayBarInteriorConversationVoiceWord(patron, patron.VoiceWordCount, turnIndex);
        }
    }

    private void PlayBarInteriorConversationVoiceWord(BarInteriorPatronRefs patron, int visibleWordIndex, int turnIndex)
    {
        if (patron?.VoiceSource == null || visibleWordIndex <= 0)
        {
            return;
        }

        int voiceId = 9000 + patron.ConversationGroup * 101 + GetBarInteriorConversationMemberIndex(patron) * 17 + (int)patron.Role;
        CitySocialVoiceProfile profile = GetCitySocialVoiceProfile(voiceId, false);
        AudioClip[] clips = GetCitySocialVoiceClips(profile);
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        AudioClip clip = clips[Mathf.Abs(voiceId * 23 + visibleWordIndex * 19 + turnIndex * 7) % clips.Length];
        if (clip == null)
        {
            return;
        }

        float rolePitch = patron.Role == BarInteriorPatronRole.Dancer ? 1.08f : patron.Role == BarInteriorPatronRole.CounterDrinker ? 0.96f : 1f;
        float wobble = 1f + Mathf.Sin((visibleWordIndex + voiceId) * 0.83f) * 0.045f;
        patron.VoiceSource.pitch = Mathf.Clamp(rolePitch * wobble, 0.76f, 1.34f);
        patron.VoiceSource.PlayOneShot(clip, BarInteriorDialogueVoiceVolume);
    }

    private void ResetBarInteriorPatronVoice(BarInteriorPatronRefs patron)
    {
        if (patron == null)
        {
            return;
        }

        patron.VoiceTurnIndex = int.MinValue;
        patron.VoiceWordCount = 0;
        StopUnityAudioSource(patron.VoiceSource);
    }

    private int GetBarInteriorConversationMemberCount(int group)
    {
        int count = 0;
        for (int i = 0; i < barInteriorPatrons.Count; i++)
        {
            if (barInteriorPatrons[i]?.ConversationGroup == group)
            {
                count++;
            }
        }

        return count;
    }

    private int GetBarInteriorConversationMemberIndex(BarInteriorPatronRefs patron)
    {
        int index = 0;
        for (int i = 0; i < barInteriorPatrons.Count; i++)
        {
            BarInteriorPatronRefs candidate = barInteriorPatrons[i];
            if (candidate?.ConversationGroup != patron.ConversationGroup)
            {
                continue;
            }

            if (candidate == patron)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    private string GetBarInteriorConversationLine(int group, int turnIndex)
    {
        if (TryGetBarInteriorKnowledgeConversationLine(group, turnIndex, out string knowledgeLine))
        {
            return knowledgeLine;
        }

        if (group < 0 || group >= BarInteriorConversationLines.Length)
        {
            group = 1;
        }

        string[] lines = BarInteriorConversationLines[group];
        if (lines == null || lines.Length == 0)
        {
            return string.Empty;
        }

        return lines[Mathf.Abs(turnIndex) % lines.Length];
    }

    private bool TryGetBarInteriorKnowledgeConversationLine(int group, int turnIndex, out string line)
    {
        line = string.Empty;
        if (!TrySelectBarInteriorKnowledgeMemory(group, turnIndex, true, out WorkerMemory memory) &&
            !TrySelectBarInteriorKnowledgeMemory(group, turnIndex, false, out memory))
        {
            return false;
        }

        line = FormatBarInteriorKnowledgeConversationLine(memory, group, turnIndex);
        return !string.IsNullOrWhiteSpace(line);
    }

    private bool TrySelectBarInteriorKnowledgeMemory(int group, int turnIndex, bool barVisitorsOnly, out WorkerMemory selectedMemory)
    {
        selectedMemory = null;
        float now = GetCurrentWorldHour();
        int count = 0;
        bool canUseCityCanon = HasBarInteriorKnowledgeAudience(barVisitorsOnly);
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (!IsBarInteriorKnowledgeCandidate(driver, barVisitorsOnly))
            {
                continue;
            }

            for (int j = 0; j < driver.Memories.Count; j++)
            {
                WorkerMemory memory = driver.Memories[j];
                if (IsWorkerMemoryDisplayable(memory) && !ShouldExpireWorkerMemory(memory, now))
                {
                    count++;
                }
            }
        }

        if (canUseCityCanon)
        {
            count += GetCityKnowledgeCanonMemoryCount();
        }

        if (count == 0)
        {
            return false;
        }

        int seed = currentDay * 53 + Mathf.FloorToInt(now) * 7 + group * 17 + turnIndex * 11 + (barVisitorsOnly ? 3 : 29);
        int selectedIndex = Mathf.Abs(seed) % count;
        int seen = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (!IsBarInteriorKnowledgeCandidate(driver, barVisitorsOnly))
            {
                continue;
            }

            for (int j = 0; j < driver.Memories.Count; j++)
            {
                WorkerMemory memory = driver.Memories[j];
                if (!IsWorkerMemoryDisplayable(memory) || ShouldExpireWorkerMemory(memory, now))
                {
                    continue;
                }

                if (seen == selectedIndex)
                {
                    selectedMemory = memory;
                    return true;
                }

                seen++;
            }
        }

        if (canUseCityCanon &&
            TryGetCityKnowledgeCanonMemoryAt(selectedIndex - seen, out WorkerMemory cityCanonMemory))
        {
            selectedMemory = cityCanonMemory;
            return true;
        }

        return false;
    }

    private bool HasBarInteriorKnowledgeAudience(bool barVisitorsOnly)
    {
        if (!barVisitorsOnly)
        {
            return true;
        }

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver != null &&
                !driver.HasDepartedTown &&
                driver.IsInsideBuilding &&
                driver.InsideBuildingType == LocationType.Bar)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBarInteriorKnowledgeCandidate(DriverAgent driver, bool barVisitorsOnly)
    {
        return driver != null &&
               !driver.HasDepartedTown &&
               driver.Memories.Count > 0 &&
               (!barVisitorsOnly || driver.IsInsideBuilding && driver.InsideBuildingType == LocationType.Bar);
    }

    private string FormatBarInteriorKnowledgeConversationLine(WorkerMemory memory, int group, int turnIndex)
    {
        if (memory == null)
        {
            return string.Empty;
        }

        if (memory.Kind == WorkerMemoryKind.BuildingExistence)
        {
            string template = BarInteriorBuildingKnowledgeConversationTemplates[Mathf.Abs(group * 31 + turnIndex) % BarInteriorBuildingKnowledgeConversationTemplates.Length];
            return template.Replace("{building}", FormatWorkerKnowledgeShareBuildingHighlightedLabel(memory));
        }

        string topicTemplate = BarInteriorTopicKnowledgeConversationTemplates[Mathf.Abs(group * 29 + turnIndex) % BarInteriorTopicKnowledgeConversationTemplates.Length];
        return topicTemplate.Replace("{topic}", FormatWorkerKnowledgeShareTopicHighlightedLabel(memory));
    }

    private static string BuildBarInteriorVisibleConversationText(string line, float normalizedVisibleTime)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return string.Empty;
        }

        float typed = Mathf.Clamp01(normalizedVisibleTime / 0.40f);
        string[] words = SplitBubbleRichTextWords(line);
        int visibleWords = Mathf.Clamp(Mathf.CeilToInt(words.Length * typed), 0, words.Length);
        return WrapBarInteriorConversationText(BuildVisibleBubbleRichTextWords(words, visibleWords));
    }

    private static int CountBarInteriorVisibleConversationWords(string line, float normalizedVisibleTime)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return 0;
        }

        float typed = Mathf.Clamp01(normalizedVisibleTime / 0.40f);
        string[] words = SplitBubbleRichTextWords(line);
        return Mathf.Clamp(Mathf.CeilToInt(words.Length * typed), 0, words.Length);
    }

    private void SetBarInteriorBubbleText(BarInteriorPatronRefs patron, string text)
    {
        if (patron.BubbleText != null)
        {
            patron.BubbleText.text = text;
        }

        if (patron.BubbleShadowText != null)
        {
            patron.BubbleShadowText.text = text;
        }
    }

    private static string WrapBarInteriorConversationText(string text)
    {
        return WrapBubbleRichText(text, 18, 2, "...");
    }

    private void UpdateBarInteriorGlowProps()
    {
        for (int i = 0; i < barInteriorGlowProps.Count; i++)
        {
            BarInteriorGlowRefs glow = barInteriorGlowProps[i];
            if (glow?.Transform == null)
            {
                continue;
            }

            float wave = 1f + Mathf.Sin(barInteriorAnimationTimer * 3.4f + glow.Phase * 5.1f) * glow.PulseStrength;
            float flutter = 1f + Mathf.Sin(barInteriorAnimationTimer * 8.7f + glow.Phase * 3.2f) * glow.PulseStrength * 0.35f;
            glow.Transform.localScale = glow.BaseScale * Mathf.Max(0.65f, wave * flutter);
            if (glow.Text != null)
            {
                glow.Text.color = Color.Lerp(glow.BaseColor * 0.72f, Color.white, Mathf.Clamp01((wave - 0.78f) * 1.7f));
            }
        }
    }
}
