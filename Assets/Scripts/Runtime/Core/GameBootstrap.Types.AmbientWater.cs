using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private sealed class DistantCloudData
    {
        public Transform RootTransform;
        public Vector3 SpawnPosition;   // world position at TravelOffset = 0
        public float TravelOffset;      // current distance along CloudTravelDir
        public float TravelSpeed;       // units/sec
        public float VerticalBobAmplitude;
        public float VerticalBobSpeed;
        public float PhaseOffset;
    }

    private sealed class AmbientAirParticleData
    {
        public Transform RootTransform;
        public Renderer Renderer;
        public Material Material;
        public Vector3 Center;
        public float HalfTravelRange;
        public float HalfLateralRange;
        public float HeightMin;
        public float HeightMax;
        public float TravelOffset;
        public float LateralOffset;
        public float BaseHeightOffset;
        public float TravelSpeed;
        public float BobAmplitude;
        public float BobSpeed;
        public float PhaseOffset;
        public Color BaseColor;
        public bool IsForestLocal;
        public bool IsHighwayDust;
    }

    private sealed class ExhaustSmokeParticle
    {
        public Transform Transform;
        public Material Material;
        public Vector3 Velocity;
        public float LifeTimer;
        public float MaxLife;
        public float BaseScale;
        public bool IsActive;
    }

    private enum MiscBirdState
    {
        Perched,
        Flying
    }

    private sealed class NightStarData
    {
        public Transform Transform;
        public Material Material;
        public Color BaseColor;
        public float TwinkleSpeed;
        public float TwinklePhase;
    }

    private sealed class MiscBirdData
    {
        public Transform RootTransform;
        public Transform BodyTransform;
        public Transform LeftWingTransform;
        public Transform RightWingTransform;
        public Vector3 StartPosition;
        public Vector3 TargetPosition;
        public int CurrentPerchIndex;
        public int TargetPerchIndex;
        public float StateTimer;
        public float FlightDuration;
        public float FlightProgress;
        public float BobPhase;
        public float WingPhase;
        public float PerchYaw;
        public MiscBirdState State;
    }

    private enum AmbientCatState
    {
        Lazing,
        Walking,
        BeingPetted
    }

    private enum AmbientSquirrelState
    {
        Idle,
        Running,
        Foraging,
        ClimbingUp,
        ClimbingDown,
    }

    private sealed class AmbientSquirrelData
    {
        public Transform RootTransform;
        public Transform BodyTransform;
        public Transform HeadTransform;
        public Transform TailTransform;
        public Vector3 CurrentPosition;
        public Vector3 StartPosition;
        public Vector3 TargetPosition;
        public int CurrentPointIndex;
        public int TargetPointIndex;
        public float StateTimer;
        public float MoveDuration;
        public float MoveProgress;
        public float AnimationPhase;
        public float TailPhase;
        public float Yaw;
        public AmbientSquirrelState State;
        public bool IsAtTreeTop;
        public float ClimbProgress;
        public float ClimbDuration;
        public float ClimbCooldown;
    }

    private sealed class AmbientCatData
    {
        public Transform RootTransform;
        public Transform BodyTransform;
        public Transform HeadTransform;
        public Transform TailTransform;
        public bool UsesImportedModel;
        public Vector3 BodyBaseScale = Vector3.one;
        public Quaternion HeadBaseRotation = Quaternion.identity;
        public Quaternion TailBaseRotation = Quaternion.identity;
        public Transform[] LegTransforms;
        public Quaternion[] LegBaseRotations;
        public Vector3 CurrentPosition;
        public Vector3 StartPosition;
        public Vector3 TargetPosition;
        public int CurrentPointIndex;
        public int TargetPointIndex;
        public float StateTimer;
        public float MoveDuration;
        public float MoveProgress;
        public float AnimationPhase;
        public float TailPhase;
        public float Yaw;
        public bool IsRelocatingHome;
        public AmbientCatState State;
        public float PettingTimer;
        public int PettedByDriverId = -1;
    }

    private sealed class AmbientBeeData
    {
        public Transform RootTransform;
        public Renderer BodyRenderer;
        public Renderer StripeRenderer;
        public Renderer LeftWingRenderer;
        public Renderer RightWingRenderer;
        public Material BodyMaterial;
        public Material StripeMaterial;
        public Material LeftWingMaterial;
        public Material RightWingMaterial;
        public Transform LeftWingTransform;
        public Transform RightWingTransform;
        public int FlowerPointIndex;
        public float OrbitRadius;
        public float OrbitHeight;
        public float OrbitSpeed;
        public float OrbitAngle;
        public float VerticalBobAmplitude;
        public float VerticalBobSpeed;
        public float PhaseOffset;
        public float Visibility;
    }

    private sealed class AmbientLanternMothSwarmData
    {
        public Transform RootTransform;
        public readonly List<Transform> ParticleTransforms = new();
        public readonly List<Renderer> ParticleRenderers = new();
        public readonly List<Material> ParticleMaterials = new();
        public int LanternIndex = -1;
        public float OrbitRadius;
        public float OrbitHeight;
        public float OrbitSpeed;
        public float VerticalBobAmplitude;
        public float VerticalBobSpeed;
        public float PhaseOffset;
        public float Visibility;
    }

    private sealed class AmbientFireflyData
    {
        public Transform Transform;
        public Material Material;
        public Vector3 BasePosition;
        public float DriftPhaseX;
        public float DriftPhaseY;
        public float DriftPhaseZ;
        public float DriftSpeedX;
        public float DriftSpeedY;
        public float DriftSpeedZ;
        public float DriftRadius;
        public float BaseGroundY;
        public float GlowPhase;
        public float GlowSpeed;
        public float Visibility;
    }

    private enum AmbientFrogState { Sitting, Croaking, Hopping }

    private sealed class AmbientFrogData
    {
        public Transform RootTransform;
        public Transform BodyTransform;
        public Transform HeadTransform;
        public Vector3 CurrentPosition;
        public Vector3 StartPosition;
        public Vector3 TargetPosition;
        public int CurrentCellIndex;
        public float StateTimer;
        public float HopProgress;
        public float HopDuration;
        public float AnimPhase;
        public float Yaw;
        public AmbientFrogState State;
    }

    private enum WeatherState { Clear, Overcast, Rainy, Foggy, Windy }

    private struct WeatherParams
    {
        public float FogMult;
        public float SatOffset;
        public float ExposureOffset;
        public float WindMult;
        public float BloomScatterAdd;
    }

    private static readonly WeatherParams[] WeatherTargetParams =
    {
        new() { FogMult = 1.00f, SatOffset =   0f, ExposureOffset =  0.00f, WindMult = 1.0f, BloomScatterAdd = 0.00f },
        new() { FogMult = 0.82f, SatOffset = -10f, ExposureOffset = -0.08f, WindMult = 1.3f, BloomScatterAdd = 0.04f },
        new() { FogMult = 0.72f, SatOffset = -16f, ExposureOffset = -0.16f, WindMult = 1.8f, BloomScatterAdd = 0.08f },
        new() { FogMult = 0.66f, SatOffset =  -8f, ExposureOffset = -0.04f, WindMult = 0.6f, BloomScatterAdd = 0.08f },
        new() { FogMult = 0.92f, SatOffset =  +3f, ExposureOffset = +0.02f, WindMult = 2.8f, BloomScatterAdd = 0.00f },
    };

    private sealed class RainDropData
    {
        public Transform T;
        public Renderer Renderer;
        public Material Material;
        public float Y;
        public float Speed;
        public float XOff;
        public float ZOff;
    }

    private sealed class WaterSurfaceTileData
    {
        public Renderer Renderer;
        public Material Material;
        public Transform Transform;
        public Mesh Mesh;
        public float BaseY;
        public float CurrentTopY;
        public Vector2Int Cell;
        public float BobAmplitude;
        public float BobSpeed;
        public float PhaseOffset;
        public int LayerIndex;
    }

    private sealed class WaterBodyTileData
    {
        public Transform Transform;
        public Mesh Mesh;
        public float BaseY;
        public float BaseTopY;
        public float BottomY;
        public float CurrentTopY;
        public Vector2Int Cell;
        public float PhaseOffset;
    }

    private sealed class WaterShoreFoamData
    {
        public Transform RootTransform;
        public Renderer Renderer;
        public Material Material;
        public float BaseY;
        public float BaseZ;
        public float Width;
        public float DriftSpeed;
        public float DriftOffset;
        public float PulseSpeed;
        public float PhaseOffset;
    }

    private sealed class WaterShoreWashPatchData
    {
        public Transform RootTransform;
        public Renderer Renderer;
        public Material Material;
        public float BaseX;
        public float BaseY;
        public float BaseZ;
        public float Width;
        public float Depth;
        public int ShoreRingIndex;
        public int SegmentIndex;
        public float PhaseOffset;
    }

    private sealed class RiverFishData
    {
        public Transform RootTransform;
        public Transform BodyTransform;
        public Transform TailTransform;
        public Renderer BodyRenderer;
        public Renderer TailRenderer;
        public Material BodyMaterial;
        public Material TailMaterial;
        public float WorldX;
        public float WorldZ;
        public float SwimSpeed;
        public float DepthY;
        public float BobPhase;
        public float TailPhase;
        public float LateralDriftAmplitude;
        public float LateralDriftSpeed;
        public Color BodyColor;
    }

    private sealed class LakeFishData
    {
        public Transform RootTransform;
        public Transform BodyTransform;
        public Transform TailTransform;
        public Material BodyMaterial;
        public Material TailMaterial;
        public float WorldX;
        public float WorldZ;
        public float DepthY;
        public float BobPhase;
        public float TailPhase;
        public float IdleTimer;
        public float TargetX;
        public float TargetZ;
        public float SwimSpeed;
        public Color BodyColor;
        public int LakeIndex;
        public float Yaw;
        public bool IsJumping;
        public float JumpProgress;
        public float JumpDuration;
        public float JumpPeakHeight;
        public float JumpCooldown;
    }

}
