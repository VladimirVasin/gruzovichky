using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float ImportedBuildingDoorOpenAngle = 78f;
    private const float ImportedBuildingDoorSpeed = 3.8f;
    private const float ImportedBuildingDoorHoldDuration = 1.2f;

    private void RegisterImportedServiceInteractionMetadata(LocationData owner, Transform modelRoot)
    {
        if (owner == null || modelRoot == null)
        {
            return;
        }

        ImportedBuildingRuntime runtime = new()
        {
            DoorTransform = FindImportedTransform(modelRoot, "Door"),
            DoorEnterMarker = FindImportedTransform(modelRoot, "P_Door_Enter"),
            DoorInsideMarker = FindImportedTransform(modelRoot, "P_Door_Inside"),
            VisitorStandMarker = FindImportedTransform(modelRoot, "P_Visitor_Stand"),
            TableLookAtMarker = FindImportedTransform(modelRoot, "P_TableLookAt") ??
                FindImportedTransform(modelRoot, "Outdoor_Table_Top") ??
                FindImportedTransform(modelRoot, "JackpotSign_Face")
        };

        RegisterImportedBuildingDoor(runtime, FindImportedTransform(modelRoot, "Door"), ImportedBuildingDoorOpenAngle);
        RegisterImportedBuildingDoor(runtime, FindImportedTransform(modelRoot, "DoubleDoor_Left"), ImportedBuildingDoorOpenAngle);
        RegisterImportedBuildingDoor(runtime, FindImportedTransform(modelRoot, "DoubleDoor_Right"), -ImportedBuildingDoorOpenAngle);

        List<ImportedBuildingSeatAnchor> seatAnchors = FindImportedServiceSeatAnchors(modelRoot, owner.Type);
        List<Transform> lookAtMarkers = FindImportedTransformsByPrefix(modelRoot, "P_TableLookAt");
        if (runtime.TableLookAtMarker != null && !lookAtMarkers.Contains(runtime.TableLookAtMarker))
        {
            lookAtMarkers.Add(runtime.TableLookAtMarker);
        }

        for (int i = 0; i < seatAnchors.Count; i++)
        {
            ImportedBuildingSeatAnchor seatAnchor = seatAnchors[i];
            Transform seatMarker = seatAnchor.Marker;
            runtime.Seats.Add(new ImportedBuildingSeat
            {
                SeatMarker = seatMarker,
                LookAtMarker = FindNearestImportedMarker(seatAnchor.WorldPosition, lookAtMarkers) ?? runtime.TableLookAtMarker,
                SeatWorldPosition = seatAnchor.WorldPosition,
                HasSeatWorldPosition = seatAnchor.HasWorldPosition
            });
        }

        owner.ImportedRuntime = runtime;
    }

    private void UpdateImportedBuildingInteractions(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        foreach (LocationData location in locations.Values)
        {
            UpdateImportedBuildingInteraction(location, deltaTime);
        }

        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            UpdateImportedBuildingInteraction(extraServiceLocations[i], deltaTime);
        }
    }

    private void UpdateImportedBuildingInteraction(LocationData location, float deltaTime)
    {
        ImportedBuildingRuntime runtime = location?.ImportedRuntime;
        if (runtime == null || runtime.Doors.Count == 0)
        {
            return;
        }

        if (runtime.DoorHoldTimer > 0f)
        {
            runtime.DoorHoldTimer = Mathf.Max(0f, runtime.DoorHoldTimer - deltaTime);
            runtime.DoorTargetOpenAmount = 1f;
        }
        else
        {
            runtime.DoorTargetOpenAmount = 0f;
        }

        runtime.DoorOpenAmount = Mathf.MoveTowards(
            runtime.DoorOpenAmount,
            runtime.DoorTargetOpenAmount,
            deltaTime * ImportedBuildingDoorSpeed);
        float eased = Mathf.SmoothStep(0f, 1f, runtime.DoorOpenAmount);
        for (int i = 0; i < runtime.Doors.Count; i++)
        {
            ImportedBuildingDoor door = runtime.Doors[i];
            if (door?.DoorTransform == null)
            {
                continue;
            }

            door.DoorTransform.localRotation = Quaternion.Slerp(
                door.ClosedLocalRotation,
                door.OpenLocalRotation,
                eased);
        }
    }

    private Vector3 GetImportedServiceVisitTarget(LocationData service)
    {
        if (TryGetImportedServiceDoorWorldPosition(service, true, out Vector3 doorPosition) &&
            IsDriverSafeWalkCell(WorldToCell(doorPosition)))
        {
            return doorPosition;
        }

        Vector3 target = GetCellCenter(service.RoadAccess == default ? service.Anchor : service.RoadAccess);
        target.x += Random.Range(-0.18f, 0.18f);
        target.z += Random.Range(-0.18f, 0.18f);
        target.y = SampleTerrainHeight(target.x, target.z);

        return target;
    }

    private bool TrySeatWorkerAtImportedService(DriverAgent driver, LocationData service)
    {
        ImportedBuildingRuntime runtime = service?.ImportedRuntime;
        if (driver?.DriverObject == null || runtime == null)
        {
            return false;
        }

        RequestImportedBuildingDoorOpen(service);
        for (int i = 0; i < runtime.Seats.Count; i++)
        {
            ImportedBuildingSeat seat = runtime.Seats[i];
            if (seat?.SeatMarker == null || seat.OccupantDriverId != 0)
            {
                continue;
            }

            seat.OccupantDriverId = driver.DriverId;
            driver.ImportedBarSeatLocationInstanceId = service.InstanceId;
            driver.ImportedBarSeatIndex = i;
            Vector3 seatPosition = GetImportedServiceSeatWorldPosition(seat);
            driver.DriverObject.SetActive(true);
            driver.DriverObject.transform.position = seatPosition;
            driver.WalkTargetWorld = seatPosition;
            FaceImportedServiceSeat(driver, seat);
            ApplyDriverSittingPose(driver);
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} sat at {service.Type} imported seat {i + 1}.");
            return true;
        }

        return false;
    }

    private void ReleaseImportedBarSeat(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        LocationData location = FindLocationByInstanceId(driver.ImportedBarSeatLocationInstanceId);
        ImportedBuildingRuntime runtime = location?.ImportedRuntime;
        int seatIndex = driver.ImportedBarSeatIndex;
        if (runtime != null && seatIndex >= 0 && seatIndex < runtime.Seats.Count)
        {
            ImportedBuildingSeat seat = runtime.Seats[seatIndex];
            if (seat != null && seat.OccupantDriverId == driver.DriverId)
            {
                seat.OccupantDriverId = 0;
            }
        }

        driver.ImportedBarSeatLocationInstanceId = 0;
        driver.ImportedBarSeatIndex = -1;
    }

    private bool TryGetImportedServiceExitPosition(DriverAgent driver, LocationType expectedType, out Vector3 exitPosition)
    {
        exitPosition = Vector3.zero;
        LocationData location = FindLocationByInstanceId(driver?.InsideBuildingInstanceId ?? 0);
        if (location == null || location.Type != expectedType)
        {
            locations.TryGetValue(expectedType, out location);
        }

        if (location == null)
        {
            return false;
        }

        RequestImportedBuildingDoorOpen(location);
        if (!TryGetImportedServiceDoorWorldPosition(location, true, out exitPosition))
        {
            exitPosition = GetCellCenter(location.RoadAccess == default ? location.Anchor : location.RoadAccess);
        }

        if (!IsDriverSafeWalkCell(WorldToCell(exitPosition)))
        {
            exitPosition = GetCellCenter(location.RoadAccess == default ? location.Anchor : location.RoadAccess);
        }

        exitPosition.y = SampleTerrainHeight(exitPosition.x, exitPosition.z);
        return true;
    }

    private bool TryApplyImportedServiceSeatPose(DriverAgent driver)
    {
        if (driver == null || driver.ImportedBarSeatLocationInstanceId <= 0 || driver.ImportedBarSeatIndex < 0)
        {
            return false;
        }

        LocationData location = FindLocationByInstanceId(driver.ImportedBarSeatLocationInstanceId);
        ImportedBuildingRuntime runtime = location?.ImportedRuntime;
        if (runtime == null || driver.ImportedBarSeatIndex >= runtime.Seats.Count)
        {
            return false;
        }

        ImportedBuildingSeat seat = runtime.Seats[driver.ImportedBarSeatIndex];
        if (seat?.SeatMarker == null)
        {
            return false;
        }

        driver.DriverObject.transform.position = GetImportedServiceSeatWorldPosition(seat);
        FaceImportedServiceSeat(driver, seat);
        ApplyDriverSittingPose(driver);
        return true;
    }

    private void RequestImportedBuildingDoorOpen(LocationData service)
    {
        ImportedBuildingRuntime runtime = service?.ImportedRuntime;
        if (runtime == null)
        {
            return;
        }

        runtime.DoorTargetOpenAmount = 1f;
        runtime.DoorHoldTimer = Mathf.Max(runtime.DoorHoldTimer, ImportedBuildingDoorHoldDuration);
    }

    private bool TryGetImportedServiceDoorWorldPosition(LocationData service, bool preferEnter, out Vector3 position)
    {
        position = Vector3.zero;
        ImportedBuildingRuntime runtime = service?.ImportedRuntime;
        if (runtime == null)
        {
            return false;
        }

        Transform marker = preferEnter
            ? runtime.DoorEnterMarker ?? runtime.VisitorStandMarker ?? runtime.DoorInsideMarker
            : runtime.DoorInsideMarker ?? runtime.DoorEnterMarker ?? runtime.VisitorStandMarker;
        if (marker == null)
        {
            return false;
        }

        position = marker.position;
        position.y = SampleTerrainHeight(position.x, position.z);
        return true;
    }

    private static Vector3 GetImportedServiceSeatWorldPosition(ImportedBuildingSeat seat)
    {
        return seat.HasSeatWorldPosition ? seat.SeatWorldPosition : seat.SeatMarker.position;
    }

    private static void FaceImportedServiceSeat(DriverAgent driver, ImportedBuildingSeat seat)
    {
        if (driver?.DriverObject == null || seat?.LookAtMarker == null)
        {
            return;
        }

        Vector3 direction = seat.LookAtMarker.position - driver.DriverObject.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            driver.DriverObject.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private struct ImportedBuildingSeatAnchor
    {
        public Transform Marker;
        public Vector3 WorldPosition;
        public bool HasWorldPosition;
    }

    private static void RegisterImportedBuildingDoor(ImportedBuildingRuntime runtime, Transform doorTransform, float openAngle)
    {
        if (runtime == null || doorTransform == null)
        {
            return;
        }

        for (int i = 0; i < runtime.Doors.Count; i++)
        {
            if (runtime.Doors[i]?.DoorTransform == doorTransform)
            {
                return;
            }
        }

        Quaternion closed = doorTransform.localRotation;
        ImportedBuildingDoor door = new()
        {
            DoorTransform = doorTransform,
            ClosedLocalRotation = closed,
            OpenLocalRotation = closed * Quaternion.Euler(0f, openAngle, 0f)
        };
        runtime.Doors.Add(door);

        if (runtime.DoorTransform == null)
        {
            runtime.DoorTransform = doorTransform;
            runtime.DoorClosedLocalRotation = door.ClosedLocalRotation;
            runtime.DoorOpenLocalRotation = door.OpenLocalRotation;
        }
    }

    private static List<ImportedBuildingSeatAnchor> FindImportedServiceSeatAnchors(Transform modelRoot, LocationType type)
    {
        List<ImportedBuildingSeatAnchor> anchors = new();
        if (modelRoot == null)
        {
            return anchors;
        }

        if (type == LocationType.GamblingHall)
        {
            AddImportedMarkerSeatAnchors(modelRoot, anchors, "P_Bench_Sit_");
            AddImportedMarkerSeatAnchors(modelRoot, anchors, "P_TableSeat_");
            AddImportedMarkerSeatAnchors(modelRoot, anchors, "P_SlotMachine_");
            if (anchors.Count > 0)
            {
                return anchors;
            }
        }

        List<Transform> visibleSeatMeshes = new();
        Transform[] transforms = modelRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (IsImportedVisibleSeatMesh(current))
            {
                visibleSeatMeshes.Add(current);
            }
        }

        visibleSeatMeshes.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        for (int i = 0; i < visibleSeatMeshes.Count; i++)
        {
            Transform seatMesh = visibleSeatMeshes[i];
            anchors.Add(new ImportedBuildingSeatAnchor
            {
                Marker = seatMesh,
                WorldPosition = ResolveImportedSeatMeshPosition(seatMesh),
                HasWorldPosition = true
            });
        }

        if (anchors.Count > 0)
        {
            return anchors;
        }

        List<Transform> seatMarkers = FindImportedTransformsByPrefix(modelRoot, "P_TableSeat_");
        seatMarkers.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        for (int i = 0; i < seatMarkers.Count; i++)
        {
            Transform marker = seatMarkers[i];
            anchors.Add(new ImportedBuildingSeatAnchor
            {
                Marker = marker,
                WorldPosition = marker.position,
                HasWorldPosition = false
            });
        }

        return anchors;
    }

    private static void AddImportedMarkerSeatAnchors(Transform modelRoot, List<ImportedBuildingSeatAnchor> anchors, string prefix)
    {
        List<Transform> markers = FindImportedTransformsByPrefix(modelRoot, prefix);
        markers.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        for (int i = 0; i < markers.Count; i++)
        {
            Transform marker = markers[i];
            anchors.Add(new ImportedBuildingSeatAnchor
            {
                Marker = marker,
                WorldPosition = marker.position,
                HasWorldPosition = false
            });
        }
    }

    private static bool IsImportedVisibleSeatMesh(Transform transform)
    {
        if (transform == null || string.IsNullOrEmpty(transform.name))
        {
            return false;
        }

        string name = transform.name;
        bool hasSeatName = name.IndexOf("Seat", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool isSeatLike = hasSeatName ||
            name.IndexOf("Stool", System.StringComparison.OrdinalIgnoreCase) >= 0;
        return isSeatLike &&
            (name.IndexOf("Stool", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             name.IndexOf("Chair", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             name.IndexOf("Bench", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static Vector3 ResolveImportedSeatMeshPosition(Transform seatMesh)
    {
        if (TryGetImportedWorldRendererBounds(seatMesh, out Bounds bounds))
        {
            return bounds.center;
        }

        return seatMesh.position;
    }

    private static bool TryGetImportedWorldRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static Transform FindImportedTransform(Transform root, string exactName)
    {
        if (root == null || string.IsNullOrEmpty(exactName))
        {
            return null;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current != null && string.Equals(current.name, exactName, System.StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }
        }

        return null;
    }

    private static List<Transform> FindImportedTransformsByPrefix(Transform root, string prefix)
    {
        List<Transform> matches = new();
        if (root == null || string.IsNullOrEmpty(prefix))
        {
            return matches;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current != null && current.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(current);
            }
        }

        return matches;
    }

    private static Transform FindNearestImportedMarker(Vector3 origin, List<Transform> markers)
    {
        Transform nearest = null;
        float bestSqrDistance = float.PositiveInfinity;
        for (int i = 0; i < markers.Count; i++)
        {
            Transform marker = markers[i];
            if (marker == null)
            {
                continue;
            }

            float sqrDistance = (marker.position - origin).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                nearest = marker;
            }
        }

        return nearest;
    }
}
