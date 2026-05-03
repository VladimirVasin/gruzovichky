using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private void SetupAmbientFrogs()
    {
        ambientFrogs.Clear();
        ambientFrogRoamCells.Clear();
        if (ambientFrogRoot != null) Destroy(ambientFrogRoot.gameObject);
        if (worldRoot == null || naturalBeachCells.Count == 0) return;

        ambientFrogRoot = new GameObject("AmbientFrogs").transform;
        ambientFrogRoot.SetParent(worldRoot, false);

        foreach (Vector2Int cell in naturalBeachCells)
            ambientFrogRoamCells.Add(cell);

        int count = Mathf.Min(AmbientFrogCount, ambientFrogRoamCells.Count);
        int step = Mathf.Max(1, ambientFrogRoamCells.Count / count);
        for (int i = 0; i < count; i++)
            CreateAmbientFrog(i * step % ambientFrogRoamCells.Count);
    }

    private void CreateAmbientFrog(int cellIndex)
    {
        if (ambientFrogRoot == null || ambientFrogRoamCells.Count == 0) return;

        Vector2Int cell = ambientFrogRoamCells[cellIndex];
        float groundY = SampleTerrainHeight(cell.x + 0.5f, cell.y + 0.5f);
        Vector3 position = new(cell.x + Random.Range(0.2f, 0.8f), groundY, cell.y + Random.Range(0.2f, 0.8f));

        GameObject frogRoot = new($"AmbientFrog_{ambientFrogs.Count + 1}");
        frogRoot.transform.SetParent(ambientFrogRoot, false);
        frogRoot.transform.position = position;

        Color bodyColor = new(0.28f, 0.46f, 0.22f);
        Color bellyColor = new(0.52f, 0.62f, 0.38f);

        // Body — flattened capsule
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(frogRoot.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.075f, 0f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.16f, 0.09f, 0.13f);
        ApplyColor(body, bodyColor);
        ConfigureStaticVisual(body);
        if (body.TryGetComponent(out Collider bc)) bc.enabled = false;

        // Belly stripe
        GameObject belly = GameObject.CreatePrimitive(PrimitiveType.Cube);
        belly.transform.SetParent(body.transform, false);
        belly.transform.localPosition = new Vector3(0f, 0f, 0.55f);
        belly.transform.localScale = new Vector3(0.7f, 0.5f, 0.45f);
        ApplyColor(belly, bellyColor);
        ConfigureStaticVisual(belly);
        if (belly.TryGetComponent(out Collider blc)) blc.enabled = false;

        // Head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(frogRoot.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.06f, 0.10f);
        head.transform.localScale = new Vector3(0.13f, 0.08f, 0.10f);
        ApplyColor(head, bodyColor);
        ConfigureStaticVisual(head);
        if (head.TryGetComponent(out Collider hc)) hc.enabled = false;

        // Eyes (two tiny spheres on head)
        foreach (float side in new[] { -0.35f, 0.35f })
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.transform.SetParent(head.transform, false);
            eye.transform.localPosition = new Vector3(side, 0.45f, 0.15f);
            eye.transform.localScale = Vector3.one * 0.28f;
            ApplyColor(eye, new Color(0.08f, 0.22f, 0.06f));
            ConfigureStaticVisual(eye);
            if (eye.TryGetComponent(out Collider ec)) ec.enabled = false;
        }

        float yaw = Random.Range(0f, 360f);
        frogRoot.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        ambientFrogs.Add(new AmbientFrogData
        {
            RootTransform    = frogRoot.transform,
            BodyTransform    = body.transform,
            HeadTransform    = head.transform,
            CurrentPosition  = position,
            StartPosition    = position,
            TargetPosition   = position,
            CurrentCellIndex = cellIndex,
            StateTimer       = Random.Range(1f, 3f),
            AnimPhase        = Random.Range(0f, 10f),
            Yaw              = yaw,
            State            = AmbientFrogState.Sitting,
        });
    }

    private void UpdateAmbientFrogs()
    {
        if (ambientFrogs.Count == 0) return;

        bool active = AreAmbientFrogsActive();
        float dt   = Time.deltaTime * gameSpeedMultiplier;
        float time = Time.time;

        for (int i = ambientFrogs.Count - 1; i >= 0; i--)
        {
            AmbientFrogData frog = ambientFrogs[i];
            if (frog?.RootTransform == null) { ambientFrogs.RemoveAt(i); continue; }

            switch (frog.State)
            {
                case AmbientFrogState.Sitting:
                    frog.StateTimer -= dt;

                    // Gentle breathe — body slightly scales Y
                    if (frog.BodyTransform != null)
                    {
                        float breathe = 1f + Mathf.Sin(time * 1.2f + frog.AnimPhase) * 0.04f;
                        frog.BodyTransform.localScale = new Vector3(0.16f, 0.09f * breathe, 0.13f);
                    }
                    frog.RootTransform.position = frog.CurrentPosition;
                    frog.RootTransform.rotation = Quaternion.Slerp(
                        frog.RootTransform.rotation,
                        Quaternion.Euler(0f, frog.Yaw, 0f),
                        6f * Time.deltaTime);

                    if (frog.StateTimer <= 0f)
                    {
                        if (!active) { frog.StateTimer = Random.Range(2f, 5f); break; }

                        float roll = Random.value;
                        if (roll < 0.35f)
                        {
                            frog.State     = AmbientFrogState.Croaking;
                            frog.StateTimer = Random.Range(1.5f, 4f);
                        }
                        else
                        {
                            int next = FindNextFrogCell(frog);
                            if (next >= 0)
                            {
                                Vector2Int targetCell = ambientFrogRoamCells[next];
                                float gy = SampleTerrainHeight(targetCell.x + 0.5f, targetCell.y + 0.5f);
                                frog.TargetPosition   = new Vector3(targetCell.x + Random.Range(0.2f, 0.8f), gy, targetCell.y + Random.Range(0.2f, 0.8f));
                                frog.StartPosition    = frog.CurrentPosition;
                                frog.CurrentCellIndex = next;
                                frog.HopProgress      = 0f;
                                frog.HopDuration      = Random.Range(0.28f, 0.52f);
                                Vector3 dir = frog.TargetPosition - frog.CurrentPosition;
                                dir.y = 0f;
                                if (dir.sqrMagnitude > 0.001f)
                                    frog.Yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                                frog.State = AmbientFrogState.Hopping;
                            }
                            else
                            {
                                frog.StateTimer = Random.Range(1f, 2.5f);
                            }
                        }
                    }
                    break;

                case AmbientFrogState.Croaking:
                    frog.StateTimer -= dt;

                    // Body pulses on Y — throat inflating
                    if (frog.BodyTransform != null)
                    {
                        float croak = 1f + Mathf.Abs(Mathf.Sin(time * 3.5f + frog.AnimPhase)) * 0.12f;
                        frog.BodyTransform.localScale = new Vector3(0.16f * croak, 0.09f, 0.13f * croak);
                    }
                    frog.RootTransform.position = frog.CurrentPosition;

                    if (frog.StateTimer <= 0f)
                    {
                        if (frog.BodyTransform != null)
                            frog.BodyTransform.localScale = new Vector3(0.16f, 0.09f, 0.13f);
                        frog.State      = AmbientFrogState.Sitting;
                        frog.StateTimer = Random.Range(2f, 5f);
                    }
                    break;

                case AmbientFrogState.Hopping:
                    frog.HopProgress += dt / Mathf.Max(0.001f, frog.HopDuration);
                    float t = Mathf.Clamp01(frog.HopProgress);

                    Vector3 hopPos = Vector3.Lerp(frog.StartPosition, frog.TargetPosition, t);
                    hopPos.y += Mathf.Sin(t * Mathf.PI) * 0.12f; // arc
                    frog.RootTransform.position = hopPos;
                    frog.RootTransform.rotation = Quaternion.Slerp(
                        frog.RootTransform.rotation,
                        Quaternion.Euler(0f, frog.Yaw, 0f),
                        12f * Time.deltaTime);

                    // Squash & stretch during hop
                    if (frog.BodyTransform != null)
                    {
                        float stretch = 1f + Mathf.Sin(t * Mathf.PI) * 0.18f;
                        frog.BodyTransform.localScale = new Vector3(0.16f / stretch, 0.09f * stretch, 0.13f);
                    }

                    if (t >= 1f)
                    {
                        frog.CurrentPosition = frog.TargetPosition;
                        if (frog.BodyTransform != null)
                            frog.BodyTransform.localScale = new Vector3(0.16f, 0.09f, 0.13f);
                        frog.State      = AmbientFrogState.Sitting;
                        frog.StateTimer = Random.Range(1.5f, 4f);
                    }
                    break;
            }
        }
    }

    private bool AreAmbientFrogsActive() => true;

    private int FindNextFrogCell(AmbientFrogData frog)
    {
        if (ambientFrogRoamCells.Count < 2) return -1;

        Vector2Int current = ambientFrogRoamCells[frog.CurrentCellIndex];
        List<int> candidates = new();
        for (int i = 0; i < ambientFrogRoamCells.Count; i++)
        {
            if (i == frog.CurrentCellIndex) continue;
            Vector2Int other = ambientFrogRoamCells[i];
            int manhattan = Mathf.Abs(other.x - current.x) + Mathf.Abs(other.y - current.y);
            if (manhattan >= 1 && manhattan <= 3) candidates.Add(i);
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : -1;
    }


}
