using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class BuildingHudWorkerEntry
    {
        public DriverAgent Driver;
        public string ActivityText;
        public float RemainingSeconds;
        public float DurationSeconds = 1f;
        public bool ShowProgress;
        public bool IsGamblingVisitor;
    }

    private void UpdateBuildingServiceWorkerSlots(LocationType locationType, bool ru)
    {
        if (buildingQuickHud?.WorkerSlots == null)
        {
            return;
        }

        bool relevant = IsBuildingQuickHudWorkerListRelevant(locationType);
        buildingQuickHud.WorkerSlotsSection.gameObject.SetActive(relevant);
        if (!relevant)
        {
            return;
        }

        List<BuildingHudWorkerEntry> entries = CollectBuildingQuickHudWorkerEntries(locationType, ru);
        EnsureBuildingWorkerSlotCount(entries.Count);

        buildingQuickHud.WorkerSlotsSectionHeader.text = ru ? "\u041b\u044e\u0434\u0438 \u0432 \u0437\u0434\u0430\u043d\u0438\u0438" : "People in building";
        if (buildingQuickHud.WorkerSlotsScroll != null)
        {
            buildingQuickHud.WorkerSlotsScroll.vertical = entries.Count > 3;
            buildingQuickHud.WorkerSlotsScroll.gameObject.SetActive(entries.Count > 0);
            LayoutElement scrollLayout = buildingQuickHud.WorkerSlotsScroll.GetComponent<LayoutElement>();
            if (scrollLayout != null)
            {
                scrollLayout.preferredHeight = entries.Count <= 0
                    ? 0f
                    : Mathf.Clamp(entries.Count * 60f + 4f, 64f, 190f);
            }
        }

        for (int i = 0; i < buildingQuickHud.WorkerSlots.Length; i++)
        {
            ServiceWorkerSlotUi slot = buildingQuickHud.WorkerSlots[i];
            if (i >= entries.Count)
            {
                ClearBuildingWorkerSlot(slot);
                continue;
            }

            UpdateBuildingWorkerSlot(slot, entries[i], ru);
        }
    }

    private bool IsBuildingQuickHudWorkerListRelevant(LocationType locationType)
    {
        return IsBuildingServiceVisitorLocation(locationType) ||
               GetMaxBuildingWorkerSlots(locationType) > 0;
    }

    private static bool IsBuildingServiceVisitorLocation(LocationType locationType)
    {
        return locationType == LocationType.Bar ||
               locationType == LocationType.Canteen ||
               locationType == LocationType.GamblingHall ||
               locationType == LocationType.CityPark ||
               locationType == LocationType.Motel ||
               locationType == LocationType.LaborExchange;
    }

    private List<BuildingHudWorkerEntry> CollectBuildingQuickHudWorkerEntries(LocationType locationType, bool ru)
    {
        List<BuildingHudWorkerEntry> entries = new();
        HashSet<int> added = new();

        foreach (DriverAgent d in driverAgents)
        {
            if (d == null)
            {
                continue;
            }

            if (IsWorkerAssignedAroundBuilding(d, locationType))
            {
                entries.Add(new BuildingHudWorkerEntry
                {
                    Driver = d,
                    ActivityText = GetAssignedBuildingWorkerActivityLabel(d, locationType, ru)
                });
                added.Add(d.DriverId);
            }
        }

        foreach (DriverAgent d in driverAgents)
        {
            if (d == null || added.Contains(d.DriverId))
            {
                continue;
            }

            if (!TryGetBuildingVisitorEntry(d, locationType, ru, out BuildingHudWorkerEntry entry))
            {
                continue;
            }

            entries.Add(entry);
            added.Add(d.DriverId);
        }

        return entries;
    }

    private bool IsWorkerAssignedAroundBuilding(DriverAgent driver, LocationType locationType)
    {
        if (driver == null || driver.AssignedBuildingType != locationType)
        {
            return false;
        }

        return driver.IsInsideBuilding ||
               driver.IsOnActiveShift ||
               driver.WalkPhase == DriverRescuePhase.ToBuildingForShift ||
               driver.WalkPhase == DriverRescuePhase.ToMotelFromBuilding ||
               IsWorkerProductionFieldPhase(driver.WalkPhase);
    }

    private static bool IsWorkerProductionFieldPhase(DriverRescuePhase phase)
    {
        return phase == DriverRescuePhase.LumberToTree ||
               phase == DriverRescuePhase.LumberChopping ||
               phase == DriverRescuePhase.LumberCarryLogToBuilding ||
               phase == DriverRescuePhase.LumberReturnToTreeForPlanting ||
               phase == DriverRescuePhase.LumberPlanting ||
               phase == DriverRescuePhase.LumberReturnToBuilding;
    }

    private bool TryGetBuildingVisitorEntry(DriverAgent driver, LocationType locationType, bool ru, out BuildingHudWorkerEntry entry)
    {
        entry = null;

        if (locationType == LocationType.Motel && driver.RestPhase == DriverRestPhase.Sleeping)
        {
            entry = new BuildingHudWorkerEntry
            {
                Driver = driver,
                ActivityText = ru ? "\u0421\u043f\u0438\u0442" : "Sleeping",
                RemainingSeconds = driver.SleepTimer,
                DurationSeconds = DriverSleepDuration,
                ShowProgress = true
            };
            return true;
        }

        if (GetDriverServiceLocation(driver.WalkPhase) != locationType)
        {
            return false;
        }

        entry = new BuildingHudWorkerEntry
        {
            Driver = driver,
            ActivityText = GetServiceBuildingActivityLabel(locationType, ru),
            RemainingSeconds = driver.IdleActivityTimer,
            DurationSeconds = GetServiceBuildingVisitDuration(locationType),
            ShowProgress = true,
            IsGamblingVisitor = locationType == LocationType.GamblingHall
        };
        return true;
    }

    private string GetAssignedBuildingWorkerActivityLabel(DriverAgent driver, LocationType locationType, bool ru)
    {
        if (driver.WalkPhase == DriverRescuePhase.ToBuildingForShift)
        {
            return ru ? "\u0418\u0434\u0435\u0442 \u043d\u0430 \u0441\u043c\u0435\u043d\u0443" : "Going to shift";
        }

        if (driver.WalkPhase == DriverRescuePhase.ToMotelFromBuilding)
        {
            return ru ? "\u0417\u0430\u043a\u043e\u043d\u0447\u0438\u043b \u0441\u043c\u0435\u043d\u0443" : "Leaving shift";
        }

        if (locationType == LocationType.Forest && IsWorkerProductionFieldPhase(driver.WalkPhase))
        {
            return driver.WalkPhase switch
            {
                DriverRescuePhase.LumberChopping => ru ? "\u0420\u0443\u0431\u0438\u0442 \u0434\u0435\u0440\u0435\u0432\u043e" : "Chopping tree",
                DriverRescuePhase.LumberCarryLogToBuilding => ru ? "\u041d\u0435\u0441\u0435\u0442 \u0431\u0440\u0435\u0432\u043d\u043e" : "Carrying log",
                DriverRescuePhase.LumberPlanting => ru ? "\u0421\u0430\u0436\u0430\u0435\u0442 \u0434\u0435\u0440\u0435\u0432\u043e" : "Planting tree",
                _ => ru ? "\u0420\u0430\u0431\u043e\u0442\u0430\u0435\u0442 \u0441\u043d\u0430\u0440\u0443\u0436\u0438" : "Working outside"
            };
        }

        if (driver.IsInsideBuilding)
        {
            return ru ? "\u041d\u0430 \u0441\u043c\u0435\u043d\u0435 \u0432\u043d\u0443\u0442\u0440\u0438" : "On shift inside";
        }

        return ru ? "\u041d\u0430 \u0441\u043c\u0435\u043d\u0435" : "On shift";
    }

    private void EnsureBuildingWorkerSlotCount(int requiredCount)
    {
        if (requiredCount <= buildingQuickHud.WorkerSlots.Length)
        {
            return;
        }

        int oldCount = buildingQuickHud.WorkerSlots.Length;
        Array.Resize(ref buildingQuickHud.WorkerSlots, requiredCount);
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Transform parent = buildingQuickHud.WorkerSlotsContent != null
            ? buildingQuickHud.WorkerSlotsContent
            : buildingQuickHud.WorkerSlotsSection;

        for (int i = oldCount; i < requiredCount; i++)
        {
            buildingQuickHud.WorkerSlots[i] = CreateBuildingWorkerSlot(i, parent, uiFont);
        }
    }

    private ServiceWorkerSlotUi CreateBuildingWorkerSlot(int index, Transform parent, Font uiFont)
    {
        ServiceWorkerSlotUi slot = new();
        RectTransform slotRoot = CreateUiObject($"WorkerSlot{index}", parent).GetComponent<RectTransform>();
        Image slotBg = slotRoot.gameObject.AddComponent<Image>();
        slotBg.color = new Color(0.14f, 0.18f, 0.25f, 1f);
        slot.FocusButton = slotRoot.gameObject.AddComponent<Button>();
        slot.FocusButton.targetGraphic = slotBg;
        int capturedIndex = index;
        slot.FocusButton.onClick.AddListener(() => OnBuildingWorkerSlotClick(capturedIndex));

        slot.SlotLayout = slotRoot.gameObject.AddComponent<LayoutElement>();
        slot.SlotLayout.preferredHeight = 54f;
        HorizontalLayoutGroup slotLayout = slotRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        slotLayout.padding = new RectOffset(6, 8, 4, 4);
        slotLayout.spacing = 8f;
        slotLayout.childControlWidth = true;
        slotLayout.childControlHeight = true;
        slotLayout.childForceExpandWidth = false;
        slotLayout.childForceExpandHeight = true;
        slot.Root = slotRoot;

        RectTransform portraitGo = CreateUiObject($"Portrait{index}", slotRoot).GetComponent<RectTransform>();
        portraitGo.gameObject.AddComponent<RectMask2D>();
        portraitGo.gameObject.AddComponent<LayoutElement>().preferredWidth = 40f;
        slot.PortraitRoot = portraitGo;

        RectTransform rightSide = CreateUiObject($"SlotRight{index}", slotRoot).GetComponent<RectTransform>();
        rightSide.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup rightLayout = rightSide.gameObject.AddComponent<VerticalLayoutGroup>();
        rightLayout.spacing = 2f;
        rightLayout.padding = new RectOffset(0, 0, 4, 4);
        rightLayout.childControlWidth = true;
        rightLayout.childControlHeight = true;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;

        RectTransform nameRow = CreateLayoutRow($"SlotNameRow{index}", rightSide, 17f, 0f);
        slot.NameText = CreateBodyText($"SlotName{index}", nameRow, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        slot.NameText.fontStyle = FontStyle.Bold;
        slot.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        slot.TimeText = CreateBodyText($"SlotTime{index}", nameRow, uiFont, string.Empty, 11, TextAnchor.MiddleRight, FleetSecondaryTextColor);
        slot.TimeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 50f;

        RectTransform reelRow = CreateUiObject($"ReelRow{index}", rightSide).GetComponent<RectTransform>();
        HorizontalLayoutGroup reelHlg = reelRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        reelHlg.spacing = 5f;
        reelHlg.childControlWidth = true;
        reelHlg.childControlHeight = true;
        reelHlg.childForceExpandWidth = false;
        reelHlg.childForceExpandHeight = true;
        reelRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        slot.ReelRow = reelRow;
        reelRow.gameObject.SetActive(false);

        for (int r = 0; r < 3; r++)
        {
            RectTransform reelBox = CreateUiObject($"Reel{index}_{r}", reelRow).GetComponent<RectTransform>();
            reelBox.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.16f, 1f);
            reelBox.gameObject.AddComponent<LayoutElement>().preferredWidth = 30f;
            Text reelTxt = CreateBodyText($"ReelChar{index}_{r}", reelBox, uiFont, "?", 18, TextAnchor.MiddleCenter, Color.gray);
            reelTxt.fontStyle = FontStyle.Bold;
            slot.ReelTexts[r] = reelTxt;
        }

        slot.ActivityText = CreateBodyText($"SlotActivity{index}", rightSide, uiFont, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        slot.ActivityTextLayout = slot.ActivityText.gameObject.AddComponent<LayoutElement>();
        slot.ActivityTextLayout.preferredHeight = 14f;

        RectTransform barBg = CreateUiObject($"SlotBarBg{index}", rightSide).GetComponent<RectTransform>();
        barBg.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.14f, 0.20f, 1f);
        barBg.gameObject.AddComponent<LayoutElement>().preferredHeight = 4f;

        RectTransform barFill = CreateUiObject($"SlotBarFill{index}", barBg).GetComponent<RectTransform>();
        barFill.anchorMin = new Vector2(0f, 0f);
        barFill.anchorMax = new Vector2(1f, 1f);
        barFill.sizeDelta = Vector2.zero;
        barFill.anchoredPosition = Vector2.zero;
        Image fillImage = barFill.gameObject.AddComponent<Image>();
        fillImage.color = new Color(0.3f, 0.8f, 0.3f);
        slot.ProgressBarFill = barFill;
        slot.ProgressBarFillImage = fillImage;
        return slot;
    }

    private void UpdateBuildingWorkerSlot(ServiceWorkerSlotUi slot, BuildingHudWorkerEntry entry, bool ru)
    {
        DriverAgent driver = entry.Driver;
        slot.Root.gameObject.SetActive(true);

        if (slot.CurrentDriverId != driver.DriverId)
        {
            slot.CurrentDriverId = driver.DriverId;
            DrawWorkerPortraitScaled(driver, slot.PortraitRoot, 0.37f);
        }

        slot.NameText.text = driver.DriverName;
        if (entry.IsGamblingVisitor)
        {
            UpdateGamblingReels(slot, driver);
            UpdateGamblingSlotActivity(slot, driver, entry.ActivityText, ru);
        }
        else
        {
            slot.SlotLayout.preferredHeight = 54f;
            slot.ReelRow?.gameObject.SetActive(false);
            slot.ActivityText.text = entry.ActivityText;
            slot.ActivityText.color = Color.white;
            slot.ActivityTextLayout.preferredHeight = 14f;
        }

        if (entry.ShowProgress)
        {
            float progress = Mathf.Clamp01(entry.RemainingSeconds / Mathf.Max(entry.DurationSeconds, 0.01f));
            slot.TimeText.text = FormatGameTimeRemaining(entry.RemainingSeconds);
            slot.ProgressBarFill.anchorMax = new Vector2(progress, 1f);
            slot.ProgressBarFillImage.color = GetQuickHudProgressColor(progress);
        }
        else
        {
            slot.TimeText.text = ru ? "\u0441\u043c\u0435\u043d\u0430" : "shift";
            slot.ProgressBarFill.anchorMax = Vector2.one;
            slot.ProgressBarFillImage.color = new Color(0.35f, 0.58f, 0.95f);
        }
    }

    private void UpdateGamblingSlotActivity(ServiceWorkerSlotUi slot, DriverAgent d, string activityLabel, bool ru)
    {
        bool spinDone = slot.SlotPhase == GamblingSlotPhase.Done || slot.SlotPhase == GamblingSlotPhase.ResultPause;
        if (spinDone)
        {
            string outcomeLabel = d.GamblingMultiplier == 0 ? (ru ? "\u041f\u0440\u043e\u0438\u0433\u0440\u044b\u0448" : "Loss")
                                : d.GamblingMultiplier == 1 ? (ru ? "\u0421\u0442\u0430\u0432\u043a\u0430 x1" : "Break even")
                                : d.GamblingMultiplier == 5 ? (ru ? "\u0412\u044b\u0438\u0433\u0440\u044b\u0448 x5" : "Win x5")
                                :                             (ru ? "\u0414\u0436\u0435\u043a\u043f\u043e\u0442 x10" : "Jackpot x10");
            int net = d.GamblingPayout - d.GamblingBet;
            string netStr = net >= 0 ? $"+${net}" : $"-${-net}";
            slot.ActivityText.text = $"{(ru ? "\u0421\u0442\u0430\u0432\u043a\u0430" : "Bet")}: ${d.GamblingBet}  {outcomeLabel}\n{(ru ? "\u0418\u0442\u043e\u0433" : "Net")}: {netStr}  ->  ${d.GamblingPayout}";
            slot.ActivityText.color = GetReelResultColor(d.GamblingMultiplier);
            slot.ActivityTextLayout.preferredHeight = 28f;
        }
        else if (slot.SlotPhase == GamblingSlotPhase.ShowBet && d.GamblingBet > 0)
        {
            slot.ActivityText.text = $"{(ru ? "\u0421\u0442\u0430\u0432\u043a\u0430" : "Bet")}: ${d.GamblingBet}";
            slot.ActivityText.color = new Color(1f, 0.85f, 0.3f);
            slot.ActivityTextLayout.preferredHeight = 14f;
        }
        else if (d.GamblingBet > 0)
        {
            slot.ActivityText.text = ru ? "\u0412\u0440\u0430\u0449\u0435\u043d\u0438\u0435..." : "Spinning...";
            slot.ActivityText.color = Color.gray;
            slot.ActivityTextLayout.preferredHeight = 14f;
        }
        else if (d.GamblerBroke)
        {
            slot.ActivityText.text = ru ? "\u041d\u0430 \u043c\u0435\u043b\u0438" : "Broke";
            slot.ActivityText.color = new Color(0.6f, 0.4f, 0.4f);
            slot.ActivityTextLayout.preferredHeight = 14f;
        }
        else
        {
            slot.ActivityText.text = activityLabel;
            slot.ActivityText.color = Color.white;
            slot.ActivityTextLayout.preferredHeight = 14f;
        }
    }

    private static Color GetQuickHudProgressColor(float progress)
    {
        return progress > 0.5f
            ? new Color(0.30f, 0.80f, 0.32f)
            : progress > 0.2f
                ? new Color(0.90f, 0.70f, 0.20f)
                : new Color(0.85f, 0.30f, 0.22f);
    }

    private void ClearBuildingWorkerSlot(ServiceWorkerSlotUi slot)
    {
        slot.Root.gameObject.SetActive(false);
        if (slot.CurrentDriverId == -1)
        {
            return;
        }

        slot.CurrentDriverId = -1;
        slot.ReelRow?.gameObject.SetActive(false);
        for (int c = slot.PortraitRoot.childCount - 1; c >= 0; c--)
        {
            Destroy(slot.PortraitRoot.GetChild(c).gameObject);
        }
    }

    private void OnBuildingWorkerSlotClick(int rowIndex)
    {
        if (buildingQuickHud?.WorkerSlots == null ||
            rowIndex < 0 ||
            rowIndex >= buildingQuickHud.WorkerSlots.Length)
        {
            return;
        }

        int driverId = buildingQuickHud.WorkerSlots[rowIndex].CurrentDriverId;
        FocusWorkerFromQuickHud(driverId, "building HUD");
    }

    private void UpdateGamblingReels(ServiceWorkerSlotUi slot, DriverAgent d)
    {
        bool hasBet = d.GamblingBet > 0;
        float uiDelta = Time.unscaledDeltaTime * Mathf.Max(0f, gameSpeedMultiplier);
        slot.SlotLayout.preferredHeight = hasBet ? 90f : 54f;

        if (!hasBet)
        {
            slot.ReelRow.gameObject.SetActive(false);
            slot.SlotPhase = GamblingSlotPhase.Idle;
            slot.LastSpinBet = 0;
            return;
        }

        if (d.GamblingBet != slot.LastSpinBet)
        {
            slot.LastSpinBet = d.GamblingBet;
            slot.SlotPhase = GamblingSlotPhase.ShowBet;
            slot.SpinTimer = 0f;
            slot.SpinCycleTimer = 0f;
            slot.ResultDisplayTimer = 0f;
            slot.ReelStopped = new bool[3];
            slot.FinalReelChars = GetReelFinalChars(d.GamblingMultiplier);
            foreach (Text t in slot.ReelTexts)
            {
                t.text = "?";
                t.color = Color.gray;
            }
            slot.ReelRow.gameObject.SetActive(false);
        }

        slot.SpinTimer += uiDelta;

        switch (slot.SlotPhase)
        {
            case GamblingSlotPhase.ShowBet:
                if (slot.SpinTimer >= 2.5f)
                {
                    slot.SlotPhase = GamblingSlotPhase.Spinning;
                    slot.SpinTimer = 0f;
                    slot.ReelRow.gameObject.SetActive(true);
                }
                break;

            case GamblingSlotPhase.Spinning:
                slot.SpinCycleTimer -= uiDelta;
                if (slot.SpinCycleTimer <= 0f)
                {
                    slot.SpinCycleTimer = 0.26f;
                    bool anyStillSpinning = false;
                    for (int r = 0; r < 3; r++)
                    {
                        if (!slot.ReelStopped[r])
                        {
                            slot.ReelTexts[r].text = SlotSymbols[UnityEngine.Random.Range(0, SlotSymbols.Length)];
                            anyStillSpinning = true;
                        }
                    }
                    if (anyStillSpinning) PlayUiSound(slotReelTickClip, 0.45f);
                }

                TryStopReel(slot, 0, 3.5f, d.GamblingMultiplier);
                TryStopReel(slot, 1, 6.0f, d.GamblingMultiplier);
                if (TryStopReel(slot, 2, 9.0f, d.GamblingMultiplier))
                {
                    slot.SlotPhase = GamblingSlotPhase.Done;
                    slot.ResultDisplayTimer = 3.5f;
                    TriggerHudGamblingResult(d);
                }
                break;

            case GamblingSlotPhase.Done:
                slot.ResultDisplayTimer -= uiDelta;
                if (slot.ResultDisplayTimer <= 0f &&
                    d.GamblingBetCount < 2 &&
                    d.IdleActivityTimer > 12f &&
                    d.Money >= WorkerGamblingMinBet)
                {
                    slot.SlotPhase = GamblingSlotPhase.ResultPause;
                    ResolveWorkerGamblingSpinResult(d);
                }
                break;
        }
    }

    private bool TryStopReel(ServiceWorkerSlotUi slot, int r, float atTime, int multiplier)
    {
        if (slot.ReelStopped[r] || slot.SpinTimer < atTime)
        {
            return false;
        }

        slot.ReelStopped[r] = true;
        slot.ReelTexts[r].text = slot.FinalReelChars[r];
        slot.ReelTexts[r].color = GetReelResultColor(multiplier);
        if (IsGamblingHallQuickHudVisible())
        {
            PlayUiSound(uiPanelCloseClip, 0.55f);
        }
        return true;
    }

    private void TriggerHudGamblingResult(DriverAgent d)
    {
        if (d.GamblingMoneyPending)
        {
            d.GamblingMoneyPending = false;
            int net = d.GamblingPayout - d.GamblingBet;
            d.Money = Mathf.Max(0, d.Money + net);

            if (locations.TryGetValue(LocationType.GamblingHall, out LocationData gh))
            {
                gh.BuildingBank = Mathf.Max(0, gh.BuildingBank - net);
            }

            if (d.DriverObject != null)
            {
                Vector3 pos = d.DriverObject.transform.position;
                if (d.GamblingMultiplier == 0) SpawnMoneySpendPopup(pos, d.GamblingBet - d.GamblingPayout);
                else if (net > 0) SpawnMoneyEarnPopup(pos, net);
            }
            SessionDebugLogger.Log("NEEDS", $"{d.DriverName} gambling resolved: net={net:+#;-#;0}, balance=${d.Money}.");
        }

        bool hudVisible = IsGamblingHallQuickHudVisible();
        if (!hudVisible)
        {
            return;
        }

        if (d.GamblingMultiplier == 0)
        {
            PlayUiSound(slotLoseClip, 0.88f);
            hudFlashColor = new Color(0.85f, 0.12f, 0.08f);
            hudFlashDuration = hudFlashTimer = 2.2f;
            hudShakeDuration = hudShakeTimer = 0.65f;
        }
        else
        {
            PlayUiSound(slotWinClip, 0.88f);
            hudFlashColor = d.GamblingMultiplier >= 5 ? new Color(0.05f, 0.82f, 0.18f) : new Color(0.4f, 0.75f, 1f);
            hudFlashDuration = hudFlashTimer = 2.2f;
            hudShakeDuration = 0f;
            hudShakeTimer = 0f;
        }
    }

    private bool IsGamblingHallQuickHudVisible()
    {
        return buildingQuickHud?.CanvasRoot != null &&
               buildingQuickHud.CanvasRoot.activeSelf &&
               selectedLocation.HasValue &&
               selectedLocation.Value == LocationType.GamblingHall;
    }

    private void UpdateHudGamblingEffects()
    {
        if (buildingQuickHud?.FlashOverlay == null) return;

        if (hudFlashTimer > 0f)
        {
            hudFlashTimer -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(hudFlashTimer / Mathf.Max(hudFlashDuration, 0.01f));
            float alpha = Mathf.SmoothStep(0f, 1f, t) * 0.38f;
            buildingQuickHud.FlashOverlay.color = new Color(hudFlashColor.r, hudFlashColor.g, hudFlashColor.b, alpha);
            buildingQuickHud.FlashOverlay.gameObject.SetActive(true);
        }
        else
        {
            buildingQuickHud.FlashOverlay.gameObject.SetActive(false);
        }

        if (hudShakeTimer > 0f && buildingQuickHud.Root != null)
        {
            hudShakeTimer -= Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(hudShakeTimer / Mathf.Max(hudShakeDuration, 0.01f));
            float shakeX = Mathf.Sin(hudShakeTimer * 42f) * 5f * progress;
            buildingQuickHud.Root.anchoredPosition = buildingQuickHud.OriginalPos + new Vector2(shakeX, 0f);
        }
        else if (buildingQuickHud.Root != null)
        {
            buildingQuickHud.Root.anchoredPosition = buildingQuickHud.OriginalPos;
        }
    }

    private static string[] GetReelFinalChars(int multiplier) => multiplier switch
    {
        10 => new[] { "7", "7", "7" },
        5 => new[] { "\u2605", "\u2605", "\u2605" },
        1 => new[] { "\u2666", "\u2666", "\u2666" },
        _ => new[] { "\u2663", "\u2660", "\u2665" }
    };

    private static Color GetReelResultColor(int multiplier) => multiplier switch
    {
        10 => new Color(1f, 0.85f, 0.1f),
        5 => new Color(0.95f, 0.80f, 0.2f),
        1 => new Color(0.4f, 0.85f, 1f),
        _ => new Color(0.6f, 0.3f, 0.3f)
    };

    private static LocationType? GetDriverServiceLocation(DriverRescuePhase phase)
    {
        return phase switch
        {
            DriverRescuePhase.IdleAtBar => LocationType.Bar,
            DriverRescuePhase.IdleAtCanteen => LocationType.Canteen,
            DriverRescuePhase.IdleAtKiosk => LocationType.Kiosk,
            DriverRescuePhase.IdleAtCoffeeShop => LocationType.CoffeeShop,
            DriverRescuePhase.IdleAtGamblingHall => LocationType.GamblingHall,
            DriverRescuePhase.IdleAtCityPark => LocationType.CityPark,
            DriverRescuePhase.AtLaborExchange => LocationType.LaborExchange,
            _ => (LocationType?)null
        };
    }

    private float GetServiceBuildingVisitDuration(LocationType type)
    {
        return type switch
        {
            LocationType.Bar => WorkerLeisureDuration,
            LocationType.Canteen => WorkerCanteenDuration,
            LocationType.Kiosk => WorkerVendorPurchaseDuration,
            LocationType.CoffeeShop => WorkerVendorPurchaseDuration,
            LocationType.GamblingHall => WorkerGamblingHallDuration,
            LocationType.CityPark => WorkerCityParkDuration,
            LocationType.Motel => DriverSleepDuration,
            LocationType.LaborExchange => LaborExchangeInterviewDuration,
            _ => 1f
        };
    }

    private static string GetServiceBuildingActivityLabel(LocationType type, bool ru)
    {
        return type switch
        {
            LocationType.Bar => ru ? "\u041f\u044c\u0435\u0442" : "Drinking",
            LocationType.Canteen => ru ? "\u0415\u0441\u0442" : "Eating",
            LocationType.Kiosk => ru ? "\u041f\u043e\u043a\u0443\u043f\u0430\u0435\u0442 \u0441\u043d\u044d\u043a" : "Buying snack",
            LocationType.CoffeeShop => ru ? "\u041f\u043e\u043a\u0443\u043f\u0430\u0435\u0442 \u043a\u043e\u0444\u0435" : "Buying coffee",
            LocationType.GamblingHall => ru ? "\u0418\u0433\u0440\u0430\u0435\u0442 \u0432 \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u044b" : "Playing slots",
            LocationType.CityPark => ru ? "\u0413\u0443\u043b\u044f\u0435\u0442 \u0432 \u043f\u0430\u0440\u043a\u0435" : "Strolling in park",
            LocationType.Motel => ru ? "\u0421\u043f\u0438\u0442" : "Sleeping",
            LocationType.LaborExchange => ru ? "\u0418\u0449\u0435\u0442 \u0440\u0430\u0431\u043e\u0442\u0443" : "Applying",
            _ => ru ? "\u0412\u043d\u0443\u0442\u0440\u0438" : "Inside"
        };
    }

    private string FormatGameTimeRemaining(float realSeconds)
    {
        float hours = realSeconds * 24f / DayNightCycleDuration;
        int h = Mathf.FloorToInt(hours);
        int m = Mathf.FloorToInt((hours - h) * 60f);
        return h > 0 ? $"{h}h {m}m" : $"{m}m";
    }
}
