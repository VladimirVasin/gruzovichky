using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private void SetupAmbientSquirrels()
    {
        ambientSquirrels.Clear();
        ambientSquirrelRoamPoints.Clear();
        ambientSquirrelPerchHeights.Clear();
        if (ambientSquirrelRoot != null)
        {
            Destroy(ambientSquirrelRoot.gameObject);
        }

        if (worldRoot == null || miscTreePerchPoints.Count < 2)
        {
            return;
        }

        ambientSquirrelRoot = new GameObject("AmbientSquirrels").transform;
        ambientSquirrelRoot.SetParent(worldRoot, false);

        foreach (Vector3 perch in miscTreePerchPoints)
        {
            float groundY = SampleTerrainHeight(perch.x, perch.z);
            ambientSquirrelRoamPoints.Add(new Vector3(perch.x, groundY, perch.z));
            ambientSquirrelPerchHeights.Add(perch.y);
        }

        int count = Mathf.Min(AmbientSquirrelCount, ambientSquirrelRoamPoints.Count);
        for (int i = 0; i < count; i++)
        {
            CreateAmbientSquirrel(i, count);
        }
    }

    private void CreateAmbientSquirrel(int squirrelIndex, int totalCount)
    {
        if (ambientSquirrelRoot == null || ambientSquirrelRoamPoints.Count == 0)
        {
            return;
        }

        Color bodyColor = new(0.72f, 0.42f, 0.14f);
        Color headColor = new(0.80f, 0.50f, 0.20f);
        Color tailColor = new(0.78f, 0.48f, 0.18f);
        Color earColor  = new(0.68f, 0.38f, 0.12f);

        GameObject sqRoot = new($"AmbientSquirrel_{squirrelIndex + 1}");
        sqRoot.transform.SetParent(ambientSquirrelRoot, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(sqRoot.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.10f, 0f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.14f, 0.10f, 0.20f);
        ApplyColor(body, bodyColor, VisualSmoothnessFabric);
        ConfigureStaticVisual(body, VisualSmoothnessFabric);
        if (body.TryGetComponent(out Collider bodyCol)) bodyCol.enabled = false;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(sqRoot.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.16f, 0.12f);
        head.transform.localScale = new Vector3(0.10f, 0.09f, 0.09f);
        ApplyColor(head, headColor, VisualSmoothnessFabric);
        ConfigureStaticVisual(head, VisualSmoothnessFabric);
        if (head.TryGetComponent(out Collider headCol)) headCol.enabled = false;

        GameObject leftEar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftEar.transform.SetParent(head.transform, false);
        leftEar.transform.localPosition = new Vector3(-0.35f, 0.55f, 0f);
        leftEar.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        leftEar.transform.localScale = new Vector3(0.25f, 0.50f, 0.22f);
        ApplyColor(leftEar, earColor, VisualSmoothnessFabric);
        ConfigureStaticVisual(leftEar, VisualSmoothnessFabric);
        if (leftEar.TryGetComponent(out Collider lEarCol)) lEarCol.enabled = false;

        GameObject rightEar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightEar.transform.SetParent(head.transform, false);
        rightEar.transform.localPosition = new Vector3(0.35f, 0.55f, 0f);
        rightEar.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        rightEar.transform.localScale = new Vector3(0.25f, 0.50f, 0.22f);
        ApplyColor(rightEar, earColor, VisualSmoothnessFabric);
        ConfigureStaticVisual(rightEar, VisualSmoothnessFabric);
        if (rightEar.TryGetComponent(out Collider rEarCol)) rEarCol.enabled = false;

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tail.transform.SetParent(sqRoot.transform, false);
        tail.transform.localPosition = new Vector3(0f, 0.18f, -0.12f);
        tail.transform.localRotation = Quaternion.Euler(-55f, 0f, 0f);
        tail.transform.localScale = new Vector3(0.06f, 0.16f, 0.06f);
        ApplyColor(tail, tailColor, VisualSmoothnessFabric);
        ConfigureStaticVisual(tail, VisualSmoothnessFabric);
        if (tail.TryGetComponent(out Collider tailCol)) tailCol.enabled = false;

        int step = Mathf.Max(1, ambientSquirrelRoamPoints.Count / totalCount);
        int pointIndex = squirrelIndex * step % ambientSquirrelRoamPoints.Count;
        Vector3 position = ambientSquirrelRoamPoints[pointIndex];
        float yaw = Random.Range(0f, 360f);
        sqRoot.transform.position = position;
        sqRoot.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        ambientSquirrels.Add(new AmbientSquirrelData
        {
            RootTransform    = sqRoot.transform,
            BodyTransform    = body.transform,
            HeadTransform    = head.transform,
            TailTransform    = tail.transform,
            CurrentPosition  = position,
            StartPosition    = position,
            TargetPosition   = position,
            CurrentPointIndex = pointIndex,
            TargetPointIndex  = pointIndex,
            StateTimer       = Random.Range(2f, 5f),
            AnimationPhase   = Random.Range(0f, 10f),
            TailPhase        = Random.Range(0f, 10f),
            Yaw              = yaw,
            State            = AmbientSquirrelState.Idle,
            ClimbCooldown    = Random.Range(6f, 18f),
        });
    }

    private void UpdateAmbientSquirrels()
    {
        if (ambientSquirrels.Count == 0 || ambientSquirrelRoamPoints.Count == 0)
        {
            return;
        }

        bool active = AreAmbientSquirrelsActive();
        float dt   = Time.deltaTime * gameSpeedMultiplier;
        float time = Time.time;

        for (int i = ambientSquirrels.Count - 1; i >= 0; i--)
        {
            AmbientSquirrelData sq = ambientSquirrels[i];
            if (sq.RootTransform == null)
            {
                ambientSquirrels.RemoveAt(i);
                continue;
            }

            switch (sq.State)
            {
                case AmbientSquirrelState.Idle:
                    sq.StateTimer -= dt;
                    sq.ClimbCooldown -= dt;

                    float idleBob = Mathf.Sin(time * 2.4f + sq.AnimationPhase) * 0.012f;
                    sq.RootTransform.position = sq.CurrentPosition + new Vector3(0f, idleBob, 0f);
                    sq.RootTransform.rotation = Quaternion.Slerp(
                        sq.RootTransform.rotation,
                        Quaternion.Euler(0f, sq.Yaw, 0f),
                        6f * Time.deltaTime);

                    if (sq.HeadTransform != null)
                    {
                        sq.HeadTransform.localRotation = Quaternion.Euler(
                            Mathf.Sin(time * 1.1f + sq.AnimationPhase) * 6f,
                            Mathf.Sin(time * 0.7f + sq.AnimationPhase) * 12f,
                            0f);
                    }

                    if (sq.TailTransform != null)
                    {
                        sq.TailTransform.localRotation = Quaternion.Euler(
                            -55f + Mathf.Sin(time * 1.8f + sq.TailPhase) * 8f,
                            Mathf.Sin(time * 1.4f + sq.TailPhase) * 10f,
                            0f);
                    }

                    if (sq.StateTimer <= 0f)
                    {
                        if (!active)
                        {
                            // At night force squirrels down from trees
                            if (sq.IsAtTreeTop) StartSquirrelClimbDown(sq);
                            else sq.StateTimer = Random.Range(2f, 5f);
                            break;
                        }

                        // At tree top: forage briefly or climb back down
                        if (sq.IsAtTreeTop)
                        {
                            if (Random.value < 0.35f)
                            {
                                sq.State      = AmbientSquirrelState.Foraging;
                                sq.StateTimer = Random.Range(1f, 2.5f);
                            }
                            else
                            {
                                StartSquirrelClimbDown(sq);
                            }
                            break;
                        }

                        // On ground: maybe climb up if cooldown expired
                        if (sq.ClimbCooldown <= 0f &&
                            sq.CurrentPointIndex >= 0 &&
                            sq.CurrentPointIndex < ambientSquirrelPerchHeights.Count)
                        {
                            float perchY = ambientSquirrelPerchHeights[sq.CurrentPointIndex];
                            if (perchY > sq.CurrentPosition.y + 0.5f)
                            {
                                StartSquirrelClimbUp(sq, perchY);
                                break;
                            }
                        }

                        // Normal roaming on ground
                        int next = FindNextSquirrelRoamPoint(sq);
                        if (next >= 0 && next != sq.CurrentPointIndex)
                        {
                            if (Random.value < 0.3f)
                            {
                                sq.State      = AmbientSquirrelState.Foraging;
                                sq.StateTimer = Random.Range(1.5f, 3f);
                            }
                            else
                            {
                                sq.TargetPointIndex = next;
                                sq.StartPosition    = sq.CurrentPosition;
                                sq.TargetPosition   = ambientSquirrelRoamPoints[next];
                                sq.MoveProgress     = 0f;
                                sq.MoveDuration     = Mathf.Clamp(
                                    Vector3.Distance(sq.StartPosition, sq.TargetPosition) / 2.2f,
                                    0.6f, 2.8f);
                                sq.State = AmbientSquirrelState.Running;
                            }
                        }
                        else
                        {
                            sq.StateTimer = Random.Range(2f, 5f);
                        }
                    }
                    break;

                case AmbientSquirrelState.Foraging:
                    sq.StateTimer -= dt;

                    float forageBob = Mathf.Abs(Mathf.Sin(time * 6f + sq.AnimationPhase)) * 0.06f;
                    sq.RootTransform.position = sq.CurrentPosition + new Vector3(0f, forageBob, 0f);

                    if (sq.HeadTransform != null)
                    {
                        float nod = Mathf.Sin(time * 7f + sq.AnimationPhase) * 22f;
                        sq.HeadTransform.localRotation = Quaternion.Euler(nod, 0f, 0f);
                    }

                    if (sq.TailTransform != null)
                    {
                        sq.TailTransform.localRotation = Quaternion.Euler(-72f, 0f, 0f);
                    }

                    if (sq.StateTimer <= 0f)
                    {
                        sq.State     = AmbientSquirrelState.Idle;
                        sq.StateTimer = Random.Range(2f, 5f);
                    }
                    break;

                case AmbientSquirrelState.Running:
                    sq.MoveProgress += dt / Mathf.Max(0.001f, sq.MoveDuration);
                    float runT = Mathf.Clamp01(sq.MoveProgress);

                    Vector3 runPos = Vector3.Lerp(sq.StartPosition, sq.TargetPosition, runT);
                    runPos.y += Mathf.Abs(Mathf.Sin(time * 14f + sq.AnimationPhase)) * 0.025f;
                    sq.RootTransform.position = runPos;

                    Vector3 toTarget = sq.TargetPosition - runPos;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.0001f)
                    {
                        sq.RootTransform.rotation = Quaternion.Slerp(
                            sq.RootTransform.rotation,
                            Quaternion.LookRotation(toTarget.normalized, Vector3.up),
                            14f * Time.deltaTime);
                    }

                    if (sq.BodyTransform != null)
                    {
                        sq.BodyTransform.localScale = new Vector3(0.14f, 0.09f, 0.20f);
                    }

                    if (sq.TailTransform != null)
                    {
                        sq.TailTransform.localRotation = Quaternion.Euler(
                            -10f + Mathf.Sin(time * 10f + sq.TailPhase) * 8f,
                            0f, 0f);
                    }

                    if (runT >= 1f)
                    {
                        sq.CurrentPointIndex = sq.TargetPointIndex;
                        sq.CurrentPosition   = sq.TargetPosition;
                        sq.Yaw               = sq.RootTransform.eulerAngles.y;
                        if (sq.BodyTransform != null)
                        {
                            sq.BodyTransform.localScale = new Vector3(0.14f, 0.10f, 0.20f);
                        }
                        sq.State      = AmbientSquirrelState.Idle;
                        sq.StateTimer = Random.Range(1.5f, 3.5f);
                    }
                    break;

                case AmbientSquirrelState.ClimbingUp:
                    sq.ClimbProgress += dt / Mathf.Max(0.001f, sq.ClimbDuration);
                    float climbUpT = Mathf.Clamp01(sq.ClimbProgress);

                    sq.CurrentPosition = Vector3.Lerp(sq.StartPosition, sq.TargetPosition, climbUpT);
                    sq.RootTransform.position = sq.CurrentPosition;
                    sq.RootTransform.rotation = Quaternion.Slerp(
                        sq.RootTransform.rotation,
                        Quaternion.Euler(-72f, sq.Yaw, 0f),
                        10f * Time.deltaTime);

                    if (sq.BodyTransform != null)
                        sq.BodyTransform.localScale = new Vector3(0.14f, 0.09f, 0.20f);

                    if (sq.TailTransform != null)
                        sq.TailTransform.localRotation = Quaternion.Euler(
                            -10f + Mathf.Sin(time * 9f + sq.TailPhase) * 14f,
                            Mathf.Sin(time * 6f + sq.TailPhase) * 10f, 0f);

                    if (climbUpT >= 1f)
                    {
                        sq.IsAtTreeTop    = true;
                        sq.CurrentPosition = sq.TargetPosition;
                        if (sq.BodyTransform != null)
                            sq.BodyTransform.localScale = new Vector3(0.14f, 0.10f, 0.20f);
                        sq.RootTransform.rotation = Quaternion.Euler(0f, sq.Yaw, 0f);
                        sq.State      = AmbientSquirrelState.Idle;
                        sq.StateTimer = Random.Range(2.5f, 6f);
                    }
                    break;

                case AmbientSquirrelState.ClimbingDown:
                    sq.ClimbProgress += dt / Mathf.Max(0.001f, sq.ClimbDuration);
                    float climbDownT = Mathf.Clamp01(sq.ClimbProgress);

                    sq.CurrentPosition = Vector3.Lerp(sq.StartPosition, sq.TargetPosition, climbDownT);
                    sq.RootTransform.position = sq.CurrentPosition;
                    sq.RootTransform.rotation = Quaternion.Slerp(
                        sq.RootTransform.rotation,
                        Quaternion.Euler(72f, sq.Yaw, 0f),
                        10f * Time.deltaTime);

                    if (sq.BodyTransform != null)
                        sq.BodyTransform.localScale = new Vector3(0.14f, 0.09f, 0.20f);

                    if (sq.TailTransform != null)
                        sq.TailTransform.localRotation = Quaternion.Euler(
                            -10f + Mathf.Sin(time * 9f + sq.TailPhase) * 14f,
                            Mathf.Sin(time * 6f + sq.TailPhase) * 10f, 0f);

                    if (climbDownT >= 1f)
                    {
                        sq.IsAtTreeTop     = false;
                        sq.CurrentPosition = sq.TargetPosition;
                        if (sq.BodyTransform != null)
                            sq.BodyTransform.localScale = new Vector3(0.14f, 0.10f, 0.20f);
                        sq.RootTransform.rotation = Quaternion.Euler(0f, sq.Yaw, 0f);
                        sq.ClimbCooldown  = Random.Range(12f, 28f);
                        sq.State          = AmbientSquirrelState.Idle;
                        sq.StateTimer     = Random.Range(1.5f, 3f);
                    }
                    break;
            }
        }
    }

    private void StartSquirrelClimbUp(AmbientSquirrelData sq, float perchY)
    {
        sq.StartPosition  = sq.CurrentPosition;
        sq.TargetPosition = new Vector3(sq.CurrentPosition.x, perchY, sq.CurrentPosition.z);
        sq.ClimbDuration  = Mathf.Clamp((perchY - sq.CurrentPosition.y) / 2.8f, 0.4f, 2f);
        sq.ClimbProgress  = 0f;
        sq.ClimbCooldown  = Random.Range(14f, 30f);
        sq.State          = AmbientSquirrelState.ClimbingUp;
    }

    private void StartSquirrelClimbDown(AmbientSquirrelData sq)
    {
        if (sq == null || ambientSquirrelRoamPoints.Count == 0)
        {
            return;
        }

        if (sq.CurrentPointIndex < 0 || sq.CurrentPointIndex >= ambientSquirrelRoamPoints.Count)
        {
            sq.CurrentPointIndex = FindNearestSquirrelRoamPoint(sq.CurrentPosition);
            if (sq.CurrentPointIndex < 0)
            {
                sq.IsAtTreeTop = false;
                sq.State = AmbientSquirrelState.Idle;
                sq.StateTimer = Random.Range(2f, 5f);
                return;
            }
        }

        float groundY     = ambientSquirrelRoamPoints[sq.CurrentPointIndex].y;
        sq.StartPosition  = sq.CurrentPosition;
        sq.TargetPosition = new Vector3(sq.CurrentPosition.x, groundY, sq.CurrentPosition.z);
        sq.ClimbDuration  = Mathf.Clamp((sq.CurrentPosition.y - groundY) / 2.8f, 0.4f, 2f);
        sq.ClimbProgress  = 0f;
        sq.State          = AmbientSquirrelState.ClimbingDown;
    }

    private bool AreAmbientSquirrelsActive()
    {
        int hour = GetCurrentHour();
        return hour >= 6 && hour < 18;
    }
    private int FindNextSquirrelRoamPoint(AmbientSquirrelData sq)
    {
        int current = sq?.CurrentPointIndex ?? -1;
        if (ambientSquirrelRoamPoints.Count < 2 || current < 0 || current >= ambientSquirrelRoamPoints.Count)
        {
            return -1;
        }

        List<int> candidates = new();
        Vector3 currentPos = ambientSquirrelRoamPoints[current];
        for (int i = 0; i < ambientSquirrelRoamPoints.Count; i++)
        {
            if (i == current)
            {
                continue;
            }

            float dist = Vector3.Distance(currentPos, ambientSquirrelRoamPoints[i]);
            if (dist >= 1.5f && dist <= 8f)
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
        {
            return (current + 1) % ambientSquirrelRoamPoints.Count;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private int FindNearestSquirrelRoamPoint(Vector3 position)
    {
        if (ambientSquirrelRoamPoints.Count == 0)
        {
            return -1;
        }

        int bestIndex = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < ambientSquirrelRoamPoints.Count; i++)
        {
            float distance = (ambientSquirrelRoamPoints[i] - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
