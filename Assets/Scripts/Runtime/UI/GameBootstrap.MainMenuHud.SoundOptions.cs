using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void CreateSoundOptionsPanel(RectTransform screenTint, Font uiFont)
    {
        RectTransform panel = CreateStyledPanel("SoundOptionsPanel", screenTint, new Color(0.07f, 0.10f, 0.15f, 0.97f));
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 0f);
        panel.pivot = new Vector2(0f, 0f);
        panel.anchoredPosition = new Vector2(428f, 34f);
        panel.sizeDelta = new Vector2(880f, 588f);
        mainMenuHud.SoundOptionsRoot = panel.gameObject;
        mainMenuHud.SoundOptionsRoot.SetActive(false);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("SoundOptionsHeader", panel, 38f, 12f);
        mainMenuHud.SoundOptionsTitleText = CreateHeaderText("SoundOptionsTitle", headerRow, uiFont, GetSoundOptionsTitle(), 22, TextAnchor.MiddleLeft, Color.white);
        mainMenuHud.SoundOptionsTitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        Button closeButton = CreateButton("SoundOptionsClose", headerRow, uiFont, out mainMenuHud.SoundOptionsCloseText, "X", 14, new Color(0.34f, 0.12f, 0.12f, 1f), Color.white);
        closeButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 44f;
        closeButton.onClick.AddListener(CloseSoundOptionsPanel);
        mainMenuHud.SoundOptionsCloseText.raycastTarget = false;

        mainMenuHud.SoundOptionsHintText = CreateBodyText("SoundOptionsHint", panel, uiFont, GetSoundOptionsHint(), 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        mainMenuHud.SoundOptionsHintText.raycastTarget = false;
        mainMenuHud.SoundOptionsHintText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        FleetCanvasUiFactory.ScrollPanelRefs scroll = CreateVerticalScrollList(
            "SoundOptionsScroll",
            panel,
            "SoundOptionsContent",
            8f,
            32f,
            preferredHeight: 428f,
            flexibleHeight: 1f);
        Image scrollImage = scroll.Root.gameObject.AddComponent<Image>();
        scrollImage.color = new Color(0.04f, 0.06f, 0.09f, 0.72f);
        scrollImage.raycastTarget = true;
        mainMenuHud.SoundOptionsContentRoot = scroll.Content;

        RectTransform footerRow = CreateLayoutRow("SoundOptionsFooter", panel, 38f, 12f);
        mainMenuHud.SoundOptionsCountText = CreateBodyText("SoundOptionsCount", footerRow, uiFont, GetSoundOptionsCountLabel(), 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        mainMenuHud.SoundOptionsCountText.raycastTarget = false;
        mainMenuHud.SoundOptionsCountText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        Button resetAllButton = CreateButton("SoundOptionsResetAll", footerRow, uiFont, out mainMenuHud.SoundOptionsResetAllText, GetSoundResetAllLabel(), 13, new Color(0.36f, 0.20f, 0.08f, 1f), Color.white);
        resetAllButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 150f;
        resetAllButton.onClick.AddListener(ResetAllSoundOptionVolumes);
        mainMenuHud.SoundOptionsResetAllText.raycastTarget = false;

        RebuildSoundOptionsRows(uiFont);
    }

    private void RebuildSoundOptionsRows(Font uiFont)
    {
        if (mainMenuHud?.SoundOptionsContentRoot == null)
        {
            return;
        }

        for (int i = mainMenuHud.SoundOptionsContentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(mainMenuHud.SoundOptionsContentRoot.GetChild(i).gameObject);
        }
        mainMenuHud.SoundOptionRows.Clear();

        string currentCategory = null;
        foreach (SoundOptionEntry entry in soundOptionEntries)
        {
            if (entry.Category != currentCategory)
            {
                currentCategory = entry.Category;
                Text section = CreateBodyText($"SoundSection_{currentCategory}", mainMenuHud.SoundOptionsContentRoot, uiFont,
                    GetSoundCategoryLabel(currentCategory), 13, TextAnchor.MiddleLeft, FleetAccentColor);
                section.fontStyle = FontStyle.Bold;
                section.raycastTarget = false;
                section.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
            }

            CreateSoundOptionRow(entry, mainMenuHud.SoundOptionsContentRoot, uiFont);
        }

        RefreshSoundOptionsPanelUI();
    }

    private void CreateSoundOptionRow(SoundOptionEntry entry, Transform parent, Font uiFont)
    {
        RectTransform row = CreateStyledPanel($"SoundRow_{entry.Id}", parent, new Color(0.10f, 0.14f, 0.20f, 0.94f));
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RectTransform labelStack = CreateVerticalStack($"SoundLabel_{entry.Id}", row, new RectOffset(), 2f, preferredWidth: 280f);
        Text nameText = CreateBodyText("Name", labelStack, uiFont, entry.Label, 13, TextAnchor.MiddleLeft, Color.white);
        nameText.fontStyle = FontStyle.Bold;
        nameText.raycastTarget = false;
        nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        Text metaText = CreateBodyText("Meta", labelStack, uiFont, string.Empty, 10, TextAnchor.MiddleLeft, FleetMutedTextColor);
        metaText.raycastTarget = false;
        metaText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        CreateSpacer($"SoundSpacer_{entry.Id}", row, flexibleWidth: 1f);

        Slider slider = CreateSoundVolumeSlider($"SoundSlider_{entry.Id}", row);
        slider.SetValueWithoutNotify(Mathf.Clamp01(entry.Volume));
        string id = entry.Id;
        slider.onValueChanged.AddListener(value => SetSoundOptionVolume(id, value));

        Text volumeText = CreateBodyText($"SoundVolume_{entry.Id}", row, uiFont, "100%", 12, TextAnchor.MiddleCenter, Color.white);
        volumeText.raycastTarget = false;
        volumeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 54f;

        Button playButton = CreateSoundActionButton($"SoundPlay_{entry.Id}", row, uiFont, out Text playText, GetSoundPlayLabel(), FleetPrimaryButtonColor);
        playButton.onClick.AddListener(() => PreviewSoundOption(id));

        Button resetButton = CreateSoundActionButton($"SoundReset_{entry.Id}", row, uiFont, out Text resetText, GetSoundResetLabel(), new Color(0.32f, 0.35f, 0.42f, 1f));
        resetButton.onClick.AddListener(() => ResetSoundOptionVolume(id));

        mainMenuHud.SoundOptionRows[entry.Id] = new SoundOptionRowRefs
        {
            NameText = nameText,
            MetaText = metaText,
            VolumeText = volumeText,
            PlayText = playText,
            ResetText = resetText,
            VolumeSlider = slider
        };
    }

    private static Slider CreateSoundVolumeSlider(string name, Transform parent)
    {
        GameObject root = new(name, typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        LayoutElement rootLayout = root.AddComponent<LayoutElement>();
        rootLayout.preferredWidth = 260f;
        rootLayout.minWidth = 260f;
        rootLayout.flexibleWidth = 0f;
        rootLayout.preferredHeight = 30f;

        Slider slider = root.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        GameObject background = new("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(root.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        StretchRect(backgroundRect, 0f, 8f, 0f, 8f);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0.03f, 0.04f, 0.07f, 1f);
        backgroundImage.raycastTarget = true;

        GameObject fillArea = new("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        StretchRect(fillAreaRect, 4f, 10f, 4f, 10f);

        GameObject fill = new("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        StretchRect(fillRect, 0f, 0f, 0f, 0f);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = FleetAccentColor;
        fillImage.raycastTarget = false;

        GameObject handleArea = new("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        StretchRect(handleAreaRect, 0f, 4f, 0f, 4f);

        GameObject handle = new("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 22f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.96f, 0.84f, 0.32f, 1f);
        handleImage.raycastTarget = true;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Button CreateSoundActionButton(string name, Transform parent, Font uiFont, out Text label, string text, Color normalColor)
    {
        Button button = CreateButton(name, parent, uiFont, out label, text, 12, normalColor, Color.white);
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 86f;
        layout.minWidth = 86f;
        layout.flexibleWidth = 0f;
        layout.preferredHeight = 34f;

        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            image.raycastTarget = true;
            image.color = normalColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.22f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        label.fontStyle = FontStyle.Bold;
        label.raycastTarget = false;
        return button;
    }

    private void ToggleSoundOptionsPanel()
    {
        if (mainMenuHud?.SoundOptionsRoot == null)
        {
            return;
        }

        bool show = !mainMenuHud.SoundOptionsRoot.activeSelf;
        mainMenuHud.SoundOptionsRoot.SetActive(show);
        if (show)
        {
            if (mainMenuHud.GraphicsOptionsRoot != null)
            {
                mainMenuHud.GraphicsOptionsRoot.SetActive(false);
            }
            mainMenuHud.SoundOptionsRoot.transform.SetAsLastSibling();
            RefreshSoundOptionsPanelUI();
        }
        else
        {
            StopSoundPreviewLoop();
        }

        PlayUiSound(show ? uiPanelOpenClip : uiPanelCloseClip, 0.9f);
    }

    private void CloseSoundOptionsPanel()
    {
        if (mainMenuHud?.SoundOptionsRoot == null)
        {
            return;
        }

        mainMenuHud.SoundOptionsRoot.SetActive(false);
        StopSoundPreviewLoop();
        PlayUiSound(uiPanelCloseClip, 0.85f);
    }

    private void RefreshSoundOptionsPanelUI()
    {
        if (mainMenuHud?.SoundOptionRows == null || soundOptionEntries.Count == 0)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        if (mainMenuHud.SoundOptionsTitleText != null)
        {
            mainMenuHud.SoundOptionsTitleText.text = GetSoundOptionsTitle();
        }
        if (mainMenuHud.SoundOptionsHintText != null)
        {
            mainMenuHud.SoundOptionsHintText.text = GetSoundOptionsHint();
        }
        if (mainMenuHud.SoundOptionsCountText != null)
        {
            mainMenuHud.SoundOptionsCountText.text = GetSoundOptionsCountLabel();
        }
        if (mainMenuHud.SoundOptionsCloseText != null)
        {
            mainMenuHud.SoundOptionsCloseText.text = "X";
        }
        if (mainMenuHud.SoundOptionsResetAllText != null)
        {
            mainMenuHud.SoundOptionsResetAllText.text = GetSoundResetAllLabel();
        }

        foreach (SoundOptionEntry entry in soundOptionEntries)
        {
            if (!mainMenuHud.SoundOptionRows.TryGetValue(entry.Id, out SoundOptionRowRefs row))
            {
                continue;
            }

            if (row.NameText != null)
            {
                row.NameText.text = entry.Label;
            }
            if (row.MetaText != null)
            {
                string kind = entry.IsLoop
                    ? (ru ? "\u043f\u0435\u0442\u043b\u044f" : "loop")
                    : "SFX";
                row.MetaText.text = $"{GetSoundCategoryLabel(entry.Category)} / {entry.Clip.name} / {kind}";
            }
            if (row.VolumeText != null)
            {
                row.VolumeText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(entry.Volume) * 100f)}%";
            }
            if (row.PlayText != null)
            {
                row.PlayText.text = GetSoundPlayLabel();
            }
            if (row.ResetText != null)
            {
                row.ResetText.text = GetSoundResetLabel();
            }
            if (row.VolumeSlider != null)
            {
                row.VolumeSlider.SetValueWithoutNotify(Mathf.Clamp01(entry.Volume));
            }
        }
    }

    private string GetSoundOptionsTitle() => IsRussianLanguage() ? "\u0417\u0432\u0443\u043a" : "Sound";

    private string GetSoundOptionsHint()
    {
        return IsRussianLanguage()
            ? "\u0414\u0435\u0431\u0430\u0433-\u0441\u043f\u0438\u0441\u043e\u043a SFX \u0438 \u043c\u0443\u0437\u044b\u043a\u0438: Play \u043f\u0440\u043e\u0438\u0433\u0440\u044b\u0432\u0430\u0435\u0442, \u0441\u043b\u0430\u0439\u0434\u0435\u0440 \u043c\u0435\u043d\u044f\u0435\u0442 \u0433\u0440\u043e\u043c\u043a\u043e\u0441\u0442\u044c \u0438 \u0441\u0440\u0430\u0437\u0443 \u0441\u043e\u0445\u0440\u0430\u043d\u044f\u0435\u0442."
            : "Debug list of SFX and music: Play previews a sound, the slider changes its volume and saves immediately.";
    }

    private string GetSoundOptionsCountLabel()
    {
        return IsRussianLanguage()
            ? $"{soundOptionEntries.Count} \u0437\u0432\u0443\u043a\u043e\u0432 / \u0442\u0435\u043c"
            : $"{soundOptionEntries.Count} sounds / themes";
    }

    private string GetSoundPlayLabel() => IsRussianLanguage() ? "Play" : "Play";
    private string GetSoundResetLabel() => IsRussianLanguage() ? "\u0421\u0431\u0440\u043e\u0441" : "Reset";
    private string GetSoundResetAllLabel() => IsRussianLanguage() ? "\u0421\u0431\u0440\u043e\u0441\u0438\u0442\u044c \u0432\u0441\u0435" : "Reset All";

    private string GetSoundCategoryLabel(string category)
    {
        if (!IsRussianLanguage())
        {
            return category;
        }

        return category switch
        {
            "UI" => "UI",
            "Economy" => "\u042d\u043a\u043e\u043d\u043e\u043c\u0438\u043a\u0430",
            "Gambling" => "\u0410\u0437\u0430\u0440\u0442\u043d\u044b\u0435",
            "Transport" => "\u0422\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442",
            "Routes" => "\u041c\u0430\u0440\u0448\u0440\u0443\u0442\u044b",
            "Ambient" => "\u0410\u0442\u043c\u043e\u0441\u0444\u0435\u0440\u0430",
            "Music" => "\u041c\u0443\u0437\u044b\u043a\u0430",
            _ => category
        };
    }
}
