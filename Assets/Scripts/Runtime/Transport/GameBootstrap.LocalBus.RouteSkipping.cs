using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private bool TryBeginFirstReachableLocalBusStop(List<LocationData> orderedStops, LocationData parking)
    {
        for (int i = 0; i < orderedStops.Count; i++)
        {
            LocationData candidate = orderedStops[i];
            if (TryBeginLocalBusDriveSegment(parking.Anchor, candidate.Anchor, LocalBusPhase.DrivingRoute))
            {
                localBusRoute.CurrentStopIndex = i;
                localBusRoute.TravelDirection = i >= orderedStops.Count - 1 ? -1 : 1;
                SessionDebugLogger.Log(
                    "BUS_SHIFT",
                    $"{localBusRoute.Driver?.DriverName ?? "Bus driver"} started local bus route cycle. Stops={orderedStops.Count}, firstReachableStop=#{candidate.StopNumber}, skippedBefore={i}.");
                return true;
            }

            SessionDebugLogger.Log(
                "BUS_SHIFT",
                $"{localBusRoute.Driver?.DriverName ?? "Bus driver"} skipped Stop #{candidate.StopNumber} at route start: no road path from Parking.");
        }

        SessionDebugLogger.Log(
            "BUS_SHIFT",
            $"{localBusRoute?.Driver?.DriverName ?? "Bus driver"} cannot start local bus route: no reachable stops from Parking.");
        return false;
    }

    private bool TryBeginNextReachableLocalBusStop(List<LocationData> orderedStops)
    {
        int originIndex = Mathf.Clamp(localBusRoute.CurrentStopIndex, 0, orderedStops.Count - 1);
        Vector2Int startAnchor = orderedStops[originIndex].Anchor;
        int scanIndex = originIndex;
        int scanDirection = localBusRoute.TravelDirection;
        int maxAttempts = Mathf.Max(1, orderedStops.Count * 2);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            LocalBusNextStopDecision decision = LocalBusRoutePlanner.GetNextStop(
                orderedStops.Count,
                scanIndex,
                scanDirection);
            if (!decision.HasNextStop)
            {
                break;
            }

            scanDirection = decision.TravelDirection;
            scanIndex = decision.NextStopIndex;
            if (scanIndex == originIndex)
            {
                continue;
            }

            LocationData candidate = orderedStops[scanIndex];
            if (TryBeginLocalBusDriveSegment(startAnchor, candidate.Anchor, LocalBusPhase.DrivingRoute))
            {
                localBusRoute.TravelDirection = scanDirection;
                localBusRoute.CurrentStopIndex = scanIndex;
                SessionDebugLogger.Log(
                    "BUS_SHIFT",
                    $"{localBusRoute.Driver?.DriverName ?? "Bus driver"} heading to next reachable stop #{candidate.StopNumber} ({(localBusRoute.TravelDirection > 0 ? "ascending" : "descending")} leg).");
                return true;
            }

            SessionDebugLogger.Log(
                "BUS_SHIFT",
                $"{localBusRoute.Driver?.DriverName ?? "Bus driver"} skipped Stop #{candidate.StopNumber}: no road path from Stop #{orderedStops[originIndex].StopNumber}.");
        }

        SessionDebugLogger.Log(
            "BUS_SHIFT",
            $"{localBusRoute?.Driver?.DriverName ?? "Bus driver"} found no reachable next stop from Stop #{orderedStops[originIndex].StopNumber}; returning to Parking.");
        BeginLocalBusReturnToParking();
        return true;
    }
}
