using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float ImportedBuildingDoorOpenAngle = 78f;
    private const float ImportedBuildingDoorSpeed = 3.8f;
    private const float ImportedBuildingDoorHoldDuration = 1.2f;
    private const float ImportedVisibleSeatRootLift = 0.18f;
    private const float ImportedSeatDuplicateRadius = 0.24f;

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

        RegisterImportedBuildingDoor(runtime, modelRoot, FindImportedTransform(modelRoot, "Door"), ImportedBuildingDoorOpenAngle);
        RegisterImportedBuildingDoor(runtime, modelRoot, FindImportedTransform(modelRoot, "DoubleDoor_Left"), ImportedBuildingDoorOpenAngle);
        RegisterImportedBuildingDoor(runtime, modelRoot, FindImportedTransform(modelRoot, "DoubleDoor_Right"), -ImportedBuildingDoorOpenAngle);

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
            Transform animatedDoorTransform = door?.HingeTransform ?? door?.DoorTransform;
            if (animatedDoorTransform == null)
            {
                continue;
            }

            animatedDoorTransform.localRotation = Quaternion.Slerp(
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
        if (driver.ImportedBarSeatLocationInstanceId > 0)
        {
            ReleaseImportedBarSeat(driver);
        }

        PruneImportedServiceSeatReservations(service, runtime);
        for (int i = 0; i < runtime.Seats.Count; i++)
        {
            ImportedBuildingSeat seat = runtime.Seats[i];
            if (!CanReserveImportedServiceSeat(service, runtime, seat, i))
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

    private void PruneImportedServiceSeatReservations(LocationData service, ImportedBuildingRuntime runtime)
    {
        if (service == null || runtime == null)
        {
            return;
        }

        for (int i = 0; i < runtime.Seats.Count; i++)
        {
            ImportedBuildingSeat seat = runtime.Seats[i];
            if (seat?.OccupantDriverId <= 0)
            {
                continue;
            }

            DriverAgent occupant = GetDriverAgentById(seat.OccupantDriverId);
            if (!IsDriverHoldingImportedServiceSeat(occupant, service.InstanceId, i))
            {
                seat.OccupantDriverId = 0;
            }
        }
    }

    private bool CanReserveImportedServiceSeat(LocationData service, ImportedBuildingRuntime runtime, ImportedBuildingSeat seat, int seatIndex)
    {
        if (service == null || runtime == null || seat?.SeatMarker == null)
        {
            return false;
        }

        Vector3 seatPosition = GetImportedServiceSeatWorldPosition(seat);
        for (int i = 0; i < runtime.Seats.Count; i++)
        {
            ImportedBuildingSeat other = runtime.Seats[i];
            if (other == null || other.OccupantDriverId <= 0)
            {
                continue;
            }

            DriverAgent occupant = GetDriverAgentById(other.OccupantDriverId);
            if (!IsDriverHoldingImportedServiceSeat(occupant, service.InstanceId, i))
            {
                other.OccupantDriverId = 0;
                continue;
            }

            if (i == seatIndex || AreImportedSeatPositionsOverlapping(seatPosition, GetImportedServiceSeatWorldPosition(other)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDriverHoldingImportedServiceSeat(DriverAgent driver, int locationInstanceId, int seatIndex)
    {
        return driver != null &&
            driver.ImportedBarSeatLocationInstanceId == locationInstanceId &&
            driver.ImportedBarSeatIndex == seatIndex &&
            (driver.WalkPhase == DriverRescuePhase.IdleAtBar ||
             driver.WalkPhase == DriverRescuePhase.IdleAtGamblingHall ||
             (driver.IsInsideBuilding && driver.InsideBuildingInstanceId == locationInstanceId));
    }

    private static bool AreImportedSeatPositionsOverlapping(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz <= ImportedSeatDuplicateRadius * ImportedSeatDuplicateRadius;
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

    private static void RegisterImportedBuildingDoor(ImportedBuildingRuntime runtime, Transform modelRoot, Transform doorTransform, float openAngle)
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

        Transform knobTransform = FindImportedDoorKnob(modelRoot, doorTransform);
        Transform hingeTransform = CreateImportedDoorHingePivot(modelRoot, doorTransform, knobTransform, openAngle);
        Transform animatedTransform = hingeTransform != null ? hingeTransform : doorTransform;
        Quaternion closed = animatedTransform.localRotation;
        ImportedBuildingDoor door = new()
        {
            DoorTransform = doorTransform,
            HingeTransform = hingeTransform,
            ClosedLocalRotation = closed,
            OpenLocalRotation = closed * Quaternion.Euler(0f, openAngle, 0f)
        };
        runtime.Doors.Add(door);

        if (runtime.DoorTransform == null)
        {
            runtime.DoorTransform = animatedTransform;
            runtime.DoorClosedLocalRotation = door.ClosedLocalRotation;
            runtime.DoorOpenLocalRotation = door.OpenLocalRotation;
        }
    }

    private static Transform CreateImportedDoorHingePivot(Transform modelRoot, Transform doorTransform, Transform knobTransform, float openAngle)
    {
        if (doorTransform == null ||
            doorTransform.parent == null ||
            !TryGetImportedWorldRendererBounds(doorTransform, out Bounds bounds))
        {
            return null;
        }

        Vector3 hingePosition = ResolveImportedDoorHingeWorldPosition(modelRoot, doorTransform, knobTransform, bounds, openAngle);
        Transform originalParent = doorTransform.parent;
        int siblingIndex = doorTransform.GetSiblingIndex();
        GameObject hingeObject = new($"{doorTransform.name}_HingePivot");
        Transform hingeTransform = hingeObject.transform;
        hingeTransform.SetParent(originalParent, false);
        hingeTransform.SetSiblingIndex(siblingIndex);
        hingeTransform.position = hingePosition;
        hingeTransform.rotation = doorTransform.rotation;
        hingeTransform.localScale = Vector3.one;

        doorTransform.SetParent(hingeTransform, true);
        if (knobTransform != null &&
            knobTransform != doorTransform &&
            !knobTransform.IsChildOf(doorTransform) &&
            knobTransform.parent == originalParent)
        {
            knobTransform.SetParent(hingeTransform, true);
        }

        return hingeTransform;
    }

    private static Vector3 ResolveImportedDoorHingeWorldPosition(
        Transform modelRoot,
        Transform doorTransform,
        Transform knobTransform,
        Bounds bounds,
        float openAngle)
    {
        Vector3 widthAxis = ResolveImportedDoorWidthAxis(doorTransform, bounds);
        GetBoundsProjection(bounds, widthAxis, out float minProjection, out float maxProjection);
        float centerProjection = Vector3.Dot(bounds.center, widthAxis);
        float hingeProjection;

        if (knobTransform != null)
        {
            float knobProjection = Vector3.Dot(knobTransform.position, widthAxis);
            hingeProjection = knobProjection >= centerProjection ? minProjection : maxProjection;
        }
        else if (doorTransform.name.IndexOf("DoubleDoor", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                 modelRoot != null)
        {
            float rootProjection = Vector3.Dot(modelRoot.position, widthAxis);
            hingeProjection = centerProjection >= rootProjection ? maxProjection : minProjection;
        }
        else
        {
            hingeProjection = openAngle >= 0f ? minProjection : maxProjection;
        }

        Vector3 hingePosition = bounds.center + widthAxis * (hingeProjection - centerProjection);
        hingePosition.y = bounds.center.y;
        return hingePosition;
    }

    private static Vector3 ResolveImportedDoorWidthAxis(Transform doorTransform, Bounds bounds)
    {
        Vector3 right = doorTransform != null ? doorTransform.right : Vector3.right;
        Vector3 forward = doorTransform != null ? doorTransform.forward : Vector3.forward;
        right.y = 0f;
        forward.y = 0f;

        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.right;
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        right.Normalize();
        forward.Normalize();
        float rightSpan = GetBoundsProjectionSpan(bounds, right);
        float forwardSpan = GetBoundsProjectionSpan(bounds, forward);
        return rightSpan >= forwardSpan ? right : forward;
    }

    private static Transform FindImportedDoorKnob(Transform modelRoot, Transform doorTransform)
    {
        if (modelRoot == null || doorTransform == null)
        {
            return null;
        }

        Transform[] transforms = modelRoot.GetComponentsInChildren<Transform>(true);
        Transform best = null;
        float bestDistanceSqr = float.PositiveInfinity;
        Vector3 doorPosition = doorTransform.position;
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null || current == doorTransform || current.IsChildOf(doorTransform))
            {
                continue;
            }

            string name = current.name;
            if (name.IndexOf("Knob", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("Handle", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            float distanceSqr = (current.position - doorPosition).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                best = current;
                bestDistanceSqr = distanceSqr;
            }
        }

        return best;
    }

    private static float GetBoundsProjectionSpan(Bounds bounds, Vector3 axis)
    {
        GetBoundsProjection(bounds, axis, out float min, out float max);
        return max - min;
    }

    private static void GetBoundsProjection(Bounds bounds, Vector3 axis, out float min, out float max)
    {
        min = float.PositiveInfinity;
        max = float.NegativeInfinity;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    float projection = Vector3.Dot(corner, axis);
                    min = Mathf.Min(min, projection);
                    max = Mathf.Max(max, projection);
                }
            }
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
            Vector3 worldPosition = ResolveImportedSeatMeshPosition(seatMesh);
            if (HasNearbyImportedSeatAnchor(anchors, worldPosition))
            {
                continue;
            }

            anchors.Add(new ImportedBuildingSeatAnchor
            {
                Marker = seatMesh,
                WorldPosition = worldPosition,
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
            if (HasNearbyImportedSeatAnchor(anchors, marker.position))
            {
                continue;
            }

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
            if (HasNearbyImportedSeatAnchor(anchors, marker.position))
            {
                continue;
            }

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
        if (name.IndexOf("Leg", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Back", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Post", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Support", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        bool hasSeatName = name.IndexOf("Seat", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool isNamedSeatFurniture =
            name.IndexOf("Stool", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Chair", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Bench", System.StringComparison.OrdinalIgnoreCase) >= 0;
        return isNamedSeatFurniture &&
            (hasSeatName || name.IndexOf("Stool", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool HasNearbyImportedSeatAnchor(List<ImportedBuildingSeatAnchor> anchors, Vector3 worldPosition)
    {
        for (int i = 0; i < anchors.Count; i++)
        {
            if (AreImportedSeatPositionsOverlapping(anchors[i].WorldPosition, worldPosition))
            {
                return true;
            }
        }

        return false;
    }

    private static Vector3 ResolveImportedSeatMeshPosition(Transform seatMesh)
    {
        if (TryGetImportedWorldRendererBounds(seatMesh, out Bounds bounds))
        {
            Vector3 position = bounds.center;
            position.y = bounds.max.y + ImportedVisibleSeatRootLift;
            return position;
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
