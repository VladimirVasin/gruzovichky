using UnityEngine;

public partial class GameBootstrap
{
    private const string ParkingImportedModelResourcePath = "Buildings/parking";
    private const float ParkingImportedModelFootprintFill = 2.21f;

    private bool TryCreateImportedParkingModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner,
            parent,
            center,
            min,
            max,
            anchor,
            LocationType.Parking,
            ParkingImportedModelResourcePath,
            "ParkingImportedModelRoot",
            "ParkingImportedModel",
            ParkingImportedModelFootprintFill,
            new Color(1f, 0.82f, 0.46f, 1f),
            new Color(1f, 0.68f, 0.30f));
    }
}
