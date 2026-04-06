using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void SetupTruck()
    {
        TruckAgent firstTruck = CreateAndRegisterTruckAgent(1, 0);
        LoadTruckState(firstTruck);
        SessionDebugLogger.Log("TRUCK", $"Spawned initial {firstTruck.DisplayName} in parking slot {firstTruck.ParkingSlotIndex}.");
    }

    private TruckAgent CreateAndRegisterTruckAgent(int truckNumber, int parkingSlotIndex)
    {
        truckCell = locations[LocationType.Parking].Anchor;
        truckTargetWorld = GetParkingSlotWorldPosition(parkingSlotIndex);
        truckSegmentStartWorld = truckTargetWorld;
        truckSmoothedForward = Vector3.forward;
        truckFuel = TruckFuelCapacity;
        truckCargoSource = CargoSource.None;
        truckCargoWood = 0;
        isTruckMoving = false;
        isTruckInteracting = false;
        isTruckWaitingForService = false;
        isDriverRescueActive = false;
        isTruckAutoModeEnabled = false;
        currentAssignedTrip = TripType.None;
        currentTripPhase = TripPhase.None;
        currentRefuelPhase = RefuelPhase.None;
        currentDriverRescuePhase = DriverRescuePhase.None;
        activeTruckInteraction = TruckInteractionType.None;
        queuedTruckInteraction = TruckInteractionType.None;
        activeServiceLocation = null;
        queuedServiceLocation = null;
        currentAssignedTripReward = 0;
        truckSegmentProgress = 0f;
        truckSegmentDuration = 0f;
        truckWheelSpinAngle = 0f;
        truckSteerAngle = 0f;
        truckInteractionTimer = 0f;
        driverWalkAnimationTime = 0f;
        activePath.Clear();
        truckWheels.Clear();
        truckFrontWheels.Clear();
        truckHeadlights.Clear();

        truckObject = new GameObject($"Truck_{truckNumber}");
        truckObject.transform.SetParent(worldRoot, false);
        truckVisualRoot = new GameObject("VisualRoot").transform;
        truckVisualRoot.SetParent(truckObject.transform, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(truckVisualRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.35f, 1f);
        ApplyColor(body, new Color(0.85f, 0.2f, 0.18f));
        ConfigureShadowVisual(body);
        truckBodyTransform = body.transform;

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(truckVisualRoot, false);
        cabin.transform.localPosition = new Vector3(0f, 0.4f, 0.2f);
        cabin.transform.localScale = new Vector3(0.55f, 0.4f, 0.45f);
        ApplyColor(cabin, new Color(0.95f, 0.82f, 0.28f));
        ConfigureShadowVisual(cabin);
        truckCabinTransform = cabin.transform;

        CreateTruckHeadlightVisual(new Vector3(-0.18f, 0.39f, 0.46f), true);
        CreateTruckHeadlightVisual(new Vector3(0.18f, 0.39f, 0.46f), false);
        CreateTruckHeadlightBeam(new Vector3(-0.18f, 0.39f, 0.5f));
        CreateTruckHeadlightBeam(new Vector3(0.18f, 0.39f, 0.5f));

        Vector3[] wheelOffsets =
        {
            new(-0.28f, 0.08f, 0.32f),
            new(0.28f, 0.08f, 0.32f),
            new(-0.28f, 0.08f, -0.32f),
            new(0.28f, 0.08f, -0.32f)
        };

        for (int i = 0; i < wheelOffsets.Length; i++)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.transform.SetParent(truckVisualRoot, false);
            wheel.transform.localPosition = wheelOffsets[i];
            wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            wheel.transform.localScale = new Vector3(0.12f, 0.05f, 0.12f);
            ApplyColor(wheel, new Color(0.14f, 0.14f, 0.14f));
            ConfigureShadowVisual(wheel);
            truckWheels.Add(wheel.transform);
            if (i < 2)
            {
                truckFrontWheels.Add(wheel.transform);
            }
        }

        truckObject.transform.position = truckTargetWorld;
        truckObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        truckInteractionTargetRotation = truckObject.transform.rotation;
        SetupDriver();

        TruckAgent truckAgent = new()
        {
            TruckNumber = truckNumber,
            DisplayName = GetTruckDisplayName(truckNumber),
            ParkingSlotIndex = parkingSlotIndex
        };

        SaveTruckState(truckAgent);
        truckAgents.Add(truckAgent);
        SessionDebugLogger.Log("TRUCK", $"Registered {truckAgent.DisplayName} at parking slot {parkingSlotIndex}.");
        return truckAgent;
    }

    private void CreateTruckHeadlightVisual(Vector3 localPosition, bool isLeft)
    {
        GameObject headlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlight.transform.SetParent(truckVisualRoot, false);
        headlight.transform.localPosition = localPosition;
        headlight.transform.localScale = new Vector3(0.12f, 0.08f, 0.04f);
        ApplyColor(headlight, new Color(1f, 0.93f, 0.75f));

        Renderer rendererComponent = headlight.GetComponent<Renderer>();
        if (isLeft)
        {
            truckHeadlightLeftRenderer = rendererComponent;
        }
        else
        {
            truckHeadlightRightRenderer = rendererComponent;
        }
    }

    private void CreateTruckHeadlightBeam(Vector3 localPosition)
    {
        GameObject lightObject = new("Headlight");
        lightObject.transform.SetParent(truckVisualRoot, false);
        lightObject.transform.localPosition = localPosition;
        lightObject.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);

        Light headlight = lightObject.AddComponent<Light>();
        headlight.type = LightType.Spot;
        headlight.color = new Color(1f, 0.86f, 0.62f);
        headlight.intensity = 0f;
        headlight.range = 5.4f;
        headlight.spotAngle = 44f;
        headlight.innerSpotAngle = 22f;
        headlight.shadows = LightShadows.None;
        headlight.enabled = false;
        truckHeadlights.Add(headlight);
    }

    private void UpdateTruckHeadlights(float stylizedDaylight)
    {
        float darkness = 1f - stylizedDaylight;
        bool headlightsOn = darkness > 0.55f;
        float headlightIntensity = headlightsOn ? Mathf.Lerp(0.7f, 3.1f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
        Color lampColor = Color.Lerp(
            new Color(0.3f, 0.26f, 0.2f),
            new Color(1f, 0.94f, 0.78f),
            Mathf.Clamp01(headlightIntensity / 3.1f));

        foreach (Light headlight in truckHeadlights)
        {
            if (headlight == null)
            {
                continue;
            }

            headlight.enabled = headlightsOn;
            headlight.intensity = headlightIntensity;
        }

        if (truckHeadlightLeftRenderer != null)
        {
            truckHeadlightLeftRenderer.material.color = lampColor;
        }

        if (truckHeadlightRightRenderer != null)
        {
            truckHeadlightRightRenderer.material.color = lampColor;
        }

        UpdateLocationNightLights(stylizedDaylight);
        UpdateDriverFlashlight(stylizedDaylight);
    }

    private void UpdateLocationNightLights(float stylizedDaylight)
    {
        float darkness = 1f - stylizedDaylight;
        bool lightsOn = darkness > 0.5f;
        float lightIntensity = lightsOn ? Mathf.Lerp(0.18f, 1.15f, Mathf.InverseLerp(0.5f, 1f, darkness)) : 0f;
        Color lampColor = Color.Lerp(
            new Color(0.28f, 0.24f, 0.18f),
            new Color(1f, 0.9f, 0.72f),
            Mathf.Clamp01(lightIntensity / 1.15f));

        foreach (Light lightComponent in locationNightLights)
        {
            if (lightComponent == null)
            {
                continue;
            }

            lightComponent.enabled = lightsOn;
            lightComponent.intensity = lightIntensity;
        }

        foreach (Renderer rendererComponent in locationNightLightRenderers)
        {
            if (rendererComponent == null)
            {
                continue;
            }

            rendererComponent.material.color = lampColor;
        }

        UpdateRoadLanternLights(darkness);
    }

    private void UpdateRoadLanternLights(float darkness)
    {
        float time = Time.time;
        foreach (RoadLanternData roadLantern in roadLanterns)
        {
            if (roadLantern.Light == null || roadLantern.GlowRenderer == null)
            {
                continue;
            }

            float activationThreshold = 0.43f + roadLantern.ActivationOffset;
            float baseActivation = Mathf.InverseLerp(activationThreshold, activationThreshold + 0.18f, darkness);
            baseActivation = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(baseActivation));

            bool lightsOn = baseActivation > 0.01f;
            float flickerBlend = 1f;
            if (lightsOn)
            {
                float softPulse = Mathf.Lerp(
                    0.84f,
                    1f,
                    Mathf.PerlinNoise(roadLantern.FlickerSeed, time * roadLantern.FlickerSpeed));
                float irregularNoise = Mathf.PerlinNoise(
                    roadLantern.FlickerSeed * 2.31f,
                    17.5f + time * (roadLantern.FlickerSpeed * 2.2f));
                float randomPulse = 1f;
                if (irregularNoise > roadLantern.FlickerThreshold)
                {
                    randomPulse = Mathf.Lerp(
                        1f - roadLantern.FlickerStrength,
                        1f,
                        Mathf.PerlinNoise(31.2f + time * 14.5f, roadLantern.FlickerSeed * 0.7f));
                }

                float blinkNoise = Mathf.PerlinNoise(
                    51.8f + roadLantern.FlickerSeed * 0.19f,
                    time * (roadLantern.FlickerSpeed * 5.5f));
                float blinkPulse = blinkNoise > 0.88f
                    ? Mathf.Lerp(0.5f, 1f, Mathf.PerlinNoise(72.4f + time * 19f, roadLantern.FlickerSeed * 1.17f))
                    : 1f;

                flickerBlend = softPulse * randomPulse * blinkPulse;
            }

            float lightIntensity = Mathf.Lerp(0.18f, 1.42f, baseActivation) * flickerBlend;
            float glowStrength = Mathf.Lerp(0.14f, 1f, baseActivation) * Mathf.Lerp(0.92f, 1f, flickerBlend);
            Color lanternColor = Color.Lerp(
                new Color(0.22f, 0.19f, 0.15f),
                new Color(1f, 0.9f, 0.72f),
                Mathf.Clamp01(glowStrength));

            roadLantern.Light.enabled = lightsOn;
            roadLantern.Light.intensity = lightIntensity;
            roadLantern.Light.color = lanternColor;
            roadLantern.GlowRenderer.material.color = lanternColor;
        }
    }

    private void UpdateDriverFlashlight(float stylizedDaylight)
    {
        if (driverFlashlightLight == null)
        {
            return;
        }

        float darkness = 1f - stylizedDaylight;
        bool flashlightOn = isDriverRescueActive && driverObject != null && driverObject.activeSelf && darkness > 0.55f;
        float flashlightIntensity = flashlightOn ? Mathf.Lerp(0.65f, 2.2f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
        Color flashlightColor = Color.Lerp(
            new Color(0.24f, 0.22f, 0.18f),
            new Color(1f, 0.92f, 0.74f),
            Mathf.Clamp01(flashlightIntensity / 2.2f));

        driverFlashlightLight.enabled = flashlightOn;
        driverFlashlightLight.intensity = flashlightIntensity;
        driverFlashlightLight.color = flashlightColor;

        if (driverFlashlightRenderer != null)
        {
            driverFlashlightRenderer.material.color = flashlightColor;
        }
    }

    private void SetupDriver()
    {
        driverObject = new GameObject("Driver");
        driverObject.transform.SetParent(worldRoot, false);
        driverVisualRoot = new GameObject("DriverVisualRoot").transform;
        driverVisualRoot.SetParent(driverObject.transform, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(driverVisualRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        body.transform.localScale = new Vector3(0.22f, 0.34f, 0.22f);
        ApplyColor(body, new Color(0.22f, 0.44f, 0.88f));
        ConfigureShadowVisual(body);
        driverBodyTransform = body.transform;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(driverVisualRoot, false);
        head.transform.localPosition = new Vector3(0f, 0.88f, 0f);
        head.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
        ApplyColor(head, new Color(0.96f, 0.82f, 0.68f));
        ConfigureShadowVisual(head);
        driverHeadTransform = head.transform;

        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap.transform.SetParent(driverVisualRoot, false);
        cap.transform.localPosition = new Vector3(0f, 1.02f, 0f);
        cap.transform.localScale = new Vector3(0.26f, 0.08f, 0.26f);
        ApplyColor(cap, new Color(0.84f, 0.22f, 0.18f));
        ConfigureShadowVisual(cap);
        driverCapTransform = cap.transform;

        driverLeftArmTransform = CreateDriverLimb("DriverLeftArm", new Vector3(-0.2f, 0.56f, 0f), new Vector3(0.09f, 0.34f, 0.09f), new Color(0.22f, 0.44f, 0.88f));
        driverRightArmTransform = CreateDriverLimb("DriverRightArm", new Vector3(0.2f, 0.56f, 0f), new Vector3(0.09f, 0.34f, 0.09f), new Color(0.22f, 0.44f, 0.88f));
        driverLeftLegTransform = CreateDriverLimb("DriverLeftLeg", new Vector3(-0.09f, 0.15f, 0f), new Vector3(0.1f, 0.42f, 0.1f), new Color(0.18f, 0.22f, 0.36f));
        driverRightLegTransform = CreateDriverLimb("DriverRightLeg", new Vector3(0.09f, 0.15f, 0f), new Vector3(0.1f, 0.42f, 0.1f), new Color(0.18f, 0.22f, 0.36f));

        GameObject fuelCan = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fuelCan.transform.SetParent(driverVisualRoot, false);
        fuelCan.transform.localPosition = new Vector3(0.18f, 0.42f, 0f);
        fuelCan.transform.localScale = new Vector3(0.14f, 0.2f, 0.1f);
        ApplyColor(fuelCan, new Color(0.9f, 0.76f, 0.18f));
        ConfigureShadowVisual(fuelCan);
        driverFuelCanTransform = fuelCan.transform;
        driverFuelCanTransform.gameObject.SetActive(false);

        GameObject flashlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flashlight.transform.SetParent(driverVisualRoot, false);
        flashlight.transform.localPosition = new Vector3(0.24f, 0.57f, 0.1f);
        flashlight.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        flashlight.transform.localScale = new Vector3(0.06f, 0.06f, 0.18f);
        ApplyColor(flashlight, new Color(0.24f, 0.24f, 0.26f));
        ConfigureShadowVisual(flashlight);
        driverFlashlightTransform = flashlight.transform;
        driverFlashlightRenderer = flashlight.GetComponent<Renderer>();

        GameObject flashlightBeamObject = new("DriverFlashlight");
        flashlightBeamObject.transform.SetParent(driverFlashlightTransform, false);
        flashlightBeamObject.transform.localPosition = new Vector3(0f, 0f, 0.14f);
        flashlightBeamObject.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        driverFlashlightLight = flashlightBeamObject.AddComponent<Light>();
        driverFlashlightLight.type = LightType.Spot;
        driverFlashlightLight.color = new Color(1f, 0.88f, 0.66f);
        driverFlashlightLight.range = 4.2f;
        driverFlashlightLight.spotAngle = 40f;
        driverFlashlightLight.innerSpotAngle = 18f;
        driverFlashlightLight.shadows = LightShadows.None;
        driverFlashlightLight.intensity = 0f;
        driverFlashlightLight.enabled = false;

        driverObject.SetActive(false);
    }

    private Transform CreateDriverLimb(string name, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        limb.name = name;
        limb.transform.SetParent(driverVisualRoot, false);
        limb.transform.localPosition = localPosition;
        limb.transform.localScale = localScale;
        ApplyColor(limb, color);
        return limb.transform;
    }

    private void SetupCargoTransferVisual()
    {
        cargoTransferCrate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cargoTransferCrate.name = "CargoTransferCrate";
        cargoTransferCrate.transform.SetParent(worldRoot, false);
        cargoTransferCrate.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);
        cargoTransferCrate.GetComponent<Collider>().enabled = false;
        ApplyColor(cargoTransferCrate, new Color(0.78f, 0.58f, 0.28f));
        cargoTransferCrate.SetActive(false);
    }

    private void SetupAudio()
    {
        uiSelectClip = CreateUiPulseClip("UI_Select", 280f, 0.09f, 0.018f);
        uiPanelOpenClip = CreateUiPulseClip("UI_Open", 420f, 0.12f, 0.025f);
        uiPanelCloseClip = CreateUiPulseClip("UI_Close", 220f, 0.1f, 0.02f);
        ambientWindClip = CreateWindClip("Ambient_Wind", 6f, 0.022f);
        dayBirdsClip = CreateDayBirdsClip("Day_Birds", 6.5f, 0.03f);
        forestRustleClip = CreateRustleClip("Forest_Rustle", 5.5f, 0.03f);
        forestChopClip = CreateForestChopClip("Forest_Chop", 0.22f, 0.075f);
        nightWindClip = CreateNightWindClip("Night_Wind", 6.8f, 0.026f);
        nightCricketsClip = CreateNightCricketsClip("Night_Crickets", 5.8f, 0.024f);
        gasStationHumClip = CreateGasStationHumClip("GasStation_Hum", 4.8f, 0.018f);
        townHumClip = CreateTownHumClip("Town_Hum", 5f, 0.018f);
        warehouseCreakClip = CreateWarehouseCreakClip("Warehouse_Creak", 0.48f, 0.065f);
        owlClip = CreateOwlClip("Night_Owl", 0.95f, 0.05f);
        lanternBuzzClip = CreateLanternBuzzClip("Lantern_Buzz", 0.36f, 0.03f);
        truckIdleClip = CreateTruckIdleClip("Truck_Idle", 2.6f, 0.032f);
        truckRollClip = CreateTruckRollClip("Truck_Roll", 1.6f, 0.03f);
        cargoPickupClip = CreateCargoThunkClip("Cargo_Pickup", 0.42f, 0.06f, 0.05f);
        cargoDropClip = CreateCargoThunkClip("Cargo_Drop", 0.46f, 0.085f, 0.08f);
        routeAssignForestWarehouseClip = CreatePentatonicMotifClip("Route_ForestToWarehouse", 0.42f, 0.06f, new[] { PentatonicD4, PentatonicE4 }, new[] { 0f, 0.12f });
        routeAssignWarehouseTownClip = CreatePentatonicMotifClip("Route_WarehouseToTown", 0.46f, 0.065f, new[] { PentatonicE4, PentatonicA4 }, new[] { 0f, 0.12f });
        routeAssignRefuelClip = CreatePentatonicMotifClip("Route_Refuel", 0.44f, 0.062f, new[] { PentatonicC4, PentatonicG4 }, new[] { 0f, 0.13f });
        forestLoadCueClip = CreatePentatonicMotifClip("Forest_Load", 0.28f, 0.05f, new[] { PentatonicD4 }, new[] { 0f });
        warehouseUnloadCueClip = CreatePentatonicMotifClip("Warehouse_Unload", 0.34f, 0.052f, new[] { PentatonicE4, PentatonicG4 }, new[] { 0f, 0.09f });
        warehouseLoadCueClip = CreatePentatonicMotifClip("Warehouse_Load", 0.3f, 0.05f, new[] { PentatonicE4 }, new[] { 0f });
        townUnloadCueClip = CreatePentatonicMotifClip("Town_Unload", 0.48f, 0.064f, new[] { PentatonicA4, PentatonicC5, PentatonicE5 }, new[] { 0f, 0.08f, 0.16f });
        gasStationRefuelCueClip = CreatePentatonicMotifClip("GasStation_Refuel", 0.38f, 0.056f, new[] { PentatonicG4, PentatonicC5 }, new[] { 0f, 0.12f });
        parkingReturnCueClip = CreatePentatonicMotifClip("Parking_Return", 0.36f, 0.05f, new[] { PentatonicC4, PentatonicE4 }, new[] { 0f, 0.1f });
        moneyRewardClip = CreateMoneyRewardClip("Money_Reward", 0.6f, 0.08f);

        uiAudioSource = CreateAudioSource("UIAudio", null, false, 0.82f, 1f, false);
        ambientAudioSource = CreateAudioSource("AmbientWind", worldRoot, true, 0.34f, 0f, false);
        dayBirdsAudioSource = CreateAudioSource("DayBirds", worldRoot, true, 0.26f, 0f, false);
        forestAudioSource = CreateAudioSource("ForestAmbience", locations[LocationType.Forest].RootObject.transform, true, 0.42f, 0.82f, false);
        forestWorkerAudioSource = CreateAudioSource("ForestWorkers", locations[LocationType.Forest].RootObject.transform, false, 0.32f, 0.9f, false);
        nightWindAudioSource = CreateAudioSource("NightWind", worldRoot, true, 0.26f, 0f, false);
        nightCricketsAudioSource = CreateAudioSource("NightCrickets", locations[LocationType.Forest].RootObject.transform, true, 0.24f, 0.82f, false);
        gasStationAudioSource = CreateAudioSource("GasStationHum", locations[LocationType.GasStation].RootObject.transform, true, 0.2f, 0.84f, false);
        townAudioSource = CreateAudioSource("TownAmbience", locations[LocationType.Town].RootObject.transform, true, 0.34f, 0.9f, false);
        warehouseAudioSource = CreateAudioSource("WarehouseAmbience", locations[LocationType.Warehouse].RootObject.transform, false, 0.18f, 0.88f, false);
        ambienceFxAudioSource = CreateAudioSource("AmbienceFX", worldRoot, false, 0.24f, 0f, false);

        ambientAudioSource.clip = ambientWindClip;
        ambientAudioSource.Play();

        dayBirdsAudioSource.clip = dayBirdsClip;
        dayBirdsAudioSource.Play();

        forestAudioSource.clip = forestRustleClip;
        forestAudioSource.Play();

        nightWindAudioSource.clip = nightWindClip;
        nightWindAudioSource.Play();

        nightCricketsAudioSource.clip = nightCricketsClip;
        nightCricketsAudioSource.Play();

        gasStationAudioSource.clip = gasStationHumClip;
        gasStationAudioSource.Play();

        townAudioSource.clip = townHumClip;
        townAudioSource.Play();

        dayBirdTimer = Random.Range(4.5f, 8f);
        nightOwlTimer = Random.Range(8f, 14f);
        lanternBuzzTimer = Random.Range(5f, 9f);
        warehouseCreakTimer = Random.Range(6f, 10f);

        foreach (TruckAgent truckAgent in truckAgents)
        {
            SetupTruckAudio(truckAgent);
        }
    }

    private void SetupTruckAudio(TruckAgent truckAgent)
    {
        if (truckAgent == null || truckAgent.TruckObject == null)
        {
            return;
        }

        truckAgent.TruckLoopAudioSource = CreateAudioSource($"TruckLoop_{truckAgent.TruckNumber}", truckAgent.TruckObject.transform, true, 0.28f, 0.65f, false);
        truckAgent.TruckFxAudioSource = CreateAudioSource($"TruckFX_{truckAgent.TruckNumber}", truckAgent.TruckObject.transform, false, 0.58f, 0.8f, false);
        truckAgent.TruckLoopAudioSource.clip = truckIdleClip;
        truckAgent.TruckLoopAudioSource.Play();
    }

}

