using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private void UpdateRoadAccessWarningMarkers()
    {
        UpdateRoadAccessWarningMarkers(locations.Values);
        UpdateRoadAccessWarningMarkers(extraServiceLocations);
        UpdateRoadAccessWarningMarkers(localStops);
    }

    private void UpdateRoadAccessWarningMarkers(IEnumerable<LocationData> locationSet)
    {
        if (locationSet == null)
        {
            return;
        }

        foreach (LocationData location in locationSet)
        {
            UpdateRoadAccessWarningMarker(location);
        }
    }

    private void UpdateRoadAccessWarningMarker(LocationData location)
    {
        if (location == null || location.RootObject == null)
        {
            return;
        }

        if (!DoesLocationRequireRoadAccess(location.Type))
        {
            if (location.RoadAccessWarningMarker != null)
            {
                location.RoadAccessWarningMarker.SetActive(false);
            }
            return;
        }

        if (location.RoadAccessWarningMarker == null)
        {
            location.RoadAccessWarningMarker = CreateRoadAccessWarningMarker(location);
        }

        if (location.RoadAccessWarningMarker != null)
        {
            location.RoadAccessWarningMarker.SetActive(!IsRequiredLocationRoadConnected(location));
        }
    }

    private bool IsRequiredLocationRoadConnected(LocationData location)
    {
        if (location == null || !DoesLocationRequireRoadAccess(location.Type))
        {
            return true;
        }

        Vector2Int access = location.RoadAccess;
        if (!IsInsideGrid(access) || IsWaterOrBeachCell(access))
        {
            return false;
        }

        bool accessIsRoad = roadCells.Contains(access) || edgeHighwayCells.Contains(access);
        if (!accessIsRoad)
        {
            return false;
        }

        foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(access))
        {
            if (!IsInsideGrid(neighbor))
            {
                continue;
            }

            if (roadCells.Contains(neighbor) || edgeHighwayCells.Contains(neighbor))
            {
                return true;
            }
        }

        return edgeHighwayCells.Contains(access);
    }

    private GameObject CreateRoadAccessWarningMarker(LocationData location)
    {
        GameObject root = new($"RoadAccessWarning_{location.Type}_{location.InstanceId}");
        root.transform.SetParent(location.RootObject.transform, false);

        Vector3 center = new(
            (location.Min.x + location.Max.x + 1) * 0.5f,
            1.92f,
            (location.Min.y + location.Max.y + 1) * 0.5f);
        root.transform.localPosition = center;

        GameObject backplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backplate.name = "RoadAccessWarningBackplate";
        backplate.transform.SetParent(root.transform, false);
        backplate.transform.localPosition = Vector3.zero;
        backplate.transform.localScale = new Vector3(0.42f, 0.42f, 0.06f);
        ApplyColor(backplate, new Color(0.95f, 0.24f, 0.16f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(backplate, VisualSmoothnessVehicleMetal);
        if (backplate.TryGetComponent(out Collider backplateCollider))
        {
            backplateCollider.enabled = false;
        }

        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = "RoadAccessWarningLine";
        line.transform.SetParent(root.transform, false);
        line.transform.localPosition = new Vector3(0f, 0.06f, -0.04f);
        line.transform.localScale = new Vector3(0.065f, 0.22f, 0.035f);
        ApplyColor(line, new Color(0.08f, 0.06f, 0.05f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(line, VisualSmoothnessVehicleMetal);
        if (line.TryGetComponent(out Collider lineCollider))
        {
            lineCollider.enabled = false;
        }

        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dot.name = "RoadAccessWarningDot";
        dot.transform.SetParent(root.transform, false);
        dot.transform.localPosition = new Vector3(0f, -0.13f, -0.04f);
        dot.transform.localScale = new Vector3(0.065f, 0.065f, 0.035f);
        ApplyColor(dot, new Color(0.08f, 0.06f, 0.05f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(dot, VisualSmoothnessVehicleMetal);
        if (dot.TryGetComponent(out Collider dotCollider))
        {
            dotCollider.enabled = false;
        }

        root.SetActive(false);
        return root;
    }
}
