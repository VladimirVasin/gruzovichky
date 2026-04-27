using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap : MonoBehaviour
{
    private void SetupRacingHud()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObj = new("RacingHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = 100;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode   = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        racingHudCanvas = canvas;

        // Panel anchored top-left
        RectTransform panel = CreateUiObject("RacingHudPanel", canvasObj.transform).GetComponent<RectTransform>();
        panel.anchorMin        = new Vector2(0f, 1f);
        panel.anchorMax        = new Vector2(0f, 1f);
        panel.pivot            = new Vector2(0f, 1f);
        panel.anchoredPosition = new Vector2(18f, -18f);
        panel.sizeDelta        = new Vector2(220f, 160f);

        Image panelBg = panel.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.05f, 0.08f, 0.80f);
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor    = new Color(0.88f, 0.62f, 0.08f, 0.5f);
        outline.effectDistance = new Vector2(2f, -2f);

        racingHudText = new GameObject("RacingHudText").AddComponent<Text>();
        racingHudText.transform.SetParent(panel, false);
        racingHudText.rectTransform.anchorMin = Vector2.zero;
        racingHudText.rectTransform.anchorMax = Vector2.one;
        racingHudText.rectTransform.offsetMin = new Vector2(12f, 10f);
        racingHudText.rectTransform.offsetMax = new Vector2(-12f, -10f);
        racingHudText.font      = font;
        racingHudText.fontSize  = 16;
        racingHudText.color     = new Color(0.92f, 0.90f, 0.86f);
        racingHudText.alignment = TextAnchor.UpperLeft;
        racingHudText.text      = "";

        RectTransform hintPanel = CreateUiObject("RacingControlHintPanel", canvasObj.transform).GetComponent<RectTransform>();
        hintPanel.anchorMin        = new Vector2(1f, 1f);
        hintPanel.anchorMax        = new Vector2(1f, 1f);
        hintPanel.pivot            = new Vector2(1f, 1f);
        hintPanel.anchoredPosition = new Vector2(-18f, -18f);
        hintPanel.sizeDelta        = new Vector2(460f, 164f);

        Image hintBg = hintPanel.gameObject.AddComponent<Image>();
        hintBg.color = new Color(0.04f, 0.05f, 0.08f, 0.86f);
        Outline hintOutline = hintPanel.gameObject.AddComponent<Outline>();
        hintOutline.effectColor    = new Color(0.88f, 0.62f, 0.08f, 0.55f);
        hintOutline.effectDistance = new Vector2(2f, -2f);

        racingControlHintText = new GameObject("RacingControlHintText").AddComponent<Text>();
        racingControlHintText.transform.SetParent(hintPanel, false);
        racingControlHintText.rectTransform.anchorMin = Vector2.zero;
        racingControlHintText.rectTransform.anchorMax = Vector2.one;
        racingControlHintText.rectTransform.offsetMin = new Vector2(18f, 12f);
        racingControlHintText.rectTransform.offsetMax = new Vector2(-18f, -12f);
        racingControlHintText.font      = font;
        racingControlHintText.fontSize  = 17;
        racingControlHintText.color     = new Color(0.96f, 0.93f, 0.84f, 1f);
        racingControlHintText.alignment = TextAnchor.MiddleLeft;
        racingControlHintText.text = IsRussianLanguage()
            ? "Управление\n" +
              "Мышь: руль\n" +
              "W / ↑: газ\n" +
              "S / ↓: тормоз / назад\n" +
              "КПП: тяни рычаг вверх / вниз\n" +
              "ESC: выйти"
            : "Controls\n" +
              "Mouse: steering\n" +
              "W / ↑: throttle\n" +
              "S / ↓: brake / reverse\n" +
              "Gearbox: drag shifter up / down\n" +
              "ESC: exit";

        SetupSpeedometer(canvasObj.transform, font);
        SetupRacingSpeedLines(canvasObj.transform);
    }

    private void SetupRacingSpeedLines(Transform canvasParent)
    {
        racingSpeedLineImages.Clear();
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 8; i++)
            {
                RectTransform line = CreateUiObject($"SpeedLine_{side}_{i}", canvasParent).GetComponent<RectTransform>();
                line.anchorMin = new Vector2(side < 0 ? 0f : 1f, 0.5f);
                line.anchorMax = new Vector2(side < 0 ? 0f : 1f, 0.5f);
                line.pivot = new Vector2(side < 0 ? 0f : 1f, 0.5f);
                line.anchoredPosition = new Vector2(side * Random.Range(20f, 90f), Random.Range(-320f, 280f));
                line.sizeDelta = new Vector2(Random.Range(90f, 180f), Random.Range(2f, 5f));
                line.localEulerAngles = new Vector3(0f, 0f, side * Random.Range(-7f, 7f));
                Image img = line.gameObject.AddComponent<Image>();
                img.color = new Color(1f, 0.92f, 0.72f, 0f);
                img.raycastTarget = false;
                racingSpeedLineImages.Add(img);
            }
        }
    }

    private void UpdateRacingSpeedLines(float speed)
    {
        if (racingSpeedLineImages.Count == 0)
        {
            return;
        }

        float speed01 = Mathf.InverseLerp(RacingMaxSpeed * 0.45f, RacingMaxSpeed, speed);
        float pulse = 0.7f + Mathf.Sin(Time.unscaledTime * 18f) * 0.3f;
        for (int i = 0; i < racingSpeedLineImages.Count; i++)
        {
            Image img = racingSpeedLineImages[i];
            if (img == null)
            {
                continue;
            }

            Color c = img.color;
            c.a = Mathf.Clamp01(speed01 * pulse) * (i % 3 == 0 ? 0.18f : 0.10f);
            img.color = c;
        }
    }

    private void SetupSpeedometer(Transform canvasParent, Font font)
    {
        const float size   = 160f;
        const float radius = 62f; // tick ring radius from center

        // Root вЂ” bottom-right corner
        RectTransform root = CreateUiObject("Speedometer", canvasParent).GetComponent<RectTransform>();
        root.anchorMin        = new Vector2(1f, 0f);
        root.anchorMax        = new Vector2(1f, 0f);
        root.pivot            = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-18f, 18f);
        root.sizeDelta        = new Vector2(size, size);

        Image bg = root.gameObject.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);
        Outline bgOutline = root.gameObject.AddComponent<Outline>();
        bgOutline.effectColor    = new Color(0.88f, 0.62f, 0.08f, 0.5f);
        bgOutline.effectDistance = new Vector2(2f, -2f);

        // Arc tick marks: 0вЂ“100 km/h в†’ -135В° to +135В° from vertical (CW positive)
        Color tickDim    = new Color(0.55f, 0.55f, 0.55f);
        Color tickBright = new Color(0.95f, 0.82f, 0.20f);
        int tickCount = 11; // 0, 10, 20 вЂ¦ 100
        for (int i = 0; i < tickCount; i++)
        {
            float t        = i / (float)(tickCount - 1);
            float angleDeg = -135f + t * 270f; // CW from vertical
            float angleRad = angleDeg * Mathf.Deg2Rad;

            RectTransform tick = CreateUiObject($"Tick{i}", root).GetComponent<RectTransform>();
            tick.anchorMin        = new Vector2(0.5f, 0.5f);
            tick.anchorMax        = new Vector2(0.5f, 0.5f);
            tick.pivot            = new Vector2(0.5f, 0f); // pivot at rim, extends inward
            tick.anchoredPosition = new Vector2(Mathf.Sin(angleRad) * radius, Mathf.Cos(angleRad) * radius);
            bool isMajor          = (i % 2 == 0);
            tick.sizeDelta        = new Vector2(isMajor ? 3f : 2f, isMajor ? 13f : 8f);
            tick.localEulerAngles = new Vector3(0f, 0f, -angleDeg);

            Image tickImg = tick.gameObject.AddComponent<Image>();
            tickImg.color = (i == 0 || i == tickCount - 1) ? tickBright : tickDim;
        }

        // Speed number labels at 0, 50, 100
        (int kmh, float angleDeg)[] labels = { (0, -135f), (50, 0f), (100, 135f) };
        foreach (var (lKmh, lAngle) in labels)
        {
            float rad = lAngle * Mathf.Deg2Rad;
            float lx  = Mathf.Sin(rad) * (radius + 16f);
            float ly  = Mathf.Cos(rad) * (radius + 16f);

            Text lbl = new GameObject($"Label{lKmh}").AddComponent<Text>();
            lbl.transform.SetParent(root, false);
            lbl.rectTransform.anchorMin        = new Vector2(0.5f, 0.5f);
            lbl.rectTransform.anchorMax        = new Vector2(0.5f, 0.5f);
            lbl.rectTransform.pivot            = new Vector2(0.5f, 0.5f);
            lbl.rectTransform.anchoredPosition = new Vector2(lx, ly);
            lbl.rectTransform.sizeDelta        = new Vector2(34f, 18f);
            lbl.font      = font;
            lbl.fontSize  = 11;
            lbl.color     = new Color(0.68f, 0.68f, 0.68f);
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.text      = lKmh.ToString();
        }

        // Needle (pivot at bottom-center so it rotates from center of gauge)
        RectTransform needle = CreateUiObject("Needle", root).GetComponent<RectTransform>();
        needle.anchorMin        = new Vector2(0.5f, 0.5f);
        needle.anchorMax        = new Vector2(0.5f, 0.5f);
        needle.pivot            = new Vector2(0.5f, 0.08f); // slightly below center for tail
        needle.anchoredPosition = new Vector2(0f, 0f);
        needle.sizeDelta        = new Vector2(3.5f, 58f);
        needle.localEulerAngles = new Vector3(0f, 0f, 150f); // starts at 0 km/h

        Image needleImg = needle.gameObject.AddComponent<Image>();
        needleImg.color = new Color(0.96f, 0.28f, 0.18f);

        racingSpeedometerNeedle = needle;

        // Center cap
        RectTransform cap = CreateUiObject("Cap", root).GetComponent<RectTransform>();
        cap.anchorMin        = new Vector2(0.5f, 0.5f);
        cap.anchorMax        = new Vector2(0.5f, 0.5f);
        cap.pivot            = new Vector2(0.5f, 0.5f);
        cap.anchoredPosition = Vector2.zero;
        cap.sizeDelta        = new Vector2(12f, 12f);
        cap.gameObject.AddComponent<Image>().color = new Color(0.86f, 0.22f, 0.16f);

        // Speed readout вЂ” center-bottom of gauge
        racingSpeedometerText = new GameObject("SpeedReadout").AddComponent<Text>();
        racingSpeedometerText.transform.SetParent(root, false);
        racingSpeedometerText.rectTransform.anchorMin        = new Vector2(0f, 0f);
        racingSpeedometerText.rectTransform.anchorMax        = new Vector2(1f, 0f);
        racingSpeedometerText.rectTransform.pivot            = new Vector2(0.5f, 0f);
        racingSpeedometerText.rectTransform.anchoredPosition = new Vector2(0f, 12f);
        racingSpeedometerText.rectTransform.sizeDelta        = new Vector2(0f, 28f);
        racingSpeedometerText.font      = font;
        racingSpeedometerText.fontSize  = 22;
        racingSpeedometerText.fontStyle = FontStyle.Bold;
        racingSpeedometerText.color     = new Color(0.95f, 0.90f, 0.84f);
        racingSpeedometerText.alignment = TextAnchor.MiddleCenter;
        racingSpeedometerText.text      = "0";

        // "km/h" sub-label
        Text unit = new GameObject("UnitLabel").AddComponent<Text>();
        unit.transform.SetParent(root, false);
        unit.rectTransform.anchorMin        = new Vector2(0f, 0f);
        unit.rectTransform.anchorMax        = new Vector2(1f, 0f);
        unit.rectTransform.pivot            = new Vector2(0.5f, 0f);
        unit.rectTransform.anchoredPosition = new Vector2(0f, 34f);
        unit.rectTransform.sizeDelta        = new Vector2(0f, 16f);
        unit.font      = font;
        unit.fontSize  = 10;
        unit.color     = new Color(0.55f, 0.55f, 0.55f);
        unit.alignment = TextAnchor.MiddleCenter;
        unit.text      = "km/h";

        // Gear indicator вЂ” sits above the speed readout in the center of the dial
        racingGearText = new GameObject("GearReadout").AddComponent<Text>();
        racingGearText.transform.SetParent(root, false);
        racingGearText.rectTransform.anchorMin        = new Vector2(0f, 1f);
        racingGearText.rectTransform.anchorMax        = new Vector2(1f, 1f);
        racingGearText.rectTransform.pivot            = new Vector2(0.5f, 1f);
        racingGearText.rectTransform.anchoredPosition = new Vector2(0f, -14f);
        racingGearText.rectTransform.sizeDelta        = new Vector2(0f, 30f);
        racingGearText.font      = font;
        racingGearText.fontSize  = 22;
        racingGearText.fontStyle = FontStyle.Bold;
        racingGearText.color     = new Color(0.95f, 0.90f, 0.84f);
        racingGearText.alignment = TextAnchor.MiddleCenter;
        racingGearText.text      = "1";

        RectTransform powerRoot = CreateUiObject("GearPowerRoot", root).GetComponent<RectTransform>();
        powerRoot.anchorMin        = new Vector2(0.12f, 0f);
        powerRoot.anchorMax        = new Vector2(0.88f, 0f);
        powerRoot.pivot            = new Vector2(0.5f, 0f);
        powerRoot.anchoredPosition = new Vector2(0f, 58f);
        powerRoot.sizeDelta        = new Vector2(0f, 18f);

        Image powerBg = powerRoot.gameObject.AddComponent<Image>();
        powerBg.color = new Color(0.03f, 0.035f, 0.055f, 0.88f);
        Outline powerOutline = powerRoot.gameObject.AddComponent<Outline>();
        powerOutline.effectColor = new Color(0.88f, 0.62f, 0.08f, 0.35f);
        powerOutline.effectDistance = new Vector2(1f, -1f);

        racingGearPowerFill = CreateUiObject("GearPowerFill", powerRoot).GetComponent<RectTransform>();
        racingGearPowerFill.anchorMin = new Vector2(0f, 0f);
        racingGearPowerFill.anchorMax = new Vector2(0f, 1f);
        racingGearPowerFill.pivot = new Vector2(0f, 0.5f);
        racingGearPowerFill.anchoredPosition = Vector2.zero;
        racingGearPowerFill.sizeDelta = new Vector2(0f, 0f);
        racingGearPowerFill.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.82f, 0.28f, 0.95f);

        racingGearPowerText = new GameObject("GearPowerText").AddComponent<Text>();
        racingGearPowerText.transform.SetParent(powerRoot, false);
        racingGearPowerText.rectTransform.anchorMin = Vector2.zero;
        racingGearPowerText.rectTransform.anchorMax = Vector2.one;
        racingGearPowerText.rectTransform.offsetMin = Vector2.zero;
        racingGearPowerText.rectTransform.offsetMax = Vector2.zero;
        racingGearPowerText.font = font;
        racingGearPowerText.fontSize = 9;
        racingGearPowerText.fontStyle = FontStyle.Bold;
        racingGearPowerText.color = new Color(0.96f, 0.92f, 0.72f, 1f);
        racingGearPowerText.alignment = TextAnchor.MiddleCenter;
        racingGearPowerText.text = "ACCEL";
    }

    private void UpdateRacingGearAccelHud()
    {
        if (racingGearPowerFill != null)
        {
            RectTransform parent = racingGearPowerFill.parent as RectTransform;
            float fullWidth = parent != null ? parent.rect.width : 92f;
            racingGearPowerFill.sizeDelta = new Vector2(fullWidth * Mathf.Clamp01(racingGearAccel01), 0f);

            if (racingGearPowerFill.TryGetComponent(out Image fill))
            {
                float load = Mathf.Clamp01(racingGearAccel01);
                fill.color = load > 0.78f
                    ? new Color(0.92f, 0.12f, 0.08f, 0.96f)
                    : Color.Lerp(
                        new Color(0.20f, 0.82f, 0.28f, 0.95f),
                        new Color(0.95f, 0.72f, 0.12f, 0.95f),
                        Mathf.InverseLerp(0.35f, 0.78f, load));
            }
        }

        if (racingGearPowerText != null)
        {
            racingGearPowerText.text = racingCurrentGear == 0
                ? "REVERSE"
                : racingGearAccel01 > 0.82f
                    ? "SHIFT!"
                    : $"ACCEL {(racingGearAccel01 * 100f):F0}%";
        }
    }

    // в”Ђв”Ђ Racing world population в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

}
