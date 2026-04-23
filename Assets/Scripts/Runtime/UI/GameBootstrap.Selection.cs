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
            GetSelectedLocationDisplayName(locationType),
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
            selectedLocalStopIndex = -1;
            isTruckDetailsOpen = false;
            isLocalBusDetailsOpen = false;
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
            selectedLocalStopIndex = -1;
            selectedLocalStopIndex = i;
            isTruckDetailsOpen = false;
            isLocalBusDetailsOpen = false;
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

        if (!TryGetSelectedBuilding(out LocationData location, out LocationType locationType, out Vector3 center))
        {
            if (selectedLocationLabelRoot != null)
            {
                selectedLocationLabelRoot.SetActive(false);
            }

            return;
        }

        if (selectedLocalStopIndex >= 0)
        {
            if (selectedLocalStopIndex < localStopSelectionHighlights.Count)
            {
                GameObject selectionHighlight = localStopSelectionHighlights[selectedLocalStopIndex];
                if (selectionHighlight != null)
                {
                    Vector3 size = new Vector3(location.Max.x - location.Min.x + 1.05f, 0.06f, location.Max.y - location.Min.y + 1.05f);
                    selectionHighlight.transform.position = center + new Vector3(0f, 0.03f, 0f);
                    selectionHighlight.transform.localScale = size;
                    selectionHighlight.SetActive(true);
                }
            }
        }
        else if (locationSelectionHighlights.TryGetValue(locationType, out GameObject selectionHighlight) && selectionHighlight != null)
        {
            Vector3 size = new Vector3(location.Max.x - location.Min.x + 1.05f, 0.06f, location.Max.y - location.Min.y + 1.05f);
            selectionHighlight.transform.position = center;
            selectionHighlight.transform.localScale = size;
            selectionHighlight.SetActive(true);
        }

        if (selectedLocationLabelRoot != null)
        {
            selectedLocationLabelRoot.transform.position = center + new Vector3(0f, 1.45f, 0f);
            selectedLocationLabelRoot.SetActive(true);
        }
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

        if (selectedLocation.HasValue && locations.TryGetValue(selectedLocation.Value, out location))
        {
            locationType = selectedLocation.Value;
            center = GetLocationCenter(locationType);
            return true;
        }

        location = null;
        locationType = default;
        center = Vector3.zero;
        return false;
    }
}


