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

        Transform cargoVisualRoot = BuildTruckVisualModel();

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
        truckAgent.TruckCargoVisualRoot = cargoVisualRoot;

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

    private DriverAgent CreateAndRegisterDriverAgent(
        bool spawnInMotel = true,
        WorkerGender? forcedGender = null,
        string forcedName = null,
        int? forcedAge = null,
        string arrivalReason = null)
    {
        DriverAgent driver = SetupDriver(spawnInMotel, forcedGender, forcedName, forcedAge);
        driverAgents.Add(driver);
        SessionDebugLogger.Log("DRIVER", spawnInMotel
            ? $"Registered {driver.DriverName} in Motel."
            : $"Registered {driver.DriverName} for bus arrival.");
        RecordWorkerArrivalThought(driver, arrivalReason ?? (spawnInMotel ? "starter worker" : "arrival bus"));
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

    private DriverAgent SetupDriver(bool spawnInMotel = true, WorkerGender? forcedGender = null, string forcedName = null, int? forcedAge = null)
    {
        WorkerGender gender = forcedGender ?? (Random.value < 0.5f ? WorkerGender.Female : WorkerGender.Male);
        DriverAgent driver = new()
        {
            DriverId = nextDriverId,
            CitizenId = nextDriverId,
            Gender   = gender,
            DriverName = string.IsNullOrWhiteSpace(forcedName) ? GenerateWorkerName(gender) : forcedName,
            ShiftStartHour = -1,
            IsOnActiveShift = false,
            Money = Random.Range(WorkerStartingMoneyMin, WorkerStartingMoneyMax + 1)
        };
        driver.Age = forcedAge.HasValue ? Mathf.Max(18, forcedAge.Value) : Random.Range(18, 51);
        AssignWorkerRace(driver);
        AssignWorkerEducation(driver);
        AssignWorkerPortrait(driver);
        AssignWorkerPerks(driver);
        nextDriverId++;

        driver.DriverObject = new GameObject("Driver");
        driver.DriverObject.transform.SetParent(worldRoot, false);
        driver.DriverVisualRoot = new GameObject("DriverVisualRoot").transform;
        driver.DriverVisualRoot.SetParent(driver.DriverObject.transform, false);

        BuildDriverVisualModel(driver);

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


