using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
internal sealed class WorkerPortraitAnimator : MonoBehaviour
{
    private const float BlinkDurationSeconds = 0.115f;
    private const float MinBlinkIntervalSeconds = 3.1f;
    private const float BlinkIntervalSpreadSeconds = 1.35f;
    private const float CartoonGlanceDurationSeconds = 0.82f;
    private const float CartoonGlanceMinIntervalSeconds = 4.8f;
    private const float CartoonGlanceIntervalSpreadSeconds = 3.1f;
    private const float CartoonExpressionDurationSeconds = 0.92f;
    private const float CartoonExpressionMinIntervalSeconds = 4.2f;
    private const float CartoonExpressionIntervalSpreadSeconds = 2.1f;

    private readonly List<LayerState> layers = new();

    private bool configured;
    private bool isFemale;
    private float portraitScale = 1f;
    private float motionScale = 1f;
    private float phaseSeed;
    private WorkerPortraitExpression expression;

    public int DriverId { get; private set; }

    public void Configure(int driverId, float scale, bool female, int raceIndex, bool usesTextureParts, WorkerPortraitExpression currentExpression)
    {
        DriverId = driverId;
        portraitScale = Mathf.Max(0.15f, scale);
        motionScale = usesTextureParts ? 1f : 0.92f;
        isFemale = female;
        SetExpression(currentExpression);
        phaseSeed = Mathf.Repeat(Mathf.Abs(driverId * 0.731f + raceIndex * 1.917f + (female ? 0.43f : 0f)), 1000f);
        configured = true;

        RebuildLayers();
        ApplyPose(Time.unscaledTime);
    }

    public void SetExpression(WorkerPortraitExpression currentExpression)
    {
        expression.Fatigue = Mathf.Clamp01(currentExpression.Fatigue);
        expression.Anxiety = Mathf.Clamp01(currentExpression.Anxiety);
        expression.Positive = Mathf.Clamp01(currentExpression.Positive);
        expression.Calm = Mathf.Clamp01(currentExpression.Calm);
        expression.Hangover = Mathf.Clamp01(currentExpression.Hangover);
        expression.FinancialPressure = Mathf.Clamp01(currentExpression.FinancialPressure);
    }

    private void LateUpdate()
    {
        if (!configured)
        {
            return;
        }

        if (layers.Count == 0 && transform.childCount > 0)
        {
            RebuildLayers();
        }

        ApplyPose(Time.unscaledTime);
    }

    private void OnDisable()
    {
        ResetLayers();
    }

    private void RebuildLayers()
    {
        layers.Clear();
        WorkerPortraitLayerMarker[] markers = GetComponentsInChildren<WorkerPortraitLayerMarker>(false);
        for (int i = 0; i < markers.Length; i++)
        {
            WorkerPortraitLayerMarker marker = markers[i];
            if (marker == null || !marker.TryGetComponent(out RectTransform rect))
            {
                continue;
            }

            Image image = marker.GetComponent<Image>();
            marker.CaptureBaseState(rect, image);
            layers.Add(new LayerState
            {
                Rect = rect,
                Image = image,
                Group = ResolveLayerGroup(marker.Slot),
                BasePosition = marker.BaseAnchoredPosition,
                BaseScale = marker.BaseLocalScale,
                BaseRotation = marker.BaseLocalRotation,
                BaseColor = marker.BaseColor,
                TextureLayer = marker.TextureLayer,
                Side = ResolveLayerSide(marker.Slot, rect.gameObject.name)
            });
        }
    }

