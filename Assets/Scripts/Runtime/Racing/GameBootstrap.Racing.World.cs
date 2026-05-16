using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private void PopulateRacingWorld()
    {
        racingLanterns.Clear();
        racingTreeObstacles.Clear();
        racingFlowerPoints.Clear();

        // Bounding center of the whole track
        Vector3 center = Vector3.zero;
        foreach (var seg in raceSegments) center += seg.Center;
        center /= raceSegments.Count;
        center.y = 0f;

        // в”Ђв”Ђ Ground base level в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        float minSegY = float.MaxValue;
        foreach (var s in raceSegments)
        {
            if (s.StartY < minSegY) minSegY = s.StartY;
            if (s.EndY   < minSegY) minSegY = s.EndY;
        }
        float groundY = (minSegY < float.MaxValue ? minSegY : 0f) - 1.5f;
        racingGroundY = groundY;

        // в”Ђв”Ђ Terrain bumps (hills + mountains) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        terrainBumps.Clear();

        // Track bounding radius вЂ” used to place mountains well outside the track
        float trackBoundsRadius = 0f;
        foreach (var s in raceSegments)
        {
            float d = Vector2.Distance(new Vector2(center.x, center.z), new Vector2(s.Center.x, s.Center.z));
            if (d + s.Length * 0.5f > trackBoundsRadius) trackBoundsRadius = d + s.Length * 0.5f;
        }

        // Hills вЂ” moderate bumps anywhere in the world area
        for (int i = 0; i < 18; i++)
        {
            terrainBumps.Add(new TerrainBump
            {
                Center = new Vector2(center.x + Random.Range(-320f, 320f),
                                     center.z + Random.Range(-320f, 320f)),
                Radius = Random.Range(22f, 48f),
                Height = Random.Range(1.8f, 5.5f),
            });
        }

        // Mountains вЂ” large, tall, placed far from track
        int mountainsPlaced = 0;
        for (int attempt = 0; attempt < 300 && mountainsPlaced < 7; attempt++)
        {
            float angle  = Random.Range(0f, Mathf.PI * 2f);
            float dist   = trackBoundsRadius + Random.Range(90f, 230f);
            Vector2 cand = new Vector2(center.x + Mathf.Cos(angle) * dist,
                                       center.z + Mathf.Sin(angle) * dist);

            // Reject if too close to any segment centre
            bool tooClose = false;
            foreach (var s in raceSegments)
            {
                float dx = cand.x - s.Center.x;
                float dz = cand.y - s.Center.z;
                if (dx * dx + dz * dz < 95f * 95f) { tooClose = true; break; }
            }
            if (tooClose) continue;

            terrainBumps.Add(new TerrainBump
            {
                Center = cand,
                Radius = Random.Range(65f, 125f),
                Height = Random.Range(10f, 24f),
            });
            mountainsPlaced++;
        }

        // в”Ђв”Ђ Procedural ground mesh в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        const int   GridN     = 88;      // 88Г—88 quads = 89ВІ = 7921 verts (safely under 65535)
        const float WorldSize = 900f;
        float step    = WorldSize / GridN;
        float originX = center.x - WorldSize * 0.5f;
        float originZ = center.z - WorldSize * 0.5f;
        Vector3 rootOfs = racingSceneRoot.transform.position; // parent offset for local coords

        int vCount = (GridN + 1) * (GridN + 1);
        int tCount = GridN * GridN * 6;
        Vector3[] verts = new Vector3[vCount];
        int[]     tris  = new int[tCount];

        for (int j = 0; j <= GridN; j++)
        {
            for (int i = 0; i <= GridN; i++)
            {
                float wx = originX + i * step;
                float wz = originZ + j * step;
                float wy = SampleGroundMeshY(wx, wz, groundY);
                // Store in root-local space (root has no rotation/scale, only XZ translation)
                verts[j * (GridN + 1) + i] = new Vector3(wx - rootOfs.x, wy, wz - rootOfs.z);
            }
        }

        int t = 0;
        for (int j = 0; j < GridN; j++)
        {
            for (int i = 0; i < GridN; i++)
            {
                int bl = j * (GridN + 1) + i;
                int br = bl + 1;
                int tl = bl + (GridN + 1);
                int tr = tl + 1;
                tris[t++] = bl; tris[t++] = tl; tris[t++] = br;
                tris[t++] = br; tris[t++] = tl; tris[t++] = tr;
            }
        }

        Mesh groundMesh = new Mesh();
        groundMesh.name = "RacingGroundMesh";
        groundMesh.vertices  = verts;
        groundMesh.triangles = tris;
        groundMesh.RecalculateNormals();
        groundMesh.RecalculateBounds();

        GameObject ground = new GameObject("RacingGround");
        ground.transform.SetParent(racingSceneRoot.transform, false);
        ground.transform.localPosition = Vector3.zero;
        MeshFilter   mf = ground.AddComponent<MeshFilter>();
        MeshRenderer mr = ground.AddComponent<MeshRenderer>();
        mf.sharedMesh = groundMesh;
        Color groundColor = new Color(0.62f, 0.74f, 0.46f);
        Shader urpLit = ShaderRefs.Lit;
        Material groundMat = new Material(urpLit);
        groundMat.color = groundColor;
        if (groundMat.HasProperty("_BaseColor"))  groundMat.SetColor("_BaseColor", groundColor);
        if (groundMat.HasProperty("_Smoothness")) groundMat.SetFloat("_Smoothness", 0.14f);
        if (groundMat.HasProperty("_Metallic"))   groundMat.SetFloat("_Metallic", 0f);
        mr.sharedMaterial = groundMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        // в”Ђв”Ђ Trees, bushes, flowers в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        int placed = 0;
        for (int attempt = 0; attempt < 1200 && placed < 380; attempt++)
        {
            float rx = center.x + Random.Range(-280f, 280f);
            float rz = center.z + Random.Range(-280f, 280f);
            Vector3 pos = new Vector3(rx, groundY + SampleTerrainY(rx, rz), rz);

            if (IsPositionOnRaceRoad(pos, 4.5f)) continue;

            int seed = attempt * 7193;
            float roll = (seed % 100) / 100f;

            GameObject obj = new($"RaceVeg_{placed}");
            obj.transform.SetParent(racingSceneRoot.transform, false);
            obj.transform.position  = pos;
            obj.transform.rotation  = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            if (roll < 0.62f)
            {
                // Tree вЂ” much bigger
                float treeScale = Random.Range(2.8f, 4.2f);
                obj.transform.localScale = Vector3.one * treeScale;
                CreateTreeVariant(obj.transform, attempt % 3);

                // Register near-road trees as collideable obstacles
                if (IsPositionOnRaceRoad(pos, 10f)) // within 10 u of road centre
                {
                    float trunkRadius = 0.18f * treeScale; // trunk ~0.18 local at scale 1
                    racingTreeObstacles.Add(new RaceObstacleData
                    {
                        PoleXZ           = new Vector2(pos.x, pos.z),
                        Root             = obj.transform,
                        OriginalLocalRot = obj.transform.localRotation,
                        CollisionRadius  = Mathf.Clamp(trunkRadius, 0.35f, 0.70f),
                        TiltAngle        = 0f,
                        TiltTarget       = 0f,
                        TiltAxisLocal    = Vector3.right,
                        IsTipped         = false,
                    });
                }
            }
            else if (roll < 0.82f)
            {
                // Berry bush
                obj.transform.localScale = Vector3.one * Random.Range(1.8f, 2.6f);
                CreateRacingBush(obj.transform, attempt);
            }
            else
            {
                // Flower patch
                obj.transform.localScale = Vector3.one * Random.Range(1.6f, 2.2f);
                CreateRacingFlowers(obj.transform, attempt);
                racingFlowerPoints.Add(pos);
            }

            placed++;
        }

        // в”Ђв”Ђ Lanterns вЂ” one pair every segment, both sides в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        for (int i = 0; i < raceSegments.Count; i++)
        {
            RaceSegment seg = raceSegments[i];
            Vector3 fwd   = seg.Rotation * Vector3.forward;
            Vector3 right = seg.Rotation * Vector3.right;
            Vector3 segStart = seg.Center - fwd * seg.Length * 0.5f;
            segStart.y = seg.StartY;

            Quaternion lanternRot = seg.Rotation;
            CreateRacingLantern(segStart + right * 3.2f,  lanternRot);
            CreateRacingLantern(segStart - right * 3.2f,  lanternRot);
        }
    }

    private void CreateRacingLantern(Vector3 worldPos, Quaternion worldRot)
    {
        // worldPos.y already set to road height by caller

        GameObject root = new("RaceLantern");
        root.transform.SetParent(racingSceneRoot.transform, false);
        root.transform.position   = worldPos;
        root.transform.rotation   = worldRot;
        root.transform.localScale = Vector3.one * 3f;

        // Pole
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.transform.SetParent(root.transform, false);
        pole.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        pole.transform.localScale    = new Vector3(0.08f, 1.42f, 0.08f);
        ApplyColor(pole, new Color(0.22f, 0.23f, 0.27f));
        ConfigureShadowVisual(pole);

        // Arm
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.transform.SetParent(root.transform, false);
        arm.transform.localPosition = new Vector3(0.14f, 1.34f, 0f);
        arm.transform.localScale    = new Vector3(0.3f, 0.06f, 0.06f);
        ApplyColor(arm, new Color(0.22f, 0.23f, 0.27f));
        ConfigureShadowVisual(arm);

        // Lamp head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0.26f, 1.16f, 0f);
        head.transform.localScale    = new Vector3(0.16f, 0.22f, 0.16f);
        ApplyColor(head, new Color(0.3f, 0.28f, 0.2f));
        ConfigureShadowVisual(head);

        // Glow sphere
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.transform.SetParent(root.transform, false);
        glow.transform.localPosition = new Vector3(0.26f, 1.05f, 0f);
        glow.transform.localScale    = new Vector3(0.12f, 0.12f, 0.12f);
        ApplyColor(glow, new Color(0.26f, 0.22f, 0.18f));
        ConfigureStaticVisual(glow);

        // Light
        GameObject lightObj = new("LanternLight");
        lightObj.transform.SetParent(root.transform, false);
        lightObj.transform.localPosition = new Vector3(0.26f, 1.02f, 0f);
        Light l = lightObj.AddComponent<Light>();
        l.type      = LightType.Point;
        l.color     = new Color(1f, 0.66f, 0.34f);
        l.range     = 14f;
        l.intensity = 0f;
        l.shadows   = LightShadows.None;
        l.enabled   = false;

        racingWorldLights.Add(l);

        // Register lantern for manual collision
        racingLanterns.Add(new RaceObstacleData
        {
            PoleXZ           = new Vector2(worldPos.x, worldPos.z),
            Root             = root.transform,
            OriginalLocalRot = root.transform.localRotation,
            CollisionRadius  = LanternPoleRadius,
            TiltAngle        = 0f,
            TiltTarget       = 0f,
            TiltAxisLocal    = Vector3.right,
            IsTipped         = false,
        });
    }

    private static void CreateRacingBush(Transform parent, int seed)
    {
        Color leafA = new Color(0.16f, 0.42f, 0.2f);
        Color leafB = new Color(0.22f, 0.52f, 0.26f);
        Vector3[] pos = { new(-0.12f, 0.18f, -0.02f), new(0.14f, 0.22f, 0.04f), new(0.02f, 0.25f, -0.14f) };
        Vector3[] scl = { new(0.32f, 0.24f, 0.3f),    new(0.36f, 0.28f, 0.32f), new(0.28f, 0.22f, 0.26f) };
        for (int i = 0; i < 3; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.transform.SetParent(parent, false);
            c.transform.localPosition = pos[i];
            c.transform.localScale    = scl[i];
            ApplyColor(c, i % 2 == 0 ? leafB : leafA);
            ConfigureStaticVisual(c);
        }
    }

    private static void CreateRacingFlowers(Transform parent, int seed)
    {
        Color stemCol = new Color(0.2f, 0.5f, 0.24f);
        Color[] petals = { new(0.94f, 0.88f, 0.24f), new(0.96f, 0.62f, 0.22f), new(0.92f, 0.48f, 0.58f) };
        for (int i = 0; i < 5; i++)
        {
            float a = (i / 5f) * Mathf.PI * 2f + seed * 0.4f;
            float r = 0.06f + (i % 2) * 0.04f;

            // Stem
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(parent, false);
            stem.transform.localPosition = new Vector3(Mathf.Cos(a)*r, 0.09f, Mathf.Sin(a)*r);
            stem.transform.localScale    = new Vector3(0.025f, 0.09f, 0.025f);
            ApplyColor(stem, stemCol);
            ConfigureStaticVisual(stem);

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(parent, false);
            head.transform.localPosition = new Vector3(Mathf.Cos(a)*r, 0.19f, Mathf.Sin(a)*r);
            head.transform.localScale    = new Vector3(0.08f, 0.06f, 0.08f);
            ApplyColor(head, petals[i % petals.Length]);
            ConfigureStaticVisual(head);
        }
    }

    // Returns raw surface Y (no +0.35 truck-centre offset) вЂ” on-road or off-road.
    private float SampleSurfaceY(float x, float z)
    {
        if (IsPositionOnRaceRoad(new Vector3(x, 0f, z), 4.8f))
        {
            float bestDSq = float.MaxValue, bestY = 0f;
            foreach (var seg in raceSegments)
            {
                Vector3 fwd = seg.Rotation * Vector3.forward;
                float sx = seg.Center.x - fwd.x * seg.Length * 0.5f;
                float sz = seg.Center.z - fwd.z * seg.Length * 0.5f;
                float t  = Mathf.Clamp01(((x - sx) * fwd.x + (z - sz) * fwd.z) / seg.Length);
                float cx = sx + fwd.x * seg.Length * t;
                float cz = sz + fwd.z * seg.Length * t;
                float dSq = (x - cx) * (x - cx) + (z - cz) * (z - cz);
                if (dSq < bestDSq) { bestDSq = dSq; bestY = Mathf.Lerp(seg.StartY, seg.EndY, t); }
            }
            return bestY;
        }
        return SampleGroundMeshY(x, z, racingGroundY);
    }

    // в”Ђв”Ђ Racing atmosphere в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

}
