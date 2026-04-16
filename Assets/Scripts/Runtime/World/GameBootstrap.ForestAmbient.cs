using UnityEngine;

public partial class GameBootstrap
{
    private void SetupForestWorkers()
    {
        forestWorkers.Clear();
        if (!locations.TryGetValue(LocationType.Forest, out LocationData forestLocation) || forestWorkPoints.Count == 0)
        {
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            ForestWorkerAmbient worker = CreateForestWorkerAmbient(i + 1, forestLocation.RootObject.transform);
            worker.WorkPointIndex = Mathf.Clamp((i * 3) % forestWorkPoints.Count, 0, forestWorkPoints.Count - 1);
            worker.TargetWorldPosition = GetForestWorkerGroundPoint(forestWorkPoints[worker.WorkPointIndex]);
            worker.RootObject.transform.position = worker.TargetWorldPosition + new Vector3(i == 0 ? -0.35f : 0.35f, 0f, i == 0 ? -0.2f : 0.2f);
            worker.RootObject.transform.rotation = Quaternion.Euler(0f, i == 0 ? 145f : 215f, 0f);
            worker.State = ForestWorkerState.Chopping;
            worker.StateTimer = Random.Range(2.4f, 3.8f);
            worker.AnimationTime = Random.Range(0f, 1.5f);
            worker.ChopSoundCooldown = Random.Range(0.08f, 0.32f);
            worker.RootObject.SetActive(false);   // hidden until a logistics worker enters
            forestWorkers.Add(worker);
        }

        SessionDebugLogger.Log("WORLD", $"Spawned {forestWorkers.Count} ambient forest workers.");
    }

    private void UpdateForestWorkers()
    {
        if (forestWorkers.Count == 0 || forestWorkPoints.Count == 0)
        {
            return;
        }

        bool workActive = IsLocationOperational(LocationType.Forest);

        foreach (ForestWorkerAmbient fw in forestWorkers)
        {
            if (fw?.RootObject != null && fw.RootObject.activeSelf != workActive)
                fw.RootObject.SetActive(workActive);
        }

        if (!workActive)
        {
            StopForestWorkerAudio();
            return;
        }

        foreach (ForestWorkerAmbient worker in forestWorkers)
        {
            if (worker?.RootObject == null)
            {
                continue;
            }

            UpdateForestWorkerFlashlight(worker);

            switch (worker.State)
            {
                case ForestWorkerState.Walking:
                    UpdateForestWorkerWalking(worker);
                    break;
                case ForestWorkerState.Pausing:
                    UpdateForestWorkerPausing(worker);
                    break;
                default:
                    UpdateForestWorkerChopping(worker);
                    break;
            }
        }
    }

    private void UpdateForestWorkerWalking(ForestWorkerAmbient worker)
    {
        Vector3 target = GetForestWorkerGroundPoint(worker.TargetWorldPosition);
        Vector3 current = worker.RootObject.transform.position;
        Vector3 direction = target - current;
        direction.y = 0f;
        float distance = direction.magnitude;

        if (distance <= 0.04f)
        {
            worker.RootObject.transform.position = target;
            BeginForestWorkerChopping(worker);
            return;
        }

        Vector3 step = direction.normalized * worker.MoveSpeed * Time.deltaTime;
        if (step.magnitude >= distance)
        {
            current = target;
        }
        else
        {
            current += step;
        }

        current.y = GetForestWorkerGroundY();
        worker.RootObject.transform.position = current;
        worker.RootObject.transform.rotation = Quaternion.Slerp(
            worker.RootObject.transform.rotation,
            Quaternion.LookRotation(direction.normalized, Vector3.up),
            10f * Time.deltaTime);

        worker.AnimationTime += Time.deltaTime * 6.9f;
        float swing = Mathf.Sin(worker.AnimationTime);
        float bob = Mathf.Abs(Mathf.Sin(worker.AnimationTime * 2f)) * 0.042f;
        ApplyForestWorkerPose(worker, swing, bob, 0f, false);
    }