    private void ApplyPose(float time)
    {
        float s = portraitScale * motionScale;
        float expressionSpeed = Mathf.Clamp(
            1f - expression.Fatigue * 0.18f +
            expression.Anxiety * 0.38f +
            expression.Positive * 0.06f -
            expression.Calm * 0.12f +
            expression.Hangover * 0.12f,
            0.75f,
            1.55f);
        float t = time * expressionSpeed + phaseSeed;
        float postureDrop = (expression.Fatigue * 0.72f + expression.Hangover * 0.36f + expression.FinancialPressure * 0.18f) * s;
        float postureLift = (expression.Positive * 0.20f + expression.Calm * 0.08f) * s;
        float anxiousJitter = expression.Anxiety * Mathf.Sin(t * 6.7f + phaseSeed * 0.31f) * 0.10f * s;
        float breathStrength = Mathf.Clamp(1f - expression.Fatigue * 0.20f - expression.Calm * 0.08f + expression.Positive * 0.12f, 0.65f, 1.25f);
        float gazeStrength = Mathf.Clamp(1f + expression.Anxiety * 1.15f - expression.Fatigue * 0.45f + expression.Positive * 0.15f, 0.42f, 2.15f);
        float hairStrength = Mathf.Clamp(1f + expression.Anxiety * 0.48f - expression.Calm * 0.16f + expression.Positive * 0.08f, 0.72f, 1.65f);
        float glanceTimeline = t + phaseSeed * 0.13f;
        float glanceInterval = GetCartoonGlanceInterval();
        float glancePulse = GetTimedPulse(glanceTimeline, glanceInterval, CartoonGlanceDurationSeconds);
        Vector2 cartoonGlanceOffset = GetCartoonGlanceOffset(glanceTimeline, glanceInterval, glancePulse, s);
        float cartoonMood = Mathf.Clamp01(
            0.32f +
            expression.Positive * 0.28f +
            expression.Anxiety * 0.35f +
            expression.FinancialPressure * 0.24f +
            expression.Hangover * 0.16f -
            expression.Calm * 0.10f);
        float cartoonExpression = GetCartoonExpressionPulse(t) * cartoonMood;
        float actingBeat = GetActingBeat(t) * Mathf.Clamp01(
            0.24f +
            expression.Positive * 0.32f +
            expression.Anxiety * 0.34f +
            expression.FinancialPressure * 0.20f +
            expression.Hangover * 0.14f -
            expression.Calm * 0.10f);
        float happyPop = cartoonExpression * Mathf.Clamp01(
            0.50f +
            expression.Positive * 0.90f -
            expression.Anxiety * 0.25f -
            expression.FinancialPressure * 0.20f);
        float worriedPop = cartoonExpression * Mathf.Clamp01(
            expression.Anxiety * 0.86f +
            expression.FinancialPressure * 0.62f +
            expression.Hangover * 0.34f -
            expression.Positive * 0.18f);
        float eyePop = Mathf.Max(
            cartoonExpression * 0.78f,
            glancePulse * Mathf.Clamp(0.24f + expression.Anxiety * 0.24f + expression.Positive * 0.12f, 0.20f, 0.58f));
        float squint = Mathf.Clamp01(expression.Fatigue * 0.20f + expression.Hangover * 0.24f + expression.FinancialPressure * 0.10f - happyPop * 0.10f);
        float breath = Mathf.Sin(t * (isFemale ? 1.22f : 1.14f) + phaseSeed * 0.07f);
        Vector2 headOffset = new(
            Mathf.Sin(t * 0.48f + phaseSeed * 0.13f) * 0.20f * s + anxiousJitter,
            breath * 0.42f * s * breathStrength - postureDrop + postureLift);
        Vector2 actingHeadOffset = new(
            Mathf.Sin(t * 2.15f + phaseSeed * 0.19f) * actingBeat * 0.05f * s,
            (happyPop * 0.13f - worriedPop * 0.08f + actingBeat * 0.05f) * s);
        Vector2 chestOffset = new(0f, breath * 0.16f * s);
        Vector2 gazeOffset = new(
            Mathf.Sin(t * 0.31f + phaseSeed * 0.41f) * 0.46f * s * gazeStrength + anxiousJitter * 0.85f,
            Mathf.Sin(t * 0.27f + phaseSeed * 0.23f) * 0.12f * s * gazeStrength);
        headOffset += cartoonGlanceOffset * 0.07f + actingHeadOffset;
        gazeOffset += cartoonGlanceOffset;
        Vector2 hairBackOffset = new(
            Mathf.Sin(t * 0.91f + phaseSeed * 0.37f) * 0.52f * s * hairStrength,
            Mathf.Sin(t * 1.07f + phaseSeed * 0.19f) * 0.18f * s * hairStrength);
        Vector2 hairFrontOffset = new(
            Mathf.Sin(t * 1.18f + phaseSeed * 0.51f) * 0.34f * s * hairStrength,
            Mathf.Sin(t * 0.97f + phaseSeed * 0.29f) * 0.12f * s * hairStrength);
        float blink = GetBlinkAmount(t);
        float browTension = expression.Anxiety * 0.34f - expression.Fatigue * 0.16f - expression.FinancialPressure * 0.14f + expression.Positive * 0.12f;
        float mouthLift = expression.Positive * 0.20f - expression.Fatigue * 0.12f - expression.Hangover * 0.12f - expression.FinancialPressure * 0.10f;
        browTension += cartoonExpression * (0.20f + expression.Anxiety * 0.16f + expression.Positive * 0.08f) + worriedPop * 0.16f;
        mouthLift += happyPop * 0.42f - worriedPop * 0.28f + actingBeat * 0.08f;
        float browAsymmetry = Mathf.Sin(t * 1.63f + phaseSeed * 0.71f) * (cartoonExpression * 0.38f + actingBeat * 0.18f);

        for (int i = 0; i < layers.Count; i++)
        {
            LayerState layer = layers[i];
            if (layer.Rect == null)
            {
                continue;
            }

            Vector2 offset = Vector2.zero;
            Vector3 localScale = layer.BaseScale;
            Quaternion rotation = layer.BaseRotation;
            Color color = layer.BaseColor;

            switch (layer.Group)
            {
                case PortraitLayerGroup.Background:
                    break;
                case PortraitLayerGroup.Clothes:
                    offset = chestOffset;
                    break;
                case PortraitLayerGroup.HairBack:
                    offset = headOffset * 0.34f + hairBackOffset;
                    rotation = GetHairRotation(layer, t, -0.34f);
                    break;
                case PortraitLayerGroup.Ears:
                    offset = headOffset * 0.92f;
                    break;
                case PortraitLayerGroup.Head:
                    offset = headOffset;
                    break;
                case PortraitLayerGroup.Eyes:
                    float eyeSide = GetLayerSideSign(layer.Side);
                    offset = headOffset + gazeOffset;
                    offset.x += eyeSide * (eyePop * 0.08f + actingBeat * 0.035f) * s;
                    offset.y += eyeSide * browAsymmetry * 0.12f * s;
                    localScale.x *= 1f + eyePop * 0.075f + actingBeat * 0.020f;
                    localScale.y *= 1f + eyePop * 0.105f - squint * 0.095f + worriedPop * 0.050f;
                    ApplyBlinkToEyes(layer, blink, ref offset, ref localScale, ref color);
                    break;
                case PortraitLayerGroup.Brows:
                    float browSide = GetLayerSideSign(layer.Side);
                    offset = headOffset + gazeOffset * 0.28f + new Vector2(0f, browTension * s - blink * 0.26f * s);
                    offset.x += browSide * (actingBeat * 0.05f + cartoonExpression * 0.03f) * s;
                    offset.y += browSide * browAsymmetry * 0.34f * s;
                    localScale.x *= 1f + cartoonExpression * 0.085f + actingBeat * 0.030f;
                    localScale.y *= 1f + cartoonExpression * 0.060f + worriedPop * 0.045f;
                    rotation = ApplyBrowRotation(layer, rotation, browSide, cartoonExpression, actingBeat, worriedPop);
                    break;
                case PortraitLayerGroup.Nose:
                    offset = headOffset * 0.96f;
                    break;
                case PortraitLayerGroup.Mouth:
                    offset = headOffset * 0.88f + new Vector2(0f, mouthLift * s - Mathf.Abs(breath) * 0.06f * s);
                    localScale.x *= 1f + expression.Positive * 0.055f - expression.FinancialPressure * 0.025f + happyPop * 0.220f + worriedPop * 0.070f + actingBeat * 0.040f;
                    localScale.y *= 1f + expression.Positive * 0.030f - expression.Fatigue * 0.035f - happyPop * 0.070f + worriedPop * 0.210f + actingBeat * 0.040f;
                    break;
                case PortraitLayerGroup.AccessoryFace:
                    offset = headOffset * 0.95f;
                    break;
                case PortraitLayerGroup.HairFront:
                    offset = headOffset * 0.72f + hairFrontOffset;
                    rotation = GetHairRotation(layer, t, 0.28f);
                    break;
                case PortraitLayerGroup.AccessoryOver:
                    offset = headOffset * 0.96f + gazeOffset * 0.08f;
                    break;
            }

            layer.Rect.anchoredPosition = layer.BasePosition + offset;
            layer.Rect.localScale = localScale;
            layer.Rect.localRotation = rotation;
            if (layer.Image != null)
            {
                layer.Image.color = color;
            }
        }
    }

