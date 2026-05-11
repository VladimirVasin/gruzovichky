using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float RoadAccessWarningBaseHeight = 2.34f;
    private const float RoadAccessWarningBobAmplitude = 0.12f;
    private const float RoadAccessWarningBobSpeed = 3.2f;
    private const float RoadAccessWarningPulseSpeed = 4.6f;
    private const float RoadAccessWarningPulseScale = 0.08f;

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
            bool shouldWarn = !IsRequiredLocationRoadConnected(location);
            location.RoadAccessWarningMarker.SetActive(shouldWarn);
            if (shouldWarn)
            {
                AnimateRoadAccessWarningMarker(location);
            }
        }
    }

    private void UpdateRoadAccessWarningMarkerRuntime()
    {
        UpdateRoadAccessWarningMarkerRuntime(locations.Values);
        UpdateRoadAccessWarningMarkerRuntime(extraServiceLocations);
        UpdateRoadAccessWarningMarkerRuntime(localStops);
    }

    private void UpdateRoadAccessWarningMarkerRuntime(IEnumerable<LocationData> locationSet)
    {
        if (locationSet == null)
        {
            return;
        }

        foreach (LocationData location in locationSet)
        {
            AnimateRoadAccessWarningMarker(location);
        }
    }

    private void AnimateRoadAccessWarningMarker(LocationData location)
    {
        if (location?.RoadAccessWarningMarker == null || !location.RoadAccessWarningMarker.activeSelf)
        {
            return;
        }

        Transform marker = location.RoadAccessWarningMarker.transform;
        float bobPhase = Time.unscaledTime * RoadAccessWarningBobSpeed + location.InstanceId * 0.73f;
        float pulsePhase = Time.unscaledTime * RoadAccessWarningPulseSpeed + location.InstanceId * 1.17f;
        Vector3 basePosition = GetRoadAccessWarningBaseLocalPosition(location);
        basePosition.y += Mathf.Sin(bobPhase) * RoadAccessWarningBobAmplitude;
        marker.localPosition = basePosition;
        marker.localScale = Vector3.one * (1f + Mathf.Sin(pulsePhase) * RoadAccessWarningPulseScale);

        if (mainCamera != null)
        {
            marker.rotation = mainCamera.transform.rotation;
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

    private static Vector3 GetRoadAccessWarningBaseLocalPosition(LocationData location)
    {
        return new Vector3(
            (location.Min.x + location.Max.x + 1) * 0.5f,
            RoadAccessWarningBaseHeight,
            (location.Min.y + location.Max.y + 1) * 0.5f);
    }

    private GameObject CreateRoadAccessWarningMarker(LocationData location)
    {
        GameObject root = new($"RoadAccessWarning_{location.Type}_{location.InstanceId}");
        root.transform.SetParent(location.RootObject.transform, false);

        root.transform.localPosition = GetRoadAccessWarningBaseLocalPosition(location);

        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        halo.name = "RoadAccessWarningHalo";
        halo.transform.SetParent(root.transform, false);
        halo.transform.localPosition = new Vector3(0f, 0f, 0.03f);
        halo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        halo.transform.localScale = new Vector3(0.86f, 0.86f, 0.045f);
        ApplyColor(halo, new Color(1f, 0.72f, 0.08f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(halo, VisualSmoothnessVehicleMetal);
        DisableRoadAccessWarningCollider(halo);

        GameObject backplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backplate.name = "RoadAccessWarningBackplate";
        backplate.transform.SetParent(root.transform, false);
        backplate.transform.localPosition = new Vector3(0f, 0f, -0.005f);
        backplate.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        backplate.transform.localScale = new Vector3(0.66f, 0.66f, 0.06f);
        ApplyColor(backplate, new Color(0.98f, 0.18f, 0.12f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(backplate, VisualSmoothnessVehicleMetal);
        DisableRoadAccessWarningCollider(backplate);

        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = "RoadAccessWarningLine";
        line.transform.SetParent(root.transform, false);
        line.transform.localPosition = new Vector3(0f, 0.09f, -0.055f);
        line.transform.localScale = new Vector3(0.09f, 0.34f, 0.035f);
        ApplyColor(line, new Color(0.08f, 0.06f, 0.05f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(line, VisualSmoothnessVehicleMetal);
        DisableRoadAccessWarningCollider(line);

        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dot.name = "RoadAccessWarningDot";
        dot.transform.SetParent(root.transform, false);
        dot.transform.localPosition = new Vector3(0f, -0.18f, -0.055f);
        dot.transform.localScale = new Vector3(0.09f, 0.09f, 0.035f);
        ApplyColor(dot, new Color(0.08f, 0.06f, 0.05f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(dot, VisualSmoothnessVehicleMetal);
        DisableRoadAccessWarningCollider(dot);

        GameObject leftRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftRoad.name = "RoadAccessWarningBrokenRoadLeft";
        leftRoad.transform.SetParent(root.transform, false);
        leftRoad.transform.localPosition = new Vector3(-0.15f, -0.31f, -0.055f);
        leftRoad.transform.localScale = new Vector3(0.19f, 0.055f, 0.035f);
        ApplyColor(leftRoad, new Color(0.08f, 0.06f, 0.05f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(leftRoad, VisualSmoothnessVehicleMetal);
        DisableRoadAccessWarningCollider(leftRoad);

        GameObject rightRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightRoad.name = "RoadAccessWarningBrokenRoadRight";
        rightRoad.transform.SetParent(root.transform, false);
        rightRoad.transform.localPosition = new Vector3(0.15f, -0.31f, -0.055f);
        rightRoad.transform.localScale = new Vector3(0.19f, 0.055f, 0.035f);
        ApplyColor(rightRoad, new Color(0.08f, 0.06f, 0.05f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(rightRoad, VisualSmoothnessVehicleMetal);
        DisableRoadAccessWarningCollider(rightRoad);

        root.SetActive(false);
        return root;
    }

    private static void DisableRoadAccessWarningCollider(GameObject target)
    {
        if (target.TryGetComponent(out Collider collider))
        {
            collider.enabled = false;
        }
    }
}