    private void UpdateForestWorkerChopping(ForestWorkerAmbient worker)
    {
        worker.StateTimer -= Time.deltaTime;
        worker.AnimationTime += Time.deltaTime * 3.65f;

        float chopWave = Mathf.Sin(worker.AnimationTime * 1.35f);
        float chopPhase = Mathf.Clamp01((chopWave + 1f) * 0.5f);
        float easedChop = 1f - Mathf.Pow(1f - chopPhase, 2f);
        float bodyBob = Mathf.Max(0f, Mathf.Sin(worker.AnimationTime * 2.7f)) * 0.025f;
        float bodyLean = Mathf.Lerp(-4f, 12f, easedChop);

        worker.ChopSoundCooldown -= Time.deltaTime;
        if (worker.ChopSoundCooldown <= 0f && chopPhase > 0.92f)
        {
            worker.ChopSoundCooldown = Random.Range(0.58f, 1.02f);
            PlayForestWorkerFx(forestChopClip, worker.RootObject.transform.position, Random.Range(0.55f, 0.82f));
            SpawnForestWoodChips(worker);
            TriggerForestTreeWobble(worker.WorkPointIndex, worker.RootObject.transform.position);
            TryAddForestLogFromChop();
        }

        Vector3 facingTarget = GetForestWorkerGroundPoint(worker.TargetWorldPosition);
        Vector3 flatFacing = facingTarget - worker.RootObject.transform.position;
        flatFacing.y = 0f;
        if (flatFacing.sqrMagnitude > 0.001f)
        {
            worker.RootObject.transform.rotation = Quaternion.Slerp(
                worker.RootObject.transform.rotation,
                Quaternion.LookRotation(flatFacing.normalized, Vector3.up),
                7f * Time.deltaTime);
        }

        ApplyForestWorkerPose(worker, 0f, bodyBob, bodyLean, true, easedChop);

        if (worker.StateTimer > 0f)
        {
            return;
        }

        worker.State = ForestWorkerState.Pausing;
        worker.StateTimer = Random.Range(0.45f, 1.1f);
        worker.PauseYaw = Random.Range(-22f, 22f);
    }

    private void UpdateForestWorkerPausing(ForestWorkerAmbient worker)
    {
        worker.StateTimer -= Time.deltaTime;
        worker.AnimationTime += Time.deltaTime * 1.05f;
        float idleBob = Mathf.Sin(worker.AnimationTime * 1.8f) * 0.01f;
        ApplyForestWorkerPose(worker, 0f, idleBob, -1.5f, false);
        worker.RootObject.transform.rotation = Quaternion.Slerp(
            worker.RootObject.transform.rotation,
            Quaternion.Euler(0f, worker.RootObject.transform.eulerAngles.y + worker.PauseYaw * Time.deltaTime, 0f),
            2f * Time.deltaTime);

        if (worker.StateTimer > 0f)
        {
            return;
        }

        int nextIndex = PickNextForestWorkPoint(worker.WorkPointIndex);
        worker.WorkPointIndex = nextIndex;
        worker.TargetWorldPosition = GetForestWorkerGroundPoint(forestWorkPoints[nextIndex]);
        worker.State = ForestWorkerState.Walking;
        worker.AnimationTime = Random.Range(0f, 0.75f);
    }