    private Quaternion GetHairRotation(LayerState layer, float t, float amplitude)
    {
        if (layer.TextureLayer)
        {
            return layer.BaseRotation;
        }

        float angle = Mathf.Sin(t * 0.82f + phaseSeed * 0.17f) * amplitude * (1f + expression.Anxiety * 0.35f - expression.Calm * 0.12f);
        return layer.BaseRotation * Quaternion.Euler(0f, 0f, angle);
    }

    private static Quaternion ApplyBrowRotation(
        LayerState layer,
        Quaternion rotation,
        float sideSign,
        float cartoonExpression,
        float actingBeat,
        float worriedPop)
    {
        if (layer.TextureLayer || Mathf.Approximately(sideSign, 0f))
        {
            return rotation;
        }

        float angle = sideSign * (cartoonExpression * 2.4f + actingBeat * 1.1f + worriedPop * 2.0f);
        return rotation * Quaternion.Euler(0f, 0f, angle);
    }

    private void ApplyBlinkToEyes(
        LayerState layer,
        float blink,
        ref Vector2 offset,
        ref Vector3 localScale,
        ref Color color)
    {
        float droop = Mathf.Clamp01(expression.Fatigue * 0.30f + expression.Hangover * 0.32f + expression.FinancialPressure * 0.08f);
        float closeAmount = Mathf.Clamp01(blink + droop * 0.42f);
        if (closeAmount <= 0f)
        {
            return;
        }

        if (layer.TextureLayer)
        {
            color.a *= Mathf.Lerp(1f, 0.12f, blink) * Mathf.Lerp(1f, 0.82f, droop);
            offset.y -= droop * 0.18f * portraitScale;
            return;
        }

        localScale.y *= Mathf.Lerp(1f, 0.14f, closeAmount);
        offset.y -= closeAmount * 0.35f * portraitScale;
    }

