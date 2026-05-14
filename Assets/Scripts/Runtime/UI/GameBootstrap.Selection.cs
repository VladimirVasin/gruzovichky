using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void SetupSelectionVisuals()
    {
        foreach (KeyValuePair<LocationType, LocationData> pair in locations)
        {
            locationSelectionHighlights[pair.Key] = SelectionVisualService.CreateHighlight(
                worldRoot,
                pair.Value.Label,
                ApplyColor,
                ConfigureStaticVisual);
        }

        selectedLocationLabelRoot = SelectionVisualService.CreateLabelRoot(worldRoot, out selectedLocationLabelText, selectedLocationLabelOutlines);
        selectedEntityHighlight = SelectionVisualService.CreateHighlight(
            worldRoot,
            "SelectedEntity",
            ApplyColor,
            ConfigureStaticVisual);
    }

    private void UpdateSelectedLocationLabel()
    {
        if (selectedLocationLabelRoot == null || selectedLocationLabelText == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            selectedLocationLabelRoot.SetActive(false);
            return;
        }

        if (!TryGetSelectedBuilding(out LocationData location, out LocationType locationType, out Vector3 center))
        {
            selectedLocationLabelRoot.SetActive(false);
            return;
        }

        Vector3 labelPosition = center + new Vector3(0f, 1.45f, 0f);
        SelectionVisualService.UpdateLabelVisual(
            selectedLocationLabelRoot,
            selectedLocationLabelText,
            selectedLocationLabelOutlines,
            GetBuildingInstanceDisplayName(locationType, location.InstanceId),
            labelPosition,
            mainCamera.transform.position,
            34f,
            58f);
    }

    private bool TryHandleLocationSelection(Vector2Int cell)
    {
        foreach (KeyValuePair<LocationType, LocationData> pair in locations)
        {
            if (!pair.Value.Contains(cell) && pair.Value.Anchor != cell)
            {
                continue;
            }

            selectedLocation = pair.Key;
            selectedLocationInstanceId = pair.Value.InstanceId;
            selectedLocalStopIndex = -1;
            selectedPersonalHouseIndex = -1;
            isTruckDetailsOpen = false;
            isLocalBusDetailsOpen = false;
            isDriverDetailsOpen = false;
            selectedDriverId = 0;
            HideBuildingQuickHudSubmenuImmediate();
            RefreshSelectionVisuals();
            PlayUiSound(uiSelectClip, 0.9f);
            return true;
        }

        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            LocationData extraLocation = extraServiceLocations[i];
            if (extraLocation == null || (!extraLocation.Contains(cell) && extraLocation.Anchor != cell))
            {
                continue;
            }

            selectedLocation = extraLocation.Type;
            selectedLocationInstanceId = extraLocation.InstanceId;
            selectedLocalStopIndex = -1;
            selectedPersonalHouseIndex = -1;
            isTruckDetailsOpen = false;
            isLocalBusDetailsOpen = false;
            isDriverDetailsOpen = false;
            selectedDriverId = 0;
            HideBuildingQuickHudSubmenuImmediate();
            RefreshSelectionVisuals();
            PlayUiSound(uiSelectClip, 0.9f);
            return true;
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            LocationData stop = localStops[i];
            if (!stop.Contains(cell) && stop.Anchor != cell)
            {
                continue;
            }

            selectedLocation = null;
            selectedLocationInstanceId = 0;
            selectedLocalStopIndex = i;
            selectedPersonalHouseIndex = -1;
            isTruckDetailsOpen = false;
            isLocalBusDetailsOpen = false;
            isDriverDetailsOpen = false;
            selectedDriverId = 0;
            HideBuildingQuickHudSubmenuImmediate();
            RefreshSelectionVisuals();
            PlayUiSound(uiSelectClip, 0.9f);
            return true;
        }

        for (int i = 0; i < personalHouses.Count; i++)
        {
            LocationData house = personalHouses[i];
            if (!house.Contains(cell) && house.Anchor != cell)
            {
                continue;
            }

            selectedLocation = null;
            selectedLocationInstanceId = 0;
            selectedLocalStopIndex = -1;
            selectedPersonalHouseIndex = i;
            isTruckDetailsOpen = false;
            isLocalBusDetailsOpen = false;
            isDriverDetailsOpen = false;
            selectedDriverId = 0;
            HideBuildingQuickHudSubmenuImmediate();
            RefreshSelectionVisuals();
            PlayUiSound(uiSelectClip, 0.9f);
            return true;
        }

        return false;
    }

    private void RefreshSelectionVisuals()
    {
        foreach (GameObject highlight in locationSelectionHighlights.Values)
        {
            if (highlight != null)
            {
                highlight.SetActive(false);
            }
        }

        for (int i = 0; i < localStopSelectionHighlights.Count; i++)
        {
            if (localStopSelectionHighlights[i] != null)
            {
                localStopSelectionHighlights[i].SetActive(false);
            }
        }

        for (int i = 0; i < personalHouseSelectionHighlights.Count; i++)
        {
            if (personalHouseSelectionHighlights[i] != null)
            {
                personalHouseSelectionHighlights[i].SetActive(false);
            }
        }

        if (selectedEntityHighlight != null)
        {
            selectedEntityHighlight.SetActive(false);
        }

        HideCleaningDepotSelectionRadius();

        if (!TryGetSelectedBuilding(out LocationData location, out LocationType locationType, out Vector3 center))
        {
            if (selectedLocationLabelRoot != null)
            {
                selectedLocationLabelRoot.SetActive(false);
            }

            UpdateSelectedEntityHighlight();
            return;
        }

        if (selectedLocalStopIndex >= 0)
        {
            if (selectedLocalStopIndex < localStopSelectionHighlights.Count)
            {
                GameObject selectionHighlight = localStopSelectionHighlights[selectedLocalStopIndex];
                if (selectionHighlight != null)
                {
                    Vector3 size = GetLocationSelectionHighlightSize(location);
                    selectionHighlight.transform.position = center + new Vector3(0f, 0.03f, 0f);
                    selectionHighlight.transform.localScale = size;
                    selectionHighlight.SetActive(true);
                }
            }
        }
        else if (selectedPersonalHouseIndex >= 0)
        {
            if (selectedPersonalHouseIndex < personalHouseSelectionHighlights.Count)
            {
                GameObject selectionHighlight = personalHouseSelectionHighlights[selectedPersonalHouseIndex];
                if (selectionHighlight != null)
                {
                    Vector3 size = GetLocationSelectionHighlightSize(location);
                    selectionHighlight.transform.position = center + new Vector3(0f, 0.03f, 0f);
                    selectionHighlight.transform.localScale = size;
                    selectionHighlight.SetActive(true);
                }
            }
        }
        else if (locationSelectionHighlights.TryGetValue(locationType, out GameObject selectionHighlight) && selectionHighlight != null)
        {
            Vector3 size = GetLocationSelectionHighlightSize(location);
            selectionHighlight.transform.position = center;
            selectionHighlight.transform.localScale = size;
            selectionHighlight.SetActive(true);
        }

        if (locationType == LocationType.CleaningDepot)
        {
            ShowCleaningDepotSelectionRadius(location);
        }

        if (selectedLocationLabelRoot != null)
        {
            selectedLocationLabelRoot.transform.position = center + new Vector3(0f, 1.45f, 0f);
            selectedLocationLabelRoot.SetActive(true);
        }
    }

    private void UpdateSelectedEntityHighlight()
    {
        if (selectedEntityHighlight == null)
        {
            return;
        }

        if (TryGetSelectedBuilding(out _, out _, out _))
        {
            selectedEntityHighlight.SetActive(false);
            return;
        }

        if (!TryGetSelectedEntityHighlightTarget(out Vector3 position, out Vector3 size))
        {
            selectedEntityHighlight.SetActive(false);
            return;
        }

        Vector3 markerPosition = new(position.x, SampleTerrainHeight(position.x, position.z) + 0.03f, position.z);
        selectedEntityHighlight.transform.position = markerPosition;
        selectedEntityHighlight.transform.localScale = size;
        selectedEntityHighlight.SetActive(true);
    }

    private static Vector3 GetLocationSelectionHighlightSize(LocationData location)
    {
        return location == null
            ? new Vector3(1.05f, 0.06f, 1.05f)
            : new Vector3(location.Max.x - location.Min.x + 1.05f, 0.06f, location.Max.y - location.Min.y + 1.05f);
    }

    private bool TryGetSelectedBuilding(out LocationData location, out LocationType locationType, out Vector3 center)
    {
        if (selectedLocalStopIndex >= 0 && selectedLocalStopIndex < localStops.Count)
        {
            location = localStops[selectedLocalStopIndex];
            locationType = LocationType.Stop;
            center = GetLocationCenter(location);
            return true;
        }

        if (selectedPersonalHouseIndex >= 0 && selectedPersonalHouseIndex < personalHouses.Count)
        {
            location = personalHouses[selectedPersonalHouseIndex];
            locationType = LocationType.PersonalHouse;
            center = GetLocationCenter(location);
            return true;
        }

        if (selectedLocation.HasValue)
        {
            if (selectedLocationInstanceId > 0)
            {
                LocationData instance = FindLocationByInstanceId(selectedLocationInstanceId);
                if (instance != null && instance.Type == selectedLocation.Value)
                {
                    location = instance;
                    locationType = selectedLocation.Value;
                    center = GetLocationCenter(location);
                    return true;
                }
            }

            if (locations.TryGetValue(selectedLocation.Value, out location))
            {
                locationType = selectedLocation.Value;
                center = GetLocationCenter(locationType);
                return true;
            }
        }

        location = null;
        locationType = default;
        center = Vector3.zero;
        return false;
    }
}
