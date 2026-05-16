using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static bool TryParseBuildCatalogTool(BuildCatalogItemData item, out BuildTool tool)
    {
        tool = BuildTool.None;
        return item != null
               && !string.IsNullOrWhiteSpace(item.tool)
               && System.Enum.TryParse(item.tool, out tool)
               && tool != BuildTool.None;
    }

    private bool TryGetBuildCatalogItem(BuildTool tool, out BuildCatalogItemData item)
    {
        GetBuildCatalogData();
        item = null;
        return cachedBuildCatalogItems != null && cachedBuildCatalogItems.TryGetValue(tool, out item);
    }

    private int GetBuildToolCost(BuildTool tool)
    {
        int baseCost = TryGetBuildCatalogItem(tool, out BuildCatalogItemData item)
            ? Mathf.Max(0, item.cost)
            : 0;
        return ApplyCityUpgradeBuildCostDiscount(baseCost);
    }

    private bool CanAffordBuildTool(BuildTool tool)
    {
        int cost = GetBuildToolCost(tool);
        return cost <= 0 || money >= cost;
    }

    private string GetBuildToolCostLabel(BuildTool tool, bool unaffordable = false)
    {
        int cost = GetBuildToolCost(tool);
        if (cost <= 0)
        {
            return string.Empty;
        }

        string amountColor = unaffordable ? "#FF6E66" : "#FFFFFF";
        return $"<color=#F5B53A>$</color> <color={amountColor}>{cost}</color>";
    }

    private string GetBuildToolNoFundsStatus(bool ru)
    {
        return ru ? "\u041d\u0435\u0442 \u0434\u0435\u043d\u0435\u0433" : "No funds";
    }

    private bool TryRejectBuildToolForInsufficientFunds(BuildTool tool)
    {
        int cost = GetBuildToolCost(tool);
        if (cost <= 0 || money >= cost)
        {
            return false;
        }

        string titleEn = GetBuildCatalogTitle(tool, false, tool.ToString());
        string titleRu = GetBuildCatalogTitle(tool, true, titleEn);
        PushFeedEvent(
            $"Not enough treasury for {titleEn}: ${money}/${cost}.",
            $"\u041d\u0435 \u0445\u0432\u0430\u0442\u0430\u0435\u0442 \u0434\u0435\u043d\u0435\u0433 \u043d\u0430 {titleRu}: ${money}/${cost}.",
            FeedEventType.Warning);
        PlayUiSound(uiDeniedClip != null ? uiDeniedClip : uiPanelCloseClip, 0.62f);
        activeBuildTool = BuildTool.None;
        hoveredBuildCell = null;
        HideBuildHoverHighlights();
        isBuildScreenDirty = true;
        RefreshSelectionVisuals();
        SessionDebugLogger.Log("BUILD", $"Build rejected: not enough treasury for {tool}; cost=${cost}, treasury=${money}.");
        return true;
    }

    private string AppendBuildToolCostDescription(BuildTool tool, string description, bool ru)
    {
        int cost = GetBuildToolCost(tool);
        if (cost <= 0)
        {
            return description;
        }

        string costLine = CanAffordBuildTool(tool)
            ? ru ? $"\u0421\u0442\u043e\u0438\u043c\u043e\u0441\u0442\u044c: ${cost}." : $"Cost: ${cost}."
            : ru ? $"\u0421\u0442\u043e\u0438\u043c\u043e\u0441\u0442\u044c: ${cost}. \u0412 \u043a\u0430\u0437\u043d\u0435 ${money}."
                 : $"Cost: ${cost}. Treasury has ${money}.";

        return string.IsNullOrWhiteSpace(description)
            ? costLine
            : description + "\n" + costLine;
    }

    private static bool IsBuildToolTemporarilyUnavailable(BuildTool tool)
    {
        return tool == BuildTool.Road;
    }

    private string GetBuildToolUnavailableStatus(bool ru)
    {
        return ru ? "\u0420\u0435\u043c\u043e\u043d\u0442" : "Rework";
    }

    private string GetBuildToolUnavailableDescription(BuildTool tool, bool ru)
    {
        return tool == BuildTool.Road
            ? ru
                ? "\u0412\u0440\u0435\u043c\u0435\u043d\u043d\u043e \u043d\u0430 \u0440\u0435\u043a\u043e\u043d\u0441\u0442\u0440\u0443\u043a\u0446\u0438\u0438. \u041f\u043e\u043a\u0430 \u0441\u0442\u0440\u043e\u0439 \u043e\u0431\u044b\u0447\u043d\u044b\u0435 \u0434\u043e\u0440\u043e\u0433\u0438 1 \u043a\u043b\u0435\u0442\u043a\u0443 \u0448\u0438\u0440\u0438\u043d\u043e\u0439."
                : "Temporarily under rework. Use regular 1-cell roads for now."
            : string.Empty;
    }

    private string GetBuildCatalogTitle(BuildTool tool, bool ru, string fallback)
    {
        return TryGetBuildCatalogItem(tool, out BuildCatalogItemData item)
            ? item.GetTitle(ru)
            : fallback;
    }

    private bool TryGetBuildCatalogDescription(BuildTool tool, bool isActive, out string description)
    {
        description = string.Empty;
        if (!TryGetBuildCatalogItem(tool, out BuildCatalogItemData item))
        {
            return false;
        }

        bool ru = IsRussianLanguage();
        string text = isActive
            ? item.GetActiveDescription(ru)
            : GetBuildToolAlreadyBuilt(tool)
                ? item.GetAlreadyBuiltDescription(ru)
                : item.GetDescription(ru);

        if (string.IsNullOrWhiteSpace(text) && !isActive && GetBuildToolAlreadyBuilt(tool))
        {
            text = ru ? "Уже построено на этой карте." : "Already built on this map.";
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        description = AppendBuildToolCostDescription(tool, text.Replace("{rot}", GetBuildRotationLabel()), ru);
        return true;
    }

    private BuildCategoryUi CreateBuildCategory(RectTransform categoryParent, RectTransform itemParent, Font font, string labelEn, string labelRu, bool expanded,
        params (BuildTool tool, string abbrev, string title, Color color)[] toolDefs)
    {
        int categoryIndex = categoryParent != null ? categoryParent.childCount : 0;
        BuildCategoryUi cat = new()
        {
            LabelEn = labelEn,
            LabelRu = labelRu,
            Index = categoryIndex,
            IsExpanded = expanded
        };

        RectTransform headerRoot = CreateUiObject("BuildCat_" + labelEn, categoryParent).GetComponent<RectTransform>();
        headerRoot.sizeDelta = new Vector2(134f, 74f);
        LayoutElement headerLayout = headerRoot.gameObject.AddComponent<LayoutElement>();
        headerLayout.preferredWidth = 134f;
        headerLayout.preferredHeight = 74f;
        Image headerBg = headerRoot.gameObject.AddComponent<Image>();
        headerBg.color = new Color(0.08f, 0.16f, 0.18f, 0.95f);
        Outline outline = headerRoot.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.34f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        cat.HeaderRoot = headerRoot;
        cat.HeaderBg = headerBg;

        RectTransform unlockGlowRect = CreateUiObject("UnlockGlow", headerRoot).GetComponent<RectTransform>();
        StretchRect(unlockGlowRect, 0f, 0f, 0f, 0f);
        Image unlockGlow = unlockGlowRect.gameObject.AddComponent<Image>();
        unlockGlow.color = Color.clear;
        unlockGlow.raycastTarget = false;
        cat.UnlockGlow = unlockGlow;

        RectTransform iconRoot = CreateUiObject("Icon", headerRoot).GetComponent<RectTransform>();
        iconRoot.anchorMin = new Vector2(0.5f, 1f);
        iconRoot.anchorMax = new Vector2(0.5f, 1f);
        iconRoot.pivot = new Vector2(0.5f, 1f);
        iconRoot.sizeDelta = new Vector2(76f, 40f);
        iconRoot.anchoredPosition = new Vector2(0f, -7f);
        cat.IconRoot = iconRoot;
        CreateBuildCategoryIconVisual(iconRoot, categoryIndex);

        Text headerText = CreateBodyText("CatLabel", headerRoot, font, labelEn, 11, TextAnchor.MiddleCenter, new Color(0.78f, 0.84f, 0.92f));
        headerText.fontStyle = FontStyle.Bold;
        RectTransform headerTextRect = headerText.GetComponent<RectTransform>();
        headerTextRect.anchorMin = new Vector2(0f, 0f);
        headerTextRect.anchorMax = new Vector2(1f, 0f);
        headerTextRect.pivot = new Vector2(0.5f, 0f);
        headerTextRect.offsetMin = new Vector2(7f, 7f);
        headerTextRect.offsetMax = new Vector2(-7f, 28f);
        headerText.horizontalOverflow = HorizontalWrapMode.Wrap;
        headerText.verticalOverflow = VerticalWrapMode.Truncate;
        cat.HeaderText = headerText;

        Button btn = headerRoot.gameObject.AddComponent<Button>();
        btn.targetGraphic = headerBg;
        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        cb.pressedColor     = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cb;
        BuildCategoryUi capturedCat = cat;
        btn.onClick.AddListener(() =>
        {
            SelectBuildCategoryFromMenu(capturedCat, true);
        });
        cat.HeaderButton = btn;
        AddBuildHoverHandlers(headerRoot.gameObject, hovered => capturedCat.IsHovered = hovered);

        cat.Items = new BuildItemUi[toolDefs.Length];
        for (int i = 0; i < toolDefs.Length; i++)
        {
            cat.Items[i] = CreateBuildItemCard(itemParent, font, toolDefs[i].tool, toolDefs[i].abbrev, toolDefs[i].title, toolDefs[i].color);
        }

        return cat;
    }

    private void UpdateBuildScreenUi()
    {
        if (buildScreenUi == null) return;

        bool shouldShow = isBuildPanelOpen;
        if (shouldShow && !buildScreenUi.CanvasRoot.activeSelf)
        {
            buildScreenUi.CanvasRoot.SetActive(true);
            isBuildScreenDirty = true;
        }

        if (!shouldShow && !buildScreenUi.CanvasRoot.activeSelf && buildScreenPanelAnimation <= 0f) return;
        if (!shouldShow && selectedBuildCategoryIndex >= 0)
        {
            selectedBuildCategoryIndex = -1;
            isBuildScreenDirty = true;
        }

        EnsureSelectedBuildCategory();
        UpdateBuildScreenDockAnimation();
        if (!shouldShow && buildScreenPanelAnimation <= 0f)
        {
            buildScreenUi.CanvasRoot.SetActive(false);
            return;
        }

        if (!isBuildScreenDirty) return;

        bool ru = IsRussianLanguage();
        int visibleCategoryNumber = 0;
        for (int categoryIndex = 0; categoryIndex < buildScreenUi.Categories.Length; categoryIndex++)
        {
            BuildCategoryUi cat = buildScreenUi.Categories[categoryIndex];
            bool anyUnlocked = false;
            foreach (BuildItemUi ci in cat.Items)
                if (IsBuildToolUnlocked(ci.Tool)) { anyUnlocked = true; break; }

            bool isSelectedCategory = categoryIndex == selectedBuildCategoryIndex;
            foreach (BuildItemUi item in cat.Items)
            {
                bool visible = anyUnlocked && isSelectedCategory && IsBuildToolUnlocked(item.Tool);
                item.Root.gameObject.SetActive(visible);
            }
            cat.HeaderRoot.gameObject.SetActive(anyUnlocked);
            if (!anyUnlocked) continue;

            visibleCategoryNumber++;
            cat.HeaderText.text = FormatBuildMenuHotkeyLabel(visibleCategoryNumber, ru ? cat.LabelRu : cat.LabelEn);
            cat.HeaderBg.color = isSelectedCategory
                ? new Color(0.12f, 0.31f, 0.36f, 0.98f)
                : new Color(0.08f, 0.16f, 0.18f, 0.95f);

            int visibleItemNumber = 0;
            foreach (BuildItemUi item in cat.Items)
            {
                bool unlocked = IsBuildToolUnlocked(item.Tool);
                bool visible = unlocked && isSelectedCategory;
                if (!visible) continue;

                bool isActive = activeBuildTool == item.Tool;
                bool isBuilt  = GetBuildToolAlreadyBuilt(item.Tool);
                bool isUnavailable = IsBuildToolTemporarilyUnavailable(item.Tool);
                bool isUnaffordable = !isUnavailable && !isBuilt && !CanAffordBuildTool(item.Tool);
                item.IsUnaffordable = isUnaffordable;
                if ((isUnavailable || isUnaffordable) && isActive)
                {
                    activeBuildTool = BuildTool.None;
                    isActive = false;
                }

                bool isBlocked = isUnavailable || isUnaffordable;
                item.Button.interactable = !isBlocked;
                item.CardBg.color = isBlocked
                    ? new Color(0.11f, 0.13f, 0.16f, 0.82f)
                    : isActive
                        ? new Color(0.20f, 0.27f, 0.37f, 1f)
                        : new Color(0.16f, 0.21f, 0.28f, 1f);
                item.AccentBg.color = isBlocked
                    ? new Color(0.20f, 0.22f, 0.25f, 0.88f)
                    : isActive
                        ? FleetAccentColor
                        : item.DefaultAccentColor;
                visibleItemNumber++;
                item.TitleText.color = isBlocked ? new Color(0.62f, 0.66f, 0.72f, 1f) : Color.white;
                item.TitleText.text = FormatBuildMenuHotkeyLabel(visibleItemNumber, GetBuildCatalogTitle(item.Tool, ru, item.TitleFallback));

                if (isUnavailable)
                {
                    item.StatusBg.gameObject.SetActive(true);
                    item.StatusBg.color  = new Color(0.16f, 0.16f, 0.18f, 0.96f);
                    item.StatusText.text = GetBuildToolUnavailableStatus(ru);
                }
                else if (isUnaffordable)
                {
                    item.StatusBg.gameObject.SetActive(true);
                    item.StatusBg.color  = new Color(0.24f, 0.06f, 0.05f, 0.98f);
                    item.StatusText.text = GetBuildToolCostLabel(item.Tool, true);
                }
                else if (isActive)
                {
                    item.StatusBg.gameObject.SetActive(true);
                    item.StatusBg.color  = new Color(0.60f, 0.36f, 0.10f, 0.96f);
                    item.StatusText.text = "Active";
                }
                else if (isBuilt)
                {
                    item.StatusBg.gameObject.SetActive(true);
                    item.StatusBg.color  = new Color(0.14f, 0.36f, 0.20f, 0.96f);
                    item.StatusText.text = "Built";
                }
                else if (!string.IsNullOrWhiteSpace(GetBuildToolCostLabel(item.Tool)))
                {
                    item.StatusBg.gameObject.SetActive(true);
                    item.StatusBg.color  = new Color(0.18f, 0.13f, 0.06f, 0.98f);
                    item.StatusText.text = GetBuildToolCostLabel(item.Tool);
                }
                else
                {
                    item.StatusBg.gameObject.SetActive(false);
                    item.StatusText.text = string.Empty;
                }
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(buildScreenUi.WindowRoot);
        LocalizeCanvas(buildScreenUi.CanvasRoot);
        isBuildScreenDirty = false;
    }

    private void EnsureSelectedBuildCategory()
    {
        if (buildScreenUi?.Categories == null || buildScreenUi.Categories.Length == 0)
        {
            selectedBuildCategoryIndex = -1;
            buildScreenTrayAnimation = 0f;
            return;
        }

        if (selectedBuildCategoryIndex < 0)
        {
            return;
        }

        if (selectedBuildCategoryIndex >= buildScreenUi.Categories.Length ||
            !HasUnlockedBuildItems(buildScreenUi.Categories[selectedBuildCategoryIndex]))
        {
            selectedBuildCategoryIndex = -1;
            isBuildScreenDirty = true;
        }
    }

    private bool HasUnlockedBuildItems(BuildCategoryUi category)
    {
        if (category?.Items == null)
        {
            return false;
        }

        for (int i = 0; i < category.Items.Length; i++)
        {
            if (IsBuildToolUnlocked(category.Items[i].Tool))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateBuildScreenDockAnimation()
    {
        TickBuildUnlockPulseTimers();

        buildScreenPanelAnimation = Mathf.MoveTowards(buildScreenPanelAnimation, isBuildPanelOpen ? 1f : 0f, Time.unscaledDeltaTime * 5.8f);
        float panelT = SmootherStep01(buildScreenPanelAnimation);
        if (buildScreenUi.PanelGroup != null)
        {
            buildScreenUi.PanelGroup.alpha = panelT;
            buildScreenUi.PanelGroup.blocksRaycasts = isBuildPanelOpen && panelT > 0.12f;
            buildScreenUi.PanelGroup.interactable = isBuildPanelOpen && panelT > 0.12f;
        }

        bool trayOpen = selectedBuildCategoryIndex >= 0 &&
                        buildScreenUi.Categories != null &&
                        selectedBuildCategoryIndex < buildScreenUi.Categories.Length &&
                        HasUnlockedBuildItems(buildScreenUi.Categories[selectedBuildCategoryIndex]);
        float trayTarget = trayOpen ? 1f : 0f;
        buildScreenTrayAnimation = Mathf.MoveTowards(buildScreenTrayAnimation, trayTarget, Time.unscaledDeltaTime * 5.2f);
        float trayT = SmootherStep01(buildScreenTrayAnimation);
        if (buildScreenUi.ItemTrayGroup != null)
        {
            buildScreenUi.ItemTrayGroup.alpha = trayT;
            buildScreenUi.ItemTrayGroup.blocksRaycasts = trayOpen && trayT > 0.55f;
            buildScreenUi.ItemTrayGroup.interactable = trayOpen && trayT > 0.55f;
        }

        if (buildScreenUi.ItemTrayRoot != null)
        {
            float trayY = Mathf.Lerp(-126f, Mathf.Lerp(104f, 128f, trayT), panelT);
            buildScreenUi.ItemTrayRoot.anchoredPosition = new Vector2(0f, trayY);
            buildScreenUi.ItemTrayRoot.localScale = Vector3.one * Mathf.Lerp(0.92f, Mathf.Lerp(0.94f, 1f, trayT), panelT);
        }

        if (buildScreenUi.DockRoot != null)
        {
            buildScreenUi.DockRoot.anchoredPosition = new Vector2(0f, Mathf.Lerp(-112f, 22f, panelT));
            buildScreenUi.DockRoot.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, panelT);
        }

        for (int i = 0; i < buildScreenUi.Categories.Length; i++)
        {
            BuildCategoryUi category = buildScreenUi.Categories[i];
            if (category?.HeaderRoot == null)
            {
                continue;
            }

            bool selected = i == selectedBuildCategoryIndex;
            float categoryTarget = category.IsHovered || selected ? 1f : 0f;
            category.HoverT = Mathf.MoveTowards(category.HoverT, categoryTarget, Time.unscaledDeltaTime * 7f);
            float categoryPulse = GetBuildCategoryUnlockPulse(category);
            float categoryPulseWave = categoryPulse > 0f ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9.5f) : 0f;
            category.HeaderRoot.localScale = Vector3.one * (Mathf.Lerp(1f, selected ? 1.09f : 1.05f, SmootherStep01(category.HoverT)) + categoryPulse * Mathf.Lerp(0.015f, 0.055f, categoryPulseWave));
            if (category.UnlockGlow != null)
            {
                category.UnlockGlow.color = new Color(1f, 0.76f, 0.24f, categoryPulse * Mathf.Lerp(0.04f, 0.20f, categoryPulseWave));
            }
            if (category.IconRoot != null)
            {
                category.IconRoot.anchoredPosition = new Vector2(0f, -7f + Mathf.Lerp(0f, 3f, SmootherStep01(category.HoverT)) + categoryPulse * Mathf.Lerp(0f, 2f, categoryPulseWave));
            }

            for (int j = 0; j < category.Items.Length; j++)
            {
                BuildItemUi item = category.Items[j];
                if (item?.Root == null || !item.Root.gameObject.activeSelf)
                {
                    continue;
                }

                bool active = activeBuildTool == item.Tool;
                float itemTarget = item.IsHovered || active ? 1f : 0f;
                item.HoverT = Mathf.MoveTowards(item.HoverT, itemTarget, Time.unscaledDeltaTime * 7f);
                float hoverT = SmootherStep01(item.HoverT);
                float itemPulse = GetBuildToolUnlockPulse(item.Tool);
                float itemPulseWave = itemPulse > 0f ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10.8f + j * 0.7f) : 0f;
                item.Root.localScale = Vector3.one * (Mathf.Lerp(1f, active ? 1.08f : 1.045f, hoverT) + itemPulse * Mathf.Lerp(0.02f, 0.07f, itemPulseWave));
                if (item.UnlockGlow != null)
                {
                    item.UnlockGlow.color = new Color(1f, 0.78f, 0.26f, itemPulse * Mathf.Lerp(0.04f, 0.24f, itemPulseWave));
                }
            }
        }
    }

    private void TickBuildUnlockPulseTimers()
    {
        if (buildToolUnlockPulseTimers.Count == 0 || (!isBuildPanelOpen && buildScreenPanelAnimation <= 0f))
        {
            return;
        }

        buildPulseScratch.Clear();
        foreach (KeyValuePair<BuildTool, float> kv in buildToolUnlockPulseTimers)
        {
            float remaining = kv.Value - Time.unscaledDeltaTime;
            if (remaining <= 0f)
            {
                buildPulseScratch.Add(kv.Key);
            }
            else
            {
                buildPulseScratchValues[kv.Key] = remaining;
            }
        }

        foreach (KeyValuePair<BuildTool, float> kv in buildPulseScratchValues)
        {
            buildToolUnlockPulseTimers[kv.Key] = kv.Value;
        }
        buildPulseScratchValues.Clear();

        for (int i = 0; i < buildPulseScratch.Count; i++)
        {
            buildToolUnlockPulseTimers.Remove(buildPulseScratch[i]);
        }
    }

    private float GetBuildToolUnlockPulse(BuildTool tool)
    {
        return buildToolUnlockPulseTimers.TryGetValue(tool, out float remaining)
            ? Mathf.Clamp01(remaining / BuildUnlockPulseDuration)
            : 0f;
    }

    private float GetBuildCategoryUnlockPulse(BuildCategoryUi category)
    {
        if (category?.Items == null)
        {
            return 0f;
        }

        float pulse = 0f;
        for (int i = 0; i < category.Items.Length; i++)
        {
            pulse = Mathf.Max(pulse, GetBuildToolUnlockPulse(category.Items[i].Tool));
        }

        return pulse;
    }

    private void AddBuildHoverHandlers(GameObject target, System.Action<bool> setHovered)
    {
        if (target == null || setHovered == null)
        {
            return;
        }

        EventTrigger trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            setHovered(true);
            isBuildScreenDirty = true;
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ =>
        {
            setHovered(false);
            isBuildScreenDirty = true;
        });
        trigger.triggers.Add(exit);
    }

    private void CreateBuildCategoryIconVisual(RectTransform iconRoot, int categoryIndex)
    {
        RectTransform P(float ax, float ay, float bx, float by, Color col)
        {
            GameObject g = new("CatIc");
            g.transform.SetParent(iconRoot, false);
            RectTransform rt = g.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(bx, by);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            g.AddComponent<Image>().color = col;
            return rt;
        }

        void R(float cx, float cy, float w, float h, Color col, float rot = 0f)
        {
            GameObject g = new("CatIcR");
            g.transform.SetParent(iconRoot, false);
            RectTransform rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(cx, cy);
            if (rot != 0f)
            {
                rt.localEulerAngles = new Vector3(0f, 0f, rot);
            }
            g.AddComponent<Image>().color = col;
        }

        switch (Mathf.Abs(categoryIndex) % 5)
        {
            case 0:
                P(0.08f, 0.20f, 0.92f, 0.78f, new Color(0.12f, 0.15f, 0.18f));
                P(0.45f, 0.20f, 0.55f, 0.78f, new Color(0.95f, 0.82f, 0.32f));
                P(0.08f, 0.12f, 0.28f, 0.25f, new Color(0.72f, 0.28f, 0.24f));
                P(0.14f, 0.25f, 0.22f, 0.44f, Color.white);
                break;
            case 1:
                P(0.08f, 0.14f, 0.58f, 0.58f, new Color(0.70f, 0.52f, 0.30f));
                P(0.06f, 0.56f, 0.62f, 0.72f, new Color(0.50f, 0.34f, 0.18f));
                P(0.64f, 0.14f, 0.92f, 0.38f, new Color(0.10f, 0.36f, 0.56f));
                P(0.70f, 0.36f, 0.88f, 0.48f, new Color(0.48f, 0.30f, 0.14f));
                break;
            case 2:
                P(0.12f, 0.16f, 0.58f, 0.54f, new Color(0.58f, 0.36f, 0.16f));
                P(0.08f, 0.52f, 0.62f, 0.66f, new Color(0.30f, 0.20f, 0.10f));
                P(0.70f, 0.10f, 0.80f, 0.44f, new Color(0.34f, 0.21f, 0.10f));
                P(0.60f, 0.42f, 0.90f, 0.64f, new Color(0.20f, 0.50f, 0.16f));
                P(0.64f, 0.62f, 0.86f, 0.78f, new Color(0.24f, 0.60f, 0.20f));
                break;
            case 3:
                P(0.10f, 0.14f, 0.90f, 0.54f, new Color(0.92f, 0.88f, 0.76f));
                R(-16f, 17f, 5f, 44f, new Color(0.46f, 0.22f, 0.14f), 32f);
                R(16f, 17f, 5f, 44f, new Color(0.46f, 0.22f, 0.14f), -32f);
                P(0.42f, 0.14f, 0.58f, 0.42f, new Color(0.20f, 0.14f, 0.10f));
                P(0.68f, 0.32f, 0.84f, 0.50f, new Color(0.60f, 0.80f, 0.90f));
                break;
            default:
                P(0.16f, 0.18f, 0.84f, 0.54f, new Color(0.20f, 0.42f, 0.50f));
                P(0.22f, 0.54f, 0.78f, 0.66f, new Color(0.96f, 0.92f, 0.84f));
                P(0.56f, 0.20f, 0.72f, 0.44f, new Color(0.90f, 0.70f, 0.22f));
                P(0.68f, 0.30f, 0.86f, 0.48f, new Color(0.90f, 0.70f, 0.22f));
                P(0.74f, 0.35f, 0.82f, 0.43f, new Color(0.08f, 0.10f, 0.12f));
                break;
        }
    }

    private bool GetBuildToolAlreadyBuilt(BuildTool tool)
    {
        return tool switch
        {
            BuildTool.Parking          => locations.ContainsKey(LocationType.Parking),
            BuildTool.Warehouse        => false,
            BuildTool.Docks            => false,
            BuildTool.SingleRoad       => false,
            BuildTool.Road             => false,
            BuildTool.Stop             => false,
            BuildTool.Forest           => false,
            BuildTool.Sawmill          => false,
            BuildTool.Motel            => locations.ContainsKey(LocationType.Motel),
            BuildTool.FurnitureFactory => false,
            BuildTool.Bar              => false,
            BuildTool.Canteen          => false,
            BuildTool.Kiosk            => false,
            BuildTool.GasStation       => false,
            BuildTool.GamblingHall     => false,
            BuildTool.CityPark         => false,
            BuildTool.PersonalHouse    => false,
            BuildTool.Kindergarten     => false,
            BuildTool.PrimarySchool    => false,
            BuildTool.SecondarySchool  => false,
            BuildTool.CarMarket        => locations.ContainsKey(LocationType.CarMarket),
            BuildTool.LaborExchange    => locations.ContainsKey(LocationType.LaborExchange),
            BuildTool.CleaningDepot    => locations.ContainsKey(LocationType.CleaningDepot),
            BuildTool.CityHall         => locations.ContainsKey(LocationType.CityHall),
            _                          => false
        };
    }

    private string GetBuildDescription(BuildTool tool, bool isActive)
    {
        if (TryGetBuildCatalogDescription(tool, isActive, out string catalogDescription))
        {
            return catalogDescription;
        }

        bool ru = IsRussianLanguage();
        string rot = GetBuildRotationLabel();

        if (isActive)
        {
            return tool switch
            {
                BuildTool.Parking          => ru ? "Парковка — основной хаб грузовиков. Поставь её вручную, когда будешь готов к автопарку." : "Mode active: place the truck yard manually when you are ready to run a fleet.",
                BuildTool.Warehouse        => ru ? $"Режим активен: поставь склад 2x2 с подъездом. R — поворот ({rot})." : $"Mode active: place a 2x2 warehouse from its driveway cell. R rotates ({rot}).",
                BuildTool.SingleRoad       => ru ? "\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u0435\u0440\u0432\u044b\u0439 \u041b\u041a\u041c \u0432\u044b\u0431\u0438\u0440\u0430\u0435\u0442 \u043d\u0430\u0447\u0430\u043b\u043e, \u0432\u0442\u043e\u0440\u043e\u0439 \u041b\u041a\u041c \u0441\u0442\u0440\u043e\u0438\u0442 \u0434\u043e\u0440\u043e\u0433\u0443." : "Mode active: first left click selects the start, second left click builds the road.",
                BuildTool.Road             => ru ? "\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u0435\u0440\u0432\u044b\u0439 \u041b\u041a\u041c \u0432\u044b\u0431\u0438\u0440\u0430\u0435\u0442 \u043d\u0430\u0447\u0430\u043b\u043e, \u0432\u0442\u043e\u0440\u043e\u0439 \u041b\u041a\u041c \u0441\u0442\u0440\u043e\u0438\u0442 \u0434\u0432\u0443\u0445\u043f\u043e\u043b\u043e\u0441\u043d\u0443\u044e \u0434\u043e\u0440\u043e\u0433\u0443." : "Mode active: first left click selects the start, second left click builds the two-way road.",
                BuildTool.Stop             => ru ? $"Режим активен: поставь автобусную остановку 2x1 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x1 bus stop from its driveway cell. R rotates ({rot}).",
                BuildTool.Forest           => ru ? $"Режим активен: поставь лагерь лесорубов 3x3 с подъездом. R — поворот ({rot})." : $"Mode active: place a 3x3 lumberjack camp from its driveway cell. R rotates ({rot}).",
                BuildTool.Sawmill          => ru ? $"Режим активен: поставь лесопилку 2x2 с подъездом. R — поворот ({rot})." : $"Mode active: place a 2x2 sawmill from its driveway cell. R rotates ({rot}).",
                BuildTool.Motel            => ru ? $"Режим активен: поставь мотель 2x2. R — поворот ({rot})." : $"Mode active: place one 2x2 motel. R rotates ({rot}).",
                BuildTool.FurnitureFactory => ru ? $"Режим активен: поставь фабрику 3x2 с подъездом. R — поворот ({rot})." : $"Mode active: place a 3x2 furniture factory from its driveway cell. R rotates ({rot}).",
                BuildTool.Bar              => ru ? $"Режим активен: поставь бар. Можно строить несколько. R — поворот ({rot})." : $"Mode active: place a bar. Multiple bars are allowed. R rotates ({rot}).",
                BuildTool.Canteen          => ru ? $"Режим активен: поставь столовую 3x2. Можно строить несколько. R — поворот ({rot})." : $"Mode active: place a 3x2 canteen. Multiple canteens are allowed. R rotates ({rot}).",
                BuildTool.Kiosk            => ru ? $"Режим активен: поставь киоск 2x1 без подъезда к дороге. Snack и Coffee покупаются здесь за $4. R — поворот ({rot})." : $"Mode active: place a 2x1 walk-up kiosk. Snacks and Coffee cost $4 here. No road driveway required. R rotates ({rot}).",
                BuildTool.GamblingHall     => ru ? $"Режим активен: поставь игровой зал 3x3. Можно строить несколько. R — поворот ({rot})." : $"Mode active: place a 3x3 gambling hall. Multiple halls are allowed. R rotates ({rot}).",
                BuildTool.CityPark         => ru ? $"Режим активен: поставь городской парк 8x8 без подъезда к дороге. R — поворот ({rot})." : $"Mode active: place an 8x8 city park with no road driveway. R rotates ({rot}).",
                BuildTool.PersonalHouse    => ru ? $"Режим активен: поставь жилой дом 5x6. R — поворот ({rot})." : $"Mode active: place one 5x6 personal house. R rotates ({rot}).",
                BuildTool.Kindergarten     => ru ? $"\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0434\u0435\u0442\u0441\u043a\u0438\u0439 \u0441\u0430\u0434 4x3. \u041c\u043e\u0436\u043d\u043e \u0441\u0442\u0440\u043e\u0438\u0442\u044c \u043d\u0435\u0441\u043a\u043e\u043b\u044c\u043a\u043e. R - \u043f\u043e\u0432\u043e\u0440\u043e\u0442 ({rot})." : $"Mode active: place a 4x3 kindergarten. Multiple kindergartens are allowed. R rotates ({rot}).",
                BuildTool.PrimarySchool    => ru ? $"\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u043d\u0430\u0447\u0430\u043b\u044c\u043d\u0443\u044e \u0448\u043a\u043e\u043b\u0443 5x3. R - \u043f\u043e\u0432\u043e\u0440\u043e\u0442 ({rot})." : $"Mode active: place a 5x3 primary school. R rotates ({rot}).",
                BuildTool.SecondarySchool  => ru ? $"\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0441\u0440\u0435\u0434\u043d\u044e\u044e \u0448\u043a\u043e\u043b\u0443 6x3. R - \u043f\u043e\u0432\u043e\u0440\u043e\u0442 ({rot})." : $"Mode active: place a 6x3 secondary school. R rotates ({rot}).",
                BuildTool.CarMarket        => ru ? $"Режим активен: поставь авторынок 5x5. R — поворот ({rot})." : $"Mode active: place one 5x5 car market. R rotates ({rot}).",
                BuildTool.LaborExchange    => ru ? $"\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0411\u0438\u0440\u0436\u0443 \u0442\u0440\u0443\u0434\u0430 3x2. R - \u043f\u043e\u0432\u043e\u0440\u043e\u0442 ({rot})." : $"Mode active: place one 3x2 labor exchange. R rotates ({rot}).",
                BuildTool.CleaningDepot    => ru ? $"\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0441\u043b\u0443\u0436\u0431\u0443 \u0443\u0431\u043e\u0440\u043a\u0438 2x2. R - \u043f\u043e\u0432\u043e\u0440\u043e\u0442 ({rot})." : $"Mode active: place one 2x2 cleaning depot. R rotates ({rot}).",
                BuildTool.CityHall         => ru ? $"\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0440\u0430\u0442\u0443\u0448\u0443 4x3. R - \u043f\u043e\u0432\u043e\u0440\u043e\u0442 ({rot})." : $"Mode active: place one 4x3 city hall. R rotates ({rot}).",
                BuildTool.GasStation       => ru ? $"\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0437\u0430\u043f\u0440\u0430\u0432\u043a\u0443 2x2 \u0441 \u043f\u043e\u0434\u044a\u0435\u0437\u0434\u043e\u043c. R - \u043f\u043e\u0432\u043e\u0440\u043e\u0442 ({rot})." : $"Mode active: place one 2x2 gas station from its driveway cell. R rotates ({rot}).",
                _                          => string.Empty
            };
        }

        string alreadyBuilt = ru ? "Уже построено на этой карте." : "Already built on this map.";
        return tool switch
        {
            BuildTool.Parking          => locations.ContainsKey(LocationType.Parking) ? alreadyBuilt : (ru ? "Парковка: база для грузовиков. В новой игре стартовых грузовиков нет, пока парковка не построена." : "Truck yard: the fleet base. New games start with no trucks until this exists."),
            BuildTool.Warehouse        => ru ? "Склад 2x2: центральное хранение ресурсов и точка для производственных цепочек." : "2x2 warehouse: central resource storage for production chains.",
            BuildTool.SingleRoad       => ru ? "Обычная дорога занимает 1 клетку. Удобна для подъездов, узких участков и ручной достройки." : "Build a regular 1-cell road for driveways, narrow links, and manual fixes.",
            BuildTool.Road             => ru ? "Двухполосная дорога строится участками: выбери начало, затем конец. Она занимает 2 клетки шириной, с центральной разметкой и движением по полосам." : "Build two-way road segments: choose a start, then an end. It occupies 2 cells of width with center markings and lane movement.",
            BuildTool.Stop             => ru ? "Автобусная остановка 2x1: локальная городская остановка для будущего транспорта рабочих." : "Place a 2x1 local bus stop for future worker public transport routes.",
            BuildTool.Forest           => ru ? "Лагерь лесорубов 3x3: рабочие выходят рубить деревья, таскают брёвна и высаживают саженцы." : "3x3 lumberjack camp: workers chop trees, carry logs, and replant saplings.",
            BuildTool.Sawmill          => ru ? "Здание 2x2: превращает брёвна в доски." : "Place a 2x2 production building that turns logs into boards.",
            BuildTool.Motel            => locations.ContainsKey(LocationType.Motel)            ? alreadyBuilt : (ru ? "Мотель 2x2: рабочие заселяются и ждут здесь." : "Place a 2x2 worker hub. New arrivals check in and idle here."),
            BuildTool.FurnitureFactory => ru ? "Фабрика 3x2: 1 Доска + 1 Ткань = 1 Мебель." : "Place a 3x2 factory that turns 1 Board + 1 Textile into 1 Furniture.",
            BuildTool.Bar              => ru ? "Соцточка — водители собираются здесь отдыхать. Можно строить несколько." : "Social hub — idle drivers gather here to rest. Multiple bars are allowed.",
            BuildTool.Canteen          => ru ? "Столовая: водители и рабочие платят $8 за обед. Можно строить несколько." : "Service building: visiting drivers/workers pay $8 for a quick meal. Multiple canteens are allowed.",
            BuildTool.Kiosk            => ru ? "Киоск: рабочие подходят к стойке и покупают Snack или Coffee за $4 в инвентарь. Подъезд к дороге не нужен." : "Walk-up kiosk: workers buy a $4 Snack or Coffee for their inventory. No road driveway required.",
            BuildTool.GamblingHall     => ru ? "Досуг: бесплатный вход — рабочие расслабляются здесь. Можно строить несколько." : "Leisure: free entry — workers unwind here. Multiple halls are allowed.",
            BuildTool.CityPark         => ru ? "Парк 8x8: рабочие гуляют и сидят на лавочках. Подъезд к дороге не нужен." : "8x8 park: workers stroll and sit on benches. No road driveway required.",
            BuildTool.PersonalHouse    => ru ? "Жилой дом 5x6 — американский пригородный дом в одной из 5 случайных вариаций." : "5x6 suburban house — one of 5 random American home styles. Decorative for now.",
            BuildTool.Kindergarten     => ru ? "\u0414\u0435\u0442\u0441\u043a\u0438\u0439 \u0441\u0430\u0434 4x3: \u0441\u0435\u0440\u0432\u0438\u0441\u043d\u044b\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u0441\u043e\u0437\u0434\u0430\u044e\u0442 \u043c\u0435\u0441\u0442\u0430 \u0434\u043b\u044f \u0434\u0435\u0442\u0435\u0439 \u0438 \u0441\u043d\u0438\u0436\u0430\u044e\u0442 \u0441\u0442\u0440\u0435\u0441\u0441 \u0441\u0435\u043c\u0435\u0439." : "4x3 kindergarten: service workers create child-care capacity and lower family stress.",
            BuildTool.PrimarySchool    => ru ? "\u041d\u0430\u0447\u0430\u043b\u044c\u043d\u0430\u044f \u0448\u043a\u043e\u043b\u0430 5x3: \u0431\u0430\u0437\u0430 4 \u043c\u0435\u0441\u0442\u0430, +8 \u0437\u0430 \u0443\u0447\u0438\u0442\u0435\u043b\u044f. \u041d\u0443\u0436\u043d\u0430 \u0434\u0435\u0442\u044f\u043c." : "5x3 primary school: base 4 seats, +8 per teacher. Needed by children.",
            BuildTool.SecondarySchool  => ru ? "\u0421\u0440\u0435\u0434\u043d\u044f\u044f \u0448\u043a\u043e\u043b\u0430 6x3: \u0431\u0430\u0437\u0430 3 \u043c\u0435\u0441\u0442\u0430, +6 \u0437\u0430 \u0443\u0447\u0438\u0442\u0435\u043b\u044f. \u041d\u0443\u0436\u043d\u0430 \u043f\u043e\u0434\u0440\u043e\u0441\u0442\u043a\u0430\u043c." : "6x3 secondary school: base 3 seats, +6 per teacher. Needed by teens.",
            BuildTool.CarMarket        => locations.ContainsKey(LocationType.CarMarket) ? alreadyBuilt : "5x5 car market: workers with $100 can buy personal cars here.",
            BuildTool.LaborExchange    => locations.ContainsKey(LocationType.LaborExchange) ? alreadyBuilt : (ru ? "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430 3x2: \u043a\u043b\u0435\u0440\u043a \u0441 \u0432\u044b\u0441\u0448\u0438\u043c \u043e\u0431\u0440\u0430\u0437\u043e\u0432\u0430\u043d\u0438\u0435\u043c \u043f\u0443\u0431\u043b\u0438\u043a\u0443\u0435\u0442 \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u0438." : "3x2 labor exchange: a higher-educated clerk publishes vacancies for workers."),
            BuildTool.CleaningDepot    => locations.ContainsKey(LocationType.CleaningDepot) ? alreadyBuilt : (ru ? "\u0421\u043b\u0443\u0436\u0431\u0430 \u0443\u0431\u043e\u0440\u043a\u0438 2x2: \u0443\u0431\u043e\u0440\u0449\u0438\u043a\u0438 \u043f\u0430\u0442\u0440\u0443\u043b\u0438\u0440\u0443\u044e\u0442 \u0443\u043b\u0438\u0446\u044b \u0438 \u0443\u0431\u0438\u0440\u0430\u044e\u0442 \u043c\u0443\u0441\u043e\u0440." : "2x2 cleaning depot: cleaners patrol streets and remove visible litter."),
            BuildTool.CityHall         => locations.ContainsKey(LocationType.CityHall) ? alreadyBuilt : (ru ? "Ратуша 4x3: горожане подают обращения, а принятые обращения становятся городскими целями на 24 часа." : "4x3 city hall: citizens file requests that can become 24h city goals."),
            BuildTool.GasStation       => ru ? "\u0417\u0430\u043f\u0440\u0430\u0432\u043a\u0430 2x2: \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438 \u0435\u0434\u0443\u0442 \u0441\u044e\u0434\u0430 \u0437\u0430 \u0442\u043e\u043f\u043b\u0438\u0432\u043e\u043c. \u041c\u043e\u0436\u043d\u043e \u0441\u0442\u0440\u043e\u0438\u0442\u044c \u043d\u0435\u0441\u043a\u043e\u043b\u044c\u043a\u043e." : "2x2 fuel service: trucks refuel here when routes get too long. Multiple stations are allowed.",
            _                          => string.Empty
        };
    }

}
