using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private static readonly string[] CarModelNames = { "Sedan", "Pickup", "Hatchback" };
    private static readonly Color[] CarBodyColors =
    {
        new(0.22f, 0.38f, 0.70f),
        new(0.75f, 0.18f, 0.15f),
        new(0.48f, 0.62f, 0.22f)
    };

    private static readonly string[] WorkerMaleFirstNames =
    {
        // Disco Elysium style
        "Raul",
        "Villem",
        "Tomas",
        "Egor",
        "Doru",
        "Remy",
        "Jovan",
        "Petyr",
        "Kuno",
        "Matis",
        "Aldor",
        "Bas",
        "Iosef",
        "Harko",
        "Denes",
        // Armenian
        "Armen",
        "Vardan",
        "Tigran",
        "Hayk",
        "Sargis"
    };

    private static readonly string[] WorkerFemaleFirstNames =
    {
        // Disco Elysium style
        "Vera",
        "Marta",
        "Neli",
        "Rina",
        "Lara",
        "Tina",
        "Dara",
        "Nina",
        "Lena",
        "Mira",
        "Vika",
        "Elvi",
        "Zara",
        // Armenian
        "Ani",
        "Sona",
        "Aida",
        "Nare",
        "Lilit",
        "Astghik",
        "Gohar"
    };

    private static readonly string[] WorkerLastNames =
    {
        // Disco Elysium style
        "Drost",
        "Kvelj",
        "Muren",
        "Vannek",
        "Tache",
        "Vollaers",
        "Struik",
        "Pemmick",
        "Larnac",
        "Renne",
        "Faroul",
        "Clauber",
        "Trant",
        "Bruk",
        "Odors",
        // Armenian
        "Petrosyan",
        "Grigoryan",
        "Mkrtchyan",
        "Hovhannisyan",
        "Sargsyan"
    };

    private void SetupTruck()
    {
        if (locations.ContainsKey(LocationType.Parking))
        {
            if (TryProvisionTruckFromParkingCapacity(out TruckAgent firstTruck, "initial Parking capacity"))
            {
                LoadTruckState(firstTruck);
                SessionDebugLogger.Log("TRUCK", $"Spawned initial {firstTruck.DisplayName} in parking slot {firstTruck.ParkingSlotIndex}.");
            }
        }
        else
        {
            SessionDebugLogger.Log("TRUCK", "Initial truck skipped: Parking not available in this mode.");
        }

        DriverAgent firstStarterWorker = null;
        bool hasStarterWithHigherEducation = false;
        for (int i = 0; i < InitialWorkerCount; i++)
        {
            DriverAgent worker = CreateAndRegisterDriverAgent();
            firstStarterWorker ??= worker;
            hasStarterWithHigherEducation |= worker.Education == WorkerEducationLevel.Higher;
            SessionDebugLogger.Log("DRIVER", $"{worker.DriverName} hired (unassigned, idle).");
        }

        if (!hasStarterWithHigherEducation)
        {
            PromoteWorkerToHigherEducation(firstStarterWorker, "starter labor exchange guarantee");
        }
    }

    private TruckAgent CreateAndRegisterTruckAgent(int truckNumber, int parkingSlotIndex)
    {
        locations.TryGetValue(LocationType.Parking, out LocationData parking);
        truckCell = parking?.Anchor ?? Vector2Int.zero;
        truckTargetWorld = parking != null ? GetParkingSlotWorldPosition(parkingSlotIndex) : Vector3.zero;
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
        ApplyColor(body, new Color(0.85f, 0.2f, 0.18f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(body, VisualSmoothnessVehicleMetal);
        truckBodyTransform = body.transform;

        GameObject bodyStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bodyStripe.transform.SetParent(truckVisualRoot, false);
        bodyStripe.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        bodyStripe.transform.localScale = new Vector3(0.74f, 0.05f, 1.02f);
        ApplyColor(bodyStripe, new Color(0.96f, 0.9f, 0.72f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(bodyStripe, VisualSmoothnessVehicleMetal);

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(truckVisualRoot, false);
        cabin.transform.localPosition = new Vector3(0f, 0.4f, 0.2f);
        cabin.transform.localScale = new Vector3(0.55f, 0.4f, 0.45f);
        ApplyColor(cabin, new Color(0.95f, 0.82f, 0.28f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(cabin, VisualSmoothnessVehicleMetal);
        truckCabinTransform = cabin.transform;

        GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windshield.transform.SetParent(truckVisualRoot, false);
        windshield.transform.localPosition = new Vector3(0f, 0.43f, 0.42f);
        windshield.transform.localScale = new Vector3(0.42f, 0.18f, 0.04f);
        ApplyColor(windshield, new Color(0.68f, 0.86f, 0.94f), VisualSmoothnessGlass);
        ConfigureShadowVisual(windshield, VisualSmoothnessGlass);

        GameObject truckShadowBlob = GameObject.CreatePrimitive(PrimitiveType.Cube);
        truckShadowBlob.transform.SetParent(truckVisualRoot, false);
        truckShadowBlob.transform.localPosition = new Vector3(0f, -0.01f, 0f);
        truckShadowBlob.transform.localScale = new Vector3(0.88f, 0.01f, 1.16f);
        Renderer truckShadowRenderer = truckShadowBlob.GetComponent<Renderer>();
        truckShadowRenderer.material = CreateTransparentOverlayMaterial(new Color(0f, 0f, 0f, 0.14f));
        truckShadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
        truckShadowRenderer.receiveShadows = false;
        if (truckShadowBlob.TryGetComponent(out Collider truckShadowCollider))
        {
            Object.Destroy(truckShadowCollider);
        }

        CreateTruckHeadlightVisual(new Vector3(-0.18f, 0.39f, 0.46f), true);
        CreateTruckHeadlightVisual(new Vector3(0.18f, 0.39f, 0.46f), false);
        CreateTruckHeadlightBeam(new Vector3(-0.18f, 0.39f, 0.5f));
        CreateTruckHeadlightBeam(new Vector3(0.18f, 0.39f, 0.5f));

        GameObject cargoVisualRoot = new("CargoVisualRoot");
        cargoVisualRoot.transform.SetParent(truckVisualRoot, false);
        cargoVisualRoot.transform.localPosition = new Vector3(0f, 0.47f, -0.24f);
        cargoVisualRoot.SetActive(false);

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
            ApplyColor(wheel, new Color(0.14f, 0.14f, 0.14f), VisualSmoothnessRubber);
            ConfigureShadowVisual(wheel, VisualSmoothnessRubber);
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
        truckAgent.TruckCargoVisualRoot = cargoVisualRoot.transform;

        SaveTruckState(truckAgent);
        UpdateTruckCargoVisual(truckAgent, forceRebuild: true);
        truckAgents.Add(truckAgent);
        SessionDebugLogger.Log("TRUCK", $"Registered {truckAgent.DisplayName} at parking slot {parkingSlotIndex}.");
        return truckAgent;
    }

    private void UpdateTruckCargoVisual(TruckAgent truckAgent, bool forceRebuild = false)
    {
        if (truckAgent?.TruckCargoVisualRoot == null)
        {
            return;
        }

        CargoType cargoType = truckAgent.TruckCargoType;
        int cargoAmount = truckAgent.TruckCargoAmount;
        bool hasCargo = cargoAmount > 0 && cargoType != CargoType.None;
        if (!forceRebuild &&
            truckAgent.TruckCargoVisualType == cargoType &&
            truckAgent.TruckCargoVisualAmount == cargoAmount)
        {
            truckAgent.TruckCargoVisualRoot.gameObject.SetActive(hasCargo);
            return;
        }

        for (int i = truckAgent.TruckCargoVisualRoot.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(truckAgent.TruckCargoVisualRoot.GetChild(i).gameObject);
        }

        truckAgent.TruckCargoVisualType = cargoType;
        truckAgent.TruckCargoVisualAmount = cargoAmount;
        truckAgent.TruckCargoVisualRoot.gameObject.SetActive(hasCargo);

        if (!hasCargo)
        {
            return;
        }

        int visibleUnits = Mathf.Clamp(cargoAmount, 1, TruckCargoCapacity);
        switch (cargoType)
        {
            case CargoType.Logs:
                BuildTruckLogCargo(truckAgent.TruckCargoVisualRoot, visibleUnits);
                break;
            case CargoType.Boards:
                BuildTruckBoardCargo(truckAgent.TruckCargoVisualRoot, visibleUnits);
                break;
            case CargoType.Cotton:
                BuildTruckBlockCargo(truckAgent.TruckCargoVisualRoot, visibleUnits, new Color(0.96f, 0.95f, 0.88f), new Vector3(0.24f, 0.18f, 0.22f), 0.08f);
                break;
            case CargoType.Textile:
                BuildTruckBlockCargo(truckAgent.TruckCargoVisualRoot, visibleUnits, new Color(0.54f, 0.45f, 0.78f), new Vector3(0.25f, 0.12f, 0.24f), 0.055f);
                break;
            case CargoType.Furniture:
                BuildTruckBlockCargo(truckAgent.TruckCargoVisualRoot, visibleUnits, new Color(0.58f, 0.36f, 0.18f), new Vector3(0.22f, 0.22f, 0.22f), 0.09f, VisualSmoothnessWood);
                break;
        }
    }

    private void BuildTruckLogCargo(Transform parent, int visibleUnits)
    {
        int count = Mathf.Clamp(visibleUnits + 1, 2, 5);
        for (int i = 0; i < count; i++)
        {
            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.transform.SetParent(parent, false);
            log.transform.localPosition = new Vector3(Mathf.Lerp(-0.22f, 0.22f, count == 1 ? 0.5f : i / (float)(count - 1)), 0.02f + (i % 2) * 0.045f, 0f);
            log.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            log.transform.localScale = new Vector3(0.055f, 0.34f, 0.055f);
            ApplyColor(log, new Color(0.42f, 0.24f, 0.12f), VisualSmoothnessWood);
            ConfigureShadowVisual(log, VisualSmoothnessWood);
        }
    }

    private void BuildTruckBoardCargo(Transform parent, int visibleUnits)
    {
        int count = Mathf.Clamp(visibleUnits + 1, 2, 5);
        for (int i = 0; i < count; i++)
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.SetParent(parent, false);
            board.transform.localPosition = new Vector3(0f, 0.018f + i * 0.028f, Mathf.Lerp(-0.22f, 0.22f, count == 1 ? 0.5f : i / (float)(count - 1)));
            board.transform.localScale = new Vector3(0.46f, 0.025f, 0.12f);
            ApplyColor(board, new Color(0.78f, 0.58f, 0.32f), VisualSmoothnessWood);
            ConfigureShadowVisual(board, VisualSmoothnessWood);
        }
    }

    private void BuildTruckBlockCargo(Transform parent, int visibleUnits, Color color, Vector3 scale, float stackStep, float smoothness = VisualSmoothnessFabric)
    {
        for (int i = 0; i < visibleUnits; i++)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.SetParent(parent, false);
            float x = i % 2 == 0 ? -0.14f : 0.14f;
            float z = -0.16f + (i / 2) * 0.16f;
            block.transform.localPosition = new Vector3(x, 0.035f + (i / 2) * stackStep, z);
            block.transform.localScale = scale;
            ApplyColor(block, color * Random.Range(0.92f, 1.06f), smoothness);
            ConfigureShadowVisual(block, smoothness);
        }
    }

    private void BuildTruckBarrelCargo(Transform parent, int visibleUnits, Color color)
    {
        int count = Mathf.Clamp(visibleUnits, 1, TruckCargoCapacity);
        for (int i = 0; i < count; i++)
        {
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.transform.SetParent(parent, false);
            float x = i % 2 == 0 ? -0.13f : 0.13f;
            float z = -0.18f + (i / 2) * 0.18f;
            barrel.transform.localPosition = new Vector3(x, 0.045f, z);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.09f, 0.11f, 0.09f);
            ApplyColor(barrel, color, VisualSmoothnessVehicleMetal);
            ConfigureShadowVisual(barrel, VisualSmoothnessVehicleMetal);
        }
    }

    private DriverAgent CreateAndRegisterDriverAgent(bool spawnInMotel = true)
    {
        DriverAgent driver = SetupDriver(spawnInMotel);
        driverAgents.Add(driver);
        SessionDebugLogger.Log("DRIVER", spawnInMotel
            ? $"Registered {driver.DriverName} in Motel."
            : $"Registered {driver.DriverName} for bus arrival.");
        RecordWorkerArrivalThought(driver, spawnInMotel ? "starter worker" : "arrival bus");
        EvaluateWorkerActiveThoughtRules(driver);
        return driver;
    }

    private void CreateTruckHeadlightVisual(Vector3 localPosition, bool isLeft)
    {
        GameObject headlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlight.transform.SetParent(truckVisualRoot, false);
        headlight.transform.localPosition = localPosition;
        headlight.transform.localScale = new Vector3(0.12f, 0.08f, 0.04f);
        ApplyColor(headlight, new Color(1f, 0.82f, 0.52f));

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
        headlight.color = new Color(1f, 0.72f, 0.42f);
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
        float headlightIntensity = headlightsOn ? Mathf.Lerp(0.78f, 3.35f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
        Color lampColor = Color.Lerp(
            new Color(0.34f, 0.22f, 0.11f),
            new Color(1f, 0.80f, 0.50f),
            Mathf.Clamp01(headlightIntensity / 3.35f));

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

        UpdateDriverFlashlight(driver, stylizedDaylight);
    }

    private void UpdateDriverFlashlight(DriverAgent driver, float stylizedDaylight)
    {
        if (driver == null || driver.DriverFlashlightLight == null)
        {
            return;
        }

        float darkness = 1f - stylizedDaylight;
        bool flashlightOn = IsDriverBusyWalkPhase(driver) && driver.DriverObject != null && driver.DriverObject.activeSelf && darkness > 0.55f;
        float flashlightIntensity = flashlightOn ? Mathf.Lerp(0.7f, 2.45f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
        Color flashlightColor = Color.Lerp(
            new Color(0.28f, 0.18f, 0.10f),
            new Color(1f, 0.82f, 0.50f),
            Mathf.Clamp01(flashlightIntensity / 2.45f));

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
        WorkerGender gender = Random.value < 0.5f ? WorkerGender.Female : WorkerGender.Male;
        DriverAgent driver = new()
        {
            DriverId = nextDriverId,
            Gender   = gender,
            DriverName = GenerateWorkerName(gender),
            ShiftStartHour = -1,
            IsOnActiveShift = false,
            Money = Random.Range(WorkerStartingMoneyMin, WorkerStartingMoneyMax + 1)
        };
        driver.Age = Random.Range(18, 51);
        AssignWorkerEducation(driver);
        AssignWorkerPortrait(driver);
        AssignWorkerPerks(driver);
        nextDriverId++;

        driver.DriverObject = new GameObject("Driver");
        driver.DriverObject.transform.SetParent(worldRoot, false);
        driver.DriverVisualRoot = new GameObject("DriverVisualRoot").transform;
        driver.DriverVisualRoot.SetParent(driver.DriverObject.transform, false);

        GameObject driverShadowBlob = GameObject.CreatePrimitive(PrimitiveType.Cube);
        driverShadowBlob.transform.SetParent(driver.DriverVisualRoot, false);
        driverShadowBlob.transform.localPosition = new Vector3(0f, -0.01f, 0f);
        driverShadowBlob.transform.localScale = new Vector3(0.34f, 0.008f, 0.34f);
        Renderer driverShadowRenderer = driverShadowBlob.GetComponent<Renderer>();
        driverShadowRenderer.material = CreateTransparentOverlayMaterial(new Color(0f, 0f, 0f, 0.16f));
        driverShadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
        driverShadowRenderer.receiveShadows = false;
        if (driverShadowBlob.TryGetComponent(out Collider driverShadowCollider))
        {
            Object.Destroy(driverShadowCollider);
        }

        bool isFemale = driver.Gender == WorkerGender.Female;

        Color[] femaleShirts =
        {
            new Color(0.2f, 0.56f, 0.52f),
            new Color(0.78f, 0.42f, 0.34f),
            new Color(0.58f, 0.34f, 0.72f),
            new Color(0.36f, 0.6f, 0.28f)
        };
        Color[] maleShirts =
        {
            new Color(0.22f, 0.44f, 0.88f),
            new Color(0.82f, 0.38f, 0.22f),
            new Color(0.28f, 0.6f, 0.34f),
            new Color(0.68f, 0.52f, 0.2f)
        };
        Color[] femaleTrousers =
        {
            new Color(0.14f, 0.26f, 0.28f),
            new Color(0.28f, 0.2f, 0.2f),
            new Color(0.2f, 0.18f, 0.32f),
            new Color(0.16f, 0.28f, 0.18f)
        };
        Color[] maleTrousers =
        {
            new Color(0.18f, 0.22f, 0.36f),
            new Color(0.24f, 0.18f, 0.18f),
            new Color(0.16f, 0.26f, 0.18f),
            new Color(0.3f, 0.24f, 0.14f)
        };

        Color shirtColor = isFemale
            ? femaleShirts[(driver.DriverId - 1) % femaleShirts.Length]
            : maleShirts[(driver.DriverId - 1) % maleShirts.Length];
        Color trouserColor = isFemale
            ? femaleTrousers[(driver.DriverId - 1) % femaleTrousers.Length]
            : maleTrousers[(driver.DriverId - 1) % maleTrousers.Length];

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(driver.DriverVisualRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        body.transform.localScale = isFemale ? new Vector3(0.20f, 0.34f, 0.20f) : new Vector3(0.22f, 0.34f, 0.22f);
        ApplyColor(body, shirtColor, VisualSmoothnessFabric);
        ConfigureShadowVisual(body, VisualSmoothnessFabric);
        driver.DriverBodyTransform = body.transform;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(driver.DriverVisualRoot, false);
        head.transform.localPosition = new Vector3(0f, 0.88f, 0f);
        head.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
        ApplyColor(head, new Color(0.96f, 0.82f, 0.68f), VisualSmoothnessSkin);
        ConfigureShadowVisual(head, VisualSmoothnessSkin);
        driver.DriverHeadTransform = head.transform;

        if (isFemale)
        {
            // Hair bun instead of flat cap
            GameObject bun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bun.transform.SetParent(driver.DriverVisualRoot, false);
            bun.transform.localPosition = new Vector3(0f, 1.02f, -0.04f);
            bun.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            ApplyColor(bun, new Color(0.31f, 0.18f, 0.09f), VisualSmoothnessFabric);
            ConfigureShadowVisual(bun, VisualSmoothnessFabric);
            driver.DriverCapTransform = bun.transform;
        }
        else
        {
            // Flat cap for men
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.transform.SetParent(driver.DriverVisualRoot, false);
            cap.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            cap.transform.localScale = new Vector3(0.26f, 0.08f, 0.26f);
            ApplyColor(cap, new Color(0.84f, 0.22f, 0.18f), VisualSmoothnessFabric);
            ConfigureShadowVisual(cap, VisualSmoothnessFabric);
            driver.DriverCapTransform = cap.transform;
        }

        driver.DriverLeftArmTransform  = CreateDriverLimb(driver.DriverVisualRoot, "DriverLeftArm",  new Vector3(-0.2f,  0.56f, 0f), new Vector3(0.09f, 0.34f, 0.09f), shirtColor);
        driver.DriverRightArmTransform = CreateDriverLimb(driver.DriverVisualRoot, "DriverRightArm", new Vector3( 0.2f,  0.56f, 0f), new Vector3(0.09f, 0.34f, 0.09f), shirtColor);
        driver.DriverLeftLegTransform  = CreateDriverLimb(driver.DriverVisualRoot, "DriverLeftLeg",  new Vector3(-0.09f, 0.15f, 0f), new Vector3(0.1f,  0.42f, 0.1f),  trouserColor);
        driver.DriverRightLegTransform = CreateDriverLimb(driver.DriverVisualRoot, "DriverRightLeg", new Vector3( 0.09f, 0.15f, 0f), new Vector3(0.1f,  0.42f, 0.1f),  trouserColor);

        GameObject fuelCan = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fuelCan.transform.SetParent(driver.DriverVisualRoot, false);
        fuelCan.transform.localPosition = new Vector3(0.18f, 0.42f, 0f);
        fuelCan.transform.localScale = new Vector3(0.14f, 0.2f, 0.1f);
        ApplyColor(fuelCan, new Color(0.9f, 0.76f, 0.18f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(fuelCan, VisualSmoothnessVehicleMetal);
        driver.DriverFuelCanTransform = fuelCan.transform;
        driver.DriverFuelCanTransform.gameObject.SetActive(false);

        GameObject flashlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flashlight.transform.SetParent(driver.DriverVisualRoot, false);
        flashlight.transform.localPosition = new Vector3(0.24f, 0.57f, 0.1f);
        flashlight.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        flashlight.transform.localScale = new Vector3(0.06f, 0.06f, 0.18f);
        ApplyColor(flashlight, new Color(0.24f, 0.24f, 0.26f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(flashlight, VisualSmoothnessVehicleMetal);
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

        driver.MotelIdlePosition = GetDriverIdleMotelPosition(driver.DriverId - 1, driver);
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
        ApplyColor(limb, color, VisualSmoothnessFabric);
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
        EnsureGeneratedAudioClipsCreated();
        EnsureUiAudioSource();

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

        if (truckAgent.TruckLoopAudioSource != null)
        {
            return;
        }

        truckAgent.TruckLoopAudioSource = CreateAudioSource($"TruckLoop_{truckAgent.TruckNumber}", truckAgent.TruckObject.transform, true, 0.4f, 0.65f, false);
        truckAgent.TruckLoopAudioSource.clip = null;
        truckAgent.TruckLoopAudioSource.Stop();
    }

}