    private void ApplyForestWorkerPose(ForestWorkerAmbient worker, float walkSwing, float bodyBob, float bodyLean, bool isChopping, float chopProgress = 0f)
    {
        if (worker.VisualRoot == null)
        {
            return;
        }

        worker.VisualRoot.localPosition = new Vector3(0f, bodyBob, 0f);
        worker.VisualRoot.localRotation = Quaternion.Euler(0f, 0f, isChopping ? 0f : walkSwing * 2.4f);

        if (worker.BodyTransform != null)
        {
            worker.BodyTransform.localRotation = Quaternion.Euler(bodyLean + (isChopping ? 0f : walkSwing * 4f), 0f, 0f);
        }

        if (worker.HeadTransform != null)
        {
            worker.HeadTransform.localRotation = Quaternion.Euler(isChopping ? -bodyLean * 0.35f : -walkSwing * 2f, 0f, 0f);
        }

        if (worker.CapTransform != null)
        {
            worker.CapTransform.localRotation = Quaternion.Euler(isChopping ? -bodyLean * 0.25f : -walkSwing * 1.5f, 0f, 0f);
        }

        if (worker.LeftArmTransform != null)
        {
            float leftArmPitch = isChopping ? Mathf.Lerp(26f, -34f, chopProgress) : walkSwing * 24f;
            worker.LeftArmTransform.localRotation = Quaternion.Euler(leftArmPitch, 0f, isChopping ? -18f : 0f);
        }

        if (worker.RightArmTransform != null)
        {
            float rightArmPitch = isChopping ? Mathf.Lerp(-88f, 104f, chopProgress) : -walkSwing * 24f;
            worker.RightArmTransform.localRotation = Quaternion.Euler(rightArmPitch, 0f, isChopping ? 14f : 0f);
        }

        if (worker.LeftLegTransform != null)
        {
            worker.LeftLegTransform.localRotation = Quaternion.Euler(isChopping ? -8f : -walkSwing * 22f, 0f, 0f);
        }

        if (worker.RightLegTransform != null)
        {
            worker.RightLegTransform.localRotation = Quaternion.Euler(isChopping ? 8f : walkSwing * 22f, 0f, 0f);
        }

        if (worker.AxeTransform != null)
        {
            worker.AxeTransform.localPosition = isChopping
                ? new Vector3(0.07f, -0.14f, 0.2f)
                : new Vector3(0.04f, -0.16f, 0.16f);
            worker.AxeTransform.localRotation = Quaternion.Euler(
                isChopping ? Mathf.Lerp(-8f, 112f, chopProgress) : 46f,
                0f,
                isChopping ? -24f : -18f);
        }

        if (worker.FlashlightTransform != null)
        {
            worker.FlashlightTransform.localPosition = new Vector3(-0.22f, 0.56f, 0.09f);
            worker.FlashlightTransform.localRotation = Quaternion.Euler(
                isChopping ? 14f + chopProgress * 18f : 12f + walkSwing * 6f,
                isChopping ? -6f : walkSwing * 3f,
                0f);
        }
    }

    private int PickNextForestWorkPoint(int currentIndex)
    {
        if (forestWorkPoints.Count <= 1)
        {
            return 0;
        }

        int nextIndex = currentIndex;
        float currentBestDistance = -1f;
        Vector3 currentPoint = forestWorkPoints[Mathf.Clamp(currentIndex, 0, forestWorkPoints.Count - 1)];

        for (int i = 0; i < forestWorkPoints.Count; i++)
        {
            if (i == currentIndex)
            {
                continue;
            }

            float distance = Vector3.Distance(currentPoint, forestWorkPoints[i]);
            if (distance > currentBestDistance)
            {
                currentBestDistance = distance;
                nextIndex = i;
            }
        }

        if (Random.value < 0.4f)
        {
            for (int attempts = 0; attempts < 6; attempts++)
            {
                int candidate = Random.Range(0, forestWorkPoints.Count);
                if (candidate == currentIndex)
                {
                    continue;
                }

                float candidateDistance = Vector3.Distance(currentPoint, forestWorkPoints[candidate]);
                if (candidateDistance > 0.65f)
                {
                    nextIndex = candidate;
                    break;
                }
            }
        }

        return nextIndex == currentIndex ? (currentIndex + 1) % forestWorkPoints.Count : nextIndex;
    }

    private float GetForestWorkerGroundY()
    {
        return locations.TryGetValue(LocationType.Forest, out LocationData forestLocation)
            ? forestLocation.RootObject.transform.position.y
            : 0f;
    }

    private Vector3 GetForestWorkerGroundPoint(Vector3 source)
    {
        return new Vector3(source.x, GetForestWorkerGroundY(), source.z);
    }

