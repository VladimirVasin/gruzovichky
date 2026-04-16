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

        if (!selectedLocation.HasValue || !locations.TryGetValue(selectedLocation.Value, out LocationData location) || mainCamera == null)
        {
            selectedLocationLabelRoot.SetActive(false);
            return;
        }

        Vector3 labelPosition = GetLocationCenter(selectedLocation.Value) + new Vector3(0f, 1.45f, 0f);
        SelectionVisualService.UpdateLabelVisual(
            selectedLocationLabelRoot,
            selectedLocationLabelText,
            selectedLocationLabelOutlines,
            GetSelectedLocationDisplayName(selectedLocation.Value),
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
            isTruckDetailsOpen = false;
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

        if (!selectedLocation.HasValue || !locations.TryGetValue(selectedLocation.Value, out LocationData location))
        {
            if (selectedLocationLabelRoot != null)
            {
                selectedLocationLabelRoot.SetActive(false);
            }

            return;
        }

        LocationType locationType = selectedLocation.Value;
        if (locationSelectionHighlights.TryGetValue(locationType, out GameObject selectionHighlight) && selectionHighlight != null)
        {
            Vector3 center = GetLocationCenter(locationType) + new Vector3(0f, 0.03f, 0f);
            Vector3 size = new Vector3(location.Max.x - location.Min.x + 1.05f, 0.06f, location.Max.y - location.Min.y + 1.05f);
            selectionHighlight.transform.position = center;
            selectionHighlight.transform.localScale = size;
            selectionHighlight.SetActive(true);
        }

        if (selectedLocationLabelRoot != null)
        {
            selectedLocationLabelRoot.transform.position = GetLocationCenter(locationType) + new Vector3(0f, 1.45f, 0f);
            selectedLocationLabelRoot.SetActive(true);
        }
    }
}
