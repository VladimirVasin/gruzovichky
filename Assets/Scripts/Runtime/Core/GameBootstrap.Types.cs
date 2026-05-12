using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private sealed class RoadLanternData
    {
        public Light Light;
        public Renderer GlowRenderer;
        public Material GlowMaterial;
        public float ActivationOffset;
        public float FlickerSeed;
        public float FlickerSpeed;
        public float FlickerStrength;
        public float FlickerThreshold;
    }

    private sealed class ShadowLodRendererData
    {
        public Renderer Renderer;
        public ShadowCastingMode OriginalShadowMode;
    }

    private sealed class MoneyLedgerEntry
    {
        public string TimeLabel;
        public int TreasuryDelta;
        public string FromLabel;
        public string ToLabel;
        public MoneyAccountKind FromAccountKind = MoneyAccountKind.External;
        public MoneyAccountKind ToAccountKind = MoneyAccountKind.External;
        public int FromOwnerId;
        public int ToOwnerId;
        public MoneyTransactionReasonKind ReasonKind = MoneyTransactionReasonKind.Other;
        public string Reason;
        public int? TreasuryAfter;
        public int? RecipientBalanceAfter;
    }

    private enum MoneyAccountKind
    {
        CityBudget,
        ResidentWallet,
        BuildingCash,
        External,
        Debug
    }

    private enum MoneyTransactionReasonKind
    {
        Construction,
        CityUpgrade,
        Salary,
        ServiceFee,
        BuildingTax,
        Trade,
        TransportFare,
        PropertyPurchase,
        VehiclePurchase,
        Gambling,
        Household,
        Debug,
        Other
    }

    private sealed class RiverBoatData
    {
        public Transform RootTransform;
        public float WorldX;
        public float TravelDirection;
        public float Speed;
        public float BobPhase;
        public float RockPhase;
        public bool HasEnteredRiver;
        public Renderer LanternRenderer;
        public Light LanternLight;
        public AudioSource BoatAudioSource;
    }

    private sealed class EdgeHighwayBusData
    {
        public Transform RootTransform;
        public float WorldX;
        public float TravelDirection;
        public bool IsCitySideLane;
        public float Speed;
        public float BobPhase;
        public Color BodyColor;
        public bool HasPlayedPassbyAudio;
        public bool HasEnteredRoadStrip;
        public Renderer HeadlightLeftRenderer;
        public Renderer HeadlightRightRenderer;
        public Material HeadlightLeftMaterial;
        public Material HeadlightRightMaterial;
        public Light HeadlightLeft;
        public Light HeadlightRight;
    }

    private sealed class LocalBusRouteData
    {
        public BusAgent Bus;
        public Transform RootTransform;
        public Renderer HeadlightLeftRenderer;
        public Renderer HeadlightRightRenderer;
        public Material HeadlightLeftMaterial;
        public Material HeadlightRightMaterial;
        public Light HeadlightLeft;
        public Light HeadlightRight;
        public DriverAgent Driver;
        public readonly List<Vector3> Waypoints = new();
        public int WaypointIndex;
        public int CurrentStopIndex = -1;
        public int TravelDirection = 1;
        public float DwellTimer;
        public float BobPhase;
        public float Speed;
        public int PassengerCount;
        public int PassengerCapacity = LocalBusMaxPassengers;
        public int Bank;
        public string LastBoardingBlockReason;
        public LocalBusPhase Phase = LocalBusPhase.None;
    }

    private enum LocalBusPhase
    {
        None,
        ParkedAwaitingShiftStart,
        DrivingRoute,
        WaitingAtStop,
        ReturningToParking
    }

    private sealed class HiringDriverArrivalData
    {
        public DriverAgent Driver;
        public readonly List<DriverAgent> Drivers = new();
        public bool IsTutorialWave;
        public bool IsMotelBootstrapWave;
        public bool HasNotifiedDisembark;
        public Transform BusRootTransform;
        public Renderer HeadlightLeftRenderer;
        public Renderer HeadlightRightRenderer;
        public Material HeadlightLeftMaterial;
        public Material HeadlightRightMaterial;
        public Light HeadlightLeft;
        public Light HeadlightRight;
        public float BusWorldX;
        public float BusSpeed;
        public float StopTimer;
        public float DisembarkTimer;
        public int NextDisembarkIndex;
        public float BobPhase;
        public HiringDriverArrivalPhase Phase;
    }

    private enum HiringDriverArrivalPhase
    {
        WaitingLaneClear,
        ApproachingStop,
        StoppedForDropoff,
        DriverWalkingToMotel,
        Departing
    }
    private TripPhase currentTripPhase = TripPhase.None;
    private RefuelPhase currentRefuelPhase = RefuelPhase.None;
    private TruckInteractionType activeTruckInteraction = TruckInteractionType.None;
    private TruckInteractionType queuedTruckInteraction = TruckInteractionType.None;
    private Quaternion truckInteractionTargetRotation;
    private Vector3 truckInteractionBuildingPoint;
    private LocationType? activeServiceLocation;
    private LocationType? queuedServiceLocation;
    private AudioClip uiSelectClip;
    private AudioClip menuHoverClip;
    private AudioClip uiPanelOpenClip;
    private AudioClip uiPanelCloseClip;
    private AudioClip truckIdleClip;
    private AudioClip truckRollClip;
    private AudioClip slotReelTickClip;
    private AudioClip slotWinClip;
    private AudioClip slotLoseClip;
    private AudioClip tutorialGoalSuccessClip;
    private AudioClip buildingCompleteClip;
    private AudioClip roadDragClip;
    private AudioClip buildingDemolishClip;
    private AudioClip[] workerGrassFootstepClips = new AudioClip[0];
    private AudioClip natureCicadasClip;
    private AudioClip natureForestBirdsClip;
    private AudioClip natureNightClip;
    private AudioClip natureRainCalmClip;
    private AudioClip natureRainStrongClip;
    private AudioClip natureRiverClip;
    private AudioClip natureWindCalmClip;
    private AudioClip natureWindForestClip;

    // HUD flash/shake for gambling result
    private float  hudFlashTimer;
    private float  hudFlashDuration;
    private Color  hudFlashColor;
    private float  hudShakeTimer;
    private float  hudShakeDuration;
    private AudioClip boatMotorClip;
    private bool wereAmbientLanternMothsActiveLastFrame;
    private bool wereAmbientFirefliesActiveLastFrame;
    private float riverFishSpawnTimer;
    private WeatherState currentWeatherState = WeatherState.Clear;
    private WeatherState nextWeatherState    = WeatherState.Clear;
    private WeatherParams activeWeatherParams;
    private float weatherHoldTimer = 180f;
    private float weatherTransitionTimer;
    private float weatherTransitionDuration;
    private bool  isWeatherTransitioning;
    private float weatherRainIntensity;
    private float lightningFlashTimer    = 18f;
    private float lightningFlashActive   = 0f;
    private float lightningFlashDuration = 0.2f;
    private Transform rainRoot;
    private readonly List<RainDropData> rainDrops = new();
    private float truckEngineAudioPhaseOffset;
    private float truckEngineAudioWobbleSpeed = 1f;
    private float truckEngineAudioPitchBias = 1f;
    private float truckEngineAudioVolumeBias = 1f;
    private float waterEffectsUpdateTimer;
    private int waterVisualLodLevel = -1;

    private enum LocationType
    {
        Parking,
        GasStation,
        Forest,
        Warehouse,
        Sawmill,
        FurnitureFactory,
        Motel,
        IntercityStop,
        Stop,
        Bar,
        Canteen,
        Kiosk,
        GamblingHall,
        CityPark,
        PersonalHouse,
        Kindergarten,
        CarMarket,
        LaborExchange,
        CleaningDepot,
        CityHall,
        Docks
    }

    /// <summary>
    /// Production locations generate or process cargo.
    /// Service locations support the truck route (fuel, rest, loading/unloading).
    /// </summary>
    private static bool IsProductionLocation(LocationType type) => type switch
    {
        LocationType.Forest           => true,
        LocationType.Sawmill          => true,
        LocationType.FurnitureFactory => true,
        LocationType.Warehouse        => true,
        LocationType.Docks            => true,
        _                             => false
    };

    private static bool IsServiceLocation(LocationType type) => !IsProductionLocation(type);

    private static bool HasServiceWorkerSlot(LocationType type) => type switch
    {
        LocationType.GasStation    => true,
        LocationType.Motel         => true,
        LocationType.Bar           => true,
        LocationType.Canteen       => true,
        LocationType.GamblingHall  => true,
        LocationType.Kindergarten   => true,
        LocationType.CarMarket     => true,
        LocationType.LaborExchange => true,
        LocationType.CleaningDepot => true,
        _                          => false
    };

    private static int GetMaxBuildingWorkerSlots(LocationType type) => GetBuildingWorkerScheduleSlotCount(type);

    private static string GetBuildingWorkerRoleLabel(LocationType type) => type switch
    {
        LocationType.Forest           => "Lumberjack",
        LocationType.Sawmill          => "Sawmill Worker",
        LocationType.FurnitureFactory => "Carpenter",
        LocationType.Warehouse        => "Warehouse Loader",
        LocationType.Docks            => "Dock Worker",
        LocationType.Kindergarten     => "Childcare Worker",
        LocationType.LaborExchange    => "Employment Clerk",
        LocationType.CleaningDepot    => "Cleaner",
        _ when HasServiceWorkerSlot(type) => "Service Worker",
        _                             => "Worker"
    };

    private bool IsLocationOperational(LocationType type) =>
        !IsProductionLocation(type) ||
        (locations.TryGetValue(type, out LocationData d) && d.Workers > 0);

    private enum CargoType
    {
        None,
        Logs,
        Boards,
        Cotton,
        Textile,
        Furniture
    }

    private enum TransportTask
    {
        None,
        ReturnToParking,
        PickUpAtForest,
        DeliverToSawmill,
        PickUpAtSawmill,
        DeliverToWarehouse,
        PickUpBoardsAtWarehouse,
        DeliverBoardsToFurnitureFactory,
        PickUpTextileAtWarehouse,
        DeliverTextileToFurnitureFactory,
        PickUpAtFurnitureFactory,
        DeliverFurnitureToWarehouse,
        PickUpLogsAtWarehouseForDocks,
        PickUpBoardsAtWarehouseForDocks,
        PickUpFurnitureAtWarehouseForDocks,
        DeliverCargoToDocks,
        PickUpCottonAtDocks,
        PickUpTextileAtDocks,
        PickUpFurnitureAtDocks,
        DeliverDocksCargoToWarehouse
    }

    private enum TruckInteractionType
    {
        None,
        LoadAtForest,
        UnloadAtSawmill,
        LoadAtSawmill,
        UnloadAtWarehouse,
        LoadBoardsAtWarehouse,
        LoadTextileAtWarehouse,
        UnloadBoardsAtFurnitureFactory,
        UnloadTextileAtFurnitureFactory,
        LoadAtFurnitureFactory,
        UnloadFurnitureAtWarehouse,
        TradeUnloadAtWarehouse,
        TradeLoadAtWarehouse,
        LoadLogsAtWarehouse,
        LoadFurnitureAtWarehouse,
        UnloadAtDocks,
        LoadAtDocks,
        UnloadDocksImportAtWarehouse,
        RefuelAtGasStation
    }

    private enum TripType
    {
        None,
        ForestToSawmill,
        ForestToWarehouse,
        SawmillToWarehouse,
        WarehouseToFurnitureFactoryBoards,
        WarehouseToFurnitureFactoryTextile,
        FurnitureFactoryToWarehouse,
        WarehouseToDocksLogs,
        WarehouseToDocksBoards,
        WarehouseToDocksFurniture,
        DocksToWarehouseCotton,
        DocksToWarehouseTextile,
        DocksToWarehouseFurniture
    }

    private enum BuildTool
    {
        None,
        Parking,
        Warehouse,
        SingleRoad,
        Road,
        Stop,
        Forest,
        FurnitureFactory,
        Sawmill,
        Motel,
        Bar,
        Canteen,
        Kiosk,
        GasStation,
        GamblingHall,
        CityPark,
        PersonalHouse,
        Kindergarten,
        CarMarket,
        LaborExchange,
        CleaningDepot,
        CityHall,
        Docks
    }

    private enum GameStartMode
    {
        Tutorial,
        NewGame
    }

    public enum TripPhase
    {
        None,
        ToPickup,
        Loading,
        ToDropoff,
        Unloading,
        ReturnToParking
    }

    public enum RefuelPhase
    {
        None,
        ToGasStation,
        Refueling,
        ReturnToParking
    }

    private enum DriverRescuePhase
    {
        None,
        IdleWander,
        ToMotelFromBusStop,
        ToGasStation,
        ToTruck,
        ToMotelEntrance,
        ToTruckAtMotel,
        ToParkingForShift,
        IdleWalkToBench,
        IdleSittingOnBench,
        IdleWalkToBar,
        IdleAtBar,
        IdleWalkToCanteen,
        IdleAtCanteen,
        IdleWalkToKiosk,
        IdleAtKiosk,
        IdleWalkToTrashCan,
        IdleAtTrashCan,
        IdleWalkToGamblingHall,
        IdleAtGamblingHall,
        IdleWalkToCityPark,
        IdleAtCityPark,
        IdleExitCityPark,
        IdleSmoking,
        IdlePhoneCall,
        IdleWalkToCat,
        IdlePettingCat,
        WalkToLocalBusStop,
        WaitingAtLocalBusStop,
        RidingLocalBus,
        ToBuildingForShift,        // walking motel -> production building (logistics pre-shift)
        ToMotelFromBuilding,       // walking building -> motel (logistics post-shift)
        LumberToTree,
        LumberChopping,
        LumberCarryLogToBuilding,
        LumberReturnToTreeForPlanting,
        LumberPlanting,
        LumberReturnToBuilding,
        ToPersonalHouseForPurchase,
        ToPersonalHouseEntrance,
        ToPersonalHouseMeal,
        IdleAtPersonalHouseMeal,
        ToPersonalHouseParking,
        ToCarMarketForPurchase,
        ToLaborExchangeForJob,
        ToIntercityStopForDeparture,
        CleanerToLitter,
        CleanerCleaning,
        CleanerReturnToDepot,
        AtLaborExchange
    }

    private enum DriverRestPhase
    {
        None,
        ToMotel,
        ParkAtMotel,
        DriverWalkToMotel,
        Sleeping,
        SleepingAtHome,
        DriverWalkToTruck,
        ReturnToParking
    }

    private enum WorkerLifeGoal
    {
        None,
        Work,
        Eat,
        Leisure,
        Sleep,
        BuyHouse,
        BuyCar,
        FindJob,
        Idle
    }

    private enum WorkerNeedKind
    {
        Meal,
        Sleep,
        Leisure
    }

    private enum WorkerNeedStatus
    {
        Ok,
        Warning,
        Critical
    }

    private enum DriverDutyMode
    {
        Local,
        Intercity,
        Logistics   // assigned directly to a production or service building
    }

    public enum TradeResourceType
    {
        Logs,
        Boards,
        Cotton,
        Textile,
        Furniture,
        Alcohol
    }

    public enum TradeOrderType
    {
        Buy,
        Sell
    }

    private enum DocksShipPhase
    {
        Waiting,
        Arriving,
        Docked,
        Departing
    }

    private sealed class LocationData
    {
        public int InstanceId;
        public LocationType Type;
        public string Label;
        public Vector2Int Min;
        public Vector2Int Max;
        public Vector2Int Anchor;
        public Vector2Int RoadAccess;
        public Color BaseColor;
        public int StopNumber;
        public int LogsStored;
        public int BoardsStored;
        public int CottonStored;
        public int TextileStored;
        public int FurnitureStored;
        public int DocksImportLogsStored;
        public int DocksImportBoardsStored;
        public int DocksImportCottonStored;
        public int DocksImportTextileStored;
        public int DocksImportFurnitureStored;
        public GameObject RootObject;
        public GameObject LocalBusWarningMarker;
        public GameObject RoadAccessWarningMarker;
        public Renderer BaseRenderer;
        public ImportedBuildingRuntime ImportedRuntime;
        public readonly List<GameObject> StoredLogVisuals = new();
        public readonly List<GameObject> StoredBoardVisuals = new();

        public int Workers;      // active assigned staff currently inside the building
        public int ServiceFee;   // Service buildings deduct from driver.Money on entry.
        public int BuildingBank; // Internal revenue: service fees in, gambling payouts out.
        public TradeResourceType DocksExportResource = TradeResourceType.Boards;
        public TradeResourceType DocksImportResource = TradeResourceType.Cotton;
        public float DocksShipTimer;
        public float DocksShipDockedTimer;
        public float DocksShipWorldX = DocksShipSpawnX;
        public DocksShipPhase DocksShipPhase;
        public bool DocksShipDocked;
        public GameObject DocksShipObject;

        public bool Contains(Vector2Int cell)
        {
            return cell.x >= Min.x && cell.x <= Max.x && cell.y >= Min.y && cell.y <= Max.y;
        }
    }

    private sealed class ImportedBuildingRuntime
    {
        public Transform DoorTransform;
        public Quaternion DoorClosedLocalRotation;
        public Quaternion DoorOpenLocalRotation;
        public float DoorOpenAmount;
        public float DoorTargetOpenAmount;
        public float DoorHoldTimer;
        public Transform DoorEnterMarker;
        public Transform DoorInsideMarker;
        public Transform VisitorStandMarker;
        public Transform TableLookAtMarker;
        public readonly List<ImportedBuildingDoor> Doors = new();
        public readonly List<ImportedBuildingSeat> Seats = new();
    }

    private sealed class ImportedBuildingDoor
    {
        public Transform DoorTransform;
        public Transform HingeTransform;
        public Quaternion ClosedLocalRotation;
        public Quaternion OpenLocalRotation;
    }

    private sealed class ImportedBuildingSeat
    {
        public Transform SeatMarker;
        public Transform LookAtMarker;
        public Vector3 SeatWorldPosition;
        public bool HasSeatWorldPosition;
        public int OccupantDriverId;
    }

    private sealed class TripOption
    {
        public TripType Type;
        public string Title;
        public string Description;
        public int Reward;
        public int Priority; // Higher values are selected first by auto logistics.
    }

    private enum ForestWorkerState
    {
        Walking,
        Chopping,
        Pausing
    }

    private sealed class ForestWorkerAmbient
    {
        public string Name;
        public GameObject RootObject;
        public Transform VisualRoot;
        public Transform BodyTransform;
        public Transform HeadTransform;
        public Transform CapTransform;
        public Transform LeftArmTransform;
        public Transform RightArmTransform;
        public Transform LeftLegTransform;
        public Transform RightLegTransform;
        public Transform AxeTransform;
        public Transform FlashlightTransform;
        public Light FlashlightLight;
        public Renderer FlashlightRenderer;
        public Material FlashlightMaterial;
        public Vector3 TargetWorldPosition;
        public ForestWorkerState State;
        public float MoveSpeed;
        public float StateTimer;
        public float AnimationTime;
        public float ChopSoundCooldown;
        public float PauseYaw;
        public int WorkPointIndex;
    }

    private sealed class ForestTreeWobble
    {
        public Transform TreeTransform;
        public Quaternion BaseRotation;
        public Vector3 Axis;
        public float Timer;
        public float Duration;
        public float Amplitude;
    }

    private sealed class MiscTreeSway
    {
        public Vector2Int Cell;
        public Transform RootTransform;
        public Quaternion BaseRotation;
        public float PhaseOffset;
        public float SecondaryPhaseOffset;
        public float Speed;
        public float PitchAmplitude;
        public float RollAmplitude;
        public float CurrentWindMult = 1f;
    }


}
