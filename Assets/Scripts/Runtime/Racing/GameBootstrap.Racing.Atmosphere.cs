using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private void PopulateRacingAtmosphere()
    {
        if (racingSceneRoot == null) return;

        // Track bounding center
        Vector3 center = Vector3.zero;
        foreach (var seg in raceSegments) center += seg.Center;
        center /= raceSegments.Count;
        center.y = 0f;

        // в”Ђв”Ђ Post-processing on racing camera в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        SetupRacingPostProcessing();

        // в”Ђв”Ђ Birds on tree tops в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        int birdCount = Mathf.Min(racingTreeObstacles.Count, 6);
        if (birdCount > 0)
        {
            int step = Mathf.Max(1, racingTreeObstacles.Count / birdCount);
            for (int i = 0; i < birdCount; i++)
            {
                RaceObstacleData tree = racingTreeObstacles[Mathf.Min(i * step, racingTreeObstacles.Count - 1)];
                float treeScale = tree.Root != null ? tree.Root.localScale.x : 3f;
                Vector3 perch = new Vector3(tree.PoleXZ.x,
                                            (tree.Root != null ? tree.Root.position.y : 0f) + treeScale * 2.6f,
                                            tree.PoleXZ.y);
                CreateRacingBird(perch);
            }
        }

        // в”Ђв”Ђ Bees near flower patches в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        int beeCount = Mathf.Min(racingFlowerPoints.Count, 10);
        if (beeCount > 0)
        {
            int step = Mathf.Max(1, racingFlowerPoints.Count / beeCount);
            for (int i = 0; i < beeCount; i++)
                CreateRacingBee(racingFlowerPoints[Mathf.Min(i * step, racingFlowerPoints.Count - 1)]);
        }

        // в”Ђв”Ђ Moths near lanterns в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        int mothCount = Mathf.Min(racingLanterns.Count, 10);
        if (mothCount > 0)
        {
            int step = Mathf.Max(1, racingLanterns.Count / mothCount);
            for (int i = 0; i < mothCount; i++)
            {
                RaceObstacleData lan = racingLanterns[Mathf.Min(i * step, racingLanterns.Count - 1)];
                // Lamp head is at local Y в‰€ 1.05, world scale 3 в†’ ~3.2 m above root
                Vector3 lampPos = lan.Root != null
                    ? lan.Root.position + Vector3.up * 3.2f
                    : new Vector3(lan.PoleXZ.x, 3.5f, lan.PoleXZ.y);
                CreateRacingMothSwarm(lampPos);
            }
        }

        // в”Ђв”Ђ Ambient dust motes в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        for (int i = 0; i < 14; i++)
            CreateRacingDustMote(center, i);

        // в”Ђв”Ђ Boulders near mountains в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        foreach (var bump in terrainBumps)
        {
            if (bump.Height < 9f) continue;
            Vector3 clusterBase = new Vector3(bump.Center.x,
                                              racingGroundY + SampleTerrainY(bump.Center.x, bump.Center.y) * 0.35f,
                                              bump.Center.y);
            CreateRacingBoulderCluster(clusterBase, bump.Radius * 0.28f);
        }

        // в”Ђв”Ђ Small ponds в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        int pondsPlaced = 0;
        for (int attempt = 0; attempt < 300 && pondsPlaced < 4; attempt++)
        {
            float rx = center.x + Random.Range(-220f, 220f);
            float rz = center.z + Random.Range(-220f, 220f);
            if (IsPositionOnRaceRoad(new Vector3(rx, 0f, rz), 14f)) continue;
            float wy = racingGroundY + SampleTerrainY(rx, rz) - 0.08f;
            CreateRacingPond(new Vector3(rx, wy, rz));
            pondsPlaced++;
        }
    }

    private void SetupRacingPostProcessing()
    {
        if (racingCamera == null || racingSceneRoot == null) return;

        UniversalAdditionalCameraData camData = racingCamera.GetUniversalAdditionalCameraData();
        if (camData != null)
        {
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.Medium;
        }

        GameObject volObj = new("RacingPostProcessVolume");
        volObj.transform.SetParent(racingSceneRoot.transform, false);
        Volume vol = volObj.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 90f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        vol.sharedProfile = profile;

        ColorAdjustments ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(0.05f);
        ca.contrast.Override(8f);
        ca.saturation.Override(14f);
        ca.colorFilter.Override(new Color(1f, 0.98f, 0.92f, 1f));

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.08f);
        bloom.scatter.Override(0.48f);
        bloom.tint.Override(new Color(1f, 0.96f, 0.88f, 1f));
    }

    private void CreateRacingBird(Vector3 perchPos)
    {
        GameObject root = new("RacingBird");
        root.transform.SetParent(racingSceneRoot.transform, false);
        root.transform.position = perchPos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.transform.SetParent(root.transform, false);
        body.transform.localScale    = new Vector3(0.12f, 0.09f, 0.18f);
        ApplyColor(body, new Color(0.22f, 0.20f, 0.18f));
        ConfigureStaticVisual(body);
        if (body.TryGetComponent(out Collider bc)) bc.enabled = false;

        GameObject lw = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lw.transform.SetParent(root.transform, false);
        lw.transform.localPosition = new Vector3(-0.06f, 0.01f, 0f);
        lw.transform.localScale    = new Vector3(0.12f, 0.02f, 0.18f);
        ApplyColor(lw, new Color(0.28f, 0.26f, 0.24f));
        ConfigureStaticVisual(lw);
        if (lw.TryGetComponent(out Collider lc)) lc.enabled = false;

        GameObject rw = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rw.transform.SetParent(root.transform, false);
        rw.transform.localPosition = new Vector3(0.06f, 0.01f, 0f);
        rw.transform.localScale    = new Vector3(0.12f, 0.02f, 0.18f);
        ApplyColor(rw, new Color(0.28f, 0.26f, 0.24f));
        ConfigureStaticVisual(rw);
        if (rw.TryGetComponent(out Collider rc)) rc.enabled = false;

        racingBirds.Add(new RacingBirdData
        {
            Root      = root.transform,
            LeftWing  = lw.transform,
            RightWing = rw.transform,
            PerchPos  = perchPos,
            BobPhase  = Random.Range(0f, 10f),
            WingPhase = Random.Range(0f, 10f),
        });
    }

    private void CreateRacingBee(Vector3 flowerPos)
    {
        GameObject root = new("RacingBee");
        root.transform.SetParent(racingSceneRoot.transform, false);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float r     = Random.Range(0.08f, 0.18f);
        root.transform.position = flowerPos + new Vector3(Mathf.Cos(angle) * r, 0.24f, Mathf.Sin(angle) * r);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(root.transform, false);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale    = new Vector3(0.035f, 0.055f, 0.035f);
        ApplyColor(body, new Color(0.96f, 0.78f, 0.12f));
        ConfigureStaticVisual(body);
        if (body.TryGetComponent(out Collider bColl)) bColl.enabled = false;

        GameObject lw = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lw.transform.SetParent(root.transform, false);
        lw.transform.localPosition = new Vector3(-0.025f, 0.02f, 0f);
        lw.transform.localScale    = new Vector3(0.055f, 0.01f, 0.035f);
        ApplyColor(lw, new Color(0.92f, 0.96f, 1f));
        ConfigureStaticVisual(lw);
        if (lw.TryGetComponent(out Collider lwC)) lwC.enabled = false;

        GameObject rw = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rw.transform.SetParent(root.transform, false);
        rw.transform.localPosition = new Vector3(0.025f, 0.02f, 0f);
        rw.transform.localScale    = new Vector3(0.055f, 0.01f, 0.035f);
        ApplyColor(rw, new Color(0.92f, 0.96f, 1f));
        ConfigureStaticVisual(rw);
        if (rw.TryGetComponent(out Collider rwC)) rwC.enabled = false;

        racingBees.Add(new RacingBeeData
        {
            Root          = root.transform,
            LeftWing      = lw.transform,
            RightWing     = rw.transform,
            FlowerPos     = flowerPos,
            OrbitAngle    = angle,
            OrbitRadius   = r,
            OrbitHeight   = Random.Range(0.18f, 0.28f),
            OrbitSpeed    = Random.Range(1.6f, 2.6f),
            BobAmplitude  = Random.Range(0.015f, 0.04f),
            BobSpeed      = Random.Range(2.2f, 3.6f),
            PhaseOffset   = Random.Range(0f, 10f),
        });
    }

    private void CreateRacingMothSwarm(Vector3 lanternPos)
    {
        GameObject root = new("RacingMothSwarm");
        root.transform.SetParent(racingSceneRoot.transform, false);
        root.transform.position = lanternPos;

        int count = Random.Range(4, 7);
        for (int i = 0; i < count; i++)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.name = $"Moth_{i + 1}";
            p.transform.SetParent(root.transform, false);
            p.transform.localScale = Vector3.one * Random.Range(0.026f, 0.042f);
            ApplyColor(p, new Color(0.9f, 0.87f, 0.65f));
            ConfigureStaticVisual(p);
            if (p.TryGetComponent(out Collider pC)) pC.enabled = false;
        }

        racingMoths.Add(new RacingMothData
        {
            Root         = root.transform,
            LanternPos   = lanternPos,
            OrbitRadius  = Random.Range(0.16f, 0.28f),
            OrbitHeight  = Random.Range(0.06f, 0.18f),
            OrbitAngle   = Random.Range(0f, Mathf.PI * 2f),
            OrbitSpeed   = Random.Range(0.9f, 1.6f),
            BobAmplitude = Random.Range(0.02f, 0.06f),
            BobSpeed     = Random.Range(1.8f, 3.4f),
            PhaseOffset  = Random.Range(0f, 10f),
        });
    }

    private void CreateRacingDustMote(Vector3 areaCenter, int index)
    {
        float a  = index / 14f * Mathf.PI * 2f;
        float rx = areaCenter.x + Mathf.Cos(a) * 14f + Random.Range(-10f, 10f);
        float rz = areaCenter.z + Mathf.Sin(a) * 14f + Random.Range(-10f, 10f);
        float wy = racingGroundY + SampleTerrainY(rx, rz) + Random.Range(0.8f, 2.4f);

        GameObject dust = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dust.name = $"RacingDust_{index + 1}";
        dust.transform.SetParent(racingSceneRoot.transform, false);
        dust.transform.position   = new Vector3(rx, wy, rz);
        dust.transform.localScale = Vector3.one * Random.Range(0.030f, 0.056f);
        ApplyUnlitColor(dust, new Color(0.96f, 0.94f, 0.86f));
        if (dust.TryGetComponent(out Collider dC)) dC.enabled = false;

        racingDustMotes.Add(new RacingDustData
        {
            Root         = dust.transform,
            AreaCenter   = new Vector3(rx, 0f, rz),
            HalfRangeX   = Random.Range(3f, 11f),
            HalfRangeZ   = Random.Range(3f, 11f),
            TravelOffset = Random.Range(0f, Mathf.PI * 2f),
            Speed        = Random.Range(0.05f, 0.14f),
            BobAmplitude = Random.Range(0.02f, 0.07f),
            BobPhase     = Random.Range(0f, 10f),
            BaseY        = wy,
        });
    }

    private void CreateRacingBoulderCluster(Vector3 clusterCenter, float radius)
    {
        int count = Random.Range(2, 5);
        for (int i = 0; i < count; i++)
        {
            float a  = Random.Range(0f, Mathf.PI * 2f);
            float d  = Random.Range(0f, radius);
            float wx = clusterCenter.x + Mathf.Cos(a) * d;
            float wz = clusterCenter.z + Mathf.Sin(a) * d;
            if (IsPositionOnRaceRoad(new Vector3(wx, 0f, wz), 8f)) continue;

            float scale   = Random.Range(0.7f, 2.4f);
            float groundY = racingGroundY + SampleTerrainY(wx, wz);

            GameObject boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boulder.name = "RacingBoulder";
            boulder.transform.SetParent(racingSceneRoot.transform, false);
            boulder.transform.position = new Vector3(wx, groundY + scale * 0.28f, wz);
            boulder.transform.rotation = Quaternion.Euler(
                Random.Range(0f, 25f), Random.Range(0f, 360f), Random.Range(0f, 25f));
            boulder.transform.localScale = new Vector3(
                scale * Random.Range(0.8f, 1.2f),
                scale * Random.Range(0.55f, 0.85f),
                scale * Random.Range(0.8f, 1.2f));

            Color rock = Color.Lerp(new Color(0.52f, 0.50f, 0.46f), new Color(0.40f, 0.38f, 0.34f), Random.value);
            ApplyColor(boulder, rock);
            ConfigureShadowVisual(boulder);
            if (boulder.TryGetComponent(out Collider bColl)) bColl.enabled = false;
        }
    }

    private void CreateRacingPond(Vector3 pos)
    {
        GameObject pond = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pond.name = "RacingPond";
        pond.transform.SetParent(racingSceneRoot.transform, false);
        pond.transform.position = pos;
        float rx = Random.Range(2.0f, 4.5f);
        float rz = Random.Range(1.6f, 3.8f);
        pond.transform.localScale = new Vector3(rx * 2f, 0.16f, rz * 2f);
        pond.transform.rotation   = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        ApplyColor(pond, new Color(0.26f, 0.50f, 0.72f));
        ConfigureStaticVisual(pond);
        if (pond.TryGetComponent(out Collider pC)) pC.enabled = false;
    }

    private void UpdateRacingAtmosphere(float dt)
    {
        float t = Time.unscaledTime;

        // в”Ђв”Ђ Birds (idle perch bob + wing twitch) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        for (int i = 0; i < racingBirds.Count; i++)
        {
            RacingBirdData b = racingBirds[i];
            if (b.Root == null) continue;
            float bob  = Mathf.Sin(t * 2.1f + b.BobPhase) * 0.012f;
            b.Root.position = b.PerchPos + new Vector3(0f, bob, 0f);
            float wingAng = Mathf.Sin(t * 3.4f + b.WingPhase) * 5f;
            if (b.LeftWing  != null) b.LeftWing.localRotation  = Quaternion.Euler(0f, 0f,  wingAng);
            if (b.RightWing != null) b.RightWing.localRotation = Quaternion.Euler(0f, 0f, -wingAng);
        }

        // в”Ђв”Ђ Bees (orbit flower + wing flutter) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        for (int i = racingBees.Count - 1; i >= 0; i--)
        {
            RacingBeeData b = racingBees[i];
            if (b.Root == null) { racingBees.RemoveAt(i); continue; }
            b.OrbitAngle += b.OrbitSpeed * dt;
            float bob  = Mathf.Sin(t * b.BobSpeed + b.PhaseOffset) * b.BobAmplitude;
            b.Root.position = b.FlowerPos + new Vector3(
                Mathf.Cos(b.OrbitAngle) * b.OrbitRadius,
                b.OrbitHeight + bob,
                Mathf.Sin(b.OrbitAngle) * b.OrbitRadius);
            float flap = Mathf.Sin(t * 28f) * 18f;
            if (b.LeftWing  != null) b.LeftWing.localRotation  = Quaternion.Euler( flap, 0f, 0f);
            if (b.RightWing != null) b.RightWing.localRotation = Quaternion.Euler(-flap, 0f, 0f);
            racingBees[i] = b;
        }

        // в”Ђв”Ђ Moths (orbit lantern) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        for (int i = racingMoths.Count - 1; i >= 0; i--)
        {
            RacingMothData m = racingMoths[i];
            if (m.Root == null) { racingMoths.RemoveAt(i); continue; }
            m.OrbitAngle += m.OrbitSpeed * dt;
            float bob = Mathf.Sin(t * m.BobSpeed + m.PhaseOffset) * m.BobAmplitude;
            m.Root.position = m.LanternPos + new Vector3(
                Mathf.Cos(m.OrbitAngle) * m.OrbitRadius,
                m.OrbitHeight + bob,
                Mathf.Sin(m.OrbitAngle) * m.OrbitRadius);
            racingMoths[i] = m;
        }

        // в”Ђв”Ђ Dust (slow lazy drift) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        for (int i = 0; i < racingDustMotes.Count; i++)
        {
            RacingDustData d = racingDustMotes[i];
            if (d.Root == null) continue;
            float phase = t * d.Speed + d.TravelOffset;
            float ox = Mathf.Cos(phase) * d.HalfRangeX;
            float oz = Mathf.Sin(phase * 0.73f) * d.HalfRangeZ;
            float oy = Mathf.Sin(t * 1.4f + d.BobPhase) * d.BobAmplitude;
            d.Root.position = new Vector3(d.AreaCenter.x + ox, d.BaseY + oy, d.AreaCenter.z + oz);
        }
    }

    private bool IsPositionOnRaceRoad(Vector3 pos, float margin)
    {
        Vector2 p = new Vector2(pos.x, pos.z);
        foreach (var seg in raceSegments)
        {
            Vector3 fwd   = seg.Rotation * Vector3.forward;
            Vector3 start = seg.Center - fwd * seg.Length * 0.5f;
            Vector3 end   = seg.Center + fwd * seg.Length * 0.5f;
            if (DistXZPointToSegment(p,
                    new Vector2(start.x, start.z),
                    new Vector2(end.x,   end.z)) < margin)
                return true;
        }
        foreach (var seg in raceExtensionSegments)
        {
            Vector3 fwd   = seg.Rotation * Vector3.forward;
            Vector3 start = seg.Center - fwd * seg.Length * 0.5f;
            Vector3 end   = seg.Center + fwd * seg.Length * 0.5f;
            if (DistXZPointToSegment(p,
                    new Vector2(start.x, start.z),
                    new Vector2(end.x,   end.z)) < margin)
                return true;
        }
        return false;
    }

    private static float DistXZPointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float sqLen = ab.sqrMagnitude;
        if (sqLen < 0.0001f) return (p - a).magnitude;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / sqLen);
        return (p - (a + ab * t)).magnitude;
    }
}