    private ForestWorkerAmbient CreateForestWorkerAmbient(int workerNumber, Transform parent)
    {
        GameObject workerRoot = new($"ForestWorker_{workerNumber}");
        workerRoot.transform.SetParent(parent, false);

        Transform visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(workerRoot.transform, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(visualRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        body.transform.localScale = new Vector3(0.22f, 0.34f, 0.22f);
        ApplyColor(body, workerNumber % 2 == 0 ? new Color(0.3f, 0.46f, 0.82f) : new Color(0.28f, 0.5f, 0.3f));
        ConfigureShadowVisual(body);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(visualRoot, false);
        head.transform.localPosition = new Vector3(0f, 0.88f, 0f);
        head.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
        ApplyColor(head, new Color(0.96f, 0.82f, 0.68f));
        ConfigureShadowVisual(head);

        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap.transform.SetParent(visualRoot, false);
        cap.transform.localPosition = new Vector3(0f, 1.02f, 0f);
        cap.transform.localScale = new Vector3(0.26f, 0.08f, 0.26f);
        ApplyColor(cap, workerNumber % 2 == 0 ? new Color(0.84f, 0.22f, 0.18f) : new Color(0.76f, 0.56f, 0.18f));
        ConfigureShadowVisual(cap);

        Transform leftArm = CreateWorkerLimb(visualRoot, "LeftArm", new Vector3(-0.2f, 0.56f, 0f), new Vector3(0.09f, 0.34f, 0.09f), new Color(0.24f, 0.42f, 0.74f));
        Transform rightArm = CreateWorkerLimb(visualRoot, "RightArm", new Vector3(0.2f, 0.56f, 0f), new Vector3(0.09f, 0.34f, 0.09f), new Color(0.24f, 0.42f, 0.74f));
        Transform leftLeg = CreateWorkerLimb(visualRoot, "LeftLeg", new Vector3(-0.09f, 0.15f, 0f), new Vector3(0.1f, 0.42f, 0.1f), new Color(0.18f, 0.22f, 0.36f));
        Transform rightLeg = CreateWorkerLimb(visualRoot, "RightLeg", new Vector3(0.09f, 0.15f, 0f), new Vector3(0.1f, 0.42f, 0.1f), new Color(0.18f, 0.22f, 0.36f));

        GameObject axeRoot = new("Axe");
        axeRoot.transform.SetParent(rightArm, false);
        axeRoot.transform.localPosition = new Vector3(0.04f, -0.16f, 0.16f);

        GameObject axeHandle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        axeHandle.transform.SetParent(axeRoot.transform, false);
        axeHandle.transform.localPosition = Vector3.zero;
        axeHandle.transform.localScale = new Vector3(0.05f, 0.34f, 0.05f);
        ApplyColor(axeHandle, new Color(0.54f, 0.35f, 0.18f));
        ConfigureShadowVisual(axeHandle);

        GameObject axeHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        axeHead.transform.SetParent(axeRoot.transform, false);
        axeHead.transform.localPosition = new Vector3(0.08f, 0.13f, 0f);
        axeHead.transform.localScale = new Vector3(0.14f, 0.1f, 0.05f);
        ApplyColor(axeHead, new Color(0.7f, 0.72f, 0.76f));
        ConfigureShadowVisual(axeHead);

        GameObject flashlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flashlight.transform.SetParent(visualRoot, false);
        flashlight.transform.localPosition = new Vector3(-0.22f, 0.56f, 0.09f);
        flashlight.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        flashlight.transform.localScale = new Vector3(0.05f, 0.05f, 0.15f);
        ApplyColor(flashlight, new Color(0.24f, 0.24f, 0.26f));
        ConfigureShadowVisual(flashlight);
        Renderer flashlightRenderer = flashlight.GetComponent<Renderer>();

        GameObject flashlightBeamObject = new("ForestWorkerFlashlight");
        flashlightBeamObject.transform.SetParent(flashlight.transform, false);
        flashlightBeamObject.transform.localPosition = new Vector3(0f, 0f, 0.12f);
        flashlightBeamObject.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        Light flashlightLight = flashlightBeamObject.AddComponent<Light>();
        flashlightLight.type = LightType.Spot;
        flashlightLight.color = new Color(1f, 0.88f, 0.66f);
        flashlightLight.range = 3.8f;
        flashlightLight.spotAngle = 36f;
        flashlightLight.innerSpotAngle = 16f;
        flashlightLight.shadows = LightShadows.None;
        flashlightLight.intensity = 0f;
        flashlightLight.enabled = false;

        return new ForestWorkerAmbient
        {
            Name = $"Forest Worker #{workerNumber}",
            RootObject = workerRoot,
            VisualRoot = visualRoot,
            BodyTransform = body.transform,
            HeadTransform = head.transform,
            CapTransform = cap.transform,
            LeftArmTransform = leftArm,
            RightArmTransform = rightArm,
            LeftLegTransform = leftLeg,
            RightLegTransform = rightLeg,
            AxeTransform = axeRoot.transform,
            FlashlightTransform = flashlight.transform,
            FlashlightLight = flashlightLight,
            FlashlightRenderer = flashlightRenderer,
            FlashlightMaterial = flashlightRenderer != null ? flashlightRenderer.material : null,
            MoveSpeed = Random.Range(1.05f, 1.35f)
        };
    }

    private void BeginForestWorkerChopping(ForestWorkerAmbient worker)
    {
        worker.State = ForestWorkerState.Chopping;
        worker.StateTimer = Random.Range(2.6f, 4.4f);
        worker.AnimationTime = Random.Range(0f, 0.75f);
        worker.ChopSoundCooldown = Random.Range(0.08f, 0.28f);
        ApplyForestWorkerPose(worker, 0f, 0f, 0f, true);
    }

    private void SpawnForestWoodChips(ForestWorkerAmbient worker)
    {
        if (worker?.AxeTransform == null || worldRoot == null)
        {
            return;
        }

        Vector3 chipOrigin = worker.AxeTransform.position + worker.RootObject.transform.forward * 0.12f + Vector3.up * 0.08f;
        for (int i = 0; i < 4; i++)
        {
            GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chip.name = "WoodChip";
            chip.transform.SetParent(worldRoot, false);
            chip.transform.position = chipOrigin + new Vector3(
                Random.Range(-0.04f, 0.04f),
                Random.Range(-0.02f, 0.05f),
                Random.Range(-0.04f, 0.04f));
            chip.transform.rotation = Random.rotation;
            chip.transform.localScale = Vector3.one * Random.Range(0.035f, 0.06f);
            ApplyColor(chip, new Color(0.76f, 0.62f, 0.34f));
            ConfigureStaticVisual(chip);

            Rigidbody rigidbody = chip.AddComponent<Rigidbody>();
            rigidbody.useGravity = true;
            rigidbody.mass = 0.02f;
            rigidbody.linearDamping = 0.8f;
            Vector3 impulse =
                worker.RootObject.transform.forward * Random.Range(0.18f, 0.42f) +
                Vector3.up * Random.Range(0.18f, 0.32f) +
                worker.RootObject.transform.right * Random.Range(-0.12f, 0.12f);
            rigidbody.AddForce(impulse, ForceMode.Impulse);

            Destroy(chip, 1.1f);
        }
    }

    private void TriggerForestTreeWobble(int workPointIndex, Vector3 workerPosition)
    {
        if (workPointIndex < 0 || workPointIndex >= forestWorkTargetTrees.Count)
        {
            return;
        }

        Transform treeTransform = forestWorkTargetTrees[workPointIndex];
        if (treeTransform == null)
        {
            return;
        }

        ForestTreeWobble wobble = null;
        for (int i = 0; i < forestTreeWobbles.Count; i++)
        {
            if (forestTreeWobbles[i].TreeTransform == treeTransform)
            {
                wobble = forestTreeWobbles[i];
                break;
            }
        }

        Vector3 awayFromWorker = treeTransform.position - workerPosition;
        awayFromWorker.y = 0f;
        if (awayFromWorker.sqrMagnitude < 0.0001f)
        {
            awayFromWorker = treeTransform.right;
        }

        Vector3 wobbleAxis = Vector3.Cross(Vector3.up, awayFromWorker.normalized);
        if (wobbleAxis.sqrMagnitude < 0.0001f)
        {
            wobbleAxis = treeTransform.right;
        }

        if (wobble == null)
        {
            wobble = new ForestTreeWobble
            {
                TreeTransform = treeTransform,
                BaseRotation = treeTransform.localRotation
            };
            forestTreeWobbles.Add(wobble);
        }

        wobble.BaseRotation = treeTransform.localRotation;
        wobble.Axis = wobbleAxis.normalized;
        wobble.Timer = 0f;
        wobble.Duration = Random.Range(0.34f, 0.5f);
        wobble.Amplitude = Random.Range(4.5f, 7.5f);
    }

    private void UpdateForestTreeWobbles()
    {
        if (!IsLocationOperational(LocationType.Forest))
        {
            for (int i = forestTreeWobbles.Count - 1; i >= 0; i--)
            {
                ForestTreeWobble wobble = forestTreeWobbles[i];
                if (wobble?.TreeTransform == null)
                {
                    forestTreeWobbles.RemoveAt(i);
                    continue;
                }

                wobble.TreeTransform.localRotation = wobble.BaseRotation;
            }

            return;
        }

        for (int i = forestTreeWobbles.Count - 1; i >= 0; i--)
        {
            ForestTreeWobble wobble = forestTreeWobbles[i];
            if (wobble?.TreeTransform == null)
            {
                forestTreeWobbles.RemoveAt(i);
                continue;
            }

            wobble.Timer += Time.deltaTime;
            float normalized = wobble.Duration <= 0.0001f ? 1f : Mathf.Clamp01(wobble.Timer / wobble.Duration);
            float envelope = 1f - normalized;
            float oscillation = Mathf.Sin(normalized * Mathf.PI * 2.5f);
            float angle = oscillation * wobble.Amplitude * envelope;
            wobble.TreeTransform.localRotation = wobble.BaseRotation * Quaternion.AngleAxis(angle, wobble.Axis);

            if (normalized < 1f)
            {
                continue;
            }

            wobble.TreeTransform.localRotation = wobble.BaseRotation;
            forestTreeWobbles.RemoveAt(i);
        }
    }

    private Transform CreateWorkerLimb(Transform visualRoot, string name, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        limb.name = name;
        limb.transform.SetParent(visualRoot, false);
        limb.transform.localPosition = localPosition;
        limb.transform.localScale = localScale;
        ApplyColor(limb, color);
        ConfigureShadowVisual(limb);
        return limb.transform;
    }

    private void StopForestWorkerAudio()
    {
        if (forestWorkerAudioSource != null && forestWorkerAudioSource.isPlaying)
        {
            forestWorkerAudioSource.Stop();
        }
    }

    private void UpdateForestWorkerFlashlight(ForestWorkerAmbient worker)
    {
        if (worker?.FlashlightLight == null)
        {
            return;
        }

        float darkness = 1f - currentStylizedDaylight;
        bool flashlightOn = darkness > 0.55f;
        float flashlightIntensity = flashlightOn ? Mathf.Lerp(0.45f, 1.7f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
        Color flashlightColor = Color.Lerp(
            new Color(0.24f, 0.22f, 0.18f),
            new Color(1f, 0.92f, 0.74f),
            Mathf.Clamp01(flashlightIntensity / 1.7f));

        worker.FlashlightLight.enabled = flashlightOn;
        worker.FlashlightLight.intensity = flashlightIntensity;
        worker.FlashlightLight.color = flashlightColor;

        if (worker.FlashlightMaterial != null)
        {
            worker.FlashlightMaterial.color = flashlightColor;
        }
    }
}
