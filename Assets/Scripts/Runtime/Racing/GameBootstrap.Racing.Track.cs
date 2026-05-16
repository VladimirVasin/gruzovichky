using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private void BuildTrackSampler()
    {
        racingSegCumLen = new float[raceSegments.Count];
        float cum = 0f;
        for (int i = 0; i < raceSegments.Count; i++)
        {
            racingSegCumLen[i] = cum;
            cum += raceSegments[i].Length;
        }
        racingTrackLen = cum;
    }

    // Returns road centreline position + segment rotation at distance t from start.
    // Extrapolates linearly before/after the track.
    private void GetTrackPoint(float t, out Vector3 pos, out Quaternion rot)
    {
        if (raceSegments.Count == 0) { pos = Vector3.zero; rot = Quaternion.identity; return; }

        int   segIdx = 0;
        float localT = 0f;

        if (t < 0f)
        {
            segIdx = 0;
            localT = t; // negative в†’ extrapolate behind start
        }
        else if (t >= racingTrackLen)
        {
            segIdx = raceSegments.Count - 1;
            localT = raceSegments[segIdx].Length + (t - racingTrackLen);
        }
        else
        {
            for (int i = raceSegments.Count - 1; i >= 0; i--)
            {
                if (racingSegCumLen[i] <= t)
                {
                    segIdx = i;
                    localT = t - racingSegCumLen[i];
                    break;
                }
            }
        }

        RaceSegment seg = raceSegments[segIdx];
        Vector3 fwd     = seg.Rotation * Vector3.forward;
        Vector3 segStart = seg.Center - fwd * seg.Length * 0.5f;
        pos = segStart + fwd * localT;
        float segFrac = seg.Length > 0.001f ? Mathf.Clamp01(localT / seg.Length) : 0f;
        pos.y = Mathf.Lerp(seg.StartY, seg.EndY, segFrac) + 0.35f;
        rot = seg.Rotation;
    }

    // Returns the terrain bump height at world XZ (add groundY for world Y).
    private float SampleTerrainY(float x, float z)
    {
        float h = 0f;
        foreach (var b in terrainBumps)
        {
            float dx = x - b.Center.x;
            float dz = z - b.Center.y;   // Center.y stores world Z (Vector2 convention)
            float denom = 2f * b.Radius * b.Radius;
            h += b.Height * Mathf.Exp(-(dx * dx + dz * dz) / denom);
        }
        return h;
    }

    // Returns terrain height suppressed to 0 near road segments (for ground mesh vertices).
    // Returns the world Y for a ground mesh vertex at (x,z).
    // Near road: blends toward the road segment's own height so ground follows the road.
    // Far from road: groundY + terrain bumps.
    private float SampleGroundMeshY(float x, float z, float groundY)
    {
        float minDist    = float.MaxValue;
        float nearRoadY  = groundY;
        Vector2 p = new Vector2(x, z);

        foreach (var seg in raceSegments)
        {
            Vector3 fwd   = seg.Rotation * Vector3.forward;
            Vector2 s2    = new Vector2(seg.Center.x - fwd.x * seg.Length * 0.5f,
                                        seg.Center.z - fwd.z * seg.Length * 0.5f);
            Vector2 e2    = new Vector2(seg.Center.x + fwd.x * seg.Length * 0.5f,
                                        seg.Center.z + fwd.z * seg.Length * 0.5f);
            float d = DistXZPointToSegment(p, s2, e2);
            if (d < minDist)
            {
                minDist = d;
                // Interpolated road surface Y at this XZ (slightly below road top)
                float t  = Mathf.Clamp01(Vector2.Dot(p - s2, (e2 - s2).normalized) / seg.Length);
                nearRoadY = Mathf.Lerp(seg.StartY, seg.EndY, t) - 0.35f;
            }
        }

        // mask: 0 = within 6 m of road centre в†’ use road Y; 1 = far away в†’ use terrain
        float mask      = Mathf.Clamp01((minDist - 6f) / 12f);
        float farY      = groundY + SampleTerrainY(x, z);
        return Mathf.Lerp(nearRoadY, farY, mask);
    }

    // Find the road surface Y at world XZ by projecting onto the nearest segment.
    private float SampleRaceRoadY(float wx, float wz)
    {
        float bestDistSq = float.MaxValue;
        float bestY      = 0.35f;

        foreach (RaceSegment seg in raceSegments)
        {
            Vector3 fwd    = seg.Rotation * Vector3.forward;
            float   startX = seg.Center.x - fwd.x * seg.Length * 0.5f;
            float   startZ = seg.Center.z - fwd.z * seg.Length * 0.5f;
            float   t      = Mathf.Clamp01(((wx - startX) * fwd.x + (wz - startZ) * fwd.z) / seg.Length);
            float   cx     = startX + fwd.x * seg.Length * t;
            float   cz     = startZ + fwd.z * seg.Length * t;
            float   dSq    = (wx - cx) * (wx - cx) + (wz - cz) * (wz - cz);

            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                bestY      = Mathf.Lerp(seg.StartY, seg.EndY, t) + 0.35f;
            }
        }

        return bestY;
    }

    private void SpawnRacingBuses()
    {
        racingBuses.Clear();
        float len = racingTrackLen;

        // Two oncoming (left lane, orange), two same-direction (right lane, blue)
        SpawnBus(len * 0.25f,  9f, -1, -1.25f, new Color(0.80f, 0.35f, 0.20f));
        SpawnBus(len * 0.65f, 11f, -1, -1.25f, new Color(0.80f, 0.35f, 0.20f));
        SpawnBus(len * 0.15f,  5f, +1, +1.25f, new Color(0.20f, 0.38f, 0.65f));
        SpawnBus(len * 0.55f, 12f, +1, +1.25f, new Color(0.20f, 0.38f, 0.65f));
    }

    private void SpawnBus(float t, float speed, int dir, float laneOffset, Color bodyColor)
    {
        RacingBusData bus = new()
        {
            T = t, Speed = speed, Direction = dir,
            LaneOffset = laneOffset, CollisionRadius = 0.90f
        };

        // Root GO вЂ” positioned every frame
        bus.Root = new GameObject("RacingBus");

        Color roofColor = Color.Lerp(bodyColor, Color.black, 0.25f);
        Color wheelColor = new Color(0.14f, 0.14f, 0.16f);

        // Body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(body.GetComponent<Collider>());
        body.name = "BusBody";
        body.transform.SetParent(bus.Root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        body.transform.localScale    = new Vector3(0.95f, 0.55f, 2.0f);
        ApplyColor(body, bodyColor);
        NoShadow(body);

        // Roof
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(roof.GetComponent<Collider>());
        roof.name = "BusRoof";
        roof.transform.SetParent(bus.Root.transform, false);
        roof.transform.localPosition = new Vector3(0f, 0.58f, 0f);
        roof.transform.localScale    = new Vector3(0.90f, 0.08f, 1.95f);
        ApplyColor(roof, roofColor);
        NoShadow(roof);

        // Wheels
        CreateBusWheel(bus.Root.transform, new Vector3(-0.52f, 0.10f,  0.65f), wheelColor);
        CreateBusWheel(bus.Root.transform, new Vector3(+0.52f, 0.10f,  0.65f), wheelColor);
        CreateBusWheel(bus.Root.transform, new Vector3(-0.52f, 0.10f, -0.65f), wheelColor);
        CreateBusWheel(bus.Root.transform, new Vector3(+0.52f, 0.10f, -0.65f), wheelColor);

        racingBuses.Add(bus);
    }

    private static void CreateBusWheel(Transform parent, Vector3 localPos, Color color)
    {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(w.GetComponent<Collider>());
        w.name = "BusWheel";
        w.transform.SetParent(parent, false);
        w.transform.localPosition = localPos;
        w.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        w.transform.localScale    = new Vector3(0.18f, 0.06f, 0.18f);
        ApplyColor(w, color);
        NoShadow(w);
    }

    private void UpdateRacingBuses(float dt)
    {
        if (racingBuses.Count == 0 || racingSegCumLen == null) return;

        float margin = 40f;
        float loop   = racingTrackLen + margin * 2f;

        for (int i = 0; i < racingBuses.Count; i++)
        {
            RacingBusData bus = racingBuses[i];

            // Advance along track
            bus.T += bus.Speed * bus.Direction * dt;

            // Wrap around
            if (bus.T > racingTrackLen + margin) bus.T -= loop;
            if (bus.T < -margin)                 bus.T += loop;

            // World position + rotation
            GetTrackPoint(bus.T, out Vector3 centre, out Quaternion segRot);
            Vector3 roadRight = segRot * Vector3.right;
            bus.Root.transform.position = centre + roadRight * bus.LaneOffset;
            bus.Root.transform.rotation = bus.Direction == -1
                ? segRot * Quaternion.Euler(0f, 180f, 0f)
                : segRot;

            // Collision with player truck
            Vector2 busXZ   = new(bus.Root.transform.position.x, bus.Root.transform.position.z);
            Vector2 truckXZ = new(racingTruckPos.x, racingTruckPos.z);
            Vector2 delta   = truckXZ - busXZ;
            float combined  = TruckCollisionRadius + bus.CollisionRadius;

            if (delta.sqrMagnitude < combined * combined)
            {
                float dist = delta.magnitude;
                Vector2 norm = dist > 0.001f ? delta / dist : Vector2.right;
                float pen = combined - dist;
                racingTruckPos.x += norm.x * pen;
                racingTruckPos.z += norm.y * pen;
                float vDotN = Vector2.Dot(racingVelocity, norm);
                if (vDotN < 0f)
                {
                    racingVelocity -= norm * vDotN;
                    racingVelocity *= (1f - CollisionEnergyLoss);
                }
                racingAngularVel += norm.x * CollisionAngularKick;
            }

            racingBuses[i] = bus;
        }
    }

    private void CleanupRacingBuses()
    {
        foreach (RacingBusData bus in racingBuses)
            if (bus.Root != null) Object.Destroy(bus.Root);
        racingBuses.Clear();
        racingSegCumLen = null;
        racingTrackLen  = 0f;
    }

    // в”Ђв”Ђ Lantern collision в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void UpdateLanternCollisions(float dt)
    {
        ProcessObstacleCollisions(racingLanterns, dt);
        ProcessObstacleCollisions(racingTreeObstacles, dt);
    }

    private void ProcessObstacleCollisions(List<RaceObstacleData> obstacles, float dt)
    {
        if (obstacles.Count == 0) return;

        Vector2 truckXZ = new Vector2(racingTruckPos.x, racingTruckPos.z);

        for (int i = 0; i < obstacles.Count; i++)
        {
            RaceObstacleData obs = obstacles[i];

            float combined = TruckCollisionRadius + obs.CollisionRadius;
            float dx       = truckXZ.x - obs.PoleXZ.x;
            float dz       = truckXZ.y - obs.PoleXZ.y;
            float distSq   = dx * dx + dz * dz;

            if (distSq < combined * combined)
            {
                float   dist = Mathf.Sqrt(distSq);
                Vector2 norm = dist > 0.001f
                    ? new Vector2(dx / dist, dz / dist)
                    : new Vector2(1f, 0f);
                float penetration = combined - dist;

                // Depenetrate
                racingTruckPos.x += norm.x * penetration;
                racingTruckPos.z += norm.y * penetration;

                // Velocity deflect
                float vDotN = Vector2.Dot(racingVelocity, norm);
                if (vDotN < 0f)
                {
                    racingVelocity -= norm * vDotN;
                    racingVelocity *= (1f - CollisionEnergyLoss);
                }

                // First contact: spin + tip
                if (!obs.IsTipped)
                {
                    float cross = racingVelocity.x * norm.y - racingVelocity.y * norm.x;
                    racingAngularVel += (cross >= 0f ? 1f : -1f) * CollisionAngularKick;

                    obs.IsTipped   = true;
                    obs.TiltTarget = LanternTiltTargetDeg;

                    Vector3 worldAxis = new Vector3(-norm.y, 0f, norm.x);
                    if (obs.Root != null)
                        obs.TiltAxisLocal = obs.Root.InverseTransformDirection(worldAxis);
                }
            }

            // Tipping animation
            if (!Mathf.Approximately(obs.TiltAngle, obs.TiltTarget))
            {
                obs.TiltAngle = Mathf.MoveTowards(
                    obs.TiltAngle, obs.TiltTarget, LanternTiltSpeed * dt);

                if (obs.Root != null)
                    obs.Root.localRotation = obs.OriginalLocalRot
                        * Quaternion.AngleAxis(obs.TiltAngle, obs.TiltAxisLocal);
            }

            obstacles[i] = obs;
        }
    }

    // в”Ђв”Ђ Track generation в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void GenerateRaceTrack()
    {
        raceSegments.Clear();

        racingSceneRoot = new GameObject("RacingScene");
        racingSceneRoot.transform.position = new Vector3(RaceTrackOffsetX, 0f, 0f);

        // Pre-generate joint heights (N+1 joints for N segments)
        float[] jointY = new float[RaceSegmentCount + 1];
        jointY[0] = 0f;
        float heightMomentum = 0f;
        for (int i = 1; i <= RaceSegmentCount; i++)
        {
            heightMomentum = Mathf.Lerp(heightMomentum, Random.Range(-3.0f, 3.0f), 0.45f);
            jointY[i] = Mathf.Clamp(jointY[i - 1] + heightMomentum, -4f, 7f);
        }

        Vector3 cursor    = new Vector3(RaceTrackOffsetX, 0f, 0f);
        float   direction = 0f; // degrees Y

        for (int i = 0; i < RaceSegmentCount; i++)
        {
            float segLen = Random.Range(14f, 28f);

            Quaternion rot = Quaternion.Euler(0f, direction, 0f);
            Vector3 fwd  = rot * Vector3.forward;
            float sy = jointY[i];
            float ey = jointY[i + 1];
            Vector3 segCenter = cursor + fwd * segLen * 0.5f;
            segCenter.y = (sy + ey) * 0.5f;

            RaceSegment seg = new()
            {
                Center   = segCenter,
                Rotation = rot,
                Length   = segLen,
                StartY   = sy,
                EndY     = ey,
            };
            raceSegments.Add(seg);

            // Road surface
            CreateRaceSegmentVisuals(seg);

            cursor += fwd * segLen;

            // Random turn for next segment (constrained to avoid impossible geometry)
            direction += Random.Range(-28f, 28f);
        }

        // Start marker (green)
        RaceSegment first = raceSegments[0];
        Vector3 startPos = first.Center - first.Rotation * Vector3.forward * first.Length * 0.45f;
        startPos.y = first.StartY + 0.14f;
        CreateRaceMarker(startPos, first.Rotation, new Color(0.18f, 0.82f, 0.28f));

        // Finish marker (yellow + light)
        RaceSegment last = raceSegments[raceSegments.Count - 1];
        raceFinishPos = last.Center + last.Rotation * Vector3.forward * last.Length * 0.45f;
        raceFinishPos.y = last.EndY + 0.35f;
        raceFinishFwd = last.Rotation * Vector3.forward;   // road direction вЂ” for strip detection
        CreateRaceMarker(raceFinishPos, last.Rotation, new Color(0.95f, 0.82f, 0.12f));

        // Finish light
        GameObject lightObj = new("FinishLight");
        lightObj.transform.SetParent(racingSceneRoot.transform, false);
        lightObj.transform.position = raceFinishPos + Vector3.up * 1.5f;
        Light fl = lightObj.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(1f, 0.62f, 0.28f);
        fl.intensity = 0.55f;
        fl.range = 7f;
        fl.shadows = LightShadows.None;

        // в”Ђв”Ђ Road extension beyond finish (decorative вЂ” truck drives into horizon) в”Ђв”Ђ
        raceExtensionSegments.Clear();
        float extStartY = last.EndY;
        Vector3 extCursor = raceFinishPos;
        extCursor.y = extStartY;
        Quaternion extRot = last.Rotation;
        Vector3 extFwd = extRot * Vector3.forward;
        for (int i = 0; i < 6; i++)
        {
            float extLen = 24f;
            Vector3 extCenter = extCursor + extFwd * extLen * 0.5f;
            extCenter.y = extStartY;
            RaceSegment ext = new() { Center = extCenter, Rotation = extRot, Length = extLen, StartY = extStartY, EndY = extStartY };
            CreateRaceSegmentVisuals(ext);
            raceExtensionSegments.Add(ext);
            extCursor += extFwd * extLen;
        }
    }

    // Compute a pitch-corrected rotation for a segment so road tiles slope with the terrain.
    private static Quaternion GetSegmentPitchedRot(RaceSegment seg)
    {
        Vector3 fwdFlat  = seg.Rotation * Vector3.forward;
        float   dy       = seg.EndY - seg.StartY;
        Vector3 fwdSloped = new Vector3(fwdFlat.x, dy / seg.Length, fwdFlat.z).normalized;
        return Quaternion.LookRotation(fwdSloped, Vector3.up);
    }

    private void CreateRaceSegmentVisuals(RaceSegment seg)
    {
        float w = 5.0f;
        Quaternion pitchedRot = GetSegmentPitchedRot(seg);
        Vector3 centre = seg.Center;

        // Road surface
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "RoadSeg";
        road.transform.SetParent(racingSceneRoot.transform, false);
        road.transform.position = centre + pitchedRot * new Vector3(0f, 0.06f, 0f);
        road.transform.rotation = pitchedRot;
        road.transform.localScale = new Vector3(w, 0.12f, seg.Length);
        ApplyColor(road, new Color(0.22f, 0.22f, 0.25f));
        ConfigureShadowVisual(road);

        // Left kerb
        CreateKerb(seg, pitchedRot, centre, -w * 0.5f - 0.14f);
        // Right kerb
        CreateKerb(seg, pitchedRot, centre, w * 0.5f + 0.14f);

        // Center dashed line (every other segment)
        if (Random.value > 0.5f)
        {
            GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dash.name = "CenterDash";
            dash.transform.SetParent(racingSceneRoot.transform, false);
            dash.transform.position = centre + pitchedRot * new Vector3(0f, 0.13f, 0f);
            dash.transform.rotation = pitchedRot;
            dash.transform.localScale = new Vector3(0.12f, 0.01f, seg.Length * 0.6f);
            ApplyColor(dash, new Color(0.92f, 0.88f, 0.56f));
            ConfigureShadowVisual(dash);
        }
    }

    private void CreateKerb(RaceSegment seg, Quaternion pitchedRot, Vector3 centre, float xOffset)
    {
        GameObject kerb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kerb.name = "Kerb";
        kerb.transform.SetParent(racingSceneRoot.transform, false);
        kerb.transform.position = centre + pitchedRot * new Vector3(xOffset, 0.09f, 0f);
        kerb.transform.rotation = pitchedRot;
        kerb.transform.localScale = new Vector3(0.28f, 0.18f, seg.Length);
        ApplyColor(kerb, new Color(0.72f, 0.72f, 0.72f));
        ConfigureShadowVisual(kerb);
    }

    private void CreateRaceMarker(Vector3 worldPos, Quaternion rot, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "RaceMarker";
        marker.transform.SetParent(racingSceneRoot.transform, false);
        marker.transform.position = worldPos; // caller supplies correct Y
        marker.transform.rotation = rot;
        marker.transform.localScale = new Vector3(5.2f, 0.06f, 1.1f);
        ApplyColor(marker, color);
        ConfigureShadowVisual(marker);
    }

    // в”Ђв”Ђ Racing truck в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

}