    private float GetBlinkAmount(float t)
    {
        float interval = MinBlinkIntervalSeconds + Mathf.Repeat(phaseSeed * 0.37f, BlinkIntervalSpreadSeconds);
        interval *= Mathf.Clamp(1f + expression.Fatigue * 0.62f + expression.Calm * 0.18f - expression.Anxiety * 0.34f, 0.52f, 1.85f);
        float duration = BlinkDurationSeconds * Mathf.Clamp(1f + expression.Fatigue * 0.46f - expression.Anxiety * 0.12f, 0.72f, 1.7f);
        float cycle = Mathf.Repeat(t, interval);
        if (cycle > duration)
        {
            return 0f;
        }

        return Mathf.Sin(cycle / duration * Mathf.PI);
    }

    private float GetCartoonGlanceInterval()
    {
        float interval = CartoonGlanceMinIntervalSeconds + Mathf.Repeat(phaseSeed * 0.61f, CartoonGlanceIntervalSpreadSeconds);
        return interval * Mathf.Clamp(1f + expression.Calm * 0.24f + expression.Fatigue * 0.14f - expression.Anxiety * 0.18f, 0.72f, 1.55f);
    }

    private Vector2 GetCartoonGlanceOffset(float timeline, float interval, float pulse, float scale)
    {
        if (pulse <= 0f)
        {
            return Vector2.zero;
        }

        int cycleIndex = Mathf.FloorToInt(timeline / Mathf.Max(0.01f, interval));
        int direction = PositiveModulo(cycleIndex + Mathf.FloorToInt(phaseSeed * 2.7f), 6);
        float amplitudeX = (0.85f + expression.Anxiety * 0.45f + expression.Positive * 0.25f - expression.Fatigue * 0.20f) * scale;
        float amplitudeY = (0.23f + expression.Positive * 0.12f + expression.Anxiety * 0.08f) * scale;
        Vector2 directionOffset = direction switch
        {
            0 => new Vector2(-0.92f * amplitudeX, 0.04f * amplitudeY),
            1 => new Vector2(0.92f * amplitudeX, 0.05f * amplitudeY),
            2 => new Vector2(-0.64f * amplitudeX, 0.75f * amplitudeY),
            3 => new Vector2(0.64f * amplitudeX, 0.72f * amplitudeY),
            4 => new Vector2(-0.45f * amplitudeX, -0.48f * amplitudeY),
            _ => new Vector2(0.45f * amplitudeX, -0.46f * amplitudeY)
        };

        return directionOffset * pulse;
    }

