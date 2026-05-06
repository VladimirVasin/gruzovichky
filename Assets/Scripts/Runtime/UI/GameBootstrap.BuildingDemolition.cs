using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private GameObject demolishConfirmCanvasRoot;
    private Text demolishConfirmTitleText;
    private Text demolishConfirmBodyText;
    private LocationData pendingDemolishLocation;
    private LocationType pendingDemolishLocationType;
    private int pendingDemolishLocationInstanceId;
    private int selectedLocationInstanceId;

    private bool IsDemolishConfirmOpen() =>
        demolishConfirmCanvasRoot != null && demolishConfirmCanvasRoot.activeSelf;

    private bool TryOpenSelectedBuildingDemolishConfirm()
    {
        if (IsDemolishConfirmOpen())
        {
            return true;
        }

        if (!TryGetSelectedBuilding(out LocationData location, out LocationType locationType, out _))
        {
            return false;
        }

        if (!CanDemolishLocation(location, locationType, out string enReason, out string ruReason))
        {
            PushFeedEvent(enReason, ruReason, FeedEventType.Warning);
            PlayUiSound(uiPanelCloseClip, 0.72f);
            return true;
        }

        pendingDemolishLocation = location;
        pendingDemolishLocationType = locationType;
        pendingDemolishLocationInstanceId = location.InstanceId;
        EnsureDemolishConfirmModal();

        bool ru = IsRussianLanguage();
        demolishConfirmTitleText.text = ru ? "\u0421\u043d\u0435\u0441\u0442\u0438 \u0437\u0434\u0430\u043d\u0438\u0435?" : "Demolish building?";
        demolishConfirmBodyText.text = ru
            ? $"{GetBuildingInstanceDisplayName(locationType, location.InstanceId)}\n\u0414\u0435\u0439\u0441\u0442\u0432\u0438\u0435 \u043d\u0435\u043b\u044c\u0437\u044f \u043e\u0442\u043c\u0435\u043d\u0438\u0442\u044c."
            : $"{GetBuildingInstanceDisplayName(locationType, location.InstanceId)}\nThis action cannot be undone.";
        demolishConfirmCanvasRoot.SetActive(true);
        PlayUiSound(uiPanelOpenClip, 0.82f);
        return true;
    }

    private bool CanDemolishLocation(LocationData location, LocationType locationType, out string enReason, out string ruReason)
    {
        enReason = "Building cannot be demolished yet.";
        ruReason = "\u0417\u0434\u0430\u043d\u0438\u0435 \u043f\u043e\u043a\u0430 \u043d\u0435\u043b\u044c\u0437\u044f \u0441\u043d\u0435\u0441\u0442\u0438.";

        if (location == null)
        {
            return false;
        }

        if (locationType == LocationType.IntercityStop)
        {
            enReason = "Intercity stop is fixed infrastructure.";
            ruReason = "\u041c\u0435\u0436\u0434\u0443\u0433\u043e\u0440\u043e\u0434\u043d\u044e\u044e \u043e\u0441\u0442\u0430\u043d\u043e\u0432\u043a\u0443 \u043d\u0435\u043b\u044c\u0437\u044f \u0441\u043d\u0435\u0441\u0442\u0438.";
            return false;
        }

        if (locationType is LocationType.Parking or LocationType.Warehouse or LocationType.Forest or
            LocationType.Sawmill or LocationType.FurnitureFactory or LocationType.Docks)
        {
            enReason = "This core logistics building cannot be demolished yet.";
            ruReason = "\u042d\u0442\u043e \u043a\u043b\u044e\u0447\u0435\u0432\u043e\u0435 \u043b\u043e\u0433\u0438\u0441\u0442\u0438\u0447\u0435\u0441\u043a\u043e\u0435 \u0437\u0434\u0430\u043d\u0438\u0435 \u043f\u043e\u043a\u0430 \u043d\u0435\u043b\u044c\u0437\u044f \u0441\u043d\u0435\u0441\u0442\u0438.";
            return false;
        }

        return true;
    }

    private void EnsureDemolishConfirmModal()
    {
        if (demolishConfirmCanvasRoot != null)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureFleetEventSystem();
        demolishConfirmCanvasRoot = new GameObject("DemolishConfirmCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        demolishConfirmCanvasRoot.transform.SetParent(transform, false);
        Canvas canvas = demolishConfirmCanvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler scaler = demolishConfirmCanvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        RectTransform overlayRect = CreateUiObject("Overlay", demolishConfirmCanvasRoot.transform).GetComponent<RectTransform>();
        StretchRect(overlayRect, 0f, 0f, 0f, 0f);
        Image overlayImage = overlayRect.gameObject.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.52f);
        overlayImage.raycastTarget = true;

        RectTransform panel = CreateVerticalStack(
            "Panel",
            overlayRect,
            new RectOffset(22, 22, 18, 18),
            12f,
            430f,
            190f,
            -1f,
            -1f);
        SetCenteredWindow(panel, 430f, 190f, 0f);
        panel.SetAsLastSibling();
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.10f, 0.13f, 0.18f, 0.98f);
        panelImage.raycastTarget = true;
        Outline panelOutline = panel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0f, 0f, 0f, 0.55f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        demolishConfirmTitleText = CreateHeaderText("Title", panel, font, string.Empty, 22, TextAnchor.MiddleLeft, Color.white);
        demolishConfirmTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        demolishConfirmBodyText = CreateBodyText("Body", panel, font, string.Empty, 14, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        demolishConfirmBodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;

        RectTransform buttonLayer = CreateUiObject("ButtonLayer", panel).GetComponent<RectTransform>();
        buttonLayer.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
        Button noButton = CreateButton("NoButton", buttonLayer, font, out Text noText, "\u041d\u0435\u0442", 14, new Color(0.25f, 0.30f, 0.38f, 1f), Color.white);
        RectTransform noRect = noButton.GetComponent<RectTransform>();
        noRect.anchorMin = new Vector2(1f, 0.5f);
        noRect.anchorMax = new Vector2(1f, 0.5f);
        noRect.pivot = new Vector2(1f, 0.5f);
        noRect.sizeDelta = new Vector2(118f, 38f);
        noRect.anchoredPosition = new Vector2(-132f, 0f);
        Button yesButton = CreateButton("YesButton", buttonLayer, font, out Text yesText, "\u0414\u0430", 14, new Color(0.58f, 0.18f, 0.13f, 1f), Color.white);
        RectTransform yesRect = yesButton.GetComponent<RectTransform>();
        yesRect.anchorMin = new Vector2(1f, 0.5f);
        yesRect.anchorMax = new Vector2(1f, 0.5f);
        yesRect.pivot = new Vector2(1f, 0.5f);
        yesRect.sizeDelta = new Vector2(118f, 38f);
        yesRect.anchoredPosition = new Vector2(0f, 0f);
        noText.raycastTarget = false;
        yesText.raycastTarget = false;
        noButton.onClick.AddListener(() => CloseDemolishConfirm(true));
        yesButton.onClick.AddListener(ConfirmPendingDemolishLocation);

        demolishConfirmCanvasRoot.SetActive(false);
    }

    private void CloseDemolishConfirm(bool playSound)
    {
        if (demolishConfirmCanvasRoot != null)
        {
            demolishConfirmCanvasRoot.SetActive(false);
        }

        pendingDemolishLocation = null;
        pendingDemolishLocationInstanceId = 0;
        if (playSound)
        {
            PlayUiSound(uiPanelCloseClip, 0.78f);
        }
    }

    private void ConfirmPendingDemolishLocation()
    {
        LocationData location = FindLocationByInstanceId(pendingDemolishLocationInstanceId) ?? pendingDemolishLocation;
        LocationType locationType = pendingDemolishLocationType;
        if (location == null || !CanDemolishLocation(location, locationType, out _, out _))
        {
            CloseDemolishConfirm(true);
            return;
        }

        string displayName = GetBuildingInstanceDisplayName(locationType, location.InstanceId);
        ClearAssignmentsForDemolishedLocation(location);
        RemoveLocationFromRuntimeCollections(location, locationType);

        if (location.RootObject != null)
        {
            Destroy(location.RootObject);
        }

        if (location.DocksShipObject != null)
        {
            Destroy(location.DocksShipObject);
            location.DocksShipObject = null;
        }

        selectedLocation = null;
        selectedLocationInstanceId = 0;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        isTruckDetailsOpen = false;
        isLocalBusDetailsOpen = false;
        isDriverDetailsOpen = false;
        if (buildingQuickHud?.CanvasRoot != null)
        {
            buildingQuickHud.CanvasRoot.SetActive(false);
        }

        CloseDemolishConfirm(false);
        RefreshSelectionVisuals();
        MarkRuntimeScreensDirtyAfterDemolition();
        PlayUiSound(buildingDemolishClip, 0.9f);
        PushFeedEvent(
            $"{displayName} demolished.",
            $"{displayName} \u0441\u043d\u0435\u0441\u0451\u043d.",
            FeedEventType.Warning);
        SessionDebugLogger.Log("BUILD", $"Demolished {locationType}#{location.InstanceId} ({displayName}).");
    }

    private void ClearAssignmentsForDemolishedLocation(LocationData location)
    {
        int instanceId = location.InstanceId;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver == null)
            {
                continue;
            }

            if (driver.DutyMode == DriverDutyMode.Logistics &&
                driver.AssignedBuildingType == location.Type &&
                ResolveBuildingInstanceId(location.Type, driver.AssignedBuildingInstanceId) == instanceId)
            {
                SetDriverDutyMode(driver, DriverDutyMode.Local);
                driver.ContractVacancyKind = VacancyKind.None;
                driver.ContractBuildingType = null;
                driver.ContractBuildingInstanceId = 0;
                driver.ContractSlotIndex = -1;
                driver.ContractShiftIndex = -1;
            }
        }

        location.Workers = 0;
    }

    private void RemoveLocationFromRuntimeCollections(LocationData location, LocationType locationType)
    {
        if (locationType == LocationType.Stop)
        {
            int stopIndex = localStops.IndexOf(location);
            if (stopIndex >= 0)
            {
                localStops.RemoveAt(stopIndex);
                if (stopIndex < localStopSelectionHighlights.Count)
                {
                    Destroy(localStopSelectionHighlights[stopIndex]);
                    localStopSelectionHighlights.RemoveAt(stopIndex);
                }
                NormalizeLocalStopNumbers();
                UpdateLocalBusStopNetworkWarnings();
            }
            return;
        }

        if (locationType == LocationType.PersonalHouse)
        {
            int houseIndex = personalHouses.IndexOf(location);
            if (houseIndex >= 0)
            {
                personalHouses.RemoveAt(houseIndex);
                if (houseIndex < personalHouseSelectionHighlights.Count)
                {
                    Destroy(personalHouseSelectionHighlights[houseIndex]);
                    personalHouseSelectionHighlights.RemoveAt(houseIndex);
                }
                UpdateResidentsAfterHouseDemolished(houseIndex);
            }
            return;
        }

        extraServiceLocations.Remove(location);
        if (locations.TryGetValue(locationType, out LocationData primary) && primary == location)
        {
            LocationData promoted = PopExtraServiceLocation(locationType);
            if (promoted != null)
            {
                locations[locationType] = promoted;
                if (worldRoot != null && !locationSelectionHighlights.ContainsKey(locationType))
                {
                    locationSelectionHighlights[locationType] = SelectionVisualService.CreateHighlight(
                        worldRoot,
                        promoted.Label,
                        ApplyColor,
                        ConfigureStaticVisual);
                }
            }
            else
            {
                locations.Remove(locationType);
                if (locationSelectionHighlights.TryGetValue(locationType, out GameObject highlight))
                {
                    Destroy(highlight);
                    locationSelectionHighlights.Remove(locationType);
                }
            }
        }
    }

    private LocationData PopExtraServiceLocation(LocationType locationType)
    {
        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            LocationData candidate = extraServiceLocations[i];
            if (candidate != null && candidate.Type == locationType)
            {
                extraServiceLocations.RemoveAt(i);
                return candidate;
            }
        }

        return null;
    }

    private void UpdateResidentsAfterHouseDemolished(int removedHouseIndex)
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver == null)
            {
                continue;
            }

            if (driver.AssignedPersonalHouseIndex == removedHouseIndex)
            {
                driver.AssignedPersonalHouseIndex = -1;
                driver.HasOwnedCarParking = false;
                if (driver.OwnedCarObject != null)
                {
                    Destroy(driver.OwnedCarObject);
                    driver.OwnedCarObject = null;
                }
            }
            else if (driver.AssignedPersonalHouseIndex > removedHouseIndex)
            {
                driver.AssignedPersonalHouseIndex--;
            }
        }

        HandleWorkerFamiliesAfterHouseDemolished(removedHouseIndex);
    }

    private void MarkRuntimeScreensDirtyAfterDemolition()
    {
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isEconomyScreenDirty = true;
        isShiftsScreenDirty = true;
        isTradeScreenDirty = true;
        isStatesScreenDirty = true;
    }
}
