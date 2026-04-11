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
        DriverAgent firstDriver = CreateAndRegisterDriverAgent();
        AssignDriverToTruckRoster(firstTruck, firstDriver);
        LoadTruckState(firstTruck);
        SessionDebugLogger.Log("TRUCK", $"Spawned initial {firstTruck.DisplayName} in parking slot {firstTruck.ParkingSlotIndex}.");
        SessionDebugLogger.Log("DRIVER", $"{firstDriver.DriverName} hired and assigned to {firstTruck.DisplayName} roster.");
    }

    private TruckAgent CreateAndRegisterTruckAgent(int truckNumber, int parkingSlotIndex)
    {
        truckCell = locations[LocationType.Parking].Anchor;
        truckTargetWorld = GetParkingSlotWorldPosition(parkingSlotIndex);
        truckSegmentStartWorld = truckTargetWorld;
        truckSmoothedForward = Vector3.forward;
        truckFuel = TruckFuelCapacity;
        truckCargoType = CargoType.None;
        truckCargoAmount = 0;
        isTruckMoving = false;
        isTruckInteracting = false;
        isTruckWaitingForService = false;
        isDriverRescueActive = false;
        isTruckAutoModeEnabled = false;
        currentAssignedTrip = TripType.None;
        currentTripPhase = TripPhase.None;
        currentRefuelPhase = RefuelPhase.None;
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
        TruckAgent truckAgent = new()
        {
            TruckNumber = truckNumber,
            DisplayName = GetTruckDisplayName(truckNumber),
            ParkingSlotIndex = parkingSlotIndex,
            EngineAudioPhaseOffset = Random.Range(0f, 100f),
            EngineAudioWobbleSpeed = Random.Range(0.82f, 1.24f),
            EngineAudioPitchBias = Random.Range(0.965f, 1.04f),
            EngineAudioVolumeBias = Random.Range(0.94f, 1.08f)
        };

        SaveTruckState(truckAgent);
        truckAgents.Add(truckAgent);
        SessionDebugLogger.Log("TRUCK", $"Registered {truckAgent.DisplayName} at parking slot {parkingSlotIndex}.");
        return truckAgent;
    }

    private DriverAgent CreateAndRegisterDriverAgent(bool spawnInMotel = true)
    {
        DriverAgent driver = SetupDriver(spawnInMotel);
        driverAgents.Add(driver);
        SessionDebugLogger.Log("DRIVER", spawnInMotel
            ? $"Registered {driver.DriverName} in Motel."
            : $"Registered {driver.DriverName} for bus arrival.");
        return driver;
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
            truckHeadlightLeftMaterial = rendererComponent != null ? rendererComponent.material : null;
        }
        else
        {
            truckHeadlightRightRenderer = rendererComponent;
            truckHeadlightRightMaterial = rendererComponent != null ? rendererComponent.material : null;
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
        headlight.shadows = LightShadows.Soft;
        headlight.enabled = false;
        truckHeadlights.Add(headlight);
    }

    private void UpdateTruckHeadlights(float stylizedDaylight, DriverAgent driver)
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

        if (truckHeadlightLeftMaterial != null)
        {
            truckHeadlightLeftMaterial.color = lampColor;
        }

        if (truckHeadlightRightMaterial != null)
        {
            truckHeadlightRightMaterial.color = lampColor;
        }

        UpdateLocationNightLights(stylizedDaylight);
        UpdateDriverFlashlight(driver, stylizedDaylight);
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

        foreach (Material material in locationNightLightMaterials)
        {
            if (material == null)
            {
                continue;
            }

            material.color = lampColor;
        }

        UpdateRoadLanternLights(darkness);
    }

    private void UpdateRoadLanternLights(float darkness)
    {
        float time = Time.time;
        foreach (RoadLanternData roadLantern in roadLanterns)
        {
            if (roadLantern.Light == null || roadLantern.GlowMaterial == null)
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
            roadLantern.GlowMaterial.color = lanternColor;
        }
    }

    private void UpdateDriverFlashlight(DriverAgent driver, float stylizedDaylight)
    {
        if (driver == null || driver.DriverFlashlightLight == null)
        {
            return;
        }

        float darkness = 1f - stylizedDaylight;
        bool flashlightOn = IsDriverBusyWalkPhase(driver) && driver.DriverObject != null && driver.DriverObject.activeSelf && darkness > 0.55f;
        float flashlightIntensity = flashlightOn ? Mathf.Lerp(0.65f, 2.2f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
        Color flashlightColor = Color.Lerp(
            new Color(0.24f, 0.22f, 0.18f),
            new Color(1f, 0.92f, 0.74f),
            Mathf.Clamp01(flashlightIntensity / 2.2f));

        driver.DriverFlashlightLight.enabled = flashlightOn;
        driver.DriverFlashlightLight.intensity = flashlightIntensity;
        driver.DriverFlashlightLight.color = flashlightColor;

        if (driver.DriverFlashlightMaterial != null)
        {
            driver.DriverFlashlightMaterial.color = flashlightColor;
        }
    }

    private DriverAgent SetupDriver(bool spawnInMotel = true)
    {
        DriverAgent driver = new()
        {
            DriverId = nextDriverId,
            DriverName = $"Driver #{nextDriverId}",
            ShiftStartHour = -1,
            IsOnActiveShift = false
        };
        nextDriverId++;

        driver.DriverObject = new GameObject("Driver");
        driver.DriverObject.transform.SetParent(worldRoot, false);
        driver.DriverVisualRoot = new GameObject("DriverVisualRoot").transform;
        driver.DriverVisualRoot.SetParent(driver.DriverObject.transform, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(driver.DriverVisualRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        body.transform.localScale = new Vector3(0.22f, 0.34f, 0.22f);
        ApplyColor(body, new Color(0.22f, 0.44f, 0.88f));
        ConfigureShadowVisual(body);
        driver.DriverBodyTransform = body.transform;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(driver.DriverVisualRoot, false);
        head.transform.localPosition = new Vector3(0f, 0.88f, 0f);
        head.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
        ApplyColor(head, new Color(0.96f, 0.82f, 0.68f));
        ConfigureShadowVisual(head);
        driver.DriverHeadTransform = head.transform;

        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap.transform.SetParent(driver.DriverVisualRoot, false);
        cap.transform.localPosition = new Vector3(0f, 1.02f, 0f);
        cap.transform.localScale = new Vector3(0.26f, 0.08f, 0.26f);
        ApplyColor(cap, new Color(0.84f, 0.22f, 0.18f));
        ConfigureShadowVisual(cap);
        driver.DriverCapTransform = cap.transform;

        driver.DriverLeftArmTransform = CreateDriverLimb(driver.DriverVisualRoot, "DriverLeftArm", new Vector3(-0.2f, 0.56f, 0f), new Vector3(0.09f, 0.34f, 0.09f), new Color(0.22f, 0.44f, 0.88f));
        driver.DriverRightArmTransform = CreateDriverLimb(driver.DriverVisualRoot, "DriverRightArm", new Vector3(0.2f, 0.56f, 0f), new Vector3(0.09f, 0.34f, 0.09f), new Color(0.22f, 0.44f, 0.88f));
        driver.DriverLeftLegTransform = CreateDriverLimb(driver.DriverVisualRoot, "DriverLeftLeg", new Vector3(-0.09f, 0.15f, 0f), new Vector3(0.1f, 0.42f, 0.1f), new Color(0.18f, 0.22f, 0.36f));
        driver.DriverRightLegTransform = CreateDriverLimb(driver.DriverVisualRoot, "DriverRightLeg", new Vector3(0.09f, 0.15f, 0f), new Vector3(0.1f, 0.42f, 0.1f), new Color(0.18f, 0.22f, 0.36f));

        GameObject fuelCan = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fuelCan.transform.SetParent(driver.DriverVisualRoot, false);
        fuelCan.transform.localPosition = new Vector3(0.18f, 0.42f, 0f);
        fuelCan.transform.localScale = new Vector3(0.14f, 0.2f, 0.1f);
        ApplyColor(fuelCan, new Color(0.9f, 0.76f, 0.18f));
        ConfigureShadowVisual(fuelCan);
        driver.DriverFuelCanTransform = fuelCan.transform;
        driver.DriverFuelCanTransform.gameObject.SetActive(false);

        GameObject flashlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flashlight.transform.SetParent(driver.DriverVisualRoot, false);
        flashlight.transform.localPosition = new Vector3(0.24f, 0.57f, 0.1f);
        flashlight.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        flashlight.transform.localScale = new Vector3(0.06f, 0.06f, 0.18f);
        ApplyColor(flashlight, new Color(0.24f, 0.24f, 0.26f));
        ConfigureShadowVisual(flashlight);
        driver.DriverFlashlightTransform = flashlight.transform;
        driver.DriverFlashlightRenderer = flashlight.GetComponent<Renderer>();
        driver.DriverFlashlightMaterial = driver.DriverFlashlightRenderer != null ? driver.DriverFlashlightRenderer.material : null;

        GameObject flashlightBeamObject = new("DriverFlashlight");
        flashlightBeamObject.transform.SetParent(driver.DriverFlashlightTransform, false);
        flashlightBeamObject.transform.localPosition = new Vector3(0f, 0f, 0.14f);
        flashlightBeamObject.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        driver.DriverFlashlightLight = flashlightBeamObject.AddComponent<Light>();
        driver.DriverFlashlightLight.type = LightType.Spot;
        driver.DriverFlashlightLight.color = new Color(1f, 0.88f, 0.66f);
        driver.DriverFlashlightLight.range = 4.2f;
        driver.DriverFlashlightLight.spotAngle = 40f;
        driver.DriverFlashlightLight.innerSpotAngle = 18f;
        driver.DriverFlashlightLight.shadows = LightShadows.None;
        driver.DriverFlashlightLight.intensity = 0f;
        driver.DriverFlashlightLight.enabled = false;

        driver.MotelIdlePosition = GetDriverIdleMotelPosition(driver.DriverId - 1);
        driver.DriverObject.transform.position = driver.MotelIdlePosition;
        driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.WalkTargetWorld = driver.MotelIdlePosition;
        driver.DriverObject.SetActive(spawnInMotel);
        driver.IsArrivingByBus = !spawnInMotel;
        return driver;
    }

    private Transform CreateDriverLimb(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        limb.name = name;
        limb.transform.SetParent(parent, false);
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
        uiSelectClip = CreateUiPulseClip("UI_Select", 280f, 0.09f, 0.024f);
        menuHoverClip = CreateUiPulseClip("Menu_Hover", 356f, 0.1f, 0.028f);
        uiPanelOpenClip = CreateUiPulseClip("UI_Open", 420f, 0.12f, 0.032f);
        uiPanelCloseClip = CreateUiPulseClip("UI_Close", 220f, 0.1f, 0.026f);
        ambientWindClip = CreateWindClip("Ambient_Wind", 6f, 0.028f);
        dayBirdsClip = CreateDayBirdsClip("Day_Birds", 6.5f, 0.038f);
        forestRustleClip = CreateRustleClip("Forest_Rustle", 5.5f, 0.038f);
        forestChopClip = CreateForestChopClip("Forest_Chop", 0.22f, 0.1f);
        nightWindClip = CreateNightWindClip("Night_Wind", 6.8f, 0.033f);
        nightCricketsClip = CreateNightCricketsClip("Night_Crickets", 5.8f, 0.031f);
        gasStationHumClip = CreateGasStationHumClip("GasStation_Hum", 4.8f, 0.024f);
        sawmillHumClip = CreateTownHumClip("Sawmill_Hum", 5f, 0.024f);
        warehouseCreakClip = CreateWarehouseCreakClip("Warehouse_Creak", 0.48f, 0.082f);
        owlClip = CreateOwlClip("Night_Owl", 0.95f, 0.065f);
        lanternBuzzClip = CreateLanternBuzzClip("Lantern_Buzz", 0.36f, 0.038f);
        truckIdleClip = CreateTruckIdleClip("Truck_Idle", 2.6f, 0.042f);
        truckRollClip = CreateTruckRollClip("Truck_Roll", 1.6f, 0.041f);
        cargoPickupClip = CreateCargoThunkClip("Cargo_Pickup", 0.42f, 0.078f, 0.064f);
        cargoDropClip = CreateCargoThunkClip("Cargo_Drop", 0.46f, 0.11f, 0.1f);
        routeAssignForestSawmillClip = CreatePentatonicMotifClip("Route_ForestToSawmill", 0.42f, 0.082f, new[] { PentatonicD4, PentatonicE4 }, new[] { 0f, 0.12f });
        routeAssignSawmillWarehouseClip = CreatePentatonicMotifClip("Route_SawmillToWarehouse", 0.46f, 0.088f, new[] { PentatonicE4, PentatonicA4 }, new[] { 0f, 0.12f });
        routeAssignRefuelClip = CreatePentatonicMotifClip("Route_Refuel", 0.44f, 0.084f, new[] { PentatonicC4, PentatonicG4 }, new[] { 0f, 0.13f });
        forestLoadCueClip = CreatePentatonicMotifClip("Forest_Load", 0.28f, 0.068f, new[] { PentatonicD4 }, new[] { 0f });
        sawmillUnloadCueClip = CreatePentatonicMotifClip("Sawmill_Unload", 0.34f, 0.07f, new[] { PentatonicE4, PentatonicG4 }, new[] { 0f, 0.09f });
        sawmillLoadCueClip = CreatePentatonicMotifClip("Sawmill_Load", 0.3f, 0.068f, new[] { PentatonicE4 }, new[] { 0f });
        warehouseUnloadBoardsCueClip = CreatePentatonicMotifClip("Warehouse_UnloadBoards", 0.48f, 0.09f, new[] { PentatonicA4, PentatonicC5, PentatonicE5 }, new[] { 0f, 0.08f, 0.16f });
        gasStationRefuelCueClip = CreatePentatonicMotifClip("GasStation_Refuel", 0.38f, 0.076f, new[] { PentatonicG4, PentatonicC5 }, new[] { 0f, 0.12f });
        parkingReturnCueClip = CreatePentatonicMotifClip("Parking_Return", 0.36f, 0.068f, new[] { PentatonicC4, PentatonicE4 }, new[] { 0f, 0.1f });
        moneyRewardClip = CreateMoneyRewardClip("Money_Reward", 0.6f, 0.1f);
        edgeHighwayBusPassbyClip = CreateBusPassbyClip("EdgeHighway_BusPassby", 1.15f, 0.055f);
        riverAmbientClip = CreateRiverAmbientClip("River_Ambient", 8f, 0.034f);
        riverSplashClip  = CreateWaterSplashClip("River_Splash", 0.28f, 0.075f);
        boatMotorClip    = CreateBoatMotorClip("Boat_Motor", 3.8f, 0.038f);

        uiAudioSource = CreateAudioSource("UIAudio", null, false, 0.96f, 0f, false);
        uiAudioSource.ignoreListenerPause = true;
        ambientAudioSource = CreateAudioSource("AmbientWind", worldRoot, true, 0.42f, 0f, false);
        dayBirdsAudioSource = CreateAudioSource("DayBirds", worldRoot, true, 0.34f, 0f, false);
        forestAudioSource = CreateAudioSource("ForestAmbience", locations[LocationType.Forest].RootObject.transform, true, 0.52f, 0.82f, false);
        forestWorkerAudioSource = CreateAudioSource("ForestWorkers", locations[LocationType.Forest].RootObject.transform, false, 0.44f, 0.9f, false);
        nightWindAudioSource = CreateAudioSource("NightWind", worldRoot, true, 0.34f, 0f, false);
        nightCricketsAudioSource = CreateAudioSource("NightCrickets", locations[LocationType.Forest].RootObject.transform, true, 0.33f, 0.82f, false);
        gasStationAudioSource = CreateAudioSource("GasStationHum", locations[LocationType.GasStation].RootObject.transform, true, 0.28f, 0.84f, false);
        townAudioSource = CreateAudioSource("SawmillAmbience", locations[LocationType.Sawmill].RootObject.transform, true, 0.44f, 0.9f, false);
        warehouseAudioSource = CreateAudioSource("WarehouseAmbience", locations[LocationType.Warehouse].RootObject.transform, false, 0.26f, 0.88f, false);
        ambienceFxAudioSource = CreateAudioSource("AmbienceFX", worldRoot, false, 0.34f, 0f, false);
        riverAmbientAudioSource = CreateAudioSource("RiverAmbient", worldRoot, true, 0.38f, 0f, false);

        ambientAudioSource.clip = null;
        ambientAudioSource.Stop();

        dayBirdsAudioSource.clip = null;
        dayBirdsAudioSource.Stop();

        forestAudioSource.clip = null;
        forestAudioSource.Stop();

        nightWindAudioSource.clip = nightWindClip;
        nightWindAudioSource.Play();

        nightCricketsAudioSource.clip = nightCricketsClip;
        nightCricketsAudioSource.Play();

        gasStationAudioSource.clip = null;
        gasStationAudioSource.Stop();

        townAudioSource.clip = null;
        townAudioSource.Stop();

        riverAmbientAudioSource.clip = riverAmbientClip;
        riverAmbientAudioSource.Play();

        dayBirdTimer = Random.Range(4.5f, 8f);
        nightOwlTimer = Random.Range(8f, 14f);
        lanternBuzzTimer = Random.Range(5f, 9f);
        warehouseCreakTimer = Random.Range(6f, 10f);
        riverSplashTimer = Random.Range(4f, 10f);

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

        truckAgent.TruckLoopAudioSource = CreateAudioSource($"TruckLoop_{truckAgent.TruckNumber}", truckAgent.TruckObject.transform, true, 0.4f, 0.65f, false);
        truckAgent.TruckFxAudioSource = CreateAudioSource($"TruckFX_{truckAgent.TruckNumber}", truckAgent.TruckObject.transform, false, 0.74f, 0.8f, false);
        truckAgent.TruckLoopAudioSource.clip = null;
        truckAgent.TruckLoopAudioSource.Stop();
    }

}