    private float GetCartoonExpressionPulse(float t)
    {
        float interval = CartoonExpressionMinIntervalSeconds + Mathf.Repeat(phaseSeed * 0.53f, CartoonExpressionIntervalSpreadSeconds);
        interval *= Mathf.Clamp(1f + expression.Calm * 0.18f + expression.Fatigue * 0.08f + expression.Anxiety * 0.10f - expression.Positive * 0.08f, 0.75f, 1.45f);
        return GetTimedPulse(t + phaseSeed * 0.29f, interval, CartoonExpressionDurationSeconds);
    }

    private float GetActingBeat(float t)
    {
        float interval = 1.45f + Mathf.Repeat(phaseSeed * 0.23f, 0.82f);
        float duration = 0.42f + Mathf.Repeat(phaseSeed * 0.17f, 0.18f);
        return GetTimedPulse(t + phaseSeed * 0.47f, interval, duration) * 0.72f;
    }

    private static float GetTimedPulse(float timeline, float interval, float duration)
    {
        if (interval <= 0f || duration <= 0f)
        {
            return 0f;
        }

        float cycle = Mathf.Repeat(timeline, interval);
        if (cycle > duration)
        {
            return 0f;
        }

        return SmoothPulse01(cycle / duration);
    }

    private static float SmoothPulse01(float value)
    {
        value = Mathf.Clamp01(value);
        if (value < 0.24f)
        {
            return SmoothStep01(value / 0.24f);
        }

        if (value > 0.74f)
        {
            return 1f - SmoothStep01((value - 0.74f) / 0.26f);
        }

        return 1f;
    }

