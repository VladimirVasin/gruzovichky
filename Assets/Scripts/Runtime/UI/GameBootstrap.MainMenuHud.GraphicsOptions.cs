using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void CreateGraphicsOptionsPanel(RectTransform screenTint, Font uiFont)
    {
        bool ru = IsRussianLanguage();
        RectTransform panel = CreateStyledPanel("GraphicsOptionsPanel", screenTint, FleetPanelColor);
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 0f);
        panel.pivot = new Vector2(0f, 0f);
        panel.anchoredPosition = new Vector2(428f, 34f);
        panel.sizeDelta = new Vector2(690f, 548f);
        mainMenuHud.GraphicsOptionsRoot = panel.gameObject;
        mainMenuHud.GraphicsOptionsRoot.SetActive(false);

        VerticalLayoutGroup vl = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(18, 18, 14, 14);
        vl.spacing = 8f;
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        Text title = CreateBodyText("GfxTitle", panel, uiFont,
            ru ? "Настройки графики" : "Graphics Settings",
            18, TextAnchor.MiddleLeft, new Color(0.94f, 0.97f, 1f));
        title.fontStyle = FontStyle.Bold;
        title.raycastTarget = false;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        Text hint = CreateBodyText("GfxHint", panel, uiFont,
            ru
                ? "Значения применяются сразу. Дефолт - обычный вид, 100 - усиленный максимум."
                : "Values apply immediately. Defaults are the normal look; 100 is boosted maximum.",
            12, TextAnchor.MiddleLeft, new Color(0.70f, 0.78f, 0.86f));
        hint.raycastTarget = false;
        hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        CreateGfxSectionLabel("GfxPostFxSection", panel, uiFont, ru ? "Постэффекты" : "Post Effects");
        CreateGfxOptionRow("GfxBloomRow", panel, uiFont, ru ? "Bloom: сила свечения" : "Bloom intensity", -1, 0);
        CreateGfxOptionRow("GfxBloomReachRow", panel, uiFont, ru ? "Bloom: дальность" : "Bloom reach", -1, 1);
        CreateGfxOptionRow("GfxSatRow", panel, uiFont, ru ? "Насыщенность" : "Saturation", -1, 2);
        CreateGfxOptionRow("GfxContrastRow", panel, uiFont, ru ? "Контраст" : "Contrast", -1, 3);
        CreateGfxOptionRow("GfxWarmthRow", panel, uiFont, ru ? "Теплота цвета" : "Color warmth", -1, 4);
        CreateGfxOptionRow("GfxVignetteRow", panel, uiFont, ru ? "Виньетка" : "Vignette", -1, 5);

        CreateGfxSectionLabel("GfxToggleSection", panel, uiFont, ru ? "Включение эффектов" : "Effect Toggles");
        CreateGfxOptionRow("GfxDofRow", panel, uiFont, ru ? "Глубина резкости" : "Depth of Field", 3, 6);
        CreateGfxOptionRow("GfxGrainRow", panel, uiFont, ru ? "Зерно пленки" : "Film Grain", 0, 7);
        CreateGfxOptionRow("GfxSmhRow", panel, uiFont, ru ? "Цветокоррекция" : "Color Grading", 1, -1);
        CreateGfxOptionRow("GfxChromRow", panel, uiFont, ru ? "Хроматическая аберрация" : "Chromatic Aberration", 2, 8);

        Button resetButton = CreateButton("GfxResetDefaultsButton", panel, uiFont, out Text resetText,
            ru ? "Сбросить на дефолт" : "Reset to Defaults",
            14, new Color(0.36f, 0.20f, 0.08f, 1f), Color.white);
        resetButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
        resetText.fontStyle = FontStyle.Bold;
        resetText.raycastTarget = false;
        resetButton.onClick.AddListener(ResetGraphicsOptionsToDefaults);

        RefreshGraphicsOptionsPanelUI();
    }

    private static void CreateGfxSectionLabel(string name, Transform parent, Font uiFont, string labelText)
    {
        Text label = CreateBodyText(name, parent, uiFont, labelText, 13, TextAnchor.MiddleLeft, new Color(1f, 0.80f, 0.25f));
        label.fontStyle = FontStyle.Bold;
        label.raycastTarget = false;
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
    }

    private void CreateGfxOptionRow(string name, Transform parent, Font uiFont, string labelText, int toggleIndex, int valueIndex)
    {
        RectTransform row = CreateLayoutRow(name, parent, 38f, 8f);
        Image rowImage = row.gameObject.AddComponent<Image>();
        rowImage.color = new Color(0.10f, 0.14f, 0.20f, 0.92f);
        rowImage.raycastTarget = false;

        if (toggleIndex >= 0)
        {
            int idx = toggleIndex;
            mainMenuHud.GfxToggleButtons[idx] = CreateGfxToggleButton($"{name}Toggle", row, uiFont, out mainMenuHud.GfxToggleTexts[idx]);
            mainMenuHud.GfxToggleButtons[idx].onClick.AddListener(() => OnGfxToggle(idx));
        }
        else
        {
            CreateGfxSpacer(row, 104f, 32f);
        }

        Text label = CreateBodyText($"{name}Label", row, uiFont, labelText, 13, TextAnchor.MiddleLeft, new Color(0.86f, 0.91f, 0.97f));
        label.raycastTarget = false;
        label.gameObject.AddComponent<LayoutElement>().preferredWidth = 285f;

        if (valueIndex >= 0)
        {
            CreateGfxNumberStepper($"{name}Stepper", row, uiFont, valueIndex);
        }
        else
        {
            CreateGfxSpacer(row, 188f, 32f);
        }
    }

    private static void CreateGfxSpacer(Transform parent, float width, float height)
    {
        GameObject go = new("GfxSpacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        layout.flexibleWidth = 0f;
    }

    private static Button CreateGfxToggleButton(string name, Transform parent, Font uiFont, out Text label)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = 104f;
        layout.preferredHeight = 32f;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.14f, 0.42f, 0.22f, 1f);
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.22f, 0.58f, 0.32f, 1f);
        colors.pressedColor = new Color(0.08f, 0.20f, 0.12f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.15f, 0.15f, 0.16f, 0.75f);
        button.colors = colors;
        button.interactable = true;

        label = CreateBodyText("Label", go.transform, uiFont, "ON", 13, TextAnchor.MiddleCenter, Color.white);
        StretchRect(label.rectTransform, 0f, 0f, 0f, 0f);
        label.fontStyle = FontStyle.Bold;
        label.raycastTarget = false;
        return button;
    }

    private void CreateGfxNumberStepper(string name, Transform parent, Font uiFont, int valueIndex)
    {
        RectTransform root = CreateLayoutRow(name, parent, 32f, 5f);
        root.gameObject.AddComponent<LayoutElement>().preferredWidth = 188f;

        Button minus = CreateButton($"{name}Minus", root, uiFont, out Text minusText, "-", 13, new Color(0.16f, 0.19f, 0.26f, 1f), Color.white);
        minus.gameObject.AddComponent<LayoutElement>().preferredWidth = 40f;
        minusText.raycastTarget = false;
        minus.onClick.AddListener(() => StepGfxValue(valueIndex, -5f));
        mainMenuHud.GfxMinusButtons[valueIndex] = minus;

        InputField field = CreateGfxNumberInput($"{name}Value", root, uiFont);
        field.onEndEdit.AddListener(value => SetGfxValueFromText(valueIndex, value));
        mainMenuHud.GfxValueFields[valueIndex] = field;

        Button plus = CreateButton($"{name}Plus", root, uiFont, out Text plusText, "+", 13, new Color(0.16f, 0.19f, 0.26f, 1f), Color.white);
        plus.gameObject.AddComponent<LayoutElement>().preferredWidth = 40f;
        plusText.raycastTarget = false;
        plus.onClick.AddListener(() => StepGfxValue(valueIndex, 5f));
        mainMenuHud.GfxPlusButtons[valueIndex] = plus;
    }

    private static InputField CreateGfxNumberInput(string name, Transform parent, Font uiFont)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 88f;
        le.preferredHeight = 32f;

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.07f, 0.09f, 0.13f, 1f);
        bg.raycastTarget = true;

        Text text = CreateBodyText("Text", go.transform, uiFont, "100", 13, TextAnchor.MiddleCenter, Color.white);
        StretchRect(text.rectTransform, 4f, 4f, 4f, 4f);
        text.supportRichText = false;
        text.raycastTarget = false;

        InputField input = go.GetComponent<InputField>();
        input.textComponent = text;
        input.targetGraphic = bg;
        input.contentType = InputField.ContentType.IntegerNumber;
        input.lineType = InputField.LineType.SingleLine;
        input.characterValidation = InputField.CharacterValidation.Integer;
        input.caretColor = Color.white;
        input.selectionColor = new Color(0.40f, 0.58f, 0.90f, 0.45f);
        return input;
    }

    private void OnGfxToggle(int index)
    {
        switch (index)
        {
            case 0: gfxFilmGrainEnabled = !gfxFilmGrainEnabled; break;
            case 1: gfxSmhEnabled = !gfxSmhEnabled; break;
            case 2: gfxChromAberrEnabled = !gfxChromAberrEnabled; break;
            case 3: gfxDepthOfFieldEnabled = !gfxDepthOfFieldEnabled; break;
        }
        SessionDebugLogger.Log("UI", $"Graphics toggle {index} clicked. Grain={gfxFilmGrainEnabled}, Color={gfxSmhEnabled}, Aberration={gfxChromAberrEnabled}, DoF={gfxDepthOfFieldEnabled}.");
        SaveGraphicsPrefs();
        ApplyGraphicsOptionsNow();
        RefreshGraphicsOptionsPanelUI();
    }

    private void ToggleGraphicsOptionsPanel()
    {
        if (mainMenuHud?.GraphicsOptionsRoot == null) return;
        bool show = !mainMenuHud.GraphicsOptionsRoot.activeSelf;
        mainMenuHud.GraphicsOptionsRoot.SetActive(show);
        if (show)
        {
            if (mainMenuHud.SoundOptionsRoot != null)
            {
                mainMenuHud.SoundOptionsRoot.SetActive(false);
                StopSoundPreviewLoop();
            }
            mainMenuHud.GraphicsOptionsRoot.transform.SetAsLastSibling();
            RefreshGraphicsOptionsPanelUI();
        }
        PlayUiSound(show ? uiPanelOpenClip : uiPanelCloseClip, 0.9f);
    }

    private void RefreshGraphicsOptionsPanelUI()
    {
        if (mainMenuHud?.GfxToggleButtons == null) return;
        bool[] enabled = { gfxFilmGrainEnabled, gfxSmhEnabled, gfxChromAberrEnabled, gfxDepthOfFieldEnabled };
        Color onColor = new Color(0.14f, 0.42f, 0.22f, 1f);
        Color offColor = new Color(0.28f, 0.28f, 0.32f, 1f);
        for (int i = 0; i < mainMenuHud.GfxToggleButtons.Length; i++)
        {
            if (mainMenuHud.GfxToggleButtons[i] == null) continue;
            mainMenuHud.GfxToggleTexts[i].text = enabled[i] ? "ON" : "OFF";
            mainMenuHud.GfxToggleButtons[i].interactable = true;
            ColorBlock cb = mainMenuHud.GfxToggleButtons[i].colors;
            cb.normalColor = enabled[i] ? onColor : offColor;
            cb.highlightedColor = enabled[i] ? new Color(0.18f, 0.52f, 0.28f, 1f) : new Color(0.36f, 0.36f, 0.42f, 1f);
            cb.pressedColor = new Color(0.10f, 0.12f, 0.16f, 1f);
            cb.selectedColor = cb.highlightedColor;
            mainMenuHud.GfxToggleButtons[i].colors = cb;
            if (mainMenuHud.GfxToggleButtons[i].targetGraphic is Image image)
            {
                image.raycastTarget = true;
                image.color = enabled[i] ? onColor : offColor;
            }
        }
        for (int i = 0; i < mainMenuHud.GfxValueFields.Length; i++)
        {
            SetGfxInputText(i, GetGfxValue(i));
        }
    }

    private void SetGfxInputText(int valueIndex, float value)
    {
        if (mainMenuHud?.GfxValueFields == null || valueIndex < 0 || valueIndex >= mainMenuHud.GfxValueFields.Length)
        {
            return;
        }

        InputField input = mainMenuHud.GfxValueFields[valueIndex];
        if (input == null) return;
        input.SetTextWithoutNotify(Mathf.RoundToInt(Mathf.Clamp01(value) * 100f).ToString(CultureInfo.InvariantCulture));
    }

    private void StepGfxValue(int valueIndex, float delta)
    {
        SetGfxPercentValue(valueIndex, Mathf.RoundToInt(GetGfxValue(valueIndex) * 100f + delta));
    }

    private void SetGfxValueFromText(int valueIndex, string rawValue)
    {
        if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) &&
            !int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
        {
            RefreshGraphicsOptionsPanelUI();
            return;
        }

        SetGfxPercentValue(valueIndex, value);
    }

    private float GetGfxValue(int valueIndex)
    {
        return valueIndex switch
        {
            0 => gfxBloomIntensity,
            1 => gfxBloomReach,
            2 => gfxSaturation,
            3 => gfxContrast,
            4 => gfxWarmth,
            5 => gfxVignette,
            6 => gfxDepthOfFieldAmount,
            7 => gfxFilmGrainIntensity,
            8 => gfxChromAberrIntensity,
            _ => 1f,
        };
    }

    private void SetGfxPercentValue(int valueIndex, int percent)
    {
        SetGfxValue(valueIndex, Mathf.Clamp(percent, 0, 100) / 100f);
    }

    private void SetGfxValue(int valueIndex, float value)
    {
        value = Mathf.Clamp01(value);
        switch (valueIndex)
        {
            case 0: gfxBloomIntensity = value; break;
            case 1: gfxBloomReach = value; break;
            case 2: gfxSaturation = value; break;
            case 3: gfxContrast = value; break;
            case 4: gfxWarmth = value; break;
            case 5: gfxVignette = value; break;
            case 6: gfxDepthOfFieldAmount = value; break;
            case 7: gfxFilmGrainIntensity = value; break;
            case 8: gfxChromAberrIntensity = value; break;
        }

        SaveGraphicsPrefs();
        ApplyGraphicsOptionsNow();
        RefreshGraphicsOptionsPanelUI();
    }

    private void ResetGraphicsOptionsToDefaults()
    {
        gfxFilmGrainEnabled = true;
        gfxSmhEnabled = true;
        gfxChromAberrEnabled = true;
        gfxDepthOfFieldEnabled = true;
        gfxBloomIntensity = GfxDefaultBloomIntensity;
        gfxBloomReach = GfxDefaultBloomReach;
        gfxSaturation = GfxDefaultSaturation;
        gfxContrast = GfxDefaultContrast;
        gfxWarmth = GfxDefaultWarmth;
        gfxVignette = GfxDefaultVignette;
        gfxDepthOfFieldAmount = GfxDefaultDepthOfField;
        gfxFilmGrainIntensity = GfxDefaultFilmGrain;
        gfxChromAberrIntensity = GfxDefaultChromAberr;

        SessionDebugLogger.Log("UI", "Graphics settings reset to defaults.");
        SaveGraphicsPrefs();
        ApplyGraphicsOptionsNow();
        RefreshGraphicsOptionsPanelUI();
    }

    private void LoadGraphicsPrefs()
    {
        gfxFilmGrainEnabled = PlayerPrefs.GetInt("gfx_grain_on", 1) == 1;
        gfxSmhEnabled = PlayerPrefs.GetInt("gfx_smh_on", 1) == 1;
        gfxChromAberrEnabled = PlayerPrefs.GetInt("gfx_chrom_on", 1) == 1;
        gfxDepthOfFieldEnabled = PlayerPrefs.GetInt("gfx_dof_on", 1) == 1;
        gfxBloomIntensity = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_bloom_intensity_v2", GfxDefaultBloomIntensity));
        gfxBloomReach = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_bloom_reach_v2", GfxDefaultBloomReach));
        gfxSaturation = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_saturation_v2", GfxDefaultSaturation));
        gfxContrast = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_contrast_v2", GfxDefaultContrast));
        gfxWarmth = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_warmth_v2", GfxDefaultWarmth));
        gfxVignette = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_vignette_v2", GfxDefaultVignette));
        gfxDepthOfFieldAmount = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_dof_amount_v2", GfxDefaultDepthOfField));
        gfxFilmGrainIntensity = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_grain_percent_v2", GfxDefaultFilmGrain));
        gfxChromAberrIntensity = Mathf.Clamp01(PlayerPrefs.GetFloat("gfx_chrom_percent_v2", GfxDefaultChromAberr));
    }

    private void SaveGraphicsPrefs()
    {
        PlayerPrefs.SetInt("gfx_grain_on", gfxFilmGrainEnabled ? 1 : 0);
        PlayerPrefs.SetInt("gfx_smh_on", gfxSmhEnabled ? 1 : 0);
        PlayerPrefs.SetInt("gfx_chrom_on", gfxChromAberrEnabled ? 1 : 0);
        PlayerPrefs.SetInt("gfx_dof_on", gfxDepthOfFieldEnabled ? 1 : 0);
        PlayerPrefs.SetFloat("gfx_bloom_intensity_v2", gfxBloomIntensity);
        PlayerPrefs.SetFloat("gfx_bloom_reach_v2", gfxBloomReach);
        PlayerPrefs.SetFloat("gfx_saturation_v2", gfxSaturation);
        PlayerPrefs.SetFloat("gfx_contrast_v2", gfxContrast);
        PlayerPrefs.SetFloat("gfx_warmth_v2", gfxWarmth);
        PlayerPrefs.SetFloat("gfx_vignette_v2", gfxVignette);
        PlayerPrefs.SetFloat("gfx_dof_amount_v2", gfxDepthOfFieldAmount);
        PlayerPrefs.SetFloat("gfx_grain_percent_v2", gfxFilmGrainIntensity);
        PlayerPrefs.SetFloat("gfx_chrom_percent_v2", gfxChromAberrIntensity);
        PlayerPrefs.Save();
    }
}