    private static float SmoothStep01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static int PositiveModulo(int value, int modulo)
    {
        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private static PortraitLayerSide ResolveLayerSide(string slot, string objectName)
    {
        string value = $"{slot} {objectName}";
        if (value.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0 ||
            value.IndexOf("PBL", StringComparison.Ordinal) >= 0 ||
            value.IndexOf("PEL", StringComparison.Ordinal) >= 0 ||
            value.IndexOf("PLE", StringComparison.Ordinal) >= 0 ||
            value.IndexOf("PChL", StringComparison.Ordinal) >= 0)
        {
            return PortraitLayerSide.Left;
        }

        if (value.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0 ||
            value.IndexOf("PBR", StringComparison.Ordinal) >= 0 ||
            value.IndexOf("PER", StringComparison.Ordinal) >= 0 ||
            value.IndexOf("PRE", StringComparison.Ordinal) >= 0 ||
            value.IndexOf("PChR", StringComparison.Ordinal) >= 0)
        {
            return PortraitLayerSide.Right;
        }

        return PortraitLayerSide.Center;
    }

    private static float GetLayerSideSign(PortraitLayerSide side)
    {
        return side switch
        {
            PortraitLayerSide.Left => -1f,
            PortraitLayerSide.Right => 1f,
            _ => 0f
        };
    }

    private void ResetLayers()
    {
        for (int i = 0; i < layers.Count; i++)
        {
            LayerState layer = layers[i];
            if (layer.Rect == null)
            {
                continue;
            }

            layer.Rect.anchoredPosition = layer.BasePosition;
            layer.Rect.localScale = layer.BaseScale;
            layer.Rect.localRotation = layer.BaseRotation;
            if (layer.Image != null)
            {
                layer.Image.color = layer.BaseColor;
            }
        }
    }

    private PortraitLayerGroup ResolveLayerGroup(string slot)
    {
        if (string.IsNullOrEmpty(slot))
        {
            return PortraitLayerGroup.Background;
        }

        switch (slot)
        {
            case "background":
            case "PBP":
            case "PBD":
                return PortraitLayerGroup.Background;
            case "clothes":
            case "PSh":
            case "PC":
                return PortraitLayerGroup.Clothes;
            case "hair_back":
                return PortraitLayerGroup.HairBack;
            case "ears":
            case "PLE":
            case "PRE":
                return PortraitLayerGroup.Ears;
            case "head":
            case "PH":
            case "PN":
            case "PCh":
            case "PChL":
            case "PChR":
                return PortraitLayerGroup.Head;
            case "eyes":
            case "PEL":
            case "PER":
            case "PELL":
            case "PERL":
                return PortraitLayerGroup.Eyes;
            case "brows":
            case "PBL":
            case "PBR":
                return PortraitLayerGroup.Brows;
            case "nose":
            case "PNo":
                return PortraitLayerGroup.Nose;
            case "mouth":
            case "PMo":
            case "PMC":
                return PortraitLayerGroup.Mouth;
            case "accessory_face":
                return PortraitLayerGroup.AccessoryFace;
            case "hair_front":
                return PortraitLayerGroup.HairFront;
            case "accessory_over":
                return PortraitLayerGroup.AccessoryOver;
        }

        return ResolveProceduralLayerGroup(slot);
    }

    private static PortraitLayerGroup ResolveProceduralLayerGroup(string name)
    {
        if (name.StartsWith("PortraitHair", StringComparison.Ordinal) ||
            name.StartsWith("PH", StringComparison.Ordinal))
        {
            return IsBackHairName(name) ? PortraitLayerGroup.HairBack : PortraitLayerGroup.HairFront;
        }

        if (name.StartsWith("PortraitLeftEye", StringComparison.Ordinal) ||
            name.StartsWith("PortraitRightEye", StringComparison.Ordinal) ||
            name.StartsWith("PortraitLeftLash", StringComparison.Ordinal) ||
            name.StartsWith("PortraitRightLash", StringComparison.Ordinal))
        {
            return PortraitLayerGroup.Eyes;
        }

        if (name.StartsWith("PortraitLeftBrow", StringComparison.Ordinal) ||
            name.StartsWith("PortraitRightBrow", StringComparison.Ordinal))
        {
            return PortraitLayerGroup.Brows;
        }

        if (name.StartsWith("PortraitMouth", StringComparison.Ordinal))
        {
            return PortraitLayerGroup.Mouth;
        }

        if (name.StartsWith("PortraitGlasses", StringComparison.Ordinal))
        {
            return PortraitLayerGroup.AccessoryOver;
        }

        if (name.StartsWith("Portrait", StringComparison.Ordinal))
        {
            return PortraitLayerGroup.AccessoryFace;
        }

        if (name.StartsWith("PA", StringComparison.Ordinal))
        {
            return name.StartsWith("PAG", StringComparison.Ordinal)
                ? PortraitLayerGroup.AccessoryOver
                : PortraitLayerGroup.AccessoryFace;
        }

        return PortraitLayerGroup.AccessoryFace;
    }

    private static bool IsBackHairName(string name)
    {
        return name.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("PonyTail", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("PonyLeft", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("PonyRight", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("BobLeft", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("BobRight", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private enum PortraitLayerGroup
    {
        Background,
        Clothes,
        HairBack,
        Ears,
        Head,
        Eyes,
        Brows,
        Nose,
        Mouth,
        AccessoryFace,
        HairFront,
        AccessoryOver
    }

    private enum PortraitLayerSide
    {
        Center,
        Left,
        Right
    }

    private sealed class LayerState
    {
        public RectTransform Rect;
        public Image Image;
        public PortraitLayerGroup Group;
        public Vector2 BasePosition;
        public Vector3 BaseScale;
        public Quaternion BaseRotation;
        public Color BaseColor;
        public bool TextureLayer;
        public PortraitLayerSide Side;
    }
}

internal struct WorkerPortraitExpression
{
    public float Fatigue;
    public float Anxiety;
    public float Positive;
    public float Calm;
    public float Hangover;
    public float FinancialPressure;
}

internal sealed class WorkerPortraitLayerMarker : MonoBehaviour
{
    public string Slot;
    public bool TextureLayer;
    public Vector2 BaseAnchoredPosition;
    public Vector3 BaseLocalScale;
    public Quaternion BaseLocalRotation;
    public Color BaseColor = Color.white;

    public void Configure(string slot, RectTransform rect, Image image, bool textureLayer)
    {
        Slot = string.IsNullOrEmpty(slot) ? gameObject.name : slot;
        TextureLayer = textureLayer;
        CaptureBaseState(rect, image);
    }

    public void CaptureBaseState(RectTransform rect, Image image)
    {
        if (rect != null)
        {
            BaseAnchoredPosition = rect.anchoredPosition;
            BaseLocalScale = rect.localScale;
            BaseLocalRotation = rect.localRotation;
        }

        BaseColor = image != null ? image.color : Color.white;
    }
}
